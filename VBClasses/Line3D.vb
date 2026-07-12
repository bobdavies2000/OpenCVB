Imports System.Runtime.InteropServices
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Line3D_Basics : Inherits TaskParent
    Public lines3D As New List(Of Point3f)
    Public lines3DMat As New Mat
    Public Sub New()
        If standalone Then task.FeatureSampleSize = 10
        desc = "Find the end cv.Point depth for the top X longest lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        labels(2) = task.lines.labels(2)

        lines3D.Clear()

        For Each lp In task.lines.lpList
            Dim rect1 = task.gridRects(task.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
            Dim depth1 = Mean(task.pcSplit(2)(rect1), task.depthmask(rect1))(0)
            If depth1 = 0 Then Continue For

            Dim rect2 = task.gridRects(task.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
            Dim depth2 = Mean(task.pcSplit(2)(rect2), task.depthmask(rect2))(0)
            If depth2 = 0 Then Continue For

            Dim p1 = Cloud_Basics.worldCoordinates(rect1.TopLeft, depth1)
            Dim p2 = Cloud_Basics.worldCoordinates(rect2.TopLeft, depth2)
            lines3D.Add(p1)
            lines3D.Add(p2)
            Line(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth, LineTypes.Link8)
            SetTrueText(depth1.ToString(fmt1), lp.p1, 2)
            SetTrueText(depth2.ToString(fmt1), lp.p2, 2)
        Next

        lines3DMat = Mat.FromPixelData(lines3D.Count / 2, 1, MatType.CV_32FC3, lines3D.ToArray)

        If task.heartBeat Then
            strOut = CStr(lines3D.Count / 2) + " 3D lines are prepared in lines3D." + vbCrLf +
                             CStr(task.lines.lpList.Count - lines3D.Count / 2) + " lines occurred in areas with no depth and were skipped."
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class XR_Line3D_Longest : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public Sub New()
        dst0 = New Mat(dst0.Size(), MatType.CV_8U, Scalar.All(0))
        desc = "Find the longest line in BGR and use it to measure the average depth for the line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        If task.lines.lpList.Count <= 1 Then Exit Sub
        Dim lp = task.lines.lpList(0)
        dst2 = src

        Line(dst2, lp.p1, lp.p2, Scalar.Yellow, task.lineWidth + 3, task.lineType)

        Dim brickMin = bricks.brickList(task.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
        Dim brickMax = bricks.brickList(task.gridMap.Get(Of Integer)(lp.p2.Y, lp.p2.X))

        dst0.SetTo(0)
        Line(dst0, lp.p1, lp.p2, 255, 3, task.lineType)
        dst0.SetTo(0, task.noDepthMask)

        Dim mm = GetMinMax(task.pcSplit(2), dst0)
        Dim ptMin = New cv.Point(brickMin.mm.minLoc.X + brickMin.rect.X, brickMin.mm.minLoc.Y + brickMin.rect.Y)
        Dim ptMax = New cv.Point(brickMin.mm.maxLoc.X + brickMin.rect.X, brickMin.mm.maxLoc.Y + brickMin.rect.Y)
        If ptMin.DistanceTo(mm.minLoc) > ptMax.DistanceTo(mm.maxLoc) Then
            Dim tmp = brickMin
            brickMin = brickMax
            brickMax = tmp
        End If

        Dim depthMin = If(brickMin.depth > 0, brickMin.depth, mm.minVal)
        Dim depthMax = If(brickMax.depth > 0, brickMax.depth, mm.maxVal)

        Dim depthMean = Mean(task.pcSplit(2), dst0)(0)
        Circle(dst2, lp.p1, task.DotSize + 4, Scalar.Red, -1, task.lineType)
        Circle(dst2, lp.p2, task.DotSize + 4, Scalar.Blue, -1, task.lineType)

        If lp.p1.DistanceTo(mm.minLoc) < lp.p2.DistanceTo(mm.maxLoc) Then
            mm.minLoc = lp.p1
            mm.maxLoc = lp.p2
        Else
            mm.minLoc = lp.p2
            mm.maxLoc = lp.p1
        End If

        SetTrueText("Average Depth = " + depthMean.ToString(fmt1) + "m",
                        New cv.Point((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2), 2)
        labels(2) = "Min Distance = " + depthMin.ToString(fmt1) + ", Max Distance = " + depthMax.ToString(fmt1) +
                      ", Mean Distance = " + depthMean.ToString(fmt1) + " meters "

        SetTrueText(depthMin.ToString(fmt1) + "m", New cv.Point(mm.minLoc.X + 5, mm.minLoc.Y - 15), 2)
        SetTrueText(depthMax.ToString(fmt1) + "m", New cv.Point(mm.maxLoc.X + 5, mm.maxLoc.Y - 15), 2)
    End Sub
End Class








Public Class Line3D_ReconstructLine : Inherits TaskParent
    Public findLine3D As New FindNonZero_Line3D
    Public selectLine As New Delaunay_LineSelect
    Public pointcloud As New Mat(dst2.Size, MatType.CV_32FC3, 0)
    Public Sub New()
        desc = "Build the 3D lines found in Line_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        selectLine.Run(src)
        dst2 = selectLine.dst2
        labels(2) = selectLine.labels(2)

        If task.lpD Is Nothing Then task.lpD = task.lines.lpList(0)
        If task.lpD.age = 1 Or task.optionsChanged Then
            findLine3D.lp = task.lpD
            findLine3D.Run(src)

            If findLine3D.veclist.Count = 0 Then Exit Sub ' nothing to work on...

            pointcloud.SetTo(0)
            For i = 0 To findLine3D.veclist.Count - 1
                Dim pt = findLine3D.ptList(i)
                Dim vec = findLine3D.veclist(i)
                pointcloud.Set(Of Vec3f)(pt.Y, pt.X, vec)
            Next
        End If

        labels(2) = findLine3D.labels(2)
    End Sub
End Class







Public Class XR_Line3D_DrawArbitrary : Inherits TaskParent
    Public p1 As cv.Point, p2 As cv.Point
    Dim plot As New PlotTime_Scalar
    Dim toggleFirstSecond As Boolean
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        plot.plotCount = 2

        dst0 = New Mat(dst0.Size(), MatType.CV_8U, Scalar.All(0))
        dst1 = New Mat(dst1.Size(), MatType.CV_32F, Scalar.All(0))

        p1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        p2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        labels(2) = "Click twice in the image below to draw a line and that line's depth is correlated in X to Z and Y to Z in the plot at right"
        desc = "Determine where a 3D line is close to the real depth data"
    End Sub
    Private Function findCorrelation(pts1 As Mat, pts2 As Mat) As Single
        Dim correlationMat As New Mat
        MatchTemplate(pts1, pts2, correlationMat, TemplateMatchModes.CCoeffNormed)
        Return correlationMat.Get(Of Single)(0, 0)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            If task.mouseClickFlag Then
                If toggleFirstSecond = False Then
                    p1 = task.clickPoint
                Else
                    p2 = task.clickPoint
                    toggleFirstSecond = False
                End If
            End If
        End If

        If toggleFirstSecond Then Exit Sub ' wait until the second cv.Point is selected...

        dst1 = src
        Line(dst1, p1, p2, task.highlight, task.lineWidth, task.lineType)
        dst0.SetTo(0)
        Line(dst0, p1, p2, 255, task.lineWidth, task.lineType)
        dst1.SetTo(0)
        task.pcSplit(0).CopyTo(dst1, dst0)
        Dim points As New Mat
        FindNonZero(dst1, points)

        Dim nextList As New List(Of Point3f)
        For i = 0 To points.Rows - 1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            nextList.Add(task.pointCloud.Get(Of Point3f)(pt.Y, pt.X))
        Next
        If nextList.Count = 0 Then Exit Sub ' line is completely in area with no depth.

        Dim pts As Mat = Mat.FromPixelData(nextList.Count, 1, MatType.CV_32FC3, nextList.ToArray)
        Dim zSplit = Split(pts)
        Dim c1 = findCorrelation(zSplit(0), zSplit(2))
        Dim c2 = findCorrelation(zSplit(1), zSplit(2))

        plot.plotData = New Scalar(c1, c2, 0)

        plot.Run(src)
        dst2 = plot.dst2
        dst3 = plot.dst3
        labels(3) = "using " + CStr(nextList.Count) + " points, the correlation of X to Z = " + c1.ToString(fmt3) + " (blue), correlation of Y to Z = " + c2.ToString(fmt3) + " (green)"
    End Sub
End Class





Public Class Line3D_Selection : Inherits TaskParent
    Public lp As lpData
    Public debugRequest As Boolean
    Dim allPoints As New Mat
    Public Sub New()
        dst1 = New Mat(dst1.Size, MatType.CV_32F, 0)
        dst2 = New Mat(dst2.Size, MatType.CV_32FC3, 0)
        dst3 = New Mat(dst2.Size, MatType.CV_8U, 0)
        desc = "Select a line using the debug slider."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If lp Is Nothing Then lp = task.lines.lpList(0)

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
        Line(dst3, lp.p1, lp.p2, 255, 1, LineTypes.Link4)
        If standaloneTest() Or debugRequest Then FindNonZero(dst3(lp.rect), allPoints)

        task.pcSplit(2)(lp.rect).CopyTo(dst1(lp.rect), dst3(lp.rect))
        dst3(lp.rect).SetTo(0, task.noDepthMask(lp.rect))
        Dim depthAvg = Mean(dst1(lp.rect), dst3(lp.rect)).Item(0)
        Dim points As New Mat
        FindNonZero(dst3(lp.rect), points)
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
                dst2.Set(Of Vec3f)(pt.Y, pt.X, Cloud_Basics.worldCoordinates(pt.X, pt.Y, depth1 + i * deltaZ))
            Next

            labels(2) = CStr(allPoints.Rows) + " pixels updated in the cv.Point cloud."
            strOut = "Average depth = " + depthAvg.ToString(fmt3) + vbCrLf
            strOut += "depth1 = " + depth1.ToString(fmt3) + vbCrLf
            strOut += "depth2 = " + depth2.ToString(fmt3) + vbCrLf
            strOut += CStr(ptList.Count) + " points found with depth" + vbCrLf
            strOut += deltaZ.ToString(fmt4) + " deltaZ for each cv.Point." + vbCrLf
            strOut += CStr(allPoints.Rows) + " points in the original line." + vbCrLf
            SetTrueText(strOut, 3)
            SetTrueText("ptlist(0)", ptList(0))
            SetTrueText("p1 " + lp.pVec1(2).ToString(fmt1) + "m", lp.p1, 3)
            SetTrueText("p2 " + lp.pVec2(2).ToString(fmt1) + "m", lp.p2, 3)
        End If
    End Sub
End Class






Public Class Line3D_DrawLines : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Dim selection As New Line3D_Selection
    Public Sub New()
        dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
        dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
        desc = "Recompute the depth for the lines found."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lpList.Clear()

        dst2 = task.pointCloud.Clone
        dst3 = src.Clone
        For Each selection.lp In task.lines.lpList
            selection.Run(emptyMat)
            lpList.Add(selection.lp)
            Line(dst3, selection.lp.p1, selection.lp.p2, task.highlight, task.lineWidth)
        Next
    End Sub
End Class






Public Class Line3D_DrawLines_Debug : Inherits TaskParent
    Dim Selection As New Line3D_Selection
    Public Sub New()
        dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
        Selection.debugRequest = True
        desc = "Use the debug slider in Global Options to select which line to test."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT Then dst1.SetTo(0)
        Selection.Run(emptyMat)
        Dim lp = Selection.lp

        dst3.SetTo(0)
        Line(dst3, lp.p1, lp.p2, 255, 1, LineTypes.Link4)
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
