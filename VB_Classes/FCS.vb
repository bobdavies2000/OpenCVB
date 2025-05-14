Imports cv = OpenCvSharp
Public Class FCS_Basics : Inherits TaskParent
    Dim fcs As New FCS_Core
    Public tour As New Tour_Basics
    Public desiredMapCount As Integer = 5
    Public Sub New()
        task.fcsMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Note that the task.fcsMap uses the same colors as the task.tourMap - same index for both."
        desc = "Create the reference map for FCS - updated on the heartbeat. "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static restartRequest As Boolean
        Dim count = task.motionMask.CountNonZero
        tour.Run(src)
        fcs.inputFeatures.Clear()
        For i = 1 To Math.Min(task.tourList.Count - 1, desiredMapCount)
            fcs.inputFeatures.Add(task.tourList(i).maxDist)
        Next
        If task.tourList.Count <= 1 Then ' when the camera is starting up the image may be too dark to process... Restart if so.
            restartRequest = True
            Exit Sub
        End If
        restartRequest = False

        fcs.Run(emptyMat)

        task.fcsMap = fcs.dst1.Clone
        dst2 = ShowPaletteFullColor(task.fcsMap)
        dst3 = tour.dst2
        labels(2) = fcs.labels(2)
    End Sub
End Class






Public Class FCS_Core : Inherits TaskParent
    Dim subdiv As New cv.Subdiv2D
    Public inputFeatures As New List(Of cv.Point2f)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        subdiv.InitDelaunay(New cv.Rect(0, 0, dst1.Width, dst1.Height))
        subdiv.Insert(inputFeatures)

        Dim facets = New cv.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        For i = 0 To Math.Min(inputFeatures.Count, facets.Count) - 1
            Dim facetList = New List(Of cv.Point)
            For Each pt In facets(i)
                facetList.Add(New cv.Point(pt.X, pt.Y))
            Next
            dst1.FillConvexPoly(facetList, i, cv.LineTypes.Link8)
        Next

        If standaloneTest() Then dst2 = ShowPalette(dst1)

        If task.heartBeat Then labels(2) = traceName + ": " + CStr(inputFeatures.Count - 1) + " cells found." ' don't count tourList(0)
    End Sub
End Class






Public Class FCS_CreateList : Inherits TaskParent
    Dim subdiv As New cv.Subdiv2D
    Dim feat As New Feature_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        task.fpMap = New cv.Mat(dst2.Size(), cv.MatType.CV_32F, 0)
        labels(3) = "CV_8U map of Delaunay cells."
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(task.grayStable)

        subdiv.InitDelaunay(New cv.Rect(0, 0, dst1.Width, dst1.Height))
        subdiv.Insert(task.features)

        Dim facets = New cv.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        task.fpList.Clear()
        Dim matchCount As Integer
        dst1.SetTo(0)
        For i = 0 To Math.Min(task.features.Count, facets.Count) - 1
            Dim fp As New fpData
            fp.pt = task.features(i)
            fp.ptHistory.Add(fp.pt)
            fp.index = i

            Dim brickIndex = task.brickMap.Get(Of Single)(fp.pt.Y, fp.pt.X)
            Dim brick = task.brickList(brickIndex)
            Dim fpIndex = task.fpFromGridCellLast.IndexOf(brickIndex)
            If fpIndex >= 0 Then
                Dim fpLast = task.fpLastList(fpIndex)
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

            task.fpList.Add(fp)

            task.fpMap.FillConvexPoly(fp.facets, CSng(brickIndex), task.lineType)
            dst1.FillConvexPoly(fp.facets, i, task.lineType)
        Next

        If task.features.Count <> facets.Length Then
            task.fpFromGridCell.Clear()
            For Each fp In task.fpList
                Dim nextIndex = task.brickMap.Get(Of Single)(fp.pt.Y, fp.pt.X)
                task.fpFromGridCell.Add(nextIndex)
            Next
        End If

        dst2 = ShowPalette(dst1)
        For Each fp In task.fpList
            If fp.depth > 0 Then DrawCircle(dst2, fp.pt, task.DotSize, task.highlight)
        Next

        If standalone Then
            fpDisplayAge()
            fpCellContour(task.fpD, task.color)
        End If
        fpDSet()

        If task.heartBeat Then
            labels(2) = traceName + ": " + Format(task.features.Count, "000") + " cells found.  Matched = " +
                        CStr(matchCount) + " of " + CStr(task.features.Count)
        End If
    End Sub
End Class








