Imports cvb = OpenCvSharp
Imports OpenCvSharp.Text
' https://docs.opencvb.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Filter_Laplacian : Inherits VB_Parent
    Public Sub New()
        labels(2) = "Sharpened image using Filter2D output"
        labels(3) = "Output of Filter2D (approximated Laplacian)"
        desc = "Use a filter to approximate the Laplacian derivative."
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        Dim imgLaplacian = src.Filter2D(cvb.MatType.CV_32F,
                                        cvb.Mat.FromPixelData(3, 3, cvb.MatType.CV_32FC1, New Single() {1, 1, 1, 1, -8, 1, 1, 1, 1}))
        src.ConvertTo(dst1, cvb.MatType.CV_32F)
        dst0 = (dst1 - imgLaplacian).ToMat
        dst0.ConvertTo(dst2, src.Type)
        imgLaplacian.ConvertTo(dst3, src.Type)
    End Sub
End Class







Public Class Filter_NormalizedKernel : Inherits VB_Parent
    Dim options As New Options_FilterNorm
    Public Sub New()
        desc = "Create a normalized kernel and use it."
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        Options.RunVB()

        Dim sum As Double
        For i = 0 To options.kernel.Width - 1
            sum += Math.Abs(options.kernel.Get(Of Single)(0, i))
        Next
        labels(2) = "kernel sum = " + Format(sum, fmt3)

        Dim dst32f = src.Filter2D(cvb.MatType.CV_32FC1, options.kernel, anchor:=New cvb.Point(0, 0))
        dst32f.ConvertTo(dst2, cvb.MatType.CV_8UC3)
    End Sub
End Class






' https://docs.opencvb.org/2.4/doc/tutorials/imgproc/imgtrans/filter_2d/filter_2d.html
Public Class Filter_Normalized2D : Inherits VB_Parent
    Dim options As New Options_Filter
    Public Sub New()
        desc = "Create and apply a normalized kernel."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()
        Dim kernelSize As Integer = If(standaloneTest(), (task.frameCount Mod 20) + 1, options.kernelSize)
        Dim kernel = New cvb.Mat(kernelSize, kernelSize, cvb.MatType.CV_32F).SetTo(1 / (kernelSize * kernelSize))
        dst2 = src.Filter2D(-1, kernel)
        labels(2) = "Normalized KernelSize = " + CStr(kernelSize)
    End Sub
End Class








'https://www.cc.gatech.edu/classes/AY2015/cs4475_summer/documents/smoothing_separable.py
Public Class Filter_SepFilter2D : Inherits VB_Parent
    Dim options As New Options_SepFilter2D
    Public Sub New()
        labels(2) = "Gaussian Blur result"
        desc = "Apply kernel X then kernel Y with OpenCV's SepFilter2D and compare to Gaussian blur"
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        Options.RunVB()
        Dim kernel = cvb.Cv2.GetGaussianKernel(options.xDim, options.sigma)
        dst2 = src.GaussianBlur(New cvb.Size(options.xDim, options.yDim), options.sigma)
        dst3 = src.SepFilter2D(cvb.MatType.CV_8UC3, kernel, kernel)
        If options.diffCheck Then
            Dim graySep = dst3.CvtColor(cvb.ColorConversionCodes.BGR2Gray)
            Dim grayGauss = dst2.CvtColor(cvb.ColorConversionCodes.BGR2Gray)
            dst3 = (graySep - grayGauss).ToMat.Threshold(0, 255, cvb.ThresholdTypes.Binary)
            labels(3) = "Gaussian - SepFilter2D " + CStr(dst3.CountNonZero) + " pixels different."
        Else
            labels(3) = "SepFilter2D Result"
        End If
    End Sub
End Class






' https://datamahadev.com/filters-in-image-processing-using-opencv/
Public Class Filter_Minimum : Inherits VB_Parent
    Dim options As New Options_Filter
    Public Sub New()
        desc = "Implement the Minimum Filter - use minimum value in kernel"
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        options.RunVB()
        Dim kernelSize As Integer = If(standaloneTest(), (task.frameCount Mod 20) + 1, options.kernelSize)
        Dim element = cvb.Cv2.GetStructuringElement(cvb.MorphShapes.Rect, New cvb.Size(kernelSize, kernelSize))
        dst2 = src.Erode(element)
    End Sub
End Class






' https://datamahadev.com/filters-in-image-processing-using-opencv/
Public Class Filter_Maximum : Inherits VB_Parent
    Dim options As New Options_Filter
    Public Sub New()
        desc = "Implement the Maximum Filter - use maximum value in kernel"
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        options.RunVB()
        Dim kernelSize As Integer = If(standaloneTest(), (task.frameCount Mod 20) + 1, options.kernelSize)
        Dim element = cvb.Cv2.GetStructuringElement(cvb.MorphShapes.Rect, New cvb.Size(kernelSize, kernelSize))
        dst2 = src.Dilate(element)
    End Sub
End Class






' https://datamahadev.com/filters-in-image-processing-using-opencv/
Public Class Filter_Mean : Inherits VB_Parent
    Dim options As New Options_Filter
    Public Sub New()
        desc = "Implement the Mean Filter - use mean value in kernel"
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        options.RunVB()
        Dim kernelSize As Integer = If(standaloneTest(), (task.frameCount Mod 20) + 1, options.kernelSize)
        Dim kernel = (cvb.Mat.Ones(cvb.MatType.CV_32FC1, kernelSize, kernelSize) / (kernelSize * kernelSize)).ToMat
        dst2 = src.Filter2D(-1, kernel)
    End Sub
End Class






' https://datamahadev.com/filters-in-image-processing-using-opencv/
Public Class Filter_Median : Inherits VB_Parent
    Dim options As New Options_Filter
    Public Sub New()
        desc = "Implement the Median Filter - use median value in kernel"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()
        Dim kernelSize As Integer = If(standaloneTest(), (task.frameCount Mod 20) + 1, options.kernelSize)
        If kernelSize Mod 2 = 0 Then kernelSize += 1
        dst2 = src.MedianBlur(kernelSize)
    End Sub
End Class
