Imports cv = OpenCvSharp
Public Class Interpolate_Basics : Inherits VBparent
    Dim flags As New Resize_Options
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Resize % (Grab to control)", 1, 100, 50)
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
        Dim rPercent = percentSlider.value / 100

        dst1 = src.Resize(New cv.Size(CInt(dst1.Width * rPercent), CInt(dst1.Height * rPercent)), 0, 0, flags.warpFlag)
        label1 = "Resize % = " + Format(rPercent, "0%")
    End Sub
End Class