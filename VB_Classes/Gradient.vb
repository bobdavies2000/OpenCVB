Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Gradient_Basics
    Inherits VBparent
    Public sobel As Edges_Sobel
    Public Sub New()
        initParent()
        sobel = New Edges_Sobel()
        label2 = "Phase Output"
        task.desc = "Use phase to compute gradient"
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        sobel.src = src
        sobel.Run()
        Dim x32f As New cv.Mat, y32f As New cv.Mat
        sobel.grayX.ConvertTo(x32f, cv.MatType.CV_32F)
        sobel.grayY.ConvertTo(y32f, cv.MatType.CV_32F)
        cv.Cv2.Phase(x32f, y32f, dst2)
        dst1 = sobel.dst1
    End Sub
End Class




Public Class Gradient_Depth
    Inherits VBparent
    Dim sobel As Edges_Sobel
    Public Sub New()
        initParent()
        sobel = New Edges_Sobel()
        task.desc = "Use phase to compute gradient on depth image"
		' task.rank = 1
        label2 = "Phase Output"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        sobel.src = task.depth32f
        sobel.Run()
        Dim x32f As New cv.Mat, y32f As New cv.Mat
        sobel.grayX.ConvertTo(x32f, cv.MatType.CV_32F)
        sobel.grayY.ConvertTo(y32f, cv.MatType.CV_32F)
        cv.Cv2.Phase(x32f, y32f, dst2)
        dst1 = sobel.dst1
    End Sub
End Class







' https://github.com/anopara/genetic-drawing
Public Class Gradient_CartToPolar
    Inherits VBparent
    Public basics As Gradient_Basics
    Public magnitude As New cv.Mat
    Public angle As New cv.Mat
    Public Sub New()
        initParent()
        basics = New Gradient_Basics()

        Static ksizeSlider = findSlider("Sobel kernel Size")
        ksizeSlider.value = 1

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Contrast exponent to use X100", 0, 200, 30)
        End If
        label1 = "CartToPolar Magnitude Output Normalized"
        label2 = "CartToPolar Angle Output"
        task.desc = "Compute the gradient and use CartToPolar to image the magnitude and angle"
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        src.ConvertTo(basics.src, cv.MatType.CV_32FC3, 1 / 255)
        basics.Run()

        basics.sobel.grayX.ConvertTo(dst1, cv.MatType.CV_32F)
        basics.sobel.grayX.ConvertTo(dst2, cv.MatType.CV_32F)

        cv.Cv2.CartToPolar(dst1, dst2, magnitude, angle, True)
        magnitude = magnitude.Normalize()
        Static contrastSlider = findSlider("Contrast exponent to use X100")
        Dim exponent = contrastSlider.Value / 100
        magnitude = magnitude.Pow(exponent)

        dst1 = magnitude
    End Sub
End Class










Public Class Gradient_StableDepth
    Inherits VBparent
    Dim motionSD As Motion_MinMaxDepth
    Dim basics As Gradient_Basics
    Public Sub New()
        initParent()
        motionSD = New Motion_MinMaxDepth
        basics = New Gradient_Basics
        label1 = "Stable depth input to Gradient"
        label2 = "Phase component of the gradient output"
        task.desc = "Use the stable depth as input to get a map of the phase of the gradient in the depth data."
		' task.rank = 1
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        motionSD.src = src
        motionSD.Run()
        dst1 = motionSD.dst1.Clone

        basics.src = dst1.Clone
        basics.Run()
        dst2 = basics.dst2
    End Sub
End Class
