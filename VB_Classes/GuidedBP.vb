Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class GuidedBP_Basics : Inherits VB_Algorithm
    Public ptHot As New GuidedBP_HotPoints
    Dim topMap As New cv.Mat, sideMap As New cv.Mat
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
        For Each entry In sortRects
            Dim r = entry.Value
            Dim pt = New cv.Point(CInt(r.X + r.Width / 2), CInt(r.Y + r.Height / 2))
            Dim index = map.Get(Of Byte)(pt.Y, pt.X)
            If index = 0 Or indices.Contains(index) Then
                If index = ptList.Count Then index = ptList.Count + 1 Else index = ptList.Count
            End If
            ptList.Add(pt)
            indices.Add(index)
        Next

        map.SetTo(0)
        For Each entry In sortRects
            Dim r = entry.Value
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






Public Class GuidedBP_CellHistograms : Inherits VB_Algorithm
    Dim gpbWare As New GuidedBP_Hulls
    Dim mats As New Mat_4Click
    Dim plot As New Plot_Histogram
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        plot.createHistogram = True
        mats.mats.lineSeparators = False

        labels = {"", "Selected histogram in lower left image", "", ""}
        desc = "Display known stats and features of an individual cell in the KWhere data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst0 = src
        gpbWare.Run(src)
        dst3 = gpbWare.dst2

        Dim gbp As New gbpData
        If gpbWare.gbpCells.Count > 1 Then
            gbp = showSelectionGBP(gpbWare.gbpCells, gpbWare.kMap)
            vbDrawContour(dst3, gbp.contour, gbp.color)
        End If

        If heartBeat() Then
            If gbp.index <> 0 Then
                Dim zeroMat = Not gbp.mask
                labels(2) = "Histograms for "
                For i = 0 To 2
                    plot.minRange = Choose(i + 1, -task.xRange, -task.yRange, 0)
                    plot.maxRange = Choose(i + 1, task.xRange, task.yRange, task.maxZmeters)
                    Dim prefix = Choose(i + 1, "X ", "Y ", "Z ")
                    labels(2) += " " + prefix + " (" + Format(plot.minRange, fmt1) + ", " + Format(plot.maxRange, fmt1) + ")"
                    If i = task.quadrantIndex Then
                        labels(1) = "histogram range (" + Format(plot.minRange, fmt1) + ", " + Format(plot.maxRange, fmt1) + ")"
                    End If
                    task.pcSplit(i).SetTo(0, zeroMat)
                    plot.Run(task.pcSplit(i))
                    mats.mat(i) = plot.dst2.Clone
                Next
                dst3.SetTo(cv.Scalar.White, gbp.mask)
            End If
        End If

        If gbp.index <> 0 Then
            vbDrawContour(dst0, gbp.contour, cv.Scalar.Yellow, task.lineWidth)
        End If

        mats.Run(Nothing)
        dst2 = mats.dst2
        dst1 = mats.dst3

        labels(3) = CStr(gpbWare.gbpCells.Count) + " objects detected - click to highlight"
        If heartBeat() And gbp.index <> 0 Then
            strOut = "Select a cell in image at right to see actual ranges " + vbCrLf +
                     "X min = " + Format(gbp.mmX.minVal, fmt1) + " X max = " + Format(gbp.mmX.maxVal, fmt1) + vbCrLf +
                     "Y min = " + Format(gbp.mmY.minVal, fmt1) + " Y max = " + Format(gbp.mmY.maxVal, fmt1) + vbCrLf +
                     "Z min = " + Format(gbp.mmZ.minVal, fmt1) + " Z max = " + Format(gbp.mmZ.maxVal, fmt1)
        End If

        Dim picTag = 2
        If task.quadrantIndex = 3 Then
            dst1.SetTo(0)
            picTag = 1
        End If
        setTrueText(strOut, New cv.Point(dst2.Width / 2 + 2, dst2.Height / 2 + 2), picTag)
        dst2.Line(New cv.Point(0, dst2.Height / 2), New cv.Point(dst2.Width, dst2.Height / 2), cv.Scalar.White, task.lineWidth)
        dst2.Line(New cv.Point(dst2.Width / 2, 0), New cv.Point(dst2.Width / 2, dst2.Height), cv.Scalar.White, task.lineWidth)
    End Sub
End Class








