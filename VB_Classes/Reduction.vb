Imports cv = OpenCvSharp
Public Class Reduction_Basics
    Inherits VBparent
    Public maskVal As Integer
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Reduction factor", 0, 4096, 64)
            sliders.setupTrackBar(1, "Bits to remove in bitwise reduction", 0, 7, 3)
        End If

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Use bitwise reduction"
            radio.check(1).Text = "Use simple reduction"
            radio.check(2).Text = "No reduction"
            radio.check(1).Checked = True
        End If

        task.desc = "Reduction: a simpler way to KMeans by reducing color resolution"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static reductionSlider = findSlider("Reduction factor")
        Dim reductionVal = CInt(reductionSlider.Value)
        Static bitwiseCheck = findRadio("Use bitwise reduction")
        Static simpleCheck = findRadio("Use simple reduction")
        If bitwiseCheck.Checked Then
            Static bitSlider = findSlider("Bits to remove in bitwise reduction")
            Dim zeroBits = Math.Pow(2, bitSlider.value) - 1
            Dim tmp = New cv.Mat(src.Size, src.Type, cv.Scalar.All(255 - zeroBits))
            cv.Cv2.BitwiseAnd(src, tmp, dst1)
        ElseIf simpleCheck.Checked Then
            If reductionVal = 0 Then reductionVal = 1
            dst1 = src / reductionVal
            dst1 *= reductionVal
            label1 = "Reduced image - factor = " + CStr(reductionVal)
        Else
            dst1 = src
            label1 = "No reduction requested"
        End If
    End Sub
End Class





Public Class Reduction_Floodfill
    Inherits VBparent
    Public flood As FloodFill_Basics
    Public reduction As Reduction_Basics
    Public Sub New()
        initParent()
        flood = New FloodFill_Basics()
        reduction = New Reduction_Basics()
        task.desc = "Use the reduction KMeans with floodfill to get masks and centroids of large masses."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        reduction.src = src
        reduction.Run()

        flood.src = reduction.dst1
        flood.Run()

        dst1 = flood.dst1
        label1 = flood.label2
    End Sub
End Class






