Imports cv = OpenCvSharp
Public Class Featureless_Basics : Inherits VBparent
    Public edges As New Edges_Basics
    Public grid As New Thread_Grid
    Public flood As New FloodFill_Palette
    Public Sub New()
        findSlider("ThreadGrid Width").Value = 10
        findSlider("ThreadGrid Height").Value = 10

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "FeatureLess rho", 1, 100, 1)
            sliders.setupTrackBar(1, "FeatureLess theta", 1, 1000, 1000 * Math.PI / 180)
            sliders.setupTrackBar(2, "FeatureLess threshold", 1, 100, 3)
        End If

        labels(2) = "Featureless mask"
        task.desc = "Multithread Houghlines to find featureless regions in an image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.Run(Nothing)

        edges.Run(src)

        Dim rhoIn = sliders.trackbar(0).Value
        Dim thetaIn = sliders.trackbar(1).Value / 1000
        Dim threshold = sliders.trackbar(2).Value

        src.CopyTo(dst2)
        Dim mask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
        Parallel.ForEach(grid.roiList,
        Sub(roi)
            Dim segments() = cv.Cv2.HoughLines(edges.dst2(roi), rhoIn, thetaIn, threshold)
            If segments.Count = 0 Then mask(roi).SetTo(255)
        End Sub)

        flood.Run(mask)
        dst2 = flood.dst2

        Dim allRegions As New cv.Mat
        cv.Cv2.BitwiseNot(flood.basics.alreadyFlooded, allRegions)
        dst3.SetTo(0)
        src.CopyTo(dst3, allRegions)
        Static floodSlider = findSlider("FloodFill Minimum Size")
        labels(3) = "FeatureLess Regions = " + CStr(flood.basics.centroids.Count) + " with more than " + CStr(floodSlider.value) + " pixels"
    End Sub
End Class





Public Class Featureless_DCT_MT : Inherits VBparent
    Dim dct As New DCT_FeatureLess
    Public Sub New()
        labels(3) = "Largest FeatureLess Region"
        task.desc = "Use DCT to find featureless regions."
    End Sub

    Public Sub Run(src As cv.Mat) ' Rank = 1
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






Public Class FeatureLess_Prediction : Inherits VBparent
    Dim fLess As New Featureless_Basics
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "FeatureLess Resize Percent", 1, 100, 1)
        End If
        task.desc = "Identify the featureless regions, use color and depth to learn the featureless label, and predict depth over the image. - needs more work"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        fLess.Run(src)
        dst2 = fLess.dst2
        dst3 = fLess.dst3
        Dim labels = fLess.dst3.Clone()

        Dim percent = Math.Sqrt(sliders.trackbar(0).Value / 100)
        Dim newSize = New cv.Size(src.Width * percent, src.Height * percent)

        Dim rgb = src.Clone()
        Dim depth32f As cv.Mat = task.depth32f.Resize(newSize)
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
        planes(3) = task.depth32f.Resize(newSize)
        cv.Cv2.Merge(planes, learnInput)

        Dim rtree = cv.ML.RTrees.Create()
        learnInput = learnInput.Reshape(1, learnInput.Rows * learnInput.Cols)
        response = response.Reshape(1, response.Rows * response.Cols)
        rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, response)

        cv.Cv2.BitwiseNot(mask, mask)
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





Public Class FeatureLess_PointTracker : Inherits VBparent
    Public fLess As New Featureless_Basics
    Public pTrack As New KNN_PointTracker
    Public Sub New()
        labels(2) = "After point tracker"
        labels(3) = "Before point tracker"
        task.desc = "Track the featureless regions with point tracker"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        fLess.Run(src)
        dst3 = fLess.dst2

        pTrack.queryPoints = fLess.flood.basics.centroids
        pTrack.queryRects = fLess.flood.basics.rects
        pTrack.queryMasks = fLess.flood.basics.masks
        pTrack.Run(src)
        dst2 = pTrack.dst2
    End Sub
End Class