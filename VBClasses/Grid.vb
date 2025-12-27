Imports cv = OpenCvSharp
Imports System.Threading
Namespace VBClasses
    Public Class Grid_Basics : Inherits TaskParent
        Public brickList As New List(Of brickData)
        Public gridNeighbors As New List(Of List(Of Integer))
        Public Sub New()
            taskAlg.gridMap = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
            taskAlg.gridMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
            desc = "Create a grid of squares covering the entire image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.mouseClickFlag And Not taskAlg.firstPass Then
                taskAlg.gridROIclicked = taskAlg.gridMap.Get(Of Single)(taskAlg.clickPoint.Y, taskAlg.clickPoint.X)
            End If

            If taskAlg.optionsChanged Then
                Dim bricksPerCol As Integer, bricksPerRow As Integer
                taskAlg.gridNabeRects.Clear()
                gridNeighbors.Clear()

                taskAlg.gridRects.Clear()
                Dim index As Integer
                For y = 0 To dst2.Height - 1 Step taskAlg.brickSize
                    For x = 0 To dst2.Width - 1 Step taskAlg.brickSize
                        Dim roi = ValidateRect(New cv.Rect(x, y, taskAlg.brickSize, taskAlg.brickSize))

                        If roi.Bottom = dst2.Height - 1 Then roi.Height += 1
                        If roi.BottomRight.X = dst2.Width - 1 Then roi.Width += 1

                        If roi.Width > 0 And roi.Height > 0 Then
                            If x = 0 Then bricksPerCol += 1
                            If y = 0 Then bricksPerRow += 1
                            taskAlg.gridRects.Add(roi)
                            index += 1
                        End If
                    Next
                Next

                taskAlg.gridMask.SetTo(0)
                For x = taskAlg.brickSize To dst2.Width - 1 Step taskAlg.brickSize
                    Dim p1 = New cv.Point(x, 0), p2 = New cv.Point(x, dst2.Height)
                    taskAlg.gridMask.Line(p1, p2, 255, 1)
                Next
                For y = taskAlg.brickSize To dst2.Height - 1 Step taskAlg.brickSize
                    Dim p1 = New cv.Point(0, y), p2 = New cv.Point(dst2.Width, y)
                    taskAlg.gridMask.Line(p1, p2, 255, 1)
                Next

                For i = 0 To taskAlg.gridRects.Count - 1
                    taskAlg.gridMap.Rectangle(taskAlg.gridRects(i), i, -1)
                Next

                ' This determines which grid rects are replaced when motion is detected.
                ' linkType = 1 means that only the grid rect is copied (the first entry)
                ' linkType = 4 means link4 gridrects and the original rect are copied (first 5 entries)
                ' linkType = 8 means link8 gridrects and the original rect are copied (all entries)
                ' After some testing, it appears that link4 is adequate.  More testing needed.
                taskAlg.motionLinkType = 4
                For i = 0 To taskAlg.gridRects.Count - 1
                    Dim rect = taskAlg.gridRects(i)
                    Dim p1 = rect.TopLeft
                    Dim p2 = rect.BottomRight
                    Dim nextList As New List(Of Integer)({i}) ' each neighbor list contains the rect.

                    If taskAlg.motionLinkType = 4 Or taskAlg.motionLinkType = 8 Then
                        If p1.X > 0 Then nextList.Add(i - 1)
                        If p2.X < dst2.Width And p2.Y <= dst2.Height Then nextList.Add(i + 1)
                        If p1.Y > 0 Then nextList.Add(i - bricksPerRow)
                        If p2.Y < dst2.Height Then nextList.Add(i + bricksPerRow)
                    End If

                    If taskAlg.motionLinkType = 8 Then
                        If p1.X > 0 And p1.Y > 0 Then nextList.Add(i - bricksPerRow - 1)
                        If p1.Y > 0 And p2.X < dst2.Width Then nextList.Add(i - bricksPerRow + 1)
                        If p1.X > 0 And p2.Y < dst2.Height Then nextList.Add(i + bricksPerRow - 1)
                        If p2.X < dst2.Width And p2.Y < dst2.Height Then
                            If i + bricksPerRow + 1 < taskAlg.gridRects.Count Then nextList.Add(i + bricksPerRow + 1)
                        End If
                    End If
                    gridNeighbors.Add(nextList)
                Next

                For Each nabeList In gridNeighbors
                    Dim xList As New List(Of Integer), yList As New List(Of Integer)
                    For Each index In nabeList
                        Dim roi = taskAlg.gridRects(index)
                        xList.Add(roi.X)
                        yList.Add(roi.Y)
                        xList.Add(roi.BottomRight.X)
                        yList.Add(roi.BottomRight.Y)
                    Next
                    Dim r = New cv.Rect(xList.Min, yList.Min, xList.Max - xList.Min, yList.Max - yList.Min)
                    If r.Width < taskAlg.brickSize * 3 Then
                        If r.X + r.Width >= dst2.Width Then r.X = dst2.Width - taskAlg.brickSize * 3
                        r.Width = taskAlg.brickSize * 3
                    End If
                    If r.Height < taskAlg.brickSize * 3 Then
                        If r.Y + r.Height >= dst2.Height Then r.Y = dst2.Height - taskAlg.brickSize * 3
                        r.Height = taskAlg.brickSize * 3
                    End If
                    If r.Width <> taskAlg.brickSize * 3 Then r.Width = taskAlg.brickSize * 3
                    If r.Height <> taskAlg.brickSize * 3 Then r.Height = taskAlg.brickSize * 3
                    taskAlg.gridNabeRects.Add(r)
                Next

                taskAlg.brickSize = taskAlg.brickSize
                taskAlg.bricksPerCol = bricksPerCol
                taskAlg.bricksPerRow = bricksPerRow
            End If
            If standaloneTest() Then
                dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
                taskAlg.color.CopyTo(dst2)
                dst2.SetTo(white, taskAlg.gridMask)
                labels(2) = "Grid_Basics " + CStr(taskAlg.gridRects.Count) + " (" + CStr(taskAlg.bricksPerCol) + "X" + CStr(taskAlg.bricksPerRow) + ") " +
                                             CStr(taskAlg.brickSize) + "X" + CStr(taskAlg.brickSize) + " regions"
            End If
        End Sub
    End Class







    Public Class Grid_BasicsTest : Inherits TaskParent
        Public Sub New()
            If standalone Then taskAlg.gOptions.GridSlider.Value = 16
            labels = {"", "", "Each grid element is assigned a value below", "The line is the diagonal for each roi.  Bottom might be a shortened roi."}
            If standalone Then desc = "Validation test for Grid_Basics algorithm"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim mean = cv.Cv2.Mean(src)

            dst2.SetTo(0)
            For i = 0 To taskAlg.gridRects.Count - 1
                Dim roi = taskAlg.gridRects(i)
                cv.Cv2.Subtract(mean, src(roi), dst2(roi))
                SetTrueText(CStr(i), New cv.Point(roi.X, roi.Y))
            Next
            dst2.SetTo(white, taskAlg.gridMask)

            dst3.SetTo(0)
            Parallel.For(0, taskAlg.gridRects.Count,
         Sub(i)
             Dim roi = taskAlg.gridRects(i)
             cv.Cv2.Subtract(mean, src(roi), dst3(roi))
             vbc.DrawLine(dst3(roi), New cv.Point(0, 0), New cv.Point(roi.Width, roi.Height), white)
         End Sub)
        End Sub
    End Class










    Public Class Grid_List : Inherits TaskParent
        Public Sub New()
            labels(2) = "Adjust grid width/height to increase thread count."
            If standalone Then desc = "List the active threads"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Parallel.ForEach(Of cv.Rect)(taskAlg.gridRects,
         Sub(roi)
             dst3(roi).SetTo(0)
         End Sub)
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

            Dim fps = taskAlg.fpsAlgorithm \ desiredFPS
            If fps = 0 Then fps = 1
            heartBeat = (taskAlg.frameCount Mod fps) = 0
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







    Public Class Grid_ValidateLocation : Inherits TaskParent
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            taskAlg.clickPoint = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            desc = "Click any grid element to see its neighbors"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src
            labels(2) = "Clickpoint is at (X, Y): " + CStr(taskAlg.clickPoint.X) + ", " + CStr(taskAlg.clickPoint.Y)

            SetTrueText("Click any grid entry to see its neighbors", 3)
            dst2.SetTo(white, taskAlg.gridMask)

            Dim roiIndex As Integer = taskAlg.gridMap.Get(Of Integer)(taskAlg.clickPoint.Y, taskAlg.clickPoint.X)
            If taskAlg.gridRects(roiIndex).Contains(taskAlg.clickPoint) Then
                labels(3) = "Grid index = " + CStr(roiIndex) + " contains the mouse clickpoint" + vbCrLf
            Else
                labels(3) = "Grid index = " + CStr(roiIndex) + " does NOT match the grid location." + vbCrLf
            End If
            dst3.SetTo(0)
            For Each index In taskAlg.grid.gridNeighbors(roiIndex)
                Dim roi = taskAlg.gridRects(index)
                dst2.Rectangle(roi, white, taskAlg.lineWidth)
                dst3.Rectangle(roi, 255, taskAlg.lineWidth)
            Next
        End Sub
    End Class








    Public Class Grid_MinMaxDepth : Inherits TaskParent
        Public minMaxLocs(0) As lpData
        Public minMaxVals(0) As cv.Vec2f
        Public Sub New()
            taskAlg.gOptions.GridSlider.Value = 8
            desc = "Find the min and max depth within each grid roi."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If minMaxLocs.Count <> taskAlg.gridRects.Count Then ReDim minMaxLocs(taskAlg.gridRects.Count - 1)
            If minMaxVals.Count <> taskAlg.gridRects.Count Then ReDim minMaxVals(taskAlg.gridRects.Count - 1)
            Dim mm As mmData
            For i = 0 To minMaxLocs.Count - 1
                Dim roi = taskAlg.gridRects(i)
                taskAlg.pcSplit(2)(roi).MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, taskAlg.depthmask(roi))
                minMaxLocs(i) = New lpData(mm.minLoc, mm.maxLoc)
                minMaxVals(i) = New cv.Vec2f(mm.minVal, mm.maxVal)
            Next

            If standaloneTest() Then
                dst2.SetTo(0)
                For i = 0 To minMaxLocs.Count - 1
                    Dim lp = minMaxLocs(i)
                    DrawCircle(dst2(taskAlg.gridRects(i)), lp.p2, taskAlg.DotSize, cv.Scalar.Red)
                    DrawCircle(dst2(taskAlg.gridRects(i)), lp.p1, taskAlg.DotSize, white)
                Next
                dst2.SetTo(white, taskAlg.gridMask)
            End If
        End Sub
    End Class







    Public Class Grid_TrackCenter : Inherits TaskParent
        Public center As cv.Point
        Dim match As New Match_Basics
        Public Sub New()
            If standalone Then taskAlg.gOptions.ShowGrid.Checked = True
            desc = "Track a cell near the center of the grid"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If match.correlation < taskAlg.fCorrThreshold Or taskAlg.gOptions.DebugCheckBox.Checked Then
                taskAlg.gOptions.DebugCheckBox.Checked = False
                Dim index As Integer = taskAlg.gridMap.Get(Of Integer)(dst2.Height / 2, dst2.Width / 2)
                Dim roi = taskAlg.gridRects(index)
                match.template = src(roi).Clone
                center = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
            End If

            Dim pad = taskAlg.brickSize / 2
            Dim searchRect = ValidateRect(New cv.Rect(center.X - pad, center.Y - pad, taskAlg.brickSize, taskAlg.brickSize))
            match.Run(src(searchRect))
            center = match.newCenter

            If standaloneTest() Then
                dst2 = src
                dst2.Rectangle(match.newRect, taskAlg.highlight, taskAlg.lineWidth + 1, taskAlg.lineType)
                DrawCircle(dst2, center, taskAlg.DotSize, white)

                If taskAlg.heartBeat Then dst3.SetTo(0)
                DrawCircle(dst3, center, taskAlg.DotSize, taskAlg.highlight)
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
            If taskAlg.optionsChanged Then
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
                    gridMask.Line(p1, p2, 255, taskAlg.lineWidth)
                Next
                For y = gridHeight To dst2.Height - 1 Step gridHeight
                    Dim p1 = New cv.Point(0, y), p2 = New cv.Point(dst2.Width, y)
                    gridMask.Line(p1, p2, 255, taskAlg.lineWidth)
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
                taskAlg.color.CopyTo(dst2)
                dst2.SetTo(white, gridMask)
                labels(2) = "Grid_Basics " + CStr(gridRects.Count) + " (" + CStr(bricksPerCol) + "X" + CStr(bricksPerRow) + ") " +
                          CStr(gridWidth) + "X" + CStr(gridHeight) + " regions"
            End If
        End Sub
    End Class
End Namespace