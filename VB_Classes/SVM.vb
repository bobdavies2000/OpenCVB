Imports cv = OpenCvSharp
Public Class SVM_Basics : Inherits VB_Algorithm
    Public options As New Options_SVM
    Dim sampleData As New SVM_SampleData
    Public points As New List(Of cv.Point2f)
    Public response As New List(Of Integer)
    Public Sub New()
        desc = "Use SVM to classify random points.  Increase the sample count to see the value of more data."
        If standalone Then gOptions.GridSize.Value = 8
        labels = {"", "", "SVM_Basics input data", "Results - white line is ground truth"}
    End Sub
    Public Sub RunVB(src as cv.Mat)
        options.RunVB() ' update any options specified in the interface.

        If standalone Then
            sampleData.Run(src)
            dst2 = sampleData.dst2
            points = sampleData.points
            response = sampleData.responses
        End If

        Dim dataMat = New cv.Mat(options.sampleCount, 2, cv.MatType.CV_32FC1, points.ToArray)
        Dim resMat = New cv.Mat(options.sampleCount, 1, cv.MatType.CV_32SC1, response.ToArray)
        dataMat *= 1 / src.Height

        Static svm As cv.ML.SVM
        If task.optionsChanged Then svm = options.createSVM()
        svm.Train(dataMat, cv.ML.SampleTypes.RowSample, resMat)

        dst3.SetTo(0)
        For Each roi In task.gridList
            If roi.X > src.Height Then Continue For ' working only with square - not rectangles.
            Dim samples() As Single = {roi.X / src.Height, roi.Y / src.Height}
            If svm.Predict(New cv.Mat(1, 2, cv.MatType.CV_32F, samples)) = 1 Then
                dst3(roi).SetTo(cv.Scalar.Red)
            Else
                dst3(roi).SetTo(cv.Scalar.GreenYellow)
            End If
        Next

        If standalone Then
            ' draw the function in both plots to show ground truth.
            For x = 1 To src.Height - 1
                Dim y1 = CInt(sampleData.inputFunction(x - 1))
                Dim y2 = CInt(sampleData.inputFunction(x))
                dst3.Line(x - 1, y1, x, y2, cv.Scalar.White, task.lineWidth, task.lineType)
            Next
        End If
    End Sub
End Class






' https://docs.opencv.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class SVM_SampleData : Inherits VB_Algorithm
    ReadOnly options As New Options_SVM
    Public points As New List(Of cv.Point2f)
    Public responses As New List(Of Integer)
    Public Sub New()
        desc = "Create sample data for a sample SVM application."
    End Sub
    Public Function inputFunction(x As Double) As Double
        Return x + 50 * Math.Sin(x / 15.0)
    End Function
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst2.SetTo(0)
        points.Clear()
        responses.Clear()
        For i = 0 To options.sampleCount - 1
            Dim x = msRNG.Next(0, src.Height - 1)
            Dim y = msRNG.Next(0, src.Height - 1)
            points.Add(New cv.Point2f(x, y))
            If y > inputFunction(x) Then
                responses.Add(1)
                dst2.Circle(x, y, 2, cv.Scalar.Red, -1, task.lineType)
            Else
                responses.Add(-1)
                dst2.Circle(x, y, 3, cv.Scalar.GreenYellow, -1, task.lineType)
            End If
        Next
    End Sub
End Class







' https://docs.opencv.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class SVM_TestCase : Inherits VB_Algorithm
    ReadOnly options As New Options_SVM
    Public Sub New()
        findSlider("Granularity").Value = 15
        labels = {"", "", "Input points - color is the category label", "Predictions"}
        desc = "Text book example on SVM"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst2.SetTo(cv.Scalar.White)
        dst3.SetTo(0)
        Dim labeled = 1
        Dim nonlabel = -1

        Static points As New List(Of cv.Point2f)
        Static responses As New List(Of Integer)

        If heartBeat() Then
            points.Clear()
            responses.Clear()
            For i = 0 To 4 - 1
                points.Add(New cv.Point2f(msRNG.Next(0, src.Width - 1), msRNG.Next(0, src.Height - 1)))
                responses.Add(Choose(i + 1, labeled, nonlabel, nonlabel, nonlabel))
            Next
        End If

        Dim trainMat = New cv.Mat(4, 2, cv.MatType.CV_32F, points.ToArray)
        Dim labelsMat = New cv.Mat(4, 1, cv.MatType.CV_32SC1, responses.ToArray)
        Dim dataMat = trainMat * 1 / src.Height

        Static svm As cv.ML.SVM
        If task.optionsChanged Then svm = options.createSVM()
        svm.Train(dataMat, cv.ML.SampleTypes.RowSample, labelsMat)

        Dim sampleMat As New cv.Mat(1, 2, cv.MatType.CV_32F)
        For y = 0 To dst2.Height - 1 Step options.granularity
            For x = 0 To dst2.Width - 1 Step options.granularity
                sampleMat.Set(Of Single)(0, 0, x / src.Height)
                sampleMat.Set(Of Single)(0, 1, y / src.Height)
                Dim response = svm.Predict(sampleMat)
                Dim color = If(response >= 0, cv.Scalar.Blue, cv.Scalar.Red)
                dst3.Circle(New cv.Point(CInt(x), CInt(y)), task.dotSize + 1, color, -1, task.lineType)
            Next
        Next

        For i = 0 To trainMat.Rows - 1
            Dim color = If(labelsMat.Get(Of Integer)(i) = 1, cv.Scalar.Blue, cv.Scalar.Red)
            Dim pt = New cv.Point(trainMat.Get(Of Single)(i, 0), trainMat.Get(Of Single)(i, 1))
            dst2.Circle(pt, task.dotSize + 2, color, -1, task.lineType)
            dst3.Circle(pt, task.dotSize + 2, color, -1, task.lineType)
        Next
    End Sub
