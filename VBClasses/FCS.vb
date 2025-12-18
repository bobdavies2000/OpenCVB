Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class FCS_Basics : Inherits TaskParent
        Public basics As New FCS_StablePoints
        Public genSorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Track the stable good features found in the BGR image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            basics.Run(src)
            dst3 = basics.dst3
            labels(3) = basics.labels(3)
            If basics.ptList.Count = 0 Then Exit Sub ' nothing to work on...

            basics.facetGen.inputPoints = New List(Of cv.Point2f)(basics.ptList)
            basics.Run(src)
            dst2 = basics.dst2

            dst1.SetTo(0)
            genSorted.Clear()
            For i = 0 To basics.ptList.Count - 1
                Dim pt = basics.ptList(i)
                If standaloneTest() Then DrawCircle(dst2, pt, algTask.DotSize + 1, cv.Scalar.Yellow)
                dst1.Set(Of Byte)(pt.Y, pt.X, 255)

                Dim g = basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                genSorted.Add(g, i)
                SetTrueText(CStr(g), pt)
                DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
            Next
            labels(2) = basics.labels(2)
            labels(3) = CStr(basics.ptList.Count) + " stable good features were found"
        End Sub
    End Class






    Public Class FCS_StablePoints : Inherits TaskParent
        Public facetGen As New Delaunay_Generations
        Public ptList As New List(Of cv.Point2f)
        Public anchorPoint As cv.Point2f
        Dim good As New Feature_KNN
        Public Sub New()
            desc = "Maintain the generation counts around the feature points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            good.Run(src)
            facetGen.inputPoints = New List(Of cv.Point2f)(good.featurePoints)

            facetGen.Run(src)
            If facetGen.inputPoints.Count = 0 Then
                dst2.SetTo(0)
                dst3.SetTo(0)
                Exit Sub ' nothing to work on ...
            End If

            ptList.Clear()
            Dim generations As New List(Of Integer)
            For Each pt In facetGen.inputPoints
                Dim fIndex = facetGen.facet.dst3.Get(Of Integer)(pt.Y, pt.X)
                If fIndex >= facetGen.facet.facetList.Count Then Continue For ' new point
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
                dst2.FillConvexPoly(bestFacet, cv.Scalar.Black, algTask.lineType)
                DrawTour(dst2, bestFacet, algTask.highlight)
            End If

            dst2 = facetGen.dst2
            dst3 = src.Clone
            For i = 0 To ptList.Count - 1
                Dim pt = ptList(i)
                DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
                DrawCircle(dst3, pt, algTask.DotSize, algTask.highlight)
            Next
            labels(2) = CStr(ptList.Count) + " stable points were identified with a max of " + CStr(maxGens) +
                    " generations."
        End Sub
    End Class





    Public Class FCS_BasicsOld : Inherits TaskParent
        Dim fcs As New FCS_Core
        Public desiredMapCount As Integer = 5
        Public Sub New()
            If algTask.contours Is Nothing Then algTask.contours = New Contour_Basics_List
            desc = "Create the reference map for FCS. "
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            algTask.contours.Run(src)
            Static restartRequest As Boolean
            fcs.inputFeatures.Clear()
            For Each contour In algTask.contours.contourList
                fcs.inputFeatures.Add(GetMaxDist(contour.mask, contour.rect))
            Next
            If algTask.contours.contourList.Count <= 1 Then ' when the camera is starting up the image may be too dark to process... Restart if so.
                restartRequest = True
                Exit Sub
            End If
            restartRequest = False

            fcs.Run(emptyMat)

            dst2 = ShowPaletteFullColor(fcs.fcsMap)
            dst3 = algTask.contours.dst2
            labels(2) = fcs.labels(2)
            labels(3) = algTask.contours.labels(2)
        End Sub
    End Class






    Public Class FCS_Core : Inherits TaskParent
        Dim subdiv As New cv.Subdiv2D
        Public inputFeatures As New List(Of cv.Point2f)
        Public fcsMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            desc = "Subdivide an image based on the points provided."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            subdiv.InitDelaunay(New cv.Rect(0, 0, fcsMap.Width, fcsMap.Height))
            subdiv.Insert(inputFeatures)

            Dim facets = New cv.Point2f()() {Nothing}
            subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

            For i = 0 To Math.Min(inputFeatures.Count, facets.Count) - 1
                Dim facetList = New List(Of cv.Point)
                For Each pt In facets(i)
                    facetList.Add(New cv.Point(pt.X, pt.Y))
                Next
                fcsMap.FillConvexPoly(facetList, i, cv.LineTypes.Link8)
            Next

            If standaloneTest() Then dst2 = PaletteFull(fcsMap)

            If algTask.heartBeat Then labels(2) = traceName + ": " + CStr(inputFeatures.Count) + " cells found."
        End Sub
    End Class






    Public Class FCS_CreateList : Inherits TaskParent
        Dim subdiv As New cv.Subdiv2D
        Dim feat As New Feature_General
        Public Sub New()
            If algTask.bricks Is Nothing Then algTask.bricks = New Brick_Basics
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            algTask.fpMap = New cv.Mat(dst2.Size(), cv.MatType.CV_32F, 0)
            labels(3) = "CV_8U map of Delaunay cells."
            desc = "Subdivide an image based on the points provided."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(algTask.grayStable)

            subdiv.InitDelaunay(New cv.Rect(0, 0, dst1.Width, dst1.Height))
            subdiv.Insert(algTask.features)

            Dim facets = New cv.Point2f()() {Nothing}
            subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

            algTask.fpList.Clear()
            Dim matchCount As Integer
            dst1.SetTo(0)
            For i = 0 To Math.Min(algTask.features.Count, facets.Count) - 1
                Dim fp As New fpData
                fp.pt = algTask.features(i)
                fp.ptHistory.Add(fp.pt)
                fp.index = i

                Dim brickIndex = algTask.gridMap.Get(Of Integer)(fp.pt.Y, fp.pt.X)
                Dim brick = algTask.bricks.brickList(brickIndex)
                Dim fpIndex = algTask.fpFromGridCellLast.IndexOf(brickIndex)
                If fpIndex >= 0 Then
                    Dim fpLast = algTask.fpLastList(fpIndex)
                    fp.ptLast = fpLast.pt
                    fp.age = fpLast.age + 1
                    matchCount += 1
                End If

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

                algTask.fpList.Add(fp)

                algTask.fpMap.FillConvexPoly(fp.facets, CSng(brickIndex), algTask.lineType)
                dst1.FillConvexPoly(fp.facets, i, algTask.lineType)
            Next

            If algTask.features.Count <> facets.Length Then
                algTask.fpFromGridCell.Clear()
                For Each fp In algTask.fpList
                    Dim nextIndex = algTask.gridMap.Get(Of Integer)(fp.pt.Y, fp.pt.X)
                    algTask.fpFromGridCell.Add(nextIndex)
                Next
            End If

            dst2 = PaletteFull(dst1)
            For Each fp In algTask.fpList
                If fp.depth > 0 Then DrawCircle(dst2, fp.pt, algTask.DotSize, algTask.highlight)
            Next

            If standalone Then
                fpDisplayAge()
                fpCellContour(algTask.fpD, algTask.color)
            End If
            fpDSet()

            If algTask.heartBeat Then
                labels(2) = traceName + ": " + Format(algTask.features.Count, "000") + " cells found.  Matched = " +
                        CStr(matchCount) + " of " + CStr(algTask.features.Count)
            End If
        End Sub
    End Class








    Public Class FCS_ViewLeft : Inherits TaskParent
        Dim fcs As New FCS_CreateList
        Public Sub New()
            desc = "Build an FCS for left view."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(algTask.leftView)
            dst2 = fcs.dst2
            dst3 = fcs.dst3

            fpDisplayAge()
            fpDSet()
            labels(2) = fcs.labels(2)
        End Sub
    End Class







    Public Class FCS_ViewRight : Inherits TaskParent
        Dim fcs As New FCS_CreateList
        Public Sub New()
            desc = "Build an FCS for right view."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(algTask.rightView)
            dst2 = fcs.dst2
            dst3 = fcs.dst3

            fpDisplayAge()
            fpDSet()
            labels(2) = fcs.labels(2)
        End Sub
    End Class







    Public Class FCS_FloodFill : Inherits TaskParent
        Dim flood As New Flood_Basics
        Dim edges As New Edge_Canny
        Public Sub New()
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            desc = "Use color to connect FCS cells - visualize the data mostly."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            flood.Run(src)
            dst2 = flood.dst2

            dst1 = src.Clone

            edges.Run(src)
            dst3 = edges.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            For i = 0 To algTask.fpList.Count - 1
                Dim fp = algTask.fpList(i)
                DrawCircle(dst1, fp.pt, algTask.DotSize, algTask.highlight)
                DrawCircle(dst2, fp.pt, algTask.DotSize, algTask.highlight)

                algTask.fpList(i) = fp
                DrawCircle(dst3, fp.pt, algTask.DotSize, algTask.highlight)
            Next
        End Sub
    End Class









    Public Class FCS_Edges : Inherits TaskParent
        Dim fcs As New FCS_CreateList
        Dim edges As New Edge_Canny
        Public Sub New()
            desc = "Use edges to connect feature points to their neighbors."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(src)
            dst2 = src

            edges.Run(src)
            dst3 = edges.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For Each fp In algTask.fpList
                If fp.depth Then
                    DrawCircle(dst2, fp.pt, algTask.DotSize, algTask.highlight)
                    DrawCircle(dst3, fp.pt, algTask.DotSize, algTask.highlight)
                End If
            Next
            fpDSet()
        End Sub
    End Class





    Public Class FCS_WithAge : Inherits TaskParent
        Dim fcs As New FCS_CreateList
        Public Sub New()
            labels(3) = "Ages are kept below 1000 to make the output more readable..."
            desc = "Display the age of each cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(algTask.grayStable)
            dst2 = fcs.dst2
            labels(2) = fcs.labels(2)

            dst3.SetTo(0)
            For Each fp In algTask.fpList
                DrawCircle(dst3, fp.pt, algTask.DotSize, algTask.highlight)
                Dim age = If(fp.age >= 900, fp.age Mod 900 + 100, fp.age)
                SetTrueText(CStr(age), fp.pt, 3)
            Next
        End Sub
    End Class





    Public Class FCS_BestAge : Inherits TaskParent
        Dim fcs As New FCS_CreateList
        Public Sub New()
            labels(3) = "Ages are kept below 1000 to make the output more readable..."
            desc = "Display the top X oldest (best) cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(algTask.grayStable)
            dst2 = fcs.dst2
            labels(2) = fcs.labels(2)

            Dim fpSorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
            For Each fp In algTask.fpList
                fpSorted.Add(fp.age, fp.index)
            Next

            dst3.SetTo(0)
            Dim maxIndex As Integer = 0
            For Each index In fpSorted.Values
                Dim fp = algTask.fpList(index)
                DrawCircle(dst3, fp.pt, algTask.DotSize, algTask.highlight)
                Dim age = If(fp.age >= 900, fp.age Mod 900 + 100, fp.age)
                SetTrueText(CStr(age), fp.pt, 3)
                maxIndex += 1
                If maxIndex >= 10 Then Exit For
            Next
        End Sub
    End Class






    Public Class FCS_RedCloud1 : Inherits TaskParent
        Dim fcs As New FCS_CreateList
        Public Sub New()
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            labels(1) = "Output of FCS_CreateList."
            desc = "Isolate FCS cells for each redCell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            fcs.Run(src)
            dst1 = fcs.dst2
            labels(3) = fcs.labels(2)
            For Each fp In algTask.fpList
                Dim val = dst2.Get(Of cv.Vec3b)(fp.pt.Y, fp.pt.X)
                dst3.FillConvexPoly(fp.facets, val)
            Next
        End Sub
    End Class






    Public Class FCS_InfoTest : Inherits TaskParent
        Dim fcs As New FCS_CreateList
        Dim info As New FCS_Info
        Public Sub New()
            desc = "Invoke FCS_CreateList and display the contents of the selected feature point cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(src)
            dst2 = fcs.dst2

            info.Run(src)
            SetTrueText(info.strOut, 3)

            fpDSet()
        End Sub
    End Class








    Public Class FCS_Motion : Inherits TaskParent
        Dim fcs As New FCS_CreateList
        Dim plot As New Plot_OverTime
        Public xDist As New List(Of Single), yDist As New List(Of Single)
        Public motionPercent As Single
        Public Sub New()
            plot.maxScale = 100
            plot.minScale = 0
            plot.plotCount = 1
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            labels(1) = "Plot of % of cells that moved - move camera to see value."
            desc = "Highlight the motion of each feature identified in the current and previous frame"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(algTask.grayStable)
            dst2 = fcs.dst2

            For Each fp In algTask.fpList
                If fp.depth > 0 Then DrawCircle(dst2, fp.pt, algTask.DotSize, algTask.highlight)
            Next

            Dim motionCount As Integer, linkedCount As Integer
            xDist.Clear()
            yDist.Clear()
            xDist.Add(0)
            yDist.Add(0)
            dst3.SetTo(0)
            For Each fp In algTask.fpList
                Dim brickIndex = algTask.gridMap.Get(Of Integer)(fp.pt.Y, fp.pt.X)
                Dim fpIndex = algTask.fpFromGridCellLast.IndexOf(brickIndex)
                If fpIndex >= 0 Then
                    linkedCount += 1
                    DrawLine(dst3, fp.pt, fp.ptLast)
                End If
                If fp.ptLast <> newPoint Then
                    motionCount += 1
                    xDist.Add(fp.ptLast.X - fp.pt.X)
                    yDist.Add(fp.ptLast.Y - fp.pt.Y)
                End If
            Next
            motionPercent = 100 * motionCount / linkedCount
            If algTask.heartBeat Then
                labels(2) = fcs.labels(2)
                labels(3) = Format(motionPercent, fmt1) + "% of linked cells had motion or " +
                        CStr(motionCount) + " of " + CStr(linkedCount) + ".  Distance moved X/Y " +
                        Format(xDist.Average, fmt1) + "/" + Format(yDist.Average, fmt1) +
                        " pixels."
            End If

            plot.plotData = New cv.Scalar(motionPercent, 0, 0)
            plot.Run(src)
            dst1 = plot.dst2
            fpDSet()
        End Sub
    End Class





    Public Class FCS_MotionDirection : Inherits TaskParent
        Dim fcsM As New FCS_Motion
        Dim plothist As New Plot_Histogram
        Dim mats As New Mat_4Click
        Dim range As Integer, rangeText As String
        Public Sub New()
            plothist.createHistogram = True
            plothist.addLabels = False
            algTask.gOptions.setHistogramBins(64) ' should this be an odd number.
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            desc = "Using all the feature points with motion, determine any with a common direction."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcsM.Run(src)
            mats.mat(2) = fcsM.dst2
            mats.mat(3) = fcsM.dst3

            plothist.maxRange = algTask.histogramBins / 2 Or 1
            plothist.minRange = -plothist.maxRange
            rangeText = " ranging from " + CStr(plothist.minRange) + " to " + CStr(plothist.maxRange)
            range = Math.Abs(plothist.maxRange - plothist.minRange)

            Dim incr = range / algTask.histogramBins

            plothist.Run(cv.Mat.FromPixelData(fcsM.xDist.Count, 1, cv.MatType.CV_32F, fcsM.xDist.ToArray))
            Dim xDist As New List(Of Single)(plothist.histArray)
            algTask.fpMotion.X = plothist.minRange + xDist.IndexOf(xDist.Max) * incr
            mats.mat(0) = plothist.dst2.Clone

            plothist.Run(cv.Mat.FromPixelData(fcsM.yDist.Count, 1, cv.MatType.CV_32F, fcsM.yDist.ToArray))
            Dim yDist As New List(Of Single)(plothist.histArray)
            algTask.fpMotion.Y = plothist.minRange + yDist.IndexOf(yDist.Max) * incr
            mats.mat(1) = plothist.dst2.Clone

            mats.Run(emptyMat)
            dst2 = mats.dst2
            dst3 = mats.dst3

            If fcsM.motionPercent < 50 Then
                algTask.fpMotion.X = 0
                algTask.fpMotion.Y = 0
            End If

            strOut = "CameraMotion estimate: " + vbCrLf + vbCrLf
            strOut += "Displacement in X: " + CStr(algTask.fpMotion.X) + vbCrLf
            strOut += "Displacement in Y: " + CStr(algTask.fpMotion.Y) + vbCrLf

            SetTrueText(strOut, 1)
            SetTrueText("X distances" + rangeText, 2)
            SetTrueText("Y distances " + rangeText, New cv.Point(dst2.Width / 2 + 2, 0), 2)
            labels = fcsM.labels
            fpDSet()
        End Sub
    End Class






    Public Class FCS_Info : Inherits TaskParent
        Public Sub New()
            If algTask.bricks Is Nothing Then algTask.bricks = New Brick_Basics
            desc = "Display the contents of the Feature Coordinate System (FCS) cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                Static fcs As New FCS_CreateList
                fcs.Run(algTask.grayStable)
                dst2 = fcs.dst2
            End If

            Dim fp = algTask.fpD
            strOut = "Feature point: " + fp.pt.ToString + vbCrLf + vbCrLf
            strOut += "index = " + CStr(fp.index) + vbCrLf
            strOut += "age (in frames) = " + CStr(fp.age) + vbCrLf
            strOut += "Facet count = " + CStr(fp.facets.Count) + " facets" + vbCrLf
            strOut += "ClickPoint = " + algTask.ClickPoint.ToString + vbCrLf + vbCrLf

            strOut += "brickIndex = " + CStr(fp.brickIndex) + vbCrLf
            Dim brick = algTask.bricks.brickList(fp.brickIndex)
            strOut += CStr(brick.age) + vbTab + "Age" + vbTab + vbCrLf
            strOut += Format(brick.correlation, fmt3) + vbTab + "Correlation to right image" + vbCrLf

            strOut += "Depth = " + Format(fp.depth, fmt1)
            strOut += vbCrLf
            strOut += "Index " + vbTab + "Facet X" + vbTab + "Facet Y" + vbCrLf
            For i = 0 To fp.facets.Count - 1
                strOut += CStr(i) + ":" + vbTab + CStr(fp.facets(i).X) + vbTab + CStr(fp.facets(i).Y) + vbCrLf
            Next

            If standalone Then SetTrueText(strOut, 3)
        End Sub
    End Class








    'Public Class FCS_RedCloud : Inherits TaskParent
    '    Dim redCombo As New RedList_Basics
    '    Dim fcs As New FCS_CreateList
    '    Dim knnMin As New KNN_MinDistance
    '    Public Sub New()
    '        desc = "Use the RedCloud maxDist points as feature points in an FCS display."
    '    End Sub
    '    Public Overrides Sub RunAlg(src As cv.Mat)
    '        redCombo.Run(src)
    '        dst3 = redCombo.dst2
    '        labels(2) = redCombo.labels(2)

    '        knnMin.inputPoints.Clear()
    '        For Each rc In algTask.redList.oldrclist
    '            knnMin.inputPoints.Add(rc.maxDist)
    '        Next
    '        knnMin.Run(src)

    '        algTask.features = New List(Of cv.Point2f)(knnMin.outputPoints2f)
    '        fcs.Run(src)
    '        dst2 = algTask.feat.fcs.dst2
    '        fpDSet()
    '        labels(3) = fcs.labels(2)
    '    End Sub
    'End Class








    Public Class FCS_Lines : Inherits TaskParent
        Dim fcs As New FCS_CreateList
        Dim options As New Options_Features
        Public Sub New()
            OptionParent.FindSlider("Min Distance").Value = 60
            algTask.featureOptions.FeatureMethod.SelectedItem() = "LineInput"
            labels(3) = "Cell boundaries with the age (in frames) for each cell."
            desc = "Use lines as input to FCS."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            fcs.Run(algTask.grayStable)
            dst2 = fcs.dst2

            fpDisplayAge()

            If algTask.heartBeat Then labels(2) = CStr(algTask.features.Count) + " lines were used to create " +
                                           CStr(algTask.fpList.Count) + " cells"
        End Sub
    End Class









    Public Class FCS_ByDepth : Inherits TaskParent
        Dim plotHist As New Plot_Histogram
        Dim fcs As New FCS_CreateList
        Dim palInput As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            plotHist.addLabels = False
            plotHist.removeZeroEntry = True
            plotHist.createHistogram = True
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            algTask.gOptions.setHistogramBins(20)
            desc = "Use cell depth to break down the layers in an image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                Static bPoint As New BrickPoint_Basics
                bPoint.Run(src)
                algTask.features.Clear()
                For Each pt In bPoint.ptList
                    algTask.features.Add(New cv.Point2f(pt.X, pt.Y))
                Next
            End If
            fcs.Run(src)
            dst2 = fcs.dst2
            labels(2) = fcs.labels(2)

            Dim bricks As New List(Of Single)
            For Each fp In algTask.fpList
                bricks.Add(fp.depth)
            Next

            plotHist.minRange = 0
            plotHist.maxRange = algTask.MaxZmeters
            plotHist.Run(cv.Mat.FromPixelData(bricks.Count, 1, cv.MatType.CV_32F, bricks.ToArray))
            dst1 = plotHist.dst2

            Dim incr = dst1.Width / algTask.histogramBins
            Dim histIndex = Math.Truncate(algTask.mouseMovePoint.X / incr)
            dst1.Rectangle(New cv.Rect(CInt(histIndex * incr), 0, incr, dst2.Height), cv.Scalar.Yellow, algTask.lineWidth)
            Dim depthIncr = (plotHist.maxRange - plotHist.minRange) / algTask.histogramBins
            Dim depthStart = histIndex * depthIncr
            Dim depthEnd = (histIndex + 1) * depthIncr

            Static fpCells As New List(Of (fpData, Integer))
            Static histIndexSave = histIndex

            If histIndexSave <> histIndex Or algTask.optionsChanged Then
                histIndexSave = histIndex
                fpCells.Clear()
            End If
            palInput.SetTo(0)

            For Each fp In algTask.fpList
                If fp.depth > depthStart And fp.depth < depthEnd Then
                    Dim val = palInput.Get(Of Byte)(fp.pt.Y, fp.pt.X)
                    If val = 0 Then
                        palInput.FillConvexPoly(fp.facets, fp.brickIndex Mod 255)
                        fpCells.Add((fp, algTask.frameCount))
                    End If
                End If
            Next

            For Each ele In fpCells
                Dim fp As fpData = ele.Item1
                SetTrueText(Format(fp.age, fmt0), fp.pt, 0)
                fpCellContour(fp, algTask.color, 0)
            Next
            dst3 = PaletteFull(palInput)
            dst3.SetTo(0, palInput.Threshold(0, 255, cv.ThresholdTypes.BinaryInv))


            Dim removeFrame As Integer = If(algTask.frameCount > algTask.frameHistoryCount, algTask.frameCount - algTask.frameHistoryCount, -1)
            For i = fpCells.Count - 1 To 0 Step -1
                Dim frame = fpCells(i).Item2
                If frame = removeFrame Then fpCells.RemoveAt(i)
            Next

            labels(3) = "Cells with depth between " + Format(depthStart, fmt1) + "m to " + Format(depthEnd, fmt1) + "m"
        End Sub
    End Class







    Public Class FCS_Periphery : Inherits TaskParent
        Public ptOutside As New List(Of cv.Point2f)
        Public ptInside As New List(Of cv.Point2f)
        Dim fcs As New FCS_CreateList
        Public Sub New()
            desc = "Display the cells which are on the periphery of the image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fcs.Run(algTask.grayStable)
            dst2 = fcs.dst2

            dst3 = dst2.Clone
            ptOutside.Clear()
            ptInside.Clear()
            For Each fp In algTask.fpList
                If fp.periph Then
                    dst3.FillConvexPoly(fp.facets, cv.Scalar.Gray, algTask.lineType)
                    DrawCircle(dst3, fp.pt, algTask.DotSize, algTask.highlight)
                    ptOutside.Add(fp.pt)
                Else
                    ptInside.Add(fp.pt)
                End If
            Next
            fpDSet()
            labels(2) = "There are " + CStr(ptOutside.Count) + " features on the periphery of the image."
            labels(3) = "There are " + CStr(algTask.fpList.Count - ptOutside.Count) + " features in the interior region of the image."
        End Sub
    End Class





    Public Class FCS_PeripheryNot : Inherits TaskParent
        Dim perif As New FCS_Periphery
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Create a mask for the cells which are not on the periphery of the image - the interior region that is fully visible and connected."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            perif.Run(algTask.grayStable)
            dst2 = perif.dst3

            dst3.SetTo(0)
            For Each fp In algTask.fpList
                If fp.periph = False Then dst3.FillConvexPoly(fp.facets, 255, algTask.lineType)
            Next
            fpDSet()
            labels = perif.labels
        End Sub
    End Class






    Public Class FCS_BrickPoints : Inherits TaskParent
        Public facetGen As New Delaunay_Generations
        Public ptList As New List(Of cv.Point2f)
        Public anchorPoint As cv.Point2f
        Dim good As New Feature_KNN
        Public Sub New()
            desc = "Maintain the generation counts around the feature points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            good.Run(src)
            facetGen.inputPoints = New List(Of cv.Point2f)(good.featurePoints)

            facetGen.Run(src)
            If facetGen.inputPoints.Count = 0 Then
                dst2.SetTo(0)
                dst3.SetTo(0)
                Exit Sub ' nothing to work on ...
            End If

            ptList.Clear()
            Dim generations As New List(Of Integer)
            For Each pt In facetGen.inputPoints
                Dim fIndex = facetGen.facet.dst3.Get(Of Integer)(pt.Y, pt.X)
                If fIndex >= facetGen.facet.facetList.Count Then Continue For ' new point
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
                dst2.FillConvexPoly(bestFacet, cv.Scalar.Black, algTask.lineType)
                DrawTour(dst2, bestFacet, algTask.highlight)
            End If

            dst2 = facetGen.dst2
            dst3 = src.Clone
            For i = 0 To ptList.Count - 1
                Dim pt = ptList(i)
                DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
                DrawCircle(dst3, pt, algTask.DotSize, algTask.highlight)
            Next
            labels(2) = CStr(ptList.Count) + " stable points were identified with a max of " + CStr(maxGens) +
                    " generations."
        End Sub
    End Class
End Namespace