Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedCloud_Basics : Inherits VB_Algorithm
    Public redCells As New List(Of rcData)
    Public lastCells As New List(Of rcData)
    Public overlappingCells As New List(Of Integer)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)

    Dim minCells As New List(Of segCell)
    Dim combine As New RedCloud_Combine

    Dim lastCellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Dim usedColors As New List(Of cv.Vec3b)
    Public useLastRC As Boolean = False
    Public removeOverlappingCells As Boolean = True
    Public displaySelectedCell As Boolean = True
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Match cells from the previous generation"
    End Sub
    Public Sub showSelectedCell(dst As cv.Mat)
        Dim rc = task.rcSelect

        task.color.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
        task.color(rc.rect).SetTo(cv.Scalar.White, rc.mask)

        task.depthRGB.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
        task.depthRGB(rc.rect).SetTo(cv.Scalar.White, rc.mask)

        dst(rc.rect).SetTo(cv.Scalar.White, rc.mask)
        dst.Circle(rc.maxDist, task.dotSize, cv.Scalar.Black, -1, task.lineType)

        dst.Circle(rc.maxDStable, task.dotSize + 2, cv.Scalar.Black, -1, task.lineType)
        dst.Circle(rc.maxDStable, task.dotSize, cv.Scalar.White, -1, task.lineType)
    End Sub
    Private Function matchPreviousCell(rp As segCell) As rcData
        Dim rc = New rcData, lrc As New rcData
        rc.rect = rp.rect
        rc.motionRect = rc.rect
        rc.mask = rp.mask.Clone
        rc.floodPoint = rp.floodPoint
        rc.maxDist = vbGetMaxDist(rc)

        Dim val = task.pcSplit(2).Get(Of Single)(rc.floodPoint.Y, rc.floodPoint.X)
        If val = 0 Then rc.depthCell = False Else rc.depthCell = True

        rc.maxDStable = rc.maxDist ' assume it has to use the latest.
        rc.indexLast = lastCellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
        If rc.indexLast < lastCells.Count And rc.indexLast <> task.redOther Then
            lrc = lastCells(rc.indexLast)
            rc.motionRect = rc.rect.Union(lrc.rect)
            rc.color = lrc.color

            Dim stableCheck = lastCellMap.Get(Of Byte)(lrc.maxDStable.Y, lrc.maxDStable.X)
            If stableCheck = rc.indexLast Then
                rc.maxDStable = lrc.maxDStable ' keep maxDStable if cell matched to previous
                rc.matchCount = lrc.matchCount + 1
            End If
        End If

        If usedColors.Contains(rc.color) Then
            rc.color = randomCellColor()
            rc.indexLast = 0
            rc.matchCount = 0
            dst3(rc.rect).SetTo(255, rc.mask)
        End If

        usedColors.Add(rc.color)

        'If rc.indexLast <> 0 And useLastRC Then
        '    lrc.index = rc.index
        '    rc = lrc
        '    rc.indexLast = lrc.index
        'Else
        rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
        vbDrawContour(rc.mask, rc.contour, 255, -1)

        rc.depthMask = rc.mask.Clone
        rc.depthMask.SetTo(0, task.noDepthMask(rc.rect))
        rc.depthPixels = rc.depthMask.CountNonZero

        'End If

        Dim minLoc As cv.Point, maxLoc As cv.Point
        If rc.depthPixels Then
            task.pcSplit(0)(rc.rect).MinMaxLoc(rc.minVec.X, rc.maxVec.X, minLoc, maxLoc, rc.depthMask)
            task.pcSplit(1)(rc.rect).MinMaxLoc(rc.minVec.Y, rc.maxVec.Y, minLoc, maxLoc, rc.depthMask)
            task.pcSplit(2)(rc.rect).MinMaxLoc(rc.minVec.Z, rc.maxVec.Z, minLoc, maxLoc, rc.depthMask)

            Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
            cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev, rc.depthMask)

            rc.depthMean = New cv.Point3f(depthMean(0), depthMean(1), depthMean(2))
            rc.depthStdev = New cv.Point3f(depthStdev(0), depthStdev(1), depthStdev(2))
        End If

        rc.pixels = rc.mask.CountNonZero

        Return rc
    End Function
    Public Sub RunVB(src As cv.Mat)
        combine.Run(src)
        minCells = combine.minCells

        If firstPass Then cellMap.SetTo(task.redOther)

        lastCellMap = cellMap.Clone
        lastCells = New List(Of rcData)(redCells)
        usedColors.Clear()
        usedColors.Add(black)

        If dst2.Size <> src.Size Then dst2 = New cv.Mat(src.Size, cv.MatType.CV_8UC3, 0)
        If heartBeat() Then dst3.SetTo(0)

        task.rcMatchMax = 0
        Dim rc As rcData
        Dim minPixels = gOptions.minPixelsSlider.Value
        Dim newCells As New List(Of rcData)
        newCells.Add(New rcData)
        For Each rp In minCells
            rc = matchPreviousCell(rp)
            rc.index = newCells.Count
            cv.Cv2.MeanStdDev(src(rc.rect), rc.colorMean, rc.colorStdev, rc.mask)
            Dim grayMean As cv.Scalar, grayStdev As cv.Scalar
            cv.Cv2.MeanStdDev(task.gray(rc.rect), grayMean, grayStdev, rc.mask)
            rc.grayMean = CInt(grayMean(0))

            If rc.mask.Size = dst2.Size Or rc.pixels < minPixels Then Continue For
            If heartBeat() Then rc.matchCount = 1
            newCells.Add(rc)

            If task.rcMatchMax < rc.matchCount Then task.rcMatchMax = rc.matchCount
            If newCells.Count >= 255 Then Exit For ' we are going to handle only the largest 255 cells - "Other" (zero) for the rest.
        Next

        overlappingCells.Clear()
        cellMap.SetTo(task.redOther)
        If redOptions.UseDepth.Checked Or redOptions.UseDepthAndColor.Checked Then
            For i = 0 To newCells.Count - 1
                rc = newCells(i)
                ' if maxdist is already occupied or the cell is in the maxDepthMask, then toss this cell...
                Dim valMap = cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                Dim maxDepth = task.maxDepthMask.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If valMap <> task.redOther Or maxDepth <> 0 Then
                    overlappingCells.Add(rc.index)
                Else
                    cellMap(rc.rect).SetTo(rc.index, rc.mask)
                End If
            Next
            If firstPass Then overlappingCells.Clear()

            If removeOverlappingCells Then
                For i = overlappingCells.Count - 1 To 0 Step -1
                    newCells.RemoveAt(overlappingCells(i))
                Next
            End If
        End If

        cellMap.SetTo(0)
        dst2.SetTo(0)
        redCells.Clear()
        For i = 0 To newCells.Count - 1
            rc = newCells(i)
            rc.index = redCells.Count
            redCells.Add(rc)
            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            dst2(rc.rect).SetTo(rc.color, rc.mask)
        Next

        Static changedTotal As Integer, unMatchedCells As Integer
        unMatchedCells = 0
        labels(3) = CStr(changedTotal) + " new/moved cells in the last second " +
                    Format(changedTotal / (task.frameCount - task.toggleFrame), fmt1) + " unmatched per frame"
        If heartBeat() Then
            Dim mostlyColor As Integer
            For i = 1 To redCells.Count - 1
                rc = redCells(i)
                If redCells(i).depthPixels / redCells(i).pixels < 0.5 Then mostlyColor += 1
                If rc.indexLast = 0 Then unMatchedCells += 1
            Next
            labels(2) = CStr(redCells.Count) + " cells, unmatched cells = " + CStr(unMatchedCells) + "   " +
                        CStr(mostlyColor) + " cells were mostly color and " + CStr(redCells.Count - mostlyColor) + " had depth."

            changedTotal = 0
        End If
        changedTotal += unMatchedCells

        showSelection(redCells, cellMap)

        dst3.SetTo(0, task.maxDepthMask)
        If displaySelectedCell Then showSelectedCell(dst2)
    End Sub
