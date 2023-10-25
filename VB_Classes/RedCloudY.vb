Imports cv = OpenCvSharp
Public Class RedCloudY_Basics : Inherits VB_Algorithm
    Public prep As New RedCloud_PrepPointCloud
    Public floodCell As New FloodCell_Basics
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
        prep.Run(Nothing)
        prep.dst0.ConvertTo(dst1, cv.MatType.CV_8U)

        floodCell.Run(dst1)
        dst2 = floodCell.dst2
        dst3 = floodCell.dst3

        If heartBeat() Then labels(2) = CStr(task.fCells.Count) + " regions were identified."
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

