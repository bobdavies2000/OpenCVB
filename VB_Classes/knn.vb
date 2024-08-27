Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Public Class KNN_Basics : Inherits VB_Parent
    Public matches As New List(Of PointPair)
    Public noMatch As New List(Of cvb.Point)
    Public knn As New KNN_Core
    Public queries As New List(Of cvb.Point2f)
    Public neighbors As New List(Of Integer)
    Dim random As New Random_Basics
    Public Sub New()
        labels(2) = "KNN_Core output with many-to-one results"
        labels(3) = "KNN_Basics output with just the closest match.  Red = training data, yellow = queries."
        desc = "Map points 1:1 with losses. Toss any farther duplicates. Easier to follow than previous version."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            If task.heartBeat Then
                Random.Run(empty)
                knn.trainInput = New List(Of cvb.Point2f)(random.PointList)
            End If
            Random.Run(empty)
            queries = New List(Of cvb.Point2f)(random.PointList)
        End If

        If queries.Count = 0 Then
            SetTrueText("Place some input points in queries before starting the knn run.")
            Exit Sub
        End If

        knn.queries = queries
        knn.Run(empty)
        knn.displayResults()
        dst2 = knn.dst2

        neighbors.Clear()
        For i = 0 To knn.neighbors.Count - 1
            neighbors.Add(knn.neighbors(i)(0))
        Next

        For i = 0 To neighbors.Count - 1
            Dim p1 = knn.queries(i)
            If neighbors(i) = -1 Then Continue For
            Dim ptn = knn.trainInput(neighbors(i))
            For j = i + 1 To neighbors.Count - 1
                If neighbors(j) = neighbors(i) Then
                    Dim p2 = knn.queries(j)
                    Dim d1 = p1.DistanceTo(ptn)
                    Dim d2 = p2.DistanceTo(ptn)
                    neighbors(If(d1 > d2, i, j)) = -1
                End If
            Next
        Next

        dst3.SetTo(0)
        For Each pt In knn.trainInput
            DrawCircle(dst3,pt, task.DotSize + 4, cvb.Scalar.Red)
        Next

        noMatch.Clear()
        matches.Clear()
        For i = 0 To neighbors.Count - 1
            Dim pt = queries(i)
            DrawCircle(dst3,pt, task.DotSize + 4, cvb.Scalar.Yellow)
            If neighbors(i) = -1 Then
                noMatch.Add(pt)
            Else
                Dim nn = knn.trainInput(neighbors(i))
                matches.Add(New PointPair(pt, nn))
                DrawLine(dst3, nn, pt, cvb.Scalar.White)
            End If
        Next
        If standaloneTest() = False Then knn.trainInput = New List(Of cvb.Point2f)(queries)
    End Sub
End Class






Public Class KNN_Core : Inherits VB_Parent
    Public knn As cvb.ML.KNearest
    Public trainInput As New List(Of cvb.Point2f) ' put training data here
    Public queries As New List(Of cvb.Point2f) ' put Query data here
    Public neighbors As New List(Of List(Of Integer))
    Public result(,) As Integer ' Get results here...
    Public desiredMatches As Integer = -1 ' -1 indicates it is to use the number of queries.
    Dim random As New Random_Basics
    Public Sub New()
        knn = cvb.ML.KNearest.Create()
        labels(2) = "Red=TrainingData, yellow = queries"
        desc = "Train a KNN model and map each query to the nearest training neighbor."
    End Sub
    Public Sub displayResults()
        dst2.SetTo(0)
        Dim dm = Math.Min(trainInput.Count, queries.Count)
        For i = 0 To queries.Count - 1
            Dim pt = queries(i)
            Dim test = result(i, 0)
            If test >= trainInput.Count Or test < 0 Then Continue For
            Dim nn = trainInput(result(i, 0))
            DrawCircle(dst2, pt, task.DotSize + 4, cvb.Scalar.Yellow)
            DrawLine(dst2, pt, nn, cvb.Scalar.Yellow)
        Next

        For Each pt In trainInput
            DrawCircle(dst2, pt, task.DotSize + 4, cvb.Scalar.Red)
        Next
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim KNNdimension = 2

        If standalone Then
            If task.heartBeat Then
                Random.Run(empty)
                trainInput = New List(Of cvb.Point2f)(random.PointList)
            End If
            Random.Run(empty)
            queries = New List(Of cvb.Point2f)(random.PointList)
        End If

        Dim queryMat = cvb.Mat.FromPixelData(queries.Count, KNNdimension, cvb.MatType.CV_32F, queries.ToArray)
        If queryMat.Rows = 0 Then
            SetTrueText("There were no queries provided.  There is nothing to do...")
            Exit Sub
        End If

        If trainInput.Count = 0 Then trainInput = New List(Of cvb.Point2f)(queries) ' first pass, just match the queries.
        Dim trainData = cvb.Mat.FromPixelData(trainInput.Count, KNNdimension, cvb.MatType.CV_32F, trainInput.ToArray)
        Dim response = cvb.Mat.FromPixelData(trainData.Rows, 1, cvb.MatType.CV_32S, Enumerable.Range(start:=0, trainData.Rows).ToArray)
        knn.Train(trainData, cvb.ML.SampleTypes.RowSample, response)
        Dim neighborMat As New cvb.Mat

        Dim dm = If(desiredMatches < 0, trainInput.Count, desiredMatches)
        knn.FindNearest(queryMat, dm, New cvb.Mat, neighborMat)
        If neighborMat.Rows <> queryMat.Rows Or neighborMat.Cols <> dm Then
            debug.writeline("KNN's FindNearest did not return the correct number of neighbors.  Marshal.copy will fail so exit.")
            Exit Sub
        End If

        Dim nData(queryMat.Rows * dm - 1) As Single
        If nData.Length = 0 Then Exit Sub
        Marshal.Copy(neighborMat.Data, nData, 0, nData.Length)

        For i = 0 To nData.Count - 1
            If Math.Abs(nData(i)) > trainInput.Count Then nData(i) = 0 ' value must be within the range of traininput
        Next

        ReDim result(queryMat.Rows - 1, dm - 1)
        neighbors = New List(Of List(Of Integer))
        For i = 0 To queryMat.Rows - 1
            Dim pt = queries(i)
            Dim res = New List(Of Integer)
            For j = 0 To dm - 1
                Dim test = nData(i * dm + j)
                If test < nData.Length And test >= 0 Then
                    result(i, j) = CInt(nData(i * dm + j))
                    Dim index = nData(i * dm + j)
                    res.Add(index)
                End If
            Next
            neighbors.Add(res)
        Next
        If standaloneTest() Then displayResults()
    End Sub
