Imports cvb = OpenCvSharp
Public Class Covariance_Basics : Inherits VB_Parent
    Dim random As New Random_Basics
    Public mean As New cvb.Mat
    Public covariance As New cvb.Mat
    Public Sub New()
        UpdateAdvice(traceName + ": use the local options to control the number of points.")
        desc = "Calculate the covariance of random depth data points."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst3.SetTo(0)
        If standaloneTest() Then
            random.Run(empty)
            src = cvb.Mat.FromPixelData(random.PointList.Count, 2, cvb.MatType.CV_32F, random.PointList.ToArray)
            For i = 0 To random.PointList.Count - 1
                DrawCircle(dst3,random.PointList(i), 3, cvb.Scalar.White)
            Next
        End If
        Dim samples2 = src.Reshape(2)
        cvb.Cv2.CalcCovarMatrix(src, covariance, mean, cvb.CovarFlags.Cols)

        strOut = "The Covariance Mat: " + vbCrLf
        For j = 0 To covariance.Rows - 1
            For i = 0 To covariance.Cols - 1
                strOut += Format(covariance.Get(Of Double)(j, i), fmt3) + ", "
            Next
            strOut += vbCrLf
        Next
        strOut += vbCrLf

        Dim overallMean = samples2.Mean()
        Dim center = New cvb.Point2f(overallMean(0), overallMean(1))
        strOut += "Mean (img1, img2) = (" + Format(center.X, fmt0) + ", " + Format(center.Y, fmt0) + ")" + vbCrLf

        If standaloneTest() Then
            Static lastCenter As cvb.Point2f = center
            DrawCircle(dst3,center, 5, cvb.Scalar.Red)
            dst3.Circle(lastCenter, 5, cvb.Scalar.Yellow, task.lineWidth + 1, task.lineType)
            dst3.Line(center, lastCenter, cvb.Scalar.Red, task.lineWidth + 1, task.lineType)
            lastCenter = center
            strOut += "Yellow is last center, red is the current center"
        End If
        SetTrueText(strOut)
    End Sub
End Class



' http://answers.opencvb.org/question/31228/how-to-use-function-calccovarmatrix/
Public Class Covariance_Test : Inherits VB_Parent
    Dim covar As New Covariance_Basics
    Public Sub New()
        desc = "Test the covariance basics algorithm."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim testInput() As Double = {1.5, 2.3, 3.0, 1.7, 1.2, 2.9, 2.1, 2.2, 3.1, 3.1, 1.3, 2.7, 2.0, 1.7, 1.0, 2.0, 0.5, 0.6, 1.0, 0.9}
        Dim samples = cvb.Mat.FromPixelData(10, 2, cvb.MatType.CV_64F, testInput)
        covar.Run(samples)
        SetTrueText(covar.strOut, New cvb.Point(20, 60))
        SetTrueText("Results should be a symmetric array with 2.1 and -2.1", New cvb.Point(20, 150))
    End Sub
End Class






' https://stackoverflow.com/questions/25547823/how-i-calculate-the-covariance-between-2-images
Public Class Covariance_Images : Inherits VB_Parent
    Dim covar As New Covariance_Basics
    Public mean As cvb.Mat
    Public covariance As cvb.Mat
    Dim last32f As New cvb.Mat
    Public Sub New()
        desc = "Calculate the covariance of 2 images"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim gray = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If task.optionsChanged Then gray.ConvertTo(last32f, cvb.MatType.CV_32F)
        dst2 = gray

        Dim gray32f As New cvb.Mat
        gray.ConvertTo(gray32f, cvb.MatType.CV_32F)
        cvb.Cv2.Merge({gray32f, last32f}, dst0)
        Dim samples = dst0.Reshape(1, dst0.Rows * dst0.Cols)
        covar.Run(samples)

        last32f = gray32f

        SetTrueText(covar.strOut, New cvb.Point(10, 10), 3)

        mean = covar.mean
        covariance = covar.covariance
    End Sub
End Class