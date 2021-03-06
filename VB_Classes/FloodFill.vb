Imports cv = OpenCvSharp
Imports System.Threading
Public Class FloodFill_Basics : Inherits VBparent
    Public maskSizes As New List(Of Integer)
    Public rects As New List(Of cv.Rect)
    Dim floodPoints As New List(Of cv.Point)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public floodFlag As Integer = cv.FloodFillFlags.FixedRange Or (255 << 8)
    Dim alreadyFlooded As cv.Mat
    Public selectedIndex As Integer
    Dim selectedRect As cv.Rect
    Dim maskPlus As cv.Mat
    Dim maskRect As cv.Rect
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "FloodFill Minimum Size", 1, 5000, 2500)
            sliders.setupTrackBar(1, "FloodFill LoDiff", 0, 255, 25)
            sliders.setupTrackBar(2, "FloodFill HiDiff", 0, 255, 25)
            sliders.setupTrackBar(3, "Step Size", 1, dst2.Cols / 2, 30)
        End If

        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U)
        labels(3) = "Palette output"
        task.desc = "Use floodfill to build image segments in a grayscale image."
    End Sub
    Private Sub findSelectedRegion()
        Static mousePoint = New cv.Point(msRNG.Next(0, dst1.Width / 2), msRNG.Next(0, dst1.Height))
        If task.mouseClickFlag Then
            mousePoint = task.mouseClickPoint
            selectedIndex = dst1.Get(Of Byte)(mousePoint.Y, mousePoint.X)
        End If

        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        Dim sample = dst1.Get(Of Byte)(mousePoint.Y, mousePoint.X)
        If rects.Count > 0 Then
            selectedIndex = If(sample = 0, -1, sample)
            If selectedIndex > 0 Then
                Dim r = rects(selectedIndex)
                dst3(r).SetTo(255, masks(selectedIndex))
                selectedRect = r
            Else
                task.drawRectClear = True
            End If
        End If
    End Sub
    Private Sub addRegion(pt As cv.Point, rect As cv.Rect, count As Integer)
        floodPoints.Add(pt)
        Dim mask = maskPlus(maskRect).Clone().SetTo(0, alreadyFlooded)(rect)
        dst1(rect).SetTo(masks.Count Mod 255, mask)

        maskSizes.Add(count)
        masks.Add(mask)
        rects.Add(rect)
        Dim m = cv.Cv2.Moments(mask, True)
        If m.M00 = 0 Or m.M01 = 0 Then
            centroids.Add(New cv.Point2f(0, 0))
        Else
            Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
            centroids.Add(centroid)
        End If
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 5
        Static minSizeSlider = findSlider("FloodFill Minimum Size")
        Static loDiffSlider = findSlider("FloodFill LoDiff")
        Static hiDiffSlider = findSlider("FloodFill HiDiff")
        Static stepSlider = findSlider("Step Size")
        Dim minFloodSize = minSizeSlider.Value
        Dim stepSize = stepSlider.Value
        Dim loDiff = cv.Scalar.All(loDiffSlider.Value)
        Dim hiDiff = cv.Scalar.All(hiDiffSlider.Value)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        maskPlus = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8UC1, 0)
        maskRect = New cv.Rect(1, 1, maskPlus.Width - 2, maskPlus.Height - 2)
        Dim gray = src.Clone()
        Dim rect As New cv.Rect
        masks.Clear()
        maskSizes.Clear()
        rects.Clear()
        floodPoints.Clear()
        centroids.Clear()
        dst1.SetTo(0)
        addRegion(New cv.Point(0, 0), New cv.Rect(0, 0, 20, 20), 400) ' fake region to get away from zero...
        alreadyFlooded = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)

        For y = stepSize To gray.Height - 1 Step stepSize
            For x = stepSize To gray.Width - stepSize - 1 Step stepSize
                If gray.Get(Of Byte)(y, x) > 0 Then
                    Dim pt = New cv.Point(CInt(x), CInt(y))
                    Dim count = cv.Cv2.FloodFill(gray, maskPlus, pt, cv.Scalar.White, rect, loDiff, hiDiff, floodFlag)
                    If count > minFloodSize Then addRegion(pt, rect, count)
                    ' Mask off any object that is too small or previously identified
                    cv.Cv2.BitwiseOr(alreadyFlooded, maskPlus(maskRect), alreadyFlooded)
                End If
            Next
        Next
        dst2 = dst1 * 255 / masks.Count
        If standalone Then
            task.palette.RunClass(dst2)
            dst3 = task.palette.dst2
            dst2 = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            findSelectedRegion()
        End If
        labels(2) = CStr(masks.Count) + " regions > " + CStr(minFloodSize) + " pixels"
    End Sub
