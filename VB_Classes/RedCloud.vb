Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedCloud_Basics : Inherits VB_Algorithm
    Public redCells As New List(Of rcData)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public combine As New RedCloud_Combine
    Dim unmatched As New RedCloud_UnmatchedCount
    Dim colorMap As New cv.Mat(256, 1, cv.MatType.CV_8UC3, 0)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        vbAddAdvice(traceName + ": there is dedicated panel for RedCloud algorithms." + vbCrLf +
                        "It is behind the global options (which affect most algorithms.)")
        desc = "Match cells from the previous generation"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        combine.Run(src)

        If task.optionsChanged Then cellMap.SetTo(0)
        Dim lastCells As New List(Of rcData)(redCells), lastCellMap As cv.Mat = cellMap.Clone
        Dim usedColors As New List(Of cv.Vec3b)({black})

        If dst2.Size <> src.Size Then dst2 = New cv.Mat(src.Size, cv.MatType.CV_8UC3, 0)

        task.rcMatchMax = 0
        Dim minPixels = gOptions.minPixelsSlider.Value
        Dim newCells As New List(Of rcData)
        For Each rc In combine.combinedCells
            rc.maxDStable = rc.maxDist ' assume it has to use the latest.
            rc.indexLast = lastCellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If rc.indexLast < lastCells.Count Then
                Dim lrc = lastCells(rc.indexLast)
                rc.color = lrc.color
                rc.matchFlag = True

                Dim stableCheck = lastCellMap.Get(Of Byte)(lrc.maxDStable.Y, lrc.maxDStable.X)
                If stableCheck = rc.indexLast Then
                    rc.maxDStable = lrc.maxDStable ' keep maxDStable if cell matched to previous
                    rc.matchCount = lrc.matchCount + 1
                End If
            End If

            If usedColors.Contains(rc.color) Then
                rc.color = randomCellColor()
                rc.matchCount = 0
                rc.matchFlag = False
            End If

            usedColors.Add(rc.color)

            rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            vbDrawContour(rc.mask, rc.contour, 255, -1)

            rc.depthMask = rc.mask.Clone
            rc.depthMask.SetTo(0, task.noDepthMask(rc.rect))
            rc.depthPixels = rc.depthMask.CountNonZero

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

            cv.Cv2.MeanStdDev(src(rc.rect), rc.colorMean, rc.colorStdev, rc.mask)

            rc.pixels = rc.mask.CountNonZero
            If rc.mask.Size = dst2.Size Or rc.pixels < minPixels Then Continue For
            If task.heartBeat Then rc.matchCount = 1
            newCells.Add(rc)

            If task.rcMatchMax < rc.matchCount Then task.rcMatchMax = rc.matchCount
            If newCells.Count >= 255 Then Exit For ' we are going to handle only the largest 255 cells - rest are zero.
        Next

        cellMap.SetTo(0)
        dst2.SetTo(0)
        redCells.Clear()
        redCells.Add(New rcData)
        For Each rc In newCells
            rc.index = redCells.Count
            colorMap.Set(Of cv.Vec3b)(rc.index, 0, rc.color) ' <<<< switch to using colormap.
            redCells.Add(rc)
            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            ' dst2(rc.rect).SetTo(rc.color, rc.mask)  ' <<<< switch to using colormap.
        Next

        Dim rcZero = redCells(0)
        rcZero.mask = cellMap.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        rcZero.pixels = rcZero.mask.CountNonZero
        rcZero.rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)

        cv.Cv2.ApplyColorMap(cellMap, dst2, colorMap)  ' <<<< switch to using colormap.
        unmatched.redCells = redCells
        unmatched.Run(src)

        If task.motionReset Then
            dst3.SetTo(0)
        Else
            dst3 = unmatched.dst3
            labels = unmatched.labels
            dst3(redCells(0).rect).SetTo(0, redCells(0).mask)
        End If

        setSelectedCell(redCells, cellMap)
    End Sub
End Class






Public Class RedCloud_OnlyCore : Inherits VB_Algorithm
    Dim prep As New RedCloud_Core
    Public redC As New RedCloud_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        gOptions.HistBinSlider.Value = 20
        desc = "Segment the image based on both the reduced point cloud and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)

        prep.Run(empty)
        prep.dst2.ConvertScaleAbs().CopyTo(reduction.dst2, task.depthMask)

        redC.Run(reduction.dst2)

        dst2 = redC.cellMap
        dst3 = redC.dst2
        labels = redC.labels
    End Sub
End Class