End Class







Public Class RedCloud_BasicsOld : Inherits VB_Algorithm
    Public redCells As New List(Of rcData)
    Public lastCells As New List(Of rcData)
    Public overlappingCells As New List(Of rcData)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)

    Dim minCells As New List(Of segCell)
    Dim combine As New RedCloud_Combine

    Dim lastCellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Dim usedColors As New List(Of cv.Vec3b)
    Public useLastRC As Boolean = False
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Match cells from the previous generation"
    End Sub
    Private Function matchPreviousCell(rp As segCell) As rcData
        Dim rc = New rcData, lrc As New rcData
        rc.index = rp.index
        rc.rect = rp.rect
        rc.motionRect = rc.rect
        rc.mask = rp.mask.Clone
        rc.floodPoint = rp.floodPoint
        rc.maxDist = vbGetMaxDist(rc)

        Dim val = task.pcSplit(2).Get(Of Single)(rc.floodPoint.Y, rc.floodPoint.X)
        If val = 0 Then rc.depthCell = False

        rc.maxDStable = rc.maxDist ' assume it has to use the latest.
        rc.indexLast = lastCellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
        If rc.indexLast < lastCells.Count And rc.indexLast <> task.redOther Then
            lrc = lastCells(rc.indexLast)
            rc.motionRect = rc.rect.Union(lrc.rect)
            rc.color = lrc.color

            Dim stableCheck = lastCellMap.Get(Of Byte)(lrc.maxDStable.Y, lrc.maxDStable.X)
            If stableCheck = rc.indexLast Then
                rc.maxDStable = lrc.maxDStable ' keep maxDStable if cell matched to previous
                rc.matchCount = lrc.matchCount + 1
            End If
        End If

        If usedColors.Contains(rc.color) Then
            rc.color = randomCellColor()
            rc.indexLast = 0
            rc.matchCount = 0
            dst3(rc.rect).SetTo(255, rc.mask)
        End If

        usedColors.Add(rc.color)

        If rc.indexLast <> 0 And useLastRC Then
            lrc.index = rc.index
            rc = lrc
            rc.indexLast = lrc.index
        Else
            rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            vbDrawContour(rc.mask, rc.contour, 255, -1)
            If rc.depthCell = False Then rc.mask.SetTo(0, task.depthMask(rc.rect))

            Dim tmp = rc.mask.Clone
            tmp.SetTo(0, task.noDepthMask(rc.rect))
            rc.depthPixels = tmp.CountNonZero
        End If

        Dim minLoc As cv.Point, maxLoc As cv.Point
        task.pcSplit(0)(rc.rect).MinMaxLoc(rc.minVec.X, rc.maxVec.X, minLoc, maxLoc, rc.mask)
        task.pcSplit(1)(rc.rect).MinMaxLoc(rc.minVec.Y, rc.maxVec.Y, minLoc, maxLoc, rc.mask)
        task.pcSplit(2)(rc.rect).MinMaxLoc(rc.minVec.Z, rc.maxVec.Z, minLoc, maxLoc, rc.mask)

        Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
        cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev, rc.mask)

        rc.depthMean = New cv.Point3f(depthMean(0), depthMean(1), depthMean(2))
        rc.depthStdev = New cv.Point3f(depthStdev(0), depthStdev(1), depthStdev(2))

        rc.pixels = rc.mask.CountNonZero

        Return rc
    End Function
    Public Sub RunVB(src As cv.Mat)
        dst0 = task.color
        dst1 = task.depthRGB

        combine.Run(src)
        minCells = combine.minCells

        If firstPass Then cellMap.SetTo(task.redOther)

        lastCellMap = cellMap.Clone
        lastCells = New List(Of rcData)(redCells)
        usedColors.Clear()
        usedColors.Add(black)

        If dst2.Size <> src.Size Then dst2 = New cv.Mat(src.Size, cv.MatType.CV_8UC3, 0)
        If heartBeat() Then dst3.SetTo(0)
        cellMap.SetTo(task.redOther)

        redCells.Clear()
        redCells.Add(New rcData)
        task.rcMatchMax = 0
        Dim unMatchedCells As Integer, rc As rcData
        Dim minPixels = gOptions.minPixelsSlider.Value
        For Each rp In minCells
            rp.index = redCells.Count
            rc = matchPreviousCell(rp)

            If rc.mask.Size = dst2.Size Or rc.pixels < minPixels Then Continue For
            If heartBeat() Then rc.matchCount = 1
            redCells.Add(rc)

            If task.rcMatchMax < rc.matchCount Then task.rcMatchMax = rc.matchCount
            If redCells.Count >= 255 Then Exit For ' we are going to handle only the largest 255 cells - "Other" (zero) for the rest.
        Next

        overlappingCells.Clear()
        For i = redCells.Count - 1 To 0 Step -1
            rc = redCells(i)
            Dim valMax = cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X) ' if maxdist is already occupied, then toss this cell...
            If valMax <> task.redOther Then overlappingCells.Add(rc)
            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            If rc.indexLast = 0 Then unMatchedCells += 1
        Next

        dst0 = dst2.Clone

        Static changedTotal As Integer
        changedTotal += unMatchedCells
        labels(3) = CStr(changedTotal) + " new/moved cells in the last second " +
                    Format(changedTotal / (task.frameCount - task.toggleFrame), fmt1) + " unmatched per frame"
        If heartBeat() Then
            Dim colorOnlyCells As Integer
            For Each rc In redCells
                If rc.depthPixels = 0 Then colorOnlyCells += 1
            Next
            labels(2) = CStr(redCells.Count) + " cells, unmatched cells = " + CStr(unMatchedCells) + "   " +
                        CStr(colorOnlyCells) + " cells were only color and " + CStr(redCells.Count - colorOnlyCells) + " had depth."

            changedTotal = 0
        End If

        task.rcSelect = New rcData
        If task.clickPoint = New cv.Point(0, 0) Then
            If redCells.Count > 2 Then
                task.clickPoint = redCells(1).maxDist
                task.rcSelect = redCells(1)
            Else
                Exit Sub
            End If
        Else
            Dim index = cellMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
            task.rcSelect = redCells(index)
        End If

        rc = task.rcSelect

        task.color.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
        task.color(rc.rect).SetTo(cv.Scalar.White, rc.mask)

        task.depthRGB.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
        task.depthRGB(rc.rect).SetTo(cv.Scalar.White, rc.mask)

        dst2(rc.rect).SetTo(cv.Scalar.White, rc.mask)
        dst2.Circle(rc.maxDist, task.dotSize, cv.Scalar.Black, -1, task.lineType)

        dst2.Circle(rc.maxDStable, task.dotSize + 2, cv.Scalar.Black, -1, task.lineType)
        dst2.Circle(rc.maxDStable, task.dotSize, cv.Scalar.White, -1, task.lineType)
    End Sub
End Class







