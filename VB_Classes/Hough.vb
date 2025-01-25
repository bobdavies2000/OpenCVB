Imports cv = OpenCvSharp
' https://docs.opencvb.org/3.1.0/d6/d10/tutorial_py_houghlines.html
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/HoughLinesSample.vb
Public Class Hough_Basics : Inherits TaskParent
    Dim edges As New Edge_Basics
    Public segments() As cv.LineSegmentPolar
    Public options As New Options_Hough
    Public Sub New()
        desc = "Use Houghlines to find lines in the image."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()
        edges.Run(src)

        segments = cv.Cv2.HoughLines(edges.dst2, options.rho, options.theta, options.threshold)
        labels(2) = "Found " + CStr(segments.Length) + " Lines"

        If standaloneTest() Then
            src.CopyTo(dst2)
            dst2.SetTo(white, edges.dst2)
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






Public Class Hough_Sudoku : Inherits TaskParent
    Dim hough As New Hough_Basics
    Public Sub New()
       optiBase.findslider("Canny threshold1").Value = 50
       optiBase.findslider("Canny threshold2").Value = 200
       optiBase.findslider("Hough rho").Value = 1
       optiBase.findslider("Hough theta").Value = 1000 * cv.Cv2.PI / 180
       optiBase.findslider("Hough threshold").Value = 150
        desc = "Successful use of Hough to find lines in Sudoku grid."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst2 = cv.Cv2.ImRead(task.HomeDir + "opencv/Samples/Data/sudoku.png").Resize(dst2.Size)
        dst3 = dst2.Clone
        hough.Run(dst2)
        houghShowLines(dst3, hough.segments, hough.options.lineCount)
    End Sub
End Class






Public Class Hough_Sudoku1 : Inherits TaskParent
    Dim lines as new Line_Basics
    Public Sub New()
        desc = "FastLineDetect version for finding lines in the Sudoku input."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst3 = cv.Cv2.ImRead(task.HomeDir + "opencv/Samples/Data/sudoku.png").Resize(dst2.Size)
        lines.Run(dst3.Clone)
        dst2 = lines.dst2
        labels(2) = lines.labels(2)
        For Each lp In task.lpList
            dst3.Line(lp.xp1, lp.xp2, cv.Scalar.Red, task.lineWidth, task.lineType)
        Next
    End Sub
End Class







' https://docs.opencvb.org/3.1.0/d6/d10/tutorial_py_houghlines.html
Public Class Hough_Circles : Inherits TaskParent
    Dim circles As New Draw_Circles
    Dim method As Integer = 3
    Public Sub New()
       optiBase.findslider("DrawCount").Value = 3
        labels(2) = "Input circles to Hough"
        labels(3) = "Hough Circles found"
        desc = "Find circles using HoughCircles."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
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









