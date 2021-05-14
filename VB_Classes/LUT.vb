Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
' https://docs.opencv.org/2.4/modules/core/doc/operations_on_arrays.html
Public Class LUT_Basics : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Number of LUT Segments", 2, 100, 10)
        End If

        task.desc = "Divide the image into n-segments controlled with a slider."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static segment() As Integer
        Static nSegSlider = findSlider("Number of LUT Segments")
        Dim segments = nSegSlider.value
        Static myLut As New cv.Mat(1, 256, cv.MatType.CV_8U)
        Static nSeg As Integer = -1
        If segments <> nSeg Then
            nSeg = segments
            Dim incr = 255 / nSeg
            ReDim segment(nSeg)
            For i = 1 To nSeg - 1
                segment(i) = i * incr
            Next
            segment(segment.Count - 1) = 255
            Dim splitIndex As Integer
            For i = 0 To 255
                myLut.Set(Of Byte)(0, i, segment(splitIndex))
                If i >= segment(splitIndex) Then splitIndex += 1
            Next
        End If
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src.LUT(myLut)
        label1 = "Image segmented into " + CStr(segments + 1) + " divisions (0-" + CStr(segments) + ")"
    End Sub
End Class








Public Class LUT_Sliders : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "LUT zero through xxx", 1, 255, 65)
            sliders.setupTrackBar(1, "LUT xxx through yyy", 1, 255, 110)
            sliders.setupTrackBar(2, "LUT xxx through yyy", 1, 255, 160)
            sliders.setupTrackBar(3, "LUT xxx through 255", 1, 255, 210)
        End If

        task.desc = "Use an OpenCV Lookup Table to define 5 regions in a grayscale image - Painterly Effect."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        sliders.sLabels(0).Text = "LUT zero through " + CStr(sliders.trackbar(0).Value)
        sliders.sLabels(1).Text = "LUT " + CStr(sliders.trackbar(0).Value) + " through " + CStr(sliders.trackbar(1).Value)
        sliders.sLabels(2).Text = "LUT " + CStr(sliders.trackbar(1).Value) + " through " + CStr(sliders.trackbar(2).Value)
        sliders.sLabels(3).Text = "LUT " + CStr(sliders.trackbar(2).Value) + " through 255"
        Dim splits = {sliders.trackbar(0).Value, sliders.trackbar(1).Value, sliders.trackbar(2).Value, sliders.trackbar(3).Value, 255}
        Dim vals = {1, sliders.trackbar(0).Value, sliders.trackbar(1).Value, sliders.trackbar(2).Value, 255}
        Dim gray = If(src.Channels = 1, src, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        Dim myLut As New cv.Mat(1, 256, cv.MatType.CV_8U)
        Dim splitIndex As Integer
        For i = 0 To 255
            myLut.Set(Of Byte)(0, i, vals(splitIndex))
            If i >= splits(splitIndex) Then splitIndex += 1
        Next
        dst1 = gray.LUT(myLut)
    End Sub
End Class








Public Class LUT_CustomColor : Inherits VBparent
    Public reduction As New Reduction_Basics
    Dim gradMap As New Palette_RandomColorMap
    Public colorMap As cv.Mat
    Public Sub New()
        findSlider("Number of color transitions (Used only with Random)").Value = 10
        label2 = "Custom Color Lookup Table"
        task.desc = "Use a palette to provide the lookup table for LUT - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateName = caller Then reduction.Run(src)
        gradMap.Run(src)
        colorMap = gradMap.gradientColorMap.Flip(cv.FlipMode.X)
        dst1 = src.LUT(colorMap)
        dst2 = colorMap.Resize(src.Size())
    End Sub
End Class








' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class LUT_Reduction : Inherits VBparent
    Public reduction As New Reduction_Basics
    Public Sub New()
        label2 = "Custom Color Lookup Table"
        task.desc = "Build and use a custom color palette - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        task.palette.Run(Nothing)
        reduction.Run(src)
        Dim vector = task.palette.gradientColorMap.Row(0).Clone
        dst1 = reduction.dst2.LUT(vector)
        If standalone Or task.intermediateName = caller Then dst2 = task.palette.gradientColorMap.Resize(src.Size())
    End Sub
End Class










Public Class LUT_RGBDepth : Inherits VBparent
    Dim lut As New LUT_Basics
    Public Sub New()
        task.desc = "Use a LUT on the RGBDepth to segregate depth data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lut.Run(task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst1 = lut.dst1
        label1 = lut.label1
    End Sub
End Class








Public Class LUT_Depth32f : Inherits VBparent
    Dim lut As New LUT_Basics
    Public Sub New()
        task.desc = "Use a LUT on the 32-bit depth to segregate depth data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lut.Run(task.depth32f.Normalize(255).ConvertScaleAbs(255))
        dst1 = lut.dst1
        dst1.SetTo(0, task.noDepthMask)
        label1 = lut.label1
    End Sub
End Class








' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class LUT_Color : Inherits VBparent
    Public Sub New()
        task.desc = "Apply the current LUT to the input image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        task.palette.Run(Nothing)

    End Sub
End Class
