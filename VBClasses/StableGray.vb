Imports cv = OpenCvSharp
Public Class StableGray_BasicsMin : Inherits TaskParent
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





Public Class StableGray_BasicsMax : Inherits TaskParent
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





Public Class StableGray_Compare : Inherits TaskParent
    Dim minRGB As New StableGray_BasicsMin
    Dim maxRGB As New StableGray_BasicsMax
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





Public Class StableGray_CompareColor : Inherits TaskParent
    Dim minRGB As New StableGray_RGBMin
    Dim maxRGB As New StableGray_RGBMax
    Public Sub New()
        desc = "Compare the difference between the min and max accumulated images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        minRGB.Run(src)
        dst2 = minRGB.dst2
        labels(2) = "Min RGB image"

        maxRGB.Run(src)
        dst3 = maxRGB.dst2
        labels(3) = "Max RGB image - should be a little lighter."
    End Sub
End Class






Public Class StableGray_MinMaxRange : Inherits TaskParent
    Dim compare As New StableGray_Compare
    Dim plot As New PlotMouse_Basics
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






Public Class StableGray_RGBMin : Inherits TaskParent
    Dim stableB As New StableGray_BasicsMin
    Dim stableG As New StableGray_BasicsMin
    Dim stableR As New StableGray_BasicsMin
    Public Sub New()
        desc = "Build a StableRGB by running StableGray_BasicsMin with all 3 channels."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim split = task.color.Split()

        stableB.Run(split(0))
        split(0) = stableB.dst2.Clone

        stableG.Run(split(1))
        split(1) = stableG.dst2.Clone

        stableR.Run(split(2))
        split(2) = stableR.dst2.Clone

        cv.Cv2.Merge(split, dst2)
    End Sub
End Class






Public Class StableGray_RGBMax : Inherits TaskParent
    Dim stableB As New StableGray_BasicsMax
    Dim stableG As New StableGray_BasicsMax
    Dim stableR As New StableGray_BasicsMax
    Public Sub New()
        desc = "Build a StableRGB by running StableGray_BasicsMin with all 3 channels."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim split = task.color.Split()

        stableB.Run(split(0))
        split(0) = stableB.dst2.Clone

        stableG.Run(split(1))
        split(1) = stableG.dst2.Clone

        stableR.Run(split(2))
        split(2) = stableR.dst2.Clone

        cv.Cv2.Merge(split, dst2)
    End Sub
End Class
