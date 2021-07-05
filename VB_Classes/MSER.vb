Imports cv = OpenCvSharp
'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_Basics : Inherits VBparent
    Dim sortedBoxes As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
    Public containers As New List(Of cv.Rect)
    Dim options As New MSER_Options
    Dim maxSlider As Windows.Forms.TrackBar
    Public Sub New()
        maxSlider = findSlider("MSER Max Area")
        maxSlider.Value = If(dst2.Width = 1280, 50000, 20000)
        task.desc = "Run MSER (Maximally Stable Extremal Region) algorithm with all default options except for maximum area"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static minSlider = findSlider("MSER Min Area")
        options.RunClass(src)

        dst2 = src.Clone
        dst3 = src.Clone

        sortedBoxes.Clear()
        For Each box In options.boxes
            sortedBoxes.Add(box.Width * box.Height, box)
        Next

        Dim maxArea = maxSlider.Value
        Dim boxList As New List(Of cv.Rect)
        For i = 0 To sortedBoxes.Count - 1
            If sortedBoxes.ElementAt(i).Key < maxArea Then boxList.Add(sortedBoxes.ElementAt(i).Value)
        Next

        containers.Clear()
        While boxList.Count > 0
            Dim box = boxList(0)
            containers.Add(box)
            Dim removeBoxes As New List(Of Integer)
            removeBoxes.Add(0)
            For i = 1 To boxList.Count - 1
                Dim b = boxList(i)
                Dim center = New cv.Point(CInt(b.X + b.Width / 2), CInt(b.Y + b.Height / 2))
                If center.X >= box.X And center.X <= (box.X + box.Width) Then
                    If center.Y >= box.Y And center.Y <= (box.Y + box.Height) Then
                        removeBoxes.Add(i)
                        dst3.Rectangle(b, cv.Scalar.Yellow, 1)
                    End If
                End If
            Next

            For i = removeBoxes.Count - 1 To 0 Step -1
                boxList.RemoveAt(removeBoxes.ElementAt(i))
            Next
        End While

        Dim minArea = minSlider.value
        For Each rect In containers
            If rect.Width * rect.Height > minArea Then dst2.Rectangle(rect, cv.Scalar.Yellow, If(src.Width = 1280, 2, 1))
        Next

        labels(2) = CStr(containers.Count) + " consolidated regions of interest located"
        labels(3) = CStr(sortedBoxes.Count) + " total rectangles found with MSER"
    End Sub
End Class









