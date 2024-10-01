Imports cvb = OpenCvSharp
Public Class Grid_Basics : Inherits VB_Parent
    Public count As Integer
    Public rectGrid As New cvb.Mat
    Public Sub New()
        labels = {"", "", "CV_32S map of lowRes grid", "Palettized version of left image"}
        task.lrGridMap = New cvb.Mat(dst2.Size, cvb.MatType.CV_32S)
        task.lrGridMask = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U)
        desc = "Build the grid using the lowRes image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standalone Then
            Static lowCore As New LowRes_Core
            lowCore.Run(src)
            src = lowCore.dst2
        End If

        If src.Channels <> 3 Then src = src.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        count = 0
        task.lowRects.Clear()
        task.ptPixel.Clear()
        Dim lrx As Integer, lry As Integer
        For y = 0 To dst2.Height - 1
            Dim val = src.Get(Of cvb.Vec3b)(y, 0)
            Dim rectStart As Integer = task.lowRects.Count
            Dim rectindex As Integer = task.lowRects.Count
            Dim lastX As Integer = 0
            task.lowRects.Add(New cvb.Rect(0, y, 0, 0))
            task.ptPixel.Add(New cvb.Point(lrx, lry))
            For x = 1 To dst2.Width - 1
                Dim vec = src.Get(Of cvb.Vec3b)(y, x)
                If vec <> val Then
                    Dim r = task.lowRects(rectindex)
                    r.Width += x - lastX
                    task.lowRects(rectindex) = r
                    rectindex += 1
                    lastX = x

                    val = vec
                    count += 1
                    task.lowRects.Add(New cvb.Rect(x, y, 0, 0))
                    lrx += 1
                    task.ptPixel.Add(New cvb.Point(lrx, lry))
                End If
            Next

            lrx = 0
            lry += 1

            Dim rlast = task.lowRects(task.lowRects.Count - 1)
            task.lowRects(task.lowRects.Count - 1) = New cvb.Rect(rlast.X, rlast.Y, dst2.Width - lastX, rlast.Height)

            Dim i = y + 1
            For i = y + 1 To dst2.Height - 1
                Dim tmp As cvb.Mat = (src.Row(i - 1) - src.Row(i)).ToMat.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
                If tmp.CountNonZero Then Exit For
            Next
            count += 1
            For j = rectStart To task.lowRects.Count - 1
                Dim r = task.lowRects(j)
                task.lowRects(j) = New cvb.Rect(r.X, r.Y, r.Width, i - y)
            Next
            y = i - 1
        Next

        task.lrGridMap.SetTo(0)
        task.lrGridMask.SetTo(0)
        For i = 0 To task.lowRects.Count - 1
            Dim r = task.lowRects(i)
            task.lrGridMap.Rectangle(r, i, -1)
            task.lrGridMask.Rectangle(r, cvb.Scalar.White, task.lineWidth)
        Next
        dst2 = ShowPalette(task.lrGridMap * 255 / task.lowRects.Count)
    End Sub
End Class



'task.flessBoundary = New cvb.Mat(task.color.Size, cvb.MatType.CV_8U, 0)
'For i = 0 To task.lowRects.Count - 2
'Dim r1 = task.lowRects(i)
'Dim r2 = task.lowRects(i + 1)
'If r2.X < r1.X Then Continue For
'            Dim v1 = dst1.Get(Of Byte)(r1.Y, r1.X)
'Dim v2 = dst1.Get(Of Byte)(r2.Y, r2.X)
'If v1 = 0 And v2 > 0 Then task.flessBoundary(r1).SetTo(255)
'            If v1 > 0 And v2 = 0 Then task.flessBoundary(r2).SetTo(255)
'        Next

'For i = 0 To task.lowRects.Count - 2
'    Dim r1 = task.lowRects(i)
'    Dim pt =
'    Dim p1 = New cvb.Point(r.X, r.Y)
'    Dim p2 = New cvb.Point(r.X, r.Y + r.Height + 1)
'    If p2.Y >= task.color.Height Then Exit For
'    Dim v1 = dst1.Get(Of Byte)(p1.Y, p1.X)
'    Dim v2 = dst1.Get(Of Byte)(p2.Y, p2.X)
'    If v1 = 0 And v2 > 0 Then task.flessBoundary(r).SetTo(255)
'    If v1 > 0 And v2 = 0 Then task.flessBoundary(r2).SetTo(255)
'Next