Imports cv = OpenCvSharp
'http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_Basics : Inherits VB_Parent
    Dim kalman() As Kalman_Simple
    Public kInput(4 - 1) As Single
    Public kOutput(4 - 1) As Single
    Public Sub New()
        desc = "Use Kalman to stabilize values (such as a cv.rect.)"
    End Sub
    Public Sub RunVB(src as cv.Mat)
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

        If task.gOptions.UseKalman.Checked Then
            For i = 0 To kalman.Length - 1
                kalman(i).inputReal = kInput(i)
                kalman(i).RunVB(Nothing)
                If Double.IsNaN(kalman(i).stateResult) Then kalman(i).stateResult = kalman(i).inputReal ' kalman failure...
                kOutput(i) = kalman(i).stateResult
            Next
        Else
            kOutput = kInput ' do nothing to the input.
        End If

        If standaloneTest() Then
            dst2 = src
            Dim rect = New cv.Rect(CInt(kOutput(0)), CInt(kOutput(1)), CInt(kOutput(2)), CInt(kOutput(3)))
            rect = validateRect(rect)
            Static lastRect = rect
            If rect = lastRect Then
                Dim r = initRandomRect(If(src.Height <= 240, 20, 50))
                kInput = New Single() {r.X, r.Y, r.Width, r.Height}
            End If
            lastRect = rect
            dst2.Rectangle(rect, cv.Scalar.White, task.lineWidth + 1)
            dst2.Rectangle(rect, cv.Scalar.Red, task.lineWidth)
        End If
    End Sub
End Class







' http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_Compare : Inherits VB_Parent
    Dim kalman() As Kalman_Single
    Public plot As New Plot_OverTimeScalar
    Public kPlot As New Plot_OverTimeScalar
    Public Sub New()
        plot.plotCount = 3
        kPlot.plotCount = 3

        labels(2) = "Kalman input: mean values for RGB"
        labels(3) = "Kalman output: smoothed mean values for RGB"
        desc = "Use this kalman filter to predict the next value."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.optionsChanged Then
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

        plot.plotData = src.Mean()
        plot.Run(empty)
        dst2 = plot.dst2

        For i = 0 To kalman.Count - 1
            kalman(i).inputReal = plot.plotData(i)
            kalman(i).Run(src)
        Next

        kPlot.plotData = New cv.Scalar(kalman(0).stateResult, kalman(1).stateResult, kalman(2).stateResult)
        kPlot.Run(empty)
        dst3 = kPlot.dst2
    End Sub
End Class






