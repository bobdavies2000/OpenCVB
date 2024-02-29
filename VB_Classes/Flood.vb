Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Flood_Basics : Inherits VB_Algorithm
    Public redCells As New List(Of rcData)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Dim binar4 As New Binarize_Split4
    Public Sub New()
        cPtr = RedCloud_Open()
        redOptions.DesiredCellSlider.Value = 50
        vbAddAdvice(traceName + ": redOptions 'Desired RedCloud Cells' determines how many regions are isolated.")
        labels(3) = "FloodFill regions - click to isolate a cell."
        desc = "Simple Floodfill each region but prepare the mask, rect, floodpoints, and pixel counts."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.motionDetected = False Then
            identifyCells(redCells)
            Exit Sub ' nothing has changed.
        End If

        binar4.Run(task.color) ' always run split4 to get colors below...
        If src.Channels = 1 Then src += binar4.dst2 Else src = binar4.dst2

        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), 0, src.Rows, src.Cols, src.Type,
                                    redOptions.DesiredCellSlider.Value, 0, 1, 0)
        handleInput.Free()

        Dim classCount = RedCloud_Count(cPtr)
        If classCount = 0 Then Exit Sub ' no data to process.
        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone

        Dim rectData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))
        Dim floodPointData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC2, RedCloud_FloodPoints(cPtr))
        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To classCount - 1
            Dim rc As New rcData
            rc.index = sortedCells.Count + 1
            rc.rect = validateRect(rectData.Get(Of cv.Rect)(i, 0))
            rc.mask = dst1(rc.rect).InRange(rc.index, rc.index).Threshold(0, 255, cv.ThresholdTypes.Binary)

            rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            vbDrawContour(rc.mask, rc.contour, 255, -1)

            rc.floodPoint = floodPointData.Get(Of cv.Point)(i, 0)
            rc.maxDist = vbGetMaxDist(rc)

            rc.color = binar4.dst3.Get(Of cv.Vec3b)(rc.floodPoint.Y, rc.floodPoint.X)
            rc.pixels = rc.mask.CountNonZero
            sortedCells.Add(rc.pixels, rc)
        Next

        dst2.SetTo(0)
        dst3.SetTo(0)
        cellMap.SetTo(0)
        redCells.Clear()
        redCells.Add(New rcData)
        For Each ele In sortedCells
            Dim rc = ele.Value
            rc.index = redCells.Count
            redCells.Add(rc)
            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            dst3(rc.rect).SetTo(rc.color, rc.mask)
            dst2.Circle(rc.maxDist, task.dotSize, task.highlightColor, -1, task.lineType)
        Next

        setSelectedContour(redCells, cellMap)
        identifyCells(redCells)

        labels(2) = $"FloodFill results with values varying from 0 to {classCount}.  The top {identifyCount} are identified."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
    End Sub
End Class






Public Class Flood_BasicsOld : Inherits VB_Algorithm
    Public classCount As Integer
    Public redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        labels(3) = "The flooded cells numbered from largest to smallast"
        desc = "FloodFill the input and paint it"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.cellMap
        dst3 = redC.dst2
        labels(2) = redC.labels(2)
        If standaloneTest() Then identifyCells(redC.redCells)
        classCount = redC.redCells.Count
    End Sub
End Class





Public Class Flood_Point : Inherits VB_Algorithm
    Public pixelCount As Integer
    Public rect As cv.Rect
    Dim edges As New Edge_BinarizedSobel
    Public centroid As cv.Point2f
    Public initialMask As New cv.Mat
    Public pt As cv.Point ' this is the floodfill point
    Dim options As New Options_Flood
    Public Sub New()
        labels(2) = "Input image to floodfill"
        labels(3) = If(standaloneTest(), "Flood_Point standaloneTest() just shows the edges", "Resulting mask from floodfill")
        desc = "Use floodfill at a single location in a grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst2 = src.Clone()
        If standaloneTest() Then
            pt = New cv.Point(msRNG.Next(0, dst2.Width - 1), msRNG.Next(0, dst2.Height - 1))
            edges.Run(src)
            dst2 = edges.mats.dst2
            dst3 = edges.mats.mat(edges.mats.quadrant)
        Else
            Dim maskPlus = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8UC1, 0)
            Dim maskRect = New cv.Rect(1, 1, dst2.Width, dst2.Height)

            Dim zero = New cv.Scalar(0)
            pixelCount = cv.Cv2.FloodFill(dst2, maskPlus, New cv.Point(CInt(pt.X), CInt(pt.Y)), cv.Scalar.White, rect, zero, zero, options.floodFlag Or (255 << 8))
            dst3 = maskPlus(maskRect).Clone
            pixelCount = pixelCount
            Dim m = cv.Cv2.Moments(maskPlus(rect), True)
            centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
            labels(3) = CStr(pixelCount) + " pixels at point pt(x=" + CStr(pt.X) + ",y=" + CStr(pt.Y)
        End If
    End Sub