Public Class RedCloud_OnlyCoreToo : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        redOptions.Reduction_Basics.Checked = True
        redOptions.RedCloud_Core.Checked = True
        gOptions.HistBinSlider.Value = 20
        desc = "Identical to RedCloud_OnlyCore but using only RedOptions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.cellMap
        dst3 = redC.dst2
        labels = redC.labels
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
        redC.cellMap.SetTo(0)
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

        Dim count As Integer
        dst3.SetTo(0)
        If task.motionDetected Then
            dst1 = redC.cellMap(task.motionRect).Clone
            Dim cppData(dst1.Total - 1) As Byte
            Marshal.Copy(dst1.Data, cppData, 0, cppData.Length - 1)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = RedCloud_FindCells_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
            handleSrc.Free()

            count = RedCloud_FindCells_TotalCount(cPtr)
            If count = 0 Then Exit Sub

            Dim cellsFound(count - 1) As Integer
            Marshal.Copy(imagePtr, cellsFound, 0, cellsFound.Length)

            cellList = cellsFound.ToList
            dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst0 = dst0.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
            For Each index In cellList
                Dim rc = redC.redCells(index)
                vbDrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
                dst3(rc.rect).SetTo(cv.Scalar.White, dst0(rc.rect))
            Next
            dst2.Rectangle(task.motionRect, cv.Scalar.White, task.lineWidth)
        End If
        identifyCells(redC.redCells)
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
        If standaloneTest() Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            redCells = New List(Of rcData)(redC.redCells)
        End If

        Dim newCells As New List(Of rcData)
        For Each rc As rcData In redCells
            If rc.contour.Count > 4 Then
                eq.rc = rc
                eq.Run(empty)
                newCells.Add(eq.rc)
            End If
        Next

        redCells = New List(Of rcData)(newCells)

        If task.heartBeat Then
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

        Dim rc = task.rc

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

        Dim rc = task.rc
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
        fps.Run(empty)

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
    Dim planeCells As New Plane_CellColor
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
            rc.eq = New cv.Vec4f
            If options.useMaskPoints Then
                rc.eq = fitDepthPlane(planeCells.buildMaskPointEq(rc))
            ElseIf options.useContourPoints Then
                rc.eq = fitDepthPlane(planeCells.buildContourPoints(rc))
            ElseIf options.use3Points Then
                rc.eq = build3PointEquation(rc)
            End If
            dst3(rc.rect).SetTo(New cv.Scalar(Math.Abs(255 * rc.eq(0)),
                                              Math.Abs(255 * rc.eq(1)),
                                              Math.Abs(255 * rc.eq(2))), rc.mask)
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
        If standaloneTest() Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        Dim rc = task.rc
        Dim fitPoints As New List(Of cv.Point3f)
        For Each pt In rc.contour
            If pt.X >= rc.rect.Width Or pt.Y >= rc.rect.Height Then Continue For
            If rc.mask.Get(Of Byte)(pt.Y, pt.X) = 0 Then Continue For
            fitPoints.Add(task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X))
        Next
        rc.eq = fitDepthPlane(fitPoints)
        If standaloneTest() Then
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
        If standaloneTest() Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        Dim rc = task.rc
        Dim fitPoints As New List(Of cv.Point3f)
        For y = 0 To rc.rect.Height - 1
            For x = 0 To rc.rect.Width - 1
                If rc.mask.Get(Of Byte)(y, x) Then fitPoints.Add(task.pointCloud(rc.rect).Get(Of cv.Point3f)(y, x))
            Next
        Next
        rc.eq = fitDepthPlane(fitPoints)
        If standaloneTest() Then
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

        Dim rc = task.rc

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
        mats.Run(empty)
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

        Dim rcX = task.rc
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

        Dim rc = task.rc
        If rc.maxVec.Z Then
            eq.rc = rc
            eq.Run(empty)
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
        If task.heartBeat Then goodList.Clear()

        Dim nextGood As New List(Of cv.Point2f)(features.feat.featurePoints)
        goodList.Add(nextGood)

        If goodList.Count >= task.frameHistoryCount Then goodList.RemoveAt(0)

        dst3.SetTo(0)
        For Each ptList In goodList
            For Each pt In ptList
                Dim c = dst2.Get(Of cv.Vec3b)(pt.Y, pt.X)
                dst3.Circle(pt, task.dotSize, c, -1, task.lineType)
            Next
        Next
    End Sub
End Class









Public Class RedCloud_FeatureLess : Inherits VB_Algorithm
    Dim cpp As New CPP_Basics
    Public Sub New()
        cpp.updateFunction(algorithmList.functionNames._CPP_RedColor_FeatureLess)
        desc = "This is a duplicate of FeatureLess_RedCloud to make it easier to find."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cpp.Run(src)
        dst2 = cpp.dst3
        labels(2) = cpp.labels(2)
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

        If task.heartBeat Or task.frameCount = 2 Then
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

        If task.heartBeat Or task.frameCount = 2 Then
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
        If standaloneTest() Then redC = New RedCloud_Basics
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
        If task.heartBeat Then
            labels(2) = "Changed cells = " + Format(changedCells, "000") + " cells or " + Format(changedCells / redC.redCells.Count, "0%")
            labels(3) = "Changed pixel total = " + Format(changedPixels / 1000, "0.0") + "k or " + Format(changedPixels / dst2.Total, "0%")
        End If
    End Sub
End Class








Public Class RedCloud_NearestStableCell : Inherits VB_Algorithm
    Public knn As New KNN_Core
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

        knn.Run(empty)
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








Public Class RedCloud_FloodPoint : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim stats As New Cell_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
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
        mats.Run(empty)
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
    Dim colorClass As New Color_Basics
    Public Sub New()
        labels(3) = "Color input to RedCloud_Basics with depth boundary blocking color connections."
        desc = "Use the depth outline as input to RedCloud_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        outline.Run(task.depthMask)

        colorClass.Run(src)
        dst1 = colorClass.dst2 + 1
        dst1.SetTo(0, outline.dst2)
        dst3 = vbPalette(dst1 * 255 / colorClass.classCount)

        redC.Run(dst1)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class






