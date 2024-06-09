Imports cv = OpenCvSharp
Imports System.Threading
Public Class Grid_Basics : Inherits VB_Parent
    Public gridList As New List(Of cv.Rect)
    Public updateTaskGridList As Boolean = True
    Public Sub New()
        desc = "Create a grid of squares covering the entire image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.mouseClickFlag And Not task.firstPass Then
            task.gridROIclicked = task.gridMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        End If
        If task.optionsChanged Then
            task.gridMask = New cv.Mat(src.Size(), cv.MatType.CV_8U)
            task.gridMap = New cv.Mat(src.Size(), cv.MatType.CV_32S, 255)

            gridList.Clear()
            task.gridIndex.Clear()
            task.gridRows = 0
            task.gridCols = 0
            Dim index As Integer
            For y = 0 To src.Height - 1 Step task.gOptions.GridSize.Value
                For x = 0 To src.Width - 1 Step task.gOptions.GridSize.Value
                    Dim roi = validateRect(New cv.Rect(x, y, task.gOptions.GridSize.Value, task.gOptions.GridSize.Value))
                    If roi.Width > 0 And roi.Height > 0 Then
                        If x = 0 Then task.gridRows += 1
                        If y = 0 Then task.gridCols += 1
                        gridList.Add(roi)
                        task.gridIndex.Add(index)
                        index += 1
                    End If
                Next
            Next
            task.subDivisionCount = 9

            If task.color Is Nothing Then Exit Sub ' startup condition.

            If src.Size = task.color.Size Then
                task.gridMask.SetTo(0)
                For x = task.gOptions.GridSize.Value To src.Width - 1 Step task.gOptions.GridSize.Value
                    Dim p1 = New cv.Point(x, 0), p2 = New cv.Point(x, src.Height)
                    task.gridMask.Line(p1, p2, 255, task.lineWidth)
                Next
                For y = task.gOptions.GridSize.Value To src.Height - 1 Step task.gOptions.GridSize.Value
                    Dim p1 = New cv.Point(0, y), p2 = New cv.Point(src.Width, y)
                    task.gridMask.Line(p1, p2, 255, task.lineWidth)
                Next

                For i = 0 To gridList.Count - 1
                    Dim roi = gridList(i)
                    task.gridMap.Rectangle(roi, i, -1)
                Next

                task.gridNeighbors.Clear()
                For Each roi In gridList
                    task.gridNeighbors.Add(New List(Of Integer))
                    For i = 0 To 8
                        Dim x = Choose(i + 1, roi.X - 1, roi.X, roi.X + roi.Width + 1,
                                          roi.X - 1, roi.X, roi.X + roi.Width + 1,
                                          roi.X - 1, roi.X, roi.X + roi.Width + 1)
                        Dim y = Choose(i + 1, roi.Y - 1, roi.Y - 1, roi.Y - 1, roi.Y, roi.Y, roi.Y,
                                          roi.Y + roi.Height + 1, roi.Y + roi.Height + 1, roi.Y + roi.Height + 1)
                        If x >= 0 And x < src.Width And y >= 0 And y < src.Height Then
                            task.gridNeighbors(task.gridNeighbors.Count - 1).Add(task.gridMap.Get(Of Integer)(y, x))
                        End If
                    Next
                Next
            End If

            For Each roi In gridList
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

        End If
        If standaloneTest() Then
            dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8U)
            task.color.CopyTo(dst2)
            dst2.SetTo(cv.Scalar.White, task.gridMask)
            labels(2) = "Grid_Basics " + CStr(gridList.Count) + " (" + CStr(task.gridRows) + "X" + CStr(task.gridCols) + ") " +
                              CStr(task.gOptions.GridSize.Value) + "X" + CStr(task.gOptions.GridSize.Value) + " regions"
        End If

        If updateTaskGridList Then task.gridList = gridList
    End Sub
End Class






