﻿Imports cv = OpenCvSharp
Public Class Gravity_Basics : Inherits TaskParent
    Dim gravity As New Gravity_Raw
    Public Sub New()
        desc = "Use kalman to smooth gravity and horizon vectors."
    End Sub
    Public Function computePerp(lp As lpData) As lpData
        Dim midPoint = New cv.Point2f((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2)
        Dim m = If(lp.slope = 0, 100000, -1 / lp.slope)
        Dim b = midPoint.Y - m * midPoint.X
        Dim p1 = New cv.Point2f(-b / m, 0)
        Dim p2 = New cv.Point2f((dst2.Height - b) / m, dst2.Height)

        Dim w = task.workingRes.Width
        Dim h = task.workingRes.Height

        If p1.X < 0 Then p1 = New cv.Point2f(0, b)
        If p1.X > w Then p1 = New cv.Point2f(w, m * w + b)
        If p1.Y < 0 Then p1 = New cv.Point2f(-b / m, 0)
        If p1.Y > h Then p1 = New cv.Point2f(w, m * w + b)

        If p2.X < 0 Then p2 = New cv.Point2f(0, b)
        If p2.X > w Then p2 = New cv.Point2f(w, m * w + b)
        If p2.Y < 0 Then p2 = New cv.Point2f(-b / m, 0)
        If p2.Y > h Then p2 = New cv.Point2f(w, m * w + b)

        Return New lpData(p1, p2)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        gravity.Run(src)

        task.gravityVec = New lpData(New cv.Point2f(gravity.xTop, 0), New cv.Point2f(gravity.xBot, dst2.Height))
        task.horizonVec = computePerp(task.gravityVec)

        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, task.highlight)
            DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.Red)
        End If
    End Sub
End Class







Public Class Gravity_Raw : Inherits TaskParent
    Public xTop As Integer, xBot As Integer
    Dim sampleSize As Integer = 25
    Dim ptList As New List(Of Integer)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(2) = "Horizon and Gravity Vectors"
        desc = "Improved method to find gravity and horizon vectors"
    End Sub
    Private Function findFirst(points As cv.Mat) As Integer
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
    Private Function findLast(points As cv.Mat) As Integer
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

        If standaloneTest() Then
            Dim gravityVec = New lpData(New cv.Point(xTop, 0), New cv.Point(xBot, dst2.Height))

            dst2.SetTo(0)
            DrawLine(dst2, gravityVec.p1, gravityVec.p2, task.highlight)
        End If
    End Sub
End Class














