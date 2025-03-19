Imports System.Runtime.InteropServices
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
            Dim horizonVec As lpData, gravityVec As lpData
            If hPoints.Total > 0 Then
                horizonVec = New lpData(New cv.Point(0, yLeft), New cv.Point(dst2.Width, yRight))
            Else
                horizonVec = New lpData
            End If

            gravityVec = New lpData(New cv.Point(xTop, 0), New cv.Point(xBot, dst2.Height))

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
        task.gravityVec = New lpData(New cv.Point2f(dst2.Width / 2, 0),
                                         New cv.Point2f(dst2.Width / 2, dst2.Height))
        task.horizonVec = New lpData(New cv.Point2f(0, dst2.Height / 2), New cv.Point2f(dst2.Width, dst2.Height / 2))
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

            Dim lp = New lpData(p1, p2)
            task.horizonVec = New lpData(lp.xp1, lp.xp2)
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
                task.gravityVec = New lpData(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
            Else
                Dim lp = New lpData(p1, p2)
                task.gravityVec = New lpData(lp.xp1, lp.xp2)
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
    Public vec As New lpData
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
            vec = New lpData
            strOut = "Horizon not found " + vbCrLf + "The distance of p1 to p2 is " + CStr(CInt(distance)) + " pixels."
        Else
            Dim lp = New lpData(p1, p2)
            vec = New lpData(lp.xp1, lp.xp2)
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
            Dim lp = New lpData(p1, p2)
            task.gravityVec = New lpData(lp.xp1, lp.xp2)
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
        Dim gravityVec = New lpData(task.gravityVec.p1, task.gravityVec.p2)
        Dim horizonVec = New lpData(task.horizonVec.p1, task.horizonVec.p2)

        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        translationX = task.gOptions.DebugSlider.Value ' Math.Round(gravityVec.p1.X - task.gravityVec.p1.X)
        translationY = task.gOptions.DebugSlider.Value ' Math.Round(horizonVec.p1.Y - task.horizonVec.p1.Y)
        If Math.Abs(translationX) >= dst2.Width / 2 Then translationX = 0
        If horizonVec.p1.Y >= dst2.Height Or horizonVec.p2.Y >= dst2.Height Or Math.Abs(translationY) >= dst2.Height / 2 Then
            horizonVec = New lpData(New cv.Point2f, New cv.Point2f(336, 0))
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

        gravityVec = New lpData(task.gravityVec.p1, task.gravityVec.p2)
        horizonVec = New lpData(task.horizonVec.p1, task.horizonVec.p2)
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
    Dim gravityVec As lpData
    Dim horizonVec As lpData
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

        Dim horizonVec = New lpData(task.horizonVec.p1, task.horizonVec.p2)

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
    Public vec As New lpData
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
        Dim lp = New lpData(p1, p2)
        vec = New lpData(lp.xp1, lp.xp2)

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









Public Class XO_GridCell_GrayScaleTest : Inherits TaskParent
    Dim options As New Options_Stdev
    Public Sub New()
        labels(3) = "grid cells where grayscale stdev and average of the 3 color stdev's"
        desc = "Is the average of the color stdev's the same as the stdev of the grayscale?"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()
        Dim threshold = options.stdevThreshold

        Dim pt = task.mouseD.ptTopLeft
        Dim grayMean As cv.Scalar, grayStdev As cv.Scalar
        Dim ColorMean As cv.Scalar, colorStdev As cv.Scalar
        Static saveTrueData As New List(Of TrueText)
        If task.heartBeat Then
            dst3.SetTo(0)
            dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim count As Integer
            For Each gc In task.gcList
                cv.Cv2.MeanStdDev(dst2(gc.rect), grayMean, grayStdev)
                cv.Cv2.MeanStdDev(task.color(gc.rect), ColorMean, colorStdev)
                Dim nextColorStdev = (colorStdev(0) + colorStdev(1) + colorStdev(2)) / 3
                Dim diff = Math.Abs(grayStdev(0) - nextColorStdev)
                If diff > threshold Then
                    dst2.Rectangle(gc.rect, 255, task.lineWidth)
                    SetTrueText(Format(grayStdev(0), fmt1) + " " + Format(colorStdev, fmt1), gc.rect.TopLeft, 2)
                    dst3.Rectangle(gc.rect, task.HighlightColor, task.lineWidth)
                    SetTrueText(Format(diff, fmt1), gc.rect.TopLeft, 3)
                    count += 1
                End If
            Next
            labels(2) = "There were " + CStr(count) + " cells where the difference was greater than " + CStr(threshold)
        End If

        If trueData.Count > 0 Then saveTrueData = New List(Of TrueText)(trueData)
        trueData = New List(Of TrueText)(saveTrueData)
    End Sub
End Class






Public Class XO_Feature_AgastHeartbeat : Inherits TaskParent
    Dim stablePoints As List(Of cv.Point2f)
    Dim agastFD As cv.AgastFeatureDetector
    Dim lastPoints As List(Of cv.Point2f)
    Public Sub New()
        agastFD = cv.AgastFeatureDetector.Create(10, True, cv.AgastFeatureDetector.DetectorType.OAST_9_16)
        desc = "Use the Agast Feature Detector in the OpenCV Contrib."
        stablePoints = New List(Of cv.Point2f)()
        lastPoints = New List(Of cv.Point2f)()
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim resizeFactor As Integer = 1
        Dim input As New cv.Mat()
        If src.Cols >= 1280 Then
            cv.Cv2.Resize(src, input, New cv.Size(src.Cols \ 4, src.Rows \ 4))
            resizeFactor = 4
        Else
            input = src
        End If
        Dim keypoints As cv.KeyPoint() = agastFD.Detect(input)
        If task.heartBeat OrElse lastPoints.Count < 10 Then
            lastPoints.Clear()
            For Each kpt As cv.KeyPoint In keypoints
                lastPoints.Add(kpt.Pt)
            Next
        End If
        stablePoints.Clear()
        dst2 = src.Clone()
        For Each pt As cv.KeyPoint In keypoints
            Dim p1 As New cv.Point2f(CSng(Math.Round(pt.Pt.X * resizeFactor)), CSng(Math.Round(pt.Pt.Y * resizeFactor)))
            If lastPoints.Contains(p1) Then
                stablePoints.Add(p1)
                DrawCircle(dst2, p1, task.DotSize, New cv.Scalar(0, 0, 255))
            End If
        Next
        lastPoints = New List(Of cv.Point2f)(stablePoints)
        If task.midHeartBeat Then
            labels(2) = $"{keypoints.Length} features found and {stablePoints.Count} of them were stable"
        End If
        labels(2) = $"Found {keypoints.Length} features"
    End Sub
End Class







Public Class XO_Line_DetectorOld : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public ptList As New List(Of cv.Point)
    Public lpMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
    Public subsetRect As cv.Rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
    Public Sub New()
        dst2 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src(subsetRect))

        lpList.Clear()
        ptList.Clear()
        lpMap.SetTo(0)
        lpList.Add(New lpData) ' zero placeholder.
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = validatePoint(New cv.Point(CInt(v(0) + subsetRect.X), CInt(v(1) + subsetRect.Y)))
                Dim p2 = validatePoint(New cv.Point(CInt(v(2) + subsetRect.X), CInt(v(3) + subsetRect.Y)))
                Dim lp = New lpData(p1, p2)
                lp.rect = ValidateRect(lp.rect)
                lp.mask = dst2(lp.rect)
                lp.index = lpList.Count
                lpMap.Line(lp.p1, lp.p2, lp.index, task.lineWidth, task.lineType)
                lpList.Add(lp)
                ptList.Add(New cv.Point(CInt(lp.p1.X), CInt(lp.p1.Y)))
            End If
        Next

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next
        labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
    End Sub
