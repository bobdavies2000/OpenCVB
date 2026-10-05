Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class FeatureMap_Basics : Inherits TaskParent
        Dim featDel As New Feature_Delaunay
        Dim delaunay As New Delaunay_Basics
        Public rcList As New List(Of rcData)
        Public idMap As New Mat(dst2.Size, MatType.CV_32F, 0)
        Public Sub New()
            delaunay.useFeatures = False
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Cursor.ai: Build rcData from each Feature_Delaunay cell using the cell rect and a filled mask."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            featDel.Run(src)
            dst3 = featDel.dst3
            labels(3) = featDel.labels(3)

            delaunay.ptList.Clear()
            For Each pt In featDel.feat.features
                delaunay.ptList.Add(New cv.Point2f(pt.X, pt.Y))
            Next

            If delaunay.ptList.Count = 0 Then
                rcList.Clear()
                idMap.SetTo(0)
                Exit Sub
            End If

            delaunay.Run(src)

            Dim rcListLast = New List(Of rcData)(rcList)
            Dim idMapLast = idMap.Clone
            Dim usedList As New List(Of Single)
            Dim reusedIndex As Integer

            idMap.SetTo(0)
            rcList.Clear()
            Dim cellMask As New Mat(dst2.Size, MatType.CV_8U, 0)
            For i = 0 To delaunay.facetList.Count - 1
                Dim facet = delaunay.facetList(i)
                If facet.Count < 3 Then Continue For

                cellMask.SetTo(0)
                FillConvexPoly(cellMask, facet, 255, LineTypes.Link4)
                If CountNonZero(cellMask) = 0 Then Continue For

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
                        If rc.age >= 1000 Then rc.age = 100
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
            For Each pt In featDel.feat.features
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next
            labels(2) = CStr(rcList.Count) + " cells from Feature_Delaunay, " + CStr(reusedIndex) +
                        " or " + (reusedIndex / rcList.Count).ToString("0%") + " kept the same color"

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





    Public Class XR_FeatureMap_Basics : Inherits TaskParent
        Dim bricks As New Brick_Basics
        Public basics As New XR_FeatureMap_StablePoints
        Public genSorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        Public Sub New()
            dst1 = New Mat(dst1.Size(), MatType.CV_8U, Scalar.All(0))
            desc = "Track the stable good features found in the BGR image."
        End Sub
        'Public Shared Sub fpDSet()
        '    If fcs.fplist.Count = 0 Then Exit Sub
        '    Dim brickIndex = task.fpMap.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)
        '    Dim fpIndex = task.fpFromGridCell.IndexOf(brickIndex)
        '    If fpIndex >= 0 Then task.fpD = fcs.fplist(fpIndex)
        'End Sub
        Public Shared Sub fpCellContour(fp As fpData, dst As Mat, Optional colorIndex As Integer = 0)
            Dim color = Choose(colorIndex + 1, Scalar.White, Scalar.Black)
            For i = 0 To fp.facets.Count - 1
                Dim p1 = fp.facets(i)
                Dim p2 = fp.facets((i + 1) Mod fp.facets.Count)
                Line(dst, p1, p2, color, task.lineWidth, task.lineType)
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.Run(src)
            basics.Run(src)
            dst3 = basics.dst3
            labels(3) = basics.labels(3)
            If basics.ptList.Count = 0 Then Exit Sub ' nothing to work on...

            basics.facetGen.ptList = New List(Of Point2f)(basics.ptList)
            basics.Run(src)
            dst2 = basics.dst2

            dst1.SetTo(0)
            genSorted.Clear()
            For i = 0 To basics.ptList.Count - 1
                Dim pt = basics.ptList(i)
                If standaloneTest() Then Circle(dst2, pt, task.DotSize + 1, Scalar.Yellow, -1, task.lineType)
                dst1.Set(Of Byte)(pt.Y, pt.X, 255)

                Dim g = basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                genSorted.Add(g, i)
                SetTrueText(CStr(g), pt)
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next
            labels(2) = basics.labels(2)
            labels(3) = CStr(basics.ptList.Count) + " stable good features were found"
        End Sub
    End Class






    Public Class XR_FeatureMap_StablePoints : Inherits TaskParent
        Public facetGen As New Delaunay_Generations
        Public ptList As New List(Of Point2f)
        Public anchorPoint As Point2f
        Dim good As New Feature_KNN
        Public Sub New()
            desc = "Maintain the generation counts around the feature points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            good.Run(src)
            facetGen.ptList.Clear()
            For Each pt In good.feat.features
                facetGen.ptList.Add(pt)
            Next

            facetGen.Run(src)
            If facetGen.ptList.Count = 0 Then
                dst2.SetTo(0)
                dst3.SetTo(0)
                Exit Sub ' nothing to work on ...
            End If

            ptList.Clear()
            Dim generations As New List(Of Integer)
            For Each pt In facetGen.ptList
                Dim fIndex = facetGen.facet.dst3.Get(Of Integer)(pt.Y, pt.X)
                If fIndex >= facetGen.facet.facetList.Count Then Continue For ' new cv.Point
                Dim g = facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                generations.Add(g)
                ptList.Add(pt)
                SetTrueText(CStr(g), pt)
            Next

            If generations.Count = 0 Then Exit Sub

            Dim maxGens = generations.Max()
            Dim index = generations.IndexOf(maxGens)
            anchorPoint = ptList(index)
            If index < facetGen.facet.facetList.Count Then
                Dim bestFacet = facetGen.facet.facetList(index)
                FillConvexPoly(dst2, bestFacet, Scalar.Black, task.lineType)
                DrawTour(dst2, bestFacet, task.highlight)
            End If

            dst2 = facetGen.dst2
            dst3 = src.Clone
            For i = 0 To ptList.Count - 1
                Dim pt = ptList(i)
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
                Circle(dst3, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next
            labels(2) = CStr(ptList.Count) + " stable points were identified with a max of " + CStr(maxGens) +
                        " generations."
        End Sub
    End Class







    Public Class XR_FeatureMap_ViewLeft : Inherits TaskParent
        Dim fcs As New FeatureMap_CreateList
        Public Sub New()
            desc = "Build an FCS for left view."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(task.leftView)
            dst2 = fcs.dst2
            dst3 = fcs.dst3

            labels = fcs.labels
        End Sub
    End Class







    Public Class XR_FeatureMap_ViewRight : Inherits TaskParent
        Dim fcs As New FeatureMap_CreateList
        Public Sub New()
            desc = "Build an FCS for right view."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(task.rightView)
            dst2 = fcs.dst2
            dst3 = fcs.dst3

            labels = fcs.labels
        End Sub
    End Class





    Public Class XR_FeatureMap_Edges : Inherits TaskParent
        Dim fcs As New FeatureMap_CreateList
        Public Sub New()
            desc = "Use edges to connect feature points to their neighbors."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(src)
            dst2 = src

            CvtColor(task.edges.dst2, dst3, ColorConversionCodes.GRAY2BGR)
            For Each fp In fcs.fpList
                If fp.depth Then
                    Circle(dst2, fp.pt, task.DotSize + 3, task.highlight, -1, task.lineType)
                    Circle(dst3, fp.pt, task.DotSize + 3, task.highlight, -1, task.lineType)
                End If
            Next
            labels = fcs.labels
        End Sub
    End Class





    Public Class XR_FeatureMap_WithAge : Inherits TaskParent
        Dim fcs As New FeatureMap_CreateList
        Public Sub New()
            desc = "Display the age of each cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(task.gray)
            dst2 = fcs.dst2
            labels = fcs.labels

            dst3.SetTo(0)
            For Each fp In fcs.fpList
                Circle(dst3, fp.pt, task.DotSize, task.highlight, -1, task.lineType)
                If fp.age >= 1000 Then fp.age = 2
            Next
        End Sub
    End Class





    Public Class XR_FeatureMap_BestAge : Inherits TaskParent
        Dim fcs As New FeatureMap_CreateList
        Public Sub New()
            labels(3) = "Ages are kept below 1000 to make the output more readable..."
            desc = "Display the top X oldest (best) cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(task.gray)
            dst2 = fcs.dst2
            labels(2) = fcs.labels(2)

            Dim fpSorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
            For Each fp In fcs.fpList
                fpSorted.Add(fp.age, fp.index)
            Next

            dst3.SetTo(0)
            Dim maxIndex As Integer = 0
            For Each index In fpSorted.Values
                Dim fp = fcs.fpList(index)
                Circle(dst3, fp.pt, task.DotSize, task.highlight, -1, task.lineType)
                If fp.age >= 1000 Then fp.age = 2
                maxIndex += 1
                If maxIndex >= 10 Then Exit For
            Next
        End Sub
    End Class






    Public Class XR_FeatureMap_MotionDirection : Inherits TaskParent
        Dim fcsM As New XR_FeatureMap_Motion
        Dim plothist As New PlotBar_Basics
        Dim mats As New Mat_4Click
        Dim range As Integer, rangeText As String
        Public Sub New()
            plothist.createHistogram = True
            plothist.addLabels = False
            task.gOptions.setHistogramBins(64) ' should this be an odd number.
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Using all the feature points with motion, determine any with a common direction."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcsM.Run(src)
            mats.mat(2) = fcsM.dst2
            mats.mat(3) = fcsM.dst3

            plothist.maxRange = task.histogramBins / 2 Or 1
            plothist.minRange = -plothist.maxRange
            rangeText = " ranging from " + CStr(plothist.minRange) + " to " + CStr(plothist.maxRange)
            range = Math.Abs(plothist.maxRange - plothist.minRange)

            Dim incr = range / task.histogramBins

            plothist.Run(Mat.FromPixelData(fcsM.xDist.Count, 1, MatType.CV_32F, fcsM.xDist.ToArray))
            Dim xDist As New List(Of Single)(plothist.histArray)
            task.motionFeatures.X = plothist.minRange + xDist.IndexOf(xDist.Max) * incr
            mats.mat(0) = plothist.dst2.Clone

            plothist.Run(Mat.FromPixelData(fcsM.yDist.Count, 1, MatType.CV_32F, fcsM.yDist.ToArray))
            Dim yDist As New List(Of Single)(plothist.histArray)
            task.motionFeatures.Y = plothist.minRange + yDist.IndexOf(yDist.Max) * incr
            mats.mat(1) = plothist.dst2.Clone

            mats.Run(emptyMat)
            dst2 = mats.dst2
            dst3 = mats.dst3

            If fcsM.motionPercent < 50 Then
                task.motionFeatures.X = 0
                task.motionFeatures.Y = 0
            End If

            strOut = "CameraMotion estimate: " + vbCrLf + vbCrLf
            strOut += "Displacement in X: " + CStr(task.motionFeatures.X) + vbCrLf
            strOut += "Displacement in Y: " + CStr(task.motionFeatures.Y) + vbCrLf

            SetTrueText(strOut, 1)
            SetTrueText("X distances" + rangeText, 2)
            SetTrueText("Y distances " + rangeText, New cv.Point(dst2.Width / 2 + 2, 0), 2)
            labels = fcsM.labels
            ' FeatureMap_Basics.fpDSet()
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





    Public Class XR_FeatureMap_PeripheryNot : Inherits TaskParent
        Dim perif As New FeatureMap_Periphery
        Public Sub New()
            dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
            desc = "Create a mask for the cells which are not on the periphery of the image - the interior region that is fully visible and connected."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            perif.Run(task.gray)
            dst2 = perif.dst3

            dst3.SetTo(0)
            For Each fp In perif.fcs.fpList
                If fp.periph = False Then FillConvexPoly(dst3, fp.facets, 255, task.lineType)
            Next
            ' FeatureMap_Basics.fpDSet()
            labels = perif.labels
        End Sub
    End Class






    Public Class XR_FeatureMap_BrickPoints : Inherits TaskParent
        Public facetGen As New Delaunay_Generations
        Public ptList As New List(Of Point2f)
        Public anchorPoint As Point2f
        Dim good As New Feature_KNN
        Public Sub New()
            desc = "Maintain the generation counts around the feature points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            good.Run(src)
            For Each pt In good.feat.features
                facetGen.ptList.Add(pt)
            Next

            facetGen.Run(src)
            If facetGen.ptList.Count = 0 Then
                dst2.SetTo(0)
                dst3.SetTo(0)
                Exit Sub ' nothing to work on ...
            End If

            ptList.Clear()
            Dim generations As New List(Of Integer)
            For Each pt In facetGen.ptList
                Dim fIndex = facetGen.facet.dst3.Get(Of Integer)(pt.Y, pt.X)
                If fIndex >= facetGen.facet.facetList.Count Then Continue For ' new cv.Point
                Dim g = facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                generations.Add(g)
                ptList.Add(pt)
                SetTrueText(CStr(g), pt)
            Next

            If generations.Count = 0 Then Exit Sub

            Dim maxGens = generations.Max()
            Dim index = generations.IndexOf(maxGens)
            anchorPoint = ptList(index)
            If index < facetGen.facet.facetList.Count Then
                Dim bestFacet = facetGen.facet.facetList(index)
                FillConvexPoly(dst2, bestFacet, Scalar.Black, task.lineType)
                DrawTour(dst2, bestFacet, task.highlight)
            End If

            dst2 = facetGen.dst2
            dst3 = src.Clone
            For i = 0 To ptList.Count - 1
                Dim pt = ptList(i)
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
                Circle(dst3, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next
            labels(2) = CStr(ptList.Count) + " stable points were identified with a max of " + CStr(maxGens) +
                        " generations."
        End Sub
    End Class






    Public Class XR_FeatureMap_Motion : Inherits TaskParent
        Dim fcs As New FeatureMap_CreateList
        Dim plot As New PlotTime_Basics
        Public xDist As New List(Of Single), yDist As New List(Of Single)
        Public motionPercent As Single
        Public Sub New()
            plot.maxScale = 100
            plot.minScale = 0
            plot.plotCount = 1
            If standalone Then task.gOptions.showMyDst1.Checked = True
            labels(1) = "Plot of % of cells that moved - move camera to see value."
            desc = "Highlight the motion of each feature identified in the current and previous frame"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(task.gray)
            dst2 = fcs.dst2

            For Each fp In fcs.fpList
                If fp.depth > 0 Then Circle(dst2, fp.pt, task.DotSize, task.highlight, -1, task.lineType)
            Next

            Dim motionCount As Integer, linkedCount As Integer
            xDist.Clear()
            yDist.Clear()
            xDist.Add(0)
            yDist.Add(0)
            dst3.SetTo(0)
            For Each fp In fcs.fpList
                'Dim brickIndex = task.gridMap.Get(Of Integer)(fp.pt.Y, fp.pt.X)
                'Dim fpIndex = task.fpFromGridCellLast.IndexOf(brickIndex)
                'If fpIndex >= 0 Then
                '    linkedCount += 1
                '    dst3.Line(fp.pt, fp.ptLast, task.highlight, task.lineWidth, task.lineType)
                'End If
                'If fp.ptLast <> new cv.Point Then
                '    motionCount += 1
                '    xDist.Add(fp.ptLast.X - fp.pt.X)
                '    yDist.Add(fp.ptLast.Y - fp.pt.Y)
                'End If
            Next
            motionPercent = 100 * motionCount / linkedCount
            If task.heartBeat Then
                labels(2) = fcs.labels(2)
                labels(3) = motionPercent.ToString(fmt1) + "% of linked cells had motion or " +
                            CStr(motionCount) + " of " + CStr(linkedCount) + ".  Distance moved X/Y " +
                            xDist.Average.ToString(fmt1) + "/" + yDist.Average.ToString(fmt1) +
                            " pixels."
            End If

            plot.plotData = New Scalar(motionPercent, 0, 0)
            plot.Run(src)
            dst1 = plot.dst2
            ' FeatureMap_Basics.fpDSet()
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
End Namespace
