Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp

Public Class Open3D_Shape : Inherits TaskParent
    Public options As New Options_AlphaShape
    Private points() As Single
    Private vertices() As Single
    Private triangles() As Integer
    Private maxPoints As Integer
    Private maxVertices As Integer
    Private maxTriangles As Integer
    Public Sub New()
        labels(2) = "Input point cloud (subsampled)"
        labels(3) = "Alpha shape mesh (top view)"
        desc = "Cursor.ai: Reconstruct a surface from the camera point cloud using Open3D CreateFromPointCloudAlphaShape."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim stepX = Math.Max(1, task.gridRects(0).Width)
        Dim stepY = Math.Max(1, task.gridRects(0).Height)
        Dim pointList As New List(Of cv.Point3f)

        For y = stepY \ 2 To task.pointCloud.Height - 1 Step stepY
            For x = stepX \ 2 To task.pointCloud.Width - 1 Step stepX
                If task.depthmask.Get(Of Byte)(y, x) = 0 Then Continue For
                Dim v = task.pointCloud.Get(Of cv.Vec3f)(y, x)
                If v.Item2 <= 0 OrElse Single.IsInfinity(v.Item2) OrElse Single.IsNaN(v.Item2) Then Continue For
                pointList.Add(New cv.Point3f(v.Item0, v.Item1, v.Item2))
            Next
        Next

        If pointList.Count < 4 Then
            labels(2) = "Need at least 4 valid depth points"
            labels(3) = "Alpha shape mesh"
            Exit Sub
        End If

        If maxPoints < pointList.Count Then
            maxPoints = pointList.Count
            ReDim points(maxPoints * 3 - 1)
            maxVertices = maxPoints * 2
            maxTriangles = maxPoints * 4
            ReDim vertices(maxVertices * 3 - 1)
            ReDim triangles(maxTriangles * 3 - 1)
        End If

        For i = 0 To pointList.Count - 1
            points(i * 3) = pointList(i).X
            points(i * 3 + 1) = pointList(i).Y
            points(i * 3 + 2) = pointList(i).Z
        Next

        Dim vertexCount As Integer
        Dim triangleCount As Integer
        Dim ok As Integer
        Dim pointsHandle = GCHandle.Alloc(points, GCHandleType.Pinned)
        Dim verticesHandle = GCHandle.Alloc(vertices, GCHandleType.Pinned)
        Dim trianglesHandle = GCHandle.Alloc(triangles, GCHandleType.Pinned)
        Try
            ok = Open3D_AlphaShape(pointsHandle.AddrOfPinnedObject(), pointList.Count, options.alpha,
                                   verticesHandle.AddrOfPinnedObject(), vertexCount, maxVertices,
                                   trianglesHandle.AddrOfPinnedObject(), triangleCount, maxTriangles)
        Finally
            pointsHandle.Free()
            verticesHandle.Free()
            trianglesHandle.Free()
        End Try

        dst2 = src.Clone()
        For Each pt In pointList
            Dim px = CInt(task.workRes.Width * pt.X / task.MaxZmeters)
            Dim py = CInt(task.workRes.Height * (1.0F - pt.Z / task.MaxZmeters))
            If px >= 0 AndAlso px < dst2.Width AndAlso py >= 0 AndAlso py < dst2.Height Then
                dst2.Circle(New cv.Point(px, py), 1, white, -1, task.lineType)
            End If
        Next

        dst3 = src.Clone()
        If ok <> 0 AndAlso vertexCount > 0 AndAlso triangleCount > 0 Then
            DrawMeshTopView(dst3, vertexCount, triangleCount)
            labels(3) = CStr(vertexCount) + " vertices, " + CStr(triangleCount) + " triangles, alpha=" + Format(options.alpha, "0.###")
        Else
            labels(3) = "Alpha shape failed for " + CStr(pointList.Count) + " points"
        End If

        labels(2) = CStr(pointList.Count) + " valid 3D points from camera point cloud"
    End Sub

    Private Sub DrawMeshTopView(dst As cv.Mat, vertexCount As Integer, triangleCount As Integer)
        Dim mapX(vertexCount - 1) As Integer
        Dim mapY(vertexCount - 1) As Integer
        For i = 0 To vertexCount - 1
            Dim x = vertices(i * 3)
            Dim z = vertices(i * 3 + 2)
            mapX(i) = CInt(task.workRes.Width * x / task.MaxZmeters)
            mapY(i) = CInt(task.workRes.Height * (1.0F - z / task.MaxZmeters))
        Next

        For t = 0 To triangleCount - 1
            Dim i0 = triangles(t * 3)
            Dim i1 = triangles(t * 3 + 1)
            Dim i2 = triangles(t * 3 + 2)
            If i0 < 0 OrElse i0 >= vertexCount OrElse i1 < 0 OrElse i1 >= vertexCount OrElse i2 < 0 OrElse i2 >= vertexCount Then
                Continue For
            End If
            dst.Line(New cv.Point(mapX(i0), mapY(i0)), New cv.Point(mapX(i1), mapY(i1)), white, 1, task.lineType)
            dst.Line(New cv.Point(mapX(i1), mapY(i1)), New cv.Point(mapX(i2), mapY(i2)), white, 1, task.lineType)
            dst.Line(New cv.Point(mapX(i2), mapY(i2)), New cv.Point(mapX(i0), mapY(i0)), white, 1, task.lineType)
        Next
    End Sub
End Class