Public Class GuidedBP_Cells : Inherits VB_Algorithm
    Dim gpbWare As New GuidedBP_Hulls
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "", "Output of GuidedBP_Hulls", "Ranges for the point cloud data"}
        desc = "Display ranges for X, Y, and Z for the selected KWhere cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        gpbWare.Run(src)
        dst2 = gpbWare.dst2

        Dim gbp As New gbpData
        If gpbWare.gbpCells.Count > 1 Then
            gbp = showSelectionGBP(gpbWare.gbpCells, gpbWare.kMap)
            vbDrawContour(dst2, gbp.contour, gbp.color)
        End If

        Static strOut As String
        If heartBeat() Then
            strOut = "Range for X min/max" + vbTab + Format(gbp.mmX.minVal, fmt1) + "/" + Format(gbp.mmX.maxVal, fmt1) + vbCrLf
            strOut += "Range for Y min/max" + vbTab + Format(gbp.mmY.minVal, fmt1) + "/" + Format(gbp.mmY.maxVal, fmt1) + vbCrLf
            strOut += "Range for Z min/max" + vbTab + Format(gbp.mmZ.minVal, fmt1) + "/" + Format(gbp.mmZ.maxVal, fmt1) + vbCrLf

            dst1 = gbp.mask
        End If
        setTrueText(strOut, 3)
    End Sub
End Class





Public Class GuidedBP_kTop : Inherits VB_Algorithm
    Dim autoX As New OpAuto_XRange
    Public colorC As New RedCloud_ColorAndCloud
    Dim contours As New Contour_Largest
    Dim hist2d As New Histogram2D_Top
    Public Sub New()
        gOptions.useHistoryCloud.Checked = False
        labels(3) = "Back projection of the top view"
        desc = "Subdivide the OpAuto_XRange output using RedCloud_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)

        autoX.Run(hist2d.histogram)

        dst1 = autoX.histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        colorC.Run(dst1)
        dst2 = colorC.dst2

        dst3.SetTo(0)
        For Each rp In colorC.rMin.minCells
            Dim histogram As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
            autoX.histogram(rp.rect).CopyTo(histogram(rp.rect), rp.mask)
            cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histogram, dst0, task.rangesTop)
            Dim mask = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            contours.Run(mask)
            setTrueText(CStr(rp.index), rp.maxDist)
            vbDrawContour(dst3, contours.bestContour, rp.color, -1)
        Next

        Static saveTrueData As List(Of trueText)
        If heartBeat() Then saveTrueData = New List(Of trueText)(trueData)
        trueData = New List(Of trueText)(saveTrueData)

        labels(2) = colorC.labels(2)
        setTrueText("camera at top", New cv.Point(dst2.Width * 2 / 3, 0), 2)
    End Sub
End Class





Public Class GuidedBP_kSide : Inherits VB_Algorithm
    Dim autoY As New OpAuto_YRange
    Public hist2d As New Histogram2D_Side
    Public colorC As New RedCloud_ColorAndCloud
    Dim contours As New Contour_Largest
    Public Sub New()
        gOptions.useHistoryCloud.Checked = False
        labels(3) = "Back projection of the top view"
        desc = "Subdivide the GuidedBP_HistogramSide output using RedCloud_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)
        autoY.Run(hist2d.histogram)

        dst1 = autoY.histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        colorC.Run(dst1)
        dst2 = colorC.dst2

        dst3.SetTo(0)
        For Each rp In colorC.rMin.minCells
            Dim histogram As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
            autoY.histogram(rp.rect).CopyTo(histogram(rp.rect), rp.mask)
            cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histogram, dst0, task.rangesSide)
            Dim mask = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            contours.Run(mask)
            setTrueText(CStr(rp.index), rp.maxDist)
            vbDrawContour(dst3, contours.bestContour, rp.color, -1)
        Next

        Static saveTrueData As List(Of trueText)
        If heartBeat() Then saveTrueData = New List(Of trueText)(trueData)
        trueData = New List(Of trueText)(saveTrueData)

        labels(2) = colorC.labels(2)
        setTrueText("camera at side", New cv.Point(0, dst2.Height / 3), 2)
    End Sub
End Class







