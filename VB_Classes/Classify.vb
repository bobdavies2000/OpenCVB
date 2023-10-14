Imports cv = OpenCvSharp
Imports Accord.Math
Imports ExcelDataReader
Imports System.Data.Common
Imports Accord.MachineLearning.Bayes
Imports Accord.Statistics.Distributions.Univariate
Imports Accord.MachineLearning.VectorMachines.Learning
Imports Accord.Statistics.Kernels
Imports Accord.Neuro
Imports Accord.MachineLearning.DecisionTrees.Learning
Imports Accord.MachineLearning.DecisionTrees
Imports CS_Classes
Imports Accord.Statistics.Models.Regression.Fitting
Imports OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/points_classifier.cpp
Public Class Classify_Basics : Inherits VB_Algorithm
    Dim random As New Random_Basics
    Public options As New Options_Classifier
    Public pointList As List(Of cv.Point2f)
    Public pointList3D As New List(Of cv.Point3f) ' for the 3 class problem
    Public responses As New List(Of Integer) ' mark the training data with its classification.
    Public colors() As cv.Scalar = {cv.Scalar.Yellow, cv.Scalar.White}
    Public dimension As Integer = 2
    Public Sub New()
        labels = {"", "", "Click anywhere to test the current classifier", ""}
        desc = "Simply classify random mouse point as left or right side using a variety of classifier algorithms"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        Static classifier As Object
        If heartBeat() Then
            If standalone Then
                random.Run(Nothing)
                pointList = New List(Of cv.Point2f)(random.pointList)
                responses.Clear()
                For Each pt In pointList
                    responses.Add(If(pt.X <= dst2.Width / 2, 0, 1))
                Next
                dst2.Line(New cv.Point(dst2.Width / 2, 0), New cv.Point(dst2.Width / 2, dst2.Height), cv.Scalar.White, task.lineWidth, task.lineType)
            End If

            Dim samples As cv.Mat
            If dimension = 3 Then
                samples = New cv.Mat(responses.Count, dimension, cv.MatType.CV_32F, pointList3D.ToArray)
            Else
                samples = New cv.Mat(responses.Count, dimension, cv.MatType.CV_32F, pointList.ToArray)
            End If
            Dim responseMat = New cv.Mat(responses.Count, 1, cv.MatType.CV_32S, responses.ToArray)

            dst2.SetTo(0)
            For i = 0 To responses.Count - 1
                If dimension = 3 Then
                    Dim pt = New cv.Point2f(pointList3D(i).X, pointList3D(i).Y)
                    dst2.Circle(pt, task.dotSize + 2, colors(responses(i)), -1, task.lineType)
                Else
                    dst2.Circle(pointList(i), task.dotSize + 2, colors(responses(i)), -1, task.lineType)
                End If
            Next
            'dst2.Line(New cv.Point(dst2.Width / 2, 0), New cv.Point(dst2.Width / 2, dst2.Height), cv.Scalar.White, task.lineWidth, task.lineType)

            Select Case options.classifierName
                Case "Normal Bayes (NBC)"
                    classifier = cv.ML.NormalBayesClassifier.Create()
                Case "K Nearest Neighbor (KNN)"
                    classifier = cv.ML.KNearest.Create()
                Case "Support Vector Machine (SVM)"
                    ' SVM has all the options available in an options form with sliders and checkboxes...
                    Static optionsSVM As New Options_SVM
                    optionsSVM.Run(Nothing)
                    classifier = optionsSVM.createSVM
                    ' NOTE: SVM predictions are the opposite of what they should be! All the others are fine.
                Case "Decision Tree (DTree)"
                    Dim dtree = cv.ML.DTrees.Create()
                    dtree.MaxDepth = 8
                    dtree.MinSampleCount = 2
                    dtree.UseSurrogates = False
                    dtree.CVFolds = 0
                    dtree.Use1SERule = False
                    dtree.TruncatePrunedTree = False
                    classifier = dtree
                Case "Boosted Tree (BTree)"
                    Dim boost = cv.ML.Boost.Create()
                    boost.BoostType = OpenCvSharp.ML.Boost.Types.Discrete
                    boost.WeakCount = 100
                    boost.WeightTrimRate = 0.95
                    boost.MaxDepth = 2
                    boost.UseSurrogates = False
                    boost.Priors = New cv.Mat
                    classifier = boost
                Case "Random Forest (RF)"
                    Dim rf As cv.ML.RTrees
                    rf = cv.ML.RTrees.Create()
                    rf.MaxDepth = 4
                    rf.MinSampleCount = 2
                    rf.RegressionAccuracy = 0.0F
                    rf.UseSurrogates = False
                    rf.MaxCategories = 16
                    rf.Priors = New cv.Mat
                    rf.CalculateVarImportance = False
                    rf.ActiveVarCount = 1
                    rf.TermCriteria = New cv.TermCriteria(cv.CriteriaTypes.MaxIter, 5, 0)
                    classifier = rf
                Case "Artificial Neural Net (ANN)"
                    ' neural net responses are positional - column 0 or 1 set to 1.0f indicates category.
                    responseMat = New cv.Mat(responses.Count, colors.Count, cv.MatType.CV_32F, 0)
                    For i = 0 To responses.Count - 1
                        responseMat.Set(Of Single)(i, responses(i), 1.0F)
                    Next

                    Dim ann = cv.ML.ANN_MLP.Create()
                    Dim layer_sizes = New cv.Mat(1, 3, cv.MatType.CV_32S, {dimension, 5, colors.Count})
                    ann.SetLayerSizes(layer_sizes)
                    ann.SetActivationFunction(cv.ML.ANN_MLP.ActivationFunctions.SigmoidSym, 1, 1)
                    ann.TermCriteria = New cv.TermCriteria(cv.CriteriaTypes.Eps, 300, 1.19209 * Math.Pow(10, -7))
                    ann.SetTrainMethod(cv.ML.ANN_MLP.TrainingMethods.BackProp, 0.001)
                    classifier = ann
            End Select
            classifier.Train(samples, cv.ML.SampleTypes.RowSample, responseMat)
        End If

        If task.mouseClickFlag Then
            Dim testSample = New cv.Mat(1, dimension, cv.MatType.CV_32F, {CSng(task.clickPoint.X), CSng(task.clickPoint.Y)})
            Dim response = classifier.predict(testSample)
            dst2.Circle(task.clickPoint, task.dotSize + 4, colors(response), -1, task.lineType)
        End If
        setTrueText("Current classifier is " + options.classifierName + vbCrLf + "Click anywhere to test the classifier", 3)
    End Sub
