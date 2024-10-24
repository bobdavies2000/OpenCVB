﻿Imports cvb = OpenCvSharp
Public Class Triangle_Basics : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public triangles As New List(Of cvb.Point3f)
    Public Sub New()
        labels = {"", "", "RedCloud_Hulls output", "Selected contour - each pixel has depth"}
        desc = "Given a contour, convert that contour to a series of triangles"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        If task.redCells.Count <= 1 Then Exit Sub
        Dim rc = task.rc
        If rc.index = 0 Then Exit Sub

        dst3.SetTo(0)
        Dim pt3D As New List(Of cvb.Point3f)
        For Each pt In rc.contour
            pt = New cvb.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)
            Dim vec = task.pointCloud.Get(Of cvb.Point3f)(pt.Y, pt.X)
            DrawCircle(dst3, pt, task.DotSize, cvb.Scalar.Yellow)
            pt3D.Add(vec)
        Next

        Dim c3D = task.pointCloud.Get(Of cvb.Point3f)(rc.maxDist.Y, rc.maxDist.X)
        triangles.Clear()
        Dim color3D As New cvb.Point3f(rc.color.Item2 / 255, rc.color.Item1 / 255, rc.color.Item0 / 255)
        For i = 0 To pt3D.Count - 1
            triangles.Add(color3D)
            triangles.Add(c3D)
            triangles.Add(pt3D(i))
            triangles.Add(pt3D((i + 1) Mod pt3D.Count))
        Next
    End Sub
End Class







Public Class Triangle_HullContour : Inherits TaskParent
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        task.gOptions.setDisplay1()
        labels = {"", "Selected cell", "RedCloud_Basics output", "Selected contour"}
        desc = "Given a contour, convert that contour to a series of triangles"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2
        If task.redCells.Count <= 1 Then Exit Sub
        Dim rc = task.rc

        rc.contour = ContourBuild(rc.mask, cvb.ContourApproximationModes.ApproxTC89L1)

        dst3.SetTo(0)
        For Each pt In rc.contour
            pt = New cvb.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)
            DrawCircle(dst3, pt, task.DotSize, cvb.Scalar.Yellow)
        Next

        dst1.SetTo(0)
        For Each pt In rc.hull
            pt = New cvb.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)
            DrawCircle(dst1, pt, task.DotSize, cvb.Scalar.Yellow)
        Next
    End Sub
End Class







Public Class Triangle_RedCloud : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public triangles As New List(Of cvb.Point3f)
    Public Sub New()
        labels = {"", "", "RedCloud_Basics output", "Selected contour - each pixel has depth"}
        desc = "Given a contour, convert that contour to a series of triangles"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst0 = redC.dst0
        dst1 = redC.dst1
        dst2 = redC.dst2

        If task.redCells.Count <= 1 Then Exit Sub
        If task.rc.index = 0 Then Exit Sub

        triangles.Clear()
        For Each rc In task.redCells
            Dim pt3D As New List(Of cvb.Point3f)
            For Each pt In rc.contour
                pt = New cvb.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)
                Dim vec = task.pointCloud.Get(Of cvb.Point3f)(pt.Y, pt.X)
                If vec.Z > 0 Then pt3D.Add(vec)
            Next

            Dim c3D = task.pointCloud.Get(Of cvb.Point3f)(rc.maxDist.Y, rc.maxDist.X)
            Dim color3D As New cvb.Point3f(rc.color.Item2 / 255, rc.color.Item1 / 255, rc.color.Item0 / 255)
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
    Dim redC As New RedCloud_Basics
    Public triangles As New List(Of cvb.Point3f)
    Public Sub New()
        labels = {"", "", "RedCloud_Basics output", "Selected contour - each pixel has depth"}
        desc = "Given a contour, convert that contour to a series of triangles"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        If task.redCells.Count <= 1 Then Exit Sub
        Dim rc = task.rc
        If rc.index = 0 Then Exit Sub

        dst3.SetTo(0)
        Dim pt3D As New List(Of cvb.Point3f)
        Dim aspectRect = rc.rect.Width / rc.rect.Height, aspect = dst2.Width / dst2.Height, cellRect As cvb.Rect
        Dim xFactor As Single, yFactor As Single
        If aspectRect > aspect Then
            cellRect = New cvb.Rect(0, 0, dst2.Width, rc.rect.Height * dst2.Width / rc.rect.Width)
            xFactor = dst2.Width
            yFactor = rc.rect.Height * dst2.Width / rc.rect.Width
        Else
            cellRect = New cvb.Rect(0, 0, rc.rect.Width * dst2.Height / rc.rect.Height, dst2.Height)
            xFactor = rc.rect.Width * dst2.Height / rc.rect.Height
            yFactor = dst2.Height
        End If
        dst3.Rectangle(cellRect, cvb.Scalar.White, task.lineWidth)

        For Each pt In rc.contour
            Dim vec = task.pointCloud(rc.rect).Get(Of cvb.Point3f)(pt.Y, pt.X)
            pt = New cvb.Point(xFactor * pt.X / rc.rect.Width, yFactor * pt.Y / rc.rect.Height)
            DrawCircle(dst3, pt, task.DotSize, cvb.Scalar.Yellow)
            pt3D.Add(vec)
        Next

        Dim c3D = task.pointCloud.Get(Of cvb.Point3f)(rc.maxDist.Y, rc.maxDist.X)
        triangles.Clear()
        Dim color3D As New cvb.Point3f(rc.color.Item2 / 255, rc.color.Item1 / 255, rc.color.Item0 / 255)
        For i = 0 To pt3D.Count - 1
            triangles.Add(color3D)
            triangles.Add(c3D)
            triangles.Add(pt3D(i))
            triangles.Add(pt3D((i + 1) Mod pt3D.Count))
        Next
    End Sub
