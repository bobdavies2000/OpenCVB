Imports cv = OpenCvSharp
Public Class Triangle_Basics : Inherits TaskParent
    Public triangles As New List(Of cv.Point3f)
    Public Sub New()
        labels = {"", "", "RedColor_Hulls output", "Selected contour - each pixel has depth"}
        desc = "Given a contour, convert that contour to a series of triangles"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        If task.rcList.Count <= 1 Then Exit Sub
        task.rc = task.rcList(1)

        dst3.SetTo(0)
        Dim pt3D As New List(Of cv.Point3f)
        For Each pt In task.rc.contour
            pt = New cv.Point(pt.X + task.rc.roi.X, pt.Y + task.rc.roi.Y)
            Dim vec = task.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X)
            DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Yellow)
            pt3D.Add(vec)
        Next

        Dim c3D = task.pointCloud.Get(Of cv.Point3f)(task.rc.maxDist.Y, task.rc.maxDist.X)
        triangles.Clear()
        Dim color3D As New cv.Point3f(task.rc.color(2) / 255, task.rc.color(1) / 255,
                                       task.rc.color(0) / 255)
        For i = 0 To pt3D.Count - 1
            triangles.Add(color3D)
            triangles.Add(c3D)
            triangles.Add(pt3D(i))
            triangles.Add(pt3D((i + 1) Mod pt3D.Count))
        Next
    End Sub
End Class







Public Class Triangle_HullContour : Inherits TaskParent
    Dim hulls As New RedColor_Hulls
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "Selected cell", "RedColor_Basics output", "Selected contour"}
        desc = "Given a contour, convert that contour to a series of triangles"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2
        If task.rcList.Count <= 1 Then Exit Sub
        Dim rc = task.rc

        rc.contour = ContourBuild(rc.mask, cv.ContourApproximationModes.ApproxTC89L1)

        dst3.SetTo(0)
        For Each pt In rc.contour
            pt = New cv.Point(pt.X + rc.roi.X, pt.Y + rc.roi.Y)
            DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Yellow)
        Next

        dst1.SetTo(0)
        For Each pt In rc.hull
            pt = New cv.Point(pt.X + rc.roi.X, pt.Y + rc.roi.Y)
            DrawCircle(dst1, pt, task.DotSize, cv.Scalar.Yellow)
        Next
    End Sub
End Class







Public Class Triangle_RedCloud : Inherits TaskParent
    Public triangles As New List(Of cv.Point3f)
    Public Sub New()
        labels = {"", "", "RedColor_Basics output", "Selected contour - each pixel has depth"}
        desc = "Given a contour, convert that contour to a series of triangles"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        If task.rcList.Count <= 1 Then Exit Sub
        task.rc = task.rcList(1)

        triangles.Clear()
        For Each rc In task.rcList
            Dim pt3D As New List(Of cv.Point3f)
            For Each pt In rc.contour
                pt = New cv.Point(pt.X + rc.roi.X, pt.Y + rc.roi.Y)
                Dim vec = task.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X)
                If vec.Z > 0 Then pt3D.Add(vec)
            Next

            Dim c3D = task.pointCloud.Get(Of cv.Point3f)(rc.maxDist.Y, rc.maxDist.X)
            Dim color3D As New cv.Point3f(rc.color(2) / 255, rc.color(1) / 255, rc.color(0) / 255)
            For i = 0 To pt3D.Count - 1
                triangles.Add(color3D)
                triangles.Add(c3D)
                triangles.Add(pt3D(i))
                triangles.Add(pt3D((i + 1) Mod pt3D.Count))
            Next
        Next
    End Sub
End Class







