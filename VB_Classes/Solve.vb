Imports cv = OpenCvSharp
' https://github.com/shimat/opencvsharp/wiki/Solve-Equation
Public Class Solve_ByMat : Inherits TaskParent
    Public Sub New()
        desc = "Solve a set of equations with OpenCV's Solve API."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        ' x + y = 10
        ' 2x + 3y = 26
        ' (x=4, y=6)
        Dim av(,) As Double = {{1, 1}, {2, 3}}
        Dim yv() As Double = {10, 26}
        Dim a = cv.Mat.FromPixelData(2, 2, cv.MatType.CV_64FC1, av)
        Dim y = cv.Mat.FromPixelData(2, 1, cv.MatType.CV_64FC1, yv)
        Dim x As New cv.Mat
        cv.Cv2.Solve(a, y, x, cv.DecompTypes.LU)

        SetTrueText("Solution ByMat: X1 = " + CStr(x.Get(Of Double)(0, 0)) + vbTab + "X2 = " + CStr(x.Get(Of Double)(0, 1)), New cv.Point(10, 125))
    End Sub
End Class




' https://github.com/shimat/opencvsharp/wiki/Solve-Equation
Public Class Solve_ByArray : Inherits TaskParent
    Public Sub New()
        desc = "Solve a set of equations with OpenCV's Solve API with a normal array as input  "
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        ' x + y = 10
        ' 2x + 3y = 26
        ' (x=4, y=6)
        Dim av(,) As Double = {{1, 1}, {2, 3}}
        Dim yv() As Double = {10, 26}
        Dim x As New cv.Mat
        cv.Cv2.Solve(cv.InputArray.Create(av), cv.InputArray.Create(yv), x, cv.DecompTypes.LU)

        SetTrueText("Solution ByArray: X1 = " + CStr(x.Get(Of Double)(0, 0)) + vbTab + "X2 = " + CStr(x.Get(Of Double)(0, 1)), New cv.Point(10, 125))
    End Sub
End Class


