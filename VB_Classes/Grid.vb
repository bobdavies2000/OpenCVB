Imports cvb = OpenCvSharp
Imports System.Threading
Public Class Grid_Basics : Inherits VB_Parent
    Public gridRects As New List(Of cvb.Rect)
    Public updateTaskgridRects As Boolean = True
    Public Sub New()
        desc = "Create a grid of squares covering the entire image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.mouseClickFlag And Not task.FirstPass Then
            task.gridROIclicked = task.gridMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
        End If
        If task.optionsChanged Then
            task.gridSize = task.gOptions.GridSlider.Value
            task.gridMask = New cvb.Mat(src.Size(), cvb.MatType.CV_8U)
            task.gridMap = New cvb.Mat(src.Size(), cvb.MatType.CV_32S, 255)

            gridRects.Clear()
            task.gridIndex.Clear()
            task.gridRows = 0
            task.gridCols = 0
            Dim index As Integer
            For y = 0 To src.Height - 1 Step task.gridSize
                For x = 0 To src.Width - 1 Step task.gridSize
                    Dim roi = ValidateRect(New cvb.Rect(x, y, task.gridSize, task.gridSize))
                    If roi.Width > 0 And roi.Height > 0 Then
                        If x = 0 Then task.gridRows += 1
                        If y = 0 Then task.gridCols += 1
                        gridRects.Add(roi)
                        task.gridIndex.Add(index)
                        index += 1
                    End If
                Next
            Next
            task.subDivisionCount = 9

            If task.color Is Nothing Then Exit Sub ' startup condition.

            If src.Size = task.color.Size Then
                task.gridMask.SetTo(0)
                For x = task.gridSize To src.Width - 1 Step task.gridSize
                    Dim p1 = New cvb.Point(x, 0), p2 = New cvb.Point(x, src.Height)
                    task.gridMask.Line(p1, p2, 255, task.lineWidth)
                Next
                For y = task.gridSize To src.Height - 1 Step task.gridSize
                    Dim p1 = New cvb.Point(0, y), p2 = New cvb.Point(src.Width, y)
                    task.gridMask.Line(p1, p2, 255, task.lineWidth)
                Next

                For i = 0 To gridRects.Count - 1
                    Dim roi = gridRects(i)
                    task.gridMap.Rectangle(roi, i, -1)
                Next

                task.gridNeighbors.Clear()
                For j = 0 To gridRects.Count - 1
                    Dim roi = gridRects(j)
                    Dim nextList As New List(Of Integer)
                    nextList.Add(j)
                    For i = 0 To 8
                        Dim x = Choose(i + 1, roi.X - 1, roi.X, roi.X + roi.Width + 1,
                                          roi.X - 1, roi.X, roi.X + roi.Width + 1,
                                          roi.X - 1, roi.X, roi.X + roi.Width + 1)
                        Dim y = Choose(i + 1, roi.Y - 1, roi.Y - 1, roi.Y - 1, roi.Y, roi.Y, roi.Y,
                                          roi.Y + roi.Height + 1, roi.Y + roi.Height + 1, roi.Y + roi.Height + 1)
                        If x >= 0 And x < src.Width And y >= 0 And y < src.Height Then
                            Dim val = task.gridMap.Get(Of Integer)(y, x)
                            If nextList.Contains(val) = False Then nextList.Add(val)
                        End If
                    Next
                    task.gridNeighbors.Add(nextList)
                Next

                task.gridAllNabes.Clear()
                For Each nlist In task.gridNeighbors
                    Dim cellx As New List(Of Integer)
                    Dim celly As New List(Of Integer)
                    For Each n In nlist
                        Dim roi = gridRects(n)
                        cellx.Add(roi.X)
                        celly.Add(roi.Y)
                    Next

                    Dim nabes = New cvb.Rect(cellx.Min, celly.Min, cellx.Max - cellx.Min,
                                                                   celly.Max - celly.Min)
                    task.gridAllNabes.Add(nabes)
                Next
            End If

            For Each roi In gridRects
                Dim xSub = roi.X + roi.Width
                Dim ySub = roi.Y + roi.Height
                If ySub <= dst2.Height / 3 Then
                    If xSub <= dst2.Width / 3 Then task.subDivisions.Add(0)
                    If xSub >= dst2.Width / 3 And xSub <= dst2.Width * 2 / 3 Then task.subDivisions.Add(1)
                    If xSub > dst2.Width * 2 / 3 Then task.subDivisions.Add(2)
                End If
                If ySub > dst2.Height / 3 And ySub <= dst2.Height * 2 / 3 Then
                    If xSub <= dst2.Width / 3 Then task.subDivisions.Add(3)
                    If xSub >= dst2.Width / 3 And xSub <= dst2.Width * 2 / 3 Then task.subDivisions.Add(4)
                    If xSub > dst2.Width * 2 / 3 Then task.subDivisions.Add(5)
                End If
                If ySub > dst2.Height * 2 / 3 Then
                    If xSub <= dst2.Width / 3 Then task.subDivisions.Add(6)
                    If xSub >= dst2.Width / 3 And xSub <= dst2.Width * 2 / 3 Then task.subDivisions.Add(7)
                    If xSub > dst2.Width * 2 / 3 Then task.subDivisions.Add(8)
                End If
            Next

            'task.gridLowResIndices = New cvb.Mat(task.gridRows, task.gridCols, cvb.MatType.CV_32FC2)
            'For y = 0 To task.gridRows - 1
            '    For x = 0 To task.gridCols - 1
            '        task.gridLowResIndices.Set(Of cvb.Point2f)(y, x, New cvb.Point2f(CSng(x), CSng(y)))
            '    Next
            'Next

            'task.gridHighResIndices = New cvb.Mat(task.color.Size, cvb.MatType.CV_32FC2)
            'Dim roiIndex As Integer
            'For y = 0 To task.gridCols - 1
            '    For x = 0 To task.gridRows - 1
            '        Dim val = task.gridLowResIndices.Get(Of cvb.Vec2f)(y, x)
            '        Dim roi = gridRects(roiIndex)
            '        task.gridHighResIndices(roi).SetTo(Val)
            '        roiIndex += 1
            '    Next
            'Next
        End If
        If standaloneTest() Then
            dst2 = New cvb.Mat(src.Size(), cvb.MatType.CV_8U)
            task.color.CopyTo(dst2)
            dst2.SetTo(cvb.Scalar.White, task.gridMask)
            labels(2) = "Grid_Basics " + CStr(gridRects.Count) + " (" + CStr(task.gridRows) + "X" + CStr(task.gridCols) + ") " +
                              CStr(task.gridSize) + "X" + CStr(task.gridSize) + " regions"
        End If

        If updateTaskgridRects Then task.gridRects = gridRects
    End Sub