End Class






Public Class KNN_Core2DTest : Inherits VB_Parent
    Public knn As New KNN_Core
    Dim random As New Random_Basics
    Public Sub New()
        FindSlider("Random Pixel Count").Value = 10
        desc = "Test knn with random 2D points in the image.  Find the nearest requested neighbors."
    End Sub
    Public Sub accumulateDisplay()
        Dim dm = Math.Min(knn.trainInput.Count, knn.queries.Count)
        For i = 0 To knn.queries.Count - 1
            Dim pt = knn.queries(i)
            Dim test = knn.result(i, 0)
            If test >= knn.trainInput.Count Or test < 0 Then Continue For
            Dim nn = knn.trainInput(knn.result(i, 0))
            DrawCircle(dst3, pt, task.DotSize + 4, cvb.Scalar.Yellow)
            DrawLine(dst3, pt, nn, cvb.Scalar.Yellow)
        Next

        For Each pt In knn.trainInput
            DrawCircle(dst3, pt, task.DotSize + 4, cvb.Scalar.Red)
        Next
    End Sub

    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            dst3.SetTo(0)
            random.Run(empty)
            knn.trainInput = New List(Of cvb.Point2f)(random.PointList)
        End If
        random.Run(empty)
        knn.queries = New List(Of cvb.Point2f)(random.PointList)

        knn.Run(empty)
        knn.displayResults()
        dst2 = knn.dst2
        accumulateDisplay()

        labels(2) = "The top " + CStr(knn.trainInput.Count) + " best matches are shown. Red=TrainingData, yellow = queries"
    End Sub
End Class







Public Class KNN_Core3D : Inherits VB_Parent
    Public knn As cvb.ML.KNearest
    Public trainInput As New List(Of cvb.Point3f) ' put training data here
    Public queries As New List(Of cvb.Point3f) ' put Query data here
    Public result(,) As Integer ' Get results here...
    Public Sub New()
        knn = cvb.ML.KNearest.Create()
        desc = "Use knn with the input 3D points in the image.  Find the nearest neighbors."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            SetTrueText("There is no output for the " + traceName + " algorithm when run standaloneTest().  Use the " + traceName + "Test algorithm")
            Exit Sub
        End If

        Dim KNNdimension = 3
        Dim queryMat = cvb.Mat.FromPixelData(queries.Count, KNNdimension, cvb.MatType.CV_32F, queries.ToArray)
        If queryMat.Rows = 0 Then
            SetTrueText("There were no queries provided.  There is nothing to do...")
            Exit Sub
        End If

        If trainInput.Count = 0 Then trainInput = New List(Of cvb.Point3f)(queries) ' first pass, just match the queries.
        Dim trainData = cvb.Mat.FromPixelData(trainInput.Count, KNNdimension, cvb.MatType.CV_32F, trainInput.ToArray)
        Dim response = cvb.Mat.FromPixelData(trainData.Rows, 1, cvb.MatType.CV_32S, Enumerable.Range(start:=0, trainData.Rows).ToArray)
        knn.Train(trainData, cvb.ML.SampleTypes.RowSample, response)
        Dim neighbors As New cvb.Mat
        Dim dm = trainInput.Count
        knn.FindNearest(queryMat, dm, New cvb.Mat, neighbors)

        Dim nData(queryMat.Rows * dm - 1) As Single
        Marshal.Copy(neighbors.Data, nData, 0, nData.Length)

        ReDim result(queryMat.Rows - 1, dm - 1)
        For i = 0 To queryMat.Rows - 1
            For j = 0 To dm - 1
                Dim test = nData(i * dm + j)
                If test < nData.Length And test >= 0 Then result(i, j) = CInt(nData(i * dm + j))
            Next
        Next
    End Sub
