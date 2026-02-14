Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://github.com/ufoym
Namespace VBClasses
    Public Class NR_RecursiveBilateralFilter_CPP : Inherits TaskParent
        Implements IDisposable
        Dim dataSrc(0) As Byte
        Dim options As New Options_RBF
        Public Sub New()
            cPtr = RecursiveBilateralFilter_Open()
            desc = "Apply the recursive bilateral filter"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If dataSrc.Length <> src.Total * src.ElemSize Then ReDim dataSrc(src.Total * src.ElemSize - 1)
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
            Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
            Dim imagePtr = RecursiveBilateralFilter_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                                        options.RBFCount)
            handleSrc.Free()

            dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr).Clone
        End Sub
        Protected Overrides Sub Finalize()
            If cPtr <> 0 Then cPtr = RecursiveBilateralFilter_Close(cPtr)
        End Sub
    End Class




End Namespace