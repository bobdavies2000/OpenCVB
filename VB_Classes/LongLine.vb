Imports cvb = OpenCvSharp
Public Class LongLine_Basics : Inherits VB_Parent
    Public lines As New LongLine_Core
    Public lpList As New List(Of PointPair)
    Dim options As New Options_LongLine
    Public Sub New()
        lines.lineCount = 1000
        desc = "Identify the longest lines"
    End Sub
    Public Function BuildLongLine(lp As PointPair) As PointPair
        If lp.p1.X <> lp.p2.X Then
            Dim b = lp.p1.Y - lp.p1.X * lp.slope
            If lp.p1.Y = lp.p2.Y Then
                Return New PointPair(New cvb.Point(0, lp.p1.Y), New cvb.Point(dst2.Width, lp.p1.Y))
            Else
                Dim xint1 = CInt(-b / lp.slope)
                Dim xint2 = CInt((dst2.Height - b) / lp.slope)
                Dim yint1 = CInt(b)
                Dim yint2 = CInt(lp.slope * dst2.Width + b)

                Dim points As New List(Of cvb.Point)
                If xint1 >= 0 And xint1 <= dst2.Width Then points.Add(New cvb.Point(xint1, 0))
                If xint2 >= 0 And xint2 <= dst2.Width Then points.Add(New cvb.Point(xint2, dst2.Height))
                If yint1 >= 0 And yint1 <= dst2.Height Then points.Add(New cvb.Point(0, yint1))
                If yint2 >= 0 And yint2 <= dst2.Height Then points.Add(New cvb.Point(dst2.Width, yint2))
                Return New PointPair(points(0), points(1))
            End If
        End If
        Return New PointPair(New cvb.Point(lp.p1.X, 0), New cvb.Point(lp.p1.X, dst2.Height))
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = src.Clone
        lines.Run(src)

        lpList.Clear()
        For Each lp In lines.lpList
            lp = BuildLongLine(lp)
            DrawLine(dst2, lp.p1, lp.p2, cvb.Scalar.White)
            If lp.p1.X > lp.p2.X Then lp = New PointPair(lp.p2, lp.p1)
            lpList.Add(lp)
            If lpList.Count >= options.maxCount Then Exit For
        Next

        labels(2) = $"{lines.lpList.Count} lines found, longest {lpList.Count} displayed."
    End Sub
End Class







Public Class LongLine_Core : Inherits VB_Parent
    Public lines As New Line_Basics
    Public lineCount As Integer = 1 ' How many of the longest lines...
    Public lpList As New List(Of PointPair) ' this will be sorted by length - longest first
    Public Sub New()
        desc = "Isolate the longest X lines."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lines.Run(src)
        dst2 = lines.dst2
        If lines.lpList.Count = 0 Then Exit Sub

        dst2 = src
        lpList.Clear()
        For Each lp In lines.lpList
            lpList.Add(lp)
            DrawLine(dst2, lp.p1, lp.p2, task.HighlightColor)
            If lpList.Count >= lineCount Then Exit For
        Next
    End Sub
End Class





