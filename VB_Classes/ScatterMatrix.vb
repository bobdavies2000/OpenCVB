Imports cv = OpenCvSharp
' https://en.wikipedia.org/wiki/Scatter_matrix
Public Class ScatterMatrix_Example : Inherits TaskParent
    Dim data(,) As Double
    Dim meanVector As Double()
    Public Sub New()
        data = {
            {1.2, 2.3},
            {2.1, 3.4},
            {3.1, 4.2},
            {4.5, 5.1},
            {5.1, 6.2}
        }
        desc = "A synthetic example of computing a scatter matrix."
    End Sub
    Function ComputeMeanVector(data(,) As Double) As Double()
        Dim numRows As Integer = data.GetLength(0)
        Dim numCols As Integer = data.GetLength(1)
        ReDim meanVector(numCols - 1)

        For j As Integer = 0 To numCols - 1
            Dim sum As Double = 0
            For i As Integer = 0 To numRows - 1
                sum += data(i, j)
            Next
            meanVector(j) = sum / numRows
        Next

        Return meanVector
    End Function
    Function ComputeScatterMatrix(data(,) As Double, meanVector() As Double) As Double(,)
        Dim numRows As Integer = data.GetLength(0)
        Dim numCols As Integer = data.GetLength(1)
        Dim scatterMatrix(numCols - 1, numCols - 1) As Double

        For i As Integer = 0 To numRows - 1
            Dim row(numCols - 1) As Double
            For j As Integer = 0 To numCols - 1
                row(j) = data(i, j) - meanVector(j)
            Next
            For j As Integer = 0 To numCols - 1
                For k As Integer = 0 To numCols - 1
                    scatterMatrix(j, k) += row(j) * row(k)
                Next
            Next
        Next

        Return scatterMatrix
    End Function

    Sub PrintMatrix(matrix(,) As Double)
        Dim numRows As Integer = matrix.GetLength(0)
        Dim numCols As Integer = matrix.GetLength(1)

        strOut = "For the input data:" + vbCrLf
        For i = 0 To data.Length / 2 - 1
            strOut += Format(data(i, 0), fmt1) + ", " + Format(data(i, 1), fmt1) + vbCrLf
        Next

        strOut += vbCrLf + vbCrLf + "The mean vector is:" + vbCrLf
        For i = 0 To meanVector.Length - 1
            strOut += Format(meanVector(i), fmt1) + ", "
        Next

        strOut += vbCrLf + vbCrLf + "The scatter matrix is:" + vbCrLf
        For i As Integer = 0 To numRows - 1
            For j As Integer = 0 To numCols - 1
                strOut += $"{matrix(i, j):F2} "
            Next
            strOut += vbCrLf
        Next
        SetTrueText(strOut, 2)
    End Sub

    Public Overrides sub runAlg(src As cv.Mat)
        ' Compute the mean vector
        Dim meanVector() As Double = ComputeMeanVector(data)

        ' Compute the scatter matrix
        Dim scatterMatrix(,) As Double = ComputeScatterMatrix(data, meanVector)

        ' Print the scatter matrix
        PrintMatrix(scatterMatrix)
    End Sub
End Class
