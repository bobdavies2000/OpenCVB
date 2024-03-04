Imports cv = OpenCvSharp
Public Class LongLine_Basics : Inherits VB_Algorithm
    Public lines As New Line_Basics
    Public lineCount As Integer = 1 ' How many of the longest lines...
    Public p1List As New List(Of cv.Point) ' p1 is always the leftmost of the matching p2 (from line_basics)
    Public p2List As New List(Of cv.Point)
    Public Sub New()
        desc = "Isolate the longest X lines."
    End Sub
    Public Function buildELine(mps As pointPair, width As Integer, height As Integer) As pointPair
        If mps.p1.X <> mps.p2.X Then
            Dim b = mps.p1.Y - mps.p1.X * mps.slope
            If mps.p1.Y = mps.p2.Y Then
                Return New pointPair(New cv.Point(0, mps.p1.Y), New cv.Point(width, mps.p1.Y))
            Else
                Dim xint1 = CInt(-b / mps.slope)
                Dim xint2 = CInt((height - b) / mps.slope)
                Dim yint1 = CInt(b)
                Dim yint2 = CInt(mps.slope * width + b)

                Dim points As New List(Of cv.Point)
                If xint1 >= 0 And xint1 <= width Then points.Add(New cv.Point(xint1, 0))
                If xint2 >= 0 And xint2 <= width Then points.Add(New cv.Point(xint2, height))
                If yint1 >= 0 And yint1 <= height Then points.Add(New cv.Point(0, yint1))
                If yint2 >= 0 And yint2 <= height Then points.Add(New cv.Point(width, yint2))
                Return New pointPair(points(0), points(1))
            End If
        End If
        Return New pointPair(New cv.Point(mps.p1.X, 0), New cv.Point(mps.p1.X, height))
    End Function
    Public Sub RunVB(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2
        If lines.sortLength.Count = 0 Then Exit Sub

        dst2 = src
        p1List.Clear()
        p2List.Clear()
        For i = 0 To Math.Min(lineCount, lines.sortLength.Count) - 1
            Dim index = lines.sortLength.ElementAt(i).Value
            Dim mps = lines.mpList(index)
            dst2.Line(mps.p1, mps.p2, task.highlightColor, task.lineWidth, task.lineType)
            p1List.Add(mps.p1)
            p2List.Add(mps.p2)
        Next
    End Sub
End Class





Public Class LongLine_Depth : Inherits VB_Algorithm
    Dim longLine As New LongLine_Consistent
    Dim plot As New Plot_OverTimeScalar
    Dim kalman As New Kalman_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        plot.dst2 = dst3
        desc = "Find the longest line in BGR and use it to measure the average depth for the line"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        longLine.Run(src.Clone)
        dst1 = src

        dst1.Line(longLine.longP1, longLine.longP2, cv.Scalar.Yellow, task.lineWidth + 2, task.lineType)

        dst0.SetTo(0)
        dst0.Line(longLine.longP1, longLine.longP2, 255, 3, task.lineType)
        dst0.SetTo(0, task.noDepthMask)

        Dim mm As mmData = vbMinMax(task.pcSplit(2), dst0)

        kalman.kInput = {mm.minLoc.X, mm.minLoc.Y, mm.maxLoc.X, mm.maxLoc.Y}
        kalman.Run(empty)
        mm.minLoc = New cv.Point(kalman.kOutput(0), kalman.kOutput(1))
        mm.maxLoc = New cv.Point(kalman.kOutput(2), kalman.kOutput(3))

        dst1.Circle(mm.minLoc, task.dotSize, cv.Scalar.Red, task.lineWidth, task.lineType)
        dst1.Circle(mm.maxLoc, task.dotSize, cv.Scalar.Blue, task.lineWidth, task.lineType)
        setTrueText(Format(mm.minVal, fmt1) + "m", New cv.Point(mm.minLoc.X + 5, mm.minLoc.Y), 1)
        setTrueText(Format(mm.maxVal, fmt1) + "m", New cv.Point(mm.maxLoc.X + 5, mm.maxLoc.Y), 1)

        Dim depth = task.pcSplit(2).Mean(dst0)(0)

        setTrueText("Average Depth = " + Format(depth, fmt1) + "m", New cv.Point((longLine.longP1.X + longLine.longP2.X) / 2 + 30,
                                                                                 (longLine.longP1.Y + longLine.longP2.Y) / 2), 1)

        labels(3) = "Mean (blue)/Min (green)/Max (red) = " + Format(depth, fmt1) + "/" + Format(mm.minVal, fmt1) + "/" +
                    Format(mm.maxVal, fmt1) + " meters "

        plot.plotData = New cv.Scalar(depth, mm.minVal, mm.maxVal)
        plot.Run(empty)
        dst2 = plot.dst2
        dst3 = plot.dst3
    End Sub
End Class









Public Class LongLine_Consistent : Inherits VB_Algorithm
    Dim longest As New LongLine_Basics
    Public longP1 As cv.Point
    Public longP2 As cv.Point
    Public Sub New()
        longest.lineCount = 4
        desc = "Isolate the line that is consistently among the longest lines present in the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.Clone
        longest.Run(src)
        If longest.p1List.Count = 0 Then Exit Sub
        If longP1 = New cv.Point Then longP1 = longest.p1List(0)
        If longP2 = New cv.Point Then longP2 = longest.p2List(0)

        Dim minDistance = Single.MaxValue
        Dim minIndex As Integer
        For i = 0 To longest.p1List.Count - 1
            Dim p1 = longest.p1List(i)
            Dim p2 = longest.p2List(i)

            Dim distance = p1.DistanceTo(longP1) + p2.DistanceTo(longP2)
            If distance < minDistance Then
                minDistance = distance
                minIndex = i
            End If
        Next

        labels(2) = "minDistance = " + Format(minDistance, fmt1)
        dst2.Line(longP1, longP2, task.highlightColor, task.lineWidth, task.lineType)
        longP1 = longest.p1List(minIndex)
        longP2 = longest.p2List(minIndex)
    End Sub
End Class









Public Class LongLine_Point : Inherits VB_Algorithm
    Dim longLine As New LongLine_Consistent
    Dim kalman As New Kalman_Basics
    Public longPt As cv.Point
    Public Sub New()
        desc = "Isolate the line that is consistently among the longest lines present in the image and then kalmanize the mid-point"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        longLine.Run(src)
        dst2 = longLine.dst2

        Dim p1 = longLine.longP1
        Dim p2 = longLine.longP2
        kalman.kInput = {p1.X, p1.Y, p2.X, p2.Y}
        kalman.Run(empty)
        p1 = New cv.Point(kalman.kOutput(0), kalman.kOutput(1))
        p2 = New cv.Point(kalman.kOutput(2), kalman.kOutput(3))
        longPt = New cv.Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2)

        dst2.Circle(longPt, task.dotSize, cv.Scalar.Red, -1, task.lineType)
    End Sub