End Class








Public Class Classify_Test1 : Inherits VB_Algorithm
    Dim random As New Random_Basics
    Dim classify As New Classify_Basics
    Public Sub New()
        desc = "Use Classify_Basics to classify data with 2 classes and 2 dimensions"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If heartBeat() Then
            random.Run(Nothing)
            dst2.SetTo(0)
            classify.pointList = New List(Of cv.Point2f)(random.pointList)
            classify.responses.Clear()
            For Each pt In random.pointList
                Dim resp = If(pt.X <= dst2.Width / 2, 0, 1)
                dst2.Circle(pt, task.dotSize + 2, classify.colors(resp), -1, task.lineType)
                classify.responses.Add(resp)
            Next
            dst2.Line(New cv.Point(dst2.Width / 2, 0), New cv.Point(dst2.Width / 2, dst2.Height), cv.Scalar.White, task.lineWidth, task.lineType)
        End If

        classify.Run(Nothing)
        dst2 = classify.dst2
        setTrueText("Current classification scheme = " + classify.options.classifierName + vbCrLf +
                    "This algorithm is identical to Classify_Basics except it is external" + vbCrLf +
                    "Click repeatedly in any quadrant And the color should match the points in that quadrant.", 3)
    End Sub
End Class







Public Class Classify_Test2 : Inherits VB_Algorithm
    Dim random As New Random_Basics
    Dim classify As New Classify_Basics
    Public Sub New()
        findSlider("Random Pixel Count").Value = 50
        classify.colors = {cv.Scalar.Yellow, cv.Scalar.White, cv.Scalar.Green, cv.Scalar.Red}
        desc = "Use Classify_Basics to classify data with 4 classes in 2 dimensions"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If heartBeat() Then
            random.Run(Nothing)
            dst2.SetTo(0)
            classify.responses.Clear()
            classify.pointList = New List(Of cv.Point2f)(random.pointList)
            For Each pt In classify.pointList
                Dim resp As Integer
                If pt.X <= dst2.Width / 2 Then
                    If pt.Y <= dst2.Height / 2 Then resp = 0 Else resp = 2
                Else
                    If pt.Y <= dst2.Height / 2 Then resp = 1 Else resp = 3
                End If

                dst2.Circle(pt, task.dotSize + 2, classify.colors(resp), -1, task.lineType)
                classify.responses.Add(resp)
            Next

            dst2.Line(New cv.Point(dst2.Width / 2, 0), New cv.Point(dst2.Width / 2, dst2.Height), cv.Scalar.White, task.lineWidth, task.lineType)
            dst2.Line(New cv.Point(0, dst2.Height / 2), New cv.Point(dst2.Width, dst2.Height / 2), cv.Scalar.White, task.lineWidth, task.lineType)
        End If

        classify.Run(Nothing)
        dst2 = classify.dst2
        setTrueText("Current classification scheme = " + classify.options.classifierName + vbCrLf +
                    "Click repeatedly in any quadrant And the color should match the points in that quadrant.", 3)
    End Sub
