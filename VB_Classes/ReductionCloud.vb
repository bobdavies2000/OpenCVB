﻿Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class ReductionCloud_Basics : Inherits VB_Algorithm
    Public redCore As New ReductionCloud_Core
    Public buildCells As New GuidedBP_FloodCells
    Public redCells As New List(Of rcData)
    Public lastCells As New List(Of rcData)
    Public rcMatch As New ReductionCloud_Match
    Public showSelected As Boolean = True
    Public Sub New()
        gOptions.HistBinSlider.Value = 15
        desc = "Segment the image based only on the reduced point cloud (as opposed to back projection)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lastCells = New List(Of rcData)(redCells)
        dst0 = task.color.Clone
        redCore.Run(src)
        buildCells.inputMask = task.noDepthMask
        buildCells.Run(redCore.dst0)

        rcMatch.inputCells = buildCells.redCells
        rcMatch.Run(src)
        redCells = New List(Of rcData)(rcMatch.redCells)
        dst2 = rcMatch.dst2

        If showSelected Then task.rcSelect = rcMatch.showSelect()
        If heartBeat() Then labels(2) = rcMatch.labels(2)
    End Sub
End Class






Public Class ReductionCloud_Core : Inherits VB_Algorithm
    Dim options As New Options_RedCloud
    Public Sub New()
        desc = "Reduction transform for the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim reduceAmt = options.reduction
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        src.ConvertTo(dst0, cv.MatType.CV_32S, 1000 / reduceAmt)

        Dim split = dst0.Split()
        Select Case options.prepDataCase
            Case 0
                dst0 = split(0) * reduceAmt
            Case 1
                dst0 = split(1) * reduceAmt
            Case 2
                dst0 = split(2) * reduceAmt
            Case 3
                dst0 = split(0) * reduceAmt + split(1) * reduceAmt
            Case 4
                dst0 = split(0) * reduceAmt + split(2) * reduceAmt
            Case 5
                dst0 = split(1) * reduceAmt + split(2) * reduceAmt
            Case 6
                dst0 = split(0) * reduceAmt + split(1) * reduceAmt + split(2) * reduceAmt
        End Select

        Dim mm = vbMinMax(dst0)
        dst2 = (dst0 - mm.minVal)

        dst2.SetTo(mm.maxVal - mm.minVal, task.maxDepthMask)
        dst2.SetTo(0, task.noDepthMask)

        labels(2) = "Reduced Pointcloud - reduction factor = " + CStr(reduceAmt)
    End Sub
End Class








