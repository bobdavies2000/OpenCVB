Imports cv = OpenCvSharp


' https://docs.opencvb.org/master/df/d3d/tutorial_py_inpainting.html#gsc.tab=0
Public Class InPaint_Basics : Inherits TaskParent
    Dim options As New Options_InPaint
    Public Sub New()
        desc = "Create a flaw in an image and then use inPaint to mask it."
        labels(3) = "Repaired Image"
    End Sub
    Public Function drawRandomLine(dst As cv.Mat) As cv.Mat
        Dim p1 = New cv.Point2f(msRNG.Next(dst.Cols / 4, dst.Cols * 3 / 4), msRNG.Next(dst.Rows / 4, dst.Rows * 3 / 4))
        Dim p2 = New cv.Point2f(msRNG.Next(dst.Cols / 4, dst.Cols * 3 / 4), msRNG.Next(dst.Rows / 4, dst.Rows * 3 / 4))
        DrawLine(dst2, p1, p2, New cv.Scalar(0, 0, 0))
        Dim mask = New cv.Mat(dst2.Size(), cv.MatType.CV_8UC1)
        mask.SetTo(0)
        DrawLine(mask, p1, p2, cv.Scalar.All(255))
        Return mask
    End Function
    Public Overrides sub runAlg(src As cv.Mat)
        Options.RunOpt()
        src.CopyTo(dst2)
        Dim mask As cv.Mat = drawRandomLine(dst2)
        cv.Cv2.Inpaint(dst2, mask, dst3, task.lineWidth, If(options.telea, cv.InpaintMethod.Telea, cv.InpaintMethod.NS))
    End Sub
End Class






Public Class InPaint_Noise : Inherits TaskParent
    Dim noise as New Draw_Noise
    Dim options As New Options_InPaint
    Public Sub New()
        desc = "Create noise in an image and then use inPaint to remove it."
        labels(3) = "Repaired Image"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        Options.RunOpt()
        noise.Run(src) ' create some noise in the result1 image.
        dst2 = noise.dst2
        cv.Cv2.Inpaint(dst2, noise.noiseMask, dst3, noise.options.noiseWidth, If(options.telea, cv.InpaintMethod.Telea, cv.InpaintMethod.NS))
    End Sub
End Class







Public Class InPaint_Depth : Inherits TaskParent
    Dim options As New Options_InPaint
    Public Sub New()
        labels(2) = "32-bit representation of original depth"
        labels(3) = "32-bit depth repaired with inpainting"
        desc = "Use Navier-Stokes to fill in the holes in the depth"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        dst2 = src.Clone
        cv.Cv2.Inpaint(src, task.noDepthMask, dst3, 20, If(options.telea, cv.InpaintMethod.Telea, cv.InpaintMethod.NS))
    End Sub
End Class







Public Class InPaint_PointCloud : Inherits TaskParent
    Dim options As New Options_InPaint
    Public Sub New()
        labels(2) = "Pointcloud before inpaint"
        labels(3) = "Pointcloud after inpaint"
        desc = "Use Navier-Stokes to fill in the holes in the depth"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()
        dst2 = task.pointCloud.Clone

        Dim split(2) As cv.Mat
        For i = 0 To task.pcSplit.Count - 1
            split(i) = New cv.Mat
            cv.Cv2.Inpaint(task.pcSplit(i), task.noDepthMask, split(i), 20,
                            If(options.telea, cv.InpaintMethod.Telea, cv.InpaintMethod.NS))
        Next
        cv.Cv2.Merge(split, dst3)
    End Sub
End Class
