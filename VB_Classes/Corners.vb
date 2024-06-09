Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/FASTSample.vb
Public Class Corners_Basics : Inherits VB_Parent
    Public features As New List(Of cv.Point2f)
    Dim options As New Options_Features
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Use Non-Max = True")
            check.Box(0).Checked = True
        End If
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        desc = "Find interesting points with the FAST (Features from Accelerated Segment Test) algorithm"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = FindSlider("FAST Threshold")
        Static nonMaxCheck = findCheckBox("Use Non-Max = True")

        dst2 = src.Clone
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim kpoints() As cv.KeyPoint = cv.Cv2.FAST(src, thresholdSlider.Value, nonMaxCheck.checked)

        features.Clear()
        For Each kp As cv.KeyPoint In kpoints
            features.Add(kp.Pt)
        Next

        If standaloneTest() Then
            dst3.SetTo(0)
            For Each kp As cv.KeyPoint In kpoints
                DrawCircle(dst2, kp.Pt, task.dotSize, cv.Scalar.Yellow)
                dst3.Set(Of Byte)(kp.Pt.Y, kp.Pt.X, 255)
            Next
        End If
        labels(2) = "There were " + CStr(features.Count) + " key points detected using FAST"
    End Sub
End Class






' https://docs.opencv.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_Harris : Inherits VB_Parent
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
        Static blockSlider = FindSlider("Corner block size")
        Static apertureSlider = FindSlider("Corner aperture size")
        Static qualitySlider = FindSlider("Corner quality level")
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
        mm = GetMinMax(mc)

        src.CopyTo(dst2)
        For j = 0 To gray.Rows - 1
            For i = 0 To gray.Cols - 1
                If mc.Get(Of Single)(j, i) > mm.minVal + (mm.maxVal - mm.minVal) * quality / qualitySlider.Maximum Then
                    DrawCircle(dst2,New cv.Point(i, j), task.dotSize, task.highlightColor)
                End If
            Next
        Next

        Dim McNormal As New cv.Mat
        cv.Cv2.Normalize(mc, McNormal, 127, 255, cv.NormTypes.MinMax)
        McNormal.ConvertTo(dst3, cv.MatType.CV_8U)
    End Sub
End Class





