Imports cv = OpenCvSharp
Public Class BRISK_Basics
    Inherits VBparent
    Public Brisk As cv.BRISK
    Public features As New List(Of cv.Point2f)
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "BRISK Radius Threshold", 1, 100, 50)
        End If
        task.desc = "Detect features with BRISK"
        Brisk = cv.BRISK.Create()
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        src.CopyTo(dst1)
        Dim keyPoints = Brisk.Detect(src)

        features.Clear()
        For Each pt In keyPoints
            Dim r = pt.Size
            If r > sliders.trackbar(0).Value Then
                features.Add(New cv.Point2f(pt.Pt.X, pt.Pt.Y))
                dst1.Circle(pt.Pt, 2, cv.Scalar.Green, r / 2, task.lineType)
            End If
        Next
        If standalone or task.intermediateReview = caller Then cv.Cv2.AddWeighted(src, 0.5, dst1, 0.5, 0, dst1)
    End Sub
End Class