End Class










Public Class Classify_Test3 : Inherits VB_Algorithm
    Dim random As New Random_Basics3D
    Dim classify As New Classify_Basics
    Public Sub New()
        findSlider("Random Pixel Count").Value = 100
        classify.dimension = 3
        classify.colors = {cv.Scalar.Yellow, cv.Scalar.White, cv.Scalar.Green, cv.Scalar.Red}
        desc = "Use Classify_Basics to classify data with 4 classes in 3 dimensions"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If heartBeat() Then
            random.Run(Nothing)
            dst2.SetTo(0)
            classify.responses.Clear()
            classify.pointList3D = New List(Of cv.Point3f)(random.PointList)
            For Each pt In classify.pointList3D
                Dim resp As Integer
                If pt.X <= dst2.Width / 2 Then
                    If pt.Y <= dst2.Height / 2 Then resp = 0 Else resp = 2
                Else
                    If pt.Y <= dst2.Height / 2 Then resp = 1 Else resp = 3
                End If

                dst2.Circle(New cv.Point(pt.X, pt.Y), task.dotSize + 2, classify.colors(resp), -1, task.lineType)
                classify.responses.Add(resp)
            Next

            dst2.Line(New cv.Point(dst2.Width / 2, 0), New cv.Point(dst2.Width / 2, dst2.Height), cv.Scalar.White, task.lineWidth, task.lineType)
            dst2.Line(New cv.Point(0, dst2.Height / 2), New cv.Point(dst2.Width, dst2.Height / 2), cv.Scalar.White, task.lineWidth, task.lineType)
        End If

        classify.Run(Nothing)
        dst2 = classify.dst2
        setTrueText("Current classification scheme = " + classify.options.classifierName + vbCrLf +
                    "Click repeatedly in any quadrant And the color should match the points in that quadrant.", 3)
    End Sub
End Class












Public Class Classify_Test4 : Inherits VB_Algorithm
    Dim csv As New CSV_Basics
    Dim classify As New Classify_Basics
    Public Sub New()
        classify.colors = {cv.Scalar.Yellow, cv.Scalar.White}
        csv.inputFile = task.homeDir + "Data/ClassificationProblem.csv"
        csv.Run(Nothing)
        classify.pointList = New List(Of cv.Point2f)
        classify.responses.Clear()
        For Each index In csv.arrayList(2)
            If index = -1 Then classify.responses.Add(0) Else classify.responses.Add(1)
        Next

        Dim minVal0 = csv.arrayList(0).Min
        Dim maxVal0 = csv.arrayList(0).Max
        Dim minVal1 = csv.arrayList(1).Min
        Dim maxVal1 = csv.arrayList(1).Max
        For i = 0 To csv.arrayList(0).Count - 1
            Dim x = dst2.Width * (csv.arrayList(0)(i) - minVal0) / (maxVal0 - minVal0)
            Dim y = dst2.Height - dst2.Height * (csv.arrayList(1)(i) - minVal1) / (maxVal1 - minVal1)
            classify.pointList.Add(New cv.Point2f(x, y))
        Next

        For i = 0 To classify.pointList.Count - 1
            Dim pt = classify.pointList(i)
            dst2.Circle(New cv.Point(pt.X, pt.Y), task.dotSize + 2, classify.colors(classify.responses(i)), -1, task.lineType)
        Next

        labels = {"", "", "Ground truth (before classification)", "Classification is learned and applied to mouse click point below"}
        desc = "Use Classify_Basics with the problem data that overlaps the 2 classes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        classify.Run(dst2)
        dst3 = classify.dst2
        setTrueText("Current classification scheme = " + classify.options.classifierName + vbCrLf +
                    "Click anywhere And the color should match the points nearby." + vbCrLf +
                    "But not always though as the classification is an approximation.", New cv.Point(0, 20), 3)
    End Sub
