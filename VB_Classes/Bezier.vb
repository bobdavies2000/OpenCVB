Imports cvb = OpenCvSharp
Public Class Bezier_Basics : Inherits TaskParent
    Public points() As cvb.Point
    Public Sub New()
        points = {New cvb.Point(100, 100),
                  New cvb.Point(150, 50),
                  New cvb.Point(250, 150),
                  New cvb.Point(300, 100),
                  New cvb.Point(350, 150),
                  New cvb.Point(450, 50)}
        UpdateAdvice(traceName + ": Update the public points array variable.  No exposed options.")
        desc = "Use n points to draw a Bezier curve."
    End Sub
    Public Function nextPoint(points() As cvb.Point, i As Integer, t As Single) As cvb.Point
        Dim x = Math.Pow(1 - t, 3) * points(i).X +
                3 * t * Math.Pow(1 - t, 2) * points(i + 1).X +
                3 * Math.Pow(t, 2) * (1 - t) * points(i + 2).X +
                Math.Pow(t, 3) * points(i + 3).X

        Dim y = Math.Pow(1 - t, 3) * points(i).Y +
                3 * t * Math.Pow(1 - t, 2) * points(i + 1).Y +
                3 * Math.Pow(t, 2) * (1 - t) * points(i + 2).Y +
                Math.Pow(t, 3) * points(i + 3).Y
        Return New cvb.Point(x, y)
    End Function
    Public Overrides sub runAlg(src As cvb.Mat)
        Dim p1 As cvb.Point
        For i = 0 To points.Count - 4 Step 3
            For j = 0 To 100
                Dim p2 = nextPoint(points, i, j / 100)
                If j > 0 Then DrawLine(dst2, p1, p2, task.HighlightColor)
                p1 = p2
            Next
        Next
        labels(2) = "Bezier output"
    End Sub
End Class







Public Class Bezier_Example : Inherits TaskParent
    Dim bezier As New Bezier_Basics
    Public points() As cvb.Point = {New cvb.Point(task.DotSize, task.DotSize), New cvb.Point(dst2.Width / 6, dst2.Width / 6),
                                   New cvb.Point(dst2.Width * 3 / 4, dst2.Height / 2),
                                   New cvb.Point(dst2.Width - task.DotSize * 2, dst2.Height - task.DotSize * 2)}
    Public Sub New()
        desc = "Draw a Bezier curve based with the 4 input points."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        dst2.SetTo(0)
        Dim p1 As cvb.Point
        For i = 0 To 100 - 1
            Dim p2 = bezier.nextPoint(points, 0, i / 100)
            If i > 0 Then DrawLine(dst2, p1, p2, task.HighlightColor)
            p1 = p2
        Next

        For i = 0 To points.Count - 1
            DrawCircle(dst2, points(i), task.DotSize + 2, white)
        Next

        DrawLine(dst2, points(0), points(1), white)
        DrawLine(dst2, points(2), points(3), white)
    End Sub
End Class






