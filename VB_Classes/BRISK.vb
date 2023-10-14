Imports cv = OpenCvSharp
Public Class BRISK_Basics : Inherits VB_Algorithm
    Dim brisk As cv.BRISK
    Dim corners As New List(Of cv.Point2f)
    Public Sub New()
        brisk = cv.BRISK.Create()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("BRISK Radius Threshold", 1, 100, 5)
        desc = "Detect features with BRISK"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static radiusSlider = findSlider("BRISK Radius Threshold")
        src.CopyTo(dst2)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim keyPoints = brisk.Detect(src)

        corners.Clear()
        Dim radius = radiusSlider.Value
        For Each pt In keyPoints
            If pt.Size > radius Then
                corners.Add(New cv.Point2f(pt.Pt.X, pt.Pt.Y))
                dst2.Circle(pt.Pt, task.dotSize + 1, task.highlightColor, -1, task.lineType)
            End If
        Next
    End Sub
End Class