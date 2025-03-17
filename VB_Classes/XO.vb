Imports System.Threading
Imports cv = OpenCvSharp
Public Class XO_Gravity_HorizonRawOld : Inherits TaskParent
    Public yLeft As Integer, yRight As Integer, xTop As Integer, xBot As Integer
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(2) = "Horizon and Gravity Vectors"
        desc = "Improved method to find gravity and horizon vectors"
    End Sub
    Private Function findFirst(points As cv.Mat, horizon As Boolean, ByRef sampleX As Integer) As Integer
        Dim ptList As New List(Of Integer)

        For i = 0 To Math.Min(10, points.Rows / 2)
            Dim pt = points.Get(Of cv.Point)(i, 0)
            If pt.X <= 0 Or pt.Y <= 0 Then Continue For
            If pt.X > dst2.Width Or pt.Y > dst2.Height Then Continue For
            If horizon Then ptList.Add(pt.Y) Else ptList.Add(pt.X)
            sampleX = pt.X ' this X value tells us if the horizon found is for the left or the right.
        Next

        If ptList.Count = 0 Then Return 0
        Return ptList.Average()
    End Function
    Private Function findLast(points As cv.Mat, horizon As Boolean, sampleX As Integer) As Integer
        Dim ptList As New List(Of Integer)

        For i = points.Rows To Math.Max(points.Rows - 10, points.Rows / 2) Step -1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            If pt.X <= 0 Or pt.Y <= 0 Then Continue For
            If pt.X > dst2.Width Or pt.Y > dst2.Height Then Continue For
            If horizon Then ptList.Add(pt.Y) Else ptList.Add(pt.X)
            sampleX = pt.X ' this X value tells us if the horizon found is for the left or the right.
        Next

        If ptList.Count = 0 Then Return 0
        Return ptList.Average()
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim threshold As Single = 0.015
        Dim work As New cv.Mat

        work = task.pcSplit(1).InRange(-threshold, threshold)
        work.SetTo(0, task.noDepthMask)
        work.ConvertTo(dst1, cv.MatType.CV_8U)
        Dim hPoints = dst1.FindNonZero()
        If hPoints.Total > 0 Then
            Dim sampleX1 As Integer, sampleX2 As Integer
            Dim y1 = findFirst(hPoints, True, sampleX1)
            Dim y2 = findLast(hPoints, True, sampleX2)

            ' This is because FindNonZero works from the top of the image down.  
            ' If the horizon has a positive slope, the first point found will be on the right.
            ' if the horizon has a negative slope, the first point found will be on the left.
            If sampleX1 < dst2.Width / 2 Then
                yLeft = y1
                yRight = y2
            Else
                yLeft = y2
                yRight = y1
            End If
        Else
            yLeft = 0
            yRight = 0
        End If

        work = task.pcSplit(0).InRange(-threshold, threshold)
        work.SetTo(0, task.noDepthMask)
        work.ConvertTo(dst3, cv.MatType.CV_8U)
        Dim gPoints = dst3.FindNonZero()
        Dim sampleUnused As Integer
        xTop = findFirst(gPoints, False, sampleUnused)
        xBot = findLast(gPoints, False, sampleUnused)

        If standaloneTest() Then
            Dim horizonVec As linePoints, gravityVec As linePoints
            If hPoints.Total > 0 Then
                horizonVec = New linePoints(New cv.Point(0, yLeft), New cv.Point(dst2.Width, yRight))
            Else
                horizonVec = New linePoints
            End If

            gravityVec = New linePoints(New cv.Point(xTop, 0), New cv.Point(xBot, dst2.Height))

            dst2.SetTo(0)
            DrawLine(dst2, gravityVec.p1, gravityVec.p2, task.HighlightColor)
            DrawLine(dst2, horizonVec.p1, horizonVec.p2, cv.Scalar.Red)
        End If
    End Sub
End Class






Public Class XO_Horizon_FindNonZero : Inherits TaskParent
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







Public Class XO_Horizon_Perpendicular : Inherits TaskParent
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





