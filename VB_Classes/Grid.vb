Imports cv = OpenCvSharp
Imports System.Threading
Imports System.Windows.Media.Media3D
Public Class Grid_Basics : Inherits TaskParent
    Public gridRects As New List(Of cv.Rect)
    Public gridMask As cv.Mat
    Public gridMap As cv.Mat
    Public gridIndex As New List(Of Integer)
    Public tilesPerCol As Integer, tilesPerRow As Integer
    Public gridNabeRects As New List(Of cv.Rect)
    Public gridNeighbors As New List(Of List(Of Integer))
    Public gridPoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Create a grid of squares covering the entire image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.mouseClickFlag And Not task.firstPass Then
            task.gridROIclicked = task.gridMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
        End If

        Dim gridSize As Integer
        If task.optionsChanged Then
            tilesPerCol = 0
            tilesPerRow = 0
            gridIndex.Clear()
            gridNabeRects.Clear()
            gridNeighbors.Clear()
            gridPoints.Clear()
            gridSize = task.gOptions.GridSlider.Value
            gridMask = New cv.Mat(src.Size(), cv.MatType.CV_8U)
            gridMap = New cv.Mat(src.Size(), cv.MatType.CV_32S, 255)

            gridRects.Clear()
            gridIndex.Clear()
            Dim index As Integer
            For y = 0 To src.Height - 1 Step gridSize
                For x = 0 To src.Width - 1 Step gridSize
                    Dim roi = ValidateRect(New cv.Rect(x, y, gridSize, gridSize))
                    If roi.Width > 0 And roi.Height > 0 Then
                        If x = 0 Then tilesPerCol += 1
                        If y = 0 Then tilesPerRow += 1
                        gridRects.Add(roi)
                        gridIndex.Add(index)
                        index += 1
                    End If
                Next
            Next
            task.subDivisionCount = 9

            gridMask.SetTo(0)
            For x = gridSize To src.Width - 1 Step gridSize
                Dim p1 = New cv.Point(x, 0), p2 = New cv.Point(x, src.Height)
                gridMask.Line(p1, p2, 255, task.lineWidth)
            Next
            For y = gridSize To src.Height - 1 Step gridSize
                Dim p1 = New cv.Point(0, y), p2 = New cv.Point(src.Width, y)
                gridMask.Line(p1, p2, 255, task.lineWidth)
            Next

            For i = 0 To gridRects.Count - 1
                Dim roi = gridRects(i)
                gridMap.Rectangle(roi, i, -1)
            Next

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
                        Dim val = gridMap.Get(Of Integer)(y, x)
                        If nextList.Contains(val) = False Then nextList.Add(val)
                    End If
                Next
                gridNeighbors.Add(nextList)
            Next

            gridNabeRects.Clear()
            For Each nabeList In gridNeighbors
                Dim xList As New List(Of Integer), yList As New List(Of Integer)
                For Each index In nabeList
                    Dim roi = gridRects(index)
                    xList.Add(roi.X)
                    yList.Add(roi.Y)
                    xList.Add(roi.BottomRight.X)
                    yList.Add(roi.BottomRight.Y)
                Next
                gridNabeRects.Add(New cv.Rect(xList.Min, yList.Min,
                                              xList.Max - xList.Min,
                                              yList.Max - yList.Min))
            Next

            gridPoints.Clear()
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
                gridPoints.Add(roi.TopLeft)
            Next

            task.gridSize = gridSize
            task.tilesPerCol = tilesPerCol
            task.tilesPerRow = tilesPerRow
            task.gridMask = gridMask
            task.gridMap = gridMap
            task.gridIndex = New List(Of Integer)(gridIndex)
            task.gridRects = gridRects
            task.gridNabeRects = New List(Of cv.Rect)(gridNabeRects)
            task.gridNeighbors = New List(Of List(Of Integer))(gridNeighbors)
            task.gridPoints = New List(Of cv.Point)(gridPoints)
        End If
        If standaloneTest() Then
            dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8U)
            task.color.CopyTo(dst2)
            dst2.SetTo(white, task.gridMask)
            labels(2) = "Grid_Basics " + CStr(gridRects.Count) + " (" + CStr(task.tilesPerCol) + "X" + CStr(task.tilesPerRow) + ") " +
                                  CStr(gridSize) + "X" + CStr(gridSize) + " regions"
        End If
    End Sub
End Class








