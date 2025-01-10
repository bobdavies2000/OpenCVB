Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedColor_Basics : Inherits TaskParent
    Public inputMask As New cv.Mat
    Public cellGen As New Cell_Generate
    Dim redCPP As New RedColor_CPP
    Dim color As New Color8U_Basics
    Public Sub New()
        task.gOptions.setHistogramBins(40)
        inputMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        UpdateAdvice(traceName + ": there are dedicated options for RedCloud algorithms." + vbCrLf +
                        "It is behind the global options (options which affect most algorithms.)")
        desc = "Find cells and then match them to the previous generation with minimum boundary"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then
            color.Run(src)
            src = color.dst2
        End If

        redCPP.inputMask = inputMask
        redCPP.Run(src)

        If redCPP.classCount = 0 Then Exit Sub ' no data to process.
        cellGen.classCount = redCPP.classCount
        cellGen.rectList = redCPP.rectList
        cellGen.floodPoints = redCPP.floodPoints
        cellGen.Run(redCPP.dst2)

        dst1 = cellGen.dst2

        labels(2) = cellGen.labels(2)

        If task.heartBeat Then
            dst2 = dst1.Clone
        Else
            For Each rc In task.redCells
                If rc.motionFlag Then dst2(rc.rect).SetTo(rc.naturalColor, rc.mask)
            Next
        End If

        If task.redOptions.DisplayCellStats.Checked Then
            Static stats As Cell_Basics
            If stats Is Nothing Then
                task.gOptions.setDisplay1()
                stats = New Cell_Basics
            End If
            stats.Run(src)
            strOut = stats.strOut
            SetTrueText(strOut, newPoint, 1)
            dst1 = stats.dst1
        End If

        If standaloneTest() Then
            If task.heartBeat Then
                dst3.SetTo(0)
                For i = 0 To Math.Min(task.redOptions.identifyCount, task.redCells.Count) - 1
                    Dim rc = task.redCells(i)
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                Next
            Else
                Dim count As Integer
                For i = 0 To task.redCells.Count - 1
                    Dim rc = task.redCells(i)
                    If rc.motionFlag Then
                        dst3(rc.rect).SetTo(rc.color, rc.mask)
                        count += 1
                        If count >= task.redOptions.identifyCount Then Exit For
                    End If
                Next
                labels(3) = CStr(count) + " largest cells shown below in the 'tracking' color " +
                            "which changes when the cell is split or lost."
            End If

        End If
    End Sub
End Class







Public Class RedColor_Reduction : Inherits TaskParent
    Public Sub New()
        task.redOptions.ColorSource.SelectedItem() = "Reduction_Basics"
        task.gOptions.setHistogramBins(20)
        desc = "Segment the image based on both the reduced color"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))
        dst3 = task.redMap
    End Sub
End Class







Public Class RedColor_Hulls : Inherits TaskParent
    Dim convex As New Convex_RedCloudDefects
    Public Sub New()
        labels = {"", "Cells where convexity defects failed", "", "Improved contour results using OpenCV's ConvexityDefects"}
        desc = "Add hulls and improved contours using ConvexityDefects to each RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

        dst3.SetTo(0)
        Dim defectCount As Integer
        task.redMap.SetTo(0)
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
                DrawContour(dst3(rc.rect), rc.hull, rc.color, -1)
                DrawContour(task.redMap(rc.rect), rc.hull, rc.index, -1)
            End If
            redCells.Add(rc)
        Next
        task.redCells = New List(Of rcData)(redCells)
        labels(2) = CStr(task.redCells.Count) + " hulls identified below.  " + CStr(defectCount) + " hulls failed to build the defect list."
    End Sub
End Class









Public Class RedColor_FindCells : Inherits TaskParent
    Public cellList As New List(Of Integer)
    Public Sub New()
        task.gOptions.pixelDiffThreshold = 25
        cPtr = RedColor_FindCells_Open()
        desc = "Find all the RedCloud cells touched by the mask created by the Motion_History rectangle"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        cellList = New List(Of Integer)

        dst2 = getRedColor(src, labels(2))

        Dim count As Integer
        dst3.SetTo(0)
        Dim cppData(dst1.Total - 1) As Byte
        Marshal.Copy(dst1.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = RedColor_FindCells_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
        handleSrc.Free()

        count = RedColor_FindCells_TotalCount(cPtr)
        If count = 0 Then Exit Sub

        Dim cellsFound(count - 1) As Integer
        Marshal.Copy(imagePtr, cellsFound, 0, cellsFound.Length)

        cellList = cellsFound.ToList
        dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst0 = dst0.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        For Each index In cellList
            If task.redCells.Count <= index Then Continue For
            Dim rc = task.redCells(index)
            DrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
            dst3(rc.rect).SetTo(rc.naturalColor, rc.mask)
        Next
        labels(3) = CStr(count) + " cells were found using the motion mask"
    End Sub
    Public Sub Close()
        RedColor_FindCells_Close(cPtr)
    End Sub
End Class









'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedColor_Planes : Inherits TaskParent
    Public planes As New RedColor_PlaneColor
    Public Sub New()
        desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        planes.Run(src)
        dst2 = planes.dst2
        dst3 = planes.dst3
        labels = planes.labels
    End Sub
End Class








Public Class RedColor_Equations : Inherits TaskParent
    Dim eq As New Plane_Equation
    Public redCells As New List(Of rcData)
    Public Sub New()
        labels(3) = "The estimated plane equations for the largest 20 RedCloud cells."
        desc = "Show the estimated plane equations for all the cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            dst2 = getRedColor(src, labels(2))
            redCells = New List(Of rcData)(task.redCells)
        End If

        Dim newCells As New List(Of rcData)
        For Each rc As rcData In redCells
            If rc.contour.Count > 4 Then
                eq.rc = rc
                eq.Run(src)
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

        SetTrueText(strOut, 3)
    End Sub
End Class








Public Class RedColor_CellsAtDepth : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Dim kalman As New Kalman_Basics
    Public Sub New()
        plot.removeZeroEntry = False
        labels(3) = "Histogram of depth weighted by the size of the cell."
        desc = "Create a histogram of depth using RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

        Dim histBins = task.histogramBins
        Dim slotList(histBins) As List(Of Integer)
        For i = 0 To slotList.Count - 1
            slotList(i) = New List(Of Integer)
        Next
        Dim hist(histBins - 1) As Single
        For Each rc In task.redCells
            Dim slot As Integer
            If rc.depthMean(2) > task.MaxZmeters Then rc.depthMean(2) = task.MaxZmeters
            slot = CInt((rc.depthMean(2) / task.MaxZmeters) * histBins)
            If slot >= hist.Length Then slot = hist.Length - 1
            slotList(slot).Add(rc.index)
            hist(slot) += rc.pixels
        Next

        kalman.kInput = hist
        kalman.Run(src)

        Dim histMat = cv.Mat.FromPixelData(histBins, 1, cv.MatType.CV_32F, kalman.kOutput)
        plot.Run(histMat)
        dst3 = plot.dst2

        Dim barWidth = dst3.Width / histBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / barWidth)
        If histIndex >= slotList.Count() Then histIndex = slotList.Count() - 1
        dst3.Rectangle(New cv.Rect(CInt(histIndex * barWidth), 0, barWidth, dst3.Height), cv.Scalar.Yellow, task.lineWidth)
        For i = 0 To slotList(histIndex).Count - 1
            Dim rc = task.redCells(slotList(histIndex)(i))
            DrawContour(dst2(rc.rect), rc.contour, cv.Scalar.Yellow)
            DrawContour(task.color(rc.rect), rc.contour, cv.Scalar.Yellow)
        Next
    End Sub
