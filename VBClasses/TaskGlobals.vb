Imports cv = OpenCvSharp
Namespace VBClasses
    Public Module vbc
        Public algTask As VBClasses.AlgorithmTask
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
            Dim dst As New cv.Mat(algTask.workRes, cv.MatType.CV_8UC3, 0)

            For Each rc In algTask.redList.oldrclist
                dst(rc.rect).SetTo(rc.color, rc.mask)
            Next

            Return dst
        End Function
        Public Function RebuildRCMap(sortedCells As SortedList(Of Integer, oldrcData)) As cv.Mat
            algTask.redList.oldrclist.Clear()
            algTask.redList.oldrclist.Add(New oldrcData) ' placeholder oldrcData so map is correct.
            algTask.redList.rcMap.SetTo(0)
            Static saveColorSetting = algTask.gOptions.trackingLabel
            For Each rc In sortedCells.Values
                rc.index = algTask.redList.oldrclist.Count

                If saveColorSetting <> algTask.gOptions.trackingLabel Then rc.color = black
                'Select Case algTask.gOptions.trackingLabel
                '    Case "Mean Color"
                '        Dim colorStdev As cv.Scalar
                '        cv.Cv2.MeanStdDev(algTask.color(rc.rect), rc.color, colorStdev, rc.mask)
                ' Case "Tracking Color"
                If rc.color = black Then rc.color = algTask.scalarColors(rc.index)
                'End Select

                algTask.redList.oldrclist.Add(rc)
                algTask.redList.rcMap(rc.rect).SetTo(rc.index, rc.mask)
                DisplayCells.Circle(rc.maxDStable, algTask.DotSize, algTask.highlight, -1)
                If rc.index >= 255 Then Exit For
            Next
            saveColorSetting = algTask.gOptions.trackingLabel
            algTask.redList.rcMap.SetTo(0, algTask.noDepthMask)
            Return DisplayCells()
        End Function
        Public Function RebuildRCMap(oldrclist As List(Of oldrcData)) As cv.Mat
            algTask.redList.rcMap.SetTo(0)
            Dim dst As New cv.Mat(algTask.workRes, cv.MatType.CV_8UC3, 0)
            For Each rc In oldrclist
                algTask.redList.rcMap(rc.rect).SetTo(rc.index, rc.mask)
                dst(rc.rect).SetTo(rc.color, rc.mask)
                If rc.index >= 255 Then Exit For
            Next
            Return dst
        End Function
        Public Function Convert32f_To_8UC3(Input As cv.Mat) As cv.Mat
            Dim outMat = Input.Normalize(0, 255, cv.NormTypes.MinMax)
            If Input.Channels() = 1 Then
                outMat.ConvertTo(outMat, cv.MatType.CV_8U)
                Return outMat.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            End If
            outMat.ConvertTo(outMat, cv.MatType.CV_8UC3)
            Return outMat
        End Function
        Public Function Check8uC3(ByVal input As cv.Mat) As cv.Mat
            Dim outMat As New cv.Mat
            If input.Type = cv.MatType.CV_8UC3 Then Return input
            If input.Type = cv.MatType.CV_32F Then
                outMat = Convert32f_To_8UC3(input)
            ElseIf input.Type = cv.MatType.CV_32SC1 Then
                input.ConvertTo(outMat, cv.MatType.CV_32F)
                outMat = Convert32f_To_8UC3(outMat)
            ElseIf input.Type = cv.MatType.CV_32SC3 Then
                input.ConvertTo(outMat, cv.MatType.CV_32F)
                outMat = outMat.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                outMat = Convert32f_To_8UC3(outMat)
            ElseIf input.Type = cv.MatType.CV_32FC3 Then
                outMat = input.ConvertScaleAbs(255)
            Else
                outMat = input.Clone
            End If
            If input.Channels() = 1 And input.Type = cv.MatType.CV_8UC1 Then outMat = input.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            Return outMat
        End Function
        Public Sub updateSettings()
            If algTask.myStopWatch Is Nothing Then algTask.myStopWatch = Stopwatch.StartNew()

            ' update the time measures
            algTask.msWatch = algTask.myStopWatch.ElapsedMilliseconds

            algTask.quarterBeat = False
            algTask.midHeartBeat = False
            algTask.heartBeat = False
            Dim ms = (algTask.msWatch - algTask.msLast) / 1000
            For i = 0 To algTask.quarter.Count - 1
                If algTask.quarter(i) = False And ms > Choose(i + 1, 0.25, 0.5, 0.75, 1.0) Then
                    algTask.quarterBeat = True
                    If i = 1 Then algTask.midHeartBeat = True
                    If i = 3 Then algTask.heartBeat = True
                    algTask.quarter(i) = True
                End If
            Next
            If algTask.heartBeat Then ReDim algTask.quarter(4)

            If algTask.frameCount = 1 Then algTask.heartBeat = True

            Static lastHeartBeatLT As Boolean = algTask.heartBeatLT
            algTask.afterHeartBeatLT = If(lastHeartBeatLT, True, False)
            lastHeartBeatLT = algTask.heartBeatLT

            Static heartBeatCount As Integer = 5
            If algTask.heartBeat Then
                heartBeatCount += 1
                If heartBeatCount >= 5 Then
                    algTask.heartBeatLT = True
                    heartBeatCount = 0
                End If
            End If

            Dim frameDuration = 1000 / algTask.fpsAlgorithm
            algTask.almostHeartBeat = If(algTask.msWatch - algTask.msLast + frameDuration * 1.5 > 1000, True, False)

            If (algTask.msWatch - algTask.msLast) > 1000 Then algTask.msLast = algTask.msWatch
            If algTask.heartBeatLT Then algTask.toggleOn = Not algTask.toggleOn

            If algTask.paused Then
                algTask.midHeartBeat = False
                algTask.almostHeartBeat = False
            End If

            'algTask.histogramBins = algTask.gOptions.HistBinBar.Value
            'algTask.lineWidth = algTask.gOptions.LineWidth.Value
            'algTask.DotSize = algTask.gOptions.DotSizeSlider.Value

            'algTask.metersPerPixel = algTask.MaxZmeters / algTask.workRes.Height ' meters per pixel in projections - side and top.
            'algTask.debugSyncUI = algTask.gOptions.debugSyncUI.Checked
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
            Dim w = algTask.cols, h = algTask.rows

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

            If xp1.Y = algTask.color.Height Then xp1.Y -= 1
            If xp2.Y = algTask.color.Height Then xp2.Y -= 1
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
            dst.Line(pt1, pt2, color, algTask.lineWidth, algTask.lineType)
        End Sub
        Public Sub DrawLine(dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f, color As cv.Scalar, lineWidth As Integer)
            dst.Line(p1, p2, color, lineWidth, algTask.lineType)
        End Sub
        Public Sub DrawLine(dst As cv.Mat, lp As lpData, color As cv.Scalar)
            dst.Line(lp.p1, lp.p2, color, algTask.lineWidth, algTask.lineType)
        End Sub
        Public Sub DrawLine(dst As cv.Mat, lp As lpData)
            dst.Line(lp.p1, lp.p2, algTask.highlight, algTask.lineWidth, algTask.lineType)
        End Sub
        Public Sub DrawLine(dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f)
            dst.Line(p1, p2, algTask.highlight, algTask.lineWidth, algTask.lineType)
        End Sub
        Public Function ValidateRect(ByVal r As cv.Rect, Optional ratio As Integer = 1) As cv.Rect
            If r.X < 0 Then r.X = 0
            If r.Y < 0 Then r.Y = 0
            If r.X + r.Width >= algTask.workRes.Width * ratio Then r.Width = algTask.workRes.Width * ratio - r.X - 1
            If r.Y + r.Height >= algTask.workRes.Height * ratio Then r.Height = algTask.workRes.Height * ratio - r.Y - 1
            If r.X >= algTask.workRes.Width * ratio Then r.X = algTask.workRes.Width - 1
            If r.Y >= algTask.workRes.Height * ratio Then r.Y = algTask.workRes.Height - 1
            If r.Width <= 0 Then r.Width = 1
            If r.Height <= 0 Then r.Height = 1
            Return r
        End Function
    End Module
End Namespace