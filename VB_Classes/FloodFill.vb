Imports cv = OpenCvSharp
Imports System.Threading
Public Class FloodFill_Basics
    Inherits VBparent
    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public floodPoints As New List(Of cv.Point)
    Public rejectedCentroids As New List(Of cv.Point2f)
    Public rejectedRects As New List(Of cv.Rect)

    Public initialMask As New cv.Mat
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 4)
            sliders.setupTrackBar(0, "FloodFill Minimum Size", 1, 5000, 2500)
            sliders.setupTrackBar(1, "FloodFill LoDiff", 0, 255, 25)
            sliders.setupTrackBar(2, "FloodFill HiDiff", 0, 255, 25)
            sliders.setupTrackBar(3, "Step Size", 1, src.Cols / 2, 10)
        End If
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U)
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        label1 = "Input image to floodfill"
        task.desc = "Use floodfill to build image segments in a grayscale image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static minSizeSlider = findSlider("FloodFill Minimum Size")
        Static loDiffSlider = findSlider("FloodFill LoDiff")
        Static hiDiffSlider = findSlider("FloodFill HiDiff")
        Static stepSlider = findSlider("Step Size")
        Dim minFloodSize = minSizeSlider.Value
        Dim loDiff = cv.Scalar.All(loDiffSlider.Value)
        Dim hiDiff = cv.Scalar.All(hiDiffSlider.Value)
        Dim stepSize = stepSlider.Value

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim maskPlus = New cv.Mat(New cv.Size(input.Width + 2, input.Height + 2), cv.MatType.CV_8UC1)
        Dim maskRect = New cv.Rect(1, 1, maskPlus.Width - 2, maskPlus.Height - 2)
        initialMask = input.EmptyClone().SetTo(0)

        masks.Clear()
        maskSizes.Clear()
        rects.Clear()
        centroids.Clear()
        rejectedCentroids.Clear()
        rejectedRects.Clear()

        maskPlus.SetTo(0)
        Dim ignoreMasks = initialMask.Clone()

        Dim gray = input.Clone()
        dst1.SetTo(0)
        dst2.SetTo(0)
        For y = 0 To gray.Height - 1 Step stepSize
            For x = 0 To gray.Width - 1 Step stepSize
                If gray.Get(Of Byte)(y, x) > 0 Then
                    Dim rect As New cv.Rect
                    Dim pt = New cv.Point(CInt(x), CInt(y))
                    Dim count = cv.Cv2.FloodFill(gray, maskPlus, pt, cv.Scalar.White, rect, loDiff, hiDiff, floodFlag Or (255 << 8))
                    If count > minFloodSize And count <> gray.Total Then
                        floodPoints.Add(pt)
                        masks.Add(maskPlus(maskRect).Clone().SetTo(0, ignoreMasks))
                        masks(masks.Count - 1).SetTo(0, initialMask) ' The initial mask is what should not be part of any mask.
                        maskSizes.Add(count, masks.Count - 1)
                        rects.Add(rect)
                        Dim m = cv.Cv2.Moments(maskPlus(rect), True)
                        Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
                        centroids.Add(centroid)
                        dst1.SetTo(255, masks(masks.Count - 1))
                    Else
                        rejectedRects.Add(rect)
                        rejectedCentroids.Add(New cv.Point2f(rect.X + rect.Width / 2, rect.Y + rect.Height / 2))
                    End If
                    ' Mask off any object that is too small or previously identified
                    cv.Cv2.BitwiseOr(ignoreMasks, maskPlus(maskRect), ignoreMasks)
                End If
            Next
        Next

        label2 = CStr(masks.Count) + " regions > " + CStr(minFloodSize) + " pixels"
    End Sub
End Class








