Imports System.Runtime.InteropServices
Imports System.Windows
Imports System.Windows.Documents
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
            DrawLine(dst2, gravityVec.p1, gravityVec.p2, task.highlight)
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

            Dim lp = findEdgePoints(New lpData(p1, p2))
            task.horizonVec = New lpData(lp.p1, lp.p2)
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
                Dim lp = findEdgePoints(New lpData(p1, p2))
                task.gravityVec = New lpData(lp.p1, lp.p2)
            End If
            DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, 255)
        End If
    End Sub
End Class







Public Class XO_Horizon_Perpendicular : Inherits TaskParent
    Dim perp As New LineRGB_Perpendicular
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
            Dim lp = findEdgePoints(New lpData(p1, p2))
            vec = New lpData(lp.p1, lp.p2)
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
            Dim lp = findEdgePoints(New lpData(p1, p2))
            task.gravityVec = New lpData(lp.p1, lp.p2)
            If standaloneTest() Then displayResults(p1, p2)
        End If

        task.horizonVec = LineRGB_Perpendicular.computePerp(task.gravityVec)
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
        OptionParent.FindSlider("Rotation Angle in degrees X100").Value = 3
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

        DrawLine(dst2, horizonVec.p1, horizonVec.p2, task.highlight)
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
        Dim lp = findEdgePoints(New lpData(p1, p2))
        vec = New lpData(lp.p1, lp.p2)

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
        task.kalman = New Kalman_Basics
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

        task.kalman.Run(emptyMat)

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









Public Class XO_Brick_GrayScaleTest : Inherits TaskParent
    Dim options As New Options_Stdev
    Public Sub New()
        labels(3) = "bricks where grayscale stdev and average of the 3 color stdev's"
        desc = "Is the average of the color stdev's the same as the stdev of the grayscale?"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        Dim threshold = options.stdevThreshold

        Dim pt = task.gcD.rect.TopLeft
        Dim grayMean As cv.Scalar, grayStdev As cv.Scalar
        Dim ColorMean As cv.Scalar, colorStdev As cv.Scalar
        Static saveTrueData As New List(Of TrueText)
        If task.heartBeat Then
            dst3.SetTo(0)
            dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim count As Integer
            For Each brick In task.brickList
                cv.Cv2.MeanStdDev(dst2(brick.rect), grayMean, grayStdev)
                cv.Cv2.MeanStdDev(task.color(brick.rect), ColorMean, colorStdev)
                Dim nextColorStdev = (colorStdev(0) + colorStdev(1) + colorStdev(2)) / 3
                Dim diff = Math.Abs(grayStdev(0) - nextColorStdev)
                If diff > threshold Then
                    dst2.Rectangle(brick.rect, 255, task.lineWidth)
                    SetTrueText(Format(grayStdev(0), fmt1) + " " + Format(colorStdev, fmt1), brick.rect.TopLeft, 2)
                    dst3.Rectangle(brick.rect, task.highlight, task.lineWidth)
                    SetTrueText(Format(diff, fmt1), brick.rect.TopLeft, 3)
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







Public Class XO_Line_BasicsRawOld : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public ptList As New List(Of cv.Point)
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
        lpList.Add(New lpData) ' zero placeholder.
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
                   v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = validatePoint(New cv.Point(CInt(v(0) + subsetRect.X), CInt(v(1) + subsetRect.Y)))
                Dim p2 = validatePoint(New cv.Point(CInt(v(2) + subsetRect.X), CInt(v(3) + subsetRect.Y)))
                Dim lp = New lpData(p1, p2)
                lp.index = lpList.Count
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







Public Class XO_Line_LeftRight : Inherits TaskParent
    Dim lineCore As New XO_Line_Core
    Public Sub New()
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Show lines in both the right and left images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lineRGB.dst2.Clone
        labels(2) = "Left view" + task.lineRGB.labels(2)

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
            If task.toggleOn Then
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
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        labels(2) = "Highlighted lines were combined from 2 lines.  Click on LineRGB_Core in Treeview to see."
        desc = "Combine lines that are approximately the same line."
    End Sub
    Private Function combine2Lines(lp1 As lpData, lp2 As lpData) As lpData
        If Math.Abs(lp1.m) >= 1 Then
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
        options.Run()
        dst2 = src.Clone

        If task.firstPass Then OptionParent.FindSlider("Min Line Length").Value = 30

        Dim tolerance = 0.1
        Dim newSet As New List(Of lpData)
        Dim removeList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        Dim addList As New List(Of lpData)
        Dim combineCount As Integer
        For i = 0 To task.lineRGB.lpList.Count - 1
            Dim lp = task.lineRGB.lpList(i)
            Dim lpRemove As Boolean = False
            For j = 0 To 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                Dim val = lineMap.Get(Of Integer)(pt.Y, pt.X)
                If val = 0 Then Continue For
                Dim mp = lpList(val - 1)
                If Math.Abs(mp.m - lp.m) < tolerance Then
                    Dim lpNew = combine2Lines(lp, mp)
                    If lpNew IsNot Nothing Then
                        addList.Add(lpNew)
                        DrawLine(dst2, lpNew.p1, lpNew.p2, task.highlight)
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
            task.lineRGB.lpList.RemoveAt(removeList.ElementAt(i).Value)
        Next

        For Each lp In addList
            task.lineRGB.lpList.Add(lp)
        Next
        lpList = New List(Of lpData)(task.lineRGB.lpList)
        lineMap.SetTo(0)
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            If lp.length > options.minLength Then lineMap.Line(lp.p1, lp.p2, i + 1, 2, cv.LineTypes.Link8)
        Next
        lineMap.ConvertTo(dst3, cv.MatType.CV_8U)
        dst3 = dst3.Threshold(0, cv.Scalar.White, cv.ThresholdTypes.Binary)
        If task.heartBeat Then
            labels(2) = CStr(task.lineRGB.lpList.Count) + " lines were input and " + CStr(combineCount) +
                            " lines were matched to the previous frame"
        End If
    End Sub
End Class





Public Class XO_Line_TopX : Inherits TaskParent
    Public Sub New()
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        labels(3) = "The top X lines by length..."
        desc = "Isolate the top X lines by length - lines are already sorted by length."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lineRGB.dst2
        labels(2) = task.lineRGB.labels(2)

        dst3.SetTo(0)
        For i = 0 To 9
            Dim lp = task.lineRGB.lpList(i)
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
            'dst2.Rectangle(tc.rect, myhighlight)
            'dst2.Rectangle(tc.searchRect, white, task.lineWidth)
            SetTrueText(tc.strOut, New cv.Point(tc.rect.X, tc.rect.Y))
        Next

        strOut = "Mask count = " + CStr(maskCount) + ", Expected count = " + CStr(distance) + " or " + Format(maskCount / distance, "0%") + vbCrLf
        DrawLine(dst2, p1, p2, task.highlight)

        strOut += "Color changes when correlation falls below threshold and new line is detected." + vbCrLf +
                      "Correlation coefficient is shown with the depth in meters."
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class XO_Line_Cells : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Dim lines As New LineRGB_RawSorted
    Public Sub New()
        desc = "Identify all lines in the RedColor_Basics cell boundaries"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        lines.Run(dst2.Clone)
        dst3 = lines.dst2
        lpList = New List(Of lpData)(lines.lpList)
        labels(3) = "Number of lines identified: " + CStr(lpList.Count)
    End Sub
End Class








Public Class XO_Line_Canny : Inherits TaskParent
    Dim canny As New Edge_Basics
    Public lpList As New List(Of lpData)
    Dim lines As New LineRGB_RawSorted
    Public Sub New()
        labels(3) = "Input to LineRGB_Basics"
        OptionParent.FindSlider("Canny Aperture").Value = 7
        OptionParent.FindSlider("Min Line Length").Value = 30
        desc = "Find lines in the Canny output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        canny.Run(src)
        dst3 = canny.dst2.Clone

        lines.Run(canny.dst2)

        dst2 = lines.dst2
        lpList = New List(Of lpData)(lines.lpList)
        labels(2) = "Number of lines identified: " + CStr(lpList.Count)
    End Sub
End Class







Public Class XO_Line_KNN : Inherits TaskParent
    Dim swarm As New Swarm_Basics
    Public Sub New()
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Use KNN to find the nearest point to an endpoint and connect the 2 lines with a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        swarm.options.Run()
        dst2 = task.lineRGB.dst2

        dst3.SetTo(0)
        swarm.knn.queries.Clear()
        For Each lp In task.lineRGB.lpList
            swarm.knn.queries.Add(lp.p1)
            swarm.knn.queries.Add(lp.p2)
            DrawLine(dst3, lp.p1, lp.p2, 255)
        Next
        swarm.knn.trainInput = New List(Of cv.Point2f)(swarm.knn.queries)
        swarm.knn.Run(src)

        dst3 = swarm.DrawLines().Clone
        labels(2) = task.lineRGB.labels(2)
    End Sub
End Class







Public Class XO_Line_RegionsVB : Inherits TaskParent
    Dim lines As New XO_Line_TimeView
    Dim reduction As New Reduction_Basics
    Const lineMatch = 254
    Public Sub New()
        task.redOptions.BitwiseReduction.Checked = True
        task.redOptions.setBitReductionBar(6)

        If OptionParent.FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Show intermediate vertical step results.")
            check.addCheckBox("Run horizontal without vertical step")
        End If

        desc = "Use the reduction values between lines to identify regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static noVertCheck = OptionParent.FindCheckBox("Run horizontal without vertical step")
        Static verticalCheck = OptionParent.FindCheckBox("Show intermediate vertical step results.")
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
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Collect lines over time"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then frameList.Clear()
        Dim nextMpList = New List(Of lpData)(task.lineRGB.lpList)
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
    Dim lines As New LineRGB_RawSorted
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "", "Lines for the current color class", "Color Class input"}
        desc = "Review lines in all the different color classes"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        dst1 = color8U.dst2

        lines.Run(dst1 * 255 / color8U.classCount)
        dst2 = lines.dst2
        dst3 = lines.dst2

        labels(1) = "Input to LineRGB_Basics"
        labels(2) = "Lines found in the " + color8U.classifier.traceName + " output"
    End Sub
End Class





Public Class XO_Line_FromContours : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim contours As New XO_Contour_Gray
    Dim lines As New LineRGB_RawSorted
    Public Sub New()
        task.redOptions.ColorSource.SelectedItem() = "Reduction_Basics" ' to enable sliders.
        task.gOptions.highlight.SelectedIndex = 3
        desc = "Find the lines in the contours."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        contours.Run(reduction.dst2)
        dst2 = contours.dst2.Clone
        lines.Run(dst2)

        dst3.SetTo(0)
        For Each lp In lines.lpList
            DrawLine(dst3, lp.p1, lp.p2, white)
        Next
    End Sub
End Class








Public Class XO_Line_ViewSide : Inherits TaskParent
    Public autoY As New OpAuto_YRange
    Dim histSide As New Projection_HistSide
    Dim lines As New LineRGB_RawSorted
    Public Sub New()
        labels = {"", "", "Hotspots in the Side View", "Lines found in the hotspots of the Side View."}
        desc = "Find lines in the hotspots for the side view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histSide.Run(src)

        autoY.Run(histSide.histogram)
        dst2 = histSide.histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

        lines.Run(dst2.Clone)
        dst3 = lines.dst2
        labels(2) = lines.labels(2)
    End Sub
End Class







