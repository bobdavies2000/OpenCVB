Imports cv = OpenCvSharp
Public Class Stabilizer_Basics : Inherits VB_Algorithm
    Dim match As New Match_Basics
    Public shiftX As Integer
    Public shiftY As Integer
    Public templateRect As cv.Rect
    Public searchRect As cv.Rect
    Public stableRect As cv.Rect
    Dim pad = 20
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Max % of lost pixels before reseting image", 0, 100, 10)
            sliders.setupTrackBar("Stabilizer Correlation Threshold X1000", 0, 1000, 950)
            sliders.setupTrackBar("Width of input to matchtemplate", 10, dst2.Width - pad, 128)
            sliders.setupTrackBar("Height of input to matchtemplate", 10, dst2.Height - pad, 96)
            sliders.setupTrackBar("Min stdev in correlation rect", 1, 50, 10)
        End If

        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(2) = "Current frame - rectangle input to matchTemplate"
        desc = "if reasonable stdev and no motion in correlation rectangle, stabilize image across frames"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static widthSlider = findSlider("Width of input to matchtemplate")
        Static heightSlider = findSlider("Height of input to matchtemplate")
        Static netSlider = findSlider("Max % of lost pixels before reseting image")
        Static stdevSlider = findSlider("Min stdev in correlation rect")
        Static thresholdSlider = findSlider("Stabilizer Correlation Threshold X1000")
        Dim lostMax = netSlider.Value / 100

        Dim resetImage As Boolean
        templateRect = New cv.Rect(src.Width / 2 - widthSlider.Value / 2, src.Height / 2 - heightSlider.Value / 2, widthSlider.Value, heightSlider.Value)

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame = input
        dst2 = input

        Dim mean As Single, stdev As Single
        cv.Cv2.MeanStdDev(dst2(templateRect), mean, stdev)

        If stdev > stdevSlider.Value Then
            Dim t = templateRect
            Dim w = t.Width + pad * 2
            Dim h = t.Height + pad * 2
            Dim x = Math.Abs(t.X - pad)
            Dim y = Math.Abs(t.Y - pad)
            searchRect = New cv.Rect(x, y, If(w < lastFrame.width, w, lastFrame.width - x - 1), If(h < lastFrame.height, h, lastFrame.height - y - 1))
            match.template = lastFrame(searchRect)
            match.Run(input(templateRect))

            If match.correlation > thresholdSlider.Value / thresholdSlider.maximum Then
                Dim maxLoc = New cv.Point(match.drawRect.X + match.drawRect.Width / 2, match.drawRect.Y + match.drawRect.Height / 2)
                shiftX = templateRect.X - maxLoc.X - searchRect.X
                shiftY = templateRect.Y - maxLoc.Y - searchRect.Y
                Dim x1 = If(shiftX < 0, Math.Abs(shiftX), 0)
                Dim y1 = If(shiftY < 0, Math.Abs(shiftY), 0)

                dst3.SetTo(0)

                Dim x2 = If(shiftX < 0, 0, shiftX)
                Dim y2 = If(shiftY < 0, 0, shiftY)
                stableRect = New cv.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
                Dim srcRect = New cv.Rect(x2, y2, stableRect.Width, stableRect.Height)
                stableRect = New cv.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
                input(srcRect).CopyTo(dst3(stableRect))
                Dim nonZero = dst3.CountNonZero / (dst3.Width * dst3.Height)
                If nonZero < (1 - lostMax) Then
                    labels(3) = "Lost pixels = " + Format(1 - nonZero, "00%")
                    resetImage = True
                End If
                labels(3) = "Offset (x, y) = (" + CStr(shiftX) + "," + CStr(shiftY) + "), " + Format(nonZero, "00%") + " preserved, cc=" + Format(match.correlation, fmt2)
            Else
                labels(3) = "Below correlation threshold " + Format(thresholdSlider.Value, fmt2) + " with " + Format(match.correlation, fmt2)
                resetImage = True
            End If
        Else
            labels(3) = "Correlation rectangle stdev is " + Format(stdev, "00") + " - too low"
            resetImage = True
        End If

        If resetImage Then
            input.CopyTo(lastFrame)
            dst3 = lastFrame.clone
        End If
        If standalone Then dst3.Rectangle(templateRect, cv.Scalar.White, 1) ' when not standalone, traceName doesn't want artificial rectangle.
    End Sub
