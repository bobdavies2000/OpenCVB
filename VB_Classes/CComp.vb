Imports cv = OpenCvSharp
Imports System.Threading
'https://github.com/oreillymedia/Learning-OpenCV-3_examples/blob/master/example_14-03.cpp
Public Class CComp_Basics : Inherits VBparent
    Public connectedComponents As Object
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public edgeMask As cv.Mat
    Dim mats As New Mat_4to1
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "CComp Min Area", 0, 10000, 500)
            sliders.setupTrackBar(1, "CComp Max Area", 0, dst1.Width * dst1.Height / 2, dst1.Width * dst1.Height / 4)
            sliders.setupTrackBar(2, "CComp threshold", 0, 255, 128)
        End If
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "Use OTSU to binarize the image"
            check.Box(1).Text = "Input to CComp is above CComp threshold"
            check.Box(0).Checked = True
        End If

        task.desc = "Draw bounding boxes around RGB binarized connected Components"
    End Sub
    Private Function renderBlobs(minSize As Integer, mask As cv.Mat, maxSize As Integer) As Integer
        Dim count As Integer = 0
        For Each blob In connectedComponents.Blobs
            If blob.Area < minSize Or blob.Area > maxSize Then Continue For ' skip it if too small or too big ...
            If blob.rect.width * blob.rect.height >= dst1.Width * dst1.Height Then Continue For
            If blob.rect.width = dst1.Width Or blob.rect.height = dst1.Height Then Continue For
            Dim rect = blob.Rect
            rects.Add(rect)
            Dim nextMask = mask(rect)
            masks.Add(nextMask)

            Dim m = cv.Cv2.Moments(nextMask, True)
            If m.M00 = 0 Then Continue For ' avoid divide by zero...
            centroids.Add(New cv.Point(CInt(m.M10 / m.M00 + rect.x), CInt(m.M01 / m.M00 + rect.y)))
            If standalone Then dst1(blob.Rect).SetTo(task.scalarColors(count), (dst2)(blob.Rect))
            count += 1
        Next
        Return count
    End Function
    Public Sub Run(src As cv.Mat)
        Static minSizeSlider = findSlider("CComp Min Area")
        Static maxSizeSlider = findSlider("CComp Max Area")
        Static thresholdSlider = findSlider("CComp threshold")
        Dim minSize = minSizeSlider.value
        Dim maxSize = maxSizeSlider.value
        Dim threshold = thresholdSlider.value

        rects.Clear()
        centroids.Clear()
        masks.Clear()
        dst1.SetTo(0)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim tFlag = If(check.Box(1).Checked, OpenCvSharp.ThresholdTypes.Binary, OpenCvSharp.ThresholdTypes.BinaryInv)
        tFlag += If(check.Box(0).Checked, OpenCvSharp.ThresholdTypes.Otsu, 0)
        mats.mat(0) = src.Threshold(threshold, 255, tFlag)
        If edgeMask IsNot Nothing Then mats.mat(0).SetTo(0, edgeMask)

        connectedComponents = cv.Cv2.ConnectedComponentsEx(mats.mat(0))
        connectedComponents.renderblobs(mats.mat(2))

        Dim count = renderBlobs(minSize, mats.mat(0), maxSize)
        cv.Cv2.BitwiseNot(mats.mat(0), mats.mat(1))

        connectedComponents = cv.Cv2.ConnectedComponentsEx(mats.mat(1))
        connectedComponents.renderblobs(mats.mat(3))

        count += renderBlobs(minSize, mats.mat(1), maxSize)
        If standalone Then
            For i = 0 To centroids.Count - 1
                dst1.Circle(centroids.ElementAt(i), 5, cv.Scalar.Yellow, -1, task.lineType)
                dst1.Rectangle(rects.ElementAt(i), cv.Scalar.White, 2)
            Next
        End If
        label1 = CStr(count) + " items found > " + CStr(minSize) + " and < " + CStr(maxSize)
        connectedComponents.renderblobs(dst1)

        mats.Run(Nothing)
        If check.Box(0).Checked Then
            If check.Box(1).Checked Then
                label2 = "OTSU light, OTSU dark, rendered light, rendered dark"
            Else
                label2 = "OTSU dark, OTSU light, rendered dark, rendered light"
            End If
        Else
            If check.Box(1).Checked Then
                label2 = ">Slider, <Slider, rendered >Slider, rendered <slider"
            Else
                label2 = "<Slider, >Slider, rendered <Slider, rendered >slider"
            End If
        End If
        dst2 = mats.dst1
    End Sub
