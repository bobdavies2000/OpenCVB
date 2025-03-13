Imports OpenCvSharp.Flann
Imports cv = OpenCvSharp
Public Class RedCell_Basics : Inherits TaskParent
    Dim plot As New Hist_Depth
    Public runRedCloud As Boolean
    Public Sub New()
        If standalone Then task.gOptions.setHistogramBins(20)
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Display the statistics for the selected cell."
    End Sub
    Public Sub statsString()
        Dim rc = task.rc

        Dim gridID = task.gridMap.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
        strOut = "rc.index = " + CStr(rc.index) + vbTab + " gridID = " + CStr(gridID) + vbTab
        strOut += "rc.age = " + CStr(rc.age) + vbCrLf
        strOut += "rc.rect: " + CStr(rc.rect.X) + ", " + CStr(rc.rect.Y) + ", "
        strOut += CStr(rc.rect.Width) + ", " + CStr(rc.rect.Height) + vbCrLf
        strOut += "rc.color = " + vbTab + CStr(CInt(rc.color(0))) + vbTab + CStr(CInt(rc.color(1)))
        strOut += vbTab + CStr(CInt(rc.color(2))) + vbCrLf
        strOut += "rc.maxDist = " + CStr(rc.maxDist.X) + "," + CStr(rc.maxDist.Y) + vbCrLf

        strOut += If(rc.depthPixels > 0, "Cell is marked as having depth" + vbCrLf, "")
        strOut += "Pixels " + Format(rc.pixels, "###,###") + vbCrLf + "depth pixels "
        If rc.depthPixels > 0 Then
            strOut += Format(rc.depthPixels, "###,###") + " or " +
                          Format(rc.depthPixels / rc.pixels, "0%") + " depth " + vbCrLf
        Else
            strOut += Format(rc.pixels, "###,###") + " - no depth data" + vbCrLf
        End If

        strOut += "Cloud Min/Max/Range: X = " + Format(rc.mmX.minVal, fmt1) + "/" + Format(rc.mmX.maxVal, fmt1)
        strOut += "/" + Format(rc.mmX.range, fmt1) + vbTab
        strOut += "Y = " + Format(rc.mmY.minVal, fmt1) + "/" + Format(rc.mmY.maxVal, fmt1)
        strOut += "/" + Format(rc.mmY.range, fmt1) + vbTab
        strOut += "Z = " + Format(rc.mmZ.minVal, fmt2) + "/" + Format(rc.mmZ.maxVal, fmt2)
        strOut += "/" + Format(rc.mmZ.range, fmt2) + vbCrLf + vbCrLf

        strOut += "Cell Depth in 3D: z = " + vbTab + Format(rc.depth, fmt2) + vbCrLf

        Dim tmp = New cv.Mat(task.rc.mask.Rows, task.rc.mask.Cols, cv.MatType.CV_32F, cv.Scalar.All(0))
        task.pcSplit(2)(task.rc.rect).CopyTo(tmp, task.rc.mask)
        plot.rc = task.rc
        plot.Run(tmp)
        dst3 = plot.dst2
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Or runRedCloud Then dst2 = runRedC(src, labels(2))
        statsString()
        SetTrueText(strOut, 1)
        labels(3) = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + CStr(CInt(task.MaxZmeters)) + " meters"
    End Sub
End Class








