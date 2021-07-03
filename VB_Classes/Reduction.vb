Imports cv = OpenCvSharp
Public Class Reduction_Basics : Inherits VBparent
    Public maskVal As Integer
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Reduction factor", 1, 4096, 64)
            sliders.setupTrackBar(1, "Bits to remove in bitwise reduction", 0, 7, 3)
        End If

        If radio.Setup(caller, 3) Then
            radio.check(0).Text = "Use simple reduction"
            radio.check(1).Text = "Use bitwise reduction"
            radio.check(2).Text = "No reduction"
            radio.check(0).Checked = True
        End If

        task.desc = "Reduction: a simpler way to KMeans by reducing color resolution"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static bitwiseCheck = findRadio("Use bitwise reduction")
        Static simpleCheck = findRadio("Use simple reduction")
        Static reductionSlider = findSlider("Reduction factor")
        Static bitSlider = findSlider("Bits to remove in bitwise reduction")
        Dim reductionVal = CInt(reductionSlider.Value)
        bitSlider.enabled = bitwiseCheck.checked
        reductionSlider.enabled = simpleCheck.checked

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If bitwiseCheck.Checked Then
            Dim zeroBits = Math.Pow(2, bitSlider.value) - 1
            dst3 = New cv.Mat(src.Size, src.Type, cv.Scalar.All(255 - zeroBits))
            cv.Cv2.BitwiseAnd(src, dst3, dst2)
        ElseIf simpleCheck.Checked Then
            dst2 = src / reductionVal
            dst2 *= reductionVal
            If task.intermediateName = caller Then dst2.ConvertTo(dst2, cv.MatType.CV_32F)
            labels(2) = "Reduced image - factor = " + CStr(reductionVal)
        Else
            dst2 = src
            labels(2) = "No reduction requested"
        End If
        task.palette.RunClass(dst2.Clone)
        dst3 = task.palette.dst2
    End Sub
End Class





Public Class Reduction_Floodfill : Inherits VBparent
    Public flood As New FloodFill_Basics
    Public reduction As New Reduction_Basics
    Public Sub New()
        task.desc = "Use the reduction KMeans with floodfill to get masks and centroids of large masses."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        reduction.RunClass(src)
        dst2 = reduction.dst2
        flood.RunClass(reduction.dst2)
        dst3 = flood.dst2
        labels(2) = flood.labels(3)
    End Sub
End Class






