Imports cv = OpenCvSharp
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/FASTSample.vb
Public Class FAST_Basics : Inherits VBparent
    Public keypoints() As cv.KeyPoint
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Threshold", 0, 200, 15)
        End If
        If check.Setup(caller, 1) Then
            check.Box(0).Text = "Use Non-Max = True"
            check.Box(0).Checked = True
        End If
        task.desc = "Find interesting points with the FAST (Features from Accelerated Segment Test) algorithm"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        src.CopyTo(dst2)
        keypoints = cv.Cv2.FAST(src, sliders.trackbar(0).Value, If(check.Box(0).Checked, True, False))

        For Each kp As cv.KeyPoint In keypoints
            dst2.Circle(kp.Pt, task.dotSize, cv.Scalar.Red, -1, task.lineType, 0)
        Next kp
        labels(2) = "FAST_Basics nonMax = " + If(check.Box(0).Checked, "True", "False")
    End Sub
End Class





Public Class FAST_Centroid : Inherits VBparent
    Dim fast As New FAST_Basics
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(1) ' 2 elements - cv.point
        task.desc = "Find interesting points with the FAST and smooth the centroid with kalman"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        fast.Run(src)
        dst2 = fast.dst2
        dst3.SetTo(0)
        For Each kp As cv.KeyPoint In fast.keypoints
            dst3.Circle(kp.Pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType, 0)
        Next kp
        Dim gray = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim m = cv.Cv2.Moments(gray, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.kInput(0) = m.M10 / m.M00
            kalman.kInput(1) = m.M01 / m.M00
            kalman.Run(src)
            dst3.Circle(New cv.Point(kalman.kOutput(0), kalman.kOutput(1)), 10, cv.Scalar.Red, -1, task.lineType)
        End If
    End Sub
End Class