Public Class RedCloud_MatchCell : Inherits VB_Algorithm
    Public rp As New segCell
    Public rc As New rcData
    Public lastCellMap As New cv.Mat
    Public lastCells As New List(Of rcData)
    Public usedColors As New List(Of cv.Vec3b)
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        strOut = "RedCloud_MatchCell takes an segCell cell and builds an rcData cell." + vbCrLf +
                 "When standalone, it just build a fake segCell cell and displays the rcData equivalent."
        desc = "Build a RedCloud cell from the segCell input"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone And heartBeat() Then
            rp.floodPoint = New cv.Point(msRNG.Next(0, dst2.Width / 2), msRNG.Next(0, dst2.Height / 2))
            Dim w = msRNG.Next(1, dst2.Width / 2), h = msRNG.Next(1, dst2.Height / 2)
            rp.rect = New cv.Rect(rp.floodPoint.X, rp.floodPoint.Y, w, h)
            rp.mask = task.depthRGB(rp.rect).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            lastCellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, task.redOther)
            dst2.SetTo(0)
        End If

        rc = New rcData
        rc.index = rp.index
        rc.rect = rp.rect
        rc.motionRect = rc.rect
        rc.mask = rp.mask

        rc.maxDStable = rc.maxDist ' assume it has to use the latest.
        rc.indexLast = lastCellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
        If rc.indexLast < lastCells.Count And rc.indexLast <> task.redOther Then
            Dim lrc = lastCells(rc.indexLast)
            rc.motionRect = rc.rect.Union(lrc.rect)
            rc.color = lrc.color

            Dim stableCheck = lastCellMap.Get(Of Byte)(lrc.maxDStable.Y, lrc.maxDStable.X)
            If stableCheck = rc.indexLast Then rc.maxDStable = lrc.maxDStable ' keep maxDStable if cell matched to previous
        End If

        If usedColors.Contains(rc.color) Then
            rc.color = randomCellColor()
            If standalone Then dst2(rc.rect).SetTo(cv.Scalar.White, rc.mask)
            rc.indexLast = 0
            If heartBeat() Then dst3.SetTo(0)
            dst3(rc.rect).SetTo(255, rc.mask)
        End If

        usedColors.Add(rc.color)

        rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
        vbDrawContour(rc.mask, rc.contour, 255, -1)

        Dim minLoc As cv.Point, maxLoc As cv.Point
        task.pcSplit(0)(rc.rect).MinMaxLoc(rc.minVec.X, rc.maxVec.X, minLoc, maxLoc, rc.mask)
        task.pcSplit(1)(rc.rect).MinMaxLoc(rc.minVec.Y, rc.maxVec.Y, minLoc, maxLoc, rc.mask)
        task.pcSplit(2)(rc.rect).MinMaxLoc(rc.minVec.Z, rc.maxVec.Z, minLoc, maxLoc, rc.mask)

        Dim tmp = New cv.Mat(rc.mask.Size, cv.MatType.CV_8U, 0)
        task.depthMask(rc.rect).CopyTo(tmp, rc.mask)
        If tmp.CountNonZero / rc.pixels > 0.1 Then
            Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
            cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev, rc.mask)

            rc.depthMean = New cv.Point3f(depthMean(0), depthMean(1), depthMean(2))
            rc.depthStdev = New cv.Point3f(depthStdev(0), depthStdev(1), depthStdev(2))
        End If

        If standalone Then setTrueText(strOut, 3)
    End Sub
End Class








Public Class RedCloud_CoreTest : Inherits VB_Algorithm
    Dim prep As New RedCloud_Core
    Public rMin As New RedMin_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        gOptions.HistBinSlider.Value = 20
        desc = "Segment the image based on both the reduced point cloud and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)

        prep.Run(Nothing)
        prep.dst2.ConvertScaleAbs().CopyTo(reduction.dst2, task.depthMask)

        rMin.Run(reduction.dst2)

        dst2 = rMin.dst2
        dst3 = rMin.dst3
        labels = rMin.labels
    End Sub
End Class








