Imports cvb = OpenCvSharp
Public Class Brightness_Basics : Inherits TaskParent
    Dim Options As New Options_BrightnessContrast
    Public Sub New()
        desc = "Implement a brightness effect"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()

        dst2 = src.ConvertScaleAbs(Options.brightness, Options.contrast)
        labels(3) = "Brightness level = " + CStr(Options.contrast)
    End Sub
End Class






' https://github.com/spmallick/learnopencv/blob/master/Photoshop-Filters-in-OpenCV/brightness.cpp
Public Class Brightness_HSV : Inherits TaskParent
    Dim options As New Options_BrightnessContrast
    Public Sub New()
        labels(3) = "HSV image"
        desc = "Implement the brightness effect for HSV images"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst3 = src.CvtColor(cvb.ColorConversionCodes.BGR2HSV)
        Dim hsv64 As New cvb.Mat
        dst3.ConvertTo(hsv64, cvb.MatType.CV_64F)
        Dim split = hsv64.Split()

        split(1) *= options.hsvBrightness
        split(1) = split(1).Threshold(255, 255, cvb.ThresholdTypes.Trunc)

        split(2) *= options.hsvBrightness
        split(2) = split(2).Threshold(255, 255, cvb.ThresholdTypes.Trunc)

        cvb.Cv2.Merge(split, hsv64)
        hsv64.ConvertTo(dst2, cvb.MatType.CV_8UC3)
        dst2 = dst2.CvtColor(cvb.ColorConversionCodes.HSV2BGR)
        labels(2) = "Brightness level = " + CStr(options.hsvBrightness)
    End Sub
End Class
