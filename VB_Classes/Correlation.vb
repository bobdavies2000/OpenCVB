Imports cv = OpenCvSharp
Public Class Correlation_Basics : Inherits VBparent
    Dim kFlood As New KMeans_FloodFill
    Dim corr As New MatchTemplate_Basics
    Public Sub New()
        If standalone Then usingdst1 = True
        labels(1) = "Click to select a mask to analyze"
        task.desc = "Compute a correlation for src rows (See also: MatchTemplate.vb"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        kFlood.Run(src)
        dst1 = kFlood.dst2
        dst2 = kFlood.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim split = task.pointCloud.Split()

        Dim row = task.mousePoint.Y

        Dim dataX As New cv.Mat(New cv.Size(src.Width, src.Height), cv.MatType.CV_32F, 0)
        Dim dataY As New cv.Mat(New cv.Size(src.Width, src.Height), cv.MatType.CV_32F, 0)
        Dim dataZ As New cv.Mat(New cv.Size(src.Width, src.Height), cv.MatType.CV_32F, 0)

        split(0).CopyTo(dataX, kFlood.dst3)
        split(1).CopyTo(dataY, kFlood.dst3)
        split(2).CopyTo(dataZ, kFlood.dst3)

        Dim row1 = dataX.Row(row)
        Dim row2 = dataZ.Row(row)
        dst2.Line(New cv.Point(0, row), New cv.Point(dst2.Width, row), cv.Scalar.Yellow, task.lineWidth + 1)

        Dim matchoption = corr.checkRadio()
        Dim correlationmat As New cv.Mat
        cv.Cv2.MatchTemplate(row1, row2, correlationmat, matchoption)
        Dim correlation = correlationmat.Get(Of Single)(0, 0)
        labels(2) = "Correlation of X to Z = " + Format(correlation, "#0.00")

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
                dst3.Circle(New cv.Point(x, y), task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
            Next
            labels(3) = "Y=" + Format(minZ, "0.00") + " to Y=" + Format(maxZ, "0.00") + " X=" + Format(minx, "0.00") + " to X=" + Format(maxx, "0.00")
        End If
    End Sub
End Class