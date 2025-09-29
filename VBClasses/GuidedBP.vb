Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class GuidedBP_Basics : Inherits TaskParent
    Public ptHot As New GuidedBP_HotPoints
    Dim topMap As New cv.Mat
    Dim sideMap As New cv.Mat
    Public Sub New()
        topMap = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        sideMap = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Correlate the hot points with the previous generation using a Map"
    End Sub
    Public Sub runMap(rectList As List(Of cv.Rect), dstindex As Integer, map As cv.Mat)
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
            SetTrueText(CStr(index), pt, dstindex)
        Next
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        ptHot.Run(src)
        dst2 = ptHot.dst2
        dst3 = ptHot.dst3

        runMap(ptHot.topRects, 2, topMap)
        runMap(ptHot.sideRects, 3, sideMap)

        labels(2) = CStr(ptHot.topRects.Count) + " objects found in the top view"
        labels(3) = CStr(ptHot.sideRects.Count) + " objects found in the Side view"
    End Sub
End Class






Public Class GuidedBP_HotPointsKNN : Inherits TaskParent
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

        If task.firstPass Then knn.trainInput = New List(Of cv.Point2f)(knn.queries)

        knn.Run(emptyMat)

        For i = 0 To knn.queries.Count - 1
            Dim p1 = knn.queries(i)
            Dim index = knn.result(i, 0)
            Dim p2 = knn.trainInput(index)
            Dim dist = p1.DistanceTo(p2)
            Dim r = rectList(i)
            If dist < r.Width / 2 And dist < r.Height / 2 Then
                dst.Rectangle(r, white, task.lineWidth)
                Dim pt = New cv.Point(r.X + r.Width, r.Y + r.Height)
                SetTrueText(CStr(index), pt, dstindex)
            End If
        Next

        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        ptHot.Run(src)
        dst2 = ptHot.dst2
        dst3 = ptHot.dst3

        runKNN(knnTop, ptHot.topRects, dst2, 2)
        runKNN(knnSide, ptHot.sideRects, dst3, 3)

        labels(2) = CStr(ptHot.topRects.Count) + " objects found in the top view"
        labels(3) = CStr(ptHot.sideRects.Count) + " objects found in the Side view"
    End Sub
End Class









Public Class GuidedBP_PlanesPlot : Inherits TaskParent
    Dim histSide As New Projection_HistSide
    Public Sub New()
        labels = {"", "", "Side view", "Plot of nonzero rows in the side view"}
        desc = "Plot the likely floor or ceiling areas."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        histSide.Run(src)
        dst2 = histSide.dst3

        Dim sumList As New List(Of Integer)
        dst3.SetTo(0)
        For i = 0 To dst2.Rows - 1
            Dim x = dst2.Row(i).CountNonZero
            sumList.Add(x)
            DrawLine(dst3, New cv.Point(0, i), New cv.Point(x, i), white)
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







Public Class GuidedBP_Lookup : Inherits TaskParent
    Dim guided As New GuidedBP_Basics
    Public Sub New()
        task.ClickPoint = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        desc = "Given a point cloud pixel, look up which object it is in.  Click in the Depth RGB image to test."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        guided.Run(src)
        dst2 = guided.dst2
        labels(2) = guided.labels(2)


    End Sub
End Class







Public Class GuidedBP_Depth : Inherits TaskParent
    Public hist As New Hist_PointCloud
    Public classCount As Integer
    Public Sub New()
        task.gOptions.setHistogramBins(16)
        desc = "Backproject the 2D histogram of depth for selected channels to categorize the depth data."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
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

        classCount = sortedHist.Values.Max
        Dim newSamples(histArray.Count - 1) As Single
        For i = 0 To sortedHist.Count - 1
            Dim index = sortedHist.ElementAt(i).Value
            newSamples(index) = i + 1
            If index >= 255 Then Exit For
        Next

        Marshal.Copy(newSamples, 0, hist.histogram.Data, newSamples.Length)

        cv.Cv2.CalcBackProject({src}, task.channels, hist.histogram, dst2, task.ranges)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)

        labels(3) = "Use task.gOptions.PointCloudReduction to select different cloud combinations."
        If standaloneTest() Then dst3 = ShowPalette(dst2 + 1)

        Dim depthCount = task.depthMask.CountNonZero
        dst3.SetTo(0, task.noDepthMask)
        Dim count = dst2.CountNonZero
        labels(2) = CStr(classCount) + " regions detected in the backprojection - " + Format(count / depthCount, "0%") + " of depth data"
    End Sub
