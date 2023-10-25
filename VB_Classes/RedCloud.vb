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

        Dim rc = task.redCells(index)
        If task.mouseClickFlag Or heartBeat() Then
            rc.maxDStable = rc.maxDist
            task.redCells(index) = rc
        End If

        If rc.index = task.redOther Then
            rc.maxDist = task.clickPoint
            rc.maxDStable = rc.maxDist
            Dim gridID = task.gridToRoiIndex.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
            rc.rect = task.gridList(gridID)
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

        dst0 = src
        If standalone Then dst1 = task.depthRGB.Resize(src.Size)

        combine.colorOnly = colorOnly
        combine.depthOnly = depthOnly
        combine.Run(src)
        prepCells = combine.prepCells

        If firstPass Then
            task.cellMap.SetTo(task.redOther)
            task.lastCells.Clear()
        End If

        matchCell.lastCellMap = task.cellMap.Clone
        task.lastCells = New List(Of rcData)(task.redCells)
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
        If heartBeat() Then
            labels(2) = CStr(task.redCells.Count) + " cells " + CStr(unMatchedCells) + " did not match the previous frame. Click a cell to see more."
            changedTotal = 0
        End If

        task.rcSelect = redSelect(dst0, dst1, dst2)
        dst3 = matchCell.dst3
    End Sub
End Class









Public Class RedCloud_MatchCell : Inherits VB_Algorithm
    Public rp As New rcPrep
    Public rc As New rcData
    Public lastCellMap As New cv.Mat
    Public usedColors As New List(Of cv.Vec3b)
    Public Sub New()
        task.cellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        lastCellMap = task.cellMap.Clone
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
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
        If rc.indexLast < task.lastCells.Count And rc.indexLast <> task.redOther Then
            Dim lrc = task.lastCells(rc.indexLast)
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
            If heartBeat() Then dst3.SetTo(0)
            dst3(rc.rect).SetTo(255, rc.mask)
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

        If standalone Then setTrueText(strOut, 3)
    End Sub
End Class








