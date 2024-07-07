Imports cv = OpenCvSharp
' https://github.com/davemk99/Cartoonify-Image/blob/master/main.cpp
Public Class Cartoonify_Basics : Inherits VB_Parent
    Dim options As New Options_Cartoonify
    Public Sub New()
        labels(2) = "Mask for Cartoon"
        labels(3) = "Cartoonify Result"
        UpdateAdvice(traceName + ": click 'Show All' to control cartoonify options.")
        desc = "Create a cartoon from a color image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        Dim gray8u = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        gray8u = gray8u.MedianBlur(options.medianBlur)
        Dim edges = gray8u.Laplacian(cv.MatType.CV_8U, options.kernelSize)
        Dim mask = edges.Threshold(options.threshold, 255, cv.ThresholdTypes.Binary)
        dst2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = src.MedianBlur(options.medianBlur2).MedianBlur(options.medianBlur2)
        src.CopyTo(dst3, mask)
    End Sub
End Class