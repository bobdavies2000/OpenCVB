Imports cv = OpenCvSharp
Public Class Correlation_Basics : Inherits VBparent
    Dim km As New KMeans_CCompMasks
    Dim corr As New MatchTemplate_Basics
    Public Sub New()
        label1 = "Click to select a mask to analyze"
        task.desc = "Compute a correlation for src rows"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        km.Run(src)
        dst1 = km.dst1
        dst2 = km.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim r = km.rects(km.selectedIndex)
        dst2.Rectangle(r, cv.Scalar.Yellow, 1)
        Dim split = task.pointCloud.Split()

        'Dim row = task.mousePoint.Y
        'If row < r.Y Then row = r.Y
        'If row >= r.Y + r.Height Then row = r.Y + r.Height - 1
        'Dim row1 = split(0)(r).Row(row)
        'Dim row2 = split(2)(r).Row(row)
        'dst2.Line(New cv.Point(0, row), New cv.Point(dst2.Width, row), cv.Scalar.Yellow, 1)

        'Dim matchOption = corr.checkRadio()
        'Dim correlationMat As New cv.Mat
        'cv.Cv2.MatchTemplate(row1, row2, correlationMat, matchOption)
        'Dim correlation = correlationMat.Get(Of Single)(0, 0)

        'Console.WriteLine("correlation = " + Format(correlation, "#0.0"))
    End Sub
End Class