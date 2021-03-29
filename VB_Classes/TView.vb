Imports cv = OpenCvSharp
Public Class TView_Basics
    Inherits VBparent
    Public sideView As Histogram_SideView2D
    Public topView As Histogram_TopView2D
    Public split() As cv.Mat
    Dim hist As Histogram_Basics
    Public Sub New()
        initParent()

        hist = New Histogram_Basics
        sideView = New Histogram_SideView2D
        topView = New Histogram_TopView2D

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Show counts > X", 0, 300, 10)
        End If
        task.desc = "Triple View that highlights concentrations of depth pixels"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static countSlider = findSlider("Show counts > X")

        sideView.Run()

        split = sideView.gCloud.dst1.Split()

        Dim sideOrig = sideView.originalHistOutput.CountNonZero()
        dst2 = sideView.originalHistOutput.Threshold(countSlider.value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)

        topView.Run()

        dst1 = topView.originalHistOutput.Threshold(countSlider.value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)

        label1 = "TopView showing all histogram entries > " + CStr(countSlider.value)
        label2 = "SideView showing all histogram entries > " + CStr(countSlider.value)
    End Sub
End Class











Public Class TView_FloodFill
    Inherits VBparent
    Public floodSide As FloodFill_Old
    Public floodTop As FloodFill_Old
    Public tBasics As TView_Basics
    Public Sub New()
        initParent()

        floodSide = New FloodFill_Old
        floodTop = New FloodFill_Old
        Dim minFloodSlider = findSlider("FloodFill Minimum Size")
        minFloodSlider.Value = 100
        tBasics = New TView_Basics

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Fuse X frames", 1, 50, 10)
        End If

        task.desc = "FloodFill the histograms of side and top views - TView_Basics"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        tBasics.Run()
        dst1 = tBasics.dst1.Clone
        dst2 = tBasics.dst2.Clone

        Static fuseSlider = findSlider("Fuse X frames")
        Static saveFuseCount = -1
        Static fuseSide As New List(Of cv.Mat)
        Static fuseTop As New List(Of cv.Mat)
        Dim fuseCount = fuseSlider.value
        If saveFuseCount <> fuseSlider.value Then
            fuseSide.Clear()
            fuseTop.Clear()
            saveFuseCount = fuseSlider.value
        End If
        If fuseSide.Count > fuseCount Then fuseSide.RemoveAt(0)
        If fuseTop.Count > fuseCount Then fuseTop.RemoveAt(0)
        For i = 0 To fuseSide.Count - 1
            cv.Cv2.Max(fuseSide(i), dst1, dst1)
            cv.Cv2.Max(fuseTop(i), dst2, dst2)
        Next
        fuseSide.Add(tBasics.dst1.Clone)
        fuseTop.Add(tBasics.dst2.Clone)

        floodTop.src = dst1
        floodTop.Run()
        dst1 = floodTop.dst1

        floodSide.src = dst2
        floodSide.Run()
        dst2 = floodSide.dst1
    End Sub
End Class








Public Class TView_Centroids
    Inherits VBparent
    Public knn As KNN_BasicsQT
    Dim tflood As TView_FloodFill
    Public queryPoints As New List(Of cv.Point2f)
    Public responses As New List(Of cv.Point2f)
    Public Sub New()
        initParent()
        tflood = New TView_FloodFill
        knn = New KNN_BasicsQT

        label1 = "Top view with centroids in yellow"
        label2 = "Side view with centroids in yellow"
        task.desc = "Use KNN to track the query points"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        tflood.Run()
        dst1 = tflood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = tflood.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        For i = 0 To tflood.floodTop.centroids.Count - 1
            dst1.Circle(tflood.floodTop.centroids(i), task.dotSize, cv.Scalar.Yellow, -1)
        Next
        For i = 0 To tflood.floodSide.centroids.Count - 1
            dst2.Circle(tflood.floodSide.centroids(i), task.dotSize, cv.Scalar.Yellow, -1)
        Next

        Dim saveTopQueries = New List(Of cv.Point2f)(tflood.floodTop.centroids)
        If saveTopQueries.Count > 0 Then
            knn.knnQT.trainingPoints = saveTopQueries
            knn.knnQT.queryPoints = New List(Of cv.Point2f)(tflood.floodTop.centroids)
            knn.Run()
            For i = 0 To knn.neighbors.Rows - 1
                Dim qPoint = tflood.floodTop.centroids(i)
                cv.Cv2.Circle(dst1, qPoint, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias, 0)
                Dim pt = saveTopQueries(knn.neighbors.Get(Of Single)(i, 0))
                Dim cpt = New cv.Point(CInt(pt.X), CInt(pt.Y))
                dst1.Line(cpt, qPoint, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
            Next

            saveTopQueries = New List(Of cv.Point2f)(tflood.floodTop.centroids)
        End If
    End Sub
End Class








Public Class TView_Rectangles
    Inherits VBparent
    Dim mOverLap As Rectangle_MultiOverlap
    Public tflood As TView_FloodFill
    Public Sub New()
        initParent()

        mOverLap = New Rectangle_MultiOverlap
        tflood = New TView_FloodFill

        label1 = "Top view with rectangles in yellow"
        label2 = "Side view with rectangles in yellow"
        task.desc = "Use KNN to track the query points"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        tflood.Run()
        dst1 = tflood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = tflood.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

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










Public Class TView_Colorized
    Inherits VBparent
    Dim tView As TView_Rectangles
    Dim cmatSide As PointCloud_ColorizeSide
    Dim cmatTop As PointCloud_ColorizeTop
    Dim mats As Mat_4Click
    Public Sub New()
        initParent()
        mats = New Mat_4Click
        cmatSide = New PointCloud_ColorizeSide
        cmatTop = New PointCloud_ColorizeTop
        tView = New TView_Rectangles
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








Public Class TView_BackProjectTop
    Inherits VBparent
    Dim tFlood As TView_FloodFill
    Dim palette As Palette_Basics
    Public Sub New()
        initParent()
        tFlood = New TView_FloodFill
        palette = New Palette_Basics
        task.desc = "Backproject the side and top views into the image view"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        tFlood.Run()
        dst2 = tFlood.dst1

        Dim rectlist = tFlood.floodTop.rects

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

                    cv.Cv2.InRange(tFlood.tBasics.split(2), minDepth, maxDepth, mask32f)
                    Dim mask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)

                    cv.Cv2.InRange(tFlood.tBasics.split(1), minHeight, maxHeight, mask32f)
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