﻿Imports cv = OpenCvSharp
Public Class LongLine_DepthDirection : Inherits TaskParent
    Public gcUpdates As New List(Of Tuple(Of Integer, Single))
    Public Sub New()
        labels(2) = "Use the 'Features' option slider to select different lines."
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Display the direction of each line in depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lineRGB.dst3

        dst1.SetTo(0)
        gcUpdates.Clear()
        Dim avg1 As Single, avg2 As Single
        Dim debugmode = If(task.selectedFeature = 0, False, True)

        For Each lp In task.lineRGB.lpList
            If lp.bricks.Count = 0 Then Continue For
            If debugmode Then If task.selectedFeature <> lp.index Then Continue For

            Dim halfSum1 As New List(Of Single), halfsum2 As New List(Of Single)
            Dim halfCount As Integer = Math.Floor(If(lp.bricks.Count Mod 2 = 0, lp.bricks.Count, lp.bricks.Count - 1) / 2)
            Dim depthValues As New List(Of Single)
            For i = 0 To halfCount - 1
                Dim gc1 = task.bbo.brickList(lp.bricks(i))
                Dim gc2 = task.bbo.brickList(lp.bricks(lp.bricks.Count - i - 1))

                Dim d1 = gc1.depth
                Dim d2 = gc2.depth

                Dim p1 = gc1.rect.TopLeft
                Dim p2 = gc2.rect.TopLeft

                If gc1.correlation >= task.fCorrThreshold Then
                    halfSum1.Add(d1)
                    depthValues.Add(d1)
                    If debugmode Then SetTrueText(Format(d1, fmt3), New cv.Point(p1.X + 20, p1.Y), 3)
                End If

                If gc2.correlation >= task.fCorrThreshold Then
                    halfsum2.Add(d2)
                    depthValues.Add(d2)
                    If debugmode Then SetTrueText(Format(d2, fmt3), New cv.Point(p2.X - 20, p2.Y), 3)
                End If
            Next

            Dim incr = 255 / lp.bricks.Count
            Dim offset As Integer
            avg1 = If(halfSum1.Count > 0, halfSum1.Average, 0)
            avg2 = If(halfsum2.Count > 0, halfsum2.Average, 0)

            If avg1 < avg2 Then offset = lp.bricks.Count
            If Math.Abs(avg1 - avg2) < 0.01 Then ' task.depthDiffMeters Then
                For Each index In lp.bricks
                    Dim brick = task.bbo.brickList(index)
                    dst1(brick.rect).SetTo(1)
                    If debugmode Then dst2.Rectangle(brick.rect, task.highlight, task.lineWidth)
                    gcUpdates.Add(New Tuple(Of Integer, Single)(index, (avg1 + avg2) / 2))
                Next
            Else
                Dim min = If(depthValues.Count, depthValues.Min, 0)
                Dim max = If(depthValues.Count, depthValues.Max, 0)
                Dim depthIncr = (max - min) / lp.bricks.Count
                For i = 0 To lp.bricks.Count - 1
                    Dim index = lp.bricks(i)
                    Dim brick = task.bbo.brickList(index)
                    If offset > 0 Then
                        dst1(brick.rect).SetTo((offset - i + 1) * incr)
                        gcUpdates.Add(New Tuple(Of Integer, Single)(index, min + (offset - i) * depthIncr))
                    Else
                        dst1(brick.rect).SetTo(i * incr + 1)
                        gcUpdates.Add(New Tuple(Of Integer, Single)(index, min + i * depthIncr))
                    End If
                    If debugmode Then dst2.Rectangle(brick.rect, task.highlight, task.lineWidth)
                Next
            End If
        Next

        cv.Cv2.ApplyColorMap(dst1, dst0, task.depthColorMap)
        dst3 = src.Clone
        dst0.CopyTo(dst3, dst1)
        If debugmode Then
            If task.heartBeat Then
                labels(3) = "Average depth for first half = " + Format(avg1) + ", average depth for second half = " + Format(avg2, fmt1) + ", " +
                            "'All yellow' bricks indicate the line is likely a 3D vertical line."
            End If
        Else
            labels(3) = "Yellow is closer than blue but 'all yellow' is a likely vertical line in 3D where depths are within " +
                        CStr(task.depthDiffMeters) + "m of each other."
        End If
    End Sub
End Class






