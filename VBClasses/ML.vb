Imports System.Runtime.InteropServices
Imports OpenCvSharp.ML
Imports VBClasses
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class ML_Basics : Inherits TaskParent
    Implements IDisposable
    Public trainMats() As Mat ' all entries are 32FCx
    Public trainResponse As Mat ' 32FC1 format
    Public testMats() As Mat ' all entries are 32FCx
    Public predictions As New Mat
    Public options As New Options_ML
    Public buildEveryPass As Boolean
    Dim classifier As Object
    Dim normalBayes As NormalBayesClassifier
    Dim knearest As KNearest
    Dim svm As SVM
    Dim dtrees As DTrees
    Dim boost As Boost
    Dim ann_mlp As ANN_MLP
    Dim logistic As LogisticRegression
    Dim rtrees As RTrees
    Public Sub New()
        desc = "Simplify the prep for ML data train and test data and run with ML algorithms."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            SetTrueText("ML_BasicsRTree has no output when run standalone." + vbCrLf + "Use LowResOld_Depth to test.")
            Exit Sub
        End If

        options.Run()
        labels(2) = "ML algorithm selected is " + options.ML_Name

        Dim trainMat As New Mat
        Merge(trainMats, trainMat)

        Dim varCount As Integer
        For Each m In trainMats
            varCount += m.ElemSize / 4 ' how many 32f variables in this Mat?
        Next

        trainMat = Mat.FromPixelData(trainMat.Total, varCount, MatType.CV_32F, trainMat.Data)
        Dim responseMat = Mat.FromPixelData(trainMats(0).Total, 1, MatType.CV_32F, trainResponse.Data)

        Dim respFormat = MatType.CV_32F
        If task.heartBeat Or buildEveryPass Then
            Select Case options.ML_Name
                Case "NormalBayesClassifier"
                    normalBayes = ML.NormalBayesClassifier.Create()
                    respFormat = MatType.CV_32S
                    classifier = normalBayes
                Case "KNearest"
                    knearest = ML.KNearest.Create()
                    knearest.DefaultK = 15
                    knearest.IsClassifier = True
                    classifier = knearest
                Case "SVM"
                    svm = ML.SVM.Create()
                    svm.C = 1
                    svm.TermCriteria = TermCriteria.Both(1000, 0.01)
                    svm.P = 0
                    svm.Nu = 0.5
                    svm.Coef0 = 1
                    svm.Gamma = 1
                    svm.Degree = 0.5
                    svm.KernelType = SVM.KernelTypes.Poly
                    svm.Type = SVM.Types.CSvc
                    respFormat = MatType.CV_32S
                    classifier = svm
                Case "DTrees"
                    dtrees = ML.DTrees.Create()
                    dtrees.CVFolds = 0
                    dtrees.TruncatePrunedTree = False
                    dtrees.UseSurrogates = False
                    dtrees.MinSampleCount = 2
                    dtrees.MaxDepth = 8
                    dtrees.Use1SERule = False
                    classifier = dtrees
                Case "Boost"
                    boost = ML.Boost.Create()
                    respFormat = MatType.CV_32S
                    boost.BoostType = Boost.Types.Discrete
                    boost.WeakCount = 100
                    boost.WeightTrimRate = 0.95
                    boost.MaxDepth = 2
                    boost.UseSurrogates = False
                    boost.Priors = New Mat()
                    classifier = boost

                Case "ANN_MLP" ' artificial neural net with multi-layer perceptron
                    ann_mlp = ML.ANN_MLP.Create()

                    ' input layer, hidden layer, output layer
                    ann_mlp.SetLayerSizes(Mat.FromPixelData(1, 3, MatType.CV_32SC1, {varCount, 5, 1}))

                    ann_mlp.SetActivationFunction(ML.ANN_MLP.ActivationFunctions.SigmoidSym, 1, 1)
                    ann_mlp.TermCriteria = TermCriteria.Both(1000, 0.000001)
                    ann_mlp.SetTrainMethod(ML.ANN_MLP.TrainingMethods.BackProp, 0.1, 0.1)
                    classifier = ann_mlp
                Case "LogisticRegression"
                    If logistic Is Nothing Then logistic = ML.LogisticRegression.Create()
                    classifier = logistic
                Case Else
                    If rtrees Is Nothing Then rtrees = ML.RTrees.Create()
                    rtrees.MinSampleCount = 2
                    rtrees.MaxDepth = 4
                    rtrees.RegressionAccuracy = 0.0
                    rtrees.UseSurrogates = False
                    rtrees.MaxCategories = 16
                    rtrees.Priors = New Mat
                    rtrees.CalculateVarImportance = False
                    rtrees.ActiveVarCount = varCount
                    rtrees.TermCriteria = TermCriteria.Both(5, 0)
                    classifier = rtrees
            End Select

            If responseMat.Type <> respFormat Then responseMat.ConvertTo(responseMat, respFormat)
            rtrees.Train(trainMat, ML.SampleTypes.RowSample, responseMat)
        End If
        Dim testMat As New Mat
        Merge(testMats, testMat)

        testMat = Mat.FromPixelData(testMat.Total, varCount, MatType.CV_32F, testMat.Data)
        rtrees.Predict(testMat, predictions)

        If predictions.Type <> MatType.CV_32F Then
            predictions.ConvertTo(predictions, MatType.CV_32F)
        End If
    End Sub
    Protected Overrides Sub Finalize()
        If normalBayes IsNot Nothing Then normalBayes.Dispose()
        If knearest IsNot Nothing Then knearest.Dispose()
        If svm IsNot Nothing Then svm.Dispose()
        If dtrees IsNot Nothing Then dtrees.Dispose()
        If boost IsNot Nothing Then boost.Dispose()
        If ann_mlp IsNot Nothing Then ann_mlp.Dispose()
        If logistic IsNot Nothing Then logistic.Dispose()
        If rtrees IsNot Nothing Then rtrees.Dispose()
    End Sub