Public Class XO_Horizon_Regress : Inherits TaskParent
    Dim horizon As New XO_Horizon_Basics
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





Public Class XO_Horizon_Basics : Inherits TaskParent
    Public points As New List(Of cv.Point)
    Dim resizeRatio As Integer = 1
    Public vec As New linePoints
    Public autoDisplay As Boolean
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find all the points where depth Y-component transitions from positive to negative"
    End Sub
    Public Sub displayResults(p1 As cv.Point2f, p2 As cv.Point2f)
        If task.heartBeat Then
            If p1.Y >= 1 And p1.Y <= dst2.Height - 1 Then
                strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf
            End If
        End If

        dst2.SetTo(0)
        For Each pt In points
            pt = New cv.Point(pt.X * resizeRatio, pt.Y * resizeRatio)
            DrawCircle(dst2, pt, task.DotSize, white)
        Next

        DrawLine(dst2, vec.p1, vec.p2, 255)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = task.gravitySplit(1) Else dst0 = src

        dst0 = dst0.Abs()
        dst1 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        dst0.SetTo(task.MaxZmeters, Not dst1)

        points.Clear()
        For i = dst0.Width / 3 To dst0.Width * 2 / 3 - 1
            Dim mm1 = GetMinMax(dst0.Col(i))
            If mm1.minVal > 0 And mm1.minVal < 0.005 Then
                dst0.Col(i).Set(Of Single)(mm1.minLoc.Y, mm1.minLoc.X, 10)
                Dim mm2 = GetMinMax(dst0.Col(i))
                If mm2.minVal > 0 And Math.Abs(mm1.minLoc.Y - mm2.minLoc.Y) <= 1 Then
                    points.Add(New cv.Point(i, mm1.minLoc.Y))
                End If
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
            vec = New linePoints
            strOut = "Horizon not found " + vbCrLf + "The distance of p1 to p2 is " + CStr(CInt(distance)) + " pixels."
        Else
            Dim lp = New linePoints(p1, p2)
            vec = New linePoints(lp.xp1, lp.xp2)
            If standaloneTest() Or autoDisplay Then
                displayResults(p1, p2)
                displayResults(New cv.Point(-p1.Y, p1.X), New cv.Point(p2.Y, -p2.X))
            End If
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class XO_Horizon_Validate : Inherits TaskParent
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






Public Class XO_Horizon_ExternalTest : Inherits TaskParent
    Dim horizon As New XO_Horizon_Basics
    Public Sub New()
        desc = "Supply the point cloud input to Horizon_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst0 = task.gravitySplit(1)
        horizon.Run(dst0)
        dst2 = horizon.dst2
    End Sub
End Class





Public Class XO_Gravity_Basics : Inherits TaskParent
    Public points As New List(Of cv.Point2f)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find all the points where depth X-component transitions from positive to negative"
    End Sub
    Public Sub displayResults(p1 As cv.Point, p2 As cv.Point)
        If task.heartBeat Then
            If p1.Y >= 1 And p1.Y <= dst2.Height - 1 Then strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf
        End If

        dst2.SetTo(0)
        dst3.SetTo(0)
        For Each pt In points
            DrawCircle(dst2, pt, task.DotSize, white)
        Next

        DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, white)
        DrawLine(dst3, task.gravityVec.p1, task.gravityVec.p2, white)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = task.gravitySplit(0) Else dst0 = src

        dst0 = dst0.Abs()
        dst1 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        dst0.SetTo(task.MaxZmeters, Not dst1)

        points.Clear()
        For i = dst0.Height / 3 To dst0.Height * 2 / 3 - 1
            Dim mm1 = GetMinMax(dst0.Row(i))
            If mm1.minVal > 0 And mm1.minVal < 0.005 Then
                dst0.Row(i).Set(Of Single)(mm1.minLoc.Y, mm1.minLoc.X, 10)
                Dim mm2 = GetMinMax(dst0.Row(i))
                If mm2.minVal > 0 And Math.Abs(mm1.minLoc.X - mm2.minLoc.X) <= 1 Then points.Add(New cv.Point(mm1.minLoc.X, i))
            End If
        Next

        labels(2) = CStr(points.Count) + " points found. "
        Dim p1 As cv.Point2f
        Dim p2 As cv.Point2f
        If points.Count >= 2 Then
            p1 = New cv.Point2f(points(points.Count - 1).X, points(points.Count - 1).Y)
            p2 = New cv.Point2f(points(0).X, points(0).Y)
        End If

        Dim distance = p1.DistanceTo(p2)
        If distance < 10 Then ' enough to get a line with some credibility
            strOut = "Gravity vector not found " + vbCrLf + "The distance of p1 to p2 is " +
                     CStr(CInt(distance)) + " pixels." + vbCrLf
            strOut += "Using the previous value for the gravity vector."
        Else
            Dim lp = New linePoints(p1, p2)
            task.gravityVec = New linePoints(lp.xp1, lp.xp2)
            If standaloneTest() Then displayResults(p1, p2)
        End If

        task.horizonVec = task.gravityHorizon.computePerp(task.gravityVec)
        SetTrueText(strOut, 3)
    End Sub