Public Class Reduction_KNN_Color : Inherits VBparent
    Public pTrack As New KNN_PointTracker
    Public reduction As New Reduction_Floodfill
    Dim highlight As New Highlight_Basics
    Public Sub New()
        labels(3) = "Original floodfill color selections"
        task.desc = "Use KNN with color reduction to consistently identify regions and color them."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static minSizeSlider = findSlider("FloodFill Minimum Size")
        reduction.RunClass(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst3 = reduction.dst2

        pTrack.queryPoints = New List(Of cv.Point2f)(reduction.flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(reduction.flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(reduction.flood.masks)
        pTrack.RunClass(src)
        dst2 = pTrack.dst2

        If standalone Then
            highlight.viewObjects = pTrack.drawRC.viewObjects
            highlight.RunClass(dst2)
            dst2 = highlight.dst2
        End If

        labels(2) = "There were " + CStr(pTrack.drawRC.viewObjects.Count) + " regions > " + CStr(minSizeSlider.value) + " pixels"
    End Sub
End Class







Public Class Reduction_KNN_ColorAndDepth : Inherits VBparent
    Dim reduction As New Reduction_KNN_Color
    Dim depth As New Depth_EdgesLaplacian
    Public Sub New()
        labels(2) = "Detecting objects using only color coherence"
        labels(3) = "Detecting objects with color and depth coherence"
        task.desc = "Reduction_KNN finds objects with depth.  This algorithm uses only color on the remaining objects."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        reduction.RunClass(src)
        dst2 = reduction.dst2

        depth.RunClass(src)
        dst3 = depth.dst2
    End Sub
End Class









Public Class Reduction_SideTopLines : Inherits VBparent
    Dim sideView As New Histogram_SideView2D
    Dim topView As New Histogram_TopView2D
    Public lDetect As New Line_Basics
    Public setupSide As New PointCloud_SetupSide
    Public setupTop As New PointCloud_SetupTop
    Dim reduction As New Reduction_PointCloud
    Public Sub New()
        labels(2) = "Gravity rotated Side View with detected lines"
        labels(3) = "Gravity rotated Top View width detected lines"
        task.desc = "Present both the top and side view to minimize pixel counts."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        reduction.RunClass(src)

        sideView.RunClass(src)
        lDetect.RunClass(sideView.dst2)

        setupSide.RunClass(lDetect.dst2)
        dst2 = setupSide.dst2

        topView.RunClass(src)
        lDetect.RunClass(topView.dst2)

        setupTop.RunClass(lDetect.dst2)
        dst3 = setupTop.dst2
    End Sub
End Class






Public Class Reduction_PointCloud : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        reduction.radio.check(0).Checked = True
        labels(2) = "Reduced depth"
        labels(3) = "Pointcloud with reduced z-Depth"
        task.desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud.Clone
        Dim split() = src.Split()

        split(2) *= 1000
        split(2).ConvertTo(src, cv.MatType.CV_32S)
        reduction.RunClass(src)
        reduction.dst2.ConvertTo(dst2, cv.MatType.CV_32F)
        split(2) = dst2 * 0.001
        cv.Cv2.Merge(split, dst3)
    End Sub
End Class






Public Class Reduction_XYZ : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        If check.Setup(caller, 3) Then
            check.Box(0).Text = "Slice point cloud in X direction"
            check.Box(1).Text = "Slice point cloud in Y direction"
            check.Box(2).Text = "Slice point cloud in Z direction"
            check.Box(2).Checked = True
        End If

        task.desc = "Use reduction to slice the point cloud in 3 dimensions"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        Dim split() = src.Split()
        For i = 0 To split.Length - 1
            If check.Box(i).Checked Then
                split(i) += 10
                split(i) *= 1000
                split(i).ConvertTo(src, cv.MatType.CV_32S)
                reduction.RunClass(src)
                reduction.dst2.ConvertTo(split(i), cv.MatType.CV_32F)
                split(i) *= 0.001
                split(i) -= 10
            End If
        Next

        cv.Cv2.Merge(split, dst3)
        setTrueText("Task.PointCloud has been reduced and is in dst3")
    End Sub
End Class









Public Class Reduction_Edges : Inherits VBparent
    Dim edges As New Edges_Laplacian
    Dim reduction As New Reduction_Basics
    Public Sub New()
        reduction.radio.check(0).Checked = True
        task.desc = "Get the edges after reducing the image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        reduction.RunClass(src)
        dst2 = reduction.dst2.Clone

        Dim reductionRequested = False
        If reduction.radio.check(0).Checked Or reduction.radio.check(1).Checked Then reductionRequested = True
        labels(2) = If(reductionRequested, "Reduced image", "Original image")
        labels(3) = If(reductionRequested, "Laplacian edges of reduced image", "Laplacian edges of original image")
        edges.RunClass(dst2)
        dst3 = edges.dst2
    End Sub
End Class








Public Class Reduction_Depth : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Dim colorizer As New Depth_Colorizer_CPP
    Public reducedDepth32F As New cv.Mat
    Public Sub New()
        reduction.radio.check(0).Checked = True
        task.desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32S Then
            src = task.depth32f
            src.ConvertTo(src, cv.MatType.CV_32S)
        End If
        reduction.RunClass(src)
        reduction.dst2.ConvertTo(reducedDepth32F, cv.MatType.CV_32F)
        colorizer.RunClass(reducedDepth32F)
        dst2 = colorizer.dst2
        labels(2) = reduction.labels(2)
    End Sub
End Class









Public Class Reduction_DepthMax : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Dim colorizer As New Depth_Colorizer_CPP
    Dim dMax As New Depth_SmoothMax
    Public reducedDepth32F As New cv.Mat
    Public Sub New()
        reduction.radio.check(0).Checked = True
        task.desc = "Use reduction to isolate depth in 1 meter increments"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f
        dMax.RunClass(src)
        dst2 = dMax.dst3

        dst2.ConvertTo(src, cv.MatType.CV_32S)
        reduction.RunClass(src)
        reduction.dst2.ConvertTo(reducedDepth32F, cv.MatType.CV_32F)

        colorizer.RunClass(reducedDepth32F)
        dst3 = colorizer.dst2
    End Sub
End Class