Public Class Grid_BasicsTest : Inherits VB_Parent
    Public Sub New()
        labels = {"", "", "Each grid element is assigned a value below", "The line is the diagonal for each roi.  Bottom might be a shortened roi."}
        If standaloneTest() Then desc = "Validation test for Grid_Basics algorithm"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim mean = cv.Cv2.Mean(src)

        dst2.SetTo(0)
        ' SetTrueText is not thread-safe...
        'Parallel.For(0, task.gridList.Count,
        ' Sub(i)
        For i = 0 To task.gridList.Count - 1
            Dim roi = task.gridList(i)
            cv.Cv2.Subtract(mean, src(roi), dst2(roi))
            setTrueText(CStr(i), New cv.Point(roi.X, roi.Y))
        Next
        'End Sub)
        dst2.SetTo(cv.Scalar.White, task.gridMask)

        dst3.SetTo(0)
        Parallel.For(0, task.gridList.Count,
         Sub(i)
             Dim roi = task.gridList(i)
             cv.Cv2.Subtract(mean, src(roi), dst3(roi))
             DrawLine(dst3(roi), New cv.Point(0, 0), New cv.Point(roi.Width, roi.Height), cv.Scalar.White)
         End Sub)
    End Sub
End Class










Public Class Grid_List : Inherits VB_Parent
    Public Sub New()
        labels(2) = "Adjust grid width/height to increase thread count."
        If standaloneTest() Then desc = "List the active threads"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Parallel.ForEach(Of cv.Rect)(task.gridList,
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
            setTrueText("There were " + CStr(threadCount) + " threads in OpenCVB with " + CStr(notIdle) + " of them not idle when traversing the gridList" + vbCrLf + str)
        Catch e As Exception
            MsgBox(e.Message)
        End Try
    End Sub
End Class








Public Class Grid_Rectangles : Inherits VB_Parent
    Public tilesPerRow As Integer
    Public tilesPerCol As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Grid Cell Width", 1, dst2.Width, 32)
            sliders.setupTrackBar("Grid Cell Height", 1, dst2.Height, 32)
        End If

        task.gridMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        task.gridMap = New cv.Mat(dst2.Size(), cv.MatType.CV_32S)
        If standaloneTest() Then desc = "Create a grid of rectangles (not necessarily squares) for use with parallel.For"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static widthSlider = FindSlider("Grid Cell Width")
        Static heightSlider = FindSlider("Grid Cell Height")
        Dim width = widthSlider.Value
        Dim height = heightSlider.Value

        If task.mouseClickFlag Then task.gridROIclicked = task.gridMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        If task.optionsChanged Then
            task.gridList.Clear()
            For y = 0 To dst2.Height - 1 Step height
                For x = 0 To dst2.Width - 1 Step width
                    Dim roi = New cv.Rect(x, y, width, height)
                    If x + roi.Width >= dst2.Width Then roi.Width = dst2.Width - x
                    If y + roi.Height >= dst2.Height Then roi.Height = dst2.Height - y
                    If roi.Width > 0 And roi.Height > 0 Then
                        If y = 0 Then tilesPerRow += 1
                        If x = 0 Then tilesPerCol += 1
                        task.gridList.Add(roi)
                    End If
                Next
            Next

            task.gridMask.SetTo(0)
            For x = width To dst2.Width - 1 Step width
                Dim p1 = New cv.Point(CInt(x), 0), p2 = New cv.Point(CInt(x), dst2.Height)
                task.gridMask.Line(p1, p2, 255, task.lineWidth)
            Next
            For y = height To dst2.Height - 1 Step height
                Dim p1 = New cv.Point(0, CInt(y)), p2 = New cv.Point(dst2.Width, CInt(y))
                task.gridMask.Line(p1, p2, 255, task.lineWidth)
            Next

            For i = 0 To task.gridList.Count - 1
                Dim roi = task.gridList(i)
                task.gridMap.Rectangle(roi, i, -1)
            Next
        End If
        If standaloneTest() Then
            task.color.CopyTo(dst2)
            dst2.SetTo(cv.Scalar.White, task.gridMask)
            labels(2) = "Grid_Basics " + CStr(task.gridList.Count) + " (" + CStr(tilesPerRow) + "X" + CStr(tilesPerCol) + ") " +
                          CStr(width) + "X" + CStr(height) + " regions"
        End If
    End Sub
End Class






Public Class Grid_FPS : Inherits VB_Parent
    Public heartBeat As Boolean
    Public fpsSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Desired FPS rate", 1, 10, 2)
        fpsSlider = FindSlider("Desired FPS rate")
        desc = "Provide a service that lets any algorithm control its frame rate"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim fps = CInt(task.fpsRate / fpsSlider.Value)
        If fps = 0 Then fps = 1
        heartBeat = (task.frameCount Mod fps) = 0
        Static skipCount As Integer
        Static saveSkip As Integer
        If heartBeat Then
            saveSkip = skipCount
            skipCount = 0
            If standaloneTest() Then dst2 = src
        Else
            skipCount += 1
        End If
        strOut = "Grid heartbeat set to " + CStr(fpsSlider.Value) + " times per second.  " + CStr(saveSkip) + " frames skipped"
    End Sub
End Class







Public Class Grid_Neighbors : Inherits VB_Parent
    Public Sub New()
        labels = {"", "", "Grid_Basics output", ""}
        desc = "Click any grid element to see its neighbors"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.gridRows <> CInt(dst2.Height / 10) Then
            task.gOptions.setGridSize(CInt(dst2.Height / 10))
            task.gridRows = task.gOptions.GridSize.Value
            task.grid.Run(src)
        End If

        dst2 = src
        If standaloneTest() Then
            If task.heartBeat Then
                task.mouseClickFlag = True
                task.clickPoint = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            End If
        End If

        setTrueText("Click any grid entry to see its neighbors", 3)
        Static mask As New cv.Mat
        If task.optionsChanged Then mask = task.gridMask.Clone

        If task.mouseClickFlag Then
            mask = task.gridMask.Clone
            Dim roiIndex = task.gridMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)

            For Each index In task.gridNeighbors(roiIndex)
                Dim roi = task.gridList(index)
                mask.Rectangle(roi, cv.Scalar.White)
            Next
        End If
        dst2.SetTo(cv.Scalar.White, mask)
    End Sub
