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






Public Class Gradient_Cloud1 : Inherits VB_Parent
    Dim plotHistOriginal As New Plot_Histogram
    Dim plotHistZoom As New Plot_Histogram
    Dim depthMask As New LowRes_DepthMask
    Public Sub New()
        plotHistZoom.createHistogram = True
        plotHistZoom.removeZeroEntry = True
        plotHistOriginal.createHistogram = True
        plotHistOriginal.removeZeroEntry = True
        desc = "Find the gradient in the x and y direction "
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim r1 = New cvb.Rect(0, 0, dst2.Width - 1, dst2.Height - 1)
        Dim r2 = New cvb.Rect(1, 1, r1.Width, r1.Height)

        depthMask.Run(empty)
        Dim pcX = task.pcSplit(0).SetTo(0, Not depthMask.dst2)
        dst1 = pcX(r1) - pcX(r2)
        Dim mm = GetMinMax(dst1)

        plotHistOriginal.Run(dst1)

        Dim firstVal As Single, lastVal As Single
        Dim total = plotHistOriginal.histArray.Sum()
        For i = 0 To task.histogramBins - 1
            If plotHistOriginal.histArray(i) > total / 100 Then
                firstVal = i
                Exit For
            End If
        Next

        For i = task.histogramBins - 1 To 0 Step -1
            If plotHistOriginal.histArray(i) > total / 100 Then
                lastVal = i
                Exit For
            End If
        Next

        Dim incr = (mm.maxVal - mm.minVal) / task.histogramBins
        dst2 = dst1.InRange(firstVal * incr, lastVal * incr)

        mm = GetMinMax(dst2)
        dst2 -= mm.minVal
        dst2 *= 255 / (mm.maxVal - mm.minVal)
        dst2 = dst2.Resize(src.Size)

        plotHistZoom.Run(dst2)
        dst3 = plotHistZoom.dst2
        If task.heartBeat Then labels(3) = plotHistZoom.labels(2)

        If task.heartBeat Then labels(2) = CStr(CInt(mm.maxVal)) + " max value " +
                                           CStr(CInt(mm.minVal)) + " min value"
    End Sub
End Class






Public Class Gradient_CloudX : Inherits VB_Parent
    Dim plotHist As New Plot_Histogram
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Delta X (mm)", 1, 1000, 10)

        task.gOptions.setDisplay0()
        task.gOptions.setDisplay1()

        plotHist.createHistogram = True
        plotHist.removeZeroEntry = True

        labels = {"Mask of pixels < 0", "Mask of pixels > deltaX", "Point Cloud deltaX data",
                  ""}
        desc = "Find the gradient in the x and y direction "
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static xSlider = FindSlider("Delta X (mm)")
        Dim deltaX As Single = xSlider.value / 1000

        Dim r1 = New cvb.Rect(0, 0, dst2.Width - 1, dst2.Height - 1)
        Dim r2 = New cvb.Rect(1, 1, r1.Width, r1.Height)

        dst2 = task.pcSplit(0)(r1) - task.pcSplit(0)(r2)

        ' by definition, difference between 2 neighbors cannot be zero. At least, highly unlikely.
        ' It can go negative because the neighbor pixel may be far behind it.
        dst2 = dst2.Resize(src.Size, 0, 0, cvb.InterpolationFlags.Nearest)
        dst2.SetTo(0, task.noDepthMask)
        dst0 = dst2.Threshold(0, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
        dst1 = dst2.Threshold(deltaX, 255, cvb.ThresholdTypes.Tozero).ConvertScaleAbs

        dst2 = dst2.Clone

        dst2.SetTo(0, dst0)
        dst2.SetTo(0, dst1)

        If task.optionsChanged Then
            plotHist.minRange = 0
            plotHist.maxRange = deltaX
            labels(3) = "First bin is for -" + CStr(xSlider.value) + " mm's difference " +
                        "last bin is for " + CStr(xSlider.value) + " mm's difference "
        End If
        plotHist.Run(dst2)
        dst3 = plotHist.dst2
    End Sub
End Class
