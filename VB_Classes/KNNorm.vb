Imports OpenCvSharp.ML
Imports cv = OpenCvSharp
Public Class KNNorm_Basics : Inherits TaskParent
    Public knn2 As New KNN_NNBasics
    Public queryInput As New List(Of Single)
    Public trainInput As New List(Of Single)
    Public result(,) As Integer ' Get results here...
    Public options As New Options_KNN
    Public Sub New()
        desc = "Default normalized KNN with dimension N"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim qRows = CInt(queryInput.Count / options.knnDimension)
        Dim queryData = cv.Mat.FromPixelData(qRows, options.knnDimension, cv.MatType.CV_32F, queryInput.ToArray)
        Dim queryMat As cv.Mat = queryData.Clone
        cv.Cv2.Normalize(queryData, queryMat, 0, 1, cv.NormTypes.L2)

        Dim tRows = CInt(trainInput.Count / options.knnDimension)
        Dim trainData = cv.Mat.FromPixelData(tRows, options.knnDimension, cv.MatType.CV_32F, trainInput.ToArray())
        cv.Cv2.Normalize(trainData, trainData, 0, 1, cv.NormTypes.L2)
        Dim response As cv.Mat = cv.Mat.FromPixelData(trainData.Rows, 1, cv.MatType.CV_32S,
                                  Enumerable.Range(start:=0, trainData.Rows).ToArray)

        knn2.trainInput = trainInput
        knn2.queries = queryInput
        knn2.Run(src)
        Dim neighbors As cv.Mat = knn2.neighbors
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





Public Class KNNorm_TestDim2 : Inherits TaskParent
    Public knn As New KNNorm_Basics
    Dim random As New Random_Basics
    Public trainInput As New List(Of cv.Point2f)
    Public queryInput As New List(Of cv.Point2f)
    Public Sub New()
       optiBase.findslider("Random Pixel Count").Value = 20
       optiBase.findslider("KNN Dimension").Value = 2
        desc = "Test KNNorm_Basics with random 2D points in the image.  Find the nearest requested neighbors."
    End Sub
    Public Sub displayResults()
        dst2.SetTo(0)
        For i = 0 To queryInput.Count - 1
            Dim pt = queryInput(i)
            Dim index = knn.result(i, 0)
            Dim nn = trainInput(index)
            DrawCircle(dst2, pt, task.DotSize + 4, cv.Scalar.Yellow)
            DrawLine(dst2, pt, nn, cv.Scalar.Yellow)
        Next

        For Each pt In trainInput
            DrawCircle(dst2, pt, task.DotSize + 2, cv.Scalar.Red)
        Next
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standalone Then
            If task.heartBeat Then
                queryInput.Clear()
                trainInput.Clear()
                random.Run(src)
                For Each pt In random.PointList
                    If trainInput.Count < 10 Then
                        trainInput.Add(pt)
                    Else
                        queryInput.Add(pt)
                    End If
                Next
            End If
        End If

        knn.trainInput.Clear()
        For Each pt In trainInput
            knn.trainInput.Add(pt.X)
            knn.trainInput.Add(pt.Y)
        Next

        knn.queryInput.Clear()
        For Each pt In queryInput
            knn.queryInput.Add(pt.X)
            knn.queryInput.Add(pt.Y)
        Next

        knn.Run(src)
        displayResults()

        labels(2) = "Top " + CStr(trainInput.Count) + " best matches. Red=TrainInput, yellow = queryInput"
    End Sub
End Class