End Class








Public Class RedColor_ShapeCorrelation : Inherits TaskParent
    Public Sub New()
        desc = "A shape correlation is between each x and y in list of contours points.  It allows classification based on angle and shape."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

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

        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class RedColor_FPS : Inherits TaskParent
    Dim fps As New Grid_FPS
    Public Sub New()
        task.gOptions.setDisplay1()
        task.gOptions.setDisplay1()
        desc = "Display RedCloud output at a fixed frame rate"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fps.Run(src)

        If fps.heartBeat Then
            getRedColor(src)
            dst0 = task.color.Clone
            dst1 = task.depthRGB.Clone
            dst2 = task.redC.dst2.Clone
        End If
        labels(2) = task.redC.labels(2) + " " + fps.strOut
    End Sub
End Class








'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedColor_PlaneColor : Inherits TaskParent
    Public options As New Options_Plane
    Dim planeCells As New Plane_CellColor
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        dst2 = getRedColor(src, labels(2))

        dst3.SetTo(0)
        Dim fitPoints As New List(Of cv.Point3f)
        For Each rc In task.redCells
            If rc.eq = newVec4f Then
                rc.eq = New cv.Vec4f
                If options.useMaskPoints Then
                    rc.eq = fitDepthPlane(planeCells.buildMaskPointEq(rc))
                ElseIf options.useContourPoints Then
                    rc.eq = fitDepthPlane(planeCells.buildContourPoints(rc))
                ElseIf options.use3Points Then
                    rc.eq = build3PointEquation(rc)
                End If
            End If
            dst3(rc.rect).SetTo(New cv.Scalar(Math.Abs(255 * rc.eq(0)),
                                              Math.Abs(255 * rc.eq(1)),
                                              Math.Abs(255 * rc.eq(2))), rc.mask)
        Next
    End Sub
End Class






'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedColor_PlaneFromContour : Inherits TaskParent
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation each cell's contour"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then dst2 = getRedColor(src, labels(2))

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
Public Class RedColor_PlaneFromMask : Inherits TaskParent
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation from the pointcloud samples in a RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then dst2 = getRedColor(src, labels(2))

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









Public Class RedColor_BProject3D : Inherits TaskParent
    Dim hcloud As New Hist3Dcloud_Basics
    Public Sub New()
        desc = "Run RedColor_Basics on the output of the RGB 3D backprojection"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hcloud.Run(src)
        dst3 = hcloud.dst3

        dst3.ConvertTo(dst0, cv.MatType.CV_8U)
        dst2 = getRedColor(dst0, labels(2))
    End Sub
End Class









Public Class RedColor_YZ : Inherits TaskParent
    Dim stats As New Cell_Basics
    Public Sub New()
        stats.runRedCloud = True
        desc = "Build horizontal RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.redOptions.YZReduction.Checked = True
        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = stats.dst2
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedColor_XZ : Inherits TaskParent
    Dim stats As New Cell_Basics
    Public Sub New()
        stats.runRedCloud = True
        desc = "Build vertical RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.redOptions.XZReduction.Checked = True
        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = stats.dst2
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedColor_World : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Dim world As New Depth_World
    Public Sub New()
        labels(3) = "Generated pointcloud"
        desc = "Display the output of a generated pointcloud as RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        world.Run(src)
        task.pointCloud = world.dst2

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
        If task.firstPass Then optiBase.FindSlider("RedCloud_Basics Reduction").Value = 1000
    End Sub
End Class







Public Class RedColor_KMeans : Inherits TaskParent
    Dim km As New KMeans_MultiChannel
    Public Sub New()
        labels = {"", "", "KMeans_MultiChannel output", "RedColor_Basics output"}
        desc = "Use RedCloud to identify the regions created by kMeans"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        km.Run(src)
        dst3 = km.dst2

        dst2 = getRedColor(dst3, labels(2))
    End Sub
End Class







Public Class RedColor_LikelyFlatSurfaces : Inherits TaskParent
    Dim verts As New Plane_Basics
    Public vCells As New List(Of rcData)
    Public hCells As New List(Of rcData)
    Public Sub New()
        labels(1) = "RedCloud output"
        desc = "Use the mask for vertical surfaces to identify RedCloud cells that appear to be flat."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        verts.Run(src)

        getRedColor(src)
        dst2.SetTo(0)
        dst3.SetTo(0)

        vCells.Clear()
        hCells.Clear()
        For Each rc In task.redCells
            If rc.depthMean(2) >= task.MaxZmeters Then Continue For
            Dim tmp As cv.Mat = verts.dst2(rc.rect) And rc.mask
            If tmp.CountNonZero / rc.pixels > 0.5 Then
                DrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
                vCells.Add(rc)
            End If
            tmp = verts.dst3(rc.rect) And rc.mask
            Dim count = tmp.CountNonZero
            If count / rc.pixels > 0.5 Then
                DrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
                hCells.Add(rc)
            End If
        Next

        Dim rcX = task.rc
        SetTrueText("mean depth = " + Format(rcX.depthMean(2), "0.0"), 3)
        labels(2) = task.redC.labels(2)
    End Sub
