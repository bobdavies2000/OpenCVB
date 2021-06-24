Imports cv = OpenCvSharp
Public Class Correlation_Basics : Inherits VBparent
    Dim km As New KMeans_CCompMasks
    Dim corr As New MatchTemplate_Basics
    Public Sub New()
        If standalone Then task.usingdst1 = True
        labels(1) = "Click to select a mask to analyze"
        task.desc = "Compute a correlation for src rows"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        km.Run(src)
        dst1 = km.dst2
        dst2 = km.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim r = km.rects(km.selectedIndex)
        dst2.Rectangle(r, cv.Scalar.Yellow, task.lineWidth)
        Dim split = task.pointCloud.Split()

        Dim row = task.mousePoint.Y
        If row < r.Y Then row = r.Y
        If row >= r.Y + r.Height Then row = r.Y + r.Height - 1

        Dim dataX As New cv.Mat(New cv.Size(r.Width, r.Height), cv.MatType.CV_32F, 0)
        Dim dataY As New cv.Mat(New cv.Size(r.Width, r.Height), cv.MatType.CV_32F, 0)
        Dim dataZ As New cv.Mat(New cv.Size(r.Width, r.Height), cv.MatType.CV_32F, 0)

        split(0)(r).CopyTo(dataX, km.dst3(r))
        split(1)(r).CopyTo(dataY, km.dst3(r))
        split(2)(r).CopyTo(dataZ, km.dst3(r))

        Dim row1 = dataX.Row(row - r.Y)
        Dim row2 = dataZ.Row(row - r.Y)
        dst2.Line(New cv.Point(r.X, row), New cv.Point(r.X + r.Width, row), cv.Scalar.Yellow, task.lineWidth + 1)

        Dim matchoption = corr.checkRadio()
        Dim correlationmat As New cv.Mat
        cv.Cv2.MatchTemplate(row1, row2, correlationmat, matchoption)
        Dim correlation = correlationmat.Get(Of Single)(0, 0)

        Console.WriteLine("correlation = " + Format(correlation, "#0.0"))

        dst3.SetTo(0)
        Dim plotX As New List(Of Single)
        Dim plotY As New List(Of Single)
        For i = 0 To row1.Cols - 1
            Dim x = row1.Get(Of Single)(0, i)
            Dim y = row2.Get(Of Single)(0, i)
            If x <> 0 And y <> 0 Then
                plotX.Add(x)
                plotY.Add(y)
            End If
        Next

        If plotX.Count > 0 Then
            Dim minx = plotX.Min, maxx = plotX.Max
            Dim miny = plotY.Min, maxy = plotY.Max
            For i = 0 To plotX.Count - 1
                Dim x = dst3.Width * (plotX(i) - minx) / (maxx - minx)
                Dim y = dst3.Height * (plotY(i) - miny) / (maxy - miny)
                dst3.Circle(New cv.Point(x, y), task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
            Next
        End If
    End Sub
End Class