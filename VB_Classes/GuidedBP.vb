Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class GuidedBP_Basics : Inherits VB_Algorithm
    Public ptHot As New GuidedBP_HotPoints
    Dim topMap As New cv.Mat
    Dim sideMap As New cv.Mat
    Public Sub New()
        topMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        sideMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Correlate the hot points with the previous generation using a Map"
    End Sub
    Private Sub runMap(rectList As List(Of cv.Rect), dstindex As Integer, map As cv.Mat)
        Dim sortRects As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
        For Each r In rectList
            sortRects.Add(r.Width * r.Height, r)
        Next

        Dim ptList As New List(Of cv.Point)
        Dim indices As New List(Of Integer)
        For Each r In sortRects.Values
            Dim pt = New cv.Point(CInt(r.X + r.Width / 2), CInt(r.Y + r.Height / 2))
            Dim index = map.Get(Of Byte)(pt.Y, pt.X)
            If index = 0 Or indices.Contains(index) Then
                If index = ptList.Count Then index = ptList.Count + 1 Else index = ptList.Count
            End If
            ptList.Add(pt)
            indices.Add(index)
        Next

        map.SetTo(0)
        For Each r In sortRects.Values
            Dim pt = New cv.Point(CInt(r.X + r.Width / 2), CInt(r.Y + r.Height / 2))
            Dim index = indices(ptList.IndexOf(pt))
            map.Rectangle(r, index, -1)
            setTrueText(CStr(index), pt, dstindex)
        Next
    End Sub
    Public Sub RunVB(src As cv.Mat)
        ptHot.Run(src)
        dst2 = ptHot.dst2
        dst3 = ptHot.dst3

        runMap(ptHot.topRects, 2, topMap)
        runMap(ptHot.sideRects, 3, sideMap)

        labels(2) = CStr(ptHot.topRects.Count) + " objects found in the top view"
        labels(3) = CStr(ptHot.sideRects.Count) + " objects found in the Side view"
    End Sub
End Class






Public Class GuidedBP_HotPointsKNN : Inherits VB_Algorithm
    Dim ptHot As New GuidedBP_HotPoints
    Dim knnSide As New KNN_Core
    Dim knnTop As New KNN_Core
    Public Sub New()
        desc = "Correlate the hot points with the previous generation to ID each object"
    End Sub
    Private Sub runKNN(knn As KNN_Core, rectList As List(Of cv.Rect), dst As cv.Mat, dstindex As Integer)
        knn.queries.Clear()
        For Each r In rectList
            knn.queries.Add(New cv.Point2f(CSng(r.X + r.Width / 2), CSng(r.Y + r.Height / 2)))
        Next

        If firstPass Then knn.trainInput = New List(Of cv.Point2f)(knn.queries)

        knn.Run(empty)

        For i = 0 To knn.queries.Count - 1
            Dim p1 = knn.queries(i)
            Dim index = knn.result(i, 0)
            Dim p2 = knn.trainInput(index)
            Dim dist = p1.DistanceTo(p2)
            Dim r = rectList(i)
            If dist < r.Width / 2 And dist < r.Height / 2 Then
                dst.Rectangle(r, cv.Scalar.White, task.lineWidth)
                Dim pt = New cv.Point(r.X + r.Width, r.Y + r.Height)
                setTrueText(CStr(index), pt, dstindex)
            End If
        Next

        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
    End Sub
    Public Sub RunVB(src As cv.Mat)
        ptHot.Run(src)
        dst2 = ptHot.dst2
        dst3 = ptHot.dst3

        runKNN(knnTop, ptHot.topRects, dst2, 2)
        runKNN(knnSide, ptHot.sideRects, dst3, 3)

        labels(2) = CStr(ptHot.topRects.Count) + " objects found in the top view"
        labels(3) = CStr(ptHot.sideRects.Count) + " objects found in the Side view"
    End Sub
End Class