End Class







Public Class CComp_Basics_FullImage : Inherits VBparent
    Dim mats As New Mat_4to1
    Dim basics As New CComp_Basics
    Public Sub New()
        task.desc = "Connect components in the light half of OTSU threshold output, then use the dark half, then combine results."
        label2 = "Masks binary+otsu used to compute mean depth"
    End Sub
    Private Function colorWithDepth(matIndex As Integer) As Integer
        Static minSizeSlider = findSlider("CComp Min Area")
        Static maxSizeSlider = findSlider("CComp Max Area")
        Dim minSize = minSizeSlider.value
        Dim maxSize = maxSizeSlider.value

        Dim cc = cv.Cv2.ConnectedComponentsEx(mats.mat(matIndex))

        Dim blobList As New List(Of cv.Rect)
        For Each blob In cc.Blobs
            If blob.Rect.Width > 1 And blob.Rect.Height > 1 Then blobList.Add(blob.Rect)
        Next

        blobList.Sort(Function(a, b) (a.Width * a.Height).CompareTo(b.Width * b.Height))

        Dim count As Integer = 0
        For Each blob In cc.Blobs
            If blob.Area < minSize Or blob.Area > maxSize Then Continue For ' skip it if too small or too big ...
            count += 1
            Dim avg = task.RGBDepth(blob.Rect).Mean(mats.mat(matIndex)(blob.Rect))
            dst1(blob.Rect).SetTo(avg, mats.mat(matIndex)(blob.Rect))
        Next
        Return count
    End Function
    Public Sub Run(src As cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1.SetTo(0)

        mats.mat(0) = src.Threshold(0, 255, cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu)
        Dim count = colorWithDepth(0)
        cv.Cv2.BitwiseNot(mats.mat(0), mats.mat(1))
        count += colorWithDepth(1)
        label1 = CStr(count) + " items found and colored mean depth"

        mats.Run(Nothing)
        dst2 = mats.dst1
    End Sub
End Class



Public Class CComp_PointTracker : Inherits VBparent
    Public basics As New CComp_Basics
    Public highlight As New Highlight_Basics
    Public trackPoints As Boolean = True
    Public Sub New()
        task.desc = "Track connected componenent centroids and use it to match coloring"
    End Sub
    Public Sub Run(src as cv.Mat)
        basics.Run(src)

        If trackPoints Then
            Static topView As New PointCloud_Kalman_TopView
            dst2 = basics.dst1
            topView.pTrack.queryPoints = basics.centroids
            topView.pTrack.queryRects = basics.rects
            topView.pTrack.queryMasks = basics.masks
            topView.pTrack.Run(src)
            dst1 = topView.pTrack.dst1

            highlight.viewObjects = topView.pTrack.drawRC.viewObjects
            highlight.Run(dst1)
            dst1 = highlight.dst1
            If highlight.highlightPoint <> New cv.Point Then
                dst2 = highlight.dst2
                label2 = "Selected region in yellow"
            Else
                dst2 = src
            End If
            label1 = basics.label1
        End If
    End Sub
End Class







Public Class CComp_MaxBlobs : Inherits VBparent
    Public tracker As New CComp_PointTracker
    Public maxBlobs As Integer = -1
    Public maxValues(255) As Integer ' march through all 255 values and find the best...
    Public incr = 2 ' some other algorithms change this...
    Public Sub New()
        Dim checkOTSU = findCheckBox("Use OTSU to binarize the image")
        checkOTSU.Checked = False ' turn off OTSU so the slider works...

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Reassess the best CComp threshold"
        End If

        task.desc = "Find the best CComp threshold to maximize the number of blobs"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static thresholdSlider = findSlider("CComp threshold")
        task.trueText("This algorithm will survey the different ccomp threshold options.", 10, 100, 3)
        If task.frameCount < 10 Then
            thresholdSlider.value = 0
            Exit Sub
        End If

        tracker.Run(src)
        dst1 = tracker.dst1

        If maxBlobs = -1 Then
            tracker.trackPoints = False ' not tracking yet...
            For i = 10 To 160 Step incr
                maxValues(thresholdSlider.value) = tracker.basics.centroids.Count
                If thresholdSlider.value + incr < 255 Then thresholdSlider.value = i
                tracker.Run(src)
            Next
            tracker.trackPoints = True

            For i = 0 To maxValues.Count - 1
                If maxBlobs <= maxValues(i) Then
                    maxBlobs = maxValues(i)
                    thresholdSlider.value = i
                End If
            Next
        End If

        If check.Box(0).Checked Then
            check.Box(0).Checked = False
            maxBlobs = -1
            thresholdSlider.value = 0
            ReDim maxValues(255)
        End If
        'label1 = "There were " + CStr(tracker.pTrack.queryPoints.Count) + " regions identified"
    End Sub
End Class





Public Class CComp_MaxPixels : Inherits VBparent
    Dim maxBlob As New CComp_MaxBlobs
    Public maxPixels As Integer = -1
    Public Sub New()
        maxBlob.incr = 5
        task.desc = "Find the best CComp threshold to maximize pixels"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static pixelValues(255) As Integer ' march through all 255 values and find the best...
        Static thresholdSlider = findSlider("CComp threshold")

        maxBlob.Run(src)
        dst1 = maxBlob.dst1

        If maxPixels = -1 Then
            For i = Math.Max(thresholdSlider.value - 10, 0) To Math.Min(thresholdSlider.value + 10, 255)
                thresholdSlider.value = i
                maxBlob.Run(src)
                Dim pixelCount = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero()
                pixelValues(thresholdSlider.value) = pixelCount
            Next
            For i = 0 To pixelValues.Count - 1
                If maxPixels < pixelValues(i) Then
                    maxPixels = pixelValues(i)
                    thresholdSlider.value = i
                End If
            Next
            maxBlob.Run(src)
            dst1 = maxBlob.dst1
        Else
            Dim pixelCount = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero()
            ' label1 = CStr(CInt(pixelCount / 1024)) + "k pixels with " + CStr(maxBlob.tracker.pTrack.queryPoints.Count) + " regions width threshold=" + CStr(thresholdSlider.value)
        End If
    End Sub
End Class




Public Class CComp_DepthEdges : Inherits VBparent
    Dim ccomp As New CComp_PointTracker
    Dim depth As New Depth_Edges
    Public Sub New()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Use edge mask in connected components"
            check.Box(0).Checked = True
        End If

        task.desc = "Use depth edges to isolate connected components in depth"
    End Sub
    Public Sub Run(src As cv.Mat)
        depth.Run(src)
        If standalone Or task.intermediateReview = caller Then dst2 = depth.dst2

        'If check.Box(0).Checked Then ccomp.basics.edgeMask = depth.dst2 Else ccomp.basics.edgeMask = Nothing
        If check.Box(0).Checked Then src.SetTo(0, depth.dst2)
        ccomp.Run(src)
        dst1 = ccomp.dst1
        If ccomp.highlight.highlightPoint <> New cv.Point Then dst2 = ccomp.highlight.dst2
    End Sub
End Class





Public Class CComp_EdgeMask : Inherits VBparent
    Dim ccomp As New CComp_ColorDepth
    Dim edges As New Edges_DepthAndColor
    Public Sub New()
        task.desc = "Isolate Color connected components after applying the Edge Mask"
        label1 = "Edges_DepthAndColor (input to ccomp)"
        label2 = "Blob Rectangles with centroids (white)"
    End Sub
    Public Sub Run(src As cv.Mat)
        edges.Run(src)
        dst1 = edges.dst1

        ccomp.Run(src)
        dst2 = ccomp.dst1
    End Sub
End Class








Public Class CComp_ColorDepth : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 1)
            sliders.setupTrackBar(0, "Min Blob size", 0, 10000, 100)
        End If
        label1 = "Color by Mean Depth"
        label2 = "Binary image using threshold binary+Otsu"
        task.desc = "Color connected components based on their depth"
    End Sub
    Public Sub Run(src As cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = src.Threshold(0, 255, OpenCvSharp.ThresholdTypes.Binary + OpenCvSharp.ThresholdTypes.Otsu)
        dst1 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim cc = cv.Cv2.ConnectedComponentsEx(dst2)

        For Each blob In cc.Blobs.Skip(1)
            Dim roi = blob.Rect
            Dim avg = task.RGBDepth(roi).Mean(dst2(roi))
            dst1(roi).SetTo(avg, dst2(roi))
        Next

        For Each blob In cc.Blobs.Skip(1)
            If blob.Area > sliders.trackbar(0).Value Then dst1.Rectangle(blob.Rect, cv.Scalar.White, 2)
        Next
    End Sub
End Class








Public Class CComp_InRange_MT : Inherits VBparent
    Public Sub New()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "InRange # of ranges", 2, 255, 15)
        sliders.setupTrackBar(1, "InRange min Blob Size (in pixels) X1000", 1, 100, 10)

        task.desc = "Connected components in specific ranges"
        label2 = "Blob rectangles - largest to smallest"
    End Sub
    Public Sub Run(src As cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim rangeCount As Integer = sliders.trackbar(0).Value
        Dim minBlobSize = sliders.trackbar(1).Value * 1000

        Dim mask = task.depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        dst1.SetTo(0)
        Dim totalBlobs As Integer
        Parallel.For(0, rangeCount,
        Sub(i)
            Dim lowerBound = i * (255 / rangeCount)
            Dim upperBound = (i + 1) * (255 / rangeCount)
            Dim binary = src.InRange(lowerBound, upperBound)
            Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
            Dim roiList As New List(Of cv.Rect)
            For Each blob In cc.Blobs.Skip(1) ' skip the blob for the whole image.
                If blob.Rect.Width * blob.Rect.Height > minBlobSize Then roiList.Add(blob.Rect)
            Next
            Interlocked.Add(totalBlobs, roiList.Count)
            roiList.Sort(Function(a, b) (a.Width * a.Height).CompareTo(b.Width * b.Height))
            For j = roiList.Count - 1 To 0 Step -1
                Dim bin = binary(roiList(j)).Clone()
                Dim depth = task.depth32f(roiList(j))
                Dim meanDepth = depth.Mean(mask(roiList(j)))
                If meanDepth.Item(0) < task.maxDepth Then
                    Dim avg = task.RGBDepth(roiList(j)).Mean(mask(roiList(j)))
                    dst1(roiList(j)).SetTo(avg, bin)
                    dst2(roiList(j)).SetTo(avg)
                End If
            Next
        End Sub)
        label1 = "# of blobs = " + CStr(totalBlobs) + " in " + CStr(rangeCount) + " regions"
    End Sub
End Class








Public Class CComp_InRange : Inherits VBparent
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "InRange # of ranges", 1, 20, 15)
            sliders.setupTrackBar(1, "InRange min Blob Size (in pixels) X1000", 1, 100, 10)
        End If
        task.desc = "Connect components in specific ranges"
    End Sub
    Public Sub Run(src As cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim rangeCount As Integer = sliders.trackbar(0).Value
        Dim minBlobSize = sliders.trackbar(1).Value * 1000

        Dim mask = task.depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        Dim roiList As New List(Of cv.Rect)
        For i = 0 To rangeCount - 1
            Dim lowerBound = i * (255 / rangeCount)
            Dim upperBound = (i + 1) * (255 / rangeCount)
            Dim binary = src.InRange(lowerBound, upperBound)
            Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
            For Each blob In cc.Blobs.Skip(1) ' skip the blob for the whole image.
                If blob.Rect.Width * blob.Rect.Height > minBlobSize Then roiList.Add(blob.Rect)
            Next
        Next
        roiList.Sort(Function(a, b) (a.Width * a.Height).CompareTo(b.Width * b.Height))
        For i = 0 To roiList.Count - 1
            Dim avg = task.RGBDepth(roiList(i)).Mean(mask(roiList(i)))
            dst1(roiList(i)).SetTo(avg)
        Next

        src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.AddWeighted(dst1, 0.5, src, 0.5, 0, dst1)
        label1 = "# of blobs = " + CStr(roiList.Count) + " in " + CStr(rangeCount) + " regions - smallest in front"
    End Sub
End Class





' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.ConnectedComponents.RenderBlobs(OpenCvSharp.Mat)/
Public Class CComp_Shapes : Inherits VBparent
    Dim shapes As cv.Mat
    Public Sub New()
        shapes = New cv.Mat(task.parms.homeDir + "Data/Shapes.png", cv.ImreadModes.Color)
        label1 = "Largest connected component"
        label2 = "RectView, LabelView, Binary, grayscale"
        task.desc = "Use connected components to isolate objects in image."
    End Sub
    Public Sub Run(src As cv.Mat)
        Dim gray = shapes.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim binary = gray.Threshold(0, 255, cv.ThresholdTypes.Otsu + cv.ThresholdTypes.Binary)
        Dim labelview = shapes.EmptyClone()
        Dim rectView = binary.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
        If cc.LabelCount <= 1 Then Exit Sub

        cc.RenderBlobs(labelview)
        For Each blob In cc.Blobs.Skip(1)
            rectView.Rectangle(blob.Rect, cv.Scalar.Red, 2)
        Next

        Dim maxBlob = cc.GetLargestBlob()
        Dim filtered = New cv.Mat
        cc.FilterByBlob(shapes, filtered, maxBlob)
        dst1 = filtered.Resize(dst1.Size())

        Dim matTop As New cv.Mat, matBot As New cv.Mat, mat As New cv.Mat
        cv.Cv2.HConcat(rectView, labelview, matTop)
        cv.Cv2.HConcat(binary, gray, matBot)
        matBot = matBot.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.VConcat(matTop, matBot, mat)
        dst2 = mat.Resize(dst2.Size())
    End Sub
End Class






'https://github.com/oreillymedia/Learning-OpenCV-3_examples/blob/master/example_14-03.cpp
Public Class CComp_Simple : Inherits VBparent
    Public connectedComponents As Object
    Public rects As New List(Of cv.Rect)
    Public centroids As New List(Of cv.Point2f)
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "CComp Min Area", 0, 10000, 500)
            sliders.setupTrackBar(1, "CComp Max Area", 0, dst1.Width * dst1.Height / 2, dst1.Width * dst1.Height / 4)
            sliders.setupTrackBar(2, "CComp threshold", 0, 255, 50)
        End If

        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        task.desc = "Draw bounding boxes around RGB binarized connected Components"
    End Sub
    Public Sub Run(src As cv.Mat)
        Static thresholdSlider = findSlider("CComp threshold")
        Static minSizeSlider = findSlider("CComp Min Area")
        Static maxSizeSlider = findSlider("CComp Max Area")
        Dim minSize = minSizeSlider.value
        Dim maxSize = maxSizeSlider.value

        rects.Clear()
        centroids.Clear()

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        dst1 = input.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.BinaryInv) '  + cv.ThresholdTypes.Otsu

        connectedComponents = cv.Cv2.ConnectedComponentsEx(dst1)
        connectedComponents.renderblobs(dst2)

        Dim count As Integer = 0
        For Each blob In connectedComponents.Blobs
            If blob.Area < minSize Or blob.Area > maxSize Then Continue For
            Dim rect = blob.Rect

            Dim m = cv.Cv2.Moments(dst1(rect), True)
            If m.M00 = 0 Then Continue For ' avoid divide by zero...
            rects.Add(rect)
            centroids.Add(New cv.Point(CInt(m.M10 / m.M00 + rect.x), CInt(m.M01 / m.M00 + rect.y)))
            count += 1
        Next

        label1 = CStr(count) + " items found > " + CStr(minSize) + " and < " + CStr(maxSize)
    End Sub
End Class







Public Class CComp_Binarized : Inherits VBparent
    Dim edges As New Edges_BinarizedSobel
    Dim ccomp As New CComp_Simple
    Public Sub New()
        task.desc = "Find connected components using an image with binarized edges"
    End Sub
    Public Sub Run(src as cv.Mat)
        edges.Run(src)
        dst1 = edges.dst2
        ccomp.Run(dst1)
        dst2 = ccomp.dst2
    End Sub
End Class