Public Class Hough_Lines_MT : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim options As New Options_Hough
    Public Sub New()
        labels(2) = "Output of the Canny Edge algorithm (no Hough lines)"
        labels(3) = "Hough Lines for each threaded cell or if no lines, the featureless cell depth data."
        desc = "Multithread Houghlines to find lines in image fragments."
    End Sub

    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()
        edges.Run(src)
        dst2 = edges.dst2

        Dim depth8uC3 = task.depthRGB
        Parallel.ForEach(task.gridRects,
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













Public Class Hough_Featureless : Inherits TaskParent
    Public edges As New Edge_Basics
    Public noDepthCount() As Integer
    Public options As New Options_Hough
    Public roiColor() As cv.Vec3b
    Public Sub New()
        task.gOptions.setGridSize(10)
        labels(2) = "Featureless mask"
        desc = "Multithread Houghlines to find featureless regions in an image."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        edges.Run(src)

        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        Dim regionCount As Integer
        ReDim noDepthCount(task.gridRects.Count - 1)
        ReDim roiColor(task.gridRects.Count - 1)

        For Each roi In task.gridRects
            Dim segments() = cv.Cv2.HoughLines(edges.dst2(roi), options.rho, options.theta, options.threshold)
            If edges.dst2(roi).CountNonZero = 0 Then
                regionCount += 1
                dst2(roi).SetTo(255)
            End If
        Next

        dst3.SetTo(0)
        src.CopyTo(dst3, dst2)
        labels(2) = "FeatureLess Regions = " + CStr(regionCount)
        labels(3) = "Of the " + CStr(task.gridRects.Count) + " grid elements, " + CStr(regionCount) + " had no edge or hough features present"
    End Sub
End Class








Public Class Hough_FeatureLessTopX : Inherits TaskParent
    Public edges As New Edge_Basics
    Public options As New Options_Hough
    Public maskFless As cv.Mat
    Public maskFeat As cv.Mat
    Public maskPredict As cv.Mat
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        task.gOptions.setGridSize(10)
        maskFless = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        maskFeat = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        maskPredict = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        labels = {"", "", "Areas without features", "Areas with features"}
        desc = "Multithread Houghlines to find featureless regions in an image."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        Static segSlider =optiBase.findslider("Minimum feature pixels")
        Dim minSegments = segSlider.Value
        edges.Run(src)

        src.CopyTo(dst2)
        maskFless.SetTo(0)
        maskFeat.SetTo(0)
        Parallel.ForEach(task.gridRects,
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








Public Class Hough_LaneFinder : Inherits TaskParent
    Dim hls As New LaneFinder_HLSColor
    Public segments As cv.LineSegmentPoint()
    Public mask As cv.Mat
    Public laneLineMinY As Integer
    Public Sub New()
        labels = {"Original video image", "Mask to isolate lane regions", "Combined yellow and white masks", "HoughLines output"}
        desc = "Use Hough to isolate features in the mask of the road."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        hls.Run(src)
        If task.optionsChanged Then
            Dim w = hls.input.video.dst2.Width
            Dim h = hls.input.video.dst2.Height

            Dim bl = New cv.Point(w * 0.1, h * 0.95)
            Dim tl = New cv.Point(w * 0.4, h * 0.6)
            Dim br = New cv.Point(w * 0.95, h * 0.95)
            Dim tr = New cv.Point(w * 0.6, h * 0.6)

            Dim pList() As cv.Point = {bl, tl, tr, br}
            mask = New cv.Mat(New cv.Size(w, h), cv.MatType.CV_8U, cv.Scalar.All(0))
            mask.FillConvexPoly(pList, white, task.lineType)
        End If
        dst1 = mask.Clone

        dst0 = hls.dst0
        dst2 = New cv.Mat(mask.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        hls.dst3.CopyTo(dst2, mask)

        Dim rho = 1
        Dim theta = cv.Cv2.PI / 180
        Dim threshold = 20
        Dim minLineLength = 20
        Dim maxLineGap = 300
        segments = cv.Cv2.HoughLinesP(dst2.Clone, rho, theta, threshold, minLineLength, maxLineGap)
        dst3 = New cv.Mat(mask.Size(), cv.MatType.CV_8UC3, cv.Scalar.All(0))
        laneLineMinY = dst2.Height
        For i = 0 To segments.Length - 1
            If laneLineMinY > segments(i).P1.Y Then laneLineMinY = segments(i).P1.Y
            If laneLineMinY > segments(i).P2.Y Then laneLineMinY = segments(i).P2.Y
            DrawLine(dst3, segments(i).P1, segments(i).P2, task.HighlightColor)
        Next
    End Sub
End Class





Public Class Hough_Lines : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim options As New Options_Hough
    Public Sub New()
        task.gOptions.GridSlider.Value = 30
        labels(2) = "Output of the Canny Edge algorithm (no Hough lines)"
        labels(3) = "Hough Lines for each threaded cell or if no lines, the featureless cell depth data."
        desc = "Multithread Houghlines to find lines in image fragments."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()
        edges.Run(src)
        dst2 = edges.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        dst3.SetTo(0)
        For Each roi In task.gridRects
            Dim segments = cv.Cv2.HoughLines(edges.dst2(roi), options.rho, options.theta, options.threshold)
            If segments.Count = 0 Then Continue For
            houghShowLines(dst2(roi), segments, 2)
            houghShowLines(dst3(roi), segments, 2)
        Next
        dst2.SetTo(white, task.gridMask)
    End Sub
End Class





Public Class Hough_FullImage : Inherits TaskParent
    Dim edges As New Edge_Basics
    Public segments() As cv.LineSegmentPolar
    Public options As New Options_Hough
    Public Sub New()
        desc = "Use Houghlines to find lines in the image."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()
        edges.Run(src)

        Dim segments = cv.Cv2.HoughLines(edges.dst2, options.rho, options.theta, options.threshold)
        labels(2) = "Found " + CStr(segments.Length) + " Lines"

        If standaloneTest() Then
            src.CopyTo(dst2)
            dst2.SetTo(white, edges.dst2)
            houghShowLines(dst2, segments, options.lineCount)

            dst3.SetTo(0)
            houghShowLines(dst3, segments, options.lineCount)
            dst3.SetTo(white, edges.dst2)
        End If
    End Sub
End Class






Public Class Hough_Probabilistic : Inherits TaskParent
    Dim edges As New Edge_Basics
    Public segments() As cv.LineSegmentPolar
    Public options As New Options_Hough
    Public Sub New()
        task.gOptions.GridSlider.Value = 30
        desc = "Use Houghlines to find lines in the image."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()
        edges.Run(src)

        Static segments As cv.LineSegmentPoint()
        If task.gOptions.debugChecked Then
            src.CopyTo(dst2)
            dst2.SetTo(white, edges.dst2)
            dst3.SetTo(0)
            segments = cv.Cv2.HoughLinesP(edges.dst2, options.rho, options.theta, options.threshold)
            For i = 0 To Math.Min(segments.Length, options.lineCount) - 1
                Dim line = segments(i)
                dst3.Line(line.P1, line.P2, cv.Scalar.Red, task.lineWidth + 2, task.lineType)
            Next
            labels(3) = "Probablistic lines = " + CStr(segments.Length)
        End If
    End Sub
End Class