Public Class Triangle_Cell : Inherits TaskParent
    Public triangles As New List(Of cv.Point3f)
    Public Sub New()
        labels = {"", "", "RedColor_Basics output", "Selected contour - each pixel has depth"}
        desc = "Given a contour, convert that contour to a series of triangles"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        If task.rcList.Count <= 1 Then Exit Sub
        Dim rc = task.rc
        If rc.index = 0 Then Exit Sub

        dst3.SetTo(0)
        Dim pt3D As New List(Of cv.Point3f)
        Dim aspectRect = rc.roi.Width / rc.roi.Height, aspect = dst2.Width / dst2.Height, cellRect As cv.Rect
        Dim xFactor As Single, yFactor As Single
        If aspectRect > aspect Then
            cellRect = New cv.Rect(0, 0, dst2.Width, rc.roi.Height * dst2.Width / rc.roi.Width)
            xFactor = dst2.Width
            yFactor = rc.roi.Height * dst2.Width / rc.roi.Width
        Else
            cellRect = New cv.Rect(0, 0, rc.roi.Width * dst2.Height / rc.roi.Height, dst2.Height)
            xFactor = rc.roi.Width * dst2.Height / rc.roi.Height
            yFactor = dst2.Height
        End If
        dst3.Rectangle(cellRect, white, task.lineWidth)

        For Each pt In rc.contour
            Dim vec = task.pointCloud(rc.roi).Get(Of cv.Point3f)(pt.Y, pt.X)
            pt = New cv.Point(xFactor * pt.X / rc.roi.Width, yFactor * pt.Y / rc.roi.Height)
            DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Yellow)
            pt3D.Add(vec)
        Next

        Dim c3D = task.pointCloud.Get(Of cv.Point3f)(rc.maxDist.Y, rc.maxDist.X)
        triangles.Clear()
        Dim color3D As New cv.Point3f(rc.color(2) / 255, rc.color(1) / 255, rc.color(0) / 255)
        For i = 0 To pt3D.Count - 1
            triangles.Add(color3D)
            triangles.Add(c3D)
            triangles.Add(pt3D(i))
            triangles.Add(pt3D((i + 1) Mod pt3D.Count))
        Next
    End Sub
End Class








Public Class Triangle_Mask : Inherits TaskParent
    Public triangles As New List(Of cv.Point3f)
    Public Sub New()
        labels = {"", "", "RedColor_Basics output", "Selected rc.mask - each pixel has depth. Red dot is maxDist."}
        desc = "Given a RedCloud cell, resize it and show the points with depth."
    End Sub

    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        If task.rcList.Count <= 1 Then Exit Sub
        Dim rc = task.rc
        If rc.index = 0 Then Exit Sub

        dst3.SetTo(0)
        Dim pt3D As New List(Of cv.Point3f)
        Dim aspectRect = rc.roi.Width / rc.roi.Height, aspect = dst2.Width / dst2.Height, cellRect As cv.Rect
        Dim xFactor As Single, yFactor As Single
        If aspectRect > aspect Then
            cellRect = New cv.Rect(0, 0, dst2.Width, rc.roi.Height * dst2.Width / rc.roi.Width)
            xFactor = dst2.Width
            yFactor = rc.roi.Height * dst2.Width / rc.roi.Width
        Else
            cellRect = New cv.Rect(0, 0, rc.roi.Width * dst2.Height / rc.roi.Height, dst2.Height)
            xFactor = rc.roi.Width * dst2.Height / rc.roi.Height
            yFactor = dst2.Height
        End If
        dst3.Rectangle(cellRect, white, task.lineWidth)

        triangles.Clear()
        For y = 0 To rc.roi.Height - 1
            For x = 0 To rc.roi.Width - 1
                If rc.mask.Get(Of Byte)(y, x) = 0 Then Continue For
                Dim vec = task.pointCloud(rc.roi).Get(Of cv.Point3f)(y, x)
                Dim pt = New cv.Point2f(xFactor * x / rc.roi.Width, yFactor * y / rc.roi.Height)
                DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Yellow)
                pt3D.Add(vec)
            Next
        Next

        Dim newMaxDist = New cv.Point2f(xFactor * (rc.maxDist.X - rc.roi.X) / rc.roi.Width,
                                      yFactor * (rc.maxDist.Y - rc.roi.Y) / rc.roi.Height)
        DrawCircle(dst3, newMaxDist, task.DotSize + 2, cv.Scalar.Red)
        labels(2) = task.redC.labels(2)
    End Sub
End Class