End Class








Public Class RedColor_PlaneEq3D : Inherits TaskParent
    Dim eq As New Plane_Equation
    Public Sub New()
        desc = "If a RedColor cell contains depth then build a plane equation"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

        Dim rc = task.rc
        If rc.maxVec.Z Then
            eq.rc = rc
            eq.Run(src)
            rc = eq.rc
        End If

        dst3.SetTo(0)
        DrawContour(dst3(rc.rect), rc.contour, rc.color, -1)

        SetTrueText(eq.strOut, 3)
    End Sub
End Class











Public Class RedColor_DelaunayGuidedFeatures : Inherits TaskParent
    Dim features As New Feature_Delaunay
    Public Sub New()
        labels(2) = "RedCloud Output of GoodFeature points"
        desc = "Track the feature points using RedCloud."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        features.Run(src)
        dst3 = features.dst3
        labels(3) = features.labels(3)

        dst2 = getRedColor(dst3, labels(2))
    End Sub
End Class








Public Class RedColor_UnstableCells : Inherits TaskParent
    Dim prevList As New List(Of cv.Point)
    Public Sub New()
        labels = {"", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing"}
        desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

        If task.heartBeat Or task.frameCount = 2 Then
            dst1 = dst2.Clone
            dst3.SetTo(0)
        End If

        Dim currList As New List(Of cv.Point)
        For Each rc In task.redCells
            If prevList.Contains(rc.maxDStable) = False Then
                DrawContour(dst1(rc.rect), rc.contour, white, -1)
                DrawContour(dst1(rc.rect), rc.contour, cv.Scalar.Black)
                DrawContour(dst3(rc.rect), rc.contour, white, -1)
            End If
            currList.Add(rc.maxDStable)
        Next

        prevList = New List(Of cv.Point)(currList)
    End Sub
End Class








Public Class RedColor_UnstableHulls : Inherits TaskParent
    Dim prevList As New List(Of cv.Point)
    Public Sub New()
        labels = {"", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing"}
        desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

        If task.heartBeat Or task.frameCount = 2 Then
            dst1 = dst2.Clone
            dst3.SetTo(0)
        End If

        Dim currList As New List(Of cv.Point)
        For Each rc In task.redCells
            rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
            If prevList.Contains(rc.maxDStable) = False Then
                DrawContour(dst1(rc.rect), rc.hull, white, -1)
                DrawContour(dst1(rc.rect), rc.hull, cv.Scalar.Black)
                DrawContour(dst3(rc.rect), rc.hull, white, -1)
            End If
            currList.Add(rc.maxDStable)
        Next

        prevList = New List(Of cv.Point)(currList)
    End Sub
End Class










Public Class RedColor_CellChanges : Inherits TaskParent
    Dim dst2Last = dst2.Clone
    Public Sub New()
        desc = "Count the cells that have changed in a RedCloud generation"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

        dst3 = (dst2 - dst2Last).ToMat

        Dim changedPixels = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero
        Dim changedCells As Integer
        For Each rc As rcData In task.redCells
            If rc.indexLast = 0 Then changedCells += 1
        Next

        dst2Last = dst2.Clone
        If task.heartBeat Then
            labels(2) = "Changed cells = " + Format(changedCells, "000") + " cells or " + Format(changedCells / task.redCells.Count, "0%")
            labels(3) = "Changed pixel total = " + Format(changedPixels / 1000, "0.0") + "k or " + Format(changedPixels / dst2.Total, "0%")
        End If
    End Sub
End Class








Public Class RedColor_FloodPoint : Inherits TaskParent
    Dim stats As New Cell_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        desc = "Verify that floodpoints correctly determine if depth is present."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

        dst1 = task.depthRGB
        For Each rc In task.redCells
            DrawCircle(dst1, rc.floodPoint, task.DotSize, white)
            DrawCircle(dst2, rc.floodPoint, task.DotSize, cv.Scalar.Yellow)
        Next
        stats.Run(src)
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedColor_CellStatsPlot : Inherits TaskParent
    Dim cells As New Cell_BasicsPlot
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        cells.runRedCloud = True
        desc = "Display the stats for the requested cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        cells.Run(src)
        dst1 = cells.dst1
        dst2 = cells.dst2
        labels(2) = cells.labels(2)

        SetTrueText(cells.strOut, 3)
    End Sub
End Class







Public Class RedColor_MostlyColor : Inherits TaskParent
    Public Sub New()
        labels(3) = "Cells that have more than 50% depth data."
        desc = "Identify cells that have more than 50% depth data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

        dst3.SetTo(0)
        For Each rc In task.redCells
            If rc.depthPixels / rc.pixels > 0.5 Then dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next
    End Sub
End Class








Public Class RedColor_OutlineColor : Inherits TaskParent
    Dim outline As New Depth_Outline
    Dim color8U As New Color8U_Basics
    Public Sub New()
        labels(3) = "Color input to RedColor_Basics with depth boundary blocking color connections."
        desc = "Use the depth outline as input to RedColor_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        outline.Run(task.depthMask)

        color8U.Run(src)
        dst1 = color8U.dst2 + 1
        dst1.SetTo(0, outline.dst2)
        dst3 = ShowPalette(dst1 * 255 / color8U.classCount)

        dst2 = getRedColor(dst1, labels(2))
    End Sub
End Class







Public Class RedColor_DepthOutline : Inherits TaskParent
    Dim outline As New Depth_Outline
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Use the Depth_Outline output over time to isolate high quality cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        outline.Run(task.depthMask)

        If task.heartBeat Then dst3.SetTo(0)
        dst3 = dst3 Or outline.dst2

        dst1.SetTo(0)
        src.CopyTo(dst1, Not dst3)
        dst2 = getRedColor(dst1, labels(2))
    End Sub
End Class








Public Class RedColor_MeterByMeter : Inherits TaskParent
    Dim meter As New BackProject_MeterByMeter
    Public Sub New()
        desc = "Run RedCloud meter by meter"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        meter.Run(src)
        dst2 = meter.dst3
        labels(2) = meter.labels(3)

        For i = 0 To task.MaxZmeters

        Next
    End Sub
End Class










Public Class RedColor_FourColor : Inherits TaskParent
    Dim binar4 As New Bin4Way_Regions
    Public Sub New()
        labels(3) = "A 4-way split of the input grayscale image based on brightness"
        desc = "Use RedCloud on a 4-way split based on light to dark in the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        binar4.Run(src)
        dst3 = ShowPalette(binar4.dst2 * 255 / 5)

        dst2 = getRedColor(binar4.dst2, labels(2))
    End Sub
End Class









' https://docs.opencv.org/master/de/d01/samples_2cpp_2connected_components_8cpp-example.html
Public Class RedColor_CCompColor : Inherits TaskParent
    Dim ccomp As New CComp_Both
    Public Sub New()
        desc = "Identify each Connected component as a RedCloud Cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(src)
        dst3 = ccomp.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        labels(3) = ccomp.labels(2)

        dst2 = getRedColor(dst3, labels(2))
    End Sub
End Class










Public Class RedColor_Flippers : Inherits TaskParent
    Public Sub New()
        labels(3) = "Highlighted below are the cells which flipped in color from the previous frame."
        desc = "Identify the 4-way split cells that are flipping between brightness boundaries."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

        Static lastMap As cv.Mat = task.redMap.Clone
        dst2.SetTo(0)
        Dim unMatched As Integer
        Dim unMatchedPixels As Integer
        For Each rc In task.redCells
            Dim lastColor = lastMap.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            If lastColor <> rc.color Then
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                unMatched += 1
                unMatchedPixels += rc.pixels
            End If
        Next
        lastMap = task.redC.dst3.Clone

        If task.heartBeat Then
            labels(3) = "Unmatched to previous frame: " + CStr(unMatched) + " totaling " + CStr(unMatchedPixels) + " pixels."
        End If
    End Sub
End Class






Public Class RedColor_OnlyColorHist3D : Inherits TaskParent
    Dim hColor As New Hist3Dcolor_Basics
    Public Sub New()
        desc = "Use the backprojection of the 3D RGB histogram as input to RedColor_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hColor.Run(src)
        dst2 = hColor.dst3
        labels(2) = hColor.labels(3)

        getRedColor(dst2)
        dst3 = task.redMap
        dst3.SetTo(0, task.noDepthMask)
        labels(3) = task.redC.labels(2)
    End Sub
End Class






Public Class RedColor_OnlyColorAlt : Inherits TaskParent
    Public Sub New()
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        getRedColor(src)
        Dim lastCells As New List(Of rcData)(task.redCells)
        Dim lastMap As cv.Mat = task.redMap.Clone
        Dim lastColors As cv.Mat = dst3.Clone

        Dim newCells As New List(Of rcData)
        task.redMap.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors = New List(Of cv.Scalar)({black})
        Dim unmatched As Integer
        For Each cell In task.redCells
            Dim index = lastMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X)
            If index < lastCells.Count Then
                cell.color = lastColors.Get(Of cv.Vec3b)(cell.maxDist.Y, cell.maxDist.X).ToVec3f
            Else
                unmatched += 1
            End If
            If usedColors.Contains(cell.color) Then
                unmatched += 1
                cell.color = randomCellColor()
            End If
            usedColors.Add(cell.color)

            If task.redMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X) = 0 Then
                cell.index = task.redCells.Count
                newCells.Add(cell)
                task.redMap(cell.rect).SetTo(cell.index, cell.mask)
                dst3(cell.rect).SetTo(cell.color, cell.mask)
            End If
        Next

        task.redCells = New List(Of rcData)(newCells)
        labels(3) = CStr(task.redCells.Count) + " cells were identified."
        labels(2) = task.redC.labels(3) + " " + CStr(unmatched) + " cells were not matched to previous frame."

        If task.redCells.Count > 0 Then dst2 = ShowPalette(lastMap * 255 / task.redCells.Count)
    End Sub
