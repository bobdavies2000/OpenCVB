Imports cv = OpenCvSharp
Public Class Delaunay_Basics : Inherits TaskParent
    Public inputPoints As New List(Of cv.Point2f)
    Public facetList As New List(Of List(Of cv.Point))
    Dim subdiv As New cv.Subdiv2D
    Public Sub New()
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_32SC1, 0)
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.heartBeat And standalone Then
            Static random As New Random_Basics
            random.Run(src)
            inputPoints = New List(Of cv.Point2f)(random.PointList)
        End If

        subdiv.InitDelaunay(New cv.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(inputPoints)

        Dim facets = New cv.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        facetList.Clear()
        For i = 0 To facets.Length - 1
            Dim nextFacet As New List(Of cv.Point)
            For j = 0 To facets(i).Length - 1
                nextFacet.Add(New cv.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            dst3.FillConvexPoly(nextFacet, i, cv.LineTypes.Link4)
            facetList.Add(nextFacet)
        Next

        dst3.ConvertTo(dst1, cv.MatType.CV_8U)
        dst2 = ShowPalette(dst1)

        labels(2) = traceName + ": " + Format(inputPoints.Count, "000") + " cells were present."
    End Sub
End Class





Public Class Delaunay_Contours : Inherits TaskParent
    Dim subdiv As New cv.Subdiv2D
    Public ptBest As New BrickPoint_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(3) = "CV_8U map of Delaunay cells"
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then ptBest.Run(src)
        subdiv.InitDelaunay(New cv.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(ptBest.features)

        Dim facets = New cv.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        dst2.SetTo(0)
        For i = 0 To facets.Length - 1
            Dim ptList As New List(Of cv.Point)
            For j = 0 To facets(i).Length - 1
                ptList.Add(New cv.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            DrawContour(dst2, ptList, 255, 1)
        Next
        labels(2) = traceName + ": " + Format(ptBest.features.Count, "000") + " cells were present."
    End Sub
End Class






' https://github.com/npinto/opencv/blob/master/samples/c/delaunay.c
Public Class Delaunay_SubDiv : Inherits TaskParent
    Dim random As New Random_Basics
    Public Sub New()
       OptionParent.FindSlider("Random Pixel Count").Value = 100
        desc = "Use Delaunay to subdivide an image into triangles."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standaloneTest() Then If Not task.heartBeat Then Exit Sub
        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, dst2.Width, dst2.Height))
        random.Run(src)
        dst2.SetTo(0)
        For Each pt In random.PointList
            subdiv.Insert(pt)
            Dim edgeList = subdiv.GetEdgeList()
            For i = 0 To edgeList.Length - 1
                Dim e = edgeList(i)
                Dim p0 = New cv.Point(Math.Round(e(0)), Math.Round(e(1)))
                Dim p1 = New cv.Point(Math.Round(e(2)), Math.Round(e(3)))
                DrawLine(dst2, p0, p1, white)
            Next
        Next

        For Each pt In random.PointList
            DrawCircle(dst2, pt, task.DotSize + 1, cv.Scalar.Red)
        Next

        Dim facets = New cv.Point2f()() {Nothing}
        Dim centers() As cv.Point2f = Nothing
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, centers)

        Dim ifacet() As cv.Point
        Dim ifacets = New cv.Point()() {Nothing}

        For i = 0 To facets.Length - 1
            ReDim ifacet(facets(i).Length - 1)
            For j = 0 To facets(i).Length - 1
                ifacet(j) = New cv.Point(Math.Round(facets(i)(j).X), Math.Round(facets(i)(j).Y))
            Next
            ifacets(0) = ifacet
            dst3.FillConvexPoly(ifacet, task.scalarColors(i Mod task.scalarColors.Length), cv.LineTypes.Link4)
            cv.Cv2.Polylines(dst3, ifacets, True, cv.Scalar.Black, task.lineWidth, cv.LineTypes.Link4, 0)
        Next
    End Sub
End Class







' https://github.com/shimat/opencvsharp/wiki/Subdiv2D
Public Class Delaunay_Subdiv2D : Inherits TaskParent
    Public Sub New()
        labels(3) = "Voronoi facets for the same subdiv2D"
        desc = "Generate random points and divide the image around those points."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If Not task.heartBeat Then Exit Sub ' too fast otherwise...
        dst2.SetTo(0)
        Dim points = Enumerable.Range(0, 100).Select(Of cv.Point2f)(
            Function(i)
                Return New cv.Point2f(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height))
            End Function).ToArray()

        For Each p In points
            DrawCircle(dst2,p, task.DotSize + 1, cv.Scalar.Red)
        Next
        dst3 = dst2.Clone()

        Dim subdiv = New cv.Subdiv2D(New cv.Rect(0, 0, dst3.Width, dst3.Height))
        subdiv.Insert(points)

        ' draw voronoi diagram
        Dim facetList()() As cv.Point2f = Nothing
        Dim facetCenters() As cv.Point2f = Nothing
        subdiv.GetVoronoiFacetList(Nothing, facetList, facetCenters)

        For Each list In facetList
            Dim before = list.Last()
            For Each p In list
                dst3.Line(before, p, cv.Scalar.Green, 1)
                before = p
            Next
        Next

        Dim edgelist = subdiv.GetEdgeList()
        For Each edge In edgelist
            Dim p1 = New cv.Point2f(edge(0), edge(1))
            Dim p2 = New cv.Point2f(edge(2), edge(3))
            DrawLine(dst2, p1, p2, cv.Scalar.Green)
        Next
    End Sub