End Class










Public Class Flood_Click : Inherits VB_Algorithm
    Dim edges As New Edge_BinarizedSobel
    Dim flood As New Flood_Point
    Public Sub New()
        flood.pt = New cv.Point(msRNG.Next(0, dst2.Width - 1), msRNG.Next(0, dst2.Height - 1))
        labels = {"", "", "Click anywhere TypeOf floodfill that area.", "Edges for 4 different light levels."}
        desc = "FloodFill where the mouse clicks"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.mouseClickFlag Then
            flood.pt = task.clickPoint
            task.mouseClickFlag = False ' preempt any other uses
        End If

        edges.Run(src)
        dst2 = edges.dst3

        If flood.pt.X Or flood.pt.Y Then
            flood.Run(dst2.Clone)
            dst2.CopyTo(dst3)
            If flood.pixelCount > 0 Then dst3.SetTo(255, flood.dst3)
        End If
    End Sub
End Class









Public Class Flood_TopX : Inherits VB_Algorithm
    Dim flood As New Flood_PointList
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Show the X largest regions in FloodFill", 1, 100, 50)
        desc = "Get the top X size regions in the floodfill output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static topXslider = findSlider("Show the X largest regions in FloodFill")
        Static desiredRegions As Integer
        If desiredRegions <> topXslider.Value Then
            flood.pointList.Clear()
            desiredRegions = topXslider.Value
        End If

        flood.Run(src)
        dst3 = flood.dst2

        Dim rc As New rcData
        If task.motionFlag Or task.optionsChanged Then dst2.SetTo(0)
        Dim rcCount = Math.Min(flood.redCells.Count, topXslider.Value)

        For i = 1 To rcCount - 1
            rc = flood.redCells(i)
            Dim c = src.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            c = New cv.Vec3b(If(c(0) < 50, 50, c(0)), If(c(1) < 50, 50, c(1)), If(c(2) < 50, 50, c(2)))
            dst2(rc.rect).SetTo(c, rc.mask)
        Next
        labels(2) = CStr(rcCount) + " regions were found.  Use the nearby slider to increase."
    End Sub
End Class










Public Class Flood_PointList : Inherits VB_Algorithm
    Public pointList As New List(Of cv.Point)
    Public redCells As New List(Of rcData)
    Public cellMap As cv.Mat
    Public options As New Options_Flood
    Dim reduction As New Reduction_Basics
    Public Sub New()
        cellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "", "Grid Dots are the input points."}
        desc = "The bare minimum to use floodfill - supply points, get mask and rect"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst3 = src.Clone

        reduction.Run(src)
        dst0 = reduction.dst2.Clone

        If task.optionsChanged Or standaloneTest() Then
            For y = options.stepSize To dst3.Height - 1 Step options.stepSize
                For x = options.stepSize To dst3.Width - options.stepSize - 1 Step options.stepSize
                    Dim p1 = New cv.Point(x, y)
                    pointList.Add(p1)
                    dst3.Circle(p1, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
                Next
            Next

            If task.optionsChanged Then
                redCells.Clear()
                cellMap.SetTo(0)
                dst2.SetTo(0)
            End If
        End If

        Dim rect As New cv.Rect

        Dim totalPixels As Integer
        Dim SizeSorted As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        Dim maskPlus = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8UC1, 0)
        Dim index = 1
        For Each pt In pointList
            Dim floodFlag = cv.FloodFillFlags.FixedRange Or (index << 8)
            Dim rc = New rcData
            rc.pixels = cv.Cv2.FloodFill(dst0, maskPlus, pt, 255, rc.rect, 0, 0, floodFlag)
            If rc.pixels >= options.minPixels And rc.rect.Width < dst2.Width And rc.rect.Height < dst2.Height And
               rc.rect.Width > 0 And rc.rect.Height > 0 Then
                rc.mask = maskPlus(rc.rect).InRange(index, index)

                rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone)
                rc.maxDist = vbGetMaxDist(rc)

                totalPixels += rc.pixels
                SizeSorted.Add(rc.pixels, rc)
            End If
        Next
        labels(2) = CStr(SizeSorted.Count) + " regions were found with " + Format(totalPixels / dst0.Total, "0%") + " pixels flooded"

        Dim lastcellMap = cellMap.Clone
        Dim lastCells = New List(Of rcData)(redCells)
        cellMap.SetTo(0)
        redCells.Clear()
        redCells.Add(New rcData) ' stay away from 0...

        dst2.SetTo(0)
        Dim usedColors As New List(Of cv.Vec3b)({black})
        For i = 0 To SizeSorted.Count - 1
            Dim rc = SizeSorted.ElementAt(i).Value
            rc.index = redCells.Count
            Dim lrc = If(lastCells.Count > 0, lastCells(lastcellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)), New rcData)
            rc.indexLast = lrc.index
            rc.color = lrc.color
            If usedColors.Contains(rc.color) Then rc.color = randomCellColor()

            vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
            vbDrawContour(cellMap(rc.rect), rc.contour, rc.index, -1)

            redCells.Add(rc)
            usedColors.Add(rc.color)
        Next
    End Sub
