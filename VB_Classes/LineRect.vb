Imports OpenCvSharp
Imports cv = OpenCvSharp
Public Class LineRect_Basics : Inherits TaskParent
    Public lpInput1 As lpData
    Public lpInput2 As lpData
    Public rotatedRect As RotatedRect
    Public Sub New()
        desc = "Create a rectangle from 2 lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lp1 = lpInput1, lp2 = lpInput2
        If lp1 Is Nothing Then
            Dim p1 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p2 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p3 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p4 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            lp1 = New lpData(p1, p2)
            lp2 = New lpData(p3, p4)
        End If

        Dim inputPoints As New List(Of cv.Point2f)
        inputPoints.Add(lp1.p1)
        inputPoints.Add(lp1.p2)
        inputPoints.Add(lp2.p1)
        inputPoints.Add(lp2.p2)
        If lp1.rotatedRect = Nothing And lp2.rotatedRect = Nothing Then
            rotatedRect = cv.Cv2.MinAreaRect(inputPoints.ToArray)
        ElseIf lp1.rotatedRect = Nothing Then
            For Each pt In lp2.rotatedRect.Points
                inputPoints.Add(pt)
            Next
        Else
            For Each pt In lp1.rotatedRect.Points
                inputPoints.Add(pt)
            Next
        End If
        If standalone And task.heartBeat Then
            dst2.SetTo(0)
            For Each pt In inputPoints
                DrawCircle(dst2, pt, task.DotSize, task.highlight)
            Next
            DrawRotatedOutline(rotatedRect, dst2, cv.Scalar.Yellow)
        End If
    End Sub
End Class






Public Class LineRect_CenterDepth : Inherits TaskParent
    Public options As New Options_LineRect
    Public Sub New()
        desc = "Remove lines which have similar depth in grid cells on either side of a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = src.Clone
        dst3 = src.Clone

        Dim depthThreshold = options.depthThreshold
        Dim depthLines As Integer, colorLines As Integer
        For Each lp In task.lpList
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
            Dim lpPerp = lp.perpendicularPoints(lp.center, task.cellSize)
            Dim index1 As Integer = task.gcMap.Get(Of Single)(lpPerp.p1.Y, lpPerp.p1.X)
            Dim index2 As Integer = task.gcMap.Get(Of Single)(lpPerp.p2.Y, lpPerp.p2.X)
            Dim idd1 = task.gcList(index1)
            Dim idd2 = task.gcList(index2)
            If Math.Abs(idd1.depth - idd2.depth) > depthThreshold Then
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                depthLines += 1
            Else
                dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
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
        options.Run()

        dst2 = src.Clone
        dst3 = src.Clone

        Dim depthThreshold = options.depthThreshold
        Dim depthLines As Integer, colorLines As Integer
        For Each lp In task.lpList
            Dim index As Integer = task.gcMap.Get(Of Single)(lp.center.Y, lp.center.X)
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
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                depthLines += 1
            Else
                dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
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
        options.Run()

        dst2 = src.Clone
        dst3 = src.Clone

        Dim depthThreshold = options.depthThreshold
        Dim depthLines As Integer, colorLines As Integer
        For Each lp In task.lpList
            Dim index As Integer = task.gcMap.Get(Of Single)(lp.center.Y, lp.center.X)
            Dim idd = task.gcList(index)
            If idd.mm.maxVal - idd.mm.minVal > depthThreshold Then
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                depthLines += 1
            Else
                dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                colorLines += 1
            End If
        Next

        If task.heartBeat Then
            labels(2) = CStr(depthLines) + " lines were found between objects (External Lines)"
            labels(3) = CStr(colorLines) + " internal lines were indentified and are not likely important"
        End If
    End Sub
End Class