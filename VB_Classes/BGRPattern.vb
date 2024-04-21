Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class BGRPattern_Basics : Inherits VB_Algorithm
    Dim denoise As New Denoise_Pixels
    Dim options As New Options_ColorFormat
    Public classCount As Integer
    Public Sub New()
        cPtr = BGRPattern_Open()
        vbAddAdvice(traceName + ": local options 'Options_ColorFormat' selects color.")
        desc = "Classify each 3-channel input pixel according to their relative values"
    End Sub
    Public Sub RunVB(ByVal src As cv.Mat)
        options.RunVB()
        src = options.dst2

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = BGRPattern_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(),
                                         src.Rows, src.Cols)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr).Clone

        classCount = BGRPattern_ClassCount(cPtr)
        denoise.classCount = classCount
        denoise.Run(dst2)
        dst2 = denoise.dst2

        If standaloneTest() Then
            dst2 = dst2 * 255 / classCount
            dst3 = vbPalette(dst2)
        End If
    End Sub
    Public Sub Close()
        BGRPattern_Close(cPtr)
    End Sub
End Class