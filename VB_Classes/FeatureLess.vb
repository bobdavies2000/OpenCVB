Imports cv = OpenCvSharp
Public Class FeatureLess_Basics : Inherits VB_Algorithm
    Dim edgeD As New EdgeDraw_Basics
    Public Sub New()
        labels = {"", "", "EdgeDraw_Basics output", ""}
        desc = "Access the EdgeDraw_Basics algorithm directly rather than through the CPP_Basics interface - more efficient"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edgeD.Run(src)
        dst2 = edgeD.dst2
        If standalone Or testIntermediate(traceName) Then
            dst3 = src.Clone
            dst3.SetTo(cv.Scalar.Yellow, dst2)
        End If
    End Sub
End Class







Public Class FeatureLess_BasicsAccum : Inherits VB_Algorithm
    Dim edgeD As New EdgeDraw_Basics
    Dim sum8u As New History_Sum8u
    Public Sub New()
        gOptions.FrameHistory.Value = 10
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "EdgeDraw_Basics output", ""}
        desc = "Access the EdgeDraw_Basics algorithm directly rather than through the CPP_Basics interface - more efficient"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edgeD.Run(src)

        sum8u.Run(edgeD.dst2)
        dst2 = sum8u.dst2
        If standalone Or testIntermediate(traceName) Then
            dst3 = src.Clone
            dst3.SetTo(cv.Scalar.Yellow, dst2)
        End If
    End Sub
End Class








Public Class FeatureLess_EdgeDrawing : Inherits VB_Algorithm
    Dim cpp As New CPP_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold distance", 0, 100, 10)
        cpp.updateFunction(algorithmList.functionNames.EdgeDraw_Basics)
        desc = "Use EdgeDrawing to define featureless regions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Threshold distance")
        cpp.Run(src)
        dst2 = Not cpp.dst2.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class








Public Class FeatureLess_Canny : Inherits VB_Algorithm
    Dim edges As New Edge_Canny
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold distance", 0, 100, 10)
        desc = "Use Canny edges to define featureless regions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Threshold distance")
        edges.Run(src)
        dst2 = Not edges.dst2.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class








Public Class FeatureLess_Sobel : Inherits VB_Algorithm
    Dim edges As New Edge_Sobel_Old
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold distance", 0, 100, 10)
        desc = "Use Sobel edges to define featureless regions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Threshold distance")
        edges.Run(src)
        dst2 = Not edges.dst2.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class








