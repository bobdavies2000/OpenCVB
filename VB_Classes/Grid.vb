Imports cv = OpenCvSharp
Imports System.Threading
Imports System.Runtime.InteropServices
Imports System.Collections.Concurrent
Public Class Grid_Basics : Inherits VB_Algorithm
    Public Sub New()
        desc = "Create a grid of squares covering the entire image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.mouseClickFlag And firstPass = False Then
            task.gridROIclicked = task.gridToRoiIndex.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        End If
        If task.optionsChanged Then
            task.gridMask = New cv.Mat(src.Size(), cv.MatType.CV_8U)
            task.gridToRoiIndex = New cv.Mat(src.Size(), cv.MatType.CV_32S)

            task.gridList.Clear()
            task.gridRows = 0
            task.gridCols = 0
            For y = 0 To src.Height - 1 Step gOptions.GridSize.Value
                For x = 0 To src.Width - 1 Step gOptions.GridSize.Value
                    Dim roi = New cv.Rect(x, y, gOptions.GridSize.Value, gOptions.GridSize.Value)
                    If x + roi.Width >= src.Width Then roi.Width = src.Width - x
                    If y + roi.Height >= src.Height Then roi.Height = src.Height - y
                    If roi.Width > 0 And roi.Height > 0 Then
                        If x = 0 Then task.gridRows += 1
                        If y = 0 Then task.gridCols += 1
                        task.gridList.Add(roi)
                    End If
                Next
            Next

            task.gridMask.SetTo(0)
            For x = gOptions.GridSize.Value To src.Width - 1 Step gOptions.GridSize.Value
                Dim p1 = New cv.Point(x, 0), p2 = New cv.Point(x, src.Height)
                task.gridMask.Line(p1, p2, 255, task.lineWidth)
            Next
            For y = gOptions.GridSize.Value To src.Height - 1 Step gOptions.GridSize.Value
                Dim p1 = New cv.Point(0, y), p2 = New cv.Point(src.Width, y)
                task.gridMask.Line(p1, p2, 255, task.lineWidth)
            Next

            For i = 0 To task.gridList.Count - 1
                Dim roi = task.gridList(i)
                task.gridToRoiIndex.Rectangle(roi, i, -1)
            Next

            task.gridNeighbors.Clear()
            For Each roi In task.gridList
                task.gridNeighbors.Add(New List(Of Integer))
                For i = 0 To 8
                    Dim x = Choose(i + 1, roi.X - 1, roi.X, roi.X + roi.Width + 1,
                                          roi.X - 1, roi.X, roi.X + roi.Width + 1,
                                          roi.X - 1, roi.X, roi.X + roi.Width + 1)
                    Dim y = Choose(i + 1, roi.Y - 1, roi.Y - 1, roi.Y - 1, roi.Y, roi.Y, roi.Y,
                                          roi.Y + roi.Height + 1, roi.Y + roi.Height + 1, roi.Y + roi.Height + 1)
                    If x >= 0 And x < src.Width And y >= 0 And y < src.Height Then
                        task.gridNeighbors(task.gridNeighbors.Count - 1).Add(task.gridToRoiIndex.Get(Of Integer)(y, x))
                    End If
                Next
            Next

        End If
        If standalone Then
            dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8U)
            task.color.CopyTo(dst2)
            dst2.SetTo(cv.Scalar.White, task.gridMask)
            labels(2) = "Grid_Basics " + CStr(task.gridList.Count) + " (" + CStr(task.gridRows) + "X" + CStr(task.gridCols) + ") " +
                          CStr(gOptions.GridSize.Value) + "X" + CStr(gOptions.GridSize.Value) + " regions"
        End If
    End Sub
End Class






Public Class Grid_BasicsTest : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "", "Each grid element is assigned a value below", "The line is the diagonal for each roi.  Bottom might be a shortened roi."}
        If standalone Then desc = "Validation test for Grid_Basics algorithm"
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
             dst3(roi).Line(New cv.Point(0, 0), New cv.Point(roi.Width, roi.Height), cv.Scalar.White, task.lineWidth, task.lineType)
         End Sub)
    End Sub
End Class










Public Class Grid_List : Inherits VB_Algorithm
    Public Sub New()
        labels(2) = "Adjust grid width/height to increase thread count."
        If standalone Then desc = "List the active threads"
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








Public Class Grid_Rectangles : Inherits VB_Algorithm
    Public tilesPerRow As Integer
    Public tilesPerCol As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Grid Cell Width", 1, dst2.Width, 32)
            sliders.setupTrackBar("Grid Cell Height", 1, dst2.Height, 32)
        End If

        task.gridMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        task.gridToRoiIndex = New cv.Mat(dst2.Size(), cv.MatType.CV_32S)
        If standalone Then desc = "Create a grid of rectangles (not necessarily squares) for use with parallel.For"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static widthSlider = findSlider("Grid Cell Width")
        Static heightSlider = findSlider("Grid Cell Height")
        Dim width = widthSlider.Value
        Dim height = heightSlider.Value

        If task.mouseClickFlag Then task.gridROIclicked = task.gridToRoiIndex.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
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
                task.gridToRoiIndex.Rectangle(roi, i, -1)
            Next
        End If
        If standalone Then
            task.color.CopyTo(dst2)
            dst2.SetTo(cv.Scalar.White, task.gridMask)
            labels(2) = "Grid_Basics " + CStr(task.gridList.Count) + " (" + CStr(tilesPerRow) + "X" + CStr(tilesPerCol) + ") " +
                          CStr(width) + "X" + CStr(height) + " regions"
        End If
    End Sub