End Class




Public Class XR_Brick_MLColorDepth : Inherits TaskParent
    Dim ml As New ML_Basics
    Dim bounds As New XR_Brick_FeaturesAndEdges
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        ml.buildEveryPass = True
        dst1 = New Mat(dst2.Size, MatType.CV_8U)
        desc = "Train an ML tree to predict each pixel of the boundary cells using color and depth from boundary neighbors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bounds.Run(src)
        Dim edgeMask = task.edges.dst2

        Dim rgb32f As New Mat, tmp As New Mat
        src.ConvertTo(rgb32f, MatType.CV_32FC3)

        dst1 = bounds.feat.fLessMask
        Dim trainRGB As Mat, trainDepth As Mat
        For i = 0 To bounds.boundaryCells.Count - 1
            Dim nList = bounds.boundaryCells(i)

            ' the first grid square is the center one and the only grid square with edges.  The rest are featureless.
            Dim r = task.gridRects(nList(0))
            Dim edgePixels As New Mat
            FindNonZero(edgeMask(r), edgePixels)

            ' mark the edge pixels as class 2 - others will be updated next
            ml.trainResponse = New Mat(nList.Count + edgePixels.Rows - 1, 1,
                                               MatType.CV_32F, New Scalar(2))
            trainRGB = New Mat(ml.trainResponse.Rows, 1, MatType.CV_32FC3)
            trainDepth = New Mat(ml.trainResponse.Rows, 1, MatType.CV_32F)

            For j = 1 To nList.Count - 1
                Dim grA = task.gridRects(nList(j))
                Dim x As Integer = Math.Floor(grA.X * task.bricksPerRow / task.cols)
                Dim y As Integer = Math.Floor(grA.Y * task.bricksPerCol / task.rows)
                Dim val = task.lowResColor.Get(Of Vec3f)(y, x)
                trainRGB.Set(Of Vec3f)(j - 1, 0, val)
                trainDepth.Set(Of Single)(j - 1, 0, task.lowResDepth.Get(Of Single)(y, x))
                ml.trainResponse.Set(Of Single)(j - 1, 0, 1)
            Next

            ' next, add the edge pixels in the target cell - they are the feature identifiers.
            Dim index = nList.Count - 1
            For j = 0 To edgePixels.Rows - 1
                Dim pt = edgePixels.Get(Of cv.Point)(j, 0)
                Dim val = rgb32f(r).Get(Of Vec3f)(pt.Y, pt.X)
                trainRGB.Set(Of Vec3f)(index + j, 0, val) ' ml.trainResponse already set to 2
                Dim depth = task.pcSplit(2)(r).Get(Of Single)(pt.Y, pt.X)
                trainDepth.Set(Of Single)(index + j, 0, depth)
            Next

            ml.trainMats = {trainRGB, trainDepth}

            Dim grB = task.gridRects(nList(0))
            ml.testMats = {rgb32f(grB), task.pcSplit(2)(grB)}
            ml.Run(src)

            Dim _thr1 As New Mat
            Threshold(ml.predictions, dst1(grB), 1.5, 255, ThresholdTypes.BinaryInv)
            dst1(grB) = dst1(grB).Reshape(1, grB.Height)
            ConvertScaleAbs(dst1(grB), dst1(grB))
        Next

        dst2.SetTo(0)
        src.CopyTo(dst2, dst1)

        dst3.SetTo(0)
        src.CopyTo(dst3, Not dst1)

        labels = {"Src image with edges.", "Src featureless regions", ml.options.ML_Name +
                      " found FeatureLess Regions", ml.options.ML_Name + " found these regions had features"}
    End Sub
