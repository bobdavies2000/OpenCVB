Imports cv = OpenCvSharp
' https://docs.opencv.org/3.3.1/d6/d73/Pyramids_8cpp-example.html
Public Class Pyramid_Basics : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Zoom in and out", -1, 1, 0)
        End If
        task.desc = "Use pyrup and pyrdown to zoom in and out of an image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim zoom = sliders.trackbar(0).Value
        If zoom <> 0 Then
            If zoom < 0 Then
                Dim tmp = src.PyrDown(New cv.Size(src.Cols / 2, src.Rows / 2))
                Dim roi = New cv.Rect((src.Cols - tmp.Cols) / 2, (src.Rows - tmp.Rows) / 2, tmp.Width, tmp.Height)
                dst2(roi) = tmp
            Else
                Dim tmp = src.PyrUp(New cv.Size(src.Cols * 2, src.Rows * 2))
                Dim roi = New cv.Rect((tmp.Cols - src.Cols) / 2, (tmp.Rows - src.Rows) / 2, src.Width, src.Height)
                dst2 = tmp(roi)
            End If
        Else
            src.CopyTo(dst2)
        End If
    End Sub
End Class






Public Class Pyramid_Filter : Inherits VBparent
    Dim laplace As New Laplacian_PyramidFilter
    Public Sub New()
        task.desc = "Link to Laplacian_PyramidFilter that uses pyrUp and pyrDown extensively"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        laplace.Run(src)
        dst2 = laplace.dst2
    End Sub
End Class



