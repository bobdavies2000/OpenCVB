Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedCloudY_Basics : Inherits VB_Algorithm
    Public redCore As New RedCloudY_Core
    Public floodCells As New FloodCell_Basics
    Public redCells As New List(Of rcData)
    Public lastCells As New List(Of rcData)
    Public showSelected As Boolean = True
    Public Sub New()
        gOptions.HistBinSlider.Value = 15
        desc = "Segment the image based only on the reduced point cloud (as opposed to back projection)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lastCells = New List(Of rcData)(redCells)
        dst0 = task.color.Clone
        redCore.Run(src)
        floodCells.inputMask = task.noDepthMask
        redCore.dst0.ConvertTo(dst1, cv.MatType.CV_8U)

        floodCells.Run(dst1)
        dst2 = floodCells.dst2
        dst3 = floodCells.dst3

        If heartBeat() Then labels(2) = CStr(task.fCells.Count) + " regions were identified."
    End Sub
End Class







'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class RedCloudY_PlaneFromContour : Inherits VB_Algorithm
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
Public Class RedCloudY_PlaneFromMask : Inherits VB_Algorithm
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









Public Class RedCloudY_PointTracker : Inherits VB_Algorithm
    Dim hulls As New RedCloud_Hulls
    Public matchedCells As New List(Of rcData)
    Dim knn As New KNN_Lossy
    Public Sub New()
        desc = "Connect each cell to previous generation with KNN"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2

        knn.queries.Clear()
        For Each rc In task.redCells
            knn.queries.Add(rc.maxDist)
        Next

        knn.Run(Nothing)
        dst3 = knn.dst2
        matchedCells.Clear()
        Static lastQueries As New List(Of cv.Point2f)(knn.queries)
        For Each mps In knn.matches
            Dim index = knn.queries.IndexOf(mps.p1)
            Dim rc = task.redCells(index)
            rc.indexLast = lastQueries.IndexOf(mps.p2)
            matchedCells.Add(rc)
        Next
        labels(3) = CStr(matchedCells.Count) + " cells of " + CStr(task.redCells.Count) + " matched.  MaxDist Is used so any changes are yellow."
        lastQueries = New List(Of cv.Point2f)(knn.queries)
    End Sub
End Class






Public Class RedCloudY_NearestStableCell : Inherits VB_Algorithm
    Public knn As New KNN_Basics
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        labels(3) = "Line connects current maxDist point to nearest neighbor using KNN."
        desc = "Find the nearest stable cell and connect them with a line."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2
        labels(2) = hulls.labels(2)

        knn.queries.Clear()
        For Each rc In task.redCells
            knn.queries.Add(New cv.Point2f(rc.maxDist.X, rc.maxDist.Y))
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)

        knn.Run(Nothing)

        dst3.SetTo(0)
        For i = 1 To knn.result.GetUpperBound(0)
            Dim rc1 = task.redCells(knn.result(i, 0))
            If rc1.indexLast = 0 Then Continue For
            For j = 1 To knn.result.GetUpperBound(1)
                Dim rc2 = task.redCells(knn.result(i, j))
                If rc2.indexLast > 0 Then
                    dst3.Circle(rc1.maxDist, task.dotSize, rc1.color, -1, task.lineType)
                    dst3.Circle(rc2.maxDist, task.dotSize, rc2.color, -1, task.lineType)
                    dst3.Line(rc1.maxDist, rc2.maxDist, rc1.color, task.lineWidth, task.lineType)
                    Exit For
                End If
            Next
        Next
    End Sub
End Class