Public Class LongLine_Depth : Inherits TaskParent
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find the longest line in BGR and use it to measure the average depth for the line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lineRGB.lpList.Count <= 1 Then Exit Sub
        Dim lp = task.lineRGB.lpList(0)
        dst2 = src

        dst2.Line(lp.p1, lp.p2, cv.Scalar.Yellow, task.lineWidth + 3, task.lineType)

        Dim gcMin = task.bbo.brickList(task.bbo.brickMap.Get(Of Single)(lp.p1.Y, lp.p1.X))
        Dim gcMax = task.bbo.brickList(task.bbo.brickMap.Get(Of Single)(lp.p2.Y, lp.p2.X))

        dst0.SetTo(0)
        dst0.Line(lp.p1, lp.p2, 255, 3, task.lineType)
        dst0.SetTo(0, task.noDepthMask)

        Dim mm = GetMinMax(task.pcSplit(2), dst0)
        If gcMin.pt.DistanceTo(mm.minLoc) > gcMin.pt.DistanceTo(mm.maxLoc) Then
            Dim tmp = gcMin
            gcMin = gcMax
            gcMax = tmp
        End If

        Dim depthMin = If(gcMin.depth > 0, gcMin.depth, mm.minVal)
        Dim depthMax = If(gcMax.depth > 0, gcMax.depth, mm.maxVal)

        Dim depthMean = task.pcSplit(2).Mean(dst0)(0)
        DrawCircle(dst2, lp.p1, task.DotSize + 4, cv.Scalar.Red)
        DrawCircle(dst2, lp.p2, task.DotSize + 4, cv.Scalar.Blue)

        If lp.p1.DistanceTo(mm.minLoc) < lp.p2.DistanceTo(mm.maxLoc) Then
            mm.minLoc = lp.p1
            mm.maxLoc = lp.p2
        Else
            mm.minLoc = lp.p2
            mm.maxLoc = lp.p1
        End If

        If task.heartBeat Then
            SetTrueText("Average Depth = " + Format(depthMean, fmt1) + "m", New cv.Point((lp.p1.X + lp.p2.X) / 2,
                                                                                     (lp.p1.Y + lp.p2.Y) / 2), 2)
            labels(2) = "Min Distance = " + Format(depthMin, fmt1) + ", Max Distance = " + Format(depthMax, fmt1) +
                    ", Mean Distance = " + Format(depthMean, fmt1) + " meters "

            SetTrueText(Format(depthMin, fmt1) + "m", New cv.Point(mm.minLoc.X + 5, mm.minLoc.Y - 15), 2)
            SetTrueText(Format(depthMax, fmt1) + "m", New cv.Point(mm.maxLoc.X + 5, mm.maxLoc.Y - 15), 2)
        End If
    End Sub
End Class








Public Class LongLine_Point : Inherits TaskParent
    Public longPt As cv.Point
    Public Sub New()
        task.kalman = New Kalman_Basics
        desc = "Isolate the line that is consistently among the longest lines present in the image and then kalmanize the mid-point"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT Then dst2 = src
        If task.lineRGB.lpList.Count = 0 Then Exit Sub
        Dim lp = task.lineRGB.lpList(0)
        task.kalman.kInput = {lp.p1.X, lp.p1.Y, lp.p2.X, lp.p2.Y}
        task.kalman.Run(emptyMat)
        lp.p1 = New cv.Point(task.kalman.kOutput(0), task.kalman.kOutput(1))
        lp.p2 = New cv.Point(task.kalman.kOutput(2), task.kalman.kOutput(3))

        dst2.Line(lp.p1, lp.p2, cv.Scalar.Red, task.lineWidth)
    End Sub
End Class







Public Class LongLine_ExtendTest : Inherits TaskParent
    Dim longLine As New XO_LongLine_BasicsEx
    Public Sub New()
        labels = {"", "", "Random Line drawn", ""}
        desc = "Test lpData constructor with random values to make sure lines are extended properly"
    End Sub

    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            Dim p1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))

            Dim lp = New lpData(p1, p2)
            dst2 = src
            DrawLine(dst2, lp.ep1, lp.ep2, task.highlight)
            DrawCircle(dst2, p1, task.DotSize + 2, cv.Scalar.Red)
            DrawCircle(dst2, p2, task.DotSize + 2, cv.Scalar.Red)
        End If
    End Sub
End Class








Public Class LongLine_ExtendAll : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public Sub New()
        labels = {"", "", "Image output from LineRGB_Core", "The extended line for each line found in LineRGB_Core"}
        desc = "Create a list of all the extended lines in an image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lineRGB.dst2

        dst3 = src.Clone
        lpList.Clear()
        For Each lp In task.lineRGB.lpList
            DrawLine(dst3, lp.ep1, lp.ep2, task.highlight)
            lpList.Add(New lpData(lp.ep1, lp.ep2))
        Next
    End Sub
End Class





Public Class LongLine_ExtendParallel : Inherits TaskParent
    Dim extendAll As New LongLine_ExtendAll
    Dim knn As New KNN_Basics
    Public parList As New List(Of coinPoints)
    Public Sub New()
        labels = {"", "", "Image output from LineRGB_Core", "Parallel extended lines"}
        desc = "Use KNN to find which lines are near each other and parallel"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        extendAll.Run(src)
        dst3 = extendAll.dst2

        knn.queries.Clear()
        For Each lp In extendAll.lpList
            knn.queries.Add(New cv.Point2f((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2))
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)

        If knn.queries.Count = 0 Then Exit Sub ' no input...possible in a dark room...

        knn.Run(src)
        dst2 = src.Clone
        parList.Clear()
        Dim checkList As New List(Of cv.Point)
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

                    Dim mps = task.lineRGB.lpList(index)
                    cp.p1 = mps.p1
                    cp.p2 = mps.p2

                    mps = task.lineRGB.lpList(i)
                    cp.p3 = mps.p1
                    cp.p4 = mps.p2

                    If checkList.Contains(cp.p1) = False And checkList.Contains(cp.p2) = False And checkList.Contains(cp.p3) = False And checkList.Contains(cp.p4) = False Then
                        If (cp.p1 = cp.p3 Or cp.p1 = cp.p4) And (cp.p2 = cp.p3 Or cp.p2 = cp.p4) Then
                            ' duplicate points...
                        Else
                            DrawLine(dst2, cp.p1, cp.p2, task.highlight)
                            DrawLine(dst2, cp.p3, cp.p4, cv.Scalar.Red)
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








Public Class LongLine_DepthUpdate : Inherits TaskParent
    Dim direction As New LongLine_DepthDirection
    Public Sub New()
        desc = "Update the line bricks with a better estimate of the distance."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        direction.Run(src)
        dst2 = direction.dst3
        labels(2) = direction.labels(3)

    End Sub
End Class
