Imports cv = OpenCvSharp
' https://bytefish.de/blog/eigenvalues_in_opencv/
Public Class Eigen_Basics : Inherits VB_Parent
    Public Sub New()
        desc = "Solve system of equations using OpenCV's EigenVV"
        labels(2) = "EigenVec (solution)"
        labels(3) = "Relationship between Eigen Vec and Vals"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim a() As Double = {1.96, -6.49, -0.47, -7.2, -0.65,
                             -6.49, 3.8, -6.39, 1.5, -6.34,
                             -0.47, -6.39, 4.17, -1.51, 2.67,
                             -7.2, 1.5, -1.51, 5.7, 1.8,
                             -0.65, -6.34, 2.67, 1.8, -7.1}
        Dim mat As New cv.Mat(5, 5, cv.MatType.CV_64FC1, a)
        Dim eigenVal As New cv.Mat, eigenVec As New cv.Mat
        cv.Cv2.Eigen(mat, eigenVal, eigenVec)
        Dim solution(mat.Cols) As Double

        Dim nextLine As String = "Eigen Vals" + vbTab + "Eigen Vectors" + vbTab + vbTab + vbTab + vbTab + vbTab + "Original Matrix" + vbCrLf + vbCrLf
        Dim scalar As cv.Scalar
        For i = 0 To eigenVal.Rows - 1
            scalar = eigenVal.Get(Of cv.Scalar)(0, i)
            solution(i) = scalar.Val0
            nextLine += Format(scalar.Val0, fmt2) + vbTab + vbTab
            For j = 0 To eigenVec.Rows - 1
                scalar = eigenVec.Get(Of cv.Scalar)(i, j)
                nextLine += Format(scalar.Val0, fmt2) + vbTab
            Next
            For j = 0 To eigenVec.Rows - 1
                nextLine += vbTab + Format(a(i * 5 + j), fmt2)
            Next
            nextLine += vbCrLf + vbCrLf
        Next

        For i = 0 To eigenVec.Rows - 1
            Dim plusSign = " + "
            For j = 0 To eigenVec.Cols - 1
                scalar = eigenVec.Get(Of cv.Scalar)(i, j)
                If j = eigenVec.Cols - 1 Then plusSign = vbTab
                nextLine += Format(scalar.Val0, fmt2) + " * " + Format(solution(j), fmt2) + plusSign
            Next
            nextLine += " = " + vbTab + "0.0" + vbCrLf
        Next
        setTrueText(nextLine)
    End Sub
End Class







Public Class Eigen_FitLineInput : Inherits VB_Parent
    Public points As New List(Of cv.Point2f)
    Public m As Single
    Public bb As Single
    Public options As New Options_Eigen
    Public Sub New()
        labels(2) = "Use sliders to adjust the width and intensityf of the line"
        desc = "Generate a noisy line in a field of random data."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        If task.heartBeat Then
            If task.testAllRunning = False Then options.recompute = False
            dst2.SetTo(0)
            Dim width = src.Width
            Dim height = src.Height

            points.Clear()
            For i = 0 To options.randomCount - 1
                Dim pt = New cv.Point2f(Rnd() * width, Rnd() * height)
                If pt.X < 0 Then pt.X = 0
                If pt.X > width Then pt.X = width
                If pt.Y < 0 Then pt.Y = 0
                If pt.Y > height Then pt.Y = height
                points.Add(pt)
                dst2.Circle(points(i), task.dotSize, cv.Scalar.White, -1, task.lineType)
            Next

            Dim p1 As cv.Point2f, p2 As cv.Point2f
            If Rnd() * 2 - 1 >= 0 Then
                p1 = New cv.Point(Rnd() * width, 0)
                p2 = New cv.Point(Rnd() * width, height)
            Else
                p1 = New cv.Point(0, Rnd() * height)
                p2 = New cv.Point(width, Rnd() * height)
            End If

            If p1.X = p2.X Then p1.X += 1
            If p1.Y = p2.Y Then p1.Y += 1
            m = (p2.Y - p1.Y) / (p2.X - p1.X)
            bb = p2.Y - p2.X * m
            Dim startx = Math.Min(p1.X, p2.X)
            Dim incr = (Math.Max(p1.X, p2.X) - startx) / options.linePairCount
            Dim highLight = cv.Scalar.White
            If options.highlight Then
                highLight = cv.Scalar.Gray
            End If
            For i = 0 To options.linePairCount - 1
                Dim noiseOffsetX = (Rnd() * 2 - 1) * options.noiseOffset
                Dim noiseOffsetY = (Rnd() * 2 - 1) * options.noiseOffset
                Dim pt = New cv.Point(startx + i * incr + noiseOffsetX, Math.Max(0, Math.Min(m * (startx + i * incr) + bb + noiseOffsetY, height)))
                If pt.X < 0 Then pt.X = 0
                If pt.X > width Then pt.X = width
                If pt.Y < 0 Then pt.Y = 0
                If pt.Y > height Then pt.Y = height
                points.Add(pt)
                dst2.Circle(pt, task.dotSize + 1, highLight, -1, task.lineType)
            Next
        End If
    End Sub
