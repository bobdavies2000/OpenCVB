Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp
Public Class FCS_Basics : Inherits TaskParent
    Public featureInput As New List(Of cvb.Point2f)
    Dim match As New Match_Basics
    Dim nabes As New FCS_Neighbors
    Dim subdiv As New cvb.Subdiv2D
    Dim mask32s As New cvb.Mat(dst2.Size, cvb.MatType.CV_32S, 0)
    Dim options As New Options_FCSMatch
    Public getNabes As Boolean = True
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
        If standalone Or featureInput.Count = 0 Then
            Static feat As New Feature_Basics
            feat.Run(src)
            featureInput = task.features
        End If

        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(featureInput)

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

            If minX < 0 Or minY < 0 Or maxX >= dst2.Width Or maxY >= dst2.Height Then
                fp = findRect(fp, mms)
                fp.periph = True
            End If

            If fp.pt = newPoint Then fp.pt = New cvb.Point(CInt(xlist.Average), CInt(ylist.Average))
            If fp.pt.X >= dst2.Width Or fp.pt.X < 0 Then fp.pt.X = CInt(fp.rect.X + fp.rect.Width / 2)
            If fp.pt.Y >= dst2.Height Or fp.pt.Y < 0 Then fp.pt.Y = CInt(fp.rect.Y + fp.rect.Height / 2)
            cvb.Cv2.MeanStdDev(task.pcSplit(2)(fp.rect), depthMean, stdev, fp.mask)
            fp.depthMean = depthMean(0)

            cvb.Cv2.MeanStdDev(task.color(fp.rect), fp.colorMean, stdev, fp.mask)

            fp.ptCenter = GetMaxDist(fp)
            fp.age = 1
            task.fpList.Add(fp)
            drawFeaturePoints(task.fpOutline, fp.facets, cvb.Scalar.White)
        Next

        task.fpMap.SetTo(0)
        For Each fp In task.fpList
            task.fpMap(fp.rect).SetTo(fp.index, fp.mask)
        Next

        If getNabes Then nabes.Run(src)

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

        If standaloneTest() Then
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
        End If

        Dim matchPercent = matchCount / featureInput.Count
        If task.heartBeat Then
            labels(2) = Format(matchPercent, "0%") + " were found and matched to the previous frame or " +
                        CStr(matchCount) + " of " + CStr(featureInput.Count)
        End If
        labels(3) = Format(matchPercent, "0%") + " matched to previous frame (instantaneous update)"
        fpLastSrc = src.Clone
    End Sub
End Class








Public Class FCS_CornerCells : Inherits TaskParent
    Dim feat As New Feature_Basics
    Dim fcs As New FCS_Basics
    Dim nabes As New FCS_Neighbors
    Public featureInput As New List(Of cvb.Point2f)
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        FindSlider("Min Distance to next").Value = task.fPointMinDistance
        labels(1) = "The index for each of the cells (if standalonetest)"
        desc = "Feature Coordinate System (FCS) - Create the fpList with rect, mask, index, and facets"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst0 = src.Clone

        If featureInput.Count = 0 Then
            feat.Run(src)
            fcs.featureInput = New List(Of cvb.Point2f)(task.features)
        Else
            fcs.featureInput = featureInput
        End If

        fcs.Run(src)

        dst2 = fcs.dst2

        If task.heartBeat Then labels(2) = CStr(featureInput.Count) + " feature cells."

        nabes.Run(src)
        strOut = nabes.strOut
        dst3 = nabes.dst3

        dst2.SetTo(0, task.fpOutline)
        Dim fp = task.fpSelected
        For i = 0 To fp.facets.Count - 1
            Dim p1 = fp.facets(i)
            Dim p2 = fp.facets((i + 1) Mod fp.facets.Count)
            dst2.Line(p1, p2, cvb.Scalar.White, task.lineWidth, task.lineType)
            dst0.Line(p1, p2, cvb.Scalar.White, task.lineWidth, task.lineType)
        Next

        dst2.Rectangle(fp.rect, task.HighlightColor, task.lineWidth)
        dst0.Rectangle(fp.rect, task.HighlightColor, task.lineWidth)
        dst1.SetTo(0)
        dst1.Rectangle(fp.rect, task.HighlightColor, task.lineWidth)
        SetTrueText(strOut, 3)

        For Each fp In task.fpList
            DrawCircle(dst2, fp.pt, task.DotSize, task.HighlightColor)
            DrawCircle(dst0, fp.pt, task.DotSize, task.HighlightColor)
            ' If fp.indexLast >= 0 Then SetTrueText(Format(fp.ID, fmt1), New cvb.Point(CInt(fp.pt.X), CInt(fp.pt.Y)), 1)
        Next
        labels(2) = fcs.labels(2)
    End Sub
