Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Salience_Basics_CPP_VB : Inherits TaskParent
    Dim grayData(0) As Byte
    Public options As New Options_Salience
    Public Sub New()
        cPtr = Salience_Open()
        desc = "Show results of Salience algorithm when using C++"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If src.Total <> grayData.Length Then ReDim grayData(src.Total - 1)
        Dim grayHandle = GCHandle.Alloc(grayData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, grayData, 0, grayData.Length)
        Dim imagePtr = Salience_Run(cPtr, options.numScales, grayHandle.AddrOfPinnedObject, src.Height, src.Width)
        grayHandle.Free()

        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8U, imagePtr).Clone
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Salience_Close(cPtr)
    End Sub
End Class



Public Class Salience_Basics_MT : Inherits TaskParent
    Dim salience As New Salience_Basics_CPP_VB
    Public Sub New()
        FindSlider("Salience numScales").Value = 2
        desc = "Show results of multi-threaded Salience algorithm when using C++.  NOTE: salience is relative."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim threads = 32
        Dim h = CInt(src.Height / threads)
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        Parallel.For(0, threads,
            Sub(i)
                Dim roi = New cvb.Rect(0, i * h, src.Width, Math.Min(h, src.Height - i * h))
                If roi.Height <= 0 Then Exit Sub

                Dim cPtr = Salience_Open()
                Dim input = src(roi).Clone()
                Dim grayData(input.Total - 1) As Byte
                Dim grayHandle = GCHandle.Alloc(grayData, GCHandleType.Pinned)
                Marshal.Copy(input.Data, grayData, 0, grayData.Length)
                Dim imagePtr = Salience_Run(cPtr, salience.options.numScales, grayHandle.AddrOfPinnedObject, roi.Height, roi.Width)
                grayHandle.Free()

                dst2(roi) = cvb.Mat.FromPixelData(roi.Height, roi.Width, cvb.MatType.CV_8U, imagePtr).Clone
                If cPtr <> 0 Then cPtr = Salience_Close(cPtr)
            End Sub)
    End Sub
End Class



