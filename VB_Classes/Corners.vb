Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://docs.opencv.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_Harris : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Corner block size", 1, 21, 3)
            sliders.setupTrackBar("Corner aperture size", 1, 21, 3)
            sliders.setupTrackBar("Corner quality level", 1, 100, 50)
        End If
        desc = "Find corners using Eigen values and vectors"
        labels(3) = "Corner Eigen values"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static blockSlider = findSlider("Corner block size")
        Static apertureSlider = findSlider("Corner aperture size")
        Static qualitySlider = findSlider("Corner quality level")
        Dim quality = qualitySlider.Value

        Static color As New cv.Mat
        Static gray As New cv.Mat
        Static mc As New cv.Mat

        gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        mc = New cv.Mat(gray.Size(), cv.MatType.CV_32FC1, 0)
        dst2 = New cv.Mat(gray.Size(), cv.MatType.CV_8U, 0)
        cv.Cv2.CornerEigenValsAndVecs(gray, dst2, blockSlider.Value Or 1, apertureSlider.Value Or 1, cv.BorderTypes.Default)

        For j = 0 To gray.Rows - 1
            For i = 0 To gray.Cols - 1
                Dim lambda_1 = dst2.Get(Of cv.Vec6f)(j, i)(0)
                Dim lambda_2 = dst2.Get(Of cv.Vec6f)(j, i)(1)
                mc.Set(Of Single)(j, i, lambda_1 * lambda_2 - 0.04 * Math.Pow(lambda_1 + lambda_2, 2))
            Next
        Next

        Static mm As mmData
        mm = vbMinMax(mc)

        src.CopyTo(dst2)
        For j = 0 To gray.Rows - 1
            For i = 0 To gray.Cols - 1
                If mc.Get(Of Single)(j, i) > mm.minVal + (mm.maxVal - mm.minVal) * quality / qualitySlider.Maximum Then
                    dst2.Circle(New cv.Point(i, j), task.dotSize, task.highlightColor, -1, task.lineType)
                End If
            Next
        Next

        Dim McNormal As New cv.Mat
        cv.Cv2.Normalize(mc, McNormal, 127, 255, cv.NormTypes.MinMax)
        McNormal.ConvertTo(dst3, cv.MatType.CV_8U)
    End Sub
End Class




Public Class Corners_SubPix : Inherits VB_Algorithm
    Public feat As New Feature_KNNBasics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("SubPix kernel Size", 1, 20, 3)
        labels(2) = "Output of GoodFeatures"
        desc = "Use PreCornerDetect to find features in the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static kernelSlider = findSlider("SubPix kernel Size")
        feat.Run(src)
        If feat.featurePoints.Count = 0 Then Exit Sub ' no good features right now...
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim winSize = New cv.Size(CInt(kernelSlider.Value), CInt(kernelSlider.Value))
        cv.Cv2.CornerSubPix(gray, feat.featurePoints, winSize, New cv.Size(-1, -1), term)

        src.CopyTo(dst2)
        Dim p As New cv.Point
        For i = 0 To feat.featurePoints.Count - 1
            p.X = CInt(feat.featurePoints(i).X)
            p.Y = CInt(feat.featurePoints(i).Y)
            dst2.Circle(p, 3, New cv.Scalar(0, 0, 255), -1, task.lineType)
        Next
    End Sub
End Class




Public Class Corners_PreCornerDetect : Inherits VB_Algorithm
    Dim median As New Math_Median_CDF
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("kernel Size", 1, 20, 19)
        desc = "Use PreCornerDetect to find features in the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static kernelSlider = findSlider("kernel Size")
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim prob As New cv.Mat
        cv.Cv2.PreCornerDetect(gray, prob, kernelSlider.Value Or 1)

        cv.Cv2.Normalize(prob, prob, 0, 255, cv.NormTypes.MinMax)
        prob.ConvertTo(gray, cv.MatType.CV_8U)
        median.Run(gray.Clone())
        dst2 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = gray.Threshold(160, 255, cv.ThresholdTypes.BinaryInv).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        labels(3) = "median = " + CStr(median.medianVal)
    End Sub
