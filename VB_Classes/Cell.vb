Imports cv = OpenCvSharp
Public Class Cell_Basics : Inherits VB_Algorithm
    Dim plot As New Histogram_Depth
    Dim pca As New PCA_Basics
    Dim eq As New Plane_Equation
    Public runRedCloud As Boolean
    Public Sub New()
        If standaloneTest() Then gOptions.HistBinSlider.Value = 20
        desc = "Display the statistics for the selected cell."
    End Sub
    Public Sub statsString()
        If task.heartBeat Then
            Dim rc = task.rc

            Dim gridID = task.gridToRoiIndex.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
            strOut = "rc.index = " + CStr(rc.index) + vbTab + " gridID = " + CStr(gridID) + vbCrLf
            strOut += "rc.rect: " + CStr(rc.rect.X) + ", " + CStr(rc.rect.Y) + ", "
            strOut += CStr(rc.rect.Width) + ", " + CStr(rc.rect.Height) + vbCrLf + "rc.color = " + rc.color.ToString() + vbCrLf
            strOut += "rc.maxDist = " + CStr(rc.maxDist.X) + "," + CStr(rc.maxDist.Y) + vbCrLf

            strOut += If(rc.depthCell, "Cell is marked as depthCell " + vbCrLf, "")
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
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        setTrueText(strOut, 3)
        labels(1) = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + CStr(CInt(task.maxZmeters)) + " meters"
    End Sub
End Class








Public Class Cell_PixelCountCompare : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
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
                    If gOptions.DebugCheckBox.Checked Then
                        strOut = CStr(rc.pixels) + ", " + CStr(rc.depthPixels)
                    Else
                        strOut = Format(rc.depthPixels / rc.pixels, "0%")
                    End If
                    setTrueText(strOut, pt, 3)
                    missCount += 1
                End If
            End If
        Next
        If task.heartBeat Then labels(3) = "There were " + CStr(missCount) + " cells that contained an island of depth pixels - value = (pixels, depthpixels)"
    End Sub
End Class










Public Class Cell_Stable : Inherits VB_Algorithm
    Dim redC As New RedCloud_TightNew
    Public rcUnstableList As New List(Of rcData)
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        desc = "Use maxDStable to identify stable cells - cells which were NOT present in the previous generation."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        dst3 = dst2.Clone
        dst3(task.rc.rect).SetTo(task.rc.color, task.rc.mask)
        labels(2) = redC.labels(2)

        Static prevList As New List(Of cv.Point)
        If task.heartBeat Or task.frameCount = 2 Then
            prevList.Clear()
            For Each rc In task.redCells
                prevList.Add(rc.maxDStable)
            Next
        End If

        Dim unstableList As New List(Of rcData)
        For Each rc In task.redCells
            If prevList.Contains(rc.maxDStable) = False Then
                vbDrawContour(dst3(rc.rect), rc.contour, cv.Scalar.White, -1)
                vbDrawContour(dst3(rc.rect), rc.contour, cv.Scalar.Black)
                unstableList.Add(rc)
            End If
        Next

        If task.almostHeartBeat Then
            rcUnstableList = New List(Of rcData)(unstableList)
            labels(1) = CStr(rcUnstableList.Count) + " found before the heartbeat."
        End If
        dst1.SetTo(0)
        For Each rc In rcUnstableList
            dst1(rc.rect).SetTo(rc.color, rc.mask)
        Next

        labels(3) = CStr(unstableList.Count) + " cells weren't present in the previous generation."
    End Sub
End Class








Public Class Cell_StableAboveAverage : Inherits VB_Algorithm
    Dim redC As New RedCloud_TightNew
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(1) = "Black rectangles outline cells that were matched less than the average matchCount."
        desc = "Highligh cells that were present the max number of match counts."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3.SetTo(0)
        dst1.SetTo(255) ' the unstable mask 
        Dim unmatched As Integer
        For Each rc In task.redCells
            If rc.matchCount < task.rcMatchAvg Then
                dst3(rc.rect).SetTo(rc.color, rc.mask)
                dst1(rc.rect).SetTo(0)
            Else
                unmatched += 1
            End If
        Next

        labels(3) = CStr(unmatched) + " cells had matchCounts that were above average."
    End Sub
End Class








Public Class Cell_ValidateColorCells : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Cells shown below have rc.depthPixels / rc.pixels < 50%"
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
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
            If rc.depthCell = False Then dst1(rc.rect).SetTo(255, rc.mask)
            If rc.depthCell And rc.index > 0 Then
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
        setTrueText(strOut, 3)
    End Sub
End Class