Public Class ReductionCloud_Match : Inherits VB_Algorithm
    Public inputCells As New List(Of rcData)
    Public redCells As New List(Of rcData)
    Public lastCells As New List(Of rcData)
    Public cellMap As New cv.Mat

    Dim lastCellMap As New cv.Mat
    Dim usedColors As New List(Of cv.Vec3b)({New cv.Vec3b(0, 0, 0)})
    Dim matchedCells As Integer
    Dim numBigCells As Integer
    Public Sub New()
        desc = "Match cells from the previous generation"
    End Sub
    Public Function showSelect() As rcData
        Return showSelection(dst2, redCells, cellMap)
    End Function
    Public Function redSelect() As rcData
        If task.drawRect <> New cv.Rect Then Return New rcData
        Dim index = cellMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
        If task.clickPoint = New cv.Point Then
            index = 0
            task.clickPoint = redCells(index).maxDist
        End If

        Dim rc = redCells(index)
        If index = task.redOther Then
            Dim gridID = task.gridToRoiIndex.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
            rc.rect = task.gridList(gridID)
            rc.mask = cellMap(rc.rect).InRange(task.redOther, task.redOther)
            rc.pixels = rc.mask.CountNonZero
            buildCell(rc)
        End If

        task.color.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
        vbDrawContour(task.color(rc.rect), rc.contour, cv.Scalar.White, 1)

        task.depthRGB.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
        vbDrawContour(task.depthRGB(rc.rect), rc.contour, cv.Scalar.White, 1)

        dst2(rc.rect).SetTo(cv.Scalar.White, rc.mask)
        dst2.Circle(rc.maxDist, task.dotSize, cv.Scalar.Black, -1, task.lineType)
        Return rc
    End Function
    Private Sub buildCell(ByRef rc As rcData)
        rc.maxDStable = rc.maxDist ' assume it has to use the latest.
        rc.indexLast = lastCellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
        rc.motionRect = rc.rect
        If rc.indexLast = numBigCells Then
            For i = rc.floodPoint.Y To Math.Min(rc.rect.Y + rc.rect.Height, dst2.Height) - 1
                rc.indexLast = lastCellMap.Get(Of Byte)(i, rc.floodPoint.X)
                If rc.indexLast = rc.index Then Exit For
            Next
        End If
        If rc.indexLast < lastCells.Count And rc.indexLast <> task.redOther Then
            Dim lrc = lastCells(rc.indexLast)
            rc.motionRect = rc.rect.Union(lrc.rect)
            rc.color = lrc.color

            Dim stableCheck = cellMap.Get(Of Byte)(lrc.maxDStable.Y, lrc.maxDStable.X)
            If stableCheck = rc.indexLast Then rc.maxDStable = lrc.maxDStable ' keep maxDStable if cell matched to previous
            matchedCells += 1
        Else
            dst3(rc.rect).SetTo(cv.Scalar.White, rc.mask)
        End If

        If usedColors.Contains(rc.color) Then
            rc.color = New cv.Vec3b(msRNG.Next(30, 240), msRNG.Next(30, 240), msRNG.Next(30, 240))
        End If

        usedColors.Add(rc.color)

        rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
        If rc.index <> 0 Then vbDrawContour(rc.mask, rc.contour, rc.index, -1)

        Dim minLoc As cv.Point, maxLoc As cv.Point
        task.pcSplit(0)(rc.rect).MinMaxLoc(rc.minVec.X, rc.maxVec.X, minLoc, maxLoc, rc.mask)
        task.pcSplit(1)(rc.rect).MinMaxLoc(rc.minVec.Y, rc.maxVec.Y, minLoc, maxLoc, rc.mask)
        task.pcSplit(2)(rc.rect).MinMaxLoc(rc.minVec.Z, rc.maxVec.Z, minLoc, maxLoc, rc.mask)

        Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
        cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev, rc.mask)

        ' If there Is no Then depth within the mask, estimate this color only cell Using rc.rect instead!
        If depthMean(2) = 0 Then
            rc.colorOnly = True
            cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev)
        End If
        rc.depthMean = New cv.Point3f(depthMean(0), depthMean(1), depthMean(2))
        rc.depthStdev = New cv.Point3f(depthStdev(0), depthStdev(1), depthStdev(2))

        cv.Cv2.MeanStdDev(task.color(rc.rect), rc.colorMean, rc.colorStdev, rc.mask)
    End Sub
    Public Sub RunVB(src As cv.Mat)
        numBigCells = 100 ' Math.Min(numSlider.value, inputCells.Count)

        If cellMap.Width <> src.Width Then cellMap = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)
        If standalone Then
            Static guided As New GuidedBP_Depth
            Static buildCells As New GuidedBP_FloodCells
            guided.Run(src)
            buildCells.inputMask = task.noDepthMask
            buildCells.Run(guided.backProject)
            inputCells = New List(Of rcData)(buildCells.redCells)
        End If

        If task.optionsChanged Or firstPass Then
            cellMap.SetTo(task.redOther)
            lastCells.Clear()
        End If

        lastCellMap = cellMap.Clone
        cellMap.SetTo(task.redOther)
        redCells.Clear()
        usedColors.Clear()
        usedColors.Add(black)
        matchedCells = 0

        If heartBeat() Then dst3.SetTo(0)
        cellMap.SetTo(numBigCells)
        dst2.SetTo(0)

        For Each rc In inputCells
            rc.index = redCells.Count
            rc.maxDist = vbGetMaxDist(rc)
            Dim spotTakenTest = cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If spotTakenTest <> numBigCells Then Continue For
            If rc.pixels > 0 And rc.pixels < task.minPixels Then Continue For
            buildCell(rc)
            redCells.Add(rc)

            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            dst2(rc.rect).SetTo(rc.color, rc.mask)

            If redCells.Count >= numBigCells - 1 Then Exit For
        Next

        Dim rcOther As New rcData
        rcOther.rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
        rcOther.floodPoint = New cv.Point(0, 0)
        rcOther.mask = cellMap.InRange(numBigCells, numBigCells)
        rcOther.pixels = rcOther.mask.CountNonZero
        rcOther.index = redCells.Count
        redCells.Add(rcOther)

        task.redOther = redCells.Count - 1
        cellMap.SetTo(task.redOther, rcOther.mask)

        Dim changed = redCells.Count - matchedCells
        Static changedTotal As Integer
        changedTotal += changed
        labels(3) = CStr(changedTotal) + " unmatched cells changed in the last second " +
                    Format(changedTotal / (task.frameCount - task.toggleFrame), fmt2) + " unmatched per frame"
        labels(2) = CStr(redCells.Count) + " cells (including other) " + CStr(matchedCells) + " matched to previous generation "
        If heartBeat() Then changedTotal = 0
        lastCells = New List(Of rcData)(redCells)
    End Sub
