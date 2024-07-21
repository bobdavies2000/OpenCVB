Imports cv = OpenCvSharp
Public Class Horizon_Basics : Inherits VB_Parent
    Public points As New List(Of cv.Point)
    Dim resizeRatio As Integer = 1
    Public vec As New PointPair
    Public vecPresent As Boolean
    Public autoDisplay As Boolean
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
        desc = "Find all the points where depth Y-component transitions from positive to negative"
    End Sub
    Public Sub displayResults(p1 As cv.Point2f, p2 As cv.Point2f)
        If task.heartBeat Then
            If p1.Y >= 1 And p1.Y <= dst2.Height - 1 Then strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf
        End If

        dst2.SetTo(0)
        For Each pt In points
            pt = New cv.Point(pt.X * resizeRatio, pt.Y * resizeRatio)
            DrawCircle(dst2, pt, task.DotSize, cv.Scalar.White)
        Next

        DrawLine(dst2, vec.p1, vec.p2, 255)
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = PrepareDepthInput(1) Else dst0 = src

        Dim resolution = task.quarterRes
        If dst0.Size <> resolution Then
            dst0 = dst0.Resize(resolution, 0, 0, cv.InterpolationFlags.Nearest)
            resizeRatio = CInt(dst2.Height / resolution.Height)
        End If

        dst0 = dst0.Abs()
        dst1 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        dst0.SetTo(task.MaxZmeters, Not dst1)

        points.Clear()
        For i = dst0.Width / 3 To dst0.Width * 2 / 3 - 1
            Dim mm1 = GetMinMax(dst0.Col(i))
            If mm1.minVal > 0 And mm1.minVal < 0.005 Then
                dst0.Col(i).Set(Of Single)(mm1.minLoc.Y, mm1.minLoc.X, 10)
                Dim mm2 = GetMinMax(dst0.Col(i))
                If mm2.minVal > 0 And Math.Abs(mm1.minLoc.Y - mm2.minLoc.Y) <= 1 Then points.Add(New cv.Point(i, mm1.minLoc.Y))
            End If
        Next

        labels(2) = CStr(points.Count) + " points found. "
        Dim p1 As cv.Point
        Dim p2 As cv.Point
        If points.Count >= 2 Then
            p1 = New cv.Point(resizeRatio * points(points.Count - 1).X, resizeRatio * points(points.Count - 1).Y)
            p2 = New cv.Point(resizeRatio * points(0).X, resizeRatio * points(0).Y)
        End If

        Dim distance = p1.DistanceTo(p2)
        If distance < 10 Then ' enough to get a line with some credibility
            points.Clear()
            vecPresent = False
            vec = New PointPair
            strOut = "Horizon not found " + vbCrLf + "The distance of p1 to p2 is " + CStr(CInt(distance)) + " pixels."
        Else
            Dim lp = New PointPair(p1, p2)
            vec = lp.edgeToEdgeLine(dst2.Size)
            vecPresent = True
            If standaloneTest() Or autoDisplay Then displayResults(p1, p2)
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class Horizon_BasicsAlt : Inherits VB_Parent
    Public cloudY As cv.Mat
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
        desc = "Search for the transition from positive to negative to find the horizon."
    End Sub
    Private Function findTransition(startCol As Integer, stopCol As Integer, stepCol As Integer) As cv.Point2f
        Dim val As Single, lastVal As Single
        Dim ptX As New List(Of Single)
        Dim ptY As New List(Of Single)
        For x = startCol To stopCol Step stepCol
            For y = 0 To cloudY.Rows - 1
                lastVal = val
                val = cloudY.Get(Of Single)(y, x)
                If val >= 0 And lastVal <= 0 Then
                    ' change sub-pixel accuracy here 
                    Dim pt = New cv.Point2f(x, y + Math.Abs(val) / Math.Abs(val - lastVal))
                    ptX.Add(pt.X)
                    ptY.Add(pt.Y)
                    If ptX.Count >= task.gOptions.FrameHistory.Value Then Return New cv.Point2f(ptX.Average, ptY.Average)
                End If
            Next
        Next
        Return New cv.Point
    End Function
    Public Sub RunVB(src As cv.Mat)
        If task.useGravityPointcloud Then
            cloudY = task.pcSplit(1) ' already oriented to gravity
        Else
            ' rebuild the pointcloud so it is oriented to gravity.
            Dim pc = (task.pointCloud.Reshape(1, task.pointCloud.Rows * task.pointCloud.Cols) * task.gMatrix).ToMat.Reshape(3, task.pointCloud.Rows)
            Dim split = pc.Split()
            cloudY = split(1)
        End If

        Dim p1 = findTransition(0, cloudY.Width - 1, 1)
        Dim p2 = findTransition(cloudY.Width - 1, 0, -1)
        Dim lp = New PointPair(p1, p2)
        task.horizonVec = lp.edgeToEdgeLine(dst2.Size)

        If p1.Y >= 1 And p1.Y <= dst2.Height - 1 Then
            strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf + "      val =  " +
                      Format(cloudY.Get(Of Single)(p1.Y, p1.X)) + vbCrLf + "lastVal = " + Format(cloudY.Get(Of Single)(p1.Y - 1, p1.X))
        End If
        SetTrueText(strOut, 3)

        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, 255)
            DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, 255)
        End If
    End Sub