End Class











Public Class RedColor_UnmatchedCount : Inherits TaskParent
    Dim myFrameCount As Integer
    Dim changedCellCounts As New List(Of Integer)
    Dim framecounts As New List(Of Integer)
    Dim frameLoc As New List(Of cv.Point)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Count the unmatched cells and display them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        myFrameCount += 1
        If standalone Then dst2 = getRedColor(src, labels(2))

        Dim unMatchedCells As Integer
        Dim mostlyColor As Integer
        For i = 0 To task.redCells.Count - 1
            Dim rc = task.redCells(i)
            If task.redCells(i).depthPixels / task.redCells(i).pixels < 0.5 Then mostlyColor += 1
            If rc.indexLast <> 0 Then
                Dim val = dst3.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If val = 0 Then
                    dst3(rc.rect).SetTo(255, rc.mask)
                    unMatchedCells += 1
                    frameLoc.Add(rc.maxDist)
                    framecounts.Add(myFrameCount)
                End If
            End If
        Next
        If standaloneTest() Then
            For i = 0 To framecounts.Count - 1
                SetTrueText(CStr(framecounts(i)), frameLoc(i), 2)
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
            labels(2) = CStr(task.redCells.Count) + " cells, unmatched cells = " + CStr(unMatchedCells) + "   " +
                        CStr(mostlyColor) + " cells were mostly color and " + CStr(task.redCells.Count - mostlyColor) + " had depth."
            changedCellCounts.Clear()
        End If
    End Sub
End Class









