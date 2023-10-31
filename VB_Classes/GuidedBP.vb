Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class GuidedBP_Basics : Inherits VB_Algorithm
    Dim heatTop As New Histogram2D_Top
    Public classCount As Integer
    Public fCell As New RedCell_Basics
    Public Sub New()
        labels(3) = "Threshold of Top View"
        desc = "Use floodfill to identify all the objects in the selected view then build a backprojection that identifies k objects in the image view."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        heatTop.Run(src)

        dst3 = heatTop.histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        task.minPixels = 1
        fCell.Run(dst3)

        Dim doctoredHist32f As New cv.Mat
        fCell.dst3.ConvertTo(doctoredHist32f, cv.MatType.CV_32F)
        classCount = task.fCells.Count

        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, doctoredHist32f, dst1, task.rangesTop)
        dst1 = dst1.ConvertScaleAbs()

        dst2 = vbPalette(dst1 * 255 / classCount)

        labels(2) = "Backprojection of both top and side views contains " + CStr(classCount) + " objects"
    End Sub
End Class






Public Class GuidedBP_Cells : Inherits VB_Algorithm
    Dim bpDoctor As New GuidedBP_Basics
    Public kCells As New List(Of kwData)
    Dim contours As New Contour_Largest
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Build the kCells for the current point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bpDoctor.Run(src)

        Dim newCells As New SortedList(Of Integer, kwData)(New compareAllowIdenticalIntegerInverted)
        For i = 1 To bpDoctor.classCount
            Dim kw As New kwData
            kw.mask = bpDoctor.dst1.InRange(i, i).Threshold(0, 255, cv.ThresholdTypes.Binary)

            contours.Run(kw.mask)
            kw.contour = contours.bestContour
            kw.hull = cv.Cv2.ConvexHull(kw.contour.ToArray, True).ToList

            kw.mask.SetTo(0)
            vbDrawContour(kw.mask, kw.contour, 255, -1)
            kw.maxDist = vbGetMaxDist(kw.mask)

            kw.mmX = vbMinMax(task.pcSplit(0), kw.mask)
            kw.mmY = vbMinMax(task.pcSplit(1), kw.mask)
            kw.mmZ = vbMinMax(task.pcSplit(2), kw.mask)
            kw.size = kw.mask.CountNonZero()

            ' only keep the object if it is larger that one half of 1% of the image.
            If kw.size >= dst2.Total * 0.005 Then newCells.Add(kw.size, kw)
        Next

        kCells.Clear()
        kCells.Add(New kwData)
        dst1.SetTo(0)
        Dim lastDst2 = dst2.Clone
        dst2.SetTo(0)
        For i = 0 To newCells.Count - 1
            Dim kw = newCells.ElementAt(i).Value
            kw.color = lastDst2.Get(Of cv.Vec3b)(kw.maxDist.Y, kw.maxDist.X)
            If kw.color = black Then
                kw.color = New cv.Vec3b(msRNG.Next(30, 240), msRNG.Next(30, 240), msRNG.Next(30, 240))
            End If
            kw.index = kCells.Count

            kCells.Add(kw)
            vbDrawContour(dst1, kw.contour, kw.index, -1)
            vbDrawContour(dst2, kw.contour, kw.color, -1)
            dst3.Circle(kw.maxDist, task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
            dst3.Circle(kw.maxDist, task.dotSize, cv.Scalar.Black, -1, task.lineType)
            setTrueText(CStr(kw.index), kw.maxDist, 2)
        Next

        Dim index = dst1.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
        Dim kwx = kCells(index)
        labels(3) = "Selected cell = " + CStr(kwx.index)

        dst3 = src
        If kwx.index > 0 Then dst3.SetTo(cv.Scalar.White, kwx.mask)

        For Each kw In kCells
            dst3.Circle(kw.maxDist, task.dotSize + 1, cv.Scalar.White, -1, task.lineType)
            dst3.Circle(kw.maxDist, task.dotSize, cv.Scalar.Black, -1, task.lineType)
        Next
        labels(2) = traceName + " identified " + CStr(kCells.Count) + " cells"
    End Sub
End Class






Public Class GuidedBP_CellHistograms : Inherits VB_Algorithm
    Dim kWare As New GuidedBP_Hulls
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
        kWare.Run(src)
        dst3 = kWare.dst2

        Dim kw = kWare.kwShowSelected(kWare.kCells, kWare.kMap, dst3)

        If heartBeat() Or task.mouseClickFlag Then
            If kw.index <> 0 Then
                Dim zeroMat = Not kw.mask
                labels(2) = "Histograms for "
                For i = 0 To 2
                    plot.minRange = Choose(i + 1, -task.xRange, -task.yRange, 0)
                    plot.maxRange = Choose(i + 1, task.xRange, task.yRange, task.maxZmeters)
                    Dim prefix = Choose(i + 1, "X ", "Y ", "Z ")
                    labels(2) += prefix + " (" + Format(plot.minRange, fmt1) + ", " + Format(plot.maxRange, fmt1) + ")"
                    If i = task.quadrantIndex Then
                        labels(1) = "histogram range (" + Format(plot.minRange, fmt1) + ", " + Format(plot.maxRange, fmt1) + ")"
                    End If
                    task.pcSplit(i).SetTo(0, zeroMat)
                    plot.Run(task.pcSplit(i))
                    mats.mat(i) = plot.dst2.Clone
                Next
                mats.mat(3) = kw.mask
            End If
        End If

        If kw.index <> 0 Then
            vbDrawContour(dst0, kw.contour, cv.Scalar.Yellow, task.lineWidth)
        End If

        mats.Run(Nothing)
        dst2 = mats.dst2
        dst1 = mats.dst3

        labels(3) = CStr(kWare.kCells.Count) + " objects detected - click to highlight"
        If heartBeat() And kw.index <> 0 Then
            strOut = "Select a cell in image at right to see actual ranges " + vbCrLf +
                     "X min = " + Format(kw.mmX.minVal, fmt1) + " X max = " + Format(kw.mmX.maxVal, fmt1) + vbCrLf +
                     "Y min = " + Format(kw.mmY.minVal, fmt1) + " Y max = " + Format(kw.mmY.maxVal, fmt1) + vbCrLf +
                     "Z min = " + Format(kw.mmZ.minVal, fmt1) + " Z max = " + Format(kw.mmZ.maxVal, fmt1)
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








Public Class GuidedBP_CellRanges : Inherits VB_Algorithm
    Dim kWare As New GuidedBP_Hulls
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "", "Output of GuidedBP_Hulls", "Ranges for the point cloud data"}
        desc = "Display ranges for X, Y, and Z for the selected KWhere cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        kWare.Run(src)
        dst2 = kWare.dst2

        Dim kw As kwData
        If heartBeat() Then kw = kWare.kwShowSelected(kWare.kCells, kWare.dst3, dst1)

        Static strOut As String
        If heartBeat() Then
            strOut = "Range for X min/max" + vbTab + Format(kw.mmX.minVal, fmt1) + "/" + Format(kw.mmX.maxVal, fmt1) + vbCrLf
            strOut += "Range for Y min/max" + vbTab + Format(kw.mmY.minVal, fmt1) + "/" + Format(kw.mmY.maxVal, fmt1) + vbCrLf
            strOut += "Range for Z min/max" + vbTab + Format(kw.mmZ.minVal, fmt1) + "/" + Format(kw.mmZ.maxVal, fmt1) + vbCrLf

            dst1 = kw.mask
        End If
        setTrueText(strOut, 3)
    End Sub