End Class







Public Class Horizon_FindNonZero : Inherits VB_Parent
    Public Sub New()
        task.redOptions.YRangeSlider.Value = 3
        If standalone Then task.gOptions.setDisplay1()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
        task.gravityVec = New PointPair(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
        task.horizonVec = New PointPair(New cv.Point2f(0, dst2.Height / 2), New cv.Point2f(dst2.Width, dst2.Height / 2))
        labels = {"", "Horizon vector mask", "Crosshairs - gravityVec (vertical) and horizonVec (horizontal)", "Gravity vector mask"}
        desc = "Create lines for the gravity vector and horizon vector in the camera image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim xRatio = dst0.Width / task.quarterRes.Width
        Dim yRatio = dst0.Height / task.quarterRes.Height

        Dim pc = task.pointCloud.Resize(task.quarterRes)
        Dim split = pc.Split()
        split(2).SetTo(task.MaxZmeters)
        cv.Cv2.Merge(split, pc)

        pc = (pc.Reshape(1, pc.Rows * pc.Cols) * task.gMatrix).ToMat.Reshape(3, pc.Rows)

        dst1 = split(1).InRange(-0.05, 0.05)
        Dim noDepth = task.noDepthMask.Resize(task.quarterRes)
        dst1.SetTo(0, noDepth)
        Dim pointsMat = dst1.FindNonZero()
        If pointsMat.Rows > 0 Then
            dst2.SetTo(0)
            Dim xVals As New List(Of Integer)
            Dim points As New List(Of cv.Point)
            For i = 0 To pointsMat.Rows - 1
                Dim pt = pointsMat.Get(Of cv.Point)(i, 0)
                xVals.Add(pt.X)
                points.Add(New cv.Point2f(pt.X * xRatio, pt.Y * yRatio))
            Next

            Dim p1 = points(xVals.IndexOf(xVals.Min()))
            Dim p2 = points(xVals.IndexOf(xVals.Max()))

            Dim lp = New PointPair(p1, p2)
            task.horizonVec = lp.edgeToEdgeLine(dst2.Size)
            DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, 255)
        End If

        dst3 = split(0).InRange(-0.01, 0.01)
        dst3.SetTo(0, noDepth)
        pointsMat = dst3.FindNonZero()
        If pointsMat.Rows > 0 Then
            Dim yVals As New List(Of Integer)
            Dim points = New List(Of cv.Point)
            For i = 0 To pointsMat.Rows - 1
                Dim pt = pointsMat.Get(Of cv.Point)(i, 0)
                yVals.Add(pt.Y)
                points.Add(New cv.Point2f(pt.X * xRatio, pt.Y * yRatio))
            Next

            Dim p1 = points(yVals.IndexOf(yVals.Min()))
            Dim p2 = points(yVals.IndexOf(yVals.Max()))
            If Math.Abs(p1.X - p2.X) < 2 Then
                task.gravityVec = New PointPair(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
            Else
                Dim lp = New PointPair(p1, p2)
                task.gravityVec = lp.edgeToEdgeLine(dst2.Size)
            End If
            DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, 255)
        End If
    End Sub
End Class







