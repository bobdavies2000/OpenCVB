Imports System.Runtime.InteropServices
Imports System.Windows.Documents
Imports cvb = OpenCvSharp
Public Class FCS_Basics : Inherits TaskParent
    Dim delaunay As New FCS_Delaunay
    Public buildFeatures As Boolean = True
    Dim match As New Match_Basics
    Dim nabes As New FCS_Neighbors
    Dim options As New Options_FCSMatch
    Dim feat As New Feature_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        task.ClickPoint = New cvb.Point2f(dst2.Width / 2, dst2.Height / 2)
        desc = "Build a Feature Coordinate System by subdividing an image based on the points provided."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        task.fpSrc = src.Clone
        If buildFeatures Then feat.Run(src)

        task.fpListLast = New List(Of fpData)(task.fpList)
        task.fpMapLast = task.fpMap.Clone
        Static fpLastSrc = src.Clone

        delaunay.Run(src)

        nabes.buildNeighbors()
        nabes.buildNeighborImage()

        Dim matchCount As Integer
        For i = 0 To task.fpList.Count - 1
            Dim fp = task.fpList(i)
            Dim indexLast = task.fpMapLast.Get(Of Integer)(fp.ptCenter.Y, fp.ptCenter.X)
            If indexLast < task.fpListLast.Count Then
                Dim fpLast = task.fpListLast(indexLast)
                Dim index = task.fpMap.Get(Of Integer)(fpLast.ptCenter.Y, fpLast.ptCenter.X)
                If index = fp.index Then
                    ' is this the same point?
                    match.template = fpLastSrc(fpLast.rect)
                    match.Run(src(fpLast.rect))
                    fp.correlation = match.correlation
                    If match.correlation > options.MinCorrelation Then
                        task.fpList(i) = fpUpdate(fp, fpLast)
                        matchCount += 1
                    End If
                End If
            End If
        Next

        dst3 = task.fpOutline
        If task.heartBeat Then dst1.SetTo(0)
        For Each fp In task.fpList
            SetTrueText(CStr(fp.age), fp.ptCenter, 3)
            If fp.correlation > options.MinCorrelation And fp.age > 5 Then
                DrawCircle(dst1, fp.pt, task.DotSize, task.HighlightColor)
            End If
        Next
        dst2 = ShowPalette(task.fpMap * 255 / task.fpList.Count)

        dst0 = src.Clone
        SetTrueText(CStr(task.fpSelected.age), task.fpSelected.ptCenter, 0)
        For i = 0 To task.fpSelected.facets.Count - 1
            Dim p1 = task.fpSelected.facets(i)
            Dim p2 = task.fpSelected.facets((i + 1) Mod task.fpSelected.facets.Count)
            dst2.Line(p1, p2, cvb.Scalar.White, task.lineWidth, task.lineType)
            dst0.Line(p1, p2, cvb.Scalar.White, task.lineWidth, task.lineType)
        Next

        Dim matchPercent = matchCount / task.features.Count
        If task.heartBeat Then
            labels(2) = Format(matchPercent, "0%") + " were found and matched to the previous frame or " +
                        CStr(matchCount) + " of " + CStr(task.features.Count)
        End If
        labels(3) = Format(matchPercent, "0%") + " matched to previous frame (instantaneous update)"
        fpLastSrc = src.Clone
    End Sub
End Class