End Class







Public Class ReductionCloud_UnstableCells : Inherits VB_Algorithm
    Dim colorC As New ReductionCloud_Basics
    Dim diff As New Diff_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Identify cells that were not the same color in the previous generation"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorC.Run(src)
        dst2 = colorC.dst2
        labels(2) = colorC.labels(2)

        diff.Run(colorC.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        Static history As New List(Of cv.Mat)
        history.Add(diff.dst3)

        dst3.SetTo(0)
        For Each m In history
            dst3 = dst3 Or m
        Next
        If history.Count >= task.historyCount Then history.RemoveAt(0)

        dst2.SetTo(0, dst3)
    End Sub
End Class





Public Class ReductionCloud_UnstableCells1 : Inherits VB_Algorithm
    Dim colorC As New ReductionCloud_Basics
    Public redCells As New List(Of rcData)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Identify cells that were not the same color in the previous generation"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorC.Run(src)
        dst2 = colorC.dst2
        labels(2) = colorC.labels(2)

        dst3.SetTo(0)
        redCells.Clear()
        Static lastImage = colorC.dst2
        For Each rc In colorC.redCells
            Dim vecNew = dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            Dim vecOld = lastImage.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            If vecOld <> New cv.Vec3b Then
                If vecNew <> vecOld Then
                    dst2(rc.rect).SetTo(vecOld, rc.mask)
                    dst3(rc.rect).SetTo(255, rc.mask)
                    rc.color = vecOld
                    vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
                End If
            End If
            redCells.Add(rc)
        Next
        colorC.redCells = New List(Of rcData)(redCells)
        If heartBeat() Then lastImage = dst2.Clone
    End Sub
End Class








Public Class ReductionCloud_ByIndex : Inherits VB_Algorithm
    Dim colorC As New ReductionCloud_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If sliders.Setup(traceName) Then sliders.setupTrackBar("RedColor cell index", 1, 100, 1)
        labels = {"", "", "RedColor Output", ""}
        desc = "Select a RedColor cell using a slider."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static indexSlider = findSlider("RedColor cell index")

        colorC.Run(src)
        dst2 = colorC.dst2
        indexSlider.maximum = colorC.redCells.Count - 1

        Dim rc = colorC.redCells(indexSlider.value)
        dst0 = task.color.Clone
        dst0.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
        vbDrawContour(dst0(rc.rect), rc.contour, cv.Scalar.White, 1)
        vbDrawContour(dst2(rc.rect), rc.contour, cv.Scalar.White, -1)
        labels(2) = colorC.labels(2)
    End Sub
End Class







Public Class ReductionCloud_Track5D : Inherits VB_Algorithm
    Dim colorC As New ReductionCloud_Basics
    Public Sub New()
        If standalone And dst2.Width > 1000 Then gOptions.LineWidth.Value = 3
        desc = "Track all cells using color and location and a distance calculation in 5 dimensions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static lastdst2 = dst3.Clone
        dst3 = lastdst2
        Dim lastCells = New List(Of rcData)(colorC.redCells)

        colorC.Run(src)
        dst2 = colorC.dst2.Clone
        labels(2) = colorC.labels(2) + " - Lines connect cells that were matched incorrectly."

        Dim doubleSize As New cv.Mat, unMatched As Integer
        cv.Cv2.HConcat(dst2, dst3, doubleSize)
        Dim minPixels As Integer = task.minPixels * 2
        For i = 1 To colorC.redCells.Count - 1
            Dim rc = colorC.redCells(i)
            If rc.pixels < minPixels Then Continue For
            Dim lrc = findClosest5(lastCells, rc, minPixels)
            If lrc.index > 0 Then
                If rc.color <> lrc.color Then
                    lrc.maxDist.X += dst2.Width
                    doubleSize.Line(rc.maxDist, lrc.maxDist, cv.Scalar.Yellow, task.lineWidth, task.lineType)
                    unMatched += 1
                End If
            End If
        Next
        lastdst2 = dst2.Clone
        doubleSize(New cv.Rect(0, 0, dst2.Width, dst2.Height)).CopyTo(dst2)
        doubleSize(New cv.Rect(dst2.Width, 0, dst2.Width, dst2.Height)).CopyTo(dst3)
        Dim rcx = task.rcSelect
        task.clickPoint = rcx.maxDist

        If heartBeat() Then
            labels(3) = CStr(unMatched) + " cells were matched incorrectly out of " + CStr(colorC.redCells.Count) + " or " +
                        Format(unMatched / colorC.redCells.Count, "0%") + " - Yellow line shows where."
        End If
    End Sub
End Class








Public Class ReductionCloud_Track8D : Inherits VB_Algorithm
    Dim colorC As New ReductionCloud_Basics
    Public Sub New()
        desc = "Track a cell using its color and location - a distance calculation in 8 dimensions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorC.Run(src)
        dst2 = colorC.dst2
        labels(2) = colorC.labels(2)

        Dim rc = showSelection(dst2, colorC.redCells, colorC.rcMatch.cellMap)

        Static saveRC As rcData
        If task.mouseClickFlag Or firstPass Then saveRC = rc

        Dim m = saveRC.colorMean
        setTrueText("Looking for:" + vbCrLf + "Cell color mean B/G/R" + vbTab + Format(m(0), fmt2) + vbTab +
                    Format(m(1), fmt2) + vbTab + Format(m(2), fmt2) + vbCrLf +
                    "near " + saveRC.maxDist.ToString + vbCrLf + "With size about " + CStr(saveRC.pixels) + " pixels", 3)

        Dim rcClosest = findClosest8(colorC.redCells, saveRC, task.minPixels)

        dst3.SetTo(0)
        If rcClosest.index <> 0 Then
            saveRC = rcClosest
            task.clickPoint = rcClosest.maxDist
        End If
        vbDrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
    End Sub
End Class






Public Class ReductionCloud_NoDepth : Inherits VB_Algorithm
    Dim colorC As New ReductionCloud_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Minimum pixels %", 0, 100, 25)

        labels = {"", "", "RedColor cells with depth percentage less than threshold option", ""}
        desc = "Find RedColor cells only for areas with insufficient depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static minSlider = findSlider("Minimum pixels %")
        Dim minPixelPercent = minSlider.value

        colorC.Run(src)
        dst3 = colorC.dst2
        labels(3) = colorC.labels(2)

        Dim redCells As New List(Of rcData)
        For Each rc In colorC.redCells
            rc.mask.SetTo(0, task.depthMask(rc.rect))
            If rc.mask.CountNonZero / rc.pixels > minPixelPercent Then
                rc.mask.SetTo(0)
            Else
                rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxTC89L1)
                If rc.contour.Count > 0 Then vbDrawContour(rc.mask, rc.contour, 255, -1)
            End If
            redCells.Add(rc)
        Next

        colorC.redCells = New List(Of rcData)(redCells)

        dst2.SetTo(0)
        For Each rc In redCells
            vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
        Next
    End Sub