Public Class FloodFill_Image
    Inherits VBparent
    Public basics As FloodFill_Basics

    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public rejectedCentroids As New List(Of cv.Point2f)
    Public rejectedRects As New List(Of cv.Rect)

    Public initialMask As New cv.Mat
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public Sub New()
        initParent()
        basics = New FloodFill_Basics
        label1 = "Input image to floodfill"
        task.desc = "Use floodfill to build image segments in a grayscale image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        basics.src = src.Clone
        basics.Run()
        dst1 = src

        maskSizes = New SortedList(Of Integer, Integer)(basics.maskSizes)
        rects = New List(Of cv.Rect)(basics.rects)
        masks = New List(Of cv.Mat)(basics.masks)
        centroids = New List(Of cv.Point2f)(basics.centroids)
        rejectedCentroids = New List(Of cv.Point2f)(basics.rejectedCentroids)
        rejectedRects = New List(Of cv.Rect)(basics.rejectedRects)
        initialMask = basics.initialMask.Clone
        floodFlag = basics.floodFlag

        dst2.SetTo(0)
        For i = 0 To basics.masks.Count - 1
            Dim maskIndex = basics.maskSizes.ElementAt(i).Value
            dst2.SetTo(task.scalarColors(i Mod 255), basics.masks(maskIndex))
        Next
        Static minSizeSlider = findSlider("FloodFill Minimum Size")
        label2 = CStr(basics.masks.Count) + " regions > " + CStr(minSizeSlider.value) + " pixels"
    End Sub
End Class









Public Class FloodFill_8Bit
    Inherits VBparent
    Public basics As FloodFill_Image
    Public palette As Palette_Basics
    Public allRegionMask As cv.Mat
    Public Sub New()
        initParent()
        palette = New Palette_Basics()
        palette.Run()

        basics = New FloodFill_Image()
        task.desc = "Create a floodfill image that is only 8-bit for use with a palette"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        basics.src = src
        basics.Run()

        dst2.SetTo(0)
        For i = 0 To basics.masks.Count - 1
            Dim maskIndex = basics.maskSizes.ElementAt(i).Value
            dst2.SetTo(cv.Scalar.All((i + 1) Mod 255), basics.masks(maskIndex))
        Next

        allRegionMask = If(dst2.Channels = 1, dst2, dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255))

        Dim incr = If(basics.masks.Count < 10, 25, 255 / basics.masks.Count)  'reduces flicker of slightly different colors
        palette.src = dst2 * cv.Scalar.All(incr) ' spread the colors 
        palette.Run()
        dst1.SetTo(0)
        palette.dst1.CopyTo(dst1, allRegionMask)

        Static minSizeSlider = findSlider("FloodFill Minimum Size")
        label2 = CStr(basics.masks.Count) + " regions > " + CStr(minSizeSlider.value) + " pixels"
        If standalone Or task.intermediateReview = caller Then dst2 = palette.gradMap.gradientColorMap.Resize(src.Size())
    End Sub
End Class






Public Class FloodFill_Top16_MT
    Inherits VBparent
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "FloodFill Minimum Size", 1, 5000, 2000)
            sliders.setupTrackBar(1, "FloodFill LoDiff", 1, 255, 5)
            sliders.setupTrackBar(2, "FloodFill HiDiff", 1, 255, 5)
        End If
        task.desc = "Use floodfill to build image segments with a grayscale image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim minFloodSize = sliders.trackbar(0).Value
        Dim loDiff = cv.Scalar.All(sliders.trackbar(1).Value)
        Dim hiDiff = cv.Scalar.All(sliders.trackbar(2).Value)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src.Clone()
        grid.Run()
        Parallel.ForEach(grid.roiList,
        Sub(roi)
            For y = roi.Y To roi.Y + roi.Height - 1
                For x = roi.X To roi.X + roi.Width - 1
                    Dim nextByte = src.Get(Of Byte)(y, x)
                    If nextByte <> 255 And nextByte > 0 Then
                        Dim count = cv.Cv2.FloodFill(src, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange)
                        If count > minFloodSize Then
                            count = cv.Cv2.FloodFill(dst1, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange)
                        End If
                    End If
                Next
            Next
        End Sub)
    End Sub
End Class