Public Class RedCloud_MasksInDepth : Inherits VB_Algorithm
    Dim outline As New Depth_Outline
    Dim redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        desc = "Create RedCloud output using only color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        outline.Run(task.depthMask)

        dst0 = src
        dst0.SetTo(0, outline.dst2)
        redC.Run(dst0)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class






Public Class RedCloud_DepthOutline : Inherits VB_Algorithm
    Dim outline As New Depth_Outline
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        redOptions.UseColor.Checked = True
        desc = "Use the Depth_Outline output over time to isolate high quality cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        outline.Run(task.depthMask)

        If task.heartBeat Then dst3.SetTo(0)
        dst3 = dst3 Or outline.dst2

        dst1.SetTo(0)
        src.CopyTo(dst1, Not dst3)
        redC.Run(dst1)
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
        desc = "Create RedCloud output using only color."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class









Public Class RedCloud_BinarizeColor : Inherits VB_Algorithm
    Dim binarize As New Binarize_FourWay
    Dim redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        labels(3) = "A 4-way split of the input grayscale image based on brightness"
        desc = "Use RedCloud on a 4-way split based on light to dark in the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binarize.Run(src)
        dst3 = vbPalette(binarize.dst2 * 255 / 5)

        redC.Run(binarize.dst2)
        dst2 = redC.dst2
        If standaloneTest() Then identifyCells(redC.redCells)
        labels(2) = redC.labels(3)
    End Sub
End Class









' https://docs.opencv.org/master/de/d01/samples_2cpp_2connected_components_8cpp-example.html
Public Class RedCloud_CCompColor : Inherits VB_Algorithm
    Dim ccomp As New CComp_Both
    Dim redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        desc = "Identify each Connected component as a RedCloud Cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(src)
        dst3 = vbNormalize32f(ccomp.dst1)
        labels(3) = ccomp.labels(2)

        redC.Run(dst3)
        dst2 = redC.dst2
        labels(2) = redC.labels(3)
    End Sub
End Class






Public Class RedCloud_LeftRight : Inherits VB_Algorithm
    Dim redLeft As New RedCloud_OnlyColor
    Dim redRight As New RedCloud_OnlyColor
    Public Sub New()
        redOptions.Reduction_Basics.Checked = True
        desc = "Floodfill left and right images after RedCloud color input reduction."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redLeft.Run(task.leftView)
        dst2 = redLeft.dst2
        labels(2) = redLeft.redC.labels(3)

        redRight.Run(task.rightView)
        dst3 = redRight.dst2
        labels(3) = redRight.redC.labels(3)
    End Sub
End Class









Public Class RedCloud_Cells : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public cellmap As New cv.Mat
    Public redCells As New List(Of rcData)
    Public Sub New()
        redOptions.UseColor.Checked = True
        desc = "Create RedCloud output using only color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        cellmap = redC.cellMap
        redCells = redC.redCells
    End Sub
End Class









Public Class RedCloud_Flippers : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        labels(3) = "Highlighted below are the cells which flipped in color from the previous frame."
        desc = "Identify the 4-way split cells that are flipping between brightness boundaries."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst3 = redC.dst3
        labels(3) = redC.labels(2)

        Static lastMap As cv.Mat = redC.cellMap.Clone
        dst2.SetTo(0)
        Dim unMatched As Integer
        Dim unMatchedPixels As Integer
        For Each cell In redC.redCells
            Dim lastColor = lastMap.Get(Of cv.Vec3b)(cell.maxDist.Y, cell.maxDist.X)
            If lastColor <> cell.color Then
                dst2(cell.rect).SetTo(cell.color, cell.mask)
                unMatched += 1
                unMatchedPixels += cell.pixels
            End If
        Next
        lastMap = redC.dst3.Clone

        If (standaloneTest()) And redC.redCells.Count > 1 Then identifyCells(redC.redCells)

        If task.heartBeat Then
            labels(3) = "Unmatched to previous frame: " + CStr(unMatched) + " totaling " + CStr(unMatchedPixels) + " pixels."
        End If
    End Sub
End Class







Public Class RedCloud_Overlaps : Inherits VB_Algorithm
    Public redCells As New List(Of rcData)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Remove the overlapping cells.  Keep the largest."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels = redC.labels

        Dim overlappingCells As New List(Of Integer)
        cellMap.SetTo(0)
        redCells.Clear()
        For Each rc In redC.redCells
            Dim valMap = cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            redCells.Add(rc)
        Next

        dst3.SetTo(0)
        For i = overlappingCells.Count - 1 To 0 Step -1
            Dim rc = redCells(overlappingCells(i))
            dst3(rc.rect).SetTo(rc.color, rc.mask)
            redCells.RemoveAt(overlappingCells(i))
        Next

        labels(3) = "Before removing overlapping cells: " + CStr(redC.redCells.Count) + ". After: " + CStr(redCells.Count)
    End Sub
End Class







