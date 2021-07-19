Imports cv = OpenCvSharp
' https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Filter_Laplacian : Inherits VBparent
    Public Sub New()
        task.desc = "Use a filter to approximate the Laplacian derivative."
        labels(2) = "Sharpened image using Filter2D output"
        labels(3) = "Output of Filter2D (approximated Laplacian)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim imgLaplacian = src.Filter2D(cv.MatType.CV_32F, New cv.Mat(3, 3, cv.MatType.CV_32FC1, New Single() {1, 1, 1, 1, -8, 1, 1, 1, 1}))
        src.ConvertTo(dst1, cv.MatType.CV_32F)
        dst0 = (dst1 - imgLaplacian).ToMat
        dst0.ConvertTo(dst2, src.Type)
        imgLaplacian.ConvertTo(dst3, src.type)
    End Sub
End Class







Public Class Filter_NormalizedKernel : Inherits VBparent
    Public Sub New()
        If radio.Setup(caller, 4) Then
            radio.check(0).Text = "INF"
            radio.check(1).Text = "L1"
            radio.check(1).Checked = True
            radio.check(2).Text = "L2"
            radio.check(3).Text = "MinMax"
        End If
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Normalize alpha X10", 1, 100, 10)
        End If
        task.desc = "Create a normalized kernel and use it."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim kernel = New cv.Mat(1, 21, cv.MatType.CV_32FC1, New Single() {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1})
        Dim normType = cv.NormTypes.L1
        Static frm = findfrm(caller + " Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                normType = Choose(i + 1, cv.NormTypes.INF, cv.NormTypes.L1, cv.NormTypes.L2, cv.NormTypes.MinMax)
                Exit For
            End If
        Next

        kernel = kernel.Normalize(sliders.trackbar(0).Value / 10, 0, normType)

        Dim sum As Double
        For i = 0 To kernel.Width - 1
            sum += Math.Abs(kernel.Get(Of Single)(0, i))
        Next
        labels(2) = "kernel sum = " + Format(sum, "#0.000")

        Dim dst32f = src.Filter2D(cv.MatType.CV_32FC1, kernel, anchor:=New cv.Point(0, 0))
        dst32f.ConvertTo(dst2, cv.MatType.CV_8UC3)
    End Sub
End Class






' https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/filter_2d/filter_2d.html
Public Class Filter_Normalized2D : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Filter Normalized 2D kernel size", 1, 21, 3)
        End If
        task.desc = "Create and apply a normalized kernel."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim kernelSize = If(standalone, (task.frameCount Mod 20) + 1, sliders.trackbar(0).Value)
        Dim kernel = New cv.Mat(kernelSize, kernelSize, cv.MatType.CV_32F).SetTo(1 / (kernelSize * kernelSize))
        dst2 = src.Filter2D(-1, kernel)
        labels(2) = "Normalized KernelSize = " + CStr(kernelSize)
    End Sub
End Class








'https://www.cc.gatech.edu/classes/AY2015/cs4475_summer/documents/smoothing_separable.py
Public Class Filter_SepFilter2D : Inherits VBparent
    Public Sub New()
        If check.Setup(caller, 1) Then
            check.Box(0).Text = "Show Difference SepFilter2D and Gaussian"
            check.Box(0).Checked = True
        End If

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Kernel X size", 1, 21, 5)
            sliders.setupTrackBar(1, "Kernel Y size", 1, 21, 11)
            sliders.setupTrackBar(2, "SepFilter2D Sigma X10", 0, 100, 17)
        End If
        labels(2) = "Gaussian Blur result"
        task.desc = "Apply kernel X then kernel Y with OpenCV's SepFilter2D and compare to Gaussian blur"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim xDim = If(sliders.trackbar(0).Value Mod 2, sliders.trackbar(0).Value, sliders.trackbar(0).Value + 1)
        Dim yDim = If(sliders.trackbar(1).Value Mod 2, sliders.trackbar(1).Value, sliders.trackbar(1).Value + 1)
        Dim sigma = sliders.trackbar(2).Value / 10
        Dim kernel = cv.Cv2.GetGaussianKernel(xDim, sigma)
        dst2 = src.GaussianBlur(New cv.Size(xDim, yDim), sigma)
        dst3 = src.SepFilter2D(cv.MatType.CV_8UC3, kernel, kernel)
        If check.Box(0).Checked Then
            Dim graySep = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim grayGauss = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst3 = (graySep - grayGauss).ToMat.Threshold(0, 255, cv.ThresholdTypes.Binary)
            labels(3) = "Gaussian - SepFilter2D " + CStr(dst3.CountNonZero()) + " pixels different."
        Else
            labels(3) = "SepFilter2D Result"
        End If
    End Sub
End Class


