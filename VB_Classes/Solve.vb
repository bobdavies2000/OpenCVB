Imports cvb = OpenCvSharp
' https://github.com/shimat/opencvsharp/wiki/Solve-Equation
Public Class Solve_ByMat : Inherits VB_Parent
    Public Sub New()
        desc = "Solve a set of equations with OpenCV's Solve API."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        ' x + y = 10
        ' 2x + 3y = 26
        ' (x=4, y=6)
        Dim av(,) As Double = {{1, 1}, {2, 3}}
        Dim yv() As Double = {10, 26}
        Dim a = cvb.Mat.FromPixelData(2, 2, cvb.MatType.CV_64FC1, av)
        Dim y = cvb.Mat.FromPixelData(2, 1, cvb.MatType.CV_64FC1, yv)
        Dim x As New cvb.Mat
        cvb.Cv2.Solve(a, y, x, cvb.DecompTypes.LU)

        SetTrueText("Solution ByMat: X1 = " + CStr(x.Get(Of Double)(0, 0)) + vbTab + "X2 = " + CStr(x.Get(Of Double)(0, 1)), New cvb.Point(10, 125))
    End Sub
End Class




' https://github.com/shimat/opencvsharp/wiki/Solve-Equation
Public Class Solve_ByArray : Inherits VB_Parent
    Public Sub New()
        desc = "Solve a set of equations with OpenCV's Solve API with a normal array as input  "
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        ' x + y = 10
        ' 2x + 3y = 26
        ' (x=4, y=6)
        Dim av(,) As Double = {{1, 1}, {2, 3}}
        Dim yv() As Double = {10, 26}
        Dim x As New cvb.Mat
        cvb.Cv2.Solve(cvb.InputArray.Create(av), cvb.InputArray.Create(yv), x, cvb.DecompTypes.LU)

        SetTrueText("Solution ByArray: X1 = " + CStr(x.Get(Of Double)(0, 0)) + vbTab + "X2 = " + CStr(x.Get(Of Double)(0, 1)), New cvb.Point(10, 125))
    End Sub
End Class


