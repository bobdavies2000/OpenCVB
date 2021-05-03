Imports cv = OpenCvSharp
'http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_Basics : Inherits VBparent
    Dim kalman() As Kalman_Simple
    Public kInput(4 - 1) As Single
    Public kOutput(4 - 1) As Single
    Public Sub New()
        task.desc = "Use Kalman to stabilize values (such as a cv.rect.)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static saveDimension = -1
        If saveDimension <> kInput.Length Then
            If kalman IsNot Nothing Then
                If kalman.Count > 0 Then
                    For i = 0 To kalman.Count - 1
                        kalman(i).Dispose()
                    Next
                End If
            End If
            saveDimension = kInput.Length
            ReDim kalman(kInput.Length - 1)
            For i = 0 To kInput.Length - 1
                kalman(i) = New Kalman_Simple()
            Next
            ReDim kOutput(kInput.Count - 1)
        End If

        If task.useKalman Then
            For i = 0 To kalman.Length - 1
                kalman(i).inputReal = kInput(i)
                kalman(i).Run(src)
                If Double.IsNaN(kalman(i).stateResult) Then kalman(i).stateResult = kalman(i).inputReal ' kalman failure...
                kOutput(i) = kalman(i).stateResult
            Next
        Else
            kOutput = kInput ' do nothing to the input.
        End If

        If standalone Or task.intermediateReview = caller Then
            dst1 = src
            Dim rect = New cv.Rect(CInt(kOutput(0)), CInt(kOutput(1)), CInt(kOutput(2)), CInt(kOutput(3)))
            rect = validateRect(rect)
            Static lastRect = rect
            If rect = lastRect Then
                Dim r = initRandomRect(src.Width, src.Height, 50)
                kInput = New Single() {r.X, r.Y, r.Width, r.Height}
            End If
            lastRect = rect
            dst1.Rectangle(rect, cv.Scalar.White, 6)
            dst1.Rectangle(rect, cv.Scalar.Red, 1)
        End If
    End Sub
End Class







Public Class Kalman_Stripped : Inherits VBparent
    Dim kalman() As Kalman_Simple
    Public kInput(4 - 1) As Single
    Public kOutput(4 - 1) As Single
    Public Sub New()
        task.desc = "High volume usage only.  Same As New Kalman_Basics but no check boxes."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static saveDimension = -1
        If saveDimension <> kInput.Length Then
            If kalman IsNot Nothing Then
                If kalman.Count > 0 Then
                    For i = 0 To kalman.Count - 1
                        kalman(i).Dispose()
                    Next
                End If
            End If
            saveDimension = kInput.Length
            ReDim kalman(kInput.Length - 1)
            For i = 0 To kInput.Length - 1
                kalman(i) = New Kalman_Simple
            Next
            ReDim kOutput(kInput.Count - 1)
        End If

        If task.useKalman Then
            For i = 0 To kalman.Length - 1
                kalman(i).inputReal = kInput(i)
                kalman(i).Run(src)
                If Double.IsNaN(kalman(i).stateResult) Then kalman(i).stateResult = kalman(i).inputReal ' kalman failure...
                kOutput(i) = kalman(i).stateResult
            Next
        Else
            kOutput = kInput ' do nothing to the input.
        End If

        If standalone Or task.intermediateReview = caller Then
            dst1 = src.Clone()
            Dim rect = New cv.Rect(CInt(kOutput(0)), CInt(kOutput(1)), CInt(kOutput(2)), CInt(kOutput(3)))
            rect = validateRect(rect)
            Static lastRect = rect
            If rect = lastRect Then
                Dim r = initRandomRect(src.Width, src.Height, 50)
                kInput = New Single() {r.X, r.Y, r.Width, r.Height}
            End If
            lastRect = rect
            dst1.Rectangle(rect, cv.Scalar.White, 6)
            dst1.Rectangle(rect, cv.Scalar.Red, 1)
        End If
    End Sub
End Class






