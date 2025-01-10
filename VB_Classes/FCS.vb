Imports System.Web.UI
Imports OpenCvSharp
Imports cv = OpenCvSharp
Public Class FCS_Basics : Inherits TaskParent
    Dim delaunay As New FCS_Delaunay
    Dim match As New Match_Basics
    Dim options As New Options_FCSMatch
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        labels(1) = "The feature point of each cell."
        desc = "Build a Feature Coordinate System by subdividing an image based on the points provided."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        options.RunOpt()

        task.feat.Run(src)

        task.fpListLast = New List(Of fpData)(task.fpList)
        task.fpMapLast = task.fpMap.Clone
        Static fpLastSrc = src.Clone

        delaunay.Run(src)

        Dim matchCount As Integer
        If task.firstPass = False Then
            For i = 0 To task.fpList.Count - 1
                Dim fp = task.fpList(i)
                Dim indexLast = task.fpMapLast.Get(Of Integer)(fp.ptCenter.Y, fp.ptCenter.X)
                Dim fpLast = task.fpListLast(indexLast)
                ' is this the same point?
                match.template = fpLastSrc(fpLast.rect)
                match.Run(src(fpLast.rect))
                fp.correlation = match.correlation
                If match.correlation > options.MinCorrelation Then
                    task.fpList(i) = fpUpdate(fp, fpLast)
                    matchCount += 1
                End If
            Next
        End If

        dst3 = task.fpOutline
        dst2 = ShowPalette(task.fpMap * 255 / task.fpList.Count)
        dst2.SetTo(0, task.fpOutline)

        If standalone Then
            fpDisplayAge()
            fpDisplayMotion()
            fpDisplayCell()
        End If

        Dim matchPercent = matchCount / task.features.Count
        If task.heartBeat Then
            labels(2) = Format(matchPercent, "0%") + " were found and matched to the previous frame or " +
                        CStr(matchCount) + " of " + CStr(task.features.Count)
        End If
        labels(3) = Format(matchPercent, "0%") + " matched to previous frame (instantaneous update)"
        fpLastSrc = src.Clone
        ' DrawCircle(task.color, task.ClickPoint, task.DotSize, task.HighlightColor)
    End Sub
End Class








Public Class FCS_ViewLeft : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Public Sub New()
        desc = "Build an FCS for left view."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        fcs.Run(task.leftView)
        dst2 = fcs.dst2
        dst3 = fcs.dst3

        fpDisplayAge()
        fpDisplayCell()
        labels(2) = fcs.labels(2)
    End Sub
End Class







Public Class FCS_ViewRight : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Public Sub New()
        desc = "Build an FCS for right view."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        fcs.Run(task.rightView)
        dst2 = fcs.dst2
        dst3 = fcs.dst3

        fpDisplayAge()
        fpDisplayCell()
        labels(2) = fcs.labels(2)
    End Sub
End Class








Public Class FCS_Motion : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Dim plot As New Plot_OverTime
    Public xDist As New List(Of Single), yDist As New List(Of Single)
    Public motionPercent As Single
    Public Sub New()
        plot.maxScale = 100
        plot.minScale = 0
        plot.plotCount = 1
        If standalone Then task.gOptions.setDisplay1()
        labels(1) = "Plot of % of cells that moved - move camera to see value."
        desc = "Highlight the motion of each feature identified in the current and previous frame"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        fcs.Run(src)
        dst2 = fcs.dst1

        task.feat.Run(src)
        For Each fp In task.fpList
            DrawCircle(dst2, fp.pt, task.DotSize, task.HighlightColor)
        Next

        dst3.SetTo(0)
        Dim motionCount As Integer, linkedCount As Integer
        xDist = New List(Of Single)
        yDist = New List(Of Single)
        xDist.Add(0)
        yDist.Add(0)
        For Each fp In task.fpList
            If fp.indexLast >= 0 Then linkedCount += 1
            Dim p1 = fp.pt
            Dim p2 = If(fp.indexLast < 0, fp.pt, task.fpListLast(fp.indexLast).pt)
            dst3.Line(p1, p2, task.HighlightColor, task.lineWidth, task.lineType)
            If p1 <> p2 Then
                motionCount += 1
                xDist.Add(p2.X - p1.X)
                yDist.Add(p2.Y - p1.Y)
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
        fpDisplayCell()
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
        If standalone Then task.gOptions.setDisplay1()
        desc = "Using all the feature points with motion, determine any with a common direction."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
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
        fpDisplayCell()
    End Sub
End Class






