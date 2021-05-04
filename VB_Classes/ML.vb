Imports cv = OpenCvSharp
Imports System.Threading
Module ML__Exports
    Private Class CompareVec3f : Implements IComparer(Of cv.Vec3f)
        Public Function Compare(ByVal a As cv.Vec3f, ByVal b As cv.Vec3f) As Integer Implements IComparer(Of cv.Vec3f).Compare
            If a(0) = b(0) And a(1) = b(1) And a(2) = b(2) Then Return 0
            Return If(a(0) < b(0), -1, 1)
        End Function
    End Class
    Public Function detectAndFillShadow(holeMask As cv.Mat, borderMask As cv.Mat, depth32f As cv.Mat, color As cv.Mat, minLearnCount As integer) As cv.Mat
        Dim learnData As New SortedList(Of cv.Vec3f, Single)(New CompareVec3f)
        Dim rng As New System.Random
        Dim holeCount = cv.Cv2.CountNonZero(holeMask)
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
Public Class ML_FillRGBDepth_MT : Inherits VBparent
    Dim shadow As New Depth_Holes
    Dim grid As New Thread_Grid
    Dim colorizer As New Depth_Colorizer_CPP
    Public Sub New()
        findSlider("ThreadGrid Width").Value = dst1.Cols / 2 ' change this higher to see the memory leak (or comment prediction loop above - it is the problem.)
        findSlider("ThreadGrid Height").Value = dst1.Rows / 4

        label1 = "ML filled shadow"
        label2 = ""
        task.desc = "Predict depth based on color and colorize depth to confirm correctness of model.  NOTE: memory leak occurs if more multi-threading is used!"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        shadow.Run(src)
        grid.Run(Nothing)
        Dim minLearnCount = 5
        Parallel.ForEach(grid.roiList,
            Sub(roi)
                task.depth32f(roi) = detectAndFillShadow(shadow.holeMask(roi), shadow.dst2(roi), task.depth32f(roi), src(roi), minLearnCount)
            End Sub)

        colorizer.Run(task.depth32f)
        dst1 = colorizer.dst1.Clone()
        dst1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class





Public Class ML_FillRGBDepth : Inherits VBparent
    Dim shadow As New Depth_Holes
    Dim colorizer As New Depth_Colorizer_CPP
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "ML Min Learn Count", 2, 100, 5)
        End If

        shadow.sliders.trackbar(0).Value = 3

        label2 = "ML filled shadow"
        task.desc = "Predict depth based on color and display colorized depth to confirm correctness of model."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        shadow.Run(src)
        Dim minLearnCount = sliders.trackbar(0).Value
        task.RGBDepth.CopyTo(dst1)
        task.depth32f = detectAndFillShadow(shadow.holeMask, shadow.dst2, task.depth32f, src, minLearnCount)
        colorizer.Run(task.depth32f)
        dst2 = colorizer.dst1
    End Sub
End Class





Public Class ML_DepthFromColor_MT : Inherits VBparent
    Dim colorizer As New Depth_Colorizer_CPP
    Dim grid As New Thread_Grid
    Dim dilate As New DilateErode_Basics
    Public Sub New()
        dilate.sliders.trackbar(1).Value = 2

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Prediction Max Depth", 500, 5000, 1000)
        End If
        findSlider("ThreadGrid Width").Value = 16
        findSlider("ThreadGrid Height").Value = 16

        label1 = "Predicted Depth"
        label2 = "Mask of color and depth input"
        task.desc = "Use RGB, X, and Y to predict depth across the entire image, maxDepth = slider value."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.Run(Nothing)

        Dim mask = task.depth32f.Threshold(sliders.trackbar(0).Value, sliders.trackbar(0).Value, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        task.depth32f.SetTo(sliders.trackbar(0).Value, mask)

        Dim predictedDepth As New cv.Mat(task.depth32f.Size(), cv.MatType.CV_32F, 0)

        mask = task.depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        dilate.Run(mask)
        mask = dilate.dst1
        dst2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat
        src.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim predictedRegions As Integer
        Parallel.ForEach(grid.roiList,
            Sub(roi)
                Dim maskCount = roi.Width * roi.Height - mask(roi).CountNonZero()
                If maskCount > 10 Then
                    Interlocked.Add(predictedRegions, 1)
                    Dim learnInput = color32f(roi).Clone()
                    learnInput = learnInput.Reshape(1, roi.Width * roi.Height)
                    Dim depthResponse = task.depth32f(roi).Clone()
                    depthResponse = depthResponse.Reshape(1, roi.Width * roi.Height)

                    Dim rtree = cv.ML.RTrees.Create()
                    rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)
                    rtree.Predict(learnInput, depthResponse)
                    predictedDepth(roi) = depthResponse.Reshape(1, roi.Height)
                End If
            End Sub)
        label2 = "Input region count = " + CStr(predictedRegions) + " of " + CStr(grid.roiList.Count)
        colorizer.Run(predictedDepth)
        dst1 = colorizer.dst1
    End Sub