Public Class RedCloudY_NeighborsOld : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim knn As New KNN_Basics
    Dim colorClass As New Color_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "", "Output of RedCloud_Basics", "Cells with similar color or depth - click anywhere to see neighbor list"}
        desc = "Find neighbors with similar color or depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        knn.queries.Clear()
        For Each rcq In task.redCells
            knn.queries.Add(New cv.Point2f(rcq.maxDist.X, rcq.maxDist.Y))
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries) ' queries and training data are the same set of cells.  Trying to merge nearest cell...

        knn.Run(Nothing)

        colorClass.Run(src)
        dst1 = colorClass.dst2

        dst3 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim joinList As New List(Of List(Of Integer))
        For i = 0 To task.redCells.Count - 1
            joinList.Add(New List(Of Integer))
        Next

        Dim rc = task.rcSelect
        If rc.index = 0 Then
            rc.index = CInt(task.redCells.Count / 2)
            task.clickPoint = rc.maxDist
        End If

        dst3.Circle(rc.maxDist, task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
        If task.redCells.Count <= 1 Then Exit Sub
        If rc.maxDist = New cv.Point Then rc = task.redCells(1) ' switch to the largest cell...
        Dim rcClass = dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
        For i = 1 To knn.neighbors.Count - 1
            rc = task.redCells(knn.neighbors(rc.index)(i))
            If rc.index = 0 Then Continue For
            Dim rcXclass = dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If rcXclass = rcClass Then
                dst3.Line(rc.maxDist, rc.maxDist, cv.Scalar.Yellow, task.lineWidth, task.lineType)
                joinList(rc.index).Add(rc.index)
            End If
        Next
    End Sub
End Class







Public Class RedCloudY_DepthMinMax : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim dMinMax As New Depth_MinMaxNone
    Public Sub New()
        desc = "Attempt to stabilize the RedCloud cells by using the depth created by Depth_MinMaxNone as input to RedCloud."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dMinMax.options.RunVB()
        If dMinMax.options.useNone Then
            labels(3) = "Point cloud unchanged."
            redC.Run(src)
        Else
            labels(3) = "Output of Depth_MinMaxNone"
            dMinMax.Run(src)
            cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1), dMinMax.dst3}, task.pointCloud)
            redC.Run(src)
        End If
        dst2 = redC.dst2
        dst3 = task.pointCloud
        labels(2) = redC.labels(2)
    End Sub
End Class






Public Class RedCloudY_World : Inherits VB_Algorithm
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






Public Class RedCloudY_DistancePoint : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "Output of RedOldCloud_Cells", "Distance transform of selected cell mask"}
        desc = "Display how the distance transform of the mask for the selected cell works."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim rc = task.rcSelect

        Dim mask = rc.mask.Clone
        mask.Rectangle(New cv.Rect(0, 0, mask.Width - 1, mask.Height - 1), cv.Scalar.Black, 1)
        Dim test32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
        Dim mm = vbMinMax(test32f)
        test32f.ConvertTo(dst1, cv.MatType.CV_8U)

        dst3.SetTo(0)
        dst3(rc.rect) = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        vbDrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
        ' dst3(rc.rect).SetTo(cv.Scalar.White, rc.mask)
        dst3(rc.rect).Circle(mm.maxLoc, task.dotSize + 1, cv.Scalar.Yellow, -1)
        dst3.Rectangle(rc.rect, cv.Scalar.White, task.lineWidth)
    End Sub
End Class








Public Class RedCloudY_KNN_Join : Inherits VB_Algorithm
    Dim knn As New KNN_Basics
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Depth difference to determine if cells should be joined (cm)", 1, 100, 50)
        labels = {"", "", "Output of RedCloud_Hulls", "Cells with similar depth are the same color - now using maxDist for more stability"}
        desc = "Find neighbors and join cells if the neighbors have similar depth (using maxDist now.)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static depthSlider = findSlider("Depth difference to determine if cells should be joined (cm)")
        Dim depthDiffMax = depthSlider.Value / 100

        hulls.Run(src)
        dst2 = hulls.dst2

        knn.queries.Clear()
        For Each rc In task.redCells
            Dim p1 = rc.maxDist
            knn.queries.Add(New cv.Point2f(p1.X, p1.Y))
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries) ' queries and training data are the same set of cells.  Trying to merge nearest cell...

        knn.Run(Nothing)

        dst3.SetTo(0)
        If task.redCells.Count < 2 Then Exit Sub
        For i = 0 To task.redCells.Count - 1
            Dim rc = task.redCells(i)
            Dim lrc = task.redCells(knn.result(i, 1))
            If rc.index = 0 Or lrc.index = 0 Then Continue For
            Dim val As cv.Vec3b = dst3.Get(Of cv.Vec3b)(lrc.maxDist.Y, lrc.maxDist.X)
            If val = black Then
                If Math.Abs(lrc.depthMean.Z - rc.depthMean.Z) < depthDiffMax Then
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                    dst3(lrc.rect).SetTo(rc.color, lrc.mask)
                Else
                    dst3(lrc.rect).SetTo(lrc.color, lrc.mask)
                End If
            End If
        Next
    End Sub
End Class









Public Class RedCloudY_FlatSurfaces : Inherits VB_Algorithm
    Dim verts As New Plane_Basics
    Dim redC As New RedCloud_Basics
    Public vCells As New List(Of rcData)
    Public hCells As New List(Of rcData)
    Public Sub New()
        desc = "Use the mask for vertical surfaces to identify RedCloud cells that appear to be flat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        verts.Run(src)
        dst2.SetTo(255, verts.dst2)

        redC.Run(src)
        dst1 = redC.dst2

        dst2.SetTo(0)
        dst3.SetTo(0)
        vCells.Clear()
        hCells.Clear()
        For Each rc In task.redCells
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