End Class





Public Class XR_ML_DepthFromColor : Inherits TaskParent
    Implements IDisposable
    Dim colorPal As New DepthColorizer_Basics_TA
    Dim mats As New Mat_4Click
    Dim resizer As New Resize_Smaller
    Dim rtree As RTrees
    Public Sub New()
        OptionParent.FindSlider("LowRes %").Value = 2 ' 2% of the image.
        labels(3) = "Click any quadrant at left to view it below"
        desc = "Use BGR to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        mats.mat(1) = task.noDepthMask.Clone

        Dim color32f As New Mat
        resizer.Run(src)

        Dim colorROI As New cv.Rect(0, 0, resizer.newSize.Width, resizer.newSize.Height)
        resizer.dst2.ConvertTo(color32f, MatType.CV_32FC3)
        Resize(mats.mat(1), mats.mat(1), color32f.Size())
        color32f.SetTo(Scalar.Black, mats.mat(1)) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Resize(task.pcSplit(2), dst0, color32f.Size())

        Dim mask As New Mat
        Threshold(dst0, mask, task.MaxZmeters, 255, ThresholdTypes.Binary)
        mask.ConvertTo(mask, MatType.CV_8U)
        Resize(mask, mats.mat(2), src.Size())

        dst0.SetTo(task.MaxZmeters, Not mask)

        ConvertScaleAbs(dst0, dst0)
        colorPal.Run(dst0)
        mats.mat(3) = colorPal.dst2.Clone()

        Threshold(dst0, mask, 1, 255, ThresholdTypes.Binary)
        ConvertScaleAbs(mask, mask)
        Dim maskCount = CountNonZero(mask)
        dst2 = mask

        Dim learnInput As Mat = color32f.Reshape(1, color32f.Total)
        Dim depthResponse As Mat = dst0.Reshape(1, dst0.Total)
        depthResponse.ConvertTo(depthResponse, MatType.CV_32F)

        ' now learn what depths are associated with which colors.
        If rtree Is Nothing Then rtree = ML.RTrees.Create()
        rtree.Train(learnInput, ML.SampleTypes.RowSample, depthResponse)

        src.ConvertTo(color32f, MatType.CV_32FC3)
        Dim input = color32f.Reshape(1, color32f.Total) ' test the entire original image.
        Dim output As New Mat
        rtree.Predict(input, output)
        Dim predictedDepth = output.Reshape(1, src.Height)

        ConvertScaleAbs(predictedDepth, dst0)
        colorPal.Run(dst0)
        mats.mat(0) = colorPal.dst2.Clone()

        mats.Run(emptyMat)
        dst2 = mats.dst2
        labels(2) = "prediction, shadow, Depth Mask < " + CStr(task.MaxZmeters) + ", Learn Input"
        dst3 = mats.dst3
    End Sub
    Protected Overrides Sub Finalize()
        If rtree IsNot Nothing Then rtree.Dispose()
    End Sub
