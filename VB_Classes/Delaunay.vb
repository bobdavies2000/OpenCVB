Imports cv = OpenCvSharp
Public Class Delaunay_Basics : Inherits VB_Algorithm
    Public inputPoints As New List(Of cv.Point2f)
    Public facetList As New List(Of List(Of cv.Point))
    Public facet32s As cv.Mat
    Dim random As New Random_Enumerable
    Dim subdiv As New cv.Subdiv2D
    Public Sub New()
        facet32s = New cv.Mat(dst2.Size, cv.MatType.CV_32SC1, 0)
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If heartBeat() And standalone Then
            Random.Run(Nothing)
            inputPoints = New List(Of cv.Point2f)(random.points)
            dst3 = random.dst2
        End If

        subdiv.InitDelaunay(New cv.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(inputPoints)

        Dim facets = New cv.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        Dim usedColors As New List(Of cv.Vec3b)
        facetList.Clear()
        Static lastColor = New cv.Mat(dst2.Size, cv.MatType.CV_8UC3, 0)
        For i = 0 To facets.Length - 1
            Dim nextFacet As New List(Of cv.Point)
            For j = 0 To facets(i).Length - 1
                nextFacet.Add(New cv.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            Dim pt = inputPoints(i)
            Dim nextColor = lastColor.Get(Of cv.Vec3b)(pt.Y, pt.X)
            If usedColors.Contains(nextColor) Then nextColor = randomCellColor()
            usedColors.Add(nextColor)

            dst2.FillConvexPoly(nextFacet, vecToScalar(nextColor))
            facet32s.FillConvexPoly(nextFacet, i, task.lineType)
            facetList.Add(nextFacet)
        Next
        facet32s.ConvertTo(dst1, cv.MatType.CV_8U)

        lastColor = dst2.Clone
        labels(2) = traceName + ": " + Format(inputPoints.Count, "000") + " cells were present."
    End Sub
End Class







' https://github.com/npinto/opencv/blob/master/samples/c/delaunay.c
Public Class Delaunay_SubDiv : Inherits VB_Algorithm
    Dim random As New Random_Basics
    Public Sub New()
        random.options.countSlider.Value = 100
        desc = "Use Delaunay to subdivide an image into triangles."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then If heartBeat() = False Then Exit Sub
        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, dst2.Width, dst2.Height))
        random.Run(Nothing)
        dst2.SetTo(0)
        For Each pt In random.pointList
            subdiv.Insert(pt)
            Dim edgeList = subdiv.GetEdgeList()
            For i = 0 To edgeList.Length - 1
                Dim e = edgeList(i)
                Dim p0 = New cv.Point(Math.Round(e(0)), Math.Round(e(1)))
                Dim p1 = New cv.Point(Math.Round(e(2)), Math.Round(e(3)))
                dst2.Line(p0, p1, cv.Scalar.White, task.lineWidth, task.lineType)
            Next
        Next

        For Each pt In random.PointList
            dst2.Circle(pt, task.dotSize + 1, cv.Scalar.Red, -1, task.lineType)
        Next

        Dim facets = New cv.Point2f()() {Nothing}
        Dim centers() As cv.Point2f
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, centers)

        Dim ifacet() As cv.Point
        Dim ifacets = New cv.Point()() {Nothing}

        For i = 0 To facets.Length - 1
            ReDim ifacet(facets(i).Length - 1)
            For j = 0 To facets(i).Length - 1
                ifacet(j) = New cv.Point(Math.Round(facets(i)(j).X), Math.Round(facets(i)(j).Y))
            Next
            ifacets(0) = ifacet
            dst3.FillConvexPoly(ifacet, task.scalarColors(i Mod task.scalarColors.Length), task.lineType)
            cv.Cv2.Polylines(dst3, ifacets, True, cv.Scalar.Black, task.lineWidth, task.lineType, 0)
        Next
    End Sub
End Class







