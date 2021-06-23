Imports cv = OpenCvSharp
Public Class Contrast_POW : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Contrast exponent to use X100", 0, 200, 30)
        End If
        labels(2) = "Original Image"
        labels(3) = "Contrast reduced"
        task.desc = "Reduce contrast with POW function"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2.ConvertTo(dst3, cv.MatType.CV_32FC3)
        dst3 = dst3.Normalize()

        Dim exponent = sliders.trackbar(0).Value / 100
        dst3 = dst3.Pow(exponent)
    End Sub
End Class






Public Class Contrast_Basics : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Brightness", 1, 100, 50)
            sliders.setupTrackBar(1, "Contrast", 1, 100, 50)
        End If
        task.desc = "Show image with varying contrast and brightness."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        src.ConvertTo(dst2, -1, sliders.trackbar(1).Value / 50, sliders.trackbar(0).Value)
        labels(2) = "Brightness/Contrast"
        labels(3) = ""
    End Sub
End Class