Public Class FCS_FloodFill : Inherits TaskParent
    Dim flood As New Flood_Basics
    Dim fcs As New FCS_Basics
    Dim edges As New Edge_Canny
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use color to connect FCS cells - visualize the data mostly."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        flood.Run(src)
        dst2 = flood.dst2

        fcs.Run(src)
        dst1 = src

        edges.Run(src)
        dst3 = edges.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        For i = 0 To task.fpList.Count - 1
            Dim fp = task.fpList(i)
            DrawCircle(dst1, fp.pt, task.DotSize, task.HighlightColor)
            DrawCircle(dst2, fp.pt, task.DotSize, task.HighlightColor)
            fp.rcIndex = task.redMap.Get(Of Byte)(fp.pt.Y, fp.pt.X)

            task.fpList(i) = fp

            'Dim rc = task.redCells(fp.rcIndex)
            'dst3(fp.rect).SetTo(rc.naturalColor, fp.mask)
            DrawCircle(dst3, fp.pt, task.DotSize, task.HighlightColor)
        Next
        dst3.SetTo(cv.Scalar.White, task.fpOutline)
    End Sub
End Class








Public Class FCS_Periphery : Inherits TaskParent
    Dim fcs As New FCS_Basics

    Public ptOutside As New List(Of cv.Point2f)
    Public ptOutID As New List(Of Single)

    Public ptInside As New List(Of cv.Point2f)
    Public ptInID As New List(Of Single)
    Public Sub New()
        desc = "Display the cells which are on the periphery of the image"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        fcs.Run(src)
        dst2 = fcs.dst2

        dst3 = dst2.Clone
        ptOutside.Clear()
        ptOutID.Clear()
        ptInside.Clear()
        ptInID.Clear()

        For Each fp In task.fpList
            If fp.periph Then
                dst3(fp.rect).SetTo(cv.Scalar.Gray, fp.mask)
                DrawCircle(dst3, fp.pt, task.DotSize, task.HighlightColor)
                ptOutside.Add(fp.pt)
                ptOutID.Add(fp.ID)
            Else
                ptInside.Add(fp.pt)
                ptInID.Add(fp.ID)
            End If
        Next
        fpDisplayCell()
        dst3.Rectangle(task.fpSelected.rect, task.HighlightColor, task.lineWidth)
    End Sub
End Class








Public Class FCS_Edges : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Dim edges As New Edge_Canny
    Public Sub New()
        desc = "Use edges to connect feature points to their neighbors."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        fcs.Run(src)
        dst2 = src

        edges.Run(src)
        dst3 = edges.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each fp In task.fpList
            DrawCircle(dst2, fp.ptCenter, task.DotSize, task.HighlightColor)
            DrawCircle(dst3, fp.ptCenter, task.DotSize, task.HighlightColor)
        Next
        dst3.SetTo(cv.Scalar.White, task.fpOutline)
        fpDisplayCell()
    End Sub
End Class






'Public Class FCS_MatchDepthColor : Inherits TaskParent
'    Dim fcs As New FCS_Basics
'    Dim match As New Match_Basics
'    Dim options As New Options_FCSMatch
'    Public Sub New()
'        desc = "Track each feature with FCS"
'    End Sub
'    Public Overrides sub runAlg(src As cv.Mat)
'        options.RunOpt()

'        fcs.Run(src)
'        dst2 = fcs.dst2
'        Dim fp1 = task.fpSelected
'        dst2(fp1.rect).SetTo(cv.Scalar.White, fp1.mask)

'        Static fpLastList = New List(Of fpData)(task.fpList)
'        Static fpLastIDs = New List(Of Single)(task.fpIDlist)
'        Static fpLastMap = task.fpMap.Clone
'        Static fpLastSrc = src.Clone
'        Dim correlationCount As Integer, depthColorCount As Integer
'        Dim noMatchCount As Integer, matchMap As Integer
'        Dim depthIndex As Integer, colorIndex As Integer
'        For i = 0 To task.fpList.Count - 1
'            Dim fp = task.fpList(i)
'            If fp.indexLast >= 0 Then
'                Dim fplast = task.fpListLast(fp.indexLast)
'                match.template = fpLastSrc(fplast.rect)
'                match.Run(src(fplast.nabeRect))
'                If match.correlation > options.MinCorrelation Then
'                    fp = fpUpdate(fp, fplast)
'                    correlationCount += 1
'                Else
'                    Dim distances As New List(Of Single)
'                    For j = 0 To fp.nabeList.Count - 1
'                        distances.Add(Math.Abs(task.fpList(j).depthMean - fplast.depthMean))
'                    Next
'                    depthIndex = fp.nabeList(distances.IndexOf(distances.Min))
'                    Dim colorDistance As New List(Of Single)
'                    For j = 0 To fp.nabeList.Count - 1
'                        colorDistance.Add(distance3D(task.fpList(j).colorMean, fplast.colorMean))
'                    Next
'                    colorIndex = colorDistance.IndexOf(colorDistance.Min)
'                    If colorIndex = depthIndex Then
'                        fp = fpUpdate(fp, fpLastList(colorIndex))
'                        depthColorCount += 1
'                    Else
'                        fp.indexLast = -1
'                        fp.age = 1
'                        noMatchCount += 1
'                    End If
'                End If
'            End If
'            task.fpList(i) = fp
'        Next

