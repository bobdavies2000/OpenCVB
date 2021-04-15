Imports cv = OpenCvSharp
' https://stackoverflow.com/questions/14770756/opencv-simpleblobdetector-filterbyinertia-meaning
Public Class Blob_Basics : Inherits VBparent
    Dim options As Blob_Options
    Dim input As Blob_Input
    Dim blobDetector As New CS_Classes.Blob_Basics
    Public Sub New()
        options = New Blob_Options
        blobDetector = New CS_Classes.Blob_Basics
        If standalone Then input = New Blob_Input()

        task.desc = "Isolate and list blobs with specified options"
        ' task.rank = 1
    End Sub
    Public Sub Run(src As cv.Mat)
        options.Run(src)

        If standalone Then
            input.Run(src)
            dst1 = input.dst1
        Else
            dst1 = src
        End If
        blobDetector.Run(dst1, dst2, options.blobParams)
    End Sub
End Class









Public Class Blob_Options : Inherits VBparent
    Dim blob As Blob_Input
    Dim blobDetector As New CS_Classes.Blob_Basics
    Public blobParams = New cv.SimpleBlobDetector.Params
    Public Sub New()
        blobDetector = New CS_Classes.Blob_Basics
        If standalone Then
            blob = New Blob_Input()
            blob.updateFrequency = 30
        End If

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 5)
            radio.check(0).Text = "FilterByArea"
            radio.check(1).Text = "FilterByCircularity"
            radio.check(2).Text = "FilterByConvexity"
            radio.check(3).Text = "FilterByInertia"
            radio.check(4).Text = "FilterByColor"
            radio.check(1).Checked = True
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "min Threshold", 0, 255, 100)
            sliders.setupTrackBar(1, "max Threshold", 0, 255, 255)
            sliders.setupTrackBar(2, "Threshold Step", 1, 50, 5)
        End If
        task.desc = "Prepare options for a blob detection run."
        ' task.rank = 1
    End Sub
    Public Sub Run(src As cv.Mat)
        blobParams = New cv.SimpleBlobDetector.Params
        If radio.check(0).Checked Then blobParams.FilterByArea = radio.check(0).Checked
        If radio.check(1).Checked Then blobParams.FilterByCircularity = radio.check(1).Checked
        If radio.check(2).Checked Then blobParams.FilterByConvexity = radio.check(2).Checked
        If radio.check(3).Checked Then blobParams.FilterByInertia = radio.check(3).Checked
        If radio.check(4).Checked Then blobParams.FilterByColor = radio.check(4).Checked

        blobParams.MaxArea = 100
        blobParams.MinArea = 0.001

        blobParams.MinThreshold = sliders.trackbar(0).Value
        blobParams.MaxThreshold = sliders.trackbar(1).Value
        blobParams.ThresholdStep = sliders.trackbar(2).Value

        blobParams.MinDistBetweenBlobs = 10
        blobParams.MinRepeatability = 1

        If standalone Then
            blob.Run(src)
            dst1 = blob.dst1

            ' The create method in SimpleBlobDetector is not available in VB.Net.  Not sure why.  To get around this, just use C# where create method works fine.
            blobDetector.Run(dst1, dst2, blobParams)
        End If
    End Sub
End Class







Public Class Blob_Input : Inherits VBparent
    Dim rectangles As Rectangle_Rotated
    Dim circles As Draw_Circles
    Dim ellipses As Draw_Ellipses
    Dim poly As Draw_Polygon
    Public Mats As Mat_4to1
    Public updateFrequency = 30
    Public Sub New()
        rectangles = New Rectangle_Rotated
        circles = New Draw_Circles
        ellipses = New Draw_Ellipses
        poly = New Draw_Polygon

        findSlider("DrawCount").Value = 5
        findSlider("Update Frequency").Value = 1
        findCheckBox("Draw filled (unchecked draw an outline)").Checked = True

        Mats = New Mat_4to1()
        Mats.noLines = True

        label1 = "Click any quadrant below to view it on the right"
        label2 = "Click any quadrant at left to view it below"
        task.desc = "Generate data to test Blob Detector."
        ' task.rank = 1
    End Sub
    Public Sub Run(src As cv.Mat)
        If task.frameCount Mod updateFrequency = 0 Then
            rectangles.Run(src)
            Mats.mat(0) = rectangles.dst1

            circles.Run(src)
            Mats.mat(1) = circles.dst1

            ellipses.Run(src)
            Mats.mat(2) = ellipses.dst1

            poly.Run(src)
            Mats.mat(3) = poly.dst2
            Mats.Run(Nothing)
            Mats.dst1.CopyTo(dst1)
            If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setMyActiveMat()
            dst2 = Mats.mat(quadrantIndex)
        End If
    End Sub
End Class




