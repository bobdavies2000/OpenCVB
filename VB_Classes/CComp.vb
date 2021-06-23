Imports cv = OpenCvSharp
Imports System.Threading
' https://docs.opencv.org/master/de/d01/samples_2cpp_2connected_components_8cpp-example.html
Public Class CComp_Basics : Inherits VBparent
    Public masks As New List(Of cv.Mat)
    Public rects As New List(Of cv.Rect)
    Public areas As New List(Of Integer)
    Public centroids As New List(Of cv.Point)
    Dim colorMap As cv.Mat
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "CComp Min Area", 0, 10000, 500)
            sliders.setupTrackBar(1, "Threshold for grayscale input", 0, 255, 128)
            sliders.setupTrackBar(2, "Select Mask - largest to smallest", 0, 100, 0)
        End If
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        task.palette.Run(task.color)
        colorMap = task.palette.gradientColorMap.Row(0).Clone
        task.desc = "Use a threshold slider on the CComp input"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 5
        Static maskSlider = findSlider("Select Mask - largest to smallest")
        Static areaSlider = findSlider("CComp Min Area")
        Static thresholdSlider = findSlider("Threshold for grayscale input")
        Dim threshVal = thresholdSlider.value
        Dim minSize = areaSlider.value

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If threshVal < 128 Then
            dst1 = src.Threshold(threshVal, 255, cv.ThresholdTypes.BinaryInv)
        Else
            dst1 = src.Threshold(threshVal, 255, cv.ThresholdTypes.Binary)
        End If
        Dim labels As New cv.Mat
        Dim stats As New cv.Mat
        Dim centroidRaw As New cv.Mat
        Dim nLabels = dst1.ConnectedComponentsWithStats(labels, stats, centroidRaw)

        rects.Clear()
        areas.Clear()
        centroids.Clear()
        Dim black = New cv.Vec3b(0, 0, 0)
        Dim colors As New List(Of cv.Vec3b)
        Dim maskOrder As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        Dim unsortedMasks As New List(Of cv.Mat)
        Dim unsortedRects As New List(Of cv.Rect)
        Dim unsortedCentroids As New List(Of cv.Point)
        Dim index As New List(Of Integer)
        For i = 0 To Math.Min(256, stats.Rows) - 1
            Dim area = stats.Get(Of Integer)(i, 4)
            If area > minSize And area <> src.Total Then
                Dim r1 = stats.Get(Of cv.Rect)(i, 0)
                Dim r = New cv.Rect(CInt(r1.X), CInt(r1.Y), CInt(r1.Width), CInt(r1.Height))
                If r.Width <> dst1.Width And r.Height <> dst1.Height Then
                    areas.Add(area)
                    unsortedRects.Add(r)
                    index.Add(i)
                    colors.Add(task.vecColors(colors.Count))
                    maskOrder.Add(area, unsortedMasks.Count)
                    unsortedMasks.Add(labels.InRange(i, i)(r))
                    Dim c = New cv.Point(CInt(centroidRaw.Get(Of Double)(i, 0)), CInt(centroidRaw.Get(Of Double)(i, 1)))
                    unsortedCentroids.Add(c)
                End If
            End If
        Next

        masks.Clear()
        For i = 0 To maskOrder.Count - 1
            Dim mIndex = maskOrder.ElementAt(i).Value
            masks.Add(unsortedMasks(mIndex))
            rects.Add(unsortedRects(mIndex))
            centroids.Add(unsortedCentroids(mIndex))
        Next

        ' this does not fix the color flashing problem but if the component count is the same (for the same areas) the colors will be stable.
        task.palette.gradientColorMap = colorMap.Clone
        For i = 0 To colors.Count - 1
            task.palette.gradientColorMap.Set(Of cv.Vec3b)(0, index(i), colors(i))
        Next

        labels.ConvertTo(labels, cv.MatType.CV_8U)
        task.palette.Run(labels)
        'dst2 = task.palette.dst1
        label2 = CStr(masks.Count) + " Connected Components with size > " + CStr(minSize) + " pixels"

        Static saveMaskCount As Integer
        If saveMaskCount <> masks.Count Then
            If maskSlider.value >= masks.Count Then maskSlider.value = 0
            maskSlider.maximum = masks.Count - 1
            saveMaskCount = masks.Count
        End If
        If masks.Count > 0 And standalone Then
            dst2.SetTo(0)
            dst2(rects(maskSlider.value)) = masks(maskSlider.value)
        End If
    End Sub