'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_Options : Inherits VBparent
    Public boxes() As cv.Rect = Nothing
    Public regions()() As cv.Point = Nothing
    Dim saveParms() As Integer
    Public mser = cv.MSER.Create
    Public Sub New()
        If sliders.Setup(caller, 9) Then
            sliders.setupTrackBar(0, "MSER Delta", 1, 100, 9)
            sliders.setupTrackBar(1, "MSER Min Area", 1, 10000, 2500)
            sliders.setupTrackBar(2, "MSER Max Area", 1000, 100000, 100000)
            sliders.setupTrackBar(3, "MSER Max Variation", 1, 100, 25)
            sliders.setupTrackBar(4, "Min Diversity", 0, 100, 20)
            sliders.setupTrackBar(5, "MSER Max Evolution", 1, 1000, 200)
            sliders.setupTrackBar(6, "MSER Area Threshold", 1, 101, 101)
            sliders.setupTrackBar(7, "MSER Min Margin", 1, 100, 3)
            sliders.setupTrackBar(8, "MSER Edge BlurSize", 1, 20, 5)
        End If

        If check.Setup(caller, 3) Then
            check.Box(0).Text = "Pass2Only"
            check.Box(1).Text = "Use Grayscale, not color input (default)"
            check.Box(2).Text = "Use all default options - ignore all but min and max area"
            check.Box(2).Checked = True
        End If
        ReDim saveParms(sliders.trackbar.Count + check.Box.Count - 1)
        task.desc = "Extract the Maximally Stable Extremal Region (MSER) for an image using all the available options."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim delta = sliders.trackbar(0).Value
        Dim minArea = sliders.trackbar(1).Value
        Dim maxArea = sliders.trackbar(2).Value
        Dim maxVariation = sliders.trackbar(3).Value / 100

        Dim minDiversity = sliders.trackbar(4).Value / 100
        Dim maxEvolution = sliders.trackbar(5).Value
        Dim areaThreshold = sliders.trackbar(6).Value / 100
        Dim minMargin = sliders.trackbar(7).Value / 1000

        Dim edgeBlurSize = sliders.trackbar(8).Value
        If edgeBlurSize Mod 2 = 0 Then edgeBlurSize += 1 ' must be odd.

        Dim changedParms As Boolean
        For i = 0 To saveParms.Length - 1
            Dim nextVal = Choose(i + 1, sliders.trackbar(0).Value, sliders.trackbar(1).Value, sliders.trackbar(2).Value, sliders.trackbar(3).Value,
                                        sliders.trackbar(4).Value, sliders.trackbar(5).Value, sliders.trackbar(6).Value, sliders.trackbar(7).Value,
                                        sliders.trackbar(8).Value, check.Box(0).Checked, check.Box(1).Checked, check.Box(2).Checked)
            If nextVal <> saveParms(i) Then changedParms = True
            saveParms(i) = nextVal
        Next

        If changedParms And check.Box(2).Checked = False Then
            mser = cv.MSER.Create(delta, minArea, maxArea, maxVariation, minDiversity, maxEvolution, areaThreshold, minMargin, edgeBlurSize)
            mser.Pass2Only = check.Box(0).Checked
        End If

        Dim input = src
        If check.Box(1).Checked Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        mser.DetectRegions(input, regions, boxes)

        If standalone Or task.intermediateActive Then
            dst2 = src.Clone
            For Each z In boxes
                If z.Size.Width * z.Size.Height > minArea Then dst2.Rectangle(z, cv.Scalar.Yellow, 1)
            Next
        End If
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_SyntheticInput : Inherits VBparent
    Private Sub addNestedRectangles(img As cv.Mat, p0 As cv.Point, width() As Integer, color() As Integer, n As Integer)
        For i = 0 To n - 1
            img.Rectangle(New cv.Rect(p0.X, p0.Y, width(i), width(i)), color(i), 1)
            p0 += New cv.Point((width(i) - width(i + 1)) / 2, (width(i) - width(i + 1)) / 2)
            img.FloodFill(p0, color(i))
        Next
    End Sub
    Private Sub addNestedCircles(img As cv.Mat, p0 As cv.Point, width() As Integer, color() As Integer, n As Integer)
        For i = 0 To n - 1
            img.Circle(p0, width(i) / 2, color(i), task.lineWidth, task.lineType)
            img.FloodFill(p0, color(i))
        Next
    End Sub
    Public Sub New()
        task.desc = "Build a synthetic image for MSER (Maximal Stable Extremal Regions) testing"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim img = New cv.Mat(800, 800, cv.MatType.CV_8U, 0)
        Dim width() = {390, 380, 300, 290, 280, 270, 260, 250, 210, 190, 150, 100, 80, 70}
        Dim color1() = {80, 180, 160, 140, 120, 100, 90, 110, 170, 150, 140, 100, 220}
        Dim color2() = {81, 181, 161, 141, 121, 101, 91, 111, 171, 151, 141, 101, 221}
        Dim color3() = {175, 75, 95, 115, 135, 155, 165, 145, 85, 105, 115, 155, 35}
        Dim color4() = {173, 73, 93, 113, 133, 153, 163, 143, 83, 103, 113, 153, 33}

        addNestedRectangles(img, New cv.Point(10, 10), width, color1, 13)
        addNestedCircles(img, New cv.Point(200, 600), width, color2, 13)

        addNestedRectangles(img, New cv.Point(410, 10), width, color3, 13)
        addNestedCircles(img, New cv.Point(600, 600), width, color4, 13)

        img = img.Resize(New cv.Size(src.Rows, src.Rows))
        dst2(New cv.Rect(0, 0, src.Rows, src.Rows)) = img.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_TestSynthetic : Inherits VBparent
    Dim mser As New MSER_Options
    Dim synth As New MSER_SyntheticInput
    Private Function testSynthetic(img As cv.Mat, pass2Only As Boolean, delta As Integer) As String
        mser.check.Box(0).Checked = pass2Only
        mser.sliders.trackbar(0).Value = delta
        mser.RunClass(img)

        Dim pixels As Integer
        Dim regionCount As Integer
        For i = 0 To mser.regions.Length - 1
            regionCount += 1
            Dim nextRegion = mser.regions(i)
            For Each pt In nextRegion
                img.Set(Of cv.Vec3b)(pt.Y, pt.X, task.vecColors(i Mod task.vecColors.Length))
                pixels += 1
            Next
        Next
        Return CStr(regionCount) + " Regions had " + CStr(pixels) + " pixels"
    End Function
    Public Sub New()
        mser.sliders.trackbar(0).Value = 10
        mser.sliders.trackbar(1).Value = 100
        mser.sliders.trackbar(2).Value = 5000
        mser.sliders.trackbar(3).Value = 2
        mser.sliders.trackbar(4).Value = 0
        mser.check.Box(1).Checked = False ' the grayscale result is quite unimpressive.

        labels(2) = "Input image to MSER"
        labels(2) = "Output image from MSER"
        task.desc = "Test MSER with the synthetic image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        synth.RunClass(src)
        dst2 = synth.dst2.Clone()
        dst3 = synth.dst2

        testSynthetic(dst3, True, 100)
    End Sub