Public Class GuidedBP_kTopSide : Inherits VB_Algorithm
    Dim kTop As New GuidedBP_kTop
    Dim kSide As New GuidedBP_kSide
    Public Sub New()
        labels = {"", "", "Objects found in the side view", "Objects found in the top view"}
        desc = "Find objects with the top view histogram, remove that pointcloud data, then find objects in the side view."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        kTop.Run(src)
        dst3 = kTop.dst2

        task.pointCloud.SetTo(0, dst3)
        kSide.Run(task.pointCloud)
        dst2 = kSide.dst2
    End Sub
End Class





Public Class GuidedBP_ObjectStats : Inherits VB_Algorithm
    Dim kObj As New GuidedBP_Objects
    Dim stats As New Cell_Basics
    Public Sub New()
        stats.runRedCloud = True
        labels(1) = "Compartments for each object"
        desc = "Compartmentalize the RedCloud_Basics cells so they stay near the objects detected."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        kObj.Run(src)
        dst2 = kObj.dst2

        stats.Run(kObj.dst1)
        dst0 = stats.dst0
        dst1 = stats.dst1
        labels(2) = stats.labels(2)
        setTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class GuidedBP_HotPointsKNN : Inherits VB_Algorithm
    Dim ptHot As New GuidedBP_HotPoints
    Dim knnSide As New KNN_Basics
    Dim knnTop As New KNN_Basics
    Public Sub New()
        desc = "Correlate the hot points with the previous generation to ID each object"
    End Sub
    Private Sub runKNN(knn As KNN_Basics, rectList As List(Of cv.Rect), dst As cv.Mat, dstindex As Integer)
        knn.queries.Clear()
        For Each r In rectList
            knn.queries.Add(New cv.Point2f(CSng(r.X + r.Width / 2), CSng(r.Y + r.Height / 2)))
        Next

        If firstPass Then knn.trainInput = New List(Of cv.Point2f)(knn.queries)

        knn.Run(Nothing)

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
    Public hotTop As New Histogram2D_Top
    Public hotSide As New Histogram2D_Side
    Public topRects As New List(Of cv.Rect)
    Public sideRects As New List(Of cv.Rect)
    Dim mask As New cv.Mat, floodRect As cv.Rect
    Dim ptList As New List(Of cv.Point)
    Dim rectList As New List(Of cv.Rect)
    Public Sub New()
        task.useXYRange = False
        mask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        floodRect = New cv.Rect(1, 1, dst2.Width - 2, dst2.Height - 2)
        desc = "Use floodfill to identify all the objects in both the top and side views."
    End Sub
    Private Function hotPoints(ByRef view As cv.Mat) As SortedList(Of Integer, Integer)
        Dim rect As cv.Rect
        Dim input = view(floodRect).Clone
        Dim points = input.FindNonZero()

        Dim viewList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        rectList.Clear()
        ptList.Clear()
        mask.SetTo(0)
        Dim minPixels = gOptions.minPixelsSlider.Value
        For i = 0 To points.Rows - 1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            Dim count = input.FloodFill(mask, pt, 255, rect, 0, 0, 4 Or cv.FloodFillFlags.MaskOnly Or (255 << 8))
            If count >= minPixels Then
                viewList.Add(count, rectList.Count)
                rectList.Add(New cv.Rect(rect.X + 1, rect.Y + 1, rect.Width + 1, rect.Height + 1))
                ptList.Add(pt)
            End If
        Next

        mask.SetTo(0)
        Dim viewCount = 1
        For Each entry In viewList
            Dim pt = ptList(entry.Value)
            input.FloodFill(mask, pt, viewCount, rect, 0, 0, 4 Or cv.FloodFillFlags.Link4 Or (255 << 8))
            viewCount += 1
        Next

        input.CopyTo(view(floodRect))
        view = view.SetTo(0, view.InRange(255, 255))
        Return viewList
    End Function
    Public Sub RunVB(src As cv.Mat)
        hotTop.Run(src)
        Dim topList = hotPoints(hotTop.dst3)

        dst2 = vbPalette(hotTop.dst3 * 255 / topList.Count)

        topRects = New List(Of cv.Rect)(rectList)

        hotSide.Run(src)
        Dim sideList = hotPoints(hotSide.dst3)

        dst3 = vbPalette(hotSide.dst3 * 255 / sideList.Count)

        sideRects = New List(Of cv.Rect)(rectList)

        If topList.Count < 8 And redOptions.TopViewThreshold.Value > redOptions.TopViewThreshold.Minimum Then redOptions.TopViewThreshold.Value -= 1
        If topList.Count > 15 And redOptions.TopViewThreshold.Value < redOptions.TopViewThreshold.Maximum Then redOptions.TopViewThreshold.Value += 1
        If sideList.Count < 8 And redOptions.SideViewThreshold.Value > redOptions.SideViewThreshold.Minimum Then redOptions.SideViewThreshold.Value -= 1
        If sideList.Count > 15 And redOptions.SideViewThreshold.Value < redOptions.SideViewThreshold.Maximum Then redOptions.SideViewThreshold.Value += 1

        If heartBeat() Then labels(2) = CStr(topList.Count) + " objects were identified in the top view."
        If heartBeat() Then labels(3) = CStr(sideList.Count) + " objects were identified in the side view."
    End Sub
