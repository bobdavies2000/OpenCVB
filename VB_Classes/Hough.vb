Imports cv = OpenCvSharp
' https://docs.opencv.org/3.1.0/d6/d10/tutorial_py_houghlines.html
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/HoughLinesSample.vb
Public Class Hough_Basics : Inherits VB_Parent
    Dim edges As New Edge_Canny
    Public segments() As cv.LineSegmentPolar
    Public options As New Options_Hough
    Public Sub New()
        desc = "Use Houghlines to find lines in the image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        edges.Run(src)

        segments = cv.Cv2.HoughLines(edges.dst2, options.rho, options.theta, options.threshold)
        labels(2) = "Found " + CStr(segments.Length) + " Lines"

        If standaloneTest() Then
            src.CopyTo(dst2)
            dst2.SetTo(cv.Scalar.White, edges.dst2)
            src.CopyTo(dst3)
            houghShowLines(dst2, segments, options.lineCount)
            Dim probSegments = cv.Cv2.HoughLinesP(edges.dst2, options.rho, options.theta, options.threshold)
            For i = 0 To Math.Min(probSegments.Length, options.lineCount) - 1
                Dim line = probSegments(i)
                dst3.Line(line.P1, line.P2, cv.Scalar.Red, task.lineWidth + 2, task.lineType)
            Next
            labels(3) = "Probablistic lines = " + CStr(probSegments.Length)
        End If
    End Sub
End Class







Module Hough_Exports
    Public Sub houghShowLines(ByRef dst As cv.Mat, segments() As cv.LineSegmentPolar, desiredCount As Integer)
        For i = 0 To Math.Min(segments.Length, desiredCount) - 1
            Dim rho As Single = segments(i).Rho
            Dim theta As Single = segments(i).Theta

            Dim a As Double = Math.Cos(theta)
            Dim b As Double = Math.Sin(theta)
            Dim x As Double = a * rho
            Dim y As Double = b * rho

            Dim pt1 As cv.Point = New cv.Point(x + 1000 * -b, y + 1000 * a)
            Dim pt2 As cv.Point = New cv.Point(x - 1000 * -b, y - 1000 * a)
            dst.Line(pt1, pt2, cv.Scalar.Red, task.lineWidth + 1, task.lineType, 0)
        Next
    End Sub

    Public Sub houghShowLines3D(ByRef dst As cv.Mat, segment As cv.Line3D)
        Dim x As Double = segment.X1 * dst.Cols
        Dim y As Double = segment.Y1 * dst.Rows
        Dim m As Double
        If segment.Vx < 0.001 Then m = 0 Else m = segment.Vy / segment.Vx ' vertical slope a no-no.
        Dim b As Double = y - m * x
        Dim pt1 As cv.Point = New cv.Point(x, y)
        Dim pt2 As cv.Point
        If m = 0 Then pt2 = New cv.Point(x, dst.Rows) Else pt2 = New cv.Point((dst.Rows - b) / m, dst.Rows)
        dst.Line(pt1, pt2, cv.Scalar.Red, task.lineWidth + 2, task.lineType, 0)
    End Sub
End Module







' https://docs.opencv.org/3.1.0/d6/d10/tutorial_py_houghlines.html
Public Class Hough_Circles : Inherits VB_Parent
    Dim circles As New Draw_Circles
    Dim method As Integer = 3
    Public Sub New()
        FindSlider("DrawCount").Value = 3
        labels(2) = "Input circles to Hough"
        labels(3) = "Hough Circles found"
        desc = "Find circles using HoughCircles."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        circles.Run(src)
        dst2 = circles.dst2
        cv.Cv2.CvtColor(dst2, dst3, cv.ColorConversionCodes.BGR2GRAY)
        Dim cFound = cv.Cv2.HoughCircles(dst3, method, 1, dst2.Rows / 4, 100, 10, 1, 200)
        Dim foundColor = New cv.Scalar(0, 0, 255)
        dst2.CopyTo(dst3)
        For i = 0 To cFound.Length - 1
            Dim pt = New cv.Point(CInt(cFound(i).Center.X), CInt(cFound(i).Center.Y))
            dst3.Circle(pt, cFound(i).Radius, foundColor, 5, task.lineType)
        Next
        labels(3) = CStr(cFound.Length) + " circles were identified"
    End Sub
End Class









Public Class Hough_Lines_MT : Inherits VB_Parent
    Dim edges As New Edge_Canny
    Dim options As New Options_Hough
    Public Sub New()
        labels(2) = "Output of the Canny Edge algorithm (no Hough lines)"
        labels(3) = "Hough Lines for each threaded cell or if no lines, the featureless cell depth data."
        desc = "Multithread Houghlines to find lines in image fragments."
    End Sub

    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        edges.Run(src)
        dst2 = edges.dst2

        Dim depth8uC3 = task.depthRGB
        Parallel.ForEach(task.gridList,
        Sub(roi)
            Dim segments() = cv.Cv2.HoughLines(dst2(roi), options.rho, options.theta, options.threshold)
            If segments.Count = 0 Then
                dst3(roi) = depth8uC3(roi)
                Exit Sub
            End If
            dst3(roi).SetTo(0)
            houghShowLines(dst3(roi), segments, 1)
        End Sub)
        dst2.SetTo(cv.Scalar.White, task.gridMask)
    End Sub
End Class













