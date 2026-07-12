Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class BRISK_Basics : Inherits TaskParent
    Implements IDisposable
    Dim brisk As XFeatures2D.BRISK
    Public features As New List(Of Point2f)
    Dim options As New Options_Features
    Public Sub New()
        brisk = XFeatures2D.BRISK.Create()
        desc = "Detect features with BRISK"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        src.CopyTo(dst2)

        Dim keyPoints = brisk.Detect(src)

        features.Clear()
        For Each pt In keyPoints
            If pt.Size > options.minDistance Then
                features.Add(New Point2f(pt.Pt.X, pt.Pt.Y))
                Circle(dst2, pt.Pt, task.DotSize + 1, task.highlight, -1, task.lineType)
            End If
        Next
        labels(2) = CStr(features.Count) + " features found with BRISK"
    End Sub
    Protected Overrides Sub Finalize()
        brisk.Dispose()
    End Sub
End Class