Public Class RedCloud_OnlyColorHist3D : Inherits VB_Algorithm
    Dim rMin As New RedCloud_OnlyColor
    Dim hColor As New Hist3Dcolor_Basics
    Public Sub New()
        desc = "Use the backprojection of the 3D RGB histogram as input to RedCloud_OnlyColor."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hColor.Run(src)
        dst2 = hColor.dst3
        labels(2) = hColor.labels(3)

        rMin.Run(dst2)
        dst3 = rMin.redC.cellMap
        dst3.SetTo(0, task.noDepthMask)
        labels(3) = rMin.labels(2)
    End Sub
End Class






Public Class RedCloud_OnlyColorAlt : Inherits VB_Algorithm
    Public redMasks As New RedCloud_Masks
    Public redCells As New List(Of rcData)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Dim lastColors As cv.Mat
    Dim lastMap As cv.Mat = dst2.Clone
    Public Sub New()
        lastColors = dst3.Clone
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redMasks.Run(src)
        Dim lastCells As New List(Of rcData)(redCells)

        redCells.Clear()
        cellMap.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors = New List(Of cv.Vec3b)({black})
        Dim unmatched As Integer
        For Each key In redMasks.sortedCells
            Dim cell = key.Value
            Dim index = lastMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X)
            If index < lastCells.Count Then
                cell.color = lastColors.Get(Of cv.Vec3b)(cell.maxDist.Y, cell.maxDist.X)
                ' cell.maxDist = lastCells(index).maxDist
            Else
                unmatched += 1
            End If
            If usedColors.Contains(cell.color) Then
                unmatched += 1
                cell.color = randomCellColor()
            End If
            usedColors.Add(cell.color)

            If cellMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X) = 0 Then
                cell.index = redCells.Count
                redCells.Add(cell)
                cellMap(cell.rect).SetTo(cell.index, cell.mask)
                dst3(cell.rect).SetTo(cell.color, cell.mask)
            End If
        Next

        If standaloneTest() Then identifyCells(redCells)

        labels(3) = CStr(redCells.Count) + " cells were identified.  The top " + CStr(identifyCount) + " are numbered"
        labels(2) = redMasks.labels(3) + " " + CStr(unmatched) + " cells were not matched to previous frame."

        lastColors = dst3.Clone
        dst2 = cellMap.Clone
        lastMap = cellMap.Clone
        If redCells.Count > 0 Then dst1 = vbPalette(lastMap * 255 / redCells.Count)
    End Sub
End Class







Public Class RedCloud_Gaps : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim frames As New History_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find the gaps that are different in the RedCloud_Basics results."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(3)

        frames.Run(redC.cellMap.InRange(0, 0))
        dst3 = frames.dst2

        If redC.redCells.Count > 0 Then
            dst2(task.rc.rect).SetTo(cv.Scalar.White, task.rc.mask)
        End If

        If redC.redCells.Count > 0 Then
            Dim rc = redC.redCells(0) ' index can now be zero.
            dst3(rc.rect).SetTo(0, rc.mask)
        End If
        Dim count = dst3.CountNonZero
        labels(3) = "Unclassified pixel count = " + CStr(count) + " or " + Format(count / src.Total, "0%")
    End Sub
End Class







Public Class RedCloud_SizeOrder : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        vbAddAdvice(traceName + ": Use the goptions 'DebugSlider' to select which cell is isolated.")
        gOptions.DebugSlider.Value = 0
        desc = "Select blobs by size using the DebugSlider in the global options"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("Use the goptions 'DebugSlider' to select cells by size." + vbCrLf + "Size order changes frequently.", 3)
        redC.Run(src)
        dst2 = redC.dst3
        labels(2) = redC.labels(3)

        Dim index = gOptions.DebugSlider.Value
        If index < redC.redCells.Count Then
            dst3.SetTo(0)
            Dim cell = redC.redCells(index)
            dst3(cell.rect).SetTo(cell.color, cell.mask)
        End If
    End Sub
End Class






