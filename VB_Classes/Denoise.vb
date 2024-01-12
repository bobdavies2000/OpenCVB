Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Denoise_Basics_CPP : Inherits VB_Algorithm
    Dim diff As New Diff_Basics
    Public Sub New()
        cPtr = Denoise_Basics_Open(3)
        labels = {"", "", "Input image", "Output: Use PixelViewer to see changes"}
        desc = "Denoise example."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src
        If dst2.Channels = 3 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY) - 1
        Dim dataSrc(dst2.Total - 1) As Byte
        Marshal.Copy(dst2.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Denoise_Basics_Run(cPtr, handleSrc.AddrOfPinnedObject(), dst2.Rows, dst2.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then
            dst3 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC1, imagePtr).Clone
            diff.lastGray = dst2
            diff.Run(dst3)
            dst3 = diff.dst2
            setTrueText("Denoise C++ code does not seem to be working.  More work needed", 3)
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Denoise_Basics_Close(cPtr)
    End Sub
End Class






Public Class Denoise_Pixels : Inherits VB_Algorithm
    Public classCount As Integer
    Dim options As New Options_Denoise
    Public Sub New()
        cPtr = Denoise_Pixels_Open()
        labels = {"", "", "Before removing single pixels", "After removing single pixels"}
        desc = "Remove single pixels between identical pixels"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If standalone Then
            Static reduction As New Reduction_Basics
            reduction.Run(src)
            src = reduction.dst2
            classCount = reduction.classCount
        End If
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If options.removeSinglePixels Then
            Dim cppData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = Denoise_Pixels_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
            handleSrc.Free()
            dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr).Clone
        Else
            dst2 = src
        End If

        If standalone Or testIntermediate(traceName) Then
            dst2 *= 255 / classCount
            dst3 = vbPalette(dst2)
        End If
        If heartBeat() Then
            strOut = CStr(classCount) + " pixel classes" + vbCrLf
            strOut += CStr(Denoise_Pixels_EdgeCountBefore(cPtr)) + " edges before" + vbCrLf
            strOut += CStr(Denoise_Pixels_EdgeCountAfter(cPtr)) + " edges after"
        End If
        setTrueText(strOut, 2)
    End Sub
    Public Sub Close()
        Denoise_Pixels_Close(cPtr)
    End Sub
End Class