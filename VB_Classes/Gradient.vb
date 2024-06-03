Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Gradient_Basics : Inherits VB_Parent
    Public sobel as new Edge_Sobel_Old
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        labels = {"", "", "Gradient_Basics - Sobel output", "Phase Output"}
        desc = "Use phase to compute gradient"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        sobel.Run(src)
        cv.Cv2.Phase(sobel.dst0, sobel.dst1, dst3)
        dst2 = sobel.dst0
    End Sub
End Class




Public Class Gradient_Depth : Inherits VB_Parent
    Dim sobel as new Edge_Sobel_Old
    Public Sub New()
        labels(3) = "Phase Output"
        desc = "Use phase to compute gradient on depth image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        sobel.Run(task.pcSplit(2))
        cv.Cv2.Phase(sobel.dst0, sobel.dst1, dst3)
        dst2 = sobel.dst0
    End Sub
End Class







' https://github.com/anopara/genetic-drawing
Public Class Gradient_CartToPolar : Inherits VB_Parent
    Public basics As New Gradient_Basics
    Public magnitude As New cv.Mat
    Public angle As New cv.Mat
    Public Sub New()
        findSlider("Sobel kernel Size").Value = 1
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Contrast exponent to use X100", 0, 200, 30)
        labels(2) = "CartToPolar Magnitude Output Normalized"
        labels(3) = "CartToPolar Angle Output"
        desc = "Compute the gradient and use CartToPolar to image the magnitude and angle"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static contrastSlider = findSlider("Contrast exponent to use X100")
        Dim tmp As New cv.Mat
        src.ConvertTo(tmp, cv.MatType.CV_32FC3, 1 / 255)
        basics.Run(tmp)

        basics.sobel.dst2.ConvertTo(dst2, cv.MatType.CV_32F)
        basics.sobel.dst2.ConvertTo(dst3, cv.MatType.CV_32F)

        magnitude = New cv.Mat
        angle = New cv.Mat
        cv.Cv2.CartToPolar(dst2, dst3, magnitude, angle, True)
        magnitude = magnitude.Normalize()
        Dim exponent = contrastSlider.Value / 100
        magnitude = magnitude.Pow(exponent)

        dst2 = magnitude
    End Sub
End Class






Public Class Gradient_Color : Inherits VB_Parent
    Public color1 = cv.Scalar.Blue
    Public color2 = cv.Scalar.Yellow
    Public gradientWidth As Integer
    Public gradient As cv.Mat
    Public Sub New()
        desc = "Provide a spectrum that is a gradient from one color to another."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        gradientWidth = dst2.Width
        Dim f As Double = 1.0
        Dim gradientColors As New cv.Mat(1, gradientWidth, cv.MatType.CV_64FC3)
        For i = 0 To gradientWidth - 1
            gradientColors.Set(Of cv.Scalar)(0, i, New cv.Scalar(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1),
                                                                 f * color2(2) + (1 - f) * color1(2)))
            f -= 1 / gradientWidth
        Next
        gradient = New cv.Mat(1, gradientWidth, cv.MatType.CV_8UC3)
        For i = 0 To gradientWidth - 1
            gradient.Col(i).SetTo(gradientColors.Get(Of cv.Scalar)(0, i))
        Next
        dst2 = gradient.Resize(dst2.Size)
    End Sub
End Class
