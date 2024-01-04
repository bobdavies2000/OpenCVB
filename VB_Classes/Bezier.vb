Imports cv = OpenCvSharp
Imports System.Drawing
Imports OpenCvSharp.Flann

Public Class Bezier_Example : Inherits VB_Algorithm
    Public points() As cv.Point = {New cv.Point(task.dotSize, task.dotSize), New cv.Point(dst2.Width / 6, dst2.Width / 6),
                                   New cv.Point(dst2.Width * 3 / 4, dst2.Height / 2),
                                   New cv.Point(dst2.Width - task.dotSize * 2, dst2.Height - task.dotSize * 2)}
    Public Sub New()
        gOptions.dotSizeSlider.Value = 3
        advice = "Update the public points array and then Run."
        desc = "Draw a Bezier curve based with the 4 input points."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2.SetTo(0)
        Dim xPrev As Single, yPrev As Single
        For i = 0 To 100 - 1
            Dim t = i / 100
            Dim x = Math.Pow(1 - t, 3) * points(0).X +
                    3 * t * Math.Pow(1 - t, 2) * points(1).X +
                    3 * Math.Pow(t, 2) * (1 - t) * points(2).X +
                    Math.Pow(t, 3) * points(3).X

            Dim y = Math.Pow(1 - t, 3) * points(0).Y +
                    3 * t * Math.Pow(1 - t, 2) * points(1).Y +
                    3 * Math.Pow(t, 2) * (1 - t) * points(2).Y +
                    Math.Pow(t, 3) * points(3).Y

            If i > 0 Then
                dst2.Line(New cv.Point(CInt(xPrev), CInt(yPrev)), New cv.Point(CInt(x), CInt(y)),
                          task.highlightColor, task.lineWidth)
            End If

            xPrev = x
            yPrev = y
        Next

        For i = 0 To points.Count - 1
            dst2.Circle(points(i), task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next

        dst2.Line(points(0), points(1), cv.Scalar.White, task.lineWidth, task.lineType)
        dst2.Line(points(2), points(3), cv.Scalar.White, task.lineWidth, task.lineType)
    End Sub
End Class








Public Class Bezier_Basics : Inherits VB_Algorithm
    Public points() As cv.Point
    Public Sub New()
        advice = "Update the public points array and then Run."
        points = {New cv.Point(100, 100),
                  New cv.Point(150, 50),
                  New cv.Point(250, 150),
                  New cv.Point(300, 100),
                  New cv.Point(350, 150),
                  New cv.Point(450, 50)}
        desc = "Use n points to draw a Bezier curve."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim p1 As cv.Point
        For i = 0 To points.Count - 4 Step 3
            For j = 0 To 100
                Dim t = j / 100
                Dim x = Math.Pow(1 - t, 3) * points(i).X +
                                3 * t * Math.Pow(1 - t, 2) * points(i + 1).X +
                                3 * Math.Pow(t, 2) * (1 - t) * points(i + 2).X +
                                Math.Pow(t, 3) * points(i + 3).X

                Dim y = Math.Pow(1 - t, 3) * points(i).Y +
                        3 * t * Math.Pow(1 - t, 2) * points(i + 1).Y +
                        3 * Math.Pow(t, 2) * (1 - t) * points(i + 2).Y +
                        Math.Pow(t, 3) * points(i + 3).Y

                Dim p2 = New cv.Point(CInt(x), CInt(y))
                If i > 0 Then
                    dst2.Line(p1, p2, task.highlightColor, task.lineWidth)
                End If

                p2 = p1
            Next
        Next
    End Sub
End Class
