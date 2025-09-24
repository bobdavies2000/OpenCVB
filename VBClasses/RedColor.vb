Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedColor_Basics : Inherits TaskParent
    Public inputRemoved As cv.Mat
    Public cellGen As New RedCell_Generate
    Public redMask As New RedMask_Basics
    Public rcList As New List(Of rcData)
    Public rcMap As cv.Mat ' redColor map
    Public Sub New()
        If task.contours Is Nothing Then task.contours = New Contour_Basics_List
        rcMap = New cv.Mat(New cv.Size(dst2.Width, dst2.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find cells and then match them to the previous generation with minimum boundary"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_8U Then
            If standalone Or task.gOptions.ColorSource.SelectedItem = "EdgeLine_Basics" Then
                dst1 = task.contours.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Else
                dst1 = srcMustBe8U(src)
            End If
        Else
            dst1 = src
        End If

        If inputRemoved IsNot Nothing Then dst1.SetTo(0, inputRemoved)
        redMask.Run(dst1)

        If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
        cellGen.mdList = redMask.mdList
        cellGen.Run(redMask.dst2)

        dst2 = cellGen.dst2

        For Each rc In task.redC.rcList
            DrawCircle(dst2, rc.maxDStable)
        Next
        labels(2) = cellGen.labels(2)
        labels(3) = ""
        SetTrueText("", newPoint, 1)
        task.setSelectedCell()
    End Sub
End Class










Public Class RedColor_FindCells : Inherits TaskParent
    Public bricks As New List(Of Integer)
    Public Sub New()
        task.gOptions.pixelDiffThreshold = 25
        cPtr = RedColor_FindBricks_Open()
        desc = "Find all the RedCloud cells touched by the mask created by the Motion_History rectangle"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks = New List(Of Integer)

        dst2 = runRedOld(src, labels(2))

        Dim cppData(task.redC.rcMap.Total - 1) As Byte
        Marshal.Copy(task.redC.rcMap.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = RedColor_FindBricks_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
        handleSrc.Free()

        Dim count = RedColor_FindBricks_TotalCount(cPtr)
        If count = 0 Then Exit Sub

        Dim cellsFound(count - 1) As Integer
        Marshal.Copy(imagePtr, cellsFound, 0, cellsFound.Length)

        bricks = cellsFound.ToList
        dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst0 = dst0.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        dst3.SetTo(0)
        For Each index In bricks
            If task.redC.rcList.Count <= index Then Continue For
            Dim rc = task.redC.rcList(index)
            DrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
            dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next
        labels(3) = CStr(count) + " cells were found using the motion mask"
    End Sub
    Public Sub Close()
        RedColor_FindBricks_Close(cPtr)
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
    Public rcList As New List(Of rcData)
    Public Sub New()
        labels(3) = "The estimated plane equations for the largest 20 RedCloud cells."
        desc = "Show the estimated plane equations for all the cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            runRedOld(src, labels(2))
            dst2 = task.redC.dst3
            rcList = New List(Of rcData)(task.redC.rcList)
        End If

        Dim newCells As New List(Of rcData)
        For Each rc As rcData In rcList
            If rc.contour.Count > 4 Then
                eq.rc = rc
                eq.Run(src)
                newCells.Add(eq.rc)
            End If
        Next

        rcList = New List(Of rcData)(newCells)

        If task.heartBeat Then
            Dim index As Integer
            strOut = ""
            For Each rc In rcList
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
    Public Sub New()
        task.kalman = New Kalman_Basics
        plot.removeZeroEntry = False
        labels(3) = "Histogram of depth weighted by the size of the cell."
        desc = "Create a histogram of depth using RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedOld(src, labels(2))

        Dim histBins = task.histogramBins
        Dim slotList(histBins) As List(Of Integer)
        For i = 0 To slotList.Count - 1
            slotList(i) = New List(Of Integer)
        Next
        Dim hist(histBins - 1) As Single
        For Each rc In task.redC.rcList
            Dim slot As Integer
            If rc.depth > task.MaxZmeters Then rc.depth = task.MaxZmeters
            slot = CInt((rc.depth / task.MaxZmeters) * histBins)
            If slot >= hist.Length Then slot = hist.Length - 1
            slotList(slot).Add(rc.index)
            hist(slot) += rc.pixels
        Next

        task.kalman.kInput = hist
        task.kalman.Run(emptyMat)

        Dim histMat = cv.Mat.FromPixelData(histBins, 1, cv.MatType.CV_32F, task.kalman.kOutput)
        plot.Run(histMat)
        dst3 = plot.dst2

        Dim barWidth = dst3.Width / histBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / barWidth)
        If histIndex >= slotList.Count() Then histIndex = slotList.Count() - 1
        dst3.Rectangle(New cv.Rect(CInt(histIndex * barWidth), 0, barWidth, dst3.Height), cv.Scalar.Yellow, task.lineWidth)
        For i = 0 To slotList(histIndex).Count - 1
            Dim rc = task.redC.rcList(slotList(histIndex)(i))
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
        dst2 = runRedOld(src, labels(2))

        Dim rc = task.rcD
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
        desc = "Display RedCloud output at a fixed frame rate"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fps.Run(src)

        If fps.heartBeat Then
            dst2 = runRedOld(src, labels(2)).Clone
            labels(2) = task.redC.labels(2) + " " + fps.strOut
        End If
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
        options.Run()

        dst2 = runRedOld(src, labels(2))

        dst3.SetTo(0)
        Dim fitPoints As New List(Of cv.Point3f)
        For Each rc In task.redC.rcList
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
        If standaloneTest() Then dst2 = runRedOld(src, labels(2))

        Dim rc = task.rcD
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
        If standaloneTest() Then dst2 = runRedOld(src, labels(2))

        Dim rc = task.rcD
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
        dst2 = runRedOld(dst0, labels(2))
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

        dst2 = runRedOld(dst3, labels(2))
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
        runRedOld(src, labels(2))
        verts.Run(src)

        dst2.SetTo(0)
        dst3.SetTo(0)

        vCells.Clear()
        hCells.Clear()
        For Each rc In task.redC.rcList
            If rc.depth >= task.MaxZmeters Then Continue For
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

        Dim rcX = task.rcD
        SetTrueText("mean depth = " + Format(rcX.depth, "0.0"), 3)
    End Sub
End Class








Public Class RedColor_PlaneEq3D : Inherits TaskParent
    Dim eq As New Plane_Equation
    Public Sub New()
        desc = "If a RedColor cell contains depth then build a plane equation"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedOld(src, labels(2))

        Dim rc = task.rcD
        If rc.mmZ.maxVal Then
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

        dst2 = runRedOld(dst3, labels(2))
    End Sub
End Class








Public Class RedColor_UnstableCells : Inherits TaskParent
    Dim prevList As New List(Of cv.Point)
    Public Sub New()
        labels = {"", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing"}
        desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedOld(src, labels(2))

        If task.heartBeat Or task.frameCount = 2 Then
            dst1 = dst2.Clone
            dst3.SetTo(0)
        End If

        Dim currList As New List(Of cv.Point)
        For Each rc In task.redC.rcList
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
        dst2 = runRedOld(src, labels(2))

        If task.heartBeat Or task.frameCount = 2 Then
            dst1 = dst2.Clone
            dst3.SetTo(0)
        End If

        Dim currList As New List(Of cv.Point)
        For Each rc In task.redC.rcList
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
        dst2 = runRedOld(src, labels(2))

        dst3 = (dst2 - dst2Last).ToMat

        Dim changedPixels = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero
        Dim changedCells As Integer
        For Each rc As rcData In task.redC.rcList
            If rc.indexLast = 0 Then changedCells += 1
        Next

        dst2Last = dst2.Clone
        If task.heartBeat Then
            labels(2) = "Changed cells = " + Format(changedCells, "000") + " cells or " + Format(changedCells / task.redC.rcList.Count, "0%")
            labels(3) = "Changed pixel total = " + Format(changedPixels / 1000, "0.0") + "k or " + Format(changedPixels / dst2.Total, "0%")
        End If
    End Sub
End Class






Public Class RedColor_CellStatsPlot : Inherits TaskParent
    Dim cells As New RedCell_BasicsPlot
    Public Sub New()
        If standaloneTest() Then task.gOptions.displayDst1.Checked = True
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
        dst2 = runRedOld(src, labels(2))

        dst3.SetTo(0)
        For Each rc In task.redC.rcList
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
        dst3 = ShowPalette(dst1)

        dst2 = runRedOld(dst1, labels(2))
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
        dst2 = runRedOld(dst1, labels(2))
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
        dst3 = ShowPalette(binar4.dst2)

        dst2 = runRedOld(binar4.dst2, labels(2))
    End Sub
End Class









' https://docs.opencv.org/master/de/d01/samples_2cpp_2Region_components_8cpp-example.html
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

        dst2 = runRedOld(dst3, labels(2))
    End Sub
End Class






Public Class RedColor_OnlyColorHist3D : Inherits TaskParent
    Dim hColor As New Hist3Dcolor_Basics
    Public Sub New()
        desc = "Use the backprojection of the 3D RGB histogram as input to RedColor_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        runRedOld(src, labels(3))
        hColor.Run(src)
        dst2 = hColor.dst3
        labels(2) = hColor.labels(3)

        dst3 = task.redC.rcMap
        dst3.SetTo(0, task.noDepthMask)
        labels(3) = task.redC.labels(2)
    End Sub
End Class






Public Class RedColor_OnlyColorAlt : Inherits TaskParent
    Public Sub New()
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        runRedOld(src, labels(3))

        Dim lastCells As New List(Of rcData)(task.redC.rcList)
        Dim lastMap As cv.Mat = task.redC.rcMap.Clone
        Dim lastColors As cv.Mat = dst3.Clone

        Dim newCells As New List(Of rcData)
        task.redC.rcMap.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors = New List(Of cv.Scalar)({black})
        Dim unmatched As Integer
        For Each rc In task.redC.rcList
            Dim index = lastMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If index < lastCells.Count Then
                rc.color = lastColors.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X).ToVec3f
            Else
                unmatched += 1
            End If
            If usedColors.Contains(rc.color) Then
                unmatched += 1
                rc.color = randomCellColor()
            End If
            usedColors.Add(rc.color)

            If task.redC.rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X) = 0 Then
                rc.index = task.redC.rcList.Count
                newCells.Add(rc)
                task.redC.rcMap(rc.rect).SetTo(rc.index, rc.mask)
                dst3(rc.rect).SetTo(rc.color, rc.mask)
            End If
        Next

        task.redC.rcList = New List(Of rcData)(newCells)
        labels(3) = CStr(task.redC.rcList.Count) + " cells were identified."
        labels(2) = task.redC.labels(3) + " " + CStr(unmatched) + " cells were not matched to previous frame."

        If task.redC.rcList.Count > 0 Then dst2 = ShowPalette(lastMap)
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
        If standalone Then dst2 = runRedOld(src, labels(2))

        Dim unMatchedCells As Integer
        Dim mostlyColor As Integer
        For i = 0 To task.redC.rcList.Count - 1
            Dim rc = task.redC.rcList(i)
            If task.redC.rcList(i).depthPixels / task.redC.rcList(i).pixels < 0.5 Then mostlyColor += 1
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
            labels(2) = CStr(task.redC.rcList.Count) + " cells, unmatched cells = " + CStr(unMatchedCells) + "   " +
                        CStr(mostlyColor) + " cells were mostly color and " + CStr(task.redC.rcList.Count - mostlyColor) + " had depth."
            changedCellCounts.Clear()
        End If
    End Sub
End Class









Public Class RedColor_ContourUpdate : Inherits TaskParent
    Public rcList As New List(Of rcData)
    Public Sub New()
        desc = "For each cell, add a contour if its count is zero."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            dst2 = runRedOld(src, labels(2))
            rcList = task.redC.rcList
        End If

        dst3.SetTo(0)
        For i = 1 To rcList.Count - 1
            Dim rc = rcList(i)
            rc.contour = ContourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            DrawContour(rc.mask, rc.contour, 255, -1)
            rcList(i) = rc
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
        dst2 = runRedOld(src, labels(2))

        For Each rc In task.redC.rcList
            DrawCircle(dst2, rc.maxDist, task.DotSize, task.highlight)
        Next

        addTour.rcList = task.redC.rcList
        addTour.Run(src)
        dst3 = addTour.dst3

        For i = 1 To addTour.rcList.Count - 1
            Dim rc = addTour.rcList(i)
            rc.maxDist = GetMaxDist(rc)
            DrawCircle(dst3, rc.maxDist, task.DotSize, task.highlight)
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
        dst1 = ShowPalette(binar4.dst2)

        tiers.Run(src)
        dst3 = tiers.dst3

        dst0 = tiers.dst2 + binar4.dst2
        dst2 = runRedOld(dst0, labels(2))
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

        dst2 = runRedOld(dst2, labels(2))
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

        dst2 = runRedOld(src, labels(2), Not dst3)
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
        For Each rc In task.redC.rcList
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            dst2.Rectangle(rc.rect, task.highlight, task.lineWidth)
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
        dst2 = runRedOld(binar4.dst2 + tiers.dst2, labels(2))
    End Sub
End Class










Public Class RedColor_Consistent : Inherits TaskParent
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

        diff.Run(task.redC.rcMap)
        dst1 = diff.dst2

        cellLists.Add(New List(Of rcData)(task.redC.rcList))
        cellmaps.Add(task.redC.rcMap And Not dst1)
        diffs.Add(dst1.Clone)

        task.redC.rcList.Clear()
        task.redC.rcList.Add(New rcData)
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
                    rc.index = task.redC.rcList.Count
                    task.redC.rcList.Add(rc)
                End If
            Next
        Next

        dst2.SetTo(0)
        task.redC.rcMap.SetTo(0)
        For Each rc In task.redC.rcList
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            task.redC.rcMap(rc.rect).SetTo(rc.index, rc.mask)
        Next

        For Each mat In diffs
            dst2.SetTo(0, mat)
        Next

        If cellmaps.Count > task.frameHistoryCount Then
            cellmaps.RemoveAt(0)
            cellLists.RemoveAt(0)
            diffs.RemoveAt(0)
        End If
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

        diff.Run(task.redC.rcMap)
        dst1 = diff.dst2

        cellLists.Add(New List(Of rcData)(task.redC.rcList))
        cellmaps.Add(task.redC.rcMap And Not dst1)
        diffs.Add(dst1.Clone)

        task.redC.rcList.Clear()
        task.redC.rcList.Add(New rcData)
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
                    rc.index = task.redC.rcList.Count
                    task.redC.rcList.Add(rc)
                End If
            Next
        Next

        dst2.SetTo(0)
        task.redC.rcMap.SetTo(0)
        For Each rc In task.redC.rcList
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            task.redC.rcMap(rc.rect).SetTo(rc.index, rc.mask)
        Next

        For Each mat In diffs
            dst2.SetTo(0, mat)
        Next

        If cellmaps.Count > task.frameHistoryCount Then
            cellmaps.RemoveAt(0)
            cellLists.RemoveAt(0)
            diffs.RemoveAt(0)
        End If
    End Sub
End Class





Public Class RedColor_MaxDist_CPP : Inherits TaskParent
    Public classCount As Integer
    Public RectList As New List(Of cv.Rect)
    Public maxList As New List(Of Integer)
    Dim color8U As New Color8U_Basics
    Public Sub New()
        cPtr = RedCloudMaxDist_Open()
        desc = "Run the C++ RedCloudMaxDist interface without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = srcMustBe8U(src)

        If task.heartBeat Then maxList.Clear() ' reevaluate all cells.
        Dim maxArray = maxList.ToArray
        Dim handleMaxList = GCHandle.Alloc(maxArray, GCHandleType.Pinned)
        RedCloudMaxDist_SetPoints(cPtr, maxList.Count / 2, handleMaxList.AddrOfPinnedObject())
        handleMaxList.Free()

        Dim imagePtr As IntPtr
        Dim inputData(dst1.Total - 1) As Byte
        Marshal.Copy(dst1.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        imagePtr = RedCloudMaxDist_Run(cPtr, handleInput.AddrOfPinnedObject(), 0, dst1.Rows, dst1.Cols)
        handleInput.Free()
        dst2 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, imagePtr).Clone
        dst3 = ShowPalette(dst2)

        classCount = RedCloudMaxDist_Count(cPtr)
        labels(2) = "CV_8U version with " + CStr(classCount) + " cells."

        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloudMaxDist_Rects(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)

        For i = 0 To rects.Length - 4 Step 4
            RectList.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
        Next
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloudMaxDist_Close(cPtr)
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
        options.Run()

        dst2 = runRedOld(src, labels(2))

        Dim rc = task.rcD

        dst0 = task.color
        Dim correlationMat As New cv.Mat, correlationXtoZ As Single, correlationYtoZ As Single
        dst3.SetTo(0)
        Select Case options.selection
            Case 0
                Dim pt = rc.maxDist
                dst2.Circle(pt, task.DotSize, task.highlight, -1, cv.LineTypes.AntiAlias)
                labels(3) = "maxDist Is at (" + CStr(pt.X) + ", " + CStr(pt.Y) + ")"
            Case 1
                dst3(rc.rect).SetTo(vbNearFar((rc.depth) / task.MaxZmeters), rc.mask)
                labels(3) = "rc.depth Is highlighted in dst2"
                labels(3) = "Mean depth for the cell Is " + Format(rc.depth, fmt3)
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
        desc = "Find the gaps that are different in the RedColor_Basics sharedResults.images.."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedOld(src, labels(2))

        frames.Run(task.redC.rcMap.InRange(0, 0))
        dst3 = frames.dst2

        If task.redC.rcList.Count > 0 Then
            dst2(task.rcD.rect).SetTo(white, task.rcD.mask)
        End If

        If task.redC.rcList.Count > 0 Then
            Dim rc = task.redC.rcList(0) ' index can now be zero.
            dst3(rc.rect).SetTo(0, rc.mask)
        End If
        Dim count = dst3.CountNonZero
        labels(3) = "Unclassified pixel count = " + CStr(count) + " or " + Format(count / src.Total, "0%")
    End Sub
End Class








Public Class RedColor_BrightnessLevel : Inherits TaskParent
    Dim bright As New Brightness_Grid
    Public Sub New()
        desc = "Adjust the brightness so there is no whiteout and then run RedCloud with that."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bright.Run(src)

        dst2 = runRedOld(bright.dst2, labels(2))
        dst3 = task.redC.dst3
    End Sub
End Class







Public Class RedColor_LeftRight : Inherits TaskParent
    Dim redLR As New LeftRight_RedMask
    Public Sub New()
        desc = "Run RedCloud on the left and right images.  Duplicate of LeftRight_RedCloudBoth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redLR.Run(src)
        dst2 = redLR.dst2
        dst3 = redLR.dst3
        labels = redLR.labels
    End Sub
End Class










Public Class RedColor_Flippers : Inherits TaskParent
    Public flipCells As New List(Of rcData)
    Public nonFlipCells As New List(Of rcData)
    Public Sub New()
        task.gOptions.TrackingColor.Checked = True
        labels(3) = "Highlighted below are the cells which flipped in color from the previous frame."
        desc = "Identify the cells that are changing color because they were split or lost."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = runRedOld(src, labels(3))
        Static lastMap As cv.Mat = DisplayCells()

        Dim unMatched As Integer
        Dim unMatchedPixels As Integer
        flipCells.Clear()
        nonFlipCells.Clear()
        dst2.SetTo(0)
        Dim currMap = DisplayCells()
        For Each rc In task.redC.rcList
            Dim lastColor = lastMap.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            Dim currColor = currMap.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            If lastColor <> currColor Then
                unMatched += 1
                unMatchedPixels += rc.pixels
                flipCells.Add(rc)
                dst2(rc.rect).SetTo(rc.color, rc.mask)
            Else
                nonFlipCells.Add(rc)
            End If
        Next

        lastMap = currMap.Clone

        If task.heartBeat Then
            labels(2) = CStr(unMatched) + " of " + CStr(task.redC.rcList.Count) + " cells changed " +
                        " tracking color, totaling " + CStr(unMatchedPixels) + " pixels."
        End If
    End Sub
End Class






Public Class RedColor_FlipTest : Inherits TaskParent
    Dim flipper As New RedColor_Flippers
    Public Sub New()
        desc = "Display nonFlipped cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = runRedOld(src, labels(2))
        Dim lastCells As New List(Of rcData)(task.redC.rcList)
        flipper.Run(src)
        dst3 = flipper.dst2

        dst2.SetTo(0)
        Dim ptmaxDstable As New List(Of cv.Point)
        For Each rc In flipper.nonFlipCells
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            ptmaxDstable.Add(rc.maxDStable)
        Next

        Dim count As Integer
        For Each rc In flipper.flipCells
            Dim lrc = lastCells(rc.indexLast)
            Dim index = ptmaxDstable.IndexOf(lrc.maxDStable)
            If index > 0 Then
                Dim rcNabe = flipper.nonFlipCells(index)
                dst2(rc.rect).SetTo(rcNabe.color, rc.mask)
                count += 1
            End If
        Next
        If task.heartBeat Then
            labels(2) = CStr(flipper.flipCells.Count) + " cells flipped and " + CStr(count) + " cells " +
                        " were flipped back to the main cell."
            labels(3) = flipper.labels(2)
        End If
    End Sub
End Class






Public Class RedColor_Contour : Inherits TaskParent
    Public Sub New()
        task.gOptions.TrackingColor.Checked = True
        desc = "Add the contour to the cell mask in the RedColor_Basics output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = runRedOld(src, labels(2))

        dst2.SetTo(0)
        For Each rc In task.redC.rcList
            For i = 1 To 8
                Dim deltaX = Choose(i, -1, 1, 0, 0, -1, 1, -1, 1)
                Dim deltaY = Choose(i, 0, 0, -1, 1, -1, 1, 1, -1)
                Dim contour As New List(Of cv.Point)
                For Each pt In rc.contour
                    pt.X += deltaX
                    pt.Y += deltaY
                    pt = lpData.validatePoint(pt)
                    contour.Add(pt)
                Next
                If i < 8 Then
                    DrawContour(dst2(rc.rect), contour, rc.color, task.lineWidth)
                Else
                    DrawContour(dst2(rc.rect), contour, rc.color, -1)
                End If
            Next
        Next
    End Sub
End Class







Public Class RedColor_TopX : Inherits TaskParent
    Public topXcells As New List(Of cv.Point)
    Public Sub New()
        desc = "Isolate the top X cells and use the rest of the image as an input mask."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = runRedOld(src, labels(2))
        dst2.SetTo(0)

        If task.heartBeat Or task.optionsChanged Then
            topXcells.Clear()
            For Each rc In task.redC.rcList
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                topXcells.Add(rc.maxDist)
            Next
        Else
            Dim maxList As New List(Of cv.Point)
            For Each pt In topXcells
                Dim index = task.redC.rcMap.Get(Of Byte)(pt.Y, pt.X)
                Dim rc = task.redC.rcList(index)
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                DrawCircle(dst2, rc.maxDist, task.DotSize, task.highlight)
                maxList.Add(rc.maxDist)
            Next
            topXcells = New List(Of cv.Point)(maxList)
        End If
        labels(2) = "The Top " + CStr(topXcells.Count) + " largest cells in rcList."

        dst1 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        task.redC.inputRemoved = dst1
    End Sub
End Class







Public Class RedColor_Motion : Inherits TaskParent
    Public Sub New()
        task.gOptions.TrackingColor.Checked = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "If a RedCloud cell has no motion, it is preserved."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.motionPercent = 0 Then Exit Sub ' full image stable means nothing needs to be done...
        runRedOld(src, labels(2))
        If task.redC.rcList.Count = 0 Then Exit Sub

        Static rcLastList As New List(Of rcData)(task.redC.rcList)

        Dim count As Integer
        dst1.SetTo(0)
        task.redC.rcList.RemoveAt(0)
        'Dim newList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        Dim newList As New List(Of rcData), tmp As New cv.Mat
        Dim countMaxD As Integer, countMissedMaxD As Integer
        For Each rc In task.redC.rcList
            tmp = task.motionMask(rc.rect) And rc.mask
            If tmp.CountNonZero = 0 Then
                If rc.indexLast <> 0 And rc.indexLast < rcLastList.Count Then
                    Dim lrc = rcLastList(rc.indexLast)
                    If lrc.maxDStable = rc.maxDStable Then
                        countMaxD += 1
                        rc = lrc
                    Else
                        countMissedMaxD += 1
                        Continue For
                    End If
                End If
                Dim testCell = dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If testCell = 0 Then newList.Add(rc)
            Else
                count += 1
                newList.Add(rc)
            End If
            dst1(rc.rect).SetTo(255, rc.mask)
        Next
        labels(3) = CStr(count) + " of " + CStr(task.redC.rcList.Count) + " redCloud cells had motion." +
                    "  There were " + CStr(countMaxD) + " maxDstable matches and " + CStr(countMissedMaxD) + " misses"

        task.redC.rcList.Clear()
        task.redC.rcList.Add(New rcData)
        For Each rc In newList
            rc.index = task.redC.rcList.Count
            task.redC.rcList.Add(rc)
        Next

        rcLastList = New List(Of rcData)(task.redC.rcList)

        dst3.SetTo(0)
        For Each rc In task.redC.rcList
            dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next

        dst2 = RebuildRCMap(task.redC.rcList)
        task.setSelectedCell()
    End Sub
End Class






Public Class RedColor_Largest : Inherits TaskParent
    Public Sub New()
        task.gOptions.FrameHistory.Value = 1
        desc = "Identify the largest redCloud cells and accumulate them by size - largest to smallest"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedOld(src, labels(2))
        If task.redC.rcList.Count = 0 Then Exit Sub ' next frame please...

        Dim rc = task.redC.rcList(1)
        Static rcSave As rcData = rc, stableCount As Integer
        If rc.maxDStable <> rcSave.maxDStable Then
            rcSave = rc
            stableCount = 1
        Else
            stableCount += 1
        End If

        dst3.SetTo(0)
        dst3(rc.rect).SetTo(rc.color, rc.mask)
        dst3.Circle(rc.maxDStable, task.DotSize + 2, cv.Scalar.Black)
        DrawCircle(dst3, rc.maxDStable)
        labels(3) = "MaxDStable was the same for " + CStr(stableCount) + " frames"
    End Sub
End Class







Public Class RedColor_GridCellsOld : Inherits TaskParent
    Dim regions As New Region_Contours
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use the brickData regions to build task.redC.rcList"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        regions.Run(src)
        dst1 = regions.dst2

        runRedOld(src, labels(2))

        Dim mdList = New List(Of maskData)(regions.redM.mdList)
        dst2.SetTo(0)
        Dim histogram As New cv.Mat
        Dim ranges = {New cv.Rangef(0, 255)}
        Dim histArray(254) As Single
        Dim rcList As New List(Of rcData)
        Dim usedList As New List(Of Integer)
        For Each md In mdList
            cv.Cv2.CalcHist({task.redC.rcMap(md.rect)}, {0}, md.mask, histogram, 1, {255}, ranges)
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
            Dim index = rcList.Count
            Dim c = dst1.Get(Of cv.Vec3b)(md.maxDist.Y, md.maxDist.X)
            Dim color = New cv.Scalar(c(0), c(1), c(2))
            For i = 1 To histArray.Count - 1
                If usedList.Contains(i) Then Continue For
                If histArray(i) > 0 Then
                    Dim rc = task.redC.rcList(i)
                    If rc.depth > md.mm.minVal And rc.depth < md.mm.maxVal Then
                        rc.index = rcList.Count
                        rc.color = color
                        dst2(rc.rect).SetTo(rc.color, rc.mask)
                        rcList.Add(rc)
                        usedList.Add(i)
                    End If
                End If
            Next
        Next

        labels(3) = CStr(rcList.Count) + " redCloud cells were found"
    End Sub
End Class






Public Class RedColor_GridCells : Inherits TaskParent
    Dim regions As New Region_Contours
    Public Sub New()
        task.gOptions.TruncateDepth.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use the brickData regions to build task.redC.rcList"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        regions.Run(src)
        dst1 = regions.dst2

        runRedOld(src, labels(2))
        Dim lastList As New List(Of rcData)(task.redC.rcList)

        dst2.SetTo(0)

        Dim rcList As New List(Of rcData)
        For Each rc In task.redC.rcList
            If task.motionMask(rc.rect).CountNonZero = 0 Then
                If rc.indexLast > 0 And rc.indexLast < lastList.Count Then rc = lastList(rc.indexLast)
            End If
            Dim index = rcList.Count
            Dim cTest = dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            If cTest <> black Then Continue For
            Dim c = dst1.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            Dim color = New cv.Scalar(c(0), c(1), c(2))
            If color = black Then color = yellow
            rc.index = rcList.Count
            rc.color = color
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            DrawCircle(dst2, rc.maxDStable)
            rcList.Add(rc)
        Next

        task.redC.rcList = New List(Of rcData)(rcList)
        labels(3) = CStr(rcList.Count) + " redCloud cells were found"
    End Sub
End Class








Public Class RedColor_GridCellsHist : Inherits TaskParent
    Dim regions As New Region_Contours
    Public Sub New()
        task.gOptions.TruncateDepth.Checked = True
        desc = "For each redCell find the highest population region it covers."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        regions.Run(src)
        dst1 = regions.redM.dst2

        runRedOld(src, labels(2))
        Static rcLastList As New List(Of rcData)(task.redC.rcList)

        Dim mdList = New List(Of maskData)(regions.redM.mdList)
        Dim histogram As New cv.Mat
        Dim ranges = {New cv.Rangef(0, 255)}
        Dim rcList As New List(Of rcData)
        Dim lastCount As Integer
        Dim histArray(mdList.Count - 1) As Single
        For Each rc In task.redC.rcList
            cv.Cv2.CalcHist({dst1(rc.rect)}, {0}, rc.mask, histogram, 1, {255}, ranges)
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
            Dim index = histArray.ToList.IndexOf(histArray.Max)
            Dim md = mdList(index)
            rc.color = task.scalarColors(md.index)
            If rc.indexLast <> 0 Then
                If (task.motionMask(rc.rect) And rc.mask).ToMat.CountNonZero = 0 Then
                    rc = rcLastList(rc.indexLast)
                    lastCount += 1
                End If
            End If
            rcList.Add(rc)
        Next

        dst2.SetTo(0)
        For Each rc In rcList
            'Dim test = dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            'If test = black Then dst2(rc.rect).SetTo(rc.color, rc.mask)
            dst2(rc.rect).SetTo(rc.color, rc.mask)
        Next

        task.redC.rcList = New List(Of rcData)(rcList)
        rcLastList = New List(Of rcData)(rcList)
        labels(3) = CStr(rcList.Count) + " redCloud cells were found and " + CStr(lastCount) + " cells had no motion."
    End Sub
End Class





Public Class RedColor_CPP : Inherits TaskParent
    Public classCount As Integer
    Public rectList As New List(Of cv.Rect)
    Public identifyCount As Integer = 255
    Public Sub New()
        cPtr = RedMask_Open()
        desc = "Run the C++ RedCloud Interface With Or without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = srcMustBe8U(src)

        Dim inputData(dst1.Total - 1) As Byte
        Marshal.Copy(dst1.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim imagePtr = RedMask_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols, 0)
        handleInput.Free()
        dst2 = cv.Mat.FromPixelData(dst1.Rows + 2, dst1.Cols + 2, cv.MatType.CV_8U, imagePtr).Clone
        dst2 = dst2(New cv.Rect(1, 1, dst2.Width - 2, dst2.Height - 2))

        classCount = Math.Min(RedMask_Count(cPtr), identifyCount * 2)
        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedMask_Rects(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)

        rectList.Clear()
        For i = 0 To classCount * 4 - 4 Step 4
            rectList.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
        Next

        If standaloneTest() Then dst3 = ShowPalette(dst2)

        If task.heartBeat Then labels(2) = "CV_8U result With " + CStr(classCount) + " regions."
        If task.heartBeat Then labels(3) = "Palette version Of the data In dst2 With " + CStr(classCount) + " regions."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedMask_Close(cPtr)
    End Sub
End Class







Public Class RedColor_BasicsNoMask : Inherits TaskParent
    Public classCount As Integer
    Public rectList As New List(Of cv.Rect)
    Public identifyCount As Integer = 255
    Public Sub New()
        cPtr = RedCloud_Open()
        desc = "Run the C++ RedCloud Interface With Or without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = srcMustBe8U(src)

        Dim inputData(dst1.Total - 1) As Byte
        Marshal.Copy(dst1.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols, 0)
        handleInput.Free()
        dst2 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, imagePtr).Clone

        classCount = Math.Min(RedCloud_Count(cPtr), identifyCount * 2)
        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)

        rectList.Clear()
        For i = 0 To classCount * 4 - 4 Step 4
            rectList.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
        Next

        If standalone Then dst3 = ShowPalette(dst2)

        If task.heartBeat Then labels(2) = "CV_8U result With " + CStr(classCount) + " regions."
        If task.heartBeat Then labels(3) = "Palette version Of the data In dst2 With " + CStr(classCount) + " regions."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
    End Sub