' https://github.com/shimat/opencvsharp/wiki/Subdiv2D
Public Class Delaunay_Subdiv2D : Inherits VB_Algorithm
    Public Sub New()
        labels(3) = "Voronoi facets for the same subdiv2D"
        desc = "Generate random points and divide the image around those points."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If heartBeat() = False Then Exit Sub ' too fast otherwise...
        dst2.SetTo(0)
        Dim points = Enumerable.Range(0, 100).Select(Of cv.Point2f)(
            Function(i)
                Return New cv.Point2f(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height))
            End Function).ToArray()

        For Each p In points
            dst2.Circle(p, task.dotSize + 1, cv.Scalar.Red, -1, task.lineType)
        Next
        dst3 = dst2.Clone()

        Dim subdiv = New cv.Subdiv2D(New cv.Rect(0, 0, dst3.Width, dst3.Height))
        subdiv.Insert(points)

        ' draw voronoi diagram
        Dim facetList()() As cv.Point2f
        Dim facetCenters() As cv.Point2f
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
            dst2.Line(p1, p2, cv.Scalar.Green, 1)
        Next
    End Sub
End Class










Public Class Delaunay_GenerationsNoKNN : Inherits VB_Algorithm
    Public inputPoints As New List(Of cv.Point2f)
    Public facet As New Delaunay_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32S, 0)
        labels = {"", "Mask of unmatched regions - generation set to 0", "Facet Image with index of each region", "Generation counts for each region."}
        desc = "Create a region in an image for each point provided with KNN."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standalone And heartBeat() Then
            Static random As New Random_Basics
            If firstPass Then random.options.countSlider.Value = 10
            random.Run(Nothing)
            inputPoints = New List(Of cv.Point2f)(random.pointList)
        End If

        facet.inputPoints = New List(Of cv.Point2f)(inputPoints)
        facet.Run(src)
        dst2 = facet.dst2

        Dim generationMap = dst3.Clone
        dst3.SetTo(0)
        Dim usedG As New List(Of Integer), g As Integer
        For Each pt In inputPoints
            Dim index = facet.facet32s.Get(Of Integer)(pt.Y, pt.X)
            If index >= facet.facetList.Count Then Continue For
            Dim nextFacet = facet.facetList(index)
            ' insure that each facet has a unique generation number
            If firstPass Then
                g = usedG.Count
            Else
                g = generationMap.Get(Of Integer)(pt.Y, pt.X) + 1
                While usedG.Contains(g)
                    g += 1
                End While
            End If
            dst3.FillConvexPoly(nextFacet, g, task.lineType)
            usedG.Add(g)
            setTrueText(CStr(g), pt, 2)
        Next
        generationMap = dst3.Clone
    End Sub
End Class









Public Class Delaunay_Generations : Inherits VB_Algorithm
    Public inputPoints As New List(Of cv.Point2f)
    Public facet As New Delaunay_Basics
    Dim knn As New KNN_Lossy
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32S, 0)
        labels = {"", "Mask of unmatched regions - generation set to 0", "Facet Image with count for each region",
                  "Generation counts in CV_32SC1 format"}
        desc = "Create a region in an image for each point provided"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standalone Then
            Static random As New Random_Basics
            If firstPass Then random.options.countSlider.Value = 10
            If heartBeat() Then random.Run(Nothing)
            inputPoints = New List(Of cv.Point2f)(random.PointList)
        End If

        knn.queries = New List(Of cv.Point2f)(inputPoints)
        knn.Run(Nothing)

        facet.inputPoints = New List(Of cv.Point2f)(inputPoints)
        facet.Run(src)
        dst2 = facet.dst2

        Dim generationMap = dst0.Clone
        dst0.SetTo(0)
        Dim usedG As New List(Of Integer), g As Integer
        For Each mp In knn.matches
            Dim index = facet.facet32s.Get(Of Integer)(mp.p2.Y, mp.p2.X)
            If index >= facet.facetList.Count Then Continue For
            Dim nextFacet = facet.facetList(index)
            ' insure that each facet has a unique generation number
            If firstPass Then
                g = usedG.Count
            Else
                g = generationMap.Get(Of Integer)(mp.p2.Y, mp.p2.X) + 1
                While usedG.Contains(g)
                    g += 1
                End While
            End If
            dst0.FillConvexPoly(nextFacet, g, task.lineType)
            usedG.Add(g)
            setTrueText(CStr(g), mp.p2, 2)
        Next
    End Sub
End Class