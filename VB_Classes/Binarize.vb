Imports cv = OpenCvSharp
Imports OpenCvSharp.XImgProc

' https://docs.opencv.org/3.4/d7/d4d/tutorial_py_thresholding.html
' https://www.learnopencv.com/otsu-thresholding-with-opencv/?ck_subscriber_id=785741175
' https://github.com/spmallick/learnopencv/tree/master/otsu-method?ck_subscriber_id=785741175
Public Class Binarize_Basics : Inherits VBparent
    Public thresholdType = cv.ThresholdTypes.Otsu
    Dim minRange = 0
    Dim maxRange = 255
    Public histogram As New cv.Mat
    Public meanScalar As cv.Scalar
    Public mask As New cv.Mat
    Dim blur As New Blur_Basics
    Public useBlur As Boolean
    Public Sub New()
        mask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 255)
        task.desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        meanScalar = cv.Cv2.Mean(src, mask)

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If useBlur Then
            blur.RunClass(input)
            dst2 = blur.dst2.Threshold(meanScalar(0), 255, thresholdType)
        Else
            dst2 = input.Threshold(meanScalar(0), 255, thresholdType)
        End If
    End Sub
End Class





'https://docs.opencv.org/3.4/d7/d4d/tutorial_py_thresholding.html
Public Class Binarize_OTSU : Inherits VBparent
    Dim binarize As Binarize_Basics
    Public Sub New()
        binarize = New Binarize_Basics()

        If radio.Setup(caller, 4) Then
            radio.check(0).Text = "Binary"
            radio.check(1).Text = "Binary + OTSU"
            radio.check(2).Text = "OTSU"
            radio.check(3).Text = "OTSU + Blur"
            radio.check(0).Checked = True
        End If

        labels(2) = "Threshold 1) binary 2) Binary+OTSU 3) OTSU 4) OTSU+Blur"
        labels(3) = "Histograms correspond to images on the left"
        task.desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        binarize.meanScalar = cv.Cv2.Mean(input)
        Static frm = findfrm(caller + " Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then labels(2) = radio.check(i).Text
        Next

        binarize.useBlur = False
        Select Case labels(2)
            Case radio.check(0).Text
                binarize.thresholdType = cv.ThresholdTypes.Binary
            Case radio.check(1).Text
                binarize.thresholdType = cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu
            Case radio.check(2).Text
                binarize.thresholdType = cv.ThresholdTypes.Otsu
            Case radio.check(3).Text
                binarize.useBlur = True
                binarize.thresholdType = cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu
        End Select
        binarize.RunClass(input)
        dst2 = binarize.dst2
    End Sub
End Class





Public Class Binarize_Niblack_Sauvola : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Kernel Size", 3, 500, 51)
            sliders.setupTrackBar(1, "Niblack k", -1000, 1000, -200)
            sliders.setupTrackBar(2, "Sauvola k", -1000, 1000, 100)
            sliders.setupTrackBar(3, "Sauvola r", 1, 100, 64)
        End If
        task.desc = "Binarize an image using Niblack and Sauvola"
        labels(2) = "Binarize Niblack"
        labels(3) = "Binarize Sauvola"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim kernelSize = sliders.trackbar(0).Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim grayBin As New cv.Mat
        cv.Extensions.Binarizer.Niblack(input, grayBin, kernelSize, sliders.trackbar(1).Value / 1000)
        dst2 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Extensions.Binarizer.Sauvola(input, grayBin, kernelSize, sliders.trackbar(2).Value / 1000, sliders.trackbar(3).Value)
        dst3 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Niblack_Nick : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Kernel Size", 3, 500, 51)
            sliders.setupTrackBar(1, "Niblack k", -1000, 1000, -200)
            sliders.setupTrackBar(2, "Nick k", -1000, 1000, 100)
        End If
        task.desc = "Binarize an image using Niblack and Nick"
        labels(2) = "Binarize Niblack"
        labels(3) = "Binarize Nick"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim kernelSize = sliders.trackbar(0).Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim grayBin As New cv.Mat
        cv.Extensions.Binarizer.Niblack(input, grayBin, kernelSize, sliders.trackbar(1).Value / 1000)
        dst2 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Extensions.Binarizer.Nick(input, grayBin, kernelSize, sliders.trackbar(2).Value / 1000)
        dst3 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Bernson : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Kernel Size", 3, 500, 51)
            sliders.setupTrackBar(1, "Contrast min", 0, 255, 50)
            sliders.setupTrackBar(2, "bg Threshold", 0, 255, 100)
        End If
        labels(2) = "Binarize Bernson (Draw Enabled)"

        task.drawRect = New cv.Rect(100, 100, 100, 100)
        task.desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim kernelSize = sliders.trackbar(0).Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayBin = gray.Clone()
        If task.drawRect = New cv.Rect() Then
            cv.Extensions.Binarizer.Bernsen(gray, grayBin, kernelSize, sliders.trackbar(1).Value, sliders.trackbar(2).Value)
        Else
            cv.Extensions.Binarizer.Bernsen(gray(task.drawRect), grayBin(task.drawRect), kernelSize, sliders.trackbar(1).Value, sliders.trackbar(2).Value)
        End If
        dst2 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Bernson_MT : Inherits VBparent
    Dim grid As New Thread_Grid
    Public Sub New()
        findSlider("ThreadGrid Width").Value = 32
        findSlider("ThreadGrid Height").Value = 32

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Kernel Size", 3, 500, 51)
            sliders.setupTrackBar(1, "Contrast min", 0, 255, 50)
            sliders.setupTrackBar(2, "bg Threshold", 0, 255, 100)
        End If
        task.desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
        labels(2) = "Binarize Bernson"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static kernelSlider = findSlider("Kernel Size")
        Static contrastSlider = findSlider("Contrast min")
        Static bgSlider = findSlider("bg Threshold")
        Dim kernelSize = kernelSlider.Value
        Dim contrastMin = contrastSlider.Value
        Dim bgThreshold = bgSlider.Value

        If kernelSize Mod 2 = 0 Then kernelSize += 1
        grid.RunClass(Nothing)

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Parallel.ForEach(grid.roiList,
            Sub(roi)
                Dim grayBin = input(roi).Clone()
                cv.Extensions.Binarizer.Bernsen(input(roi), grayBin, kernelSize, contrastMin, bgThreshold)
                dst2(roi) = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            End Sub)
    End Sub
End Class








Public Class Binarize_Reduction : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Dim basics As Binarize_Basics
    Public Sub New()
        basics = New Binarize_Basics
        findRadio("Use bitwise reduction").Checked = True
        findSlider("Reduction factor").Value = 256
        labels(2) = "Binarize output from reduction"
        labels(3) = "Binarize Basics Output"
        task.desc = "Binarize an image using reduction"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim tmp = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        reduction.RunClass(tmp)
        dst2 = reduction.dst2.Threshold(reduction.maskVal / 2, 255, cv.ThresholdTypes.Binary)

        basics.RunClass(tmp)
        dst3 = basics.dst2
    End Sub
End Class






Public Class Binarize_Simple : Inherits VBparent
    Public meanScalar As cv.Scalar
    Public mask As New cv.Mat
    Public Sub New()
        mask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 255)

        task.desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        Dim input = src.Clone
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If mask.Width Then
            Dim tmp As New cv.Mat(input.Size, cv.MatType.CV_8U, 255)
            If mask.Type <> cv.MatType.CV_8U Then mask.ConvertTo(mask, cv.MatType.CV_8U)
            input.CopyTo(tmp, mask)
            meanScalar = cv.Cv2.Mean(tmp, mask)
        Else
            meanScalar = cv.Cv2.Mean(input)
        End If

        dst2 = input.Threshold(meanScalar(0), 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Binarize_Recurse : Inherits VBparent
    Dim binarize As Binarize_Simple
    Public mats As New Mat_4Click
    Public Sub New()
        binarize = New Binarize_Simple
        labels(2) = "Lighter half, lightest, darker half, darkest"
        task.desc = "Binarize an image twice using masks"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        Dim gray = If(src.Channels = 1, src.Clone, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        binarize.mask = New cv.Mat
        binarize.RunClass(gray)
        mats.mat(0) = binarize.dst2.Clone

        binarize.mask = mats.mat(0)
        binarize.RunClass(gray)
        mats.mat(1) = binarize.dst2.Clone

        cv.Cv2.BitwiseNot(mats.mat(0), mats.mat(2))
        binarize.mask = mats.mat(0).Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        binarize.RunClass(gray)
        mats.mat(3) = binarize.dst2.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)

        If standalone Or task.intermediateActive Then mats.RunClass(src)
        dst2 = mats.dst2
        dst3 = mats.dst3
        labels(3) = mats.labels(3)
    End Sub
End Class

