Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Imports System.Runtime.InteropServices
Public Class Interpolate_Basics : Inherits TaskParent
    Public options As New Options_Resize
    Public iOptions As New Options_Interpolate
    Dim direction = 1
    Public Sub New()
        desc = "Resize image using all available interpolation methods in OpenCV"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        iOptions.Run()
        Static saveSliderValue As Integer = iOptions.interpolationThreshold

        If standaloneTest() Then
            Dim userGrab As Boolean = iOptions.interpolationThreshold <> iOptions.saveDefaultThreshold
            If userGrab = False Then
                saveSliderValue += direction
                If saveSliderValue > 50 Then direction = -1
                If saveSliderValue = 1 Then direction = 1
            Else
                userGrab = True
                saveSliderValue = iOptions.interpolationThreshold
            End If
        Else
            saveSliderValue = iOptions.interpolationThreshold
        End If

        dst2 = src.Clone
        Dim newSize = New cv.Size(CInt(dst2.Width * saveSliderValue / 100), CInt(dst2.Height * saveSliderValue / 100))
        cv.Cv2.Resize(src, dst2, newSize, 0, 0, options.warpFlag)
        labels(2) = "Resize % = " + (saveSliderValue / 100).ToString("0%")
    End Sub
End Class








Public Class Interpolate_Kalman : Inherits TaskParent
    Dim inter As New Interpolate_Basics
    Dim updatedFrames As Integer
    Dim myFrameCount As Integer
    Dim heartCount As Integer
    Dim options As New Options_Kalman
    Dim kalman As New Kalman_Basics
    Public Sub New()
        desc = "Use Kalman to smooth the grayscale results of interpolation"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        inter.Run(src)

        cv.Cv2.CvtColor(inter.dst2, dst2, cv.ColorConversionCodes.BGR2GRAY)
        If task.optionsChanged Then
            ReDim kalman.kInput(dst2.Width * dst2.Height - 1)
            myFrameCount = 1
            updatedFrames = 0
        End If

        Dim tmp32f As New cv.Mat
        dst2.ConvertTo(tmp32f, cv.MatType.CV_32F)
        tmp32f.GetArray(Of Single)(kalman.kInput)
        kalman.Run(emptyMat)

        Dim results(kalman.kInput.Length - 1) As Byte
        For i = 0 To kalman.kOutput.Length - 1
            Dim val = kalman.kOutput(i)
            If Single.IsNaN(val) Then val = 255
            If val < 0 Then val = 0
            If val > 255 Then val = 255
            results(i) = val
        Next
        Marshal.Copy(results, 0, dst2.Data, kalman.kOutput.Length)

        If options.useKalman Then
            labels(2) = "Kalman-smoothed output after resizing to " + CStr(dst2.Width) + "x" + CStr(dst2.Height)
        Else
            labels(2) = "Raw output after resizing to " + CStr(dst2.Width) + "x" + CStr(dst2.Height)
        End If

        Static lastframe As cv.Mat = dst2.Clone
        If lastframe.Size <> dst2.Size Then lastframe = dst2.Clone
        Dim tmp As cv.Mat = dst2 - lastframe
        Dim diffCount = cv.Cv2.CountNonZero(tmp)
        If diffCount > inter.iOptions.pixelCountThreshold Then
            lastframe = dst2.Clone
            dst3 = src.Clone
            updatedFrames += 1
        End If

        labels(3) = "Total frames = " + CStr(myFrameCount) + " updates=" + CStr(updatedFrames) +
                        " savings = " + CStr(myFrameCount - updatedFrames) + " or " +
                        ((myFrameCount - updatedFrames) / myFrameCount).ToString("0%") + " diffCount = " + CStr(diffCount)

        If task.heartBeat Then
            heartCount += 1
            If heartCount Mod 10 = 0 Then
                myFrameCount = 0
                updatedFrames = 0
            End If
        End If
        myFrameCount += 1
    End Sub
End Class






Public Class XR_Interpolate_Lines : Inherits TaskParent
    Dim inter As New Interpolate_Basics
    Public Sub New()
        OptionParent.FindSlider("Interpolation Resize %").Value = 80
        OptionParent.FindSlider("Interpolation threshold").Value = 100
        desc = "Detect lines in interpolation sharedResults.images.."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        inter.Run(src)
        Dim _cvtResize As New cv.Mat
        cv.Cv2.CvtColor(inter.dst2, _cvtResize, cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.Resize(_cvtResize, dst1, dst3.Size)
        cv.Cv2.Threshold(dst1, dst1, inter.iOptions.interpolationThreshold, 255, cv.ThresholdTypes.Binary)

        dst2 = task.lines.dst2
        dst3 = src

        For Each lp In task.lines.lpList
            cv.Cv2.Line(dst3, lp.p1, lp.p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
        labels(3) = "There were " + CStr(task.lines.lpList.Count) + " lines found"
        labels(2) = inter.labels(2)
    End Sub
End Class






Public Class XR_Interpolate_Difference : Inherits TaskParent
    Dim inter As New Interpolate_Kalman
    Dim diff As New Diff_Basics
    Public Sub New()
        desc = "Highlight the difference between the interpolation results and the current image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        inter.Run(src)
        cv.Cv2.CvtColor(inter.dst3, dst2, cv.ColorConversionCodes.BGR2GRAY)
        labels(2) = inter.labels(3)

        diff.lastFrame = task.gray
        diff.Run(dst2)
        dst3 = diff.dst2
    End Sub
End Class








Public Class XR_Interpolate_QuarterBeat : Inherits TaskParent
    Dim diff As New Diff_Basics
    Dim updatedFrames As Integer
    Dim myFrameCount As Integer
    Dim cameraFPS As Single
    Dim processedFPS As Single
    Dim nextTime = Now
    Public Sub New()
        desc = "Highlight the image differences after every quarter second."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.quarterBeat Then
            diff.Run(src)
            dst3 = diff.dst2
If cv.Cv2.CountNonZero(diff.dst2) > 0 Then
                diff.lastFrame = task.gray
                dst2 = src
                updatedFrames += 1
            End If
        End If

        If task.heartBeat Then
            Static heartCount As Integer
            heartCount += 1
            If heartCount Mod 3 = 0 Then
                Dim newTime = Now
                Dim elapsedTicks = newTime.Ticks - nextTime.Ticks
                Dim span = New TimeSpan(elapsedTicks)
                cameraFPS = myFrameCount / (span.Ticks / TimeSpan.TicksPerSecond)
                processedFPS = updatedFrames / (span.Ticks / TimeSpan.TicksPerSecond)
                nextTime = newTime
                myFrameCount = 0
                updatedFrames = 0
            End If
        End If
        myFrameCount += 1

        labels(2) = "Total frames = " + CStr(myFrameCount) + " updates=" + CStr(updatedFrames) +
                        " savings = " + CStr(myFrameCount - updatedFrames) + " or " +
                        ((myFrameCount - updatedFrames) / myFrameCount).ToString("0%") + " cameraFPS = " + cameraFPS.ToString("00.0") +
                        " processedFPS = " + processedFPS.ToString("00.0")
    End Sub
End Class