End Class








Public Class Triangle_Mask : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public triangles As New List(Of cvb.Point3f)
    Public Sub New()
        labels = {"", "", "RedCloud_Basics output", "Selected rc.mask - each pixel has depth. Red dot is maxDist."}
        desc = "Given a RedCloud cell, resize it and show the points with depth."
    End Sub

    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        If task.redCells.Count <= 1 Then Exit Sub
        Dim rc = task.rc
        If rc.index = 0 Then Exit Sub

        dst3.SetTo(0)
        Dim pt3D As New List(Of cvb.Point3f)
        Dim aspectRect = rc.rect.Width / rc.rect.Height, aspect = dst2.Width / dst2.Height, cellRect As cvb.Rect
        Dim xFactor As Single, yFactor As Single
        If aspectRect > aspect Then
            cellRect = New cvb.Rect(0, 0, dst2.Width, rc.rect.Height * dst2.Width / rc.rect.Width)
            xFactor = dst2.Width
            yFactor = rc.rect.Height * dst2.Width / rc.rect.Width
        Else
            cellRect = New cvb.Rect(0, 0, rc.rect.Width * dst2.Height / rc.rect.Height, dst2.Height)
            xFactor = rc.rect.Width * dst2.Height / rc.rect.Height
            yFactor = dst2.Height
        End If
        dst3.Rectangle(cellRect, cvb.Scalar.White, task.lineWidth)

        triangles.Clear()
        For y = 0 To rc.rect.Height - 1
            For x = 0 To rc.rect.Width - 1
                If rc.mask.Get(Of Byte)(y, x) = 0 Then Continue For
                Dim vec = task.pointCloud(rc.rect).Get(Of cvb.Point3f)(y, x)
                Dim pt = New cvb.Point2f(xFactor * x / rc.rect.Width, yFactor * y / rc.rect.Height)
                DrawCircle(dst3, pt, task.DotSize, cvb.Scalar.Yellow)
                pt3D.Add(vec)
            Next
        Next

        Dim newMaxDist = New cvb.Point2f(xFactor * (rc.maxDist.X - rc.rect.X) / rc.rect.Width,
                                      yFactor * (rc.maxDist.Y - rc.rect.Y) / rc.rect.Height)
        DrawCircle(dst3, newMaxDist, task.DotSize + 2, cvb.Scalar.Red)
        labels(2) = redC.labels(2)
    End Sub
End Class