Public Class Corners_PreCornerDetect : Inherits VB_Parent
    Dim median As New Math_Median_CDF
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("kernel Size", 1, 20, 19)
        desc = "Use PreCornerDetect to find features in the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static kernelSlider = FindSlider("kernel Size")
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
Public Class Corners_ShiTomasi_CPP : Inherits VB_Parent
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Corner block size", 1, 21, 3)
            sliders.setupTrackBar("Corner aperture size", 1, 21, 3)
            sliders.setupTrackBar("Corner normalize threshold", 0, 32, 0)
        End If
        desc = "Find corners using Eigen values and vectors"
        labels(3) = "Corner Eigen values using ShiTomasi which is also what is used in GoodFeatures."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static blockSlider = FindSlider("Corner block size")
        Static apertureSlider = FindSlider("Corner aperture size")
        Static thresholdSlider = FindSlider("Corner normalize threshold")
        Dim threshold = thresholdSlider.Value

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim data(src.Total - 1) As Byte
        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        Marshal.Copy(src.Data, data, 0, data.Length)
        Dim imagePtr = Corners_ShiTomasi(handle.AddrOfPinnedObject, src.Rows, src.Cols, blockSlider.Value Or 1, apertureSlider.Value Or 1)
        handle.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_32F, imagePtr).Clone

        dst3 = GetNormalize32f(dst2)
        dst3 = dst3.Threshold(threshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class





Public Class Corners_BasicsCentroid : Inherits VB_Parent
    Dim fast As New Corners_Basics
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
            DrawCircle(dst3, pt, task.dotSize + 2, cv.Scalar.White)
        Next
        Dim gray = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim m = cv.Cv2.Moments(gray, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run(src)
            DrawCircle(dst3, New cv.Point(kalman.kOutput(0), kalman.kOutput(1)), 10, cv.Scalar.Red)
        End If
    End Sub
End Class






Public Class Corners_BasicsStablePoints : Inherits VB_Parent
    Public features As New List(Of cv.Point)
    Dim fast As New Corners_Basics
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
                DrawCircle(dst2, test, task.dotSize, cv.Scalar.Yellow)
                newPts.Add(test)
                dst3.Set(Of Byte)(test.Y, test.X, 255)
            End If
        Next

        features = New List(Of cv.Point)(newPts)
        labels(2) = Format(features.Count, "000") + " identified FAST stable points - slider adjusts threshold"
    End Sub
End Class







Public Class Corners_BasicsCentroids : Inherits VB_Parent
    Dim fast As New Corners_Basics
    Dim fastCenters() As cv.Point2f
    Public Sub New()
        If standaloneTest() Then task.gOptions.setGridSize(16)
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
            DrawCircle(dst2, fastCenters(i), task.dotSize, cv.Scalar.Yellow)
        Next
        dst2.SetTo(cv.Scalar.White, task.gridMask)
    End Sub
End Class









' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class Corners_Harris_CPP : Inherits VB_Parent
    Dim addw As New AddWeighted_Basics
    Dim options As New Options_Harris
    Public Sub New()
        cPtr = Harris_Features_Open()
        desc = "Use Harris feature detectors to identify interesting points."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim dataSrc(src.Total - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Harris_Features_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.threshold,
                                           options.neighborhood, options.aperture, options.harrisParm)
        handleSrc.Free()

        Dim gray32f = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_32F, imagePtr)
        '  gray32f = GetNormalize32f(gray32f)
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
Public Class Corners_HarrisDetector : Inherits VB_Parent
    Public features As New List(Of cv.Point2f)
    Dim options As New Options_Features
    Public Sub New()
        desc = "Use Harris detector to identify interesting points."
        cPtr = Harris_Detector_Open()
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static qualitySlider = FindSlider("Quality Level")
        dst2 = src.Clone

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim dataSrc(src.Total) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)

        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Harris_Detector_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, qualitySlider.Value / 100)
        handleSrc.Free()
        Dim ptCount = Harris_Detector_Count(cPtr)
        If ptCount > 1 Then
            Dim ptMat = New cv.Mat(ptCount, 2, cv.MatType.CV_32S, imagePtr).Clone
            features.Clear()
            For i = 0 To ptCount - 1
                features.Add(New cv.Point2f(ptMat.Get(Of Integer)(i, 0), ptMat.Get(Of Integer)(i, 1)))
                DrawCircle(dst2,features(i), task.dotSize, cv.Scalar.Yellow)
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
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        corners.Run(task.cellMap)

        dst3 = task.color.Clone
        For Each pt In corners.nPoints
            DrawCircle(dst2,pt, task.dotSize, task.highlightColor)
            DrawCircle(dst3,pt, task.dotSize, cv.Scalar.Yellow)
        Next
    End Sub
End Class




Public Class Corners_SubPix : Inherits VB_Parent
    Public feat As New Feature_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("SubPix kernel Size", 1, 20, 3)
        labels(2) = "Output of PreCornerDetect"
        desc = "Use PreCornerDetect to refine the feature points to sub-pixel accuracy."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static kernelSlider = FindSlider("SubPix kernel Size")
        Dim kernelSize As Integer = kernelSlider.value Or 1

        dst2 = src.Clone
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        feat.Run(src)
        cv.Cv2.CornerSubPix(src, task.features, New cv.Size(kernelSize, kernelSize), New cv.Size(-1, -1), term)

        task.featurePoints.Clear()
        For i = 0 To task.features.Count - 1
            Dim pt = task.features(i)
            task.featurePoints.Add(New cv.Point(pt.X, pt.Y))
            DrawCircle(dst2,pt, task.dotSize, task.highlightColor)
        Next
    End Sub
End Class