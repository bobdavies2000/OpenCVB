Imports cv = OpenCvSharp
' https://stackoverflow.com/questions/14770756/opencv-simpleblobdetector-filterbyinertia-meaning
Public Class Blob_Basics : Inherits VBparent
    Dim options As New Blob_Options
    Dim input As New Blob_Input
    Dim blobDetector As New CS_Classes.Blob_Basics
    Public Sub New()
        blobDetector = New CS_Classes.Blob_Basics
        task.desc = "Isolate and list blobs with specified options"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        options.RunClass(src)

        If standalone Then
            input.RunClass(src)
            dst2 = input.dst2
        Else
            dst2 = src
        End If
        blobDetector.Run(dst2, dst3, options.blobParams)
    End Sub
End Class









Public Class Blob_Options : Inherits VBparent
    Dim blob As New Blob_Input
    Dim blobDetector As New CS_Classes.Blob_Basics
    Public blobParams = New cv.SimpleBlobDetector.Params
    Public Sub New()
        blobDetector = New CS_Classes.Blob_Basics
        If standalone Then blob.updateFrequency = 30

        If radio.Setup(caller, 5) Then
            radio.check(0).Text = "FilterByArea"
            radio.check(1).Text = "FilterByCircularity"
            radio.check(2).Text = "FilterByConvexity"
            radio.check(3).Text = "FilterByInertia"
            radio.check(4).Text = "FilterByColor"
            radio.check(1).Checked = True
        End If

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "min Threshold", 0, 255, 100)
            sliders.setupTrackBar(1, "max Threshold", 0, 255, 255)
            sliders.setupTrackBar(2, "Threshold Step", 1, 50, 5)
        End If
        task.desc = "Prepare options for a blob detection run."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
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
            blob.RunClass(src)
            dst2 = blob.dst2

            ' The create method in SimpleBlobDetector is not available in VB.Net.  Not sure why.  To get around this, just use C# where create method works fine.
            blobDetector.Run(dst2, dst3, blobParams)
        End If
    End Sub
End Class







Public Class Blob_Input : Inherits VBparent
    Dim rectangles As New Rectangle_Rotated
    Dim circles As New Draw_Circles
    Dim ellipses As New Draw_Ellipses
    Dim poly As New Draw_Polygon
    Public Mats As New Mat_4Click
    Public updateFrequency = 30
    Public Sub New()
        findSlider("DrawCount").Value = 5
        findSlider("Update Frequency").Value = 1
        findCheckBox("Draw filled (unchecked draw an outline)").Checked = True

        Mats.mats.lineSeparators = False

        labels(2) = "Click any quadrant below to view it on the right"
        labels(3) = "Click any quadrant at left to view it below"
        task.desc = "Generate data to test Blob Detector."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        rectangles.RunClass(src)
        Mats.mat(0) = rectangles.dst2

        circles.RunClass(src)
        Mats.mat(1) = circles.dst2

        ellipses.RunClass(src)
        Mats.mat(2) = ellipses.dst2

        poly.RunClass(src)
        Mats.mat(3) = poly.dst3
        mats.RunClass(src)
        dst2 = Mats.dst2
        dst3 = Mats.dst3
    End Sub
End Class




