Imports cv = OpenCvSharp
Public Class Blur_Basics : Inherits VB_Parent
    Dim options As New Options_Blur
    Public Sub New()
        UpdateAdvice(traceName + ": use local options to control the kernel size and sigma.")
        desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        cv.Cv2.GaussianBlur(src, dst2, New cv.Size(options.kernelSize, options.kernelSize), options.sigma, options.sigma)
    End Sub
End Class







Public Class Blur_Homogeneous : Inherits VB_Parent
    Dim blur As New Blur_Basics
    Public Sub New()
        desc = "Smooth each pixel with a kernel of 1's of different sizes."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static blurKernelSlider = FindSlider("Blur Kernel Size")
        Dim kernelSize = CInt(blurKernelSlider.Value) Or 1
        dst2 = src.Blur(New cv.Size(kernelSize, kernelSize), New cv.Point(-1, -1))
    End Sub
End Class







Public Class Blur_Median : Inherits VB_Parent
    Dim blur As New Blur_Basics
    Public Sub New()
        desc = "Replace each pixel with the median of neighborhood of varying sizes."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static blurKernelSlider = FindSlider("Blur Kernel Size")
        Dim kernelSize = CInt(blurKernelSlider.Value) Or 1
        cv.Cv2.MedianBlur(src, dst2, kernelSize)
    End Sub
End Class





' https://docs.opencv.org/2.4/modules/imgproc/doc/filtering.html?highlight=bilateralfilter
' https://www.tutorialspoint.com/opencv/opencv_bilateral_filter.htm
Public Class Blur_Bilateral : Inherits VB_Parent
    Dim blur As New Blur_Basics
    Public Sub New()
        desc = "Smooth each pixel with a Gaussian kernel of different sizes but preserve edges"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static blurKernelSlider = FindSlider("Blur Kernel Size")
        Dim kernelSize = CInt(blurKernelSlider.Value) Or 1
        cv.Cv2.BilateralFilter(src, dst2, kernelSize, kernelSize * 2, kernelSize / 2)
    End Sub
End Class






Public Class Blur_PlusHistogram : Inherits VB_Parent
    Dim mat2to1 As New Mat_2to1
    Dim blur As New Blur_Bilateral
    Dim myhist As New Hist_EqualizeGray
    Public Sub New()
        labels(2) = "Use Blur slider to see impact on histogram peak values"
        labels(3) = "Top is before equalize, Bottom is after Equalize"
        desc = "Compound algorithms Blur and Histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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






Public Class Blur_TopoMap : Inherits VB_Parent
    Dim gradient As New Gradient_CartToPolar
    Dim addw As New AddWeighted_Basics
    Dim options As New Options_BlurTopo
    Public Sub New()
        labels(2) = "Image Gradient"
        desc = "Create a topo map from the blurred image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        gradient.Run(src)
        dst2 = gradient.magnitude

        If options.kernelSize > 1 Then cv.Cv2.GaussianBlur(dst2, dst3, New cv.Size(options.kernelSize, options.kernelSize), 0, 0)
        dst3 = dst3.Normalize(255)
        dst3 = dst3.ConvertScaleAbs(255)

        dst3 = (dst3 * 1 / options.reduction).ToMat
        dst3 = (dst3 * options.reduction).ToMat

        addw.src2 = ShowPalette(dst3)
        addw.Run(task.color)
        dst3 = addw.dst2

        labels(3) = "Blur = " + CStr(options.nextPercent) + "% Reduction Factor = " + CStr(options.reduction)
        If task.frameCount Mod options.frameCycle = 0 Then options.nextPercent -= 1
        If options.nextPercent <= 0 Then options.nextPercent = options.savePercent
    End Sub
End Class








Public Class Blur_Detection : Inherits VB_Parent
    Dim laplace As New Laplacian_Basics
    Dim blur As New Blur_Basics
    Public Sub New()
        FindSlider("Laplacian Threshold").Value = 50
        FindSlider("Blur Kernel Size").Value = 11
        labels = {"", "", "Draw a rectangle to blur a region in alternating frames and test further", "Detected blur in the highlight regions - non-blur is white."}
        desc = "Detect blur in an image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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
        setTrueText("Blur variance is " + Format(stdev * stdev, fmt3), 3)

        If standaloneTest() Then dst2.Rectangle(r, cv.Scalar.White, task.lineWidth)
    End Sub
End Class







Public Class Blur_Depth : Inherits VB_Parent
    Dim blur As New Blur_Basics
    Public Sub New()
        desc = "Blur the depth results to help find the boundaries to large depth regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst3 = task.depthRGB.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)

        blur.Run(dst3)
        dst2 = blur.dst2
    End Sub
End Class