End Class







Public Class XO_Line_Core : Inherits TaskParent
    Dim lines As New XO_Line_DetectorOld
    Public lpList As New List(Of lpData)
    Public lpMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Collect lines as always but don't update lines where there was no motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat
        Dim lastList As New List(Of lpData)(lpList)
        Dim histarray(lastList.Count - 1) As Single

        lpList.Clear()
        lpList.Add(New lpData) ' placeholder to allow us to build a map.
        If lastList.Count > 0 Then
            lpMap.SetTo(0, Not task.motionMask)
            cv.Cv2.CalcHist({lpMap}, {0}, emptyMat, histogram, 1, {lastList.Count}, New cv.Rangef() {New cv.Rangef(0, lastList.Count)})
            Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

            For i = 1 To histarray.Count - 1
                If histarray(i) = 0 Then lpList.Add(lastList(i))
            Next
        End If

        lines.Run(src.Clone)
        ReDim histarray(lines.lpList.Count - 1)

        Dim tmp = lines.lpMap.Clone
        tmp.SetTo(0, Not task.motionMask)
        cv.Cv2.CalcHist({tmp}, {0}, emptyMat, histogram, 1, {lines.lpList.Count}, New cv.Rangef() {New cv.Rangef(0, lines.lpList.Count)})
        Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

        For i = 1 To histarray.Count - 1
            If histarray(i) > 0 Then lpList.Add(lines.lpList(i))
        Next

        dst2.SetTo(0)
        lpMap.SetTo(0)
        For i = 0 To lpList.Count - 1
            lpList(i).index = i
            Dim lp = lpList(i)
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            lpMap.Line(lp.p1, lp.p2, lp.index, task.lineWidth, task.lineType)
        Next

        If task.heartBeat Then
            labels(2) = CStr(lines.lpList.Count) + " lines found in Line_Detector in the current image with " +
                        CStr(lpList.Count) + " after filtering with the motion mask."
        End If
    End Sub
