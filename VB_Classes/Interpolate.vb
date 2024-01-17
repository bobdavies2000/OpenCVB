Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Interpolate_Basics : Inherits VB_Algorithm
    Public options As New Options_Resize
    Public iOptions As New Options_Interpolate
    Public Sub New()
        desc = "Resize image using all available interpolation methods in OpenCV"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        iOptions.RunVB()
        Static saveSliderValue As Integer = iOptions.threshold

        If standalone Then
            Dim userGrab As Boolean = iOptions.threshold <> iOptions.saveDefaultThreshold
            If userGrab = False Then
                Static direction = 1
                saveSliderValue += direction
                If saveSliderValue > 50 Then direction = -1
                If saveSliderValue = 1 Then direction = 1
            Else
                userGrab = True
                saveSliderValue = iOptions.threshold
            End If
        End If

        dst2 = src.Clone
        dst2 = src.Resize(New cv.Size(CInt(dst2.Width * saveSliderValue / 100),
                                      CInt(dst2.Height * saveSliderValue / 100)), 0, 0, options.warpFlag)
        labels(2) = "Resize % = " + Format(saveSliderValue / 100, "0%")
    End Sub
End Class








Public Class Interpolate_Kalman : Inherits VB_Algorithm
    Dim inter As New Interpolate_Basics
    Dim kalman As New Kalman_Basics
    Public Sub New()
        desc = "Use Kalman to smooth the grayscale results of interpolation"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static updatedFrames As Integer
        Static radioSelection As Integer

        inter.Run(src)
        Dim iThreshold = inter.iOptions.interpolationThreshold

        dst2 = inter.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.optionsChanged Then
            ReDim kalman.kInput(dst2.Width * dst2.Height - 1)
            task.frameCount = 1
            updatedFrames = 0
            radioSelection = inter.options.radioIndex
        End If

        Dim tmp32f As New cv.Mat
        dst2.ConvertTo(tmp32f, cv.MatType.CV_32F)
        Marshal.Copy(dst2.Data, kalman.kInput, 0, kalman.kInput.Length)
        kalman.Run(empty)

        Dim i As Integer
        For y = 0 To dst2.Height - 1
            For x = 0 To dst2.Width - 1
                Dim val = kalman.kOutput(i)
                If Single.IsNaN(val) Then val = 0
                If val < 0 Then val = 0
                If val > 255 Then val = 255
                dst2.Set(Of Byte)(y, x, val)
                i += 1
            Next
        Next

        If gOptions.UseKalman.Checked Then
            labels(2) = "Kalman-smoothed output after resizing to " + CStr(dst2.Width) + "x" + CStr(dst2.Height)
        Else
            labels(2) = "Raw output after resizing to " + CStr(dst2.Width) + "x" + CStr(dst2.Height)
        End If

        Static lastframe As cv.Mat = dst2.Clone
        If lastframe.size <> dst2.Size Then lastframe = dst2.Clone
        Dim tmp As cv.Mat = dst2 - lastframe
        If tmp.CountNonZero > 0 Then
            lastframe = dst2.Clone
            dst3 = src.Clone
            updatedFrames += 1
        End If
        labels(3) = "Total frames = " + CStr(task.frameCount) + " updates=" + CStr(updatedFrames) +
                    " savings = " + CStr(task.frameCount - updatedFrames) + " or " +
                    Format((task.frameCount - updatedFrames) / task.frameCount, "0%")
    End Sub
End Class





Public Class Interpolate_KalmanOld : Inherits VB_Algorithm
    Dim inter As New Interpolate_Basics
    Dim kalman As New Kalman_Basics
    Public Sub New()
        findRadio("Area").Checked = True
        desc = "Use Kalman to smooth the grayscale results of interpolation"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static updatedFrames As Integer

        inter.Run(src)
        dst2 = inter.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.optionsChanged Then
            ReDim kalman.kInput(dst2.Width * dst2.Height - 1)
            task.frameCount = 1
            updatedFrames = 0
        End If

        Dim i As Integer
        For y = 0 To dst2.Height - 1
            For x = 0 To dst2.Width - 1
                kalman.kInput(i) = dst2.Get(Of Byte)(y, x)
                i += 1
            Next
        Next

        kalman.Run(empty)

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

        If gOptions.UseKalman.Checked Then
            labels(2) = "Kalman-smoothed output after resizing to " + CStr(dst2.Width) + "x" + CStr(dst2.Height)
        Else
            labels(2) = "Raw output after resizing to " + CStr(dst2.Width) + "x" + CStr(dst2.Height)
        End If

        Static lastframe As cv.Mat = dst2.Clone
        If lastframe.size <> dst2.Size Then lastframe = dst2.Clone
        Dim tmp As cv.Mat = dst2 - lastframe
        If tmp.CountNonZero > 0 Then
            lastframe = dst2.Clone
            dst3 = src.Clone
            updatedFrames += 1
        End If
        labels(3) = "Total frames = " + CStr(task.frameCount) + " updates=" + CStr(updatedFrames) +
                    " savings = " + CStr(task.frameCount - updatedFrames) + " or " +
                    Format((task.frameCount - updatedFrames) / task.frameCount, "0%")
    End Sub
End Class






Public Class Interpolate_Lines : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Dim inter As New Interpolate_Basics
    Public Sub New()
        findRadio("Area").Checked = True
        findSlider("Resize % (Grab to control)").Value = 4
        desc = "Detect lines in interpolation results."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static thresholdSlider = findSlider("Interpolation threshold")
        inter.Run(src)
        lines.Run(inter.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Resize(dst3.Size).Threshold(thresholdSlider.Value, 255, cv.ThresholdTypes.Binary))
        dst2 = lines.dst2
        dst3 = src
        For Each mps In lines.mpList
            dst3.Line(mps.p1, mps.p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
        labels(2) = inter.labels(2)
        labels(3) = "There were " + CStr(lines.mpList.Count) + " lines found"
    End Sub
End Class