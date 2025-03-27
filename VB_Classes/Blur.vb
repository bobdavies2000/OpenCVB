Imports System.Windows.Forms
Imports cv = OpenCvSharp
Public Class Blur_Basics : Inherits TaskParent
    Public Options As New Options_Blur
    Public Sub New()
        UpdateAdvice(traceName + ": use local options to control the kernel size and sigma.")
        desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Options.Run()
        cv.Cv2.GaussianBlur(src, dst2, New cv.Size(Options.kernelSize, Options.kernelSize),
                            Options.sigmaX, Options.sigmaY)
    End Sub
End Class







Public Class Blur_Homogeneous : Inherits TaskParent
    Dim blur As New Blur_Basics
    Dim blurKernelSlider As TrackBar
    Public Sub New()
        desc = "Smooth each pixel with a kernel of 1's of different sizes."
        blurKernelSlider =optiBase.findslider("Blur Kernel Size")
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim kernelSize = CInt(blurKernelSlider.Value) Or 1
        dst2 = src.Blur(New cv.Size(kernelSize, kernelSize), New cv.Point(-1, -1))
    End Sub
End Class







Public Class Blur_Median : Inherits TaskParent
    Dim blur As New Blur_Basics
    Dim blurKernelSlider As TrackBar
    Public Sub New()
        desc = "Replace each pixel with the median of neighborhood of varying sizes."
        blurKernelSlider =optiBase.findslider("Blur Kernel Size")
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim kernelSize = CInt(blurKernelSlider.Value) Or 1
        cv.Cv2.MedianBlur(src, dst2, kernelSize)
    End Sub
End Class





' https://docs.opencvb.org/2.4/modules/imgproc/doc/filtering.html?highlight=bilateralfilter
' https://www.tutorialspoint.com/opencv/opencv_bilateral_filter.htm
Public Class Blur_Bilateral : Inherits TaskParent
    Dim blur As New Blur_Basics
    Dim blurKernelSlider As TrackBar
    Public Sub New()
        desc = "Smooth each pixel with a Gaussian kernel of different sizes but preserve edges"
        blurKernelSlider =optiBase.findslider("Blur Kernel Size")
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim kernelSize = CInt(blurKernelSlider.Value) Or 1
        cv.Cv2.BilateralFilter(src, dst2, kernelSize, kernelSize * 2, kernelSize / 2)
    End Sub
End Class






Public Class Blur_PlusHistogram : Inherits TaskParent
    Dim mat2to1 As New Mat_2to1
    Dim blur As New Blur_Bilateral
    Dim myhist As New Hist_EqualizeGray
    Public Sub New()
        labels(2) = "Use Blur slider to see impact on histogram peak values"
        labels(3) = "Top is before equalize, Bottom is after Equalize"
        desc = "Compound algorithms Blur and Histogram"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        myhist.Run(src)

        mat2to1.mat(0) = myhist.dst2.Clone

        blur.Run(src)
        dst2 = blur.dst2.Clone

        myhist.Run(blur.dst2)

        mat2to1.mat(1) = myhist.dst2.Clone
        mat2to1.Run(src)
        dst3 = mat2to1.dst2
    End Sub
End Class






Public Class Blur_TopoMap : Inherits TaskParent
    Dim gradient As New Gradient_CartToPolar
    Dim options As New Options_BlurTopo
    Public Sub New()
        labels(2) = "Image Gradient"
        desc = "Create a topo map from the blurred image"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        gradient.Run(src)
        dst2 = gradient.magnitude

        If options.kernelSize > 1 Then cv.Cv2.GaussianBlur(dst2, dst3, New cv.Size(options.kernelSize, options.kernelSize), 0, 0)
        dst3 = dst3.Normalize(255)
        dst3 = dst3.ConvertScaleAbs(255)

        dst3 = (dst3 * 1 / options.reduction).ToMat
        dst3 = (dst3 * options.reduction).ToMat

        dst3 = ShowAddweighted(dst3, task.color, labels(3))

        labels(2) = "Blur = " + CStr(options.nextPercent) + "% Reduction Factor = " + CStr(options.reduction)
        If task.frameCount Mod options.frameCycle = 0 Then options.nextPercent -= 1
        If options.nextPercent <= 0 Then options.nextPercent = options.savePercent
    End Sub
End Class








Public Class Blur_Detection : Inherits TaskParent
    Dim laplace As New Laplacian_Basics
    Dim blur As New Blur_Basics
    Public Sub New()
       optiBase.findslider("Laplacian Threshold").Value = 50
       optiBase.findslider("Blur Kernel Size").Value = 11
        labels = {"", "", "Draw a rectangle to blur a region in alternating frames and test further", "Detected blur in the highlight regions - non-blur is white."}
        desc = "Detect blur in an image"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim r = New cv.Rect(dst2.Width / 2 - 25, dst2.Height / 2 - 25, 50, 50)
        If standaloneTest() Then
            If task.drawRect <> New cv.Rect Then r = task.drawRect
            ' deliberately blur a small region to test the algorithm
            If task.frameCount Mod 2 Then
                blur.Run(src(r))
                src(r) = blur.dst2
            End If
        End If

        dst2 = src
        laplace.Run(src)
        dst3 = laplace.dst2

        Dim mean As Single, stdev As Single
        cv.Cv2.MeanStdDev(dst2, mean, stdev)
        SetTrueText("Blur variance is " + Format(stdev * stdev, fmt3), 3)

        If standaloneTest() Then dst2.Rectangle(r, white, task.lineWidth)
    End Sub
End Class







Public Class Blur_Depth : Inherits TaskParent
    Dim blur As New Blur_Basics
    Public Sub New()
        desc = "Blur the depth results to help find the boundaries to large depth regions"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst3 = task.depthRGB.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)

        blur.Run(dst3)
        dst2 = blur.dst2
    End Sub
End Class





Public Class Blur_Gaussian : Inherits TaskParent
    Public options As New Options_Blur()
    Public Sub New()
        desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()
        cv.Cv2.GaussianBlur(src, dst2, New cv.Size(options.kernelSize, options.kernelSize), 0, 0)
    End Sub
End Class