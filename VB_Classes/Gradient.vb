Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Gradient_Basics : Inherits VBparent
    Public sobel As New Edges_Sobel
    Public Sub New()
        label2 = "Phase Output"
        task.desc = "Use phase to compute gradient"
    End Sub
    Public Sub Run(src As cv.Mat)
        sobel.Run(src)
        Dim x32f As New cv.Mat, y32f As New cv.Mat
        sobel.grayX.ConvertTo(x32f, cv.MatType.CV_32F)
        sobel.grayY.ConvertTo(y32f, cv.MatType.CV_32F)
        cv.Cv2.Phase(x32f, y32f, dst2)
        dst1 = sobel.dst1
    End Sub
End Class




Public Class Gradient_Depth : Inherits VBparent
    Dim sobel As New Edges_Sobel
    Public Sub New()
        task.desc = "Use phase to compute gradient on depth image"
        label2 = "Phase Output"
    End Sub
    Public Sub Run(src as cv.Mat)
        sobel.Run(task.depth32f)
        Dim x32f As New cv.Mat, y32f As New cv.Mat
        sobel.grayX.ConvertTo(x32f, cv.MatType.CV_32F)
        sobel.grayY.ConvertTo(y32f, cv.MatType.CV_32F)
        cv.Cv2.Phase(x32f, y32f, dst2)
        dst1 = sobel.dst1
    End Sub
End Class







' https://github.com/anopara/genetic-drawing
Public Class Gradient_CartToPolar : Inherits VBparent
    Public basics As Gradient_Basics
    Public magnitude As New cv.Mat
    Public angle As New cv.Mat
    Public Sub New()
        basics = New Gradient_Basics()

        findSlider("Sobel kernel Size").Value = 1

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Contrast exponent to use X100", 0, 200, 30)
        End If
        label1 = "CartToPolar Magnitude Output Normalized"
        label2 = "CartToPolar Angle Output"
        task.desc = "Compute the gradient and use CartToPolar to image the magnitude and angle"
    End Sub
    Public Sub Run(src as cv.Mat)
        Static contrastSlider = findSlider("Contrast exponent to use X100")
        Dim tmp As New cv.Mat
        src.ConvertTo(tmp, cv.MatType.CV_32FC3, 1 / 255)
        basics.Run(tmp)

        basics.sobel.grayX.ConvertTo(dst1, cv.MatType.CV_32F)
        basics.sobel.grayX.ConvertTo(dst2, cv.MatType.CV_32F)

        cv.Cv2.CartToPolar(dst1, dst2, magnitude, angle, True)
        magnitude = magnitude.Normalize()
        Dim exponent = contrastSlider.Value / 100
        magnitude = magnitude.Pow(exponent)

        dst1 = magnitude
    End Sub
End Class










Public Class Gradient_StableDepth : Inherits VBparent
    Dim motionSD As Motion_MinMaxDepth
    Dim basics As Gradient_Basics
    Public Sub New()
        motionSD = New Motion_MinMaxDepth
        basics = New Gradient_Basics
        label1 = "Stable depth input to Gradient"
        label2 = "Phase component of the gradient output"
        task.desc = "Use the stable depth as input to get a map of the phase of the gradient in the depth data."
    End Sub
    Public Sub Run(src as cv.Mat)

        motionSD.Run(src)
        dst1 = motionSD.dst1.Clone

        basics.Run(dst1.Clone)
        dst2 = basics.dst2
    End Sub
End Class