End Class





Public Class FloodFill_WithDepth : Inherits VBparent
    Dim range As New FloodFill_RelativeRange
    Public Sub New()
        labels(2) = "Floodfill results after removing unknown depth"
        labels(3) = "Mask showing where depth data is missing"
        task.desc = "Floodfill only the areas where there is depth"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        range.RunClass(src)
        dst3 = task.noDepthMask
        dst2 = range.dst3
        dst2.SetTo(0, dst3)
    End Sub
End Class






'Public Class Floodfill_Identifiers : Inherits VBparent
'    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
'    Public rects As New List(Of cv.Rect)
'    Public masks As New List(Of cv.Mat)
'    Public centroids As New List(Of cv.Point2f)
'    Public minFloodSize As Integer
'    Public basics As New FloodFill_Basics
'    Public Sub New()
'        labels(2) = "Input image to floodfill"
'        task.desc = "Use floodfill on a projection to determine how many objects and where they are - needs more work"
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        Static minSizeSlider = findSlider("FloodFill Minimum Size")
'        Static loDiffSlider = findSlider("FloodFill LoDiff")
'        Static hiDiffSlider = findSlider("FloodFill HiDiff")
'        Static stepSlider = findSlider("Step Size")

'        minFloodSize = minSizeSlider.Value
'        Dim loDiff = cv.Scalar.All(loDiffSlider.Value)
'        Dim hiDiff = cv.Scalar.All(hiDiffSlider.Value)
'        Dim stepSize = stepSlider.Value

'        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        dst2 = src.Clone()
'        Dim maskPlus = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8UC1)

'        rects.Clear()
'        centroids.Clear()
'        masks.Clear()
'        cv.Cv2.BitwiseNot(src, src)
'        For y = 0 To src.Height - 1 Step stepSize
'            For x = 0 To src.Width - 1 Step stepSize
'                If src.Get(Of Byte)(y, x) < 255 Then
'                    Dim rect As New cv.Rect
'                    maskPlus.SetTo(0)
'                    Dim count = cv.Cv2.FloodFill(src, maskPlus, New cv.Point(CInt(x), CInt(y)), cv.Scalar.White, rect, loDiff, hiDiff, floodFlag Or (255 << 8))
'                    If count > minFloodSize Then
'                        rects.Add(rect)
'                        masks.Add(maskPlus(rect).Clone())
'                        Dim m = cv.Cv2.Moments(maskPlus(rect), True)
'                        Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
'                        centroids.Add(centroid)
'                    End If
'                End If
'            Next
'        Next

'        labels(3) = CStr(rects.Count) + " regions > " + CStr(minFloodSize) + " pixels"

'        dst3.SetTo(0)
'        For i = 0 To masks.Count - 1
'            Dim rect = rects(i)
'            If rect.Width = dst3.Width And rect.Height = dst3.Height Then Continue For
'            dst3(rect).SetTo(task.scalarColors(i Mod 255), masks(i))
'        Next
'    End Sub
'End Class






'Public Class Floodfill_ColorObjects : Inherits VBparent
'    Public pFlood As New Floodfill_Identifiers
'    Public rects As New List(Of cv.Rect)
'    Public masks As New List(Of cv.Mat)
'    Public centroids As New List(Of cv.Point2f)
'    Public Sub New()
'        task.desc = "Use floodfill to identify each of the region candidates using only color."
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        pFlood.RunClass(src)
'        dst2 = pFlood.dst3.Clone