Public Class Horizon_UnstableResults : Inherits VB_Parent
    Dim lines As New Line_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
        desc = "Create lines for the gravity vector and horizon vector in the camera image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        dst1 = task.pcSplit(1).InRange(-0.05, 0.05)
        dst0.SetTo(0)
        dst0.SetTo(cv.Scalar.White, dst1)
        dst0.SetTo(0, task.noDepthMask)
        lines.Run(dst0)

        dst2.SetTo(0)
        If lines.lpList.Count > 0 Then
            Dim distances As New SortedList(Of Single, PointPair)(New compareAllowIdenticalSingleInverted)
            For Each lp In lines.lpList
                distances.Add(lp.p1.DistanceTo(lp.p2), lp)
            Next

            Dim lpBest = distances.ElementAt(0).Value
            Dim p1 = New cv.Point2f(0, lpBest.yIntercept)
            Dim p2 = New cv.Point2f(dst2.Width, lpBest.slope * dst2.Width + lpBest.yIntercept)
            task.horizonVec = New PointPair(p1, p2)
            DrawLine(dst2, p1, p2, cv.Scalar.White)
            labels(2) = "horizonVec slope/intercept = " + Format(lpBest.slope, fmt1) + "/" + Format(lpBest.yIntercept, fmt1)
        End If

        dst1 = task.pcSplit(0).InRange(-0.01, 0.01)
        dst0.SetTo(0)
        dst0.SetTo(cv.Scalar.White, dst1)
        dst0.SetTo(0, task.noDepthMask)
        lines.Run(dst0)

        If lines.lpList.Count > 0 Then
            Dim distances As New SortedList(Of Single, PointPair)(New compareAllowIdenticalSingleInverted)
            For Each lp In lines.lpList
                distances.Add(lp.p1.DistanceTo(lp.p2), lp)
            Next

            Dim lpBest = distances.ElementAt(0).Value
            Dim p1 = New cv.Point2f(0, lpBest.yIntercept)
            Dim p2 = New cv.Point2f(dst2.Width, lpBest.slope * dst2.Width + lpBest.yIntercept)
            task.gravityVec = New PointPair(p1, p2)
            DrawLine(dst2, p1, p2, cv.Scalar.White)
            labels(3) = "gravityVec slope/intercept = " + Format(lpBest.slope, fmt1) + "/" + Format(lpBest.yIntercept, fmt1)
        End If
    End Sub
End Class







