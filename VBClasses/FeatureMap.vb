Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class FeatureMap_Basics : Inherits TaskParent
        Dim featD As New Feature_Delaunay
        Dim delaunay As New Delaunay_Basics
        Public rcList As New List(Of rcData)
        Public idMap As New Mat(dst2.Size, MatType.CV_32F, 0)
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Cursor.ai: Build rcData from each Feature_Delaunay cell using the cell rect and a filled mask."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            featD.Run(src)

            If featD.feat.features.Count = 0 Then
                rcList.Clear()
                idMap.SetTo(0)
                Exit Sub
            End If

            delaunay.ptList.Clear()
            For Each pt In featD.feat.features
                delaunay.ptList.Add(New cv.Point2f(pt.X, pt.Y))
            Next

            delaunay.Run(src)

            Dim rcListLast = New List(Of rcData)(rcList)
            Dim idMapLast = idMap.Clone
            Dim usedList As New List(Of Single)
            Dim reusedIndex As Integer

            idMap.SetTo(0)
            rcList.Clear()
            Dim cellMask As New Mat(dst2.Size, MatType.CV_8U, 0)
            If task.heartBeatLT Then dst3.SetTo(0)
            For i = 0 To delaunay.facetList.Count - 1
                Dim facet = delaunay.facetList(i)
                cellMask.SetTo(0)
                FillConvexPoly(cellMask, facet, 255, LineTypes.Link4)

                Dim nz As New Mat
                FindNonZero(cellMask, nz)
                Dim rect = ValidateRect(BoundingRect(nz))

                Dim rc As New rcData(cellMask(rect), rect, 255)
                If rc.pixels = 0 Then Continue For

                Dim prevPt = rc.maxDist
                If i < delaunay.ptList.Count Then
                    Dim p = delaunay.ptList(i)
                    prevPt = New cv.Point(CInt(p.X), CInt(p.Y))
                End If
                If prevPt.X < 0 Then prevPt.X = 0
                If prevPt.Y < 0 Then prevPt.Y = 0
                If prevPt.X >= idMapLast.Width Then prevPt.X = idMapLast.Width - 1
                If prevPt.Y >= idMapLast.Height Then prevPt.Y = idMapLast.Height - 1

                Dim previousIndex = idMapLast.Get(Of Single)(prevPt.Y, prevPt.X)
                If previousIndex <> 0 AndAlso usedList.Contains(previousIndex) = False Then
                    rc.index = previousIndex
                    usedList.Add(rc.index)
                    reusedIndex += 1
                    Dim rcLast = RedC_Basics.rcIndexFind(rcListLast, CInt(rc.index))
                    If rcLast IsNot Nothing Then
                        rc.age = rcLast.age + 1
                        If rc.age >= 1000 Then rc.age = 10
                        Circle(dst3, featD.feat.features(i), task.DotSize, white, -1, task.lineType)
                    Else
                        rc.age = 1
                    End If
                End If
                rcList.Add(rc)
            Next

            Dim nextIndex As Integer = 1
            For Each rc In rcList
                If rc.index = 0 Then
                    While usedList.Contains(nextIndex) Or nextIndex Mod 255 = 0
                        nextIndex += 1
                    End While
                    rc.index = nextIndex
                    usedList.Add(nextIndex)
                    rc.age = 1
                End If
                rc.id = rc.index
                idMap(rc.rect).SetTo(rc.index Mod 255, rc.mask)
            Next

            dst2 = Palettize(idMap)
            For Each pt In featD.feat.features
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next

            labels(2) = CStr(rcList.Count) + " cells from Feature_Delaunay, " + CStr(reusedIndex) +
                        " or " + (reusedIndex / rcList.Count).ToString("0%") + " kept the same color"
            labels(3) = CStr(reusedIndex) + " features with age greater than 1."

            Dim clickIndex = CInt(idMap.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X))
            Dim selected As rcData = Nothing
            For Each rc In rcList
                If rc.index = clickIndex Then
                    selected = rc
                    Exit For
                End If
            Next

            If selected IsNot Nothing Then
                task.rcD = selected
                task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
                Rectangle(task.color, task.rcD.rect, task.highlight, task.lineWidth)
                Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
                Circle(dst2, task.rcD.maxDist, task.DotSize + 1, white, -1)
                strOut = task.rcD.displayCell
                SetTrueText(strOut, 1)
            End If
        End Sub
    End Class





    Public Class FeatureMap_Periphery : Inherits TaskParent
        Public ptOutside As New List(Of Point2f)
        Public ptInside As New List(Of Point2f)
        Public fcs As New FeatureMap_CreateList
        Public Sub New()
            desc = "Display the cells which are on the periphery of the image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(task.gray)
            dst2 = fcs.dst2

            dst3 = dst2.Clone
            ptOutside.Clear()
            ptInside.Clear()
            For Each fp In fcs.fpList
                If fp.periph Then
                    FillConvexPoly(dst3, fp.facets, Scalar.Gray, task.lineType)
                    Circle(dst3, fp.pt, task.DotSize, task.highlight, -1, task.lineType)
                    ptOutside.Add(fp.pt)
                Else
                    ptInside.Add(fp.pt)
                End If
            Next
            ' FeatureMap_Basics.fpDSet()
            labels(2) = "There are " + CStr(ptOutside.Count) + " features on the periphery of the image."
            labels(3) = "There are " + CStr(fcs.fpList.Count - ptOutside.Count) + " features in the interior region of the image."
        End Sub
    End Class




    Public Class FeatureMap_CreateList : Inherits TaskParent
        Dim subdiv As New Subdiv2D
        Public feat As New Feature_Basics
        Dim bricks As New Brick_Basics
        Public fpList As New List(Of fpData)
        Public Sub New()
            dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
            labels(3) = "Visualization of the map of feature points."
            desc = "Subdivide an image based on the points provided."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.Run(src)

            feat.Run(task.gray)

            subdiv.InitDelaunay(New cv.Rect(0, 0, dst2.Width, dst2.Height))
            Dim features As New List(Of Point2f)
            For Each pt In feat.features
                features.Add(pt)
            Next
            subdiv.Insert(features)

            Dim facets = New Point2f()() {Nothing}
            subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

            fpList.Clear()
            dst2.SetTo(0)
            For i = 0 To Math.Min(feat.features.Count, facets.Length) - 1
                Dim fp As New fpData With {.pt = feat.features(i), .index = i}
                fp.ptHistory.Add(fp.pt)

                Dim brickIndex = task.gridMap.Get(Of Integer)(fp.pt.Y, fp.pt.X)
                Dim brick = bricks.brickList(brickIndex)
                fp.brickIndex = brickIndex

                fp.facets = New List(Of cv.Point)
                Dim xlist As New List(Of Integer), ylist As New List(Of Integer)
                For j = 0 To facets(i).Length - 1
                    Dim pt = New cv.Point(facets(i)(j).X, facets(i)(j).Y)
                    xlist.Add(pt.X)
                    ylist.Add(pt.Y)
                    fp.facets.Add(New cv.Point(facets(i)(j).X, facets(i)(j).Y))
                Next

                Dim minX = xlist.Min, minY = ylist.Min, maxX = xlist.Max, maxY = ylist.Max

                If minX < 0 Or minY < 0 Or maxX >= dst2.Width Or maxY >= dst2.Height Then fp.periph = True

                fp.depth = brick.depth

                fpList.Add(fp)

                FillConvexPoly(dst2, fp.facets, i Mod 256, task.lineType)
            Next

            dst3 = Palettize(dst2)
            For Each fp In fpList
                If fp.depth > 0 Then Circle(dst3, fp.pt, task.DotSize, task.highlight, -1, task.lineType)
            Next

            If standalone And task.fpD IsNot Nothing Then XR_FeatureMap_Basics.fpCellContour(task.fpD, task.color)
            If task.heartBeat Then labels(2) = traceName + ": " + feat.features.Count.ToString("000") + " cells found " +
                                           "using " + task.fOptions.FeatureMethod.Text
        End Sub
    End Class





    Public Class FeatureMap_ByDepth : Inherits TaskParent
        Dim plotHist As New PlotBar_Basics
        Dim featMap As New FeatureMap_CreateList
        Dim palInput As New Mat(dst2.Size, MatType.CV_8U, 0)
        Dim bPoint As New BrickPoint_Basics
        Public Sub New()
            plotHist.addLabels = False
            plotHist.removeZeroEntry = True
            plotHist.createHistogram = True
            If standalone Then task.gOptions.showMyDst1.Checked = True
            task.gOptions.setHistogramBins(20)
            desc = "Use cell depth to break down the layers in an image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bPoint.Run(src)
            featMap.feat.features = New List(Of cv.Point)(bPoint.ptList)

            featMap.Run(src)
            dst2 = featMap.dst2
            labels(2) = featMap.labels(2)

            Dim dBricks As New List(Of Single)
            For Each fp In featMap.fpList
                dBricks.Add(fp.depth)
            Next

            plotHist.minRange = 0
            plotHist.maxRange = task.MaxZmeters
            plotHist.Run(Mat.FromPixelData(dBricks.Count, 1, MatType.CV_32F, dBricks.ToArray))
            dst1 = plotHist.dst2

            Dim incr = dst1.Width / task.histogramBins
            Dim histIndex = Math.Truncate(task.mouseMovePoint.X / incr)
            Rectangle(dst1, New cv.Rect(CInt(histIndex * incr), 0, incr, dst2.Height), Scalar.Yellow, task.lineWidth)
            Dim depthIncr = (plotHist.maxRange - plotHist.minRange) / task.histogramBins
            Dim depthStart = histIndex * depthIncr
            Dim depthEnd = (histIndex + 1) * depthIncr

            Static fpCells As New List(Of (fpData, Integer))
            Static histIndexSave = histIndex

            If histIndexSave <> histIndex Or task.optionsChanged Then
                histIndexSave = histIndex
                fpCells.Clear()
            End If
            palInput.SetTo(0)

            For Each fp In featMap.fpList
                If fp.depth > depthStart And fp.depth < depthEnd Then
                    Dim val = palInput.Get(Of Byte)(fp.pt.Y, fp.pt.X)
                    If val = 0 Then
                        FillConvexPoly(palInput, fp.facets, fp.brickIndex Mod 255)
                        fpCells.Add((fp, task.fOptions.FrameHistoryCount.Value))
                    End If
                End If
            Next

            For Each ele In fpCells
                Dim fp As fpData = ele.Item1
                XR_FeatureMap_Basics.fpCellContour(fp, task.color, 0)
            Next
            dst3 = Palettize(palInput)
            Dim tmp As New Mat
            Threshold(palInput, tmp, 0, Scalar.All(255), ThresholdTypes.BinaryInv)
            dst3.SetTo(Scalar.All(0), tmp)


            Dim removeFrame As Integer = If(task.fOptions.FrameHistoryCount.Value > task.fOptions.FrameHistoryCount.Value, task.fOptions.FrameHistoryCount.Value - task.fOptions.FrameHistoryCount.Value, -1)
            For i = fpCells.Count - 1 To 0 Step -1
                Dim frame = fpCells(i).Item2
                If frame = removeFrame Then fpCells.RemoveAt(i)
            Next

            labels(3) = "Cells with depth between " + depthStart.ToString(fmt1) + "m to " + depthEnd.ToString(fmt1) + "m"
        End Sub
    End Class




    Public Class FeatureMap_Tracker1 : Inherits TaskParent
        Public shiftXY As New cv.Point2f
        Public rotateAngle As Single
        Public agreeCount As Integer
        Dim featMap As New FeatureMap_Basics
        Dim refPt As New Dictionary(Of Integer, cv.Point2f)
        Const sameTol As Single = 5.0F
        Const minAgree As Integer = 4
        Private Structure trackDisp
            Dim pref As cv.Point2f
            Dim pnow As cv.Point2f
            Dim delta As cv.Point2f
        End Structure
        Public Sub New()
            desc = "Cursor.ai: Measure camera X, Y, and rotation from FeatureMap_Basics cells that share a displacement since heartBeatLT."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            featMap.Run(src)
            dst2 = featMap.dst2

            If task.heartBeatLT Or task.firstPass Then
                refPt.Clear()
                For Each rc In featMap.rcList
                    If rc.index = 0 Then Continue For
                    refPt(rc.index) = New cv.Point2f(rc.maxDist.X, rc.maxDist.Y)
                Next
                shiftXY = New cv.Point2f
                rotateAngle = 0
                agreeCount = refPt.Count
            End If

            Dim samples As New List(Of trackDisp)
            For Each rc In featMap.rcList
                If rc.index = 0 OrElse refPt.ContainsKey(rc.index) = False Then Continue For
                Dim pref = refPt(rc.index)
                Dim pnow = New cv.Point2f(rc.maxDist.X, rc.maxDist.Y)
                samples.Add(New trackDisp With {
                    .pref = pref,
                    .pnow = pnow,
                    .delta = New cv.Point2f(pnow.X - pref.X, pnow.Y - pref.Y)
                })
            Next

            Dim bestI As Integer = -1
            Dim bestCount As Integer = 0
            If samples.Count = 0 Then
                shiftXY = New cv.Point2f
                rotateAngle = 0
                agreeCount = 0
            End If
            For i = 0 To samples.Count - 1
                Dim n = 0
                For j = 0 To samples.Count - 1
                    Dim dx = samples(i).delta.X - samples(j).delta.X
                    Dim dy = samples(i).delta.Y - samples(j).delta.Y
                    If dx * dx + dy * dy <= sameTol * sameTol Then n += 1
                Next
                If n > bestCount Then
                    bestCount = n
                    bestI = i
                End If
            Next

            Dim inlier(samples.Count - 1) As Boolean
            agreeCount = 0
            If bestI >= 0 AndAlso bestCount >= minAgree Then
                Dim sx As Single, sy As Single
                For i = 0 To samples.Count - 1
                    Dim dx = samples(i).delta.X - samples(bestI).delta.X
                    Dim dy = samples(i).delta.Y - samples(bestI).delta.Y
                    If dx * dx + dy * dy <= sameTol * sameTol Then
                        inlier(i) = True
                        agreeCount += 1
                        sx += samples(i).delta.X
                        sy += samples(i).delta.Y
                    End If
                Next
                shiftXY = New cv.Point2f(sx / agreeCount, sy / agreeCount)

                Dim center = New cv.Point2f(dst2.Width / 2.0F, dst2.Height / 2.0F)
                Dim angles As New List(Of Single)
                For i = 0 To samples.Count - 1
                    If inlier(i) = False Then Continue For
                    Dim v0 = New cv.Point2f(samples(i).pref.X - center.X, samples(i).pref.Y - center.Y)
                    Dim v1 = New cv.Point2f(samples(i).pnow.X - shiftXY.X - center.X, samples(i).pnow.Y - shiftXY.Y - center.Y)
                    If v0.X * v0.X + v0.Y * v0.Y < 25 OrElse v1.X * v1.X + v1.Y * v1.Y < 25 Then Continue For
                    Dim cross = v0.X * v1.Y - v0.Y * v1.X
                    Dim dot = v0.X * v1.X + v0.Y * v1.Y
                    angles.Add(CSng(Math.Atan2(cross, dot)))
                Next
                rotateAngle = 0
                If angles.Count > 0 Then
                    angles.Sort()
                    rotateAngle = CSng(angles(angles.Count \ 2) * RadToDeg)
                End If
            Else
                shiftXY = New cv.Point2f
                rotateAngle = 0
                agreeCount = bestCount
            End If

            dst3 = task.color.Clone
            For i = 0 To samples.Count - 1
                Dim color = If(inlier(i), task.highlight, Scalar.Gray)
                Line(dst3, samples(i).pref, samples(i).pnow, color, task.lineWidth, task.lineType)
                Circle(dst3, samples(i).pnow, task.DotSize, color, -1, task.lineType)
            Next
            Dim origin = New cv.Point(dst3.Width \ 2, dst3.Height \ 2)
            Dim tip = New cv.Point(CInt(origin.X + shiftXY.X), CInt(origin.Y + shiftXY.Y))
            Line(dst3, origin, tip, white, task.lineWidth + 1, task.lineType)
            Circle(dst3, tip, task.DotSize, white, -1, task.lineType)

            labels(2) = featMap.labels(2)
            If agreeCount >= minAgree Then
                labels(3) = "Camera motion x=" + shiftXY.X.ToString(fmt1) + " y=" + shiftXY.Y.ToString(fmt1) +
                            " rotation=" + rotateAngle.ToString(fmt1) + " deg from " + CStr(agreeCount) + " cells"
            Else
                labels(3) = CStr(agreeCount) + " cells share a displacement since heartBeatLT"
            End If
        End Sub
    End Class





    Public Class FeatureMap_Tracker : Inherits TaskParent
        Dim featMap As New FeatureMap_Basics
        Dim rcSnap As New List(Of rcData)
        Public Sub New()
            desc = "Track the features since the last heartbeat to find a trend."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            featMap.Run(src)
            dst2 = featMap.dst2
            labels(2) = featMap.labels(2)

            If task.heartBeatLT Then
                rcSnap = New List(Of rcData)(featMap.rcList)
            End If


        End Sub
    End Class
End Namespace
