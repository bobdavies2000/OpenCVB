Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedCloud_Basics : Inherits VB_Parent
    Public genCells As New Cell_Generate
    Dim redCPP As New RedCloud_CPP_VB
    Public inputMask As New cvb.Mat
    Dim color As Color8U_Basics
    Public smallCellThreshold As Integer = dst2.Total / 1000
    Public Sub New()
        task.gOptions.setHistogramBins(40)
        task.redOptions.setIdentifyCells(True)
        inputMask = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        UpdateAdvice(traceName + ": there is dedicated panel for RedCloud algorithms." + vbCrLf +
                        "It is behind the global options (which affect most algorithms.)")
        desc = "Find cells and then match them to the previous generation with minimum boundary"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels <> 1 Then
            If color Is Nothing Then color = New Color8U_Basics
            color.Run(src)
            src = color.dst2
        End If
        redCPP.inputMask = inputMask
        redCPP.Run(src)

        If redCPP.classCount = 0 Then Exit Sub ' no data to process.
        genCells.classCount = redCPP.classCount
        genCells.rectList = redCPP.rectList
        genCells.floodPoints = redCPP.floodPoints
        genCells.Run(redCPP.dst2)

        dst2 = genCells.dst2

        labels(2) = genCells.labels(2)

        dst3.SetTo(0)
        Dim cellCount As Integer
        For Each rc In task.redCells
            If rc.pixels > smallCellThreshold Then
                DrawCircle(dst3, rc.maxDist, task.DotSize, task.HighlightColor)
                cellCount += 1
            End If
        Next
        labels(3) = CStr(cellCount) + " RedCloud cells with more than " + CStr(smallCellThreshold) + " pixels.  " + CStr(task.redCells.Count) + " cells present."
    End Sub
End Class






Public Class RedCloud_Reduction : Inherits VB_Parent
    Public redC As New RedCloud_Basics
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        task.redOptions.ColorSource.SelectedItem() = "Reduction_Basics"
        task.gOptions.setHistogramBins(20)
        desc = "Segment the image based on both the reduced color"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst3 = task.cellMap
        dst2 = redC.dst2
        labels = redC.labels
    End Sub
End Class







Public Class RedCloud_Hulls : Inherits VB_Parent
    Dim convex As New Convex_RedCloudDefects
    Public redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "Cells where convexity defects failed", "", "Improved contour results using OpenCV's ConvexityDefects"}
        desc = "Add hulls and improved contours using ConvexityDefects to each RedCloud cell"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        dst3.SetTo(0)
        Dim defectCount As Integer
        task.cellMap.SetTo(0)
        Dim redCells As New List(Of rcData)
        For Each rc In task.redCells
            If rc.contour.Count >= 5 Then
                rc.hull = cvb.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
                Dim hullIndices = cvb.Cv2.ConvexHullIndices(rc.hull.ToArray, False)
                Try
                    Dim defects = cvb.Cv2.ConvexityDefects(rc.contour, hullIndices)
                    rc.contour = convex.betterContour(rc.contour, defects)
                Catch ex As Exception
                    defectCount += 1
                End Try
                DrawContour(dst3(rc.rect), rc.hull, rc.color, -1)
                DrawContour(task.cellMap(rc.rect), rc.hull, rc.index, -1)
            End If
            redCells.Add(rc)
        Next
        task.redCells = New List(Of rcData)(redCells)
        labels(2) = CStr(task.redCells.Count) + " hulls identified below.  " + CStr(defectCount) + " hulls failed to build the defect list."
    End Sub
End Class









Public Class RedCloud_FindCells : Inherits VB_Parent
    Public cellList As New List(Of Integer)
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.redOptions.setIdentifyCells(True)
        task.gOptions.pixelDiffThreshold = 25
        cPtr = RedCloud_FindCells_Open()
        desc = "Find all the RedCloud cells touched by the mask created by the Motion_History rectangle"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        cellList = New List(Of Integer)

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim count As Integer
        dst3.SetTo(0)
        Dim cppData(dst1.Total - 1) As Byte
        Marshal.Copy(dst1.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = RedCloud_FindCells_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
        handleSrc.Free()

        count = RedCloud_FindCells_TotalCount(cPtr)
        If count = 0 Then Exit Sub

        Dim cellsFound(count - 1) As Integer
        Marshal.Copy(imagePtr, cellsFound, 0, cellsFound.Length)

        cellList = cellsFound.ToList
        dst0 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        dst0 = dst0.Threshold(0, 255, cvb.ThresholdTypes.BinaryInv)
        For Each index In cellList
            If task.redCells.Count <= index Then Continue For
            Dim rc = task.redCells(index)
            DrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
            dst3(rc.rect).SetTo(If(task.redOptions.NaturalColor.Checked, rc.naturalColor, cvb.Scalar.White), rc.mask)
        Next
        labels(3) = CStr(count) + " cells were found using the motion mask"
    End Sub
    Public Sub Close()
        RedCloud_FindCells_Close(cPtr)
    End Sub
End Class









'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedCloud_Planes : Inherits VB_Parent
    Public planes As New RedCloud_PlaneColor
    Public Sub New()
        desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        planes.Run(src)
        dst2 = planes.dst2
        dst3 = planes.dst3
        labels = planes.labels
    End Sub
End Class








Public Class RedCloud_Equations : Inherits VB_Parent
    Dim eq As New Plane_Equation
    Public redCells As New List(Of rcData)
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "The estimated plane equations for the largest 20 RedCloud cells."
        desc = "Show the estimated plane equations for all the cells."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            redCells = New List(Of rcData)(task.redCells)
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

        SetTrueText(strOut, 3)
    End Sub
End Class








Public Class RedCloud_CellsAtDepth : Inherits VB_Parent
    Dim plot As New Plot_Histogram
    Dim kalman As New Kalman_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        plot.removeZeroEntry = False
        labels(3) = "Histogram of depth weighted by the size of the cell."
        desc = "Create a histogram of depth using RedCloud cells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

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

        Dim histMat = cvb.Mat.FromPixelData(histBins, 1, cvb.MatType.CV_32F, kalman.kOutput)
        plot.Run(histMat)
        dst3 = plot.dst2

        Dim barWidth = dst3.Width / histBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / barWidth)
        If histIndex >= slotList.Count() Then histIndex = slotList.Count() - 1
        dst3.Rectangle(New cvb.Rect(CInt(histIndex * barWidth), 0, barWidth, dst3.Height), cvb.Scalar.Yellow, task.lineWidth)
        For i = 0 To slotList(histIndex).Count - 1
            Dim rc = task.redCells(slotList(histIndex)(i))
            DrawContour(dst2(rc.rect), rc.contour, cvb.Scalar.Yellow)
            DrawContour(task.color(rc.rect), rc.contour, cvb.Scalar.Yellow)
        Next
    End Sub
End Class








Public Class RedCloud_ShapeCorrelation : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "A shape correlation is between each x and y in list of contours points.  It allows classification based on angle and shape."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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

        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class RedCloud_FPS : Inherits VB_Parent
    Dim fps As New Grid_FPS
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.gOptions.setDisplay1()
        task.gOptions.setDisplay1()
        desc = "Display RedCloud output at a fixed frame rate"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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
Public Class RedCloud_PlaneColor : Inherits VB_Parent
    Public options As New Options_Plane
    Public redC As New RedCloud_Basics
    Dim planeMask As New RedCloud_PlaneFromMask
    Dim planeContour As New RedCloud_PlaneFromContour
    Dim planeCells As New Plane_CellColor
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.motionDetected = False Then Exit Sub
        options.RunOpt()

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        Dim fitPoints As New List(Of cvb.Point3f)
        For Each rc In task.redCells
            If rc.eq = newVec4f Then
                rc.eq = New cvb.Vec4f
                If options.useMaskPoints Then
                    rc.eq = fitDepthPlane(planeCells.buildMaskPointEq(rc))
                ElseIf options.useContourPoints Then
                    rc.eq = fitDepthPlane(planeCells.buildContourPoints(rc))
                ElseIf options.use3Points Then
                    rc.eq = build3PointEquation(rc)
                End If
            End If
            dst3(rc.rect).SetTo(New cvb.Scalar(Math.Abs(255 * rc.eq(0)),
                                              Math.Abs(255 * rc.eq(1)),
                                              Math.Abs(255 * rc.eq(2))), rc.mask)
        Next
    End Sub
