Imports cvb = OpenCvSharp
Public Class TextureFlow_Basics : Inherits VB_Parent
    Dim options As New Options_Texture
    Public Sub New()
        desc = "Find and mark the texture flow in an image - see texture_flow.py"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = src.Clone
        If src.Channels() <> 1 Then src = src.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY)
        Dim eigen = src.CornerEigenValsAndVecs(options.TFblockSize, options.TFksize)
        Dim split = eigen.Split()
        Dim d2 = options.TFdelta / 2
        For y = d2 To dst2.Height - 1 Step d2
            For x = d2 To dst2.Width - 1 Step d2
                Dim delta = New cvb.Point2f(split(4).Get(Of Single)(y, x), split(5).Get(Of Single)(y, x)) * options.TFdelta
                Dim p1 = New cvb.Point(CInt(x - delta.X), CInt(y - delta.Y))
                Dim p2 = New cvb.Point(CInt(x + delta.X), CInt(y + delta.Y))
                DrawLine(dst2, p1, p2, task.HighlightColor)
            Next
        Next
    End Sub
End Class





Public Class TextureFlow_Depth : Inherits VB_Parent
    Dim flow As New TextureFlow_Basics
    Public Sub New()
        desc = "Display texture flow in the depth data"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        flow.Run(task.depthRGB)
        dst2 = flow.dst2
    End Sub
End Class






Public Class TextureFlow_Reduction : Inherits VB_Parent
    Dim flow As New TextureFlow_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Display texture flow in the reduced color image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2

        flow.Run(reduction.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR))
        dst3 = flow.dst2
    End Sub
End Class







Public Class TextureFlow_DepthSegments : Inherits VB_Parent
    Dim segments As New Hist_CloudSegments
    Dim diffx As New Edge_DiffX_CPP_VB
    Dim flow As New TextureFlow_Basics
    Public Sub New()
        labels = {"", "", "TextureFlow output", "TextureFlow Input"}
        desc = "Find the texture flow for the depth segments output"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        segments.Run(src)
        diffx.Run(segments.dst1)
        dst3 = segments.dst3
        flow.Run(dst3)
        dst2 = flow.dst2
    End Sub
End Class