﻿Imports OpenCvSharp
Imports cv = OpenCvSharp
Public Class Triangle_Basics : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public triangles As New List(Of cv.Point3f)
    Public Sub New()
        labels = {"", "", "RedCloud_Hulls output", "Selected contour - each pixel has depth"}
        desc = "Given a contour, convert that contour to a series of triangles"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        If task.redCells.Count <= 1 Then Exit Sub
        Dim rc = task.rcSelect
        If rc.index = 0 Then Exit Sub

        dst3.SetTo(0)
        Dim pt3D As New List(Of cv.Point3f)
        For Each pt In rc.contour
            pt = New cv.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)
            Dim vec = task.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X)
            dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
            pt3D.Add(vec)
        Next

        Dim c3D = task.pointCloud.Get(Of cv.Point3f)(rc.maxDist.Y, rc.maxDist.X)
        triangles.Clear()
        Dim color3D As New cv.Point3f(rc.color.Item2 / 255, rc.color.Item1 / 255, rc.color.Item0 / 255)
        For i = 0 To pt3D.Count - 1
            triangles.Add(color3D)
            triangles.Add(c3D)
            triangles.Add(pt3D(i))
            triangles.Add(pt3D((i + 1) Mod pt3D.Count))
        Next
    End Sub
End Class







Public Class Triangle_HullContour : Inherits VB_Algorithm
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "Selected hull", "RedCloud_Hulls output", "Selected contour"}
        desc = "Given a contour, convert that contour to a series of triangles"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2
        If task.redCells.Count <= 1 Then Exit Sub
        Dim rc = task.rcSelect

        rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxTC89L1)

        dst3.SetTo(0)
        For Each pt In rc.contour
            pt = New cv.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)
            dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next

        dst1.SetTo(0)
        For Each pt In rc.hull
            pt = New cv.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)
            dst1.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next
    End Sub
End Class







Public Class Triangle_RedCloud : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public triangles As New List(Of cv.Point3f)
    Public Sub New()
        labels = {"", "", "RedCloud_Hulls output", "Selected contour - each pixel has depth"}
        desc = "Given a contour, convert that contour to a series of triangles"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        If task.redCells.Count <= 1 Then Exit Sub
        Dim rc = task.rcSelect
        If rc.index = 0 Then Exit Sub

        triangles.Clear()
        For Each rc In task.redCells
            Dim pt3D As New List(Of cv.Point3f)
            For Each pt In rc.contour
                pt = New cv.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)
                Dim vec = task.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X)
                If vec.Z > 0 Then pt3D.Add(vec)
            Next

            Dim c3D = task.pointCloud.Get(Of cv.Point3f)(rc.maxDist.Y, rc.maxDist.X)
            Dim color3D As New cv.Point3f(rc.color.Item2 / 255, rc.color.Item1 / 255, rc.color.Item0 / 255)
            For i = 0 To pt3D.Count - 1
                triangles.Add(color3D)
                triangles.Add(c3D)
                triangles.Add(pt3D(i))
                triangles.Add(pt3D((i + 1) Mod pt3D.Count))
            Next
        Next
    End Sub
End Class







Public Class Triangle_Cell : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public triangles As New List(Of cv.Point3f)
    Public Sub New()
        labels = {"", "", "RedCloud_Hulls output", "Selected contour - each pixel has depth"}
        desc = "Given a contour, convert that contour to a series of triangles"
    End Sub

    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        If task.redCells.Count <= 1 Then Exit Sub
        Dim rc = task.rcSelect
        If rc.index = 0 Then Exit Sub

        dst3.SetTo(0)
        Dim pt3D As New List(Of cv.Point3f)
        Dim aspectRect = rc.rect.Width / rc.rect.Height, aspect = dst2.Width / dst2.Height, cellRect As cv.Rect
        Dim xFactor As Single, yFactor As Single
        If aspectRect > aspect Then
            cellRect = New cv.Rect(0, 0, dst2.Width, rc.rect.Height * dst2.Width / rc.rect.Width)
            xFactor = dst2.Width
            yFactor = rc.rect.Height * dst2.Width / rc.rect.Width
        Else
            cellRect = New cv.Rect(0, 0, rc.rect.Width * dst2.Height / rc.rect.Height, dst2.Height)
            xFactor = rc.rect.Width * dst2.Height / rc.rect.Height
            yFactor = dst2.Height
        End If
        dst3.Rectangle(cellRect, cv.Scalar.White, task.lineWidth)

        For Each pt In rc.contour
            Dim vec = task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X)
            pt = New cv.Point(xFactor * pt.X / rc.rect.Width, yFactor * pt.Y / rc.rect.Height)
            dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
            pt3D.Add(vec)
        Next

        Dim c3D = task.pointCloud.Get(Of cv.Point3f)(rc.maxDist.Y, rc.maxDist.X)
        triangles.Clear()
        Dim color3D As New cv.Point3f(rc.color.Item2 / 255, rc.color.Item1 / 255, rc.color.Item0 / 255)
        For i = 0 To pt3D.Count - 1
            triangles.Add(color3D)
            triangles.Add(c3D)
            triangles.Add(pt3D(i))
            triangles.Add(pt3D((i + 1) Mod pt3D.Count))
        Next
    End Sub
End Class








Public Class Triangle_Mask : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public triangles As New List(Of cv.Point3f)
    Public Sub New()
        labels = {"", "", "RedCloud_Hulls output", "Selected rc.mask - each pixel has depth. Red dot is maxDist."}
        desc = "Given a RedCloud cell, resize it and show the points with depth."
    End Sub

    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        If task.redCells.Count <= 1 Then Exit Sub
        Dim rc = task.rcSelect
        If rc.index = 0 Then Exit Sub

        dst3.SetTo(0)
        Dim pt3D As New List(Of cv.Point3f)
        Dim aspectRect = rc.rect.Width / rc.rect.Height, aspect = dst2.Width / dst2.Height, cellRect As cv.Rect
        Dim xFactor As Single, yFactor As Single
        If aspectRect > aspect Then
            cellRect = New cv.Rect(0, 0, dst2.Width, rc.rect.Height * dst2.Width / rc.rect.Width)
            xFactor = dst2.Width
            yFactor = rc.rect.Height * dst2.Width / rc.rect.Width
        Else
            cellRect = New cv.Rect(0, 0, rc.rect.Width * dst2.Height / rc.rect.Height, dst2.Height)
            xFactor = rc.rect.Width * dst2.Height / rc.rect.Height
            yFactor = dst2.Height
        End If
        dst3.Rectangle(cellRect, cv.Scalar.White, task.lineWidth)

        triangles.Clear()
        For y = 0 To rc.rect.Height - 1
            For x = 0 To rc.rect.Width - 1
                If rc.mask.Get(Of Byte)(y, x) = 0 Then Continue For
                Dim vec = task.pointCloud(rc.rect).Get(Of cv.Point3f)(y, x)
                Dim pt = New cv.Point2f(xFactor * x / rc.rect.Width, yFactor * y / rc.rect.Height)
                dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
                pt3D.Add(vec)
            Next
        Next

        Dim newMaxDist = New cv.Point2f(xFactor * (rc.maxDist.X - rc.rect.X) / rc.rect.Width,
                                      yFactor * (rc.maxDist.Y - rc.rect.Y) / rc.rect.Height)
        dst3.Circle(newMaxDist, task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
        labels(2) = redC.labels(2)
    End Sub
End Class