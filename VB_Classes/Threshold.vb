Imports cv = OpenCvSharp
' https://opencv24-python-tutorials.readthedocs.io/en/latest/py_tutorials/py_imgproc/py_thresholding/py_thresholding.html
Public Class Threshold_Basics : Inherits VB_Algorithm
    Public options As New Options_Threshold
    Public Sub New()
        labels(2) = "Original image"
        desc = "Demonstrate the use of OpenCV's threshold and all its options"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        labels(3) = "Image after thresholding with threshold = " + CStr(options.threshold)
        dst2 = src
        If options.inputGray Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If options.otsuOption Then options.thresholdOption += cv.ThresholdTypes.Otsu
        If (options.otsuOption Or options.thresholdOption = cv.ThresholdTypes.Triangle) And dst2.Channels <> 1 Then
            dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        dst3 = dst2.Threshold(options.threshold, options.maxVal, options.thresholdOption)
    End Sub
End Class










' https://www.tutorialspoint.com/opencv/opencv_adaptive_threshold.htm
Public Class Threshold_Adaptive : Inherits VB_Algorithm
    Dim options As New Options_Threshold
    Dim optionsAd As New Options_Threshold_Adaptive
    Public Sub New()
        labels = {"", "", "Original input", "Output of AdaptiveThreshold"}
        desc = "Explore what adaptive threshold can do."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        optionsAd.RunVB()

        If src.Channels <> 1 Then dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else dst2 = src
        dst3 = dst2.AdaptiveThreshold(options.maxVal, optionsAd.method, options.thresholdOption, optionsAd.blockSize, optionsAd.constantVal)
    End Sub
End Class







' https://docs.opencv.org/4.x/d7/d4d/tutorial_py_thresholding.html
Public Class Threshold_Definitions : Inherits VB_Algorithm
    Dim gradient As New Gradient_Color
    Dim mats As New Mat_4Click
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold", 0, 255, 127)
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"Gradient input (from Gradient_Basics)", "Binary threshold output of Gradient input at left", "Clockwise: binaryInv, Trunc, ToZero, ToZeroInv", "Current selection"}
        desc = "Demonstrate BinaryInv, Trunc, ToZero, and ToZero_Inv threshold methods"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static truncateSlider = findSlider("Threshold")
        Dim threshold = truncateSlider.value
        gradient.Run(Nothing)
        dst0 = gradient.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = dst0.Threshold(threshold, 255, cv.ThresholdTypes.Binary)
        mats.mat(0) = dst0.Threshold(threshold, 255, cv.ThresholdTypes.BinaryInv)
        mats.mat(1) = dst0.Threshold(threshold, 255, cv.ThresholdTypes.Trunc)
        mats.mat(2) = dst0.Threshold(threshold, 255, cv.ThresholdTypes.Tozero)
        mats.mat(3) = dst0.Threshold(threshold, 255, cv.ThresholdTypes.TozeroInv)
        mats.Run(Nothing)
        dst2 = mats.dst2
        dst3 = mats.dst3
        setTrueText("Input Gradient Image", 0)
        setTrueText("Binary", New cv.Point(dst2.Width / 2 + 5, 10), 1)
        setTrueText("BinaryInv", 2)
        setTrueText("Trunc", New cv.Point(dst2.Width / 2 + 5, 10), 2)
        setTrueText("ToZero", New cv.Point(10, dst2.Height / 2 + 10), 2)
        setTrueText("ToZeroInv", New cv.Point(dst2.Width / 2 + 5, dst2.Height / 2 + 10), 2)
        setTrueText("Current selection from grid at left", 3)
    End Sub
End Class