Public Class XO_Line_Movement : Inherits TaskParent
    Public p1 As cv.Point
    Public p2 As cv.Point
    Dim gradientColors(100) As cv.Scalar
    Dim frameCount As Integer
    Public Sub New()
        task.kalman = New Kalman_Basics
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
            task.kalman.Run(emptyMat)
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
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        labels(2) = "Lines defined in BGR"
        labels(3) = "Lines in BGR confirmed in the point cloud"
        desc = "Find the BGR lines and confirm they are present in the cloud data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lineRGB.dst2
        If task.lineRGB.lpList.Count = 0 Then Exit Sub

        Dim lineList = New List(Of cv.Rect)
        If task.optionsChanged Then dst3.SetTo(0)
        dst3.SetTo(0, task.motionMask)
        p1List.Clear()
        p2List.Clear()
        z1List.Clear()
        z2List.Clear()
        For Each lp In task.lineRGB.lpList
            Dim rect = findRectFromLine(lp)
            Dim mask = New cv.Mat(New cv.Size(rect.Width, rect.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
            mask.Line(New cv.Point(CInt(lp.p1.X - rect.X), CInt(lp.p1.Y - rect.Y)),
                          New cv.Point(CInt(lp.p2.X - rect.X), CInt(lp.p2.Y - rect.Y)), 255, task.lineWidth, cv.LineTypes.Link4)
            Dim mean = task.pointCloud(rect).Mean(mask)

            If mean <> New cv.Scalar Then
                Dim mmX = GetMinMax(task.pcSplit(0)(rect), mask)
                Dim mmY = GetMinMax(task.pcSplit(1)(rect), mask)
                Dim len1 = mmX.minLoc.DistanceTo(mmX.maxLoc)
                Dim len2 = mmY.minLoc.DistanceTo(mmY.maxLoc)
                If len1 > len2 Then
                    lp.p1 = New cv.Point(mmX.minLoc.X + rect.X, mmX.minLoc.Y + rect.Y)
                    lp.p2 = New cv.Point(mmX.maxLoc.X + rect.X, mmX.maxLoc.Y + rect.Y)
                Else
                    lp.p1 = New cv.Point(mmY.minLoc.X + rect.X, mmY.minLoc.Y + rect.Y)
                    lp.p2 = New cv.Point(mmY.maxLoc.X + rect.X, mmY.maxLoc.Y + rect.Y)
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










Public Class XO_Line_Core : Inherits TaskParent
    Dim lines As New XO_Line_Core
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
            labels(2) = CStr(lines.lpList.Count) + " lines found in LineRGB_RawSorted in the current image with " +
                            CStr(lpList.Count) + " after filtering with the motion mask."
        End If
    End Sub
End Class





Public Class XO_Line_Basics : Inherits TaskParent
    Public lpMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public lpList As New List(Of lpData)
    Dim lineCore As New XO_Line_Core
    Public Sub New()
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Collect lines across frames using the motion mask."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lineCore.Run(src)

        lpMap.SetTo(0)
        dst2 = src
        dst3.SetTo(0)
        dst2.SetTo(cv.Scalar.White, lineCore.dst2)
        For Each lp In lineCore.lpList
            lpMap.Line(lp.p1, lp.p2, lp.index, task.lineWidth + 1, cv.LineTypes.Link8)
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next

        lpList = New List(Of lpData)(lineCore.lpList)
        task.lineRGB.lpList = New List(Of lpData)(lineCore.lpList)
        labels(2) = lineCore.labels(2)
    End Sub
End Class








Public Class XO_BackProject_LineSide : Inherits TaskParent
    Dim line As New XO_Line_ViewSide
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Backproject the lines found in the side view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        line.Run(src)

        dst2.SetTo(0)
        Dim w = task.lineWidth + 5
        lpList.Clear()
        For Each lp In task.lineRGB.lpList
            If Math.Abs(lp.m) < 0.1 Then
                lp = findEdgePoints(lp)
                dst2.Line(lp.p1, lp.p2, 255, w, task.lineType)
                lpList.Add(lp)
            End If
        Next

        Dim histogram = line.autoY.histogram
        histogram.SetTo(0, Not dst2)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histogram, dst1, task.rangesSide)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        dst3 = src
        dst3.SetTo(white, dst1)
    End Sub
End Class






Public Class XO_OpAuto_FloorCeiling : Inherits TaskParent
    Public bpLine As New XO_BackProject_LineSide
    Public yList As New List(Of Single)
    Public floorY As Single
    Public ceilingY As Single
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Automatically find the Y values that best describes the floor and ceiling (if present)"
    End Sub
    Private Sub rebuildMask(maskLabel As String, min As Single, max As Single)
        Dim mask = task.pcSplit(1).InRange(min, max).ConvertScaleAbs

        Dim mean As cv.Scalar, stdev As cv.Scalar
        cv.Cv2.MeanStdDev(task.pointCloud, mean, stdev, mask)

        strOut += "The " + maskLabel + " mask has Y mean and stdev are:" + vbCrLf
        strOut += maskLabel + " Y Mean = " + Format(mean(1), fmt3) + vbCrLf
        strOut += maskLabel + " Y Stdev = " + Format(stdev(1), fmt3) + vbCrLf + vbCrLf

        If Math.Abs(mean(1)) > task.yRange / 4 Then dst1 = mask Or dst1
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pad As Single = 0.05 ' pad the estimate by X cm's

        dst2 = src.Clone
        bpLine.Run(src)

        If bpLine.lpList.Count > 0 Then
            strOut = "Y range = " + Format(task.yRange, fmt3) + vbCrLf + vbCrLf
            If task.heartBeat Then yList.Clear()
            If task.heartBeat Then dst1.SetTo(0)
            Dim h = dst2.Height / 2
            For Each lp In bpLine.lpList
                Dim nextY = task.yRange * (lp.p1.Y - h) / h
                If Math.Abs(nextY) > task.yRange / 4 Then yList.Add(nextY)
            Next

            If yList.Count > 0 Then
                If yList.Max > 0 Then rebuildMask("floor", yList.Max - pad, task.yRange)
                If yList.Min < 0 Then rebuildMask("ceiling", -task.yRange, yList.Min + pad)
            End If

            dst2.SetTo(white, dst1)
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class XO_Hough_Sudoku1 : Inherits TaskParent
    Dim lines As New LineRGB_RawSorted
    Public Sub New()
        desc = "FastLineDetect version for finding lines in the Sudoku input."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = cv.Cv2.ImRead(task.HomeDir + "opencv/Samples/Data/sudoku.png").Resize(dst2.Size)
        lines.Run(dst3.Clone)
        dst2 = lines.dst2
        labels(2) = lines.labels(2)
        For Each lp In lines.lpList
            lp = findEdgePoints(lp)
            dst3.Line(lp.p1, lp.p2, cv.Scalar.Red, task.lineWidth, task.lineType)
        Next
    End Sub
End Class




Public Class XO_Line_InterceptsUI : Inherits TaskParent
    Dim lines As New LineRGB_Intercepts
    Dim p2 As cv.Point
    Dim redRadio As System.Windows.Forms.RadioButton
    Dim greenRadio As System.Windows.Forms.RadioButton
    Dim yellowRadio As System.Windows.Forms.RadioButton
    Dim blueRadio As System.Windows.Forms.RadioButton
    Public Sub New()
        redRadio = OptionParent.findRadio("Show Top intercepts")
        greenRadio = OptionParent.findRadio("Show Bottom intercepts")
        yellowRadio = OptionParent.findRadio("Show Right intercepts")
        blueRadio = OptionParent.findRadio("Show Left intercepts")
        labels(2) = "Use mouse in right image to highlight lines"
        desc = "An alternative way to highlight line segments with common slope"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src)
        dst3.SetTo(0)

        Dim red = New cv.Scalar(0, 0, 255)
        Dim green = New cv.Scalar(1, 128, 0)
        Dim yellow = New cv.Scalar(2, 255, 255)
        Dim blue = New cv.Scalar(254, 0, 0)

        Dim center = New cv.Point(dst3.Width / 2, dst3.Height / 2)
        dst3.Line(New cv.Point(0, 0), center, blue, task.lineWidth, cv.LineTypes.Link4)
        dst3.Line(New cv.Point(dst2.Width, 0), center, red, task.lineWidth, cv.LineTypes.Link4)
        dst3.Line(New cv.Point(0, dst2.Height), center, blue, task.lineWidth, cv.LineTypes.Link4)
        dst3.Line(New cv.Point(dst2.Width, dst2.Height), center, yellow, task.lineWidth, cv.LineTypes.Link4)

        Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, cv.Scalar.All(0))
        Dim pt = New cv.Point(center.X, center.Y - 30)
        cv.Cv2.FloodFill(dst3, mask, pt, red, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X, center.Y + 30)
        cv.Cv2.FloodFill(dst3, mask, pt, green, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X - 30, center.Y)
        cv.Cv2.FloodFill(dst3, mask, pt, blue, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X + 30, center.Y)
        cv.Cv2.FloodFill(dst3, mask, pt, yellow, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))
        Dim color = dst3.Get(Of cv.Vec3b)(task.mouseMovePoint.Y, task.mouseMovePoint.X)

        Dim p1 = task.mouseMovePoint
        If p1.X = center.X Then
            If p1.Y <= center.Y Then p2 = New cv.Point(dst3.Width / 2, 0) Else p2 = New cv.Point(dst3.Width, dst3.Height)
        Else
            Dim m = (center.Y - p1.Y) / (center.X - p1.X)
            Dim b = p1.Y - p1.X * m

            If color(0) = 0 Then p2 = New cv.Point(-b / m, 0) ' red zone
            If color(0) = 1 Then p2 = New cv.Point((dst3.Height - b) / m, dst3.Height) ' green
            If color(0) = 2 Then p2 = New cv.Point(dst3.Width, dst3.Width * m + b) ' yellow
            If color(0) = 254 Then p2 = New cv.Point(0, b) ' blue
            DrawLine(dst3, center, p2, cv.Scalar.Black)
        End If
        DrawCircle(dst3, center, task.DotSize, white)
        If color(0) = 0 Then redRadio.Checked = True
        If color(0) = 1 Then greenRadio.Checked = True
        If color(0) = 2 Then yellowRadio.Checked = True
        If color(0) = 254 Then blueRadio.Checked = True

        For Each inter In lines.intercept
            Select Case lines.options.selectedIntercept
                Case 0
                    dst3.Line(New cv.Point(inter.Key, 0), New cv.Point(inter.Key, 10), white,
                             task.lineWidth)
                Case 1
                    dst3.Line(New cv.Point(inter.Key, dst3.Height), New cv.Point(inter.Key, dst3.Height - 10),
                             white, task.lineWidth)
                Case 2
                    dst3.Line(New cv.Point(0, inter.Key), New cv.Point(10, inter.Key), white,
                             task.lineWidth)
                Case 3
                    dst3.Line(New cv.Point(dst3.Width, inter.Key), New cv.Point(dst3.Width - 10, inter.Key),
                             white, task.lineWidth)
            End Select
        Next
        dst2 = lines.dst2
    End Sub
End Class






Public Class XO_Diff_Heartbeat : Inherits TaskParent
    Public cumulativePixels As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "Unstable mask", "Pixel difference"}
        desc = "Diff an image with one from the last heartbeat."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            dst1 = task.gray.Clone
            dst2.SetTo(0)
        End If

        cv.Cv2.Absdiff(task.gray, dst1, dst3)
        cumulativePixels = dst3.CountNonZero
        dst2 = dst2 Or dst3.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







Public Class XO_FitLine_Hough3D : Inherits TaskParent
    Dim hlines As New Hough_Lines_MT
    Public Sub New()
        desc = "Use visual lines to find 3D lines.  This algorithm is NOT working."
        labels(3) = "White is featureless RGB, blue depth shadow"
    End Sub
    Public Sub houghShowLines3D(ByRef dst As cv.Mat, segment As cv.Line3D)
        Dim x As Double = segment.X1 * dst.Cols
        Dim y As Double = segment.Y1 * dst.Rows
        Dim m As Double
        If segment.Vx < 0.001 Then m = 0 Else m = segment.Vy / segment.Vx ' vertical slope a no-no.
        Dim b As Double = y - m * x
        Dim pt1 As cv.Point = New cv.Point(x, y)
        Dim pt2 As cv.Point
        If m = 0 Then pt2 = New cv.Point(x, dst.Rows) Else pt2 = New cv.Point((dst.Rows - b) / m, dst.Rows)
        dst.Line(pt1, pt2, cv.Scalar.Red, task.lineWidth + 2, task.lineType, 0)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If Not task.heartBeat Then Exit Sub
        hlines.Run(src)
        dst3 = hlines.dst3
        Dim mask = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst3 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        src.CopyTo(dst2)

        Dim lines As New List(Of cv.Line3D)
        Dim nullLine = New cv.Line3D(0, 0, 0, 0, 0, 0)
        Parallel.ForEach(task.gridRects,
        Sub(roi)
            Dim depth = task.pcSplit(2)(roi)
            Dim fMask = mask(roi)
            Dim points As New List(Of cv.Point3f)
            Dim rows = src.Rows, cols = src.Cols
            For y = 0 To roi.Height - 1
                For x = 0 To roi.Width - 1
                    If fMask.Get(Of Byte)(y, x) > 0 Then
                        Dim d = depth.Get(Of Single)(y, x)
                        If d > 0 And d < 10000 Then
                            points.Add(New cv.Point3f(x / rows, y / cols, d / 10000))
                        End If
                    End If
                Next
            Next
            Dim line = nullLine
            If points.Count = 0 Then
                ' save the average color for this roi
                Dim mean = task.depthRGB(roi).Mean()
                mean(0) = 255 - mean(0)
                dst3.Rectangle(roi, mean)
            Else
                line = cv.Cv2.FitLine(points.ToArray, cv.DistanceTypes.L2, 0, 0, 0.01)
            End If
            SyncLock lines
                lines.Add(line)
            End SyncLock
        End Sub)
        ' putting this in the parallel for above causes a memory leak - could not find it...
        For i = 0 To task.gridRects.Count - 1
            houghShowLines3D(dst2(task.gridRects(i)), lines.ElementAt(i))
        Next
    End Sub
End Class



Public Class XO_Brick_Basics : Inherits TaskParent
    Public options As New Options_GridCells
    Public thresholdRangeZ As Single
    Public instantUpdate As Boolean = True
    Dim lastCorrelation() As Single
    Public quad As New XO_Quad_Basics
    Public Sub New()
        task.rgbLeftAligned = If(task.cameraName.StartsWith("StereoLabs") Or task.cameraName.StartsWith("Orbbec"), True, False)
        desc = "Create the grid of bricks that reduce depth volatility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If task.optionsChanged Then
            ReDim lastCorrelation(task.gridRects.Count - 1)
        End If

        Dim stdev As cv.Scalar, mean As cv.Scalar
        Dim correlationMat As New cv.Mat
        Dim leftview = If(task.gOptions.LRMeanSubtraction.Checked, task.LRMeanSub.dst2, task.leftView)
        Dim rightView = If(task.gOptions.LRMeanSubtraction.Checked, task.LRMeanSub.dst3, task.rightView)

        task.brickList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim brick As New brickData
            brick.rect = task.gridRects(i)
            brick.age = task.motionBasics.cellAge(i)
            brick.rect = brick.rect
            brick.lRect = brick.rect ' for some cameras the color image and the left image are the same but not all, i.e. Intel Realsense.
            brick.center = New cv.Point(brick.rect.X + brick.rect.Width / 2, brick.rect.Y + brick.rect.Height / 2)
            If task.depthMask(brick.rect).CountNonZero Then
                cv.Cv2.MeanStdDev(task.pcSplit(2)(brick.rect), mean, stdev, task.depthMask(brick.rect))
                brick.depth = mean(0)
            End If

            If brick.depth = 0 Then
                brick.correlation = 0
                brick.rRect = emptyRect
            Else
                brick.mm = GetMinMax(task.pcSplit(2)(brick.rect), task.depthMask(brick.rect))
                If task.rgbLeftAligned Then
                    brick.lRect = brick.rect
                    brick.rRect = brick.lRect
                    brick.rRect.X -= task.calibData.baseline * task.calibData.rgbIntrinsics.fx / brick.depth
                    brick.rRect = ValidateRect(brick.rRect)
                    cv.Cv2.MatchTemplate(leftview(brick.lRect), rightView(brick.rRect), correlationMat,
                                                     cv.TemplateMatchModes.CCoeffNormed)

                    brick.correlation = correlationMat.Get(Of Single)(0, 0)
                Else
                    Dim irPt = Intrinsics_Basics.translate_LeftToRight(task.pointCloud.Get(Of cv.Point3f)(brick.rect.Y, brick.rect.X))
                    If irPt.X < 0 Or (irPt.X = 0 And irPt.Y = 0 And i > 0) Or (irPt.X >= dst2.Width Or irPt.Y >= dst2.Height) Then
                        brick.depth = 0 ' off the grid.
                        brick.lRect = emptyRect
                        brick.rRect = emptyRect
                    Else
                        brick.lRect = New cv.Rect(irPt.X, irPt.Y, brick.rect.Width, brick.rect.Height)
                        brick.lRect = ValidateRect(brick.lRect)

                        brick.rRect = brick.lRect
                        brick.rRect.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / brick.depth
                        brick.rRect = ValidateRect(brick.rRect)
                        cv.Cv2.MatchTemplate(leftview(brick.lRect), rightView(brick.rRect), correlationMat,
                                                      cv.TemplateMatchModes.CCoeffNormed)

                        brick.correlation = correlationMat.Get(Of Single)(0, 0)
                    End If
                End If
            End If

            lastCorrelation(i) = brick.correlation
            brick.index = task.brickList.Count
            task.brickMap(brick.rect).SetTo(i)
            task.brickList.Add(brick)
        Next

        quad.Run(src)

        If task.heartBeat Then labels(2) = CStr(task.brickList.Count) + " bricks have the useful depth values."
    End Sub
End Class




Public Class XO_Quad_Basics : Inherits TaskParent
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        desc = "Create a quad representation of the redCloud data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim shift As cv.Point3f
        If task.ogl IsNot Nothing Then
            Dim ptM = task.ogl.options4.moveAmount
            shift = New cv.Point3f(ptM(0), ptM(1), ptM(2))
        End If

        task.brickMap.SetTo(0)
        dst2.SetTo(0)
        For i = 0 To task.brickList.Count - 1
            Dim brick = task.brickList(i)
            task.brickMap(brick.rect).SetTo(i)
            If brick.depth > 0 Then
                brick.corners.Clear()

                Dim p0 = getWorldCoordinates(brick.rect.TopLeft, brick.depth)
                Dim p1 = getWorldCoordinates(brick.rect.BottomRight, brick.depth)

                ' clockwise around starting in upper left.
                brick.corners.Add(New cv.Point3f(p0.X + shift.X, p0.Y + shift.Y, brick.depth))
                brick.corners.Add(New cv.Point3f(p1.X + shift.X, p0.Y + shift.Y, brick.depth))
                brick.corners.Add(New cv.Point3f(p1.X + shift.X, p1.Y + shift.Y, brick.depth))
                brick.corners.Add(New cv.Point3f(p0.X + shift.X, p1.Y + shift.Y, brick.depth))
            End If
        Next
    End Sub
End Class







Public Class XO_PointCloud_Infinities : Inherits TaskParent
    Public Sub New()
        desc = "Find out if pointcloud has an nan's or inf's.  StereoLabs had some... look for PatchNans."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim infTotal(2) As Integer
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim vec = task.pointCloud.Get(Of cv.Vec3f)(y, x)
                If Single.IsInfinity(vec(0)) Then infTotal(0) += 1
                If Single.IsInfinity(vec(1)) Then infTotal(1) += 1
                If Single.IsInfinity(vec(2)) Then infTotal(2) += 1
            Next
        Next
        SetTrueText("infinities: X " + CStr(infTotal(0)) + ", Y = " + CStr(infTotal(1)) + " Z = " +
                    CStr(infTotal(2)))
    End Sub
End Class





