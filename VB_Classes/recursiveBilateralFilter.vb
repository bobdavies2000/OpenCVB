Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module RecursiveBilateralFilter_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RecursiveBilateralFilter_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub RecursiveBilateralFilter_Close(rbf As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RecursiveBilateralFilter_Run(rbf As IntPtr, inputPtr As IntPtr, rows As integer, cols As integer, recursions As integer) As IntPtr
    End Function
End Module


' https://github.com/ufoym
Public Class RecursiveBilateralFilter_CPP : Inherits VBparent
    Dim srcData(0) As Byte
    Dim rbf As IntPtr
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "RBF Recursion count", 1, 20, 2)
        End If
        rbf = RecursiveBilateralFilter_Open()
        task.desc = "Apply the recursive bilateral filter"
    End Sub
    Public Sub Run(src as cv.Mat)
        If srcData.Length <> src.Total * src.ElemSize Then ReDim srcData(src.Total * src.ElemSize - 1)
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = RecursiveBilateralFilter_Run(rbf, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, sliders.trackbar(0).Value)
        handleSrc.Free() ' free the pinned memory...

        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)
    End Sub
    Public Sub Close()
        RecursiveBilateralFilter_Close(rbf)
    End Sub
End Class



