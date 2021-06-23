Imports cv = OpenCvSharp
' https://github.com/davemk99/Cartoonify-Image/blob/master/main.cpp
Public Class CartoonifyImage_Basics : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Cartoon Median Blur kernel", 1, 21, 7)
            sliders.setupTrackBar(1, "Cartoon Median Blur kernel 2", 1, 21, 3)
            sliders.setupTrackBar(2, "Cartoon threshold", 1, 255, 80)
            sliders.setupTrackBar(3, "Cartoon Laplacian kernel", 1, 21, 5)
        End If
        labels(2) = "Mask for Cartoon"
        labels(3) = "Cartoonify Result"
        task.desc = "Create a cartoon from a color image - Painterly Effect"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim medianBlur = If(sliders.trackbar(0).Value Mod 2, sliders.trackbar(0).Value, sliders.trackbar(0).Value + 1)
        Dim medianBlur2 = If(sliders.trackbar(1).Value Mod 2, sliders.trackbar(1).Value, sliders.trackbar(1).Value + 1)
        Dim kernelSize = If(sliders.trackbar(3).Value Mod 2, sliders.trackbar(3).Value, sliders.trackbar(3).Value + 1)
        Dim gray8u = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        gray8u = gray8u.MedianBlur(medianBlur)
        Dim edges = gray8u.Laplacian(cv.MatType.CV_8U, kernelSize)
        Dim mask = edges.Threshold(sliders.trackbar(2).Value, 255, cv.ThresholdTypes.Binary)
        dst2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = src.MedianBlur(medianBlur2).MedianBlur(medianBlur2)
        src.CopyTo(dst3, mask)
    End Sub
End Class



