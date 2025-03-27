Imports cv = OpenCvSharp
Public Class Correlation_Basics : Inherits TaskParent
    Dim kFlood As New KMeans_Edges
    Dim options As New Options_FeatureMatch
    Public Sub New()
        labels(3) = "Plot of z (vertical scale) to x with ranges shown on the plot."
        UpdateAdvice(traceName + ": there are several local options panels.")
        desc = "Compute a correlation for src rows (See also: Match.vb"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        kFlood.Run(src)
        dst1 = kFlood.dst2
        dst2 = kFlood.dst3

        Dim row = task.mouseMovePoint.Y
        If row = 0 Then SetTrueText("Move mouse across image to see the relationship between X and Z" + vbCrLf +
                                    "A linear relationship is a useful correlation", New cv.Point(0, 10), 3)

        Dim dataX As New cv.Mat(New cv.Size(src.Width, src.Height), cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim dataY As New cv.Mat(New cv.Size(src.Width, src.Height), cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim dataZ As New cv.Mat(New cv.Size(src.Width, src.Height), cv.MatType.CV_32F, cv.Scalar.All(0))

        Dim mask = kFlood.dst3.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        task.pcSplit(0).CopyTo(dataX, mask)
        task.pcSplit(1).CopyTo(dataY, mask)
        task.pcSplit(2).CopyTo(dataZ, mask)

        Dim row1 = dataX.Row(row)
        Dim row2 = dataZ.Row(row)
        dst2.Line(New cv.Point(0, row), New cv.Point(dst2.Width, row), cv.Scalar.Yellow, task.lineWidth + 1)

        Dim correlationmat As New cv.Mat
        cv.Cv2.MatchTemplate(row1, row2, correlationmat, options.matchOption)
        Dim correlation = correlationmat.Get(Of Single)(0, 0)
        labels(2) = "Correlation of X to Z = " + Format(correlation, fmt2)

        dst3.SetTo(0)
        Dim plotX As New List(Of Single)
        Dim plotZ As New List(Of Single)
        For i = 0 To row1.Cols - 1
            Dim x = row1.Get(Of Single)(0, i)
            Dim z = row2.Get(Of Single)(0, i)
            If x <> 0 And z <> 0 Then
                plotX.Add(x)
                plotZ.Add(z)
            End If
        Next

        If plotX.Count > 0 Then
            Dim minx = plotX.Min, maxx = plotX.Max
            Dim minZ = plotZ.Min, maxZ = plotZ.Max
            For i = 0 To plotX.Count - 1
                Dim x = dst3.Width * (plotX(i) - minx) / (maxx - minx)
                Dim y = dst3.Height * (plotZ(i) - minZ) / (maxZ - minZ)
                DrawCircle(dst3,New cv.Point(x, y), task.DotSize, cv.Scalar.Yellow)
            Next
            SetTrueText("Z-min " + Format(minZ, fmt2), New cv.Point(10, 5), 3)
            SetTrueText("Z-max " + Format(maxZ, fmt2) + vbCrLf + vbTab + "X-min " + Format(minx, fmt2), New cv.Point(0, dst3.Height - 20), 3)
            SetTrueText("X-max " + Format(maxx, fmt2), New cv.Point(dst3.Width - 40, dst3.Height - 10), 3)
        End If
    End Sub
End Class