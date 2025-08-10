Imports cv = OpenCvSharp
Public Class Line3D_Basics : Inherits TaskParent
    Public lines3D As New List(Of cv.Point3f)
    Public lines3DMat As New cv.Mat
    Public Sub New()
        task.brickRunFlag = True
        desc = "Find all the lines in 3D using the structured slices through the bricks."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        labels(2) = task.lines.labels(2)

        Static brickList As New List(Of brickData)(task.bricks.brickList)

        For i = 0 To task.gridRects.Count - 1
            Dim brick = task.bricks.brickList(i)
            Dim val = task.motionMask.Get(Of Byte)(brick.center.Y, brick.center.X)
            If val Then brickList(i) = brick
        Next

        lines3D.Clear()

        For Each lp In task.lines.lpList
            Dim gc1 = brickList(task.grid.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
            If gc1.depth = 0 Then Continue For

            Dim gc2 = brickList(task.grid.gridMap.Get(Of Integer)(lp.p2.Y, lp.p2.X))
            If gc2.depth = 0 Then Continue For

            lines3D.Add(New cv.Point3f(0, 0.9, 0.9))
            Dim p1 = getWorldCoordinates(gc1.center, gc1.depth)
            Dim p2 = getWorldCoordinates(gc2.center, gc2.depth)
            'p1.Z -= 0.5
            'p2.Z -= 0.5 ' so the line will appear in front of the pointcloud data by 0.5 meter
            lines3D.Add(p1)
            lines3D.Add(p2)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link8)
        Next

        lines3DMat = cv.Mat.FromPixelData(lines3D.Count / 3, 1, cv.MatType.CV_32FC3, lines3D.ToArray)

        If task.heartBeat Then
            strOut = CStr(lines3D.Count / 3) + " 3D lines are prepared in lines3D." + vbCrLf +
                     CStr(task.lines.lpList.Count - lines3D.Count / 3) + " lines occurred in areas with no depth and were skipped."
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class Line3D_Depth : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public lpDepth As New List(Of Single) ' 2 depths for each lp
    Public lines3DMat As New cv.Mat
    Public Sub New()
        If standalone Then task.featureOptions.FeatureSampleSize.Value = 10
        desc = "Find all the lines in 3D using the depth data for the first and last brick."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        labels(2) = task.lines.labels(2)

        lpList.Clear()
        For Each lp In task.lines.lpList
            Dim index = task.grid.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X)
            Dim depth1 = task.pcSplit(2)(task.gridRects(index)).Mean(task.depthMask(task.gridRects(index))).Val0
            If depth1 = 0 Then Continue For


            index = task.grid.gridMap.Get(Of Integer)(lp.p2.Y, lp.p2.X)
            Dim depth2 = task.pcSplit(2)(task.gridRects(index)).Mean(task.depthMask(task.gridRects(index))).Val0
            If depth2 = 0 Then Continue For

            lpList.Add(lp)
            lpDepth.Add(depth1)
            lpDepth.Add(depth2)
            SetTrueText(Format(depth1, fmt1), lp.p2)
            SetTrueText(Format(depth2, fmt1), lp.p1)

            If lpList.Count >= task.featureOptions.FeatureSampleSize.Value Then Exit For
            DrawLine(dst2, lp)
        Next
    End Sub
End Class







Public Class Line3D_Draw : Inherits TaskParent
    Public p1 As cv.Point, p2 As cv.Point
    Dim plot As New Plot_OverTimeScalar
    Dim toggleFirstSecond As Boolean
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        plot.plotCount = 2

        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))

        p1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        p2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        labels(2) = "Click twice in the image below to draw a line and that line's depth is correlated in X to Z and Y to Z in the plot at right"
        desc = "Determine where a 3D line is close to the real depth data"
    End Sub
    Private Function findCorrelation(pts1 As cv.Mat, pts2 As cv.Mat) As Single
        Dim correlationMat As New cv.Mat
        cv.Cv2.MatchTemplate(pts1, pts2, correlationMat, cv.TemplateMatchModes.CCoeffNormed)
        Return correlationMat.Get(Of Single)(0, 0)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            If task.mouseClickFlag Then
                If toggleFirstSecond = False Then
                    p1 = task.ClickPoint
                Else
                    p2 = task.ClickPoint
                    toggleFirstSecond = False
                End If
            End If
        End If

        If toggleFirstSecond Then Exit Sub ' wait until the second point is selected...

        dst1 = src
        DrawLine(dst1, p1, p2, task.highlight)
        dst0.SetTo(0)
        DrawLine(dst0, p1, p2, 255)
        dst1.SetTo(0)
        task.pcSplit(0).CopyTo(dst1, dst0)
        Dim points = dst1.FindNonZero()

        Dim nextList As New List(Of cv.Point3f)
        For i = 0 To points.Rows - 1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            nextList.Add(task.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X))
        Next
        If nextList.Count = 0 Then Exit Sub ' line is completely in area with no depth.

        Dim pts As cv.Mat = cv.Mat.FromPixelData(nextList.Count, 1, cv.MatType.CV_32FC3, nextList.ToArray)
        Dim zSplit = pts.Split()
        Dim c1 = findCorrelation(zSplit(0), zSplit(2))
        Dim c2 = findCorrelation(zSplit(1), zSplit(2))

        plot.plotData = New cv.Scalar(c1, c2, 0)

        plot.Run(src)
        dst2 = plot.dst2
        dst3 = plot.dst3
        labels(3) = "using " + CStr(nextList.Count) + " points, the correlation of X to Z = " + Format(c1, fmt3) + " (blue), correlation of Y to Z = " + Format(c2, fmt3) + " (green)"
    End Sub
End Class







Public Class Line3D_Constructed : Inherits TaskParent
    Dim lines As New Line3D_Basics
    Public Sub New()
        desc = "Build the 3D lines found in Line3D_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2
        dst3 = lines.dst3
        labels(2) = lines.labels(2)
    End Sub
End Class





Public Class Line3D_Longest : Inherits TaskParent
    Public Sub New()
        task.brickRunFlag = True
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find the longest line in BGR and use it to measure the average depth for the line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count <= 1 Then Exit Sub
        Dim lp = task.lines.lpList(0)
        dst2 = src

        dst2.Line(lp.p1, lp.p2, cv.Scalar.Yellow, task.lineWidth + 3, task.lineType)

        Dim gcMin = task.bricks.brickList(task.grid.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
        Dim gcMax = task.bricks.brickList(task.grid.gridMap.Get(Of Integer)(lp.p2.Y, lp.p2.X))

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