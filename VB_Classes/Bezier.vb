Imports cv = OpenCvSharp
Public Class Bezier_Basics : Inherits VB_Parent
    Public points() As cv.Point
    Public Sub New()
        points = {New cv.Point(100, 100),
                  New cv.Point(150, 50),
                  New cv.Point(250, 150),
                  New cv.Point(300, 100),
                  New cv.Point(350, 150),
                  New cv.Point(450, 50)}
        vbAddAdvice(traceName + ": Update the public points array variable.  No exposed options.")
        desc = "Use n points to draw a Bezier curve."
    End Sub
    Public Function nextPoint(points() As cv.Point, i As Integer, t As Single) As cv.Point
        Dim x = Math.Pow(1 - t, 3) * points(i).X +
                3 * t * Math.Pow(1 - t, 2) * points(i + 1).X +
                3 * Math.Pow(t, 2) * (1 - t) * points(i + 2).X +
                Math.Pow(t, 3) * points(i + 3).X

        Dim y = Math.Pow(1 - t, 3) * points(i).Y +
                3 * t * Math.Pow(1 - t, 2) * points(i + 1).Y +
                3 * Math.Pow(t, 2) * (1 - t) * points(i + 2).Y +
                Math.Pow(t, 3) * points(i + 3).Y
        Return New cv.Point(x, y)
    End Function
    Public Sub RunVB(src As cv.Mat)
        Dim p1 As cv.Point
        For i = 0 To points.Count - 4 Step 3
            For j = 0 To 100
                Dim p2 = nextPoint(points, i, j / 100)
                If j > 0 Then dst2.Line(p1, p2, task.highlightColor, task.lineWidth)
                p1 = p2
            Next
        Next
        labels(2) = "Bezier output"
    End Sub
End Class







Public Class Bezier_Example : Inherits VB_Parent
    Dim bezier As New Bezier_Basics
    Public points() As cv.Point = {New cv.Point(task.dotSize, task.dotSize), New cv.Point(dst2.Width / 6, dst2.Width / 6),
                                   New cv.Point(dst2.Width * 3 / 4, dst2.Height / 2),
                                   New cv.Point(dst2.Width - task.dotSize * 2, dst2.Height - task.dotSize * 2)}
    Public Sub New()
        desc = "Draw a Bezier curve based with the 4 input points."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2.SetTo(0)
        Dim p1 As cv.Point
        For i = 0 To 100 - 1
            Dim p2 = bezier.nextPoint(points, 0, i / 100)
            If i > 0 Then dst2.Line(p1, p2, task.highlightColor, task.lineWidth)
            p1 = p2
        Next

        For i = 0 To points.Count - 1
            dst2.Circle(points(i), task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
        Next

        dst2.Line(points(0), points(1), cv.Scalar.White, task.lineWidth, task.lineType)
        dst2.Line(points(2), points(3), cv.Scalar.White, task.lineWidth, task.lineType)
    End Sub
End Class