Imports VB_Classes.Options_Reduction
Imports cv = OpenCvSharp
Public Class Gravity_Basics : Inherits TaskParent
    Public options As New Options_Features
    Dim gravityRaw As New Gravity_BasicsRaw
    Public trackLine As New TrackLine_Basics
    Public gravityRGB As lpData
    Public Sub New()
        desc = "Use the slope of the longest RGB line to figure out if camera moved enough to obtain the IMU gravity vector."
    End Sub
    Public Shared Sub showVectors(dst As cv.Mat)
        dst.Line(task.gravityVec.p1, task.gravityVec.p2, white, task.lineWidth, task.lineType)
        dst.Line(task.horizonVec.p1, task.horizonVec.p2, white, task.lineWidth, task.lineType)
        dst.Line(task.gravityBasics.gravityRGB.p1, task.gravityBasics.gravityRGB.p2, task.highlight, task.lineWidth + 1, task.lineType)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        gravityRaw.Run(emptyMat)
        trackLine.Run(src)
        gravityRGB = trackLine.lp

        Dim deltaX1 = Math.Abs(task.gravityVec.ep1.X - gravityRGB.ep1.X)
        Dim deltaX2 = Math.Abs(task.gravityVec.ep2.X - gravityRGB.ep2.X)
        If Math.Abs(deltaX1 - deltaX2) > options.pixelThreshold Then
            task.gravityVec = task.gravityIMU
        End If

        task.horizonVec = LineRGB_Perpendicular.computePerp(task.gravityVec)

        If standaloneTest() Then
            dst2.SetTo(0)
            showVectors(dst2)
            dst3 = task.lineRGB.dst3
            labels(3) = task.lineRGB.labels(3)
        End If
    End Sub
End Class







Public Class Gravity_BasicsRaw : Inherits TaskParent
    Public xTop As Single, xBot As Single
    Dim sampleSize As Integer = 25
    Dim ptList As New List(Of Integer)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(2) = "Horizon and Gravity Vectors"
        desc = "Improved method to find gravity and horizon vectors"
    End Sub
    Private Function findFirst(points As cv.Mat) As Single
        ptList.Clear()

        For i = 0 To Math.Min(sampleSize, points.Rows / 2)
            Dim pt = points.Get(Of cv.Point)(i, 0)
            If pt.X <= 0 Or pt.Y < 0 Then Continue For
            If pt.X > dst2.Width Or pt.Y > dst2.Height Then Continue For
            ptList.Add(pt.X)
        Next

        If ptList.Count = 0 Then Return 0
        Return ptList.Average()
    End Function
    Private Function findLast(points As cv.Mat) As Single
        ptList.Clear()

        For i = points.Rows To Math.Max(points.Rows - sampleSize, points.Rows / 2) Step -1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            If pt.X <= 5 Or pt.Y <= 5 Then Continue For
            If pt.X > dst2.Width Or pt.Y > dst2.Height Then Continue For
            ptList.Add(pt.X)
        Next

        If ptList.Count = 0 Then Return 0
        Return ptList.Average()
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim threshold As Single = 0.015

        dst3 = task.splitOriginalCloud(0).InRange(-threshold, threshold)
        dst3.SetTo(0, task.noDepthMask)
        Dim gPoints = dst3.FindNonZero()
        If gPoints.Rows = 0 Then Exit Sub ' no point cloud data to get the gravity line in the image coordinates.
        xTop = findFirst(gPoints)
        xBot = findLast(gPoints)
        task.gravityIMU = New lpData(New cv.Point2f(xTop, 0), New cv.Point2f(xBot, dst2.Height))

        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, task.gravityIMU.p1, task.gravityIMU.p2, task.highlight)
        End If
    End Sub
End Class






Public Class Gravity_BasicsKalman : Inherits TaskParent
    Dim kalman As New Kalman_Basics
    Dim gravity As New Gravity_BasicsRaw
    Public Sub New()
        desc = "Use kalman to smooth gravity and horizon vectors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        gravity.Run(src)

        kalman.kInput = {task.gravityVec.ep1.X, task.gravityVec.ep1.Y, task.gravityVec.ep2.X, task.gravityVec.ep2.Y}
        kalman.Run(emptyMat)
        task.gravityVec = New lpData(New cv.Point2f(kalman.kOutput(0), kalman.kOutput(1)),
                                     New cv.Point2f(kalman.kOutput(2), kalman.kOutput(3)))

        task.horizonVec = LineRGB_Perpendicular.computePerp(task.gravityVec)

        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, task.highlight)
            DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.Red)
        End If
    End Sub