Public Class FCS_BasicsOld : Inherits TaskParent
    Public feat As New Feature_Basics
    Public buildFeatures As Boolean = True
    Dim match As New Match_Basics
    Dim nabes As New FCS_Neighbors
    Dim subdiv As New cvb.Subdiv2D
    Dim mask32s As New cvb.Mat(dst2.Size, cvb.MatType.CV_32S, 0)
    Dim options As New Options_FCSMatch
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        task.ClickPoint = New cvb.Point2f(dst2.Width / 2, dst2.Height / 2)
        desc = "Build a Feature Coordinate System by subdividing an image based on the points provided."
    End Sub
    Private Function buildRect(fp As fpData, mms() As Single) As fpData
        fp.rect = ValidateRect(New cvb.Rect(mms(0), mms(1), mms(2) - mms(0) + 1, mms(3) - mms(1) + 1))

        mask32s(fp.rect).SetTo(0)
        mask32s.FillConvexPoly(fp.facets, white, task.lineType)
        mask32s(fp.rect).ConvertTo(fp.mask, cvb.MatType.CV_8U)

        Return fp
    End Function
    Private Function findRect(fp As fpData, mms() As Single) As fpData
        Dim pts As cvb.Mat = fp.mask.FindNonZero()

        Dim points(pts.Total * 2 - 1) As Integer
        Marshal.Copy(pts.Data, points, 0, points.Length)

        Dim minX As Integer = Integer.MaxValue, miny As Integer = Integer.MaxValue
        Dim maxX As Integer, maxY As Integer
        For i = 0 To points.Length - 1 Step 2
            Dim x = points(i)
            Dim y = points(i + 1)
            If x < minX Then minX = x
            If y < miny Then miny = y
            If x > maxX Then maxX = x
            If y > maxY Then maxY = y
        Next

        fp.mask = fp.mask(New cvb.Rect(minX, miny, maxX - minX + 1, maxY - miny + 1))
        fp.rect = New cvb.Rect(fp.rect.X + minX, fp.rect.Y + miny, maxX - minX + 1, maxY - miny + 1)
        Return fp
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        task.fpSrc = src.Clone
        If buildFeatures Then feat.Run(src)

        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(task.features)

        Dim facets = New cvb.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        task.fpListLast = New List(Of fpData)(task.fpList)
        task.fpMapLast = task.fpMap.Clone

        task.fpList.Clear()
        task.fpIDlist.Clear()
        Static fpLastSrc = src.Clone

        Dim depthMean As cvb.Scalar, stdev As cvb.Scalar
        task.fpOutline = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
        For i = 0 To facets.Length - 1
            Dim fp = New fpData
            If i < task.features.Count Then fp.pt = task.features(i)
            fp.index = i

            fp.ID = CSng(task.gridMap32S.Get(Of Integer)(fp.pt.Y, fp.pt.X))

            While 1
                If task.fpIDlist.Contains(fp.ID) Then fp.ID += 0.1 Else Exit While
            End While

            task.fpIDlist.Add(fp.ID)

            fp.facet2f = New List(Of cvb.Point2f)(facets(i))
            fp.facets = New List(Of cvb.Point)

            Dim xlist As New List(Of Integer)
            Dim ylist As New List(Of Integer)
            For j = 0 To facets(i).Length - 1
                Dim pt = New cvb.Point(facets(i)(j).X, facets(i)(j).Y)
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

            cvb.Cv2.MeanStdDev(task.pcSplit(2)(fp.rect), depthMean, stdev, fp.mask)
            fp.depthMean = depthMean(0)
            fp.depthStdev = stdev(0)

            cvb.Cv2.MeanStdDev(task.color(fp.rect), fp.colorMean, stdev, fp.mask)

            fp.age = 1
            task.fpList.Add(fp)
            drawFeaturePoints(task.fpOutline, fp.facets, cvb.Scalar.White)
        Next

        task.fpMap.SetTo(0)
        For Each fp In task.fpList
            task.fpMap(fp.rect).SetTo(fp.index, fp.mask)
        Next

        nabes.buildNeighbors()
        nabes.buildNeighborImage()

        Dim matchCount As Integer
        For i = 0 To task.fpList.Count - 1
            Dim fp = task.fpList(i)
            Dim indexLast = task.fpMapLast.Get(Of Integer)(fp.ptCenter.Y, fp.ptCenter.X)
            If indexLast < task.fpListLast.Count Then
                Dim fpLast = task.fpListLast(indexLast)
                Dim index = task.fpMap.Get(Of Integer)(fpLast.ptCenter.Y, fpLast.ptCenter.X)
                If index = fp.index Then
                    ' is this the same point?
                    match.template = fpLastSrc(fpLast.rect)
                    match.Run(src(fpLast.rect))
                    fp.correlation = match.correlation
                    If match.correlation > options.MinCorrelation Then
                        task.fpList(i) = fpUpdate(fp, fpLast)
                        matchCount += 1
                    End If
                End If
            End If
        Next

        dst3 = task.fpOutline
        If task.heartBeat Then dst1.SetTo(0)
        For Each fp In task.fpList
            SetTrueText(CStr(fp.age), fp.ptCenter, 3)
            If fp.correlation > options.MinCorrelation And fp.age > 5 Then
                DrawCircle(dst1, fp.pt, task.DotSize, task.HighlightColor)
            End If
        Next
        dst2 = ShowPalette(task.fpMap * 255 / task.fpList.Count)

        dst0 = src.Clone
        SetTrueText(CStr(task.fpSelected.age), task.fpSelected.ptCenter, 0)
        For i = 0 To task.fpSelected.facets.Count - 1
            Dim p1 = task.fpSelected.facets(i)
            Dim p2 = task.fpSelected.facets((i + 1) Mod task.fpSelected.facets.Count)
            dst2.Line(p1, p2, cvb.Scalar.White, task.lineWidth, task.lineType)
            dst0.Line(p1, p2, cvb.Scalar.White, task.lineWidth, task.lineType)
        Next

        Dim matchPercent = matchCount / task.features.Count
        If task.heartBeat Then
            labels(2) = Format(matchPercent, "0%") + " were found and matched to the previous frame or " +
                        CStr(matchCount) + " of " + CStr(task.features.Count)
        End If
        labels(3) = Format(matchPercent, "0%") + " matched to previous frame (instantaneous update)"
        fpLastSrc = src.Clone
    End Sub
