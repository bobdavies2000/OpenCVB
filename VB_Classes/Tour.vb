Imports cv = OpenCvSharp
Public Class Tour_Basics : Inherits TaskParent
    Public core As New Tour_Core
    Public Sub New()
        desc = "Track each contour using tourMap and tourList."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        core.Run(src)
        dst2 = core.dst2
        dst3 = core.dst3
        labels = core.labels
    End Sub
End Class






Public Class Tour_Core : Inherits TaskParent
    Public contours As New Contour_Basics
    Public tourMap = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
    Public tourList As New List(Of tourData)
    Public Sub New()
        labels(3) = "ColorMap - similar to tourMap."
        desc = "Create the tourList and tourMap from FeatureLess_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contours.Run(src)
        labels(2) = contours.labels(2)

        tourList.Clear()
        tourList.Add(New tourData) ' placeholder for zero which is the background
        tourMap.SetTo(0)
        For Each tour In contours.tourlist
            Dim td = New tourData
            td.index = tourList.Count
            td.contour = New List(Of cv.Point)(tour)
            td.pixels = contours.areaList(td.index - 1)

            Dim minX As Single = td.contour.Min(Function(p) p.X)
            Dim maxX As Single = td.contour.Max(Function(p) p.X)
            Dim minY As Single = td.contour.Min(Function(p) p.Y)
            Dim maxY As Single = td.contour.Max(Function(p) p.Y)

            td.rect = New cv.Rect(minX, minY, maxX - minX, maxY - minY)
            If td.rect.Width = 0 Or td.rect.Height = 0 Then Continue For

            td.mask = contours.dst0(td.rect).Clone
            td.mask = td.mask.InRange(td.index, td.index)
            tourMap(td.rect).SetTo(td.index, td.mask)

            td.maxDist = GetMaxDist(td.mask, td.rect)
            td.maxDStable = td.maxDist
            tourList.Add(td)
        Next

        dst2 = ShowPalette((tourMap * 255 / tourList.Count).ToMat)
        Dim tIndex = tourMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
        task.color(tourList(tIndex).rect).SetTo(white, tourList(tIndex).mask)
        task.color.Circle(tourList(tIndex).maxDist, task.DotSize, black, -1)
    End Sub
End Class
