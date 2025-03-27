Imports cv = OpenCvSharp
Public Class Diff_Basics : Inherits TaskParent
    Public changedPixels As Integer
    Public lastFrame As cv.Mat
    Public Sub New()
        labels = {"", "", "Unstable mask", ""}
        UpdateAdvice(traceName + ": use goption 'Color Difference Threshold' to control changed pixels.")
        desc = "Capture an image and compare it to previous frame using absDiff and threshold"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.firstPass Or lastFrame Is Nothing Then lastFrame = src.Clone
        If task.optionsChanged Or lastFrame.Size <> src.Size Then lastFrame = src.Clone

        cv.Cv2.Absdiff(src, lastFrame, dst0)
        dst2 = dst0.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
        changedPixels = dst2.CountNonZero
        If changedPixels > 0 Then
            lastFrame = src.Clone
            strOut = "Motion detected - " + CStr(changedPixels) + " pixels changed with threshold " + CStr(task.gOptions.pixelDiffThreshold)
            If task.heartBeat Then labels(3) = strOut
        Else
            strOut = "No motion detected"
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class Diff_Color : Inherits TaskParent
    Public diff As New Diff_Basics
    Public Sub New()
        labels = {"", "", "Each channel displays the channel's difference", "Mask with all differences"}
        desc = "Use Diff_Basics with a color image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        diff.Run(src.Reshape(1, src.Rows * 3))
        dst2 = diff.dst2.Reshape(3, src.Rows)
        dst3 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class





Public Class Diff_UnstableDepthAndColor : Inherits TaskParent
    Public diff As New Diff_Basics
    Public depth As New Depth_NotMissing
    Public Sub New()
        labels = {"", "", "Stable depth and color", "Unstable depth/color mask"}
        desc = "Build a mask for any pixels that have either unstable depth or color"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        diff.Run(src)
        Dim unstableGray = diff.dst2.Clone()
        depth.Run(task.depthRGB)
        Dim unstableDepth As New cv.Mat
        Dim mask As New cv.Mat
        unstableDepth = Not depth.dst3
        If unstableGray.Channels() = 3 Then unstableGray = unstableGray.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        mask = unstableGray Or unstableDepth
        dst2 = src.Clone()
        dst2.SetTo(0, mask)
        dst3 = mask
    End Sub
End Class






Public Class Diff_RGBAccum : Inherits TaskParent
    Dim diff As New Diff_Basics
    Dim history As New List(Of cv.Mat)
    Public Sub New()
        labels = {"", "", "Accumulated BGR image", "Mask of changed pixels"}
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Run Diff_Basics and accumulate BGR diff data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        diff.Run(src)
        If task.optionsChanged Then history.Clear()
        history.Add(diff.dst2)
        If history.Count > task.frameHistoryCount Then history.RemoveAt(0)

        dst2.SetTo(0)
        For Each m In history
            dst2 = dst2 Or m
        Next
    End Sub
End Class








Public Class Diff_Lines : Inherits TaskParent
    Dim diff As New Diff_RGBAccum
    Public Sub New()
        labels = {"", "", "Add motion to see Diff output and lines input", "Wave at the camera to see results"}
        desc = "identify lines in the diff output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        diff.Run(src)
        dst2 = diff.dst2

        task.lines.Run(dst2)
        dst3 = task.lines.dst2
        labels(2) = task.lines.labels(2)
    End Sub
End Class







Public Class Diff_Depth32f : Inherits TaskParent
    Public lastDepth32f As New cv.Mat
    Dim options As New Options_DiffDepth
    Public Sub New()
        desc = "Where is the depth difference between frames greater than X centimeters."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.optionsChanged Or lastDepth32f.Width = 0 Then lastDepth32f = task.pcSplit(2).Clone

        cv.Cv2.Absdiff(task.pcSplit(2), lastDepth32f, dst1)
        Dim mm As mmData = GetMinMax(dst1)

        dst2 = dst1.Threshold(options.meters, 255, cv.ThresholdTypes.Binary)

        lastDepth32f = task.pcSplit(2).Clone
        If task.heartBeat Then
            labels(2) = "Mask where depth difference between frames is more than " + CStr(options.millimeters) + " mm's"
            Dim count = dst2.CountNonZero()
            labels(3) = CStr(count) + " pixels (" + Format(count / task.depthMask.CountNonZero, "0%") +
                        " of all depth pixels) were different by more than " + CStr(options.millimeters) + " mm's"
        End If
    End Sub
End Class







Public Class Diff_Identical : Inherits TaskParent
    Dim diffColor As New Diff_Color
    Dim noMotionFrames As Integer
    Dim flowText As New List(Of String)
    Public Sub New()
        desc = "Count frames that are identical to the previous - a driver issue.  The interrupt is triggered by something other than an RGB image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        diffColor.Run(src)
        dst2 = diffColor.dst2
        If diffColor.diff.changedPixels = 0 Then noMotionFrames += 1

        If task.heartBeat Then
            labels(2) = CStr(noMotionFrames) + " frames since the last heartbeat with no motion " +
                        " or " + Format(noMotionFrames / task.fpsAlgorithm, "0%")
            flowText.Add(labels(2))
            noMotionFrames = 0
            If flowText.Count > 20 Then flowText.RemoveAt(0)
            strOut = ""
            For Each txt In flowText
                strOut += txt + vbCrLf
            Next
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class