End Class








Public Class GuidedBP_PlanesPlot : Inherits VB_Algorithm
    Dim hotSide As New Histogram2D_Side
    Public Sub New()
        labels = {"", "", "Side view", "Plot of nonzero rows in the side view"}
        desc = "Plot the likely floor or ceiling areas."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hotSide.Run(src)
        dst2 = hotSide.dst3

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






Public Class GuidedBP_Depth : Inherits VB_Algorithm
    Public hist As New PointCloud_Histograms
    Dim myPalette As New Palette_Random
    Public classCount As Integer
    Public givenClassCount As Integer
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
        Dim minPixels = gOptions.minPixelsSlider.Value
        Dim maxClassCount = 255 - givenClassCount - 1
        For i = 0 To sortedHist.Count - 1
            Dim index = sortedHist.ElementAt(i).Value
            count += sortedHist.ElementAt(i).Key
            newSamples(index) = classCount
            classCount += 1
            ' if we have 95% of the pixels, good enough...
            ' But leave room for the color classcount (usually < 10)
            If count / src.Total > redOptions.imageThresholdPercent Or classCount >= maxClassCount Then Exit For
        Next

        Marshal.Copy(newSamples, 0, hist.histogram.Data, newSamples.Length)

        cv.Cv2.CalcBackProject({src}, redOptions.channels, hist.histogram, dst2, redOptions.rangesCloud)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        ' dst2.SetTo(254, task.depthOutline)
        If standalone Or testIntermediate(traceName) Then
            labels(3) = "Note that colors are shifting because this is before RedCloud matching."
            dst2 += 1
            dst2.SetTo(0, task.noDepthMask)
            myPalette.Run(dst2)
            dst3 = myPalette.dst2
        End If

        labels(2) = CStr(classCount) + " regions detected in the backprojection - " + Format(count / src.Total, "0%")
    End Sub
End Class