'https://github.com/opencv/opencv/blob/master/samples/cpp/kalman.cpp
Public Class Kalman_RotatingPoint : Inherits VB_Parent
    Dim kf As New cv.KalmanFilter(2, 1, 0)
    Dim kState As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Dim processNoise As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Dim measurement As New cv.Mat(1, 1, cv.MatType.CV_32F, 0)
    Dim center As cv.Point2f, statePt As cv.Point2f
    Dim radius As Single
    Private Function calcPoint(center As cv.Point2f, R As Double, angle As Double) As cv.Point
        Return center + New cv.Point2f(Math.Cos(angle), -Math.Sin(angle)) * R
    End Function
    Private Sub drawCross(dst2 As cv.Mat, center As cv.Point, color As cv.Scalar)
        Dim d = 3
        DrawLine(dst2, New cv.Point(center.X - d, center.Y - d), New cv.Point(center.X + d, center.Y + d), color)
        DrawLine(dst2, New cv.Point(center.X + d, center.Y - d), New cv.Point(center.X - d, center.Y + d), color)
    End Sub
    Public Sub New()
        labels(2) = "Estimate Yellow < Real Red (if working)"

        cv.Cv2.Randn(kState, New cv.Scalar(0), cv.Scalar.All(0.1))
        kf.TransitionMatrix = New cv.Mat(2, 2, cv.MatType.CV_32F, New Single() {1, 1, 0, 1})

        cv.Cv2.SetIdentity(kf.MeasurementMatrix)
        cv.Cv2.SetIdentity(kf.ProcessNoiseCov, cv.Scalar.All(0.00001))
        cv.Cv2.SetIdentity(kf.MeasurementNoiseCov, cv.Scalar.All(0.1))
        cv.Cv2.SetIdentity(kf.ErrorCovPost, cv.Scalar.All(1))
        cv.Cv2.Randn(kf.StatePost, New cv.Scalar(0), cv.Scalar.All(1))
        radius = dst2.Rows / 2.4 ' so we see the entire circle...
        center = New cv.Point2f(dst2.Cols / 2, dst2.Rows / 2)
        desc = "Track a rotating point using a Kalman filter. Yellow line (estimate) should be shorter than red (real)."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim stateAngle = kState.Get(Of Single)(0)

        Dim prediction = kf.Predict()
        Dim predictAngle = prediction.Get(Of Single)(0)
        Dim predictPt = calcPoint(center, radius, predictAngle)
        statePt = calcPoint(center, radius, stateAngle)

        cv.Cv2.Randn(measurement, New cv.Scalar(0), cv.Scalar.All(kf.MeasurementNoiseCov.Get(Of Single)(0)))

        measurement += kf.MeasurementMatrix * kState
        Dim measAngle = measurement.Get(Of Single)(0)
        Dim measPt = calcPoint(center, radius, measAngle)

        dst2.SetTo(0)
        drawCross(dst2, statePt, cv.Scalar.White)
        drawCross(dst2, measPt, cv.Scalar.White)
        drawCross(dst2, predictPt, cv.Scalar.White)
        dst2.Line(statePt, measPt, New cv.Scalar(0, 0, 255), task.lineWidth + 2, task.lineType)
        dst2.Line(statePt, predictPt, New cv.Scalar(0, 255, 255), task.lineWidth + 2, task.lineType)

        If msRNG.Next(0, 4) <> 0 Then kf.Correct(measurement)

        cv.Cv2.Randn(processNoise, cv.Scalar.Black, cv.Scalar.All(Math.Sqrt(kf.ProcessNoiseCov.Get(Of Single)(0, 0))))
        kState = kf.TransitionMatrix * kState + processNoise
    End Sub
End Class






' http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
' https://www.codeproject.com/Articles/865935/Object-Tracking-Kalman-Filter-with-Ease
Public Class Kalman_MousePredict : Inherits VB_Parent
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(2 - 1)
        ReDim kalman.kOutput(2 - 1)

        labels(2) = "Red is real mouse, white is prediction"
        desc = "Use kalman filter to predict the next mouse location."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.frameCount Mod 300 = 0 Then dst2.SetTo(0)

        Dim lastStateResult = New cv.Point(kalman.kOutput(0), kalman.kOutput(1))
        Static lastRealMouse = task.mouseMovePoint
        kalman.kInput = {task.mouseMovePoint.X, task.mouseMovePoint.Y}
        kalman.Run(src)
        DrawLine(dst2, New cv.Point(kalman.kOutput(0), kalman.kOutput(1)), lastStateResult, white)
        dst2.Line(task.mouseMovePoint, lastRealMouse, cv.Scalar.Red)
        lastRealMouse = task.mouseMovePoint
    End Sub
End Class







Public Class Kalman_CVMat : Inherits VB_Parent
    Dim kalman() As Kalman_Simple
    Public output As cv.Mat
    Dim basics As New Kalman_Basics
    Public input As cv.Mat
    Public Sub New()
        ReDim basics.kInput(4 - 1)
        input = New cv.Mat(4, 1, cv.MatType.CV_32F, 0)
        If standaloneTest() Then labels(2) = "Rectangle moves smoothly to random locations"
        desc = "Use Kalman to stabilize a set of values such as a cv.rect or cv.Mat"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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

        If task.gOptions.UseKalman.Checked Then
            For i = 0 To kalman.Length - 1
                kalman(i).inputReal = input.Get(Of Single)(i, 0)
                kalman(i).RunVB(src)
                output.Set(Of Single)(i, 0, kalman(i).stateResult)
            Next
        Else
            output = input ' do nothing to the input.
        End If

        If standaloneTest() Then
            Dim rx(input.Rows - 1) As Single
            Dim testrect As New cv.Rect
            For i = 0 To input.Rows - 1
                rx(i) = output.Get(Of Single)(i, 0)
            Next
            dst2 = src
            Dim rect = New cv.Rect(CInt(rx(0)), CInt(rx(1)), CInt(rx(2)), CInt(rx(3)))
            rect = validateRect(rect)

            Static lastRect As cv.Rect = rect
            If lastRect = rect Then
                Dim r = initRandomRect(25)
                Dim array() As Single = {r.X, r.Y, r.Width, r.Height}
                input = New cv.Mat(4, 1, cv.MatType.CV_32F, array)
            End If
            dst2.Rectangle(rect, cv.Scalar.Red, 2)
            lastRect = rect
        End If
    End Sub