'        fpLastList = New List(Of fpData)(task.fpList)
'        fpLastIDs = New List(Of Single)(task.fpIDlist)
'        fpLastMap = task.fpMap.Clone
'        fpLastSrc = src.Clone
'        labels(2) = fcs.labels(2) + " Matched with Map/Correlation/Neighbor/Unmatched: " +
'                    CStr(matchMap) + "/" + CStr(correlationCount) + "/" +
'                    CStr(depthColorCount) + "/" + CStr(noMatchCount)
'    End Sub
'End Class







'Public Class FCS_MatchNeighbors : Inherits TaskParent
'    Dim fcs As New FCS_MatchDepthColor
'    Public Sub New()
'        If standalone Then task.gOptions.setDisplay0()
'        desc = "Track all the feature points and show their ID"
'    End Sub
'    Public Overrides sub runAlg(src As cv.Mat)
'        dst0 = src.Clone
'        fcs.Run(src)
'        dst2 = fcs.dst2

'        Dim fp = task.fpSelected
'        dst3.SetTo(0)
'        For Each index In fp.nabeList
'            Dim fpNabe = task.fpList(index)
'            DrawCircle(dst3, fpNabe.ptCenter, task.DotSize, task.HighlightColor)
'            SetTrueText(CStr(fpNabe.age), fpNabe.ptCenter, 3)
'        Next
'        Static finfo As New FCS_Info
'        finfo.Run(src)
'        SetTrueText(finfo.strOut, 3)
'        labels(2) = fcs.labels(2)
'        labels(3) = CStr(task.fpList.Count) + " cells found.  Dots below are at fp.ptCenter (not feature point)"
'        drawFeaturePoints(dst0, fp.facets, cv.Scalar.White)
'    End Sub
'End Class




'Public Class FCS_MatchEdges : Inherits TaskParent
'    Dim fcs As New FCS_Basics
'    Dim edges As New Edge_Canny
'    Dim match As New Match_Basics
'    Dim options As New Options_FCSMatch
'    Public Sub New()
'        If standalone Then task.gOptions.setDisplay1()
'        labels(3) = "The age of each feature point cell."
'        desc = "Try to improve the match count to the previous frame using correlation"
'    End Sub
'    Public Overrides sub runAlg(src As cv.Mat)
'        options.RunOpt()

'        edges.Run(src)
'        dst1 = edges.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

'        fcs.Run(src)
'        dst2 = fcs.dst2
'        labels(2) = fcs.labels(2)

'        Static fpLastEdges = dst1.Clone
'        Static fpLastSrc = src.Clone

'        Dim matchEdges As Integer, matchSrc As Integer
'        For i = 0 To task.fpList.Count - 1
'            Dim fp As fpData = task.fpList(i)
'            If fp.indexLast < 0 Then
'                Dim indexLast = task.fpMapLast.Get(Of Integer)(fp.ptCenter.Y, fp.ptCenter.X)
'                Dim fpLast = task.fpListLast(indexLast)
'                match.template = fpLastEdges(fpLast.rect)
'                match.Run(dst1(fpLast.nabeRect))
'                If match.correlation > options.MinCorrelation Then
'                    fp = fpUpdate(fp, fpLast)
'                    matchEdges += 1
'                Else
'                    match.template = fpLastSrc(fpLast.rect)
'                    match.Run(src(fpLast.nabeRect))
'                    If match.correlation > options.MinCorrelation Then
'                        fp = fpUpdate(fp, fpLast)
'                        matchSrc += 1
'                    Else
'                        fp.indexLast = -1
'                        fp.age = 1
'                    End If
'                End If
'            End If
'            task.fpList(i) = fp
'        Next

'        fpLastEdges = dst1.Clone
'        fpLastEdges = src.Clone
'        labels(2) = CStr(matchEdges) + " cells were edge matched.  " + CStr(matchSrc) + " cells match with src"

'        dst3.SetTo(0)
'        For Each fp In task.fpList
'            DrawCircle(dst1, fp.ptCenter, task.DotSize, task.HighlightColor)
'            DrawCircle(dst3, fp.ptCenter, task.DotSize, task.HighlightColor)
'            SetTrueText(CStr(fp.age), fp.ptCenter, 3)
'        Next
'    End Sub
'End Class







