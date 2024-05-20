Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Threading
Public Class ML_Basics : Inherits VB_Algorithm
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        labels = {"", "depth32f - 32fc3 format with missing depth filled with predicted depth based on color (brighter is farther)", "", "Color used for roi prediction"}
        desc = "Predict depth from color to fill in the depth shadow areas"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static noDepthCount() As Integer
        Static roiColor() As cv.Vec3b
        ReDim noDepthCount(task.gridList.Count - 1)
        ReDim roiColor(task.gridList.Count - 1)

        dst2.SetTo(0)
        Parallel.For(0, task.gridList.Count,
        Sub(i)
            Dim roi = task.gridList(i)
            roiColor(i) = src(roi).Get(Of cv.Vec3b)(roi.Height / 2, roi.Width / 2)
            dst2(roi).SetTo(roiColor(i), task.depthMask(roi))
            noDepthCount(i) = task.noDepthMask(roi).CountNonZero
        End Sub)

        Dim rtree = cv.ML.RTrees.Create()
        Dim mlInput As New List(Of mlData)
        Dim mResponse As New List(Of Single)
        For i = 0 To task.gridList.Count - 1
            If noDepthCount(i) = 0 Then Continue For
            Dim ml As mlData
            Dim roi = task.gridList(i)
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
            setTrueText(strOut, 3)
            Exit Sub
        End If

        Dim mLearn As cv.Mat = New cv.Mat(mlInput.Count, 5, cv.MatType.CV_32F, mlInput.ToArray)
        Dim response As cv.Mat = New cv.Mat(mResponse.Count, 1, cv.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cv.ML.SampleTypes.RowSample, response)

        Dim predictList As New List(Of mlData)
        Dim colors As New List(Of cv.Vec3b)
        Dim saveRoi As New List(Of cv.Rect)
        Dim depthMask As New List(Of cv.Mat)
        For i = 0 To task.gridList.Count - 1
            If noDepthCount(i) = 0 Then Continue For
            Dim roi = task.gridList(i)
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

        Dim predMat = New cv.Mat(predictList.Count, 5, cv.MatType.CV_32F, predictList.ToArray)
        Dim output = New cv.Mat(predictList.Count, cv.MatType.CV_32FC1, 0)
        rtree.Predict(predMat, output)

        dst1 = task.pcSplit(2)
        dst3.SetTo(0)
        For i = 0 To predictList.Count - 1
            Dim roi = saveRoi(i)
            Dim depth = output.Get(Of Single)(i, 0)
            dst1(roi).SetTo(depth, depthMask(i))
            dst3(roi).SetTo(colors(i), depthMask(i))
        Next

        labels(2) = CStr(task.gridList.Count) + " regions with " + CStr(mlInput.Count) + " used for learning and " + CStr(predictList.Count) + " were predicted"
    End Sub
End Class