End Class






Public Class KNN_Core4D : Inherits VB_Parent
    Public knn As cvb.ML.KNearest
    Public trainInput As New List(Of cvb.Vec4f) ' put training data here
    Public queries As New List(Of cvb.Vec4f) ' put Query data here
    Public result(,) As Integer ' Get results here...
    Public Sub New()
        knn = cvb.ML.KNearest.Create()
        labels(2) = "Red=TrainingData, yellow = queries, text shows Z distance to that point from query point"
        desc = "Use knn with the input 4D points in the image.  Find the nearest neighbors."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            SetTrueText("There is no output for the " + traceName + " algorithm when run standaloneTest().  Use the " + traceName + "Test algorithm")
            Exit Sub
        End If

        Dim KNNdimension = 4
        Dim queryMat = cvb.Mat.FromPixelData(queries.Count, KNNdimension, cvb.MatType.CV_32F, queries.ToArray)
        If queryMat.Rows = 0 Then
            SetTrueText("There were no queries provided.  There is nothing to do...")
            Exit Sub
        End If

        If trainInput.Count = 0 Then trainInput = New List(Of cvb.Vec4f)(queries) ' first pass, just match the queries.
        Dim trainData = cvb.Mat.FromPixelData(trainInput.Count, KNNdimension, cvb.MatType.CV_32F, trainInput.ToArray)
        Dim response = cvb.Mat.FromPixelData(trainData.Rows, 1, cvb.MatType.CV_32S, Enumerable.Range(start:=0, trainData.Rows).ToArray)
        knn.Train(trainData, cvb.ML.SampleTypes.RowSample, response)
        Dim neighbors As New cvb.Mat
        Dim dm = trainInput.Count
        knn.FindNearest(queryMat, dm, New cvb.Mat, neighbors)

        Dim nData(queryMat.Rows * dm - 1) As Single
        Marshal.Copy(neighbors.Data, nData, 0, nData.Length)

        ReDim result(queryMat.Rows - 1, dm - 1)
        For i = 0 To queryMat.Rows - 1
            For j = 0 To dm - 1
                Dim test = nData(i * dm + j)
                If test < nData.Length And test >= 0 Then result(i, j) = CInt(nData(i * dm + j))
            Next
        Next
    End Sub
End Class






Public Class KNN_CoreN : Inherits VB_Parent
    Public knn As cvb.ML.KNearest
    Public trainInput As New List(Of Single) ' put training data here
    Public queries As New List(Of Single) ' put Query data here
    Public result(,) As Integer ' Get results here...
    Dim messageSent As Boolean
    Public options As New Options_KNN
    Public Sub New()
        knn = cvb.ML.KNearest.Create()
        desc = "Generalize the use knn with X input points.  Find the nearest requested neighbors."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim responseList As IEnumerable(Of Integer) = Enumerable.Range(0, 10).Select(Function(x) x)
        If standaloneTest() Then
            SetTrueText("There is no output for the " + traceName + " algorithm when run standaloneTest().  Use the " + traceName + "_Test algorithm")
            Exit Sub
        End If

        If options.knnDimension = 0 Then
            If messageSent = False Then MsgBox("The KNN dimension needs to be set for the general purpose KNN_Core to start")
            Exit Sub
        End If

        Dim qRows = CInt(queries.Count / options.knnDimension)
        If qRows = 0 Then
            SetTrueText("There were no queries provided.  There is nothing to do...")
            Exit Sub
        End If
        Dim queryMat = cvb.Mat.FromPixelData(qRows, options.knnDimension, cvb.MatType.CV_32F, queries.ToArray)

        Dim trainData = cvb.Mat.FromPixelData(CInt(trainInput.Count / options.knnDimension), options.knnDimension, cvb.MatType.CV_32F, trainInput.ToArray)
        Dim response = cvb.Mat.FromPixelData(trainData.Rows, 1, cvb.MatType.CV_32S, Enumerable.Range(start:=0, trainData.Rows).ToArray)
        knn.Train(trainData, cvb.ML.SampleTypes.RowSample, response)
        Dim neighbors As New cvb.Mat
        Dim dm = trainInput.Count
        knn.FindNearest(queryMat, dm, New cvb.Mat, neighbors)

        ReDim result(neighbors.Rows - 1, neighbors.Cols - 1)
        For i = 0 To neighbors.Rows - 1
            For j = 0 To neighbors.Cols - 1
                Dim test = neighbors.Get(Of Single)(i, j)
                If test < trainInput.Count And test >= 0 Then result(i, j) = neighbors.Get(Of Single)(i, j)
            Next
        Next
    End Sub
