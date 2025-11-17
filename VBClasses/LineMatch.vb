Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class LineMatch_Basics : Inherits TaskParent
    Public desiredLineMatches As Integer = 10
    Public maxOffset As Integer = 5 ' look at X pixels before and X pixels after the line center.
    Public Sub New()
        desc = "Find lines with an image slice to locate the best matching line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lastFrame As cv.Mat = src.Clone

        If task.toggleOn Or Not standalone Then
            dst2 = task.gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3 = lastFrame.Clone
        Else
            dst2.SetTo(0)
            dst3.SetTo(0)
        End If
        Static lpListLast As New List(Of lpData)(task.lines.lpList)
        Static lpMapLast As cv.Mat = task.lines.dst1.Clone
        Dim lpMatch As lpData
        Dim count As Integer, missCount As Integer
        For i = 0 To Math.Min(task.lines.lpList.Count, desiredLineMatches) - 1
            Dim lp = task.lines.lpList(i)
            Dim color = task.scalarColors(lp.index + 1)

            Dim ptMin = New cv.Point(Math.Max(lp.ptCenter.X - maxOffset, 0), lp.ptCenter.Y)
            Dim ptMax = New cv.Point(Math.Min(lp.ptCenter.X + maxOffset, dst2.Width), lp.ptCenter.Y)

            Dim r = New cv.Rect(ptMin.X, lp.ptCenter.Y, ptMax.X - ptMin.X, 1)
            Dim lastSlice(r.Width - 1) As Byte
            Marshal.Copy(lpMapLast(r).Data, lastSlice, 0, lastSlice.Length)

            Dim angleDelta As New List(Of Single)
            Dim lineIndex As New List(Of Integer)
            For j = 0 To lastSlice.Length - 1
                If lastSlice(j) > 0 Then
                    Dim lastIndex = lastSlice(j) - 1
                    lpMatch = lpListLast(lastIndex)
                    angleDelta.Add(Math.Abs(lp.angle - lpMatch.angle))
                    lineIndex.Add(lastIndex)
                End If
            Next

            dst2.Line(lp.p1, lp.p2, color, task.lineWidth + 2, task.lineType)
            If angleDelta.Count > 0 Then
                Dim minAngleDelta = angleDelta.Min
                If minAngleDelta < 5 Then
                    Dim index = lineIndex(angleDelta.IndexOf(minAngleDelta))
                    lpMatch = lpListLast(index)
                    dst3.Line(lpMatch.p1, lpMatch.p2, color, task.lineWidth + 2, task.lineType)
                    count += 1
                Else
                    missCount += 1
                End If
            Else
                missCount += 1
            End If
        Next

        lpListLast = New List(Of lpData)(task.lines.lpList)
        lpMapLast = task.lines.dst1.Clone

        If task.heartBeat Then
            labels(2) = "Searching " + CStr(maxOffset) + " pixels around center for the top " +
                        CStr(desiredLineMatches) + " lines"
            labels(3) = CStr(count) + " lines matched."
        End If

        lastFrame = task.color.Clone
    End Sub
End Class