Public Class GuidedBP_Hulls : Inherits VB_Algorithm
    Dim bpDoctor As New GuidedBP_Points
    Public gbpCells As New List(Of gbpData)
    Public gbpCellsLast As New List(Of gbpData)
    Public kMap As New cv.Mat
    Dim contours As New Contour_Largest
    Dim plot As New Histogram_Depth
    Public Sub New()
        kMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Top X identified objects."
        desc = "Find and display k objects using doctored back projection."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static saveTtext As List(Of trueText)
        If task.paused Then Exit Sub

        ' Use the DebugCheckBox box and it will rerun the exact same input.
        If gOptions.DebugCheckBox.Checked = False Then bpDoctor.Run(src)

        Dim newCells As New SortedList(Of Integer, gbpData)(New compareAllowIdenticalIntegerInverted)
        Dim kMapLast = kMap.Clone
        If heartBeat() Then
            kMap.SetTo(0)
            dst2.SetTo(0)
            Dim sizes As New List(Of Integer)
            For i = 1 To bpDoctor.classCount
                Dim kw As New gbpData
                kw.mask = bpDoctor.backP.InRange(i, i).Threshold(0, 255, cv.ThresholdTypes.Binary)

                contours.Run(kw.mask)
                kw.contour = contours.bestContour
                kw.hull = cv.Cv2.ConvexHull(kw.contour.ToArray, True).ToList

                vbDrawContour(kw.mask, kw.contour, 255, -1)
                kw.maxDist = vbHullCenter(kw.hull)
                Dim validate = kw.mask.Get(Of Byte)(kw.maxDist.Y, kw.maxDist.X)
                If validate = 0 Then kw.maxDist = vbGetMaxDist(kw.mask)

                Dim mask = kw.mask And task.depthMask
                kw.mmX = vbMinMax(task.pcSplit(0), mask)
                kw.mmY = vbMinMax(task.pcSplit(1), mask)
                kw.mmZ = vbMinMax(task.pcSplit(2), mask)
                kw.size = kw.mask.CountNonZero()
                If sizes.Contains(kw.size) = False Then
                    newCells.Add(kw.size, kw)
                    sizes.Add(kw.size)
                End If
                kw.color = randomCellColor()
                vbDrawContour(dst2, kw.contour, kw.color, -1)
            Next
            dst0 = task.color.Clone
            gbpCellsLast = New List(Of gbpData)(gbpCells)
            gbpCells.Clear()
            gbpCells.Add(New gbpData)

            Dim cellUpdates As New List(Of gbpData)
            Dim usedColors As New List(Of cv.Vec3b)({black})
            For Each entry In newCells
                Dim kw As New gbpData
                kw = entry.Value

                kw.index = gbpCells.Count
                Dim index = kMapLast.Get(Of Byte)(kw.maxDist.Y, kw.maxDist.X)
                Dim lkw As gbpData
                If index <> 0 Then
                    lkw = gbpCellsLast(index)
                    kw.indexLast = lkw.index
                    lkw.index = gbpCells.Count
                    kw.color = lkw.color
                End If

                If usedColors.Contains(kw.color) Then kw.color = randomCellColor()

                vbDrawContour(kMap, kw.hull, kw.index, -1)
                vbDrawContour(dst2, kw.contour, kw.color, -1)

                gbpCells.Add(kw)
                usedColors.Add(kw.color)
                If gbpCells.Count > 20 Then Exit For ' more than 20 objects?  Likely small artifacts...
            Next
            strOut = "Index" + vbTab + "Size" + vbTab + "Depth Range" + vbTab
            strOut += "Hull" + vbTab + vbTab + vbTab + vbCrLf
            For Each kw In gbpCells
                If kw.index = 0 Then Continue For
                strOut += CStr(kw.index) + vbTab + Format(kw.size, fmt0) + vbTab
                strOut += Format(kw.mmZ.minVal, fmt2) + "/" + Format(kw.mmZ.maxVal, fmt2) + vbTab
                strOut += hullStr(kw.hull) + vbCrLf
            Next
            setTrueText(strOut, 3)

            For Each kw In gbpCells
                If kw.index = 0 Then Continue For
                setTrueText(CStr(kw.index), kw.maxDist, 2)
                dst2.Circle(kw.maxDist, task.dotSize, cv.Scalar.White, -1, task.lineType)
            Next

            saveTtext = New List(Of trueText)(trueData)
        End If

        Dim gbp As New gbpData
        If gbpCells.Count > 1 Then
            plot.gbp = showSelectionGBP(gbpCells, kMap)
            vbDrawContour(dst2, gbp.contour, gbp.color)
        End If

        plot.Run(task.pcSplit(2))
        dst1 = plot.dst2

        trueData = New List(Of trueText)(saveTtext)
        labels(2) = "There were " + CStr(gbpCells.Count) + " objects identified in the image."
    End Sub
End Class





Public Class GuidedBP_Objects : Inherits VB_Algorithm
    Dim kHist As New GuidedBP_History
    Dim reduction As New Reduction_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Create the input to RedCloud_Basics combining color and GuidedBP_Hulls output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        kHist.Run(src)
        dst2 = kHist.gpbWare.dst2
        setTrueText(kHist.strOut, 3)

        reduction.Run(src)

        dst1.SetTo(0)
        For Each kw In kHist.gbpCells
            If kw.index = 0 Then Continue For
            vbDrawContour(dst1, kw.contour, kw.index + reduction.classCount, -1)
            setTrueText(CStr(kw.index), kw.maxDist, 2)
        Next

        If kHist.gpbWare.gbpCells.Count > 1 Then
            Dim gbp = showSelectionGBP(kHist.gpbWare.gbpCells, kHist.gpbWare.kMap)
            vbDrawContour(dst2, gbp.hull, cv.Scalar.White, task.lineWidth)
        End If

        reduction.dst2.SetTo(0, dst1)
        dst1 += reduction.dst2

        labels(2) = CStr(kHist.gbpCells.Count) + " objects found"
    End Sub