End Class







Public Class Grid_Special : Inherits VB_Parent
    Public gridWidth As Integer = 10
    Public gridHeight As Integer = 10
    Public gridList As New List(Of cv.Rect)
    Public gridRows As Integer
    Public gridCols As Integer
    Public gridMask As cv.Mat
    Public gridNeighbors As New List(Of List(Of Integer))
    Public gridMap As cv.Mat
    Public Sub New()
        gridMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        gridMap = New cv.Mat(dst2.Size(), cv.MatType.CV_32S)
        desc = "Grids are normally square.  Grid_Special allows grid elements to be rectangles.  Specify the Y size."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.optionsChanged Then
            gridWidth = task.gOptions.GridSize.Value
            gridList.Clear()
            gridRows = 0
            gridCols = 0
            For y = 0 To dst2.Height - 1 Step gridHeight
                For x = 0 To dst2.Width - 1 Step gridWidth
                    Dim roi = New cv.Rect(x, y, gridWidth, gridHeight)
                    If x + roi.Width >= dst2.Width Then roi.Width = dst2.Width - x
                    If y + roi.Height >= dst2.Height Then roi.Height = dst2.Height - y
                    If roi.Width > 0 And roi.Height > 0 Then
                        If x = 0 Then gridRows += 1
                        If y = 0 Then gridCols += 1
                        gridList.Add(roi)
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

            For i = 0 To task.gridList.Count - 1
                Dim roi = gridList(i)
                gridMap.Rectangle(roi, i, -1)
            Next

            gridNeighbors.Clear()
            For Each roi In gridList
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
            dst2.SetTo(cv.Scalar.White, gridMask)
            labels(2) = "Grid_Basics " + CStr(gridList.Count) + " (" + CStr(gridRows) + "X" + CStr(gridCols) + ") " +
                          CStr(gridWidth) + "X" + CStr(gridHeight) + " regions"
        End If
    End Sub
End Class







