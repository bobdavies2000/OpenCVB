Imports cv = OpenCvSharp
Public Class TimeView_Basics
    Inherits VBparent
    Public sideView As Histogram_SideView2D
    Public topView As Histogram_TopView2D
    Public Sub New()
        initParent()

        sideView = New Histogram_SideView2D
        topView = New Histogram_TopView2D

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram Threshold Count", 0, 100, 4)
            sliders.setupTrackBar(1, "Number of frames to include", 2, 30, 10)
        End If
        dst1 = New cv.Mat(src.Size, cv.MatType.CV_32F, 0)
        dst2 = New cv.Mat(src.Size, cv.MatType.CV_32F, 0)
        task.desc = "TimeView that highlights concentrations of depth pixels"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static countSlider = findSlider("Histogram Threshold Count")

        Static sideFrames As New List(Of cv.Mat)
        Static topFrames As New List(Of cv.Mat)
        Static saveFrameCount As Integer
        Static frameSlider = findSlider("Number of frames to include")
        If saveFrameCount <> frameSlider.value Then
            saveFrameCount = frameSlider.value
            sideFrames.Clear()
            topFrames.Clear()
            dst1.SetTo(0)
            dst2.SetTo(0)
        End If

        sideView.src = src
        sideView.Run()

        sideFrames.Add(sideView.originalHistOutput.Threshold(countSlider.value, 255, cv.ThresholdTypes.Binary))

        topView.Run()
        topFrames.Add(topView.originalHistOutput.Threshold(countSlider.value, 255, cv.ThresholdTypes.Binary))

        dst1 = dst1.Add(sideFrames.ElementAt(sideFrames.Count - 1))
        dst2 = dst2.Add(topFrames.ElementAt(sideFrames.Count - 1))

        label1 = "Accum " + CStr(topFrames.Count) + " side frames with hist threshold > " + CStr(countSlider.value)
        label2 = "Accum " + CStr(topFrames.Count) + " top frames with hist threshold > " + CStr(countSlider.value)

        If sideFrames.Count >= saveFrameCount Then
            dst1 = dst1.Subtract(sideFrames.ElementAt(0))
            dst2 = dst2.Subtract(topFrames.ElementAt(0))
            sideFrames.RemoveAt(0)
            topFrames.RemoveAt(0)
        End If
    End Sub
End Class








Public Class TimeView_TopBackProjection
    Inherits VBparent
    Dim tFlood As TimeView_FloodFill
    Dim palette As Palette_Basics
    Public Sub New()
        initParent()
        tFlood = New TimeView_FloodFill
        palette = New Palette_Basics
        Dim hotRadio = findRadio("Hot")
        hotRadio.Checked = True
        task.desc = "Backproject the side and top views into the image view"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        tFlood.Run()
        dst2 = tFlood.dst2

        Dim rectlist = tFlood.floodTop.rects
        Dim split = tFlood.tBasics.sideView.gCloud.dst1.Split()
        If rectlist.Count > 0 Then
            Dim colorBump = CInt(255 / rectlist.Count)

            Static minSlider = findSlider("InRange Min Depth (mm)")
            Dim minVal = minSlider.value

            Dim colorMask = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            dst1 = src
            For i = 0 To rectlist.Count - 1
                Dim r = rectlist(i)
                If r.Width > 0 And r.Height > 0 Then
                    Dim minDepth = task.maxZ * r.X / dst2.Width
                    Dim maxDepth = task.maxZ * (r.X + r.Width) / dst2.Width

                    Dim minHeight = task.maxZ - task.maxZ * (r.Y + r.Height) / dst2.Height - task.sideFrustrumAdjust
                    Dim maxHeight = task.maxZ - task.maxZ * r.Y / dst2.Height - task.sideFrustrumAdjust

                    Dim mask32f = New cv.Mat

                    cv.Cv2.InRange(split(2), minDepth, maxDepth, mask32f)
                    Dim mask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)

                    cv.Cv2.InRange(split(1), minHeight, maxHeight, mask32f)
                    Dim hMask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)
                    cv.Cv2.BitwiseAnd(mask, hMask, mask)

                    colorMask.SetTo((i * colorBump) Mod 255, mask)
                End If
            Next
            palette.src = colorMask
            palette.Run()
            dst1 = palette.dst1
        Else
            task.trueText("No objects found")
        End If
    End Sub
End Class







