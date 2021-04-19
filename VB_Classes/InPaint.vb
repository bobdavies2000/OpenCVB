Imports cv = OpenCvSharp


' https://docs.opencv.org/master/df/d3d/tutorial_py_inpainting.html#gsc.tab=0
Public Class InPaint_Basics : Inherits VBparent
    Public thickness As Integer
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Thickness", 1, 25, 2)
        End If
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 2)
            radio.check(0).Text = "TELEA"
            radio.check(1).Text = "Navier-Stokes"
            radio.check(0).Checked = True
        End If

        task.desc = "Create a flaw in an image and then use inPaint to mask it."
        label2 = "Repaired Image"
    End Sub
    Public Function drawRandomLine(dst As cv.Mat)
        Static thickSlider = findSlider("Thickness")
        thickness = thickSlider.value
        Dim p1 = New cv.Point2f(msRNG.Next(dst.Cols / 4, dst.Cols * 3 / 4), msRNG.Next(dst.Rows / 4, dst.Rows * 3 / 4))
        Dim p2 = New cv.Point2f(msRNG.Next(dst.Cols / 4, dst.Cols * 3 / 4), msRNG.Next(dst.Rows / 4, dst.Rows * 3 / 4))
        dst1.Line(p1, p2, New cv.Scalar(0, 0, 0), thickness, task.lineType)
        Dim mask = New cv.Mat(dst1.Size(), cv.MatType.CV_8UC1)
        mask.SetTo(0)
        mask.Line(p1, p2, cv.Scalar.All(255), thickness, task.lineType)
        Return mask
    End Function
    Public Sub Run(src as cv.Mat)
        Dim inPaintFlag = If(radio.check(0).Checked, cv.InpaintMethod.Telea, cv.InpaintMethod.NS)

        If task.frameCount Mod 30 Then Exit Sub
        src.CopyTo(dst1)
        Dim mask = drawRandomLine(dst1)
        cv.Cv2.Inpaint(dst1, mask, dst2, thickness, inPaintFlag)
    End Sub
End Class






Public Class InPaint_Noise : Inherits VBparent
    Dim noise As Draw_Noise
    Public Sub New()
        noise = New Draw_Noise()

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 2)
            radio.check(0).Text = "TELEA"
            radio.check(1).Text = "Navier-Stokes"
            radio.check(0).Checked = True
        End If

        task.desc = "Create noise in an image and then use inPaint to remove it."
        label2 = "Repaired Image"
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.frameCount Mod 100 Then Exit Sub ' give them time to review the inpaint results
        noise.Run(src) ' create some noise in the result1 image.
        dst1 = noise.dst1
        Dim inPaintFlag = If(radio.check(0).Checked, cv.InpaintMethod.Telea, cv.InpaintMethod.NS)
        cv.Cv2.Inpaint(dst1, noise.noiseMask, dst2, noise.maxNoiseWidth, inPaintFlag)
    End Sub
End Class







Public Class InPaint_Depth : Inherits VBparent
    Public Sub New()
        label1 = "32-bit representation of original depth"
        label2 = "32-bit depth repaired with inpainting"
        task.desc = "Use Navier-Stokes to fill in the holes in the depth"
    End Sub
    Public Sub Run(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f
        dst1 = src.Clone
        cv.Cv2.Inpaint(src, task.noDepthMask, dst2, 20, cv.InpaintMethod.NS)
    End Sub
End Class