End Class





Public Class GuidedBP_kTop : Inherits VB_Algorithm
    Dim autoX As New OpAuto_XRange
    Public colorC As New RedBP_ColorAndCloud
    Dim contours As New Contour_Largest
    Dim hist2d As New Histogram2D_Top
    Public Sub New()
        gOptions.useHistoryCloud.Checked = False
        labels(3) = "Back projection of the top view"
        desc = "Subdivide the OpAuto_XRange output using RedBP_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)

        autoX.Run(hist2d.histogram)

        dst1 = autoX.histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        colorC.Run(dst1)
        dst2 = colorC.dst2

        dst3.SetTo(0)
        For Each fc In task.fCells
            Dim histogram As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
            autoX.histogram(fc.rect).CopyTo(histogram(fc.rect), fc.mask)
            cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histogram, dst0, task.rangesTop)
            Dim mask = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            contours.Run(mask)
            setTrueText(CStr(fc.index), fc.maxDist)
            vbDrawContour(dst3, contours.bestContour, fc.color, -1)
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
    Public colorC As New RedBP_ColorAndCloud
    Dim contours As New Contour_Largest
    Public Sub New()
        gOptions.useHistoryCloud.Checked = False
        labels(3) = "Back projection of the top view"
        desc = "Subdivide the GuidedBP_HistogramSide output using RedBP_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)
        autoY.Run(hist2d.histogram)

        dst1 = autoY.histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        colorC.Run(dst1)
        dst2 = colorC.dst2

        dst3.SetTo(0)
        For Each fc In task.fCells
            Dim histogram As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
            autoY.histogram(fc.rect).CopyTo(histogram(fc.rect), fc.mask)
            cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histogram, dst0, task.rangesSide)
            Dim mask = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            contours.Run(mask)
            setTrueText(CStr(fc.index), fc.maxDist)
            vbDrawContour(dst3, contours.bestContour, fc.color, -1)
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