End Class







Public Class Kalman_ImageSmall : Inherits VB_Parent
    Dim kalman As New Kalman_CVMat
    Dim resize As Resize_Smaller
    Public Sub New()
        resize = New Resize_Smaller()

        labels(2) = "The small image is processed by the Kalman filter"
        labels(3) = "Mask of the smoothed image minus original"
        desc = "Resize the image to allow the Kalman filter to process the whole image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        resize.Run(src)

        Dim saveOriginal = resize.dst2.Clone()
        Dim gray32f As New cv.Mat
        resize.dst2.ConvertTo(gray32f, cv.MatType.CV_32F)
        kalman.input = gray32f.Reshape(1, gray32f.Width * gray32f.Height)
        kalman.Run(src)
        Dim tmp As New cv.Mat
        kalman.output.ConvertTo(tmp, cv.MatType.CV_8U)
        tmp = tmp.Reshape(1, gray32f.Height)
        dst2 = tmp.Resize(dst2.Size())
        cv.Cv2.Subtract(tmp, saveOriginal, dst3)
        dst3 = dst3.Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst3 = dst3.Resize(dst2.Size())
    End Sub
End Class





Public Class Kalman_DepthSmall : Inherits VB_Parent
    Dim kalman As New Kalman_ImageSmall
    Public Sub New()
        labels(2) = "Mask of non-zero depth after Kalman smoothing"
        labels(3) = "Mask of the smoothed image minus original"
        desc = "Use a resized depth Mat to find where depth is decreasing (something getting closer.)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        kalman.Run(task.depthRGB)
        dst2 = kalman.dst2
        dst3 = kalman.dst3
    End Sub
End Class







Public Class Kalman_Depth32f : Inherits VB_Parent
    Dim kalman As New Kalman_CVMat
    Dim resize As Resize_Smaller
    Public Sub New()
        resize = New Resize_Smaller()
        FindSlider("Resize Percentage (%)").Value = 4

        labels(2) = "Mask of non-zero depth after Kalman smoothing"
        labels(3) = "Difference from original depth"
        desc = "Use a resized depth Mat to find where depth is decreasing (getting closer.)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        resize.Run(task.pcSplit(2))

        kalman.input = resize.dst2.Reshape(1, resize.dst2.Width * resize.dst2.Height)
        kalman.Run(src)
        dst2 = kalman.output.Reshape(1, resize.dst2.Height)
        dst2 = dst2.Resize(src.Size())
        cv.Cv2.Subtract(dst2, task.pcSplit(2), dst3)
        dst3 = dst3.Normalize(255)
    End Sub
End Class







