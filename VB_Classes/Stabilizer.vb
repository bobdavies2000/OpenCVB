Imports cv = OpenCvSharp
Public Class Stabilizer_Basics : Inherits VBparent
    Dim match As MatchTemplate_Basics
    Public shiftX As Integer
    Public shiftY As Integer
    Public templateRect As cv.Rect
    Public searchRect As cv.Rect
    Public stableRect As cv.Rect
    Dim pad = 20
    Public Sub New()
        match = New MatchTemplate_Basics

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 5)
            sliders.setupTrackBar(0, "Maximum percentage of lost pixels before image is reset", 0, 100, 10)
            sliders.setupTrackBar(1, "Stabilizer Correlation Threshold X1000", 0, 1000, 950)
            sliders.setupTrackBar(2, "Width of input to matchtemplate", 10, dst1.Width - pad, 128)
            sliders.setupTrackBar(3, "Height of input to matchtemplate", 10, dst1.Height - pad, 96)
            sliders.setupTrackBar(4, "Min stdev in correlation rect", 1, 50, 10)
        End If

        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        label1 = "Current frame - rectangle input to matchTemplate"
        task.desc = "if reasonable stdev and no motion in correlation rectangle, stabilize image across frames"
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim resetImage As Boolean

        Static widthSlider = findSlider("Width of input to matchtemplate")
        Static heightSlider = findSlider("Height of input to matchtemplate")
        templateRect = New cv.Rect(src.Width / 2 - widthSlider.value / 2, src.Height / 2 - heightSlider.value / 2, widthSlider.value, heightSlider.value)

        Static netSlider = findSlider("Maximum percentage of lost pixels before image is reset")
        Dim lostMax = netSlider.value / 100

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame = input
        dst1 = input

        Dim mean As Single, stdev As Single
        cv.Cv2.MeanStdDev(dst1(templateRect), mean, stdev)

        Static stdevSlider = findSlider("Min stdev in correlation rect")
        If stdev > stdevSlider.value Then
            Dim t = templateRect
            searchRect = New cv.Rect(t.X - pad, t.Y - pad, t.Width + pad * 2, t.Height + pad * 2)
            match.searchArea = lastFrame(searchRect)
            match.template = input(templateRect)
            match.Run(src)

            Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point
            match.correlationMat.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

            Static thresholdSlider = findSlider("Stabilizer Correlation Threshold X1000")
            If maxVal > thresholdSlider.value / thresholdSlider.maximum Then
                shiftX = templateRect.X - maxLoc.X - searchRect.X
                shiftY = templateRect.Y - maxLoc.Y - searchRect.Y
                Dim x1 = If(shiftX < 0, Math.Abs(shiftX), 0)
                Dim y1 = If(shiftY < 0, Math.Abs(shiftY), 0)

                dst2.SetTo(0)

                Dim x2 = If(shiftX < 0, 0, shiftX)
                Dim y2 = If(shiftY < 0, 0, shiftY)
                stableRect = New cv.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
                Dim srcRect = New cv.Rect(x2, y2, stableRect.Width, stableRect.Height)
                stableRect = New cv.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
                input(srcRect).CopyTo(dst2(stableRect))
                Dim nonZero = dst2.CountNonZero() / (dst2.Width * dst2.Height)
                If nonZero < (1 - lostMax) Then
                    label2 = "Lost pixels = " + Format(1 - nonZero, "00%")
                    resetImage = True
                End If
                label2 = "Offset (x, y) = (" + CStr(shiftX) + "," + CStr(shiftY) + "), " + Format(nonZero, "00%") + " preserved, cc=" + Format(maxVal, "0.00")
            Else
                label2 = "Below correlation threshold " + Format(thresholdSlider.value, "0.00") + " with " + Format(maxVal, "0.00")
                resetImage = True
            End If
        Else
            label2 = "Correlation rectangle stdev is " + Format(stdev, "00") + " - too low"
            resetImage = True
        End If

        If resetImage Then
            input.CopyTo(lastFrame)
            dst2 = lastFrame.clone
        End If
        If standalone Then dst2.Rectangle(templateRect, cv.Scalar.White, 1) ' when not standalone, caller doesn't want artificial rectangle.
    End Sub
End Class