Public Class GuidedBP_kCellStats : Inherits VB_Algorithm
    Dim kTopSide As New GuidedBP_kTopSide
    Dim stats As New RedBP_CellStats
    Public Sub New()
        stats.redC = New RedBP_Basics
        desc = "Display all the stats for a RedColor cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        kTopSide.Run(src)
        dst2 = kTopSide.dst2

        stats.Run(dst2)

        setTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class GuidedBP_DelaunayStats : Inherits VB_Algorithm
    Dim delaunay As New GuidedBP_Delaunay
    Dim reduction As New Reduction_Basics
    Dim stats As New RedBP_CellStats
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        stats.redC = New RedBP_Basics
        labels(1) = "Compartments for each object"
        desc = "Compartmentalize the RedBP_Basics cells so they stay near the objects detected."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)

        delaunay.Run(src)
        dst2 = delaunay.dst2

        dst0 = reduction.dst0 + delaunay.dst3

        stats.Run(dst0)
        dst1 = stats.dst1
        labels(2) = stats.labels(2)
        setTrueText(stats.strOut, 3)
    End Sub
End Class







Public Class GuidedBP_Delaunay : Inherits VB_Algorithm
    Public kWare As New GuidedBP_Hulls
    Dim delaunay As New Delaunay_Basics
    Public kCells As New List(Of kwData)
    Dim colorC As New RedBP_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Use Delaunay to create regions from objects"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If heartBeat() Then
            kWare.Run(src)
            dst2 = kWare.dst2

            Dim usedColors As New List(Of cv.Vec3b)
            kCells.Clear()
            delaunay.inputPoints.Clear()
            usedColors.Clear()
            For Each kw In kWare.kCells
                delaunay.inputPoints.Add(kw.maxDist)
                kCells.Add(kw)
            Next
            delaunay.Run(Nothing)
            dst3 = delaunay.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
    End Sub
End Class





