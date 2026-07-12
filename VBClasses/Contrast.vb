Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class XR_Contrast_Basics : Inherits TaskParent
    Dim options As New Options_BrightnessContrast
    Public Sub New()
        labels(2) = "Brightness/Contrast"
        desc = "Show image with varying contrast and brightness."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        ConvertScaleAbs(src, dst2, options.brightness, options.contrast)
    End Sub
End Class






Public Class XR_Contrast_POW : Inherits TaskParent
    Dim options As New Options_BrightnessContrast
    Public Sub New()
        labels = {"", "", "Original Image", "Contrast reduced with POW function"}
        desc = "Reduce contrast with POW function"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = task.gray
        dst2.ConvertTo(dst3, MatType.CV_32FC3)
        Normalize(dst3, dst3)
        Pow(dst3, options.exponent, dst3)
    End Sub
End Class