End Class



Public Class XR_ML_DepthFromXYColor : Inherits TaskParent
    Implements IDisposable
    Dim mats As New Mat_4to1
    Dim resizer As New Resize_Smaller
    Dim colorizer As New DepthColorizer_CPP
    Dim rtree As RTrees
    Public Sub New()
        labels(2) = "Predicted Depth"
        OptionParent.FindSlider("LowRes %").Value = 2 ' 2% of the image.
        ' desc = "Use BGR to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim _cvtInline As New Mat
        CvtColor(task.noDepthMask, _cvtInline, ColorConversionCodes.GRAY2BGR)
        mats.mat(0) =_cvtInline

        Dim color32f As New Mat

        resizer.Run(src)

        Dim colorROI As New cv.Rect(0, 0, resizer.newSize.Width, resizer.newSize.Height)
        resizer.dst2.ConvertTo(color32f, MatType.CV_32FC3)
        Resize(task.noDepthMask, dst0, color32f.Size())
        color32f.SetTo(Scalar.Black, dst0) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Resize(task.pcSplit(2), dst1, color32f.Size())

        Dim mask As New Mat
        Threshold(dst1, mask, task.MaxZmeters, task.MaxZmeters, ThresholdTypes.BinaryInv)
        mask.SetTo(0, dst0) ' remove the unknown depth...
        mask.ConvertTo(mask, MatType.CV_8U)
        Dim _cvtResize As New Mat
        CvtColor(mask, _cvtResize, ColorConversionCodes.GRAY2BGR)
        Resize(_cvtResize, mats.mat(2), src.Size)

        mask = Not mask
        dst1.SetTo(task.MaxZmeters, mask)
        ConvertScaleAbs(dst1, dst1)

        colorizer.Run(dst1)
        mats.mat(3) = colorizer.dst2.Clone()

        Threshold(dst1, mask, 1, 255, ThresholdTypes.Binary)
        ConvertScaleAbs(mask, mask)
        Dim maskCount = CountNonZero(mask)
        CvtColor(mask, dst2, ColorConversionCodes.GRAY2BGR)

        Dim c = color32f.Reshape(1, color32f.Total)
        Dim depthResponse = dst1.Reshape(1, dst1.Total)
        depthResponse.ConvertTo(depthResponse, MatType.CV_32F)

        Dim learnInput As New Mat(c.Rows, 6, MatType.CV_32F, Scalar.All(0))
        For y = 0 To c.Rows - 1
            For x = 0 To c.Cols - 1
                Dim v6 = New Vec6f(c.Get(Of Single)(y, x), c.Get(Of Single)(y, x + 1), c.Get(Of Single)(y, x + 2), x, y, 0)
                learnInput.Set(Of Vec6f)(y, x, v6)
            Next
        Next

        ' Now learn what depths are associated with which colors.
        If rtree Is Nothing Then rtree = ML.RTrees.Create()
        rtree.Train(learnInput, ML.SampleTypes.RowSample, depthResponse)

        src.ConvertTo(color32f, MatType.CV_32FC3)
        Dim allC = color32f.Reshape(1, color32f.Total) ' test the entire original image.
        Dim input As New Mat(allC.Rows, 6, MatType.CV_32F, Scalar.All(0))
        For y = 0 To allC.Rows - 1
            For x = 0 To allC.Cols - 1
                Dim v6 = New Vec6f(allC.Get(Of Single)(y, x), allC.Get(Of Single)(y, x + 1), allC.Get(Of Single)(y, x + 2), x, y, 0)
                input.Set(Of Vec6f)(y, x, v6)
            Next
        Next

        Dim output As New Mat
        rtree.Predict(input, output)
        Dim predictedDepth = output.Reshape(1, src.Height)

        ConvertScaleAbs(predictedDepth, predictedDepth)
        colorizer.Run(predictedDepth)
        dst2 = colorizer.dst2.Clone()

        mats.Run(emptyMat)
        dst3 = mats.dst2
        labels(3) = "shadow, empty, Depth Mask < " + CStr(task.MaxZmeters) + ", Learn Input"
    End Sub
    Protected Overrides Sub Finalize()
        If rtree IsNot Nothing Then rtree.Dispose()
    End Sub