Public Class RedCloud_StructuredH : Inherits VB_Algorithm
    Dim motion As New RedCloud_MotionBGsubtract
    Dim transform As New Structured_TransformH
    Dim topView As New Histogram2D_Top
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        desc = "Display the RedCloud cells found with a horizontal slice through the cellMap."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim sliceMask = transform.createSliceMaskH()
        dst0 = src

        motion.Run(sliceMask.Clone)

        If task.heartBeat Then dst1.SetTo(0)
        dst1.SetTo(cv.Scalar.White, sliceMask)
        labels = motion.labels

        dst2.SetTo(0)
        For Each rc In motion.redCells
            If rc.motionFlag Then vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
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
    Dim motion As New RedCloud_MotionBGsubtract
    Dim transform As New Structured_TransformV
    Dim sideView As New Histogram2D_Side
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        desc = "Display the RedCloud cells found with a vertical slice through the cellMap."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim sliceMask = transform.createSliceMaskV()
        dst0 = src

        motion.Run(sliceMask.Clone)

        If task.heartBeat Then dst1.SetTo(0)
        dst1.SetTo(cv.Scalar.White, sliceMask)
        labels = motion.labels
        setTrueText("Move mouse in image to see impact.", 3)

        dst2.SetTo(0)
        For Each rc In motion.redCells
            If rc.motionFlag Then vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
        Next

        Dim pc As New cv.Mat(task.pointCloud.Size, cv.MatType.CV_32FC3, 0)
        task.pointCloud.CopyTo(pc, dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        sideView.Run(pc)
        dst3 = sideView.dst2

        dst2.SetTo(cv.Scalar.White, sliceMask)
        dst0.SetTo(cv.Scalar.White, sliceMask)
    End Sub
End Class






Public Class RedCloud_MotionBasics : Inherits VB_Algorithm
    Public redMasks As New RedCloud_Masks
    Public redCells As New List(Of rcData)
    Public rMotion As New RedCloud_MotionBGsubtract
    Dim lastColors = dst3.Clone
    Dim lastMap As cv.Mat = dst2.Clone
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "Mask of active RedCloud cells", "CV_8U representation of redCells", ""}
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redMasks.Run(src)

        rMotion.sortedCells = redMasks.sortedCells
        rMotion.Run(task.color.Clone)

        Dim lastCells As New List(Of rcData)(redCells)

        redCells.Clear()
        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors = New List(Of cv.Vec3b)({black})
        Dim motionCount As Integer
        For Each cell In rMotion.redCells
            Dim index = lastMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X)
            If cell.motionFlag = False Then
                If index > 0 And index < lastCells.Count Then cell = lastCells(index - 1)
            Else
                motionCount += 1
            End If

            If index > 0 And index < lastCells.Count Then
                cell.color = lastColors.Get(Of cv.Vec3b)(cell.maxDist.Y, cell.maxDist.X)
            End If
            If usedColors.Contains(cell.color) Then cell.color = randomCellColor()
            usedColors.Add(cell.color)

            If dst2.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X) = 0 Then
                cell.index = redCells.Count + 1
                redCells.Add(cell)
                dst2(cell.rect).SetTo(cell.index, cell.mask)
                dst3(cell.rect).SetTo(cell.color, cell.mask)

                setTrueText(CStr(cell.index), cell.maxDist, 2)
                setTrueText(CStr(cell.index), cell.maxDist, 3)
            End If
        Next

        labels(3) = "There were " + CStr(redCells.Count) + " collected cells and " + CStr(motionCount) +
                            " cells removed because of motion.  "

        lastColors = dst3.Clone
        lastMap = dst2.Clone
        If redCells.Count > 0 Then dst1 = vbPalette(lastMap * 255 / redCells.Count)
    End Sub
End Class







Public Class RedCloud_MotionBGsubtract : Inherits VB_Algorithm
    Public motion As New BGSubtract_Basics
    Public redCells As New List(Of rcData)
    Public sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        gOptions.PixelDiffThreshold.Value = 25
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use absDiff to build a mask of cells that changed."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        motion.Run(src)
        dst3 = motion.dst2

        Static redC As New RedCloud_Basics
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(3)

        redCells.Clear()
        dst1.SetTo(0)
        For Each rc In redC.redCells
            Dim tmp As cv.Mat = rc.mask And motion.dst2(rc.rect)
            If tmp.CountNonZero Then
                dst1(rc.rect).SetTo(rc.color, rc.mask)
                rc.motionFlag = True
            End If
            redCells.Add(rc)
        Next

    End Sub
End Class







Public Class RedCloud_ContourVsFeatureLess : Inherits VB_Algorithm
    Dim redMasks As New RedCloud_Masks
    Dim contour As New Contour_WholeImage
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        labels = {"", "Contour_WholeImage Input", "RedCloud_Masks - toggling between Contour and Featureless inputs",
                  "FeatureLess_Basics Input"}
        desc = "Compare Contour_WholeImage and FeatureLess_Basics as input to RedCloud_Masks"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static useContours = findRadio("Use Contour_WholeImage")

        contour.Run(src)
        dst1 = contour.dst2

        fLess.Run(src)
        dst3 = fLess.dst2

        If task.toggleOn Then redMasks.Run(dst3) Else redMasks.Run(dst1)
        dst2 = redMasks.dst3
    End Sub
End Class









Public Class RedCloud_UnmatchedCount : Inherits VB_Algorithm
    Public redCells As New List(Of rcData)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Count the unmatched cells and display them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static myFrameCount As Integer
        Static changedCellCounts As New List(Of Integer)
        myFrameCount += 1
        If standaloneTest() Then
            setTrueText("RedCloud_UnmatchedCount has no output when run standaloneTest()." + vbCrLf +
                        "It requires redCells and RedCloud_Basics is the only way to create redCells." + vbCrLf +
                        "Since RedCloud_Basics calls RedCloud_UnmatchedCount, it would be circular and never finish the initialize.")
            Exit Sub
        End If

        Dim unMatchedCells As Integer
        Dim mostlyColor As Integer
        Static framecounts As New List(Of Integer)
        Static frameLoc As New List(Of cv.Point)
        For i = 0 To redCells.Count - 1
            Dim rc = redCells(i)
            If redCells(i).depthPixels / redCells(i).pixels < 0.5 Then mostlyColor += 1
            If rc.matchFlag = False Then
                Dim val = dst3.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If val = 0 Then
                    dst3(rc.rect).SetTo(255, rc.mask)
                    unMatchedCells += 1
                    frameLoc.Add(rc.maxDist)
                    framecounts.Add(myFrameCount)
                End If
            End If
        Next
        If showIntermediate() Then
            For i = 0 To framecounts.Count - 1
                setTrueText(CStr(framecounts(i)), frameLoc(i), 2)
            Next
        End If
        changedCellCounts.Add(unMatchedCells)

        If task.heartBeat Then
            dst3.SetTo(0)
            framecounts.Clear()
            frameLoc.Clear()
            myFrameCount = 0
            Dim sum = changedCellCounts.Sum(), avg = If(changedCellCounts.Count > 0, changedCellCounts.Average(), 0)
            labels(3) = CStr(sum) + " new/moved cells in the last second " + Format(avg, fmt1) + " changed per frame"
            labels(2) = CStr(redCells.Count) + " cells, unmatched cells = " + CStr(unMatchedCells) + "   " +
                        CStr(mostlyColor) + " cells were mostly color and " + CStr(redCells.Count - mostlyColor) + " had depth."
            changedCellCounts.Clear()
        End If
    End Sub