End Class




' https://github.com/shimat/opencvsharp/wiki/MSER
Public Class MSER_CPPStyle : Inherits VBparent
    Dim gray As cv.Mat
    Dim image As cv.Mat
    Public Sub New()
        labels(2) = "Contour regions from MSER"
        labels(3) = "Box regions from MSER"
        task.desc = "Maximally Stable Extremal Regions example - still image"
        image = cv.Cv2.ImRead(task.parms.homeDir + "Data/MSERtestfile.jpg", cv.ImreadModes.Color)
        gray = image.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim mser = cv.MSER.Create()
        Dim regions()() As cv.Point = Nothing
        Dim boxes() As cv.Rect = Nothing
        mser.DetectRegions(image, regions, boxes)
        Dim mat = image.Clone()
        For Each pts In regions
            Dim color = cv.Scalar.RandomColor
            For Each pt In pts
                mat.Circle(pt, task.dotSize, color, -1, task.lineType)
            Next
        Next
        dst2 = mat.Resize(dst2.Size())

        mat = image.Clone()
        For Each box In boxes
            Dim color = cv.Scalar.RandomColor
            mat.Rectangle(box, color, -1, task.lineType)
        Next
        dst3 = mat.Resize(dst3.Size())
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/python/mser.py
Public Class MSER_Contours : Inherits VBparent
    Dim mser As New MSER_Options
    Public Sub New()
        mser.sliders.trackbar(1).Value = 4000
        task.desc = "Use MSER but show the contours of each region."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        mser.RunClass(src)

        Dim pixels As integer
        dst2 = src
        Dim hull() As cv.Point
        For i = 0 To mser.regions.Length - 1
            Dim nextRegion = mser.regions(i)
            pixels += nextRegion.Length
            hull = cv.Cv2.ConvexHull(nextRegion, True)
            Dim listOfPoints = New List(Of List(Of cv.Point))
            Dim points = New List(Of cv.Point)
            For j = 0 To hull.Count - 1
                points.Add(hull(j))
            Next
            listOfPoints.Add(points)
            dst2.DrawContours(listOfPoints, 0, cv.Scalar.Yellow, 1)
        Next

        labels(2) = CStr(mser.regions.Length) + " Regions " + Format(pixels / mser.regions.Length, "#0.0") + " pixels/region (avg)"
    End Sub
End Class


