Imports cv = OpenCvSharp
Public Class Resize_Basics : Inherits VBparent
    Public newSize As cv.Size
    Public rotateOptions As New Resize_Options
    Public Sub New()
        ' warp is not allowed in resize.  The options are shared with GetRotationMatrix2D_Basics
        findRadio("WarpFillOutliers").Enabled = False
        findRadio("WarpInverseMap").Enabled = False

        task.desc = "Resize with different options and compare them"
        label1 = "Rectangle highlight above resized"
        label2 = "Difference from Cubic Resize (Best)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        rotateOptions.Run(src)

        If standalone Or task.intermediateName = caller Then
            Dim roi = New cv.Rect(src.Width / 4, src.Height / 4, src.Width / 2, src.Height / 2)
            If task.drawRect.Width <> 0 Then roi = task.drawRect

            dst2 = src(roi).Resize(dst2.Size(), 0, 0, rotateOptions.warpFlag)
            dst3 = (src(roi).Resize(dst2.Size(), 0, 0, cv.InterpolationFlags.Cubic) - dst2).ToMat.Threshold(0, 255, cv.ThresholdTypes.Binary)
            src.Rectangle(roi, cv.Scalar.White, 2)
        Else
            dst2 = src.Resize(newSize, 0, 0, rotateOptions.warpFlag)
        End If
    End Sub
End Class







Public Class Resize_Percentage : Inherits VBparent
    Public resizeOptions As New Resize_Basics
    Public Sub New()
        resizeOptions = New Resize_Basics()
        If sliders.Setup(caller) Then sliders.setupTrackBar(0, "Resize Percentage (%)", 1, 100, 3)
        task.desc = "Resize by a percentage of the image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim percent As Double = CDbl(sliders.trackbar(0).Value / 100)
        Dim resizePercent = sliders.trackbar(0).Value / 100
        resizePercent = Math.Sqrt(resizePercent)
        resizeOptions.newSize = New cv.Size(Math.Ceiling(src.Width * resizePercent), Math.Ceiling(src.Height * resizePercent))
        resizeOptions.Run(src)

        If standalone or task.intermediateName = caller Then
            Dim roi As New cv.Rect(0, 0, resizeOptions.dst2.Width, resizeOptions.dst2.Height)
            dst2 = resizeOptions.dst2(roi).Resize(resizeOptions.dst2.Size())
            label1 = "Image after resizing to " + Format(sliders.trackbar(0).Value, "#0.0") + "% of original size"
            label2 = ""
        Else
            dst2 = resizeOptions.dst2
        End If
    End Sub
End Class






Public Class Resize_Options : Inherits VBparent
    Public warpFlag As cv.InterpolationFlags
    Public radioIndex As Integer
    Public Sub New()
        If radio.Setup(caller, 7) Then
            radio.check(0).Text = "Area"
            radio.check(1).Text = "Cubic flag (best blended)"
            radio.check(2).Text = "Lanczos4"
            radio.check(3).Text = "Linear"
            radio.check(4).Text = "Nearest (preserves pixel values best)"
            radio.check(5).Text = "WarpFillOutliers"
            radio.check(6).Text = "WarpInverseMap"
            radio.check(3).Checked = True
        End If

        task.desc = "Options for Resize_Basics and GetRototaionMatrix2D_Basics"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static frm = findfrm(caller + " Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                warpFlag = Choose(i + 1, cv.InterpolationFlags.Area, cv.InterpolationFlags.Cubic, cv.InterpolationFlags.Lanczos4, cv.InterpolationFlags.Linear,
                                         cv.InterpolationFlags.Nearest, cv.InterpolationFlags.WarpFillOutliers, cv.InterpolationFlags.WarpInverseMap)
                radioIndex = i
                Exit For
            End If
        Next

        If standalone Or task.intermediateName = caller Then setTrueText("No output - just options for Resize and GetRotationMatrix2D")
    End Sub
End Class