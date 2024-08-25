Imports cvb = OpenCvSharp
Public Class SVM_Basics : Inherits VB_Parent
    Public options As New Options_SVM
    Dim sampleData As New SVM_SampleData
    Public points As New List(Of cvb.Point2f)
    Public response As New List(Of Integer)
    Dim svm As cvb.ML.SVM
    Public Sub New()
        desc = "Use SVM to classify random points.  Increase the sample count to see the value of more data."
        If standaloneTest() Then task.gOptions.setGridSize(8)
        labels = {"", "", "SVM_Basics input data", "Results - white line is ground truth"}
    End Sub
    Public Sub RunVB(src as cvb.Mat)
        options.RunVB() ' update any options specified in the interface.

        If standaloneTest() Then
            sampleData.Run(src)
            dst2 = sampleData.dst2
            points = sampleData.points
            response = sampleData.responses
        End If

        Dim dataMat = cvb.Mat.FromPixelData(options.sampleCount, 2, cvb.MatType.CV_32FC1, points.ToArray)
        Dim resMat = cvb.Mat.FromPixelData(options.sampleCount, 1, cvb.MatType.CV_32SC1, response.ToArray)
        dataMat *= 1 / src.Height

        If task.optionsChanged Then svm = options.createSVM()
        svm.Train(dataMat, cvb.ML.SampleTypes.RowSample, resMat)

        dst3.SetTo(0)
        For Each roi In task.gridList
            If roi.X > src.Height Then Continue For ' working only with square - not rectangles.
            Dim samples() As Single = {roi.X / src.Height, roi.Y / src.Height}
            If svm.Predict(cvb.Mat.FromPixelData(1, 2, cvb.MatType.CV_32F, samples)) = 1 Then
                dst3(roi).SetTo(cvb.Scalar.Red)
            Else
                dst3(roi).SetTo(cvb.Scalar.GreenYellow)
            End If
        Next

        If standaloneTest() Then
            ' draw the function in both plots to show ground truth.
            For x = 1 To src.Height - 1
                Dim y1 = CInt(sampleData.inputFunction(x - 1))
                Dim y2 = CInt(sampleData.inputFunction(x))
                DrawLine(dst3, New cvb.Point2f(x - 1, y1), New cvb.Point2f(x, y2), cvb.Scalar.White)
            Next
        End If
    End Sub
End Class






' https://docs.opencvb.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class SVM_SampleData : Inherits VB_Parent
    ReadOnly options As New Options_SVM
    Public points As New List(Of cvb.Point2f)
    Public responses As New List(Of Integer)
    Public Sub New()
        desc = "Create sample data for a sample SVM application."
    End Sub
    Public Function inputFunction(x As Double) As Double
        Return x + 50 * Math.Sin(x / 15.0)
    End Function
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()

        dst2.SetTo(0)
        points.Clear()
        responses.Clear()
        For i = 0 To options.sampleCount - 1
            Dim x = msRNG.Next(0, src.Height - 1)
            Dim y = msRNG.Next(0, src.Height - 1)
            points.Add(New cvb.Point2f(x, y))
            If y > inputFunction(x) Then
                responses.Add(1)
                DrawCircle(dst2, New cvb.Point(x, y), 2, cvb.Scalar.Red)
            Else
                responses.Add(-1)
                DrawCircle(dst2, New cvb.Point(x, y), 3, cvb.Scalar.GreenYellow)
            End If
        Next
    End Sub
End Class







' https://docs.opencvb.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class SVM_TestCase : Inherits VB_Parent
    Dim options As New Options_SVM
    Dim points As New List(Of cvb.Point2f)
    Dim responses As New List(Of Integer)
    Dim svm As cvb.ML.SVM
    Public Sub New()
        FindSlider("Granularity").Value = 15
        labels = {"", "", "Input points - color is the category label", "Predictions"}
        desc = "Text book example on SVM"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        options.RunVB()

        dst2.SetTo(cvb.Scalar.White)
        dst3.SetTo(0)
        Dim labeled = 1
        Dim nonlabel = -1

        If task.heartBeat Then
            points.Clear()
            responses.Clear()
            For i = 0 To 4 - 1
                points.Add(New cvb.Point2f(msRNG.Next(0, src.Width - 1), msRNG.Next(0, src.Height - 1)))
                responses.Add(Choose(i + 1, labeled, nonlabel, nonlabel, nonlabel))
            Next
        End If

        Dim trainMat = cvb.Mat.FromPixelData(4, 2, cvb.MatType.CV_32F, points.ToArray)
        Dim labelsMat = cvb.Mat.FromPixelData(4, 1, cvb.MatType.CV_32SC1, responses.ToArray)
        Dim dataMat = trainMat * 1 / src.Height

        If task.optionsChanged Then svm = options.createSVM()
        svm.Train(dataMat, cvb.ML.SampleTypes.RowSample, labelsMat)

        Dim sampleMat As New cvb.Mat(1, 2, cvb.MatType.CV_32F)
        For y = 0 To dst2.Height - 1 Step options.granularity
            For x = 0 To dst2.Width - 1 Step options.granularity
                sampleMat.Set(Of Single)(0, 0, x / src.Height)
                sampleMat.Set(Of Single)(0, 1, y / src.Height)
                Dim response = svm.Predict(sampleMat)
                Dim color = If(response >= 0, cvb.Scalar.Blue, cvb.Scalar.Red)
                DrawCircle(dst3,New cvb.Point(CInt(x), CInt(y)), task.DotSize + 1, color)
            Next
        Next

        For i = 0 To trainMat.Rows - 1
            Dim color = If(labelsMat.Get(Of Integer)(i) = 1, cvb.Scalar.Blue, cvb.Scalar.Red)
            Dim pt = New cvb.Point(trainMat.Get(Of Single)(i, 0), trainMat.Get(Of Single)(i, 1))
            DrawCircle(dst2,pt, task.DotSize + 2, color)
            DrawCircle(dst3,pt, task.DotSize + 2, color)
        Next
    End Sub
