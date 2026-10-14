Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class LineIterator_Basics : Inherits TaskParent
        Public lp As lpData
        Public depthList As New List(Of Single)
        Public Sub New()
            If task.lines Is Nothing Then task.lines = New Line_Basics_TA
            labels(2) = "Longest line from Line_Basics_TA"
            labels(3) = "Depth along that line"
            desc = "Cursor.ai: Walk the longest line and read depth from task.pcSplit(2)."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                dst2 = task.color.Clone
                lp = task.lines.lpList(0)
            End If

            Line(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth + 1, task.lineType)

            Dim iter As New LineIterator(task.pcSplit(2), lp.p1, lp.p2,
                                         PixelConnectivity.Connectivity8, False)
            depthList.Clear()
            For Each pt In iter
                Dim depth = pt.GetValue(Of Single)()
                If depth <= 0 Then Continue For
                depthList.Add(depth)
            Next

            If standaloneTest() Then
                If depthList.Count = 0 Then
                    strOut = "Longest line has no depth." + vbCrLf + "Length " +
                             lp.length.ToString(fmt1) + " pixels, " + CStr(iter.Count) + " points."
                Else
                    strOut = "Longest line  length " + lp.length.ToString(fmt1) +
                             " pixels" + vbCrLf + CStr(iter.Count) + " points, " +
                             CStr(depthList.Count) + " with depth" + vbCrLf +
                             "Average " + depthList.Average.ToString(fmt3) + " m" + vbCrLf +
                             "Min " + depthList.Min.ToString(fmt3) + " m" + vbCrLf +
                             "Max " + depthList.Max.ToString(fmt3) + " m"
                End If
                SetTrueText(strOut, 3)
            End If
        End Sub
    End Class
End Namespace
