﻿Imports cv = OpenCvSharp
Imports System.Threading
Imports System.Windows.Forms
Public Class Grid_Basics : Inherits TaskParent
    Public gridMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
    Public brickList As New List(Of brickData)
    Public Sub New()
        task.gridMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        desc = "Create a grid of squares covering the entire image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.mouseClickFlag And Not task.firstPass Then
            task.gridROIclicked = gridMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
        End If

        Dim cellSize = task.gOptions.GridSlider.Value
        If task.optionsChanged Then
            Dim bricksPerCol As Integer, bricksPerRow As Integer
            task.gridNabeRects.Clear()
            task.gridNeighbors.Clear()

            task.gridRects.Clear()
            Dim index As Integer
            For y = 0 To dst2.Height - 1 Step cellSize
                For x = 0 To dst2.Width - 1 Step cellSize
                    Dim roi = ValidateRect(New cv.Rect(x, y, cellSize, cellSize))

                    If roi.Bottom = dst2.Height - 1 Then roi.Height += 1
                    If roi.BottomRight.X = dst2.Width - 1 Then roi.Width += 1

                    If roi.Width > 0 And roi.Height > 0 Then
                        If x = 0 Then bricksPerCol += 1
                        If y = 0 Then bricksPerRow += 1
                        task.gridRects.Add(roi)
                        index += 1
                    End If
                Next
            Next

            task.gridMask.SetTo(0)
            For x = cellSize To dst2.Width - 1 Step cellSize
                Dim p1 = New cv.Point(x, 0), p2 = New cv.Point(x, dst2.Height)
                task.gridMask.Line(p1, p2, 255, task.lineWidth)
            Next
            For y = cellSize To dst2.Height - 1 Step cellSize
                Dim p1 = New cv.Point(0, y), p2 = New cv.Point(dst2.Width, y)
                task.gridMask.Line(p1, p2, 255, task.lineWidth)
            Next

            For i = 0 To task.gridRects.Count - 1
                Dim roi = task.gridRects(i)
                gridMap.Rectangle(roi, i, -1)
            Next

            For j = 0 To task.gridRects.Count - 1
                Dim roi = task.gridRects(j)
                Dim nextList As New List(Of Integer)
                nextList.Add(j)
                For i = 0 To 8
                    Dim x = Choose(i + 1, roi.X - 1, roi.X, roi.X + roi.Width + 1,
                                              roi.X - 1, roi.X, roi.X + roi.Width + 1,
                                              roi.X - 1, roi.X, roi.X + roi.Width + 1)
                    Dim y = Choose(i + 1, roi.Y - 1, roi.Y - 1, roi.Y - 1, roi.Y, roi.Y, roi.Y,
                                              roi.Y + roi.Height + 1, roi.Y + roi.Height + 1, roi.Y + roi.Height + 1)
                    If x >= 0 And x < dst2.Width And y >= 0 And y < dst2.Height Then
                        Dim nextIndex As Integer = gridMap.Get(Of Single)(y, x)
                        If nextList.Contains(nextIndex) = False Then nextList.Add(nextIndex)
                    End If
                Next
                task.gridNeighbors.Add(nextList)
            Next

            For Each nabeList In task.gridNeighbors
                Dim xList As New List(Of Integer), yList As New List(Of Integer)
                For Each index In nabeList
                    Dim roi = task.gridRects(index)
                    xList.Add(roi.X)
                    yList.Add(roi.Y)
                    xList.Add(roi.BottomRight.X)
                    yList.Add(roi.BottomRight.Y)
                Next
                Dim r = New cv.Rect(xList.Min, yList.Min, xList.Max - xList.Min, yList.Max - yList.Min)
                If r.Width < cellSize * 3 Then
                    If r.X + r.Width >= dst2.Width Then r.X = dst2.Width - cellSize * 3
                    r.Width = cellSize * 3
                End If
                If r.Height < cellSize * 3 Then
                    If r.Y + r.Height >= dst2.Height Then r.Y = dst2.Height - cellSize * 3
                    r.Height = cellSize * 3
                End If
                If r.Width <> cellSize * 3 Then r.Width = cellSize * 3
                If r.Height <> cellSize * 3 Then r.Height = cellSize * 3
                task.gridNabeRects.Add(r)
            Next

            task.brickSize = cellSize
            task.bricksPerCol = bricksPerCol
            task.bricksPerRow = bricksPerRow
        End If
        If standaloneTest() Then
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
            task.color.CopyTo(dst2)
            dst2.SetTo(white, task.gridMask)
            labels(2) = "Grid_Basics " + CStr(task.gridRects.Count) + " (" + CStr(task.bricksPerCol) + "X" + CStr(task.bricksPerRow) + ") " +
                                         CStr(cellSize) + "X" + CStr(cellSize) + " regions"
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
            MessageBox.Show(e.Message)
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
        Static fpsSlider = OptionParent.FindSlider("Desired FPS rate")
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
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        task.ClickPoint = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        desc = "Click any grid element to see its neighbors"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src

        SetTrueText("Click any grid entry to see its neighbors", 3)
        dst2.SetTo(white, task.gridMask)

        Dim roiIndex As Integer = task.grid.gridMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)

        dst3.SetTo(0)
        For Each index In task.gridNeighbors(roiIndex)
            Dim roi = task.gridRects(index)
            dst2.Rectangle(roi, white, task.lineWidth)
            dst3.Rectangle(roi, 255, task.lineWidth)
        Next
    End Sub