End Class





Public Class FCS_Info : Inherits TaskParent
    Public Sub New()
        desc = "Display the contents of the Feature Coordinate System (FCS) cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.fpList.Count = 0 Then Exit Sub
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
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        labels = {"", "Edge_Canny", "Line_Basics output", "Feature_Basics Output"}
        desc = "Use lines as input to FCS."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst0 = src

        lines.Run(src)
        dst2 = lines.dst3

        fcs.featureInput.Clear()
        For Each lp In lines.lpList
            fcs.featureInput.Add(lp.center)
        Next

        fcs.Run(src)
        dst2 = fcs.dst2
        dst2.SetTo(white, lines.dst3)

        For i = 0 To lines.lpList.Count - 1
            Dim lp = lines.lpList(i)
            DrawCircle(dst2, lp.center, task.DotSize, red, -1)
            dst0.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
            dst2.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
            SetTrueText(CStr(i), lp.center, 1)
        Next

        SetTrueText(fcs.strOut, 3)
        If task.heartBeat Then labels(2) = CStr(fcs.featureInput.Count) + " lines were found."
    End Sub
End Class






Public Class FCS_LinesAndEdges : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim feat As New Feature_Basics
    Dim edges As New Edge_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        labels = {"", "Edge_Canny", "Line_Basics output", "Feature_Basics Output"}
        desc = "Run Feature_Basics and Line_Basics for comparison."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        edges.Run(src)
        dst1 = edges.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        lines.Run(src)
        dst2 = lines.dst3

        feat.Run(src)
        dst3 = feat.dst2

        For Each pt In task.features
            DrawCircle(dst1, pt, task.DotSize, task.HighlightColor)
        Next
    End Sub
End Class






Public Class FCS_NoTracking : Inherits TaskParent
    Public inputPoints As New List(Of cvb.Point2f)
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
        If standalone Then
            Static feat As New Feature_Basics
            feat.Run(src)
            inputPoints = New List(Of cvb.Point2f)(task.features)
        End If

        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(inputPoints)

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
        labels(2) = traceName + ": " + Format(inputPoints.Count, "000") + " cells were present."
    End Sub
End Class







Public Class FCS_ViewLeftRight : Inherits TaskParent
    Dim feat As New Feature_Basics
    Dim fcsL As New FCS_Basics
    Dim fcsR As New FCS_Basics

    Dim saveLeftMap As New cvb.Mat(dst2.Size, cvb.MatType.CV_32S, 0)
    Dim saveLeftList As New List(Of fpData)
    Dim saveLeftIDs As New List(Of Single)

    Dim saveRightMap As New cvb.Mat(dst2.Size, cvb.MatType.CV_32S, 0)
    Dim saveRightList As New List(Of fpData)
    Dim saveRightIDs As New List(Of Single)
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Build an FCS for both left and right views.  NOT WORKING - right image is just what the left has.  NEEDSWORK"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.fpMap = saveLeftMap.Clone
        task.fpList = New List(Of fpData)(saveLeftList)
        task.fpIDlist = New List(Of Single)(saveLeftIDs)

        feat.Run(task.leftView)
        fcsL.featureInput = New List(Of cvb.Point2f)(task.features)

        fcsL.Run(task.leftView)
        dst0 = fcsL.dst0.Clone
        dst2 = fcsL.dst2.Clone

        saveLeftMap = task.fpMap.Clone
        saveLeftList = New List(Of fpData)(task.fpList)
        saveLeftIDs = New List(Of Single)(task.fpIDlist)

        task.fpMap = saveRightMap.Clone
        task.fpList = New List(Of fpData)(saveRightList)
        task.fpIDlist = New List(Of Single)(saveRightIDs)

        feat.Run(task.rightView)
        fcsR.featureInput = New List(Of cvb.Point2f)(task.features)

        fcsR.Run(task.rightView)
        dst1 = fcsR.dst0.Clone
        dst3 = fcsR.dst2.Clone

        saveRightMap = task.fpMap.Clone
        saveRightList = New List(Of fpData)(task.fpList)
        saveRightIDs = New List(Of Single)(task.fpIDlist)
    End Sub
