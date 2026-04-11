Imports System.Diagnostics.Metrics
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

        ' If task.heartBeat Then lastGray = src.Clone

        cv.Cv2.Min(src, lastGray, dst2)
        src.CopyTo(dst2, task.motion.motionMask)

        cv.Cv2.Absdiff(lastGray, dst2, dst3)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        lastGray = dst2.Clone

        labels(2) = CStr(dst3.CountNonZero) + " pixels were updated with new minimums."
    End Sub
End Class



Public Class StableGray_Basics_TA : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(2) = "Accumulated pixels were updated with new min/max pixels."
        desc = "Accumulate Min values where there is no motion and don't compute difference in dst3."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray
        Static lastGray As cv.Mat = src.Clone

        ' If task.heartBeat Then lastGray = src.Clone

        ' the following statement can be min as well as max.
        cv.Cv2.Min(src, lastGray, dst2)
        src.CopyTo(dst2, task.motion.motionMask)

        lastGray = dst2.Clone

        task.gray = dst2.Clone
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

        'If task.heartBeat Then lastGray = src.Clone

        cv.Cv2.Max(src, lastGray, dst2)
        src.CopyTo(dst2, task.motion.motionMask)

        cv.Cv2.Absdiff(lastGray, dst2, dst3)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        lastGray = dst2.Clone

        labels(2) = CStr(dst3.CountNonZero) + " pixels were updated with new maximums."
    End Sub
End Class





Public Class StableGray_RGBMinMaxCompare : Inherits TaskParent
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




Public Class StableGray_RGBMin : Inherits TaskParent
    Dim stableB As New StableGray_BasicsMin
    Dim stableG As New StableGray_BasicsMin
    Dim stableR As New StableGray_BasicsMin
    Public Sub New()
        desc = "Build a StableRGB by running StableGray_BasicsMin with all 3 channels."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.gOptions.stabilizeDepthRGB.Checked Then
            Dim split = task.color.Split()

            stableB.Run(split(0))
            split(0) = stableB.dst2.Clone

            stableG.Run(split(1))
            split(1) = stableG.dst2.Clone

            stableR.Run(split(2))
            split(2) = stableR.dst2.Clone

            cv.Cv2.Merge(split, dst2)
        Else
            dst2 = task.color.Clone
        End If
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
        If task.gOptions.stabilizeDepthRGB.Checked Then
            Dim split = task.color.Split()

            stableB.Run(split(0))
            split(0) = stableB.dst2.Clone

            stableG.Run(split(1))
            split(1) = stableG.dst2.Clone

            stableR.Run(split(2))
            split(2) = stableR.dst2.Clone

            cv.Cv2.Merge(split, dst2)
        Else
            dst2 = task.color.Clone
        End If
    End Sub
End Class





Public Class StableGray_LeftRight : Inherits TaskParent
    Dim stableLeft As New StableGray_Basics_TA
    Dim stableRight As New StableGray_Basics_TA
    Public Sub New()
        labels = {"", "", "Stable gray left image", "Stable gray right image"}
        desc = "Create the stable gray left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.gOptions.stabilizeDepthRGB.Checked Then
            stableLeft.Run(task.leftView)
            dst2 = stableLeft.dst2
            If task.heartBeat Then labels(2) = stableLeft.labels(2)

            stableRight.Run(task.rightView)
            dst3 = stableRight.dst2
            If task.heartBeat Then labels(3) = stableRight.labels(2)
        Else
            dst2 = task.leftView
            dst3 = task.rightView
        End If
    End Sub
End Class




Public Class StableGray_MinMaxCompare : Inherits TaskParent
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







Public Class StableGray_MinMaxRange : Inherits TaskParent
    Dim compare As New StableGray_MinMaxCompare
    Dim plot As New PlotMouse_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Show the absDiff(min, max)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        compare.Run(emptyMat)
        dst1 = compare.dst3
        labels(1) = "Min gray image"

        cv.Cv2.Absdiff(compare.dst1, compare.dst3, dst0)
        Dim average = dst0.Mean().Val0
        labels(3) = Format(average, fmt2) + " average pixel difference between min and max stable gray."

        plot.Run(dst0)
        dst2 = plot.dst2
        dst3 = plot.dst3
        labels(2) = plot.labels(2)
    End Sub
End Class




Public Class StableGray_Measure_TA : Inherits TaskParent
    Dim plot As New PlotMouse_Basics
    Public percentZero As Single
    Public averageDiff As Single
    Public averageMinimum As Single
    Dim averageHistory As New List(Of Single)
    Public motionDecision As Boolean
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Measure the amount of variation in the grayscale image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lastFrame As cv.Mat = task.grayOriginal

        cv.Cv2.Absdiff(task.grayOriginal, lastFrame, dst0)
        averageDiff = dst0.Mean().Val0

        averageHistory.Add(averageDiff)
        If averageHistory.Count > 50 Then averageHistory.RemoveAt(0)
        averageMinimum = averageHistory.Min
        motionDecision = averageDiff >= averageMinimum + 0.1
        labels(3) = "Motion decision: " + CStr(motionDecision) + " " + Format(averageDiff, fmt2) +
                    " average pixel difference gray to stable gray."

        plot.Run(dst0)
        dst2 = plot.dst2
        dst3 = plot.dst3
        labels(2) = plot.labels(2)

        percentZero = 100 * plot.plotHist.histArray(0) / src.Total
        strOut = "Diff" + vbTab + "Count" + vbCrLf
        For i = 0 To plot.plotHist.histArray.Count - 1
            strOut += CStr(i) + vbTab + Format(plot.plotHist.histArray(i), fmt0) + vbCrLf
        Next
        SetTrueText(strOut, 1)

        lastFrame = task.grayOriginal.Clone
    End Sub
End Class




Public Class StableGray_MeasureOverTime : Inherits TaskParent
    Dim plotPercent As New PlotTime_Basics
    Dim plotAverage As New PlotTime_Basics
    Public Sub New()
        plotPercent.maxScale = 100
        plotPercent.minScale = 0
        plotPercent.plotCount = 1
        plotAverage.maxScale = 10
        plotAverage.minScale = 0
        plotAverage.plotCount = 1

        desc = "Measure % of pixels with zero difference over time."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        plotPercent.plotData = New cv.Scalar(task.motionStable.percentZero, 0, 0)
        plotPercent.Run(src)
        dst2 = plotPercent.dst2

        plotAverage.plotData = New cv.Scalar(task.motionStable.averageDiff, 0, 0)
        plotAverage.Run(src)
        dst3 = plotAverage.dst2

        labels(2) = "Percent of image identical at the pixel level = " + Format(task.motionStable.percentZero / 100, "0%")
        labels(3) = task.motionStable.labels(3)
    End Sub
End Class





Public Class StableGray_ToggleMinMax : Inherits TaskParent
    Dim minRGB As New StableGray_BasicsMin
    Dim maxRGB As New StableGray_BasicsMax
    Public Sub New()
        desc = "Toggle between the min and max stable gray images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.toggleOn Then
            minRGB.Run(task.grayOriginal)
            dst2 = minRGB.dst2
            labels(2) = "Min gray image"
        Else
            maxRGB.Run(task.grayOriginal)
            dst2 = maxRGB.dst2
            labels(2) = "Max gray image - should be a little lighter."
        End If
    End Sub
End Class