End Class









Public Class Grid_TrackCenter : Inherits VB_Algorithm
    Dim match As New Match_Basics
    Public Sub New()
        If standalone Then gOptions.GridSize.Value = dst2.Width / 10
        desc = "Track a cell near the center of the grid"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static lastImage As cv.Mat = src.Clone

        If match.correlation < 0.8 Then
            Dim index = task.gridCols * (task.gridRows / 2) + task.gridCols / 2
            match.drawRect = task.gridList(index)
            match.template = lastImage(match.drawRect).Clone
        End If

        ' If task.motionFlag = False Then
        match.Run(src)
        dst3 = match.displayResults()
        ' End If
        Static lastPoint As cv.Point = match.matchCenter

        dst2 = src
        dst2.Rectangle(match.drawRect, task.highlightColor, task.lineWidth, task.lineType)

        lastImage = src.Clone
        lastPoint = match.matchCenter
        dst2.SetTo(cv.Scalar.White, task.gridMask)
        labels(3) = "Match correlation = " + Format(match.correlation, fmt3)
    End Sub
End Class






Public Class Grid_FPS : Inherits VB_Algorithm
    Public heartBeat As Boolean
    Public fpsSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Desired FPS rate", 1, 10, 2)
        fpsSlider = findSlider("Desired FPS rate")
        desc = "Provide a service that lets any algorithm control its frame rate"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim fps = CInt(task.fpsRate / fpsSlider.Value)
        If fps = 0 Then fps = 1
        HeartBeat = (task.frameCount Mod fps) = 0
        Static skipCount As Integer
        Static saveSkip As Integer
        If heartBeat Then
            saveSkip = skipCount
            skipCount = 0
            If standalone Then dst2 = src
        Else
            skipCount += 1
        End If
        strOut = "Grid heartbeat set to " + CStr(fpsSlider.value) + " times per second.  " + CStr(saveSkip) + " frames skipped"
    End Sub
End Class







Public Class Grid_Neighbors : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "", "Grid_Basics output", ""}
        desc = "Click any grid element to see its neighbors"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.gridRows <> CInt(dst2.Height / 10) Then
            gOptions.GridSize.Value = CInt(dst2.Height / 10)
            task.gridRows = gOptions.GridSize.Value
            task.grid.Run(src)
        End If

        dst2 = src
        If standalone Then
            If heartBeat() Then
                task.mouseClickFlag = True
                task.clickPoint = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            End If
        End If

        setTrueText("Click any grid entry to see its neighbors", 3)
        Static mask As New cv.Mat
        If task.optionsChanged Then mask = task.gridMask.Clone

        If task.mouseClickFlag Then
            mask = task.gridMask.Clone
            Dim roiIndex = task.gridToRoiIndex.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)

            For Each index In task.gridNeighbors(roiIndex)
                Dim roi = task.gridList(index)
                mask.Rectangle(roi, cv.Scalar.White, -1, task.lineType)
            Next
        End If
        dst2.SetTo(cv.Scalar.White, mask)
    End Sub
End Class







Public Class Grid_Special : Inherits VB_Algorithm
    Public gridWidth As Integer = 10
    Public gridHeight As Integer = 10
    Public gridList As New List(Of cv.Rect)
    Public gridRows As Integer
    Public gridCols As Integer
    Public gridMask As cv.Mat
    Public gridNeighbors As New List(Of List(Of Integer))
    Public gridToRoiIndex As cv.Mat
    Public Sub New()
        gridMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        gridToRoiIndex = New cv.Mat(dst2.Size(), cv.MatType.CV_32S)
        desc = "Grids are normally square.  Grid_Special allows grid elements to be rectangles.  Specify the Y size."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.optionsChanged Then
            gridWidth = gOptions.GridSize.Value
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
                gridToRoiIndex.Rectangle(roi, i, -1)
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
                        gridNeighbors(gridNeighbors.Count - 1).Add(gridToRoiIndex.Get(Of Integer)(y, x))
                    End If
                Next
            Next

        End If
        If standalone Then
            task.color.CopyTo(dst2)
            dst2.SetTo(cv.Scalar.White, gridMask)
            labels(2) = "Grid_Basics " + CStr(gridList.Count) + " (" + CStr(gridRows) + "X" + CStr(gridCols) + ") " +
                          CStr(gridWidth) + "X" + CStr(gridHeight) + " regions"
        End If
    End Sub
End Class