End Class





Public Class XO_Line_Stable1 : Inherits TaskParent
    Dim lines As New Line_Detector
    Public lpStable As New List(Of lpData)
    Public ptStable As New List(Of cv.Point)
    Public Sub New()
        desc = "Identify features that consistently present in the image - with motion ignored."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lpSets As New List(Of List(Of lpData))
        Static ptSets As New List(Of List(Of cv.Point))

        If task.optionsChanged Then
            lpSets.Clear()
            ptSets.Clear()
            dst1 = src.Clone
        Else
            src.CopyTo(dst1, task.motionMask)
        End If

        lines.Run(dst1)

        Dim ageThreshold = task.gOptions.FrameHistory.Value

        Static lpStable As New List(Of lpData)
        Static ptStable As New List(Of cv.Point)
        For j = 0 To lines.ptList.Count - 1
            Dim pt = lines.ptList(j)
            Dim lp = lines.lpList(j)

            Dim age As Integer = 0
            For i = ptSets.Count - 1 To 0 Step -1
                If ptSets(i).Contains(pt) Then age += 1
            Next

            If age = ageThreshold Then
                Dim index = ptStable.IndexOf(pt)
                If index < 0 Then
                    lpStable.Add(lp)
                    ptStable.Add(pt)
                Else
                    lp = lpStable(index)
                    lp.pt = pt
                    lp.age += 1
                    lpStable(index) = lp
                    ptStable(index) = pt
                End If
            End If
        Next

        Dim removelist As New List(Of Integer)
        For i = 0 To ptStable.Count - 1
            Dim pt = ptStable(i)
            If lines.ptList.Contains(pt) = False Then removelist.Add(i)
        Next

        For i = removelist.Count - 1 To 0 Step -1
            Dim index = removelist(i)
            lpStable.RemoveAt(index)
            ptStable.RemoveAt(index)
        Next
        dst2 = src
        dst3.SetTo(0)
        For Each lp In lpStable
            dst2.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth)
            dst3.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth)
            ' SetTrueText(CStr(lp.age), lp.pt, 3)
        Next

        lpSets.Add(lines.lpList)
        ptSets.Add(lines.ptList)

        If ptSets.Count > task.gOptions.FrameHistory.Value Then
            ptSets.RemoveAt(0)
            lpSets.RemoveAt(0)
        End If
        labels(3) = "There were " + CStr(lpStable.Count) + " stable lines found."
    End Sub
End Class






Public Class XO_Line_LeftRight : Inherits TaskParent
    Dim lineCore As New XO_Line_Core
    Public Sub New()
        task.gOptions.displayDst1.Checked = True
        desc = "Show lines in both the right and left images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        runLines(src) ' the default is the left view.

        dst2 = task.lines.dst2.Clone
        labels(2) = "Left view" + task.lines.labels(2)

        dst1 = task.rightView
        lineCore.Run(task.rightView)
        dst3 = lineCore.dst2.Clone
        labels(3) = "Right View: " + lineCore.labels(2)

        If standalone Then
            If task.gOptions.DebugCheckBox.Checked Then
                dst2.SetTo(0, task.noDepthMask)
                dst3.SetTo(0, task.noDepthMask)
            End If
        Else
            If task.toggleOnOff Then
                dst2.SetTo(0, task.noDepthMask)
                dst3.SetTo(0, task.noDepthMask)
            End If
        End If
    End Sub
End Class




