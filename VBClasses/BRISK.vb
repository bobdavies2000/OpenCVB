Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class BRISK_Basics : Inherits TaskParent
        Implements IDisposable
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

            Dim keyPoints = brisk.Detect(src)

            features.Clear()
            For Each pt In keyPoints
                If pt.Size > options.minDistance Then
                    features.Add(New cv.Point2f(pt.Pt.X, pt.Pt.Y))
                    DrawCircle(dst2, pt.Pt, tsk.DotSize + 1, tsk.highlight)
                End If
            Next
            labels(2) = CStr(features.Count) + " features found with BRISK"
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            brisk.Dispose()
        End Sub
    End Class
End Namespace