Public Class Kalman_Single : Inherits VB_Parent
    Dim plot As New Plot_OverTimeScalar
    Dim kf As New cv.KalmanFilter(2, 1, 0)
    Dim processNoise As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Public measurement As New cv.Mat(1, 1, cv.MatType.CV_32F, 0)
    Public inputReal As Single
    Public stateResult As Single
    Public ProcessNoiseCov As Single = 0.00001
    Public MeasurementNoiseCov As Single = 0.1
    Public ErrorCovPost As Single = 1
    Public transitionMatrix() As Single = {1, 1, 0, 1} ' Change the transition matrix externally and set newTransmissionMat_vbbacrix.
    Public newTransmissionMatrix As Boolean = True
    Public Sub New()
        Dim tMatrix() As Single = {1, 1, 0, 1}
        kf.TransitionMatrix = New cv.Mat(2, 2, cv.MatType.CV_32F, tMatrix)
        kf.MeasurementMatrix.SetIdentity(1)
        kf.ProcessNoiseCov.SetIdentity(0.00001)
        kf.MeasurementNoiseCov.SetIdentity(0.1)
        kf.ErrorCovPost.SetIdentity(1)
        plot.plotCount = 2
        desc = "Estimate a single value using a Kalman Filter - in the default case, the value of the mean of the grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            inputReal = dst1.Mean()(0)
        End If

        Dim prediction = kf.Predict()
        measurement.Set(Of Single)(0, 0, inputReal)
        stateResult = kf.Correct(measurement).Get(Of Single)(0, 0)
        If standaloneTest() Then
            plot.plotData = New cv.Scalar(inputReal, stateResult, 0, 0)
            plot.Run(empty)
            dst2 = plot.dst2
            dst3 = plot.dst3
            labels(2) = "Mean of the grayscale image is predicted"
            labels(3) = "Mean (blue) = " + Format(inputReal, "0.0") + " predicted (green) = " + Format(stateResult, "0.0")
        End If
    End Sub
End Class






' This algorithm is different and does not inherit from VB_Parent.  It is the minimal work to implement kalman to allow large Kalman sets.
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
    Public newTMatrix As Boolean = True
    Public Sub updateTMatrix()
        kf.TransitionMatrix = New cv.Mat(2, 2, cv.MatType.CV_32F, transitionMatrix)
        kf.MeasurementMatrix.SetIdentity(1)
        kf.ProcessNoiseCov.SetIdentity(0.00001)
        kf.MeasurementNoiseCov.SetIdentity(0.1)
        kf.ErrorCovPost.SetIdentity(1)
    End Sub
    Public Sub New()
        Dim tMatrix() As Single = {1, 1, 0, 1}
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If newTMatrix Then
            newTMatrix = False
            updateTMatrix()
        End If
        Dim prediction = kf.Predict()
        measurement.Set(Of Single)(0, 0, inputReal)
        stateResult = kf.Correct(measurement).Get(Of Single)(0, 0)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        ' required dispose function.  It is tempting to remove this but it is needed...It does not inherit from VB_Parent...
    End Sub
End Class






' https://www.codeproject.com/Articles/865935/Object-Tracking-Kalman-Filter-with-Ease
' https://www.codeproject.com/Articles/326657/KalmanDemo
Public Class Kalman_VB : Inherits VB_Parent
    Const MAX_INPUT = 20
    Dim oRand As Random
    Dim P(,) As Single = {{1, 0}, {0, 1}} '2x2 This is the covarience matrix
    Dim q_bias As Single
    Dim rate As Single
    Dim R_angle As Single = 0.002
    Dim Q_angle As Single = 0.001 'This is the process covarience matrix. It's how much we trust the accelerometer
    Dim Q_gyro As Single = 0.3
    Dim options As New Options_Kalman_VB
    Public Sub New()
        oRand = New Random(DateTime.Now.Millisecond)
        For i = 0 To MAX_INPUT - 1
            options.matrix.Add(0)
        Next
        desc = "A native VB Kalman filter"
    End Sub
    Public Sub State_Update(ByVal q_m As Single)
        Dim dt As Single = 1 / 20
        Dim q As Single = q_m - q_bias 'Unbias our gyro
        Dim Pdot() As Single = {Q_angle - P(0, 1) - P(1, 0), -P(1, 1), -P(1, 1), Q_gyro}
        rate = q 'Store our unbias gyro estimate
        options.angle += q * dt

        'Update the covariance matrix
        P(0, 0) += Pdot(0) * dt
        P(0, 1) += Pdot(1) * dt
        P(1, 0) += Pdot(2) * dt
        P(1, 1) += Pdot(3) * dt
    End Sub
    Public Sub Kalman_Update()
        Dim angle_err As Single = options.kalmanInput - options.angle
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

        options.angle += K_0 * angle_err 'Update our state estimate
        q_bias += K_1 * angle_err
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        'The Kalman Filter code comes from:
        'http://www.rotomotion.com/downloads/tilt.c

        'This is the Kalman Filter
        State_Update(options.noisyInput)
        'If ticks = 1 Then Kalman_Update(input) 'This updates the filter every 5 cycles
        Kalman_Update() 'This updates the filter every cycle

        strOut = "Use first slider in the options to test the algorithm.  The other sliders in the options visualize the impact." + vbCrLf +
                 "In the options form nearby, resize the sliders to show more of them. " + vbCrLf +
                 "This shows more details of the impact of moving the first slider."
        setTrueText(strOut + vbCrLf + "The results are all in the output of the sliders in the options for " + traceName)
    End Sub