End Class







Public Class FCS_ViewLeft : Inherits TaskParent
    Dim feat As New Feature_Basics
    Dim fcs As New FCS_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        desc = "Build an FCS for left view."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(task.leftView)
        fcs.featureInput = New List(Of cvb.Point2f)(task.features)

        fcs.Run(task.leftView)
        dst0 = fcs.dst0
        dst1 = fcs.dst1
        dst2 = fcs.dst2
        dst3 = fcs.dst3

        labels(2) = fcs.labels(2)
    End Sub
End Class







Public Class FCS_ViewRight : Inherits TaskParent
    Dim feat As New Feature_Basics
    Dim fcs As New FCS_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        desc = "Build an FCS for right view."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(task.rightView)
        fcs.featureInput = New List(Of cvb.Point2f)(task.features)

        fcs.Run(task.rightView)
        dst0 = fcs.dst0
        dst1 = fcs.dst1
        dst2 = fcs.dst2
        dst3 = fcs.dst3
        labels(2) = fcs.labels(2)
    End Sub
End Class






Public Class FCS_DepthCells : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Dim fInfo As New FCS_Info
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        dst1 = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U, 0)
        desc = "Assign the depth of the feature point to the whole cell and display."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst0 = src.Clone
        fcs.Run(src)

        fInfo.Run(empty)
        SetTrueText(fInfo.strOut, 3)

        dst1.SetTo(0)
        For Each fp In task.fpList
            Dim mask = fp.mask And task.depthMask(fp.rect)
            dst1(fp.rect).SetTo(255 * fp.depthMean / task.MaxZmeters, mask)
        Next

        dst2 = ShowPalette(dst1 * 255 / task.fpList.Count)

        For Each fp In task.fpList
            If fp.indexLast Then
                DrawCircle(dst2, fp.pt, task.DotSize, task.HighlightColor)
                DrawCircle(dst0, fp.pt, task.DotSize, task.HighlightColor)
            Else
                DrawCircle(dst2, fp.pt, task.DotSize + 2, cvb.Scalar.Red)
                DrawCircle(dst0, fp.pt, task.DotSize + 2, cvb.Scalar.Red)
            End If
            If fp.indexLast >= 0 Then
                SetTrueText(Format(fp.ID, fmt1), New cvb.Point(CInt(fp.pt.X), CInt(fp.pt.Y)), 1)
            End If
        Next

        For i = 0 To task.fpSelected.facets.Count - 1
            Dim p1 = task.fpSelected.facets(i)
            Dim p2 = task.fpSelected.facets((i + 1) Mod task.fpSelected.facets.Count)
            dst2.Line(p1, p2, cvb.Scalar.White, task.lineWidth + 1, task.lineType)
            dst0.Line(p1, p2, cvb.Scalar.White, task.lineWidth + 1, task.lineType)
        Next
    End Sub
End Class







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

        Dim sz = task.gOptions.GridSlider.Value
        For i = 0 To task.fpCorners.Count - 1
            fp = task.fpList(task.fpCorners(i))
            DrawCircle(dst3, fp.pt, task.DotSize, task.HighlightColor)
            Dim r = New cvb.Rect(fp.pt.X - sz, fp.pt.Y - sz, sz * 2, sz * 2)
            task.fpCornerRect(i) = verifyRect(r, sz, sz * 2)
            dst3.Rectangle(r, task.HighlightColor, task.lineWidth)

            r = New cvb.Rect(r.X - sz, r.Y - sz, sz * 4, sz * 4)
            task.fpSearchRect(i) = verifyRect(r, sz, sz * 4)
            dst3.Rectangle(r, cvb.Scalar.White, task.lineWidth)
        Next
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standalone Then
            Static fcs As New FCS_Basics
            fcs.getNabes = False
            fcs.Run(src)
            dst2 = fcs.dst2
        End If

        buildNeighbors()
        buildNeighborImage()

        fInfo.Run(empty)
        SetTrueText(fInfo.strOut, 3)

        If standalone = False Then
            fInfo.Run(empty)
            strOut = fInfo.strOut
        End If
    End Sub
End Class





