Imports cv = OpenCvSharp
Public Class BRISK_Basics : Inherits VB_Parent
    Dim brisk As cv.BRISK
    Public features As New List(Of cv.Point2f)
    Dim options As New Options_Features
    Public Sub New()
        brisk = cv.BRISK.Create()
        UpdateAdvice(traceName + ": use 'Options_Features Min Distance' to control the BRISK Radius threshold.")
        desc = "Detect features with BRISK"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        src.CopyTo(dst2)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim keyPoints = brisk.Detect(src)

        features.Clear()
        For Each pt In keyPoints
            If pt.Size > options.minDistance Then
                features.Add(New cv.Point2f(pt.Pt.X, pt.Pt.Y))
                DrawCircle(dst2,pt.Pt, task.dotSize + 1, task.highlightColor)
            End If
        Next
        labels(2) = CStr(features.Count) + " features found with BRISK"
    End Sub
End Class