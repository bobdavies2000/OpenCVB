Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module SuperPixel_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixel_Open(width As Integer, height As Integer, num_superpixels As Integer, num_levels As Integer, prior As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub SuperPixel_Close(spPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixel_GetLabels(spPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixel_Run(spPtr As IntPtr, rgbPtr As IntPtr) As IntPtr
    End Function
End Module





Public Class SuperPixel_Basics_CPP : Inherits VBparent
    Dim spPtr As IntPtr = 0
    Public wireGrid As cv.Mat
    Public gridColor = cv.Scalar.White
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Number of SuperPixels", 1, 1000, 400)
            sliders.setupTrackBar(1, "Iterations", 0, 10, 4)
            sliders.setupTrackBar(2, "Prior", 1, 10, 2)
        End If

        labels(3) = "Superpixel label data (0-255)"
        task.desc = "Sub-divide the image into super pixels."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static numSuperPixels As integer
        Static numIterations As integer
        Static prior As integer
        If numSuperPixels <> sliders.trackbar(0).Value Or numIterations <> sliders.trackbar(1).Value Or prior <> sliders.trackbar(2).Value Then
            numSuperPixels = sliders.trackbar(0).Value
            numIterations = sliders.trackbar(1).Value
            prior = sliders.trackbar(2).Value
            If spPtr <> 0 Then SuperPixel_Close(spPtr)
            spPtr = SuperPixel_Open(src.Width, src.Height, numSuperPixels, numIterations, prior)
        End If

        Dim input = src
        If input.Channels = 1 Then input = input.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim srcData(input.Total * input.ElemSize - 1) As Byte
        Marshal.Copy(input.Data, srcData, 0, srcData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = SuperPixel_Run(spPtr, handleSrc.AddrOfPinnedObject())
        handleSrc.Free()

        If imagePtr <> 0 Then
            dst2 = input
            wireGrid = New cv.Mat(input.Rows, input.Cols, cv.MatType.CV_8UC1, imagePtr)
            dst2.SetTo(gridColor, wireGrid)
        End If

        Dim labelData(input.Total * 4 - 1) As Byte ' labels are 32-bit integers.
        Dim labelPtr = SuperPixel_GetLabels(spPtr)
        Marshal.Copy(labelPtr, labelData, 0, labelData.Length)
        Dim labels = New cv.Mat(input.Rows, input.Cols, cv.MatType.CV_32S, labelData)
        If numSuperPixels < 255 Then labels *= 255 / numSuperPixels
        labels.ConvertTo(dst3, cv.MatType.CV_8U)
    End Sub
    Public Sub Close()
        SuperPixel_Close(spPtr)
    End Sub
End Class






Public Class SuperPixel_BinarizedImage : Inherits VBparent
    Dim pixels As New SuperPixel_Basics_CPP
    Dim binarize As Binarize_Basics
    Public Sub New()
        binarize = New Binarize_Basics()
        pixels.gridColor = cv.Scalar.Red
        findSlider("Number of SuperPixels").Value = 20 ' find the top 20 super pixels.
        task.desc = "Create SuperPixels from a binary image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        binarize.RunClass(src)

        pixels.RunClass(binarize.dst2)
        dst2 = pixels.dst2
        dst3 = pixels.dst3
        dst3.SetTo(cv.Scalar.White, pixels.wireGrid)
    End Sub
End Class






Public Class SuperPixel_Depth : Inherits VBparent
    Dim pixels As New SuperPixel_Basics_CPP
    Public Sub New()
        task.desc = "Create SuperPixels using RGBDepth image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        pixels.RunClass(task.RGBDepth)
        dst2 = pixels.dst2
        dst3 = pixels.dst3
    End Sub
End Class






Public Class SuperPixel_WithCanny : Inherits VBparent
    Dim pixels As New SuperPixel_Basics_CPP
    Dim edges As New Edges_Basics
    Public Sub New()
        task.desc = "Create SuperPixels using RGBDepth image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        edges.RunClass(src)
        src = task.color.Clone()
        src.SetTo(cv.Scalar.White, edges.dst2)
        pixels.RunClass(src)
        dst2 = pixels.dst2
        dst3 = pixels.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3.SetTo(cv.Scalar.Red, edges.dst2)
        labels(3) = "Edges provided by Canny in red"
    End Sub
End Class






Public Class SuperPixel_WithLineDetector : Inherits VBparent
    Dim pixels As New SuperPixel_Basics_CPP
    Dim lines As New Line_Basics
    Public Sub New()
        labels(3) = "Input to superpixel basics."
        task.desc = "Create SuperPixels using RGBDepth image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lines.RunClass(src)
        dst3 = lines.dst2
        pixels.RunClass(dst3)
        dst2 = pixels.dst2
    End Sub
End Class