Public Class FCS_ViewLeft : Inherits TaskParent
    Dim fcs As New FCS_CreateList
    Public Sub New()
        desc = "Build an FCS for left view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fcs.Run(task.leftView)
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
        fcs.Run(task.rightView)
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
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use color to connect FCS cells - visualize the data mostly."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        flood.Run(src)
        dst2 = flood.dst2

        dst1 = src.Clone

        edges.Run(src)
        dst3 = edges.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        For i = 0 To task.fpList.Count - 1
            Dim fp = task.fpList(i)
            DrawCircle(dst1, fp.pt, task.DotSize, task.highlight)
            DrawCircle(dst2, fp.pt, task.DotSize, task.highlight)

            task.fpList(i) = fp
            DrawCircle(dst3, fp.pt, task.DotSize, task.highlight)
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
        For Each fp In task.fpList
            If fp.depth Then
                DrawCircle(dst2, fp.pt, task.DotSize, task.highlight)
                DrawCircle(dst3, fp.pt, task.DotSize, task.highlight)
            End If
        Next
        fpDSet()
    End Sub
End Class






'Public Class FCS_KNNfeatures : Inherits TaskParent
'    Dim fcs As New FCS_CreateList
'    Dim knn As New KNNorm_Basics
'    Dim info As New FCS_Info
'    Dim dimension As Integer
'    Public Sub New()
'        task.gOptions.debugSyncUI.Checked = True
'        If standalone Then task.gOptions.displayDst1.Checked = True
'        optiBase.FindSlider("KNN Dimension").Value = 3
'        desc = "Can we distinguish each feature point cell with color, depth, and grid."
'    End Sub
'    Private Function buildEntry(fp As fpData) As List(Of Single)
'        Dim dataList As New List(Of Single)
'        For i = 0 To dimension - 1
'            dataList.Add(Choose(i + 1, fp.depth, fp.depthMin, fp.depthMax))
'        Next
'        Return dataList
'    End Function
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        Static dimensionSlider = optiBase.FindSlider("KNN Dimension")
'        dimension = dimensionSlider.value

'        fcs.Run(src)
'        dst2 = fcs.dst2

'        Static fpSave As fpData
'        If task.firstPass Or task.mouseClickFlag Then
'            fpSave = task.fpList(task.fpMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X))
'        End If

'        info.Run(src)
'        SetTrueText(info.strOut, 1)

'        Dim query = buildEntry(fpSave)
'        knn.queryInput.Clear()
'        For Each e In query
'            knn.queryInput.Add(e)
'        Next

'        knn.trainInput.Clear()

'        For Each fp In task.fpList
'            Dim entry = buildEntry(fp)
'            For Each e In entry
'                knn.trainInput.Add(e)
'            Next
'        Next

'        knn.Run(src)

'        fpDSet()
'        fpCellContour(task.fpD, dst2)
'        For i = 0 To 10
'            Dim fp = task.fpList(knn.result(0, i))
'            fpCellContour(fp, task.color, 1)
'            SetTrueText(CStr(i), fp.pt, 3)
'        Next

'        task.fpD = task.fpList(knn.result(0, 0))
'        info.Run(src)
'        SetTrueText(info.strOut, 3)
'        task.ClickPoint = task.fpD.pt
'    End Sub
'End Class