Public Class FloodFill_Color_MT
    Inherits VBparent
    Dim flood As FloodFill_Top16_MT
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        flood = New FloodFill_Top16_MT()

        task.desc = "Use floodfill to build image segments in an RGB image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim minFloodSize = flood.sliders.trackbar(0).Value
        Dim loDiff = cv.Scalar.All(flood.sliders.trackbar(1).Value)
        Dim hiDiff = cv.Scalar.All(flood.sliders.trackbar(2).Value)

        dst1 = src.Clone()
        grid.Run()
        Dim vec255 = New cv.Vec3b(255, 255, 255)
        Dim vec0 = New cv.Vec3b(0, 0, 0)
        Dim regionCount As Integer = 0
        Parallel.ForEach(grid.roiList,
        Sub(roi)
            For y = roi.Y To roi.Y + roi.Height - 1
                For x = roi.X To roi.X + roi.Width - 1
                    Dim vec = src.Get(Of cv.Vec3b)(y, x)
                    If vec <> vec255 And vec <> vec0 Then
                        Dim count = cv.Cv2.FloodFill(src, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange + cv.FloodFillFlags.Link4)
                        If count > minFloodSize Then
                            Interlocked.Increment(regionCount)
                            count = cv.Cv2.FloodFill(dst1, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange + cv.FloodFillFlags.Link4)
                        End If
                    End If
                Next
            Next
        End Sub)
        label1 = CStr(regionCount) + " regions were filled with Floodfill"
    End Sub
End Class




Public Class FloodFill_DCT
    Inherits VBparent
    Dim flood As FloodFill_Color_MT
    Dim dct As DCT_FeatureLess
    Public Sub New()
        initParent()
        flood = New FloodFill_Color_MT()

        dct = New DCT_FeatureLess()
        task.desc = "Find surfaces that lack any texture with DCT (highest frequency removed) and use floodfill to isolate those surfaces."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        dct.src = src
        dct.Run()

        flood.src = dct.dst2.Clone()
        flood.Run()
        dst1 = flood.dst1
    End Sub
End Class






Public Class FloodFill_CComp
    Inherits VBparent
    Dim ccomp As CComp_Basics
    Dim range As FloodFill_RelativeRange
    Public Sub New()
        initParent()

        ccomp = New CComp_Basics
        range = New FloodFill_RelativeRange

        label1 = "Input to Floodfill "
        task.desc = "Use Floodfill with the output of the connected components to stabilize the colors used."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        ccomp.src = src
        ccomp.Run()

        range.src = ccomp.dst1
        range.Run()
        dst1 = range.dst1
        dst2 = range.dst2
        Static minSlider = findSlider("FloodFill Minimum Size")
        label2 = CStr(ccomp.connectedComponents.blobs.length) + " blobs found. " + CStr(range.fBasics.rects.Count) + " were more than " +
                 CStr(minSlider.Value) + " pixels"
    End Sub
End Class





Public Class FloodFill_RelativeRange
    Inherits VBparent
    Public fBasics As FloodFill_Image
    Public Sub New()
        initParent()
        fBasics = New FloodFill_Image()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 3)
            check.Box(0).Text = "Use Fixed range - when off, it means use relative range "
            check.Box(0).Checked = True
            check.Box(1).Text = "Use 4 nearest pixels (Link4) - when off, it means use 8 nearest pixels (Link8)"
            check.Box(1).Checked = True ' link4 produces better results.
            check.Box(2).Text = "Use 'Mask Only'"
        End If
        label1 = "Input to floodfill basics"
        label2 = "Output of floodfill basics"
        task.desc = "Experiment with 'relative' range option to floodfill.  Compare to fixed range option."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        fBasics.floodFlag = 0
        If check.Box(0).Checked Then fBasics.floodFlag += cv.FloodFillFlags.FixedRange
        If check.Box(1).Checked Then fBasics.floodFlag += cv.FloodFillFlags.Link4 Else fBasics.floodFlag += cv.FloodFillFlags.Link8
        If check.Box(2).Checked Then fBasics.floodFlag += cv.FloodFillFlags.MaskOnly
        fBasics.src = src
        fBasics.Run()
        dst1 = src
        dst2 = fBasics.dst2
    End Sub