Public Class RedColor_ContourUpdate : Inherits TaskParent
    Public redCells As New List(Of rcData)
    Public Sub New()
        desc = "For each cell, add a contour if its count is zero."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            dst2 = getRedColor(src, labels(2))
            redCells = task.redCells
        End If

        dst3.SetTo(0)
        For i = 1 To redCells.Count - 1
            Dim rc = redCells(i)
            rc.contour = ContourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            DrawContour(rc.mask, rc.contour, 255, -1)
            redCells(i) = rc
            DrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
        Next
    End Sub
End Class








Public Class RedColor_MaxDist : Inherits TaskParent
    Dim addTour As New RedColor_ContourUpdate
    Public Sub New()
        desc = "Show the maxdist before and after updating the mask with the contour."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

        For Each rc In task.redCells
            DrawCircle(dst2, rc.maxDist, task.DotSize, task.HighlightColor)
        Next

        addTour.redCells = task.redCells
        addTour.Run(src)
        dst3 = addTour.dst3

        For i = 1 To addTour.redCells.Count - 1
            Dim rc = addTour.redCells(i)
            rc.maxDist = GetMaxDist(rc)
            DrawCircle(dst3, rc.maxDist, task.DotSize, task.HighlightColor)
        Next
    End Sub
End Class







Public Class RedColor_Tiers : Inherits TaskParent
    Dim tiers As New Depth_Tiers
    Dim binar4 As New Bin4Way_Regions
    Public Sub New()
        desc = "Use the Depth_TierZ algorithm to create a color-based RedCloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        binar4.Run(src)
        dst1 = ShowPalette((binar4.dst2 * 255 / binar4.classCount).ToMat)

        tiers.Run(src)
        dst3 = tiers.dst3

        dst0 = tiers.dst2 + binar4.dst2
        dst2 = getRedColor(dst0, labels(2))
        labels(3) = tiers.labels(2)
    End Sub
End Class




Public Class RedColor_TiersBinarize : Inherits TaskParent
    Dim tiers As New Depth_Tiers
    Dim binar4 As New Bin4Way_Regions
    Public Sub New()
        desc = "Use the Depth_TierZ with Bin4Way_Regions algorithm to create a color-based RedCloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        binar4.Run(src)

        tiers.Run(src)
        dst2 = tiers.dst2 + binar4.dst2

        dst2 = getRedColor(dst2, labels(2))
    End Sub
End Class










Public Class RedColor_TopX : Inherits TaskParent
    Public options As New Options_TopX
    Public Sub New()
        desc = "Show only the top X cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        getRedColor(src)

        dst2.SetTo(0)
        For Each rc In task.redCells
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            If rc.index > options.topX Then Exit For
        Next
        labels(2) = $"The top {options.topX} RedCloud cells by size."
    End Sub
End Class





Public Class RedColor_TopXHulls : Inherits TaskParent
    Dim topX As New RedColor_TopX
    Public Sub New()
        desc = "Build the hulls for the top X RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        topX.Run(src)
        labels = task.redC.labels

        Dim newCells As New List(Of rcData)
        task.redMap.SetTo(0)
        dst2.SetTo(0)
        For Each rc In task.redCells
            If rc.contour.Count >= 5 Then
                rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
                DrawContour(dst2(rc.rect), rc.hull, rc.color, -1)
                DrawContour(rc.mask, rc.hull, 255, -1)
                task.redMap(rc.rect).SetTo(rc.index, rc.mask)
            End If
            newCells.Add(rc)
            If rc.index > topX.options.topX Then Exit For
        Next

        task.redCells = New List(Of rcData)(newCells)
        task.setSelectedCell()
    End Sub
End Class







Public Class RedColor_Hue : Inherits TaskParent
    Dim hue As New Color8U_Hue
    Public Sub New()
        labels(3) = "Mask of the areas with Hue"
        desc = "Run RedCloud on just the red hue regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hue.Run(src)
        dst3 = hue.dst2

        task.redC.inputMask = Not dst3
        dst2 = getRedColor(src, labels(2))
    End Sub
End Class







Public Class RedColor_GenCellContains : Inherits TaskParent
    Dim flood As New Flood_Basics
    Dim contains As New Flood_ContainedCells
    Public Sub New()
        desc = "Merge cells contained in the top X cells and remove all other cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        flood.Run(src)
        dst3 = flood.dst2
        If task.heartBeat Then Exit Sub
        labels(2) = flood.labels(2)

        contains.Run(src)

        dst2.SetTo(0)
        Dim count = Math.Min(task.redOptions.identifyCount, task.redCells.Count)
        For i = 0 To count - 1
            Dim rc = task.redCells(i)
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            dst2.Rectangle(rc.rect, task.HighlightColor, task.lineWidth)
        Next

        For i = task.redOptions.identifyCount To task.redCells.Count - 1
            Dim rc = task.redCells(i)
            dst2(rc.rect).SetTo(task.redCells(rc.container).color, rc.mask)
        Next
    End Sub
End Class







Public Class RedColor_PlusTiers : Inherits TaskParent
    Dim tiers As New Depth_Tiers
    Dim binar4 As New Bin4Way_Regions
    Public Sub New()
        desc = "Add the depth tiers to the input for RedColor_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        tiers.Run(src)
        binar4.Run(src)
        dst2 = getRedColor(binar4.dst2 + tiers.dst2, labels(2))
    End Sub
End Class








Public Class RedColor_Depth : Inherits TaskParent
    Dim flood As New Flood_Basics
    Public Sub New()
        task.redOptions.UseDepth.Checked = True
        desc = "Create RedCloud output using only depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        flood.Run(src)
        dst2 = flood.dst2
        labels(2) = flood.labels(2)
    End Sub
End Class