End Class





Public Class LongLine_Match : Inherits VB_Algorithm
    Dim longest As New LongLine_Consistent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Reduction for width/height in pixels", 1, 20, 3)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32F, 0)
        desc = "Find the longest line from last image and use matchTemplate to find the line in the latest image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static searchSlider = findSlider("Reduction for width/height in pixels")
        Dim pad = searchSlider.Value

        longest.Run(src)
        dst2 = longest.dst2

        Dim p1 = longest.longP1
        Dim p2 = longest.longP2

        Dim x1 = Math.Min(p1.X - pad, p2.X - pad), x2 = Math.Max(p1.X + pad, p2.X + pad)
        Dim y1 = Math.Min(p1.Y - pad, p2.Y - pad), y2 = Math.Max(p1.Y + pad, p2.Y + pad)
        Dim rect = validateRect(New cv.Rect(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x1 - x2), Math.Abs(y1 - y2)))
        dst2.Rectangle(rect, task.highlightColor, task.lineWidth)

        Static template As cv.Mat = src(rect).Clone
        cv.Cv2.MatchTemplate(template, src, dst0, cv.TemplateMatchModes.CCoeffNormed)
        Dim mm As mmData = vbMinMax(dst0)

        mm.maxLoc = New cv.Point(mm.maxLoc.X + rect.Width / 2, mm.maxLoc.Y + rect.Height / 2)
        dst2.Circle(mm.maxLoc, task.dotSize, cv.Scalar.Red, -1, task.lineType)

        dst3.SetTo(0)
        dst0 = dst0.Normalize(0, 255, cv.NormTypes.MinMax)
        dst0.CopyTo(dst3(New cv.Rect((dst3.Width - dst0.Width) / 2, (dst3.Height - dst0.Height) / 2, dst0.Width, dst0.Height)))
        dst3.Circle(mm.maxLoc, task.dotSize, 255, -1, task.lineType)

        template = src(rect).Clone
    End Sub
End Class







