Imports cvb = OpenCvSharp
Public Class LineCoin_Basics : Inherits TaskParent
    Public longLines As New LongLine_Basics
    Public lpList As New List(Of linePoints)
    Dim lpLists As New List(Of List(Of linePoints))
    Public Sub New()
        dst2 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Find the coincident lines in the image and measure their value."
    End Sub
    Public Function findLines(lpLists As List(Of List(Of linePoints))) As List(Of linePoints)
        Dim p1List As New List(Of cvb.Point)
        Dim p2List As New List(Of cvb.Point)
        Dim ptCounts As New List(Of Integer)
        Dim lp As linePoints
        For Each lpList In lpLists
            For Each mp In lpList
                mp.slope = CInt(mp.slope * 10) / 10
                If mp.slope = 0 Then
                    lp = New linePoints(New cvb.Point(mp.p1.X, 0), New cvb.Point(mp.p1.X, dst2.Height))
                Else
                    lp = longLines.BuildLongLine(mp)
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
        Next
        lpList.Clear()
        dst2.SetTo(0)
        For i = 0 To p1List.Count - 1
            If ptCounts(i) >= task.frameHistoryCount Then
                DrawLine(dst2, p1List(i), p2List(i), 255)
                lpList.Add(New linePoints(p1List(i), p2List(i)))
            End If
        Next
        If lpLists.Count >= task.frameHistoryCount Then lpLists.RemoveAt(0)
        Return lpList
    End Function
    Public Overrides sub runAlg(src As cvb.Mat)
        If task.optionsChanged Then lpLists.Clear()

        longLines.Run(src)
        lpLists.Add(longLines.lpList)
        lpList = findLines(lpLists)

        If standaloneTest() Then
            dst3 = src
            For Each lp In lpList
                dst3.Line(lp.p1, lp.p2, white)
            Next
        End If

        labels(2) = $"The {lpList.Count} lines below were present in each of the last " + CStr(task.frameHistoryCount) + " frames"
    End Sub
End Class





Public Class LineCoin_HistoryIntercept : Inherits TaskParent
    Dim coin As New LineCoin_Basics
    Public lpList As New List(Of linePoints)
    Dim mpLists As New List(Of List(Of linePoints))
    Public Sub New()
        dst2 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "find lines with coincident slopes and intercepts."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        If task.optionsChanged Then mpLists.Clear()

        coin.Run(src)
        dst2 = coin.dst2

        labels(2) = $"The {lpList.Count} lines below were present in each of the last " + CStr(task.frameHistoryCount) + " frames"
    End Sub
End Class





Public Class LineCoin_Parallel : Inherits TaskParent
    Dim parallel As New LongLine_ExtendParallel
    Dim near As New Line_Nearest
    Public coinList As New List(Of coinPoints)
    Public Sub New()
        desc = "Find the lines that are coincident in the parallel lines"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        parallel.Run(src)

        coinList.Clear()

        For Each cp In parallel.parList
            near.lp = New linePoints(cp.p1, cp.p2)
            near.pt = cp.p3
            near.Run(empty)
            Dim d1 = near.distance

            near.pt = cp.p4
            near.Run(empty)
            If near.distance <= 1 Or d1 <= 1 Then coinList.Add(cp)
        Next

        dst2 = src.Clone
        For Each cp In coinList
            dst2.Line(cp.p3, cp.p4, cvb.Scalar.Red, task.lineWidth + 2, task.lineType)
            dst2.Line(cp.p1, cp.p2, task.HighlightColor, task.lineWidth + 1, task.lineType)
        Next
        labels(2) = CStr(coinList.Count) + " coincident lines were detected"
    End Sub
End Class