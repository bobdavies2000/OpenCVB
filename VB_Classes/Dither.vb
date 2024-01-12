Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://www.codeproject.com/Articles/5259216/Dither-Ordered-and-Floyd-Steinberg-Monochrome-Colo
Public Class Dither_Basics : Inherits VB_Algorithm
    Dim options As New Options_Dither
    Public Sub New()
        labels = {"", "", "Dither applied to the BGR image", "Dither applied to the Depth image"}
        desc = "Explore all the varieties of dithering"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        Dim w = dst2.Width
        Dim h = dst2.Height
        Dim nColors = Choose(options.bppIndex, 1, 3, 7, 15, 31) ' indicate 3, 6, 9, 12, 15 bits per pixel.
        Dim pixels(dst2.Total * dst2.ElemSize - 1) As Byte
        Dim hpixels = GCHandle.Alloc(pixels, GCHandleType.Pinned)
        For i = 0 To 1
            Dim copySrc = Choose(i + 1, src, task.depthRGB)
            Marshal.Copy(copySrc.Data, pixels, 0, pixels.Length)
            Select Case options.radioIndex
                Case 0
                    ditherBayer16(hpixels.AddrOfPinnedObject, w, h)
                Case 1
                    ditherBayer8(hpixels.AddrOfPinnedObject, w, h)
                Case 2
                    ditherBayer4(hpixels.AddrOfPinnedObject, w, h)
                Case 3
                    ditherBayer3(hpixels.AddrOfPinnedObject, w, h)
                Case 4
                    ditherBayer2(hpixels.AddrOfPinnedObject, w, h)
                Case 5
                    ditherBayerRgbNbpp(hpixels.AddrOfPinnedObject, w, h, nColors)
                Case 6
                    ditherBayerRgb3bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 7
                    ditherBayerRgb6bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 8
                    ditherBayerRgb9bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 9
                    ditherBayerRgb12bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 10
                    ditherBayerRgb15bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 11
                    ditherBayerRgb18bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 12
                    ditherFSRgbNbpp(hpixels.AddrOfPinnedObject, w, h, nColors)
                Case 13
                    ditherFS(hpixels.AddrOfPinnedObject, w, h)
                Case 14
                    ditherFSRgb3bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 15
                    ditherFSRgb6bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 16
                    ditherFSRgb9bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 17
                    ditherFSRgb12bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 18
                    ditherFSRgb15bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 19
                    ditherFSRgb18bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 20
                    ditherSierraLiteRgbNbpp(hpixels.AddrOfPinnedObject, w, h, nColors)
                Case 21
                    ditherSierraLite(hpixels.AddrOfPinnedObject, w, h)
                Case 22
                    ditherSierraRgbNbpp(hpixels.AddrOfPinnedObject, w, h, nColors)
                Case 23
                    ditherSierra(hpixels.AddrOfPinnedObject, w, h)
            End Select
            If i = 0 Then
                dst2 = New cv.Mat(src.Height, src.Width, cv.MatType.CV_8UC3, pixels).Clone()
            Else
                dst3 = New cv.Mat(src.Height, src.Width, cv.MatType.CV_8UC3, pixels).Clone()
            End If
        Next
        hpixels.Free()
    End Sub
End Class


