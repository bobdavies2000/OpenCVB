Imports cv = OpenCvSharp
Public Class StableRGB_Basics : Inherits TaskParent
    Public Sub New()
        labels(3) = "Mask of pixels that are different between last image and current after processing."
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Accumulate Min values where there is no motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray
        Static lastGray As cv.Mat = src.Clone

        cv.Cv2.Min(src, lastGray, dst2)
        src.CopyTo(dst2, task.motion.motionMask)

        cv.Cv2.Absdiff(lastGray, dst2, dst3)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        lastGray = dst2.Clone

        labels(2) = CStr(dst3.CountNonZero) + " pixels were updated with new minimums."
    End Sub
End Class




Public Class StableRGB_Max : Inherits TaskParent
    Public Sub New()
        labels(3) = "Mask of pixels that are different between last image and current after processing."
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Accumulate Max values where there is no motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray
        Static lastGray As cv.Mat = src.Clone

        cv.Cv2.Max(src, lastGray, dst2)
        src.CopyTo(dst2, task.motion.motionMask)

        cv.Cv2.Absdiff(lastGray, dst2, dst3)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        lastGray = dst2.Clone

        labels(2) = CStr(dst3.CountNonZero) + " pixels were updated with new minimums."
    End Sub
End Class





Public Class StableRGB_Compare : Inherits TaskParent
    Dim minRGB As New StableRGB_Basics
    Dim maxRGB As New StableRGB_Max
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Compare the difference between the min and max accumulated images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        minRGB.Run(task.gray)
        dst1 = minRGB.dst2
        labels(1) = "Min gray image"

        maxRGB.Run(task.gray)
        dst3 = maxRGB.dst2
        labels(3) = "Max gray image - should be a little lighter."

        cv.Cv2.Absdiff(dst1, dst3, dst0)
        dst2 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        Dim count = dst2.CountNonZero
        labels(2) = CStr(count) + " pixels or " + Format(count / src.Total, "0.0%") +
                    " differ between min and max images. (Should be very high)"
    End Sub
End Class






Public Class StableRGB_MinMaxRange : Inherits TaskParent
    Dim compare As New StableRGB_Compare
    Dim plot As New plotMouse_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Show the absDiff(min, max)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        compare.Run(task.gray)
        dst1 = compare.dst0
        labels(1) = "Min gray image"

        plot.Run(dst1)
        dst2 = plot.dst2

        dst3 = compare.dst3.Clone
    End Sub
End Class