Public Class TimeView_FloodFill
    Inherits VBparent
    Public floodSide As FloodFill_Basics
    Public floodTop As FloodFill_Basics
    Public tBasics As TimeView_Basics
    Public Sub New()
        initParent()

        floodSide = New FloodFill_Basics
        floodTop = New FloodFill_Basics
        Dim minFloodSlider = findSlider("FloodFill Minimum Size")
        minFloodSlider.Value = 10
        tBasics = New TimeView_Basics
        task.desc = "FloodFill the histograms of side and top views - TimeView_Basics"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        tBasics.src = src
        tBasics.Run()

        floodSide.src = tBasics.dst1.ConvertScaleAbs(255)
        floodSide.Run()
        dst1 = floodSide.dst1
        label1 = "SideView " + floodSide.label1

        floodTop.src = tBasics.dst2.ConvertScaleAbs(255)
        floodTop.Run()
        dst2 = floodTop.dst1
        label2 = "TopView " + floodTop.label1
    End Sub
End Class








Public Class TimeView_Centroids
    Inherits VBparent
    Public knn As KNN_BasicsQT
    Dim tflood As TimeView_FloodFill
    Public queryPoints As New List(Of cv.Point2f)
    Public responses As New List(Of cv.Point2f)
    Public Sub New()
        initParent()
        tflood = New TimeView_FloodFill
        knn = New KNN_BasicsQT

        label1 = "Top view with centroids in yellow"
        label2 = "Side view with centroids in yellow"
        task.desc = "Use KNN to track the query points"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        tflood.Run()
        dst1 = tflood.dst2
        dst2 = tflood.dst1

        For i = 0 To tflood.floodTop.centroids.Count - 1
            dst1.Circle(tflood.floodTop.centroids(i), task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next
        For i = 0 To tflood.floodSide.centroids.Count - 1
            dst2.Circle(tflood.floodSide.centroids(i), task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next

        Dim saveTopQueries = New List(Of cv.Point2f)(tflood.floodTop.centroids)
        If saveTopQueries.Count > 0 Then
            knn.knnQT.trainingPoints = saveTopQueries
            knn.knnQT.queryPoints = New List(Of cv.Point2f)(tflood.floodTop.centroids)
            knn.Run()
            For i = 0 To knn.neighbors.Rows - 1
                Dim qPoint = tflood.floodTop.centroids(i)
                cv.Cv2.Circle(dst1, qPoint, 3, cv.Scalar.Red, -1, task.lineType, 0)
                Dim pt = saveTopQueries(knn.neighbors.Get(Of Single)(i, 0))
                Dim cpt = New cv.Point(CInt(pt.X), CInt(pt.Y))
                dst1.Line(cpt, qPoint, cv.Scalar.Red, 1, task.lineType)
            Next

            saveTopQueries = New List(Of cv.Point2f)(tflood.floodTop.centroids)
        End If
    End Sub
End Class








Public Class TimeView_Rectangles
    Inherits VBparent
    Dim mOverLap As Rectangle_MultiOverlap
    Public tflood As TimeView_FloodFill
    Public Sub New()
        initParent()

        mOverLap = New Rectangle_MultiOverlap
        tflood = New TimeView_FloodFill

        label1 = "Top view with rectangles in yellow"
        label2 = "Side view with rectangles in yellow"
        task.desc = "Use KNN to track the query points"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        tflood.Run()
        dst1 = tflood.dst2
        dst2 = tflood.dst1

        mOverLap.inputRects = New List(Of cv.Rect)(tflood.floodTop.rects)
        mOverLap.Run()
        For i = 0 To mOverLap.outputRects.Count - 1
            dst1.Rectangle(mOverLap.outputRects(i), cv.Scalar.Yellow, 1)
        Next

        mOverLap.inputRects = New List(Of cv.Rect)(tflood.floodSide.rects)
        mOverLap.Run()
        For i = 0 To mOverLap.outputRects.Count - 1
            dst2.Rectangle(mOverLap.outputRects(i), cv.Scalar.Yellow, 1)
        Next
    End Sub
End Class










Public Class TimeView_Frustrum
    Inherits VBparent
    Dim tView As TimeView_Rectangles
    Dim cmatSide As PointCloud_ColorizeSide
    Dim cmatTop As PointCloud_ColorizeTop
    Dim mats As Mat_4Click
    Public Sub New()
        initParent()
        mats = New Mat_4Click
        cmatSide = New PointCloud_ColorizeSide
        cmatTop = New PointCloud_ColorizeTop
        tView = New TimeView_Rectangles
        label2 = "Click a quadrant in dst1 to show it in dst2 "
        task.desc = "Colorize the back and side views"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        tView.Run()
        mats.mat(0) = tView.dst1.Clone
        mats.mat(1) = tView.dst2.Clone

        cmatTop.src = tView.dst1
        cmatTop.Run()
        mats.mat(2) = cmatTop.dst1

        cmatSide.src = tView.dst2
        cmatSide.Run()
        mats.mat(3) = cmatSide.dst1

        mats.Run()
        dst1 = mats.dst1
        dst2 = mats.dst2
    End Sub
End Class