Public Class XO_PointCloud_VerticalHorizontal : Inherits TaskParent
    Public actualCount As Integer

    Public allPointsH As New List(Of cv.Point3f)
    Public allPointsV As New List(Of cv.Point3f)

    Public hList As New List(Of List(Of cv.Point3f))
    Public xyHList As New List(Of List(Of cv.Point))

    Public vList As New List(Of List(Of cv.Point3f))
    Public xyVList As New List(Of List(Of cv.Point))
    Dim options As New Options_PointCloud()
    Public Sub New()
        setPointCloudGrid()
        desc = "Reduce the point cloud to a manageable number points in 3D"
    End Sub
    Public Function findHorizontalPoints(ByRef xyList As List(Of List(Of cv.Point))) As List(Of List(Of cv.Point3f))
        Dim ptlist As New List(Of List(Of cv.Point3f))
        Dim lastVec = New cv.Point3f
        For y = 0 To task.pointCloud.Height - 1 Step task.gridRects(0).Height - 1
            Dim vecList As New List(Of cv.Point3f)
            Dim xyVec As New List(Of cv.Point)
            For x = 0 To task.pointCloud.Width - 1 Step task.gridRects(0).Width - 1
                Dim vec = task.pointCloud.Get(Of cv.Point3f)(y, x)
                Dim jumpZ As Boolean = False
                If vec.Z > 0 Then
                    If (Math.Abs(lastVec.Z - vec.Z) < options.deltaThreshold And lastVec.X < vec.X) Or lastVec.Z = 0 Then
                        actualCount += 1
                        DrawCircle(dst2, New cv.Point(x, y), task.DotSize, white)
                        vecList.Add(vec)
                        xyVec.Add(New cv.Point(x, y))
                    Else
                        jumpZ = True
                    End If
                End If
                If vec.Z = 0 Or jumpZ Then
                    If vecList.Count > 1 Then
                        ptlist.Add(New List(Of cv.Point3f)(vecList))
                        xyList.Add(New List(Of cv.Point)(xyVec))
                    End If
                    vecList.Clear()
                    xyVec.Clear()
                End If
                lastVec = vec
            Next
        Next
        Return ptlist
    End Function
    Public Function findVerticalPoints(ByRef xyList As List(Of List(Of cv.Point))) As List(Of List(Of cv.Point3f))
        Dim ptlist As New List(Of List(Of cv.Point3f))
        Dim lastVec = New cv.Point3f
        For x = 0 To task.pointCloud.Width - 1 Step task.gridRects(0).Width - 1
            Dim vecList As New List(Of cv.Point3f)
            Dim xyVec As New List(Of cv.Point)
            For y = 0 To task.pointCloud.Height - 1 Step task.gridRects(0).Height - 1
                Dim vec = task.pointCloud.Get(Of cv.Point3f)(y, x)
                Dim jumpZ As Boolean = False
                If vec.Z > 0 Then
                    If (Math.Abs(lastVec.Z - vec.Z) < options.deltaThreshold And lastVec.Y < vec.Y) Or lastVec.Z = 0 Then
                        actualCount += 1
                        DrawCircle(dst2, New cv.Point(x, y), task.DotSize, white)
                        vecList.Add(vec)
                        xyVec.Add(New cv.Point(x, y))
                    Else
                        jumpZ = True
                    End If
                End If
                If vec.Z = 0 Or jumpZ Then
                    If vecList.Count > 1 Then
                        ptlist.Add(New List(Of cv.Point3f)(vecList))
                        xyList.Add(New List(Of cv.Point)(xyVec))
                    End If
                    vecList.Clear()
                    xyVec.Clear()
                End If
                lastVec = vec
            Next
        Next
        Return ptlist
    End Function

    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = src
        actualCount = 0

        xyHList.Clear()
        hList = findHorizontalPoints(xyHList)

        allPointsH.Clear()
        For Each h In hList
            For Each pt In h
                allPointsH.Add(pt)
            Next
        Next

        xyVList.Clear()
        vList = findVerticalPoints(xyVList)

        allPointsV.Clear()
        For Each v In vList
            For Each pt In v
                allPointsV.Add(pt)
            Next
        Next

        labels(2) = "Point series found = " + CStr(hList.Count + vList.Count)
    End Sub
End Class







Public Class XO_Line3D_CandidatesFirstLast : Inherits TaskParent
    Public pts As New XO_PointCloud_VerticalHorizontal
    Public pcLines As New List(Of cv.Point3f)
    Public pcLinesMat As cv.Mat
    Public actualCount As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Get a list of points from PointCloud_Basics.  Identify first and last as the line " +
               "in the sequence"
    End Sub
    Private Sub addLines(nextList As List(Of List(Of cv.Point3f)), xyList As List(Of List(Of cv.Point)))
        Dim white32 As New cv.Point3f(0, 1, 1)
        For i = 0 To nextList.Count - 1
            pcLines.Add(white32)
            pcLines.Add(nextList(i)(0))
            pcLines.Add(nextList(i)(nextList(i).Count - 1))
        Next

        For Each ptlist In xyList
            Dim p1 = ptlist(0)
            Dim p2 = ptlist(ptlist.Count - 1)
            DrawLine(dst2, p1, p2, white)
        Next
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pts.Run(src)
        dst2 = pts.dst2

        pcLines.Clear()
        addLines(pts.hList, pts.xyHList)
        addLines(pts.vList, pts.xyVList)

        pcLinesMat = cv.Mat.FromPixelData(pcLines.Count, 1, cv.MatType.CV_32FC3, pcLines.ToArray)
        labels(2) = "Point series found = " + CStr(pts.hList.Count + pts.vList.Count)
    End Sub
End Class







Public Class XO_PointCloud_PCPointsPlane : Inherits TaskParent
    Dim pcBasics As New XO_Line3D_CandidatesFirstLast
    Public pcPoints As New List(Of cv.Point3f)
    Public xyList As New List(Of cv.Point)
    Dim white32 = New cv.Point3f(1, 1, 1)
    Public Sub New()
        setPointCloudGrid()
        desc = "Find planes using a reduced set of 3D points and the intersection of vertical and horizontal lines through those points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pcBasics.Run(src)

        pcPoints.Clear()
        ' points in both the vertical and horizontal lists are likely to designate a plane
        For Each pt In pcBasics.pts.allPointsH
            If pcBasics.pts.allPointsV.Contains(pt) Then
                pcPoints.Add(white32)
                pcPoints.Add(pt)
            End If
        Next

        labels(2) = "Point series found = " + CStr(pcPoints.Count / 2)
    End Sub
End Class






Public Class XO_OpenGL_PClinesFirstLast : Inherits TaskParent
    Dim lines As New XO_Line3D_CandidatesFirstLast
    Public Sub New()
        task.ogl.oglFunction = oCase.pcLines
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        desc = "Draw the 3D lines found from the PCpoints"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        If lines.pcLinesMat.Rows = 0 Then task.ogl.dataInput = New cv.Mat Else task.ogl.dataInput = lines.pcLinesMat
        'task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(New cv.Mat)
        If task.gOptions.getOpenGLCapture() Then dst3 = task.ogl.dst3
        labels(2) = "OpenGL_PClines found " + CStr(lines.pcLinesMat.Rows / 3) + " lines"
    End Sub
End Class







Public Class XO_OpenGL_PCLineCandidates : Inherits TaskParent
    Dim pts As New XO_PointCloud_VerticalHorizontal
    Public Sub New()
        task.ogl.oglFunction = oCase.pcPointsAlone
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        desc = "Display the output of the PointCloud_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pts.Run(src)
        dst2 = pts.dst2

        task.ogl.dataInput = cv.Mat.FromPixelData(pts.allPointsH.Count, 1, cv.MatType.CV_32FC3, pts.allPointsH.ToArray)
        task.ogl.Run(New cv.Mat)
        If task.gOptions.getOpenGLCapture() Then dst3 = task.ogl.dst3
        labels(2) = "Point cloud points found = " + CStr(pts.actualCount / 2)
    End Sub
End Class








Public Class XO_PointCloud_NeighborV : Inherits TaskParent
    Dim options As New Options_Neighbors
    Public Sub New()
        desc = "Show where vertical neighbor depth values are within task.depthDiffMeters"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim tmp32f = New cv.Mat(dst2.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim r1 = New cv.Rect(options.pixels, 0, dst2.Width - options.pixels, dst2.Height)
        Dim r2 = New cv.Rect(0, 0, dst2.Width - options.pixels, dst2.Height)
        cv.Cv2.Absdiff(src(r1), src(r2), tmp32f(r1))
        tmp32f = tmp32f.Threshold(options.threshold, 255, cv.ThresholdTypes.BinaryInv)
        dst2 = tmp32f.ConvertScaleAbs(255)
        dst2.SetTo(0, task.noDepthMask)
        dst2(New cv.Rect(0, dst2.Height - options.pixels, dst2.Width, options.pixels)).SetTo(0)
        labels(2) = "White: z is within " + Format(options.threshold * 1000, fmt0) + " mm's with Y pixel offset " + CStr(options.pixels)
    End Sub
End Class








Public Class XO_PointCloud_Visualize : Inherits TaskParent
    Public Sub New()
        labels = {"", "", "Pointcloud visualized", ""}
        desc = "Display the pointcloud as a BGR image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pcSplit = {task.pcSplit(0).ConvertScaleAbs(255), task.pcSplit(1).ConvertScaleAbs(255), task.pcSplit(2).ConvertScaleAbs(255)}
        cv.Cv2.Merge(pcSplit, dst2)
    End Sub
End Class







Public Class XO_PointCloud_Raw_CPP : Inherits TaskParent
    Dim depthBytes() As Byte
    Public Sub New()
        labels(2) = "Top View"
        labels(3) = "Side View"
        desc = "Project the depth data onto a top view And side view."
        cPtr = SimpleProjectionOpen()
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.firstPass Then ReDim depthBytes(task.pcSplit(2).Total * task.pcSplit(2).ElemSize - 1)

        Marshal.Copy(task.pcSplit(2).Data, depthBytes, 0, depthBytes.Length)
        Dim handleDepth = GCHandle.Alloc(depthBytes, GCHandleType.Pinned)

        Dim imagePtr = SimpleProjectionRun(cPtr, handleDepth.AddrOfPinnedObject, 0, task.MaxZmeters, task.pcSplit(2).Height, task.pcSplit(2).Width)

        dst2 = cv.Mat.FromPixelData(task.pcSplit(2).Rows, task.pcSplit(2).Cols, cv.MatType.CV_8U, imagePtr).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = cv.Mat.FromPixelData(task.pcSplit(2).Rows, task.pcSplit(2).Cols, cv.MatType.CV_8U, SimpleProjectionSide(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        handleDepth.Free()
        labels(2) = "Top View (looking down)"
        labels(3) = "Side View"
    End Sub
    Public Sub Close()
        SimpleProjectionClose(cPtr)
    End Sub
End Class





Public Class XO_PointCloud_Raw : Inherits TaskParent
    Public Sub New()
        labels(2) = "Top View"
        labels(3) = "Side View"
        desc = "Project the depth data onto a top view And side view - Using only VB code (too slow.)"
        cPtr = SimpleProjectionOpen()
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim range As Single = task.MaxZmeters

        ' this VB.Net version is much slower than the optimized C++ version below.
        dst2 = src.EmptyClone.SetTo(white)
        dst3 = dst2.Clone()
        Dim black = New cv.Vec3b(0, 0, 0)
        Parallel.ForEach(task.gridRects,
             Sub(roi)
                 For y = roi.Y To roi.Y + roi.Height - 1
                     For x = roi.X To roi.X + roi.Width - 1
                         Dim m = task.depthMask.Get(Of Byte)(y, x)
                         If m > 0 Then
                             Dim depth = task.pcSplit(2).Get(Of Single)(y, x)
                             Dim dy = CInt(src.Height * depth / range)
                             If dy < src.Height And dy > 0 Then dst2.Set(Of cv.Vec3b)(src.Height - dy, x, black)
                             Dim dx = CInt(src.Width * depth / range)
                             If dx < src.Width And dx > 0 Then dst3.Set(Of cv.Vec3b)(y, dx, black)
                         End If
                     Next
                 Next
             End Sub)
        labels(2) = "Top View (looking down)"
        labels(3) = "Side View"
    End Sub
    Public Sub Close()
        SimpleProjectionClose(cPtr)
    End Sub
End Class







Public Class XO_PointCloud_PCpointsMask : Inherits TaskParent
    Public pcPoints As cv.Mat
    Public actualCount As Integer
    Public Sub New()
        setPointCloudGrid()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Reduce the point cloud to a manageable number points in 3D representing the averages of X, Y, and Z in that roi."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then pcPoints = New cv.Mat(task.cellsPerCol, task.cellsPerRow, cv.MatType.CV_32FC3, cv.Scalar.All(0))

        dst2.SetTo(0)
        actualCount = 0
        Dim lastMeanZ As Single
        For y = 0 To task.cellsPerCol - 1
            For x = 0 To task.cellsPerRow - 1
                Dim roi = task.gridRects(y * task.cellsPerRow + x)
                Dim mean = task.pointCloud(roi).Mean(task.depthMask(roi))
                If Single.IsNaN(mean(0)) Then Continue For
                If Single.IsNaN(mean(1)) Then Continue For
                If Single.IsInfinity(mean(2)) Then Continue For
                Dim depthPresent = task.depthMask(roi).CountNonZero > roi.Width * roi.Height / 2
                If (depthPresent And mean(2) > 0 And Math.Abs(lastMeanZ - mean(2)) < 0.2 And
                    mean(2) < task.MaxZmeters) Or (lastMeanZ = 0 And mean(2) > 0) Then

                    pcPoints.Set(Of cv.Point3f)(y, x, New cv.Point3f(mean(0), mean(1), mean(2)))
                    actualCount += 1
                    DrawCircle(dst2, New cv.Point(roi.X, roi.Y), task.DotSize * Math.Max(mean(2), 1), white)
                End If
                lastMeanZ = mean(2)
            Next
        Next
        labels(2) = "PointCloud Point Points found = " + CStr(actualCount)
    End Sub
End Class







Public Class XO_PointCloud_PCPoints : Inherits TaskParent
    Public pcPoints As New List(Of cv.Point3f)
    Public Sub New()
        setPointCloudGrid()
        desc = "Reduce the point cloud to a manageable number points in 3D using the mean value"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim rw = task.gridRects(0).Width / 2, rh = task.gridRects(0).Height / 2
        Dim red32 = New cv.Point3f(0, 0, 1), blue32 = New cv.Point3f(1, 0, 0), white32 = New cv.Point3f(1, 1, 1)
        Dim red = cv.Scalar.Red, blue = cv.Scalar.Blue

        pcPoints.Clear()
        dst2 = src
        For Each roi In task.gridRects
            Dim pt = New cv.Point(roi.X + rw, roi.Y + rh)
            Dim mean = task.pointCloud(roi).Mean(task.depthMask(roi))

            If mean(2) > 0 Then
                pcPoints.Add(Choose(pt.Y Mod 3 + 1, red32, blue32, white32))
                pcPoints.Add(New cv.Point3f(mean(0), mean(1), mean(2)))
                DrawCircle(dst2, pt, task.DotSize, Choose(CInt(pt.Y) Mod 3 + 1, red, blue, cv.Scalar.White))
            End If
        Next
        labels(2) = "PointCloud Point Points found = " + CStr(pcPoints.Count / 2)
    End Sub
End Class









Public Class OpenGL_PCpoints : Inherits TaskParent
    Dim pts As New XO_PointCloud_PCPoints
    Public Sub New()
        task.ogl.oglFunction = oCase.pcPoints
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        desc = "Display the output of the PointCloud_Points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pts.Run(src)
        dst2 = pts.dst2

        task.ogl.dataInput = cv.Mat.FromPixelData(pts.pcPoints.Count, 1, cv.MatType.CV_32FC3, pts.pcPoints.ToArray)
        task.ogl.Run(New cv.Mat)
        If task.gOptions.getOpenGLCapture() Then dst3 = task.ogl.dst3
        labels(2) = "Point cloud points found = " + CStr(pts.pcPoints.Count / 2)
    End Sub
End Class






Public Class XO_Region_Palette : Inherits TaskParent
    Dim hRects As New XO_Region_RectsH
    Dim vRects As New XO_Region_RectsV
    Dim mats As New Mat_4Click
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Assign an index to each of vertical and horizontal rects in Region_Rects"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hRects.Run(src)

        Dim indexH As Integer
        dst1.SetTo(0)
        For Each r In hRects.hRects
            If r.Y = 0 Then
                indexH += 1
                dst1(r).SetTo(indexH)
            Else
                Dim foundLast As Boolean
                For x = r.X To r.X + r.Width - 1
                    Dim lastIndex = dst1.Get(Of Byte)(r.Y - 1, x)
                    If lastIndex <> 0 Then
                        dst1(r).SetTo(lastIndex)
                        foundLast = True
                        Exit For
                    End If
                Next
                If foundLast = False Then
                    indexH += 1
                    dst1(r).SetTo(indexH)
                End If
            End If
        Next
        mats.mat(0) = ShowPalette(dst1)

        mats.mat(1) = ShowAddweighted(src, mats.mat(0), labels(3))

        vRects.Run(src)
        Dim indexV As Integer
        dst1.SetTo(0)
        For Each r In vRects.vRects
            If r.X = 0 Then
                indexV += 1
                dst1(r).SetTo(indexV)
            Else
                Dim foundLast As Boolean
                For y = r.Y To r.Y + r.Height - 1
                    Dim lastIndex = dst1.Get(Of Byte)(y, r.X - 1)
                    If lastIndex <> 0 Then
                        dst1(r).SetTo(lastIndex)
                        foundLast = True
                        Exit For
                    End If
                Next
                If foundLast = False Then
                    indexV += 1
                    dst1(r).SetTo(indexV)
                End If
            End If
        Next
        mats.mat(2) = ShowPalette(dst1)

        mats.mat(3) = ShowAddweighted(src, mats.mat(2), labels(3))
        If task.heartBeat Then labels(2) = CStr(indexV + indexH) + " regions were found that were connected in depth."

        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class






Public Class XO_Sort_FeatureLess : Inherits TaskParent
    Public connect As New XO_Region_Palette
    Public sort As New Sort_Basics
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        task.gOptions.setHistogramBins(256)
        task.gOptions.GridSlider.Value = 8
        desc = "Sort all the featureless grayscale pixels."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)
        dst2 = connect.dst3
        labels(2) = connect.labels(2)
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1.SetTo(0, Not connect.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary))

        sort.Run(dst1)

        plot.Run(sort.dst2)
        dst3 = plot.dst2
    End Sub
End Class







Public Class XO_Region_RectsH : Inherits TaskParent
    Public hRects As New List(Of cv.Rect)
    Dim connect As New Region_Core
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Connect bricks with similar depth - horizontally scanning."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)

        dst2.SetTo(0)
        dst3.SetTo(0)
        hRects.Clear()
        Dim index As Integer
        For Each tup In connect.hTuples
            If tup.Item1 = tup.Item2 Then Continue For
            Dim brick1 = task.brickList(tup.Item1)
            Dim brick2 = task.brickList(tup.Item2)

            Dim w = brick2.rect.BottomRight.X - brick1.rect.X
            Dim h = brick1.rect.Height

            Dim r = New cv.Rect(brick1.rect.X + 1, brick1.rect.Y, w - 1, h)

            hRects.Add(r)
            dst2(r).SetTo(255)

            index += 1
            dst3(r).SetTo(task.scalarColors(index Mod 256))
        Next
    End Sub
End Class






Public Class XO_Region_RectsV : Inherits TaskParent
    Public vRects As New List(Of cv.Rect)
    Dim connect As New Region_Core
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Connect bricks with similar depth - vertically scanning."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)

        dst2.SetTo(0)
        dst3.SetTo(0)
        vRects.Clear()
        Dim index As Integer
        For Each tup In connect.vTuples
            If tup.Item1 = tup.Item2 Then Continue For
            Dim brick1 = task.brickList(tup.Item1)
            Dim brick2 = task.brickList(tup.Item2)

            Dim w = brick1.rect.Width
            Dim h = brick2.rect.BottomRight.Y - brick1.rect.Y

            Dim r = New cv.Rect(brick1.rect.X, brick1.rect.Y + 1, w, h - 1)
            vRects.Add(r)
            dst2(r).SetTo(255)

            index += 1
            dst3(r).SetTo(task.scalarColors(index Mod 256))
        Next
    End Sub
End Class






Public Class XO_Region_Rects : Inherits TaskParent
    Dim hConn As New XO_Region_RectsH
    Dim vConn As New XO_Region_RectsV
    Public Sub New()
        desc = "Isolate the connected depth bricks both vertically and horizontally."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hConn.Run(src)
        vConn.Run(src)

        dst2 = (Not vConn.dst2).ToMat Or (Not hConn.dst2).ToMat

        dst3 = src
        dst3.SetTo(0, dst2)
    End Sub
End Class






Public Class XO_Region_RedColor : Inherits TaskParent
    Dim connect As New Region_Contours
    Public Sub New()
        desc = "Color each redCell with the color of the nearest brick region."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)

        dst3 = runRedC(src, labels(3))
        For Each rc In task.rcList
            Dim index = connect.dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            dst2(rc.rect).SetTo(task.scalarColors(index), rc.mask)
        Next
    End Sub
