Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Public Class BlurMotion_Basics_TA : Inherits TaskParent
    Public kernel As cv.Mat
    Public options As New Options_MotionBlur
    Dim blurSlider As TrackBar
    Dim blurAngleSlider As TrackBar
    Public Sub New()
        blurSlider = OptionParent.FindSlider("Motion Blur Length")
        blurAngleSlider = OptionParent.FindSlider("Motion Blur Angle")
        desc = "Use Filter2D to create a motion blur"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If standaloneTest() Then
            blurAngleSlider.Value = If(blurAngleSlider.Value < blurAngleSlider.Maximum, blurAngleSlider.Value + 1, blurAngleSlider.Minimum)
        End If
        kernel = New cv.Mat(options.kernelSize, options.kernelSize, cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim pt1 = New cv.Point(0, (options.kernelSize - 1) / 2)
        Dim pt2 = New cv.Point(options.kernelSize * Math.Cos(options.theta) + pt1.X, options.kernelSize * Math.Sin(options.theta) + pt1.Y)
        Line(kernel, pt1, pt2, New cv.Scalar(1 / options.kernelSize))
        Filter2D(src, dst2, -1, kernel)
        pt1 += New cv.Point(src.Width / 2, src.Height / 2)
        pt2 += New cv.Point(src.Width / 2, src.Height / 2)
        If options.showDirection Then Line(dst2, pt1, pt2, cv.Scalar.Yellow, task.lineWidth + 3, task.lineType)
    End Sub
End Class






' https://docs.opencvb.org/trunk/d1/dfd/tutorial_motion_deblur_filter.html
Public Class XR_BlurMotion_Deblur : Inherits TaskParent
    Dim mblur As New BlurMotion_Basics_TA
    Private Function calcPSF(filterSize As cv.Size, len As Integer, theta As Double) As cv.Mat
        Dim h As New cv.Mat(filterSize, cv.MatType.CV_32F, 0)
        Dim pt = New cv.Point(filterSize.Width / 2, filterSize.Height / 2)
        Ellipse(h, pt, New cv.Size(0, CInt(len / 2)), 90 - theta, 0, 360, New cv.Scalar(255), -1)
        Dim summa As cv.Scalar = Sum(h)
        Return h / summa(0)
    End Function
    Private Function calcWeinerFilter(input_h_PSF As cv.Mat, nsr As Double) As cv.Mat
        Dim h_PSF_shifted = fftShift(input_h_PSF)
        Dim planes() = {h_PSF_shifted.Clone(), New cv.Mat(h_PSF_shifted.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))}
        Dim complexI As New cv.Mat
        Merge(planes, complexI)
        Dft(complexI, complexI)
        planes = Split(complexI)
        Dim denom As New cv.Mat
        Pow(Abs(planes(0)), 2, denom)
        denom += nsr
        Dim output_G As New cv.Mat
        Divide(planes(0), denom, output_G)
        Return output_G
    End Function
    Private Function fftShift(inputImg As cv.Mat) As cv.Mat
        Dim outputImg = inputImg.Clone()
        Dim cx = outputImg.Width / 2
        Dim cy = outputImg.Height / 2
        Dim q0 = outputImg(New cv.Rect(0, 0, cx, cy))
        Dim q1 = outputImg(New cv.Rect(cx, 0, cx, cy))
        Dim q2 = outputImg(New cv.Rect(0, cy, cx, cy))
        Dim q3 = outputImg(New cv.Rect(cx, cy, cx, cy))
        Dim tmp = q0.Clone()
        q3.CopyTo(q0)
        tmp.CopyTo(q3)
        q1.CopyTo(tmp)
        q2.CopyTo(q1)
        tmp.CopyTo(q2)
        Return outputImg
    End Function
    Private Function edgeTaper(inputImg As cv.Mat, gamma As Double, beta As Double) As cv.Mat
        Dim nx = inputImg.Width
        Dim ny = inputImg.Height
        Dim w1 As New cv.Mat(1, nx, cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim w2 As New cv.Mat(ny, 1, cv.MatType.CV_32F, cv.Scalar.All(0))

        Dim dx = CSng(2.0 * Math.PI / nx)
        Dim x = CSng(-Math.PI)
        For i = 0 To nx - 1
            w1.Set(Of Single)(0, i, 0.5 * (Math.Tanh((x + gamma / 2) / beta) - Math.Tanh((x - gamma / 2) / beta)))
            x += dx
        Next

        Dim dy = CSng(2.0 * Math.PI / ny)
        Dim y = CSng(-Math.PI)
        For i = 0 To ny - 1
            w2.Set(Of Single)(i, 0, 0.5 * (Math.Tanh((y + gamma / 2) / beta) - Math.Tanh((y - gamma / 2) / beta)))
            y += dy
        Next
        Dim w = w2 * w1
        Dim outputImg As New cv.Mat
        Multiply(inputImg, w, outputImg)
        Return outputImg
    End Function
    Private Function filter2DFreq(inputImg As cv.Mat, H As cv.Mat) As cv.Mat
        Dim planes() = {inputImg.Clone(), New cv.Mat(inputImg.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))}
        Dim complexI As New cv.Mat
        Merge(planes, complexI)
        Dft(complexI, complexI, cv.DftFlags.Scale)
        Dim planesH() = {H.Clone(), New cv.Mat(H.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))}
        Dim complexH As New cv.Mat
        Merge(planesH, complexH)
        Dim complexIH As New cv.Mat
        MulSpectrums(complexI, complexH, complexIH, 0)

        Idft(complexIH, complexIH)
        planes = Split(complexIH)
        Return planes(0)
    End Function
    Public Sub New()
        desc = "Deblur a motion blurred image"
        labels(2) = "Blurred Image Input"
        labels(3) = "Deblurred Image Output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        mblur.options.Run()

        If task.heartBeat Then mblur.options.redoCheckBox.Checked = True
        If mblur.options.redoCheckBox.Checked Then
            mblur.Run(src)
            mblur.options.showDirection = False
            mblur.options.redoCheckBox.Checked = False
        Else
            mblur.Run(src)
        End If

        dst2 = mblur.dst2
        Dim beta = 0.2

        Dim width = src.Width
        Dim height = src.Height
        Dim roi = New cv.Rect(0, 0, If(width Mod 2, width - 1, width), If(height Mod 2, height - 1, height))

        Dim h = calcPSF(roi.Size(), mblur.options.restoreLen, mblur.options.theta)
        Dim hW = calcWeinerFilter(h, 1.0 / mblur.options.SNR)

        Dim gray8u As New cv.Mat
        CvtColor(dst2, gray8u, cv.ColorConversionCodes.BGR2GRAY)
        Dim imgIn As New cv.Mat
        gray8u.ConvertTo(imgIn, cv.MatType.CV_32F)
        imgIn = edgeTaper(imgIn, mblur.options.gamma, beta)

        Dim imgOut = filter2DFreq(imgIn(roi), hW)
        imgOut.ConvertTo(dst3, cv.MatType.CV_8U)
        Normalize(dst3, dst3, 0, 255, cv.NormTypes.MinMax)
    End Sub
End Class



