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
            If vec.Z = 0 Then
                vec = getWorldCoordinates(New cv.Point3f(pt.X, pt.Y, task.rc.depthMean))
            End If
            DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Yellow)
            pt3D.Add(vec)
        Next

        Dim c3D = task.pointCloud.Get(Of cv.Point3f)(task.rc.maxDist.Y, task.rc.maxDist.X)
        triangles.Clear()
        Dim color3D As New cv.Point3f(task.rc.color(0), task.rc.color(1), task.rc.color(2))
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






Public Class Triangle_Basics2D : Inherits TaskParent
    Public points As New List(Of cv.Point3f)
    Public colors As New List(Of cv.Scalar)
    Public oglOptions As New Options_OpenGLFunctions
    Public hulls As New RedColor_Hulls
    Public Sub New()
        task.gOptions.GridSlider.Value = 30
        desc = "Prepare the list of 2D triangles"
    End Sub
    Private Function addTriangle(c1 As cv.Point, c2 As cv.Point, center As cv.Point, rc As rcData, shift As cv.Point3f) As List(Of cv.Point)
        Dim pt1 = getWorldCoordinates(New cv.Point3f(c1.X, c1.Y, rc.depthMean))
        Dim ptCenter = getWorldCoordinates(New cv.Point3f(center.X, center.Y, rc.depthMean))
        Dim pt2 = getWorldCoordinates(New cv.Point3f(c2.X, c2.Y, rc.depthMean))

        colors.Add(rc.color)
        points.Add(New cv.Point3f(pt1.X + shift.X, pt1.Y + shift.Y, pt1.Z + shift.Z))
        points.Add(New cv.Point3f(ptCenter.X + shift.X, ptCenter.Y + shift.Y, ptCenter.Z + shift.Z))
        points.Add(New cv.Point3f(pt2.X + shift.X, pt2.Y + shift.Y, pt2.Z + shift.Z))

        Dim points2d As New List(Of cv.Point)
        points2d.Add(c1)
        points2d.Add(center)
        points2d.Add(c2)
        Return points2d
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        oglOptions.RunOpt()
        Dim ptM = oglOptions.moveAmount
        Dim shift As New cv.Point3f(ptM(0), ptM(1), ptM(2))

        hulls.Run(src)
        dst2 = hulls.dst2
        points.Clear()
        colors.Clear()
        Dim listOfPoints = New List(Of List(Of cv.Point))
        For Each rc In task.rcList
            If rc.contour Is Nothing Then Continue For
            If rc.contour.Count < 5 Then Continue For
            Dim corners(4 - 1) As cv.Point
            For i = 0 To corners.Count - 1
                Dim pt = rc.contour(i * rc.contour.Count / 4)
                corners(i) = New cv.Point(rc.roi.X + pt.X, rc.roi.Y + pt.Y)
            Next
            Dim center = New cv.Point(rc.roi.X + rc.roi.Width / 2, rc.roi.Y + rc.roi.Height / 2)
            DrawLine(dst2, corners(0), center, white)
            DrawLine(dst2, corners(1), center, white)
            DrawLine(dst2, corners(2), center, white)
            DrawLine(dst2, corners(3), center, white)

            listOfPoints.Add(addTriangle(corners(0), corners(3), center, rc, shift))
            listOfPoints.Add(addTriangle(corners(1), corners(0), center, rc, shift))
            listOfPoints.Add(addTriangle(corners(2), corners(1), center, rc, shift))
            listOfPoints.Add(addTriangle(corners(3), corners(2), center, rc, shift))
        Next
        dst3.SetTo(0)
        For i = 0 To colors.Count - 1
            cv.Cv2.DrawContours(dst3, listOfPoints, i, colors(i), -1)
        Next
        labels(2) = CStr(colors.Count) + " triangles from " + CStr(task.rcList.Count) + " RedCloud cells"
    End Sub
End Class