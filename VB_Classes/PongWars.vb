Imports cv = OpenCvSharp
' https://github.com/vnglst/pong-wars
' https://twitter.com/nicolasdnl/status/1749715070928433161
Public Class PongWars_Basics : Inherits TaskParent
    Dim sqWidth As Integer = 25
    Dim sqHeight As Integer = 25 * task.dst2.Height / task.dst2.Width
    Dim numSquaresX As Integer = task.dst2.Width / sqWidth
    Dim numSquaresY As Integer = task.dst2.Height / sqHeight

    Dim DAY_COLOR = 1, DAY_BALL_COLOR = 2, NIGHT_COLOR = 3, NIGHT_BALL_COLOR = 4
    Dim squares(numSquaresX - 1, numSquaresY - 1) As Integer

    Dim p1 = New cv.Point(task.dst2.Width / 4, task.dst2.Height / 2)
    Dim d1 As cv.Point2f = New cv.Point(12.5, -12.5)

    Dim p2 = New cv.Point((task.dst2.Width / 4) * 3, task.dst2.Height / 2)
    Dim d2 As cv.Point2f = New cv.Point(-12.5, 12.5)

    Dim iteration As Integer = 0
    Dim p1Last As New cv.Point, p2Last As New cv.Point
    Public Sub New()
        For i As Integer = 0 To numSquaresX - 1
            For j As Integer = 0 To numSquaresY - 1
                squares(i, j) = If(i < numSquaresX / 2, DAY_COLOR, NIGHT_COLOR)
            Next
        Next
        p1 = New cv.Point(msRNG.Next(0, dst2.Width / 4), msRNG.Next(0, dst2.Height / 2))
        p2 = New cv.Point(msRNG.Next(dst2.Width / 2, dst2.Width), msRNG.Next(dst2.Height / 4, dst2.Height))
        UpdateAdvice(traceName + ": <place advice here on any options that are useful>")
        desc = "Pong as war between the forces of light and darkness."
    End Sub
    Private Function UpdateSquareAndBounce(pt As cv.Point, dxy As cv.Point2f, sqClass As Integer) As cv.Point2f
        For angle As Double = 0 To Math.PI * 2 Step Math.PI / 4
            Dim checkX As Double = pt.X + Math.Cos(angle) * (sqWidth / 2)
            Dim checkY As Double = pt.Y + Math.Sin(angle) * (sqHeight / 2)
            Dim i As Integer = Math.Floor(checkX / sqWidth)
            Dim j As Integer = Math.Floor(checkY / sqHeight)

            If i >= 0 AndAlso i < numSquaresX AndAlso j >= 0 AndAlso j < numSquaresY Then
                If squares(i, j) <> sqClass Then
                    squares(i, j) = sqClass

                    If Math.Abs(Math.Cos(angle)) > Math.Abs(Math.Sin(angle)) Then
                        dxy.X = -dxy.X
                    Else
                        dxy.Y = -dxy.Y
                    End If
                End If
            End If
        Next

        Return dxy
    End Function
    Private Function CheckBoundaryCollision(pt As cv.Point, dxy As cv.Point2f) As cv.Point2f
        If pt.X + dxy.X > dst2.Width - sqWidth / 2 OrElse pt.X + dxy.X < sqWidth / 2 Then dxy.X = -dxy.X
        If pt.Y + dxy.Y > dst2.Height - sqHeight / 2 OrElse pt.Y + dxy.Y < sqHeight / 2 Then dxy.Y = -dxy.Y
        Return dxy
    End Function
    Private Sub UpdateScoreElement()
        Dim dayScore As Integer = 0
        Dim nightScore As Integer = 0

        For i As Integer = 0 To numSquaresX - 1
            For j As Integer = 0 To numSquaresY - 1
                If squares(i, j) = DAY_COLOR Then
                    dayScore += 1
                ElseIf squares(i, j) = NIGHT_COLOR Then
                    nightScore += 1
                End If
            Next
        Next

        If task.heartBeat Then labels(2) = $"Pong War: day {dayScore} | night {nightScore}"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        iteration += 1
        If iteration Mod 1000 = 0 Then
            debug.writeline("iteration " & iteration)
        End If

        d1 = UpdateSquareAndBounce(p1, d1, DAY_COLOR)
        d2 = UpdateSquareAndBounce(p2, d2, NIGHT_COLOR)

        d1 = CheckBoundaryCollision(p1, d1)
        d2 = CheckBoundaryCollision(p2, d2)

        p1.x += d1.X
        p1.y += d1.Y
        p2.x += d2.X
        p2.y += d2.Y

        If p1Last = p1 Then p1 = New cv.Point(msRNG.Next(0, dst2.Width / 2), msRNG.Next(0, dst2.Height / 2))
        p1Last = p1
        If p2Last = p2 Then p2 = New cv.Point(msRNG.Next(0, dst2.Width / 2), msRNG.Next(0, dst2.Height / 2))
        p2Last = p2

        UpdateScoreElement()

        dst2.SetTo(0)
        For i As Integer = 0 To numSquaresX - 1
            For j As Integer = 0 To numSquaresY - 1
                Dim rect = New cv.Rect(i * sqWidth, j * sqHeight, sqWidth, sqHeight)
                Dim index = squares(i, j)
                dst2.Rectangle(rect, task.scalarColors(index), -1)
            Next
        Next

        Dim pt = New cv.Point(CInt(p1.x - sqWidth / 2), CInt(p1.y - sqHeight / 2))
        DrawCircle(dst2, pt, task.DotSize + 5, task.scalarColors(DAY_BALL_COLOR))

        pt = New cv.Point(CInt(p2.x - sqWidth / 2), CInt(p2.y - sqHeight / 2))
        DrawCircle(dst2, pt, task.DotSize + 5, task.scalarColors(NIGHT_BALL_COLOR))
    End Sub
End Class








Public Class PongWars_Two : Inherits TaskParent
    Dim pong1 As New PongWars_Basics
    Dim pong2 As New PongWars_Basics
    Public Sub New()
        desc = "Running 2 pong wars at once.  Randomness inserted with starting location."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        pong1.Run(src)
        dst2 = pong1.dst2.Clone
        labels(2) = pong1.labels(2)

        pong2.Run(src)
        dst3 = pong2.dst2.Clone
        labels(3) = pong2.labels(2)
    End Sub
End Class
