Imports cv = OpenCvSharp
'https://www.pyimagesearch.com/2017/11/06/deep-learning-opencvs-blobfromimage-works/
Public Class MeanSubtraction_Basics : Inherits VB_Parent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Scaling Factor = mean/scaling factor X100", 1, 500, 100)
        desc = "Subtract the mean from the image with a scaling factor"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static scaleSlider = FindSlider("Scaling Factor = mean/scaling factor X100")
        Dim mean = cv.Cv2.Mean(src)
        cv.Cv2.Subtract(mean, src, dst2)
        dst2 *= CSng(100 / scaleSlider.Value)
    End Sub
End Class