End Class







Public Class ReductionCloud_Plane3D : Inherits VB_Algorithm
    Dim colorC As New ReductionCloud_Basics
    Dim eq As New Plane_Equation
    Public Sub New()
        desc = "If a RedColor cell contains depth then build a plane equation"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorC.Run(src)
        dst2 = colorC.dst2
        labels(2) = colorC.labels(2)

        Dim rc = task.rcSelect
        If rc.maxVec.Z Then
            eq.rc = rc
            eq.Run(Nothing)
            rc = eq.rc
        End If

        dst3.SetTo(0)
        vbDrawContour(dst3(rc.rect), rc.contour, rc.color, -1)

        setTrueText(eq.strOut, 3)
    End Sub
End Class








Public Class ReductionCloud_BProject3D : Inherits VB_Algorithm
    Dim colorC As New ReductionCloud_Basics
    Dim bp3d As New Histogram3D_BP
    Public Sub New()
        desc = "Run ReductionCloud_Basics on the output of the RGB 3D backprojection"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bp3d.Run(src)
        dst3 = bp3d.dst2

        colorC.Run(dst3)
        dst2 = colorC.dst2
    End Sub
End Class






Public Class ReductionCloud_ByDepth : Inherits VB_Algorithm
    Dim colorC As New ReductionCloud_Basics
    Dim depth As New Depth_Tiers
    Public Sub New()
        desc = "Run RedCloud with depth layers - a reduced image view"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        depth.Run(src)
        dst3 = depth.dst2

        colorC.Run(dst3)
        dst2 = colorC.dst2
        labels = colorC.labels
    End Sub
