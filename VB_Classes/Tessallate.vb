Imports cvb = OpenCvSharp
Public Class Tessallate_Basics : Inherits TaskParent
    Public points As New List(Of cvb.Point3f)
    Public colors As New List(Of cvb.Scalar)
    Public oglOptions As New Options_OpenGLFunctions
    Public hulls As New RedCloud_Hulls
    Public Sub New()
        task.gOptions.setGridSize(30)
        desc = "Prepare the list of 2D triangles"
    End Sub
    Private Function addTriangle(c1 As cvb.Point, c2 As cvb.Point, center As cvb.Point, rc As rcData, shift As cvb.Point3f) As List(Of cvb.Point)
        Dim pt1 = getWorldCoordinates(New cvb.Point3f(c1.X, c1.Y, rc.depthMean(2)))
        Dim ptCenter = getWorldCoordinates(New cvb.Point3f(center.X, center.Y, rc.depthMean(2)))
        Dim pt2 = getWorldCoordinates(New cvb.Point3f(c2.X, c2.Y, rc.depthMean(2)))

        colors.Add(rc.color)
        points.Add(New cvb.Point3f(pt1.X + shift.X, pt1.Y + shift.Y, pt1.Z + shift.Z))
        points.Add(New cvb.Point3f(ptCenter.X + shift.X, ptCenter.Y + shift.Y, ptCenter.Z + shift.Z))
        points.Add(New cvb.Point3f(pt2.X + shift.X, pt2.Y + shift.Y, pt2.Z + shift.Z))

        Dim points2d As New List(Of cvb.Point)
        points2d.Add(c1)
        points2d.Add(center)
        points2d.Add(c2)
        Return points2d
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        oglOptions.RunOpt()
        Dim ptM = oglOptions.moveAmount
        Dim shift As New cvb.Point3f(ptM(0), ptM(1), ptM(2))

        hulls.Run(src)
        dst2 = hulls.dst2
        points.Clear()
        colors.Clear()
        Dim listOfPoints = New List(Of List(Of cvb.Point))
        For Each rc In task.redCells
            If rc.contour Is Nothing Then Continue For
            If rc.contour.Count < 5 Then Continue For
            Dim corners(4 - 1) As cvb.Point
            For i = 0 To corners.Count - 1
                Dim pt = rc.contour(i * rc.contour.Count / 4)
                corners(i) = New cvb.Point(rc.rect.X + pt.X, rc.rect.Y + pt.Y)
            Next
            Dim center = New cvb.Point(rc.rect.X + rc.rect.Width / 2, rc.rect.Y + rc.rect.Height / 2)
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
            cvb.Cv2.DrawContours(dst3, listOfPoints, i, colors(i), -1)
        Next
        labels(2) = CStr(colors.Count) + " triangles from " + CStr(task.redCells.Count) + " RedCloud cells"
    End Sub
End Class







Public Class Tessallate_Triangles : Inherits TaskParent
    Public basics As New Tessallate_Basics
    Public oglData As New List(Of cvb.Point3f)
    Public Sub New()
        labels = {"", "", "", ""}
        desc = "Prepare colors and triangles for use in OpenGL Triangle presentation."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        basics.Run(src)
        dst2 = basics.dst2
        dst3 = basics.dst3

        oglData.Clear()
        For i = 0 To basics.colors.Count - 1
            oglData.Add(New cvb.Point3f(basics.colors(i)(2) / 255, basics.colors(i)(1) / 255, basics.colors(i)(0) / 255)) ' BGR to RGB
            For j = 0 To 3 - 1
                oglData.Add(basics.points(i * 3 + j))
            Next
        Next

        labels = basics.labels
    End Sub
End Class








Public Class Tessallate_QuadSimple : Inherits TaskParent
    Public oglData As New List(Of cvb.Point3f)
    Public oglOptions As New Options_OpenGLFunctions
    Public Sub New()
        task.gOptions.setGridSize(20)
        desc = "Prepare to tessellate the point cloud with RedCloud data"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        oglOptions.RunOpt()
        Dim ptM = oglOptions.moveAmount
        Dim shift As New cvb.Point3f(ptM(0), ptM(1), ptM(2))

        task.redC.Run(src)
        dst2 = task.redC.dst2
        oglData.Clear()
        dst3.SetTo(0)

        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)

            Dim center = New cvb.Point(CInt(roi.X + roi.Width / 2), CInt(roi.Y + roi.Height / 2))
            Dim index = task.redMap.Get(Of Byte)(center.Y, center.X)

            If index <= 0 Then Continue For
            Dim rc = task.redCells(index)

            dst3(roi).SetTo(rc.color)
            SetTrueText(Format(rc.depthMean(2), fmt1), New cvb.Point(roi.X, roi.Y))

            Dim topLeft = getWorldCoordinates(New cvb.Point3f(roi.X, roi.Y, rc.depthMean(2)))
            Dim botRight = getWorldCoordinates(New cvb.Point3f(roi.X + roi.Width, roi.Y + roi.Height, rc.depthMean(2)))

            oglData.Add(New cvb.Point3f(rc.color(2) / 255, rc.color(1) / 255, rc.color(0) / 255))
            oglData.Add(New cvb.Point3f(topLeft.X + shift.X, topLeft.Y + shift.Y, rc.depthMean(2) + shift.Z))
            oglData.Add(New cvb.Point3f(botRight.X + shift.X, topLeft.Y + shift.Y, rc.depthMean(2) + shift.Z))
            oglData.Add(New cvb.Point3f(botRight.X + shift.X, botRight.Y + shift.Y, rc.depthMean(2) + shift.Z))
            oglData.Add(New cvb.Point3f(topLeft.X + shift.X, botRight.Y + shift.Y, rc.depthMean(2) + shift.Z))
        Next
        labels = {"", "", traceName + " completed with " + Format(oglData.Count / 5, fmt0) + " quad sets (with a 5th element for color)", "Output of Tessallate_QuadSimple"}
    End Sub
