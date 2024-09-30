Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class ML_BasicsRTree : Inherits VB_Parent
    Public trainMats() As cvb.Mat ' all entries are 32FCx
    Public trainResponse As cvb.Mat ' 32FC1 format
    Public testMats() As cvb.Mat ' all entries are 32FCx
    Public predictions As New cvb.Mat
    Public Sub New()
        desc = "Simplify the prep for ML data train and test data and run with ML algorithms."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standalone Then
            SetTrueText("ML_BasicsRTree has no output when run standalone.")
            Exit Sub
        End If
        Dim trainMat As New cvb.Mat
        cvb.Cv2.Merge(trainMats, trainMat)

        Dim varCount As Integer
        For Each m In trainMats
            varCount += m.ElemSize / 4 ' how many 32f variables in this Mat?
        Next

        trainMat = cvb.Mat.FromPixelData(trainMat.Total, varCount, cvb.MatType.CV_32F, trainMat.Data)
        Dim responseMat = cvb.Mat.FromPixelData(trainMats(0).Total, 1, cvb.MatType.CV_32F, trainResponse.Data)

        Dim rtree = cvb.ML.RTrees.Create()
        rtree.Train(trainMat, cvb.ML.SampleTypes.RowSample, responseMat)

        Dim testMat As New cvb.Mat
        cvb.Cv2.Merge(testMats, testMat)

        testMat = cvb.Mat.FromPixelData(testMat.Total, varCount, cvb.MatType.CV_32F, testMat.Data)
        rtree.Predict(testMat, predictions)
    End Sub
End Class





Public Class ML_BasicsOld : Inherits VB_Parent
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "depth32f - 32fc3 format with missing depth filled with predicted depth based on color (brighter is farther)", "", "Color used for roi prediction"}
        desc = "Predict depth from color to fill in the depth shadow areas"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim noDepthCount(task.gridRects.Count - 1) As Integer
        Dim roiColor(task.gridRects.Count - 1) As cvb.Vec3b

        dst2.SetTo(0)
        Parallel.For(0, task.gridRects.Count,
        Sub(i)
            Dim roi = task.gridRects(i)
            roiColor(i) = src(roi).Get(Of cvb.Vec3b)(roi.Height / 2, roi.Width / 2)
            dst2(roi).SetTo(roiColor(i), task.depthMask(roi))
            noDepthCount(i) = task.noDepthMask(roi).CountNonZero
        End Sub)

        Dim rtree = cvb.ML.RTrees.Create()
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

        Dim mLearn As cvb.Mat = cvb.Mat.FromPixelData(mlInput.Count, 5, cvb.MatType.CV_32F, mlInput.ToArray)
        Dim response As cvb.Mat = cvb.Mat.FromPixelData(mResponse.Count, 1, cvb.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cvb.ML.SampleTypes.RowSample, response)

        Dim predictList As New List(Of mlData)
        Dim colors As New List(Of cvb.Vec3b)
        Dim saveRoi As New List(Of cvb.Rect)
        Dim depthMask As New List(Of cvb.Mat)
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

        Dim predMat = cvb.Mat.FromPixelData(predictList.Count, 5, cvb.MatType.CV_32F, predictList.ToArray)
        Dim output = New cvb.Mat(predictList.Count, 1, cvb.MatType.CV_32FC1, cvb.Scalar.All(0))
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