End Class






Public Class Grid_BasicsTest : Inherits VB_Parent
    Public Sub New()
        labels = {"", "", "Each grid element is assigned a value below", "The line is the diagonal for each roi.  Bottom might be a shortened roi."}
        If standaloneTest() Then desc = "Validation test for Grid_Basics algorithm"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim mean = cvb.Cv2.Mean(src)

        dst2.SetTo(0)
        ' SetTrueText is not thread-safe...
        'Parallel.For(0, task.gridRects.Count,
        ' Sub(i)
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            cvb.Cv2.Subtract(mean, src(roi), dst2(roi))
            SetTrueText(CStr(i), New cvb.Point(roi.X, roi.Y))
        Next
        'End Sub)
        dst2.SetTo(cvb.Scalar.White, task.gridMask)

        dst3.SetTo(0)
        Parallel.For(0, task.gridRects.Count,
         Sub(i)
             Dim roi = task.gridRects(i)
             cvb.Cv2.Subtract(mean, src(roi), dst3(roi))
             DrawLine(dst3(roi), New cvb.Point(0, 0), New cvb.Point(roi.Width, roi.Height), cvb.Scalar.White)
         End Sub)
    End Sub
End Class










Public Class Grid_List : Inherits VB_Parent
    Public Sub New()
        labels(2) = "Adjust grid width/height to increase thread count."
        If standaloneTest() Then desc = "List the active threads"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Parallel.ForEach(Of cvb.Rect)(task.gridRects,
         Sub(roi)
             dst3(roi).SetTo(0)
         End Sub)
        Try
            Dim CurrentProcess As Process = Process.GetCurrentProcess()
            Dim myThreads As ProcessThreadCollection = CurrentProcess.Threads
            Dim str = ""
            Dim threadCount As Integer
            Dim notIdle As Integer
            For Each thread In myThreads
                str += CStr(thread.id) + " state = " + CStr(thread.threadstate) + ", "
                threadCount += 1
                If threadCount Mod 5 = 0 Then str += vbCrLf
                If thread.threadstate <> 5 Then notIdle += 1
            Next thread
            SetTrueText("There were " + CStr(threadCount) + " threads in OpenCVB with " + CStr(notIdle) + " of them not idle when traversing the gridRects" + vbCrLf + str)
        Catch e As Exception
            MsgBox(e.Message)
        End Try
    End Sub
