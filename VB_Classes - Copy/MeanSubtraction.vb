Imports cvb = OpenCvSharp
'https://www.pyimagesearch.com/2017/11/06/deep-learning-opencvs-blobfromimage-works/
Public Class MeanSubtraction_Basics : Inherits TaskParent
    Dim options As New Options_MeanSubtraction
    Public Sub New()
        desc = "Subtract the mean from the image with a scaling factor"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim mean = cvb.Cv2.Mean(src)
        cvb.Cv2.Subtract(mean, src, dst2)
        dst2 *= CSng(100 / options.scaleVal)
    End Sub
End Class


