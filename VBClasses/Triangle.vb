Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Triangle_Basics : Inherits TaskParent
        Public triangles As New List(Of cv.Point3f)
        Dim redC As New RedColor_Basics
        Public Sub New()
            labels = {"", "", "RedColor_Basics output", "Selected contour - each pixel has depth"}
            desc = "Given a contour, convert that contour to a series of triangles"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            dst3.SetTo(0)
            Dim pt3D As New List(Of cv.Point3f)
            For Each pt In task.rcD.contour
                pt = New cv.Point(pt.X + task.rcD.rect.X, pt.Y + task.rcD.rect.Y)
                Dim vec = task.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X)
                If vec.Z = 0 Then
                    vec = Cloud_Basics.worldCoordinates(New cv.Point3f(pt.X, pt.Y, task.rcD.wcMean(2)))
                End If
                DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Yellow)
                pt3D.Add(vec)
            Next

            Dim c3D = task.pointCloud.Get(Of cv.Point3f)(task.rcD.maxDist.Y, task.rcD.maxDist.X)
            triangles.Clear()
            Dim color3D As New cv.Point3f(task.rcD.color(0), task.rcD.color(1), task.rcD.color(2))
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
            labels = {"", "Selected hull", "RedColor_Basics output", "Selected contour"}
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
                DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Yellow)
            Next

            dst1.SetTo(0)
            If rc.hull IsNot Nothing Then
                For Each pt In rc.hull
                    pt = New cv.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)
                    DrawCircle(dst1, pt, task.DotSize, cv.Scalar.Yellow)
                Next
            End If
        End Sub
    End Class







    Public Class NR_Triangle_Cell : Inherits TaskParent
        Public triangles As New List(Of cv.Point3f)
        Dim redC As New RedCloud_Basics
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels = {"", "", "RedMask_List output", "Selected contour - each pixel has depth"}
            desc = "Given a contour, convert that contour to a series of triangles"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 1)
            If task.rcD Is Nothing Then
                SetTrueText("Select any cell", 1)
                Exit Sub
            End If

            Dim rc = task.rcD
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
            dst3.Rectangle(cellRect, white, task.lineWidth)

            For Each pt In rc.contour
                Dim vec = task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X)
                pt = New cv.Point(xFactor * pt.X / rc.rect.Width, yFactor * pt.Y / rc.rect.Height)
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








    Public Class NR_Triangle_Mask : Inherits TaskParent
        Public triangles As New List(Of cv.Point3f)
        Dim redC As New RedCloud_Basics
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels = {"", "", "RedMask_List output", "Selected rc.mask - each pixel has depth. Red dot is maxDist."}
            desc = "Given a RedCloud cell, resize it and show the points with depth."
        End Sub

        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 1)
            If task.rcD Is Nothing Then
                SetTrueText("Select any cell", 1)
                Exit Sub
            End If

            Dim rc = task.rcD
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
            dst3.Rectangle(cellRect, white, task.lineWidth)

            triangles.Clear()
            For y = 0 To rc.rect.Height - 1
                For x = 0 To rc.rect.Width - 1
                    If rc.mask.Get(Of Byte)(y, x) = 0 Then Continue For
                    Dim vec = task.pointCloud(rc.rect).Get(Of cv.Point3f)(y, x)
                    Dim pt = New cv.Point2f(xFactor * x / rc.rect.Width, yFactor * y / rc.rect.Height)
                    DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Yellow)
                    pt3D.Add(vec)
                Next
            Next

            Dim newMaxDist = New cv.Point2f(xFactor * (rc.maxDist.X - rc.rect.X) / rc.rect.Width,
                                      yFactor * (rc.maxDist.Y - rc.rect.Y) / rc.rect.Height)
            DrawCircle(dst3, newMaxDist, task.DotSize + 2, cv.Scalar.Red)
        End Sub
    End Class



    Public Class Triangle_Find : Inherits TaskParent
        Public triangle As cv.Mat
        Public options As New Options_MinArea
        Public srcPoints As List(Of cv.Point2f)
        Public Sub New()
            desc = "Find minimum containing triangle for a set of points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            If task.heartBeat Then
                srcPoints = New List(Of cv.Point2f)(options.srcPoints)
            Else
                If srcPoints.Count < 3 Then Exit Sub ' not enough points
            End If

            Dim dataSrc(srcPoints.Count * 2 - 1) As Single ' input is a list of points.
            Dim dstData(3 * 2 - 1) As Single ' minTriangle returns 3 points

            dst2.SetTo(white)

            Dim input As cv.Mat = cv.Mat.FromPixelData(1, srcPoints.Count, cv.MatType.CV_32FC2, srcPoints.ToArray)
            Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length)
            Dim srcHandle = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
            Dim dstHandle = GCHandle.Alloc(dstData, GCHandleType.Pinned)
            MinTriangle_Run(srcHandle.AddrOfPinnedObject(), srcPoints.Count, dstHandle.AddrOfPinnedObject)
            srcHandle.Free()
            dstHandle.Free()
            triangle = cv.Mat.FromPixelData(3, 1, cv.MatType.CV_32FC2, dstData)

            For i = 0 To 2
                Dim pt = triangle.Get(Of cv.Point2f)(i)
                Dim p1 = New cv.Point(pt.X, pt.Y)
                pt = triangle.Get(Of cv.Point2f)((i + 1) Mod 3)
                Dim p2 = New cv.Point(pt.X, pt.Y)
                vbc.DrawLine(dst2, p1, p2, cv.Scalar.Black)
            Next

            For Each pt In srcPoints
                DrawCircle(dst2, pt, task.DotSize + 1, cv.Scalar.Red)
            Next
        End Sub
    End Class
End Namespace