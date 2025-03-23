Imports cv = OpenCvSharp
Public Class LineTrack_Basics : Inherits TaskParent
    Public delaunay As New Delaunay_Basics
    Public lpList As New List(Of lpData)
    Public Sub New()
        desc = "Track lines from frame to frame"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.lines.Run(src)

        'delaunay.inputPoints.Clear()
        'For Each lp In task.lpList
        '    delaunay.inputPoints.Add(lp.center)
        'Next
        'delaunay.Run(src)
        'dst2 = delaunay.dst2
        dst2.SetTo(0)

        If task.firstPass Then
            lineMap = delaunay.dst3
            For Each lp In task.lpList
                lp.colorIndex = task.lpMap.Get(Of Byte)(lp.center.Y, lp.center.X)
                lpList.Add(lp)
            Next
        End If

        Dim linesTracked As Integer
        For Each lp In lpList
            Dim val = lineMap.Get(Of Byte)(lp.center.Y, lp.center.X)
            If val = 0 Then Continue For
            Dim mp = task.lpList(val - 1)
            If (lp.slope <= 1 And mp.slope <= 1) Or (lp.slope > 1 And mp.slope > 1) Then
                ' likely the same.  If not, the old line is tossed
                If Math.Abs(lp.length - mp.length) < 20 Then
                    mp.colorIndex = lp.colorIndex
                    task.lpList(val - 1) = mp
                    linesTracked += 1
                End If
            End If
        Next

        lpList = New List(Of lpData)(task.lpList)

        dst3.SetTo(0)
        For Each lp In task.lpList
            dst3.Line(lp.p1, lp.p2, lp.color, 3, task.lineType)
            DrawLine(dst2, lp.p1, lp.p2, white, task.lineWidth)
            DrawCircle(dst2, lp.center, task.DotSize, task.highlight, -1)
        Next

        lineMap = delaunay.dst3
        If task.heartBeat Then
            labels(3) = CStr(linesTracked) + " lines were tracked from the previous frame and " +
                        CStr(lpList.Count - linesTracked) + " new lines were added."
        End If
    End Sub
End Class






Public Class LineTrack_RedCloud : Inherits TaskParent
    Public delaunay As New Delaunay_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Track the line regions with RedCloud"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        task.lines.Run(src)

        delaunay.inputPoints.Clear()
        For Each lp In task.lpList
            delaunay.inputPoints.Add(lp.center)
        Next
        delaunay.Run(src)

        dst2 = runRedC(delaunay.dst3, labels(2))

        dst3.SetTo(0)
        For Each lp In task.lpList
            DrawLine(dst2, lp.p1, lp.p2, white, task.lineWidth)
            DrawCircle(dst2, lp.center, task.DotSize, task.highlight, -1)
        Next
    End Sub
End Class