Public Class LongLine_ExtendTest : Inherits VB_Algorithm
    Dim longLine As New LongLine_Basics
    Public Sub New()
        labels = {"", "", "Random Line drawn", ""}
        desc = "Test pointPair constructor with random values to make sure lines are extended properly"
    End Sub

    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            Dim p1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))

            Dim mps = New pointPair(p1, p2)
            Dim emps = longLine.buildELine(mps, dst2.Width, dst2.Height)
            dst2 = src
            dst2.Line(emps.p1, emps.p2, task.highlightColor, task.lineWidth, task.lineType)
            dst2.Circle(p1, task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
            dst2.Circle(p2, task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
        End If
    End Sub
End Class








Public Class LongLine_ExtendAll : Inherits VB_Algorithm
    Public lines As New Line_Basics
    Dim extend As New LongLine_Extend
    Public p1List As New List(Of cv.Point)
    Public p2List As New List(Of cv.Point)
    Public Sub New()
        labels = {"", "", "Image output from Line_Basics", "The extended line for each line found in Line_Basics"}
        desc = "Create a list of all the extended lines in an image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        dst3 = src.Clone
        p1List.Clear()
        p2List.Clear()
        For Each mps In lines.mpList
            p1List.Add(mps.p1)
            p2List.Add(mps.p2)
            dst3.Line(mps.p1, mps.p2, task.highlightColor, task.lineWidth, task.lineType)
        Next
    End Sub
End Class






Public Class LongLine_ExtendParallel : Inherits VB_Algorithm
    Dim extendAll As New LongLine_ExtendAll
    Dim knn As New KNN_Core
    Dim near As New Line_Nearest
    Public parList As New List(Of cPoints)
    Public Sub New()
        labels = {"", "", "Image output from Line_Basics", "Parallel extended lines"}
        desc = "Use KNN to find which lines are near each other and parallel"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        extendAll.Run(src)
        dst3 = extendAll.dst2

        knn.queries.Clear()
        For i = 0 To extendAll.p1List.Count - 1
            Dim p1 = extendAll.p1List(i)
            Dim p2 = extendAll.p2List(i)
            knn.queries.Add(New cv.Point2f((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2))
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)

        If knn.queries.Count = 0 Then Exit Sub ' no input...possible in a dark room...

        knn.Run(empty)
        dst2 = src.Clone
        parList.Clear()
        Dim checkList As New List(Of cv.Point)
        For i = 0 To knn.result.GetUpperBound(0) - 1
            For j = 0 To knn.queries.Count - 1
                Dim index = knn.result(i, j)
                If index >= extendAll.p1List.Count Or index < 0 Then Continue For
                Dim p1 = extendAll.p1List(index)
                Dim p2 = extendAll.p2List(index)
                Dim e1 = extendAll.p1List(i)
                Dim e2 = extendAll.p2List(i)
                Dim mid = knn.queries(i)
                Dim near = knn.trainInput(index)
                Dim distanceMid = mid.DistanceTo(near)
                Dim distance1 = p1.DistanceTo(e1)
                Dim distance2 = p2.DistanceTo(e2)
                If distance1 > distanceMid * 2 Then
                    distance1 = p1.DistanceTo(e2)
                    distance2 = p2.DistanceTo(e1)
                End If
                If distance1 < distanceMid * 2 And distance2 < distanceMid * 2 Then
                    Dim cp As cPoints

                    Dim mps = extendAll.lines.mpList(index)
                    cp.p1 = mps.p1
                    cp.p2 = mps.p2

                    mps = extendAll.lines.mpList(i)
                    cp.p3 = mps.p1
                    cp.p4 = mps.p2

                    If checkList.Contains(cp.p1) = False And checkList.Contains(cp.p2) = False And checkList.Contains(cp.p3) = False And checkList.Contains(cp.p4) = False Then
                        If (cp.p1 = cp.p3 Or cp.p1 = cp.p4) And (cp.p2 = cp.p3 Or cp.p2 = cp.p4) Then
                            ' duplicate points...
                        Else
                            dst2.Line(cp.p1, cp.p2, task.highlightColor, task.lineWidth, task.lineType)
                            dst2.Line(cp.p3, cp.p4, cv.Scalar.Red, task.lineWidth, task.lineType)
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
        labels(3) = CStr(extendAll.p1List.Count) + " lines were found in the image before finding the parallel lines"
    End Sub
End Class









Public Class LongLine_Coincident : Inherits VB_Algorithm
    Dim parallel As New LongLine_ExtendParallel
    Dim near As New Line_Nearest
    Public coinList As New List(Of cPoints)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Max Distance to qualify as coincident", 0, 20, 10)
        desc = "Find the lines that are coincident in the parallel lines"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static distSlider = findSlider("Max Distance to qualify as coincident")
        Dim maxDistance = distSlider.Value
        parallel.Run(src)

        coinList.Clear()

        For Each cp In parallel.parList
            near.p1 = cp.p1
            near.p2 = cp.p2
            near.pt = cp.p3
            near.Run(empty)
            If near.distance1 < maxDistance Or near.distance2 < maxDistance Then
                coinList.Add(cp)
            Else
                near.pt = cp.p4
                near.Run(empty)
                If near.distance1 < maxDistance Or near.distance2 < maxDistance Then coinList.Add(cp)
            End If
        Next

        dst2 = src.Clone
        For Each cp In coinList
            dst2.Line(cp.p3, cp.p4, cv.Scalar.Red, task.lineWidth + 2, task.lineType)
            dst2.Line(cp.p1, cp.p2, task.highlightColor, task.lineWidth + 1, task.lineType)
        Next
        labels(2) = CStr(coinList.Count) + " coincident lines were detected"
    End Sub
End Class







Public Class LongLine_Extend : Inherits VB_Algorithm
    Dim lines As New LongLine_Basics
    Public Sub New()
        labels = {"", "", "Original Line", "Original line Extended"}
        desc = "Given 2 points, extend the line to the edges of the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static saveP1 As cv.Point, saveP2 As cv.Point, p1 As cv.Point, p2 As cv.Point
        If standaloneTest() And task.heartBeat Then
            p1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            saveP1 = p1
            saveP2 = p2
        End If

        Dim mps = New pointPair(p1, p2)
        Dim emps = lines.buildELine(mps, dst2.Width, dst2.Height)

        If standaloneTest() Then
            labels(2) = emps.p1.ToString + " and " + emps.p2.ToString + " started with " + saveP1.ToString + " and " + saveP2.ToString
            dst2 = src
            dst2.Line(emps.p1, emps.p2, task.highlightColor, task.lineWidth, task.lineType)
            dst2.Circle(saveP1, task.dotSize, cv.Scalar.Red, -1, task.lineType)
            dst2.Circle(saveP2, task.dotSize, cv.Scalar.Red, -1, task.lineType)
        End If
    End Sub
End Class







Public Class LongLine_NoDepth : Inherits VB_Algorithm
    Dim lineHist As New LineCoin_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Find any lines in regions without depth."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lineHist.Run(src)
        dst2 = lineHist.dst2
        dst2.SetTo(0, task.depthMask)
    End Sub
End Class







Public Class LongLine_HistoryIntercept : Inherits VB_Algorithm
    Dim coin As New LineCoin_Basics
    Public lpList As New List(Of pointPair)
    Public Sub New()
        dst2 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Collect lines over time"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static mpLists As New List(Of List(Of pointPair))
        If task.optionsChanged Then mpLists.Clear()

        coin.longLines.Run(src)

        mpLists.Add(coin.longLines.lines.mpList)

        coin.p1List.Clear()
        coin.p2List.Clear()
        coin.ptCounts.Clear()
        For Each mplist In mpLists
            coin.findLines(mplist)
        Next

        Dim historyCount = gOptions.FrameHistory.Value
        dst2.SetTo(0)
        Dim highlight As Integer
        lpList.Clear()
        For i = 0 To coin.p1List.Count - 1
            highlight = 128
            If coin.ptCounts(i) > historyCount Then highlight = 255
            If coin.ptCounts(i) >= historyCount Then
                dst2.Line(coin.p1List(i), coin.p2List(i), highlight, task.lineWidth, task.lineType)
                lpList.Add(New pointPair(coin.p1List(i), coin.p2List(i)))
            End If
        Next

        If mpLists.Count >= historyCount Then mpLists.RemoveAt(0)

        ' this is an OpAuto algorithm.
        Static llSlider = findSlider("Line length threshold in pixels")
        If lpList.Count > 50 Then llSlider.value += 1
        If lpList.Count < 30 And llSlider.value > 10 Then llSlider.value -= 1

        If standaloneTest() Then
            dst3 = src
            For Each lp In lpList
                dst3.Line(lp.p1, lp.p2, cv.Scalar.White, task.lineWidth, task.lineType)
            Next
        End If

        labels(2) = $"The {lpList.Count} lines below were present in each of the last " + CStr(historyCount) + " frames"
    End Sub
End Class







Public Class LongLine_Length : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Public lpList As New List(Of pointPair)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Number of lines to display", 0, 100, 25)
        findSlider("Line length threshold in pixels").Value = 1
        desc = "Identify the longest lines"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static countSlider = findSlider("Number of lines to display")
        Dim maxCount = countSlider.value

        lines.Run(src)

        dst2.SetTo(0)
        lpList.Clear()
        For Each ele In lines.sortByLen
            Dim lp = ele.Value
            dst2.Line(lp.p1, lp.p2, cv.Scalar.White, task.lineWidth, task.lineType)
            lpList.Add(lp)
            If lpList.Count >= maxCount Then Exit For
        Next

        labels(2) = $"{lines.sortByLen.Count} lines found, longest {lpList.Count} displayed."
    End Sub
End Class
