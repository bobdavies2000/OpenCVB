Imports cv = OpenCvSharp 
Public Class Coherent_Basics : Inherits VBparent
    Dim flood As New Coherent_FloodFill
    Dim pixel As Pixel_Sampler
    Public Sub New()
        pixel = New Pixel_Sampler
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC1, 0)
        task.desc = "Segment image with same values at the same locations"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        flood.RunClass(src)

        Static lastCount = flood.lastPoints.Count
        Static lastFrame = flood.dst2.Clone
        If flood.lastPoints.Count < lastCount Then
            lastFrame = flood.dst2.Clone
            dst2 = flood.dst2.Clone
        Else
            For i = 0 To flood.lastRects.Count - 1
                Dim rect = flood.lastRects(i)
                Dim mask = flood.lastMasks(i)(rect)
                pixel.RunClass(lastFrame(rect).Clone.setto(0, 255 - mask))
                dst2(rect).SetTo(pixel.dominantGray, mask)
            Next
            lastFrame = dst2.Clone
        End If
        lastCount = flood.lastPoints.Count

        If standalone Then
            task.palette.RunClass(dst2)
            dst3 = task.palette.dst2
        End If
        labels(2) = CStr(flood.lastRects.Count) + " regions identified in the last frame"
    End Sub
End Class








Public Class Coherent_FloodFill : Inherits VBparent
    Public basics As New FloodFill_Basics
    Dim knn As New KNN_1_to_1
    Public lastPoints As New List(Of cv.Point2f)
    Public lastMasks As New List(Of cv.Mat)
    Public lastRects As New List(Of cv.Rect)
    Public lastSizes As New List(Of Integer)
    Public Sub New()
        task.desc = "Floodfill an image and make the colors consistent."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static minSlider = findSlider("FloodFill Minimum Size")
        Dim threshold = minSlider.value

        basics.RunClass(src)

        Dim queryPoints = New List(Of cv.Point2f)
        For Each r In basics.rects
            queryPoints.Add(New cv.Point2f(r.X, r.Y))
        Next

        knn.basics.knnQT.queryPoints = New List(Of cv.Point2f)(queryPoints)
        knn.basics.knnQT.trainingPoints = New List(Of cv.Point2f)(lastPoints)
        knn.RunClass(src)

        If task.cameraStable = False Or task.frameCount = 0 Then
            dst2 = New cv.Mat(src.Size, cv.MatType.CV_8UC1, 0)
            lastPoints.Clear()
        End If

        If queryPoints.Count > lastPoints.Count Then
            lastPoints = New List(Of cv.Point2f)(queryPoints)
            lastMasks = New List(Of cv.Mat)(basics.masks)
            lastRects = New List(Of cv.Rect)(basics.rects)
            lastSizes = New List(Of Integer)(basics.maskSizes)
        End If

        dst2 = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)
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
            dst2(rect).SetTo(index * incr, mask(rect))
        Next

        labels(2) = CStr(lastPoints.Count) + " regions identified"
    End Sub
End Class






Public Class Coherent_Palette : Inherits VBparent
    Public flood As New Coherent_Pixel
    Public Sub New()
        task.desc = "Highlight a consistent 8-bit grayscale image regions with a palette"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        flood.RunClass(src)

        task.palette.RunClass(flood.dst2)
        dst2 = task.palette.dst2
        labels(2) = flood.labels(2)
    End Sub
End Class








Public Class Coherent_Pixel : Inherits VBparent
    Public flood As New FloodFill_Basics
    Dim pixel As Pixel_Sampler
    Public Sub New()
        pixel = New Pixel_Sampler
        task.desc = "Floodfill an image and sample masks to make the colors consistent."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        flood.RunClass(src)

        Static lastFrame As cv.Mat
        If task.cameraStable = False Or task.frameCount = 0 Then
            dst2 = New cv.Mat(src.Size, cv.MatType.CV_8UC1, 0)
            lastFrame = flood.dst3.Clone
        End If
        For i = 0 To flood.rects.Count - 1
            Dim rect = flood.rects(i)
            Dim mask = flood.masks(i)(rect)
            pixel.RunClass(lastFrame(rect).Clone.SetTo(0, 255 - mask))
            dst2(rect).SetTo(pixel.dominantGray, mask)
        Next
        labels(2) = CStr(flood.rects.Count) + " regions identified in the last frame"
        lastFrame = dst2.Clone
    End Sub
End Class
