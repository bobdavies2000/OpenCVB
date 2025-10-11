Imports OpenCvSharp.XImgProc
Imports cv = OpenCvSharp

' https://docs.opencvb.org/3.4/d7/d4d/tutorial_py_thresholding.html
' https://www.learnopencvb.com/otsu-thresholding-with-opencv/?ck_subscriber_id=785741175
' https://github.com/spmallick/learnopencv/tree/master/otsu-method?ck_subscriber_id=785741175
Public Class Binarize_Basics : Inherits TaskParent
    Public thresholdType = cv.ThresholdTypes.Otsu
    Public histogram As New cv.Mat
    Public meanScalar As cv.Scalar
    Public mask As New cv.Mat
    Dim blur As New Blur_Basics
    Public useBlur As Boolean
    Public Sub New()
        mask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 255)
        desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        meanScalar = cv.Cv2.Mean(src, mask)

        If useBlur Then
            blur.Run(task.gray)
            dst2 = blur.dst2.Threshold(meanScalar(0), 255, thresholdType)
        Else
            dst2 = task.gray.Threshold(meanScalar(0), 255, thresholdType)
        End If
    End Sub
End Class





'https://docs.opencvb.org/3.4/d7/d4d/tutorial_py_thresholding.html
Public Class Binarize_OTSU : Inherits TaskParent
    Dim binarize As Binarize_Basics
    Dim options As New Options_Binarize
    Public Sub New()
        binarize = New Binarize_Basics()

        labels(2) = "Threshold 1) binary 2) Binary+OTSU 3) OTSU 4) OTSU+Blur"
        labels(3) = "Histograms correspond to images on the left"
        desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        binarize.meanScalar = cv.Cv2.Mean(task.gray)

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
        binarize.Run(task.gray)
        dst2 = binarize.dst2
    End Sub
End Class





Public Class Binarize_Niblack_Sauvola : Inherits TaskParent
    Dim options As New Options_BinarizeNiBlack
    Public Sub New()
        desc = "Binarize an image using Niblack and Sauvola"
        labels(2) = "Binarize Niblack"
        labels(3) = "Binarize Sauvola"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        CvXImgProc.NiblackThreshold(task.gray, dst0, 255, cv.ThresholdTypes.Binary, 5, 0.5, LocalBinarizationMethods.Niblack)
        dst2 = dst0.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        CvXImgProc.NiblackThreshold(task.gray, dst0, 255, cv.ThresholdTypes.Binary, 5, 0.5, LocalBinarizationMethods.Sauvola)
        dst3 = dst0.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class






Public Class Binarize_Wolf_Nick : Inherits TaskParent
    Dim options As New Options_BinarizeNiBlack
    Public Sub New()
        desc = "Binarize an image using Niblack and Nick"
        labels(2) = "Binarize Niblack"
        labels(3) = "Binarize Nick"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        CvXImgProc.NiblackThreshold(task.gray, dst2, 255, cv.ThresholdTypes.Binary, 5, 0.5, LocalBinarizationMethods.Wolf)
        CvXImgProc.NiblackThreshold(task.gray, dst3, 255, cv.ThresholdTypes.Binary, 5, 0.5, LocalBinarizationMethods.Nick)
    End Sub
End Class






Public Class Binarize_KMeansMasks : Inherits TaskParent
    Dim km As New KMeans_Image
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Ordered from dark to light, top left darkest, bottom right lightest "
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Display the top 4 masks from the BGR kmeans output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        km.Run(src)
        For i = 0 To km.masks.Count - 1
            mats.mat(i) = km.masks(i)
            dst1.SetTo(i + 1, km.masks(i))
            If i >= 3 Then Exit For
        Next

        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class








Public Class Binarize_KMeansRGB : Inherits TaskParent
    Dim km As New KMeans_Image
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Ordered from dark to light, top left darkest, bottom right lightest "
        desc = "Display the top 4 masks from the BGR kmeans output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        km.Run(src)
        dst1.SetTo(0)
        For i = 0 To km.masks.Count - 1
            mats.mat(i) = New cv.Mat(dst2.Size(), cv.MatType.CV_8UC3, cv.Scalar.All(0))
            src.CopyTo(mats.mat(i), km.masks(i))
            If i >= 3 Then Exit For
        Next
        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class






Public Class Binarize_DepthTiers : Inherits TaskParent
    Dim tiers As New Depth_Tiers
    Dim binar4 As New Bin4Way_Regions
    Public classCount = 200 ' 4-way split with 50 depth levels at 10 cm's each.
    Public Sub New()
        desc = "Add the Depth_TierZ and Bin4Way_Regions output in preparation for RedCloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        binar4.Run(src)
        tiers.Run(src)

        dst2 = tiers.dst2 + binar4.dst2

        If standaloneTest() Then dst3 = tiers.dst3

        classCount = binar4.classCount + tiers.classCount
    End Sub
End Class








Public Class Binarize_Simple : Inherits TaskParent
    Public meanScalar As cv.Scalar
    Public injectVal As Integer = 255
    Public Sub New()
        desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        meanScalar = cv.Cv2.Mean(task.gray)
        dst2 = src.Threshold(meanScalar(0), injectVal, cv.ThresholdTypes.Binary)
    End Sub
End Class









Public Class Binarize_FourPixelFlips : Inherits TaskParent
    Dim binar4 As New Bin4Way_Regions
    Public Sub New()
        desc = "Identify the marginal regions that flip between subdivisions based on brightness."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        binar4.Run(src)
        dst2 = PaletteFull(binar4.dst2)

        Static lastSubD As cv.Mat = binar4.dst2.Clone
        dst3 = lastSubD - binar4.dst2
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary)
        lastSubD = binar4.dst2.Clone
    End Sub
End Class