Public Class RedCell_ValidateColor : Inherits TaskParent
    Public Sub New()
        labels(3) = "Cells shown below have rc.depthPixels / rc.pixels < 50%"
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Validate that all the depthCells are correctly identified."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        dst1.SetTo(0)
        dst3.SetTo(0)
        Dim percentDepth As New List(Of Single)
        For Each rc In task.rcList
            If rc.depthPixels > 0 Then dst1(rc.rect).SetTo(255, rc.mask)
            If rc.depthPixels > 0 And rc.index > 0 Then
                Dim pc = rc.depthPixels / rc.pixels
                percentDepth.Add(pc)

                If pc < 0.5 Then dst3(rc.rect).SetTo(rc.color, rc.mask)
            End If
        Next

        Dim beforeCount = dst1.CountNonZero
        dst1.SetTo(0, task.depthMask)
        Dim aftercount = dst1.CountNonZero

        If beforeCount <> aftercount Then
            strOut = "There are color cells with limited depth in them" + vbCrLf
        Else
            strOut = "There are no color cells with depth in them." + vbCrLf
        End If
        If percentDepth.Count > 0 Then
            strOut += "grid cell percentage average " + Format(percentDepth.Average, "0%") + vbCrLf
            strOut += "grid cell percentage range " + Format(percentDepth.Min, "0%") + " to " + Format(percentDepth.Max, "0%")
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class RedCell_Distance : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "Depth distance to selected cell", "", "Color distance to selected cell"}
        desc = "Measure the color distance of each cell to the selected cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Or task.quarterBeat Then
            dst2 = runRedC(src, labels(2))
            dst0 = task.color

            Dim depthDistance As New List(Of Single)
            Dim colorDistance As New List(Of Single)
            Dim selectedMean As cv.Scalar = src(task.rc.rect).Mean(task.rc.mask)
            For Each rc In task.rcList
                colorDistance.Add(distance3D(selectedMean, src(rc.rect).Mean(rc.mask)))
                depthDistance.Add(distance3D(task.rc.depth, rc.depth))
            Next

            dst1.SetTo(0)
            dst3.SetTo(0)
            Dim maxColorDistance = colorDistance.Max()
            For i = 0 To task.rcList.Count - 1
                Dim rc = task.rcList(i)
                dst1(rc.rect).SetTo(255 - depthDistance(i) * 255 / task.MaxZmeters, rc.mask)
                dst3(rc.rect).SetTo(255 - colorDistance(i) * 255 / maxColorDistance, rc.mask)
            Next
        End If
    End Sub
End Class








Public Class RedCell_Binarize : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "Binarized image", "", "Relative gray image"}
        desc = "Separate the image into light and dark using RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst0 = src
        If task.heartBeat Or task.quarterBeat Then
            runRedC(src)
            dst2 = task.redC.dst2
            labels(2) = task.redC.labels(2)

            Dim grayMeans As New List(Of Single)
            Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            For Each rc In task.rcList
                Dim grayMean As cv.Scalar, grayStdev As cv.Scalar
                cv.Cv2.MeanStdDev(gray(rc.rect), grayMean, grayStdev, rc.mask)
                grayMeans.Add(grayMean(0))
            Next
            Dim min = grayMeans.Min
            Dim max = grayMeans.Max
            Dim avg = grayMeans.Average

            dst3.SetTo(0)
            For Each rc In task.rcList
                Dim color = (grayMeans(rc.index) - min) * 255 / (max - min)
                dst3(rc.rect).SetTo(color, rc.mask)
                dst1(rc.rect).SetTo(If(grayMeans(rc.index) > avg, 255, 0), rc.mask)
            Next
        End If
    End Sub
End Class






Public Class RedCell_FloodFill : Inherits TaskParent
    Dim flood As New Flood_Basics
    Dim stats As New RedCell_Basics
    Public Sub New()
        desc = "Provide cell stats on the flood_basics cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        flood.Run(src)

        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = flood.dst2
        labels = flood.labels
        SetTrueText(stats.strOut, 3)
    End Sub
End Class







Public Class RedCell_BasicsPlot : Inherits TaskParent
    Dim plot As New Hist_Depth
    Public runRedCloud As Boolean
    Dim stats As New RedCell_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        If standalone Then task.gOptions.setHistogramBins(20)
        desc = "Display the statistics for the selected cell."
    End Sub
    Public Sub statsString(src As cv.Mat)
        Dim tmp = New cv.Mat(task.rc.mask.Rows, task.rc.mask.Cols, cv.MatType.CV_32F, cv.Scalar.All(0))
        task.pcSplit(2)(task.rc.rect).CopyTo(tmp, task.rc.mask)
        plot.rc = task.rc
        plot.Run(tmp)
        dst3 = plot.dst2

        stats.statsString()
        strOut = stats.strOut
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Or runRedCloud Then
            dst2 = runRedC(src, labels(2))
            If task.ClickPoint = newPoint Then
                If task.rcList.Count > 1 Then
                    task.rc = task.rcList(1)
                    task.ClickPoint = task.rc.maxDist
                End If
            End If
        End If
        If task.heartBeat Then statsString(src)

        SetTrueText(strOut, 1)
        labels(1) = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + CStr(CInt(task.MaxZmeters)) + " meters"
    End Sub
