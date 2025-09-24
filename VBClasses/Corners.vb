Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Corners_Basics : Inherits TaskParent
    Public fast As New Corners_Core
    Public features As New List(Of cv.Point2f)
    Public Sub New()
        labels = {"", "", "", "FAST stable points without context"}
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find and save only the stable points in the FAST output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim featurePoints = New List(Of cv.Point)(task.featurePoints)
        fast.Run(src)

        Dim threshold = task.featurePoints.Count / 10

        dst2 = src
        dst3.SetTo(0)
        Dim newPts As New List(Of cv.Point)
        Dim new2f As New List(Of cv.Point2f)
        For i = 0 To task.featurePoints.Count - 1
            Dim pt = task.featurePoints(i)
            If featurePoints.Contains(pt) Then
                DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Yellow)
                newPts.Add(pt)
                new2f.Add(fast.features(i))
                dst3.Set(Of Byte)(pt.Y, pt.X, 255)
            End If
        Next


        task.featurePoints = If(newPts.Count <= threshold, task.featurePoints, New List(Of cv.Point)(newPts))
        features = If(new2f.Count <= threshold, fast.features, New List(Of cv.Point2f)(new2f))
        labels(2) = Format(task.featurePoints.Count, "000") + " identified FAST stable points - slider adjusts threshold"
    End Sub
End Class






' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/FASTSample.vb
Public Class Corners_Core : Inherits TaskParent
    Dim options As New Options_FAST
    Public features As New List(Of cv.Point2f)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U)
        desc = "Find interesting points with the FAST (Features from Accelerated Segment Test) algorithm"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = src.Clone
        Dim kpoints() As cv.KeyPoint = cv.Cv2.FAST(task.gray, task.FASTthreshold, options.useNonMax)

        features.Clear()
        task.featurePoints.Clear()
        For Each kp As cv.KeyPoint In kpoints
            task.featurePoints.Add(New cv.Point(kp.Pt.X, kp.Pt.Y))
            features.Add(kp.Pt)
        Next

        If standaloneTest() Then
            dst3.SetTo(0)
            For Each kp As cv.KeyPoint In kpoints
                DrawCircle(dst2, kp.Pt, task.DotSize, cv.Scalar.Yellow)
                dst3.Set(Of Byte)(kp.Pt.Y, kp.Pt.X, 255)
            Next
        End If
        labels(2) = "There were " + CStr(features.Count) + " key points detected using FAST"
    End Sub
End Class






