Imports cv = OpenCvSharp
Public Class Gravity_Basics : Inherits TaskParent
    Public points As New List(Of cv.Point2f)
    Public autoDisplay As Boolean
    Dim perp As New Line_Perpendicular
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
            If standaloneTest() Or autoDisplay Then displayResults(p1, p2)
        End If

        task.horizonVec = perp.computePerp(task.gravityVec)
        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class Gravity_BasicsOriginal : Inherits TaskParent
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






Public Class Gravity_HorizonCompare : Inherits TaskParent
    Dim gravity As New Gravity_Basics
    Dim horizon As New XO_Horizon_Basics
    Public Sub New()
        gravity.autoDisplay = True
        horizon.autoDisplay = True
        desc = "Collect results from Horizon_Basics with Gravity_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        gravity.Run(src)
        Dim g1 = task.gravityVec
        Dim h1 = task.gravityVec

        horizon.Run(src)
        Dim g2 = task.horizonVec
        Dim h2 = task.horizonVec

        If standaloneTest() Then
            SetTrueText(strOut, 3)

            dst2.SetTo(0)
            DrawLine(dst2, g1.p1, g1.p2, task.HighlightColor)
            DrawLine(dst2, g2.p1, g2.p2, task.HighlightColor)

            DrawLine(dst2, h1.p1, h1.p2, cv.Scalar.Red)
            DrawLine(dst2, h2.p1, h2.p2, cv.Scalar.Red)
        End If
    End Sub
End Class









Public Class Gravity_HorizonOriginal : Inherits TaskParent
    Dim gravity As New Gravity_Basics
    Dim horizon As New XO_Horizon_Basics
    Dim lastVec As linePoints
    Public Sub New()
        gravity.autoDisplay = True
        horizon.autoDisplay = True
        labels(2) = "Gravity vector in yellow and Horizon vector in red."
        desc = "Compute the gravity vector and the horizon vector separately"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        gravity.Run(src)

        horizon.Run(src)
        If task.firstPass Then lastVec = horizon.vec
        If horizon.vec.p1.Y > 0 Then lastVec = horizon.vec
        If lastVec IsNot Nothing And horizon.vec.p1.Y = 0 Then horizon.vec = lastVec

        task.horizonVec = horizon.vec
        If standaloneTest() Then
            SetTrueText("Gravity vector (yellow):" + vbCrLf + gravity.strOut + vbCrLf + vbCrLf + "Horizon Vector (red): " + vbCrLf + horizon.strOut, 3)
            dst2.SetTo(0)
            DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, task.HighlightColor)
            DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.Red)
        End If
    End Sub
End Class












Public Class Gravity_Raw : Inherits TaskParent
    Public xTop As Integer, xBot As Integer
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(2) = "Horizon and Gravity Vectors"
        desc = "Improved method to find gravity and horizon vectors"
    End Sub
    Private Function findFirst(points As cv.Mat) As Integer
        Dim ptList As New List(Of Integer)

        For i = 0 To Math.Min(10, points.Rows / 2)
            Dim pt = points.Get(Of cv.Point)(i, 0)
            If pt.X <= 0 Or pt.Y <= 0 Then Continue For
            If pt.X > dst2.Width Or pt.Y > dst2.Height Then Continue For
            ptList.Add(pt.X)
        Next

        If ptList.Count = 0 Then Return 0
        Return ptList.Average()
    End Function
    Private Function findLast(points As cv.Mat) As Integer
        Dim ptList As New List(Of Integer)

        For i = points.Rows To Math.Max(points.Rows - 10, points.Rows / 2) Step -1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            If pt.X <= 0 Or pt.Y <= 0 Then Continue For
            If pt.X > dst2.Width Or pt.Y > dst2.Height Then Continue For
            ptList.Add(pt.X)
        Next

        If ptList.Count = 0 Then Return 0
        Return ptList.Average()
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim threshold As Single = 0.015
        Dim work As New cv.Mat

        work = task.pcSplit(0).InRange(-threshold, threshold)
        work.SetTo(0, task.noDepthMask)
        work.ConvertTo(dst3, cv.MatType.CV_8U)
        Dim gPoints = dst3.FindNonZero()
        If gPoints.Rows = 0 Then Exit Sub ' no point cloud data to get the gravity line in the image coordinates.
        xTop = findFirst(gPoints)
        xBot = findLast(gPoints)

        If standaloneTest() Then
            Dim gravityVec = New linePoints(New cv.Point(xTop, 0), New cv.Point(xBot, dst2.Height))

            dst2.SetTo(0)
            DrawLine(dst2, gravityVec.p1, gravityVec.p2, task.HighlightColor)
        End If
    End Sub
End Class






Public Class Gravity_Horizon : Inherits TaskParent
    Dim ghRaw As New Gravity_Raw
    Dim kalman As New Kalman_Basics
    Dim line As New Line_Perpendicular
    Public Sub New()
        ReDim kalman.kInput(2 - 1)
        desc = "Use kalman to smooth gravity and horizon vectors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ghRaw.Run(src)

        With kalman
            .kInput = {ghRaw.xBot, ghRaw.xTop}
            .Run(src)

            task.gravityVec = New linePoints(New cv.Point2f(.kOutput(1), 0),
                                             New cv.Point2f(.kOutput(0), dst2.Height))
            task.horizonVec = line.computePerp(task.gravityVec)
        End With

        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, task.HighlightColor)
            DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.Red)
        End If
    End Sub
End Class