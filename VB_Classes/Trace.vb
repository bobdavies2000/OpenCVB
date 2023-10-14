Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module Trace_OpenCV_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Trace_OpenCV_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Trace_OpenCV_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Trace_OpenCV_Run(Trace_OpenCVPtr As IntPtr, rgbPtr As IntPtr, rows As integer, cols As integer, channels As integer) As IntPtr
    End Function
End Module





' https://github.com/opencv/opencv/wiki/Profiling-OpenCV-Applications
Public Class Trace_OpenCV_CPP : Inherits VB_Algorithm
    Public Sub New()
        cPtr = Trace_OpenCV_Open()
        desc = "Use OpenCV's Trace facility - applicable to C++ code - and requires Intel's VTune (see link in code.)"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Trace_OpenCV_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        If imagePtr <> 0 Then dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr).Clone
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Trace_OpenCV_Close(cPtr)
    End Sub
End Class