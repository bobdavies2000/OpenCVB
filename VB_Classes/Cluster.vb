Imports cv = OpenCvSharp

Public Class Cluster_Basics : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "", "Grayscale", "dst3Label"}
        vbAddAdvice(traceName + ": <place advice here on any options that are useful>")
        desc = "description"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class