'        masks = New List(Of cv.Mat)(pFlood.masks)
'        rects = New List(Of cv.Rect)(pFlood.rects)
'        centroids = New List(Of cv.Point2f)(pFlood.centroids)

'        For i = 0 To pFlood.masks.Count - 1
'            masks.Add(pFlood.masks(i))
'            rects.Add(pFlood.rects(i))
'            centroids.Add(pFlood.centroids(i))
'        Next
'    End Sub
'End Class





'Public Class FloodFill_PointTracker : Inherits VBparent
'    Dim pTrack As New KNN_PointTracker
'    Dim flood As New FloodFill_Palette
'    Public Sub New()
'        labels(2) = "Point tracker output"
'        task.desc = "Test the FloodFill output as input into the point tracker"
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        flood.RunClass(src)
'        dst3 = flood.dst2

'        pTrack.queryPoints = flood.basics.centroids
'        pTrack.queryRects = flood.basics.rects
'        pTrack.queryMasks = flood.basics.masks
'        pTrack.RunClass(src)

'        labels(3) = CStr(pTrack.drawRC.viewObjects.Count) + " regions were found"
'        dst2 = pTrack.dst2
'    End Sub
'End Class









'Public Class FloodFill_Top16 : Inherits VBparent
'    Public flood As New FloodFill_Basics

'    Public thumbNails As New cv.Mat
'    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
'    Public Sub New()
'        If check.Setup(caller, 1) Then
'            check.Box(0).Text = "Show (up to) the first 16 largest objects in view (in order of size)"
'        End If

'        labels(2) = "Input image to floodfill"
'        task.desc = "Use floodfill to build image segments in a grayscale image."
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        Static minSizeSlider = findSlider("FloodFill Minimum Size")
'        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

'        thumbNails = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
'        Dim allSize = New cv.Size(thumbNails.Width / 4, thumbNails.Height / 4) ' show the first 16 masks
'        flood.RunClass(src)

'        dst2.SetTo(0)
'        Dim thumbCount As Integer
'        Dim allRect = New cv.Rect(0, 0, allSize.Width, allSize.Height)
'        For i = 0 To flood.masks.Count - 1
'            Dim maskIndex = flood.sortedSizes.ElementAt(i).Value
'            Dim nextColor = task.scalarColors(i Mod 255)
'            dst2.SetTo(nextColor, flood.masks(maskIndex))
'            If thumbCount < 16 Then
'                thumbNails(allRect) = flood.masks(maskIndex).Resize(allSize).Threshold(0, 255, cv.ThresholdTypes.Binary)
'                thumbNails.Rectangle(allRect, cv.Scalar.White, 1)
'                allRect.X += allSize.Width
'                If allRect.X >= thumbNails.Width Then
'                    allRect.X = 0
'                    allRect.Y += allSize.Height
'                End If
'                thumbCount += 1
'            End If
'        Next
'        If check.Box(0).Checked Then dst2 = thumbNails.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
'        labels(2) = CStr(flood.masks.Count) + " regions > " + CStr(minSizeSlider.value) + " pixels"
'    End Sub
'End Class









'Public Class FloodFill_FullImage : Inherits VBparent
'    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
'    Public rects As New List(Of cv.Rect)
'    Public masks As New List(Of cv.Mat)
'    Public centroids As New List(Of cv.Point2f)
'    Public floodPoints As New List(Of cv.Point)
'    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
'    Public edges As New Edges_BinarizedSobel
'    Dim initialMask As New cv.Mat
'    Dim palette As New Palette_RandomColorMap
'    Public motion As New Motion_Basics
'    Public mats As New Mat_4Click
'    Public missingSegments As cv.Mat
'    Public Sub New()
'        If sliders.Setup(caller) Then
'            sliders.setupTrackBar(0, "FloodFill Step Size", 1, dst2.Cols / 2, 15)
'            sliders.setupTrackBar(1, "FloodFill point distance from edge", 1, 25, 10)
'            sliders.setupTrackBar(2, "Minimum length for missing contours", 3, 25, 4)
'        End If

'        If check.Setup(caller, 1) Then
'            check.Box(0).Text = "Use filter to remove low stdev areas"
'        End If

