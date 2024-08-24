Imports cv = OpenCvSharp
Public Class History_Basics : Inherits VB_Parent
    Public saveFrames As New List(Of cv.Mat)
    Public Sub New()
        desc = "Create a frame history to sum the last X frames"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.frameHistoryCount = 1 Then
            dst2 = src
            Exit Sub
        End If
        If src.Type <> cv.MatType.CV_32F Then src.ConvertTo(src, cv.MatType.CV_32F)

        If dst1.Type <> src.Type Or dst1.Channels() <> src.Channels() Or task.optionsChanged Then
            dst1 = src
            saveFrames.Clear()
        End If

        If saveFrames.Count >= task.frameHistoryCount Then saveFrames.RemoveAt(0)
        saveFrames.Add(src.Clone)

        For Each m In saveFrames
            dst1 += m
        Next
        dst1 *= 1 / (saveFrames.Count + 1)
        If src.Channels() = 1 Then
            dst1.ConvertTo(dst2, cv.MatType.CV_8U)
        Else
            dst1.ConvertTo(dst2, cv.MatType.CV_8UC3)
        End If
    End Sub
End Class








Public Class History_MotionRect : Inherits VB_Parent
    Public Sub New()
        desc = "Create an image that is the motionRect applied to the previous image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then dst2 = src.Clone

        If task.motionDetected Then
            src(task.motionRect).CopyTo(dst2(task.motionRect))
        End If
    End Sub
End Class








Public Class History_Cloud : Inherits VB_Parent
    Public frames As New History_BasicsNoSaturation
    Dim saveFrames As New List(Of cv.Mat)
    Public Sub New()
        desc = "Create a frame history and sum the last X task.pointcloud's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Or src.Channels() <> 3 Then src = task.pointCloud

        If task.optionsChanged Or dst3.Type <> cv.MatType.CV_32FC3 Then
            saveFrames.Clear()
            dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC3, 0)
        End If

        If saveFrames.Count >= task.frameHistoryCount Then
            dst3 = dst3.Subtract(saveFrames.ElementAt(0))
            saveFrames.RemoveAt(0)
        End If

        saveFrames.Add(src.Clone)
        dst3 = src + dst3
        dst2 = dst3 / saveFrames.Count

        frames.Run(task.depthMask)
        dst2.SetTo(0, Not frames.dst2)
    End Sub
End Class





Public Class History_BasicsNoSaturation : Inherits VB_Parent
    Public saveFrames As New List(Of cv.Mat)
    Public Sub New()
        desc = "Create a frame history and sum the last X frames (without saturation!)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim input = src.Clone
        If input.Channels() <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If input.Type <> cv.MatType.CV_32F Then input.ConvertTo(input, cv.MatType.CV_32F)
        If dst3.Type <> input.Type Or dst3.Channels() <> input.Channels() Then dst3 = New cv.Mat(input.Size(), input.Type, 0)
        input /= 255 ' input is all zeros or ones.

        If task.optionsChanged Then
            saveFrames.Clear()
            dst3.SetTo(0)
        End If

        If saveFrames.Count >= task.frameHistoryCount Then
            dst3 = dst3.Subtract(saveFrames.ElementAt(0))
            saveFrames.RemoveAt(0)
        End If

        saveFrames.Add(input)
        dst3 += input
        dst1 = 255 * dst3 / saveFrames.Count
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)
    End Sub
End Class







Public Class History_BasicsDiff : Inherits VB_Parent
    Dim frames As New History_BasicsNoSaturation
    Dim diff As New Diff_Basics
    Public Sub New()
        task.gOptions.pixelDiffThreshold = 0
        desc = "Find the floodfill trouble spots."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        frames.Run(src)
        dst2 = ShowPalette(frames.dst2)

        diff.Run(frames.dst2)
        dst3 = diff.dst2
    End Sub
End Class





Public Class History_Basics8U : Inherits VB_Parent
    Public saveFrames As New List(Of cv.Mat)
    Dim mats As New Mat_4to1
    Dim lastFrame As cv.Mat
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Create a frame history by Or'ing the last X frames of CV_8U data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If task.FirstPass Then lastFrame = src.Clone
            cv.Cv2.Absdiff(src, lastFrame, dst3)
            lastFrame = src.Clone
            src = dst3.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
        End If

        If task.frameHistoryCount = 1 Then
            dst2 = src
            Exit Sub
        End If

        If saveFrames.Count > task.frameHistoryCount Then saveFrames.RemoveAt(0)
        saveFrames.Add(src.Clone)

        dst2.SetTo(0)
        For Each m In saveFrames
            dst2 = dst2 Or m
        Next

        If task.algName = traceName Then
            For i = 0 To Math.Min(saveFrames.Count, 4) - 1
                mats.mat(i) = saveFrames(i).Clone
            Next
            mats.Run(empty)
            dst3 = mats.dst2
        End If
    End Sub
End Class





Public Class History_ReliableDepth : Inherits VB_Parent
    Public saveFrames As New List(Of cv.Mat)
    Public Sub New()
        task.gOptions.FrameHistory.Value = 30
        desc = "Create a frame history by Or'ing the last X frames of CV_8U data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then src = task.noDepthMask

        If task.frameHistoryCount = 1 Then
            dst2 = task.depthMask
            Exit Sub
        End If

        If task.optionsChanged Then saveFrames.Clear()

        If saveFrames.Count > task.frameHistoryCount Then saveFrames.RemoveAt(0)
        saveFrames.Add(src.Clone)

        dst2 = saveFrames(0)
        For i = 1 To saveFrames.Count - 1
            dst2 = dst2 Or saveFrames(i)
        Next
        dst2 = Not dst2
        dst3.SetTo(0)
        task.depthRGB.CopyTo(dst3, dst2)
    End Sub
End Class