Public Class GuidedBP_HotPoints : Inherits VB_Algorithm
    Public histTop As New Projection_HistTop
    Public histSide As New Projection_HistSide
    Public topRects As New List(Of cv.Rect)
    Public sideRects As New List(Of cv.Rect)
    Public Sub New()
        task.useXYRange = False
        desc = "Use floodfill to identify all the objects in both the top and side views."
    End Sub
    Private Function hotPoints(ByRef view As cv.Mat) As List(Of cv.Rect)
        Static floodRect As New cv.Rect(1, 1, dst2.Width - 2, dst2.Height - 2)
        Static mask As New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U)
        Dim rect As cv.Rect
        Dim points = view.FindNonZero()

        Dim viewList As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalIntegerInverted)
        mask.SetTo(0)
        Dim lastCount As Integer = 0
        For i = 0 To points.Rows - 1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            Dim count = view.FloodFill(mask, pt, 0, rect, 0, 0, 4 Or cv.FloodFillFlags.MaskOnly Or (255 << 8))
            If count > 0 Then viewList.Add(count, pt)
        Next

        mask.SetTo(0)
        Dim rectList As New List(Of cv.Rect)
        For i = 0 To Math.Min(viewList.Count, 10) - 1
            Dim pt = viewList.ElementAt(i).Value
            view.FloodFill(mask, pt, 0, rect, 0, 0, 4 Or cv.FloodFillFlags.FixedRange Or (i + 1 << 8))
            rectList.Add(New cv.Rect(rect.X - 1, rect.Y - 1, rect.Width, rect.Height))
        Next

        mask(floodRect).CopyTo(view)
        Return rectList
    End Function
    Public Sub RunVB(src As cv.Mat)
        histTop.Run(src.Clone)
        topRects = hotPoints(histTop.dst3)
        dst2 = vbPalette(histTop.dst3 * 255 / topRects.Count)

        histSide.Run(src)
        sideRects = hotPoints(histSide.dst3)
        dst3 = vbPalette(histSide.dst3 * 255 / sideRects.Count)

        If task.heartBeat Then labels(2) = "Top " + CStr(topRects.Count) + " objects identified in the top view."
        If task.heartBeat Then labels(3) = "Top " + CStr(sideRects.Count) + " objects identified in the side view."
    End Sub
End Class









Public Class GuidedBP_PlanesPlot : Inherits VB_Algorithm
    Dim histSide As New Projection_HistSide
    Public Sub New()
        labels = {"", "", "Side view", "Plot of nonzero rows in the side view"}
        desc = "Plot the likely floor or ceiling areas."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        histSide.Run(src)
        dst2 = histSide.dst3

        Dim sumList As New List(Of Integer)
        dst3.SetTo(0)
        For i = 0 To dst2.Rows - 1
            Dim x = dst2.Row(i).CountNonZero
            sumList.Add(x)
            dst3.Line(New cv.Point(0, i), New cv.Point(x, i), cv.Scalar.White, task.lineWidth)
        Next

        Dim flatSurfacesInRow As New List(Of Integer)
        For i = 0 To sumList.Count - 1
            If sumList(i) > 5 Then
                Dim maxSpike As Integer = sumList(i)
                Dim maxRow As Integer = i
                For j = i + 1 To sumList.Count - 1
                    If maxSpike < sumList(j) Then
                        maxSpike = sumList(j)
                        maxRow = j
                    End If
                    If sumList(j) = 0 Then
                        i = j
                        flatSurfacesInRow.Add(maxRow)
                        Exit For
                    End If
                Next
            End If
        Next

        labels(2) = "There were " + CStr(flatSurfacesInRow.Count) + " flat surface candidates found."
    End Sub
End Class






'Public Class GuidedBP_Hulls : Inherits VB_Algorithm
'    Dim bpDoctor As New GuidedBP_Points
'    Public redCells As New List(Of rcData)
'    Public redCellsLast As New List(Of rcData)
'    Public kMap As New cv.Mat
'    Dim contours As New Contour_Largest
'    Dim plot As New Hist_Depth
'    Public Sub New()
'        kMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
'        labels(3) = "Top X identified objects."
'        desc = "Find and display k objects using doctored back projection."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static saveTtext As List(Of trueText)
'        If task.paused Then Exit Sub