End Class






Public Class GuidedBP_History : Inherits VB_Algorithm
    Public gpbWare As New GuidedBP_Hulls
    Dim kCellList As New List(Of List(Of gbpData))
    Public gbpCells As New List(Of gbpData)
    Public kMap As New cv.Mat
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Minimum distance between cells (cm)", 10, 100, 50)
            sliders.setupTrackBar("Minimum size kCell", 1, dst2.Total / 10, dst2.Total / 100)
        End If

        kMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Find the cells that are consistently present over time."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static minSlider = findSlider("Minimum size kCell")
        Static distSlider = findSlider("Minimum distance between cells (cm)")
        Dim minSize = minSlider.value
        Dim distanceMin = distSlider.value / 100

        If task.optionsChanged Then kCellList.Clear()

        gpbWare.Run(src)

        kCellList.Add(New List(Of gbpData)(gpbWare.gbpCells))

        Dim sortCells As New SortedList(Of Integer, gbpData)(New compareAllowIdenticalIntegerInverted)
        For Each cellList In kCellList
            For i = 1 To cellList.Count - 1
                sortCells.Add(cellList(i).size, cellList(i))
            Next
        Next

        kMap.SetTo(0)
        gbpCells.Clear()
        gbpCells.Add(New gbpData)
        Dim lastDst2 = dst2.Clone
        Dim usedColors As New List(Of cv.Vec3b)({black})
        For Each entry In sortCells
            Dim kw = entry.Value
            Dim index = kMap.Get(Of Byte)(kw.maxDist.Y, kw.maxDist.X)
            Dim lkw = gbpCells(index)
            If Math.Abs((kw.mmZ.minVal + kw.mmZ.maxVal) / 2 - (lkw.mmZ.minVal + lkw.mmZ.maxVal) / 2) > distanceMin Then
                If kw.size >= minSize Then
                    kw.index = gbpCells.Count
                    Dim color = lastDst2.Get(Of cv.Vec3b)(kw.maxDist.Y, kw.maxDist.X)
                    If usedColors.Contains(color) = False Then kw.color = color
                    vbDrawContour(kMap, kw.contour, kw.index, -1)
                    gbpCells.Add(kw)
                    usedColors.Add(kw.color)
                End If
            End If
        Next

        dst2.SetTo(0)
        For Each kw In gbpCells
            vbDrawContour(dst2, kw.contour, kw.color, -1)
            setTrueText(CStr(kw.index), kw.maxDist, 2)
        Next

        setTrueText(gpbWare.strOut, 3)

        If gbpCells.Count > 1 Then
            Dim gbp = showSelectionGBP(gbpCells, kMap)
            vbDrawContour(dst2, gbp.hull, cv.Scalar.White, task.lineWidth)
            vbDrawContour(task.color, gbp.contour, cv.Scalar.Yellow)
        End If


        If kCellList.Count >= task.historyCount Then kCellList.RemoveAt(0)
        If heartBeat() Then labels(2) = CStr(gbpCells.Count) + " objects were consistently present"
    End Sub
End Class






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

        hotPoints.ptHot.hotTop.dst3.ConvertTo(histogramTop, cv.MatType.CV_32F)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histogramTop, backP, task.rangesTop)

        topRects = New List(Of cv.Rect)(hotPoints.ptHot.topRects)
        sideRects = New List(Of cv.Rect)(hotPoints.ptHot.sideRects)

        dst2 = vbPalette(backP * 255 / topRects.Count)

        hotPoints.ptHot.hotSide.dst3.ConvertTo(histogramSide, cv.MatType.CV_32F)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histogramSide, dst3, task.rangesSide)

        dst3 = vbPalette(dst3 * 255 / sideRects.Count)

        classCount = topRects.Count + sideRects.Count

        If task.mouseClickFlag Then selectedPoint = task.clickPoint
        If heartBeat() Then labels(2) = CStr(topRects.Count) + " objects were identified in the top view."
        If heartBeat() Then labels(3) = CStr(sideRects.Count) + " objects were identified in the side view."
    End Sub
End Class