Public Class GuidedBP_ObjectStats : Inherits VB_Algorithm
    Dim kObj As New GuidedBP_Objects
    Dim stats As New RedBP_CellStats
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        stats.redC = New RedBP_Basics
        labels(1) = "Compartments for each object"
        desc = "Compartmentalize the RedBP_Basics cells so they stay near the objects detected."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        kObj.Run(src)
        dst2 = kObj.dst2

        stats.Run(kObj.dst1)
        dst1 = stats.dst1
        labels(2) = stats.labels(2)
        setTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class GuidedBP_Objects : Inherits VB_Algorithm
    Dim kHist As New GuidedBP_History
    Dim reduction As New Reduction_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Create the input to RedBP_Basics combining color and GuidedBP_Hulls output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        kHist.Run(src)
        dst2 = kHist.kWare.dst2
        setTrueText(kHist.strOut, 3)

        reduction.Run(src)

        dst1.SetTo(0)
        For Each kw In kHist.kCells
            If kw.index = 0 Then Continue For
            vbDrawContour(dst1, kw.contour, kw.index + reduction.classCount, -1)
            setTrueText(CStr(kw.index), kw.maxDist, 2)
        Next

        Dim kwX = kHist.kWare.kwShowSelected(kHist.kCells, kHist.kMap, dst2)
        vbDrawContour(dst2, kwX.hull, cv.Scalar.White, task.lineWidth)
        reduction.dst0.SetTo(0, dst1)
        dst1 += reduction.dst0

        labels(2) = CStr(kHist.kCells.Count) + " objects found"
    End Sub
End Class






Public Class GuidedBP_History : Inherits VB_Algorithm
    Public kWare As New GuidedBP_Hulls
    Dim kCellList As New List(Of List(Of kwData))
    Public kCells As New List(Of kwData)
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

        kWare.Run(src)

        kCellList.Add(New List(Of kwData)(kWare.kCells))

        Dim sortCells As New SortedList(Of Integer, kwData)(New compareAllowIdenticalIntegerInverted)
        For Each cellList In kCellList
            For i = 1 To cellList.Count - 1
                sortCells.Add(cellList(i).size, cellList(i))
            Next
        Next

        kMap.SetTo(0)
        kCells.Clear()
        kCells.Add(New kwData)
        Dim lastDst2 = dst2.Clone
        Dim usedColors As New List(Of cv.Vec3b)({black})
        For Each entry In sortCells
            Dim kw = entry.Value
            Dim index = kMap.Get(Of Byte)(kw.maxDist.Y, kw.maxDist.X)
            Dim lkw = kCells(index)
            If Math.Abs((kw.mmZ.minVal + kw.mmZ.maxVal) / 2 - (lkw.mmZ.minVal + lkw.mmZ.maxVal) / 2) > distanceMin Then
                If kw.size >= minSize Then
                    kw.index = kCells.Count
                    Dim color = lastDst2.Get(Of cv.Vec3b)(kw.maxDist.Y, kw.maxDist.X)
                    If usedColors.Contains(color) = False Then kw.color = color
                    vbDrawContour(kMap, kw.contour, kw.index, -1)
                    kCells.Add(kw)
                    usedColors.Add(kw.color)
                End If
            End If
        Next

        dst2.SetTo(0)
        For Each kw In kCells
            vbDrawContour(dst2, kw.contour, kw.color, -1)
            setTrueText(CStr(kw.index), kw.maxDist, 2)
        Next

        setTrueText(kWare.strOut, 3)

        Dim kwS = kWare.kwShowSelected(kCells, kMap, dst2)
        vbDrawContour(task.color, kwS.contour, cv.Scalar.Yellow)

        If kCellList.Count >= task.historyCount Then kCellList.RemoveAt(0)
        If heartBeat() Then labels(2) = CStr(kCells.Count) + " objects were consistently present"
    End Sub
End Class










