Imports cvb = OpenCvSharp
Public Class AddWeighted_Basics : Inherits VB_Parent
    Public src2 As cvb.Mat  ' user normally provides src2! 
    Public options As New Options_AddWeighted
    Public weight As Double
    Public Sub New()
        UpdateAdvice(traceName + ": use the local option slider 'Add Weighted %'")
        desc = "Add 2 images with specified weights."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If standalone Then src2 = task.depthRGB
        If src2.Type <> src.Type Then
            If src.Type <> cvb.MatType.CV_8UC3 Or src2.Type <> cvb.MatType.CV_8UC3 Then
                If src.Type = cvb.MatType.CV_32FC1 Then src = Convert32f_To_8UC3(src)
                If src2.Type = cvb.MatType.CV_32FC1 Then src2 = Convert32f_To_8UC3(src2)
                If src.Type <> cvb.MatType.CV_8UC3 Then src = src.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
                If src2.Type <> cvb.MatType.CV_8UC3 Then src2 = src2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
            End If
        End If

        weight = options.addWeighted
        cvb.Cv2.AddWeighted(src, weight, src2, 1.0 - weight, 0, dst2)
        labels(2) = $"Depth %: {100 - weight * 100} BGR %: {CInt(weight * 100)}"
    End Sub
End Class









Public Class AddWeighted_DepthAccumulate : Inherits VB_Parent
    Dim options As New Options_AddWeighted
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_32F, 0)
        desc = "Update a running average of the image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        cvb.Cv2.AccumulateWeighted(task.pcSplit(2) * 1000, dst2, options.accumWeighted, New cvb.Mat)
    End Sub
End Class







Public Class AddWeighted_InfraRed : Inherits VB_Parent
    Dim addw As New AddWeighted_Basics
    Dim src2 As New cvb.Mat
    Public Sub New()
        desc = "Align the depth data with the left or right view.  Oak-D is aligned with the right image.  Some cameras are not close to aligned."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.toggleOnOff Then
            dst1 = task.leftView
            labels(2) = "Left view combined with depthRGB"
        Else
            dst1 = task.rightView
            labels(2) = "Right view combined with depthRGB"
        End If

        addw.src2 = dst1
        addw.Run(task.depthRGB)
        dst2 = addw.dst2.Clone
    End Sub
End Class







Public Class AddWeighted_Edges : Inherits VB_Parent
    Dim edges As New Edge_Basics
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        labels = {"", "", "Edges_BinarizedSobel output", "AddWeighted edges and BGR image"}
        desc = "Add in the edges separating light and dark to the color image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        edges.Run(src)
        dst2 = edges.dst2
        labels(2) = edges.labels(2)

        addw.src2 = edges.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        addw.Run(src)
        dst3 = addw.dst2
    End Sub
End Class