End Class






Public Class Tessallate_QuadHulls : Inherits TaskParent
    Public oglData As New List(Of cvb.Point3f)
    Public depthList As New List(Of List(Of Single))
    Public colorList As New List(Of cvb.Scalar)
    Public oglOptions As New Options_OpenGLFunctions
    Dim hulls As New RedCloud_Hulls
    Const depthListMaxCount As Integer = 10
    Public Sub New()
        task.gOptions.setGridSize(20)
        desc = "Prepare to tessellate the point cloud with RedCloud data"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        oglOptions.RunOpt()
        Dim ptM = oglOptions.moveAmount
        Dim shift As New cvb.Point3f(ptM(0), ptM(1), ptM(2))

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

            Dim center = New cvb.Point(CInt(roi.X + roi.Width / 2), CInt(roi.Y + roi.Height / 2))
            Dim index = task.redMap.Get(Of Byte)(center.Y, center.X)

            If index <= 0 Then
                depthList(i).Clear()
                colorList(i) = black
                Continue For
            End If

            Dim rc = task.redCells(index)
            If rc.depthMean(2) = 0 Then Continue For

            If colorList(i) <> rc.color Then depthList(i).Clear()

            depthList(i).Add(rc.depthMean(2))
            colorList(i) = rc.color

            If depthList(i).Count > 0 Then
                dst3(roi).SetTo(colorList(i))

                Dim depth = depthList(i).Average
                Dim topLeft = getWorldCoordinates(New cvb.Point3f(roi.X, roi.Y, depth))
                Dim botRight = getWorldCoordinates(New cvb.Point3f(roi.X + roi.Width, roi.Y + roi.Height, depth))

                oglData.Add(New cvb.Point3f(rc.color(2) / 255, rc.color(1) / 255, rc.color(0) / 255))
                oglData.Add(New cvb.Point3f(topLeft.X + shift.X, topLeft.Y + shift.Y, depth + shift.Z))
                oglData.Add(New cvb.Point3f(botRight.X + shift.X, topLeft.Y + shift.Y, depth + shift.Z))
                oglData.Add(New cvb.Point3f(botRight.X + shift.X, botRight.Y + shift.Y, depth + shift.Z))
                oglData.Add(New cvb.Point3f(topLeft.X + shift.X, botRight.Y + shift.Y, depth + shift.Z))

                If depthList(i).Count >= depthListMaxCount Then depthList(i).RemoveAt(0)
            End If
        Next
        labels(2) = traceName + " completed with " + Format(oglData.Count / 5, fmt0) + " quad sets (with a 5th element for color)"
    End Sub
End Class







