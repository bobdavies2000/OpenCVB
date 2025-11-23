Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class LineMatch_Basics : Inherits TaskParent
    Public lpListLast As List(Of lpData) = New List(Of lpData)(task.lines.lpList)
    Public lpList As New List(Of lpData)
    Public lpMatches As New List(Of lpData)
    Dim slices As New LineMatch_Slices
    Public lpMapLast = task.lines.dst1.Clone
    Public Sub New()
        desc = "Match lines with image slices to locate the best matching line.  Confirm with angle."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count <= 1 Then Exit Sub

        slices.Run(emptyMat)

        Dim lpMatch As lpData
        lpMatches.Clear()
        lpList.Clear()
        Dim missCount As Integer
        dst2.SetTo(0)
        dst3.SetTo(0)
        For i = 0 To Math.Min(task.lines.lpList.Count, slices.xSlices.Count) - 1
            Dim lp = task.lines.lpList(i)
            Dim color = task.scalarColors(lp.index + 1)

            For Each sliceSet In {slices.xSlices(i), slices.ySlices(i)}
                Dim angleDelta As New List(Of Single)
                Dim lineIndex As New List(Of Integer)
                For j = 0 To sliceSet.Count - 1
                    Dim lastIndex = sliceSet(j) - 1
                    If lastIndex >= 0 And lastIndex < lpListLast.Count Then
                        lpMatch = lpListLast(lastIndex)
                        angleDelta.Add(Math.Abs(lp.angle - lpMatch.angle))
                        lineIndex.Add(lastIndex)
                    End If
                Next

                If angleDelta.Count > 0 Then
                    Dim minAngleDelta = angleDelta.Min
                    If minAngleDelta < 5 Then ' within 5 degrees of the original line's angle
                        Dim index = lineIndex(angleDelta.IndexOf(minAngleDelta))
                        lpMatch = lpListLast(index)
                        dst2.Line(lp.p1, lp.p2, color, task.lineWidth + 2, task.lineType)
                        dst3.Line(lpMatch.p1, lpMatch.p2, color, task.lineWidth + 2, task.lineType)
                        lpList.Add(lp)
                        lpMatches.Add(lpMatch)
                    Else
                        missCount += 1
                    End If
                Else
                    missCount += 1
                End If
            Next
        Next

        If task.heartBeat Then
            labels(2) = "Searching " + CStr(slices.lineMaxOffset) + " pixels around center "
            labels(3) = CStr(lpMatches.Count) + " lines matched."
        End If

        lpListLast = New List(Of lpData)(task.lines.lpList)
    End Sub
End Class






Public Class LineMatch_Tester : Inherits TaskParent
    Dim match As New LineMatch_Basics
    Public Sub New()
        task.gOptions.DebugCheckBox.Checked = True
        desc = "Test the line match algorithm by just occasionally capturing the current state."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count <= 1 Then Exit Sub

        Static lpListLast As New List(Of lpData)(task.lines.lpList)
        Static srcLast = src.Clone
        Static lpListTest As New List(Of lpData)(task.lines.lpList)
        Static lpMapTest = task.lines.dst1.Clone
        If task.optionsChanged Or task.gOptions.DebugCheckBox.Checked Then
            lpListTest = New List(Of lpData)(task.lines.lpList)
            lpMapTest = task.lines.dst1.Clone
            srcLast = src.Clone
        End If

        ' task.gOptions.DebugCheckBox.Checked = task.heartBeatLT

        match.lpListLast = New List(Of lpData)(lpListTest)
        match.lpMapLast = lpMapTest.clone
        match.Run(task.gray)

        If task.toggleOn Then
            dst2 = src
            dst3 = srcLast
        Else
            dst2.SetTo(0)
            dst3.SetTo(0)
        End If
        For i = 0 To match.lpList.Count - 1
            Dim lp = match.lpList(i)
            Dim color = task.scalarColors(lp.index + 1)
            dst2.Line(lp.p1, lp.p2, color, task.lineWidth + 2, task.lineType)
            dst2.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)

            lp = match.lpMatches(i)
            dst3.Line(lp.p1, lp.p2, color, task.lineWidth + 2, task.lineType)
            dst3.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
        Next
        labels(2) = match.labels(2)
        labels(3) = match.labels(3)

        task.gOptions.DebugCheckBox.Checked = False
    End Sub
End Class





Public Class LineMatch_XYOffsets : Inherits TaskParent
    Public lpMapLast As cv.Mat
    Public lpMatches As New List(Of lpData)
    Public lpList As New List(Of lpData)
    Public maxOffset As Integer
    Public Sub New()
        desc = "Determine the X and Y offset of the lines and their matched partner."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim currX As New List(Of Single)
        Dim prevX As New List(Of Single)

        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            currX.Add(lp.ptCenter.X)
        Next
    End Sub
End Class






Public Class LineMatch_Slices : Inherits TaskParent
    Public xSlices As New List(Of List(Of Byte))
    Public ySlices As New List(Of List(Of Byte))
    Public lineMaxOffset As Integer = 10 ' how many pixels to search for lines.
    Public Sub New()
        labels(2) = "White lines are slices used to find previous line locations."
        desc = "Build slices in X and Y from the previous image near the each line's center."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lpMapLast As cv.Mat = task.lines.dst1.Clone

        dst2.SetTo(0)
        xSlices.Clear()
        ySlices.Clear()
        For i = 0 To Math.Min(task.lines.lpList.Count, 5) - 1
            Dim lp = task.lines.lpList(i)
            Dim color = task.scalarColors(lp.index + 1)
            dst2.Line(lp.p1, lp.p2, color, task.lineWidth + 2, task.lineType)

            Dim ptMinX = New cv.Point(Math.Max(lp.ptCenter.X - lineMaxOffset, 0), lp.ptCenter.Y)
            Dim ptMaxX = New cv.Point(Math.Min(lp.ptCenter.X + lineMaxOffset, dst2.Width), lp.ptCenter.Y)

            Dim rX = New cv.Rect(ptMinX.X, lp.ptCenter.Y, ptMaxX.X - ptMinX.X, 1)
            Dim SliceX(rX.Width - 1) As Byte
            Marshal.Copy(lpMapLast(rX).Data, SliceX, 0, SliceX.Length)
            dst2.Line(ptMinX, ptMaxX, white, task.lineWidth)

            xSlices.Add(SliceX.ToList)

            Dim ptMinY = New cv.Point(lp.ptCenter.X, Math.Max(lp.ptCenter.Y - lineMaxOffset, 0))
            Dim ptMaxY = New cv.Point(lp.ptCenter.X, Math.Min(lp.ptCenter.Y + lineMaxOffset, dst2.Height))

            Dim rY = New cv.Rect(lp.ptCenter.X, ptMinY.Y, 1, ptMaxY.Y - ptMinY.Y)
            Dim SliceY(rY.Height - 1) As Byte
            Marshal.Copy(lpMapLast(rY).Data, SliceY, 0, SliceY.Length)
            dst2.Line(ptMinY, ptMaxY, white, task.lineWidth)

            ySlices.Add(SliceY.ToList)
        Next

        lpMapLast = task.lines.dst1.Clone
    End Sub
End Class