Public Class FCS_Neighbors : Inherits TaskParent
    Dim fInfo As New FCS_Info
    Dim fcs As New FCS_Basics
    Public Sub New()
        labels(3) = "The neighbor cells with the corner feature rectangles."
        desc = "Show the midpoints in each cell and build the nabelist for each cell"
    End Sub
    Public Sub buildNeighbors()
        For i = 0 To task.fpList.Count - 1
            Dim fp = task.fpList(i)
            Dim facets As New List(Of cv.Point)(fp.facets)
            If fp.periph Then
                facets.Add(fp.rect.TopLeft)
                facets.Add(fp.rect.Location)
                facets.Add(fp.rect.BottomRight)
                facets.Add(New cv.Point(fp.rect.Location.X + fp.rect.Width, fp.rect.Location.Y))
            End If
            fp.nabeRect = fp.rect
            For Each pt In facets
                If pt.X < 0 Or pt.X > dst2.Width Then Continue For
                If pt.Y < 0 Or pt.Y > dst2.Height Then Continue For
                Dim index As Integer
                For j = 0 To 8
                    Dim ptNabe = Choose(j + 1, New cv.Point(pt.X - 1, pt.Y - 1),
                                               New cv.Point(pt.X, pt.Y - 1),
                                               New cv.Point(pt.X + 1, pt.Y - 1),
                                               New cv.Point(pt.X - 1, pt.Y),
                                               New cv.Point(pt.X, pt.Y),
                                               New cv.Point(pt.X + 1, pt.Y),
                                               New cv.Point(pt.X - 1, pt.Y + 1),
                                               New cv.Point(pt.X, pt.Y + 1),
                                               New cv.Point(pt.X + 1, pt.Y + 1))
                    If ptNabe.x >= 0 And ptNabe.x < dst2.Width And
                       ptNabe.y >= 0 And ptNabe.y < dst2.Height Then
                        index = task.fpMap.Get(Of Integer)(ptNabe.y, ptNabe.x)
                    End If
                    If fp.nabeList.Contains(index) = False Then
                        fp.nabeList.Add(index)
                        fp.nabeRect = fp.nabeRect.Union(task.fpList(index).rect)
                    End If
                Next
            Next
            task.fpList(i) = fp
        Next
    End Sub
    Private Function verifyRect(r As cv.Rect, sz As Integer, szNew As Integer) As cv.Rect
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.BottomRight.X >= dst2.Width Then r.X = dst2.Width - szNew
        If r.BottomRight.Y >= dst2.Height Then r.Y = dst2.Height - szNew
        Return r
    End Function
    Public Overrides Sub runAlg(src As cv.Mat)
        fcs.Run(src)
        dst2 = fcs.dst2

        buildNeighbors()

        dst3.SetTo(0)
        Dim fp = task.fpSelected
        If fp IsNot Nothing Then
            For Each index In fp.nabeList
                Dim nabe = task.fpList(index)
                Dim vec = dst2.Get(Of cv.Vec3b)(nabe.ptCenter.Y, nabe.ptCenter.X)
                Dim color = New cv.Scalar(vec.Item0, vec.Item1, vec.Item2)
                dst3(nabe.rect).SetTo(color, nabe.mask)
            Next
            fpCellContour(task.fpSelected, dst3)
            fpDisplayCell()
        End If
    End Sub
End Class