Public Class XO_Line_Matching : Inherits TaskParent
    Public options As New Options_Line
    Dim lineMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Dim lpList As New List(Of lpData)
    Public Sub New()
        labels(2) = "Highlighted lines were combined from 2 lines.  Click on Line_Core in Treeview to see."
        desc = "Combine lines that are approximately the same line."
    End Sub
    Private Function combine2Lines(lp1 As lpData, lp2 As lpData) As lpData
        If Math.Abs(lp1.slope) >= 1 Then
            If lp1.p1.Y < lp2.p1.Y Then
                Return New lpData(lp1.p1, lp2.p2)
            Else
                Return New lpData(lp2.p1, lp1.p2)
            End If
        Else
            If lp1.p1.X < lp2.p1.X Then
                Return New lpData(lp1.p1, lp2.p2)
            Else
                Return New lpData(lp2.p1, lp1.p2)
            End If
        End If
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()
        dst2 = src.Clone

        If standalone Then task.lines.Run(src)

        If task.firstPass Then optiBase.FindSlider("Min Line Length").Value = 30

        Dim tolerance = 0.1
        Dim newSet As New List(Of lpData)
        Dim removeList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        Dim addList As New List(Of lpData)
        Dim combineCount As Integer
        For i = 0 To task.lpList.Count - 1
            Dim lp = task.lpList(i)
            Dim lpRemove As Boolean = False
            For j = 0 To 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                Dim val = lineMap.Get(Of Integer)(pt.Y, pt.X)
                If val = 0 Then Continue For
                Dim mp = lpList(val - 1)
                If Math.Abs(mp.slope - lp.slope) < tolerance Then
                    Dim lpNew = combine2Lines(lp, mp)
                    If lpNew IsNot Nothing Then
                        addList.Add(lpNew)
                        DrawLine(dst2, lpNew.p1, lpNew.p2, task.HighlightColor)
                        If removeList.Values.Contains(j) = False Then removeList.Add(j, j)
                        lpRemove = True
                        combineCount += 1
                    End If
                End If
            Next
            If lpRemove Then
                If removeList.Values.Contains(i) = False Then removeList.Add(i, i)
            End If
        Next

        For i = 0 To removeList.Count - 1
            task.lpList.RemoveAt(removeList.ElementAt(i).Value)
        Next

        For Each lp In addList
            task.lpList.Add(lp)
        Next
        lpList = New List(Of lpData)(task.lpList)
        lineMap.SetTo(0)
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            If lp.length > options.minLength Then lineMap.Line(lp.p1, lp.p2, i + 1, 2, cv.LineTypes.Link8)
        Next
        lineMap.ConvertTo(dst3, cv.MatType.CV_8U)
        dst3 = dst3.Threshold(0, cv.Scalar.White, cv.ThresholdTypes.Binary)
        If task.heartBeat Then
            labels(2) = CStr(task.lpList.Count) + " lines were input and " + CStr(combineCount) +
                        " lines were matched to the previous frame"
        End If
    End Sub
End Class





Public Class XO_Line_TopX : Inherits TaskParent
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        labels(3) = "The top X lines by length..."
        desc = "Isolate the top X lines by length - lines are already sorted by length."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.lines.Run(src)
        dst2 = task.lines.dst2
        labels(2) = task.lines.labels(2)

        dst3.SetTo(0)
        For i = 0 To 9
            Dim lp = task.lpList(i)
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next
    End Sub
End Class






Public Class XO_Line_DisplayInfoOld : Inherits TaskParent
    Public tcells As New List(Of tCell)
    Dim canny As New Edge_Basics
    Dim blur As New Blur_Basics
    Public distance As Integer
    Public maskCount As Integer
    Dim myCurrentFrame As Integer = -1
    Public Sub New()
        dst1 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(2) = "When running standaloneTest(), a pair of random points is used to test the algorithm."
        desc = "Display the line provided in mp"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src
        If standaloneTest() And task.heartBeat Then
            Dim tc As tCell
            tcells.Clear()
            For i = 0 To 2 - 1
                tc.center = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                tcells.Add(tc)
            Next
        End If
        If tcells.Count < 2 Then Exit Sub

        If myCurrentFrame < task.frameCount Then
            canny.Run(src)
            blur.Run(canny.dst2)
            myCurrentFrame = task.frameCount
        End If
        dst1.SetTo(0)
        Dim p1 = tcells(0).center
        Dim p2 = tcells(1).center
        DrawLine(dst1, p1, p2, 255)

        dst3.SetTo(0)
        blur.dst2.Threshold(1, 255, cv.ThresholdTypes.Binary).CopyTo(dst3, dst1)
        distance = p1.DistanceTo(p2)
        maskCount = dst3.CountNonZero

        For Each tc In tcells
            'dst2.Rectangle(tc.rect, myHighlightColor)
            'dst2.Rectangle(tc.searchRect, white, task.lineWidth)
            SetTrueText(tc.strOut, New cv.Point(tc.rect.X, tc.rect.Y))
        Next

        strOut = "Mask count = " + CStr(maskCount) + ", Expected count = " + CStr(distance) + " or " + Format(maskCount / distance, "0%") + vbCrLf
        DrawLine(dst2, p1, p2, task.HighlightColor)

        strOut += "Color changes when correlation falls below threshold and new line is detected." + vbCrLf +
                  "Correlation coefficient is shown with the depth in meters."
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class XO_Line_Cells : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public Sub New()
        desc = "Identify all lines in the RedColor_Basics cell boundaries"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        task.lines.Run(dst2.Clone)
        dst3 = task.lines.dst2
        lpList = New List(Of lpData)(task.lpList)
        labels(3) = "Number of lines identified: " + CStr(lpList.Count)
    End Sub