End Class
Public Class RedColor_UnstableHulls : Inherits VB_Algorithm
    Dim colorC As New ReductionCloud_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDist changing (when maxDist hits the boundary of a cell)"}
        desc = "Use maxDist to identify unstable cells - cells which were NOT present in the previous generation."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorC.Run(src)
        dst2 = colorC.dst2
        labels(2) = colorC.labels(2)

        If heartBeat() Or task.frameCount = 2 Then
            dst1 = dst2.Clone
            dst3.SetTo(0)
        End If

        Dim currList As New List(Of cv.Point)
        Static prevList As New List(Of cv.Point)

        For Each rc In colorC.redCells
            rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
            If prevList.Contains(rc.maxDist) = False Then
                vbDrawContour(dst1(rc.rect), rc.hull, cv.Scalar.White, -1)
                vbDrawContour(dst1(rc.rect), rc.hull, cv.Scalar.Black)
                vbDrawContour(dst3(rc.rect), rc.hull, cv.Scalar.White, -1)
            End If
            currList.Add(rc.maxDist)
        Next

        prevList = New List(Of cv.Point)(currList)
    End Sub
End Class







Public Class ReductionCloud_Binarize : Inherits VB_Algorithm
    Dim binarize As New Binarize_RecurseAdd
    Dim redC As New ReductionCloud_Basics
    Public Sub New()
        labels(3) = "A 4-way split of the input grayscale image based on the amount of light"
        desc = "Use RedCloud on a 4-way split of the image based on light"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binarize.Run(src)
        dst3 = vbPalette(binarize.dst1 * 255 / 4)

        redC.Run(binarize.dst1)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class







Public Class ReductionCloud_CCompBinarized : Inherits VB_Algorithm
    Dim edges As New Edge_BinarizedSobel
    Dim ccomp As New ReductionCloud_Binarize
    Public Sub New()
        labels(3) = "Binarized Sobel output"
        desc = "Use the binarized edges to find the different blobs in the image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edges.Run(src)
        dst3 = edges.dst2

        ccomp.Run(dst3)
        dst2 = ccomp.dst2
        labels(2) = ccomp.labels(2)
    End Sub
End Class







' https://docs.opencv.org/master/de/d01/samples_2cpp_2connected_components_8cpp-example.html
Public Class ReductionCloud_CComp : Inherits VB_Algorithm
    Dim ccomp As New CComp_Both
    Dim redC As New ReductionCloud_Basics
    Public Sub New()
        desc = "Identify each Connected component as a RedCloud Cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(src)
        dst3 = ccomp.dst1.Clone
        redC.Run(dst3)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class








Public Class ReductionCloud_Diff : Inherits VB_Algorithm
    Dim diff As New Diff_RGBAccum
    Dim colorC As New ReductionCloud_Basics
    Public Sub New()
        labels = {"", "", "Diff output, RedCloud input", "RedCloud output"}
        desc = "Isolate blobs in the diff output with RedCloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("Wave at the camera to see the segmentation of the motion.", 3)
        diff.Run(src)
        dst2 = diff.dst2
        colorC.Run(dst2)

        dst3.SetTo(0)
        colorC.dst2.CopyTo(dst3, dst2)

        labels(3) = CStr(colorC.redCells.Count) + " objects identified in the diff output"
    End Sub
End Class








Public Class ReductionCloud_HistValley : Inherits VB_Algorithm
    Dim colorC As New ReductionCloud_Binarize
    Dim valley As New HistValley_Basics
    Dim dValley As New HistValley_Depth
    Dim canny As New Edge_Canny
    Public Sub New()
        desc = "Use ReductionCloud_Basics with the output of HistValley_Basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        valley.Run(src)
        dst1 = valley.dst1.Clone

        dValley.Run(src)
        canny.Run(dValley.dst1)
        dst1.SetTo(0, canny.dst2)

        canny.Run(valley.dst1)
        dst1.SetTo(0, canny.dst2)

        colorC.Run(dst1)
        dst2 = colorC.dst2
    End Sub
End Class