Module ML__Exports
    Private Class CompareVec3f : Implements IComparer(Of cv.Vec3f)
        Public Function Compare(ByVal a As cv.Vec3f, ByVal b As cv.Vec3f) As Integer Implements IComparer(Of cv.Vec3f).Compare
            If a(0) = b(0) And a(1) = b(1) And a(2) = b(2) Then Return 0
            Return If(a(0) < b(0), -1, 1)
        End Function
    End Class
    Public Function detectAndFillShadow(holeMask As cv.Mat, borderMask As cv.Mat, depth32f As cv.Mat, color As cv.Mat, minLearnCount As Integer) As cv.Mat
        Dim learnData As New SortedList(Of cv.Vec3f, Single)(New CompareVec3f)
        Dim rng As New System.Random
        Dim holeCount = cv.Cv2.CountNonZero(holeMask)
        If borderMask.Channels <> 1 Then borderMask = borderMask.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim borderCount = cv.Cv2.CountNonZero(borderMask)
        If holeCount > 0 And borderCount > minLearnCount Then
            Dim color32f As New cv.Mat
            color.ConvertTo(color32f, cv.MatType.CV_32FC3)

            Dim learnInputList As New List(Of cv.Vec3f)
            Dim responseInputList As New List(Of Single)

            For y = 0 To holeMask.Rows - 1
                For x = 0 To holeMask.Cols - 1
                    If borderMask.Get(Of Byte)(y, x) Then
                        Dim vec = color32f.Get(Of cv.Vec3f)(y, x)
                        If learnData.ContainsKey(vec) = False Then
                            learnData.Add(vec, depth32f.Get(Of Single)(y, x)) ' keep out duplicates.
                            learnInputList.Add(vec)
                            responseInputList.Add(depth32f.Get(Of Single)(y, x))
                        End If
                    End If
                Next
            Next

            Dim learnInput As New cv.Mat(learnData.Count, 3, cv.MatType.CV_32F, learnInputList.ToArray())
            Dim depthResponse As New cv.Mat(learnData.Count, 1, cv.MatType.CV_32F, responseInputList.ToArray())

            ' now learn what depths are associated with which colors.
            Dim rtree = cv.ML.RTrees.Create()
            rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)

            ' now predict what the depth is based just on the color (and proximity to the region)
            Using predictMat As New cv.Mat(1, 3, cv.MatType.CV_32F)
                For y = 0 To holeMask.Rows - 1
                    For x = 0 To holeMask.Cols - 1
                        If holeMask.Get(Of Byte)(y, x) Then
                            predictMat.Set(Of cv.Vec3f)(0, 0, color32f.Get(Of cv.Vec3f)(y, x))
                            depth32f.Set(Of Single)(y, x, rtree.Predict(predictMat))
                        End If
                    Next
                Next
            End Using
        End If
        Return depth32f
    End Function
End Module







Public Class ML_FillRGBDepth_MT : Inherits VB_Algorithm
    Dim shadow As New Depth_Holes
    Dim colorizer As New Depth_Colorizer_CPP
    Public Sub New()
        gOptions.GridSize.Maximum = dst2.Cols / 2
        gOptions.GridSize.Value = dst2.Cols / 2

        labels = {"", "", "ML filled shadow", ""}
        desc = "Predict depth based on color and colorize depth to confirm correctness of model.  NOTE: memory leak occurs if more multi-threading is used!"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim minLearnCount = 5
        Parallel.ForEach(task.gridList,
            Sub(roi)
                task.pcSplit(2)(roi) = detectAndFillShadow(task.noDepthMask(roi), shadow.dst3(roi), task.pcSplit(2)(roi), src(roi), minLearnCount)
            End Sub)

        colorizer.Run(task.pcSplit(2))
        dst2 = colorizer.dst2.Clone()
        dst2.SetTo(cv.Scalar.White, task.gridMask)
    End Sub
End Class






