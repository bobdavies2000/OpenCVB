Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Triangle_Basics : Inherits TaskParent
        Public triangles As New List(Of cv.Point3f)
        Public Sub New()
            labels = {"", "", "RedList_Hulls output", "Selected contour - each pixel has depth"}
            desc = "Given a contour, convert that contour to a series of triangles"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))

            If taskA.redList.oldrclist.Count <= 1 Then Exit Sub
            taskA.oldrcD = taskA.redList.oldrclist(1)

            dst3.SetTo(0)
            Dim pt3D As New List(Of cv.Point3f)
            For Each pt In taskA.oldrcD.contour
                pt = New cv.Point(pt.X + taskA.oldrcD.rect.X, pt.Y + taskA.oldrcD.rect.Y)
                Dim vec = taskA.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X)
                If vec.Z = 0 Then
                    vec = Cloud_Basics.worldCoordinates(New cv.Point3f(pt.X, pt.Y, taskA.oldrcD.depth))
                End If
                DrawCircle(dst3, pt, taskA.DotSize, cv.Scalar.Yellow)
                pt3D.Add(vec)
            Next

            Dim c3D = taskA.pointCloud.Get(Of cv.Point3f)(taskA.oldrcD.maxDist.Y, taskA.oldrcD.maxDist.X)
            triangles.Clear()
            Dim color3D As New cv.Point3f(taskA.oldrcD.color(0), taskA.oldrcD.color(1), taskA.oldrcD.color(2))
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
            If standalone Then taskA.gOptions.displayDst1.Checked = True
            labels = {"", "Selected cell", "RedColor_Basics output", "Selected contour"}
            desc = "Given a contour, convert that contour to a series of triangles"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hulls.Run(src)
            dst2 = hulls.dst2
            If hulls.rclist.Count <= 1 Then Exit Sub
            Static rc As rcData = hulls.rclist(0)

            rc.contour = ContourBuild(rc.mask, cv.ContourApproximationModes.ApproxTC89L1)

            dst3.SetTo(0)
            For Each pt In rc.contour
                pt = New cv.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)
                DrawCircle(dst3, pt, taskA.DotSize, cv.Scalar.Yellow)
            Next

            dst1.SetTo(0)
            For Each pt In rc.hull
                pt = New cv.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)
                DrawCircle(dst1, pt, taskA.DotSize, cv.Scalar.Yellow)
            Next

            RedCloud_Cell.selectCell(hulls.rcMap, hulls.rclist)
            rc = If(taskA.rcD Is Nothing, hulls.rclist(0), taskA.rcD)
        End Sub
    End Class







    Public Class NR_Triangle_Cell : Inherits TaskParent
        Public triangles As New List(Of cv.Point3f)
        Public Sub New()
            labels = {"", "", "RedList_Basics output", "Selected contour - each pixel has depth"}
            desc = "Given a contour, convert that contour to a series of triangles"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))
            If taskA.redList.oldrclist.Count <= 1 Then Exit Sub
            Dim rc = taskA.oldrcD
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
            dst3.Rectangle(cellRect, white, taskA.lineWidth)

            For Each pt In rc.contour
                Dim vec = taskA.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X)
                pt = New cv.Point(xFactor * pt.X / rc.rect.Width, yFactor * pt.Y / rc.rect.Height)
                DrawCircle(dst3, pt, taskA.DotSize, cv.Scalar.Yellow)
                pt3D.Add(vec)
            Next

            Dim c3D = taskA.pointCloud.Get(Of cv.Point3f)(rc.maxDist.Y, rc.maxDist.X)
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








    Public Class NR_Triangle_Mask : Inherits TaskParent
        Public triangles As New List(Of cv.Point3f)
        Public Sub New()
            labels = {"", "", "RedList_Basics output", "Selected rc.mask - each pixel has depth. Red dot is maxDist."}
            desc = "Given a RedCloud cell, resize it and show the points with depth."
        End Sub

        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))
            If taskA.redList.oldrclist.Count <= 1 Then Exit Sub
            Dim rc = taskA.oldrcD
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
            dst3.Rectangle(cellRect, white, taskA.lineWidth)

            triangles.Clear()
            For y = 0 To rc.rect.Height - 1
                For x = 0 To rc.rect.Width - 1
                    If rc.mask.Get(Of Byte)(y, x) = 0 Then Continue For
                    Dim vec = taskA.pointCloud(rc.rect).Get(Of cv.Point3f)(y, x)
                    Dim pt = New cv.Point2f(xFactor * x / rc.rect.Width, yFactor * y / rc.rect.Height)
                    DrawCircle(dst3, pt, taskA.DotSize, cv.Scalar.Yellow)
                    pt3D.Add(vec)
                Next
            Next

            Dim newMaxDist = New cv.Point2f(xFactor * (rc.maxDist.X - rc.rect.X) / rc.rect.Width,
                                      yFactor * (rc.maxDist.Y - rc.rect.Y) / rc.rect.Height)
            DrawCircle(dst3, newMaxDist, taskA.DotSize + 2, cv.Scalar.Red)
            labels(2) = taskA.redList.labels(2)
        End Sub
    End Class
End Namespace