Public Class ReductionCloud_CellStats : Inherits VB_Algorithm
    Dim plot As New Histogram_Depth
    Public rc As New rcData
    Public redC As Object
    Dim pca As New PCA_Basics
    Dim eq As New Plane_Equation
    Public Sub New()
        If standalone Then redC = New RedCloud_Basics
        If standalone Then gOptions.displayDst1.Checked = True

        dst0 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        desc = "Display the statistics for the selected cell."
    End Sub
    Public Sub statsString(src As cv.Mat)
        If rc.index > 0 Then
            Dim tmp = New cv.Mat(rc.mask.Rows, rc.mask.Cols, cv.MatType.CV_32F, 0)
            task.pcSplit(2)(rc.rect).CopyTo(tmp, rc.mask)
            plot.rc = rc
            plot.Run(tmp)
            dst1 = plot.dst2
        End If

        Dim lrc = If(redC.lastCells.count > 0 And redC.lastcells.count > rc.indexLast,
        redC.lastCells(rc.indexLast), New rcData)

        strOut = "rc.index = " + CStr(rc.index) + " of " + CStr(task.redCells.Count) + vbTab
        strOut += "rc.indexlast = " + CStr(rc.indexLast) + " of " + CStr(redC.lastCells.Count) + vbTab
        Dim gridID = task.gridToRoiIndex.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
        Dim lastGridID = task.gridToRoiIndex.Get(Of Integer)(lrc.maxDist.Y, lrc.maxDist.X)
        strOut += " gridID = " + CStr(gridID) + vbTab + "lastGridID = " + CStr(lastGridID) + vbCrLf
        strOut += "rc.rect: " + CStr(rc.rect.X) + ", " + CStr(rc.rect.Y) + ", "
        strOut += CStr(rc.rect.Width) + ", " + CStr(rc.rect.Height) + vbTab

        strOut += "lrc.rect: " + CStr(lrc.rect.X) + ", " + CStr(lrc.rect.Y) + ", " + CStr(lrc.rect.Width) + ", "
        strOut += CStr(lrc.rect.Height) + vbCrLf
        strOut += "rc.pixels " + CStr(rc.pixels) + vbTab + vbTab + "lrc.pixels " + CStr(lrc.pixels) + vbTab
        strOut += "rc.color = " + rc.color.ToString() + vbCrLf

        strOut += "rc.maxDist = " + CStr(rc.maxDist.X) + ", " + CStr(rc.maxDist.Y) + vbTab
        strOut += "lrc.maxDist = " + CStr(lrc.maxDist.X) + ", " + CStr(lrc.maxDist.Y) + vbCrLf

        strOut += "Min/Max/Range: X = " + Format(rc.minVec.X, fmt1) + "/" + Format(rc.maxVec.X, fmt1)
        strOut += "/" + Format(rc.maxVec.X - rc.minVec.X, fmt1) + vbTab

        strOut += "Y = " + Format(rc.minVec.Y, fmt1) + "/" + Format(rc.maxVec.Y, fmt1)
        strOut += "/" + Format(rc.maxVec.Y - rc.minVec.Y, fmt1) + vbTab

        strOut += "Z = " + Format(rc.minVec.Z, fmt1) + "/" + Format(rc.maxVec.Z, fmt1)
        strOut += "/" + Format(rc.maxVec.X - rc.minVec.X, fmt1) + vbCrLf + vbCrLf

        strOut += "Cell Mean in 3D: x/y/z = " + vbTab + Format(rc.depthMean.X, fmt2) + vbTab
        strOut += Format(rc.depthMean.Y, fmt2) + vbTab + Format(rc.depthMean.Z, fmt2) + vbCrLf

        strOut += "Cell color mean B/G/R " + vbTab + Format(rc.colorMean(0), fmt2) + vbTab
        strOut += Format(rc.colorMean(1), fmt2) + vbTab + Format(rc.colorMean(2), fmt2) + vbCrLf

        strOut += "Cell color stdev B/G/R " + vbTab + Format(rc.colorStdev(0), fmt2) + vbTab
        strOut += Format(rc.colorStdev(1), fmt2) + vbTab + Format(rc.colorStdev(2), fmt2) + vbCrLf

        If rc.maxVec.Z > 0 Then
            eq.rc = rc
            eq.Run(src)
            rc = eq.rc
            strOut += vbCrLf + eq.strOut + vbCrLf

            pca.rc = rc
            pca.Run(Nothing)
            strOut += vbCrLf + pca.strOut
        End If
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        rc = task.rcSelect
        If (heartBeat() Or task.mouseClickFlag) And task.redCells.Count > 0 Then statsString(src)

        setTrueText(strOut, 3)

        labels(1) = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + CStr(CInt(task.maxZmeters)) + " meters"
    End Sub
End Class