' https://docs.opencvb.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_Harris : Inherits TaskParent
    Dim options As New Options_HarrisCorners
    Dim gray As New cv.Mat
    Dim mc As New cv.Mat, mm As mmData

    Public Sub New()
        desc = "Find corners using Eigen values and vectors"
        labels(3) = "Corner Eigen values"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        mc = New cv.Mat(task.gray.Size(), cv.MatType.CV_32FC1, 0)
        dst2 = New cv.Mat(task.gray.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        cv.Cv2.CornerEigenValsAndVecs(task.gray, dst2, options.blockSize, options.aperture, cv.BorderTypes.Default)

        For y = 0 To task.gray.Rows - 1
            For x = 0 To task.gray.Cols - 1
                Dim lambda_1 = dst2.Get(Of cv.Vec6f)(y, x)(0)
                Dim lambda_2 = dst2.Get(Of cv.Vec6f)(y, x)(1)
                mc.Set(Of Single)(y, x, lambda_1 * lambda_2 - 0.04 * Math.Pow(lambda_1 + lambda_2, 2))
            Next
        Next

        mm = GetMinMax(mc)

        src.CopyTo(dst2)
        Dim count As Integer
        For y = 0 To task.gray.Rows - 1
            For x = 0 To task.gray.Cols - 1
                If mc.Get(Of Single)(y, x) > mm.minVal + (mm.maxVal - mm.minVal) * options.quality / options.qualityMax Then
                    DrawCircle(dst2, New cv.Point(x, y), task.DotSize, task.highlight)
                    count += 1
                End If
            Next
        Next

        labels(2) = "Corners_Harris found " + CStr(count) + " corners in the image."

        Dim McNormal As New cv.Mat
        cv.Cv2.Normalize(mc, McNormal, 127, 255, cv.NormTypes.MinMax)
        McNormal.ConvertTo(dst3, cv.MatType.CV_8U)
    End Sub
End Class






Public Class Corners_PreCornerDetect : Inherits TaskParent
    Dim median As New Math_Median_CDF
    Dim options As New Options_PreCorners
    Public Sub New()
        desc = "Use PreCornerDetect to find features in the image."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        Dim prob As New cv.Mat
        cv.Cv2.PreCornerDetect(task.gray, prob, options.kernelSize)

        cv.Cv2.Normalize(prob, prob, 0, 255, cv.NormTypes.MinMax)
        prob.ConvertTo(task.gray, cv.MatType.CV_8U)
        median.Run(task.gray.Clone())
        dst2 = task.gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = task.gray.Threshold(160, 255, cv.ThresholdTypes.BinaryInv).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        labels(3) = "median = " + CStr(median.medianVal)
    End Sub
End Class




' https://docs.opencvb.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_ShiTomasi_CPP : Inherits TaskParent
    Dim options As New Options_ShiTomasi
    Public Sub New()
        desc = "Find corners using Eigen values and vectors"
        labels(3) = "Corner Eigen values using ShiTomasi which is also what is used in GoodFeatures."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        Dim data(task.gray.Total - 1) As Byte
        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        Marshal.Copy(task.gray.Data, data, 0, data.Length)
        Dim imagePtr = Corners_ShiTomasi(handle.AddrOfPinnedObject, src.Rows, src.Cols,
                                         options.blocksize, options.aperture)
        handle.Free()

        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32F, imagePtr).Clone

        dst3 = Convert32f_To_8UC3(dst2)
        dst3 = dst3.Threshold(options.threshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class





Public Class Corners_BasicsCentroid : Inherits TaskParent
    Dim fast As New Corners_Basics
    Public Sub New()
        task.kalman = New Kalman_Basics
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        ReDim task.kalman.kInput(1) ' 2 elements - cv.point
        desc = "Find interesting points with the FAST and smooth the centroid with kalman"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        fast.Run(src)
        dst2 = fast.dst2
        dst3.SetTo(0)
        For Each pt In fast.features
            DrawCircle(dst3, pt, task.DotSize + 2, white)
        Next
        Dim m = cv.Cv2.Moments(dst3, True)
        If m.M00 > 500 Then ' if more than x pixels are present (avoiding a zero area!)
            task.kalman.kInput(0) = m.M10 / m.M00
            task.kalman.kInput(1) = m.M01 / m.M00
            task.kalman.Run(emptyMat)
            DrawCircle(dst2, New cv.Point(task.kalman.kOutput(0), task.kalman.kOutput(1)), 10, cv.Scalar.Red)
        End If
    End Sub
End Class








Public Class Corners_BasicsCentroids : Inherits TaskParent
    Dim fast As New Corners_Basics
    Dim fastCenters() As cv.Point2f
    Public Sub New()
        desc = "Use a thread grid to find the centroids in each grid element"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        fast.Run(src)
        ReDim fastCenters(task.gridRects.Count - 1)
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            Dim tmp = fast.dst3(roi).FindNonZero()
            If tmp.Rows > 0 Then
                Dim mean = tmp.Mean()
                fastCenters(i) = New cv.Point2f(roi.X + mean(0), roi.Y + mean(1))
            End If
        Next

        For i = 0 To fastCenters.Count - 1
            DrawCircle(dst2, fastCenters(i), task.DotSize, cv.Scalar.Yellow)
        Next
        ' dst2.SetTo(white, task.gridMask)
    End Sub
End Class









' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class Corners_Harris_CPP : Inherits TaskParent
    Dim options As New Options_Harris
    Public Sub New()
        cPtr = Harris_Features_Open()
        desc = "Use Harris feature detectors to identify interesting points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim dataSrc(task.gray.Total - 1) As Byte
        Marshal.Copy(task.gray.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Harris_Features_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.threshold,
                                           options.neighborhood, options.aperture, options.harrisParm)
        handleSrc.Free()

        Dim gray32f = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32F, imagePtr)
        '  gray32f = Convert32f_To_8UC3(gray32f)
        gray32f.ConvertTo(dst2, cv.MatType.CV_8U)

        dst3 = ShowAddweighted(dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.color, labels(3))
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Harris_Features_Close(cPtr)
    End Sub
End Class






' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class Corners_HarrisDetector_CPP : Inherits TaskParent
    Public features As New List(Of cv.Point2f)
    Dim options As New Options_Features
    Public Sub New()
        desc = "Use Harris detector to identify interesting points."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If cPtr = 0 Then cPtr = Harris_Detector_Open()
        options.Run()

        dst2 = src.Clone

        Dim dataSrc(task.gray.Total) As Byte
        Marshal.Copy(task.gray.Data, dataSrc, 0, dataSrc.Length)

        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Harris_Detector_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.quality)
        handleSrc.Free()
        Dim ptCount = Harris_Detector_Count(cPtr)
        If ptCount > 1 Then
            Dim ptMat = cv.Mat.FromPixelData(ptCount, 2, cv.MatType.CV_32S, imagePtr).Clone
            features.Clear()
            For i = 0 To ptCount - 1
                features.Add(New cv.Point2f(ptMat.Get(Of Integer)(i, 0), ptMat.Get(Of Integer)(i, 1)))
                DrawCircle(dst2, features(i), task.DotSize, cv.Scalar.Yellow)
            Next
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Harris_Detector_Close(cPtr)
    End Sub
End Class








Public Class Corners_RedCloud : Inherits TaskParent
    Dim corners As New Neighbor_Intersects
    Public Sub New()
        labels = {"", "", "Grayscale", "Highlighted points show where more than 2 cells intersect."}
        desc = "Find the corners for each RedCloud cell."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst2 = runRedOld(src, labels(2))

        corners.Run(task.redC.rcMap)

        dst3 = task.color.Clone
        For Each pt In corners.nPoints
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
            DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Yellow)
        Next
    End Sub
End Class




Public Class Corners_SubPix : Inherits TaskParent
    Dim fast As New Corners_Basics
    Dim options As New Options_PreCorners
    Public Sub New()
        labels(2) = "Output of PreCornerDetect"
        desc = "Use PreCornerDetect to refine the feature points to sub-pixel accuracy."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        fast.Run(src)

        If fast.features.Count = 0 Then Exit Sub ' completely dark?  No features...
        cv.Cv2.CornerSubPix(task.gray, fast.features, New cv.Size(options.subpixSize, options.subpixSize),
                            New cv.Size(-1, -1), term)

        dst2 = src
        For Each pt In fast.features
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
        Next
    End Sub
End Class