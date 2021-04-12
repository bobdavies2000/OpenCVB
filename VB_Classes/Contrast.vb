Imports cv = OpenCvSharp
Public Class Contrast_POW
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Contrast exponent to use X100", 0, 200, 30)
        End If
        label1 = "Original Image"
        label2 = "Contrast reduced"
        task.desc = "Reduce contrast with POW function"
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1.ConvertTo(dst2, cv.MatType.CV_32FC3)
        dst2 = dst2.Normalize()

        Dim exponent = sliders.trackbar(0).Value / 100
        dst2 = dst2.Pow(exponent)
    End Sub
End Class






Public Class Contrast_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Brightness", 1, 100, 50)
            sliders.setupTrackBar(1, "Contrast", 1, 100, 50)
        End If
        task.desc = "Show image with varying contrast and brightness."
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        src.ConvertTo(dst1, -1, sliders.trackbar(1).Value / 50, sliders.trackbar(0).Value)
        label1 = "Brightness/Contrast"
        label2 = ""
    End Sub
End Class

