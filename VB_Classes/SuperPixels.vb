Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class SuperPixel_Basics : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(2) = "Super Pixel cells"
        desc = "A Better superpixel algorithm"
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        dst3 = src
        For Each rc In task.redCells
            DrawContour(dst3(rc.rect), rc.contour, cvb.Scalar.White, task.lineWidth)
        Next
    End Sub
End Class





Public Class SuperPixel_Basics_CPP_VB : Inherits VB_Parent
    Public wireGrid As cvb.Mat
    Public gridColor = cvb.Scalar.White
    Dim options As New Options_SuperPixels
    Public Sub New()
        labels(3) = "Superpixel label data (0-255)"
        desc = "Sub-divide the image into super pixels."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()

        If task.optionsChanged Then
            If cPtr <> 0 Then SuperPixel_Close(cPtr)
            cPtr = SuperPixel_Open(src.Width, src.Height, options.numSuperPixels, options.numIterations, options.prior)
        End If

        Dim input = src
        If input.Channels() = 1 Then input = input.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        Dim dataSrc(input.Total * input.ElemSize - 1) As Byte
        Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = SuperPixel_Run(cPtr, handleSrc.AddrOfPinnedObject())
        handleSrc.Free()

        dst2 = input
        dst2.SetTo(gridColor, cvb.Mat.FromPixelData(input.Rows, input.Cols, cvb.MatType.CV_8UC1, imagePtr))

        Dim labelData(input.Total * 4 - 1) As Byte ' labels are 32-bit integers.
        Dim labelPtr = SuperPixel_GetLabels(cPtr)
        Marshal.Copy(labelPtr, labelData, 0, labelData.Length)
        Dim labels = cvb.Mat.FromPixelData(input.Rows, input.Cols, cvb.MatType.CV_32S, labelData)
        If options.numSuperPixels < 255 Then labels *= 255 / options.numSuperPixels
        labels.ConvertTo(dst3, cvb.MatType.CV_8U)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = SuperPixel_Close(cPtr)
    End Sub
End Class






Public Class SuperPixel_BinarizedImage : Inherits VB_Parent
    Dim pixels As New SuperPixel_Basics_CPP_VB
    Dim binarize As Binarize_Basics
    Public Sub New()
        binarize = New Binarize_Basics()
        pixels.gridColor = cvb.Scalar.Red
        FindSlider("Number of SuperPixels").Value = 20 ' find the top 20 super pixels.
        desc = "Create SuperPixels from a binary image."
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        binarize.Run(src)

        pixels.Run(binarize.dst2)
        dst2 = pixels.dst2
        dst3 = pixels.dst3
        dst3.SetTo(cvb.Scalar.White, pixels.wireGrid)
    End Sub
End Class






Public Class SuperPixel_Depth : Inherits VB_Parent
    Dim pixels As New SuperPixel_Basics_CPP_VB
    Public Sub New()
        desc = "Create SuperPixels using RGBDepth image."
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        pixels.Run(task.depthRGB)
        dst2 = pixels.dst2
        dst3 = pixels.dst3
    End Sub
End Class






Public Class SuperPixel_WithCanny : Inherits VB_Parent
    Dim pixels As New SuperPixel_Basics_CPP_VB
    Dim edges As New Edge_Canny
    Public Sub New()
        desc = "Create SuperPixels using RGBDepth image."
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        edges.Run(src)
        src = task.color.Clone()
        src.SetTo(cvb.Scalar.White, edges.dst2)
        pixels.Run(src)
        dst2 = pixels.dst2
        dst3 = pixels.dst3.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        dst3.SetTo(cvb.Scalar.Red, edges.dst2)
        labels(3) = "Edges provided by Canny in red"
    End Sub
End Class






Public Class SuperPixel_WithLineDetector : Inherits VB_Parent
    Dim pixels As New SuperPixel_Basics_CPP_VB
    Dim lines As New Line_Basics
    Public Sub New()
        labels(3) = "Input to superpixel basics."
        desc = "Create SuperPixels using RGBDepth image."
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        lines.Run(src)
        dst3 = lines.dst2
        pixels.Run(dst3)
        dst2 = pixels.dst2
    End Sub
End Class
