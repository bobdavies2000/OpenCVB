Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports OpenCvSharp.ML
Public Class ML_Basics : Inherits TaskParent
    Public trainMats() As cv.Mat ' all entries are 32FCx
    Public trainResponse As cv.Mat ' 32FC1 format
    Public testMats() As cv.Mat ' all entries are 32FCx
    Public predictions As New cv.Mat
    Public options As New Options_ML
    Public buildEveryPass As Boolean
    Public Sub New()
        desc = "Simplify the prep for ML data train and test data and run with ML algorithms."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standalone Then
            SetTrueText("ML_BasicsRTree has no output when run standalone." + vbCrLf + "Use LowResOld_Depth to test.")
            Exit Sub
        End If

        options.Run()
        labels(2) = "ML algorithm selected is " + options.ML_Name

        Dim trainMat As New cv.Mat
        cv.Cv2.Merge(trainMats, trainMat)

        Dim varCount As Integer
        For Each m In trainMats
            varCount += m.ElemSize / 4 ' how many 32f variables in this Mat?
        Next

        trainMat = cv.Mat.FromPixelData(trainMat.Total, varCount, cv.MatType.CV_32F, trainMat.Data)
        Dim responseMat = cv.Mat.FromPixelData(trainMats(0).Total, 1, cv.MatType.CV_32F, trainResponse.Data)

        Static classifier As Object
        Dim respFormat = cv.MatType.CV_32F
        If task.heartBeat Or buildEveryPass Then
            Select Case options.ML_Name
                Case "NormalBayesClassifier"
                    classifier = cv.ML.NormalBayesClassifier.Create()
                    respFormat = cv.MatType.CV_32S
                Case "KNearest"
                    classifier = cv.ML.KNearest.Create()
                    classifier.DefaultK = 15
                    classifier.IsClassifier = True
                Case "SVM"
                    classifier = cv.ML.SVM.Create()
                    classifier.C = 1
                    classifier.TermCriteria = cv.TermCriteria.Both(1000, 0.01)
                    classifier.P = 0
                    classifier.Nu = 0.5
                    classifier.Coef0 = 1
                    classifier.Gamma = 1
                    classifier.Degree = 0.5
                    classifier.KernelType = SVM.KernelTypes.Poly
                    classifier.Type = SVM.Types.CSvc
                    respFormat = cv.MatType.CV_32S
                Case "DTrees"
                    classifier = cv.ML.DTrees.Create()
                    classifier.CVFolds = 0
                    classifier.TruncatePrunedTree = False
                    classifier.UseSurrogates = False
                    classifier.MinSampleCount = 2
                    classifier.MaxDepth = 8
                    classifier.Use1SERule = False
                Case "Boost"
                    classifier = cv.ML.Boost.Create()
                    respFormat = cv.MatType.CV_32S
                    classifier.BoostType = Boost.Types.Discrete
                    classifier.WeakCount = 100
                    classifier.WeightTrimRate = 0.95
                    classifier.MaxDepth = 2
                    classifier.UseSurrogates = False
                    classifier.Priors = New cv.Mat()

                Case "ANN_MLP" ' artificial neural net with multi-layer perceptron
                    classifier = cv.ML.ANN_MLP.Create()

                    ' input layer, hidden layer, output layer
                    classifier.SetLayerSizes(cv.Mat.FromPixelData(1, 3, cv.MatType.CV_32SC1, {varCount, 5, 1}))

                    classifier.SetActivationFunction(cv.ML.ANN_MLP.ActivationFunctions.SigmoidSym, 1, 1)
                    classifier.TermCriteria = cv.TermCriteria.Both(1000, 0.000001)
                    classifier.SetTrainMethod(cv.ML.ANN_MLP.TrainingMethods.BackProp, 0.1, 0.1)
                Case "LogisticRegression"
                    classifier = cv.ML.LogisticRegression.Create()
                Case Else
                    classifier = cv.ML.RTrees.Create()
                    classifier.MinSampleCount = 2
                    classifier.MaxDepth = 4
                    classifier.RegressionAccuracy = 0.0
                    classifier.UseSurrogates = False
                    classifier.MaxCategories = 16
                    classifier.Priors = New cv.Mat
                    classifier.CalculateVarImportance = False
                    classifier.ActiveVarCount = varCount
                    classifier.TermCriteria = cv.TermCriteria.Both(5, 0)
            End Select

            If responseMat.Type <> respFormat Then responseMat.ConvertTo(responseMat, respFormat)

            classifier.Train(trainMat, cv.ML.SampleTypes.RowSample, responseMat)
        End If
        Dim testMat As New cv.Mat
        cv.Cv2.Merge(testMats, testMat)

        testMat = cv.Mat.FromPixelData(testMat.Total, varCount, cv.MatType.CV_32F, testMat.Data)
        classifier.Predict(testMat, predictions)

        If predictions.Type <> cv.MatType.CV_32F Then
            predictions.ConvertTo(predictions, cv.MatType.CV_32F)
        End If
        Dim test = cv.ML.ANN_MLP.Create
        classifier.Dispose
    End Sub
