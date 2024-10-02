Imports cvb = OpenCvSharp
Public Class Grid_Basics : Inherits VB_Parent
    Public count As Integer
    Public rectGrid As New cvb.Mat
    Public Sub New()
        labels = {"", "", "CV_32S map of lowRes grid", "Palettized version of left image"}
        task.lrFullSizeMap = New cvb.Mat(dst2.Size, cvb.MatType.CV_32S)
        task.lrSquares = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U)
        desc = "Build the grid using the lowRes image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standalone Then
            Static lowCore As New LowRes_Core
            lowCore.Run(src)
            src = lowCore.dst2
        End If

        If src.Channels <> 3 Then src = src.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        Dim rectsByRow As New List(Of List(Of cvb.Rect))
        For y = 0 To dst2.Height - 1
            Dim val = src.Get(Of cvb.Vec3b)(y, 0)
            Dim rectList = New List(Of cvb.Rect)
            Dim currRect As cvb.Rect
            Dim lastX = 0
            For x = 1 To dst2.Width - 1
                Dim vec = src.Get(Of cvb.Vec3b)(y, x)
                If vec <> val Then
                    currRect = New cvb.Rect(lastX, y, x - lastX, 0)
                    lastX = x

                    val = vec
                    rectList.Add(currRect)
                End If
            Next

            rectList.Add(New cvb.Rect(lastX, y, dst2.Width - lastX, 0))

            Dim i = y + 1
            For i = i To dst2.Height - 1
                Dim tmp As cvb.Mat = (src.Row(y) - src.Row(i)).ToMat.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
                If tmp.CountNonZero Then Exit For
            Next
            For j = 0 To rectList.Count - 1
                Dim r = rectList(j)
                r = New cvb.Rect(r.X, r.Y, r.Width, i - y)
                rectList(j) = r
            Next

            For Each r In rectList
            Next

            rectsByRow.Add(rectList)
            y = i - 1
        Next

        Dim correctedY As New List(Of List(Of cvb.Rect))
        For i = 0 To rectsByRow.Count - 1
            Dim mergeRow As Boolean = False
            Dim rectlist = rectsByRow(i)
            For Each r In rectlist
                If r.Height = 1 Then
                    mergeRow = True
                    Exit For ' add this row to the following row
                End If
            Next

            If mergeRow Then
                Dim newRectlist = rectsByRow(i + 1)
                For j = 0 To newRectlist.Count - 1
                    Dim r = newRectlist(j)
                    r = New cvb.Rect(r.X, r.Y - 1, r.Width, r.Height + 1)
                    newRectlist(j) = r
                Next
                correctedY.Add(newRectlist)
            Else
                correctedY.Add(rectlist)
            End If
        Next

        task.lrRectsByRow.Clear()
        For i = 0 To correctedY.Count - 1
            Dim rectlist = correctedY(i)
            Dim mergeCol As Boolean = False
            For j = 0 To rectlist.Count - 1
                Dim r = rectlist(j)
                If r.Width = 1 Then
                    Dim rNew = rectlist(j + 1)
                    rNew = New cvb.Rect(rNew.X - 1, rNew.Y, rNew.Width + 1, rNew.Height)
                    rectlist(j) = rNew
                    mergeCol = True
                Else
                    rectlist(j) = r
                End If
            Next
            If mergeCol Then i += 1
            task.lrRectsByRow.Add(rectlist)
        Next

        task.lrFullSizeMap.SetTo(0)
        task.lrSquares.SetTo(0)
        task.lrAllRects.Clear()
        For Each rectlist In task.lrRectsByRow
            For i = 0 To rectlist.Count - 1
                Dim r = rectlist(i)
                task.lrFullSizeMap.Rectangle(r, task.lrAllRects.Count, -1)
                task.lrAllRects.Add(r)
                task.lrSquares.Rectangle(r, cvb.Scalar.White, task.lineWidth)
            Next
        Next
        dst3 = ShowPalette(task.lrFullSizeMap * 255 / task.lrAllRects.Count)
        dst2 = task.lrFullSizeMap
        dst1 = task.lrSquares
    End Sub
End Class