Public Class ML_DepthFromColor : Inherits VB_Algorithm
    Dim colorizer As New Depth_Colorizer_CPP
    Dim mats As New Mat_4Click
    Dim resizer As New Resize_Smaller
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Prediction Max Depth", 1000, 5000, 1500)
        findSlider("Resize Percentage (%)").Value = 2 ' 2% of the image.
        labels(3) = "Click any quadrant at left to view it below"
        desc = "Use BGR to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static depthSlider = findSlider("Prediction Max Depth")
        mats.mat(1) = task.noDepthMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat
        resizer.Run(src)

        Dim colorROI As New cv.Rect(0, 0, resizer.newSize.Width, resizer.newSize.Height)
        resizer.dst2.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim shadowSmall = mats.mat(1).Resize(color32f.Size()).Clone()
        color32f.SetTo(cv.Scalar.Black, shadowSmall) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Dim depth = task.pcSplit(2).Resize(color32f.Size())

        Dim mask = depth.Threshold(depthSlider.Value / 1000, depthSlider.Value / 1000, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        mats.mat(2) = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        mask = Not mask
        depth.SetTo(depthSlider.Value / 1000, mask)

        colorizer.Run(depth)
        mats.mat(3) = colorizer.dst2.Clone()

        mask = depth.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim maskCount = mask.CountNonZero
        dst2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

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

        colorizer.Run(predictedDepth)
        mats.mat(0) = colorizer.dst2.Clone()

        mats.Run(empty)
        dst2 = mats.dst2
        labels(2) = "prediction, shadow, Depth Mask < " + CStr(depthSlider.Value) + ", Learn Input"
        dst3 = mats.dst3
    End Sub
End Class



Public Class ML_DepthFromXYColor : Inherits VB_Algorithm
    Dim mats As New Mat_4to1
    Dim shadow As New Depth_Holes
    Dim resizer As New Resize_Smaller
    Dim colorizer As New Depth_Colorizer_CPP
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Prediction Max Depth", 1000, 5000, 1500)
        findSlider("Resize Percentage (%)").Value = 2 ' 2% of the image.
        labels(2) = "Predicted Depth"
        desc = "Use BGR to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static depthSlider = findSlider("Prediction Max Depth")
        shadow.Run(src)
        mats.mat(0) = shadow.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat

        resizer.Run(src)

        Dim colorROI As New cv.Rect(0, 0, resizer.newSize.Width, resizer.newSize.Height)
        resizer.dst2.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim shadowSmall = shadow.dst2.Resize(color32f.Size()).Clone()
        color32f.SetTo(cv.Scalar.Black, shadowSmall) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Dim depth32f = task.pcSplit(2).Resize(color32f.Size())

        Dim mask = depth32f.Threshold(depthSlider.Value, depthSlider.Value, cv.ThresholdTypes.BinaryInv)
        mask.SetTo(0, shadowSmall) ' remove the unknown depth...
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        mats.mat(2) = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        mask = Not mask
        depth32f.SetTo(depthSlider.Value, mask)

        colorizer.Run(depth32f)
        mats.mat(3) = colorizer.dst2.Clone()

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim maskCount = mask.CountNonZero
        dst2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim c = color32f.Reshape(1, color32f.Total)
        Dim depthResponse = depth32f.Reshape(1, depth32f.Total)

        Dim learnInput As New cv.Mat(c.Rows, 6, cv.MatType.CV_32F, 0)
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
        Dim input As New cv.Mat(allC.Rows, 6, cv.MatType.CV_32F, 0)
        For y = 0 To allC.Rows - 1
            For x = 0 To allC.Cols - 1
                Dim v6 = New cv.Vec6f(allC.Get(Of Single)(y, x), allC.Get(Of Single)(y, x + 1), allC.Get(Of Single)(y, x + 2), x, y, 0)
                input.Set(Of cv.Vec6f)(y, x, v6)
            Next
        Next

        Dim output As New cv.Mat
        rtree.Predict(input, output)
        Dim predictedDepth = output.Reshape(1, src.Height)

        colorizer.Run(predictedDepth)
        dst2 = colorizer.dst2.Clone()

        mats.Run(empty)
        dst3 = mats.dst2
        labels(3) = "shadow, empty, Depth Mask < " + CStr(depthSlider.Value) + ", Learn Input"
    End Sub
End Class






Public Structure mlColor
    Dim colorIndex As Single
    Dim x As Single
    Dim y As Single
End Structure


Public Class ML_Color2Depth : Inherits VB_Algorithm
    Dim minMax As New Grid_MinMaxDepth
    Dim colorClass As New BGR2Gray_Basics
    Public Sub New()
        redOptions.ColorSource.SelectedItem() = "Bin4Way_Regions"
        desc = "Prepare a grid of color and depth data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorClass.Run(src)
        dst2 = colorClass.dst3
        labels(2) = "Output of BGR2Gray_Basics running " + redOptions.colorInputName

        Dim rtree = cv.ML.RTrees.Create()
        Dim mlInput As New List(Of mlColor)
        Dim mResponse As New List(Of Single)
        Dim predictList As New List(Of mlColor)
        Dim roiPredict As New List(Of cv.Rect)
        For i = 0 To task.gridList.Count - 1
            Dim roi = task.gridList(i)
            Dim mls As mlColor
            mls.colorIndex = colorClass.dst2.Get(Of Byte)(roi.Y, roi.X)
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
            setTrueText("No learning data was found or provided.  Exit...", 3)
            Exit Sub
        End If

        Dim mLearn As cv.Mat = New cv.Mat(mlInput.Count, 3, cv.MatType.CV_32F, mlInput.ToArray)
        Dim response As cv.Mat = New cv.Mat(mResponse.Count, 1, cv.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cv.ML.SampleTypes.RowSample, response)

        Dim predMat = New cv.Mat(predictList.Count, 3, cv.MatType.CV_32F, predictList.ToArray)
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





Public Structure mlColorInTier
    Dim colorIndex As Single
    Dim x As Single
    Dim y As Single
End Structure
Public Class ML_ColorInTier2Depth : Inherits VB_Algorithm
    Dim minMax As New Grid_MinMaxDepth
    Dim colorClass As New BGR2Gray_Basics
    Dim tiers As New Contour_DepthTiers
    Public Sub New()
        redOptions.ColorSource.SelectedItem() = "Bin4Way_Regions"
        desc = "Prepare a grid of color and depth data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorClass.Run(src)
        dst2 = colorClass.dst3
        labels(2) = "Output of BGR2Gray_Basics running " + redOptions.colorInputName

        Dim rtree = cv.ML.RTrees.Create()
        Dim mlInput As New List(Of mlColorInTier)
        Dim mResponse As New List(Of Single)
        Dim predictList As New List(Of mlColorInTier)
        Dim roiPredict As New List(Of cv.Rect)
        For i = 0 To task.gridList.Count - 1
            Dim roi = task.gridList(i)
            Dim mls As mlColorInTier
            mls.colorIndex = colorClass.dst2.Get(Of Byte)(roi.Y, roi.X)
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
            setTrueText("No learning data was found or provided.  Exit...", 3)
            Exit Sub
        End If

        Dim mLearn As cv.Mat = New cv.Mat(mlInput.Count, 3, cv.MatType.CV_32F, mlInput.ToArray)
        Dim response As cv.Mat = New cv.Mat(mResponse.Count, 1, cv.MatType.CV_32F, mResponse.ToArray)
        rtree.Train(mLearn, cv.ML.SampleTypes.RowSample, response)

        Dim predMat = New cv.Mat(predictList.Count, 3, cv.MatType.CV_32F, predictList.ToArray)
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







Public Class ML_RemoveDups_CPP : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold for sort input", 0, 255, 127)
        cPtr = ML_RemoveDups_Open()
        labels = {"", "", "BGR input below is converted to BGRA and sorted as integers - use slider to adjust", ""}
        desc = "The input is BGR, convert to BGRA, and sorted as an integer.  The output is a sorted BGR Mat file with duplicates removed."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Threshold for sort input")
        If src.Type = cv.MatType.CV_8UC3 Then
            dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_32S, src.CvtColor(cv.ColorConversionCodes.BGR2BGRA).Data)
        Else
            dst2 = src.Clone
        End If

        Dim dataSrc(dst2.Total * dst2.ElemSize) As Byte
        Marshal.Copy(dst2.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = ML_RemoveDups_Run(cPtr, handleSrc.AddrOfPinnedObject(), dst2.Rows, dst2.Cols, dst2.Type)
        handleSrc.Free()

        Dim compressedCount = ML_RemoveDups_GetCount(cPtr)
        If src.Type = cv.MatType.CV_32S Then
            dst3 = New cv.Mat(dst2.Rows, dst2.Cols, dst2.Type, imagePtr).Clone
            Dim tmp = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC4, dst3.Data)
            dst3 = tmp.CvtColor(cv.ColorConversionCodes.BGRA2BGR)
        Else
            dst3 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
        End If

        labels(3) = "The BGR data in dst2 after removing duplicate BGR entries.  Input count = " + CStr(dst2.Total) + " output = " + CStr(compressedCount)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = ML_RemoveDups_Close(cPtr)
    End Sub
End Class