Public Class FeatureLess_Prediction : Inherits VB_Algorithm
    Dim fLess As New FeatureLess_MotionAccum
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("FeatureLess Resize Percent", 1, 100, 1)
        desc = "Identify the featureless regions, use color and depth to learn the featureless label, and predict depth over the image. - needs more work"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static percentSlider = findSlider("FeatureLess Resize Percent")
        fLess.Run(src)
        dst2 = fLess.dst2
        dst3 = fLess.dst3
        Dim labels = fLess.dst3.Clone()

        Dim percent = Math.Sqrt(percentSlider.Value / 100)
        Dim newSize = New cv.Size(src.Width * percent, src.Height * percent)

        Dim rgb = src.Clone()
        Dim depth32f As cv.Mat = task.pcSplit(2).Resize(newSize)
        Dim mask = fLess.dst3

        rgb = rgb.Resize(newSize)

        ' manually resize the mask to make sure there is no dithering...
        mask = New cv.Mat(depth32f.Size(), cv.MatType.CV_8U, 0)
        Dim labelSmall As New cv.Mat(mask.Size(), cv.MatType.CV_32S, 0)
        Dim xFactor = CInt(fLess.dst3.Width / newSize.Width)
        Dim yFactor = CInt(fLess.dst3.Height / newSize.Height)
        For y = 0 To mask.Height - 2
            For x = 0 To mask.Width - 2
                If fLess.dst3.Get(Of Byte)(y * yFactor, x * xFactor) = 255 Then
                    mask.Set(Of Byte)(y, x, 255)
                    labelSmall.Set(Of Byte)(y, x, labels.Get(Of Byte)(y, x))
                End If
            Next
        Next

        rgb.SetTo(0, mask)
        depth32f.SetTo(0, mask)

        Dim rgb32f As New cv.Mat, response As New cv.Mat
        rgb.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        labelSmall.ConvertTo(response, cv.MatType.CV_32S)

        Dim saveRGB = rgb32f.Clone()

        Dim learnInput As New cv.Mat
        Dim planes() = rgb32f.Split()
        ReDim Preserve planes(3)
        planes(3) = task.pcSplit(2).Resize(newSize)
        cv.Cv2.Merge(planes, learnInput)

        Dim rtree = cv.ML.RTrees.Create()
        learnInput = learnInput.Reshape(1, learnInput.Rows * learnInput.Cols)
        response = response.Reshape(1, response.Rows * response.Cols)
        rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, response)

        mask = Not mask
        rgb32f.SetTo(0)
        depth32f.SetTo(0)
        saveRGB.CopyTo(rgb32f, mask)

        planes = rgb32f.Split()
        ReDim Preserve planes(3)
        planes(3) = depth32f.Clone()
        cv.Cv2.Merge(planes, learnInput)

        learnInput = learnInput.Reshape(1, learnInput.Rows * learnInput.Cols)
        response = response.Reshape(1, response.Rows * response.Cols)
        rtree.Predict(learnInput, response)
        Dim predictedDepth = response.Reshape(1, depth32f.Height)
        predictedDepth.Normalize(0, 255, cv.NormTypes.MinMax)
        predictedDepth.ConvertTo(mask, cv.MatType.CV_8U)
        dst3 = mask.ConvertScaleAbs().Resize(src.Size())
    End Sub
End Class








Public Class FeatureLess_UniquePixels : Inherits VB_Algorithm
    Dim fless As New Hough_FeatureLessTopX
    Dim sort As New Sort_1Channel
    Public Sub New()
        If standalone Then findSlider("Threshold for sort input").Value = 0
        labels = {"", "Gray scale input to sort/remove dups", "Unique pixels", ""}
        desc = "Find the unique gray pixels for the featureless regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fless.Run(src)
        dst2 = fless.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        sort.Run(dst2)
        dst3 = sort.dst2
    End Sub
End Class







Public Class FeatureLess_Unique3Pixels : Inherits VB_Algorithm
    Dim fless As New Hough_FeatureLessTopX
    Dim sort3 As New Sort_3Channel
    Public Sub New()
        desc = "Find the unique 3-channel pixels for the featureless regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fless.Run(src)
        dst2 = fless.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        sort3.Run(fless.dst2)
        dst3 = sort3.dst2
    End Sub
End Class






Public Class FeatureLess_Histogram : Inherits VB_Algorithm
    Dim backP As New BackProject_FeatureLess
    Public Sub New()
        desc = "Create a histogram of the featureless regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        backP.Run(src)
        dst2 = backP.dst2
        dst3 = backP.dst3
        labels = backP.labels
    End Sub
End Class










