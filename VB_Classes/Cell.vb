Imports cv = OpenCvSharp
Public Class Cell_Basics : Inherits VB_Parent
    Dim plot As New Hist_Depth
    Dim pca As New PCA_Basics
    Dim eq As New Plane_Equation
    Public runRedCloud As Boolean
    Dim redC As New RedCloud_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setHistogramBins(20)
        desc = "Display the statistics for the selected cell."
    End Sub
    Public Sub statsString()
        If task.heartBeat Then
            Dim rc = task.rc

            Dim gridID = task.gridMap.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
            strOut = "rc.index = " + CStr(rc.index) + vbTab + " gridID = " + CStr(gridID) + vbCrLf
            strOut += "rc.rect: " + CStr(rc.rect.X) + ", " + CStr(rc.rect.Y) + ", "
            strOut += CStr(rc.rect.Width) + ", " + CStr(rc.rect.Height) + vbCrLf + "rc.color = " + rc.color.ToString() + vbCrLf
            strOut += "rc.maxDist = " + CStr(rc.maxDist.X) + "," + CStr(rc.maxDist.Y) + vbCrLf

            strOut += If(rc.depthPixels > 0, "Cell is marked as depthCell " + vbCrLf, "")
            If rc.depthPixels > 0 Then
                strOut += "depth pixels " + CStr(rc.pixels) + vbCrLf + "rc.depthPixels = " + CStr(rc.depthPixels) +
                      " or " + Format(rc.depthPixels / rc.pixels, "0%") + " depth " + vbCrLf
            Else
                strOut += "depth pixels " + CStr(rc.pixels) + " - no depth data" + vbCrLf
            End If

            strOut += "Depth Min/Max/Range: X = " + Format(rc.minVec.X, fmt1) + "/" + Format(rc.maxVec.X, fmt1)
            strOut += "/" + Format(rc.maxVec.X - rc.minVec.X, fmt1) + vbTab
            strOut += "Y = " + Format(rc.minVec.Y, fmt1) + "/" + Format(rc.maxVec.Y, fmt1)
            strOut += "/" + Format(rc.maxVec.Y - rc.minVec.Y, fmt1) + vbTab
            strOut += "Z = " + Format(rc.minVec.Z, fmt2) + "/" + Format(rc.maxVec.Z, fmt2)
            strOut += "/" + Format(rc.maxVec.Z - rc.minVec.Z, fmt2) + vbCrLf + vbCrLf

            strOut += "Cell Mean in 3D: x/y/z = " + vbTab + Format(rc.depthMean(0), fmt2) + vbTab
            strOut += Format(rc.depthMean(1), fmt2) + vbTab + Format(rc.depthMean(2), fmt2) + vbCrLf

            strOut += "Color Mean  RGB: " + vbTab + Format(rc.colorMean(0), fmt1) + vbTab + Format(rc.colorMean(1), fmt1) + vbTab
            strOut += Format(rc.colorMean(2), fmt1) + vbCrLf
            strOut += "Color Stdev RGB: " + vbTab + Format(rc.colorStdev(0), fmt1) + vbTab + Format(rc.colorStdev(1), fmt1) + vbTab
            strOut += Format(rc.colorStdev(2), fmt1) + vbCrLf

            Dim tmp = New cv.Mat(task.rc.mask.Rows, task.rc.mask.Cols, cv.MatType.CV_32F, 0)
            task.pcSplit(2)(task.rc.rect).CopyTo(tmp, task.rc.mask)
            plot.rc = task.rc
            plot.Run(tmp)
            dst1 = plot.dst2

            'If rc.depthMean(2) = 0 Then
            '    strOut += vbCrLf + "No depth data is available for that cell. "
            'Else
            '    eq.rc = rc
            '    eq.Run(src)
            '    rc = eq.rc
            '    strOut += vbCrLf + eq.strOut + vbCrLf

            '    pca.Run(empty)
            '    strOut += vbCrLf + pca.strOut
            'End If
        End If
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Or runRedCloud Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        statsString()
        SetTrueText(strOut, 3)
        labels(1) = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + CStr(CInt(task.MaxZmeters)) + " meters"
    End Sub
End Class