Public Class Hough_Featureless : Inherits VB_Parent
    Public edges As New Edge_Canny
    Public noDepthCount() As Integer
    Public options As New Options_Hough
    Public roiColor() As cv.Vec3b
    Public Sub New()
        task.gOptions.setGridSize(10)
        labels(2) = "Featureless mask"
        desc = "Multithread Houghlines to find featureless regions in an image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        edges.Run(src)

        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
        Dim regionCount As Integer
        ReDim noDepthCount(task.gridList.Count - 1)
        ReDim roiColor(task.gridList.Count - 1)

        For Each roi In task.gridList
            Dim segments() = cv.Cv2.HoughLines(edges.dst2(roi), options.rho, options.theta, options.threshold)
            If edges.dst2(roi).CountNonZero = 0 Then
                regionCount += 1
                dst2(roi).SetTo(255)
            End If
        Next

        dst3.SetTo(0)
        src.CopyTo(dst3, dst2)
        labels(2) = "FeatureLess Regions = " + CStr(regionCount)
        labels(3) = "Of the " + CStr(task.gridList.Count) + " grid elements, " + CStr(regionCount) + " had no edge or hough features present"
    End Sub
End Class








Public Class Hough_FeatureLessTopX : Inherits VB_Parent
    Public edges As New Edge_Canny
    Public options As New Options_Hough
    Public maskFless As cv.Mat
    Public maskFeat As cv.Mat
    Public maskPredict As cv.Mat
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        task.gOptions.setGridSize(10)
        maskFless = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        maskFeat = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        maskPredict = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        labels = {"", "", "Areas without features", "Areas with features"}
        desc = "Multithread Houghlines to find featureless regions in an image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Static segSlider = FindSlider("Minimum feature pixels")
        Dim minSegments = segSlider.Value
        edges.Run(src)

        src.CopyTo(dst2)
        maskFless.SetTo(0)
        maskFeat.SetTo(0)
        Parallel.ForEach(task.gridList,
        Sub(roi)
            Dim segments() = cv.Cv2.HoughLines(edges.dst2(roi), options.rho, options.theta, options.threshold)
            If segments.Count = 0 Then maskFless(roi).SetTo(255)
            If edges.dst2(roi).CountNonZero >= minSegments Then maskFeat(roi).SetTo(255)
        End Sub)

        maskPredict.SetTo(255)
        maskPredict.SetTo(0, maskFless)
        maskPredict.SetTo(0, maskFeat)

        dst1.SetTo(0)
        src.CopyTo(dst1, maskPredict)
        Dim pCount = maskPredict.CountNonZero
        labels(1) = Format(pCount / dst1.Total, "0%") + " are inbetween feature and featureless"

        dst2.SetTo(0)
        src.CopyTo(dst2, maskFless)
        dst3.SetTo(0)
        src.CopyTo(dst3, maskFeat)
    End Sub
End Class








Public Class Hough_LaneFinder : Inherits VB_Parent
    Dim hls As New LaneFinder_HLSColor
    Public segments As cv.LineSegmentPoint()
    Public mask As cv.Mat
    Public laneLineMinY As Integer
    Public Sub New()
        labels = {"Original video image", "Mask to isolate lane regions", "Combined yellow and white masks", "HoughLines output"}
        desc = "Use Hough to isolate features in the mask of the road."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hls.Run(empty)
        If task.optionsChanged Then
            Dim w = hls.input.video.dst2.Width
            Dim h = hls.input.video.dst2.Height

            Dim bl = New cv.Point(w * 0.1, h * 0.95)
            Dim tl = New cv.Point(w * 0.4, h * 0.6)
            Dim br = New cv.Point(w * 0.95, h * 0.95)
            Dim tr = New cv.Point(w * 0.6, h * 0.6)

            Dim pList() As cv.Point = {bl, tl, tr, br}
            mask = New cv.Mat(New cv.Size(w, h), cv.MatType.CV_8U, 0)
            mask.FillConvexPoly(pList, cv.Scalar.White, task.lineType)
        End If
        dst1 = mask.Clone

        dst0 = hls.dst0
        dst2 = New cv.Mat(mask.Size(), cv.MatType.CV_8U, 0)
        hls.dst3.CopyTo(dst2, mask)

        Dim rho = 1
        Dim theta = cv.Cv2.PI / 180
        Dim threshold = 20
        Dim minLineLength = 20
        Dim maxLineGap = 300
        segments = cv.Cv2.HoughLinesP(dst2.Clone, rho, theta, threshold, minLineLength, maxLineGap)
        dst3 = New cv.Mat(mask.Size(), cv.MatType.CV_8UC3, 0)
        laneLineMinY = dst2.Height
        For i = 0 To segments.Length - 1
            If laneLineMinY > segments(i).P1.Y Then laneLineMinY = segments(i).P1.Y
            If laneLineMinY > segments(i).P2.Y Then laneLineMinY = segments(i).P2.Y
            DrawLine(dst3, segments(i).P1, segments(i).P2, task.HighlightColor)
        Next
    End Sub
End Class






Public Class Hough_Sudoku : Inherits VB_Parent
    Dim hough As New Hough_Basics
    Public Sub New()
        desc = "Successful use of Hough to find lines in Sudoku grid."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst2 = cv.Cv2.ImRead(task.HomeDir + "opencv/Samples/Data/sudoku.png").Resize(dst2.Size)
        dst3 = dst2.Clone
        hough.Run(dst2)
        houghShowLines(dst3, hough.segments, hough.options.lineCount)
    End Sub
End Class
