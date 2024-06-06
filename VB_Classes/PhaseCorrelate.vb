Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/phase_corr.cpp
' https://docs.opencv.org/master/d7/df3/group__imgproc__motion.html
Public Class PhaseCorrelate_Basics : Inherits VB_Parent
    Dim hanning As New cv.Mat
    Public stableRect As cv.Rect
    Public srcRect As cv.Rect
    Public center As cv.Point
    Public radius As Single
    Public shift As cv.Point2d
    Public lastFrame As cv.Mat
    Public response As Double
    Public resetLastFrame As Boolean
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold shift to cause reset of lastFrame", 0, 100, 30)
        cv.Cv2.CreateHanningWindow(hanning, dst2.Size, cv.MatType.CV_64F)
        desc = "Look for a shift between the current frame and the previous"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static thresholdSlider = FindSlider("Threshold shift to cause reset of lastFrame")

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim input64 As New cv.Mat
        input.ConvertTo(input64, cv.MatType.CV_64F)
        If lastFrame Is Nothing Then lastFrame = input64.Clone

        shift = cv.Cv2.PhaseCorrelate(lastFrame, input64, hanning, response)

        If Double.IsNaN(response) Then
            setTrueText("PhaseCorrelate_Basics has detected NaN's in the input image.", 3)
        Else
            radius = Math.Sqrt(shift.X * shift.X + shift.Y * shift.Y)
            resetLastFrame = False
            If thresholdSlider.Value < radius Then resetLastFrame = True

            Dim x1 = If(shift.X < 0, Math.Abs(shift.X), 0)
            Dim y1 = If(shift.Y < 0, Math.Abs(shift.Y), 0)

            stableRect = New cv.Rect(x1, y1, src.Width - Math.Abs(shift.X), src.Height - Math.Abs(shift.Y))
            stableRect = validateRect(stableRect)

            If stableRect.Width > 0 And stableRect.Height > 0 Then
                Dim x2 = If(shift.X < 0, 0, shift.X)
                Dim y2 = If(shift.Y < 0, 0, shift.Y)
                srcRect = New cv.Rect(x2, y2, stableRect.Width, stableRect.Height)

                center = New cv.Point(input64.Cols / 2, input64.Rows / 2)
                If src.Channels = 1 Then src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                dst2 = src.Clone
                dst2.Circle(center, radius, cv.Scalar.Yellow, task.lineWidth + 2, task.lineType)
                dst2.Line(center, New cv.Point(center.X + shift.X, center.Y + shift.Y), cv.Scalar.Red, task.lineWidth + 1, task.lineType)

                src(srcRect).CopyTo(dst3(stableRect))

                If radius > 5 Then
                    dst3.Circle(center, radius, cv.Scalar.Yellow, task.lineWidth + 2, task.lineType)
                    dst3.Line(center, New cv.Point(center.X + shift.X, center.Y + shift.Y), cv.Scalar.Red, task.lineWidth + 1, task.lineType)
                End If
            Else
                resetLastFrame = True
            End If
        End If

        labels(3) = If(resetLastFrame, "lastFrame Reset", "Restored lastFrame")
        If resetLastFrame Then lastFrame = input64

        labels(2) = "Shift = (" + Format(shift.X, fmt2) + "," + Format(shift.Y, fmt2) + ") with radius = " + Format(radius, fmt2)
    End Sub
End Class








Public Class PhaseCorrelate_BasicsTest : Inherits VB_Parent
    Dim random As New Stabilizer_BasicsRandomInput
    Dim stable As New PhaseCorrelate_Basics
    Public Sub New()
        labels(2) = "Unstable input to PhaseCorrelate_Basics"
        labels(3) = "Stabilized output from Phase_Correlate_Basics"
        desc = "Test the PhaseCorrelate_Basics with random movement"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        random.Run(src)

        stable.Run(random.dst3.Clone)

        dst2 = stable.dst2
        dst3 = stable.dst3
        labels(3) = stable.labels(3)
    End Sub
End Class









Public Class PhaseCorrelate_Depth : Inherits VB_Parent
    Dim phaseC As New PhaseCorrelate_Basics
    Public Sub New()
        desc = "Use phase correlation on the depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static lastFrame = task.pcSplit(2).Clone
        phaseC.Run(task.pcSplit(2))
        dst2 = task.pcSplit(2)
        Dim tmp = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        If phaseC.resetLastFrame Then task.pcSplit(2).CopyTo(lastFrame)
        If Double.IsNaN(phaseC.response) Then
            setTrueText("PhaseCorrelate_Basics has detected NaN's in the input image.", 3)
        End If
        If phaseC.srcRect.Width = phaseC.stableRect.Width And phaseC.srcRect.Width <> 0 Then
            lastFrame(phaseC.srcRect).CopyTo(tmp(phaseC.stableRect))
            labels(2) = phaseC.labels(2)
            labels(3) = phaseC.labels(3)

            tmp = tmp.Normalize(0, 255, cv.NormTypes.MinMax)
            tmp.ConvertTo(dst3, cv.MatType.CV_8UC1)
            dst3 = dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            dst3.Circle(phaseC.center, phaseC.radius, cv.Scalar.Yellow, task.lineWidth + 2, task.lineType)
            dst3.Line(phaseC.center, New cv.Point(phaseC.center.X + phaseC.shift.X, phaseC.center.Y + phaseC.shift.Y), cv.Scalar.Red, task.lineWidth + 1, task.lineType)
        End If
    End Sub
End Class







' https://docs.opencv.org/master/d7/df3/group__imgproc__motion.html
Public Class PhaseCorrelate_HanningWindow : Inherits VB_Parent
    Public Sub New()
        labels(2) = "Looking down on a bell curve in 2 dimensions"
        desc = "Show what a Hanning window looks like"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        cv.Cv2.CreateHanningWindow(dst2, src.Size, cv.MatType.CV_32F)
    End Sub
End Class
