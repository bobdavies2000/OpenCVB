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






Public Class Triangle_Basics2D : Inherits TaskParent
    Public points As New List(Of cv.Point3f)
    Public colors As New List(Of cv.Scalar)
    Public oglOptions As New Options_OpenGLFunctions
    Public hulls As New RedColor_Hulls
    Public Sub New()
        task.gOptions.setGridSize(30)
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







Public Class Tessallate_Triangles : Inherits TaskParent
    Public basics As New Triangle_Basics2D
    Public oglData As New List(Of cv.Point3f)
    Public Sub New()
        labels = {"", "", "", ""}
        desc = "Prepare colors and triangles for use in OpenGL Triangle presentation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        basics.Run(src)
        dst2 = basics.dst2
        dst3 = basics.dst3

        oglData.Clear()
        For i = 0 To basics.colors.Count - 1
            oglData.Add(New cv.Point3f(basics.colors(i)(2) / 255, basics.colors(i)(1) / 255, basics.colors(i)(0) / 255)) ' BGR to RGB
            For j = 0 To 3 - 1
                oglData.Add(basics.points(i * 3 + j))
            Next
        Next

        labels = basics.labels
    End Sub
End Class








Public Class Triangle_QuadSimple : Inherits TaskParent
    Public oglData As New List(Of cv.Point3f)
    Public oglOptions As New Options_OpenGLFunctions
    Public Sub New()
        task.gOptions.setGridSize(20)
        desc = "Create a triangle representation of the point cloud with RedCloud data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        oglOptions.RunOpt()
        Dim ptM = oglOptions.moveAmount
        Dim shift As New cv.Point3f(ptM(0), ptM(1), ptM(2))

        dst2 = runRedC(src, labels(2))
        oglData.Clear()
        dst3.SetTo(0)

        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)

            Dim center = New cv.Point(CInt(roi.X + roi.Width / 2), CInt(roi.Y + roi.Height / 2))
            Dim index = task.rcMap.Get(Of Byte)(center.Y, center.X)

            If index <= 0 Then Continue For
            Dim rc = task.rcList(index)

            dst3(roi).SetTo(rc.color)
            SetTrueText(Format(rc.depthMean, fmt1), New cv.Point(roi.X, roi.Y))

            Dim topLeft = getWorldCoordinates(New cv.Point3f(roi.X, roi.Y, rc.depthMean))
            Dim botRight = getWorldCoordinates(New cv.Point3f(roi.X + roi.Width, roi.Y + roi.Height, rc.depthMean))

            oglData.Add(New cv.Point3f(rc.color(2) / 255, rc.color(1) / 255, rc.color(0) / 255))
            oglData.Add(New cv.Point3f(topLeft.X + shift.X, topLeft.Y + shift.Y, rc.depthMean + shift.Z))
            oglData.Add(New cv.Point3f(botRight.X + shift.X, topLeft.Y + shift.Y, rc.depthMean + shift.Z))
            oglData.Add(New cv.Point3f(botRight.X + shift.X, botRight.Y + shift.Y, rc.depthMean + shift.Z))
            oglData.Add(New cv.Point3f(topLeft.X + shift.X, botRight.Y + shift.Y, rc.depthMean + shift.Z))
        Next
        labels = {"", "", traceName + " completed with " + Format(oglData.Count / 5, fmt0) + " quad sets (with a 5th element for color)", "Output of Triangle_QuadSimple"}
    End Sub
End Class