End Class








Public Class XO_Line_Canny : Inherits TaskParent
    Dim canny As New Edge_Basics
    Public lpList As New List(Of lpData)
    Dim options As New Options_Line
    Public Sub New()
        labels(3) = "Input to Line_Basics"
        optiBase.FindSlider("Canny Aperture").Value = 7
        optiBase.FindSlider("Min Line Length").Value = 30
        desc = "Find lines in the Canny output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        canny.Run(src)
        dst3 = canny.dst2.Clone

        task.lines.Run(canny.dst2)

        dst2 = task.lines.dst2
        lpList = New List(Of lpData)(task.lpList)
        labels(2) = "Number of lines identified: " + CStr(lpList.Count)
    End Sub
End Class







Public Class XO_Line_KNN : Inherits TaskParent
    Dim swarm As New Swarm_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Use KNN to find the nearest point to an endpoint and connect the 2 lines with a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        runFeature(src)

        swarm.options.RunOpt()
        task.lines.Run(src)
        dst2 = task.lines.dst2

        dst3.SetTo(0)
        swarm.knn.queries.Clear()
        For Each lp In task.lpList
            swarm.knn.queries.Add(lp.p1)
            swarm.knn.queries.Add(lp.p2)
            DrawLine(dst3, lp.p1, lp.p2, 255)
        Next
        swarm.knn.trainInput = New List(Of cv.Point2f)(swarm.knn.queries)
        swarm.knn.Run(src)

        dst3 = swarm.DrawLines().Clone
        labels(2) = task.lines.labels(2)
    End Sub
End Class







Public Class XO_Line_RegionsVB : Inherits TaskParent
    Dim lines As New XO_Line_TimeView
    Dim reduction As New Reduction_Basics
    Const lineMatch = 254
    Public Sub New()
        task.redOptions.BitwiseReduction.Checked = True
        task.redOptions.setBitReductionBar(6)

        If optiBase.FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Show intermediate vertical step results.")
            check.addCheckBox("Run horizontal without vertical step")
        End If

        desc = "Use the reduction values between lines to identify regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static noVertCheck = optiBase.FindCheckBox("Run horizontal without vertical step")
        Static verticalCheck = optiBase.FindCheckBox("Show intermediate vertical step results.")
        reduction.Run(src)
        dst2 = reduction.dst2
        dst3 = dst2.Clone

        lines.Run(src)

        Dim lineMask = lines.dst3
        dst2.SetTo(lineMatch, lineMask)
        dst3.SetTo(lineMatch, lineMask)

        Dim nextB As Byte
        Dim region As Integer = -1
        Dim indexer1 = dst2.GetGenericIndexer(Of Byte)()
        Dim indexer2 = dst3.GetGenericIndexer(Of Byte)()
        If noVertCheck.checked = False Then
            For x = 0 To dst2.Width - 1
                region = -1
                For y = 0 To dst2.Height - 1
                    nextB = indexer1(y, x)
                    If nextB = lineMatch Then
                        region = -1
                    Else
                        If region = -1 Then
                            region = nextB
                        Else
                            indexer1(y, x) = region
                        End If
                    End If
                Next
            Next
        End If

        For y = 0 To dst3.Height - 1
            region = -1
            For x = 0 To dst3.Width - 1
                nextB = indexer2(y, x)
                If nextB = lineMatch Then
                    region = -1
                Else
                    If region = -1 Then
                        If y = 0 Then
                            region = indexer1(y, x)
                        Else
                            Dim vals As New List(Of Integer)
                            Dim counts As New List(Of Integer)
                            For i = x To dst3.Width - 1
                                Dim nextVal = indexer1(y - 1, i)
                                If nextVal = lineMatch Then Exit For
                                If vals.Contains(nextVal) Then
                                    counts(vals.IndexOf(nextVal)) += 1
                                Else
                                    vals.Add(nextVal)
                                    counts.Add(1)
                                End If
                                Dim maxVal = counts.Max
                                region = vals(counts.IndexOf(maxVal))
                            Next
                        End If
                    Else
                        indexer2(y, x) = region
                    End If
                End If
            Next
        Next
        labels(2) = If(verticalCheck.checked, "Intermediate result of vertical step", "Lines detected (below) Regions detected (right image)")
        If noVertCheck.checked And verticalCheck.checked Then labels(2) = "Input to vertical step"
        If verticalCheck.checked = False Then dst2 = lines.dst2.Clone
    End Sub
End Class






