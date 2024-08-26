Imports cvb = OpenCvSharp


' https://docs.opencvb.org/master/df/d3d/tutorial_py_inpainting.html#gsc.tab=0
Public Class InPaint_Basics : Inherits VB_Parent
    Dim options As New Options_InPaint
    Public Sub New()
        desc = "Create a flaw in an image and then use inPaint to mask it."
        labels(3) = "Repaired Image"
    End Sub
    Public Function drawRandomLine(dst As cvb.Mat) As cvb.Mat
        Dim p1 = New cvb.Point2f(msRNG.Next(dst.Cols / 4, dst.Cols * 3 / 4), msRNG.Next(dst.Rows / 4, dst.Rows * 3 / 4))
        Dim p2 = New cvb.Point2f(msRNG.Next(dst.Cols / 4, dst.Cols * 3 / 4), msRNG.Next(dst.Rows / 4, dst.Rows * 3 / 4))
        DrawLine(dst2, p1, p2, New cvb.Scalar(0, 0, 0))
        Dim mask = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8UC1)
        mask.SetTo(0)
        DrawLine(mask, p1, p2, cvb.Scalar.All(255))
        Return mask
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()
        src.CopyTo(dst2)
        Dim mask As cvb.Mat = drawRandomLine(dst2)
        cvb.Cv2.Inpaint(dst2, mask, dst3, task.lineWidth, If(options.telea, cvb.InpaintMethod.Telea, cvb.InpaintMethod.NS))
    End Sub
End Class






Public Class InPaint_Noise : Inherits VB_Parent
    Dim noise as New Draw_Noise
    Dim options As New Options_InPaint
    Public Sub New()
        desc = "Create noise in an image and then use inPaint to remove it."
        labels(3) = "Repaired Image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()
        noise.Run(src) ' create some noise in the result1 image.
        dst2 = noise.dst2
        cvb.Cv2.Inpaint(dst2, noise.noiseMask, dst3, noise.options.noiseWidth, If(options.telea, cvb.InpaintMethod.Telea, cvb.InpaintMethod.NS))
    End Sub
End Class







Public Class InPaint_Depth : Inherits VB_Parent
    Dim options As New Options_InPaint
    Public Sub New()
        labels(2) = "32-bit representation of original depth"
        labels(3) = "32-bit depth repaired with inpainting"
        desc = "Use Navier-Stokes to fill in the holes in the depth"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()
        If src.Type <> cvb.MatType.CV_32F Then src = task.pcSplit(2)
        dst2 = src.Clone
        cvb.Cv2.Inpaint(src, task.noDepthMask, dst3, 20, If(options.telea, cvb.InpaintMethod.Telea, cvb.InpaintMethod.NS))
    End Sub
End Class
