﻿Imports cvb = OpenCvSharp
' https://opencv24-python-tutorials.readthedocs.io/en/latest/py_tutorials/py_imgproc/py_thresholding/py_thresholding.html
Public Class Threshold_Basics : Inherits TaskParent
    Public options As New Options_Threshold
    Public Sub New()
        labels(2) = "Original image"
        desc = "Demonstrate the use of OpenCV's threshold and all its options"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()

        labels(3) = "Image after thresholding with threshold = " + CStr(options.threshold)
        dst2 = src
        If options.inputGray Then dst2 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2Gray)
        If options.otsuOption Then options.thresholdMethod += cvb.ThresholdTypes.Otsu
        If (options.otsuOption Or options.thresholdMethod = cvb.ThresholdTypes.Triangle) And dst2.Channels() <> 1 Then
            dst2 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2Gray)
        End If
        dst3 = dst2.Threshold(options.threshold, 255, options.thresholdMethod)
    End Sub
End Class










' https://www.tutorialspoint.com/opencv/opencv_adaptive_threshold.htm
Public Class Threshold_Adaptive : Inherits TaskParent
    Dim options As New Options_Threshold
    Dim optionsAdaptive As New Options_AdaptiveThreshold
    Public Sub New()
        labels = {"", "", "Original input", "Output of AdaptiveThreshold"}
        desc = "Explore what adaptive threshold can do."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        optionsAdaptive.RunOpt()

        If src.Channels() <> 1 Then dst2 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY) Else dst2 = src
        dst3 = dst2.AdaptiveThreshold(255, optionsAdaptive.method, options.thresholdMethod,
                                      optionsAdaptive.blockSize, optionsAdaptive.constantVal)
    End Sub
End Class







' https://docs.opencvb.org/4.x/d7/d4d/tutorial_py_thresholding.html
Public Class Threshold_Definitions : Inherits TaskParent
    Dim gradient As New Gradient_Color
    Dim mats As New Mat_4to1
    Dim options As New Options_ThresholdDef
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        labels = {"Gradient input (from Gradient_Basics)", "Binary threshold output of Gradient input at left", "Clockwise: binaryInv, Trunc, ToZero, ToZeroInv", "Current selection"}
        desc = "Demonstrate BinaryInv, Trunc, ToZero, and ToZero_Inv threshold methods"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        gradient.Run(empty)
        dst0 = gradient.dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        dst1 = dst0.Threshold(options.threshold, 255, cvb.ThresholdTypes.Binary)
        mats.mat(0) = dst0.Threshold(options.threshold, 255, cvb.ThresholdTypes.BinaryInv)
        mats.mat(1) = dst0.Threshold(options.threshold, 255, cvb.ThresholdTypes.Trunc)
        mats.mat(2) = dst0.Threshold(options.threshold, 255, cvb.ThresholdTypes.Tozero)
        mats.mat(3) = dst0.Threshold(options.threshold, 255, cvb.ThresholdTypes.TozeroInv)
        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
        SetTrueText("Input Gradient Image", 0)
        SetTrueText("Binary", New cvb.Point(dst2.Width / 2 + 5, 10), 1)
        SetTrueText("BinaryInv", 2)
        SetTrueText("Trunc", New cvb.Point(dst2.Width / 2 + 5, 10), 2)
        SetTrueText("ToZero", New cvb.Point(10, dst2.Height / 2 + 10), 2)
        SetTrueText("ToZeroInv", New cvb.Point(dst2.Width / 2 + 5, dst2.Height / 2 + 10), 2)
        SetTrueText(
            vbCrLf + "Upper left:  the input for all the tests below..." + vbCrLf +
            vbCrLf + "Upper right: dst0.Threshold(threshold, 255, cvb.ThresholdTypes.Binary)" + vbCrLf +
            vbCrLf + "0: dst0.Threshold(threshold, 255, cvb.ThresholdTypes.BinaryInv)" + vbCrLf +
            vbCrLf + "1: dst0.Threshold(threshold, 255, cvb.ThresholdTypes.Trunc)" + vbCrLf +
            vbCrLf + "2: dst0.Threshold(threshold, 255, cvb.ThresholdTypes.Tozero)" + vbCrLf +
            vbCrLf + "1: dst0.Threshold(threshold, 255, cvb.ThresholdTypes.TozeroInv)" + vbCrLf,
        3)
    End Sub
End Class









Public Class Threshold_ByChannels : Inherits TaskParent
    Dim optionsColor As New Options_Colors
    Dim options As New Options_Threshold
    Public Sub New()
        labels(3) = "Threshold Inverse"
        FindRadio("Trunc").Checked = True
        UpdateAdvice(traceName + ": see local options.")
        desc = "Threshold by channel - use red threshold slider to impact grayscale results."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        optionsColor.RunOpt()

        If src.Channels = 1 Then
            dst2 = src.Threshold(optionsColor.redS, 255, options.thresholdMethod)
        ElseIf options.inputGray Then
            src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
            dst2 = src.Threshold(optionsColor.redS, 255, options.thresholdMethod)
        Else
            Dim split = src.Split()
            split(0) = split(0).Threshold(optionsColor.blueS, 255, options.thresholdMethod)
            split(1) = split(1).Threshold(optionsColor.greenS, 255, options.thresholdMethod)
            split(2) = split(2).Threshold(optionsColor.redS, 255, options.thresholdMethod)
            cvb.Cv2.Merge(split, dst2)
        End If
        dst3 = Not dst2
        labels(2) = "Threshold method: " + options.thresholdName
    End Sub
End Class










Public Class Threshold_ColorSource : Inherits TaskParent
    Dim color8U As New Color8U_Basics
    Dim byChan As New Threshold_ByChannels
    Public Sub New()
        UpdateAdvice(traceName + ": Use redOptions color source to change the input.  Also, see local options.")
        desc = "Use all the alternative color sources as input to Threshold_ByChannels."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        color8U.Run(src)
        byChan.Run(color8U.dst3)
        dst2 = byChan.dst2
        dst3 = byChan.dst3
        labels = byChan.labels
    End Sub
End Class
