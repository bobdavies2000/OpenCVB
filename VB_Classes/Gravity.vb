Imports cv = OpenCvSharp
Public Class Gravity_Basics : Inherits TaskParent
    Public options As New Options_GravityLines
    Dim gravityRaw As New Gravity_BasicsRaw
    Dim imuGravityCount As Integer
    Public featLine As New FeatureLine_Basics
    Public cameraMotionProxy As lpData
    Public gravityRGB As lpData
    Public gravityMatch As New LineRGB_MatchGravity
    Public Sub New()
        desc = "Use the slope of the longest RGB line to figure out if camera moved enough to obtain the IMU gravity vector."
    End Sub
    Private Shared Sub showVec(dst As cv.Mat, vec As lpData)
        dst.Line(vec.p1, vec.p2, task.highlight, task.lineWidth * 2, task.lineType)
        Dim gIndex = task.grid.gridMap.Get(Of Single)(vec.p1.Y, vec.p1.X)
        Dim firstRect = task.gridNabeRects(gIndex)
        gIndex = task.grid.gridMap.Get(Of Single)(vec.p2.Y, vec.p2.X)
        Dim lastRect = task.gridNabeRects(gIndex)
        dst.Rectangle(firstRect, task.highlight, task.lineWidth)
        dst.Rectangle(lastRect, task.highlight, task.lineWidth)
    End Sub
    Public Shared Sub showVectors(dst As cv.Mat)
        dst.Line(task.horizonVec.p1, task.horizonVec.p2, white, task.lineWidth, task.lineType)
        dst.Line(task.gravityVec.p1, task.gravityVec.p2, white, task.lineWidth, task.lineType)

        showVec(dst, task.gravityBasics.cameraMotionProxy)
        showVec(dst, task.gravityBasics.gravityRGB)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.firstPass Then gravityRaw.Run(emptyMat)

        gravityMatch.Run(src)
        If gravityMatch.gLines.Count > 0 Then gravityRGB = gravityMatch.gLines(0)

        Static savedLine = task.gravityVec
        featLine.Run(src)

        cameraMotionProxy = featLine.cameraMotionProxy
        If CInt(savedLine.ep1.X) <> CInt(cameraMotionProxy.ep1.X) Or
           CInt(savedLine.ep1.Y) <> CInt(cameraMotionProxy.ep1.Y) Or
           CInt(savedLine.ep2.X) <> CInt(cameraMotionProxy.ep2.X) Or
           CInt(savedLine.ep2.Y) <> CInt(cameraMotionProxy.ep2.Y) Or
           task.lineRGB.lpList.Count = 0 Then ' No lines to confirm the gravity vector means use raw gravity vector.

            imuGravityCount += 1
            gravityRaw.Run(src)
            savedLine = featLine.cameraMotionProxy
            task.gravityVec = gravityRaw.gravityVec
        End If

        task.horizonVec = LineRGB_Perpendicular.computePerp(task.gravityVec)

        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, task.highlight)
            DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.Red)
            dst3 = src
            dst3.Line(featLine.cameraMotionProxy.p1, featLine.cameraMotionProxy.p2, task.highlight, task.lineWidth, task.lineType)
        End If
        labels(2) = "IMU gravity use " + CStr(imuGravityCount) + " times or " + Format(imuGravityCount / task.frameCount, "0%")
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

        kalman.kInput = {gravity.gravityVec.ep1.X, gravity.gravityVec.ep1.Y, gravity.gravityVec.ep2.X, gravity.gravityVec.ep2.Y}
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







Public Class Gravity_BasicsRaw : Inherits TaskParent
    Public xTop As Single, xBot As Single
    Dim sampleSize As Integer = 25
    Dim ptList As New List(Of Integer)
    Public gravityVec As lpData
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
        gravityVec = New lpData(New cv.Point2f(xTop, 0), New cv.Point2f(xBot, dst2.Height))

        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, gravityVec.p1, gravityVec.p2, task.highlight)
        End If

        If task.gravityBasics.gravityRGB IsNot Nothing Then
            Dim deltaX1 = Math.Abs(gravityVec.ep1.X - task.gravityBasics.gravityRGB.ep1.X)
            Dim deltaX2 = Math.Abs(gravityVec.ep2.X - task.gravityBasics.gravityRGB.ep2.X)
            If Math.Abs(deltaX1 - deltaX2) < task.gravityBasics.options.pixelThreshold Then
                gravityVec.ep1 = task.gravityBasics.gravityRGB.ep1
                gravityVec.ep2 = task.gravityBasics.gravityRGB.ep2
            End If
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

