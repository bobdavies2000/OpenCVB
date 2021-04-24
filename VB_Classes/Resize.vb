Imports cv = OpenCvSharp
Public Class Resize_Basics : Inherits VBparent
    Public newSize As cv.Size
    Public rotateOptions As New GetRotationMatrix2D_Options
    Public Sub New()
        ' warp is not allowed in resize
        rotateOptions.radio.check(5).Enabled = False
        rotateOptions.radio.check(6).Enabled = False

        task.desc = "Resize with different options and compare them"
        label1 = "Rectangle highlight above resized"
        label2 = "Difference from Cubic Resize (Best)"
    End Sub
    Public Sub Run(src as cv.Mat)
        Static frm = findfrm(caller + " Radio Options")

        rotateOptions.Run(src)

        If standalone Or task.intermediateReview = caller Then
            Dim roi = New cv.Rect(src.Width / 4, src.Height / 4, src.Width / 2, src.Height / 2)
            If task.drawRect.Width <> 0 Then roi = task.drawRect

            dst1 = src(roi).Resize(dst1.Size(), 0, 0, rotateOptions.warpFlag)
            dst2 = (src(roi).Resize(dst1.Size(), 0, 0, cv.InterpolationFlags.Cubic) - dst1).ToMat.Threshold(0, 255, cv.ThresholdTypes.Binary)
            src.Rectangle(roi, cv.Scalar.White, 2)
        Else
            dst1 = src.Resize(newSize, 0, 0, rotateOptions.warpFlag)
        End If
    End Sub
End Class







Public Class Resize_Percentage : Inherits VBparent
    Public resizeOptions As Resize_Basics
    Public Sub New()
        resizeOptions = New Resize_Basics()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Resize Percentage (%)", 1, 100, 3)
        End If
        task.desc = "Resize by a percentage of the image."
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim percent As Double = CDbl(sliders.trackbar(0).Value / 100)
        Dim resizePercent = sliders.trackbar(0).Value / 100
        resizePercent = Math.Sqrt(resizePercent)
        resizeOptions.newSize = New cv.Size(Math.Ceiling(src.Width * resizePercent), Math.Ceiling(src.Height * resizePercent))
        resizeOptions.Run(src)

        If standalone or task.intermediateReview = caller Then
            Dim roi As New cv.Rect(0, 0, resizeOptions.dst1.Width, resizeOptions.dst1.Height)
            dst1 = resizeOptions.dst1(roi).Resize(resizeOptions.dst1.Size())
            label1 = "Image after resizing to " + Format(sliders.trackbar(0).Value, "#0.0") + "% of original size"
            label2 = ""
        Else
            dst1 = resizeOptions.dst1
        End If
    End Sub
End Class