Public Class Triangle_QuadHulls : Inherits TaskParent
    Public oglData As New List(Of cv.Point3f)
    Public depthList As New List(Of List(Of Single))
    Public colorList As New List(Of cv.Scalar)
    Public oglOptions As New Options_OpenGLFunctions
    Dim hulls As New RedColor_Hulls
    Const depthListMaxCount As Integer = 10
    Public Sub New()
        task.gOptions.setGridSize(20)
        desc = "Create a triangle representation of the point cloud with RedCloud data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        oglOptions.RunOpt()
        Dim ptM = oglOptions.moveAmount
        Dim shift As New cv.Point3f(ptM(0), ptM(1), ptM(2))

        hulls.Run(src)
        dst2 = hulls.dst2

        If task.optionsChanged Then
            depthList = New List(Of List(Of Single))
            For i = 0 To task.gridRects.Count
                depthList.Add(New List(Of Single))
                colorList.Add(black)
            Next
        End If

        oglData.Clear()
        dst3.SetTo(0)

        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)

            Dim center = New cv.Point(CInt(roi.X + roi.Width / 2), CInt(roi.Y + roi.Height / 2))
            Dim index = task.rcMap.Get(Of Byte)(center.Y, center.X)

            If index <= 0 Then
                depthList(i).Clear()
                colorList(i) = black
                Continue For
            End If

            Dim rc = task.rcList(index)
            If rc.depthMean = 0 Then Continue For

            If colorList(i) <> rc.color Then depthList(i).Clear()

            depthList(i).Add(rc.depthMean)
            colorList(i) = rc.color

            If depthList(i).Count > 0 Then
                dst3(roi).SetTo(colorList(i))

                Dim depth = depthList(i).Average
                Dim topLeft = getWorldCoordinates(New cv.Point3f(roi.X, roi.Y, depth))
                Dim botRight = getWorldCoordinates(New cv.Point3f(roi.X + roi.Width, roi.Y + roi.Height, depth))

                oglData.Add(New cv.Point3f(rc.color(2) / 255, rc.color(1) / 255, rc.color(0) / 255))
                oglData.Add(New cv.Point3f(topLeft.X + shift.X, topLeft.Y + shift.Y, depth + shift.Z))
                oglData.Add(New cv.Point3f(botRight.X + shift.X, topLeft.Y + shift.Y, depth + shift.Z))
                oglData.Add(New cv.Point3f(botRight.X + shift.X, botRight.Y + shift.Y, depth + shift.Z))
                oglData.Add(New cv.Point3f(topLeft.X + shift.X, botRight.Y + shift.Y, depth + shift.Z))

                If depthList(i).Count >= depthListMaxCount Then depthList(i).RemoveAt(0)
            End If
        Next
        labels(2) = traceName + " completed with " + Format(oglData.Count / 5, fmt0) + " quad sets (with a 5th element for color)"
    End Sub
End Class







