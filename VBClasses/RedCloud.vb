Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class RedCloud_Basics : Inherits TaskParent
    Public redSweep As New RedCloud_Sweep
    Public rcList As New List(Of rcData)
    Public rcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public percentImage As Single
    Public Sub New()
        task.redCloud = Me
        desc = "Build contours for each cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redSweep.Run(src)
        dst3 = redSweep.dst3
        labels(3) = redSweep.labels(3)
        labels(2) = redSweep.labels(2) + If(standalone, "  Number is cell age", "")

        Static rcListLast = New List(Of rcData)(rcList)
        Static pcMapLast As cv.Mat = rcMap.clone

        rcList.Clear()
        Dim r2 As cv.Rect
        rcMap.setto(0)
        dst2.SetTo(0)
        For Each rc In redSweep.rcList
            Dim r1 = rc.rect
            r2 = New cv.Rect(0, 0, 1, 1) ' fake rect for conditional below...
            Dim indexLast = pcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X) - 1
            If indexLast > 0 Then r2 = rcListLast(indexLast).rect
            If indexLast >= 0 And r1.IntersectsWith(r2) And task.optionsChanged = False Then
                rc.age = rcListLast(indexLast).age + 1
                If rc.age > 1000 Then rc.age = 2
                If task.heartBeat = False And rc.rect.Contains(rcListLast(indexLast).maxdist) Then
                    rc.maxDist = rcListLast(indexLast).maxdist
                End If
                rc.color = rcListLast(indexLast).color
            End If
            rc.index = rcList.Count + 1
            rcMap(rc.rect).setto(rc.index, rc.contourMask)
            dst2(rc.rect).SetTo(rc.color, rc.contourMask)
            rcList.Add(rc)
        Next

        For Each rc In rcList
            dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
            SetTrueText(CStr(rc.age), rc.maxDist)
        Next

        strOut = RedCell_Basics.selectCell(rcMap, rcList)
        If task.rcD IsNot Nothing Then task.color(task.rcD.rect).SetTo(white, task.rcD.contourMask)
        SetTrueText(strOut, 3)

        rcListLast = New List(Of rcData)(rcList)
        pcMapLast = rcMap.clone
    End Sub
End Class






Public Class RedCloud_HeartBeat : Inherits TaskParent
    Dim redCore As New RedCloud_Basics
    Public rcList As New List(Of rcData)
    Public percentImage As Single
    Public rcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public prepEdges As New RedPrep_Basics
    Public Sub New()
        redCore.redSweep.prepEdges = prepEdges
        desc = "Run RedCloud_Map on the heartbeat but just floodFill at maxDist otherwise."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Or task.optionsChanged Then
            redCore.Run(src)
            dst2 = redCore.dst2
            labels(2) = redCore.labels(2)
            rcList = New List(Of rcData)(redCore.rcList)
            dst3 = redCore.dst2
            dst1 = redCore.redSweep.prepEdges.dst2
        Else
            Dim rcListLast = New List(Of rcData)(redCore.rcList)

            prepEdges.Run(src)
            dst1 = prepEdges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

            Dim index As Integer = 1
            Dim rect As New cv.Rect
            Dim maskRect = New cv.Rect(1, 1, dst1.Width, dst1.Height)
            Dim mask = New cv.Mat(New cv.Size(dst1.Width + 2, dst1.Height + 2), cv.MatType.CV_8U, 0)
            Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
            Dim minCount = dst1.Total * 0.001
            rcList.Clear()
            rcMap.SetTo(0)
            For Each rc In rcListLast
                Dim pt = rc.maxDist
                If rcMap.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                    Dim count = cv.Cv2.FloodFill(dst1, mask, pt, index, rect, 0, 0, flags)
                    If rect.Width > 0 And rect.Height > 0 And rect.Width < dst2.Width And rect.Height < dst2.Height Then
                        Dim pcc = MaxDist_Basics.setCloudData(dst1(rect), rect, index)
                        If pcc IsNot Nothing Then
                            pcc.index = index
                            pcc.color = rc.color
                            pcc.age = rc.age + 1
                            rcList.Add(pcc)
                            rcMap(pcc.rect).SetTo(pcc.index Mod 255, pcc.contourMask)

                            index += 1
                        End If
                    End If
                End If
            Next

            dst2 = PaletteBlackZero(rcMap)
            labels(2) = CStr(rcList.Count) + " regions were identified "
        End If

        strOut = RedCell_Basics.selectCell(rcMap, rcList)
        If task.rcD IsNot Nothing Then task.color(task.rcD.rect).SetTo(white, task.rcD.contourMask)
        SetTrueText(strOut + vbCrLf + vbCrLf + Format(percentImage, "0.0%") + " of image" + vbCrLf + CStr(rcList.Count) + " cells present", 3)
    End Sub
