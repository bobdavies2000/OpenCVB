Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Diff_Basics : Inherits TaskParent
        Public changedPixels As Integer
        Public lastFrame As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 255)
        Public Sub New()
            labels = {"", "", "Highlighting the changed pixels ", "AbsDiff output"}
            desc = "Capture an image and compare it to previous frame using absDiff and threshold"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            If task.firstPass Then lastFrame.SetTo(0)

            cv.Cv2.Absdiff(src, lastFrame, dst3)
            dst2 = dst3.Threshold(task.colorDiffThreshold, 255, cv.ThresholdTypes.Binary)
            changedPixels = dst2.CountNonZero
            If changedPixels > 0 Then
                lastFrame = src.Clone
                strOut = "Motion detected - " + CStr(changedPixels) + " pixels changed with threshold " +
                          CStr(task.colorDiffThreshold)
            End If
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
        Dim options As New Options_History
        Public Sub New()
            labels = {"", "", "Accumulated BGR image", "Mask of changed pixels"}
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Run Diff_Basics and accumulate BGR diff data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If task.optionsChanged Then history.Clear()

            diff.Run(src)
            history.Add(diff.dst2)
            If history.Count > task.frameHistoryCount Then history.RemoveAt(0)

            dst2.SetTo(0)
            For Each m In history
                dst2 = dst2 Or m
            Next
        End Sub
    End Class








    Public Class Diff_Depth32f : Inherits TaskParent
        Public lastDepth32f As cv.Mat
        Dim options As New Options_DiffDepth
        Public Sub New()
            desc = "Where is the depth difference between frames greater than X centimeters."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2).Clone

            If task.optionsChanged Or lastDepth32f Is Nothing Then lastDepth32f = task.pcSplit(2).Clone

            cv.Cv2.Absdiff(src, lastDepth32f, dst1)
            Dim mm As mmData = GetMinMax(dst1)

            dst2 = dst1.Threshold(options.meters, 255, cv.ThresholdTypes.Binary)

            lastDepth32f = src.Clone
            If task.heartBeat Then
                labels(2) = "Mask where depth difference between frames is more than " + CStr(options.millimeters) + " mm's"
                Dim count = dst2.CountNonZero()
                labels(3) = CStr(count) + " pixels (" + Format(count / task.depthmask.CountNonZero, "0%") +
                        " of all depth pixels) were different by more than " + CStr(options.millimeters) + " mm's"
            End If
        End Sub
    End Class







    Public Class NR_Diff_Identical : Inherits TaskParent
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




    Public Class Diff_RGB : Inherits TaskParent
        Dim diff(2) As Diff_Basics
        Dim mats As New Mat_4Click
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            For i = 0 To diff.Count - 1
                diff(i) = New Diff_Basics
            Next
            labels(3) = "This is the diff of the grayscale image."
            desc = "Create a mask that shows when R, G, and B are different.  Compare it to diff for grayscale."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim split = src.Split()
            dst2.SetTo(0)
            For i = 0 To 2
                diff(i).Run(split(i))
                mats.mat(i) = diff(i).dst2
                mats.mat(i).SetTo(1, mats.mat(i))
                dst2 += mats.mat(i)
            Next

            dst2 = dst2.Threshold(2, 255, cv.ThresholdTypes.Binary)
            dst3 = task.motionBasics.dst2

            If task.heartBeat Then
                labels(2) = "Diff of RGB.split has " + CStr(dst2.CountNonZero) + " while gray has " + CStr(dst3.CountNonZero)
            End If
        End Sub
    End Class

End Namespace