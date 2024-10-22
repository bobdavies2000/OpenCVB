Imports System.Runtime.InteropServices
Imports OpenCvSharp
Imports cvb = OpenCvSharp
Public Class BGRPattern_Basics : Inherits TaskParent
    Dim denoise As New Denoise_Pixels_CPP_VB
    Public colorFmt As New Color_Basics
    Public classCount As Integer
    Public Sub New()
        cPtr = BGRPattern_Open()
        UpdateAdvice(traceName + ": local options 'Options_ColorFormat' selects color.")
        desc = "Classify each 3-channel input pixel according to their relative values"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        colorFmt.Run(src)
        src = colorFmt.dst2

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = BGRPattern_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(),
                                         src.Rows, src.Cols)
        handleSrc.Free()

        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC1, imagePtr).Clone

        classCount = BGRPattern_ClassCount(cPtr)
        denoise.classCount = classCount
        denoise.Run(dst2)
        dst2 = denoise.dst2

        If standaloneTest() Then
            dst2 = dst2 * 255 / classCount
            dst3 = ShowPalette(dst2)
        End If
    End Sub
    Public Sub Close()
        BGRPattern_Close(cPtr)
    End Sub
End Class