End Class







Public Class GuidedBP_HotPoints : Inherits TaskParent
    Public histTop As New Projection_HistTop
    Public histSide As New Projection_HistSide
    Public topRects As New List(Of cv.Rect)
    Public sideRects As New List(Of cv.Rect)
    Dim floodRect As New cv.Rect(1, 1, dst2.Width - 2, dst2.Height - 2)
    Dim mask As New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U)
    Public Sub New()
        task.useXYRange = False
        desc = "Use floodfill to identify all the objects in both the top and side views."
    End Sub
    Private Function hotPoints(ByRef view As cv.Mat) As List(Of cv.Rect)
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
    Public Overrides sub RunAlg(src As cv.Mat)
        histTop.Run(src.Clone)
        topRects = hotPoints(histTop.dst3)
        dst2 = ShowPalette(histTop.dst3)

        histSide.Run(src)
        sideRects = hotPoints(histSide.dst3)
        dst3 = ShowPalette(histSide.dst3)

        If task.heartBeat Then labels(2) = "Top " + CStr(topRects.Count) + " objects identified in the top view."
        If task.heartBeat Then labels(3) = "Top " + CStr(sideRects.Count) + " objects identified in the side view."
    End Sub
End Class






Public Class GuidedBP_MultiSlice : Inherits TaskParent
    Dim histTop As New Projection_HistTop
    Dim histSide As New Projection_HistSide
    Public sliceMask As cv.Mat
    Public split() As cv.Mat
    Public options As New Options_Structured
    Public classCount As Integer
    Public Sub New()
        desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()
        Dim stepSize = options.stepSize

        histTop.Run(src.Clone)

        classCount = 1
        For x = 0 To histTop.dst3.Height - stepSize Step stepSize
            Dim r = New cv.Rect(x, 0, stepSize, dst2.Height)
            Dim slice = histTop.dst3(r)
            If slice.CountNonZero Then
                histTop.histogram(r).SetTo(classCount, slice)
                classCount += 1
            End If
        Next
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histTop.histogram, dst0, task.rangesTop)
        Dim mm = GetMinMax(dst0)
        dst2 = ShowPalette(dst0)
        labels(2) = "The nonzero horizontal slices produced " + CStr(classCount) + " classes"

        histSide.Run(src.Clone)

        classCount = 1
        For y = 0 To histSide.dst3.Height - stepSize Step stepSize
            Dim r = New cv.Rect(0, y, dst2.Width, stepSize)
            Dim slice = histSide.dst3(r)
            If slice.CountNonZero Then
                histSide.histogram(r).SetTo(classCount, slice)
                classCount += 1
            End If
        Next
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histSide.histogram, dst1, task.rangesSide)
        dst3 = ShowPalette(dst1)
        labels(3) = "The nonzero vertical slices produced " + CStr(classCount) + " classes"
    End Sub
End Class