End Class






Public Structure mlColor
    Dim colorIndex As Single
    Dim x As Single
    Dim y As Single
End Structure


Public Class XR_ML_Color2Depth : Inherits TaskParent
    Implements IDisposable
    Dim color8U As New Color8U_Basics
    Dim rtree As RTrees
    Public Sub New()
        task.fOptions.Color8USource.SelectedItem() = "Bin4Way_Regions"
        desc = "Prepare a grid of color and depth data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        dst2 = color8U.dst3
        labels(2) = "Output of Color8U_Basics running " + task.fOptions.Color8USource.Text

        If rtree Is Nothing Then rtree = ML.RTrees.Create()
        Dim mlInput As New List(Of mlColor)
        Dim mResponse As New List(Of Single)
        Dim predictList As New List(Of mlColor)
        Dim grPredict As New List(of cv.Rect)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim mls As mlColor
            mls.colorIndex = color8U.dst2.Get(Of Byte)(r.Y, r.X)
            mls.x = r.X
            mls.y = r.Y

If CountNonZero(task.noDepthMask(r)) > 0 Then
                grPredict.Add(r)
                predictList.Add(mls)
            Else
                mlInput.Add(mls)
                mResponse.Add(Mean(task.pcSplit(2)(r)))
            End If
        Next

        If mlInput.Count = 0 Then
            SetTrueText("No learning data was found or provided.  Exit...", 3)
            Exit Sub
        End If

        Dim mLearn As Mat = Mat.FromPixelData(mlInput.Count, 3, MatType.CV_32F, mlInput.ToArray)
        Dim response As Mat = Mat.FromPixelData(mResponse.Count, 1, MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, ML.SampleTypes.RowSample, response)

        Dim predMat = Mat.FromPixelData(predictList.Count, 3, MatType.CV_32F, predictList.ToArray)
        Dim output = New Mat(predictList.Count, 1, MatType.CV_32FC1, Scalar.All(0))
        rtree.Predict(predMat, output)

        dst3 = task.pcSplit(2).Clone
        For i = 0 To predictList.Count - 1
            Dim mls = predictList(i)
            Dim r = grPredict(i)
            Dim depth = output.Get(Of Single)(i, 0)
            dst3(r).SetTo(depth, task.noDepthMask(r))
        Next

    End Sub
    Protected Overrides Sub Finalize()
        If rtree IsNot Nothing Then rtree.Dispose()
    End Sub
End Class





Public Structure mlColorInTier
    Dim colorIndex As Single
    Dim x As Single
    Dim y As Single
End Structure





Public Class XR_ML_ColorInTier2Depth : Inherits TaskParent
    Implements IDisposable
    Dim color8U As New Color8U_Basics
    Dim rtree As RTrees
    Public Sub New()
        task.fOptions.Color8USource.SelectedItem() = "Bin4Way_Regions"
        desc = "Prepare a grid of color and depth data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        dst2 = color8U.dst3
        labels(2) = "Output of Color8U_Basics running " + task.fOptions.Color8USource.Text

        If rtree Is Nothing Then rtree = ML.RTrees.Create()
        Dim mlInput As New List(Of mlColorInTier)
        Dim mResponse As New List(Of Single)
        Dim predictList As New List(Of mlColorInTier)
        Dim grPredict As New List(of cv.Rect)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim mls As mlColorInTier
            mls.colorIndex = color8U.dst2.Get(Of Byte)(r.Y, r.X)
            mls.x = r.X
            mls.y = r.Y