End Class






'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedCloud_PlaneFromContour : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation each cell's contour"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        Dim rc = task.rc
        Dim fitPoints As New List(Of cvb.Point3f)
        For Each pt In rc.contour
            If pt.X >= rc.rect.Width Or pt.Y >= rc.rect.Height Then Continue For
            If rc.mask.Get(Of Byte)(pt.Y, pt.X) = 0 Then Continue For
            fitPoints.Add(task.pointCloud(rc.rect).Get(Of cvb.Point3f)(pt.Y, pt.X))
        Next
        rc.eq = fitDepthPlane(fitPoints)
        If standaloneTest() Then
            dst3.SetTo(0)
            dst3(rc.rect).SetTo(New cvb.Scalar(Math.Abs(255 * rc.eq(0)), Math.Abs(255 * rc.eq(1)), Math.Abs(255 * rc.eq(2))), rc.mask)
        End If
    End Sub
End Class







'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedCloud_PlaneFromMask : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation from the pointcloud samples in a RedCloud cell"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End If

        Dim rc = task.rc
        Dim fitPoints As New List(Of cvb.Point3f)
        For y = 0 To rc.rect.Height - 1
            For x = 0 To rc.rect.Width - 1
                If rc.mask.Get(Of Byte)(y, x) Then fitPoints.Add(task.pointCloud(rc.rect).Get(Of cvb.Point3f)(y, x))
            Next
        Next
        rc.eq = fitDepthPlane(fitPoints)
        If standaloneTest() Then
            dst3.SetTo(0)
            dst3(rc.rect).SetTo(New cvb.Scalar(Math.Abs(255 * rc.eq(0)), Math.Abs(255 * rc.eq(1)), Math.Abs(255 * rc.eq(2))), rc.mask)
        End If
    End Sub
End Class









Public Class RedCloud_BProject3D : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim hcloud As New Hist3Dcloud_Basics
    Public Sub New()
        desc = "Run RedCloud_Basics on the output of the RGB 3D backprojection"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        hcloud.Run(src)
        dst3 = hcloud.dst2

        dst3.ConvertTo(dst0, cvb.MatType.CV_8U)
        redC.Run(dst0)
        dst2 = redC.dst2
    End Sub
End Class









Public Class RedCloud_YZ : Inherits VB_Parent
    Dim stats As New Cell_Basics
    Public Sub New()
        stats.runRedCloud = True
        desc = "Build horizontal RedCloud cells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.redOptions.YZReduction.Checked = True
        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = stats.dst2
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_XZ : Inherits VB_Parent
    Dim stats As New Cell_Basics
    Public Sub New()
        stats.runRedCloud = True
        desc = "Build vertical RedCloud cells."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.redOptions.XZReduction.Checked = True
        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = stats.dst2
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_World : Inherits VB_Parent
    Dim redC As New RedCloud_Reduce
    Dim world As New Depth_World
    Public Sub New()
        labels(3) = "Generated pointcloud"
        desc = "Display the output of a generated pointcloud as RedCloud cells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        world.Run(src)
        task.pointCloud = world.dst2

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
        If task.FirstPass Then FindSlider("RedCloud_Reduce Reduction").Value = 1000
    End Sub
End Class







Public Class RedCloud_KMeans : Inherits VB_Parent
    Dim km As New KMeans_MultiChannel
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "KMeans_MultiChannel output", "RedCloud_Basics output"}
        desc = "Use RedCloud to identify the regions created by kMeans"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        km.Run(src)
        dst3 = km.dst2

        redC.Run(km.dst3)
        dst2 = redC.dst2
    End Sub
End Class










Public Class RedCloud_Diff : Inherits VB_Parent
    Dim diff As New Diff_RGBAccum
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "Diff output, RedCloud input", "RedCloud output"}
        desc = "Isolate blobs in the diff output with RedCloud"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        SetTrueText("Wave at the camera to see the segmentation of the motion.", 3)
        diff.Run(src)
        dst3 = diff.dst2

        redC.Run(dst3)
        dst2.SetTo(0)
        redC.dst2.CopyTo(dst2, dst3)

        labels(3) = CStr(task.redCells.Count) + " objects identified in the diff output"
    End Sub
End Class








Public Class RedCloud_ProjectCell : Inherits VB_Parent
    Dim topView As New Hist_ShapeTop
    Dim sideView As New Hist_ShapeSide
    Dim mats As New Mat_4Click
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.gOptions.setDisplay1()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels(3) = "Top: XZ values and mask, Bottom: ZY values and mask"
        desc = "Visualize the top and side projection of a RedCloud cell"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        'labels(2) = redC.labels(2)

        'Dim rc = task.rc

        'Dim pc = New cvb.Mat(rc.rect.Height, rc.rect.Width, cvb.MatType.CV_32FC3, 0)
        'task.pointCloud(rc.rect).CopyTo(pc, rc.mask)

        'topView.rc = rc
        'topView.Run(pc)

        'sideView.rc = rc
        'sideView.Run(pc)

        'mats.mat(0) = topView.dst2
        'mats.mat(1) = topView.dst3
        'mats.mat(2) = sideView.dst2
        'mats.mat(3) = sideView.dst3
        'mats.Run(empty)
        'dst1 = mats.dst2
        'dst3 = mats.dst3

        'Dim padX = dst2.Width / 15
        'Dim padY = dst2.Height / 20
        'strOut = "Top" + vbTab + "Top Mask" + vbCrLf + vbCrLf + "Side" + vbTab + "Side Mask"
        'SetTrueText(strOut, New cvb.Point(dst2.Width / 2 - padX, dst2.Height / 2 - padY), 1)
        'SetTrueText("Select a RedCloud cell above to project it into the top and side views at left.", 3)
    End Sub
End Class







Public Class RedCloud_LikelyFlatSurfaces : Inherits VB_Parent
    Dim verts As New Plane_Basics
    Dim redC As New RedCloud_Basics
    Public vCells As New List(Of rcData)
    Public hCells As New List(Of rcData)
    Public Sub New()
        labels(1) = "RedCloud output"
        desc = "Use the mask for vertical surfaces to identify RedCloud cells that appear to be flat."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        verts.Run(src)

        redC.Run(src)
        dst2.SetTo(0)
        dst3.SetTo(0)

        vCells.Clear()
        hCells.Clear()
        For Each rc In task.redCells
            If rc.depthMean(2) >= task.MaxZmeters Then Continue For
            Dim tmp As cvb.Mat = verts.dst2(rc.rect) And rc.mask
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
        labels(2) = redC.labels(2)
    End Sub
End Class








Public Class RedCloud_PlaneEq3D : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim eq As New Plane_Equation
    Public Sub New()
        desc = "If a RedColor cell contains depth then build a plane equation"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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
        DrawContour(dst3(rc.rect), rc.contour, rc.color, -1)

        SetTrueText(eq.strOut, 3)
    End Sub
End Class











