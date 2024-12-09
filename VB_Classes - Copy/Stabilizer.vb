Imports OpenCvSharp.Features2D
Imports cvb = OpenCvSharp
Public Class Stabilizer_Basics : Inherits TaskParent
    Dim match As New Match_Basics
    Public shiftX As Integer
    Public shiftY As Integer
    Public templateRect As cvb.Rect
    Public searchRect As cvb.Rect
    Public stableRect As cvb.Rect
    Dim options As New Options_Stabilizer
    Dim lastFrame As cvb.Mat
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels(2) = "Current frame - rectangle input to matchTemplate"
        desc = "if reasonable stdev and no motion in correlation rectangle, stabilize image across frames"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim resetImage As Boolean
        templateRect = New cvb.Rect(src.Width / 2 - options.width / 2, src.Height / 2 - options.height / 2,
                                   options.width, options.height)

        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If task.FirstPass Then lastFrame = src.Clone()

        dst2 = src.Clone

        Dim mean As cvb.Scalar
        Dim stdev As cvb.Scalar
        cvb.Cv2.MeanStdDev(dst2(templateRect), mean, stdev)

        If stdev > options.minStdev Then
            Dim t = templateRect
            Dim w = t.Width + options.pad * 2
            Dim h = t.Height + options.pad * 2
            Dim x = Math.Abs(t.X - options.pad)
            Dim y = Math.Abs(t.Y - options.pad)
            searchRect = New cvb.Rect(x, y, If(w < lastFrame.width, w, lastFrame.width - x - 1), If(h < lastFrame.height, h, lastFrame.height - y - 1))
            match.template = lastFrame(searchRect)
            match.Run(src(templateRect))

            If match.correlation > options.corrThreshold Then
                Dim maxLoc = New cvb.Point(match.matchCenter.X, match.matchCenter.Y)
                shiftX = templateRect.X - maxLoc.X - searchRect.X
                shiftY = templateRect.Y - maxLoc.Y - searchRect.Y
                Dim x1 = If(shiftX < 0, Math.Abs(shiftX), 0)
                Dim y1 = If(shiftY < 0, Math.Abs(shiftY), 0)

                dst3.SetTo(0)

                Dim x2 = If(shiftX < 0, 0, shiftX)
                Dim y2 = If(shiftY < 0, 0, shiftY)
                stableRect = New cvb.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
                Dim srcRect = New cvb.Rect(x2, y2, stableRect.Width, stableRect.Height)
                stableRect = New cvb.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
                src(srcRect).CopyTo(dst3(stableRect))
                Dim nonZero = dst3.CountNonZero / (dst3.Width * dst3.Height)
                If nonZero < (1 - options.lostMax) Then
                    labels(3) = "Lost pixels = " + Format(1 - nonZero, "00%")
                    resetImage = True
                End If
                labels(3) = "Offset (x, y) = (" + CStr(shiftX) + "," + CStr(shiftY) + "), " + Format(nonZero, "00%") + " preserved, cc=" + Format(match.correlation, fmt2)
            Else
                labels(3) = "Below correlation threshold " + Format(options.corrThreshold, fmt2) + " with " +
                            Format(match.correlation, fmt2)
                resetImage = True
            End If
        Else
            labels(3) = "Correlation rectangle stdev is " + Format(stdev(0), "00") + " - too low"
            resetImage = True
        End If

        If resetImage Then
            src.CopyTo(lastFrame)
            dst3 = lastFrame.clone
        End If
        If standaloneTest() Then dst3.Rectangle(templateRect, white, 1) ' when not standaloneTest(), traceName doesn't want artificial rectangle.
    End Sub
End Class









Public Class Stabilizer_BasicsRandomInput : Inherits TaskParent
    Dim options As New Options_StabilizerOther
    Dim lastShiftX As Integer
    Dim lastShiftY As Integer

    Public Sub New()
        labels(2) = "Current frame (before)"
        labels(3) = "Image after shift"
        desc = "Generate images that have been arbitrarily shifted"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim input = src
        If input.Channels() <> 1 Then input = input.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Dim shiftX = msRNG.Next(-options.range, options.range)
        Dim shiftY = msRNG.Next(-options.range, options.range)

        If task.FirstPass Then
            lastShiftX = shiftX
            lastShiftY = shiftY
        End If
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

            Dim srcRect = New cvb.Rect(x, y, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
            Dim dstRect = New cvb.Rect(x2, y2, srcRect.Width, srcRect.Height)
            dst2(srcRect).CopyTo(input(dstRect))
        End If

        dst3 = input
    End Sub
End Class








Public Class Stabilizer_BasicsTest : Inherits TaskParent
    Dim random As New Stabilizer_BasicsRandomInput
    Dim stable As New Stabilizer_Basics
    Public Sub New()
        labels(2) = "Unstable input to Stabilizer_Basics"
        desc = "Test the Stabilizer_Basics with random movement"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)

        random.Run(src)
        stable.Run(random.dst3.Clone)

        dst2 = stable.dst2
        dst3 = stable.dst3
        If standaloneTest() Then dst3.Rectangle(stable.templateRect, white, 1)
        labels(3) = stable.labels(3)
    End Sub
