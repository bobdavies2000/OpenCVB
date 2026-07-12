Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Covariance_Basics : Inherits TaskParent
    Dim random As New Random_Basics
    Public meanVal As New cv.Mat
    Public covariance As New cv.Mat
    Public Sub New()
        desc = "Calculate the covariance of random depth data points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3.SetTo(0)
        If standaloneTest() Then
            random.Run(src)
            src = cv.Mat.FromPixelData(random.PointList.Count, 2, cv.MatType.CV_32F, random.PointList.ToArray)
            For i = 0 To random.PointList.Count - 1
            Circle(dst3, random.PointList(i), 3, white, -1, task.lineType)
            Next
        End If
        Dim samples2 = src.Reshape(2)
        CalcCovarMatrix(src, covariance, meanVal, cv.CovarFlags.Cols)

        strOut = "The Covariance Mat: " + vbCrLf
        For j = 0 To covariance.Rows - 1
            For i = 0 To covariance.Cols - 1
                strOut += covariance.Get(Of Double)(j, i).ToString(fmt3) + ", "
            Next
            strOut += vbCrLf
        Next
        strOut += vbCrLf

        Dim overallMean = cv.Cv2.Mean(samples2)
        Dim center = New cv.Point2f(overallMean(0), overallMean(1))
        strOut += "Mean (img1, img2) = (" + center.X.ToString(fmt0) + ", " + center.Y.ToString(fmt0) + ")" + vbCrLf

        If standaloneTest() Then
            Static lastCenter As cv.Point2f = center
            Circle(dst3, center, 5, cv.Scalar.Red, -1, task.lineType)
            Circle(dst3, lastCenter, 5, task.highlight, task.lineWidth + 1, task.lineType)
            Line(dst3, center, lastCenter, cv.Scalar.Red, task.lineWidth + 1, task.lineType)
            lastCenter = center
            strOut += "Yellow is last center, red is the current center"
        End If
        SetTrueText(strOut)
    End Sub
End Class



' http://answers.opencvb.org/question/31228/how-to-use-function-calccovarmatrix/
Public Class XR_Covariance_Test : Inherits TaskParent
    Dim covar As New Covariance_Basics
    Public Sub New()
        desc = "Test the covariance basics algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim testInput() As Double = {1.5, 2.3, 3.0, 1.7, 1.2, 2.9, 2.1, 2.2, 3.1, 3.1, 1.3, 2.7, 2.0, 1.7, 1.0, 2.0, 0.5, 0.6, 1.0, 0.9}
        Dim samples = cv.Mat.FromPixelData(10, 2, cv.MatType.CV_64F, testInput)
        covar.Run(samples)
        SetTrueText(covar.strOut, New cv.Point(20, 60))
        SetTrueText("Results should be a symmetric array with 2.1 and -2.1", New cv.Point(20, 150))
    End Sub
End Class






' https://stackoverflow.com/questions/25547823/how-i-calculate-the-covariance-between-2-images
Public Class Covariance_Images : Inherits TaskParent
    Dim covar As New Covariance_Basics
    Public meanVal As cv.Mat
    Public covariance As cv.Mat
    Dim last32f As New cv.Mat
    Public Sub New()
        desc = "Calculate the covariance of 2 images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then task.gray.ConvertTo(last32f, cv.MatType.CV_32F)
        dst2 = task.gray

        Dim gray32f As New cv.Mat
        task.gray.ConvertTo(gray32f, cv.MatType.CV_32F)
        Merge({gray32f, last32f}, dst0)
        Dim samples = dst0.Reshape(1, dst0.Rows * dst0.Cols)
        covar.Run(samples)

        last32f = gray32f

        SetTrueText(covar.strOut, New cv.Point(10, 10), 3)

        meanVal = covar.meanVal
        covariance = covar.covariance
    End Sub
End Class