End Class





Public Class RedCloud_Sweep : Inherits TaskParent
    Public prepEdges As New RedPrep_Basics
    Public rcList As New List(Of rcData)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Find the biggest chunks of consistent depth data "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prepEdges.Run(src)
        dst3 = Not prepEdges.dst2

        Dim index As Integer = 1
        Dim rect As New cv.Rect
        Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
        Dim minCount = dst3.Total * 0.001
        rcList.Clear()
        dst1.SetTo(0)
        Dim rc As rcData = Nothing
        For y = 0 To dst3.Height - 1
            For x = 0 To dst3.Width - 1
                Dim pt = New cv.Point(x, y)
                ' skip the regions with no depth or those that were already floodfilled.
                If dst3.Get(Of Byte)(pt.Y, pt.X) > index Then
                    Dim count = cv.Cv2.FloodFill(dst3, mask, pt, index, rect, 0, 0, flags)
                    If rect.Width > 0 And rect.Height > 0 Then
                        If count >= minCount Then
                            rc = MaxDist_Basics.setCloudData(dst3(rect), rect, index)
                            rc.color = task.vecColors(rc.index)
                            rcList.Add(rc)
                            dst1(rc.rect).SetTo(rc.index Mod 255, rc.mask)
                            SetTrueText(CStr(rc.index), rc.rect.TopLeft)
                            index += 1
                        Else
                            dst3(rect).SetTo(255, mask(rect))
                        End If
                    End If
                End If
            Next
        Next

        dst2 = PaletteBlackZero(dst1)

        For Each rc In rcList
            dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
        Next
        labels(2) = CStr(rcList.Count) + " regions were identified.  Bright areas are < " +
                    CStr(CInt(minCount)) + " pixels (too small.)"
        labels(3) = "Reduced point cloud - use 'Reduction Target' option to increase/decrease cell sizes.  White cells were to small (> " +
                    CStr(minCount) + " pixels)"
    End Sub
End Class







Public Class RedCloud_Defect : Inherits TaskParent
    Public hull As New List(Of cv.Point)
    Public Sub New()
        desc = "Find defects in the RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedCloud(src, labels(2))

        dst3.SetTo(0)
        For Each rc In task.redCloud.rcList
            Dim hullIndices = cv.Cv2.ConvexHullIndices(rc.contour, False)
            For i = 0 To rc.contour.Count - 1
                Dim p1 = rc.contour(i)
                For j = i + 1 To rc.contour.Count - 1
                    Dim p2 = rc.contour(j)
                    If p1 = p2 Then Continue For
                Next
            Next

            Try
                Dim defects = cv.Cv2.ConvexityDefects(rc.contour, hullIndices.ToList)
                Dim lastV As Integer = -1
                Dim newC As New List(Of cv.Point)
                For Each v In defects
                    If v(0) <> lastV And lastV >= 0 Then
                        For i = lastV To v(0) - 1
                            newC.Add(rc.contour(i))
                        Next
                    End If
                    newC.Add(rc.contour(v(0)))
                    newC.Add(rc.contour(v(2)))
                    newC.Add(rc.contour(v(1)))
                    lastV = v(1)
                Next
                DrawTour(dst3(rc.rect), newC, rc.color)
            Catch ex As Exception
                Continue For
            End Try
        Next
    End Sub
End Class






Public Class RedCloud_CellDepthHistogram : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        task.gOptions.setHistogramBins(100)
        plot.createHistogram = True
        desc = "Display the histogram of a selected RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedCloud(src, labels(2))

        strOut = RedCell_Basics.selectCell(task.redCloud.rcMap, task.redCloud.rcList)
        If task.rcD IsNot Nothing Then task.color(task.rcD.rect).SetTo(white, task.rcD.contourMask)
        SetTrueText(strOut, 3)

        If task.rcD Is Nothing Then
            labels(3) = "Select a RedCloud cell to see the histogram"
            Exit Sub
        End If

        Dim depth As cv.Mat = task.pcSplit(2)(task.rcD.rect)
        depth.SetTo(0, task.noDepthMask(task.rcD.rect))
        plot.minRange = 0
        plot.maxRange = task.MaxZmeters
        plot.Run(depth)
        labels(3) = "0 meters to " + Format(task.MaxZmeters, fmt0) + " meters - vertical lines every meter"

        Dim incr = dst2.Width / task.MaxZmeters
        For i = 1 To CInt(task.MaxZmeters - 1)
            Dim x = incr * i
            DrawLine(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White)
        Next
        dst3 = plot.dst2
    End Sub