Module Red_Module

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor_Map_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Neighbor_Map_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor_Map_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor_NabList(cPtr As IntPtr) As IntPtr
    End Function






    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Rects(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_FloodPoints(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Sizes(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodCell_Run(
                cPtr As IntPtr, dataPtr As IntPtr, maskPtr As IntPtr, rows As Integer, cols As Integer,
                type As Integer, sizeThreshold As Single, maxClassCount As Integer, diff As Integer) As IntPtr
    End Function







    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function PlotOpenCV_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function PlotOpenCV_Close(cPtr As IntPtr) As IntPtr
    End Function

    Public backColor = cv.Scalar.DarkGray

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function PlotOpenCV_Run(cPtr As IntPtr, inX As IntPtr, inY As IntPtr, inLen As Integer,
                                     rows As Integer, cols As Integer) As IntPtr
    End Function




    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RedCloud_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Run(cPtr As IntPtr, dataPtr As IntPtr, maskPtr As IntPtr, rows As Integer, cols As Integer,
                                 sizeThreshold As Single, maxClassCount As Integer) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_FloodPointList(cPtr As IntPtr) As IntPtr
    End Function





    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_FindCells_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub RedCloud_FindCells_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_FindCells_TotalCount(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_FindCells_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function






    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedMin_FindPixels_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub RedMin_FindPixels_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedMin_FindPixels_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedMin_FindPixels_Sizes(cPtr As IntPtr) As IntPtr
    End Function





    Public Function shapeCorrelation(points As List(Of cv.Point)) As Single
        Dim pts As New cv.Mat(points.Count, 1, cv.MatType.CV_32SC2, points.ToArray)
        Dim pts32f As New cv.Mat
        pts.ConvertTo(pts32f, cv.MatType.CV_32FC2)
        Dim split = pts32f.Split()
        Dim correlationMat As New cv.Mat
        cv.Cv2.MatchTemplate(split(0), split(1), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
        Return correlationMat.Get(Of Single)(0, 0)
    End Function
End Module











Public Class RedCloud_Motion : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Dim diff As New Diff_Basics
    Public motionList As New List(Of Integer)
    Public Sub New()
        gOptions.PixelDiffThreshold.Value = 9
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use absDiff to build a mask of cells that changed."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        diff.Run(src)

        redC.Run(src)
        dst2 = redC.dst2

        Dim minPixels = gOptions.minPixelsSlider.Value
        Dim rect As New cv.Rect
        For Each rc In redC.redCells
            Dim tmp As cv.Mat = rc.mask And diff.dst3(rc.rect)
            If tmp.CountNonZero > minPixels And rc.index > 0 Then
                If rect.Width = 0 Then rect = rc.motionRect Else rect = rect.Union(rc.motionRect)
            End If
        Next

        dst3.SetTo(0)
        If rect.Width > 0 Then dst3(rect).SetTo(255)
    End Sub
End Class









Public Class RedCloud_Hulls : Inherits VB_Algorithm
    Dim convex As New Convex_RedCloudDefects
    Public redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "Cells where convexity defects failed", "", "Improved contour results using OpenCV's ConvexityDefects"}
        desc = "Add hulls and improved contours using ConvexityDefects to each RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        dst3.SetTo(0)
        Dim defectCount As Integer
        redC.cellMap.SetTo(task.redOther)
        Dim redCells As New List(Of rcData)
        For Each rc In redC.redCells
            If rc.contour.Count >= 5 Then
                rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
                Dim hullIndices = cv.Cv2.ConvexHullIndices(rc.hull.ToArray, False)
                Try
                    Dim defects = cv.Cv2.ConvexityDefects(rc.contour, hullIndices)
                    rc.contour = convex.betterContour(rc.contour, defects)
                Catch ex As Exception
                    defectCount += 1
                End Try
                vbDrawContour(dst3(rc.rect), rc.hull, rc.color, -1)
                vbDrawContour(redC.cellMap(rc.rect), rc.hull, rc.index, -1)
            End If
            redCells.Add(rc)
        Next
        redC.redCells = New List(Of rcData)(redCells)
        labels(2) = CStr(redC.redCells.Count) + " hulls identified below.  " + CStr(defectCount) + " hulls failed to build the defect list."
    End Sub
End Class









Public Class RedCloud_FindCells : Inherits VB_Algorithm
    Public cellList As New List(Of Integer)
    Dim redC As New RedCloud_Basics
    Public Sub New()
        gOptions.PixelDiffThreshold.Value = 25
        cPtr = RedCloud_FindCells_Open()
        desc = "Find all the RedCloud cells touched by the mask created by the Motion_History rectangle"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cellList = New List(Of Integer)

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim rc = task.rcSelect

        Dim cells As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim r = New cv.Rect(rc.rect.X - 1, rc.rect.Y - 1, rc.rect.Width + 2, rc.rect.Height + 2)
        r = validateRect(r)
        redC.cellMap(r).CopyTo(cells(r))

        Dim cppData(cells.Total - 1) As Byte
        Marshal.Copy(cells.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = RedCloud_FindCells_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), cells.Rows, cells.Cols)
        handleSrc.Free()

        Dim count = RedCloud_FindCells_TotalCount(cPtr)
        If count = 0 Then Exit Sub

        Dim cellsFound(count - 1) As Integer
        Marshal.Copy(imagePtr, cellsFound, 0, cellsFound.Length)

        cellList = cellsFound.ToList
        dst3.SetTo(0)
        dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst0 = dst0.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        For Each index In cellList
            Dim rcX As rcData = redC.redCells(index)
            vbDrawContour(dst3(rcX.rect), rcX.contour, rcX.color, -1)
            dst3(rcX.rect).SetTo(cv.Scalar.White, dst0(rcX.rect))
        Next
        labels(3) = CStr(count) + " cells were found using the motion mask"
    End Sub
    Public Sub Close()
        RedCloud_FindCells_Close(cPtr)
    End Sub
End Class









'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedCloud_Planes : Inherits VB_Algorithm
    Public planes As New RedCloud_PlaneColor
    Public Sub New()
        desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        planes.Run(src)
        dst2 = planes.dst2
        dst3 = planes.dst3
        labels = planes.labels
    End Sub
End Class








Public Class RedCloud_Equations : Inherits VB_Algorithm
    Dim eq As New Plane_Equation
    Public redCells As New List(Of rcData)
    Public Sub New()
        labels(3) = "The estimated plane equations for the largest 20 RedCloud cells."
        desc = "Show the estimated plane equations for all the cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            redCells = New List(Of rcData)(redC.redCells)
        End If

        Dim newCells As New List(Of rcData)
        For Each rc As rcData In redCells
            If rc.contour.Count > 4 Then
                eq.rc = rc
                eq.Run(Nothing)
                newCells.Add(eq.rc)
            End If
        Next

        redCells = New List(Of rcData)(newCells)

        If heartBeat() Then
            Dim index As Integer
            strOut = ""
            For Each rc In redCells
                If rc.contour.Count > 4 Then
                    Dim justEquation = Format(rc.eq(0), fmt3) + "*X + " + Format(rc.eq(1), fmt3) + "*Y + "
                    justEquation += Format(rc.eq(2), fmt3) + "*Z + " + Format(rc.eq(3), fmt3) + vbCrLf
                    strOut += justEquation
                    index += 1
                    If index >= 20 Then Exit For
                End If
            Next
        End If

        setTrueText(strOut, 3)
    End Sub
End Class








Public Class RedCloud_CellsAtDepth : Inherits VB_Algorithm
    Dim plot As New Plot_Histogram
    Dim kalman As New Kalman_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        plot.removeZeroEntry = False
        labels(3) = "Histogram of depth weighted by the size of the cell."
        desc = "Create a histogram of depth using RedCloud cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim histBins = gOptions.HistBinSlider.Value
        Dim slotList(histBins) As List(Of Integer)
        For i = 0 To slotList.Count - 1
            slotList(i) = New List(Of Integer)
        Next
        Dim hist(histBins - 1) As Single
        For Each rc In redC.redCells
            Dim slot As Integer
            If rc.depthMean.Z > task.maxZmeters Then rc.depthMean.Z = task.maxZmeters
            slot = CInt((rc.depthMean.Z / task.maxZmeters) * histBins)
            If slot >= hist.Length Then slot = hist.Length - 1
            slotList(slot).Add(rc.index)
            hist(slot) += rc.pixels
        Next

        kalman.kInput = hist
        kalman.Run(src)

        Dim histMat = New cv.Mat(histBins, 1, cv.MatType.CV_32F, kalman.kOutput)
        plot.Run(histMat)
        dst3 = plot.dst2

        Dim barWidth = dst3.Width / histBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / barWidth)
        dst3.Rectangle(New cv.Rect(CInt(histIndex * barWidth), 0, barWidth, dst3.Height), cv.Scalar.Yellow, task.lineWidth)
        For i = 0 To slotList(histIndex).Count - 1
            Dim rc = redC.redCells(slotList(histIndex)(i))
            vbDrawContour(dst2(rc.rect), rc.contour, cv.Scalar.Yellow)
            vbDrawContour(task.color(rc.rect), rc.contour, cv.Scalar.Yellow)
        Next
    End Sub
End Class










Public Class RedCloud_Features : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("MaxDist Location")
            radio.addRadio("Depth mean")
            radio.addRadio("Correlation X to Z")
            radio.addRadio("Correlation Y to Z")
            radio.check(3).Checked = True
        End If

        desc = "Display And validate the keyPoints for each RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static frm = findfrm(traceName + " Radio Buttons")
        Dim selection As Integer
        Dim labelName As String
        For selection = 0 To frm.check.Count - 1
            If frm.check(selection).Checked Then
                labelName = frm.check(selection).text
                Exit For
            End If
        Next

        redC.Run(src)
        dst2 = redC.dst2

        Dim rc = task.rcSelect

        dst0 = task.color
        Dim correlationMat As New cv.Mat, correlationXtoZ As Single, correlationYtoZ As Single
        dst3.SetTo(0)
        Select Case selection
            Case 0
                Dim pt = rc.maxDist
                labels(3) += "maxDist Is at (" + CStr(pt.X) + ", " + CStr(pt.Y) + ")"
                dst2.Circle(pt, task.dotSize, task.highlightColor, -1, cv.LineTypes.AntiAlias)
            Case 1
                dst3(rc.rect).SetTo(vbNearFar((rc.depthMean.Z) / task.maxZmeters), rc.mask)
                labels(3) = "rc.depthMean.Z Is highlighted in dst2"
                labels(3) = "Mean depth for the cell Is " + Format(rc.depthMean.Z, fmt3)
            Case 2
                cv.Cv2.MatchTemplate(task.pcSplit(0)(rc.rect), task.pcSplit(2)(rc.rect), correlationMat, cv.TemplateMatchModes.CCoeffNormed, rc.mask)
                correlationXtoZ = correlationMat.Get(Of Single)(0, 0)
                labels(3) = "High correlation X to Z Is yellow, low correlation X to Z Is blue"
            Case 3
                cv.Cv2.MatchTemplate(task.pcSplit(1)(rc.rect), task.pcSplit(2)(rc.rect), correlationMat, cv.TemplateMatchModes.CCoeffNormed, rc.mask)
                correlationYtoZ = correlationMat.Get(Of Single)(0, 0)
                labels(3) = "High correlation Y to Z Is yellow, low correlation Y to Z Is blue"
        End Select
        If selection = 3 Or selection = 4 Then
            dst3(rc.rect).SetTo(vbNearFar(If(selection = 3, correlationXtoZ, correlationYtoZ) + 1), rc.mask)
            setTrueText("(" + Format(correlationXtoZ, fmt3) + ", " + Format(correlationYtoZ, fmt3) + ")", New cv.Point(rc.rect.X, rc.rect.Y), 3)
        End If
        vbDrawContour(dst0(rc.rect), rc.contour, cv.Scalar.Yellow)
        setTrueText(labels(3), 3)
        labels(2) = "Highlighted feature = " + labelName
    End Sub
End Class







Public Class RedCloud_ShapeCorrelation : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "A shape correlation is between each x and y in list of contours points.  It allows classification based on angle and shape."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim rc = task.rcSelect
        If rc.contour.Count > 0 Then
            Dim shape = shapeCorrelation(rc.contour)
            strOut = "Contour correlation for selected cell contour X to Y = " + Format(shape, fmt3) + vbCrLf + vbCrLf +
                     "Select different cells and notice the pattern for the correlation of the contour.X to contour.Y values:" + vbCrLf +
                     "(The contour correlation - contour.x to contour.y - Is computed above.)" + vbCrLf + vbCrLf +
                     "If shape leans left, correlation Is positive And proportional to the lean." + vbCrLf +
                     "If shape leans right, correlation Is negative And proportional to the lean. " + vbCrLf +
                     "If shape Is symmetric (i.e. rectangle Or circle), correlation Is near zero." + vbCrLf +
                     "(Remember that Y increases from the top of the image to the bottom.)"
        End If

        setTrueText(strOut, 3)
    End Sub
End Class





Public Class RedCloud_FPS : Inherits VB_Algorithm
    Dim fps As New Grid_FPS
    Dim redC As New RedCloud_Basics
    Public Sub New()
        gOptions.displayDst0.Checked = True
        gOptions.displayDst1.Checked = True
        desc = "Display RedCloud output at a fixed frame rate"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fps.Run(Nothing)

        If fps.heartBeat Then
            redC.Run(src)
            dst0 = task.color.Clone
            dst1 = task.depthRGB.Clone
            dst2 = redC.dst2.Clone
        End If
        labels(2) = redC.labels(2) + " " + fps.strOut
    End Sub
End Class








'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedCloud_PlaneColor : Inherits VB_Algorithm
    Public options As New Options_Plane
    Public redC As New RedCloud_Basics
    Dim planeMask As New RedCloud_PlaneFromMask
    Dim planeContour As New RedCloud_PlaneFromContour
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        Dim fitPoints As New List(Of cv.Point3f)
        For Each rc In redC.redCells
            If options.useMaskPoints Then
                planeMask.Run(Nothing)
            ElseIf options.useContourPoints Then
                planeContour.Run(Nothing)
            ElseIf options.use3Points Then
                If rc.contour.Count > 3 Then rc.eq = build3PointEquation(rc)
            End If
            dst3(rc.rect).SetTo(New cv.Scalar(Math.Abs(255 * rc.eq(0)), Math.Abs(255 * rc.eq(1)), Math.Abs(255 * rc.eq(2))), rc.mask)
        Next
    End Sub
End Class






'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedCloud_PlaneFromContour : Inherits VB_Algorithm
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation each cell's contour"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        Dim rc = task.rcSelect
        Dim fitPoints As New List(Of cv.Point3f)
        For Each pt In rc.contour
            If pt.X >= rc.rect.Width Or pt.Y >= rc.rect.Height Then Continue For
            If rc.mask.Get(Of Byte)(pt.Y, pt.X) = 0 Then Continue For
            fitPoints.Add(task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X))
        Next
        rc.eq = fitDepthPlane(fitPoints)
        If standalone Then
            dst3.SetTo(0)
            dst3(rc.rect).SetTo(New cv.Scalar(Math.Abs(255 * rc.eq(0)), Math.Abs(255 * rc.eq(1)), Math.Abs(255 * rc.eq(2))), rc.mask)
        End If
    End Sub
End Class







'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedCloud_PlaneFromMask : Inherits VB_Algorithm
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation from the pointcloud samples in a RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        Dim rc = task.rcSelect
        Dim fitPoints As New List(Of cv.Point3f)
        For y = 0 To rc.rect.Height - 1
            For x = 0 To rc.rect.Width - 1
                If rc.mask.Get(Of Byte)(y, x) Then fitPoints.Add(task.pointCloud(rc.rect).Get(Of cv.Point3f)(y, x))
            Next
        Next
        rc.eq = fitDepthPlane(fitPoints)
        If standalone Then
            dst3.SetTo(0)
            dst3(rc.rect).SetTo(New cv.Scalar(Math.Abs(255 * rc.eq(0)), Math.Abs(255 * rc.eq(1)), Math.Abs(255 * rc.eq(2))), rc.mask)
        End If
    End Sub
End Class









Public Class RedCloud_BProject3D : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim hcloud As New Hist3Dcloud_Basics
    Public Sub New()
        desc = "Run RedCloud_Basics on the output of the RGB 3D backprojection"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hcloud.Run(src)
        dst3 = hcloud.dst2

        dst3.ConvertTo(dst0, cv.MatType.CV_8U)
        redC.Run(dst0)
        dst2 = redC.dst2
    End Sub
End Class









Public Class RedCloud_YZ : Inherits VB_Algorithm
    Dim stats As New Cell_Basics
    Public Sub New()
        stats.runRedCloud = True
        desc = "Build horizontal RedCloud cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redOptions.YZReduction.Checked = True
        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = stats.dst2
        setTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_XZ : Inherits VB_Algorithm
    Dim stats As New Cell_Basics
    Public Sub New()
        stats.runRedCloud = True
        desc = "Build vertical RedCloud cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redOptions.XZReduction.Checked = True
        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = stats.dst2
        setTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_World : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim world As New Depth_World
    Public Sub New()
        labels(3) = "Generated pointcloud"
        redOptions.RedCloud_Core.Checked = True
        desc = "Display the output of a generated pointcloud as RedCloud cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        world.Run(src)
        task.pointCloud = world.dst2

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
        If firstPass Then findSlider("RedCloud_Core Reduction").Value = 1000
    End Sub
End Class







Public Class RedCloud_KMeans : Inherits VB_Algorithm
    Dim km As New KMeans_MultiChannel
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "KMeans_MultiChannel output", "RedCloud_Basics output"}
        desc = "Use RedCloud to identify the regions created by kMeans"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(src)
        dst3 = km.dst2

        redC.Run(km.dst3)
        dst2 = redC.dst2
    End Sub
