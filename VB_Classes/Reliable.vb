Imports cv = OpenCvSharp
Public Class Reliable_Depth : Inherits VB_Parent
    Dim rDepth As New History_ReliableDepth
    Public Sub New()
        task.reliableDepthMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32F)
        labels = {"", "", "Mask of unreliable depth data", "Task.DepthRGB after removing unreliable depth (compare with above.)"}
        desc = "Provide only depth that has been present over the last framehistory frames."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rDepth.Run(task.noDepthMask)
        dst3.SetTo(0)
        task.reliableDepthMask = rDepth.dst2
        If standaloneTest() Then task.pcSplit(2).CopyTo(dst3, task.reliableDepthMask)
    End Sub
End Class






Public Class Reliable_MaxDepth : Inherits VB_Parent
    Public options As New Options_MinMaxNone
    Public Sub New()
        desc = "Create a mas"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim split() As cv.Mat
        If src.Type = cv.MatType.CV_32FC3 Then split = src.Split() Else split = task.pcSplit

        If task.heartBeat Then
            dst3 = split(2)
        End If
        If options.useMax Then
            labels(2) = "Point cloud maximum values at each pixel"
            cv.Cv2.Max(split(2), dst3, split(2))
        End If
        If options.useMin Then
            labels(2) = "Point cloud minimum values at each pixel"
            Dim saveMat = split(2).Clone
            cv.Cv2.Min(split(2), dst3, split(2))
            Dim mask = split(2).InRange(0, 0.1)
            saveMat.CopyTo(split(2), mask)
        End If
        cv.Cv2.Merge(split, dst2)
        dst3 = split(2)
    End Sub
End Class