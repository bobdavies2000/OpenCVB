Imports cv = OpenCvSharp
Public Class TimeView_Basics : Inherits VBparent
    Public sideView As New Histogram_SideView2D
    Public topView As New Histogram_TopView2D
    Dim setupSide As New PointCloud_SetupSide
    Dim setupTop As New PointCloud_SetupTop
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Number of frames to include", 2, 30, 10)
        End If
        dst1 = New cv.Mat(task.color.Size, cv.MatType.CV_32F, 0)
        dst2 = New cv.Mat(task.color.Size, cv.MatType.CV_32F, 0)
        task.desc = "TimeView that highlights concentrations of depth pixels"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static frameSlider = findSlider("Number of frames to include")
        Static sideAccum As New cv.Mat(src.Size, cv.MatType.CV_32FC1)
        Static topAccum As New cv.Mat(src.Size, cv.MatType.CV_32FC1)

        Static sideFrames As New List(Of cv.Mat)
        Static topFrames As New List(Of cv.Mat)
        Static saveFrameCount As Integer

        If saveFrameCount <> frameSlider.value Then
            saveFrameCount = frameSlider.value
            sideFrames.Clear()
            topFrames.Clear()
            sideAccum.SetTo(0)
            topAccum.SetTo(0)
        End If

        sideView.Run(src)

        sideFrames.Add(sideView.originalHistOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary))

        topView.Run(src)
        topFrames.Add(topView.originalHistOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary))

        sideAccum = sideAccum.Add(sideFrames.ElementAt(sideFrames.Count - 1))
        topAccum = topAccum.Add(topFrames.ElementAt(sideFrames.Count - 1))
        If standalone Or task.intermediateReview = caller Then
            setupSide.Run(sideAccum.ConvertScaleAbs(255).CvtColor(cv.ColorConversionCodes.GRAY2BGR))
            dst1 = setupSide.dst1
            setupTop.Run(topAccum.ConvertScaleAbs(255).CvtColor(cv.ColorConversionCodes.GRAY2BGR))
            dst2 = setupTop.dst1
        Else
            dst1 = sideAccum
            dst2 = topAccum
        End If

        label1 = "Accum " + CStr(topFrames.Count) + " side frames with hist threshold > " + CStr(task.hist3DThreshold)
        label2 = "Accum " + CStr(topFrames.Count) + " top frames with hist threshold > " + CStr(task.hist3DThreshold)

        If sideFrames.Count >= saveFrameCount Then
            sideAccum = sideAccum.Subtract(sideFrames.ElementAt(0))
            topAccum = topAccum.Subtract(topFrames.ElementAt(0))
            sideFrames.RemoveAt(0)
            topFrames.RemoveAt(0)
        End If
    End Sub
End Class








Public Class TimeView_TopBackProjection : Inherits VBparent
    Dim tFlood As New TimeView_FloodFill
    Public Sub New()
        task.desc = "Backproject the side and top views into the image view"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        tFlood.Run(src)
        dst2 = tFlood.dst2

        Dim rectlist = tFlood.floodTop.rects
        Dim split = tFlood.tBasics.sideView.gCloud.dst1.Split()
        If rectlist.Count > 0 Then
            Dim colorBump = CInt(255 / rectlist.Count)

            Dim colorMask = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            dst1 = src
            For i = 0 To rectlist.Count - 1
                Dim r = rectlist(i)
                If r.Width > 0 And r.Height > 0 Then
                    Dim minDepth = task.maxZ * r.X / dst2.Width
                    Dim maxDepth = task.maxZ * (r.X + r.Width) / dst2.Width

                    Dim minHeight = task.maxZ - task.maxZ * (r.Y + r.Height) / dst2.Height - task.maxY
                    Dim maxHeight = task.maxZ - task.maxZ * r.Y / dst2.Height - task.maxY

                    Dim mask32f = New cv.Mat

                    cv.Cv2.InRange(split(2), minDepth, maxDepth, mask32f)
                    Dim mask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)

                    cv.Cv2.InRange(split(1), minHeight, maxHeight, mask32f)
                    Dim hMask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)
                    cv.Cv2.BitwiseAnd(mask, hMask, mask)

                    colorMask.SetTo((i * colorBump) Mod 255, mask)
                End If
            Next
            task.palette.Run(colorMask)
            dst1 = task.palette.dst1
        Else
            task.trueText("No objects found")
        End If
    End Sub
End Class