End Class






Public Class FCS_Info : Inherits TaskParent
    Public Sub New()
        desc = "Display the contents of the Feature Coordinate System (FCS) cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standalone Then
            SetTrueText("Call FCS_Info from any algorithm to display the task.fpSelected fpData")
            Exit Sub
        End If
        Dim fp = task.fpSelected
        strOut = "FCS cell selected: " + vbCrLf
        strOut += "Feature point: " + fp.pt.ToString + vbCrLf + vbCrLf
        strOut += "Travel distance: " + Format(fp.travelDistance, fmt1) + vbCrLf
        strOut += "Average Travel distance: " + Format(task.fpTravelAvg, fmt1) + vbCrLf + vbCrLf
        strOut += "Rect: x/y " + CStr(fp.rect.X) + "/" + CStr(fp.rect.Y) + " w/h "
        strOut += CStr(fp.rect.Width) + "/" + CStr(fp.rect.Height) + vbCrLf
        strOut += "ID = " + Format(fp.ID, fmt1) + ", index = " + CStr(fp.index) + vbCrLf
        strOut += "age (in frames) = " + CStr(fp.age) + ", indexLast = " + CStr(fp.indexLast) + vbCrLf
        strOut += "Facet count = " + CStr(fp.facets.Count) + " facets" + vbCrLf
        strOut += "ClickPoint = " + task.ClickPoint.ToString + vbCrLf + vbCrLf
        Dim vec = task.pointCloud.Get(Of cvb.Point3f)(fp.pt.Y, fp.pt.X)
        strOut += "Pointcloud at fp.pt: " + Format(vec.X, fmt1) + "/" + Format(vec.Y, fmt1) + "/" +
                                            Format(vec.Z, fmt1) + vbCrLf
        strOut += "Pointcloud mean: " + Format(fp.depthMean, fmt1) + vbCrLf
        strOut += "Color mean B/G/R: " + Format(fp.colorMean(0), fmt1) + "/" +
                              Format(fp.colorMean(1), fmt1) + "/" + Format(fp.colorMean(2), fmt1) + vbCrLf
        strOut += "Neighbor Count = " + CStr(fp.nabeList.Count) + vbCrLf
        strOut += "Neighbors: "
        For Each index In fp.nabeList
            strOut += CStr(index) + ", "
        Next
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