End Class






Public Class ML_BasicsOld : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        labels = {"", "depth32f - 32fc3 format with missing depth filled with predicted depth based on color (brighter is farther)", "", "Color used for roi prediction"}
        desc = "Predict depth from color to fill in the depth shadow areas"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim noDepthCount(task.gridRects.Count - 1) As Integer
        Dim roiColor(task.gridRects.Count - 1) As cv.Vec3b

        dst2.SetTo(0)
        Parallel.For(0, task.gridRects.Count,
        Sub(i)
            Dim roi = task.gridRects(i)
            roiColor(i) = src(roi).Get(Of cv.Vec3b)(roi.Height / 2, roi.Width / 2)
            dst2(roi).SetTo(roiColor(i), task.depthMask(roi))
            noDepthCount(i) = task.noDepthMask(roi).CountNonZero
        End Sub)

        Dim rtree = cv.ML.RTrees.Create()
        Dim mlInput As New List(Of mlData)
        Dim mResponse As New List(Of Single)
        For i = 0 To task.gridRects.Count - 1
            If noDepthCount(i) = 0 Then Continue For
            Dim ml As mlData
            Dim roi = task.gridRects(i)
            ml.row = roi.Y + roi.Height / 2
            ml.col = roi.X + roi.Width / 2
            Dim c = roiColor(i)
            ml.blue = c(0)
            ml.green = c(1)
            ml.red = c(2)
            mlInput.Add(ml)
            mResponse.Add(task.pcSplit(2)(roi).Mean())
        Next

        If mlInput.Count = 0 Then
            strOut = "No learning data was found or provided.  Exit..."
            dst3.SetTo(0)
            SetTrueText(strOut, 3)
            Exit Sub
        End If

        Dim mLearn As cv.Mat = cv.Mat.FromPixelData(mlInput.Count, 5, cv.MatType.CV_32F, mlInput.ToArray)
        Dim response As cv.Mat = cv.Mat.FromPixelData(mResponse.Count, 1, cv.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cv.ML.SampleTypes.RowSample, response)

        Dim predictList As New List(Of mlData)
        Dim colors As New List(Of cv.Vec3b)
        Dim saveRoi As New List(Of cv.Rect)
        Dim depthMask As New List(Of cv.Mat)
        For i = 0 To task.gridRects.Count - 1
            If noDepthCount(i) = 0 Then Continue For
            Dim roi = task.gridRects(i)
            depthMask.Add(task.noDepthMask(roi))
            Dim ml As mlData
            ml.row = roi.Y + roi.Height / 2
            ml.col = roi.X + roi.Width / 2
            Dim c = roiColor(i)
            ml.blue = c(0)
            ml.green = c(1)
            ml.red = c(2)
            predictList.Add(ml)
            colors.Add(c)
            saveRoi.Add(roi)
        Next

        Dim predMat = cv.Mat.FromPixelData(predictList.Count, 5, cv.MatType.CV_32F, predictList.ToArray)
        Dim output = New cv.Mat(predictList.Count, 1, cv.MatType.CV_32FC1, cv.Scalar.All(0))
        rtree.Predict(predMat, output)

        dst1 = task.pcSplit(2)
        dst3.SetTo(0)
        For i = 0 To predictList.Count - 1
            Dim roi = saveRoi(i)
            Dim depth = output.Get(Of Single)(i, 0)
            dst1(roi).SetTo(depth, depthMask(i))
            dst3(roi).SetTo(colors(i), depthMask(i))
        Next

        labels(2) = CStr(task.gridRects.Count) + " regions with " + CStr(mlInput.Count) + " used for learning and " + CStr(predictList.Count) + " were predicted"
    End Sub
