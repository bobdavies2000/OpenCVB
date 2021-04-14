Imports cv = OpenCvSharp
Imports CS_Classes
Module matrixInverse_Module
    Public Function printMatrixResults(src As cv.Mat, dst1 As cv.Mat) As String
        Dim outstr As String = "Original Matrix " + vbCrLf
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                outstr += Format(src.Get(of Double)(y, x), "#0.0000") + vbTab
            Next
            outstr += vbCrLf
        Next
        outstr += vbCrLf + "Matrix Inverse" + vbCrLf
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                outstr += Format(dst1.Get(of Double)(y, x), "#0.0000") + vbTab
            Next
            outstr += vbCrLf
        Next
        Return outstr
    End Function
End Module





' https://visualstudiomagazine.com/articles/2020/04/06/invert-matrix.aspx
Public Class MatrixInverse_Basics_CS
    Inherits VBparent
    Public matrix As New MatrixInverse ' NOTE: C# class
    Dim defaultInput(,) As Double = {{3, 7, 2, 5}, {4, 0, 1, 1}, {1, 6, 3, 0}, {2, 8, 4, 3}}
    Dim defaultBVector() As Double = {12, 7, 7, 13}
    Dim input As cv.Mat
    Public Sub New()
        initParent()
        input = New cv.Mat(4, 4, cv.MatType.CV_64F, defaultInput)
        task.desc = "Manually invert a matrix"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If input.Width <> input.Height Then
            task.trueText("The src matrix must be square!")
            Exit Sub
        End If

        If standalone or task.intermediateReview = caller Then matrix.bVector = defaultBVector

        Dim result = matrix.Run(input) ' C# class Run - see MatrixInverse.cs file...

        Dim outstr = printMatrixResults(input, result)
        task.trueText(outstr + vbCrLf + "Intermediate results are optionally available in the console log.")
    End Sub
End Class






Public Class MatrixInverse_OpenCV
    Inherits VBparent
    Dim defaultInput(,) As Double = {{3, 7, 2, 5}, {4, 0, 1, 1}, {1, 6, 3, 0}, {2, 8, 4, 3}}
    Public input As cv.Mat
    Public Sub New()
        initParent()
        input = New cv.Mat(4, 4, cv.MatType.CV_64F, defaultInput)
        task.desc = "Use OpenCV to invert a matrix"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If input.Width <> input.Height Then
            task.trueText("The input matrix must be square!")
            Exit Sub
        End If

        Dim result As New cv.Mat
        cv.Cv2.Invert(input, result, cv.DecompTypes.LU)
        Dim outstr = printMatrixResults(input, result)
        task.trueText(outstr)
    End Sub
End Class