' http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_Compare : Inherits VBparent
    Dim kalman() As Kalman_Single
    Public plot As New Plot_OverTime
    Public kPlot As New Plot_OverTime
    Public Sub New()
        plot.plotCount = 3
        plot.topBottomPad = 20

        kPlot.plotCount = 3
        kPlot.topBottomPad = 20

        label1 = "Kalman input: mean values for RGB"
        label2 = "Kalman output: smoothed mean values for RGB"
        task.desc = "Use this kalman filter to predict the next value."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.frameCount = 0 Then
            If kalman IsNot Nothing Then
                If kalman.Count > 0 Then
                    For i = 0 To kalman.Count - 1
                        kalman(i).Dispose()
                    Next
                End If
            End If
            ReDim kalman(3 - 1)
            For i = 0 To kalman.Count - 1
                kalman(i) = New Kalman_Single
            Next
        End If

        ' if either one has triggered a reset for the scale, do them both...
        If kPlot.offChartCount >= kPlot.plotTriggerRescale Or plot.offChartCount >= plot.plotTriggerRescale Then
            kPlot.offChartCount = kPlot.plotTriggerRescale + 1
            plot.offChartCount = plot.plotTriggerRescale + 1
        End If

        plot.plotData = src.Mean()
        plot.Run(Nothing)
        dst1 = plot.dst1

        For i = 0 To kalman.Count - 1
            kalman(i).inputReal = plot.plotData.Item(i)
            kalman(i).Run(src)
        Next

        kPlot.maxScale = plot.maxScale ' keep the scale the same for the side-by-side plots.
        kPlot.minScale = plot.minScale
        kPlot.plotData = New cv.Scalar(kalman(0).stateResult, kalman(1).stateResult, kalman(2).stateResult)
        kplot.Run(Nothing)
        dst2 = kPlot.dst1
    End Sub
End Class



'https://github.com/opencv/opencv/blob/master/samples/cpp/kalman.cpp
Public Class Kalman_RotatingPoint : Inherits VBparent
    Dim kf As New cv.KalmanFilter(2, 1, 0)
    Dim kState As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Dim processNoise As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Dim measurement As New cv.Mat(1, 1, cv.MatType.CV_32F, 0)
    Dim center As cv.Point2f, statePt As cv.Point2f
    Dim radius As Single
    Private Function calcPoint(center As cv.Point2f, R As Double, angle As Double) As cv.Point
        Return center + New cv.Point2f(Math.Cos(angle), -Math.Sin(angle)) * R
    End Function
    Private Sub drawCross(dst1 As cv.Mat, center As cv.Point, color As cv.Scalar)
        Dim d = 3
        cv.Cv2.Line(dst1, New cv.Point(center.X - d, center.Y - d), New cv.Point(center.X + d, center.Y + d), color, 1, task.lineType)
        cv.Cv2.Line(dst1, New cv.Point(center.X + d, center.Y - d), New cv.Point(center.X - d, center.Y + d), color, 1, task.lineType)
    End Sub
    Public Sub New()
        label1 = "Estimate Yellow < Real Red (if working)"

        cv.Cv2.Randn(kState, New cv.Scalar(0), cv.Scalar.All(0.1))
        kf.TransitionMatrix = New cv.Mat(2, 2, cv.MatType.CV_32F, New Single() {1, 1, 0, 1})

        cv.Cv2.SetIdentity(kf.MeasurementMatrix)
        cv.Cv2.SetIdentity(kf.ProcessNoiseCov, cv.Scalar.All(0.00001))
        cv.Cv2.SetIdentity(kf.MeasurementNoiseCov, cv.Scalar.All(0.1))
        cv.Cv2.SetIdentity(kf.ErrorCovPost, cv.Scalar.All(1))
        cv.Cv2.Randn(kf.StatePost, New cv.Scalar(0), cv.Scalar.All(1))
        radius = dst1.Rows / 2.4 ' so we see the entire circle...
        center = New cv.Point2f(dst1.Cols / 2, dst1.Rows / 2)
        task.desc = "Track a rotating point using a Kalman filter. Yellow line (estimate) should be shorter than red (real)."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim stateAngle = kState.Get(Of Single)(0)

        Dim prediction = kf.Predict()
        Dim predictAngle = prediction.Get(Of Single)(0)
        Dim predictPt = calcPoint(center, radius, predictAngle)
        statePt = calcPoint(center, radius, stateAngle)

        cv.Cv2.Randn(measurement, New cv.Scalar(0), cv.Scalar.All(kf.MeasurementNoiseCov.Get(Of Single)(0)))

        measurement += kf.MeasurementMatrix * kState
        Dim measAngle = measurement.Get(Of Single)(0)
        Dim measPt = calcPoint(center, radius, measAngle)

        dst1.SetTo(0)
        drawCross(dst1, statePt, cv.Scalar.White)
        drawCross(dst1, measPt, cv.Scalar.White)
        drawCross(dst1, predictPt, cv.Scalar.White)
        cv.Cv2.Line(dst1, statePt, measPt, New cv.Scalar(0, 0, 255), 3, task.lineType)
        cv.Cv2.Line(dst1, statePt, predictPt, New cv.Scalar(0, 255, 255), 3, task.lineType)

        If msRNG.Next(0, 4) <> 0 Then kf.Correct(measurement)

        cv.Cv2.Randn(processNoise, cv.Scalar.Black, cv.Scalar.All(Math.Sqrt(kf.ProcessNoiseCov.Get(Of Single)(0, 0))))
        kState = kf.TransitionMatrix * kState + processNoise
    End Sub
