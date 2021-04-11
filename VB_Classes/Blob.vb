Imports cv = OpenCvSharp
Public Class Blob_Basics
    Inherits VBparent
    Dim blob As Blob_Input
    Dim blobDetector As New CS_Classes.Blob_Basics
    Public Sub New()
        initParent()
        blobDetector = New CS_Classes.Blob_Basics
        blob = New Blob_Input()
        blob.updateFrequency = 1 ' it is pretty fast but sloppy...
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 5)
            check.Box(0).Text = "FilterByArea"
            check.Box(1).Text = "FilterByCircularity"
            check.Box(2).Text = "FilterByConvexity"
            check.Box(3).Text = "FilterByInertia"
            check.Box(4).Text = "FilterByColor"
            check.Box(4).Checked = True ' filter by color...
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "min Threshold", 0, 255, 100)
            sliders.setupTrackBar(1, "max Threshold", 0, 255, 255)
            sliders.setupTrackBar(2, "Threshold Step", 1, 50, 5)
        End If
        task.desc = "Test C# Blob Detector."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim blobParams = New cv.SimpleBlobDetector.Params
        blobParams.FilterByArea = check.Box(0).Checked
        blobParams.FilterByCircularity = check.Box(1).Checked
        blobParams.FilterByConvexity = check.Box(2).Checked
        blobParams.FilterByInertia = check.Box(3).Checked
        blobParams.FilterByColor = check.Box(4).Checked

        blobParams.MaxArea = 100
        blobParams.MinArea = 0.001

        blobParams.MinThreshold = sliders.trackbar(0).Value
        blobParams.MaxThreshold = sliders.trackbar(1).Value
        blobParams.ThresholdStep = sliders.trackbar(2).Value

        blobParams.MinDistBetweenBlobs = 10
        blobParams.MinRepeatability = 1

        blob.src = src
        blob.Run()
        dst1 = blob.dst1
        dst2 = dst1.EmptyClone

        ' The create method in SimpleBlobDetector is not available in VB.Net.  Not sure why.  To get around this, just use C# where create method works fine.
        blobDetector.Run(dst1, dst2, blobParams)
    End Sub
End Class







Public Class Blob_Input
    Inherits VBparent
    Dim rectangles As Rectangle_Rotated
    Dim circles As Draw_Circles
    Dim ellipses As Draw_Ellipses
    Dim poly As Draw_Polygon
    Public Mats As Mat_4to1
    Public updateFrequency = 30
    Public Sub New()
        initParent()
        rectangles = New Rectangle_Rotated()
        circles = New Draw_Circles()
        ellipses = New Draw_Ellipses()
        poly = New Draw_Polygon()

        Dim countSlider = findSlider("Rectangle Count")
        countSlider.Value = 5

        circles.sliders.trackbar(0).Value = 5
        ellipses.sliders.trackbar(0).Value = 5
        poly.sliders.trackbar(0).Value = 5

        rectangles.rect.updateFrequency = 1
        circles.updateFrequency = 1
        ellipses.updateFrequency = 1

        poly.radio.check(1).Checked = True ' we want the convex polygon filled.

        Mats = New Mat_4to1()

        label1 = "Click any quadrant below to view it on the right"
        label2 = "Click any quadrant at left to view it below"
        task.desc = "Generate data to test Blob Detector."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        rectangles.src = src
        rectangles.Run()
        Mats.mat(0) = rectangles.dst1

        circles.src = src
        circles.Run()
        Mats.mat(1) = circles.dst1

        ellipses.src = src
        ellipses.Run()
        Mats.mat(2) = ellipses.dst1

        poly.src = src
        poly.Run()
        Mats.mat(3) = poly.dst2
        Mats.Run()
        Mats.dst1.CopyTo(dst1)
        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setMyActiveMat()
        dst2 = Mats.mat(quadrantIndex)
    End Sub
End Class




Public Class Blob_RenderBlobs
    Inherits VBparent
    Dim blob As Blob_Input
    Public Sub New()
        initParent()
        blob = New Blob_Input()
        blob.updateFrequency = 1

        task.desc = "Use connected components to find blobs."
        label1 = "Input blobs"
        label2 = "Showing only the largest blob in test data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        If task.frameCount Mod 100 = 0 Then
            blob.src = src
            blob.Run()
            dst1 = blob.dst1
            Dim gray = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim binary = gray.Threshold(0, 255, cv.ThresholdTypes.Otsu Or cv.ThresholdTypes.BinaryInv)
            Dim labelView = dst1.EmptyClone
            Dim stats As New cv.Mat
            Dim centroids As New cv.Mat
            Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
            Dim labelCount = cv.Cv2.ConnectedComponentsWithStats(binary, labelView, stats, centroids)
            If cc.LabelCount <= 1 Then Exit Sub
            cc.RenderBlobs(labelView)

            Dim maxBlob = cc.GetLargestBlob()
            dst2.SetTo(0)
            cc.FilterByBlob(dst1, dst2, maxBlob)

            For Each b In cc.Blobs.Skip(1)
                dst1.Rectangle(b.Rect, cv.Scalar.Red, 2, task.lineType)
            Next
        End If
    End Sub