End Class






' https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
Public Class Stabilizer_OpticalFlow : Inherits TaskParent
    Public inputFeat As New List(Of cvb.Point2f)
    Public borderCrop = 30
    Dim sumScale As cvb.Mat, sScale As cvb.Mat, features1 As cvb.Mat
    Dim errScale As cvb.Mat, qScale As cvb.Mat, rScale As cvb.Mat
    Public Sub New()
        desc = "Stabilize video with a Kalman filter.  Shake camera to see image edges appear.  This is not really working!"
        labels(2) = "Stabilized Image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim vert_Border = borderCrop * src.Rows / src.Cols
        If task.optionsChanged Then
            errScale = New cvb.Mat(New cvb.Size(1, 5), cvb.MatType.CV_64F, 1)
            qScale = New cvb.Mat(New cvb.Size(1, 5), cvb.MatType.CV_64F, 0.004)
            rScale = New cvb.Mat(New cvb.Size(1, 5), cvb.MatType.CV_64F, 0.5)
            sumScale = New cvb.Mat(New cvb.Size(1, 5), cvb.MatType.CV_64F, 0)
            sScale = New cvb.Mat(New cvb.Size(1, 5), cvb.MatType.CV_64F, 0)
        End If

        dst2 = src

        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        inputFeat = New List(Of cvb.Point2f)(task.features)
        features1 = cvb.Mat.FromPixelData(inputFeat.Count, 1, cvb.MatType.CV_32FC2, inputFeat.ToArray)

        Static lastFrame As cvb.Mat = src.Clone()
        If task.frameCount > 0 Then
            Dim features2 = New cvb.Mat
            Dim status As New cvb.Mat
            Dim err As New cvb.Mat
            Dim winSize As New cvb.Size(3, 3)
            cvb.Cv2.CalcOpticalFlowPyrLK(src, lastFrame, features1, features2, status, err, winSize, 3, term, cvb.OpticalFlowFlags.None)
            lastFrame = src.Clone()

            Dim commonPoints = New List(Of cvb.Point2f)
            Dim lastFeatures As New List(Of cvb.Point2f)
            For i = 0 To status.Rows - 1
                If status.Get(Of Byte)(i, 0) Then
                    Dim pt1 = features1.Get(Of cvb.Point2f)(i, 0)
                    Dim pt2 = features2.Get(Of cvb.Point2f)(i, 0)
                    Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                    If length < 10 Then
                        commonPoints.Add(pt1)
                        lastFeatures.Add(pt2)
                    End If
                End If
            Next
            Dim affine = cvb.Cv2.GetAffineTransform(commonPoints.ToArray, lastFeatures.ToArray)

            Dim dx = affine.Get(Of Double)(0, 2)
            Dim dy = affine.Get(Of Double)(1, 2)
            Dim da = Math.Atan2(affine.Get(Of Double)(1, 0), affine.Get(Of Double)(0, 0))
            Dim ds_x = affine.Get(Of Double)(0, 0) / Math.Cos(da)
            Dim ds_y = affine.Get(Of Double)(1, 1) / Math.Cos(da)
            Dim saveDX = dx, saveDY = dy, saveDA = da

            Dim text = "Original dx = " + Format(dx, fmt2) + vbNewLine + " dy = " + Format(dy, fmt2) + vbNewLine + " da = " + Format(da, fmt2)
            SetTrueText(text)

            Dim sx = ds_x, sy = ds_y

            Dim delta As cvb.Mat = cvb.Mat.FromPixelData(5, 1, cvb.MatType.CV_64F, New Double() {ds_x, ds_y, da, dx, dy})
            cvb.Cv2.Add(sumScale, delta, sumScale)

            Dim diff As New cvb.Mat
            cvb.Cv2.Subtract(sScale, sumScale, diff)

            da += diff.Get(Of Double)(2, 0)
            dx += diff.Get(Of Double)(3, 0)
            dy += diff.Get(Of Double)(4, 0)
            If Math.Abs(dx) > 50 Then dx = saveDX
            If Math.Abs(dy) > 50 Then dy = saveDY
            If Math.Abs(da) > 50 Then da = saveDA

            text = "dx = " + Format(dx, fmt2) + vbNewLine + " dy = " + Format(dy, fmt2) + vbNewLine + " da = " + Format(da, fmt2)
            SetTrueText(text, New cvb.Point(10, 100))

            Dim smoothedMat = New cvb.Mat(2, 3, cvb.MatType.CV_64F)
            smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 2, dx)
            smoothedMat.Set(Of Double)(1, 2, dy)

            Dim smoothedFrame = task.color.WarpAffine(smoothedMat, src.Size())
            smoothedFrame = smoothedFrame(New cvb.Range(vert_Border, smoothedFrame.Rows - vert_Border), New cvb.Range(borderCrop, smoothedFrame.Cols - borderCrop))
            dst3 = smoothedFrame.Resize(src.Size())

            For i = 0 To commonPoints.Count - 1
                DrawCircle(dst2, commonPoints.ElementAt(i), task.DotSize + 3, cvb.Scalar.Red)
                DrawCircle(dst2, lastFeatures.ElementAt(i), task.DotSize + 1, cvb.Scalar.Blue)
            Next
        End If
        inputFeat = Nothing ' show that we consumed the current set of features.
    End Sub
