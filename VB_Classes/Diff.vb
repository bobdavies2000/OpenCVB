Imports cvb = OpenCvSharp
Public Class Diff_Basics : Inherits TaskParent
    Public changedPixels As Integer
    Public lastFrame As cvb.Mat
    Public Sub New()
        labels = {"", "", "Unstable mask", ""}
        UpdateAdvice(traceName + ": use goption 'Pixel Difference Threshold' to control changed pixels.")
        desc = "Capture an image and compare it to previous frame using absDiff and threshold"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If task.firstPass Or lastFrame Is Nothing Then lastFrame = src.Clone
        If task.optionsChanged Or lastFrame.Size <> src.Size Then lastFrame = src.Clone

        cvb.Cv2.Absdiff(src, lastFrame, dst0)
        dst2 = dst0.Threshold(task.gOptions.pixelDiffThreshold, 255, cvb.ThresholdTypes.Binary)
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
    Public Sub RunAlg(src As cvb.Mat)
        diff.Run(src.Reshape(1, src.Rows * 3))
        dst2 = diff.dst2.Reshape(3, src.Rows)
        dst3 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class





Public Class Diff_UnstableDepthAndColor : Inherits TaskParent
    Public diff As New Diff_Basics
    Public depth As New Depth_NotMissing
    Public Sub New()
        labels = {"", "", "Stable depth and color", "Unstable depth/color mask"}
        desc = "Build a mask for any pixels that have either unstable depth or color"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        diff.Run(src)
        Dim unstableGray = diff.dst2.Clone()
        depth.Run(task.depthRGB)
        Dim unstableDepth As New cvb.Mat
        Dim mask As New cvb.Mat
        unstableDepth = Not depth.dst3
        If unstableGray.Channels() = 3 Then unstableGray = unstableGray.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        mask = unstableGray Or unstableDepth
        dst2 = src.Clone()
        dst2.SetTo(0, mask)
        dst3 = mask
    End Sub
End Class






Public Class Diff_RGBAccum : Inherits TaskParent
    Dim diff As New Diff_Basics
    Dim history As New List(Of cvb.Mat)
    Public Sub New()
        labels = {"", "", "Accumulated BGR image", "Mask of changed pixels"}
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Run Diff_Basics and accumulate BGR diff data."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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
    Dim lines As New Line_Basics
    Public Sub New()
        lines.displayLines = True
        labels = {"", "", "Add motion to see Diff output and lines input", "Wave at the camera to see results"}
        desc = "identify lines in the diff output"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        diff.Run(src)
        dst2 = diff.dst2

        lines.Run(dst2)
        dst3 = lines.dst2
        labels(2) = lines.labels(2)
    End Sub
End Class






Public Class Diff_Heartbeat : Inherits TaskParent
    Public cumulativePixels As Integer
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels = {"", "", "Unstable mask", "Pixel difference"}
        desc = "Diff an image with one from the last heartbeat."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Then
            dst1 = src.Clone
            dst2.SetTo(0)
        End If

        cvb.Cv2.Absdiff(src, dst1, dst3)
        cumulativePixels = dst3.CountNonZero
        dst2 = dst2 Or dst3.Threshold(task.gOptions.pixelDiffThreshold, 255, cvb.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Diff_Depth32f : Inherits TaskParent
    Public lastDepth32f As New cvb.Mat
    Dim options As New Options_DiffDepth
    Public Sub New()
        desc = "Where is the depth difference between frames greater than X centimeters."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If task.optionsChanged Or lastDepth32f.Width = 0 Then lastDepth32f = task.pcSplit(2).Clone

        cvb.Cv2.Absdiff(task.pcSplit(2), lastDepth32f, dst1)
        Dim mm As mmData = GetMinMax(dst1)

        dst2 = dst1.Threshold(options.meters, 255, cvb.ThresholdTypes.Binary)

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
    Public Sub RunAlg(src As cvb.Mat)
        diffColor.Run(src)
        dst2 = diffColor.dst2
        If diffColor.diff.changedPixels = 0 Then noMotionFrames += 1

        If task.heartBeat Then
            labels(2) = CStr(noMotionFrames) + " frames since the last heartbeat with no motion " +
                        " or " + Format(noMotionFrames / task.fpsRate, "0%")
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