End Class








Public Class KNN_Core3DTest : Inherits VB_Parent
    Dim knn As New KNN_Core3D
    Dim dist As New Distance_Point3D
    Dim random As New Random_Basics3D
    Public Sub New()
        labels(2) = "Red=TrainingData, yellow = queries, text shows Euclidean distance to that point from query point"
        FindSlider("Random Pixel Count").Value = 100
        desc = "Validate that knn works with random 3D points in the image.  Find the nearest requested neighbors."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            knn.queries.Clear()
            knn.trainInput.Clear()
            random.Run(empty)
            For Each pt In random.PointList
                Dim vec = task.pointCloud.Get(Of cvb.Point3f)(pt.Y, pt.X)
                If knn.trainInput.Count = 10 Then
                    If vec.Z Then
                        knn.queries.Add(pt)
                        Exit For
                    End If
                Else
                    If vec.Z Then knn.trainInput.Add(pt)
                End If
            Next
        End If

        knn.Run(empty)

        dst2.SetTo(0)
        dist.inPoint1 = knn.queries(0)
        For i = 0 To knn.trainInput.Count - 1
            Dim pt = New cvb.Point2f(knn.trainInput(i).X, knn.trainInput(i).Y)
            DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Red)
            dist.inPoint2 = knn.trainInput(i)
            dist.Run(src)
            SetTrueText("depth=" + CStr(knn.trainInput(i).Z) + vbCrLf + "dist=" + Format(dist.distance, fmt0), pt)
        Next
        For i = 0 To knn.queries.Count - 1
            Dim pt = New cvb.Point2f(knn.queries(i).X, knn.queries(i).Y)
            For j = 0 To Math.Min(2, knn.trainInput.Count) - 1
                Dim index = knn.result(i, j)
                If index >= knn.trainInput.Count Or index < 0 Then Continue For
                Dim nn = New cvb.Point2f(knn.trainInput(index).X, knn.trainInput(index).Y)
                DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Yellow)
                DrawLine(dst2, pt, nn, cvb.Scalar.Yellow)
                Dim midPt = New cvb.Point2f((pt.X + nn.X) / 2, (pt.Y + nn.Y) / 2)
                SetTrueText(CStr(j), midPt)
                SetTrueText("depth=" + CStr(knn.queries(i).Z), pt)
            Next
        Next
    End Sub
End Class








Public Class KNN_Core4DTest : Inherits VB_Parent
    Dim knn As New KNN_Core4D
    Dim dist As New Distance_Point4D
    Dim random As New Random_Basics4D
    Public Sub New()
        labels(2) = "Red=TrainingData, yellow = queries, text shows Euclidean distance to that point from query point"
        FindSlider("Random Pixel Count").Value = 5
        desc = "Validate that knn works with random 3D points in the image.  Find the nearest requested neighbors."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            random.Run(empty)
            knn.trainInput = New List(Of cvb.Vec4f)(random.PointList)
            knn.queries.Clear()
            knn.queries.Add(New cvb.Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, dst2.Height), msRNG.Next(0, dst2.Height)))
        End If

        knn.Run(empty)

        dst2.SetTo(0)
        dist.inPoint1 = knn.queries(0)
        For i = 0 To knn.trainInput.Count - 1
            Dim pt = New cvb.Point2f(knn.trainInput(i)(0), knn.trainInput(i)(1))
            DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Red)
            dist.inPoint2 = knn.trainInput(i)
            dist.Run(src)
            SetTrueText("dist=" + Format(dist.distance, fmt0), pt)
        Next
        For i = 0 To knn.queries.Count - 1
            Dim pt = New cvb.Point2f(knn.queries(i)(0), knn.queries(i)(1))
            For j = knn.result.GetLowerBound(1) To knn.result.GetUpperBound(1)
                Dim index = knn.result(i, j)
                If index >= knn.trainInput.Count Or index < 0 Then Continue For
                Dim nn = New cvb.Point2f(knn.trainInput(index)(0), knn.trainInput(index)(1))
                DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Yellow)
                DrawLine(dst2, pt, nn, task.HighlightColor)
                Dim midPt = New cvb.Point2f((pt.X + nn.X) / 2, (pt.Y + nn.Y) / 2)
                SetTrueText(CStr(j), midPt)
            Next
        Next
    End Sub
End Class