Public Class Reduction_KNN_Color
    Inherits VBparent
    Public reduction As Reduction_Floodfill
    Public pTrack As KNN_PointTracker
    Dim highlight As Highlight_Basics
    Public Sub New()
        initParent()

        pTrack = New KNN_PointTracker()
        reduction = New Reduction_Floodfill()
        If standalone Then highlight = New Highlight_Basics()

        label2 = "Original floodfill color selections"
        task.desc = "Use KNN with color reduction to consistently identify regions and color them."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        reduction.src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        reduction.Run()
        dst2 = reduction.dst1

        pTrack.queryPoints = New List(Of cv.Point2f)(reduction.flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(reduction.flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(reduction.flood.masks)
        pTrack.Run()
        dst1 = pTrack.dst1

        If standalone Or task.intermediateReview = caller Then
            highlight.viewObjects = pTrack.drawRC.viewObjects
            highlight.src = dst1
            highlight.Run()
            dst1 = highlight.dst1
        End If

        Static minSizeSlider = findSlider("FloodFill Minimum Size")
        label1 = "There were " + CStr(pTrack.drawRC.viewObjects.Count) + " regions > " + CStr(minSizeSlider.value) + " pixels"
    End Sub
End Class







Public Class Reduction_KNN_ColorAndDepth
    Inherits VBparent
    Dim reduction As Reduction_KNN_Color
    Dim depth As Depth_Edges
    Public Sub New()
        initParent()
        depth = New Depth_Edges()
        reduction = New Reduction_KNN_Color()
        label1 = "Detecting objects using only color coherence"
        label2 = "Detecting objects with color and depth coherence"
        task.desc = "Reduction_KNN finds objects with depth.  This algorithm uses only color on the remaining objects."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        reduction.src = src
        reduction.Run()
        dst1 = reduction.dst1

        depth.Run()
        dst2 = depth.dst1
    End Sub
End Class









Public Class Reduction_Lines
    Inherits VBparent
    Dim sideView As Histogram_SideView2D
    Dim topView As Histogram_TopView2D
    Public lDetect As Line_Basics
    Public cmatSide As PointCloud_ColorizeSide
    Public cmatTop As PointCloud_ColorizeTop
    Dim reduction As Reduction_PointCloud
    Public Sub New()
        initParent()

        cmatSide = New PointCloud_ColorizeSide
        cmatTop = New PointCloud_ColorizeTop
        sideView = New Histogram_SideView2D()
        topView = New Histogram_TopView2D()
        reduction = New Reduction_PointCloud

        lDetect = New Line_Basics()

        label1 = "Gravity rotated Side View with detected lines"
        label2 = "Gravity rotated Top View width detected lines"
        task.desc = "Present both the top and side view to minimize pixel counts."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        reduction.Run()

        sideView.Run()
        lDetect.src = sideView.dst1
        lDetect.Run()

        cmatSide.src = lDetect.dst1
        cmatSide.Run()
        dst1 = cmatSide.dst1

        topView.Run()
        lDetect.src = topView.dst1
        lDetect.Run()

        cmatTop.src = lDetect.dst1
        cmatTop.Run()
        dst2 = cmatTop.dst1
    End Sub
End Class







Public Class Reduction_Histogram
    Inherits VBparent
    Dim basics As Reduction_Basics
    Dim hist As Histogram_BackProjectionGrayscale
    Public Sub New()
        initParent()

        basics = New Reduction_Basics()
        hist = New Histogram_BackProjectionGrayscale()

        label2 = "Backprojection of highlighted histogram bin"
        task.desc = "Use the histogram of a reduced RGB image to isolate featureless portions of an image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        basics.src = src
        basics.Run()
        Static reductionSlider = findSlider("Reduction factor")
        reductionSlider.value = 112

        hist.src = basics.dst1
        hist.Run()
        dst1 = hist.dst1
        dst2 = hist.dst2
        label1 = "Reduction = " + CStr(reductionSlider.value) + " and bins = " + CStr(hist.binSlider.Value)
    End Sub
End Class





Public Class Reduction_PointCloud
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Public Sub New()
        initParent()
        reduction = New Reduction_Basics()
        reduction.radio.check(0).Checked = True
        label1 = "Reduced depth"
        label2 = "Pointcloud with reduced z-Depth"
        task.desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        Dim input = src
        If input.Type <> cv.MatType.CV_32FC3 Then input = task.pointCloud
        Dim split() = input.Split()

        split(2) *= 1000
        split(2).ConvertTo(reduction.src, cv.MatType.CV_32S)
        reduction.Run()
        reduction.dst1.ConvertTo(dst1, cv.MatType.CV_32F)
        split(2) = dst1 * 0.001
        cv.Cv2.Merge(split, dst2)
    End Sub
End Class






Public Class Reduction_XYZ
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Public Sub New()
        initParent()
        reduction = New Reduction_Basics()

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 3)
            check.Box(0).Text = "Slice point cloud in X direction"
            check.Box(1).Text = "Slice point cloud in Y direction"
            check.Box(2).Text = "Slice point cloud in Z direction"
            check.Box(2).Checked = True
        End If

        task.desc = "Use reduction to slice the point cloud in 3 dimensions"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        Dim input = src
        If input.Type <> cv.MatType.CV_32FC3 Then input = task.pointCloud
        Dim split() = input.Split()

        For i = 0 To 3 - 1
            If check.Box(i).Checked Then
                split(i) += 10
                split(i) *= 1000
                split(i).ConvertTo(reduction.src, cv.MatType.CV_32S)
                reduction.Run()
                reduction.dst1.ConvertTo(split(i), cv.MatType.CV_32F)
                split(i) *= 0.001
                split(i) -= 10
            End If
        Next

        cv.Cv2.Merge(split, dst2)
        task.trueText("Task.PointCloud has been reduced and is in dst2")
    End Sub
End Class









Public Class Reduction_Edges
    Inherits VBparent
    Dim edges As Edges_Laplacian
    Dim reduction As Reduction_Basics
    Public Sub New()
        initParent()

        edges = New Edges_Laplacian()
        reduction = New Reduction_Basics()
        reduction.radio.check(0).Checked = True

        task.desc = "Get the edges after reducing the image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        reduction.src = src
        reduction.Run()
        dst1 = reduction.dst1.Clone

        Dim reductionRequested = False
        If reduction.radio.check(0).Checked Or reduction.radio.check(1).Checked Then reductionRequested = True
        label1 = If(reductionRequested, "Reduced image", "Original image")
        label2 = If(reductionRequested, "Laplacian edges of reduced image", "Laplacian edges of original image")
        edges.src = dst1
        edges.Run()
        dst2 = edges.dst1
    End Sub
End Class








Public Class Reduction_Depth
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Dim colorizer As Depth_Colorizer_CPP
    Public reducedDepth32F As New cv.Mat
    Public Sub New()
        initParent()
        reduction = New Reduction_Basics()
        reduction.radio.check(0).Checked = True
        colorizer = New Depth_Colorizer_CPP()
        task.desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        If src.Type = cv.MatType.CV_32S Then
            reduction.src = src
        Else
            src = task.depth32f
            src.ConvertTo(reduction.src, cv.MatType.CV_32S)
        End If
        reduction.Run()
        reduction.dst1.ConvertTo(reducedDepth32F, cv.MatType.CV_32F)
        colorizer.src = reducedDepth32F
        colorizer.Run()
        dst1 = colorizer.dst1
        label1 = reduction.label1
    End Sub
End Class









Public Class Reduction_DepthMax
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Dim colorizer As Depth_Colorizer_CPP
    Dim dMax As Depth_SmoothMax
    Public reducedDepth32F As New cv.Mat
    Public Sub New()
        initParent()
        reduction = New Reduction_Basics()
        reduction.radio.check(0).Checked = True
        colorizer = New Depth_Colorizer_CPP()
        dMax = New Depth_SmoothMax
        task.desc = "Use reduction to isolate depth in 1 meter increments"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        dMax.src = src
        If dMax.src.Type <> cv.MatType.CV_32F Then dMax.src = task.depth32f
        dMax.Run()
        dst1 = dMax.dst2

        dst1.ConvertTo(reduction.src, cv.MatType.CV_32S)
        reduction.Run()
        reduction.dst1.ConvertTo(reducedDepth32F, cv.MatType.CV_32F)

        colorizer.src = reducedDepth32F
        colorizer.Run()
        dst2 = colorizer.dst1
    End Sub
End Class