End Class







Public Class RedColor_Reduction : Inherits TaskParent
    Public Sub New()
        task.gOptions.ColorSource.SelectedItem() = "Reduction_Basics"
        task.gOptions.setHistogramBins(20)
        desc = "Segment the image based On both the reduced color"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedOld(src, labels(2))
        dst3 = task.redC.rcMap
    End Sub
End Class









Public Class RedColor_Hulls : Inherits TaskParent
    Public rcList As New List(Of rcData)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        labels = {"", "Cells where convexity defects failed", "", "Improved contour results Using OpenCV's ConvexityDefects"}
        desc = "Add hulls and improved contours using ConvexityDefects to each RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedOld(src, labels(2))

        Dim defectCount As Integer
        task.redC.rcMap.SetTo(0)
        rcList.Clear()
        For Each rc In task.redC.rcList
            If rc.contour.Count >= 5 Then
                rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
                Dim hullIndices = cv.Cv2.ConvexHullIndices(rc.hull.ToArray, False)
                Try
                    Dim defects = cv.Cv2.ConvexityDefects(rc.contour, hullIndices)
                    rc.contour = Convex_RedColorDefects.betterContour(rc.contour, defects)
                Catch ex As Exception
                    defectCount += 1
                End Try
                DrawContour(rcMap(rc.rect), rc.hull, rc.index, -1)
                rcList.Add(rc)
            End If
        Next
        dst3 = ShowPalette(rcMap)
        labels(3) = CStr(rcList.Count) + " hulls identified below.  " + CStr(defectCount) +
                    " hulls failed to build the defect list."
    End Sub
End Class






Public Class RedColor_BasicsHist : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        task.gOptions.setHistogramBins(100)
        plot.createHistogram = True
        desc = "Display the histogram of a selected RedColor cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedOld(src, labels(2))
        If task.heartBeat Then
            Dim depth As cv.Mat = task.pcSplit(2)(task.rcD.rect)
            depth.SetTo(0, task.noDepthMask(task.rcD.rect))
            plot.minRange = 0
            plot.maxRange = task.MaxZmeters
            plot.Run(depth)
            labels(3) = "0 meters to " + Format(task.MaxZmeters, fmt0) + "meters - vertical lines every meter"

            Dim incr = dst2.Width / task.MaxZmeters
            For i = 1 To CInt(task.MaxZmeters - 1)
                Dim x = incr * i
                DrawLine(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White)
            Next
        End If
        dst3 = plot.dst2
    End Sub
End Class