Public Class RedColor_Consistent1 : Inherits TaskParent
    Dim redC As New Bin3Way_RedCloud
    Dim diff As New Diff_Basics
    Dim cellmaps As New List(Of cv.Mat)
    Dim cellLists As New List(Of List(Of rcData))
    Dim diffs As New List(Of cv.Mat)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        task.gOptions.pixelDiffThreshold = 1
        desc = "Remove RedCloud results that are inconsistent with the previous frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        diff.Run(task.redMap)
        dst1 = diff.dst2

        cellLists.Add(New List(Of rcData)(task.redCells))
        cellmaps.Add(task.redMap And Not dst1)
        diffs.Add(dst1.Clone)

        task.redCells.Clear()
        task.redCells.Add(New rcData)
        For i = 0 To cellLists.Count - 1
            For Each rc In cellLists(i)
                Dim present As Boolean = True
                For j = 0 To cellmaps.Count - 1
                    Dim val = cellmaps(i).Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                    If val = 0 Then
                        present = False
                        Exit For
                    End If
                Next
                If present Then
                    rc.index = task.redCells.Count
                    task.redCells.Add(rc)
                End If
            Next
        Next

        dst2.SetTo(0)
        task.redMap.SetTo(0)
        For Each rc In task.redCells
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            task.redMap(rc.rect).SetTo(rc.index, rc.mask)
        Next

        For Each mat In diffs
            dst2.SetTo(0, mat)
        Next

        If cellmaps.Count > task.gOptions.FrameHistory.Value Then
            cellmaps.RemoveAt(0)
            cellLists.RemoveAt(0)
            diffs.RemoveAt(0)
        End If
    End Sub
End Class









Public Class RedColor_Consistent2 : Inherits TaskParent
    Dim redC As New Bin3Way_RedCloud
    Dim diff As New Diff_Basics
    Dim cellmaps As New List(Of cv.Mat)
    Dim cellLists As New List(Of List(Of rcData))
    Dim diffs As New List(Of cv.Mat)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        task.gOptions.pixelDiffThreshold = 1
        desc = "Remove RedCloud results that are inconsistent with the previous frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        diff.Run(task.redMap)
        dst1 = diff.dst2

        cellLists.Add(New List(Of rcData)(task.redCells))
        cellmaps.Add(task.redMap And Not dst1)
        diffs.Add(dst1.Clone)

        task.redCells.Clear()
        task.redCells.Add(New rcData)
        For i = 0 To cellLists.Count - 1
            For Each rc In cellLists(i)
                Dim present As Boolean = True
                For j = 0 To cellmaps.Count - 1
                    Dim val = cellmaps(i).Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                    If val = 0 Then
                        present = False
                        Exit For
                    End If
                Next
                If present Then
                    rc.index = task.redCells.Count
                    task.redCells.Add(rc)
                End If
            Next
        Next

        dst2.SetTo(0)
        task.redMap.SetTo(0)
        For Each rc In task.redCells
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            task.redMap(rc.rect).SetTo(rc.index, rc.mask)
        Next

        For Each mat In diffs
            dst2.SetTo(0, mat)
        Next

        If cellmaps.Count > task.gOptions.FrameHistory.Value Then
            cellmaps.RemoveAt(0)
            cellLists.RemoveAt(0)
            diffs.RemoveAt(0)
        End If
    End Sub
End Class









Public Class RedColor_Consistent : Inherits TaskParent
    Dim redC As New Bin3Way_RedCloud
    Dim cellmaps As New List(Of cv.Mat)
    Dim cellLists As New List(Of List(Of rcData))
    Dim lastImage As cv.Mat = redC.dst2.Clone
    Public Sub New()
        desc = "Remove RedCloud results that are inconsistent with the previous frame(s)."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)

        cellLists.Add(New List(Of rcData)(task.redCells))
        cellmaps.Add(task.redMap.Clone)

        Dim newCells As New List(Of rcData)
        newCells.Add(New rcData)
        For Each rc In task.redCells
            Dim maxDStable = rc.maxDStable
            Dim count As Integer = 0
            Dim sizes As New List(Of Integer)
            Dim redData As New List(Of rcData)
            For i = 0 To cellmaps.Count - 1
                Dim index = cellmaps(i).Get(Of Byte)(rc.maxDStable.Y, rc.maxDStable.X)
                If cellLists(i)(index).maxDStable = maxDStable Then
                    count = count + 1
                    sizes.Add(cellLists(i)(index).pixels)
                    redData.Add(cellLists(i)(index))
                Else
                    Exit For
                End If
            Next
            If count = cellmaps.Count Then
                Dim index = sizes.IndexOf(sizes.Max)
                rc = redData(index)
                Dim color = lastImage.Get(Of cv.Vec3b)(rc.maxDStable.Y, rc.maxDStable.X)
                If color <> black Then rc.color = color
                rc.index = newCells.Count
                newCells.Add(rc)
            End If
        Next

        task.redCells = New List(Of rcData)(newCells)
        dst2 = DisplayCells()
        lastImage = dst2.Clone

        If cellmaps.Count > task.gOptions.FrameHistory.Value Then
            cellmaps.RemoveAt(0)
            cellLists.RemoveAt(0)
        End If
    End Sub
End Class







Public Class RedColor_NaturalColor : Inherits TaskParent
    Public Sub New()
        desc = "Display the RedCloud results with the mean color of the cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        getRedColor(src)
        labels(2) = task.redC.labels(2)
        dst2 = DisplayCells()
    End Sub
End Class





Public Class RedColor_ColorAndDepth : Inherits TaskParent
    Dim flood As New Flood_Basics
    Dim floodPC As New Flood_Basics
    Dim colorCells As New List(Of rcData)
    Dim colorMap As New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
    Dim depthCells As New List(Of rcData)
    Dim depthMap As New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
    Dim mousePicTag = task.mousePicTag
    Public Sub New()
        task.redOptions.setIdentifyCells(False)
        desc = "Run Flood_Basics and use the cells to map the depth cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.redOptions.UseColorOnly.Checked = True

        task.redCells = New List(Of rcData)(colorCells)
        task.redMap = colorMap.Clone
        flood.Run(src)
        dst2 = flood.dst2
        colorCells = New List(Of rcData)(task.redCells)
        colorMap = task.redMap.Clone
        labels(2) = flood.labels(2)

        task.redOptions.UseDepth.Checked = True

        task.redCells = New List(Of rcData)(depthCells)
        task.redMap = depthMap.Clone
        floodPC.Run(src)
        dst3 = floodPC.dst2
        depthCells = New List(Of rcData)(task.redCells)
        depthMap = task.redMap.Clone
        labels(3) = floodPC.labels(2)

        If task.mouseClickFlag Then mousePicTag = task.mousePicTag
        Select Case mousePicTag
            Case 1
                ' setSelectedCell()
            Case 2
                task.setSelectedCell(colorCells, colorMap)
            Case 3
                task.setSelectedCell(depthCells, depthMap)
        End Select
        dst2.Rectangle(task.rc.rect, task.HighlightColor, task.lineWidth)
        dst3(task.rc.rect).SetTo(white, task.rc.mask)
    End Sub