End Class







Public Class Floodfill_Objects
    Inherits VBparent
    Dim basics As FloodFill_Image
    Dim minSlider As Windows.Forms.TrackBar
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 1)
            sliders.setupTrackBar(0, "Desired number of objects", 1, 100, 30)
        End If
        basics = New FloodFill_Image()
        minSlider = findSlider("FloodFill Minimum Size")
        minSlider.Value = (src.Width Mod 100) * 25

        task.desc = "Use floodfill to identify the desired number of objects"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        basics.src = src
        basics.Run()
        dst1 = basics.dst2

        Static loDiffSlider = findSlider("FloodFill LoDiff")
        Static hiDiffSlider = findSlider("FloodFill HiDiff")
        Static stepSlider = findSlider("Step Size")

        label1 = CStr(basics.masks.Count) + " objects with more than " + CStr(minSlider.Value) + " bytes"
        Static lastSetting As Integer = loDiffSlider.Value
        If dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero() < 0.9 * basics.src.Total And minSlider.Value > 500 Then
            minSlider.Value -= 10
        Else
            If basics.masks.Count >= sliders.trackbar(0).Value Then
                If loDiffSlider.Value < loDiffSlider.Maximum Then loDiffSlider.Value += 1
                If hiDiffSlider.Value < hiDiffSlider.Maximum Then hiDiffSlider.Value += 1
            Else
                If loDiffSlider.Value > 1 Then
                    loDiffSlider.Value -= 1
                    hiDiffSlider.Value -= 1
                End If
            End If
            lastSetting = loDiffSlider.Value
        End If
    End Sub
End Class





Public Class FloodFill_WithDepth
    Inherits VBparent
    Dim range As FloodFill_RelativeRange
    Public Sub New()
        initParent()

        range = New FloodFill_RelativeRange()

        label1 = "Floodfill results after removing unknown depth"
        label2 = "Mask showing where depth data is missing"
        task.desc = "Floodfill only the areas where there is depth"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        range.src = src
        range.Run()
        dst2 = task.noDepthMask
        dst1 = range.dst2
        dst1.SetTo(0, dst2)
    End Sub
End Class






Public Class Floodfill_Identifiers
    Inherits VBparent
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public minFloodSize As Integer
    Public basics As FloodFill_Image
    Public Sub New()
        initParent()
        basics = New FloodFill_Image()
        label1 = "Input image to floodfill"
        task.desc = "Use floodfill on a projection to determine how many objects and where they are - needs more work"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static minSizeSlider = findSlider("FloodFill Minimum Size")
        Static loDiffSlider = findSlider("FloodFill LoDiff")
        Static hiDiffSlider = findSlider("FloodFill HiDiff")
        Static stepSlider = findSlider("Step Size")

        minFloodSize = minSizeSlider.Value
        Dim loDiff = cv.Scalar.All(loDiffSlider.Value)
        Dim hiDiff = cv.Scalar.All(hiDiffSlider.Value)
        Dim stepSize = stepSlider.Value

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src.Clone()
        Dim maskPlus = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8UC1)

        rects.Clear()
        centroids.Clear()
        masks.Clear()
        dst2.SetTo(0)
        cv.Cv2.BitwiseNot(src, src)
        For y = 0 To src.Height - 1 Step stepSize
            For x = 0 To src.Width - 1 Step stepSize
                If src.Get(Of Byte)(y, x) < 255 Then
                    Dim rect As New cv.Rect
                    maskPlus.SetTo(0)
                    Dim count = cv.Cv2.FloodFill(src, maskPlus, New cv.Point(CInt(x), CInt(y)), cv.Scalar.White, rect, loDiff, hiDiff, floodFlag Or (255 << 8))
                    If count > minFloodSize Then
                        rects.Add(rect)
                        masks.Add(maskPlus(rect).Clone())
                        Dim m = cv.Cv2.Moments(maskPlus(rect), True)
                        Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
                        centroids.Add(centroid)
                    End If
                End If
            Next
        Next

        label2 = CStr(rects.Count) + " regions > " + CStr(minFloodSize) + " pixels"

        For i = 0 To masks.Count - 1
            Dim rect = rects(i)
            dst2(rect).SetTo(task.scalarColors(i Mod 255), masks(i))
        Next
    End Sub
