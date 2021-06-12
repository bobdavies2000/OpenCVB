Imports cv = OpenCvSharp
Public Class Interpolate_Basics : Inherits VBparent
    Dim flags As New Resize_Options
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
        flags.Run(Nothing)

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

        dst1 = src.Clone
        Dim rPercent = percentSlider.value / 100
        dst1 = src.Resize(New cv.Size(CInt(dst1.Width * rPercent), CInt(dst1.Height * rPercent)), 0, 0, flags.warpFlag)
        label1 = "Resize % = " + Format(rPercent, "0%")
    End Sub
End Class








Public Class Interpolate_Kalman : Inherits VBparent
    Dim inter As New Interpolate_Basics
    Dim kalman As New Kalman_Basics
    Public Sub New()
        findRadio("Nearest (preserves pixel values best)").Checked = True
        findSlider("Resize % (Grab to control)").Value = 1
        findSlider("Interpolation threshold").Value = 1
        task.desc = "Use Kalman to smooth the grayscale results of interpolation"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("Interpolation threshold")

        inter.Run(src)
        dst1 = inter.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If dst1.Width * dst1.Height <> kalman.kInput.Length Then
            ReDim kalman.kInput(dst1.Width * dst1.Height)
        End If

        Dim i As Integer
        For y = 0 To dst1.Height - 1
            For x = 0 To dst1.Width - 1
                kalman.kInput(i) = dst1.Get(Of Byte)(y, x)
                i += 1
            Next
        Next

        kalman.Run(Nothing)

        i = 0
        For y = 0 To dst1.Height - 1
            For x = 0 To dst1.Width - 1
                Dim val = kalman.kOutput(i)
                If val < 0 Then val = 0
                If val > 255 Then val = 255
                dst1.Set(Of Byte)(y, x, val)
                i += 1
            Next
        Next

        If task.useKalman Then
            label1 = "Kalman-smoothed output after resizing to " + CStr(dst1.Width) + "x" + CStr(dst1.Height)
        Else
            label1 = "Raw output after resizing to " + CStr(dst1.Width) + "x" + CStr(dst1.Height)
        End If

        Static lastframe = dst1.Clone
        If lastframe.size <> dst1.Size Then lastframe = dst1.Clone
        dst2 = (dst1 - lastframe).tomat.threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
        lastframe = dst1.Clone
    End Sub
End Class






Public Class Interpolate_Lines : Inherits VBparent
    Dim lines As New Line_Basics
    Dim inter As New Interpolate_Basics
    Public Sub New()
        findRadio("Nearest (preserves pixel values best)").Checked = True
        findSlider("Resize % (Grab to control)").Value = 1
        task.desc = "Detect lines in interpolation results."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("Interpolation threshold")
        inter.Run(src)
        lines.Run(inter.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Resize(dst2.Size).Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary))
        dst1 = lines.dst1
        dst2 = src
        For i = 0 To lines.pt1List.Count - 1
            dst2.Line(lines.pt1List(i), lines.pt2List(i), cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
        label1 = inter.label1
        label2 = "There were " + CStr(lines.pt1List.Count) + " lines found"
    End Sub
End Class