Public Class FCS_Lines : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim fcs As New FCS_Basics
    Public Sub New()
        fcs.buildFeatures = False
        If standalone Then task.gOptions.setDisplay0()
        labels = {"", "Edge_Canny", "Line_Basics output", "Feature_Basics Output"}
        desc = "Use lines as input to FCS."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst0 = src

        lines.Run(src)

        task.features.Clear()
        For Each lp In lines.lpList
            task.features.Add(lp.center)
        Next

        fcs.Run(src)
        dst2 = fcs.dst2
        dst2.SetTo(white, lines.dst3)

        For i = 0 To lines.lpList.Count - 1
            Dim lp = lines.lpList(i)
            DrawCircle(dst2, lp.center, task.DotSize, red, -1)
            dst0.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
            dst2.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
        Next

        displayAge()

        If task.heartBeat Then labels(2) = CStr(task.features.Count) + " lines were found."
    End Sub
End Class







Public Class FCS_ViewLeft : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        desc = "Build an FCS for left view."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        fcs.Run(task.leftView)
        dst0 = fcs.dst0
        dst2 = fcs.dst2
        dst3 = fcs.dst3

        displayAge()

        labels(2) = fcs.labels(2)
    End Sub
End Class







Public Class FCS_ViewRight : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        desc = "Build an FCS for right view."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        fcs.Run(task.rightView)
        dst0 = fcs.dst0
        dst2 = fcs.dst2
        dst3 = fcs.dst3

        displayAge()

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
    Public Sub RunAlg(src As cvb.Mat)
        fcs.Run(src)
        dst2 = fcs.dst1
        If task.fpListLast.Count = 0 Then task.fpListLast = New List(Of fpData)(task.fpList)

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

        plot.plotData = New cvb.Scalar(motionPercent, 0, 0)
        plot.Run(empty)
        dst1 = plot.dst2
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
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Using all the feature points with motion, determine any with a common direction."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        fcsM.Run(src)
        mats.mat(2) = fcsM.dst2
        mats.mat(3) = fcsM.dst3

        plothist.maxRange = task.histogramBins / 2 Or 1
        plothist.minRange = -plothist.maxRange
        rangeText = " ranging from " + CStr(plothist.minRange) + " to " + CStr(plothist.maxRange)
        range = Math.Abs(plothist.maxRange - plothist.minRange)

        Dim incr = range / task.histogramBins

        plothist.Run(cvb.Mat.FromPixelData(fcsM.xDist.Count, 1, cvb.MatType.CV_32F, fcsM.xDist.ToArray))
        Dim xDist As New List(Of Single)(plothist.histArray)
        task.fpMotion.X = plothist.minRange + xDist.IndexOf(xDist.Max) * incr
        mats.mat(0) = plothist.dst2.Clone

        plothist.Run(cvb.Mat.FromPixelData(fcsM.yDist.Count, 1, cvb.MatType.CV_32F, fcsM.yDist.ToArray))
        Dim yDist As New List(Of Single)(plothist.histArray)
        task.fpMotion.Y = plothist.minRange + yDist.IndexOf(yDist.Max) * incr
        mats.mat(1) = plothist.dst2.Clone

        mats.Run(empty)
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
        SetTrueText("Y distances " + rangeText, New cvb.Point(dst2.Width / 2 + 2, 0), 2)
        labels = fcsM.labels

        If standalone Then
            dst0 = src.Clone
            For Each fp In task.fpList
                DrawCircle(dst0, fp.pt, task.DotSize, task.HighlightColor)
            Next
        End If
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
    Public Sub RunAlg(src As cvb.Mat)
        flood.Run(src)
        dst2 = flood.dst2

        fcs.Run(src)
        dst1 = src

        edges.Run(src)
        dst3 = edges.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

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
        dst3.SetTo(cvb.Scalar.White, task.fpOutline)
    End Sub