End Class










Public Class RedCloud_Diff : Inherits VB_Algorithm
    Dim diff As New Diff_RGBAccum
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "Diff output, RedCloud input", "RedCloud output"}
        desc = "Isolate blobs in the diff output with RedCloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("Wave at the camera to see the segmentation of the motion.", 3)
        diff.Run(src)
        dst3 = diff.dst2

        redC.Run(dst3)
        dst2.SetTo(0)
        redC.dst2.CopyTo(dst2, dst3)

        labels(3) = CStr(redC.redCells.Count) + " objects identified in the diff output"
    End Sub
End Class








Public Class RedCloud_ProjectCell : Inherits VB_Algorithm
    Dim topView As New Histogram_ShapeTop
    Dim sideView As New Histogram_ShapeSide
    Dim mats As New Mat_4Click
    Dim redC As New RedCloud_Basics
    Public Sub New()
        gOptions.displayDst1.Checked = True
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Top: XZ values and mask, Bottom: ZY values and mask"
        desc = "Visualize the top and side projection of a RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        labels(2) = redC.labels(2)

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
        dst1 = mats.dst2
        dst3 = mats.dst3

        Dim padX = dst2.Width / 15
        Dim padY = dst2.Height / 20
        strOut = "Top" + vbTab + "Top Mask" + vbCrLf + vbCrLf + "Side" + vbTab + "Side Mask"
        setTrueText(strOut, New cv.Point(dst2.Width / 2 - padX, dst2.Height / 2 - padY), 1)
        setTrueText("Select a RedCloud cell above to project it into the top and side views at left.", 3)
    End Sub