End Class








'https://github.com/accord-net/framework/wiki/Classification
Public Class Classify_YinYangData : Inherits VB_Algorithm
    Dim excel As New CSV_Excel
    Public pointList As New List(Of cv.Point2f)
    Public responses As New List(Of Integer)
    Public inputInts(,) As Integer
    Public inputs(,) As Double
    Public outputs() As Integer
    Public Sub New()
        excel.inputFile = task.homeDir + "Data/examples.xls"
        excel.Run(Nothing)

        Dim xArray = excel.dataTable.Columns("Column0").ToArray
        Dim yArray = excel.dataTable.Columns("Column1").ToArray
        Dim color = excel.dataTable.Columns("Column2").ToArray
        Dim xlist = xArray.ToList
        Dim ylist = yArray.ToList

        For i = 0 To color.Count - 1
            If color(i) = -1 Then responses.Add(0) Else responses.Add(1)
        Next

        Dim minVal0 = xlist.Min
        Dim maxVal0 = xlist.Max
        Dim minVal1 = ylist.Min
        Dim maxVal1 = ylist.Max
        For i = 0 To xlist.Count - 1
            Dim x = dst2.Width * (xlist(i) - minVal0) / (maxVal0 - minVal0)
            Dim y = dst2.Height - dst2.Height * (ylist(i) - minVal1) / (maxVal1 - minVal1)
            pointList.Add(New cv.Point2f(x, y))
        Next

        Dim colors = {cv.Scalar.Yellow, cv.Scalar.White}
        For i = 0 To pointList.Count - 1
            Dim pt = pointList(i)
            dst2.Circle(New cv.Point(pt.X, pt.Y), task.dotSize + 2, colors(responses(i)), -1, task.lineType)
        Next

        ReDim inputInts(pointList.Count - 1, 2 - 1)
        ReDim inputs(pointList.Count - 1, 2 - 1)
        ReDim outputs(pointList.Count - 1)
        For i = 0 To pointList.Count - 1
            Dim pt = pointList(i)
            inputInts(i, 0) = pt.X
            inputInts(i, 1) = pt.Y
            inputs(i, 0) = pt.X
            inputs(i, 1) = pt.Y
            outputs(i) = responses(i)
        Next

        labels = {"", "", "Ground truth", "After classification"}
        desc = "Accord: Read the Excel data for the YinYang data from the Accord Neuro/Classification problem"
    End Sub
    Public Sub RunVB(src as cv.Mat)
    End Sub
End Class









'https://github.com/accord-net/framework/wiki/Classification
Public Class Classify_DecisionTree_CS : Inherits VB_Algorithm
    Dim data As New Classify_YinYangData
    Dim DTree As New CS_DecisionTrees
    Public Sub New()
        data.Run(Nothing)
        dst2 = data.dst2
        labels = {"", "", "Ground truth", "After classification"}
        desc = "Accord: Use learning techniques with overlapping data in 2 classes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim inputInts(data.pointList.Count - 1, 2 - 1) As Double
        For i = 0 To data.pointList.Count - 1
            Dim pt = data.pointList(i)
            inputInts(i, 0) = pt.X
            inputInts(i, 1) = pt.Y
        Next

        DTree.RunCS(inputInts.ToJagged, data.responses.ToArray)

        For i = 0 To DTree.answers.Count - 1
            Dim pt = data.pointList(i)
            Dim c = If(DTree.answers(i), cv.Scalar.White, cv.Scalar.Yellow)
            dst3.Circle(New cv.Point(pt.X, pt.Y), task.dotSize + 2, c, -1, task.lineType)
        Next
    End Sub
