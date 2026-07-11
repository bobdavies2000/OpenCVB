Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
' https://github.com/davemk99/Cartoonify-Image/blob/master/main.cpp
Public Class Cartoonify_Basics : Inherits TaskParent
    Dim options As New Options_Cartoonify
    Public Sub New()
        labels(2) = "Mask for Cartoon"
        labels(3) = "Cartoonify Result"
        desc = "Create a cartoon from a color image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim gray8u As New cv.Mat
        cv.Cv2.MedianBlur(task.gray, gray8u, options.medianBlur)
        Dim edges As New cv.Mat
        cv.Cv2.Laplacian(gray8u, edges, cv.MatType.CV_8U, options.kernelSize)
        Dim mask As New cv.Mat
        cv.Cv2.Threshold(edges, mask, options.threshold, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.CvtColor(mask, dst2, cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.MedianBlur(src, dst3, options.medianBlur2)
        cv.Cv2.MedianBlur(dst3, dst3, options.medianBlur2)
        src.CopyTo(dst3, mask)
    End Sub
End Class