End Class



Public Class ML_DepthFromColor : Inherits VBparent
    Dim colorizer As New Depth_Colorizer_CPP
    Dim mats As New Mat_4to1
    Dim shadow As New Depth_Holes
    Dim resized As Resize_Percentage
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Prediction Max Depth", 1000, 5000, 1500)
        End If
        resized = New Resize_Percentage()
        resized.sliders.trackbar(0).Value = 2 ' 2% of the image.


        label2 = "Click any quadrant at left to view it below"
        task.desc = "Use RGB to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        shadow.Run(src)
        mats.mat(1) = shadow.holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat

        resized.Run(src)

        Dim colorROI As New cv.Rect(0, 0, resized.resizeOptions.newSize.Width, resized.resizeOptions.newSize.Height)
        resized.dst1.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim shadowSmall = mats.mat(1).Resize(color32f.Size()).Clone()
        color32f.SetTo(cv.Scalar.Black, shadowSmall) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Dim depth32f = task.depth32f.Resize(color32f.Size())

        Dim mask = depth32f.Threshold(sliders.trackbar(0).Value, sliders.trackbar(0).Value, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        mats.mat(2) = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        cv.Cv2.BitwiseNot(mask, mask)
        depth32f.SetTo(sliders.trackbar(0).Value, mask)

        colorizer.Run(depth32f)
        mats.mat(3) = colorizer.dst1.Clone()

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim maskCount = mask.CountNonZero()
        dst1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim learnInput = color32f.Reshape(1, color32f.Total)
        Dim depthResponse = depth32f.Reshape(1, depth32f.Total)

        ' now learn what depths are associated with which colors.
        Dim rtree = cv.ML.RTrees.Create()
        rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)

        src.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim input = color32f.Reshape(1, color32f.Total) ' test the entire original image.
        Dim output As New cv.Mat
        rtree.Predict(input, output)
        Dim predictedDepth = output.Reshape(1, src.Height)

        colorizer.Run(predictedDepth)
        mats.mat(0) = colorizer.dst1.Clone()

        mats.Run(Nothing)
        dst1 = mats.dst1
        label1 = "prediction, shadow, Depth Mask < " + CStr(sliders.trackbar(0).Value) + ", Learn Input"
        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setMyActiveMat()
        dst2 = mats.mat(quadrantIndex)
    End Sub
End Class



Public Class ML_DepthFromXYColor : Inherits VBparent
    Dim mats As New Mat_4to1
    Dim shadow As New Depth_Holes
    Dim resized As Resize_Percentage
    Dim colorizer As New Depth_Colorizer_CPP
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Prediction Max Depth", 1000, 5000, 1500)
        End If
        resized = New Resize_Percentage()
        resized.sliders.trackbar(0).Value = 2

        label1 = "Predicted Depth"
        task.desc = "Use RGB to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        shadow.Run(src)
        mats.mat(0) = shadow.holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat

        resized.Run(src)

        Dim colorROI As New cv.Rect(0, 0, resized.resizeOptions.newSize.Width, resized.resizeOptions.newSize.Height)
        resized.dst1.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim shadowSmall = shadow.holeMask.Resize(color32f.Size()).Clone()
        color32f.SetTo(cv.Scalar.Black, shadowSmall) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Dim depth32f = task.depth32f.Resize(color32f.Size())

        Dim mask = depth32f.Threshold(sliders.trackbar(0).Value, sliders.trackbar(0).Value, cv.ThresholdTypes.BinaryInv)
        mask.SetTo(0, shadowSmall) ' remove the unknown depth...
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        mats.mat(2) = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        cv.Cv2.BitwiseNot(mask, mask)
        depth32f.SetTo(sliders.trackbar(0).Value, mask)

        colorizer.Run(depth32f)
        mats.mat(3) = colorizer.dst1.Clone()

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim maskCount = mask.CountNonZero()
        dst1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

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
        dst1 = colorizer.dst1.Clone()

        mats.Run(Nothing)
        dst2 = mats.dst1
        label2 = "shadow, empty, Depth Mask < " + CStr(sliders.trackbar(0).Value) + ", Learn Input"
    End Sub
End Class