End Class









Public Class Stabilizer_BasicsRandomInput : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Range of random motion introduced (absolute value in pixels)", 0, 30, 8)
        labels(2) = "Current frame (before)"
        labels(3) = "Image after shift"
        desc = "Generate images that have been arbitrarily shifted"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static rangeSlider = findSlider("Range of random motion introduced (absolute value in pixels)")
        Dim range = rangeSlider.Value

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim shiftX = msRNG.Next(-range, range)
        Dim shiftY = msRNG.Next(-range, range)

        Static lastShiftX = shiftX
        Static lastShiftY = shiftY
        If task.frameCount Mod 2 = 0 Then
            shiftX = lastShiftX
            shiftY = lastShiftY
        End If
        lastShiftX = shiftX
        lastShiftY = shiftY

        dst2 = input.Clone
        If shiftX <> 0 Or shiftY <> 0 Then
            Dim x = If(shiftX < 0, Math.Abs(shiftX), 0)
            Dim y = If(shiftY < 0, Math.Abs(shiftY), 0)

            Dim x2 = If(shiftX < 0, 0, shiftX)
            Dim y2 = If(shiftY < 0, 0, shiftY)

            Dim srcRect = New cv.Rect(x, y, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
            Dim dstRect = New cv.Rect(x2, y2, srcRect.Width, srcRect.Height)
            dst2(srcRect).CopyTo(input(dstRect))
        End If

        dst3 = input
    End Sub
End Class








Public Class Stabilizer_BasicsTest : Inherits VB_Algorithm
    Dim random As New Stabilizer_BasicsRandomInput
    Dim stable As New Stabilizer_Basics
    Public Sub New()
        labels(2) = "Unstable input to Stabilizer_Basics"
        desc = "Test the Stabilizer_Basics with random movement"
    End Sub
    Public Sub RunVB(src as cv.Mat)

        random.Run(src)
        stable.Run(random.dst3.Clone)

        dst2 = stable.dst2
        dst3 = stable.dst3
        If standalone Then dst3.Rectangle(stable.templateRect, cv.Scalar.White, 1)
        labels(3) = stable.labels(3)
    End Sub
End Class






' https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
Public Class Stabilizer_OpticalFlow : Inherits VB_Algorithm
    Public good As New Feature_Basics
    Public inputFeat As New List(Of cv.Point2f)
    Public borderCrop = 30
    Dim sumScale As cv.Mat, sScale As cv.Mat, features1 As cv.Mat
    Dim errScale As cv.Mat, qScale As cv.Mat, rScale As cv.Mat
    Public Sub New()
        desc = "Stabilize video with a Kalman filter.  Shake camera to see image edges appear.  This is not really working!"
        labels(2) = "Stabilized Image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim vert_Border = borderCrop * src.Rows / src.Cols
        If task.optionsChanged Then
            errScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 1)
            qScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.004)
            rScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.5)
            sumScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
            sScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
        End If

        dst2 = src

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        good.Run(src)
        inputFeat = New List(Of cv.Point2f)(good.featurePoints)
        features1 = New cv.Mat(inputFeat.Count, 1, cv.MatType.CV_32FC2, inputFeat.ToArray)

        Static lastFrame As cv.Mat = src.Clone()
        If task.frameCount > 0 Then
            Dim features2 = New cv.Mat
            Dim status As New cv.Mat
            Dim err As New cv.Mat
            Dim winSize As New cv.Size(3, 3)
            cv.Cv2.CalcOpticalFlowPyrLK(src, lastFrame, features1, features2, status, err, winSize, 3, term, cv.OpticalFlowFlags.None)
            lastFrame = src.Clone()

            Dim commonPoints = New List(Of cv.Point2f)
            Dim lastFeatures As New List(Of cv.Point2f)
            For i = 0 To status.Rows - 1
                If status.Get(Of Byte)(i, 0) Then
                    Dim pt1 = features1.Get(Of cv.Point2f)(i, 0)
                    Dim pt2 = features2.Get(Of cv.Point2f)(i, 0)
                    Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                    If length < 10 Then
                        commonPoints.Add(pt1)
                        lastFeatures.Add(pt2)
                    End If
                End If
            Next
            Dim affine = cv.Cv2.GetAffineTransform(commonPoints.ToArray, lastFeatures.ToArray)

            Dim dx = affine.Get(Of Double)(0, 2)
            Dim dy = affine.Get(Of Double)(1, 2)
            Dim da = Math.Atan2(affine.Get(Of Double)(1, 0), affine.Get(Of Double)(0, 0))
            Dim ds_x = affine.Get(Of Double)(0, 0) / Math.Cos(da)
            Dim ds_y = affine.Get(Of Double)(1, 1) / Math.Cos(da)
            Dim saveDX = dx, saveDY = dy, saveDA = da

            Dim text = "Original dx = " + Format(dx, fmt2) + vbNewLine + " dy = " + Format(dy, fmt2) + vbNewLine + " da = " + Format(da, fmt2)
            setTrueText(text)

            Dim sx = ds_x, sy = ds_y

            Dim delta As New cv.Mat(5, 1, cv.MatType.CV_64F, New Double() {ds_x, ds_y, da, dx, dy})
            cv.Cv2.Add(sumScale, delta, sumScale)

            Dim diff As New cv.Mat
            cv.Cv2.Subtract(sScale, sumScale, diff)

            da += diff.Get(Of Double)(2, 0)
            dx += diff.Get(Of Double)(3, 0)
            dy += diff.Get(Of Double)(4, 0)
            If Math.Abs(dx) > 50 Then dx = saveDX
            If Math.Abs(dy) > 50 Then dy = saveDY
            If Math.Abs(da) > 50 Then da = saveDA

            text = "dx = " + Format(dx, fmt2) + vbNewLine + " dy = " + Format(dy, fmt2) + vbNewLine + " da = " + Format(da, fmt2)
            setTrueText(text, New cv.Point(10, 100))

            Dim smoothedMat = New cv.Mat(2, 3, cv.MatType.CV_64F)
            smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 2, dx)
            smoothedMat.Set(Of Double)(1, 2, dy)

            Dim smoothedFrame = task.color.WarpAffine(smoothedMat, src.Size())
            smoothedFrame = smoothedFrame(New cv.Range(vert_Border, smoothedFrame.Rows - vert_Border), New cv.Range(borderCrop, smoothedFrame.Cols - borderCrop))
            dst3 = smoothedFrame.Resize(src.Size())

            For i = 0 To commonPoints.Count - 1
                dst2.Circle(commonPoints.ElementAt(i), task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
                dst2.Circle(lastFeatures.ElementAt(i), task.dotSize + 1, cv.Scalar.Blue, -1, task.lineType)
            Next
        End If
        inputFeat = Nothing ' show that we consumed the current set of features.
    End Sub
End Class









'Public Class Stabilizer_MotionDetect : Inherits VB_Algorithm
'    Dim motion As New Motion_Contours
'    Dim stable As New Stabilizer_Basics
'    Public Sub New()
'        If sliders.Setup(traceName) Then
'            sliders.setupTrackBar("Offset of stable rectangle from each side in pixels", 0, 100, 30)
'        End If

'        desc = "Detect motion in the stabilizer output"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        Static offsetSlider = findSlider("Offset of stable rectangle from each side in pixels")
'        Dim offset = offsetSlider.Value

'        stable.Run(src)

'        motion.Run(stable.dst3(stable.templateRect))
'        dst2 = stable.dst3
'        dst2.Rectangle(stable.templateRect, cv.Scalar.White, 1)
'        dst3 = motion.dst3
'    End Sub
'End Class










Public Class Stabilizer_VerticalIMUKalman : Inherits VB_Algorithm
    Dim rotate As New Rotate_Basics
    Dim kalman As New Kalman_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Use last X values for IMU", 1, 100, 20)

        desc = "Stabilize the vertical orientation of the image using only the IMU."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static angleSlider = findSlider("Rotation Angle in degrees")
        Static lastXSlider = findSlider("Use last X values for IMU")
        Dim xFrames = lastXSlider.Value

        Static angleXValue As New List(Of Single)
        Static angleYValue As New List(Of Single)

        angleXValue.Add(task.accRadians.X)
        angleYValue.Add(task.accRadians.Y)

        kalman.kInput = {angleXValue.Average, angleYValue.Average}
        kalman.Run(empty)

        strOut = "IMU X" + vbTab + "IMU Y" + vbTab + "IMU Z" + vbCrLf
        strOut += Format(task.accRadians.X * 57.2958, fmt1) + vbTab + Format(task.accRadians.Y * 57.2958, fmt1) + vbTab + Format(task.accRadians.Z * 57.2958, fmt1) + vbCrLf
        strOut += "Angle X" + vbTab + "Angle Y" + vbCrLf
        strOut += Format(kalman.kInput(0), fmt3) + vbTab + Format(kalman.kInput(1), fmt3) + vbCrLf

        strOut += "Angle X" + vbTab + "Angle Y (after Kalman filter" + vbCrLf
        strOut += Format(kalman.kOutput(0), fmt3) + vbTab + Format(kalman.kOutput(1), fmt3) + vbCrLf

        Dim angle = 90 - kalman.kOutput(1) * 57.2958
        If kalman.kOutput(0) < 0 Then angle *= -1
        angleSlider.Value = angle
        labels(2) = "stabilizer_Vertical Angle = " + Format(angle, fmt1)

        rotate.Run(src)
        dst2 = rotate.dst2
        setTrueText(strOut, 3)

        If angleXValue.Count >= xFrames Then angleXValue.RemoveAt(0)
        If angleYValue.Count >= xFrames Then angleYValue.RemoveAt(0)
    End Sub
End Class








Public Class Stabilizer_VerticalIMU : Inherits VB_Algorithm
    Dim rotate As New Rotate_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Use last X values for IMU", 1, 100, 20)
        desc = "Stabilize the vertical orientation of the image using only the IMU."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static angleSlider = findSlider("Rotation Angle in degrees")
        Static lastXSlider = findSlider("Use last X values for IMU")
        Dim xFrames = lastXSlider.Value

        Static angleXValue As New List(Of Single)
        Static angleYValue As New List(Of Single)

        angleXValue.Add(task.accRadians.X)
        angleYValue.Add(task.accRadians.Y)

        strOut = "IMU X" + vbTab + "IMU Y" + vbTab + "IMU Z" + vbCrLf
        strOut += Format(task.accRadians.X * 57.2958, fmt1) + vbTab + Format(task.accRadians.Y * 57.2958, fmt1) + vbTab + Format(task.accRadians.Z * 57.2958, fmt1) + vbCrLf

        Dim avgX = angleXValue.Average
        Dim avgY = angleYValue.Average
        strOut += "Angle X" + vbTab + "Angle Y" + vbCrLf
        strOut += Format(avgX, fmt3) + vbTab + Format(avgY, fmt3) + vbCrLf

        Dim angle = 90 - avgY * 57.2958
        If avgX < 0 Then angle *= -1
        angleSlider.Value = angle
        labels(2) = "stabilizer_Vertical Angle = " + Format(angle, fmt1)

        rotate.Run(src)
        dst2 = rotate.dst2
        setTrueText(strOut, 3)

        If angleXValue.Count >= xFrames Then angleXValue.RemoveAt(0)
        If angleYValue.Count >= xFrames Then angleYValue.RemoveAt(0)
    End Sub
End Class
