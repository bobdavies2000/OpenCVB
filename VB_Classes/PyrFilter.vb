Imports cv = OpenCvSharp
'http://study.marearts.com/2014/12/opencv-meanshiftfiltering-example.html
Public Class PyrFilter_Basics : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("MeanShift Spatial Radius", 1, 100, 1)
            sliders.setupTrackBar("MeanShift color Radius", 1, 100, 20)
            sliders.setupTrackBar("MeanShift Max Pyramid level", 1, 8, 3)
        End If
        desc = "Use PyrMeanShiftFiltering to segment an image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static radiusSlider = findSlider("MeanShift Spatial Radius")
        Static colorSlider = findSlider("MeanShift color Radius")
        Static maxSlider = findSlider("MeanShift Max Pyramid level")
        cv.Cv2.PyrMeanShiftFiltering(src, dst2, radiusSlider.Value, colorSlider.Value, maxSlider.Value)
    End Sub
End Class







Public Class PyrFilter_RedCloud : Inherits VB_Algorithm
    Dim pyr As New PyrFilter_Basics
    Dim colorC As New RedCloudY_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        labels = {"", "", "RedCloudY_Basics output", "PyrFilter output before reduction"}
        desc = "Use RedColor to segment the output of PyrFilter"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        pyr.Run(src)
        dst3 = pyr.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        reduction.Run(dst3)

        colorC.Run(reduction.dst2)
        dst2 = colorC.dst2
        labels(2) = colorC.labels(2)
    End Sub
End Class