'        ' Use the DebugCheckBox box and it will rerun the exact same input.
'        If gOptions.DebugCheckBox.Checked = False Then bpDoctor.Run(src)

'        Dim newCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
'        Dim kMapLast = kMap.Clone
'        If task.heartBeat Then
'            kMap.SetTo(0)
'            dst2.SetTo(0)
'            Dim sizes As New List(Of Integer)
'            For i = 1 To bpDoctor.classCount
'                Dim rc As New rcData
'                rc.mask = bpDoctor.backP.InRange(i, i).Threshold(0, 255, cv.ThresholdTypes.Binary)

'                contours.Run(rc.mask)
'                rc.contour = contours.bestContour
'                rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList

'                vbDrawContour(rc.mask, rc.contour, 255, -1)
'                rc.maxDist = vbHullCenter(rc.hull)
'                Dim validate = rc.mask.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
'                If validate = 0 Then rc.maxDist = vbGetMaxDist(rc)

'                Dim mask = rc.mask And task.depthMask
'                rc.mmX = vbMinMax(task.pcSplit(0), mask)
'                rc.mmY = vbMinMax(task.pcSplit(1), mask)
'                rc.mmZ = vbMinMax(task.pcSplit(2), mask)
'                rc.pixels = rc.mask.CountNonZero()
'                If sizes.Contains(rc.pixels) = False Then
'                    newCells.Add(rc.pixels, rc)
'                    sizes.Add(rc.pixels)
'                End If
'                rc.color = randomCellColor()
'                vbDrawContour(dst2, rc.contour, rc.color, -1)
'            Next
'            dst0 = task.color.Clone
'            redCellsLast = New List(Of rcData)(redCells)
'            redCells.Clear()
'            redCells.Add(New rcData)

'            Dim cellUpdates As New List(Of rcData)
'            Dim usedColors As New List(Of cv.Vec3b)({black})
'            For Each entry In newCells
'                Dim rc As New rcData
'                rc = entry.Value

'                rc.index = redCells.Count
'                Dim index = kMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
'                Dim lrc As rcData
'                If index <> 0 Then
'                    lrc = redCellsLast(index)
'                    rc.indexLast = lrc.index
'                    lrc.index = redCells.Count
'                    rc.color = lrc.color
'                End If

'                If usedColors.Contains(rc.color) Then rc.color = randomCellColor()

'                vbDrawContour(kMap, rc.hull, rc.index, -1)
'                vbDrawContour(dst2, rc.contour, rc.color, -1)

'                redCells.Add(rc)
'                usedColors.Add(rc.color)
'                If redCells.Count > 20 Then Exit For ' more than 20 objects?  Likely small artifacts...
'            Next
'            strOut = "Index" + vbTab + "Size" + vbTab + "Depth Range" + vbTab
'            strOut += "Hull" + vbTab + vbTab + vbTab + vbCrLf
'            For Each rc In redCells
'                If rc.index = 0 Then Continue For
'                strOut += CStr(rc.index) + vbTab + Format(rc.pixels, fmt0) + vbTab
'                strOut += Format(rc.mmZ.minVal, fmt2) + "/" + Format(rc.mmZ.maxVal, fmt2) + vbTab
'                strOut += hullStr(rc.hull) + vbCrLf
'            Next
'            setTrueText(strOut, 3)

'            For Each rc In redCells
'                If rc.index = 0 Then Continue For
'                setTrueText(CStr(rc.index), rc.maxDist, 2)
'                dst2.Circle(rc.maxDist, task.dotSize, cv.Scalar.White, -1, task.lineType)
'            Next

'            saveTtext = New List(Of trueText)(trueData)
'        End If

'        If redCells.Count > 1 Then
'            setSelectedContour(redCells, kMap)
'            vbDrawContour(dst2, task.rc.contour, task.rc.color)
'        End If

'        plot.Run(task.pcSplit(2))
'        dst1 = plot.dst2

