Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class RedCloud_Basics : Inherits VB_Algorithm
    Public prepCells As New List(Of rcPrep)
    Dim matchCell As New RedCloud_MatchCell
    Public combine As New RedCloud_CombineColor
    Public colorOnly As Boolean = False
    Public depthOnly As Boolean = False
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Match cells from the previous generation"
    End Sub
    Public Function redSelect(ByRef dstInput0 As cv.Mat, ByRef dstInput1 As cv.Mat, ByRef dstInput2 As cv.Mat) As rcData
        If task.drawRect <> New cv.Rect Then Return New rcData
        If task.redCells.Count = 0 Then Return New rcData
        If task.clickPoint = New cv.Point(0, 0) Then Return New rcData
        Dim index = task.cellMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
        'If task.clickPoint = New cv.Point Then index = 1

        Dim rc = task.redCells(index)
        If task.mouseClickFlag Or heartBeat() Then
            rc.maxDStable = rc.maxDist
            task.redCells(index) = rc
        End If

        If rc.index = task.redOther Then
            rc.maxDist = task.clickPoint
            rc.maxDStable = rc.maxDist
            rc.gridID = task.gridToRoiIndex.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
            rc.rect = task.gridList(rc.gridID)
            rc.mask = task.cellMap(rc.rect).InRange(task.redOther, task.redOther)
        End If

        dstInput0.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
        dstInput0(rc.rect).SetTo(cv.Scalar.White, rc.mask)

        dstInput1.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
        dstInput1(rc.rect).SetTo(cv.Scalar.White, rc.mask)

        dstInput2(rc.rect).SetTo(cv.Scalar.White, rc.mask)
        dstInput2.Circle(rc.maxDist, task.dotSize, cv.Scalar.Black, -1, task.lineType)

        dstInput2.Circle(rc.maxDStable, task.dotSize + 2, cv.Scalar.Black, -1, task.lineType)
        dstInput2.Circle(rc.maxDStable, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Return rc
    End Function
    Public Sub RunVB(src As cv.Mat)
        task.redOther = 0

        If standalone Then
            dst0 = src
            dst1 = task.depthRGB.Resize(src.Size)
        End If

        combine.colorOnly = colorOnly
        combine.depthOnly = depthOnly
        combine.Run(src)
        prepCells = combine.prepCells

        If firstPass Then
            task.cellMap.SetTo(task.redOther)
            matchCell.lastCells.Clear()
        End If

        matchCell.lastCellMap = task.cellMap.Clone
        matchCell.lastCells = New List(Of rcData)(task.redCells)
        matchCell.usedColors.Clear()
        matchCell.usedColors.Add(black)

        If heartBeat() Then matchCell.dst2 = New cv.Mat(src.Size, cv.MatType.CV_8UC3, 0)

        If dst2.Size <> src.Size Then dst2 = New cv.Mat(src.Size, cv.MatType.CV_8UC3, 0)
        task.cellMap.SetTo(task.redOther)

        task.redCells.Clear()
        task.redCells.Add(New rcData)
        Dim spotsRemoved As Integer
        For Each rp In prepCells
            rp.maxDist = vbGetMaxDist(rp)
            Dim spotTakenTest = task.cellMap.Get(Of Byte)(rp.maxDist.Y, rp.maxDist.X)
            If spotTakenTest <> task.redOther Then
                spotsRemoved += 1
                Continue For
            End If

            rp.index = task.redCells.Count
            matchCell.rp = rp
            matchCell.Run(Nothing)

            If matchCell.rc.pixels < task.minPixels Then
                task.cellMap(matchCell.rc.rect).SetTo(task.redOther, matchCell.rc.mask)
                Continue For
            End If
            task.redCells.Add(matchCell.rc)

            If task.redCells.Count >= 255 Then Exit For ' we are going to handle only the largest 255 cells - "Other" (zero) for the rest.
        Next

        If task.drawRect = New cv.Rect Then
            Dim rp As New rcPrep
            rp.rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
            rp.mask = task.cellMap(rp.rect).InRange(task.redOther, task.redOther)
            rp.maxDist = New cv.Point(0, 0)
            rp.floodPoint = rp.maxDist
            rp.pixels = rp.mask.CountNonZero
            rp.index = task.redOther
            matchCell.rp = rp
            matchCell.Run(Nothing)
            task.redCells(0) = matchCell.rc
            task.cellMap(matchCell.rc.rect).SetTo(matchCell.rc.index, matchCell.rc.mask)
        End If

        Dim unMatchedCells As Integer
        For i = task.redCells.Count - 1 To 0 Step -1
            Dim rc = task.redCells(i)
            Dim valMax = task.cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            ' if the maxdist has been overlaid by another cell, then correct it here.
            If valMax <> rc.index Then task.cellMap.Set(Of Byte)(rc.maxDist.Y, rc.maxDist.X, rc.index)

            task.cellMap(rc.rect).SetTo(rc.index, rc.mask)
            dst2(rc.rect).SetTo(rc.color, rc.mask)

            If rc.indexLast = 0 Then unMatchedCells += 1
        Next

        Static changedTotal As Integer
        changedTotal += unMatchedCells
        labels(3) = CStr(changedTotal) + " new/moved cells in the last second " +
                    Format(changedTotal / (task.frameCount - task.toggleFrame), fmt1) + " unmatched per frame"
        labels(2) = CStr(task.redCells.Count) + " cells " + CStr(unMatchedCells) + " did not match the previous frame. Click a cell to see more."
        If heartBeat() Then changedTotal = 0

        task.rcSelect = redSelect(dst0, dst1, dst2)
    End Sub
End Class









Public Class RedCloud_MatchCell : Inherits VB_Algorithm
    Public rp As New rcPrep
    Public rc As New rcData
    Public lastCells As New List(Of rcData)
    Public lastCellMap As New cv.Mat
    Public usedColors As New List(Of cv.Vec3b)
    Public Sub New()
        task.cellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        lastCellMap = task.cellMap.Clone
        strOut = "RedCloud_MatchCell takes an rcPrep cell and builds an rcData cell." + vbCrLf +
                 "When standalone, it just build a fake rcPrep cell and displays the rcData equivalent."
        desc = "Build a RedCloud cell from the rcPrep input"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone And heartBeat() Then
            rp.floodPoint = New cv.Point(msRNG.Next(0, dst2.Width / 2), msRNG.Next(0, dst2.Height / 2))
            Dim w = msRNG.Next(1, dst2.Width / 2), h = msRNG.Next(1, dst2.Height / 2)
            rp.rect = New cv.Rect(rp.floodPoint.X, rp.floodPoint.Y, w, h)
            rp.mask = task.depthRGB(rp.rect).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            rp.pixels = rp.mask.CountNonZero
            rp.maxDist = vbGetMaxDist(rp)
            dst2.SetTo(0)
        End If

        rc = New rcData
        rc.index = rp.index
        rc.rect = rp.rect
        rc.motionRect = rc.rect
        rc.mask = rp.mask
        rc.pixels = rp.pixels
        rc.floodPoint = rp.floodPoint
        rc.maxDist = rp.maxDist

        rc.maxDStable = rc.maxDist ' assume it has to use the latest.
        rc.indexLast = lastCellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
        If rc.indexLast = task.redOther Then
            For i = rc.floodPoint.Y To Math.Min(rc.rect.Y + rc.rect.Height, dst2.Height) - 1
                rc.indexLast = lastCellMap.Get(Of Byte)(i, rc.floodPoint.X)
                If rc.indexLast = rc.index Then Exit For
            Next
        End If
        If rc.indexLast < lastCells.Count And rc.indexLast <> task.redOther Then
            Dim lrc = lastCells(rc.indexLast)
            rc.motionRect = rc.rect.Union(lrc.rect)
            rc.color = lrc.color
            rc.maxDStable = lrc.maxDStable

            Dim stableCheck = lastCellMap.Get(Of Byte)(lrc.maxDStable.Y, lrc.maxDStable.X)
            If stableCheck = rc.indexLast Then rc.maxDStable = lrc.maxDStable ' keep maxDStable if cell matched to previous
        End If

        If usedColors.Contains(rc.color) Then
            rc.color = New cv.Vec3b(msRNG.Next(30, 240), msRNG.Next(30, 240), msRNG.Next(30, 240))
            If standalone Then dst2(rc.rect).SetTo(cv.Scalar.White, rc.mask)
            rc.indexLast = 0
        End If

        usedColors.Add(rc.color)

        rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
        vbDrawContour(rc.mask, rc.contour, rc.index, -1)

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
        rc.gridID = task.gridToRoiIndex.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)

        If standalone Then setTrueText(strOut, 3)
    End Sub