End Class








Public Class Gravity_RGB : Inherits TaskParent
    Dim survey As New BrickPoint_PopulationSurvey
    Public Sub New()
        desc = "Rotate the RGB image using the offset from gravity."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static rotateAngle As Double = task.verticalizeAngle - 2
        Static rotateCenter = New cv.Point2f(dst2.Width / 2, dst2.Height / 2)

        rotateAngle += 0.1
        If rotateAngle >= task.verticalizeAngle + 2 Then rotateAngle = task.verticalizeAngle - 2

        Dim M = cv.Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
        dst3 = src.WarpAffine(M, src.Size(), cv.InterpolationFlags.Nearest)

        survey.Run(dst3)
        dst2 = survey.dst2

        Dim incrX = dst1.Width / task.cellSize
        Dim incrY = dst1.Height / task.cellSize
        For y = 0 To task.cellSize - 1
            For x = 0 To task.cellSize - 1
                SetTrueText(CStr(survey.results(x, y)), New cv.Point(x * incrX, y * incrY), 2)
            Next
        Next
    End Sub
End Class






Public Class Gravity_BrickRotate : Inherits TaskParent
    Dim survey As New BrickPoint_PopulationSurvey
    Public Sub New()
        task.brickRunFlag = True
        desc = "Rotate the grid point using the offset from gravity."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim angle = Math.Abs(task.verticalizeAngle)
        Static rotateAngle As Double = -angle
        Static rotateCenter = New cv.Point2f(dst2.Width / 2, dst2.Height / 2)

        rotateAngle += 0.1
        If rotateAngle >= angle Then rotateAngle = -angle

        dst1 = src
        For Each brick In task.bricks.brickList
            If brick.pt.Y = brick.rect.Y Then dst1.Circle(brick.pt, task.DotSize, task.highlight, -1, task.lineType)
        Next

        Dim M = cv.Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
        dst3 = dst1.WarpAffine(M, dst1.Size(), cv.InterpolationFlags.Nearest)

        survey.Run(dst3)
        dst2 = survey.dst2

        Dim incrX = dst1.Width / task.cellSize
        Dim incrY = dst1.Height / task.cellSize
        For y = 0 To task.cellSize - 1
            For x = 0 To task.cellSize - 1
                SetTrueText(CStr(survey.results(x, y)), New cv.Point(x * incrX, y * incrY), 2)
            Next
        Next
    End Sub
End Class





Public Class Gravity_BasicsOld : Inherits TaskParent
    Public points As New List(Of cv.Point2f)
    Public autoDisplay As Boolean
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
        If src.Type <> cv.MatType.CV_32F Then dst0 = task.pcSplit(0) Else dst0 = src

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
            task.gravityVec = New lpData(lp.ep1, lp.ep2)
            If standaloneTest() Or autoDisplay Then displayResults(p1, p2)
        End If

        task.horizonVec = LineRGB_Perpendicular.computePerp(task.gravityVec)
        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class Gravity_BasicsOriginal : Inherits TaskParent
    Public vec As New lpData
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Search for the transition from positive to negative to find the gravity vector."
    End Sub
    Public Shared Function PrepareDepthInput(index As Integer) As cv.Mat
        If task.useGravityPointcloud Then Return task.pcSplit(index) ' already oriented to gravity

        ' rebuild the pointcloud so it is oriented to gravity.
        Dim pc = (task.pointCloud.Reshape(1, task.pointCloud.Rows * task.pointCloud.Cols) * task.gMatrix).ToMat.Reshape(3, task.pointCloud.Rows)
        Dim split = pc.Split()
        Return split(index)
    End Function
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
                    If ptX.Count >= task.gOptions.FrameHistory.Value Then Return New cv.Point2f(ptX.Average, ptY.Average)
                End If
            Next
        Next
        Return New cv.Point
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = task.pcSplit(0) Else dst0 = src

        Dim p1 = findTransition(0, dst0.Height - 1, 1)
        Dim p2 = findTransition(dst0.Height - 1, 0, -1)
        Dim lp = New lpData(p1, p2)
        vec = New lpData(lp.ep1, lp.ep2)

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





