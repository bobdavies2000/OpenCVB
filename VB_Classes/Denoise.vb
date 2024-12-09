Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Denoise_Basics_CPP_VB : Inherits TaskParent
    Dim diff As New Diff_Basics
    Public Sub New()
        cPtr = Denoise_Basics_Open(3)
        labels = {"", "", "Input image", "Output: Use PixelViewer to see changes"}
        desc = "Denoise example."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY) - 1
        Dim dataSrc(src.Total - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = Denoise_Basics_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then
            dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC1, imagePtr).Clone
            diff.Run(dst2)
            dst3 = diff.dst2
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Denoise_Basics_Close(cPtr)
    End Sub
End Class






Public Class Denoise_Pixels_CPP_VB : Inherits TaskParent
    Public classCount As Integer
    Dim reduction As New Reduction_Basics
    Dim options As New Options_Denoise
    Public Sub New()
        cPtr = Denoise_Pixels_Open()
        labels = {"", "", "Before removing single pixels", "After removing single pixels"}
        desc = "Remove single pixels between identical pixels"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If standaloneTest() Then
            reduction.Run(src)
            src = reduction.dst2
            classCount = reduction.classCount
        End If
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        If options.removeSinglePixels Then
            Dim cppData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, cppData, 0, cppData.Length)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = Denoise_Pixels_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
            handleSrc.Free()
            dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC1, imagePtr).Clone
        Else
            dst2 = src
        End If

        If standaloneTest() Then
            dst2 *= 255 / classCount
            dst3 = ShowPalette(dst2)
        End If
        If task.heartBeat Then
            strOut = CStr(classCount) + " pixel classes" + vbCrLf
            strOut += CStr(Denoise_Pixels_EdgeCountBefore(cPtr)) + " edges before" + vbCrLf
            strOut += CStr(Denoise_Pixels_EdgeCountAfter(cPtr)) + " edges after"
        End If
        SetTrueText(strOut, 2)
    End Sub
    Public Sub Close()
        Denoise_Pixels_Close(cPtr)
    End Sub
End Class





Public Class Denoise_Reliable : Inherits TaskParent
    Dim denoise As New Denoise_SinglePixels_CPP_VB
    Dim relyGray As New Reliable_Gray
    Public Sub New()
        task.gOptions.setPixelDifference(10)
        labels(2) = "Before denoising"
        labels(3) = "After denoising single pixels"
        desc = "Manually remove single pixels in the binary image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        relyGray.Run(src)
        dst2 = relyGray.dst2

        denoise.Run(dst2)
        dst3 = denoise.dst2
    End Sub
End Class





Public Class Denoise_SinglePixels_CPP_VB : Inherits TaskParent
    Dim options As New Options_Denoise
    Public Sub New()
        cPtr = Denoise_SinglePixels_Open()
        labels = {"", "", "Input image", "Output: Use PixelViewer to see changes"}
        desc = "Remove any single pixels sitting alone..."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If options.removeSinglePixels Then
            If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
            Dim dataSrc(src.Total - 1) As Byte
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
            Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
            Dim imagePtr = Denoise_SinglePixels_Run(cPtr, handleSrc.AddrOfPinnedObject(),
                                                    src.Rows, src.Cols)
            handleSrc.Free()

            If imagePtr <> 0 Then dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, src.Type, imagePtr).Clone
        Else
            dst2 = src
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Denoise_SinglePixels_Close(cPtr)
    End Sub
End Class