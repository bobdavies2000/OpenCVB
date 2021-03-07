Imports cv = OpenCvSharp
Imports mnum = MathNet.Numerics
Imports System.Runtime.InteropServices
' https://www.codeproject.com/Tips/5296095/Perform-a-2D-Fourier-Transform-with-the-Package-ma?msg=5791718#xx5791718xx
Public Class Fourier_MathNet
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Use the 1D Fourier support in MathNet to do a 2D Fourier transform"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim srcData(input.Rows * input.Cols) As Byte
        Marshal.Copy(input.Data, srcData, 0, srcData.Length)

        Dim inputC = New mnum.LinearAlgebra.Complex32.DenseMatrix(input.Rows, input.Cols)
        Dim index As Integer
        For i = 0 To input.Rows - 1
            For j = 0 To input.Cols - 1
                inputC(i, j) = New mnum.Complex32(srcData(index), 0.0)
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
        label1 = "Magnitude=" + Format(magnitude, "###,##0") + " phase=" + Format(phase, "#.000")
    End Sub
End Class









Public Class Fourier_Shapes
    Inherits VBparent
    Dim dft As DFT_Basics
    Dim circle As Draw_Circles
    Dim ellipse As Draw_Ellipses
    Dim polygon As Draw_Polygon
    Dim rectangle As Rectangle_Basics
    Dim lines As Draw_Line
    Dim symShapes As Draw_SymmetricalShapes
    Public Sub New()
        initParent()
        dft = New DFT_Basics

        circle = New Draw_Circles
        ellipse = New Draw_Ellipses
        polygon = New Draw_Polygon
        rectangle = New Rectangle_Basics
        lines = New Draw_Line
        symShapes = New Draw_SymmetricalShapes

        Dim circleSlider = findSlider("Circle Count")
        circleSlider.Value = 1
        Dim ellipseSlider = findSlider("Ellipse Count")
        ellipseSlider.Value = 1
        Dim polySlider = findSlider("Polygon Count")
        polySlider.Value = 1
        Dim rectangleSlider = findSlider("Rectangle Count")
        rectangleSlider.Value = 1
        Dim lineslider = findSlider("Line Count")
        lineslider.Value = 1

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 6)
            radio.check(0).Text = "Draw Circle"
            radio.check(1).Text = "Draw Ellipse"
            radio.check(2).Text = "Draw Rectangle"
            radio.check(3).Text = "Draw Polygon"
            radio.check(4).Text = "Draw Line"
            radio.check(5).Text = "Draw Symmetrical Shapes"
            radio.check(0).Checked = True
        End If

        label1 = "Input to the DFT"
        label2 = "Discrete Fourier Transform Output"
        task.desc = "Show the spectrum magnitude for some standard shapes. Painterly"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static circleRadio = findRadio("Draw Circle")
        Static ellipseRadio = findRadio("Draw Ellipse")
        Static lineRadio = findRadio("Draw Line")
        Static rectangleRadio = findRadio("Draw Rectangle")
        Static polygonRadio = findRadio("Draw Polygon")
        Static symShapeRadio = findRadio("Draw Symmetrical Shapes")

        If circleRadio.checked Then
            circle.Run()
            dst1 = circle.dst1
        ElseIf ellipseRadio.checked Then
            ellipse.Run()
            dst1 = ellipse.dst1
        ElseIf rectangleRadio.checked Then
            rectangle.Run()
            dst1 = rectangle.dst1
        ElseIf polygonRadio.checked Then
            polygon.Run()
            dst1 = polygon.dst1
        ElseIf symShapeRadio.checked Then
            symShapes.Run()
            dst1 = symShapes.dst1
        ElseIf lineRadio.checked Then
            lines.Run()
            dst1 = lines.dst1
        End If

        dft.src = dst1
        dft.Run()
        dst2 = dft.dst2

        ' uncomment the following line to view the inverse of the DFT transform.  It is the grayscale image of the input - no surprise.  It works!
        ' dst1 = inverseDFT(dft.complexImage)
    End Sub
End Class


