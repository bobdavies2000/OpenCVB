Imports cv = OpenCvSharp
Public Class Covariance_Basics : Inherits VBparent
    Dim random As New Random_Basics
    Public samples As cv.Mat
    Public Sub New()
        task.desc = "Calculate the covariance of random depth data points."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim covariance As New cv.Mat, mean = New cv.Mat
        dst3.SetTo(0)
        If standalone Or task.intermediateActive Then
            random.RunClass(Nothing)
            samples = New cv.Mat(random.Points.Length, 2, cv.MatType.CV_32F, random.Points2f)
            For i = 0 To random.Points.Length - 1
                dst3.Circle(random.Points(i), 3, cv.Scalar.White, -1, task.lineType)
            Next
        End If
        Dim samples2 = samples.Reshape(2)
        cv.Cv2.CalcCovarMatrix(samples, covariance, mean, cv.CovarFlags.Cols)
        Dim overallMean = samples2.Mean()
        Dim output = "Covar(0, 0), Covar(0, 1)" + vbTab + Format(covariance.Get(Of Double)(0, 0), "#0.0") + vbTab +
                      Format(covariance.Get(Of Double)(0, 1), "#0.0") + vbCrLf
        output += "Covar(1 0), Covar(1, 1)" + vbTab + Format(covariance.Get(Of Double)(1, 0), "#0.0") + vbTab +
                   Format(covariance.Get(Of Double)(1, 1), "#0.0") + vbCrLf
        output += "Mean X, Mean Y" + vbTab + vbTab + Format(overallMean(0), "#0.00") + vbTab + vbTab +
                     Format(overallMean(1), "#0.00") + vbCrLf
        If standalone Or task.intermediateActive Then
            Dim newCenter = New cv.Point(overallMean(0), overallMean(1))
            Static lastCenter = newCenter
            dst3.Circle(newCenter, 5, cv.Scalar.Red, -1, task.lineType)
            dst3.Circle(lastCenter, 5, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
            dst3.Line(newCenter, lastCenter, cv.Scalar.Red, task.lineWidth + 1, task.lineType)
            lastCenter = newCenter
            output += "Yellow is last center, red is the current center"
        End If
        setTrueText(output, 20, 60)
    End Sub
End Class



' http://answers.opencv.org/question/31228/how-to-use-function-calccovarmatrix/
Public Class Covariance_Test : Inherits VBparent
    Dim covar As New Covariance_Basics
    Public Sub New()
        task.desc = "Test the covariance basics algorithm."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim testInput() As Double = {1.5, 2.3, 3.0, 1.7, 1.2, 2.9, 2.1, 2.2, 3.1, 3.1, 1.3, 2.7, 2.0, 1.7, 1.0, 2.0, 0.5, 0.6, 1.0, 0.9}
        covar.samples = New cv.Mat(10, 2, cv.MatType.CV_64F, testInput)
        covar.RunClass(src)
        setTrueText("Results should be a symmetric array with 2.1 and -2.1", 20, 150)
    End Sub
End Class



