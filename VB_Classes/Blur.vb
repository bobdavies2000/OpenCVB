Imports cv = OpenCvSharp
Imports CS_Classes
Public Class Blur_Basics : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Blur Kernel Size", 0, 32, 5)
        End If
        task.desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Sub Run(src as cv.Mat)
        Static kernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize As Integer = kernelSlider.Value
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            cv.Cv2.GaussianBlur(src, dst1, New cv.Size(kernelSize, kernelSize), 0, 0)
            If standalone Or task.intermediateReview = caller Then cv.Cv2.GaussianBlur(task.RGBDepth, dst2, New cv.Size(kernelSize, kernelSize), 0, 0)
        Else
            dst1 = src
        End If
    End Sub
End Class






Public Class Blur_Gaussian : Inherits VBparent
    Dim CS_BlurGaussian As New CS_BlurGaussian
    Dim blur As Blur_Basics
    Public Sub New()
        blur = New Blur_Basics()
        task.desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Sub Run(src as cv.Mat)
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = blurKernelSlider.Value
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            CS_BlurGaussian.Run(src, dst1, kernelSize)
            If standalone Or task.intermediateReview = caller Then CS_BlurGaussian.Run(task.RGBDepth, dst2, kernelSize)
        Else
            dst1 = src
        End If
    End Sub
End Class






Public Class Blur_Median_CS : Inherits VBparent
    Dim CS_BlurMedian As New CS_BlurMedian
    Dim blur As Blur_Basics
    Public Sub New()
        blur = New Blur_Basics()
        task.desc = "Replace each pixel with the median of neighborhood of varying sizes."
    End Sub
    Public Sub Run(src as cv.Mat)
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = blurKernelSlider.Value
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            CS_BlurMedian.Run(src, dst1, kernelSize)
            If standalone or task.intermediateReview = caller Then CS_BlurMedian.Run(task.RGBDepth, dst2, kernelSize)
        Else
            dst1 = src
        End If
    End Sub
End Class






Public Class Blur_Homogeneous : Inherits VBparent
    Dim blur As Blur_Basics
    Public Sub New()
        blur = New Blur_Basics()
        task.desc = "Smooth each pixel with a kernel of 1's of different sizes."
    End Sub
    Public Sub Run(src as cv.Mat)
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = CDbl(blurKernelSlider.Value)
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            dst1 = src.Blur(New cv.Size(kernelSize, kernelSize), New cv.Point(-1, -1))
            If standalone or task.intermediateReview = caller Then dst2 = task.RGBDepth.Blur(New cv.Size(kernelSize, kernelSize), New cv.Point(-1, -1))
        Else
            dst1 = src
            dst2 = task.RGBDepth
        End If
    End Sub
End Class







Public Class Blur_Median : Inherits VBparent
    Dim blur As Blur_Basics
    Public Sub New()
        blur = New Blur_Basics()
        task.desc = "Replace each pixel with the median of neighborhood of varying sizes."
    End Sub
    Public Sub Run(src as cv.Mat)
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = CDbl(blurKernelSlider.Value)
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            cv.Cv2.MedianBlur(src, dst1, kernelSize)
            If standalone or task.intermediateReview = caller Then cv.Cv2.MedianBlur(task.RGBDepth, dst2, kernelSize)
        Else
            dst1 = src
            dst2 = task.RGBDepth
        End If
    End Sub
End Class





' https://docs.opencv.org/2.4/modules/imgproc/doc/filtering.html?highlight=bilateralfilter
' https://www.tutorialspoint.com/opencv/opencv_bilateral_filter.htm
Public Class Blur_Bilateral : Inherits VBparent
    Dim blur As Blur_Basics
    Public Sub New()
        blur = New Blur_Basics()
        task.desc = "Smooth each pixel with a Gaussian kernel of different sizes but preserve edges"
    End Sub
    Public Sub Run(src as cv.Mat)
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = CDbl(blurKernelSlider.Value)
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            cv.Cv2.BilateralFilter(src, dst1, kernelSize, kernelSize * 2, kernelSize / 2)
        Else
            dst1 = src
        End If
    End Sub
End Class






Public Class Blur_PlusHistogram : Inherits VBparent
    Dim mat2to1 As Mat_2to1
    Dim blur As Blur_Bilateral
    Dim myhist As Histogram_EqualizeGray
    Public Sub New()
        mat2to1 = New Mat_2to1()

        blur = New Blur_Bilateral()
        myhist = New Histogram_EqualizeGray()

        label1 = "Use Blur slider to see impact on histogram peak values"
        label2 = "Top is before equalize, Bottom is after Equalize"
        task.desc = "Compound algorithms Blur and Histogram"
    End Sub
    Public Sub Run(src as cv.Mat)
        myhist.Run(src)

        mat2to1.mat(0) = myhist.dst1.Clone()

        blur.Run(src)

        myhist.Run(blur.dst1.Clone)

        mat2to1.mat(1) = myhist.dst2.Clone()
        mat2to1.Run(src)
        dst2 = mat2to1.dst1
        dst1 = blur.dst1
    End Sub
End Class






Public Class Blur_TopoMap : Inherits VBparent
    Dim gradient As Gradient_CartToPolar
    Dim addw As AddWeighted_Basics
    Public Sub New()

        addw = New AddWeighted_Basics
        findSlider("Weight").Value = 15

        gradient = New Gradient_CartToPolar

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Percent of Blurring", 0, 100, 20)
            sliders.setupTrackBar(1, "Reduction Factor", 2, 64, 20)
            sliders.setupTrackBar(2, "Frame Count Cycle", 1, 200, 50)
        End If
        label1 = "Image Gradient"
        task.desc = "Create a topo map from the blurred image"
    End Sub
    Public Sub Run(src as cv.Mat)
        Static savePercent As Single
        Static nextPercent As Single
        Static reductionSlider = findSlider("Reduction Factor")
        Static frameSlider = findSlider("Frame Count Cycle")
        Static percentSlider = findSlider("Percent of Blurring")
        If savePercent <> percentSlider.Value Then
            savePercent = percentSlider.Value
            nextPercent = savePercent
        End If

        Dim kernelSize = CInt(nextPercent / 100 * src.Width)
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        gradient.Run(src)
        dst1 = gradient.magnitude

        If kernelSize > 1 Then cv.Cv2.GaussianBlur(dst1, dst2, New cv.Size(kernelSize, kernelSize), 0, 0)
        dst2 = dst2.Normalize(255)
        dst2 = dst2.ConvertScaleAbs(255)

        dst2 = (dst2 * 1 / reductionSlider.Value).tomat
        dst2 = (dst2 * reductionSlider.Value).toMat

        task.palette.Run(dst2)

        addw.src2 = task.palette.dst1
        addw.Run(task.color)
        dst2 = addw.dst1

        label2 = "Blur = " + CStr(nextPercent) + "% Reduction Factor = " + CStr(reductionSlider.Value)
        If task.frameCount Mod frameSlider.Value = 0 Then nextPercent -= 1
        If nextPercent <= 0 Then nextPercent = savePercent
    End Sub
End Class

