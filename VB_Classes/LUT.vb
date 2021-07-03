Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
' https://docs.opencv.org/2.4/modules/core/doc/operations_on_arrays.html
Public Class LUT_Basics : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then sliders.setupTrackBar(0, "Number of LUT Segments", 2, 100, 10)
        task.desc = "Divide the image into n-segments controlled with a slider."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 2
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
        dst2 = src.LUT(myLut)
        labels(2) = "Image segmented into " + CStr(segments + 1) + " divisions (0-" + CStr(segments) + ")"
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
        dst2 = gray.LUT(myLut)
    End Sub
End Class








Public Class LUT_CustomColor : Inherits VBparent
    Public reduction As New Reduction_Basics
    Dim gradMap As New Palette_RandomColorMap
    Public colorMap As cv.Mat
    Public Sub New()
        findSlider("Number of color transitions (Used only with Random)").Value = 10
        labels(3) = "Custom Color Lookup Table"
        task.desc = "Use a palette to provide the lookup table for LUT - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateName = caller Then reduction.RunClass(src)
        gradMap.RunClass(src)
        colorMap = gradMap.gradientColorMap.Flip(cv.FlipMode.X)
        dst2 = src.LUT(colorMap)
        dst3 = colorMap.Resize(src.Size())
    End Sub
End Class








' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class LUT_Reduction : Inherits VBparent
    Public reduction As New Reduction_Basics
    Public Sub New()
        labels(3) = "Custom Color Lookup Table"
        task.desc = "Build and use a custom color palette - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        task.palette.RunClass(Nothing)
        reduction.RunClass(src)
        Dim vector = task.palette.gradientColorMap.Row(0).Clone
        dst2 = reduction.dst3.LUT(vector)
        If standalone Or task.intermediateName = caller Then dst3 = task.palette.gradientColorMap.Resize(src.Size())
    End Sub
End Class










Public Class LUT_RGBDepth : Inherits VBparent
    Dim lut As New LUT_Basics
    Public Sub New()
        task.desc = "Use a LUT on the RGBDepth to segregate depth data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lut.RunClass(task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = lut.dst2
        labels(2) = lut.labels(2)
    End Sub
End Class








Public Class LUT_Depth32f : Inherits VBparent
    Dim lut As New LUT_Basics
    Public Sub New()
        task.desc = "Use a LUT on the 32-bit depth to segregate depth data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        lut.RunClass(task.depth32f.Normalize(255).ConvertScaleAbs(255))
        dst2 = lut.dst2
        dst2.SetTo(0, task.noDepthMask)
        labels(2) = lut.labels(2)
    End Sub
End Class









Public Class LUT_FloodFill : Inherits VBparent
    Dim edges As New Edges_Basics
    Public flood As New FloodFill_Basics
    Public lut As New LUT_Equalized
    Public selectedIndex = 1
    Public Sub New()
        usingdst1 = True
        findSlider("Canny threshold1").Value = 170
        labels(1) = "Click anywhere to see the selected region isolated in dst3"
        labels(2) = "FloodFill Results - click to select another region"
        task.desc = "Use LUT output with floodfill to identify each segment in the image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 4
        edges.RunClass(src)
        src.SetTo(cv.Scalar.White, edges.dst2)

        lut.RunClass(src)
        dst1 = lut.dst2

        flood.RunClass(lut.dst2)
        dst2 = flood.dst2
        dst3 = flood.dst3
        If flood.rects.Count = 0 Then Exit Sub ' image is likely very dark and nothing is actually seen...
        labels(3) = CStr(flood.masks.Count) + " regions.  Selected region = " + CStr(flood.selectedIndex)
    End Sub
End Class








Public Class LUT_Equalized : Inherits VBparent
    Dim eq As New Histogram_EqualizeGray
    Dim lut As New LUT_Basics
    Public Sub New()
        If standalone Then usingdst1 = True
        labels(1) = "Equalized has the same number of pixels"
        labels(2) = "Without Histogram Equalized"
        labels(3) = "With Histogram Equalized"
        task.desc = "Use LUT_Basics but with an equalized histogram image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 2
        Static segSlider = findSlider("Number of LUT Segments")
        If standalone Then
            lut.RunClass(src.Clone)
            dst3 = lut.dst2.Clone
        End If

        eq.RunClass(src)
        lut.RunClass(eq.dst2)
        dst2 = lut.dst2

        If standalone Then
            dst1 = eq.dst3
            Dim lineCount = segSlider.value
            Dim incr = dst1.Width / lineCount
            For i = 0 To lineCount - 1
                Dim p1 = New cv.Point(CInt(i * incr), 0)
                Dim p2 = New cv.Point(CInt(i * incr), dst1.Height)
                dst1.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth)
            Next
        End If
    End Sub
End Class









Public Class LUT_Watershed : Inherits VBparent
    Public wShed As New Watershed_Basics
    Public lut As New LUT_Equalized
    Public selectedIndex = 1
    Dim edges As New Edges_Basics
    Public Sub New()
        usingdst1 = True
        labels(1) = "LUT output - draw a rectangle to create a region"
        labels(2) = "Watershed Results - draw a rectangle to create a region"
        wShed.UseCorners = True
        task.desc = "Use watershed algorithm with LUT input to identify regions in the image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 4
        Static mousePoint = New cv.Point(msRNG.Next(0, dst1.Width), msRNG.Next(0, dst1.Height))

        lut.RunClass(src)
        dst1 = lut.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        edges.RunClass(src)
        dst1.SetTo(cv.Scalar.White, edges.dst2)

        wShed.RunClass(dst1)
        dst2 = wShed.dst3
    End Sub
End Class