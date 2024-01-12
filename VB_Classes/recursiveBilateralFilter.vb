Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://github.com/ufoym
Public Class RecursiveBilateralFilter_CPP : Inherits VB_Algorithm
    Dim dataSrc(0) As Byte
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("RBF Recursion count", 1, 20, 2)
        cPtr = RecursiveBilateralFilter_Open()
        desc = "Apply the recursive bilateral filter"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static countSlider = findSlider("RBF Recursion count")
        If dataSrc.Length <> src.Total * src.ElemSize Then ReDim dataSrc(src.Total * src.ElemSize - 1)
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = RecursiveBilateralFilter_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, countSlider.Value)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr).Clone
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RecursiveBilateralFilter_Close(cPtr)
    End Sub
End Class