Public Class RedCloudY_StructuredH : Inherits VB_Algorithm
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

        motion.Run(sliceMask)

        If heartBeat() Then dst1.SetTo(0)
        dst1.SetTo(cv.Scalar.White, sliceMask)
        labels = motion.labels

        dst2.SetTo(0)
        For Each index In motion.motionList
            Dim rc As rcData = task.redCells(index)
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






Public Class RedCloudY_StructuredV : Inherits VB_Algorithm
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

        motion.Run(sliceMask)

        If heartBeat() Then dst1.SetTo(0)
        dst1.SetTo(cv.Scalar.White, sliceMask)
        labels = motion.labels
        setTrueText("Move mouse in image to see impact.", 3)

        dst2.SetTo(0)
        For Each index In motion.motionList
            Dim rc As rcData = task.redCells(index)
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








Public Class RedCloudY_SliceH : Inherits VB_Algorithm
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






Public Class RedCloudY_SliceV : Inherits VB_Algorithm
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







Public Class RedCloudY_Core : Inherits VB_Algorithm
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








'Public Class RedCloudY_Match : Inherits VB_Algorithm
'    Public inputCells As New List(Of rcData)
'    Public redCells As New List(Of rcData)
'    Public lastCells As New List(Of rcData)
'    Public cellMap As New cv.Mat

'    Dim lastCellMap As New cv.Mat
'    Dim usedColors As New List(Of cv.Vec3b)({New cv.Vec3b(0, 0, 0)})
'    Dim matchedCells As Integer
'    Dim numBigCells As Integer
'    Public Sub New()
'        desc = "Match cells from the previous generation"
'    End Sub
'    Public Function showSelect() As rcData
'        Return showSelection(dst2)
'    End Function
'    Public Function redSelect() As rcData
'        If task.drawRect <> New cv.Rect Then Return New rcData
'        Dim index = cellMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
'        If task.clickPoint = New cv.Point Then
'            index = 0
'            task.clickPoint = redCells(index).maxDist
'        End If

'        Dim rc = redCells(index)
'        If index = task.redOther Then
'            Dim gridID = task.gridToRoiIndex.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
'            rc.rect = task.gridList(gridID)
'            rc.mask = cellMap(rc.rect).InRange(task.redOther, task.redOther)
'            rc.pixels = rc.mask.CountNonZero
'            buildCell(rc)
'        End If

'        task.color.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
'        vbDrawContour(task.color(rc.rect), rc.contour, cv.Scalar.White, 1)

'        task.depthRGB.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
'        vbDrawContour(task.depthRGB(rc.rect), rc.contour, cv.Scalar.White, 1)

'        dst2(rc.rect).SetTo(cv.Scalar.White, rc.mask)
'        dst2.Circle(rc.maxDist, task.dotSize, cv.Scalar.Black, -1, task.lineType)
'        Return rc
'    End Function
'    Private Sub buildCell(ByRef rc As rcData)
'        rc.maxDStable = rc.maxDist ' assume it has to use the latest.
'        rc.indexLast = lastCellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
'        rc.motionRect = rc.rect
'        If rc.indexLast = numBigCells Then
'            For i = rc.floodPoint.Y To Math.Min(rc.rect.Y + rc.rect.Height, dst2.Height) - 1
'                rc.indexLast = lastCellMap.Get(Of Byte)(i, rc.floodPoint.X)
'                If rc.indexLast = rc.index Then Exit For
'            Next
'        End If
'        If rc.indexLast < lastCells.Count And rc.indexLast <> task.redOther Then
'            Dim lrc = lastCells(rc.indexLast)
'            rc.motionRect = rc.rect.Union(lrc.rect)
'            rc.color = lrc.color

'            Dim stableCheck = cellMap.Get(Of Byte)(lrc.maxDStable.Y, lrc.maxDStable.X)
'            If stableCheck = rc.indexLast Then rc.maxDStable = lrc.maxDStable ' keep maxDStable if cell matched to previous
'            matchedCells += 1
'        Else
'            dst3(rc.rect).SetTo(cv.Scalar.White, rc.mask)
'        End If

'        If usedColors.Contains(rc.color) Then
'            rc.color = New cv.Vec3b(msRNG.Next(30, 240), msRNG.Next(30, 240), msRNG.Next(30, 240))
'        End If

'        usedColors.Add(rc.color)