End Class










Public Class Delaunay_GenerationsNoKNN : Inherits TaskParent
    Public inputPoints As New List(Of cv.Point2f)
    Public facet As New Delaunay_Basics
    Dim random As New Random_Basics
    Public Sub New()
       OptionParent.FindSlider("Random Pixel Count").Value = 10
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_32S, 0)
        labels = {"", "Mask of unmatched regions - generation set to 0", "Facet Image with index of each region", "Generation counts for each region."}
        desc = "Create a region in an image for each point provided without using KNN."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standaloneTest() And task.heartBeat Then
            random.Run(src)
            inputPoints = New List(Of cv.Point2f)(random.PointList)
        End If

        facet.inputPoints = New List(Of cv.Point2f)(inputPoints)
        facet.Run(src)
        dst2 = facet.dst2

        Dim generationMap = dst3.Clone
        dst3.SetTo(0)
        Dim usedG As New List(Of Integer), g As Integer
        For Each pt In inputPoints
            Dim index = facet.dst3.Get(Of Integer)(pt.Y, pt.X)
            If index >= facet.facetList.Count Then Continue For
            Dim nextFacet = facet.facetList(index)
            ' insure that each facet has a unique generation number
            If task.firstPass Then
                g = usedG.Count
            Else
                g = generationMap.Get(Of Integer)(pt.Y, pt.X) + 1
                While usedG.Contains(g)
                    g += 1
                End While
            End If
            dst3.FillConvexPoly(nextFacet, g, cv.LineTypes.Link4)
            usedG.Add(g)
            SetTrueText(CStr(g), pt, 2)
        Next
        generationMap = dst3.Clone
    End Sub
End Class









Public Class Delaunay_Generations : Inherits TaskParent
    Public inputPoints As New List(Of cv.Point2f)
    Public facet As New Delaunay_Basics
    Dim knn As New KNN_OneToOne
    Dim random As New Random_Basics
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_32S, 0)
        labels = {"", "Mask of unmatched regions - generation set to 0", "Facet Image with count for each region",
                  "Generation counts in CV_32SC1 format"}
       OptionParent.FindSlider("Random Pixel Count").Value = 10
        desc = "Create a region in an image for each point provided"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            If task.heartBeatLT Then random.Run(src)
            inputPoints = New List(Of cv.Point2f)(random.PointList)
        End If

        knn.queries = New List(Of cv.Point2f)(inputPoints)
        knn.Run(src)

        facet.inputPoints = New List(Of cv.Point2f)(inputPoints)
        facet.Run(src)
        dst2 = facet.dst2

        Dim generationMap = dst0.Clone
        dst0.SetTo(0)
        Dim usedG As New List(Of Integer), g As Integer
        For Each lp In knn.matches
            Dim index = facet.dst3.Get(Of Byte)(lp.p2.Y, lp.p2.X)
            If index >= facet.facetList.Count Then Continue For
            Dim nextFacet = facet.facetList(index)
            ' insure that each facet has a unique generation number
            If task.firstPass Then
                g = usedG.Count
            Else
                g = generationMap.Get(Of Integer)(lp.p2.Y, lp.p2.X) + 1
                While usedG.Contains(g)
                    g += 1
                End While
            End If
            dst0.FillConvexPoly(nextFacet, g, cv.LineTypes.Link4)
            usedG.Add(g)
            SetTrueText(CStr(g), lp.p2, 2)
        Next
    End Sub
End Class





Public Class Delaunay_ConsistentColor : Inherits TaskParent
    Public inputPoints As New List(Of cv.Point2f)
    Public facetList As New List(Of List(Of cv.Point))
    Public facet32s As cv.Mat
    Dim randEnum As New Random_Enumerable
    Dim subdiv As New cv.Subdiv2D
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        facet32s = New cv.Mat(dst2.Size(), cv.MatType.CV_32SC1, 0)
        labels(1) = "Input points to subdiv"
        labels(3) = "Inconsistent colors in dst2 are duplicate randomCellColor output."
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.heartBeat And standalone Then
            randEnum.Run(src)
            inputPoints = New List(Of cv.Point2f)(randEnum.points)
        End If

        subdiv.InitDelaunay(New cv.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(inputPoints)

        Dim facets = New cv.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        Dim usedColors As New List(Of cv.Scalar)
        facetList.Clear()
        Static lastColor = New cv.Mat(dst2.Size(), cv.MatType.CV_8UC3, cv.Scalar.All(0))
        For i = 0 To facets.Length - 1
            Dim nextFacet As New List(Of cv.Point)
            For j = 0 To facets(i).Length - 1
                nextFacet.Add(New cv.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            Dim pt = inputPoints(i)
            Dim vec As cv.Vec3b = lastColor.Get(Of cv.Vec3b)(pt.Y, pt.X)
            Dim nextColor As cv.Scalar = vec.ToVec3d
            If usedColors.Contains(nextColor) Then nextColor = randomCellColor()
            usedColors.Add(nextColor)

            dst2.FillConvexPoly(nextFacet, nextColor)
            facet32s.FillConvexPoly(nextFacet, i, cv.LineTypes.Link4)
            facetList.Add(nextFacet)
        Next

        dst1.SetTo(0)
        For Each pt In inputPoints
            dst1.Circle(New cv.Point(pt.X, pt.Y), task.DotSize, task.highlight, -1, cv.LineTypes.Link4)
        Next
        lastColor = dst2.Clone
        labels(2) = traceName + ": " + Format(inputPoints.Count, "000") + " cells were present."
    End Sub
End Class

