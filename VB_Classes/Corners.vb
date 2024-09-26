Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Imports OpenCvSharp
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/FASTSample.vb
Public Class Corners_Basics : Inherits VB_Parent
    Public features As New List(Of cvb.Point2f)
    Dim options As New Options_Features
    Dim optionCorner As New Options_Corners
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U)
        desc = "Find interesting points with the FAST (Features from Accelerated Segment Test) algorithm"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        optionCorner.RunOpt()
        options.RunOpt()

        dst2 = src.Clone
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim kpoints() As cvb.KeyPoint = cvb.Cv2.FAST(src, task.FASTthreshold, optionCorner.useNonMax)

        features.Clear()
        For Each kp As cvb.KeyPoint In kpoints
            features.Add(kp.Pt)
        Next

        If standaloneTest() Then
            dst3.SetTo(0)
            For Each kp As cvb.KeyPoint In kpoints
                DrawCircle(dst2, kp.Pt, task.DotSize, cvb.Scalar.Yellow)
                dst3.Set(Of Byte)(kp.Pt.Y, kp.Pt.X, 255)
            Next
        End If
        labels(2) = "There were " + CStr(features.Count) + " key points detected using FAST"
    End Sub
End Class






' https://docs.opencvb.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_Harris : Inherits VB_Parent
    Dim options As New Options_HarrisCorners
    Dim gray As New cvb.Mat
    Dim mc As New cvb.Mat, mm As mmData

    Public Sub New()
        desc = "Find corners using Eigen values and vectors"
        labels(3) = "Corner Eigen values"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        gray = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        mc = New cvb.Mat(gray.Size(), cvb.MatType.CV_32FC1, 0)
        dst2 = New cvb.Mat(gray.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        cvb.Cv2.CornerEigenValsAndVecs(gray, dst2, options.blockSize, options.aperture, cvb.BorderTypes.Default)

        For y = 0 To gray.Rows - 1
            For x = 0 To gray.Cols - 1
                Dim lambda_1 = dst2.Get(Of cvb.Vec6f)(y, x)(0)
                Dim lambda_2 = dst2.Get(Of cvb.Vec6f)(y, x)(1)
                mc.Set(Of Single)(y, x, lambda_1 * lambda_2 - 0.04 * Math.Pow(lambda_1 + lambda_2, 2))
            Next
        Next

        mm = GetMinMax(mc)

        src.CopyTo(dst2)
        Dim count As Integer
        For y = 0 To gray.Rows - 1
            For x = 0 To gray.Cols - 1
                If mc.Get(Of Single)(y, x) > mm.minVal + (mm.maxVal - mm.minVal) * options.quality / options.qualityMax Then
                    DrawCircle(dst2, New cvb.Point(x, y), task.DotSize, task.HighlightColor)
                    count += 1
                End If
            Next
        Next

        labels(2) = "Corners_Harris found " + CStr(count) + " corners in the image."

        Dim McNormal As New cvb.Mat
        cvb.Cv2.Normalize(mc, McNormal, 127, 255, cvb.NormTypes.MinMax)
        McNormal.ConvertTo(dst3, cvb.MatType.CV_8U)
    End Sub
End Class






Public Class Corners_PreCornerDetect : Inherits VB_Parent
    Dim median As New Math_Median_CDF
    Dim options As New Options_PreCorners
    Public Sub New()
        desc = "Use PreCornerDetect to find features in the image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim gray = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim prob As New cvb.Mat
        cvb.Cv2.PreCornerDetect(gray, prob, options.kernelSize)

        cvb.Cv2.Normalize(prob, prob, 0, 255, cvb.NormTypes.MinMax)
        prob.ConvertTo(gray, cvb.MatType.CV_8U)
        median.Run(gray.Clone())
        dst2 = gray.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        dst3 = gray.Threshold(160, 255, cvb.ThresholdTypes.BinaryInv).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        labels(3) = "median = " + CStr(median.medianVal)
    End Sub
End Class




' https://docs.opencvb.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_ShiTomasi_CPP_VB : Inherits VB_Parent
    Dim options As New Options_ShiTomasi
    Public Sub New()
        desc = "Find corners using Eigen values and vectors"
        labels(3) = "Corner Eigen values using ShiTomasi which is also what is used in GoodFeatures."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Dim data(src.Total - 1) As Byte
        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        Marshal.Copy(src.Data, data, 0, data.Length)
        Dim imagePtr = Corners_ShiTomasi(handle.AddrOfPinnedObject, src.Rows, src.Cols, options.blocksize, options.aperture)
        handle.Free()

        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_32F, imagePtr).Clone

        dst3 = Convert32f_To_8UC3(dst2)
        dst3 = dst3.Threshold(options.threshold, 255, cvb.ThresholdTypes.Binary)
    End Sub
End Class





