﻿Imports cv = OpenCvSharp
Public Class Interpolate_Basics : Inherits VBparent
    Public flags As New Resize_Options
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Resize % (Grab to control)", 1, 100, 50)
            sliders.setupTrackBar(1, "Interpolation threshold", 1, 255, 128)
            findRadio("WarpFillOutliers").Enabled = False ' does not work here...
            findRadio("WarpInverseMap").Enabled = False ' does not work here...
        End If

        task.desc = "Resize image using all available interpolation methods in OpenCV"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static percentSlider = findSlider("Resize % (Grab to control)")
        flags.RunClass(Nothing)

        If standalone Then
            Static userGrab As Boolean
            Static lastPercent = percentSlider.value ' let the user grab to slider and control it...
            If Math.Abs(percentSlider.value - lastPercent) <= 1 And userGrab = False Then
                lastPercent = percentSlider.value
                Static direction = 1
                percentSlider.value += direction
                If percentSlider.value >= 50 Then direction = -1
                If percentSlider.value = 1 Then direction = 1
            Else
                userGrab = True
            End If
        End If

        dst2 = src.Clone
        Dim rPercent = percentSlider.value / 100
        dst2 = src.Resize(New cv.Size(CInt(dst2.Width * rPercent), CInt(dst2.Height * rPercent)), 0, 0, flags.warpFlag)
        labels(2) = "Resize % = " + Format(rPercent, "0%")
    End Sub
End Class








Public Class Interpolate_Kalman : Inherits VBparent
    Dim inter As New Interpolate_Basics
    Dim kalman As New Kalman_Basics
    Public Sub New()
        findRadio("Area").Checked = True
        findSlider("Resize % (Grab to control)").Value = 3
        findSlider("Interpolation threshold").Value = 4
        task.desc = "Use Kalman to smooth the grayscale results of interpolation"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("Interpolation threshold")
        Static updatedFrames As Integer
        Static radioSelection As Integer

        inter.RunClass(src)
        dst2 = inter.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If dst2.Width * dst2.Height <> kalman.kInput.Length Or radioSelection <> inter.flags.radioIndex Then
            ReDim kalman.kInput(dst2.Width * dst2.Height - 1)
            task.frameCount = 1
            updatedFrames = 0
            radioSelection = inter.flags.radioIndex
        End If

        Dim i As Integer
        For y = 0 To dst2.Height - 1
            For x = 0 To dst2.Width - 1
                kalman.kInput(i) = dst2.Get(Of Byte)(y, x)
                i += 1
            Next
        Next

        kalman.RunClass(Nothing)

        i = 0
        For y = 0 To dst2.Height - 1
            For x = 0 To dst2.Width - 1
                Dim val = kalman.kOutput(i)
                If val < 0 Then val = 0
                If val > 255 Then val = 255
                dst2.Set(Of Byte)(y, x, val)
                i += 1
            Next
        Next

        If task.useKalman Then
            labels(2) = "Kalman-smoothed output after resizing to " + CStr(dst2.Width) + "x" + CStr(dst2.Height)
        Else
            labels(2) = "Raw output after resizing to " + CStr(dst2.Width) + "x" + CStr(dst2.Height)
        End If

        Static lastframe = dst2.Clone
        If lastframe.size <> dst2.Size Then lastframe = dst2.Clone
        Dim tmp = (dst2 - lastframe).tomat.threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
        If tmp.CountNonZero > 0 Then
            Console.WriteLine("Count non zero = " + CStr(tmp.countnonzero))
            lastframe = dst2.Clone
            dst3 = src.Clone
            updatedFrames += 1
        End If
        labels(3) = "Total frames = " + CStr(task.frameCount) + " updates=" + CStr(updatedFrames) + " savings = " + CStr(task.frameCount - updatedFrames)
    End Sub
End Class






Public Class Interpolate_Lines : Inherits VBparent
    Dim lines As New Line_Basics
    Dim inter As New Interpolate_Basics
    Public Sub New()
        findRadio("Area").Checked = True
        findSlider("Resize % (Grab to control)").Value = 4
        task.desc = "Detect lines in interpolation results."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("Interpolation threshold")
        inter.RunClass(src)
        lines.RunClass(inter.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Resize(dst3.Size).Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary))
        dst2 = lines.dst2
        dst3 = src
        For i = 0 To lines.pt1List.Count - 1
            dst3.Line(lines.pt1List(i), lines.pt2List(i), cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
        labels(2) = inter.labels(2)
        labels(3) = "There were " + CStr(lines.pt1List.Count) + " lines found"
    End Sub
End Class