End Class







Public Class ML_DepthFromColor : Inherits TaskParent
    Dim colorPal As New DepthColorizer_Basics
    Dim mats As New Mat_4Click
    Dim resizer As New Resize_Smaller
    Public Sub New()
       OptionParent.FindSlider("LowRes %").Value = 2 ' 2% of the image.
        labels(3) = "Click any quadrant at left to view it below"
        desc = "Use BGR to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        mats.mat(1) = task.noDepthMask.Clone

        Dim color32f As New cv.Mat
        resizer.Run(src)

        Dim colorROI As New cv.Rect(0, 0, resizer.newSize.Width, resizer.newSize.Height)
        resizer.dst2.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim shadowSmall = mats.mat(1).Resize(color32f.Size()).Clone()
        color32f.SetTo(cv.Scalar.Black, shadowSmall) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Dim depth = task.pcSplit(2).Resize(color32f.Size())

        Dim mask = depth.Threshold(task.MaxZmeters, 255, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        mats.mat(2) = mask.Resize(src.Size())

        depth.SetTo(task.MaxZmeters, Not mask)

        colorPal.Run(depth.ConvertScaleAbs())
        mats.mat(3) = colorPal.dst2.Clone()

        mask = depth.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim maskCount = mask.CountNonZero
        dst2 = mask

        Dim learnInput = color32f.Reshape(1, color32f.Total)
        Dim depthResponse = depth.Reshape(1, depth.Total)

        ' now learn what depths are associated with which colors.
        Dim rtree = cv.ML.RTrees.Create()
        rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)

        src.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim input = color32f.Reshape(1, color32f.Total) ' test the entire original image.
        Dim output As New cv.Mat
        rtree.Predict(input, output)
        Dim predictedDepth = output.Reshape(1, src.Height)

        colorPal.Run(predictedDepth.ConvertScaleAbs())
        mats.mat(0) = colorPal.dst2.Clone()

        mats.Run(emptyMat)
        dst2 = mats.dst2
        labels(2) = "prediction, shadow, Depth Mask < " + CStr(task.MaxZmeters) + ", Learn Input"
        dst3 = mats.dst3
    End Sub
End Class