End Class








Public Class Blob_Rectangles
    Inherits VBparent
    Dim blobs As Blob_Largest
    Dim kalman() As Kalman_Basics
    Private Class CompareRect : Implements IComparer(Of cv.Rect)
        Public Function Compare(ByVal a As cv.Rect, ByVal b As cv.Rect) As Integer Implements IComparer(Of cv.Rect).Compare
            Dim aSize = a.Width * a.Height
            Dim bSize = b.Width * b.Height
            If aSize > bSize Then Return -1
            Return 1
        End Function
    End Class
    Public Sub New()
        initParent()
        blobs = New Blob_Largest()
        task.desc = "Get the blobs and their masks and outline them with a rectangle."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        blobs.Run()
        dst1 = src.Clone()
        dst2 = blobs.dst2

        ' sort the blobs by size before delivery to kalman
        Dim sortedBlobs As New SortedList(Of cv.Rect, Integer)(New CompareRect)
        For i = 0 To blobs.rects.Count - 1
            sortedBlobs.Add(blobs.rects(i), i)
        Next
        Static blobCount As Integer
        Dim blobsToShow = Math.Min(3, blobs.rects.Count - 1)
        If blobCount <> blobsToShow And blobsToShow > 0 Then
            blobCount = blobsToShow
            ReDim kalman(blobsToShow - 1)
            For i = 0 To blobsToShow - 1
                kalman(i) = New Kalman_Basics()
                ReDim kalman(i).kInput(4 - 1)
            Next
        End If

        label1 = "Showing top " + CStr(blobsToShow) + " of the " + CStr(blobs.rects.Count) + " blobs found "
        For i = 0 To blobsToShow - 1
            Dim rect = sortedBlobs.ElementAt(i).Key
            kalman(i).kInput = {rect.X, rect.Y, rect.Width, rect.Height}
            kalman(i).Run()
            rect = New cv.Rect(kalman(i).kOutput(0), kalman(i).kOutput(1), kalman(i).kOutput(2), kalman(i).kOutput(3))
            dst1.Rectangle(rect, task.scalarColors(i Mod 255), 2)
        Next
    End Sub
End Class






Public Class Blob_Largest
    Inherits VBparent
    Dim blobs As Blob_DepthClusters
    Public rects As List(Of cv.Rect)
    Public masks As List(Of cv.Mat)
    Public kalman As Kalman_Basics
    Public blobIndex As Integer
    Public maskIndex As Integer
    Public Sub New()
        initParent()
        kalman = New Kalman_Basics()
        ReDim kalman.kInput(4 - 1)

        blobs = New Blob_DepthClusters()
        task.desc = "Gather all the blob data and display the largest."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        blobs.src = src
        blobs.Run()
        dst2 = blobs.dst2
        rects = blobs.flood.rects
        masks = blobs.flood.masks

        If masks.Count > 0 Then
            dst1.SetTo(0)
            maskIndex = blobs.flood.sortedSizes.ElementAt(blobIndex).Value ' this is the largest boundary rectangle
            src.CopyTo(dst1, masks(maskIndex))
            kalman.kInput = {rects(maskIndex).X, rects(maskIndex).Y, rects(maskIndex).Width, rects(maskIndex).Height}
            kalman.Run()
            Dim res = kalman.kOutput
            Dim rect = New cv.Rect(CInt(res(0)), CInt(res(1)), CInt(res(2)), CInt(res(3)))
            dst1.Rectangle(rect, cv.Scalar.Red, 2)
        End If
        label1 = "Show the largest blob of the " + CStr(rects.Count) + " blobs"
    End Sub
End Class





'Public Class Blob_LargestDepthCluster
'    Inherits VBparent
'    Dim blobs As Blob_DepthClusters
'    Public Sub New()
'        initParent()
'        blobs = New Blob_DepthClusters()

'        task.desc = "Display only the largest depth cluster (might not be contiguous.)"
'    End Sub
'    Public Sub Run()
'        If task.intermediateReview = caller Then task.intermediateObject = Me
'        blobs.src = src
'        blobs.Run()
'        dst2 = blobs.dst2
'        Dim blobList = blobs.histBlobs.ranges