End Class








Public Class RedColor_CPP : Inherits TaskParent
    Public inputMask As cv.Mat
    Public classCount As Integer
    Public rectList As New List(Of cv.Rect)
    Public floodPoints As New List(Of cv.Point)
    Public Sub New()
        cPtr = RedColor_Open()
        inputMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Run the C++ RedCloud interface with or without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then
            Static color As Color8U_Basics
            If color Is Nothing Then color = New Color8U_Basics
            color.Run(src)
            src = color.dst2
        End If
        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim maskData(src.Total - 1) As Byte
        Marshal.Copy(inputMask.Data, maskData, 0, maskData.Length)
        Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)

        Dim imagePtr = RedColor_Run(cPtr, handleInput.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleMask.Free()
        handleInput.Free()
        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone

        classCount = RedColor_Count(cPtr)
        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedColor_Rects(cPtr))
        Dim floodPointData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC2, RedColor_FloodPoints(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)
        Dim ptList(classCount * 2) As Integer
        Marshal.Copy(floodPointData.Data, ptList, 0, ptList.Length)

        rectList.Clear()
        For i = 0 To rects.Length - 4 Step 4
            rectList.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
        Next

        floodPoints.Clear()
        For i = 0 To ptList.Length - 2 Step 2
            floodPoints.Add(New cv.Point(ptList(i), ptList(i + 1)))
        Next

        If standalone Then dst3 = ShowPalette(dst2 * 255 / classCount)

        If task.heartBeat Then labels(2) = "CV_8U result with " + CStr(classCount) + " regions."
        If task.heartBeat Then labels(3) = "Palette version of the data in dst2 with " + CStr(classCount) + " regions."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedColor_Close(cPtr)
    End Sub
End Class





Public Class RedColor_MaxDist_CPP : Inherits TaskParent
    Public classCount As Integer
    Public RectList As New List(Of cv.Rect)
    Public floodPoints As New List(Of cv.Point)
    Public maxList As New List(Of Integer)
    Dim color8U As New Color8U_Basics
    Public Sub New()
        cPtr = RedCloudMaxDist_Open()
        desc = "Run the C++ RedCloudMaxDist interface without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then
            color8U.Run(src)
            src = color8U.dst2
        End If

        If task.heartBeat Then maxList.Clear() ' reevaluate all cells.
        Dim maxArray = maxList.ToArray
        Dim handleMaxList = GCHandle.Alloc(maxArray, GCHandleType.Pinned)
        RedCloudMaxDist_SetPoints(cPtr, maxList.Count / 2, handleMaxList.AddrOfPinnedObject())
        handleMaxList.Free()

        Dim imagePtr As IntPtr
        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        imagePtr = RedCloudMaxDist_Run(cPtr, handleInput.AddrOfPinnedObject(), 0, src.Rows, src.Cols)
        handleInput.Free()
        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
        dst3 = ShowPalette(dst2)

        classCount = RedCloudMaxDist_Count(cPtr)
        labels(2) = "CV_8U version with " + CStr(classCount) + " cells."

        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloudMaxDist_Rects(cPtr))
        Dim floodPointData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC2, RedCloudMaxDist_FloodPoints(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)
        Dim ptList(classCount * 2) As Integer
        Marshal.Copy(floodPointData.Data, ptList, 0, ptList.Length)

        For i = 0 To rects.Length - 4 Step 4
            RectList.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
        Next
        For i = 0 To ptList.Length - 2 Step 2
            floodPoints.Add(New cv.Point(ptList(i), ptList(i + 1)))
        Next
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloudMaxDist_Close(cPtr)
    End Sub
End Class