Public Class GuidedBP_RedColorCloud : Inherits VB_Algorithm
    Dim bpDoctor As New GuidedBP_Basics
    Dim stats As New RedBP_CellStats
    Public Sub New()
        stats.redC = New RedBP_Basics
        labels = {"", "", "GuidedBP_Basics output", ""}
        desc = "Run RedBP_CellStats on the output of GuidedBP_Points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bpDoctor.Run(src)
        stats.Run(bpDoctor.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = stats.dst2

        setTrueText(stats.strOut, 3)
    End Sub
End Class









Public Class GuidedBP_RedColor : Inherits VB_Algorithm
    Dim bpDoctor As New GuidedBP_Cells
    Dim stats As New RedBP_CellStats
    Dim colorClass As New Color_Basics
    Public Sub New()
        stats.redC = New RedBP_Basics
        labels = {"", "", "RedBP_CellStats output", ""}
        desc = "Run RedBP_CellStats on the output of GuidedBP_Basics after merging with task.color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bpDoctor.Run(src)
        dst2 = bpDoctor.dst2

        colorClass.Run(src)
        dst1 = bpDoctor.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        colorClass.dst2.CopyTo(dst1, task.noDepthMask)

        stats.Run(dst1)

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






Public Class GuidedBP_HotPointsMap : Inherits VB_Algorithm
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








Public Class GuidedBP_Duplicates : Inherits VB_Algorithm
    Public hotPoints As New GuidedBP_HotPoints
    Dim topRects As New List(Of cv.Rect)
    Dim sideRects As New List(Of cv.Rect)
    Public Sub New()
        desc = "Find those objects that are in both the top view and the side view."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hotPoints.Run(src)
        topRects = New List(Of cv.Rect)(hotPoints.topRects)
        sideRects = New List(Of cv.Rect)(hotPoints.sideRects)

        dst2 = hotPoints.dst2
        dst3 = hotPoints.dst3
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
        For i = 0 To points.Rows - 1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            Dim count = input.FloodFill(mask, pt, 255, rect, 0, 0, 4 Or cv.FloodFillFlags.MaskOnly Or (255 << 8))
            If count >= task.minPixels Then
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







Public Class GuidedBP_Points : Inherits VB_Algorithm
    Public hotPoints As New GuidedBP_HotPointsMap
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





Public Class GuidedBP_Hulls : Inherits VB_Algorithm
    Dim bpDoctor As New GuidedBP_Points
    Public kCells As New List(Of kwData)
    Public kCellsLast As New List(Of kwData)
    Public kMap As New cv.Mat
    Dim contours As New Contour_Largest
    Dim plot As New Histogram_Depth
    Public Sub New()
        kMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Top X identified objects."
        desc = "Find and display k objects using doctored back projection."
    End Sub
    Public Function kwShowSelected(cells As List(Of kwData), mapMat As cv.Mat, dst As cv.Mat) As kwData
        Dim index = mapMat.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
        If index = 0 Then Return New kwData
        Dim kw = cells(index)
        If index > 0 Then vbDrawContour(dst, kw.contour, cv.Scalar.White, -1)
        Return kw
    End Function
    Public Sub RunVB(src As cv.Mat)
        Static saveTtext As List(Of trueText)
        If task.paused Then Exit Sub

        ' Use the DebugCheckBox box and it will rerun the exact same input.
        If gOptions.DebugCheckBox.Checked = False Then bpDoctor.Run(src)

        Dim newCells As New SortedList(Of Integer, kwData)(New compareAllowIdenticalIntegerInverted)
        Dim kMapLast = kMap.Clone
        If heartBeat() Then
            kMap.SetTo(0)
            dst2.SetTo(0)
            For i = 1 To bpDoctor.classCount
                Dim kw As New kwData
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
                newCells.Add(kw.size, kw)

                kw.color = New cv.Vec3b(msRNG.Next(30, 240), msRNG.Next(30, 240), msRNG.Next(30, 240))
                vbDrawContour(dst2, kw.contour, kw.color, -1)
            Next
            dst0 = task.color.Clone
            kCellsLast = New List(Of kwData)(kCells)
            kCells.Clear()
            kCells.Add(New kwData)

            Dim cellUpdates As New List(Of kwData)
            Dim usedColors As New List(Of cv.Vec3b)({black})
            For Each entry In newCells
                Dim kw As New kwData
                kw = entry.Value

                kw.index = kCells.Count
                Dim index = kMapLast.Get(Of Byte)(kw.maxDist.Y, kw.maxDist.X)
                Dim lkw As kwData
                If index <> 0 Then
                    lkw = kCellsLast(index)
                    kw.indexLast = lkw.index
                    lkw.index = kCells.Count
                    kw.color = lkw.color
                End If

                If usedColors.Contains(kw.color) Then
                    kw.color = New cv.Vec3b(msRNG.Next(30, 240), msRNG.Next(30, 240), msRNG.Next(30, 240))
                End If

                vbDrawContour(kMap, kw.hull, kw.index, -1)
                vbDrawContour(dst2, kw.contour, kw.color, -1)

                kCells.Add(kw)
                usedColors.Add(kw.color)
                If kCells.Count > 20 Then Exit For ' more than 20 objects?  Likely small artifacts...
            Next
            strOut = "Index" + vbTab + "Size" + vbTab + "Depth Range" + vbTab
            strOut += "Hull" + vbTab + vbTab + vbTab + vbCrLf
            For Each kw In kCells
                If kw.index = 0 Then Continue For
                strOut += CStr(kw.index) + vbTab + Format(kw.size, fmt0) + vbTab
                strOut += Format(kw.mmZ.minVal, fmt2) + "/" + Format(kw.mmZ.maxVal, fmt2) + vbTab
                strOut += hullStr(kw.hull) + vbCrLf
            Next

            For Each kw In kCells
                If kw.index = 0 Then Continue For
                setTrueText(CStr(kw.index), kw.maxDist, 2)
                dst2.Circle(kw.maxDist, task.dotSize, cv.Scalar.White, -1, task.lineType)
            Next
            setTrueText(strOut, 3)

            saveTtext = New List(Of trueText)(trueData)
        End If

        plot.kw = kwShowSelected(kCells, kMap, dst0)
        plot.Run(task.pcSplit(2))
        dst1 = plot.dst2

        trueData = New List(Of trueText)(saveTtext)
        labels(2) = "There were " + CStr(kCells.Count) + " objects identified in the image."
    End Sub
End Class





Public Class GuidedBP_Map : Inherits VB_Algorithm
    Dim bpDoctor As New GuidedBP_Points
    Public kCells As New List(Of kwData)
    Public kCellsLast As New List(Of kwData)
    Public kMap As New cv.Mat
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

        Dim newCells As New SortedList(Of Integer, kwData)(New compareAllowIdenticalIntegerInverted)
        Dim kMapLast = kMap.Clone
        If heartBeat() Then
            kMap.SetTo(0)
            For i = 0 To bpDoctor.topRects.Count - 1
                Dim kw As New kwData
                Dim index = i + 1
                kw.mask = bpDoctor.histogramTop.InRange(index, index).Threshold(0, 255, cv.ThresholdTypes.Binary)
                kw.rect = bpDoctor.topRects(i)
                kw.maxDist = vbGetMaxDist(kw.mask)
                kMap(kw.rect).SetTo(index)
                kw.size = bpDoctor.histogramTop(kw.rect).Sum()
                newCells.Add(kw.size, kw)
            Next

            Dim usedColors As New List(Of cv.Vec3b)({black})
            kCellsLast = New List(Of kwData)(kCells)
            kCells.Clear()
            kCells.Add(New kwData)
            For Each kw In newCells.Values
                kw.index = kCells.Count
                Dim index = kMap.Get(Of Byte)(kw.maxDist.Y, kw.maxDist.X)
                Dim lkw As kwData
                If index <> 0 And index < kCellsLast.Count Then
                    lkw = kCellsLast(index)
                    kw.indexLast = lkw.index
                    lkw.index = kCells.Count
                    kw.color = dst2.Get(Of cv.Vec3b)(kw.maxDist.Y, kw.maxDist.X)
                End If

                If usedColors.Contains(kw.color) Then
                    kw.color = New cv.Vec3b(msRNG.Next(30, 240), msRNG.Next(30, 240), msRNG.Next(30, 240))
                End If

                kCells.Add(kw)
                usedColors.Add(kw.color)
                If kCells.Count > 20 Then Exit For ' more than 20 objects?  Likely small artifacts...
            Next

            dst2.SetTo(0)
            For Each kw In kCells
                If kw.index > 0 Then dst2(kw.rect).SetTo(kw.color)
            Next

            strOut = "Index" + vbTab + "Size" + vbTab + "Depth Range" + vbTab
            strOut += "Hull" + vbTab + vbTab + vbTab + vbCrLf
            For Each kw In kCells
                If kw.index = 0 Then Continue For
                strOut += CStr(kw.index) + vbTab + Format(kw.size, fmt0) + vbTab
                strOut += Format(kw.mmZ.minVal, fmt2) + "/" + Format(kw.mmZ.maxVal, fmt2) + vbTab
                strOut += hullStr(kw.hull) + vbCrLf
            Next

            For Each kw In kCells
                If kw.index = 0 Then Continue For
                setTrueText(CStr(kw.index), kw.maxDist, 2)
                dst2.Circle(kw.maxDist, task.dotSize, cv.Scalar.White, -1, task.lineType)
            Next
            setTrueText(strOut, 3)

            saveTtext = New List(Of trueText)(trueData)
        End If

        trueData = New List(Of trueText)(saveTtext)
        labels(2) = "There were " + CStr(kCells.Count) + " objects identified in the image."
    End Sub
End Class








Public Class GuidedBP_Depth : Inherits VB_Algorithm
    Public hist2d As New Histogram2D_PointCloud
    Dim opAuto As New OpAuto_GuidedBP
    Public Sub New()
        redOptions.HistBinSlider.Value = 15
        redOptions.RedBPonly.Enabled = True
        desc = "Backproject the 2D histogram of depth for selected channels to discretize the depth data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        hist2d.Run(src)

        Dim samples(hist2d.histogram.Total - 1) As Single
        Marshal.Copy(hist2d.histogram.Data, samples, 0, samples.Length)

        Dim histList = samples.ToList
        samples(histList.IndexOf(histList.Max)) = 0

        opAuto.nonzeroSamples = 0
        For i = 0 To samples.Count - 1
            If samples(i) > 0 Then
                opAuto.nonZeroSamples += 1
                ' this is where the histogram is doctored to create the different regions
                samples(i) = If(opAuto.nonzeroSamples <= 255, 255 - opAuto.nonzeroSamples, 0)
            End If
        Next

        opAuto.runvb(Nothing)

        Marshal.Copy(samples, 0, hist2d.histogram.Data, samples.Length)

        cv.Cv2.CalcBackProject({src}, hist2d.channels, hist2d.histogram, dst2, hist2d.ranges)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)

        dst3 = vbPalette(dst2)
    End Sub
