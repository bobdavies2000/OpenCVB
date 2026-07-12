Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
' https://docs.opencvb.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Laplacian_Basics : Inherits TaskParent
    Dim options As New Options_Laplacian
    Dim erode As New Erode_Basics
    Dim dilate As New Dilate_Basics
    Public Sub New()
        desc = "Laplacian filter - the second derivative."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If standaloneTest() Then GaussianBlur(src, src, options.kernel, 0, 0)
        If src.Channels() <> 1 Then src = task.gray
        Laplacian(src, dst3, cv.MatType.CV_16S, options.kernel.Width, options.scale, options.delta)
        Dim tmp As New cv.Mat
        Threshold(dst3, tmp, options.threshold, 255, cv.ThresholdTypes.Binary)
        erode.Run(tmp)
        dilate.Run(erode.dst2)
        dst2 = dilate.dst2

        labels(2) = "Laplacian Filter k = " + CStr(options.kernel.Width)
        labels(3) = "Laplacian after " + CStr(erode.options.iterations) + " erode iterations and " + CStr(dilate.options.iterations) + " dilate iterations"
    End Sub
End Class





' https://docs.opencvb.org/3.2.0/de/db2/laplace_8cpp-example.html
Public Class XR_Laplacian_Blur : Inherits TaskParent
    Dim options As New Options_Laplacian
    Public Sub New()
        desc = "Laplacian filter - the second derivative - with different bluring techniques"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim blurText As String
        If options.gaussianBlur Then
            GaussianBlur(src, src, options.kernel, 0, 0)
            blurText = "Gaussian"
        ElseIf options.boxFilterBlur Then
            Blur(src, src, options.kernel)
            blurText = "boxfilter"
        Else
            MedianBlur(src, src, options.kernel.Width)
            blurText = "MedianBlur"
        End If
        If src.Channels() <> 1 Then src = task.gray
        Laplacian(src, dst2, cv.MatType.CV_16S, options.kernel.Width, options.scale, options.delta)
        ConvertScaleAbs(dst2, dst2)
        labels(2) = "Laplacian+" + blurText + " k = " + CStr(options.kernel.Width)
    End Sub
End Class






Public Class Laplacian_PyramidFilter : Inherits TaskParent
    Dim options As New Options_LaPlacianPyramid
    Public Sub New()
        desc = "VB.Net version of the Laplacian Pyramid Filter - see http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.54.299."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        src.ConvertTo(options.img, cv.MatType.CV_32F)
        options.Run()
        options.img.ConvertTo(dst2, cv.MatType.CV_8UC3)
    End Sub
End Class
