Imports OpenCvSharp.ML
Imports cvb = OpenCvSharp
Public Class KNNorm_Basics : Inherits TaskParent
    Public knn2 As New KNN_NNBasics
    Public queries As New List(Of Single)
    Public trainInput As New List(Of Single)
    Public result(,) As Integer ' Get results here...
    Public options As New Options_KNN
    Public Sub New()
        desc = "Default unnormalized KNN with dimension 2"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim qRows = CInt(queries.Count / options.knnDimension)
        Dim queryData = cvb.Mat.FromPixelData(qRows, options.knnDimension, cvb.MatType.CV_32F, queries.ToArray)
        Dim queryMat As cvb.Mat = queryData.Clone
        cvb.Cv2.Normalize(queryData, queryMat, 0, 1, cvb.NormTypes.L2)

        Dim tRows = CInt(trainInput.Count / options.knnDimension)
        Dim trainData = cvb.Mat.FromPixelData(tRows, options.knnDimension, cvb.MatType.CV_32F, trainInput.ToArray())
        cvb.Cv2.Normalize(trainData, trainData, 0, 1, cvb.NormTypes.L2)
        Dim response As cvb.Mat = cvb.Mat.FromPixelData(trainData.Rows, 1, cvb.MatType.CV_32S,
                                  Enumerable.Range(start:=0, trainData.Rows).ToArray)

        knn2.trainInput = trainInput
        knn2.queries = queries
        knn2.Run(src)
        Dim neighbors As cvb.Mat = knn2.neighbors
        ReDim result(neighbors.Rows - 1, neighbors.Cols - 1)
        For i = 0 To neighbors.Rows - 1
            For j = 0 To neighbors.Cols - 1
                Dim test = neighbors.Get(Of Single)(i, j)
                If test < trainInput.Count And test >= 0 Then result(i, j) = neighbors.Get(Of Single)(i, j)
            Next
        Next
    End Sub
End Class