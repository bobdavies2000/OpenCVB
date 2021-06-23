Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Gradient_Basics : Inherits VBparent
    Public sobel As New Edges_Sobel
    Public Sub New()
        labels(2) = "Gradient_Basics - Sobel output"
        labels(3) = "Phase Output"
        task.desc = "Use phase to compute gradient"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        sobel.Run(src)
        Dim x32f As New cv.Mat, y32f As New cv.Mat
        sobel.grayX.ConvertTo(x32f, cv.MatType.CV_32F)
        sobel.grayY.ConvertTo(y32f, cv.MatType.CV_32F)
        cv.Cv2.Phase(x32f, y32f, dst3)
        dst2 = sobel.dst2
    End Sub
End Class




Public Class Gradient_Depth : Inherits VBparent
    Dim sobel As New Edges_Sobel
    Public Sub New()
        task.desc = "Use phase to compute gradient on depth image"
        labels(3) = "Phase Output"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        sobel.Run(task.depth32f)
        Dim x32f As New cv.Mat, y32f As New cv.Mat
        sobel.grayX.ConvertTo(x32f, cv.MatType.CV_32F)
        sobel.grayY.ConvertTo(y32f, cv.MatType.CV_32F)
        cv.Cv2.Phase(x32f, y32f, dst3)
        dst2 = sobel.dst2
    End Sub
End Class







' https://github.com/anopara/genetic-drawing
Public Class Gradient_CartToPolar : Inherits VBparent
    Public basics As New Gradient_Basics
    Public magnitude As New cv.Mat
    Public angle As New cv.Mat
    Public Sub New()
        findSlider("Sobel kernel Size").Value = 1

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Contrast exponent to use X100", 0, 200, 30)
        End If
        labels(2) = "CartToPolar Magnitude Output Normalized"
        labels(3) = "CartToPolar Angle Output"
        task.desc = "Compute the gradient and use CartToPolar to image the magnitude and angle"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static contrastSlider = findSlider("Contrast exponent to use X100")
        Dim tmp As New cv.Mat
        src.ConvertTo(tmp, cv.MatType.CV_32FC3, 1 / 255)
        basics.Run(tmp)

        basics.sobel.grayX.ConvertTo(dst2, cv.MatType.CV_32F)
        basics.sobel.grayX.ConvertTo(dst3, cv.MatType.CV_32F)

        cv.Cv2.CartToPolar(dst2, dst3, magnitude, angle, True)
        magnitude = magnitude.Normalize()
        Dim exponent = contrastSlider.Value / 100
        magnitude = magnitude.Pow(exponent)

        dst2 = magnitude
    End Sub
End Class










Public Class Gradient_StableDepth : Inherits VBparent
    Dim motionSD As New Motion_MinMaxDepth
    Dim basics As New Gradient_Basics
    Public Sub New()
        labels(2) = "Stable depth input to Gradient"
        labels(3) = "Phase component of the gradient output"
        task.desc = "Use the stable depth as input to get a map of the phase of the gradient in the depth data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        motionSD.Run(src)
        dst2 = motionSD.dst2.Clone
        basics.Run(dst2.Clone)
        dst3 = basics.dst3
    End Sub
End Class