Public Class FCS_WithAge : Inherits TaskParent
    Dim fcs As New FCS_CreateList
    Public Sub New()
        labels(3) = "Ages are kept below 1000 to make the output more readable..."
        desc = "Display the age of each cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fcs.Run(task.grayStable)
        dst2 = fcs.dst2
        labels(2) = fcs.labels(2)

        dst3.SetTo(0)
        For Each fp In task.fpList
            DrawCircle(dst3, fp.pt, task.DotSize, task.highlight)
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
        fcs.Run(task.grayStable)
        dst2 = fcs.dst2
        labels(2) = fcs.labels(2)

        Dim fpSorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For Each fp In task.fpList
            fpSorted.Add(fp.age, fp.index)
        Next

        dst3.SetTo(0)
        Dim maxIndex As Integer = 0
        For Each index In fpSorted.Values
            Dim fp = task.fpList(index)
            DrawCircle(dst3, fp.pt, task.DotSize, task.highlight)
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
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "Output of FCS_CreateList."
        desc = "Isolate FCS cells for each redCell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then runRedC(src)
        dst2 = task.redC.dst2
        labels(2) = task.redC.labels(2)

        fcs.Run(src)
        dst1 = fcs.dst2
        labels(3) = fcs.labels(2)
        For Each fp In task.fpList
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
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "Plot of % of cells that moved - move camera to see value."
        desc = "Highlight the motion of each feature identified in the current and previous frame"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fcs.Run(task.grayStable)
        dst2 = fcs.dst2

        For Each fp In task.fpList
            If fp.depth > 0 Then DrawCircle(dst2, fp.pt, task.DotSize, task.highlight)
        Next

        Dim motionCount As Integer, linkedCount As Integer
        xDist.Clear()
        yDist.Clear()
        xDist.Add(0)
        yDist.Add(0)
        dst3.SetTo(0)
        For Each fp In task.fpList
            Dim brickIndex = task.brickMap.Get(Of Single)(fp.pt.Y, fp.pt.X)
            Dim fpIndex = task.fpFromGridCellLast.IndexOf(brickIndex)
            If fpIndex >= 0 Then
                linkedCount += 1
                dst3.Line(fp.pt, fp.ptLast, task.highlight, task.lineWidth, task.lineType)
            End If
            If fp.ptLast <> newPoint Then
                motionCount += 1
                xDist.Add(fp.ptLast.X - fp.pt.X)
                yDist.Add(fp.ptLast.Y - fp.pt.Y)
            End If
        Next
        motionPercent = 100 * motionCount / linkedCount
        If task.heartBeat Then
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
        task.gOptions.setHistogramBins(64) ' should this be an odd number.
        If standalone Then task.gOptions.displayDst1.Checked = True
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

        plothist.Run(cv.Mat.FromPixelData(fcsM.xDist.Count, 1, cv.MatType.CV_32F, fcsM.xDist.ToArray))
        Dim xDist As New List(Of Single)(plothist.histArray)
        task.fpMotion.X = plothist.minRange + xDist.IndexOf(xDist.Max) * incr
        mats.mat(0) = plothist.dst2.Clone

        plothist.Run(cv.Mat.FromPixelData(fcsM.yDist.Count, 1, cv.MatType.CV_32F, fcsM.yDist.ToArray))
        Dim yDist As New List(Of Single)(plothist.histArray)
        task.fpMotion.Y = plothist.minRange + yDist.IndexOf(yDist.Max) * incr
        mats.mat(1) = plothist.dst2.Clone

        mats.Run(src)
        dst2 = mats.dst2
        dst3 = mats.dst3

        If fcsM.motionPercent < 50 Then
            task.fpMotion.X = 0
            task.fpMotion.Y = 0
        End If

        strOut = "CameraMotion estimate: " + vbCrLf + vbCrLf
        strOut += "Displacement in X: " + CStr(task.fpMotion.X) + vbCrLf
        strOut += "Displacement in Y: " + CStr(task.fpMotion.Y) + vbCrLf

        SetTrueText(strOut, 1)
        SetTrueText("X distances" + rangeText, 2)
        SetTrueText("Y distances " + rangeText, New cv.Point(dst2.Width / 2 + 2, 0), 2)
        labels = fcsM.labels
        fpDSet()
    End Sub
End Class






Public Class FCS_Info : Inherits TaskParent
    Public Sub New()
        desc = "Display the contents of the Feature Coordinate System (FCS) cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Static fcs As New FCS_CreateList
            fcs.Run(task.grayStable)
            dst2 = fcs.dst2
        End If

        Dim fp = task.fpD
        strOut = "Feature point: " + fp.pt.ToString + vbCrLf + vbCrLf
        strOut += "index = " + CStr(fp.index) + vbCrLf
        strOut += "age (in frames) = " + CStr(fp.age) + vbCrLf
        strOut += "Facet count = " + CStr(fp.facets.Count) + " facets" + vbCrLf
        strOut += "ClickPoint = " + task.ClickPoint.ToString + vbCrLf + vbCrLf

        strOut += "brickIndex = " + CStr(fp.brickIndex) + vbCrLf
        Dim brick = task.brickList(fp.brickIndex)
        strOut += CStr(brick.age) + vbTab + "Age" + vbTab + vbCrLf
        strOut += Format(brick.correlation, fmt3) + vbTab + "Correlation to right image" + vbCrLf
        strOut += Format(brick.disparity, fmt1) + vbTab + "Disparity to right image" + vbCrLf

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
'    Dim redCombo As New RedColor_Basics
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
'        For Each rc In task.rcList
'            knnMin.inputPoints.Add(rc.maxDist)
'        Next
'        knnMin.Run(src)

'        task.features = New List(Of cv.Point2f)(knnMin.outputPoints2f)
'        fcs.Run(src)
'        dst2 = task.feat.fcs.dst2
'        fpDSet()
'        labels(3) = fcs.labels(2)
'    End Sub
'End Class








