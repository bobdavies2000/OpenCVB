Imports cv = OpenCvSharp
Public Class Derivative_PointCloudXY : Inherits VB_Algorithm
    Dim erode As New Erode_CloudXY
    Dim sobel As New Edge_Sobel
    Public Sub New()
        findSlider("Threshold to zero pixels below this value").Value = 5
        findSlider("Sobel kernel Size").Value = 5
        labels = {"", "", "Sobel of the X point cloud data", "Sobel of the Y point cloud data"}
        desc = "Take the derivative of the point cloud X and Y data"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static thresholdSlider = findSlider("Threshold to zero pixels below this value")
        erode.Run(src)

        sobel.Run(erode.dst2)
        dst2 = sobel.dst2 + sobel.dst3
        dst2 = dst2.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)

        sobel.Run(erode.dst3)
        dst3 = sobel.dst2 + sobel.dst3
        dst3 = dst3.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class