End Class





Public Class XO_Region_Gaps : Inherits TaskParent
    Dim connect As New Region_Core
    Public Sub New()
        labels(2) = "bricks with single cells removed for both vertical and horizontal connected cells."
        labels(3) = "Vertical cells with single cells removed."
        desc = "Use the horizontal/vertical connected cells to find gaps in depth and the like featureless regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)
        dst2 = connect.dst2
        dst3 = connect.dst3

        For Each tup In connect.hTuples
            If tup.Item2 - tup.Item1 = 0 Then
                Dim brick = task.brickList(tup.Item1)
                dst2(brick.rect).SetTo(0)
            End If
        Next

        For Each tup In connect.vTuples
            Dim brick1 = task.brickList(tup.Item1)
            Dim brick2 = task.brickList(tup.Item2)
            If brick2.rect.Y - brick1.rect.Y = 0 Then
                dst2(brick1.rect).SetTo(0)
                dst3(brick1.rect).SetTo(0)
            End If
        Next
    End Sub
End Class






Public Class XO_Brick_FeatureGaps : Inherits TaskParent
    Dim feat As New Brick_Features
    Dim gaps As New XO_Region_Gaps
    Public Sub New()
        labels(2) = "The output of Brick_Gaps overlaid with the output of the Brick_Features"
        desc = "Overlay the features on the image of the gaps"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(src)
        gaps.Run(src)
        dst2 = ShowAddweighted(feat.dst2, gaps.dst2, labels(3))
    End Sub
End Class




Public Class XO_FCSLine_Basics : Inherits TaskParent
    Dim delaunay As New Delaunay_Basics
    Public Sub New()
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Build a feature coordinate system (FCS) based on lines, not features."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lastMap = task.fpMap.Clone
        Dim lastCount = task.lineRGB.lpList.Count

        dst2 = task.lineRGB.dst2

        delaunay.inputPoints.Clear()

        For Each lp In task.lineRGB.lpList
            Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
            delaunay.inputPoints.Add(center)
        Next

        delaunay.Run(src)

        task.fpMap.SetTo(0)
        dst1.SetTo(0)
        For i = 0 To delaunay.facetList.Count - 1
            Dim lp = task.lineRGB.lpList(i)
            Dim facets = delaunay.facetList(i)

            DrawContour(dst1, facets, 255, task.lineWidth)
            DrawContour(task.fpMap, facets, lp.index)
            Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
            Dim brick = task.brickList(task.brickMap.Get(Of Single)(center.Y, center.X))
            task.lineRGB.lpList(i) = lp
        Next

        Dim index = task.fpMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
        task.lpD = task.lineRGB.lpList(index)
        Dim facetsD = delaunay.facetList(task.lpD.index)
        DrawContour(dst2, facetsD, white, task.lineWidth)

        labels(2) = task.lineRGB.labels(2)
        labels(3) = delaunay.labels(2)
    End Sub
End Class







Public Class XO_FCSLine_Vertical : Inherits TaskParent
    Dim verts As New LineRGB_VerticalTrig
    Dim minRect As New LineRect_Basics
    Dim options As New Options_FCSLine
    Public Sub New()
        desc = "Find all verticle lines and combine them if they are 'close'."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        verts.Run(src)

        dst2.SetTo(0)
        dst3.SetTo(0)
        For i = 0 To verts.vertList.Count - 1
            Dim lp1 = verts.vertList(i)
            For j = i + 1 To verts.vertList.Count - 1
                Dim lp2 = verts.vertList(j)
                Dim center = New cv.Point(CInt((lp1.p1.X + lp1.p2.X) / 2), CInt((lp1.p1.Y + lp1.p2.Y) / 2))
                Dim lpPerp = lp1.perpendicularPoints(center, task.cellSize)
                Dim intersectionPoint = IntersectTest(lp1, lpPerp)
                Dim distance = intersectionPoint.DistanceTo(center)
                If distance <= options.proximity Then
                    minRect.lpInput1 = lp1
                    minRect.lpInput2 = lp2
                    Dim rotatedRect1 = cv.Cv2.MinAreaRect({lp1.p1, lp1.p2})
                    Dim rotatedRect2 = cv.Cv2.MinAreaRect({lp2.p1, lp2.p2})
                    minRect.Run(src)
                    dst2.Line(lp1.p1, lp1.p2, task.highlight, task.lineWidth, task.lineType)
                    dst2.Line(lp2.p1, lp2.p2, task.highlight, task.lineWidth, task.lineType)
                    DrawRotatedOutline(minRect.rotatedRect, dst3, cv.Scalar.Yellow)
                End If
            Next
        Next
    End Sub
End Class








Public Class XO_FeatureLine_VerticalVerify : Inherits TaskParent
    Dim linesVH As New FeatureLine_VH
    Public verify As New IMU_VerticalVerify
    Public Sub New()
        desc = "Select a line or group of lines and track the result"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        linesVH.Run(src)

        verify.brickCells = New List(Of gravityLine)(linesVH.brickCells)
        verify.Run(src)
        dst2 = verify.dst2
    End Sub
End Class







Public Class XO_FeatureLine_Finder3D : Inherits TaskParent
    Public lines2D As New List(Of cv.Point2f)
    Public lines3D As New List(Of cv.Point3f)
    Public sorted2DV As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public sortedVerticals As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public sortedHorizontals As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Dim options As New Options_LineFinder()
    Public Sub New()
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        desc = "Find all the lines in the image and determine which are vertical and horizontal"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst3 = src.Clone

        lines2D.Clear()
        lines3D.Clear()
        sorted2DV.Clear()
        sortedVerticals.Clear()
        sortedHorizontals.Clear()

        dst2 = task.lineRGB.dst2

        Dim raw2D As New List(Of lpData)
        Dim raw3D As New List(Of cv.Point3f)
        For Each lp In task.lineRGB.lpList
            Dim pt1 As cv.Point3f, pt2 As cv.Point3f
            For j = 0 To 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                Dim rect = ValidateRect(New cv.Rect(pt.x - options.kSize, pt.y - options.kSize, options.kernelSize, options.kernelSize))
                Dim val = task.pointCloud(rect).Mean(task.depthMask(rect))
                If j = 0 Then pt1 = New cv.Point3f(val(0), val(1), val(2)) Else pt2 = New cv.Point3f(val(0), val(1), val(2))
            Next

            If pt1.Z > 0 And pt2.Z > 0 And pt1.Z < 4 And pt2.Z < 4 Then ' points more than X meters away are not accurate...
                raw2D.Add(lp)
                raw3D.Add(pt1)
                raw3D.Add(pt2)
            End If
        Next

        If raw3D.Count = 0 Then
            SetTrueText("No vertical or horizontal lines were found")
        Else
            Dim matLines3D As cv.Mat = (cv.Mat.FromPixelData(raw3D.Count, 3, cv.MatType.CV_32F, raw3D.ToArray)) * task.gMatrix

            For i = 0 To raw2D.Count - 2 Step 2
                Dim pt1 = matLines3D.Get(Of cv.Point3f)(i, 0)
                Dim pt2 = matLines3D.Get(Of cv.Point3f)(i + 1, 0)
                Dim len3D = distance3D(pt1, pt2)
                Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
                If Math.Abs(arcY - 90) < options.tolerance Then
                    DrawLine(dst3, raw2D(i).p1, raw2D(i).p2, cv.Scalar.Blue)
                    sortedVerticals.Add(len3D, lines3D.Count)
                    sorted2DV.Add(raw2D(i).p1.DistanceTo(raw2D(i).p2), lines2D.Count)
                    If pt1.Y > pt2.Y Then
                        lines3D.Add(pt1)
                        lines3D.Add(pt2)
                        lines2D.Add(raw2D(i).p1)
                        lines2D.Add(raw2D(i).p2)
                    Else
                        lines3D.Add(pt2)
                        lines3D.Add(pt1)
                        lines2D.Add(raw2D(i).p2)
                        lines2D.Add(raw2D(i).p1)
                    End If
                End If
                If Math.Abs(arcY) < options.tolerance Then
                    DrawLine(dst3, raw2D(i).p1, raw2D(i).p2, cv.Scalar.Yellow)
                    sortedHorizontals.Add(len3D, lines3D.Count)
                    If pt1.X < pt2.X Then
                        lines3D.Add(pt1)
                        lines3D.Add(pt2)
                        lines2D.Add(raw2D(i).p1)
                        lines2D.Add(raw2D(i).p2)
                    Else
                        lines3D.Add(pt2)
                        lines3D.Add(pt1)
                        lines2D.Add(raw2D(i).p2)
                        lines2D.Add(raw2D(i).p1)
                    End If
                End If
            Next
        End If
        labels(2) = "Starting with " + Format(task.lineRGB.lpList.Count, "000") + " lines, there are " +
                                       Format(lines3D.Count / 2, "000") + " with depth data."
        labels(3) = "There were " + CStr(sortedVerticals.Count) + " vertical lines (blue) and " + CStr(sortedHorizontals.Count) + " horizontal lines (yellow)"
    End Sub
End Class






Public Class XO_Structured_FeatureLines : Inherits TaskParent
    Dim struct As New Structured_MultiSlice
    Public lines As New XO_FeatureLine_Finder3D
    Public Sub New()
        desc = "Find the lines in the Structured_MultiSlice algorithm output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        struct.Run(src)
        dst2 = struct.dst2

        lines.Run(struct.dst2)
        dst3 = src.Clone
        For i = 0 To lines.lines2D.Count - 1 Step 2
            Dim p1 = lines.lines2D(i), p2 = lines.lines2D(i + 1)
            dst3.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
    End Sub
End Class








Public Class XO_FeatureLine_Tutorial2 : Inherits TaskParent
    Dim options As New Options_LineFinder()
    Public Sub New()
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        desc = "Find all the lines in the image and determine which are vertical and horizontal"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = task.lineRGB.dst2

        Dim raw3D As New List(Of cv.Point3f)
        For Each lp In task.lineRGB.lpList
            Dim pt1 As cv.Point3f, pt2 As cv.Point3f
            For j = 0 To 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                Dim rect = ValidateRect(New cv.Rect(pt.x - options.kSize, pt.y - options.kSize, options.kernelSize, options.kernelSize))
                Dim val = task.pointCloud(rect).Mean(task.depthMask(rect))
                If j = 0 Then pt1 = New cv.Point3f(val(0), val(1), val(2)) Else pt2 = New cv.Point3f(val(0), val(1), val(2))
            Next
            If pt1.Z > 0 And pt2.Z > 0 Then
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p1.Y, lp.p1.X))
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p2.Y, lp.p2.X))
            End If
        Next

        If task.heartBeat Then labels(2) = "Starting with " + Format(task.lineRGB.lpList.Count, "000") +
                               " lines, there are " + Format(raw3D.Count, "000") + " with depth data."
        If raw3D.Count = 0 Then
            SetTrueText("No vertical or horizontal lines were found")
        Else
            task.gMatrix = task.gmat.gMatrix
            Dim matLines3D = cv.Mat.FromPixelData(raw3D.Count, 3, cv.MatType.CV_32F, raw3D.ToArray) * task.gmat.gMatrix
        End If
    End Sub
End Class









Public Class XO_FeatureLine_LongestVerticalKNN : Inherits TaskParent
    Dim gLines As New LineRGB_GCloud
    Dim longest As New XO_FeatureLine_Longest
    Public Sub New()
        labels(3) = "All vertical lines.  The numbers: index and Arc-Y for the longest X vertical lines."
        desc = "Find all the vertical lines and then track the longest one with a lightweight KNN."
    End Sub
    Private Function testLastPair(lastPair As lpData, brick As gravityLine) As Boolean
        Dim distance1 = lastPair.p1.DistanceTo(lastPair.p2)
        Dim p1 = brick.tc1.center
        Dim p2 = brick.tc2.center
        If distance1 < 0.75 * p1.DistanceTo(p2) Then Return True ' it the longest vertical * 0.75 > current lastPair, then use the longest vertical...
        Return False
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        gLines.Run(src)
        If gLines.sortedVerticals.Count = 0 Then
            SetTrueText("No vertical lines were present", 3)
            Exit Sub
        End If

        dst3 = src.Clone
        Dim index As Integer

        If testLastPair(longest.knn.lastPair, gLines.sortedVerticals.ElementAt(0).Value) Then longest.knn.lastPair = New lpData
        For Each brick In gLines.sortedVerticals.Values
            If index >= 10 Then Exit For

            Dim p1 = brick.tc1.center
            Dim p2 = brick.tc2.center
            If longest.knn.lastPair.compare(New lpData) Then longest.knn.lastPair = New lpData(p1, p2)
            Dim pt = New cv.Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2)
            SetTrueText(CStr(index) + vbCrLf + Format(brick.arcY, fmt1), pt, 3)
            index += 1

            DrawLine(dst3, p1, p2, task.highlight)
            longest.knn.trainInput.Add(p1)
            longest.knn.trainInput.Add(p2)
        Next

        longest.Run(src)
        dst2 = longest.dst2
    End Sub
