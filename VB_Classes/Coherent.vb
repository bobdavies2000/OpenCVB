Imports cv = OpenCvSharp
Public Class Coherent_Basics
    Inherits VBparent
    Dim flood As Coherent_FloodFill
    Dim pixel As Pixel_Sampler
    Dim palette As Palette_Basics
    Public Sub New()
        initParent()
        pixel = New Pixel_Sampler
        palette = New Palette_Basics
        flood = New Coherent_FloodFill
        dst1 = New cv.Mat(src.Size, cv.MatType.CV_8UC1, 0)
        task.desc = "Segment image with same values at the same locations"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        flood.src = input
        flood.Run()

        Dim sortedColor As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        For i = 0 To flood.lastPoints.Count - 1
            sortedColor.Add(input.Mean(flood.lastMasks(i)).Item(0), i)
        Next
        dst1 = dst1.Clone
        Dim incr = 255 / flood.lastPoints.Count
        For i = sortedColor.Count - 1 To 0 Step -1
            Dim index = sortedColor.ElementAt(i).Value
            Dim rect = flood.lastRects(index)
            Dim mask = flood.lastMasks(index)
            dst1(rect).SetTo(255 - i * incr, mask(rect))
        Next

        Static lastCount = flood.lastPoints.Count
        Static lastFrame = flood.dst1.Clone
        If flood.lastPoints.Count < lastCount Then
            lastFrame = flood.dst1.Clone
            dst1 = flood.dst1.Clone
        Else
            For i = 0 To flood.lastRects.Count - 1
                Dim rect = flood.lastRects(i)
                Dim mask = flood.lastMasks(i)(rect)
                pixel.src = lastFrame(rect).Clone
                Dim inverse = 255 - mask
                pixel.src.SetTo(0, inverse)
                pixel.Run()
                dst1(rect).SetTo(pixel.dominantGray, mask)
            Next
            lastFrame = dst1.Clone
        End If
        lastCount = flood.lastPoints.Count

        If standalone Then
            palette.src = dst1
            palette.Run()
            dst2 = palette.dst1
        End If
        label1 = CStr(flood.lastRects.Count) + " regions identified in the last frame"
    End Sub
End Class








Public Class Coherent_FloodFill
    Inherits VBparent
    Public basics As FloodFill_Basics
    Dim knn As KNN_1_to_1
    Public lastPoints As New List(Of cv.Point2f)
    Public lastMasks As New List(Of cv.Mat)
    Public lastRects As New List(Of cv.Rect)
    Public lastSizes As New List(Of Integer)
    Public Sub New()
        initParent()
        knn = New KNN_1_to_1
        basics = New FloodFill_Basics
        Dim thresholdSlider = findSlider("Threshold in camera motion in radians X100")
        thresholdSlider.Value = 2
        task.desc = "Floodfill an image and make the colors consistent."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static minSlider = findSlider("FloodFill Minimum Size")
        Dim threshold = minSlider.value

        basics.src = src
        basics.Run()

        Dim queryPoints = New List(Of cv.Point2f)
        For Each r In basics.rects
            queryPoints.Add(New cv.Point2f(r.X, r.Y))
        Next

        knn.basics.knnQT.queryPoints = New List(Of cv.Point2f)(queryPoints)
        knn.basics.knnQT.trainingPoints = New List(Of cv.Point2f)(lastPoints)
        knn.Run()

        If task.cameraStable = False Or task.frameCount = 0 Then
            dst1 = New cv.Mat(src.Size, cv.MatType.CV_8UC1, 0)
            lastPoints.Clear()
        End If

        If queryPoints.Count > lastPoints.Count Then
            lastPoints = New List(Of cv.Point2f)(queryPoints)
            lastMasks = New List(Of cv.Mat)(basics.masks)
            lastRects = New List(Of cv.Rect)(basics.rects)
            lastSizes = New List(Of Integer)(basics.maskSizes)
        End If

        dst1 = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)
        For i = 0 To knn.matchedPoints.Count - 1
            Dim pt = knn.matchedPoints(i)
            If pt.X >= 0 Then
                Dim index = lastPoints.IndexOf(pt)
                If index < 0 Then
                    If lastPoints.Contains(pt) = False Then
                        lastPoints.Add(pt)
                        lastRects.Add(basics.rects(i))
                        lastMasks.Add(basics.masks(i))
                        lastSizes.Add(basics.maskSizes(i))
                    End If
                Else
                    lastPoints(index) = pt
                    lastRects(index) = basics.rects(i)
                    lastMasks(index) = basics.masks(i)
                End If
            End If
        Next

        For i = 0 To knn.unmatchedPoints.Count - 1
            Dim pt = knn.unmatchedPoints(i)
            If lastPoints.Contains(pt) = False Then
                Dim index = queryPoints.IndexOf(pt)
                Dim rect = basics.rects(index)
                Dim mask = basics.masks(index)

                lastPoints.Add(pt)
                lastRects.Add(rect)
                lastMasks.Add(mask)
                lastSizes.Add(basics.maskSizes(i))
            End If
        Next

        Dim sortedMasks As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To lastPoints.Count - 1
            sortedMasks.Add(lastSizes(i), i)
        Next
        Dim incr = 255 / lastPoints.Count
        For i = 0 To sortedMasks.Count - 1
            Dim index = sortedMasks.ElementAt(i).Value
            Dim rect = lastRects(index)
            Dim mask = lastMasks(index)
            dst1(rect).SetTo(index * incr, mask(rect))
        Next

        label1 = CStr(lastPoints.Count) + " regions identified"
    End Sub
End Class






Public Class Coherent_Palette
    Inherits VBparent
    Public flood As Coherent_Pixel
    Public palette As Palette_Basics
    Public Sub New()
        initParent()
        palette = New Palette_Basics()
        palette.Run()

        flood = New Coherent_Pixel
        task.desc = "Highlight a consistent 8-bit grayscale image regions with a palette"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        flood.src = src
        flood.Run()

        palette.src = flood.dst1
        palette.Run()
        dst1 = palette.dst1
        label1 = flood.label1
    End Sub
End Class








Public Class Coherent_Pixel
    Inherits VBparent
    Public flood As FloodFill_Basics
    Dim pixel As Pixel_Sampler
    Public Sub New()
        initParent()
        flood = New FloodFill_Basics
        pixel = New Pixel_Sampler
        task.desc = "Floodfill an image and sample masks to make the colors consistent."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        flood.src = src
        flood.Run()

        Static lastFrame As cv.Mat
        If task.cameraStable = False Or task.frameCount = 0 Then
            dst1 = New cv.Mat(src.Size, cv.MatType.CV_8UC1, 0)
            lastFrame = flood.dst2.Clone
        End If
        For i = 0 To flood.rects.Count - 1
            Dim rect = flood.rects(i)
            Dim mask = flood.masks(i)(rect)
            pixel.src = lastFrame(rect).Clone
            Dim inverse = 255 - mask
            pixel.src.SetTo(0, inverse)
            pixel.Run()
            dst1(rect).SetTo(pixel.dominantGray, mask)
        Next
        label1 = CStr(flood.rects.Count) + " regions identified in the last frame"
        lastFrame = dst1.Clone
    End Sub
End Class