Public Class FCS_NoTracking : Inherits TaskParent
    Public facetList As New List(Of List(Of cv.Point))
    Public facet32s As cv.Mat
    Dim subdiv As New cv.Subdiv2D
    Public Sub New()
        facet32s = New cv.Mat(dst2.Size(), cv.MatType.CV_32SC1, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "CV_8U map of Delaunay cells"
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        subdiv.InitDelaunay(New cv.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(task.features)

        Dim facets = New cv.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        facetList.Clear()
        For i = 0 To facets.Length - 1
            Dim nextFacet As New List(Of cv.Point)
            For j = 0 To facets(i).Length - 1
                nextFacet.Add(New cv.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            facet32s.FillConvexPoly(nextFacet, i, task.lineType)
            facetList.Add(nextFacet)
        Next

        dst1.SetTo(0)
        For i = 0 To facets.Length - 1
            Dim ptList As New List(Of cv.Point)
            For j = 0 To facets(i).Length - 1
                ptList.Add(New cv.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            DrawContour(dst1, ptList, 255, 1)
        Next

        facet32s.ConvertTo(dst3, cv.MatType.CV_8U)
        dst2 = ShowPalette(dst3 * 255 / (facets.Length + 1))

        dst3.SetTo(0, dst1)
        dst2.SetTo(white, dst1)
        labels(2) = traceName + ": " + Format(task.features.Count, "000") + " cells were present."
    End Sub
End Class






Public Class FCS_Delaunay : Inherits TaskParent
    Dim subdiv As New cv.Subdiv2D
    Public Sub New()
        task.fpMap = New cv.Mat(dst2.Size(), cv.MatType.CV_32SC1, 0)
        labels(3) = "CV_8U map of Delaunay cells"
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        task.feat.Run(src)

        subdiv.InitDelaunay(New cv.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(task.features)

        Dim facets = New cv.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        task.fpList.Clear()
        task.fpIDlist.Clear()
        task.fpOutline.SetTo(0)
        Dim depthMean As cv.Scalar, stdev As cv.Scalar
        For i = 0 To facets.Length - 1
            Dim fp As New fpData
            fp.pt = task.features(i)
            fp.ptHistory.Add(fp.pt)
            fp.index = i

            fp.ID = CSng(task.gridMap32S.Get(Of Integer)(fp.pt.Y, fp.pt.X))

            While 1
                If task.fpIDlist.Contains(fp.ID) Then fp.ID += 0.1 Else Exit While
            End While

            task.fpIDlist.Add(fp.ID)

            fp.facets = New List(Of cv.Point)
            For j = 0 To facets(i).Length - 1
                fp.facets.Add(New cv.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            task.fpMap.FillConvexPoly(fp.facets, i, task.lineType)
            Dim xlist As New List(Of Integer)
            Dim ylist As New List(Of Integer)
            For j = 0 To facets(i).Length - 1
                Dim pt = New cv.Point(facets(i)(j).X, facets(i)(j).Y)
                xlist.Add(pt.X)
                ylist.Add(pt.Y)
                fp.facets.Add(pt)
            Next

            Dim minX = xlist.Min, minY = ylist.Min, maxX = xlist.Max, maxY = ylist.Max
            Dim mms() As Single = {minX, minY, maxX, maxY}
            fp = buildRect(fp, mms)
            fp.ptCenter = GetMaxDist(fp)

            If minX < 0 Or minY < 0 Or maxX >= dst2.Width Or maxY >= dst2.Height Then
                fp = findRect(fp, mms)
                fp.periph = True
            End If

            If fp.pt.X >= dst2.Width Or fp.pt.X < 0 Or fp.pt.Y >= dst2.Height Or fp.pt.Y < 0 Then
                fp.pt = fp.ptCenter
            End If

            cv.Cv2.MeanStdDev(task.pcSplit(2)(fp.rect), depthMean, stdev, fp.mask)
            fp.depthMean = depthMean(0)
            fp.depthStdev = stdev(0)
            Dim mask As cv.Mat = fp.mask And task.depthMask(fp.rect)
            Dim mm = GetMinMax(task.pcSplit(2)(fp.rect), mask)
            fp.depthMin = mm.minVal
            fp.depthMax = mm.maxVal
            fp.colorTracking = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))

            cv.Cv2.MeanStdDev(task.color(fp.rect), fp.colorMean, fp.colorStdev, fp.mask)

            fp.age = 1
            task.fpList.Add(fp)
            DrawContour(task.fpOutline, fp.facets, 255, 1)
        Next

        task.fpMap.ConvertTo(dst3, cv.MatType.CV_8U)
        dst2 = ShowPalette(dst3 * 255 / (facets.Length + 1))

        If standalone Then fpDisplayCell()

        dst2.SetTo(black, task.fpOutline)
        labels(2) = traceName + ": " + Format(task.features.Count, "000") + " cells were present."
    End Sub
End Class





Public Class FCS_TravelDistance : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Display the travel distance "
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        fcs.Run(src)
        dst2 = fcs.dst2
        dst2.SetTo(0, task.fpOutline)
        labels(2) = fcs.labels(2)

        dst3.SetTo(0)
        Dim travelCount As Integer
        Dim distanceList As New List(Of Single)
        distanceList.Add(0)
        For Each fp In task.fpList
            If fp.age > 20 Then
                If fp.travelDistance > 0.5 Then
                    SetTrueText(Format(fp.travelDistance, fmt0), fp.ptCenter, 3)
                    travelCount += 1
                    distanceList.Add(fp.travelDistance)
                End If
            End If
        Next
        labels(3) = "Travel distance average = " + Format(distanceList.Average, fmt1) + ", max = " +
                    Format(distanceList.Max, fmt1)
        fpDisplayMotion()
        fpDisplayCell()
    End Sub
End Class






Public Class FCS_Info : Inherits TaskParent
    Public fpSelection As fpData
    Public Sub New()
        desc = "Display the contents of the Feature Coordinate System (FCS) cell."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        task.feat.Run(src)
        Dim fp = fpSelection
        If fp Is Nothing Then fp = task.fpSelected
        If task.fpList.Count = 0 Then
            SetTrueText("FCS_Info can be called in any algorithm that has setup the task.fplist" + vbCrLf +
                        "It does not appear that task.fpList has any contents so no results to show.")
            Exit Sub
        Else
            If fp Is Nothing Then fp = task.fpList(task.fpMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X))
        End If

        strOut = "Feature point: " + fp.pt.ToString + vbCrLf + vbCrLf
        strOut += "Travel distance: " + Format(fp.travelDistance, fmt1) + vbCrLf
        strOut += "Rect: x/y " + CStr(fp.rect.X) + "/" + CStr(fp.rect.Y) + " w/h "
        strOut += CStr(fp.rect.Width) + "/" + CStr(fp.rect.Height) + vbCrLf
        strOut += "ID = " + Format(fp.ID, fmt1) + ", index = " + CStr(fp.index) + vbCrLf
        strOut += "age (in frames) = " + CStr(fp.age) + vbCrLf + "indexLast = " + CStr(fp.indexLast) + vbCrLf
        strOut += "Facet count = " + CStr(fp.facets.Count) + " facets" + vbCrLf
        strOut += "ClickPoint = " + task.ClickPoint.ToString + vbCrLf + vbCrLf
        Dim vec = task.pointCloud.Get(Of cv.Point3f)(fp.pt.Y, fp.pt.X)
        strOut += "Pointcloud at fp.pt: " + Format(vec.X, fmt1) + "/" +
                                            Format(vec.Y, fmt1) + "/" +
                                            Format(vec.Z, fmt1) + vbCrLf
        strOut += "Depth min/mean/max: " + Format(fp.depthMin, fmt1) + "/" + Format(fp.depthMean, fmt1) + "/" +
                                           Format(fp.depthMax, fmt1) + vbCrLf
        strOut += "Color mean B/G/R: " + Format(fp.colorMean(0), fmt1) + "/" +
                                         Format(fp.colorMean(1), fmt1) + "/" +
                                         Format(fp.colorMean(2), fmt1) + vbCrLf
        strOut += "Color Stdev B/G/R: " + Format(fp.colorStdev(0), fmt1) + "/" +
                                          Format(fp.colorStdev(1), fmt1) + "/" +
                                          Format(fp.colorStdev(2), fmt1) + vbCrLf
        strOut += vbCrLf
        strOut += "Index " + vbTab + "Facet X" + vbTab + "Facet Y" + vbCrLf
        For i = 0 To fp.facets.Count - 1
            strOut += CStr(i) + ":" + vbTab + CStr(fp.facets(i).X) + vbTab + CStr(fp.facets(i).Y) + vbCrLf
        Next

        If standalone Then
            SetTrueText("Select a feature grid cell to get more information.", 2)
        End If
    End Sub
End Class






Public Class FCS_InfoTest : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Dim info As New FCS_Info
    Public Sub New()
        desc = "Invoke FCS_Basics and display the contents of the selected feature point cell"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        fcs.Run(src)
        dst2 = fcs.dst2

        info.Run(src)
        SetTrueText(info.strOut, 3)

        fpDisplayCell()
    End Sub
End Class






Public Class FCS_ByDepth : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Dim fcs As New FCS_Basics
    Dim palInput As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        plot.addLabels = False
        plot.removeZeroEntry = True
        plot.createHistogram = True
        If standalone Then task.gOptions.setDisplay1()
        task.gOptions.setHistogramBins(20)
        desc = "Use cell depth to break down the layers in an image."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        fcs.Run(src)
        dst2 = fcs.dst2
        labels(2) = fcs.labels(2)

        Dim cellList As New List(Of Single)
        For Each fp In task.fpList
            cellList.Add(fp.depthMean)
        Next

        plot.minRange = 0
        plot.maxRange = task.MaxZmeters
        plot.Run(cv.Mat.FromPixelData(cellList.Count, 1, cv.MatType.CV_32F, cellList.ToArray))
        dst1 = plot.dst2

        Dim incr = dst1.Width / task.histogramBins
        Dim histIndex = Math.Truncate(task.mouseMovePoint.X / incr)
        dst1.Rectangle(New cv.Rect(CInt(histIndex * incr), 0, incr, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
        Dim depthIncr = (plot.maxRange - plot.minRange) / task.histogramBins
        Dim depthStart = histIndex * depthIncr
        Dim depthEnd = (histIndex + 1) * depthIncr

        Static depthCells As New List(Of (fpData, Integer))
        Static histIndexSave = histIndex

        If histIndexSave <> histIndex Or task.optionsChanged Then
            histIndexSave = histIndex
            depthCells.Clear()
        End If
        palInput.SetTo(0)

        For Each fp In task.fpList
            If fp.depthMean > depthStart And fp.depthMean < depthEnd Then
                Dim val = palInput.Get(Of Byte)(fp.pt.Y, fp.pt.X)
                If val = 0 Then
                    palInput(fp.rect).SetTo(fp.index, fp.mask)
                    depthCells.Add((fp, task.frameCount))
                End If
            End If
        Next

        For Each ele In depthCells
            Dim fp As fpData = ele.Item1
            SetTrueText(Format(fp.age, fmt0), fp.ptCenter, 0)
            fpCellContour(fp, task.color, 0)
        Next
        dst3 = ShowPalette(palInput * 255 / task.fpList.Count)

        Dim removeFrame As Integer = If(task.frameCount > task.frameHistoryCount, task.frameCount - task.frameHistoryCount, -1)
        For i = depthCells.Count - 1 To 0 Step -1
            Dim frame = depthCells(i).Item2
            If frame = removeFrame Then depthCells.RemoveAt(i)
        Next

        labels(3) = "Cells with depth between " + Format(depthStart, fmt1) + "m to " + Format(depthEnd, fmt1) + "m"
    End Sub
End Class





Public Class FCS_KNNfeatures : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Dim knn As New KNNorm_Basics
    Dim info As New FCS_Info
    Dim dimension As Integer
    Public Sub New()
        task.gOptions.debugSyncUI.Checked = True
        If standalone Then task.gOptions.setDisplay1()
        optiBase.FindSlider("KNN Dimension").Value = 10
        desc = "Can we distinguish each feature point cell with color, depth, and grid."
    End Sub
    Private Function buildEntry(fp As fpData) As List(Of Single)
        Dim dataList As New List(Of Single)
        For i = 0 To dimension - 1
            dataList.Add(Choose(i + 1, fp.depthMean, fp.depthMin, fp.depthMax,
                                       fp.colorMean(0), fp.colorMean(1), fp.colorMean(2),
                                       fp.depthStdev, fp.colorStdev(0), fp.colorStdev(1),
                                       fp.colorStdev(2)))
        Next
        Return dataList
    End Function
    Public Overrides Sub runAlg(src As cv.Mat)
        Static dimensionSlider = optiBase.FindSlider("KNN Dimension")
        dimension = dimensionSlider.value

        fcs.Run(src)
        dst2 = fcs.dst2

        Static fpSave As fpData
        If task.firstPass Or task.mouseClickFlag Then
            fpSave = task.fpList(task.fpMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X))
        End If

        info.Run(src)
        SetTrueText(info.strOut, 1)

        Dim query = buildEntry(fpSave)
        knn.queryInput.Clear()
        For Each e In query
            knn.queryInput.Add(e)
        Next

        knn.trainInput.Clear()

        For Each fp In task.fpList
            Dim entry = buildEntry(fp)
            For Each e In entry
                knn.trainInput.Add(e)
            Next
        Next

        knn.Run(src)

        fpDisplayCell()
        fpCellContour(task.fpSelected, dst2)
        For i = 0 To 10
            Dim fp = task.fpList(knn.result(0, i))
            fpCellContour(fp, task.color, 1)
            SetTrueText(CStr(i), fp.ptCenter, 3)
        Next

        info.fpSelection = task.fpList(knn.result(0, 0))
        info.Run(src)
        SetTrueText(info.strOut, 3)
        task.ClickPoint = info.fpSelection.ptCenter
    End Sub
End Class







Public Class FCS_Tracker : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        labels(3) = "dst2 is a tracking color while dst3 is the color mean"
        desc = "Track the selected cell"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        fcs.Run(src)
        dst1 = fcs.dst2
        labels(2) = fcs.labels(2)
        labels(1) = labels(2)

        fpDisplayCell()

        Dim colors As New List(Of cv.Scalar)
        For i = 0 To task.fpList.Count - 1
            Dim fp = task.fpList(i)
            If colors.Contains(fp.colorTracking) Then
                fp.colorTracking = New cv.Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255))
                task.fpList(i) = fp
            End If
            dst2(fp.rect).SetTo(fp.colorTracking, fp.mask)
            dst3(fp.rect).SetTo(fp.colorMean, fp.mask)

            colors.Add(fp.colorTracking)
        Next

        task.ClickPoint = task.fpSelected.ptCenter
    End Sub
End Class







Public Class FCS_Lines1 : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim fcs As New FCS_Basics
    Public Sub New()
        labels = {"", "Edge_Canny", "Line_Basics output", "Feature_Basics Output"}
        desc = "Use lines as input to FCS."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        lines.Run(src)

        task.features.Clear()
        For Each lp In task.lpList
            task.features.Add(lp.center)
        Next

        fcs.Run(src)
        dst2 = fcs.dst2
        dst2.SetTo(white, lines.dst2)

        For Each lp In task.lpList
            DrawCircle(dst2, lp.center, task.DotSize, red, -1)
            dst0.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
            dst2.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
        Next

        fpDisplayAge()
        fpDisplayCell()

        If task.heartBeat Then labels(2) = CStr(task.features.Count) + " lines were found."
    End Sub
End Class






Public Class FCS_Lines : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim fcs As New FCS_Basics
    Public Sub New()
        optiBase.FindSlider("Min Line Length").Value = 60
        optiBase.FindSlider("Distance to next center").Value = 1
        labels(3) = "Cell boundaries with the age (in frames) for each cell."
        desc = "Use lines as input to FCS."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        Static minSlider = optiBase.FindSlider("Min Distance to next")
        Dim minDistance = minSlider.value
        lines.Run(src)

        task.features.Clear()
        For Each lp In task.lpList
            Dim pair = lp.perpendicularPoints(lp.p1, minDistance)
            task.features.Add(pair.Item1)
            task.features.Add(pair.Item2)

            pair = lp.perpendicularPoints(lp.p2, minDistance)
            task.features.Add(pair.Item1)
            task.features.Add(pair.Item2)

            dst2.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
        Next

        fcs.Run(src)
        dst2 = fcs.dst2
        dst2.SetTo(white, lines.dst2)

        For Each pt In task.features
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next
        task.color.SetTo(0, task.fpOutline)
        fpDisplayAge()

        If task.heartBeat Then labels(2) = CStr(task.features.Count) + " lines were used to create " +
                                           CStr(task.fpList.Count) + " cells"
    End Sub
End Class





Public Class FCS_WithAge : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Public Sub New()
        labels(3) = "Ages are kept below 1000 to make the output more readable..."
        desc = "Display the age of each cell."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        fcs.Run(src)
        dst2 = fcs.dst2
        labels(2) = fcs.labels(2)

        dst3.SetTo(0)
        For Each fp In task.fpList
            DrawCircle(dst3, fp.pt, task.DotSize, task.HighlightColor)
            Dim age = If(fp.age >= 900, fp.age Mod 900 + 100, fp.age)
            SetTrueText(CStr(age), fp.pt, 3)
        Next
    End Sub
End Class





Public Class FCS_BestAge : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Public Sub New()
        labels(3) = "Ages are kept below 1000 to make the output more readable..."
        desc = "Display the top X oldest (best) cells."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        fcs.Run(src)
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
            DrawCircle(dst3, fp.pt, task.DotSize, task.HighlightColor)
            Dim age = If(fp.age >= 900, fp.age Mod 900 + 100, fp.age)
            SetTrueText(CStr(age), fp.pt, 3)
            maxIndex += 1
            If maxIndex >= 10 Then Exit For
        Next
    End Sub
End Class







Public Class FCS_RedCloud : Inherits TaskParent
    Dim redCombo As New RedColor_Combine
    Dim fcs As New FCS_Basics
    Dim knnMin As New KNN_MinDistance
    Public Sub New()
        desc = "Use the RedCloud maxDist points as feature points in an FCS display."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        redCombo.Run(src)
        dst3 = redCombo.dst2
        labels(2) = redCombo.labels(2)

        knnMin.inputPoints.Clear()
        For Each rc In task.redCells
            knnMin.inputPoints.Add(rc.maxDist)
        Next
        knnMin.Run(src)

        task.features = New List(Of cv.Point2f)(knnMin.outputPoints2f)
        fcs.Run(src)
        dst2 = fcs.dst2
        fpDisplayCell()
        labels(3) = fcs.labels(2)
    End Sub
End Class





Public Class FCS_RedCloud1 : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        labels(1) = "Output of FCS_Basics."
        desc = "Isolate FCS cells for each redCell."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If standalone Then getRedCloud(src)
        dst2 = task.redC.dst2
        labels(2) = task.redC.labels(2)

        fcs.Run(src)
        dst1 = fcs.dst2
        labels(3) = fcs.labels(2)
        For Each fp In task.fpList
            Dim val = dst2.Get(Of cv.Vec3b)(fp.ptCenter.Y, fp.ptCenter.X)
            dst3(fp.rect).SetTo(val, fp.mask)
        Next
    End Sub
End Class
