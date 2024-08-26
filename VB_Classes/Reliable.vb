Imports cvb = OpenCvSharp
Public Class Reliable_Basics : Inherits VB_Parent
    Dim bgs As New BGSubtract_Basics
    Dim relyDepth As New Reliable_Depth
    Dim diff
    Public Sub New()
        task.gOptions.setDisplay1()
        desc = "Identify each grid element with unreliable data or motion."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)

        bgs.Run(src)
        dst2 = bgs.dst2

        relyDepth.Run(src)
        dst3 = relyDepth.dst2
    End Sub
End Class







Public Class Reliable_Depth : Inherits VB_Parent
    Dim rDepth As New History_ReliableDepth
    Public Sub New()
        labels = {"", "", "Mask of Reliable depth data", "Task.DepthRGB after removing unreliable depth (compare with above.)"}
        desc = "Provide only depth that has been present over the last framehistory frames."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        rDepth.Run(task.noDepthMask)
        dst2 = rDepth.dst2

        If standaloneTest() Then
            dst3.SetTo(0)
            task.depthRGB.CopyTo(dst3, dst2)
        End If
    End Sub
End Class






Public Class Reliable_MaxDepth : Inherits VB_Parent
    Public options As New Options_MinMaxNone
    Public Sub New()
        desc = "Create a mas"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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





Public Class Reliable_Gray : Inherits VB_Parent
    Dim diff As New Motion_Diff
    Dim history As New History_Basics8U
    Dim options As New Options_Denoise
    Dim singles As New Denoise_SinglePixels_CPP_VB
    Public Sub New()
        task.gOptions.setPixelDifference(10)
        labels = {"", "Mask of unreliable data after denoising", "Mask of unreliable color data (before denoising", "Color image with unreliable pixels set to zero"}
        desc = "Accumulate those color pixels that are volatile - different by more than the global options 'Pixel Difference threshold'"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        diff.Run(src)
        history.Run(diff.dst2)
        dst2 = history.dst2
        If options.removeSinglePixels Then
            singles.Run(dst2)
            dst2 = singles.dst2
        End If

        If standalone Then
            dst3 = src.Clone
            dst3.SetTo(0, dst2)
        End If
    End Sub
End Class





Public Class Reliable_RGB : Inherits VB_Parent
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
    Public Sub RunAlg(src As cvb.Mat)
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








Public Class Reliable_CompareBGR : Inherits VB_Parent
    Dim relyBGR As New Reliable_RGB
    Dim relyGray As New Reliable_Gray
    Public Sub New()
        task.gOptions.setDisplay1()
        labels = {"", "Reliable_Gray output", "Compare Reliable_Gray and Reliable_RGB - if blank, they are the same.", "Reliable_RGB output"}
        desc = "Compare the results of Reliable_Color and Reliable_Gray"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        relyBGR.Run(src)
        dst1 = relyBGR.dst2
        relyGray.Run(src)
        dst3 = relyBGR.dst2

        dst2.SetTo(0)
        dst2.SetTo(cvb.Scalar.Yellow, dst1)
        dst2.SetTo(0, dst3)
        SetTrueText("if dst2 is blank, the Reliable_Gray and Reliable_RGB produce the same results.")
    End Sub
End Class







Public Class Reliable_Histogram : Inherits VB_Parent
    Public hist As New Hist_Basics
    Dim relyGray As New Reliable_Gray
    Public Sub New()
        task.gOptions.setDisplay1()
        task.gOptions.setHistogramBins(255)
        desc = "Create a histogram of reliable pixels"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        hist.Run(src)
        dst2 = hist.dst2.Clone
        labels(2) = hist.labels(2)

        relyGray.Run(src)
        dst1 = src And relyGray.dst2
        hist.Run(dst1)
        dst3 = hist.dst2
        labels(3) = "Histogram of unreliable grayscale pixels - pretty much everywhere."
    End Sub
End Class







Public Class Reliable_Edges : Inherits VB_Parent
    Dim edges As New Edge_Basics
    Dim denoise As New Denoise_SinglePixels_CPP_VB
    Dim relyGray As New Reliable_Gray
    Public Sub New()
        task.gOptions.setDisplay1()
        desc = "Does removing unreliable pixels improve the edge detection.  Unreliable pixels are concentrated in edges."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        relyGray.Run(src)

        dst1 = relyGray.dst2
        denoise.Run(dst1)
        dst3 = denoise.dst2

        src.SetTo(0, dst3)

        edges.Run(src)
        dst2 = edges.dst2
    End Sub
End Class