Public Class LongLine_Depth : Inherits VB_Parent
    Dim longLine As New LongLine_Consistent
    Dim plot As New Plot_OverTimeScalar
    Dim kalman As New Kalman_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        dst0 = New cvb.Mat(dst0.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        plot.dst2 = dst3
        desc = "Find the longest line in BGR and use it to measure the average depth for the line"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        longLine.Run(src.Clone)
        dst1 = src

        dst1.Line(longLine.ptLong.p1, longLine.ptLong.p2, cvb.Scalar.Yellow, task.lineWidth + 2, task.lineType)

        dst0.SetTo(0)
        dst0.Line(longLine.ptLong.p1, longLine.ptLong.p2, 255, 3, task.lineType)
        dst0.SetTo(0, task.noDepthMask)

        Dim mm As mmData = GetMinMax(task.pcSplit(2), dst0)

        kalman.kInput = {mm.minLoc.X, mm.minLoc.Y, mm.maxLoc.X, mm.maxLoc.Y}
        kalman.Run(empty)
        mm.minLoc = New cvb.Point(kalman.kOutput(0), kalman.kOutput(1))
        mm.maxLoc = New cvb.Point(kalman.kOutput(2), kalman.kOutput(3))

        DrawCircle(dst1, mm.minLoc, task.DotSize, cvb.Scalar.Red)
        DrawCircle(dst1, mm.maxLoc, task.DotSize, cvb.Scalar.Blue)
        SetTrueText(Format(mm.minVal, fmt1) + "m", New cvb.Point(mm.minLoc.X + 5, mm.minLoc.Y), 1)
        SetTrueText(Format(mm.maxVal, fmt1) + "m", New cvb.Point(mm.maxLoc.X + 5, mm.maxLoc.Y), 1)

        Dim depth = task.pcSplit(2).Mean(dst0)(0)

        SetTrueText("Average Depth = " + Format(depth, fmt1) + "m", New cvb.Point((longLine.ptLong.p1.X + longLine.ptLong.p2.X) / 2 + 30,
                                                                                 (longLine.ptLong.p1.Y + longLine.ptLong.p2.Y) / 2), 1)

        labels(3) = "Mean (blue)/Min (green)/Max (red) = " + Format(depth, fmt1) + "/" + Format(mm.minVal, fmt1) + "/" +
                    Format(mm.maxVal, fmt1) + " meters "

        plot.plotData = New cvb.Scalar(depth, mm.minVal, mm.maxVal)
        plot.Run(empty)
        dst2 = plot.dst2
        dst3 = plot.dst3
    End Sub
End Class









Public Class LongLine_Consistent : Inherits VB_Parent
    Dim longest As New LongLine_Core
    Public ptLong As PointPair
    Public Sub New()
        longest.lineCount = 4
        desc = "Isolate the line that is consistently among the longest lines present in the image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.Clone
        longest.Run(src)
        If longest.lpList.Count = 0 Then Exit Sub
        If ptLong Is Nothing Then ptLong = longest.lpList(0)

        Dim minDistance = Single.MaxValue
        Dim lpMin As PointPair
        For Each lp In longest.lpList
            Dim distance = lp.p1.DistanceTo(ptLong.p1) + lp.p2.DistanceTo(ptLong.p2)
            If distance < minDistance Then
                minDistance = distance
                lpMin = lp
            End If
        Next

        labels(2) = "minDistance = " + Format(minDistance, fmt1)
        DrawLine(dst2, ptLong.p1, ptLong.p2, task.HighlightColor)
        ptLong = lpMin
    End Sub
End Class









Public Class LongLine_Point : Inherits VB_Parent
    Dim longLine As New LongLine_Consistent
    Dim kalman As New Kalman_Basics
    Public longPt As cvb.Point
    Public Sub New()
        desc = "Isolate the line that is consistently among the longest lines present in the image and then kalmanize the mid-point"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        longLine.Run(src)
        dst2 = longLine.dst2

        Dim lp = longLine.ptLong
        kalman.kInput = {lp.p1.X, lp.p1.Y, lp.p2.X, lp.p2.Y}
        kalman.Run(empty)
        lp.p1 = New cvb.Point(kalman.kOutput(0), kalman.kOutput(1))
        lp.p2 = New cvb.Point(kalman.kOutput(2), kalman.kOutput(3))
        longPt = New cvb.Point((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2)

        DrawCircle(dst2, longPt, task.DotSize, cvb.Scalar.Red)
    End Sub
End Class





Public Class LongLine_Match : Inherits VB_Parent
    Dim longest As New LongLine_Consistent
    Dim options As New Options_LongLine
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_32F, cvb.Scalar.All(0))
        desc = "Find the longest line from last image and use matchTemplate to find the line in the latest image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        longest.Run(src)
        dst2 = longest.dst2

        Dim lp = longest.ptLong

        Dim x1 = Math.Min(lp.p1.X - options.pad, lp.p2.X - options.pad), x2 = Math.Max(lp.p1.X + options.pad, lp.p2.X + options.pad)
        Dim y1 = Math.Min(lp.p1.Y - options.pad, lp.p2.Y - options.pad), y2 = Math.Max(lp.p1.Y + options.pad, lp.p2.Y + options.pad)
        Dim rect = ValidateRect(New cvb.Rect(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x1 - x2), Math.Abs(y1 - y2)))
        dst2.Rectangle(rect, task.HighlightColor, task.lineWidth)

        Static template As cvb.Mat = src(rect).Clone
        cvb.Cv2.MatchTemplate(template, src, dst0, cvb.TemplateMatchModes.CCoeffNormed)
        Dim mm As mmData = GetMinMax(dst0)

        mm.maxLoc = New cvb.Point(mm.maxLoc.X + rect.Width / 2, mm.maxLoc.Y + rect.Height / 2)
        DrawCircle(dst2, mm.maxLoc, task.DotSize, cvb.Scalar.Red)

        dst3.SetTo(0)
        dst0 = dst0.Normalize(0, 255, cvb.NormTypes.MinMax)
        dst0.CopyTo(dst3(New cvb.Rect((dst3.Width - dst0.Width) / 2, (dst3.Height - dst0.Height) / 2, dst0.Width, dst0.Height)))
        DrawCircle(dst3, mm.maxLoc, task.DotSize, 255)

        template = src(rect).Clone
    End Sub
