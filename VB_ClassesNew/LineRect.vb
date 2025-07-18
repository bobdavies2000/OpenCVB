Imports cv = OpenCvSharp
Public Class LineRect_Basics : Inherits TaskParent
    Public lpInput1 As lpData
    Public lpInput2 As lpData
    Public rotatedRect As cv.RotatedRect
    Public Sub New()
        desc = "Create a rectangle from 2 lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Dim p1 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p2 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p3 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p4 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            lpInput1 = New lpData(p1, p2)
            lpInput2 = New lpData(p3, p4)
        End If

        Dim inputPoints() As cv.Point2f = {lpInput1.p1, lpInput1.p2, lpInput2.p1, lpInput2.p2}
        rotatedRect = cv.Cv2.MinAreaRect(inputPoints)
        If standalone And task.heartBeat Then
            dst2.SetTo(0)
            For Each pt In inputPoints
                DrawCircle(dst2, pt, task.DotSize, task.highlight)
            Next
            DrawLine(dst2, lpInput1.p1, lpInput1.p2)
            DrawLine(dst2, lpInput2.p1, lpInput2.p2)
            SetTrueText("Line 1", lpInput1.p1, 2)
            SetTrueText("Line 2", lpInput2.p1, 2)
            DrawRotatedOutline(rotatedRect, dst2, cv.Scalar.Yellow)
        End If
    End Sub
End Class









Public Class LineRect_CenterNeighbor : Inherits TaskParent
    Public options As New Options_LineRect
    Public Sub New()
        task.brickRunFlag = True
        desc = "Remove lines which have similar depth in bricks on either side of a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = src.Clone
        dst3 = src.Clone

        Dim depthThreshold = options.depthThreshold
        Dim depthLines As Integer, colorLines As Integer
        For Each lp In task.lineRGB.lpList
            Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
            Dim index As Integer = task.grid.gridMap.Get(Of Single)(center.Y, center.X)
            Dim nabeList = task.gridNeighbors(index)
            Dim foundObjectLine As Boolean = False
            For i = 1 To nabeList.Count - 1
                Dim brick1 = task.bricks.brickList(nabeList(i))
                If brick1.depth = 0 Then Continue For
                For j = i + 1 To nabeList.Count - 1
                    Dim brick2 = task.bricks.brickList(nabeList(j))
                    If brick2.depth = 0 Then Continue For
                    If Math.Abs(brick1.depth - brick2.depth) > depthThreshold Then
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
        task.brickRunFlag = True
        desc = "Remove lines which have similar depth in bricks on either side of a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = src.Clone
        dst3 = src.Clone

        Dim depthThreshold = options.depthThreshold
        Dim depthLines As Integer, colorLines As Integer
        For Each lp In task.lineRGB.lpList
            Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
            Dim index As Integer = task.grid.gridMap.Get(Of Single)(center.Y, center.X)
            Dim brick = task.bricks.brickList(index)
            If brick.mm.maxVal - brick.mm.minVal > depthThreshold Then
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                depthLines += 1
            Else
                dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                colorLines += 1
            End If
        Next

        If task.heartBeat Then
            labels(2) = CStr(depthLines) + " lines were found between objects (External Lines)"
            labels(3) = CStr(colorLines) + " internal lines were indentified"
        End If
    End Sub
End Class