Public Class Triangle_QuadMinMax : Inherits TaskParent
    Public oglData As New List(Of cv.Point3f)
    Public depthList1 As New List(Of List(Of Single))
    Public depthList2 As New List(Of List(Of Single))
    Public colorList As New List(Of cv.Scalar)
    Public oglOptions As New Options_OpenGLFunctions
    Const depthListMaxCount As Integer = 10
    Public Sub New()
        task.gOptions.setGridSize(20)
        desc = "Create a triangle representation of the point cloud with RedCloud data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        oglOptions.RunOpt()
        Dim ptM = oglOptions.moveAmount
        Dim shift As New cv.Point3f(ptM(0), ptM(1), ptM(2))

        If task.optionsChanged Then
            depthList1 = New List(Of List(Of Single))
            depthList2 = New List(Of List(Of Single))
            For i = 0 To task.gridRects.Count
                depthList1.Add(New List(Of Single))
                depthList2.Add(New List(Of Single))
                colorList.Add(black)
            Next
        End If

        oglData.Clear()
        dst3.SetTo(0)

        Dim depth32f As cv.Mat = task.pcSplit(2) * 1000, depth32s As New cv.Mat
        depth32f.ConvertTo(depth32s, cv.MatType.CV_32S)
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)

            Dim center = New cv.Point(CInt(roi.X + roi.Width / 2), CInt(roi.Y + roi.Height / 2))
            Dim index = task.rcMap.Get(Of Byte)(center.Y, center.X)

            If index <= 0 Then
                depthList1(i).Clear()
                depthList2(i).Clear()
                colorList(i) = black
                Continue For
            End If

            Dim rc = task.rcList(index)
            If rc.depthMean = 0 Then Continue For

            If colorList(i) <> rc.color Then
                depthList1(i).Clear()
                depthList2(i).Clear()
            End If

            'Dim depthMin As Single, depthMax As Single, minLoc As cv.Point, maxLoc As cv.Point
            Dim mm = GetMinMax(depth32s(roi), task.depthMask(roi))
            'depth32s(roi).MinMaxLoc(depthMin, depthMax, minLoc, maxLoc, task.depthMask(roi))
            'depthMax /= 1000
            'depthMin /= 1000
            depthList1(i).Add(mm.minVal / 1000)
            depthList2(i).Add(mm.maxVal / 1000)
            colorList(i) = rc.color

            Dim d1 = depthList1(i).Average
            Dim d2 = depthList2(i).Average

            Dim depthCount = If(d1 = d2, 1, 2)
            For j = 0 To depthCount - 1
                Dim depth = Choose(j + 1, d1, d2)
                Dim topLeft = getWorldCoordinates(New cv.Point3f(roi.X, roi.Y, depth))
                Dim botRight = getWorldCoordinates(New cv.Point3f(roi.X + roi.Width, roi.Y + roi.Height, depth))

                Dim color = rc.color
                dst3(roi).SetTo(color)
                oglData.Add(New cv.Point3f(color(2) / 255, color(1) / 255, color(0) / 255))
                oglData.Add(New cv.Point3f(topLeft.X + shift.X, topLeft.Y + shift.Y, depth + shift.Z))
                oglData.Add(New cv.Point3f(botRight.X + shift.X, topLeft.Y + shift.Y, depth + shift.Z))
                oglData.Add(New cv.Point3f(botRight.X + shift.X, botRight.Y + shift.Y, depth + shift.Z))
                oglData.Add(New cv.Point3f(topLeft.X + shift.X, botRight.Y + shift.Y, depth + shift.Z))
            Next
            SetTrueText(Format(d1, fmt1) + vbCrLf + Format(d2, fmt1), New cv.Point(roi.X, roi.Y), 3)

            If depthList1(i).Count >= depthListMaxCount Then depthList1(i).RemoveAt(0)
            If depthList2(i).Count >= depthListMaxCount Then depthList2(i).RemoveAt(0)
        Next
        labels(2) = traceName + " completed with " + Format(oglData.Count / 5, fmt0) + " quad sets (with a 5th element for color)"
    End Sub
End Class






