Imports System.IO
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes
Imports System.Runtime.InteropServices
Imports System.Threading
Imports OpenCvSharp
Imports OpenCvSharp.ML
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class XO_Model_FlatSurfaces : Inherits TaskParent
        Public totalPixels As Integer
        Dim floorList As New List(Of Single)
        Dim ceilingList As New List(Of Single)
        Public Sub New()
            desc = "Minimalist approach to find a flat surface that is oriented to gravity (floor or ceiling)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim ranges() = New cv.Rangef() {New cv.Rangef(-taskAlg.yRange, taskAlg.yRange), New cv.Rangef(0, taskAlg.MaxZmeters)}
            cv.Cv2.CalcHist({taskAlg.pointCloud}, {1, 2}, New cv.Mat, dst0, 2,
                            {dst2.Height, dst2.Width}, ranges)

            Dim thicknessCMs = 0.1, rect As cv.Rect, nextY As Single
            totalPixels = 0
            For y = dst0.Height - 2 To 0 Step -1
                rect = New cv.Rect(0, y, dst0.Width - 1, 1)
                Dim count = dst0(rect).CountNonZero
                Dim pixelCount = dst0(rect).Sum()
                totalPixels += pixelCount.Val0
                If count > 10 Then
                    nextY = -taskAlg.yRange * (taskAlg.sideCameraPoint.Y - y) / taskAlg.sideCameraPoint.Y - thicknessCMs
                    Exit For
                End If
            Next

            Dim floorY = rect.Y
            floorList.Add(nextY)
            taskAlg.pcFloor = floorList.Average()
            If floorList.Count > taskAlg.frameHistoryCount Then floorList.RemoveAt(0)
            labels(2) = "Y = " + Format(taskAlg.pcFloor, fmt3) + " separates the floor.  Total pixels below floor level = " + Format(totalPixels, fmt0)

            For y = 0 To dst2.Height - 1
                rect = New cv.Rect(0, y, dst0.Width - 1, 1)
                Dim count = dst0(rect).CountNonZero
                Dim pixelCount = dst0(rect).Sum()
                totalPixels += pixelCount.Val0
                If count > 10 Then
                    nextY = -taskAlg.yRange * (taskAlg.sideCameraPoint.Y - y) / taskAlg.sideCameraPoint.Y - thicknessCMs
                    Exit For
                End If
            Next

            Dim ceilingY = rect.Y
            ceilingList.Add(nextY)
            taskAlg.pcCeiling = ceilingList.Average()
            If ceilingList.Count > taskAlg.frameHistoryCount Then ceilingList.RemoveAt(0)
            labels(3) = "Y = " + Format(taskAlg.pcCeiling, fmt3) + " separates the ceiling.  Total pixels above ceiling level = " + Format(totalPixels, fmt0)

            If standaloneTest() Then
                dst2 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)
                dst2.ConvertTo(dst2, cv.MatType.CV_8U)
                dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                dst2.Line(New cv.Point(0, floorY), New cv.Point(dst2.Width, floorY), cv.Scalar.Red, taskAlg.lineWidth + 2, taskAlg.lineType)
                dst2.Line(New cv.Point(0, ceilingY), New cv.Point(dst2.Width, ceilingY), cv.Scalar.Red, taskAlg.lineWidth + 2, taskAlg.lineType)
            End If
        End Sub
    End Class








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

            work = taskAlg.pcSplit(1).InRange(-threshold, threshold)
            work.SetTo(0, taskAlg.noDepthMask)
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

            work = taskAlg.pcSplit(0).InRange(-threshold, threshold)
            work.SetTo(0, taskAlg.noDepthMask)
            work.ConvertTo(dst3, cv.MatType.CV_8U)
            Dim gPoints = dst3.FindNonZero()
            Dim sampleUnused As Integer
            xTop = findFirst(gPoints, False, sampleUnused)
            xBot = findLast(gPoints, False, sampleUnused)

            If standaloneTest() Then
                Dim lineHorizon As lpData, lineGravity As lpData
                If hPoints.Total > 0 Then
                    lineHorizon = New lpData(New cv.Point(0, yLeft), New cv.Point(dst2.Width, yRight))
                Else
                    lineHorizon = New lpData
                End If

                lineGravity = New lpData(New cv.Point(xTop, 0), New cv.Point(xBot, dst2.Height))

                dst2.SetTo(0)
                vbc.DrawLine(dst2, lineGravity.p1, lineGravity.p2, taskAlg.highlight)
                vbc.DrawLine(dst2, lineHorizon.p1, lineHorizon.p2, cv.Scalar.Red)
            End If
        End Sub
    End Class





    Public Class XO_Horizon_FindNonZero : Inherits TaskParent
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            taskAlg.lineGravity = New lpData(New cv.Point2f(dst2.Width / 2, 0),
                                             New cv.Point2f(dst2.Width / 2, dst2.Height))
            taskAlg.lineHorizon = New lpData(New cv.Point2f(0, dst2.Height / 2), New cv.Point2f(dst2.Width, dst2.Height / 2))
            labels = {"", "Horizon vector mask", "Crosshairs - lineGravity (vertical) and lineHorizon (horizontal)", "Gravity vector mask"}
            desc = "Create lines for the gravity vector and horizon vector in the camera image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim pc = taskAlg.pointCloud
            Dim split = pc.Split()
            split(2).SetTo(taskAlg.MaxZmeters)
            cv.Cv2.Merge(split, pc)

            pc = (pc.Reshape(1, pc.Rows * pc.Cols) * taskAlg.gMatrix).ToMat.Reshape(3, pc.Rows)

            dst1 = split(1).InRange(-0.05, 0.05)
            dst1.SetTo(0, taskAlg.noDepthMask)
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
                taskAlg.lineHorizon = New lpData(lp.p1, lp.p2)
                vbc.DrawLine(dst2, taskAlg.lineHorizon.p1, taskAlg.lineHorizon.p2, 255)
            End If

            dst3 = split(0).InRange(-0.01, 0.01)
            dst3.SetTo(0, taskAlg.noDepthMask)
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
                    taskAlg.lineGravity = New lpData(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
                Else
                    Dim lp = findEdgePoints(New lpData(p1, p2))
                    taskAlg.lineGravity = New lpData(lp.p1, lp.p2)
                End If
                vbc.DrawLine(dst2, taskAlg.lineGravity.p1, taskAlg.lineGravity.p2, 255)
            End If
        End Sub
    End Class







    Public Class XO_Horizon_Perpendicular : Inherits TaskParent
        Dim perp As New Line_PerpendicularTest
        Public Sub New()
            labels(2) = "Yellow line is the perpendicular to the horizon.  White is gravity vector from the IMU."
            desc = "Find the gravity vector using the perpendicular to the horizon."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src
            vbc.DrawLine(dst2, taskAlg.lineHorizon.p1, taskAlg.lineHorizon.p2, white)

            perp.input = taskAlg.lineHorizon
            perp.Run(src)
            vbc.DrawLine(dst2, perp.output.p1, perp.output.p2, cv.Scalar.Yellow)

            Dim gVec = taskAlg.lineGravity
            gVec.p1.X += 10
            gVec.p2.X += 10
            vbc.DrawLine(dst2, gVec.p1, gVec.p2, white)
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
            If taskAlg.heartBeat Then
                If p1.Y >= 1 And p1.Y <= dst2.Height - 1 Then
                    strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf
                End If
            End If

            dst2.SetTo(0)
            For Each pt In points
                pt = New cv.Point(pt.X * resizeRatio, pt.Y * resizeRatio)
                DrawCircle(dst2, pt, taskAlg.DotSize, white)
            Next

            vbc.DrawLine(dst2, vec.p1, vec.p2, 255)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32F Then dst0 = taskAlg.pcSplit(1) Else dst0 = src

            dst0 = dst0.Abs()
            dst1 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
            dst0.SetTo(taskAlg.MaxZmeters, Not dst1)

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
        Dim options As New Options_Features
        Public Sub New()
            desc = "Validate the horizon points using Match_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim pad = taskAlg.brickSize / 2

            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If taskAlg.heartBeat Then
                ptLeft = taskAlg.lineGravity.p1
                ptRight = taskAlg.lineGravity.p2
                Dim r = ValidateRect(New cv.Rect(ptLeft.X - pad, ptLeft.Y - pad, taskAlg.brickSize, taskAlg.brickSize))
                leftTemplate = src(r)

                r = ValidateRect(New cv.Rect(ptRight.X - pad, ptRight.Y - pad, taskAlg.brickSize, taskAlg.brickSize))
                rightTemplate = src(r)
            Else
                Dim r = ValidateRect(New cv.Rect(ptLeft.X - pad, ptLeft.Y - pad, taskAlg.brickSize, taskAlg.brickSize))
                match.template = leftTemplate.Clone
                match.Run(src)
                ptLeft = match.newCenter

                r = ValidateRect(New cv.Rect(ptRight.X - pad, ptRight.Y - pad, taskAlg.brickSize, taskAlg.brickSize))
                match.template = leftTemplate.Clone
                match.Run(src)
                ptLeft = match.newCenter
            End If
        End Sub
    End Class






    Public Class XO_Horizon_ExternalTest : Inherits TaskParent
        Dim horizon As New XO_Horizon_Basics
        Public Sub New()
            desc = "Supply the point cloud input to Horizon_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst0 = taskAlg.pcSplit(1)
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
            If taskAlg.heartBeat Then
                If p1.Y >= 1 And p1.Y <= dst2.Height - 1 Then strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf
            End If

            dst2.SetTo(0)
            dst3.SetTo(0)
            For Each pt In points
                DrawCircle(dst2, pt, taskAlg.DotSize, white)
            Next

            vbc.DrawLine(dst2, taskAlg.lineGravity.p1, taskAlg.lineGravity.p2, white)
            vbc.DrawLine(dst3, taskAlg.lineGravity.p1, taskAlg.lineGravity.p2, white)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32F Then dst0 = taskAlg.pcSplit(0) Else dst0 = src

            dst0 = dst0.Abs()
            dst1 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
            dst0.SetTo(taskAlg.MaxZmeters, Not dst1)

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
                taskAlg.lineGravity = New lpData(lp.p1, lp.p2)
                If standaloneTest() Then displayResults(p1, p2)
            End If

            taskAlg.lineHorizon = Line_PerpendicularTest.computePerp(taskAlg.lineGravity)
            SetTrueText(strOut, 3)
        End Sub
    End Class




    Public Class XO_CameraMotion_Basics : Inherits TaskParent
        Public translationX As Integer
        Public translationY As Integer
        Public secondOpinion As Boolean
        Dim feat As New Swarm_Basics
        Dim options As New Options_Diff
        Public Sub New()
            dst2 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            dst3 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            taskAlg.gOptions.DebugSlider.Value = 3
            desc = "Merge with previous image using just translation of the gravity vector and horizon vector (if present)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim lineGravity = New lpData(taskAlg.lineGravity.p1, taskAlg.lineGravity.p2)
            Dim lineHorizon = New lpData(taskAlg.lineHorizon.p1, taskAlg.lineHorizon.p2)

            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            translationX = taskAlg.gOptions.DebugSlider.Value ' Math.Round(lineGravity.p1.X - taskAlg.lineGravity.p1.X)
            translationY = taskAlg.gOptions.DebugSlider.Value ' Math.Round(lineHorizon.p1.Y - taskAlg.lineHorizon.p1.Y)
            If Math.Abs(translationX) >= dst2.Width / 2 Then translationX = 0
            If lineHorizon.p1.Y >= dst2.Height Or lineHorizon.p2.Y >= dst2.Height Or Math.Abs(translationY) >= dst2.Height / 2 Then
                lineHorizon = New lpData(New cv.Point2f, New cv.Point2f(336, 0))
                translationY = 0
            End If

            Dim r1 As cv.Rect, r2 As cv.Rect
            If translationX = 0 And translationY = 0 Then
                dst2 = src
                taskAlg.camMotionPixels = 0
                taskAlg.camDirection = 0
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

                taskAlg.camMotionPixels = Math.Sqrt(translationX * translationX + translationY * translationY)
                If translationX = 0 Then
                    If translationY < 0 Then taskAlg.camDirection = Math.PI / 4 Else taskAlg.camDirection = Math.PI * 3 / 4
                Else
                    taskAlg.camDirection = Math.Atan(translationY / translationX)
                End If

                If secondOpinion Then
                    dst3.SetTo(0)
                    ' the point cloud contributes one set of camera motion distance and direction.  Now confirm it with feature points
                    feat.Run(src)
                    strOut = "Swarm distance = " + Format(feat.distanceAvg, fmt1) + " when camMotionPixels = " + Format(taskAlg.camMotionPixels, fmt1)
                    If (feat.distanceAvg < taskAlg.camMotionPixels / 2) Or taskAlg.heartBeat Then
                        taskAlg.camMotionPixels = 0
                        src.CopyTo(dst2)
                    End If
                    dst3 = (src - dst2).ToMat.Threshold(options.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
                End If
            End If

            lineGravity = New lpData(taskAlg.lineGravity.p1, taskAlg.lineGravity.p2)
            lineHorizon = New lpData(taskAlg.lineHorizon.p1, taskAlg.lineHorizon.p2)
            SetTrueText(strOut, 3)

            labels(2) = "Translation (X, Y) = (" + CStr(translationX) + ", " + CStr(translationY) + ")" +
                        If(lineHorizon.p1.Y = 0 And lineHorizon.p2.Y = 0, " there is no horizon present", "")
            labels(3) = "Camera direction (radians) = " + Format(taskAlg.camDirection, fmt1) + " with distance = " + Format(taskAlg.camMotionPixels, fmt1)
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
        Dim lineGravity As lpData
        Dim lineHorizon As lpData
        Dim options As New Options_Diff
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            dst3 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Merge with previous image using rotation AND translation of the camera motion - not as good as translation alone."
        End Sub
        Public Sub translateRotateX(x1 As Integer, x2 As Integer)
            rotationX = Math.Atan(Math.Abs((x1 - x2)) / dst2.Height) * 57.2958
            centerX = New cv.Point2f((taskAlg.lineGravity.p1.X + taskAlg.lineGravity.p2.X) / 2, (taskAlg.lineGravity.p1.Y + taskAlg.lineGravity.p2.Y) / 2)
            If x1 >= 0 And x2 > 0 Then
                translationX = If(x1 > x2, x1 - x2, x2 - x1)
                centerX = taskAlg.lineGravity.p2
            ElseIf x1 <= 0 And x2 < 0 Then
                translationX = If(x1 > x2, x1 - x2, x2 - x1)
                centerX = taskAlg.lineGravity.p1
            ElseIf x1 < 0 And x2 > 0 Then
                translationX = 0
            Else
                translationX = 0
                rotationX *= -1
            End If
        End Sub
        Public Sub translateRotateY(y1 As Integer, y2 As Integer)
            rotationY = Math.Atan(Math.Abs((y1 - y2)) / dst2.Width) * 57.2958
            centerY = New cv.Point2f((taskAlg.lineHorizon.p1.X + taskAlg.lineHorizon.p2.X) / 2, (taskAlg.lineHorizon.p1.Y + taskAlg.lineHorizon.p2.Y) / 2)
            If y1 > 0 And y2 > 0 Then
                translationY = If(y1 > y2, y1 - y2, y2 - y1)
                centerY = taskAlg.lineHorizon.p2
            ElseIf y1 < 0 And y2 < 0 Then
                translationY = If(y1 > y2, y1 - y2, y2 - y1)
                centerY = taskAlg.lineHorizon.p1
            ElseIf y1 < 0 And y2 > 0 Then
                translationY = 0
            Else
                translationY = 0
                rotationY *= -1
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If taskAlg.firstPass Then
                lineGravity = taskAlg.lineGravity
                lineHorizon = taskAlg.lineHorizon
            End If

            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Dim x1 = lineGravity.p1.X - taskAlg.lineGravity.p1.X
            Dim x2 = lineGravity.p2.X - taskAlg.lineGravity.p2.X

            Dim y1 = lineHorizon.p1.Y - taskAlg.lineHorizon.p1.Y
            Dim y2 = lineHorizon.p2.Y - taskAlg.lineHorizon.p2.Y

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
                dst3 = (src - dst2).ToMat.Threshold(options.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            Else
                dst2 = src
            End If

            lineGravity = taskAlg.lineGravity
            lineHorizon = taskAlg.lineHorizon

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

            Dim lineHorizon = New lpData(taskAlg.lineHorizon.p1, taskAlg.lineHorizon.p2)

            lineHorizon.p1 = RotatePoint(taskAlg.lineHorizon.p1, rotate.rotateCenter, -rotate.rotateAngle)
            lineHorizon.p2 = RotatePoint(taskAlg.lineHorizon.p2, rotate.rotateCenter, -rotate.rotateAngle)

            vbc.DrawLine(dst2, lineHorizon.p1, lineHorizon.p2, taskAlg.highlight)
            vbc.DrawLine(dst2, taskAlg.lineHorizon.p1, taskAlg.lineHorizon.p2, white)

            Dim y1 = lineHorizon.p1.Y - taskAlg.lineHorizon.p1.Y
            Dim y2 = lineHorizon.p2.Y - taskAlg.lineHorizon.p2.Y
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
                        If ptX.Count >= taskAlg.frameHistoryCount Then Return New cv.Point2f(ptX.Average, ptY.Average)
                    End If
                Next
            Next
            Return New cv.Point
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32F Then dst0 = taskAlg.pcSplit(0) Else dst0 = src

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
                vbc.DrawLine(dst2, vec.p1, vec.p2, 255)
            End If
        End Sub
    End Class




    Public Class XO_Depth_MinMaxToVoronoi : Inherits TaskParent
        Public Sub New()
            taskAlg.kalman = New Kalman_Basics
            ReDim taskAlg.kalman.kInput(taskAlg.gridRects.Count * 4 - 1)

            labels = {"", "", "Red is min distance, blue is max distance", "Voronoi representation of min point (only) for each cell."}
            desc = "Find min and max depth in each roi and create a voronoi representation using the min and max points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.optionsChanged Then ReDim taskAlg.kalman.kInput(taskAlg.gridRects.Count * 4 - 1)

            Parallel.For(0, taskAlg.gridRects.Count,
            Sub(i)
                Dim roi = taskAlg.gridRects(i)
                Dim mm As mmData = GetMinMax(taskAlg.pcSplit(2)(roi), taskAlg.depthMask(roi))
                If mm.minLoc.X < 0 Or mm.minLoc.Y < 0 Then mm.minLoc = New cv.Point2f(0, 0)
                taskAlg.kalman.kInput(i * 4) = mm.minLoc.X
                taskAlg.kalman.kInput(i * 4 + 1) = mm.minLoc.Y
                taskAlg.kalman.kInput(i * 4 + 2) = mm.maxLoc.X
                taskAlg.kalman.kInput(i * 4 + 3) = mm.maxLoc.Y
            End Sub)

            taskAlg.kalman.Run(emptyMat)

            Static minList(taskAlg.gridRects.Count - 1) As cv.Point2f
            Static maxList(taskAlg.gridRects.Count - 1) As cv.Point2f
            If taskAlg.optionsChanged Then
                ReDim minList(taskAlg.gridRects.Count - 1)
                ReDim maxList(taskAlg.gridRects.Count - 1)
            End If
            For Each index In taskAlg.motionBasics.motionList
                Dim rect = taskAlg.gridRects(index)
                Dim ptmin = New cv.Point2f(taskAlg.kalman.kOutput(index * 4) + rect.X,
                                       taskAlg.kalman.kOutput(index * 4 + 1) + rect.Y)
                Dim ptmax = New cv.Point2f(taskAlg.kalman.kOutput(index * 4 + 2) + rect.X,
                                       taskAlg.kalman.kOutput(index * 4 + 3) + rect.Y)
                ptmin = lpData.validatePoint(ptmin)
                ptmax = lpData.validatePoint(ptmax)
                minList(index) = ptmin
                maxList(index) = ptmax
            Next

            dst1 = src.Clone()
            dst1.SetTo(white, taskAlg.gridMask)
            Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, src.Width, src.Height))
            For i = 0 To minList.Count - 1
                Dim ptMin = minList(i)
                subdiv.Insert(ptMin)
                DrawCircle(dst1, ptMin, taskAlg.DotSize, cv.Scalar.Red)
                DrawCircle(dst1, maxList(i), taskAlg.DotSize, cv.Scalar.Blue)
            Next

            If taskAlg.optionsChanged Then dst2 = dst1.Clone Else dst1.CopyTo(dst2, taskAlg.motionMask)

            Dim facets = New cv.Point2f()() {Nothing}
            Dim centers() As cv.Point2f = Nothing
            subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, centers)

            Dim ifacet() As cv.Point
            Dim ifacets = New cv.Point()() {Nothing}

            For i = 0 To facets.Length - 1
                ReDim ifacet(facets(i).Length - 1)
                For j = 0 To facets(i).Length - 1
                    ifacet(j) = New cv.Point(Math.Round(facets(i)(j).X), Math.Round(facets(i)(j).Y))
                Next
                ifacets(0) = ifacet
                dst3.FillConvexPoly(ifacet, taskAlg.scalarColors(i Mod taskAlg.scalarColors.Length), taskAlg.lineType)
                cv.Cv2.Polylines(dst3, ifacets, True, cv.Scalar.Black, taskAlg.lineWidth, taskAlg.lineType, 0)
            Next
        End Sub
    End Class









    Public Class XO_Brick_GrayScaleTest : Inherits TaskParent
        Dim options As New Options_Stdev
        Public Sub New()
            If taskAlg.bricks Is Nothing Then taskAlg.bricks = New Brick_Basics
            labels(3) = "bricks where grayscale stdev and average of the 3 color stdev's"
            desc = "Is the average of the color stdev's the same as the stdev of the grayscale?"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            Dim threshold = options.stdevThreshold

            Dim pt = taskAlg.brickD.rect.TopLeft
            Dim grayMean As cv.Scalar, grayStdev As cv.Scalar
            Dim ColorMean As cv.Scalar, colorStdev As cv.Scalar
            Static saveTrueData As New List(Of TrueText)
            If taskAlg.heartBeat Then
                dst3.SetTo(0)
                dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                Dim count As Integer
                For Each brick In taskAlg.bricks.brickList
                    cv.Cv2.MeanStdDev(dst2(brick.rect), grayMean, grayStdev)
                    cv.Cv2.MeanStdDev(taskAlg.color(brick.rect), ColorMean, colorStdev)
                    Dim nextColorStdev = (colorStdev(0) + colorStdev(1) + colorStdev(2)) / 3
                    Dim diff = Math.Abs(grayStdev(0) - nextColorStdev)
                    If diff > threshold Then
                        dst2.Rectangle(brick.rect, 255, taskAlg.lineWidth)
                        SetTrueText(Format(grayStdev(0), fmt1) + " " + Format(colorStdev, fmt1), brick.rect.TopLeft, 2)
                        dst3.Rectangle(brick.rect, taskAlg.highlight, taskAlg.lineWidth)
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
            If taskAlg.heartBeat OrElse lastPoints.Count < 10 Then
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
                    DrawCircle(dst2, p1, taskAlg.DotSize, New cv.Scalar(0, 0, 255))
                End If
            Next
            lastPoints = New List(Of cv.Point2f)(stablePoints)
            If taskAlg.midHeartBeat Then
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
                    Dim p1 = lpData.validatePoint(New cv.Point(CInt(v(0) + subsetRect.X), CInt(v(1) + subsetRect.Y)))
                    Dim p2 = lpData.validatePoint(New cv.Point(CInt(v(2) + subsetRect.X), CInt(v(3) + subsetRect.Y)))
                    Dim lp = New lpData(p1, p2)
                    lpList.Add(lp)
                    ptList.Add(New cv.Point(CInt(lp.p1.X), CInt(lp.p1.Y)))
                End If
            Next

            dst2.SetTo(0)
            For Each lp In lpList
                dst2.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth, taskAlg.lineType)
            Next
            labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
        End Sub
        Public Sub Close()
            ld.Dispose()
        End Sub
    End Class







    Public Class XO_Line_LeftRight : Inherits TaskParent
        Dim lineCore As New XO_Line_Core
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Show lines in both the right and left images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = taskAlg.lines.dst2.Clone
            labels(2) = "Left view" + taskAlg.lines.labels(2)

            dst1 = taskAlg.rightView
            lineCore.Run(taskAlg.rightView)
            dst3 = lineCore.dst2.Clone
            labels(3) = "Right View: " + lineCore.labels(2)

            If standalone Then
                If taskAlg.gOptions.DebugCheckBox.Checked Then
                    dst2.SetTo(0, taskAlg.noDepthMask)
                    dst3.SetTo(0, taskAlg.noDepthMask)
                End If
            Else
                If taskAlg.toggleOn Then
                    dst2.SetTo(0, taskAlg.noDepthMask)
                    dst3.SetTo(0, taskAlg.noDepthMask)
                End If
            End If
        End Sub
    End Class




    Public Class XO_Line_Matching : Inherits TaskParent
        Public options As New Options_Line
        Dim lpMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
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
            options.Run()
            dst2 = src.Clone

            If taskAlg.firstPass Then OptionParent.FindSlider("Min Line Length").Value = 30

            Dim tolerance = 0.1
            Dim newSet As New List(Of lpData)
            Dim removeList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
            Dim addList As New List(Of lpData)
            Dim combineCount As Integer
            For i = 0 To taskAlg.lines.lpList.Count - 1
                Dim lp = taskAlg.lines.lpList(i)
                Dim lpRemove As Boolean = False
                For j = 0 To 1
                    Dim pt = Choose(j + 1, lp.p1, lp.p2)
                    Dim val = lpMap.Get(Of Integer)(pt.Y, pt.X)
                    If val = 0 Then Continue For
                    Dim mp = lpList(val - 1)
                    If Math.Abs(mp.slope - lp.slope) < tolerance Then
                        Dim lpNew = combine2Lines(lp, mp)
                        If lpNew IsNot Nothing Then
                            addList.Add(lpNew)
                            vbc.DrawLine(dst2, lpNew.p1, lpNew.p2, taskAlg.highlight)
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
                taskAlg.lines.lpList.RemoveAt(removeList.ElementAt(i).Value)
            Next

            For Each lp In addList
                taskAlg.lines.lpList.Add(lp)
            Next
            lpList = New List(Of lpData)(taskAlg.lines.lpList)
            lpMap.SetTo(0)
            For i = 0 To lpList.Count - 1
                Dim lp = lpList(i)
                If lp.length > options.minLength Then lpMap.Line(lp.p1, lp.p2, i + 1, 2, cv.LineTypes.Link8)
            Next
            lpMap.ConvertTo(dst3, cv.MatType.CV_8U)
            dst3 = dst3.Threshold(0, cv.Scalar.White, cv.ThresholdTypes.Binary)
            If taskAlg.heartBeat Then
                labels(2) = CStr(taskAlg.lines.lpList.Count) + " lines were input and " + CStr(combineCount) +
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
            dst2 = taskAlg.lines.dst2
            labels(2) = taskAlg.lines.labels(2)

            dst3.SetTo(0)
            For i = 0 To 9
                Dim lp = taskAlg.lines.lpList(i)
                dst3.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth, taskAlg.lineType)
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
            If standaloneTest() And taskAlg.heartBeat Then
                Dim tc As New tCell
                tcells.Clear()
                For i = 0 To 2 - 1
                    tc.center = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                    tcells.Add(tc)
                Next
            End If
            If tcells.Count < 2 Then Exit Sub

            If myCurrentFrame < taskAlg.frameCount Then
                canny.Run(src)
                blur.Run(canny.dst2)
                myCurrentFrame = taskAlg.frameCount
            End If
            dst1.SetTo(0)
            Dim p1 = tcells(0).center
            Dim p2 = tcells(1).center
            vbc.DrawLine(dst1, p1, p2, 255)

            dst3.SetTo(0)
            blur.dst2.Threshold(1, 255, cv.ThresholdTypes.Binary).CopyTo(dst3, dst1)
            distance = p1.DistanceTo(p2)
            maskCount = dst3.CountNonZero

            For Each tc In tcells
                'dst2.Rectangle(tc.rect, myhighlight)
                'dst2.Rectangle(tc.searchRect, white, taskAlg.lineWidth)
                SetTrueText(tc.strOut, New cv.Point(tc.rect.X, tc.rect.Y))
            Next

            strOut = "Mask count = " + CStr(maskCount) + ", Expected count = " + CStr(distance) + " or " + Format(maskCount / distance, "0%") + vbCrLf
            vbc.DrawLine(dst2, p1, p2, taskAlg.highlight)

            strOut += "Color changes when correlation falls below threshold and new line is detected." + vbCrLf +
                      "Correlation coefficient is shown with the depth in meters."
            SetTrueText(strOut, 3)
        End Sub
    End Class






    Public Class XO_Line_Cells : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Dim lines As New XO_Line_RawSorted
        Public Sub New()
            desc = "Identify all lines in the RedList_Basics cell boundaries"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            lines.Run(dst2.Clone)
            dst3 = lines.dst2
            lpList = New List(Of lpData)(lines.lpList)
            labels(3) = "Number of lines identified: " + CStr(lpList.Count)
        End Sub
    End Class








    Public Class XO_Line_Canny : Inherits TaskParent
        Dim canny As New Edge_Basics
        Public lpList As New List(Of lpData)
        Dim lines As New XO_Line_RawSorted
        Public Sub New()
            labels(3) = "Input to Line_Basics"
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








    Public Class XO_Line_RegionsVB : Inherits TaskParent
        Dim lines As New XO_Line_TimeView
        Dim reduction As New Reduction_Basics
        Const lineMatch = 254
        Public Sub New()
            OptionParent.findRadio("Use Bitwise Reduction").Checked = True

            If OptionParent.FindFrm(traceName + " CheckBoxes") Is Nothing Then
                check.Setup(traceName)
                check.addCheckBox("Show intermediate vertical step taskAlg.results..")
                check.addCheckBox("Run horizontal without vertical step")
            End If

            desc = "Use the reduction values between lines to identify regions."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static noVertCheck = OptionParent.FindCheckBox("Run horizontal without vertical step")
            Static verticalCheck = OptionParent.FindCheckBox("Show intermediate vertical step taskAlg.results..")
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
            If standaloneTest() And taskAlg.heartBeat Then
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
                vbc.DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
                vbc.DrawLine(dst2, pt, nearPoint, white)
                DrawCircle(dst2, pt, taskAlg.DotSize, white)
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
            If taskAlg.optionsChanged Then frameList.Clear()
            Dim nextMpList = New List(Of lpData)(taskAlg.lines.lpList)
            frameList.Add(nextMpList)

            dst2 = src
            dst3.SetTo(0)
            lpList.Clear()
            Dim lineTotal As Integer
            For i = 0 To frameList.Count - 1
                lineTotal += frameList(i).Count
                For Each lp In frameList(i)
                    vbc.DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
                    vbc.DrawLine(dst3, lp.p1, lp.p2, white)
                    lpList.Add(lp)
                Next
            Next

            If frameList.Count >= taskAlg.frameHistoryCount Then frameList.RemoveAt(0)
            pixelcount = dst3.CountNonZero
            labels(3) = "There were " + CStr(lineTotal) + " lines detected using " + Format(pixelcount / 1000, "#.0") + "k pixels"
        End Sub
    End Class







    Public Class XO_Line_ColorClass : Inherits TaskParent
        Dim color8U As New Color8U_Basics
        Dim lines As New XO_Line_RawSorted
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"", "", "Lines for the current color class", "Color Class input"}
            desc = "Review lines in all the different color classes"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            color8U.Run(src)
            dst1 = color8U.dst2

            lines.Run(dst1 * 255 / color8U.classCount)
            dst2 = lines.dst2
            dst3 = lines.dst2

            labels(1) = "Input to Line_Basics"
            labels(2) = "Lines found in the " + color8U.classifier.traceName + " output"
        End Sub
    End Class





    Public Class XO_Line_FromContours : Inherits TaskParent
        Dim reduction As New Reduction_Basics
        Dim contours As New XO_Contour_Gray
        Dim lines As New XO_Line_RawSorted
        Public Sub New()
            taskAlg.gOptions.highlight.SelectedIndex = 3
            desc = "Find the lines in the contours."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            reduction.Run(src)
            contours.Run(reduction.dst2)
            dst2 = contours.dst2.Clone
            lines.Run(dst2)

            dst3.SetTo(0)
            For Each lp In lines.lpList
                vbc.DrawLine(dst3, lp.p1, lp.p2, white)
            Next
        End Sub
    End Class








    Public Class XO_Line_ViewSide : Inherits TaskParent
        Public autoY As New XO_OpAuto_YRange
        Dim histSide As New Projection_HistSide
        Dim lines As New XO_Line_RawSorted
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
            taskAlg.kalman = New Kalman_Basics
            taskAlg.kalman.kOutput = {0, 0, 0, 0}

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
                taskAlg.kalman.kInput = {k1.X, k1.Y, k2.X, k2.Y}
                taskAlg.kalman.Run(emptyMat)
                p1 = New cv.Point(taskAlg.kalman.kOutput(0), taskAlg.kalman.kOutput(1))
                p2 = New cv.Point(taskAlg.kalman.kOutput(2), taskAlg.kalman.kOutput(3))
            End If
            frameCount += 1
            vbc.DrawLine(dst2, p1, p2, gradientColors(frameCount Mod gradientColors.Count))
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
            dst2 = taskAlg.lines.dst2

            Dim lineList = New List(Of cv.Rect)
            If taskAlg.optionsChanged Then dst3.SetTo(0)
            dst3.SetTo(0, taskAlg.motionMask)
            p1List.Clear()
            p2List.Clear()
            z1List.Clear()
            z2List.Clear()
            For Each lp In taskAlg.lines.lpList
                Dim rect = findRectFromLine(lp)
                Dim mask = New cv.Mat(New cv.Size(rect.Width, rect.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
                mask.Line(New cv.Point(lp.p1.X - rect.X, lp.p1.Y - rect.Y),
                          New cv.Point(lp.p2.X - rect.X, lp.p2.Y - rect.Y), 255, taskAlg.lineWidth, cv.LineTypes.Link4)
                Dim mean = taskAlg.pointCloud(rect).Mean(mask)

                If mean <> New cv.Scalar Then
                    Dim mmX = GetMinMax(taskAlg.pcSplit(0)(rect), mask)
                    Dim mmY = GetMinMax(taskAlg.pcSplit(1)(rect), mask)
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
                        vbc.DrawLine(dst3, lp.p1, lp.p2, cv.Scalar.Yellow)
                        p1List.Add(lp.p1)
                        p2List.Add(lp.p2)
                        z1List.Add(taskAlg.pointCloud.Get(Of cv.Point3f)(lp.p1.Y, lp.p1.X))
                        z2List.Add(taskAlg.pointCloud.Get(Of cv.Point3f)(lp.p2.Y, lp.p2.X))
                    End If
                End If
            Next
        End Sub
    End Class










    Public Class XO_Line_Core : Inherits TaskParent
        Dim lines As New XO_Line_Core
        Public lpList As New List(Of lpData)
        Public lpRectMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
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
                lpRectMap.SetTo(0, Not taskAlg.motionMask)
                cv.Cv2.CalcHist({lpRectMap}, {0}, emptyMat, histogram, 1, {lastList.Count}, New cv.Rangef() {New cv.Rangef(0, lastList.Count)})
                Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

                For i = 1 To histarray.Count - 1
                    If histarray(i) = 0 Then lpList.Add(lastList(i))
                Next
            End If

            lines.Run(src.Clone)
            ReDim histarray(lines.lpList.Count - 1)

            Dim tmp = lines.lpRectMap.Clone
            tmp.SetTo(0, Not taskAlg.motionMask)
            cv.Cv2.CalcHist({tmp}, {0}, emptyMat, histogram, 1, {lines.lpList.Count}, New cv.Rangef() {New cv.Rangef(0, lines.lpList.Count)})
            Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

            For i = 1 To histarray.Count - 1
                If histarray(i) > 0 Then lpList.Add(lines.lpList(i))
            Next

            dst2.SetTo(0)
            lpRectMap.SetTo(0)
            For i = 0 To lpList.Count - 1
                Dim lp = lpList(i)
                dst2.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth, taskAlg.lineType)
                lpRectMap.Line(lp.p1, lp.p2, i, taskAlg.lineWidth, taskAlg.lineType)
            Next

            If taskAlg.heartBeat Then
                labels(2) = CStr(lines.lpList.Count) + " lines found in XO_Line_RawSorted in the current image with " +
                            CStr(lpList.Count) + " after filtering with the motion mask."
            End If
        End Sub
    End Class





    Public Class XO_Line_Basics : Inherits TaskParent
        Public lpRectMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        Public lpList As New List(Of lpData)
        Dim lineCore As New XO_Line_Core
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Collect lines across frames using the motion mask."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            lineCore.Run(src)

            lpRectMap.SetTo(0)
            dst2 = src
            dst3.SetTo(0)
            dst2.SetTo(cv.Scalar.White, lineCore.dst2)
            For Each lp In lineCore.lpList
                lpRectMap.Line(lp.p1, lp.p2, lp.index, taskAlg.lineWidth + 1, cv.LineTypes.Link8)
                dst3.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth, taskAlg.lineType)
            Next

            lpList = New List(Of lpData)(lineCore.lpList)
            taskAlg.lines.lpList = New List(Of lpData)(lineCore.lpList)
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
            Dim w = taskAlg.lineWidth + 5
            lpList.Clear()
            For Each lp In taskAlg.lines.lpList
                If Math.Abs(lp.slope) < 0.1 Then
                    lp = findEdgePoints(lp)
                    dst2.Line(lp.p1, lp.p2, 255, w, taskAlg.lineType)
                    lpList.Add(lp)
                End If
            Next

            Dim histogram = line.autoY.histogram
            histogram.SetTo(0, Not dst2)
            cv.Cv2.CalcBackProject({taskAlg.pointCloud}, taskAlg.channelsSide, histogram, dst1, taskAlg.rangesSide)
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
            Dim mask = taskAlg.pcSplit(1).InRange(min, max).ConvertScaleAbs

            Dim mean As cv.Scalar, stdev As cv.Scalar
            cv.Cv2.MeanStdDev(taskAlg.pointCloud, mean, stdev, mask)

            strOut += "The " + maskLabel + " mask has Y mean and stdev are:" + vbCrLf
            strOut += maskLabel + " Y Mean = " + Format(mean(1), fmt3) + vbCrLf
            strOut += maskLabel + " Y Stdev = " + Format(stdev(1), fmt3) + vbCrLf + vbCrLf

            If Math.Abs(mean(1)) > taskAlg.yRange / 4 Then dst1 = mask Or dst1
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim pad As Single = 0.05 ' pad the estimate by X cm's

            dst2 = src.Clone
            bpLine.Run(src)

            If bpLine.lpList.Count > 0 Then
                strOut = "Y range = " + Format(taskAlg.yRange, fmt3) + vbCrLf + vbCrLf
                If taskAlg.heartBeat Then yList.Clear()
                If taskAlg.heartBeat Then dst1.SetTo(0)
                Dim h = dst2.Height / 2
                For Each lp In bpLine.lpList
                    Dim nextY = taskAlg.yRange * (lp.p1.Y - h) / h
                    If Math.Abs(nextY) > taskAlg.yRange / 4 Then yList.Add(nextY)
                Next

                If yList.Count > 0 Then
                    If yList.Max > 0 Then rebuildMask("floor", yList.Max - pad, taskAlg.yRange)
                    If yList.Min < 0 Then rebuildMask("ceiling", -taskAlg.yRange, yList.Min + pad)
                End If

                dst2.SetTo(white, dst1)
            End If
            SetTrueText(strOut, 3)
        End Sub
    End Class







    Public Class XO_Hough_Sudoku1 : Inherits TaskParent
        Dim lines As New XO_Line_RawSorted
        Public Sub New()
            desc = "FastLineDetect version for finding lines in the Sudoku input."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = cv.Cv2.ImRead(taskAlg.homeDir + "opencv/Samples/Data/sudoku.png").Resize(dst2.Size)
            lines.Run(dst3.Clone)
            dst2 = lines.dst2
            labels(2) = lines.labels(2)
            For Each lp In lines.lpList
                lp = findEdgePoints(lp)
                dst3.Line(lp.p1, lp.p2, cv.Scalar.Red, taskAlg.lineWidth, taskAlg.lineType)
            Next
        End Sub
    End Class




    Public Class XO_Line_InterceptsUI : Inherits TaskParent
        Dim lines As New XO_Line_Intercepts
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
            dst3.Line(New cv.Point(0, 0), center, blue, taskAlg.lineWidth, cv.LineTypes.Link4)
            dst3.Line(New cv.Point(dst2.Width, 0), center, red, taskAlg.lineWidth, cv.LineTypes.Link4)
            dst3.Line(New cv.Point(0, dst2.Height), center, blue, taskAlg.lineWidth, cv.LineTypes.Link4)
            dst3.Line(New cv.Point(dst2.Width, dst2.Height), center, yellow, taskAlg.lineWidth, cv.LineTypes.Link4)

            Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, cv.Scalar.All(0))
            Dim pt = New cv.Point(center.X, center.Y - 30)
            cv.Cv2.FloodFill(dst3, mask, pt, red, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

            pt = New cv.Point(center.X, center.Y + 30)
            cv.Cv2.FloodFill(dst3, mask, pt, green, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

            pt = New cv.Point(center.X - 30, center.Y)
            cv.Cv2.FloodFill(dst3, mask, pt, blue, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

            pt = New cv.Point(center.X + 30, center.Y)
            cv.Cv2.FloodFill(dst3, mask, pt, yellow, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))
            Dim color = dst3.Get(Of cv.Vec3b)(taskAlg.mouseMovePoint.Y, taskAlg.mouseMovePoint.X)

            Dim p1 = taskAlg.mouseMovePoint
            If p1.X = center.X Then
                If p1.Y <= center.Y Then p2 = New cv.Point(dst3.Width / 2, 0) Else p2 = New cv.Point(dst3.Width, dst3.Height)
            Else
                Dim m = (center.Y - p1.Y) / (center.X - p1.X)
                Dim b = p1.Y - p1.X * m

                If color(0) = 0 Then p2 = New cv.Point(-b / m, 0) ' red zone
                If color(0) = 1 Then p2 = New cv.Point((dst3.Height - b) / m, dst3.Height) ' green
                If color(0) = 2 Then p2 = New cv.Point(dst3.Width, dst3.Width * m + b) ' yellow
                If color(0) = 254 Then p2 = New cv.Point(0, b) ' blue
                vbc.DrawLine(dst3, center, p2, cv.Scalar.Black)
            End If
            DrawCircle(dst3, center, taskAlg.DotSize, white)
            If color(0) = 0 Then redRadio.Checked = True
            If color(0) = 1 Then greenRadio.Checked = True
            If color(0) = 2 Then yellowRadio.Checked = True
            If color(0) = 254 Then blueRadio.Checked = True

            For Each inter In lines.intercept
                Select Case lines.options.selectedIntercept
                    Case 0
                        dst3.Line(New cv.Point(inter.Key, 0), New cv.Point(inter.Key, 10), white,
                             taskAlg.lineWidth)
                    Case 1
                        dst3.Line(New cv.Point(inter.Key, dst3.Height), New cv.Point(inter.Key, dst3.Height - 10),
                             white, taskAlg.lineWidth)
                    Case 2
                        dst3.Line(New cv.Point(0, inter.Key), New cv.Point(10, inter.Key), white,
                             taskAlg.lineWidth)
                    Case 3
                        dst3.Line(New cv.Point(dst3.Width, inter.Key), New cv.Point(dst3.Width - 10, inter.Key),
                             white, taskAlg.lineWidth)
                End Select
            Next
            dst2 = lines.dst2
        End Sub
    End Class






    Public Class XO_Diff_Heartbeat : Inherits TaskParent
        Public cumulativePixels As Integer
        Dim options As New Options_Diff
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            labels = {"", "", "Unstable mask", "Pixel difference"}
            desc = "Diff an image with one from the last heartbeat."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If taskAlg.heartBeat Then
                dst1 = taskAlg.gray.Clone
                dst2.SetTo(0)
            End If

            cv.Cv2.Absdiff(taskAlg.gray, dst1, dst3)
            cumulativePixels = dst3.CountNonZero
            dst2 = dst2 Or dst3.Threshold(options.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
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
            dst.Line(pt1, pt2, cv.Scalar.Red, taskAlg.lineWidth + 2, taskAlg.lineType, 0)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If Not taskAlg.heartBeat Then Exit Sub
            hlines.Run(src)
            dst3 = hlines.dst3
            Dim mask = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
            dst3 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            src.CopyTo(dst2)

            Dim lines As New List(Of cv.Line3D)
            Dim nullLine = New cv.Line3D(0, 0, 0, 0, 0, 0)
            Parallel.ForEach(taskAlg.gridRects,
        Sub(roi)
            Dim depth = taskAlg.pcSplit(2)(roi)
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
                Dim mean = taskAlg.depthRGB(roi).Mean()
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
            For i = 0 To taskAlg.gridRects.Count - 1
                houghShowLines3D(dst2(taskAlg.gridRects(i)), lines.ElementAt(i))
            Next
        End Sub
    End Class



    Public Class XO_Brick_Basics : Inherits TaskParent
        Public options As New Options_GridCells
        Public thresholdRangeZ As Single
        Public instantUpdate As Boolean = True
        Dim lastCorrelation() As Single
        ' Public quad As New XO_Quad_Basics
        Dim LRMeanSub As New MeanSubtraction_LeftRight
        Public Sub New()
            desc = "Create the grid of bricks that reduce depth volatility"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            If taskAlg.optionsChanged Then
                ReDim lastCorrelation(taskAlg.gridRects.Count - 1)
            End If

            LRMeanSub.Run(src)

            Dim stdev As cv.Scalar, mean As cv.Scalar
            Dim correlationMat As New cv.Mat

            taskAlg.bricks.brickList.Clear()
            For i = 0 To taskAlg.gridRects.Count - 1
                Dim brick As New brickData
                brick.rect = taskAlg.gridRects(i)
                brick.rect = brick.rect
                brick.lRect = brick.rect ' for some cameras the color image and the left image are the same but not all, i.e. Intel Realsense.
                brick.center = New cv.Point(brick.rect.X + brick.rect.Width / 2, brick.rect.Y + brick.rect.Height / 2)
                If taskAlg.depthMask(brick.rect).CountNonZero Then
                    cv.Cv2.MeanStdDev(taskAlg.pcSplit(2)(brick.rect), mean, stdev, taskAlg.depthMask(brick.rect))
                    brick.depth = mean(0)
                End If

                If brick.depth = 0 Then
                    brick.correlation = 0
                    brick.rRect = emptyRect
                Else
                    brick.mm = GetMinMax(taskAlg.pcSplit(2)(brick.rect), taskAlg.depthMask(brick.rect))
                    If taskAlg.rgbLeftAligned Then
                        brick.lRect = brick.rect
                        brick.rRect = brick.lRect
                        brick.rRect.X -= taskAlg.calibData.baseline * taskAlg.calibData.rgbIntrinsics.fx / brick.depth
                        brick.rRect = ValidateRect(brick.rRect)
                        cv.Cv2.MatchTemplate(LRMeanSub.dst2(brick.lRect), LRMeanSub.dst3(brick.rRect), correlationMat,
                                                     cv.TemplateMatchModes.CCoeffNormed)

                        brick.correlation = correlationMat.Get(Of Single)(0, 0)
                    Else
                        Dim irPt = Intrinsics_Basics.translate_LeftToRight(taskAlg.pointCloud.Get(Of cv.Point3f)(brick.rect.Y, brick.rect.X))
                        If irPt.X < 0 Or (irPt.X = 0 And irPt.Y = 0 And i > 0) Or (irPt.X >= dst2.Width Or irPt.Y >= dst2.Height) Then
                            brick.depth = 0 ' off the grid.
                            brick.lRect = emptyRect
                            brick.rRect = emptyRect
                        Else
                            brick.lRect = New cv.Rect(irPt.X, irPt.Y, brick.rect.Width, brick.rect.Height)
                            brick.lRect = ValidateRect(brick.lRect)

                            brick.rRect = brick.lRect
                            brick.rRect.X -= taskAlg.calibData.baseline * taskAlg.calibData.leftIntrinsics.fx / brick.depth
                            brick.rRect = ValidateRect(brick.rRect)
                            cv.Cv2.MatchTemplate(LRMeanSub.dst2(brick.lRect), LRMeanSub.dst3(brick.rRect), correlationMat,
                                                      cv.TemplateMatchModes.CCoeffNormed)

                            brick.correlation = correlationMat.Get(Of Single)(0, 0)
                        End If
                    End If
                End If

                lastCorrelation(i) = brick.correlation
                brick.index = taskAlg.bricks.brickList.Count
                taskAlg.gridMap(brick.rect).SetTo(i)
                taskAlg.bricks.brickList.Add(brick)
            Next

            ' quad.Run(src)

            If taskAlg.heartBeat Then labels(2) = CStr(taskAlg.bricks.brickList.Count) + " bricks have the useful depth values."
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
                    Dim vec = taskAlg.pointCloud.Get(Of cv.Vec3f)(y, x)
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
            desc = "Reduce the point cloud to a manageable number points in 3D"
        End Sub
        Public Function findHorizontalPoints(ByRef xyList As List(Of List(Of cv.Point))) As List(Of List(Of cv.Point3f))
            Dim ptlist As New List(Of List(Of cv.Point3f))
            Dim lastVec = New cv.Point3f
            For y = 0 To taskAlg.pointCloud.Height - 1 Step taskAlg.gridRects(0).Height - 1
                Dim vecList As New List(Of cv.Point3f)
                Dim xyVec As New List(Of cv.Point)
                For x = 0 To taskAlg.pointCloud.Width - 1 Step taskAlg.gridRects(0).Width - 1
                    Dim vec = taskAlg.pointCloud.Get(Of cv.Point3f)(y, x)
                    Dim jumpZ As Boolean = False
                    If vec.Z > 0 Then
                        If (Math.Abs(lastVec.Z - vec.Z) < options.deltaThreshold And lastVec.X < vec.X) Or lastVec.Z = 0 Then
                            actualCount += 1
                            DrawCircle(dst2, New cv.Point(x, y), taskAlg.DotSize, white)
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
            For x = 0 To taskAlg.pointCloud.Width - 1 Step taskAlg.gridRects(0).Width - 1
                Dim vecList As New List(Of cv.Point3f)
                Dim xyVec As New List(Of cv.Point)
                For y = 0 To taskAlg.pointCloud.Height - 1 Step taskAlg.gridRects(0).Height - 1
                    Dim vec = taskAlg.pointCloud.Get(Of cv.Point3f)(y, x)
                    Dim jumpZ As Boolean = False
                    If vec.Z > 0 Then
                        If (Math.Abs(lastVec.Z - vec.Z) < options.deltaThreshold And lastVec.Y < vec.Y) Or lastVec.Z = 0 Then
                            actualCount += 1
                            DrawCircle(dst2, New cv.Point(x, y), taskAlg.DotSize, white)
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
                vbc.DrawLine(dst2, p1, p2, white)
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








    Public Class XO_PointCloud_NeighborV : Inherits TaskParent
        Dim options As New Options_Neighbors
        Public Sub New()
            desc = "Show where vertical neighbor depth values are within taskAlg.depthDiffMeters"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            If src.Type <> cv.MatType.CV_32F Then src = taskAlg.pcSplit(2)

            Dim tmp32f = New cv.Mat(dst2.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
            Dim r1 = New cv.Rect(options.pixels, 0, dst2.Width - options.pixels, dst2.Height)
            Dim r2 = New cv.Rect(0, 0, dst2.Width - options.pixels, dst2.Height)
            cv.Cv2.Absdiff(src(r1), src(r2), tmp32f(r1))
            tmp32f = tmp32f.Threshold(options.threshold, 255, cv.ThresholdTypes.BinaryInv)
            dst2 = tmp32f.ConvertScaleAbs(255)
            dst2.SetTo(0, taskAlg.noDepthMask)
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
            Dim pcSplit = {taskAlg.pcSplit(0).ConvertScaleAbs(255), taskAlg.pcSplit(1).ConvertScaleAbs(255), taskAlg.pcSplit(2).ConvertScaleAbs(255)}
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
            If taskAlg.firstPass Then ReDim depthBytes(taskAlg.pcSplit(2).Total * taskAlg.pcSplit(2).ElemSize - 1)

            Marshal.Copy(taskAlg.pcSplit(2).Data, depthBytes, 0, depthBytes.Length)
            Dim handleDepth = GCHandle.Alloc(depthBytes, GCHandleType.Pinned)

            Dim imagePtr = SimpleProjectionRun(cPtr, handleDepth.AddrOfPinnedObject, 0, taskAlg.MaxZmeters, taskAlg.pcSplit(2).Height, taskAlg.pcSplit(2).Width)

            dst2 = cv.Mat.FromPixelData(taskAlg.pcSplit(2).Rows, taskAlg.pcSplit(2).Cols, cv.MatType.CV_8U, imagePtr).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3 = cv.Mat.FromPixelData(taskAlg.pcSplit(2).Rows, taskAlg.pcSplit(2).Cols, cv.MatType.CV_8U, SimpleProjectionSide(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)

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
            Dim range As Single = taskAlg.MaxZmeters

            ' this VB.Net version is much slower than the optimized C++ version below.
            dst2 = src.EmptyClone.SetTo(white)
            dst3 = dst2.Clone()
            Dim black = New cv.Vec3b(0, 0, 0)
            Parallel.ForEach(taskAlg.gridRects,
             Sub(roi)
                 For y = roi.Y To roi.Y + roi.Height - 1
                     For x = roi.X To roi.X + roi.Width - 1
                         Dim m = taskAlg.depthMask.Get(Of Byte)(y, x)
                         If m > 0 Then
                             Dim depth = taskAlg.pcSplit(2).Get(Of Single)(y, x)
                             Dim dy = src.Height * depth \ range
                             If dy < src.Height And dy > 0 Then dst2.Set(Of cv.Vec3b)(src.Height - dy, x, black)
                             Dim dx = src.Width * depth \ range
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
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Reduce the point cloud to a manageable number points in 3D representing the averages of X, Y, and Z in that roi."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.optionsChanged Then pcPoints = New cv.Mat(taskAlg.bricksPerCol, taskAlg.bricksPerRow, cv.MatType.CV_32FC3, cv.Scalar.All(0))

            dst2.SetTo(0)
            actualCount = 0
            Dim lastMeanZ As Single
            For y = 0 To taskAlg.bricksPerCol - 1
                For x = 0 To taskAlg.bricksPerRow - 1
                    Dim roi = taskAlg.gridRects(y * taskAlg.bricksPerRow + x)
                    Dim mean = taskAlg.pointCloud(roi).Mean(taskAlg.depthMask(roi))
                    If Single.IsNaN(mean(0)) Then Continue For
                    If Single.IsNaN(mean(1)) Then Continue For
                    If Single.IsInfinity(mean(2)) Then Continue For
                    Dim depthPresent = taskAlg.depthMask(roi).CountNonZero > roi.Width * roi.Height / 2
                    If (depthPresent And mean(2) > 0 And Math.Abs(lastMeanZ - mean(2)) < 0.2 And
                    mean(2) < taskAlg.MaxZmeters) Or (lastMeanZ = 0 And mean(2) > 0) Then

                        pcPoints.Set(Of cv.Point3f)(y, x, New cv.Point3f(mean(0), mean(1), mean(2)))
                        actualCount += 1
                        DrawCircle(dst2, New cv.Point(roi.X, roi.Y), taskAlg.DotSize * Math.Max(mean(2), 1), white)
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
            desc = "Reduce the point cloud to a manageable number points in 3D using the mean value"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim rw = taskAlg.gridRects(0).Width / 2, rh = taskAlg.gridRects(0).Height / 2
            Dim red32 = New cv.Point3f(0, 0, 1), blue32 = New cv.Point3f(1, 0, 0), white32 = New cv.Point3f(1, 1, 1)
            Dim red = cv.Scalar.Red, blue = cv.Scalar.Blue

            pcPoints.Clear()
            dst2 = src
            For Each roi In taskAlg.gridRects
                Dim pt = New cv.Point(roi.X + rw, roi.Y + rh)
                Dim mean = taskAlg.pointCloud(roi).Mean(taskAlg.depthMask(roi))

                If mean(2) > 0 Then
                    pcPoints.Add(Choose(pt.Y Mod 3 + 1, red32, blue32, white32))
                    pcPoints.Add(New cv.Point3f(mean(0), mean(1), mean(2)))
                    DrawCircle(dst2, pt, taskAlg.DotSize, Choose(CInt(pt.Y) Mod 3 + 1, red, blue, cv.Scalar.White))
                End If
            Next
            labels(2) = "PointCloud Point Points found = " + CStr(pcPoints.Count / 2)
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
            mats.mat(0) = PaletteFull(dst1)

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
            mats.mat(2) = PaletteFull(dst1)

            mats.mat(3) = ShowAddweighted(src, mats.mat(2), labels(3))
            If taskAlg.heartBeat Then labels(2) = CStr(indexV + indexH) + " regions were found that were connected in depth."

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
            taskAlg.gOptions.setHistogramBins(255)
            taskAlg.gOptions.GridSlider.Value = 8
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
            If taskAlg.bricks Is Nothing Then taskAlg.bricks = New Brick_Basics
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
                Dim brick1 = taskAlg.bricks.brickList(tup.Item1)
                Dim brick2 = taskAlg.bricks.brickList(tup.Item2)

                Dim w = brick2.rect.BottomRight.X - brick1.rect.X
                Dim h = brick1.rect.Height

                Dim r = New cv.Rect(brick1.rect.X + 1, brick1.rect.Y, w - 1, h)

                hRects.Add(r)
                dst2(r).SetTo(255)

                index += 1
                dst3(r).SetTo(taskAlg.scalarColors(index Mod 256))
            Next
        End Sub
    End Class






    Public Class XO_Region_RectsV : Inherits TaskParent
        Public vRects As New List(Of cv.Rect)
        Dim connect As New Region_Core
        Public Sub New()
            If taskAlg.bricks Is Nothing Then taskAlg.bricks = New Brick_Basics
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
                Dim brick1 = taskAlg.bricks.brickList(tup.Item1)
                Dim brick2 = taskAlg.bricks.brickList(tup.Item2)

                Dim w = brick1.rect.Width
                Dim h = brick2.rect.BottomRight.Y - brick1.rect.Y

                Dim r = New cv.Rect(brick1.rect.X, brick1.rect.Y + 1, w, h - 1)
                vRects.Add(r)
                dst2(r).SetTo(255)

                index += 1
                dst3(r).SetTo(taskAlg.scalarColors(index Mod 256))
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

            dst3 = runRedList(src, labels(3))
            For Each rc In taskAlg.redList.oldrclist
                Dim index = connect.dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                dst2(rc.rect).SetTo(taskAlg.scalarColors(index), rc.mask)
            Next
        End Sub
    End Class





    Public Class XO_Region_Gaps : Inherits TaskParent
        Dim connect As New Region_Core
        Public Sub New()
            If taskAlg.bricks Is Nothing Then taskAlg.bricks = New Brick_Basics
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
                    Dim brick = taskAlg.bricks.brickList(tup.Item1)
                    dst2(brick.rect).SetTo(0)
                End If
            Next

            For Each tup In connect.vTuples
                Dim brick1 = taskAlg.bricks.brickList(tup.Item1)
                Dim brick2 = taskAlg.bricks.brickList(tup.Item2)
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
            If taskAlg.bricks Is Nothing Then taskAlg.bricks = New Brick_Basics
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Build a feature coordinate system (FCS) based on lines, not features."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lastMap = taskAlg.fpMap.Clone
            Dim lastCount = taskAlg.lines.lpList.Count

            dst2 = taskAlg.lines.dst2

            delaunay.inputPoints.Clear()

            For Each lp In taskAlg.lines.lpList
                Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
                delaunay.inputPoints.Add(center)
            Next

            delaunay.Run(src)

            taskAlg.fpMap.SetTo(0)
            dst1.SetTo(0)
            For i = 0 To delaunay.facetList.Count - 1
                Dim lp = taskAlg.lines.lpList(i)
                Dim facets = delaunay.facetList(i)

                DrawTour(dst1, facets, 255, taskAlg.lineWidth)
                DrawTour(taskAlg.fpMap, facets, i)
                Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
                Dim brick = taskAlg.bricks.brickList(taskAlg.gridMap.Get(Of Integer)(center.Y, center.X))
                taskAlg.lines.lpList(i) = lp
            Next

            Dim index = taskAlg.fpMap.Get(Of Single)(taskAlg.ClickPoint.Y, taskAlg.ClickPoint.X)
            taskAlg.lpD = taskAlg.lines.lpList(index)
            Dim facetsD = delaunay.facetList(taskAlg.lpD.index)
            DrawTour(dst2, facetsD, white, taskAlg.lineWidth)

            labels(2) = taskAlg.lines.labels(2)
            labels(3) = delaunay.labels(2)
        End Sub
    End Class







    Public Class XO_FCSLine_Vertical : Inherits TaskParent
        Dim verts As New XO_Line_TrigVertical
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
                    Dim lpPerp = lp1.perpendicularPoints(center)
                    Dim intersectionPoint = Line_Intersection.IntersectTest(lp1, lpPerp)
                    Dim distance = intersectionPoint.DistanceTo(center)
                    If distance <= options.proximity Then
                        minRect.lpInput1 = lp1
                        minRect.lpInput2 = lp2
                        Dim rotatedRect1 = cv.Cv2.MinAreaRect({lp1.p1, lp1.p2})
                        Dim rotatedRect2 = cv.Cv2.MinAreaRect({lp2.p1, lp2.p2})
                        minRect.Run(src)
                        dst2.Line(lp1.p1, lp1.p2, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
                        dst2.Line(lp2.p1, lp2.p2, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
                        Draw_Arc.DrawRotatedOutline(minRect.rotatedRect, dst3, cv.Scalar.Yellow)
                    End If
                Next
            Next
        End Sub
    End Class








    Public Class XO_FeatureLine_VerticalVerify : Inherits TaskParent
        Dim linesVH As New LineEnds_VH
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

            dst2 = taskAlg.lines.dst2

            Dim raw2D As New List(Of lpData)
            Dim raw3D As New List(Of cv.Point3f)
            For Each lp In taskAlg.lines.lpList
                Dim pt1 As cv.Point3f, pt2 As cv.Point3f
                For j = 0 To 1
                    Dim pt = Choose(j + 1, lp.p1, lp.p2)
                    Dim rect = ValidateRect(New cv.Rect(pt.x - options.kSize, pt.y - options.kSize, options.kernelSize, options.kernelSize))
                    Dim val = taskAlg.pointCloud(rect).Mean(taskAlg.depthMask(rect))
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
                Dim matLines3D As cv.Mat = (cv.Mat.FromPixelData(raw3D.Count, 3, cv.MatType.CV_32F, raw3D.ToArray)) * taskAlg.gMatrix

                For i = 0 To raw2D.Count - 2 Step 2
                    Dim pt1 = matLines3D.Get(Of cv.Point3f)(i, 0)
                    Dim pt2 = matLines3D.Get(Of cv.Point3f)(i + 1, 0)
                    Dim len3D = Distance_Basics.distance3D(pt1, pt2)
                    Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
                    If Math.Abs(arcY - 90) < options.tolerance Then
                        vbc.DrawLine(dst3, raw2D(i).p1, raw2D(i).p2, cv.Scalar.Blue)
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
                        vbc.DrawLine(dst3, raw2D(i).p1, raw2D(i).p2, cv.Scalar.Yellow)
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
            labels(2) = "Starting with " + Format(taskAlg.lines.lpList.Count, "000") + " lines, there are " +
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
                dst3.Line(p1, p2, cv.Scalar.Yellow, taskAlg.lineWidth, taskAlg.lineType)
            Next
        End Sub
    End Class








    Public Class XO_FeatureLine_Tutorial2 : Inherits TaskParent
        Dim options As New Options_LineFinder()
        Public Sub New()
            desc = "Find all the lines in the image and determine which are vertical and horizontal"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst2 = taskAlg.lines.dst2

            Dim raw3D As New List(Of cv.Point3f)
            For Each lp In taskAlg.lines.lpList
                Dim pt1 As cv.Point3f, pt2 As cv.Point3f
                For j = 0 To 1
                    Dim pt = Choose(j + 1, lp.p1, lp.p2)
                    Dim rect = ValidateRect(New cv.Rect(pt.x - options.kSize, pt.y - options.kSize, options.kernelSize, options.kernelSize))
                    Dim val = taskAlg.pointCloud(rect).Mean(taskAlg.depthMask(rect))
                    If j = 0 Then pt1 = New cv.Point3f(val(0), val(1), val(2)) Else pt2 = New cv.Point3f(val(0), val(1), val(2))
                Next
                If pt1.Z > 0 And pt2.Z > 0 Then
                    raw3D.Add(taskAlg.pointCloud.Get(Of cv.Point3f)(lp.p1.Y, lp.p1.X))
                    raw3D.Add(taskAlg.pointCloud.Get(Of cv.Point3f)(lp.p2.Y, lp.p2.X))
                End If
            Next

            If taskAlg.heartBeat Then labels(2) = "Starting with " + Format(taskAlg.lines.lpList.Count, "000") +
                               " lines, there are " + Format(raw3D.Count, "000") + " with depth data."
            If raw3D.Count = 0 Then
                SetTrueText("No vertical or horizontal lines were found")
            Else
                taskAlg.gMatrix = taskAlg.gmat.gMatrix
                Dim matLines3D = cv.Mat.FromPixelData(raw3D.Count, 3, cv.MatType.CV_32F, raw3D.ToArray) * taskAlg.gmat.gMatrix
            End If
        End Sub
    End Class









    Public Class XO_FeatureLine_LongestVerticalKNN : Inherits TaskParent
        Dim gLines As New XO_Line_GCloud
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

                vbc.DrawLine(dst3, p1, p2, taskAlg.highlight)
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
            vbc.DrawLine(dst2, p1, p2, taskAlg.highlight)
            dst3.SetTo(0)
            vbc.DrawLine(dst3, p1, p2, taskAlg.highlight)
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
            vbc.DrawLine(dst2, p1, p2, taskAlg.highlight)
            dst3.SetTo(0)
            vbc.DrawLine(dst3, p1, p2, taskAlg.highlight)

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
            labels(3) = "Length rejects = " + Format(lengthReject / (taskAlg.frameCount + 1), "0%")
        End Sub
    End Class







    Public Class XO_FeatureLine_VerticalLongLine : Inherits TaskParent
        Dim lines As New XO_FeatureLine_Finder3D
        Public Sub New()
            desc = "Use FeatureLine_Finder data to identify the longest lines and show its angle."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.heartBeat Then
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
            vbc.DrawLine(dst2, p1, p2, taskAlg.highlight)
            dst3.SetTo(0)
            vbc.DrawLine(dst3, p1, p2, taskAlg.highlight)
            Dim pt1 = lines.lines3D(index)
            Dim pt2 = lines.lines3D(index + 1)
            Dim len3D = Distance_Basics.distance3D(pt1, pt2)
            Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
            SetTrueText(Format(arcY, fmt3) + vbCrLf + Format(len3D, fmt3) + "m len" + vbCrLf + Format(pt1.Z, fmt1) + "m dist", p1)
            SetTrueText(Format(arcY, fmt3) + vbCrLf + Format(len3D, fmt3) + "m len" + vbCrLf + Format(pt1.Z, fmt1) + "m distant", p1, 3)
        End Sub
    End Class









    Public Class XO_KNN_ClosestVertical : Inherits TaskParent
        Public lines As New XO_FeatureLine_Finder3D
        Public knn As New XO_KNN_ClosestLine
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
            vbc.DrawLine(dst2, knn.lastP1, knn.lastP2, taskAlg.highlight)
        End Sub
    End Class











    Public Class XO_Line_VerticalHorizontalCells : Inherits TaskParent
        Dim lines As New XO_FeatureLine_Finder3D
        Dim hulls As New RedList_Hulls
        Public Sub New()
            labels(2) = "RedList_Hulls output with lines highlighted"
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
                vbc.DrawLine(dst3, p1, p2, cv.Scalar.Yellow)
            Next
            For i = 0 To lines.sortedVerticals.Count - 1
                Dim index = lines.sortedVerticals.ElementAt(i).Value
                Dim p1 = lines.lines2D(index), p2 = lines.lines2D(index + 1)
                vbc.DrawLine(dst3, p1, p2, cv.Scalar.Blue)
            Next
            labels(3) = CStr(lines.sortedVerticals.Count) + " vertical and " + CStr(lines.sortedHorizontals.Count) + " horizontal lines identified in the RedCloud output"
        End Sub
    End Class







    Public Class XO_Line_VerticalHorizontal1 : Inherits TaskParent
        Dim nearest As New XO_Line_Nearest
        Dim options As New Options_Diff
        Public Sub New()
            taskAlg.gOptions.LineWidth.Value = 2
            desc = "Find all the lines in the color image that are parallel to gravity or the horizon using distance to the line instead of slope."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim pixelDiff = options.pixelDiffThreshold

            dst2 = src.Clone
            If standaloneTest() Then dst3 = taskAlg.lines.dst2

            nearest.lp = taskAlg.lineGravity
            vbc.DrawLine(dst2, taskAlg.lineGravity.p1, taskAlg.lineGravity.p2, white)
            For Each lp In taskAlg.lines.lpList
                Dim ptInter = Line_Intersection.IntersectTest(lp.p1, lp.p2, taskAlg.lineGravity.p1, taskAlg.lineGravity.p2)
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
                    vbc.DrawLine(dst2, lp.p1, lp.p2, taskAlg.highlight)
                End If
            Next

            vbc.DrawLine(dst2, taskAlg.lineHorizon.p1, taskAlg.lineHorizon.p2, white)
            nearest.lp = taskAlg.lineHorizon
            For Each lp In taskAlg.lines.lpList
                Dim ptInter = Line_Intersection.IntersectTest(lp.p1, lp.p2, taskAlg.lineHorizon.p1, taskAlg.lineHorizon.p2)
                If ptInter.X >= 0 And ptInter.X < dst2.Width And ptInter.Y >= 0 And ptInter.Y < dst2.Height Then Continue For

                nearest.pt = lp.p1
                nearest.Run(Nothing)
                Dim d1 = nearest.distance

                nearest.pt = lp.p2
                nearest.Run(Nothing)
                Dim d2 = nearest.distance

                If Math.Abs(d1 - d2) <= pixelDiff Then
                    vbc.DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Red)
                End If
            Next
            labels(2) = "Slope for gravity is " + Format(taskAlg.lineGravity.slope, fmt1) + ".  Slope for horizon is " +
                    Format(taskAlg.lineHorizon.slope, fmt1)
        End Sub
    End Class






    Public Class XO_FeatureLine_BasicsRaw : Inherits TaskParent
        Dim lines As New XO_Line_RawSubset
        Dim lineDisp As New XO_Line_DisplayInfoOld
        Dim options As New Options_Features
        Dim match As New XO_Match_tCell
        Public tcells As List(Of tCell)
        Public Sub New()
            Dim tc As New tCell
            tcells = New List(Of tCell)({tc, tc})
            labels = {"", "", "Longest line present.", ""}
            desc = "Find and track a line using the end points"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            Dim distanceThreshold = 50 ' pixels - arbitrary but realistically needs some value
            Dim linePercentThreshold = 0.7 ' if less than 70% of the pixels in the line are edges, then find a better line.  Again, arbitrary but realistic.

            Dim correlationTest = tcells(0).correlation <= taskAlg.fCorrThreshold Or tcells(1).correlation <= taskAlg.fCorrThreshold
            lineDisp.distance = tcells(0).center.DistanceTo(tcells(1).center)
            If taskAlg.optionsChanged Or correlationTest Or lineDisp.maskCount / lineDisp.distance < linePercentThreshold Or
           lineDisp.distance < distanceThreshold Then

                Dim pad = taskAlg.brickSize / 2
                lines.subsetRect = New cv.Rect(pad * 3, pad * 3, src.Width - pad * 6, src.Height - pad * 6)
                lines.Run(src.Clone)

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
            If taskAlg.heartBeat Then
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
                    vbc.DrawLine(dst2, p1, p2, taskAlg.highlight)
                    SetTrueText(CStr(i), If(i Mod 2, p1, p2), 2)
                    vbc.DrawLine(dst3, p1, p2, taskAlg.highlight)

                    Dim pt1 = lines.lines3D(index)
                    Dim pt2 = lines.lines3D(index + 1)
                    Dim len3D = Distance_Basics.distance3D(pt1, pt2)
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
                    ", first was best " + Format(firstBest / taskAlg.frameCount, "0%") + " of the time, Avg of longest line " + Format(firstAverage.Average, fmt1)
            If arcLongAverage.Count > 1000 Then
                arcLongAverage.RemoveAt(0)
                firstAverage.RemoveAt(0)
            End If
        End Sub
    End Class







    Public Class XO_FeatureLine_LongestKNN : Inherits TaskParent
        Dim glines As New XO_Line_GCloud
        Public knn As New XO_KNN_ClosestTracker
        Public gline As gravityLine
        Public match As New Match_Basics
        Dim p1 As cv.Point, p2 As cv.Point
        Public Sub New()
            desc = "Find and track the longest line in the BGR image with a lightweight KNN."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src

            knn.Run(src.Clone)
            p1 = knn.lastPair.p1
            p2 = knn.lastPair.p2
            gline = glines.updateGLine(src, gline, p1, p2)

            Dim rect = ValidateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
            match.template = src(rect).Clone
            match.Run(src)
            If match.correlation >= taskAlg.fCorrThreshold Then
                dst3 = match.dst0.Resize(dst3.Size)
                vbc.DrawLine(dst2, p1, p2, taskAlg.highlight)
                DrawCircle(dst2, p1, taskAlg.DotSize, taskAlg.highlight)
                DrawCircle(dst2, p2, taskAlg.DotSize, taskAlg.highlight)
                rect = ValidateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
                match.template = src(rect).Clone
            Else
                taskAlg.highlight = If(taskAlg.highlight = cv.Scalar.Yellow, cv.Scalar.Blue, cv.Scalar.Yellow)
                knn.lastPair = New lpData(New cv.Point2f, New cv.Point2f)
            End If
            labels(2) = "Longest line end points had correlation of " + Format(match.correlation, fmt3) + " with the original longest line."
        End Sub
    End Class






    Public Class XO_FeatureLine_Longest : Inherits TaskParent
        Dim glines As New XO_Line_GCloud
        Public knn As New XO_KNN_ClosestTracker
        Public gline As gravityLine
        Public match1 As New Match_Basics
        Public match2 As New Match_Basics
        Public Sub New()
            labels(2) = "Longest line end points are highlighted "
            desc = "Find and track the longest line in the BGR image with a lightweight KNN."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone
            Dim pad = taskAlg.brickSize / 2

            Static p1 As cv.Point, p2 As cv.Point
            If taskAlg.heartBeat Or match1.correlation < taskAlg.fCorrThreshold And
                             match2.correlation < taskAlg.fCorrThreshold Then
                knn.Run(src.Clone)

                p1 = knn.lastPair.p1
                Dim r1 = ValidateRect(New cv.Rect(p1.X - pad, p1.Y - pad, taskAlg.brickSize, taskAlg.brickSize))
                match1.template = src(r1).Clone

                p2 = knn.lastPair.p2
                Dim r2 = ValidateRect(New cv.Rect(p2.X - pad, p2.Y - pad, taskAlg.brickSize, taskAlg.brickSize))
                match2.template = src(r2).Clone
            End If

            match1.Run(src)
            p1 = match1.newCenter

            match2.Run(src)
            p2 = match2.newCenter

            gline = glines.updateGLine(src, gline, p1, p2)
            vbc.DrawLine(dst2, p1, p2, taskAlg.highlight)
            DrawCircle(dst2, p1, taskAlg.DotSize, taskAlg.highlight)
            DrawCircle(dst2, p2, taskAlg.DotSize, taskAlg.highlight)
            SetTrueText(Format(match1.correlation, fmt3), p1)
            SetTrueText(Format(match2.correlation, fmt3), p2)
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
            If input.Type <> cv.MatType.CV_32F Then input = taskAlg.pcSplit(2)

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
                    Dim d1 = taskAlg.pcSplit(2).Get(Of Single)(p1.Y, p1.X)
                    Dim d2 = taskAlg.pcSplit(2).Get(Of Single)(p2.Y, p2.X)
                    If stepX * options.threshold > Math.Abs(d1 - d2) And d1 > 0 And d2 > 0 Then
                        Dim p = taskAlg.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
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
                        Dim meanVal = cv.Cv2.Mean(taskAlg.pcSplit(2)(r), taskAlg.depthMask(r))
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
            taskAlg.gOptions.GridSlider.Value = 10
            desc = "Attempt to impose a linear structure on the pointcloud."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim yLines = options.xLines * dst2.Height \ dst2.Width

            Dim stepX = dst3.Width / options.xLines
            Dim stepY = dst3.Height / yLines
            dst2 = New cv.Mat(dst3.Size(), cv.MatType.CV_32FC3, 0)
            For y = 0 To yLines - 1
                For x = 0 To options.xLines - 1
                    Dim r = New cv.Rect(x * stepX, y * stepY, stepX - 1, stepY - 1)
                    Dim p1 = New cv.Point(r.X, r.Y)
                    Dim p2 = New cv.Point(r.X + r.Width, r.Y + r.Height)
                    Dim vec1 = taskAlg.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
                    Dim vec2 = taskAlg.pointCloud.Get(Of cv.Vec3f)(p2.Y, p2.X)
                    If vec1(2) > 0 And vec2(2) > 0 Then dst2(r).SetTo(vec1)
                Next
            Next
            labels(2) = "Structured_Cloud with " + CStr(yLines) + " rows " + CStr(options.xLines) + " columns"
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
            Dim yLines = xLines * dst2.Width \ dst2.Height
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
                                dst2.Line(p1, p2, taskAlg.highlight, taskAlg.lineWidth + 1, taskAlg.lineType)
                            End If
                        End If
                        If y = sCloud.options.indexY Then
                            Dim p1 = New cv.Point(pointX.Get(Of Integer)(y, x - 1), pointY.Get(Of Integer)(y, x - 1))
                            If p1.X > 0 Then
                                Dim p2 = New cv.Point(xx, yy)
                                dst2.Line(p1, p2, taskAlg.highlight, taskAlg.lineWidth + 1, taskAlg.lineType)
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
            taskAlg.gOptions.GridSlider.Value = 10
            desc = "Simplify the point cloud so it can be represented as quads in OpenGL"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = New cv.Mat(dst3.Size(), cv.MatType.CV_32FC3, 0)
            For Each roi In taskAlg.gridRects
                Dim d = taskAlg.pointCloud(roi).Mean(taskAlg.depthMask(roi))
                Dim depth = New cv.Vec3f(d.Val0, d.Val1, d.Val2)
                Dim pt = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
                Dim vec = taskAlg.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X)
                If vec(2) > 0 Then dst2(roi).SetTo(depth)
            Next

            labels(2) = traceName + " with " + CStr(taskAlg.gridRects.Count) + " regions was created"
        End Sub
    End Class








    Public Class XO_Structured_Tiles : Inherits TaskParent
        Public oglData As New List(Of cv.Vec3f)
        Dim hulls As New RedList_Hulls
        Public Sub New()
            taskAlg.gOptions.GridSlider.Value = 10
            desc = "Use the OpenGL point size to represent the point cloud as data"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hulls.Run(src)
            dst2 = hulls.dst3

            dst3.SetTo(0)
            oglData.Clear()
            For Each roi In taskAlg.gridRects
                Dim c = dst2.Get(Of cv.Vec3b)(roi.Y, roi.X)
                If c = black Then Continue For
                oglData.Add(New cv.Vec3f(c(2) / 255, c(1) / 255, c(0) / 255))

                Dim v = taskAlg.pointCloud(roi).Mean(taskAlg.depthMask(roi))
                oglData.Add(New cv.Vec3f(v.Val0, v.Val1, v.Val2))
                dst3(roi).SetTo(c)
            Next
            labels(2) = traceName + " with " + CStr(taskAlg.gridRects.Count) + " regions was created"
        End Sub
    End Class






    Public Class XO_LineRect_CenterDepth : Inherits TaskParent
        Public options As New Options_LineRect
        Public Sub New()
            If taskAlg.bricks Is Nothing Then taskAlg.bricks = New Brick_Basics
            desc = "Remove lines which have similar depth in bricks on either side of a line."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst2 = src.Clone
            dst3 = src.Clone

            Dim depthThreshold = options.depthThreshold
            Dim depthLines As Integer, colorLines As Integer
            For Each lp In taskAlg.lines.lpList
                dst2.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth, cv.LineTypes.Link4)
                Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
                Dim lpPerp = lp.perpendicularPoints(center)
                Dim index1 As Integer = taskAlg.gridMap.Get(Of Integer)(lpPerp.p1.Y, lpPerp.p1.X)
                Dim index2 As Integer = taskAlg.gridMap.Get(Of Integer)(lpPerp.p2.Y, lpPerp.p2.X)
                Dim brick1 = taskAlg.bricks.brickList(index1)
                Dim brick2 = taskAlg.bricks.brickList(index2)
                If Math.Abs(brick1.depth - brick2.depth) > depthThreshold Then
                    dst2.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth, cv.LineTypes.Link4)
                    depthLines += 1
                Else
                    dst3.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth, cv.LineTypes.Link4)
                    colorLines += 1
                End If
            Next

            If taskAlg.heartBeat Then
                labels(2) = CStr(depthLines) + " lines were found between objects (depth Lines)"
                labels(3) = CStr(colorLines) + " internal lines were indentified and are not likely important"
            End If
        End Sub
    End Class





    Public Class XO_LineCoin_Parallel : Inherits TaskParent
        Dim parallel As New XO_Line_Parallel
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
                dst2.Line(cp.p3, cp.p4, cv.Scalar.Red, taskAlg.lineWidth + 2, taskAlg.lineType)
                dst2.Line(cp.p1, cp.p2, taskAlg.highlight, taskAlg.lineWidth + 1, taskAlg.lineType)
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
            Dim perMeter = dst3.Height / taskAlg.MaxZmeters
            dst3.SetTo(0)
            Dim white As New cv.Vec3b(255, 255, 255)
            For y = 0 To mask.Height - 1
                For x = 0 To mask.Width - 1
                    Dim val = mask.Get(Of Byte)(y, x)
                    If val > 0 Then
                        Dim depth = taskAlg.pcSplit(2).Get(Of Single)(y, x)
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
            taskAlg.kalman = New Kalman_Basics
            ReDim taskAlg.kalman.kInput(2 - 1)
            OptionParent.FindCheckBox("Top View (Unchecked Side View)").Checked = False
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

            taskAlg.kalman.kInput(0) = floorY
            taskAlg.kalman.kInput(1) = ceilingY
            taskAlg.kalman.Run(emptyMat)

            labels(2) = "Current slice is at row =" + CStr(taskAlg.mouseMovePoint.Y)
            labels(3) = "Ceiling is at row =" + CStr(CInt(taskAlg.kalman.kOutput(1))) + " floor at y=" + CStr(CInt(taskAlg.kalman.kOutput(0)))

            vbc.DrawLine(dst2, New cv.Point(0, floorY), New cv.Point(dst2.Width, floorY), cv.Scalar.Yellow)
            SetTrueText("floor", New cv.Point(10, floorY + taskAlg.DotSize), 3)

            Dim rect = New cv.Rect(0, Math.Max(ceilingY - 5, 0), dst2.Width, 10)
            Dim mask = slice.heat.dst3(rect)
            Dim mean As cv.Scalar, stdev As cv.Scalar
            cv.Cv2.MeanStdDev(mask, mean, stdev)
            If mean(0) < mean(2) Then
                vbc.DrawLine(dst2, New cv.Point(0, ceilingY), New cv.Point(dst2.Width, ceilingY), cv.Scalar.Yellow)
                SetTrueText("ceiling", New cv.Point(10, ceilingY + taskAlg.DotSize), 3)
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
            Dim output As New cv.Mat(taskAlg.pcSplit(1).Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
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
                Dim planeX = -taskAlg.xRange * (taskAlg.topCameraPoint.X - i) / taskAlg.topCameraPoint.X
                If i > taskAlg.topCameraPoint.X Then planeX = taskAlg.xRange * (i - taskAlg.topCameraPoint.X) / (dst3.Width - taskAlg.topCameraPoint.X)

                cv.Cv2.InRange(taskAlg.pcSplit(0), planeX - thickness, planeX + thickness, sliceMask)
                output.SetTo(planeX, sliceMask)
            Next
            Return output
        End Function
        Private Function rebuildY(viewY As cv.Mat) As cv.Mat
            Dim output As New cv.Mat(taskAlg.pcSplit(1).Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
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
                Dim planeY = -taskAlg.yRange * (taskAlg.sideCameraPoint.Y - i) / taskAlg.sideCameraPoint.Y
                If i > taskAlg.sideCameraPoint.Y Then planeY = taskAlg.yRange * (i - taskAlg.sideCameraPoint.Y) / (dst3.Height - taskAlg.sideCameraPoint.Y)

                cv.Cv2.InRange(taskAlg.pcSplit(1), planeY - thickness, planeY + thickness, sliceMask)
                output.SetTo(planeY, sliceMask)
            Next
            Return output
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim metersPerPixel = taskAlg.MaxZmeters / dst3.Height
            thickness = options.sliceSize * metersPerPixel
            heat.Run(src)

            If options.rebuilt Then
                taskAlg.pcSplit(0) = rebuildX(heat.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
                taskAlg.pcSplit(1) = rebuildY(heat.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
                cv.Cv2.Merge(taskAlg.pcSplit, pointcloud)
            Else
                taskAlg.pcSplit = taskAlg.pointCloud.Split()
                pointcloud = taskAlg.pointCloud
            End If

            dst2 = Mat_Convert.Mat_32f_To_8UC3(taskAlg.pcSplit(0))
            dst3 = Mat_Convert.Mat_32f_To_8UC3(taskAlg.pcSplit(1))
            dst2.SetTo(0, taskAlg.noDepthMask)
            dst3.SetTo(0, taskAlg.noDepthMask)
        End Sub
    End Class









    Public Class XO_tructured_MouseSlice : Inherits TaskParent
        Dim slice As New Structured_SliceEither
        Dim lines As New XO_Line_RawSorted
        Public Sub New()
            labels(2) = "Center Slice in yellow"
            labels(3) = "White = SliceV output, Red Dot is avgPt"
            desc = "Find the vertical center line with accurate depth data.."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.mouseMovePoint = newPoint Then taskAlg.mouseMovePoint = New cv.Point(dst2.Width / 2, dst2.Height)
            slice.Run(src)

            lines.Run(slice.sliceMask)
            Dim tops As New List(Of Integer)
            Dim bots As New List(Of Integer)
            Dim topsList As New List(Of cv.Point)
            Dim botsList As New List(Of cv.Point)
            If taskAlg.lines.lpList.Count > 0 Then
                dst3 = lines.dst2
                For Each lp In taskAlg.lines.lpList
                    dst3.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth + 3, taskAlg.lineType)
                    tops.Add(If(lp.p1.Y < lp.p2.Y, lp.p1.Y, lp.p2.Y))
                    bots.Add(If(lp.p1.Y > lp.p2.Y, lp.p1.Y, lp.p2.Y))
                    topsList.Add(lp.p1)
                    botsList.Add(lp.p2)
                Next

                'Dim topPt = topsList(tops.IndexOf(tops.Min))
                'Dim botPt = botsList(bots.IndexOf(bots.Max))
                'DrawCircle(dst3,New cv.Point2f((topPt.X + botPt.X) / 2, (topPt.Y + botPt.Y) / 2), taskAlg.DotSize + 5, cv.Scalar.Red)
                'dst3.Line(topPt, botPt, cv.Scalar.Red, taskAlg.lineWidth, taskAlg.lineType)
                'DrawLine(dst2,topPt, botPt, taskAlg.highlight, taskAlg.lineWidth + 2, taskAlg.lineType)
            End If
            If standaloneTest() Then
                dst2 = src
                dst2.SetTo(white, dst3)
            End If
        End Sub
    End Class







    Public Class XO_Contour_Gray : Inherits TaskParent
        Public contour As New List(Of cv.Point)
        Public options As New Options_Contours
        Dim myFrameCount As Integer = taskAlg.frameCount
        Dim reduction As New Reduction_Basics
        Public Sub New()
            desc = "Find the contour for the src."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If myFrameCount <> taskAlg.frameCount Then
                options.Run() ' avoid running options more than once per frame.
                myFrameCount = taskAlg.frameCount
            End If

            If standalone Then
                reduction.Run(src)
                src = reduction.dst2
            End If

            Dim allContours As cv.Point()() = Nothing
            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            cv.Cv2.FindContours(src, allContours, Nothing, cv.RetrievalModes.External, options.ApproximationMode)
            If allContours.Count = 0 Then Exit Sub

            dst2 = src
            For Each tour In allContours
                DrawTour(dst2, tour.ToList, white, taskAlg.lineWidth)
            Next
            labels(2) = $"There were {allContours.Count} contours found."
        End Sub
    End Class







    Public Class XO_Contour_RC_AddContour : Inherits TaskParent
        Public contour As New List(Of cv.Point)
        Public options As New Options_Contours
        Dim myFrameCount As Integer = taskAlg.frameCount
        Dim reduction As New Reduction_Basics
        Dim contours As New Contour_Regions
        Public Sub New()
            desc = "Find the contour for the src."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If myFrameCount <> taskAlg.frameCount Then
                options.Run() ' avoid running options more than once per frame.
                myFrameCount = taskAlg.frameCount
            End If

            If standalone Then
                reduction.Run(src)
                src = reduction.dst2
            End If
            dst2 = src.Clone
            dst3 = PaletteFull(dst2)

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
            DrawTour(dst2, contour, taskAlg.highlight, taskAlg.lineWidth)
        End Sub
    End Class






    Public Class XO_Pixel_Unique_CPP : Inherits TaskParent
        Public Sub New()
            cPtr = Pixels_Vector_Open()
            desc = "Create the list of pixels in a RedCloud Cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.drawRect <> New cv.Rect Then src = src(taskAlg.drawRect)
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
            labels = {"", "", "RedList_Basics output", ""}
            desc = "Find the 4 points farthest from the center in each quadrant of the selected RedCloud cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

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
                vbc.DrawLine(dst3, taskAlg.oldrcD.maxDist, corners(i), white)
                SetTrueText(nextLabel, New cv.Point(corners(i).X, corners(i).Y), 3)
            Next

            If corners.Count Then SetTrueText(sides.strOut, 3) Else SetTrueText(strOut, 3)
        End Sub
    End Class






    Public Class XO_Contour_RedCloudCorners : Inherits TaskParent
        Public corners(4 - 1) As cv.Point
        Public rc As New oldrcData
        Public Sub New()
            labels(2) = "The RedCloud Output with the highlighted contour to smooth"
            desc = "Find the point farthest from the center in each cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                dst2 = runRedList(src, labels(2))
                rc = taskAlg.oldrcD
            End If

            dst3.SetTo(0)
            DrawCircle(dst3, rc.maxDist, taskAlg.DotSize, white)
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

            DrawTour(dst3(rc.rect), rc.contour, white)
            For i = 0 To corners.Count - 1
                vbc.DrawLine(dst3(rc.rect), center, corners(i), white)
            Next
        End Sub
    End Class





    Public Class XO_Sides_Profile : Inherits TaskParent
        Dim sides As New Contour_SidePoints
        Public Sub New()
            labels = {"", "", "RedList_Basics Output", "Selected Cell"}
            desc = "Find the 6 corners - left/right, top/bottom, front/back - of a RedCloud cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

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
            dst2 = runRedList(src, labels(2))

            sides.Run(src)
            dst3 = sides.dst3
        End Sub
    End Class






    Public Class XO_Contour_RedCloudEdges : Inherits TaskParent
        Dim edgeline As New EdgeLine_Basics
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            labels = {"", "EdgeLine_Basics output", "", "Pixels below are both cell boundaries and edges."}
            desc = "Intersect the cell contours and the edges in the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edgeline.Run(taskAlg.grayStable)
            runRedList(src, labels(3))
            labels(2) = taskAlg.redList.labels(2) + " - Contours only.  Click anywhere to select a cell"

            dst2.SetTo(0)
            For Each rc In taskAlg.redList.oldrclist
                DrawTour(dst2(rc.rect), rc.contour, 255, taskAlg.lineWidth)
            Next

            dst3 = edgeline.dst2 And dst2
        End Sub
    End Class







    Public Class XO_LeftRight_Markers : Inherits TaskParent
        Dim redView As New LeftRight_Reduction
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
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
            Dim lineLen = taskAlg.gOptions.DebugSlider.Value
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
            Dim lineLen = taskAlg.gOptions.DebugSlider.Value
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






    Public Class XO_Color8U_Edges : Inherits TaskParent
        Dim edges As New Edge_Canny
        Dim edgeline As New EdgeLine_Basics
        Public Sub New()
            desc = "Find edges in the Color8U_Basics output"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edgeline.Run(taskAlg.grayStable)
            dst2 = edgeline.dst2

            edges.Run(dst2)
            dst3 = edges.dst2
            labels(2) = edgeline.strOut
        End Sub
    End Class






    Public Class XO_Brick_FitLeftInColor : Inherits TaskParent
        Public Sub New()
            If taskAlg.bricks Is Nothing Then taskAlg.bricks = New Brick_Basics
            taskAlg.drawRect = New cv.Rect(10, 10, 50, 50)
            labels(3) = "Draw a rectangle to update."
            desc = "Translate the left image into the same coordinates as the color image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim correlationMat As New cv.Mat

            Dim p1 = taskAlg.bricks.brickList(0).lRect.TopLeft
            Dim p2 = taskAlg.bricks.brickList(taskAlg.bricks.brickList.Count - 1).lRect.BottomRight

            ' Dim rect = ValidateRect(New cv.Rect(p1.X - taskAlg.brickSize, p1.Y - taskAlg.brickSize, taskAlg.brickSize * 2, taskAlg.brickSize * 2))
            cv.Cv2.MatchTemplate(taskAlg.gray(taskAlg.drawRect), taskAlg.leftView, dst2, cv.TemplateMatchModes.CCoeffNormed)
            Dim mm = GetMinMax(dst2)
            dst3 = src(ValidateRect(New cv.Rect(mm.maxLoc.X / 2, mm.maxLoc.Y / 2, dst2.Width, dst2.Height)))
            labels(2) = "Correlation coefficient peak = " + Format(mm.maxVal, fmt3)
        End Sub
    End Class





    Public Class XO_Depth_MeanStdev_MT : Inherits TaskParent
        Dim meanSeries As New cv.Mat
        Dim maxMeanVal As Single, maxStdevVal As Single
        Public Sub New()
            If standalone Then taskAlg.gOptions.GridSlider.Value = taskAlg.gOptions.GridSlider.Maximum
            dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8U, cv.Scalar.All(0))
            dst3 = New cv.Mat(dst3.Rows, dst3.Cols, cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Collect a time series of depth mean and stdev to highlight where depth is unstable."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.optionsChanged Then meanSeries = New cv.Mat(taskAlg.gridRects.Count, taskAlg.frameHistoryCount, cv.MatType.CV_32F, cv.Scalar.All(0))

            Dim index = taskAlg.frameCount Mod taskAlg.frameHistoryCount
            Dim meanValues(taskAlg.gridRects.Count - 1) As Single
            Dim stdValues(taskAlg.gridRects.Count - 1) As Single
            Parallel.For(0, taskAlg.gridRects.Count,
        Sub(i)
            Dim roi = taskAlg.gridRects(i)
            Dim mean As cv.Scalar, stdev As cv.Scalar
            cv.Cv2.MeanStdDev(taskAlg.pcSplit(2)(roi), mean, stdev, taskAlg.depthMask(roi))
            meanSeries.Set(Of Single)(i, index, mean)
            If taskAlg.frameCount >= taskAlg.frameHistoryCount - 1 Then
                cv.Cv2.MeanStdDev(meanSeries.Row(i), mean, stdev)
                meanValues(i) = mean
                stdValues(i) = stdev
            End If
        End Sub)

            If taskAlg.frameCount >= taskAlg.frameHistoryCount Then
                Dim means As cv.Mat = cv.Mat.FromPixelData(taskAlg.gridRects.Count, 1, cv.MatType.CV_32F, meanValues.ToArray)
                Dim stdevs As cv.Mat = cv.Mat.FromPixelData(taskAlg.gridRects.Count, 1, cv.MatType.CV_32F, stdValues.ToArray)
                Dim meanmask = means.Threshold(1, taskAlg.MaxZmeters, cv.ThresholdTypes.Binary).ConvertScaleAbs()
                Dim mm As mmData = GetMinMax(means, meanmask)
                Dim stdMask = stdevs.Threshold(0.001, taskAlg.MaxZmeters, cv.ThresholdTypes.Binary).ConvertScaleAbs() ' volatile region is x cm stdev.
                Dim mmStd = GetMinMax(stdevs, stdMask)

                maxMeanVal = Math.Max(maxMeanVal, mm.maxVal)
                maxStdevVal = Math.Max(maxStdevVal, mmStd.maxVal)

                Parallel.For(0, taskAlg.gridRects.Count,
            Sub(i)
                Dim roi = taskAlg.gridRects(i)
                dst3(roi).SetTo(255 * stdevs.Get(Of Single)(i, 0) / maxStdevVal)
                dst3(roi).SetTo(0, taskAlg.noDepthMask(roi))

                dst2(roi).SetTo(255 * means.Get(Of Single)(i, 0) / maxMeanVal)
                dst2(roi).SetTo(0, taskAlg.noDepthMask(roi))
            End Sub)

                If taskAlg.heartBeat Then
                    maxMeanVal = 0
                    maxStdevVal = 0
                End If

                If standaloneTest() Then
                    For i = 0 To taskAlg.gridRects.Count - 1
                        Dim roi = taskAlg.gridRects(i)
                        SetTrueText(Format(meanValues(i), fmt3) + vbCrLf +
                                Format(stdValues(i), fmt3), roi.Location, 3)
                    Next
                End If

                dst3 = dst3 Or taskAlg.gridMask
                labels(2) = "The regions where the depth is volatile are brighter.  Stdev min " + Format(mmStd.minVal, fmt3) + " Stdev Max " + Format(mmStd.maxVal, fmt3)
                labels(3) = "Mean/stdev for each ROI: Min " + Format(mm.minVal, fmt3) + " Max " + Format(mm.maxVal, fmt3)
            End If
        End Sub
    End Class





    Public Class XO_ML_FillRGBDepth_MT : Inherits TaskParent
        Dim shadow As New Depth_Holes
        Dim colorizer As New DepthColorizer_CPP
        Public Sub New()
            taskAlg.gOptions.GridSlider.Maximum = dst2.Cols / 2
            taskAlg.gOptions.GridSlider.Value = dst2.Cols \ 2

            labels = {"", "", "ML filled shadow", ""}
            desc = "Predict depth based on color and colorize depth to confirm correctness of model.  NOTE: memory leak occurs if more multi-threading is used!"
        End Sub
        Private Class CompareVec3f : Implements IComparer(Of cv.Vec3f)
            Public Function Compare(ByVal a As cv.Vec3f, ByVal b As cv.Vec3f) As Integer Implements IComparer(Of cv.Vec3f).Compare
                If a(0) = b(0) And a(1) = b(1) And a(2) = b(2) Then Return 0
                Return If(a(0) < b(0), -1, 1)
            End Function
        End Class
        Public Function detectAndFillShadow(holeMask As cv.Mat, borderMask As cv.Mat, depth32f As cv.Mat, color As cv.Mat, minLearnCount As Integer) As cv.Mat
            Dim learnData As New SortedList(Of cv.Vec3f, Single)(New CompareVec3f)
            Dim rng As New System.Random
            Dim holeCount = cv.Cv2.CountNonZero(holeMask)
            If borderMask.Channels() <> 1 Then borderMask = borderMask.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim borderCount = cv.Cv2.CountNonZero(borderMask)
            If holeCount > 0 And borderCount > minLearnCount Then
                Dim color32f As New cv.Mat
                color.ConvertTo(color32f, cv.MatType.CV_32FC3)

                Dim learnInputList As New List(Of cv.Vec3f)
                Dim responseInputList As New List(Of Single)

                For y = 0 To holeMask.Rows - 1
                    For x = 0 To holeMask.Cols - 1
                        If borderMask.Get(Of Byte)(y, x) Then
                            Dim vec = color32f.Get(Of cv.Vec3f)(y, x)
                            If learnData.ContainsKey(vec) = False Then
                                learnData.Add(vec, depth32f.Get(Of Single)(y, x)) ' keep out duplicates.
                                learnInputList.Add(vec)
                                responseInputList.Add(depth32f.Get(Of Single)(y, x))
                            End If
                        End If
                    Next
                Next

                Dim learnInput As cv.Mat = cv.Mat.FromPixelData(learnData.Count, 3, cv.MatType.CV_32F, learnInputList.ToArray())
                Dim depthResponse As cv.Mat = cv.Mat.FromPixelData(learnData.Count, 1, cv.MatType.CV_32F, responseInputList.ToArray())

                ' now learn what depths are associated with which colors.
                Dim rtree = cv.ML.RTrees.Create()
                rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)

                ' now predict what the depth is based just on the color (and proximity to the region)
                Using predictMat As New cv.Mat(1, 3, cv.MatType.CV_32F)
                    For y = 0 To holeMask.Rows - 1
                        For x = 0 To holeMask.Cols - 1
                            If holeMask.Get(Of Byte)(y, x) Then
                                predictMat.Set(Of cv.Vec3f)(0, 0, color32f.Get(Of cv.Vec3f)(y, x))
                                depth32f.Set(Of Single)(y, x, rtree.Predict(predictMat))
                            End If
                        Next
                    Next
                End Using
            End If
            Return depth32f
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim minLearnCount = 5
            Parallel.ForEach(taskAlg.gridRects,
            Sub(roi)
                taskAlg.pcSplit(2)(roi) = detectAndFillShadow(taskAlg.noDepthMask(roi), shadow.dst3(roi), taskAlg.pcSplit(2)(roi), src(roi),
                                                           minLearnCount)
            End Sub)

            colorizer.Run(taskAlg.pcSplit(2))
            dst2 = colorizer.dst2.Clone()
            dst2.SetTo(white, taskAlg.gridMask)
        End Sub
    End Class





    Public Class XO_Line_BasicsAlternative : Inherits TaskParent
        Public lines As New XO_Line_RawSorted
        Public Sub New()
            dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0) ' can't use 32S because calcHist won't use it...
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Collect lines across frames using the motion mask.  Results are in taskAlg.lines.lpList."
        End Sub
        Private Function getLineCounts(lpList As List(Of lpData)) As Single()
            Dim histarray(lpList.Count - 1) As Single
            If lpList.Count > 0 Then
                Dim histogram As New cv.Mat
                dst1.SetTo(0)
                For Each lp In lpList
                    dst1.Line(lp.p1, lp.p2, lp.index + 1, taskAlg.lineWidth, cv.LineTypes.Link4)
                Next

                cv.Cv2.CalcHist({dst1}, {0}, taskAlg.motionMask, histogram, 1, {lpList.Count}, New cv.Rangef() {New cv.Rangef(0, lpList.Count)})

                Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)
            End If

            Return histarray
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.optionsChanged Then taskAlg.lines.lpList.Clear()

            Dim histArray = getLineCounts(lines.lpList)
            Dim newList As New List(Of lpData)
            For i = histArray.Count - 1 To 0 Step -1
                If histArray(i) = 0 Then newList.Add(lines.lpList(i))
            Next

            If src.Channels = 1 Then lines.Run(src) Else lines.Run(taskAlg.grayStable.Clone)

            histArray = getLineCounts(taskAlg.lines.lpList)
            For i = histArray.Count - 1 To 1 Step -1
                If histArray(i) Then
                    newList.Add(taskAlg.lines.lpList(i)) ' Add the lines in the motion mask.
                End If
            Next

            dst3.SetTo(0)
            For Each lp In newList
                dst3.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth, cv.LineTypes.Link4)
            Next

            Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
            For Each lp In newList
                If lp.length > 0 Then sortlines.Add(lp.length, lp)
            Next

            taskAlg.lines.lpList.Clear()
            ' placeholder for zero so we can distinguish line 1 from the background which is 0.
            taskAlg.lines.lpList.Add(New lpData(New cv.Point, New cv.Point))

            dst2 = src
            For Each lp In sortlines.Values
                taskAlg.lines.lpList.Add(lp)
                dst2.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
            Next

            labels(2) = CStr(taskAlg.lines.lpList.Count) + " lines were found."
            labels(3) = CStr(lines.lpList.Count) + " lines were in the motion mask."
        End Sub
    End Class






    Public Class XO_FeatureLine_Basics : Inherits TaskParent
        Dim match As New Match_Basics
        Public cameraMotionProxy As New lpData
        Public gravityRGB As lpData
        Dim matchRuns As Integer, lineRuns As Integer, totalLineRuns As Integer
        Public runOnEachFrame As Boolean
        Public gravityMatch As New XO_Line_MatchGravity
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Find and track the longest line by matching line bricks."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.optionsChanged Then taskAlg.lines.lpList.Clear()

            If matchRuns > 500 Then
                Dim percent = lineRuns / matchRuns
                lineRuns = 10
                matchRuns = lineRuns / percent
            End If

            Dim index = taskAlg.gridMap.Get(Of Integer)(cameraMotionProxy.p1.Y, cameraMotionProxy.p1.X)
            Dim firstRect = taskAlg.gridNabeRects(index)
            index = taskAlg.gridMap.Get(Of Integer)(cameraMotionProxy.p2.Y, cameraMotionProxy.p2.X)
            Dim lastRect = taskAlg.gridNabeRects(index)

            dst2 = src.Clone
            If taskAlg.lines.lpList.Count > 0 Then
                matchRuns += 1
                cameraMotionProxy = taskAlg.lines.lpList(0)

                Dim matchInput As New cv.Mat
                cv.Cv2.HConcat(src(firstRect), src(lastRect), matchInput)

                match.Run(matchInput)

                labels(2) = "Line end point correlation:  " + Format(match.correlation, fmt3) + " / " +
                        " with " + Format(lineRuns / matchRuns, "0%") + " requiring line detection.  " +
                        "line detection runs = " + CStr(totalLineRuns)
            End If

            If taskAlg.heartBeatLT Or taskAlg.lines.lpList.Count <= 1 Or match.correlation < 0.98 Or runOnEachFrame Then
                taskAlg.motionMask.SetTo(255) ' force a complete line detection
                taskAlg.lines.Run(src.Clone)

                cameraMotionProxy = taskAlg.lines.lpList(0)
                lineRuns += 1
                totalLineRuns += 1

                Dim matchTemplate As New cv.Mat
                cv.Cv2.HConcat(src(firstRect), src(lastRect), matchTemplate)
                match.template = matchTemplate.Clone
            End If

            labels(3) = "Currently available lines."
            dst3 = taskAlg.lines.dst3
            labels(3) = taskAlg.lines.labels(3)

            gravityMatch.Run(src)
            If gravityMatch.gLines.Count > 0 Then gravityRGB = gravityMatch.gLines(0)

            dst2.Rectangle(firstRect, taskAlg.highlight, taskAlg.lineWidth)
            dst2.Rectangle(lastRect, taskAlg.highlight, taskAlg.lineWidth)
            dst2.Line(cameraMotionProxy.p1, cameraMotionProxy.p2, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
            dst2.Line(taskAlg.lineGravity.pE1, taskAlg.lineGravity.pE2, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
        End Sub
    End Class







    Public Class XO_MiniCloud_Basics : Inherits TaskParent
        Dim resize As Resize_Smaller
        Public rect As cv.Rect
        Public options As New Options_IMU
        Public Sub New()
            resize = New Resize_Smaller
            OptionParent.FindSlider("LowRes %").Value = 25
            desc = "Create a mini point cloud for use with histograms"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            resize.Run(taskAlg.pointCloud)

            Dim split = resize.dst2.Split()
            split(2).SetTo(0, taskAlg.noDepthMask.Resize(split(2).Size))
            rect = New cv.Rect(0, 0, resize.dst2.Width, resize.dst2.Height)
            If rect.Height < dst2.Height / 2 Then rect.Y = dst2.Height / 4 ' move it below the dst2 caption
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            dst2(rect) = split(2).ConvertScaleAbs(255)
            dst2.Rectangle(rect, white, 1)
            cv.Cv2.Merge(split, dst3)
            labels(2) = "MiniPC is " + CStr(rect.Width) + "x" + CStr(rect.Height) + " total pixels = " + CStr(rect.Width * rect.Height)
        End Sub
    End Class








    Public Class XO_MiniCloud_Rotate : Inherits TaskParent
        Public mini As New XO_MiniCloud_Basics
        Public histogram As New cv.Mat
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            labels(3) = "Side view after resize percentage - use Y-Axis slider to rotate image."
            desc = "Create a histogram for the mini point cloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static ySlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis (degrees)")

            Dim input = src
            mini.Run(input)
            input = mini.dst3
            taskAlg.accRadians.Y = ySlider.Value

            Dim cx As Double = 1, sx As Double = 0, cy As Double = 1, sy As Double = 0, cz As Double = 1, sz As Double = 0
            Dim gM(,) As Single = {{cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                               {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                               {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz}}
            '[cos(a) 0 -sin(a)]
            '[0      1       0]
            '[sin(a) 0   cos(a] rotate the point cloud around the y-axis.
            cy = Math.Cos(taskAlg.accRadians.Y * cv.Cv2.PI / 180)
            sy = Math.Sin(taskAlg.accRadians.Y * cv.Cv2.PI / 180)
            gM = {{gM(0, 0) * cy + gM(0, 1) * 0 + gM(0, 2) * sy}, {gM(0, 0) * 0 + gM(0, 1) * 1 + gM(0, 2) * 0}, {gM(0, 0) * -sy + gM(0, 1) * 0 + gM(0, 2) * cy},
              {gM(1, 0) * cy + gM(1, 1) * 0 + gM(1, 2) * sy}, {gM(1, 0) * 0 + gM(1, 1) * 1 + gM(1, 2) * 0}, {gM(1, 0) * -sy + gM(1, 1) * 0 + gM(1, 2) * cy},
              {gM(2, 0) * cy + gM(2, 1) * 0 + gM(2, 2) * sy}, {gM(2, 0) * 0 + gM(2, 1) * 1 + gM(2, 2) * 0}, {gM(2, 0) * -sy + gM(2, 1) * 0 + gM(2, 2) * cy}}

            Dim gMat = cv.Mat.FromPixelData(3, 3, cv.MatType.CV_32F, gM)
            Dim gInput = input.Reshape(1, input.Rows * input.Cols)
            Dim gOutput = (gInput * gMat).ToMat
            input = gOutput.Reshape(3, input.Rows)

            Dim split = input.Split()
            Dim mask = split(2).Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
            input.SetTo(0, mask.ConvertScaleAbs(255)) ' remove zero depth pixels with non-zero x and y.

            Dim ranges() = New cv.Rangef() {New cv.Rangef(-taskAlg.yRange, taskAlg.yRange), New cv.Rangef(0, taskAlg.MaxZmeters)}
            cv.Cv2.CalcHist({input}, {1, 2}, New cv.Mat, histogram, 2, {input.Height, input.Width}, ranges)

            dst2(mini.rect) = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
            dst3(mini.rect) = input.ConvertScaleAbs(255)
        End Sub
    End Class








    Public Class XO_MiniCloud_RotateAngle : Inherits TaskParent
        Dim peak As New XO_MiniCloud_Rotate
        Dim mats As New Mat_4to1
        Public plot As New Plot_OverTimeSingle
        Dim resetCheck As System.Windows.Forms.CheckBox
        Public Sub New()
            taskAlg.accRadians.Y = -cv.Cv2.PI / 2

            labels(2) = "peak dst2, peak dst3, changed mask, maxvalues history"
            labels(3) = "Blue is maxVal, green is mean * 100"
            desc = "Find a peak value in the side view histograms"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32FC3 Then
                peak.mini.Run(src)
                src = peak.mini.dst3
            End If

            Static ySlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis (degrees)")
            If ySlider.Value + 1 >= ySlider.maximum Then ySlider.Value = ySlider.minimum Else ySlider.Value += 1

            peak.Run(src)
            Dim mm As mmData = GetMinMax(peak.histogram)

            Dim mean = peak.histogram.Mean()(0) * 100
            Dim mask = peak.histogram.Threshold(mean, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
            mats.mat(2) = mask

            plot.plotData = New cv.Scalar(mm.maxVal)
            plot.Run(src)
            dst3 = plot.dst2
            labels(3) = "Histogram maxVal = " + Format(mm.maxVal, fmt1) + " histogram mean = " + Format(mean, fmt1)
            mats.mat(3) = peak.histogram.ConvertScaleAbs(255)

            mats.mat(0) = peak.dst2(peak.mini.rect)
            mats.mat(1) = peak.dst3(peak.mini.rect)
            mats.Run(emptyMat)
            dst2 = mats.dst2
        End Sub
    End Class









    Public Class XO_MiniCloud_RotateSinglePass : Inherits TaskParent
        Dim peak As New XO_MiniCloud_Rotate
        Public Sub New()
            taskAlg.accRadians.Y = -cv.Cv2.PI
            desc = "Same operation as New MiniCloud_RotateAngle but in a single pass."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static ySlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis (degrees)")
            peak.mini.Run(src)

            Dim maxHist = Single.MinValue
            Dim bestAngle As Integer
            Dim bestLoc As cv.Point
            Dim mm As mmData
            For i = ySlider.minimum To ySlider.maximum - 1
                peak.Run(peak.mini.dst3)
                ySlider.Value = i
                mm = GetMinMax(peak.histogram)
                If mm.maxVal > maxHist Then
                    maxHist = mm.maxVal
                    bestAngle = i
                    bestLoc = mm.maxLoc
                End If
            Next
            peak.Run(peak.mini.dst3)
            taskAlg.accRadians.Y = bestAngle
            dst2 = peak.dst2
            dst3 = peak.dst3

            SetTrueText("Peak concentration in the histogram is at angle " + CStr(bestAngle) + " degrees", 3)
        End Sub
    End Class




    Public Class XO_OpAuto_XRange : Inherits TaskParent
        Public histogram As New cv.Mat
        Dim adjustedCount As Integer = 0
        Public Sub New()
            labels(2) = "Optimized top view to show as many samples as possible."
            desc = "Automatically adjust the X-Range option of the pointcloud to maximize visible pixels"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim expectedCount = taskAlg.depthMask.CountNonZero

            Dim diff = Math.Abs(expectedCount - adjustedCount)

            ' the input is a histogram.  If standaloneTest(), go get one...
            If standaloneTest() Then
                cv.Cv2.CalcHist({taskAlg.pointCloud}, taskAlg.channelsTop, New cv.Mat, histogram, 2, taskAlg.bins2D, taskAlg.rangesTop)
                histogram.Row(0).SetTo(0)
                dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
                dst3 = histogram.Threshold(taskAlg.projectionThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
                src = histogram
            End If

            histogram = src
            adjustedCount = histogram.Sum()(0)

            strOut = "Adjusted = " + vbTab + CStr(adjustedCount) + "k" + vbCrLf +
                 "Expected = " + vbTab + CStr(expectedCount) + "k" + vbCrLf +
                 "Diff = " + vbTab + vbTab + CStr(diff) + vbCrLf +
                 "xRange = " + vbTab + Format(taskAlg.xRange, fmt3)

            If taskAlg.useXYRange Then
                Dim saveOptionState = taskAlg.optionsChanged ' the xRange and yRange change frequently.  It is safe to ignore it.
                Dim leftGap = histogram.Col(0).CountNonZero
                Dim rightGap = histogram.Col(histogram.Width - 1).CountNonZero
                'If leftGap = 0 And rightGap = 0 And taskAlg.gOptions.XRangeBar.Value > 3 Then
                '    taskAlg.gOptions.XRangeBar.Value -= 1
                'Else
                '    If adjustedCount < expectedCount Then taskAlg.gOptions.XRangeBar.Value += 1 Else taskAlg.gOptions.XRangeBar.Value -= 1
                'End If
                taskAlg.optionsChanged = saveOptionState
            End If

            SetTrueText(strOut, 3)
        End Sub
    End Class





    Public Class XO_OpAuto_YRange : Inherits TaskParent
        Public histogram As New cv.Mat
        Dim adjustedCount As Integer = 0
        Public Sub New()
            labels(2) = "Optimized side view to show as much as possible."
            desc = "Automatically adjust the Y-Range option of the pointcloud to maximize visible pixels"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim expectedCount = taskAlg.depthMask.CountNonZero

            Dim diff = Math.Abs(expectedCount - adjustedCount)

            ' the input is a histogram.  If standaloneTest(), go get one...
            If standaloneTest() Then
                cv.Cv2.CalcHist({taskAlg.pointCloud}, taskAlg.channelsSide, New cv.Mat, histogram, 2, taskAlg.bins2D, taskAlg.rangesSide)
                histogram.Col(0).SetTo(0)
                dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
                dst3 = histogram.Threshold(taskAlg.projectionThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
                src = histogram
            End If

            histogram = src
            adjustedCount = histogram.Sum()(0)

            strOut = "Adjusted = " + vbTab + CStr(adjustedCount) + "k" + vbCrLf +
                 "Expected = " + vbTab + CStr(expectedCount) + "k" + vbCrLf +
                 "Diff = " + vbTab + vbTab + CStr(diff) + vbCrLf +
                 "yRange = " + vbTab + Format(taskAlg.yRange, fmt3)

            If taskAlg.useXYRange Then
                Dim saveOptionState = taskAlg.optionsChanged ' the xRange and yRange change frequently.  It is safe to ignore it.
                Dim topGap = histogram.Row(0).CountNonZero
                Dim botGap = histogram.Row(histogram.Height - 1).CountNonZero
                'If topGap = 0 And botGap = 0 And taskAlg.gOptions.YRangeSlider.Value > 3 Then
                '    taskAlg.gOptions.YRangeSlider.Value -= 1
                'Else
                '    If adjustedCount < expectedCount Then taskAlg.gOptions.YRangeSlider.Value += 1 Else taskAlg.gOptions.YRangeSlider.Value -= 1
                'End If
                taskAlg.optionsChanged = saveOptionState
            End If
            SetTrueText(strOut, 3)
        End Sub
    End Class





    Public Class XO_Mat_ToList : Inherits TaskParent
        Dim autoX As New XO_OpAuto_XRange
        Dim histTop As New Projection_HistTop
        Public Sub New()
            desc = "Convert a Mat to List of points in 2 ways to measure which is better"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            histTop.Run(src)

            autoX.Run(histTop.histogram)
            dst2 = histTop.histogram.Threshold(taskAlg.projectionThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

            Dim ptList As New List(Of cv.Point)
            If taskAlg.gOptions.DebugCheckBox.Checked Then
                For y = 0 To dst2.Height - 1
                    For x = 0 To dst2.Width - 1
                        If dst2.Get(Of Byte)(y, x) <> 0 Then ptList.Add(New cv.Point(x, y))
                    Next
                Next
            Else
                Dim points = dst2.FindNonZero()
                For i = 0 To points.Rows - 1
                    ptList.Add(points.Get(Of cv.Point)(i, 0))
                Next
            End If

            labels(2) = "There were " + CStr(ptList.Count) + " points identified"
        End Sub
    End Class










    Public Class XO_RedPrep_BasicsCalcHist : Inherits TaskParent
        Public Sub New()
            desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim histogram As New cv.Mat

            Dim channels() As Integer = {0}
            Select Case taskAlg.reductionName
                Case "X Reduction"
                    dst0 = taskAlg.pcSplit(0)
                Case "Y Reduction"
                    dst0 = taskAlg.pcSplit(1)
                Case "Z Reduction"
                    dst0 = taskAlg.pcSplit(2)
                Case "XY Reduction"
                    dst0 = taskAlg.pcSplit(0) + taskAlg.pcSplit(1)
                    channels = {0, 1}
                Case "XZ Reduction"
                    dst0 = taskAlg.pcSplit(0) + taskAlg.pcSplit(2)
                    channels = {0, 1}
                Case "YZ Reduction"
                    dst0 = taskAlg.pcSplit(1) + taskAlg.pcSplit(2)
                    channels = {0, 1}
                Case "XYZ Reduction"
                    dst0 = taskAlg.pcSplit(0) + taskAlg.pcSplit(1) + taskAlg.pcSplit(2)
                    channels = {0, 1}
            End Select

            Dim mm = GetMinMax(dst0)
            Dim ranges = New cv.Rangef() {New cv.Rangef(mm.minVal, mm.maxVal)}
            cv.Cv2.CalcHist({dst0}, channels, taskAlg.depthMask, histogram, 1, {taskAlg.histogramBins}, ranges)

            Dim histArray(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            For i = 0 To histArray.Count - 1
                histArray(i) = i
            Next

            histogram = cv.Mat.FromPixelData(histogram.Rows, 1, cv.MatType.CV_32F, histArray)
            cv.Cv2.CalcBackProject({dst0}, {0}, histogram, dst1, ranges)
            dst1.ConvertTo(dst2, cv.MatType.CV_8U)
            dst3 = PaletteFull(dst2)
            dst3.SetTo(0, taskAlg.noDepthMask)

            labels(2) = "Pointcloud data backprojection to " + CStr(taskAlg.histogramBins) + " classes."
        End Sub
    End Class






    Public Class XO_Contour_Depth : Inherits TaskParent
        Dim options As New Options_Contours
        Public depthContourList As New List(Of contourData)
        Public depthcontourMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        Dim sortContours As New Contour_Sort
        Dim prep As New RedPrep_Basics
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            labels(3) = "ShowPalette output of the depth contours in dst2"
            desc = "Isolate the contours in the output of BackProject_Basics_Depth"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim mode = options.options2.ApproximationMode
            prep.Run(src)
            dst3.ConvertTo(dst1, cv.MatType.CV_32SC1)
            cv.Cv2.FindContours(dst1, sortContours.allContours, Nothing, cv.RetrievalModes.FloodFill, mode)
            If sortContours.allContours.Count <= 1 Then Exit Sub

            sortContours.Run(src)

            depthContourList = sortContours.contourList
            depthcontourMap = sortContours.contourMap
            labels(2) = sortContours.labels(2)
            dst2 = sortContours.dst2

            dst2.SetTo(0)
            For i = 0 To Math.Min(depthContourList.Count, 6) - 1
                Dim contour = depthContourList(i)
                dst2(contour.rect).SetTo(contour.ID Mod 255, contour.mask)
                Dim str = CStr(contour.ID) + " ID" + vbCrLf + CStr(contour.pixels) + " pixels" + vbCrLf +
                      Format(contour.depth, fmt3) + "m depth" + vbCrLf + Format(contour.mm.range, fmt3) + " range in m"
                SetTrueText(str, contour.maxDist, 2)
            Next

            Static saveTrueData As New List(Of TrueText)
            If taskAlg.heartBeatLT Then
                saveTrueData = New List(Of TrueText)(trueData)
            Else
                trueData = New List(Of TrueText)(saveTrueData)
            End If

            dst3 = PaletteFull(dst2)
            labels(2) = "CV_8U format of the " + CStr(depthContourList.Count) + " depth contours"
        End Sub
    End Class





    Public Class XO_TrackLine_Basics : Inherits TaskParent
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Track the line regions with RedCloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1.SetTo(0)
            For Each lp In taskAlg.lines.lpList
                dst1.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth + 1, cv.LineTypes.Link8)
            Next

            dst2 = runRedList(dst1, labels(2), Not dst1)

            dst3.SetTo(0)
            For Each lp In taskAlg.lines.lpList
                vbc.DrawLine(dst3, lp.p1, lp.p2, white, taskAlg.lineWidth)
                Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
                DrawCircle(dst3, center, taskAlg.DotSize, taskAlg.highlight, -1)
            Next
        End Sub
    End Class






    Public Class XO_TrackLine_Map : Inherits TaskParent
        Dim lTrack As New XO_TrackLine_Basics
        Public Sub New()
            If taskAlg.bricks Is Nothing Then taskAlg.bricks = New Brick_Basics
            taskAlg.gOptions.CrossHairs.Checked = False
            desc = "Show the gridMap and fpMap (features points) "
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            lTrack.Run(src)
            dst2 = lTrack.dst2
            dst1 = lTrack.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
            labels(2) = lTrack.labels(2)

            Dim count As Integer
            dst3.SetTo(0)
            Dim histarray(taskAlg.redList.oldrclist.Count - 1) As Single
            Dim histogram As New cv.Mat
            For Each brick In taskAlg.bricks.brickList
                cv.Cv2.CalcHist({taskAlg.redList.rcMap(brick.rect)}, {0}, emptyMat, histogram, 1, {taskAlg.redList.oldrclist.Count},
                             New cv.Rangef() {New cv.Rangef(1, taskAlg.redList.oldrclist.Count)})

                Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)
                ' if multiple lines intersect a grid rect, choose the largest redcloud cell containing them.
                ' The largest will be the index of the first non-zero histogram entry.
                For j = 1 To histarray.Count - 1
                    If histarray(j) > 0 Then
                        Dim rc = taskAlg.redList.oldrclist(j)
                        dst3(brick.rect).SetTo(rc.color)
                        ' dst3(brick.rect).SetTo(0, Not dst1(brick.rect))
                        count += 1
                        Exit For
                    End If
                Next
            Next

            labels(3) = "The redCloud cells are completely covered by " + CStr(count) + " bricks"
        End Sub
    End Class





    Public Class XO_TrackLine_BasicsSimple : Inherits TaskParent
        Dim lp As New lpData
        Dim match As New Match_Basics
        Public rawLines As New Line_Core
        Dim matchRect As cv.Rect
        Public Sub New()
            desc = "Track an individual line as best as possible."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lplist = taskAlg.lines.lpList

            If standalone Then
                If lplist(0).length > lp.length Then
                    lp = lplist(0)
                    matchRect = lp.rect
                    match.template = src(matchRect).Clone
                End If
            End If

            If matchRect.Width <= 1 Then Exit Sub ' nothing yet...
            match.Run(src)
            matchRect = match.newRect

            If standaloneTest() Then
                dst2 = src
                DrawCircle(dst2, match.newCenter, taskAlg.DotSize, white)
                dst2.Rectangle(matchRect, taskAlg.highlight, taskAlg.lineWidth)
                dst3 = match.dst0.Normalize(0, 255, cv.NormTypes.MinMax)
                SetTrueText(Format(match.correlation, fmt3), match.newCenter)
            End If

            rawLines.Run(src(matchRect))
            If rawLines.lpList.Count > 0 Then lp = rawLines.lpList(0)
            dst2(matchRect).Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
        End Sub
    End Class





    Public Class XO_TrackLine_BasicsOld : Inherits TaskParent
        Public lpInput As lpData
        Public foundLine As Boolean
        Dim match As New LineEnds_Correlation
        Public rawLines As New Line_Core
        Public Sub New()
            desc = "Track an individual line as best as possible."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lplist = taskAlg.lines.lpList
            If standalone And foundLine = False Then lpInput = taskAlg.lineLongest

            Static subsetrect = lpInput.rect
            If subsetrect.width <= dst2.Height / 10 Then
                lpInput = taskAlg.lineLongest
                subsetrect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
                Exit Sub
            End If

            Dim lpLast = lpInput

            Dim lpRectMap = XO_Line_CoreNew.createMap()
            Dim index = lpRectMap.Get(Of Byte)(lpInput.ptCenter.Y, lpInput.ptCenter.X)
            If index > 0 Then
                Dim lp = lplist(index - 1)
                If lpInput.index = lp.index Then
                    foundLine = True
                Else
                    match.lpInput = lpInput
                    match.Run(src)

                    foundLine = match.p1Correlation >= taskAlg.fCorrThreshold And match.p2Correlation >= taskAlg.fCorrThreshold
                    If foundLine Then
                        lpInput = match.lpInput
                        subsetrect = lpInput.rect
                    End If
                End If
            Else
                rawLines.Run(src(subsetrect))
                dst3(subsetrect) = rawLines.dst2(subsetrect)
                If rawLines.lpList.Count > 0 Then
                    Dim p1 = New cv.Point(CInt(rawLines.lpList(0).p1.X + subsetrect.X), CInt(rawLines.lpList(0).p1.Y + subsetrect.Y))
                    Dim p2 = New cv.Point(CInt(rawLines.lpList(0).p2.X + subsetrect.X), CInt(rawLines.lpList(0).p2.Y + subsetrect.Y))
                    lpInput = New lpData(p1, p2)
                Else
                    lpInput = lplist(0)
                End If

                Dim deltaX1 = Math.Abs(taskAlg.gravityIMU.pE1.X - lpInput.pE1.X)
                Dim deltaX2 = Math.Abs(taskAlg.gravityIMU.pE2.X - lpInput.pE2.X)
                If Math.Abs(deltaX1 - deltaX2) > taskAlg.gravityBasics.options.pixelThreshold Then
                    lpInput = taskAlg.lineLongest
                End If
                subsetrect = lpInput.rect
            End If

            dst2 = src
            dst2.Line(lpInput.p1, lpInput.p2, taskAlg.highlight, taskAlg.lineWidth + 1, taskAlg.lineType)
            dst2.Rectangle(subsetrect, taskAlg.highlight, taskAlg.lineWidth)
        End Sub
    End Class





    Public Class XO_TrackLine_BasicsSave : Inherits TaskParent
        Dim match As New Match_Basics
        Dim matchRect As cv.Rect
        Public rawLines As New Line_Core
        Dim lplist As List(Of lpData)
        Dim knn As New KNN_NNBasics
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            OptionParent.FindSlider("KNN Dimension").Value = 6
            desc = "Track an individual line as best as possible."
        End Sub
        Private Function restartLine(src As cv.Mat) As lpData
            For Each lpTemp In lplist
                If Math.Abs(taskAlg.lineGravity.angle - lpTemp.angle) < taskAlg.angleThreshold Then
                    matchRect = lpTemp.rect
                    match.template = src(matchRect).Clone
                    Return lpTemp
                End If
            Next
            Return New lpData
        End Function
        Private Sub prepEntry(knnList As List(Of Single), lpNext As lpData)
            Dim brick1 = taskAlg.gridMap.Get(Of Integer)(lpNext.p1.Y, lpNext.p1.X)
            Dim brick2 = taskAlg.gridMap.Get(Of Integer)(lpNext.p2.Y, lpNext.p2.X)
            knnList.Add(lpNext.p1.X)
            knnList.Add(lpNext.p1.Y)
            knnList.Add(lpNext.p2.X)
            knnList.Add(lpNext.p2.Y)
            knnList.Add(brick1)
            knnList.Add(brick2)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            lplist = taskAlg.lines.lpList

            Static lp As New lpData, lpLast As lpData
            lpLast = lp

            If match.correlation < taskAlg.fCorrThreshold Or matchRect.Width <= 1 Then ' Or taskAlg.heartBeatLT 
                lp = restartLine(src)
            End If

            match.Run(src)

            knn.trainInput.Clear()
            For Each nextlp In taskAlg.lines.lpList
                prepEntry(knn.trainInput, nextlp)
            Next

            knn.queries.Clear()
            prepEntry(knn.queries, lp)
            knn.Run(emptyMat)

            lp = taskAlg.lines.lpList(knn.result(0, 0))
            labels(3) = "Index of the current lp = " + CStr(lp.index - 1)

            If standaloneTest() Then
                dst2 = src.Clone
                DrawCircle(dst2, match.newCenter, taskAlg.DotSize, white)
                dst2.Rectangle(lp.rect, taskAlg.highlight, taskAlg.lineWidth)
                dst2.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
                dst3 = match.dst0.Normalize(0, 255, cv.NormTypes.MinMax)
                SetTrueText(Format(match.correlation, fmt3), match.newCenter)

                dst2.Rectangle(lp.rect, taskAlg.highlight, taskAlg.lineWidth)
            End If

            Dim lpRectMap = XO_Line_CoreNew.createMap()
            dst1 = PaletteBlackZero(lpRectMap)
            dst1.Circle(lp.ptCenter, taskAlg.DotSize, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)

            labels(2) = "Selected line has a correlation of " + Format(match.correlation, fmt3) + " with the previous frame."
        End Sub
    End Class






    Public Class XO_BrickPoint_VetLines : Inherits TaskParent
        Dim bPoint As New BrickPoint_Basics
        Public lpList As New List(Of lpData)
        Public Sub New()
            desc = "Vet the lines - make sure there are at least 2 brickpoints in the line."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone
            dst3 = src

            bPoint.Run(src.Clone)

            Dim pointsPerLine(taskAlg.gridRects.Count) As List(Of Integer)
            Dim lpRectMap = XO_Line_CoreNew.createMap()
            For Each pt In bPoint.ptList
                Dim index = lpRectMap.Get(Of Byte)(pt.Y, pt.X)
                If index > 0 And index < taskAlg.lines.lpList.Count Then
                    Dim lp = taskAlg.lines.lpList(index)
                    If pointsPerLine(lp.index) Is Nothing Then pointsPerLine(lp.index) = New List(Of Integer)
                    pointsPerLine(lp.index).Add(lp.index)
                    dst2.Circle(pt, taskAlg.DotSize * 3, lp.color, -1, taskAlg.lineType)
                End If
            Next

            lpList.Clear()
            For Each ppl In pointsPerLine
                If ppl Is Nothing Then Continue For
                If ppl.Count > 1 Then lpList.Add(taskAlg.lines.lpList(ppl(0)))
            Next

            dst3 = src
            For Each lp In lpList
                dst3.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
            Next
            labels(3) = CStr(lpList.Count) + " lines were confirmed with brickpoints"
        End Sub
    End Class




    Public Class XO_Gravity_Basics1 : Inherits TaskParent
        Public options As New Options_Features
        Dim gravityRaw As New Gravity_Basics
        Public gravityMatch As New XO_Line_MatchGravity
        Public gravityRGB As lpData
        Dim nearest As New XO_Line_FindNearest
        Public Sub New()
            desc = "Use the slope of the longest RGB line to figure out if camera moved enough to obtain the IMU gravity vector."
        End Sub
        Private Shared Sub showVec(dst As cv.Mat, vec As lpData)
            dst.Line(vec.p1, vec.p2, taskAlg.highlight, taskAlg.lineWidth * 2, taskAlg.lineType)
            Dim gIndex = taskAlg.gridMap.Get(Of Integer)(vec.p1.Y, vec.p1.X)
            Dim firstRect = taskAlg.gridNabeRects(gIndex)
            gIndex = taskAlg.gridMap.Get(Of Integer)(vec.p2.Y, vec.p2.X)
            Dim lastRect = taskAlg.gridNabeRects(gIndex)
            dst.Rectangle(firstRect, taskAlg.highlight, taskAlg.lineWidth)
            dst.Rectangle(lastRect, taskAlg.highlight, taskAlg.lineWidth)
        End Sub
        Public Shared Sub showVectors(dst As cv.Mat)
            dst.Line(taskAlg.lineGravity.p1, taskAlg.lineGravity.p2, white, taskAlg.lineWidth, taskAlg.lineType)
            dst.Line(taskAlg.lineHorizon.p1, taskAlg.lineHorizon.p2, white, taskAlg.lineWidth, taskAlg.lineType)
            showVec(dst, taskAlg.lineLongest)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            gravityRaw.Run(emptyMat)
            gravityMatch.Run(src)
            labels(2) = CStr(gravityMatch.gLines.Count) + " of the lines found were parallel to gravity."

            Static RGBcandidate As New lpData

            Dim stillPresent As Integer
            Dim lpRectMap = XO_Line_CoreNew.createMap()
            If RGBcandidate.length = 0 Then
                If gravityMatch.gLines.Count > 0 Then RGBcandidate = gravityMatch.gLines(0)
            Else
                stillPresent = lpRectMap.Get(Of Byte)(RGBcandidate.ptCenter.Y, RGBcandidate.ptCenter.X)
            End If

            If stillPresent Then
                nearest.lpInput = RGBcandidate
                nearest.Run(src)
                RGBcandidate = nearest.lpOutput
                Dim deltaX1 = Math.Abs(taskAlg.lineGravity.pE1.X - RGBcandidate.pE1.X)
                Dim deltaX2 = Math.Abs(taskAlg.lineGravity.pE2.X - RGBcandidate.pE2.X)
                If Math.Abs(deltaX1 - deltaX2) > options.pixelThreshold Then
                    taskAlg.lineGravity = taskAlg.gravityIMU
                    RGBcandidate = New lpData
                    If gravityMatch.gLines.Count > 0 Then RGBcandidate = gravityMatch.gLines(0)
                End If
            Else
                taskAlg.lineGravity = taskAlg.gravityIMU
                RGBcandidate = New lpData
                If gravityMatch.gLines.Count > 0 Then RGBcandidate = gravityMatch.gLines(0)
            End If

            taskAlg.lineHorizon = Line_PerpendicularTest.computePerp(taskAlg.lineGravity)

            gravityRGB = RGBcandidate

            If standaloneTest() Then
                dst2.SetTo(0)
                showVectors(dst2)
                dst3 = taskAlg.lines.dst3
                labels(3) = taskAlg.lines.labels(3)
            End If
        End Sub
    End Class




    Public Class XO_FPoly_Basics : Inherits TaskParent
        Public resync As Boolean
        Public resyncCause As String
        Public resyncFrames As Integer
        Public maskChangePercent As Single
        Dim feat As New XO_FPoly_TopFeatures
        Public sides As New XO_FPoly_Sides
        Dim options As New Options_Features
        Public Sub New()
            taskAlg.featureOptions.FeatureSampleSize.Value = 30
            If dst2.Width >= 640 Then OptionParent.FindSlider("Resync if feature moves > X pixels").Value = 15
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"", "Feature Polygon with perpendicular lines for center of rotation.", "Feature polygon created by highest generation counts",
                  "Ordered Feature polygons of best features - white is original, yellow latest"}
            desc = "Build a Feature polygon with the top generation counts of the good features"
        End Sub
        Public Shared Sub DrawFPoly(ByRef dst As cv.Mat, poly As List(Of cv.Point2f), color As cv.Scalar)
            Dim minMod = Math.Min(poly.Count, taskAlg.polyCount)
            For i = 0 To minMod - 1
                vbc.DrawLine(dst, poly(i), poly((i + 1) Mod minMod), color)
            Next
        End Sub
        Public Shared Sub DrawPolys(dst As cv.Mat, poly As fPolyData)
            DrawFPoly(dst, poly.prevPoly, cv.Scalar.White)
            DrawFPoly(dst, poly.currPoly, cv.Scalar.Yellow)
            dst.Line(poly.currPoly(poly.polyPrevSideIndex), poly.currPoly((poly.polyPrevSideIndex + 1) Mod taskAlg.polyCount),
                 taskAlg.highlight, taskAlg.lineWidth * 3, taskAlg.lineType)
            dst.Line(poly.prevPoly(poly.polyPrevSideIndex), poly.prevPoly((poly.polyPrevSideIndex + 1) Mod taskAlg.polyCount),
                 taskAlg.highlight, taskAlg.lineWidth * 3, taskAlg.lineType)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If taskAlg.firstPass Then sides.prevImage = src.Clone
            sides.options.Run()

            feat.Run(src)
            dst2 = src.Clone
            sides.currPoly = New List(Of cv.Point2f)(feat.topFeatures)
            If sides.currPoly.Count < taskAlg.polyCount Then Exit Sub
            sides.Run(src)
            dst3 = sides.dst2

            For i = 0 To sides.currPoly.Count - 1
                SetTrueText(CStr(i), sides.currPoly(i), 3)
                vbc.DrawLine(dst2, sides.currPoly(i), sides.currPoly((i + 1) Mod sides.currPoly.Count))
            Next

            Dim causes As String = ""
            If Math.Abs(sides.rotateAngle * 57.2958) > 10 Then
                resync = True
                causes += " - Rotation angle exceeded threshold."
                sides.rotateAngle = 0
            End If
            causes += vbCrLf

            If taskAlg.optionsChanged Then
                resync = True
                causes += " - Options changed"
            End If
            causes += vbCrLf

            If resyncFrames > sides.options.autoResyncAfterX Then
                resync = True
                causes += " - More than " + CStr(sides.options.autoResyncAfterX) + " frames without resync"
            End If
            causes += vbCrLf

            If Math.Abs(sides.currLengths.Sum() - sides.prevLengths.Sum()) > sides.options.removeThreshold * taskAlg.polyCount Then
                resync = True
                causes += " - The top " + CStr(taskAlg.polyCount) + " vertices have moved because of the generation counts"
            Else
                If Math.Abs(sides.prevFLineLen - sides.currFLineLen) > sides.options.removeThreshold Then
                    resync = True
                    causes += " - The Feature polygon's longest side (FLine) changed more than the threshold of " +
                              CStr(sides.options.removeThreshold) + " pixels"
                End If
            End If
            causes += vbCrLf

            If resync Or sides.prevPoly.Count <> taskAlg.polyCount Or taskAlg.optionsChanged Then
                sides.prevPoly = New List(Of cv.Point2f)(sides.currPoly)
                sides.prevLengths = New List(Of Single)(sides.currLengths)
                sides.prevSideIndex = sides.prevLengths.IndexOf(sides.prevLengths.Max)
                sides.prevImage = src.Clone
                resyncFrames = 0
                resyncCause = causes
            End If
            resyncFrames += 1

            strOut = "Rotation: " + Format(sides.rotateAngle * 57.2958, fmt1) + " degrees" + vbCrLf
            strOut += "Translation: " + CStr(CInt(sides.centerShift.X)) + ", " + CStr(CInt(sides.centerShift.Y)) + vbCrLf
            strOut += "Frames since last resync: " + Format(resyncFrames, "000") + vbCrLf + vbCrLf
            strOut += "Resync last caused by: " + vbCrLf + resyncCause

            For Each pt In sides.currPoly ' topFeatures.stable.goodCounts
                Dim index = feat.stable.basics.ptList.IndexOf(pt)
                If index >= 0 Then
                    pt = feat.stable.basics.ptList(index)
                    Dim g = feat.stable.basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                    SetTrueText(CStr(g), pt)
                End If
            Next

            SetTrueText(strOut, 1)
            resync = False
        End Sub
    End Class







    Public Class XO_FPoly_Sides : Inherits TaskParent
        Public currPoly As New List(Of cv.Point2f)
        Public currSideIndex As Integer
        Public currLengths As New List(Of Single)
        Public currFLineLen As Single
        Public mpCurr As lpData

        Public prevPoly As New List(Of cv.Point2f)
        Public prevSideIndex As Integer
        Public prevLengths As New List(Of Single)
        Public prevFLineLen As Single
        Public mpPrev As lpData

        Public prevImage As cv.Mat

        Public rotateCenter As cv.Point2f
        Public rotateAngle As Single
        Public centerShift As cv.Point2f

        Public options As New Options_FPoly
        Dim near As New XO_Line_Nearest
        Public rotatePoly As New XO_Rotate_PolyQT
        Dim newPoly As New List(Of cv.Point2f)
        Dim random As New Random_Basics
        Public Sub New()
            labels(2) = "White is the original FPoly and yellow is the current FPoly."
            desc = "Compute the lengths of each side in a polygon"
        End Sub
        Public Shared Sub DrawFatLine(dst As cv.Mat, lp As lpData, color As cv.Scalar)
            dst.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth * 3, taskAlg.lineType)
        End Sub
        Public Shared Sub DrawFatLine(p1 As cv.Point2f, p2 As cv.Point2f, dst As cv.Mat, color As cv.Scalar)
            dst.Line(p1, p2, taskAlg.highlight, taskAlg.lineWidth * 3, taskAlg.lineType)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If taskAlg.firstPass Then prevImage = src.Clone
            options.Run()

            If standaloneTest() And taskAlg.heartBeat Then
                random.Run(src)
                currPoly = New List(Of cv.Point2f)(random.PointList)
            End If

            dst2.SetTo(0)
            currLengths.Clear()
            For i = 0 To currPoly.Count - 2
                currLengths.Add(currPoly(i).DistanceTo(currPoly(i + 1)))
            Next
            currSideIndex = currLengths.IndexOf(currLengths.Max)

            If taskAlg.firstPass Then
                prevPoly = New List(Of cv.Point2f)(currPoly)
                prevLengths = New List(Of Single)(currLengths)
                prevSideIndex = prevLengths.IndexOf(prevLengths.Max)
            End If

            If prevPoly.Count = 0 Then Exit Sub

            mpPrev = New lpData(prevPoly(prevSideIndex), prevPoly((prevSideIndex + 1) Mod taskAlg.polyCount))
            mpCurr = New lpData(currPoly(currSideIndex), currPoly((currSideIndex + 1) Mod taskAlg.polyCount))

            prevFLineLen = mpPrev.p1.DistanceTo(mpPrev.p2)
            currFLineLen = mpCurr.p1.DistanceTo(mpCurr.p2)

            Dim d1 = mpPrev.p1.DistanceTo(mpCurr.p1)
            Dim d2 = mpPrev.p2.DistanceTo(mpCurr.p2)

            Dim newNear As lpData
            If d1 < d2 Then
                centerShift = New cv.Point2f(mpPrev.p1.X - mpCurr.p1.X, mpPrev.p1.Y - mpCurr.p1.Y)
                rotateCenter = mpPrev.p1
                newNear = New lpData(mpPrev.p2, mpCurr.p2)
            Else
                centerShift = New cv.Point2f(mpPrev.p2.X - mpCurr.p2.X, mpPrev.p2.Y - mpCurr.p2.Y)
                rotateCenter = mpPrev.p2
                newNear = New lpData(mpPrev.p1, mpCurr.p1)
            End If

            Dim transPoly As New List(Of cv.Point2f)
            For i = 0 To currPoly.Count - 1
                transPoly.Add(New cv.Point2f(currPoly(i).X - centerShift.X, currPoly(i).Y - centerShift.Y))
            Next
            newNear.p1 = New cv.Point2f(newNear.p1.X - centerShift.X, newNear.p1.Y - centerShift.Y)
            newNear.p2 = New cv.Point2f(newNear.p2.X - centerShift.X, newNear.p2.Y - centerShift.Y)
            rotateCenter = New cv.Point2f(rotateCenter.X - centerShift.X, rotateCenter.Y - centerShift.Y)

            strOut = "No rotation" + vbCrLf
            rotateAngle = 0
            If d1 <> d2 Then
                If newNear.p1.DistanceTo(newNear.p2) > options.removeThreshold Then
                    near.lp = mpPrev
                    near.pt = newNear.p1
                    near.Run(src)
                    dst1.Line(near.pt, near.nearPoint, cv.Scalar.Red, taskAlg.lineWidth + 5, taskAlg.lineType)

                    Dim hypotenuse = rotateCenter.DistanceTo(near.pt)
                    rotateAngle = -Math.Asin(near.nearPoint.DistanceTo(near.pt) / hypotenuse)
                    If Single.IsNaN(rotateAngle) Then rotateAngle = 0
                    strOut = "Angle is " + Format(rotateAngle * 57.2958, fmt1) + " degrees" + vbCrLf
                End If
            End If
            strOut += "Translation (shift) is " + Format(-centerShift.X, fmt0) + ", " + Format(-centerShift.Y, fmt0)

            If Math.Abs(rotateAngle) > 0 Then
                rotatePoly.rotateCenter = rotateCenter
                rotatePoly.rotateAngle = rotateAngle
                rotatePoly.poly.Clear()
                rotatePoly.poly.Add(newNear.p1)
                rotatePoly.Run(src)

                If near.nearPoint.DistanceTo(rotatePoly.poly(0)) > newNear.p1.DistanceTo(rotatePoly.poly(0)) Then rotateAngle *= -1

                rotatePoly.rotateAngle = rotateAngle
                rotatePoly.poly = New List(Of cv.Point2f)(transPoly)
                rotatePoly.Run(src)
                newPoly = New List(Of cv.Point2f)(rotatePoly.poly)
            End If

            XO_FPoly_Basics.DrawFPoly(dst2, prevPoly, white)
            XO_FPoly_Basics.DrawFPoly(dst2, currPoly, cv.Scalar.Yellow)
            DrawFatLine(dst2, mpPrev, white)
            DrawFatLine(dst2, mpCurr, taskAlg.highlight)
        End Sub
    End Class










    Public Class XO_FPoly_BasicsOriginal : Inherits TaskParent
        Public fPD As New fPolyData
        Public resyncImage As cv.Mat
        Public resync As Boolean
        Public resyncCause As String
        Public resyncFrames As Integer
        Public maskChangePercent As Single

        Dim feat As New XO_FPoly_TopFeatures
        Public options As New Options_FPoly
        Public center As Object
        Dim optionsEx As New Options_Features
        Public Sub New()
            center = New XO_FPoly_Center
            taskAlg.featureOptions.FeatureSampleSize.Value = 30
            If dst2.Width >= 640 Then OptionParent.FindSlider("Resync if feature moves > X pixels").Value = 15
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"", "Feature Polygon with perpendicular lines for center of rotation.",
                      "Feature polygon created by highest generation counts",
                  "Ordered Feature polygons of best features - white is original, yellow latest"}
            desc = "Build a Feature polygon with the top generation counts of the good features"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.firstPass Then resyncImage = src.Clone
            options.Run()
            optionsEx.Run()

            feat.Run(src)
            dst2 = feat.dst2
            dst1 = feat.dst3
            fPD.currPoly = New List(Of cv.Point2f)(feat.topFeatures)

            If taskAlg.optionsChanged Then fPD = New fPolyData(fPD.currPoly)
            If fPD.currPoly.Count < taskAlg.polyCount Then Exit Sub

            fPD.computeCurrLengths()
            For i = 0 To fPD.currPoly.Count - 1
                SetTrueText(CStr(i), fPD.currPoly(i), 1)
            Next
            If taskAlg.firstPass Then fPD.lengthPrevious = New List(Of Single)(fPD.currLength)

            center.fPD = fPD
            center.Run(src)
            fPD = center.fPD
            dst1 = (dst1 Or center.dst2).tomat
            dst0 = center.dst3

            fPD.jitterTest(dst2, Me) ' the feature line has not really moved.

            Dim causes As String = ""
            If Math.Abs(fPD.rotateAngle * 57.2958) > 10 Then
                resync = True
                causes += " - Rotation angle exceeded threshold."
                fPD.rotateAngle = 0
            End If
            causes += vbCrLf

            If maskChangePercent > 0.2 Then
                resync = True
                causes += " - Difference of startFrame and current frame exceeded 20% of image size"
            End If
            causes += vbCrLf

            If taskAlg.optionsChanged Then
                resync = True
                causes += " - Options changed"
            End If
            causes += vbCrLf

            If resyncFrames > options.autoResyncAfterX Then
                resync = True
                causes += " - More than " + CStr(options.autoResyncAfterX) + " frames without resync"
            End If
            causes += vbCrLf

            If Math.Abs(fPD.currLength.Sum() - fPD.lengthPrevious.Sum()) > options.removeThreshold * taskAlg.polyCount Then
                resync = True
                causes += " - The top " + CStr(taskAlg.polyCount) + " vertices have moved because of the generation counts"
            Else
                If fPD.computeFLineLength() > options.removeThreshold Then
                    resync = True
                    causes += " - The Feature polygon's longest side (FLine) changed more than the threshold of " +
                              CStr(options.removeThreshold) + " pixels"
                End If
            End If
            causes += vbCrLf

            If resync Or fPD.prevPoly.Count <> taskAlg.polyCount Or taskAlg.optionsChanged Then
                fPD.resync()
                resyncImage = src.Clone
                resyncFrames = 0
                resyncCause = causes
            End If
            resyncFrames += 1

            XO_FPoly_Basics.DrawFPoly(dst2, fPD.currPoly, white)
            XO_FPoly_Basics.DrawPolys(dst1, fPD)
            For i = 0 To fPD.prevPoly.Count - 1
                SetTrueText(CStr(i), fPD.currPoly(i), 1)
                SetTrueText(CStr(i), fPD.currPoly(i), 1)
            Next

            strOut = "Rotation: " + Format(fPD.rotateAngle * 57.2958, fmt1) + " degrees" + vbCrLf
            strOut += "Translation: " + CStr(CInt(fPD.centerShift.X)) + ", " + CStr(CInt(fPD.centerShift.Y)) + vbCrLf
            strOut += "Frames since last resync: " + Format(resyncFrames, "000") + vbCrLf
            strOut += "Last resync cause(s): " + vbCrLf + resyncCause

            For Each keyval In feat.stable.goodCounts
                Dim pt = feat.stable.basics.ptList(keyval.Value)
                Dim g = feat.stable.basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                SetTrueText(CStr(g), pt)
            Next

            SetTrueText(strOut, 1)
            dst3 = center.dst3
            labels(3) = center.labels(3)
            resync = False
        End Sub
    End Class








    Public Class XO_FPoly_Plot : Inherits TaskParent
        Public fGrid As New XO_FPoly_Core
        Dim plotHist As New Plot_Histogram
        Public hist() As Single
        Public distDiff As New List(Of Single)
        Public Sub New()
            plotHist.minRange = 0
            plotHist.removeZeroEntry = False
            labels = {"", "", "", "anchor and companions - input to distance difference"}
            desc = "Feature Grid: compute distances between good features from frame to frame and plot the distribution"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lastDistance = fGrid.dst0.Clone

            fGrid.Run(src)
            dst3 = fGrid.dst3

            dst3 = src.Clone
            ReDim hist(fGrid.threshold + 1)
            distDiff.Clear()
            For i = 0 To fGrid.stable.basics.facetGen.facet.facetList.Count - 1
                Dim pt = fGrid.stable.basics.ptList(i)
                Dim d = fGrid.anchor.DistanceTo(pt)
                Dim lastd = lastDistance.Get(Of Single)(pt.Y, pt.X)
                Dim absDiff = Math.Abs(lastd - d)
                If absDiff >= hist.Length Then absDiff = hist.Length - 1
                If absDiff < fGrid.threshold Then
                    hist(CInt(absDiff)) += 1
                    vbc.DrawLine(dst3, fGrid.anchor, pt, taskAlg.highlight)
                    distDiff.Add(absDiff)
                Else
                    hist(fGrid.threshold) += 1
                End If
            Next

            Dim hlist = hist.ToList
            Dim peak = hlist.Max
            Dim peakIndex = hlist.IndexOf(peak)

            Dim histMat = cv.Mat.FromPixelData(hist.Length, 1, cv.MatType.CV_32F, hist.ToArray)
            plotHist.maxRange = fGrid.stable.basics.ptList.Count
            plotHist.Run(histMat)
            dst2 = plotHist.dst2
            Dim avg = If(distDiff.Count > 0, distDiff.Average, 0)
            labels(2) = "Average distance change (after threshholding) = " + Format(avg, fmt3) + ", peak at " + CStr(peakIndex) +
                        " with " + Format(peak, fmt1) + " occurances"
        End Sub
    End Class








    Public Class XO_FPoly_PlotWeighted : Inherits TaskParent
        Public fPlot As New XO_FPoly_Plot
        Dim plotHist As New Plot_Histogram
        Public Sub New()
            taskAlg.kalman = New Kalman_Basics
            plotHist.minRange = 0
            plotHist.removeZeroEntry = False
            labels = {"", "Distance change from previous frame", "", "anchor and companions - input to distance difference"}
            desc = "Feature Grid: compute distances between good features from frame to frame and plot with weighting and Kalman to smooth results"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fPlot.Run(src)
            dst3 = fPlot.dst3

            Dim lastPlot As cv.Mat = plotHist.dst2.Clone
            If taskAlg.optionsChanged Then ReDim taskAlg.kalman.kInput(fPlot.hist.Length - 1)

            taskAlg.kalman.kInput = fPlot.hist
            taskAlg.kalman.Run(emptyMat)
            fPlot.hist = taskAlg.kalman.kOutput

            Dim hlist = fPlot.hist.ToList
            Dim peak = hlist.Max
            Dim peakIndex = hlist.IndexOf(peak)
            Dim histMat = cv.Mat.FromPixelData(fPlot.hist.Length, 1, cv.MatType.CV_32F, fPlot.hist)
            plotHist.maxRange = fPlot.fGrid.stable.basics.ptList.Count
            plotHist.Run(histMat)
            dst2 = ShowAddweighted(plotHist.dst2, lastPlot, labels(2))
            If taskAlg.heartBeat Then
                Dim avg = If(fPlot.distDiff.Count > 0, fPlot.distDiff.Average, 0)
                labels(2) = "Average distance change (after threshholding) = " + Format(avg, fmt3) + ", peak at " +
                        CStr(peakIndex) + " with " + Format(peak, fmt1) + " occurances"
            End If
        End Sub
    End Class






    Public Class XO_FPoly_Stablizer : Inherits TaskParent
        Public fGrid As New XO_FPoly_Core
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"", "Movement amount - dot is current anchor point", "SyncImage aligned to current image - slide camera left or right",
                  "current image with distance map"}
            desc = "Feature Grid: show the accumulated camera movement in X and Y (no rotation)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fGrid.Run(src.Clone)
            dst3 = fGrid.dst3
            labels(3) = fGrid.labels(2)

            Static syncImage = src.Clone
            If fGrid.startAnchor = fGrid.anchor Then syncImage = src.Clone

            Dim shift As cv.Point2f = New cv.Point2f(fGrid.startAnchor.X - fGrid.anchor.X, fGrid.startAnchor.Y - fGrid.anchor.Y)
            Dim rect As New cv.Rect
            If shift.X < 0 Then rect.X = 0 Else rect.X = shift.X
            If shift.Y < 0 Then rect.Y = 0 Else rect.Y = shift.Y
            rect.Width = dst1.Width - Math.Abs(shift.X)
            rect.Height = dst1.Height - Math.Abs(shift.Y)

            dst1.SetTo(0)
            dst1(rect) = syncImage(rect)
            Dim lp As New lpData(fGrid.startAnchor, fGrid.anchor)
            XO_FPoly_Sides.DrawFatLine(dst1, lp, white)

            XO_Match_Points.DrawPolkaDot(fGrid.anchor, dst1)

            Dim r = New cv.Rect(0, 0, rect.Width, rect.Height)
            If fGrid.anchor.X > fGrid.startAnchor.X Then r.X = fGrid.anchor.X - fGrid.startAnchor.X
            If fGrid.anchor.Y > fGrid.startAnchor.Y Then r.Y = fGrid.anchor.Y - fGrid.startAnchor.Y

            dst2.SetTo(0)
            dst2(r) = syncImage(rect)
        End Sub
    End Class








    Public Class XO_FPoly_StartPoints : Inherits TaskParent
        Public startPoints As New List(Of cv.Point2f)
        Public goodPoints As New List(Of cv.Point2f)
        Dim fGrid As New XO_FPoly_Core
        Public Sub New()
            dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, 255)
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Track the feature grid points back to the last sync point"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static thresholdSlider = OptionParent.FindSlider("Resync if feature moves > X pixels")
            Dim threshold = thresholdSlider.Value
            Dim maxShift = fGrid.anchor.DistanceTo(fGrid.startAnchor) + threshold

            fGrid.Run(src)
            dst2 = fGrid.dst3
            Static facets As New List(Of List(Of cv.Point))
            Dim lastPoints = dst0.Clone
            If fGrid.startAnchor = fGrid.anchor Or goodPoints.Count < 5 Then
                startPoints = New List(Of cv.Point2f)(fGrid.goodPoints)
                facets = New List(Of List(Of cv.Point))(fGrid.goodFacets)
            End If

            dst0.SetTo(255)
            If standaloneTest() Then dst1.SetTo(0)
            Dim lpList As New List(Of lpData)
            goodPoints = New List(Of cv.Point2f)(fGrid.goodPoints)
            Dim facet As New List(Of cv.Point)
            Dim usedGood As New List(Of Integer)
            For i = 0 To goodPoints.Count - 1
                Dim pt = goodPoints(i)
                Dim startPoint = lastPoints.Get(Of Byte)(pt.Y, pt.X)
                If startPoint = 255 And i < 256 Then startPoint = i
                If startPoint < startPoints.Count And usedGood.Contains(startPoint) = False Then
                    usedGood.Add(startPoint)
                    facet = facets(startPoint)
                    dst0.FillConvexPoly(facet, startPoint, cv.LineTypes.Link4)
                    If standaloneTest() Then dst1.FillConvexPoly(facet, taskAlg.scalarColors(startPoint), taskAlg.lineType)
                    lpList.Add(New lpData(startPoints(startPoint), pt))
                End If
            Next

            ' dst3.SetTo(0)
            For Each lp In lpList
                If lp.p1.DistanceTo(lp.p2) <= maxShift Then vbc.DrawLine(dst1, lp.p1, lp.p2, cv.Scalar.Yellow)
                DrawCircle(dst1, lp.p1, taskAlg.DotSize, cv.Scalar.Yellow)
            Next
            dst1.Line(fGrid.anchor, fGrid.startAnchor, white, taskAlg.lineWidth + 1, taskAlg.lineType)
        End Sub
    End Class








    Public Class XO_FPoly_Triangle : Inherits TaskParent
        Dim triangle As New FindTriangle_Basics
        Dim fGrid As New XO_FPoly_Core
        Public Sub New()
            desc = "Find the minimum triangle that contains the feature grid"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fGrid.Run(src)
            dst2 = fGrid.dst2

            triangle.srcPoints = New List(Of cv.Point2f)(fGrid.goodPoints)
            triangle.Run(src)
            dst3 = triangle.dst2
        End Sub
    End Class






    Public Class XO_FPoly_WarpAffinePoly : Inherits TaskParent
        Dim rotatePoly As New XO_Rotate_PolyQT
        Dim warp As New WarpAffine_BasicsQT
        Dim fPoly As New XO_FPoly_BasicsOriginal
        Public Sub New()
            labels = {"", "", "Feature polygon after just rotation - white (original), yellow (current)",
                  "Feature polygon with rotation and shift - should be aligned"}
            desc = "Rotate and shift just the Feature polygon as indicated by XO_FPoly_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fPoly.Run(src)
            Dim polyPrev = fPoly.fPD.prevPoly
            Dim poly = New List(Of cv.Point2f)(fPoly.fPD.currPoly)

            dst2.SetTo(0)
            dst3.SetTo(0)

            XO_FPoly_Basics.DrawFPoly(dst2, polyPrev, white)

            warp.rotateCenter = fPoly.fPD.rotateCenter
            warp.rotateAngle = fPoly.fPD.rotateAngle
            warp.Run(dst2)
            dst3 = warp.dst2

            rotatePoly.rotateAngle = fPoly.fPD.rotateAngle
            rotatePoly.rotateCenter = fPoly.fPD.rotateCenter
            rotatePoly.poly = New List(Of cv.Point2f)(poly)
            rotatePoly.Run(src)

            If rotatePoly.poly.Count = 0 Then Exit Sub
            If fPoly.fPD.polyPrevSideIndex > rotatePoly.poly.Count Then fPoly.fPD.polyPrevSideIndex = 0

            Dim offset = New cv.Point2f(rotatePoly.poly(fPoly.fPD.polyPrevSideIndex).X - polyPrev(fPoly.fPD.polyPrevSideIndex).X,
                                    rotatePoly.poly(fPoly.fPD.polyPrevSideIndex).Y - polyPrev(fPoly.fPD.polyPrevSideIndex).Y)

            Dim r1 = New cv.Rect(offset.X, offset.Y, dst2.Width - Math.Abs(offset.X), dst2.Height - Math.Abs(offset.Y))
            If offset.X < 0 Then r1.X = 0
            If offset.Y < 0 Then r1.Y = 0

            Dim r2 = New cv.Rect(Math.Abs(offset.X), Math.Abs(offset.Y), r1.Width, r1.Height)
            If offset.X > 0 Then r2.X = 0
            If offset.Y > 0 Then r2.Y = 0

            dst3(r1) = dst2(r2)
            dst3 = dst3 - dst2

            XO_FPoly_Basics.DrawFPoly(dst3, rotatePoly.poly, cv.Scalar.Yellow)
            XO_FPoly_Basics.DrawFPoly(dst2, rotatePoly.poly, cv.Scalar.Yellow)

            SetTrueText(fPoly.strOut, 3)
        End Sub
    End Class










    Public Class XO_FPoly_RotatePoints : Inherits TaskParent
        Dim rotatePoly As New XO_Rotate_PolyQT
        Public poly As New List(Of cv.Point2f)
        Public polyPrev As New List(Of cv.Point2f)
        Public rotateAngle As Single
        Public rotateCenter As cv.Point2f
        Public polyPrevSideIndex As Integer
        Public centerShift As cv.Point2f
        Public Sub New()
            labels = {"", "", "Feature polygon after just rotation - white (original), yellow (current)",
                  "Feature polygons with rotation and shift - should be aligned"}
            desc = "Rotate and shift just the Feature polygon as indicated by XO_FPoly_Basics"
        End Sub
        Public Function shiftPoly(polyPrev As List(Of cv.Point2f), poly As List(Of cv.Point2f)) As cv.Point2f
            rotatePoly.rotateAngle = rotateAngle
            rotatePoly.rotateCenter = rotateCenter
            rotatePoly.poly = New List(Of cv.Point2f)(poly)
            rotatePoly.Run(emptyMat)

            Dim totalX = rotatePoly.poly(polyPrevSideIndex).X - polyPrev(polyPrevSideIndex).X
            Dim totalY = rotatePoly.poly(polyPrevSideIndex).Y - polyPrev(polyPrevSideIndex).Y

            Return New cv.Point2f(totalX, totalY)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                SetTrueText(traceName + " is meant only to run with XO_FPoly_Basics to validate the translation")
                Exit Sub
            End If

            dst2.SetTo(0)
            dst3.SetTo(0)

            Dim rotateAndShift As New List(Of cv.Point2f)
            centerShift = shiftPoly(polyPrev, poly)
            XO_FPoly_Basics.DrawFPoly(dst2, polyPrev, white)
            XO_FPoly_Basics.DrawFPoly(dst2, rotatePoly.poly, cv.Scalar.Yellow)
            For i = 0 To polyPrev.Count - 1
                Dim p1 = New cv.Point2f(rotatePoly.poly(i).X - centerShift.X, rotatePoly.poly(i).Y - centerShift.Y)
                Dim p2 = New cv.Point2f(rotatePoly.poly((i + 1) Mod taskAlg.polyCount).X - centerShift.X,
                                    rotatePoly.poly((i + 1) Mod taskAlg.polyCount).Y - centerShift.Y)
                rotateAndShift.Add(p1)
                SetTrueText(CStr(i), rotatePoly.poly(i), 2)
                SetTrueText(CStr(i), polyPrev(i), 2)
            Next
            XO_FPoly_Basics.DrawFPoly(dst3, polyPrev, white)
            XO_FPoly_Basics.DrawFPoly(dst3, rotateAndShift, cv.Scalar.Yellow)

            strOut = "After Rotation: " + Format(rotatePoly.rotateAngle, fmt0) + " degrees " +
                 "After Translation (shift) of: " + Format(centerShift.X, fmt0) + ", " + Format(centerShift.Y, fmt0) + vbCrLf +
                 "Center of Rotation: " + Format(rotateCenter.X, fmt0) + ", " + Format(rotateCenter.Y, fmt0) + vbCrLf +
                 "If the algorithm is working properly, the white and yellow Feature polygons below " + vbCrLf +
                 "should match in size and location."
            SetTrueText(strOut, 3)
        End Sub
    End Class







    Public Class XO_FPoly_WarpAffineImage : Inherits TaskParent
        Dim warp As New WarpAffine_BasicsQT
        Dim fPoly As New XO_FPoly_BasicsOriginal
        Dim options As New Options_Diff
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Use OpenCV's WarpAffine to rotate and translate the starting image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            fPoly.Run(src)

            warp.rotateCenter = fPoly.fPD.rotateCenter
            warp.rotateAngle = fPoly.fPD.rotateAngle
            warp.Run(fPoly.resyncImage.Clone)
            dst2 = warp.dst2
            dst1 = fPoly.dst1

            Dim offset = fPoly.fPD.centerShift

            Dim r1 = New cv.Rect(offset.X, offset.Y, dst2.Width - Math.Abs(offset.X), dst2.Height - Math.Abs(offset.Y))
            If offset.X < 0 Then r1.X = 0
            If offset.Y < 0 Then r1.Y = 0

            Dim r2 = New cv.Rect(Math.Abs(offset.X), Math.Abs(offset.Y), r1.Width, r1.Height)
            If offset.X > 0 Then r2.X = 0
            If offset.Y > 0 Then r2.Y = 0

            dst3(r1) = dst2(r2)
            dst3 = src - dst2

            Dim tmp = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim changed = tmp.Threshold(options.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            Dim diffCount = changed.CountNonZero
            strOut = fPoly.strOut
            strOut += vbCrLf + Format(diffCount / 1000, fmt0) + "k pixels differ or " +
                           Format(diffCount / dst3.Total, "0%")

            SetTrueText(strOut, 1)
        End Sub
    End Class








    ' https://www.google.com/search?q=geometry+find+the+center+of+rotation&rlz=1C1CHBF_enUS838US838&oq=geometry+find+the+center+of+rotation&aqs=chrome..69i57j0i22i30j0i390l3.9576j0j4&sourceid=chrome&ie=UTF-8#kpvalbx=_rgg1Y9rbGM3n0PEP-ae4oAc_34
    Public Class XO_FPoly_Perpendiculars : Inherits TaskParent
        Public altCenterShift As cv.Point2f
        Public fPD As fPolyData
        Public rotatePoints As New XO_FPoly_RotatePoints
        Dim near As New XO_Line_Nearest
        Public Sub New()
            taskAlg.kalman = New Kalman_Basics
            labels = {"", "", "Output of XO_FPoly_Basics", "Center of rotation is where the extended lines intersect"}
            desc = "Find the center of rotation using the perpendicular lines from polymp and FLine (feature line) in XO_FPoly_Basics"
        End Sub
        Private Function findrotateAngle(p1 As cv.Point2f, p2 As cv.Point2f, pt As cv.Point2f) As Single
            near.lp = New lpData(p1, p2)
            near.pt = pt
            near.Run(emptyMat)
            vbc.DrawLine(dst2, pt, near.nearPoint, cv.Scalar.Red)
            Dim d1 = fPD.rotateCenter.DistanceTo(pt)
            Dim d2 = fPD.rotateCenter.DistanceTo(near.nearPoint)
            Dim angle = Math.Asin(near.nearPoint.DistanceTo(pt) / If(d1 > d2, d1, d2))
            If Single.IsNaN(angle) Then Return 0
            Return angle
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                SetTrueText("There is no output for the " + traceName + " algorithm when run standaloneTest().")
                Exit Sub
            End If

            Static perp1 As New Line_PerpendicularTest
            Static perp2 As New Line_PerpendicularTest

            dst2.SetTo(0)
            perp1.input = New lpData(fPD.currPoly(fPD.polyPrevSideIndex),
                                    fPD.currPoly((fPD.polyPrevSideIndex + 1) Mod taskAlg.polyCount))
            perp1.Run(src)

            vbc.DrawLine(dst2, perp1.output.p1, perp1.output.p2, cv.Scalar.Yellow)

            perp2.input = New lpData(fPD.prevPoly(fPD.polyPrevSideIndex),
                                   fPD.prevPoly((fPD.polyPrevSideIndex + 1) Mod taskAlg.polyCount))
            perp2.Run(src)
            vbc.DrawLine(dst2, perp2.output.p1, perp2.output.p2, white)

            fPD.rotateCenter = Line_Intersection.IntersectTest(perp2.output.p1, perp2.output.p2, perp1.output.p1, perp1.output.p2)
            If fPD.rotateCenter = New cv.Point2f Then
                fPD.rotateAngle = 0
            Else
                DrawCircle(dst2, fPD.rotateCenter, taskAlg.DotSize + 2, cv.Scalar.Red)
                fPD.rotateAngle = findrotateAngle(perp2.output.p1, perp2.output.p2, perp1.output.p1)
            End If
            If fPD.rotateAngle = 0 Then fPD.rotateCenter = New cv.Point2f

            altCenterShift = New cv.Point2f(fPD.currPoly(fPD.polyPrevSideIndex).X - fPD.prevPoly(fPD.polyPrevSideIndex).X,
                                        fPD.currPoly(fPD.polyPrevSideIndex).Y - fPD.prevPoly(fPD.polyPrevSideIndex).Y)

            taskAlg.kalman.kInput = {fPD.rotateAngle}
            taskAlg.kalman.Run(emptyMat)
            fPD.rotateAngle = taskAlg.kalman.kOutput(0)

            rotatePoints.poly = fPD.currPoly
            rotatePoints.polyPrev = fPD.prevPoly
            rotatePoints.polyPrevSideIndex = fPD.polyPrevSideIndex
            rotatePoints.rotateAngle = fPD.rotateAngle
            rotatePoints.Run(src)
            fPD.centerShift = rotatePoints.centerShift
            dst3 = rotatePoints.dst3
        End Sub
    End Class








    Public Class XO_FPoly_Image : Inherits TaskParent
        Public fpoly As New XO_FPoly_BasicsOriginal
        Dim rotate As New Rotate_BasicsQT
        Public resync As Boolean
        Dim options As New Options_Diff
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"", "Feature polygon alignment, White is original, Yellow is current, Red Dot (if present) is center of rotation",
                  "Resync Image after rotation and translation", "Difference between current image and dst2"}
            desc = "Rotate and shift the image as indicated by XO_FPoly_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim input = src.Clone
            fpoly.Run(src)
            dst1 = fpoly.dst1

            If fpoly.resync = False Then
                If fpoly.fPD.featureLineChanged = False Then
                    dst2.SetTo(0)
                    dst3.SetTo(0)
                    rotate.rotateAngle = fpoly.fPD.rotateAngle
                    rotate.rotateCenter = fpoly.fPD.rotateCenter
                    rotate.Run(fpoly.resyncImage)
                    dst0 = rotate.dst2

                    Dim offset As cv.Point2f = fpoly.fPD.centerShift

                    Dim r1 = New cv.Rect(offset.X, offset.Y, dst2.Width - Math.Abs(offset.X), dst2.Height - Math.Abs(offset.Y))
                    r1 = ValidateRect(r1)
                    If offset.X < 0 Then r1.X = 0
                    If offset.Y < 0 Then r1.Y = 0

                    Dim r2 = New cv.Rect(Math.Abs(offset.X), Math.Abs(offset.Y), r1.Width, r1.Height)
                    r2.Width = r1.Width
                    r2.Height = r1.Height
                    If r2.X < 0 Or r2.X >= dst2.Width Then Exit Sub ' wedged...
                    If r2.Y < 0 Or r2.Y >= dst2.Height Then Exit Sub ' wedged...
                    If offset.X > 0 Then r2.X = 0
                    If offset.Y > 0 Then r2.Y = 0

                    Dim mask2 As New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 255)
                    rotate.Run(mask2)
                    mask2 = rotate.dst2

                    Dim mask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
                    mask(r1).SetTo(255)
                    mask(r1) = mask2(r2)
                    mask = Not mask

                    dst2(r1) = dst0(r2)
                    dst3 = input - dst2
                    dst3.SetTo(0, mask)
                End If

                Dim tmp = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                Dim changed = tmp.Threshold(options.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
                Dim diffCount = changed.CountNonZero
                resync = fpoly.resync
                fpoly.maskChangePercent = diffCount / dst3.Total
                strOut = fpoly.strOut
                strOut += vbCrLf + Format(diffCount / 1000, fmt0) + "k pixels differ or " + Format(fpoly.maskChangePercent, "00%")

            Else
                dst2 = fpoly.resyncImage.Clone
                dst3.SetTo(0)
            End If

            SetTrueText(strOut, 1)
        End Sub
    End Class








    Public Class XO_FPoly_ImageMask : Inherits TaskParent
        Public fImage As New XO_FPoly_Image
        Dim options As New Options_Diff
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            taskAlg.featureOptions.ColorDiffSlider.Value = 10
            desc = "Build the image mask of the differences between the current frame and resync image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            fImage.Run(src)
            dst2 = fImage.dst3
            dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst3 = dst0.Threshold(options.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            labels = fImage.labels
            dst1 = fImage.fpoly.dst1
            SetTrueText(fImage.strOut, 1)
        End Sub
    End Class







    Public Class XO_FPoly_PointCloud : Inherits TaskParent
        Public fMask As New XO_FPoly_ImageMask
        Public fPolyCloud As cv.Mat
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Update changed point cloud pixels as indicated by the FPoly_ImageMask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fMask.Run(src)
            If fMask.fImage.fpoly.resync Or taskAlg.firstPass Then fPolyCloud = taskAlg.pointCloud.Clone
            dst1 = fMask.dst1
            dst2 = fMask.dst2
            dst3 = fMask.dst3
            taskAlg.pointCloud.CopyTo(fPolyCloud, dst3)

            SetTrueText(fMask.fImage.strOut, 1)
        End Sub
    End Class







    Public Class XO_FPoly_ResyncCheck : Inherits TaskParent
        Dim fPoly As New XO_FPoly_BasicsOriginal
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "If there was no resync, check the longest side of the feature polygon (Feature Line) for unnecessary jitter."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fPoly.Run(src)
            dst2 = fPoly.dst1
            SetTrueText(fPoly.strOut, 2)

            Static lastPixelCount As Integer
            If fPoly.resync Then
                dst3.SetTo(0)
                lastPixelCount = 0
            End If

            If fPoly.fPD.currPoly.Count < 2 Then Exit Sub ' polygon not found...

            Dim polymp = fPoly.fPD.currmp()
            vbc.DrawLine(dst3, polymp.p1, polymp.p2, 255)

            Dim pixelCount = dst3.CountNonZero
            SetTrueText(Format(Math.Abs(lastPixelCount - pixelCount)) + " pixels ", 3)
            lastPixelCount = pixelCount
        End Sub
    End Class








    Public Class XO_FPoly_Center : Inherits TaskParent
        Public rotatePoly As New XO_Rotate_PolyQT
        Dim near As New XO_Line_Nearest
        Public fPD As fPolyData
        Dim newPoly As List(Of cv.Point2f)
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"", "Layout of feature polygons after just translation - red line is used in sine computation",
                      "Layout of the starting (white) and current (yellow) feature polygons",
                      "Layout of feature polygons after rotation and translation"}
            desc = "Manually rotate and translate the current feature polygon to a previous feature polygon."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                SetTrueText(traceName + " is called by XO_FPoly_Basics to get the image movement." + vbCrLf +
                        "It does not produce any output when run standaloneTest().")
                Exit Sub
            End If

            Static thresholdSlider = OptionParent.FindSlider("Resync if feature moves > X pixels")
            Dim threshold = thresholdSlider.Value

            Dim sindex1 = fPD.polyPrevSideIndex
            Dim sIndex2 = (sindex1 + 1) Mod taskAlg.polyCount

            Dim mp1 = fPD.currmp()
            Dim mp2 = fPD.prevmp()
            Dim d1 = mp1.p1.DistanceTo(mp2.p1)
            Dim d2 = mp1.p2.DistanceTo(mp2.p2)
            Dim newNear As lpData
            If d1 < d2 Then
                fPD.centerShift = New cv.Point2f(mp1.p1.X - mp2.p1.X, mp1.p1.Y - mp2.p1.Y)
                fPD.rotateCenter = mp1.p1
                newNear = New lpData(mp1.p2, mp2.p2)
            Else
                fPD.centerShift = New cv.Point2f(mp1.p2.X - mp2.p2.X, mp1.p2.Y - mp2.p2.Y)
                fPD.rotateCenter = mp1.p2
                newNear = New lpData(mp1.p1, mp2.p1)
            End If

            Dim transPoly As New List(Of cv.Point2f)
            For i = 0 To fPD.currPoly.Count - 1
                transPoly.Add(New cv.Point2f(fPD.currPoly(i).X - fPD.centerShift.X, fPD.currPoly(i).Y - fPD.centerShift.Y))
            Next
            newNear.p1 = New cv.Point2f(newNear.p1.X - fPD.centerShift.X, newNear.p1.Y - fPD.centerShift.Y)
            newNear.p2 = New cv.Point2f(newNear.p2.X - fPD.centerShift.X, newNear.p2.Y - fPD.centerShift.Y)
            fPD.rotateCenter = New cv.Point2f(fPD.rotateCenter.X - fPD.centerShift.X, fPD.rotateCenter.Y - fPD.centerShift.Y)

            dst1.SetTo(0)
            XO_FPoly_Basics.DrawPolys(dst1, fPD)

            strOut = "No rotation" + vbCrLf
            fPD.rotateAngle = 0
            If d1 <> d2 Then
                If newNear.p1.DistanceTo(newNear.p2) > threshold Then
                    near.lp = New lpData(fPD.prevPoly(sindex1), fPD.prevPoly(sIndex2))
                    near.pt = newNear.p1
                    near.Run(src)
                    dst1.Line(near.pt, near.nearPoint, cv.Scalar.Red, taskAlg.lineWidth + 5, taskAlg.lineType)

                    Dim hypotenuse = fPD.rotateCenter.DistanceTo(near.pt)
                    fPD.rotateAngle = -Math.Asin(near.nearPoint.DistanceTo(near.pt) / hypotenuse)
                    If Single.IsNaN(fPD.rotateAngle) Then fPD.rotateAngle = 0
                    strOut = "Angle is " + Format(fPD.rotateAngle * 57.2958, fmt1) + " degrees" + vbCrLf
                End If
            End If
            strOut += "Translation (shift) is " + Format(-fPD.centerShift.X, fmt0) + ", " + Format(-fPD.centerShift.Y, fmt0)

            If Math.Abs(fPD.rotateAngle) > 0 Then
                rotatePoly.rotateCenter = fPD.rotateCenter
                rotatePoly.rotateAngle = fPD.rotateAngle
                rotatePoly.poly.Clear()
                rotatePoly.poly.Add(newNear.p1)
                rotatePoly.Run(src)

                If near.nearPoint.DistanceTo(rotatePoly.poly(0)) > newNear.p1.DistanceTo(rotatePoly.poly(0)) Then fPD.rotateAngle *= -1

                rotatePoly.rotateAngle = fPD.rotateAngle
                rotatePoly.poly = New List(Of cv.Point2f)(transPoly)
                rotatePoly.Run(src)

                newPoly = New List(Of cv.Point2f)(rotatePoly.poly)
            End If
            dst3.SetTo(0)
            XO_FPoly_Basics.DrawPolys(dst3, fPD)
            SetTrueText(strOut, 2)
        End Sub
    End Class








    Public Class XO_FPoly_EdgeRemoval : Inherits TaskParent
        Dim fMask As New XO_FPoly_ImageMask
        Dim edges As New Edge_Basics
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Remove edges from the FPoly_ImageMask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fMask.Run(src)
            dst2 = fMask.dst3

            edges.Run(src)
            dst1 = edges.dst2

            dst3 = dst2 And Not dst1
        End Sub
    End Class








    Public Class XO_FPoly_ImageNew : Inherits TaskParent
        Public fpoly As New XO_FPoly_Basics
        Dim rotate As New Rotate_BasicsQT
        Public resync As Boolean
        Dim options As New Options_Diff
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"", "Feature polygon alignment, White is original, Yellow is current, Red Dot (if present) is center of rotation",
                  "Resync Image after rotation and translation", "Difference between current image and dst2"}
            desc = "Rotate and shift the image as indicated by XO_FPoly_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim input = src.Clone
            fpoly.Run(src)
            dst1 = fpoly.dst3

            If fpoly.resync = False Then
                ' If fpoly.sides.featureLineChanged = False Then
                dst2.SetTo(0)
                dst3.SetTo(0)
                rotate.rotateAngle = fpoly.sides.rotateAngle
                rotate.rotateCenter = fpoly.sides.rotateCenter
                rotate.Run(fpoly.sides.prevImage)
                dst0 = rotate.dst2

                Dim offset As cv.Point2f = fpoly.sides.centerShift

                Dim r1 = New cv.Rect(offset.X, offset.Y, dst2.Width - Math.Abs(offset.X), dst2.Height - Math.Abs(offset.Y))
                If offset.X < 0 Then r1.X = 0
                If offset.Y < 0 Then r1.Y = 0

                Dim r2 = New cv.Rect(Math.Abs(offset.X), Math.Abs(offset.Y), r1.Width, r1.Height)
                If offset.X > 0 Then r2.X = 0
                If offset.Y > 0 Then r2.Y = 0

                Dim mask2 As New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 255)
                rotate.Run(mask2)
                mask2 = rotate.dst2

                Dim mask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
                mask(r1).SetTo(255)
                mask(r1) = mask2(r2)
                mask = Not mask

                dst2(r1) = dst0(r2)
                dst3 = input - dst2
                dst3.SetTo(0, mask)
                ' End If

                Dim tmp = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                Dim changed = tmp.Threshold(options.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
                Dim diffCount = changed.CountNonZero
                resync = fpoly.resync
                fpoly.maskChangePercent = diffCount / dst3.Total
                strOut = fpoly.strOut
                strOut += vbCrLf + Format(diffCount / 1000, fmt0) + "k pixels differ or " + Format(fpoly.maskChangePercent, "00%")
            Else
                dst2 = fpoly.sides.prevImage.Clone
                dst3.SetTo(0)
            End If

            SetTrueText(strOut, 1)
        End Sub
    End Class






    Public Class XO_FPoly_LeftRight : Inherits TaskParent
        Dim leftPoly As New XO_FPoly_Basics
        Dim rightPoly As New XO_FPoly_Basics
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"Left image", "Right image", "FPoly output for left image", "FPoly output for right image"}
            desc = "Measure camera motion through the left and right images using FPoly"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst0 = taskAlg.leftView
            dst1 = taskAlg.rightView
            leftPoly.Run(taskAlg.leftView)
            dst2 = leftPoly.dst3
            SetTrueText(leftPoly.strOut, 2)

            rightPoly.Run(taskAlg.rightView)
            dst3 = rightPoly.dst3
            SetTrueText(rightPoly.strOut, 3)
        End Sub
    End Class








    Public Class XO_FPoly_Core : Inherits TaskParent
        Public stable As New FCS_Basics
        Public anchor As cv.Point2f
        Public startAnchor As cv.Point2f
        Public goodPoints As New List(Of cv.Point2f)
        Public goodFacets As New List(Of List(Of cv.Point))
        Public threshold As Integer
        Dim options As New Options_FPoly
        Dim optionsCore As New Options_FPolyCore
        Dim optionsEx As New Options_Features
        Public Sub New()
            dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
            taskAlg.featureOptions.FeatureSampleSize.Value = 20
            labels(3) = "Feature points with anchor"
            desc = "Feature Grid: compute distances between good features from frame to frame"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            optionsCore.Run()
            optionsEx.Run()

            stable.Run(src)
            dst3 = stable.basics.dst3

            Dim lastDistance = dst0.Clone
            anchor = stable.basics.anchorPoint
            Static lastAnchor = anchor
            If lastAnchor.distanceto(anchor) > optionsCore.anchorMovement Then lastDistance.SetTo(0)

            dst0.SetTo(0)
            goodPoints.Clear()
            goodFacets.Clear()
            dst2.SetTo(0)
            For i = 0 To stable.basics.facetGen.facet.facetList.Count - 1
                Dim facet = stable.basics.facetGen.facet.facetList(i)
                Dim pt = stable.basics.ptList(i)
                Dim d = anchor.DistanceTo(pt)
                dst0.FillConvexPoly(facet, d, taskAlg.lineType)
                Dim lastd = lastDistance.Get(Of Single)(pt.Y, pt.X)
                Dim absDiff = Math.Abs(lastd - d)
                If absDiff < threshold Or threshold = 0 Then
                    goodPoints.Add(pt)
                    goodFacets.Add(facet)
                    SetTrueText(Format(absDiff, fmt1), pt, 2)
                    vbc.DrawLine(dst3, anchor, pt, taskAlg.highlight)
                    dst2.Set(Of cv.Vec3b)(pt.Y, pt.X, white.ToVec3b)
                End If
            Next

            Dim shift As cv.Point2f = New cv.Point2f(startAnchor.X - anchor.X, startAnchor.Y - anchor.Y)
            If goodPoints.Count = 0 Or Math.Abs(shift.X) > optionsCore.maxShift Or Math.Abs(shift.Y) > optionsCore.maxShift Then startAnchor = anchor
            labels(2) = "Distance change (after threshholding) since last reset = " + shift.ToString
            lastAnchor = anchor
        End Sub
    End Class







    Public Class XO_FPoly_TopFeatures : Inherits TaskParent
        Public stable As New Stable_BasicsCount
        Public options As New Options_FPoly
        Dim feat As New Feature_General
        Public topFeatures As New List(Of cv.Point2f)
        Public Sub New()
            desc = "Get the top features and validate them using Delaunay regions."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            feat.Run(taskAlg.grayStable)

            stable.Run(src)
            dst2 = stable.dst2
            topFeatures.Clear()
            Dim showText = standaloneTest()
            For Each keyVal In stable.goodCounts
                Dim pt = stable.basics.ptList(keyVal.Value)
                Dim g = stable.basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                If showText Then SetTrueText(CStr(g), pt)
                If topFeatures.Count < taskAlg.polyCount Then topFeatures.Add(pt)
            Next

            For i = 0 To topFeatures.Count - 2
                vbc.DrawLine(dst2, topFeatures(i), topFeatures(i + 1), white)
            Next
        End Sub
    End Class






    Public Class XO_FPoly_Line : Inherits TaskParent
        Dim feat As New XO_FPoly_TopFeatures
        Public lp As New lpData
        Dim bPoint As New BrickPoint_Basics
        Public Sub New()
            labels = {"", "", "Points found with FPoly_TopFeatures", "Longest line in feat.topFeatures"}
            desc = "Identify the longest line in topFeatures"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bPoint.Run(src)

            taskAlg.features.Clear()
            For Each pt In bPoint.ptList
                taskAlg.features.Add(New cv.Point2f(pt.X, pt.Y))
            Next

            feat.Run(src)
            dst2.SetTo(0)
            Dim pts = feat.topFeatures
            Dim distances As New List(Of Single)
            For i = 0 To pts.Count - 2
                vbc.DrawLine(dst2, pts(i), pts(i + 1), taskAlg.highlight)
                distances.Add(pts(i).DistanceTo(pts(i + 1)))
            Next

            If distances.Count Then
                Dim index = distances.IndexOf(distances.Max)
                lp = New lpData(pts(index), pts(index + 1))
                dst3 = src
                vbc.DrawLine(dst3, lp.p1, lp.p2, taskAlg.highlight)
            End If
        End Sub
    End Class






    Public Class XO_FPoly_LineRect : Inherits TaskParent
        Dim fLine As New XO_FPoly_Line
        Public lpRect As New cv.Rect
        Public Sub New()
            labels(2) = "The rectangle is formed by the longest line between the taskAlg.topFeatures"
            desc = "Build the rectangle formed by the longest line in taskAlg.topFeatures."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLine.Run(src)

            Dim lp = fLine.lp
            Dim rotatedRect = cv.Cv2.MinAreaRect({lp.p1, lp.p2})
            lpRect = rotatedRect.BoundingRect

            dst2 = src
            vbc.DrawLine(dst2, lp.p1, lp.p2, taskAlg.highlight)
            dst2.Rectangle(lpRect, taskAlg.highlight, taskAlg.lineWidth)
        End Sub
    End Class










    Public Class XO_Delaunay_Points : Inherits TaskParent
        Dim delaunay As New Delaunay_Basics
        Dim feat As New XO_FPoly_TopFeatures
        Public Sub New()
            OptionParent.FindSlider("Points to use in Feature Poly").Value = 2
            desc = "This algorithm explores what happens when Delaunay is used on 2 points"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                Static bPoint As New BrickPoint_Basics
                bPoint.Run(src)
                taskAlg.features.Clear()
                For Each pt In bPoint.ptList
                    taskAlg.features.Add(New cv.Point2f(pt.X, pt.Y))
                Next
            End If
            Static ptSlider = OptionParent.FindSlider("Points to use in Feature Poly")

            feat.Run(src)
            dst3 = feat.dst3

            delaunay.inputPoints.Clear()
            For i = 0 To Math.Min(ptSlider.value, feat.topFeatures.Count) - 1
                delaunay.inputPoints.Add(feat.topFeatures(i))
            Next
            delaunay.Run(src)
            dst2 = delaunay.dst2
        End Sub
    End Class









    Public Class XO_Homography_FPoly : Inherits TaskParent
        Dim fPoly As New XO_FPoly_BasicsOriginal
        Dim hGraph As New Homography_Basics
        Public Sub New()
            desc = "Use the feature polygon to warp the current image to a previous image.  This is not useful but demonstrates how to use homography."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fPoly.Run(src)
            dst2 = fPoly.dst1
            If fPoly.fPD.currPoly Is Nothing Or fPoly.fPD.prevPoly Is Nothing Then Exit Sub
            If fPoly.fPD.currPoly.Count = 0 Or fPoly.fPD.prevPoly.Count = 0 Then Exit Sub
            If fPoly.fPD.currPoly.Count <> fPoly.fPD.prevPoly.Count Then Exit Sub

            hGraph.corners1.Clear()
            hGraph.corners2.Clear()
            For i = 0 To fPoly.fPD.currPoly.Count - 1
                Dim p1 = fPoly.fPD.currPoly(i)
                Dim p2 = fPoly.fPD.prevPoly(i)
                hGraph.corners1.Add(New cv.Point2d(p1.X, p1.Y))
                hGraph.corners2.Add(New cv.Point2d(p2.X, p2.Y))
            Next

            hGraph.Run(src)
            dst3 = hGraph.dst2
        End Sub
    End Class






    Public Class XO_Motion_FPolyRect : Inherits TaskParent
        Dim fRect As New XO_FPoly_LineRect
        Public match As New Match_Basics
        Dim srcSave As New cv.Mat
        Public Sub New()
            desc = "Confirm the FPoly_LineRect matched the previous image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fRect.Run(src)

            If taskAlg.heartBeatLT Or match.correlation < 0.5 Then
                srcSave = src.Clone
                dst2 = fRect.dst2.Clone()
            End If
            match.template = srcSave(ValidateRect(fRect.lpRect)).Clone
            match.Run(src)
            dst3 = src
            dst3.Rectangle(match.newRect, taskAlg.highlight, taskAlg.lineWidth)
            labels(3) = "Correlation Coefficient = " + Format(match.correlation * 100, fmt1)
        End Sub
    End Class





    Public Class XO_Motion_TopFeatures : Inherits TaskParent
        Dim feat As New XO_FPoly_TopFeatures
        Public featureRects As New List(Of cv.Rect)
        Public searchRects As New List(Of cv.Rect)
        Dim match As New Match_Basics
        Dim half As Integer
        Public Sub New()
            labels(2) = "Track the feature rect (small one) in each larger rectangle"
            desc = "Find the top feature cells and track them in the next frame."
        End Sub
        Private Sub snapShotFeatures()
            searchRects.Clear()
            featureRects.Clear()
            For Each pt In feat.topFeatures
                Dim index As Integer = taskAlg.gridMap.Get(Of Integer)(pt.Y, pt.X)
                Dim roi = New cv.Rect(pt.X - half, pt.Y - half, taskAlg.brickSize, taskAlg.brickSize)
                roi = ValidateRect(roi)
                featureRects.Add(roi)
                searchRects.Add(taskAlg.gridNabeRects(index))
            Next

            dst2 = dst1.Clone
            For Each pt In feat.topFeatures
                Dim index As Integer = taskAlg.gridMap.Get(Of Integer)(pt.Y, pt.X)
                Dim roi = New cv.Rect(pt.X - half, pt.Y - half, taskAlg.brickSize, taskAlg.brickSize)
                roi = ValidateRect(roi)
                dst2.Rectangle(roi, taskAlg.highlight, taskAlg.lineWidth)
                dst2.Rectangle(taskAlg.gridNabeRects(index), taskAlg.highlight, taskAlg.lineWidth)
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            half = CInt(taskAlg.brickSize / 2)

            dst1 = src.Clone
            feat.Run(src)

            If taskAlg.heartBeatLT Then
                snapShotFeatures()
            End If

            dst3 = src.Clone
            Dim matchRects As New List(Of cv.Rect)
            For i = 0 To featureRects.Count - 1
                Dim roi = featureRects(i)
                match.template = dst1(roi).Clone
                match.Run(src(searchRects(i)))
                dst3.Rectangle(match.newRect, taskAlg.highlight, taskAlg.lineWidth)
                matchRects.Add(match.newRect)
            Next

            searchRects.Clear()
            featureRects.Clear()
            For Each roi In matchRects
                Dim pt = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
                Dim index As Integer = taskAlg.gridMap.Get(Of Integer)(pt.Y, pt.X)
                featureRects.Add(roi)
                searchRects.Add(taskAlg.gridNabeRects(index))
            Next
        End Sub
    End Class








    ' https://academo.org/demos/rotation-about-point/
    Public Class XO_Rotate_PolyQT : Inherits TaskParent
        Public poly As New List(Of cv.Point2f)
        Public rotateCenter As cv.Point2f
        Public rotateAngle As Single
        Public Sub New()
            labels = {"", "", "Polygon before rotation", ""}
            desc = "Rotate a triangle around a center of rotation"
        End Sub
        Private Sub drawPolygon(dst As cv.Mat, color As cv.Scalar)
            Dim minMod = Math.Min(poly.Count, taskAlg.polyCount)
            For i = 0 To minMod - 1
                vbc.DrawLine(dst, poly(i), poly((i + 1) Mod minMod), color)
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.heartBeat Then
                dst2.SetTo(0)
                dst3.SetTo(0)
            End If

            drawPolygon(dst2, red)

            If standaloneTest() Then
                SetTrueText(traceName + " has no output when run standaloneTest().")
                Exit Sub
            End If

            labels(3) = "White is the original polygon, yellow has been rotated " + Format(rotateAngle * 57.2958) + " degrees"

            ' translate so the center of rotation is 0,0
            Dim translated As New List(Of cv.Point2f)
            For i = 0 To poly.Count - 1
                Dim pt = poly(i)
                translated.Add(New cv.Point2f(poly(i).X - rotateCenter.X, poly(i).Y - rotateCenter.Y))
            Next

            Dim rotated As New List(Of cv.Point2f)
            For i = 0 To poly.Count - 1
                Dim pt = translated(i)
                Dim x = pt.X * Math.Cos(rotateAngle) - pt.Y * Math.Sin(rotateAngle)
                Dim y = pt.Y * Math.Cos(rotateAngle) + pt.X * Math.Sin(rotateAngle)
                rotated.Add(New cv.Point2f(x, y))
            Next

            drawPolygon(dst3, white)

            poly.Clear()
            For Each pt In rotated
                poly.Add(New cv.Point2f(pt.X + rotateCenter.X, pt.Y + rotateCenter.Y))
            Next

            drawPolygon(dst3, taskAlg.highlight)
        End Sub
    End Class







    ' https://academo.org/demos/rotation-about-point/
    Public Class XO_Rotate_Poly : Inherits TaskParent
        Dim optionsFPoly As New Options_FPoly
        Public options As New Options_RotatePoly
        Public rotateQT As New XO_Rotate_PolyQT
        Dim rPoly As New List(Of cv.Point2f)
        Public Sub New()
            labels = {"", "", "Triangle before rotation", "Triangle after rotation"}
            desc = "Rotate a triangle around a center of rotation"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            optionsFPoly.Run()

            If options.changeCheck.Checked Or taskAlg.firstPass Then
                rPoly.Clear()
                For i = 0 To taskAlg.polyCount - 1
                    rPoly.Add(New cv.Point2f(msRNG.Next(dst2.Width / 4, dst2.Width * 3 / 4), msRNG.Next(dst2.Height / 4, dst2.Height * 3 / 4)))
                Next
                rotateQT.rotateCenter = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                options.changeCheck.Checked = False
            End If

            rotateQT.poly = New List(Of cv.Point2f)(rPoly)
            rotateQT.rotateAngle = options.angleSlider.Value
            rotateQT.Run(src)
            dst2 = rotateQT.dst3

            DrawCircle(dst2, rotateQT.rotateCenter, taskAlg.DotSize + 2, cv.Scalar.Yellow)
            SetTrueText("center of rotation", rotateQT.rotateCenter)
            labels(3) = rotateQT.labels(3)
        End Sub
    End Class




    Public Class XO_Stabilizer_Basics : Inherits TaskParent
        Dim match As New Match_Basics
        Public shiftX As Integer
        Public shiftY As Integer
        Public templateRect As cv.Rect
        Public searchRect As cv.Rect
        Public stableRect As cv.Rect
        Dim options As New Options_Stabilizer
        Dim lastFrame As cv.Mat
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            labels(2) = "Current frame - rectangle input to matchTemplate"
            desc = "if reasonable stdev and no motion in correlation rectangle, stabilize image across frames"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim resetImage As Boolean
            templateRect = New cv.Rect(src.Width / 2 - options.width / 2, src.Height / 2 - options.height / 2,
                                   options.width, options.height)

            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If taskAlg.firstPass Then lastFrame = src.Clone()

            dst2 = src.Clone

            Dim mean As cv.Scalar
            Dim stdev As cv.Scalar
            cv.Cv2.MeanStdDev(dst2(templateRect), mean, stdev)

            If stdev > options.minStdev Then
                Dim t = templateRect
                Dim w = t.Width + options.pad * 2
                Dim h = t.Height + options.pad * 2
                Dim x = Math.Abs(t.X - options.pad)
                Dim y = Math.Abs(t.Y - options.pad)
                searchRect = New cv.Rect(x, y, If(w < lastFrame.Width, w, lastFrame.Width - x - 1), If(h < lastFrame.Height, h, lastFrame.Height - y - 1))
                match.template = lastFrame(searchRect)
                match.Run(src(templateRect))

                If match.correlation > options.corrThreshold Then
                    Dim maxLoc = New cv.Point(match.newCenter.X, match.newCenter.Y)
                    shiftX = templateRect.X - maxLoc.X - searchRect.X
                    shiftY = templateRect.Y - maxLoc.Y - searchRect.Y
                    Dim x1 = If(shiftX < 0, Math.Abs(shiftX), 0)
                    Dim y1 = If(shiftY < 0, Math.Abs(shiftY), 0)

                    dst3.SetTo(0)

                    Dim x2 = If(shiftX < 0, 0, shiftX)
                    Dim y2 = If(shiftY < 0, 0, shiftY)
                    stableRect = New cv.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
                    Dim srcRect = New cv.Rect(x2, y2, stableRect.Width, stableRect.Height)
                    stableRect = New cv.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
                    src(srcRect).CopyTo(dst3(stableRect))
                    Dim nonZero = dst3.CountNonZero / (dst3.Width * dst3.Height)
                    If nonZero < (1 - options.lostMax) Then
                        labels(3) = "Lost pixels = " + Format(1 - nonZero, "00%")
                        resetImage = True
                    End If
                    labels(3) = "Offset (x, y) = (" + CStr(shiftX) + "," + CStr(shiftY) + "), " + Format(nonZero, "00%") + " preserved, cc=" + Format(match.correlation, fmt2)
                Else
                    labels(3) = "Below correlation threshold " + Format(options.corrThreshold, fmt2) + " with " +
                            Format(match.correlation, fmt2)
                    resetImage = True
                End If
            Else
                labels(3) = "Correlation rectangle stdev is " + Format(stdev(0), "00") + " - too low"
                resetImage = True
            End If

            If resetImage Then
                src.CopyTo(lastFrame)
                dst3 = lastFrame.Clone
            End If
            If standaloneTest() Then dst3.Rectangle(templateRect, white, 1) ' when not standaloneTest(), traceName doesn't want artificial rectangle.
        End Sub
    End Class









    Public Class XO_Stabilizer_BasicsTest : Inherits TaskParent
        Dim random As New PhaseCorrelate_RandomInput
        Dim stable As New XO_Stabilizer_Basics
        Public Sub New()
            labels(2) = "Unstable input to Stabilizer_Basics"
            desc = "Test the Stabilizer_Basics with random movement"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)

            random.Run(src)
            stable.Run(random.dst3.Clone)

            dst2 = stable.dst2
            dst3 = stable.dst3
            If standaloneTest() Then dst3.Rectangle(stable.templateRect, white, 1)
            labels(3) = stable.labels(3)
        End Sub
    End Class






    ' https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
    Public Class XO_Stabilizer_OpticalFlow : Inherits TaskParent
        Public inputFeat As New List(Of cv.Point2f)
        Public borderCrop = 30
        Dim sumScale As cv.Mat, sScale As cv.Mat, features1 As cv.Mat
        Dim errScale As cv.Mat, qScale As cv.Mat, rScale As cv.Mat
        Public Sub New()
            desc = "Stabilize video with a Kalman filter.  Shake camera to see image edges appear.  This is not really working!"
            labels(2) = "Stabilized Image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim vert_Border = borderCrop * src.Rows / src.Cols
            If taskAlg.optionsChanged Then
                errScale = New cv.Mat(New cv.Size(1, 5), cv.MatType.CV_64F, 1)
                qScale = New cv.Mat(New cv.Size(1, 5), cv.MatType.CV_64F, 0.004)
                rScale = New cv.Mat(New cv.Size(1, 5), cv.MatType.CV_64F, 0.5)
                sumScale = New cv.Mat(New cv.Size(1, 5), cv.MatType.CV_64F, 0)
                sScale = New cv.Mat(New cv.Size(1, 5), cv.MatType.CV_64F, 0)
            End If

            dst2 = src

            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            inputFeat = New List(Of cv.Point2f)(taskAlg.features)
            features1 = cv.Mat.FromPixelData(inputFeat.Count, 1, cv.MatType.CV_32FC2, inputFeat.ToArray)

            Static lastFrame As cv.Mat = src.Clone()
            If taskAlg.frameCount > 0 Then
                Dim features2 = New cv.Mat
                Dim status As New cv.Mat
                Dim err As New cv.Mat
                Dim winSize As New cv.Size(3, 3)
                cv.Cv2.CalcOpticalFlowPyrLK(src, lastFrame, features1, features2, status, err, winSize, 3, term, cv.OpticalFlowFlags.None)
                lastFrame = src.Clone()

                Dim commonPoints = New List(Of cv.Point2f)
                Dim lastFeatures As New List(Of cv.Point2f)
                For i = 0 To status.Rows - 1
                    If status.Get(Of Byte)(i, 0) Then
                        Dim pt1 = features1.Get(Of cv.Point2f)(i, 0)
                        Dim pt2 = features2.Get(Of cv.Point2f)(i, 0)
                        Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                        If length < 10 Then
                            commonPoints.Add(pt1)
                            lastFeatures.Add(pt2)
                        End If
                    End If
                Next

                If commonPoints.Count = 0 Or lastFeatures.Count = 0 Then Exit Sub ' nothing to work on...
                Dim affine = cv.Cv2.GetAffineTransform(commonPoints.ToArray, lastFeatures.ToArray)

                Dim dx = affine.Get(Of Double)(0, 2)
                Dim dy = affine.Get(Of Double)(1, 2)
                Dim da = Math.Atan2(affine.Get(Of Double)(1, 0), affine.Get(Of Double)(0, 0))
                Dim ds_x = affine.Get(Of Double)(0, 0) / Math.Cos(da)
                Dim ds_y = affine.Get(Of Double)(1, 1) / Math.Cos(da)
                Dim saveDX = dx, saveDY = dy, saveDA = da

                Dim text = "Original dx = " + Format(dx, fmt2) + vbCrLf + " dy = " + Format(dy, fmt2) + vbCrLf + " da = " + Format(da, fmt2)
                SetTrueText(text)

                Dim sx = ds_x, sy = ds_y

                Dim delta As cv.Mat = cv.Mat.FromPixelData(5, 1, cv.MatType.CV_64F, New Double() {ds_x, ds_y, da, dx, dy})
                cv.Cv2.Add(sumScale, delta, sumScale)

                Dim diff As New cv.Mat
                cv.Cv2.Subtract(sScale, sumScale, diff)

                da += diff.Get(Of Double)(2, 0)
                dx += diff.Get(Of Double)(3, 0)
                dy += diff.Get(Of Double)(4, 0)
                If Math.Abs(dx) > 50 Then dx = saveDX
                If Math.Abs(dy) > 50 Then dy = saveDY
                If Math.Abs(da) > 50 Then da = saveDA

                text = "dx = " + Format(dx, fmt2) + vbCrLf + " dy = " + Format(dy, fmt2) + vbCrLf + " da = " + Format(da, fmt2)
                SetTrueText(text, New cv.Point(10, 100))

                Dim smoothedMat = New cv.Mat(2, 3, cv.MatType.CV_64F)
                smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(da))
                smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(da))
                smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(da))
                smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(da))
                smoothedMat.Set(Of Double)(0, 2, dx)
                smoothedMat.Set(Of Double)(1, 2, dy)

                Dim smoothedFrame = taskAlg.color.WarpAffine(smoothedMat, src.Size())
                smoothedFrame = smoothedFrame(New cv.Range(vert_Border, smoothedFrame.Rows - vert_Border), New cv.Range(borderCrop, smoothedFrame.Cols - borderCrop))
                dst3 = smoothedFrame.Resize(src.Size())

                For i = 0 To commonPoints.Count - 1
                    DrawCircle(dst2, commonPoints.ElementAt(i), taskAlg.DotSize + 3, cv.Scalar.Red)
                    DrawCircle(dst2, lastFeatures.ElementAt(i), taskAlg.DotSize + 1, cv.Scalar.Blue)
                Next
            End If
            inputFeat = Nothing ' show that we consumed the current set of features.
        End Sub
    End Class










    Public Class XO_Stabilizer_CornerPoints : Inherits TaskParent
        Public basics As New Stable_Basics
        Public features As New List(Of cv.Point2f)
        Dim options As New Options_FAST
        Dim ul As cv.Rect, ur As cv.Rect, ll As cv.Rect, lr As cv.Rect
        Public Sub New()
            desc = "Track the FAST feature points found in the corners of the BGR image."
        End Sub
        Private Sub getKeyPoints(src As cv.Mat, r As cv.Rect)
            Dim kpoints() As cv.KeyPoint = cv.Cv2.FAST(src(r), options.FASTthreshold, True)
            For Each kp In kpoints
                features.Add(New cv.Point2f(kp.Pt.X + r.X, kp.Pt.Y + r.Y))
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If taskAlg.optionsChanged Then
                Dim size = taskAlg.brickSize
                ul = New cv.Rect(0, 0, size, size)
                ur = New cv.Rect(dst2.Width - size, 0, size, size)
                ll = New cv.Rect(0, dst2.Height - size, size, size)
                lr = New cv.Rect(dst2.Width - size, dst2.Height - size, size, size)
            End If

            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            features.Clear()
            getKeyPoints(src, ul)
            getKeyPoints(src, ur)
            getKeyPoints(src, ll)
            getKeyPoints(src, lr)

            dst2.SetTo(0)
            For Each pt In features
                DrawCircle(dst2, pt, taskAlg.DotSize, cv.Scalar.Yellow)
            Next
            labels(2) = "There were " + CStr(features.Count) + " key points detected"
        End Sub
    End Class




    Public Class XO_MatchRect_Basics : Inherits TaskParent
        Public match As New Match_Basics
        Public rectInput As New cv.Rect
        Public rectOutput As New cv.Rect
        Dim rectSave As New cv.Rect
        Public Sub New()
            desc = "Track a RedCloud rectangle using MatchTemplate.  Click on a cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.optionsChanged Then match.correlation = 0
            If match.correlation < taskAlg.fCorrThreshold Or rectSave <> rectInput Or taskAlg.mouseClickFlag Then
                If standalone Then
                    dst2 = runRedList(src, labels(2)).Clone
                    rectInput = taskAlg.oldrcD.rect
                End If
                rectSave = rectInput
                match.template = src(rectInput).Clone
            End If

            match.Run(src)
            rectOutput = match.newRect

            If standalone Then
                If taskAlg.heartBeat Then dst3.SetTo(0)
                DrawRect(dst3, rectOutput)
            End If
        End Sub
    End Class




    Public Class XO_MatchRect_RedCloud : Inherits TaskParent
        Dim matchRect As New XO_MatchRect_Basics
        Public Sub New()
            desc = "Track a RedCloud cell using MatchTemplate."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))
            taskAlg.ClickPoint = taskAlg.oldrcD.maxDist

            If taskAlg.heartBeat Then matchRect.rectInput = taskAlg.oldrcD.rect

            matchRect.Run(src)
            If standalone Then
                If taskAlg.heartBeat Then dst3.SetTo(0)
                DrawRect(dst3, matchRect.rectOutput)
            End If
            labels(2) = "MatchLine correlation = " + Format(matchRect.match.correlation, fmt3) +
                    " - Red = current gravity vector, yellow is matchLine output"
        End Sub
    End Class






    Public Class XO_MatchLine_Test : Inherits TaskParent
        Public cameraMotionProxy As New lpData
        Dim match As New LineEnds_Correlation
        Public Sub New()
            desc = "Find and track the longest line by matching line bricks."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.optionsChanged Then taskAlg.lines.lpList.Clear()

            dst2 = src.Clone
            If taskAlg.lines.lpList.Count > 0 Then
                cameraMotionProxy = taskAlg.lines.lpList(0)
                match.lpInput = cameraMotionProxy
                match.Run(src)
                dst1 = match.dst2

                labels(2) = "EndPoint1 correlation:  " + Format(match.p1Correlation, fmt3) + vbTab +
                        "EndPoint2 correlation:  " + Format(match.p1Correlation, fmt3)

                If match.p1Correlation < taskAlg.fCorrThreshold Or taskAlg.frameCount < 10 Or
               match.p2Correlation < taskAlg.fCorrThreshold Then

                    taskAlg.motionMask.SetTo(255) ' force a complete line detection
                    taskAlg.lines.Run(src.Clone)

                    match.lpInput = taskAlg.lines.lpList(0)
                    match.Run(src)
                End If
            End If

            dst3 = taskAlg.lines.dst3
            labels(3) = taskAlg.lines.labels(3)

            dst2.Line(cameraMotionProxy.p1, cameraMotionProxy.p2, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
        End Sub
    End Class










    Public Class XO_Match_Points : Inherits TaskParent
        Public ptx As New List(Of cv.Point2f)
        Public correlation As New List(Of Single)
        Public mPoint As New Match_Point
        Public Sub New()
            labels(2) = "Rectangle shown is the search rectangle."
            desc = "Track the selected points"
        End Sub
        Public Shared Sub DrawPolkaDot(pt As cv.Point2f, dst As cv.Mat)
            dst.Circle(pt, taskAlg.DotSize + 2, white, -1, taskAlg.lineType)
            dst.Circle(pt, taskAlg.DotSize, black, -1, taskAlg.lineType)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.firstPass Then mPoint.target = src.Clone

            If standaloneTest() Then
                ptx = New List(Of cv.Point2f)(taskAlg.features)
                SetTrueText("Move camera around to watch the point being tracked", 3)
            End If

            dst2 = src.Clone
            correlation.Clear()
            For i = 0 To ptx.Count - 1
                mPoint.pt = ptx(i)
                mPoint.Run(src)
                correlation.Add(mPoint.correlation)
                ptx(i) = mPoint.pt
                DrawPolkaDot(ptx(i), dst2)
            Next
            mPoint.target = src.Clone
        End Sub
    End Class







    Public Class Feature_PointTracker : Inherits TaskParent
        Dim flow As New Font_FlowText
        Dim mPoints As New XO_Match_Points
        Public Sub New()
            flow.parentData = Me
            flow.dst = 3
            labels(3) = "Correlation coefficients for each remaining cell"
            desc = "Use the top X goodFeatures and then use matchTemplate to find track them."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim pad = taskAlg.brickSize / 2
            strOut = ""
            If mPoints.ptx.Count <= 3 Then
                mPoints.ptx.Clear()
                For Each pt In taskAlg.features
                    mPoints.ptx.Add(pt)
                    Dim rect = ValidateRect(New cv.Rect(pt.X - pad, pt.Y - pad, taskAlg.brickSize, taskAlg.brickSize))
                Next
                strOut = "Restart tracking -----------------------------------------------------------------------------" + vbCrLf
            End If
            mPoints.Run(src)

            dst2 = src.Clone
            For i = mPoints.ptx.Count - 1 To 0 Step -1
                If mPoints.correlation(i) > taskAlg.fCorrThreshold Then
                    DrawCircle(dst2, mPoints.ptx(i), taskAlg.DotSize, taskAlg.highlight)
                    strOut += Format(mPoints.correlation(i), fmt3) + ", "
                Else
                    mPoints.ptx.RemoveAt(i)
                End If
            Next
            If standaloneTest() Then
                flow.nextMsg = strOut
                flow.Run(src)
            End If

            labels(2) = "Of the " + CStr(taskAlg.features.Count) + " input points, " + CStr(mPoints.ptx.Count) +
                    " points were tracked with correlation above " + Format(taskAlg.fCorrThreshold, fmt2)
        End Sub
    End Class





    Public Class XO_Line_LongestTest : Inherits TaskParent
        Public matchBrick As New Match_Brick
        Dim lp As New lpData
        Public Sub New()
            desc = "Identify a line by matching each of the points to the previous image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim threshold = taskAlg.fCorrThreshold
            Dim lplist = taskAlg.lines.lpList

            taskAlg.lineLongestChanged = False
            ' camera is often warming up for the first few images.
            If taskAlg.frameCount < 10 Or taskAlg.heartBeat Then
                lp = lplist(0)
                taskAlg.lineLongestChanged = True
            End If

            matchBrick.gridIndex = lp.p1GridIndex
            matchBrick.Run(emptyMat)
            Dim p1Correlation = matchBrick.correlation
            Dim p1 = New cv.Point(lp.p1.X + matchBrick.deltaX, lp.p1.Y + matchBrick.deltaY)

            strOut = matchBrick.labels(2) + vbCrLf
            labels(2) = matchBrick.labels(2) + vbTab

            matchBrick.gridIndex = lp.p2GridIndex
            matchBrick.Run(emptyMat)
            Dim p2Correlation = matchBrick.correlation
            Dim p2 = New cv.Point(lp.p2.X + matchBrick.deltaX, lp.p2.Y + matchBrick.deltaY)

            strOut += matchBrick.labels(2) + vbCrLf
            labels(2) += ", " + matchBrick.labels(2)

            If p1Correlation >= threshold And p2Correlation >= threshold Then
                lp = New lpData(p1, p2)
                taskAlg.lineLongestChanged = False
            Else
                taskAlg.lineLongestChanged = True
            End If

            If standaloneTest() Then
                dst2 = src
                vbc.DrawLine(dst2, lp)
                DrawRect(dst2, lp.rect)
                dst3 = taskAlg.lines.dst2
            End If

            ' taskAlg.lineLongest = lp
            Static strList As New List(Of String)
            strList.Add(strOut)
            If strList.Count > 10 Then strList.RemoveAt(0)
            strOut = ""
            For Each strNext In strList
                strOut += strNext
            Next
            SetTrueText(strOut, 3)
        End Sub
    End Class








    Public Class XO_Line_Matching2 : Inherits TaskParent
        Public match As New Match_Basics
        Public Sub New()
            desc = "For each line from the last frame, find its correlation to the current frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim correlations As New List(Of Single)
            Static lpLast = New List(Of lpData)(taskAlg.lines.lpList)
            For Each lp In lpLast
                match.template = taskAlg.gray(lp.rect)
                match.Run(taskAlg.gray.Clone)
                correlations.Add(match.correlation)
            Next

            dst2 = taskAlg.lines.dst2

            labels(2) = "Mean correlation of all the lines is " + Format(correlations.Average, fmt3)
            labels(3) = "Min/Max correlation = " + Format(correlations.Min, fmt3) + "/" + Format(correlations.Max, fmt3)
            lpLast = New List(Of lpData)(taskAlg.lines.lpList)
        End Sub
    End Class





    Public Class XO_Line_Gravity : Inherits TaskParent
        Dim match As New Match_Basics
        Public lp As lpData
        Public Sub New()
            desc = "Find the longest RGB line that is parallel to gravity"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lplist = taskAlg.lines.lpList

            ' camera is often warming up for the first few images.
            If match.correlation < taskAlg.fCorrThreshold Or taskAlg.frameCount < 10 Or lp Is Nothing Then
                lp = lplist(0)
                For Each lp In lplist
                    If Math.Abs(taskAlg.lineGravity.angle - lp.angle) < taskAlg.angleThreshold Then Exit For
                Next
                match.template = src(lp.rect)
            End If

            If Math.Abs(taskAlg.lineGravity.angle - lp.angle) >= taskAlg.angleThreshold Then
                lp = Nothing
                Exit Sub
            End If

            match.Run(src.Clone)

            If match.correlation < taskAlg.fCorrThreshold Then
                If lplist.Count > 1 Then
                    Dim histogram As New cv.Mat
                    cv.Cv2.CalcHist({taskAlg.lines.dst1(lp.rect)}, {0}, emptyMat, histogram, 1, {lplist.Count},
                                 New cv.Rangef() {New cv.Rangef(1, lplist.Count)})

                    Dim histArray(histogram.Total - 1) As Single
                    Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

                    Dim histList = histArray.ToList
                    ' pick the lp that has the most pixels in the lp.rect.
                    lp = lplist(histList.IndexOf(histList.Max))
                    match.template = src(lp.rect)
                    match.correlation = 1
                Else
                    match.correlation = 0 ' force a restart
                End If
            Else
                Dim deltaX = match.newRect.X - lp.rect.X
                Dim deltaY = match.newRect.Y - lp.rect.Y
                Dim p1 = New cv.Point(lp.p1.X + deltaX, lp.p1.Y + deltaY)
                Dim p2 = New cv.Point(lp.p2.X + deltaX, lp.p2.Y + deltaY)
                lp = New lpData(p1, p2)
            End If

            If standaloneTest() Then
                dst2 = src
                dst2.Rectangle(lp.rect, taskAlg.highlight, taskAlg.lineWidth)
                vbc.DrawLine(dst2, lp.p1, lp.p2)
            End If

            labels(2) = "Selected line has a correlation of " + Format(match.correlation, fmt3) + " with the previous frame."
        End Sub
    End Class






    Public Class XO_Line_ExtendLineTest : Inherits TaskParent
        Public Sub New()
            labels = {"", "", "Random Line drawn", ""}
            desc = "Test lpData constructor with random values to make sure lines are extended properly"
        End Sub

        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.heartBeat Then
                Dim p1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                Dim p2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))

                Dim lp = New lpData(p1, p2)
                dst2 = src
                vbc.DrawLine(dst2, lp.pE1, lp.pE2, taskAlg.highlight)
                DrawCircle(dst2, p1, taskAlg.DotSize + 2, cv.Scalar.Red)
                DrawCircle(dst2, p2, taskAlg.DotSize + 2, cv.Scalar.Red)
            End If
        End Sub
    End Class




    Public Class XO_Line_ExtendAll : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Public Sub New()
            labels = {"", "", "Image output from Line_Core", "The extended line for each line found in Line_Core"}
            desc = "Create a list of all the extended lines in an image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = taskAlg.lines.dst2

            dst3 = src.Clone
            lpList.Clear()
            For Each lp In taskAlg.lines.lpList
                vbc.DrawLine(dst3, lp.pE1, lp.pE2, taskAlg.highlight)
                lpList.Add(New lpData(lp.pE1, lp.pE2))
            Next
        End Sub
    End Class










    Public Class XO_Line_Intercepts : Inherits TaskParent
        Public extended As New XO_Line_ExtendLineTest
        Public p1List As New List(Of cv.Point2f)
        Public p2List As New List(Of cv.Point2f)
        Public options As New Options_Intercepts
        Public intercept As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Public topIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Public botIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Public leftIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Public rightIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Public interceptArray = {topIntercepts, botIntercepts, leftIntercepts, rightIntercepts}
        Public Sub New()
            labels(2) = "Highlight line x- and y-intercepts.  Move mouse over the image."
            desc = "Show lines with similar y-intercepts"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst2 = src
            p1List.Clear()
            p2List.Clear()
            intercept = interceptArray(options.selectedIntercept)
            topIntercepts.Clear()
            botIntercepts.Clear()
            leftIntercepts.Clear()
            rightIntercepts.Clear()
            Dim index As Integer
            For Each lp In taskAlg.lines.lpList
                Dim minXX = Math.Min(lp.p1.X, lp.p2.X)
                If lp.p1.X <> minXX Then ' leftmost point is always in p1
                    Dim tmp = lp.p1
                    lp.p1 = lp.p2
                    lp.p2 = tmp
                End If

                p1List.Add(lp.p1)
                p2List.Add(lp.p2)
                vbc.DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)

                Dim saveP1 = lp.p1, saveP2 = lp.p2

                If lp.pE1.X = 0 Then leftIntercepts.Add(saveP1.Y, index)
                If lp.pE1.Y = 0 Then topIntercepts.Add(saveP1.X, index)
                If lp.pE1.X = dst2.Width Then rightIntercepts.Add(saveP1.Y, index)
                If lp.pE1.Y = dst2.Height Then botIntercepts.Add(saveP1.X, index)

                If lp.pE2.X = 0 Then leftIntercepts.Add(saveP2.Y, index)
                If lp.pE2.Y = 0 Then topIntercepts.Add(saveP2.X, index)
                If lp.pE2.X = dst2.Width Then rightIntercepts.Add(saveP2.Y, index)
                If lp.pE2.Y = dst2.Height Then botIntercepts.Add(saveP2.X, index)
                index += 1
            Next

            If standaloneTest() Then
                For Each inter In intercept
                    If Math.Abs(options.mouseMovePoint - inter.Key) < options.interceptRange Then
                        vbc.DrawLine(dst2, p1List(inter.Value), p2List(inter.Value), cv.Scalar.Blue)
                    End If
                Next
            End If
        End Sub
    End Class






    Public Class XO_Line_BasicsNoAging : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Public lpRectMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public rawLines As New Line_Core
        Public Sub New()
            desc = "Retain line from earlier image if not in motion mask.  If new line is in motion mask, add it."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.optionsChanged Then
                lpList.Clear()
                taskAlg.motionMask.SetTo(255)
            End If

            Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)

            rawLines.Run(src)
            dst3 = rawLines.dst2
            labels(3) = rawLines.labels(2)

            For Each lp In rawLines.lpList
                sortlines.Add(lp.length, lp)
            Next

            lpList.Clear()
            dst2 = src
            lpRectMap.SetTo(0)
            For Each lp In sortlines.Values
                lpList.Add(lp)
                vbc.DrawLine(dst2, lp.p1, lp.p2)
                lpRectMap.Line(lp.p1, lp.p2, sortlines.Values.IndexOf(lp) + 1, taskAlg.lineWidth * 3, cv.LineTypes.Link8)

                If standaloneTest() Then
                    dst2.Line(lp.p1, lp.p2, taskAlg.highlight, 10, cv.LineTypes.Link8)
                End If
                If lpList.Count >= taskAlg.FeatureSampleSize Then Exit For
            Next

            If standaloneTest() Then dst1 = PaletteFull(lpRectMap)
            labels(2) = "Of the " + CStr(rawLines.lpList.Count) + " raw lines found, shown below are the " + CStr(lpList.Count) + " longest."
        End Sub
    End Class





    Public Class XO_Line_ViewLeftRight : Inherits TaskParent
        Dim lines As New Line_Basics
        Dim rawLines As New Line_Core
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
            desc = "Find lines in the left and right images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            lines.Run(taskAlg.leftView)
            dst2.SetTo(0)
            For Each lp In taskAlg.lines.lpList
                dst2.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth)
            Next
            labels(2) = lines.labels(2)

            rawLines.Run(taskAlg.rightView)
            dst3 = rawLines.dst2
            labels(3) = rawLines.labels(2)
        End Sub
    End Class







    Public Class XO_Swarm_KNN : Inherits TaskParent
        Dim swarm As New Swarm_Basics
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Use KNN to find the nearest point to an endpoint and connect the 2 lines with a line."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            swarm.options.Run()
            dst2 = taskAlg.lines.dst2

            dst3.SetTo(0)
            swarm.knn.queries.Clear()
            For Each lp In taskAlg.lines.lpList
                swarm.knn.queries.Add(lp.p1)
                swarm.knn.queries.Add(lp.p2)
                vbc.DrawLine(dst3, lp.p1, lp.p2, 255)
            Next
            swarm.knn.trainInput = New List(Of cv.Point2f)(swarm.knn.queries)
            swarm.knn.Run(src)

            dst3 = swarm.DrawLines().Clone
            labels(2) = taskAlg.lines.labels(2)
        End Sub
    End Class








    Public Class XO_Line_MatchGravity : Inherits TaskParent
        Public gLines As New List(Of lpData)
        Public Sub New()
            desc = "Find all the lines similar to gravity."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = taskAlg.lines.dst3
            labels(3) = taskAlg.lines.labels(3)

            gLines.Clear()
            dst2 = src.Clone
            For Each lp In taskAlg.lines.lpList
                If Math.Abs(taskAlg.lineGravity.angle - lp.angle) < 2 Then
                    dst2.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth + 1, taskAlg.lineType)
                    gLines.Add(lp)
                End If
            Next

            If gLines.Count = 0 Then
                labels(2) = "There were no lines parallel to gravity in the RGB image."
            Else
                labels(2) = "Of the " + CStr(gLines.Count) + " lines found, the best line parallel to gravity was " +
                       CStr(CInt(gLines(0).length)) + " pixels in length."
            End If
        End Sub
    End Class






    Public Class XO_Line_RawSubset : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Public subsetRect As cv.Rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
        Public rawLines As New Line_Core
        Public Sub New()
            taskAlg.drawRect = New cv.Rect(25, 25, 25, 25)
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset rectangle (provided externally)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then subsetRect = taskAlg.drawRect
            rawLines.Run(src(subsetRect))

            lpList.Clear()
            dst2 = taskAlg.lines.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For Each lp In rawLines.lpList
                dst2(subsetRect).Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth * 3, taskAlg.lineType)
                lpList.Add(lp)
            Next
            labels(2) = CStr(lpList.Count) + " lines were detected in src(subsetRect)"
        End Sub
    End Class





    Public Class XO_Line_TrigHorizontal : Inherits TaskParent
        Public horizList As New List(Of lpData)
        Public Sub New()
            desc = "Find all the Horizontal lines with horizon vector"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src

            Dim p1 = taskAlg.lineHorizon.p1, p2 = taskAlg.lineHorizon.p2
            Dim sideOpposite = p2.Y - p1.Y
            If p1.X = 0 Then sideOpposite = p1.Y - p2.Y
            Dim hAngle = Math.Atan(sideOpposite / dst2.Width) * 57.2958

            horizList.Clear()
            For Each lp In taskAlg.lines.lpList
                If Math.Abs(taskAlg.lineHorizon.angle - lp.angle) < taskAlg.angleThreshold Then
                    vbc.DrawLine(dst2, lp.p1, lp.p2)
                    horizList.Add(lp)
                End If
            Next
            labels(2) = "There are " + CStr(horizList.Count) + " lines similar to the horizon " + Format(hAngle, fmt1) + " degrees"
        End Sub
    End Class




    Public Class XO_Line_TrigVertical : Inherits TaskParent
        Public vertList As New List(Of lpData)
        Public Sub New()
            desc = "Find all the vertical lines with gravity vector"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src

            Dim p1 = taskAlg.lineGravity.p1, p2 = taskAlg.lineGravity.p2
            Dim sideOpposite = p2.X - p1.X
            If p1.Y = 0 Then sideOpposite = p1.X - p2.X
            Dim gAngle = Math.Atan(sideOpposite / dst2.Height) * 57.2958

            vertList.Clear()
            For Each lp In taskAlg.lines.lpList
                If Math.Abs(taskAlg.lineGravity.angle - lp.angle) < taskAlg.angleThreshold Then
                    vbc.DrawLine(dst2, lp.p1, lp.p2)
                    vertList.Add(lp)
                End If
            Next
            labels(2) = "There are " + CStr(vertList.Count) + " lines similar to the Gravity " + Format(gAngle, fmt1) + " degrees"
        End Sub
    End Class







    Public Class XO_Line_VerticalHorizontalRaw : Inherits TaskParent
        Dim verts As New XO_Line_TrigVertical
        Dim horiz As New XO_Line_TrigHorizontal
        Public vertList As New List(Of lpData)
        Public horizList As New List(Of lpData)
        Public Sub New()
            taskAlg.gOptions.LineWidth.Value = 2
            labels(3) = "Vertical lines are in yellow and horizontal lines in red."
            desc = "Highlight both vertical and horizontal lines"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone
            verts.Run(src)
            horiz.Run(src)

            Dim vList As New SortedList(Of Integer, lpData)(New compareAllowIdenticalIntegerInverted)
            Dim hList As New SortedList(Of Integer, lpData)(New compareAllowIdenticalIntegerInverted)

            dst3.SetTo(0)
            For Each lp In verts.vertList
                vList.Add(lp.length, lp)
                vbc.DrawLine(dst2, lp.p1, lp.p2, taskAlg.highlight)
                vbc.DrawLine(dst3, lp.p1, lp.p2, taskAlg.highlight)
            Next

            For Each lp In horiz.horizList
                hList.Add(lp.length, lp)
                vbc.DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Red)
                vbc.DrawLine(dst3, lp.p1, lp.p2, cv.Scalar.Red)
            Next

            vertList = New List(Of lpData)(vList.Values)
            horizList = New List(Of lpData)(hList.Values)
            labels(2) = "Number of lines identified (vertical/horizontal): " + CStr(vList.Count) + "/" + CStr(hList.Count)
        End Sub
    End Class







    Public Class XO_Line_FindNearest : Inherits TaskParent
        Public lpInput As lpData
        Public lpOutput As lpData
        Public distance As Single
        Public Sub New()
            desc = "Find the line that is closest to the input line"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then lpInput = taskAlg.lineLongest
            Dim lpList = taskAlg.lines.lpList

            Dim sortDistance As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
            For Each lp In lpList
                sortDistance.Add(lpInput.ptCenter.DistanceTo(lp.ptCenter), lp.index)
            Next

            lpOutput = lpList(sortDistance.ElementAt(0).Value)

            If standaloneTest() Then
                dst2 = src
                vbc.DrawLine(dst2, lpOutput.p1, lpOutput.p2)
                labels(2) = "Distance = " + Format(sortDistance.ElementAt(0).Key, fmt1)
            End If
        End Sub
    End Class







    Public Class XO_KNN_LongestLine : Inherits TaskParent
        Public lp As lpData
        Dim knn As New KNN_NNBasics
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            OptionParent.FindSlider("KNN Dimension").Value = 6
            desc = "Track the longest line"
        End Sub
        Private Sub prepEntry(knnList As List(Of Single), lpNext As lpData)
            Dim brick1 = taskAlg.gridMap.Get(Of Integer)(lpNext.p1.Y, lpNext.p1.X)
            Dim brick2 = taskAlg.gridMap.Get(Of Integer)(lpNext.p2.Y, lpNext.p2.X)
            knnList.Add(lpNext.p1.X)
            knnList.Add(lpNext.p1.Y)
            knnList.Add(lpNext.p2.X)
            knnList.Add(lpNext.p2.Y)
            knnList.Add(brick1)
            knnList.Add(brick2)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lplist = taskAlg.lines.lpList

            If standalone And taskAlg.heartBeatLT Then lp = lplist(0)

            knn.trainInput.Clear()
            For Each lpNext In lplist
                prepEntry(knn.trainInput, lpNext)
            Next

            knn.queries.Clear()
            prepEntry(knn.queries, lp)

            knn.Run(emptyMat)

            lp = lplist(knn.result(0, 0))
            dst2 = src
            dst2.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth + 1, taskAlg.lineType)

            dst3 = taskAlg.lines.dst3
            labels(3) = taskAlg.lines.labels(3)

            Dim lpRectMap = XO_Line_CoreNew.createMap()
            dst1 = PaletteBlackZero(lpRectMap)
        End Sub
    End Class







    Public Class XO_KNN_BoundingRect : Inherits TaskParent
        Public lp As lpData
        Dim rawlines As New Line_Core
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Find the line with the largest bounding rectangle."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lplist = taskAlg.lines.lpList

            If standalone And taskAlg.heartBeatLT Then
                Dim sortRects As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
                For Each lpNext In lplist
                    sortRects.Add(lpNext.rect.Width * lpNext.rect.Height, lpNext.index)
                Next
                lp = lplist(sortRects.ElementAt(0).Value)
            End If

            Dim lpRectMap = XO_Line_CoreNew.createMap()
            dst1 = PaletteBlackZero(lpRectMap)
            DrawCircle(dst1, lp.ptCenter)

            Dim index = lpRectMap.Get(Of Byte)(lp.ptCenter.Y, lp.ptCenter.X)
            If index > 0 Then lp = lplist(index - 1)
            dst2 = src
            dst2.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth + 1, taskAlg.lineType)

            dst3 = taskAlg.lines.dst3
            labels(3) = taskAlg.lines.labels(3)
        End Sub
    End Class







    ' https://stackoverflow.com/questions/7446126/opencv-2d-line-intersection-helper-function
    Public Class XO_Line_IntersectionPT : Inherits TaskParent
        Public p1 As cv.Point2f, p2 As cv.Point2f, p3 As cv.Point2f, p4 As cv.Point2f
        Public intersectionPoint As cv.Point2f
        Public Sub New()
            desc = "Determine if 2 lines intersect, where the point is, and if that point is in the image."
        End Sub

        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.heartBeat Then
                p1 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                p2 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                p3 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                p4 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            End If

            intersectionPoint = Line_Intersection.IntersectTest(p1, p2, p3, p4)
            intersectionPoint = Line_Intersection.IntersectTest(New lpData(p1, p2), New lpData(p3, p4))

            dst2.SetTo(0)
            dst2.Line(p1, p2, cv.Scalar.Yellow, taskAlg.lineWidth, taskAlg.lineType)
            dst2.Line(p3, p4, cv.Scalar.Yellow, taskAlg.lineWidth, taskAlg.lineType)
            If intersectionPoint <> New cv.Point2f Then
                DrawCircle(dst2, intersectionPoint, taskAlg.DotSize + 4, white)
                labels(2) = "Intersection point = " + CStr(CInt(intersectionPoint.X)) + " x " + CStr(CInt(intersectionPoint.Y))
            Else
                labels(2) = "Parallel!!!"
            End If
            If intersectionPoint.X < 0 Or intersectionPoint.X > dst2.Width Or intersectionPoint.Y < 0 Or intersectionPoint.Y > dst2.Height Then
                labels(2) += " (off screen)"
            End If
        End Sub
    End Class








    Public Class XO_Line_Grid : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Public rawLines As New Line_Core
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "find the lines in each grid rectangle"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = src
            dst2.SetTo(0)
            For Each rect In taskAlg.gridNabeRects
                rawLines.Run(src(rect))
                For Each lp In rawLines.lpList
                    dst2(rect).Line(lp.p1, lp.p2, 255, taskAlg.lineWidth, taskAlg.lineType)
                    vbc.DrawLine(dst3, lp.p1, lp.p2)
                    lpList.Add(lp)
                Next
            Next
        End Sub
    End Class









    Public Class XO_Line_GravityToLongest : Inherits TaskParent
        Dim kalman As New Kalman_Basics
        Dim matchLine As New LineEnds_Correlation
        Public Sub New()
            desc = "Highlight both vertical and horizontal lines"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim gravityDelta As Single = taskAlg.lineGravity.pE1.X - taskAlg.lineGravity.pE2.X

            kalman.kInput = {gravityDelta}
            kalman.Run(emptyMat)
            gravityDelta = kalman.kOutput(0)

            matchLine.lpInput = Nothing
            For Each lp In taskAlg.lines.lpList
                If Math.Abs(lp.angle) > 45 Then
                    matchLine.lpInput = lp
                    Exit For
                End If
            Next
            If matchLine.lpInput Is Nothing Then Exit Sub
            matchLine.Run(src)
            dst2 = matchLine.dst2
            dst3 = taskAlg.lines.dst2
        End Sub
    End Class











    Public Class XO_Line_GravityToAverage : Inherits TaskParent
        Public vertList As New List(Of lpData)
        Public Sub New()
            desc = "Highlight both vertical and horizontal lines - not terribly good..."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim gravityDelta As Single = taskAlg.lineGravity.pE1.X - taskAlg.lineGravity.pE2.X

            dst2 = src
            If standalone Then dst3 = taskAlg.lines.dst2
            Dim deltaList As New List(Of Single)
            vertList.Clear()
            For Each lp In taskAlg.lines.lpList
                If Math.Abs(lp.angle) > 45 And Math.Sign(taskAlg.lineGravity.slope) = Math.Sign(lp.slope) Then
                    Dim delta = lp.pE1.X - lp.pE2.X
                    If Math.Abs(gravityDelta - delta) < taskAlg.gravityBasics.options.pixelThreshold Then
                        deltaList.Add(delta)
                        vertList.Add(lp)
                        vbc.DrawLine(dst2, lp.pE1, lp.pE2)
                        If standalone Then vbc.DrawLine(dst3, lp.p1, lp.p2, taskAlg.highlight)
                    End If
                End If
            Next

            If taskAlg.heartBeat Then
                labels(3) = "Gravity offset at image edge = " + Format(gravityDelta, fmt3) + " and m = " +
                        Format(taskAlg.lineGravity.slope, fmt3)
                If deltaList.Count > 0 Then
                    labels(2) = Format(gravityDelta, fmt3) + "/" + Format(deltaList.Average(), fmt3) + " gravity delta/line average delta"
                Else
                    labels(2) = "No lines matched the gravity vector..."
                End If
            End If
        End Sub
    End Class







    Public Class XO_Line_Parallel : Inherits TaskParent
        Dim extendAll As New XO_Line_ExtendAll
        Dim knn As New KNN_Basics
        Public parList As New List(Of coinPoints)
        Public Sub New()
            labels = {"", "", "Image output from Line_Core", "Parallel extended lines"}
            desc = "Use KNN to find which lines are near each other and parallel"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            extendAll.Run(src)
            dst3 = extendAll.dst2

            knn.queries.Clear()
            For Each lp In extendAll.lpList
                knn.queries.Add(New cv.Point2f((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2))
            Next
            knn.trainInput = New List(Of cv.Point2f)(knn.queries)

            If knn.queries.Count = 0 Then Exit Sub ' no input...possible in a dark room...

            knn.Run(src)
            dst2 = src.Clone
            parList.Clear()
            Dim checkList As New List(Of cv.Point)
            For i = 0 To knn.result.GetUpperBound(0) - 1
                For j = 0 To knn.queries.Count - 1
                    Dim index = knn.result(i, j)
                    If index >= extendAll.lpList.Count Or index < 0 Then Continue For
                    Dim lp = extendAll.lpList(index)
                    Dim elp = extendAll.lpList(i)
                    Dim mid = knn.queries(i)
                    Dim near = knn.trainInput(index)
                    Dim distanceMid = mid.DistanceTo(near)
                    Dim distance1 = lp.p1.DistanceTo(elp.p1)
                    Dim distance2 = lp.p2.DistanceTo(elp.p2)
                    If distance1 > distanceMid * 2 Then
                        distance1 = lp.p1.DistanceTo(elp.p2)
                        distance2 = lp.p2.DistanceTo(elp.p1)
                    End If
                    If distance1 < distanceMid * 2 And distance2 < distanceMid * 2 Then
                        Dim cp As coinPoints

                        Dim mps = taskAlg.lines.lpList(index)
                        cp.p1 = mps.p1
                        cp.p2 = mps.p2

                        mps = taskAlg.lines.lpList(i)
                        cp.p3 = mps.p1
                        cp.p4 = mps.p2

                        If checkList.Contains(cp.p1) = False And checkList.Contains(cp.p2) = False And checkList.Contains(cp.p3) = False And checkList.Contains(cp.p4) = False Then
                            If (cp.p1 = cp.p3 Or cp.p1 = cp.p4) And (cp.p2 = cp.p3 Or cp.p2 = cp.p4) Then
                                ' duplicate points...
                            Else
                                vbc.DrawLine(dst2, cp.p1, cp.p2, taskAlg.highlight)
                                vbc.DrawLine(dst2, cp.p3, cp.p4, cv.Scalar.Red)
                                parList.Add(cp)
                                checkList.Add(cp.p1)
                                checkList.Add(cp.p2)
                                checkList.Add(cp.p3)
                                checkList.Add(cp.p4)
                            End If
                        End If
                    End If
                Next
            Next
            labels(2) = CStr(parList.Count) + " parallel lines were found in the image"
            labels(3) = CStr(extendAll.lpList.Count) + " lines were found in the image before finding the parallel lines"
        End Sub
    End Class










    Public Class XO_Line_Points : Inherits TaskParent
        Dim knn As New KNN_Basics
        Public Sub New()
            desc = "Display end points of the lines and map them."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = taskAlg.lines.dst2

            knn.queries.Clear()
            For Each lp In taskAlg.lines.lpList
                Dim rect = taskAlg.gridNabeRects(taskAlg.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
                dst2.Rectangle(rect, taskAlg.highlight, taskAlg.lineWidth)
                knn.queries.Add(lp.ptCenter)
            Next

            Static lastQueries As New List(Of cv.Point2f)(knn.queries)
            knn.trainInput = lastQueries


            knn.Run(emptyMat)

            dst3 = taskAlg.lines.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For i = 0 To knn.neighbors.Count - 1
                Dim p1 = knn.queries(i)
                Dim p2 = knn.trainInput(knn.neighbors(i)(0))
                dst3.Line(p1, p2, taskAlg.highlight, taskAlg.lineWidth + 3, taskAlg.lineType)
            Next

            lastQueries = New List(Of cv.Point2f)(knn.queries)
        End Sub
    End Class





    Public Class XO_Line_RawEPLines : Inherits TaskParent
        Dim ld As cv.XImgProc.FastLineDetector
        Public lpList As New List(Of lpData)
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
            desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

            Dim lines = ld.Detect(src)
            Dim tmplist As New List(Of lpData)
            dst3.SetTo(0)
            For Each v In lines
                If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                    Dim p1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                    Dim p2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                    If p1.X >= 0 And p1.X < dst2.Width And p1.Y >= 0 And p1.Y < dst2.Height And
                   p2.X >= 0 And p2.X < dst2.Width And p2.Y >= 0 And p2.Y < dst2.Height Then
                        p1 = lpData.validatePoint(p1)
                        p2 = lpData.validatePoint(p2)
                        Dim lp = New lpData(p1, p2)
                        lp.index = tmplist.Count
                        tmplist.Add(lp)
                        vbc.DrawLine(dst3, lp, white)
                    End If
                End If
            Next

            Dim removeList As New List(Of Integer)
            For Each lp In tmplist
                Dim x1 = CInt(lp.pE1.X)
                Dim y1 = CInt(lp.pE1.Y)
                Dim x2 = CInt(lp.pE2.X)
                Dim y2 = CInt(lp.pE2.Y)
                For j = lp.index + 1 To tmplist.Count - 1
                    If CInt(tmplist(j).pE1.X) <> x1 Then Continue For
                    If CInt(tmplist(j).pE1.Y) <> y1 Then Continue For
                    If CInt(tmplist(j).pE2.X) <> x2 Then Continue For
                    If CInt(tmplist(j).pE2.Y) <> y2 Then Continue For
                    If removeList.Contains(tmplist(j).index) = False Then removeList.Add(tmplist(j).index)
                Next
            Next

            lpList.Clear()
            For Each lp In tmplist
                If removeList.Contains(lp.index) = False Then lpList.Add(New lpData(lp.pE1, lp.pE2))
            Next

            dst2.SetTo(0)
            For Each lp In lpList
                dst2.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth + 1, taskAlg.lineType)
            Next

            labels(2) = CStr(lpList.Count) + " highlighted lines were detected in the current frame. Others were too similar."
            labels(3) = "There were " + CStr(removeList.Count) + " coincident lines"
        End Sub
        Public Sub Close()
            ld.Dispose()
        End Sub
    End Class









    Public Class XO_Line_RawSorted : Inherits TaskParent
        Dim ld As cv.XImgProc.FastLineDetector
        Public lpList As New List(Of lpData)
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
            desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

            Dim lines = ld.Detect(src)

            Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
            For Each v In lines
                If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                    Dim p1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                    Dim p2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                    If p1.X >= 0 And p1.X < dst2.Width And p1.Y >= 0 And p1.Y < dst2.Height And
                   p2.X >= 0 And p2.X < dst2.Width And p2.Y >= 0 And p2.Y < dst2.Height Then
                        Dim lp = New lpData(p1, p2)
                        sortlines.Add(lp.length, lp)
                    End If
                End If
            Next

            lpList.Clear()
            For Each lp In sortlines.Values
                lp.p1 = lpData.validatePoint(lp.p1)
                lp.p2 = lpData.validatePoint(lp.p2)
                lpList.Add(lp)
            Next

            If standaloneTest() Then
                dst2.SetTo(0)
                For Each lp In lpList
                    dst2.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth, taskAlg.lineType)
                Next
            End If

            labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
        End Sub
        Public Sub Close()
            ld.Dispose()
        End Sub
    End Class





    Public Class XO_MotionCam_MultiLine : Inherits TaskParent
        Public edgeList As New List(Of SortedList(Of Single, Integer))
        Public minDistance As Integer = dst2.Width * 0.02
        Dim knn As New XO_KNN_EdgePoints
        Public Sub New()
            desc = "Find all the line edge points and display them."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = taskAlg.lines.dst2
            labels(3) = "The top " + CStr(taskAlg.lines.lpList.Count) + " longest lines in the image."

            knn.lpInput = taskAlg.lines.lpList
            knn.Run(emptyMat)

            For Each lpIn In taskAlg.lines.lpList
                Dim lp = HullLine_EdgePoints.EdgePointOffset(lpIn, 1)
                DrawCircle(dst2, New cv.Point(CInt(lp.pE1.X), CInt(lp.pE1.Y)))
                DrawCircle(dst2, New cv.Point(CInt(lp.pE2.X), CInt(lp.pE2.Y)))
            Next

            Static lpLast As New List(Of lpData)(taskAlg.lines.lpList)
            For Each lpIn In lpLast
                Dim lp = HullLine_EdgePoints.EdgePointOffset(lpIn, 5)
                DrawCircle(dst2, New cv.Point(CInt(lp.pE1.X), CInt(lp.pE1.Y)), white)
                DrawCircle(dst2, New cv.Point(CInt(lp.pE2.X), CInt(lp.pE2.Y)), white)
            Next

            lpLast = New List(Of lpData)(taskAlg.lines.lpList)

            labels(2) = knn.labels(2)
        End Sub
    End Class






    Public Class XO_MotionCam_MatchLast : Inherits TaskParent
        Dim motion As New XO_MotionCam_SideApproach
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Find the common trends in the image edge points of the top, left, right, and bottom."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motion.Run(src)
            dst1 = motion.dst1
            labels(1) = motion.labels(1)

            Static edgeList As New List(Of SortedList(Of Single, Integer))(motion.edgeList)
            Static lpLastList As New List(Of lpData)(taskAlg.lines.lpList)

            For i = 0 To edgeList.Count - 1
                If edgeList(i).Count = motion.edgeList(i).Count Then
                    For j = 0 To edgeList(i).Count - 1
                        If edgeList(i).ElementAt(j).Key <> motion.edgeList(i).ElementAt(j).Key Then Dim k = 0
                    Next
                End If
            Next

            motion.buildDisplay(edgeList, lpLastList, 20, white)
            dst2 = motion.dst2
            trueData = motion.trueData

            edgeList = New List(Of SortedList(Of Single, Integer))(motion.edgeList)
            lpLastList = New List(Of lpData)(taskAlg.lines.lpList)

            labels(2) = motion.labels(2) + "  White points are for the previous frame"
        End Sub
    End Class





    Public Class XO_MotionCam_SideApproach : Inherits TaskParent
        Public edgeList As New List(Of SortedList(Of Single, Integer))
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Find all the line edge points and display them."
        End Sub
        Public Sub buildDisplay(edgePoints As List(Of SortedList(Of Single, Integer)), lpList As List(Of lpData),
                            offset1 As Integer, color As cv.Scalar)
            Dim pt As cv.Point2f
            Dim index As Integer
            For Each sortlist In edgePoints
                Dim ptIndex As Integer = 0
                For Each ele In sortlist
                    Dim lp = lpList(ele.Value)

                    Select Case index
                        Case 0 ' top
                            pt = New cv.Point2f(ele.Key, offset1)
                        Case 1 ' left
                            pt = New cv.Point2f(offset1, ele.Key)
                        Case 2 ' right
                            pt = New cv.Point2f(dst2.Width - 10 - offset1, ele.Key)
                        Case 3 ' bottom
                            pt = New cv.Point2f(ele.Key, dst2.Height - 10 - offset1)
                    End Select

                    DrawCircle(dst2, pt, color)
                    ptIndex += 1
                Next
                index += 1
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = taskAlg.lines.dst2
            labels(1) = "The top " + CStr(taskAlg.lines.lpList.Count) + " longest lines in the image."

            Dim top As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
            Dim left As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
            Dim right As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
            Dim bottom As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)

            Dim lpList = taskAlg.lines.lpList
            For Each lp In lpList
                If lp.pE1.X = 0 Then left.Add(lp.pE1.Y, lp.index)
                If lp.pE1.Y = 0 Then top.Add(lp.pE1.X, lp.index)
                If lp.pE2.X = 0 Then left.Add(lp.pE2.Y, lp.index)
                If lp.pE2.Y = 0 Then top.Add(lp.pE2.X, lp.index)

                If lp.pE1.X = dst2.Width Then right.Add(lp.pE1.X, lp.index)
                If lp.pE1.Y = dst2.Height Then bottom.Add(lp.pE1.X, lp.index)
                If lp.pE2.X = dst2.Width Then right.Add(lp.pE2.Y, lp.index)
                If lp.pE2.Y = dst2.Height Then bottom.Add(lp.pE2.X, lp.index)
            Next

            edgeList.Clear()
            For i = 0 To 3
                Dim sortList = Choose(i + 1, top, left, right, bottom)
                edgeList.Add(sortList)
            Next

            dst2 = src.Clone
            buildDisplay(edgeList, lpList, 0, taskAlg.highlight)

            labels(2) = CStr(taskAlg.lines.lpList.Count * 2) + " edge points of the top " + CStr(taskAlg.lines.lpList.Count) +
                    " longest lines in the image are shown."
        End Sub
    End Class






    Public Class XO_MotionCam_Measure : Inherits TaskParent
        Public deltaX1 As Single, deltaX2 As Single, deltaY1 As Single, deltaY2 As Single
        Public Sub New()
            desc = "Measure how much the camera has moved."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static vecLast = taskAlg.lineLongest
            Dim vec = taskAlg.lineLongest

            deltaX1 = vec.pE1.X - vecLast.pE1.x
            deltaY1 = vec.pE1.Y - vecLast.pE1.Y

            deltaX2 = vec.pE2.X - vecLast.pE2.x
            deltaY2 = vec.pE2.Y - vecLast.pE2.Y

            Static strList As New List(Of String)
            strList.Add(Format(deltaX1, fmt1) + " " + Format(deltaX2, fmt1) + " " +
                    Format(deltaY1, fmt1) + " " + Format(deltaY2, fmt1) +
                    If(taskAlg.frameCount Mod 6 = 0, vbCrLf, vbTab))
            If strList.Count >= 132 Then strList.RemoveAt(0)

            strOut = ""
            For Each nextStr In strList
                strOut += nextStr
            Next
            SetTrueText(strOut, 3)

            vecLast = vec
        End Sub
    End Class






    Public Class XO_MotionCam_Plot : Inherits TaskParent
        Dim plot As New Plot_OverTime
        Dim measure As New XO_MotionCam_Measure
        Public Sub New()
            plot.minScale = -10
            plot.maxScale = 10
            desc = "Plot the variables describing the camera motion."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            measure.Run(src)

            plot.plotData = New cv.Scalar(measure.deltaX1, measure.deltaY1, measure.deltaX2, measure.deltaY2)
            plot.Run(src)
            dst2 = plot.dst2
            dst3 = plot.dst3
        End Sub
    End Class






    Public Class XO_Motion_TopFeatureFail : Inherits TaskParent
        Dim features As New Feature_General
        Public featureRects As New List(Of cv.Rect)
        Public searchRects As New List(Of cv.Rect)
        Dim match As New Match_Basics
        Dim saveMat As New cv.Mat
        Public Sub New()
            labels(2) = "Track the feature in the brick in the neighbors"
            desc = "Find the top feature cells and track them in the next frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim half As Integer = CInt(taskAlg.brickSize / 2)
            Dim pt As cv.Point
            If taskAlg.heartBeatLT Then
                features.Run(src)
                searchRects.Clear()
                featureRects.Clear()
                saveMat = src.Clone
                For Each pt In taskAlg.features
                    Dim index As Integer = taskAlg.gridMap.Get(Of Integer)(pt.Y, pt.X)
                    Dim roi = New cv.Rect(pt.X - half, pt.Y - half, taskAlg.brickSize, taskAlg.brickSize)
                    roi = ValidateRect(roi) ' stub bricks are fixed here 
                    featureRects.Add(roi)
                    searchRects.Add(taskAlg.gridNabeRects(index))
                Next

                dst2 = saveMat.Clone
                For Each pt In taskAlg.features
                    Dim index As Integer = taskAlg.gridMap.Get(Of Integer)(pt.Y, pt.X)
                    Dim roi = New cv.Rect(pt.X - half, pt.Y - half, taskAlg.brickSize, taskAlg.brickSize)
                    roi = ValidateRect(roi) ' stub bricks are fixed here 
                    dst2.Rectangle(roi, taskAlg.highlight, taskAlg.lineWidth)
                    dst2.Rectangle(taskAlg.gridNabeRects(index), taskAlg.highlight, taskAlg.lineWidth)
                Next
            End If

            dst3 = src.Clone
            Dim matchRects As New List(Of cv.Rect)
            For i = 0 To featureRects.Count - 1
                Dim roi = featureRects(i)
                match.template = saveMat(roi).Clone
                match.Run(src(searchRects(i)))
                dst3.Rectangle(match.newRect, taskAlg.highlight, taskAlg.lineWidth)
                matchRects.Add(match.newRect)
            Next

            saveMat = src.Clone
            searchRects.Clear()
            featureRects.Clear()
            For Each roi In matchRects
                half = roi.Width \ 2 ' stubby bricks are those at the bottom or right side of the image.
                pt = New cv.Point(roi.X + half, roi.Y + half)
                Dim index As Integer = taskAlg.gridMap.Get(Of Integer)(pt.Y, pt.X)
                featureRects.Add(roi)
                searchRects.Add(taskAlg.gridNabeRects(index))
            Next
        End Sub
    End Class





    Public Class XO_ML_BasicsOld : Inherits TaskParent
        Dim rtree As RTrees
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"", "depth32f - 32fc3 format with missing depth filled with predicted depth based on color (brighter is farther)", "", "Color used for roi prediction"}
            desc = "Predict depth from color to fill in the depth shadow areas"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim noDepthCount(taskAlg.gridRects.Count - 1) As Integer
            Dim roiColor(taskAlg.gridRects.Count - 1) As cv.Vec3b

            dst2.SetTo(0)
            Parallel.For(0, taskAlg.gridRects.Count,
        Sub(i)
            Dim roi = taskAlg.gridRects(i)
            roiColor(i) = src(roi).Get(Of cv.Vec3b)(roi.Height / 2, roi.Width / 2)
            dst2(roi).SetTo(roiColor(i), taskAlg.depthMask(roi))
            noDepthCount(i) = taskAlg.noDepthMask(roi).CountNonZero
        End Sub)

            If rtree Is Nothing Then rtree = cv.ML.RTrees.Create()
            Dim mlInput As New List(Of mlData)
            Dim mResponse As New List(Of Single)
            For i = 0 To taskAlg.gridRects.Count - 1
                If noDepthCount(i) = 0 Then Continue For
                Dim ml As mlData
                Dim roi = taskAlg.gridRects(i)
                ml.row = roi.Y + roi.Height / 2
                ml.col = roi.X + roi.Width / 2
                Dim c = roiColor(i)
                ml.blue = c(0)
                ml.green = c(1)
                ml.red = c(2)
                mlInput.Add(ml)
                mResponse.Add(taskAlg.pcSplit(2)(roi).Mean())
            Next

            If mlInput.Count = 0 Then
                strOut = "No learning data was found or provided.  Exit..."
                dst3.SetTo(0)
                SetTrueText(strOut, 3)
                Exit Sub
            End If

            Dim mLearn As cv.Mat = cv.Mat.FromPixelData(mlInput.Count, 5, cv.MatType.CV_32F, mlInput.ToArray)
            Dim response As cv.Mat = cv.Mat.FromPixelData(mResponse.Count, 1, cv.MatType.CV_32F, mResponse.ToArray)
            rtree.Train(mLearn, cv.ML.SampleTypes.RowSample, response)

            Dim predictList As New List(Of mlData)
            Dim colors As New List(Of cv.Vec3b)
            Dim saveRoi As New List(Of cv.Rect)
            Dim depthMask As New List(Of cv.Mat)
            For i = 0 To taskAlg.gridRects.Count - 1
                If noDepthCount(i) = 0 Then Continue For
                Dim roi = taskAlg.gridRects(i)
                depthMask.Add(taskAlg.noDepthMask(roi))
                Dim ml As mlData
                ml.row = roi.Y + roi.Height / 2
                ml.col = roi.X + roi.Width / 2
                Dim c = roiColor(i)
                ml.blue = c(0)
                ml.green = c(1)
                ml.red = c(2)
                predictList.Add(ml)
                colors.Add(c)
                saveRoi.Add(roi)
            Next

            Dim predMat = cv.Mat.FromPixelData(predictList.Count, 5, cv.MatType.CV_32F, predictList.ToArray)
            Dim output = New cv.Mat(predictList.Count, 1, cv.MatType.CV_32FC1, cv.Scalar.All(0))
            rtree.Predict(predMat, output)

            dst1 = taskAlg.pcSplit(2)
            dst3.SetTo(0)
            For i = 0 To predictList.Count - 1
                Dim roi = saveRoi(i)
                Dim depth = output.Get(Of Single)(i, 0)
                dst1(roi).SetTo(depth, depthMask(i))
                dst3(roi).SetTo(colors(i), depthMask(i))
            Next

            labels(2) = CStr(taskAlg.gridRects.Count) + " regions with " + CStr(mlInput.Count) + " used for learning and " + CStr(predictList.Count) + " were predicted"
        End Sub
        Public Sub Close()
            If rtree IsNot Nothing Then rtree.Dispose()
        End Sub
    End Class





    Public Class XO_Line3D_ReconstructLines : Inherits TaskParent
        Public findLine3D As New FindNonZero_Line3D
        Public lines3DList As New List(Of List(Of cv.Vec3f))
        Public pointcloud As New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        Public Sub New()
            desc = "Build the 3D lines found in Line_Basics if there has been motion at their endpoints"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static totalPixels As Integer
            taskAlg.FeatureSampleSize = 1000 ' use as many lines as are available.
            lines3DList.Clear()
            pointcloud.SetTo(0)
            totalPixels = 0
            For Each lp In taskAlg.lines.lpList
                findLine3D.lp = lp
                findLine3D.Run(src)

                Dim veclist = findLine3D.veclist
                If veclist.Count = 0 Then Continue For

                Dim depthInit = veclist(0)(2)
                Dim incr = (depthInit - veclist(veclist.Count - 1)(2)) / veclist.Count
                Dim newLine3D As New List(Of cv.Vec3f)
                For i = 0 To veclist.Count - 1
                    Dim pt = findLine3D.ptList(i)
                    'If taskAlg.toggleOn Then
                    '    pointcloud.Set(Of cv.Vec3f)(pt.Y, pt.X, taskAlg.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X))
                    'Else
                    Dim vec = Cloud_Basics.worldCoordinates(pt, depthInit + incr * i)
                    newLine3D.Add(vec)
                    pointcloud.Set(Of cv.Vec3f)(pt.Y, pt.X, vec)
                    'End If
                Next
                lines3DList.Add(newLine3D)
                totalPixels += newLine3D.Count
            Next

            dst2 = taskAlg.lines.dst2
            labels(2) = CStr(lines3DList.Count) + " lines were found and " + CStr(totalPixels) +
                    " pixels were updated in the point cloud."
        End Sub
    End Class







    Public Class XO_Line3D_ReconstructLinesNew : Inherits TaskParent
        Public findLine3D As New FindNonZero_Line3D
        Public lines3DList As New List(Of List(Of cv.Vec3f))
        Public Sub New()
            desc = "Build the 3D lines found in Line_Basics if there is 3D info at both end points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            taskAlg.FeatureSampleSize = 1000 ' use as many lines as are available.
            lines3DList.Clear()
            Dim totalPixels As Integer
            For Each lp In taskAlg.lines.lpList
                findLine3D.lp = lp
                findLine3D.Run(src)

                Dim veclist = findLine3D.veclist
                If veclist.Count = 0 Then Continue For

                Dim depthInit = veclist(0)(2)
                Dim incr = (depthInit - veclist(veclist.Count - 1)(2)) / veclist.Count
                Dim newLine3D As New List(Of cv.Vec3f)
                For i = 0 To veclist.Count - 1
                    Dim pt = findLine3D.ptList(i)
                    'If taskAlg.toggleOn Then
                    '    pointcloud.Set(Of cv.Vec3f)(pt.Y, pt.X, taskAlg.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X))
                    'Else
                    Dim vec = Cloud_Basics.worldCoordinates(pt, depthInit + incr * i)
                    newLine3D.Add(vec)
                    taskAlg.pointCloud.Set(Of cv.Vec3f)(pt.Y, pt.X, vec)
                    totalPixels += 1
                    'End If
                Next
                lines3DList.Add(newLine3D)
            Next

            dst2 = taskAlg.lines.dst2
            labels(2) = CStr(lines3DList.Count) + " lines were found and " + CStr(totalPixels) +
                    " pixels were updated in the point cloud."
        End Sub
    End Class






    Public Class XO_GL_Draw3DLinesAndCloud : Inherits TaskParent
        Dim line3D As New XO_Line3D_ReconstructLines
        Public Sub New()
            taskAlg.featureOptions.FeatureSampleSize.Value = taskAlg.featureOptions.FeatureSampleSize.Maximum
            desc = "Draw the RGB lines in SharpGL and include the line points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32FC3 Then src = taskAlg.pointCloud.Clone
            dst2 = taskAlg.lines.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
            labels(2) = taskAlg.lines.labels(2)

            dst0 = src
            dst0.SetTo(0, Not dst2)

            dst1.SetTo(red)
            strOut = taskAlg.sharpGL.RunSharp(Common.oCase.draw3DLinesAndCloud, dst0, taskAlg.lines.dst2)
            SetTrueText(strOut, 3)

            dst2 = taskAlg.lines.dst2
        End Sub
    End Class







    Public Class XO_Cloud_XRangeTest : Inherits TaskParent
        Dim split2 As New Cloud_ReduceSplit2
        Public Sub New()
            desc = "Test adjusting the X-Range value to squeeze a histogram into dst2."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            split2.Run(src)

            cv.Cv2.CalcHist({split2.dst3}, taskAlg.channelsTop, New cv.Mat, dst1, 2, taskAlg.bins2D, taskAlg.rangesTop)

            dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
            dst1 = dst1.Flip(cv.FlipMode.X)
            dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
        End Sub
    End Class




    Public Class XO_Cloud_YRangeTest : Inherits TaskParent
        Dim split2 As New Cloud_ReduceSplit2
        Public Sub New()
            desc = "Test adjusting the Y-Range value to squeeze a histogram into dst2."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            split2.Run(src)

            cv.Cv2.CalcHist({split2.dst3}, taskAlg.channelsSide, New cv.Mat, dst1, 2, taskAlg.bins2D, taskAlg.rangesSide)

            dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
            dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
        End Sub
    End Class




    Public Class XO_Cloud_Spin : Inherits TaskParent
        Dim options As New Options_IMU
        Dim gMat As New IMU_GMatrixWithOptions
        Dim xBump = 1, yBump = 1, zBump = 1
        Public Sub New()
            If OptionParent.FindFrm(traceName + " CheckBoxes") Is Nothing Then
                check.Setup(traceName)
                check.addCheckBox("Spin pointcloud on X-axis")
                check.addCheckBox("Spin pointcloud on Y-axis")
                check.addCheckBox("Spin pointcloud on Z-axis")
                check.Box(2).Checked = True
            End If

            taskAlg.gOptions.setGravityUsage(False)
            desc = "Spin the point cloud exercise"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static xCheck = OptionParent.FindCheckBox("Spin pointcloud on X-axis")
            Static yCheck = OptionParent.FindCheckBox("Spin pointcloud on Y-axis")
            Static zCheck = OptionParent.FindCheckBox("Spin pointcloud on Z-axis")
            Static xRotateSlider = OptionParent.FindSlider("Rotate pointcloud around X-axis (degrees)")
            Static yRotateSlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis (degrees)")
            Static zRotateSlider = OptionParent.FindSlider("Rotate pointcloud around Z-axis (degrees)")

            If xCheck.checked Then
                If xRotateSlider.value = -90 Then xBump = 1
                If xRotateSlider.value = 90 Then xBump = -1
                xRotateSlider.value += xBump
            End If

            If yCheck.checked Then
                If yRotateSlider.value = -90 Then yBump = 1
                If yRotateSlider.value = 90 Then yBump = -1
                yRotateSlider.value += yBump
            End If

            If zCheck.checked Then
                If zRotateSlider.value = -90 Then zBump = 1
                If zRotateSlider.value = 90 Then zBump = -1
                zRotateSlider.value += zBump
            End If

            gMat.Run(src)

            Dim gOutput = (taskAlg.pointCloud.Reshape(1, dst2.Rows * dst2.Cols) * gMat.gMatrix).ToMat  ' <<<<<<<<<<<<<<<<<<<<<<< this is the rotation...
            dst2 = gOutput.Reshape(3, src.Rows)
        End Sub
    End Class





    Public Class XO_Cloud_Spin2 : Inherits TaskParent
        Dim spin As New XO_Cloud_Spin
        Dim redCSpin As New XO_RedList_Basics
        Public Sub New()
            labels = {"", "", "RedCloud output", "Spinning RedCloud output - use options to spin on different axes."}
            desc = "Spin the RedCloud output exercise"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            spin.Run(src)
            taskAlg.pointCloud = spin.dst2
            redCSpin.Run(src)
            dst3 = redCSpin.dst2
        End Sub
    End Class





    Public Class XO_Line3D_DrawLines : Inherits TaskParent
        Public line3d As New XO_Line3D_DrawLine
        Public lpList As New List(Of lpData)
        Public Sub New()
            If standalone Then taskAlg.gOptions.LineWidth.Value = 3
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Recompute the depth for the lines found."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then lpList = New List(Of lpData)(taskAlg.lines.lpList)
            dst2 = taskAlg.pointCloud.Clone
            dst1.SetTo(0)
            For Each lp In lpList
                dst1.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth, cv.LineTypes.Link4)
            Next

            dst3 = src
            taskAlg.lines.dst2.CopyTo(dst3, dst1)
            For Each line3d.lp In lpList
                line3d.Run(emptyMat)
                Dim index As Integer = 0
                If line3d.lp IsNot Nothing Then
                    For i = 0 To line3d.points.Rows - 1
                        Dim pt = line3d.points.Get(Of cv.Point)(index, 0)
                        dst2.Set(Of cv.Vec3f)(pt.Y, pt.X, Cloud_Basics.worldCoordinates(pt.X, pt.Y, line3d.depth1 + index * line3d.incr))
                        index += 1
                    Next
                End If
            Next
            labels(2) = "At least one end of a line should fade into the surrounding (except where depth data is limited)"
            labels(3) = taskAlg.lines.labels(2)
        End Sub
    End Class





    Public Class XO_Line3D_DrawLine : Inherits TaskParent
        Public lp As lpData
        Public depth1 As Single
        Public incr As Single
        Public points As cv.Mat
        Public Sub New()
            If standalone Then taskAlg.gOptions.LineWidth.Value = 3
            dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Create a 3D line where there is a detected line in 2D."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3.SetTo(0)
            If standalone Then lp = taskAlg.lineLongest
            If lp.pVec1(2) = 0 Or lp.pVec2(2) = 0 Then
                lp = Nothing ' no result...
                Exit Sub
            End If

            dst3.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth, cv.LineTypes.Link4)

            points = dst3.FindNonZero()
            Dim count As Integer = points.Rows

            Dim pt = points.Get(Of cv.Point)(0, 0), depth2 As Single
            If lp.p1.DistanceTo(pt) <= taskAlg.lineWidth Then
                depth1 = lp.pVec1(2)
                depth2 = lp.pVec2(2)
            Else
                depth1 = lp.pVec2(2)
                depth2 = lp.pVec1(2)
            End If
            incr = (depth1 - depth2) / count

            If standalone Then
                dst2 = taskAlg.pointCloud.Clone
                For i = 0 To points.Rows - 1
                    pt = points.Get(Of cv.Point)(i, 0)
                    dst2.Set(Of cv.Vec3f)(pt.Y, pt.X, Cloud_Basics.worldCoordinates(pt.X, pt.Y, depth1 + i * incr))
                Next
                labels(2) = "Point cloud with " + CStr(count) + " pixels updated with linear results."
            End If
        End Sub
    End Class




    Public Class XO_Line3D_DrawLineAlt : Inherits TaskParent
        Public lp As lpData
        Public depthAvg As Single
        Public incr As Single
        Public points As cv.Mat
        Public Sub New()
            If standalone Then taskAlg.gOptions.LineWidth.Value = 3
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Create a 3D line where there is a detected line in 2D."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then lp = taskAlg.lineLongest
            If lp.pVec1(2) = 0 Or lp.pVec2(2) = 0 Then
                lp = Nothing ' no result...
                Exit Sub
            End If

            dst3.SetTo(0)
            dst3.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth, cv.LineTypes.Link4)

            dst1.SetTo(0)
            taskAlg.pcSplit(2)(lp.rect).CopyTo(dst1(lp.rect), dst3(lp.rect))
            depthAvg = dst1(lp.rect).Mean(dst3(lp.rect)).Item(0)
            points = dst3.FindNonZero()

            Dim ptArray(points.Total - 1) As Integer
            Marshal.Copy(points.Data, ptArray, 0, ptArray.Length)

            Dim ptList As New List(Of cv.Point)
            Dim indexMid As Integer = -1
            For i = 0 To ptArray.Count - 2 Step 2
                Dim pt = New cv.Point(ptArray(i), ptArray(i + 1))
                If i >= ptArray.Count / 4 And indexMid < 0 Then indexMid = ptList.Count
                ptList.Add(pt)
            Next

            Dim p1 As cv.Point, p2 As cv.Point
            Dim d1 As Single, d2 As Single
            Dim incrList As New List(Of Single)
            For i = 1 To ptList.Count - 1
                p1 = ptList(i - 1)
                p2 = ptList(i)
                d1 = taskAlg.pcSplit(2).Get(Of Single)(p1.Y, p1.X)
                d2 = taskAlg.pcSplit(2).Get(Of Single)(p2.Y, p2.X)
                Dim delta = d2 - d1
                If Math.Abs(delta) < 0.1 Then incrList.Add(delta) ' if delta is less than 10 centimeters, then keep it.
            Next
            incr = incrList.Average()

            dst2 = taskAlg.pointCloud.Clone
            Dim dirSign As Integer = If(lp.p1.DistanceTo(ptList(0)) < lp.p2.DistanceTo(ptList(0)), -1, 1)
            For i = indexMid To 0 Step -1
                Dim pt = ptList(i)
                dst2.Set(Of cv.Vec3f)(pt.Y, pt.X, Cloud_Basics.worldCoordinates(pt.X, pt.Y, depthAvg + -dirSign * (i - indexMid) * incr))
            Next
            For i = indexMid To ptList.Count - 1
                Dim pt = ptList(i)
                dst2.Set(Of cv.Vec3f)(pt.Y, pt.X, Cloud_Basics.worldCoordinates(pt.X, pt.Y, depthAvg + dirSign * (i - indexMid) * incr))
            Next
        End Sub
    End Class




    Public Class XO_Reliable_Basics : Inherits TaskParent
        Dim bgs As New BGSubtract_Basics
        Dim relyDepth As New XO_Reliable_Depth
        Public Sub New()
            desc = "Identify each grid element with unreliable data or motion."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bgs.Run(src)
            dst2 = bgs.dst2

            relyDepth.Run(src)
            dst3 = relyDepth.dst2
        End Sub
    End Class







    Public Class XO_Reliable_Depth : Inherits TaskParent
        Dim rDepth As New History_ReliableDepth
        Public Sub New()
            labels = {"", "", "Mask of Reliable depth data", "taskAlg.DepthRGB after removing unreliable depth (compare with above.)"}
            desc = "Provide only depth that has been present over the last framehistory frames."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            rDepth.Run(taskAlg.noDepthMask)
            dst2 = rDepth.dst2

            If standaloneTest() Then
                dst3.SetTo(0)
                taskAlg.depthRGB.CopyTo(dst3, dst2)
            End If
        End Sub
    End Class






    Public Class XO_Reliable_MaxDepth : Inherits TaskParent
        Public options As New Options_MinMaxNone
        Public Sub New()
            desc = "Create a mas"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            Dim split() As cv.Mat
            If src.Type = cv.MatType.CV_32FC3 Then split = src.Split() Else split = taskAlg.pcSplit

            If taskAlg.heartBeat Then
                dst3 = split(2)
            End If
            If options.useMax Then
                labels(2) = "Point cloud maximum values at each pixel"
                cv.Cv2.Max(split(2), dst3, split(2))
            End If
            If options.useMin Then
                labels(2) = "Point cloud minimum values at each pixel"
                Dim saveMat = split(2).Clone
                cv.Cv2.Min(split(2), dst3, split(2))
                Dim mask = split(2).InRange(0, 0.1)
                saveMat.CopyTo(split(2), mask)
            End If
            cv.Cv2.Merge(split, dst2)
            dst3 = split(2)
        End Sub
    End Class





    Public Class XO_Reliable_RGB : Inherits TaskParent
        Dim diff(2) As XO_Motion_Diff
        Dim history(2) As History_Basics8U
        Public Sub New()
            For i = 0 To diff.Count - 1
                diff(i) = New XO_Motion_Diff
                history(i) = New History_Basics8U
            Next
            taskAlg.featureOptions.ColorDiffSlider.Value = 10
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            labels = {"", "", "Mask of unreliable color data", "Color image after removing unreliable pixels"}
            desc = "Accumulate those color pixels that are volatile - different by more than the global options 'Color Difference threshold'"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = src.Clone
            dst2.SetTo(0)
            For i = 0 To diff.Count - 1
                diff(i).Run(src)
                history(i).Run(diff(i).dst2)
                dst2 = dst2 Or history(i).dst2
            Next
            dst3.SetTo(0, dst2)
        End Sub
    End Class





    Public Class XO_RedCloud_Contours : Inherits TaskParent
        Dim prep As New RedPrep_Depth
        Public Sub New()
            If taskAlg.contours Is Nothing Then taskAlg.contours = New Contour_Basics_List
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Run the reduced pointcloud output through the RedList_CPP algorithm."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            taskAlg.contours.Run(src)
            prep.Run(src)
            dst3 = prep.dst3

            dst2 = taskAlg.contours.dst2
            labels(2) = taskAlg.contours.labels(2)
        End Sub
    End Class








    Public Class XO_RedCloud_Mats : Inherits TaskParent
        Dim mats As New Mat_4Click
        Public Sub New()
            desc = "Simple transforms for the point cloud using CalcHist instead of reduction."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim histogram As New cv.Mat

            For i = 0 To 2
                Select Case i
                    Case 0 ' X Reduction
                        dst0 = taskAlg.pcSplit(0)
                    Case 1 ' Y Reduction
                        dst0 = taskAlg.pcSplit(1)
                    Case 2 ' Z Reduction
                        dst0 = taskAlg.pcSplit(2)
                End Select

                Dim mm = GetMinMax(dst0)
                Dim ranges = New cv.Rangef() {New cv.Rangef(mm.minVal, mm.maxVal)}
                cv.Cv2.CalcHist({dst0}, {0}, taskAlg.depthMask, histogram, 1, {taskAlg.histogramBins}, ranges)

                Dim histArray(histogram.Total - 1) As Single
                Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

                For j = 0 To histArray.Count - 1
                    histArray(j) = j
                Next

                histogram = cv.Mat.FromPixelData(histogram.Rows, 1, cv.MatType.CV_32F, histArray)
                cv.Cv2.CalcBackProject({dst0}, {0}, histogram, dst0, ranges)
                dst0.ConvertTo(dst1, cv.MatType.CV_8U)
                mats.mat(i) = PaletteFull(dst1)
                mats.mat(i).SetTo(0, taskAlg.noDepthMask)
            Next

            mats.Run(emptyMat)
            dst2 = mats.dst2
        End Sub
    End Class








    Public Class XO_RedCloud_PrepOutline : Inherits TaskParent
        Public prep As New RedPrep_Depth
        Public Sub New()
            desc = "Remove corners of RedCloud cells in the prep data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst2 = prep.dst2.Clone

            Dim val1 As Byte, val2 As Byte
            For y = 0 To dst2.Height - 2
                For x = 0 To dst2.Width - 2
                    Dim zipData As Boolean = False

                    val1 = dst2.Get(Of Byte)(y, x)
                    val2 = dst2.Get(Of Byte)(y, x + 1)
                    If val1 <> 0 And val2 <> 0 Then If val1 <> val2 Then zipData = True

                    val2 = dst2.Get(Of Byte)(y + 1, x)
                    If val1 <> 0 And val2 <> 0 Then If val1 <> val2 Then zipData = True

                    If zipData Then
                        dst2.Set(Of Byte)(y, x, 0)
                        dst2.Set(Of Byte)(y, x + 1, 0)
                        dst2.Set(Of Byte)(y + 1, x, 0)
                        dst2.Set(Of Byte)(y + 1, x + 1, 0)
                    End If
                Next
            Next

            dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        End Sub
    End Class





    Public Class XO_Contour_RedCloud1 : Inherits TaskParent
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Show all the contours found in the RedCloud output"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            dst3.SetTo(0)
            For Each rc In taskAlg.redList.oldrclist
                DrawTour(dst3(rc.rect), rc.contour, 255, taskAlg.lineWidth)
            Next
        End Sub
    End Class





    Public Class XO_Contour_RedCloud : Inherits TaskParent
        Dim prep As New XO_RedCloud_PrepOutline
        Public options As New Options_Contours
        Public contourList As New List(Of contourData)
        Public contourMap As New cv.Mat(taskAlg.workRes, cv.MatType.CV_32F, 0)
        Dim sortContours As New Contour_Sort
        Public Sub New()
            desc = "Use the RedPrep_Basics as input to contours_basics."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            prep.Run(src)
            prep.dst2.ConvertTo(dst1, cv.MatType.CV_8U)
            dst3 = prep.prep.dst3
            labels(3) = prep.labels(2)

            Dim mode = options.options2.ApproximationMode
            cv.Cv2.FindContours(dst1, sortContours.allContours, Nothing, cv.RetrievalModes.List, mode)
            If sortContours.allContours.Count <= 1 Then Exit Sub

            sortContours.Run(src)

            contourList = sortContours.contourList
            contourMap = sortContours.contourMap
            If taskAlg.heartBeat Then labels(2) = sortContours.labels(2)
            dst2 = sortContours.dst2
        End Sub
    End Class







    Public Class XO_Contour_RedCloudCompare : Inherits TaskParent
        Dim prep As New XO_RedCloud_PrepOutline
        Public options As New Options_Contours
        Public contourList As New List(Of contourData)
        Public contourMap As New cv.Mat(taskAlg.workRes, cv.MatType.CV_32F, 0)
        Public contourIDs As New List(Of Integer)
        Public Sub New()
            If taskAlg.contours Is Nothing Then taskAlg.contours = New Contour_Basics_List
            desc = "Use the RedPrep_Basics as input to contours_basics."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            taskAlg.contours.Run(src)
            dst2 = taskAlg.contours.dst2
            labels(2) = taskAlg.contours.labels(2)

            prep.Run(src)
            prep.dst2.ConvertTo(dst1, cv.MatType.CV_8U)
            dst3 = prep.prep.dst3
            labels(3) = prep.labels(2)
        End Sub
    End Class







    Public Class XO_RedPrep_BasicsShow : Inherits TaskParent
        Public prep As New XO_RedCloud_PrepOutline
        Public Sub New()
            desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst2 = prep.dst2

            ' dst2.SetTo(0, taskAlg.noDepthMask)
            dst2.ConvertTo(dst2, cv.MatType.CV_8U)
            Dim mm = GetMinMax(dst2)
            dst3 = PaletteFull(dst2)
            XO_RedList_Basics.setSelectedCell()

            labels(2) = CStr(mm.maxVal + 1) + " regions were mapped in the depth data - region 0 (black) has no depth."
        End Sub
    End Class







    Public Class XO_RedCloud_World : Inherits TaskParent
        Dim world As New Depth_World
        Dim prep As New RedPrep_Basics
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels(3) = "Generated pointcloud"
            desc = "Display the output of a generated pointcloud as RedCloud cells"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            world.Run(src)

            prep.Run(world.dst2)

            dst2 = runRedList(prep.dst2, labels(2))
        End Sub
    End Class





    Public Class XO_RedCloud_BasicsXY : Inherits TaskParent
        Dim prep As New RedPrep_Depth
        Dim redMask As New RedMask_Basics
        Dim cellGen As New XO_RedCell_Color
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Run the reduced pointcloud output through the RedList_CPP algorithm."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            redMask.Run(prep.dst2)

            If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
            cellGen.mdList = redMask.mdList
            cellGen.Run(redMask.dst2)

            dst2 = cellGen.dst2

            labels(2) = cellGen.labels(2)
        End Sub
    End Class




    Public Class XO_RedCloud_BasicsTest : Inherits TaskParent
        Dim redCold As New XO_RedCloud_HeartBeat
        Dim prep As New RedPrep_Basics
        Public rcList As New List(Of rcData)
        Public Sub New()
            desc = "Floodfill each region of the RedPrep_Basics output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.heartBeat Then
                redCold.Run(src)
                dst2 = redCold.dst2
                dst3 = redCold.dst3
                Exit Sub
            End If

            prep.Run(src)
            dst3 = prep.dst2

            Dim flags = cv.FloodFillFlags.FixedRange Or (255 << 8) Or cv.FloodFillFlags.MaskOnly
            Dim rect As New cv.Rect
            Dim index As Integer = 1
            Dim minCount = dst3.Total * 0.001, maxCount = dst3.Total * 3 / 4
            Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
            rcList.Clear()
            Dim maskRect = New cv.Rect(1, 1, dst3.Width, dst3.Height)
            For Each pc In redCold.rcList
                Dim count = cv.Cv2.FloodFill(dst3, mask, pc.maxDist, index, rect, 0, 0, flags)
                If count >= minCount And count < maxCount Then
                    Dim pd = New rcData(dst3(rect), rect, index)
                    dst2(rect).SetTo(taskAlg.scalarColors(index), mask(rect))
                    rcList.Add(pd)
                    index += 1
                End If
            Next

            For Each pd In rcList
                dst2.Circle(pd.maxDist, taskAlg.DotSize, taskAlg.highlight, -1)
            Next

            labels(2) = CStr(index) + " regions were identified"
        End Sub
    End Class






    Public Class XO_RedCloud_Basics_CPP : Inherits TaskParent
        Dim prep As New RedPrep_Basics
        Dim stats As New XO_RedCell_Color
        Public Sub New()
            OptionParent.findRadio("XY Reduction").Checked = True
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Run the reduced pointcloud output through the RedList_CPP algorithm."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(prep.dst2, labels(2))
            If standaloneTest() Then
                stats.Run(src)
                dst1 = stats.dst3
                SetTrueText(stats.strOut, 3)
            End If
        End Sub
    End Class




    Public Class XO_RedCloud_Basics : Inherits TaskParent
        Dim prepEdges As New RedPrep_EdgeMask
        Public rcList As New List(Of rcData)
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "Map of reduced point cloud - CV_8U"
            desc = "Find the biggest chunks of consistent depth data "
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepEdges.Run(src)
            dst3 = prepEdges.dst2

            Dim index As Integer = 1
            Dim rect As New cv.Rect
            Dim maskRect = New cv.Rect(1, 1, dst3.Width, dst3.Height)
            Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
            Dim flags = cv.FloodFillFlags.FixedRange Or (255 << 8) Or cv.FloodFillFlags.MaskOnly
            Dim maskUsed As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            Dim minCount = dst3.Total * 0.001, maxCount = dst3.Total * 3 / 4
            Dim newList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalInteger)
            For y = 1 To dst3.Height - 2
                For x = 0 To dst3.Width - 2
                    Dim pt = New cv.Point(x, y)
                    ' skip the regions with no depth 
                    If dst3.Get(Of Byte)(pt.Y, pt.X) > 0 Then
                        ' skip flooding near good chunks of depth data.
                        If maskUsed.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                            Dim count = cv.Cv2.FloodFill(dst3, mask, pt, index, rect, 0, 0, flags)
                            Dim r = New cv.Rect(rect.X + 1, rect.Y + 1, rect.Width - 1, rect.Height - 1)
                            maskUsed.Rectangle(r, 255, -1)
                            If count >= minCount And count < maxCount Then
                                Dim pc = New rcData(mask(r), r, index)
                                index += 1
                                newList.Add(pc.maxDist.Y, pc)
                            End If
                        End If
                    End If
                Next
            Next

            rcList.Clear()
            dst1.SetTo(0)
            For Each pc In newList.Values
                pc.index = rcList.Count + 1
                rcList.Add(pc)
                dst1(pc.rect).SetTo(pc.index Mod 255, pc.mask)
                SetTrueText(CStr(pc.index), New cv.Point(pc.rect.X, pc.rect.Y))
            Next
            dst2 = PaletteBlackZero(dst1)

            Dim clickIndex = dst1.Get(Of Byte)(taskAlg.ClickPoint.Y, taskAlg.ClickPoint.X)
            If clickIndex > 0 And clickIndex < rcList.Count Then
                taskAlg.color(rcList(clickIndex - 1).rect).SetTo(white, rcList(clickIndex - 1).mask)
                taskAlg.color.Rectangle(rcList(clickIndex - 1).rect, white, taskAlg.lineWidth, taskAlg.lineType)
            End If
            labels(2) = CStr(newList.Count) + " regions were identified. Region " + CStr(clickIndex) + " was selected."
        End Sub
    End Class





    Public Class XO_Flood_CloudData : Inherits TaskParent
        Public Sub New()
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Floodfill each region of the RedPrep_Basics output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                Static prep As New RedPrep_Basics
                prep.Run(src)
                src = Not prep.dst2
            End If

            Dim index As Integer = 1
            Dim rect As New cv.Rect
            Dim maskRect = New cv.Rect(1, 1, src.Width, src.Height)
            Dim mask = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8U, 0)
            Dim flags = cv.FloodFillFlags.FixedRange Or (255 << 8) Or cv.FloodFillFlags.MaskOnly
            dst0.SetTo(0)
            dst1.SetTo(0)
            Dim minCount = src.Total * 0.001
            For y = 0 To src.Height - 1
                For x = 0 To src.Width - 1
                    Dim pt = New cv.Point(x, y)
                    Dim val = src.Get(Of Byte)(pt.Y, pt.X) ' skip the regions with no depth
                    If val > 0 Then
                        val = dst1.Get(Of Byte)(pt.Y, pt.X)
                        If val = 0 Then
                            Dim count = cv.Cv2.FloodFill(src, mask, pt, index, rect, 0, 0, flags)
                            If count >= minCount Then
                                dst1.Rectangle(rect, 255, -1)
                                dst0(rect).SetTo(index, mask(rect))
                                index += 1
                            End If
                        End If
                    End If
                Next
            Next

            dst3 = PaletteBlackZero(dst0)
            labels(2) = CStr(index) + " regions were identified"
        End Sub
    End Class





    Public Class XO_RedCloud_XY : Inherits TaskParent
        Dim prep As New RedPrep_ReductionChoices
        Public Sub New()
            OptionParent.findRadio("XY Reduction").Checked = True
            labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
            desc = "Build XY RedCloud cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(prep.dst2, labels(2))
            If standaloneTest() Then
                Static stats As New XO_RedCell_Basics
                stats.Run(src)
                SetTrueText(stats.strOut, 3)
            End If
        End Sub
    End Class






    Public Class XO_RedCloud_YZ : Inherits TaskParent
        Dim prep As New RedPrep_ReductionChoices
        Dim stats As New XO_RedCell_Basics
        Public Sub New()
            OptionParent.findRadio("YZ Reduction").Checked = True
            labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Build YZ RedCloud cells"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)

            dst2 = runRedList(prep.dst2, labels(2))

            stats.Run(src)
            dst1 = stats.dst3
            SetTrueText(stats.strOut, 3)
        End Sub
    End Class






    Public Class XO_RedCloud_XZ : Inherits TaskParent
        Dim prep As New RedPrep_ReductionChoices
        Dim stats As New XO_RedCell_Basics
        Public Sub New()
            OptionParent.findRadio("XZ Reduction").Checked = True
            labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Build XZ RedCloud cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)

            dst2 = runRedList(prep.dst2, labels(2))
            stats.Run(src)
            dst1 = stats.dst3
            SetTrueText(stats.strOut, 3)
        End Sub
    End Class






    Public Class XO_RedCloud_X : Inherits TaskParent
        Dim prep As New RedPrep_ReductionChoices
        Dim stats As New XO_RedCell_Basics
        Public Sub New()
            OptionParent.findRadio("X Reduction").Checked = True
            labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Build X RedCloud cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)

            dst2 = runRedList(prep.dst2, labels(2))
            stats.Run(src)
            dst1 = stats.dst3
            SetTrueText(stats.strOut, 3)
        End Sub
    End Class






    Public Class XO_RedCloud_Y : Inherits TaskParent
        Dim prep As New RedPrep_ReductionChoices
        Dim stats As New XO_RedCell_Basics
        Public Sub New()
            OptionParent.findRadio("Y Reduction").Checked = True
            labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Build Y RedCloud cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)

            dst2 = runRedList(prep.dst2, labels(2))
            stats.Run(src)
            dst1 = stats.dst3
            SetTrueText(stats.strOut, 3)
        End Sub
    End Class





    Public Class XO_RedCloud_Z : Inherits TaskParent
        Dim prep As New RedPrep_ReductionChoices
        Dim stats As New XO_RedCell_Basics
        Public Sub New()
            OptionParent.findRadio("Z Reduction").Checked = True
            labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Build Z RedCloud cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)

            dst2 = runRedList(prep.dst2, labels(2))
            stats.Run(src)
            dst1 = stats.dst3
            SetTrueText(stats.strOut, 3)
        End Sub
    End Class









    Public Class XO_RedCell_ValidateColor : Inherits TaskParent
        Public Sub New()
            labels(3) = "Cells shown below have rc.depthPixels / rc.pixels < 50%"
            dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Validate that all the depthCells are correctly identified."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            dst1.SetTo(0)
            dst3.SetTo(0)
            Dim percentDepth As New List(Of Single)
            For Each rc In taskAlg.redList.oldrclist
                If rc.depthPixels > 0 Then dst1(rc.rect).SetTo(255, rc.mask)
                If rc.depthPixels > 0 And rc.index > 0 Then
                    Dim pc = rc.depthPixels / rc.pixels
                    percentDepth.Add(pc)

                    If pc < 0.5 Then dst3(rc.rect).SetTo(rc.color, rc.mask)
                End If
            Next

            Dim beforeCount = dst1.CountNonZero
            dst1.SetTo(0, taskAlg.depthMask)
            Dim aftercount = dst1.CountNonZero

            If beforeCount <> aftercount Then
                strOut = "There are color cells with limited depth in them" + vbCrLf
            Else
                strOut = "There are no color cells with depth in them." + vbCrLf
            End If
            If percentDepth.Count > 0 Then
                strOut += "Percentage average " + Format(percentDepth.Average, "0%") + vbCrLf
                strOut += "Percentage range " + Format(percentDepth.Min, "0%") + " to " + Format(percentDepth.Max, "0%")
            End If
            SetTrueText(strOut, 3)
        End Sub
    End Class




    Public Class XO_RedCell_Basics : Inherits TaskParent
        Dim plot As New Hist_Depth
        Public runRedCflag As Boolean
        Public Sub New()
            If standalone Then taskAlg.gOptions.setHistogramBins(20)
            desc = "Display the statistics for the selected cell."
        End Sub
        Public Sub statsString()
            Dim rc = taskAlg.oldrcD

            Dim gridID As Integer = taskAlg.gridMap.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
            strOut = "rc.index = " + CStr(rc.index) + vbTab + " gridID = " + CStr(gridID) + vbTab
            strOut += "rc.age = " + CStr(rc.age) + vbCrLf
            strOut += "rc.rect: " + CStr(rc.rect.X) + ", " + CStr(rc.rect.Y) + ", "
            strOut += CStr(rc.rect.Width) + ", " + CStr(rc.rect.Height) + vbCrLf
            strOut += "rc.color = " + vbTab + CStr(CInt(rc.color(0))) + vbTab + CStr(CInt(rc.color(1)))
            strOut += vbTab + CStr(CInt(rc.color(2))) + vbCrLf
            strOut += "rc.maxDist = " + CStr(rc.maxDist.X) + "," + CStr(rc.maxDist.Y) + vbCrLf

            strOut += If(rc.depthPixels > 0, "Cell is marked as having depth" + vbCrLf, "")
            strOut += "Pixels " + Format(rc.pixels, "###,###") + vbCrLf + "depth pixels "
            If rc.depthPixels > 0 Then
                strOut += Format(rc.depthPixels, "###,###") + " or " +
                          Format(rc.depthPixels / rc.pixels, "0%") + " depth " + vbCrLf
            Else
                strOut += Format(rc.pixels, "###,###") + " - no depth data" + vbCrLf
            End If

            strOut += "Cloud Min/Max/Range: X = " + Format(rc.mmX.minVal, fmt1) + "/" + Format(rc.mmX.maxVal, fmt1)
            strOut += "/" + Format(rc.mmX.range, fmt1) + vbTab
            strOut += "Y = " + Format(rc.mmY.minVal, fmt1) + "/" + Format(rc.mmY.maxVal, fmt1)
            strOut += "/" + Format(rc.mmY.range, fmt1) + vbTab
            strOut += "Z = " + Format(rc.mmZ.minVal, fmt2) + "/" + Format(rc.mmZ.maxVal, fmt2)
            strOut += "/" + Format(rc.mmZ.range, fmt2) + vbCrLf + vbCrLf

            strOut += "Cell Depth in 3D: z = " + vbTab + Format(rc.depth, fmt2) + vbCrLf

            Dim tmp = New cv.Mat(taskAlg.oldrcD.mask.Rows, taskAlg.oldrcD.mask.Cols, cv.MatType.CV_32F, cv.Scalar.All(0))
            taskAlg.pcSplit(2)(taskAlg.oldrcD.rect).CopyTo(tmp, taskAlg.oldrcD.mask)
            plot.rc = taskAlg.oldrcD
            plot.Run(tmp)
            dst3 = plot.dst2
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Or runRedCflag Then dst2 = runRedList(src, labels(2))
            statsString()
            SetTrueText(strOut, 3)
            labels(3) = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + CStr(CInt(taskAlg.MaxZmeters)) + " meters"
        End Sub
    End Class
    Public Class XO_RedCell_Distance : Inherits TaskParent
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            labels = {"", "Depth distance to selected cell", "", "Color distance to selected cell"}
            desc = "Measure the color distance of each cell to the selected cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.heartBeat Or taskAlg.quarterBeat Then
                dst2 = runRedList(src, labels(2))
                dst0 = taskAlg.color

                Dim depthDistance As New List(Of Single)
                Dim colorDistance As New List(Of Single)
                Dim selectedMean As cv.Scalar = src(taskAlg.oldrcD.rect).Mean(taskAlg.oldrcD.mask)
                If taskAlg.redList.oldrclist.Count = 0 Then Exit Sub ' next frame please...
                For Each rc In taskAlg.redList.oldrclist
                    colorDistance.Add(Distance_Basics.distance3D(selectedMean, src(rc.rect).Mean(rc.mask)))
                    depthDistance.Add(Distance_Basics.distance3D(taskAlg.oldrcD.depth, rc.depth))
                Next

                dst1.SetTo(0)
                dst3.SetTo(0)
                Dim maxColorDistance = colorDistance.Max()
                For i = 0 To taskAlg.redList.oldrclist.Count - 1
                    Dim rc = taskAlg.redList.oldrclist(i)
                    dst1(rc.rect).SetTo(255 - depthDistance(i) * 255 / taskAlg.MaxZmeters, rc.mask)
                    dst3(rc.rect).SetTo(255 - colorDistance(i) * 255 / maxColorDistance, rc.mask)
                Next
            End If
        End Sub
    End Class








    Public Class XO_RedCell_Binarize : Inherits TaskParent
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            dst1 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            labels = {"", "Binarized image", "", "Relative gray image"}
            desc = "Separate the image into light and dark using RedCloud cells"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst0 = src
            If taskAlg.heartBeat Or taskAlg.quarterBeat Then
                dst2 = runRedList(src, labels(2))

                Dim grayMeans As New List(Of Single)
                Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                If taskAlg.redList.oldrclist.Count = 0 Then Exit Sub ' next frame please...
                For Each rc In taskAlg.redList.oldrclist
                    Dim grayMean As cv.Scalar, grayStdev As cv.Scalar
                    cv.Cv2.MeanStdDev(gray(rc.rect), grayMean, grayStdev, rc.mask)
                    grayMeans.Add(grayMean(0))
                Next
                Dim min = grayMeans.Min
                Dim max = grayMeans.Max
                Dim avg = grayMeans.Average

                dst3.SetTo(0)
                For Each rc In taskAlg.redList.oldrclist
                    Dim color = (grayMeans(rc.index) - min) * 255 / (max - min)
                    dst3(rc.rect).SetTo(color, rc.mask)
                    dst1(rc.rect).SetTo(If(grayMeans(rc.index) > avg, 255, 0), rc.mask)
                Next
            End If
        End Sub
    End Class






    Public Class XO_RedCell_FloodFill : Inherits TaskParent
        Dim flood As New Flood_Basics
        Dim stats As New XO_RedCell_Basics
        Public Sub New()
            desc = "Provide cell stats on the flood_basics cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            flood.Run(src)

            stats.Run(src)
            dst0 = stats.dst0
            dst1 = stats.dst1
            dst2 = flood.dst2
            labels = flood.labels
            SetTrueText(stats.strOut, 3)
        End Sub
    End Class







    Public Class XO_RedCell_BasicsPlot : Inherits TaskParent
        Dim plot As New Hist_Depth
        Public runRedCflag As Boolean
        Dim stats As New XO_RedCell_Basics
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            If standalone Then taskAlg.gOptions.setHistogramBins(20)
            desc = "Display the statistics for the selected cell."
        End Sub
        Public Sub statsString(src As cv.Mat)
            Dim tmp = New cv.Mat(taskAlg.oldrcD.mask.Rows, taskAlg.oldrcD.mask.Cols, cv.MatType.CV_32F, cv.Scalar.All(0))
            taskAlg.pcSplit(2)(taskAlg.oldrcD.rect).CopyTo(tmp, taskAlg.oldrcD.mask)
            plot.rc = taskAlg.oldrcD
            plot.Run(tmp)
            dst3 = plot.dst2

            stats.statsString()
            strOut = stats.strOut
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Or runRedCflag Then
                dst2 = runRedList(src, labels(2))
                If taskAlg.ClickPoint = newPoint Then
                    If taskAlg.redList.oldrclist.Count > 1 Then
                        taskAlg.oldrcD = taskAlg.redList.oldrclist(1)
                        taskAlg.ClickPoint = taskAlg.oldrcD.maxDist
                    End If
                End If
            End If
            If taskAlg.heartBeat Then statsString(src)

            SetTrueText(strOut, 1)
            labels(1) = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + CStr(CInt(taskAlg.MaxZmeters)) + " meters"
        End Sub
    End Class










    Public Class XO_RedCell_ValidateColorCloud : Inherits TaskParent
        Public Sub New()
            labels(3) = "Cells shown below have rc.depthPixels / rc.pixels < 50%"
            dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Validate that all the RedCloud cells are correctly identified."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(src, labels(2))

            dst1.SetTo(0)
            dst3.SetTo(0)
            Dim percentDepth As New List(Of Single)
            For Each pc In taskAlg.redCloud.rcList
                If pc.pixels > 0 Then dst1(pc.rect).SetTo(255, pc.mask)
                If pc.pixels > 0 Then
                    Dim tmp As cv.Mat = taskAlg.depthMask(pc.rect) And pc.mask

                    Dim percent = tmp.CountNonZero / pc.pixels
                    percentDepth.Add(percent)

                    If percent < 0.5 Then dst3(pc.rect).SetTo(pc.color, pc.mask)
                End If
            Next

            Dim beforeCount = dst1.CountNonZero
            dst1.SetTo(0, taskAlg.depthMask)
            Dim aftercount = dst1.CountNonZero

            If beforeCount <> aftercount Then
                strOut = "There are color cells with limited depth in them" + vbCrLf
            Else
                strOut = "There are no color cells with depth in them." + vbCrLf
            End If
            If percentDepth.Count > 0 Then
                strOut += "Percentage average " + Format(percentDepth.Average, "0%") + vbCrLf
                strOut += "Percentage range " + Format(percentDepth.Min, "0%") + " to " + Format(percentDepth.Max, "0%")
            End If
            SetTrueText(strOut, 3)
        End Sub
    End Class







    Public Class XO_RedCloud_Hulls : Inherits TaskParent
        Public Sub New()
            desc = "Create a hull for each RedCloud cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(src, labels(2))

            dst3.SetTo(0)
            Dim hullCounts As New List(Of Integer)
            For Each pc In taskAlg.redCloud.rcList
                pc.hull = cv.Cv2.ConvexHull(pc.hull.ToArray, True).ToList
                DrawTour(dst3(pc.rect), pc.hull, pc.color, -1)
                hullCounts.Add(pc.hull.Count)
                SetTrueText(CStr(pc.age), pc.maxDist)
            Next
            labels(3) = "Average hull length = " + Format(hullCounts.Average, fmt1) + " points.  "
        End Sub
    End Class








    Public Class XO_RedList_BasicsNoMask : Inherits TaskParent
        Public classCount As Integer
        Public rectList As New List(Of cv.Rect)
        Public identifyCount As Integer = 255
        Public Sub New()
            cPtr = RedCloud_Open()
            desc = "Run the C++ RedCloud Interface With Or without a mask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = Mat_Basics.srcMustBe8U(src)

            Dim inputData(dst1.Total - 1) As Byte
            Marshal.Copy(dst1.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            Dim imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
            handleInput.Free()
            dst2 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, imagePtr).Clone

            classCount = Math.Min(RedCloud_Count(cPtr), identifyCount * 2)
            If classCount = 0 Then Exit Sub ' no data to process.

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))

            Dim rects(classCount * 4) As Integer
            Marshal.Copy(rectData.Data, rects, 0, rects.Length)

            rectList.Clear()
            For i = 0 To classCount * 4 - 4 Step 4
                rectList.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
            Next

            If standalone Then dst3 = PaletteFull(dst2)

            If taskAlg.heartBeat Then labels(2) = "CV_8U result With " + CStr(classCount) + " regions."
            If taskAlg.heartBeat Then labels(3) = "Palette version Of the data In dst2 With " + CStr(classCount) + " regions."
        End Sub
        Public Sub Close()
            If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
        End Sub
    End Class





    Public Class XO_RedList_BProject3D : Inherits TaskParent
        Dim hcloud As New Hist3Dcloud_Basics
        Public Sub New()
            desc = "Run RedList_Basics on the output of the RGB 3D backprojection"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hcloud.Run(src)
            dst3 = hcloud.dst3

            dst3.ConvertTo(dst0, cv.MatType.CV_8U)
            dst2 = runRedList(dst0, labels(2))
        End Sub
    End Class







    Public Class XO_RedList_BrightnessLevel : Inherits TaskParent
        Dim bright As New Brightness_Grid
        Public Sub New()
            desc = "Adjust the brightness so there is no whiteout and then run RedCloud with that."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bright.Run(src)

            dst2 = runRedList(bright.dst2, labels(2))
            dst3 = taskAlg.redList.dst3
        End Sub
    End Class










    ' https://docs.opencv.org/master/de/d01/samples_2cpp_2Region_components_8cpp-example.html
    Public Class XO_RedList_CCompColor : Inherits TaskParent
        Dim ccomp As New CComp_Both
        Public Sub New()
            desc = "Identify each Connected component as a RedCloud Cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            ccomp.Run(src)
            dst3 = ccomp.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            labels(3) = ccomp.labels(2)

            dst2 = runRedList(dst3, labels(2))
        End Sub
    End Class










    Public Class XO_RedList_Consistent : Inherits TaskParent
        Dim redCold As New Bin3Way_RedCloud
        Dim diff As New Diff_Basics
        Dim cellmaps As New List(Of cv.Mat)
        Dim cellLists As New List(Of List(Of oldrcData))
        Dim diffs As New List(Of cv.Mat)
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            taskAlg.featureOptions.ColorDiffSlider.Value = 1
            desc = "Remove RedCloud results that are inconsistent with the previous frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCold.Run(src)
            dst2 = redCold.dst2

            diff.Run(taskAlg.redList.rcMap)
            dst1 = diff.dst2

            cellLists.Add(New List(Of oldrcData)(taskAlg.redList.oldrclist))
            cellmaps.Add(taskAlg.redList.rcMap And Not dst1)
            diffs.Add(dst1.Clone)

            taskAlg.redList.oldrclist.Clear()
            taskAlg.redList.oldrclist.Add(New oldrcData)
            For i = 0 To cellLists.Count - 1
                For Each rc In cellLists(i)
                    Dim present As Boolean = True
                    For j = 0 To cellmaps.Count - 1
                        Dim val = cellmaps(i).Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                        If val = 0 Then
                            present = False
                            Exit For
                        End If
                    Next
                    If present Then
                        rc.index = taskAlg.redList.oldrclist.Count
                        taskAlg.redList.oldrclist.Add(rc)
                    End If
                Next
            Next

            dst2.SetTo(0)
            taskAlg.redList.rcMap.SetTo(0)
            For Each rc In taskAlg.redList.oldrclist
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                taskAlg.redList.rcMap(rc.rect).SetTo(rc.index, rc.mask)
            Next

            For Each mat In diffs
                dst2.SetTo(0, mat)
            Next

            If cellmaps.Count > taskAlg.frameHistoryCount Then
                cellmaps.RemoveAt(0)
                cellLists.RemoveAt(0)
                diffs.RemoveAt(0)
            End If
        End Sub
    End Class






    Public Class XO_RedList_Contour : Inherits TaskParent
        Public Sub New()
            desc = "Add the contour to the cell mask in the RedList_Basics output"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = runRedList(src, labels(2))

            dst2.SetTo(0)
            For Each rc In taskAlg.redList.oldrclist
                For i = 1 To 8
                    Dim deltaX = Choose(i, -1, 1, 0, 0, -1, 1, -1, 1)
                    Dim deltaY = Choose(i, 0, 0, -1, 1, -1, 1, 1, -1)
                    Dim contour As New List(Of cv.Point)
                    For Each pt In rc.contour
                        pt.X += deltaX
                        pt.Y += deltaY
                        pt = lpData.validatePoint(pt)
                        contour.Add(pt)
                    Next
                    If i < 8 Then
                        DrawTour(dst2(rc.rect), contour, rc.color, taskAlg.lineWidth)
                    Else
                        DrawTour(dst2(rc.rect), contour, rc.color, -1)
                    End If
                Next
            Next
        End Sub
    End Class








    Public Class XO_RedList_ContourAdd : Inherits TaskParent
        Public oldrclist As New List(Of oldrcData)
        Public Sub New()

            desc = "Add a contour for each RedColor cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                dst2 = runRedList(src, labels(2))
                oldrclist = taskAlg.redList.oldrclist
            End If

            dst3.SetTo(0)
            For i = 1 To oldrclist.Count - 1
                Dim rc = oldrclist(i)
                rc.contour = ContourBuild(rc.mask)
                DrawTour(rc.mask, rc.contour, 255, -1)
                oldrclist(i) = rc
                DrawTour(dst3(rc.rect), rc.contour, rc.color, -1)
            Next
        End Sub
    End Class









    Public Class XO_RedList_MaxDist : Inherits TaskParent
        Dim addTour As New XO_RedList_ContourAdd
        Public Sub New()
            desc = "Show the maxdist before and after updating the mask with the contour."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            For Each rc In taskAlg.redList.oldrclist
                DrawCircle(dst2, rc.maxDist, taskAlg.DotSize, taskAlg.highlight)
            Next

            addTour.oldrclist = taskAlg.redList.oldrclist
            addTour.Run(src)
            dst3 = addTour.dst3

            For i = 1 To addTour.oldrclist.Count - 1
                Dim rc = addTour.oldrclist(i)
                rc.maxDist = Distance_Basics.GetMaxDist(rc)
                DrawCircle(dst3, rc.maxDist, taskAlg.DotSize, taskAlg.highlight)
            Next
        End Sub
    End Class












    Public Class XO_RedList_DelaunayGuidedFeatures : Inherits TaskParent
        Dim features As New Feature_Delaunay
        Public Sub New()
            labels(2) = "RedCloud Output of GoodFeature points"
            desc = "Track the feature points using RedCloud."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            features.Run(src)
            dst3 = features.dst3
            labels(3) = features.labels(3)

            dst2 = runRedList(dst3, labels(2))
        End Sub
    End Class







    Public Class XO_RedList_DepthOutline : Inherits TaskParent
        Dim outline As New Depth_Outline
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Use the Depth_Outline output over time to isolate high quality cells"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            outline.Run(taskAlg.depthMask)

            If taskAlg.heartBeat Then dst3.SetTo(0)
            dst3 = dst3 Or outline.dst2

            dst1.SetTo(0)
            src.CopyTo(dst1, Not dst3)
            dst2 = runRedList(dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY), labels(2))
        End Sub
    End Class




    Public Class XO_RedList_NoDepth : Inherits TaskParent
        Public Sub New()
            taskAlg.featureOptions.Color8USource.SelectedItem = "Reduction_Basics"
            desc = "Run RedList_Basics on just the regions with no depth."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))
            dst2.SetTo(0, taskAlg.depthMask)
        End Sub
    End Class




    Public Class XO_RedList_FPS : Inherits TaskParent
        Dim fps As New Grid_FPS
        Public Sub New()
            desc = "Display RedCloud output at a fixed frame rate"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fps.Run(src)

            If fps.heartBeat Then
                dst2 = runRedList(src, labels(2)).Clone
                labels(2) = taskAlg.redList.labels(2) + " " + fps.strOut
            End If
        End Sub
    End Class






    Public Class XO_RedList_Gaps : Inherits TaskParent
        Dim frames As New History_Basics
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Find the gaps that are different in the RedList_Basics sharedResults.images.."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            frames.Run(taskAlg.redList.rcMap.InRange(0, 0))
            dst3 = frames.dst2

            If taskAlg.redList.oldrclist.Count > 0 Then
                dst2(taskAlg.oldrcD.rect).SetTo(white, taskAlg.oldrcD.mask)
            End If

            If taskAlg.redList.oldrclist.Count > 0 Then
                Dim rc = taskAlg.redList.oldrclist(0) ' index can now be zero.
                dst3(rc.rect).SetTo(0, rc.mask)
            End If
            Dim count = dst3.CountNonZero
            labels(3) = "Unclassified pixel count = " + CStr(count) + " or " + Format(count / src.Total, "0%")
        End Sub
    End Class







    Public Class XO_RedList_GenCellContains : Inherits TaskParent
        Dim flood As New Flood_Basics
        Dim contains As New Flood_ContainedCells
        Public Sub New()
            desc = "Merge cells contained in the top X cells and remove all other cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            flood.Run(src)
            dst3 = flood.dst2
            If taskAlg.heartBeat Then Exit Sub
            labels(2) = flood.labels(2)

            contains.Run(src)

            dst2.SetTo(0)
            For Each rc In taskAlg.redList.oldrclist
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                dst2.Rectangle(rc.rect, taskAlg.highlight, taskAlg.lineWidth)
            Next
        End Sub
    End Class








    Public Class XO_RedList_GridCellsOld : Inherits TaskParent
        Dim regions As New Region_Contours
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Use the brickData regions to build taskAlg.redList.oldrclist"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            regions.Run(src)
            dst1 = regions.dst2

            runRedList(src, labels(2))

            Dim mdList = New List(Of maskData)(regions.redM.mdList)
            dst2.SetTo(0)
            Dim histogram As New cv.Mat
            Dim ranges = {New cv.Rangef(0, 255)}
            Dim histArray(254) As Single
            Dim oldrclist As New List(Of oldrcData)
            Dim usedList As New List(Of Integer)
            For Each md In mdList
                cv.Cv2.CalcHist({taskAlg.redList.rcMap(md.rect)}, {0}, md.mask, histogram, 1, {255}, ranges)
                Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
                Dim index = oldrclist.Count
                Dim c = dst1.Get(Of cv.Vec3b)(md.maxDist.Y, md.maxDist.X)
                Dim color = New cv.Scalar(c(0), c(1), c(2))
                For i = 1 To histArray.Count - 1
                    If usedList.Contains(i) Then Continue For
                    If histArray(i) > 0 Then
                        Dim rc = taskAlg.redList.oldrclist(i)
                        If rc.depth > md.mm.minVal And rc.depth < md.mm.maxVal Then
                            rc.index = oldrclist.Count
                            rc.color = color
                            dst2(rc.rect).SetTo(rc.color, rc.mask)
                            oldrclist.Add(rc)
                            usedList.Add(i)
                        End If
                    End If
                Next
            Next

            labels(3) = CStr(oldrclist.Count) + " redCloud cells were found"
        End Sub
    End Class







    Public Class XO_RedList_GridCells : Inherits TaskParent
        Dim regions As New Region_Contours
        Public Sub New()
            taskAlg.gOptions.TruncateDepth.Checked = True
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Use the brickData regions to build taskAlg.redList.oldrclist"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            regions.Run(src)
            dst1 = regions.dst2

            runRedList(src, labels(2))
            Dim lastList As New List(Of oldrcData)(taskAlg.redList.oldrclist)

            dst2.SetTo(0)

            Dim oldrclist As New List(Of oldrcData)
            For Each rc In taskAlg.redList.oldrclist
                If taskAlg.motionMask(rc.rect).CountNonZero = 0 Then
                    If rc.indexLast > 0 And rc.indexLast < lastList.Count Then rc = lastList(rc.indexLast)
                End If
                Dim index = oldrclist.Count
                Dim cTest = dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                If cTest <> black Then Continue For
                Dim c = dst1.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                Dim color = New cv.Scalar(c(0), c(1), c(2))
                If color = black Then color = yellow
                rc.index = oldrclist.Count
                rc.color = color
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                DrawCircle(dst2, rc.maxDStable)
                oldrclist.Add(rc)
            Next

            taskAlg.redList.oldrclist = New List(Of oldrcData)(oldrclist)
            labels(3) = CStr(oldrclist.Count) + " redCloud cells were found"
        End Sub
    End Class









    Public Class XO_RedList_GridCellsHist : Inherits TaskParent
        Dim regions As New Region_Contours
        Public Sub New()
            taskAlg.gOptions.TruncateDepth.Checked = True
            desc = "For each redCell find the highest population region it covers."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            regions.Run(src)
            dst1 = regions.redM.dst2

            runRedList(src, labels(2))
            Static rcLastList As New List(Of oldrcData)(taskAlg.redList.oldrclist)

            Dim mdList = New List(Of maskData)(regions.redM.mdList)
            Dim histogram As New cv.Mat
            Dim ranges = {New cv.Rangef(0, 255)}
            Dim oldrclist As New List(Of oldrcData)
            Dim lastCount As Integer
            Dim histArray(mdList.Count - 1) As Single
            For Each rc In taskAlg.redList.oldrclist
                cv.Cv2.CalcHist({dst1(rc.rect)}, {0}, rc.mask, histogram, 1, {255}, ranges)
                Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
                Dim index = histArray.ToList.IndexOf(histArray.Max)
                Dim md = mdList(index)
                rc.color = taskAlg.scalarColors(md.index)
                If rc.indexLast <> 0 Then
                    If (taskAlg.motionMask(rc.rect) And rc.mask).ToMat.CountNonZero = 0 Then
                        rc = rcLastList(rc.indexLast)
                        lastCount += 1
                    End If
                End If
                oldrclist.Add(rc)
            Next

            dst2.SetTo(0)
            For Each rc In oldrclist
                'Dim test = dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                'If test = black Then dst2(rc.rect).SetTo(rc.color, rc.mask)
                dst2(rc.rect).SetTo(rc.color, rc.mask)
            Next

            taskAlg.redList.oldrclist = New List(Of oldrcData)(oldrclist)
            rcLastList = New List(Of oldrcData)(oldrclist)
            labels(3) = CStr(oldrclist.Count) + " redCloud cells were found and " + CStr(lastCount) + " cells had no motion."
        End Sub
    End Class








    Public Class XO_RedList_KMeans : Inherits TaskParent
        Dim km As New KMeans_MultiChannel
        Public Sub New()
            labels = {"", "", "KMeans_MultiChannel output", "RedList_Basics output"}
            desc = "Use RedCloud to identify the regions created by kMeans"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            km.Run(src)
            dst3 = km.dst2

            dst2 = runRedList(dst3, labels(2))
        End Sub
    End Class






    Public Class XO_RedList_Largest : Inherits TaskParent
        Dim options As New Options_History
        Public Sub New()
            OptionParent.FindSlider("Frame History").Value = 1
            desc = "Identify the largest redCloud cells and accumulate them by size - largest to smallest"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))
            If taskAlg.redList.oldrclist.Count = 0 Then Exit Sub ' next frame please...

            Dim rc = taskAlg.redList.oldrclist(1)
            Static rcSave As oldrcData = rc, stableCount As Integer
            If rc.maxDStable <> rcSave.maxDStable Then
                rcSave = rc
                stableCount = 1
            Else
                stableCount += 1
            End If

            dst3.SetTo(0)
            dst3(rc.rect).SetTo(rc.color, rc.mask)
            dst3.Circle(rc.maxDStable, taskAlg.DotSize + 2, cv.Scalar.Black)
            DrawCircle(dst3, rc.maxDStable)
            labels(3) = "MaxDStable was the same for " + CStr(stableCount) + " frames"
        End Sub
    End Class









    Public Class LeftRight_RedMask : Inherits TaskParent
        Dim redLeft As New XO_LeftRight_RedLeft
        Dim redRight As New XO_LeftRight_RedRight
        Public Sub New()
            desc = "Display the RedMask_Basics output for both the left and right images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redLeft.Run(taskAlg.leftView)
            dst2 = redLeft.dst2.Clone
            If standaloneTest() Then
                For Each md In redLeft.redMask.mdList
                    DrawCircle(dst2, md.maxDist, taskAlg.DotSize, taskAlg.highlight)
                Next
            End If

            redRight.Run(taskAlg.rightView)
            dst3 = redRight.dst2.Clone
            If standaloneTest() Then
                For Each md In redRight.redMask.mdList
                    DrawCircle(dst3, md.maxDist, taskAlg.DotSize, taskAlg.highlight)
                Next
            End If
            labels(2) = redLeft.labels(2)
            labels(3) = redRight.labels(2)
        End Sub
    End Class






    Public Class XO_LeftRight_RedRight : Inherits TaskParent
        Dim fLess As New FeatureLess_Basics
        Public redMask As New RedMask_Basics
        Public Sub New()
            desc = "Segment the right view image with RedMask_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = taskAlg.rightView.Clone
            fLess.Run(taskAlg.rightView)
            dst2 = fLess.dst2
            redMask.Run(fLess.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            dst2 = PaletteFull(redMask.dst2)
            labels(2) = redMask.labels(2)
        End Sub
    End Class







    Public Class XO_LeftRight_RedLeft : Inherits TaskParent
        Dim fLess As New FeatureLess_Basics
        Public redMask As New RedMask_Basics
        Public Sub New()
            desc = "Segment the left view image with RedMask_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = taskAlg.leftView
            fLess.Run(src)
            redMask.Run(fLess.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            dst2 = PaletteFull(redMask.dst2)
            labels(2) = redMask.labels(2)
        End Sub
    End Class






    Public Class XO_RedList_LeftRight : Inherits TaskParent
        Dim redLR As New LeftRight_RedMask
        Public Sub New()
            desc = "Run RedCloud on the left and right images.  Duplicate of LeftRight_RedCloudBoth"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redLR.Run(src)
            dst2 = redLR.dst2
            dst3 = redLR.dst3
            labels = redLR.labels
        End Sub
    End Class






    Public Class XO_RedList_OutlineColor : Inherits TaskParent
        Dim outline As New Depth_Outline
        Dim color8U As New Color8U_Basics
        Public Sub New()
            labels(3) = "Color input to RedList_Basics with depth boundary blocking color connections."
            desc = "Use the depth outline as input to RedList_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            outline.Run(taskAlg.depthMask)

            color8U.Run(src)
            dst1 = color8U.dst2 + 1
            dst1.SetTo(0, outline.dst2)
            dst3 = PaletteFull(dst1)

            dst2 = runRedList(dst1, labels(2))
        End Sub
    End Class





    Public Class XO_RedList_PlusTiers : Inherits TaskParent
        Dim tiers As New Depth_Tiers
        Dim binar4 As New Bin4Way_Regions
        Public Sub New()
            desc = "Add the depth tiers to the input for RedList_Basics."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            tiers.Run(src)
            binar4.Run(src)
            dst2 = runRedList(binar4.dst2 + tiers.dst2, labels(2))
        End Sub
    End Class




    Public Class XO_RedList_Reduction : Inherits TaskParent
        Public Sub New()
            taskAlg.gOptions.setHistogramBins(20)
            desc = "Segment the image based On both the reduced color"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))
            dst3 = taskAlg.redList.rcMap
        End Sub
    End Class







    Public Class XO_RedList_CellChanges : Inherits TaskParent
        Dim dst2Last As cv.Mat = dst2.Clone
        Public Sub New()
            desc = "Count the cells that have changed in a RedCloud generation"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            dst3 = dst2 - dst2Last

            Dim changedPixels = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero
            Dim changedCells As Integer
            For Each rc As oldrcData In taskAlg.redList.oldrclist
                If rc.indexLast = 0 Then changedCells += 1
            Next

            dst2Last = dst2.Clone
            If taskAlg.heartBeat Then
                labels(2) = "Changed cells = " + Format(changedCells, "000") + " cells or " + Format(changedCells / taskAlg.redList.oldrclist.Count, "0%")
                labels(3) = "Changed pixel total = " + Format(changedPixels / 1000, "0.0") + "k or " + Format(changedPixels / dst2.Total, "0%")
            End If
        End Sub
    End Class






    Public Class XO_Bin2Way_RedCloud : Inherits TaskParent
        Dim bin2 As New Bin2Way_RecurseOnce
        Dim flood As New Flood_BasicsMask
        Dim cellMaps(3) As cv.Mat, oldrclist(3) As List(Of oldrcData)
        Dim options As New Options_Bin2WayRedCloud
        Public Sub New()
            flood.showSelected = False
            desc = "Identify the lightest, darkest, and other regions separately and then combine the oldrcData."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            dst3 = runRedList(src, labels(3))

            If taskAlg.optionsChanged Then
                For i = 0 To oldrclist.Count - 1
                    oldrclist(i) = New List(Of oldrcData)
                    cellMaps(i) = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
                Next
            End If

            bin2.Run(src)

            Dim sortedCells As New SortedList(Of Integer, oldrcData)(New compareAllowIdenticalIntegerInverted)
            For i = options.startRegion To options.endRegion
                taskAlg.redList.rcMap = cellMaps(i)

                taskAlg.redList.oldrclist = oldrclist(i)
                flood.inputRemoved = Not bin2.mats.mat(i)
                flood.Run(bin2.mats.mat(i))
                cellMaps(i) = taskAlg.redList.rcMap.Clone
                oldrclist(i) = New List(Of oldrcData)(taskAlg.redList.oldrclist)
                For Each orc In taskAlg.redList.oldrclist
                    If orc.index = 0 Then Continue For
                    sortedCells.Add(orc.pixels, orc)
                Next
            Next

            dst2 = RebuildRCMap(sortedCells.Values.ToList)

            If taskAlg.heartBeat Then labels(2) = CStr(taskAlg.redList.oldrclist.Count) + " cells were identified and matched to the previous image"
        End Sub
    End Class





    Public Class XO_RedCloudAndColor_Basics : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim reduction As New Reduction_Basics
        Public Sub New()
            taskAlg.gOptions.UseMotionMask.Checked = False
            desc = "Use RedColor for regions with no depth to add cells to RedCloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(src, labels(1))

            Static rcListLast = New List(Of rcData)
            Dim rcMapLast = rcMap.clone
            rcList = New List(Of rcData)(taskAlg.redCloud.rcList)

            dst3 = taskAlg.gray
            dst3.SetTo(0, taskAlg.depthMask)
            reduction.Run(dst3)
            dst1 = reduction.dst2 - 1

            Dim index = 1
            Dim rect As cv.Rect
            Dim minCount = dst2.Total * 0.001
            Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
            Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
            Dim newList As New List(Of rcData)
            For y = 0 To dst1.Height - 1
                For x = 0 To dst1.Width - 1
                    Dim pt = New cv.Point(x, y)
                    ' skip the regions with no depth or those that were already floodfilled.
                    If dst1.Get(Of Byte)(pt.Y, pt.X) >= index Then
                        Dim count = cv.Cv2.FloodFill(dst1, mask, pt, index, rect, 0, 0, flags)
                        If rect.Width > 0 And rect.Height > 0 Then
                            'If count >= minCount Then
                            Dim pc = New rcData(dst3(rect), rect, index)
                            If pc Is Nothing Then Continue For
                            pc.color = taskAlg.scalarColors(pc.index)
                            newList.Add(pc)
                            'dst1(pc.rect).SetTo(pc.index Mod 255, pc.mask)
                            SetTrueText(CStr(pc.index), pc.rect.TopLeft)
                            index += 1
                            'Else
                            '    dst1(rect).SetTo(255, mask(rect))
                            'End If
                        End If
                    End If
                Next
            Next

            RedCloud_Cell.selectCell(rcMap, rcList)
            If taskAlg.rcD IsNot Nothing Then strOut = taskAlg.rcD.displayCell()
            SetTrueText(strOut, 3)

            labels(2) = "Cells found = " + CStr(rcList.Count) + " and " + CStr(newList.Count) + " were color only cells."

            rcListLast = New List(Of rcData)(rcList)
        End Sub
    End Class





    Public Class XO_Line_Motion : Inherits TaskParent
        Dim diff As New Diff_RGBAccum
        Dim lineHistory As New List(Of List(Of lpData))
        Dim options As New Options_History
        Public Sub New()
            labels(3) = "Wave at the camera to see results - "
            desc = "Track lines that are the result of motion."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            diff.Run(src)
            dst2 = diff.dst2

            If taskAlg.heartBeat Then dst3 = src
            lineHistory.Add(taskAlg.lines.lpList)
            For Each lplist In lineHistory
                For Each lp In lplist
                    vbc.DrawLine(dst3, lp.p1, lp.p2)
                Next
            Next
            If lineHistory.Count > taskAlg.frameHistoryCount Then lineHistory.RemoveAt(0)

            labels(2) = CStr(taskAlg.lines.lpList.Count) + " lines were found in the diff output"
        End Sub
    End Class






    Public Class XO_MinMath_Line : Inherits TaskParent
        Dim bPoints As New BrickPoint_Basics
        Public lpList As New List(Of lpData) ' lines after being checked with brick points.
        Public Sub New()
            desc = "Track lines with brickpoints."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bPoints.Run(src)
            dst2 = bPoints.dst2
            labels(2) = bPoints.labels(2)

            Dim linesFound As New List(Of Byte)
            Dim ptList(taskAlg.lines.lpList.Count - 1) As List(Of cv.Point)
            Dim lpRectMap = XO_Line_CoreNew.createMap()
            For Each bp In bPoints.ptList
                Dim val = lpRectMap.Get(Of Byte)(bp.Y, bp.X)
                If val = 0 Then Continue For
                If linesFound.Contains(val) = False Then
                    linesFound.Add(val)
                    ptList(val) = New List(Of cv.Point)
                End If
                ptList(val).Add(bp)
            Next

            dst3.SetTo(0)
            lpList.Clear()
            For i = 0 To ptList.Count - 1
                If ptList(i) Is Nothing Then Continue For
                Dim p1 = ptList(i)(0)
                Dim p2 = ptList(i)(ptList(i).Count - 1)
                vbc.DrawLine(dst2, p1, p2)
                Dim lp = New lpData(p1, p2)
                lpList.Add(lp)
                vbc.DrawLine(dst3, p1, p2)
            Next

            For Each index In linesFound
                Dim lp = taskAlg.lines.lpList(index - 1)
            Next
            labels(3) = CStr(linesFound.Count) + " lines were confirmed by brick points."
        End Sub
    End Class





    Public Class XO_Motion_Basics : Inherits TaskParent
        Public lastColor(0) As cv.Vec3f
        Public cellAge(0) As Integer
        Public motionFlags(0) As Boolean
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "Below Is the difference between the current image And the dst2 at left which Is composed Using the motion mask."
            desc = "Isolate all motion In the scene"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If lastColor.Count <> taskAlg.gridRects.Count Then
                ReDim lastColor(taskAlg.gridRects.Count - 1)
                ReDim cellAge(taskAlg.gridRects.Count - 1)
            End If

            Dim colorstdev As cv.Scalar, colorMean As cv.Scalar
            ReDim motionFlags(taskAlg.gridRects.Count - 1)
            Dim motionList As New List(Of Integer)
            For i = 0 To taskAlg.gridRects.Count - 1
                cv.Cv2.MeanStdDev(src(taskAlg.gridRects(i)), colorMean, colorstdev)
                Dim colorVec = New cv.Vec3f(colorMean(0), colorMean(1), colorMean(2))
                Dim colorChange = Distance_Basics.distance3D(colorVec, lastColor(i))
                If colorChange > taskAlg.motionThreshold Then
                    lastColor(i) = colorVec
                    For Each index In taskAlg.grid.gridNeighbors(i)
                        If motionList.Contains(index) = False Then
                            motionFlags(index) = True
                            motionList.Add(index)
                        End If
                    Next
                End If
            Next

            dst1.SetTo(0)
            For Each i In motionList
                dst1(taskAlg.gridRects(i)).SetTo(255)
                motionFlags(i) = True
            Next

            labels(2) = Format(motionList.Count / taskAlg.gridRects.Count, "00%") + " Of bricks had motion."

            If taskAlg.gOptions.UseMotionMask.Checked = False Then dst1.SetTo(255)

            If standaloneTest() Then
                If taskAlg.gOptions.UseMotionMask.Checked Then src.CopyTo(dst2, dst3)
                Static diff As New Diff_Basics
                diff.lastFrame = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                diff.Run(src)
                dst3 = diff.dst2
                SetTrueText("NOTE: the differences below should be small - no artifacts should be present." + vbCrLf +
                        "Any differences that persist should not be visible in the RGB image at left." + vbCrLf, 3)
            End If
            If taskAlg.heartBeat Then dst2 = src.Clone
            taskAlg.motionMask = dst1.Clone
        End Sub
    End Class






    Public Class XO_RedColor_BasicsFast : Inherits TaskParent
        Public classCount As Integer
        Public RectList As New List(Of cv.Rect)
        Public Sub New()
            cPtr = RedCloud_Open()
            desc = "Run the C++ RedCloud interface without a mask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = Mat_Basics.srcMustBe8U(src)

            Dim imagePtr As IntPtr
            Dim inputData(dst1.Total - 1) As Byte
            Marshal.Copy(dst1.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
            handleInput.Free()
            dst2 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, imagePtr).Clone
            dst3 = PaletteFull(dst2)

            classCount = RedCloud_Count(cPtr)
            labels(2) = "CV_8U version with " + CStr(classCount) + " cells."

            If classCount = 0 Then Exit Sub ' no data to process.

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))

            Dim rects(classCount * 4) As Integer
            Marshal.Copy(rectData.Data, rects, 0, rects.Length)

            Dim minPixels = dst2.Total * 0.001
            RectList.Clear()

            For i = 0 To rects.Length - 4 Step 4
                Dim r = New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3))
                If r.Width * r.Height >= minPixels Then
                    RectList.Add(r)
                    dst3.Rectangle(r, taskAlg.highlight, taskAlg.lineWidth)
                End If
            Next
            labels(3) = CStr(RectList.Count) + " cells were found."
        End Sub
        Public Sub Close()
            If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
        End Sub
    End Class




    Public Class XO_Motion_RectHistory : Inherits TaskParent
        Dim motion As New XO_Motion_Enclosing
        Dim diff As New Diff_Basics
        Dim lastRects As New List(Of cv.Rect)
        Public Sub New()
            labels(3) = "The white spots show the difference of the constructed image from the current image."
            desc = "Track taskAlg.gray using Motion_Enclosing to isolate the motion"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motion.Run(src)
            Dim r = motion.motionRect
            If taskAlg.heartBeat Then
                dst2 = src.Clone
                lastRects.Clear()
            Else
                For Each rect In lastRects
                    r = r.Union(rect)
                Next
                dst2 = taskAlg.motionBasics.dst2
                lastRects.Add(r)
                If lastRects.Count > taskAlg.frameHistoryCount Then lastRects.RemoveAt(0)
            End If

            If standaloneTest() Then
                diff.lastFrame = taskAlg.gray
                diff.Run(dst2)
                dst3 = diff.dst3
                dst3.Rectangle(r, white, taskAlg.lineWidth)
            End If
        End Sub
    End Class





    Public Class XO_Motion_ThruCorrelation : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then
                sliders.setupTrackBar("Correlation threshold X1000", 0, 1000, 900)
                sliders.setupTrackBar("Stdev threshold for using correlation", 0, 100, 15)
                sliders.setupTrackBar("Pad size in pixels for the search area", 0, 100, 20)
            End If

            dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Detect motion through the correlation coefficient"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static ccSlider = OptionParent.FindSlider("Correlation threshold X1000")
            Static padSlider = OptionParent.FindSlider("Pad size in pixels for the search area")
            Static stdevSlider = OptionParent.FindSlider("Stdev threshold for using correlation")
            Dim pad = padSlider.Value
            Dim ccThreshold = ccSlider.Value
            Dim stdevThreshold = stdevSlider.Value

            Dim input = src.Clone
            If input.Channels() <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Static lastFrame As cv.Mat = input.Clone
            dst3.SetTo(0)
            Parallel.For(0, taskAlg.gridRects.Count,
        Sub(i)
            Dim roi = taskAlg.gridRects(i)
            Dim correlation As New cv.Mat
            Dim mean As Single, stdev As Single
            cv.Cv2.MeanStdDev(input(roi), mean, stdev)
            If stdev > stdevThreshold Then
                cv.Cv2.MatchTemplate(lastFrame(roi), input(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                Dim mm As mmData = GetMinMax(correlation)
                If mm.maxVal < ccThreshold / 1000 Then
                    If (i Mod taskAlg.bricksPerCol) <> 0 Then dst3(taskAlg.gridRects(i - 1)).SetTo(255)
                    If (i Mod taskAlg.bricksPerCol) < taskAlg.bricksPerCol And i < taskAlg.gridRects.Count - 1 Then dst3(taskAlg.gridRects(i + 1)).SetTo(255)
                    If i > taskAlg.bricksPerCol Then
                        dst3(taskAlg.gridRects(i - taskAlg.bricksPerCol)).SetTo(255)
                        dst3(taskAlg.gridRects(i - taskAlg.bricksPerCol + 1)).SetTo(255)
                    End If
                    If i < (taskAlg.gridRects.Count - taskAlg.bricksPerCol - 1) Then
                        dst3(taskAlg.gridRects(i + taskAlg.bricksPerCol)).SetTo(255)
                        dst3(taskAlg.gridRects(i + taskAlg.bricksPerCol + 1)).SetTo(255)
                    End If
                    dst3(roi).SetTo(255)
                End If
            End If
        End Sub)

            lastFrame = input.Clone

            If taskAlg.heartBeat Then dst2 = src.Clone Else src.CopyTo(dst2, dst3)
        End Sub
    End Class




    '  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
    Public Class XO_Motion_Diff : Inherits TaskParent
        Public options As New Options_Diff
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            labels = {"", "", "Unstable mask", "Pixel difference"}
            desc = "Capture an image and use absDiff/threshold to compare it to the last snapshot"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Channels() <> 1 Then src = taskAlg.gray
            If taskAlg.heartBeat Or dst1.Channels <> 1 Then
                dst1 = src.Clone
                dst2.SetTo(0)
            End If

            cv.Cv2.Absdiff(src, dst1, dst3)
            dst2 = dst3.Threshold(options.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            dst1 = src.Clone
        End Sub
    End Class





    Public Class XO_Motion_BGSub : Inherits TaskParent
        Public bgSub As New BGSubtract_MOG2
        Dim motion As New XO_Motion_BGSub_QT
        Public Sub New()
            desc = "Use floodfill to find all the real motion in an image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bgSub.Run(src)
            motion.Run(bgSub.dst2)
            dst2 = motion.dst2
            labels(2) = motion.labels(2)
        End Sub
    End Class






    Public Class XO_Motion_BGSub_QT : Inherits TaskParent
        Public bgSub As New BGSubtract_MOG2
        Dim rectList As New List(Of cv.Rect)
        Public Sub New()
            taskAlg.redList = New XO_RedList_Basics
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "The option-free version of Motion_BGSub"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() <> 1 Then
                bgSub.Run(src)
                src = bgSub.dst2
            End If

            dst2 = src

            taskAlg.redList.Run(src.Threshold(0, 255, cv.ThresholdTypes.Binary))
            If taskAlg.redList.oldrclist.Count < 2 Then
                rectList.Clear()
            Else
                Dim nextRect = taskAlg.redList.oldrclist.ElementAt(1).rect
                For i = 2 To taskAlg.redList.oldrclist.Count - 1
                    Dim rc = taskAlg.redList.oldrclist.ElementAt(i)
                    nextRect = nextRect.Union(rc.rect)
                Next
            End If

            If standaloneTest() Then
                If taskAlg.redList.oldrclist.Count > 1 Then
                    labels(2) = CStr(taskAlg.redList.oldrclist.Count) + " RedMask cells had motion"
                Else
                    labels(2) = "No motion detected"
                End If
                labels(3) = ""
            End If
        End Sub
    End Class






    Public Class XO_Motion_BasicsHistory : Inherits TaskParent
        Public motionList As New List(Of Integer)
        Dim diff As New Diff_Basics
        Public Sub New()
            dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "The motion mask"
            desc = "Isolate all motion in the scene"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.gOptions.UseMotionMask.Checked = False Then Exit Sub

            If src.Channels <> 1 Then src = taskAlg.gray
            If taskAlg.heartBeat Then dst2 = src.Clone

            diff.Run(src)

            motionList.Clear()
            dst3.SetTo(0)
            For i = 0 To taskAlg.gridRects.Count - 1
                Dim diffCount = diff.dst2(taskAlg.gridRects(i)).CountNonZero
                If diffCount >= taskAlg.motionThreshold Then
                    For Each index In taskAlg.grid.gridNeighbors(i)
                        If motionList.Contains(index) = False Then
                            motionList.Add(index)
                        End If
                    Next
                End If
            Next

            Static cellList As New List(Of List(Of Integer))
            cellList.Add(motionList)
            dst3.SetTo(0)
            For Each lst In cellList
                For Each index In lst
                    dst3(taskAlg.gridRects(index)).SetTo(255)
                Next
            Next

            If cellList.Count >= taskAlg.frameHistoryCount Then cellList.RemoveAt(0)
            src.CopyTo(dst2, dst3)
            taskAlg.motionMask = dst3.Clone

            labels(2) = CStr(motionList.Count) + " grid rect's or " +
                    Format(motionList.Count / taskAlg.gridRects.Count, "0.0%") +
                    " of bricks had motion."
        End Sub
    End Class




    Public Class XO_Motion_BestBricks : Inherits TaskParent
        Dim match As New Match_Basics
        Dim brickLine As New BrickLine_LeftRight
        Public Sub New()
            labels(2) = "Best bricks are shown below and their match offsets show in dst3 - X/Y"
            desc = "Identify the motion for each of the 'Best' bricks"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            brickLine.Run(src)
            dst2 = brickLine.dst2
            Static lastGray = taskAlg.gray.Clone

            Dim offsetX As New List(Of Single)
            Dim offsetY As New List(Of Single)
            dst3 = lastGray.clone
            For Each index In brickLine.bestBricks
                Dim nabeRect = taskAlg.gridNabeRects(index)
                match.template = lastGray(taskAlg.gridRects(index))
                match.Run(taskAlg.gray(nabeRect))

                Dim x = match.newCenter.X - nabeRect.Width / 2
                Dim y = match.newCenter.Y - nabeRect.Height / 2
                offsetX.Add(x)
                offsetY.Add(y)

                Dim rect = match.newRect
                rect.X += nabeRect.X
                rect.Y += nabeRect.Y
                SetTrueText(Format(x, fmt0) + "/" + Format(y, fmt0), rect.TopLeft, 3)
            Next

            lastGray = taskAlg.gray.Clone
            If offsetX.Count > 0 Then
                labels(3) = "Average offset X/Y = " + Format(offsetX.Average(), fmt3) + "/" + Format(offsetY.Average(), fmt3)
            End If
        End Sub
    End Class






    Public Class XO_Motion_Blob : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold for punch", 0, 255, 250)
            desc = "Identify the difference in pixels from one image to the next"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static thresholdSlider = OptionParent.FindSlider("Threshold for punch")
            Dim threshold = thresholdSlider.value

            Static lastColor As cv.Mat = src.Clone

            dst2 = src.Clone
            dst2 -= lastColor
            dst3 = dst2.Threshold(0, New cv.Scalar(threshold, threshold, threshold), cv.ThresholdTypes.Binary).ConvertScaleAbs

            dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

            lastColor = src.Clone
        End Sub
    End Class





    Public Class XO_Motion_BlobGray : Inherits TaskParent
        Public Sub New()
            desc = "Identify the difference in pixels from one image to the next"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static lastGray As cv.Mat = taskAlg.gray.Clone

            dst2 = taskAlg.gray.Clone
            dst2 -= lastGray
            dst3 = dst2.Threshold(taskAlg.motionThreshold, 255, cv.ThresholdTypes.Binary)

            lastGray = taskAlg.gray.Clone
        End Sub
    End Class





    Public Class XO_Motion_CenterRect : Inherits TaskParent
        Dim gravitySnap As New lpData
        Public template As cv.Mat
        Dim options As New Options_Features
        Dim correlation As Single
        Dim matchRect As cv.Rect
        Public inputRect As cv.Rect
        Public matchCenter As cv.Point
        Public translation As cv.Point2f
        Public angle As Single ' in degrees.
        Public rotatedRect As cv.RotatedRect
        Dim drawRotate As New Draw_RotatedRect
        Public Sub New()
            labels(3) = "MatchTemplate output for centerRect - center is black"
            desc = "Build a center rectangle and track it with MatchTemplate."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            ' set a low threshold to make the results more visible.
            Dim correlationThreshold = 0.95 ' If(taskAlg.gOptions.debugChecked, 0.5, 0.9)
            If taskAlg.heartBeatLT Or gravitySnap.p1.X = 0 Or correlation < correlationThreshold Then
                If inputRect.Width <> 0 Then taskAlg.centerRect = inputRect
                template = src(taskAlg.centerRect).Clone
                gravitySnap = taskAlg.lineGravity
            End If

            cv.Cv2.MatchTemplate(template, src, dst3, options.matchOption)

            Dim mm = GetMinMax(dst3)

            correlation = mm.maxVal
            Dim w = template.Width, h = template.Height
            matchCenter = New cv.Point(mm.maxLoc.X + taskAlg.centerRect.X, mm.maxLoc.Y + taskAlg.centerRect.Y)
            matchRect = New cv.Rect(mm.maxLoc.X, mm.maxLoc.Y, w, h)

            dst2 = src.Clone
            dst2.Rectangle(matchRect, taskAlg.highlight, taskAlg.lineWidth)

            dst3 = dst3.Normalize(0, 255, cv.NormTypes.MinMax).Resize(dst2.Size)
            DrawCircle(dst3, matchCenter, taskAlg.DotSize, cv.Scalar.Black)

            Dim smp = New lpData(gravitySnap.p1, gravitySnap.p2)
            dst2.Line(smp.p1, smp.p2, taskAlg.highlight, taskAlg.lineWidth + 2, taskAlg.lineType)

            Dim xDisp = matchCenter.X - dst2.Width / 2
            Dim yDisp = matchCenter.Y - dst2.Height / 2
            translation = New cv.Point2f(xDisp, yDisp)

            Dim mp = taskAlg.lineGravity
            dst2.Line(mp.p1, mp.p2, black, taskAlg.lineWidth, taskAlg.lineType)

            Dim sideAdjacent = dst2.Height / 2
            Dim sideOpposite = Math.Abs(smp.p1.X - dst2.Width / 2)
            Dim rotationSnap = Math.Atan(sideOpposite / sideAdjacent) * 180 / cv.Cv2.PI

            sideOpposite = Math.Abs(mp.p1.X - dst2.Width / 2)
            Dim rotationGravity = Math.Atan(sideOpposite / sideAdjacent) * 180 / cv.Cv2.PI

            angle = rotationSnap - rotationGravity
            rotatedRect = New cv.RotatedRect(matchCenter, matchRect.Size, angle)

            drawRotate.rr = rotatedRect
            drawRotate.Run(dst2)
            dst2 = drawRotate.dst2

            labels(2) = "Correlation = " + Format(correlation, fmt3) + ", Translation = (" +
                    Format(xDisp, fmt1) + "," + Format(yDisp, fmt1) + ") " +
                    "Rotation = " + Format(angle, fmt1) + " degrees"
        End Sub
    End Class







    Public Class XO_Motion_CenterKalman : Inherits TaskParent
        Dim motion As New XO_Motion_CenterRect
        Dim kalmanRR As New Kalman_Basics
        Dim centerRect As cv.Rect
        Dim drawRotate As New Draw_RotatedRect
        Public Sub New()
            taskAlg.kalman = New Kalman_Basics
            ReDim taskAlg.kalman.kInput(2 - 1)
            labels(3) = "Template for motion matchTemplate.  Shake the camera to see Kalman impact."
            desc = "Kalmanize the output of center rotation"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.firstPass Then centerRect = taskAlg.centerRect
            dst2 = src.Clone
            motion.Run(src)

            Dim newRect As cv.Rect
            If motion.translation.X = 0 And motion.translation.Y = 0 And motion.angle = 0 Then
                newRect = centerRect
                drawRotate.rr = New cv.RotatedRect(motion.matchCenter, taskAlg.centerRect.Size, 0)
            Else
                taskAlg.kalman.kInput = {motion.translation.X, motion.translation.Y}
                taskAlg.kalman.Run(emptyMat)

                newRect = New cv.Rect(centerRect.X + taskAlg.kalman.kOutput(0), centerRect.Y + taskAlg.kalman.kOutput(1),
                                  centerRect.Width, centerRect.Height)

                kalmanRR.kInput = New Single() {motion.matchCenter.X, motion.matchCenter.Y, motion.angle}
                kalmanRR.Run(src)

                Dim pt = New cv.Point2f(kalmanRR.kOutput(0), kalmanRR.kOutput(1))
                drawRotate.rr = New cv.RotatedRect(pt, taskAlg.centerRect.Size, kalmanRR.kOutput(2))
            End If

            drawRotate.Run(dst2)
            dst2 = drawRotate.dst2
            dst2.Rectangle(newRect, taskAlg.highlight, taskAlg.lineWidth)

            dst3(centerRect) = motion.template
            labels(2) = motion.labels(2)
        End Sub
    End Class






    Public Class XO_Motion_CenterLeftRight : Inherits TaskParent
        Dim CenterC As New XO_Motion_CenterRect
        Dim leftC As New XO_Motion_CenterRect
        Dim rightC As New XO_Motion_CenterRect
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Calculate translation and rotation for both left and right images"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            CenterC.Run(src)
            dst1 = CenterC.dst2
            labels(1) = CenterC.labels(2)

            If taskAlg.leftView.Channels = 1 Then
                leftC.Run(taskAlg.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
            Else
                leftC.Run(taskAlg.leftView)
            End If

            dst2 = leftC.dst2
            labels(2) = leftC.labels(2)

            If taskAlg.rightView.Channels = 1 Then
                rightC.Run(taskAlg.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
            Else
                rightC.Run(taskAlg.rightView)
            End If

            dst3 = rightC.dst2
            labels(3) = rightC.labels(2)

            Debug.WriteLine("translation X,Y (C/L/R): " + Format(CenterC.translation.X, fmt0) + "/" +
                         Format(leftC.translation.X, fmt0) + "/" + Format(rightC.translation.X, fmt0) +
                         ", " + Format(CenterC.translation.Y, fmt0) + "/" + Format(leftC.translation.Y, fmt0) +
                         "/" + Format(rightC.translation.Y, fmt0) + " rotation angle = " + Format(CenterC.angle, fmt1) +
                         "/" + Format(leftC.angle, fmt1) + "/" + Format(rightC.angle, fmt1))
        End Sub
    End Class





    Public Class XO_Motion_CenterRotation : Inherits TaskParent
        Dim motion As New XO_Motion_CenterRect
        Dim vertRect As cv.Rect
        Dim options As New Options_Threshold
        Public mp As lpData
        Public angle As Single
        Public rotatedRect As cv.RotatedRect
        Dim drawRotate As New Draw_RotatedRect
        Public Sub New()
            Dim w = dst2.Width
            vertRect = New cv.Rect(w / 2 - w / 4, 0, w / 2, dst2.Height)
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            OptionParent.FindSlider("Threshold value").Value = 200
            desc = "Find the approximate rotation angle using the diamond shape " +
               "from the thresholded MatchTemplate output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim dCount = dst2.CountNonZero
            Static tSlider = OptionParent.FindSlider("Threshold value")
            If dCount > dst2.Total / 100 Then
                Dim nextval = tSlider.value + 1
                If nextval < tSlider.maximum Then tSlider.value = nextval
            ElseIf dCount < dst2.Total / 200 Then
                Dim nextval = tSlider.value - 1
                If nextval >= 0 Then tSlider.value = nextval
            End If

            motion.Run(src)
            dst1 = motion.dst3

            dst1(vertRect).ConvertTo(dst0(vertRect), cv.MatType.CV_8U)

            Dim mm = GetMinMax(dst1)
            dst2 = dst0.Threshold(options.threshold, 255, cv.ThresholdTypes.Binary)

            dst3 = src.Clone

            Dim tmp As New cv.Mat
            cv.Cv2.FindNonZero(dst2, tmp)

            If tmp.Rows > 2 Then
                Dim topPoint = tmp.Get(Of cv.Point)(0, 0)
                Dim botPoint = tmp.Get(Of cv.Point)(tmp.Rows - 1, 0)

                Dim pair = New lpData(topPoint, botPoint)
                mp = findEdgePoints(pair)
                dst3.Line(mp.p1, mp.p2, taskAlg.highlight, taskAlg.lineWidth + 1, taskAlg.lineType)

                Dim sideAdjacent = dst2.Height
                Dim sideOpposite = mp.p1.X - mp.p2.X
                angle = Math.Atan(sideOpposite / sideAdjacent) * 180 / cv.Cv2.PI
                If mp.p1.Y = dst2.Height Then angle = -angle
                rotatedRect = New cv.RotatedRect(mm.maxLoc, taskAlg.centerRect.Size, angle)
                labels(3) = "angle = " + Format(angle, fmt1) + " degrees"
                drawRotate.rr = rotatedRect
                drawRotate.Run(dst3)
                dst3 = drawRotate.dst2
            End If
            labels(3) = motion.labels(2)
        End Sub
    End Class






    Public Class XO_Motion_Enclosing : Inherits TaskParent
        Dim learnRate As Double
        Public motionRect As New cv.Rect
        Public Sub New()
            If dst2.Width >= 1280 Then learnRate = 0.5 Else learnRate = 0.1 ' learn faster with large images (slower frame rate)
            cPtr = BGSubtract_BGFG_Open(4)
            labels(2) = "MOG2 is the best option.  See BGSubtract_Basics to see more options."
            desc = "Build an enclosing rectangle for the motion"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
            Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
            Dim imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels, learnRate)
            handleSrc.Free()

            dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr).Threshold(0, 255, cv.ThresholdTypes.Binary)

            dst3 = runRedList(dst2, labels(2), Not dst2)

            motionRect = New cv.Rect
            If taskAlg.redList.oldrclist.Count < 2 Then Exit Sub
            motionRect = taskAlg.redList.oldrclist.ElementAt(1).rect
            For i = 2 To taskAlg.redList.oldrclist.Count - 1
                Dim rc = taskAlg.redList.oldrclist.ElementAt(i)
                motionRect = motionRect.Union(rc.rect)
            Next

            If motionRect.Width > dst2.Width / 2 And motionRect.Height > dst2.Height / 2 Then
                motionRect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
            End If
            DrawRect(dst2, motionRect, 255)
        End Sub
        Public Sub Close()
            If cPtr <> 0 Then cPtr = BGSubtract_BGFG_Close(cPtr)
        End Sub
    End Class




    Public Class XO_Motion_Longest : Inherits TaskParent
        Public Sub New()
            labels(2) = "Move camera to show tolerance to motion for the longest line."
            desc = "Determine the motion of the end points of the longest line."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = taskAlg.gray

            Dim lp = New lpData(taskAlg.lineLongest.pE1, taskAlg.lineLongest.pE2)
            vbc.DrawLine(dst2, lp, white)
        End Sub
    End Class





    Public Class XO_Motion_PixelDiff : Inherits TaskParent
        Public changedPixels As Integer
        Dim changeCount As Integer, frames As Integer
        Public options As New Options_Diff
        Public Sub New()
            desc = "Count the number of changed pixels in the current frame and accumulate them.  If either exceeds thresholds, then set flag = true.  " +
               "To get the Options Slider, use " + traceName + "QT"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Static lastFrame As cv.Mat = src
            cv.Cv2.Absdiff(src, lastFrame, dst2)
            dst2 = dst2.Threshold(options.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            changedPixels = dst2.CountNonZero
            Dim motionTest = changedPixels > 0

            If motionTest Then changeCount += 1
            frames += 1
            If taskAlg.heartBeat Then
                strOut = "Pixels changed = " + CStr(changedPixels) + " at last heartbeat.  Since last heartbeat: " +
                     Format(changeCount / frames, "0%") + " of frames were different"
                changeCount = 0
                frames = 0
            End If
            SetTrueText(strOut, 3)
            If motionTest Then lastFrame = src
        End Sub
    End Class




    Public Class XO_RedCloud_MotionSimple : Inherits TaskParent
        Dim redContours As New RedCloud_Basics
        Public Sub New()
            taskAlg.gOptions.HistBinBar.Maximum = 255
            taskAlg.gOptions.HistBinBar.Value = 255
            desc = "Use motion to identify which cells changed."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redContours.Run(src)
            dst1 = redContours.dst1
            dst2 = redContours.dst2
            labels(2) = redContours.labels(2)

            dst1.SetTo(0, Not taskAlg.motionMask)

            Dim histogram As New cv.Mat
            Dim ranges = {New cv.Rangef(1, 256)}
            cv.Cv2.CalcHist({dst1}, {0}, New cv.Mat, histogram, 1, {taskAlg.histogramBins}, ranges)

            Dim histArray(histogram.Rows - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            Dim pcUsed As New List(Of Integer)
            If taskAlg.heartBeat Then dst3 = dst2.Clone
            For i = 1 To histArray.Count - 1
                If histArray(i) > 0 And pcUsed.Contains(i) = False Then
                    Dim rc = redContours.rcList(i)
                    dst3(rc.rect).SetTo(taskAlg.scalarColors(rc.index), rc.mask)
                    pcUsed.Add(i)
                End If
            Next
        End Sub
    End Class






    Public Class XO_RedPrep_FloodFill : Inherits TaskParent
        Public classCount As Integer
        Public rectList As New List(Of cv.Rect)
        Public identifyCount As Integer = 255
        Public Sub New()
            cPtr = RedCloud_Open()
            desc = "Run the C++ RedCloud to create a list of mask, rect, and other info about image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim inputData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            Dim imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
            handleInput.Free()
            dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone

            classCount = Math.Min(RedCloud_Count(cPtr), identifyCount * 2)
            If classCount = 0 Then Exit Sub ' no data to process.

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))

            Dim rects(classCount * 4) As Integer
            Marshal.Copy(rectData.Data, rects, 0, rects.Length)

            rectList.Clear()
            For i = 0 To classCount * 4 - 4 Step 4
                rectList.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
            Next

            If standalone Then dst3 = PaletteFull(dst2)

            If taskAlg.heartBeat Then labels(2) = "CV_8U result With " + CStr(classCount) + " regions."
            If taskAlg.heartBeat Then labels(3) = "Palette version Of the data In dst2 With " + CStr(classCount) + " regions."
        End Sub
        Public Sub Close()
            If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
        End Sub
    End Class







    Public Class XO_Contour_Outline : Inherits TaskParent
        Public rc As New oldrcData
        Public Sub New()
            desc = "Create a simplified contour of the selected cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))
            Dim ptList As List(Of cv.Point) = rc.contour

            dst3.SetTo(0)

            Dim newContour As New List(Of cv.Point)
            rc = taskAlg.oldrcD
            If rc.contour.Count = 0 Then Exit Sub
            Dim p1 As cv.Point, p2 As cv.Point
            newContour.Add(p1)
            For i = 0 To rc.contour.Count - 2
                p1 = rc.contour(i)
                p2 = rc.contour(i + 1)
                dst3(rc.rect).Line(p1, p2, white, taskAlg.lineWidth + 1)
                newContour.Add(p2)
            Next
            rc.contour = New List(Of cv.Point)(newContour)
            dst3(rc.rect).Line(rc.contour(rc.contour.Count - 1), rc.contour(0), white, taskAlg.lineWidth + 1)

            labels(2) = "Input points = " + CStr(rc.contour.Count)
        End Sub
    End Class





    Public Class XO_RedCloud_MotionTest : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public percentImage As Single
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Build contours for each cell"
        End Sub
        Public Function motionDisplayCell() As rcData
            Dim clickIndex = rcMap.Get(Of Byte)(taskAlg.ClickPoint.Y, taskAlg.ClickPoint.X) - 1
            If clickIndex >= 0 Then
                Return rcList(clickIndex)
            End If
            Return Nothing
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = runRedCloud(src, labels(3))
            labels(2) = taskAlg.redCloud.labels(2) + If(standalone, "  Age of each cell is displayed as well.", "")

            Static rcListLast = New List(Of rcData)(rcList)
            Static rcMapLast As cv.Mat = rcMap.Clone

            rcList.Clear()
            Dim r2 As cv.Rect
            rcMap.SetTo(0)
            dst2.SetTo(0)
            Dim unchangedCount As Integer
            For Each rc In taskAlg.redCloud.rcList
                Dim r1 = rc.rect
                r2 = New cv.Rect(0, 0, 1, 1) ' fake rect for conditional below...
                Dim indexLast = rcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X) - 1
                If indexLast > 0 Then r2 = rcListLast(indexLast).rect
                If indexLast >= 0 And r1.IntersectsWith(r2) And taskAlg.optionsChanged = False Then
                    If rc.rect.Contains(rcListLast(indexLast).maxdist) Then
                        rc = rcListLast(indexLast)
                        unchangedCount += 1
                    End If

                    rc.age = rcListLast(indexLast).age + 1
                    If rc.age > 1000 Then rc.age = 2
                End If
                rc.index = rcList.Count + 1
                rcMap(rc.rect).SetTo(rc.index, rc.mask)
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                dst2.Circle(rc.maxDist, taskAlg.DotSize, taskAlg.highlight, -1)
                SetTrueText(CStr(rc.age), rc.maxDist)
                rcList.Add(rc)
            Next

            RedCloud_Cell.selectCell(taskAlg.redCloud.rcMap, taskAlg.redCloud.rcList)
            If taskAlg.rcD IsNot Nothing Then strOut = taskAlg.rcD.displayCell + vbCrLf + vbCrLf +
                    Format(percentImage, "0.0%") + " of image" + vbCrLf +
                    CStr(rcList.Count) + " cells present"

            SetTrueText(strOut, 1)

            rcListLast = New List(Of rcData)(rcList)
            rcMapLast = rcMap.Clone
        End Sub
    End Class




    Public Class XO_RedCloud_MotionCells : Inherits TaskParent
        Public Sub New()
            taskAlg.gOptions.HistBinBar.Maximum = 255
            taskAlg.gOptions.HistBinBar.Value = 255
            desc = "Use motion to identify which cells changed."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(src, labels(2))
            dst1 = taskAlg.redCloud.dst1

            dst3.SetTo(0)
            Dim count As Integer
            For Each rc In taskAlg.redCloud.rcList
                If rc.age > 10 Then
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                    count += 1
                Else
                    dst3(rc.rect).SetTo(white, rc.mask)
                End If
                dst3.Circle(rc.maxDist, taskAlg.DotSize, taskAlg.highlight, -1)
                SetTrueText(CStr(rc.age), rc.maxDist)
            Next
            labels(3) = CStr(count) + " cells had no RGB motion... white cells had motion."
        End Sub
    End Class




    Public Class XO_RedCloud_HeartBeat : Inherits TaskParent
        Dim redCore As New RedCloud_Basics
        Public rcList As New List(Of rcData)
        Public percentImage As Single
        Public rcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public prepEdges As New RedPrep_Basics
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            redCore.redSweep.prepEdges = prepEdges
            desc = "Run RedCloud_Map on the heartbeat but just floodFill at maxDist otherwise."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.heartBeat Or taskAlg.optionsChanged Then
                redCore.Run(src)
                dst2 = redCore.dst2
                labels(2) = redCore.labels(2)
                rcList = New List(Of rcData)(redCore.rcList)
                dst3 = redCore.dst2
                dst1 = redCore.redSweep.prepEdges.dst2
                labels(3) = redCore.labels(2)
            Else
                Dim rcListLast = New List(Of rcData)(redCore.rcList)

                prepEdges.Run(src)
                dst1 = prepEdges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

                Dim index As Integer = 1
                Dim rect As New cv.Rect
                Dim maskRect = New cv.Rect(1, 1, dst1.Width, dst1.Height)
                Dim mask = New cv.Mat(New cv.Size(dst1.Width + 2, dst1.Height + 2), cv.MatType.CV_8U, 0)
                Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
                Dim minCount = dst1.Total * 0.001
                rcList.Clear()
                rcMap.SetTo(0)
                For Each rc In rcListLast
                    Dim pt = rc.maxDist
                    If rcMap.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                        Dim count = cv.Cv2.FloodFill(dst1, mask, pt, index, rect, 0, 0, flags)
                        If rect.Width > 0 And rect.Height > 0 And rect.Width < dst2.Width And rect.Height < dst2.Height Then
                            Dim pcc = New rcData(dst1(rect), rect, index)
                            If pcc.index >= 0 Then
                                pcc.index = index
                                pcc.color = rc.color
                                pcc.age = rc.age + 1
                                rcList.Add(pcc)
                                rcMap(pcc.rect).SetTo(pcc.index Mod 255, pcc.mask)

                                index += 1
                            End If
                        End If
                    End If
                Next

                dst2 = PaletteBlackZero(rcMap)
                labels(2) = CStr(rcList.Count) + " regions were identified "
            End If

            RedCloud_Cell.selectCell(rcMap, rcList)
            If taskAlg.rcD IsNot Nothing Then
                strOut = taskAlg.rcD.displayCell + vbCrLf + vbCrLf + Format(percentImage, "0.0%") + " of image" + vbCrLf + CStr(rcList.Count) + " cells present"
                taskAlg.color(taskAlg.rcD.rect).SetTo(white, taskAlg.rcD.mask)
            End If
            SetTrueText(strOut, 1)
        End Sub
    End Class





    Public Class XO_RedCell_Color : Inherits TaskParent
        Public mdList As New List(Of maskData)
        Public Sub New()
            desc = "Generate the RedColor cells from the rects, mask, and pixel counts."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                SetTrueText("RedCell_Color is run by numerous algorithms but generates no output when standalone. ", 2)
                Exit Sub
            End If
            If taskAlg.redList Is Nothing Then taskAlg.redList = New XO_RedList_Basics

            Dim initialList As New List(Of oldrcData)
            For i = 0 To mdList.Count - 1
                Dim rc As New oldrcData
                rc.rect = mdList(i).rect
                If rc.rect.Size = dst2.Size Then Continue For ' RedList_Basics can find a cell this big.  
                rc.mask = mdList(i).mask
                rc.maxDist = mdList(i).maxDist
                rc.maxDStable = rc.maxDist
                rc.indexLast = taskAlg.redList.rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                rc.contour = mdList(i).contour
                DrawTour(rc.mask, rc.contour, 255, -1)
                rc.pixels = mdList(i).mask.CountNonZero
                If rc.indexLast >= taskAlg.redList.oldrclist.Count Then rc.indexLast = 0
                If rc.indexLast > 0 Then
                    Dim lrc = taskAlg.redList.oldrclist(rc.indexLast)
                    rc.age = lrc.age + 1
                    rc.depth = lrc.depth
                    rc.depthPixels = lrc.depthPixels
                    rc.mmX = lrc.mmX
                    rc.mmY = lrc.mmY
                    rc.mmZ = lrc.mmZ
                    rc.maxDStable = lrc.maxDStable

                    If rc.pixels < dst2.Total * 0.001 Then
                        rc.color = yellow
                    Else
                        ' verify that the maxDStable is still good.
                        Dim v1 = taskAlg.redList.rcMap.Get(Of Byte)(rc.maxDStable.Y, rc.maxDStable.X)
                        If v1 <> lrc.index Then
                            rc.maxDStable = rc.maxDist

                            rc.age = 1 ' a new cell was found that was probably part of another in the previous frame.
                        End If
                    End If
                Else
                    rc.age = 1
                End If

                Dim brickIndex = taskAlg.gridMap.Get(Of Integer)(rc.maxDStable.Y, rc.maxDStable.X)
                rc.color = taskAlg.scalarColors(brickIndex Mod 255)
                initialList.Add(rc)
            Next

            Dim sortedCells As New SortedList(Of Integer, oldrcData)(New compareAllowIdenticalIntegerInverted)

            Dim rcNewCount As Integer
            Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
            For Each rc In initialList
                rc.pixels = rc.mask.CountNonZero
                If rc.pixels = 0 Then Continue For

                Dim depthMask = rc.mask.Clone
                depthMask.SetTo(0, taskAlg.noDepthMask(rc.rect))
                Dim depthPixels = depthMask.CountNonZero

                If depthPixels / rc.pixels > 0.1 Then
                    rc.mmX = GetMinMax(taskAlg.pcSplit(0)(rc.rect), depthMask)
                    rc.mmY = GetMinMax(taskAlg.pcSplit(1)(rc.rect), depthMask)
                    rc.mmZ = GetMinMax(taskAlg.pcSplit(2)(rc.rect), depthMask)

                    cv.Cv2.MeanStdDev(taskAlg.pointCloud(rc.rect), depthMean, depthStdev, depthMask)
                    rc.depth = depthMean(2)
                    If Single.IsNaN(rc.depth) Or rc.depth < 0 Then rc.depth = 0
                End If

                If rc.age = 1 Then rcNewCount += 1
                sortedCells.Add(rc.pixels, rc)
            Next

            If taskAlg.heartBeat Then
                labels(2) = CStr(taskAlg.redList.oldrclist.Count) + " total cells (shown with '" + taskAlg.gOptions.trackingLabel + "' and " +
                        CStr(taskAlg.redList.oldrclist.Count - rcNewCount) + " matched to previous frame"
            End If

            dst2 = RebuildRCMap(sortedCells.Values.ToList.ToList)
        End Sub
    End Class





    Public Class XO_RedCell_Color1 : Inherits TaskParent
        Public mdList As New List(Of maskData)
        Public Sub New()
            desc = "Generate the RedColor cells from the rects, mask, and pixel counts."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                SetTrueText("RedCell_Color is run by numerous algorithms but generates no output when standalone. ", 2)
                Exit Sub
            End If
            If taskAlg.redList Is Nothing Then taskAlg.redList = New XO_RedList_Basics

            Dim initialList As New List(Of oldrcData)
            For i = 0 To mdList.Count - 1
                Dim rc As New oldrcData
                rc.rect = mdList(i).rect
                If rc.rect.Size = dst2.Size Then Continue For ' RedList_Basics can find a cell this big.  
                rc.mask = mdList(i).mask
                rc.maxDist = mdList(i).maxDist
                rc.maxDStable = rc.maxDist
                rc.indexLast = taskAlg.redList.rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                rc.contour = mdList(i).contour
                DrawTour(rc.mask, rc.contour, 255, -1)
                rc.pixels = mdList(i).mask.CountNonZero
                If rc.indexLast >= taskAlg.redList.oldrclist.Count Then rc.indexLast = 0
                If rc.indexLast > 0 Then
                    Dim lrc = taskAlg.redList.oldrclist(rc.indexLast)
                    rc.age = lrc.age + 1
                    rc.depth = lrc.depth
                    rc.depthPixels = lrc.depthPixels
                    rc.mmX = lrc.mmX
                    rc.mmY = lrc.mmY
                    rc.mmZ = lrc.mmZ
                    rc.maxDStable = lrc.maxDStable

                    If rc.pixels < dst2.Total * 0.001 Then
                        rc.color = yellow
                    Else
                        ' verify that the maxDStable is still good.
                        Dim v1 = taskAlg.redList.rcMap.Get(Of Byte)(rc.maxDStable.Y, rc.maxDStable.X)
                        If v1 <> lrc.index Then
                            rc.maxDStable = rc.maxDist

                            rc.age = 1 ' a new cell was found that was probably part of another in the previous frame.
                        End If
                    End If
                Else
                    rc.age = 1
                End If

                Dim brickIndex = taskAlg.gridMap.Get(Of Integer)(rc.maxDStable.Y, rc.maxDStable.X)
                rc.color = taskAlg.scalarColors(brickIndex Mod 255)
                initialList.Add(rc)
            Next

            Dim sortedCells As New SortedList(Of Integer, oldrcData)(New compareAllowIdenticalIntegerInverted)

            Dim rcNewCount As Integer
            Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
            For Each rc In initialList
                rc.pixels = rc.mask.CountNonZero
                If rc.pixels = 0 Then Continue For

                Dim depthMask = rc.mask.Clone
                depthMask.SetTo(0, taskAlg.noDepthMask(rc.rect))
                Dim depthPixels = depthMask.CountNonZero

                If depthPixels / rc.pixels > 0.1 Then
                    rc.mmX = GetMinMax(taskAlg.pcSplit(0)(rc.rect), depthMask)
                    rc.mmY = GetMinMax(taskAlg.pcSplit(1)(rc.rect), depthMask)
                    rc.mmZ = GetMinMax(taskAlg.pcSplit(2)(rc.rect), depthMask)

                    cv.Cv2.MeanStdDev(taskAlg.pointCloud(rc.rect), depthMean, depthStdev, depthMask)
                    rc.depth = depthMean(2)
                    If Single.IsNaN(rc.depth) Or rc.depth < 0 Then rc.depth = 0
                End If

                If rc.age = 1 Then rcNewCount += 1
                sortedCells.Add(rc.pixels, rc)
            Next

            If taskAlg.heartBeat Then
                labels(2) = CStr(taskAlg.redList.oldrclist.Count) + " total cells (shown with '" + taskAlg.gOptions.trackingLabel + "' and " +
                        CStr(taskAlg.redList.oldrclist.Count - rcNewCount) + " matched to previous frame"
            End If
            dst2 = RebuildRCMap(sortedCells.Values.ToList)
        End Sub
    End Class





    Public Class XO_RedCC_Basics : Inherits TaskParent
        Public Sub New()
            desc = "Show the image segmentation for both the point cloud and the color image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32F Then src = taskAlg.pointCloud
            dst2 = runRedCloud(src, labels(2))
            dst3 = runRedColor(src, labels(3))

            If standaloneTest() Then
                For Each rc In taskAlg.redCloud.rcList
                    dst2.Circle(rc.maxDist, taskAlg.DotSize, taskAlg.highlight, -1)
                    SetTrueText(CStr(rc.age), rc.maxDist)
                Next

                For Each rc In taskAlg.redColor.rcList
                    dst3.Circle(rc.maxDist, taskAlg.DotSize, taskAlg.highlight, -1)
                    SetTrueText(CStr(rc.age), rc.maxDist, 3)
                Next
            End If

            Static picTag As Integer
            If taskAlg.mouseClickFlag Then picTag = taskAlg.mousePicTag
            If picTag = 2 Then
                RedCloud_Cell.selectCell(taskAlg.redCloud.rcMap, taskAlg.redCloud.rcList)
                If taskAlg.rcD IsNot Nothing Then dst3(taskAlg.rcD.rect).SetTo(white, taskAlg.rcD.mask)
            Else
                RedCloud_Cell.selectCell(taskAlg.redColor.rcMap, taskAlg.redColor.rcList)
                If taskAlg.rcD IsNot Nothing Then dst2(taskAlg.rcD.rect).SetTo(white, taskAlg.rcD.mask)
            End If
        End Sub
    End Class






    Public Class XO_Bin3Way_RedCloudNew : Inherits TaskParent
        Dim bin3 As New Bin3Way_KMeans
        Dim flood As New Flood_BasicsMask
        Dim color8U As New Color8U_Basics
        Dim cellMaps(2) As cv.Mat, rclist(2) As List(Of rcData)
        Dim options As New Options_Bin3WayRedCloud
        Public Sub New()
            desc = "Identify the lightest, darkest, and 'Other' regions separately and then combine the oldrcData."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            dst3 = runRedColor(src, labels(3))

            If taskAlg.optionsChanged Then
                For i = 0 To rclist.Count - 1
                    rclist(i) = New List(Of rcData)
                    cellMaps(i) = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
                Next
            End If

            bin3.Run(src)

            For i = options.startRegion To options.endRegion
                taskAlg.redColor.rcMap = cellMaps(i)
                taskAlg.redColor.rcList = rclist(i)
                If i = 2 Then
                    flood.inputRemoved = bin3.bin3.mats.mat(0) Or bin3.bin3.mats.mat(1)
                    color8U.Run(src)
                    flood.Run(color8U.dst2)
                Else
                    flood.inputRemoved = Not bin3.bin3.mats.mat(i)
                    flood.Run(bin3.bin3.mats.mat(i))
                End If
                cellMaps(i) = taskAlg.redColor.rcMap.Clone
                rclist(i) = New List(Of rcData)(taskAlg.redColor.rcList)
            Next

            Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            For i = 0 To 2
                For Each rc In rclist(i)
                    sortedCells.Add(rc.pixels, rc)
                Next
            Next

            'dst2 = RebuildRCMap(sortedCells)

            If taskAlg.heartBeat Then labels(2) = CStr(taskAlg.redColor.rcList.Count) + " cells were identified and matched to the previous image"
        End Sub
    End Class





    Public Class XO_Bin3Way_RedCloud : Inherits TaskParent
        Dim bin3 As New Bin3Way_KMeans
        Dim flood As New Flood_BasicsMask
        Dim cellMaps(2) As cv.Mat, oldrclist(2) As List(Of oldrcData)
        Dim options As New Options_Bin3WayRedCloud
        Public Sub New()
            flood.showSelected = False
            desc = "Identify the lightest, darkest, and other regions separately and then combine the oldrcData."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            dst3 = runRedList(src, labels(3))

            If taskAlg.optionsChanged Then
                For i = 0 To oldrclist.Count - 1
                    oldrclist(i) = New List(Of oldrcData)
                    cellMaps(i) = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
                Next
            End If

            bin3.Run(src)

            Dim sortedCells As New SortedList(Of Integer, oldrcData)(New compareAllowIdenticalIntegerInverted)
            For i = options.startRegion To options.endRegion
                taskAlg.redList.rcMap = cellMaps(i)
                taskAlg.redList.oldrclist = oldrclist(i)
                flood.inputRemoved = Not bin3.bin3.mats.mat(i)
                flood.Run(bin3.bin3.mats.mat(i))
                cellMaps(i) = taskAlg.redList.rcMap.Clone
                oldrclist(i) = New List(Of oldrcData)(taskAlg.redList.oldrclist)
                For Each rc In oldrclist(i)
                    If rc.index = 0 Then Continue For
                    sortedCells.Add(rc.pixels, rc)
                Next
            Next

            dst2 = RebuildRCMap(sortedCells.Values.ToList)

            If taskAlg.heartBeat Then labels(2) = CStr(taskAlg.redList.oldrclist.Count) + " cells were identified and matched to the previous image"
        End Sub
    End Class





    Public Class XO_Flood_BasicsMask : Inherits TaskParent
        Public inputRemoved As cv.Mat
        Public cellGen As New XO_RedCell_Color
        Dim redMask As New RedMask_Basics
        Public buildinputRemoved As Boolean
        Public showSelected As Boolean = True
        Dim color8U As New Color8U_Basics
        Public Sub New()
            labels(3) = "The inputRemoved mask is used to limit how much of the image is processed."
            desc = "Floodfill by color as usual but this is run repeatedly with the different tiers."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Or buildinputRemoved Then
                color8U.Run(src)
                inputRemoved = taskAlg.pcSplit(2).InRange(taskAlg.MaxZmeters, taskAlg.MaxZmeters).ConvertScaleAbs()
                src = color8U.dst2
            End If

            dst3 = inputRemoved
            If inputRemoved IsNot Nothing Then src.SetTo(0, inputRemoved)
            redMask.Run(src)

            cellGen.mdList = redMask.mdList
            cellGen.Run(redMask.dst2)

            dst2 = cellGen.dst2

            If taskAlg.heartBeat Then labels(2) = $"{taskAlg.redList.oldrclist.Count} cells identified"

            If showSelected Then XO_RedList_Basics.setSelectedCell()
        End Sub
    End Class





    Public Class XO_Density_Basics : Inherits TaskParent
        Public Sub New()
            cPtr = Density_2D_Open()
            desc = "Isolate points in 3D using the distance to the 8 neighboring points in the pointcloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32F Then src = taskAlg.pcSplit(2)

            Dim cppData(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, cppData, 0, cppData.Length)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = Density_2D_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
            handleSrc.Free()

            dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
        End Sub
        Public Sub Close()
            Density_2D_Close(cPtr)
        End Sub
    End Class





    Public Class XO_Density_Phase : Inherits TaskParent
        Dim dense As New XO_Density_Basics
        Dim gradient As New Gradient_PhaseDepth
        Public Sub New()
            desc = "Display gradient phase and 2D density side by side."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            gradient.Run(src)
            dst3 = Mat_Convert.Mat_32f_To_8UC3(gradient.dst3)

            dense.Run(src)
            dst2 = dense.dst2
        End Sub
    End Class







    Public Class XO_Density_Count_CPP : Inherits TaskParent
        Public Sub New()
            cPtr = Density_Count_Open()
            desc = "Isolate points in 3D by counting 8 neighboring Z points in the pointcloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32F Then src = taskAlg.pcSplit(2)

            Dim cppData(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, cppData, 0, cppData.Length)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = Density_Count_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
            handleSrc.Free()

            dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
        End Sub
        Public Sub Close()
            Density_Count_Close(cPtr)
        End Sub
    End Class







    Public Class XO_Density_Mask : Inherits TaskParent
        Public pointList As New List(Of cv.Point)
        Public Sub New()
            desc = "Measure a mask's size in any image and track the biggest regions."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() <> 1 Then src = taskAlg.gray
            src.SetTo(0, taskAlg.noDepthMask)

            Dim threshold = taskAlg.brickSize * taskAlg.brickSize / 2
            Dim activeList(taskAlg.gridRects.Count - 1) As Boolean
            dst3.SetTo(0)
            Parallel.For(0, taskAlg.gridRects.Count,
             Sub(i)
                 Dim roi = taskAlg.gridRects(i)
                 Dim count = src(roi).CountNonZero
                 If count > threshold Then
                     dst3(roi).SetTo(white)
                     activeList(i) = True
                 End If
             End Sub)

            pointList.Clear()

            For i = 0 To activeList.Count - 1
                If activeList(i) Then
                    Dim roi = taskAlg.gridRects(i)
                    pointList.Add(New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2))
                End If
            Next
        End Sub
    End Class






    Public Class XO_Edge_MotionOverlay : Inherits TaskParent
        Dim options As New Options_EdgeOverlay
        Dim options1 As New Options_Diff
        Public Sub New()
            labels(3) = "AbsDiff output of offset with original"
            desc = "Find edges by displacing the current BGR image in any direction and diff it with the original."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            options1.Run()

            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Static offsetImage As cv.Mat = src.Clone
            Dim rect1 = New cv.Rect(options.xDisp, options.yDisp, dst2.Width - options.xDisp - 1, dst2.Height - options.yDisp - 1)
            Dim rect2 = New cv.Rect(0, 0, dst2.Width - options.xDisp - 1, dst2.Height - options.yDisp - 1)
            offsetImage(rect2) = src(rect1).Clone

            cv.Cv2.Absdiff(src, offsetImage, dst0)
            dst2 = dst0.Threshold(options1.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            labels(2) = "Src offset (x,y) = (" + CStr(options.xDisp) + "," + CStr(options.yDisp) + ")"
        End Sub
    End Class





    Public Class XO_EdgeLine_BasicsNoMotion : Inherits TaskParent
        Public segments As New List(Of List(Of cv.Point))
        Public rectList As New List(Of cv.Rect)
        Public classCount As Integer
        Public Sub New()
            cPtr = EdgeLineRaw_Open()
            labels(3) = "Each line is highlighted with the color of the contour where it resides. "
            desc = "Use EdgeLines to find edges/lines but without using motionMask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Dim cppData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, cppData, 0, cppData.Length)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imageEdgeWidth = 2
            Dim imagePtr = EdgeLineRaw_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, imageEdgeWidth)
            handleSrc.Free()
            If imagePtr <> 0 Then dst1 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32S, imagePtr)
            dst1.ConvertTo(dst2, cv.MatType.CV_8U)

            If dst2.Width >= 1280 Then imageEdgeWidth = 4
            dst2.Rectangle(New cv.Rect(0, 0, dst2.Width - 1, dst2.Height - 1), 255, imageEdgeWidth) ' prevent leaks at the image boundary...

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, EdgeLineRaw_Rects(cPtr))

            classCount = EdgeLineRaw_GetSegCount(cPtr)
            If classCount = 0 Then Exit Sub ' nothing to work with....
            Dim rects(classCount * 4) As Integer
            Marshal.Copy(rectData.Data, rects, 0, rects.Length)

            rectList.Clear()
            For i = 0 To classCount * 4 - 4 Step 4
                rectList.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
            Next

            segments.Clear()
            Dim pointCount As Integer
            For i = 0 To classCount - 1
                Dim len = EdgeLineRaw_NextLength(cPtr)
                If len < 2 Then Continue For
                Dim nextSeg(len * 2 - 1) As Integer
                Dim segPtr = EdgeLineRaw_NextSegment(cPtr)
                Marshal.Copy(segPtr, nextSeg, 0, nextSeg.Length)

                Dim segment As New List(Of cv.Point)
                For j = 0 To nextSeg.Length - 2 Step 2
                    segment.Add(New cv.Point(nextSeg(j), nextSeg(j + 1)))
                    pointCount += 1
                Next
                segments.Add(segment)
            Next
            labels(2) = CStr(classCount) + " segments were found using " + CStr(pointCount) + " points."

            dst3.SetTo(0)
        End Sub
        Public Sub Close()
            EdgeLineRaw_Close(cPtr)
        End Sub
    End Class





    Public Class XO_EdgeLine_Basics : Inherits TaskParent
        Public segments As New List(Of List(Of cv.Point))
        Public classCount As Integer
        Public Sub New()
            cPtr = EdgeLineRaw_Open()
            If standalone Then taskAlg.gOptions.showMotionMask.Checked = True
            desc = "Use EdgeLines to find edges/lines but without using motionMask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = taskAlg.grayStable

            Dim cppData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, cppData, 0, cppData.Length)
            Dim handlesrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = EdgeLineRaw_RunCPP(cPtr, handlesrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                          taskAlg.lineWidth)
            handlesrc.Free()
            dst1 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32S, imagePtr)
            dst1.ConvertTo(dst2, cv.MatType.CV_8U)

            Dim imageEdgeWidth = If(dst2.Width >= 1280, 4, 2)
            ' prevent leaks at the image boundary...
            dst2.Rectangle(New cv.Rect(0, 0, dst2.Width - 1, dst2.Height - 1), 255, imageEdgeWidth)

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, EdgeLineRaw_Rects(cPtr))

            classCount = EdgeLineRaw_GetSegCount(cPtr)
            If classCount = 0 Then Exit Sub ' nothing to work with....
            Dim rects(classCount * 4) As Integer
            Marshal.Copy(rectData.Data, rects, 0, rects.Length)

            Dim rectList As New List(Of cv.Rect)
            rectList.Clear()
            For i = 0 To classCount * 4 - 4 Step 4
                rectList.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
            Next

            segments.Clear()
            Dim pointCount As Integer
            For i = 0 To classCount - 1
                Dim len = EdgeLineRaw_NextLength(cPtr)
                If len < 2 Then Continue For
                Dim nextSeg(len * 2 - 1) As Integer
                Dim segPtr = EdgeLineRaw_NextSegment(cPtr)
                Marshal.Copy(segPtr, nextSeg, 0, nextSeg.Length)

                Dim segment As New List(Of cv.Point)
                For j = 0 To nextSeg.Length - 2 Step 2
                    segment.Add(New cv.Point(nextSeg(j), nextSeg(j + 1)))
                    pointCount += 1
                Next
                segments.Add(segment)
            Next
            labels(2) = CStr(classCount) + " segments were found using " + CStr(pointCount) + " points. " +
                    CStr(taskAlg.toggleOn)

            dst3.SetTo(0)
        End Sub
        Public Sub Close()
            EdgeLineRaw_Close(cPtr)
        End Sub
    End Class




    Public Class XO_Contour_Basics_List : Inherits TaskParent
        Public contourList As New List(Of contourData)
        Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        Public sortContours As New Contour_SortNew
        Public options As New Options_Contours
        Public Sub New()
            labels(3) = "Details for the selected contour."
            taskAlg.featureOptions.Color8USource.SelectedItem = "EdgeLine_Basics"
            desc = "List retrieval mode contour finder"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst3 = Mat_Basics.srcMustBe8U(src)

            sortContours.allContours = Contour_Basics.buildContours(dst3)
            sortContours.Run(src)

            contourList = sortContours.rcList
            contourMap = sortContours.rcMap
            labels(2) = sortContours.labels(2)
            dst2 = sortContours.dst2
            strOut = sortContours.strOut
        End Sub
    End Class






    Public Class XO_Contour_Basics_CComp : Inherits TaskParent
        Public options As New Options_Contours
        Public contourList As New List(Of contourData)
        Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        Dim sortContours As New Contour_Sort
        Public Sub New()
            OptionParent.findRadio("CComp").Checked = True
            taskAlg.featureOptions.Color8USource.SelectedItem = "EdgeLine_Basics"
            desc = "CComp retrieval mode contour finder"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst3 = Mat_Basics.srcMustBe8U(src)

            sortContours.allContours = Contour_Basics.buildContours(dst3)
            sortContours.Run(dst3)

            contourList = sortContours.contourList
            contourMap = sortContours.contourMap
            labels(2) = sortContours.labels(2)
            dst2 = sortContours.dst2
        End Sub
    End Class





    Public Class XO_Contour_Basics_FloodFill : Inherits TaskParent
        Public options As New Options_Contours
        Public contourList As New List(Of contourData)
        Public contourMap As New cv.Mat(taskAlg.workRes, cv.MatType.CV_32F, 0)
        Dim sortContours As New Contour_Sort
        Public Sub New()
            desc = "FloodFill retrieval mode contour finder"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst3 = Mat_Basics.srcMustBe8U(src)

            Dim mode = options.options2.ApproximationMode
            dst3.ConvertTo(dst1, cv.MatType.CV_32SC1)
            cv.Cv2.FindContours(dst1, sortContours.allContours, Nothing, cv.RetrievalModes.FloodFill, mode)
            If sortContours.allContours.Count <= 1 Then Exit Sub

            sortContours.Run(src)

            contourList = sortContours.contourList
            contourMap = sortContours.contourMap
            labels(2) = sortContours.labels(2)
            dst2 = sortContours.dst2
        End Sub
    End Class







    Public Class XO_Contour_Basics_External : Inherits TaskParent
        Public options As New Options_Contours
        Public contourList As New List(Of contourData)
        Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        Dim sortContours As New Contour_Sort
        Public Sub New()
            desc = "External retrieval mode contour finder"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst1 = Mat_Basics.srcMustBe8U(src)

            Dim mode = options.options2.ApproximationMode
            cv.Cv2.FindContours(dst1, sortContours.allContours, Nothing, cv.RetrievalModes.List, mode)
            If sortContours.allContours.Count <= 1 Then Exit Sub

            sortContours.Run(src)

            contourList = sortContours.contourList
            contourMap = sortContours.contourMap
            labels(2) = sortContours.labels(2)
            dst2 = sortContours.dst2
        End Sub
    End Class






    Public Class XO_Contour_Basics_Tree : Inherits TaskParent
        Public options As New Options_Contours
        Public contourList As New List(Of contourData)
        Public contourMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        Dim sortContours As New Contour_Sort
        Public Sub New()
            desc = "Tree retrieval mode contour finder"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst1 = Mat_Basics.srcMustBe8U(src)

            Dim mode = options.options2.ApproximationMode
            If dst1.Type <> cv.MatType.CV_8U Then dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            cv.Cv2.FindContours(dst1, sortContours.allContours, Nothing, cv.RetrievalModes.Tree, mode)
            If sortContours.allContours.Count <= 1 Then Exit Sub

            sortContours.Run(src)

            contourList = sortContours.contourList
            contourMap = sortContours.contourMap
            labels(2) = sortContours.labels(2)
            dst2 = sortContours.dst2
        End Sub
    End Class






    Public Class XO_Contour_SortTest : Inherits TaskParent
        Public Sub New()
            If taskAlg.contours Is Nothing Then taskAlg.contours = New Contour_Basics_List
            desc = "Test the contour sort (by size) algorithm nearby. Contour_Sort standalone does nothing."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            taskAlg.contours.Run(src)
            dst2 = taskAlg.contours.dst2
            labels = taskAlg.contours.labels
            SetTrueText(taskAlg.contours.strOut, 3)
        End Sub
    End Class






    Public Class XO_Motion_FromEdgeColorize : Inherits TaskParent
        Dim cAccum As New Edge_CannyAccum
        Public Sub New()
            labels = {"", "", "Canny edges accumulated", "Colorized version of dst2 - blue indicates motion."}
            desc = "Colorize the output of Edge_CannyAccum to show values off the peak value which indicate motion."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            cAccum.Run(src)
            dst2 = cAccum.dst2
            dst3 = PaletteFull(dst2)
        End Sub
    End Class





    Public Class XO_Motion_BasicsOld : Inherits TaskParent
        Public mCore As New XO_Motion_CoreOld
        Public Sub New()
            If standalone Then taskAlg.gOptions.showMotionMask.Checked = True
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
            labels(3) = "Updated taskAlg.motionRect"
            desc = "Use the motionlist of rects to create one motion rectangle."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.algorithmPrep = False Then Exit Sub

            mCore.Run(src)

            If taskAlg.heartBeat Then dst2 = taskAlg.gray

            dst3.SetTo(0)
            If mCore.motionList.Count > 0 Then
                taskAlg.motionRect = taskAlg.gridRects(mCore.motionList(0))
                For Each index In mCore.motionList
                    taskAlg.motionRect = taskAlg.motionRect.Union(taskAlg.gridRects(index))
                Next
                dst3(taskAlg.motionRect).SetTo(255)
                taskAlg.gray(taskAlg.motionRect).CopyTo(dst2(taskAlg.motionRect))
            Else
                taskAlg.motionRect = New cv.Rect
            End If

            labels(2) = CStr(mCore.motionList.Count) + " grid rect's or " +
                    Format(mCore.motionList.Count / taskAlg.gridRects.Count, "0.0%") +
                    " of bricks had motion."
        End Sub
    End Class





    Public Class XO_Motion_CoreOld : Inherits TaskParent
        Public motionList As New List(Of Integer)
        Dim diff As New Diff_Basics
        Public Sub New()
            If standalone Then taskAlg.gOptions.showMotionMask.Checked = True
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "The motion mask"
            desc = "Find all the grid rects that had motion since the last frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = taskAlg.gray
            If taskAlg.heartBeat Or taskAlg.optionsChanged Then dst2 = src.Clone

            diff.Run(src)

            motionList.Clear()
            For i = 0 To taskAlg.gridRects.Count - 1
                Dim diffCount = diff.dst2(taskAlg.gridRects(i)).CountNonZero
                If diffCount >= taskAlg.motionThreshold Then
                    For Each index In taskAlg.grid.gridNeighbors(i)
                        If motionList.Contains(index) = False Then motionList.Add(index)
                    Next
                End If
            Next

            dst3.SetTo(0)
            For Each index In motionList
                Dim rect = taskAlg.gridRects(index)
                src(rect).CopyTo(dst2(rect))
                dst3(rect).SetTo(255)
            Next

            taskAlg.motionMask = dst3.Clone
            labels(2) = CStr(motionList.Count) + " grid rects had motion."
        End Sub
    End Class









    Public Class XO_Line_Generations : Inherits TaskParent
        Dim knn As New KNN_Basics
        Dim match3 As New XO_Line_LeftRightMatch3
        Public lpOutput As New List(Of lpData)
        Public Sub New()
            desc = "Identify any lines in both the current and the previous frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            match3.Run(emptyMat)
            dst2 = match3.dst2
            labels(2) = match3.labels(2)

            If match3.lpOutput.Count = 0 Then Exit Sub ' nothing was matched...

            Static lplast As New List(Of lpData)(match3.lpOutput)

            knn.queries.Clear()
            For Each lp In match3.lpOutput
                Dim pt As New cv.Point(lp.p1GridIndex, lp.p2GridIndex)
                knn.queries.Add(pt)
            Next

            If taskAlg.firstPass Then knn.trainInput = New List(Of cv.Point2f)(knn.queries)

            knn.Run(emptyMat)

            If lplast.Count = 0 Then lplast = New List(Of lpData)(match3.lpOutput)

            lpOutput.Clear()
            For Each lp In match3.lpOutput
                Dim index = knn.result(lp.index, 0)
                If index >= match3.lpOutput.Count Then Continue For
                If index >= lplast.Count And lplast.Count > 0 Then Continue For
                Dim age As Integer = 1
                If Math.Abs(lplast(index).angle - match3.lpOutput(index).angle) < taskAlg.angleThreshold Then
                    Dim index1 = match3.lpOutput(index).p1GridIndex
                    Dim index2 = match3.lpOutput(index).p2GridIndex
                    If taskAlg.grid.gridNeighbors(index1).Contains(lplast(index).p1GridIndex) And
                    taskAlg.grid.gridNeighbors(index2).Contains(lplast(index).p2GridIndex) Then
                        age = lplast(index).age + 1
                    End If
                End If
                lp.age = age
                lpOutput.Add(lp)
                SetTrueText(CStr(lp.age), lp.ptCenter, 2)
                SetTrueText(CStr(lp.age), lp.ptCenter, 3)
            Next

            knn.trainInput = New List(Of cv.Point2f)(knn.queries)
            lplast = New List(Of lpData)(lpOutput)
        End Sub
    End Class




    Public Class XO_Line_TestAge : Inherits TaskParent
        Dim knnLine As New XO_Line_Generations
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Are there ever frames where no line is connected to a line on a previous frame?"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            knnLine.Run(src)
            dst2 = knnLine.dst2
            labels(2) = knnLine.labels(2)

            Dim matched As New List(Of Integer)
            For Each lp In knnLine.lpOutput
                If lp.age > 1 Then matched.Add(lp.index)
                SetTrueText(CStr(lp.age), lp.ptCenter, 3)
            Next

            Static strList As New List(Of String)
            Dim stepSize = 10
            If matched.Count > 0 Then
                strList.Add(CStr(matched.Count) + ", ")
            Else
                strList.Add("    ")
            End If
            If strList.Count > 200 Then
                For i = 0 To stepSize - 1
                    strList.RemoveAt(0)
                Next
            End If

            strOut = ""
            Dim missingCount As Integer
            For i = 0 To strList.Count - 1 Step stepSize
                For j = i To Math.Min(strList.Count, i + stepSize) - 1
                    If strList(j) = "    " Then missingCount += 1
                    strOut += vbTab + strList(j)
                Next
                strOut += vbCrLf
            Next
            SetTrueText(strOut, 1)
            SetTrueText("In the last 200 frames there were " + CStr(missingCount) +
                    " frames without a matched line to the previous frame.", 3)

            labels(3) = "Of the " + CStr(knnLine.lpOutput.Count) + " lines found " + CStr(matched.Count) +
                    " were matched to the previous frame"
        End Sub
    End Class






    Public Class XO_Line_Stabilize : Inherits TaskParent
        Dim knnLine As New XO_Line_Generations
        Dim stable As New Stable_Basics
        Public Sub New()
            desc = "Stabilize the image by identifying a line in both the current frame and the previous."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            knnLine.Run(src)
            If knnLine.lpOutput.Count = 0 Then Exit Sub
            labels(2) = knnLine.labels(2)

            If taskAlg.firstPass Then
                stable.lpLast = knnLine.lpOutput(0)
                stable.lp = stable.lpLast
            Else
                For Each stable.lp In knnLine.lpOutput
                    If stable.lp.age > 1 Then Exit For
                Next
            End If

            stable.Run(src)
            dst2 = stable.dst2
            vbc.DrawLine(dst2, stable.lp)
            SetTrueText("Age = " + CStr(stable.lp.age), stable.lp.ptCenter)

            stable.lpLast = stable.lp
        End Sub
    End Class






    Public Class XO_Motion_PointCloud_MotionRect : Inherits TaskParent
        Public originalPointcloud As cv.Mat
        Public Sub New()
            labels = {"", "", "Pointcloud updated only with motion Rect",
                  "Diff of camera depth And motion-updated depth (always different)"}
            desc = "Update the pointcloud only with the motion Rect.  Resync heartbeatLT."
        End Sub
        Public Shared Function checkNanInf(pc As cv.Mat) As cv.Mat
            ' these don't work because there are NaN's and Infinity's (both are often present)
            ' cv.Cv2.PatchNaNs(pc, 0.0) 
            ' Dim mask As New cv.Mat
            ' cv.Cv2.Compare(pc, pc, mask, cv.CmpType.EQ)

            Dim count As Integer
            Dim vec As New cv.Vec3f(0, 0, 0)
            ' The stereolabs camera has some weird -inf and inf values in the Y-plane 
            ' with and without gravity transform.  Probably my fault but just fix it here.
            For y = 0 To pc.Rows - 1
                For x = 0 To pc.Cols - 1
                    Dim val = pc.Get(Of cv.Vec3f)(y, x)
                    If Single.IsNaN(val(0)) Or Single.IsInfinity(val(0)) Then
                        pc.Set(Of cv.Vec3f)(y, x, vec)
                        count += 1
                    End If
                Next
            Next

            'Dim mean As cv.Scalar, stdev As cv.Scalar
            'cv.Cv2.MeanStdDev(originalPointcloud, mean, stdev)
            'Debug.WriteLine("Before Motion mean " + mean.ToString())

            Return pc
        End Function
        Public Sub preparePointcloud()
            If taskAlg.gOptions.gravityPointCloud.Checked Then
                '******* this is the gravity rotation *******
                taskAlg.gravityCloud = (taskAlg.pointCloud.Reshape(1,
                            taskAlg.rows * taskAlg.cols) * taskAlg.gMatrix).ToMat.Reshape(3, taskAlg.rows)
                taskAlg.pointCloud = taskAlg.gravityCloud
            End If

            taskAlg.pcSplit = taskAlg.pointCloud.Split

            If taskAlg.optionsChanged Then
                taskAlg.maxDepthMask = New cv.Mat(taskAlg.pcSplit(2).Size, cv.MatType.CV_8U, 0)
            End If
            If taskAlg.gOptions.TruncateDepth.Checked Then
                taskAlg.pcSplit(2) = taskAlg.pcSplit(2).Threshold(taskAlg.MaxZmeters,
                                                        taskAlg.MaxZmeters, cv.ThresholdTypes.Trunc)
                taskAlg.maxDepthMask = taskAlg.pcSplit(2).InRange(taskAlg.MaxZmeters,
                                                        taskAlg.MaxZmeters).ConvertScaleAbs()
                cv.Cv2.Merge(taskAlg.pcSplit, taskAlg.pointCloud)
            End If

            taskAlg.depthMask = taskAlg.pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            taskAlg.noDepthMask = Not taskAlg.depthMask

            If taskAlg.xRange <> taskAlg.xRangeDefault Or taskAlg.yRange <> taskAlg.yRangeDefault Then
                Dim xRatio = taskAlg.xRangeDefault / taskAlg.xRange
                Dim yRatio = taskAlg.yRangeDefault / taskAlg.yRange
                taskAlg.pcSplit(0) *= xRatio
                taskAlg.pcSplit(1) *= yRatio

                cv.Cv2.Merge(taskAlg.pcSplit, taskAlg.pointCloud)
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)

            If taskAlg.settings.cameraName = "StereoLabs ZED 2/2i" Then
                originalPointcloud = checkNanInf(taskAlg.pointCloud).Clone
            Else
                originalPointcloud = taskAlg.pointCloud.Clone ' save the original camera pointcloud.
            End If

            If taskAlg.optionsChanged Then
                If taskAlg.rangesCloud Is Nothing Then
                    Dim rx = New cv.Vec2f(-taskAlg.xRangeDefault, taskAlg.xRangeDefault)
                    Dim ry = New cv.Vec2f(-taskAlg.yRangeDefault, taskAlg.yRangeDefault)
                    Dim rz = New cv.Vec2f(0, taskAlg.MaxZmeters)
                    taskAlg.rangesCloud = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1),
                                                    New cv.Rangef(ry.Item0, ry.Item1),
                                                    New cv.Rangef(rz.Item0, rz.Item1)}
                End If
            End If

            If taskAlg.gOptions.UseMotionMask.Checked Then
                If taskAlg.heartBeatLT Or taskAlg.frameCount < 5 Or taskAlg.optionsChanged Then
                    dst2 = taskAlg.pointCloud.Clone
                End If

                If taskAlg.motionRect.Width = 0 And taskAlg.optionsChanged = False Then
                    taskAlg.pointCloud = dst2
                    Exit Sub ' nothing changed...
                End If
                taskAlg.pointCloud(taskAlg.motionRect).CopyTo(dst2(taskAlg.motionRect))
                taskAlg.pointCloud = dst2
            End If

            ' this will move the motion-updated pointcloud into production.
            preparePointcloud()

            If standaloneTest() Then
                Static diff As New Diff_Depth32f
                Dim split = originalPointcloud.Split()
                diff.lastDepth32f = split(2)
                diff.Run(taskAlg.pcSplit(2))
                dst3 = diff.dst2
                dst3.Rectangle(taskAlg.motionRect, white, taskAlg.lineWidth)
            End If
        End Sub
    End Class




    Public Class XO_Motion_CoreAccum : Inherits TaskParent
        Public motionList As New List(Of Integer)
        Dim diff As New Diff_Basics
        Dim motionLists As New List(Of List(Of Integer))
        Public Sub New()
            If standalone Then taskAlg.gOptions.showMotionMask.Checked = True
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "The motion mask"
            desc = "Accumulate grid rects that had motion in the last X frames."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = taskAlg.gray
            If taskAlg.heartBeat Or taskAlg.optionsChanged Then dst2 = src.Clone

            diff.Run(src)

            motionList.Clear()
            For i = 0 To taskAlg.gridRects.Count - 1
                Dim diffCount = diff.dst2(taskAlg.gridRects(i)).CountNonZero
                If diffCount >= taskAlg.motionThreshold Then
                    For Each index In taskAlg.grid.gridNeighbors(i)
                        If motionList.Contains(index) = False Then motionList.Add(index)
                    Next
                End If
            Next

            motionLists.Add(motionList)

            dst3.SetTo(0)
            For Each mList In motionLists
                For Each index In motionList
                    Dim rect = taskAlg.gridRects(index)
                    src(rect).CopyTo(dst2(rect))
                    dst3(rect).SetTo(255)
                Next
            Next

            If motionLists.Count > 10 Then motionLists.RemoveAt(0)

            taskAlg.motionMask = dst3.Clone
            labels(2) = CStr(motionList.Count) + " grid rects had motion."
        End Sub
    End Class




    Public Class XO_Motion_BasicsAccum : Inherits TaskParent
        Public mCore As New XO_Motion_CoreAccum
        Public Sub New()
            If standalone Then taskAlg.gOptions.showMotionMask.Checked = True
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
            labels(3) = "Updated taskAlg.motionRect"
            desc = "Use the motionlist of rects to create one motion rectangle."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            mCore.Run(src)

            If taskAlg.heartBeat Then dst2 = taskAlg.gray

            dst3.SetTo(0)
            If mCore.motionList.Count > 0 Then
                taskAlg.motionRect = taskAlg.gridRects(mCore.motionList(0))
                For Each index In mCore.motionList
                    taskAlg.motionRect = taskAlg.motionRect.Union(taskAlg.gridRects(index))
                Next
                dst3(taskAlg.motionRect).SetTo(255)
                taskAlg.gray(taskAlg.motionRect).CopyTo(dst2(taskAlg.motionRect))
            Else
                taskAlg.motionRect = New cv.Rect
            End If

            labels(2) = CStr(mCore.motionList.Count) + " grid rect's or " +
                    Format(mCore.motionList.Count / taskAlg.gridRects.Count, "0.0%") +
                    " of bricks had motion."
        End Sub
    End Class





    Public Class XO_Motion_FromCorrelation_MP : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then sliders.setupTrackBar("Correlation Threshold", 800, 1000, 950)
            desc = "Detect Motion in the color image using multi-threading - slower than single-threaded!"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static correlationSlider = OptionParent.FindSlider("Correlation Threshold")
            Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If taskAlg.heartBeat Then dst3 = src.Clone

            dst2 = src

            Dim updateCount As Integer
            Parallel.ForEach(Of cv.Rect)(taskAlg.gridRects,
            Sub(roi)
                Dim correlation As New cv.Mat
                cv.Cv2.MatchTemplate(src(roi), dst3(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                    Interlocked.Increment(updateCount)
                    src(roi).CopyTo(dst3(roi))
                    dst2.Rectangle(roi, white, taskAlg.lineWidth)
                End If
            End Sub)
            labels(2) = "Motion added to dst3 for " + CStr(updateCount) + " segments out of " + CStr(taskAlg.gridRects.Count)
            labels(3) = CStr(taskAlg.gridRects.Count - updateCount) + " segments out of " + CStr(taskAlg.gridRects.Count) + " had > " +
                         Format(correlationSlider.Value / 1000, "0.0%") + " correlation. "
        End Sub
    End Class





    Public Class XO_Motion_FromCorrelation : Inherits TaskParent
        Public Sub New()
            If sliders.Setup(traceName) Then sliders.setupTrackBar("Correlation Threshold", 800, 1000, 950)
            desc = "Detect Motion in the color image.  Rectangles outlines didn't have high correlation."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static correlationSlider = OptionParent.FindSlider("Correlation Threshold")
            Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If taskAlg.heartBeat Then dst3 = src.Clone

            Dim roiMotion As New List(Of cv.Rect)
            For Each roi In taskAlg.gridRects
                Dim correlation As New cv.Mat
                cv.Cv2.MatchTemplate(src(roi), dst3(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                    src(roi).CopyTo(dst3(roi))
                    roiMotion.Add(roi)
                End If
            Next
            dst2 = src
            For Each roi In roiMotion
                dst2.Rectangle(roi, white, taskAlg.lineWidth)
            Next
            labels(2) = "Motion added to dst3 for " + CStr(roiMotion.Count) + " segments out of " + CStr(taskAlg.gridRects.Count)
            labels(3) = CStr(taskAlg.gridRects.Count - roiMotion.Count) + " segments out of " + CStr(taskAlg.gridRects.Count) + " had > " +
                         Format(correlationSlider.Value / 1000, "0.0%") + " correlation. "
        End Sub
    End Class






    Public Class XO_Motion_FromEdge : Inherits TaskParent
        Dim cAccum As New Edge_CannyAccum
        Public Sub New()
            desc = "Detect motion from pixels less than max value in an accumulation."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            cAccum.Run(src)

            Dim mm = GetMinMax(cAccum.dst2)
            labels(3) = "Max value = " + CStr(mm.maxVal) + " min value = " + CStr(mm.minVal)

            dst2 = cAccum.dst2.Threshold(mm.maxVal, 255, cv.ThresholdTypes.TozeroInv)
            dst3 = cAccum.dst2.InRange(1, 254)
        End Sub
    End Class





    Public Class XO_PCdiff_Basics1 : Inherits TaskParent
        Public options As New Options_ImageOffset
        Dim options1 As New Options_Diff
        Public masks(2) As cv.Mat
        Public dst(2) As cv.Mat
        Public pcFiltered(2) As cv.Mat
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32FC1, New cv.Scalar(0))
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC1, New cv.Scalar(0))
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32FC1, New cv.Scalar(0))
            desc = "Compute various differences between neighboring pixels"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            options1.Run()

            Dim r1 = New cv.Rect(1, 1, taskAlg.cols - 2, taskAlg.rows - 2)
            Dim r2 As cv.Rect
            Select Case options.offsetDirection
                Case "Upper Left"
                    r2 = New cv.Rect(0, 0, r1.Width, r1.Height)
                Case "Above"
                    r2 = New cv.Rect(1, 0, r1.Width, r1.Height)
                Case "Upper Right"
                    r2 = New cv.Rect(2, 0, r1.Width, r1.Height)
                Case "Left"
                    r2 = New cv.Rect(0, 1, r1.Width, r1.Height)
                Case "Right"
                    r2 = New cv.Rect(2, 1, r1.Width, r1.Height)
                Case "Lower Left"
                    r2 = New cv.Rect(0, 2, r1.Width, r1.Height)
                Case "Below"
                    r2 = New cv.Rect(1, 2, r1.Width, r1.Height)
                Case "Below Right"
                    r2 = New cv.Rect(2, 2, r1.Width, r1.Height)
            End Select

            Dim r3 = New cv.Rect(1, 1, r1.Width, r1.Height)

            cv.Cv2.Absdiff(taskAlg.pcSplit(0)(r1), taskAlg.pcSplit(0)(r2), dst1(r3))
            cv.Cv2.Absdiff(taskAlg.pcSplit(1)(r1), taskAlg.pcSplit(1)(r2), dst2(r3))
            cv.Cv2.Absdiff(taskAlg.pcSplit(2)(r1), taskAlg.pcSplit(2)(r2), dst3(r3))

            dst = {dst1, dst2, dst3}
            For i = 0 To dst.Count - 1
                masks(i) = dst(i).Threshold(options1.pixelDiffThreshold / 1000, 255,
                                        cv.ThresholdTypes.BinaryInv).ConvertScaleAbs
                pcFiltered(i) = New cv.Mat(src.Size, cv.MatType.CV_32FC1, New cv.Scalar(0))
                taskAlg.pcSplit(i).CopyTo(pcFiltered(i), masks(i))
            Next
        End Sub
    End Class





    Public Class XO_RedCloud_Defect : Inherits TaskParent
        Public hull As New List(Of cv.Point)
        Public Sub New()
            desc = "Find defects in the RedCloud cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(src, labels(2))

            dst3.SetTo(0)
            For Each rc In taskAlg.redCloud.rcList
                Dim contour = ContourBuild(rc.mask)
                Dim hullIndices = cv.Cv2.ConvexHullIndices(contour, False)
                For i = 0 To contour.Count - 1
                    Dim p1 = contour(i)
                    For j = i + 1 To contour.Count - 1
                        Dim p2 = contour(j)
                        If p1 = p2 Then Continue For
                    Next
                Next

                Dim defects = cv.Cv2.ConvexityDefects(contour, hullIndices.ToList)
                Dim lastV As Integer = -1
                Dim newC As New List(Of cv.Point)
                For Each v In defects
                    If v(0) <> lastV And lastV >= 0 Then
                        For i = lastV To v(0) - 1
                            newC.Add(contour(i))
                        Next
                    End If
                    newC.Add(contour(v(0)))
                    newC.Add(contour(v(2)))
                    newC.Add(contour(v(1)))
                    lastV = v(1)
                Next
                DrawTour(dst3(rc.rect), newC, rc.color)
            Next
        End Sub
    End Class





    Public Class XO_RedCloud_Motion : Inherits TaskParent
        Public Sub New()
            desc = "Run RedCloud with the motion-updated version of the pointcloud."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static unchanged As Integer
            If taskAlg.motionRect.Width Then
                dst2 = runRedCloud(taskAlg.pointCloud, labels(2))
            Else
                unchanged += 1
            End If
            If taskAlg.heartBeatLT Then unchanged = 0

            dst2.Rectangle(taskAlg.motionRect, taskAlg.highlight, taskAlg.lineWidth)

            If standaloneTest() Then
                For Each rc In taskAlg.redCloud.rcList
                    SetTrueText(CStr(rc.age), rc.maxDist)
                Next

                RedCloud_Cell.selectCell(taskAlg.redCloud.rcMap, taskAlg.redCloud.rcList)
                If taskAlg.rcD IsNot Nothing Then strOut = taskAlg.rcD.displayCell()
                SetTrueText(strOut, 3)
            End If

            labels(2) = "RedCloud cells were unchanged " + CStr(unchanged) + " times since last heartBeatLT"
        End Sub
    End Class






    Public Class XO_RedColor_BasicsFastAlt : Inherits TaskParent
        Public classCount As Integer
        Public RectList As New List(Of cv.Rect)
        Public Sub New()
            cPtr = RedCloud_Open()
            desc = "Run the C++ RedCloud interface without a mask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = Mat_Basics.srcMustBe8U(src)

            Dim imagePtr As IntPtr
            Dim inputData(dst1.Total - 1) As Byte
            Marshal.Copy(dst1.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
            handleInput.Free()
            dst3 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, imagePtr).Clone
            dst2 = PaletteFull(dst3)

            classCount = RedCloud_Count(cPtr)
            labels(3) = "CV_8U version with " + CStr(classCount) + " cells."

            If classCount = 0 Then Exit Sub ' no data to process.

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))

            Dim rects(classCount * 4) As Integer
            Marshal.Copy(rectData.Data, rects, 0, rects.Length)

            Dim minPixels = dst2.Total * 0.001
            RectList.Clear()

            For i = 0 To rects.Length - 4 Step 4
                Dim r = New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3))
                If r.Width * r.Height >= minPixels Then RectList.Add(r)
            Next
            labels(2) = CStr(RectList.Count) + " cells were found."
        End Sub
        Public Sub Close()
            If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
        End Sub
    End Class




    Public Class XO_RedColor_BasicsSlow : Inherits TaskParent
        Public redSweep As New XO_RedColor_Sweep
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Track the RedColor cells from RedColor_Core"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redSweep.Run(src)
            dst3 = redSweep.dst3

            Static rcListLast = New List(Of rcData)(redSweep.rcList)
            Static rcMapLast As cv.Mat = redSweep.rcMap.clone

            rcList.Clear()
            Dim r2 As cv.Rect
            rcMap.SetTo(0)
            dst2.SetTo(0)
            For Each rc In redSweep.rcList
                Dim r1 = rc.rect
                r2 = New cv.Rect(0, 0, 1, 1) ' fake rect for conditional below...
                Dim indexLast = rcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X) - 1
                If indexLast > 0 Then r2 = rcListLast(indexLast).rect
                If indexLast >= 0 And r1.IntersectsWith(r2) And taskAlg.optionsChanged = False Then
                    rc.age = rcListLast(indexLast).age + 1
                    If rc.age >= 1000 Then rc.age = 2
                    If taskAlg.heartBeat = False And rc.rect.Contains(rcListLast(indexLast).maxdist) Then
                        rc.maxDist = rcListLast(indexLast).maxdist
                    End If
                    rc.color = rcListLast(indexLast).color
                End If
                rc.index = rcList.Count + 1
                rcMap(rc.rect).SetTo(rc.index, rc.mask)
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                If standaloneTest() Then
                    dst2.Circle(rc.maxDist, taskAlg.DotSize, taskAlg.highlight, -1)
                    SetTrueText(CStr(rc.age), rc.maxDist)
                End If
                rcList.Add(rc)
            Next

            labels(2) = CStr(rcList.Count) + " redColor cells were identified "
            labels(3) = redSweep.labels(3)

            rcListLast = New List(Of rcData)(rcList)
            rcMapLast = rcMap.Clone

            RedCloud_Cell.selectCell(rcMap, rcList)
            If taskAlg.rcD IsNot Nothing Then strOut = taskAlg.rcD.displayCell()
            SetTrueText(strOut, 1)
        End Sub
    End Class








    Public Class XO_RedColor_HeartBeat : Inherits TaskParent
        Dim redCore As New XO_RedColor_BasicsSlow
        Public rcList As New List(Of rcData)
        Public rcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            desc = "Run RedColor_Core on the heartbeat but just floodFill at maxDist otherwise."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static rcLost As New List(Of Integer)
            If taskAlg.heartBeat Or taskAlg.optionsChanged Then
                rcLost.Clear()
                redCore.Run(src)
                dst2 = redCore.dst2
                labels(2) = redCore.labels(2)
            Else
                If src.Type <> cv.MatType.CV_8U Then src = taskAlg.gray
                redCore.redSweep.reduction.Run(src)
                dst1 = redCore.redSweep.reduction.dst2 + 1

                Dim index As Integer = 1
                Dim rect As New cv.Rect
                Dim maskRect = New cv.Rect(1, 1, dst1.Width, dst1.Height)
                Dim mask = New cv.Mat(New cv.Size(dst1.Width + 2, dst1.Height + 2), cv.MatType.CV_8U, 0)
                Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
                Dim minCount = dst1.Total * 0.001
                rcList.Clear()
                rcMap.SetTo(0)
                For Each rc In redCore.rcList
                    Dim pt = rc.maxDist
                    If rcMap.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                        Dim count = cv.Cv2.FloodFill(dst1, mask, pt, index, rect, 0, 0, flags)
                        If count > minCount Then
                            Dim pcc = New rcData(dst1(rect), rect, index)
                            If pcc.index >= 0 Then
                                pcc.color = rc.color
                                pcc.age = rc.age + 1
                                rcList.Add(pcc)
                                rcMap(pcc.rect).SetTo(pcc.index Mod 255, pcc.mask)
                                index += 1
                            End If
                        Else
                            If rcLost.Contains(rc.index - 1) = False Then rcLost.Add(rc.index - 1)
                        End If
                    End If
                Next

                dst2 = PaletteBlackZero(rcMap)
                labels(2) = CStr(rcList.Count) + " regions were identified "
            End If

            If standaloneTest() Then
                For Each rc In rcList
                    dst2.Circle(rc.maxDist, taskAlg.DotSize, taskAlg.highlight, -1)
                Next

                dst3.SetTo(0)
                For Each index In rcLost
                    Dim rc = redCore.rcList(index)
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                Next
                labels(3) = "There were " + CStr(rcLost.Count) + " cells temporarily lost."

                RedCloud_Cell.selectCell(rcMap, rcList)
                If taskAlg.rcD IsNot Nothing Then strOut = taskAlg.rcD.displayCell()
                SetTrueText(strOut, 3)
            End If
        End Sub
    End Class







    Public Class XO_RedColor_CloudCellsNoContour : Inherits TaskParent
        Dim redMotion As New XO_RedCloud_Motion
        Dim reduction As New Reduction_Basics
        Public Sub New()
            desc = "Insert the RedCloud cells into the RedColor_Basics input."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redMotion.Run(src)
            reduction.Run(src)

            Dim index = reduction.classCount + 1
            For Each rc In taskAlg.redCloud.rcList
                reduction.dst2(rc.rect).SetTo(index, rc.mask)
                index += 1
                If index >= 255 Then Exit For
            Next

            dst2 = runRedColor(reduction.dst2, labels(2))

            If standaloneTest() Then
                RedCloud_Cell.selectCell(taskAlg.redColor.rcMap, taskAlg.redColor.rcList)
                If taskAlg.rcD IsNot Nothing Then strOut = taskAlg.rcD.displayCell()
                SetTrueText(strOut, 3)
            End If
        End Sub
    End Class





    Public Class XO_RedColor_CloudMask : Inherits TaskParent
        Dim redCell As New RedCloud_CellMask
        Dim reduction As New Reduction_Basics
        Public Sub New()
            desc = "Use the RedCloud_CellMask to build better RedColor cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCell.Run(src)

            reduction.Run(src)
            reduction.dst2.SetTo(0, redCell.dst3)
            dst2 = runRedColor(reduction.dst2, labels(2))

            If standaloneTest() Then
                RedCloud_Cell.selectCell(taskAlg.redColor.rcMap, taskAlg.redColor.rcList)
                If taskAlg.rcD IsNot Nothing Then strOut = taskAlg.rcD.displayCell()
                SetTrueText(strOut, 3)
            End If
        End Sub
    End Class



    Public Class XO_RedColor_Sweep : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public reduction As New Reduction_Basics
        Public rcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            desc = "Find RedColor cells in the reduced color image using a simple floodfill loop."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_8U Then src = taskAlg.gray
            reduction.Run(src)
            dst3 = reduction.dst2 + 1
            labels(3) = reduction.labels(2)

            Dim index As Integer = 1
            Dim rect As New cv.Rect
            Dim maskRect = New cv.Rect(1, 1, dst3.Width, dst3.Height)
            Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
            Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
            Dim minCount = dst3.Total * 0.001
            rcList.Clear()
            rcMap.SetTo(0)
            For y = 0 To dst3.Height - 1
                For x = 0 To dst3.Width - 1
                    Dim pt = New cv.Point(x, y)
                    If dst3.Get(Of Byte)(pt.Y, pt.X) > 0 Then
                        Dim count = cv.Cv2.FloodFill(dst3, mask, pt, index, rect, 0, 0, flags)
                        If count > minCount Then
                            Dim rc = New rcData(dst3(rect), rect, index)
                            If rc.index >= 0 Then
                                rcList.Add(rc)
                                rcMap(rc.rect).SetTo(rc.index Mod 255, rc.mask)
                                index += 1
                            End If
                        Else
                            If rect.Width > 0 And rect.Height > 0 Then dst3(rect).SetTo(255, mask(rect))
                        End If
                    End If
                Next
            Next

            dst2 = PaletteBlackZero(rcMap)

            If standaloneTest() Then
                For Each rc In rcList
                    dst2.Circle(rc.maxDist, taskAlg.DotSize, taskAlg.highlight, -1)
                Next

                RedCloud_Cell.selectCell(rcMap, rcList)
                If taskAlg.rcD IsNot Nothing Then strOut = taskAlg.rcD.displayCell()
                SetTrueText(strOut, 3)
            End If

            labels(2) = CStr(rcList.Count) + " regions were identified "
        End Sub
    End Class





    Public Class XO_RedCC_Histograms : Inherits TaskParent
        Dim hist As New Hist_Basics
        Public redCC As New RedCC_Color8U
        Public colorIDList As New List(Of List(Of Integer))
        Public Sub New()
            desc = "Add Color8U id's to each RedCloud cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redCC.Run(src)
            dst2 = redCC.dst2
            dst1 = redCC.color8u.dst2
            dst3 = redCC.color8u.dst3
            labels = redCC.labels

            hist.Run(dst1)
            Dim actualClasses As Integer
            For i = 1 To hist.histArray.Count - 1
                If hist.histArray(i) Then actualClasses += 1
            Next
            If taskAlg.gOptions.HistBinBar.Maximum >= actualClasses + 1 Then
                taskAlg.gOptions.HistBinBar.Value = actualClasses + 1
            End If

            colorIDList.Clear()
            For Each rc In taskAlg.redCloud.rcList
                Dim tmp = dst1(rc.rect)
                tmp.SetTo(0, Not rc.mask)

                Dim mm = GetMinMax(tmp)
                hist.Run(tmp)

                Dim colorIDs As New List(Of Integer)
                For i = 1 To hist.histArray.Count - 1 ' ignore zeros
                    If hist.histArray(i) Then colorIDs.Add(i)
                Next
                colorIDList.Add(colorIDs)

                If standaloneTest() Then
                    dst2.Circle(rc.maxDist, taskAlg.DotSize, taskAlg.highlight, -1)
                    strOut = ""
                    For Each index In colorIDs
                        strOut += CStr(index) + ","
                    Next
                    SetTrueText(strOut, rc.maxDist, 2)
                    SetTrueText(strOut, rc.maxDist, 3)
                End If
            Next
            If taskAlg.rcD IsNot Nothing Then dst3.Rectangle(taskAlg.rcD.rect, white, taskAlg.lineWidth)
        End Sub
    End Class




    Public Class XO_RedCC_UseHistIDs : Inherits TaskParent
        Dim histID As New XO_RedCC_Histograms
        Public Sub New()
            desc = "Add the colors to the cell mask if they are in the use colorIDs"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            histID.Run(src)
            dst2 = histID.dst2
            labels(2) = histID.labels(2)

            For Each rc In taskAlg.redCloud.rcList
                Dim colorMask As New cv.Mat(rc.rect.Size, cv.MatType.CV_8U, 0)
                For Each index In histID.colorIDList(rc.index - 1)
                    colorMask = colorMask Or histID.redCC.color8u.dst2(rc.rect).InRange(index, index)
                Next
                rc.mask = rc.mask Or colorMask
            Next

            RedCloud_Cell.selectCell(taskAlg.redCloud.rcMap, taskAlg.redCloud.rcList)
            If taskAlg.rcD IsNot Nothing Then
                strOut = taskAlg.rcD.displayCell
                dst3.SetTo(0)
                dst3(taskAlg.rcD.rect).SetTo(white, taskAlg.rcD.mask)
                taskAlg.color(taskAlg.rcD.rect).SetTo(white, taskAlg.rcD.mask)
            End If
            SetTrueText(strOut, 3)
        End Sub
    End Class







    Public Class XO_Line_CoreNew : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Public rawLines As New Line_Core
        Public Sub New()
            desc = "The core algorithm to find lines.  Line_Basics is a taskAlg algorithm that exits when run as a normal algorithm."
        End Sub
        Private Function lpMotion(lp As lpData) As Boolean
            ' return true if either line endpoint was in the motion mask.
            If taskAlg.motionMask.Get(Of Byte)(lp.p1.Y, lp.p1.X) Then Return True
            If taskAlg.motionMask.Get(Of Byte)(lp.p2.Y, lp.p2.X) Then Return True
            Return False
        End Function
        Public Shared Function createMap() As cv.Mat
            Dim lpRectMap As New cv.Mat(taskAlg.workRes, cv.MatType.CV_8U, 0)
            lpRectMap.SetTo(0)
            For Each lp In taskAlg.lines.lpList
                lpRectMap.Rectangle(lp.rect, lp.index, -1)
            Next
            Return lpRectMap
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If lpList.Count <= 1 Then
                taskAlg.motionMask.SetTo(255)
                rawLines.Run(src)
                lpList = New List(Of lpData)(rawLines.lpList)
            End If

            Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
            Dim count As Integer
            For Each lp In lpList
                If lpMotion(lp) = False Then
                    lp.age += 1
                    sortlines.Add(lp.length, lp)
                    count += 1
                End If
            Next

            rawLines.Run(src)

            For Each lp In rawLines.lpList
                If lpMotion(lp) Then
                    lp.age = 1
                    sortlines.Add(lp.length, lp)
                End If
            Next

            lpList.Clear()
            For Each lp In sortlines.Values
                lp.index = lpList.Count
                lpList.Add(lp)
            Next

            For Each lp In lpList
                vbc.DrawLine(dst2, lp, lp.color)
            Next

            labels(2) = CStr(lpList.Count) + " lines - " + CStr(lpList.Count - count) + " were new"
        End Sub
    End Class





    Public Class XO_Line_Motion2 : Inherits TaskParent
        Dim diff As New Diff_RGBAccum
        Dim lineHistory As New List(Of List(Of lpData))
        Public Sub New()
            labels(3) = "Wave at the camera to see results - "
            desc = "Track lines that are the result of motion."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.optionsChanged Then lineHistory.Clear()

            diff.Run(src)
            dst2 = diff.dst2

            If taskAlg.heartBeat Then dst3 = src
            lineHistory.Add(taskAlg.lines.lpList)
            For Each lplist In lineHistory
                For Each lp In lplist
                    dst3.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
                Next
            Next
            If lineHistory.Count >= taskAlg.frameHistoryCount Then lineHistory.RemoveAt(0)

            labels(2) = CStr(taskAlg.lines.lpList.Count) + " lines were found in the diff output"
        End Sub
    End Class





    Public Class XO_Line_Backprojection : Inherits TaskParent
        Dim backP As New BackProject_DisplayColor
        Dim rawLines As New Line_Core
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
            labels = {"", "", "Lines found in the back projection", "Backprojection results"}
            desc = "Find lines in the back projection"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            backP.Run(src)

            rawLines.Run(backP.dst2)
            labels(2) = rawLines.labels(2)
            dst2 = src
            dst3.SetTo(0)
            For Each lp In rawLines.lpList
                dst2.Line(lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
                dst3.Line(lp.p1, lp.p2, 255, taskAlg.lineWidth, taskAlg.lineType)
            Next
        End Sub
    End Class







    Public Class XO_Line_BrickPoints : Inherits TaskParent
        Public sortLines As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Public Sub New()
            If taskAlg.feat Is Nothing Then taskAlg.feat = New Feature_Basics
            If taskAlg.feat Is Nothing Then taskAlg.feat = New Feature_Basics
            desc = "Assign brick points to each of the lines"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = taskAlg.lines.dst2

            sortLines.Clear()
            dst3.SetTo(0)
            For Each pt In taskAlg.features
                Dim lineIndex = taskAlg.lines.dst1.Get(Of Byte)(pt.Y, pt.X)
                If lineIndex = 0 Then Continue For
                Dim color = vecToScalar(taskAlg.lines.dst2.Get(Of cv.Vec3b)(pt.Y, pt.X))
                Dim index As Integer = sortLines.Keys.Contains(lineIndex)
                Dim gridindex = taskAlg.gridMap.Get(Of Integer)(pt.Y, pt.X)
                sortLines.Add(lineIndex, gridindex)
                DrawCircle(dst3, pt, color)
            Next
        End Sub
    End Class





    Public Class XO_KNNLine_SliceTemp : Inherits TaskParent
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Slice the previous image with a horizontal line at ptCenter's height to " +
               "find all the match candidates"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2.SetTo(0)
            dst3.SetTo(0)
            Static lpListLast As New List(Of lpData)(taskAlg.lines.lpList)
            Dim lpMatch As lpData
            Static count As Integer
            Static missCount As Integer
            For i = 0 To taskAlg.lines.lpList.Count - 1
                Dim lp = taskAlg.lines.lpList(i)
                If lp.index > 10 Then Exit For
                Dim color = taskAlg.scalarColors(lp.index + 1)
                dst2.Line(lp.p1, lp.p2, color, taskAlg.lineWidth + 1, taskAlg.lineType)
                Dim r = New cv.Rect(0, lp.ptCenter.Y, dst2.Width, 1) ' create a rect for the slice.
                Dim histogram As New cv.Mat
                cv.Cv2.CalcHist({taskAlg.lines.dst1(r)}, {0}, emptyMat, histogram, 1,
                            {taskAlg.lines.lpList.Count},
                            New cv.Rangef() {New cv.Rangef(0, taskAlg.lines.lpList.Count)})

                Dim histArray(histogram.Total - 1) As Single
                Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

                Dim distances As New List(Of Single)
                Dim indexLast As New List(Of Integer)
                For j = 1 To histArray.Count - 1
                    If histArray(j) > 0 Then
                        lpMatch = lpListLast(j - 1)
                        'knn.trainInput.Add(lpMatch.p1.X)
                        'knn.trainInput.Add(lpMatch.p1.Y)
                        distances.Add(lp.ptCenter.DistanceTo(lpMatch.ptCenter))
                        indexLast.Add(lpMatch.index)
                        ' knn.trainInput.Add(lpMatch.ptCenter.X)
                        'knn.trainInput.Add(lpMatch.ptCenter.Y)
                        'knn.trainInput.Add(lpMatch.p2.X)
                        'knn.trainInput.Add(lpMatch.p2.Y)
                    End If
                Next

                'knn.queries.Add(lp.p1.X)
                'knn.queries.Add(lp.p1.Y)
                'knn.queries.Add(lp.ptCenter.X)
                'knn.queries.Add(lp.ptCenter.Y)
                'knn.queries.Add(lp.p2.X)
                'knn.queries.Add(lp.p2.Y)

                'knn.Run(emptyMat)
                Dim index = indexLast(distances.IndexOf(distances.Min))

                ' Dim index = Math.Floor(knn.result(0, 0))
                lpMatch = lpListLast(index)
                dst3.Circle(lpMatch.ptCenter, taskAlg.DotSize, color, -1)
                If lp.ptCenter.DistanceTo(lpMatch.ptCenter) < 10 Then
                    dst3.Line(lp.p1, lp.p2, color, taskAlg.lineWidth + 1, taskAlg.lineType)
                    count += 1
                Else
                    missCount += 1
                End If
            Next

            dst1 = taskAlg.lines.dst2
            lpListLast = New List(Of lpData)(taskAlg.lines.lpList)
            labels(3) = CStr(count) + " lines were confirmed after matching and " + CStr(missCount) +
                    " could not be confirmed since last heartBeatLT"

            If taskAlg.heartBeatLT Then
                count = 0
                missCount = 0
            End If
        End Sub
    End Class
    Public Class XO_KNNLine_Basics : Inherits TaskParent
        Dim knn As New KNN_Basics
        Public results(,) As Integer
        Public Sub New()
            labels(2) = "The line's end points or center closest to the mouse is highlighted."
            desc = "Use KNN to determine which line is being selected with mouse."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = taskAlg.lines.dst2.Clone
            knn.trainInput.Clear()
            knn.queries.Clear()
            For Each lp In taskAlg.lines.lpList
                knn.trainInput.Add(lp.p1)
                knn.trainInput.Add(lp.ptCenter)
                knn.trainInput.Add(lp.p2)
            Next

            knn.queries.Add(taskAlg.mouseMovePoint)
            knn.Run(emptyMat)

            results = knn.result

            If standaloneTest() Then
                Dim index = Math.Floor(results(0, 0) / 3)
                Dim lpNext = taskAlg.lines.lpList(index)
                dst2.Line(lpNext.p1, lpNext.p2, taskAlg.highlight, taskAlg.lineWidth * 3, cv.LineTypes.AntiAlias)
            End If
        End Sub
    End Class






    Public Class XO_KNNLine_Query : Inherits TaskParent
        Dim knnLine As New XO_KNNLine_Basics
        Public Sub New()
            desc = "Query the KNN results for the nearest line to the mouse."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            knnLine.Run(src)
            dst2 = knnLine.dst2
            labels(2) = knnLine.labels(2)

            Dim index = Math.Floor(knnLine.results(0, 0) / 3)
            Dim lpNext = taskAlg.lines.lpList(index)
            dst2.Line(lpNext.p1, lpNext.p2, taskAlg.highlight, taskAlg.lineWidth * 3, cv.LineTypes.AntiAlias)
        End Sub
    End Class





    Public Class XO_KNNLine_Connect : Inherits TaskParent
        Dim knnLine As New XO_KNNLine_Basics
        Public Sub New()
            desc = "Connect each line to its likely predecessor."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = dst1.Clone
            labels(3) = labels(2)
            Static lpListLast As New List(Of lpData)(taskAlg.lines.lpList)

            knnLine.Run(src)
            dst2 = knnLine.dst2.Clone
            labels(2) = taskAlg.lines.labels(2)

            Dim count As Integer
            For i = 0 To taskAlg.lines.lpList.Count - 1
                Dim lp1 = taskAlg.lines.lpList(i)
                Dim p1 = New cv.Point(dst2.Width, lp1.ptCenter.Y)
                dst2.Line(lp1.ptCenter, p1, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)

                Dim lp2 = lpListLast(lp1.index)
                Dim p2 = New cv.Point2f(0, lp2.ptCenter.Y)
                dst3.Line(p2, lp2.ptCenter, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
                count += 1
                If count >= 10 Then Exit For
            Next
            dst1 = knnLine.dst2.Clone
            lpListLast = New List(Of lpData)(taskAlg.lines.lpList)
        End Sub
    End Class





    Public Class XO_KNNLine_SliceList : Inherits TaskParent
        Dim knn As New KNN_NNBasics
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            OptionParent.FindSlider("KNN Dimension").Value = 1
            desc = "Slice the previous image with a horizontal line at ptCenter's height to " +
               "find all the match candidates"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim knnDimension = knn.options.knnDimension
            dst2.SetTo(0)
            dst3.SetTo(0)
            Static lpListLast As New List(Of lpData)(taskAlg.lines.lpList)
            Dim lpMatch As lpData
            Static count As Integer
            Static missCount As Integer
            For i = 0 To taskAlg.lines.lpList.Count - 1
                Dim lp = taskAlg.lines.lpList(i)
                If lp.index > 10 Then Exit For
                Dim color = taskAlg.scalarColors(lp.index + 1)
                dst2.Line(lp.p1, lp.p2, color, taskAlg.lineWidth + 1, taskAlg.lineType)
                Dim r = New cv.Rect(0, lp.ptCenter.Y, dst2.Width, 1) ' create a rect for the slice.
                Dim histogram As New cv.Mat
                cv.Cv2.CalcHist({taskAlg.lines.dst1(r)}, {0}, emptyMat, histogram, 1,
                            {taskAlg.lines.lpList.Count},
                            New cv.Rangef() {New cv.Rangef(0, taskAlg.lines.lpList.Count)})

                Dim histArray(histogram.Total - 1) As Single
                Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

                knn.trainInput.Clear()
                knn.queries.Clear()
                For j = 1 To histArray.Count - 1
                    If histArray(j) > 0 Then
                        lpMatch = lpListLast(j - 1)
                        'knn.trainInput.Add(lpMatch.p1.X)
                        'knn.trainInput.Add(lpMatch.p1.Y)
                        knn.trainInput.Add(lpMatch.ptCenter.X)
                        'knn.trainInput.Add(lpMatch.ptCenter.Y)
                        'knn.trainInput.Add(lpMatch.p2.X)
                        'knn.trainInput.Add(lpMatch.p2.Y)
                    End If
                Next

                'knn.queries.Add(lp.p1.X)
                'knn.queries.Add(lp.p1.Y)
                knn.queries.Add(lp.ptCenter.X)
                'knn.queries.Add(lp.ptCenter.Y)
                'knn.queries.Add(lp.p2.X)
                'knn.queries.Add(lp.p2.Y)

                knn.Run(emptyMat)

                Dim index = Math.Floor(knn.result(0, 0))
                lpMatch = lpListLast(index)
                dst3.Circle(lpMatch.ptCenter, taskAlg.DotSize, color, -1)
                If lp.ptCenter.DistanceTo(lpMatch.ptCenter) < 10 Then
                    dst3.Line(lp.p1, lp.p2, color, taskAlg.lineWidth + 1, taskAlg.lineType)
                    count += 1
                Else
                    missCount += 1
                End If
            Next

            dst1 = taskAlg.lines.dst2
            lpListLast = New List(Of lpData)(taskAlg.lines.lpList)
            labels(3) = CStr(count) + " lines were confirmed after matching and " + CStr(missCount) +
                    " could not be confirmed since last heartBeatLT"

            If taskAlg.heartBeatLT Then
                count = 0
                missCount = 0
            End If
        End Sub
    End Class







    Public Class XO_KNNLine_SliceIndex : Inherits TaskParent
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Compute the distances of the centers of only the longest lines"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1.SetTo(0)
            dst2.SetTo(0)
            dst3.SetTo(0)
            Static lpListLast As New List(Of lpData)(taskAlg.lines.lpList)
            Dim lpMatch As lpData
            Dim count As Integer
            Dim maxCheck = 10
            For i = 0 To Math.Min(taskAlg.lines.lpList.Count, maxCheck) - 1
                Dim lp = taskAlg.lines.lpList(i)
                Dim color = taskAlg.scalarColors(lp.index + 1)

                dst2.Line(lp.p1, lp.p2, color, taskAlg.lineWidth + 1, taskAlg.lineType)
                Dim distances As New List(Of Single)
                Dim indexLast As New List(Of Integer)
                For j = 0 To Math.Min(lpListLast.Count - 1, maxCheck)
                    lpMatch = lpListLast(j)
                    distances.Add(lp.ptCenter.DistanceTo(lpMatch.ptCenter))
                    indexLast.Add(j)
                Next

                Dim index = indexLast(distances.IndexOf(distances.Min))

                lpMatch = lpListLast(index)
                dst1.Line(lp.ptCenter, lpMatch.ptCenter, taskAlg.highlight, taskAlg.lineWidth, taskAlg.lineType)
                dst3.Line(lp.p1, lp.p2, color, taskAlg.lineWidth + 1, taskAlg.lineType)
                count += 1
            Next

            If taskAlg.heartBeat And taskAlg.gOptions.DebugCheckBox.Checked Then
                lpListLast = New List(Of lpData)(taskAlg.lines.lpList)
            End If
            If taskAlg.heartBeat Then labels(3) = CStr(count) + " lines were matched."
        End Sub
    End Class





    Public Class XO_LineDepth_Basics : Inherits TaskParent
        Public Sub New()
            If taskAlg.bricks Is Nothing Then taskAlg.bricks = New Brick_Basics
            dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Find the longest line in BGR and use it to measure the average depth for the line"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.lines.lpList.Count <= 1 Then Exit Sub
            Dim lp = taskAlg.lines.lpList(0)
            dst2 = src

            dst2.Line(lp.p1, lp.p2, cv.Scalar.Yellow, taskAlg.lineWidth + 3, taskAlg.lineType)

            Dim gcMin = taskAlg.bricks.brickList(taskAlg.gridMap.Get(Of Single)(lp.p1.Y, lp.p1.X))
            Dim gcMax = taskAlg.bricks.brickList(taskAlg.gridMap.Get(Of Single)(lp.p2.Y, lp.p2.X))

            dst0.SetTo(0)
            dst0.Line(lp.p1, lp.p2, 255, 3, taskAlg.lineType)
            dst0.SetTo(0, taskAlg.noDepthMask)

            Dim mm = GetMinMax(taskAlg.pcSplit(2), dst0)
            If gcMin.pt.DistanceTo(mm.minLoc) > gcMin.pt.DistanceTo(mm.maxLoc) Then
                Dim tmp = gcMin
                gcMin = gcMax
                gcMax = tmp
            End If

            Dim depthMin = If(gcMin.depth > 0, gcMin.depth, mm.minVal)
            Dim depthMax = If(gcMax.depth > 0, gcMax.depth, mm.maxVal)

            Dim depthMean = taskAlg.pcSplit(2).Mean(dst0)(0)
            DrawCircle(dst2, lp.p1, taskAlg.DotSize + 4, cv.Scalar.Red)
            DrawCircle(dst2, lp.p2, taskAlg.DotSize + 4, cv.Scalar.Blue)

            If lp.p1.DistanceTo(mm.minLoc) < lp.p2.DistanceTo(mm.maxLoc) Then
                mm.minLoc = lp.p1
                mm.maxLoc = lp.p2
            Else
                mm.minLoc = lp.p2
                mm.maxLoc = lp.p1
            End If

            If taskAlg.heartBeat Then
                SetTrueText("Average Depth = " + Format(depthMean, fmt1) + "m", New cv.Point((lp.p1.X + lp.p2.X) / 2,
                                                                                     (lp.p1.Y + lp.p2.Y) / 2), 2)
                labels(2) = "Min Distance = " + Format(depthMin, fmt1) + ", Max Distance = " + Format(depthMax, fmt1) +
                    ", Mean Distance = " + Format(depthMean, fmt1) + " meters "

                SetTrueText(Format(depthMin, fmt1) + "m", New cv.Point(mm.minLoc.X + 5, mm.minLoc.Y - 15), 2)
                SetTrueText(Format(depthMax, fmt1) + "m", New cv.Point(mm.maxLoc.X + 5, mm.maxLoc.Y - 15), 2)
            End If
        End Sub
    End Class






    Public Class XO_Line_Degrees : Inherits TaskParent
        Public Sub New()
            desc = "Find similar lines using the angle variable."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim degrees = taskAlg.gOptions.DebugSlider.Value
            dst2 = src
            Dim count As Integer
            For Each lp In taskAlg.lines.lpList
                If Math.Abs(lp.angle - degrees) < taskAlg.angleThreshold Then
                    vbc.DrawLine(dst2, lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth * 2)
                    count += 1
                Else
                    vbc.DrawLine(dst2, lp, taskAlg.highlight)
                End If
            Next

            SetTrueText("Use the debug slider to identify which lines to display (value indicates degrees.)")
            labels(2) = CStr(count) + " lines were found with angle " + CStr(degrees) + " degrees"
        End Sub
    End Class






    Public Class XO_Line_Info : Inherits TaskParent
        Public Sub New()
            labels(3) = "The selected line with details."
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Display details about the line selected."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then taskAlg.lpD = taskAlg.lineGravity
            labels(2) = taskAlg.lines.labels(2) + " - Use the global option 'DebugSlider' to select a line."

            If taskAlg.lines.lpList.Count <= 1 Then Exit Sub
            dst2.SetTo(0)
            For Each lp In taskAlg.lines.lpList
                dst2.Line(lp.p1, lp.p2, white, taskAlg.lineWidth, cv.LineTypes.Link8)
                DrawCircle(dst2, lp.p1, taskAlg.DotSize, taskAlg.highlight)
            Next

            dst2.Line(taskAlg.lpD.p1, taskAlg.lpD.p2, taskAlg.highlight, taskAlg.lineWidth + 1, taskAlg.lineType)

            strOut = "Line ID = " + CStr(taskAlg.lpD.p1GridIndex) + " Age = " + CStr(taskAlg.lpD.age) + vbCrLf
            strOut += "Length (pixels) = " + Format(taskAlg.lpD.length, fmt1) + " index = " + CStr(taskAlg.lpD.index) + vbCrLf
            strOut += "p1GridIndex = " + CStr(taskAlg.lpD.p1GridIndex) + " p2GridIndex = " + CStr(taskAlg.lpD.p2GridIndex) + vbCrLf

            strOut += "p1 = " + taskAlg.lpD.p1.ToString + ", p2 = " + taskAlg.lpD.p2.ToString + vbCrLf
            strOut += "pE1 = " + taskAlg.lpD.pE1.ToString + ", pE2 = " + taskAlg.lpD.pE2.ToString + vbCrLf + vbCrLf
            strOut += "RGB Angle = " + CStr(taskAlg.lpD.angle) + vbCrLf
            strOut += "RGB Slope = " + Format(taskAlg.lpD.slope, fmt3) + vbCrLf
            strOut += vbCrLf + "NOTE: the Y-Axis is inverted - Y increases down so slopes are inverted." + vbCrLf + vbCrLf

            SetTrueText(strOut, 3)
        End Sub
    End Class







    Public Class XO_Line_Intersects : Inherits TaskParent
        Public intersects As New List(Of cv.Point2f)
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Find any intersects in the image and track them."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            intersects.Clear()

            For i = 0 To taskAlg.lines.lpList.Count - 1
                Dim lp1 = taskAlg.lines.lpList(i)
                For j = i + 1 To taskAlg.lines.lpList.Count - 1
                    Dim lp2 = taskAlg.lines.lpList(j)
                    Dim intersectionPoint = Line_Intersection.IntersectTest(lp1, lp2)
                    If intersectionPoint.X >= 0 And intersectionPoint.X < dst2.Width Then
                        If intersectionPoint.Y >= 0 And intersectionPoint.Y < dst2.Height Then
                            intersects.Add(intersectionPoint)
                            If intersects.Count >= taskAlg.FeatureSampleSize Then Exit For
                        End If
                    End If
                Next
                If intersects.Count >= taskAlg.FeatureSampleSize Then Exit For
            Next

            dst2 = src
            If dst3.CountNonZero > taskAlg.FeatureSampleSize * 10 Then dst3.SetTo(0)
            For Each pt In intersects
                DrawCircle(dst2, pt, taskAlg.highlight)
                DrawCircle(dst3, pt, white)
            Next
        End Sub
    End Class







    Public Class XO_Line_LeftRightMatch : Inherits TaskParent
        Dim lrLines As New Line_LeftRight
        Public lp As New lpData
        Public lpOutput As New lpData
        Public Sub New()
            desc = "Identify a line that is a match in the left and right images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            lp = taskAlg.lineLongest

            lrLines.Run(emptyMat)
            dst2 = lrLines.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3 = lrLines.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            Dim r1 = taskAlg.gridRects(taskAlg.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
            Dim r2 = taskAlg.gridRects(taskAlg.gridMap.Get(Of Integer)(lp.ptCenter.Y, lp.ptCenter.X))
            Dim r3 = taskAlg.gridRects(taskAlg.gridMap.Get(Of Integer)(lp.p2.Y, lp.p2.X))
            Dim depth1 = taskAlg.pcSplit(2)(r1).Mean().Val0
            Dim depth2 = taskAlg.pcSplit(2)(r2).Mean().Val0
            Dim depth3 = taskAlg.pcSplit(2)(r3).Mean().Val0
            Dim disp1 = taskAlg.calibData.baseline * taskAlg.calibData.leftIntrinsics.fx / depth1
            Dim disp2 = taskAlg.calibData.baseline * taskAlg.calibData.leftIntrinsics.fx / depth2
            Dim disp3 = taskAlg.calibData.baseline * taskAlg.calibData.leftIntrinsics.fx / depth3

            Dim lp1 = New lpData(New cv.Point2f(lp.p1.X - disp1, lp.p1.Y),
                             New cv.Point2f(lp.ptCenter.X - disp2, lp.ptCenter.Y))
            Dim lp2 = New lpData(New cv.Point2f(lp.p1.X - disp1, lp.p1.Y), New cv.Point2f(lp.p2.X - disp3, lp.p2.Y))
            If Math.Abs(lp1.angle - lp2.angle) < taskAlg.angleThreshold Then lpOutput = lp2
            vbc.DrawLine(dst3, lpOutput.p1, lpOutput.p2, taskAlg.highlight, taskAlg.lineWidth + 1)
            vbc.DrawLine(dst2, lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth + 1)
        End Sub
    End Class







    Public Class XO_Line_LeftRightMatch3 : Inherits TaskParent
        Dim lrLines As New Line_LeftRight
        Public lp As New lpData
        Public lpOutput As New List(Of lpData)
        Public Sub New()
            desc = "Identify a line that is a match in the left and right images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            lrLines.Run(emptyMat)
            dst2 = lrLines.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3 = lrLines.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            Dim lplist As New List(Of lpData)(taskAlg.lines.lpList)

            lpOutput.Clear()
            For i = 0 To lplist.Count - 1
                lp = lplist(i)
                Dim r1 = taskAlg.gridRects(taskAlg.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
                Dim r2 = taskAlg.gridRects(taskAlg.gridMap.Get(Of Integer)(lp.ptCenter.Y, lp.ptCenter.X))
                Dim r3 = taskAlg.gridRects(taskAlg.gridMap.Get(Of Integer)(lp.p2.Y, lp.p2.X))

                Dim depth1 = taskAlg.pcSplit(2)(r1).Mean().Val0
                Dim depth2 = taskAlg.pcSplit(2)(r2).Mean().Val0
                Dim depth3 = taskAlg.pcSplit(2)(r3).Mean().Val0

                If depth1 = 0 Then Continue For
                If depth2 = 0 Then Continue For
                If depth3 = 0 Then Continue For

                Dim disp1 = taskAlg.calibData.baseline * taskAlg.calibData.leftIntrinsics.fx / depth1
                Dim disp2 = taskAlg.calibData.baseline * taskAlg.calibData.leftIntrinsics.fx / depth2
                Dim disp3 = taskAlg.calibData.baseline * taskAlg.calibData.leftIntrinsics.fx / depth3

                Dim lp1 = New lpData(New cv.Point2f(lp.p1.X - disp1, lp.p1.Y),
                                 New cv.Point2f(lp.ptCenter.X - disp2, lp.ptCenter.Y))
                Dim lp2 = New lpData(New cv.Point2f(lp.p1.X - disp1, lp.p1.Y),
                                 New cv.Point2f(lp.p2.X - disp3, lp.p2.Y))
                If Math.Abs(lp1.angle - lp2.angle) >= taskAlg.angleThreshold Then Continue For

                Dim lpOut = lp2
                lp.index = lpOutput.Count
                lpOutput.Add(lp)
                vbc.DrawLine(dst3, lpOut.p1, lpOut.p2, taskAlg.highlight, taskAlg.lineWidth + 1)
                vbc.DrawLine(dst2, lp.p1, lp.p2, taskAlg.highlight, taskAlg.lineWidth + 1)
            Next
            labels(2) = CStr(lpOutput.Count) + " left image lines were matched in the right image and confirmed with the center point."
        End Sub
    End Class





    Public Class XO_Line_Longest : Inherits TaskParent
        Public match As New Match_Basics
        Public deltaX As Single, deltaY As Single
        Dim lp As New lpData
        Public Sub New()
            desc = "Identify the longest line in the output of line_basics."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lplist = taskAlg.lines.lpList
            taskAlg.lineLongestChanged = False
            ' camera is often warming up for the first few images.
            If match.correlation < taskAlg.fCorrThreshold Or taskAlg.frameCount < 10 Or taskAlg.heartBeat Then
                lp = lplist(0)
                match.template = taskAlg.gray(lp.rect)
                taskAlg.lineLongestChanged = True
            End If

            match.Run(taskAlg.gray.Clone)

            If match.correlation < taskAlg.fCorrThreshold Then
                taskAlg.lineLongestChanged = True
                If lplist.Count > 1 Then
                    Dim histogram As New cv.Mat
                    cv.Cv2.CalcHist({taskAlg.lines.dst1(lp.rect)}, {0}, emptyMat, histogram, 1, {lplist.Count},
                                 New cv.Rangef() {New cv.Rangef(1, lplist.Count)})

                    Dim histArray(histogram.Total - 1) As Single
                    Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

                    Dim histList = histArray.ToList
                    ' pick the lp that has the most pixels in the lp.rect.
                    lp = lplist(histList.IndexOf(histList.Max))
                    match.template = taskAlg.gray(lp.rect)
                    match.correlation = 1
                Else
                    match.correlation = 0 ' force a restart
                End If
            Else
                deltaX = match.newRect.X - lp.rect.X
                deltaY = match.newRect.Y - lp.rect.Y
                Dim p1 = New cv.Point(lp.p1.X + deltaX, lp.p1.Y + deltaY)
                Dim p2 = New cv.Point(lp.p2.X + deltaX, lp.p2.Y + deltaY)
                lp = New lpData(p1, p2)
            End If

            If standaloneTest() Then
                dst2 = src
                vbc.DrawLine(dst2, lp)
                DrawRect(dst2, lp.rect)
                dst3 = taskAlg.lines.dst2
            End If

            taskAlg.lineLongest = lp
            labels(2) = "Selected line has a correlation of " + Format(match.correlation, fmt3) + " with the previous frame."
        End Sub
    End Class



    Public Class XO_Gravity_Basics2 : Inherits TaskParent
        Public options As New Options_Features
        Dim gravityRaw As New Gravity_Basics
        Dim longLine As New XO_Line_Longest
        Public Sub New()
            desc = "Use the slope of the longest RGB line to figure out if camera moved enough to obtain the IMU gravity vector."
        End Sub
        Public Shared Sub showVectors(dst As cv.Mat)
            dst.Line(taskAlg.lineGravity.pE1, taskAlg.lineGravity.pE2, white, taskAlg.lineWidth, taskAlg.lineType)
            dst.Line(taskAlg.lineHorizon.pE1, taskAlg.lineHorizon.pE2, white, taskAlg.lineWidth, taskAlg.lineType)
            If taskAlg.lineLongest IsNot Nothing Then
                dst.Line(taskAlg.lineLongest.p1, taskAlg.lineLongest.p2, taskAlg.highlight, taskAlg.lineWidth * 2, taskAlg.lineType)
                vbc.DrawLine(dst, taskAlg.lineLongest.pE1, taskAlg.lineLongest.pE2, white)
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            gravityRaw.Run(emptyMat)
            longLine.Run(src)
            Static lastLongest = taskAlg.lines.lpList(0)
            If taskAlg.lineLongest.length <> lastLongest.length Or taskAlg.lineGravity.length = 0 Or
            taskAlg.frameCount < 5 Then

                taskAlg.lineGravity = taskAlg.gravityIMU
                taskAlg.lineHorizon = Line_PerpendicularTest.computePerp(taskAlg.lineGravity)
                lastLongest = taskAlg.lineLongest
            End If
            If standaloneTest() Then
                dst2.SetTo(0)
                showVectors(dst2)
                dst3.SetTo(0)
                For Each lp In taskAlg.lines.lpList
                    If Math.Abs(taskAlg.lineGravity.angle - lp.angle) < taskAlg.angleThreshold Then vbc.DrawLine(dst3, lp, white)
                Next
                labels(3) = taskAlg.lines.labels(3)
            End If
        End Sub
    End Class






    Public Class XO_Gravity_BasicsKalman : Inherits TaskParent
        Dim kalman As New Kalman_Basics
        Dim gravity As New Gravity_Basics
        Public Sub New()
            desc = "Use kalman to smooth gravity and horizon vectors."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            gravity.Run(src)

            kalman.kInput = {taskAlg.lineGravity.pE1.X, taskAlg.lineGravity.pE1.Y, taskAlg.lineGravity.pE2.X, taskAlg.lineGravity.pE2.Y}
            kalman.Run(emptyMat)
            taskAlg.lineGravity = New lpData(New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1)),
                                     New cv.Point2f(kalman.kOutput(2), kalman.kOutput(3)))

            taskAlg.lineHorizon = Line_PerpendicularTest.computePerp(taskAlg.lineGravity)

            If standaloneTest() Then
                dst2.SetTo(0)
                vbc.DrawLine(dst2, taskAlg.lineGravity.p1, taskAlg.lineGravity.p2, taskAlg.highlight)
                vbc.DrawLine(dst2, taskAlg.lineHorizon.p1, taskAlg.lineHorizon.p2, cv.Scalar.Red)
            End If
        End Sub
    End Class






    Public Class XO_Line_Trace : Inherits TaskParent
        Public Sub New()
            labels(2) = "Move camera to see the impact"
            desc = "Trace the longestline to visualize the line over time"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.heartBeat Then dst2.SetTo(0)
            vbc.DrawLine(dst2, taskAlg.lineLongest, taskAlg.highlight)
            labels(2) = "Longest line is " + Format(taskAlg.lineLongest.length, fmt1) + " pixels, slope = " +
                     Format(taskAlg.lineLongest.slope, fmt1)

            Static strList = New List(Of String)({labels(2)})
            strList.add(labels(2))
            strOut = ""
            For Each strNext In strList
                strOut += strNext + vbCrLf
            Next

            If strList.Count > 20 Then strList.RemoveAt(0)
            SetTrueText(strOut, 3)
        End Sub
    End Class







    Public Class XO_Line_TraceCenter : Inherits TaskParent
        Public match As New Match_Basics
        Dim intersect As New Line_Intersection
        Public trackPoint As cv.Point2f
        Public Sub New()
            labels(2) = "White line is the last longest line and yellow is the current perpendicular to the longest line."
            desc = "Trace the center of the longest line."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lplist = taskAlg.lines.lpList

            Static lpLast = New lpData(taskAlg.lineLongest.pE1, taskAlg.lineLongest.pE2)
            Dim linePerp = Line_PerpendicularTest.computePerp(taskAlg.lineLongest)

            dst2 = src
            vbc.DrawLine(dst2, lpLast, white)
            vbc.DrawLine(dst2, linePerp, taskAlg.highlight)

            intersect.lp1 = lpLast
            intersect.lp2 = linePerp
            intersect.Run(emptyMat)

            If taskAlg.heartBeatLT Then dst3.SetTo(0)
            trackPoint = intersect.intersectionPoint
            DrawCircle(dst3, trackPoint)
            DrawCircle(dst3, trackPoint)

            lpLast = New lpData(taskAlg.lineLongest.pE1, taskAlg.lineLongest.pE2)
        End Sub
    End Class





    Public Class XO_Line_GCloud : Inherits TaskParent
        Public sortedVerticals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
        Public sortedHorizontals As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
        Public allLines As New SortedList(Of Single, gravityLine)(New compareAllowIdenticalSingleInverted)
        Public options As New Options_LineFinder
        Dim match As New XO_Match_tCell
        Dim angleSlider As System.Windows.Forms.TrackBar
        Dim rawLines As New Line_Core
        Public Sub New()
            angleSlider = OptionParent.FindSlider("Angle tolerance in degrees")
            labels(2) = "XO_Line_GCloud - Blue are vertical lines using the angle thresholds."
            desc = "Find all the vertical lines using the point cloud rectified with the IMU vector for gravity."
        End Sub
        Public Function updateGLine(src As cv.Mat, brick As gravityLine, p1 As cv.Point, p2 As cv.Point) As gravityLine
            brick.tc1.center = p1
            brick.tc2.center = p2
            brick.tc1 = match.createCell(src, brick.tc1.correlation, p1)
            brick.tc2 = match.createCell(src, brick.tc2.correlation, p2)
            brick.tc1.strOut = Format(brick.tc1.correlation, fmt2) + vbCrLf + Format(brick.tc1.depth, fmt2) + "m"
            brick.tc2.strOut = Format(brick.tc2.correlation, fmt2) + vbCrLf + Format(brick.tc2.depth, fmt2) + "m"

            Dim mean = taskAlg.pointCloud(brick.tc1.rect).Mean(taskAlg.depthMask(brick.tc1.rect))
            brick.pt1 = New cv.Point3f(mean(0), mean(1), mean(2))
            brick.tc1.depth = brick.pt1.Z
            mean = taskAlg.pointCloud(brick.tc2.rect).Mean(taskAlg.depthMask(brick.tc2.rect))
            brick.pt2 = New cv.Point3f(mean(0), mean(1), mean(2))
            brick.tc2.depth = brick.pt2.Z

            brick.len3D = Distance_Basics.distance3D(brick.pt1, brick.pt2)
            If brick.pt1 = New cv.Point3f Or brick.pt2 = New cv.Point3f Then
                brick.len3D = 0
            Else
                brick.arcX = Math.Asin((brick.pt1.X - brick.pt2.X) / brick.len3D) * 57.2958
                brick.arcY = Math.Abs(Math.Asin((brick.pt1.Y - brick.pt2.Y) / brick.len3D) * 57.2958)
                If brick.arcY > 90 Then brick.arcY -= 90
                brick.arcZ = Math.Asin((brick.pt1.Z - brick.pt2.Z) / brick.len3D) * 57.2958
            End If

            Return brick
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim maxAngle = angleSlider.Value

            dst2 = src.Clone
            rawLines.Run(src.Clone)

            sortedVerticals.Clear()
            sortedHorizontals.Clear()
            For Each lp In rawLines.lpList
                Dim brick As New gravityLine
                brick = updateGLine(src, brick, lp.p1, lp.p2)
                allLines.Add(lp.p1.DistanceTo(lp.p2), brick)
                If Math.Abs(90 - brick.arcY) < maxAngle And brick.tc1.depth > 0 And brick.tc2.depth > 0 Then
                    sortedVerticals.Add(lp.p1.DistanceTo(lp.p2), brick)
                    vbc.DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Blue)
                End If
                If Math.Abs(brick.arcY) <= maxAngle And brick.tc1.depth > 0 And brick.tc2.depth > 0 Then
                    sortedHorizontals.Add(lp.p1.DistanceTo(lp.p2), brick)
                    vbc.DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
                End If
            Next

            labels(2) = Format(sortedHorizontals.Count, "00") + " Horizontal lines were identified and " +
                    Format(sortedVerticals.Count, "00") + " Vertical lines were identified."
        End Sub
    End Class








    Public Class XO_KNN_ClosestTracker : Inherits TaskParent
        Public lastPair As New lpData
        Public trainInput As New List(Of cv.Point2f)
        Dim minDistances As New List(Of Single)
        Public Sub New()
            labels = {"", "", "Highlight the tracked line (move camera to see track results)", "Candidate lines - standaloneTest() only"}
            desc = "Find the longest line and keep finding it among the list of lines using a minimized KNN test."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone

            Dim p1 As cv.Point2f, p2 As cv.Point2f
            If trainInput.Count = 0 Then
                dst3 = taskAlg.lines.dst2
            Else
                p1 = lastPair.p1
                p2 = lastPair.p2
            End If

            For Each lp In taskAlg.lines.lpList
                If trainInput.Count = 0 Then
                    p1 = lp.p1
                    p2 = lp.p2
                End If
                trainInput.Add(lp.p1)
                trainInput.Add(lp.p2)
                If trainInput.Count >= 10 Then Exit For
            Next

            If trainInput.Count = 0 Then
                SetTrueText("No lines were found in the current image.")
                Exit Sub
            End If

            If lastPair.compare(New lpData) Then lastPair = New lpData(p1, p2)
            Dim distances As New List(Of Single)
            For i = 0 To trainInput.Count - 1 Step 2
                Dim pt1 = trainInput(i)
                Dim pt2 = trainInput(i + 1)
                distances.Add(Math.Min(pt1.DistanceTo(lastPair.p1) + pt2.DistanceTo(lastPair.p2), pt1.DistanceTo(lastPair.p2) + pt2.DistanceTo(lastPair.p2)))
            Next

            Dim minDist = distances.Min
            Dim index = distances.IndexOf(minDist) * 2
            p1 = trainInput(index)
            p2 = trainInput(index + 1)

            If minDistances.Count > 0 Then
                If minDist > minDistances.Max * 2 Then
                    Debug.WriteLine("Overriding KNN min Distance Rule = " + Format(minDist, fmt0) + " max = " + Format(minDistances.Max, fmt0))
                    lastPair = New lpData(trainInput(0), trainInput(1))
                Else
                    lastPair = New lpData(p1, p2)
                End If
            Else
                lastPair = New lpData(p1, p2)
            End If

            If minDist > 0 Then minDistances.Add(minDist)
            If minDistances.Count > 100 Then minDistances.RemoveAt(0)

            vbc.DrawLine(dst2, p1, p2, taskAlg.highlight)
            trainInput.Clear()
        End Sub
    End Class






    Public Class XO_KNN_ClosestLine : Inherits TaskParent
        Public lastP1 As cv.Point2f
        Public lastP2 As cv.Point2f
        Public lastIndex As Integer
        Public trainInput As New List(Of cv.Point2f)
        Public Sub New()
            desc = "Try to find the closest pair of points in the traininput.  Dynamically compute distance ceiling to determine when to report fail."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone

            If lastP1 = New cv.Point2f Then
                SetTrueText("KNN_ClosestLine is only run with other KNN algorithms" + vbCrLf +
                        "lastP1 and lastP2 need to be initialized by the other algorithm." + vbCrLf +
                        "Initialize with a pair of points to track a line. " + vbCrLf +
                        "Use KNN_ClosestVertical to test this algorithm.", 3)
                Exit Sub
            End If

            Dim distances As New List(Of Single)
            For i = 0 To trainInput.Count - 1 Step 2
                Dim pt1 = trainInput(i)
                Dim pt2 = trainInput(i + 1)
                distances.Add(Math.Min(pt1.DistanceTo(lastP1) + pt2.DistanceTo(lastP2), pt1.DistanceTo(lastP2) + pt2.DistanceTo(lastP2)))
            Next

            Dim minDist = distances.Min
            lastIndex = distances.IndexOf(minDist) * 2
            lastP1 = trainInput(lastIndex)
            lastP2 = trainInput(lastIndex + 1)

            Static minDistances As New List(Of Single)({distances(0)})
            If minDist > minDistances.Max * 4 Then
                Debug.WriteLine("Overriding KNN min Distance Rule = " + Format(minDist, fmt0) + " max = " + Format(minDistances.Max, fmt0))
                lastP1 = trainInput(0)
                lastP2 = trainInput(1)
            End If

            ' track the last 100 non-zero minDist values to use as a guide to determine when a line was lost and a new pair has to be used.
            If minDist > 0 Then minDistances.Add(minDist)
            If minDistances.Count > 100 Then minDistances.RemoveAt(0)

            vbc.DrawLine(dst2, lastP1, lastP2, taskAlg.highlight)
            trainInput.Clear()
        End Sub
    End Class





    Public Class XO_MatchLine_BasicsOriginal : Inherits TaskParent
        Public match As New Match_Basics
        Public lpInput As New lpData
        Public lpOutput As lpData
        Public corner1 As Integer, corner2 As Integer
        Dim lpSave As New lpData
        Dim knn As New XO_KNN_ClosestTracker
        Public Sub New()
            desc = "Find and track a line in the BGR image."
        End Sub
        Private Function cornerToPoint(whichCorner As Integer, r As cv.Rect) As cv.Point2f
            Select Case whichCorner
                Case 0
                    Return r.TopLeft
                Case 1
                    Return New cv.Point2f(r.BottomRight.X, r.TopLeft.Y)
                Case 2
                    Return r.BottomRight
            End Select
            Return New cv.Point2f(r.TopLeft.X, r.BottomRight.Y)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone

            If match.correlation < taskAlg.fCorrThreshold Or lpSave.p1 <> lpInput.p1 Or lpSave.p2 <> lpInput.p2 Then
                lpSave = lpInput

                If standalone Then lpInput = taskAlg.lines.lpList(0)

                Dim r = ValidateRect(New cv.Rect(Math.Min(lpInput.p1.X, lpInput.p2.X), Math.Min(lpInput.p1.Y, lpInput.p2.Y),
                                             Math.Abs(lpInput.p1.X - lpInput.p2.X), Math.Abs(lpInput.p1.Y - lpInput.p2.Y)))
                match.template = src(r).Clone

                Dim p1 = New cv.Point(CInt(lpInput.p1.X), CInt(lpInput.p1.Y))
                ' Determine which corner - numbering topleft = 0 clockwise, 1, 2, 3
                If r.TopLeft.DistanceTo(p1) <= 2 Then
                    corner1 = 0
                    corner2 = 2
                ElseIf r.BottomRight.DistanceTo(p1) <= 2 Then
                    corner1 = 2
                    corner2 = 0
                ElseIf r.Y = p1.Y Then
                    corner1 = 1
                    corner2 = 3
                Else
                    corner1 = 3
                    corner2 = 1
                End If
            End If

            match.Run(src)
            If match.correlation >= taskAlg.fCorrThreshold Then
                If standaloneTest() Then dst3 = match.dst0.Resize(dst3.Size)
                Dim p1 = cornerToPoint(corner1, match.newRect)
                Dim p2 = cornerToPoint(corner2, match.newRect)
                dst2.Line(p1, p2, taskAlg.highlight, taskAlg.lineWidth + 2, taskAlg.lineType)
                lpOutput = New lpData(p1, p2)
            End If
            labels(2) = "Longest line end points had correlation of " + Format(match.correlation, fmt3) + " with the original longest line."
        End Sub
    End Class







    Public Class XO_MatchLine_Longest : Inherits TaskParent
        Public knn As New XO_KNN_ClosestTracker
        Public matchLine As New XO_MatchLine_BasicsOriginal
        Public Sub New()
            desc = "Find and track the longest line in the BGR image with a lightweight KNN."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            knn.Run(src.Clone)
            matchLine.lpInput = New lpData(knn.lastPair.p1, knn.lastPair.p2)

            matchLine.Run(src)
            dst2 = matchLine.dst2
            vbc.DrawLine(dst2, matchLine.lpOutput.p1, matchLine.lpOutput.p2, cv.Scalar.Red)

            labels(2) = "Longest line end points had correlation of " + Format(matchLine.match.correlation, fmt3) +
                    " with the original longest line."
        End Sub
    End Class






    Public Class XO_MatchLine_Horizon : Inherits TaskParent
        Dim matchLine As New XO_MatchLine_BasicsOriginal
        Public Sub New()
            desc = "Verify the horizon using MatchTemplate."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            'If matchLine.match.correlation < matchLine.match.options.correlationThreshold Then matchLine.lpInput = taskAlg.lineHorizon
            If taskAlg.quarterBeat Then matchLine.lpInput = taskAlg.lineHorizon
            matchLine.Run(src)
            dst2 = matchLine.dst2
            vbc.DrawLine(dst2, taskAlg.lineHorizon.p1, taskAlg.lineHorizon.p2, cv.Scalar.Red)
            labels(2) = "MatchLine correlation = " + Format(matchLine.match.correlation, fmt3) + " - Red = current horizon, yellow is matchLine output"
        End Sub
    End Class





    Public Class XO_MatchLine_Gravity : Inherits TaskParent
        Dim matchLine As New XO_MatchLine_BasicsOriginal
        Public Sub New()
            desc = "Verify the gravity vector using MatchTemplate."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            matchLine.lpInput = taskAlg.lineGravity
            matchLine.Run(src)
            dst2 = matchLine.dst2
            vbc.DrawLine(dst2, taskAlg.lineGravity.p1, taskAlg.lineGravity.p2, cv.Scalar.Red)
            labels(2) = "MatchLine correlation = " + Format(matchLine.match.correlation, fmt3) +
                    " - Red = current gravity vector, yellow is matchLine output"
        End Sub
    End Class




    Public Class XO_KNN_TrackMean : Inherits TaskParent
        Dim plot As New Plot_Histogram
        Dim knn As New KNN_OneToOne
        Const maxDistance As Integer = 50
        Public shiftX As Single
        Public shiftY As Single
        Dim motionTrack As New List(Of cv.Point2f)
        Dim lastImage As cv.Mat
        Dim dotSlider As TrackBar
        Dim options As New Options_KNN
        Dim feat As New Feature_General
        Public Sub New()
            taskAlg.featureOptions.FeatureSampleSize.Value = 200
            dotSlider = OptionParent.FindSlider("Average distance multiplier")
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"", "Histogram of Y-Axis camera motion", "Yellow points are good features and the white trail in the center estimates camera motion.", "Histogram of X-Axis camera motion"}
            desc = "Track points with KNN and match the goodFeatures from frame to frame"
        End Sub
        Private Function plotDiff(diffList As List(Of Integer), xyStr As String, labelImage As Integer, ByRef label As String) As Single
            Dim count = diffList.Max - diffList.Min + 1
            Dim hist(maxDistance - 1) As Single
            Dim zeroLoc = hist.Count / 2
            Dim nonZero As Integer
            Dim zeroCount As Integer
            For Each diff In diffList
                If diff <> 0 Then nonZero += 1 Else zeroCount += 1
                diff += zeroLoc
                If diff >= maxDistance Then diff = maxDistance - 1
                If diff < 0 Then diff = 0
                hist(diff) += 1
            Next
            plot.Run(cv.Mat.FromPixelData(hist.Count, 1, cv.MatType.CV_32F, hist.ToArray))
            Dim histList = hist.ToList
            Dim maxVal = histList.Max
            Dim maxIndex = histList.IndexOf(maxVal)
            plot.maxRange = Math.Ceiling((maxVal + 50) - (maxVal + 50) Mod 50)
            label = xyStr + "Max count = " + CStr(maxVal) + " at " + CStr(maxIndex - zeroLoc) + " with " + CStr(nonZero) + " non-zero values or " +
                             Format(nonZero / (nonZero + zeroCount), "0%")

            Dim histSum As Single
            For i = 0 To histList.Count - 1
                histSum += histList(i) * (i - zeroLoc)
            Next
            Return histSum / histList.Count
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            feat.Run(taskAlg.grayStable)

            If taskAlg.firstPass Then lastImage = src.Clone
            Dim multiplier = dotSlider.Value

            knn.queries = New List(Of cv.Point2f)(taskAlg.features)
            knn.Run(src)

            Dim diffX As New List(Of Integer)
            Dim diffY As New List(Of Integer)
            Dim correlationMat As New cv.Mat
            dst2 = src.Clone
            Dim sz = taskAlg.brickSize
            For Each mps In knn.matches
                Dim currRect = ValidateRect(New cv.Rect(mps.p1.X - sz, mps.p1.Y - sz, sz * 2, sz * 2))
                Dim prevRect = ValidateRect(New cv.Rect(mps.p2.X - sz, mps.p2.Y - sz, currRect.Width, currRect.Height))
                cv.Cv2.MatchTemplate(lastImage(prevRect), src(currRect), correlationMat, feat.options.matchOption)
                Dim corrNext = correlationMat.Get(Of Single)(0, 0)
                DrawCircle(dst2, mps.p1, taskAlg.DotSize, taskAlg.highlight)
                diffX.Add(mps.p1.X - mps.p2.X)
                diffY.Add(mps.p1.Y - mps.p2.Y)
            Next

            If diffX.Count = 0 Or diffY.Count = 0 Then Exit Sub

            Dim xLabel As String = Nothing, yLabel As String = Nothing
            shiftX = multiplier * plotDiff(diffX, " X ", 3, xLabel)
            dst3 = plot.dst2.Clone
            dst3.Line(New cv.Point(plot.plotCenter, 0), New cv.Point(plot.plotCenter, dst2.Height), white, 1)

            shiftY = multiplier * plotDiff(diffY, " Y ", 1, yLabel)
            dst1 = plot.dst2
            dst1.Line(New cv.Point(plot.plotCenter, 0), New cv.Point(plot.plotCenter, dst2.Height), white, 1)

            lastImage = src.Clone

            motionTrack.Add(New cv.Point2f(shiftX + dst2.Width / 2, shiftY + dst2.Height / 2))
            If motionTrack.Count > taskAlg.fpsAlgorithm Then motionTrack.RemoveAt(0)
            Dim lastpt = motionTrack(0)
            For Each pt In motionTrack
                vbc.DrawLine(dst2, pt, lastpt, white)
                lastpt = pt
            Next
            SetTrueText(yLabel, 1)
            SetTrueText(xLabel, 3)
        End Sub
    End Class






    Public Class XO_KNN_TrackEach : Inherits TaskParent
        Dim knn As New KNN_OneToOne
        Dim trackAll As New List(Of List(Of lpData))
        Public options As New Options_Features
        Public Sub New()
            desc = "Track each good feature with KNN and match the features from frame to frame"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim minDistance = options.minDistance

            ' if there was no motion, use minDistance to eliminate the unstable points.
            If taskAlg.optionsChanged = False Then minDistance = 2

            knn.queries = New List(Of cv.Point2f)(taskAlg.features)
            knn.Run(src)

            Dim tracker As New List(Of lpData)
            dst2 = src.Clone
            For Each lp In knn.matches
                If lp.p1.DistanceTo(lp.p2) < minDistance Then tracker.Add(lp)
            Next

            trackAll.Add(tracker)

            For i = 0 To trackAll.Count - 1 Step 2
                Dim t1 = trackAll(i)
                For Each lp In t1
                    DrawCircle(dst2, lp.p1, taskAlg.DotSize, taskAlg.highlight)
                    DrawCircle(dst2, lp.p2, taskAlg.DotSize, taskAlg.highlight)
                    vbc.DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Red)
                Next
            Next

            labels(2) = CStr(taskAlg.features.Count) + " good features were tracked across " + CStr(taskAlg.frameHistoryCount) + " frames."
            SetTrueText(labels(2) + vbCrLf + "The highlighted dots are the feature points", 3)

            If trackAll.Count > taskAlg.frameHistoryCount Then trackAll.RemoveAt(0)
        End Sub
    End Class







    Public Class XO_KNN_NNearest : Inherits TaskParent
        Public knn As cv.ML.KNearest
        Public queries As New List(Of Single)
        Public trainInput As New List(Of Single)
        Public trainData As cv.Mat
        Public queryData As cv.Mat
        Public result(,) As Integer ' Get results here...
        Public options As New Options_KNN
        Public Sub New()
            knn = cv.ML.KNearest.Create()
            desc = "Find the nearest cells to the selected cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim responseList As IEnumerable(Of Integer) = Enumerable.Range(0, 10).Select(Function(x) x)
            If standaloneTest() Then
                SetTrueText("There is no output for the " + traceName + " algorithm when run standaloneTest().  Use the " + traceName + "_Test algorithm")
                Exit Sub
            End If

            Dim qRows = CInt(queries.Count / options.knnDimension)
            If qRows = 0 Then
                SetTrueText("There were no queries provided.  There is nothing to do...")
                Exit Sub
            End If

            queryData = cv.Mat.FromPixelData(qRows, options.knnDimension, cv.MatType.CV_32F, queries.ToArray)
            Dim queryMat As cv.Mat = queryData.Clone

            Dim tRows = CInt(trainInput.Count / options.knnDimension)
            trainData = cv.Mat.FromPixelData(tRows, options.knnDimension, cv.MatType.CV_32F, trainInput.ToArray())

            Dim response As cv.Mat = cv.Mat.FromPixelData(trainData.Rows, 1, cv.MatType.CV_32S,
                                  Enumerable.Range(start:=0, trainData.Rows).ToArray)

            knn.Train(trainData, cv.ML.SampleTypes.RowSample, response)
            Dim neighbors As New cv.Mat
            knn.FindNearest(queryMat, trainData.Rows, New cv.Mat, neighbors)

            ReDim result(neighbors.Rows - 1, neighbors.Cols - 1)
            For i = 0 To neighbors.Rows - 1
                For j = 0 To neighbors.Cols - 1
                    Dim test = neighbors.Get(Of Single)(i, j)
                    If test < trainData.Rows And test >= 0 Then result(i, j) = neighbors.Get(Of Single)(i, j)
                Next
            Next
        End Sub
        Public Sub Close()
            If knn IsNot Nothing Then knn.Dispose()
        End Sub
    End Class






    Public Class XO_KNN_Hulls : Inherits TaskParent
        Dim knn As New KNN_Basics
        Dim redC As New RedCloud_Basics
        Public matchList As New List(Of cv.Point2f)
        Public Sub New()
            knn.desiredMatches = 2
            taskAlg.gOptions.DebugSlider.Value = 2
            labels(2) = "Use the debugslider to define the maximum distance between 'close' points."
            desc = "Use KNN to connect hulls logically."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            If redC.rcList.Count = 0 Then Exit Sub
            dst2 = redC.dst2.Clone

            knn.ptListQuery.Clear()
            dst3.SetTo(0)
            For Each pc In redC.rcList
                dst3(pc.rect).SetTo(pc.color, pc.mask)
                For Each pt In pc.hull
                    dst2(pc.rect).Circle(pt, taskAlg.DotSize, taskAlg.highlight, -1)
                    knn.ptListQuery.Add(New cv.Point(CInt(pt.X) + pc.rect.X, CInt(pt.Y) + pc.rect.Y))
                Next
            Next

            knn.ptListTrain = New List(Of cv.Point)(knn.ptListQuery)

            knn.Run(src)
            matchList.Clear()
            Dim distanceMax = Math.Min(Math.Abs(taskAlg.gOptions.DebugSlider.Value), 10)
            For i = 0 To knn.result.GetUpperBound(0) - 1
                Dim p1 As cv.Point2f = knn.ptListQuery(knn.result(i, 0))
                For j = 0 To knn.result.GetUpperBound(1) - 1
                    Dim p2 As cv.Point2f = knn.ptListQuery(knn.result(i, 1))
                    If p1.DistanceTo(p2) <= distanceMax Then
                        matchList.Add(p1)
                        matchList.Add(p2)
                        dst3.Circle(p1, taskAlg.DotSize, taskAlg.highlight, -1)
                        dst3.Circle(p2, taskAlg.DotSize, taskAlg.highlight, -1)
                    End If
                Next
            Next
            labels(3) = CStr(matchList.Count / 2) + " points were within " + CStr(distanceMax) + " pixels of another cell's hull point"
        End Sub
    End Class






    Public Class XO_KNN_EdgePoints : Inherits TaskParent
        Public lpInput As New List(Of lpData)
        Dim knn As New KNN_N2Basics
        Public distances() As Single
        Public minDistance As Integer = dst2.Width * 0.2
        Public Sub New()
            desc = "Match edgepoints from the current and previous frames."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then lpInput = taskAlg.lines.lpList

            dst2 = src.Clone
            For Each lp In taskAlg.lines.lpList
                HullLine_EdgePoints.EdgePointOffset(lp, 1)
                DrawCircle(dst2, New cv.Point(CInt(lp.pE1.X), CInt(lp.pE1.Y)))
                DrawCircle(dst2, New cv.Point(CInt(lp.pE2.X), CInt(lp.pE2.Y)))
            Next

            knn.queries.Clear()
            For Each lp In lpInput
                knn.queries.Add(lp.pE1)
                knn.queries.Add(lp.pE2)
            Next

            knn.Run(emptyMat)
            knn.trainInput = New List(Of cv.Point2f)(knn.queries) ' for the next iteration.

            ReDim distances(minDistance - 1)
            For i = 0 To knn.queries.Count - 1
                Dim p1 = knn.queries(i)
                Dim index = knn.result(i, 0)
                If index >= knn.trainInput.Count Then Continue For
                Dim p2 = knn.trainInput(index)

                Dim intDistance = CInt(p1.DistanceTo(p2))
                If intDistance >= minDistance Then intDistance = distances.Length - 1
                distances(intDistance) += 1
            Next

            If distances.Count > 0 Then
                Dim distList = distances.ToList
                Dim maxIndex = distList.IndexOf(distList.Max)
                labels(2) = CStr(lpInput.Count * 2) + " edge points found.  Peak distance at " + CStr(maxIndex) + " pixels"

                If standalone Then
                    Static plot As New Plot_OverTimeSingle
                    plot.plotData = maxIndex
                    plot.Run(src)
                    dst3 = plot.dst2
                End If
            End If
        End Sub
    End Class







    Public Class XO_KNN_Emax : Inherits TaskParent
        Dim random As New Random_Basics
        Public knn As New KNN_Basics
        Dim em As New EMax_Basics
        Public Sub New()
            labels(2) = "Output from Emax"
            labels(3) = "Red=TrainingData, yellow = queries - use EMax sigma to introduce more chaos."
            desc = "Emax centroids move but here KNN is used to matched the old and new locations and keep the colors the same."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            em.Run(src)
            random.Run(src)

            knn.queries = New List(Of cv.Point2f)(em.centers)
            knn.Run(src)
            dst2 = em.dst2 + knn.dst2

            knn.knn2.displayResults()
            dst3 = knn.dst2

            knn.trainInput = New List(Of cv.Point2f)(knn.queries)
        End Sub
    End Class







    Public Class XO_Match_tCell : Inherits TaskParent
        Public tCells As New List(Of tCell)
        Dim options As New Options_Features
        Dim lineDisp As New XO_Line_DisplayInfoOld
        Public Sub New()
            tCells.Add(New tCell)
            desc = "Use MatchTemplate to find the new location of the template and update the tc that was provided."
        End Sub
        Public Function createCell(src As cv.Mat, correlation As Single, pt As cv.Point2f) As tCell
            Dim tc As New tCell

            tc.rect = ValidateRect(New cv.Rect(pt.X - taskAlg.brickSize, pt.Y - taskAlg.brickSize, taskAlg.brickSize * 2, taskAlg.brickSize * 2))
            tc.correlation = correlation
            tc.depth = taskAlg.pcSplit(2)(tc.rect).Mean(taskAlg.depthMask(tc.rect))(0) / 1000
            tc.center = pt
            tc.searchRect = ValidateRect(New cv.Rect(tc.center.X - taskAlg.brickSize * 3, tc.center.Y - taskAlg.brickSize * 3,
                                                 taskAlg.brickSize * 6, taskAlg.brickSize * 6))
            If tc.template Is Nothing Then tc.template = src(tc.rect).Clone
            Return tc
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim rSize = taskAlg.brickSize
            If standaloneTest() And taskAlg.heartBeat Then
                options.Run()
                tCells.Clear()
                tCells.Add(createCell(src, 0, New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))))
                tCells.Add(createCell(src, 0, New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))))
            End If

            For i = 0 To tCells.Count - 1
                Dim tc = tCells(i)
                Dim input = src(tc.searchRect)
                cv.Cv2.MatchTemplate(tc.template, input, dst0, cv.TemplateMatchModes.CCoeffNormed)
                Dim mm As mmData = GetMinMax(dst0)
                tc.center = New cv.Point2f(tc.searchRect.X + mm.maxLoc.X + rSize, tc.searchRect.Y + mm.maxLoc.Y + rSize)
                tc.searchRect = ValidateRect(New cv.Rect(tc.center.X - rSize * 3, tc.center.Y - rSize * 3, rSize * 6, rSize * 6))
                tc.rect = ValidateRect(New cv.Rect(tc.center.X - rSize, tc.center.Y - rSize, rSize * 2, rSize * 2))
                tc.correlation = mm.maxVal
                tc.depth = taskAlg.pcSplit(2)(tc.rect).Mean(taskAlg.depthMask(tc.rect))(0) / 1000
                tc.strOut = Format(tc.correlation, fmt2) + vbCrLf + Format(tc.depth, fmt2) + "m"
                tCells(i) = tc
            Next

            If standaloneTest() Then
                lineDisp.tcells = tCells
                lineDisp.Run(src)
                dst2 = lineDisp.dst2
            End If
        End Sub
    End Class









    Public Class XO_EdgeLine_JustLines : Inherits TaskParent
        Public Sub New()
            cPtr = EdgeLine_Image_Open()
            labels = {"", "", "EdgeLine_Image output", ""}
            desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Dim cppData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, cppData, 0, cppData.Length)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = EdgeLine_Image_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, taskAlg.lineWidth)
            handleSrc.Free()
            If imagePtr <> 0 Then dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr)
        End Sub
        Public Sub Close()
            EdgeLine_Image_Close(cPtr)
        End Sub
    End Class




    Public Class XO_Motion_RightMask : Inherits TaskParent
        Public Sub New()
            If taskAlg.bricks Is Nothing Then taskAlg.bricks = New Brick_Basics
            If standalone Then taskAlg.gOptions.showMotionMask.Checked = True
            taskAlg.motionMaskRight = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            labels = {"", "Right View", "Motion Mask for the left view", "Motion Mask for the right view."}
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Build the MotionMask for the right image from the left image bricks with " + vbCrLf +
               "motion.  The result is sloppy and should not be used."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            taskAlg.bricks.Run(taskAlg.grayStable)
            dst2 = taskAlg.motionMask
            dst1 = taskAlg.rightView

            taskAlg.motionMaskRight.SetTo(0)
            For Each index In taskAlg.motionBasics.motionList
                Dim brick = taskAlg.bricks.brickList(index)
                taskAlg.motionMaskRight.Rectangle(brick.rRect, 255, -1)
                dst1.Rectangle(brick.rRect, 255, taskAlg.lineWidth)
            Next
            dst3 = taskAlg.motionMaskRight.Clone
        End Sub
    End Class










    Public Class XO_RedList_Flippers : Inherits TaskParent
        Public flipCells As New List(Of oldrcData)
        Public nonFlipCells As New List(Of oldrcData)
        Public Sub New()
            labels(3) = "Highlighted below are the cells which flipped in color from the previous frame."
            desc = "Identify the cells that are changing color because they were split or lost."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = runRedList(src, labels(3))
            Static lastMap As cv.Mat = XO_RedList_Basics.DisplayCells()

            Dim unMatched As Integer
            Dim unMatchedPixels As Integer
            flipCells.Clear()
            nonFlipCells.Clear()
            dst2.SetTo(0)
            Dim currMap = XO_RedList_Basics.DisplayCells()
            For Each rc In taskAlg.redList.oldrclist
                Dim lastColor = lastMap.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                Dim currColor = currMap.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                If lastColor <> currColor Then
                    unMatched += 1
                    unMatchedPixels += rc.pixels
                    flipCells.Add(rc)
                    dst2(rc.rect).SetTo(rc.color, rc.mask)
                Else
                    nonFlipCells.Add(rc)
                End If
            Next

            lastMap = currMap.Clone

            If taskAlg.heartBeat Then
                labels(2) = CStr(unMatched) + " of " + CStr(taskAlg.redList.oldrclist.Count) + " cells changed " +
                        " tracking color, totaling " + CStr(unMatchedPixels) + " pixels."
            End If
        End Sub
    End Class








    Public Class XO_RedList_FlipTest : Inherits TaskParent
        Dim flipper As New XO_RedList_Flippers
        Public Sub New()
            desc = "Display nonFlipped cells"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = runRedList(src, labels(2))
            Dim lastCells As New List(Of oldrcData)(taskAlg.redList.oldrclist)
            flipper.Run(src)
            dst3 = flipper.dst2

            dst2.SetTo(0)
            Dim ptmaxDstable As New List(Of cv.Point)
            For Each rc In flipper.nonFlipCells
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                ptmaxDstable.Add(rc.maxDStable)
            Next

            Dim count As Integer
            For Each rc In flipper.flipCells
                Dim lrc = lastCells(rc.indexLast)
                Dim index = ptmaxDstable.IndexOf(lrc.maxDStable)
                If index > 0 Then
                    Dim rcNabe = flipper.nonFlipCells(index)
                    dst2(rc.rect).SetTo(rcNabe.color, rc.mask)
                    count += 1
                End If
            Next
            If taskAlg.heartBeat Then
                labels(2) = CStr(flipper.flipCells.Count) + " cells flipped and " + CStr(count) + " cells " +
                        " were flipped back to the main cell."
                labels(3) = flipper.labels(2)
            End If
        End Sub
    End Class






    Public Class XO_RedList_Basics : Inherits TaskParent
        Public inputRemoved As cv.Mat
        Public cellGen As New XO_RedCell_Color
        Public redMask As New RedMask_Basics
        Public oldrclist As New List(Of oldrcData)
        Public rcMap As cv.Mat ' redColor map 
        Public contours As New Contour_Basics
        Public Sub New()
            rcMap = New cv.Mat(New cv.Size(dst2.Width, dst2.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Find cells and then match them to the previous generation with minimum boundary"
        End Sub
        Public Shared Sub setSelectedCell()
            If taskAlg.redList Is Nothing Then Exit Sub
            If taskAlg.redList.oldrclist.Count = 0 Then Exit Sub
            If taskAlg.clickPoint = newPoint And taskAlg.redList.oldrclist.Count > 1 Then
                taskAlg.clickPoint = taskAlg.redList.oldrclist(1).maxDist
            End If
            Dim index = taskAlg.redList.rcMap.Get(Of Byte)(taskAlg.clickPoint.Y, taskAlg.clickPoint.X)
            If index = 0 Then Exit Sub
            If index > 0 And index < taskAlg.redList.oldrclist.Count Then
                taskAlg.oldrcD = taskAlg.redList.oldrclist(index)
                taskAlg.color(taskAlg.oldrcD.rect).SetTo(cv.Scalar.White, taskAlg.oldrcD.mask)
            Else
                ' the 0th cell is always the upper left corner with just 1 pixel.
                If taskAlg.redList.oldrclist.Count > 1 Then taskAlg.oldrcD = taskAlg.redList.oldrclist(1)
            End If
        End Sub
        Public Shared Function DisplayCells() As cv.Mat
            Dim dst As New cv.Mat(taskAlg.workRes, cv.MatType.CV_8UC3, 0)

            For Each rc In taskAlg.redList.oldrclist
                dst(rc.rect).SetTo(rc.color, rc.mask)
            Next

            Return dst
        End Function
        Public Shared Function RebuildRCMap(sortedCells As List(Of oldrcData)) As cv.Mat
            taskAlg.redList.oldrclist.Clear()
            taskAlg.redList.oldrclist.Add(New oldrcData) ' placeholder oldrcData so map is correct.
            taskAlg.redList.rcMap.SetTo(0)
            Static saveColorSetting = taskAlg.gOptions.trackingLabel
            For Each rc In sortedCells
                rc.index = taskAlg.redList.oldrclist.Count

                If saveColorSetting <> taskAlg.gOptions.trackingLabel Then rc.color = black
                If rc.color = black Then rc.color = taskAlg.scalarColors(rc.index)

                taskAlg.redList.oldrclist.Add(rc)
                taskAlg.redList.rcMap(rc.rect).SetTo(rc.index, rc.mask)
                DisplayCells.Circle(rc.maxDStable, taskAlg.DotSize, taskAlg.highlight, -1)
                If rc.index >= 255 Then Exit For
            Next
            saveColorSetting = taskAlg.gOptions.trackingLabel
            taskAlg.redList.rcMap.SetTo(0, taskAlg.noDepthMask)
            Return DisplayCells()
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            If src.Type <> cv.MatType.CV_8U Then
                If standalone And taskAlg.featureOptions.Color8USource.SelectedItem = "EdgeLine_Basics" Then
                    dst1 = contours.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                Else
                    dst1 = Mat_Basics.srcMustBe8U(src)
                End If
            Else
                dst1 = src
            End If

            If inputRemoved IsNot Nothing Then dst1.SetTo(0, inputRemoved)
            redMask.Run(dst1)

            If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
            cellGen.mdList = redMask.mdList
            cellGen.Run(redMask.dst2)

            dst2 = cellGen.dst2

            For Each rc In taskAlg.redList.oldrclist
                DrawCircle(dst2, rc.maxDStable)
            Next
            labels(2) = cellGen.labels(2)
            labels(3) = ""
            SetTrueText("", newPoint, 1)
            ' setSelectedCell()
        End Sub
    End Class




    Public Class XO_RedList_BasicsNew : Inherits TaskParent
        Public inputRemoved As cv.Mat
        Public cellGen As New XO_RedCell_Color
        Public redMask As New RedMask_Basics
        Public rclist As New List(Of rcData)
        Public rcMap As cv.Mat ' redColor map 
        Public contours As New Contour_Basics
        Public Sub New()
            rcMap = New cv.Mat(New cv.Size(dst2.Width, dst2.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Find cells and then match them to the previous generation with minimum boundary"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            If src.Type <> cv.MatType.CV_8U Then
                If standalone And taskAlg.featureOptions.Color8USource.SelectedItem = "EdgeLine_Basics" Then
                    dst1 = contours.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                Else
                    dst1 = Mat_Basics.srcMustBe8U(src)
                End If
            Else
                dst1 = src
            End If

            If inputRemoved IsNot Nothing Then dst1.SetTo(0, inputRemoved)
            redMask.Run(dst1)

            If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
            cellGen.mdList = redMask.mdList
            cellGen.Run(redMask.dst2)

            dst2 = cellGen.dst2

            For Each rc In rclist
                DrawCircle(dst2, rc.maxDist)
            Next
            labels(2) = cellGen.labels(2)
            labels(3) = ""
            SetTrueText("", newPoint, 1)
            XO_RedList_Basics.setSelectedCell()
        End Sub
    End Class







    Public Class XO_RedList_FindCells : Inherits TaskParent
        Public bricks As New List(Of Integer)
        Public Sub New()
            taskAlg.featureOptions.ColorDiffSlider.Value = 25
            cPtr = RedList_FindBricks_Open()
            desc = "Find all the RedCloud cells touched by the mask created by the Motion_History rectangle"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks = New List(Of Integer)

            dst2 = runRedList(src, labels(2))

            Dim cppData(taskAlg.redList.rcMap.Total - 1) As Byte
            Marshal.Copy(taskAlg.redList.rcMap.Data, cppData, 0, cppData.Length)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = RedList_FindBricks_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
            handleSrc.Free()

            Dim count = RedList_FindBricks_TotalCount(cPtr)
            If count = 0 Then Exit Sub

            Dim cellsFound(count - 1) As Integer
            Marshal.Copy(imagePtr, cellsFound, 0, cellsFound.Length)

            bricks = cellsFound.ToList
            dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst0 = dst0.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
            dst3.SetTo(0)
            For Each index In bricks
                If taskAlg.redList.oldrclist.Count <= index Then Continue For
                Dim rc = taskAlg.redList.oldrclist(index)
                DrawTour(dst3(rc.rect), rc.contour, rc.color, -1)
                dst3(rc.rect).SetTo(rc.color, rc.mask)
            Next
            labels(3) = CStr(count) + " cells were found using the motion mask"
        End Sub
        Public Sub Close()
            RedList_FindBricks_Close(cPtr)
        End Sub
    End Class









    '  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
    ' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
    Public Class XO_RedList_Planes : Inherits TaskParent
        Public planes As New XO_RedList_PlaneColor
        Public Sub New()
            desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            planes.Run(src)
            dst2 = planes.dst2
            dst3 = planes.dst3
            labels = planes.labels
        End Sub
    End Class








    Public Class XO_RedList_Equations : Inherits TaskParent
        Dim eq As New Plane_Equation
        Public oldrclist As New List(Of oldrcData)
        Public Sub New()
            labels(3) = "The estimated plane equations for the largest 20 RedCloud cells."
            desc = "Show the estimated plane equations for all the cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                dst2 = runRedList(src, labels(2))
                oldrclist = New List(Of oldrcData)(taskAlg.redList.oldrclist)
            End If

            Dim newCells As New List(Of oldrcData)
            For Each orc As oldrcData In oldrclist
                If orc.contour.Count > 4 Then
                    eq.rc = orc
                    eq.Run(src)
                    newCells.Add(eq.rc)
                End If
            Next

            oldrclist = New List(Of oldrcData)(newCells)

            If taskAlg.heartBeat Then
                Dim index As Integer
                strOut = ""
                For Each rc In oldrclist
                    If rc.contour.Count > 4 Then
                        Dim justEquation = Format(rc.eq(0), fmt3) + "*X + " + Format(rc.eq(1), fmt3) + "*Y + "
                        justEquation += Format(rc.eq(2), fmt3) + "*Z + " + Format(rc.eq(3), fmt3) + vbCrLf
                        strOut += justEquation
                        index += 1
                        If index >= 20 Then Exit For
                    End If
                Next
            End If

            SetTrueText(strOut, 3)
        End Sub
    End Class








    Public Class XO_RedList_CellsAtDepth : Inherits TaskParent
        Dim plot As New Plot_Histogram
        Public Sub New()
            taskAlg.kalman = New Kalman_Basics
            plot.removeZeroEntry = False
            taskAlg.gOptions.HistBinBar.Value = 20
            labels(3) = "Use mouse to select depth to highlight.  Histogram shows count of cells at each depth."
            desc = "Create a histogram of depth using RedCloud cells"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            Dim histBins = taskAlg.histogramBins
            Dim slotList(histBins) As List(Of Integer)
            For i = 0 To slotList.Count - 1
                slotList(i) = New List(Of Integer)
            Next
            Dim hist(histBins - 1) As Single
            For Each rc In taskAlg.redList.oldrclist
                Dim slot As Integer
                If rc.depth > taskAlg.MaxZmeters Then rc.depth = taskAlg.MaxZmeters
                slot = rc.depth \ taskAlg.MaxZmeters * histBins
                If slot >= hist.Length Then slot = hist.Length - 1
                slotList(slot).Add(rc.index)
                hist(slot) += rc.pixels
            Next

            taskAlg.kalman.kInput = hist
            taskAlg.kalman.Run(emptyMat)

            Dim histMat = cv.Mat.FromPixelData(histBins, 1, cv.MatType.CV_32F, taskAlg.kalman.kOutput)
            plot.Run(histMat)
            dst3 = plot.dst2

            Dim barWidth = dst3.Width / histBins
            Dim histIndex = Math.Floor(taskAlg.mouseMovePoint.X / barWidth)
            If histIndex >= slotList.Count() Then histIndex = slotList.Count() - 1
            dst3.Rectangle(New cv.Rect(CInt(histIndex * barWidth), 0, barWidth, dst3.Height), cv.Scalar.Yellow, taskAlg.lineWidth)
            For i = 0 To slotList(histIndex).Count - 1
                Dim rc = taskAlg.redList.oldrclist(slotList(histIndex)(i))
                DrawTour(dst2(rc.rect), rc.contour, cv.Scalar.Yellow)
                DrawTour(taskAlg.color(rc.rect), rc.contour, cv.Scalar.Yellow)
            Next
        End Sub
    End Class








    Public Class XO_RedList_ShapeCorrelation : Inherits TaskParent
        Public Sub New()
            desc = "A shape correlation is between each x and y in list of contours points.  It allows classification based on angle and shape."
        End Sub
        Public Shared Function shapeCorrelation(points As List(Of cv.Point)) As Single
            Dim pts As cv.Mat = cv.Mat.FromPixelData(points.Count, 1, cv.MatType.CV_32SC2, points.ToArray)
            Dim pts32f As New cv.Mat
            pts.ConvertTo(pts32f, cv.MatType.CV_32FC2)
            Dim split = pts32f.Split()
            Dim correlationMat As New cv.Mat
            cv.Cv2.MatchTemplate(split(0), split(1), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
            Return correlationMat.Get(Of Single)(0, 0)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            Dim rc = taskAlg.oldrcD
            If rc.contour.Count > 0 Then
                Dim shape = shapeCorrelation(rc.contour)
                strOut = "Contour correlation for selected cell contour X to Y = " + Format(shape, fmt3) + vbCrLf + vbCrLf +
                     "Select different cells and notice the pattern for the correlation of the contour.X to contour.Y values:" + vbCrLf +
                     "(The contour correlation - contour.x to contour.y - Is computed above.)" + vbCrLf + vbCrLf +
                     "If shape leans left, correlation Is positive And proportional to the lean." + vbCrLf +
                     "If shape leans right, correlation Is negative And proportional to the lean. " + vbCrLf +
                     "If shape Is symmetric (i.e. rectangle Or circle), correlation Is near zero." + vbCrLf +
                     "(Remember that Y increases from the top of the image to the bottom.)"
            End If

            SetTrueText(strOut, 3)
        End Sub
    End Class









    '  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
    ' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
    Public Class XO_RedList_PlaneColor : Inherits TaskParent
        Public options As New Options_Plane
        Dim planeCells As New Plane_CellColor
        Public Sub New()
            labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
            desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst2 = runRedList(src, labels(2))

            dst3.SetTo(0)
            Dim fitPoints As New List(Of cv.Point3f)
            For Each rc In taskAlg.redList.oldrclist
                If rc.eq = newVec4f Then
                    rc.eq = New cv.Vec4f
                    If options.useMaskPoints Then
                        rc.eq = Plane_Basics.fitDepthPlane(planeCells.buildMaskPointEq(rc))
                    ElseIf options.useContourPoints Then
                        rc.eq = Plane_Basics.fitDepthPlane(planeCells.buildContourPoints(rc))
                    ElseIf options.use3Points Then
                        rc.eq = Plane_Basics.build3PointEquation(rc)
                    End If
                End If
                dst3(rc.rect).SetTo(New cv.Scalar(Math.Abs(255 * rc.eq(0)),
                                              Math.Abs(255 * rc.eq(1)),
                                              Math.Abs(255 * rc.eq(2))), rc.mask)
            Next
        End Sub
    End Class






    '  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
    ' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
    Public Class XO_RedList_PlaneFromContour : Inherits TaskParent
        Public Sub New()
            labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
            desc = "Create a plane equation each cell's contour"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then dst2 = runRedList(src, labels(2))

            Dim rc = taskAlg.oldrcD
            Dim fitPoints As New List(Of cv.Point3f)
            For Each pt In rc.contour
                If pt.X >= rc.rect.Width Or pt.Y >= rc.rect.Height Then Continue For
                If rc.mask.Get(Of Byte)(pt.Y, pt.X) = 0 Then Continue For
                fitPoints.Add(taskAlg.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X))
            Next
            rc.eq = Plane_Basics.fitDepthPlane(fitPoints)
            If standaloneTest() Then
                dst3.SetTo(0)
                dst3(rc.rect).SetTo(New cv.Scalar(Math.Abs(255 * rc.eq(0)), Math.Abs(255 * rc.eq(1)), Math.Abs(255 * rc.eq(2))), rc.mask)
            End If
        End Sub
    End Class







    '  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
    ' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
    Public Class XO_RedList_PlaneFromMask : Inherits TaskParent
        Public Sub New()
            labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
            desc = "Create a plane equation from the pointcloud samples in a RedCloud cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then dst2 = runRedList(src, labels(2))

            Dim rc = taskAlg.oldrcD
            Dim fitPoints As New List(Of cv.Point3f)
            For y = 0 To rc.rect.Height - 1
                For x = 0 To rc.rect.Width - 1
                    If rc.mask.Get(Of Byte)(y, x) Then fitPoints.Add(taskAlg.pointCloud(rc.rect).Get(Of cv.Point3f)(y, x))
                Next
            Next
            rc.eq = Plane_Basics.fitDepthPlane(fitPoints)
            If standaloneTest() Then
                dst3.SetTo(0)
                dst3(rc.rect).SetTo(New cv.Scalar(Math.Abs(255 * rc.eq(0)), Math.Abs(255 * rc.eq(1)), Math.Abs(255 * rc.eq(2))), rc.mask)
            End If
        End Sub
    End Class








    Public Class XO_RedList_PlaneEq3D : Inherits TaskParent
        Dim eq As New Plane_Equation
        Public Sub New()
            desc = "If a RedColor cell contains depth then build a plane equation"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            Dim rc = taskAlg.oldrcD
            If rc.mmZ.maxVal Then
                eq.rc = rc
                eq.Run(src)
                rc = eq.rc
            End If

            dst3.SetTo(0)
            DrawTour(dst3(rc.rect), rc.contour, rc.color, -1)

            SetTrueText(eq.strOut, 3)
        End Sub
    End Class









    Public Class XO_RedList_UnstableCells : Inherits TaskParent
        Dim prevList As New List(Of cv.Point)
        Public Sub New()
            labels = {"", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing"}
            desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            If taskAlg.heartBeat Or taskAlg.frameCount = 2 Then
                dst1 = dst2.Clone
                dst3.SetTo(0)
            End If

            Dim currList As New List(Of cv.Point)
            For Each rc In taskAlg.redList.oldrclist
                If prevList.Contains(rc.maxDStable) = False Then
                    DrawTour(dst1(rc.rect), rc.contour, white, -1)
                    DrawTour(dst1(rc.rect), rc.contour, cv.Scalar.Black)
                    DrawTour(dst3(rc.rect), rc.contour, white, -1)
                End If
                currList.Add(rc.maxDStable)
            Next

            prevList = New List(Of cv.Point)(currList)
        End Sub
    End Class








    Public Class XO_RedList_UnstableHulls : Inherits TaskParent
        Dim prevList As New List(Of cv.Point)
        Public Sub New()
            labels = {"", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing"}
            desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            If taskAlg.heartBeat Or taskAlg.frameCount = 2 Then
                dst1 = dst2.Clone
                dst3.SetTo(0)
            End If

            Dim currList As New List(Of cv.Point)
            For Each rc In taskAlg.redList.oldrclist
                rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
                If prevList.Contains(rc.maxDStable) = False Then
                    DrawTour(dst1(rc.rect), rc.hull, white, -1)
                    DrawTour(dst1(rc.rect), rc.hull, cv.Scalar.Black)
                    DrawTour(dst3(rc.rect), rc.hull, white, -1)
                End If
                currList.Add(rc.maxDStable)
            Next

            prevList = New List(Of cv.Point)(currList)
        End Sub
    End Class









    Public Class XO_Color8U_MotionFiltered : Inherits TaskParent
        Dim color8U As New XO_Color8U_Sweep
        Public classCount As Integer
        Dim motion As New XO_Motion_BGSub
        Public Sub New()
            desc = "Prepare a Color8U_Basics image using the taskAlg.motionMask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.motionMask.CountNonZero Then
                src.SetTo(0, Not taskAlg.motionMask)
                color8U.Run(src)
                dst2 = color8U.dst3
                dst2.CopyTo(dst3, taskAlg.motionMask)
                dst2.SetTo(0, Not taskAlg.motionMask)
                classCount = color8U.classCount
            End If
            If taskAlg.heartBeatLT Then dst3.SetTo(0)
            labels(2) = color8U.strOut
        End Sub
    End Class

End Namespace