Public Class KNN_CoreNTest : Inherits VB_Parent
    Dim knn As New KNN_CoreN
    Public Sub New()
        labels(2) = "Highlight color (Yellow) is query.  The red dots are the training set."
        desc = "Test the use of the general form KNN_CoreN algorithm"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            knn.trainInput.Clear()
            For i = 0 To knn.options.numPoints - 1
                For j = 0 To knn.options.knnDimension - 1
                    knn.trainInput.Add(msRNG.Next(dst2.Height))
                Next
            Next

            knn.queries.Clear()
            For j = 0 To knn.options.knnDimension - 1
                knn.queries.Add(msRNG.Next(dst2.Height))
            Next
        End If

        knn.Run(empty)
        dst2.SetTo(0)
        For i = 0 To knn.trainInput.Count - 1 Step knn.options.knnDimension
            Dim pt = New cvb.Point2f(knn.trainInput(i), knn.trainInput(i + 1))
            DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Red)
        Next
        For i = 0 To knn.queries.Count - 1 Step knn.options.knnDimension
            Dim pt = New cvb.Point2f(knn.queries(i), knn.queries(i + 1))
            Dim index = knn.result(i, 0)
            If index * knn.options.knnDimension >= knn.trainInput.Count Or index < 0 Then Continue For
            Dim nn = New cvb.Point2f(knn.trainInput(index * knn.options.knnDimension), knn.trainInput(index * knn.options.knnDimension + 1))
            DrawCircle(dst2, pt, task.DotSize + 1, task.HighlightColor)
            DrawLine(dst2, pt, nn, task.HighlightColor)
        Next
        If standaloneTest() Then
            SetTrueText("Results are easily verified for the 2-dimensional case.  For higher dimension, " + vbCrLf +
                        "the results may appear incorrect because the higher dimensions are projected into " + vbCrLf +
                        "a 2-dimensional presentation.", 3)
        End If
    End Sub
End Class







Public Class KNN_Emax : Inherits VB_Parent
    Dim random As New Random_Basics
    Public knn As New KNN_Core
    Dim em As New EMax_Basics
    Public Sub New()
        labels(2) = "Output from Emax"
        labels(3) = "Red=TrainingData, yellow = queries - use EMax sigma to introduce more chaos."
        desc = "Emax centroids move but here KNN is used to matched the old and new locations and keep the colors the same."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        em.Run(src)
        random.Run(empty)

        knn.queries = New List(Of cvb.Point2f)(em.centers)
        knn.Run(src)
        dst2 = em.dst2 + knn.dst2

        knn.displayResults()
        dst3 = knn.dst2

        knn.trainInput = New List(Of cvb.Point2f)(knn.queries)
    End Sub
End Class








Public Class KNN_TrackMean : Inherits VB_Parent
    Dim plot As New Plot_Histogram
    Dim knn As New KNN_Basics
    Dim feat As New Feature_Basics
    Const maxDistance As Integer = 50
    Public shiftX As Single
    Public shiftY As Single
    Dim motionTrack As New List(Of cvb.Point2f)
    Dim lastImage As cvb.Mat
    Dim dotSlider As TrackBar
    Dim options As New Options_KNN
    Public Sub New()
        FindSlider("Feature Sample Size").Value = 200
        dotSlider = FindSlider("Average distance multiplier")
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "Histogram of Y-Axis camera motion", "Yellow points are good features and the white trail in the center estimates camera motion.", "Histogram of X-Axis camera motion"}
        desc = "Track points with KNN and match the goodFeatures from frame to frame"
    End Sub
    Private Function plotDiff(diffList As List(Of Integer), xyStr As String, labelImage As Integer, ByRef label As String) As Single
        Dim count = diffList.Max - diffList.Min + 1
        Dim hist(maxDistance - 1) As Single
        Dim zeroLoc = hist.Count / 2
        Dim nonZero As Integer
        Dim zeroCount As Integer
        For Each diff In diffList
            If diff <> 0 Then nonZero += 1 Else zeroCount += 1
            diff += zeroLoc
            If diff >= maxDistance Then diff = maxDistance - 1
            If diff < 0 Then diff = 0
            hist(diff) += 1
        Next
        plot.Run(cvb.Mat.FromPixelData(hist.Count, 1, cvb.MatType.CV_32F, hist.ToArray))
        Dim histList = hist.ToList
        Dim maxVal = histList.Max
        Dim maxIndex = histList.IndexOf(maxVal)
        plot.maxValue = Math.Ceiling((maxVal + 50) - (maxVal + 50) Mod 50)
        label = xyStr + "Max count = " + CStr(maxVal) + " at " + CStr(maxIndex - zeroLoc) + " with " + CStr(nonZero) + " non-zero values or " +
                             Format(nonZero / (nonZero + zeroCount), "0%")

        Dim histSum As Single
        For i = 0 To histList.Count - 1
            histSum += histList(i) * (i - zeroLoc)
        Next
        Return histSum / histList.Count
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        If task.FirstPass Then lastImage = src.Clone
        Dim multiplier = dotSlider.Value

        feat.Run(src)

        knn.queries = New List(Of cvb.Point2f)(task.features)
        knn.Run(src)

        Dim diffX As New List(Of Integer)
        Dim diffY As New List(Of Integer)
        Dim correlationMat As New cvb.Mat
        dst2 = src.Clone
        Dim sz = task.gridSize
        For Each mps In knn.matches
            Dim currRect = ValidateRect(New cvb.Rect(mps.p1.X - sz, mps.p1.Y - sz, sz * 2, sz * 2))
            Dim prevRect = ValidateRect(New cvb.Rect(mps.p2.X - sz, mps.p2.Y - sz, currRect.Width, currRect.Height))
            cvb.Cv2.MatchTemplate(lastImage(prevRect), src(currRect), correlationMat, feat.options.matchOption)
            Dim corrNext = correlationMat.Get(Of Single)(0, 0)
            DrawCircle(dst2, mps.p1, task.DotSize, task.HighlightColor)
            diffX.Add(mps.p1.X - mps.p2.X)
            diffY.Add(mps.p1.Y - mps.p2.Y)
        Next

        If diffX.Count = 0 Or diffY.Count = 0 Then Exit Sub

        Dim xLabel As String, yLabel As String
        shiftX = multiplier * plotDiff(diffX, " X ", 3, xLabel)
        dst3 = plot.dst2.Clone
        dst3.Line(New cvb.Point(plot.plotCenter, 0), New cvb.Point(plot.plotCenter, dst2.Height), cvb.Scalar.White, 1)

        shiftY = multiplier * plotDiff(diffY, " Y ", 1, yLabel)
        dst1 = plot.dst2
        dst1.Line(New cvb.Point(plot.plotCenter, 0), New cvb.Point(plot.plotCenter, dst2.Height), cvb.Scalar.White, 1)

        lastImage = src.Clone

        motionTrack.Add(New cvb.Point2f(shiftX + dst2.Width / 2, shiftY + dst2.Height / 2))
        If motionTrack.Count > task.fpsRate Then motionTrack.RemoveAt(0)
        Dim lastpt = motionTrack(0)
        For Each pt In motionTrack
            DrawLine(dst2, pt, lastpt, cvb.Scalar.White)
            lastpt = pt
        Next
        SetTrueText(yLabel, 1)
        SetTrueText(xLabel, 3)
    End Sub