Public Class Triangle_Bricks : Inherits TaskParent
    Public oglData As New List(Of cv.Point3f)
    Public depths As New List(Of Single)
    Public options As New Options_OpenGLFunctions
    Public hulls As New RedColor_Hulls
    Dim depthMinList As New List(Of List(Of Single))
    Dim depthMaxList As New List(Of List(Of Single))
    Dim myListMax = 10
    Public Sub New()
        task.gOptions.setGridSize(20)
        desc = "Create triangles from each brick in point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            depthMinList.Clear()
            depthMaxList.Clear()
            For i = 0 To task.gridRects.Count - 1
                depthMinList.Add(New List(Of Single))
                depthMaxList.Add(New List(Of Single))
            Next
        End If

        options.RunOpt()
        Dim ptM = options.moveAmount
        Dim shift As New cv.Point3f(ptM(0), ptM(1), ptM(2))

        oglData.Clear()
        hulls.Run(src)
        dst2 = hulls.dst2

        Dim min(4 - 1) As cv.Point3f, max(4 - 1) As cv.Point3f
        depths.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            Dim center = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
            Dim index = task.rcMap.Get(Of Byte)(center.Y, center.X)
            Dim depthMin As Single = 0, depthMax As Single = 0, minLoc As cv.Point, maxLoc As cv.Point
            If index >= 0 Then
                task.pcSplit(2)(roi).MinMaxLoc(depthMin, depthMax, minLoc, maxLoc, task.depthMask(roi))
                Dim rc = task.rcList(index)
                depthMin = If(depthMax > rc.depthMean, rc.depthMean, depthMin)

                If depthMin > 0 And depthMax > 0 And depthMax < task.MaxZmeters Then
                    depthMinList(i).Add(depthMin)
                    depthMaxList(i).Add(depthMax)

                    depthMin = depthMinList(i).Average
                    Dim avg = depthMaxList(i).Average - depthMin
                    depthMax = depthMin + If(avg < 0.2, avg, 0.2) ' trim the max depth - often unreliable 
                    Dim color = rc.color
                    oglData.Add(New cv.Point3f(color(2) / 255, color(1) / 255, color(0) / 255))
                    For j = 0 To 4 - 1
                        Dim x = Choose(j + 1, roi.X, roi.X + roi.Width, roi.X + roi.Width, roi.X)
                        Dim y = Choose(j + 1, roi.Y, roi.Y, roi.Y + roi.Height, roi.Y + roi.Height)
                        min(j) = getWorldCoordinates(New cv.Point3f(x, y, depthMin))
                        max(j) = getWorldCoordinates(New cv.Point3f(x, y, depthMax))
                        min(j) += shift
                        oglData.Add(min(j))
                    Next

                    For j = 0 To 4 - 1
                        max(j) += shift
                        oglData.Add(max(j))
                    Next

                    oglData.Add(max(0))
                    oglData.Add(min(0))
                    oglData.Add(min(1))
                    oglData.Add(max(1))

                    oglData.Add(max(0))
                    oglData.Add(min(0))
                    oglData.Add(min(3))
                    oglData.Add(max(3))

                    oglData.Add(max(1))
                    oglData.Add(min(1))
                    oglData.Add(min(2))
                    oglData.Add(max(2))

                    oglData.Add(max(2))
                    oglData.Add(min(2))
                    oglData.Add(min(3))
                    oglData.Add(max(3))

                    SetTrueText(Format(depthMin, fmt1) + vbCrLf + Format(depthMax, fmt1), New cv.Point(roi.X, roi.Y))
                    If depthMinList(i).Count >= myListMax Then depthMinList(i).RemoveAt(0)
                    If depthMaxList(i).Count >= myListMax Then depthMaxList(i).RemoveAt(0)
                End If
            End If
            depths.Add(depthMin)
            depths.Add(depthMax)
        Next
        labels(2) = traceName + " completed: " + Format(task.gridRects.Count, fmt0) + " ROI's produced " + Format(oglData.Count / 25, fmt0) + " six sided bricks with color"
        SetTrueText("There should be no 0.0 values in the list of min and max depths in the dst2 image.", 3)
    End Sub
End Class






Public Class Triangle_IdealShapes : Inherits TaskParent
    Public triangles As New List(Of cv.Point3f)
    Dim shape As New Ideal_Shape
    Public Sub New()
        labels = {"", "", "RedColor_Hulls output", "Selected contour - each pixel has depth"}
        desc = "Build triangles from the Ideal_Shapes using the corners of the cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        shape.Run(src)
        labels(2) = shape.labels(2)

        dst2 = src
        Dim ptLast As cv.Point
        triangles.Clear()
        Dim cellSize = task.idealD.cellSize
        Dim keyPoints = {New cv.Point(0, 0), New cv.Point(0, cellSize - 1), New cv.Point(cellSize - 1, 0),
                         New cv.Point(0, cellSize - 1), New cv.Point(cellSize - 1, cellSize - 1), New cv.Point(cellSize - 1, 0)}
        For Each id In task.idList
            Dim r = id.lRect
            If id.lRect.Height <> cellSize Or id.lRect.Width <> cellSize Then Continue For
            ptLast = keyPoints(2)
            For i = 0 To 5
                Dim index = i Mod 3
                Dim pt = keyPoints(i)
                Dim vec = id.pcFrag.Get(Of cv.Point3f)(pt.Y, pt.X)
                If index = 0 Or index = 3 Then
                    triangles.Add(New cv.Point3f(id.color.Z / 255, id.color.Y / 255, id.color.X / 255))
                End If
                triangles.Add(vec)
                DrawLine(dst2(r), ptLast, pt, cv.Scalar.White, task.lineWidth)
                ptLast = pt
            Next
        Next

        If task.heartBeat Then
            labels(3) = CStr(CInt(triangles.Count / 3)) + " triangles generated from " + CStr(task.idList.Count) +
                        " ideal depth cells. Validation = " + CStr(CInt(triangles.Count / 8)) + " should equal " +
                        CStr(task.idList.Count)
        End If
    End Sub
End Class
