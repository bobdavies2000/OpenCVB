Imports cv = OpenCvSharp
Public Class FCSLine_Basics : Inherits TaskParent
    Dim delaunay As New Delaunay_Basics
    Public Sub New()
        task.fcsMap = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Build a feature coordinate system (FCS) based on lines, not features."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lastMap = task.fcsMap.Clone
        Dim lastCount = task.lpList.Count

        task.lines.Run(src)
        dst2 = task.lines.dst2

        delaunay.inputPoints.Clear()

        For Each lp In task.lpList
            delaunay.inputPoints.Add(lp.center)
        Next

        delaunay.Run(src)

        task.fcsMap.SetTo(0)
        dst1.SetTo(0)
        For i = 0 To delaunay.facetList.Count - 1
            Dim lp = task.lpList(i)
            lp.facets = delaunay.facetList(i)

            DrawContour(dst1, lp.facets, 255, task.lineWidth)
            DrawContour(task.fcsMap, lp.facets, lp.index)
            DrawContour(dst3, lp.facets, lp.color)
            task.lpList(i) = lp
        Next

        Dim index = task.fcsMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X)
        task.lpD = task.lpList(index)
        DrawContour(dst2, task.lpD.facets, white, task.lineWidth)

        labels(2) = task.lines.labels(2)
        labels(3) = delaunay.labels(2)
    End Sub
End Class







Public Class FCSLine_Parallel : Inherits TaskParent
    Dim findVH As New Line_VerticalHorizontal
    Dim options As New Options_FCSLine
    Public Sub New()
        desc = "Find all verticle lines and combine them if they are 'close'."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        findVH.Run(src)
        dst2 = findVH.dst2
        dst3 = findVH.dst3

    End Sub
End Class