End Class







Public Class LongLine_ExtendTest : Inherits VB_Parent
    Dim longLine As New LongLine_Basics
    Public Sub New()
        labels = {"", "", "Random Line drawn", ""}
        desc = "Test PointPair constructor with random values to make sure lines are extended properly"
    End Sub

    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            Dim p1 = New cvb.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p2 = New cvb.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))

            Dim mps = New PointPair(p1, p2)
            Dim emps = longLine.BuildLongLine(mps)
            dst2 = src
            DrawLine(dst2, emps.p1, emps.p2, task.HighlightColor)
            DrawCircle(dst2, p1, task.DotSize + 2, cvb.Scalar.Red)
            DrawCircle(dst2, p2, task.DotSize + 2, cvb.Scalar.Red)
        End If
    End Sub
End Class








Public Class LongLine_ExtendAll : Inherits VB_Parent
    Public lines As New Line_Basics
    Public lpList As New List(Of PointPair)
    Public Sub New()
        labels = {"", "", "Image output from Line_Basics", "The extended line for each line found in Line_Basics"}
        desc = "Create a list of all the extended lines in an image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        dst3 = src.Clone
        lpList.Clear()
        For Each lp In lines.lpList
            lpList.Add(lp)
            DrawLine(dst3, lp.p1, lp.p2, task.HighlightColor)
        Next
    End Sub
End Class