Public Class Cell_PixelCountCompare : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.gOptions.DebugChecked = True
        desc = "The rc.mask is filled and may completely contain depth pixels.  This alg finds cells that contain depth islands."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        Dim missCount As Integer
        For Each rc In task.redCells
            If rc.depthPixels <> 0 Then
                If rc.pixels <> rc.depthPixels Then
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                    Dim pt = New cv.Point(rc.maxDist.X - 10, rc.maxDist.Y)
                    If task.gOptions.DebugChecked Then
                        strOut = CStr(rc.pixels) + "/" + CStr(rc.depthPixels)
                    Else
                        strOut = Format(rc.depthPixels / rc.pixels, "0%")
                    End If
                    If missCount < task.redOptions.identifyCount Then SetTrueText(strOut, pt, 3)
                    missCount += 1
                End If
            End If
        Next
        If task.heartBeat Then labels(3) = "There were " + CStr(missCount) + " cells containing depth - showing rc.pixels/rc.depthpixels"
    End Sub
End Class







Public Class Cell_ValidateColorCells : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Cells shown below have rc.depthPixels / rc.pixels < 50%"
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, 0)
        desc = "Validate that all the depthCells are correctly identified."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst1.SetTo(0)
        dst3.SetTo(0)
        Dim percentDepth As New List(Of Single)
        For Each rc In task.redCells
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
            strOut = "There are color cells with depth in them - not good" + vbCrLf
        Else
            strOut = "There are no color cells with depth in them." + vbCrLf
        End If
        If percentDepth.Count > 0 Then
            strOut += "Depth cell percentage average " + Format(percentDepth.Average, "0%") + vbCrLf
            strOut += "Depth cell percentage range " + Format(percentDepth.Min, "0%") + " to " + Format(percentDepth.Max, "0%")
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Cell_Distance : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        If standalone Then task.gOptions.setDisplay1()
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, 0)
        labels = {"", "Depth distance to selected cell", "", "Color distance to selected cell"}
        desc = "Measure the color distance of each cell to the selected cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Or task.quarterBeat Then
            redC.Run(src)
            dst0 = task.color
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim depthDistance As New List(Of Single)
            Dim colorDistance As New List(Of Single)
            Dim selectedMean As cv.Scalar = src(task.rc.rect).Mean(task.rc.mask)
            For Each rc In task.redCells
                colorDistance.Add(distance3D(selectedMean, src(rc.rect).Mean(rc.mask)))
                depthDistance.Add(distance3D(task.rc.depthMean, rc.depthMean))
            Next

            dst1.SetTo(0)
            dst3.SetTo(0)
            Dim maxColorDistance = colorDistance.Max()
            For i = 0 To task.redCells.Count - 1
                Dim rc = task.redCells(i)
                dst1(rc.rect).SetTo(255 - depthDistance(i) * 255 / task.MaxZmeters, rc.mask)
                dst3(rc.rect).SetTo(255 - colorDistance(i) * 255 / maxColorDistance, rc.mask)
            Next
        End If
    End Sub
End Class








Public Class Cell_Binarize : Inherits VB_Parent
    Public redC As New RedCloud_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        If standaloneTest() Then task.gOptions.setDisplay1()
        dst1 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, 0)
        labels = {"", "Binarized image", "", "Relative gray image"}
        desc = "Separate the image into light and dark using RedCloud cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst0 = src
        If task.heartBeat Or task.quarterBeat Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim grayMeans As New List(Of Single)
            Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            For Each rc In task.redCells
                Dim grayMean As cv.Scalar, grayStdev As cv.Scalar
                cv.Cv2.MeanStdDev(gray(rc.rect), grayMean, grayStdev, rc.mask)
                grayMeans.Add(grayMean(0))
            Next
            Dim min = grayMeans.Min
            Dim max = grayMeans.Max
            Dim avg = grayMeans.Average

            dst3.SetTo(0)
            For Each rc In task.redCells
                Dim color = (grayMeans(rc.index) - min) * 255 / (max - min)
                dst3(rc.rect).SetTo(color, rc.mask)
                dst1(rc.rect).SetTo(If(grayMeans(rc.index) > avg, 255, 0), rc.mask)
            Next
        End If
    End Sub
End Class






