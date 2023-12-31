Imports cv = OpenCvSharp


' https://docs.opencv.org/master/df/d3d/tutorial_py_inpainting.html#gsc.tab=0
Public Class InPaint_Basics : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 2)
            radio.check(0).Text = "TELEA"
            radio.check(1).Text = "Navier-Stokes"
            radio.check(0).Checked = True
        End If

        task.desc = "Create a flaw in an image and then use inPaint to mask it."
        labels(3) = "Repaired Image"
    End Sub
    Public Function drawRandomLine(dst As cv.Mat)
        Dim p1 = New cv.Point2f(msRNG.Next(dst.Cols / 4, dst.Cols * 3 / 4), msRNG.Next(dst.Rows / 4, dst.Rows * 3 / 4))
        Dim p2 = New cv.Point2f(msRNG.Next(dst.Cols / 4, dst.Cols * 3 / 4), msRNG.Next(dst.Rows / 4, dst.Rows * 3 / 4))
        dst2.Line(p1, p2, New cv.Scalar(0, 0, 0), task.lineWidth, task.lineType)
        Dim mask = New cv.Mat(dst2.Size(), cv.MatType.CV_8UC1)
        mask.SetTo(0)
        mask.Line(p1, p2, cv.Scalar.All(255), task.lineWidth, task.lineType)
        Return mask
    End Function
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim inPaintFlag = If(radio.check(0).Checked, cv.InpaintMethod.Telea, cv.InpaintMethod.NS)

        src.CopyTo(dst2)
        Dim mask As cv.Mat = drawRandomLine(dst2)
        cv.Cv2.Inpaint(dst2, mask, dst3, task.lineWidth, inPaintFlag)
    End Sub
End Class






Public Class InPaint_Noise : Inherits VBparent
    Dim noise as New Draw_Noise
    Public Sub New()
        If radio.Setup(caller, 2) Then
            radio.check(0).Text = "TELEA"
            radio.check(1).Text = "Navier-Stokes"
            radio.check(0).Checked = True
        End If

        task.desc = "Create noise in an image and then use inPaint to remove it."
        labels(3) = "Repaired Image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.frameCount Mod 100 Then Exit Sub ' give them time to review the inpaint results
        noise.RunClass(src) ' create some noise in the result1 image.
        dst2 = noise.dst2
        Dim inPaintFlag = If(radio.check(0).Checked, cv.InpaintMethod.Telea, cv.InpaintMethod.NS)
        cv.Cv2.Inpaint(dst2, noise.noiseMask, dst3, noise.maxNoiseWidth, inPaintFlag)
    End Sub
End Class







Public Class InPaint_Depth : Inherits VBparent
    Public Sub New()
        labels(2) = "32-bit representation of original depth"
        labels(3) = "32-bit depth repaired with inpainting"
        task.desc = "Use Navier-Stokes to fill in the holes in the depth"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f
        dst2 = src.Clone
        cv.Cv2.Inpaint(src, task.noDepthMask, dst3, 20, cv.InpaintMethod.NS)
    End Sub
End Class
