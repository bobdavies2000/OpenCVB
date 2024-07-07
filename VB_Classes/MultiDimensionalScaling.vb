Imports cv = OpenCvSharp
' https://github.com/shimat/opencvsharp_2410/blob/master/sample/CStyleSamplesCS/Samples/MDS.cs
Public Class MultiDimensionScaling_Cities : Inherits VB_Parent
    Dim CityDistance() As Double = { ' 10x10 array of distances for 10 cities
        0, 587, 1212, 701, 1936, 604, 748, 2139, 2182, 543,       ' Atlanta
        587, 0, 920, 940, 1745, 1188, 713, 1858, 1737, 597,       ' Chicago
        1212, 920, 0, 879, 831, 1726, 1631, 949, 1021, 1494,      ' Denver
        701, 940, 879, 0, 1734, 968, 1420, 1645, 1891, 1220,      ' Houston
        1936, 1745, 831, 1734, 0, 2339, 2451, 347, 959, 2300,     ' Los Angeles
        604, 1188, 1726, 968, 2339, 0, 1092, 2594, 2734, 923,     ' Miami
        748, 713, 1631, 1420, 2451, 1092, 0, 2571, 2408, 205,     ' New York
        2139, 1858, 949, 1645, 347, 2594, 2571, 0, 678, 2442,     ' San Francisco
        2182, 1737, 1021, 1891, 959, 2734, 2408, 678, 0, 2329,    ' Seattle
        543, 597, 1494, 1220, 2300, 923, 205, 2442, 2329, 0}      ' Washington D.C.
    Public Sub New()
        labels(2) = "Resulting solution using cv.Eigen"
        desc = "Use OpenCV's Eigen function to solve a system of equations"
    End Sub
    Private Function Torgerson(src As cv.Mat) As Double
        Dim rows = src.Rows
        Dim mm as mmData = GetMinMax(src)
        Dim c1 = 0
        For i = 0 To rows - 1
            For j = 0 To rows - 1
                For k = 0 To rows - 1
                    Dim v = src.Get(Of Double)(i, k) - src.Get(Of Double)(i, j) - src.Get(Of Double)(j, k)
                    If v > c1 Then c1 = v
                Next
            Next
        Next
        Return Math.Max(Math.Max(c1, mm.maxVal), 0)
    End Function
    Private Function CenteringMatrix(n As integer) As cv.Mat
        Return cv.Mat.Eye(n, n, cv.MatType.CV_64F) - 1.0 / n
    End Function
    Public Sub RunVB(src as cv.Mat)
        Dim size = 10 ' we are working with 10 cities.
        Dim cityMat = New cv.Mat(size, size, cv.MatType.CV_64FC1, CityDistance)
        cityMat += Torgerson(cityMat)
        cityMat = cityMat.Mul(cityMat)
        Dim g = CenteringMatrix(size)

        ' calculates the inner product matrix b
        Dim b = g * cityMat * g.T * -0.5
        Dim vectors = New cv.Mat(size, size, cv.MatType.CV_64F)
        Dim values = New cv.Mat(size, 1, cv.MatType.CV_64F)

        cv.Cv2.Eigen(b, values, vectors)
        values.Threshold(0, 0, cv.ThresholdTypes.Tozero)

        Dim result = vectors.RowRange(0, 2)
        Dim at = result.GetGenericIndexer(Of Double)()
        For r = 0 To result.Rows - 1
            For c = 0 To result.Cols - 1
                at(r, c) *= Math.Sqrt(values.Get(Of Double)(r))
            Next
        Next

        result.Normalize(0, 800, cv.NormTypes.MinMax)

        at = result.GetGenericIndexer(Of Double)()
        Dim maxX As Double, maxY As Double, minX As Double = Double.MaxValue, minY As Double = Double.MaxValue
        For c = 0 To size - 1
            Dim x = -at(0, c)
            Dim y = at(1, c)
            If maxX < x Then maxX = x
            If maxY < y Then maxY = y
            If minX > x Then minX = x
            If minY > y Then minY = y
        Next
        Dim w = dst2.Width
        Dim h = dst2.Height
        dst2.SetTo(0)
        For c = 0 To size - 1
            Dim x = -at(0, c)
            Dim y = at(1, c)
            x = w * 0.1 + 0.7 * w * (x - minX) / (maxX - minX)
            y = h * 0.1 + 0.7 * h * (y - minY) / (maxY - minY)
            DrawCircle(dst2, New cv.Point(x, y), task.DotSize + 3, cv.Scalar.Red)
            Dim textPos = New cv.Point(x + 5, y + 10)
            Dim cityName = Choose(c + 1, "Atlanta", "Chicago", "Denver", "Houston", "Los Angeles", "Miami", "New York", "San Francisco",
                                         "Seattle", "Washington D.C.")
            SetTrueText(cityName, textPos, 2)
        Next
    End Sub
End Class


