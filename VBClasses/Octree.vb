Imports OpenCvSharp
Imports OpenCvSharp.Cv2
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Octree_Basics : Inherits TaskParent
        Dim options As New Options_Octree
        Dim world2cam As Mat
        Public meshVerts As New List(Of Point3f)
        Public meshColors As New List(Of Point3f)
        Public neighbors As New List(Of Point3f)
        Public queryPt As Point3f
        Public hasQuery As Boolean
        Public Sub New()
            world2cam = New Mat(4, 4, MatType.CV_64F, Scalar.All(0))
            world2cam.Set(Of Double)(0, 0, 1)
            world2cam.Set(Of Double)(1, 1, -1)
            world2cam.Set(Of Double)(2, 2, -1)
            world2cam.Set(Of Double)(3, 3, 1)
            desc = "Cursor.ai: Build an octree from the point cloud and rasterize a mesh of those points."
        End Sub
        Private Shared Function PointCount(m As Mat) As Integer
            If m Is Nothing OrElse m.Empty Then Return 0
            If m.Channels = 3 Then Return CInt(m.Total)
            If m.Cols = 3 Then Return m.Rows
            Return CInt(m.Total)
        End Function
        Private Shared Function PointAt(m As Mat, i As Integer) As Point3f
            If m.Channels = 3 Then
                If m.Cols = 1 Then Return m.Get(Of Point3f)(i, 0)
                Return m.Get(Of Point3f)(0, i)
            End If
            Return New Point3f(m.Get(Of Single)(i, 0), m.Get(Of Single)(i, 1), m.Get(Of Single)(i, 2))
        End Function
        Private Shared Function Project(p As Point3f) As cv.Point
            Dim fx = task.calibData.leftIntrinsics.fx
            Dim fy = task.calibData.leftIntrinsics.fy
            If p.Z <= 0 Or fx = 0 Or fy = 0 Then Return New cv.Point(-1, -1)
            Dim u = CInt(fx * p.X / p.Z + task.calibData.leftIntrinsics.ppx)
            Dim v = CInt(fy * p.Y / p.Z + task.calibData.leftIntrinsics.ppy)
            Return New cv.Point(u, v)
        End Function
        Private Shared Sub DrawPoint(dst As Mat, p As Point3f, color As Scalar, radius As Integer)
            Dim pt = Project(p)
            If pt.X < 0 Or pt.Y < 0 Or pt.X >= dst.Width Or pt.Y >= dst.Height Then Exit Sub
            Circle(dst, pt, radius, color, -1, task.lineType)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            meshVerts.Clear()
            meshColors.Clear()
            neighbors.Clear()
            hasQuery = False
            options.Run()

            Dim cloud = task.pointCloud
            If cloud.Empty Or cloud.Type <> MatType.CV_32FC3 Then
                SetTrueText("Point cloud is not ready.", 2)
                Exit Sub
            End If

            Dim stepSize = Math.Max(2, options.stepSize)
            Dim cols = (cloud.Width - 1) \ stepSize + 1
            Dim rows = (cloud.Height - 1) \ stepSize + 1
            If cols < 2 Or rows < 2 Then Exit Sub

            Dim zNear As Double = 0.05
            Dim zFar As Double = Math.Max(1.0, task.MaxZmeters) + 1
            Dim depthBreak = options.depthBreakCm / 100.0F
            Dim useColor = src.Type = MatType.CV_8UC3

            Dim vertices As New List(Of Point3f)
            Dim colors As New List(Of Point3f)
            Dim indexMap(rows * cols - 1) As Integer
            For i = 0 To indexMap.Length - 1
                indexMap(i) = -1
            Next

            For gy = 0 To rows - 1
                Dim y = Math.Min(cloud.Height - 1, gy * stepSize)
                For gx = 0 To cols - 1
                    Dim x = Math.Min(cloud.Width - 1, gx * stepSize)
                    Dim p = cloud.Get(Of Point3f)(y, x)
                    If Single.IsNaN(p.X) Or Single.IsNaN(p.Y) Or Single.IsNaN(p.Z) Then Continue For
                    If Single.IsInfinity(p.X) Or Single.IsInfinity(p.Y) Or Single.IsInfinity(p.Z) Then Continue For
                    If p.Z <= zNear Or p.Z >= zFar - 1 Then Continue For
                    indexMap(gy * cols + gx) = vertices.Count
                    vertices.Add(p)
                    If useColor Then
                        Dim bgr = src.Get(Of Vec3b)(y, x)
                        colors.Add(New Point3f(bgr(0) / 255.0F, bgr(1) / 255.0F, bgr(2) / 255.0F))
                    Else
                        colors.Add(New Point3f(1, 1, 1))
                    End If
                Next
            Next

            If vertices.Count < 3 Then
                SetTrueText("Not enough depth points for an octree.", 2)
                Exit Sub
            End If

            Dim triangles As New List(Of Vec3i)
            For gy = 0 To rows - 2
                For gx = 0 To cols - 2
                    Dim i00 = indexMap(gy * cols + gx)
                    Dim i10 = indexMap(gy * cols + gx + 1)
                    Dim i01 = indexMap((gy + 1) * cols + gx)
                    Dim i11 = indexMap((gy + 1) * cols + gx + 1)
                    If i00 >= 0 And i10 >= 0 And i11 >= 0 Then
                        If DepthSpan(vertices(i00), vertices(i10), vertices(i11)) <= depthBreak Then
                            triangles.Add(New Vec3i(i00, i10, i11))
                            AddMeshTri(vertices, colors, i00, i10, i11)
                        End If
                    End If
                    If i00 >= 0 And i11 >= 0 And i01 >= 0 Then
                        If DepthSpan(vertices(i00), vertices(i11), vertices(i01)) <= depthBreak Then
                            triangles.Add(New Vec3i(i00, i11, i01))
                            AddMeshTri(vertices, colors, i00, i11, i01)
                        End If
                    End If
                Next
            Next

            Dim vertArr = vertices.ToArray()
            Dim colorArr = colors.ToArray()
            ' Map the cloud into [0, 3] so OpenCV's octree cube contains every point.
            ' CreateWithDepth sizes that cube to half the next power of two of the extent,
            ' which leaves the far corner outside the cube. Distances stay uniform.
            Dim minX = vertices.Min(Function(p As Point3f) p.X)
            Dim minY = vertices.Min(Function(p As Point3f) p.Y)
            Dim minZ = vertices.Min(Function(p As Point3f) p.Z)
            Dim maxX = vertices.Max(Function(p As Point3f) p.X)
            Dim maxY = vertices.Max(Function(p As Point3f) p.Y)
            Dim maxZ = vertices.Max(Function(p As Point3f) p.Z)
            Dim extent = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ))
            If extent <= 0.000001F Then
                SetTrueText("Point cloud has no spatial extent.", 2)
                Exit Sub
            End If
            Dim scale = 3.0F / extent
            Dim octPts(vertices.Count - 1) As Point3f
            For i = 0 To vertices.Count - 1
                Dim p = vertices(i)
                octPts(i) = New Point3f((p.X - minX) * scale, (p.Y - minY) * scale, (p.Z - minZ) * scale)
            Next
            Dim resolution = 4.0 / (1 << options.maxDepth)

            Dim vertMat = Mat.FromPixelData(vertArr.Length, 1, MatType.CV_32FC3, vertArr).Clone()
            Dim colorMat = Mat.FromPixelData(colorArr.Length, 1, MatType.CV_32FC3, colorArr).Clone()
            Dim octMat = Mat.FromPixelData(octPts.Length, 1, MatType.CV_32FC3, octPts).Clone()

            Dim leafCount As Integer
            Dim knnPts As New Mat
            Dim radiusCount As Integer
            Dim query As Point3f
            Dim haveQuery As Boolean

            Using cloudTree = Octree.CreateWithResolution(resolution, octMat, colorMat)
                Dim restored As New Mat
                cloudTree.GetPointCloud(restored)
                leafCount = PointCount(restored)

                Dim click = task.clickPoint
                Dim gx = Math.Min(cols - 1, Math.Max(0, click.X \ stepSize))
                Dim gy = Math.Min(rows - 1, Math.Max(0, click.Y \ stepSize))
                Dim qIndex = indexMap(gy * cols + gx)
                If qIndex < 0 Then
                    Dim best = Integer.MaxValue
                    For i = 0 To vertices.Count - 1
                        Dim pt = Project(vertices(i))
                        Dim dx = pt.X - click.X
                        Dim dy = pt.Y - click.Y
                        Dim d = dx * dx + dy * dy
                        If d < best Then
                            best = d
                            qIndex = i
                        End If
                    Next
                End If

                If qIndex >= 0 Then
                    query = vertices(qIndex)
                    haveQuery = True
                    Dim qScaled = New Point3f((query.X - minX) * scale, (query.Y - minY) * scale, (query.Z - minZ) * scale)
                    cloudTree.KNNSearch(qScaled, Math.Min(options.knnCount, vertices.Count), knnPts, New Mat)
                    radiusCount = cloudTree.RadiusNNSearch(qScaled, CSng(options.radiusCm / 100.0F * scale), New Mat, New Mat)
                End If
            End Using

            Dim colorBuf As New Mat(dst2.Size, MatType.CV_32FC3, New Scalar(0))
            Dim depthBuf As New Mat(dst2.Size, MatType.CV_32F, New Scalar(zFar))
            If triangles.Count > 0 Then
                Dim triArr = triangles.ToArray()
                Dim indexMat = Mat.FromPixelData(triArr.Length, 1, MatType.CV_32SC3, triArr).Clone()
                Dim fy = task.calibData.leftIntrinsics.fy
                Dim fovY As Double = Math.PI / 3
                If fy > 1 Then fovY = 2 * Math.Atan(cloud.Height / (2.0 * fy))
                Dim settings = New TriangleRasterizeSettings(TriangleShadingType.Shaded,
                                                             TriangleCullingMode.None,
                                                             TriangleGlCompatibleMode.Disabled)
                TriangleRasterize(vertMat, indexMat, colorMat, colorBuf, depthBuf, world2cam, fovY, zNear, zFar, settings)
            End If

            colorBuf.ConvertTo(dst2, MatType.CV_8UC3, 255)

            Dim farMask As New Mat
            InRange(depthBuf, zFar - 0.01, zFar + 0.01, farMask)
            Dim depth8 As New Mat
            Normalize(depthBuf, depth8, 0, 255, NormTypes.MinMax, MatType.CV_8U)
            ApplyColorMap(depth8, dst3, ColormapTypes.Jet)
            dst3.SetTo(0, farMask)

            Dim knnShown = 0
            If haveQuery Then
                For i = 0 To PointCount(knnPts) - 1
                    Dim p = PointAt(knnPts, i)
                    p = New Point3f(p.X / scale + minX, p.Y / scale + minY, p.Z / scale + minZ)
                    neighbors.Add(p)
                    DrawPoint(dst2, p, yellow, task.DotSize)
                    DrawPoint(dst3, p, yellow, task.DotSize)
                    knnShown += 1
                Next
                DrawPoint(dst2, query, red, task.DotSize + 2)
                DrawPoint(dst3, query, red, task.DotSize + 2)
                queryPt = query
                hasQuery = True
            End If

            labels(2) = "TriangleRasterize color of the point-cloud mesh" + CStr(leafCount) +
                        " octree leaves from " + CStr(vertices.Count) +
                        " points, " + CStr(triangles.Count) + " triangles"
            labels(3) = "Rasterized depth with octree neighbors" + "Click KNN " +
                        CStr(knnShown) + ", radius " + (options.radiusCm / 100.0).ToString(fmt2) +
                        "m found " + CStr(radiusCount)
        End Sub
        Private Sub AddMeshTri(verts As List(Of Point3f), cols As List(Of Point3f), i0 As Integer, i1 As Integer, i2 As Integer)
            meshVerts.Add(verts(i0))
            meshVerts.Add(verts(i1))
            meshVerts.Add(verts(i2))
            meshColors.Add(cols(i0))
            meshColors.Add(cols(i1))
            meshColors.Add(cols(i2))
        End Sub
        Private Shared Function DepthSpan(a As Point3f, b As Point3f, c As Point3f) As Single
            Dim zmax = Math.Max(a.Z, Math.Max(b.Z, c.Z))
            Dim zmin = Math.Min(a.Z, Math.Min(b.Z, c.Z))
            Return zmax - zmin
        End Function
    End Class




    Public Class Octree_Rasterize : Inherits TaskParent
        ' Axis-aligned 3D bounding box
        Public Structure Rect3d
            Public X As Double
            Public Y As Double
            Public Z As Double
            Public Width As Double
            Public Height As Double
            Public Depth As Double

            Public Sub New(x As Double, y As Double, z As Double,
                   width As Double, height As Double, depth As Double)
                Me.X = x
                Me.Y = y
                Me.Z = z
                Me.Width = width
                Me.Height = height
                Me.Depth = depth
            End Sub

            ' Check if a Point3f lies inside this box
            Public Function Contains(p As Point3f) As Boolean
                Return p.X >= X AndAlso p.X <= X + Width AndAlso
               p.Y >= Y AndAlso p.Y <= Y + Height AndAlso
               p.Z >= Z AndAlso p.Z <= Z + Depth
            End Function

            ' Center of the box
            Public ReadOnly Property Center As Point3f
                Get
                    Return New Point3f(
                CSng(X + Width / 2),
                CSng(Y + Height / 2),
                CSng(Z + Depth / 2))
                End Get
            End Property
        End Structure

        Public Class OctreeNode
            Public Bounds As Rect3d
            Public Points As List(Of Point3f)
            Public Children As OctreeNode()

            Public Sub New(bounds As Rect3d)
                Me.Bounds = bounds
                Me.Points = New List(Of Point3f)()
                Me.Children = Nothing
            End Sub

            Public Sub Insert(p As Point3f, maxPoints As Integer)
                ' If this node has children, delegate
                If Children IsNot Nothing Then
                    For Each child In Children
                        If child.Bounds.Contains(p) Then
                            child.Insert(p, maxPoints)
                            Return
                        End If
                    Next
                End If

                ' Otherwise, add point
                Points.Add(p)

                ' Split if too many points
                If Points.Count > maxPoints Then
                    Subdivide(maxPoints)
                End If
            End Sub

            Private Sub Subdivide(maxPoints As Integer)
                Children = New OctreeNode(7) {} ' 8 children
                Dim cx = Bounds.X + Bounds.Width / 2
                Dim cy = Bounds.Y + Bounds.Height / 2
                Dim cz = Bounds.Z + Bounds.Depth / 2

                Dim hw = Bounds.Width / 2
                Dim hh = Bounds.Height / 2
                Dim hd = Bounds.Depth / 2

                Dim i As Integer = 0
                For dx = 0 To 1
                    For dy = 0 To 1
                        For dz = 0 To 1
                            Children(i) = New OctreeNode(New Rect3d(
                        Bounds.X + dx * hw,
                        Bounds.Y + dy * hh,
                        Bounds.Z + dz * hd,
                        hw, hh, hd))
                            i += 1
                        Next
                    Next
                Next

                ' Reinsert points into children
                Dim oldPoints = Points
                Points = New List(Of Point3f)()
                For Each p In oldPoints
                    Insert(p, maxPoints)
                Next
            End Sub
        End Class

        Public tri3D As New List(Of Point3f)
        Public triBGR As Point3f
        Public Sub New()
            triBGR = New Point3f(0, 0, 1)
            desc = "Cursor.ai: Synthetic example of rasterizing a 3D object."
        End Sub
        Private Shared Function ToWorld(pt As cv.Point) As Point3f
            Dim x = Math.Min(Math.Max(pt.X, 0), task.workRes.Width - 1)
            Dim y = Math.Min(Math.Max(pt.Y, 0), task.workRes.Height - 1)
            If task.pointCloud IsNot Nothing AndAlso task.pointCloud.Width > 0 Then
                Dim p = task.pointCloud.Get(Of Point3f)(y, x)
                If p.Z > 0 Then Return p
            End If
            Return Cloud_Basics.worldCoordinates(x, y, 1.0F)
        End Function
        Public Shared Sub RasterizeTriangle(img As Mat, p1 As Point, p2 As Point, p3 As Point, color As Scalar)
            ' Sort vertices by Y
            Dim pts = New List(Of Point) From {p1, p2, p3}
            pts.Sort(Function(a, b) a.Y.CompareTo(b.Y))

            Dim v1 = pts(0)
            Dim v2 = pts(1)
            Dim v3 = pts(2)

            ' Compute slopes
            Dim invslope1 As Double = (v2.X - v1.X) / (v2.Y - v1.Y)
            Dim invslope2 As Double = (v3.X - v1.X) / (v3.Y - v1.Y)

            Dim curx1 As Double = v1.X
            Dim curx2 As Double = v1.X

            For scanlineY = v1.Y To v2.Y
                Cv2.Line(img, New Point(CInt(curx1), scanlineY), New Point(CInt(curx2), scanlineY), color)
                curx1 += invslope1
                curx2 += invslope2
            Next

            invslope1 = (v3.X - v2.X) / (v3.Y - v2.Y)
            curx1 = v2.X
            curx2 = curx2

            For scanlineY = v2.Y To v3.Y
                Cv2.Line(img, New Point(CInt(curx1), scanlineY), New Point(CInt(curx2), scanlineY), color)
                curx1 += invslope1
                curx2 += invslope2
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim p1 = New cv.Point(50, 50)
            Dim p2 = New cv.Point(150, 100)
            Dim p3 = New cv.Point(100, 150)
            dst2 = task.color.Clone
            RasterizeTriangle(dst2, p1, p2, p3, Scalar.Red)

            tri3D.Clear()
            tri3D.Add(ToWorld(p1))
            tri3D.Add(ToWorld(p2))
            tri3D.Add(ToWorld(p3))
            triBGR = New Point3f(0, 0, 1)
            labels(2) = "Octree_Rasterize triangle"
        End Sub
    End Class

End Namespace