End Class






' http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
' https://www.codeproject.com/Articles/865935/Object-Tracking-Kalman-Filter-with-Ease
Public Class Kalman_MousePredict : Inherits VBparent
    Dim kalman As New Kalman_Basics
    Dim lineWidth As Integer
    Public Sub New()
        ReDim kalman.kInput(2 - 1)
        ReDim kalman.kOutput(2 - 1)

        lineWidth = dst1.Width / 300
        label1 = "Red is real mouse, white is prediction"
        task.desc = "Use kalman filter to predict the next mouse location."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.frameCount Mod 100 = 0 Then dst1.SetTo(0)

        Static lastRealMouse = task.mousePoint
        kalman.kInput(0) = task.mousePoint.X
        kalman.kInput(1) = task.mousePoint.Y
        Dim lastStateResult = New cv.Point(kalman.kOutput(0), kalman.kOutput(1))
        kalman.Run(src)
        cv.Cv2.Line(dst1, New cv.Point(kalman.kOutput(0), kalman.kOutput(1)), lastStateResult, cv.Scalar.All(255), lineWidth, task.lineType)
        cv.Cv2.Line(dst1, task.mousePoint, lastRealMouse, New cv.Scalar(0, 0, 255), lineWidth, task.lineType)
        lastRealMouse = task.mousePoint
    End Sub
End Class







Public Class Kalman_CVMat : Inherits VBparent
    Dim kalman() As Kalman_Simple
    Public output As cv.Mat
    Dim basics As New Kalman_Basics
    Public input As cv.Mat
    Public Sub New()
        ReDim basics.kInput(4 - 1)
        input = New cv.Mat(4, 1, cv.MatType.CV_32F, 0)
        If standalone Then label1 = "Rectangle moves smoothly to random locations"
        task.desc = "Use Kalman to stabilize a set of values such as a cv.rect or cv.Mat"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static saveDimension = -1
        If saveDimension <> input.Rows Then
            If kalman IsNot Nothing Then
                If kalman.Count > 0 Then
                    For i = 0 To kalman.Count - 1
                        kalman(i).Dispose()
                    Next
                End If
            End If
            saveDimension = input.Rows
            ReDim kalman(input.Rows - 1)
            For i = 0 To input.Rows - 1
                kalman(i) = New Kalman_Simple
            Next
            output = New cv.Mat(input.Rows, 1, cv.MatType.CV_32F, 0)
        End If

        If task.useKalman Then
            For i = 0 To kalman.Length - 1
                kalman(i).inputReal = input.Get(Of Single)(i, 0)
                kalman(i).Run(src)
                output.Set(Of Single)(i, 0, kalman(i).stateResult)
            Next
        Else
            output = input ' do nothing to the input.
        End If


        If standalone Or task.intermediateReview = caller Then
            Dim rx(input.Rows - 1) As Single
            Dim testrect As New cv.Rect
            For i = 0 To input.Rows - 1
                rx(i) = output.Get(Of Single)(i, 0)
            Next
            dst1 = src
            Dim rect = New cv.Rect(CInt(rx(0)), CInt(rx(1)), CInt(rx(2)), CInt(rx(3)))
            rect = validateRect(rect)

            Static lastRect As cv.Rect = rect
            If lastRect = rect Then
                Dim r = initRandomRect(src.Width, src.Height, 25)
                Dim array() As Single = {r.X, r.Y, r.Width, r.Height}
                input = New cv.Mat(4, 1, cv.MatType.CV_32F, array)
            End If
            dst1.Rectangle(rect, cv.Scalar.Red, 2)
            lastRect = rect
        End If
    End Sub