Public Class Blob_RenderBlobs : Inherits VBparent
    Dim input As New Blob_Input
    Public Sub New()
        labels(2) = "Input blobs"
        labels(3) = "Largest blob, centroid in yellow"
        task.desc = "Use connected components to find blobs."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.frameCount Mod input.updateFrequency = 0 Then
            input.RunClass(src)
            dst2 = input.dst2
            Dim gray = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim binary = gray.Threshold(0, 255, cv.ThresholdTypes.Otsu Or cv.ThresholdTypes.Binary)
            Dim labelView = dst2.EmptyClone
            Dim stats As New cv.Mat
            Dim centroids As New cv.Mat
            Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
            Dim labelCount = cv.Cv2.ConnectedComponentsWithStats(binary, labelView, stats, centroids)
            cc.RenderBlobs(labelView)

            For Each b In cc.Blobs.Skip(1)
                dst2.Rectangle(b.Rect, cv.Scalar.Red, task.lineWidth + 1, task.lineType)
            Next

            Dim maxBlob = cc.GetLargestBlob()
            dst3.SetTo(0)
            cc.FilterByBlob(dst2, dst3, maxBlob)

            dst3.Circle(maxBlob.Centroid, task.dotSize + 3, cv.Scalar.Blue, -1, task.lineType)
            dst3.Circle(maxBlob.Centroid, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        End If
    End Sub
End Class







'Public Class Blob_DepthPixelSampler : Inherits VBparent
'    Public histBlobs As New Proximity_Clusters
'    Public flood As New FloodFill_Basics
'    Dim pixel As Pixel_Sampler
'    Public Sub New()
'        pixel = New Pixel_Sampler
'        findSlider("FloodFill LoDiff").Value = 1
'        findSlider("FloodFill HiDiff").Value = 1

'        labels(3) = "Backprojection of identified histogram depth clusters."
'        task.desc = "Highlight the distinct histogram blobs found with depth clustering."
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        histBlobs.RunClass(task.noDepthMask)
'        dst2 = histBlobs.dst2
'        flood.initialMask = task.noDepthMask
'        flood.RunClass(histBlobs.dst3)

'        Static lastFrame = flood.dst3
'        Static lastCount = flood.rects.Count
'        If task.cameraStable = False Or task.frameCount = 0 Then
'            lastFrame = flood.dst3.Clone
'            dst3 = flood.dst3.Clone
'        Else
'            For i = 0 To 0 ' flood.rects.Count - 1
'                Dim rect = flood.rects(i)
'                Dim mask = flood.masks(i)
'                pixel.RunClass(lastFrame(rect).Clone.setTo(0, 255 - mask))
'                dst3(rect).SetTo(pixel.dominantGray, mask)
'            Next
'            lastFrame = dst3.Clone
'        End If
'        labels(2) = CStr(histBlobs.valleys.ranges.Count) + " Depth Clusters"
'    End Sub
'End Class







Public Class Blob_DepthRanges : Inherits VBparent
    Public histBlobs As New Proximity_Clusters
    Public grayOnly As Boolean
    Public masks As New List(Of cv.Mat)
    Public maskSizes As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public ranges As New List(Of cv.Point)
    Public Sub New()
        labels(3) = "Identified histogram depth clusters."
        task.desc = "Highlight the distinct histogram blobs found with depth clustering."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        histBlobs.RunClass(src)
        dst2 = histBlobs.dst2

        ranges = New List(Of cv.Point)(histBlobs.valleys.ranges)
        Dim map = task.palette.gradientColorMap
        Dim mask As New cv.Mat
        masks.Clear()
        maskSizes.Clear()
        Dim spread = 255 / ranges.Count
        If grayOnly Then dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        For i = 0 To ranges.Count - 1
            cv.Cv2.InRange(task.depth32f, ranges(i).X, ranges(i).Y, mask)
            Dim nextColor = If(grayOnly, i + 1, map.Get(Of cv.Vec3b)(0, (i + 1) * spread))
            masks.Add(mask.Clone)
            maskSizes.Add(mask.CountNonZero(), i)
            dst3.SetTo(nextColor, mask)
        Next
        dst3.SetTo(0, task.noDepthMask)

        labels(2) = CStr(histBlobs.valleys.ranges.Count) + " Depth Clusters"
    End Sub
End Class







Public Class Blob_Largest : Inherits VBparent
    Public blobs As New Blob_DepthRanges
    Public Sub New()
        task.desc = "Gather all the blob data and display the largest."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        blobs.RunClass(src)
        dst3 = blobs.dst3

        If blobs.masks.Count > 0 Then
            dst2.SetTo(0)
            Dim maskIndex = blobs.maskSizes.ElementAt(blobs.masks.Count - 1).Value
            src.CopyTo(dst2, blobs.masks(maskIndex))
        End If
        labels(2) = "Show the largest blob of the " + CStr(blobs.masks.Count) + " blobs"
    End Sub
End Class

