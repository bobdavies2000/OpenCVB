Imports cv = OpenCvSharp
' https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Laplacian_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Laplacian Kernel size", 1, 21, 3)
            sliders.setupTrackBar(1, "Laplacian Scale", 0, 100, 100)
            sliders.setupTrackBar(2, "Laplacian Delta", 0, 1000, 0)
        End If
        task.desc = "Laplacian filter - the second derivative."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim kernelSize = sliders.trackbar(0).Value()
        If kernelSize Mod 2 = 0 Then kernelSize += 1
        Dim scale = sliders.trackbar(1).Value / 100
        Dim delta = sliders.trackbar(2).Value / 100
        Dim ddepth = cv.MatType.CV_16S

        If standalone or task.intermediateReview = caller Then src = src.GaussianBlur(New cv.Size(kernelSize, kernelSize), 0, 0)
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = gray.Laplacian(ddepth, kernelSize, scale, delta).ConvertScaleAbs()
        label1 = "Laplacian Filter k = " + CStr(kernelSize)
    End Sub
End Class





' https://docs.opencv.org/3.2.0/de/db2/laplace_8cpp-example.html
Public Class Laplacian_Blur
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Laplacian Kernel size", 1, 21, 3)
            sliders.setupTrackBar(1, "Laplacian Scale", 0, 100, 100)
            sliders.setupTrackBar(2, "Laplacian Delta", 0, 1000, 0)
        End If

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Add Gaussian Blur"
            radio.check(1).Text = "Add boxfilter Blur"
            radio.check(2).Text = "Add median Blur"
            radio.check(0).Checked = True
        End If
        task.desc = "Laplacian filter - the second derivative - with different bluring techniques"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim kernelSize = sliders.trackbar(0).Value()
        If kernelSize Mod 2 = 0 Then kernelSize += 1
        Dim scale = sliders.trackbar(1).Value / 100
        Dim delta = sliders.trackbar(2).Value / 100
        Dim ddepth = cv.MatType.CV_16S

        Dim blurText As String
        If radio.check(0).Checked Then
            src = src.GaussianBlur(New cv.Size(kernelSize, kernelSize), 0, 0)
            blurText = "Gaussian"
        ElseIf radio.check(1).Checked Then
            src = src.Blur(New cv.Size(kernelSize, kernelSize))
            blurText = "boxfilter"
        Else
            src = src.MedianBlur(kernelSize)
            blurText = "MedianBlur"
        End If
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src.Laplacian(ddepth, kernelSize, scale, delta).ConvertScaleAbs()
        label1 = "Laplacian+" + blurText + " k = " + CStr(kernelSize)
    End Sub
End Class






' http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.54.299
Public Class Laplacian_PyramidFilter
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 6)
            sliders.setupTrackBar(0, "Sharpest", 0, 10, 1)
            sliders.setupTrackBar(1, "blurryMin", 0, 10, 1)
            sliders.setupTrackBar(2, "blurryMed1", 0, 10, 1)
            sliders.setupTrackBar(3, "blurryMed2", 0, 10, 1)
            sliders.setupTrackBar(4, "blurryMax", 0, 10, 1)
            sliders.setupTrackBar(5, "Saturate", 0, 10, 1)
        End If
        task.desc = "VB.Net version of the Laplacian Pyramid Filter - see reference."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim levelMat(sliders.trackbar.Length - 1) As cv.Mat
        Dim img As New cv.Mat
        src.ConvertTo(img, cv.MatType.CV_32F)
        For i = 0 To sliders.trackbar.Length - 2
            Dim nextImg = img.PyrDown()
            levelMat(i) = (img - nextImg.PyrUp(img.Size)) * sliders.trackbar(i).Value
            img = nextImg
        Next
        levelMat(sliders.trackbar.Length - 1) = img * sliders.trackbar(sliders.trackbar.Length - 1).Value

        img = levelMat(sliders.trackbar.Length - 1)
        For i = sliders.trackbar.Length - 1 To 1 Step -1
            img = img.PyrUp(levelMat(i - 1).Size)
            img += levelMat(i - 1)
        Next
        img.ConvertTo(dst1, cv.MatType.CV_8UC3)
    End Sub
End Class
