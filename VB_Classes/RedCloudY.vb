Imports cv = OpenCvSharp
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







Public Class RedCloudY_CellsAtDepth : Inherits VB_Algorithm
    Dim plot As New Plot_Histogram
    Dim kalman As New Kalman_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Histogram of depth weighted by the size of the cell."
        desc = "Create a histogram of depth using RedCloud cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)

        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim slotList(task.histogramBins) As List(Of Integer)
        For i = 0 To slotList.Count - 1
            slotList(i) = New List(Of Integer)
        Next
        Dim hist(task.histogramBins - 1) As Single
        For Each rc In task.redCells
            Dim slot As Integer
            If Single.IsNaN(rc.depthMean.Z) Or Single.IsInfinity(rc.depthMean.Z) Then
                slot = 0
            Else
                slot = CInt((rc.depthMean.Z / task.maxZmeters) * task.histogramBins)
            End If
            If slot >= hist.Length Then slot = hist.Length - 1
            slotList(slot).Add(rc.index)
            hist(slot) += rc.pixels
        Next
        hist(0) = 0 ' remove the count of cells with no depth.

        kalman.kInput = hist
        kalman.Run(src)

        Dim histMat = New cv.Mat(task.histogramBins, 1, cv.MatType.CV_32F, kalman.kOutput)
        plot.Run(histMat)
        dst3 = plot.dst2

        Dim barWidth = dst3.Width / task.histogramBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / barWidth)
        dst3.Rectangle(New cv.Rect(CInt(histIndex * barWidth), 0, barWidth, dst3.Height), cv.Scalar.Yellow, task.lineWidth)
        For i = 0 To slotList(histIndex).Count - 1
            Dim rc = task.redCells(slotList(histIndex)(i))
            vbDrawContour(dst2(rc.rect), rc.contour, cv.Scalar.Yellow)
            vbDrawContour(task.color(rc.rect), rc.contour, cv.Scalar.Yellow)
        Next
    End Sub
End Class









Public Class RedCloudY_Features : Inherits VB_Algorithm
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






Public Class RedCloudY_MinMaxNone : Inherits VB_Algorithm
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







Public Class RedCloudY_ShapeCorrelation : Inherits VB_Algorithm
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        desc = "A shape correlation is a correlation between each x and y in list of contours points.  It allows easy classification."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst3
        labels(2) = hulls.labels(2)

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

        showSelection(dst2, task.redCells, task.cellMap)
        setTrueText(strOut, 3)
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





Public Class RedCloudY_FPS : Inherits VB_Algorithm
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

        dst1 = motion.dst1
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

        dst1 = motion.dst1
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
    Dim stats As New ReductionCloud_CellStats
    Public Sub New()
        stats.redC = New RedCloud_Basics
        findRadio("Channels 1 and 2").Checked = True
        desc = "Build horizontal RedCloud cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        stats.Run(src)
        dst2 = stats.dst2
        setTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloudY_SliceV : Inherits VB_Algorithm
    Dim stats As New ReductionCloud_CellStats
    Public Sub New()
        stats.redC = New RedCloud_Basics
        findRadio("Channels 0 and 2").Checked = True
        desc = "Build vertical RedCloud cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        stats.Run(src)
        dst2 = stats.dst2
        setTrueText(stats.strOut, 3)
    End Sub
End Class







Public Class RedCloudY_History : Inherits VB_Algorithm
    Dim hCloud As New History_Cloud
    Dim stats As New ReductionCloud_CellStats
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        stats.redC = New RedCloud_Basics
        desc = "Use the point cloud history instead of the raw pointcloud as input to RedCloud_Basics.  Note better PCA."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hCloud.Run(src)

        stats.Run(hCloud.dst2)
        dst1 = stats.dst1
        dst2 = stats.dst2
        labels(2) = stats.redC.labels(2)
        setTrueText(stats.strOut, 3)
    End Sub
End Class


