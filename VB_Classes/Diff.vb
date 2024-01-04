Imports cv = OpenCvSharp
Public Class Diff_Basics : Inherits VB_Algorithm
    Public changedPixels As Integer
    Public lastGray As New cv.Mat
    Public Sub New()
        labels = {"", "", "Stable gray", "Unstable mask"}
        desc = "Capture an image and compare it to previous frame using absDiff and threshold"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If firstPass Then lastGray = src.Clone
        If task.optionsChanged Or lastGray.Size <> src.Size Then
            lastGray = src.Clone
            dst3 = src.Clone
        End If

        cv.Cv2.Absdiff(src, lastGray, dst0)
        dst3 = dst0.Threshold(gOptions.PixelDiffThreshold.Value, 255, cv.ThresholdTypes.Binary)
        changedPixels = dst3.CountNonZero
        If changedPixels > 0 Then
            dst3 = dst0.Threshold(gOptions.PixelDiffThreshold.Value, 255, cv.ThresholdTypes.Binary)
            dst2 = src.Clone
            dst2.SetTo(0, dst3)
            lastGray = src.Clone
        End If
    End Sub
End Class






Public Class Diff_Color : Inherits VB_Algorithm
    Public diff As New Diff_Basics
    Public Sub New()
        labels = {"", "", "Each channel displays the channel's difference", "Mask with all differences"}
        desc = "Use Diff_Basics with a color image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If firstPass Then diff.lastGray = src.Reshape(1, src.Rows * 3)
        diff.Run(src.Reshape(1, src.Rows * 3))
        dst2 = diff.dst3.Reshape(3, src.Rows)
        dst3 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class





Public Class Diff_UnstableDepthAndColor : Inherits VB_Algorithm
    Public diff As New Diff_Basics
    Public depth As New Depth_NotMissing
    Public Sub New()
        labels = {"", "", "Stable depth and color", "Unstable depth/color mask"}
        desc = "Build a mask for any pixels that have either unstable depth or color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        diff.Run(src)
        Dim unstableGray = diff.dst3.Clone()
        depth.Run(task.depthRGB)
        Dim unstableDepth As New cv.Mat
        Dim mask As New cv.Mat
        unstableDepth = Not depth.dst3
        If unstableGray.Channels = 3 Then unstableGray = unstableGray.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        mask = unstableGray Or unstableDepth
        dst2 = src.Clone()
        dst2.SetTo(0, mask)
        dst3 = mask
    End Sub
End Class








Public Class Diff_Depth : Inherits VB_Algorithm
    Dim diff As New Diff_Basics ' not used but has options...
    Public depthFrames As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Depth varies more than X mm's", 1, 100, 50)
        desc = "Where is the depth difference between frames greater than X centimeters."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static mmSlider = findSlider("Depth varies more than X mm's")
        Dim millimeters As Integer = mmSlider.value

        Dim depth32f As cv.Mat = 1000 * task.pcSplit(2)
        depth32f.ConvertTo(dst0, cv.MatType.CV_32S)

        Static lastDepth32s As cv.Mat = dst0.Clone
        If task.optionsChanged Then lastDepth32s = dst0.Clone

        cv.Cv2.Absdiff(dst0, lastDepth32s, dst1)
        dst1 = dst1.ConvertScaleAbs
        Dim mm = vbMinMax(dst1)

        dst2 = dst1.Threshold(millimeters - 1, 255, cv.ThresholdTypes.Binary)

        lastDepth32s = dst0.Clone
        If heartBeat() Then
            labels(2) = "Mask where depth difference between frames is more than " + CStr(millimeters) + " mm's"
            Dim count = dst2.CountNonZero()
            labels(3) = CStr(count) + " pixels (" + Format(count / task.depthMask.CountNonZero, "0%") +
                        " of all depth pixels)" + "were different by more than " + CStr(millimeters) + " mm's"
        End If
    End Sub
End Class








Public Class Diff_RGBAccum : Inherits VB_Algorithm
    Dim diff As New Diff_Basics
    Dim history As New List(Of cv.Mat)
    Public Sub New()
        labels = {"", "", "Accumulated BGR image", "Mask of changed pixels"}
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Run Diff_Basics and accumulate BGR diff data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        diff.Run(src)
        If task.optionsChanged Then history.Clear()
        history.Add(diff.dst3)
        If history.Count > task.historyCount Then history.RemoveAt(0)

        dst2.SetTo(0)
        For Each m In history
            dst2 = dst2 Or m
        Next
    End Sub
End Class








Public Class Diff_Lines : Inherits VB_Algorithm
    Dim diff As New Diff_RGBAccum
    Dim lines As New Line_Basics
    Public Sub New()
        labels = {"", "", "Diff output, lines input", "Lines output"}
        desc = "identify lines in the diff output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        diff.Run(src)
        dst2 = diff.dst2

        lines.Run(dst2)
        dst3 = src
        For Each mp In lines.mpList
            dst3.Line(mp.p1, mp.p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
    End Sub
End Class






Public Class Diff_Heartbeat : Inherits VB_Algorithm
    Public cumulativePixels As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "Unstable mask", "Pixel difference"}
        desc = "Diff an image with one from the last heartbeat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If heartBeat() Then
            dst1 = src.Clone
            dst2.SetTo(0)
        End If

        cv.Cv2.Absdiff(src, dst1, dst3)
        cumulativePixels = dst3.CountNonZero
        dst2 = dst2 Or dst3.Threshold(gOptions.PixelDiffThreshold.Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Diff_DepthAccum : Inherits VB_Algorithm
    Dim diff As New Diff_Depth
    Dim sum8u As New History_Sum8u
    Public Sub New()
        desc = "Accumulate the mask of depth differences."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        diff.Run(src)
        sum8u.Run(diff.dst2)
        dst2 = sum8u.dst2
        labels = diff.labels
    End Sub
End Class