Public Class RedCloud_DelaunayGuidedFeatures : Inherits VB_Parent
    Dim features As New Feature_Delaunay
    Dim redC As New RedCloud_Basics
    Dim goodList As New List(Of List(Of cvb.Point2f))
    Public Sub New()
        labels = {"", "Format CV_8U of Delaunay data", "RedCloud output", "RedCloud Output of GoodFeature points"}
        desc = "Track the GoodFeatures points using RedCloud."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        features.Run(src)
        dst1 = features.dst3

        redC.Run(dst1)
        dst2 = redC.dst2

        If task.heartBeat Then goodList.Clear()

        Dim nextGood As New List(Of cvb.Point2f)(task.features)
        goodList.Add(nextGood)

        If goodList.Count >= task.frameHistoryCount Then goodList.RemoveAt(0)

        dst3.SetTo(0)
        For Each ptList In goodList
            For Each pt In ptList
                Dim c = dst2.Get(Of cvb.Vec3b)(pt.Y, pt.X)
                DrawCircle(dst3, pt, task.DotSize, c)
            Next
        Next
    End Sub
End Class








Public Class RedCloud_UnstableCells : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim prevList As New List(Of cvb.Point)
    Public Sub New()
        labels = {"", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing"}
        desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        If task.heartBeat Or task.frameCount = 2 Then
            dst1 = dst2.Clone
            dst3.SetTo(0)
        End If

        Dim currList As New List(Of cvb.Point)
        For Each rc In task.redCells
            If prevList.Contains(rc.maxDStable) = False Then
                DrawContour(dst1(rc.rect), rc.contour, cvb.Scalar.White, -1)
                DrawContour(dst1(rc.rect), rc.contour, cvb.Scalar.Black)
                DrawContour(dst3(rc.rect), rc.contour, cvb.Scalar.White, -1)
            End If
            currList.Add(rc.maxDStable)
        Next

        prevList = New List(Of cvb.Point)(currList)
    End Sub
End Class








Public Class RedCloud_UnstableHulls : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim prevList As New List(Of cvb.Point)
    Public Sub New()
        labels = {"", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing"}
        desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        If task.heartBeat Or task.frameCount = 2 Then
            dst1 = dst2.Clone
            dst3.SetTo(0)
        End If

        Dim currList As New List(Of cvb.Point)
        For Each rc In task.redCells
            rc.hull = cvb.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
            If prevList.Contains(rc.maxDStable) = False Then
                DrawContour(dst1(rc.rect), rc.hull, cvb.Scalar.White, -1)
                DrawContour(dst1(rc.rect), rc.hull, cvb.Scalar.Black)
                DrawContour(dst3(rc.rect), rc.hull, cvb.Scalar.White, -1)
            End If
            currList.Add(rc.maxDStable)
        Next

        prevList = New List(Of cvb.Point)(currList)
    End Sub
End Class










Public Class RedCloud_CellChanges : Inherits VB_Parent
    Dim redC As Object
    Dim dst2Last = dst2.Clone
    Public Sub New()
        If standaloneTest() Then redC = New RedCloud_Basics
        desc = "Count the cells that have changed in a RedCloud generation"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        dst3 = (dst2 - dst2Last).ToMat

        Dim changedPixels = dst3.CvtColor(cvb.ColorConversionCodes.BGR2GRAY).CountNonZero
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








Public Class RedCloud_FloodPoint : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim stats As New Cell_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        desc = "Verify that floodpoints correctly determine if depth is present."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst1 = task.depthRGB
        For Each rc In task.redCells
            DrawCircle(dst1, rc.floodPoint, task.DotSize, cvb.Scalar.White)
            DrawCircle(dst2, rc.floodPoint, task.DotSize, cvb.Scalar.Yellow)
        Next
        stats.Run(src)
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_CellStatsPlot : Inherits VB_Parent
    Dim cells As New Cell_BasicsPlot
    Public Sub New()
        task.redOptions.setIdentifyCells(True)
        If standaloneTest() Then task.gOptions.setDisplay1()
        cells.runRedCloud = True
        desc = "Display the stats for the requested cell"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        cells.Run(src)
        dst1 = cells.dst1
        dst2 = cells.dst2
        labels(2) = cells.labels(2)

        SetTrueText(cells.strOut, 3)
    End Sub
End Class







Public Class RedCloud_MostlyColor : Inherits VB_Parent
    Public redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Cells that have no depth data."
        desc = "Identify cells that have no depth"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        For Each rc In task.redCells
            If rc.depthPixels > 0 Then dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next
    End Sub
End Class








Public Class RedCloud_OutlineColor : Inherits VB_Parent
    Dim outline As New Depth_Outline
    Dim redC As New RedCloud_Basics
    Dim color8U As New Color8U_Basics
    Public Sub New()
        labels(3) = "Color input to RedCloud_Basics with depth boundary blocking color connections."
        desc = "Use the depth outline as input to RedCloud_Basics"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        outline.Run(task.depthMask)

        color8U.Run(src)
        dst1 = color8U.dst2 + 1
        dst1.SetTo(0, outline.dst2)
        dst3 = ShowPalette(dst1 * 255 / color8U.classCount)

        redC.Run(dst1)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class







Public Class RedCloud_DepthOutline : Inherits VB_Parent
    Dim outline As New Depth_Outline
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        task.redOptions.setUseColorOnly(True)
        desc = "Use the Depth_Outline output over time to isolate high quality cells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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








Public Class RedCloud_MeterByMeter : Inherits VB_Parent
    Dim meter As New BackProject_MeterByMeter
    Public Sub New()
        desc = "Run RedCloud meter by meter"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        meter.Run(src)
        dst2 = meter.dst3
        labels(2) = meter.labels(3)

        For i = 0 To task.MaxZmeters

        Next
    End Sub
End Class










Public Class RedCloud_FourColor : Inherits VB_Parent
    Dim binar4 As New Bin4Way_Regions
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.redOptions.setIdentifyCells(True)
        task.redOptions.setUseColorOnly(True)
        labels(3) = "A 4-way split of the input grayscale image based on brightness"
        desc = "Use RedCloud on a 4-way split based on light to dark in the image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        binar4.Run(src)
        dst3 = ShowPalette(binar4.dst2 * 255 / 5)

        redC.Run(binar4.dst2)
        dst2 = redC.dst2
        labels(2) = redC.labels(3)
    End Sub
End Class









' https://docs.opencvb.org/master/de/d01/samples_2cpp_2connected_components_8cpp-example.html
Public Class RedCloud_CCompColor : Inherits VB_Parent
    Dim ccomp As New CComp_Both
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        desc = "Identify each Connected component as a RedCloud Cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(src)
        dst3 = Convert32f_To_8UC3(ccomp.dst1)
        labels(3) = ccomp.labels(2)

        redC.Run(dst3)
        dst2 = redC.dst2
        labels(2) = redC.labels(3)
    End Sub
End Class










Public Class RedCloud_Cells : Inherits VB_Parent
    Public redC As New RedCloud_Basics
    Public cellmap As New cvb.Mat
    Public redCells As New List(Of rcData)
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        desc = "Create RedCloud output using only color"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        cellmap = task.cellMap
        redCells = task.redCells
    End Sub
End Class









Public Class RedCloud_Flippers : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.redOptions.setIdentifyCells(True)
        task.redOptions.setUseColorOnly(True)
        labels(3) = "Highlighted below are the cells which flipped in color from the previous frame."
        desc = "Identify the 4-way split cells that are flipping between brightness boundaries."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst3 = redC.dst3
        labels(3) = redC.labels(2)

        Static lastMap As cvb.Mat = task.cellMap.Clone
        dst2.SetTo(0)
        Dim unMatched As Integer
        Dim unMatchedPixels As Integer
        For Each cell In task.redCells
            Dim lastColor = lastMap.Get(Of cvb.Vec3b)(cell.maxDist.Y, cell.maxDist.X)
            If lastColor <> cell.color Then
                dst2(cell.rect).SetTo(cell.color, cell.mask)
                unMatched += 1
                unMatchedPixels += cell.pixels
            End If
        Next
        lastMap = redC.dst3.Clone

        If task.heartBeat Then
            labels(3) = "Unmatched to previous frame: " + CStr(unMatched) + " totaling " + CStr(unMatchedPixels) + " pixels."
        End If
    End Sub
End Class







Public Class RedCloud_Overlaps : Inherits VB_Parent
    Public redCells As New List(Of rcData)
    Public cellMap As New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Remove the overlapping cells.  Keep the largest."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels = redC.labels

        Dim overlappingCells As New List(Of Integer)
        cellMap.SetTo(0)
        redCells.Clear()
        For Each rc In task.redCells
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

        labels(3) = "Before removing overlapping cells: " + CStr(task.redCells.Count) + ". After: " + CStr(redCells.Count)
    End Sub
End Class







Public Class RedCloud_OnlyColorHist3D : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim hColor As New Hist3Dcolor_Basics
    Public Sub New()
        desc = "Use the backprojection of the 3D RGB histogram as input to RedCloud_Basics."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        hColor.Run(src)
        dst2 = hColor.dst3
        labels(2) = hColor.labels(3)

        redC.Run(dst2)
        dst3 = task.cellMap
        dst3.SetTo(0, task.noDepthMask)
        labels(3) = redC.labels(2)
    End Sub
End Class






Public Class RedCloud_OnlyColorAlt : Inherits VB_Parent
    Public redMasks As New RedCloud_Basics
    Public Sub New()
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redMasks.Run(src)
        Dim lastCells As New List(Of rcData)(task.redCells)
        Dim lastMap As cvb.Mat = task.cellMap.Clone
        Dim lastColors As cvb.Mat = dst3.Clone

        Dim newCells As New List(Of rcData)
        task.cellMap.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors = New List(Of cvb.Vec3b)({black})
        Dim unmatched As Integer
        For Each cell In task.redCells
            Dim index = lastMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X)
            If index < lastCells.Count Then
                cell.color = lastColors.Get(Of cvb.Vec3b)(cell.maxDist.Y, cell.maxDist.X)
            Else
                unmatched += 1
            End If
            If usedColors.Contains(cell.color) Then
                unmatched += 1
                cell.color = randomCellColor()
            End If
            usedColors.Add(cell.color)

            If task.cellMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X) = 0 Then
                cell.index = task.redCells.Count
                newCells.Add(cell)
                task.cellMap(cell.rect).SetTo(cell.index, cell.mask)
                dst3(cell.rect).SetTo(cell.color, cell.mask)
            End If
        Next

        task.redCells = New List(Of rcData)(newCells)
        labels(3) = CStr(task.redCells.Count) + " cells were identified.  The top " + CStr(task.redOptions.identifyCount) + " are numbered"
        labels(2) = redMasks.labels(3) + " " + CStr(unmatched) + " cells were not matched to previous frame."

        If task.redCells.Count > 0 Then dst2 = ShowPalette(lastMap * 255 / task.redCells.Count)
    End Sub
