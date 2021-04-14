Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://docs.opencv.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_Harris
    Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Corner block size", 1, 21, 3)
            sliders.setupTrackBar(1, "Corner aperture size", 1, 21, 3)
            sliders.setupTrackBar(2, "Corner quality level", 1, 100, 50)
        End If
        task.desc = "Find corners using Eigen values and vectors"
		' task.rank = 1
        label2 = "Corner Eigen values"
    End Sub
    Public Sub Run(src as cv.Mat)
        Static color As New cv.Mat
        Static gray As New cv.Mat
        Static mc As New cv.Mat
        Static minval As Double, maxval As Double

        gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        mc = New cv.Mat(gray.Size(), cv.MatType.CV_32FC1, 0)
        dst1 = New cv.Mat(gray.Size(), cv.MatType.CV_8U, 0)
        Dim blocksize = sliders.trackbar(0).Value
        If blocksize Mod 2 = 0 Then blocksize += 1
        Dim aperture = sliders.trackbar(1).Value
        If aperture Mod 2 = 0 Then aperture += 1
        cv.Cv2.CornerEigenValsAndVecs(gray, dst1, blocksize, aperture, cv.BorderTypes.Default)

        For j = 0 To gray.Rows - 1
            For i = 0 To gray.Cols - 1
                Dim lambda_1 = dst1.Get(Of cv.Vec6f)(j, i)(0)
                Dim lambda_2 = dst1.Get(Of cv.Vec6f)(j, i)(1)
                mc.Set(Of Single)(j, i, lambda_1 * lambda_2 - 0.04 * Math.Pow(lambda_1 + lambda_2, 2))
            Next
        Next

        mc.MinMaxLoc(minval, maxval)

        src.CopyTo(dst1)
        For j = 0 To gray.Rows - 1
            For i = 0 To gray.Cols - 1
                If mc.Get(Of Single)(j, i) > minval + (maxval - minval) * sliders.trackbar(2).Value / sliders.trackbar(2).Maximum Then
                    dst1.Circle(New cv.Point(i, j), 4, cv.Scalar.White, -1, task.lineType)
                    dst1.Circle(New cv.Point(i, j), 2, cv.Scalar.Red, -1, task.lineType)
                End If
            Next
        Next

        Dim McNormal As New cv.Mat
        cv.Cv2.Normalize(mc, McNormal, 127, 255, cv.NormTypes.MinMax)
        McNormal.ConvertTo(dst2, cv.MatType.CV_8U)
    End Sub
End Class




Public Class Corners_SubPix
    Inherits VBparent
    Public good As Features_GoodFeatures
    Public Sub New()
        good = New Features_GoodFeatures()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "SubPix kernel Size", 1, 20, 3)
        End If
        label1 = "Output of GoodFeatures"
        task.desc = "Use PreCornerDetect to find features in the image."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        good.Run(src)
        If good.goodFeatures.Count = 0 Then Exit Sub ' no good features right now...
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim winSize = New cv.Size(sliders.trackbar(0).Value, sliders.trackbar(0).Value)
        cv.Cv2.CornerSubPix(gray, good.goodFeatures, winSize, New cv.Size(-1, -1), term)

        src.CopyTo(dst1)
        Dim p As New cv.Point
        For i = 0 To good.goodFeatures.Count - 1
            p.X = CInt(good.goodFeatures(i).X)
            p.Y = CInt(good.goodFeatures(i).Y)
            cv.Cv2.Circle(dst1, p, 3, New cv.Scalar(0, 0, 255), -1, task.lineType)
        Next
    End Sub
End Class




Public Class Corners_PreCornerDetect
    Inherits VBparent
    Dim median As Math_Median_CDF
    Public Sub New()
        median = New Math_Median_CDF()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "kernel Size", 1, 20, 19)
        End If
        task.desc = "Use PreCornerDetect to find features in the image."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim ksize = sliders.trackbar(0).Value
        If ksize Mod 2 = 0 Then ksize += 1
        Dim prob As New cv.Mat
        cv.Cv2.PreCornerDetect(gray, prob, ksize)

        cv.Cv2.Normalize(prob, prob, 0, 255, cv.NormTypes.MinMax)
        prob.ConvertTo(gray, cv.MatType.CV_8U)
        median.Run(gray.Clone())
        dst1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = gray.Threshold(160, 255, cv.ThresholdTypes.BinaryInv).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        label2 = "median = " + CStr(median.medianVal)
    End Sub
End Class



Module corners_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Corners_ShiTomasi(grayPtr As IntPtr, rows As integer, cols As integer, blocksize As integer, aperture As integer) As IntPtr
    End Function
End Module



' https://docs.opencv.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_ShiTomasi_CPP
    Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Corner block size", 1, 21, 3)
            sliders.setupTrackBar(1, "Corner aperture size", 1, 21, 3)
            sliders.setupTrackBar(2, "Corner quality level", 1, 100, 50)
            sliders.setupTrackBar(3, "Corner normalize alpha", 1, 255, 127)
        End If
        task.desc = "Find corners using Eigen values and vectors"
		' task.rank = 1
        label2 = "Corner Eigen values"
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim data(src.Total - 1) As Byte

        Dim blocksize = If(sliders.trackbar(0).Value Mod 2, sliders.trackbar(0).Value, sliders.trackbar(0).Value + 1)
        Dim aperture = If(sliders.trackbar(1).Value Mod 2, sliders.trackbar(1).Value, sliders.trackbar(1).Value + 1)

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        Marshal.Copy(dst1.Data, data, 0, data.Length)
        Dim imagePtr = Corners_ShiTomasi(handle.AddrOfPinnedObject, src.Rows, src.Cols, blocksize, aperture)
        handle.Free()

        Dim output As New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_32F, imagePtr)

        Dim stNormal As New cv.Mat
        cv.Cv2.Normalize(output, stNormal, sliders.trackbar(3).Value, 255, cv.NormTypes.MinMax)
        stNormal.ConvertTo(dst2, cv.MatType.CV_8U)
    End Sub
End Class