Public Class GuidedBP_RedCloud : Inherits TaskParent
    Dim guide As New GuidedBP_MultiSlice
    Public redCx As New RedColor_Basics
    Public redCy As New RedColor_Basics
    Public rcListX As New List(Of rcData)
    Public rcListY As New List(Of rcData)
    Public rcMapX As New cv.Mat
    Public rcMapY As New cv.Mat
    Public Sub New()
        desc = "Identify each segment in the X and Y point cloud data"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        guide.Run(src)

        redCx.Run(guide.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        rcMapX = task.redCold.rcMap.Clone
        dst2 = redCx.dst2
        rcListX = New List(Of rcData)(task.redCold.rcList)
        labels(2) = CStr(task.redCold.rcList.Count) + " cells were found in vertical segments"

        redCx.Run(guide.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        rcMapY = task.redCold.rcMap.Clone
        dst3 = redCx.dst2
        rcListY = New List(Of rcData)(task.redCold.rcList)
        labels(3) = CStr(task.redCold.rcList.Count) + " cells were found in horizontal segments"
    End Sub
End Class








Public Class GuidedBP_Regions : Inherits TaskParent
    Public redCold As New GuidedBP_RedCloud
    Public mats As New Mat_4Click
    Dim options As New Options_BP_Regions
    Public rcListX As New List(Of rcData)
    Public rcListY As New List(Of rcData)
    Public rcMapX As New cv.Mat
    Public rcMapY As New cv.Mat
    Public Sub New()
        If standalone Then task.gOptions.displayDst0.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "Click a quadrant in the left image and see it below."
        desc = "Identify the top X regions in the GuidedBP_RedCloud output"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        redCold.Run(src)

        rcMapX = redCold.rcMapX.Threshold(options.cellCount - 1, 255, cv.ThresholdTypes.TozeroInv)
        rcMapY = redCold.rcMapY.Threshold(options.cellCount - 1, 255, cv.ThresholdTypes.TozeroInv)
        If standaloneTest() Then
            dst0 = ShowPalette(rcMapX)
            dst1 = ShowPalette(rcMapY)
        End If

        mats.mat(0) = redCold.dst2
        mats.mat(1) = redCold.dst3

        mats.mat(2).SetTo(0)
        mats.mat(3).SetTo(0)

        rcListX.Clear()
        rcListY.Clear()

        For i = 1 To Math.Min(options.cellCount, redCold.rcListX.Count) - 1
            Dim rc = redCold.rcListX(i)
            mats.mat(2)(rc.rect).SetTo(rc.color, rc.mask)
            rcListX.Add(rc)
        Next
        For i = 1 To Math.Min(options.cellCount, redCold.rcListY.Count) - 1
            Dim rc = redCold.rcListY(i)
            mats.mat(3)(rc.rect).SetTo(rc.color, rc.mask)
            rcListY.Add(rc)
        Next

        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3

        labels(2) = "(left to right) Regions from cloud X, Regions from Cloud Y, Top " + CStr(options.cellCount) +
                    " X regions, Top " + CStr(options.cellCount) + " Y regions"
    End Sub
End Class







Public Class GuidedBP_Points : Inherits TaskParent
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
    Public Overrides sub RunAlg(src As cv.Mat)
        hotPoints.Run(src)

        hotPoints.ptHot.histTop.dst3.ConvertTo(histogramTop, cv.MatType.CV_32F)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histogramTop, backP,
                                task.rangesTop)

        topRects = New List(Of cv.Rect)(hotPoints.ptHot.topRects)
        sideRects = New List(Of cv.Rect)(hotPoints.ptHot.sideRects)

        dst2 = ShowPalette(backP)

        hotPoints.ptHot.histSide.dst3.ConvertTo(histogramSide, cv.MatType.CV_32F)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histogramSide, dst3, task.rangesSide)

        dst3 = ShowPalette(dst3)

        classCount = topRects.Count + sideRects.Count

        If task.mouseClickFlag Then selectedPoint = task.ClickPoint
        If task.heartBeat Then labels(2) = CStr(topRects.Count) + " objects were identified in the top view."
        If task.heartBeat Then labels(3) = CStr(sideRects.Count) + " objects were identified in the side view."
    End Sub
End Class





Public Class GuidedBP_Top : Inherits TaskParent
    Public ptHot As New GuidedBP_HotPoints
    Dim hotPoints As New GuidedBP_Basics
    Dim topMap As New cv.Mat
    Public Sub New()
        topMap = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Correlate the hot points with the previous generation using a Map"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        ptHot.Run(src)
        dst2 = ptHot.dst2

        hotPoints.trueData.Clear()
        hotPoints.runMap(ptHot.topRects, 2, topMap)
        trueData = hotPoints.trueData

        labels(2) = CStr(ptHot.topRects.Count) + " objects found in the top view"
    End Sub
End Class






Public Class GuidedBP_TopView : Inherits TaskParent
    Public hotPoints As New GuidedBP_Top
    Public classCount As Integer
    Public topRects As New List(Of cv.Rect)
    Public histogramTop As New cv.Mat
    Public backP As New cv.Mat
    Public Sub New()
        desc = "Use floodfill to identify all the objects in the selected view then build a backprojection that identifies k objects in the image view."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        hotPoints.Run(src)

        hotPoints.ptHot.histTop.dst3.ConvertTo(histogramTop, cv.MatType.CV_32F)
        cv.Cv2.CalcBackProject({src}, task.channelsTop, histogramTop, backP,
                                task.rangesTop)

        topRects = New List(Of cv.Rect)(hotPoints.ptHot.topRects)

        dst2 = ShowPalette(backP)
        classCount = topRects.Count

        If task.heartBeat Then labels(2) = CStr(topRects.Count) + " objects were identified in the top view."
    End Sub
End Class
