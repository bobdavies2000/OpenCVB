﻿Imports cv = OpenCvSharp
Public Class Horizon_Basics : Inherits TaskParent
    Public points As New List(Of cv.Point)
    Dim resizeRatio As Integer = 1
    Public vec As New linePoints
    Public vecPresent As Boolean
    Public autoDisplay As Boolean
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find all the points where depth Y-component transitions from positive to negative"
    End Sub
    Public Sub displayResults(p1 As cv.Point2f, p2 As cv.Point2f)
        If task.heartBeat Then
            If p1.Y >= 1 And p1.Y <= dst2.Height - 1 Then strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf
        End If

        dst2.SetTo(0)
        For Each pt In points
            pt = New cv.Point(pt.X * resizeRatio, pt.Y * resizeRatio)
            DrawCircle(dst2, pt, task.DotSize, white)
        Next

        DrawLine(dst2, vec.p1, vec.p2, 255)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = PrepareDepthInput(1) Else dst0 = src

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
            vec = New linePoints
            strOut = "Horizon not found " + vbCrLf + "The distance of p1 to p2 is " + CStr(CInt(distance)) + " pixels."
        Else
            Dim lp = New linePoints(p1, p2)
            vec = New linePoints(lp.xp1, lp.xp2)
            vecPresent = True
            If standaloneTest() Or autoDisplay Then
                displayResults(p1, p2)
                displayResults(New cv.Point(-p1.Y, p1.X), New cv.Point(p2.Y, -p2.X))
            End If
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class Horizon_FindNonZero : Inherits TaskParent
    Public Sub New()
        task.redOptions.YRangeSlider.Value = 3
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        task.gravityVec = New linePoints(New cv.Point2f(dst2.Width / 2, 0),
                                         New cv.Point2f(dst2.Width / 2, dst2.Height))
        task.horizonVec = New linePoints(New cv.Point2f(0, dst2.Height / 2), New cv.Point2f(dst2.Width, dst2.Height / 2))
        labels = {"", "Horizon vector mask", "Crosshairs - gravityVec (vertical) and horizonVec (horizontal)", "Gravity vector mask"}
        desc = "Create lines for the gravity vector and horizon vector in the camera image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pc = task.pointCloud
        Dim split = pc.Split()
        split(2).SetTo(task.MaxZmeters)
        cv.Cv2.Merge(split, pc)

        pc = (pc.Reshape(1, pc.Rows * pc.Cols) * task.gMatrix).ToMat.Reshape(3, pc.Rows)

        dst1 = split(1).InRange(-0.05, 0.05)
        dst1.SetTo(0, task.noDepthMask)
        Dim pointsMat = dst1.FindNonZero()
        If pointsMat.Rows > 0 Then
            dst2.SetTo(0)
            Dim xVals As New List(Of Integer)
            Dim points As New List(Of cv.Point)
            For i = 0 To pointsMat.Rows - 1
                Dim pt = pointsMat.Get(Of cv.Point)(i, 0)
                xVals.Add(pt.X)
                points.Add(New cv.Point2f(pt.X, pt.Y))
            Next

            Dim p1 = points(xVals.IndexOf(xVals.Min()))
            Dim p2 = points(xVals.IndexOf(xVals.Max()))

            Dim lp = New linePoints(p1, p2)
            task.horizonVec = New linePoints(lp.xp1, lp.xp2)
            DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, 255)
        End If

        dst3 = split(0).InRange(-0.01, 0.01)
        dst3.SetTo(0, task.noDepthMask)
        pointsMat = dst3.FindNonZero()
        If pointsMat.Rows > 0 Then
            Dim yVals As New List(Of Integer)
            Dim points = New List(Of cv.Point)
            For i = 0 To pointsMat.Rows - 1
                Dim pt = pointsMat.Get(Of cv.Point)(i, 0)
                yVals.Add(pt.Y)
                points.Add(New cv.Point2f(pt.X, pt.Y))
            Next

            Dim p1 = points(yVals.IndexOf(yVals.Min()))
            Dim p2 = points(yVals.IndexOf(yVals.Max()))
            If Math.Abs(p1.X - p2.X) < 2 Then
                task.gravityVec = New linePoints(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
            Else
                Dim lp = New linePoints(p1, p2)
                task.gravityVec = New linePoints(lp.xp1, lp.xp2)
            End If
            DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, 255)
        End If
    End Sub
End Class






Public Class Horizon_Validate : Inherits TaskParent
    Dim match As New Match_Basics
    Dim ptLeft As New cv.Point2f, ptRight As New cv.Point2f
    Dim leftTemplate As cv.Mat, rightTemplate As cv.Mat
    Public Sub New()
        desc = "Validate the horizon points using Match_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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






Public Class Horizon_Regress : Inherits TaskParent
    Dim horizon As New Horizon_Basics
    Dim regress As New LinearRegression_Basics
    Public Sub New()
        desc = "Collect the horizon points and run a linear regression on all the points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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





Public Class Horizon_ExternalTest : Inherits TaskParent
    Dim horizon As New Horizon_Basics
    Public Sub New()
        desc = "Supply the point cloud input to Horizon_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst0 = PrepareDepthInput(1)
        horizon.Run(dst0)
        dst2 = horizon.dst2
    End Sub
End Class







Public Class Horizon_Perpendicular : Inherits TaskParent
    Dim perp As New Line_Perpendicular
    Public Sub New()
        labels(2) = "Yellow line is the perpendicular to the horizon.  White is gravity vector from the IMU."
        desc = "Find the gravity vector using the perpendicular to the horizon."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src
        DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, white)

        perp.input = task.horizonVec
        perp.Run(src)
        DrawLine(dst2, perp.output.p1, perp.output.p2, cv.Scalar.Yellow)

        Dim gVec = task.gravityVec
        gVec.p1.X += 10
        gVec.p2.X += 10
        DrawLine(dst2, gVec.p1, gVec.p2, white)
    End Sub
End Class





Public Class Horizon_Simple : Inherits TaskParent
    Dim perp As New Line_Perpendicular
    Public Sub New()
        desc = "Find the horizon with the perpendicular to gravity"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then dst2 = src.Clone

        task.horizonVec = perp.computePerp(task.gravityVec)
        DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.Yellow)
    End Sub
End Class
