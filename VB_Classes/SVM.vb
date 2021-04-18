Imports cv = OpenCvSharp
' https://docs.opencv.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class SVM_Options : Inherits VBparent
    Public kernelType = cv.ML.SVM.KernelTypes.Rbf
    Public SVMType = cv.ML.SVM.Types.CSvc
    Public points() As cv.Point2f
    Public responses() As Integer
    Public Sub New()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 8)
            sliders.setupTrackBar(0, "SampleCount", 5, 1000, 500)
            sliders.setupTrackBar(1, "Granularity", 1, 50, 5)
            sliders.setupTrackBar(2, "SVM Degree", 1, 200, 100)
            sliders.setupTrackBar(3, "SVM Gamma ", 1, 200, 100)
            sliders.setupTrackBar(3, "SVM Coef0 X100", 1, 200, 100)
            sliders.setupTrackBar(4, "SVM C X100", 0, 100, 100)
            sliders.setupTrackBar(5, "SVM Nu X100", 1, 85, 50)
            sliders.setupTrackBar(6, "SVM P X100", 0, 100, 10)
        End If

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 4)
            radio.check(0).Text = "kernel Type = Linear"
            radio.check(1).Text = "kernel Type = Poly (not working)"
            radio.check(1).Enabled = False
            radio.check(2).Text = "kernel Type = RBF"
            radio.check(2).Checked = True
            radio.check(3).Text = "kernel Type = Sigmoid (not working)"
            radio.check(3).Enabled = False
        End If

        radio1.Setup(caller, 5)
        radio1.check(0).Text = "SVM Type = CSvc"
        radio1.check(0).Checked = True
        radio1.check(1).Text = "SVM Type = EpsSvr"
        radio1.check(2).Text = "SVM Type = NuSvc"
        radio1.check(3).Text = "SVM Type = NuSvr"
        radio1.check(4).Text = "SVM Type = OneClass"

        radio1.Text = caller + " SVM Type Radio Options"
        aOptions.optionsTitle.Add(radio1.Text)
        aOptions.hiddenOptions.Remove(caller + " Radio Options")
        radio1.Show()
        label1 = "SVM_Options - only options, no output"
        task.desc = "SVM has many options - enough to make a class for it."
    End Sub
    Public Function createSVM() As cv.ML.SVM
        Static frm = findfrm(caller + " Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                kernelType = Choose(i + 1, cv.ML.SVM.KernelTypes.Linear, cv.ML.SVM.KernelTypes.Poly, cv.ML.SVM.KernelTypes.Rbf, cv.ML.SVM.KernelTypes.Sigmoid)
                Exit For
            End If
        Next

        Static frm1 = findfrm(caller + " SVM Type Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                SVMType = Choose(i + 1, cv.ML.SVM.Types.CSvc, cv.ML.SVM.Types.EpsSvr, cv.ML.SVM.Types.NuSvc, cv.ML.SVM.Types.NuSvr, cv.ML.SVM.Types.OneClass)
                Exit For
            End If
        Next

        Dim svmx = cv.ML.SVM.Create()
        svmx.Type = SVMType
        svmx.KernelType = kernelType
        svmx.TermCriteria = cv.TermCriteria.Both(1000, 0.000001)
        svmx.Degree = CSng(sliders.trackbar(2).Value)
        svmx.Gamma = CSng(sliders.trackbar(3).Value)
        svmx.Coef0 = sliders.trackbar(4).Value / 100
        svmx.C = sliders.trackbar(5).Value / 100
        svmx.Nu = sliders.trackbar(6).Value / 100
        svmx.P = sliders.trackbar(7).Value / 100

        Return svmx
    End Function
    Public Function f(x As Double) As Double
        Return x + 50 * Math.Sin(x / 15.0)
    End Function
    Public Sub Run(src as cv.Mat)
        ReDim points(sliders.trackbar(0).Value)
        ReDim responses(points.Length - 1)
        For i = 0 To points.Length - 1
            Dim x = msRNG.Next(0, src.Height - 1)
            Dim y = msRNG.Next(0, src.Height - 1)
            points(i) = New cv.Point2f(x, y)
            responses(i) = If(y > f(x), 1, 2)
        Next

        dst1.SetTo(0)
        For i = 0 To points.Length - 1
            Dim x = CInt(points(i).X)
            Dim y = CInt(src.Height - points(i).Y)
            Dim res = responses(i)
            Dim color As cv.Scalar = If(res = 1, cv.Scalar.Red, cv.Scalar.GreenYellow)
            Dim cSize = If(res = 1, 2, 4)
            dst1.Circle(x, y, cSize, color, -1, task.lineType)
        Next
    End Sub
End Class



Public Class SVM_Basics : Inherits VBparent
    Dim svmOptions As SVM_Options
    Public Sub New()
        svmOptions = New SVM_Options()
        task.desc = "Use SVM to classify random points.  Increase the sample count to see the value of more data."
        label1 = "SVM_Basics input data"
        label2 = "Results - white line is ground truth"
    End Sub

    Public Sub Run(src as cv.Mat)
        svmOptions.Run(src) ' update any options specified in the interface.
        dst1 = svmOptions.dst1

        Dim dataMat = New cv.Mat(svmOptions.points.Length - 1, 2, cv.MatType.CV_32FC1, svmOptions.points)
        Dim resMat = New cv.Mat(svmOptions.responses.Length - 1, 1, cv.MatType.CV_32SC1, svmOptions.responses)
        Dim svmx = svmOptions.createSVM()
        dataMat *= 1 / src.Height

        svmx.Train(dataMat, cv.ML.SampleTypes.RowSample, resMat)

        Dim granularity = svmOptions.sliders.trackbar(1).Value
        Dim sampleMat As New cv.Mat(1, 2, cv.MatType.CV_32F)
        For x = 0 To src.Height - 1 Step granularity
            For y = 0 To src.Height - 1 Step granularity
                sampleMat.Set(Of Single)(0, 0, x / CSng(src.Height))
                sampleMat.Set(Of Single)(0, 1, y / CSng(src.Height))
                Dim ret = svmx.Predict(sampleMat)
                Dim plotRect = New cv.Rect(x, src.Height - 1 - y, granularity * 2, granularity * 2)
                If ret = 1 Then
                    dst2.Rectangle(plotRect, cv.Scalar.Red, -1)
                ElseIf ret = 2 Then
                    dst2.Rectangle(plotRect, cv.Scalar.GreenYellow, -1)
                End If
            Next
        Next

        ' draw the function in both plots to show ground truth.
        For x = 1 To src.Height - 1
            Dim y1 = CInt(src.Height - svmOptions.f(x - 1))
            Dim y2 = CInt(src.Height - svmOptions.f(x))
            dst1.Line(x - 1, y1, x, y2, cv.Scalar.White, 1, task.lineType)
            dst2.Line(x - 1, y1, x, y2, cv.Scalar.White, 1, task.lineType)
        Next
    End Sub
End Class




Public Class SVM_Random : Inherits VBparent
    Dim svmOptions As SVM_Options
    Public Sub New()
        svmOptions = New SVM_Options()
        svmOptions.sliders.trackbar(1).Value = 15

        task.drawRect = New cv.Rect(task.color.Cols / 4, task.color.Rows / 4, task.color.Cols / 2, task.color.Rows / 2)

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Restrict random test to square area"
        End If

        label1 = "SVM Training data"
        task.desc = "Use SVM to classify random points - testing if height must equal width - needs more work"
    End Sub
    Public Sub Run(src as cv.Mat)
        svmOptions.Run(src)
        dst1.SetTo(cv.Scalar.White)
        dst2.SetTo(cv.Scalar.White)

        Dim rect = task.drawRect

        Dim dataSize = svmOptions.sliders.trackbar(0).Value ' get the sample count
        Dim trainData As New cv.Mat(dataSize, 2, cv.MatType.CV_32F)
        Dim response = New cv.Mat(dataSize, 1, cv.MatType.CV_32S)
        Dim width = src.Width
        Dim setLinear = check.Box(0).Checked
        If setLinear Then
            width = src.Height
            rect.X = 0
            rect.Y = src.Height - rect.Height
            rect.Width = width
        End If

        For i = 0 To dataSize
            Dim pt = New cv.Point2f(msRNG.Next(0, width - 1), msRNG.Next(0, src.Height - 1))
            Dim resp As Integer
            If setLinear Then
                If pt.X >= pt.Y Then resp = 1 Else resp = -1
            Else
                If pt.X > rect.X And pt.X < rect.X + rect.Width And pt.Y > rect.Y And pt.Y < rect.Y + rect.Height Then resp = 1 Else resp = -1
            End If
            response.Set(Of Integer)(i, 0, resp)
            dst1.Circle(pt, 5, If(resp = 1, cv.Scalar.Blue, cv.Scalar.Green), -1, task.lineType)
            trainData.Set(Of Single)(i, 0, pt.X)
            trainData.Set(Of Single)(i, 1, pt.Y)
        Next

        Dim output As New List(Of Single)
        Using svmx = cv.ML.SVM.Create()
            svmx.Train(trainData, cv.ML.SampleTypes.RowSample, response)

            Dim sampleMat As New cv.Mat(1, 2, cv.MatType.CV_32F)
            Dim granularity = svmOptions.sliders.trackbar(1).Value
            Dim blueCount As Integer = 0
            For y = 0 To dst2.Height - 1 Step granularity
                For x = 0 To width - 1 Step granularity
                    sampleMat.Set(Of Single)(0, 0, x)
                    sampleMat.Set(Of Single)(0, 1, y)
                    Dim ret = svmx.Predict(sampleMat)
                    output.Add(ret)
                    If ret >= 0 Then
                        dst2.Circle(New cv.Point(x, y), 5, cv.Scalar.Blue, -1, task.lineType)
                        blueCount += 1
                    Else
                        dst2.Circle(New cv.Point(x, y), 5, cv.Scalar.Green, -1, task.lineType)
                    End If
                Next
            Next
            label2 = "There were " + CStr(blueCount) + " blue predictions"
            If setLinear = False Then
                dst1.Rectangle(rect, cv.Scalar.Black, 2)
                dst2.Rectangle(rect, cv.Scalar.Black, 2)
            End If
        End Using
    End Sub
End Class






' https://docs.opencv.org/3.4/d1/d73/tutorial_introduction_to_svm.html
Public Class SVM_TestCase : Inherits VBparent
    Dim labels() As Integer = {1, -1, -1, -1}
    Dim trainData(,) As Single = {{501, 50}, {255, 50}, {501, 255}, {50, 200}}
    Dim trainMat As cv.Mat
    Dim labelsMat As cv.Mat
    Dim svmOptions As SVM_Options
    Public Sub New()

        trainMat = New cv.Mat(4, 2, cv.MatType.CV_32F, trainData)
        labelsMat = New cv.Mat(4, 1, cv.MatType.CV_32SC1, labels)

        svmOptions = New SVM_Options()
        svmOptions.sliders.trackbar(1).Value = 15
        svmOptions.radio.check(3).Enabled = False

        task.desc = "Text book example on SVM"
    End Sub
    Public Sub Run(src as cv.Mat)
        dst1.SetTo(cv.Scalar.White)
        dst2.SetTo(0)
        svmOptions.Run(src)

        Dim svmx = svmOptions.createSVM()
        svmx.Train(trainMat, cv.ML.SampleTypes.RowSample, labelsMat)

        Dim sampleMat As New cv.Mat(1, 2, cv.MatType.CV_32F)
        Dim granularity = svmOptions.sliders.trackbar(1).Value
        For y = 0 To dst1.Height - 1 Step granularity
            For x = 0 To dst1.Width - 1 Step granularity
                sampleMat.Set(Of Single)(0, 0, x)
                sampleMat.Set(Of Single)(0, 1, y)
                Dim response = svmx.Predict(sampleMat)
                Dim color = If(response >= 0, cv.Scalar.Blue, cv.Scalar.Green)
                dst2.Circle(New cv.Point(x, y), 5, color, -1, task.lineType)
            Next
        Next

        For i = 0 To trainMat.Rows - 1
            Dim color = If(labelsMat.Get(Of Integer)(i) = 1, cv.Scalar.Yellow, cv.Scalar.Red)
            Dim pt = New cv.Point(trainMat.Get(Of Single)(i, 0), trainMat.Get(Of Single)(i, 1))
            dst1.Circle(pt, 5, color, -1, task.lineType)
            dst2.Circle(pt, 5, color, -1, task.lineType)
        Next
    End Sub
End Class