Public Class Cell_JumpUp : Inherits VB_Algorithm
    Public redC As New RedCloud_TightNew
    Public jumpCells As New SortedList(Of Integer, cv.Vec2i)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Percent jump in size", 1, 100, 25)
        desc = "Identify cells that have jumped up in size since the last frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static percentSlider = findSlider("Percent jump in size")
        Dim percentJump = percentSlider.value / 100

        Dim lastCells = New List(Of rcData)(task.redCells)
        redC.Run(src)
        dst2 = redC.dst2
        If task.heartBeat Then dst3.SetTo(0)
        labels(2) = redC.labels(2)

        jumpCells.Clear()
        For Each rc In task.redCells
            If rc.indexLast > 0 Then
                Dim lrc = lastCells(rc.indexLast)
                If (rc.pixels - lrc.pixels) / rc.pixels >= percentJump Then
                    dst3(lrc.rect).SetTo(cv.Scalar.White, lrc.mask)
                    jumpCells.Add(rc.index, New cv.Vec2i(lrc.index, rc.index))
                End If
            End If
        Next
        If task.heartBeat Then labels(3) = "There were " + CStr(jumpCells.Count) + " cells jumped up more than " +
                                         Format(percentJump, "0%")
        If task.almostHeartBeat Then dst1 = dst3.Clone
    End Sub
End Class










Public Class Cell_JumpDown : Inherits VB_Algorithm
    Public redC As New RedCloud_TightNew
    Public jumpCells As New SortedList(Of Integer, cv.Vec2i)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Percent jump in size", 1, 100, 25)
        desc = "Identify cells that have jumped down in size since the last frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static percentSlider = findSlider("Percent jump in size")
        Dim percentJump = percentSlider.value / 100

        Dim lastCells = New List(Of rcData)(task.redCells)
        redC.Run(src)
        dst2 = redC.dst2
        If task.heartBeat Then dst3.SetTo(0)
        labels(2) = redC.labels(2)

        jumpCells.Clear()
        For Each rc In task.redCells
            If rc.indexLast > 0 Then
                Dim lrc = lastCells(rc.indexLast)
                If (lrc.pixels - rc.pixels) / rc.pixels >= percentJump Then
                    dst3(lrc.rect).SetTo(cv.Scalar.White, lrc.mask)
                    jumpCells.Add(rc.index, New cv.Vec2i(lrc.index, rc.index))
                End If
            End If
        Next
        If task.heartBeat Then labels(3) = "There were " + CStr(jumpCells.Count) + " cells jumped down more than " +
                                         Format(percentJump, "0%")
        If task.almostHeartBeat Then dst1 = dst3.Clone
    End Sub
End Class










Public Class Cell_JumpUnstable : Inherits VB_Algorithm
    Public redC As New RedCloud_TightNew
    Public jumpCells As New SortedList(Of Integer, cv.Vec2i)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Percent jump in size", 1, 100, 25)
        desc = "Identify cells that have changed size more than X% since the last frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static percentSlider = findSlider("Percent jump in size")
        Dim percentJump = percentSlider.value / 100

        If task.heartBeat Or task.midHeartBeat Then
            Dim lastCells As New List(Of rcData)(task.redCells)
            redC.Run(src)
            dst2 = redC.dst2
            dst3 = dst1.Clone
            dst1.SetTo(0)
            labels(2) = redC.labels(2)

            jumpCells.Clear()
            For Each rc In task.redCells
                If rc.indexLast > 0 Then
                    Dim lrc = lastCells(rc.indexLast)
                    If Math.Abs(lrc.pixels - rc.pixels) / rc.pixels >= percentJump Then
                        dst1(lrc.rect).SetTo(cv.Scalar.White, lrc.mask)
                        jumpCells.Add(rc.index, New cv.Vec2i(lrc.index, rc.index))
                    End If
                End If
            Next
            labels(3) = "There were " + CStr(jumpCells.Count) + " cells changed more than " + Format(percentJump, "0%") + " up or down"
        End If
    End Sub
End Class






Public Class Cell_Distance : Inherits VB_Algorithm
    Dim redC As New RedCloud_TightNew
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
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
                dst1(rc.rect).SetTo(255 - depthDistance(i) * 255 / task.maxZmeters, rc.mask)
                dst3(rc.rect).SetTo(255 - colorDistance(i) * 255 / maxColorDistance, rc.mask)
            Next
        End If
    End Sub
End Class








Public Class Cell_Binarize : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
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











Public Class Cell_DistanceDepth : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public colorOnly As New RedCloud_Cells
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Measure color distance from black for both color and depth cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Or task.quarterBeat Then
            redOptions.UseDepth.Checked = True
            redC.Run(src)
            dst2 = redC.dst2.Clone
            labels(2) = redC.labels(2)

            redOptions.UseColorOnly.Checked = True
            colorOnly.Run(src)
            dst3 = colorOnly.dst2.Clone
            labels(3) = colorOnly.labels(2)
        End If
    End Sub
End Class





Public Class Cell_Floodfill : Inherits VB_Algorithm
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
        setTrueText(stats.strOut, 3)
    End Sub
End Class







Public Class Cell_BasicsPlot : Inherits VB_Algorithm
    Dim plot As New Histogram_Depth
    Public runRedCloud As Boolean
    Dim stats As New Cell_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.HistBinSlider.Value = 20
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
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            If task.clickPoint = New cv.Point Then
                If task.redCells.Count > 1 Then
                    task.rc = task.redCells(1)
                    task.clickPoint = task.rc.maxDist
                End If
            End If
            identifyCells()
        End If
        If task.heartBeat Then statsString(src)

        setTrueText(strOut, 3)
        labels(1) = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + CStr(CInt(task.maxZmeters)) + " meters"
    End Sub
