Imports cv = OpenCvSharp
'https://www.pyimagesearch.com/2017/11/06/deep-learning-opencvs-blobfromimage-works/
Public Class MeanSubtraction_Basics
    Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Scaling Factor = mean/scaling factor X100", 1, 500, 100)
        End If
        task.desc = "Subtract the mean from the image with a scaling factor"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        Dim mean = cv.Cv2.Mean(src)
        cv.Cv2.Subtract(mean, src, dst1)
        Dim scalingFactor = sliders.trackbar(0).Value / 100
        dst1 *= 1 / scalingFactor
    End Sub
End Class