End Class







Public Class FCS_RedCloud : Inherits TaskParent
    Dim redC As New RedCloud_Combine
    Dim fcs As New FCS_Basics
    Dim knnMin As New KNN_MinDistance
    Public Sub New()
        fcs.buildFeatures = False
        desc = "Use the RedCloud maxDist points as feature points in an FCS display."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        knnMin.inputPoints.Clear()
        For Each rc In task.redCells
            knnMin.inputPoints.Add(rc.maxDist)
        Next
        knnMin.Run(src)

        task.features = New List(Of cvb.Point2f)(knnMin.outputPoints2f)
        fcs.Run(src)
        dst3 = fcs.dst2
        labels(3) = fcs.labels(2)
    End Sub
End Class







Public Class FCS_Periphery : Inherits TaskParent
    Dim fcs As New FCS_Basics

    Public ptOutside As New List(Of cvb.Point2f)
    Public ptOutID As New List(Of Single)

    Public ptInside As New List(Of cvb.Point2f)
    Public ptInID As New List(Of Single)
    Public Sub New()
        desc = "Display the cells which are on the periphery of the image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        fcs.Run(src)
        dst2 = fcs.dst2

        dst3 = dst2.Clone
        ptOutside.Clear()
        ptOutID.Clear()
        ptInside.Clear()
        ptInID.Clear()

        For Each fp In task.fpList
            If fp.periph Then
                dst3(fp.rect).SetTo(cvb.Scalar.Gray, fp.mask)
                DrawCircle(dst3, fp.pt, task.DotSize, task.HighlightColor)
                ptOutside.Add(fp.pt)
                ptOutID.Add(fp.ID)
            Else
                ptInside.Add(fp.pt)
                ptInID.Add(fp.ID)
            End If
        Next
        dst3.Rectangle(task.fpSelected.rect, task.HighlightColor, task.lineWidth)
    End Sub
End Class








Public Class FCS_Edges : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Dim edges As New Edge_Canny
    Public Sub New()
        desc = "Use edges to connect feature points to their neighbors."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)

        fcs.Run(src)
        dst2 = src

        edges.Run(src)
        dst3 = edges.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        For Each fp In task.fpList
            DrawCircle(dst2, fp.ptCenter, task.DotSize, task.HighlightColor)
            DrawCircle(dst3, fp.ptCenter, task.DotSize, task.HighlightColor)
        Next
        dst3.SetTo(cvb.Scalar.White, task.fpOutline)
    End Sub
End Class






'Public Class FCS_MatchDepthColor : Inherits TaskParent
'    Dim fcs As New FCS_Basics
'    Dim match As New Match_Basics
'    Dim options As New Options_FCSMatch
'    Public Sub New()
'        desc = "Track each feature with FCS"
'    End Sub
'    Public Sub RunAlg(src As cvb.Mat)
'        options.RunOpt()

'        fcs.Run(src)
'        dst2 = fcs.dst2
'        Dim fp1 = task.fpSelected
'        dst2(fp1.rect).SetTo(cvb.Scalar.White, fp1.mask)

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
'    Public Sub RunAlg(src As cvb.Mat)
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
'        finfo.Run(empty)
'        SetTrueText(finfo.strOut, 3)
'        labels(2) = fcs.labels(2)
'        labels(3) = CStr(task.fpList.Count) + " cells found.  Dots below are at fp.ptCenter (not feature point)"
'        drawFeaturePoints(dst0, fp.facets, cvb.Scalar.White)
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
'    Public Sub RunAlg(src As cvb.Mat)
'        options.RunOpt()