End Class









Public Class RedCloud_NoDepth : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Minimum pixels %", 0, 100, 25)

        labels = {"", "", "", "Cells with depth percentage that is less than the threshold specified."}
        desc = "Find RedColor cells only for areas with insufficient depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static minSlider = findSlider("Minimum pixels %")
        Dim minPixelPercent = minSlider.value

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim redCells As New List(Of rcData)
        For Each rc In redC.redCells
            rc.mask.SetTo(0, task.depthMask(rc.rect))
            If rc.mask.CountNonZero / rc.pixels > minPixelPercent Then
                rc.mask.SetTo(0)
            Else
                rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxTC89L1)
                If rc.contour.Count > 0 Then vbDrawContour(rc.mask, rc.contour, 255, -1)
            End If
            redCells.Add(rc)
        Next

        redC.redCells = New List(Of rcData)(redCells)

        dst3.SetTo(0)
        For Each rc In redCells
            vbDrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
        Next
    End Sub
End Class






Public Class RedCloud_StructuredH : Inherits VB_Algorithm
    Dim motion As New RedCloud_Motion
    Dim transform As New Structured_TransformH
    Dim topView As New Histogram2D_Top
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Display the RedCloud cells found with a horizontal slice through the cellMap."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim sliceMask = transform.createSliceMaskH()
        dst0 = src

        motion.Run(sliceMask.Clone)

        If heartBeat() Then dst1.SetTo(0)
        dst1.SetTo(cv.Scalar.White, sliceMask)
        labels = motion.labels

        dst2.SetTo(0)
        For Each index In motion.motionList
            Dim rc As rcData = motion.redC.redCells(index)
            vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
        Next

        Dim pc As New cv.Mat(task.pointCloud.Size, cv.MatType.CV_32FC3, 0)
        task.pointCloud.CopyTo(pc, dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        topView.Run(pc)
        dst3 = topView.dst2

        dst2.SetTo(cv.Scalar.White, sliceMask)
        dst0.SetTo(cv.Scalar.White, sliceMask)
    End Sub
End Class






Public Class RedCloud_StructuredV : Inherits VB_Algorithm
    Dim motion As New RedCloud_Motion
    Dim transform As New Structured_TransformV
    Dim sideView As New Histogram2D_Side
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Display the RedCloud cells found with a vertical slice through the cellMap."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim sliceMask = transform.createSliceMaskV()
        dst0 = src

        motion.Run(sliceMask.Clone)

        If heartBeat() Then dst1.SetTo(0)
        dst1.SetTo(cv.Scalar.White, sliceMask)
        labels = motion.labels
        setTrueText("Move mouse in image to see impact.", 3)

        dst2.SetTo(0)
        For Each index In motion.motionList
            Dim rc As rcData = motion.redC.redCells(index)
            vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
        Next

        Dim pc As New cv.Mat(task.pointCloud.Size, cv.MatType.CV_32FC3, 0)
        task.pointCloud.CopyTo(pc, dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        sideView.Run(pc)
        dst3 = sideView.dst2

        dst2.SetTo(cv.Scalar.White, sliceMask)
        dst0.SetTo(cv.Scalar.White, sliceMask)
    End Sub
End Class










Public Class RedCloud_LikelyFlatSurfaces : Inherits VB_Algorithm
    Dim verts As New Plane_Basics
    Dim redC As New RedCloud_Basics
    Public vCells As New List(Of rcData)
    Public hCells As New List(Of rcData)
    Public Sub New()
        labels(1) = "RedCloud output"
        desc = "Use the mask for vertical surfaces to identify RedCloud cells that appear to be flat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        verts.Run(src)

        redC.Run(src)
        dst2.SetTo(0)
        dst3.SetTo(0)

        vCells.Clear()
        hCells.Clear()
        For Each rc In redC.redCells
            If rc.depthMean.Z >= task.maxZmeters Then Continue For
            Dim tmp As cv.Mat = verts.dst2(rc.rect) And rc.mask
            If tmp.CountNonZero / rc.pixels > 0.5 Then
                vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
                vCells.Add(rc)
            End If
            tmp = verts.dst3(rc.rect) And rc.mask
            Dim count = tmp.CountNonZero
            If count / rc.pixels > 0.5 Then
                vbDrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
                hCells.Add(rc)
            End If
        Next

        Dim rcX = task.rcSelect
        setTrueText("mean depth = " + Format(rcX.depthMean.Z, "0.0"), 3)
        labels(2) = redC.labels(2)
    End Sub
End Class








Public Class RedCloud_PlaneEq3D : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim eq As New Plane_Equation
    Public Sub New()
        desc = "If a RedColor cell contains depth then build a plane equation"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

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











Public Class RedCloud_DelaunayGuidedFeatures : Inherits VB_Algorithm
    Dim features As New Feature_PointsDelaunay
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "Format CV_8U of Delaunay data", "RedCloud output", "RedCloud Output of GoodFeature points"}
        desc = "Track the GoodFeatures points using RedCloud."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        features.Run(src)
        dst1 = features.dst3

        redC.Run(dst1)
        dst2 = redC.dst2

        Static goodList As New List(Of List(Of cv.Point2f))
        If heartBeat() Then goodList.Clear()

        Dim nextGood As New List(Of cv.Point2f)(features.good.corners)
        goodList.Add(nextGood)

        If goodList.Count >= task.historyCount Then goodList.RemoveAt(0)

        dst3.SetTo(0)
        For Each ptList In goodList
            For Each pt In ptList
                Dim c = dst2.Get(Of cv.Vec3b)(pt.Y, pt.X)
                dst3.Circle(pt, task.dotSize, c, -1, task.lineType)
            Next
        Next
    End Sub
End Class








Public Class RedCloud_Other : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Highlight the unidentified pixels in the RedCloud output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        dst3 = redC.cellMap.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        If heartBeat() Then labels(2) = redC.labels(2)
        If heartBeat() Then labels(3) = CStr(dst3.CountNonZero) + " unclassified pixels - identified with task.redOther value."
    End Sub
End Class








Public Class RedCloud_ColorAndCloud : Inherits VB_Algorithm
    Dim guided As New GuidedBP_Depth
    Public rMin As New RedMin_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Segment the image based on both the reduced point cloud and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        guided.Run(src)

        reduction.Run(src)
        Dim combined = reduction.dst2.Clone
        guided.dst2.CopyTo(combined, task.depthMask)
        rMin.Run(combined)

        dst2 = rMin.dst2
        dst3 = rMin.dst3
        labels = rMin.labels

        If heartBeat() Then labels(2) = CStr(rMin.minCells.Count) + " regions identified"
    End Sub
End Class







Public Class RedCloud_FeatureLess : Inherits VB_Algorithm
    Dim fLess As New FeatureLess_RedCloud
    Public Sub New()
        desc = "This is a duplicate of FeatureLess_RedCloud to make it easier to find."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fLess.Run(src)
        dst2 = fLess.dst2
        labels(2) = fLess.labels(2)
    End Sub
End Class







Public Class RedCloud_JoinCells : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim fLess As New FeatureLess_RedCloud
    Public Sub New()
        gOptions.HistBinSlider.Value = 20
        labels = {"", "FeatureLess_RedCloud output.", "RedCloud_Basics output", "RedCloud_Basics cells joined by using the color from the FeatureLess_RedCloud cellMap"}
        desc = "Run RedCloud_Basics with depth and use FeatureLess_RedCloud to join cells that are in the same featureless regions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        fLess.Run(src)
        dst1 = fLess.dst2

        dst3.SetTo(0)
        For Each rc In redC.redCells
            Dim color = fLess.dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            dst3(rc.rect).SetTo(color, rc.mask)
        Next
    End Sub
End Class










Public Class RedCloud_UnstableCells : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing"}
        desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        If heartBeat() Or task.frameCount = 2 Then
            dst1 = dst2.Clone
            dst3.SetTo(0)
        End If

        Dim currList As New List(Of cv.Point)
        Static prevList As New List(Of cv.Point)

        For Each rc In redC.redCells
            If prevList.Contains(rc.maxDStable) = False Then
                vbDrawContour(dst1(rc.rect), rc.contour, cv.Scalar.White, -1)
                vbDrawContour(dst1(rc.rect), rc.contour, cv.Scalar.Black)
                vbDrawContour(dst3(rc.rect), rc.contour, cv.Scalar.White, -1)
            End If
            currList.Add(rc.maxDStable)
        Next

        prevList = New List(Of cv.Point)(currList)
    End Sub
End Class








Public Class RedCloud_UnstableHulls : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing"}
        desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        If heartBeat() Or task.frameCount = 2 Then
            dst1 = dst2.Clone
            dst3.SetTo(0)
        End If

        Dim currList As New List(Of cv.Point)
        Static prevList As New List(Of cv.Point)

        For Each rc In redC.redCells
            rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
            If prevList.Contains(rc.maxDStable) = False Then
                vbDrawContour(dst1(rc.rect), rc.hull, cv.Scalar.White, -1)
                vbDrawContour(dst1(rc.rect), rc.hull, cv.Scalar.Black)
                vbDrawContour(dst3(rc.rect), rc.hull, cv.Scalar.White, -1)
            End If
            currList.Add(rc.maxDStable)
        Next

        prevList = New List(Of cv.Point)(currList)
    End Sub
End Class










Public Class RedCloud_CellChanges : Inherits VB_Algorithm
    Dim redC As Object
    Public Sub New()
        If standalone Then redC = New RedCloud_Basics
        desc = "Count the cells that have changed in a RedCloud generation"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        Static dst2Last = dst2.Clone
        dst3 = (dst2 - dst2Last).tomat

        Dim changedPixels = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero
        Dim changedCells As Integer
        For Each rc As rcData In redC.redCells
            If rc.indexLast = 0 Then changedCells += 1
        Next

        dst2Last = dst2.Clone
        If heartBeat() Then
            labels(2) = "Changed cells = " + Format(changedCells, "000") + " cells or " + Format(changedCells / redC.redCells.Count, "0%")
            labels(3) = "Changed pixel total = " + Format(changedPixels / 1000, "0.0") + "k or " + Format(changedPixels / dst2.Total, "0%")
        End If
    End Sub
End Class








Public Class RedCloud_NearestStableCell : Inherits VB_Algorithm
    Public knn As New KNN_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Line connects current maxDStable point to nearest neighbor using KNN."
        desc = "Find the nearest stable cell and connect them with a line."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        If redC.redCells.Count = 1 Then Exit Sub
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        knn.queries.Clear()
        Dim redCells As New List(Of rcData)
        For Each rc In redC.redCells
            If rc.matchCount = task.rcMatchMax Then
                knn.queries.Add(New cv.Point2f(rc.maxDStable.X, rc.maxDStable.Y))
                redCells.Add(rc)
            End If
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)

        knn.Run(Nothing)
        If knn.queries.Count < 3 Then Exit Sub

        dst3 = dst2.Clone
        For i = 0 To knn.result.GetUpperBound(0)
            Dim rc1 = redCells(knn.result(i, 0))
            Dim rc2 = redCells(knn.result(i, 1))
            dst3.Circle(rc1.maxDStable, task.dotSize, white, -1, task.lineType)
            dst3.Circle(rc2.maxDStable, task.dotSize, white, -1, task.lineType)
            dst3.Line(rc1.maxDStable, rc2.maxDStable, white, task.lineWidth, task.lineType)
        Next
    End Sub
