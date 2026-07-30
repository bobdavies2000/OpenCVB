Imports System.Runtime.InteropServices
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Flood_Basics : Inherits TaskParent
        Public rectList As New List(Of cv.Rect)
        Public indexList As New List(Of Integer)
        Public mask As New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
            dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
            labels(3) = "FloodFill mask"
            desc = "Cursor.ai: FloodFill the input and list regions sorted by pixel count."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then
                Static color8u As New Color8U_Basics
                color8u.Run(src)
                src = color8u.dst2
            End If
            dst1 = src.Clone
            dst2 = Palettize(src)

            Dim sortList As New SortedList(Of Integer, (count As Integer, rect As cv.Rect, index As Integer))(
                                           New compareAllowIdenticalIntegerInverted)
            Dim rect As cv.Rect
            Dim index As Integer = 1

            mask.SetTo(0)
            For y = 0 To src.Height - 1
                For x = 0 To src.Width - 1
                    If mask.Get(Of Byte)(y, x) = 0 Then ' it is surprising how much performance benefits from this statement.
                        Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                        Dim count = FloodFill(src, mask, New cv.Point(x, y), index, rect, 0, 0, flags)
                        If count >= 10 Then
                            sortList.Add(count, (count, ValidateRect(rect), index))
                            index += 1
                            If index >= 255 Then index = 1
                        End If
                    End If
                Next
            Next

            rectList.Clear()
            indexList.Clear()
            For Each item In sortList.Values
                If item.count >= 10 Then
                    rectList.Add(item.rect)
                    indexList.Add(item.index)
                End If
            Next

            labels(2) = CStr(rectList.Count) + " regions found, sorted by size"
        End Sub
    End Class






    Public Class Flood_Original : Inherits TaskParent
        Implements IDisposable
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_32S, 0)
        Public fLess As New FeatureLess_DepthFull
        Dim lastCenters As New HashSet(Of cv.Rect)
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
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
            rcMap.SetTo(0)
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
                        rcMap(rc.rect).SetTo(rc.mapID, rc.mask)
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
        Dim flood As New Flood_Original
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
            task.gOptions.displayDst1.Checked = True
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
            If standalone Then task.gOptions.displayDst1.Checked = True
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




    Public Class Flood_FeatureLess : Inherits TaskParent
        Dim fLess As New FeatureLess_DepthFull
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




    Public Class Flood_OriginalNew : Inherits TaskParent
        Implements IDisposable
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_32S, 0)
        Public fLess As New FeatureLess_DepthFull
        Dim lastCenters As New HashSet(Of cv.Rect)
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
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
            rcMap.SetTo(0)
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
                        rcMap(rc.rect).SetTo(rc.mapID, rc.mask)
                    End If
                End If
            Next

            lastCenters.Clear()
            For Each rc In rcList
                lastCenters.Add(task.gridNabeRects(rc.index))
            Next

            If standalone Then
                'strOut = Utility_Basics.selectCell(rcMap, rcList)
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
            dst2 = Palettize(redC.rcMap, 0)
            labels(2) = redC.labels(2)

            redC.Run(Not dst1)
            dst3 = Palettize(redC.rcMap, 0)
            labels(3) = redC.labels(2)
        End Sub
    End Class

End Namespace