End Class




Public Class XO_CameraMotion_Basics : Inherits TaskParent
    Public translationX As Integer
    Public translationY As Integer
    Public secondOpinion As Boolean
    Dim feat As New Swarm_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        task.gOptions.setDebugSlider(3)
        desc = "Merge with previous image using just translation of the gravity vector and horizon vector (if present)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim gravityVec = New linePoints(task.gravityVec.p1, task.gravityVec.p2)
        Dim horizonVec = New linePoints(task.horizonVec.p1, task.horizonVec.p2)

        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        translationX = task.gOptions.DebugSlider.Value ' Math.Round(gravityVec.p1.X - task.gravityVec.p1.X)
        translationY = task.gOptions.DebugSlider.Value ' Math.Round(horizonVec.p1.Y - task.horizonVec.p1.Y)
        If Math.Abs(translationX) >= dst2.Width / 2 Then translationX = 0
        If horizonVec.p1.Y >= dst2.Height Or horizonVec.p2.Y >= dst2.Height Or Math.Abs(translationY) >= dst2.Height / 2 Then
            horizonVec = New linePoints(New cv.Point2f, New cv.Point2f(336, 0))
            translationY = 0
        End If

        Dim r1 As cv.Rect, r2 As cv.Rect
        If translationX = 0 And translationY = 0 Then
            dst2 = src
            task.camMotionPixels = 0
            task.camDirection = 0
        Else
            ' dst2.SetTo(0)
            r1 = New cv.Rect(translationX, translationY, Math.Min(dst2.Width - translationX * 2, dst2.Width),
                                                         Math.Min(dst2.Height - translationY * 2, dst2.Height))
            If r1.X < 0 Then
                r1.X = -r1.X
                r1.Width += translationX * 2
            End If
            If r1.Y < 0 Then
                r1.Y = -r1.Y
                r1.Height += translationY * 2
            End If

            r2 = New cv.Rect(Math.Abs(translationX), Math.Abs(translationY), r1.Width, r1.Height)

            task.camMotionPixels = Math.Sqrt(translationX * translationX + translationY * translationY)
            If translationX = 0 Then
                If translationY < 0 Then task.camDirection = Math.PI / 4 Else task.camDirection = Math.PI * 3 / 4
            Else
                task.camDirection = Math.Atan(translationY / translationX)
            End If

            If secondOpinion Then
                dst3.SetTo(0)
                ' the point cloud contributes one set of camera motion distance and direction.  Now confirm it with feature points
                feat.Run(src)
                strOut = "Swarm distance = " + Format(feat.distanceAvg, fmt1) + " when camMotionPixels = " + Format(task.camMotionPixels, fmt1)
                If (feat.distanceAvg < task.camMotionPixels / 2) Or task.heartBeat Then
                    task.camMotionPixels = 0
                    src.CopyTo(dst2)
                End If
                dst3 = (src - dst2).ToMat.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            End If
        End If

        gravityVec = New linePoints(task.gravityVec.p1, task.gravityVec.p2)
        horizonVec = New linePoints(task.horizonVec.p1, task.horizonVec.p2)
        SetTrueText(strOut, 3)

        labels(2) = "Translation (X, Y) = (" + CStr(translationX) + ", " + CStr(translationY) + ")" +
                    If(horizonVec.p1.Y = 0 And horizonVec.p2.Y = 0, " there is no horizon present", "")
        labels(3) = "Camera direction (radians) = " + Format(task.camDirection, fmt1) + " with distance = " + Format(task.camMotionPixels, fmt1)
    End Sub
