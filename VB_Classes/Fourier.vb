Imports cv = OpenCvSharp
Imports mnum = MathNet.Numerics
Imports System.Runtime.InteropServices
' https://www.codeproject.com/Tips/5296095/Perform-a-2D-Fourier-Transform-with-the-Package-ma?msg=5791718#xx5791718xx
Public Class Fourier_MathNet : Inherits VB_Algorithm
    Public Sub New()
        desc = "Use the 1D Fourier support in MathNet to do a 2D Fourier transform"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim dataSrc(input.Rows * input.Cols) As Byte
        Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)

        Dim inputC = New mnum.LinearAlgebra.Complex32.DenseMatrix(input.Rows, input.Cols)
        Dim index As Integer
        For i = 0 To input.Rows - 1
            For j = 0 To input.Cols - 1
                inputC(i, j) = New mnum.Complex32(dataSrc(index), 0.0)
                index += 1
            Next
        Next

        For i = 0 To input.Rows - 1
            Dim inRow = inputC.Row(i).ToArray
            mnum.IntegralTransforms.Fourier.Forward(inRow)
            inputC.SetRow(i, inRow)
        Next

        For j = 0 To input.Cols - 1
            Dim inCol = inputC.Column(j).ToArray
            mnum.IntegralTransforms.Fourier.Forward(inCol)
            inputC.SetColumn(j, inCol)
        Next

        Dim magnitude = inputC.Enumerate().Max(Function(x) mnum.Complex32.Abs(x))
        Dim phase = inputC.Enumerate().Max(Function(x As mnum.Complex32) Math.Abs(x.Phase))
        setTrueText("Magnitude=" + Format(magnitude, "###,##0") + " phase=" + Format(phase, "#.000") + vbCrLf +
                    "The 1D fourier output from MathNet is pretty slow.")
    End Sub
End Class