Public Class ML_EdgeDepth_MT : Inherits VBparent
    Dim colorizer As New Depth_Colorizer_CPP
    Dim grid As New Thread_Grid
    Dim dilate As New DilateErode_Basics
    Public Sub New()
        dilate.sliders.trackbar(1).Value = 5

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Prediction Max Depth", 500, 5000, 1000)
        End If
        findSlider("ThreadGrid Width").Value = 16
        findSlider("ThreadGrid Height").Value = 16

        label1 = "Depth Shadow (inverse of color and depth)"
        label2 = "Predicted Depth"
        task.desc = "Use RGB to predict depth near edges."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.Run(Nothing)

        Dim mask = task.depth32f.Threshold(sliders.trackbar(0).Value, sliders.trackbar(0).Value, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        task.depth32f.SetTo(sliders.trackbar(0).Value, mask)

        Dim predictedDepth As New cv.Mat(task.depth32f.Size(), cv.MatType.CV_32F, 0)

        mask = task.depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        dilate.Run(mask)
        dst1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat
        src.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim predictedRegions As integer
        Parallel.ForEach(grid.roiList,
            Sub(roi)
                Dim maskCount = mask(roi).CountNonZero()
                If maskCount = 0 Then ' if no bad pixels, then learn and predict
                    maskCount = mask(roi).Total() - maskCount
                    Interlocked.Add(predictedRegions, 1)
                    Dim learnInput = color32f(roi).Clone()
                    learnInput = learnInput.Reshape(1, maskCount)
                    Dim depthResponse = task.depth32f(roi).Clone()
                    depthResponse = depthResponse.Reshape(1, maskCount)

                    Dim rtree = cv.ML.RTrees.Create()
                    rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)
                    rtree.Predict(learnInput, depthResponse)
                    predictedDepth(roi) = depthResponse.Reshape(1, roi.Height)
                End If
            End Sub)
        label2 = "Input region count = " + CStr(predictedRegions) + " of " + CStr(grid.roiList.Count)
        colorizer.Run(predictedDepth)
        dst2 = colorizer.dst1
    End Sub
End Class







'Public Class ML_Simple
'    Inherits VBParent
'    Public trainData As cv.Mat
'    Public response As cv.Mat
'    Dim rtree = cv.ML.RTrees.Create()
'    Public predictions As New cv.Mat
'    Dim emax as EMax_Centroids
'    Public Sub New()

'        If standalone or task.intermediateReview = caller Then
'            emax = New EMax_Centroids()
'            emax.emaxCPP.basics.grid.sliders.trackbar(0).Value = 270
'            emax.emaxCPP.basics.grid.sliders.trackbar(1).Value = 150
'        End If

'        label1 = ""
'        label2 = ""
'        task.desc = "Simplest form for using RandomForest in OpenCV"
'    End Sub
'    Private Function convertScalarToVec3b(s As cv.Scalar) As cv.Vec3b
'        Dim vec As New cv.Mat
'        Dim tmp = New cv.Mat(1, 1, cv.MatType.CV_32FC3, s)
'        tmp.ConvertTo(vec, cv.MatType.CV_8UC3)
'        Return New cv.Vec3b(vec.Get(Of Byte)(0, 0), vec.Get(Of Byte)(0, 1), vec.Get(Of Byte)(0, 2))
'    End Function
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        Static lastColors As New cv.Mat
'        If standalone or task.intermediateReview = caller Then
'            emax.Run()
'            dst1 = emax.dst1.Clone()
'        End If
'        Dim nextResponse = emax.response.Clone
'        Dim nextInput = emax.descriptors.Clone
'        If task.frameCount = 0 Then
'            trainData = nextInput
'            response = nextResponse
'            rtree.Train(trainData, cv.ML.SampleTypes.RowSample, response)
'            lastColors = emax.dst1.Clone()
'        Else
'            Dim residual As Integer = 20 * nextInput.Rows ' we need about x iterations to settle in on the right values...
'            If trainData.Rows > residual Then
'                cv.Cv2.VConcat(trainData(New cv.Rect(0, trainData.Rows - residual, trainData.Cols, residual)), nextInput, trainData)
'                cv.Cv2.VConcat(response(New cv.Rect(0, response.Rows - residual, response.Cols, residual)), nextResponse, response)
'            Else
'                cv.Cv2.VConcat(trainData, nextInput, trainData)
'                cv.Cv2.VConcat(response, nextResponse, response)
'            End If
'        End If

'        rtree.Predict(nextInput, predictions)

'        If standalone or task.intermediateReview = caller Then
'            Dim truthCount As Integer
'            For i = 0 To nextInput.Rows - 1
'                Dim pt = nextInput.Get(Of cv.Point2f)(i, 0)
'                Dim cIndex = CInt(predictions.Get(Of Single)(i, 0))
'                cv.Cv2.FloodFill(dst1, New cv.Mat, pt, task.scalarColors(cIndex), New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8) Or 4)
'                Dim vec = convertScalarToVec3b(task.scalarColors(cIndex))
'                If vec = lastColors.Get(Of cv.Vec3b)(pt.Y, pt.X) Then truthCount += 1
'                dst1.Circle(pt, task.dotsize + 5, cv.Scalar.Black, -1, task.lineType)
'            Next
'            dst2 = (dst1 - lastColors).ToMat
'            label2 = CStr(truthCount) + " colors correctly predicted with centroid"
'        End If

'        rtree.Train(trainData, cv.ML.SampleTypes.RowSample, response) ' use the latest results to train the next iteration.
'        lastColors = emax.dst1.Clone()
'    End Sub
'End Class

