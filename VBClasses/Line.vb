Imports cv = OpenCvSharp
Public Class Line_Basics_TA : Inherits TaskParent
    Implements IDisposable
    Public lpList As New List(Of lpData)
    Public motionMask As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 255)
    Dim ld As cv.XImgProc.FastLineDetector
    Public removeOverlappingLines As Boolean = True
    Public overLappingCount As Integer
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        If standalone Then task.gOptions.showMotionMask.Checked = True
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "If line is NOT in motion mask, then keep it.  If line is in motion mask, add it."
    End Sub
    Public Shared Function getRawLines(lines As cv.Vec4f()) As List(Of lpData)
        Dim lpList As New List(Of lpData)
        For Each v In lines
            If v(0) >= 0 And v(0) <= task.workRes.Width And v(1) >= 0 And v(1) <= task.workRes.Height And
                           v(2) >= 0 And v(2) <= task.workRes.Width And v(3) >= 0 And v(3) <= task.workRes.Height Then
                Dim p1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim p2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                If p1.X >= 0 And p1.X < task.workRes.Width And p1.Y >= 0 And p1.Y < task.workRes.Height And
                   p2.X >= 0 And p2.X < task.workRes.Width And p2.Y >= 0 And p2.Y < task.workRes.Height Then
                    p1 = lpData.validatePoint(p1)
                    p2 = lpData.validatePoint(p2)
                    Dim lp = New lpData(p1, p2)
                    If lp.pVec1(2) > 0 And lp.pVec2(2) > 0 Then lpList.Add(lp)
                End If
            End If
        Next
        Return lpList
    End Function
    Public Function getRawVecs(src As cv.Mat) As cv.Vec4f()
        ' task.lines is always going to present.  Reuse the stateless lp detector.
        Return ld.Detect(src)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then motionMask = task.motion.motionMask

        If src.Channels <> 1 Or src.Type <> cv.MatType.CV_8U Then src = task.gray.Clone
        dst2 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If lpList.Count <= 1 Then
            motionMask.SetTo(255)
            lpList = getRawLines(ld.Detect(src))
        End If

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each lp In lpList
            If Not (motionMask.Get(Of Byte)(lp.p1.Y, lp.p1.X) Or motionMask.Get(Of Byte)(lp.p2.Y, lp.p2.X)) Then
                lp.age += 1
                sortlines.Add(lp.length, lp)
            End If
        Next
        Dim count As Integer = sortlines.Count

        lpList = getRawLines(ld.Detect(src))

        For Each lp In lpList
            If motionMask.Get(Of Byte)(lp.p1.Y, lp.p1.X) Or motionMask.Get(Of Byte)(lp.p2.Y, lp.p2.X) Then
                sortlines.Add(lp.length, lp)
            End If
        Next
        Dim newCount As Integer = sortlines.Count - count

        lpList.Clear()
        overLappingCount = 0
        dst1.SetTo(0)
        For Each lp In sortlines.Values
            lp.index = lpList.Count
            If removeOverlappingLines Then
                If lp.rect.Width = 0 Then Continue For
                If lp.rect.Height = 0 Then Continue For
                If dst1(lp.rect).CountNonZero > 0 Then
                    overLappingCount += 1
                    Continue For
                End If
            End If
            dst1.Line(lp.p1, lp.p2, lp.index + 1, task.lineWidth + 1, cv.LineTypes.Link4)
            dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth + 1, task.lineType)
            lpList.Add(lp)
        Next

        dst3 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

        If lpList.Count > 0 Then
            If task.lpD.rect.Width = 0 Then task.lpD = lpList(0)
        End If

        labels(2) = CStr(count) + " lines retained - " + CStr(newCount) + " were new"
        If removeOverlappingLines Then labels(2) += ". " + CStr(overLappingCount) + " overlap(s) removed."
    End Sub
    Protected Overrides Sub Finalize()
        ld.Dispose()
    End Sub
End Class





Public Class NR_Line_Basics_TATest : Inherits TaskParent
    Dim lines As New Line_Basics_TA
    Public Sub New()
        desc = "Line_Basics_TA is a task algorithm so this is the better way to test it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.motionMask = task.motion.motionMask
        lines.Run(task.gray)
        dst2.SetTo(0)
        For Each lp In lines.lpList
            dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth, task.lineType)
        Next
        labels(2) = lines.labels(2)
    End Sub
End Class






Public Class NR_Line_Core : Inherits TaskParent
    Implements IDisposable
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
                       "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = task.gray
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim vecArray = task.lines.getRawVecs(src)
        lpList = Line_Basics_TA.getRawLines(vecArray)

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next

        labels(2) = CStr(lpList.Count) + " lines were detected."
    End Sub
End Class






