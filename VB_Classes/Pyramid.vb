Imports cvb = OpenCvSharp
' https://docs.opencvb.org/3.3.1/d6/d73/Pyramids_8cpp-example.html
Public Class Pyramid_Basics : Inherits TaskParent
    Dim options As New Options_Pyramid
    Public Sub New()
        desc = "Use pyrup and pyrdown to zoom in and out of an image."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        options.RunOpt()

        If options.zoom <> 0 Then
            If options.zoom < 0 Then
                Dim tmp = src.PyrDown(New cvb.Size(src.Cols / 2, src.Rows / 2))
                Dim roi = New cvb.Rect((src.Cols - tmp.Cols) / 2, (src.Rows - tmp.Rows) / 2, tmp.Width, tmp.Height)
                dst2(roi) = tmp
            Else
                Dim tmp = src.PyrUp(New cvb.Size(src.Cols * 2, src.Rows * 2))
                Dim roi = New cvb.Rect((tmp.Cols - src.Cols) / 2, (tmp.Rows - src.Rows) / 2, src.Width, src.Height)
                dst2 = tmp(roi)
            End If
        Else
            src.CopyTo(dst2)
        End If
    End Sub
End Class






Public Class Pyramid_Filter : Inherits TaskParent
    Dim laplace As New Laplacian_PyramidFilter
    Public Sub New()
        desc = "Link to Laplacian_PyramidFilter that uses pyrUp and pyrDown extensively"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        laplace.Run(src)
        dst2 = laplace.dst2
    End Sub
End Class