End Class








Public Class RedCloud_Combine : Inherits VB_Algorithm
    Dim color As New Color_Basics
    Public guided As New GuidedBP_Depth
    Public redMasks As New RedCloud_Masks
    Public combinedCells As New List(Of rcData)
    Public Sub New()
        desc = "Combined the color and cloud as indicated in the RedOptions panel."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If redOptions.UseColor.Checked Or redOptions.UseDepthAndColor.Checked Then
            redMasks.inputMask = Nothing
            If src.Channels = 3 Then
                color.Run(src)
                dst2 = color.dst2.Clone
            Else
                dst2 = src
            End If
        Else
            redMasks.inputMask = task.noDepthMask
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        End If

        If redOptions.UseDepth.Checked Or redOptions.UseDepthAndColor.Checked Then
            Select Case redOptions.depthInputIndex
                Case 0 ' "GuidedBP_Depth"
                    guided.Run(src)
                    If color.classCount > 0 Then guided.dst2 += color.classCount
                    guided.dst2.CopyTo(dst2, task.depthMask)
                Case 1 ' "RedCloud_Core"
                    Static prep As New RedCloud_Core
                    prep.Run(task.pointCloud)
                    If color.classCount > 0 Then prep.dst2 += color.classCount
                    prep.dst2.CopyTo(dst2, task.depthMask)
            End Select
        End If

        redMasks.Run(dst2)
        dst2 = redMasks.dst2
        dst3 = redMasks.dst3

        combinedCells.Clear()
        Dim drawRectOnlyRun As Boolean
        If task.drawRect.Width * task.drawRect.Height > 10 Then drawRectOnlyRun = True
        For Each key In redMasks.sortedCells
            Dim rc = key.Value
            If drawRectOnlyRun Then If task.drawRect.Contains(rc.floodPoint) = False Then Continue For
            combinedCells.Add(rc)
        Next
    End Sub
End Class








Public Class RedCloud_Both : Inherits VB_Algorithm
    Public colorC As New RedCloud_Basics
    Public redC As New RedCloud_Basics
    Public Sub New()
        desc = "Run RedCloud for depth and for color at the same time and then combine."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redOptions.UseDepth.Checked = True
        task.optionsChanged = False
        redC.Run(src)
        dst2 = redC.dst2.Clone
        dst2.SetTo(0, task.noDepthMask)
        labels(2) = redC.labels(2)

        redOptions.UseColor.Checked = True
        task.optionsChanged = False
        colorC.Run(src)
        dst3 = colorC.dst2.Clone
        labels(3) = colorC.labels(2)

        Dim cellmap = colorC.cellMap.Clone
        redC.cellMap.CopyTo(cellmap, task.depthMask)

        Static redCSelected As Integer
        If task.mouseClickFlag Then
            redCSelected = If(task.mousePicTag = 2, RESULT_DST2, RESULT_DST3)
        End If

        If redCSelected = RESULT_DST2 Then
            setSelectedCell(redC.redCells, redC.cellMap)
        ElseIf redCSelected = RESULT_DST3 Then
            setSelectedCell(colorC.redCells, colorC.cellMap)
        End If
        dst3(task.rc.rect).SetTo(cv.Scalar.White, task.rc.mask)
    End Sub
End Class







Public Class RedCloud_Core : Inherits VB_Algorithm
    Public classCount As Integer
    Public givenClassCount As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("RedCloud_Core Reduction", 1, 2500, 250)
        If standaloneTest() Then redOptions.RedCloud_Core.Checked = True
        desc = "Reduction transform for the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static reductionSlider = findSlider("RedCloud_Core Reduction")
        Dim reduceAmt = reductionSlider.value
        task.pointCloud.ConvertTo(dst0, cv.MatType.CV_32S, 1000 / reduceAmt)

        Dim split = dst0.Split()

        Select Case redOptions.PCReduction
            Case 0 ' "X Reduction"
                dst0 = (split(0) * reduceAmt).toMat
            Case 1 ' "Y Reduction"
                dst0 = (split(1) * reduceAmt).toMat
            Case 2 ' "Z Reduction"
                dst0 = (split(2) * reduceAmt).toMat
            Case 3 ' "XY Reduction"
                dst0 = (split(0) * reduceAmt + split(1) * reduceAmt).toMat
            Case 4 ' "XZ Reduction"
                dst0 = (split(0) * reduceAmt + split(2) * reduceAmt).toMat
            Case 5 ' "YZ Reduction"
                dst0 = (split(1) * reduceAmt + split(2) * reduceAmt).toMat
            Case 6 ' "XYZ Reduction"
                dst0 = (split(0) * reduceAmt + split(1) * reduceAmt + split(2) * reduceAmt).toMat
        End Select

        Dim mm As mmData = vbMinMax(dst0)
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