End Class






Public Class Floodfill_ColorObjects
    Inherits VBparent
    Public pFlood As Floodfill_Identifiers
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public Sub New()
        initParent()
        pFlood = New Floodfill_Identifiers()

        task.desc = "Use floodfill to identify each of the region candidates using only color."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        pFlood.src = src
        pFlood.Run()
        dst1 = pFlood.dst2.Clone

        masks = New List(Of cv.Mat)(pFlood.masks)
        rects = New List(Of cv.Rect)(pFlood.rects)
        centroids = New List(Of cv.Point2f)(pFlood.centroids)

        For i = 0 To pFlood.masks.Count - 1
            masks.Add(pFlood.masks(i))
            rects.Add(pFlood.rects(i))
            centroids.Add(pFlood.centroids(i))
        Next
    End Sub
End Class





Public Class FloodFill_PointTracker
    Inherits VBparent
    Dim pTrack As KNN_PointTracker
    Dim flood As FloodFill_8Bit
    Public Sub New()
        initParent()

        pTrack = New KNN_PointTracker()
        flood = New FloodFill_8Bit()

        label1 = "Point tracker output"
        task.desc = "Test the FloodFill output as input into the point tracker"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        flood.src = src
        flood.Run()
        dst2 = flood.dst1

        pTrack.queryPoints = flood.basics.centroids
        pTrack.queryRects = flood.basics.rects
        pTrack.queryMasks = flood.basics.masks
        pTrack.Run()

        label2 = CStr(pTrack.drawRC.viewObjects.Count) + " regions were found"
        dst1 = pTrack.dst1
    End Sub
End Class









Public Class FloodFill_Top16
    Inherits VBparent
    Public flood As FloodFill_Image

    Public thumbNails As New cv.Mat
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public Sub New()
        initParent()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Show (up to) the first 16 largest objects in view (in order of size)"
        End If

        flood = New FloodFill_Image()

        label1 = "Input image to floodfill"
        task.desc = "Use floodfill to build image segments in a grayscale image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        flood.src = src
        thumbNails = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
        Dim allSize = New cv.Size(thumbNails.Width / 4, thumbNails.Height / 4) ' show the first 16 masks
        flood.Run()

        dst1.SetTo(0)
        Dim thumbCount As Integer
        Dim allRect = New cv.Rect(0, 0, allSize.Width, allSize.Height)
        For i = 0 To flood.masks.Count - 1
            Dim maskIndex = flood.maskSizes.ElementAt(i).Value
            Dim nextColor = task.scalarColors(i Mod 255)
            dst1.SetTo(nextColor, flood.masks(maskIndex))
            If thumbCount < 16 Then
                thumbNails(allRect) = flood.masks(maskIndex).Resize(allSize).Threshold(0, 255, cv.ThresholdTypes.Binary)
                thumbNails.Rectangle(allRect, cv.Scalar.White, 1)
                allRect.X += allSize.Width
                If allRect.X >= thumbNails.Width Then
                    allRect.X = 0
                    allRect.Y += allSize.Height
                End If
                thumbCount += 1
            End If
        Next
        If check.Box(0).Checked Then dst1 = thumbNails.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Static minSizeSlider = findSlider("FloodFill Minimum Size")
        label1 = CStr(flood.masks.Count) + " regions > " + CStr(minSizeSlider.value) + " pixels"
    End Sub
End Class










