Imports System.Runtime.InteropServices : Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Flood_Basics : Inherits TaskParent
        Public rectList As New List(Of cv.Rect)
        Public indexList As New List(Of Integer)
        Public mask As New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "FloodFill the input and create a list of rect's sorted by pixel count."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then
                Static color8u As New Color8U_Basics
                color8u.Run(src)
                dst1 = color8u.dst2.clone
            Else
                dst1 = src.Clone
            End If

            dst1.ConvertTo(dst1, cv.MatType.CV_32S)
            dst2 = Palettize(dst1, 0)

            Dim sortList As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
            Dim sortIndexList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
            Dim rect As cv.Rect
            mask.SetTo(0)
            For y = 0 To src.Height - 1
                For x = 0 To src.Width - 1
                    If mask.Get(Of Byte)(y, x) = 0 Then ' it is surprising how much performance benefits from this statement.
                        Dim index = sortList.Count + 1
                        Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                        Dim count = FloodFill(dst1, mask, New cv.Point(x, y), index, rect, 0, 0, flags)
                        If count >= 10 Then
                            sortList.Add(count, ValidateRect(rect))
                            sortIndexList.Add(count, index)
                        End If
                    End If
                Next
            Next

            rectList = New List(Of cv.Rect)(sortList.Values)
            indexList = New List(Of Integer)(sortIndexList.Values)
            labels(2) = CStr(rectList.Count) + " regions found, sorted by size"
        End Sub
    End Class







    Public Class XR_Flood_Original : Inherits TaskParent
        Implements IDisposable
        Public rcList As New List(Of rcData)
        Public rcIndexMap As New Mat(dst2.Size, MatType.CV_32F, 0)
        Public fLess As New XR_FeatureLess_DepthFull
        Dim lastCenters As New HashSet(Of cv.Rect)
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            cPtr = RedFlood_Open()
            desc = "Match the previous featureLess regions as best as possible."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.grayOriginal.Clone)
            dst1 = fLess.dst1

            Dim imagePtr As IntPtr
            Dim inputData(src.Total - 1) As Byte
            dst1.GetArray(Of Byte)(inputData)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            Dim minSize = task.gridWH * task.gridWH
            imagePtr = RedFlood_Run(cPtr, handleInput.AddrOfPinnedObject(), dst2.Rows, dst2.Cols, minSize)
            handleInput.Free()

            Dim rMask = New cv.Rect(1, 1, dst2.Width, dst2.Height)
            Dim mask = Mat.FromPixelData(dst2.Rows + 2, dst2.Cols + 2, MatType.CV_8U, imagePtr)
            dst0 = mask(rMask).Clone

            Dim classCount = RedFlood_Count(cPtr)
            If classCount = 0 Then Exit Sub ' no data to process.

            Dim rectData = Mat.FromPixelData(classCount, 1, MatType.CV_32SC4, RedFlood_Rects(cPtr))
            Dim rects(classCount - 1) As cv.Rect
            rectData.GetArray(Of cv.Rect)(rects)

            Dim rcLastList = New List(Of rcData)(rcList)

            rcList.Clear()
            rcList.Add(New rcData)
            rcIndexMap.SetTo(0)
            dst2.SetTo(0)
            Dim gRectSize = New cv.Size(task.gridWH, task.gridWH)
            For Each r In rects
                ' skip the cells that are just one gridRect.
                If r.Size <> gRectSize Then
                    Dim rc = New rcData(dst0(r), r, rcList.Count)
                    If rc.pixels > 0 Then
                        For i = 0 To lastCenters.Count - 1
                            Dim rect = lastCenters(i)
                            If rect.Contains(rc.maxDist) Then
                                rc.age = rcLastList(i).age + 1
                                Exit For
                            End If
                        Next
                        rc.index = rcList.Count
                        rcList.Add(rc)
                        dst2(rc.rect).SetTo(task.scalarColors(rc.index Mod 255), rc.mask)
                        rcIndexMap(rc.rect).SetTo(rc.mapID, rc.mask)
                    End If
                End If
            Next

            lastCenters.Clear()
            For Each rc In rcList
                lastCenters.Add(task.gridNabeRects(rc.index))
            Next

            labels(2) = CStr(rcList.Count) + " cells found. "
        End Sub
        Protected Overrides Sub Finalize()
            If cPtr <> 0 Then cPtr = RedFlood_Close(cPtr)
        End Sub
    End Class






    Public Class Flood_OriginalDemo : Inherits TaskParent
        Dim flood As New XR_Flood_Original
        Public Sub New()
            labels(3) = "Edge_Canny output"
            desc = "Use color to connect FCS cells - visualize the data mostly."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            flood.Run(src)
            dst2 = flood.dst2

            dst1 = src.Clone

            CvtColor(task.edges.dst2, dst3, ColorConversionCodes.GRAY2BGR)

            dst2.SetTo(white, dst3)
        End Sub
    End Class







    Public Class XR_Flood_Tiers : Inherits TaskParent
        Dim flood As New Flood_OriginalMask
        Dim color8U As New Color8U_Basics
        Dim tiers As New Depth_Tiers
        Public Sub New()
            task.gOptions.showMyDst1.Checked = True
            desc = "Subdivide the Flood_Original cells using depth tiers."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim tier = task.gOptions.DebugSlider.Value
            tiers.Run(src)

            If tier >= tiers.classCount Then tier = 0

            If tier = 0 Then
                InRange(tiers.dst2, 0, 1, dst0)
                dst0 = Not dst0
            Else
                InRange(tiers.dst2, tier, tier, dst0)
                dst0 = Not dst0
            End If

            labels(2) = tiers.labels(2) + " in tier " + CStr(tier) + ".  Use the global options 'DebugSlider' to select different tiers."

            color8U.Run(src)

            flood.inputRemoved = dst0
            flood.Run(color8U.dst2)

            dst2 = flood.dst2
            dst3 = flood.dst3

            SetTrueText(flood.redC.strOut, 1)
        End Sub
    End Class





    Public Class XR_Flood_Minimal : Inherits TaskParent
        Dim prep As New RedPrep_Basics
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            labels(2) = "Output is from RedPrep_Core. Click any region to floodfill it."
            labels(3) = "Mask resulting region selected by the click."
            desc = "Floodfill the selected segment of the RedPrep image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prep.Run(src)
            dst2 = prep.dst1

            If task.mouseClickFlag Then
                Dim rect As New cv.Rect
                Dim pt = task.clickPoint
                Dim mask = New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
                Dim flags = FloodFillFlags.FixedRange Or (255 << 8) Or FloodFillFlags.MaskOnly
                Dim count = FloodFill(dst2, mask, pt, 255, rect, 0, 0, flags)
                dst1.SetTo(0)
                dst3 = mask(New cv.Rect(1, 1, dst2.Width, dst2.Height)).Clone
                Rectangle(dst1, rect, Scalar.All(255), task.lineWidth)
            End If
        End Sub
    End Class






    Public Class Flood_Edges : Inherits TaskParent
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Floodfill the selected segment of the RedPrep image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = task.edges.dst2
            labels(3) = task.edges.labels(2)

            Dim rcList = RedCloud_Core.sweepImage(dst3, 0)

            Static rcIndex As Integer
            dst1.SetTo(0)
            If rcIndex >= rcList.Count Then rcIndex = 0
            Dim rc = rcList(rcIndex)
            dst1(rc.rect).SetTo(task.scalarColors(rc.index Mod 255), rc.mask)
            If task.heartBeatLT Then
                rcIndex += 1
                If rcIndex >= rcList.Count Then rcIndex = 0
            End If

            dst2.SetTo(0)
            For Each rc In rcList
                dst2(rc.rect).SetTo(task.scalarColors(rc.index Mod 255), rc.mask)
            Next

            labels(2) = CStr(rcList.Count) + " cells were found."
        End Sub
    End Class






    Public Class Flood_OriginalMask : Inherits TaskParent
        Public inputRemoved As New Mat
        Public showSelected As Boolean = True
        Public redC As New RedC_Basics
        Dim color8U As New Color8U_Basics
        Public Sub New()
            labels(3) = "The inputRemoved mask is used to limit how much of the image is processed."
            desc = "Floodfill by color as usual."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            color8U.Run(src)
            InRange(task.pcSplit(2), task.MaxZmeters, 1000, inputRemoved)
            ConvertScaleAbs(inputRemoved, inputRemoved)
            src = color8U.dst2

            src.SetTo(0, inputRemoved)

            redC.Run(src)
            labels(2) = redC.labels(2)
            dst2 = redC.dst2.SetTo(0, inputRemoved)

            labels(2) = $"{redC.rcList.Count} cells identified"

            If showSelected Then SetTrueText(redC.strOut, 3)
        End Sub
    End Class




    Public Class XR_Flood_FeatureLess : Inherits TaskParent
        Dim fLess As New XR_FeatureLess_DepthFull
        Dim redC As New RedC_Basics
        Dim edges As New Edge_Basics_TA
        Public Sub New()
            desc = "Match flooded cells with FeatureLess clusters"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray)
            dst2 = fLess.dst2
            labels(2) = fLess.labels(2)

            redC.Run(src)
            dst3 = redC.dst2
            labels(3) = redC.labels(2)

            Dim _edges_cvt As New Mat
            CvtColor(dst2, _edges_cvt, ColorConversionCodes.BGR2GRAY)
            edges.Run(_edges_cvt)
            dst3.SetTo(white, edges.dst2)

            SetTrueText(redC.strOut, 1)
        End Sub
    End Class




    Public Class XR_Flood_OriginalNew : Inherits TaskParent
        Implements IDisposable
        Public rcList As New List(Of rcData)
        Public rcIndexMap As New Mat(dst2.Size, MatType.CV_32F, 0)
        Public fLess As New XR_FeatureLess_DepthFull
        Dim lastCenters As New HashSet(Of cv.Rect)
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            cPtr = RedFlood_Open()
            desc = "Match the previous featureLess regions as best as possible."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.grayOriginal.Clone)
            dst1 = fLess.dst1

            Dim imagePtr As IntPtr
            Dim inputData(src.Total - 1) As Byte
            dst1.GetArray(Of Byte)(inputData)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            Dim minSize = task.gridWH * task.gridWH
            imagePtr = RedFlood_Run(cPtr, handleInput.AddrOfPinnedObject(), dst2.Rows, dst2.Cols, minSize)
            handleInput.Free()

            Dim rMask = New cv.Rect(1, 1, dst2.Width, dst2.Height)
            Dim mask = Mat.FromPixelData(dst2.Rows + 2, dst2.Cols + 2, MatType.CV_8U, imagePtr)
            dst0 = mask(rMask).Clone

            Dim classCount = RedFlood_Count(cPtr)
            If classCount = 0 Then Exit Sub ' no data to process.

            Dim rectData = Mat.FromPixelData(classCount, 1, MatType.CV_32SC4, RedFlood_Rects(cPtr))
            Dim rects(classCount - 1) As cv.Rect
            rectData.GetArray(Of cv.Rect)(rects)

            Dim rcLastList = New List(Of rcData)(rcList)

            rcList.Clear()
            rcIndexMap.SetTo(0)
            dst2.SetTo(0)
            For Each r In rects
                ' skip the cells that are just one gridRect.
                If r.Size <> task.gridRects(0).Size Then
                    Dim rc = New rcData(dst0(r), r, rcList.Count + 1)
                    If rc.pixels > 0 Then
                        For i = 0 To lastCenters.Count - 1
                            Dim rect = lastCenters(i)
                            If rect.Contains(rc.maxDist) Then
                                rc.age = rcLastList(i).age + 1
                                Exit For
                            End If
                        Next

                        rcList.Add(rc)
                        dst2(rc.rect).SetTo(task.scalarColors(rc.index Mod 255), rc.mask)
                        rcIndexMap(rc.rect).SetTo(rc.mapID, rc.mask)
                    End If
                End If
            Next

            lastCenters.Clear()
            For Each rc In rcList
                lastCenters.Add(task.gridNabeRects(rc.index))
            Next

            If standalone Then
                'strOut = Utility_Basics.selectCell(rcIndexMap, rcList)
                'SetTrueText(strOut, 3)
            End If

            labels(2) = CStr(rcList.Count) + " cells found. "
        End Sub
        Protected Overrides Sub Finalize()
            If cPtr <> 0 Then cPtr = RedFlood_Close(cPtr)
        End Sub
    End Class




    'Public Class RedFlood_OriginalNew
    '    Public src As Mat
    '    Public result As Mat
    '    Public cellRects As New List(Of Rect)
    '    Public Sub New()
    '    End Sub
    '    Public Sub RunCPP(minSize As Integer)
    '        ' result is (rows+2, cols+2) because OpenCV floodFill requires a padded mask
    '        result = New Mat(src.Rows + 2, src.Cols + 2, MatType.CV_8U)
    '        result.SetTo(0)

    '        Dim maskFill As Integer = 255

    '        ' Equivalent to C++: multimap<int, Point, greater<int>>
    '        Dim sizeSorted As New SortedDictionary(Of Integer, List(Of Point))(Comparer(Of Integer).Create(Function(a, b) b.CompareTo(a)))

    '        Dim floodFlag As Integer = FloodFillFlags.MaskOnly Or FloodFillFlags.FixedRange Or 4

    '        For y = 0 To src.Rows - 1
    '            For x = 0 To src.Cols - 1
    '                If src.Get(Of Byte)(y, x) <> 0 Then
    '                    Dim pt As New Point(x, y)

    '                    Dim count As Integer =
    '                    Cv2.FloodFill(
    '                        src,
    '                        result,
    '                        pt,
    '                        New Scalar(255),
    '                        Nothing,
    '                        New Scalar(0),
    '                        New Scalar(0),
    '                        floodFlag Or (maskFill << 8)
    '                    )

    '                    If count > minSize Then
    '                        If Not sizeSorted.ContainsKey(count) Then sizeSorted(count) = New List(Of Point)
    '                        sizeSorted(count).Add(pt)
    '                    End If
    '                End If
    '            Next
    '        Next

    '        cellRects.Clear()
    '        maskFill = 1
    '        result.SetTo(0)

    '        For Each kv In sizeSorted
    '            For Each pt In kv.Value
    '                Dim rect As Rect

    '                Dim count As Integer =
    '                Cv2.FloodFill(
    '                    src,
    '                    result,
    '                    pt,
    '                    New Scalar(255),
    '                    rect,
    '                    New Scalar(0),
    '                    New Scalar(0),
    '                    floodFlag Or (maskFill << 8)
    '                )

    '                If count >= 1 Then
    '                    cellRects.Add(rect)

    '                    If maskFill >= 255 Then Exit Sub
    '                    maskFill += 1
    '                End If
    '            Next
    '        Next
    '    End Sub
    'End Class





    'Public Class RedFlood_OriginalNew : Inherits TaskParent
    '    Public Sub New()
    '        desc = "description"
    '    End Sub
    '    Public Overrides Sub RunAlg(src As cv.Mat)
    '        CvtColor(src, src, cv.ColorConversionCodes.BGR2GRAY)
    '    End Sub
    'End Class





    Public Class XR_Flood_DarkLight : Inherits TaskParent
        Dim redC As New RedC_Basics
        Dim options As New Options_CComp
        Public Sub New()
            desc = "FloodFill the light half of the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Threshold(task.gray, dst1, options.light, 255, ThresholdTypes.Binary)
            redC.Run(dst1)
            dst2 = Palettize(redC.rcIndexMap, 0)
            labels(2) = redC.labels(2)

            redC.Run(Not dst1)
            dst3 = Palettize(redC.rcIndexMap, 0)
            labels(3) = redC.labels(2)
        End Sub
    End Class





    Public Class Flood_RectMatsOld : Inherits TaskParent
        Dim color8U As New Color8U_Basics
        Public fLess As New FeatureLess_Core
        Public rectList As New List(Of (cv.Rect, cv.Mat))
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Build a list of cv.rects and cv.mats for each floodfill region in the Color8U input."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then
                color8U.Run(src)
                src = color8U.dst2.Clone
                labels(2) = color8U.labels(2)
            End If

            fLess.Run(src)
            dst2 = color8U.dst3.Clone

            Dim mask As New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
            Dim rect As cv.Rect
            Dim filled As New cv.Mat
            rectList.Clear()
            dst1 = color8U.dst2.Clone

            For Each lp In task.lines.lpList
                Line(dst1, lp.p1, lp.p2, cv.Scalar.All(255), task.lineWidth + 2, cv.LineTypes.Link8)
            Next

            cv.Cv2.ImShow("dst1", dst1)

            For y = 0 To dst2.Height - 1
                For x = 0 To dst2.Width - 1
                    If mask.Get(Of Byte)(y, x) = 0 Then
                        Dim index = rectList.Count + 1
                        Dim flags = FloodFillFlags.FixedRange Or ((index) << 8)
                        Dim count = FloodFill(dst1, mask, New cv.Point(x, y), index, rect, 0, 0, flags)
                        If count = 0 Or rect.Width <= 0 Or rect.Height <= 0 Then Continue For
                        rect = ValidateRect(rect)
                        If count >= task.minCellSize Then
                            InRange(dst1(rect), index, index, filled)
                            rectList.Add((rect, filled.Clone))
                        End If
                    End If
                Next
            Next

            dst1.SetTo(0)
            For i = 0 To rectList.Count - 1
                dst1(rectList(i).Item1).SetTo(i + 1, rectList(i).Item2)
            Next

            dst3 = Palettize(dst1, 0)
            For Each rc In fLess.rcList
                If rc.contour Is Nothing OrElse rc.contour.Count < 3 Then Continue For
                DrawContours(dst2(rc.rect), {rc.contour}, 0, white, task.lineWidth)
            Next
            labels(1) = fLess.labels(2)
            labels(3) = CStr(rectList.Count) + " color input cells.  A warning will appear if more than 255 cells."
            If rectList.Count > 255 Then MsgBox("Flood_RectMats needs to increase the minimum cell size - too many to fit in CV_8U!")
        End Sub
    End Class




    Public Class Flood_CellMergeOld : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Dim rectMats As New Flood_RectMats
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Use CalcHist on FeatureLess_Core cells to find Color8U floodfill regions."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            rectMats.Run(src)
            dst2 = rectMats.dst2
            labels(2) = rectMats.labels(3)

            Dim binCount = 256
            Dim ranges() As Rangef = {New Rangef(0, binCount)}
            Dim histogram As New Mat
            Dim histArray() As Single = Nothing

            dst1.SetTo(0)
            rcList = New List(Of rcData)(rectMats.fLess.rcList)
            Dim consumed(binCount) As Integer
            For i = 0 To rcList.Count - 1
                Dim rc = rcList(i)
                CalcHist({rectMats.dst1(rc.rect)}, {0}, rc.mask, histogram, 1, {binCount}, ranges)
                histogram.Set(Of Single)(0, 0, 0)
                histogram.GetArray(Of Single)(histArray)

                For j = 1 To rectMats.rectList.Count - 1
                    If histArray(j) > task.minCellSize Then
                        Dim tupleRect = rectMats.rectList(j - 1).Item1
                        Dim tupleMask = rectMats.rectList(j - 1).Item2
                        rc.rect = rc.rect.Union(tupleRect)
                        rc.index = i + 1
                        dst1(tupleRect).SetTo(rc.index, tupleMask)
                        consumed(j) = i
                    End If
                Next

                InRange(dst1(rc.rect), rc.index, rc.index, rc.mask)
                rc.pixels = CountNonZero(rc.mask)
                rcList(i) = rc
            Next

            ' this cleans up any small holes in the resulting cells.  Skip if performance is a problem.
            For Each rc In rcList
                Dim contour = ContourBuild(rc.mask, cv.ContourApproximationModes.ApproxSimple)
                If contour.Count < 3 Then Continue For
                rc.contour = contour
                DrawContours(rc.mask, {rc.contour}, 0, cv.Scalar.All(255), -1, cv.LineTypes.Link4)
                rc.pixels = CountNonZero(rc.mask)

                dst1(rc.rect).SetTo(rc.index, rc.mask)
            Next

            dst3 = Palettize(dst1, 0)

            Dim clickIndex = dst1.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
            If clickIndex > 0 Then
                Dim rcTest = rcList(clickIndex - 1)
                Rectangle(dst3, rcTest.rect, task.highlight, task.lineWidth)
            End If


            labels(3) = CStr(rectMats.rectList.Count) + " cells merged into " + CStr(rcList.Count)
        End Sub
    End Class




    Public Class Flood_RectMats : Inherits TaskParent
        Dim color8U As New Color8U_Basics
        Public fLess As New FeatureLess_Core
        Public rectList As New List(Of (cv.Rect, cv.Mat))
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Build a list of cv.rects and cv.mats for each floodfill region in the Color8U input."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then
                color8U.Run(src)
                src = color8U.dst2.Clone
                labels(2) = color8U.labels(2)
            End If

            fLess.Run(src)
            dst2 = color8U.dst3.Clone

            Dim mask As New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
            Dim rect As cv.Rect
            Dim filled As New cv.Mat
            rectList.Clear()
            dst1 = color8U.dst2.Clone
            For y = 0 To dst2.Height - 1
                For x = 0 To dst2.Width - 1
                    If mask.Get(Of Byte)(y, x) = 0 Then
                        Dim index = rectList.Count + 1
                        Dim flags = FloodFillFlags.FixedRange Or ((index) << 8)
                        Dim count = FloodFill(dst1, mask, New cv.Point(x, y), index, rect, 0, 0, flags)
                        If count = 0 Or rect.Width <= 0 Or rect.Height <= 0 Then Continue For
                        rect = ValidateRect(rect)
                        If count >= task.minCellSize Then
                            InRange(dst1(rect), index, index, filled)
                            rectList.Add((rect, filled.Clone))
                        End If
                    End If
                Next
            Next

            dst3 = Palettize(dst1, 0)
            For Each rc In fLess.rcList
                If rc.contour Is Nothing OrElse rc.contour.Count < 3 Then Continue For
                DrawContours(dst3(rc.rect), {rc.contour}, 0, white, task.lineWidth)
            Next

            Dim clickIndex = dst1.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
            If clickIndex > 0 Then
                Dim tuple = rectList(clickIndex - 1)
                Rectangle(dst2, tuple.Item1, task.highlight, task.lineWidth)
                dst2(tuple.Item1).SetTo(white, tuple.Item2)
                SetTrueText(CStr(clickIndex), tuple.Item1.TopLeft)
            End If

            labels(1) = fLess.labels(2)
            labels(3) = CStr(rectList.Count) + " color input cells.  A warning will appear if more than 255 input cells."
            If rectList.Count > 255 Then MsgBox("Flood_RectMats needs to increase the minimum cell size - too many to fit in CV_8U!")
        End Sub
    End Class




    Public Class Flood_CellMerge : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Dim rectMats As New Flood_RectMats
        Dim rcFill As New flood_FillMask
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Use CalcHist on FeatureLess_Core cells to find Color8U floodfill regions."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            rectMats.Run(src)
            dst2 = rectMats.dst3
            labels(2) = rectMats.labels(3)

            Dim binCount = 256
            Dim ranges() As Rangef = {New Rangef(0, binCount)}
            Dim histogram As New Mat
            Dim histArray() As Single = Nothing

            dst1.SetTo(0)
            rcList = New List(Of rcData)(rectMats.fLess.rcList)
            Dim consumed() As Integer = Enumerable.Repeat(-1, 256).ToArray()
            Dim removeList As New List(Of Integer)
            For i = 0 To rcList.Count - 1
                Dim rc = rcList(i)
                CalcHist({rectMats.dst1(rc.rect)}, {0}, rc.mask, histogram, 1, {binCount}, ranges)
                histogram.GetArray(Of Single)(histArray)
                If task.gOptions.DebugSlider.Value = i Then Dim k = 0

                Dim rcListConsumer As Integer = -1
                For j = 1 To rectMats.rectList.Count - 1
                    If histArray(j) > task.minCellSize And consumed(j) Then
                        rcListConsumer = consumed(j)
                        Exit For
                    End If
                Next

                If rcListConsumer = -1 Then
                    Dim allConsumed As Boolean = True
                    For j = 1 To rectMats.rectList.Count - 1
                        If histArray(j) > task.minCellSize And consumed(j) = -1 Then
                            allConsumed = False
                            Exit For
                        End If
                    Next
                    If allConsumed Then
                        removeList.Add(i)
                        Continue For
                    End If
                    rcListConsumer = i
                End If

                rc = rcList(rcListConsumer)

                For j = 1 To rectMats.rectList.Count - 1
                    If histArray(j) > task.minCellSize Then
                        Dim tupleRect = rectMats.rectList(j - 1).Item1
                        Dim tupleMask = rectMats.rectList(j - 1).Item2
                        If tupleRect.IntersectsWith(rc.rect) Then
                            rc.rect = rc.rect.Union(tupleRect)
                            dst1(tupleRect).SetTo(rcListConsumer + 1, tupleMask)
                            consumed(j) = rcListConsumer
                        End If
                    End If
                Next

                InRange(dst1(rc.rect), rcListConsumer + 1, rcListConsumer + 1, rc.mask)
                rcList(rcListConsumer) = rc
                If rc.index = 3 Then Exit For
            Next

            For i = removeList.Count - 1 To 0 Step -1
                rcList.RemoveAt(i)
            Next

            Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            dst1.SetTo(0)
            For Each rc In rcList
                rcFill.rc = rc
                rcFill.Run(Nothing)

                rcFill.rc.index = sortList.Count + 1

                dst1(rcFill.rc.rect).SetTo(rcFill.rc.index, rcFill.rc.mask)
                sortList.Add(CountNonZero(rc.mask), rcFill.rc)
                If rc.index = 3 Then Exit For
            Next

            rcList = New List(Of rcData)(sortList.Values)
            dst3 = Palettize(dst1, 0)

            Dim clickIndex = dst1.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
            If clickIndex > 0 Then
                Dim rc = rcList(clickIndex - 1)
                Rectangle(dst3, rc.rect, task.highlight, task.lineWidth)
                SetTrueText(rc.displayCell, 1)
            End If

            labels(3) = CStr(rectMats.rectList.Count) + " input cells merged into the " + CStr(rcList.Count) + " featureless regions."
        End Sub
    End Class




    Public Class flood_FillMask : Inherits TaskParent
        Public rc As rcData
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "create a contour that fills in the gaps."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim saveRect = rc.rect
            Dim mask As New Mat(New Size(rc.mask.Width + 2, rc.mask.Height + 2), MatType.CV_8U, 0)
            dst1.SetTo(0)
            rc.mask.CopyTo(dst1(rc.rect))
            Dim pt = New cv.Point(rc.maxDist.X - saveRect.X, rc.maxDist.Y - saveRect.Y)
            Dim count = FloodFill(dst1(rc.rect), mask, pt, 255, rc.rect, 0, 0, FloodFillFlags.FixedRange Or ((255) << 8))

            rc = New rcData(mask(rc.rect), rc.rect, 255)
            rc.rect.X += saveRect.X
            rc.rect.Y += saveRect.Y
            rc.maxDist = New cv.Point(rc.maxDist.X + saveRect.X, rc.maxDist.Y + saveRect.Y)
        End Sub
    End Class
End Namespace