'        edges.Run(src)
'        dst1 = edges.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

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
    Public Sub New()
        dst1 = New cvb.Mat(dst1.Size, cvb.MatType.CV_8U)
        labels(3) = "The neighbor cells with the corner feature rectangles."
        desc = "Show the midpoints in each cell and build the nabelist for each cell"
    End Sub
    Public Sub buildNeighbors()
        For i = 0 To task.fpList.Count - 1
            Dim fp = task.fpList(i)
            Dim facets As New List(Of cvb.Point)(fp.facets)
            If fp.periph Then
                facets.Add(fp.rect.TopLeft)
                facets.Add(fp.rect.Location)
                facets.Add(fp.rect.BottomRight)
                facets.Add(New cvb.Point(fp.rect.Location.X + fp.rect.Width, fp.rect.Location.Y))
            End If
            fp.nabeRect = fp.rect
            For Each pt In facets
                If pt.X < 0 Or pt.X > dst2.Width Then Continue For
                If pt.Y < 0 Or pt.Y > dst2.Height Then Continue For
                Dim index As Integer
                For j = 0 To 8
                    Dim ptNabe = Choose(j + 1, New cvb.Point(pt.X - 1, pt.Y - 1),
                                               New cvb.Point(pt.X, pt.Y - 1),
                                               New cvb.Point(pt.X + 1, pt.Y - 1),
                                               New cvb.Point(pt.X - 1, pt.Y),
                                               New cvb.Point(pt.X, pt.Y),
                                               New cvb.Point(pt.X + 1, pt.Y),
                                               New cvb.Point(pt.X - 1, pt.Y + 1),
                                               New cvb.Point(pt.X, pt.Y + 1),
                                               New cvb.Point(pt.X + 1, pt.Y + 1))
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
    Private Function verifyRect(r As cvb.Rect, sz As Integer, szNew As Integer) As cvb.Rect
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.BottomRight.X >= dst2.Width Then r.X = dst2.Width - szNew
        If r.BottomRight.Y >= dst2.Height Then r.Y = dst2.Height - szNew
        Return r
    End Function
    Public Sub buildNeighborImage()
        task.fpSelected = task.fpList(task.fpMap.Get(Of Integer)(task.ClickPoint.Y, task.ClickPoint.X))
        Dim fp = task.fpSelected
        dst1.SetTo(0)

        dst1(fp.nabeRect).SetTo(0, task.fpOutline(fp.nabeRect))

        For Each fp In task.fpList
            Dim r = fp.rect
            If r.X = 0 And r.Y = 0 Then task.fpCorners(0) = fp.index
            If r.Y = 0 And r.BottomRight.X = dst2.Width Then task.fpCorners(1) = fp.index
            If r.X = 0 And r.BottomRight.Y = dst2.Height Then task.fpCorners(2) = fp.index
            If r.BottomRight.X = dst2.Width Then task.fpCorners(3) = fp.index
        Next
        dst3 = ShowPalette(dst1 * 255 / task.fpList.Count)
        dst3.Rectangle(task.fpSelected.nabeRect, task.HighlightColor, task.lineWidth)

        'Dim sz = task.gOptions.GridSlider.Value
        'For i = 0 To task.fpCorners.Count - 1
        '    fp = task.fpList(task.fpCorners(i))
        '    DrawCircle(dst3, fp.pt, task.DotSize, task.HighlightColor)
        '    Dim r = New cvb.Rect(fp.pt.X - sz, fp.pt.Y - sz, sz * 2, sz * 2)
        '    task.fpCornerRect(i) = verifyRect(r, sz, sz * 2)
        '    dst3.Rectangle(r, task.HighlightColor, task.lineWidth)

        '    r = New cvb.Rect(r.X - sz, r.Y - sz, sz * 4, sz * 4)
        '    task.fpSearchRect(i) = verifyRect(r, sz, sz * 4)
        '    dst3.Rectangle(r, cvb.Scalar.White, task.lineWidth)
        'Next
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        SetTrueText("FCS_Neighbors provides the functions to find neighbors." + vbCrLf +
                    "FCS_Basics always finds the neighbors so it cannot run FCS_Basics.")
    End Sub
