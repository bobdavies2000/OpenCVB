Imports cv = OpenCvSharp
' https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Laplacian_Basics : Inherits VB_Algorithm
    Dim options As New Options_Laplacian
    Dim erode As New Erode_Basics
    Dim dilate As New Dilate_Basics
    Public Sub New()
        desc = "Laplacian filter - the second derivative."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        If standalone Then src = src.GaussianBlur(options.kernel, 0, 0)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst3 = src.Laplacian(cv.MatType.CV_16S, options.kernel.Width, options.scale, options.delta).ConvertScaleAbs()

        erode.Run(dst3.Threshold(options.threshold, 255, cv.ThresholdTypes.Binary))
        dilate.Run(erode.dst2)
        dst2 = dilate.dst2

        labels(2) = "Laplacian Filter k = " + CStr(options.kernel.Width)
        labels(3) = "Laplacian after " + CStr(erode.options.iterations) + " erode iterations and " + CStr(dilate.options.iterations) + " dilate iterations"
    End Sub
End Class





' https://docs.opencv.org/3.2.0/de/db2/laplace_8cpp-example.html
Public Class Laplacian_Blur : Inherits VB_Algorithm
    Dim options As New Options_Laplacian
    Public Sub New()
        desc = "Laplacian filter - the second derivative - with different bluring techniques"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

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
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = src.Laplacian(cv.MatType.CV_16S, options.kernel.Width, options.scale, options.delta).ConvertScaleAbs()
        labels(2) = "Laplacian+" + blurText + " k = " + CStr(options.kernel.Width)
    End Sub
End Class






' http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.54.299
Public Class Laplacian_PyramidFilter : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Sharpest", 0, 10, 1)
            sliders.setupTrackBar("blurryMin", 0, 10, 1)
            sliders.setupTrackBar("blurryMed1", 0, 10, 1)
            sliders.setupTrackBar("blurryMed2", 0, 10, 1)
            sliders.setupTrackBar("blurryMax", 0, 10, 1)
            sliders.setupTrackBar("Saturate", 0, 10, 1)
        End If
        desc = "VB.Net version of the Laplacian Pyramid Filter - see reference."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        ' this usage of sliders.trackbar(x) is OK as long as this algorithm is not reused in multiple places (which it isn't)
        Dim levelMat(sliders.trackbar.Count - 1) As cv.Mat
        Dim img As New cv.Mat
        src.ConvertTo(img, cv.MatType.CV_32F)
        For i = 0 To sliders.trackbar.Count - 2
            Dim nextImg = img.PyrDown()
            levelMat(i) = (img - nextImg.PyrUp(img.Size)) * sliders.trackbar(i).Value
            img = nextImg
        Next
        levelMat(sliders.trackbar.Count - 1) = img * sliders.trackbar(sliders.trackbar.Count - 1).Value

        img = levelMat(sliders.trackbar.Count - 1)
        For i = sliders.trackbar.Count - 1 To 1 Step -1
            img = img.PyrUp(levelMat(i - 1).Size)
            img += levelMat(i - 1)
        Next
        img.ConvertTo(dst2, cv.MatType.CV_8UC3)
    End Sub
End Class