End Class








Public Class Flood_FeaturelessHulls : Inherits VB_Algorithm
    Public classCount As Integer
    Dim redC As New RedCloud_Basics
    Dim redCells As New List(Of rcData)
    Public Sub New()
        redOptions.UseColor.Checked = True
        labels = {"", "", "", "Palette output of image at left"}
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "FloodFill the input and paint it with LUT"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            Static fless As New FeatureLess_Basics
            fless.Run(src)
            src = fless.dst2
        End If

        redC.Run(src)
        classCount = redC.redCells.Count

        dst2.SetTo(0)
        redCells.Clear()
        For Each rp In redC.redCells
            Dim contour = contourBuild(rp.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            Dim hull = cv.Cv2.ConvexHull(contour, True).ToList
            vbDrawContour(dst2(rp.rect), hull, rp.index, -1)
            redCells.Add(rp)
        Next

        labels(2) = "Hulls were added for each of the " + CStr(redCells.Count) + " cells identified"
        dst3 = vbPalette(dst2 * 255 / redCells.Count)
    End Sub
End Class







Public Class Flood_TierTest : Inherits VB_Algorithm
    Dim flood As New Flood_Basics
    Dim tiers As New Depth_Tiers
    Public Sub New()
        desc = "Add depth tiers to the 8uc1 input to flood_basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        tiers.Run(src)

        dst1 = tiers.dst2
        dst1.SetTo(0, task.noDepthMask)

        flood.Run(dst1)
        dst2 = flood.dst2
        dst3 = flood.dst3
        labels = flood.labels
    End Sub
End Class






Public Class Flood_Tiers : Inherits VB_Algorithm
    Dim flood As New Flood_Basics
    Dim tiers As New Contour_DepthTiers
    Dim plot As New Plot_Histogram
    Public redCells As New List(Of rcData)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        plot.removeZeroEntry = False
        desc = "Subdivide the Flood_Basics cells using depth tiers."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flood.Run(src)
        dst2 = flood.dst3

        tiers.Run(src)

        redCells.Clear()
        cellMap.SetTo(0)
        Dim ranges = {New cv.Rangef(-1, tiers.classCount + 1)}
        For Each rc In flood.redCells
            rc.depthMask = rc.mask And task.depthMask(rc.rect)
            If rc.depthMask.CountNonZero Then
                cv.Cv2.CalcHist({tiers.dst2(rc.rect)}, {0}, New cv.Mat, rc.tierHist, 1, {tiers.classCount}, ranges)

                ReDim rc.tierHistArray(tiers.classCount - 1)
                Dim samples(rc.tierHist.Total - 1) As Single
                Marshal.Copy(rc.tierHist.Data, rc.tierHistArray, 0, rc.tierHistArray.Length)
            End If
            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            redCells.Add(rc)
        Next

        setSelectedContour(redCells, cellMap)

        If task.rc.tierHist.Rows > 0 Then
            plot.Run(task.rc.tierHist)
            dst3 = plot.dst2
        End If
        identifyCells(redCells)
    End Sub
End Class





Public Class Flood_Minimal : Inherits VB_Algorithm
    Public redCells As New List(Of rcData)
    Public cellMap As New cv.Mat
    Public desiredCells As Integer = 10
    Public rc As rcData
    Public Sub New()
        cPtr = RedCloud_Open()
        desc = "A minimilist version of Flood_Basics so individual cells can be broken down."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then
            setTrueText("Input to Flood_Minimal is always a single color only cell to be broken down.")
            Exit Sub
        End If

        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim inputMask As cv.Mat = Not rc.mask
        Dim maskData(inputMask.Total - 1) As Byte
        Marshal.Copy(inputMask.Data, maskData, 0, maskData.Length)
        Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)

        Dim imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), src.Rows, src.Cols,
                                    src.Type, redOptions.DesiredCellSlider.Value, 0, 1, 0)
        handleMask.Free()
        handleInput.Free()

        cellMap = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)
        Dim classCount = RedCloud_Count(cPtr)
        If classCount = 0 Then Exit Sub ' no data to process.
        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone

        Dim rectData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))
        Dim floodPointData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC2, RedCloud_FloodPoints(cPtr))
        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To classCount - 1
            Dim rc As New rcData
            rc.index = sortedCells.Count + 1
            rc.rect = validateRect(rectData.Get(Of cv.Rect)(i, 0))
            rc.mask = dst1(rc.rect).InRange(rc.index, rc.index).Threshold(0, 255, cv.ThresholdTypes.Binary)

            rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            vbDrawContour(rc.mask, rc.contour, 255, -1)

            rc.floodPoint = floodPointData.Get(Of cv.Point)(i, 0)
            rc.maxDist = vbGetMaxDist(rc)
            rc.tier = src(rc.rect).Mean(rc.mask)
            rc.pixels = rc.mask.CountNonZero
            sortedCells.Add(rc.pixels, rc)
        Next

        dst2.SetTo(0)
        redCells.Clear()
        redCells.Add(New rcData)
        For Each ele In sortedCells
            Dim rc = ele.Value
            rc.index = redCells.Count
            redCells.Add(rc)
            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            dst2.Circle(rc.maxDist, task.dotSize, task.highlightColor, -1, task.lineType)
        Next
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
    End Sub