Public Class FCS_Lines : Inherits TaskParent
    Dim fcs As New FCS_CreateList
    Public Sub New()
        task.featureOptions.DistanceSlider.Value = 60
        task.featureOptions.FeatureMethod.SelectedItem() = "LineInput"
        labels(3) = "Cell boundaries with the age (in frames) for each cell."
        desc = "Use lines as input to FCS."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fcs.Run(task.grayStable)
        dst2 = fcs.dst2

        fpDisplayAge()

        If task.heartBeat Then labels(2) = CStr(task.features.Count) + " lines were used to create " +
                                           CStr(task.fpList.Count) + " cells"
    End Sub
End Class









Public Class FCS_ByDepth : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Dim fcs As New FCS_CreateList
    Dim palInput As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        plot.addLabels = False
        plot.removeZeroEntry = True
        plot.createHistogram = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        task.gOptions.setHistogramBins(20)
        desc = "Use cell depth to break down the layers in an image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Static ptBest As New BrickPoint_Basics
            ptBest.Run(src)
            task.features = ptBest.intensityFeatures
        End If
        fcs.Run(src)
        dst2 = fcs.dst2
        labels(2) = fcs.labels(2)

        Dim bricks As New List(Of Single)
        For Each fp In task.fpList
            bricks.Add(fp.depth)
        Next

        plot.minRange = 0
        plot.maxRange = task.MaxZmeters
        plot.Run(cv.Mat.FromPixelData(bricks.Count, 1, cv.MatType.CV_32F, bricks.ToArray))
        dst1 = plot.dst2

        Dim incr = dst1.Width / task.histogramBins
        Dim histIndex = Math.Truncate(task.mouseMovePoint.X / incr)
        dst1.Rectangle(New cv.Rect(CInt(histIndex * incr), 0, incr, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
        Dim depthIncr = (plot.maxRange - plot.minRange) / task.histogramBins
        Dim depthStart = histIndex * depthIncr
        Dim depthEnd = (histIndex + 1) * depthIncr

        Static fpCells As New List(Of (fpData, Integer))
        Static histIndexSave = histIndex

        If histIndexSave <> histIndex Or task.optionsChanged Then
            histIndexSave = histIndex
            fpCells.Clear()
        End If
        palInput.SetTo(0)

        For Each fp In task.fpList
            If fp.depth > depthStart And fp.depth < depthEnd Then
                Dim val = palInput.Get(Of Byte)(fp.pt.Y, fp.pt.X)
                If val = 0 Then
                    palInput.FillConvexPoly(fp.facets, fp.brickIndex Mod 255)
                    fpCells.Add((fp, task.frameCount))
                End If
            End If
        Next

        For Each ele In fpCells
            Dim fp As fpData = ele.Item1
            SetTrueText(Format(fp.age, fmt0), fp.pt, 0)
            fpCellContour(fp, task.color, 0)
        Next
        dst3 = ShowPalette(palInput)
        dst3.SetTo(0, palInput.Threshold(0, 255, cv.ThresholdTypes.BinaryInv))


        Dim removeFrame As Integer = If(task.frameCount > task.frameHistoryCount, task.frameCount - task.frameHistoryCount, -1)
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
        fcs.Run(task.grayStable)
        dst2 = fcs.dst2

        dst3 = dst2.Clone
        ptOutside.Clear()
        ptInside.Clear()
        For Each fp In task.fpList
            If fp.periph Then
                dst3.FillConvexPoly(fp.facets, cv.Scalar.Gray, task.lineType)
                DrawCircle(dst3, fp.pt, task.DotSize, task.highlight)
                ptOutside.Add(fp.pt)
            Else
                ptInside.Add(fp.pt)
            End If
        Next
        fpDSet()
        labels(2) = "There are " + CStr(ptOutside.Count) + " features on the periphery of the image."
        labels(3) = "There are " + CStr(task.fpList.Count - ptOutside.Count) + " features in the interior region of the image."
    End Sub
End Class





Public Class FCS_PeripheryNot : Inherits TaskParent
    Dim perif As New FCS_Periphery
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Create a mask for the cells which are not on the periphery of the image - the interior region that is fully visible and connected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        perif.Run(task.grayStable)
        dst2 = perif.dst3

        dst3.SetTo(0)
        For Each fp In task.fpList
            If fp.periph = False Then dst3.FillConvexPoly(fp.facets, 255, task.lineType)
        Next
        fpDSet()
        labels = perif.labels
    End Sub
End Class
