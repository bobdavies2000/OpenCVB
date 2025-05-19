Imports cv = OpenCvSharp
Public Class Gradient_Basics : Inherits TaskParent
    Public sobel As New Edge_Sobel
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        labels = {"", "", "Gradient_Basics - Sobel output", "Phase Output"}
        desc = "Use phase to compute gradient"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sobel.Run(src)
        cv.Cv2.Phase(sobel.dst0, sobel.dst1, dst3)
        dst2 = sobel.dst0
    End Sub
End Class




Public Class Gradient_PhaseDepth : Inherits TaskParent
    Dim sobel As New Edge_Sobel
    Public Sub New()
        labels(3) = "Phase Output"
        desc = "Use phase to compute gradient on depth image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sobel.Run(task.pcSplit(2))
        cv.Cv2.Phase(sobel.dst0, sobel.dst1, dst3)
        dst2 = sobel.dst0
    End Sub
End Class







' https://github.com/anopara/genetic-drawing
Public Class Gradient_CartToPolar : Inherits TaskParent
    Public basics As New Gradient_Basics
    Public magnitude As New cv.Mat
    Public angle As New cv.Mat
    Dim options As New Options_Gradient
    Public Sub New()
        OptionParent.FindSlider("Sobel kernel Size").Value = 1
        labels(2) = "CartToPolar Magnitude Output Normalized"
        labels(3) = "CartToPolar Angle Output"
        desc = "Compute the gradient and use CartToPolar to image the magnitude and angle"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim tmp As New cv.Mat
        src.ConvertTo(tmp, cv.MatType.CV_32FC3, 1 / 255)
        basics.Run(tmp)

        basics.sobel.dst2.ConvertTo(dst2, cv.MatType.CV_32F)
        basics.sobel.dst2.ConvertTo(dst3, cv.MatType.CV_32F)

        magnitude = New cv.Mat
        angle = New cv.Mat
        cv.Cv2.CartToPolar(dst2, dst3, magnitude, angle, True)
        magnitude = magnitude.Normalize()
        magnitude = magnitude.Pow(options.exponent)

        dst2 = magnitude
    End Sub
End Class






Public Class Gradient_Color : Inherits TaskParent
    Public color1 = cv.Scalar.Blue
    Public color2 = cv.Scalar.Yellow
    Public gradientWidth As Integer
    Public gradient As cv.Mat
    Public Sub New()
        desc = "Provide a spectrum that is a gradient from one color to another."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        gradientWidth = dst2.Width
        Dim f As Double = 1.0
        Dim gradientColors As New cv.Mat(1, gradientWidth, cv.MatType.CV_64FC3)
        For i = 0 To gradientWidth - 1
            gradientColors.Set(Of cv.Scalar)(0, i, New cv.Scalar(f * color2(0) + (1 - f) * color1(0),
                                                                 f * color2(1) + (1 - f) * color1(1),
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





Public Class Gradient_DepthLines : Inherits TaskParent
    Dim options As New Options_Distance
    Public lp As lpData
    Public Sub New()
        If standalone Then lp = New lpData
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32F, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Test using the distance transform to create a gradient in depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Static depthRange As Single, brickNear As brickData, brickFar As brickData
        dst0(lp.rect).SetTo(0)
        dst1(lp.rect).SetTo(0)
        dst2(lp.rect).SetTo(0)
        dst3(lp.rect).SetTo(0)
        If standalone Then lp = task.logicalLines(0)
        For Each brick In lp.bricks
            dst3.Rectangle(task.brickList(brick).rect, 255, -1)
        Next

        brickNear = task.brickList(lp.bricks.First) ' p1 is always closest to the camera.
        brickFar = task.brickList(lp.bricks.Last)

        ' the end points of the bricks may have depth contaminated by a line extending into a brick that is on a depth edge.
        ' here we use the next brick in toward the center's depth as a purer depth estimate.
        brickNear.depth = task.brickList(lp.bricks(1)).depth
        brickFar.depth = task.brickList(lp.bricks(lp.bricks.Count - 2)).depth

        dst1(lp.rect).SetTo(255)
        If lp.vertical Then
            dst1(brickNear.rect).Row(If(lp.inverted, brickNear.rect.Height - 1, 0)).SetTo(0)
        Else
            dst1(brickNear.rect).Col(If(lp.inverted, brickNear.rect.Width - 1, 0)).SetTo(0)
        End If
        depthRange = Math.Abs(brickFar.depth - brickNear.depth)
        dst0(lp.rect) = dst1(lp.rect).DistanceTransform(options.distanceType, 0)

        Dim mm = GetMinMax(dst0(lp.rect))
        Dim normVal = 1 / mm.maxVal
        dst2(lp.rect) = dst0(lp.rect) * normVal

        dst2(lp.rect) *= depthRange
        dst2(lp.rect) += brickNear.depth
        dst2(lp.rect) = dst2(lp.rect).SetTo(0, Not dst3(lp.rect))

        labels(2) = task.lineRGB.labels(2)
    End Sub
End Class






Public Class Gradient_Contour : Inherits TaskParent
    Public Sub New()
        desc = "Use contours in color to create linear depth covering the entire contour."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
    End Sub
End Class