Public Class RedCloud_Masks : Inherits VB_Algorithm
    Public sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
    Public inputMask As cv.Mat
    Public classCount As Integer
    Public imageThresholdPercent As Single = 0.98
    Public cellMinPercent As Single = 0.0001
    Public Sub New()
        cPtr = RedCloud_Open()
        desc = "Core interface to the C++ code for floodfill."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then
            Static colorClass As New Color_Basics
            colorClass.Run(src)
            src = colorClass.dst2
        End If

        Dim imagePtr As IntPtr
        If inputMask Is Nothing Then
            Dim inputData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), 0, src.Rows, src.Cols,
                                     src.Type, redOptions.DesiredCellSlider.Value, 0, imageThresholdPercent, cellMinPercent)
            handleInput.Free()
        Else
            Dim inputData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            Dim maskData(inputMask.Total - 1) As Byte
            Marshal.Copy(inputMask.Data, maskData, 0, maskData.Length)
            Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)

            imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), src.Rows, src.Cols,
                                    src.Type, redOptions.DesiredCellSlider.Value, 0, imageThresholdPercent, cellMinPercent)
            handleMask.Free()
            handleInput.Free()
        End If

        classCount = RedCloud_Count(cPtr)
        If classCount = 0 Then Exit Sub ' no data to process.

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
        If standaloneTest() Then dst3 = vbPalette(dst2 * 255 / classCount)

        If task.heartBeat Then labels(3) = CStr(classCount) + " cells found"

        Dim sizeData = New cv.Mat(classCount, 1, cv.MatType.CV_32S, RedCloud_Sizes(cPtr))
        Dim rectData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))
        Dim floodPointData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC2, RedCloud_FloodPoints(cPtr))
        sortedCells.Clear()
        For i = 0 To classCount - 1
            Dim rc As New rcData
            rc.index = sortedCells.Count + 1
            rc.rect = validateRect(rectData.Get(Of cv.Rect)(i, 0))
            rc.mask = dst2(rc.rect).InRange(rc.index, rc.index).Threshold(0, 255, cv.ThresholdTypes.Binary)

            rc.pixels = sizeData.Get(Of Integer)(i, 0)
            rc.floodPoint = floodPointData.Get(Of cv.Point)(i, 0)

            ' rc.mask.Rectangle(New cv.Rect(0, 0, rc.mask.Width, rc.mask.Height), 0, 1)
            Dim pt = vbGetMaxDist(rc.mask)
            rc.maxDist = New cv.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)

            If rc.pixels > 0 Then sortedCells.Add(rc.pixels, rc)
        Next

        If task.heartBeat Then labels(2) = "CV_8U format - " + CStr(classCount) + " cells were identified."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
    End Sub
End Class







Public Class RedCloud_MatchCell : Inherits VB_Algorithm
    Public rp As New rcData
    Public rc As New rcData
    Public lastCellMap As New cv.Mat
    Public lastColors As New cv.Mat
    Public lastCells As New List(Of rcData)
    Public usedColors As New List(Of cv.Vec3b)
    Dim tour As New Contour_RC_AddContour
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        strOut = "RedCloud_MatchCell takes an rcData cell and builds an rcData cell." + vbCrLf +
                 "When standaloneTest(), it just build a fake rcData cell and displays the rcData equivalent."
        desc = "Build a RedCloud cell from the rcData input"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() And task.heartBeat Then
            rp.floodPoint = New cv.Point(msRNG.Next(0, dst2.Width / 2), msRNG.Next(0, dst2.Height / 2))
            Dim w = msRNG.Next(1, dst2.Width / 2), h = msRNG.Next(1, dst2.Height / 2)
            rp.rect = New cv.Rect(rp.floodPoint.X, rp.floodPoint.Y, w, h)
            rp.mask = task.depthRGB(rp.rect).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            lastCellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst2.SetTo(0)
            dst2(rp.rect).SetTo(cv.Scalar.White)
        End If

        rc = New rcData
        rc.index = rp.index
        rc.rect = rp.rect
        rc.mask = rp.mask
        rc.maxDist = rp.maxDist

        rc.indexLast = lastCellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
        If rc.indexLast < lastCells.Count Then
            Dim lrc = lastCells(rc.indexLast)
            rc.color = lrc.color
            rc.matchCount = lrc.matchCount

            Dim stableCheck = lastCellMap.Get(Of Byte)(lrc.maxDStable.Y, lrc.maxDStable.X)
            If stableCheck = rc.indexLast Then rc.maxDStable = lrc.maxDStable ' keep maxDStable if cell matched to previous
        End If

        If usedColors.Contains(rc.color) Then
            rc.color = randomCellColor()
            rc.indexLast = 0
            rc.matchCount = 1
            rc.maxDStable = rc.maxDist
        Else
            rc.matchCount += 1
        End If

        Dim color = lastColors.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
        If color <> rc.color And rc.pixels > 200 Then Dim k = 0

        usedColors.Add(rc.color)

        'tour.Run(rc.mask)
        'rc.contour = tour.contour
        'rc.mask = tour.dst2
        'rc.pixels = rc.mask.CountNonZero

        'Dim minLoc As cv.Point, maxLoc As cv.Point
        'task.pcSplit(0)(rc.rect).MinMaxLoc(rc.minVec.X, rc.maxVec.X, minLoc, maxLoc, rc.mask)
        'task.pcSplit(1)(rc.rect).MinMaxLoc(rc.minVec.Y, rc.maxVec.Y, minLoc, maxLoc, rc.mask)
        'task.pcSplit(2)(rc.rect).MinMaxLoc(rc.minVec.Z, rc.maxVec.Z, minLoc, maxLoc, rc.mask)

        'Dim tmp = New cv.Mat(rc.mask.Size, cv.MatType.CV_8U, 0)
        'task.depthMask(rc.rect).CopyTo(tmp, rc.mask)
        'If tmp.CountNonZero / rc.pixels > 0.1 Then
        '    Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
        '    cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev, rc.mask)

        '    rc.depthMean = New cv.Point3f(depthMean(0), depthMean(1), depthMean(2))
        '    rc.depthStdev = New cv.Point3f(depthStdev(0), depthStdev(1), depthStdev(2))
        '    rc.depthCell = True
        'End If
    End Sub
