﻿Imports cvb = OpenCvSharp
Public Class Brightness_Basics : Inherits TaskParent
    Dim Options As New Options_BrightnessContrast
    Public Sub New()
        desc = "Implement a brightness effect"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
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
    Public Overrides sub runAlg(src As cvb.Mat)
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







Public Class Brightness_Grid : Inherits TaskParent
    Dim bright As New Brightness_Basics
    Public brightRect As cvb.Rect
    Public Sub New()
        desc = "Adjust the brightness to get all gray levels below X (here 200) - no whiteout."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        Static alphaSlider = FindSlider("Alpha (contrast)")

        bright.Run(src)
        dst2 = bright.dst2

        Dim meanVals As New List(Of Single)
        For Each r In task.gridRects
            meanVals.Add(dst2(r).Mean()(0))
        Next

        Dim max = meanVals.Max
        If max > 200 Then
            Dim nextVal = alphaSlider.value - 10
            If nextVal > 0 Then alphaSlider.value = nextVal
        End If
        brightRect = task.gridRects(meanVals.IndexOf(max))
        If standaloneTest() Then
            dst3.SetTo(0)
            dst2(brightRect).CopyTo(dst3(brightRect))
        End If
    End Sub
End Class