'        task.desc = "Floodfill each of the segments outlined by the Edges_BinarizedSobel algorithm"
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        Static stepSlider = findSlider("FloodFill Step Size")
'        Static fillSlider = findSlider("FloodFill point distance from edge")
'        Dim fill = fillSlider.value
'        Dim stepSize = stepSlider.Value

'        Static saveStepSize As Integer
'        Static saveFillDistance As Integer
'        Dim resetColors As Boolean
'        If saveStepSize <> stepSize Or saveFillDistance <> fill Then
'            resetColors = True
'            saveStepSize = stepSize
'            saveFillDistance = fill
'        End If

'        If task.mouseClickFlag And task.mousePicTag = RESULT_DST2 Then setMyActiveMat()

'        motion.RunClass(task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
'        mats.mat(0) = motion.dst2

'        masks.Clear()
'        maskSizes.Clear()
'        rects.Clear()
'        centroids.Clear()

'        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        Static zero As New cv.Scalar(0)
'        Static maskRect = New cv.Rect(1, 1, dst2.Width, dst2.Height)

'        edges.RunClass(src)
'        dst2 = edges.dst3.Clone
'        mats.mat(1).SetTo(255, dst2.Clone)

'        Dim maskPlus = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8UC1, 0)
'        Dim rect As cv.Rect
'        Dim pt As cv.Point
'        Dim testCount As Integer
'        Dim floodCount As Integer
'        floodPoints.Clear()

'        Dim inputRect As New cv.Rect(0, 0, fill, fill)
'        Dim depthThreshold = fill * fill / 2
'        Static lastFrame = dst2.Clone
'        If motion.intersect.inputRects.Count > 0 Then
'            If dst3.Channels = 1 Then dst3 = dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
'            For Each r In motion.intersect.inputRects
'                mats.mat(0).Rectangle(r, cv.Scalar.Yellow, 2)
'                lastFrame(r).setto(0)
'            Next
'            For Each rect In motion.intersect.enclosingRects
'                dst3.Rectangle(rect, cv.Scalar.Red, 2)
'            Next
'        End If
'        Dim index As Integer
'        For y = fill To dst2.Height - fill - 1 Step stepSize
'            For x = fill To dst2.Width - fill - 1 Step stepSize
'                testCount += 1
'                inputRect.X = x
'                inputRect.Y = y
'                floodCount += 1
'                pt.X = x + fill / 2
'                pt.Y = y + fill / 2
'                index += 1
'                Dim pixelCount = cv.Cv2.FloodFill(dst2, maskPlus, pt, cv.Scalar.All(index), rect, zero, zero, floodFlag Or (255 << 8))

'                If rect.Width And rect.Height Then
'                    floodPoints.Add(pt)
'                    Dim m = cv.Cv2.Moments(maskPlus(rect), True)
'                    Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)

'                    maskSizes.Add(pixelCount, masks.Count)
'                    masks.Add(maskPlus(maskRect)(rect))
'                    rects.Add(rect)
'                    centroids.Add(centroid)
'                End If
'            Next
'        Next

'        motion.resetAll = False
'        lastFrame = dst2.Clone
'        task.palette.RunClass(dst2)
'        mats.mat(3) = task.palette.dst2
'        mats.mat(3).SetTo(0, mats.mat(1)) ' show the pixels that are not assigned (missing)
'        labels(3) = "Checked " + CStr(testCount) + " locations and used floodfill on " + CStr(floodCount)

'        missingSegments = dst2.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
'        Dim missed = mats.mat(1).CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero()
'        Dim segmentedCount = src.Width * src.Height - missed
'        Dim percentRGB = Format(segmentedCount / (src.Width * src.Height), "#0%")
'        labels(2) = "Segmented pixels = " + Format(segmentedCount, "###,###") + " or " + percentRGB + " of total pixels"

'        mats.mat(2) = mats.mat(1).Clone
'        For Each pt In floodPoints
'            mats.mat(2).Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
'        Next

'        mats.RunClass(src)
'        dst2 = mats.dst2
'        dst3 = mats.mat(quadrantIndex)
'    End Sub
'End Class