End Class







Public Class RedCloud_Gaps : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim frames As New History_Basics
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Find the gaps that are different in the RedCloud_Basics results."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(3)

        frames.Run(task.cellMap.InRange(0, 0))
        dst3 = frames.dst2

        If task.redCells.Count > 0 Then
            dst2(task.rc.rect).SetTo(cvb.Scalar.White, task.rc.mask)
        End If

        If task.redCells.Count > 0 Then
            Dim rc = task.redCells(0) ' index can now be zero.
            dst3(rc.rect).SetTo(0, rc.mask)
        End If
        Dim count = dst3.CountNonZero
        labels(3) = "Unclassified pixel count = " + CStr(count) + " or " + Format(count / src.Total, "0%")
    End Sub
End Class







Public Class RedCloud_SizeOrder : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        UpdateAdvice(traceName + ": Use the goptions 'DebugSlider' to select which cell is isolated.")
        task.gOptions.setDebugSlider(0)
        desc = "Select blobs by size using the DebugSlider in the global options"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        SetTrueText("Use the goptions 'DebugSlider' to select cells by size." + vbCrLf + "Size order changes frequently.", 3)
        redC.Run(src)
        dst2 = redC.dst3
        labels(2) = redC.labels(3)

        Dim index = task.gOptions.DebugSliderValue
        If index < task.redCells.Count Then
            dst3.SetTo(0)
            Dim cell = task.redCells(index)
            dst3(cell.rect).SetTo(cell.color, cell.mask)
        End If
    End Sub
End Class






Public Class RedCloud_StructuredH : Inherits VB_Parent
    Dim motion As New RedCloud_MotionBGsubtract
    Dim transform As New Structured_TransformH
    Dim histTop As New Projection_HistTop
    Public Sub New()
        If standalone Then
            task.redOptions.setIdentifyCells(False)
            task.gOptions.setDisplay1()
            task.gOptions.setDisplay1()
        End If
        desc = "Display the RedCloud cells found with a horizontal slice through the cellMap."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim sliceMask = transform.createSliceMaskH()
        dst0 = src

        motion.Run(sliceMask.Clone)

        If task.heartBeat Then dst1.SetTo(0)
        dst1.SetTo(cvb.Scalar.White, sliceMask)
        labels = motion.labels

        dst2.SetTo(0)
        For Each rc In motion.redCells
            If rc.motionFlag Then DrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
        Next

        Dim pc As New cvb.Mat(task.pointCloud.Size(), cvb.MatType.CV_32FC3, 0)
        task.pointCloud.CopyTo(pc, dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))
        histTop.Run(pc)
        dst3 = histTop.dst2

        dst2.SetTo(cvb.Scalar.White, sliceMask)
        dst0.SetTo(cvb.Scalar.White, sliceMask)
    End Sub
End Class






Public Class RedCloud_StructuredV : Inherits VB_Parent
    Dim motion As New RedCloud_MotionBGsubtract
    Dim transform As New Structured_TransformV
    Dim histSide As New Projection_HistSide
    Public Sub New()
        If standalone Then
            task.redOptions.setIdentifyCells(False)
            task.gOptions.setDisplay1()
            task.gOptions.setDisplay1()
        End If
        desc = "Display the RedCloud cells found with a vertical slice through the cellMap."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim sliceMask = transform.createSliceMaskV()
        dst0 = src

        motion.Run(sliceMask.Clone)

        If task.heartBeat Then dst1.SetTo(0)
        dst1.SetTo(cvb.Scalar.White, sliceMask)
        labels = motion.labels
        SetTrueText("Move mouse in image to see impact.", 3)

        dst2.SetTo(0)
        For Each rc In motion.redCells
            If rc.motionFlag Then DrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
        Next

        Dim pc As New cvb.Mat(task.pointCloud.Size(), cvb.MatType.CV_32FC3, 0)
        task.pointCloud.CopyTo(pc, dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))
        histSide.Run(pc)
        dst3 = histSide.dst2

        dst2.SetTo(cvb.Scalar.White, sliceMask)
        dst0.SetTo(cvb.Scalar.White, sliceMask)
    End Sub
End Class






Public Class RedCloud_MotionBasics : Inherits VB_Parent
    Public redMasks As New RedCloud_Basics
    Public redCells As New List(Of rcData)
    Public rMotion As New RedCloud_MotionBGsubtract
    Dim lastColors = dst3.Clone
    Dim lastMap As cvb.Mat = dst2.Clone
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels = {"", "Mask of active RedCloud cells", "CV_8U representation of redCells", ""}
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redMasks.Run(src)

        rMotion.Run(task.color.Clone)

        Dim lastCells As New List(Of rcData)(redCells)

        redCells.Clear()
        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors = New List(Of cvb.Vec3b)({black})
        Dim motionCount As Integer
        For Each cell In rMotion.redCells
            Dim index = lastMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X)
            If cell.motionFlag = False Then
                If index > 0 And index < lastCells.Count Then cell = lastCells(index - 1)
            Else
                motionCount += 1
            End If

            If index > 0 And index < lastCells.Count Then
                cell.color = lastColors.Get(Of cvb.Vec3b)(cell.maxDist.Y, cell.maxDist.X)
            End If
            If usedColors.Contains(cell.color) Then cell.color = randomCellColor()
            usedColors.Add(cell.color)

            If dst2.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X) = 0 Then
                cell.index = redCells.Count + 1
                redCells.Add(cell)
                dst2(cell.rect).SetTo(cell.index, cell.mask)
                dst3(cell.rect).SetTo(cell.color, cell.mask)

                SetTrueText(CStr(cell.index), cell.maxDist, 2)
                SetTrueText(CStr(cell.index), cell.maxDist, 3)
            End If
        Next

        labels(3) = "There were " + CStr(redCells.Count) + " collected cells and " + CStr(motionCount) +
                            " cells removed because of motion.  "

        lastColors = dst3.Clone
        lastMap = dst2.Clone
        If redCells.Count > 0 Then dst1 = ShowPalette(lastMap * 255 / redCells.Count)
    End Sub