End Class








Public Class RedCloud_Core : Inherits VB_Algorithm
    Public classCount As Integer
    Public givenClassCount As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("RedCloud_Core Reduction", 1, 2500, 250)
        If standalone Then redOptions.RedCloud_Core.Checked = True
        desc = "Reduction transform for the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static reductionSlider = findSlider("RedCloud_Core Reduction")
        Dim reduceAmt = reductionSlider.value
        task.pointCloud.ConvertTo(dst0, cv.MatType.CV_32S, 1000 / reduceAmt)

        Dim split = dst0.Split()

        Select Case redOptions.PCReduction
            Case "X Reduction"
                dst0 = (split(0) * reduceAmt).toMat
            Case "Y Reduction"
                dst0 = (split(1) * reduceAmt).toMat
            Case "Z Reduction"
                dst0 = (split(2) * reduceAmt).toMat
            Case "XY Reduction"
                dst0 = (split(0) * reduceAmt + split(1) * reduceAmt).toMat
            Case "XZ Reduction"
                dst0 = (split(0) * reduceAmt + split(2) * reduceAmt).toMat
            Case "YZ Reduction"
                dst0 = (split(1) * reduceAmt + split(2) * reduceAmt).toMat
            Case "XYZ Reduction"
                dst0 = (split(0) * reduceAmt + split(1) * reduceAmt + split(2) * reduceAmt).toMat
        End Select

        Dim mm = vbMinMax(dst0)
        dst2 = (dst0 - mm.minVal)

        dst2.SetTo(mm.maxVal - mm.minVal, task.maxDepthMask)
        mm = vbMinMax(dst2)
        classCount = 255 - givenClassCount - 1
        dst2 *= classCount / mm.maxVal
        dst2 += givenClassCount + 1
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)

        labels(2) = "Reduced Pointcloud - reduction factor = " + CStr(reduceAmt) + " produced " + CStr(classCount) + " regions"
    End Sub
End Class








