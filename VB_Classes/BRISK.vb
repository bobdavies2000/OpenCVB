Imports cv = OpenCvSharp
Public Class BRISK_Basics : Inherits VBparent
    Public Brisk As cv.BRISK
    Public features As New List(Of cv.Point2f)
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "BRISK Radius Threshold", 1, 100, 50)
        End If
        task.desc = "Detect features with BRISK"
        Brisk = cv.BRISK.Create()
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        src.CopyTo(dst1)
        Dim keyPoints = Brisk.Detect(src)

        features.Clear()
        For Each pt In keyPoints
            Dim r = pt.Size
            If r > sliders.trackbar(0).Value Then
                features.Add(New cv.Point2f(pt.Pt.X, pt.Pt.Y))
                dst1.Circle(pt.Pt, task.dotSize, cv.Scalar.Green, r / task.lineWidth + 1, task.lineType)
            End If
        Next
        If standalone or task.intermediateReview = caller Then cv.Cv2.AddWeighted(src, 0.5, dst1, 0.5, 0, dst1)
    End Sub
End Class