End Class








Public Class Cell_Generate : Inherits VB_Algorithm
    Public classCount As Integer
    Public rectData As cv.Mat
    Public floodPointData As cv.Mat
    Public removeContour As Boolean
    Public cellLimit As Integer = 255
    Public matchCount As Integer
    Public Sub New()
        task.cellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        task.redCells = New List(Of rcData)
        desc = "Generate the RedCloud cells from the rects, mask, and pixel counts."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static bounds As New Boundary_RemovedRects
            bounds.Run(src)
            dst1 = bounds.dst2
            task.cellMap = bounds.bRects.bounds.dst2
            src = task.cellMap Or dst1

            Static redCPP As New RedCloud_MaskNone_CPP
            redCPP.Run(src)

            If redCPP.classCount = 0 Then Exit Sub ' no data to process.
            classCount = redCPP.classCount
            rectData = redCPP.rectData
            floodPointData = redCPP.floodPointData
            removeContour = False
            src = redCPP.dst2
        End If

        Dim redCells = task.redCells

        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        Dim usedColors As New List(Of cv.Vec3b)
        Dim cellCount = Math.Min(cellLimit, classCount)
        For i = 1 To cellCount - 1
            Dim rc As New rcData
            rc.index = sortedCells.Count + 1
            rc.rect = rectData.Get(Of cv.Rect)(i - 1, 0)
            If rc.rect.Size = dst2.Size Then Continue For
            rc.mask = src(rc.rect).InRange(i, i)
            rc.floodPoint = floodPointData.Get(Of cv.Point)(i - 1, 0)

            rc.depthMask = rc.mask.Clone
            rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            vbDrawContour(rc.mask, rc.contour, 255, -1)
            If removeContour Then vbDrawContour(rc.mask, rc.contour, 0, 2) ' no overlap with neighbors.

            rc.maxDist = vbGetMaxDist(rc)

            If rc.color = black Then
                rc.maxDStable = rc.maxDist ' assume it has to use the latest.
                rc.indexLast = task.cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If rc.indexLast > 0 And rc.indexLast < redCells.Count Then
                    Dim lrc = redCells(rc.indexLast)
                    rc.color = lrc.color
                    Dim stableCheck = task.cellMap.Get(Of Byte)(lrc.maxDist.Y, lrc.maxDist.X)
                    If stableCheck = rc.indexLast Then rc.maxDStable = lrc.maxDStable ' keep maxDStable if cell matched to previous
                    rc.matchCount = If(lrc.matchCount > 100, lrc.matchCount, lrc.matchCount + 1)
                Else
                    rc.color = task.vecColors(rc.index)
                End If
            End If

            rc.pixels = rc.mask.CountNonZero
            If rc.pixels = 0 Then Continue For
            rc.depthMask.SetTo(0, task.noDepthMask(rc.rect))
            rc.depthPixels = rc.depthMask.CountNonZero
            rc.depthCell = rc.depthPixels > 0

            If rc.depthPixels Then
                task.pcSplit(0)(rc.rect).MinMaxLoc(rc.minVec.X, rc.maxVec.X, rc.minLoc, rc.maxLoc, rc.depthMask)
                task.pcSplit(1)(rc.rect).MinMaxLoc(rc.minVec.Y, rc.maxVec.Y, rc.minLoc, rc.maxLoc, rc.depthMask)
                task.pcSplit(2)(rc.rect).MinMaxLoc(rc.minVec.Z, rc.maxVec.Z, rc.minLoc, rc.maxLoc, rc.depthMask)

                cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), rc.depthMean, rc.depthStdev, rc.depthMask)
            End If
            cv.Cv2.MeanStdDev(task.color(rc.rect), rc.colorMean, rc.colorStdev, rc.mask)

            If usedColors.Contains(rc.color) Then rc.color = task.vecColors(rc.index)
            usedColors.Add(rc.color)

            sortedCells.Add(rc.pixels, rc)
        Next

        dst2.SetTo(0)
        task.cellMap.SetTo(0)
        task.redCells.Clear()
        task.redCells.Add(New rcData)
        matchCount = 0
        Dim matches As New List(Of Integer)
        For Each rc In sortedCells.Values
            Dim val = task.cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If val <> 0 Then Continue For ' already occupied.
            rc.index = task.redCells.Count
            task.redCells.Add(rc)

            If rc.indexLast <> 0 Then matchCount += 1
            matches.Add(rc.matchCount)

            vbDrawContour(task.cellMap(rc.rect), rc.contour, rc.color, task.lineWidth)
            task.cellMap(rc.rect).SetTo(rc.index, rc.mask)
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            vbDrawContour(dst2(rc.rect), rc.contour, rc.color, task.lineWidth)
        Next

        If matches.Count > 0 Then task.rcMatchAvg = matches.Average() Else task.rcMatchAvg = 0
        If task.heartBeat Then labels(2) = $"{task.redCells.Count} cells and {matchCount} were matched to the previous gen."
    End Sub
End Class