End Class




' http://www.cs.cmu.edu/~youngwoo/doc/lineFittingTest.cpp
Public Class Eigen_Fitline : Inherits VB_Parent
    Dim noisyLine As New Eigen_FitLineInput
    Public Sub New()
        labels(2) = "blue is Ground Truth, red is fitline, yellow is EigenFit"
        labels(3) = "Raw input (use sliders below to explore)"
        desc = "Remove outliers when trying to fit a line.  Fitline and the Eigen computation below produce the same result."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static eigenVec As New cv.Mat(2, 2, cv.MatType.CV_32F, 0), eigenVal As New cv.Mat(2, 2, cv.MatType.CV_32F, 0)
        Static theta As Single
        Static len As Single
        Static m2 As Single

        noisyLine.options.recompute = True
        noisyLine.Run(src)

        dst3 = noisyLine.dst2
        dst2.SetTo(0)
        noisyLine.options.recompute = False

        Dim width = src.Width

        Dim line = cv.Cv2.FitLine(noisyLine.points, cv.DistanceTypes.L2, 1, 0.01, 0.01)
        Dim m = line.Vy / line.Vx
        Dim bb = line.Y1 - m * line.X1
        Dim p1 = New cv.Point(0, bb)
        Dim p2 = New cv.Point(width, m * width + bb)
        dst2.Line(p1, p2, cv.Scalar.Red, 20, task.lineType)

        Dim pointMat = New cv.Mat(noisyLine.options.randomCount, 1, cv.MatType.CV_32FC2, noisyLine.points.ToArray)
        Dim mean = pointMat.Mean()
        Dim split() = pointMat.Split()
        Dim mmX = vbMinMax(split(0))
        Dim mmY = vbMinMax(split(1))

        Dim eigenInput As New cv.Vec4f
        For i = 0 To noisyLine.points.Count - 1
            Dim pt = noisyLine.points(i)
            Dim x = pt.X - mean.Val0
            Dim y = pt.Y - mean.Val1
            eigenInput(0) += x * x
            eigenInput(1) += x * y
            eigenInput(3) += y * y
        Next
        eigenInput(2) = eigenInput(1)

        Dim vec4f As New List(Of cv.Point2f)
        vec4f.Add(New cv.Point2f(eigenInput(0), eigenInput(1)))
        vec4f.Add(New cv.Point2f(eigenInput(1), eigenInput(3)))

        Dim D = New cv.Mat(2, 2, cv.MatType.CV_32FC1, vec4f.ToArray)
        cv.Cv2.Eigen(D, eigenVal, eigenVec)
        theta = Math.Atan2(eigenVec.Get(Of Single)(1, 0), eigenVec.Get(Of Single)(0, 0))

        len = Math.Sqrt(Math.Pow(mmX.maxVal - mmX.minVal, 2) + Math.Pow(mmY.maxVal - mmY.minVal, 2))

        p1 = New cv.Point2f(mean.Val0 - Math.Cos(theta) * len / 2, mean.Val1 - Math.Sin(theta) * len / 2)
        p2 = New cv.Point2f(mean.Val0 + Math.Cos(theta) * len / 2, mean.Val1 + Math.Sin(theta) * len / 2)
        m2 = (p2.Y - p1.Y) / (p2.X - p1.X)

        If Math.Abs(m2) > 1.0 Then
            dst2.Line(p1, p2, task.highlightColor, 10, task.lineType)
        Else
            p1 = New cv.Point2f(mean.Val0 - Math.Cos(-theta) * len / 2, mean.Val1 - Math.Sin(-theta) * len / 2)
            p2 = New cv.Point2f(mean.Val0 + Math.Cos(-theta) * len / 2, mean.Val1 + Math.Sin(-theta) * len / 2)
            m2 = (p2.Y - p1.Y) / (p2.X - p1.X)
            dst2.Line(p1, p2, cv.Scalar.Yellow, 10, task.lineType)
        End If
        p1 = New cv.Point(0, noisyLine.bb)
        p2 = New cv.Point(width, noisyLine.m * width + noisyLine.bb)
        dst2.Line(p1, p2, cv.Scalar.Blue, task.lineWidth + 2, task.lineType)
        setTrueText("Ground Truth m = " + Format(noisyLine.m, fmt2) + " eigen m = " + Format(m2, fmt2) + "    len = " + CStr(CInt(len)) + vbCrLf +
                    "Confidence = " + Format(eigenVal.Get(Of Single)(0, 0) / eigenVal.Get(Of Single)(1, 0), fmt1) + vbCrLf +
                    "theta: atan2(" + Format(eigenVec.Get(Of Single)(1, 0), fmt1) + ", " + Format(eigenVec.Get(Of Single)(0, 0), fmt1) + ") = " +
                    Format(theta, fmt4))
    End Sub
End Class