End Class






Public Class CComp_Both : Inherits VBparent
    Dim above As New CComp_Basics
    Dim below As New CComp_Basics
    Public Sub New()
        label1 = "Connected components in the dark half of the image"
        label2 = "Connected components in the light half of the image"
        task.desc = "Prepare the connected components for both above and below the threshold"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("Threshold for grayscale input")
        thresholdSlider.value = 120
        below.Run(src)
        dst1 = below.dst2

        thresholdSlider.value = 130
        above.Run(src)
        dst2 = above.dst2
    End Sub
End Class







'https://github.com/oreillymedia/Learning-OpenCV-3_examples/blob/master/example_14-03.cpp
Public Class CComp_BasicsOld : Inherits VBparent
    Public connectedComponents As Object
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Dim mats As New Mat_4Click
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "CComp Min Area", 0, 10000, 500)
            sliders.setupTrackBar(1, "CComp Max Area", 0, dst1.Width * dst1.Height / 2, dst1.Width * dst1.Height / 4)
            sliders.setupTrackBar(2, "CComp threshold", 0, 255, 128)
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
    Public Sub Run(src As cv.Mat) ' Rank = 1
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
        If threshold < 128 Then
            mats.mat(0) = src.Threshold(threshold, 255, cv.ThresholdTypes.BinaryInv)
        Else
            mats.mat(0) = src.Threshold(threshold, 255, cv.ThresholdTypes.Binary)
        End If

        connectedComponents = cv.Cv2.ConnectedComponentsEx(mats.mat(0))
        connectedComponents.renderblobs(mats.mat(2))

        Dim count = renderBlobs(minSize, mats.mat(0), maxSize)
        cv.Cv2.BitwiseNot(mats.mat(0), mats.mat(1))

        connectedComponents = cv.Cv2.ConnectedComponentsEx(mats.mat(1))
        connectedComponents.renderblobs(mats.mat(3))

        count += renderBlobs(minSize, mats.mat(1), maxSize)
        label2 = CStr(count) + " items found > " + CStr(minSize) + " and < " + CStr(maxSize)
        connectedComponents.renderblobs(dst1)
        If standalone Then
            For i = 0 To centroids.Count - 1
                dst1.Circle(centroids.ElementAt(i), task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)
                dst1.Rectangle(rects.ElementAt(i), cv.Scalar.White, 2)
            Next
        End If

        mats.Run(src)
        dst1 = mats.dst1
        dst2 = mats.dst2
        label1 = ">Slider, <Slider, rendered >Slider, rendered <slider"
    End Sub
End Class




Public Class CComp_PointTracker : Inherits VBparent
    Public basics As New CComp_BasicsOld
    Public highlight As New Highlight_Basics
    Public trackPoints As Boolean = True
    Public Sub New()
        task.desc = "Track connected componenent centroids and use it to match coloring"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        basics.Run(src)

        If trackPoints Then
            Static topView As New PointCloud_TrackerTop
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





Public Class CComp_DepthEdges : Inherits VBparent
    Dim ccomp As New CComp_PointTracker
    Dim depth As New Depth_Edges
    Public Sub New()
        If check.Setup(caller, 1) Then
            check.Box(0).Text = "Use edge mask in connected components"
            check.Box(0).Checked = True
        End If

        task.desc = "Use depth edges to isolate connected components in depth"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        depth.Run(src)
        If standalone Or task.intermediateName = caller Then dst2 = depth.dst2

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
    Public Sub Run(src As cv.Mat) ' Rank = 1
        edges.Run(src)
        dst1 = edges.dst1

        ccomp.Run(src)
        dst2 = ccomp.dst1
    End Sub
End Class