End Class





Public Class RedCell_Generate : Inherits TaskParent
    Public mdList As New List(Of maskData)
    Public Sub New()
        task.rcMap = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        task.rcList = New List(Of rcData)
        desc = "Generate the RedCloud cells from the rects, mask, and pixel counts."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Static redMask As New RedMask_Basics
            redMask.Run(src)
            mdList = redMask.mdList
        End If

        Dim initialList As New List(Of rcData)
        For i = 0 To mdList.Count - 1
            Dim rc As New rcData
            rc.rect = mdList(i).rect
            If rc.rect.Size = dst2.Size Then Continue For ' RedColor_Basics can find a cell this big.  
            rc.mask = mdList(i).mask
            rc.maxDist = mdList(i).maxDist
            rc.maxDStable = rc.maxDist
            rc.indexLast = task.rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            rc.contour = mdList(i).contour
            ' rc.contour = ContourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            DrawContour(rc.mask, rc.contour, 255, -1)
            rc.pixels = mdList(i).mask.CountNonZero
            If rc.indexLast >= task.rcList.Count Then rc.indexLast = 0
            If rc.indexLast > 0 Then
                Dim lrc = task.rcList(rc.indexLast)
                rc.age = lrc.age + 1
                rc.color = lrc.color
                rc.depth = lrc.depth
                rc.depthMask = lrc.depthMask
                rc.depthPixels = lrc.depthPixels
                rc.mmX = lrc.mmX
                rc.mmY = lrc.mmY
                rc.mmZ = lrc.mmZ
                rc.maxDStable = lrc.maxDStable

                If rc.pixels < task.rcPixelThreshold Then
                    rc.color = task.rcOtherPixelColor
                Else
                    ' verify that the maxDStable is still good.
                    Dim v1 = task.rcMap.Get(Of Byte)(rc.maxDStable.Y, rc.maxDStable.X)
                    If v1 <> lrc.index Then
                        If rc.pixels > 20000 Then Dim k = 0

                        rc.maxDStable = rc.maxDist

                        rc.age = 1 ' a new cell was found that was probably part of another in the previous frame.
                        rc.color = randomCellColor()
                    End If
                End If
            Else
                rc.age = 1
                rc.color = randomCellColor()
            End If

            initialList.Add(rc)
        Next

        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)

        Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
        For Each rc In initialList
            rc.pixels = rc.mask.CountNonZero
            If rc.pixels = 0 Then Continue For

            rc.depthMask = rc.mask.Clone
            rc.depthMask.SetTo(0, task.noDepthMask(rc.rect))
            rc.depthPixels = rc.depthMask.CountNonZero

            If rc.depthPixels / rc.pixels > 0.1 Then
                rc.mmX = GetMinMax(task.pcSplit(0)(rc.rect), rc.depthMask)
                rc.mmY = GetMinMax(task.pcSplit(1)(rc.rect), rc.depthMask)
                rc.mmZ = GetMinMax(task.pcSplit(2)(rc.rect), rc.depthMask)

                cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev, rc.depthMask)
                rc.depth = depthMean(2)
                If Single.IsNaN(rc.depth) Or rc.depth < 0 Then rc.depth = 0
            End If

            sortedCells.Add(rc.pixels, rc)
        Next

        Dim rcNewCount As Integer
        For Each rc In task.rcList
            If rc.age = 1 Then rcNewCount += 1
        Next

        If task.heartBeat Then
            labels(2) = CStr(task.rcList.Count) + " total cells (shown with '" + task.redOptions.trackingLabel + "' and " +
                        CStr(task.rcList.Count - rcNewCount) + " matched to previous frame"
        End If
        dst2 = RebuildRCMap(sortedCells)
    End Sub
End Class