End Class





' https://towardsdatascience.com/kalman-filter-interview-bdc39f3e6cf3
' https://towardsdatascience.com/extended-kalman-filter-43e52b16757d
' https://towardsdatascience.com/the-unscented-kalman-filter-anything-ekf-can-do-i-can-do-it-better-ce7c773cf88d
Public Class Kalman_VB_Basics : Inherits VB_Parent
    Public kInput As Single
    Public kOutput As Single
    Public kAverage As Single
    Dim P(,) As Single = {{1, 0}, {0, 1}} '2x2 This is the covarience matrix
    Dim q_bias As Single
    Dim outputError As Single = 0.002
    Dim processCovar As Single = 0.001 'This is the process covarience matrix. It's how much we trust the accelerometer
    Dim matrix As New List(Of Single)
    Dim plot As New Plot_OverTimeScalar
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Average input count", 1, 500, 20)
            sliders.setupTrackBar("Delta Time X100", 1, 30, 5)
            sliders.setupTrackBar("Process Covariance X10000", 0, 10000, 10)
            sliders.setupTrackBar("pDot entry X1000", 0, 1000, 300)
        End If
        labels(2) = "Blue = grayscale mean after Kalman, green is grayscale mean value without Kalman, red is the grayscale average without Kalman"
        desc = "Build a generic kalman filter based on Kalman_VB"
    End Sub
    Public Sub State_Update(ByVal q_m As Single)
        Static deltaSlider = FindSlider("Delta Time X100")
        Static covarSlider = FindSlider("Process Covariance X10000")
        Static pDotSlider = FindSlider("pDot entry X1000")

        Dim dt As Single = deltaSlider.Value / 100
        Dim unbias As Single = q_m - q_bias 'Unbias our gyro
        Dim pdotEntry = pDotSlider.Value / 1000
        processCovar = covarSlider.Value / 10000
        Dim Pdot() As Single = {processCovar - P(0, 1) - P(1, 0), -P(1, 1), -P(1, 1), pdotEntry}
        kOutput += unbias * dt

        plot.plotCount = 3

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
    Public Sub RunVB(src As cv.Mat)

        If standaloneTest() Then kInput = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Mean()(0)

        Static avgSlider = FindSlider("Average input count")
        Static saveAvgCount As Integer
        If avgSlider.Value <> saveAvgCount Then
            saveAvgCount = avgSlider.Value
            matrix.Clear()
            For i = 0 To saveAvgCount - 1
                matrix.Add(kInput)
            Next
        End If

        matrix(task.frameCount Mod saveAvgCount) = kInput
        kAverage = (New cv.Mat(saveAvgCount, 1, cv.MatType.CV_32F, matrix.ToArray)).Mean()(0)

        If task.gOptions.UseKalman.Checked Then
            'The Kalman Filter code comes from:
            'http://www.rotomotion.com/downloads/tilt.c
            State_Update(kInput)
            Kalman_Update()
        Else
            kOutput = kInput
        End If

        If standaloneTest() Then
            plot.plotData = New cv.Scalar(kOutput, kInput, kAverage)
            plot.Run(empty)
            dst2 = plot.dst2
            dst3 = plot.dst3
        End If
        labels(3) = "Move the camera around to see the impact of the Kalman filter."
    End Sub
End Class
