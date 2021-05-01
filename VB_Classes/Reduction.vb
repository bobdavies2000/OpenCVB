Imports cv = OpenCvSharp
Public Class Reduction_Basics : Inherits VBparent
    Public maskVal As Integer
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Reduction factor", 1, 4096, 64)
            sliders.setupTrackBar(1, "Bits to remove in bitwise reduction", 0, 7, 3)
        End If

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
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
            dst2 = New cv.Mat(src.Size, src.Type, cv.Scalar.All(255 - zeroBits))
            cv.Cv2.BitwiseAnd(src, dst2, dst1)
        ElseIf simpleCheck.Checked Then
            dst1 = src / reductionVal
            dst1 *= reductionVal
            label1 = "Reduced image - factor = " + CStr(reductionVal)
        Else
            dst1 = src
            label1 = "No reduction requested"
        End If
        task.palette.Run(dst1)
        dst2 = task.palette.dst1
    End Sub
End Class





Public Class Reduction_Floodfill : Inherits VBparent
    Public flood As New FloodFill_Basics
    Public reduction As New Reduction_Basics
    Public Sub New()
        task.desc = "Use the reduction KMeans with floodfill to get masks and centroids of large masses."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        reduction.Run(src)
        flood.Run(reduction.dst1)

        dst1 = flood.dst1
        label1 = flood.label2
    End Sub
End Class






Public Class Reduction_KNN_Color : Inherits VBparent
    Public pTrack As New KNN_PointTracker
    Public reduction As New Reduction_Floodfill
    Dim highlight As New Highlight_Basics
    Public Sub New()
        label2 = "Original floodfill color selections"
        task.desc = "Use KNN with color reduction to consistently identify regions and color them."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static minSizeSlider = findSlider("FloodFill Minimum Size")
        reduction.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = reduction.dst1

        pTrack.queryPoints = New List(Of cv.Point2f)(reduction.flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(reduction.flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(reduction.flood.masks)
        pTrack.Run(src)
        dst1 = pTrack.dst1

        If standalone Or task.intermediateReview = caller Then
            highlight.viewObjects = pTrack.drawRC.viewObjects
            highlight.Run(dst1)
            dst1 = highlight.dst1
        End If

        label1 = "There were " + CStr(pTrack.drawRC.viewObjects.Count) + " regions > " + CStr(minSizeSlider.value) + " pixels"
    End Sub
End Class







Public Class Reduction_KNN_ColorAndDepth : Inherits VBparent
    Dim reduction As New Reduction_KNN_Color
    Dim depth As New Depth_Edges
    Public Sub New()
        label1 = "Detecting objects using only color coherence"
        label2 = "Detecting objects with color and depth coherence"
        task.desc = "Reduction_KNN finds objects with depth.  This algorithm uses only color on the remaining objects."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        reduction.Run(src)
        dst1 = reduction.dst1

        depth.Run(src)
        dst2 = depth.dst1
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
        label1 = "Gravity rotated Side View with detected lines"
        label2 = "Gravity rotated Top View width detected lines"
        task.desc = "Present both the top and side view to minimize pixel counts."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        reduction.Run(src)

        sideView.Run(src)
        lDetect.Run(sideView.dst1)

        setupSide.Run(lDetect.dst1)
        dst1 = setupSide.dst1

        topView.Run(src)
        lDetect.Run(topView.dst1)

        setupTop.Run(lDetect.dst1)
        dst2 = setupTop.dst1
    End Sub
End Class






Public Class Reduction_PointCloud : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        reduction.radio.check(0).Checked = True
        label1 = "Reduced depth"
        label2 = "Pointcloud with reduced z-Depth"
        task.desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud.Clone
        Dim split() = src.Split()

        split(2) *= 1000
        split(2).ConvertTo(src, cv.MatType.CV_32S)
        reduction.Run(src)
        reduction.dst1.ConvertTo(dst1, cv.MatType.CV_32F)
        split(2) = dst1 * 0.001
        cv.Cv2.Merge(split, dst2)
    End Sub
End Class






Public Class Reduction_XYZ : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 3)
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

        For i = 0 To 3 - 1
            If check.Box(i).Checked Then
                split(i) += 10
                split(i) *= 1000
                split(i).ConvertTo(src, cv.MatType.CV_32S)
                reduction.Run(src)
                reduction.dst1.ConvertTo(split(i), cv.MatType.CV_32F)
                split(i) *= 0.001
                split(i) -= 10
            End If
        Next

        cv.Cv2.Merge(split, dst2)
        task.trueText("Task.PointCloud has been reduced and is in dst2")
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
        reduction.Run(src)
        dst1 = reduction.dst1.Clone

        Dim reductionRequested = False
        If reduction.radio.check(0).Checked Or reduction.radio.check(1).Checked Then reductionRequested = True
        label1 = If(reductionRequested, "Reduced image", "Original image")
        label2 = If(reductionRequested, "Laplacian edges of reduced image", "Laplacian edges of original image")
        edges.Run(dst1)
        dst2 = edges.dst1
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
        reduction.Run(src)
        reduction.dst1.ConvertTo(reducedDepth32F, cv.MatType.CV_32F)
        colorizer.Run(reducedDepth32F)
        dst1 = colorizer.dst1
        label1 = reduction.label1
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
        dMax.Run(src)
        dst1 = dMax.dst2

        dst1.ConvertTo(src, cv.MatType.CV_32S)
        reduction.Run(src)
        reduction.dst1.ConvertTo(reducedDepth32F, cv.MatType.CV_32F)

        colorizer.Run(reducedDepth32F)
        dst2 = colorizer.dst1
    End Sub
End Class