End Class








Public Class XO_FeatureLine_LongestV_Tutorial1 : Inherits TaskParent
    Dim lines As New XO_FeatureLine_Finder3D
    Public Sub New()
        desc = "Use FeatureLine_Finder to find all the vertical lines and show the longest."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)

        If lines.sortedVerticals.Count = 0 Then
            SetTrueText("No vertical lines were found", 3)
            Exit Sub
        End If

        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim p1 = lines.lines2D(index)
        Dim p2 = lines.lines2D(index + 1)
        DrawLine(dst2, p1, p2, task.highlight)
        dst3.SetTo(0)
        DrawLine(dst3, p1, p2, task.highlight)
    End Sub
End Class






Public Class XO_FeatureLine_LongestV_Tutorial2 : Inherits TaskParent
    Dim lines As New XO_FeatureLine_Finder3D
    Dim knn As New KNN_N4Basics
    Public pt1 As New cv.Point3f
    Public pt2 As New cv.Point3f
    Dim lengthReject As Integer
    Public Sub New()
        desc = "Use FeatureLine_Finder to find all the vertical lines.  Use KNN_Basics4D to track each line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)
        dst1 = lines.dst3

        If lines.sortedVerticals.Count = 0 Then
            SetTrueText("No vertical lines were found", 3)
            Exit Sub
        End If

        Dim match3D As New List(Of cv.Point3f)
        knn.trainInput.Clear()
        For i = 0 To lines.sortedVerticals.Count - 1
            Dim sIndex = lines.sortedVerticals.ElementAt(i).Value
            Dim x1 = lines.lines2D(sIndex)
            Dim x2 = lines.lines2D(sIndex + 1)
            Dim vec = If(x1.Y < x2.Y, New cv.Vec4f(x1.X, x1.Y, x2.X, x2.Y), New cv.Vec4f(x2.X, x2.Y, x1.X, x1.Y))
            If knn.queries.Count = 0 Then knn.queries.Add(vec)
            knn.trainInput.Add(vec)
            match3D.Add(lines.lines3D(sIndex))
            match3D.Add(lines.lines3D(sIndex + 1))
        Next

        Dim saveVec = knn.queries(0)
        knn.Run(src)

        Dim index = knn.result(0, 0)
        Dim p1 = New cv.Point2f(knn.trainInput(index)(0), knn.trainInput(index)(1))
        Dim p2 = New cv.Point2f(knn.trainInput(index)(2), knn.trainInput(index)(3))
        pt1 = match3D(index * 2)
        pt2 = match3D(index * 2 + 1)
        DrawLine(dst2, p1, p2, task.highlight)
        dst3.SetTo(0)
        DrawLine(dst3, p1, p2, task.highlight)

        Static lastLength = lines.sorted2DV.ElementAt(0).Key
        Dim bestLength = lines.sorted2DV.ElementAt(0).Key
        knn.queries.Clear()
        If lastLength > 0.5 * bestLength Then
            knn.queries.Add(New cv.Vec4f(p1.X, p1.Y, p2.X, p2.Y))
            lastLength = p1.DistanceTo(p2)
        Else
            lengthReject += 1
            lastLength = bestLength
        End If
        labels(3) = "Length rejects = " + Format(lengthReject / (task.frameCount + 1), "0%")
    End Sub
End Class







Public Class XO_FeatureLine_VerticalLongLine : Inherits TaskParent
    Dim lines As New XO_FeatureLine_Finder3D
    Public Sub New()
        desc = "Use FeatureLine_Finder data to identify the longest lines and show its angle."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            dst2 = src.Clone
            lines.Run(src)

            If lines.sortedVerticals.Count = 0 Then
                SetTrueText("No vertical lines were found", 3)
                Exit Sub
            End If
        End If

        If lines.sortedVerticals.Count = 0 Then Exit Sub ' nothing found...
        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim p1 = lines.lines2D(index)
        Dim p2 = lines.lines2D(index + 1)
        DrawLine(dst2, p1, p2, task.highlight)
        dst3.SetTo(0)
        DrawLine(dst3, p1, p2, task.highlight)
        Dim pt1 = lines.lines3D(index)
        Dim pt2 = lines.lines3D(index + 1)
        Dim len3D = distance3D(pt1, pt2)
        Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
        SetTrueText(Format(arcY, fmt3) + vbCrLf + Format(len3D, fmt3) + "m len" + vbCrLf + Format(pt1.Z, fmt1) + "m dist", p1)
        SetTrueText(Format(arcY, fmt3) + vbCrLf + Format(len3D, fmt3) + "m len" + vbCrLf + Format(pt1.Z, fmt1) + "m distant", p1, 3)
    End Sub
End Class









Public Class XO_KNN_ClosestVertical : Inherits TaskParent
    Public lines As New XO_FeatureLine_Finder3D
    Public knn As New KNN_ClosestLine
    Public pt1 As New cv.Point3f
    Public pt2 As New cv.Point3f
    Public Sub New()
        labels = {"", "", "Highlight the tracked line", "Candidate vertical lines are in Blue"}
        desc = "Test the code find the longest line and track it using a minimized KNN test."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        lines.Run(src)
        If lines.sortedVerticals.Count = 0 Then
            SetTrueText("No vertical lines were found.")
            Exit Sub
        End If

        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim lastDistance = knn.lastP1.DistanceTo(knn.lastP2)
        Dim bestDistance = lines.lines2D(index).DistanceTo(lines.lines2D(index + 1))
        If knn.lastP1 = New cv.Point2f Or lastDistance < 0.75 * bestDistance Then
            knn.lastP1 = lines.lines2D(index)
            knn.lastP2 = lines.lines2D(index + 1)
        End If

        knn.trainInput.Clear()
        For i = 0 To lines.sortedVerticals.Count - 1
            index = lines.sortedVerticals.ElementAt(i).Value
            knn.trainInput.Add(lines.lines2D(index))
            knn.trainInput.Add(lines.lines2D(index + 1))
        Next

        knn.Run(src)

        pt1 = lines.lines3D(knn.lastIndex)
        pt2 = lines.lines3D(knn.lastIndex + 1)

        dst3 = lines.dst3
        DrawLine(dst2, knn.lastP1, knn.lastP2, task.highlight)
    End Sub
End Class











Public Class XO_Line_VerticalHorizontalCells : Inherits TaskParent
    Dim lines As New XO_FeatureLine_Finder3D
    Dim hulls As New RedColor_Hulls
    Public Sub New()
        labels(2) = "RedColor_Hulls output with lines highlighted"
        desc = "Identify the lines created by the RedCloud Cells and separate vertical from horizontal"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2

        lines.Run(dst2.Clone)
        dst3 = src
        For i = 0 To lines.sortedHorizontals.Count - 1
            Dim index = lines.sortedHorizontals.ElementAt(i).Value
            Dim p1 = lines.lines2D(index), p2 = lines.lines2D(index + 1)
            DrawLine(dst3, p1, p2, cv.Scalar.Yellow)
        Next
        For i = 0 To lines.sortedVerticals.Count - 1
            Dim index = lines.sortedVerticals.ElementAt(i).Value
            Dim p1 = lines.lines2D(index), p2 = lines.lines2D(index + 1)
            DrawLine(dst3, p1, p2, cv.Scalar.Blue)
        Next
        labels(3) = CStr(lines.sortedVerticals.Count) + " vertical and " + CStr(lines.sortedHorizontals.Count) + " horizontal lines identified in the RedCloud output"
    End Sub
End Class







Public Class XO_Line_VerticalHorizontal1 : Inherits TaskParent
    Dim nearest As New XO_Line_Nearest
    Public Sub New()
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        task.gOptions.LineWidth.Value = 2
        desc = "Find all the lines in the color image that are parallel to gravity or the horizon using distance to the line instead of slope."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pixelDiff = task.gOptions.pixelDiffThreshold

        dst2 = src.Clone
        If standaloneTest() Then dst3 = task.lineRGB.dst2

        nearest.lp = task.gravityVec
        DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, white)
        For Each lp In task.lineRGB.lpList
            Dim ptInter = IntersectTest(lp.p1, lp.p2, task.gravityVec.p1, task.gravityVec.p2)
            If ptInter.X >= 0 And ptInter.X < dst2.Width And ptInter.Y >= 0 And ptInter.Y < dst2.Height Then
                Continue For
            End If

            nearest.pt = lp.p1
            nearest.Run(Nothing)
            Dim d1 = nearest.distance

            nearest.pt = lp.p2
            nearest.Run(Nothing)
            Dim d2 = nearest.distance

            If Math.Abs(d1 - d2) <= pixelDiff Then
                DrawLine(dst2, lp.p1, lp.p2, task.highlight)
            End If
        Next

        DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, white)
        nearest.lp = task.horizonVec
        For Each lp In task.lineRGB.lpList
            Dim ptInter = IntersectTest(lp.p1, lp.p2, task.horizonVec.p1, task.horizonVec.p2)
            If ptInter.X >= 0 And ptInter.X < dst2.Width And ptInter.Y >= 0 And ptInter.Y < dst2.Height Then Continue For

            nearest.pt = lp.p1
            nearest.Run(Nothing)
            Dim d1 = nearest.distance

            nearest.pt = lp.p2
            nearest.Run(Nothing)
            Dim d2 = nearest.distance

            If Math.Abs(d1 - d2) <= pixelDiff Then
                DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Red)
            End If
        Next
        labels(2) = "Slope for gravity is " + Format(task.gravityVec.m, fmt1) + ".  Slope for horizon is " + Format(task.horizonVec.m, fmt1)
    End Sub
End Class









Public Class XO_OpenGL_VerticalOrHorizontal : Inherits TaskParent
    Dim vLine As New XO_FeatureLine_Finder3D
    Public Sub New()
        If OptionParent.FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Show Vertical Lines")
            radio.addRadio("Show Horizontal Lines")
            radio.check(0).Checked = True
        End If

        task.ogl.oglFunction = oCase.drawLineAndCloud
        desc = "Visualize all the vertical lines found in FeatureLine_Finder"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static verticalRadio = OptionParent.findRadio("Show Vertical Lines")
        Dim showVerticals = verticalRadio.checked

        vLine.Run(src)
        dst2 = vLine.dst3

        task.ogl.pointCloudInput = task.pointCloud

        'Dim lines3D As New List(Of cv.Point3f)
        'Dim count = If(showVerticals, vLine.sortedVerticals.Count, vLine.sortedHorizontals.Count)
        'For i = 0 To count - 1
        '    Dim index = If(showVerticals, vLine.sortedVerticals.ElementAt(i).Value, vLine.sortedHorizontals.ElementAt(i).Value)
        '    lines3D.Add(vLine.lines3D(index))
        '    lines3D.Add(vLine.lines3D(index + 1))
        'Next
        'task.ogl.dataInput = cv.Mat.FromPixelData(lines3D.Count, 1, cv.MatType.CV_32FC3, lines3D.ToArray)
        'task.ogl.Run(task.color)
        'If task.gOptions.getOpenGLCapture() Then dst3 = task.ogl.dst3
    End Sub
End Class






Public Class XO_FeatureLine_BasicsRaw : Inherits TaskParent
    Dim lines As New LineRGB_RawSubset
    Dim lineDisp As New XO_Line_DisplayInfoOld
    Dim options As New Options_Features
    Dim match As New Match_tCell
    Public tcells As List(Of tCell)
    Public Sub New()
        Dim tc As tCell
        tcells = New List(Of tCell)({tc, tc})
        labels = {"", "", "Longest line present.", ""}
        desc = "Find and track a line using the end points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        Dim distanceThreshold = 50 ' pixels - arbitrary but realistically needs some value
        Dim linePercentThreshold = 0.7 ' if less than 70% of the pixels in the line are edges, then find a better line.  Again, arbitrary but realistic.

        Dim correlationTest = tcells(0).correlation <= task.fCorrThreshold Or tcells(1).correlation <= task.fCorrThreshold
        lineDisp.distance = tcells(0).center.DistanceTo(tcells(1).center)
        If task.optionsChanged Or correlationTest Or lineDisp.maskCount / lineDisp.distance < linePercentThreshold Or lineDisp.distance < distanceThreshold Then
            Dim templatePad = options.templatePad
            lines.subsetRect = New cv.Rect(templatePad * 3, templatePad * 3, src.Width - templatePad * 6, src.Height - templatePad * 6)
            lines.Run(src.Clone)

            If lines.lpList.Count = 0 Then
                SetTrueText("No lines found.", 3)
                Exit Sub
            End If
            Dim lp = lines.lpList(0)

            tcells(0) = match.createCell(src, 0, lp.p1)
            tcells(1) = match.createCell(src, 0, lp.p2)
        End If

        dst2 = src.Clone
        For i = 0 To tcells.Count - 1
            match.tCells(0) = tcells(i)
            match.Run(src)
            tcells(i) = match.tCells(0)
            SetTrueText(tcells(i).strOut, New cv.Point(tcells(i).rect.X, tcells(i).rect.Y))
            SetTrueText(tcells(i).strOut, New cv.Point(tcells(i).rect.X, tcells(i).rect.Y), 3)
        Next

        lineDisp.tcells = New List(Of tCell)(tcells)
        lineDisp.Run(src)
        dst2 = lineDisp.dst2
        SetTrueText(lineDisp.strOut, New cv.Point(10, 40), 3)
    End Sub
End Class







Public Class XO_FeatureLine_DetailsAll : Inherits TaskParent
    Dim lines As New XO_FeatureLine_Finder3D
    Dim flow As New Font_FlowText
    Dim arcList As New List(Of Single)
    Dim arcLongAverage As New List(Of Single)
    Dim firstAverage As New List(Of Single)
    Dim firstBest As Integer
    Dim title = "ID" + vbTab + "length" + vbTab + "distance "
    Public Sub New()
        flow.parentData = Me
        flow.dst = 3
        desc = "Use FeatureLine_Finder data to collect vertical lines and measure accuracy of each."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            dst2 = src.Clone
            lines.Run(src)

            If lines.sortedVerticals.Count = 0 Then
                SetTrueText("No vertical lines were found", 3)
                Exit Sub
            End If

            dst3.SetTo(0)
            arcList.Clear()
            flow.nextMsg = title
            For i = 0 To Math.Min(10, lines.sortedVerticals.Count) - 1
                Dim index = lines.sortedVerticals.ElementAt(i).Value
                Dim p1 = lines.lines2D(index)
                Dim p2 = lines.lines2D(index + 1)
                DrawLine(dst2, p1, p2, task.highlight)
                SetTrueText(CStr(i), If(i Mod 2, p1, p2), 2)
                DrawLine(dst3, p1, p2, task.highlight)

                Dim pt1 = lines.lines3D(index)
                Dim pt2 = lines.lines3D(index + 1)
                Dim len3D = distance3D(pt1, pt2)
                If len3D > 0 Then
                    Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
                    arcList.Add(arcY)
                    flow.nextMsg += Format(arcY, fmt3) + " degrees" + vbTab + Format(len3D, fmt3) + "m " + vbTab + Format(pt1.Z, fmt1) + "m"
                End If
            Next
            If flow.nextMsg = title Then flow.nextMsg = "No feature line found..."
        End If
        flow.Run(src)
        If arcList.Count = 0 Then Exit Sub

        Dim mostAccurate = arcList(0)
        firstAverage.Add(mostAccurate)
        For Each arc In arcList
            If arc > mostAccurate Then
                mostAccurate = arc
                Exit For
            End If
        Next
        If mostAccurate = arcList(0) Then firstBest += 1

        Dim avg = arcList.Average()
        arcLongAverage.Add(avg)
        labels(3) = "arcY avg = " + Format(avg, fmt1) + ", long term average = " + Format(arcLongAverage.Average, fmt1) +
                    ", first was best " + Format(firstBest / task.frameCount, "0%") + " of the time, Avg of longest line " + Format(firstAverage.Average, fmt1)
        If arcLongAverage.Count > 1000 Then
            arcLongAverage.RemoveAt(0)
            firstAverage.RemoveAt(0)
        End If
    End Sub