Public Class Stabilizer_BasicsRandomInput : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Range of random motion introduced (absolute value in pixels)", 0, 30, 8)
        End If

        label1 = "Current frame (before)"
        label2 = "Image after shift"
        task.desc = "Generate images that have been arbitrarily shifted"
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static rangeSlider = findSlider("Range of random motion introduced (absolute value in pixels)")
        Dim range = rangeSlider.value
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

        dst1 = input.Clone
        If shiftX <> 0 Or shiftY <> 0 Then
            Dim x = If(shiftX < 0, Math.Abs(shiftX), 0)
            Dim y = If(shiftY < 0, Math.Abs(shiftY), 0)

            Dim x2 = If(shiftX < 0, 0, shiftX)
            Dim y2 = If(shiftY < 0, 0, shiftY)

            Dim srcRect = New cv.Rect(x, y, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
            Dim dstRect = New cv.Rect(x2, y2, srcRect.Width, srcRect.Height)
            dst1(srcRect).CopyTo(input(dstRect))
        End If

        dst2 = input
    End Sub
End Class








Public Class Stabilizer_BasicsTest : Inherits VBparent
    Dim random As Stabilizer_BasicsRandomInput
    Dim stable As Stabilizer_Basics
    Public Sub New()
        stable = New Stabilizer_Basics
        random = New Stabilizer_BasicsRandomInput

        label1 = "Unstable input to Stabilizer_Basics"
        task.desc = "Test the Stabilizer_Basics with random movement"
    End Sub
    Public Sub Run(src as cv.Mat)

        random.Run(src)
        stable.Run(random.dst2.Clone)

        dst1 = stable.dst1
        dst2 = stable.dst2
        If standalone Then dst2.Rectangle(stable.templateRect, cv.Scalar.White, 1)
        label2 = stable.label2
    End Sub
End Class






' https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
Public Class Stabilizer_OpticalFlow : Inherits VBparent
    Public good As Features_GoodFeatures
    Public inputFeat As New List(Of cv.Point2f)
    Public borderCrop = 30
    Dim sumScale As cv.Mat, sScale As cv.Mat, features1 As cv.Mat
    Dim errScale As cv.Mat, qScale As cv.Mat, rScale As cv.Mat
    Public Sub New()
        good = New Features_GoodFeatures()

        task.desc = "Stabilize video with a Kalman filter.  Shake camera to see image edges appear.  This is not really working!"
        label1 = "Stabilized Image"
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim vert_Border = borderCrop * src.Rows / src.Cols
        If task.frameCount = 0 Then
            errScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 1)
            qScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.004)
            rScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.5)
            sumScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
            sScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
        End If

        dst1 = src

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If inputFeat Is Nothing Then
            good.Run(src)
            inputFeat = good.goodFeatures
        End If
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

            Dim text = "Original dx = " + Format(dx, "#0.00") + vbNewLine + " dy = " + Format(dy, "#0.00") + vbNewLine + " da = " + Format(da, "#0.00")
            task.trueText(text)

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

            text = "dx = " + Format(dx, "#0.00") + vbNewLine + " dy = " + Format(dy, "#0.00") + vbNewLine + " da = " + Format(da, "#0.00")
            task.trueText(text, 10, 100)

            Dim smoothedMat = New cv.Mat(2, 3, cv.MatType.CV_64F)
            smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 2, dx)
            smoothedMat.Set(Of Double)(1, 2, dy)

            Dim smoothedFrame = task.color.WarpAffine(smoothedMat, src.Size())
            smoothedFrame = smoothedFrame(New cv.Range(vert_Border, smoothedFrame.Rows - vert_Border), New cv.Range(borderCrop, smoothedFrame.Cols - borderCrop))
            dst2 = smoothedFrame.Resize(src.Size())

            For i = 0 To commonPoints.Count - 1
                dst1.Circle(commonPoints.ElementAt(i), 5, cv.Scalar.Red, -1, task.lineType)
                dst1.Circle(lastFeatures.ElementAt(i), 3, cv.Scalar.Blue, -1, task.lineType)
            Next
        End If
        inputFeat = Nothing ' show that we consumed the current set of features.
    End Sub
End Class









Public Class Stabilizer_MotionDetect : Inherits VBparent
    Dim motion As Motion_Basics
    Dim stable As Stabilizer_Basics
    Public Sub New()
        motion = New Motion_Basics
        stable = New Stabilizer_Basics


        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Offset of stable rectangle from each side in pixels", 0, 100, 30)
        End If

        task.desc = "Detect motiion in the stabilizer output"
    End Sub
    Public Sub Run(src as cv.Mat)

        stable.Run(src)

        Static offsetSlider = findSlider("Offset of stable rectangle from each side in pixels")
        Dim offset = offsetSlider.value
        motion.Run(stable.dst2(stable.templateRect))
        dst1 = stable.dst2
        dst1.Rectangle(stable.templateRect, cv.Scalar.White, 1)
        dst2 = motion.dst2
    End Sub
End Class