Module RedCloud_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FCell_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FCell_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FCell_Rects(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FCell_Sizes(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FCell_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FCell_Run(
                cPtr As IntPtr, dataPtr As IntPtr, maskPtr As IntPtr, rows As Integer, cols As Integer,
                type As Integer, minPixels As Integer, diff As Integer) As IntPtr
    End Function



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





    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodEx_Open(width As Integer, height As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodEx_Close(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodEx_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodEx_CellRects(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodEx_CellSizes(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function FloodEx_Run(cPtr As IntPtr,
               grayPtr As IntPtr, diff As Integer) As IntPtr
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
    Public runRedCloud As Boolean
    Public redC As Object
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        If standalone Then redC = New RedCloud_Basics
        If standalone Then gOptions.HistBinSlider.Value = 20

        desc = "Display the statistics for the selected cell."
    End Sub
    Public Sub statsString(src As cv.Mat)
        Dim tmp = New cv.Mat(task.rcSelect.mask.Rows, task.rcSelect.mask.Cols, cv.MatType.CV_32F, 0)
        task.pcSplit(2)(task.rcSelect.rect).CopyTo(tmp, task.rcSelect.mask)
        plot.rc = task.rcSelect
        plot.Run(tmp)
        dst1 = plot.dst2

        Dim rcSelect = task.rcSelect
        strOut = "rc.index = " + CStr(rcSelect.index) + " of " + CStr(task.redCells.Count) + vbTab
        Dim gridID = task.gridToRoiIndex.Get(Of Integer)(rcSelect.maxDist.Y, rcSelect.maxDist.X)
        strOut += " gridID = " + CStr(gridID) + vbCrLf
        strOut += "rc.rect: " + CStr(rcSelect.rect.X) + ", " + CStr(rcSelect.rect.Y) + ", "
        strOut += CStr(rcSelect.rect.Width) + ", " + CStr(rcSelect.rect.Height) + vbTab

        strOut += "rc.pixels " + CStr(rcSelect.pixels) + vbTab + "rc.color = " + rcSelect.color.ToString() + vbCrLf

        strOut += "rc.maxDist = " + CStr(rcSelect.maxDist.X) + ", " + CStr(rcSelect.maxDist.Y) + vbCrLf

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
        If standalone Or runRedCloud Then
            If firstPass Then redC = New RedCloud_Basics
            redC.Run(src)
            dst0 = redC.dst0
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        If task.mouseClickFlag Then statsString(src) Else setTrueText("Click any RedCloud cell to see depth histogram here.", 1)

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












Public Class RedCloud_CellsAtDepth : Inherits VB_Algorithm
    Dim plot As New Plot_Histogram
    Dim kalman As New Kalman_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        plot.noZeroEntry = False
        labels(3) = "Histogram of depth weighted by the size of the cell."
        desc = "Create a histogram of depth using RedCloud cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)

        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim histBins = redOptions.HistBinSlider.Value
        Dim slotList(histBins) As List(Of Integer)
        For i = 0 To slotList.Count - 1
            slotList(i) = New List(Of Integer)
        Next
        Dim hist(histBins - 1) As Single
        For Each rc In task.redCells
            Dim slot As Integer
            If rc.depthMean.Z > task.maxZmeters Then rc.depthMean.Z = task.maxZmeters
            slot = CInt((rc.depthMean.Z / task.maxZmeters) * histBins)
            If slot >= hist.Length Then slot = hist.Length - 1
            slotList(slot).Add(rc.index)
            If rc.pixels > dst2.Total / 2 Then Dim k = 0
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
            Dim rc = task.redCells(slotList(histIndex)(i))
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
        desc = "Display RedCloud output at a fixed frame rate"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fps.Run(Nothing)

        If fps.heartBeat Then
            redC.Run(src)
            dst2 = redC.dst2.Clone
        End If
        labels(2) = redC.labels(2) + " " + fps.strOut
    End Sub
End Class






Public Class RedCloud_MinMaxNone : Inherits VB_Algorithm
    Dim depth As New Depth_MinMaxNone
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        labels = {"", "", "MinMaxNone point cloud", "RedCloud output with MinMaxNone input"}
        desc = "Use the MinMaxNone point cloud as input to RedCloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        depth.Run(src)
        dst2 = depth.dst3
        dst2.ConvertTo(dst0, cv.MatType.CV_8U)
        hulls.Run(dst0)
        dst3 = hulls.dst2
    End Sub
End Class








Public Class OpenGL_RedCloudStable : Inherits VB_Algorithm
    Dim redC As New RedCloud_MinMaxNone
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Using MinMaxNone as source for RedCloud instead of point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        dst3 = redC.dst3
        labels = redC.labels

        cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1), dst2}, task.ogl.pointCloudInput)
        task.ogl.Run(redC.dst3)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
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






'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedCloud_PlaneFromContour : Inherits VB_Algorithm
    Public rc As New rcData
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation each cell's contour"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then setTrueText("Provide cell data to compute plane equation")
        If rc.contour Is Nothing Then Exit Sub
        Dim fitPoints As New List(Of cv.Point3f)
        For Each pt In rc.contour
            If pt.X >= rc.rect.Width Or pt.Y >= rc.rect.Height Then Continue For
            If rc.mask.Get(Of Byte)(pt.Y, pt.X) = 0 Then Continue For
            fitPoints.Add(task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X))
        Next
        rc.eq = fitDepthPlane(fitPoints)
    End Sub
End Class







'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedCloud_PlaneFromMask : Inherits VB_Algorithm
    Public rc As New rcData
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation from the pointcloud samples in a RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then setTrueText("Provide cell data to compute plane equation")
        If rc.contour Is Nothing Then Exit Sub
        Dim fitPoints As New List(Of cv.Point3f)
        For y = 0 To rc.rect.Height - 1
            For x = 0 To rc.rect.Width - 1
                If rc.mask.Get(Of Byte)(y, x) Then fitPoints.Add(task.pointCloud(rc.rect).Get(Of cv.Point3f)(y, x))
            Next
        Next
        rc.eq = fitDepthPlane(fitPoints)
    End Sub
End Class







Public Class RedCloud_ColorAndCloudSeparate : Inherits VB_Algorithm
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







Public Class RedCloud_ColorAndCloud : Inherits VB_Algorithm
    Dim guided As New GuidedBP_Depth
    Public fCell As New RedCell_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        gOptions.HistBinSlider.Value = 20
        desc = "Segment the image based on both the reduced point cloud and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        guided.Run(src)

        reduction.Run(src)
        Dim combined = reduction.dst2.Clone
        guided.backProject.CopyTo(combined, task.depthMask)
        fCell.Run(combined)

        dst2 = fCell.dst2
        dst3 = fCell.dst3

        If heartBeat() Then labels(2) = CStr(task.fCells.Count) + " regions identified"
    End Sub
End Class








Public Class RedCloud_PrepPointCloud : Inherits VB_Algorithm
    Public Sub New()
        desc = "Reduction transform for the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim reduceAmt = redOptions.PCreductionSlider.Value
        task.pointCloud.ConvertTo(dst0, cv.MatType.CV_32S, 1000 / reduceAmt)

        Dim split = dst0.Split()

        Select Case redOptions.PCReduction
            Case OptionsRedCloud.reduceX
                dst0 = split(0) * reduceAmt
            Case OptionsRedCloud.reduceY
                dst0 = split(1) * reduceAmt
            Case OptionsRedCloud.reduceZ
                dst0 = split(2) * reduceAmt
            Case OptionsRedCloud.reduceXY
                dst0 = split(0) * reduceAmt + split(1) * reduceAmt
            Case OptionsRedCloud.reduceXZ
                dst0 = split(0) * reduceAmt + split(2) * reduceAmt
            Case OptionsRedCloud.reduceYZ
                dst0 = split(1) * reduceAmt + split(2) * reduceAmt
            Case OptionsRedCloud.reduceXYZ
                dst0 = split(0) * reduceAmt + split(1) * reduceAmt + split(2) * reduceAmt
        End Select

        Dim mm = vbMinMax(dst0)
        dst2 = (dst0 - mm.minVal)

        dst2.SetTo(mm.maxVal - mm.minVal, task.maxDepthMask)
        dst2.SetTo(0, task.noDepthMask)
        mm = vbMinMax(dst2)
        dst2 *= 254 / mm.maxVal
        dst2 += 1

        labels(2) = "Reduced Pointcloud - reduction factor = " + CStr(reduceAmt)
    End Sub
End Class







Public Class RedCloud_PrepTest : Inherits VB_Algorithm
    Dim prep As New RedCloud_PrepPointCloud
    Public fCell As New RedCell_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        gOptions.HistBinSlider.Value = 20
        desc = "Segment the image based on both the reduced point cloud and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)

        prep.Run(Nothing)
        prep.dst2.ConvertScaleAbs().CopyTo(reduction.dst2, task.depthMask)

        fCell.Run(reduction.dst2)

        dst2 = fCell.dst2
        dst3 = fCell.dst3

        If heartBeat() Then labels(2) = CStr(task.fCells.Count) + " regions identified"
    End Sub
End Class








Public Class RedCloud_BProject3D : Inherits VB_Algorithm
    Dim colorC As New RedCloud_Basics
    Dim bp3d As New Histogram3D_BP
    Public Sub New()
        desc = "Run RedCloudY_Basics on the output of the RGB 3D backprojection"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bp3d.Run(src)
        dst3 = bp3d.dst2

        colorC.Run(dst3)
        dst2 = colorC.dst2
    End Sub
End Class









Public Class RedCloud_SliceH : Inherits VB_Algorithm
    Dim stats As New RedCloud_CellStats
    Public Sub New()
        stats.redC = New RedCloud_Basics
        redOptions.Channels12.Checked = True
        desc = "Build horizontal RedCloud cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        stats.redC.run(src)

        stats.Run(src)
        dst2 = stats.redC.dst2
        setTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_SliceV : Inherits VB_Algorithm
    Dim stats As New RedCloud_CellStats
    Public Sub New()
        stats.redC = New RedCloud_Basics
        redOptions.Channels02.Checked = True
        desc = "Build vertical RedCloud cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        stats.redC.run(src)

        stats.Run(src)
        dst2 = stats.redC.dst2
        setTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_World : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim world As New Depth_World
    Public Sub New()
        labels = {"", "", "RedCloud reduction of generated point cloud", "Generated pointcloud"}
        desc = "Display the output of a generated pointcloud as RedCloud cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        world.Run(src)
        task.pointCloud = world.dst2

        redC.Run(src)
        dst2 = redC.dst2
    End Sub
End Class







Public Class RedCloud_ByDepth : Inherits VB_Algorithm
    Dim colorC As New RedCloud_Basics
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








Public Class RedCloud_UnstableCells : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim diff As New Diff_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Identify cells that were not the same color in the previous generation"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        diff.Run(redC.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

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











Public Class RedCloud_ContourCorners : Inherits VB_Algorithm
    Public corners(4 - 1) As cv.Point
    Public rc As New rcData
    Public Sub New()
        labels(2) = "The RedCloud Output with the highlighted contour to smooth"
        desc = "Find the point farthest from the center in each cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
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







Public Class RedCloud_KMeans : Inherits VB_Algorithm
    Dim km As New KMeans_MultiChannel
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "KMeans_MultiChannel output", "RedCloudY_Basics output"}
        desc = "Use RedCloud to identify the regions created by kMeans"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(src)
        dst2 = km.dst2

        redC.Run(km.dst3)
        dst3 = redC.dst2
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
        dst2 = diff.dst2
        redC.Run(dst2)

        dst3.SetTo(0)
        redC.dst2.CopyTo(dst3, dst2)

        labels(3) = CStr(task.redCells.Count) + " objects identified in the diff output"
    End Sub
End Class











Public Class RedCloud_LineID : Inherits VB_Algorithm
    Public lines As New Line_Basics
    Public rCells As New List(Of rcData)
    Dim p1list As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
    Dim p2list As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
    Dim rectList As New List(Of cv.Point)
    Dim maxDistance As Integer
    Public redC As New RedCloud_Basics
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
        rInput.setto(0)
        p1list.Clear()
        For i = lines.sortLength.Count - 1 To 0 Step -1
            Dim mps = lines.mpList(lines.sortLength.ElementAt(i).Value)
            rInput.Line(mps.p1, mps.p2, 0, isolineWidth, cv.LineTypes.Link4)
            rInput.Line(mps.p1, mps.p2, 255, lineWidth, cv.LineTypes.Link4)
            p1list.Add(mps.p1.Y, mps.p1)
        Next

        If rInput.Type = cv.MatType.CV_32SC1 Then rInput.convertto(rInput, cv.MatType.CV_8U)

        redC.Run(rInput)
        dst2.SetTo(0)
        For Each rc In task.redCells
            If rc.rect.Width = 0 Or rc.rect.Height = 0 Then Continue For
            If rc.rect.Width < dst2.Width / 2 Or rc.rect.Height < dst2.Height / 2 Then dst2(rc.rect).SetTo(rc.color, rc.mask)
        Next

        If task.redCells.Count < 3 Then Exit Sub ' dark room - no cells.

        Dim rcLargest As New rcData
        For Each rc In task.redCells
            If rc.rect.Width > dst2.Width / 2 And rc.rect.Height > dst2.Height / 2 Then Continue For
            If rc.pixels > rcLargest.pixels Then rcLargest = rc
        Next

        dst2.Rectangle(rcLargest.rect, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
        labels(2) = CStr(task.redCells.Count) + " lines were identified.  Largest line detected is highlighted in yellow"
    End Sub
End Class










Public Class RedCloud_KNNCenters : Inherits VB_Algorithm
    Dim lines As New RedCloud_LineID
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
        For Each rc In task.redCells
            knn.queries.Add(rc.maxDist)
        Next

        knn.Run(Nothing)

        Dim trace As New List(Of cv.Point2f)
        Static regularPt As New List(Of cv.Point2f)
        dst3.SetTo(0)
        regularPt.Clear()
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







Public Class RedCloud_ProjectCell : Inherits VB_Algorithm
    Dim topView As New Histogram_ShapeTop
    Dim sideView As New Histogram_ShapeSide
    Dim mats As New Mat_4Click
    Dim colorC As New RedCloud_Basics
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