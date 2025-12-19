Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Filter_Basics : Inherits TaskParent
        Public filterList As String() = {"Original", "PhotoShop_HSV", "PhotoShop_SharpenDetail", "PhotoShop_WhiteBalance"
                                         }
        Dim filters(filterList.Count - 1) As Object
        Public grayFilter As New Filter_BasicsGray
        Public Sub New()
            desc = "Filter the input for algorithm or set the defaults."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim filterIndex As Integer
            For Each cb In taskAlg.featureOptions.colorCheckbox
                If cb.Checked Then
                    Select Case cb.Text
                        Case "Original"
                            dst2 = taskAlg.color
                        Case "PhotoShop_WhiteBalance"
                            If filters(cb.Tag) Is Nothing Then filters(cb.Tag) = New PhotoShop_WhiteBalance
                        Case "PhotoShop_SharpenDetail"
                            If filters(cb.Tag) Is Nothing Then filters(cb.Tag) = New PhotoShop_SharpenDetail
                        Case "PhotoShop_HSV"
                            If filters(cb.Tag) Is Nothing Then filters(cb.Tag) = New PhotoShop_HSV
                    End Select
                    filterIndex = cb.Tag
                End If
            Next

            labels(2) = "Color input to all algorithms - " + taskAlg.featureOptions.colorCheckbox(filterIndex).Text
            If filterIndex > 0 Then
                filters(filterIndex).run(dst2)
                dst2 = filters(filterIndex).dst2
            End If

            grayFilter.Run(dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            labels(3) = grayFilter.labels(2)
            dst3 = grayFilter.dst2

            taskAlg.color = dst2
            taskAlg.gray = dst3
        End Sub
    End Class





    Public Class Filter_BasicsGray : Inherits TaskParent
        Public filterIndex As Integer = -1
        Public filterList As String() = {"Original", "Blur_Basics", "Brightness_Basics", "Contrast_Basics",
                                     "Dilate_Basics", "Erode_Basics", "Filter_Equalize", "Filter_Laplacian",
                                     "MeanSubtraction_Gray", "PhotoShop_Gamma"}
        Dim filters(filterList.Count - 1) As Object
        Public Sub New()
            desc = "Demo the RGB Filters selected in 'FeatureOptions'.  If none selected, just the input is displayed."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src
            For Each cb In taskAlg.featureOptions.grayCheckbox
                If cb.Checked Then
                    Select Case cb.Text
                        Case "Original"
                        Case "Blur_Basics"
                            If filters(cb.Tag) Is Nothing Then filters(cb.Tag) = New Blur_Basics
                        Case "Brightness_Basics"
                            If filters(cb.Tag) Is Nothing Then filters(cb.Tag) = New Brightness_Basics
                        Case "Contrast_Basics"
                            If filters(cb.Tag) Is Nothing Then filters(cb.Tag) = New Contrast_Basics
                        Case "Dilate_Basics"
                            If filters(cb.Tag) Is Nothing Then filters(cb.Tag) = New Dilate_Basics
                        Case "Erode_Basics"
                            If filters(cb.Tag) Is Nothing Then filters(cb.Tag) = New Erode_Basics
                        Case "Filter_Equalize"
                            If filters(cb.Tag) Is Nothing Then filters(cb.Tag) = New Filter_Equalize
                        Case "Filter_Laplacian"
                            If filters(cb.Tag) Is Nothing Then filters(cb.Tag) = New Filter_Laplacian
                        Case "MeanSubtraction_Gray"
                            If filters(cb.Tag) Is Nothing Then filters(cb.Tag) = New MeanSubtraction_Gray
                        Case "PhotoShop_Gamma"
                            If filters(cb.Tag) Is Nothing Then filters(cb.Tag) = New PhotoShop_Gamma
                    End Select
                    filterIndex = cb.Tag
                End If
            Next

            labels(2) = "Grayscale input to all algorithms - " + taskAlg.featureOptions.grayCheckbox(filterIndex).Text
            If filterIndex > 0 Then
                filters(filterIndex).run(dst2)
                dst2 = filters(filterIndex).dst2
                If dst2.Channels <> 1 Then
                    MessageBox.Show("Filter_BasicsGray failure - " + filterList(filterIndex) + " needs to return " + vbCrLf +
                       "an 8UC1 image, not 8UC3.  Reevaluate any new filters added above!")
                    Dim k = 0 ' if you set a breakpoint here when you get this message, you can debug it more easily.
                End If
            End If
        End Sub
    End Class





    ' https://docs.opencvb.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
    Public Class Filter_Laplacian : Inherits TaskParent
        Public Sub New()
            labels(2) = "Sharpened image using Filter2D output"
            labels(3) = "Output of Filter2D (approximated Laplacian)"
            desc = "Use a filter to approximate the Laplacian derivative."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim imgLaplacian = src.Filter2D(cv.MatType.CV_32F,
                                        cv.Mat.FromPixelData(3, 3, cv.MatType.CV_32FC1, New Single() {1, 1, 1, 1, -8, 1, 1, 1, 1}))
            src.ConvertTo(dst1, cv.MatType.CV_32F)
            dst0 = (dst1 - imgLaplacian).ToMat
            dst0.ConvertTo(dst2, src.Type)
            imgLaplacian.ConvertTo(dst3, src.Type)
        End Sub
    End Class







    Public Class Filter_NormalizedKernel : Inherits TaskParent
        Dim options As New Options_FilterNorm
        Public Sub New()
            desc = "Create a normalized kernel and use it."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Options.Run()

            Dim sum As Double
            For i = 0 To options.kernel.Width - 1
                sum += Math.Abs(options.kernel.Get(Of Single)(0, i))
            Next
            labels(2) = "kernel sum = " + Format(sum, fmt3)

            Dim dst32f = src.Filter2D(cv.MatType.CV_32FC1, options.kernel, anchor:=New cv.Point(0, 0))
            dst32f.ConvertTo(dst2, cv.MatType.CV_8UC3)
        End Sub
    End Class






    ' https://docs.opencvb.org/2.4/doc/tutorials/imgproc/imgtrans/filter_2d/filter_2d.html
    Public Class Filter_Normalized2D : Inherits TaskParent
        Dim options As New Options_Filter
        Public Sub New()
            desc = "Create and apply a normalized kernel."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            Dim kernelSize As Integer = If(standaloneTest(), (taskAlg.frameCount Mod 20) + 1, options.kernelSize)
            Dim kernel = New cv.Mat(kernelSize, kernelSize, cv.MatType.CV_32F).SetTo(1 / (kernelSize * kernelSize))
            dst2 = src.Filter2D(-1, kernel)
            labels(2) = "Normalized KernelSize = " + CStr(kernelSize)
        End Sub
    End Class








    'https://www.cc.gatech.edu/classes/AY2015/cs4475_summer/documents/smoothing_separable.py
    Public Class Filter_SepFilter2D : Inherits TaskParent
        Dim options As New Options_SepFilter2D
        Public Sub New()
            labels(2) = "Gaussian Blur result"
            desc = "Apply kernel X then kernel Y with OpenCV's SepFilter2D and compare to Gaussian blur"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Options.Run()
            Dim kernel = cv.Cv2.GetGaussianKernel(options.xDim, options.sigma)
            dst2 = src.GaussianBlur(New cv.Size(options.xDim, options.yDim), options.sigma)
            dst3 = src.SepFilter2D(cv.MatType.CV_8UC3, kernel, kernel)
            If options.diffCheck Then
                Dim graySep = dst3.CvtColor(cv.ColorConversionCodes.BGR2Gray)
                Dim grayGauss = dst2.CvtColor(cv.ColorConversionCodes.BGR2Gray)
                dst3 = (graySep - grayGauss).ToMat.Threshold(0, 255, cv.ThresholdTypes.Binary)
                labels(3) = "Gaussian - SepFilter2D " + CStr(dst3.CountNonZero) + " pixels different."
            Else
                labels(3) = "SepFilter2D Result"
            End If
        End Sub
    End Class






    ' https://datamahadev.com/filters-in-image-processing-using-opencv/
    Public Class Filter_Minimum : Inherits TaskParent
        Dim options As New Options_Filter
        Public Sub New()
            desc = "Implement the Minimum Filter - use minimum value in kernel"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            Dim kernelSize As Integer = If(standaloneTest(), (taskAlg.frameCount Mod 20) + 1, options.kernelSize)
            Dim element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(kernelSize, kernelSize))
            dst2 = src.Erode(element)
        End Sub
    End Class






    ' https://datamahadev.com/filters-in-image-processing-using-opencv/
    Public Class Filter_Maximum : Inherits TaskParent
        Dim options As New Options_Filter
        Public Sub New()
            desc = "Implement the Maximum Filter - use maximum value in kernel"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            Dim kernelSize As Integer = If(standaloneTest(), (taskAlg.frameCount Mod 20) + 1, options.kernelSize)
            Dim element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(kernelSize, kernelSize))
            dst2 = src.Dilate(element)
        End Sub
    End Class






    ' https://datamahadev.com/filters-in-image-processing-using-opencv/
    Public Class Filter_Mean : Inherits TaskParent
        Dim options As New Options_Filter
        Public Sub New()
            desc = "Implement the Mean Filter - use mean value in kernel"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            Dim kernelSize As Integer = If(standaloneTest(), (taskAlg.frameCount Mod 20) + 1, options.kernelSize)
            Dim kernel = (cv.Mat.Ones(cv.MatType.CV_32FC1, kernelSize, kernelSize) / (kernelSize * kernelSize)).ToMat
            dst2 = src.Filter2D(-1, kernel)
        End Sub
    End Class






    ' https://datamahadev.com/filters-in-image-processing-using-opencv/
    Public Class Filter_Median : Inherits TaskParent
        Dim options As New Options_Filter
        Public Sub New()
            desc = "Implement the Median Filter - use median value in kernel"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            Dim kernelSize As Integer = If(standaloneTest(), (taskAlg.frameCount Mod 20) + 1, options.kernelSize)
            If kernelSize Mod 2 = 0 Then kernelSize += 1
            dst2 = src.MedianBlur(kernelSize)
        End Sub
    End Class













    'https://docs.opencvb.org/master/d1/db7/tutorial_py_Hist_begins.html
    Public Class Filter_Equalize : Inherits TaskParent
        Public Sub New()
            labels(2) = "Equalized image"
            desc = "Create an equalized image of the grayscale input."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels = 1 Then cv.Cv2.EqualizeHist(src, dst2) Else cv.Cv2.EqualizeHist(taskAlg.grayStable, dst2)
        End Sub
    End Class
End Namespace