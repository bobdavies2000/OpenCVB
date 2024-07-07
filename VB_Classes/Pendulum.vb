Imports cv = OpenCvSharp
'https://github.com/DinoZ1729/Double-Pendulum/blob/main/pendulum_with_trace.cpp
Public Class Pendulum_Basics : Inherits VB_Parent
    Dim l1 As Single = 150, l2 As Single = 150, m1 As Single = 10, m2 As Single = 10
    Dim o1 = 2 * cv.Cv2.PI / 2, o2 = 2 * cv.Cv2.PI / 3
    Dim w1 As Single, w2 As Single
    Dim g = 9.81
    Dim dw As Single = 2, dh As Single = 4
    Dim center = New cv.Point2f(dst2.Width / 2, 0)
    Dim fps As Single = 300
    Public Sub New()
        If FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Reset initial conditions")
            check.Box(0).Checked = True
        End If

        If sliders.Setup(traceName) Then sliders.setupTrackBar("Pendulum FPS", 10, 1000, 300)
        labels = {"", "", "A double pendulum representation", "Trace of the pendulum end points (p1 and p2)"}
        desc = "Build a double pendulum"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static initCheck = FindCheckBox("Reset initial conditions")
        Static timeSlider = FindSlider("Pendulum FPS")

        Dim accumulator As Single

        If task.frameCount Mod 1000 = 0 Or task.optionsChanged Then
            dst2.SetTo(0)
            dst3.SetTo(0)
        End If

        If initCheck.checked Then
            l1 = msRNG.Next(50, 300)
            l2 = msRNG.Next(50, 300)
            dw = msRNG.Next(2, 4)
            dh = 2 * dw
            initCheck.checked = False
        End If

        fps = timeSlider.Value
        Dim dt = 1 / fps

        Dim alfa1 = (-g * (2 * m1 + m2) * Math.Sin(o1) - g * m2 * Math.Sin(o1 - 2 * o2) - 2 * m2 * Math.Sin(o1 - o2) * (w2 * w2 * l2 + w1 * w1 * l1 * Math.Cos(o1 - o2))) / (l1 * (2 * m1 + m2 - m2 * Math.Cos(2 * o1 - 2 * o2)))
        Dim alfa2 = (2 * Math.Sin(o1 - o2)) * (w1 * w1 * l1 * (m1 + m2) + g * (m1 + m2) * Math.Cos(o1) + w2 * w2 * l2 * m2 * Math.Cos(o1 - o2)) / l2 / (2 * m1 + m2 - m2 * Math.Cos(2 * o1 - 2 * o2))

        w1 += 10 * dt * alfa1
        w2 += 10 * dt * alfa2
        o1 += 10 * dt * w1
        o2 += 10 * dt * w2

        accumulator += dt

        Dim p1 = New cv.Point2f((dst2.Width / 2 + Math.Sin(o1) * l1 + dw * 0.5) / dw, dst2.Height - (Math.Cos(o1) * l1 + dh * 0.5) / dh + dst2.Height / dh / 2)
        ' adjust to fit in the image better
        p1 = New cv.Point2f(p1.X * 2, p1.Y * 0.5)
        Dim p2 = New cv.Point2f(p1.X + (Math.Sin(o2) * l2 + dw * 0.5) / dw, p1.Y - (Math.Cos(o2) * l2 + dh * 0.5) / dh)

        DrawLine(dst2, center, p1, task.HighlightColor)
        DrawLine(dst2, p1, p2, task.HighlightColor)

        DrawCircle(dst3,p1, task.DotSize, task.HighlightColor)
        DrawCircle(dst3,p2, task.DotSize, task.HighlightColor)
    End Sub
End Class