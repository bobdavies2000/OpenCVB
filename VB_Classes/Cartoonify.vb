Imports cvb = OpenCvSharp
' https://github.com/davemk99/Cartoonify-Image/blob/master/main.cpp
Public Class Cartoonify_Basics : Inherits TaskParent
    Dim options As New Options_Cartoonify
    Public Sub New()
        labels(2) = "Mask for Cartoon"
        labels(3) = "Cartoonify Result"
        UpdateAdvice(traceName + ": click 'Show All' to control cartoonify options.")
        desc = "Create a cartoon from a color image"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        Options.RunOpt()

        Dim gray8u = src.CvtColor(cvb.ColorConversionCodes.BGR2Gray)
        gray8u = gray8u.MedianBlur(options.medianBlur)
        Dim edges = gray8u.Laplacian(cvb.MatType.CV_8U, options.kernelSize)
        Dim mask = edges.Threshold(options.threshold, 255, cvb.ThresholdTypes.Binary)
        dst2 = mask.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        dst3 = src.MedianBlur(options.medianBlur2).MedianBlur(options.medianBlur2)
        src.CopyTo(dst3, mask)
    End Sub
End Class