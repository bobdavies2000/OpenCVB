Imports System.Windows.Automation
Imports cv = OpenCvSharp

' https://docs.opencv.org/3.4/d7/d4d/tutorial_py_thresholding.html
' https://www.learnopencv.com/otsu-thresholding-with-opencv/?ck_subscriber_id=785741175
' https://github.com/spmallick/learnopencv/tree/master/otsu-method?ck_subscriber_id=785741175
Public Class Binarize_Basics : Inherits VB_Algorithm
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
        vbAddAdvice(traceName + ": use local options to control the kernel size and sigma.")
        desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        meanScalar = cv.Cv2.Mean(src, mask)

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If useBlur Then
            blur.Run(input)
            dst2 = blur.dst2.Threshold(meanScalar(0), 255, thresholdType)
        Else
            dst2 = input.Threshold(meanScalar(0), 255, thresholdType)
        End If
    End Sub
End Class





'https://docs.opencv.org/3.4/d7/d4d/tutorial_py_thresholding.html
Public Class Binarize_OTSU : Inherits VB_Algorithm
    Dim binarize As Binarize_Basics
    Public Sub New()
        binarize = New Binarize_Basics()

        If radio.Setup(traceName) Then
            radio.addRadio("Binary")
            radio.addRadio("Binary + OTSU")
            radio.addRadio("OTSU")
            radio.addRadio("OTSU + Blur")
            radio.check(0).Checked = True
        End If

        labels(2) = "Threshold 1) binary 2) Binary+OTSU 3) OTSU 4) OTSU+Blur"
        labels(3) = "Histograms correspond to images on the left"
        desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        binarize.meanScalar = cv.Cv2.Mean(input)
        Static frm = findfrm(traceName + " Radio Buttons")
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then labels(2) = radio.check(i).Text
        Next

        binarize.useBlur = False
        Select Case labels(2)
            Case "Binary"
                binarize.thresholdType = cv.ThresholdTypes.Binary
            Case "Binary + OTSU"
                binarize.thresholdType = cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu
            Case "OTSU"
                binarize.thresholdType = cv.ThresholdTypes.Otsu
            Case "OTSU + Blur"
                binarize.useBlur = True
                binarize.thresholdType = cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu
        End Select
        binarize.Run(input)
        dst2 = binarize.dst2
    End Sub
End Class





#Disable Warning BC40000
Public Class Binarize_Niblack_Sauvola : Inherits VB_Algorithm
    Dim options As New Options_BinarizeNiBlack
    Public Sub New()
        desc = "Binarize an image using Niblack and Sauvola"
        labels(2) = "Binarize Niblack"
        labels(3) = "Binarize Sauvola"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Extensions.Binarizer.Niblack(src, dst0, options.kernelSize, options.niBlackK)
        dst2 = dst0.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Extensions.Binarizer.Sauvola(src, dst0, options.kernelSize, options.sauvolaK, options.sauvolaR)
        dst3 = dst0.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class






Public Class Binarize_Niblack_Nick : Inherits VB_Algorithm
    Dim options As New Options_BinarizeNiBlack
    Public Sub New()
        desc = "Binarize an image using Niblack and Nick"
        labels(2) = "Binarize Niblack"
        labels(3) = "Binarize Nick"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        cv.Extensions.Binarizer.Niblack(src, dst2, options.kernelSize, options.niBlackK)
        cv.Extensions.Binarizer.Nick(src, dst3, options.kernelSize, options.nickK)
    End Sub
End Class






Public Class Binarize_Bernson : Inherits VB_Algorithm
    Dim options As New Options_Bernson
    Public Sub New()
        labels(2) = "Binarize Bernson (Draw Enabled)"

        Dim w = 40, h = 40
        task.drawRect = New cv.Rect(dst2.Width / 2 - w, dst2.Height / 2 - h, w, h)
        desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst0 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Extensions.Binarizer.Bernsen(dst0(task.drawRect), dst0(task.drawRect), options.kernelSize, options.contrastMin, options.bgThreshold)
        dst2 = dst0.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Bernson_MT : Inherits VB_Algorithm
    Dim options As New Options_Bernson
    Public Sub New()
        gOptions.GridSize.Value = 32
        desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
        labels(2) = "Binarize Bernson"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Parallel.ForEach(task.gridList,
            Sub(roi)
                Dim grayBin = src(roi).Clone()
                cv.Extensions.Binarizer.Bernsen(src(roi), grayBin, options.kernelSize, options.contrastMin, options.bgThreshold)
                dst2(roi) = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            End Sub)
    End Sub
End Class








Public Class Binarize_Simple : Inherits VB_Algorithm
    Public meanScalar As cv.Scalar
    Public injectVal As Integer = 255
    Public Sub New()
        desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        meanScalar = cv.Cv2.Mean(src)
        dst2 = src.Threshold(meanScalar(0), injectVal, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Binarize_KMeansMasks : Inherits VB_Algorithm
    Dim km As New KMeans_Image
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Ordered from dark to light, top left darkest, bottom right lightest "
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Display the top 4 masks from the BGR kmeans output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(src)
        For i = 0 To km.masks.Count - 1
            mats.mat(i) = km.masks(i)
            dst1.SetTo(i + 1, km.masks(i))
            If i >= 3 Then Exit For
        Next

        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class








Public Class Binarize_KMeansRGB : Inherits VB_Algorithm
    Dim km As New KMeans_Image
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Ordered from dark to light, top left darkest, bottom right lightest "
        desc = "Display the top 4 masks from the BGR kmeans output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(src)
        dst1.SetTo(0)
        For i = 0 To km.masks.Count - 1
            mats.mat(i) = New cv.Mat(dst2.Size, cv.MatType.CV_8UC3, 0)
            src.CopyTo(mats.mat(i), km.masks(i))
            If i >= 3 Then Exit For
        Next
        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class








Public Class Binarize_FourPixelFlips : Inherits VB_Algorithm
    Dim binar4 As New Bin4Way_Regions
    Public Sub New()
        desc = "Identify the marginal regions that flip between subdivisions based on brightness."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binar4.Run(src)
        dst2 = vbPalette(binar4.dst2 * 255 / 5)

        Static lastSubD As cv.Mat = binar4.dst2.Clone
        dst3 = lastSubD - binar4.dst2
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary)
        lastSubD = binar4.dst2.Clone
    End Sub
End Class






Public Class Binarize_DepthTiers : Inherits VB_Algorithm
    Dim tiers As New Depth_TiersZ
    Dim binar4 As New Bin4Way_Regions
    Public classCount = 200 ' 4-way split with 50 depth levels at 10 cm's each.
    Public Sub New()
        redOptions.UseColorOnly.Checked = True
        desc = "Add the Depth_TiersZ and Bin4Way_Regions output in preparation for RedCloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binar4.Run(src)
        tiers.Run(src)
        dst3 = tiers.dst3

        dst0 = tiers.dst2 + binar4.dst2

        If task.heartBeat Then
            dst2 = dst0.Clone
        ElseIf task.motionDetected Then
            dst0(task.motionRect).CopyTo(dst2(task.motionRect))
        End If
        classCount = binar4.classCount + tiers.classCount
    End Sub
End Class