End Class







Public Class XO_FeatureLine_LongestKNN : Inherits TaskParent
    Dim glines As New LineRGB_GCloud
    Public knn As New KNN_ClosestTracker
    Public options As New Options_Features
    Public gline As gravityLine
    Public match As New Match_Basics
    Dim p1 As cv.Point, p2 As cv.Point
    Public Sub New()
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        dst2 = src

        knn.Run(src.Clone)
        p1 = knn.lastPair.p1
        p2 = knn.lastPair.p2
        gline = glines.updateGLine(src, gline, p1, p2)

        Dim rect = ValidateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
        match.template = src(rect)
        match.Run(src)
        If match.correlation >= task.fCorrThreshold Then
            dst3 = match.dst0.Resize(dst3.Size)
            DrawLine(dst2, p1, p2, task.highlight)
            DrawCircle(dst2, p1, task.DotSize, task.highlight)
            DrawCircle(dst2, p2, task.DotSize, task.highlight)
            rect = ValidateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
            match.template = src(rect).Clone
        Else
            task.highlight = If(task.highlight = cv.Scalar.Yellow, cv.Scalar.Blue, cv.Scalar.Yellow)
            knn.lastPair = New lpData(New cv.Point2f, New cv.Point2f)
        End If
        labels(2) = "Longest line end points had correlation of " + Format(match.correlation, fmt3) + " with the original longest line."
    End Sub
End Class






Public Class XO_FeatureLine_Longest : Inherits TaskParent
    Dim glines As New LineRGB_GCloud
    Public knn As New KNN_ClosestTracker
    Public options As New Options_Features
    Public gline As gravityLine
    Public match1 As New Match_Basics
    Public match2 As New Match_Basics
    Public Sub New()
        labels(2) = "Longest line end points are highlighted "
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        dst2 = src.Clone
        Dim templatePad = match1.options.templatePad
        Dim templateSize = match1.options.templateSize

        Static p1 As cv.Point, p2 As cv.Point
        If task.heartBeat Or match1.correlation < task.fCorrThreshold And match2.correlation < task.fCorrThreshold Then
            knn.Run(src.Clone)

            p1 = knn.lastPair.p1
            Dim r1 = ValidateRect(New cv.Rect(p1.X - templatePad, p1.Y - templatePad, templateSize, templateSize))
            match1.template = src(r1).Clone

            p2 = knn.lastPair.p2
            Dim r2 = ValidateRect(New cv.Rect(p2.X - templatePad, p2.Y - templatePad, templateSize, templateSize))
            match2.template = src(r2).Clone
        End If

        match1.Run(src)
        p1 = match1.matchCenter

        match2.Run(src)
        p2 = match2.matchCenter

        gline = glines.updateGLine(src, gline, p1, p2)
        DrawLine(dst2, p1, p2, task.highlight)
        DrawCircle(dst2, p1, task.DotSize, task.highlight)
        DrawCircle(dst2, p2, task.DotSize, task.highlight)
        SetTrueText(Format(match1.correlation, fmt3), p1)
        SetTrueText(Format(match2.correlation, fmt3), p2)
    End Sub
End Class







Public Class XO_Swarm_Flood2 : Inherits TaskParent
    Public lines As New XO_Line_KNN
    Public flood As New Flood_BasicsMask
    Dim color8U As New Color8U_Basics
    Public Sub New()
        desc = "Floodfill the color image using the swarm outline as a mask"
    End Sub
    Public Function runRedCloud(src As cv.Mat) As cv.Mat
        lines.Run(src)
        color8U.Run(src)

        flood.inputRemoved = lines.dst3
        flood.Run(color8U.dst2)
        Return flood.dst2
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If Not task.heartBeat Then Exit Sub

        dst2 = runRedCloud(src).Clone()
        dst3 = lines.dst3.Clone

        task.setSelectedCell()
        labels(1) = "Color8U_Basics input = " + task.redOptions.ColorSource.Text
        labels(2) = flood.cellGen.labels(2)
        labels(3) = lines.labels(2)
    End Sub
End Class







Public Class XO_Swarm_Flood3 : Inherits TaskParent
    Dim swarm As New XO_Swarm_Flood2
    Public Sub New()
        desc = "Create RedCloud cells every heartbeat and compare the results against RedCloud cells created with the current frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        swarm.Run(src)
        dst2 = swarm.dst2
        labels(2) = swarm.labels(2)

        dst3 = swarm.runRedCloud(src)
        labels(3) = swarm.labels(2)
    End Sub
End Class








Public Class XO_BrickPoint_FeatureLessOld2 : Inherits TaskParent
    Public edges As New EdgeLine_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "CV_8U Mask for the featureless regions"
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        desc = "Isolate the featureless regions using the sobel intensity."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.grayStable.Clone)

        dst0.SetTo(0)
        Dim brickPrev = task.brickList(0)
        Dim fLessCount As Integer = 1
        For Each brick In task.brickList
            If brick.rect.X = 0 Or brick.rect.Y = 0 Then Continue For

            Dim brickAbove = task.brickList(brick.index - task.cellsPerRow)
            Dim val = brickAbove.contourFull
            If val = 0 Then val = dst0.Get(Of Byte)(brickPrev.rect.Y, brickPrev.rect.X)
            Dim count = edges.dst2(brick.rect).CountNonZero
            If val = 0 And count = 0 Then
                val = fLessCount
                fLessCount += 1
            End If
            If count = 0 Then
                brick.contourFull = val
                dst0(brick.rect).SetTo(val Mod 255)
            End If
            brickPrev = brick
        Next

        For i = task.brickList.Count - 1 To 1 Step -1
            Dim brick = task.brickList(i)
            If brick.contourFull > 0 Then
                brickPrev = task.brickList(i - 1)
                If brickPrev.contourFull > 0 And brickPrev.contourFull <> 0 And brickPrev.contourFull <> brick.contourFull And
                    brickPrev.contourFull <> 0 Then
                    brickPrev.contourFull = brick.contourFull
                    dst0(brickPrev.rect).SetTo(brick.contourFull)
                    task.brickList(i - 1) = brickPrev
                End If
            End If
        Next

        labels(3) = "Mask for the " + CStr(fLessCount) + " featureless regions."
        If standaloneTest() Then
            dst1 = ShowPalette(dst0)
            dst2 = ShowAddweighted(src, dst1, labels(2))
        End If
    End Sub
End Class




Public Class XO_BrickPoint_FeatureLessOld : Inherits TaskParent
    Public edges As New EdgeLine_Basics
    Public classCount As Integer
    Public fLessMask As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)  ' mask for the featureless regions.
    Public Sub New()
        labels(3) = "CV_8U Mask for the featureless regions"
        desc = "Isolate the featureless regions using the sobel intensity."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.grayStable.Clone)

        fLessMask.SetTo(0)
        For Each brick In task.brickList
            If brick.rect.X = 0 Or brick.rect.Y = 0 Then Continue For

            If edges.dst2(brick.rect).CountNonZero = 0 Then
                brick.contourFull = 255
                fLessMask(brick.rect).SetTo(255)
            End If
        Next

        Dim brickPrev = task.brickList(0)
        classCount = 0
        For Each brick In task.brickList
            If brick.rect.X = 0 Or brick.rect.Y = 0 Then Continue For
            If brick.contourFull = 255 Then
                Dim brickAbove = task.brickList(brick.index - task.cellsPerRow)
                Dim val = brickAbove.contourFull
                If val = 0 Then val = brickPrev.contourFull
                If val = 0 And brick.contourFull <> 0 Then
                    classCount += 1
                    val = classCount
                End If
                If val <> 0 Then
                    brick.contourFull = val
                    fLessMask(brick.rect).SetTo(brick.contourFull)
                End If
            End If
            brickPrev = brick
        Next

        labels(3) = "Mask for the " + CStr(classCount) + " featureless regions."
        dst3 = ShowPalette(fLessMask)
        If standaloneTest() Then dst2 = ShowAddweighted(src, dst3, labels(2))
    End Sub
End Class








Public Class XO_Structured_Cloud2 : Inherits TaskParent
    Dim mmPixel As New Pixel_Measure
    Dim options As New Options_StructuredCloud
    Public Sub New()
        desc = "Attempt to impose a structure on the point cloud data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

        Dim stepX = dst2.Width / options.xLines
        Dim stepY = dst2.Height / options.yLines
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC3, 0)
        Dim midX = dst2.Width / 2
        Dim midY = dst2.Height / 2
        Dim halfStepX = stepX / 2
        Dim halfStepy = stepY / 2
        For y = 1 To options.yLines - 2
            For x = 1 To options.xLines - 2
                Dim p1 = New cv.Point2f(x * stepX, y * stepY)
                Dim p2 = New cv.Point2f((x + 1) * stepX, y * stepY)
                Dim d1 = task.pcSplit(2).Get(Of Single)(p1.Y, p1.X)
                Dim d2 = task.pcSplit(2).Get(Of Single)(p2.Y, p2.X)
                If stepX * options.threshold > Math.Abs(d1 - d2) And d1 > 0 And d2 > 0 Then
                    Dim p = task.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
                    Dim mmPP = mmPixel.Compute(d1)
                    If options.xConstraint Then
                        p(0) = (p1.X - midX) * mmPP
                        If p1.X = midX Then p(0) = mmPP
                    End If
                    If options.yConstraint Then
                        p(1) = (p1.Y - midY) * mmPP
                        If p1.Y = midY Then p(1) = mmPP
                    End If
                    Dim r = New cv.Rect(p1.X - halfStepX, p1.Y - halfStepy, stepX, stepY)
                    Dim meanVal = cv.Cv2.Mean(task.pcSplit(2)(r), task.depthMask(r))
                    p(2) = (d1 + d2) / 2
                    dst3.Set(Of cv.Vec3f)(y, x, p)
                End If
            Next
        Next
        dst2 = dst3(New cv.Rect(0, 0, options.xLines, options.yLines)).Resize(dst2.Size(), 0, 0,
                                                                              cv.InterpolationFlags.Nearest)
    End Sub
End Class








Public Class XO_Structured_Cloud : Inherits TaskParent
    Public options As New Options_StructuredCloud
    Public Sub New()
        task.gOptions.GridSlider.Value = 10
        desc = "Attempt to impose a linear structure on the pointcloud."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim yLines = CInt(options.xLines * dst2.Height / dst2.Width)

        Dim stepX = dst3.Width / options.xLines
        Dim stepY = dst3.Height / yLines
        dst2 = New cv.Mat(dst3.Size(), cv.MatType.CV_32FC3, 0)
        For y = 0 To yLines - 1
            For x = 0 To options.xLines - 1
                Dim r = New cv.Rect(x * stepX, y * stepY, stepX - 1, stepY - 1)
                Dim p1 = New cv.Point(r.X, r.Y)
                Dim p2 = New cv.Point(r.X + r.Width, r.Y + r.Height)
                Dim vec1 = task.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
                Dim vec2 = task.pointCloud.Get(Of cv.Vec3f)(p2.Y, p2.X)
                If vec1(2) > 0 And vec2(2) > 0 Then dst2(r).SetTo(vec1)
            Next
        Next
        labels(2) = "Structured_Cloud with " + CStr(yLines) + " rows " + CStr(options.xLines) + " columns"
    End Sub
End Class








Public Class XO_OpenGL_StructuredCloud : Inherits TaskParent
    Dim sCloud As New XO_Structured_Cloud
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        labels(2) = "Structured cloud 32fC3 data"
        desc = "Visualize the Structured_Cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sCloud.Run(src)

        dst2 = runRedC(src, labels(2))
        task.ogl.pointCloudInput = sCloud.dst2
        task.ogl.Run(dst2)
        If task.gOptions.getOpenGLCapture() Then dst3 = task.ogl.dst3
    End Sub
End Class





Public Class XO_OpenGL_PCpointsPlane : Inherits TaskParent
    Dim pts As New XO_PointCloud_PCPointsPlane
    Public Sub New()
        task.ogl.oglFunction = oCase.pcPoints
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        desc = "Display the points that are likely to be in a plane - found by both the vertical and horizontal searches"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pts.Run(src)

        task.ogl.dataInput = cv.Mat.FromPixelData(pts.pcPoints.Count, 1, cv.MatType.CV_32FC3, pts.pcPoints.ToArray)
        task.ogl.Run(New cv.Mat)
        If task.gOptions.getOpenGLCapture() Then dst3 = task.ogl.dst3
        labels(2) = "Point cloud points found = " + CStr(pts.pcPoints.Count / 2)
    End Sub
End Class










Public Class XO_Structured_Crosshairs : Inherits TaskParent
    Dim sCloud As New XO_Structured_Cloud
    Dim minX As Single, maxX As Single, minY As Single, maxY As Single
    Public Sub New()
        desc = "Connect vertical and horizontal dots that are in the same column and row."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim xLines = sCloud.options.indexX
        Dim yLines = CInt(xLines * dst2.Width / dst2.Height)
        If sCloud.options.indexX > xLines Then sCloud.options.indexX = xLines - 1
        If sCloud.options.indexY > yLines Then sCloud.options.indexY = yLines - 1

        sCloud.Run(src)
        Dim split = cv.Cv2.Split(sCloud.dst2)

        Dim mmX = GetMinMax(split(0))
        Dim mmY = GetMinMax(split(1))

        minX = If(minX > mmX.minVal, mmX.minVal, minX)
        minY = If(minY > mmY.minVal, mmY.minVal, minY)
        maxX = If(maxX < mmX.maxVal, mmX.maxVal, maxX)
        maxY = If(maxY < mmY.maxVal, mmY.maxVal, maxY)

        SetTrueText("mmx min/max = " + Format(minX, "0.00") + "/" + Format(maxX, "0.00") + " mmy min/max " + Format(minY, "0.00") +
                    "/" + Format(maxY, "0.00"), 3)

        dst2.SetTo(0)
        Dim white = New cv.Vec3b(255, 255, 255)
        Dim pointX As New cv.Mat(sCloud.dst2.Size(), cv.MatType.CV_32S, 0)
        Dim pointY As New cv.Mat(sCloud.dst2.Size(), cv.MatType.CV_32S, 0)
        Dim yy As Integer, xx As Integer
        For y = 1 To sCloud.dst2.Height - 1
            For x = 1 To sCloud.dst2.Width - 1
                Dim p = sCloud.dst2.Get(Of cv.Vec3f)(y, x)
                If p(2) > 0 Then
                    If Single.IsNaN(p(0)) Or Single.IsNaN(p(1)) Or Single.IsNaN(p(2)) Then Continue For
                    xx = dst2.Width * (maxX - p(0)) / (maxX - minX)
                    yy = dst2.Height * (maxY - p(1)) / (maxY - minY)
                    If xx < 0 Then xx = 0
                    If yy < 0 Then yy = 0
                    If xx >= dst2.Width Then xx = dst2.Width - 1
                    If yy >= dst2.Height Then yy = dst2.Height - 1
                    yy = dst2.Height - yy - 1
                    xx = dst2.Width - xx - 1
                    dst2.Set(Of cv.Vec3b)(yy, xx, white)

                    pointX.Set(Of Integer)(y, x, xx)
                    pointY.Set(Of Integer)(y, x, yy)
                    If x = sCloud.options.indexX Then
                        Dim p1 = New cv.Point(pointX.Get(Of Integer)(y - 1, x), pointY.Get(Of Integer)(y - 1, x))
                        If p1.X > 0 Then
                            Dim p2 = New cv.Point(xx, yy)
                            dst2.Line(p1, p2, task.highlight, task.lineWidth + 1, task.lineType)
                        End If
                    End If
                    If y = sCloud.options.indexY Then
                        Dim p1 = New cv.Point(pointX.Get(Of Integer)(y, x - 1), pointY.Get(Of Integer)(y, x - 1))
                        If p1.X > 0 Then
                            Dim p2 = New cv.Point(xx, yy)
                            dst2.Line(p1, p2, task.highlight, task.lineWidth + 1, task.lineType)
                        End If
                    End If
                End If
            Next
        Next
    End Sub