End Class





Public Class RedCloud_MasksBoth : Inherits VB_Algorithm
    Public colorCells As New List(Of rcData)
    Public cloudCells As New List(Of rcData)
    Dim color As New Color_Basics
    Public redMasks As New RedCloud_Masks
    Dim redCore As New RedCloud_Core
    Dim guided As New GuidedBP_Depth
    Dim matchCell As New RedCloud_MatchCell
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Create the color masks and the pointcloud masks"
    End Sub
    Private Function matchCells(dst As cv.Mat, input As cv.Mat, redCells As List(Of rcData)) As cv.Mat
        matchCell.lastCellMap = input.Clone
        matchCell.lastColors = dst.Clone
        matchCell.usedColors.Clear()
        matchCell.usedColors.Add(black)
        matchCell.lastCells = New List(Of rcData)(redCells)

        redCells.Clear()
        redCells.Add(New rcData)
        dst.SetTo(0)
        Dim tmp As New cv.Mat(input.Size, cv.MatType.CV_8U, 0)
        For Each key In redMasks.sortedCells
            matchCell.rp = key.Value
            matchCell.Run(empty)
            Dim rc = matchCell.rc
            rc.index = redCells.Count
            tmp(rc.rect).SetTo(rc.index, rc.mask)
            dst(rc.rect).SetTo(rc.color, rc.mask)
            redCells.Add(rc)
        Next

        redCells(0).mask = input.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        redCells(0).pixels = redCells(0).mask.CountNonZero

        Return tmp
    End Function
    Public Sub RunVB(src As cv.Mat)
        color.Run(src)
        redMasks.Run(color.dst2)

        dst0 = matchCells(dst2, dst0, colorCells)
        dst2.SetTo(0, colorCells(0).mask)

        Dim cloudInput As New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        Select Case redOptions.depthInputIndex
            Case 0 ' GuidedBP_Depth
                guided.Run(src)
                guided.dst2.CopyTo(cloudInput, task.depthMask)
                redMasks.Run(guided.dst2)
            Case 1 ' RedCloud_Core
                redCore.Run(task.pointCloud)
                redCore.dst2.CopyTo(cloudInput, task.depthMask)
                redMasks.Run(redCore.dst2)
        End Select

        dst1 = matchCells(dst3, dst1, cloudCells)
        dst3.SetTo(0, cloudCells(0).mask)
        dst3.SetTo(0, task.noDepthMask)

        Static redCSelected As Integer
        If task.mouseClickFlag Then
            redCSelected = If(task.mousePicTag = 2, RESULT_DST2, RESULT_DST3)
        End If

        If redCSelected = RESULT_DST2 Then
            setSelectedCell(colorCells, dst0)
        ElseIf redCSelected = RESULT_DST3 Then
            setSelectedCell(cloudCells, dst1)
        End If
    End Sub
End Class








Public Class RedCloud_MasksCombine : Inherits VB_Algorithm
    Dim masks As New RedCloud_MasksBoth
    Public Sub New()
        desc = "Combine the cloud masks using the color masks"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        masks.Run(src)
        dst3 = vbPalette(masks.dst2)

        dst2.SetTo(0)
        For Each rc In masks.cloudCells
            Dim index = masks.dst2.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If index < masks.colorCells.Count Then
                Dim rcX = masks.colorCells.ElementAt(index)
                rc.color = rcX.color
                dst2(rc.rect).SetTo(rc.color, rc.mask)
            End If
        Next
    End Sub
End Class
