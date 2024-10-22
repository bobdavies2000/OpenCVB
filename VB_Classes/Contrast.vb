Imports cvb = OpenCvSharp
Public Class Contrast_POW : Inherits TaskParent
    Dim options As New Options_BrightnessContrast
    Public Sub New()
        labels = {"", "", "Original Image", "Contrast reduced with POW function"}
        desc = "Reduce contrast with POW function"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        dst2.ConvertTo(dst3, cvb.MatType.CV_32FC3)
        dst3 = dst3.Normalize()
        dst3 = dst3.Pow(options.exponent)
    End Sub
End Class






Public Class Contrast_Basics : Inherits TaskParent
    Dim options As New Options_BrightnessContrast
    Public Sub New()
        labels(2) = "Brightness/Contrast"
        UpdateAdvice(traceName + ": use the local options to control brightness and contrast.")
        desc = "Show image with varying contrast and brightness."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = src.ConvertScaleAbs(options.brightness, options.contrast)
    End Sub
End Class

