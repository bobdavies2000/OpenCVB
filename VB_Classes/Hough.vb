Imports cv = OpenCvSharp

Module Hough_Exports
    Public Sub houghShowLines(ByRef dst1 As cv.Mat, segments() As cv.LineSegmentPolar, desiredCount As integer)
        For i = 0 To Math.Min(segments.Length, desiredCount) - 1
            Dim rho As Single = segments(i).Rho
            Dim theta As Single = segments(i).Theta

            Dim a As Double = Math.Cos(theta)
            Dim b As Double = Math.Sin(theta)
            Dim x As Double = a * rho
            Dim y As Double = b * rho

            Dim pt1 As cv.Point = New cv.Point(Math.Round(x + 1000 * -b), Math.Round(y + 1000 * a))
            Dim pt2 As cv.Point = New cv.Point(Math.Round(x - 1000 * -b), Math.Round(y - 1000 * a))
            dst1.Line(pt1, pt2, cv.Scalar.Red, 2, task.lineType, 0)
        Next
    End Sub

    Public Sub houghShowLines3D(ByRef dst1 As cv.Mat, segment As cv.Line3D)
        Dim x As Double = segment.X1 * dst1.Cols
        Dim y As Double = segment.Y1 * dst1.Rows
        Dim m As Double
        If segment.Vx < 0.001 Then
            m = 0
        Else
            m = segment.Vy / segment.Vx ' vertical slope a no-no.
        End If
        Dim b As Double = y - m * x
        Dim pt1 As cv.Point = New cv.Point(x, y)
        Dim pt2 As cv.Point
        If m = 0 Then pt2 = New cv.Point(x, dst1.Rows) Else pt2 = New cv.Point((dst1.Rows - b) / m, dst1.Rows)
        dst1.Line(pt1, pt2, cv.Scalar.Red, 3, task.lineType, 0)
    End Sub
End Module




' https://docs.opencv.org/3.1.0/d6/d10/tutorial_py_houghlines.html
Public Class Hough_Circles
    Inherits VBparent
    Dim circles As Draw_Circles
    Public Sub New()
        initParent()
        circles = New Draw_Circles()
        circles.sliders.trackbar(0).Value = 3
        task.desc = "Find circles using HoughCircles."
		' task.rank = 1
        label1 = "Input circles to Hough"
        label2 = "Hough Circles found"
    End Sub
    Public Sub Run(src as cv.Mat)
		If task.intermediateReview = caller Then task.intermediateObject = Me
        circles.Run(src)
        dst1 = circles.dst1
        Static Dim method As integer = 3
        cv.Cv2.CvtColor(dst1, dst2, cv.ColorConversionCodes.BGR2GRAY)
        Dim cFound = cv.Cv2.HoughCircles(dst2, method, 1, dst1.Rows / 4, 100, 10, 1, 200)
        Dim foundColor = New cv.Scalar(0, 0, 255)
        dst1.CopyTo(dst2)
        For i = 0 To cFound.Length - 1
            cv.Cv2.Circle(dst2, New cv.Point(CInt(cFound(i).Center.X), CInt(cFound(i).Center.Y)), cFound(i).Radius, foundColor, 5, task.lineType)
        Next
        label2 = CStr(cFound.Length) + " circles were identified"
    End Sub
End Class



' https://docs.opencv.org/3.1.0/d6/d10/tutorial_py_houghlines.html
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/HoughLinesSample.vb
Public Class Hough_Lines
    Inherits VBparent
    Dim edges As Edges_Basics
    Public segments() As cv.LineSegmentPolar
    Public Sub New()
        initParent()
        edges = New Edges_Basics()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "rho", 1, 100, 1)
            sliders.setupTrackBar(1, "theta", 1, 1000, 1000 * Math.PI / 180)
            sliders.setupTrackBar(2, "threshold", 1, 100, 50)
            sliders.setupTrackBar(3, "Lines to Plot", 1, 1000, 25)
        End If
        task.desc = "Use Houghlines to find lines in the image."
		' task.rank = 1
    End Sub

    Public Sub Run(src as cv.Mat)
		If task.intermediateReview = caller Then task.intermediateObject = Me
        edges.Run(src)

        Dim rhoIn = sliders.trackbar(0).Value
        Dim thetaIn = sliders.trackbar(1).Value / 1000
        Dim threshold = sliders.trackbar(2).Value

        segments = cv.Cv2.HoughLines(edges.dst1, rhoIn, thetaIn, threshold)
        label1 = "Found " + CStr(segments.Length) + " Lines"

        If standalone or task.intermediateReview = caller Then
            src.CopyTo(dst1)
            dst1.SetTo(cv.Scalar.White, edges.dst1)
            src.CopyTo(dst2)
            houghShowLines(dst1, segments, sliders.trackbar(3).Value)
            Dim probSegments = cv.Cv2.HoughLinesP(edges.dst1, rhoIn, thetaIn, threshold)
            For i = 0 To Math.Min(probSegments.Length, sliders.trackbar(3).Value) - 1
                Dim line = probSegments(i)
                dst2.Line(line.P1, line.P2, cv.Scalar.Red, 3, task.lineType)
            Next
            label2 = "Probablistic lines = " + CStr(probSegments.Length)
        End If
    End Sub
End Class





Public Class Hough_Lines_MT
    Inherits VBparent
    Dim edges As Edges_Basics
    Public grid As Thread_Grid
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "rho", 1, 100, 1)
            sliders.setupTrackBar(1, "theta", 1, 1000, 1000 * Math.PI / 180)
            sliders.setupTrackBar(2, "threshold", 1, 100, 3)
        End If
        edges = New Edges_Basics()

        grid = New Thread_Grid

        task.desc = "Multithread Houghlines to find lines in image fragments."
		' task.rank = 1
        label1 = "Hough_Lines_MT"
        label2 = "Hough_Lines_MT"
    End Sub

    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me

        If task.frameCount = 0 Then
            Static gridWidthSlider = findSlider("ThreadGrid Width")
            Static gridHeightSlider = findSlider("ThreadGrid Height")
            gridWidthSlider.Value = 16
            gridHeightSlider.Value = 16
        End If
        grid.run(src)

        edges.Run(src)
        dst1 = edges.dst1

        Dim rhoIn = sliders.trackbar(0).Value
        Dim thetaIn = sliders.trackbar(1).Value / 1000
        Dim threshold = sliders.trackbar(2).Value

        Parallel.ForEach(grid.roiList,
        Sub(roi)
            Dim segments() = cv.Cv2.HoughLines(dst1(roi), rhoIn, thetaIn, threshold)
            If segments.Count = 0 Then
                dst2(roi) = task.RGBDepth(roi)
                Exit Sub
            End If
            dst2(roi).SetTo(0)
            houghShowLines(dst2(roi), segments, 1)
        End Sub)
        dst1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class