Public Class ML_DepthFromXYColor : Inherits TaskParent
    Dim mats As New Mat_4to1
    Dim resizer As New Resize_Smaller
    Dim colorizer As New DepthColorizer_CPP
    Public Sub New()
        labels(2) = "Predicted Depth"
       OptionParent.FindSlider("LowRes %").Value = 2 ' 2% of the image.
        ' desc = "Use BGR to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        mats.mat(0) = task.noDepthMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat

        resizer.Run(src)

        Dim colorROI As New cv.Rect(0, 0, resizer.newSize.Width, resizer.newSize.Height)
        resizer.dst2.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim shadowSmall = task.noDepthMask.Resize(color32f.Size())
        color32f.SetTo(cv.Scalar.Black, shadowSmall) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Dim depth32f = task.pcSplit(2).Resize(color32f.Size())

        Dim mask = depth32f.Threshold(task.MaxZmeters, task.MaxZmeters, cv.ThresholdTypes.BinaryInv)
        mask.SetTo(0, shadowSmall) ' remove the unknown depth...
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        mats.mat(2) = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR).Resize(src.Size)

        mask = Not mask
        depth32f.SetTo(task.MaxZmeters, mask)

        colorizer.Run(depth32f.ConvertScaleAbs)
        mats.mat(3) = colorizer.dst2.Clone()

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim maskCount = mask.CountNonZero
        dst2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim c = color32f.Reshape(1, color32f.Total)
        Dim depthResponse = depth32f.Reshape(1, depth32f.Total)

        Dim learnInput As New cv.Mat(c.Rows, 6, cv.MatType.CV_32F, cv.Scalar.All(0))
        For y = 0 To c.Rows - 1
            For x = 0 To c.Cols - 1
                Dim v6 = New cv.Vec6f(c.Get(Of Single)(y, x), c.Get(Of Single)(y, x + 1), c.Get(Of Single)(y, x + 2), x, y, 0)
                learnInput.Set(Of cv.Vec6f)(y, x, v6)
            Next
        Next

        ' Now learn what depths are associated with which colors.
        Dim rtree = cv.ML.RTrees.Create()
        rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)

        src.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim allC = color32f.Reshape(1, color32f.Total) ' test the entire original image.
        Dim input As New cv.Mat(allC.Rows, 6, cv.MatType.CV_32F, cv.Scalar.All(0))
        For y = 0 To allC.Rows - 1
            For x = 0 To allC.Cols - 1
                Dim v6 = New cv.Vec6f(allC.Get(Of Single)(y, x), allC.Get(Of Single)(y, x + 1), allC.Get(Of Single)(y, x + 2), x, y, 0)
                input.Set(Of cv.Vec6f)(y, x, v6)
            Next
        Next

        Dim output As New cv.Mat
        rtree.Predict(input, output)
        Dim predictedDepth = output.Reshape(1, src.Height)

        colorizer.Run(predictedDepth.ConvertScaleAbs)
        dst2 = colorizer.dst2.Clone()

        mats.Run(emptyMat)
        dst3 = mats.dst2
        labels(3) = "shadow, empty, Depth Mask < " + CStr(task.MaxZmeters) + ", Learn Input"
    End Sub
End Class






Public Structure mlColor
    Dim colorIndex As Single
    Dim x As Single
    Dim y As Single
End Structure


Public Class ML_Color2Depth : Inherits TaskParent
    Dim minMax As New Grid_MinMaxDepth
    Dim color8U As New Color8U_Basics
    Public Sub New()
        task.gOptions.ColorSource.SelectedItem() = "Bin4Way_Regions"
        desc = "Prepare a grid of color and depth data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        dst2 = color8U.dst3
        labels(2) = "Output of Color8U_Basics running " + task.gOptions.ColorSource.Text

        Dim rtree = cv.ML.RTrees.Create()
        Dim mlInput As New List(Of mlColor)
        Dim mResponse As New List(Of Single)
        Dim predictList As New List(Of mlColor)
        Dim roiPredict As New List(Of cv.Rect)
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            Dim mls As mlColor
            mls.colorIndex = color8U.dst2.Get(Of Byte)(roi.Y, roi.X)
            mls.x = roi.X
            mls.y = roi.Y

            If task.noDepthMask(roi).CountNonZero > 0 Then
                roiPredict.Add(roi)
                predictList.Add(mls)
            Else
                mlInput.Add(mls)
                mResponse.Add(task.pcSplit(2)(roi).Mean())
            End If
        Next

        If mlInput.Count = 0 Then
            SetTrueText("No learning data was found or provided.  Exit...", 3)
            Exit Sub
        End If

        Dim mLearn As cv.Mat = cv.Mat.FromPixelData(mlInput.Count, 3, cv.MatType.CV_32F, mlInput.ToArray)
        Dim response As cv.Mat = cv.Mat.FromPixelData(mResponse.Count, 1, cv.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cv.ML.SampleTypes.RowSample, response)

        Dim predMat = cv.Mat.FromPixelData(predictList.Count, 3, cv.MatType.CV_32F, predictList.ToArray)
        Dim output = New cv.Mat(predictList.Count, 1, cv.MatType.CV_32FC1, cv.Scalar.All(0))
        rtree.Predict(predMat, output)

        dst3 = task.pcSplit(2).Clone
        For i = 0 To predictList.Count - 1
            Dim mls = predictList(i)
            Dim roi = roiPredict(i)
            Dim depth = output.Get(Of Single)(i, 0)
            dst3(roi).SetTo(depth, task.noDepthMask(roi))
        Next

    End Sub