End Class







Public Class Kalman_ImageSmall : Inherits VBparent
    Dim kalman As New Kalman_CVMat
    Dim resize As Resize_Percentage
    Public Sub New()
        resize = New Resize_Percentage()

        label1 = "The small image is processed by the Kalman filter"
        label2 = "Mask of the smoothed image minus original"
        task.desc = "Resize the image to allow the Kalman filter to process the whole image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        resize.Run(src)

        Dim saveOriginal = resize.dst1.Clone()
        Dim gray32f As New cv.Mat
        resize.dst1.ConvertTo(gray32f, cv.MatType.CV_32F)
        kalman.input = gray32f.Reshape(1, gray32f.Width * gray32f.Height)
        kalman.Run(src)
        Dim tmp As New cv.Mat
        kalman.output.ConvertTo(tmp, cv.MatType.CV_8U)
        tmp = tmp.Reshape(1, gray32f.Height)
        dst1 = tmp.Resize(dst1.Size())
        cv.Cv2.Subtract(tmp, saveOriginal, dst2)
        dst2 = dst2.Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst2 = dst2.Resize(dst1.Size())
    End Sub
End Class





Public Class Kalman_DepthSmall : Inherits VBparent
    Dim kalman As New Kalman_ImageSmall
    Public Sub New()
        label1 = "Mask of non-zero depth after Kalman smoothing"
        label2 = "Mask of the smoothed image minus original"
        task.desc = "Use a resized depth Mat to find where depth is decreasing (something getting closer.)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        kalman.Run(task.RGBDepth)
        dst1 = kalman.dst1
        dst2 = kalman.dst2
    End Sub
End Class







Public Class Kalman_Depth32f : Inherits VBparent
    Dim kalman As New Kalman_CVMat
    Dim resize As Resize_Percentage
    Public Sub New()
        resize = New Resize_Percentage()
        resize.sliders.trackbar(0).Value = 4

        label1 = "Mask of non-zero depth after Kalman smoothing"
        label2 = "Difference from original depth"
        task.desc = "Use a resized depth Mat to find where depth is decreasing (getting closer.)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        resize.Run(task.depth32f)

        kalman.input = resize.dst1.Reshape(1, resize.dst1.Width * resize.dst1.Height)
        kalman.Run(src)
        dst1 = kalman.output.Reshape(1, resize.dst1.Height)
        dst1 = dst1.Resize(src.Size())
        cv.Cv2.Subtract(dst1, task.depth32f, dst2)
        dst2 = dst2.Normalize(255)
    End Sub
End Class







Public Class Kalman_Single : Inherits VBparent
    Dim plot As New Plot_OverTime
    Dim kf As New cv.KalmanFilter(2, 1, 0)
    Dim processNoise As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Public measurement As New cv.Mat(1, 1, cv.MatType.CV_32F, 0)
    Public inputReal As Single
    Public stateResult As Single
    Public ProcessNoiseCov As Single = 0.00001
    Public MeasurementNoiseCov As Single = 0.1
    Public ErrorCovPost As Single = 1
    Public transitionMatrix() As Single = {1, 1, 0, 1} ' Change the transition matrix externally and set newTransmissionMatrix.
    Public newTransmissionMatrix As Boolean = True
    Public Sub New()
        Dim tMatrix() As Single = {1, 1, 0, 1}
        kf.TransitionMatrix = New cv.Mat(2, 2, cv.MatType.CV_32F, tMatrix)
        kf.MeasurementMatrix.SetIdentity(1)
        kf.ProcessNoiseCov.SetIdentity(0.00001)
        kf.MeasurementNoiseCov.SetIdentity(0.1)
        kf.ErrorCovPost.SetIdentity(1)
        If standalone Then plot.plotCount = 2 ' 2 items to plot
        task.desc = "Estimate a single value using a Kalman Filter - in the default case, the value of the mean of the grayscale image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateReview = caller Then
            dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            inputReal = dst1.Mean().Item(0)
        End If

        Dim prediction = kf.Predict()
        measurement.Set(Of Single)(0, 0, inputReal)
        stateResult = kf.Correct(measurement).Get(Of Single)(0, 0)
        If standalone Or task.intermediateReview = caller Then
            plot.plotData = New cv.Scalar(inputReal, stateResult, 0, 0)
            plot.Run(Nothing)
            dst2 = plot.dst1
            label1 = "Mean of the grayscale image is predicted"
            label2 = "Mean (blue) = " + Format(inputReal, "0.0") + " predicted (green) = " + Format(stateResult, "0.0")
        End If
    End Sub
