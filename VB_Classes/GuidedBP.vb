Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class GuidedBP_Basics : Inherits VB_Parent
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






Public Class GuidedBP_HotPointsKNN : Inherits VB_Parent
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







Public Class GuidedBP_HotPoints : Inherits VB_Parent
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
        dst2 = ShowPalette(histTop.dst3 * 255 / topRects.Count)

        histSide.Run(src)
        sideRects = hotPoints(histSide.dst3)
        dst3 = ShowPalette(histSide.dst3 * 255 / sideRects.Count)

        If task.heartBeat Then labels(2) = "Top " + CStr(topRects.Count) + " objects identified in the top view."
        If task.heartBeat Then labels(3) = "Top " + CStr(sideRects.Count) + " objects identified in the side view."
    End Sub
End Class









Public Class GuidedBP_PlanesPlot : Inherits VB_Parent
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
            drawLine(dst3, New cv.Point(0, i), New cv.Point(x, i), cv.Scalar.White)
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






Public Class GuidedBP_Points : Inherits VB_Parent
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

        dst2 = ShowPalette(backP * 255 / topRects.Count)

        hotPoints.ptHot.histSide.dst3.ConvertTo(histogramSide, cv.MatType.CV_32F)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histogramSide, dst3, task.rangesSide)

        dst3 = ShowPalette(dst3 * 255 / sideRects.Count)

        classCount = topRects.Count + sideRects.Count

        If task.mouseClickFlag Then selectedPoint = task.clickPoint
        If task.heartBeat Then labels(2) = CStr(topRects.Count) + " objects were identified in the top view."
        If task.heartBeat Then labels(3) = CStr(sideRects.Count) + " objects were identified in the side view."
    End Sub
End Class







Public Class GuidedBP_Lookup : Inherits VB_Parent
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







Public Class GuidedBP_Depth : Inherits VB_Parent
    Public hist As New PointCloud_Histograms
    Dim myPalette As New Palette_Random
    Public classCount As Integer
    Public Sub New()
        task.gOptions.HistBinSlider.Value = 16
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

        cv.Cv2.CalcBackProject({src}, task.redOptions.channels, hist.histogram, dst2, task.redOptions.ranges)
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