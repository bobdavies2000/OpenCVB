Imports cv = OpenCvSharp
Public Class Tour_Basics : Inherits TaskParent
    Public contours As New Contour_Basics
    Dim tour As New Tour_Core
    Public Sub New()
        task.tourMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        optiBase.FindSlider("Max contours").Value = 10
        desc = "Create the task.tourList and task.tourMap from Contour_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        tour.Run(src)
        labels = tour.labels

        task.tourList.Clear()
        task.tourList.Add(New tourData)
        task.tourMap.SetTo(0)
        For Each td In tour.tourList
            td.index = task.tourList.Count
            td.maxDist = GetMaxDist(td.mask, td.rect)
            task.tourList.Add(td)
            task.tourMap(td.rect).SetTo(td.index, td.mask)
        Next

        dst2 = ShowPalette(task.tourMap)

        Static ptTour = task.ClickPoint
        If task.mouseClickFlag Then ptTour = task.ClickPoint
        Dim index = task.tourMap.Get(Of Byte)(ptTour.Y, ptTour.X)
        task.tourD = task.tourList(index)

        task.color(task.tourD.rect).SetTo(white, task.tourD.mask)
    End Sub
End Class






Public Class Tour_Core : Inherits TaskParent
    Public contours As New Contour_Basics
    Public tourList As New List(Of tourData)
    Public Sub New()
        optiBase.FindSlider("Max contours").Value = 10
        desc = "Create only the tourList from Contour_Basics but without a placeholder for zero."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contours.Run(src)
        dst2 = contours.dst2
        labels(2) = contours.labels(2)

        tourList.Clear()
        For Each tour In contours.contourList
            Dim td = New tourData
            td.index = tourList.Count
            td.contour = New List(Of cv.Point)(tour)
            td.pixels = contours.areaList(td.index)

            Dim minX As Single = td.contour.Min(Function(p) p.X)
            Dim maxX As Single = td.contour.Max(Function(p) p.X)
            Dim minY As Single = td.contour.Min(Function(p) p.Y)
            Dim maxY As Single = td.contour.Max(Function(p) p.Y)

            td.rect = New cv.Rect(minX, minY, maxX - minX, maxY - minY)
            If td.rect.Width = 0 Or td.rect.Height = 0 Then Continue For

            td.mask = contours.dst0(td.rect).Clone
            td.mask = td.mask.InRange(td.index + 1, td.index + 1)

            td.maxDist = GetMaxDist(td.mask, td.rect)

            tourList.Add(td)

            If standalone Then
                dst2.Circle(td.maxDist, task.DotSize + 3, task.highlight, -1)
                dst2.Rectangle(td.rect, task.highlight, task.lineWidth)
            End If
        Next
    End Sub
End Class







Public Class Tour_Features : Inherits TaskParent
    Dim feat As New Feature_Basics
    Dim core As New Tour_Basics
    Public Sub New()
        desc = "Show contours and features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(src)

        core.Run(src)
        dst2 = core.dst2
        labels(2) = core.labels(2)

        For Each pt In task.features
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
        Next
    End Sub
End Class







Public Class Tour_Bricks : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Basics
    Dim core As New Tour_Basics
    Public Sub New()
        desc = "Show contours and Brick points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBrick.Run(task.grayStable)

        core.Run(src)
        dst2 = core.dst2
        labels(2) = core.labels(2)

        For Each pt In ptBrick.intensityFeatures
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
        Next
    End Sub
End Class







Public Class Tour_Info : Inherits TaskParent
    Public index As Integer
    Public td As tourData
    Public Sub New()
        desc = "Provide details about the selected contour's tourList entry."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Static tour As New Tour_Basics
            tour.Run(src)
            dst2 = ShowAddweighted(tour.dst2, task.fcsBasics.dst2, labels(0))
            labels(2) = task.fcsBasics.labels(2)
        End If

        index = task.fcsMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X)
        td = task.tourList(index)

        strOut = vbCrLf + vbCrLf
        strOut += "Index = " + CStr(td.index) + vbCrLf
        strOut += "Number of pixels in the mask: " + CStr(td.pixels) + vbCrLf
        strOut += "Number of points in the contour: " + CStr(td.contour.Count) + vbCrLf
        strOut += td.maxDist.ToString + vbCrLf
        dst2.Rectangle(td.rect, task.highlight, task.lineWidth)
        dst2.Circle(td.maxDist, task.DotSize, black, -1)

        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Tour_Lines : Inherits TaskParent
    Dim core As New Tour_Basics
    Public Sub New()
        desc = "Identify contour by its Lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        core.Run(src)
        dst2 = core.dst2
        labels(2) = core.labels(2)

        For Each lp In task.lpList
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next
    End Sub
End Class








Public Class Tour_Delaunay : Inherits TaskParent
    Dim core As New Tour_Basics
    Dim delaunay As New Delaunay_Basics
    Public Sub New()
        desc = "Use Delaunay to track maxDist point."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        core.Run(src)
        labels(2) = core.labels(2)

        Dim ptSorted As New SortedList(Of Integer, cv.Point2f)(New compareAllowIdenticalInteger)
        For Each td In task.tourList
            ptSorted.Add(td.index, td.maxDist)
        Next

        delaunay.inputPoints = New List(Of cv.Point2f)(ptSorted.Values)
        delaunay.Run(emptyMat)
        dst2 = delaunay.dst2.Clone

        For Each td In task.tourList
            dst2.Circle(td.maxDist, task.DotSize, task.highlight, -1)
        Next

        dst3 = ShowPalette(task.tourMap)
    End Sub
End Class