Public Class LongLine_ExtendParallel : Inherits VB_Parent
    Dim extendAll As New LongLine_ExtendAll
    Dim knn As New KNN_Core
    Dim near As New Line_Nearest
    Public parList As New List(Of coinPoints)
    Public Sub New()
        labels = {"", "", "Image output from Line_Basics", "Parallel extended lines"}
        desc = "Use KNN to find which lines are near each other and parallel"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        extendAll.Run(src)
        dst3 = extendAll.dst2

        knn.queries.Clear()
        For Each lp In extendAll.lpList
            knn.queries.Add(New cvb.Point2f((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2))
        Next
        knn.trainInput = New List(Of cvb.Point2f)(knn.queries)

        If knn.queries.Count = 0 Then Exit Sub ' no input...possible in a dark room...

        knn.Run(empty)
        dst2 = src.Clone
        parList.Clear()
        Dim checkList As New List(Of cvb.Point)
        For i = 0 To knn.result.GetUpperBound(0) - 1
            For j = 0 To knn.queries.Count - 1
                Dim index = knn.result(i, j)
                If index >= extendAll.lpList.Count Or index < 0 Then Continue For
                Dim lp = extendAll.lpList(index)
                Dim elp = extendAll.lpList(i)
                Dim mid = knn.queries(i)
                Dim near = knn.trainInput(index)
                Dim distanceMid = mid.DistanceTo(near)
                Dim distance1 = lp.p1.DistanceTo(elp.p1)
                Dim distance2 = lp.p2.DistanceTo(elp.p2)
                If distance1 > distanceMid * 2 Then
                    distance1 = lp.p1.DistanceTo(elp.p2)
                    distance2 = lp.p2.DistanceTo(elp.p1)
                End If
                If distance1 < distanceMid * 2 And distance2 < distanceMid * 2 Then
                    Dim cp As coinPoints

                    Dim mps = extendAll.lines.lpList(index)
                    cp.p1 = mps.p1
                    cp.p2 = mps.p2

                    mps = extendAll.lines.lpList(i)
                    cp.p3 = mps.p1
                    cp.p4 = mps.p2

                    If checkList.Contains(cp.p1) = False And checkList.Contains(cp.p2) = False And checkList.Contains(cp.p3) = False And checkList.Contains(cp.p4) = False Then
                        If (cp.p1 = cp.p3 Or cp.p1 = cp.p4) And (cp.p2 = cp.p3 Or cp.p2 = cp.p4) Then
                            ' duplicate points...
                        Else
                            DrawLine(dst2, cp.p1, cp.p2, task.HighlightColor)
                            DrawLine(dst2, cp.p3, cp.p4, cvb.Scalar.Red)
                            parList.Add(cp)
                            checkList.Add(cp.p1)
                            checkList.Add(cp.p2)
                            checkList.Add(cp.p3)
                            checkList.Add(cp.p4)
                        End If
                    End If
                End If
            Next
        Next
        labels(2) = CStr(parList.Count) + " parallel lines were found in the image"
        labels(3) = CStr(extendAll.lpList.Count) + " lines were found in the image before finding the parallel lines"
    End Sub
End Class








Public Class LongLine_Extend : Inherits VB_Parent
    Dim lines As New LongLine_Basics
    Dim saveP1 As cvb.Point, saveP2 As cvb.Point, p1 As cvb.Point, p2 As cvb.Point
    Public Sub New()
        labels = {"", "", "Original Line", "Original line Extended"}
        desc = "Given 2 points, extend the line to the edges of the image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() And task.heartBeat Then
            p1 = New cvb.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p2 = New cvb.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            saveP1 = p1
            saveP2 = p2
        End If

        Dim mps = New PointPair(p1, p2)
        Dim emps = lines.BuildLongLine(mps)

        If standaloneTest() Then
            labels(2) = emps.p1.ToString + " and " + emps.p2.ToString + " started with " + saveP1.ToString + " and " + saveP2.ToString
            dst2 = src
            DrawLine(dst2, emps.p1, emps.p2, task.HighlightColor)
            DrawCircle(dst2,saveP1, task.DotSize, cvb.Scalar.Red)
            DrawCircle(dst2,saveP2, task.DotSize, cvb.Scalar.Red)
        End If
    End Sub
End Class







Public Class LongLine_NoDepth : Inherits VB_Parent
    Dim lineHist As New LineCoin_Basics
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Find any lines in regions without depth."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lineHist.Run(src)
        dst2 = lineHist.dst2
        dst2.SetTo(0, task.depthMask)
    End Sub
End Class










Public Class LongLine_History : Inherits VB_Parent
    Dim lines As New LongLine_Basics
    Public lpList As New List(Of PointPair)
    Dim mpList As New List(Of List(Of PointPair))
    Public Sub New()
        desc = "Find the longest lines and toss any that are intermittant."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        mpList.Add(lines.lpList)

        Dim tmplist As New List(Of PointPair)
        Dim lpCount As New List(Of Integer)
        For Each list In mpList
            For Each lp In list
                Dim index = tmplist.IndexOf(lp)
                If index < 0 Then
                    tmplist.Add(lp)
                    lpCount.Add(1)
                Else
                    lpCount(index) += 1
                End If
            Next
        Next

        lpList.Clear
        For i = 0 To lpCount.Count - 1
            Dim count = lpCount(i)
            If count >= task.frameHistoryCount Then lpList.Add(tmplist(i))
        Next

        For Each lp In lpList
            DrawLine(dst2, lp.p1, lp.p2, cvb.Scalar.White)
        Next
        If mpList.Count > task.gOptions.FrameHistory.Value Then mpList.RemoveAt(0)

        labels(2) = $"{lpList.Count} were found that were present for every one of the last {task.frameHistoryCount} frames."
    End Sub
End Class