Public Class FCS_CornerCorrelation : Inherits TaskParent
    Public options As New Options_Features
    Dim fcs As New FCS_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Search for the previous image corners in the current image to get the camera movement."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Static lastImage As cvb.Mat = src.Clone

        'fcs.Run(src)
        'dst2 = ShowPalette(task.fpMap * 255 / task.fpList.Count)
        'dst2.SetTo(0, task.fpOutline)

        'strOut = "Correlation Coefficients:" + vbCrLf
        'dst1.SetTo(0)
        'dst3.SetTo(0)
        'Dim sz = task.gOptions.getGridSize()
        'For i = 0 To task.fpCorners.Count - 1
        '    Dim r = task.fpCornerRect(i)
        '    Dim searchRect = task.fpSearchRect(i)

        '    cvb.Cv2.MatchTemplate(lastImage(r), src(searchRect), dst0, options.matchOption)
        '    Dim mm = GetMinMax(dst0)

        '    dst3(r) = src(r)
        '    Dim rLast = ValidateRect(New cvb.Rect(r.X + mm.maxLoc.X - sz, r.Y + mm.maxLoc.Y - sz, sz * 2, sz * 2))
        '    dst1(rLast) = lastImage(rLast)

        '    Dim correlation = mm.maxVal

        '    Dim name = Choose(i + 1, "Upper left", "Upper right", "Lower left", "Lower right")
        '    strOut += name + " " + Format(correlation, fmt3) + vbCrLf
        'Next
        'SetTrueText(strOut, New cvb.Point(dst2.Width / 2, dst2.Height / 2), 3)

        'lastImage = src.Clone()
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
        Dim fpLastList = New List(Of fpData)(task.fpList)
        fcs.Run(src)
        dst2 = fcs.dst2

        For Each fp In task.fpList
            DrawCircle(dst2, fp.pt, task.DotSize, task.HighlightColor)
        Next

        dst3.SetTo(0)
        Dim motionCount As Integer, linkedCount As Integer
        xDist.Clear()
        yDist.Clear()
        For Each fp In task.fpList
            If fp.indexLast <> -1 Then
                linkedCount += 1
                Dim p1 = fp.pt
                Dim p2 = fpLastList(fp.indexLast).pt
                If p1 <> p2 Then
                    dst3.Line(p1, p2, task.HighlightColor, task.lineWidth, task.lineType)
                    motionCount += 1
                    xDist.Add(p2.X - p1.X)
                    yDist.Add(p2.Y - p1.Y)
                End If
            End If
        Next
        motionPercent = 100 * motionCount / linkedCount
        If task.heartBeat Then
            labels(2) = fcs.labels(2)
            labels(3) = Format(motionPercent, fmt1) + "% of linked cells had motion or " +
                        CStr(motionCount) + " of " + CStr(linkedCount) + ".    " + Format(task.fpTravelAvg, fmt1) +
                        " average travel distance."
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
        plothist.minRange = -7
        plothist.maxRange = 7
        rangeText = " ranging from " + CStr(plothist.minRange) + " to " + CStr(plothist.maxRange)
        range = Math.Abs(plothist.maxRange - plothist.minRange)
        task.gOptions.setHistogramBins(15) ' should this be an odd number.
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Using all the feature points with motion, determine any with a common direction."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        fcsM.Run(src)
        mats.mat(2) = fcsM.dst2
        mats.mat(3) = fcsM.dst3

        Dim incr = range / task.histogramBins

        plothist.Run(cvb.Mat.FromPixelData(fcsM.xDist.Count, 1, cvb.MatType.CV_32F, fcsM.xDist.ToArray))
        Dim xDist As New List(Of Single)(plothist.histArray)
        task.fpMotion.X = plothist.minRange + xDist.IndexOf(xDist.Max) * incr
        mats.mat(0) = plothist.dst2.Clone

        plothist.Run(cvb.Mat.FromPixelData(fcsM.xDist.Count, 1, cvb.MatType.CV_32F, fcsM.yDist.ToArray))
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

        If task.heartBeat Then
            strOut = "CameraMotion estimate: " + vbCrLf + vbCrLf
            strOut += "Displacement in X: " + CStr(task.fpMotion.X) + vbCrLf
            strOut += "Displacement in Y: " + CStr(task.fpMotion.Y) + vbCrLf
        End If
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





