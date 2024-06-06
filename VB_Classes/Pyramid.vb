Imports cv = OpenCvSharp
' https://docs.opencv.org/3.3.1/d6/d73/Pyramids_8cpp-example.html
Public Class Pyramid_Basics : Inherits VB_Parent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Zoom in and out", -1, 1, 0)
        desc = "Use pyrup and pyrdown to zoom in and out of an image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static zoomSlider = FindSlider("Zoom in and out")
        Dim zoom = zoomSlider.Value
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






Public Class Pyramid_Filter : Inherits VB_Parent
    Dim laplace As New Laplacian_PyramidFilter
    Public Sub New()
        desc = "Link to Laplacian_PyramidFilter that uses pyrUp and pyrDown extensively"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        laplace.Run(src)
        dst2 = laplace.dst2
    End Sub
End Class