Public Class Line_PerpendicularTest : Inherits TaskParent
    Public input As lpData
    Public output As lpData
    Dim midPoint As cv.Point2f
    Public Sub New()
        labels = {"", "", "White is the line selected for display and yellow is perpendicular line", ""}
        desc = "Find the line perpendicular to the line created by the points provided."
    End Sub
    Public Shared Function computePerp(lp As lpData) As lpData
        Dim midPoint = New cv.Point2f((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2)
        Dim m = If(lp.slope = 0, lpData.maxSlope, -1 / lp.slope)
        Dim b = midPoint.Y - m * midPoint.X
        Dim p1 = New cv.Point2f(-b / m, 0)
        Dim p2 = New cv.Point2f((task.workRes.Height - b) / m, task.workRes.Height)

        Dim w = task.workRes.Width
        Dim h = task.workRes.Height

        If p1.X < 0 Then p1 = New cv.Point2f(0, b)
        If p1.X > w Then p1 = New cv.Point2f(w, m * w + b)
        If p1.Y < 0 Then p1 = New cv.Point2f(-b / m, 0)
        If p1.Y > h Then p1 = New cv.Point2f(w, m * w + b)

        If p2.X < 0 Then p2 = New cv.Point2f(0, b)
        If p2.X > w Then p2 = New cv.Point2f(w, m * w + b)
        If p2.Y < 0 Then p2 = New cv.Point2f(-b / m, 0)
        If p2.Y > h Then p2 = New cv.Point2f(w, m * w + b)

        Return New lpData(p1, p2)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then input = task.lpGravity
        dst2.SetTo(0)
        dst2.Line(input.p1, input.p2, white, task.lineWidth, task.lineType)

        output = computePerp(input)
        DrawCircle(dst2, input.ptCenter, task.DotSize + 2, cv.Scalar.Red)
        dst2.Line(output.p1, output.p2, yellow, task.lineWidth, task.lineType)

        If standaloneTest() Then SetTrueText("The line displayed at left is the gravity vector.", 3)
    End Sub
End Class






Public Class NR_Line_Parallel : Inherits TaskParent
    Public classes() As List(Of Integer) ' groups of lines that are parallel
    Public unParallel As New List(Of Integer) ' lines which are not parallel
    Public Sub New()
        labels(2) = "Text shows the parallel class with 0 being unparallel."
        desc = "Identify lines that are parallel (or nearly so), perpendicular, and not parallel."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        Dim parallels As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
        For Each lp In task.lines.lpList
            parallels.Add(lp.angle, lp.index)
        Next

        ReDim classes(task.lines.lpList.Count - 1)
        Dim index As Integer, j As Integer
        unParallel.Clear()
        For i = 0 To parallels.Count - 1
            Dim lp1 = task.lines.lpList(parallels.ElementAt(i).Value)
            For j = i + 1 To parallels.Count - 1
                Dim lp2 = task.lines.lpList(parallels.ElementAt(j).Value)
                If Math.Abs(lp1.angle - lp2.angle) < task.angleThreshold Then
                    If classes(index) Is Nothing Then classes(index) = New List(Of Integer)({lp1.index})
                    classes(index).Add(lp2.index)
                Else
                    Exit For
                End If
            Next
            If classes(index) Is Nothing Then unParallel.Add(lp1.index)
            If j > i + 1 Then index += 1
            i = j - 1
        Next

        dst2 = src
        Dim colorIndex As Integer = 1
        For i = 0 To classes.Count - 1
            If classes(i) Is Nothing Then Exit For
            For j = 0 To classes(i).Count - 1
                Dim lp = task.lines.lpList(classes(i).ElementAt(j))
                dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth * 2, task.lineType)
                SetTrueText(CStr(colorIndex), lp.ptCenter)
            Next
            colorIndex += 1
        Next

        For Each index In unParallel
            Dim lp = task.lines.lpList(index)
            vbc.DrawLine(dst2, lp)
            SetTrueText("0", lp.ptCenter)
        Next

        dst3 = task.lines.dst2
        labels(3) = task.lines.labels(2)
    End Sub
End Class