'        rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
'        If rc.index <> 0 Then vbDrawContour(rc.mask, rc.contour, rc.index, -1)

'        Dim minLoc As cv.Point, maxLoc As cv.Point
'        task.pcSplit(0)(rc.rect).MinMaxLoc(rc.minVec.X, rc.maxVec.X, minLoc, maxLoc, rc.mask)
'        task.pcSplit(1)(rc.rect).MinMaxLoc(rc.minVec.Y, rc.maxVec.Y, minLoc, maxLoc, rc.mask)
'        task.pcSplit(2)(rc.rect).MinMaxLoc(rc.minVec.Z, rc.maxVec.Z, minLoc, maxLoc, rc.mask)

'        Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
'        cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev, rc.mask)

'        ' If there Is no Then depth within the mask, estimate this color only cell Using rc.rect instead!
'        If depthMean(2) = 0 Then
'            rc.colorOnly = True
'            cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev)
'        End If
'        rc.depthMean = New cv.Point3f(depthMean(0), depthMean(1), depthMean(2))
'        rc.depthStdev = New cv.Point3f(depthStdev(0), depthStdev(1), depthStdev(2))

'        cv.Cv2.MeanStdDev(task.color(rc.rect), rc.colorMean, rc.colorStdev, rc.mask)
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        numBigCells = 100 ' Math.Min(numSlider.value, inputCells.Count)

'        If cellMap.Width <> src.Width Then cellMap = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)
'        If standalone Then
'            Static guided As New GuidedBP_Depth
'            Static buildCells As New GuidedBP_FloodCells
'            guided.Run(src)
'            buildCells.inputMask = task.noDepthMask
'            buildCells.Run(guided.backProject)
'            inputCells = New List(Of rcData)(buildCells.redCells)
'        End If

'        If task.optionsChanged Or firstPass Then
'            cellMap.SetTo(task.redOther)
'            lastCells.Clear()
'        End If

'        lastCellMap = cellMap.Clone
'        cellMap.SetTo(task.redOther)
'        redCells.Clear()
'        usedColors.Clear()
'        usedColors.Add(black)
'        matchedCells = 0

'        If heartBeat() Then dst3.SetTo(0)
'        cellMap.SetTo(numBigCells)
'        dst2.SetTo(0)

'        For Each rc In inputCells
'            rc.index = redCells.Count
'            rc.maxDist = vbGetMaxDist(rc)
'            Dim spotTakenTest = cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
'            If spotTakenTest <> numBigCells Then Continue For
'            If rc.pixels > 0 And rc.pixels < task.minPixels Then Continue For
'            buildCell(rc)
'            redCells.Add(rc)

'            cellMap(rc.rect).SetTo(rc.index, rc.mask)
'            dst2(rc.rect).SetTo(rc.color, rc.mask)

'            If redCells.Count >= numBigCells - 1 Then Exit For
'        Next

'        Dim rcOther As New rcData
'        rcOther.rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
'        rcOther.floodPoint = New cv.Point(0, 0)
'        rcOther.mask = cellMap.InRange(numBigCells, numBigCells)
'        rcOther.pixels = rcOther.mask.CountNonZero
'        rcOther.index = redCells.Count
'        redCells.Add(rcOther)

'        task.redOther = redCells.Count - 1
'        cellMap.SetTo(task.redOther, rcOther.mask)

'        Static changedTotal As Integer = 0
'        changedTotal += redCells.Count - matchedCells
'        labels(3) = CStr(changedTotal) + " new/moved cells in the last second " +
'                    Format(changedTotal / (task.frameCount - task.toggleFrame), fmt1) + " unmatched per frame"
'        If heartBeat() Then
'            labels(2) = CStr(redCells.Count) + " cells " + CStr(redCells.Count - matchedCells) + " did not match the previous frame. Click a cell to see more."
'            changedTotal = 0
'        End If

'        lastCells = New List(Of rcData)(redCells)
'    End Sub
'End Class







Public Class RedCloudY_UnstableCells : Inherits VB_Algorithm
    Dim colorC As New RedCloudY_Basics
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





Public Class RedCloudY_UnstableCells1 : Inherits VB_Algorithm
    Dim colorC As New RedCloudY_Basics
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








Public Class RedCloud_ByIndex : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If sliders.Setup(traceName) Then sliders.setupTrackBar("RedColor cell index", 1, 100, 1)
        labels = {"", "", "RedColor Output", ""}
        desc = "Select a RedColor cell using a slider."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static indexSlider = findSlider("RedColor cell index")

        redC.Run(src)
        dst0 = redC.dst0
        dst2 = redC.dst2

        indexSlider.value = 0
        indexSlider.maximum = task.redCells.Count - 1
        Dim rc = task.redCells(indexSlider.value)

        labels(2) = redC.labels(2)
    End Sub
