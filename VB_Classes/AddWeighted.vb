Imports cv = OpenCvSharp
Imports System.Threading
Public Class AddWeighted_Basics : Inherits VB_Algorithm
    Public src2 As cv.Mat
    Public Sub New()
        desc = "Add 2 images with specified weights."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim srcPlus = src2
        If standalone Or src2 Is Nothing Then srcPlus = task.depthRGB ' algorithm user normally provides src2! 
        If srcPlus.Type <> src.Type Then
            If src.Type <> cv.MatType.CV_8UC3 Or srcPlus.Type <> cv.MatType.CV_8UC3 Then
                If src.Type = cv.MatType.CV_32FC1 Then src = vbNormalize32f(src)
                If srcPlus.Type = cv.MatType.CV_32FC1 Then srcPlus = vbNormalize32f(srcPlus)
                If src.Type <> cv.MatType.CV_8UC3 Then src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                If srcPlus.Type <> cv.MatType.CV_8UC3 Then srcPlus = srcPlus.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            End If
        End If
        Dim wt = task.AddWeighted
        cv.Cv2.AddWeighted(src, wt, srcPlus, 1.0 - wt, 0, dst2)
        labels(2) = "depth " + CStr(CInt(1 - wt) * 100) + " BGR " + CStr(CInt(wt * 100))
    End Sub
End Class






Public Class AddWeighted_Edges : Inherits VB_Algorithm
    Dim edges As New Edge_All
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        labels = {"", "", "Edges_BinarizedSobel output", "AddWeighted edges and BGR image"}
        desc = "Add in the edges separating light and dark to the color image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        edges.Run(src)
        dst2 = edges.dst2
        labels(2) = edges.labels(2)

        addw.src2 = edges.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        addw.Run(src)
        dst3 = addw.dst2
    End Sub
End Class








Public Class AddWeighted_ImageAccumulate : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Accumulation weight of each image X100", 1, 100, 10)
        desc = "Update a running average of the image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static weightSlider = findSlider("Accumulation weight of each image X100")
        If task.optionsChanged Then dst2 = task.pcSplit(2) * 1000
        cv.Cv2.AccumulateWeighted(task.pcSplit(2) * 1000, dst2, weightSlider.Value / 100, New cv.Mat)
    End Sub
End Class







Public Class AddWeighted_InfraRed : Inherits VB_Algorithm
    Dim addw As New AddWeighted_Basics
    Dim src2 As New cv.Mat
    Public Sub New()
        desc = "Align the depth data with the left or right view.  Oak-D is aligned with the right image.  Some cameras are not close to aligned."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.toggleEverySecond Then
            dst1 = task.leftview
            labels(2) = "Left view combined with depthRGB"
        Else
            dst1 = task.rightview
            labels(2) = "Right view combined with depthRGB"
        End If

        addw.src2 = dst1
        addw.Run(task.depthRGB)
        dst2 = addw.dst2.Clone
    End Sub
End Class