' https://stackoverflow.com/questions/7446126/opencv-2d-line-intersection-helper-function
Public Class Line_Intersection : Inherits TaskParent
    Public lp1 As lpData, lp2 As lpData
    Public intersectionPoint As cv.Point2f
    Public Sub New()
        desc = "Determine if 2 lines intersect, where the point is, and if that point is in the image."
    End Sub
    Public Shared Function IntersectTest(p1 As cv.Point2f, p2 As cv.Point2f, p3 As cv.Point2f, p4 As cv.Point2f) As cv.Point2f
        Dim x = p3 - p1
        Dim d1 = p2 - p1
        Dim d2 = p4 - p3
        Dim cross = d1.X * d2.Y - d1.Y * d2.X
        If Math.Abs(cross) < 0.000001 Then Return New cv.Point2f
        Dim t1 = (x.X * d2.Y - x.Y * d2.X) / cross
        Dim pt = p1 + d1 * t1
        Return pt
    End Function
    Public Shared Function IntersectTest(lp1 As lpData, lp2 As lpData) As cv.Point2f
        Dim x = lp2.p1 - lp1.p1
        Dim d1 = lp1.p2 - lp1.p1
        Dim d2 = lp2.p2 - lp2.p1
        Dim cross = d1.X * d2.Y - d1.Y * d2.X
        If Math.Abs(cross) < 0.000001 Then Return New cv.Point2f
        Dim t1 = (x.X * d2.Y - x.Y * d2.X) / cross
        Dim pt = lp1.p1 + d1 * t1
        Return pt
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            If task.heartBeat Then
                lp1 = New lpData(New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)),
                                     New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)))
                lp2 = New lpData(New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)),
                                     New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height)))
            End If
        End If

        intersectionPoint = Line_Intersection.IntersectTest(lp1, lp2)

        If standaloneTest() Then
            dst2.SetTo(0)
            dst2.Line(lp1.p1, lp1.p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
            dst2.Line(lp2.p1, lp2.p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
            If intersectionPoint <> New cv.Point2f Then
                DrawCircle(dst2, intersectionPoint, task.DotSize + 4, white)
                labels(2) = "Intersection point = " + CStr(CInt(intersectionPoint.X)) + " x " + CStr(CInt(intersectionPoint.Y))
            Else
                labels(2) = "Parallel!!!"
            End If
            If intersectionPoint.X < 0 Or intersectionPoint.X > dst2.Width Or intersectionPoint.Y < 0 Or intersectionPoint.Y > dst2.Height Then
                labels(2) += " (off screen)"
            End If
        End If
    End Sub
End Class





Public Class NR_Line_Select : Inherits TaskParent
    Public delaunay As New Delaunay_LineSelect
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Select a line with mouse movement and put the selection into task.lpD."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        delaunay.Run(src)
        dst2 = delaunay.dst1
        labels(2) = delaunay.labels(2)
        strOut = task.lpD.displayCell(dst3)
        SetTrueText(strOut, 1) ' the line info is already prepped in strout in delaunay.
    End Sub
End Class





Public Class NR_Line_Vertical : Inherits TaskParent
    Dim vbPoints As New NR_BrickPoint_Vertical
    Dim knn As New KNN_Basics
    Public Sub New()
        desc = "Match points to the nearest line that is also vertical"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        vbPoints.Run(src)
        dst2 = vbPoints.dst2

        knn.ptListTrain = New List(Of cv.Point)(vbPoints.ptList)
        knn.ptListQuery = New List(Of cv.Point)(vbPoints.ptList)
        knn.Run(dst2)
        labels(3) = "There are " + CStr(knn.result.GetUpperBound(0)) + " input points to KNN."

        Dim lpList As New List(Of lpData)
        For i = 0 To knn.result.GetUpperBound(0) - 1
            Dim deltaX As New List(Of Single)
            Dim ptList As New List(Of cv.Point)
            Dim p1 = vbPoints.ptList(knn.result(i, 0))
            For j = 1 To Math.Min(knn.result.Length - 1, 6) - 1
                Dim p2 = vbPoints.ptList(knn.result(i, j))
                Dim delta = Math.Abs(p1.X - p2.X)
                deltaX.Add(delta)
                ptList.Add(p2)
            Next

            Dim minVal = deltaX.Min
            Dim index = deltaX.IndexOf(minVal)
            If minVal < task.brickEdgeLen Then
                Dim lp = New lpData(p1, ptList(index))
                If lp.indexVTop < 0 Or lp.indexVBot < 0 Then Continue For
                lp.index = lpList.Count
                lpList.Add(lp)
                dst2.Line(p1, ptList(index), task.highlight, task.lineWidth, task.lineType)
            End If
        Next

        Dim topGroups(task.bricksPerRow - 1) As List(Of Integer)
        For Each lp In lpList
            If topGroups(lp.indexVTop) Is Nothing Then topGroups(lp.indexVTop) = New List(Of Integer)
            topGroups(lp.indexVTop).Add(lp.index)
        Next

        Dim indexVTop = Math.Abs(task.gOptions.DebugSlider.Value)
        dst3.SetTo(0)
        If indexVTop < topGroups.Count Then
            If topGroups(indexVTop) IsNot Nothing Then
                Dim botGroups(task.bricksPerRow - 1) As List(Of Integer)
                For Each index In topGroups(indexVTop)
                    Dim lp = lpList(index)
                    If botGroups(lp.indexVBot) Is Nothing Then botGroups(lp.indexVBot) = New List(Of Integer)
                    botGroups(lp.indexVBot).Add(lp.index)
                Next

                Dim maxIndex As Integer
                Dim maxCount As Integer
                For i = 0 To botGroups.Count - 1
                    If botGroups(i) Is Nothing Then Continue For
                    If maxCount < botGroups(i).Count Then
                        maxCount = botGroups.Count
                        maxIndex = i
                    End If
                Next
                For Each index In botGroups(maxIndex)
                    Dim lp = lpList(index)
                    vbc.DrawLine(dst3, lp)
                Next
            End If
        End If

        labels(2) = "There were " + CStr(lpList.Count) + " neighbors that formed good lines."
    End Sub
End Class






Public Class Line_DepthHistogram : Inherits TaskParent
    Dim lineVert As New Line_Vertical
    Dim plot As New PlotMouse_Basics
    Public Sub New()
        plot.plotHist.createHistogram = True
        plot.plotHist.removeZeroEntry = True
        If standalone Then task.gOptions.DebugCheckBox.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Show the histogram of the depth data for a line.  Use debug check box to study longest line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lineVert.Run(src)
        dst3 = lineVert.dst2
        For Each lp In lineVert.lpLeft
            Dim depth = task.pcSplit(2)(lp.rect)
            Dim depthMask As New cv.Mat(lp.rect.Size, cv.MatType.CV_8U, 0)
            Dim p1 = New cv.Point2f(lp.p1.X - lp.rect.X, lp.p1.Y - lp.rect.Y)
            Dim p2 = New cv.Point2f(lp.p2.X - lp.rect.BottomRight.X, lp.p2.Y - lp.rect.BottomRight.Y)
            depthMask.Line(p1, p2, 255, task.lineWidth, task.lineType)
            Dim mmDepth = GetMinMax(depth, depthMask)
            plot.plotHist.Run(depth)
            Dim hist = plot.plotHist.histArray.ToList
            Dim bestIndex = hist.IndexOf(hist.Max)
            Dim incr = (mmDepth.maxVal - mmDepth.minVal) / task.gOptions.HistBinBar.Value
            Dim depth1 = mmDepth.minVal + incr * bestIndex
            If task.gOptions.DebugCheckBox.Checked Then
                dst2 = plot.plotHist.dst2
                dst3.Rectangle(lp.rect, task.highlight, task.lineWidth)
                labels(3) = "The histogram at left indicates that the depth is likely at " + Format(depth1, fmt1) + "m" + vbCrLf
                labels(2) = plot.plotHist.labels(2)
                Exit For
            End If
        Next
        strOut = "To view any line, uncheck the debugCheckBox in the global options." + vbCrLf
        strOut += "With debugCheckBox checked, only the longest line will be displayed." + vbCrLf
        strOut += "Hover with the mouse over the line whose depth will be plotted." + vbCrLf
        SetTrueText(strOut, 1)
    End Sub
End Class





Public Class Line_Motion : Inherits TaskParent
    Dim lrLines As New Line_LeftRightMotion
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        desc = "Show lines with motion and lines with no motion in the leftView."
    End Sub
    Private Function lpMotion(lp As lpData) As Boolean
        ' return true if either line endpoint was in the motion mask.
        If task.lines.motionMask.Get(Of Byte)(lp.p1.Y, lp.p1.X) Then Return True
        If task.lines.motionMask.Get(Of Byte)(lp.p2.Y, lp.p2.X) Then Return True
        Return False
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = dst2.Clone
        lrLines.Run(Nothing)

        Dim motionCount As Integer
        Dim noMotionCount As Integer
        For Each lp In lrLines.linesLeft.lpList
            If lpMotion(lp) Then
                dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth + 1, task.lineType)
                motionCount += 1
            Else
                dst3.Line(lp.p1, lp.p2, lp.color, task.lineWidth + 1, task.lineType)
                noMotionCount += 1
            End If
        Next
        labels(2) = CStr(motionCount) + " lines showed motion"
        labels(3) = CStr(noMotionCount) + " lines showed no motion"
    End Sub
End Class




Public Class Line_LeftRightMotion : Inherits TaskParent
    Public linesLeft As New Line_Basics_TA
    Public linesRight As New Line_Basics_TA
    Dim motionLeft As New Motion_Basics_TA
    Dim motionRight As New Motion_Basics_TA
    Public Sub New()
        labels = {"", "", "Left image lines", "Right image lines"}
        desc = "Find the lines in the Left and Right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        motionLeft.Run(task.leftView)
        linesLeft.motionMask = motionLeft.dst3
        linesLeft.Run(task.leftView)

        dst2 = linesLeft.dst2
        labels(2) = linesLeft.labels(2)

        motionRight.Run(task.rightView)
        linesRight.motionMask = motionRight.dst3
        linesRight.Run(task.rightView)

        dst3 = linesRight.dst2
        labels(3) = linesLeft.labels(2)
    End Sub
End Class





Public Class Line_Vertical : Inherits TaskParent
    Dim lrLines As New Line_LeftRightMotion
    Public lpLeft As New List(Of lpData)
    Public lpRight As New List(Of lpData)
    Public Sub New()
        desc = "Find just the vertical lines in the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lrLines.Run(src)
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        lpLeft.Clear()
        For Each lp In lrLines.linesLeft.lpList
            If Math.Abs(lp.angle) > 87 Then
                lpLeft.Add(lp)
                dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth, task.lineType)
            End If
        Next

        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        lpRight.Clear()
        For Each lp In lrLines.linesRight.lpList
            If Math.Abs(lp.angle) > 87 Then
                lpRight.Add(lp)
                dst3.Line(lp.p1, lp.p2, lp.color, task.lineWidth, task.lineType)
            End If
        Next
    End Sub