Public Class Horizon_FindNonZeroOld : Inherits VB_Parent
    Public Sub New()
        task.gOptions.setGravityUsage(False)
        task.redOptions.YRangeSlider.Value = 3
        If standalone Then task.gOptions.setDisplay1()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
        task.gravityVec = New PointPair(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
        task.horizonVec = New PointPair(New cv.Point2f(0, dst2.Height / 2), New cv.Point2f(dst2.Width, dst2.Height / 2))
        labels = {"", "Horizon vector mask", "Crosshairs - gravityVec (vertical) and horizonVec (horizontal)", "Gravity vector mask"}
        desc = "Create lines for the gravity vector and horizon vector in the camera image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim xRatio = dst0.Width / task.quarterRes.Width
        Dim yRatio = dst0.Height / task.quarterRes.Height
        Dim splitX = task.pcSplit(0)
        Dim splitY = task.pcSplit(1)
        Dim noDepth = task.noDepthMask
        If splitX.Size <> task.quarterRes Then
            splitX = splitX.Resize(task.quarterRes, 0, 0, cv.InterpolationFlags.Nearest)
            splitY = splitY.Resize(task.quarterRes, 0, 0, cv.InterpolationFlags.Nearest)
            noDepth = task.noDepthMask.Resize(task.quarterRes, 0, 0, cv.InterpolationFlags.Nearest)
        End If

        dst1 = splitY.InRange(-0.05, 0.05)
        dst1.SetTo(0, noDepth)
        Dim pointsMat = dst1.FindNonZero()
        If pointsMat.Rows > 0 Then
            dst2.SetTo(0)
            Dim xVals As New List(Of Integer)
            Dim points As New List(Of cv.Point)
            For i = 0 To pointsMat.Rows - 1
                Dim pt = pointsMat.Get(Of cv.Point)(i, 0)
                xVals.Add(pt.X)
                points.Add(New cv.Point2f(pt.X * xRatio, pt.Y * yRatio))
            Next

            Dim p1 = points(xVals.IndexOf(xVals.Min()))
            Dim p2 = points(xVals.IndexOf(xVals.Max()))

            Dim lp = New PointPair(p1, p2)
            task.horizonVec = lp.edgeToEdgeLine(dst2.Size)
            DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, 255)
        End If

        'If task.horizonVec.originalLength < dst2.Width / 2 And task.redOptions.YRangeSlider.Value < task.redOptions.YRangeSlider.Maximum Or pointsMat.Rows = 0 Then
        '    task.redOptions.YRangeSlider.Value += 1
        'End If

        dst3 = splitX.InRange(-0.01, 0.01)
        dst3.SetTo(0, noDepth)
        pointsMat = dst3.FindNonZero()
        If pointsMat.Rows > 0 Then
            Dim yVals As New List(Of Integer)
            Dim points = New List(Of cv.Point)
            For i = 0 To pointsMat.Rows - 1
                Dim pt = pointsMat.Get(Of cv.Point)(i, 0)
                yVals.Add(pt.Y)
                points.Add(New cv.Point2f(pt.X * xRatio, pt.Y * yRatio))
            Next

            Dim p1 = points(yVals.IndexOf(yVals.Min()))
            Dim p2 = points(yVals.IndexOf(yVals.Max()))
            If Math.Abs(p1.X - p2.X) < 2 Then
                task.gravityVec = New PointPair(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
            Else
                Dim lp = New PointPair(p1, p2)
                task.gravityVec = lp.edgeToEdgeLine(dst2.Size)
            End If
            DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, 255)
        End If

        'If task.gravityVec.originalLength < dst2.Height / 2 And task.redOptions.XRangeSlider.Value < task.redOptions.XRangeSlider.Maximum Or pointsMat.Rows = 0 Then
        '    task.redOptions.XRangeSlider.Value += 1
        'End If
    End Sub
End Class






Public Class Horizon_Validate : Inherits VB_Parent
    Dim match As New Match_Basics
    Dim ptLeft As New cv.Point2f, ptRight As New cv.Point2f
    Dim leftTemplate As cv.Mat, rightTemplate As cv.Mat
    Public Sub New()
        desc = "Validate the horizon points using Match_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim templatePad = match.options.templatePad
        Dim templateSize = match.options.templateSize

        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Then
            ptLeft = task.gravityVec.p1
            ptRight = task.gravityVec.p2
            Dim r = ValidateRect(New cv.Rect(ptLeft.X - templatePad, ptLeft.Y - templatePad, templateSize, templateSize))
            leftTemplate = src(r)

            r = ValidateRect(New cv.Rect(ptRight.X - templatePad, ptRight.Y - templatePad, templateSize, templateSize))
            rightTemplate = src(r)
        Else
            Dim r = ValidateRect(New cv.Rect(ptLeft.X - templatePad, ptLeft.Y - templatePad, templateSize, templateSize))
            match.template = leftTemplate
            match.Run(src)
            ptLeft = match.matchCenter

            r = ValidateRect(New cv.Rect(ptRight.X - templatePad, ptRight.Y - templatePad, templateSize, templateSize))
            match.template = leftTemplate
            match.Run(src)
            ptLeft = match.matchCenter
        End If
    End Sub
End Class






Public Class Horizon_Regress : Inherits VB_Parent
    Dim horizon As New Horizon_Basics
    Dim regress As New LinearRegression_Basics
    Public Sub New()
        desc = "Collect the horizon points and run a linear regression on all the points."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        horizon.Run(src)

        For i = 0 To horizon.points.Count - 1
            regress.x.Add(horizon.points(i).X)
            regress.y.Add(horizon.points(i).Y)
        Next

        regress.Run(Nothing)
        horizon.displayResults(regress.p1, regress.p2)
        dst2 = horizon.dst2
    End Sub
End Class





Public Class Horizon_ExternalTest : Inherits VB_Parent
    Dim horizon As New Horizon_Basics
    Public Sub New()
        desc = "Supply the point cloud input to Horizon_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst0 = PrepareDepthInput(1)
        horizon.Run(dst0)
        dst2 = horizon.dst2
    End Sub
End Class