End Class




Public Class XO_CameraMotion_WithRotation : Inherits TaskParent
    Public translationX As Single
    Public rotationX As Single
    Public centerX As cv.Point2f
    Public translationY As Single
    Public rotationY As Single
    Public centerY As cv.Point2f
    Public rotate As New Rotate_BasicsQT
    Dim gravityVec As linePoints
    Dim horizonVec As linePoints
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Merge with previous image using rotation AND translation of the camera motion - not as good as translation alone."
    End Sub
    Public Sub translateRotateX(x1 As Integer, x2 As Integer)
        rotationX = Math.Atan(Math.Abs((x1 - x2)) / dst2.Height) * 57.2958
        centerX = New cv.Point2f((task.gravityVec.p1.X + task.gravityVec.p2.X) / 2, (task.gravityVec.p1.Y + task.gravityVec.p2.Y) / 2)
        If x1 >= 0 And x2 > 0 Then
            translationX = If(x1 > x2, x1 - x2, x2 - x1)
            centerX = task.gravityVec.p2
        ElseIf x1 <= 0 And x2 < 0 Then
            translationX = If(x1 > x2, x1 - x2, x2 - x1)
            centerX = task.gravityVec.p1
        ElseIf x1 < 0 And x2 > 0 Then
            translationX = 0
        Else
            translationX = 0
            rotationX *= -1
        End If
    End Sub
    Public Sub translateRotateY(y1 As Integer, y2 As Integer)
        rotationY = Math.Atan(Math.Abs((y1 - y2)) / dst2.Width) * 57.2958
        centerY = New cv.Point2f((task.horizonVec.p1.X + task.horizonVec.p2.X) / 2, (task.horizonVec.p1.Y + task.horizonVec.p2.Y) / 2)
        If y1 > 0 And y2 > 0 Then
            translationY = If(y1 > y2, y1 - y2, y2 - y1)
            centerY = task.horizonVec.p2
        ElseIf y1 < 0 And y2 < 0 Then
            translationY = If(y1 > y2, y1 - y2, y2 - y1)
            centerY = task.horizonVec.p1
        ElseIf y1 < 0 And y2 > 0 Then
            translationY = 0
        Else
            translationY = 0
            rotationY *= -1
        End If
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.firstPass Then
            gravityVec = task.gravityVec
            horizonVec = task.horizonVec
        End If

        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim x1 = gravityVec.p1.X - task.gravityVec.p1.X
        Dim x2 = gravityVec.p2.X - task.gravityVec.p2.X

        Dim y1 = horizonVec.p1.Y - task.horizonVec.p1.Y
        Dim y2 = horizonVec.p2.Y - task.horizonVec.p2.Y

        translateRotateX(x1, x2)
        translateRotateY(y1, y2)

        dst1.SetTo(0)
        dst3.SetTo(0)
        If Math.Abs(x1 - x2) > 0.5 Or Math.Abs(y1 - y2) > 0.5 Then
            Dim r1 = New cv.Rect(translationX, translationY, dst2.Width - translationX, dst2.Height - translationY)
            Dim r2 = New cv.Rect(0, 0, r1.Width, r1.Height)
            dst1(r2) = src(r1)
            rotate.rotateAngle = rotationY
            rotate.rotateCenter = centerY
            rotate.Run(dst1)
            dst2 = rotate.dst2
            dst3 = (src - dst2).ToMat.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
        Else
            dst2 = src
        End If

        gravityVec = task.gravityVec
        horizonVec = task.horizonVec

        labels(2) = "Translation X = " + Format(translationX, fmt1) + " rotation X = " + Format(rotationX, fmt1) + " degrees " +
                    " center of rotation X = " + Format(centerX.X, fmt0) + ", " + Format(centerX.Y, fmt0)
        labels(3) = "Translation Y = " + Format(translationY, fmt1) + " rotation Y = " + Format(rotationY, fmt1) + " degrees " +
                    " center of rotation Y = " + Format(centerY.X, fmt0) + ", " + Format(centerY.Y, fmt0)
    End Sub
