Imports cv = OpenCvSharp

Module Delaunay_Exports
    Public Sub draw_line(img As cv.Mat, org As cv.Point, output As cv.Point, active_color As cv.Scalar)
        If org.X >= 0 And org.X <= img.Width Then
            If org.Y >= 0 And org.Y <= img.Height Then
                If output.X >= 0 And output.X <= img.Width Then
                    If output.Y >= 0 And output.Y <= img.Height Then
                        cv.Cv2.Line(img, org, output, active_color, 1, task.lineType, 0)
                    End If
                End If
            End If
        End If
    End Sub

    Public Sub draw_subdiv(img As cv.Mat, subdiv As cv.Subdiv2D, delaunay_color As cv.Scalar, testval As integer)
        If testval Then
            Dim trianglelist() = subdiv.GetTriangleList()
            Dim pt(3) As cv.Point

            For i = 0 To trianglelist.Length - 1
                Dim t = trianglelist(i)
                pt(0) = New cv.Point(Math.Round(t(0)), Math.Round(t(1)))
                pt(1) = New cv.Point(Math.Round(t(2)), Math.Round(t(3)))
                pt(2) = New cv.Point(Math.Round(t(4)), Math.Round(t(5)))
                draw_line(img, pt(0), pt(1), delaunay_color)
                draw_line(img, pt(1), pt(2), delaunay_color)
                draw_line(img, pt(2), pt(0), delaunay_color)
            Next
        Else
            Dim edgeList = subdiv.GetEdgeList()
            For i = 0 To edgeList.Length - 1
                Dim e = edgeList(i)
                Dim pt0 = New cv.Point(Math.Round(e(0)), Math.Round(e(1)))
                Dim pt1 = New cv.Point(Math.Round(e(2)), Math.Round(e(3)))
                draw_line(img, pt0, pt1, delaunay_color)
            Next
        End If
    End Sub
    Public Sub locate_point(img As cv.Mat, subdiv As cv.Subdiv2D, fp As cv.Point2f, active_color As cv.Scalar)
        Dim e0 As integer, vector As integer

        subdiv.Locate(fp, e0, vector)
        If e0 > 0 Then
            Dim e = e0
            Do
                Dim org As cv.Point2f, dstpt As cv.Point2f
                If subdiv.EdgeOrg(e, org) > 0 And subdiv.EdgeDst(e, dstpt) > 0 Then
                    'draw_line(img, org, dst1, active_color)
                End If

                e = subdiv.GetEdge(e, 19) ' next_around_left const missing ?
                If e = e0 Then Exit Do
            Loop
        End If

        cv.Cv2.Circle(img, fp, 10, active_color, -1, task.lineType, 0)
    End Sub
    Public Sub paint_voronoi(colors() As cv.Scalar, img As cv.Mat, subdiv As cv.Subdiv2D)
        Dim facets = New cv.Point2f()() {Nothing}
        Dim centers() As cv.Point2f = Nothing
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, centers)

        Dim ifacet() As cv.Point = Nothing
        Dim ifacets = New cv.Point()() {Nothing}

        For i = 0 To facets.Length - 1
            ReDim ifacet(facets(i).Length - 1)
            For j = 0 To facets(i).Length - 1
                ifacet(j) = New cv.Point(Math.Round(facets(i)(j).X), Math.Round(facets(i)(j).Y))
            Next
            Dim nextColor = colors(i Mod colors.Length)
            ifacets(0) = ifacet
            cv.Cv2.FillConvexPoly(img, ifacet, nextColor, task.lineType)
            cv.Cv2.Polylines(img, ifacets, True, New cv.Scalar(0), 1, task.lineType, 0)
        Next
    End Sub
End Module




Public Class Delaunay_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Use Delaunay to subdivide an image into triangles."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim active_facet_color = New cv.Scalar(0, 0, 255)
        Dim rect = New cv.Rect(0, 0, src.Width, src.Height)

        Dim subdiv As New cv.Subdiv2D(rect)

        For i = 0 To 100
            Dim fp = New cv.Point2f(msRNG.Next(0, rect.Width), msRNG.Next(0, rect.Height))
            locate_point(dst1, subdiv, fp, active_facet_color)
            subdiv.Insert(fp)
            draw_subdiv(dst1, subdiv, cv.Scalar.White, task.frameCount Mod 2)
        Next

        paint_voronoi(task.scalarColors, dst1, subdiv)
    End Sub