Module ML__Exports
    Private Class CompareVec3f : Implements IComparer(Of cvb.Vec3f)
        Public Function Compare(ByVal a As cvb.Vec3f, ByVal b As cvb.Vec3f) As Integer Implements IComparer(Of cvb.Vec3f).Compare
            If a(0) = b(0) And a(1) = b(1) And a(2) = b(2) Then Return 0
            Return If(a(0) < b(0), -1, 1)
        End Function
    End Class
    Public Function detectAndFillShadow(holeMask As cvb.Mat, borderMask As cvb.Mat, depth32f As cvb.Mat, color As cvb.Mat, minLearnCount As Integer) As cvb.Mat
        Dim learnData As New SortedList(Of cvb.Vec3f, Single)(New CompareVec3f)
        Dim rng As New System.Random
        Dim holeCount = cvb.Cv2.CountNonZero(holeMask)
        If borderMask.Channels() <> 1 Then borderMask = borderMask.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim borderCount = cvb.Cv2.CountNonZero(borderMask)
        If holeCount > 0 And borderCount > minLearnCount Then
            Dim color32f As New cvb.Mat
            color.ConvertTo(color32f, cvb.MatType.CV_32FC3)

            Dim learnInputList As New List(Of cvb.Vec3f)
            Dim responseInputList As New List(Of Single)

            For y = 0 To holeMask.Rows - 1
                For x = 0 To holeMask.Cols - 1
                    If borderMask.Get(Of Byte)(y, x) Then
                        Dim vec = color32f.Get(Of cvb.Vec3f)(y, x)
                        If learnData.ContainsKey(vec) = False Then
                            learnData.Add(vec, depth32f.Get(Of Single)(y, x)) ' keep out duplicates.
                            learnInputList.Add(vec)
                            responseInputList.Add(depth32f.Get(Of Single)(y, x))
                        End If
                    End If
                Next
            Next

            Dim learnInput As cvb.Mat = cvb.Mat.FromPixelData(learnData.Count, 3, cvb.MatType.CV_32F, learnInputList.ToArray())
            Dim depthResponse As cvb.Mat = cvb.Mat.FromPixelData(learnData.Count, 1, cvb.MatType.CV_32F, responseInputList.ToArray())

            ' now learn what depths are associated with which colors.
            Dim rtree = cvb.ML.RTrees.Create()
            rtree.Train(learnInput, cvb.ML.SampleTypes.RowSample, depthResponse)

            ' now predict what the depth is based just on the color (and proximity to the region)
            Using predictMat As New cvb.Mat(1, 3, cvb.MatType.CV_32F)
                For y = 0 To holeMask.Rows - 1
                    For x = 0 To holeMask.Cols - 1
                        If holeMask.Get(Of Byte)(y, x) Then
                            predictMat.Set(Of cvb.Vec3f)(0, 0, color32f.Get(Of cvb.Vec3f)(y, x))
                            depth32f.Set(Of Single)(y, x, rtree.Predict(predictMat))
                        End If
                    Next
                Next
            End Using
        End If
        Return depth32f
    End Function
End Module







Public Class ML_FillRGBDepth_MT : Inherits VB_Parent
    Dim shadow As New Depth_Holes
    Dim colorizer As New Depth_Colorizer_CPP_VB
    Public Sub New()
        task.gOptions.GridSlider.Maximum = dst2.Cols / 2
        task.gOptions.setGridSize(CInt(dst2.Cols / 2))

        labels = {"", "", "ML filled shadow", ""}
        desc = "Predict depth based on color and colorize depth to confirm correctness of model.  NOTE: memory leak occurs if more multi-threading is used!"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim minLearnCount = 5
        Parallel.ForEach(task.gridRects,
            Sub(roi)
                task.pcSplit(2)(roi) = detectAndFillShadow(task.noDepthMask(roi), shadow.dst3(roi), task.pcSplit(2)(roi), src(roi), minLearnCount)
            End Sub)

        colorizer.Run(task.pcSplit(2))
        dst2 = colorizer.dst2.Clone()
        dst2.SetTo(cvb.Scalar.White, task.gridMask)
    End Sub
End Class