End Class








Public Class XO_Structured_ROI : Inherits TaskParent
    Public data As New cv.Mat
    Public oglData As New List(Of cv.Point3f)
    Public Sub New()
        task.gOptions.GridSlider.Value = 10
        desc = "Simplify the point cloud so it can be represented as quads in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = New cv.Mat(dst3.Size(), cv.MatType.CV_32FC3, 0)
        For Each roi In task.gridRects
            Dim d = task.pointCloud(roi).Mean(task.depthMask(roi))
            Dim depth = New cv.Vec3f(d.Val0, d.Val1, d.Val2)
            Dim pt = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
            Dim vec = task.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X)
            If vec(2) > 0 Then dst2(roi).SetTo(depth)
        Next

        labels(2) = traceName + " with " + CStr(task.gridRects.Count) + " regions was created"
    End Sub
End Class








Public Class XO_Structured_Tiles : Inherits TaskParent
    Public oglData As New List(Of cv.Vec3f)
    Dim hulls As New RedColor_Hulls
    Public Sub New()
        task.gOptions.GridSlider.Value = 10
        desc = "Use the OpenGL point size to represent the point cloud as data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst3

        dst3.SetTo(0)
        oglData.Clear()
        For Each roi In task.gridRects
            Dim c = dst2.Get(Of cv.Vec3b)(roi.Y, roi.X)
            If c = black Then Continue For
            oglData.Add(New cv.Vec3f(c(2) / 255, c(1) / 255, c(0) / 255))

            Dim v = task.pointCloud(roi).Mean(task.depthMask(roi))
            oglData.Add(New cv.Vec3f(v.Val0, v.Val1, v.Val2))
            dst3(roi).SetTo(c)
        Next
        labels(2) = traceName + " with " + CStr(task.gridRects.Count) + " regions was created"
    End Sub
End Class






Public Class XO_LineRect_CenterDepth : Inherits TaskParent
    Public options As New Options_LineRect
    Public Sub New()
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        desc = "Remove lines which have similar depth in bricks on either side of a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = src.Clone
        dst3 = src.Clone

        Dim depthThreshold = options.depthThreshold
        Dim depthLines As Integer, colorLines As Integer
        For Each lp In task.lineRGB.lpList
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
            Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
            Dim lpPerp = lp.perpendicularPoints(center, task.cellSize)
            Dim index1 As Integer = task.brickMap.Get(Of Single)(lpPerp.p1.Y, lpPerp.p1.X)
            Dim index2 As Integer = task.brickMap.Get(Of Single)(lpPerp.p2.Y, lpPerp.p2.X)
            Dim brick1 = task.brickList(index1)
            Dim brick2 = task.brickList(index2)
            If Math.Abs(brick1.depth - brick2.depth) > depthThreshold Then
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                depthLines += 1
            Else
                dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                colorLines += 1
            End If
        Next

        If task.heartBeat Then
            labels(2) = CStr(depthLines) + " lines were found between objects (depth Lines)"
            labels(3) = CStr(colorLines) + " internal lines were indentified and are not likely important"
        End If
    End Sub
End Class






Public Class XO_LongLine_BasicsEx : Inherits TaskParent
    Public lines As New XO_LongLine_Basics
    Public lpList As New List(Of lpData)
    Public Sub New()
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        desc = "Identify the longest lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        lpList.Clear()
        ' placeholder for zero so we can distinguish line 1 from the background which is 0.
        lpList.Add(New lpData(New cv.Point, New cv.Point))
        For Each lp In task.lineRGB.lpList
            DrawLine(dst2, lp.ep1, lp.ep2, white)
            If lp.p1.X > lp.p2.X Then lp = New lpData(lp.p2, lp.p1)
            lp.index = lpList.Count
            lpList.Add(lp)
        Next

        labels(2) = $"{task.lineRGB.lpList.Count} lines found, longest {lpList.Count} displayed."
    End Sub
End Class







Public Class XO_LongLine_Basics : Inherits TaskParent
    Public lpList As New List(Of lpData) ' The top X longest lines
    Dim hist As New Hist_GridCell
    Public Sub New()
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        desc = "Isolate the longest X lines and update the list of bricks containing each line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.algorithmPrep = False Then Exit Sub ' a direct call from another algorithm is unnecessary - already been run...
        If task.lineRGB.lpList.Count = 0 Then Exit Sub

        Dim lpLast As New List(Of lpData)
        Dim ptLast As New List(Of cv.Point)
        For Each lp In lpList
            ptLast.Add(lp.p1)
            ptLast.Add(lp.p2)
            lp.index = lpLast.Count
            lpLast.Add(lp)
        Next

        dst1.SetTo(0)
        lpList.Clear()
        ' placeholder for zero so we can distinguish line 1 from the background which is 0.
        lpList.Add(New lpData(New cv.Point, New cv.Point))
        Dim usedList As New List(Of cv.Point)
        For i = 1 To task.lineRGB.lpList.Count - 1
            Dim lp = task.lineRGB.lpList(i)
            lp.index = lpList.Count

            dst1.Line(lp.p1, lp.p2, lp.index, task.lineWidth, cv.LineTypes.Link4)
            lp.bricks.Clear()
            lpList.Add(lp)
        Next

        For Each brick In task.brickList
            If dst1(brick.rect).CountNonZero = 0 Then Continue For
            hist.Run(dst1(brick.rect))
            For i = hist.histarray.Count - 1 To 1 Step -1 ' why reverse?  So longer lines will claim the brick last.
                If hist.histarray(i) > 0 Then
                    lpList(i).bricks.Add(brick.index)
                End If
            Next
        Next

        dst3 = src.Clone
        dst2 = src
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            For Each index In lp.bricks
                dst2.Rectangle(task.brickList(index).rect, task.highlight, task.lineWidth)
            Next
        Next

        For Each lp In lpList
            Dim index As Integer = ptLast.IndexOf(lp.p1) / 2
            If index > 0 And ptLast.Contains(lp.p2) And index < lpLast.Count Then
                lp.age = lpLast(index).age + 1
            End If
        Next

        labels(2) = CStr(lpList.Count - 1) + " longest lines in the image in " + CStr(task.lineRGB.lpList.Count) + " total lines."
        labels(3) = labels(2)
    End Sub
End Class






Public Class XO_LineCoin_Basics : Inherits TaskParent
    Public longLines As New XO_LongLine_BasicsEx
    Public lpList As New List(Of lpData)
    Dim lpLists As New List(Of List(Of lpData))
    Public Sub New()
        dst2 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find the coincident lines in the image and measure their value."
    End Sub
    Public Function findLines(lpLists As List(Of List(Of lpData))) As List(Of lpData)
        Dim p1List As New List(Of cv.Point)
        Dim p2List As New List(Of cv.Point)
        Dim ptCounts As New List(Of Integer)
        Dim lp As lpData
        For Each lpList In lpLists
            For Each mp In lpList
                mp.m = CInt(mp.m * 10) / 10
                If mp.m = 0 Then
                    lp = New lpData(New cv.Point(mp.p1.X, 0), New cv.Point(mp.p1.X, dst2.Height))
                Else
                    lp = New lpData(mp.ep1, mp.ep2)
                End If
                Dim index = p1List.IndexOf(lp.p1)
                If index >= 0 Then
                    ptCounts(index) += 1
                Else
                    p1List.Add(lp.p1)
                    p2List.Add(lp.p2)
                    ptCounts.Add(1)
                End If
            Next
        Next
        lpList.Clear()
        dst2.SetTo(0)
        For i = 0 To p1List.Count - 1
            If ptCounts(i) >= task.frameHistoryCount Then
                DrawLine(dst2, p1List(i), p2List(i), 255)
                lpList.Add(New lpData(p1List(i), p2List(i)))
            End If
        Next
        If lpLists.Count >= task.frameHistoryCount Then lpLists.RemoveAt(0)
        Return lpList
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then lpLists.Clear()

        longLines.Run(src)
        lpLists.Add(longLines.lpList)
        lpList = findLines(lpLists)

        If standaloneTest() Then
            dst3 = src
            For Each lp In lpList
                dst3.Line(lp.p1, lp.p2, white)
            Next
        End If

        labels(2) = $"The {lpList.Count} lines below were present in each of the last " + CStr(task.frameHistoryCount) + " frames"
    End Sub
End Class





Public Class XO_LineCoin_HistoryIntercept : Inherits TaskParent
    Dim coin As New XO_LineCoin_Basics
    Public lpList As New List(Of lpData)
    Dim mpLists As New List(Of List(Of lpData))
    Public Sub New()
        dst2 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "find lines with coincident slopes and intercepts."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then mpLists.Clear()

        coin.Run(src)
        dst2 = coin.dst2

        labels(2) = $"The {lpList.Count} lines below were present in each of the last " + CStr(task.frameHistoryCount) + " frames"
    End Sub
End Class





Public Class XO_LineCoin_Parallel : Inherits TaskParent
    Dim parallel As New LongLine_ExtendParallel
    Dim near As New XO_Line_Nearest
    Public coinList As New List(Of coinPoints)
    Public Sub New()
        desc = "Find the lines that are coincident in the parallel lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        parallel.Run(src)

        coinList.Clear()

        For Each cp In parallel.parList
            near.lp = New lpData(cp.p1, cp.p2)
            near.pt = cp.p3
            near.Run(src)
            Dim d1 = near.distance

            near.pt = cp.p4
            near.Run(src)
            If near.distance <= 1 Or d1 <= 1 Then coinList.Add(cp)
        Next

        dst2 = src.Clone
        For Each cp In coinList
            dst2.Line(cp.p3, cp.p4, cv.Scalar.Red, task.lineWidth + 2, task.lineType)
            dst2.Line(cp.p1, cp.p2, task.highlight, task.lineWidth + 1, task.lineType)
        Next
        labels(2) = CStr(coinList.Count) + " coincident lines were detected"
    End Sub
End Class










Public Class XO_Structured_Depth : Inherits TaskParent
    Dim sliceH As New Structured_SliceH
    Public Sub New()
        labels = {"", "", "Use mouse to explore slices", "Top down view of the highlighted slice (at left)"}
        desc = "Use the structured depth to enhance the depth away from the centerline."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sliceH.Run(src)
        dst0 = sliceH.dst3
        dst2 = sliceH.dst2

        Dim mask = sliceH.sliceMask
        Dim perMeter = dst3.Height / task.MaxZmeters
        dst3.SetTo(0)
        Dim white As New cv.Vec3b(255, 255, 255)
        For y = 0 To mask.Height - 1
            For x = 0 To mask.Width - 1
                Dim val = mask.Get(Of Byte)(y, x)
                If val > 0 Then
                    Dim depth = task.pcSplit(2).Get(Of Single)(y, x)
                    Dim row = dst1.Height - depth * perMeter
                    dst3.Set(Of cv.Vec3b)(If(row < 0, 0, row), x, white)
                End If
            Next
        Next
    End Sub
End Class








Public Class XO_Structured_FloorCeiling : Inherits TaskParent
    Public slice As New Structured_SliceEither
    Public Sub New()
        task.kalman = New Kalman_Basics
        ReDim task.kalman.kInput(2 - 1)
        OptionParent.findCheckBox("Top View (Unchecked Side View)").Checked = False
        desc = "Find the floor or ceiling plane"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        slice.Run(src)
        dst2 = slice.heat.dst3

        Dim floorMax As Single
        Dim floorY As Integer
        Dim floorBuffer = dst2.Height / 4
        For i = dst2.Height - 1 To 0 Step -1
            Dim nextSum = slice.heat.dst3.Row(i).Sum()(0)
            If nextSum > 0 Then floorBuffer -= 1
            If floorBuffer = 0 Then Exit For
            If nextSum > floorMax Then
                floorMax = nextSum
                floorY = i
            End If
        Next

        Dim ceilingMax As Single
        Dim ceilingY As Integer
        Dim ceilingBuffer = dst2.Height / 4
        For i = 0 To dst3.Height - 1
            Dim nextSum = slice.heat.dst3.Row(i).Sum()(0)
            If nextSum > 0 Then ceilingBuffer -= 1
            If ceilingBuffer = 0 Then Exit For
            If nextSum > ceilingMax Then
                ceilingMax = nextSum
                ceilingY = i
            End If
        Next

        task.kalman.kInput(0) = floorY
        task.kalman.kInput(1) = ceilingY
        task.kalman.Run(emptyMat)

        labels(2) = "Current slice is at row =" + CStr(task.mouseMovePoint.Y)
        labels(3) = "Ceiling is at row =" + CStr(CInt(task.kalman.kOutput(1))) + " floor at y=" + CStr(CInt(task.kalman.kOutput(0)))

        DrawLine(dst2, New cv.Point(0, floorY), New cv.Point(dst2.Width, floorY), cv.Scalar.Yellow)
        SetTrueText("floor", New cv.Point(10, floorY + task.DotSize), 3)

        Dim rect = New cv.Rect(0, Math.Max(ceilingY - 5, 0), dst2.Width, 10)
        Dim mask = slice.heat.dst3(rect)
        Dim mean As cv.Scalar, stdev As cv.Scalar
        cv.Cv2.MeanStdDev(mask, mean, stdev)
        If mean(0) < mean(2) Then
            DrawLine(dst2, New cv.Point(0, ceilingY), New cv.Point(dst2.Width, ceilingY), cv.Scalar.Yellow)
            SetTrueText("ceiling", New cv.Point(10, ceilingY + task.DotSize), 3)
        Else
            SetTrueText("Ceiling does not appear to be present", 3)
        End If
    End Sub
End Class