End Class








Module RedCloud_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RedCloud_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Run(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Sizes(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Points(cPtr As IntPtr) As IntPtr
    End Function





    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Neighbors_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub RedCloud_Neighbors_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Neighbors_List(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Neighbors_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Neighbors_RunCPP(cPtr As IntPtr, dataPtr As IntPtr,
                                              rows As Integer, cols As Integer,
                                              contourPtr As IntPtr, contourCount As Integer,
                                              cellIndex As Integer) As Integer
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Corners_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub RedCloud_Corners_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Corners_List(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RedCloud_Corners_RunCPP(cPtr As IntPtr, dataPtr As IntPtr,
                                            rows As Integer, cols As Integer,
                                            distance As Integer) As Integer
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










Public Class RedCloud_CellStats : Inherits VB_Algorithm
    Dim plot As New Histogram_Depth
    Dim pca As New PCA_Basics
    Dim eq As New Plane_Equation
    Public redC As Object
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        If standalone Then redC = New RedCloud_Basics
        dst0 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        desc = "Display the statistics for the selected cell."
    End Sub
    Public Sub statsString(src As cv.Mat)
        If standalone Then
            redC.Run(src)
            dst2 = redC.dst2
            dst3 = redC.dst3
        End If

        Dim tmp = New cv.Mat(task.rcSelect.mask.Rows, task.rcSelect.mask.Cols, cv.MatType.CV_32F, 0)
        task.pcSplit(2)(task.rcSelect.rect).CopyTo(tmp, task.rcSelect.mask)
        plot.rc = task.rcSelect
        plot.Run(tmp)
        dst1 = plot.dst2

        Dim rcSelect = task.rcSelect
        strOut = "rc.index = " + CStr(rcSelect.index) + " of " + CStr(task.redCells.Count) + vbTab
        strOut += " rc.gridID = " + CStr(rcSelect.gridID) + vbCrLf
        strOut += "rc.rect: " + CStr(rcSelect.rect.X) + ", " + CStr(rcSelect.rect.Y) + ", "
        strOut += CStr(rcSelect.rect.Width) + ", " + CStr(rcSelect.rect.Height) + vbTab

        strOut += "rc.pixels " + CStr(rcSelect.pixels) + vbTab + "rc.color = " + rcSelect.color.ToString() + vbCrLf

        strOut += "rc.maxDist = " + CStr(rcSelect.maxDist.X) + ", " + CStr(rcSelect.maxDist.Y) + vbCrLf
        strOut += "rc.centroid = " + CStr(rcSelect.centroid.X) + ", " + CStr(rcSelect.centroid.Y) + vbCrLf

        strOut += "Min/Max/Range: X = " + Format(rcSelect.minVec.X, fmt1) + "/" + Format(rcSelect.maxVec.X, fmt1)
        strOut += "/" + Format(rcSelect.maxVec.X - rcSelect.minVec.X, fmt1) + vbTab

        strOut += "Y = " + Format(rcSelect.minVec.Y, fmt1) + "/" + Format(rcSelect.maxVec.Y, fmt1)
        strOut += "/" + Format(rcSelect.maxVec.Y - rcSelect.minVec.Y, fmt1) + vbTab

        strOut += "Z = " + Format(rcSelect.minVec.Z, fmt1) + "/" + Format(rcSelect.maxVec.Z, fmt1)
        strOut += "/" + Format(rcSelect.maxVec.X - rcSelect.minVec.X, fmt1) + vbCrLf + vbCrLf

        strOut += "Cell Mean in 3D: x/y/z = " + vbTab + Format(rcSelect.depthMean.X, fmt2) + vbTab
        strOut += Format(rcSelect.depthMean.Y, fmt2) + vbTab + Format(rcSelect.depthMean.Z, fmt2) + vbCrLf

        strOut += "Cell color mean B/G/R " + vbTab + Format(rcSelect.colorMean(0), fmt2) + vbTab
        strOut += Format(rcSelect.colorMean(1), fmt2) + vbTab + Format(rcSelect.colorMean(2), fmt2) + vbCrLf

        strOut += "Cell color stdev B/G/R " + vbTab + Format(rcSelect.colorStdev(0), fmt2) + vbTab
        strOut += Format(rcSelect.colorStdev(1), fmt2) + vbTab + Format(rcSelect.colorStdev(2), fmt2) + vbCrLf

        If rcSelect.colorOnly = False Then
            eq.rc = rcSelect
            eq.Run(src)
            rcSelect = eq.rc
            strOut += vbCrLf + eq.strOut + vbCrLf

            pca.rc = rcSelect
            pca.Run(Nothing)
            strOut += vbCrLf + pca.strOut
        End If
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static redP As New RedCloud_Basics
            redP.Run(src)
            dst2 = redP.dst2
            labels(2) = redP.labels(2)
        End If

        If heartBeat() Or task.mouseClickFlag Then statsString(src)

        setTrueText(strOut, 3)

        labels(1) = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + CStr(CInt(task.maxZmeters)) + " meters"
    End Sub
End Class









Public Class RedCloud_Motion : Inherits VB_Algorithm
    Dim redP As New RedCloud_Basics
    Dim diff As New Diff_Basics
    Public rect As New cv.Rect
    Public motionList As New List(Of Integer)
    ' Dim opAuto As New OpAuto_PixelDifference
    Public Sub New()
        gOptions.PixelDiffThreshold.Value = 9
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Use absDiff to build a mask of cells that changed."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        diff.Run(src)
        ' opAuto.Run(diff.dst3)

        redP.Run(src)
        dst3 = redP.dst2

        If heartBeat() Then
            rect = New cv.Rect
            dst2.SetTo(0)
        End If
        For Each rc In task.redCells
            Dim tmp As cv.Mat = rc.mask And diff.dst3(rc.rect)
            If tmp.CountNonZero > task.minPixels And rc.index > 0 Then
                If rect.Width = 0 Then rect = rc.motionRect Else rect = rect.Union(rc.motionRect)
            End If
        Next

        If rect.Width > 0 Then dst2(rect).SetTo(255)
    End Sub
End Class







Public Class RedCloud_MotionTest : Inherits VB_Algorithm
    Dim rMotion As New RedCloud_Motion
    Public Sub New()
        desc = "Reconstruct the latest RGB image from an older RGB image and RedCloud_Motion output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMotion.Run(src)
        dst3 = rMotion.dst2

        If heartBeat() Then dst2 = src.Clone Else src.CopyTo(dst2, rMotion.dst2)
        task.color.Rectangle(rMotion.rect, cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class








'Public Class RedCloud_MinRes : Inherits VB_Algorithm
'    Public redC As New RedCloud_Basics
'    Dim grid As New Grid_Basics
'    Public Sub New()
'        desc = "Compute the RedCloud_Basics cells at minimum resolution."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        dst1 = vbMinResize(task.pointCloud)
'        grid.Run(dst1)

'        redC.Run(dst1)
'        dst2 = redC.dst2
'        If heartBeat() Then labels(2) = redC.labels(2)
'    End Sub
'End Class










Public Class RedCloud_Hulls : Inherits VB_Algorithm
    Dim convex As New Convex_RedCloudDefects
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "Cells where convexity defects failed", "", "Improved contour results using OpenCV's ConvexityDefects"}
        desc = "Add hulls and improved contours using ConvexityDefects to each RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        dst3.SetTo(0)
        Dim defectCount As Integer
        task.cellMap.SetTo(0)
        Dim redCells As New List(Of rcData)
        For Each rc In task.redCells
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
                vbDrawContour(task.cellMap(rc.rect), rc.hull, rc.index, -1)
            End If
            redCells.Add(rc)
        Next
        task.redCells = New List(Of rcData)(redCells)
        labels(2) = CStr(task.redCells.Count) + " hulls identified below.  " + CStr(defectCount) + " hulls failed to build the defect list."
    End Sub
End Class









Public Class RedCloud_FindCells : Inherits VB_Algorithm
    Public cellList As New List(Of Integer)
    Public Sub New()
        gOptions.PixelDiffThreshold.Value = 25
        cPtr = RedCloud_FindCells_Open()
        desc = "Find all the RedCloud cells touched by the mask created by the Motion_History rectangle"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cellList = New List(Of Integer)

        If standalone Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        Dim rc = task.rcSelect

        Dim cells As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim r = New cv.Rect(rc.rect.X - 1, rc.rect.Y - 1, rc.rect.Width + 2, rc.rect.Height + 2)
        r = validateRect(r)
        task.cellMap(r).CopyTo(cells(r))

        Dim cppData(cells.Total - 1) As Byte
        Marshal.Copy(cells.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = RedCloud_FindCells_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), cells.Rows, cells.Cols)
        handleSrc.Free()

        Dim count = RedCloud_FindCells_TotalCount(cPtr)
        Dim cellsFound(count - 1) As Integer
        Marshal.Copy(imagePtr, cellsFound, 0, cellsFound.Length)

        cellList = cellsFound.ToList
        dst3.SetTo(0)
        dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst0 = dst0.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        For Each index In cellList
            Dim rcX As rcData = task.redCells(index)
            vbDrawContour(dst3(rcX.rect), rcX.contour, rcX.color, -1)
            dst3(rcX.rect).SetTo(cv.Scalar.White, dst0(rcX.rect))
        Next
        labels(3) = CStr(count) + " cells were found using the motion mask"
    End Sub
    Public Sub Close()
        RedCloud_FindCells_Close(cPtr)
    End Sub
End Class





Public Class RedCloud_Neighbors_CPP : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        cPtr = RedCloud_Neighbors_Open()
        desc = "Find the list of neighbors for a cell using the cell contour and cellmap"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        Dim rc = task.rcSelect
        If rc.contour.Count = 0 Then
            setTrueText("The selected cell has no contour", 3)
            Exit Sub
        End If

        Dim r = New cv.Rect(rc.rect.X - 1, rc.rect.Y - 1, rc.rect.Width + 2,
                            rc.rect.Height + 2)
        r = validateRect(r)

        Dim input = task.cellMap(r).Clone
        Dim cppData(input.Total - 1) As Byte
        Marshal.Copy(input.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)

        Dim contour As New cv.Mat(rc.contour.Count, 1, cv.MatType.CV_32S, rc.contour.ToArray)
        Dim contourData(contour.Total * 2 - 1) As Integer
        Marshal.Copy(contour.Data, contourData, 0, contourData.Length - 1)
        Dim handleContour = GCHandle.Alloc(contourData, GCHandleType.Pinned)

        Dim imagePtr = RedCloud_Neighbors_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(),
                                                 input.Rows, input.Cols,
                                                 handleContour.AddrOfPinnedObject(),
                                                 rc.contour.Count, rc.index)
        handleContour.Free()
        handleSrc.Free()

        dst3.SetTo(0)
        Dim count = RedCloud_Neighbors_Count(cPtr)
        If count > 0 Then
            Dim idList = New cv.Mat(count, 1, cv.MatType.CV_32S, RedCloud_Neighbors_List(cPtr))
            For i = 0 To count - 1
                Dim rcX = task.redCells(idList.Get(Of Integer)(i, 0))
                vbDrawContour(dst3(rcX.rect), rcX.contour, rcX.color, -1)
            Next
        End If
        labels(2) = redC.labels(2)
        labels(3) = CStr(count) + " neighbors for the cell"
    End Sub
    Public Sub Close()
        RedCloud_Neighbors_Close(cPtr)
    End Sub
End Class




Public Class RedCloud_Corners_CPP : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min distance to nearby point", 0, 100, 5)
        End If

        cPtr = RedCloud_Corners_Open()
        desc = "Find all the corners in the RedCloud cellmap"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static distanceSlider = findSlider("Min distance to nearby point")

        redC.Run(src)
        dst2 = redC.dst2

        Dim mask = task.cellMap.InRange(task.redOther, task.redOther)
        task.cellMap.SetTo(0, mask)

        Dim cppData(task.cellMap.Total - 1) As Byte
        Marshal.Copy(task.cellMap.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim count = RedCloud_Corners_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(),
                                            task.cellMap.Rows, task.cellMap.Cols,
                                            distanceSlider.value)
        handleSrc.Free()

        If count > 0 Then
            Dim tmp = New cv.Mat(count, 1, cv.MatType.CV_32SC2, RedCloud_Corners_List(cPtr))
            For i = 0 To count - 1
                Dim pt = tmp.Get(Of cv.Point)(i, 0)
                dst2.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1)
            Next
        End If
        labels(3) = CStr(count) + " corners were found"
    End Sub
    Public Sub Close()
        RedCloud_Corners_Close(cPtr)
    End Sub
End Class







Public Class RedCloudY_Color : Inherits VB_Algorithm
    Public guided As New GuidedBP_Depth
    Public buildCells As New GuidedBP_FloodCells
    Public redCells As New List(Of rcData)
    Dim rcMatch As New ReductionCloud_Match
    Public showSelected As Boolean = True
    Dim reduction As New Reduction_Basics
    Public Sub New()
        gOptions.HistBinSlider.Value = 15
        desc = "Segment the image based on both the reduced point cloud and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        guided.Run(src)

        reduction.Run(src)
        Dim combined = reduction.dst2.Clone
        guided.backProject.CopyTo(combined, task.depthMask)
        buildCells.Run(combined)

        rcMatch.redCells = buildCells.redCells
        rcMatch.Run(src)
        redCells = New List(Of rcData)(rcMatch.redCells)
        dst2 = rcMatch.dst2

        If showSelected Then task.rcSelect = rcMatch.showSelect()
        If heartBeat() Then labels(2) = rcMatch.labels(2)
    End Sub
End Class









Public Class RedCloud_FeatureLess : Inherits VB_Algorithm
    Public colorC As New RedCloud_Basics
    Dim fLess As New FeatureLess_Basics
    Public lastCells As New List(Of rcData)
    Public Sub New()
        labels = {"", "", "RedCloud output", "Featureless input"}
        desc = "Use RedCloud on the output of a featureless algorithm"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fLess.Run(src)
        colorC.Run(fLess.dst2)
        dst2 = colorC.dst2

        lastCells = task.redCells
        labels(2) = CStr(task.redCells.Count) + " cells detected by " + traceName
    End Sub
End Class









Public Class RedCloud_NeighborRect : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public redCells As New List(Of rcData)
    Public Sub New()
        desc = "Use an expanded rc.rect to determine neighbors for each RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static rcx As New rcData
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        redCells.Clear()
        For Each rc In task.redCells
            Dim rect = expandRect(rc.rect)
            Dim minDepth = rc.minVec.Z
            Dim maxDepth = rc.maxVec.Z
            rc.neighbors = New List(Of Integer)
            rc.neighbors.Add(rc.index)
            For Each rcN In task.redCells
                If rcN.rect.IntersectsWith(rect) Then
                    If maxDepth = 0 Or rcN.maxVec.Z = 0 Then
                        rc.neighbors.Add(rcN.index)
                    Else
                        If rcN.depthMean.Z >= minDepth And rcN.depthMean.Z <= maxDepth Then
                            rc.neighbors.Add(rcN.index)
                        End If
                    End If
                End If
            Next
            redCells.Add(rc)
            ' setTrueText(CStr(rc.index), rc.maxDist)
        Next

        rcx = task.rcSelect
        dst2.Rectangle(expandRect(rcx.rect), cv.Scalar.Yellow, task.lineWidth, task.lineType)

        If rcx.neighbors Is Nothing Then Exit Sub
        dst3.SetTo(0)
        For Each index In rcx.neighbors
            Dim rc = redCells(index)
            vbDrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
        Next
    End Sub
End Class







Public Class RedCloud_ViewRight : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public rightCells As New List(Of rcData)
    Public rightMap As New cv.Mat
    Public Sub New()
        redC.colorOnly = True
        rightMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Segment the right view image with RedCloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.redCells = New List(Of rcData)(rightCells)
        task.cellMap = rightMap.Clone

        redC.Run(task.rightView)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        rightCells = New List(Of rcData)(task.redCells)
        rightMap = task.cellMap.Clone
    End Sub
End Class







Public Class RedCloud_ViewLeft : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public leftCells As New List(Of rcData)
    Public leftMap As New cv.Mat
    Public Sub New()
        redC.colorOnly = True
        leftMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Segment the left view image with RedCloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.redCells = New List(Of rcData)(leftCells)
        task.cellMap = leftMap.Clone

        redC.Run(task.leftView)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        leftCells = New List(Of rcData)(task.redCells)
        leftMap = task.cellMap.Clone
    End Sub
End Class








Public Class RedCloud_LeftRight : Inherits VB_Algorithm
    Dim stLeft As New RedCloud_ViewRight
    Dim stRight As New RedCloud_ViewLeft
    Public Sub New()
        desc = "Match cells in the left view to the right view - something is flipped here..."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        stRight.Run(Nothing)
        dst2 = stRight.dst2
        labels(2) = "Left view - " + stRight.labels(2)

        stLeft.Run(Nothing)
        dst3 = stLeft.dst2
        labels(3) = "Right view - " + stLeft.labels(2)
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







'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedCloud_PlaneColor : Inherits VB_Algorithm
    Public options As New Options_Plane
    Public redC As New RedCloud_Basics
    Dim planeMask As New RedCloudY_PlaneFromMask
    Dim planeContour As New RedCloudY_PlaneFromContour
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
        For Each rc In task.redCells
            If options.useMaskPoints Then
                planeMask.rc = rc
                planeMask.Run(Nothing)
                rc = planeMask.rc
            ElseIf options.useContourPoints Then
                planeContour.rc = rc
                planeContour.Run(Nothing)
                rc = planeContour.rc
            ElseIf options.use3Points Then
                If rc.contour.Count > 3 Then rc.eq = build3PointEquation(rc)
            End If
            dst3(rc.rect).SetTo(New cv.Scalar(Math.Abs(255 * rc.eq(0)), Math.Abs(255 * rc.eq(1)), Math.Abs(255 * rc.eq(2))), rc.mask)
        Next
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
            redCells = New List(Of rcData)(task.redCells)
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

        Dim changedCells = task.redCells.Count
        Dim usedPixels = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero
        Dim changedPixels = usedPixels
        For Each rc As rcData In task.redCells
            Dim vec = dst3.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            If vec = New cv.Vec3b Then
                changedCells -= 1
                changedPixels -= rc.pixels
            End If
        Next

        dst2Last = dst2.Clone
        If heartBeat() Then
            labels(2) = "Changed cells = " + Format(changedCells, "000") + " cells or " + Format(changedCells / task.redCells.Count, "0%")
            labels(3) = "Changed pixel total = " + Format(changedPixels / 1000, "0.0") + "k or " + Format(changedPixels / usedPixels, "0%")
        End If
    End Sub
End Class











Public Class RedCloud_BothColorAndCloud : Inherits VB_Algorithm
    Dim redC As New RedCloud_DepthOnly
    Dim colorC As New RedCloud_ColorOnly
    Public Sub New()
        desc = "Compare the RedCloud results for the point cloud and color."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        colorC.Run(src)
        dst3 = colorC.dst2
        labels(3) = colorC.labels(2)
    End Sub
End Class














Public Class RedCloud_ColorOnly : Inherits VB_Algorithm
    Public colorC As New RedCloud_Basics
    Dim colorCells As New List(Of rcData)
    Dim colorMap As New cv.Mat
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        colorMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        colorC.colorOnly = True
        desc = "Create RedCloud output using only color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.redCells = New List(Of rcData)(colorCells)
        task.cellMap = colorMap.Clone

        colorC.Run(src)
        dst2 = colorC.dst2
        dst3 = colorC.dst3

        If standalone Then
            dst0 = src
            dst1 = task.depthRGB
            colorC.redSelect(dst0, dst1, dst2)
        End If

        colorCells = New List(Of rcData)(task.redCells)
        colorMap = task.cellMap.Clone
        labels(2) = CStr(colorCells.Count) + " cells in colorCells"
    End Sub
End Class












Public Class RedCloud_DepthOnly : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Dim depthCells As New List(Of rcData)
    Dim depthMap As New cv.Mat
    Public Sub New()
        depthMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        redC.depthOnly = True
        desc = "Create RedCloud output using only depth."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.redCells = New List(Of rcData)(depthCells)
        task.cellMap = depthMap.Clone

        redC.Run(src)
        dst2 = redC.dst2
        dst2.SetTo(0, task.noDepthMask)

        depthCells = New List(Of rcData)(task.redCells)
        depthMap = task.cellMap.Clone
        labels(2) = redC.labels(2)
    End Sub
End Class






Public Class RedCloud_CombineColor : Inherits VB_Algorithm
    Public guided As New GuidedBP_Depth
    Public redP As New RedCloud_CPP
    Public prepCells As New List(Of rcPrep)
    Dim color As New Color_Basics
    Public colorOnly As Boolean = False
    Public depthOnly As Boolean = False
    Public Sub New()
        desc = "Segment the image on based both the reduced point cloud and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim combined As New cv.Mat
        If colorOnly Then
            color.Run(src)
            combined = color.dst2
        Else
            guided.Run(src)
            Dim maskOfDepth = guided.backProject.Threshold(0, 255, cv.ThresholdTypes.Binary)

            If depthOnly = False Then
                color.Run(task.color.Resize(src.Size))
                color.dst2.ConvertTo(combined, cv.MatType.CV_32F)
            End If

            guided.dst0.CopyTo(combined, maskOfDepth)
            combined.ConvertTo(combined, cv.MatType.CV_8U)
        End If
        redP.Run(combined)
        dst2 = redP.dst2

        prepCells.Clear()
        For Each key In redP.prepCells
            Dim rp = key.Value
            If task.drawRect <> New cv.Rect Then
                If task.drawRect.Contains(rp.floodPoint) = False Then Continue For
            End If

            prepCells.Add(rp)
        Next
    End Sub
End Class





Public Class RedCloud_CPP : Inherits VB_Algorithm
    Public prepCells As New SortedList(Of Integer, rcPrep)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        cPtr = RedCloud_Open()
        gOptions.HistBinSlider.Value = 15 ' jumpstart the likely option automation result.
        desc = "Floodfill every pixel in the prepared input."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then
            Static guided As New GuidedBP_Depth
            guided.Run(src)
            src = guided.backProject
            src.ConvertTo(src, cv.MatType.CV_8U)
        End If

        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)

        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim classCount = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleInput.Free()

        Dim ptData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC2, RedCloud_Points(cPtr))
        Dim sizes = New cv.Mat(classCount, 1, cv.MatType.CV_32S, RedCloud_Sizes(cPtr))

        Dim floodPoints As New List(Of cv.Point)
        Dim size As New List(Of Integer)
        For i = 0 To classCount - 1
            floodPoints.Add(ptData.Get(Of cv.Point)(i, 0))
            size.Add(sizes.Get(Of Integer)(i, 0))
        Next

        Dim floodFlag = 4 Or cv.FloodFillFlags.FixedRange
        dst3 = src

        prepCells.Clear()
        Dim other = New rcPrep
        other.mask = New cv.Mat(1, 1, cv.MatType.CV_8U, 255)
        other.rect = New cv.Rect(0, 0, 1, 1)
        other.pixels = 1
        prepCells.Add(1, other)
        Dim mask As New cv.Mat(src.Height + 2, src.Width + 2, cv.MatType.CV_8U, 0)
        Dim fill As Integer, rect As cv.Rect
        For i = 0 To floodPoints.Count - 1
            Dim rp As New rcPrep
            fill = prepCells.Count Mod 255
            rp.floodPoint = floodPoints(i)
            If mask.Get(Of Byte)(rp.floodPoint.Y, rp.floodPoint.X) = 0 Then
                rp.pixels = src.FloodFill(mask, rp.floodPoint, New cv.Scalar(fill), rect, 0, 0, floodFlag Or fill << 8)
                If rect.Width = 0 Then Continue For
                rp.mask = src(rect).InRange(fill, fill)
                rp.rect = New cv.Rect(rect.X, rect.Y, rect.Width, rect.Height)
                rp.floodPoint = New cv.Point(rp.floodPoint.X, rp.floodPoint.Y)
                prepCells.Add(rp.pixels, rp)
            End If
        Next

        labels(3) = "8-bit version of the output with " + CStr(dst3.InRange(0, 0).CountNonZero) + " zero pixels"
        dst2 = vbPalette(dst3)
        If heartBeat() Then labels(2) = CStr(prepCells.Count) + " regions found"
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
    End Sub
End Class