End Class








Public Class RedCloud_ContourVsFeatureLess : Inherits VB_Parent
    Dim redMasks As New RedCloud_Basics
    Dim contour As New Contour_WholeImage
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "Contour_WholeImage Input", "RedCloud_Basics - toggling between Contour and Featureless inputs",
                  "FeatureLess_Basics Input"}
        desc = "Compare Contour_WholeImage and FeatureLess_Basics as input to RedCloud_Basics"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static useContours = FindRadio("Use Contour_WholeImage")

        contour.Run(src)
        dst1 = contour.dst2

        fLess.Run(src)
        dst3 = fLess.dst2

        If task.toggleOnOff Then redMasks.Run(dst3) Else redMasks.Run(dst1)
        dst2 = redMasks.dst3
    End Sub
End Class









Public Class RedCloud_UnmatchedCount : Inherits VB_Parent
    Public redCells As New List(Of rcData)
    Dim myFrameCount As Integer
    Dim changedCellCounts As New List(Of Integer)
    Dim framecounts As New List(Of Integer)
    Dim frameLoc As New List(Of cvb.Point)
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Count the unmatched cells and display them."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        myFrameCount += 1
        If standaloneTest() Then
            SetTrueText("RedCloud_UnmatchedCount has no output when run standaloneTest()." + vbCrLf +
                        "It requires redCells and RedCloud_Basics is the only way to create redCells." + vbCrLf +
                        "Since RedCloud_Basics calls RedCloud_UnmatchedCount, it would be circular and never finish the initialize.")
            Exit Sub
        End If

        Dim unMatchedCells As Integer
        Dim mostlyColor As Integer
        For i = 0 To redCells.Count - 1
            Dim rc = redCells(i)
            If redCells(i).depthPixels / redCells(i).pixels < 0.5 Then mostlyColor += 1
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
        If ShowIntermediate() Then
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
            labels(2) = CStr(redCells.Count) + " cells, unmatched cells = " + CStr(unMatchedCells) + "   " +
                        CStr(mostlyColor) + " cells were mostly color and " + CStr(redCells.Count - mostlyColor) + " had depth."
            changedCellCounts.Clear()
        End If
    End Sub
End Class