End Class




' https://docs.opencv.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_ShiTomasi_CPP : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Corner block size", 1, 21, 3)
            sliders.setupTrackBar("Corner aperture size", 1, 21, 3)
            sliders.setupTrackBar("Corner normalize threshold", 0, 32, 0)
        End If
        desc = "Find corners using Eigen values and vectors"
        labels(3) = "Corner Eigen values"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static blockSlider = findSlider("Corner block size")
        Static apertureSlider = findSlider("Corner aperture size")
        Static thresholdSlider = findSlider("Corner normalize threshold")
        Dim threshold = thresholdSlider.Value

        If src.Channels = 1 Then dst2 = src Else dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim data(dst2.Total - 1) As Byte
        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        Marshal.Copy(dst2.Data, data, 0, data.Length)
        Dim imagePtr = Corners_ShiTomasi(handle.AddrOfPinnedObject, src.Rows, src.Cols, blockSlider.Value Or 1, apertureSlider.Value Or 1)
        handle.Free()

        Dim output As New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_32F, imagePtr)

        dst3 = vbNormalize32f(output)
        dst3 = dst3.Threshold(threshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/FASTSample.vb
Public Class Corners_FAST : Inherits VB_Algorithm
    Public features As New List(Of cv.Point2f)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("FAST Threshold", 0, 200, task.FASTthreshold)
        If check.Setup(traceName) Then
            check.addCheckBox("Use Non-Max = True")
            check.Box(0).Checked = True
        End If
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        desc = "Find interesting points with the FAST (Features from Accelerated Segment Test) algorithm"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("FAST Threshold")
        Static nonMaxCheck = findCheckBox("Use Non-Max = True")

        dst2 = src
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim kpoints() As cv.KeyPoint = cv.Cv2.FAST(src, thresholdSlider.Value, nonMaxCheck.checked)

        dst3.SetTo(0)
        features.Clear()
        For Each kp As cv.KeyPoint In kpoints
            dst2.Circle(kp.Pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType, 0)
            dst3.Set(Of Byte)(kp.Pt.Y, kp.Pt.X, 255)
            features.Add(kp.Pt)
        Next
        labels(2) = "There were " + CStr(features.Count) + " key points detected"
    End Sub
End Class





Public Class Corners_FASTCentroid : Inherits VB_Algorithm
    Dim fast As New Corners_FAST
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(1) ' 2 elements - cv.point
        desc = "Find interesting points with the FAST and smooth the centroid with kalman"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fast.Run(src)
        dst2 = fast.dst2
        dst3.SetTo(0)
        For Each pt In fast.features
            dst3.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType, 0)
        Next
        Dim gray = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim m = cv.Cv2.Moments(gray, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run(src)
            dst3.Circle(New cv.Point(kalman.kOutput(0), kalman.kOutput(1)), 10, cv.Scalar.Red, -1, task.lineType)
        End If
    End Sub
End Class






Public Class Corners_FASTStablePoints : Inherits VB_Algorithm
    Public features As New List(Of cv.Point)
    Dim fast As New Corners_FAST
    Public Sub New()
        labels = {"", "", "", "FAST stable points without context"}
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find and save only the stable points in the FAST output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fast.Run(src)

        If task.motionFlag Or task.optionsChanged Then
            For Each pt In fast.features
                features.Add(pt)
            Next
        End If
        Dim newPts As New List(Of cv.Point)
        dst2 = src
        dst3.SetTo(0)
        For Each pt In fast.features
            Dim test = New cv.Point(pt.X, pt.Y)
            If features.Contains(test) Then
                dst2.Circle(test, task.dotSize, cv.Scalar.Yellow, -1, task.lineType, 0)
                newPts.Add(test)
                dst3.Set(Of Byte)(test.Y, test.X, 255)
            End If
        Next

        features = New List(Of cv.Point)(newPts)
        labels(2) = Format(features.Count, "000") + " identified FAST stable points - slider adjusts threshold"
    End Sub
End Class







Public Class Corners_FASTCentroids : Inherits VB_Algorithm
    Dim fast As New Corners_FAST
    Dim fastCenters() As cv.Point2f
    Public Sub New()
        If standaloneTest() Then gOptions.GridSize.Value = 16
        desc = "Use a thread grid to find the centroids in each grid element"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.Clone

        fast.Run(src)
        ReDim fastCenters(task.gridList.Count - 1)
        For i = 0 To task.gridList.Count - 1
            Dim roi = task.gridList(i)
            Dim tmp = fast.dst3(roi).FindNonZero()
            If tmp.Rows > 0 Then
                Dim mean = tmp.Mean()
                fastCenters(i) = New cv.Point2f(roi.X + mean(0), roi.Y + mean(1))
            End If
        Next

        For i = 0 To fastCenters.Count - 1
            dst2.Circle(fastCenters(i), task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next
        dst2.SetTo(cv.Scalar.White, task.gridMask)
    End Sub
End Class









' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class Corners_Harris_CPP : Inherits VB_Algorithm
    Dim dataSrc() As Byte
    Dim addw As New AddWeighted_Basics
    Dim options As New Options_Harris
    Public Sub New()
        ReDim dataSrc(dst2.Total - 1)
        cPtr = Harris_Features_Open()
        desc = "Use Harris feature detectors to identify interesting points."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Harris_Features_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.threshold,
                                           options.neighborhood, options.aperture, options.harrisParm)
        handleSrc.Free()

        Dim gray32f = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_32F, imagePtr)
        gray32f.ConvertTo(dst2, cv.MatType.CV_8U)
        addw.src2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        addw.Run(task.color)
        dst3 = addw.dst2
        labels(3) = "RGB overlaid with Harris result. "
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Harris_Features_Close(cPtr)
    End Sub