End Class







Public Class RedCloudY_Track5D : Inherits VB_Algorithm
    Dim colorC As New RedCloudY_Basics
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








Public Class RedCloudY_Track8D : Inherits VB_Algorithm
    Dim colorC As New RedCloudY_Basics
    Public Sub New()
        desc = "Track a cell using its color and location - a distance calculation in 8 dimensions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorC.Run(src)
        dst2 = colorC.dst2
        labels(2) = colorC.labels(2)

        Dim rc = showSelection(dst2)

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






Public Class RedCloudY_NoDepth : Inherits VB_Algorithm
    Dim colorC As New RedCloudY_Basics
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







Public Class RedCloudY_Plane3D : Inherits VB_Algorithm
    Dim colorC As New RedCloudY_Basics
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








Public Class RedCloudY_BProject3D : Inherits VB_Algorithm
    Dim colorC As New RedCloudY_Basics
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






Public Class RedCloudY_ByDepth : Inherits VB_Algorithm
    Dim colorC As New RedCloudY_Basics
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
    Dim colorC As New RedCloudY_Basics
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







Public Class RedCloudY_Binarize : Inherits VB_Algorithm
    Dim binarize As New Binarize_RecurseAdd
    Dim redC As New RedCloudY_Basics
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







Public Class RedCloudY_CCompBinarized : Inherits VB_Algorithm
    Dim edges As New Edge_BinarizedSobel
    Dim ccomp As New RedCloudY_Binarize
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
Public Class RedCloudY_CComp : Inherits VB_Algorithm
    Dim ccomp As New CComp_Both
    Dim redC As New RedCloudY_Basics
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








Public Class RedCloudY_Diff : Inherits VB_Algorithm
    Dim diff As New Diff_RGBAccum
    Dim colorC As New RedCloudY_Basics
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








Public Class RedCloudY_HistValley : Inherits VB_Algorithm
    Dim colorC As New RedCloudY_Binarize
    Dim valley As New HistValley_Basics
    Dim dValley As New HistValley_Depth
    Dim canny As New Edge_Canny
    Public Sub New()
        desc = "Use RedCloudY_Basics with the output of HistValley_Basics."
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






Public Class RedCloudY_CellStats : Inherits VB_Algorithm
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

        Dim lrc = If(task.lastCells.Count > 0 And task.lastCells.Count > rc.indexLast, task.lastCells(rc.indexLast), New rcData)

        strOut = "rc.index = " + CStr(rc.index) + " of " + CStr(task.redCells.Count) + vbTab
        strOut += "rc.indexlast = " + CStr(rc.indexLast) + " of " + CStr(task.lastCells.Count) + vbTab
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







Public Class RedCloudY_Hulls : Inherits VB_Algorithm
    Dim convex As New Convex_RedCloudDefects
    Dim colorC As New RedCloudY_Basics
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

        showSelection(dst3)
        labels(2) = CStr(redCells.Count) + " hulls identified below.  " + CStr(defectCount) + " hulls failed to build the defect list."
    End Sub
End Class









Public Class RedCloudY_LineID : Inherits VB_Algorithm
    Public lines As New Line_Basics
    Public rCells As New List(Of rcData)
    Dim p1list As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
    Dim p2list As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
    Dim rectList As New List(Of cv.Point)
    Dim maxDistance As Integer
    Public colorC As New RedCloudY_Basics
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










Public Class RedCloudY_KNNCenters : Inherits VB_Algorithm
    Dim lines As New RedCloudY_LineID
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







Public Class RedCloudY_ProjectCell : Inherits VB_Algorithm
    Dim topView As New Histogram_ShapeTop
    Dim sideView As New Histogram_ShapeSide
    Dim mats As New Mat_4Click
    Dim colorC As New RedCloudY_Basics
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











Public Class RedCloudY_ContourCorners : Inherits VB_Algorithm
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










Public Class RedCloudY_KMeans : Inherits VB_Algorithm
    Dim km As New KMeans_MultiChannel
    Dim colorC As New RedCloudY_Basics
    Public Sub New()
        labels = {"", "", "KMeans_MultiChannel output", "RedCloudY_Basics output"}
        desc = "Use RedCloud to identify the regions created by kMeans"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        km.Run(src)
        dst2 = km.dst2

        colorC.Run(km.dst3)
        dst3 = colorC.dst2
    End Sub
End Class