Public Class XO_Line_Nearest : Inherits TaskParent
    Public pt As cv.Point2f ' How close is this point to the input line?
    Public lp As New lpData ' the input line.
    Public nearPoint As cv.Point2f
    Public onTheLine As Boolean
    Public distance As Single
    Public Sub New()
        labels(2) = "Yellow line is input line, white dot is the input point, and the white line is the nearest path to the input line."
        desc = "Find the nearest point on a line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() And task.heartBeat Then
            lp.p1 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            lp.p2 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            pt = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        End If

        Dim minX = Math.Min(lp.p1.X, lp.p2.X)
        Dim minY = Math.Min(lp.p1.Y, lp.p2.Y)
        Dim maxX = Math.Max(lp.p1.X, lp.p2.X)
        Dim maxY = Math.Max(lp.p1.Y, lp.p2.Y)

        onTheLine = True
        If lp.p1.X = lp.p2.X Then
            nearPoint = New cv.Point2f(lp.p1.X, pt.Y)
            If pt.Y < minY Or pt.Y > maxY Then onTheLine = False
        Else
            Dim m = (lp.p1.Y - lp.p2.Y) / (lp.p1.X - lp.p2.X)
            If m = 0 Then
                nearPoint = New cv.Point2f(pt.X, lp.p1.Y)
                If pt.X < minX Or pt.X > maxX Then onTheLine = False
            Else
                Dim b1 = lp.p1.Y - lp.p1.X * m

                Dim b2 = pt.Y + pt.X / m
                Dim a1 = New cv.Point2f(0, b2)
                Dim a2 = New cv.Point2f(dst2.Width, b2 + dst2.Width / m)
                Dim x = m * (b2 - b1) / (m * m + 1)
                nearPoint = New cv.Point2f(x, m * x + b1)

                If nearPoint.X < minX Or nearPoint.X > maxX Or nearPoint.Y < minY Or nearPoint.Y > maxY Then onTheLine = False
            End If
        End If

        Dim distance1 = Math.Sqrt(Math.Pow(pt.X - lp.p1.X, 2) + Math.Pow(pt.Y - lp.p1.Y, 2))
        Dim distance2 = Math.Sqrt(Math.Pow(pt.X - lp.p2.X, 2) + Math.Pow(pt.Y - lp.p2.Y, 2))
        If onTheLine = False Then nearPoint = If(distance1 < distance2, lp.p1, lp.p2)
        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
            DrawLine(dst2, pt, nearPoint, white)
            DrawCircle(dst2, pt, task.DotSize, white)
        End If
        distance = Math.Sqrt(Math.Pow(pt.X - nearPoint.X, 2) + Math.Pow(pt.Y - nearPoint.Y, 2))
    End Sub
End Class








Public Class XO_Line_TimeView : Inherits TaskParent
    Public frameList As New List(Of List(Of lpData))
    Public pixelcount As Integer
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Collect lines over time"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.lines.Run(src)

        If task.optionsChanged Then frameList.Clear()
        Dim nextMpList = New List(Of lpData)(task.lpList)
        frameList.Add(nextMpList)

        dst2 = src
        dst3.SetTo(0)
        lpList.Clear()
        Dim lineTotal As Integer
        For i = 0 To frameList.Count - 1
            lineTotal += frameList(i).Count
            For Each lp In frameList(i)
                DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
                DrawLine(dst3, lp.p1, lp.p2, white)
                lpList.Add(lp)
            Next
        Next

        If frameList.Count >= task.frameHistoryCount Then frameList.RemoveAt(0)
        pixelcount = dst3.CountNonZero
        labels(3) = "There were " + CStr(lineTotal) + " lines detected using " + Format(pixelcount / 1000, "#.0") + "k pixels"
    End Sub
End Class







Public Class XO_Line_ColorClass : Inherits TaskParent
    Dim color8U As New Color8U_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "", "Lines for the current color class", "Color Class input"}
        desc = "Review lines in all the different color classes"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        dst1 = color8U.dst3

        task.lines.Run(dst1 * 255 / color8U.classCount)
        dst2 = task.lines.dst2
        dst3 = task.lines.dst2

        labels(1) = "Input to Line_Basics"
        labels(2) = "Lines found in the " + color8U.classifier.traceName + " output"
    End Sub
End Class





Public Class XO_Line_FromContours : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim contours As New Contour_Gray
    Public Sub New()
        task.redOptions.ColorSource.SelectedItem() = "Reduction_Basics" ' to enable sliders.
        task.gOptions.HighlightColor.SelectedIndex = 3
        UpdateAdvice("Use the reduction sliders in the redoptions to control contours and subsequent lines found.")
        desc = "Find the lines in the contours."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        contours.Run(reduction.dst2)
        dst2 = contours.dst2.Clone
        task.lines.Run(dst2)

        dst3.SetTo(0)
        For Each lp In task.lpList
            DrawLine(dst3, lp.p1, lp.p2, white)
        Next
    End Sub