Public Class RedCloud_FloodPoints : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim stats As New Cell_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Verify that floodpoints correctly determine if depth is present."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst1 = task.depthRGB
        For Each rc In redC.redCells
            dst1.Circle(rc.floodPoint, task.dotSize, cv.Scalar.White, -1, task.lineType)
            dst2.Circle(rc.floodPoint, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next
        stats.Run(src)
        setTrueText(stats.strOut, 3)
    End Sub
End Class









Public Class RedCloud_Combine2Pass : Inherits VB_Algorithm
    Dim redC1 As New RedCloud_Basics
    Dim redC2 As New RedCloud_Basics
    Dim mats As New Mat_4Click
    Public Sub New()
        desc = "Run RedCloud_Basics and then combine the unstable pixels into the input for RedCloud."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC1.Run(src)
        dst0 = redC1.dst0
        dst1 = redC1.dst1
        mats.mat(0) = redC1.dst2.Clone
        mats.mat(1) = redC1.dst3.Clone

        Dim tmp = redC1.cellMap.Clone
        tmp.SetTo(1, redC1.dst3)
        redC2.Run(tmp)
        labels(2) = redC2.labels(2)
        mats.mat(2) = redC2.dst2.Clone
        mats.mat(3) = redC2.dst3.Clone
        mats.Run(Nothing)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class








Public Class RedCloud_CellStats : Inherits VB_Algorithm
    Dim cells As New Cell_Basics
    Public Sub New()
        cells.runRedCloud = True
        desc = "Display the stats for the requested cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cells.Run(src)
        dst2 = cells.dst2
        labels(2) = cells.labels(2)

        setTrueText(cells.strOut, 3)
    End Sub
End Class







Public Class RedCloud_Combine2Runs : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public colorOnly As New RedColor_Cells
    Public Sub New()
        redC.displaySelectedCell = False
        desc = "Run RedColor_Cells and RedCloud_OnlyDepth and then combine."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redOptions.UseDepth.Checked = True
        redC.Run(src)
        dst2 = redC.dst2.Clone
        labels(2) = redC.labels(2)

        redOptions.UseColor.Checked = True
        colorOnly.Run(src)
        dst3 = colorOnly.dst2.Clone
        labels(3) = colorOnly.labels(2)

        Dim cellmap = colorOnly.cellmap.Clone
        redC.cellMap.CopyTo(cellmap, task.depthMask)

        Dim val = task.depthMask.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
        Dim index = cellmap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
        If val Then
            task.rcSelect = redC.redCells(index)
        Else
            task.rcSelect = colorOnly.redCells(index)
        End If
        redC.showSelectedCell(dst2)
    End Sub
End Class






Public Class RedCloud_MostlyColor : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Cells that are mostly color - < 50% depth."
        desc = "Create RedCloud output using only color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        For Each rc In redC.redCells
            If rc.depthCell = False Then dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next
    End Sub
End Class








Public Class RedCloud_OutlineColor : Inherits VB_Algorithm
    Dim outline As New Depth_Outline
    Dim redC As New RedCloud_Basics
    Dim color As New Color_Basics
    Public Sub New()
        labels(3) = "Color input to RedCloud_Basics with depth boundary blocking color connections."
        desc = "Use the depth outline as input to RedCloud_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        outline.Run(src)

        color.Run(src)
        dst1 = color.dst2 + 1
        dst1.SetTo(0, task.depthOutline)
        dst3 = vbPalette(dst1 * 255 / color.classCount)

        redC.Run(dst1)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class






Public Class RedCloud_ColorInDepth : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        desc = "Create RedCloud output using only color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst0 = src
        dst0.SetTo(0, task.depthOutline)
        redC.Run(dst0)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class






Public Class RedCloud_DepthOutline : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        redOptions.UseColor.Checked = True
        desc = "Use the task.depthOutline over time to isolate high quality cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If heartBeat() Then dst3.SetTo(0)
        dst3 = dst3 Or task.depthOutline

        dst1.SetTo(0)
        src.CopyTo(dst1, Not dst3)
        redC.Run(dst1)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class







Public Class RedCloud_Combine : Inherits VB_Algorithm
    Dim color As New Color_Basics
    Public guided As New GuidedBP_Depth
    Dim redP As New RedCloud_CPP
    Public minCells As New List(Of segCell)
    Public Sub New()
        desc = "Combined the color and cloud as indicated in the RedOptions panel."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If redOptions.UseColor.Checked Or redOptions.UseDepthAndColor.Checked Then
            If src.Channels = 3 Then
                color.Run(src)
                dst2 = color.dst2
            Else
                dst2 = src
            End If
        Else
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        End If

        If redOptions.UseDepth.Checked Or redOptions.UseDepthAndColor.Checked Then
            Select Case redOptions.depthInput
                Case "GuidedBP_Depth"
                    guided.Run(src)
                    guided.dst2 += color.classCount + 1 ' keep separate color and depth regions
                    guided.dst2.CopyTo(dst2, task.depthMask)
                Case "RedCloud_Core"
                    Static prep As New RedCloud_Core
                    prep.Run(task.pointCloud)
                    prep.dst2 += color.classCount + 1 ' keep separate color and depth regions
                    prep.dst2.CopyTo(dst2, task.depthMask)
            End Select
        End If

        redP.Run(dst2)
        dst2 = redP.dst2
        dst3 = redP.dst3

        minCells.Clear()
        Dim limitedPrepRun As Boolean
        If task.drawRect.Width * task.drawRect.Height > 10 Then limitedPrepRun = True
        For Each rp In redP.minCells
            If limitedPrepRun Then If task.drawRect.Contains(rp.floodPoint) = False Then Continue For
            minCells.Add(rp)
        Next
    End Sub
End Class









Public Class RedCloud_CPP : Inherits VB_Algorithm
    Public minCells As New List(Of segCell)
    Public classCount As Integer
    Public Sub New()
        cPtr = RedCloud_Open()
        gOptions.HistBinSlider.Value = 16 ' jumpstart the likely option automation result.
        desc = "Floodfill every pixel in the prepared input."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then
            Static guided As New GuidedBP_Depth
            guided.Run(src)
            src = guided.dst2
            src.ConvertTo(src, cv.MatType.CV_8U)
        End If

        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)

        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        If redOptions.UseDepth.Checked Then
            Dim maskData(task.noDepthMask.Total - 1) As Byte
            Marshal.Copy(task.noDepthMask.Data, maskData, 0, maskData.Length)
            Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)
            classCount = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(),
                                      src.Rows, src.Cols, redOptions.imageThresholdPercent, 250)
            handleMask.Free()
        Else
            classCount = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), 0, src.Rows, src.Cols,
                                      redOptions.imageThresholdPercent, 250)
        End If
        handleInput.Free()

        If classCount = 0 Then Exit Sub ' no depth yet...

        Dim ptData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC2, RedCloud_FloodPointList(cPtr))

        Dim floodPoints As New List(Of cv.Point)
        For i = 0 To classCount - 1
            floodPoints.Add(ptData.Get(Of cv.Point)(i, 0))
        Next

        Dim floodFlag = 4 Or cv.FloodFillFlags.FixedRange

        minCells.Clear()
        Dim other = New segCell
        other.mask = New cv.Mat(1, 1, cv.MatType.CV_8U, 255)
        other.rect = New cv.Rect(0, 0, 1, 1)
        minCells.Add(other)
        Dim mask As New cv.Mat(src.Height + 2, src.Width + 2, cv.MatType.CV_8U, 0)
        If redOptions.UseDepth.Checked Then
            task.noDepthMask.CopyTo(mask(New cv.Rect(1, 1, mask.Width - 2, mask.Height - 2)))
            mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 255, 1)
        End If
        Dim fill As Integer
        Dim totalPixels As Integer
        classCount = 1
        Dim colorRun = redOptions.UseColor.Checked
        For i = 0 To floodPoints.Count - 1
            Dim rp As New segCell
            fill = classCount
            rp.floodPoint = floodPoints(i)
            If mask.Get(Of Byte)(rp.floodPoint.Y, rp.floodPoint.X) = 0 Then
                If colorRun Then
                    If task.depthOutline.Get(Of Byte)(rp.floodPoint.Y, rp.floodPoint.X) <> 0 Then Continue For
                End If
                rp.index = classCount
                rp.pixels = src.FloodFill(mask, rp.floodPoint, New cv.Scalar(fill), rp.rect, 0, 0, floodFlag Or fill << 8)
                If rp.rect.Width = 0 Then Continue For
                rp.mask = mask(rp.rect).InRange(fill, fill)
                minCells.Add(rp)
                totalPixels += rp.pixels
                classCount += 1
            End If
        Next

        dst2 = src
        dst3 = vbPalette(dst2 * 255 / classCount)
        If heartBeat() Then
            labels(2) = "Found " + CStr(classCount) + " classes - " + Format(totalPixels / src.Total, "0%") + " of the image."
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
    End Sub
End Class






Public Class RedCloud_OnlyDepth : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseDepth.Checked = True  ' <<<<<<< this is what is different.
        desc = "Create RedCloud output using only depth."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class







Public Class RedCloud_OnlyColor : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True  ' <<<<<<< this is what is different.
        redOptions.FeatureLessRadio.Checked = True
        desc = "Create RedCloud output using only color."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class







Public Class RedCloud_MeterByMeter : Inherits VB_Algorithm
    Dim meter As New BackProject_MeterByMeter
    Public Sub New()
        desc = "Run RedCloud meter by meter"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        meter.Run(src)
        dst2 = meter.dst3
        labels(2) = meter.labels(3)

        For i = 0 To task.maxZmeters

        Next
    End Sub
End Class