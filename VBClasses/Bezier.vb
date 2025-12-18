Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Bezier_Basics : Inherits TaskParent
        Public points() As cv.Point
        Public Sub New()
            points = {New cv.Point(100, 100),
                      New cv.Point(150, 50),
                      New cv.Point(250, 150),
                      New cv.Point(300, 100),
                      New cv.Point(350, 150),
                      New cv.Point(450, 50)}
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
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim p1 As cv.Point
            For i = 0 To points.Count - 4 Step 3
                For j = 0 To 100
                    Dim p2 = nextPoint(points, i, j / 100)
                    If j > 0 Then dst2.Line(p1, p2, algTask.highlight, algTask.lineWidth, algTask.lineWidth)
                    p1 = p2
                Next
            Next
            labels(2) = "Bezier output"
        End Sub
    End Class







    Public Class Bezier_Example : Inherits TaskParent
        Dim bezier As New Bezier_Basics
        Public points() As cv.Point = {New cv.Point(algTask.DotSize, algTask.DotSize), New cv.Point(dst2.Width / 6, dst2.Width / 6),
                                   New cv.Point(dst2.Width * 3 / 4, dst2.Height / 2),
                                   New cv.Point(dst2.Width - algTask.DotSize * 2, dst2.Height - algTask.DotSize * 2)}
        Public Sub New()
            desc = "Draw a Bezier curve based with the 4 input points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2.SetTo(0)
            Dim p1 As cv.Point
            For i = 0 To 100 - 1
                Dim p2 = bezier.nextPoint(points, 0, i / 100)
                If i > 0 Then dst2.Line(p1, p2, algTask.highlight, algTask.lineWidth, algTask.lineWidth)
                p1 = p2
            Next

            For i = 0 To points.Count - 1
                DrawCircle(dst2, points(i), algTask.DotSize + 2, white)
            Next

            dst2.Line(points(0), points(1), white, algTask.lineWidth, algTask.lineWidth)
            dst2.Line(points(2), points(3), white, algTask.lineWidth, algTask.lineWidth)
        End Sub
    End Class
End Namespace