Imports cv = OpenCvSharp
Imports CS_Classes
Public Class Blur_Basics : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Blur Kernel Size", 0, 32, 5)
        End If
        task.desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static kernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize As Integer = kernelSlider.Value
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            cv.Cv2.GaussianBlur(src, dst2, New cv.Size(kernelSize, kernelSize), 0, 0)
            If standalone Or task.intermediateName = caller Then cv.Cv2.GaussianBlur(task.RGBDepth, dst3, New cv.Size(kernelSize, kernelSize), 0, 0)
        Else
            dst2 = src
        End If
    End Sub
End Class






Public Class Blur_Gaussian : Inherits VBparent
    Dim CS_BlurGaussian As New CS_BlurGaussian
    Dim blur As New Blur_Basics
    Public Sub New()
        task.desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = blurKernelSlider.Value
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            CS_BlurGaussian.Run(src, dst2, kernelSize)
            If standalone Or task.intermediateName = caller Then CS_BlurGaussian.Run(task.RGBDepth, dst3, kernelSize)
        Else
            dst2 = src
        End If
    End Sub
End Class






Public Class Blur_Median_CS : Inherits VBparent
    Dim CS_BlurMedian As New CS_BlurMedian
    Dim blur As New Blur_Basics
    Public Sub New()
        task.desc = "Replace each pixel with the median of neighborhood of varying sizes."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = blurKernelSlider.Value
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            CS_BlurMedian.Run(src, dst2, kernelSize)
            If standalone Or task.intermediateName = caller Then CS_BlurMedian.Run(task.RGBDepth, dst3, kernelSize)
        Else
            dst2 = src
        End If
    End Sub
End Class






Public Class Blur_Homogeneous : Inherits VBparent
    Dim blur As New Blur_Basics
    Public Sub New()
        task.desc = "Smooth each pixel with a kernel of 1's of different sizes."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = CDbl(blurKernelSlider.Value)
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            dst2 = src.Blur(New cv.Size(kernelSize, kernelSize), New cv.Point(-1, -1))
            If standalone Or task.intermediateName = caller Then dst3 = task.RGBDepth.Blur(New cv.Size(kernelSize, kernelSize), New cv.Point(-1, -1))
        Else
            dst2 = src
            dst3 = task.RGBDepth
        End If
    End Sub
End Class







Public Class Blur_Median : Inherits VBparent
    Dim blur As New Blur_Basics
    Public Sub New()
        task.desc = "Replace each pixel with the median of neighborhood of varying sizes."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = CDbl(blurKernelSlider.Value)
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            cv.Cv2.MedianBlur(src, dst2, kernelSize)
            If standalone Or task.intermediateName = caller Then cv.Cv2.MedianBlur(task.RGBDepth, dst3, kernelSize)
        Else
            dst2 = src
            dst3 = task.RGBDepth
        End If
    End Sub
End Class





' https://docs.opencv.org/2.4/modules/imgproc/doc/filtering.html?highlight=bilateralfilter
' https://www.tutorialspoint.com/opencv/opencv_bilateral_filter.htm
Public Class Blur_Bilateral : Inherits VBparent
    Dim blur As New Blur_Basics
    Public Sub New()
        task.desc = "Smooth each pixel with a Gaussian kernel of different sizes but preserve edges"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = CDbl(blurKernelSlider.Value)
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            cv.Cv2.BilateralFilter(src, dst2, kernelSize, kernelSize * 2, kernelSize / 2)
        Else
            dst2 = src
        End If
    End Sub
End Class






Public Class Blur_PlusHistogram : Inherits VBparent
    Dim mat2to1 As New Mat_2to1
    Dim blur As New Blur_Bilateral
    Dim myhist As New Histogram_EqualizeGray
    Public Sub New()
        labels(2) = "Use Blur slider to see impact on histogram peak values"
        labels(3) = "Top is before equalize, Bottom is after Equalize"
        task.desc = "Compound algorithms Blur and Histogram"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        myhist.RunClass(src)

        mat2to1.mat(0) = myhist.dst2.Clone()

        blur.RunClass(src)

        myhist.RunClass(blur.dst2.Clone)

        mat2to1.mat(1) = myhist.dst3.Clone()
        mat2to1.RunClass(src)
        dst3 = mat2to1.dst2
        dst2 = blur.dst2
    End Sub
End Class






Public Class Blur_TopoMap : Inherits VBparent
    Dim gradient As New Gradient_CartToPolar
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Percent of Blurring", 0, 100, 20)
            sliders.setupTrackBar(1, "Reduction Factor", 2, 64, 20)
            sliders.setupTrackBar(2, "Frame Count Cycle", 1, 200, 50)
        End If
        labels(2) = "Image Gradient"
        task.desc = "Create a topo map from the blurred image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static reductionSlider = findSlider("Reduction Factor")
        Static frameSlider = findSlider("Frame Count Cycle")
        Static percentSlider = findSlider("Percent of Blurring")

        Static savePercent As Single
        Static nextPercent As Single
        If savePercent <> percentSlider.Value Then
            savePercent = percentSlider.Value
            nextPercent = savePercent
        End If

        Dim kernelSize = CInt(nextPercent / 100 * src.Width)
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        gradient.RunClass(src)
        dst2 = gradient.magnitude

        If kernelSize > 1 Then cv.Cv2.GaussianBlur(dst2, dst3, New cv.Size(kernelSize, kernelSize), 0, 0)
        dst3 = dst3.Normalize(255)
        dst3 = dst3.ConvertScaleAbs(255)

        dst3 = (dst3 * 1 / reductionSlider.Value).tomat
        dst3 = (dst3 * reductionSlider.Value).toMat

        task.palette.RunClass(dst3)

        addw.src2 = task.palette.dst2
        addw.RunClass(task.color)
        dst3 = addw.dst2

        labels(3) = "Blur = " + CStr(nextPercent) + "% Reduction Factor = " + CStr(reductionSlider.Value)
        If task.frameCount Mod frameSlider.Value = 0 Then nextPercent -= 1
        If nextPercent <= 0 Then nextPercent = savePercent
    End Sub
End Class