End Class







'https://github.com/accord-net/framework/wiki/Classification
Public Class Classify_NaiveBayes : Inherits VB_Algorithm
    Dim data As New Classify_YinYangData
    Public Sub New()
        data.Run(Nothing)
        dst2 = data.dst2
        labels = {"", "", "Ground truth", "After classification"}
        desc = "Accord: Use learning techniques with overlapping data in 2 classes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim teacher = New NaiveBayesLearning()
        ' Dim nb = teacher.Learn(data.inputs.ToJagged(), data.outputs) ' This statement will correct the problem but will not compile.  NuGet problem?
        Dim nb = teacher.Learn(data.inputInts.ToJagged(), data.outputs)
        Dim answers = nb.Decide(data.inputInts.ToJagged())

        Dim colors = {cv.Scalar.Yellow, cv.Scalar.White}
        For i = 0 To answers.Count - 1
            Dim pt = data.pointList(i)
            dst3.Circle(New cv.Point(pt.X, pt.Y), task.dotSize + 2, colors(answers(i)), -1, task.lineType)
        Next
        setTrueText("Fails because input has to be integer and should be double!" + vbCrLf + "C# version works fine but VB.Net version fails.")
    End Sub
End Class









'https://github.com/accord-net/framework/wiki/Classification
Public Class Classify_NaiveBayes_CS : Inherits VB_Algorithm
    Dim data As New Classify_YinYangData
    Dim nb As New CS_NaiveBayes
    Public Sub New()
        data.Run(Nothing)
        dst2 = data.dst2
        labels = {"", "", "Ground truth", "After classification"}
        desc = "Accord: Use learning techniques with overlapping data in 2 classes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        nb.RunCS(data.inputs.ToJagged, data.responses.ToArray)

        Dim colors = {cv.Scalar.Yellow, cv.Scalar.White}
        For i = 0 To nb.answers.Count - 1
            Dim pt = data.pointList(i)
            dst3.Circle(New cv.Point(pt.X, pt.Y), task.dotSize + 2, colors(nb.answers(i)), -1, task.lineType)
        Next
    End Sub
End Class









'https://github.com/accord-net/framework/wiki/Classification
Public Class Classify_SVMLinear : Inherits VB_Algorithm
    Dim data As New Classify_YinYangData
    Public Sub New()
        data.Run(Nothing)
        dst2 = data.dst2
        labels = {"", "", "Ground truth", "After classification"}
        desc = "Accord: Use learning techniques with overlapping data in 2 classes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim teacher = New LinearCoordinateDescent()

        Dim svm = teacher.Learn(data.inputs.ToJagged, data.outputs)
        Dim answers = svm.Decide(data.inputs.ToJagged())

        Static func As System.Func(Of Double, Boolean) = Function(x)
                                                             Return x > 0
                                                         End Function
        Dim idx = teacher.Lagrange.Find(func)
        Dim sv = data.inputs.Get(idx)

        For i = 0 To answers.Count - 1
            Dim pt = data.pointList(i)
            Dim c = If(answers(i), cv.Scalar.White, cv.Scalar.Yellow)
            dst3.Circle(New cv.Point(pt.X, pt.Y), task.dotSize + 2, c, -1, task.lineType)
        Next
        setTrueText("Fails in VB.Net and C#.  Not sure why.")
    End Sub
End Class









'https://github.com/accord-net/framework/wiki/Classification
Public Class Classify_SVMLinear_CS : Inherits VB_Algorithm
    Dim data As New Classify_YinYangData
    Dim svm As New CS_SVMLinear
    Public Sub New()
        data.Run(Nothing)
        dst2 = data.dst2
        labels = {"", "", "Ground truth", "After classification"}
        desc = "Accord: Use learning techniques with overlapping data in 2 classes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        svm.RunCS(data.inputs.ToJagged, data.responses.ToArray)
        For i = 0 To svm.answers.Count - 1
            Dim pt = data.pointList(i)
            Dim c = If(svm.answers(i), cv.Scalar.White, cv.Scalar.Yellow)
            dst3.Circle(New cv.Point(pt.X, pt.Y), task.dotSize + 2, c, -1, task.lineType)
        Next
        setTrueText("Fails in VB.Net and C#.  Not sure why.")
    End Sub
