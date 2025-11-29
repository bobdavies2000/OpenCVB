Imports cv = OpenCvSharp
Public Class AddWeighted_Accumulate : Inherits TaskParent
    Dim options As New Options_AddWeighted
    Public Sub New()
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        labels(3) = "Instantaneous gray scale image"
        desc = "Update a running average of the image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If src.Channels <> 1 Then src = task.gray
        src.ConvertTo(dst3, cv.MatType.CV_32F)
        cv.Cv2.AccumulateWeighted(dst3, dst1, options.accumWeighted, New cv.Mat)
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)
        labels(2) = "Accumulated gray scale image"
    End Sub
End Class






Public Class AddWeighted_Basics : Inherits TaskParent
    Public src2 As cv.Mat  ' user normally provides src2! 
    Public options As New Options_AddWeighted
    Public weight As Double
    Public Sub New()
        desc = "Add 2 images with specified weights."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standalone Then src2 = task.depthRGB
        If src2.Type <> src.Type Then
            If src.Type <> cv.MatType.CV_8UC3 Or src2.Type <> cv.MatType.CV_8UC3 Then
                If src.Type = cv.MatType.CV_32FC1 Then src = Convert32f_To_8UC3(src)
                If src2.Type = cv.MatType.CV_32FC1 Then src2 = Convert32f_To_8UC3(src2)
                If src.Type <> cv.MatType.CV_8UC3 Then src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                If src2.Type <> cv.MatType.CV_8UC3 Then src2 = src2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            End If
        End If

        weight = options.addWeighted
        cv.Cv2.AddWeighted(src, weight, src2, 1.0 - weight, 0, dst2)
        labels(2) = $"Depth %: {100 - weight * 100} BGR %: {CInt(weight * 100)}"
    End Sub
End Class





Public Class AddWeighted_DepthAccumulate : Inherits TaskParent
    Dim options As New Options_AddWeighted
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        desc = "Update a running average of the image"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        cv.Cv2.AccumulateWeighted(task.pcSplit(2) * 1000, dst2, options.accumWeighted, New cv.Mat)
    End Sub
End Class









Public Class AddWeighted_InfraRed : Inherits TaskParent
    Public Sub New()
        desc = "Align the depth data with the left or right view.  Oak-D is aligned with the right image.  Some cameras are not close to aligned."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.toggleOn Then
            dst1 = task.leftView
            labels(2) = "Left view combined with depthRGB"
        Else
            dst1 = task.rightView
            labels(2) = "Right view combined with depthRGB"
        End If

        dst2 = ShowAddweighted(dst1, task.depthRGB, labels(3))
    End Sub
End Class







Public Class AddWeighted_Edges : Inherits TaskParent
    Dim edges As New Edge_Basics
    Public Sub New()
        labels = {"", "", "Edges_BinarizedSobel output", "AddWeighted edges and BGR image"}
        desc = "Add in the edges separating light and dark to the color image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(src)
        dst2 = edges.dst2
        labels(2) = edges.labels(2)

        dst3 = ShowAddweighted(edges.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR), src, labels(3))
    End Sub
End Class







Public Class AddWeighted_LeftRight : Inherits TaskParent
    Public Sub New()
        desc = "Use AddWeighted to add the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = ShowAddweighted(task.rightView, task.leftView, labels(2))
    End Sub
End Class






