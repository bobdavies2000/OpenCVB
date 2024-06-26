Imports cv = OpenCvSharp
Public Class Contrast_POW : Inherits VB_Parent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Contrast exponent to use X100", 0, 200, 30)
        labels = {"", "", "Original Image", "Contrast reduced with POW function"}
        desc = "Reduce contrast with POW function"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static expSlider = FindSlider("Contrast exponent to use X100")
        dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2.ConvertTo(dst3, cv.MatType.CV_32FC3)
        dst3 = dst3.Normalize()
        dst3 = dst3.Pow(expSlider.Value / 100)
    End Sub
End Class






Public Class Contrast_Basics : Inherits VB_Parent
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Brightness", 1, 100, 1)
            sliders.setupTrackBar("Contrast", 1, 100, 100)
        End If
        labels(2) = "Brightness/Contrast"
        UpdateAdvice(traceName + ": use the local options to control brightness and contrast.")
        desc = "Show image with varying contrast and brightness."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static brightnessSlider = FindSlider("Brightness")
        Static contrastSlider = FindSlider("Contrast")
        src.ConvertTo(dst2, -1, contrastSlider.Value / 50, brightnessSlider.Value)
    End Sub
End Class