End Class






Public Class XO_Rotate_Horizon : Inherits TaskParent
    Dim rotate As New Rotate_Basics
    Dim edges As New XO_CameraMotion_WithRotation
    Public Sub New()
        optiBase.FindSlider("Rotation Angle in degrees").Value = 3
        labels(2) = "White is the current horizon vector of the camera.  Highlighted color is the rotated horizon vector."
        desc = "Rotate the horizon independently from the rotation of the image to validate the Edge_CameraMotion algorithm."
    End Sub
    Function RotatePoint(point As cv.Point2f, center As cv.Point2f, angle As Double) As cv.Point2f
        Dim radians As Double = angle * (cv.Cv2.PI / 180.0)

        Dim sinAngle As Double = Math.Sin(radians)
        Dim cosAngle As Double = Math.Cos(radians)

        Dim x As Double = point.X - center.X
        Dim y As Double = point.Y - center.Y

        Dim xNew As Double = x * cosAngle - y * sinAngle
        Dim yNew As Double = x * sinAngle + y * cosAngle

        xNew += center.X
        yNew += center.Y

        Return New cv.Point2f(xNew, yNew)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        rotate.Run(src)
        dst2 = rotate.dst2.Clone
        dst1 = dst2.Clone

        Dim horizonVec = New linePoints(task.horizonVec.p1, task.horizonVec.p2)

        horizonVec.p1 = RotatePoint(task.horizonVec.p1, rotate.rotateCenter, -rotate.rotateAngle)
        horizonVec.p2 = RotatePoint(task.horizonVec.p2, rotate.rotateCenter, -rotate.rotateAngle)

        DrawLine(dst2, horizonVec.p1, horizonVec.p2, task.HighlightColor)
        DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, white)

        Dim y1 = horizonVec.p1.Y - task.horizonVec.p1.Y
        Dim y2 = horizonVec.p2.Y - task.horizonVec.p2.Y
        edges.translateRotateY(y1, y2)

        rotate.rotateAngle = edges.rotationY
        rotate.rotateCenter = edges.centerY
        rotate.Run(dst1)
        dst3 = rotate.dst2.Clone

        strOut = edges.strOut
    End Sub
End Class






Public Class XO_Gravity_BasicsOriginal : Inherits TaskParent
    Public vec As New linePoints
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Search for the transition from positive to negative to find the gravity vector."
    End Sub
    Private Function findTransition(startRow As Integer, stopRow As Integer, stepRow As Integer) As cv.Point2f
        Dim val As Single, lastVal As Single
        Dim ptX As New List(Of Single)
        Dim ptY As New List(Of Single)
        For y = startRow To stopRow Step stepRow
            For x = 0 To dst0.Cols - 1
                lastVal = val
                val = dst0.Get(Of Single)(y, x)
                If val > 0 And lastVal < 0 Then
                    ' change to sub-pixel accuracy here 
                    Dim pt = New cv.Point2f(x + Math.Abs(val) / Math.Abs(val - lastVal), y)
                    ptX.Add(pt.X)
                    ptY.Add(pt.Y)
                    If ptX.Count >= task.frameHistoryCount Then Return New cv.Point2f(ptX.Average, ptY.Average)
                End If
            Next
        Next
        Return New cv.Point
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = task.gravitySplit(0) Else dst0 = src

        Dim p1 = findTransition(0, dst0.Height - 1, 1)
        Dim p2 = findTransition(dst0.Height - 1, 0, -1)
        Dim lp = New linePoints(p1, p2)
        vec = New linePoints(lp.xp1, lp.xp2)

        If p1.X >= 1 Then
            strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf + "      val =  " +
                      Format(dst0.Get(Of Single)(p1.Y, p1.X)) + vbCrLf + "lastVal = " + Format(dst0.Get(Of Single)(p1.Y, p1.X - 1))
        End If
        SetTrueText(strOut, 3)

        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, vec.p1, vec.p2, 255)
        End If
    End Sub
