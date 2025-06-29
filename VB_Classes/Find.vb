Imports cv = OpenCvSharp
Public Class Find_PolyLines : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Basics
    Dim polyLine As New PolyLine_Basics
    Public Sub New()
        desc = "Find lines using the brick points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBrick.Run(src)
        labels(2) = ptBrick.labels(2)

        polyLine.Run(src)
        dst2 = polyLine.dst3
        labels(2) = polyLine.labels(3)


        For Each pt In ptBrick.bpList
            dst2.Circle(pt, task.DotSize, task.highlight, -1, task.lineType)
        Next
    End Sub
End Class



Public Class Find_EdgeLine : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Basics
    Public classCount As Integer
    Public Sub New()
        desc = "Find lines using the brick points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBrick.Run(src)
        labels(2) = ptBrick.labels(2)

        dst2 = task.edges.dst2
        dst3 = ShowPalette(task.edges.dst2)

        Dim segments(task.edges.classCount) As List(Of cv.Point2f)
        Dim brickCount As Integer, segmentCount
        For Each pt In ptBrick.bpList
            Dim val = task.edges.dst2.Get(Of Byte)(pt.Y, pt.X)
            If val > 0 And val < 255 Then
                If segments(val) Is Nothing Then
                    segments(val) = New List(Of cv.Point2f)
                    segmentCount += 1
                End If
                segments(val).Add(pt)
                brickCount += 1
            End If
        Next

        labels(3) = CStr(task.edges.classCount) + " segments were found and " + CStr(segmentCount) + " contained brick points"
        labels(3) += " " + CStr(brickCount) + " bricks were part of a segment"

        classCount = 0
        For Each segment In segments
            If segment Is Nothing Then Continue For
            classCount += 1
            Dim p1 = segment(0)
            For Each p2 In segment
                dst3.Circle(p2, task.DotSize, task.highlight, -1, task.lineType)
                'dst3.Line(p1, p2, task.highlight, task.lineWidth, task.lineType)
                p1 = p2
            Next
        Next

        If standaloneTest() Then
            Static debugSegment = 1
            If debugSegment >= segments.Length Then debugSegment = 1
            While segments(debugSegment) Is Nothing
                debugSegment += 1
                If debugSegment >= segments.Length Then Exit Sub ' nothing left to show...
            End While
            If debugSegment Then
                dst1 = task.edges.dst2.InRange(debugSegment, debugSegment)
                dst1.CopyTo(dst2, dst1)
            End If
            debugSegment += 1
        End If
    End Sub
End Class






Public Class Find_Segment : Inherits TaskParent
    Dim edges As New EdgeLine_Basics
    Public Sub New()
        desc = "Break up any segments that cross depth boundaries."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
    End Sub
End Class


