Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
' https://docs.opencv.org/2.4/modules/core/doc/operations_on_arrays.html
Public Class LUT_Basics : Inherits VB_Algorithm
    Public classCount As Integer
    Dim options As New Options_LUT
    Public Sub New()
        desc = "Divide the image into n-segments controlled with a slider."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Static segment(255) As Byte
        Static myLut As cv.Mat
        If classCount <> options.lutSegments Then
            classCount = options.lutSegments
            Dim incr = Math.Truncate(255 / classCount)
            For i = 0 To classCount - 1
                Dim val = CInt(i * incr)
                For j = 0 To incr - 1
                    segment(val + j) = val
                Next
            Next
            For i = incr * classCount To 255
                segment(i) = 255
            Next
            myLut = New cv.Mat(1, 256, cv.MatType.CV_8U, segment)
        End If
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = src.LUT(myLut) * classCount / 255

        dst3 = vbPalette(dst2 * 255 / classCount)
        labels(2) = "Image segmented into " + CStr(classCount + 1) + " divisions (0-" + CStr(classCount) + ")"
    End Sub
End Class







Public Class LUT_Sliders : Inherits VB_Algorithm
    Dim options As New Options_LUT
    Public Sub New()
        desc = "Use an OpenCV Lookup Table to define 5 regions in a grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim gray = If(src.Channels = 1, src, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        Dim myLut As New cv.Mat(1, 256, cv.MatType.CV_8U)
        Dim splitIndex As Integer
        For i = 0 To 255
            myLut.Set(Of Byte)(0, i, options.vals(splitIndex))
            If i >= options.splits(splitIndex) Then splitIndex += 1
        Next
        dst2 = gray.LUT(myLut)
    End Sub
End Class









' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class LUT_Reduction : Inherits VB_Algorithm
    Public reduction As New Reduction_Basics
    Dim vector = New cv.Mat(256, 1, cv.MatType.CV_8UC3, 0)
    Public Sub New()
        For i = 0 To 255
            vector.Set(Of cv.Vec3b)(i, 0, randomCellColor())
        Next
        labels(3) = "Custom Color Lookup Table"
        desc = "Build and use a custom color palette"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR).LUT(vector)
    End Sub
End Class










Public Class LUT_RGBDepth : Inherits VB_Algorithm
    Dim lut As New LUT_Basics
    Public Sub New()
        desc = "Use a LUT on the RGBDepth to segregate depth data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lut.Run(task.depthRGB.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = lut.dst2 * 255 / lut.classCount
        labels(2) = lut.labels(2)
    End Sub
End Class








Public Class LUT_Depth32f : Inherits VB_Algorithm
    Dim lut As New LUT_Basics
    Public Sub New()
        desc = "Use a LUT on the 32-bit depth to segregate depth data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lut.Run(task.pcSplit(2).Normalize(255).ConvertScaleAbs(255))
        dst2 = lut.dst2 * 255 / lut.classCount
        dst2.SetTo(0, task.noDepthMask)
        labels(2) = lut.labels(2)
    End Sub
End Class









Public Class LUT_Equalized : Inherits VB_Algorithm
    Dim eq As New Hist_EqualizeGray
    Dim lut As New LUT_Basics
    Public Sub New()
        labels(2) = "Without Histogram Equalized"
        labels(3) = "With Histogram Equalized"
        desc = "Use LUT_Basics but with an equalized histogram image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lut.Run(src)
        dst3 = lut.dst2 * 255 / lut.classCount

        eq.dst3.SetTo(0)
        eq.Run(src)
        lut.Run(eq.dst2)
        dst2 = lut.dst2 * 255 / lut.classCount
    End Sub
End Class









Public Class LUT_Watershed : Inherits VB_Algorithm
    Public wShed As New Watershed_Basics
    Public lut As New LUT_Equalized
    Dim edges As New Edge_Canny
    Public Sub New()
        labels(3) = "LUT output with edges highlighted."
        labels(2) = "Watershed Results - draw a rectangle to create a region"
        wShed.UseCorners = True
        desc = "Use watershed algorithm with LUT input to identify regions in the image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lut.Run(src)
        dst3 = lut.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        edges.Run(src)
        dst3.SetTo(cv.Scalar.White, edges.dst2)

        wShed.Run(dst3)
        dst2 = wShed.dst3
    End Sub
End Class








Public Class LUT_Custom : Inherits VB_Algorithm
    Dim gradMap As New Palette_RandomColorMap
    Public colorMap As cv.Mat
    Public Sub New()
        findSlider("Color transitions").Value = 5
        labels(3) = "Custom Color Lookup Table"
        desc = "Use a palette to provide the lookup table for LUT"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static colorSlider = findSlider("Color transitions")
        Static saveColorCount = -1
        If task.optionsChanged Or task.heartBeat Then
            If saveColorCount = 20 Then colorSlider.Value = 5 Else colorSlider.Value += 1
            saveColorCount = colorSlider.Value
            gradMap.Run(src)
            colorMap = gradMap.gradientColorMap.Flip(cv.FlipMode.X)
        End If
        dst2 = src.LUT(colorMap)
        dst3 = colorMap.Resize(src.Size())
    End Sub
End Class






Public Class LUT_RedCloud : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim sort3 As New Sort_3Channel
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Use LUT on the grayscale image after masking with rc.mask"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        Dim rc = task.rc
        src(rc.rect).CopyTo(dst3(rc.rect), rc.mask)

        sort3.Run(dst3)
        dst1 = sort3.dst2
    End Sub
End Class






Public Class LUT_Create : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("LUT entry diff threshold", 1, 100, 10)
        desc = "Create a LUT table that can map similar pixels to the same exact pixel."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static diffSlider = findSlider("LUT entry diff threshold")
        Dim threshold = diffSlider.value

        Dim split = src.Split()
        Static pixels(2)() As Byte
        For i = 0 To 2
            If firstPass Then ReDim pixels(i)(src.Total - 1)
            Marshal.Copy(split(i).Data, pixels(i), 0, pixels(i).Length)
        Next

        Dim totals(255) As Single
        Dim lutI(255) As cv.Vec3i
        For i = 0 To src.Total - 1
            Dim index = CInt(0.299 * pixels(2)(i) + 0.587 * pixels(1)(i) + 0.114 * pixels(0)(i))
            totals(index) += 1
            Dim v1 = lutI(index)
            Dim v2 = New cv.Vec3i(pixels(0)(i), pixels(1)(i), pixels(2)(i))
            lutI(index) = New cv.Vec3i((v1(0) + v2(0)) / 2, (v1(1) + v2(1)) / 2, (v1(2) + v2(2)) / 2)
        Next

        Dim lastVec = lutI(0)
        For i = 1 To lutI.Count - 1
            Dim vec = lutI(i)
            Dim diff = Math.Abs(vec(0) - lastVec(0)) + Math.Abs(vec(1) - lastVec(1)) + Math.Abs(vec(2) - lastVec(2))
            If diff < threshold Then
                lutI(i) = lastVec
            Else
                lastVec = vec
            End If
        Next

        Dim lut(255) As cv.Vec3b
        For i = 0 To lutI.Count - 1
            lut(i) = New cv.Vec3b(lutI(i)(0), lutI(i)(1), lutI(i)(2))
        Next

        dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim myLut As New cv.Mat(1, 256, cv.MatType.CV_8U, lut)
        dst3 = dst2.LUT(myLut)
    End Sub
End Class