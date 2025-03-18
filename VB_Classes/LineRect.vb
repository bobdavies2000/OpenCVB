Imports cv = OpenCvSharp
Public Class LineRect_CenterDepth : Inherits TaskParent
    Public options As New Options_LineRect
    Public Sub New()
        desc = "Remove lines which have similar depth in grid cells on either side of a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        dst2 = src.Clone
        dst3 = src.Clone

        task.lines.Run(src)

        Dim depthThreshold = options.depthThreshold
        Dim depthLines As Integer, colorLines As Integer
        For Each lp In task.lpList
            dst2.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, cv.LineTypes.Link4)
            Dim pts = lp.perpendicularPoints(lp.center, task.cellSize)
            Dim index1 = task.iddMap.Get(Of Integer)(pts.Item1.Y, pts.Item1.X)
            Dim index2 = task.iddMap.Get(Of Integer)(pts.Item2.Y, pts.Item2.X)
            Dim idd1 = task.gcList(index1)
            Dim idd2 = task.gcList(index2)
            If Math.Abs(idd1.depth - idd2.depth) > depthThreshold Then
                dst2.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, cv.LineTypes.Link4)
                depthLines += 1
            Else
                dst3.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, cv.LineTypes.Link4)
                colorLines += 1
            End If
        Next

        If task.heartBeat Then
            labels(2) = CStr(depthLines) + " lines were found between objects (depth Lines)"
            labels(3) = CStr(colorLines) + " internal lines were indentified and are not likely important"
        End If
    End Sub
End Class








Public Class LineRect_CenterNeighbor : Inherits TaskParent
    Public options As New Options_LineRect
    Public Sub New()
        desc = "Remove lines which have similar depth in grid cells on either side of a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        dst2 = src.Clone
        dst3 = src.Clone

        task.lines.Run(src)

        Dim depthThreshold = options.depthThreshold
        Dim depthLines As Integer, colorLines As Integer
        For Each lp In task.lpList
            Dim index = task.iddMap.Get(Of Integer)(lp.center.Y, lp.center.X)
            Dim nabeList = task.gridNeighbors(index)
            Dim foundObjectLine As Boolean = False
            For i = 1 To nabeList.Count - 1
                Dim idd1 = task.gcList(nabeList(i))
                If idd1.depth = 0 Then Continue For
                For j = i + 1 To nabeList.Count - 1
                    Dim idd2 = task.gcList(nabeList(j))
                    If idd2.depth = 0 Then Continue For
                    If Math.Abs(idd1.depth - idd2.depth) > depthThreshold Then
                        foundObjectLine = True
                        Exit For
                    End If
                Next
                If foundObjectLine Then Exit For
            Next
            If foundObjectLine Then
                dst2.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, cv.LineTypes.Link4)
                depthLines += 1
            Else
                dst3.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, cv.LineTypes.Link4)
                colorLines += 1
            End If
        Next

        If task.heartBeat Then
            labels(2) = CStr(depthLines) + " lines were found between objects (External Lines)"
            labels(3) = CStr(colorLines) + " internal lines were indentified and are not likely important"
        End If
    End Sub
End Class








Public Class LineRect_CenterRange : Inherits TaskParent
    Public options As New Options_LineRect
    Public Sub New()
        desc = "Remove lines which have similar depth in grid cells on either side of a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        dst2 = src.Clone
        dst3 = src.Clone

        task.lines.Run(src)

        Dim depthThreshold = options.depthThreshold
        Dim depthLines As Integer, colorLines As Integer
        For Each lp In task.lpList
            Dim index = task.iddMap.Get(Of Integer)(lp.center.Y, lp.center.X)
            Dim idd = task.gcList(index)
            If idd.mm.maxVal - idd.mm.minVal > depthThreshold Then
                dst2.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, cv.LineTypes.Link4)
                depthLines += 1
            Else
                dst3.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth, cv.LineTypes.Link4)
                colorLines += 1
            End If
        Next

        If task.heartBeat Then
            labels(2) = CStr(depthLines) + " lines were found between objects (External Lines)"
            labels(3) = CStr(colorLines) + " internal lines were indentified and are not likely important"
        End If
    End Sub
End Class