Public Class Grid_Rectangles : Inherits TaskParent
    Public tilesPerRow As Integer
    Public tilesPerCol As Integer
    Public gridMap As cv.Mat
    Public gridRectsAll As New List(Of cv.Rect)
    Public Sub New()
        gridMap = New cv.Mat(dst2.Size(), cv.MatType.CV_32S)
        If standalone Then desc = "Create a grid of rectangles (not necessarily squares) for use with parallel.For"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            gridRectsAll.Clear()
            tilesPerRow = 0
            tilesPerCol = 0
            For y = 0 To dst2.Height - 1 Step task.dCellSize
                For x = 0 To dst2.Width - 1 Step task.dCellSize
                    Dim roi = ValidateRect(New cv.Rect(x, y, task.dCellSize, task.dCellSize))
                    If roi.Width > 0 And roi.Height > 0 Then
                        If y = 0 Then tilesPerRow += 1
                        If x = 0 Then tilesPerCol += 1
                        gridRectsAll.Add(roi)
                    End If
                Next
            Next

            For i = 0 To gridRectsAll.Count - 1
                Dim roi = gridRectsAll(i)
                gridMap.Rectangle(roi, i, -1)
            Next
        End If
        If standaloneTest() Then
            gridMap.ConvertTo(dst1, cv.MatType.CV_32F)
            Dim mm = GetMinMax(dst1)
            dst1 = dst1 * 255 / mm.maxVal
            dst1.ConvertTo(dst2, cv.MatType.CV_8U)
            labels(2) = "Grid_Basics " + CStr(gridRectsAll.Count) + " tiles, " + CStr(tilesPerRow) +
                        " cols by " + CStr(tilesPerCol) + " rows, with " +
                        CStr(task.dCellSize) + "X" + CStr(task.dCellSize) + " cells"
        End If
        If task.mouseClickFlag Then
            task.gridROIclicked = gridMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
        End If
    End Sub
End Class






Public Class Grid_BasicsTest : Inherits TaskParent
    Public Sub New()
        labels = {"", "", "Each grid element is assigned a value below", "The line is the diagonal for each roi.  Bottom might be a shortened roi."}
        If standalone Then desc = "Validation test for Grid_Basics algorithm"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim mean = cv.Cv2.Mean(src)

        dst2.SetTo(0)
        ' SetTrueText is not thread-safe...
        'Parallel.For(0, task.gridRects.Count,
        ' Sub(i)
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            cv.Cv2.Subtract(mean, src(roi), dst2(roi))
            SetTrueText(CStr(i), New cv.Point(roi.X, roi.Y))
        Next
        'End Sub)
        dst2.SetTo(white, task.gridMask)

        dst3.SetTo(0)
        Parallel.For(0, task.gridRects.Count,
         Sub(i)
             Dim roi = task.gridRects(i)
             cv.Cv2.Subtract(mean, src(roi), dst3(roi))
             DrawLine(dst3(roi), New cv.Point(0, 0), New cv.Point(roi.Width, roi.Height), white)
         End Sub)
    End Sub
End Class










Public Class Grid_List : Inherits TaskParent
    Public Sub New()
        labels(2) = "Adjust grid width/height to increase thread count."
        If standalone Then desc = "List the active threads"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Parallel.ForEach(Of cv.Rect)(task.gridRects,
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






Public Class Grid_FPS : Inherits TaskParent
    Public desiredFPS As Integer = 2
    Public heartBeat As Boolean
    Dim skipCount As Integer
    Dim saveSkip As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Desired FPS rate", 1, 10, desiredFPS)
        End If
        desc = "Provide a service that lets any algorithm control its frame rate"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static fpsSlider = optiBase.FindSlider("Desired FPS rate")
        desiredFPS = fpsSlider.value

        Dim fps = CInt(task.fpsAlgorithm / desiredFPS)
        If fps = 0 Then fps = 1
        heartBeat = (task.frameCount Mod fps) = 0
        If heartBeat Then
            saveSkip = skipCount
            skipCount = 0
            If standaloneTest() Then dst2 = src
        Else
            skipCount += 1
        End If
        strOut = "Grid heartbeat set to " + CStr(desiredFPS) + " times per second.  " +
                  CStr(saveSkip) + " frames skipped"
    End Sub
End Class







Public Class Grid_Neighbors : Inherits TaskParent
    Dim mask As New cv.Mat
    Public Sub New()
        labels = {"", "", "Grid_Basics output", ""}
        desc = "Click any grid element to see its neighbors"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.tilesPerCol <> CInt(dst2.Height / 10) Then
            task.gOptions.GridSlider.Value = CInt(dst2.Height / 10)
            task.tilesPerCol = task.gridSize
            task.grid.Run(src)
        End If

        dst2 = src
        If standaloneTest() Then
            If task.heartBeat Then
                task.mouseClickFlag = True
                task.ClickPoint = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            End If
        End If

        SetTrueText("Click any grid entry to see its neighbors", 3)
        If task.optionsChanged Then mask = task.gridMask.Clone

        If task.mouseClickFlag Then
            mask = task.gridMask.Clone
            Dim roiIndex = task.gridMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)

            For Each index In task.gridNeighbors(roiIndex)
                Dim roi = task.gridRects(index)
                mask.Rectangle(roi, white)
            Next
        End If
        dst2.SetTo(white, mask)
    End Sub
End Class