End Class




Public Class Delaunay_GoodFeatures
    Inherits VBparent
    Dim features As Features_GoodFeatures
    Public Sub New()
        initParent()
        features = New Features_GoodFeatures()
        label2 = "Voronoi facets of delauney good features"
        task.desc = "Use Delaunay with the points provided by GoodFeaturesToTrack."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        features.Run(src)

        dst1 = src
        Dim active_facet_color = New cv.Scalar(0, 0, 255)
        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, src.Width, src.Height))
        For i = 0 To features.goodFeatures.Count - 1
            locate_point(dst1, subdiv, features.goodFeatures(i), active_facet_color)
            subdiv.Insert(features.goodFeatures(i))
        Next

        paint_voronoi(task.scalarColors, dst2, subdiv)
        Static lastFrame As cv.Mat = dst2
        For i = 0 To features.goodFeatures.Count - 1
            Dim pt = features.goodFeatures(i)
            Dim color = lastFrame.Get(Of cv.Vec3b)(pt.Y, pt.X)
            If color.Item0 < 100 Then color = task.vecColors(i)
            dst2.FloodFill(pt, color)
            dst1.FloodFill(pt, color)
        Next
        lastFrame = dst2.Clone
    End Sub
End Class




' https://github.com/shimat/opencvsharp/wiki/Subdiv2D
Public Class Delauney_Subdiv2D
    Inherits VBparent
    Public updateFrequency As Integer = 30
    Public Sub New()
        initParent()
        label2 = "Voronoi facets for the same subdiv2D"
        task.desc = "Generate random points and divide the image around those points."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
		If task.intermediateReview = caller Then task.intermediateObject = Me
        If task.frameCount Mod updateFrequency <> 0 Then Exit Sub ' too fast otherwise...
        Dim rand As New Random()
        dst1.SetTo(0)
        Dim points = Enumerable.Range(0, 100).Select(Of cv.Point2f)(
            Function(i)
                Return New cv.Point2f(rand.Next(0, src.Width), rand.Next(0, src.Height))
            End Function).ToArray()
        For Each p In points
            dst1.Circle(p, 4, cv.Scalar.Red, -1, task.lineType)
        Next
        dst2 = dst1.Clone()

        Dim subdiv = New cv.Subdiv2D()
        subdiv.InitDelaunay(New cv.Rect(0, 0, dst2.Width, dst2.Height))
        subdiv.Insert(points)

        ' draw voronoi diagram
        Dim facetList()() As cv.Point2f = Nothing
        Dim facetCenters() As cv.Point2f = Nothing
        subdiv.GetVoronoiFacetList(Nothing, facetList, facetCenters)

        For Each list In facetList
            Dim before = list.Last()
            For Each p In list
                dst2.Line(before, p, cv.Scalar.Green, 1)
                before = p
            Next
        Next

        ' draw the delauney diagram
        Dim edgelist = subdiv.GetEdgeList()
        For Each edge In edgelist
            Dim p1 = New cv.Point(edge.Item0, edge.Item1)
            Dim p2 = New cv.Point(edge.Item2, edge.Item3)
            dst1.Line(p1, p2, cv.Scalar.Green, 1)
        Next
    End Sub
End Class






Public Class Delauney_Coverage
    Inherits VBparent
    Dim delauney As Delauney_Subdiv2D
    Public Sub New()
        initParent()
        delauney = New Delauney_Subdiv2D()
        delauney.updateFrequency = 1
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Clear image after x frames", 1, 100, 50)
        End If
        label1 = "Coverage of space"
        task.desc = "Combine random points with linear connections to neighbors to cover space. Note that space fills rapidly."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
		If task.intermediateReview = caller Then task.intermediateObject = Me
        If task.frameCount Mod sliders.trackbar(0).Value = 0 Then dst1.SetTo(0)
        delauney.Run(src)
        cv.Cv2.BitwiseOr(delauney.dst1, dst1, dst1)
    End Sub
End Class