Public Class Grid_QuarterRes : Inherits VB_Parent
    Public gridList As New List(Of cv.Rect)
    Dim grid As New Grid_Basics
    Public Sub New()
        grid.updateTaskGridList = False
        desc = "Provide the grid list for the lowest resolution of the current stream."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static inputSrc As New cv.Mat(task.quarterRes, cv.MatType.CV_8U, 0)
        grid.Run(inputSrc)
        gridList = grid.gridList
    End Sub
End Class







Public Class Grid_LowRes : Inherits VB_Parent
    Public gridList As New List(Of cv.Rect)
    Dim grid As New Grid_Basics
    Public Sub New()
        grid.updateTaskGridList = False
        desc = "Provide the grid list for the lowest resolution of the current stream."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static inputSrc As New cv.Mat(task.lowRes, cv.MatType.CV_8U, 0)
        grid.Run(inputSrc)
        gridList = grid.gridList
    End Sub
End Class







Public Class Grid_MinMaxDepth : Inherits VB_Parent
    Public minMaxLocs(0) As pointPair
    Public minMaxVals(0) As cv.Vec2f
    Public Sub New()
        task.gOptions.setGridSize(8)
        UpdateAdvice(traceName + ": goptions 'Grid Square Size' has direct impact.")
        desc = "Find the min and max depth within each grid roi."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If minMaxLocs.Count <> task.gridList.Count Then ReDim minMaxLocs(task.gridList.Count - 1)
        If minMaxVals.Count <> task.gridList.Count Then ReDim minMaxVals(task.gridList.Count - 1)
        Dim mm As mmData
        For i = 0 To minMaxLocs.Count - 1
            Dim roi = task.gridList(i)
            task.pcSplit(2)(roi).MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, task.depthMask(roi))
            minMaxLocs(i) = New pointPair(mm.minLoc, mm.maxLoc)
            minMaxVals(i) = New cv.Vec2f(mm.minVal, mm.maxVal)
        Next

        If standaloneTest() Then
            dst2.SetTo(0)
            For i = 0 To minMaxLocs.Count - 1
                Dim lp = minMaxLocs(i)
                DrawCircle(dst2(task.gridList(i)), lp.p2, task.dotSize, cv.Scalar.Red)
                DrawCircle(dst2(task.gridList(i)), lp.p1, task.dotSize, cv.Scalar.White)
            Next
            dst2.SetTo(cv.Scalar.White, task.gridMask)
        End If
    End Sub
End Class







Public Class Grid_TrackCenter : Inherits VB_Parent
    Public center As cv.Point
    Dim match As New Match_Basics
    Public Sub New()
        If standalone Then task.gOptions.ShowGrid.Checked = True

        desc = "Track a cell near the center of the grid"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If match.correlation < match.options.correlationMin Or task.gOptions.DebugChecked Then
            task.gOptions.DebugChecked = False
            Dim index = task.gridMap.Get(Of Integer)(dst2.Height / 2, dst2.Width / 2)
            Dim roi = task.gridList(index)
            match.template = src(roi).Clone
            center = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
        End If

        Dim templatePad = match.options.templatePad
        Dim templateSize = match.options.templateSize
        match.searchRect = validateRect(New cv.Rect(center.X - templatePad, center.Y - templatePad, templateSize, templateSize))
        match.Run(src)
        center = match.matchCenter

        If standaloneTest() Then
            dst2 = src
            dst2.Rectangle(match.matchRect, task.highlightColor, task.lineWidth + 1, task.lineType)
            DrawCircle(dst2, center, task.dotSize, cv.Scalar.White)

            If task.heartBeat Then dst3.SetTo(0)
            DrawCircle(dst3, center, task.dotSize, task.highlightColor)
            setTrueText(Format(match.correlation, fmt3), center, 3)

            labels(3) = "Match correlation = " + Format(match.correlation, fmt3)
        End If
    End Sub
End Class





Public Class Grid_ShowMap : Inherits VB_Parent
    Public Sub New()
        desc = "Verify that task.gridMap is laid out correctly"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.gridMap.ConvertTo(dst2, cv.MatType.CV_8U)
        dst3 = ShowPalette(dst2)
    End Sub
End Class
