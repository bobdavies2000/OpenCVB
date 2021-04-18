Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/phase_corr.cpp
' https://docs.opencv.org/master/d7/df3/group__imgproc__motion.html
Public Class PhaseCorrelate_Basics : Inherits VBparent
    Dim hanning As New cv.Mat
    Public stableRect As cv.Rect
    Public srcRect As cv.Rect
    Public center As cv.Point
    Public radius As Single
    Public shift As cv.Point2d
    Public lastFrame As cv.Mat
    Public resetLastFrame As Boolean
    Public Sub New()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Threshold shift to cause reset of lastFrame", 0, 100, 30)
        End If

        cv.Cv2.CreateHanningWindow(hanning, dst1.Size, cv.MatType.CV_64F)
        task.desc = "Look for a shift between the current frame and the previous"
    End Sub
    Public Sub Run(src as cv.Mat)

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim input64 As New cv.Mat
        input.ConvertTo(input64, cv.MatType.CV_64F)
        If lastFrame Is Nothing Then lastFrame = input64.Clone

        Dim response As Double
        shift = cv.Cv2.PhaseCorrelate(lastFrame, input64, hanning, response)

        radius = Math.Sqrt(shift.X * shift.X + shift.Y * shift.Y)
        Static thresholdSlider = findSlider("Threshold shift to cause reset of lastFrame")
        resetLastFrame = False
        If thresholdSlider.value < radius Then resetLastFrame = True

        Dim x1 = If(shift.X < 0, Math.Abs(shift.X), 0)
        Dim y1 = If(shift.Y < 0, Math.Abs(shift.Y), 0)

        stableRect = New cv.Rect(x1, y1, src.Width - Math.Abs(shift.X), src.Height - Math.Abs(shift.Y))

        If stableRect.Width > 0 And stableRect.Height > 0 Then
            Dim x2 = If(shift.X < 0, 0, shift.X)
            Dim y2 = If(shift.Y < 0, 0, shift.Y)
            srcRect = New cv.Rect(x2, y2, stableRect.Width, stableRect.Height)

            center = New cv.Point(input64.Cols / 2, input64.Rows / 2)
            If src.Channels = 1 Then src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst1 = src.Clone
            dst1.Circle(center, radius, cv.Scalar.Yellow, 3, task.lineType)
            dst1.Line(center, New cv.Point(center.X + shift.X, center.Y + shift.Y), cv.Scalar.Red, 2, task.lineType)

            src(srcRect).CopyTo(dst2(stableRect))

            If radius > 5 Then
                dst2.Circle(center, radius, cv.Scalar.Yellow, 3, task.lineType)
                dst2.Line(center, New cv.Point(center.X + shift.X, center.Y + shift.Y), cv.Scalar.Red, 2, task.lineType)
            End If
        Else
            resetLastFrame = True
        End If

        label2 = If(resetLastFrame, "lastFrame Reset", "Restored lastFrame")
        If resetLastFrame Then lastFrame = input64

        label1 = "Shift = (" + Format(shift.X, "0.00") + "," + Format(shift.Y, "0.00") + ") with radius = " + Format(radius, "#0.00")
    End Sub
End Class








Public Class PhaseCorrelate_BasicsTest : Inherits VBparent
    Dim random As Stabilizer_BasicsRandomInput
    Dim stable As PhaseCorrelate_Basics
    Public Sub New()
        stable = New PhaseCorrelate_Basics
        random = New Stabilizer_BasicsRandomInput

        label1 = "Unstable input to PhaseCorrelate_Basics"
        label2 = "Stabilized output from Phase_Correlate_Basics"
        task.desc = "Test the PhaseCorrelate_Basics with random movement"
    End Sub
    Public Sub Run(src as cv.Mat)

        random.Run(src)

        stable.Run(random.dst2.Clone)

        dst1 = stable.dst1
        dst2 = stable.dst2
        label2 = stable.label2
    End Sub
End Class









Public Class PhaseCorrelate_Depth : Inherits VBparent
    Dim phaseC As PhaseCorrelate_Basics
    Public Sub New()
        phaseC = New PhaseCorrelate_Basics
        task.desc = "Use phase correlation on the depth data"
    End Sub
    Public Sub Run(src as cv.Mat)

        Static lastFrame = task.depth32f.Clone
        phaseC.Run(task.depth32f)
        dst1 = task.depth32f
        Dim tmp = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        If phaseC.resetLastFrame Then task.depth32f.CopyTo(lastFrame)
        lastFrame(phaseC.srcRect).CopyTo(tmp(phaseC.stableRect))
        label1 = phaseC.label1
        label2 = phaseC.label2

        tmp = tmp.Normalize(0, 255, cv.NormTypes.MinMax)
        tmp.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        dst2.Circle(phaseC.center, phaseC.radius, cv.Scalar.Yellow, 3, task.lineType)
        dst2.Line(phaseC.center, New cv.Point(phaseC.center.X + phaseC.shift.X, phaseC.center.Y + phaseC.shift.Y), cv.Scalar.Red, 2, task.lineType)
    End Sub
End Class







' https://docs.opencv.org/master/d7/df3/group__imgproc__motion.html
Public Class PhaseCorrelate_HanningWindow : Inherits VBparent
    Public Sub New()
        label1 = "Looking down on a bell curve in 2 dimensions"
        task.desc = "Show what a Hanning window looks like"
    End Sub
    Public Sub Run(src as cv.Mat)
        cv.Cv2.CreateHanningWindow(dst1, src.Size, cv.MatType.CV_32F)
    End Sub
End Class