'Public Class FloodFill_Step : Inherits VBparent
'    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
'    Public rects As New List(Of cv.Rect)
'    Public masks As New List(Of cv.Mat)
'    Public centroids As New List(Of cv.Point2f)
'    Public floodPoints As New List(Of cv.Point)
'    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
'    Dim initialMask As New cv.Mat
'    Dim contours As New Contours_Binarized
'    Dim edgesInput As cv.Mat
'    Dim contourInput As New SortedList(Of Integer, cv.Point())(New compareAllowIdenticalIntegerInverted)
'    Public Sub New()
'        If sliders.Setup(caller) Then
'            sliders.setupTrackBar(0, "FloodFill Step Size", 1, dst2.Width / 2, 15)
'            sliders.setupTrackBar(1, "FloodFill point distance from edge", 1, 25, 10)
'            sliders.setupTrackBar(2, "Minimum length for missing contours", 3, 25, 4)
'        End If

'        task.desc = "Step through the current image to floodfill using colors from the previous image"
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        Static stepSlider = findSlider("FloodFill Step Size")
'        Static fillSlider = findSlider("FloodFill point distance from edge")
'        Dim fill = fillSlider.value
'        Dim stepSize = stepSlider.Value

'        If standalone Then
'            contours.RunClass(src)
'            contourInput = contours.basics.sortedContours
'            dst2 = contours.dst2
'            src = contours.dst3
'        End If

'        Static saveStepSize As Integer
'        Static saveFillDistance As Integer
'        Dim resetColors As Boolean
'        If saveStepSize <> stepSize Or saveFillDistance <> fill Then
'            resetColors = True
'            saveStepSize = stepSize
'            saveFillDistance = fill
'        End If

'        masks.Clear()
'        maskSizes.Clear()
'        rects.Clear()
'        centroids.Clear()

'        Dim input = src
'        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

'        Static zero As New cv.Scalar(0)
'        Static maskRect = New cv.Rect(1, 1, dst2.Width, dst2.Height)

'        Dim maskPlus = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8UC1, 0)
'        Dim rect As cv.Rect
'        Dim pt As cv.Point
'        Dim testCount As Integer
'        Dim floodCount As Integer
'        floodPoints.Clear()

'        Static lastFrame As cv.Mat = src.Clone
'        dst3 = src.Clone
'        Dim inputRect As New cv.Rect(0, 0, fill, fill)
'        For y = fill To dst2.Height - fill - 1 Step stepSize
'            For x = fill To dst2.Width - fill - 1 Step stepSize
'                testCount += 1
'                inputRect.X = x
'                inputRect.Y = y
'                Dim edgeCount = dst2(inputRect).CountNonZero
'                If edgeCount = 0 Then
'                    floodCount += 1
'                    pt.X = x + fill / 2
'                    pt.Y = y + fill / 2
'                    Dim c = lastFrame.Get(Of cv.Vec3b)(pt.Y, pt.X)
'                    If c <> New cv.Vec3b Then
'                        Dim nextColor = New cv.Scalar(c.Item0, c.Item1, c.Item2)
'                        If resetColors Or c = New cv.Vec3b Then nextColor = task.scalarColors(x Mod 255)
'                        Dim pixelCount = cv.Cv2.FloodFill(dst3, maskPlus, pt, nextColor, rect, zero, zero, floodFlag Or (255 << 8))

'                        If rect.Width And rect.Height Then
'                            floodPoints.Add(pt)
'                            Dim m = cv.Cv2.Moments(maskPlus(rect), True)
'                            Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)

'                            maskSizes.Add(pixelCount, masks.Count)
'                            masks.Add(maskPlus(maskRect)(rect))
'                            rects.Add(rect)
'                            centroids.Add(centroid)

'                            dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
'                        End If
'                    End If
'                End If
'            Next
'        Next

'        lastFrame = src.Clone
'    End Sub
'End Class