If CountNonZero(task.noDepthMask(r)) > 0 Then
                grPredict.Add(r)
                predictList.Add(mls)
            Else
                mlInput.Add(mls)
                mResponse.Add(Mean(task.pcSplit(2)(r)))
            End If
        Next

        If mlInput.Count = 0 Then
            SetTrueText("No learning data was found or provided.  Exit...", 3)
            Exit Sub
        End If

        Dim mLearn As Mat = Mat.FromPixelData(mlInput.Count, 3, MatType.CV_32F, mlInput.ToArray)
        Dim response As Mat = Mat.FromPixelData(mResponse.Count, 1, MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, ML.SampleTypes.RowSample, response)

        Dim predMat = Mat.FromPixelData(predictList.Count, 3, MatType.CV_32F, predictList.ToArray)
        Dim output = New Mat(predictList.Count, MatType.CV_32FC1, 0)
        rtree.Predict(predMat, output)

        dst3 = task.pcSplit(2).Clone
        For i = 0 To predictList.Count - 1
            Dim mls = predictList(i)
            Dim r = grPredict(i)
            Dim depth = output.Get(Of Single)(i, 0)
            dst3(r).SetTo(depth, task.noDepthMask(r))
        Next
    End Sub
    Protected Overrides Sub Finalize()
        If rtree IsNot Nothing Then rtree.Dispose()
    End Sub
End Class





Public Class ML_RemoveDups_CPP : Inherits TaskParent
    Implements IDisposable
    Public Sub New()
        cPtr = ML_RemoveDups_Open()
        labels = {"", "", "BGR input below is converted to BGRA and sorted as integers", ""}
        desc = "The input is BGR, convert to BGRA, and sorted as an integer.  The output is a sorted BGR Mat file with duplicates removed."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type = MatType.CV_8U Then dst2 = src.Clone Else dst2 = task.gray

        Dim dataSrc(dst2.Total * dst2.ElemSize) As Byte
        Marshal.Copy(dst2.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = ML_RemoveDups_Run(cPtr, handleSrc.AddrOfPinnedObject(), dst2.Rows, dst2.Cols, dst2.Type)
        handleSrc.Free()

        Dim compressedCount = ML_RemoveDups_GetCount(cPtr)
        dst3 = Mat.FromPixelData(src.Rows, src.Cols, MatType.CV_8U, imagePtr).Clone

        labels(3) = "The BGR data in dst2 after removing duplicate BGR entries.  Input count = " + CStr(dst2.Total) + " output = " + CStr(compressedCount)
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = ML_RemoveDups_Close(cPtr)
    End Sub
End Class





Public Class ML_RandomForest : Inherits TaskParent
    Implements IDisposable
    Public trainMat As New Mat
    Public trainResponse As Mat ' 32FC1 format
    Public testMat As New Mat
    Public predictions As New Mat
    Dim rtrees As RTrees
    Public Sub New()
        rtrees = ML.RTrees.Create()
        desc = "Run RandomForest on the provided inputs..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            SetTrueText("No output when run standalone...")
            Exit Sub
        End If
        Dim nSamples = trainMat.Rows
        Dim varCount = trainMat.Cols
        If trainResponse.Rows <> nSamples Then
            SetTrueText("ML_RandomForest: trainResponse must be " + CStr(nSamples) + "x1 (one label per row of trainMat).")
            Exit Sub
        End If

        rtrees.MinSampleCount = 2
        rtrees.MaxDepth = 4
        rtrees.RegressionAccuracy = 0.0
        rtrees.UseSurrogates = False
        rtrees.MaxCategories = 16
        rtrees.Priors = New Mat
        rtrees.CalculateVarImportance = False
        rtrees.ActiveVarCount = varCount
        rtrees.TermCriteria = TermCriteria.Both(5, 0)

        If task.heartBeatLT Then
            rtrees.Train(trainMat, ML.SampleTypes.RowSample, trainResponse)
        Else
            predictions = New Mat
            rtrees.Predict(testMat, predictions)
        End If
    End Sub
    Protected Overrides Sub Finalize()
        If rtrees IsNot Nothing Then rtrees.Dispose()
    End Sub
End Class
