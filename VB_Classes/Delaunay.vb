Imports cvb = OpenCvSharp
Public Class Delaunay_Basics : Inherits VB_Parent
    Public inputPoints As New List(Of cvb.Point2f)
    Public facetList As New List(Of List(Of cvb.Point))
    Public facet32s As cvb.Mat
    Dim randEnum As New Random_Enumerable
    Dim subdiv As New cvb.Subdiv2D
    Public Sub New()
        facet32s = New cvb.Mat(dst2.Size(), cvb.MatType.CV_32SC1, 0)
        labels(3) = "CV_8U map of Delaunay cells"
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If task.heartBeat And standalone Then
            randEnum.Run(empty)
            inputPoints = New List(Of cvb.Point2f)(randEnum.points)
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
        facet32s.ConvertTo(dst3, cvb.MatType.CV_8U)
        dst2 = ShowPalette(dst3)
        labels(2) = traceName + ": " + Format(inputPoints.Count, "000") + " cells were present."
    End Sub
End Class







' https://github.com/npinto/opencv/blob/master/samples/c/delaunay.c
Public Class Delaunay_SubDiv : Inherits VB_Parent
    Dim random As New Random_Basics
    Public Sub New()
        FindSlider("Random Pixel Count").Value = 100
        desc = "Use Delaunay to subdivide an image into triangles."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If standaloneTest() Then If not task.heartBeat Then Exit Sub
        Dim subdiv As New cvb.Subdiv2D(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        random.Run(empty)
        dst2.SetTo(0)
        For Each pt In random.PointList
            subdiv.Insert(pt)
            Dim edgeList = subdiv.GetEdgeList()
            For i = 0 To edgeList.Length - 1
                Dim e = edgeList(i)
                Dim p0 = New cvb.Point(Math.Round(e(0)), Math.Round(e(1)))
                Dim p1 = New cvb.Point(Math.Round(e(2)), Math.Round(e(3)))
                DrawLine(dst2, p0, p1, cvb.Scalar.White)
            Next
        Next

        For Each pt In random.PointList
            DrawCircle(dst2,pt, task.DotSize + 1, cvb.Scalar.Red)
        Next

        Dim facets = New cvb.Point2f()() {Nothing}
        Dim centers() As cvb.Point2f
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, centers)

        Dim ifacet() As cvb.Point
        Dim ifacets = New cvb.Point()() {Nothing}

        For i = 0 To facets.Length - 1
            ReDim ifacet(facets(i).Length - 1)
            For j = 0 To facets(i).Length - 1
                ifacet(j) = New cvb.Point(Math.Round(facets(i)(j).X), Math.Round(facets(i)(j).Y))
            Next
            ifacets(0) = ifacet
            dst3.FillConvexPoly(ifacet, task.scalarColors(i Mod task.scalarColors.Length), task.lineType)
            cvb.Cv2.Polylines(dst3, ifacets, True, cvb.Scalar.Black, task.lineWidth, task.lineType, 0)
        Next
    End Sub
End Class







' https://github.com/shimat/opencvsharp/wiki/Subdiv2D
Public Class Delaunay_Subdiv2D : Inherits VB_Parent
    Public Sub New()
        labels(3) = "Voronoi facets for the same subdiv2D"
        desc = "Generate random points and divide the image around those points."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If not task.heartBeat Then Exit Sub ' too fast otherwise...
        dst2.SetTo(0)
        Dim points = Enumerable.Range(0, 100).Select(Of cvb.Point2f)(
            Function(i)
                Return New cvb.Point2f(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height))
            End Function).ToArray()

        For Each p In points
            DrawCircle(dst2,p, task.DotSize + 1, cvb.Scalar.Red)
        Next
        dst3 = dst2.Clone()

        Dim subdiv = New cvb.Subdiv2D(New cvb.Rect(0, 0, dst3.Width, dst3.Height))
        subdiv.Insert(points)

        ' draw voronoi diagram
        Dim facetList()() As cvb.Point2f
        Dim facetCenters() As cvb.Point2f
        subdiv.GetVoronoiFacetList(Nothing, facetList, facetCenters)

        For Each list In facetList
            Dim before = list.Last()
            For Each p In list
                dst3.Line(before, p, cvb.Scalar.Green, 1)
                before = p
            Next
        Next

        Dim edgelist = subdiv.GetEdgeList()
        For Each edge In edgelist
            Dim p1 = New cvb.Point2f(edge(0), edge(1))
            Dim p2 = New cvb.Point2f(edge(2), edge(3))
            DrawLine(dst2, p1, p2, cvb.Scalar.Green)
        Next
    End Sub
End Class