End Class





Public Structure mlColorInTier
    Dim colorIndex As Single
    Dim x As Single
    Dim y As Single
End Structure





Public Class ML_ColorInTier2Depth : Inherits TaskParent
    Dim color8U As New Color8U_Basics
    Public Sub New()
        task.gOptions.ColorSource.SelectedItem() = "Bin4Way_Regions"
        desc = "Prepare a grid of color and depth data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        dst2 = color8U.dst3
        labels(2) = "Output of Color8U_Basics running " + task.gOptions.ColorSource.Text

        Dim rtree = cv.ML.RTrees.Create()
        Dim mlInput As New List(Of mlColorInTier)
        Dim mResponse As New List(Of Single)
        Dim predictList As New List(Of mlColorInTier)
        Dim roiPredict As New List(Of cv.Rect)
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            Dim mls As mlColorInTier
            mls.colorIndex = color8U.dst2.Get(Of Byte)(roi.Y, roi.X)
            mls.x = roi.X
            mls.y = roi.Y

            If task.noDepthMask(roi).CountNonZero > 0 Then
                roiPredict.Add(roi)
                predictList.Add(mls)
            Else
                mlInput.Add(mls)
                mResponse.Add(task.pcSplit(2)(roi).Mean())
            End If
        Next

        If mlInput.Count = 0 Then
            SetTrueText("No learning data was found or provided.  Exit...", 3)
            Exit Sub
        End If

        Dim mLearn As cv.Mat = cv.Mat.FromPixelData(mlInput.Count, 3, cv.MatType.CV_32F, mlInput.ToArray)
        Dim response As cv.Mat = cv.Mat.FromPixelData(mResponse.Count, 1, cv.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cv.ML.SampleTypes.RowSample, response)

        Dim predMat = cv.Mat.FromPixelData(predictList.Count, 3, cv.MatType.CV_32F, predictList.ToArray)
        Dim output = New cv.Mat(predictList.Count, cv.MatType.CV_32FC1, 0)
        rtree.Predict(predMat, output)

        dst3 = task.pcSplit(2).Clone
        For i = 0 To predictList.Count - 1
            Dim mls = predictList(i)
            Dim roi = roiPredict(i)
            Dim depth = output.Get(Of Single)(i, 0)
            dst3(roi).SetTo(depth, task.noDepthMask(roi))
        Next

    End Sub
End Class








Public Class ML_LearnZfromXGray : Inherits TaskParent
    Dim regions As New GuidedBP_Regions
    Public Sub New()
        desc = "This runs and is helpful to understanding how to use rtree.  Learn Z from X, Y, and grayscale of the RedCloud cells."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) ' input to ML

        regions.Run(src)

        Dim ptList As New List(Of cv.Point3f)
        Dim mlInput As New List(Of cv.Vec3f)
        Dim mResponse As New List(Of Single)
        For y = 0 To regions.rcMapX.Height - 1
            For x = 0 To regions.rcMapX.Width - 1
                Dim zVal = task.pcSplit(2).Get(Of Single)(y, x)
                Dim val = CSng(gray.Get(Of Byte)(y, x))
                If zVal = 0 Then
                    ptList.Add(New cv.Point3f(CSng(x), CSng(y), val))
                Else
                    mlInput.Add(New cv.Vec3f(val, x, y))
                    mResponse.Add(zVal)
                End If
            Next
        Next

        Dim rtree = cv.ML.RTrees.Create()
        Dim mLearn As cv.Mat = cv.Mat.FromPixelData(mlInput.Count, 3, cv.MatType.CV_32F, mlInput.ToArray)
        Dim response As cv.Mat = cv.Mat.FromPixelData(mResponse.Count, 1, cv.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cv.ML.SampleTypes.RowSample, response)

        Dim predMat = cv.Mat.FromPixelData(ptList.Count, 3, cv.MatType.CV_32F, ptList.ToArray)
        Dim output = New cv.Mat(ptList.Count, 1, cv.MatType.CV_32FC1, cv.Scalar.All(0))
        rtree.Predict(predMat, output)
    End Sub