Public Class Tessallate_QuadMinMax : Inherits TaskParent
    Public oglData As New List(Of cvb.Point3f)
    Public depthList1 As New List(Of List(Of Single))
    Public depthList2 As New List(Of List(Of Single))
    Public colorList As New List(Of cvb.Scalar)
    Public oglOptions As New Options_OpenGLFunctions
    Const depthListMaxCount As Integer = 10
    Public Sub New()
        task.gOptions.setGridSize(20)
        desc = "Prepare to tessellate the point cloud with RedCloud data"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.redC.Run(src)
        dst2 = task.redC.dst2

        oglOptions.RunOpt()
        Dim ptM = oglOptions.moveAmount
        Dim shift As New cvb.Point3f(ptM(0), ptM(1), ptM(2))

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

        Dim depth32f As cvb.Mat = task.pcSplit(2) * 1000, depth32s As New cvb.Mat
        depth32f.ConvertTo(depth32s, cvb.MatType.CV_32S)
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)

            Dim center = New cvb.Point(CInt(roi.X + roi.Width / 2), CInt(roi.Y + roi.Height / 2))
            Dim index = task.redMap.Get(Of Byte)(center.Y, center.X)

            If index <= 0 Then
                depthList1(i).Clear()
                depthList2(i).Clear()
                colorList(i) = black
                Continue For
            End If

            Dim rc = task.redCells(index)
            If rc.depthMean(2) = 0 Then Continue For

            If colorList(i) <> rc.color Then
                depthList1(i).Clear()
                depthList2(i).Clear()
            End If

            Dim depthMin As Single, depthMax As Single, minLoc As cvb.Point, maxLoc As cvb.Point
            depth32s(roi).MinMaxLoc(depthMin, depthMax, minLoc, maxLoc, task.depthMask(roi))
            depthMax /= 1000
            depthMin /= 1000
            If depthMax > rc.depthMean(2) + rc.depthStdev(2) * 3 Then depthMax = rc.depthMean(2) + 3 * rc.depthStdev(2)
            depthList1(i).Add(depthMin)
            depthList2(i).Add(depthMax)
            colorList(i) = rc.color

            Dim d1 = depthList1(i).Average
            Dim d2 = depthList2(i).Average

            Dim depthCount = If(d1 = d2, 1, 2)
            For j = 0 To depthCount - 1
                Dim depth = Choose(j + 1, d1, d2)
                Dim topLeft = getWorldCoordinates(New cvb.Point3f(roi.X, roi.Y, depth))
                Dim botRight = getWorldCoordinates(New cvb.Point3f(roi.X + roi.Width, roi.Y + roi.Height, depth))

                Dim color = rc.color
                dst3(roi).SetTo(color)
                oglData.Add(New cvb.Point3f(color(2) / 255, color(1) / 255, color(0) / 255))
                oglData.Add(New cvb.Point3f(topLeft.X + shift.X, topLeft.Y + shift.Y, depth + shift.Z))
                oglData.Add(New cvb.Point3f(botRight.X + shift.X, topLeft.Y + shift.Y, depth + shift.Z))
                oglData.Add(New cvb.Point3f(botRight.X + shift.X, botRight.Y + shift.Y, depth + shift.Z))
                oglData.Add(New cvb.Point3f(topLeft.X + shift.X, botRight.Y + shift.Y, depth + shift.Z))
            Next
            SetTrueText(Format(d1, fmt1) + vbCrLf + Format(d2, fmt1), New cvb.Point(roi.X, roi.Y), 3)

            If depthList1(i).Count >= depthListMaxCount Then depthList1(i).RemoveAt(0)
            If depthList2(i).Count >= depthListMaxCount Then depthList2(i).RemoveAt(0)
        Next
        labels(2) = traceName + " completed with " + Format(oglData.Count / 5, fmt0) + " quad sets (with a 5th element for color)"
    End Sub
End Class






Public Class Tessallate_Bricks : Inherits TaskParent
    Public oglData As New List(Of cvb.Point3f)
    Public depths As New List(Of Single)
    Public options As New Options_OpenGLFunctions
    Public hulls As New RedCloud_Hulls
    Dim depthMinList As New List(Of List(Of Single))
    Dim depthMaxList As New List(Of List(Of Single))
    Dim myListMax = 10
    Public Sub New()
        task.gOptions.setGridSize(20)
        desc = "Tessellate each quad in point cloud"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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
        Dim shift As New cvb.Point3f(ptM(0), ptM(1), ptM(2))

        oglData.Clear()
        hulls.Run(src)
        dst2 = hulls.dst2

        Dim min(4 - 1) As cvb.Point3f, max(4 - 1) As cvb.Point3f
        depths.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            Dim center = New cvb.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
            Dim index = task.redMap.Get(Of Byte)(center.Y, center.X)
            Dim depthMin As Single = 0, depthMax As Single = 0, minLoc As cvb.Point, maxLoc As cvb.Point
            If index >= 0 Then
                task.pcSplit(2)(roi).MinMaxLoc(depthMin, depthMax, minLoc, maxLoc, task.depthMask(roi))
                Dim rc = task.redCells(index)
                depthMin = If(depthMax > rc.depthMean(2), rc.depthMean(2), depthMin)
                Dim test = depthMin + rc.depthStdev(2) * 3
                If test < depthMax Then depthMax = test

                If depthMin > 0 And depthMax > 0 And depthMax < task.MaxZmeters Then
                    depthMinList(i).Add(depthMin)
                    depthMaxList(i).Add(depthMax)

                    depthMin = depthMinList(i).Average
                    Dim avg = depthMaxList(i).Average - depthMin
                    depthMax = depthMin + If(avg < 0.2, avg, 0.2) ' trim the max depth - often unreliable 
                    Dim color = rc.color
                    oglData.Add(New cvb.Point3f(color(2) / 255, color(1) / 255, color(0) / 255))
                    For j = 0 To 4 - 1
                        Dim x = Choose(j + 1, roi.X, roi.X + roi.Width, roi.X + roi.Width, roi.X)
                        Dim y = Choose(j + 1, roi.Y, roi.Y, roi.Y + roi.Height, roi.Y + roi.Height)
                        min(j) = getWorldCoordinates(New cvb.Point3f(x, y, depthMin))
                        max(j) = getWorldCoordinates(New cvb.Point3f(x, y, depthMax))
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

                    SetTrueText(Format(depthMin, fmt1) + vbCrLf + Format(depthMax, fmt1), New cvb.Point(roi.X, roi.Y))
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