Public Class Grid_Special : Inherits TaskParent
    Public gridWidth As Integer = 10
    Public gridHeight As Integer = 10
    Public gridRects As New List(Of cv.Rect)
    Public tilesPerCol As Integer
    Public tilesPerRow As Integer
    Public gridMask As cv.Mat
    Public gridNeighbors As New List(Of List(Of Integer))
    Public gridMap As cv.Mat
    Public Sub New()
        gridMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        gridMap = New cv.Mat(dst2.Size(), cv.MatType.CV_32S)
        desc = "Grids are normally square.  Grid_Special allows grid elements to be rectangles." +
                "  Specify the Y size."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            gridWidth = task.gridSize
            gridRects.Clear()
            tilesPerCol = 0
            tilesPerRow = 0
            For y = 0 To dst2.Height - 1 Step gridHeight
                For x = 0 To dst2.Width - 1 Step gridWidth
                    Dim roi = New cv.Rect(x, y, gridWidth, gridHeight)
                    If x + roi.Width >= dst2.Width Then roi.Width = dst2.Width - x
                    If y + roi.Height >= dst2.Height Then roi.Height = dst2.Height - y
                    If roi.Width > 0 And roi.Height > 0 Then
                        If x = 0 Then tilesPerCol += 1
                        If y = 0 Then tilesPerRow += 1
                        gridRects.Add(roi)
                    End If
                Next
            Next

            gridMask.SetTo(0)
            For x = gridWidth To dst2.Width - 1 Step gridWidth
                Dim p1 = New cv.Point(x, 0), p2 = New cv.Point(x, dst2.Height)
                gridMask.Line(p1, p2, 255, task.lineWidth)
            Next
            For y = gridHeight To dst2.Height - 1 Step gridHeight
                Dim p1 = New cv.Point(0, y), p2 = New cv.Point(dst2.Width, y)
                gridMask.Line(p1, p2, 255, task.lineWidth)
            Next

            For Each roi In gridRects
                gridMap.Rectangle(roi, gridRects.IndexOf(roi), -1)
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
            dst2.SetTo(white, gridMask)
            labels(2) = "Grid_Basics " + CStr(gridRects.Count) + " (" + CStr(tilesPerCol) + "X" + CStr(tilesPerRow) + ") " +
                          CStr(gridWidth) + "X" + CStr(gridHeight) + " regions"
        End If
    End Sub
End Class







Public Class Grid_MinMaxDepth : Inherits TaskParent
    Public minMaxLocs(0) As linePoints
    Public minMaxVals(0) As cv.Vec2f
    Public Sub New()
        task.gOptions.GridSlider.Value = 8
        UpdateAdvice(traceName + ": goptions 'grid Square Size' has direct impact.")
        desc = "Find the min and max depth within each grid roi."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If minMaxLocs.Count <> task.gridRects.Count Then ReDim minMaxLocs(task.gridRects.Count - 1)
        If minMaxVals.Count <> task.gridRects.Count Then ReDim minMaxVals(task.gridRects.Count - 1)
        Dim mm As mmData
        For i = 0 To minMaxLocs.Count - 1
            Dim roi = task.gridRects(i)
            task.pcSplit(2)(roi).MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, task.depthMask(roi))
            minMaxLocs(i) = New linePoints(mm.minLoc, mm.maxLoc)
            minMaxVals(i) = New cv.Vec2f(mm.minVal, mm.maxVal)
        Next

        If standaloneTest() Then
            dst2.SetTo(0)
            For i = 0 To minMaxLocs.Count - 1
                Dim lp = minMaxLocs(i)
                DrawCircle(dst2(task.gridRects(i)), lp.p2, task.DotSize, cv.Scalar.Red)
                DrawCircle(dst2(task.gridRects(i)), lp.p1, task.DotSize, white)
            Next
            dst2.SetTo(white, task.gridMask)
        End If
    End Sub
End Class







Public Class Grid_TrackCenter : Inherits TaskParent
    Public center As cv.Point
    Dim match As New Match_Basics
    Public Sub New()
        If standalone Then task.gOptions.ShowGrid.Checked = True

        desc = "Track a cell near the center of the grid"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If match.correlation < match.options.correlationMin Or task.gOptions.DebugCheckBox.Checked Then
            task.gOptions.DebugCheckBox.Checked = False
            Dim index = task.gridMap.Get(Of Integer)(dst2.Height / 2, dst2.Width / 2)
            Dim roi = task.gridRects(index)
            match.template = src(roi).Clone
            center = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
        End If

        Dim templatePad = match.options.templatePad
        Dim templateSize = match.options.templateSize
        match.searchRect = ValidateRect(New cv.Rect(center.X - templatePad, center.Y - templatePad, templateSize, templateSize))
        match.Run(src)
        center = match.matchCenter

        If standaloneTest() Then
            dst2 = src
            dst2.Rectangle(match.matchRect, task.HighlightColor, task.lineWidth + 1, task.lineType)
            DrawCircle(dst2, center, task.DotSize, white)

            If task.heartBeat Then dst3.SetTo(0)
            DrawCircle(dst3, center, task.DotSize, task.HighlightColor)
            SetTrueText(Format(match.correlation, fmt3), center, 3)

            labels(3) = "Match correlation = " + Format(match.correlation, fmt3)
        End If
    End Sub
End Class





Public Class Grid_ShowMap : Inherits TaskParent
    Public Sub New()
        desc = "Verify that task.gridMap is laid out correctly"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = ShowPalette(task.gridMap * 255 / task.gridRects.Count)
    End Sub
End Class