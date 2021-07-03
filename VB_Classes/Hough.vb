Imports cv = OpenCvSharp

Module Hough_Exports
    Public Sub houghShowLines(ByRef dst2 As cv.Mat, segments() As cv.LineSegmentPolar, desiredCount As integer)
        For i = 0 To Math.Min(segments.Length, desiredCount) - 1
            Dim rho As Single = segments(i).Rho
            Dim theta As Single = segments(i).Theta

            Dim a As Double = Math.Cos(theta)
            Dim b As Double = Math.Sin(theta)
            Dim x As Double = a * rho
            Dim y As Double = b * rho

            Dim pt1 As cv.Point = New cv.Point(Math.Round(x + 1000 * -b), Math.Round(y + 1000 * a))
            Dim pt2 As cv.Point = New cv.Point(Math.Round(x - 1000 * -b), Math.Round(y - 1000 * a))
            dst2.Line(pt1, pt2, cv.Scalar.Red, task.lineWidth + 1, task.lineType, 0)
        Next
    End Sub

    Public Sub houghShowLines3D(ByRef dst2 As cv.Mat, segment As cv.Line3D)
        Dim x As Double = segment.X1 * dst2.Cols
        Dim y As Double = segment.Y1 * dst2.Rows
        Dim m As Double
        If segment.Vx < 0.001 Then
            m = 0
        Else
            m = segment.Vy / segment.Vx ' vertical slope a no-no.
        End If
        Dim b As Double = y - m * x
        Dim pt1 As cv.Point = New cv.Point(x, y)
        Dim pt2 As cv.Point
        If m = 0 Then pt2 = New cv.Point(x, dst2.Rows) Else pt2 = New cv.Point((dst2.Rows - b) / m, dst2.Rows)
        dst2.Line(pt1, pt2, cv.Scalar.Red, task.lineWidth + 2, task.lineType, 0)
    End Sub
End Module




' https://docs.opencv.org/3.1.0/d6/d10/tutorial_py_houghlines.html
Public Class Hough_Circles : Inherits VBparent
    Dim circles As New Draw_Circles
    Public Sub New()
        findSlider("DrawCount").Value = 3
        labels(2) = "Input circles to Hough"
        labels(3) = "Hough Circles found"
        task.desc = "Find circles using HoughCircles."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        circles.RunClass(src)
        dst2 = circles.dst2
        Static Dim method As Integer = 3
        cv.Cv2.CvtColor(dst2, dst3, cv.ColorConversionCodes.BGR2GRAY)
        Dim cFound = cv.Cv2.HoughCircles(dst3, method, 1, dst2.Rows / 4, 100, 10, 1, 200)
        Dim foundColor = New cv.Scalar(0, 0, 255)
        dst2.CopyTo(dst3)
        For i = 0 To cFound.Length - 1
            dst3.Circle(New cv.Point(CInt(cFound(i).Center.X), CInt(cFound(i).Center.Y)), cFound(i).Radius, foundColor, 5, task.lineType)
        Next
        labels(3) = CStr(cFound.Length) + " circles were identified"
    End Sub
End Class



' https://docs.opencv.org/3.1.0/d6/d10/tutorial_py_houghlines.html
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/HoughLinesSample.vb
Public Class Hough_Lines : Inherits VBparent
    Dim edges As New Edges_Basics
    Public segments() As cv.LineSegmentPolar
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "rho", 1, 100, 1)
            sliders.setupTrackBar(1, "theta", 1, 1000, 1000 * Math.PI / 180)
            sliders.setupTrackBar(2, "threshold", 1, 100, 50)
            sliders.setupTrackBar(3, "Lines to Plot", 1, 1000, 25)
        End If
        task.desc = "Use Houghlines to find lines in the image."
    End Sub

    Public Sub Run(src As cv.Mat) ' Rank = 1
        edges.RunClass(src)

        Dim rhoIn = sliders.trackbar(0).Value
        Dim thetaIn = sliders.trackbar(1).Value / 1000
        Dim threshold = sliders.trackbar(2).Value

        segments = cv.Cv2.HoughLines(edges.dst2, rhoIn, thetaIn, threshold)
        labels(2) = "Found " + CStr(segments.Length) + " Lines"

        If standalone Or task.intermediateName = caller Then
            src.CopyTo(dst2)
            dst2.SetTo(cv.Scalar.White, edges.dst2)
            src.CopyTo(dst3)
            houghShowLines(dst2, segments, sliders.trackbar(3).Value)
            Dim probSegments = cv.Cv2.HoughLinesP(edges.dst2, rhoIn, thetaIn, threshold)
            For i = 0 To Math.Min(probSegments.Length, sliders.trackbar(3).Value) - 1
                Dim line = probSegments(i)
                dst3.Line(line.P1, line.P2, cv.Scalar.Red, task.lineWidth + 2, task.lineType)
            Next
            labels(3) = "Probablistic lines = " + CStr(probSegments.Length)
        End If
    End Sub
End Class





Public Class Hough_Lines_MT : Inherits VBparent
    Dim edges As New Edges_Basics
    Public grid As New Thread_Grid
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "rho", 1, 100, 1)
            sliders.setupTrackBar(1, "theta", 1, 1000, 1000 * Math.PI / 180)
            sliders.setupTrackBar(2, "threshold", 1, 100, 3)
        End If

        findSlider("ThreadGrid Width").Value = 16
        findSlider("ThreadGrid Height").Value = 16

        task.desc = "Multithread Houghlines to find lines in image fragments."
        labels(2) = "Hough_Lines_MT"
        labels(3) = "Hough_Lines_MT"
    End Sub

    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.RunClass(Nothing)

        edges.RunClass(src)
        dst2 = edges.dst2

        Dim rhoIn = sliders.trackbar(0).Value
        Dim thetaIn = sliders.trackbar(1).Value / 1000
        Dim threshold = sliders.trackbar(2).Value

        Parallel.ForEach(grid.roiList,
        Sub(roi)
            Dim segments() = cv.Cv2.HoughLines(dst2(roi), rhoIn, thetaIn, threshold)
            If segments.Count = 0 Then
                dst3(roi) = task.RGBDepth(roi)
                Exit Sub
            End If
            dst3(roi).SetTo(0)
            houghShowLines(dst3(roi), segments, 1)
        End Sub)
        dst2.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class