End Class








Public Class Grid_Rectangles : Inherits VB_Parent
    Public tilesPerRow As Integer
    Public tilesPerCol As Integer
    Dim options As New Options_Grid
    Public Sub New()
        task.gridMask = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U)
        task.gridMap = New cvb.Mat(dst2.Size(), cvb.MatType.CV_32S)
        If standaloneTest() Then desc = "Create a grid of rectangles (not necessarily squares) for use with parallel.For"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If task.mouseClickFlag Then task.gridROIclicked = task.gridMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
        If task.optionsChanged Then
            task.gridRects.Clear()
            For y = 0 To dst2.Height - 1 Step options.height
                For x = 0 To dst2.Width - 1 Step options.width
                    Dim roi = New cvb.Rect(x, y, options.width, options.height)
                    If x + roi.Width >= dst2.Width Then roi.Width = dst2.Width - x
                    If y + roi.Height >= dst2.Height Then roi.Height = dst2.Height - y
                    If roi.Width > 0 And roi.Height > 0 Then
                        If y = 0 Then tilesPerRow += 1
                        If x = 0 Then tilesPerCol += 1
                        task.gridRects.Add(roi)
                    End If
                Next
            Next

            task.gridMask.SetTo(0)
            For x = options.width To dst2.Width - 1 Step options.width
                Dim p1 = New cvb.Point(CInt(x), 0), p2 = New cvb.Point(CInt(x), dst2.Height)
                task.gridMask.Line(p1, p2, 255, task.lineWidth)
            Next
            For y = options.height To dst2.Height - 1 Step options.height
                Dim p1 = New cvb.Point(0, CInt(y)), p2 = New cvb.Point(dst2.Width, CInt(y))
                task.gridMask.Line(p1, p2, 255, task.lineWidth)
            Next

            For i = 0 To task.gridRects.Count - 1
                Dim roi = task.gridRects(i)
                task.gridMap.Rectangle(roi, i, -1)
            Next
        End If
        If standaloneTest() Then
            task.color.CopyTo(dst2)
            dst2.SetTo(cvb.Scalar.White, task.gridMask)
            labels(2) = "Grid_Basics " + CStr(task.gridRects.Count) + " (" + CStr(tilesPerRow) + "X" + CStr(tilesPerCol) + ") " +
                          CStr(options.width) + "X" + CStr(options.height) + " regions"
        End If
    End Sub
End Class






Public Class Grid_FPS : Inherits VB_Parent
    Public heartBeat As Boolean
    Dim skipCount As Integer
    Dim saveSkip As Integer
    Dim options As New Options_Grid
    Public Sub New()
        desc = "Provide a service that lets any algorithm control its frame rate"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim fps = CInt(task.fpsRate / options.desiredFPS)
        If fps = 0 Then fps = 1
        heartBeat = (task.frameCount Mod fps) = 0
        If heartBeat Then
            saveSkip = skipCount
            skipCount = 0
            If standaloneTest() Then dst2 = src
        Else
            skipCount += 1
        End If
        strOut = "Grid heartbeat set to " + CStr(options.desiredFPS) + " times per second.  " + CStr(saveSkip) + " frames skipped"
    End Sub