End Class







Public Class KNN_ClosestTracker : Inherits VB_Parent
    Public lines As New Line_Basics
    Public lastPair As New PointPair
    Public trainInput As New List(Of cvb.Point2f)
    Dim minDistances As New List(Of Single)
    Public Sub New()
        labels = {"", "", "Highlight the tracked line (move camera to see track results)", "Candidate lines - standaloneTest() only"}
        desc = "Find the longest line and keep finding it among the list of lines using a minimized KNN test."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.Clone

        Dim p1 As cvb.Point2f, p2 As cvb.Point2f
        If trainInput.Count = 0 Then
            lines.Run(src)
            dst3 = lines.dst2
        Else
            p1 = lastPair.p1
            p2 = lastPair.p2
        End If

        For Each lp In lines.lpList
            If trainInput.Count = 0 Then
                p1 = lp.p1
                p2 = lp.p2
            End If
            trainInput.Add(lp.p1)
            trainInput.Add(lp.p2)
            If trainInput.Count >= 10 Then Exit For
        Next

        If trainInput.Count = 0 Then
            SetTrueText("No lines were found in the current image.")
            Exit Sub
        End If

        If lastPair.compare(New PointPair) Then lastPair = New PointPair(p1, p2)
        Dim distances As New List(Of Single)
        For i = 0 To trainInput.Count - 1 Step 2
            Dim pt1 = trainInput(i)
            Dim pt2 = trainInput(i + 1)
            distances.Add(Math.Min(pt1.DistanceTo(lastPair.p1) + pt2.DistanceTo(lastPair.p2), pt1.DistanceTo(lastPair.p2) + pt2.DistanceTo(lastPair.p2)))
        Next

        Dim minDist = distances.Min
        Dim index = distances.IndexOf(minDist) * 2
        p1 = trainInput(index)
        p2 = trainInput(index + 1)

        If minDistances.Count > 0 Then
            If minDist > minDistances.Max * 2 Then
                debug.writeline("Overriding KNN min Distance Rule = " + Format(minDist, fmt0) + " max = " + Format(minDistances.Max, fmt0))
                lastPair = New PointPair(trainInput(0), trainInput(1))
            Else
                lastPair = New PointPair(p1, p2)
            End If
        Else
            lastPair = New PointPair(p1, p2)
        End If

        If minDist > 0 Then minDistances.Add(minDist)
        If minDistances.Count > 100 Then minDistances.RemoveAt(0)

        DrawLine(dst2, p1, p2, task.HighlightColor)
        trainInput.Clear()
    End Sub
End Class