End Class







Public Class ML_LearnRegions : Inherits TaskParent
    Dim regions As New GuidedBP_Regions
    Dim color8U As New Color8U_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "", "Entire image after ML", "ML Predictions where no region was defined."}
        desc = "Learn region from X, Y, and grayscale for the RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        regions.Run(src)

        color8U.Run(src)
        dst1 = color8U.dst3

        Dim graySrc = If(dst1.Channels = 1, dst1, dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)) ' input to ML
        Dim regionX = regions.rcMapX ' Target variable

        Dim ptList As New List(Of cv.Point3f)
        Dim mlInput As New List(Of cv.Vec3f)
        Dim mResponse As New List(Of Single)

        For y = 0 To regions.rcMapX.Height - 1
            For x = 0 To regions.rcMapX.Width - 1
                Dim gray = CSng(graySrc.Get(Of Byte)(y, x))
                Dim region = CSng(regionX.Get(Of Byte)(y, x))
                If region = 0 Then
                    ptList.Add(New cv.Point3f(CSng(x), CSng(y), gray))
                Else
                    mlInput.Add(New cv.Vec3f(gray, x, y))
                    mResponse.Add(region)
                End If
            Next
        Next

        Dim rtree = cv.ML.RTrees.Create()
        Dim mLearn As cv.Mat = cv.Mat.FromPixelData(mlInput.Count, 3, cv.MatType.CV_32F, mlInput.ToArray)
        Dim response As cv.Mat = cv.Mat.FromPixelData(mResponse.Count, 1, cv.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cv.ML.SampleTypes.RowSample, response)

        Dim predMat = cv.Mat.FromPixelData(ptList.Count, 3, cv.MatType.CV_32F, ptList.ToArray)
        Dim output = New cv.Mat(ptList.Count, 1, cv.MatType.CV_32FC1, cv.Scalar.All(0))
        rtree.Predict(predMat, output)

        regions.mats.mat(0).CopyTo(dst2)
        dst3.SetTo(0)
        For i = 0 To ptList.Count - 1
            Dim pt = ptList(i)
            Dim regionID = CInt(output.Get(Of Single)(i, 0))
            If regionID < regions.rcListX.Count Then
                Dim rc = regions.rcListX(regionID)
                Dim color As cv.Vec3b = New cv.Vec3b(rc.color(0), rc.color(1), rc.color(2))
                dst2.Set(Of cv.Vec3b)(pt.Y, pt.X, color)
                dst3.Set(Of cv.Vec3b)(pt.Y, pt.X, color)
            End If
        Next
    End Sub
End Class







Public Class ML_RemoveDups_CPP : Inherits TaskParent
    Public Sub New()
        cPtr = ML_RemoveDups_Open()
        labels = {"", "", "BGR input below is converted to BGRA and sorted as integers", ""}
        desc = "The input is BGR, convert to BGRA, and sorted as an integer.  The output is a sorted BGR Mat file with duplicates removed."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type = cv.MatType.CV_8U Then dst2 = src.Clone Else dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim dataSrc(dst2.Total * dst2.ElemSize) As Byte
        Marshal.Copy(dst2.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = ML_RemoveDups_Run(cPtr, handleSrc.AddrOfPinnedObject(), dst2.Rows, dst2.Cols, dst2.Type)
        handleSrc.Free()

        Dim compressedCount = ML_RemoveDups_GetCount(cPtr)
        dst3 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone

        labels(3) = "The BGR data in dst2 after removing duplicate BGR entries.  Input count = " + CStr(dst2.Total) + " output = " + CStr(compressedCount)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = ML_RemoveDups_Close(cPtr)
    End Sub
End Class