End Class









' https://docs.opencvb.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class SVM_ReuseBasics : Inherits VB_Parent
    Dim svm As New SVM_Basics
    Dim points As New List(Of cvb.Point2f)
    Dim responses As New List(Of Integer)

    Public Sub New()
        FindSlider("Granularity").Value = 15
        FindSlider("SVM Sample Count").Value = 4
        labels = {"", "", "Input points", "Predictions"}
        desc = "Text book example on SVM"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        Dim labeled = 1
        Dim nonlabel = -1

        If task.heartBeat Then
            points.Clear()
            responses.Clear()
            For i = 0 To 4 - 1
                points.Add(New cvb.Point2f(msRNG.Next(0, src.Height - 1), msRNG.Next(0, src.Height - 1))) ' note: working with a square, not a rectangle
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

        dst2.SetTo(cvb.Scalar.White)
        For i = 0 To svm.points.Count - 1
            Dim color = If(svm.response(i) = 1, cvb.Scalar.Blue, cvb.Scalar.Red)
            DrawCircle(dst2,svm.points(i), task.DotSize, color)
            DrawCircle(dst3,svm.points(i), task.DotSize, color)
        Next
    End Sub
End Class







Public Class SVM_ReuseRandom : Inherits VB_Parent
    ReadOnly svm As New SVM_Basics
    Dim blueCount As Integer
    Public Sub New()
        FindSlider("Granularity").Value = 15
        task.drawRect = New cvb.Rect(dst2.Cols / 4, dst2.Rows / 4, dst2.Cols / 2, dst2.Rows / 2)
        labels(2) = "SVM Training data - draw a rectangle anywhere to test further."
        desc = "Use SVM to classify random points - testing if height must equal width - needs more work"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        svm.options.RunVB()

        Dim rect = task.drawRect
        Dim contour As New List(Of cvb.Point)
        contour.Clear()
        contour.Add(New cvb.Point(rect.X, rect.Y))
        contour.Add(New cvb.Point(rect.X, rect.Y + rect.Height))
        contour.Add(New cvb.Point(rect.X + rect.Width, rect.Y + rect.Height))
        contour.Add(New cvb.Point(rect.X + rect.Width, rect.Y))

        Dim width = src.Width
        If svm.options.kernelType = cvb.ML.SVM.KernelTypes.Linear Then
            width = src.Height
            rect.X = 0
            rect.Y = src.Height - rect.Height
            rect.Width = width
        End If

        If task.heartBeat Then
            dst2.SetTo(0)
            blueCount = 0
            svm.points.Clear()
            svm.response.Clear()
            For i = 0 To svm.options.sampleCount - 1
                Dim pt = New cvb.Point2f(msRNG.Next(0, width - 1), msRNG.Next(0, src.Height - 1))
                svm.points.Add(pt)
                Dim res = 0
                If svm.options.kernelType = cvb.ML.SVM.KernelTypes.Linear Then
                    res = If(pt.X >= pt.Y, 1, -1)
                Else
                    res = If(cvb.Cv2.PointPolygonTest(contour, pt, False) >= 0, 1, -1)
                End If

                svm.response.Add(res)
                If res > 0 Then blueCount += 1
                DrawCircle(dst2, pt, task.DotSize, If(res = 1, cvb.Scalar.Blue, cvb.Scalar.Green))
            Next

            svm.Run(src)
            dst3 = svm.dst3
        End If

        labels(3) = "There were " + CStr(blueCount) + " blue points out of " + CStr(svm.options.sampleCount)
        If svm.options.kernelType = cvb.ML.SVM.KernelTypes.Linear = False Then
            dst2.Rectangle(rect, cvb.Scalar.Black, 2)
            dst3.Rectangle(rect, cvb.Scalar.Black, 2)
        End If
    End Sub
End Class