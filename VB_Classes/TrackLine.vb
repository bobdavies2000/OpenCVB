Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class TrackLine_Basics : Inherits TaskParent
    Dim match As New Match_Basics
    Public lp As New lpData
    Public Sub New()
        desc = "Identify and track the longest line, preferably a gravityproxy if available."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lineRGB.lpList
        If lplist.Count = 0 Then
            SetTrueText("There are no lines present in the image.", 3)
            Exit Sub
        End If

        If match.correlation < task.fCorrThreshold Then
            lp = lplist(0)
            For Each lp In lplist
                If lp.gravityProxy Then Exit For
            Next
            match.template = src(lp.rect)
        End If

        match.Run(src.Clone)

        If match.correlation < task.fCorrThreshold Then
            If lplist.Count > 1 Then
                Dim histogram As New cv.Mat
                cv.Cv2.CalcHist({task.lineRGB.lpMap(lp.rect)}, {0}, emptyMat, histogram, 1, {lplist.Count},
                                 New cv.Rangef() {New cv.Rangef(1, lplist.Count)})

                Dim histArray(histogram.Total - 1) As Single
                Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
                Dim histList = histArray.ToList
                lp = lplist(histList.IndexOf(histList.Max))
                match.template = src(lp.rect)
                match.correlation = 1
            Else
                match.correlation = 0 ' force a restart
            End If
        Else
            Dim deltaX = match.newRect.X - lp.rect.X
            Dim deltaY = match.newRect.Y - lp.rect.Y
            Dim p1 = New cv.Point(lp.p1.X + deltaX, lp.p1.Y + deltaY)
            Dim p2 = New cv.Point(lp.p2.X + deltaX, lp.p2.Y + deltaY)
            lp = New lpData(p1, p2)
        End If

        If standaloneTest() Then
            dst2 = src
            dst2.Rectangle(lp.rect, task.highlight, task.lineWidth)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        End If

        labels(2) = "Selected line has a correlation of " + Format(match.correlation, fmt3) + " with the previous frame."
    End Sub
End Class