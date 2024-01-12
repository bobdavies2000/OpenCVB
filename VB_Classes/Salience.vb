Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Salience_Basics_CPP : Inherits VB_Algorithm
    Dim grayData(0) As Byte
    Dim numScales As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Salience numScales", 1, 6, 6)
        cPtr = Salience_Open()
        desc = "Show results of Salience algorithm when using C++"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static numSlider = findSlider("Salience numScales")
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Total <> grayData.Length Then ReDim grayData(src.Total - 1)
        Dim grayHandle = GCHandle.Alloc(grayData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, grayData, 0, grayData.Length)
        Dim imagePtr = Salience_Run(cPtr, numSlider.Value, grayHandle.AddrOfPinnedObject, src.Height, src.Width)
        grayHandle.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Salience_Close(cPtr)
    End Sub
End Class



Public Class Salience_Basics_MT : Inherits VB_Algorithm
    Dim salience As New Salience_Basics_CPP
    Public Sub New()
        findSlider("Salience numScales").Value = 2
        desc = "Show results of multi-threaded Salience algorithm when using C++.  NOTE: salience is relative."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static scaleSlider = findSlider("Salience numScales")
        Dim numScales = scaleSlider.Value
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim threads = 32
        Dim h = CInt(src.Height / threads)
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
        Parallel.For(0, threads,
            Sub(i)
                Dim roi = New cv.Rect(0, i * h, src.Width, Math.Min(h, src.Height - i * h))
                If roi.Height <= 0 Then Exit Sub

                Dim cPtr = Salience_Open()
                Dim input = src(roi).Clone()
                Dim grayData(input.Total - 1) As Byte
                Dim grayHandle = GCHandle.Alloc(grayData, GCHandleType.Pinned)
                Marshal.Copy(input.Data, grayData, 0, grayData.Length)
                Dim imagePtr = Salience_Run(cPtr, numScales, grayHandle.AddrOfPinnedObject, roi.Height, roi.Width)
                grayHandle.Free()

                dst2(roi) = New cv.Mat(roi.Height, roi.Width, cv.MatType.CV_8U, imagePtr).Clone
                If cPtr <> 0 Then cPtr = Salience_Close(cPtr)
            End Sub)
    End Sub
End Class