End Class






' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class Corners_HarrisDetector : Inherits VB_Algorithm
    Dim dataSrc() As Byte
    Dim ptCount(1) As Integer
    Public FeaturePoints As New List(Of cv.Point2f)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Harris quality level", 1, 100, 2)
        desc = "Use Harris detector to identify interesting points."
        ReDim dataSrc(dst2.Total - 1)
        cPtr = Harris_Detector_Open()
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static qualitySlider = findSlider("Harris quality level")
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)

        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim handleCount = GCHandle.Alloc(ptCount, GCHandleType.Pinned)
        Dim imagePtr = Harris_Detector_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, qualitySlider.Value / 100, handleCount.AddrOfPinnedObject())
        handleSrc.Free()
        handleCount.Free()
        If ptCount(0) > 1 Then
            Dim ptMat = New cv.Mat(ptCount(0), 2, cv.MatType.CV_32S, imagePtr).Clone
            src.CvtColor(cv.ColorConversionCodes.GRAY2BGR).CopyTo(dst2)
            FeaturePoints.Clear()
            For i = 0 To ptMat.Rows - 1
                FeaturePoints.Add(New cv.Point2f(ptMat.Get(Of Integer)(i, 0), ptMat.Get(Of Integer)(i, 1)))
                dst2.Circle(FeaturePoints(i), task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)
            Next
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Harris_Detector_Close(cPtr)
    End Sub
End Class








Public Class Corners_RedCloud : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim corners As New Neighbors_Intersects
    Public Sub New()
        labels = {"", "", "Grayscale", "Highlighted points show where more than 2 cells intersect."}
        desc = "Find the corners for each RedCloud cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        corners.Run(task.cellMap)

        dst3 = task.color.Clone
        For Each pt In corners.nPoints
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next
    End Sub
End Class