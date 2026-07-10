Imports cv = OpenCvSharp
' https://opencv24-python-tutorials.readthedocs.io/en/latest/py_tutorials/py_imgproc/py_thresholding/py_thresholding.html
Public Class Threshold_Basics : Inherits TaskParent
    Public options As New Options_Threshold
    Public Sub New()
        labels(2) = "Original image"
        desc = "Demonstrate the use of OpenCV's threshold and all its options"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        labels(3) = "Image after thresholding with threshold = " + CStr(options.threshold)
        dst2 = src
        If options.inputGray Then cv.Cv2.CvtColor(dst2, dst2, cv.ColorConversionCodes.BGR2GRAY)
        If options.otsuOption Then options.thresholdMethod += cv.ThresholdTypes.Otsu
        If (options.otsuOption Or options.thresholdMethod = cv.ThresholdTypes.Triangle) And dst2.Channels() <> 1 Then
            cv.Cv2.CvtColor(dst2, dst2, cv.ColorConversionCodes.BGR2GRAY)
        End If
        cv.Cv2.Threshold(dst2, dst3, options.threshold, 255, options.thresholdMethod)
    End Sub
End Class










' https://www.tutorialspoint.com/opencv/opencv_adaptive_threshold.htm
Public Class XR_Threshold_Adaptive : Inherits TaskParent
    Dim options As New Options_Threshold
    Dim optionsAdaptive As New Options_AdaptiveThreshold
    Public Sub New()
        labels = {"", "", "Original input", "Output of AdaptiveThreshold"}
        desc = "Explore what adaptive threshold can do."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        optionsAdaptive.Run()

        If src.Channels() <> 1 Then dst2 = task.gray Else dst2 = src
        cv.Cv2.AdaptiveThreshold(dst2, dst3, 255, optionsAdaptive.method, options.thresholdMethod, optionsAdaptive.blockSize, optionsAdaptive.constantVal)
    End Sub
End Class







' https://docs.opencvb.org/4.x/d7/d4d/tutorial_py_thresholding.html
Public Class Threshold_Definitions : Inherits TaskParent
    Dim gradient As New Gradient_Color
    Dim mats As New Mat_4to1
    Dim options As New Options_ThresholdDef
    Public Sub New()
        If standalone Then task.gOptions.displayDst0.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"Gradient input (from Gradient_Basics)", "Binary threshold output of Gradient input at left", "Clockwise: binaryInv, Trunc, ToZero, ToZeroInv", "Current selection"}
        desc = "Demonstrate BinaryInv, Trunc, ToZero, and ToZero_Inv threshold methods"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        gradient.Run(src)
        cv.Cv2.CvtColor(gradient.dst2, dst0, cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.Flip(dst0, dst0, cv.FlipMode.Y)
        cv.Cv2.Threshold(dst0, dst1, options.threshold, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.Threshold(dst0, mats.mat(0), options.threshold, 255, cv.ThresholdTypes.BinaryInv)
        cv.Cv2.Threshold(dst0, mats.mat(1), options.threshold, 255, cv.ThresholdTypes.Trunc)
        cv.Cv2.Threshold(dst0, mats.mat(2), options.threshold, 255, cv.ThresholdTypes.Tozero)
        cv.Cv2.Threshold(dst0, mats.mat(3), options.threshold, 255, cv.ThresholdTypes.TozeroInv)
        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3
        SetTrueText("Input Gradient Image", 0)
        SetTrueText("Binary", New cv.Point(dst2.Width / 2 + 5, 10), 1)
        SetTrueText("BinaryInv", 2)
        SetTrueText("Trunc", New cv.Point(dst2.Width / 2 + 5, 10), 2)
        SetTrueText("ToZero", New cv.Point(10, dst2.Height / 2 + 10), 2)
        SetTrueText("ToZeroInv", New cv.Point(dst2.Width / 2 + 5, dst2.Height / 2 + 10), 2)
        Dim thresh = CStr(options.threshold)
        strOut = "Upper left:  the input for all the tests below..." + vbCrLf
        strOut += "Upper right:cv.Cv2.Threshold(dst0, dst0, " + thresh + ", 255, cv.ThresholdTypes.Binary)" + vbCrLf + vbCrLf
        strOut += "For the 4 images at the left:" + vbCrLf + vbCrLf
        strOut += "0:cv.Cv2.Threshold(dst0, dst0, " + thresh + ", 255, cv.ThresholdTypes.BinaryInv)" + vbCrLf
        strOut += "1:cv.Cv2.Threshold(dst0, dst0, " + thresh + ", 255, cv.ThresholdTypes.Trunc)" + vbCrLf
        strOut += "2:cv.Cv2.Threshold(dst0, dst0, " + thresh + ", 255, cv.ThresholdTypes.Tozero)" + vbCrLf
        strOut += "3:cv.Cv2.Threshold(dst0, dst0, " + thresh + ", 255, cv.ThresholdTypes.TozeroInv)" + vbCrLf

        SetTrueText(strOut, 3)

        labels(3) = "Current threshold is " + CStr(options.threshold)
    End Sub
End Class






Public Class Threshold_ByChannels : Inherits TaskParent
    Dim optionsColor As New Options_Colors
    Dim options As New Options_Threshold
    Public Sub New()
        labels(3) = "Threshold Inverse"
        OptionParent.findRadio("Trunc").Checked = True
        desc = "Threshold by channel - use red threshold slider to impact grayscale sharedResults.images.."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        optionsColor.Run()

        If src.Channels = 1 Then
            cv.Cv2.Threshold(src, dst2, optionsColor.redS, 255, options.thresholdMethod)
        ElseIf options.inputGray Then
            src = task.gray
            cv.Cv2.Threshold(src, dst2, optionsColor.redS, 255, options.thresholdMethod)
        Else
            Dim split = cv.Cv2.Split(src)
            cv.Cv2.Threshold(split(0), split(0), optionsColor.blueS, 255, options.thresholdMethod)
            cv.Cv2.Threshold(split(1), split(1), optionsColor.greenS, 255, options.thresholdMethod)
            cv.Cv2.Threshold(split(2), split(2), optionsColor.redS, 255, options.thresholdMethod)
            cv.Cv2.Merge(split, dst2)
        End If
        dst3 = Not dst2
        labels(2) = "Threshold method: " + options.thresholdName
    End Sub
End Class










Public Class XR_Threshold_ColorSource : Inherits TaskParent
    Dim color8U As New Color8U_Basics
    Dim byChan As New Threshold_ByChannels
    Public Sub New()
        desc = "Use all the alternative color sources as input to Threshold_ByChannels."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        byChan.Run(color8U.dst3)
        dst2 = byChan.dst2
        dst3 = byChan.dst3
        labels = byChan.labels
    End Sub
End Class
