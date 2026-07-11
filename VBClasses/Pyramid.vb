Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
' https://docs.opencvb.org/3.3.1/d6/d73/Pyramids_8cpp-example.html
Public Class Pyramid_Basics : Inherits TaskParent
    Dim options As New Options_Pyramid
    Public Sub New()
        desc = "Use pyrup and pyrdown to zoom in and out of an image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If options.zoom <> 0 Then
            Dim tmp As New cv.Mat
            If options.zoom < 0 Then
                cv.Cv2.PyrDown(src, tmp, New cv.Size(src.Cols / 2, src.Rows / 2))
                Dim roi = New cv.Rect((src.Cols - tmp.Cols) / 2, (src.Rows - tmp.Rows) / 2, tmp.Width, tmp.Height)
                dst2(roi) = tmp
            Else
                cv.Cv2.PyrUp(src, tmp, New cv.Size(src.Cols * 2, src.Rows * 2))
                Dim roi = New cv.Rect((tmp.Cols - src.Cols) / 2, (tmp.Rows - src.Rows) / 2, src.Width, src.Height)
                dst2 = tmp(roi)
            End If
        Else
            src.CopyTo(dst2)
        End If
    End Sub
End Class






Public Class XR_Pyramid_Filter : Inherits TaskParent
    Dim laplace As New Laplacian_PyramidFilter
    Public Sub New()
        desc = "Link to Laplacian_PyramidFilter that uses pyrUp and pyrDown extensively"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        laplace.Run(src)
        dst2 = laplace.dst2
    End Sub
End Class
