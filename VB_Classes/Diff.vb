Imports cv = OpenCvSharp
Public Class Diff_Basics : Inherits VBparent
    Public lastFrame As cv.Mat
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Change threshold for each pixel", 1, 255, 25)
        End If
        labels(2) = "Stable Color"
        labels(3) = "Unstable Color mask"
        task.desc = "Capture an image and compare it to previous frame using absDiff and threshold"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("Change threshold for each pixel")
        Dim gray = src
        If src.Channels = 3 Then gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If lastFrame Is Nothing Then lastFrame = src.Clone
        If task.frameCount > 0 Then
            dst2 = lastFrame
            cv.Cv2.Absdiff(gray, lastFrame, dst3)
            If dst3.Type = cv.MatType.CV_8U Then
                dst3 = dst3.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
            Else
                dst3 = dst3.ConvertScaleAbs(255).Threshold(thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)
            End If
            dst2 = src.Clone().SetTo(0, dst3)
        Else
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8UC1, 0)
        End If
        lastFrame = gray.Clone()
    End Sub
End Class




Public Class Diff_UnstableDepthAndColor : Inherits VBparent
    Public diff As New Diff_Basics
    Public depth As New Depth_NotMissing
    Dim lastFrames() As cv.Mat
    Public Sub New()
        diff.sliders.trackbar(0).Value = 20 ' this is color threshold - low means detecting more motion.
        labels(2) = "Stable depth and color"
        task.desc = "Build a mask for any pixels that have either unstable depth or color"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        diff.Run(src)
        Dim unstableGray = diff.dst3.Clone()
        depth.Run(task.RGBDepth)
        Dim unstableDepth As New cv.Mat
        Dim mask As New cv.Mat
        cv.Cv2.BitwiseNot(depth.dst3, unstableDepth)
        If unstableGray.Channels = 3 Then unstableGray = unstableGray.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.BitwiseOr(unstableGray, unstableDepth, mask)
        dst2 = src.Clone()
        dst2.SetTo(0, mask)
        labels(3) = "Unstable depth/color mask"
        dst3 = mask
    End Sub
End Class