Public Class TimeView_FloodFill : Inherits VBparent
    Public floodSide As New FloodFill_Basics
    Public floodTop As New FloodFill_Basics
    Public tBasics As New TimeView_Basics
    Public Sub New()
        findSlider("FloodFill Minimum Size").Value = 10
        task.desc = "FloodFill the histograms of side and top views - TimeView_Basics"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        tBasics.Run(src)

        floodSide.Run(tBasics.dst1.ConvertScaleAbs(255))
        dst1 = floodSide.dst1
        label1 = "SideView " + floodSide.label1

        floodTop.Run(tBasics.dst2.ConvertScaleAbs(255))
        dst2 = floodTop.dst1
        label2 = "TopView " + floodTop.label1
    End Sub
End Class








Public Class TimeView_Centroids : Inherits VBparent
    Public knn As New KNN_BasicsQT
    Dim tflood As New TimeView_FloodFill
    Public queryPoints As New List(Of cv.Point2f)
    Public responses As New List(Of cv.Point2f)
    Public Sub New()
        label1 = "Top view with centroids in yellow"
        label2 = "Side view with centroids in yellow"
        findSlider("Amount to rotate pointcloud around Y-axis (degrees)").Value = 1
        task.desc = "Use KNN to track the query points"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        tflood.Run(src)
        dst1 = tflood.dst2
        dst2 = tflood.dst1

        For i = 0 To tflood.floodTop.centroids.Count - 1
            dst1.Circle(tflood.floodTop.centroids(i), task.dotSize + 3, cv.Scalar.Yellow, -1, task.lineType)
        Next
        For i = 0 To tflood.floodSide.centroids.Count - 1
            dst2.Circle(tflood.floodSide.centroids(i), task.dotSize + 3, cv.Scalar.Yellow, -1, task.lineType)
        Next

        Dim saveTopQueries = New List(Of cv.Point2f)(tflood.floodTop.centroids)
        If saveTopQueries.Count > 0 Then
            knn.knnQT.trainingPoints = saveTopQueries
            knn.knnQT.queryPoints = New List(Of cv.Point2f)(tflood.floodTop.centroids)
            knn.Run(src)
            For i = 0 To knn.neighbors.Rows - 1
                Dim qPoint = tflood.floodTop.centroids(i)
                dst1.Circle(qPoint, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType, 0)
                Dim pt = saveTopQueries(knn.neighbors.Get(Of Single)(i, 0))
                If Single.IsNaN(pt.X) = False And Single.IsNaN(pt.Y) = False Then
                    Dim cpt = New cv.Point(CInt(pt.X), CInt(pt.Y))
                    dst1.Line(cpt, qPoint, cv.Scalar.Red, 1, task.lineType)
                End If
            Next

            saveTopQueries = New List(Of cv.Point2f)(tflood.floodTop.centroids)
        End If
    End Sub
End Class








Public Class TimeView_Rectangles : Inherits VBparent
    Dim mOverLap As New Rectangle_MultiOverlap
    Public tflood As New TimeView_FloodFill
    Public Sub New()
        label1 = "Top view with rectangles in yellow"
        label2 = "Side view with rectangles in yellow"
        task.desc = "Use KNN to track the query points"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        tflood.Run(src)
        dst1 = tflood.dst2
        dst2 = tflood.dst1

        mOverLap.inputRects = New List(Of cv.Rect)(tflood.floodTop.rects)
        mOverLap.Run(src)
        For i = 0 To mOverLap.outputRects.Count - 1
            dst1.Rectangle(mOverLap.outputRects(i), cv.Scalar.Yellow, 1)
        Next

        mOverLap.inputRects = New List(Of cv.Rect)(tflood.floodSide.rects)
        mOverLap.Run(src)
        For i = 0 To mOverLap.outputRects.Count - 1
            dst2.Rectangle(mOverLap.outputRects(i), cv.Scalar.Yellow, 1)
        Next
    End Sub
End Class










Public Class TimeView_Frustrum : Inherits VBparent
    Dim tView As New TimeView_Rectangles
    Dim setupSide As New PointCloud_SetupSide
    Dim setupTop As New PointCloud_SetupTop
    Dim mats As New Mat_4Click
    Public Sub New()
        label2 = "Click a quadrant in dst1 to show it in dst2 "
        task.desc = "Colorize the back and side views"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        tView.Run(src)
        mats.mat(0) = tView.dst1.Clone
        mats.mat(1) = tView.dst2.Clone

        setupTop.Run(tView.dst1)
        mats.mat(2) = setupTop.dst1

        setupSide.Run(tView.dst2)
        mats.mat(3) = setupSide.dst1

        mats.Run(Nothing)
        dst1 = mats.dst1
        dst2 = mats.dst2
    End Sub
End Class