End Class






' This algorithm is different and does not inherit from VBParent.  It is the minimal work to implement kalman to allow large Kalman sets.
Public Class Kalman_Simple : Implements IDisposable
    Dim kf As New cv.KalmanFilter(2, 1, 0)
    Dim processNoise As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Public measurement As New cv.Mat(1, 1, cv.MatType.CV_32F, 0)
    Public inputReal As Single
    Public stateResult As Single
    Public ProcessNoiseCov As Single = 0.00001
    Public MeasurementNoiseCov As Single = 0.1
    Public ErrorCovPost As Single = 1
    Public transitionMatrix() As Single = {1, 1, 0, 1} ' Change the transition matrix externally and set newTransmissionMatrix.
    Public newTransmissionMatrix As Boolean = True
    Public Sub New()
        Dim tMatrix() As Single = {1, 1, 0, 1}
        kf.TransitionMatrix = New cv.Mat(2, 2, cv.MatType.CV_32F, tMatrix)
        kf.MeasurementMatrix.SetIdentity(1)
        kf.ProcessNoiseCov.SetIdentity(0.00001)
        kf.MeasurementNoiseCov.SetIdentity(0.1)
        kf.ErrorCovPost.SetIdentity(1)
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim prediction = kf.Predict()
        measurement.Set(Of Single)(0, 0, inputReal)
        stateResult = kf.Correct(measurement).Get(Of Single)(0, 0)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class






