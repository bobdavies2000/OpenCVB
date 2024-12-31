Imports cvb = OpenCvSharp
Public Class Reliable_Basics : Inherits TaskParent
    Dim bgs As New BGSubtract_Basics
    Dim relyDepth As New Reliable_Depth
    Dim diff
    Public Sub New()
        task.gOptions.setDisplay1()
        desc = "Identify each grid element with unreliable data or motion."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)

        bgs.Run(src)
        dst2 = bgs.dst2

        relyDepth.Run(src)
        dst3 = relyDepth.dst2
    End Sub
End Class







Public Class Reliable_Depth : Inherits TaskParent
    Dim rDepth As New History_ReliableDepth
    Public Sub New()
        labels = {"", "", "Mask of Reliable depth data", "Task.DepthRGB after removing unreliable depth (compare with above.)"}
        desc = "Provide only depth that has been present over the last framehistory frames."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        rDepth.Run(task.noDepthMask)
        dst2 = rDepth.dst2

        If standaloneTest() Then
            dst3.SetTo(0)
            task.depthRGB.CopyTo(dst3, dst2)
        End If
    End Sub
End Class






Public Class Reliable_MaxDepth : Inherits TaskParent
    Public options As New Options_MinMaxNone
    Public Sub New()
        desc = "Create a mas"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        options.RunOpt()
        Dim split() As cvb.Mat
        If src.Type = cvb.MatType.CV_32FC3 Then split = src.Split() Else split = task.pcSplit

        If task.heartBeat Then
            dst3 = split(2)
        End If
        If options.useMax Then
            labels(2) = "Point cloud maximum values at each pixel"
            cvb.Cv2.Max(split(2), dst3, split(2))
        End If
        If options.useMin Then
            labels(2) = "Point cloud minimum values at each pixel"
            Dim saveMat = split(2).Clone
            cvb.Cv2.Min(split(2), dst3, split(2))
            Dim mask = split(2).InRange(0, 0.1)
            saveMat.CopyTo(split(2), mask)
        End If
        cvb.Cv2.Merge(split, dst2)
        dst3 = split(2)
    End Sub
End Class





Public Class Reliable_RGB : Inherits TaskParent
    Dim diff(2) As Motion_Diff
    Dim history(2) As History_Basics8U
    Public Sub New()
        For i = 0 To diff.Count - 1
            diff(i) = New Motion_Diff
            history(i) = New History_Basics8U
        Next
        task.gOptions.setPixelDifference(10)
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
        labels = {"", "", "Mask of unreliable color data", "Color image after removing unreliable pixels"}
        desc = "Accumulate those color pixels that are volatile - different by more than the global options 'Pixel Difference threshold'"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        dst3 = src.Clone
        dst2.SetTo(0)
        For i = 0 To diff.Count - 1
            diff(i).Run(src)
            history(i).Run(diff(i).dst2)
            dst2 = dst2 Or history(i).dst2
        Next
        dst3.SetTo(0, dst2)
    End Sub
End Class