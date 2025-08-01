Imports cv = OpenCvSharp
Public Class BRISK_Basics : Inherits TaskParent
    Dim brisk As cv.BRISK
    Public features As New List(Of cv.Point2f)
    Dim options As New Options_Features
    Public Sub New()
        brisk = cv.BRISK.Create()
        desc = "Detect features with BRISK"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        src.CopyTo(dst2)

        Dim keyPoints = brisk.Detect(task.gray)

        features.Clear()
        For Each pt In keyPoints
            If pt.Size > task.minDistance Then
                features.Add(New cv.Point2f(pt.Pt.X, pt.Pt.Y))
                DrawCircle(dst2, pt.Pt, task.DotSize + 1, task.highlight)
            End If
        Next
        labels(2) = CStr(features.Count) + " features found with BRISK"
    End Sub
    Public Sub Close()
        brisk.Dispose()
    End Sub
End Class