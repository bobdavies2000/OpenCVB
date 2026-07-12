Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Gradient_Basics : Inherits TaskParent
    Public sobel As New Edge_SobelNaive
    Public Sub New()
        dst3 = New Mat(dst2.Size(), MatType.CV_32F, Scalar.All(0))
        labels = {"", "", "Gradient_Basics - Sobel output", "Phase Output"}
        desc = "Use phase to compute gradient"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sobel.Run(src)
        Phase(sobel.dst0, sobel.dst1, dst3)
        dst2 = sobel.dst0
    End Sub
End Class




Public Class Gradient_PhaseDepth : Inherits TaskParent
    Dim sobel As New Edge_SobelNaive
    Public Sub New()
        labels(3) = "Phase Output"
        desc = "Use phase to compute gradient on depth image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sobel.Run(task.pcSplit(2))
        Phase(sobel.dst0, sobel.dst1, dst3)
        dst2 = sobel.dst0
    End Sub
End Class







' https://github.com/anopara/genetic-drawing
Public Class Gradient_CartToPolar : Inherits TaskParent
    Public basics As New Gradient_Basics
    Public magnitude As New Mat
    Public angle As New Mat
    Dim options As New Options_Gradient
    Public Sub New()
        OptionParent.FindSlider("Sobel kernel Size").Value = 1
        labels(2) = "CartToPolar Magnitude Output Normalized"
        labels(3) = "CartToPolar Angle Output"
        desc = "Compute the gradient and use CartToPolar to image the magnitude and angle"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim tmp As New Mat
        src.ConvertTo(tmp, MatType.CV_32FC3, 1 / 255)
        basics.Run(tmp)

        basics.sobel.dst2.ConvertTo(dst2, MatType.CV_32F)
        basics.sobel.dst2.ConvertTo(dst3, MatType.CV_32F)

        magnitude = New Mat
        angle = New Mat
        CartToPolar(dst2, dst3, magnitude, angle, True)
        Normalize(magnitude, magnitude)
        Pow(magnitude, options.exponent, magnitude)

        dst2 = magnitude
    End Sub
End Class






Public Class Gradient_Color : Inherits TaskParent
    Public color1 = Scalar.Blue
    Public color2 = Scalar.Yellow
    Public gradientWidth As Integer
    Public gradient As Mat
    Public Sub New()
        desc = "Provide a spectrum that is a gradient from one color to another."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        gradientWidth = dst2.Width
        Dim f As Double = 1.0
        Dim gradientColors As New Mat(1, gradientWidth, MatType.CV_64FC3)
        For i = 0 To gradientWidth - 1
            gradientColors.Set(Of Scalar)(0, i, New Scalar(f * color2(0) + (1 - f) * color1(0),
                                                                     f * color2(1) + (1 - f) * color1(1),
                                                                     f * color2(2) + (1 - f) * color1(2)))
            f -= 1 / gradientWidth
        Next
        gradient = New Mat(1, gradientWidth, MatType.CV_8UC3)
        For i = 0 To gradientWidth - 1
            gradient.Col(i).SetTo(gradientColors.Get(Of Scalar)(0, i))
        Next
        Resize(gradient, dst2, dst2.Size)
    End Sub
End Class