Public Class ML_DepthFromColor : Inherits VB_Parent
    Dim colorizer As New Depth_Colorizer_CPP_VB
    Dim mats As New Mat_4Click
    Dim resizer As New Resize_Smaller
    Public Sub New()
        FindSlider("Resize Percentage (%)").Value = 2 ' 2% of the image.
        labels(3) = "Click any quadrant at left to view it below"
        desc = "Use BGR to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)

        mats.mat(1) = task.noDepthMask.Clone

        Dim color32f As New cvb.Mat
        resizer.Run(src)

        Dim colorROI As New cvb.Rect(0, 0, resizer.newSize.Width, resizer.newSize.Height)
        resizer.dst2.ConvertTo(color32f, cvb.MatType.CV_32FC3)
        Dim shadowSmall = mats.mat(1).Resize(color32f.Size()).Clone()
        color32f.SetTo(cvb.Scalar.Black, shadowSmall) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Dim depth = task.pcSplit(2).Resize(color32f.Size())

        Dim mask = depth.Threshold(task.gOptions.maxDepth, task.gOptions.maxDepth, cvb.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cvb.MatType.CV_8U)
        mats.mat(2) = mask

        mask = Not mask
        depth.SetTo(task.gOptions.maxDepth, mask)

        colorizer.Run(depth)
        mats.mat(3) = colorizer.dst2.Clone()

        mask = depth.Threshold(1, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim maskCount = mask.CountNonZero
        dst2 = mask

        Dim learnInput = color32f.Reshape(1, color32f.Total)
        Dim depthResponse = depth.Reshape(1, depth.Total)

        ' now learn what depths are associated with which colors.
        Dim rtree = cvb.ML.RTrees.Create()
        rtree.Train(learnInput, cvb.ML.SampleTypes.RowSample, depthResponse)

        src.ConvertTo(color32f, cvb.MatType.CV_32FC3)
        Dim input = color32f.Reshape(1, color32f.Total) ' test the entire original image.
        Dim output As New cvb.Mat
        rtree.Predict(input, output)
        Dim predictedDepth = output.Reshape(1, src.Height)

        colorizer.Run(predictedDepth)
        mats.mat(0) = colorizer.dst2.Clone()

        mats.Run(empty)
        dst2 = mats.dst2
        labels(2) = "prediction, shadow, Depth Mask < " + CStr(task.gOptions.maxDepth) + ", Learn Input"
        dst3 = mats.dst3
    End Sub
End Class



Public Class ML_DepthFromXYColor : Inherits VB_Parent
    Dim mats As New Mat_4to1
    Dim shadow As New Depth_Holes
    Dim resizer As New Resize_Smaller
    Dim colorizer As New Depth_Colorizer_CPP_VB
    Public Sub New()
        FindSlider("Resize Percentage (%)").Value = 2 ' 2% of the image.
        labels(2) = "Predicted Depth"
        desc = "Use BGR to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        shadow.Run(src)
        mats.mat(0) = shadow.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cvb.Mat

        resizer.Run(src)

        Dim colorROI As New cvb.Rect(0, 0, resizer.newSize.Width, resizer.newSize.Height)
        resizer.dst2.ConvertTo(color32f, cvb.MatType.CV_32FC3)
        Dim shadowSmall = shadow.dst2.Resize(color32f.Size()).Clone()
        color32f.SetTo(cvb.Scalar.Black, shadowSmall) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Dim depth32f = task.pcSplit(2).Resize(color32f.Size())

        Dim mask = depth32f.Threshold(task.gOptions.maxDepth, task.gOptions.maxDepth, cvb.ThresholdTypes.BinaryInv)
        mask.SetTo(0, shadowSmall) ' remove the unknown depth...
        mask.ConvertTo(mask, cvb.MatType.CV_8U)
        mats.mat(2) = mask.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        mask = Not mask
        depth32f.SetTo(task.gOptions.maxDepth, mask)

        colorizer.Run(depth32f)
        mats.mat(3) = colorizer.dst2.Clone()

        mask = depth32f.Threshold(1, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim maskCount = mask.CountNonZero
        dst2 = mask.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        Dim c = color32f.Reshape(1, color32f.Total)
        Dim depthResponse = depth32f.Reshape(1, depth32f.Total)

        Dim learnInput As New cvb.Mat(c.Rows, 6, cvb.MatType.CV_32F, cvb.Scalar.All(0))
        For y = 0 To c.Rows - 1
            For x = 0 To c.Cols - 1
                Dim v6 = New cvb.Vec6f(c.Get(Of Single)(y, x), c.Get(Of Single)(y, x + 1), c.Get(Of Single)(y, x + 2), x, y, 0)
                learnInput.Set(Of cvb.Vec6f)(y, x, v6)
            Next
        Next

        ' Now learn what depths are associated with which colors.
        Dim rtree = cvb.ML.RTrees.Create()
        rtree.Train(learnInput, cvb.ML.SampleTypes.RowSample, depthResponse)

        src.ConvertTo(color32f, cvb.MatType.CV_32FC3)
        Dim allC = color32f.Reshape(1, color32f.Total) ' test the entire original image.
        Dim input As New cvb.Mat(allC.Rows, 6, cvb.MatType.CV_32F, cvb.Scalar.All(0))
        For y = 0 To allC.Rows - 1
            For x = 0 To allC.Cols - 1
                Dim v6 = New cvb.Vec6f(allC.Get(Of Single)(y, x), allC.Get(Of Single)(y, x + 1), allC.Get(Of Single)(y, x + 2), x, y, 0)
                input.Set(Of cvb.Vec6f)(y, x, v6)
            Next
        Next

        Dim output As New cvb.Mat
        rtree.Predict(input, output)
        Dim predictedDepth = output.Reshape(1, src.Height)

        colorizer.Run(predictedDepth)
        dst2 = colorizer.dst2.Clone()

        mats.Run(empty)
        dst3 = mats.dst2
        labels(3) = "shadow, empty, Depth Mask < " + CStr(task.gOptions.maxDepth) + ", Learn Input"
    End Sub
End Class






Public Structure mlColor
    Dim colorIndex As Single
    Dim x As Single
    Dim y As Single
End Structure


Public Class ML_Color2Depth : Inherits VB_Parent
    Dim minMax As New Gridgid_MinMaxDepth
    Dim color8U As New Color8U_Basics
    Public Sub New()
        task.redOptions.ColorSource.SelectedItem() = "Bin4Way_Regions"
        desc = "Prepare a grid of color and depth data."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        color8U.Run(src)
        dst2 = color8U.dst3
        labels(2) = "Output of Color8U_Basics running " + task.redOptions.colorInputName

        Dim rtree = cvb.ML.RTrees.Create()
        Dim mlInput As New List(Of mlColor)
        Dim mResponse As New List(Of Single)
        Dim predictList As New List(Of mlColor)
        Dim roiPredict As New List(Of cvb.Rect)
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

        Dim mLearn As cvb.Mat = cvb.Mat.FromPixelData(mlInput.Count, 3, cvb.MatType.CV_32F, mlInput.ToArray)
        Dim response As cvb.Mat = cvb.Mat.FromPixelData(mResponse.Count, 1, cvb.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cvb.ML.SampleTypes.RowSample, response)

        Dim predMat = cvb.Mat.FromPixelData(predictList.Count, 3, cvb.MatType.CV_32F, predictList.ToArray)
        Dim output = New cvb.Mat(predictList.Count, 1, cvb.MatType.CV_32FC1, cvb.Scalar.All(0))
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
Public Class ML_ColorInTier2Depth : Inherits VB_Parent
    Dim minMax As New Gridgid_MinMaxDepth
    Dim color8U As New Color8U_Basics
    Dim tiers As New Contour_DepthTiers
    Public Sub New()
        task.redOptions.ColorSource.SelectedItem() = "Bin4Way_Regions"
        desc = "Prepare a grid of color and depth data."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        color8U.Run(src)
        dst2 = color8U.dst3
        labels(2) = "Output of Color8U_Basics running " + task.redOptions.colorInputName

        Dim rtree = cvb.ML.RTrees.Create()
        Dim mlInput As New List(Of mlColorInTier)
        Dim mResponse As New List(Of Single)
        Dim predictList As New List(Of mlColorInTier)
        Dim roiPredict As New List(Of cvb.Rect)
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

        Dim mLearn As cvb.Mat = cvb.Mat.FromPixelData(mlInput.Count, 3, cvb.MatType.CV_32F, mlInput.ToArray)
        Dim response As cvb.Mat = cvb.Mat.FromPixelData(mResponse.Count, 1, cvb.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cvb.ML.SampleTypes.RowSample, response)

        Dim predMat = cvb.Mat.FromPixelData(predictList.Count, 3, cvb.MatType.CV_32F, predictList.ToArray)
        Dim output = New cvb.Mat(predictList.Count, cvb.MatType.CV_32FC1, 0)
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







Public Class ML_RemoveDups_CPP_VB : Inherits VB_Parent
    Public Sub New()
        cPtr = ML_RemoveDups_Open()
        labels = {"", "", "BGR input below is converted to BGRA and sorted as integers", ""}
        desc = "The input is BGR, convert to BGRA, and sorted as an integer.  The output is a sorted BGR Mat file with duplicates removed."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Type = cvb.MatType.CV_8UC3 Then
            dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_32S, src.CvtColor(cvb.ColorConversionCodes.BGR2BGRA).Data)
        Else
            dst2 = src.Clone
        End If

        Dim dataSrc(dst2.Total * dst2.ElemSize) As Byte
        Marshal.Copy(dst2.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = ML_RemoveDups_Run(cPtr, handleSrc.AddrOfPinnedObject(), dst2.Rows, dst2.Cols, dst2.Type)
        handleSrc.Free()

        Dim compressedCount = ML_RemoveDups_GetCount(cPtr)
        If src.Type = cvb.MatType.CV_32S Then
            dst3 = cvb.Mat.FromPixelData(dst2.Rows, dst2.Cols, dst2.Type, imagePtr).Clone
            Dim tmp = cvb.Mat.FromPixelData(dst2.Rows, dst2.Cols, cvb.MatType.CV_8UC4, dst3.Data)
            dst3 = tmp.CvtColor(cvb.ColorConversionCodes.BGRA2BGR)
        Else
            dst3 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8U, imagePtr).Clone
        End If

        labels(3) = "The BGR data in dst2 after removing duplicate BGR entries.  Input count = " + CStr(dst2.Total) + " output = " + CStr(compressedCount)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = ML_RemoveDups_Close(cPtr)
    End Sub
End Class







Public Class ML_LearnZfromXGray : Inherits VB_Parent
    Dim regions As New GuidedBP_Regions
    Public Sub New()
        task.redOptions.IdentifyCells.Checked = False
        desc = "This runs and is helpful to understanding how to use rtree.  Learn Z from X, Y, and grayscale of the RedCloud cells."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim gray = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY) ' input to ML

        regions.Run(src)

        Dim ptList As New List(Of cvb.Point3f)
        Dim mlInput As New List(Of cvb.Vec3f)
        Dim mResponse As New List(Of Single)
        For y = 0 To regions.cellMapX.Height - 1
            For x = 0 To regions.cellMapX.Width - 1
                Dim zVal = task.pcSplit(2).Get(Of Single)(y, x)
                Dim val = CSng(gray.Get(Of Byte)(y, x))
                If zVal = 0 Then
                    ptList.Add(New cvb.Point3f(CSng(x), CSng(y), val))
                Else
                    mlInput.Add(New cvb.Vec3f(val, x, y))
                    mResponse.Add(zVal)
                End If
            Next
        Next

        Dim rtree = cvb.ML.RTrees.Create()
        Dim mLearn As cvb.Mat = cvb.Mat.FromPixelData(mlInput.Count, 3, cvb.MatType.CV_32F, mlInput.ToArray)
        Dim response As cvb.Mat = cvb.Mat.FromPixelData(mResponse.Count, 1, cvb.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cvb.ML.SampleTypes.RowSample, response)

        Dim predMat = cvb.Mat.FromPixelData(ptList.Count, 3, cvb.MatType.CV_32F, ptList.ToArray)
        Dim output = New cvb.Mat(ptList.Count, 1, cvb.MatType.CV_32FC1, cvb.Scalar.All(0))
        rtree.Predict(predMat, output)
    End Sub