' https://www.codeproject.com/Articles/865935/Object-Tracking-Kalman-Filter-with-Ease
' https://www.codeproject.com/Articles/326657/KalmanDemo
Public Class Kalman_VB : Inherits VBparent
    Const MAX_INPUT = 20
    Dim matrix As New List(Of Single)
    Dim oRand As Random
    Dim P(,) As Single = {{1, 0}, {0, 1}} '2x2 This is the covarience matrix
    Dim angle As Single
    Dim q_bias As Single
    Dim rate As Single
    Dim R_angle As Single = 0.002
    Dim Q_angle As Single = 0.001 'This is the process covarience matrix. It's how much we trust the accelerometer
    Dim Q_gyro As Single = 0.3
    Public input As Single
    Public Sub New()
        oRand = New Random(DateTime.Now.Millisecond)
        For i = 0 To MAX_INPUT - 1
            matrix.Add(0)
        Next
        If sliders.Setup(caller, 9) Then
            sliders.setupTrackBar(0, "Move this to see results", 0, 1000, 500)
            sliders.setupTrackBar(1, "Input with Noise", 0, 1000, 500)
            sliders.setupTrackBar(2, "20 point average of output", 0, 1000, 500)
            sliders.setupTrackBar(3, "Kalman Output", 0, 1000, 500)
            sliders.setupTrackBar(4, "20 Point average difference", 0, 1000, 500)
            sliders.setupTrackBar(5, "Kalman difference", 0, 1000, 500)
            sliders.setupTrackBar(6, "Simulated Noise", 0, 100, 25)
            sliders.setupTrackBar(7, "Simulated Bias", -100, 100, 0)
            sliders.setupTrackBar(8, "Simulated Scale", 0, 100, 0)
        End If
        label1 = "Use first slider in the options to test the algorithm.  The other sliders in the options visualize the impact."
        task.desc = "A native VB Kalman filter"
    End Sub
    Public Sub State_Update(ByVal q_m As Single)
        Dim dt As Single = 1 / 20
        Dim q As Single = q_m - q_bias 'Unbias our gyro
        Dim Pdot() As Single = {Q_angle - P(0, 1) - P(1, 0), -P(1, 1), -P(1, 1), Q_gyro}
        rate = q 'Store our unbias gyro estimate
        angle += q * dt

        'Update the covariance matrix
        P(0, 0) += Pdot(0) * dt
        P(0, 1) += Pdot(1) * dt
        P(1, 0) += Pdot(2) * dt
        P(1, 1) += Pdot(3) * dt
    End Sub
    Public Sub Kalman_Update()
        Dim angle_err As Single = input - angle
        Dim C_0 As Single = 1
        Dim PCt_0 = C_0 * P(0, 0) '+ C_1 * P(0, 1) 'This second part is always 0, so we don't bother
        Dim PCt_1 = C_0 * P(1, 0) '+ C_1 * P(1, 1)
        Dim E As Single = R_angle + C_0 * PCt_0 'Compute the error estimate.
        Dim K_0 As Single = PCt_0 / E 'Compute the Kalman filter gains
        Dim K_1 As Single = PCt_1 / E
        Dim t_0 As Single = PCt_0
        Dim t_1 As Single = C_0 * P(0, 1)

        P(0, 0) -= K_0 * t_0 'Update covariance matrix
        P(0, 1) -= K_0 * t_1
        P(1, 0) -= K_1 * t_0
        P(1, 1) -= K_1 * t_1

        angle += K_0 * angle_err 'Update our state estimate
        q_bias += K_1 * angle_err
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        input = sliders.trackbar(0).Value
        Dim noiselevel = sliders.trackbar(6).Value
        Dim additionalbias = sliders.trackbar(7).Value
        Dim scalefactor As Single = (sliders.trackbar(8).Value / 100) + 1 'This should be between 1 and 2
        Dim iRand = oRand.Next(0, noiselevel)
        Dim noisyInput = CInt((input * scalefactor) + additionalbias + iRand - (noiselevel / 2))

        If noisyInput < 0 Then noisyInput = 0
        If noisyInput > sliders.trackbar(1).Maximum Then noisyInput = sliders.trackbar(1).Maximum
        sliders.trackbar(1).Value = noisyInput

        matrix(task.frameCount Mod MAX_INPUT) = input
        Dim AverageOutput = (New cv.Mat(MAX_INPUT, 1, cv.MatType.CV_32F, matrix.ToArray)).Mean().Item(0)

        If AverageOutput < 0 Then AverageOutput = 0
        If AverageOutput > sliders.trackbar(2).Maximum Then AverageOutput = sliders.trackbar(2).Maximum
        sliders.trackbar(2).Value = CInt(AverageOutput)

        Dim AverageDiff = CInt(Math.Abs(AverageOutput - input) * 10)
        If AverageDiff > sliders.trackbar(4).Maximum Then AverageDiff = sliders.trackbar(4).Maximum
        sliders.trackbar(4).Value = AverageDiff

        'The Kalman Filter code comes from:
        'http://www.rotomotion.com/downloads/tilt.c

        'This is the Kalman Filter
        State_Update(noisyInput)
        'If ticks = 1 Then Kalman_Update(input) 'This updates the filter every 5 cycles
        Kalman_Update() 'This updates the filter every cycle
        Dim KalmanOutput As Single = angle

        If KalmanOutput < 0 Then KalmanOutput = 0
        If KalmanOutput > sliders.trackbar(3).Maximum Then KalmanOutput = sliders.trackbar(3).Maximum
        sliders.trackbar(3).Value = CInt(KalmanOutput)

        Dim KalmanDiff = CInt(Math.Abs(KalmanOutput - input) * 10)
        If KalmanDiff > sliders.trackbar(5).Maximum Then KalmanDiff = sliders.trackbar(5).Maximum
        sliders.trackbar(5).Value = KalmanDiff

        task.trueText(label1)
    End Sub
End Class