End Class









Public Class Stabilizer_VerticalIMU : Inherits TaskParent
    Public stableTest As Boolean
    Public stableStr As String
    Dim angleXValue As New List(Of Single)
    Dim angleYValue As New List(Of Single)
    Dim stableCount As New List(Of Integer)
    Dim lastAngleX As Single, lastAngleY As Single
    Public Sub New()
        desc = "Use the IMU angular velocity to determine if the camera is moving or stable."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        angleXValue.Add(task.accRadians.X)
        angleYValue.Add(task.accRadians.Y)

        strOut = "IMU X" + vbTab + "IMU Y" + vbTab + "IMU Z" + vbCrLf
        strOut += Format(task.accRadians.X * 57.2958, fmt3) + vbTab + Format(task.accRadians.Y * 57.2958, fmt3) + vbTab +
                  Format(task.accRadians.Z * 57.2958, fmt3) + vbCrLf
        Dim avgX = angleXValue.Average
        Dim avgY = angleYValue.Average
        If task.FirstPass Then
            lastAngleX = avgX
            lastAngleY = avgY
        End If
        strOut += "Angle X" + vbTab + "Angle Y" + vbCrLf
        strOut += Format(avgX, fmt3) + vbTab + Format(avgY, fmt3) + vbCrLf

        Dim angle = 90 - avgY * 57.2958
        If avgX < 0 Then angle *= -1
        labels(2) = "stabilizer_Vertical Angle = " + Format(angle, fmt1)

        stableTest = Math.Abs(lastAngleX - avgX) < 0.001 And Math.Abs(lastAngleY - avgY) < 0.01
        stableCount.Add(If(stableTest, 1, 0))
        If task.heartBeat Then
            Dim avgStable = stableCount.Average
            stableStr = "IMU stable = " + Format(avgStable, "0.0%") + " of the time"
            stableCount.Clear()
        End If
        SetTrueText(strOut + vbCrLf + stableStr, 2)

        lastAngleX = avgX
        lastAngleY = avgY

        If angleXValue.Count >= task.frameHistoryCount Then angleXValue.RemoveAt(0)
        If angleYValue.Count >= task.frameHistoryCount Then angleYValue.RemoveAt(0)
    End Sub
End Class









Public Class Stabilizer_CornerPoints : Inherits TaskParent
    Public basics As New Stable_Basics
    Public features As New List(Of cvb.Point2f)
    Dim ul As cvb.Rect, ur As cvb.Rect, ll As cvb.Rect, lr As cvb.Rect
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("FAST Threshold", 0, 200, task.FASTthreshold)
        desc = "Track the FAST feature points found in the corners of the BGR image."
    End Sub
    Private Sub getKeyPoints(src As cvb.Mat, r As cvb.Rect)
        Static thresholdSlider = FindSlider("FAST Threshold")
        Dim kpoints() As cvb.KeyPoint = cvb.Cv2.FAST(src(r), thresholdSlider.value, True)
        For Each kp In kpoints
            features.Add(New cvb.Point2f(kp.Pt.X + r.X, kp.Pt.Y + r.Y))
        Next
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.optionsChanged Then
            Dim size = task.gridSize
            ul = New cvb.Rect(0, 0, size, size)
            ur = New cvb.Rect(dst2.Width - size, 0, size, size)
            ll = New cvb.Rect(0, dst2.Height - size, size, size)
            lr = New cvb.Rect(dst2.Width - size, dst2.Height - size, size, size)
        End If

        src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        features.Clear()
        getKeyPoints(src, ul)
        getKeyPoints(src, ur)
        getKeyPoints(src, ll)
        getKeyPoints(src, lr)

        dst2.SetTo(0)
        For Each pt In features
            DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Yellow)
        Next
        labels(2) = "There were " + CStr(features.Count) + " key points detected"
    End Sub
End Class