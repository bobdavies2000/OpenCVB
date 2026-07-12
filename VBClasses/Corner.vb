Imports System.Runtime.InteropServices
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Corner_Basics : Inherits TaskParent
    Public fast As New Corner_Core
    Public features As New List(Of cv.Point2f)
    Public Sub New()
        labels = {"", "", "", "FAST stable points without context"}
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find and save only the stable points in the FAST output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lastFeatures = New HashSet(Of cv.Point2f)(fast.features)
        fast.Run(src)

        Dim threshold = fast.features.Count / 10

        dst2 = src
        dst3.SetTo(0)
        Dim newPts As New List(Of cv.Point)
        For i = 0 To fast.features.Count - 1
            Dim pt = fast.features(i)
            If lastFeatures.Contains(pt) Then
            Circle(dst2, pt, task.DotSize, cv.Scalar.Yellow, -1, task.lineType)
                newPts.Add(pt)
                dst3.Set(Of Byte)(pt.Y, pt.X, 255)
            End If
        Next

        If newPts.Count < threshold Then
            features = New List(Of cv.Point2f)(fast.features)
        Else
            features.Clear()
            For Each pt In newPts
                features.Add(pt)
            Next
        End If
        labels(2) = features.Count.ToString("000") + " identified FAST stable points - slider adjusts threshold"
    End Sub
End Class






' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/FASTSample.vb
Public Class Corner_Core : Inherits TaskParent
    Dim options As New Options_FAST
    Public features As New List(Of cv.Point2f)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U)
        desc = "Find interesting points with the FAST (Features from Accelerated Segment Test) algorithm"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = src.Clone
        Dim kpoints() As cv.KeyPoint = FAST(task.gray, options.FASTthreshold, options.useNonMax)

        features.Clear()
        For Each kp As cv.KeyPoint In kpoints
            features.Add(kp.Pt)
        Next

        If standaloneTest() Then
            dst3.SetTo(0)
            For Each kp As cv.KeyPoint In kpoints
            Circle(dst2, kp.Pt, task.DotSize, cv.Scalar.Yellow, -1, task.lineType)
                dst3.Set(Of Byte)(kp.Pt.Y, kp.Pt.X, 255)
            Next
        End If
        labels(2) = "There were " + CStr(features.Count) + " key points detected using FAST"
    End Sub
End Class






' https://docs.opencvb.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corner_Harris : Inherits TaskParent
    Dim options As New Options_HarrisCorners
    Dim gray As New cv.Mat
    Dim mc As New cv.Mat, mm As mmData

    Public Sub New()
        desc = "Find corners using Eigen values and vectors"
        labels(3) = "Corner Eigen values"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        mc = New cv.Mat(task.gray.Size(), cv.MatType.CV_32FC1, 0)
        dst2 = New cv.Mat(task.gray.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        CornerEigenValsAndVecs(task.gray, dst2, options.blockSize, options.aperture, cv.BorderTypes.Default)

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
                Circle(dst2, New cv.Point(x, y), task.DotSize, task.highlight, -1, task.lineType)
                    count += 1
                End If
            Next
        Next

        labels(2) = "Corner_Harris found " + CStr(count) + " corners in the image."

        Dim McNormal As New cv.Mat
        Normalize(mc, McNormal, 127, 255, cv.NormTypes.MinMax)
        McNormal.ConvertTo(dst3, cv.MatType.CV_8U)
    End Sub
End Class






Public Class XR_Corner_PreCornerDetect : Inherits TaskParent
    Dim median As New Math_Median_CDF
    Dim options As New Options_PreCorners
    Public Sub New()
        desc = "Use PreCornerDetect to find features in the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim prob As New cv.Mat
        PreCornerDetect(task.gray, prob, options.kernelSize)

        Normalize(prob, prob, 0, 255, cv.NormTypes.MinMax)
        prob.ConvertTo(task.gray, cv.MatType.CV_8U)
        median.Run(task.gray.Clone())
        CvtColor(task.gray, dst2, cv.ColorConversionCodes.GRAY2BGR)
        CvtColor(task.gray, dst3, cv.ColorConversionCodes.GRAY2BGR)
        Threshold(dst3, dst3, 160, 255, cv.ThresholdTypes.BinaryInv)
        labels(3) = "median = " + CStr(median.medianVal)
    End Sub
End Class




' https://docs.opencvb.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corner_ShiTomasi_CPP : Inherits TaskParent
    Dim options As New Options_ShiTomasi
    Public Sub New()
        desc = "Find corners using Eigen values and vectors"
        labels(3) = "Corner Eigen values using ShiTomasi which is also what is used in GoodFeatures."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim data(task.gray.Total - 1) As Byte
        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        task.gray.GetArray(Of Byte)(data)
        Dim imagePtr = Corner_ShiTomasi(handle.AddrOfPinnedObject, src.Rows, src.Cols, options.blocksize, options.aperture)
        handle.Free()

        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32F, imagePtr).Clone

        dst3 = Mat_Convert.Mat_32f_To_8UC3(dst2)
        Threshold(dst3, dst3, options.threshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class





Public Class XR_Corner_BasicsCentroid : Inherits TaskParent
    Dim fast As New Corner_Basics
    Dim kalman As New Kalman_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        ReDim kalman.kInput(1) ' 2 elements - cv.point
        desc = "Find interesting points with the FAST and smooth the centroid with kalman"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fast.Run(src)
        dst2 = fast.dst2
        dst3.SetTo(0)
        For Each pt In fast.features
        Circle(dst3, pt, task.DotSize + 2, white, -1, task.lineType)
        Next
        Dim m = cv.Cv2.Moments(dst3, True)
        If m.M00 > 500 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run(emptyMat)
            Circle(dst2, New cv.Point(kalman.kOutput(0), kalman.kOutput(1)), 10, cv.Scalar.Red, -1, task.lineType)
        End If
    End Sub
End Class








Public Class XR_Corner_BasicsCentroids : Inherits TaskParent
    Dim fast As New Corner_Basics
    Dim fastCenters() As cv.Point2f
    Public Sub New()
        desc = "Use a thread grid to find the centroids in each grid element"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        fast.Run(src)
        ReDim fastCenters(task.gridRects.Count - 1)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim tmp As New cv.Mat
            FindNonZero(fast.dst3(r), tmp)
            If tmp.Rows > 0 Then
                Dim meanVal = Mean(tmp)
                fastCenters(i) = New cv.Point2f(r.X + meanVal(0), r.Y + meanVal(1))
            End If
        Next

        For i = 0 To fastCenters.Count - 1
        Circle(dst2, fastCenters(i), task.DotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next
        ' dst2.SetTo(white, task.gridMask)
    End Sub
End Class









' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class XR_Corner_Harris_CPP : Inherits TaskParent
    Implements IDisposable
    Dim options As New Options_Harris
    Public Sub New()
        cPtr = Harris_Features_Open()
        desc = "Use Harris feature detectors to identify interesting points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim dataSrc(task.gray.Total - 1) As Byte
        task.gray.GetArray(Of Byte)(dataSrc)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Harris_Features_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.threshold,
                                               options.neighborhood, options.aperture, options.harrisParm)
        handleSrc.Free()

        Dim gray32f = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32F, imagePtr)
        gray32f.ConvertTo(dst2, cv.MatType.CV_8U)

        CvtColor(dst2, dst2, cv.ColorConversionCodes.GRAY2BGR)
        ShowAddweighted(dst2, task.color, labels(3))
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = Harris_Features_Close(cPtr)
    End Sub
