Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class RedCloud_Basics : Inherits TaskParent
    Public redCore As New RedCloud_Core
    Public pcList As New List(Of cloudData)
    Public pcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public percentImage As Single
    Public Sub New()
        task.redCloud = Me
        desc = "Build contours for each cell"
    End Sub
    Public Shared Function selectCell() As String
        task.pcD = RedCell_Basics.displayCell()
        If task.pcD IsNot Nothing Then
            If task.pcD.rect.Contains(task.ClickPoint) Then
                task.color(task.pcD.rect).SetTo(white, task.pcD.mask)
                Return task.pcD.displayCell
            End If
        End If
        Return Nothing
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        redCore.Run(src)
        dst3 = redCore.dst3
        labels(3) = redCore.labels(3)
        labels(2) = redCore.labels(2) + If(standalone, "  Age of each cell is displayed as well.", "")

        Static pcListLast = New List(Of cloudData)(pcList)
        Static pcMapLast As cv.Mat = pcMap.clone

        pcList.Clear()
        Dim r2 As cv.Rect
        pcMap.setto(0)
        dst2.SetTo(0)
        For Each pc In redCore.pcList
            Dim r1 = pc.rect
            r2 = New cv.Rect(0, 0, 1, 1) ' fake rect for conditional below...
            Dim indexLast = pcMapLast.Get(Of Byte)(pc.maxDist.Y, pc.maxDist.X) - 1
            If indexLast > 0 Then r2 = pcListLast(indexLast).rect
            If indexLast >= 0 And r1.IntersectsWith(r2) And task.optionsChanged = False Then
                pc.age = pcListLast(indexLast).age + 1
                If pc.age > 1000 Then pc.age = 2
                If task.heartBeat = False And pc.rect.Contains(pcListLast(indexLast).maxdist) Then
                    pc.maxDist = pcListLast(indexLast).maxdist
                End If
                pc.color = pcListLast(indexLast).color
            End If
            pc.index = pcList.Count + 1
            pcMap(pc.rect).setto(pc.index, pc.hullMask)
            dst2(pc.rect).SetTo(pc.color, pc.hullMask)
            dst2.Circle(pc.maxDist, task.DotSize, task.highlight, -1)
            pcList.Add(pc)
            SetTrueText(CStr(pc.age), pc.maxDist)
        Next

        Dim cellsOnly = dst3.Threshold(1, 255, cv.ThresholdTypes.Binary).CountNonZero
        percentImage = cellsOnly / task.depthMask.CountNonZero
        Static targetSlider = OptionParent.FindSlider("Reduction Target")
        If percentImage < 0.8 Then
            If targetSlider.value + 1 < targetSlider.maximum Then targetSlider.value += 1
        End If

        SetTrueText(selectCell() + vbCrLf + vbCrLf + Format(percentImage, "0.0%") + " of image" + vbCrLf +
                    CStr(pcList.Count) + " cells present", 3)

        pcListLast = New List(Of cloudData)(task.redCloud.pcList)
        pcMapLast = pcMap.clone
    End Sub
End Class





Public Class RedCloud_Core : Inherits TaskParent
    Public prepEdges As New RedPrep_Basics
    Public pcList As New List(Of cloudData)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Reduced point cloud - use 'Reduction Target' option to increase/decrease cell sizes."
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
        pcList.Clear()
        dst1.SetTo(0)
        Dim pc As cloudData = Nothing
        For y = 0 To dst3.Height - 1
            For x = 0 To dst3.Width - 1
                Dim pt = New cv.Point(x, y)
                ' skip the regions with no depth or those that were already floodfilled.
                If dst3.Get(Of Byte)(pt.Y, pt.X) > index Then
                    Dim count = cv.Cv2.FloodFill(dst3, mask, pt, index, rect, 0, 0, flags)
                    If rect.Width > 0 And rect.Height > 0 Then
                        If count >= minCount Then
                            ' pc = New cloudData(dst3(rect).InRange(index, index), rect)
                            pc = MaxDist_Basics.setCloudData(dst3(rect).InRange(index, index), rect)
                            pc.index = index
                            pc.color = task.vecColors(pc.index)
                            pcList.Add(pc)
                            dst1(pc.rect).SetTo(pc.index Mod 255, pc.mask)
                            SetTrueText(CStr(pc.index), pc.rect.TopLeft)
                            index += 1
                            'pc = MaxDist_Basics.setCloudData(mask(rect), rect)
                            'If pc Is Nothing Then
                            '    'dst3(rect).SetTo(255, mask(rect))
                            'Else
                            '    pc.index = index
                            '    pcList.Add(pc)
                            '    dst1(pc.rect).SetTo(pc.index Mod 255, pc.mask)
                            '    SetTrueText(CStr(pc.index), pc.rect.TopLeft)
                            '    index += 1
                            'End If
                        Else
                            dst3(rect).SetTo(255, mask(rect))
                        End If
                    End If
                End If
            Next
        Next

        dst2 = ShowPalette254(dst1)

        For Each pc In pcList
            dst2.Circle(pc.maxDist, task.DotSize, task.highlight, -1)
        Next
        labels(2) = CStr(pcList.Count) + " regions were identified.  Bright areas are < " + CStr(CInt(minCount)) + " pixels (too small.)"
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
        For Each pc In task.redCloud.pcList
            Dim hullIndices = cv.Cv2.ConvexHullIndices(pc.contour, False)
            For i = 0 To pc.contour.Count - 1
                Dim p1 = pc.contour(i)
                For j = i + 1 To pc.contour.Count - 1
                    Dim p2 = pc.contour(j)
                    If p1 = p2 Then Continue For
                Next
            Next

            Try
                Dim defects = cv.Cv2.ConvexityDefects(pc.contour, hullIndices.ToList)
                Dim lastV As Integer = -1
                Dim newC As New List(Of cv.Point)
                For Each v In defects
                    If v(0) <> lastV And lastV >= 0 Then
                        For i = lastV To v(0) - 1
                            newC.Add(pc.contour(i))
                        Next
                    End If
                    newC.Add(pc.contour(v(0)))
                    newC.Add(pc.contour(v(2)))
                    newC.Add(pc.contour(v(1)))
                    lastV = v(1)
                Next
                DrawTour(dst3(pc.rect), newC, pc.color)
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
        RedCloud_Basics.selectCell()
        If task.pcD Is Nothing Then
            labels(3) = "Select a RedCloud cell to see the histogram"
            Exit Sub
        End If

        Dim depth As cv.Mat = task.pcSplit(2)(task.pcD.rect)
        depth.SetTo(0, task.noDepthMask(task.pcD.rect))
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
        If task.heartBeatLT Then dst3 = dst2.Clone
        For i = 1 To histArray.Count - 1
            If histArray(i) > 0 And pcUsed.Contains(i) = False Then
                Dim pc = redContours.pcList(i)
                dst3(pc.rect).SetTo(task.scalarColors(pc.index), pc.mask)
                pcUsed.Add(i)
            End If
        Next
    End Sub
End Class






Public Class RedCloud_Motion : Inherits TaskParent
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

        dst3.SetTo(0)
        For Each pc In task.redCloud.pcList
            If pc.age > 10 Then DrawTour(dst3(pc.rect), pc.contour, pc.color)
        Next
    End Sub
End Class