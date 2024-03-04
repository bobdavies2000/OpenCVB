Imports cv = OpenCvSharp
Public Class LineCoin_Basics : Inherits VB_Algorithm
    Public longLines As New LongLine_Basics
    Public lpList As New List(Of pointPair)
    Public p1List As New List(Of cv.Point)
    Public p2List As New List(Of cv.Point)
    Public ptCounts As New List(Of Integer)
    Public Sub New()
        findSlider("Line length threshold in pixels").Value = 1
        dst2 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find the coincident lines in the image and measure their value."
    End Sub
    Public Sub findLines(mplist As List(Of pointPair))
        Dim lp As pointPair
        For Each mp In mplist
            mp.slope = CInt(mp.slope * 10) / 10
            If mp.slope = 0 Then
                lp = New pointPair(New cv.Point(mp.p1.X, 0), New cv.Point(mp.p1.X, dst2.Height))
            Else
                lp = longLines.buildELine(mp, dst2.Width, dst2.Height)
            End If
            Dim index = p1List.IndexOf(lp.p1)
            If index >= 0 Then
                ptCounts(index) += 1
            Else
                p1List.Add(lp.p1)
                p2List.Add(lp.p2)
                ptCounts.Add(1)
            End If
        Next
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static mpLists As New List(Of List(Of pointPair))
        If task.optionsChanged Then mpLists.Clear()

        longLines.Run(src)

        mpLists.Add(longLines.lines.mpList)

        p1List.Clear()
        p2List.Clear()
        ptCounts.Clear()
        For Each mplist In mpLists
            findLines(mplist)
        Next

        Dim historyCount = gOptions.FrameHistory.Value
        dst2.SetTo(0)
        lpList.Clear()
        For i = 0 To p1List.Count - 1
            If ptCounts(i) > historyCount Then
                dst2.Line(p1List(i), p2List(i), 255, task.lineWidth, task.lineType)
                lpList.Add(New pointPair(p1List(i), p2List(i)))
            End If
        Next

        If mpLists.Count >= historyCount Then mpLists.RemoveAt(0)

        If standaloneTest() Then
            dst3 = src
            For Each lp In lpList
                dst3.Line(lp.p1, lp.p2, cv.Scalar.White, task.lineWidth, task.lineType)
            Next
        End If

        labels(2) = $"The {lpList.Count} lines below were present in each of the last " + CStr(historyCount) + " frames"
    End Sub
End Class






Public Class LineCoin_HistoryIntercept : Inherits VB_Algorithm
    Dim coin As New LineCoin_Basics
    Public lpList As New List(Of pointPair)
    Public Sub New()
        dst2 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "find lines with coincident slopes and intercepts."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static mpLists As New List(Of List(Of pointPair))
        If task.optionsChanged Then mpLists.Clear()

        coin.Run(src)
        dst2 = coin.dst2

        labels(2) = $"The {lpList.Count} lines below were present in each of the last " + CStr(gOptions.FrameHistory.Value) + " frames"
    End Sub
End Class