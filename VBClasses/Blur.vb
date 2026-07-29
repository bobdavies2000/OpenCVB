Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Blur_Basics : Inherits TaskParent
        Public Options As New Options_Blur
        Public Sub New()
            desc = "Smooth each pixel with a Gaussian kernel of different sizes."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Options.Run()
            GaussianBlur(src, dst2, New Size(Options.kernelSize, Options.kernelSize),
                                        Options.sigmaX, Options.sigmaY)
        End Sub
    End Class







    Public Class XR_Blur_Homogeneous : Inherits TaskParent
        Dim blurC As New Blur_Basics
        Dim blurKernelSlider As TrackBar
        Public Sub New()
            desc = "Smooth each pixel with a kernel of 1's of different sizes."
            blurKernelSlider = OptionParent.FindSlider("Blur Kernel Size")
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim kernelSize = CInt(blurKernelSlider.Value) Or 1
            Blur(src, dst2, New Size(kernelSize, kernelSize), New cv.Point(-1, -1))
        End Sub
    End Class







    Public Class XR_Blur_Median : Inherits TaskParent
        Dim blurC As New Blur_Basics
        Dim blurKernelSlider As TrackBar
        Public Sub New()
            desc = "Replace each pixel with the median of neighborhood of varying sizes."
            blurKernelSlider = OptionParent.FindSlider("Blur Kernel Size")
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim kernelSize = CInt(blurKernelSlider.Value) Or 1
            MedianBlur(src, dst2, kernelSize)
        End Sub
    End Class





    ' https://docs.opencvb.org/2.4/modules/imgproc/doc/filtering.html?highlight=bilateralfilter
    ' https://www.tutorialspoint.com/opencv/opencv_bilateral_filter.htm
    Public Class Blur_Bilateral : Inherits TaskParent
        Dim Options As New Options_Blur
        Public Sub New()
            desc = "Smooth each pixel with a Gaussian kernel of different sizes but preserve edges"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Options.Run()

            BilateralFilter(src, dst2, Options.kernelSize, Options.kernelSize * 2, Options.kernelSize / 2)
        End Sub
    End Class







    Public Class XR_Blur_TopoMap : Inherits TaskParent
        Dim gradient As New Gradient_CartToPolar
        Dim options As New Options_BlurTopo
        Public Sub New()
            labels(2) = "Image Gradient"
            desc = "Create a topo map from the blurred image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            gradient.Run(src)
            dst2 = gradient.magnitude

            If options.kernelSize > 1 Then GaussianBlur(dst2, dst3, New Size(options.kernelSize, options.kernelSize), 0, 0)
            Normalize(dst3, dst3, 255)
            ConvertScaleAbs(dst3, dst3, 255)

            dst3 = (dst3 * 1 / options.blurReduction).ToMat
            dst3 = (dst3 * options.blurReduction).ToMat

            dst3 = ShowAddweighted(dst3, task.color, labels(3))

            labels(2) = "Blur = " + CStr(options.nextPercent) + "% Reduction Factor = " + CStr(options.blurReduction)
            If task.fOptions.FrameHistoryCount.Value Mod options.frameCycle = 0 Then options.nextPercent -= 1
            If options.nextPercent <= 0 Then options.nextPercent = options.savePercent
        End Sub
    End Class








    Public Class XR_Blur_Detection : Inherits TaskParent
        Dim laplace As New Laplacian_Basics
        Dim blurC As New Blur_Basics
        Public Sub New()
            OptionParent.FindSlider("Laplacian Threshold").Value = 50
            OptionParent.FindSlider("Blur Kernel Size").Value = 11
            labels = {"", "", "Detected blur in the highlight regions - non-blur is white.", "Draw a rectangle to blur a region in alternating frames and test further"}
            desc = "Detect blur in an image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim r = New cv.Rect(dst2.Width / 2 - 25, dst2.Height / 2 - 25, 50, 50)
            If standaloneTest() Then
                If task.drawRect <> New cv.Rect Then
                    r = task.drawRect
                    If r.Width = 0 Then r.Width = 50
                    If r.Height = 0 Then r.Height = 50
                End If
                r = ValidateRect(r)
                ' deliberately blur a small region to test the algorithm
                If task.fOptions.FrameHistoryCount.Value Mod 2 Then
                    blurC.Run(src(r))
                    src(r) = blurC.dst2
                End If
            End If

            laplace.Run(src)
            dst2 = laplace.dst2
            dst3 = laplace.dst3

            Dim mean As Single, stdev As Single
            MeanStdDev(dst2, mean, stdev)
            SetTrueText("Blur variance is " + (stdev * stdev).ToString(fmt3), 3)

            If standaloneTest() Then Rectangle(dst2, r, white, task.lineWidth)
        End Sub
    End Class







    Public Class XR_Blur_Depth : Inherits TaskParent
        Dim blurC As New Blur_Basics
        Public Sub New()
            desc = "Blur the depth results to help find the boundaries to large depth regions"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim _cvt1 As New Mat
            CvtColor(task.depthRGB, _cvt1, ColorConversionCodes.BGR2GRAY)
            Threshold(_cvt1, dst3, 0, 255, ThresholdTypes.Binary)

            blurC.Run(dst3)
            dst2 = blurC.dst2
        End Sub
    End Class





    Public Class XR_Blur_Gaussian : Inherits TaskParent
        Public options As New Options_Blur()
        Public Sub New()
            desc = "Smooth each pixel with a Gaussian kernel of different sizes."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            GaussianBlur(src, dst2, New Size(options.kernelSize, options.kernelSize), 0, 0)
        End Sub
    End Class






    Public Class XR_Blur_PlusHistogram : Inherits TaskParent
        Dim mat2to1 As New Mat_2to1
        Dim blurB As New Blur_Bilateral
        Dim myhist As New Histogram_EqualizeGray
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels(2) = "Use Blur slider to see impact on histogram peak values"
            desc = "Compound algorithms Blur and Histogram"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            myhist.Run(task.gray)

            mat2to1.mat(0) = myhist.dst2.Clone

            blurB.Run(task.gray)
            dst3 = blurB.dst2.Clone

            myhist.Run(blurB.dst2)
            dst2 = myhist.dst3

            mat2to1.mat(1) = myhist.dst2.Clone
            mat2to1.Run(src)
            dst1 = mat2to1.dst2
            SetTrueText("Top is before equalize, Bottom is after Equalize", 1)
        End Sub
    End Class







    Public Class XR_Blur_Histogram : Inherits TaskParent
        Dim blurB As New Blur_Bilateral
        Dim myhist As New Histogram_Basics
        Public Sub New()
            labels(2) = "Histogram of the input without any blurC."
            desc = "Visualize the impact of blurring with the histogram.  Draw a rectangle anywhere to test a section of the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.drawRect <> New cv.Rect Then src = task.gray(task.drawRect) Else src = task.gray
            Static kernelSlider = OptionParent.FindSlider("Blur Kernel Size")

            myhist.Run(src)
            dst2 = myhist.dst2.Clone

            blurB.Run(src)

            myhist.Run(blurB.dst2)
            dst3 = myhist.dst2

            If task.heartBeat Then
                If kernelSlider.value >= kernelSlider.maximum Then kernelSlider.value = 1
                kernelSlider.value += 2
                labels(3) = "Blur kernel size = " + CStr(kernelSlider.value)
            End If
        End Sub
    End Class





    Public Class Blur_Motion : Inherits TaskParent
        Public kernel As Mat
        Public options As New Options_MotionBlur
        Dim blurAngleSlider As TrackBar
        Public Sub New()
            blurAngleSlider = OptionParent.FindSlider("Motion Blur Angle")
            desc = "Use Filter2D to create a motion blur"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            If standaloneTest() Then
                blurAngleSlider.Value = If(blurAngleSlider.Value < blurAngleSlider.Maximum, blurAngleSlider.Value + 1, blurAngleSlider.Minimum)
            End If
            kernel = New Mat(options.kernelSize, options.kernelSize, MatType.CV_32F, Scalar.All(0))
            Dim pt1 = New cv.Point(0, (options.kernelSize - 1) / 2)
            Dim pt2 = New cv.Point(options.kernelSize * Math.Cos(options.theta) + pt1.X, options.kernelSize * Math.Sin(options.theta) + pt1.Y)
            Line(kernel, pt1, pt2, New Scalar(1 / options.kernelSize))
            Filter2D(src, dst2, -1, kernel)
            pt1 += New cv.Point(src.Width / 2, src.Height / 2)
            pt2 += New cv.Point(src.Width / 2, src.Height / 2)
            If options.showDirection Then Line(dst2, pt1, pt2, Scalar.Yellow, task.lineWidth + 3, task.lineType)
        End Sub
    End Class






    ' https://docs.opencvb.org/trunk/d1/dfd/tutorial_motion_deblur_filter.html
    Public Class XR_Blur_Deblur : Inherits TaskParent
        Dim mblur As New Blur_Motion
        Private Shared Function calcPSF(filterSize As Size, len As Integer, theta As Double) As Mat
            Dim h As New Mat(filterSize, MatType.CV_32F, 0)
            Dim pt = New cv.Point(filterSize.Width / 2, filterSize.Height / 2)
            Ellipse(h, pt, New Size(0, CInt(len / 2)), 90 - theta, 0, 360, New Scalar(255), -1)
            Dim summa As Scalar = Sum(h)
            Return h / summa(0)
        End Function
        Private Shared Function calcWeinerFilter(input_h_PSF As Mat, nsr As Double) As Mat
            Dim h_PSF_shifted = fftShift(input_h_PSF)
            Dim planes() = {h_PSF_shifted.Clone(), New Mat(h_PSF_shifted.Size(), MatType.CV_32F, Scalar.All(0))}
            Dim complexI As New Mat
            Merge(planes, complexI)
            Dft(complexI, complexI)
            planes = Split(complexI)
            Dim denom As New Mat
            Pow(Abs(planes(0)), 2, denom)
            denom += nsr
            Dim output_G As New Mat
            Divide(planes(0), denom, output_G)
            Return output_G
        End Function
        Private Shared Function fftShift(inputImg As Mat) As Mat
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
        Private Shared Function edgeTaper(inputImg As Mat, gamma As Double, beta As Double) As Mat
            Dim nx = inputImg.Width
            Dim ny = inputImg.Height
            Dim w1 As New Mat(1, nx, MatType.CV_32F, Scalar.All(0))
            Dim w2 As New Mat(ny, 1, MatType.CV_32F, Scalar.All(0))

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
            Dim outputImg As New Mat
            Multiply(inputImg, w, outputImg)
            Return outputImg
        End Function
        Private Shared Function filter2DFreq(inputImg As Mat, H As Mat) As Mat
            Dim planes() = {inputImg.Clone(), New Mat(inputImg.Size(), MatType.CV_32F, Scalar.All(0))}
            Dim complexI As New Mat
            Merge(planes, complexI)
            Dft(complexI, complexI, DftFlags.Scale)
            Dim planesH() = {H.Clone(), New Mat(H.Size(), MatType.CV_32F, Scalar.All(0))}
            Dim complexH As New Mat
            Merge(planesH, complexH)
            Dim complexIH As New Mat
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

            Dim gray8u As New Mat
            CvtColor(dst2, gray8u, ColorConversionCodes.BGR2GRAY)
            Dim imgIn As New Mat
            gray8u.ConvertTo(imgIn, MatType.CV_32F)
            imgIn = edgeTaper(imgIn, mblur.options.gamma, beta)

            Dim imgOut = filter2DFreq(imgIn(roi), hW)
            imgOut.ConvertTo(dst3, MatType.CV_8U)
            Normalize(dst3, dst3, 0, 255, NormTypes.MinMax)
        End Sub
    End Class
End Namespace