Public Class KNN_ClosestLine : Inherits VB_Parent
    Public lastP1 As cvb.Point2f
    Public lastP2 As cvb.Point2f
    Public lastIndex As Integer
    Public trainInput As New List(Of cvb.Point2f)
    Public Sub New()
        desc = "Try to find the closest pair of points in the traininput.  Dynamically compute distance ceiling to determine when to report fail."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.Clone

        If lastP1 = New cvb.Point2f Then
            SetTrueText("KNN_ClosestLine is only run with other KNN algorithms" + vbCrLf +
                        "lastP1 and lastP2 need to be initialized by the other algorithm." + vbCrLf +
                        "Initialize with a pair of points to track a line. ", 3)
            Exit Sub
        End If

        Dim distances As New List(Of Single)
        For i = 0 To trainInput.Count - 1 Step 2
            Dim pt1 = trainInput(i)
            Dim pt2 = trainInput(i + 1)
            distances.Add(Math.Min(pt1.DistanceTo(lastP1) + pt2.DistanceTo(lastP2), pt1.DistanceTo(lastP2) + pt2.DistanceTo(lastP2)))
        Next

        Dim minDist = distances.Min
        lastIndex = distances.IndexOf(minDist) * 2
        lastP1 = trainInput(lastIndex)
        lastP2 = trainInput(lastIndex + 1)

        Static minDistances As New List(Of Single)({distances(0)})
        If minDist > minDistances.Max * 4 Then
            debug.writeline("Overriding KNN min Distance Rule = " + Format(minDist, fmt0) + " max = " + Format(minDistances.Max, fmt0))
            lastP1 = trainInput(0)
            lastP2 = trainInput(1)
        End If

        ' track the last 100 non-zero minDist values to use as a guide to determine when a line was lost and a new pair has to be used.
        If minDist > 0 Then minDistances.Add(minDist)
        If minDistances.Count > 100 Then minDistances.RemoveAt(0)

        DrawLine(dst2, lastP1, lastP2, task.HighlightColor)
        trainInput.Clear()
    End Sub
End Class









Public Class KNN_ClosestVertical : Inherits VB_Parent
    Public lines As New FeatureLine_Finder
    Public knn As New KNN_ClosestLine
    Public pt1 As New cvb.Point3f
    Public pt2 As New cvb.Point3f
    Public Sub New()
        labels = {"", "", "Highlight the tracked line", "Candidate vertical lines are in Blue"}
        desc = "Test the code find the longest line and track it using a minimized KNN test."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.Clone

        lines.Run(src)
        If lines.sortedVerticals.Count = 0 Then
            SetTrueText("No vertical lines were found.")
            Exit Sub
        End If

        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim lastDistance = knn.lastP1.DistanceTo(knn.lastP2)
        Dim bestDistance = lines.lines2D(index).DistanceTo(lines.lines2D(index + 1))
        If knn.lastP1 = New cvb.Point2f Or lastDistance < 0.75 * bestDistance Then
            knn.lastP1 = lines.lines2D(index)
            knn.lastP2 = lines.lines2D(index + 1)
        End If

        knn.trainInput.Clear()
        For i = 0 To lines.sortedVerticals.Count - 1
            index = lines.sortedVerticals.ElementAt(i).Value
            knn.trainInput.Add(lines.lines2D(index))
            knn.trainInput.Add(lines.lines2D(index + 1))
        Next

        knn.Run(src)

        pt1 = lines.lines3D(knn.lastIndex)
        pt2 = lines.lines3D(knn.lastIndex + 1)

        dst3 = lines.dst3
        DrawLine(dst2, knn.lastP1, knn.lastP2, task.HighlightColor)
    End Sub
End Class








Public Class KNN_BasicsOld : Inherits VB_Parent
    Public matches As New List(Of PointPair)
    Public noMatch As New List(Of cvb.Point)
    Public knn As New KNN_Core
    Public queries As New List(Of cvb.Point2f)
    Dim random As New Random_Basics
    Public Sub New()
        labels(2) = "KNN_Core output with many-to-one results"
        labels(3) = "KNN_BasicsOld output with just the closest match.  Red = training data, yellow = queries."
        desc = "Map points 1:1 with losses.  When duplicates are found, toss the farthest.  Too hard to follow.  Trying a better approach."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            If task.heartBeat Then
                Random.Run(empty)
                knn.trainInput = New List(Of cvb.Point2f)(random.PointList)
            End If
            Random.Run(empty)
            queries = New List(Of cvb.Point2f)(random.PointList)
        End If

        If queries.Count = 0 Then
            SetTrueText("Place some input points in queries before starting the knn run.")
            Exit Sub
        End If

        knn.queries = queries
        knn.Run(empty)
        knn.displayResults()
        dst2 = knn.dst2

        Dim nearest As New List(Of Integer)
        ' map the points 1 to 1: find duplicates, choose which is better.
        ' loser must relinquish the training data element
        Dim sortedResults As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        For i = 0 To queries.Count - 1
            nearest.Add(knn.result(i, 0))
            sortedResults.Add(knn.result(i, 0), i)
        Next

        For i = 0 To sortedResults.Count - 2 ' we are comparing each element to the next
            Dim resultA = sortedResults.ElementAt(i).Key
            Dim resultB = sortedResults.ElementAt(i + 1).Key
            If resultA = resultB Then
                Dim nn = knn.trainInput(resultA)
                Dim queryA = sortedResults.ElementAt(i).Value
                For j = i + 1 To sortedResults.Count - 1
                    resultB = sortedResults.ElementAt(j).Key
                    If resultA <> resultB Then Exit For
                    Dim queryB = sortedResults.ElementAt(j).Value
                    Dim p1 = queries(queryA)
                    Dim p2 = queries(queryB)
                    Dim distance1 = Math.Sqrt((p1.X - nn.X) * (p1.X - nn.X) + (p1.Y - nn.Y) * (p1.Y - nn.Y))
                    Dim distance2 = Math.Sqrt((p2.X - nn.X) * (p2.X - nn.X) + (p2.Y - nn.Y) * (p2.Y - nn.Y))
                    If distance1 < distance2 Then
                        nearest(queryB) = -1
                    Else
                        nearest(queryA) = -1
                        queryA = queryB
                    End If
                Next
            End If
        Next

        dst3.SetTo(0)
        For Each pt In knn.trainInput
            DrawCircle(dst3, pt, task.DotSize + 4, cvb.Scalar.Red)
        Next

        noMatch.Clear()
        matches.Clear()
        For i = 0 To queries.Count - 1
            Dim pt = queries(i)
            DrawCircle(dst3, pt, task.DotSize + 4, cvb.Scalar.Yellow)
            If nearest(i) = -1 Then
                noMatch.Add(pt)
            Else
                If nearest(i) < knn.trainInput.Count Then ' there seems like a boundary condition when there is only 1 traininput...
                    Dim nn = knn.trainInput(nearest(i))
                    matches.Add(New PointPair(pt, nn))
                    DrawLine(dst3, nn, pt, cvb.Scalar.White)
                End If
            End If
        Next
        If standaloneTest() = False Then knn.trainInput = New List(Of cvb.Point2f)(queries)
    End Sub