End Class






''' <summary>Holds a line with a stable track ID and color for multi-frame tracking.</summary>
Friend Class TrackedLine
    Public trackId As Integer
    Public lp As lpData
    Public missedCount As Integer
End Class


''' <summary>Find all lines in the left image, assign each a stable ID, and track them as the camera moves.</summary>
Public Class Line_LeftTrack : Inherits TaskParent
    ''' <summary>Tracked lines: (trackId, lpData, color, missedCount).</summary>
    Dim tracked As New List(Of TrackedLine)
    Dim nextTrackId As Integer = 1
    Const minLength As Single = 12.0F
    Const maxMissed As Integer = 5
    Const maxTracked As Integer = 200
    Const angleThresh As Single = 8.0F
    Const distThresh As Single = 120.0F
    Const lenRatioThresh As Single = 0.45F

    Public lpList As New List(Of lpData)
    Dim lines As New Line_Basics_TA
    Dim options As New Options_LeftRightCorrelation
    Dim motionLeft As New Motion_Basics_TA
    Public Sub New()
        If standalone Then task.gOptions.displayDst0.Checked = True
        labels = {"", "", "Left image: detected lines with stable track IDs", ""}
        desc = "Cursor.ai: Find all lines in the left image, identify each and track them."
    End Sub
    Private Function matchScore(r As lpData, t As TrackedLine) As Single
        Dim ad = Math.Abs(r.angle - t.lp.angle)
        If ad > 90 Then ad = 180 - ad
        If ad > angleThresh Then Return Single.MaxValue
        Dim dist = r.ptCenter.DistanceTo(t.lp.ptCenter)
        If dist > distThresh Then Return Single.MaxValue
        Dim mx = Math.Max(r.length, t.lp.length) + 1.0F
        Dim lr = Math.Abs(r.length - t.lp.length) / mx
        If lr > lenRatioThresh Then Return Single.MaxValue
        Return ad * 2.0F + dist / 20.0F + lr * 20.0F
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst0 = task.leftView
        motionLeft.Run(task.leftView)
        lines.motionMask = motionLeft.dst3
        lines.Run(task.leftView)
        Dim raw = lines.lpList

        Dim usedRaw As New HashSet(Of lpData)
        Dim usedTracked As New HashSet(Of TrackedLine)

        ' Greedy assignment: longest raw lines first to reduce conflicts
        Dim rawByLen = raw.OrderByDescending(Function(x) x.length).ToList()
        For Each r In rawByLen
            Dim bestT As TrackedLine = Nothing
            Dim bestSc As Single = Single.MaxValue
            For Each t In tracked
                If usedTracked.Contains(t) Then Continue For
                Dim sc = matchScore(r, t)
                If sc < bestSc Then bestSc = sc : bestT = t
            Next
            If bestT IsNot Nothing Then
                bestT.lp = r
                bestT.lp.trackID = bestT.trackId
                bestT.missedCount = 0
                usedRaw.Add(r)
                usedTracked.Add(bestT)
            End If
        Next

        ' Increment missed; remove if over threshold
        For Each t In tracked
            If usedTracked.Contains(t) Then Continue For
            t.missedCount += 1
        Next
        tracked.RemoveAll(Function(t) t.missedCount > maxMissed)

        ' Add new tracks for unmatched raw
        For Each r In raw
            If usedRaw.Contains(r) Then Continue For
            If tracked.Count >= maxTracked Then Exit For
            Dim t As New TrackedLine With {
                            .trackId = nextTrackId,
                            .lp = r,
                            .missedCount = 0
                        }
            r.color = t.lp.color
            r.index = t.trackId
            nextTrackId += 1
            tracked.Add(t)
        Next

        ' Build lpList and draw
        lpList.Clear()
        dst3.SetTo(0)
        For Each t In tracked
            t.lp.index = t.trackId
            If lpList.Count < 10 Then
                dst2.Line(t.lp.p1, t.lp.p2, t.lp.color, options.lineTrackerWidth, cv.LineTypes.Link8)
            End If
            lpList.Add(t.lp)
        Next

        dst2 = task.leftView.Clone
        If dst2.Channels = 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)

        For Each t In tracked
            dst2.Line(t.lp.p1, t.lp.p2, t.lp.color, task.lineWidth, task.lineType)
            dst1.Line(t.lp.p1, t.lp.p2, t.trackId Mod 255 + 1, 1, cv.LineTypes.Link4)
            SetTrueText(CStr(t.trackId), New cv.Point(CInt(t.lp.ptCenter.X), CInt(t.lp.ptCenter.Y)), 2)
        Next

        labels(2) = "Tracked " + CStr(tracked.Count) + " lines, " + CStr(raw.Count) + " detected this frame"
    End Sub
End Class






Public Class Line_Tracker : Inherits TaskParent
    Dim lines As New Line_Basics_TA
    Dim options As New Options_LeftRightCorrelation
    Dim lpList As New List(Of lpData)
    Dim motionLeft As New Motion_Basics_TA
    Public Sub New()
        If standalone Then task.gOptions.displayDst0.Checked = True
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Track lines in the left image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst0 = task.leftView
        motionLeft.Run(task.leftView)
        lines.motionMask = motionLeft.dst3
        lines.Run(task.leftView)
        labels(2) = lines.labels(2)

        dst2.SetTo(0)
        lpList.Clear()
        For Each lp In lines.lpList
            dst2.Line(lp.p1, lp.p2, lp.index + 1, options.lineTrackerWidth, cv.LineTypes.Link8)
            lpList.Add(lp)
            If lpList.Count > 10 Then Exit For
        Next

        dst3 = Palettize(dst2, 0)
    End Sub
End Class






Public Class Line_BrickList : Inherits TaskParent
    Public lp As lpData ' set this input
    Public lpOutput As lpData ' this is the result lp
    Public sobel As New Edge_Sobel
    Public ptList As New List(Of cv.Point)
    Dim options As New Options_LeftRightCorrelation
    Public Sub New()
        labels(3) = "Find the line's bricks containing the line."
        dst3 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Add a bricklist to the requested lp"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.lines.lpList.Count = 0 Then Exit Sub

        If standalone Then
            lp = task.lines.lpList(0)
            If lp.length = 0 Then Exit Sub
        End If

        dst3.SetTo(0)
        dst3.Line(lp.p1, lp.p2, lp.index + 1, options.lineTrackerWidth, cv.LineTypes.Link8)

        Dim r = lp.rect
        dst1.SetTo(0)
        sobel.Run(task.gray)
        sobel.dst2(r).CopyTo(dst1(r), dst3(r))
        DrawRect(dst1, r, black)

        Dim allPoints As New List(Of cv.Point)
        Dim brickList As New List(Of cv.Rect)
        For Each rect In task.gridRects
            Dim brick = dst1(rect)
            If brick.CountNonZero = 0 Then Continue For
            Dim mm = GetMinMax(brick)
            Dim pt = New cv.Point(mm.maxLoc.X + rect.X, mm.maxLoc.Y + rect.Y)
            allPoints.Add(pt)
            brickList.Add(rect)
        Next

        ptList.Clear()
        Dim angles As New List(Of Single)
        Dim epListX1 As New List(Of Single)
        Dim epListY1 As New List(Of Single)
        Dim epListX2 As New List(Of Single)
        Dim epListY2 As New List(Of Single)
        For i = 0 To allPoints.Count - 1
            Dim pt = allPoints(i)
            For j = i + 1 To allPoints.Count - 1
                Dim lpTest = New lpData(pt, allPoints(j))
                'If Math.Abs(lp.angle - lpTest.angle) < task.angleThreshold Then
                angles.Add(lpTest.angle)
                ptList.Add(pt)
                ptList.Add(allPoints(j))
                epListX1.Add(lpTest.pE1.X)
                epListY1.Add(lpTest.pE1.Y)
                epListX2.Add(lpTest.pE2.X)
                epListY2.Add(lpTest.pE2.Y)
                'End If
            Next
        Next

        If ptList.Count < 2 Then
            SetTrueText("No edges were found in the area.", 3)
            lp = Nothing
            Exit Sub
        End If
        dst2 = src
        For Each pt In ptList
            DrawCircle(dst2, pt)
        Next

        Dim x1 = epListX1.Average
        Dim y1 = epListY1.Average
        Dim x2 = epListX2.Average
        Dim y2 = epListY2.Average
        lpOutput = New lpData(New cv.Point2f(x1, y1), New cv.Point2f(x2, y2))
        vbc.DrawLine(dst2, lpOutput)

        If standalone Then lp = lpOutput

        For Each r In brickList
            DrawRect(dst3, r, white)
        Next
    End Sub
End Class



Public Class Line_BrickListTest : Inherits TaskParent
    Dim brickLines As New Line_BrickList
    Public Sub New()
        desc = "Find the brick list for each line in the lines.lplist"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        brickLines.lp = task.lines.lpList(0)
        brickLines.Run(task.gray)
        'For Each r In brickLines.brickList
        '    DrawRect(dst3, r, white)
        'Next
    End Sub
End Class





Public Class Line_MapRects : Inherits TaskParent
    Public lpList As New List(Of lpData) ' the list of non-overlapping lines.
    Public pointCloud As New cv.Mat
    Dim depthToWorld As New Cloud_DepthToWorld
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "Move mouse over any image to see line."
        labels(3) = "Each rectangle is divided into 2 regions defined by the line."
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Create a map with the lp.rect field."
    End Sub
    Private Function fillTriangle(lp As lpData, p1 As cv.Point) As Boolean
        Dim val = dst3.Get(Of Byte)(p1.Y, p1.X)
        If val > 0 Then
            dst3.FloodFill(p1, 255)
            Return True
        End If
        Return False
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim mmList As New List(Of mmData)
        Dim pad = 5
        dst3.SetTo(0)
        dst0.SetTo(0)
        lpList.Clear()
        For Each lp In task.lines.lpList
            Dim val = dst3.Get(Of Byte)(lp.ptCenter.Y, lp.ptCenter.X)
            If val = 0 Then
                dst0.Rectangle(lp.rect, lp.index + 1, -1)
                dst3.Rectangle(lp.rect, lp.index + 1, -1)
                dst3.Line(lp.p1, lp.p2, 0, task.lineWidth, cv.LineTypes.Link8)
                lpList.Add(lp)
            End If
        Next
        labels(2) = CStr(lpList.Count) + " non-overlapping lines were found."

        For Each lp In task.lines.lpList
            If fillTriangle(lp, lp.rect.TopLeft) Then Continue For
            If fillTriangle(lp, lp.rect.BottomRight) Then Continue For

            Dim topRight As New cv.Point(lp.rect.X + lp.rect.Width, lp.rect.Top)
            If fillTriangle(lp, topRight) Then Continue For

            Dim botleft As New cv.Point(lp.rect.X, lp.rect.Top + lp.rect.Height)
            If fillTriangle(lp, botleft) Then Continue For
        Next

        dst2 = Palettize(dst3, 0)
        Dim pcZ = task.pcSplit(2).Clone
        For Each lp In task.lines.lpList
            Dim mask1 = dst3(lp.rect).Clone
            mask1 = mask1.InRange(255, 255)
            Dim mask2 = Not mask1

            Dim depth1 = pcZ(lp.rect).Mean(mask1)(0)
            Dim depth2 = pcZ(lp.rect).Mean(mask2)(0)

            ' if the depth change at the line is less than 5 cm's, ignore it.
            If Math.Abs(depth1 - depth2) > 0.05 Then
                depth2 = depth1
                pcZ(lp.rect).SetTo(depth1, mask1)
                pcZ(lp.rect).SetTo(depth2, mask2)

                depthToWorld.Run(pcZ(lp.rect))
                depthToWorld.dst2.CopyTo(pcZ(lp.rect))
            End If
        Next

        cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1), pcZ}, pointCloud)

        Dim index = dst0.Get(Of Byte)(task.mouseMovePoint.Y, task.mouseMovePoint.X) - 1
        If task.lines.lpList.Count > 0 Then
            If index <= 0 Then
                If task.lpD Is Nothing Then task.lpD = task.lines.lpList(0)
            Else
                task.lpD = task.lines.lpList(index)
            End If
            task.lpD.displayCell(dst1)
        End If
    End Sub
End Class





Public Class Line_Map : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "Move mouse over any image to see line."
        labels(3) = "Each rectangle is divided into 2 regions defined by the line."
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Create a map with the lp.rect field."
    End Sub
    Private Function fillTriangle(lp As lpData, p1 As cv.Point) As Boolean
        Dim val = dst3.Get(Of Byte)(p1.Y, p1.X)
        If val > 0 Then
            dst3.FloodFill(p1, 255)
            Return True
        End If
        Return False
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim mmList As New List(Of mmData)
        Dim pad = 5
        dst3.SetTo(0)
        For Each lp In task.lines.lpList
            dst3.Line(lp.p1, lp.p2, lp.index + 1, task.lineWidth * 3, cv.LineTypes.Link8)
        Next
        labels(2) = CStr(task.lines.lpList.Count) + " non-overlapping lines were found."

        dst2 = Palettize(dst3, 0)

        Dim index = dst3.Get(Of Byte)(task.mouseMovePoint.Y, task.mouseMovePoint.X) - 1
        If task.lines.lpList.Count > 0 Then
            If index <= 0 Then
                If task.lpD Is Nothing Then task.lpD = task.lines.lpList(0)
            Else
                task.lpD = task.lines.lpList(index)
            End If
            task.lpD.displayCell(dst1)
        End If
    End Sub
End Class




Public Class Line_LeftRight : Inherits TaskParent
    Dim linesLeft As New Line_Basics_TA
    Dim linesRight As New Line_Basics_TA
    Dim stableLR As New StableGray_LeftRight
    Public Sub New()
        desc = "Find the lines in the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        stableLR.Run(emptyMat)

        Dim lpList As New List(Of lpData)
        If task.Settings.cameraName.Contains("StereoLabs") = False Then
            linesLeft.Run(task.leftView)
            lpList = linesLeft.lpList
            dst2 = linesLeft.dst2
        Else
            dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            lpList = task.lines.lpList
        End If

        For Each lp In task.lines.lpList
            dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth + 1, task.lineType)
        Next
        labels(2) = task.lines.labels(2)

        linesRight.Run(stableLR.dst3)
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each lp In linesRight.lpList
            dst3.Line(lp.p1, lp.p2, lp.color, task.lineWidth + 1, task.lineType)
        Next
        labels(3) = linesRight.labels(2)
    End Sub
End Class




Public Class Line_BasicsNoMotion1 : Inherits TaskParent
    Implements IDisposable
    Public lpList As New List(Of lpData)
    Dim ld As cv.XImgProc.FastLineDetector
    Public removeOverlappingLines As Boolean = True
    Public overLappingCount As Integer
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        If standalone Then task.gOptions.showMotionMask.Checked = True
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "NOT Working.  The test using dst1 is not reliable."
    End Sub
    Public Shared Function getRawLines(lines As cv.Vec4f()) As List(Of lpData)
        Dim lpList As New List(Of lpData)
        For Each v In lines
            If v(0) >= 0 And v(0) <= task.workRes.Width And v(1) >= 0 And v(1) <= task.workRes.Height And
                           v(2) >= 0 And v(2) <= task.workRes.Width And v(3) >= 0 And v(3) <= task.workRes.Height Then
                Dim p1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim p2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                If p1.X >= 0 And p1.X < task.workRes.Width And p1.Y >= 0 And p1.Y < task.workRes.Height And
                               p2.X >= 0 And p2.X < task.workRes.Width And p2.Y >= 0 And p2.Y < task.workRes.Height Then
                    p1 = lpData.validatePoint(p1)
                    p2 = lpData.validatePoint(p2)
                    Dim lp = New lpData(p1, p2)
                    If lp.pVec1(2) > 0 And lp.pVec2(2) > 0 Then lpList.Add(lp)
                End If
            End If
        Next
        Return lpList
    End Function
    Public Function getRawVecs(src As cv.Mat) As cv.Vec4f()
        ' task.lines is always going to present.  Reuse the stateless lp detector.
        Return ld.Detect(src)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray.Clone
        dst2 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If lpList.Count <= 1 Then lpList = getRawLines(ld.Detect(src))

        Static lastLineImage As cv.Mat = dst1.Clone
        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each lp In lpList
            Dim val = lastLineImage.Get(Of Byte)(lp.ptCenter.Y, lp.ptCenter.X)
            If val = lp.index + 1 Then
                lp.age += 1
                sortlines.Add(lp.length, lp)
            End If
        Next
        Dim count As Integer = sortlines.Count

        lpList = getRawLines(ld.Detect(src))

        For Each lp In lpList
            Dim val = lastLineImage.Get(Of Byte)(lp.ptCenter.Y, lp.ptCenter.X)
            If val = 0 Then sortlines.Add(lp.length, lp)
        Next
        Dim newCount As Integer = sortlines.Count - count

        lpList.Clear()
        overLappingCount = 0
        dst1.SetTo(0)
        For Each lp In sortlines.Values
            lp.index = lpList.Count
            If removeOverlappingLines Then
                If lp.rect.Width = 0 Then Continue For
                If lp.rect.Height = 0 Then Continue For
                If dst1(lp.rect).CountNonZero > 0 Then
                    overLappingCount += 1
                    Continue For
                End If
            End If
            dst1.Line(lp.p1, lp.p2, lp.index + 1, task.lineWidth, cv.LineTypes.Link4)
            dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth + 1, task.lineType)
            lpList.Add(lp)
        Next

        dst3 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

        If lpList.Count > 0 Then
            If task.lpD.rect.Width = 0 Then task.lpD = lpList(0)
        End If

        lastLineImage = dst1.Clone

        labels(2) = CStr(count) + " lines retained - " + CStr(newCount) + " were new"
        If removeOverlappingLines Then labels(2) += ". " + CStr(overLappingCount) + " overlap(s) removed."
    End Sub
    Protected Overrides Sub Finalize()
        ld.Dispose()
    End Sub
End Class





Public Class Line_BasicsNoMotion : Inherits TaskParent
    Dim lines As New Line_Basics_TA
    Public Sub New()
        desc = "Ignore motion when finding the lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray.Clone

        lines.motionMask.SetTo(255) ' every pixel has motion...
        lines.Run(src)
        dst2 = lines.dst2
        labels(2) = lines.labels(2)
    End Sub
End Class





Public Class Line_TranslatedRightView : Inherits TaskParent
    Dim lines As New Line_Basics_TA
    Public lpListRight As New List(Of lpData)
    Public Sub New()
        desc = "Translate lines from the color (left for ZED) image to the right image.."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray.Clone
        dst2 = task.lines.dst2
        labels(2) = task.lines.labels(2)

        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        lines.Run(src) ' we could use to validate the lines that are translated from the left view.

        lpListRight.Clear()
        Dim pt1 As cv.Point, pt2 As cv.Point
        For Each lp In task.lines.lpList
            Dim depth1 = task.pcSplit(2).Get(Of Single)(lp.p1.Y, lp.p1.X)
            If depth1 = 0 Then
                Dim r = task.gridRects(lp.p1GridIndex)
                depth1 = task.pcSplit(2)(r).Mean(task.depthmask(r))
            End If
            Dim depth2 = task.pcSplit(2).Get(Of Single)(lp.p2.Y, lp.p2.X)
            If depth2 = 0 Then
                Dim r = task.gridRects(lp.p2GridIndex)
                depth2 = task.pcSplit(2)(r).Mean(task.depthmask(r))
            End If
            If depth1 = 0 Or depth2 = 0 Then Continue For

            pt1 = lp.p1
            pt1.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth1
            If pt1.X < 0 Or pt1.X >= dst2.Width Then Continue For

            pt2 = lp.p2
            pt2.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth2
            If pt2.X < 0 Or pt2.X >= dst2.Width Then Continue For

            Dim lpR As New lpData(pt1, pt2)
            dst3.Line(lpR.p1, lpR.p2, lp.color, task.lineWidth + 1, task.lineType)
            lpListRight.Add(lpR)
        Next
        labels(3) = CStr(lpListRight.Count) + " lines were translated from the left image to the right image."
    End Sub
End Class





Public Class Line_EdgeLineCompare : Inherits TaskParent
    Dim edgeLine As New EdgeLine_Basics
    Public Sub New()
        labels(3) = "Lines where edgeline_basics and line_basics_TA agree."
        desc = "Compare the output of EdgeLine_Basics and Line_Basics_TA"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edgeLine.Run(src)
        dst2 = edgeLine.dst2
        labels(2) = edgeLine.labels(2)

        dst3.SetTo(0)
        dst2.CopyTo(dst3, task.lines.dst3)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class





Public Class Line_Longest : Inherits TaskParent
    Public Sub New()
        desc = "Compare the longest lines of the current and previous image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lpLast As lpData = task.lines.lpList(0)
        dst2 = task.color.Clone

        Dim lp = task.lines.lpList(0)
        dst2.Line(lp.pE1, lp.pE2, task.highlight, task.lineWidth + 3)

        Dim distance1 As Single, distance2 As Single
        If lp.pE1.Y = 0 Then
            distance1 = lp.pE1.X - lpLast.pE1.X
            distance2 = lp.pE2.X - lpLast.pE2.X
        End If
        If lp.pE1.Y = 0 Then
            distance1 = lp.pE1.X - lpLast.pE1.X
            distance2 = lp.pE2.X - lpLast.pE2.X
        End If
        ' Debug.WriteLine("distance1 = " + Format(distance1, fmt2) + " distance2 = " + Format(distance2, fmt2))
        If distance1 <> 0 Or distance2 <> 0 Then Dim k = 0
        lpLast = task.lines.lpList(0)
    End Sub
End Class






Public Class Line_KNNTop : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "The same lines in their location in the previous frame."
        desc = "Find all the lines that intersect the top and bottom of the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat = False Then
            SetTrueText(strOut, 1)
            Exit Sub
        End If
        dst2 = task.color.Clone
        dst3 = task.color.Clone

        knn.queries.Clear()
        Dim lpList As New List(Of lpData)
        For Each lp In task.lines.lpList
            If lp.pE1.Y = 0 And lp.pE2.Y = dst2.Height - 1 Then
                knn.queries.Add(New cv.Point2f(lp.p1.X, lp.p2.X))
                lpList.Add(lp)
                If lpList.Count > 3 Then Exit For
            End If
        Next

        If task.firstPass Then knn.trainInput = New List(Of cv.Point2f)(knn.queries)

        knn.Run(emptyMat)

        Dim tops As New List(Of Single)
        Dim bots As New List(Of Single)
        strOut = "Index" + vbTab + "Before X" + vbTab + "After X" + vbCrLf
        For i = 0 To knn.queries.Count - 1
            Dim p1 = knn.queries(i)
            Dim p2 = knn.trainInput(knn.result(i, 1))

            Dim d1 = p1.X - p2.X
            Dim d2 = p1.Y - p2.Y

            'If Math.Abs(d1) < 5 And Math.Abs(d2) < 5 Then
            tops.Add(d1)
            bots.Add(d2)

            Dim lp1 = New lpData(New cv.Point2f(p1.X, 0), New cv.Point2f(p1.Y, dst2.Height))
            dst3.Line(lp1.p1, lp1.p2, white, task.lineWidth + 1)

            Dim lp2 = New lpData(New cv.Point2f(p2.X, 0), New cv.Point2f(p2.Y, dst2.Height))
            dst2.Line(lp2.p1, lp2.p2, task.highlight, task.lineWidth + 1)
            strOut += CStr(i) + vbTab + CStr(lp1.p1.X) + vbTab + CStr(lp1.p2.X) + vbCrLf
            strOut += CStr(i) + vbTab + CStr(lp2.p1.X) + vbTab + CStr(lp2.p2.X) + vbCrLf

            ' End If
        Next
        SetTrueText(strOut, 1)
        If tops.Count > 0 And bots.Count > 0 Then
            labels(2) = CStr(tops.Count) + " Top points have moved " + Format(tops.Average, fmt1) +
                        CStr(bots.Count) + " bottom points have moved " + Format(bots.Average, fmt1)
        End If

        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
    End Sub
End Class





Public Class Line_EdgeLine : Inherits TaskParent
    Dim edgeLine As New EdgeLine_Basics
    Dim lines As New Line_Basics_TA
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Search for lines in the EdgeLine output."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edgeLine.Run(task.gray)

        Static matList As New List(Of cv.Mat)
        matList.Add(edgeLine.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary))
        If matList.Count > 5 Then matList.RemoveAt(0)

        dst2.SetTo(0)
        For Each mat In matList
            dst2 = dst2 Or mat
        Next

        lines.Run(dst2)
        dst3 = lines.dst2
        labels(3) = lines.labels(2)
    End Sub
End Class






Public Class Line_FindSimple : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim side As Integer
    Dim pixels(side * side) As cv.Point
    Public Sub New()
        side = task.gOptions.GridSlider.Value
        ReDim pixels(side * side)
        desc = "Find lines within each brick."
    End Sub
    Public Shared Function testPixels(pixels() As cv.Point) As lpData
        Dim testX As Boolean = True
        Dim testY As Boolean = True

        For j = 1 To pixels.Count - 1
            If Math.Abs(pixels(j - 1).X - pixels(j).X) > 1 Then
                testX = False
                Exit For
            End If
        Next

        For j = 1 To pixels.Count - 1
            If Math.Abs(pixels(j - 1).Y - pixels(j).Y) > 1 Then
                testX = False
                Exit For
            End If
        Next
        If testX Or testY Then Return New lpData(pixels(0), pixels(pixels.Count - 1))
        Return Nothing
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.gray)
        dst2 = edges.dst2
        labels(2) = edges.labels(2)

        dst3.SetTo(0)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim pixelCount = dst2(r).CountNonZero
            If pixelCount = 0 Or pixelCount > 20 Then Continue For
            Dim pixelMat = dst2(r).FindNonZero

            pixelMat.GetArray(Of cv.Point)(pixels)
            If task.drawRect.Contains(r.TopLeft) Then Dim k = 0

            Dim lp = testPixels(pixels)
            If lp IsNot Nothing Then
                dst3(r).Line(lp.p1, lp.p2, task.highlight, task.lineWidth)
            End If
        Next
    End Sub
End Class






Public Class Line_FinderPlus : Inherits TaskParent
    Dim find As New Line_Finder
    Dim lines As New Line_Basics_TA
    Public Sub New()
        desc = "Find lines in the brickline_find output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        find.Run(task.gray)
        dst2 = find.dst3

        lines.Run(dst2)
        dst3 = lines.dst2
        labels(3) = lines.labels(2)
    End Sub
End Class






Public Class Line_Finder : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim side As Integer
    Dim pixels(side * side) As cv.Point
    Dim pNothing As New cv.Point(-1, -1)
    Public Sub New()
        side = task.gOptions.GridSlider.Value
        ReDim pixels(side * side)
        desc = "Find lines within each brick."
    End Sub
    Public Function findLines(pixels() As cv.Point) As List(Of lpData)
        Dim lpList As New List(Of lpData)
        Dim ptList As New List(Of cv.Point)
        For i = 1 To pixels.Count - 1
            If pixels(i - 1).X > 0 Or pixels(i).X > 0 Then
                If ptList.Count = 0 Then
                    ptList.Add(pixels(i - 1))
                Else
                    If Math.Abs(pixels(i).X - ptList.Last.X) <= 1 Then
                        ptList.Add(pixels(i))
                    ElseIf Math.Abs(pixels(i - 1).X - ptList.Last.X) <= 1 Then
                        ptList.Add(pixels(i - 1))
                    Else
                        lpList.Add(New lpData(ptList(0), ptList.Last))
                        ptList.Clear()
                    End If
                End If
            End If
        Next
        If ptList.Count Then lpList.Add(New lpData(ptList.First, ptList.Last))

        Return lpList
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.gray)
        dst2 = edges.dst2
        labels(2) = edges.labels(2)

        dst3.SetTo(0)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)

            Dim pixelCount = dst2(r).CountNonZero
            If pixelCount = 0 Or pixelCount > 20 Then Continue For
            If pixelCount < side Then Continue For

            Dim pixelMat = dst2(r).FindNonZero

            pixelMat.GetArray(Of cv.Point)(pixels)

            ' use this line for debugging...
            If task.drawRect.Contains(r.TopLeft) Then Dim k = 0

            Dim lpList = findLines(pixels)
            For Each lp In lpList
                dst3(r).Line(lp.p1, lp.p2, task.highlight, task.lineWidth)
            Next
        Next
    End Sub
End Class




Public Class Line_RedFlood : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim redFlood As New RedFlood_Basics
    Public Sub New()
        desc = "Use the edges as input to RedFlood."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.gray)
        dst3 = edges.dst2
        labels(3) = edges.labels(2)

        redFlood.Run(dst2)
        dst2 = redFlood.dst2
        labels(2) = redFlood.labels(2)
    End Sub
End Class