Public Class RedColor_FeatureLessReduce : Inherits TaskParent
    Dim devGrid As New FeatureROI_Basics
    Public redCells As New List(Of rcData)
    Public cellMap As New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
    Dim addw As New AddWeighted_Basics
    Dim options As New Options_RedColorEx
    Public Sub New()
        desc = "Remove any cells which are in a featureless region - they are part " +
               "of the neighboring (and often surrounding) region."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        devGrid.Run(src)

        dst2 = getRedColor(src, labels(2))

        dst3.SetTo(0)
        redCells.Clear()
        For Each rc In task.redCells
            Dim tmp = New cv.Mat(rc.mask.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            devGrid.dst3(rc.rect).CopyTo(tmp, rc.mask)
            Dim count = tmp.CountNonZero
            If count / rc.pixels < options.threshold Then
                dst3(rc.rect).SetTo(rc.color, rc.mask)
                rc.index = redCells.Count
                redCells.Add(rc)
                cellMap(rc.rect).SetTo(rc.index, rc.mask)
            End If
        Next

        addw.src2 = devGrid.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        addw.Run(dst2)
        dst2 = addw.dst2

        labels(3) = $"{redCells.Count} cells after removing featureless cells that were part " +
                     "of their surrounding.  " +
                    $"{task.redCells.Count - redCells.Count} were removed."

        task.setSelectedCell()
    End Sub
End Class









Public Class RedColor_Features : Inherits TaskParent
    Dim options As New Options_RedCloudFeatures
    Public Sub New()
        desc = "Display And validate the keyPoints for each RedCloud cell"
    End Sub
    Private Function vbNearFar(factor As Single) As cv.Vec3b
        Dim nearYellow As New cv.Vec3b(255, 0, 0)
        Dim farBlue As New cv.Vec3b(0, 255, 255)
        If Single.IsNaN(factor) Then Return New cv.Vec3b
        If factor > 1 Then factor = 1
        If factor < 0 Then factor = 0
        Return New cv.Vec3b(((1 - factor) * farBlue(0) + factor * nearYellow(0)),
                            ((1 - factor) * farBlue(1) + factor * nearYellow(1)),
                            ((1 - factor) * farBlue(2) + factor * nearYellow(2)))
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        dst2 = getRedColor(src, labels(2))

        Dim rc = task.rc

        dst0 = task.color
        Dim correlationMat As New cv.Mat, correlationXtoZ As Single, correlationYtoZ As Single
        dst3.SetTo(0)
        Select Case options.selection
            Case 0
                Dim pt = rc.maxDist
                dst2.Circle(pt, task.DotSize, task.HighlightColor, -1, cv.LineTypes.AntiAlias)
                labels(3) = "maxDist Is at (" + CStr(pt.X) + ", " + CStr(pt.Y) + ")"
            Case 1
                dst3(rc.rect).SetTo(vbNearFar((rc.depthMean(2)) / task.MaxZmeters), rc.mask)
                labels(3) = "rc.depthMean(2) Is highlighted in dst2"
                labels(3) = "Mean depth for the cell Is " + Format(rc.depthMean(2), fmt3)
            Case 2
                cv.Cv2.MatchTemplate(task.pcSplit(0)(rc.rect), task.pcSplit(2)(rc.rect), correlationMat, cv.TemplateMatchModes.CCoeffNormed, rc.mask)
                correlationXtoZ = correlationMat.Get(Of Single)(0, 0)
                labels(3) = "High correlation X to Z Is yellow, low correlation X to Z Is blue"
            Case 3
                cv.Cv2.MatchTemplate(task.pcSplit(1)(rc.rect), task.pcSplit(2)(rc.rect), correlationMat, cv.TemplateMatchModes.CCoeffNormed, rc.mask)
                correlationYtoZ = correlationMat.Get(Of Single)(0, 0)
                labels(3) = "High correlation Y to Z Is yellow, low correlation Y to Z Is blue"
        End Select
        If options.selection = 2 Or options.selection = 3 Then
            dst3(rc.rect).SetTo(vbNearFar(If(options.selection = 2, correlationXtoZ, correlationYtoZ) + 1), rc.mask)
            SetTrueText("(" + Format(correlationXtoZ, fmt3) + ", " + Format(correlationYtoZ, fmt3) + ")", New cv.Point(rc.rect.X, rc.rect.Y), 3)
        End If
        DrawContour(dst0(rc.rect), rc.contour, cv.Scalar.Yellow)
        SetTrueText(labels(3), 3)
        labels(2) = "Highlighted feature = " + options.labelName
    End Sub
End Class






Public Class RedColor_Gaps : Inherits TaskParent
    Dim frames As New History_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find the gaps that are different in the RedColor_Basics results."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = getRedColor(src, labels(2))

        frames.Run(task.redMap.InRange(0, 0))
        dst3 = frames.dst2

        If task.redCells.Count > 0 Then
            dst2(task.rc.rect).SetTo(white, task.rc.mask)
        End If

        If task.redCells.Count > 0 Then
            Dim rc = task.redCells(0) ' index can now be zero.
            dst3(rc.rect).SetTo(0, rc.mask)
        End If
        Dim count = dst3.CountNonZero
        labels(3) = "Unclassified pixel count = " + CStr(count) + " or " + Format(count / src.Total, "0%")
    End Sub
End Class





Public Class RedColor_Combine : Inherits TaskParent
    Dim color8U As New Color8U_Basics
    Public guided As New GuidedBP_Depth
    Public combinedCells As New List(Of rcData)
    Dim maxDepth As New Depth_MaxMask
    Dim prep As New RedCloud_Basics
    Public Sub New()
        desc = "Combine the color and cloud as indicated in the RedOptions panel."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        maxDepth.Run(src)
        If task.redOptions.UseColorOnly.Checked Or task.redOptions.UseGuidedProjection.Checked Then
            task.redC.inputMask.SetTo(0)
            If src.Channels() = 3 Then
                color8U.Run(src)
                dst2 = color8U.dst2.Clone
            Else
                dst2 = src
            End If
        Else
            task.redC.inputMask = task.noDepthMask
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        End If

        If task.redOptions.UseDepth.Checked Or task.redOptions.UseGuidedProjection.Checked Then
            Select Case task.redOptions.depthInputIndex
                Case 0 ' "GuidedBP_Depth"
                    guided.Run(src)
                    If color8U.classCount > 0 Then guided.dst2 += color8U.classCount
                    guided.dst2.CopyTo(dst2, task.depthMask)
                Case 1 ' "RedCloud_Basics"
                    prep.Run(task.pointCloud)
                    If color8U.classCount > 0 Then prep.dst2 += color8U.classCount
                    prep.dst2.CopyTo(dst2, task.depthMask)
            End Select
        End If

        dst2 = getRedColor(dst2, labels(2))

        combinedCells.Clear()
        Dim drawRectOnlyRun As Boolean
        If task.drawRect.Width * task.drawRect.Height > 10 Then drawRectOnlyRun = True
        For Each rc In task.redCells
            If drawRectOnlyRun Then If task.drawRect.Contains(rc.floodPoint) = False Then Continue For
            combinedCells.Add(rc)
        Next
        labels(2) = CStr(combinedCells.Count) + " cells were found.  Dots indicate maxDist points."
    End Sub
End Class







Public Class RedColor_BrightnessLevel : Inherits TaskParent
    Dim bright As New Brightness_Grid
    Public Sub New()
        desc = "Adjust the brightness so there is no whiteout and then run RedCloud with that."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bright.Run(src)

        dst2 = getRedColor(bright.dst2, labels(2))
        dst3 = task.redC.dst3
    End Sub
End Class







Public Class RedColor_LeftRight : Inherits TaskParent
    Dim redC As New LeftRight_RedCloudBoth
    Public Sub New()
        desc = "Run RedCloud on the left and right images.  Duplicate of LeftRight_RedCloudBoth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        dst3 = redC.dst3
        labels = redC.labels
    End Sub
End Class
