Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Namespace VBClasses
    Public Class SuperPixel_Basics : Inherits TaskParent
        Public Sub New()
            desc = "A Better superpixel algorithm"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            dst3 = src
            For Each rc In atask.redList.oldrclist
                DrawTour(dst3(rc.rect), rc.contour, white, atask.lineWidth)
            Next
        End Sub
    End Class





    Public Class SuperPixel_Basics_CPP : Inherits TaskParent
        Implements IDisposable
        Public wireGrid As cv.Mat
        Public gridColor = white
        Dim options As New Options_SuperPixels
        Public Sub New()
            labels(3) = "Superpixel label data (0-255)"
            desc = "Sub-divide the image into super pixels."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If atask.optionsChanged Then
                If cPtr <> 0 Then SuperPixel_Close(cPtr)
                cPtr = SuperPixel_Open(src.Width, src.Height, options.numSuperPixels, options.numIterations, options.prior)
            End If

            Dim input = src
            If input.Channels() = 1 Then input = input.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            Dim dataSrc(input.Total * input.ElemSize - 1) As Byte
            Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)
            Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
            Dim imagePtr = SuperPixel_Run(cPtr, handleSrc.AddrOfPinnedObject())
            handleSrc.Free()

            dst2 = input
            dst2.SetTo(gridColor, cv.Mat.FromPixelData(input.Rows, input.Cols, cv.MatType.CV_8UC1, imagePtr))

            Dim labelData(input.Total * 4 - 1) As Byte ' labels are 32-bit integers.
            Dim labelPtr = SuperPixel_GetLabels(cPtr)
            Marshal.Copy(labelPtr, labelData, 0, labelData.Length)
            Dim labels = cv.Mat.FromPixelData(input.Rows, input.Cols, cv.MatType.CV_32S, labelData)
            If options.numSuperPixels < 255 Then labels *= 255 / options.numSuperPixels
            labels.ConvertTo(dst3, cv.MatType.CV_8U)
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            If cPtr <> 0 Then cPtr = SuperPixel_Close(cPtr)
        End Sub
    End Class






    Public Class NR_SuperPixel_BinarizedImage : Inherits TaskParent
        Dim pixels As New SuperPixel_Basics_CPP
        Dim binarize As Binarize_Basics
        Public Sub New()
            binarize = New Binarize_Basics()
            pixels.gridColor = cv.Scalar.Red
            OptionParent.FindSlider("Number of SuperPixels").Value = 20 ' find the top 20 super pixels.
            desc = "Create SuperPixels from a binary image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            binarize.Run(src)

            pixels.Run(binarize.dst2)
            dst2 = pixels.dst2
            dst3 = pixels.dst3
            dst3.SetTo(cv.Scalar.White, pixels.wireGrid)
        End Sub
    End Class






    Public Class NR_SuperPixel_Depth : Inherits TaskParent
        Dim pixels As New SuperPixel_Basics_CPP
        Public Sub New()
            desc = "Create SuperPixels using RGBDepth image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            pixels.Run(atask.depthRGB)
            dst2 = pixels.dst2
            dst3 = pixels.dst3
        End Sub
    End Class






    Public Class NR_SuperPixel_WithCanny : Inherits TaskParent
        Dim pixels As New SuperPixel_Basics_CPP
        Dim edges As New Edge_Basics
        Public Sub New()
            desc = "Create SuperPixels using RGBDepth image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(src)
            src = atask.color.Clone()
            src.SetTo(white, edges.dst2)
            pixels.Run(src)
            dst2 = pixels.dst2
            dst3 = pixels.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3.SetTo(cv.Scalar.Red, edges.dst2)
            labels(3) = "Edges provided by Canny in red"
        End Sub
    End Class






    Public Class NR_SuperPixel_WithLineDetector : Inherits TaskParent
        Dim pixels As New SuperPixel_Basics_CPP
        Public Sub New()
            labels(3) = "Input to superpixel basics."
            desc = "Create SuperPixels using RGBDepth image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = atask.lines.dst2
            pixels.Run(dst3)
            dst2 = pixels.dst2
            labels(3) = atask.lines.labels(2)
        End Sub
    End Class
End Namespace