End Class








Public Class XO_Line_ViewSide : Inherits TaskParent
    Public autoY As New OpAuto_YRange
    Dim histSide As New Projection_HistSide
    Public Sub New()
        labels = {"", "", "Hotspots in the Side View", "Lines found in the hotspots of the Side View."}
        desc = "Find lines in the hotspots for the side view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histSide.Run(src)

        autoY.Run(histSide.histogram)
        dst2 = histSide.histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

        task.lines.Run(dst2.Clone)
        dst3 = task.lines.dst2
        labels(2) = task.lines.labels(2)
    End Sub
End Class






Public Class XO_Line_ViewTop : Inherits TaskParent
    Public autoX As New OpAuto_XRange
    Dim histTop As New Projection_HistTop
    Public Sub New()
        labels = {"", "", "Hotspots in the Top View", "Lines found in the hotspots of the Top View."}
        desc = "Find lines in the hotspots for the Top View."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histTop.Run(src)

        autoX.Run(histTop.histogram)
        dst2 = histTop.histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

        task.lines.Run(dst2)
        dst3 = task.lines.dst2
        labels(2) = task.lines.labels(2)
    End Sub







    Public Class XO_Line_GCloud : Inherits TaskParent
        Public sortedVerticals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
        Public sortedHorizontals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
        Public allLines As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
        Public options As New Options_LineFinder
        Dim match As New Match_tCell
        Dim angleSlider As System.Windows.Forms.TrackBar
        Public Sub New()
            angleSlider = optiBase.FindSlider("Angle tolerance in degrees")
            labels(2) = "Line_GCloud - Blue are vertical lines using the angle thresholds."
            desc = "Find all the vertical lines using the point cloud rectified with the IMU vector for gravity."
        End Sub
        Public Function updateGLine(src As cv.Mat, gc As gravityLine, p1 As cv.Point, p2 As cv.Point) As gravityLine
            gc.tc1.center = p1
            gc.tc2.center = p2
            gc.tc1 = match.createCell(src, gc.tc1.correlation, p1)
            gc.tc2 = match.createCell(src, gc.tc2.correlation, p2)
            gc.tc1.strOut = Format(gc.tc1.correlation, fmt2) + vbCrLf + Format(gc.tc1.depth, fmt2) + "m"
            gc.tc2.strOut = Format(gc.tc2.correlation, fmt2) + vbCrLf + Format(gc.tc2.depth, fmt2) + "m"

            Dim mean = task.pointCloud(gc.tc1.rect).Mean(task.depthMask(gc.tc1.rect))
            gc.pt1 = New cv.Point3f(mean(0), mean(1), mean(2))
            gc.tc1.depth = gc.pt1.Z
            mean = task.pointCloud(gc.tc2.rect).Mean(task.depthMask(gc.tc2.rect))
            gc.pt2 = New cv.Point3f(mean(0), mean(1), mean(2))
            gc.tc2.depth = gc.pt2.Z

            gc.len3D = distance3D(gc.pt1, gc.pt2)
            If gc.pt1 = New cv.Point3f Or gc.pt2 = New cv.Point3f Then
                gc.len3D = 0
            Else
                gc.arcX = Math.Asin((gc.pt1.X - gc.pt2.X) / gc.len3D) * 57.2958
                gc.arcY = Math.Abs(Math.Asin((gc.pt1.Y - gc.pt2.Y) / gc.len3D) * 57.2958)
                If gc.arcY > 90 Then gc.arcY -= 90
                gc.arcZ = Math.Asin((gc.pt1.Z - gc.pt2.Z) / gc.len3D) * 57.2958
            End If

            Return gc
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.RunOpt()

            Dim maxAngle = angleSlider.Value

            dst2 = src.Clone
            task.lines.Run(src.Clone)

            sortedVerticals.Clear()
            sortedHorizontals.Clear()
            For Each lp In task.lpList
                Dim gc As gravityLine
                gc = updateGLine(src, gc, lp.p1, lp.p2)
                allLines.Add(lp.p1.DistanceTo(lp.p2), gc)
                If Math.Abs(90 - gc.arcY) < maxAngle And gc.tc1.depth > 0 And gc.tc2.depth > 0 Then
                    sortedVerticals.Add(lp.p1.DistanceTo(lp.p2), gc)
                    DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Blue)
                End If
                If Math.Abs(gc.arcY) <= maxAngle And gc.tc1.depth > 0 And gc.tc2.depth > 0 Then
                    sortedHorizontals.Add(lp.p1.DistanceTo(lp.p2), gc)
                    DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
                End If
            Next

            labels(2) = Format(sortedHorizontals.Count, "00") + " Horizontal lines were identified and " + Format(sortedVerticals.Count, "00") + " Vertical lines were identified."
        End Sub
    End Class
