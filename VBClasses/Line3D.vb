Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Line3D_Basics : Inherits TaskParent
    Public lines3D As New List(Of cv.Point3f)
    Public lines3DMat As New cv.Mat
    Public Sub New()
        If standalone Then task.featureOptions.FeatureSampleSize.Value = 10
        desc = "Find the end point depth for the top X longest lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        labels(2) = task.lines.labels(2)

        lines3D.Clear()

        For Each lp In task.lines.lpList
            Dim rect1 = task.gridRects(task.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
            Dim depth1 = task.pcSplit(2)(rect1).Mean(task.depthMask(rect1))(0)
            If depth1 = 0 Then Continue For

            Dim rect2 = task.gridRects(task.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
            Dim depth2 = task.pcSplit(2)(rect2).Mean(task.depthMask(rect2))(0)
            If depth2 = 0 Then Continue For

            Dim p1 = Cloud_Basics.worldCoordinates(rect1.TopLeft, depth1)
            Dim p2 = Cloud_Basics.worldCoordinates(rect2.TopLeft, depth2)
            lines3D.Add(p1)
            lines3D.Add(p2)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link8)
            SetTrueText(Format(depth1, fmt1), lp.p1, 2)
            SetTrueText(Format(depth2, fmt1), lp.p2, 2)
        Next

        lines3DMat = cv.Mat.FromPixelData(lines3D.Count / 2, 1, cv.MatType.CV_32FC3, lines3D.ToArray)

        If task.heartBeat Then
            strOut = CStr(lines3D.Count / 2) + " 3D lines are prepared in lines3D." + vbCrLf +
                     CStr(task.lines.lpList.Count - lines3D.Count / 2) + " lines occurred in areas with no depth and were skipped."
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class Line3D_Longest : Inherits TaskParent
    Public Sub New()
        If task.bricks Is Nothing Then task.bricks = New Brick_Basics
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find the longest line in BGR and use it to measure the average depth for the line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count <= 1 Then Exit Sub
        Dim lp = task.lines.lpList(0)
        dst2 = src

        dst2.Line(lp.p1, lp.p2, cv.Scalar.Yellow, task.lineWidth + 3, task.lineType)

        Dim gcMin = task.bricks.brickList(task.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
        Dim gcMax = task.bricks.brickList(task.gridMap.Get(Of Integer)(lp.p2.Y, lp.p2.X))

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

        SetTrueText("Average Depth = " + Format(depthMean, fmt1) + "m",
                    New cv.Point((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2), 2)
        labels(2) = "Min Distance = " + Format(depthMin, fmt1) + ", Max Distance = " + Format(depthMax, fmt1) +
                  ", Mean Distance = " + Format(depthMean, fmt1) + " meters "

        SetTrueText(Format(depthMin, fmt1) + "m", New cv.Point(mm.minLoc.X + 5, mm.minLoc.Y - 15), 2)
        SetTrueText(Format(depthMax, fmt1) + "m", New cv.Point(mm.maxLoc.X + 5, mm.maxLoc.Y - 15), 2)
    End Sub
End Class








Public Class Line3D_ReconstructLine : Inherits TaskParent
    Public findLine3D As New FindNonZero_Line3D
    Public selectLine As New Delaunay_LineSelect
    Public pointcloud As New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
    Public Sub New()
        desc = "Build the 3D lines found in Line_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        selectLine.Run(src)
        dst2 = selectLine.dst2
        labels(2) = selectLine.labels(2)

        If task.lpD.age = 1 Or task.optionsChanged Then
            findLine3D.lp = task.lpD
            findLine3D.Run(src)

            If findLine3D.veclist.Count = 0 Then Exit Sub ' nothing to work on...

            pointcloud.SetTo(0)
            For i = 0 To findLine3D.veclist.Count - 1
                Dim pt = findLine3D.ptList(i)
                Dim vec = findLine3D.veclist(i)
                pointcloud.Set(Of cv.Vec3f)(pt.Y, pt.X, vec)
            Next
        End If

        labels(2) = findLine3D.labels(2)
    End Sub
End Class







Public Class Line3D_DrawArbitrary : Inherits TaskParent
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





Public Class Line3D_Selection : Inherits TaskParent
    Public lp As New lpData
    Public debugRequest As Boolean
    Dim allPoints As New cv.Mat
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Select a line using the debug slider."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count = 0 Then Exit Sub ' no lines found.

        If task.firstPass = False Then
            dst1(lp.rect).SetTo(0)
            dst2(lp.rect).SetTo(0)
            dst3(lp.rect).SetTo(0)
        End If

        If standalone Or debugRequest Then
            If Math.Abs(task.gOptions.DebugSlider.Value) < task.lines.lpList.Count Then
                lp = task.lines.lpList(Math.Abs(task.gOptions.DebugSlider.Value))
            End If
        End If
        dst3.Line(lp.p1, lp.p2, 255, 1, cv.LineTypes.Link4)
        If standaloneTest() Or debugRequest Then allPoints = dst3(lp.rect).FindNonZero()

        task.pcSplit(2)(lp.rect).CopyTo(dst1(lp.rect), dst3(lp.rect))
        dst3(lp.rect).SetTo(0, task.noDepthMask(lp.rect))
        Dim depthAvg = dst1(lp.rect).Mean(dst3(lp.rect)).Item(0)
        Dim points = dst3(lp.rect).FindNonZero()
        Dim ptList As New List(Of cv.Point)
        If points.Rows = 0 Then
            ptList.Add(lp.p1)
            ptList.Add(lp.p2) ' end points of a line will always have depth (or the line wouldn't be there.)
        Else
            Dim ptArray(points.Total * 2 - 1) As Integer
            Marshal.Copy(points.Data, ptArray, 0, ptArray.Length)

            For i = 0 To ptArray.Count - 2 Step 2
                Dim pt = New cv.Point(lp.rect.X + ptArray(i), lp.rect.Y + ptArray(i + 1))
                ptList.Add(pt)
            Next
        End If

        Dim p1 As cv.Point, p2 As cv.Point
        Dim d1 As Single, d2 As Single
        Dim incrList As New List(Of Single)
        For i = 1 To ptList.Count - 1
            p1 = ptList(i - 1)
            p2 = ptList(i)
            If p1.DistanceTo(p2) > 2 Then Continue For ' too big a gap of missing depth.
            d1 = task.pcSplit(2).Get(Of Single)(p1.Y, p1.X)
            d2 = task.pcSplit(2).Get(Of Single)(p2.Y, p2.X)
            Dim delta = d2 - d1
            If Math.Abs(delta) < 0.1 Then incrList.Add(delta) ' if delta is less than 10 centimeters, then keep it.
        Next

        Dim deltaZ As Single
        If incrList.Count Then
            deltaZ = Math.Abs(incrList.Average())
        Else
            ' fallback method.
            deltaZ = (lp.pVec1(2) - lp.pVec2(2)) / lp.length
        End If

        Dim depth1 As Single, depth2 As Single
        If lp.pVec1(2) < lp.pVec2(2) Then
            depth1 = depthAvg - deltaZ * lp.length / 2
            depth2 = depth1 + deltaZ * lp.length
        Else
            depth1 = depthAvg + deltaZ * lp.length / 2
            depth2 = depth1 - deltaZ * lp.length
            deltaZ = -deltaZ
        End If

        ' This updates the lp so that it may be used to draw a line in 3D without further calculation.
        lp.pVec1 = Cloud_Basics.worldCoordinates(ptList(0).X, ptList(0).Y, depth1)
        lp.pVec2 = Cloud_Basics.worldCoordinates(ptList.Last.X, ptList.Last.Y, depth2)

        If standaloneTest() Or debugRequest Then
            For i = 0 To allPoints.Rows - 1
                Dim pt = allPoints.Get(Of cv.Point)(i, 0)
                pt.X += lp.rect.X
                pt.Y += lp.rect.Y
                dst2.Set(Of cv.Vec3f)(pt.Y, pt.X, Cloud_Basics.worldCoordinates(pt.X, pt.Y, depth1 + i * deltaZ))
            Next

            labels(2) = CStr(allPoints.Rows) + " pixels updated in the point cloud."
            strOut = "Average depth = " + Format(depthAvg, fmt3) + vbCrLf
            strOut += "depth1 = " + Format(depth1, fmt3) + vbCrLf
            strOut += "depth2 = " + Format(depth2, fmt3) + vbCrLf
            strOut += CStr(ptList.Count) + " points found with depth" + vbCrLf
            strOut += Format(deltaZ, fmt4) + " deltaZ for each point." + vbCrLf
            strOut += CStr(allPoints.Rows) + " points in the original line." + vbCrLf
            SetTrueText(strOut, 3)
            SetTrueText("ptlist(0)", ptList(0))
            SetTrueText("p1 " + Format(lp.pVec1(2), fmt1) + "m", lp.p1, 3)
            SetTrueText("p2 " + Format(lp.pVec2(2), fmt1) + "m", lp.p2, 3)
        End If
    End Sub
End Class






Public Class Line3D_DrawLines : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Dim selection As New Line3D_Selection
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Recompute the depth for the lines found."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lpList.Clear()

        dst2 = task.pointCloud.Clone
        dst3 = src.Clone
        For Each selection.lp In task.lines.lpList
            selection.run(emptyMat)
            lpList.Add(selection.lp)
            dst3.Line(selection.lp.p1, selection.lp.p2, task.highlight, task.lineWidth)
        Next
    End Sub
End Class






Public Class Line3D_DrawLines_Debug : Inherits TaskParent
    Dim Selection As New Line3D_Selection
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        Selection.debugRequest = True
        desc = "Use the debug slider in Global Options to select which line to test."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT Then dst1.SetTo(0)
        Selection.Run(emptyMat)
        Dim lp = Selection.lp

        dst3.SetTo(0)
        dst3.Line(lp.p1, lp.p2, 255, 1, cv.LineTypes.Link4)
        dst1(lp.rect).SetTo(task.highlight, dst3(lp.rect))

        dst2 = task.pointCloud.Clone
        Selection.dst2.CopyTo(dst2, dst3)

        labels(2) = "Line " + CStr(lp.index) + " selected of " + CStr(task.lines.lpList.Count) + " top lines.  " +
                    "Use Global Options debug slider to select other lines."
        labels(3) = "Mask of selected line - use debug slider to select other lines."

        SetTrueText(Selection.strOut, 3)
    End Sub
End Class






Public Class Line3D_LogicalLines : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Dim selection As New Line3D_Selection
    Public Sub New()
        desc = "Create logical lines for all the lines detected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2
        labels = task.lines.labels

        Dim count As Integer
        lpList.Clear()

        For Each selection.lp In task.lines.lpList
            If selection.lp.age = 1 Then
                count += 1
                selection.Run(src)
            End If
            lpList.Add(selection.lp)
        Next

        SetTrueText(CStr(count) + " new lines were found and there were " + CStr(lpList.Count - count) + " total lines.", 3)
    End Sub
End Class