Public Class FCS_MotionApplied : Inherits TaskParent
    Dim fcsMD As New FCS_MotionDirection
    Dim cDiff As New Diff_Color
    Public stableRect As cvb.Rect
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        task.gOptions.displayDst0.Checked = True
        task.gOptions.displayDst1.Checked = True
        desc = "Apply the results of FCS_MotionDirection to the RGB image and display the result"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static leftX As Single, rightX As Single
        Static topY As Single, botY As Single

        fcsMD.Run(src)
        If task.heartBeatLT Then
            dst0 = src.Clone
            leftX = 0
            rightX = dst2.Width
            topY = 0
            botY = dst2.Height
            stableRect = New cvb.Rect(0, 0, dst2.Width, dst2.Height)
        End If

        leftX = Math.Max(0, leftX - task.fpMotion.X)
        rightX = Math.Min(dst2.Width, rightX - task.fpMotion.X)

        topY = -Math.Max(0, topY + task.fpMotion.Y)
        botY = Math.Min(dst2.Height, botY + task.fpMotion.Y)

        topY = 0
        botY = dst2.Height ' working on this some other time...

        Dim newRect As cvb.Rect = ValidateRect(New cvb.Rect(leftX, topY, rightX - leftX, botY - topY))
        Dim oldRect As cvb.Rect = New cvb.Rect(0, 0, rightX - leftX, botY - topY)
        If leftX = 0 Then oldRect = New cvb.Rect(dst2.Width - rightX, 0, newRect.Width, botY)
        If topY = 0 Then oldRect = New cvb.Rect(oldRect.X, dst2.Height - botY, oldRect.Width, newRect.Height)

        'dst1.SetTo(0)
        'dst2.SetTo(0)
        'dst0(oldRect).CopyTo(dst1(newRect))
        'dst0(oldRect).CopyTo(dst2(newRect))

        'addw.src2 = src
        'addw.Run(dst1)
        'dst3 = addw.dst2

        'Dim mask = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
        'task.motionMask(stableRect).CopyTo(mask(stableRect))

        'src.CopyTo(dst0, mask)
        'src.CopyTo(dst2, mask)

        'If standalone Then
        '    cDiff.diff.lastFrame = dst0.Reshape(1, dst0.Rows * 3)
        '    cDiff.Run(dst2)
        '    dst3 = cDiff.dst2
        'End If
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

        fcs.featureInput = New List(Of cvb.Point2f)(knnMin.outputPoints2f)
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







Public Class FCS_MatchNeighbors : Inherits TaskParent
    Dim fcs As New FCS_MatchDepthColor
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        desc = "Track all the feature points and show their ID"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst0 = src.Clone
        fcs.Run(src)
        dst2 = fcs.dst2

        Dim fp = task.fpSelected
        dst3.SetTo(0)
        For Each index In fp.nabeList
            Dim fpNabe = task.fpList(index)
            DrawCircle(dst3, fpNabe.ptCenter, task.DotSize, task.HighlightColor)
            SetTrueText(CStr(fpNabe.age), fpNabe.ptCenter, 3)
        Next
        Static finfo As New FCS_Info
        finfo.Run(empty)
        SetTrueText(finfo.strOut, 3)
        labels(2) = fcs.labels(2)
        labels(3) = CStr(task.fpList.Count) + " cells found.  Dots below are at fp.ptCenter (not feature point)"
        drawFeaturePoints(dst0, fp.facets, cvb.Scalar.White)
    End Sub
End Class