End Class









' https://docs.opencv.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class SVM_ReuseBasics : Inherits VB_Algorithm
    ReadOnly svm As New SVM_Basics
    Public Sub New()
        findSlider("Granularity").Value = 15
        findSlider("SVM Sample Count").Value = 4
        labels = {"", "", "Input points", "Predictions"}
        desc = "Text book example on SVM"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim labeled = 1
        Dim nonlabel = -1

        Static points As New List(Of cv.Point2f)
        Static responses As New List(Of Integer)

        If heartBeat() Then
            points.Clear()
            responses.Clear()
            For i = 0 To 4 - 1
                points.Add(New cv.Point2f(msRNG.Next(0, src.Height - 1), msRNG.Next(0, src.Height - 1))) ' note: working with a square, not a rectangle
                responses.Add(Choose(i + 1, labeled, nonlabel, nonlabel, nonlabel))
            Next
        End If

        svm.points.Clear()
        svm.response.Clear()
        For i = 0 To points.Count - 1
            svm.points.Add(points(i))
            svm.response.Add(Choose(i + 1, labeled, nonlabel, nonlabel, nonlabel))
        Next

        svm.Run(src)
        dst3 = svm.dst3

        dst2.SetTo(cv.Scalar.White)
        For i = 0 To svm.points.Count - 1
            Dim color = If(svm.response(i) = 1, cv.Scalar.Blue, cv.Scalar.Red)
            dst2.Circle(svm.points(i), task.dotSize, color, -1, task.lineType)
            dst3.Circle(svm.points(i), task.dotSize, color, -1, task.lineType)
        Next
    End Sub
End Class







Public Class SVM_ReuseRandom : Inherits VB_Algorithm
    ReadOnly svm As New SVM_Basics
    Public Sub New()
        findSlider("Granularity").Value = 15
        task.drawRect = New cv.Rect(dst2.Cols / 4, dst2.Rows / 4, dst2.Cols / 2, dst2.Rows / 2)
        labels(2) = "SVM Training data - draw a rectangle anywhere to test further."
        desc = "Use SVM to classify random points - testing if height must equal width - needs more work"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        svm.options.RunVB()

        Dim rect = task.drawRect
        Dim contour As New List(Of cv.Point)
        contour.Clear()
        contour.Add(New cv.Point(rect.X, rect.Y))
        contour.Add(New cv.Point(rect.X, rect.Y + rect.Height))
        contour.Add(New cv.Point(rect.X + rect.Width, rect.Y + rect.Height))
        contour.Add(New cv.Point(rect.X + rect.Width, rect.Y))

        Dim width = src.Width
        If svm.options.kernelType = cv.ML.SVM.KernelTypes.Linear Then
            width = src.Height
            rect.X = 0
            rect.Y = src.Height - rect.Height
            rect.Width = width
        End If

        Static blueCount As Integer
        If heartBeat() Then
            dst2.SetTo(0)
            blueCount = 0
            svm.points.Clear()
            svm.response.Clear()
            For i = 0 To svm.options.sampleCount - 1
                Dim pt = New cv.Point2f(msRNG.Next(0, width - 1), msRNG.Next(0, src.Height - 1))
                svm.points.Add(pt)
                Dim res = 0
                If svm.options.kernelType = cv.ML.SVM.KernelTypes.Linear Then
                    res = If(pt.X >= pt.Y, 1, -1)
                Else
                    res = If(cv.Cv2.PointPolygonTest(contour, pt, False) >= 0, 1, -1)
                End If

                svm.response.Add(res)
                If res > 0 Then blueCount += 1
                dst2.Circle(pt, task.dotSize, If(res = 1, cv.Scalar.Blue, cv.Scalar.Green), -1, task.lineType)
            Next

            svm.Run(src)
            dst3 = svm.dst3
        End If

        labels(3) = "There were " + CStr(blueCount) + " blue points out of " + CStr(svm.options.sampleCount)
        If svm.options.kernelType = cv.ML.SVM.KernelTypes.Linear = False Then
            dst2.Rectangle(rect, cv.Scalar.Black, 2)
            dst3.Rectangle(rect, cv.Scalar.Black, 2)
        End If
    End Sub
End Class