Public Class XO_Structured_Rebuild : Inherits TaskParent
    Dim heat As New HeatMap_Basics
    Dim options As New Options_Structured
    Dim thickness As Single
    Public pointcloud As New cv.Mat
    Public Sub New()
        labels = {"", "", "X values in point cloud", "Y values in point cloud"}
        desc = "Rebuild the point cloud using inrange - not useful yet"
    End Sub
    Private Function rebuildX(viewX As cv.Mat) As cv.Mat
        Dim output As New cv.Mat(task.pcSplit(1).Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim firstCol As Integer
        For firstCol = 0 To viewX.Width - 1
            If viewX.Col(firstCol).CountNonZero > 0 Then Exit For
        Next

        Dim lastCol As Integer
        For lastCol = viewX.Height - 1 To 0 Step -1
            If viewX.Row(lastCol).CountNonZero > 0 Then Exit For
        Next

        Dim sliceMask As New cv.Mat
        For i = firstCol To lastCol
            Dim planeX = -task.xRange * (task.topCameraPoint.X - i) / task.topCameraPoint.X
            If i > task.topCameraPoint.X Then planeX = task.xRange * (i - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X)

            cv.Cv2.InRange(task.pcSplit(0), planeX - thickness, planeX + thickness, sliceMask)
            output.SetTo(planeX, sliceMask)
        Next
        Return output
    End Function
    Private Function rebuildY(viewY As cv.Mat) As cv.Mat
        Dim output As New cv.Mat(task.pcSplit(1).Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim firstLine As Integer
        For firstLine = 0 To viewY.Height - 1
            If viewY.Row(firstLine).CountNonZero > 0 Then Exit For
        Next

        Dim lastLine As Integer
        For lastLine = viewY.Height - 1 To 0 Step -1
            If viewY.Row(lastLine).CountNonZero > 0 Then Exit For
        Next

        Dim sliceMask As New cv.Mat
        For i = firstLine To lastLine
            Dim planeY = -task.yRange * (task.sideCameraPoint.Y - i) / task.sideCameraPoint.Y
            If i > task.sideCameraPoint.Y Then planeY = task.yRange * (i - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y)

            cv.Cv2.InRange(task.pcSplit(1), planeY - thickness, planeY + thickness, sliceMask)
            output.SetTo(planeY, sliceMask)
        Next
        Return output
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim metersPerPixel = task.MaxZmeters / dst3.Height
        thickness = options.sliceSize * metersPerPixel
        heat.Run(src)

        If options.rebuilt Then
            task.pcSplit(0) = rebuildX(heat.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            task.pcSplit(1) = rebuildY(heat.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            cv.Cv2.Merge(task.pcSplit, pointcloud)
        Else
            task.pcSplit = task.pointCloud.Split()
            pointcloud = task.pointCloud
        End If

        dst2 = Convert32f_To_8UC3(task.pcSplit(0))
        dst3 = Convert32f_To_8UC3(task.pcSplit(1))
        dst2.SetTo(0, task.noDepthMask)
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class







Public Class XO_OpenGL_Rebuilt : Inherits TaskParent
    Dim rebuild As New XO_Structured_Rebuild
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Review the rebuilt point cloud from Structured_Rebuild"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rebuild.Run(src)
        dst2 = rebuild.dst2
        task.ogl.pointCloudInput = rebuild.pointcloud
        task.ogl.Run(task.color)
        If task.gOptions.getOpenGLCapture() Then dst3 = task.ogl.dst3
    End Sub
End Class








Public Class XO_tructured_MouseSlice : Inherits TaskParent
    Dim slice As New Structured_SliceEither
    Dim lines As New LineRGB_RawSorted
    Public Sub New()
        If task.lineRGB Is Nothing Then task.lineRGB = New LineRGB_Basics
        labels(2) = "Center Slice in yellow"
        labels(3) = "White = SliceV output, Red Dot is avgPt"
        desc = "Find the vertical center line with accurate depth data.."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.mouseMovePoint = newPoint Then task.mouseMovePoint = New cv.Point(dst2.Width / 2, dst2.Height)
        slice.Run(src)

        lines.Run(slice.sliceMask)
        Dim tops As New List(Of Integer)
        Dim bots As New List(Of Integer)
        Dim topsList As New List(Of cv.Point)
        Dim botsList As New List(Of cv.Point)
        If task.lineRGB.lpList.Count > 0 Then
            dst3 = lines.dst2
            For Each lp In task.lineRGB.lpList
                dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth + 3, task.lineType)
                tops.Add(If(lp.p1.Y < lp.p2.Y, lp.p1.Y, lp.p2.Y))
                bots.Add(If(lp.p1.Y > lp.p2.Y, lp.p1.Y, lp.p2.Y))
                topsList.Add(lp.p1)
                botsList.Add(lp.p2)
            Next

            'Dim topPt = topsList(tops.IndexOf(tops.Min))
            'Dim botPt = botsList(bots.IndexOf(bots.Max))
            'DrawCircle(dst3,New cv.Point2f((topPt.X + botPt.X) / 2, (topPt.Y + botPt.Y) / 2), task.DotSize + 5, cv.Scalar.Red)
            'dst3.Line(topPt, botPt, cv.Scalar.Red, task.lineWidth, task.lineType)
            'DrawLine(dst2,topPt, botPt, task.highlight, task.lineWidth + 2, task.lineType)
        End If
        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(white, dst3)
        End If
    End Sub
End Class








Public Class XO_OpenGL_Tiles : Inherits TaskParent
    Dim sCloud As New XO_Structured_Tiles
    Public Sub New()
        task.ogl.oglFunction = oCase.drawTiles
        labels = {"", "", "Input from Structured_Tiles", ""}
        desc = "Display the quads built by Structured_Tiles in OpenGL - uses OpenGL's point size"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sCloud.Run(src)
        dst2 = sCloud.dst2
        dst3 = sCloud.dst3

        task.ogl.dataInput = cv.Mat.FromPixelData(sCloud.oglData.Count, 1, cv.MatType.CV_32FC3, sCloud.oglData.ToArray)
        task.ogl.Run(src)
        If task.gOptions.getOpenGLCapture() Then dst3 = task.ogl.dst3
    End Sub
End Class







Public Class XO_Contour_Gray : Inherits TaskParent
    Public contour As New List(Of cv.Point)
    Public options As New Options_Contours
    Dim myFrameCount As Integer = task.frameCount
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Find the contour for the src."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If myFrameCount <> task.frameCount Then
            options.Run() ' avoid running options more than once per frame.
            myFrameCount = task.frameCount
        End If

        If standalone Then
            task.redOptions.ColorSource.SelectedItem() = "Reduction_Basics"
            reduction.Run(src)
            src = reduction.dst2
        End If

        Dim allContours As cv.Point()()
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.FindContours(src, allContours, Nothing, cv.RetrievalModes.External, options.ApproximationMode)
        If allContours.Count = 0 Then Exit Sub

        dst2 = src
        For Each tour In allContours
            DrawContour(dst2, tour.ToList, white, task.lineWidth)
        Next
        labels(2) = $"There were {allContours.Count} contours found."
    End Sub
End Class







Public Class XO_Contour_RC_AddContour : Inherits TaskParent
    Public contour As New List(Of cv.Point)
    Public options As New Options_Contours
    Dim myFrameCount As Integer = task.frameCount
    Dim reduction As New Reduction_Basics
    Dim contours As New Contour_Regions
    Public Sub New()
        desc = "Find the contour for the src."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If myFrameCount <> task.frameCount Then
            options.Run() ' avoid running options more than once per frame.
            myFrameCount = task.frameCount
        End If

        If standalone Then
            reduction.Run(src)
            src = reduction.dst2
        End If
        dst2 = src.Clone
        dst3 = ShowPalette(dst2)

        contours.Run(dst2)

        Dim maxCount As Integer, maxIndex As Integer
        For i = 0 To contours.contourList.Count - 1
            Dim len = CInt(contours.contourList(i).Count)
            If len > maxCount Then
                maxCount = len
                maxIndex = i
            End If
        Next
        If contours.contourList.Count = 0 Then Exit Sub
        Dim contour = New List(Of cv.Point)(contours.contourList(maxIndex).ToList)
        DrawContour(dst2, contour, task.highlight, task.lineWidth)
    End Sub
End Class





Public Class XO_Contour_RedCloud : Inherits TaskParent
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Show all the contours found in the RedCloud output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        dst3.SetTo(0)
        For Each rc In task.rcList
            DrawContour(dst3(rc.rect), rc.contour, 255, task.lineWidth)
        Next
    End Sub
End Class







Public Class XO_OpenGL_PlaneClusters3D : Inherits TaskParent
    Dim eq As New Plane_Equation
    Public Sub New()
        task.ogl.oglFunction = oCase.pcPoints
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        labels(3) = "Only the cells with a high probability plane are presented - blue on X-axis, green on Y-axis, red on Z-axis"
        desc = "Cluster the plane equations to find major planes in the image and display the clusters in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        dst3 = task.redC.dst3

        Dim pcPoints As New List(Of cv.Point3f)
        Dim blue As New cv.Point3f(0, 0, 1), red As New cv.Point3f(1, 0, 0), green As New cv.Point3f(0, 1, 0) ' NOTE: RGB, not BGR...
        For Each rc In task.rcList
            If rc.mmZ.maxVal > 0 Then
                eq.rc = rc
                eq.Run(src)
                rc = eq.rc
            End If
            If rc.eq = New cv.Vec4f Then Continue For

            If rc.eq.Item0 > rc.eq.Item1 And rc.eq.Item0 > rc.eq.Item2 Then pcPoints.Add(red)
            If rc.eq.Item1 > rc.eq.Item0 And rc.eq.Item1 > rc.eq.Item2 Then pcPoints.Add(green)
            If rc.eq.Item2 > rc.eq.Item0 And rc.eq.Item2 > rc.eq.Item1 Then pcPoints.Add(blue)

            pcPoints.Add(New cv.Point3f(rc.eq.Item0 * 0.5, rc.eq.Item1 * 0.5, rc.eq.Item2 * 0.5))
        Next

        task.ogl.dataInput = cv.Mat.FromPixelData(pcPoints.Count, 1, cv.MatType.CV_32FC3, pcPoints.ToArray)
        task.ogl.Run(New cv.Mat)
    End Sub
End Class





Public Class XO_Pixel_Unique_CPP : Inherits TaskParent
    Public Sub New()
        cPtr = Pixels_Vector_Open()
        desc = "Create the list of pixels in a RedCloud Cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.drawRect <> New cv.Rect Then src = src(task.drawRect)
        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim classCount = Pixels_Vector_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        If classCount = 0 Then Exit Sub
        Dim pixelData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_8UC3, Pixels_Vector_Pixels(cPtr))
        SetTrueText(CStr(classCount) + " unique BGR pixels were found in the src." + vbCrLf +
                    "Or " + Format(classCount / src.Total, "0%") + " of the input were unique pixels.")
    End Sub
    Public Sub Close()
        Pixels_Vector_Close(cPtr)
    End Sub
End Class





Public Class XO_Sides_Corner : Inherits TaskParent
    Dim sides As New XO_Contour_RedCloudCorners
    Public Sub New()
        labels = {"", "", "RedColor_Basics output", ""}
        desc = "Find the 4 points farthest from the center in each quadrant of the selected RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        sides.Run(src)
        dst3 = sides.dst3
        SetTrueText("Center point is rcSelect.maxDist", 3)
    End Sub
End Class









Public Class XO_Sides_Basics : Inherits TaskParent
    Public sides As New Profile_Basics
    Public corners As New XO_Contour_RedCloudCorners
    Public Sub New()
        labels = {"", "", "RedCloud output", "Selected Cell showing the various extrema."}
        desc = "Find the 6 extrema and the 4 farthest points in each quadrant for the selected RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sides.Run(src)
        dst2 = sides.dst2
        dst3 = sides.dst3

        Dim corners = sides.corners.ToList
        For i = 0 To corners.Count - 1
            Dim nextColor = sides.cornerColors(i)
            Dim nextLabel = sides.cornerNames(i)
            DrawLine(dst3, task.rcD.maxDist, corners(i), white)
            SetTrueText(nextLabel, New cv.Point(corners(i).X, corners(i).Y), 3)
        Next

        If corners.Count Then SetTrueText(sides.strOut, 3) Else SetTrueText(strOut, 3)
    End Sub
End Class






Public Class XO_Contour_RedCloudCorners : Inherits TaskParent
    Public corners(4 - 1) As cv.Point
    Public rc As New rcData
    Public Sub New()
        labels(2) = "The RedCloud Output with the highlighted contour to smooth"
        desc = "Find the point farthest from the center in each cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            dst2 = runRedC(src, labels(2))
            rc = task.rcD
        End If

        dst3.SetTo(0)
        DrawCircle(dst3, rc.maxDist, task.DotSize, white)
        Dim center As New cv.Point(rc.maxDist.X - rc.rect.X, rc.maxDist.Y - rc.rect.Y)
        Dim maxDistance(4 - 1) As Single
        For i = 0 To corners.Length - 1
            corners(i) = center ' default is the center - a triangle shape can omit a corner
        Next
        If rc.contour Is Nothing Then Exit Sub
        For Each pt In rc.contour
            Dim quad As Integer
            If pt.X - center.X >= 0 And pt.Y - center.Y <= 0 Then quad = 0 ' upper right quadrant
            If pt.X - center.X >= 0 And pt.Y - center.Y >= 0 Then quad = 1 ' lower right quadrant
            If pt.X - center.X <= 0 And pt.Y - center.Y >= 0 Then quad = 2 ' lower left quadrant
            If pt.X - center.X <= 0 And pt.Y - center.Y <= 0 Then quad = 3 ' upper left quadrant
            Dim dist = center.DistanceTo(pt)
            If dist > maxDistance(quad) Then
                maxDistance(quad) = dist
                corners(quad) = pt
            End If
        Next

        DrawContour(dst3(rc.rect), rc.contour, white)
        For i = 0 To corners.Count - 1
            DrawLine(dst3(rc.rect), center, corners(i), white)
        Next
    End Sub
End Class





Public Class XO_Sides_Profile : Inherits TaskParent
    Dim sides As New Contour_SidePoints
    Public Sub New()
        labels = {"", "", "RedColor_Basics Output", "Selected Cell"}
        desc = "Find the 6 corners - left/right, top/bottom, front/back - of a RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        sides.Run(src)
        dst3 = sides.dst3
        SetTrueText(sides.strOut, 3)
    End Sub
End Class









Public Class XO_Sides_ColorC : Inherits TaskParent
    Dim sides As New XO_Sides_Basics
    Public Sub New()
        labels = {"", "", "RedColor Output", "Cell Extrema"}
        desc = "Find the extrema - top/bottom, left/right, near/far - points for a RedColor Cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        sides.Run(src)
        dst3 = sides.dst3
    End Sub
End Class






Public Class XO_Contour_RedCloudEdges : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "EdgeLine_Basics output", "", "Pixels below are both cell boundaries and edges."}
        desc = "Intersect the cell contours and the edges in the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        runRedC(src)
        labels(2) = task.redC.labels(2) + " - Contours only.  Click anywhere to select a cell"

        dst2.SetTo(0)
        For Each rc In task.rcList
            DrawContour(dst2(rc.rect), rc.contour, 255, task.lineWidth)
        Next

        dst3 = task.edges.dst2 And dst2
    End Sub
End Class







Public Class XO_LeftRight_Markers : Inherits TaskParent
    Dim redView As New LeftRight_Reduction
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "Reduced Left Image", "Reduced Right Image"}
        desc = "Use the left/right reductions to find hard markers - neighboring pixels of identical values"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redView.Run(src)
        dst2 = redView.reduction.dst3.Clone
        dst3 = redView.reduction.dst3.Clone

        Dim left = redView.dst2
        Dim right = redView.dst3

        ' find combinations in the left image - they are markers.
        Dim impList As New List(Of List(Of Integer))
        Dim lineLen = task.gOptions.DebugSlider.Value
        For y = 0 To left.Height - 1
            Dim important As New List(Of Integer)
            Dim impCounts As New List(Of Integer)
            For x = 0 To left.Width - 1
                Dim m1 = left.Get(Of Byte)(y, x)
                If important.Contains(m1) = False Then
                    important.Add(m1)
                    impCounts.Add(1)
                Else
                    impCounts(important.IndexOf(m1)) += 1
                End If
            Next
            impList.Add(important)
            impList.Add(impCounts)
        Next

        dst0.SetTo(0)
        dst1.SetTo(0)

        For i = 0 To left.Rows - 1
            Dim important = impList(i * 2)
            Dim impcounts = impList(i * 2 + 1)
            Dim maxVal = important(impcounts.IndexOf(impcounts.Max))

            Dim tmp = left.Row(i).InRange(maxVal, maxVal)
            dst0.Row(i).SetTo(255, tmp)

            tmp = right.Row(i).InRange(maxVal, maxVal)
            dst1.Row(i).SetTo(255, tmp)
        Next
    End Sub
End Class








Public Class XO_LeftRight_Markers1 : Inherits TaskParent
    Dim redView As New LeftRight_Reduction
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "Reduced Left Image", "Reduced Right Image"}
        desc = "Use the left/right reductions to find markers - neighboring pixels of identical values"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redView.Run(src)
        dst0 = redView.dst2
        dst1 = redView.dst3

        ' find combinations in the left image - they are markers.
        Dim impList As New List(Of List(Of Integer))
        Dim lineLen = task.gOptions.DebugSlider.Value
        For y = 0 To dst2.Height - 1
            Dim important As New List(Of Integer)
            Dim impCounts As New List(Of Integer)
            For x = 0 To dst0.Width - 1
                Dim m1 = dst0.Get(Of Byte)(y, x)
                If important.Contains(m1) = False Then
                    important.Add(m1)
                    impCounts.Add(1)
                Else
                    impCounts(important.IndexOf(m1)) += 1
                End If
            Next
            impList.Add(important)
            impList.Add(impCounts)
        Next

        dst2.SetTo(0)
        dst3.SetTo(0)

        For i = 0 To dst2.Rows - 1
            Dim important = impList(i * 2)
            Dim impcounts = impList(i * 2 + 1)
            Dim maxVal = important(impcounts.IndexOf(impcounts.Max))

            Dim tmp = dst0.Row(i).InRange(maxVal, maxVal)
            dst2.Row(i).SetTo(255, tmp)

            tmp = dst1.Row(i).InRange(maxVal, maxVal)
            dst3.Row(i).SetTo(255, tmp)
        Next
    End Sub
End Class