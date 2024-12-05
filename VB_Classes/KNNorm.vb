Imports OpenCvSharp.ML
Imports cvb = OpenCvSharp
Public Class KNNorm_Basics : Inherits TaskParent
    Public knn2 As New KNN_NNBasics
    Public queryInput As New List(Of Single)
    Public trainInput As New List(Of Single)
    Public result(,) As Integer ' Get results here...
    Public options As New Options_KNN
    Public Sub New()
        desc = "Default normalized KNN with dimension N"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim qRows = CInt(queryInput.Count / options.knnDimension)
        Dim queryData = cvb.Mat.FromPixelData(qRows, options.knnDimension, cvb.MatType.CV_32F, queryInput.ToArray)
        Dim queryMat As cvb.Mat = queryData.Clone
        cvb.Cv2.Normalize(queryData, queryMat, 0, 1, cvb.NormTypes.L2)

        Dim tRows = CInt(trainInput.Count / options.knnDimension)
        Dim trainData = cvb.Mat.FromPixelData(tRows, options.knnDimension, cvb.MatType.CV_32F, trainInput.ToArray())
        cvb.Cv2.Normalize(trainData, trainData, 0, 1, cvb.NormTypes.L2)
        Dim response As cvb.Mat = cvb.Mat.FromPixelData(trainData.Rows, 1, cvb.MatType.CV_32S,
                                  Enumerable.Range(start:=0, trainData.Rows).ToArray)

        knn2.trainInput = trainInput
        knn2.queries = queryInput
        knn2.Run(src)
        Dim neighbors As cvb.Mat = knn2.neighbors
        ReDim result(neighbors.Rows - 1, neighbors.Cols - 1)
        For i = 0 To neighbors.Rows - 1
            For j = 0 To neighbors.Cols - 1
                Dim test = neighbors.Get(Of Single)(i, j)
                If test < trainInput.Count And test >= 0 Then result(i, j) = neighbors.Get(Of Single)(i, j)
            Next
        Next

        If standalone Then SetTrueText("Runs but no output when run standalone.")
    End Sub
End Class





Public Class KNNorm_N2BasicsTest : Inherits TaskParent
    Public knn As New KNNorm_Basics
    Dim random As New Random_Basics
    Dim trainInput As New List(Of cvb.Point2f)
    Dim queryInput As New List(Of cvb.Point2f)
    Public Sub New()
        FindSlider("Random Pixel Count").Value = 10
        FindSlider("KNN Dimension").Value = 2
        desc = "Test KNNorm_Basics with random 2D points in the image.  Find the nearest requested neighbors."
    End Sub
    Public Sub displayResults()
        dst2.SetTo(0)
        For i = 0 To queryInput.Count - 1
            Dim pt = queryInput(i)
            Dim index = knn.result(i, 0)
            If index >= trainInput.Count Or index < 0 Then Continue For
            Dim nn = trainInput(index)
            DrawCircle(dst2, pt, task.DotSize + 4, cvb.Scalar.Yellow)
            DrawLine(dst2, pt, nn, cvb.Scalar.Yellow)
        Next

        For Each pt In trainInput
            DrawCircle(dst2, pt, task.DotSize + 2, cvb.Scalar.Red)
        Next
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            random.Run(empty)
            trainInput = New List(Of cvb.Point2f)(random.PointList)
        End If

        knn.trainInput.Clear()
        For Each pt In trainInput
            knn.trainInput.Add(pt.X)
            knn.trainInput.Add(pt.Y)
        Next

        random.Run(empty)
        knn.queryInput.Clear()
        queryInput = New List(Of cvb.Point2f)(random.PointList)
        For Each pt In queryInput
            knn.queryInput.Add(pt.X)
            knn.queryInput.Add(pt.Y)
        Next

        knn.Run(empty)
        displayResults()

        labels(2) = "The top " + CStr(knn.trainInput.Count) + " best matches are shown. Red=TrainingData, yellow = queries"
    End Sub
End Class