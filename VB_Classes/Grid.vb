Imports cvb = OpenCvSharp
Public Class Grid_Basics : Inherits VB_Parent
    Public count As Integer
    Public rectList As New List(Of cvb.Rect)
    Public rectGrid As New cvb.Mat
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_32S)
        dst3 = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U)
        desc = "Build the grid using the lowRes image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels <> 3 Then src = src.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        count = 0
        rectList.Clear()
        For y = 0 To dst2.Height - 1
            Dim val = src.Get(Of cvb.Vec3b)(y, 0)
            Dim rectStart As Integer = rectList.Count
            Dim rectindex As Integer = rectList.Count
            Dim lastX As Integer = 0
            rectList.Add(New cvb.Rect(0, y, 0, 0))
            For x = 0 To dst2.Width - 1
                Dim vec = src.Get(Of cvb.Vec3b)(y, x)
                If vec <> val Then
                    Dim r = rectList(rectindex)
                    r.Width += x - lastX
                    rectList(rectindex) = r
                    rectindex += 1
                    lastX = x

                    val = vec
                    count += 1
                    rectList.Add(New cvb.Rect(x, y, 0, 0))
                End If
            Next
            Dim rlast = rectList(rectList.Count - 1)
            rectList(rectList.Count - 1) = New cvb.Rect(rlast.X, rlast.Y, dst2.Width - lastX, rlast.Height)

            Dim i = y + 1
            For i = y + 1 To dst2.Height - 1
                Dim tmp As cvb.Mat = (src.Row(i - 1) - src.Row(i)).ToMat.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
                If tmp.CountNonZero Then Exit For
            Next
            count += 1
            For j = rectStart To rectList.Count - 1
                Dim r = rectList(j)
                rectList(j) = New cvb.Rect(r.X, r.Y, r.Width, i - y)
            Next
            y = i - 1
        Next

        dst2.SetTo(0)
        dst3.SetTo(0)
        For i = 0 To rectList.Count - 1
            Dim r = rectList(i)
            dst2.Rectangle(r, i, -1)
            dst3.Rectangle(r, cvb.Scalar.White, task.lineWidth)
        Next
    End Sub
End Class