'        Dim maxSize = Single.MinValue
'        Dim maxIndex As Integer
'        For i = 0 To blobs.histBlobs.counts.Count - 1
'            If maxSize < blobs.histBlobs.counts(i) Then
'                maxSize = blobs.histBlobs.counts(i)
'                maxIndex = i
'            End If
'        Next
'        Dim startEndDepth = blobs.histBlobs.ranges(maxIndex)
'        Dim tmp As New cv.Mat, mask As New cv.Mat
'        cv.Cv2.InRange(task.depth32f, startEndDepth.X, startEndDepth.Y, tmp)
'        cv.Cv2.ConvertScaleAbs(tmp, mask)
'        dst1.SetTo(0)
'        src.CopyTo(dst1, mask)
'        label1 = "Largest Depth Blob: " + Format(maxSize, "#,000") + " pixels (" + Format(maxSize / src.Total, "#0.0%") + ")"
'    End Sub
'End Class







Public Class Blob_DepthClusters
    Inherits VBparent
    Public histBlobs As Histogram_DepthValleys
    Public flood As FloodFill_Basics
    Public Sub New()
        initParent()

        histBlobs = New Histogram_DepthValleys

        flood = New FloodFill_Basics()
        Dim loSlider = findSlider("FloodFill LoDiff")
        Dim hiSlider = findSlider("FloodFill HiDiff")
        loSlider.Value = 1 ' pixels are exact.
        hiSlider.Value = 1 ' pixels are exact.

        label2 = "Backprojection of identified histogram depth clusters."
        task.desc = "Highlight the distinct histogram blobs found with depth clustering."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        histBlobs.src = task.noDepthMask
        histBlobs.Run()
        dst1 = histBlobs.dst1
        flood.src = histBlobs.dst2
        flood.initialMask = task.noDepthMask
        flood.Run()
        dst2 = flood.dst2
        label1 = CStr(histBlobs.ranges.Count) + " Depth Clusters"
    End Sub
End Class







Public Class Blob_DepthPixelSampler
    Inherits VBparent
    Public histBlobs As Histogram_DepthClusters
    Public flood As FloodFill_Basics
    Dim pixel As Pixel_Sampler
    Public Sub New()
        initParent()
        pixel = New Pixel_Sampler
        histBlobs = New Histogram_DepthClusters

        flood = New FloodFill_Basics()
        Dim loSlider = findSlider("FloodFill LoDiff")
        Dim hiSlider = findSlider("FloodFill HiDiff")
        loSlider.Value = 1 ' pixels are exact.
        hiSlider.Value = 1 ' pixels are exact.

        label2 = "Backprojection of identified histogram depth clusters."
        task.desc = "Highlight the distinct histogram blobs found with depth clustering."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        histBlobs.src = task.noDepthMask
        histBlobs.Run()
        dst1 = histBlobs.dst1
        flood.src = histBlobs.dst2
        flood.initialMask = task.noDepthMask
        flood.Run()

        Static lastFrame = flood.dst2
        Static lastCount = flood.rects.Count
        If task.cameraStable = False Or task.frameCount = 0 Then
            lastFrame = flood.dst2.Clone
            dst2 = flood.dst2.Clone
        Else
            For i = 0 To flood.rects.Count - 1
                Dim rect = flood.rects(i)
                Dim mask = flood.masks(i)(rect)
                pixel.src = lastFrame(rect).Clone
                Dim inverse = 255 - mask
                pixel.src.SetTo(0, inverse)
                pixel.Run()
                dst2(rect).SetTo(pixel.dominantGray, mask)
            Next
            lastFrame = dst2.Clone
        End If
        'dst2.SetTo(0, task.noDepthMask)
        label1 = CStr(histBlobs.valleys.ranges.Count) + " Depth Clusters"
    End Sub
End Class







Public Class Blob_DepthRanges
    Inherits VBparent
    Public histBlobs As Histogram_DepthClusters
    Public Sub New()
        initParent()
        histBlobs = New Histogram_DepthClusters

        label2 = "Identified histogram depth clusters."
        task.desc = "Highlight the distinct histogram blobs found with depth clustering."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        histBlobs.Run()
        dst1 = histBlobs.dst1

        Dim ranges = New List(Of cv.Point)(histBlobs.valleys.ranges)
        Dim rangeColors = New List(Of Integer)(histBlobs.valleys.rangeColors)
        Dim map = histBlobs.valleys.palette.gradientColorMap

        Dim tmp As New cv.Mat
        For i = 0 To ranges.Count - 1
            cv.Cv2.InRange(task.depth32f, ranges(i).X, ranges(i).Y, tmp)
            dst2.SetTo(map.Get(Of cv.Vec3b)(0, rangeColors(i)), tmp)
        Next
        dst2.SetTo(0, task.noDepthMask)

        label1 = CStr(histBlobs.valleys.ranges.Count) + " Depth Clusters"
    End Sub
End Class