Public Class Cell_Floodfill : Inherits VB_Parent
    Dim flood As New Flood_Basics
    Dim stats As New Cell_Basics
    Public Sub New()
        desc = "Provide cell stats on the flood_basics cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flood.Run(src)

        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = flood.dst2
        labels = flood.labels
        SetTrueText(stats.strOut, 3)
    End Sub
End Class







Public Class Cell_BasicsPlot : Inherits VB_Parent
    Dim plot As New Hist_Depth
    Public runRedCloud As Boolean
    Dim stats As New Cell_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.redOptions.setIdentifyCells(True)
        If standalone Then task.gOptions.setDisplay1()
        If standalone Then task.gOptions.setHistogramBins(20)
        desc = "Display the statistics for the selected cell."
    End Sub
    Public Sub statsString(src As cv.Mat)
        Dim tmp = New cv.Mat(task.rc.mask.Rows, task.rc.mask.Cols, cv.MatType.CV_32F, 0)
        task.pcSplit(2)(task.rc.rect).CopyTo(tmp, task.rc.mask)
        plot.rc = task.rc
        plot.Run(tmp)
        dst1 = plot.dst2

        stats.statsString()
        strOut = stats.strOut
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Or runRedCloud Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            If task.ClickPoint = New cv.Point Then
                If task.redCells.Count > 1 Then
                    task.rc = task.redCells(1)
                    task.ClickPoint = task.rc.maxDist
                End If
            End If
        End If
        If task.heartBeat Then statsString(src)

        SetTrueText(strOut, 3)
        labels(1) = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + CStr(CInt(task.MaxZmeters)) + " meters"
    End Sub
End Class












Public Class Cell_Stable : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Below are cells that were not exact matches."
        desc = "Identify cells which were NOT present in the previous generation."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
        If task.heartBeat Then Exit Sub

        Dim retained As Integer
        dst3.SetTo(0)
        For Each rc In task.redCells
            If rc.exactMatch Then
                retained += 1
            Else
                dst3(rc.rect).SetTo(rc.color, rc.mask)
            End If
        Next
        labels(3) = CStr(task.redCells.Count - retained) + " were not exact matches (shown below)"
    End Sub
End Class