'        trueData = New List(Of trueText)(saveTtext)
'        labels(2) = "There were " + CStr(redCells.Count) + " objects identified in the image."
'    End Sub
'End Class





'Public Class GuidedBP_Objects : Inherits VB_Algorithm
'    Dim kHist As New GuidedBP_History
'    Dim reduction As New Reduction_Basics
'    Public Sub New()
'        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
'        desc = "Create the input to RedCloud_Basics combining color and GuidedBP_Hulls output"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        kHist.Run(src)
'        dst2 = kHist.guided.dst2
'        setTrueText(kHist.strOut, 3)

'        reduction.Run(src)

'        dst1.SetTo(0)
'        For Each rc In kHist.redCells
'            If rc.index = 0 Then Continue For
'            vbDrawContour(dst1, rc.contour, rc.index + reduction.classCount, -1)
'            setTrueText(CStr(rc.index), rc.maxDist, 2)
'        Next

'        If kHist.guided.redCells.Count > 1 Then
'            setSelectedContour(kHist.guided.redCells, kHist.guided.kMap)
'            vbDrawContour(dst2, task.rc.hull, cv.Scalar.White, task.lineWidth)
'        End If

'        reduction.dst2.SetTo(0, dst1)
'        dst1 += reduction.dst2

'        labels(2) = CStr(kHist.redCells.Count) + " objects found"
'    End Sub
'End Class






'Public Class GuidedBP_History : Inherits VB_Algorithm
'    Public guided As New GuidedBP_Hulls
'    Dim kCellList As New List(Of List(Of rcData))
'    Public redCells As New List(Of rcData)
'    Public kMap As New cv.Mat
'    Public Sub New()
'        If sliders.Setup(traceName) Then
'            sliders.setupTrackBar("Minimum distance between cells (cm)", 10, 100, 50)
'            sliders.setupTrackBar("Minimum size kCell", 1, dst2.Total / 10, dst2.Total / 100)
'        End If

'        kMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
'        desc = "Find the cells that are consistently present over time."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static minSlider = findSlider("Minimum size kCell")
'        Static distSlider = findSlider("Minimum distance between cells (cm)")
'        Dim minSize = minSlider.value
'        Dim distanceMin = distSlider.value / 100

'        If task.optionsChanged Then kCellList.Clear()

'        guided.Run(src)

'        kCellList.Add(New List(Of rcData)(guided.redCells))

'        Dim sortCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
'        For Each cellList In kCellList
'            For i = 1 To cellList.Count - 1
'                sortCells.Add(cellList(i).pixels, cellList(i))
'            Next
'        Next

'        kMap.SetTo(0)
'        redCells.Clear()
'        redCells.Add(New rcData)
'        Dim lastDst2 = dst2.Clone
'        Dim usedColors As New List(Of cv.Vec3b)({black})
'        For Each rc In sortCells.Values
'            Dim index = kMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
'            Dim lrc = redCells(index)
'            If Math.Abs((rc.mmZ.minVal + rc.mmZ.maxVal) / 2 - (lrc.mmZ.minVal + lrc.mmZ.maxVal) / 2) > distanceMin Then
'                If rc.pixels >= minSize Then
'                    rc.index = redCells.Count
'                    Dim color = lastDst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
'                    If usedColors.Contains(color) = False Then rc.color = color
'                    vbDrawContour(kMap, rc.contour, rc.index, -1)
'                    redCells.Add(rc)
'                    usedColors.Add(rc.color)
'                End If
'            End If
'        Next

'        dst2.SetTo(0)
'        For Each rc In redCells
'            vbDrawContour(dst2, rc.contour, rc.color, -1)
'            setTrueText(CStr(rc.index), rc.maxDist, 2)
'        Next

'        setTrueText(guided.strOut, 3)

'        If redCells.Count > 1 Then
'            setSelectedContour(redCells, kMap)
'            vbDrawContour(dst2, task.rc.hull, cv.Scalar.White, task.lineWidth)
'            vbDrawContour(task.color, task.rc.contour, cv.Scalar.Yellow)
'        End If