End Class





Public Class RedCloud_MotionSimple : Inherits TaskParent
    Dim redContours As New RedCloud_Basics
    Public Sub New()
        task.gOptions.HistBinBar.Maximum = 255
        task.gOptions.HistBinBar.Value = 255
        desc = "Use motion to identify which cells changed."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redContours.Run(src)
        dst1 = redContours.dst1
        dst2 = redContours.dst2
        labels(2) = redContours.labels(2)

        dst1.SetTo(0, Not task.motionMask)

        Dim histogram As New cv.Mat
        Dim ranges = {New cv.Rangef(1, 256)}
        cv.Cv2.CalcHist({dst1}, {0}, New cv.Mat, histogram, 1, {task.histogramBins}, ranges)

        Dim histArray(histogram.Rows - 1) As Single
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

        Dim pcUsed As New List(Of Integer)
        If task.heartBeat Then dst3 = dst2.Clone
        For i = 1 To histArray.Count - 1
            If histArray(i) > 0 And pcUsed.Contains(i) = False Then
                Dim rc = redContours.rcList(i)
                dst3(rc.rect).SetTo(task.scalarColors(rc.index), rc.mask)
                pcUsed.Add(i)
            End If
        Next
    End Sub
End Class






Public Class RedCloud_Motion : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.gOptions.HistBinBar.Maximum = 255
        task.gOptions.HistBinBar.Value = 255
        desc = "Use motion to identify which cells changed."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst1 = redC.dst1
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        For Each rc In task.redCloud.rcList
            If rc.age > 10 Then DrawTour(dst3(rc.rect), rc.contour, rc.color)
        Next
    End Sub
End Class




Public Class RedCloud_MotionNew : Inherits TaskParent
    Public redCore As New RedCloud_Basics
    Public rcList As New List(Of rcData)
    Public rcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public percentImage As Single
    Public Sub New()
        desc = "Build contours for each cell"
    End Sub
    Public Function motionDisplayCell() As rcData
        Dim clickIndex = rcMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X) - 1
        If clickIndex >= 0 Then
            Return rcList(clickIndex)
        End If
        Return Nothing
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        redCore.Run(src)
        dst3 = redCore.dst3
        labels(3) = redCore.labels(3)
        labels(2) = redCore.labels(2) + If(standalone, "  Age of each cell is displayed as well.", "")

        Static rcListLast = New List(Of rcData)(rcList)
        Static pcMapLast As cv.Mat = rcMap.clone

        rcList.Clear()
        Dim r2 As cv.Rect
        rcMap.setto(0)
        dst2.SetTo(0)
        Dim unchangedCount As Integer
        For Each rc In redCore.rcList
            Dim r1 = rc.rect
            r2 = New cv.Rect(0, 0, 1, 1) ' fake rect for conditional below...
            Dim indexLast = pcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X) - 1
            If indexLast > 0 Then r2 = rcListLast(indexLast).rect
            If indexLast >= 0 And r1.IntersectsWith(r2) And task.optionsChanged = False Then
                Dim tmp = task.motionMask(rc.rect)
                tmp.SetTo(0, rc.mask)

                If task.heartBeat = False And rc.rect.Contains(rcListLast(indexLast).maxdist) And tmp.CountNonZero = 0 Then
                    ' rc.maxDist = rcListLast(indexLast).maxdist
                    rc = rcListLast(indexLast)
                    unchangedCount += 1
                End If

                rc.color = rcListLast(indexLast).color
                rc.age = rcListLast(indexLast).age + 1
                If rc.age > 1000 Then rc.age = 2
            End If
            rc.index = rcList.Count + 1
            rcMap(rc.rect).setto(rc.index, rc.contourMask)
            dst2(rc.rect).SetTo(rc.color, rc.contourMask)
            dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
            SetTrueText(CStr(rc.age), rc.maxDist)
            rcList.Add(rc)
        Next

        strOut = RedCell_Basics.selectCell(task.redCloud.rcMap, task.redCloud.rcList)
        If task.rcD IsNot Nothing Then task.color(task.rcD.rect).SetTo(white, task.rcD.contourMask)

        SetTrueText(strOut + vbCrLf + vbCrLf + Format(percentImage, "0.0%") + " of image" + vbCrLf +
                    CStr(rcList.Count) + " cells present", 3)

        rcListLast = New List(Of rcData)(rcList)
        pcMapLast = rcMap.clone
    End Sub
End Class