End Class








Public Class Grid_MinMaxDepth : Inherits TaskParent
    Public minMaxLocs(0) As lpData
    Public minMaxVals(0) As cv.Vec2f
    Public Sub New()
        task.gOptions.GridSlider.Value = 8
        desc = "Find the min and max depth within each grid roi."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If minMaxLocs.Count <> task.gridRects.Count Then ReDim minMaxLocs(task.gridRects.Count - 1)
        If minMaxVals.Count <> task.gridRects.Count Then ReDim minMaxVals(task.gridRects.Count - 1)
        Dim mm As mmData
        For i = 0 To minMaxLocs.Count - 1
            Dim roi = task.gridRects(i)
            task.pcSplit(2)(roi).MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, task.depthMask(roi))
            minMaxLocs(i) = New lpData(mm.minLoc, mm.maxLoc)
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
        If match.correlation < task.fCorrThreshold Or task.gOptions.DebugCheckBox.Checked Then
            task.gOptions.DebugCheckBox.Checked = False
            Dim index As Integer = task.grid.gridMap.Get(Of Single)(dst2.Height / 2, dst2.Width / 2)
            Dim roi = task.gridRects(index)
            match.template = src(roi).Clone
            center = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
        End If

        Dim pad = task.brickSize / 2
        Dim searchRect = ValidateRect(New cv.Rect(center.X - pad, center.Y - pad, task.brickSize, task.brickSize))
        match.Run(src(searchRect))
        center = match.newCenter

        If standaloneTest() Then
            dst2 = src
            dst2.Rectangle(match.newRect, task.highlight, task.lineWidth + 1, task.lineType)
            DrawCircle(dst2, center, task.DotSize, white)

            If task.heartBeat Then dst3.SetTo(0)
            DrawCircle(dst3, center, task.DotSize, task.highlight)
            SetTrueText(Format(match.correlation, fmt3), center, 3)

            labels(3) = "Match correlation = " + Format(match.correlation, fmt3)
        End If
    End Sub
End Class







Public Class Grid_Rectangles : Inherits TaskParent
    Public gridWidth As Integer = 10
    Public gridHeight As Integer = 10
    Public gridRects As New List(Of cv.Rect)
    Public gridMap As New cv.Mat
    Public bricksPerCol As Integer
    Public bricksPerRow As Integer
    Public gridMask As cv.Mat
    Public gridNeighbors As New List(Of List(Of Integer))
    Public Sub New()
        gridMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        gridMap = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        desc = "Grids are normally square.  Grid_Special allows grid elements to be rectangles." +
                "  Specify the Y size."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            gridRects.Clear()
            bricksPerCol = 0
            bricksPerRow = 0
            For y = 0 To dst2.Height - 1 Step gridHeight
                For x = 0 To dst2.Width - 1 Step gridWidth
                    Dim roi = New cv.Rect(x, y, gridWidth, gridHeight)
                    If x + roi.Width >= dst2.Width Then roi.Width = dst2.Width - x
                    If y + roi.Height >= dst2.Height Then roi.Height = dst2.Height - y
                    If roi.Width > 0 And roi.Height > 0 Then
                        If x = 0 Then bricksPerCol += 1
                        If y = 0 Then bricksPerRow += 1
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
            labels(2) = "Grid_Basics " + CStr(gridRects.Count) + " (" + CStr(bricksPerCol) + "X" + CStr(bricksPerRow) + ") " +
                          CStr(gridWidth) + "X" + CStr(gridHeight) + " regions"
        End If
    End Sub
End Class