Public Class Cell_Generate : Inherits VB_Parent
    Public classCount As Integer
    Public rectList As New List(Of cv.Rect)
    Public floodPoints As New List(Of cv.Point)
    Public removeContour As Boolean
    Dim diffLeft As New Diff_Basics
    Dim diffRight As New Diff_Basics
    Public useLeftImage As Boolean = True
    Dim bounds As New Boundary_RemovedRects
    Dim redCPP As RedCloud_CPP
    Public Sub New()
        task.cellMap = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
        task.redCells = New List(Of rcData)
        desc = "Generate the RedCloud cells from the rects, mask, and pixel counts."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            bounds.Run(src)
            task.cellMap = bounds.bRects.bounds.dst2
            src = task.cellMap Or bounds.dst2
            If task.FirstPass Then task.cellMap.SetTo(0)

            redCPP = bounds.bRects.bounds.redCPP

            If redCPP.classCount = 0 Then Exit Sub ' no data to process.
            classCount = redCPP.classCount
            rectList = redCPP.rectList
            floodPoints = redCPP.floodPoints
            removeContour = False
            src = redCPP.dst2
        End If

        If useLeftImage Then diffLeft.Run(task.leftView) Else diffRight.Run(task.rightView)

        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        Dim usedColors As New List(Of cv.Vec3b)({black})
        Dim retained As Integer
        Dim initList As New List(Of rcData)({New rcData})
        For i = 1 To classCount - 1
            Dim rc As New rcData
            rc.rect = rectList(i - 1)
            If rc.rect.Size = dst2.Size Then Continue For ' FeatureLess_RedCloud find a cell this big.  
            rc.floodPoint = floodPoints(i - 1)
            rc.mask = src(rc.rect).InRange(i, i)

            If task.heartBeat Or rc.indexLast = 0 Or rc.indexLast >= task.redCells.Count Then
                If useLeftImage Then
                    cv.Cv2.MeanStdDev(task.color(rc.rect), rc.colorMean, rc.colorStdev, rc.mask)
                Else
                    cv.Cv2.MeanStdDev(task.rightView(rc.rect), rc.colorMean, rc.colorStdev, rc.mask)
                End If
            Else
                rc.colorMean = task.redCells(rc.indexLast).colorMean
            End If

            rc.naturalColor = New cv.Vec3b(rc.colorMean(0), rc.colorMean(1), rc.colorMean(2))
            rc.naturalGray = CInt(rc.colorMean(2) * 0.299 + rc.colorMean(1) * 0.587 + rc.colorMean(0) * 0.114)

            rc.maxDist = GetMaxDist(rc)
            rc.indexLast = task.cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If useLeftImage Then rc.motionPixels = diffLeft.dst2(rc.rect).CountNonZero Else rc.motionPixels = diffRight.dst2(rc.rect).CountNonZero
            If rc.indexLast > 0 And rc.indexLast < task.redCells.Count Then
                Dim lrc = task.redCells(rc.indexLast)
                If (task.heartBeat = False Or task.FirstPass) And Math.Abs(lrc.naturalGray - rc.naturalGray) <= 1 And rc.motionPixels = 0 Then
                    rc = lrc
                    rc.exactMatch = True
                    retained += 1
                End If
            End If
            initList.Add(rc)
        Next

        For Each rc In initList
            If rc.exactMatch = False Then
                rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
                DrawContour(rc.mask, rc.contour, 255, -1)
                If removeContour Then DrawContour(rc.mask, rc.contour, 0, 2) ' no overlap with neighbors.

                rc.maxDStable = rc.maxDist ' assume it has to use the latest.
                rc.indexLast = task.cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)

                If rc.indexLast > 0 And rc.indexLast < task.redCells.Count Then
                    Dim lrc = task.redCells(rc.indexLast)
                    If task.heartBeat = False And Math.Abs(lrc.naturalGray - rc.naturalGray) <= 1 And rc.motionPixels = 0 Then
                        rc = lrc
                        rc.exactMatch = True
                    Else
                        rc.color = lrc.color
                        Dim stableCheck = task.cellMap.Get(Of Byte)(lrc.maxDist.Y, lrc.maxDist.X)
                        If stableCheck = rc.indexLast Then rc.maxDStable = lrc.maxDStable ' keep maxDStable if cell matched to previous
                        Dim val = task.cellMap.Get(Of Byte)(rc.maxDStable.Y, rc.maxDStable.X)
                        If val <> rc.indexLast Then rc.maxDStable = rc.maxDist ' maxDist has finally hit the edges of the cell.
                        rc.pointMatch = True
                    End If
                End If

                If rc.pointMatch = False And rc.exactMatch = False Then
                    rc.color = New cv.Vec3b(msRNG.Next(40, 220), msRNG.Next(40, 220), msRNG.Next(40, 220))
                End If

                If usedColors.Contains(rc.color) Then rc.color = task.vecColors(sortedCells.Count + 1)
                usedColors.Add(rc.color)

                rc.pixels = rc.mask.CountNonZero ' the number of pixels may have changed with the infill or contour.
                If rc.pixels = 0 Then Continue For

                rc.depthMask = rc.mask.Clone
                rc.depthMask.SetTo(0, task.noDepthMask(rc.rect))
                rc.depthPixels = rc.depthMask.CountNonZero

                If rc.depthPixels Then
                    task.pcSplit(0)(rc.rect).MinMaxLoc(rc.minVec.X, rc.maxVec.X, rc.minLoc, rc.maxLoc, rc.depthMask)
                    task.pcSplit(1)(rc.rect).MinMaxLoc(rc.minVec.Y, rc.maxVec.Y, rc.minLoc, rc.maxLoc, rc.depthMask)
                    task.pcSplit(2)(rc.rect).MinMaxLoc(rc.minVec.Z, rc.maxVec.Z, rc.minLoc, rc.maxLoc, rc.depthMask)

                    cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), rc.depthMean, rc.depthStdev, rc.depthMask)
                End If
            End If

            sortedCells.Add(rc.pixels, rc)
        Next

        task.redCells = New List(Of rcData)(sortedCells.Values)
        dst2 = RebuildCells(sortedCells)

        Static saveRetained As Integer = retained
        If retained > 0 Then saveRetained = retained
        If task.heartBeat Then labels(2) = CStr(task.redCells.Count) + " total cells with " + CStr(saveRetained) + " exact matches"
    End Sub
End Class