Public Class FloodFill_Click
    Inherits VBparent
    Dim edges As Edges_BinarizedSobel
    Dim flood As FloodFill_Point
    Public Sub New()
        initParent()
        edges = New Edges_BinarizedSobel
        flood = New FloodFill_Point
        flood.pt = New cv.Point(msRNG.Next(0, dst1.Width - 1), msRNG.Next(0, dst1.Height - 1))
        label2 = "Click anywhere to floodfill that area"
        task.desc = "FloodFill where the mouse clicks"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If task.mouseClickFlag Then
            flood.pt = task.mouseClickPoint
            task.mouseClickFlag = False ' preempt any other uses
        End If

        edges.src = src
        edges.Run()
        dst1 = edges.dst2

        If flood.pt.X Or flood.pt.Y Then
            flood.src = dst1.Clone
            flood.Run()
            dst1.CopyTo(dst2)
            If flood.pixelCount > 0 Then dst2.SetTo(255, flood.dst2)
        End If
    End Sub
End Class






Public Class FloodFill_Point
    Inherits VBparent
    Public pixelCount As Integer
    Public rect As cv.Rect
    Dim edges As Edges_BinarizedSobel
    Public centroid As cv.Point2f
    Public initialMask As New cv.Mat
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public pt As cv.Point ' this is the floodfill point
    Public Sub New()
        initParent()
        If standalone Then
            edges = New Edges_BinarizedSobel
            label2 = "FloodFill_Point standalone just shows the edges"
        Else
            label2 = "Resulting mask from floodfill"
        End If
        label1 = "Input image to floodfill"
        task.desc = "Use floodfill at a single location in a grayscale image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        dst1 = src.Clone()
        If standalone Then
            pt = New cv.Point(msRNG.Next(0, dst1.Width - 1), msRNG.Next(0, dst1.Height - 1))
            edges.src = src
            edges.Run()
            dst1 = edges.mats.dst1
            dst2 = edges.mats.mat(quadrantIndex)
        Else
            Dim maskPlus = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8UC1, 0)
            Dim maskRect = New cv.Rect(1, 1, maskPlus.Width - 2, maskPlus.Height - 2)

            Dim zero = New cv.Scalar(0)
            pixelCount = cv.Cv2.FloodFill(dst1, maskPlus, New cv.Point(CInt(pt.X), CInt(pt.Y)), cv.Scalar.White, rect, zero, zero, floodFlag Or (255 << 8))
            dst2 = maskPlus(maskRect).Clone
            pixelCount = pixelCount
            Dim m = cv.Cv2.Moments(maskPlus(rect), True)
            centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
            label2 = CStr(pixelCount) + " pixels at point pt(x=" + CStr(pt.X) + ",y=" + CStr(pt.Y)
        End If
    End Sub
End Class








