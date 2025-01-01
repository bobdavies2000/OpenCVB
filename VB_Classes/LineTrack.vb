Imports cv = OpenCvSharp
Public Class LineTrack_Basics : Inherits TaskParent
    Public lines as new Line_Basics
    Public delaunay As New Delaunay_Basics
    Public contours As New Delaunay_Contours
    Public lpList As New List(Of linePoints)
    Dim lineMap As New cv.Mat
    Public Sub New()
        labels(3) = "White lines are the previous frame.  Red the current."
        desc = "Track lines from frame to frame"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        lines.Run(src)

        delaunay.inputPoints.Clear()
        For Each lp In task.lpList
            delaunay.inputPoints.Add(lp.center)
        Next
        delaunay.Run(src)
        dst2 = delaunay.dst2

        If task.firstPass Then
            lineMap = delaunay.dst3
            For Each lp In task.lpList
                lp.colorIndex = lineMap.Get(Of Byte)(lp.center.Y, lp.center.X)
                lpList.Add(lp)
            Next
        End If

        Dim newSet As New List(Of linePoints)
        For Each lp In lpList
            Dim val = task.motionMask.Get(Of Byte)(lp.center.Y, lp.center.X)
            If val = 0 Then newSet.Add(lp)
        Next

        For Each lp In task.lpList
            Dim val = task.motionMask.Get(Of Byte)(lp.center.Y, lp.center.X)
            If val <> 0 Then newSet.Add(lp)
        Next

        dst3.SetTo(0)
        Dim linesTracked As Integer
        For i = 0 To newSet.Count - 1
            Dim lp = newSet(i)
            Dim val = lineMap.Get(Of Byte)(lp.center.Y, lp.center.X)
            Dim mp = task.lpList(val)

            If i < 10 Then
                SetTrueText("lp " + CStr(i), lp.p1, 3)
                SetTrueText("mp " + CStr(i), mp.p2, 3)
                dst3.Line(lp.p1, lp.p2, white, task.lineWidth + 2, task.lineType)
                dst3.Line(mp.p1, mp.p2, cv.Scalar.Red, task.lineWidth, task.lineType)
            End If

        Next

        lpList = New List(Of linePoints)(newSet)

        Dim usedIndex As New List(Of Integer)
        For Each lp In lpList
            usedIndex.Add(lp.colorIndex)
        Next

        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            If lp.colorIndex < 0 Then
                For j = 0 To usedIndex.Count - 1
                    If usedIndex.Contains(j) = False Then
                        lp.colorIndex = j Mod 255 ' reuse colors if more than 255
                        Exit For
                    End If
                Next
                lpList(i) = lp
            End If
            'dst3.Line(lp.p1, lp.p2, task.vecColors(lp.colorIndex), 3, task.lineType)
            DrawLine(dst2, lp.p1, lp.p2, white, task.lineWidth)
            DrawCircle(dst2, lp.center, task.DotSize, task.HighlightColor, -1)
        Next

        lineMap = delaunay.dst3
        If task.heartBeat Then
            labels(2) = CStr(linesTracked) + " lines were tracked from the previous frame and " +
                        CStr(lpList.Count - linesTracked) + " new lines were added."
        End If
    End Sub
End Class





Public Class LineTrack_RedCloud : Inherits TaskParent
    Public lines As New Line_Basics
    Public delaunay As New Delaunay_Basics
    Public Sub New()
        desc = "Track the line regions with RedCloud"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        lines.Run(src)

        delaunay.inputPoints.Clear()
        For Each lp In task.lpList
            delaunay.inputPoints.Add(lp.center)
        Next
        delaunay.Run(src)

        task.redC.Run(delaunay.dst3)
        dst2 = task.redC.dst2

        dst3.SetTo(0)
        For Each lp In task.lpList
            DrawLine(dst2, lp.p1, lp.p2, white, task.lineWidth)
            DrawCircle(dst2, lp.center, task.DotSize, task.HighlightColor, -1)
        Next
    End Sub
End Class






Public Class LineTrack_Basics1 : Inherits TaskParent
    Public lines As New Line_Basics
    Public delaunay As New Delaunay_Basics
    Public lpList As New List(Of linePoints)
    Dim lineMap As New cv.Mat
    Public Sub New()
        desc = "Track lines from frame to frame"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        lines.Run(src)

        delaunay.inputPoints.Clear()
        For Each lp In task.lpList
            delaunay.inputPoints.Add(lp.center)
        Next
        delaunay.Run(src)
        dst2 = delaunay.dst2

        If task.firstPass Then
            lineMap = delaunay.dst3
            For Each lp In task.lpList
                lp.colorIndex = lineMap.Get(Of Byte)(lp.center.Y, lp.center.X)
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

        lpList = New List(Of linePoints)(task.lpList)

        Dim usedIndex As New List(Of Integer)
        For Each lp In lpList
            usedIndex.Add(lp.colorIndex)
        Next

        dst3.SetTo(0)
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            If lp.colorIndex < 0 Then
                For j = 0 To usedIndex.Count - 1
                    If usedIndex.Contains(j) = False Then
                        lp.colorIndex = j Mod 255 ' reuse colors if more than 255
                        Exit For
                    End If
                Next
                lpList(i) = lp
            End If
            dst3.Line(lp.p1, lp.p2, task.vecColors(lp.colorIndex), 3, task.lineType)
            DrawLine(dst2, lp.p1, lp.p2, white, task.lineWidth)
            DrawCircle(dst2, lp.center, task.DotSize, task.HighlightColor, -1)
        Next

        lineMap = delaunay.dst3
        If task.heartBeat Then
            labels(3) = CStr(linesTracked) + " lines were tracked from the previous frame and " +
                        CStr(lpList.Count - linesTracked) + " new lines were added."
        End If
    End Sub
End Class