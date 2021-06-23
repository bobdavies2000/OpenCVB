Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module Trace_OpenCV_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Trace_OpenCV_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Trace_OpenCV_Close(Trace_OpenCVPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Trace_OpenCV_Run(Trace_OpenCVPtr As IntPtr, rgbPtr As IntPtr, rows As integer, cols As integer, channels As integer) As IntPtr
    End Function
End Module





' https://github.com/opencv/opencv/wiki/Profiling-OpenCV-Applications
Public Class Trace_OpenCV_CPP : Inherits VBparent
    Dim Trace_OpenCV As IntPtr
    Public Sub New()
        Trace_OpenCV = Trace_OpenCV_Open()
        task.desc = "Use OpenCV's Trace facility - applicable to C++ code - and requires Intel's VTune (see link in code.)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = Trace_OpenCV_Run(Trace_OpenCV, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        If imagePtr <> 0 Then dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr)
    End Sub
    Public Sub Close()
        Trace_OpenCV_Close(Trace_OpenCV)
    End Sub
End Class