Public Class FloodFill_FullImage
    Inherits VBparent
    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public floodPoints As New List(Of cv.Point)
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public edges As Edges_BinarizedSobel
    Public edgeStd As Edges_Stdev
    Dim initialMask As New cv.Mat
    Dim palette As Palette_Basics
    Public motion As Motion_Basics
    Public mats As Mat_4Click
    Public missingSegments As cv.Mat
    Public Sub New()
        initParent()

        motion = New Motion_Basics
        mats = New Mat_4Click
        palette = New Palette_Basics
        edges = New Edges_BinarizedSobel
        edgeStd = New Edges_Stdev

        Dim paletteRadio = findRadio("Random - use slider to adjust")
        paletteRadio.Checked = True

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "FloodFill Step Size", 1, src.Cols / 2, 15)
            sliders.setupTrackBar(1, "FloodFill point distance from edge", 1, 25, 10)
            sliders.setupTrackBar(2, "Minimum length for missing contours", 3, 25, 4)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Use filter to remove low stdev areas"
        End If

        Dim paletteSlider = findSlider("Number of color transitions (Used only with Random)")
        paletteSlider.Value = 180 ' insures every region will be a significantly different color
        task.desc = "Floodfill each of the segments outlined by the Edges_BinarizedSobel algorithm"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static stepSlider = findSlider("FloodFill Step Size")
        Static fillSlider = findSlider("FloodFill point distance from edge")
        Dim fill = fillSlider.value
        Dim stepSize = stepSlider.Value

        Static saveStepSize As Integer
        Static saveFillDistance As Integer
        Dim resetColors As Boolean
        If saveStepSize <> stepSize Or saveFillDistance <> fill Then
            resetColors = True
            saveStepSize = stepSize
            saveFillDistance = fill
        End If

        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setMyActiveMat()


        motion.src = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        motion.Run()
        mats.mat(0) = motion.dst1

        masks.Clear()
        maskSizes.Clear()
        rects.Clear()
        centroids.Clear()

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static zero As New cv.Scalar(0)
        Static maskRect = New cv.Rect(1, 1, dst1.Width, dst1.Height)

        Static stdCheck = findCheckBox("Use filter to remove low stdev areas")
        Dim edgeOutput As cv.Mat
        If stdCheck.checked Then
            edgeStd.src = input
            edgeStd.Run()
            edgeOutput = edgeStd.dst1.Clone
        Else
            edges.src = input
            edges.Run()
            edgeOutput = edges.dst2.Clone
        End If
        dst1 = edgeOutput.Clone

        Dim maskPlus = New cv.Mat(New cv.Size(dst1.Width + 2, dst1.Height + 2), cv.MatType.CV_8UC1, 0)
        Dim rect As cv.Rect
        Dim pt As cv.Point
        Dim testCount As Integer
        Dim floodCount As Integer
        floodPoints.Clear()

        Dim inputRect As New cv.Rect(0, 0, fill, fill)
        Dim depthThreshold = fill * fill / 2
        Static lastFrame = dst1.Clone
        If motion.intersect.inputRects.Count > 0 Then
            If dst2.Channels = 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For Each r In motion.intersect.inputRects
                mats.mat(0).Rectangle(r, cv.Scalar.Yellow, 2)
                lastFrame(r).setto(0)
            Next
            For Each rect In motion.intersect.enclosingRects
                dst2.Rectangle(rect, cv.Scalar.Red, 2)
            Next
        End If
        For y = fill To dst1.Height - fill - 1 Step stepSize
            For x = fill To dst1.Width - fill - 1 Step stepSize
                testCount += 1
                inputRect.X = x
                inputRect.Y = y
                Dim edgeCount = dst1(inputRect).CountNonZero
                Dim depthCount = task.depth32f(inputRect).CountNonZero
                If edgeCount = 0 And depthCount > depthThreshold Then
                    floodCount += 1
                    pt.X = x + fill / 2
                    pt.Y = y + fill / 2
                    Dim colorIndex = lastFrame.Get(Of Byte)(pt.Y, pt.X)
                    If resetColors Or motion.resetAll Or colorIndex = 0 Then colorIndex = (255 - masks.Count - 1) Mod 255
                    Dim pixelCount = cv.Cv2.FloodFill(dst1, maskPlus, pt, cv.Scalar.All(colorIndex), rect, zero, zero, floodFlag Or (255 << 8))

                    If rect.Width And rect.Height Then
                        floodPoints.Add(pt)
                        Dim m = cv.Cv2.Moments(maskPlus(rect), True)
                        Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)

                        maskSizes.Add(pixelCount, masks.Count)
                        masks.Add(maskPlus(maskRect)(rect))
                        rects.Add(rect)
                        centroids.Add(centroid)
                    End If
                End If
            Next
        Next

        motion.resetAll = False
        lastFrame = dst1.Clone
        palette.src = dst1
        palette.Run()
        mats.mat(3) = palette.dst1
        mats.mat(3).SetTo(0, mats.mat(1)) ' show the pixels that are not assigned (missing)
        label2 = "Checked " + CStr(testCount) + " locations and used floodfill on " + CStr(floodCount)

        mats.mat(1) = dst1.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        missingSegments = mats.mat(1).Clone
        Dim missed = mats.mat(1).CountNonZero()
        mats.mat(1).SetTo(255, edgeOutput)
        Dim segmentedCount = src.Width * src.Height - missed
        Dim percentRGB = Format(segmentedCount / (src.Width * src.Height), "#0%")
        label1 = "Segmented pixels = " + Format(segmentedCount, "###,###") + " or " + percentRGB + " of total pixels"

        dst1 = edgeOutput.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each pt In floodPoints
            dst1.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Next
        mats.mat(2) = dst1

        mats.Run()
        dst1 = mats.dst1
        dst2 = mats.mat(quadrantIndex)
    End Sub
