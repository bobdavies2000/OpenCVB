Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedList_Basics : Inherits TaskParent
    Public inputRemoved As cv.Mat
    Public cellGen As New RedCell_Color
    Public redMask As New RedMask_Basics
    Public oldrclist As New List(Of oldrcData)
    Public rcMap As cv.Mat ' redColor map 
    Public Sub New()
        If task.contours Is Nothing Then task.contours = New Contour_Basics_List
        rcMap = New cv.Mat(New cv.Size(dst2.Width, dst2.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find cells and then match them to the previous generation with minimum boundary"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.contours.Run(src)
        If src.Type <> cv.MatType.CV_8U Then
            If standalone And task.gOptions.ColorSource.SelectedItem = "EdgeLine_Basics" Then
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

        For Each rc In task.redList.oldrclist
            DrawCircle(dst2, rc.maxDStable)
        Next
        labels(2) = cellGen.labels(2)
        labels(3) = ""
        SetTrueText("", newPoint, 1)
        task.setSelectedCell()
    End Sub
End Class






Public Class RedList_FindCells : Inherits TaskParent
    Public bricks As New List(Of Integer)
    Public Sub New()
        OptionParent.FindSlider("Color Difference Threshold").Value = 25
        cPtr = RedList_FindBricks_Open()
        desc = "Find all the RedCloud cells touched by the mask created by the Motion_History rectangle"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks = New List(Of Integer)

        dst2 = runRedList(src, labels(2))

        Dim cppData(task.redList.rcMap.Total - 1) As Byte
        Marshal.Copy(task.redList.rcMap.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = RedList_FindBricks_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
        handleSrc.Free()

        Dim count = RedList_FindBricks_TotalCount(cPtr)
        If count = 0 Then Exit Sub

        Dim cellsFound(count - 1) As Integer
        Marshal.Copy(imagePtr, cellsFound, 0, cellsFound.Length)

        bricks = cellsFound.ToList
        dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst0 = dst0.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        dst3.SetTo(0)
        For Each index In bricks
            If task.redList.oldrclist.Count <= index Then Continue For
            Dim rc = task.redList.oldrclist(index)
            DrawTour(dst3(rc.rect), rc.contour, rc.color, -1)
            dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next
        labels(3) = CStr(count) + " cells were found using the motion mask"
    End Sub
    Public Sub Close()
        RedList_FindBricks_Close(cPtr)
    End Sub
End Class









'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedList_Planes : Inherits TaskParent
    Public planes As New RedList_PlaneColor
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








Public Class RedList_Equations : Inherits TaskParent
    Dim eq As New Plane_Equation
    Public oldrclist As New List(Of oldrcData)
    Public Sub New()
        labels(3) = "The estimated plane equations for the largest 20 RedCloud cells."
        desc = "Show the estimated plane equations for all the cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            dst2 = runRedList(src, labels(2))
            oldrclist = New List(Of oldrcData)(task.redList.oldrclist)
        End If

        Dim newCells As New List(Of oldrcData)
        For Each orc As oldrcData In oldrclist
            If orc.contour.Count > 4 Then
                eq.rc = orc
                eq.Run(src)
                newCells.Add(eq.rc)
            End If
        Next

        oldrclist = New List(Of oldrcData)(newCells)

        If task.heartBeat Then
            Dim index As Integer
            strOut = ""
            For Each rc In oldrclist
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








Public Class RedList_CellsAtDepth : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        task.kalman = New Kalman_Basics
        plot.removeZeroEntry = False
        task.gOptions.HistBinBar.Value = 20
        labels(3) = "Use mouse to select depth to highlight.  Histogram shows count of cells at each depth."
        desc = "Create a histogram of depth using RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedList(src, labels(2))

        Dim histBins = task.histogramBins
        Dim slotList(histBins) As List(Of Integer)
        For i = 0 To slotList.Count - 1
            slotList(i) = New List(Of Integer)
        Next
        Dim hist(histBins - 1) As Single
        For Each rc In task.redList.oldrclist
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
            Dim rc = task.redList.oldrclist(slotList(histIndex)(i))
            DrawTour(dst2(rc.rect), rc.contour, cv.Scalar.Yellow)
            DrawTour(task.color(rc.rect), rc.contour, cv.Scalar.Yellow)
        Next
    End Sub
End Class








Public Class RedList_ShapeCorrelation : Inherits TaskParent
    Public Sub New()
        desc = "A shape correlation is between each x and y in list of contours points.  It allows classification based on angle and shape."
    End Sub
    Public Shared Function shapeCorrelation(points As List(Of cv.Point)) As Single
        Dim pts As cv.Mat = cv.Mat.FromPixelData(points.Count, 1, cv.MatType.CV_32SC2, points.ToArray)
        Dim pts32f As New cv.Mat
        pts.ConvertTo(pts32f, cv.MatType.CV_32FC2)
        Dim split = pts32f.Split()
        Dim correlationMat As New cv.Mat
        cv.Cv2.MatchTemplate(split(0), split(1), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
        Return correlationMat.Get(Of Single)(0, 0)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedList(src, labels(2))

        Dim rc = task.oldrcD
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









'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedList_PlaneColor : Inherits TaskParent
    Public options As New Options_Plane
    Dim planeCells As New Plane_CellColor
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = runRedList(src, labels(2))

        dst3.SetTo(0)
        Dim fitPoints As New List(Of cv.Point3f)
        For Each rc In task.redList.oldrclist
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
Public Class RedList_PlaneFromContour : Inherits TaskParent
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation each cell's contour"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then dst2 = runRedList(src, labels(2))

        Dim rc = task.oldrcD
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
Public Class RedList_PlaneFromMask : Inherits TaskParent
    Public Sub New()
        labels(3) = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"
        desc = "Create a plane equation from the pointcloud samples in a RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then dst2 = runRedList(src, labels(2))

        Dim rc = task.oldrcD
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








Public Class RedList_PlaneEq3D : Inherits TaskParent
    Dim eq As New Plane_Equation
    Public Sub New()
        desc = "If a RedColor cell contains depth then build a plane equation"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedList(src, labels(2))

        Dim rc = task.oldrcD
        If rc.mmZ.maxVal Then
            eq.rc = rc
            eq.Run(src)
            rc = eq.rc
        End If

        dst3.SetTo(0)
        DrawTour(dst3(rc.rect), rc.contour, rc.color, -1)

        SetTrueText(eq.strOut, 3)
    End Sub
End Class









Public Class RedList_UnstableCells : Inherits TaskParent
    Dim prevList As New List(Of cv.Point)
    Public Sub New()
        labels = {"", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing"}
        desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedList(src, labels(2))

        If task.heartBeat Or task.frameCount = 2 Then
            dst1 = dst2.Clone
            dst3.SetTo(0)
        End If

        Dim currList As New List(Of cv.Point)
        For Each rc In task.redList.oldrclist
            If prevList.Contains(rc.maxDStable) = False Then
                DrawTour(dst1(rc.rect), rc.contour, white, -1)
                DrawTour(dst1(rc.rect), rc.contour, cv.Scalar.Black)
                DrawTour(dst3(rc.rect), rc.contour, white, -1)
            End If
            currList.Add(rc.maxDStable)
        Next

        prevList = New List(Of cv.Point)(currList)
    End Sub
End Class








Public Class RedList_UnstableHulls : Inherits TaskParent
    Dim prevList As New List(Of cv.Point)
    Public Sub New()
        labels = {"", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing"}
        desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedList(src, labels(2))

        If task.heartBeat Or task.frameCount = 2 Then
            dst1 = dst2.Clone
            dst3.SetTo(0)
        End If

        Dim currList As New List(Of cv.Point)
        For Each rc In task.redList.oldrclist
            rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
            If prevList.Contains(rc.maxDStable) = False Then
                DrawTour(dst1(rc.rect), rc.hull, white, -1)
                DrawTour(dst1(rc.rect), rc.hull, cv.Scalar.Black)
                DrawTour(dst3(rc.rect), rc.hull, white, -1)
            End If
            currList.Add(rc.maxDStable)
        Next

        prevList = New List(Of cv.Point)(currList)
    End Sub
End Class






Public Class RedList_CellStatsPlot : Inherits TaskParent
    Dim cells As New XO_RedCell_BasicsPlot
    Public Sub New()
        If standaloneTest() Then task.gOptions.displayDst1.Checked = True
        cells.runRedCflag = True
        desc = "Display the stats for the selected cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        cells.Run(src)
        dst1 = cells.dst3
        dst2 = cells.dst2
        labels(2) = cells.labels(2)

        SetTrueText(cells.strOut, 3)
    End Sub
End Class









Public Class RedList_FourColor : Inherits TaskParent
    Dim binar4 As New Bin4Way_Regions
    Public Sub New()
        labels(3) = "A 4-way split of the input grayscale image based on brightness"
        desc = "Use RedCloud on a 4-way split based on light to dark in the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        binar4.Run(src)
        dst3 = PaletteFull(binar4.dst2)

        dst2 = runRedList(binar4.dst2, labels(2))
    End Sub
End Class









Public Class RedList_Hue : Inherits TaskParent
    Dim hue As New Color8U_Hue
    Public Sub New()
        labels(3) = "Mask of the areas with Hue"
        desc = "Run RedCloud on just the red hue regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hue.Run(src)
        dst3 = hue.dst2

        dst2 = runRedList(src, labels(2), Not dst3)
    End Sub
End Class










Public Class RedList_Consistent : Inherits TaskParent
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        OptionParent.FindSlider("Color Difference Threshold").Value = 1
        desc = "Remove RedColor results that are inconsistent with the previous frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedList(src, labels(2))

        dst3.SetTo(0)
        Dim count As Integer
        For Each rc In task.redList.oldrclist
            If rc.age > 1 Then
                dst3(rc.rect).SetTo(rc.color, rc.mask)
                count += 1
            End If
        Next
        labels(3) = CStr(count) + " cells matched the previous generation."
    End Sub
End Class







Public Class RedList_Features : Inherits TaskParent
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

        dst2 = runRedList(src, labels(2))

        Dim rc = task.oldrcD

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
        DrawTour(dst0(rc.rect), rc.contour, cv.Scalar.Yellow)
        SetTrueText(labels(3), 3)
        labels(2) = "Highlighted feature = " + options.labelName
    End Sub
End Class











Public Class RedList_Flippers : Inherits TaskParent
    Public flipCells As New List(Of oldrcData)
    Public nonFlipCells As New List(Of oldrcData)
    Public Sub New()
        task.gOptions.TrackingColor.Checked = True
        labels(3) = "Highlighted below are the cells which flipped in color from the previous frame."
        desc = "Identify the cells that are changing color because they were split or lost."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = runRedList(src, labels(3))
        Static lastMap As cv.Mat = DisplayCells()

        Dim unMatched As Integer
        Dim unMatchedPixels As Integer
        flipCells.Clear()
        nonFlipCells.Clear()
        dst2.SetTo(0)
        Dim currMap = DisplayCells()
        For Each rc In task.redList.oldrclist
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
            labels(2) = CStr(unMatched) + " of " + CStr(task.redList.oldrclist.Count) + " cells changed " +
                        " tracking color, totaling " + CStr(unMatchedPixels) + " pixels."
        End If
    End Sub
End Class






Public Class RedList_FlipTest : Inherits TaskParent
    Dim flipper As New RedList_Flippers
    Public Sub New()
        desc = "Display nonFlipped cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = runRedList(src, labels(2))
        Dim lastCells As New List(Of oldrcData)(task.redList.oldrclist)
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








Public Class RedList_CPP : Inherits TaskParent
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

        If standaloneTest() Then dst3 = PaletteFull(dst2)

        If task.heartBeat Then labels(2) = "CV_8U result With " + CStr(classCount) + " regions."
        If task.heartBeat Then labels(3) = "Palette version of the data In dst2 With " + CStr(classCount) + " regions."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedMask_Close(cPtr)
    End Sub
End Class










Public Class RedList_Hulls : Inherits TaskParent
    Public oldrclist As New List(Of oldrcData)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        labels = {"", "Cells where convexity defects failed", "", "Improved contour results Using OpenCV's ConvexityDefects"}
        desc = "Add hulls and improved contours using ConvexityDefects to each RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedList(src, labels(2))

        Dim defectCount As Integer
        task.redList.rcMap.SetTo(0)
        oldrclist.Clear()
        For Each rc In task.redList.oldrclist
            If rc.contour.Count >= 5 Then
                rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
                Dim hullIndices = cv.Cv2.ConvexHullIndices(rc.hull.ToArray, False)
                Try
                    Dim defects = cv.Cv2.ConvexityDefects(rc.contour, hullIndices)
                    rc.contour = Convex_RedColorDefects.betterContour(rc.contour, defects)
                Catch ex As Exception
                    defectCount += 1
                End Try
                DrawTour(rcMap(rc.rect), rc.hull, rc.index, -1)
                oldrclist.Add(rc)
            End If
        Next
        dst3 = PaletteFull(rcMap)
        labels(3) = CStr(oldrclist.Count) + " hulls identified below.  " + CStr(defectCount) +
                    " hulls failed to build the defect list."
    End Sub
End Class






Public Class RedList_CellDepthHistogram : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        task.gOptions.setHistogramBins(100)
        plot.createHistogram = True
        desc = "Display the histogram of a selected RedColor cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedList(src, labels(2))
        If task.heartBeat Then
            Dim depth As cv.Mat = task.pcSplit(2)(task.oldrcD.rect)
            depth.SetTo(0, task.noDepthMask(task.oldrcD.rect))
            plot.minRange = 0
            plot.maxRange = task.MaxZmeters
            plot.Run(depth)
            labels(3) = "0 meters to " + Format(task.MaxZmeters, fmt0) + " meters - vertical lines every meter"

            Dim incr = dst2.Width / task.MaxZmeters
            For i = 1 To CInt(task.MaxZmeters - 1)
                Dim x = incr * i
                DrawLine(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White)
            Next
        End If
        dst3 = plot.dst2
    End Sub
End Class






Public Class RedList_EdgesZ : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim edgesZ As New RedPrep_EdgesZ
    Public Sub New()
        desc = "Add the depth edges in Z to the color image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)

        edgesZ.Run(reduction.dst3)

        dst2 = runRedList(edgesZ.dst2, labels(2))
    End Sub
End Class