Public Class Blob_RenderBlobs : Inherits VBparent
    Dim input As Blob_Input
    Public Sub New()
        input = New Blob_Input()

        label1 = "Input blobs"
        label2 = "Largest blob, centroid in yellow"
        task.desc = "Use connected components to find blobs."
        ' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.frameCount Mod input.updateFrequency = 0 Then
            input.Run(src)
            dst1 = input.dst1
            Dim gray = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim binary = gray.Threshold(0, 255, cv.ThresholdTypes.Otsu Or cv.ThresholdTypes.Binary)
            Dim labelView = dst1.EmptyClone
            Dim stats As New cv.Mat
            Dim centroids As New cv.Mat
            Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
            Dim labelCount = cv.Cv2.ConnectedComponentsWithStats(binary, labelView, stats, centroids)
            cc.RenderBlobs(labelView)

            For Each b In cc.Blobs.Skip(1)
                dst1.Rectangle(b.Rect, cv.Scalar.Red, 2, task.lineType)
            Next

            Dim maxBlob = cc.GetLargestBlob()
            dst2.SetTo(0)
            cc.FilterByBlob(dst1, dst2, maxBlob)

            dst2.Circle(maxBlob.Centroid, task.dotSize + 3, cv.Scalar.Blue, -1, task.lineType)
            dst2.Circle(maxBlob.Centroid, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        End If
    End Sub
End Class







Public Class Blob_DepthClusters : Inherits VBparent
    Public histBlobs As Histogram_DepthValleys
    Public flood As FloodFill_Basics
    Public Sub New()
        histBlobs = New Histogram_DepthValleys

        flood = New FloodFill_Basics()
        findSlider("FloodFill LoDiff").Value = 1
        findSlider("FloodFill HiDiff").Value = 1

        task.desc = "Highlight the distinct histogram blobs found with depth clustering."
        'task.rank = 3
    End Sub
    Public Sub Run(src as cv.Mat)
        histBlobs.Run(task.noDepthMask)
        dst1 = histBlobs.dst1
        label1 = CStr(histBlobs.ranges.Count) + " Depth Clusters"
    End Sub
End Class







Public Class Blob_DepthPixelSampler : Inherits VBparent
    Public histBlobs As Histogram_DepthClusters
    Public flood As FloodFill_Basics
    Dim pixel As Pixel_Sampler
    Public Sub New()
        pixel = New Pixel_Sampler
        histBlobs = New Histogram_DepthClusters

        flood = New FloodFill_Basics()
        findSlider("FloodFill LoDiff").Value = 1
        findSlider("FloodFill HiDiff").Value = 1

        label2 = "Backprojection of identified histogram depth clusters."
        task.desc = "Highlight the distinct histogram blobs found with depth clustering."
        'task.rank = 2
    End Sub
    Public Sub Run(src as cv.Mat)
        histBlobs.Run(task.noDepthMask)
        dst1 = histBlobs.dst1
        flood.initialMask = task.noDepthMask
        flood.Run(histBlobs.dst2)

        Static lastFrame = flood.dst2
        Static lastCount = flood.rects.Count
        If task.cameraStable = False Or task.frameCount = 0 Then
            lastFrame = flood.dst2.Clone
            dst2 = flood.dst2.Clone
        Else
            For i = 0 To flood.rects.Count - 1
                Dim rect = flood.rects(i)
                Dim mask = flood.masks(i)(rect)
                pixel.Run(lastFrame(rect).Clone.setTo(0, 255 - mask))
                dst2(rect).SetTo(pixel.dominantGray, mask)
            Next
            lastFrame = dst2.Clone
        End If
        'dst2.SetTo(0, task.noDepthMask)
        label1 = CStr(histBlobs.valleys.ranges.Count) + " Depth Clusters"
    End Sub
End Class







Public Class Blob_DepthRanges : Inherits VBparent
    Public histBlobs As Histogram_DepthClusters
    Public grayOnly As Boolean
    Public masks As New List(Of cv.Mat)
    Public maskSizes As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public ranges As New List(Of cv.Point)
    Public Sub New()
        histBlobs = New Histogram_DepthClusters

        label2 = "Identified histogram depth clusters."
        task.desc = "Highlight the distinct histogram blobs found with depth clustering."
        'task.rank = 4
    End Sub
    Public Sub Run(src As cv.Mat)
        histBlobs.Run(src)
        dst1 = histBlobs.dst1

        ranges = New List(Of cv.Point)(histBlobs.valleys.ranges)
        Dim map = task.palette.gradientColorMap

        ' this is close and more efficient but not precise!
        'Dim rangeColors = New List(Of Integer)(histBlobs.valleys.rangeColors)
        'Dim depth = task.depth32f.Normalize(255).ConvertScaleAbs(255).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        'Dim myLUT = New cv.Mat(1, 256, cv.MatType.CV_8UC3)
        'Dim index As Integer
        'For i = 0 To 255
        '    If rangeColors(index) = i And rangeColors(index) < 255 Then index += 1
        '    myLUT.Set(Of cv.Vec3b)(0, i, map.Get(Of cv.Vec3b)(0, rangeColors(index)))
        'Next
        'dst2 = depth.LUT(myLUT)

        Dim mask As New cv.Mat
        masks.Clear()
        maskSizes.Clear()
        Dim spread = 255 / ranges.Count
        If grayOnly Then dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        For i = 0 To ranges.Count - 1
            cv.Cv2.InRange(task.depth32f, ranges(i).X, ranges(i).Y, mask)
            Dim nextColor = If(grayOnly, i + 1, map.Get(Of cv.Vec3b)(0, (i + 1) * spread))
            masks.Add(mask.Clone)
            maskSizes.Add(mask.CountNonZero(), i)
            dst2.SetTo(nextColor, mask)
        Next
        dst2.SetTo(0, task.noDepthMask)

        label1 = CStr(histBlobs.valleys.ranges.Count) + " Depth Clusters"
    End Sub
End Class







Public Class Blob_Largest : Inherits VBparent
    Public blobs As Blob_DepthRanges
    Public Sub New()
        blobs = New Blob_DepthRanges()
        task.desc = "Gather all the blob data and display the largest."
        ' task.rank = 3
    End Sub
    Public Sub Run(src as cv.Mat)
        blobs.Run(src)
        dst2 = blobs.dst2

        If blobs.masks.Count > 0 Then
            dst1.SetTo(0)
            Dim maskIndex = blobs.maskSizes.ElementAt(blobs.masks.Count - 1).Value
            src.CopyTo(dst1, blobs.masks(maskIndex))
        End If
        label1 = "Show the largest blob of the " + CStr(blobs.masks.Count) + " blobs"
    End Sub
End Class