Public Class FCS_MatchDepthColor : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Dim match As New Match_Basics
    Dim options As New Options_FCSMatch
    Public Sub New()
        desc = "Track each feature with FCS"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        fcs.Run(src)
        dst2 = fcs.dst2
        Dim fp1 = task.fpSelected
        dst2(fp1.rect).SetTo(cvb.Scalar.White, fp1.mask)

        Static fpLastList = New List(Of fpData)(task.fpList)
        Static fpLastIDs = New List(Of Single)(task.fpIDlist)
        Static fpLastMap = task.fpMap.Clone
        Static fpLastSrc = src.Clone
        Dim correlationCount As Integer, depthColorCount As Integer
        Dim noMatchCount As Integer, matchMap As Integer
        Dim depthIndex As Integer, colorIndex As Integer
        For i = 0 To task.fpList.Count - 1
            Dim fp = task.fpList(i)
            Dim indexLast = fpLastMap.Get(Of Integer)(fp.ptCenter.Y, fp.ptCenter.X)
            Dim fpLast = fpLastList(indexLast)
            Dim index = task.fpMap.Get(Of Integer)(fpLast.ptCenter.Y, fpLast.ptCenter.X)
            If index = fp.index Then
                fp = fpUpdate(fp, fpLast)
                matchMap += 1
            Else
                match.template = fpLastSrc(fpLast.rect)
                match.Run(src(fpLast.naberect))
                If match.correlation > options.MinCorrelation Then
                    fp = fpUpdate(fp, fpLast)
                    correlationCount += 1
                Else
                    Dim distances As New List(Of Single)
                    For j = 0 To fp.nabeList.Count - 1
                        distances.Add(Math.Abs(task.fpList(j).depthMean - fpLast.depthMean))
                    Next
                    depthIndex = fp.nabeList(distances.IndexOf(distances.Min))
                    Dim colorDistance As New List(Of Single)
                    For j = 0 To fp.nabeList.Count - 1
                        colorDistance.Add(distance3D(task.fpList(j).colorMean, fpLast.colorMean))
                    Next
                    colorIndex = colorDistance.IndexOf(colorDistance.Min)
                    If colorIndex = depthIndex Then
                        fp = fpUpdate(fp, fpLastList(colorIndex))
                        depthColorCount += 1
                    Else
                        fp.indexLast = -1
                        fp.age = 1
                        noMatchCount += 1
                    End If
                End If
            End If
            task.fpList(i) = fp
        Next

        fpLastList = New List(Of fpData)(task.fpList)
        fpLastIDs = New List(Of Single)(task.fpIDlist)
        fpLastMap = task.fpMap.Clone
        fpLastSrc = src.Clone
        labels(2) = fcs.labels(2) + " Matched with Map/Correlation/Neighbor/Unmatched: " +
                    CStr(matchMap) + "/" + CStr(correlationCount) + "/" +
                    CStr(depthColorCount) + "/" + CStr(noMatchCount)
    End Sub
End Class







Public Class FCS_Edges : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Dim edges As New Edge_Canny
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use edges to connect feature points to their neighbors."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        fcs.Run(src)
        dst2 = src

        edges.Run(src)
        dst3 = edges.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        dst1.SetTo(0)
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






Public Class FCS_MatchEdges : Inherits TaskParent
    Dim fcs As New FCS_Basics
    Dim edges As New Edge_Canny
    Dim match As New Match_Basics
    Dim options As New Options_FCSMatch
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        labels(3) = "The age of each feature point cell."
        desc = "Try to improve the match count to the previous frame using correlation"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        edges.Run(src)
        dst1 = edges.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        fcs.Run(src)
        dst2 = fcs.dst2
        labels(2) = fcs.labels(2)

        Static fpLastMap = task.fpMap.Clone
        Static fpLastEdges = dst1.Clone
        Static fpLastSrc = src.Clone

        Dim matchEdges As Integer, matchSrc As Integer
        For i = 0 To task.fpList.Count - 1
            Dim fp As fpData = task.fpList(i)
            If fp.indexLast < 0 Then
                Dim indexLast = task.fpMapLast.Get(Of Integer)(fp.ptCenter.Y, fp.ptCenter.X)
                Dim fpLast = task.fpListLast(indexLast)
                match.template = fpLastEdges(fpLast.rect)
                match.Run(dst1(fpLast.nabeRect))
                If match.correlation > options.MinCorrelation Then
                    fp = fpUpdate(fp, fpLast)
                    matchEdges += 1
                Else
                    match.template = fpLastSrc(fpLast.rect)
                    match.Run(src(fpLast.nabeRect))
                    If match.correlation > options.MinCorrelation Then
                        fp = fpUpdate(fp, fpLast)
                        matchSrc += 1
                    Else
                        fp.indexLast = -1
                        fp.age = 1
                    End If
                End If
            End If
            task.fpList(i) = fp
        Next

        fpLastMap = task.fpMap.Clone
        fpLastEdges = dst1.Clone
        fpLastEdges = src.Clone
        labels(2) = CStr(matchEdges) + " cells were edge matched.  " + CStr(matchSrc) + " cells match with src"

        dst3.SetTo(0)
        For Each fp In task.fpList
            DrawCircle(dst1, fp.ptCenter, task.DotSize, task.HighlightColor)
            DrawCircle(dst3, fp.ptCenter, task.DotSize, task.HighlightColor)
            SetTrueText(CStr(fp.age), fp.ptCenter, 3)
        Next
    End Sub
End Class