End Class







Public Class Grid_Neighbors : Inherits VB_Parent
    Dim mask As New cvb.Mat
    Public Sub New()
        labels = {"", "", "Grid_Basics output", ""}
        desc = "Click any grid element to see its neighbors"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.gridRows <> CInt(dst2.Height / 10) Then
            task.gOptions.setGridSize(CInt(dst2.Height / 10))
            task.gridRows = task.gridSize
            task.grid.Run(src)
        End If

        dst2 = src
        If standaloneTest() Then
            If task.heartBeat Then
                task.mouseClickFlag = True
                task.ClickPoint = New cvb.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            End If
        End If

        SetTrueText("Click any grid entry to see its neighbors", 3)
        If task.optionsChanged Then mask = task.gridMask.Clone

        If task.mouseClickFlag Then
            mask = task.gridMask.Clone
            Dim roiIndex = task.gridMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)

            For Each index In task.gridNeighbors(roiIndex)
                Dim roi = task.gridRects(index)
                mask.Rectangle(roi, cvb.Scalar.White)
            Next
        End If
        dst2.SetTo(cvb.Scalar.White, mask)
    End Sub
End Class







Public Class Grid_Special : Inherits VB_Parent
    Public gridWidth As Integer = 10
    Public gridHeight As Integer = 10
    Public gridRects As New List(Of cvb.Rect)
    Public gridRows As Integer
    Public gridCols As Integer
    Public gridMask As cvb.Mat
    Public gridNeighbors As New List(Of List(Of Integer))
    Public gridMap As cvb.Mat
    Public Sub New()
        gridMask = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U)
        gridMap = New cvb.Mat(dst2.Size(), cvb.MatType.CV_32S)
        desc = "Grids are normally square.  Grid_Special allows grid elements to be rectangles.  Specify the Y size."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.optionsChanged Then
            gridWidth = task.gridSize
            gridRects.Clear()
            gridRows = 0
            gridCols = 0
            For y = 0 To dst2.Height - 1 Step gridHeight
                For x = 0 To dst2.Width - 1 Step gridWidth
                    Dim roi = New cvb.Rect(x, y, gridWidth, gridHeight)
                    If x + roi.Width >= dst2.Width Then roi.Width = dst2.Width - x
                    If y + roi.Height >= dst2.Height Then roi.Height = dst2.Height - y
                    If roi.Width > 0 And roi.Height > 0 Then
                        If x = 0 Then gridRows += 1
                        If y = 0 Then gridCols += 1
                        gridRects.Add(roi)
                    End If
                Next
            Next

            gridMask.SetTo(0)
            For x = gridWidth To dst2.Width - 1 Step gridWidth
                Dim p1 = New cvb.Point(x, 0), p2 = New cvb.Point(x, dst2.Height)
                gridMask.Line(p1, p2, 255, task.lineWidth)
            Next
            For y = gridHeight To dst2.Height - 1 Step gridHeight
                Dim p1 = New cvb.Point(0, y), p2 = New cvb.Point(dst2.Width, y)
                gridMask.Line(p1, p2, 255, task.lineWidth)
            Next

            For i = 0 To task.gridRects.Count - 1
                Dim roi = gridRects(i)
                gridMap.Rectangle(roi, i, -1)
            Next

            gridNeighbors.Clear()
            For Each roi In gridRects
                gridNeighbors.Add(New List(Of Integer))
                For i = 0 To 8
                    Dim x = Choose(i + 1, roi.X - 1, roi.X, roi.X + roi.Width + 1,
                                          roi.X - 1, roi.X, roi.X + roi.Width + 1,
                                          roi.X - 1, roi.X, roi.X + roi.Width + 1)
                    Dim y = Choose(i + 1, roi.Y - 1, roi.Y - 1, roi.Y - 1, roi.Y, roi.Y, roi.Y,
                                          roi.Y + roi.Height + 1, roi.Y + roi.Height + 1, roi.Y + roi.Height + 1)
                    If x >= 0 And x < dst2.Width And y >= 0 And y < dst2.Height Then
                        gridNeighbors(gridNeighbors.Count - 1).Add(gridMap.Get(Of Integer)(y, x))
                    End If
                Next
            Next

        End If
        If standaloneTest() Then
            task.color.CopyTo(dst2)
            dst2.SetTo(cvb.Scalar.White, gridMask)
            labels(2) = "Grid_Basics " + CStr(gridRects.Count) + " (" + CStr(gridRows) + "X" + CStr(gridCols) + ") " +
                          CStr(gridWidth) + "X" + CStr(gridHeight) + " regions"
        End If
    End Sub
