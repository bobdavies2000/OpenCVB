﻿Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Interpolate_Basics : Inherits TaskParent
    Public options As New Options_Resize
    Public iOptions As New Options_Interpolate
    Dim direction = 1
    Public Sub New()
        desc = "Resize image using all available interpolation methods in OpenCV"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
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
        dst2 = src.Resize(newSize, 0, 0, options.warpFlag)
        labels(2) = "Resize % = " + Format(saveSliderValue / 100, "0%")
    End Sub
End Class








Public Class Interpolate_Kalman : Inherits TaskParent
    Dim inter As New Interpolate_Basics
    Dim updatedFrames As Integer
    Dim myFrameCount As Integer
    Dim heartCount As Integer
    Public Sub New()
        task.kalman = New Kalman_Basics
        desc = "Use Kalman to smooth the grayscale results of interpolation"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        inter.Run(src)

        dst2 = inter.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.optionsChanged Then
            ReDim task.kalman.kInput(dst2.Width * dst2.Height - 1)
            myFrameCount = 1
            updatedFrames = 0
        End If

        Dim tmp32f As New cv.Mat
        dst2.ConvertTo(tmp32f, cv.MatType.CV_32F)
        Marshal.Copy(tmp32f.Data, task.kalman.kInput, 0, task.kalman.kInput.Length)
        task.kalman.Run(emptyMat)

        Dim results(task.kalman.kInput.Length - 1) As Byte
        For i = 0 To task.kalman.kOutput.Length - 1
            Dim val = task.kalman.kOutput(i)
            If Single.IsNaN(val) Then val = 255
            If val < 0 Then val = 0
            If val > 255 Then val = 255
            results(i) = val
        Next
        Marshal.Copy(results, 0, dst2.Data, task.kalman.kOutput.Length)

        If task.gOptions.UseKalman.Checked Then
            labels(2) = "Kalman-smoothed output after resizing to " + CStr(dst2.Width) + "x" + CStr(dst2.Height)
        Else
            labels(2) = "Raw output after resizing to " + CStr(dst2.Width) + "x" + CStr(dst2.Height)
        End If

        Static lastframe As cv.Mat = dst2.Clone
        If lastframe.Size <> dst2.Size Then lastframe = dst2.Clone
        Dim tmp As cv.Mat = dst2 - lastframe
        Dim diffCount = tmp.CountNonZero
        If diffCount > inter.iOptions.pixelCountThreshold Then
            lastframe = dst2.Clone
            dst3 = src.Clone
            updatedFrames += 1
        End If

        labels(3) = "Total frames = " + CStr(myFrameCount) + " updates=" + CStr(updatedFrames) +
                    " savings = " + CStr(myFrameCount - updatedFrames) + " or " +
                    Format((myFrameCount - updatedFrames) / myFrameCount, "0%") + " diffCount = " + CStr(diffCount)

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






Public Class Interpolate_Lines : Inherits TaskParent
    Dim inter As New Interpolate_Basics
    Public Sub New()
        OptionParent.FindSlider("Interpolation Resize %").Value = 80
        OptionParent.FindSlider("Interpolation threshold").Value = 100
        desc = "Detect lines in interpolation results."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        inter.Run(src)
        dst1 = inter.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Resize(dst3.Size)
        dst1 = dst1.Threshold(inter.iOptions.interpolationThreshold, 255, cv.ThresholdTypes.Binary)

        dst2 = task.lines.dst2
        dst3 = src

        For Each lp In task.lines.lpList
            DrawLine(dst3, lp.p1, lp.p2, cv.Scalar.Yellow)
        Next
        labels(3) = "There were " + CStr(task.lines.lpList.Count) + " lines found"
        labels(2) = inter.labels(2)
    End Sub
End Class






Public Class Interpolate_Difference : Inherits TaskParent
    Dim inter As New Interpolate_Kalman
    Dim diff As New Diff_Basics
    Public Sub New()
        desc = "Highlight the difference between the interpolation results and the current image."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        inter.Run(src)
        dst2 = inter.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        labels(2) = inter.labels(3)

        diff.lastFrame = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        diff.Run(dst2)
        dst3 = diff.dst2
    End Sub
End Class








Public Class Interpolate_QuarterBeat : Inherits TaskParent
    Dim diff As New Diff_Basics
    Dim updatedFrames As Integer
    Dim myFrameCount As Integer
    Dim cameraFPS As Single
    Dim processedFPS As Single
    Dim nextTime = Now
    Public Sub New()
        desc = "Highlight the image differences after every quarter second."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.quarterBeat Then
            diff.Run(src)
            dst3 = diff.dst2
            If diff.dst2.CountNonZero > 0 Then
                diff.lastFrame = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
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
                    Format((myFrameCount - updatedFrames) / myFrameCount, "0%") + " cameraFPS = " + Format(cameraFPS, "00.0") +
                    " processedFPS = " + Format(processedFPS, "00.0")
    End Sub
End Class