End Class







Public Class XO_Line_Movement : Inherits TaskParent
    Public p1 As cv.Point
    Public p2 As cv.Point
    Dim gradientColors(100) As cv.Scalar
    Dim frameCount As Integer
    Public Sub New()
        task.kalman.kOutput = {0, 0, 0, 0}

        Dim color1 = cv.Scalar.Yellow, color2 = cv.Scalar.Blue
        Dim f As Double = 1.0
        For i = 0 To gradientColors.Length - 1
            gradientColors(i) = New cv.Scalar(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1), f * color2(2) + (1 - f) * color1(2))
            f -= 1 / gradientColors.Length
        Next

        labels = {"", "", "Line Movement", ""}
        desc = "Show the movement of the line provided"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            Static k1 = p1
            Static k2 = p2
            If k1.DistanceTo(p1) = 0 And k2.DistanceTo(p2) = 0 Then
                k1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                k2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                dst2.SetTo(0)
            End If
            task.kalman.kInput = {k1.X, k1.Y, k2.X, k2.Y}
            task.kalman.Run(src)
            p1 = New cv.Point(task.kalman.kOutput(0), task.kalman.kOutput(1))
            p2 = New cv.Point(task.kalman.kOutput(2), task.kalman.kOutput(3))
        End If
        frameCount += 1
        DrawLine(dst2, p1, p2, gradientColors(frameCount Mod gradientColors.Count))
    End Sub
End Class







Public Class XO_Line_InDepthAndBGR : Inherits TaskParent
    Public p1List As New List(Of cv.Point2f)
    Public p2List As New List(Of cv.Point2f)
    Public z1List As New List(Of cv.Point3f) ' the point cloud values corresponding to p1 and p2
    Public z2List As New List(Of cv.Point3f)
    Public Sub New()
        labels(2) = "Lines defined in BGR"
        labels(3) = "Lines in BGR confirmed in the point cloud"
        desc = "Find the BGR lines and confirm they are present in the cloud data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.lines.Run(src)
        dst2 = task.lines.dst2
        If task.lpList.Count = 0 Then Exit Sub

        Dim lineList = New List(Of cv.Rect)
        If task.optionsChanged Then dst3.SetTo(0)
        dst3.SetTo(0, task.motionMask)
        p1List.Clear()
        p2List.Clear()
        z1List.Clear()
        z2List.Clear()
        For Each lp In task.lpList
            If lp.rect.Width = 0 Then Continue For ' skip placeholder
            Dim mask = New cv.Mat(New cv.Size(lp.rect.Width, lp.rect.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
            mask.Line(New cv.Point(CInt(lp.p1.X - lp.rect.X), CInt(lp.p1.Y - lp.rect.Y)),
                      New cv.Point(CInt(lp.p2.X - lp.rect.X), CInt(lp.p2.Y - lp.rect.Y)), 255, task.lineWidth, cv.LineTypes.Link4)
            Dim mean = task.pointCloud(lp.rect).Mean(mask)

            If mean <> New cv.Scalar Then
                Dim mmX = GetMinMax(task.pcSplit(0)(lp.rect), mask)
                Dim mmY = GetMinMax(task.pcSplit(1)(lp.rect), mask)
                Dim len1 = mmX.minLoc.DistanceTo(mmX.maxLoc)
                Dim len2 = mmY.minLoc.DistanceTo(mmY.maxLoc)
                If len1 > len2 Then
                    lp.p1 = New cv.Point(mmX.minLoc.X + lp.rect.X, mmX.minLoc.Y + lp.rect.Y)
                    lp.p2 = New cv.Point(mmX.maxLoc.X + lp.rect.X, mmX.maxLoc.Y + lp.rect.Y)
                Else
                    lp.p1 = New cv.Point(mmY.minLoc.X + lp.rect.X, mmY.minLoc.Y + lp.rect.Y)
                    lp.p2 = New cv.Point(mmY.maxLoc.X + lp.rect.X, mmY.maxLoc.Y + lp.rect.Y)
                End If
                If lp.p1.DistanceTo(lp.p2) > 1 Then
                    DrawLine(dst3, lp.p1, lp.p2, cv.Scalar.Yellow)
                    p1List.Add(lp.p1)
                    p2List.Add(lp.p2)
                    z1List.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p1.Y, lp.p1.X))
                    z2List.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p2.Y, lp.p2.X))
                End If
            End If
        Next
    End Sub
End Class