End Class






' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class Corner_HarrisDetector_CPP : Inherits TaskParent
    Implements IDisposable
    Public features As New List(Of cv.Point2f)
    Dim options As New Options_Features
    Public Sub New()
        desc = "Use Harris detector to identify interesting points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If cPtr = 0 Then cPtr = Harris_Detector_Open()
        options.Run()

        dst2 = src.Clone

        Dim dataSrc(task.gray.Total) As Byte
        task.gray.GetArray(Of Byte)(dataSrc)

        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Harris_Detector_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.quality)
        handleSrc.Free()
        Dim ptCount = Harris_Detector_Count(cPtr)
        If ptCount > 1 Then
            Dim ptMat = cv.Mat.FromPixelData(ptCount, 2, cv.MatType.CV_32S, imagePtr).Clone
            features.Clear()
            For i = 0 To ptCount - 1
                features.Add(New cv.Point2f(ptMat.Get(Of Integer)(i, 0), ptMat.Get(Of Integer)(i, 1)))
                Circle(dst2, features(i), task.DotSize, cv.Scalar.Yellow, -1, task.lineType)
            Next
        End If
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = Harris_Detector_Close(cPtr)
    End Sub
End Class








Public Class XR_Corner_RedCloud : Inherits TaskParent
    Dim corners As New Neighbor_Intersects32S
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "Grayscale", "Highlighted points show where more than 2 cells intersect."}
        desc = "Find the corners for each RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        corners.Run(redC.rcMap)

        dst3 = task.color.Clone
        For Each pt In corners.nPoints
        Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
        Circle(dst3, pt, task.DotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next
    End Sub
End Class




Public Class XR_Corner_SubPix : Inherits TaskParent
    Dim fast As New Corner_Basics
    Dim options As New Options_PreCorners
    Public Sub New()
        labels(2) = "Output of PreCornerDetect"
        desc = "Use PreCornerDetect to refine the feature points to sub-pixel accuracy."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        fast.Run(src)

        If fast.features.Count = 0 Then Exit Sub ' completely dark?  No features...
        CornerSubPix(task.gray, fast.features, New cv.Size(options.subpixSize, options.subpixSize),
                                New cv.Size(-1, -1), term)

        dst2 = src
        For Each pt In fast.features
        Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
        Next
    End Sub
End Class





Public Class Corner_RedPrepData : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Dim fast As New Corner_Basics
    Public Sub New()
        desc = "Find the corners in the RedPrep XY data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst2 = prep.dst1
        labels(2) = prep.labels(2)

        dst3.SetTo(0)
        fast.Run(prep.dst3)
        dst3 = fast.dst2
        labels(3) = fast.labels(2)
    End Sub
End Class
