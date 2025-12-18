Imports System.Threading
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Module vbc
        Public task As AlgorithmTask
        Public imageLock As New Mutex(True, "imageLock")
        Public Const fmt0 = "0"
        Public Const fmt1 = "0.0"
        Public Const fmt2 = "0.00"
        Public Const fmt3 = "0.000"
        Public Const fmt4 = "0.0000"
        Public newPoint As New cv.Point
        Public msRNG As New System.Random
        Public white As New cv.Scalar(255, 255, 255), black As New cv.Scalar(0, 0, 0)
        Public grayColor As New cv.Scalar(127, 127, 127)
        Public yellow As New cv.Scalar(0, 255, 255), purple As New cv.Scalar(255, 0, 255)
        Public teal As New cv.Scalar(255, 255, 0)
        Public red As New cv.Scalar(0, 0, 255), green As New cv.Scalar(0, 255, 0)
        Public blue As New cv.Scalar(255, 0, 0)

        Public zero3f As New cv.Point3f
        Public newVec4f As New cv.Vec4f
        Public emptyMat As New cv.Mat
        Public pipeCount As Integer
        Public saveVecColors(0) As cv.Vec3b
        Public saveScalarColors(0) As cv.Scalar
        Public saveDepthColorMap As cv.Mat
        Public term As New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 10, 1.0)
        <System.Runtime.CompilerServices.Extension()>
        Public Sub SwapWith(Of T)(ByRef thisObj As T, ByRef withThisObj As T)
            Dim tempObj = thisObj
            thisObj = withThisObj
            withThisObj = tempObj
        End Sub
        Public Function vecToScalar(c As cv.Vec3b) As cv.Scalar
            Return New cv.Scalar(c(0), c(1), c(2))
        End Function
        Public Function DisplayCells() As cv.Mat
            Dim dst As New cv.Mat(Task.workRes, cv.MatType.CV_8UC3, 0)

            For Each rc In Task.redList.oldrclist
                dst(rc.rect).SetTo(rc.color, rc.mask)
            Next

            Return dst
        End Function
        Public Function RebuildRCMap(sortedCells As SortedList(Of Integer, oldrcData)) As cv.Mat
            Task.redList.oldrclist.Clear()
            Task.redList.oldrclist.Add(New oldrcData) ' placeholder oldrcData so map is correct.
            Task.redList.rcMap.SetTo(0)
            Static saveColorSetting = Task.gOptions.trackingLabel
            For Each rc In sortedCells.Values
                rc.index = Task.redList.oldrclist.Count

                If saveColorSetting <> Task.gOptions.trackingLabel Then rc.color = black
                'Select Case task.gOptions.trackingLabel
                '    Case "Mean Color"
                '        Dim colorStdev As cv.Scalar
                '        cv.Cv2.MeanStdDev(task.color(rc.rect), rc.color, colorStdev, rc.mask)
                ' Case "Tracking Color"
                If rc.color = black Then rc.color = Task.scalarColors(rc.index)
                'End Select

                Task.redList.oldrclist.Add(rc)
                Task.redList.rcMap(rc.rect).SetTo(rc.index, rc.mask)
                DisplayCells.Circle(rc.maxDStable, Task.DotSize, Task.highlight, -1)
                If rc.index >= 255 Then Exit For
            Next
            saveColorSetting = Task.gOptions.trackingLabel
            Task.redList.rcMap.SetTo(0, Task.noDepthMask)
            Return DisplayCells()
        End Function
        Public Function RebuildRCMap(oldrclist As List(Of oldrcData)) As cv.Mat
            Task.redList.rcMap.SetTo(0)
            Dim dst As New cv.Mat(Task.workRes, cv.MatType.CV_8UC3, 0)
            For Each rc In oldrclist
                Task.redList.rcMap(rc.rect).SetTo(rc.index, rc.mask)
                dst(rc.rect).SetTo(rc.color, rc.mask)
                If rc.index >= 255 Then Exit For
            Next
            Return dst
        End Function
        Public Sub taskUpdate()
            If Task.myStopWatch Is Nothing Then Task.myStopWatch = Stopwatch.StartNew()

            ' update the time measures
            Task.msWatch = Task.myStopWatch.ElapsedMilliseconds

            Task.quarterBeat = False
            Task.midHeartBeat = False
            Task.heartBeat = False
            Dim ms = (Task.msWatch - Task.msLast) / 1000
            For i = 0 To Task.quarter.Count - 1
                If Task.quarter(i) = False And ms > Choose(i + 1, 0.25, 0.5, 0.75, 1.0) Then
                    Task.quarterBeat = True
                    If i = 1 Then Task.midHeartBeat = True
                    If i = 3 Then Task.heartBeat = True
                    Task.quarter(i) = True
                End If
            Next
            If Task.heartBeat Then ReDim Task.quarter(4)

            If Task.frameCount = 1 Then Task.heartBeat = True

            Static lastHeartBeatLT As Boolean = Task.heartBeatLT
            Task.afterHeartBeatLT = If(lastHeartBeatLT, True, False)
            lastHeartBeatLT = Task.heartBeatLT

            Static heartBeatCount As Integer = 5
            If Task.heartBeat Then
                heartBeatCount += 1
                If heartBeatCount >= 5 Then
                    Task.heartBeatLT = True
                    heartBeatCount = 0
                End If
            End If

            Dim frameDuration = 1000 / Task.fpsAlgorithm
            Task.almostHeartBeat = If(Task.msWatch - Task.msLast + frameDuration * 1.5 > 1000, True, False)

            If (Task.msWatch - Task.msLast) > 1000 Then Task.msLast = Task.msWatch
            If Task.heartBeatLT Then Task.toggleOn = Not Task.toggleOn

            If Task.paused Then
                Task.midHeartBeat = False
                Task.almostHeartBeat = False
            End If

            'task.histogramBins = task.gOptions.HistBinBar.Value
            'task.lineWidth = task.gOptions.LineWidth.Value
            'task.DotSize = task.gOptions.DotSizeSlider.Value

            'task.metersPerPixel = task.MaxZmeters / task.workRes.Height ' meters per pixel in projections - side and top.
            'task.debugSyncUI = task.gOptions.debugSyncUI.Checked
        End Sub

        Public Function findRectFromLine(lp As lpData) As cv.Rect
            Dim rect = New cv.Rect(lp.p1.X, lp.p1.Y, Math.Abs(lp.p1.X - lp.p2.X), Math.Abs(lp.p1.Y - lp.p2.Y))
            If lp.p1.Y > lp.p2.Y Then rect = New cv.Rect(lp.p1.X, lp.p2.Y, rect.Width, rect.Height)
            If rect.Width < 2 Then rect.Width = 2
            If rect.Height < 2 Then rect.Height = 2
            Return rect
        End Function


        Public Function findEdgePoints(lp As lpData) As lpData
            ' compute the edge to edge line - might be useful...
            Dim yIntercept = lp.p1.Y - lp.slope * lp.p1.X
            Dim w = Task.cols, h = Task.rows

            Dim xp1 = New cv.Point2f(0, yIntercept)
            Dim xp2 = New cv.Point2f(w, w * lp.slope + yIntercept)
            Dim xIntercept = -yIntercept / lp.slope
            If xp1.Y > h Then
                xp1.X = (h - yIntercept) / lp.slope
                xp1.Y = h
            End If
            If xp1.Y < 0 Then
                xp1.X = xIntercept
                xp1.Y = 0
            End If

            If xp2.Y > h Then
                xp2.X = (h - yIntercept) / lp.slope
                xp2.Y = h
            End If
            If xp2.Y < 0 Then
                xp2.X = xIntercept
                xp2.Y = 0
            End If

            If xp1.Y = Task.color.Height Then xp1.Y -= 1
            If xp2.Y = Task.color.Height Then xp2.Y -= 1
            Return New lpData(xp1, xp2)

        End Function
        Public Function GetMinMax(mat As cv.Mat, Optional mask As cv.Mat = Nothing) As mmData
            Dim mm As mmData
            If mask Is Nothing Then
                mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc)
            Else
                mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, mask)
            End If

            If Double.IsInfinity(mm.maxVal) Then
                Console.WriteLine("IsInfinity encountered in getMinMax.")
                mm.maxVal = 0 ' skip ...
            End If
            mm.range = mm.maxVal - mm.minVal
            Return mm
        End Function
        ' alternative optional parameter: ApproxTC89L1 or ApproxNone
        Public Function ContourBuild(mask As cv.Mat, Optional approxMode As cv.ContourApproximationModes = cv.ContourApproximationModes.ApproxNone) As List(Of cv.Point)
            Dim allContours As cv.Point()() = Nothing
            cv.Cv2.FindContours(mask, allContours, Nothing, cv.RetrievalModes.External, approxMode)

            Dim tourCount As New List(Of Integer)
            For Each tour In allContours
                tourCount.Add(tour.Count)
            Next
            If tourCount.Count > 0 Then
                Return New List(Of cv.Point)(allContours(tourCount.IndexOf(tourCount.Max)).ToList)
            End If
            Return New List(Of cv.Point)
        End Function
        Public Sub DrawLine(ByRef dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f, color As cv.Scalar)
            Dim pt1 = New cv.Point(p1.X, p1.Y)
            Dim pt2 = New cv.Point(p2.X, p2.Y)
            dst.Line(pt1, pt2, color, Task.lineWidth, Task.lineType)
        End Sub
        Public Sub DrawLine(dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f, color As cv.Scalar, lineWidth As Integer)
            dst.Line(p1, p2, color, lineWidth, Task.lineType)
        End Sub
        Public Sub DrawLine(dst As cv.Mat, lp As lpData, color As cv.Scalar)
            dst.Line(lp.p1, lp.p2, color, Task.lineWidth, Task.lineType)
        End Sub
        Public Sub DrawLine(dst As cv.Mat, lp As lpData)
            dst.Line(lp.p1, lp.p2, Task.highlight, Task.lineWidth, Task.lineType)
        End Sub
        Public Sub DrawLine(dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f)
            dst.Line(p1, p2, Task.highlight, Task.lineWidth, Task.lineType)
        End Sub
        Public Function ValidateRect(ByVal r As cv.Rect, Optional ratio As Integer = 1) As cv.Rect
            If r.X < 0 Then r.X = 0
            If r.Y < 0 Then r.Y = 0
            If r.X + r.Width >= Task.workRes.Width * ratio Then r.Width = Task.workRes.Width * ratio - r.X - 1
            If r.Y + r.Height >= Task.workRes.Height * ratio Then r.Height = Task.workRes.Height * ratio - r.Y - 1
            If r.X >= Task.workRes.Width * ratio Then r.X = Task.workRes.Width - 1
            If r.Y >= Task.workRes.Height * ratio Then r.Y = Task.workRes.Height - 1
            If r.Width <= 0 Then r.Width = 1
            If r.Height <= 0 Then r.Height = 1
            Return r
        End Function
    End Module
End Namespace