End Class







Public Class FCS_ViewLeftRight : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Dim feat As New Feature_Basics
    Public options As New Options_Features
    Public Sub New()
        fcs.buildFeatures = False
        desc = "Use both the left and right features as input to the FCS_Basics"
    End Sub
    Private Function getPoints(src As cvb.Mat) As List(Of cvb.Point2f)
        If src.Channels <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Return cvb.Cv2.GoodFeaturesToTrack(src, options.featurePoints, options.quality,
                                           options.minDistance, New cvb.Mat, options.blockSize,
                                           True, options.k).ToList
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim fLeft = getPoints(task.leftView)
        Dim fRight = getPoints(task.rightView)

        task.features.Clear()
        Dim ptLeft As New List(Of cvb.Point)
        Dim ptRight As New List(Of cvb.Point)
        For Each pt In fLeft
            Dim p = New cvb.Point(CInt(pt.X), CInt(pt.Y))
            task.features.Add(p)
            ptLeft.Add(p)
        Next
        For Each pt In fRight
            Dim p = New cvb.Point(CInt(pt.X), CInt(pt.Y))
            task.features.Add(p)
            ptRight.Add(p)
        Next

        task.features = feat.motionFilter(task.features)
        fcs.Run(src)
        dst2 = fcs.dst2
        dst3 = fcs.dst3.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        For i = 0 To task.fpList.Count - 1
            Dim fp = task.fpList(i)
            Dim index = ptLeft.IndexOf(fp.pt)
            If index >= 0 Then
                dst3(fp.rect).SetTo(cvb.Scalar.Blue, fp.mask)
            Else
                dst3(fp.rect).SetTo(cvb.Scalar.Red, fp.mask)
            End If
            DrawCircle(dst3, fp.ptCenter, task.DotSize, task.HighlightColor)
        Next

        labels(2) = CStr(task.features.Count) + " features with "
        labels(3) = "Left image (blue) had " + CStr(ptLeft.Count) + " points while the right image (red) had " +
                    CStr(ptRight.Count) + " points"
    End Sub
End Class