End Class










Public Class FloodFill_Step
    Inherits VBparent
    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public floodPoints As New List(Of cv.Point)
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Dim initialMask As New cv.Mat
    Dim contours As Contours_Binarized
    Dim edgesInput As cv.Mat
    Dim contourInput As New SortedList(Of Integer, cv.Point())(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        initParent()

        contours = New Contours_Binarized
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "FloodFill Step Size", 1, src.Cols / 2, 15)
            sliders.setupTrackBar(1, "FloodFill point distance from edge", 1, 25, 10)
            sliders.setupTrackBar(2, "Minimum length for missing contours", 3, 25, 4)
        End If

        task.desc = "Step through the current image to floodfill using colors from the previous image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone Then
            contours.src = src
            contours.Run()
            contourInput = contours.basics.sortedContours
            dst1 = contours.dst1
            src = contours.dst2
        End If

        Static stepSlider = findSlider("FloodFill Step Size")
        Static fillSlider = findSlider("FloodFill point distance from edge")
        Dim fill = fillSlider.value
        Dim stepSize = stepSlider.Value

        Static saveStepSize As Integer
        Static saveFillDistance As Integer
        Dim resetColors As Boolean
        If saveStepSize <> stepSize Or saveFillDistance <> fill Then
            resetColors = True
            saveStepSize = stepSize
            saveFillDistance = fill
        End If

        masks.Clear()
        maskSizes.Clear()
        rects.Clear()
        centroids.Clear()

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static zero As New cv.Scalar(0)
        Static maskRect = New cv.Rect(1, 1, dst1.Width, dst1.Height)

        Dim maskPlus = New cv.Mat(New cv.Size(dst1.Width + 2, dst1.Height + 2), cv.MatType.CV_8UC1, 0)
        Dim rect As cv.Rect
        Dim pt As cv.Point
        Dim testCount As Integer
        Dim floodCount As Integer
        floodPoints.Clear()

        Static lastFrame As cv.Mat = src.Clone
        dst2 = src.Clone
        Dim inputRect As New cv.Rect(0, 0, fill, fill)
        For y = fill To dst1.Height - fill - 1 Step stepSize
            For x = fill To dst1.Width - fill - 1 Step stepSize
                testCount += 1
                inputRect.X = x
                inputRect.Y = y
                Dim edgeCount = dst1(inputRect).CountNonZero
                If edgeCount = 0 Then
                    floodCount += 1
                    pt.X = x + fill / 2
                    pt.Y = y + fill / 2
                    Dim c = lastFrame.Get(Of cv.Vec3b)(pt.Y, pt.X)
                    If c <> New cv.Vec3b Then
                        Dim nextColor = New cv.Scalar(c.Item0, c.Item1, c.Item2)
                        If resetColors Or c = New cv.Vec3b Then nextColor = task.scalarColors(x Mod 255)
                        Dim pixelCount = cv.Cv2.FloodFill(dst2, maskPlus, pt, nextColor, rect, zero, zero, floodFlag Or (255 << 8))

                        If rect.Width And rect.Height Then
                            floodPoints.Add(pt)
                            Dim m = cv.Cv2.Moments(maskPlus(rect), True)
                            Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)

                            maskSizes.Add(pixelCount, masks.Count)
                            masks.Add(maskPlus(maskRect)(rect))
                            rects.Add(rect)
                            centroids.Add(centroid)

                            dst2.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1)
                        End If
                    End If
                End If
            Next
        Next

        lastFrame = src.Clone
    End Sub
End Class