' https://towardsdatascience.com/kalman-filter-interview-bdc39f3e6cf3
' https://towardsdatascience.com/extended-kalman-filter-43e52b16757d
' https://towardsdatascience.com/the-unscented-kalman-filter-anything-ekf-can-do-i-can-do-it-better-ce7c773cf88d
Public Class Kalman_VB_Basics : Inherits VBparent
    Public kInput As Single
    Public kOutput As Single
    Public kAverage As Single
    Dim P(,) As Single = {{1, 0}, {0, 1}} '2x2 This is the covarience matrix
    Dim q_bias As Single
    Dim outputError As Single = 0.002
    Dim processCovar As Single = 0.001 'This is the process covarience matrix. It's how much we trust the accelerometer
    Dim matrix As New List(Of Single)
    Dim plot As New Plot_OverTime
    Dim basics As New Kalman_Basics
    Public Sub New()
        plot.plotCount = 3
        plot.topBottomPad = 20
        plot.dst1 = dst1

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Average input count", 1, 500, 20)
            sliders.setupTrackBar(1, "Delta Time X100", 1, 30, 5)
            sliders.setupTrackBar(2, "Process Covariance X10000", 0, 10000, 10)
            sliders.setupTrackBar(3, "pDot entry X1000", 0, 1000, 300)
        End If
        label1 = "Blue = gray mean, green = kalman, red = kalman avg"
        task.desc = "Build a generic kalman filter based on Kalman_VB"
    End Sub
    Public Sub State_Update(ByVal q_m As Single)
        Static deltaSlider = findSlider("Delta Time X100")
        Static covarSlider = findSlider("Process Covariance X10000")
        Static pDotSlider = findSlider("pDot entry X1000")

        Dim dt As Single = deltaSlider.value / 100
        Dim unbias As Single = q_m - q_bias 'Unbias our gyro
        Dim pdotEntry = pDotSlider.value / 1000
        processCovar = covarSlider.value / 10000
        Dim Pdot() As Single = {processCovar - P(0, 1) - P(1, 0), -P(1, 1), -P(1, 1), pdotEntry}
        kOutput += unbias * dt

        'Update the covariance matrix
        P(0, 0) += Pdot(0) * dt
        P(0, 1) += Pdot(1) * dt
        P(1, 0) += Pdot(2) * dt
        P(1, 1) += Pdot(3) * dt
    End Sub
    Public Sub Kalman_Update()
        Dim kError As Single = kInput - kOutput
        Dim C_0 As Single = 1
        Dim PCt_0 = C_0 * P(0, 0) '+ C_1 * P(0, 1) 'This second part is always 0, so we don't bother
        Dim PCt_1 = C_0 * P(1, 0) '+ C_1 * P(1, 1)
        Dim err As Single = outputError + C_0 * PCt_0 'Compute the error estimate.
        Dim K_0 As Single = PCt_0 / err 'Compute the Kalman filter gains
        Dim K_1 As Single = PCt_1 / err
        Dim t_0 As Single = PCt_0
        Dim t_1 As Single = C_0 * P(0, 1)

        P(0, 0) -= K_0 * t_0 'Update covariance matrix
        P(0, 1) -= K_0 * t_1
        P(1, 0) -= K_1 * t_0
        P(1, 1) -= K_1 * t_1

        kOutput += K_0 * kError 'Update our state estimate
        q_bias += K_1 * kError
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        If standalone Or task.intermediateReview = caller Then
            Dim gray = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            kInput = gray.Mean().Item(0)
        End If

        Static avgSlider = findSlider("Average input count")
        Static saveAvgCount As Integer
        If avgSlider.value <> saveAvgCount Then
            saveAvgCount = avgSlider.value
            matrix.Clear()
            For i = 0 To saveAvgCount - 1
                matrix.Add(kInput)
            Next
        End If

        matrix(task.frameCount Mod saveAvgCount) = kInput
        kAverage = (New cv.Mat(saveAvgCount, 1, cv.MatType.CV_32F, matrix.ToArray)).Mean().Item(0)

        If task.useKalman Then
            'The Kalman Filter code comes from:
            'http://www.rotomotion.com/downloads/tilt.c
            State_Update(kInput)
            Kalman_Update()
        Else
            kOutput = kInput
        End If

        If standalone Or task.intermediateReview = caller Then
            plot.plotData = New cv.Scalar(kOutput, kInput, kAverage)
            plot.Run(Nothing)
        End If
    End Sub
End Class