Public Class ReductionCloud_Hulls : Inherits VB_Algorithm
    Dim convex As New Convex_RedCloudDefects
    Dim colorC As New ReductionCloud_Basics
    Public redCells As New List(Of rcData)
    Public cellMap As New cv.Mat
    Public Sub New()
        cellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Improved contour results using OpenCV's ConvexityDefects"
        desc = "Add hulls and improved contours using ConvexityDefects to each RedColor cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorC.Run(src)
        dst2 = colorC.dst2

        dst3.SetTo(0)
        redCells.Clear()
        Dim defectCount As Integer
        For Each rc In colorC.redCells
            rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
            Dim hullIndices = cv.Cv2.ConvexHullIndices(rc.hull.ToArray, False)
            Try
                Dim defects = cv.Cv2.ConvexityDefects(rc.contour, hullIndices)
                rc.contour = convex.betterContour(rc.contour, defects)
            Catch ex As Exception
                Console.WriteLine("Defect encountered in the rc.contour - see 'RedColor_Hulls' defectCount")
                defectCount += 1
            End Try
            redCells.Add(rc)

            vbDrawContour(cellMap(rc.rect), rc.hull, rc.index, -1)
            vbDrawContour(dst3(rc.rect), rc.hull, rc.color, -1)
            If standalone Then dst3.Circle(rc.maxDist, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next

        showSelection(dst3, redCells, cellMap)
        labels(2) = CStr(redCells.Count) + " hulls identified below.  " + CStr(defectCount) + " hulls failed to build the defect list."
    End Sub
End Class









Public Class ReductionCloud_LineID : Inherits VB_Algorithm
    Public lines As New Line_Basics
    Public rCells As New List(Of rcData)
    Dim p1list As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
    Dim p2list As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
    Dim rectList As New List(Of cv.Point)
    Dim maxDistance As Integer
    Public colorC As New ReductionCloud_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Width of line detected in the image", 1, 10, 2)
            sliders.setupTrackBar("Width of Isolating line", 2, 10, 5)
            sliders.setupTrackBar("Max distance between point and rect", 1, 20, 10)
        End If

        gOptions.useMotion.Checked = False
        labels(3) = "Input to RedCloud"
        desc = "Identify and isolate each line in the current image"
    End Sub
    Private Function connectDistance(rpt As cv.Point) As Integer
        For i = 0 To p1list.Count - 1
            Dim dist = p1list.ElementAt(i).Value.DistanceTo(rpt)
            If dist < maxDistance Then Return i
        Next
        Return -1
    End Function
    Public Sub RunVB(src As cv.Mat)
        Static lineSlider = findSlider("Width of line detected in the image")
        Static isoSlider = findSlider("Width of Isolating line")
        Static distSlider = findSlider("Max distance between point and rect")
        Dim lineWidth = lineSlider.Value
        Dim isolineWidth = isoSlider.Value
        maxDistance = distSlider.Value

        lines.Run(src)
        If lines.sortLength.Count = 0 Then Exit Sub

        Static rInput = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        If heartBeat() Then rInput.setto(0)
        p1list.Clear()
        For i = lines.sortLength.Count - 1 To 0 Step -1
            Dim mps = lines.mpList(lines.sortLength.ElementAt(i).Value)
            rInput.Line(mps.p1, mps.p2, 0, isolineWidth, cv.LineTypes.Link4)
            rInput.Line(mps.p1, mps.p2, 255, lineWidth, cv.LineTypes.Link4)
            p1list.Add(mps.p1.Y, mps.p1)
        Next

        If heartBeat() Then
            If rInput.Type = cv.MatType.CV_32SC1 Then rInput.convertto(rInput, cv.MatType.CV_8U)

            colorC.Run(rInput)
            dst2.SetTo(0)
            For Each rc In colorC.redCells
                If rc.rect.Width = 0 Or rc.rect.Height = 0 Then Continue For
                If rc.rect.Width < dst2.Width / 2 Or rc.rect.Height < dst2.Height / 2 Then dst2(rc.rect).SetTo(rc.color, rc.mask)
            Next

            If colorC.redCells.Count < 3 Then Exit Sub ' dark room - no cells.

            Dim rcLargest As New rcData
            For Each rc In colorC.redCells
                If rc.rect.Width > dst2.Width / 2 And rc.rect.Height > dst2.Height / 2 Then Continue For
                If rc.pixels > rcLargest.pixels Then rcLargest = rc
            Next

            dst2.Rectangle(rcLargest.rect, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
            labels(2) = CStr(colorC.redCells.Count) + " lines were identified.  Largest line detected is highlighted in yellow"
        End If
    End Sub
End Class










Public Class ReductionCloud_KNNCenters : Inherits VB_Algorithm
    Dim lines As New ReductionCloud_LineID
    Dim knn As New KNN_Lossy
    Dim ptTrace As New List(Of List(Of cv.Point))
    Public Sub New()
        labels = {"", "", "Line_ID output", "KNN_Basics output"}
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "use the mid-points in each line with KNN and identify the movement in each line"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        knn.queries.Clear()
        For Each rc In lines.colorC.redCells
            knn.queries.Add(rc.maxDist)
        Next

        knn.Run(Nothing)

        Dim trace As New List(Of cv.Point2f)
        Static regularPt As New List(Of cv.Point2f)
        If heartBeat() Then
            dst3.SetTo(0)
            regularPt.Clear()
        End If
        Dim preciseCount As Integer

        For i = 0 To knn.matches.Count - 1
            Dim mps = knn.matches(i)
            Dim distance = mps.p1.DistanceTo(mps.p2)
            If distance <= 2 Then
                regularPt.Add(mps.p1)
                dst3.Set(Of Byte)(mps.p2.Y, mps.p2.X, 255)
                preciseCount += 1
            End If
        Next
        labels(3) = CStr(preciseCount) + " of " + CStr(knn.matches.Count) + " KNN_One_To_One matches"
    End Sub
End Class







Public Class ReductionCloud_ProjectCell : Inherits VB_Algorithm
    Dim topView As New Histogram_ShapeTop
    Dim sideView As New Histogram_ShapeSide
    Dim mats As New Mat_4Click
    Dim colorC As New ReductionCloud_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Top: XZ values and mask, Bottom: ZY values and mask"
        desc = "Visualize the top and side projection of a RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorC.Run(src)
        dst1 = colorC.dst2

        labels(2) = colorC.labels(2)

        Dim rc = task.rcSelect

        Dim pc = New cv.Mat(rc.rect.Height, rc.rect.Width, cv.MatType.CV_32FC3, 0)
        task.pointCloud(rc.rect).CopyTo(pc, rc.mask)

        topView.rc = rc
        topView.Run(pc)

        sideView.rc = rc
        sideView.Run(pc)

        mats.mat(0) = topView.dst2
        mats.mat(1) = topView.dst3
        mats.mat(2) = sideView.dst2
        mats.mat(3) = sideView.dst3
        mats.Run(Nothing)
        dst2 = mats.dst2
        dst3 = mats.dst3

        Dim padX = dst2.Width / 15
        Dim padY = dst2.Height / 20
        strOut = "Top" + vbTab + "Top Mask" + vbCrLf + vbCrLf + "Side" + vbTab + "Side Mask"
        setTrueText(strOut, New cv.Point(dst2.Width / 2 - padX, dst2.Height / 2 - padY), 2)
        setTrueText("Select a RedCloud cell above to project it into the top and side views at left.", 3)
    End Sub
End Class











Public Class ReductionCloud_ContourCorners : Inherits VB_Algorithm
    Public corners(4 - 1) As cv.Point
    Public rc As New rcData
    Public Sub New()
        labels(2) = "The RedCloud Output with the highlighted contour to smooth"
        desc = "Find the point farthest from the center in each cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static colorC As New RedCloud_Basics
            colorC.Run(src)
            dst2 = colorC.dst2
            labels(2) = colorC.labels(2)
            rc = task.rcSelect
        End If

        dst3.SetTo(0)
        dst3.Circle(rc.maxDist, task.dotSize, cv.Scalar.White, task.lineWidth)
        Dim center As New cv.Point(rc.maxDist.X - rc.rect.X, rc.maxDist.Y - rc.rect.Y)
        Dim maxDistance(4 - 1) As Single
        For i = 0 To corners.Length - 1
            corners(i) = center ' default is the center - a triangle shape can omit a corner
        Next
        If rc.contour Is Nothing Then Exit Sub
        For Each pt In rc.contour
            Dim quad As Integer
            If pt.X - center.X >= 0 And pt.Y - center.Y <= 0 Then quad = 0 ' upper right quadrant
            If pt.X - center.X >= 0 And pt.Y - center.Y >= 0 Then quad = 1 ' lower right quadrant
            If pt.X - center.X <= 0 And pt.Y - center.Y >= 0 Then quad = 2 ' lower left quadrant
            If pt.X - center.X <= 0 And pt.Y - center.Y <= 0 Then quad = 3 ' upper left quadrant
            Dim dist = center.DistanceTo(pt)
            If dist > maxDistance(quad) Then
                maxDistance(quad) = dist
                corners(quad) = pt
            End If
        Next

        vbDrawContour(dst3(rc.rect), rc.contour, cv.Scalar.White)
        For i = 0 To corners.Count - 1
            dst3(rc.rect).Line(center, corners(i), cv.Scalar.White, task.lineWidth, task.lineType)
        Next
    End Sub
End Class










Public Class ReductionCloud_KMeans : Inherits VB_Algorithm
    Dim km As New KMeans_MultiChannel
    Dim colorC As New ReductionCloud_Basics
    Public Sub New()
        labels = {"", "", "KMeans_MultiChannel output", "ReductionCloud_Basics output"}
        desc = "Use RedCloud to identify the regions created by kMeans"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(src)
        dst2 = km.dst2

        colorC.Run(km.dst3)
        dst3 = colorC.dst2
    End Sub
End Class