Public Class KNNorm_TestDim3 : Inherits TaskParent
    Public knn As New KNNorm_Basics
    Dim random As New Random_Basics3D
    Public trainInput As New List(Of cv.Point3f)
    Public queryInput As New List(Of cv.Point3f)
    Public Sub New()
       optiBase.findslider("Random Pixel Count").Value = 20
       optiBase.findslider("KNN Dimension").Value = 3
        desc = "Test KNNorm_Basics with random 3D points.  Find the nearest neighbors."
    End Sub
    Public Sub displayResults()
        dst2.SetTo(0)
        For i = 0 To queryInput.Count - 1
            Dim pt = New cv.Point(queryInput(i).X, queryInput(i).Y)
            Dim index = knn.result(i, 0)
            Dim nn = New cv.Point(trainInput(index).X, trainInput(index).Y)
            DrawCircle(dst2, pt, task.DotSize + 4, cv.Scalar.Yellow)
            DrawLine(dst2, pt, nn, cv.Scalar.Yellow)
        Next

        For Each pt In trainInput
            Dim p1 = New cv.Point(pt.X, pt.Y)
            DrawCircle(dst2, p1, task.DotSize + 2, cv.Scalar.Red)
        Next
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standalone Then
            If task.heartBeat Then
                queryInput.Clear()
                trainInput.Clear()
                random.Run(src)
                For Each pt In random.PointList
                    If trainInput.Count < 10 Then
                        trainInput.Add(pt)
                    Else
                        queryInput.Add(pt)
                    End If
                Next
            End If
        End If

        knn.trainInput.Clear()
        For Each pt In trainInput
            knn.trainInput.Add(pt.X)
            knn.trainInput.Add(pt.Y)
            knn.trainInput.Add(pt.Z)
        Next

        knn.queryInput.Clear()
        For Each pt In queryInput
            knn.queryInput.Add(pt.X)
            knn.queryInput.Add(pt.Y)
            knn.queryInput.Add(pt.Z)
        Next

        knn.Run(src)
        displayResults()

        labels(2) = "Top " + CStr(trainInput.Count) + " best matches. Red=TrainInput, yellow = queryInput"
    End Sub
End Class





Public Class KNNorm_TestDim4 : Inherits TaskParent
    Public knn As New KNNorm_Basics
    Dim random As New Random_Basics4D
    Public trainInput As New List(Of cv.Vec4f)
    Public queryInput As New List(Of cv.Vec4f)
    Public Sub New()
       optiBase.findslider("Random Pixel Count").Value = 20
       optiBase.findslider("KNN Dimension").Value = 4
        desc = "Test KNNorm_Basics with random 4D points.  Find the nearest neighbors."
    End Sub
    Public Sub displayResults()
        dst2.SetTo(0)
        For i = 0 To queryInput.Count - 1
            Dim pt = New cv.Point(queryInput(i).Item(0), queryInput(i).Item(1))
            Dim index = knn.result(i, 0)
            Dim nn = New cv.Point(trainInput(index).Item(0), trainInput(index).Item(1))
            DrawCircle(dst2, pt, task.DotSize + 4, cv.Scalar.Yellow)
            DrawLine(dst2, pt, nn, cv.Scalar.Yellow)
        Next

        For Each pt In trainInput
            Dim p1 = New cv.Point(pt.Item(0), pt.Item(1))
            DrawCircle(dst2, p1, task.DotSize + 2, cv.Scalar.Red)
        Next
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standalone Then
            If task.heartBeat Then
                queryInput.Clear()
                trainInput.Clear()
                random.Run(src)
                For Each pt In random.PointList
                    If trainInput.Count < 10 Then
                        trainInput.Add(pt)
                    Else
                        queryInput.Add(pt)
                    End If
                Next
            End If
        End If

        knn.trainInput.Clear()
        For Each pt In trainInput
            knn.trainInput.Add(pt.Item(0))
            knn.trainInput.Add(pt.Item(1))
            knn.trainInput.Add(pt.Item(2))
            knn.trainInput.Add(pt.Item(3))
        Next

        knn.queryInput.Clear()
        For Each pt In queryInput
            knn.queryInput.Add(pt.Item(0))
            knn.queryInput.Add(pt.Item(1))
            knn.queryInput.Add(pt.Item(2))
            knn.queryInput.Add(pt.Item(3))
        Next

        knn.Run(src)
        displayResults()

        labels(2) = "Top " + CStr(trainInput.Count) + " best matches. Red=TrainInput, yellow = queryInput"
    End Sub
End Class