End Class




Public Class XO_Depth_MinMaxToVoronoi : Inherits TaskParent
    Public Sub New()
        ReDim task.kalman.kInput(task.gridRects.Count * 4 - 1)

        labels = {"", "", "Red is min distance, blue is max distance", "Voronoi representation of min point (only) for each cell."}
        desc = "Find min and max depth in each roi and create a voronoi representation using the min and max points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then ReDim task.kalman.kInput(task.gridRects.Count * 4 - 1)

        Parallel.For(0, task.gridRects.Count,
        Sub(i)
            Dim roi = task.gridRects(i)
            Dim mm As mmData = GetMinMax(task.pcSplit(2)(roi), task.depthMask(roi))
            If mm.minLoc.X < 0 Or mm.minLoc.Y < 0 Then mm.minLoc = New cv.Point2f(0, 0)
            task.kalman.kInput(i * 4) = mm.minLoc.X
            task.kalman.kInput(i * 4 + 1) = mm.minLoc.Y
            task.kalman.kInput(i * 4 + 2) = mm.maxLoc.X
            task.kalman.kInput(i * 4 + 3) = mm.maxLoc.Y
        End Sub)

        task.kalman.Run(src)

        Static minList(task.gridRects.Count - 1) As cv.Point2f
        Static maxList(task.gridRects.Count - 1) As cv.Point2f
        For i = 0 To task.gridRects.Count - 1
            Dim rect = task.gridRects(i)
            If task.motionBasics.motionFlags(i) Then
                Dim ptmin = New cv.Point2f(task.kalman.kOutput(i * 4) + rect.X, task.kalman.kOutput(i * 4 + 1) + rect.Y)
                Dim ptmax = New cv.Point2f(task.kalman.kOutput(i * 4 + 2) + rect.X, task.kalman.kOutput(i * 4 + 3) + rect.Y)
                ptmin = validatePoint(ptmin)
                ptmax = validatePoint(ptmax)
                minList(i) = ptmin
                maxList(i) = ptmax
            End If
        Next

        dst1 = src.Clone()
        dst1.SetTo(white, task.gridMask)
        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, src.Width, src.Height))
        For i = 0 To minList.Count - 1
            Dim ptMin = minList(i)
            subdiv.Insert(ptMin)
            DrawCircle(dst1, ptMin, task.DotSize, cv.Scalar.Red)
            DrawCircle(dst1, maxList(i), task.DotSize, cv.Scalar.Blue)
        Next

        If task.optionsChanged Then dst2 = dst1.Clone Else dst1.CopyTo(dst2, task.motionMask)

        Dim facets = New cv.Point2f()() {Nothing}
        Dim centers() As cv.Point2f
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, centers)

        Dim ifacet() As cv.Point
        Dim ifacets = New cv.Point()() {Nothing}

        For i = 0 To facets.Length - 1
            ReDim ifacet(facets(i).Length - 1)
            For j = 0 To facets(i).Length - 1
                ifacet(j) = New cv.Point(Math.Round(facets(i)(j).X), Math.Round(facets(i)(j).Y))
            Next
            ifacets(0) = ifacet
            dst3.FillConvexPoly(ifacet, task.scalarColors(i Mod task.scalarColors.Length), task.lineType)
            cv.Cv2.Polylines(dst3, ifacets, True, cv.Scalar.Black, task.lineWidth, task.lineType, 0)
        Next
    End Sub
End Class