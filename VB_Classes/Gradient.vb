Imports cvb = OpenCvSharp
Public Class Gradient_Basics : Inherits VB_Parent
    Public sobel As New Edge_Sobel
    Public Sub New()
        dst3 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_32F, cvb.Scalar.All(0))
        labels = {"", "", "Gradient_Basics - Sobel output", "Phase Output"}
        desc = "Use phase to compute gradient"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        sobel.Run(src)
        cvb.Cv2.Phase(sobel.dst0, sobel.dst1, dst3)
        dst2 = sobel.dst0
    End Sub
End Class




Public Class Gradient_Depth : Inherits VB_Parent
    Dim sobel As New Edge_Sobel
    Public Sub New()
        labels(3) = "Phase Output"
        desc = "Use phase to compute gradient on depth image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        sobel.Run(task.pcSplit(2))
        cvb.Cv2.Phase(sobel.dst0, sobel.dst1, dst3)
        dst2 = sobel.dst0
    End Sub
End Class







' https://github.com/anopara/genetic-drawing
Public Class Gradient_CartToPolar : Inherits VB_Parent
    Public basics As New Gradient_Basics
    Public magnitude As New cvb.Mat
    Public angle As New cvb.Mat
    Dim options As New Options_Gradient
    Public Sub New()
        FindSlider("Sobel kernel Size").Value = 1
        labels(2) = "CartToPolar Magnitude Output Normalized"
        labels(3) = "CartToPolar Angle Output"
        desc = "Compute the gradient and use CartToPolar to image the magnitude and angle"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim tmp As New cvb.Mat
        src.ConvertTo(tmp, cvb.MatType.CV_32FC3, 1 / 255)
        basics.Run(tmp)

        basics.sobel.dst2.ConvertTo(dst2, cvb.MatType.CV_32F)
        basics.sobel.dst2.ConvertTo(dst3, cvb.MatType.CV_32F)

        magnitude = New cvb.Mat
        angle = New cvb.Mat
        cvb.Cv2.CartToPolar(dst2, dst3, magnitude, angle, True)
        magnitude = magnitude.Normalize()
        magnitude = magnitude.Pow(options.exponent)

        dst2 = magnitude
    End Sub
End Class






Public Class Gradient_Color : Inherits VB_Parent
    Public color1 = cvb.Scalar.Blue
    Public color2 = cvb.Scalar.Yellow
    Public gradientWidth As Integer
    Public gradient As cvb.Mat
    Public Sub New()
        desc = "Provide a spectrum that is a gradient from one color to another."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        gradientWidth = dst2.Width
        Dim f As Double = 1.0
        Dim gradientColors As New cvb.Mat(1, gradientWidth, cvb.MatType.CV_64FC3)
        For i = 0 To gradientWidth - 1
            gradientColors.Set(Of cvb.Scalar)(0, i, New cvb.Scalar(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1),
                                                                 f * color2(2) + (1 - f) * color1(2)))
            f -= 1 / gradientWidth
        Next
        gradient = New cvb.Mat(1, gradientWidth, cvb.MatType.CV_8UC3)
        For i = 0 To gradientWidth - 1
            gradient.Col(i).SetTo(gradientColors.Get(Of cvb.Scalar)(0, i))
        Next
        dst2 = gradient.Resize(dst2.Size)
    End Sub
End Class
