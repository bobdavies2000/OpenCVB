Imports cv = OpenCvSharp
' https://github.com/davemk99/Cartoonify-Image/blob/master/main.cpp
Public Class Cartoonify_Basics : Inherits TaskParent
    Dim options As New Options_Cartoonify
    Public Sub New()
        labels(2) = "Mask for Cartoon"
        labels(3) = "Cartoonify Result"
        desc = "Create a cartoon from a color image"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Options.Run()

        Dim gray8u = task.gray.MedianBlur(options.medianBlur)
        Dim edges = gray8u.Laplacian(cv.MatType.CV_8U, options.kernelSize)
        Dim mask = edges.Threshold(options.threshold, 255, cv.ThresholdTypes.Binary)
        dst2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = src.MedianBlur(options.medianBlur2).MedianBlur(options.medianBlur2)
        src.CopyTo(dst3, mask)
    End Sub
End Class