End Class








Public Class GuidedBP_DepthOriginal : Inherits VB_Algorithm
    Public hist2d As New Histogram2D_PointCloud
    Public backProject As New cv.Mat
    Public Sub New()
        desc = "Backproject the 2D histogram of depth for selected channels to discretize the depth data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(task.pointCloud)
        dst3 = hist2d.dst2

        Dim samples(hist2d.histogram.Total - 1) As Single
        Marshal.Copy(hist2d.histogram.Data, samples, 0, samples.Length)

        Dim index As Integer
        For i = 0 To samples.Count - 1
            If samples(i) > dst2.Total * 0.0001 Then
                samples(i) = index
                index += 1
            Else
                samples(i) = 0
            End If
        Next

        ' A practical use of optionAutomation.  Any image with more regions is quite complex.
        Dim saveit = task.optionsChanged
        If index > 150 Then gOptions.HistBinSlider.Value -= 1
        If index < 100 Then gOptions.HistBinSlider.Value += 1
        task.optionsChanged = saveit

        Marshal.Copy(samples, 0, hist2d.histogram.Data, samples.Length)

        cv.Cv2.CalcBackProject({task.pointCloud}, hist2d.channels, hist2d.histogram, backProject, hist2d.ranges)
        backProject.SetTo(0, task.noDepthMask)

        dst2 = vbPalette(backProject.ConvertScaleAbs)
    End Sub
End Class