End Class







Public Class Grid_MinMaxDepth : Inherits VB_Parent
    Public minMaxLocs(0) As PointPair
    Public minMaxVals(0) As cvb.Vec2f
    Public Sub New()
        task.gOptions.setGridSize(8)
        UpdateAdvice(traceName + ": goptions 'grid Square Size' has direct impact.")
        desc = "Find the min and max depth within each grid roi."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If minMaxLocs.Count <> task.gridRects.Count Then ReDim minMaxLocs(task.gridRects.Count - 1)
        If minMaxVals.Count <> task.gridRects.Count Then ReDim minMaxVals(task.gridRects.Count - 1)
        Dim mm As mmData
        For i = 0 To minMaxLocs.Count - 1
            Dim roi = task.gridRects(i)
            task.pcSplit(2)(roi).MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, task.depthMask(roi))
            minMaxLocs(i) = New PointPair(mm.minLoc, mm.maxLoc)
            minMaxVals(i) = New cvb.Vec2f(mm.minVal, mm.maxVal)
        Next

        If standaloneTest() Then
            dst2.SetTo(0)
            For i = 0 To minMaxLocs.Count - 1
                Dim lp = minMaxLocs(i)
                DrawCircle(dst2(task.gridRects(i)), lp.p2, task.DotSize, cvb.Scalar.Red)
                DrawCircle(dst2(task.gridRects(i)), lp.p1, task.DotSize, cvb.Scalar.White)
            Next
            dst2.SetTo(cvb.Scalar.White, task.gridMask)
        End If
    End Sub
End Class







Public Class Grid_TrackCenter : Inherits VB_Parent
    Public center As cvb.Point
    Dim match As New Match_Basics
    Public Sub New()
        If standalone Then task.gOptions.ShowGrid.Checked = True

        desc = "Track a cell near the center of the grid"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If match.correlation < match.options.correlationMin Or task.gOptions.debugChecked Then
            task.gOptions.debugChecked = False
            Dim index = task.gridMap.Get(Of Integer)(dst2.Height / 2, dst2.Width / 2)
            Dim roi = task.gridRects(index)
            match.template = src(roi).Clone
            center = New cvb.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
        End If

        Dim templatePad = match.options.templatePad
        Dim templateSize = match.options.templateSize
        match.searchRect = ValidateRect(New cvb.Rect(center.X - templatePad, center.Y - templatePad, templateSize, templateSize))
        match.Run(src)
        center = match.matchCenter

        If standaloneTest() Then
            dst2 = src
            dst2.Rectangle(match.matchRect, task.HighlightColor, task.lineWidth + 1, task.lineType)
            DrawCircle(dst2, center, task.DotSize, cvb.Scalar.White)

            If task.heartBeat Then dst3.SetTo(0)
            DrawCircle(dst3, center, task.DotSize, task.HighlightColor)
            SetTrueText(Format(match.correlation, fmt3), center, 3)

            labels(3) = "Match correlation = " + Format(match.correlation, fmt3)
        End If
    End Sub
End Class





Public Class Grid_ShowMap : Inherits VB_Parent
    Public Sub New()
        desc = "Verify that task.gridMap is laid out correctly"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.gridMap.ConvertTo(dst2, cvb.MatType.CV_8U)
        dst3 = ShowPalette(dst2)
    End Sub
End Class