'        If kCellList.Count >= task.frameHistoryCount Then kCellList.RemoveAt(0)
'        If task.heartBeat Then labels(2) = CStr(redCells.Count) + " objects were consistently present"
'    End Sub
'End Class






Public Class GuidedBP_Points : Inherits VB_Algorithm
    Public hotPoints As New GuidedBP_Basics
    Public classCount As Integer
    Public selectedPoint As cv.Point
    Public topRects As New List(Of cv.Rect)
    Public sideRects As New List(Of cv.Rect)
    Public histogramTop As New cv.Mat
    Public histogramSide As New cv.Mat
    Public backP As New cv.Mat
    Public Sub New()
        desc = "Use floodfill to identify all the objects in the selected view then build a backprojection that identifies k objects in the image view."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hotPoints.Run(src)

        hotPoints.ptHot.histTop.dst3.ConvertTo(histogramTop, cv.MatType.CV_32F)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histogramTop, backP, task.rangesTop)

        topRects = New List(Of cv.Rect)(hotPoints.ptHot.topRects)
        sideRects = New List(Of cv.Rect)(hotPoints.ptHot.sideRects)

        dst2 = vbPalette(backP * 255 / topRects.Count)

        hotPoints.ptHot.histSide.dst3.ConvertTo(histogramSide, cv.MatType.CV_32F)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histogramSide, dst3, task.rangesSide)

        dst3 = vbPalette(dst3 * 255 / sideRects.Count)

        classCount = topRects.Count + sideRects.Count

        If task.mouseClickFlag Then selectedPoint = task.clickPoint
        If task.heartBeat Then labels(2) = CStr(topRects.Count) + " objects were identified in the top view."
        If task.heartBeat Then labels(3) = CStr(sideRects.Count) + " objects were identified in the side view."
    End Sub
End Class







Public Class GuidedBP_Lookup : Inherits VB_Algorithm
    Dim guided As New GuidedBP_Basics
    Public Sub New()
        task.clickPoint = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        desc = "Given a point cloud pixel, look up which object it is in.  Click in the Depth RGB image to test."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        guided.Run(src)
        dst2 = guided.dst2
        labels(2) = guided.labels(2)


    End Sub
End Class







Public Class GuidedBP_Depth : Inherits VB_Algorithm
    Public hist As New PointCloud_Histograms
    Dim myPalette As New Palette_Random
    Public classCount As Integer
    Public Sub New()
        gOptions.HistBinSlider.Value = 16
        desc = "Backproject the 2D histogram of depth for selected channels to discretize the depth data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        hist.Run(src)

        Dim histArray(hist.histogram.Total - 1) As Single
        Marshal.Copy(hist.histogram.Data, histArray, 0, histArray.Length)

        Dim histList = histArray.ToList
        histArray(histList.IndexOf(histList.Max)) = 0

        Dim sortedHist As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)

        For i = 0 To histArray.Count - 1
            sortedHist.Add(histArray(i), i)
        Next

        classCount = 0
        Dim count As Integer
        Dim newSamples(histArray.Count - 1) As Single
        For i = 0 To sortedHist.Count - 1
            Dim index = sortedHist.ElementAt(i).Value
            count += sortedHist.ElementAt(i).Key
            newSamples(index) = classCount
            classCount += 1
            If classCount >= 255 Then Exit For
        Next

        Marshal.Copy(newSamples, 0, hist.histogram.Data, newSamples.Length)

        cv.Cv2.CalcBackProject({src}, redOptions.channels, hist.histogram, dst2, redOptions.ranges)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)

        If standaloneTest() Then
            labels(3) = "Note that colors are shifting because this is before any matching."
            dst2 += 1
            dst2.SetTo(0, task.noDepthMask)
            myPalette.Run(dst2)
            dst3 = myPalette.dst2
        End If

        Dim depthCount = task.depthMask.CountNonZero
        labels(2) = CStr(classCount) + " regions detected in the backprojection - " + Format(count / depthCount, "0%") + " of depth data"
    End Sub
End Class