Public Class FCS_NoTracking : Inherits TaskParent
    Public facetList As New List(Of List(Of cvb.Point))
    Public facet32s As cvb.Mat
    Dim subdiv As New cvb.Subdiv2D
    Public Sub New()
        facet32s = New cvb.Mat(dst2.Size(), cvb.MatType.CV_32SC1, 0)
        dst1 = New cvb.Mat(dst1.Size, cvb.MatType.CV_8U, 0)
        labels(3) = "CV_8U map of Delaunay cells"
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static feat As New Feature_Basics
        feat.Run(src)

        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(task.features)

        Dim facets = New cvb.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        facetList.Clear()
        For i = 0 To facets.Length - 1
            Dim nextFacet As New List(Of cvb.Point)
            For j = 0 To facets(i).Length - 1
                nextFacet.Add(New cvb.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            facet32s.FillConvexPoly(nextFacet, i, task.lineType)
            facetList.Add(nextFacet)
        Next

        dst1.SetTo(0)
        For i = 0 To facets.Length - 1
            Dim ptList As New List(Of cvb.Point)
            For j = 0 To facets(i).Length - 1
                ptList.Add(New cvb.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            DrawContour(dst1, ptList, 255, 1)
        Next

        facet32s.ConvertTo(dst3, cvb.MatType.CV_8U)
        dst2 = ShowPalette(dst3 * 255 / (facets.Length + 1))

        dst3.SetTo(0, dst1)
        dst2.SetTo(white, dst1)
        labels(2) = traceName + ": " + Format(task.features.Count, "000") + " cells were present."
    End Sub
End Class






Public Class FCS_Delaunay : Inherits TaskParent
    Dim subdiv As New cvb.Subdiv2D
    Public Sub New()
        task.fpMap = New cvb.Mat(dst2.Size(), cvb.MatType.CV_32SC1, 0)
        labels(3) = "CV_8U map of Delaunay cells"
        desc = "Subdivide an image based on the points provided."
    End Sub
    Private Function buildRect(fp As fpData, mms() As Single) As fpData
        fp.rect = ValidateRect(New cvb.Rect(mms(0), mms(1), mms(2) - mms(0) + 1, mms(3) - mms(1) + 1))

        Static mask32s As New cvb.Mat(dst2.Size, cvb.MatType.CV_32S, 0)
        mask32s(fp.rect).SetTo(0)
        mask32s.FillConvexPoly(fp.facets, white, task.lineType)
        mask32s(fp.rect).ConvertTo(fp.mask, cvb.MatType.CV_8U)

        Return fp
    End Function
    Private Function findRect(fp As fpData, mms() As Single) As fpData
        Dim pts As cvb.Mat = fp.mask.FindNonZero()

        Dim points(pts.Total * 2 - 1) As Integer
        Marshal.Copy(pts.Data, points, 0, points.Length)

        Dim minX As Integer = Integer.MaxValue, miny As Integer = Integer.MaxValue
        Dim maxX As Integer, maxY As Integer
        For i = 0 To points.Length - 1 Step 2
            Dim x = points(i)
            Dim y = points(i + 1)
            If x < minX Then minX = x
            If y < miny Then miny = y
            If x > maxX Then maxX = x
            If y > maxY Then maxY = y
        Next

        fp.mask = fp.mask(New cvb.Rect(minX, miny, maxX - minX + 1, maxY - miny + 1))
        fp.rect = New cvb.Rect(fp.rect.X + minX, fp.rect.Y + miny, maxX - minX + 1, maxY - miny + 1)
        Return fp
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        If standalone Then
            Static feat As New Feature_Basics
            feat.Run(src)
        End If

        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(task.features)

        Dim facets = New cvb.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        task.fpList.Clear()
        task.fpIDlist.Clear()
        task.fpOutline.SetTo(0)
        Dim depthMean As cvb.Scalar, stdev As cvb.Scalar
        For i = 0 To facets.Length - 1
            Dim fp As New fpData
            fp.pt = task.features(i)
            fp.index = i

            fp.ID = CSng(task.gridMap32S.Get(Of Integer)(fp.pt.Y, fp.pt.X))

            While 1
                If task.fpIDlist.Contains(fp.ID) Then fp.ID += 0.1 Else Exit While
            End While

            task.fpIDlist.Add(fp.ID)

            fp.facets = New List(Of cvb.Point)
            For j = 0 To facets(i).Length - 1
                fp.facets.Add(New cvb.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            task.fpMap.FillConvexPoly(fp.facets, i, task.lineType)
            Dim xlist As New List(Of Integer)
            Dim ylist As New List(Of Integer)
            For j = 0 To facets(i).Length - 1
                Dim pt = New cvb.Point(facets(i)(j).X, facets(i)(j).Y)
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

            cvb.Cv2.MeanStdDev(task.pcSplit(2)(fp.rect), depthMean, stdev, fp.mask)
            fp.depthMean = depthMean(0)
            fp.depthStdev = stdev(0)

            cvb.Cv2.MeanStdDev(task.color(fp.rect), fp.colorMean, fp.colorStdev, fp.mask)

            fp.age = 1
            task.fpList.Add(fp)
            DrawContour(task.fpOutline, fp.facets, 255, 1)
        Next

        task.fpMap.ConvertTo(dst3, cvb.MatType.CV_8U)
        dst2 = ShowPalette(dst3 * 255 / (facets.Length + 1))

        dst2.SetTo(black, task.fpOutline)
        labels(2) = traceName + ": " + Format(task.features.Count, "000") + " cells were present."
    End Sub
End Class