End Class








Public Class KNN_Farthest : Inherits VB_Parent
    Dim knn As New KNN_Core
    Public mpFar As PointPair
    Dim random As New Random_Basics
    Public Sub New()
        labels = {"", "", "Lines connecting pairs that are farthest.", "Training Input which is also query input and longest line"}
        desc = "Use KNN to find the farthest point from each query point."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            If task.heartBeat Then
                Random.Run(empty)
                knn.trainInput = New List(Of cvb.Point2f)(random.PointList)
                knn.queries = New List(Of cvb.Point2f)(knn.trainInput)
            End If
        End If

        knn.Run(empty)

        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim farthest = New List(Of PointPair)
        Dim distances As New List(Of Single)
        For i = 0 To knn.result.GetUpperBound(0) - 1
            Dim farIndex = knn.result(i, knn.result.GetUpperBound(1))
            Dim mp = New PointPair(knn.queries(i), knn.trainInput(farIndex))
            DrawCircle(dst2,mp.p1, task.DotSize + 4, cvb.Scalar.Yellow)
            DrawCircle(dst2,mp.p2, task.DotSize + 4, cvb.Scalar.Yellow)
            DrawLine(dst2, mp.p1, mp.p2, cvb.Scalar.Yellow)
            farthest.Add(mp)
            distances.Add(mp.p1.DistanceTo(mp.p2))
        Next

        For Each pt In knn.queries
            DrawCircle(dst3,pt, task.DotSize + 4, cvb.Scalar.Red)
        Next

        Dim maxIndex = distances.IndexOf(distances.Max())
        mpFar = farthest(maxIndex)
        DrawLine(dst3, mpFar.p1, mpFar.p2, cvb.Scalar.White)
    End Sub
End Class









Public Class KNN_TrackEach : Inherits VB_Parent
    Dim knn As New KNN_Basics
    Dim feat As New Feature_Basics
    Dim trackAll As New List(Of List(Of PointPair))
    Public Sub New()
        desc = "Track each good feature with KNN and match the goodFeatures from frame to frame"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim minDistance = feat.options.minDistance
        ' if there was no motion, use minDistance to eliminate the unstable points.
        If task.motionFlag = False Or task.optionsChanged = False Then minDistance = 2

        feat.Run(src)

        knn.queries = New List(Of cvb.Point2f)(task.features)
        knn.Run(src)

        Dim tracker As New List(Of PointPair)
        dst2 = src.Clone
        For Each mp In knn.matches
            If mp.p1.DistanceTo(mp.p2) < minDistance Then tracker.Add(mp)
        Next

        trackAll.Add(tracker)

        For i = 0 To trackAll.Count - 1 Step 2
            Dim t1 = trackAll(i)
            For Each mp In t1
                DrawCircle(dst2,mp.p1, task.DotSize, task.HighlightColor)
                DrawCircle(dst2,mp.p2, task.DotSize, task.HighlightColor)
                DrawLine(dst2, mp.p1, mp.p2, cvb.Scalar.Red)
            Next
        Next

        labels(2) = CStr(task.features.Count) + " good features were tracked across " + CStr(task.frameHistoryCount) + " frames."
        SetTrueText(labels(2) + vbCrLf + "The highlighted dots are the good feature points", 3)

        If trackAll.Count > task.frameHistoryCount Then trackAll.RemoveAt(0)
    End Sub
End Class