Public Class Delaunay_GenerationsNoKNN : Inherits VB_Parent
    Public inputPoints As New List(Of cvb.Point2f)
    Public facet As New Delaunay_Basics
    Dim random As New Random_Basics
    Public Sub New()
        FindSlider("Random Pixel Count").Value = 10
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_32S, 0)
        labels = {"", "Mask of unmatched regions - generation set to 0", "Facet Image with index of each region", "Generation counts for each region."}
        desc = "Create a region in an image for each point provided without using KNN."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If standaloneTest() And task.heartBeat Then
            random.Run(empty)
            inputPoints = New List(Of cvb.Point2f)(random.PointList)
        End If

        facet.inputPoints = New List(Of cvb.Point2f)(inputPoints)
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
            If task.FirstPass Then
                g = usedG.Count
            Else
                g = generationMap.Get(Of Integer)(pt.Y, pt.X) + 1
                While usedG.Contains(g)
                    g += 1
                End While
            End If
            dst3.FillConvexPoly(nextFacet, g, task.lineType)
            usedG.Add(g)
            SetTrueText(CStr(g), pt, 2)
        Next
        generationMap = dst3.Clone
    End Sub
End Class









Public Class Delaunay_Generations : Inherits VB_Parent
    Public inputPoints As New List(Of cvb.Point2f)
    Public facet As New Delaunay_Basics
    Dim knn As New KNN_Basics
    Dim random As New Random_Basics
    Public Sub New()
        dst0 = New cvb.Mat(dst0.Size(), cvb.MatType.CV_32S, 0)
        labels = {"", "Mask of unmatched regions - generation set to 0", "Facet Image with count for each region",
                  "Generation counts in CV_32SC1 format"}
        FindSlider("Random Pixel Count").Value = 10
        desc = "Create a region in an image for each point provided"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If standaloneTest() Then
            If task.heartBeat Then Random.Run(empty)
            inputPoints = New List(Of cvb.Point2f)(random.PointList)
        End If

        knn.queries = New List(Of cvb.Point2f)(inputPoints)
        knn.Run(empty)

        facet.inputPoints = New List(Of cvb.Point2f)(inputPoints)
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
            If task.FirstPass Then
                g = usedG.Count
            Else
                g = generationMap.Get(Of Integer)(mp.p2.Y, mp.p2.X) + 1
                While usedG.Contains(g)
                    g += 1
                End While
            End If
            dst0.FillConvexPoly(nextFacet, g, task.lineType)
            usedG.Add(g)
            SetTrueText(CStr(g), mp.p2, 2)
        Next
    End Sub
End Class





Public Class Delaunay_ConsistentColor : Inherits VB_Parent
    Public inputPoints As New List(Of cvb.Point2f)
    Public facetList As New List(Of List(Of cvb.Point))
    Public facet32s As cvb.Mat
    Dim randEnum As New Random_Enumerable
    Dim subdiv As New cvb.Subdiv2D
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        facet32s = New cvb.Mat(dst2.Size(), cvb.MatType.CV_32SC1, 0)
        UpdateAdvice(traceName + ": use local options to control the number of points")
        labels(1) = "Input points to subdiv"
        labels(3) = "Inconsistent colors in dst2 are duplicate randomCellColor output."
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If task.heartBeat And standalone Then
            randEnum.Run(empty)
            inputPoints = New List(Of cvb.Point2f)(randEnum.points)
        End If

        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(inputPoints)

        Dim facets = New cvb.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        Dim usedColors As New List(Of cvb.Vec3b)
        facetList.Clear()
        Static lastColor = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8UC3, cvb.Scalar.All(0))
        For i = 0 To facets.Length - 1
            Dim nextFacet As New List(Of cvb.Point)
            For j = 0 To facets(i).Length - 1
                nextFacet.Add(New cvb.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            Dim pt = inputPoints(i)
            Dim nextColor = lastColor.Get(Of cvb.Vec3b)(pt.Y, pt.X)
            If usedColors.Contains(nextColor) Then nextColor = randomCellColor()
            usedColors.Add(nextColor)

            dst2.FillConvexPoly(nextFacet, vecToScalar(nextColor))
            facet32s.FillConvexPoly(nextFacet, i, task.lineType)
            facetList.Add(nextFacet)
        Next

        dst1.SetTo(0)
        For Each pt In inputPoints
            dst1.Circle(New cvb.Point(pt.X, pt.Y), task.DotSize, task.HighlightColor, -1, task.lineType)
        Next
        lastColor = dst2.Clone
        labels(2) = traceName + ": " + Format(inputPoints.Count, "000") + " cells were present."
    End Sub
End Class




Public Class Delaunay_Contours : Inherits VB_Parent
    Public inputPoints As New List(Of cvb.Point2f)
    Dim randEnum As New Random_Enumerable
    Dim subdiv As New cvb.Subdiv2D
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels(3) = "CV_8U map of Delaunay cells"
        desc = "Subdivide an image based on the points provided."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If task.heartBeat And standalone Then
            randEnum.Run(empty)
            inputPoints = New List(Of cvb.Point2f)(randEnum.points)
        End If

        subdiv.InitDelaunay(New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(inputPoints)

        Dim facets = New cvb.Point2f()() {Nothing}
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, Nothing)

        dst2.SetTo(0)
        For i = 0 To facets.Length - 1
            Dim ptList As New List(Of cvb.Point)
            For j = 0 To facets(i).Length - 1
                ptList.Add(New cvb.Point(facets(i)(j).X, facets(i)(j).Y))
            Next

            DrawContour(dst2, ptList, 255, 1)
        Next
        labels(2) = traceName + ": " + Format(inputPoints.Count, "000") + " cells were present."
    End Sub
End Class
