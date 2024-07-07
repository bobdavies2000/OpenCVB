Imports cv = OpenCvSharp
Public Class MatrixInverse_OpenCV : Inherits VB_Parent
    Dim defaultInput(,) As Double = {{3, 7, 2, 5}, {4, 0, 1, 1}, {1, 6, 3, 0}, {2, 8, 4, 3}}
    Public input As cv.Mat
    Public Sub New()
        input = New cv.Mat(4, 4, cv.MatType.CV_64F, defaultInput)
        desc = "Use OpenCV to invert a matrix"
    End Sub
    Private Function printMatrixResults(src As cv.Mat, dst2 As cv.Mat) As String
        Dim outstr As String = "Original Matrix " + vbCrLf
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                outstr += Format(src.Get(Of Double)(y, x), fmt4) + vbTab
            Next
            outstr += vbCrLf
        Next
        outstr += vbCrLf + "Matrix Inverse" + vbCrLf
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                outstr += Format(dst2.Get(Of Double)(y, x), fmt4) + vbTab
            Next
            outstr += vbCrLf
        Next
        Return outstr
    End Function
    Public Sub RunVB(src As cv.Mat)
        If input.Width <> input.Height Then
            SetTrueText("The input matrix must be square!")
            Exit Sub
        End If

        Dim result As New cv.Mat
        cv.Cv2.Invert(input, result, cv.DecompTypes.LU)
        Dim outstr = printMatrixResults(input, result)
        SetTrueText(outstr)
    End Sub
End Class