Public Class CComp_ColorDepth : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Min Blob size", 0, 10000, 100)
        End If
        label1 = "Color by Mean Depth"
        label2 = "Binary image using threshold binary+Otsu"
        task.desc = "Color connected components based on their depth"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
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
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "InRange # of ranges", 2, 255, 15)
            sliders.setupTrackBar(1, "InRange min Blob Size (in pixels) X1000", 1, 100, 10)
        End If
        task.desc = "Connected components in specific ranges"
        label2 = "Blob rectangles - largest to smallest"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
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
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "InRange # of ranges", 1, 20, 15)
            sliders.setupTrackBar(1, "InRange min Blob Size (in pixels) X1000", 1, 100, 10)
        End If
        task.desc = "Connect components in specific ranges"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
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
    Public Sub Run(src As cv.Mat) ' Rank = 1
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
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "CComp Min Area", 0, 10000, 500)
            sliders.setupTrackBar(1, "CComp Max Area", 0, dst1.Width * dst1.Height / 2, dst1.Width * dst1.Height / 4)
            sliders.setupTrackBar(2, "CComp threshold", 0, 255, 50)
        End If

        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        task.desc = "Draw bounding boxes around RGB binarized connected Components"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
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
    Public Sub Run(src As cv.Mat) ' Rank = 1
        edges.Run(src)
        dst1 = edges.dst2
        ccomp.Run(dst1)
        dst2 = ccomp.dst2
    End Sub
End Class






'https://github.com/oreillymedia/Learning-OpenCV-3_examples/blob/master/example_14-03.cpp
Public Class CComp_GrayScale : Inherits VBparent
    Public rects As New List(Of cv.Rect)
    Public labels As New cv.Mat
    Dim vecColors(255) As cv.Vec3b
    Dim colorMap As cv.Mat
    Public Sub New()
        Dim msrng As New System.Random
        For i = 0 To vecColors.Length - 1
            vecColors(i) = New cv.Vec3b(msrng.Next(50, 255), msrng.Next(50, 255), msrng.Next(50, 255)) ' note: cannot generate black!
        Next
        If sliders.Setup(caller) Then sliders.setupTrackBar(0, "CComp Min Area", 0, 10000, 500)
        task.palette.Run(task.color)
        colorMap = task.palette.gradientColorMap.Row(0).Clone
        task.desc = "Isolate the full image using RGB binarized connected Components"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static areaSlider = findSlider("CComp Min Area")
        Dim minSize = areaSlider.value
        If src.Channels <> 1 Then
            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim meanScalar = cv.Cv2.Mean(src)
            dst1 = src.Threshold(meanScalar(0), 255, cv.ThresholdTypes.Otsu)
        Else
            dst1 = src
        End If
        Dim stats As New cv.Mat
        Dim centroids As New cv.Mat
        Dim nLabels = dst1.ConnectedComponentsWithStats(labels, stats, centroids)

        rects.Clear()
        Dim black = New cv.Vec3b(0, 0, 0)
        Dim colors As New List(Of cv.Vec3b)
        Dim index As New List(Of Integer)
        For i = 0 To Math.Min(256, stats.Rows) - 1
            Dim area = stats.Get(Of Integer)(i, 4)
            If area > minSize And area <> src.Total Then
                Dim r = stats.Get(Of cv.Rect)(i, 0)
                If r.Width <> dst1.Width And r.Height <> dst1.Height Then
                    rects.Add(r)
                    colors.Add(vecColors(colors.Count))
                    index.Add(i)
                End If
            End If
        Next

        ' this does not fix the color flashing problem but if the component count is the same (for the same areas) the colors will be stable.
        task.palette.gradientColorMap = colorMap.Clone
        For i = 0 To colors.Count - 1
            task.palette.gradientColorMap.Set(Of cv.Vec3b)(0, index(i), colors(i))
        Next

        labels.ConvertTo(labels, cv.MatType.CV_8U)
        task.palette.Run(labels)
        dst2 = task.palette.dst1
        label2 = CStr(nLabels) + " Connected Components found"
    End Sub
End Class