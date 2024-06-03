Imports cv = OpenCvSharp
Public Class Brightness_Basics : Inherits VB_Parent
    Dim Options As New Options_BrightnessContrast
    Public Sub New()
        desc = "Implement a brightness effect"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Options.RunVB()

        dst2 = src.ConvertScaleAbs(Options.alpha, Options.beta)
        labels(3) = "Brightness level = " + CStr(Options.beta)
    End Sub
End Class






' https://github.com/spmallick/learnopencv/blob/master/Photoshop-Filters-in-OpenCV/brightness.cpp
Public Class Brightness_HSV : Inherits VB_Parent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Brightness Value", 0, 150, 100)
        labels(3) = "HSV image"
        desc = "Implement the brightness effect for HSV images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static brightnessSlider = findSlider("Brightness Value")
        Dim brightness As Single = brightnessSlider.Value / 100

        dst3 = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim hsv64 As New cv.Mat
        dst3.ConvertTo(hsv64, cv.MatType.CV_64F)
        Dim split = hsv64.Split()

        split(1) *= brightness
        split(1) = split(1).Threshold(255, 255, cv.ThresholdTypes.Trunc)

        split(2) *= brightness
        split(2) = split(2).Threshold(255, 255, cv.ThresholdTypes.Trunc)

        cv.Cv2.Merge(split, hsv64)
        hsv64.ConvertTo(dst2, cv.MatType.CV_8UC3)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        labels(2) = "Brightness level = " + CStr(brightnessSlider.Value)
    End Sub
End Class