Public Class RedCloud_ContourUpdate : Inherits VB_Parent
    Public redCells As New List(Of rcData)
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "For each cell, add a contour if its count is zero."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            labels = redC.labels
            redCells = task.redCells
        End If

        dst3.SetTo(0)
        For i = 1 To redCells.Count - 1
            Dim rc = redCells(i)
            rc.contour = ContourBuild(rc.mask, cvb.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            DrawContour(rc.mask, rc.contour, 255, -1)
            redCells(i) = rc
            DrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
        Next
    End Sub
End Class








Public Class RedCloud_MaxDist : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim addTour As New RedCloud_ContourUpdate
    Public Sub New()
        desc = "Show the maxdist before and after updating the mask with the contour."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels = redC.labels

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







Public Class RedCloud_Tiers : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim tiers As New Depth_Tiers
    Dim binar4 As New Bin4Way_Regions
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        desc = "Use the Depth_TierZ algorithm to create a color-based RedCloud"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        binar4.Run(src)
        dst1 = ShowPalette((binar4.dst2 * 255 / binar4.classCount).ToMat)

        tiers.Run(src)
        dst3 = tiers.dst3

        dst0 = tiers.dst2 + binar4.dst2
        redC.Run(dst0)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class




Public Class RedCloud_TiersBinarize : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim tiers As New Depth_Tiers
    Dim binar4 As New Bin4Way_Regions
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        desc = "Use the Depth_TierZ with Bin4Way_Regions algorithm to create a color-based RedCloud"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        binar4.Run(src)

        tiers.Run(src)
        dst2 = tiers.dst2 + binar4.dst2

        redC.Run(dst2)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class








Public Class RedCloud_Combine : Inherits VB_Parent
    Dim color8U As New Color8U_Basics
    Public guided As New GuidedBP_Depth
    Public redMasks As New RedCloud_Basics
    Public combinedCells As New List(Of rcData)
    Dim maxDepth As New Depth_MaxMask
    Dim prep As New RedCloud_Reduce
    Public Sub New()
        desc = "Combined the color and cloud as indicated in the RedOptions panel."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        maxDepth.Run(src)
        If task.redOptions.UseColorOnly.Checked Or task.redOptions.UseGuidedProjection.Checked Then
            redMasks.inputMask.SetTo(0)
            If src.Channels() = 3 Then
                color8U.Run(src)
                dst2 = color8U.dst2.Clone
            Else
                dst2 = src
            End If
        Else
            redMasks.inputMask = task.noDepthMask
            dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        End If

        If task.redOptions.UseDepth.Checked Or task.redOptions.UseGuidedProjection.Checked Then
            Select Case task.redOptions.depthInputIndex
                Case 0 ' "GuidedBP_Depth"
                    guided.Run(src)
                    If color8U.classCount > 0 Then guided.dst2 += color8U.classCount
                    guided.dst2.CopyTo(dst2, task.depthMask)
                Case 1 ' "RedCloud_Reduce"
                    prep.Run(task.pointCloud)
                    If color8U.classCount > 0 Then prep.dst2 += color8U.classCount
                    prep.dst2.CopyTo(dst2, task.depthMask)
            End Select
        End If

        redMasks.Run(dst2)
        dst2 = redMasks.dst2
        dst3 = redMasks.dst3

        combinedCells.Clear()
        Dim drawRectOnlyRun As Boolean
        If task.drawRect.Width * task.drawRect.Height > 10 Then drawRectOnlyRun = True
        For Each rc In task.redCells
            If drawRectOnlyRun Then If task.drawRect.Contains(rc.floodPoint) = False Then Continue For
            combinedCells.Add(rc)
        Next
    End Sub
End Class









Public Class RedCloud_TopX : Inherits VB_Parent
    Public redC As New RedCloud_Basics
    Public options As New Options_TopX
    Public Sub New()
        desc = "Show only the top X cells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        redC.Run(src)

        dst2.SetTo(0)
        For Each rc In task.redCells
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            If rc.index > options.topX Then Exit For
        Next
        labels(2) = $"The top {options.topX} RedCloud cells by size."
    End Sub
End Class







Public Class RedCloud_TopXNeighbors : Inherits VB_Parent
    Dim options As New Options_TopX
    Dim nab As New Neighbors_Precise
    Public Sub New()
        nab.runRedCloud = True
        desc = "Add unused neighbors to each of the top X cells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        nab.Run(src)
        SetTrueText("Review the neighbors_Precise algorithm")

        'If task.heartBeat Then dst2.SetTo(0)
        'cellMap.SetTo(0)
        'Dim tmpMap = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        'redCells.Clear()
        'redCells.Add(nab.redCells(0)) ' placeholder for zero.
        'For i = 1 To options.topX - 1
        '    Dim rc = nab.redCells(i)
        '    tmpMap.SetTo(0)
        '    tmpMap(rc.rect).SetTo(rc.index, rc.mask)
        '    Dim count = 0
        '    For Each index In rc.nabs
        '        Dim rcX = nab.redCells(index)
        '        If rcX.index > options.topX And rcX.depthPixels > 0 Then
        '            Dim val = cellMap.Get(Of Byte)(rcX.maxDStable.Y, rcX.maxDStable.X)
        '            If val = 0 Then
        '                rc.rect = rc.rect.Union(rcX.rect)
        '                tmpMap(rcX.rect).SetTo(rc.index, rcX.mask)
        '                count += 1
        '            End If
        '        End If
        '    Next
        '    rc.mask = New cvb.Mat(rc.rect.Height, rc.rect.Width, cvb.MatType.CV_8U, cvb.Scalar.All(0))
        '    rc.mask = tmpMap(rc.rect).InRange(rc.index, rc.index)
        '    cellMap(rc.rect).SetTo(rc.index, rc.mask)
        '    dst2(rc.rect).SetTo(rc.color, rc.mask)
        '    redCells.Add(rc)
        'Next
        'redCells(0).mask = cellMap.Threshold(0, 255, cvb.ThresholdTypes.BinaryInv)

        'setSelectedContour()
        'If task.rc.index = 0 Then task.rc = redCells(redCells.Count - 1)
        'labels(2) = $"The top {options.topX} RedCloud cells by size."
    End Sub
End Class








Public Class RedCloud_TopXHulls : Inherits VB_Parent
    Dim topX As New RedCloud_TopX
    Public Sub New()
        desc = "Build the hulls for the top X RedCloud cells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        topX.Run(src)
        labels = topX.redC.labels

        Dim newCells As New List(Of rcData)
        task.cellMap.SetTo(0)
        dst2.SetTo(0)
        For Each rc In task.redCells
            If rc.contour.Count >= 5 Then
                rc.hull = cvb.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
                DrawContour(dst2(rc.rect), rc.hull, rc.color, -1)
                DrawContour(rc.mask, rc.hull, 255, -1)
                task.cellMap(rc.rect).SetTo(rc.index, rc.mask)
            End If
            newCells.Add(rc)
            If rc.index > topX.options.topX Then Exit For
        Next

        task.redCells = New List(Of rcData)(newCells)
        task.setSelectedContour()
    End Sub
End Class







Public Class RedCloud_Hue : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim hue As New Color8U_Hue
    Public Sub New()
        task.redOptions.setUseColorOnly(True)
        desc = "Run RedCloud on just the red hue regions."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        hue.Run(src)
        dst3 = hue.dst2

        redC.inputMask = Not dst3
        redC.Run(src)
        dst2 = redC.dst2
    End Sub
End Class







Public Class RedCloud_GenCellContains : Inherits VB_Parent
    Dim flood As New Flood_Basics
    Dim contains As New Flood_ContainedCells
    Public Sub New()
        task.redOptions.setIdentifyCells(True)
        desc = "Merge cells contained in the top X cells and remove all other cells."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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







Public Class RedCloud_PlusTiers : Inherits VB_Parent
    Dim tiers As New Depth_Tiers
    Dim binar4 As New Bin4Way_Regions
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Add the depth tiers to the input for RedCloud_Basics."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        tiers.Run(src)
        binar4.Run(src)
        redC.Run(binar4.dst2 + tiers.dst2)
        dst2 = redC.dst2
        labels = redC.labels
    End Sub
End Class








Public Class RedCloud_Depth : Inherits VB_Parent
    Dim flood As New Flood_Basics
    Public Sub New()
        task.redOptions.UseDepth.Checked = True
        desc = "Create RedCloud output using only depth."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        flood.Run(src)
        dst2 = flood.dst2
        labels(2) = flood.labels(2)
    End Sub
End Class









Public Class RedCloud_Consistent1 : Inherits VB_Parent
    Dim redC As New Bin3Way_RedCloud
    Dim diff As New Diff_Basics
    Dim cellmaps As New List(Of cvb.Mat)
    Dim cellLists As New List(Of List(Of rcData))
    Dim diffs As New List(Of cvb.Mat)
    Public Sub New()
        dst1 = New cvb.Mat(dst1.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        task.gOptions.pixelDiffThreshold = 1
        desc = "Remove RedCloud results that are inconsistent with the previous frame."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        diff.Run(task.cellMap)
        dst1 = diff.dst2

        cellLists.Add(New List(Of rcData)(task.redCells))
        cellmaps.Add(task.cellMap And Not dst1)
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
        task.cellMap.SetTo(0)
        For Each rc In task.redCells
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            task.cellMap(rc.rect).SetTo(rc.index, rc.mask)
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









Public Class RedCloud_Consistent2 : Inherits VB_Parent
    Dim redC As New Bin3Way_RedCloud
    Dim diff As New Diff_Basics
    Dim cellmaps As New List(Of cvb.Mat)
    Dim cellLists As New List(Of List(Of rcData))
    Dim diffs As New List(Of cvb.Mat)
    Public Sub New()
        dst1 = New cvb.Mat(dst1.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        task.gOptions.pixelDiffThreshold = 1
        desc = "Remove RedCloud results that are inconsistent with the previous frame."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        diff.Run(task.cellMap)
        dst1 = diff.dst2

        cellLists.Add(New List(Of rcData)(task.redCells))
        cellmaps.Add(task.cellMap And Not dst1)
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
        task.cellMap.SetTo(0)
        For Each rc In task.redCells
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            task.cellMap(rc.rect).SetTo(rc.index, rc.mask)
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









Public Class RedCloud_Consistent : Inherits VB_Parent
    Dim redC As New Bin3Way_RedCloud
    Dim cellmaps As New List(Of cvb.Mat)
    Dim cellLists As New List(Of List(Of rcData))
    Dim lastImage As cvb.Mat = redC.dst2.Clone
    Public Sub New()
        desc = "Remove RedCloud results that are inconsistent with the previous frame(s)."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)

        cellLists.Add(New List(Of rcData)(task.redCells))
        cellmaps.Add(task.cellMap.Clone)

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
                Dim color = lastImage.Get(Of cvb.Vec3b)(rc.maxDStable.Y, rc.maxDStable.X)
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







Public Class RedCloud_NaturalColor : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Display the RedCloud results with the mean color of the cell"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        labels(2) = redC.labels(2)
        dst2 = DisplayCells()
    End Sub
End Class









Public Class RedCloud_MotionBGsubtract : Inherits VB_Parent
    Public bgSub As New BGSubtract_Basics
    Public redCells As New List(Of rcData)
    Dim redC As New RedCloud_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        task.gOptions.pixelDiffThreshold = 25
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Use absDiff to build a mask of cells that changed."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        bgSub.Run(src)
        dst3 = bgSub.dst2

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(3)

        redCells.Clear()
        dst1.SetTo(0)
        For Each rc In task.redCells
            Dim tmp As cvb.Mat = rc.mask And bgSub.dst2(rc.rect)
            If tmp.CountNonZero Then
                dst1(rc.rect).SetTo(rc.color, rc.mask)
                rc.motionFlag = True
            End If
            redCells.Add(rc)
        Next

    End Sub
End Class







Public Class RedCloud_JoinCells : Inherits VB_Parent
    Dim fLess As New FeatureLess_RedCloud
    Public Sub New()
        task.gOptions.setHistogramBins(20)
        labels = {"", "FeatureLess_RedCloud output.", "RedCloud_Basics output", "RedCloud_Basics cells joined by using the color from the FeatureLess_RedCloud cellMap"}
        desc = "Run RedCloud_Basics and use FeatureLess_RedCloud to join cells that are in the same featureless regions."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        fLess.Run(src)
        dst2 = fLess.dst2
        labels(2) = fLess.labels(2)

        dst3.SetTo(0)
        For Each rc In task.redCells
            Dim color = fLess.dst2.Get(Of cvb.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            dst3(rc.rect).SetTo(color, rc.mask)
        Next
    End Sub
End Class








Public Class RedCloud_LeftRight : Inherits VB_Parent
    Dim redC As New Flood_LeftRight
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Placeholder to make it easier to find where left and right images are floodfilled."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst1 = redC.dst1
        dst2 = redC.dst2
        dst3 = redC.dst3
        labels = redC.labels
    End Sub
End Class






Public Class RedCloud_ColorAndDepth : Inherits VB_Parent
    Dim flood As New Flood_Basics
    Dim floodPC As New Flood_Basics
    Dim colorCells As New List(Of rcData)
    Dim colorMap As New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
    Dim depthCells As New List(Of rcData)
    Dim depthMap As New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
    Dim mousePicTag = task.mousePicTag
    Public Sub New()
        task.redOptions.setIdentifyCells(False)
        desc = "Run Flood_Basics and use the cells to map the depth cells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.redOptions.setUseColorOnly(True)
        task.redCells = New List(Of rcData)(colorCells)
        task.cellMap = colorMap.Clone
        flood.Run(src)
        dst2 = flood.dst2
        colorCells = New List(Of rcData)(task.redCells)
        colorMap = task.cellMap.Clone
        labels(2) = flood.labels(2)

        task.redOptions.UseDepth.Checked = True
        task.redCells = New List(Of rcData)(depthCells)
        task.cellMap = depthMap.Clone
        floodPC.Run(src)
        dst3 = floodPC.dst2
        depthCells = New List(Of rcData)(task.redCells)
        depthMap = task.cellMap.Clone
        labels(3) = floodPC.labels(2)

        If task.mouseClickFlag Then mousePicTag = task.mousePicTag
        Select Case mousePicTag
            Case 1
                ' setSelectedContour()
            Case 2
                task.setSelectedContour(colorCells, colorMap)
            Case 3
                task.setSelectedContour(depthCells, depthMap)
        End Select
        dst2.Rectangle(task.rc.rect, task.HighlightColor, task.lineWidth)
        dst3(task.rc.rect).SetTo(cvb.Scalar.White, task.rc.mask)
    End Sub
End Class







Public Class RedCloud_Delaunay : Inherits VB_Parent
    Dim redCPP As New RedCloud_CPP_VB
    Dim delaunay As New Feature_Delaunay
    Dim color As Color8U_Basics
    Public Sub New()
        desc = "Test Feature_Delaunay points after Delaunay contours have been added."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        delaunay.Run(src)
        dst1 = delaunay.dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        If src.Channels <> 1 Then
            If color Is Nothing Then color = New Color8U_Basics
            color.Run(src)
            src = color.dst2
        End If
        redCPP.inputMask = dst1
        redCPP.Run(src)

        dst2 = redCPP.dst2
        labels(2) = redCPP.labels(2)
    End Sub
End Class







Public Class RedCloud_CPP_VB : Inherits VB_Parent
    Public inputMask As cvb.Mat
    Public classCount As Integer
    Public rectList As New List(Of cvb.Rect)
    Public floodPoints As New List(Of cvb.Point)
    Dim color As Color8U_Basics
    Public Sub New()
        inputMask = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        cPtr = RedCloud_Open()
        desc = "Run the C++ RedCloud interface with or without a mask"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels <> 1 Then
            If color Is Nothing Then color = New Color8U_Basics
            color.Run(src)
            src = color.dst2
        End If
        Dim imagePtr As IntPtr
        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim maskData(src.Total - 1) As Byte
        Marshal.Copy(inputMask.Data, maskData, 0, maskData.Length)
        Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)

        imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleMask.Free()
        handleInput.Free()
        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8U, imagePtr).Clone

        classCount = RedCloud_Count(cPtr)
        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cvb.Mat.FromPixelData(classCount, 1, cvb.MatType.CV_32SC4, RedCloud_Rects(cPtr))
        Dim floodPointData = cvb.Mat.FromPixelData(classCount, 1, cvb.MatType.CV_32SC2, RedCloud_FloodPoints(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)
        Dim ptList(classCount * 2) As Integer
        Marshal.Copy(floodPointData.Data, ptList, 0, ptList.Length)

        rectList.Clear()
        For i = 0 To rects.Length - 4 Step 4
            rectList.Add(New cvb.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
        Next

        floodPoints.Clear()
        For i = 0 To ptList.Length - 2 Step 2
            floodPoints.Add(New cvb.Point(ptList(i), ptList(i + 1)))
        Next

        If standalone Then dst3 = ShowPalette(dst2 * 255 / classCount)

        If task.heartBeat Then labels(2) = "CV_8U result with " + CStr(classCount) + " regions."
        If task.heartBeat Then labels(3) = "Palette version of the data in dst2 with " + CStr(classCount) + " regions."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
    End Sub
End Class





Public Class RedCloud_MaxDist_CPP_VB : Inherits VB_Parent
    Public classCount As Integer
    Public RectList As New List(Of cvb.Rect)
    Public floodPoints As New List(Of cvb.Point)
    Public maxList As New List(Of Integer)
    Dim color8U As New Color8U_Basics
    Public Sub New()
        cPtr = RedCloudMaxDist_Open()
        desc = "Run the C++ RedCloudMaxDist interface without a mask"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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
        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8U, imagePtr).Clone
        dst3 = ShowPalette(dst2)

        classCount = RedCloudMaxDist_Count(cPtr)
        labels(2) = "CV_8U version with " + CStr(classCount) + " cells."

        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cvb.Mat.FromPixelData(classCount, 1, cvb.MatType.CV_32SC4, RedCloudMaxDist_Rects(cPtr))
        Dim floodPointData = cvb.Mat.FromPixelData(classCount, 1, cvb.MatType.CV_32SC2, RedCloudMaxDist_FloodPoints(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)
        Dim ptList(classCount * 2) As Integer
        Marshal.Copy(floodPointData.Data, ptList, 0, ptList.Length)

        For i = 0 To rects.Length - 4 Step 4
            RectList.Add(New cvb.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
        Next
        For i = 0 To ptList.Length - 2 Step 2
            floodPoints.Add(New cvb.Point(ptList(i), ptList(i + 1)))
        Next
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloudMaxDist_Close(cPtr)
    End Sub
End Class









Public Class RedCloud_NaturalGray : Inherits VB_Parent
    Dim redC As New RedCloud_Consistent
    Dim options As New Options_RedCloudOther
    Public Sub New()
        desc = "Display the RedCloud results with the mean grayscale value of the cell +- delta"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim rc = task.rc
        Dim val = CInt(0.299 * rc.colorMean(0) + 0.587 * rc.colorMean(1) + 0.114 * rc.colorMean(2))

        dst1 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        dst0 = dst1.InRange(val - options.range, val + options.range)

        Dim color = New cvb.Vec3b(rc.colorMean(0), rc.colorMean(1), rc.colorMean(2))
        dst3.SetTo(0)
        dst3.SetTo(cvb.Scalar.White, dst0)
    End Sub
End Class







Public Class RedCloud_FeatureLessReduce : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim devGrid As New FeatureROI_Basics
    Public redCells As New List(Of rcData)
    Public cellMap As New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
    Dim addw As New AddWeighted_Basics
    Dim options As New Options_RedCloudOther
    Public Sub New()
        desc = "Remove any cells which are in a featureless region - they are part of the neighboring (and often surrounding) region."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        devGrid.Run(src)

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        redCells.Clear()
        For Each rc In task.redCells
            Dim tmp = New cvb.Mat(rc.mask.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
            devGrid.dst3(rc.rect).CopyTo(tmp, rc.mask)
            Dim count = tmp.CountNonZero
            If count / rc.pixels < options.threshold Then
                dst3(rc.rect).SetTo(rc.color, rc.mask)
                rc.index = redCells.Count
                redCells.Add(rc)
                cellMap(rc.rect).SetTo(rc.index, rc.mask)
            End If
        Next

        addw.src2 = devGrid.dst3.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        addw.Run(dst2)
        dst2 = addw.dst2

        labels(3) = $"{redCells.Count} cells after removing featureless cells that were part of their surrounding.  " +
                    $"{task.redCells.Count - redCells.Count} were removed."

        task.setSelectedContour()
    End Sub
End Class









Public Class RedCloud_Features : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim options As New Options_RedCloudFeatures
    Public Sub New()
        desc = "Display And validate the keyPoints for each RedCloud cell"
    End Sub
    Private Function vbNearFar(factor As Single) As cvb.Vec3b
        Dim nearYellow As New cvb.Vec3b(255, 0, 0)
        Dim farBlue As New cvb.Vec3b(0, 255, 255)
        If Single.IsNaN(factor) Then Return New cvb.Vec3b
        If factor > 1 Then factor = 1
        If factor < 0 Then factor = 0
        Return New cvb.Vec3b(((1 - factor) * farBlue(0) + factor * nearYellow(0)),
                            ((1 - factor) * farBlue(1) + factor * nearYellow(1)),
                            ((1 - factor) * farBlue(2) + factor * nearYellow(2)))
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        redC.Run(src)
        dst2 = redC.dst2

        Dim rc = task.rc

        dst0 = task.color
        Dim correlationMat As New cvb.Mat, correlationXtoZ As Single, correlationYtoZ As Single
        dst3.SetTo(0)
        Select Case options.selection
            Case 0
                Dim pt = rc.maxDist
                dst2.Circle(pt, task.DotSize, task.HighlightColor, -1, cvb.LineTypes.AntiAlias)
                labels(3) = "maxDist Is at (" + CStr(pt.X) + ", " + CStr(pt.Y) + ")"
            Case 1
                dst3(rc.rect).SetTo(vbNearFar((rc.depthMean(2)) / task.MaxZmeters), rc.mask)
                labels(3) = "rc.depthMean(2) Is highlighted in dst2"
                labels(3) = "Mean depth for the cell Is " + Format(rc.depthMean(2), fmt3)
            Case 2
                cvb.Cv2.MatchTemplate(task.pcSplit(0)(rc.rect), task.pcSplit(2)(rc.rect), correlationMat, cvb.TemplateMatchModes.CCoeffNormed, rc.mask)
                correlationXtoZ = correlationMat.Get(Of Single)(0, 0)
                labels(3) = "High correlation X to Z Is yellow, low correlation X to Z Is blue"
            Case 3
                cvb.Cv2.MatchTemplate(task.pcSplit(1)(rc.rect), task.pcSplit(2)(rc.rect), correlationMat, cvb.TemplateMatchModes.CCoeffNormed, rc.mask)
                correlationYtoZ = correlationMat.Get(Of Single)(0, 0)
                labels(3) = "High correlation Y to Z Is yellow, low correlation Y to Z Is blue"
        End Select
        If options.selection = 2 Or options.selection = 3 Then
            dst3(rc.rect).SetTo(vbNearFar(If(options.selection = 2, correlationXtoZ, correlationYtoZ) + 1), rc.mask)
            SetTrueText("(" + Format(correlationXtoZ, fmt3) + ", " + Format(correlationYtoZ, fmt3) + ")", New cvb.Point(rc.rect.X, rc.rect.Y), 3)
        End If
        DrawContour(dst0(rc.rect), rc.contour, cvb.Scalar.Yellow)
        SetTrueText(labels(3), 3)
        labels(2) = "Highlighted feature = " + options.labelName
    End Sub
End Class







Public Class RedCloud_Reduce : Inherits VB_Parent
    Public classCount As Integer
    Dim options As New Options_RedCloudOther
    Public Sub New()
        task.redOptions.UseDepth.Checked = True
        desc = "Reduction transform for the point cloud"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        task.pointCloud.ConvertTo(dst0, cvb.MatType.CV_32S, 1000 / options.reduceAmt)

        Dim split = dst0.Split()

        Select Case task.redOptions.PointCloudReduction
            Case 0 ' "X Reduction"
                dst0 = (split(0) * options.reduceAmt).ToMat
            Case 1 ' "Y Reduction"
                dst0 = (split(1) * options.reduceAmt).ToMat
            Case 2 ' "Z Reduction"
                dst0 = (split(2) * options.reduceAmt).ToMat
            Case 3 ' "XY Reduction"
                dst0 = (split(0) * options.reduceAmt + split(1) * options.reduceAmt).ToMat
            Case 4 ' "XZ Reduction"
                dst0 = (split(0) * options.reduceAmt + split(2) * options.reduceAmt).ToMat
            Case 5 ' "YZ Reduction"
                dst0 = (split(1) * options.reduceAmt + split(2) * options.reduceAmt).ToMat
            Case 6 ' "XYZ Reduction"
                dst0 = (split(0) * options.reduceAmt + split(1) * options.reduceAmt + split(2) * options.reduceAmt).ToMat
        End Select

        Dim mm As mmData = GetMinMax(dst0)
        dst0 = (dst0 - mm.minVal)
        dst2 = dst0 * 255 / (mm.maxVal - mm.minVal)
        dst2.ConvertTo(dst2, cvb.MatType.CV_8U)
        mm = GetMinMax(dst0)

        labels(2) = task.redOptions.PointCloudReductionLabel + " with reduction factor = " + CStr(options.reduceAmt)
    End Sub
End Class






Public Class RedCloud_ReduceHist : Inherits VB_Parent
    Dim reduce As New RedCloud_Reduce
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        desc = "Display the histogram of the RedCloud_Reduce output"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduce.Run(src)
        dst2 = reduce.dst2
        Dim mm = GetMinMax(dst2, task.depthMask)
        plot.minRange = mm.minVal
        plot.maxRange = mm.maxVal
        plot.Run(dst2)
        dst3 = plot.dst2
        labels(2) = reduce.labels(2)
    End Sub
End Class






Public Class RedCloud_ReduceTest : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim redInput As New RedCloud_Reduce
    Public Sub New()
        desc = "Run RedCloud with the depth reduction."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redInput.Run(src)

        redC.Run(redInput.dst2)
        dst2 = redC.dst2
    End Sub
End Class







Public Class RedCloud_MotionCompare : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Compare motion only vs full image RedCloud"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        src.SetTo(0, task.noMotionMask)
        redC.Run(src)
        If task.heartBeatLT Then dst3.SetTo(0)
        redC.dst2.CopyTo(dst3, task.motionMask)
    End Sub
End Class







Public Class RedCloud_CellFLess : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim edges As New LowRes_Edges
    Dim tiers As New Depth_Tiers
    Public classCount As Integer
    Public Sub New()
        dst0 = New cvb.Mat(dst0.Size, cvb.MatType.CV_8U, 0)
        desc = "Isolate the featureless regions in the image by cell and depth."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        tiers.Run(src)

        edges.Run(src)
        labels(2) = edges.labels(2)

        dst1 = tiers.dst2 + task.fLessMask * 1 / 255
        redC.Run(dst1)
        dst2 = redC.dst2

        dst3 = redC.dst2
        dst3.SetTo(0, task.featureMask)
        labels(3) = CStr(task.fLessRects.Count) + " featureless cells were found."
    End Sub
End Class







Public Class RedCloud_CellFeatures : Inherits VB_Parent
    Dim redC As New RedCloud_Basics
    Dim edges As New LowRes_Edges
    Dim tiers As New Depth_Tiers
    Public classCount As Integer
    Public Sub New()
        desc = "Isolate the featureless regions in the image by cell and depth."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        tiers.Run(src)

        edges.Run(src)
        labels(2) = edges.labels(2)

        src.SetTo(0, task.fLessMask)

        redC.Run(src)
        dst2 = redC.dst2
        dst3 = redC.dst2
        dst3.SetTo(0, task.fLessMask)
        labels(3) = CStr(task.featureRects.Count) + " feature cells were found."
    End Sub
End Class