Public Class FeatureLess_DCT : Inherits VB_Algorithm
    Dim dct As New DCT_FeatureLess
    Public Sub New()
        labels(3) = "Largest FeatureLess Region"
        desc = "Use DCT to find featureless regions."
    End Sub

    Public Sub RunVB(src As cv.Mat)
        dct.Run(src)
        dst2 = dct.dst2
        dst3 = dct.dst3

        Dim mask = dst2.Clone()
        Dim objectSize As New List(Of Integer)
        Dim regionCount = 1
        For y = 0 To mask.Rows - 1
            For x = 0 To mask.Cols - 1
                If mask.Get(Of Byte)(y, x) = 255 Then
                    Dim pt As New cv.Point(x, y)
                    Dim floodCount = mask.FloodFill(pt, regionCount)
                    objectSize.Add(floodCount)
                    regionCount += 1
                End If
            Next
        Next

        Dim maxSize As Integer, maxIndex As Integer
        For i = 0 To objectSize.Count - 1
            If maxSize < objectSize.ElementAt(i) Then
                maxSize = objectSize.ElementAt(i)
                maxIndex = i
            End If
        Next

        Dim label = mask.InRange(maxIndex + 1, maxIndex + 1)
        Dim nonZ = label.CountNonZero()
        labels(3) = "Largest FeatureLess Region (" + CStr(nonZ) + " " + Format(nonZ / label.Total, "#0.0%") + " pixels)"
        dst3.SetTo(cv.Scalar.White, label)
    End Sub
End Class








Public Class FeatureLess_LeftRight : Inherits VB_Algorithm
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        labels = {"", "", "FeatureLess Left mask", "FeatureLess Right mask"}
        desc = "Find the featureless regions of the left and right images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fLess.Run(task.leftview)
        dst2 = fLess.dst2.Clone

        fLess.Run(task.rightview)
        dst3 = fLess.dst2
    End Sub
End Class







Public Class FeatureLess_Density : Inherits VB_Algorithm
    Dim edgeD As New EdgeDraw_Basics
    Dim density As New Density_Mask
    Dim flood As New Flood_PointList
    Public Sub New()
        desc = "Use density regions as input points to floodfill"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edgeD.Run(src)

        density.Run(edgeD.dst2)
        dst3 = density.dst3

        flood.pointList = New List(Of cv.Point)(density.pointList)
        flood.Run(src)
        dst2 = flood.dst2

        labels(2) = CStr(flood.pointList.Count) + " points found " + CStr(flood.redCells.Count) + " regions > " +
                    CStr(gOptions.minPixelsSlider.Value) + " pixels"
    End Sub
End Class










Public Class FeatureLess_Edge_CPP : Inherits VB_Algorithm
    Dim cpp As New CPP_Basics
    Public Sub New()
        cpp.updateFunction(algorithmList.functionNames.EdgeDraw_Basics)
        desc = "Floodfill the output of the Edge Drawing filter (C++)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cpp.Run(src)
        dst2 = cpp.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If standalone Then dst3 = cpp.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class









Public Class FeatureLess_MotionAccum : Inherits VB_Algorithm
    Public edges As New Edge_MotionAccum
    Public Sub New()
        gOptions.GridSize.Value = 10
        labels(2) = "Featureless mask"
        desc = "Use Houghlines to find featureless regions in an image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edges.Run(src)

        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim regionCount As Integer
        For Each roi In task.gridList
            If edges.dst2(roi).CountNonZero = 0 Then
                regionCount += 1
                dst2(roi).SetTo(255)
            End If
        Next

        dst3.SetTo(0)
        src.CopyTo(dst3, dst2)
        labels(3) = "FeatureLess Regions = " + CStr(regionCount)
    End Sub
End Class







Public Class FeatureLess_History : Inherits VB_Algorithm
    Dim fLess As New FeatureLess_Basics
    Dim sum8u As New History_Sum8u
    Public Sub New()
        desc = "Accumulate the edges over a span of X images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fLess.Run(src)
        dst2 = fLess.dst2

        sum8u.Run(dst2)
        dst3 = sum8u.dst2
    End Sub
End Class








Public Class FeatureLess_RedCloud : Inherits VB_Algorithm
    Public rMin As New RedMin_Basics
    Dim fless As New FeatureLess_Basics
    Public Sub New()
        desc = "Floodfill the FeatureLess output so each cell can be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fless.Run(src)
        rMin.Run(fless.dst2)

        dst2 = rMin.dst2
        dst3 = rMin.dst3
        labels(2) = rMin.labels(2)
    End Sub
End Class