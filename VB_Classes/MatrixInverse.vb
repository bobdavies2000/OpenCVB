Imports cv = OpenCvSharp
Imports CS_Classes
Module matrixInverse_Module
    Public Function printMatrixResults(src As cv.Mat, dst2 As cv.Mat) As String
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
End Module





' https://visualstudiomagazine.com/articles/2020/04/06/invert-matrix.aspx
Public Class MatrixInverse_Basics_CS : Inherits VB_Parent
    Public matrix As New MatrixInverse ' NOTE: C# class
    Dim defaultInput(,) As Double = {{3, 7, 2, 5}, {4, 0, 1, 1}, {1, 6, 3, 0}, {2, 8, 4, 3}}
    Dim defaultBVector() As Double = {12, 7, 7, 13}
    Dim input As cv.Mat
    Public Sub New()
        input = New cv.Mat(4, 4, cv.MatType.CV_64F, defaultInput)
        desc = "Manually invert a matrix"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If input.Width <> input.Height Then
            setTrueText("The src matrix must be square!")
            Exit Sub
        End If

        If standaloneTest() Then matrix.bVector = defaultBVector

        Dim result = matrix.RunCS(input) ' C# class Run - see MatrixInverse.cs file...

        Dim outstr = printMatrixResults(input, result)
        setTrueText(outstr + vbCrLf + "Intermediate results are optionally available in the console log.")
    End Sub
End Class






Public Class MatrixInverse_OpenCV : Inherits VB_Parent
    Dim defaultInput(,) As Double = {{3, 7, 2, 5}, {4, 0, 1, 1}, {1, 6, 3, 0}, {2, 8, 4, 3}}
    Public input As cv.Mat
    Public Sub New()
        input = New cv.Mat(4, 4, cv.MatType.CV_64F, defaultInput)
        desc = "Use OpenCV to invert a matrix"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If input.Width <> input.Height Then
            setTrueText("The input matrix must be square!")
            Exit Sub
        End If

        Dim result As New cv.Mat
        cv.Cv2.Invert(input, result, cv.DecompTypes.LU)
        Dim outstr = printMatrixResults(input, result)
        setTrueText(outstr)
    End Sub
End Class