End Class







Public Class ML_LearnRegions : Inherits VB_Parent
    Dim regions As New GuidedBP_Regions
    Dim color8U As New Color8U_Basics
    Public Sub New()
        task.gOptions.setDisplay1()
        task.redOptions.IdentifyCells.Checked = False
        labels = {"", "", "Entire image after ML", "ML Predictions where no region was defined."}
        desc = "Learn region from X, Y, and grayscale for the RedCloud cells."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        regions.Run(src)

        color8U.Run(src)
        dst1 = color8U.dst3

        Dim graySrc = If(dst1.Channels = 1, dst1, dst1.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)) ' input to ML
        Dim regionX = regions.cellMapX ' Target variable

        Dim ptList As New List(Of cvb.Point3f)
        Dim mlInput As New List(Of cvb.Vec3f)
        Dim mResponse As New List(Of Single)

        For y = 0 To regions.cellMapX.Height - 1
            For x = 0 To regions.cellMapX.Width - 1
                Dim gray = CSng(graySrc.Get(Of Byte)(y, x))
                Dim region = CSng(regionX.Get(Of Byte)(y, x))
                If region = 0 Then
                    ptList.Add(New cvb.Point3f(CSng(x), CSng(y), gray))
                Else
                    mlInput.Add(New cvb.Vec3f(gray, x, y))
                    mResponse.Add(region)
                End If
            Next
        Next

        Dim rtree = cvb.ML.RTrees.Create()
        Dim mLearn As cvb.Mat = cvb.Mat.FromPixelData(mlInput.Count, 3, cvb.MatType.CV_32F, mlInput.ToArray)
        Dim response As cvb.Mat = cvb.Mat.FromPixelData(mResponse.Count, 1, cvb.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cvb.ML.SampleTypes.RowSample, response)

        Dim predMat = cvb.Mat.FromPixelData(ptList.Count, 3, cvb.MatType.CV_32F, ptList.ToArray)
        Dim output = New cvb.Mat(ptList.Count, 1, cvb.MatType.CV_32FC1, cvb.Scalar.All(0))
        rtree.Predict(predMat, output)

        regions.mats.mat(0).CopyTo(dst2)
        dst3.SetTo(0)
        For i = 0 To ptList.Count - 1
            Dim pt = ptList(i)
            Dim regionID = CInt(output.Get(Of Single)(i, 0))
            Dim rc = regions.xCells(regionID)
            dst2.Set(Of cvb.Vec3b)(pt.Y, pt.X, rc.naturalColor)
            dst3.Set(Of cvb.Vec3b)(pt.Y, pt.X, rc.naturalColor)
        Next
    End Sub
End Class