'Public Class FloodFill_Neighbors : Inherits VBparent
'    Public basics As New FloodFill_Basics
'    Public initialMask As New cv.Mat
'    Public rangeColors As New List(Of Integer)
'    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
'    Dim loDiff = cv.Scalar.All(1)
'    Dim hiDiff = cv.Scalar.All(1)
'    Public Sub New()
'        If standalone Then
'            loDiff = cv.Scalar.All(10)
'            hiDiff = cv.Scalar.All(10)
'        End If
'        labels(2) = "Grayscale version"
'        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
'        task.desc = "Use floodfill to combine neighboring labels - regions that differ by only 1"
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        Static minSizeSlider = findSlider("FloodFill Minimum Size")
'        Static stepSlider = findSlider("Step Size")
'        Dim minFloodSize = minSizeSlider.Value
'        Dim stepSize = stepSlider.Value

'        dst2 = src
'        If dst2.Channels <> 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        Dim maskPlus = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8UC1)
'        Dim maskRect = New cv.Rect(1, 1, maskPlus.Width - 2, maskPlus.Height - 2)
'        initialMask = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)

'        basics.masks.Clear()
'        basics.sortedSizes.Clear()
'        basics.rects.Clear()
'        basics.centroids.Clear()
'        rangeColors.Clear()

'        maskPlus.SetTo(0)
'        Dim ignoreMasks = initialMask.Clone()

'        dst3.SetTo(0)
'        For y = 0 To dst2.Height - 1 Step stepSize
'            For x = 0 To dst2.Width - 1 Step stepSize
'                If ignoreMasks.Get(Of Byte)(y, x) = 0 Then
'                    Dim rect As New cv.Rect
'                    Dim pt = New cv.Point(CInt(x), CInt(y))
'                    Dim labelID = dst2.Get(Of Byte)(pt.Y, pt.X)
'                    If labelID = 0 Then Continue For ' skip where depth is absent.
'                    Dim count = cv.Cv2.FloodFill(dst2, maskPlus, pt, labelID, rect, loDiff, hiDiff, floodFlag Or (255 << 8))
'                    If count > minFloodSize And count <> dst2.Total Then
'                        basics.floodPoints.Add(pt)
'                        basics.masks.Add(maskPlus(maskRect).Clone().SetTo(0, ignoreMasks))
'                        Dim i = basics.masks.Count - 1
'                        basics.masks(i).SetTo(0, initialMask) ' The initial mask is what should not be part of any mask.
'                        basics.sortedSizes.Add(count, i)
'                        basics.maskSizes.Add(count)
'                        basics.rects.Add(rect)
'                        Dim m = cv.Cv2.Moments(maskPlus(rect), True)
'                        Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
'                        basics.centroids.Add(centroid)
'                        dst3.SetTo(labelID, basics.masks(i))
'                        If rangeColors.Contains(labelID) = False Then rangeColors.Add(labelID)
'                    End If
'                    ' Mask off any object that is too small or previously identified
'                    cv.Cv2.BitwiseOr(ignoreMasks, maskPlus(maskRect), ignoreMasks)
'                End If
'            Next
'        Next

'        Dim spread = If(rangeColors.Count > 0, CInt(255 / rangeColors.Count), 255)
'        task.palette.RunClass(dst3 * spread)
'        dst2 = task.palette.dst2
'        labels(3) = CStr(basics.masks.Count) + " regions > " + CStr(minFloodSize) + " pixels"
'    End Sub
'End Class






Public Class FloodFill_RelativeRange : Inherits VBparent
    Public fBasics As New FloodFill_Basics
    Public Sub New()
        If check.Setup(caller, 3) Then
            check.Box(0).Text = "Use Fixed range - when off, it means use relative range "
            check.Box(0).Checked = True
            check.Box(1).Text = "Use 4 nearest pixels (Link4) - when off, it means use 8 nearest pixels (Link8)"
            check.Box(1).Checked = True ' link4 produces better results.
            check.Box(2).Text = "Use 'Mask Only'"
        End If
        labels(2) = "Output of floodfill basics"
        task.desc = "Experiment with 'relative' range option to floodfill.  Compare to fixed range option."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        fBasics.floodFlag = 0
        If check.Box(0).Checked Then fBasics.floodFlag += cv.FloodFillFlags.FixedRange
        If check.Box(1).Checked Then fBasics.floodFlag += cv.FloodFillFlags.Link4 Else fBasics.floodFlag += cv.FloodFillFlags.Link8
        If check.Box(2).Checked Then fBasics.floodFlag += cv.FloodFillFlags.MaskOnly
        fBasics.RunClass(src)
        dst2 = fBasics.dst2
    End Sub
