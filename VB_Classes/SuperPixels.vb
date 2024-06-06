Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class SuperPixel_Basics : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(2) = "Super Pixel cells"
        desc = "A Better superpixel algorithm"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        dst3 = src
        For Each rc In task.redCells
            drawContour(dst3(rc.rect), rc.contour, cv.Scalar.White, task.lineWidth)
        Next
    End Sub
End Class





Public Class SuperPixel_Basics_CPP : Inherits VB_Parent
    Public wireGrid As cv.Mat
    Public gridColor = cv.Scalar.White
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of SuperPixels", 1, 1000, 400)
            sliders.setupTrackBar("SuperPixel Iterations", 0, 10, 4)
            sliders.setupTrackBar("Prior", 1, 10, 2)
        End If

        labels(3) = "Superpixel label data (0-255)"
        desc = "Sub-divide the image into super pixels."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static countSlider = FindSlider("Number of SuperPixels")
        Static iterSlider = FindSlider("SuperPixel Iterations")
        Static priorSlider = FindSlider("Prior")

        Static numSuperPixels As Integer
        Static numIterations As Integer
        Static prior As Integer
        If numSuperPixels <> countSlider.Value Or numIterations <> iterSlider.Value Or prior <> priorSlider.Value Then
            numSuperPixels = countSlider.Value
            numIterations = iterSlider.Value
            prior = priorSlider.Value
            If cPtr <> 0 Then SuperPixel_Close(cPtr)
            cPtr = SuperPixel_Open(src.Width, src.Height, numSuperPixels, numIterations, prior)
        End If

        Dim input = src
        If input.Channels = 1 Then input = input.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim dataSrc(input.Total * input.ElemSize - 1) As Byte
        Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = SuperPixel_Run(cPtr, handleSrc.AddrOfPinnedObject())
        handleSrc.Free()

        dst2 = input
        dst2.SetTo(gridColor, New cv.Mat(input.Rows, input.Cols, cv.MatType.CV_8UC1, imagePtr))

        Dim labelData(input.Total * 4 - 1) As Byte ' labels are 32-bit integers.
        Dim labelPtr = SuperPixel_GetLabels(cPtr)
        Marshal.Copy(labelPtr, labelData, 0, labelData.Length)
        Dim labels = New cv.Mat(input.Rows, input.Cols, cv.MatType.CV_32S, labelData)
        If numSuperPixels < 255 Then labels *= 255 / numSuperPixels
        labels.ConvertTo(dst3, cv.MatType.CV_8U)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = SuperPixel_Close(cPtr)
    End Sub
End Class






Public Class SuperPixel_BinarizedImage : Inherits VB_Parent
    ReadOnly pixels As New SuperPixel_Basics_CPP
    ReadOnly binarize As Binarize_Basics
    Public Sub New()
        binarize = New Binarize_Basics()
        pixels.gridColor = cv.Scalar.Red
        FindSlider("Number of SuperPixels").Value = 20 ' find the top 20 super pixels.
        desc = "Create SuperPixels from a binary image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        binarize.Run(src)

        pixels.Run(binarize.dst2)
        dst2 = pixels.dst2
        dst3 = pixels.dst3
        dst3.SetTo(cv.Scalar.White, pixels.wireGrid)
    End Sub
End Class






Public Class SuperPixel_Depth : Inherits VB_Parent
    Dim pixels As New SuperPixel_Basics_CPP
    Public Sub New()
        desc = "Create SuperPixels using RGBDepth image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        pixels.Run(task.depthRGB)
        dst2 = pixels.dst2
        dst3 = pixels.dst3
    End Sub
End Class






Public Class SuperPixel_WithCanny : Inherits VB_Parent
    ReadOnly pixels As New SuperPixel_Basics_CPP
    ReadOnly edges As New Edge_Canny
    Public Sub New()
        desc = "Create SuperPixels using RGBDepth image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        edges.Run(src)
        src = task.color.Clone()
        src.SetTo(cv.Scalar.White, edges.dst2)
        pixels.Run(src)
        dst2 = pixels.dst2
        dst3 = pixels.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3.SetTo(cv.Scalar.Red, edges.dst2)
        labels(3) = "Edges provided by Canny in red"
    End Sub
End Class






Public Class SuperPixel_WithLineDetector : Inherits VB_Parent
    ReadOnly pixels As New SuperPixel_Basics_CPP
    ReadOnly lines As New Line_Basics
    Public Sub New()
        labels(3) = "Input to superpixel basics."
        desc = "Create SuperPixels using RGBDepth image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        lines.Run(src)
        dst3 = lines.dst2
        pixels.Run(dst3)
        dst2 = pixels.dst2
    End Sub
End Class
