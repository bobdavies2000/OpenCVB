Imports cv = OpenCvSharp
Public Class Diff_Basics
    Inherits VBparent
    Public lastFrame As cv.Mat
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Change threshold for each pixel", 1, 255, 25)
        End If
        label1 = "Stable Color"
        label2 = "Unstable Color mask"
        task.desc = "Capture an image and compare it to previous frame using absDiff and threshold"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim gray = src
        If src.Channels = 3 Then gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If lastFrame Is Nothing Then lastFrame = src.Clone
        If task.frameCount > 0 Then
            dst1 = lastFrame
            cv.Cv2.Absdiff(gray, lastFrame, dst2)
            Static thresholdSlider = findSlider("Change threshold for each pixel")
            If dst2.Type = cv.MatType.CV_8U Then
                dst2 = dst2.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
            Else
                dst2 = dst2.ConvertScaleAbs(255).Threshold(thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)
            End If
            dst1 = src.Clone().SetTo(0, dst2)
        Else
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC1, 0)
        End If
        lastFrame = gray.Clone()
    End Sub
End Class




Public Class Diff_UnstableDepthAndColor
    Inherits VBparent
    Public diff As Diff_Basics
    Public depth As Depth_NotMissing
    Dim lastFrames() As cv.Mat
    Public Sub New()
        initParent()
        diff = New Diff_Basics()
        diff.sliders.trackbar(0).Value = 20 ' this is color threshold - low means detecting more motion.

        depth = New Depth_NotMissing()

        label1 = "Stable depth and color"
        task.desc = "Build a mask for any pixels that have either unstable depth or color"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        diff.Run(src)
        Dim unstableColor = diff.dst2.Clone()
        depth.Run(task.RGBDepth)
        Dim unstableDepth As New cv.Mat
        Dim mask As New cv.Mat
        cv.Cv2.BitwiseNot(depth.dst2, unstableDepth)
        If unstableColor.Channels = 3 Then unstableColor = unstableColor.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.BitwiseOr(unstableColor, unstableDepth, mask)
        dst1 = src.Clone()
        dst1.SetTo(0, mask)
        label2 = "Unstable depth/color mask"
        dst2 = mask
    End Sub
End Class