End Class






Public Class FloodFill_Point : Inherits VBparent
    Public pixelCount As Integer
    Public rect As cv.Rect
    Dim edges As New Edges_BinarizedSobel
    Public centroid As cv.Point2f
    Public initialMask As New cv.Mat
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public pt As cv.Point ' this is the floodfill point
    Public Sub New()
        If standalone Then
            labels(3) = "FloodFill_Point standalone just shows the edges"
        Else
            labels(3) = "Resulting mask from floodfill"
        End If
        labels(2) = "Input image to floodfill"
        task.desc = "Use floodfill at a single location in a grayscale image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        dst2 = src.Clone()
        If standalone Then
            pt = New cv.Point(msRNG.Next(0, dst2.Width - 1), msRNG.Next(0, dst2.Height - 1))
            edges.RunClass(src)
            dst2 = edges.mats.dst2
            dst3 = edges.mats.mat(quadrantIndex)
        Else
            Dim maskPlus = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8UC1, 0)
            Dim maskRect = New cv.Rect(1, 1, maskPlus.Width - 2, maskPlus.Height - 2)

            Dim zero = New cv.Scalar(0)
            pixelCount = cv.Cv2.FloodFill(dst2, maskPlus, New cv.Point(CInt(pt.X), CInt(pt.Y)), cv.Scalar.White, rect, zero, zero, floodFlag Or (255 << 8))
            dst3 = maskPlus(maskRect).Clone
            pixelCount = pixelCount
            Dim m = cv.Cv2.Moments(maskPlus(rect), True)
            centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
            labels(3) = CStr(pixelCount) + " pixels at point pt(x=" + CStr(pt.X) + ",y=" + CStr(pt.Y)
        End If
    End Sub
End Class










Public Class FloodFill_Click : Inherits VBparent
    Dim edges As New Edges_BinarizedSobel
    Dim flood As New FloodFill_Point
    Public Sub New()
        flood.pt = New cv.Point(msRNG.Next(0, dst2.Width - 1), msRNG.Next(0, dst2.Height - 1))
        labels(3) = "Click anywhere to floodfill that area"
        task.desc = "FloodFill where the mouse clicks"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.mouseClickFlag Then
            flood.pt = task.mouseClickPoint
            task.mouseClickFlag = False ' preempt any other uses
        End If

        edges.RunClass(src)
        dst2 = edges.dst3

        If flood.pt.X Or flood.pt.Y Then
            flood.RunClass(dst2.Clone)
            dst2.CopyTo(dst3)
            If flood.pixelCount > 0 Then dst3.SetTo(255, flood.dst3)
        End If
    End Sub
End Class









Public Class FloodFill_Palette : Inherits VBparent
    Public basics As New FloodFill_Basics
    Public Sub New()
        labels(3) = "Image below is 8UC1 input to Palette_Basics"
        task.desc = "Create a floodfill image that is only 8-bit for use with a palette"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static minSizeSlider = findSlider("FloodFill Minimum Size")
        basics.RunClass(src)

        dst3.SetTo(0)
        Dim sortedSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
        For i = 0 To basics.maskSizes.Count - 1
            sortedSizes.Add(basics.maskSizes(i), i)
        Next

        For i = 0 To basics.masks.Count - 1
            Dim maskIndex = sortedSizes.ElementAt(i).Value
            Dim r = basics.rects(maskIndex)
            dst3(r).SetTo(cv.Scalar.All((i + 1) Mod 255), basics.masks(maskIndex))
        Next

        task.palette.RunClass(dst3 * 255 / basics.masks.Count) ' spread the colors 
        dst2 = task.palette.dst2

        labels(2) = CStr(basics.masks.Count) + " regions > " + CStr(minSizeSlider.value) + " pixels"
    End Sub
End Class