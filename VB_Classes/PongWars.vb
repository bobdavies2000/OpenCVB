Imports System.Drawing
Imports cv = OpenCvSharp
' https://github.com/vnglst/pong-wars
Public Class PongWars_Basics : Inherits VB_Algorithm
    Dim SQUARE_SIZE As Integer = 25
    Dim numSquaresX As Integer = task.workingRes.Width / SQUARE_SIZE
    Dim numSquaresY As Integer = task.workingRes.Height / SQUARE_SIZE

    Dim DAY_COLOR = 1
    Dim DAY_BALL_COLOR = 2
    Dim NIGHT_COLOR = 3
    Dim NIGHT_BALL_COLOR = 4

    Dim squares(numSquaresX - 1, numSquaresY - 1) As Integer

    Dim x1 As Integer = task.workingRes.Width / 4
    Dim y1 As Integer = task.workingRes.Height / 2
    Dim dx1 As Double = 12.5
    Dim dy1 As Double = -12.5

    Dim x2 As Integer = (task.workingRes.Width / 4) * 3
    Dim y2 As Integer = task.workingRes.Height / 2
    Dim dx2 As Double = -12.5
    Dim dy2 As Double = 12.5

    Dim iteration As Integer = 0
    Public Sub New()
        For i As Integer = 0 To numSquaresX - 1
            For j As Integer = 0 To numSquaresY - 1
                squares(i, j) = If(i < numSquaresX / 2, DAY_COLOR, NIGHT_COLOR)
            Next
        Next

        vbAddAdvice(traceName + ": <place advice here on any options that are useful>")
        desc = "Pong as war between the forces of light and darkness."
    End Sub
    Private Function UpdateSquareAndBounce(x As Integer, y As Integer, dx As Double, dy As Double, sqClass As Integer) As (dx As Double, dy As Double)
        Dim updatedDx As Double = dx
        Dim updatedDy As Double = dy

        For angle As Double = 0 To Math.PI * 2 Step Math.PI / 4
            Dim checkX As Double = x + Math.Cos(angle) * (SQUARE_SIZE / 2)
            Dim checkY As Double = y + Math.Sin(angle) * (SQUARE_SIZE / 2)
            Dim i As Integer = Math.Floor(checkX / SQUARE_SIZE)
            Dim j As Integer = Math.Floor(checkY / SQUARE_SIZE)

            If i >= 0 AndAlso i < numSquaresX AndAlso j >= 0 AndAlso j < numSquaresY Then
                If squares(i, j) <> sqClass Then
                    squares(i, j) = sqClass

                    If Math.Abs(Math.Cos(angle)) > Math.Abs(Math.Sin(angle)) Then
                        updatedDx = -updatedDx
                    Else
                        updatedDy = -updatedDy
                    End If
                End If
            End If
        Next

        Return (updatedDx, updatedDy)
    End Function
    Private Function CheckBoundaryCollision(x As Integer, y As Integer, dx As Double, dy As Double) As (dx As Double, dy As Double)
        If x + dx > dst2.Width - SQUARE_SIZE / 2 OrElse x + dx < SQUARE_SIZE / 2 Then
            dx = -dx
        End If

        If y + dy > dst2.Height - SQUARE_SIZE / 2 OrElse y + dy < SQUARE_SIZE / 2 Then
            dy = -dy
        End If
        Return (dx, dy)
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

        setTrueText($"Pong War: day {dayScore} | night {nightScore}", 3)
    End Sub
    Public Sub RunVB(src As cv.Mat)
        iteration += 1
        If iteration Mod 1000 = 0 Then
            Console.WriteLine("iteration " & iteration)
        End If

        Static lastBounce1 = (0, 0)
        Dim bounce1 = UpdateSquareAndBounce(x1, y1, dx1, dy1, DAY_COLOR)
        dx1 = bounce1.dx
        dy1 = bounce1.dy
        'If lastBounce1 = bounce1 Then bounce1 = (0, 0)
        'lastBounce1 = bounce1

        Static lastBounce2 = (0, 0)
        Dim bounce2 = UpdateSquareAndBounce(x2, y2, dx2, dy2, NIGHT_COLOR)
        dx2 = bounce2.dx
        dy2 = bounce2.dy
        'If lastBounce2 = bounce2 Then bounce2 = (0, 0)
        'lastBounce2 = bounce2

        Dim boundary1 = CheckBoundaryCollision(x1, y1, dx1, dy1)
        dx1 = boundary1.dx
        dy1 = boundary1.dy

        Dim boundary2 = CheckBoundaryCollision(x2, y2, dx2, dy2)
        dx2 = boundary2.dx
        dy2 = boundary2.dy

        x1 += dx1
        y1 += dy1
        x2 += dx2
        y2 += dy2

        UpdateScoreElement()

        For i As Integer = 0 To numSquaresX - 1
            For j As Integer = 0 To numSquaresY - 1
                Dim rect = New cv.Rect(i * SQUARE_SIZE, j * SQUARE_SIZE, SQUARE_SIZE, SQUARE_SIZE)
                Dim index = squares(i, j)
                dst2.Rectangle(rect, task.scalarColors(index), -1, task.lineType)
            Next
        Next

        Dim pt = New cv.Point(CInt(x1 - SQUARE_SIZE / 2), CInt(y1 - SQUARE_SIZE / 2))
        dst2.Circle(pt, task.dotSize + 5, task.scalarColors(DAY_BALL_COLOR), -1, task.lineType)

        pt = New cv.Point(CInt(x2 - SQUARE_SIZE / 2), CInt(y2 - SQUARE_SIZE / 2))
        dst2.Circle(pt, task.dotSize + 5, task.scalarColors(NIGHT_BALL_COLOR), -1, task.lineType)
    End Sub
End Class