End Class








'https://github.com/accord-net/framework/wiki/Classification
Public Class Classify_KernelMethod_CS : Inherits VB_Algorithm
    Dim data As New Classify_YinYangData
    Dim kernel As New CS_KernelMethod
    Public Sub New()
        data.Run(Nothing)
        dst2 = data.dst2
        labels = {"", "", "Ground truth", "After classification"}
        desc = "Accord: Use learning techniques with overlapping data in 2 classes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        kernel.RunCS(data.inputs.ToJagged, data.responses.ToArray)
        For i = 0 To kernel.answers.Count - 1
            Dim pt = data.pointList(i)
            Dim c = If(kernel.answers(i), cv.Scalar.White, cv.Scalar.Yellow)
            dst3.Circle(New cv.Point(pt.X, pt.Y), task.dotSize + 2, c, -1, task.lineType)
        Next
    End Sub
End Class








'https://github.com/accord-net/framework/wiki/Classification
Public Class Classify_MultiClass_CS : Inherits VB_Algorithm
    Dim multi As New CS_MultiClass
    Public Sub New()
        desc = "Accord: Use learning techniques with overlapping data in 2 classes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        multi.RunCS()
        strOut = ""
        For i = 0 To multi.answers.Count - 1
            strOut += CStr(multi.answers(i)) + vbTab
        Next
        setTrueText(strOut)
    End Sub
End Class








'https://github.com/accord-net/framework/wiki/Classification
Public Class Classify_MultiLabel_CS : Inherits VB_Algorithm
    Dim multi As New CS_MultiLabel
    Public Sub New()
        desc = "Accord: Use learning techniques with overlapping data in 2 classes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        multi.RunCS()
        strOut = ""
        For i = 0 To multi.answers.GetUpperBound(0)
            Dim result = multi.answers(i)
            For j = 0 To 3 - 1
                strOut += CStr(result(j)) + vbTab
            Next
            strOut += vbCrLf
        Next
        setTrueText(strOut)
    End Sub
End Class








'https://github.com/accord-net/framework/wiki/Classification
Public Class Classify_BipolarSigmoid_CS : Inherits VB_Algorithm
    Dim data As New Classify_YinYangData
    Dim sigmoid As New CS_BipolarSigmoid
    Public Sub New()
        data.Run(Nothing)
        dst2 = data.dst2
        labels = {"", "", "Ground truth", "After classification"}
        desc = "Accord: Use learning techniques with overlapping data in 2 classes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        sigmoid.RunCS(data.inputs.ToJagged, data.responses.ToArray)
        If sigmoid.answers Is Nothing Then Exit Sub
        For i = 0 To sigmoid.answers.Count - 1
            Dim pt = data.pointList(i)
            Dim c = If(sigmoid.answers(i), cv.Scalar.White, cv.Scalar.Yellow)
            dst3.Circle(New cv.Point(pt.X, pt.Y), task.dotSize + 2, c, -1, task.lineType)
        Next
        setTrueText("Can't find the toDouble() in CS_BipolarSigmoid" + vbCrLf + "See 'CS_BipolarSigmoid' code comments for error.", 3)
    End Sub
End Class








'https://github.com/accord-net/framework/wiki/Classification
Public Class Classify_LogisticRegression_CS : Inherits VB_Algorithm
    Dim data As New Classify_YinYangData
    Dim logistic As New CS_LogisticRegression
    Public Sub New()
        data.Run(Nothing)
        dst2 = data.dst2
        labels = {"", "", "Ground truth", "After classification"}
        desc = "Accord: Use learning techniques with overlapping data in 2 classes"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        logistic.RunCS(data.inputs.ToJagged, data.responses.ToArray)
        For i = 0 To logistic.answers.Count - 1
            Dim pt = data.pointList(i)
            Dim c = If(logistic.answers(i), cv.Scalar.White, cv.Scalar.Yellow)
            dst3.Circle(New cv.Point(pt.X, pt.Y), task.dotSize + 2, c, -1, task.lineType)
        Next
    End Sub
End Class
