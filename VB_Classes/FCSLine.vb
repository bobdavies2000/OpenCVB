Imports cv = OpenCvSharp
Public Class FCSLine_Basics : Inherits TaskParent
    Dim delaunay As New Delaunay_Basics
    Public Sub New()
        desc = "Build a feature coordinate system (FCS) based on the line centers."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.lines.Run(src)
        dst2 = task.lines.dst2

        delaunay.inputPoints.Clear()

        For Each lp In task.lpList
            delaunay.inputPoints.Add(lp.center)
        Next

        delaunay.Run(src)
        dst3 = delaunay.dst2

        labels(2) = task.lines.labels(2)
        labels(3) = delaunay.labels(2)
    End Sub
End Class
