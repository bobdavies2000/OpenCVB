Imports cv = OpenCvSharp
' https://docs.opencvb.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Laplacian_Basics : Inherits TaskParent
    Dim options As New Options_Laplacian
    Dim erode As New Erode_Basics
    Dim dilate As New Dilate_Basics
    Public Sub New()
        desc = "Laplacian filter - the second derivative."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Options.Run()
        If standaloneTest() Then src = src.GaussianBlur(options.kernel, 0, 0)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        dst3 = src.Laplacian(cv.MatType.CV_16S, options.kernel.Width, options.scale, options.delta).ConvertScaleAbs()

        erode.Run(dst3.Threshold(options.threshold, 255, cv.ThresholdTypes.Binary))
        dilate.Run(erode.dst2)
        dst2 = dilate.dst2

        labels(2) = "Laplacian Filter k = " + CStr(options.kernel.Width)
        labels(3) = "Laplacian after " + CStr(erode.options.iterations) + " erode iterations and " + CStr(dilate.options.iterations) + " dilate iterations"
    End Sub
End Class





' https://docs.opencvb.org/3.2.0/de/db2/laplace_8cpp-example.html
Public Class Laplacian_Blur : Inherits TaskParent
    Dim options As New Options_Laplacian
    Public Sub New()
        desc = "Laplacian filter - the second derivative - with different bluring techniques"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Options.Run()

        Dim blurText As String
        If options.gaussianBlur Then
            src = src.GaussianBlur(options.kernel, 0, 0)
            blurText = "Gaussian"
        ElseIf options.boxFilterBlur Then
            src = src.Blur(options.kernel)
            blurText = "boxfilter"
        Else
            src = src.MedianBlur(options.kernel.Width)
            blurText = "MedianBlur"
        End If
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        dst2 = src.Laplacian(cv.MatType.CV_16S, options.kernel.Width, options.scale, options.delta).ConvertScaleAbs()
        labels(2) = "Laplacian+" + blurText + " k = " + CStr(options.kernel.Width)
    End Sub
End Class






Public Class Laplacian_PyramidFilter : Inherits TaskParent
    Dim options As New Options_LaPlacianPyramid
    Public Sub New()
        desc = "VB.Net version of the Laplacian Pyramid Filter - see http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.54.299."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        src.ConvertTo(options.img, cv.MatType.CV_32F)
        options.Run()
        options.img.ConvertTo(dst2, cv.MatType.CV_8UC3)
    End Sub
End Class