End Class






Public Class Flood_Cell : Inherits VB_Algorithm
    Dim flood As New Flood_Basics
    Dim tiersC As New Contour_DepthTiers
    Dim tiers As New Depth_Tiers
    Dim floodMin As New Flood_Minimal
    Dim redCells As New List(Of rcData)
    Dim cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        gOptions.useFilter.Checked = False
        desc = "Run RedCloud on an individual cell with limited count of desired cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            flood.Run(src)
            dst2 = flood.dst3
            labels = flood.labels
        End If

        dst1 = New cv.Mat(task.rc.mask.Size, cv.MatType.CV_8U, 0)
        If gOptions.DebugCheckBox.Checked Then
            tiers.Run(src)
            tiers.dst2(task.rc.rect).CopyTo(dst1, task.rc.mask)
        Else
            tiersC.Run(src)
            tiersC.dst2(task.rc.rect).CopyTo(dst1, task.rc.mask)
        End If

        floodMin.rc = task.rc
        floodMin.Run(dst1)
        dst3.SetTo(0)
        dst3(task.rc.rect) = vbPalette(floodMin.cellMap * 255 / floodMin.redCells.Count)

        redCells.Clear()
        cellMap.SetTo(0)
        For Each rc In floodMin.redCells
            redCells.Add(rc)
            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            If rc.index <= identifyCount And rc.index > 0 Then
                Dim pt = New cv.Point(rc.maxDist.X + task.rc.rect.X + 20, rc.maxDist.Y + task.rc.rect.Y)
                setTrueText(CStr(CInt(rc.tier)), pt, 3)
            End If
        Next
        labels(3) = $"{floodMin.redCells.Count} were identified within the selected cell."
        If standalone Then identifyCells(flood.redCells)
    End Sub
End Class







Public Class Flood_Cells : Inherits VB_Algorithm
    Dim flood As New Flood_Basics
    Dim tiers As New Contour_DepthTiers
    Dim floodMin As New Flood_Minimal
    Dim redCells As New List(Of rcData)
    Dim cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        gOptions.useFilter.Checked = False
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Run RedCloud on an individual cell with limited count of desired cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            flood.Run(src)
            dst2 = flood.dst3
            labels = flood.labels
        End If

        dst1.SetTo(0)
        tiers.Run(src)

        redCells.Clear()
        cellMap.SetTo(0)
        dst3.SetTo(0)
        For Each cell In flood.redCells
            floodMin.rc = cell
            tiers.dst2(cell.rect).CopyTo(dst1, cell.mask)
            floodMin.Run(dst1)

            For Each rc In floodMin.redCells
                If rc.pixels >= tiers.options.minPixels Then
                    redCells.Add(rc)
                    cellMap(rc.rect).SetTo(rc.index, rc.mask)
                End If
            Next
            If cell.index > 5 Then Exit For
        Next

        dst3 = vbPalette(cellMap * 255 / redCells.Count)

        labels(3) = $"{floodMin.redCells.Count} were identified within the selected cell."
        If standalone Then identifyCells(flood.redCells)
    End Sub
End Class