Public Class Corners_BasicsCentroid : Inherits VB_Parent
    Dim fast As New Corners_Basics
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(1) ' 2 elements - cvb.point
        desc = "Find interesting points with the FAST and smooth the centroid with kalman"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        fast.Run(src)
        dst2 = fast.dst2
        dst3.SetTo(0)
        For Each pt In fast.features
            DrawCircle(dst3, pt, task.DotSize + 2, cvb.Scalar.White)
        Next
        Dim gray = dst3.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim m = cvb.Cv2.Moments(gray, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run(src)
            DrawCircle(dst3, New cvb.Point(kalman.kOutput(0), kalman.kOutput(1)), 10, cvb.Scalar.Red)
        End If
    End Sub
End Class






Public Class Corners_BasicsStablePoints : Inherits VB_Parent
    Public features As New List(Of cvb.Point)
    Dim fast As New Corners_Basics
    Public Sub New()
        labels = {"", "", "", "FAST stable points without context"}
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Find and save only the stable points in the FAST output"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        fast.Run(src)

        If task.motionFlag Or task.optionsChanged Then
            For Each pt In fast.features
                features.Add(pt)
            Next
        End If
        Dim newPts As New List(Of cvb.Point)
        dst2 = src
        dst3.SetTo(0)
        For Each pt In fast.features
            Dim test = New cvb.Point(pt.X, pt.Y)
            If features.Contains(test) Then
                DrawCircle(dst2, test, task.DotSize, cvb.Scalar.Yellow)
                newPts.Add(test)
                dst3.Set(Of Byte)(test.Y, test.X, 255)
            End If
        Next

        features = New List(Of cvb.Point)(newPts)
        labels(2) = Format(features.Count, "000") + " identified FAST stable points - slider adjusts threshold"
    End Sub
End Class







Public Class Corners_BasicsCentroids : Inherits VB_Parent
    Dim fast As New Corners_Basics
    Dim fastCenters() As cvb.Point2f
    Public Sub New()
        If standaloneTest() Then task.gOptions.setGridSize(16)
        desc = "Use a thread grid to find the centroids in each grid element"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.Clone

        fast.Run(src)
        ReDim fastCenters(task.gridRects.Count - 1)
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            Dim tmp = fast.dst3(roi).FindNonZero()
            If tmp.Rows > 0 Then
                Dim mean = tmp.Mean()
                fastCenters(i) = New cvb.Point2f(roi.X + mean(0), roi.Y + mean(1))
            End If
        Next

        For i = 0 To fastCenters.Count - 1
            DrawCircle(dst2, fastCenters(i), task.DotSize, cvb.Scalar.Yellow)
        Next
        dst2.SetTo(cvb.Scalar.White, task.gridMask)
    End Sub
End Class









' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class Corners_Harris_CPP_VB : Inherits VB_Parent
    Dim addw As New AddWeighted_Basics
    Dim options As New Options_Harris
    Public Sub New()
        cPtr = Harris_Features_Open()
        desc = "Use Harris feature detectors to identify interesting points."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim dataSrc(src.Total - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Harris_Features_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.threshold,
                                           options.neighborhood, options.aperture, options.harrisParm)
        handleSrc.Free()

        Dim gray32f = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_32F, imagePtr)
        '  gray32f = Convert32f_To_8UC3(gray32f)
        gray32f.ConvertTo(dst2, cvb.MatType.CV_8U)
        addw.src2 = dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        addw.Run(task.color)
        dst3 = addw.dst2
        labels(3) = "RGB overlaid with Harris result. "
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Harris_Features_Close(cPtr)
    End Sub
End Class






' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class Corners_HarrisDetector_CPP_VB : Inherits VB_Parent
    Public features As New List(Of cvb.Point2f)
    Dim options As New Options_Features
    Public Sub New()
        desc = "Use Harris detector to identify interesting points."
        cPtr = Harris_Detector_Open()
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = src.Clone

        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim dataSrc(src.Total) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)

        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Harris_Detector_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.quality)
        handleSrc.Free()
        Dim ptCount = Harris_Detector_Count(cPtr)
        If ptCount > 1 Then
            Dim ptMat = cvb.Mat.FromPixelData(ptCount, 2, cvb.MatType.CV_32S, imagePtr).Clone
            features.Clear()
            For i = 0 To ptCount - 1
                features.Add(New cvb.Point2f(ptMat.Get(Of Integer)(i, 0), ptMat.Get(Of Integer)(i, 1)))
                DrawCircle(dst2, features(i), task.DotSize, cvb.Scalar.Yellow)
            Next
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Harris_Detector_Close(cPtr)
    End Sub
End Class








Public Class Corners_RedCloud : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim corners As New Neighbors_Intersects
    Public Sub New()
        labels = {"", "", "Grayscale", "Highlighted points show where more than 2 cells intersect."}
        desc = "Find the corners for each RedCloud cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        corners.Run(task.cellMap)

        dst3 = task.color.Clone
        For Each pt In corners.nPoints
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
            DrawCircle(dst3, pt, task.DotSize, cvb.Scalar.Yellow)
        Next
    End Sub
End Class




Public Class Corners_SubPix : Inherits VB_Parent
    Public feat As New Feature_Stable
    Dim options As New Options_PreCorners
    Public Sub New()
        labels(2) = "Output of PreCornerDetect"
        desc = "Use PreCornerDetect to refine the feature points to sub-pixel accuracy."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = src.Clone
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        feat.Run(src)
        cvb.Cv2.CornerSubPix(src, task.features, New cvb.Size(options.subpixSize, options.subpixSize), New cvb.Size(-1, -1), term)

        task.featurePoints.Clear()
        For i = 0 To task.features.Count - 1
            Dim pt = task.features(i)
            task.featurePoints.Add(New cvb.Point(pt.X, pt.Y))
            DrawCircle(dst2,pt, task.DotSize, task.HighlightColor)
        Next
    End Sub
End Class