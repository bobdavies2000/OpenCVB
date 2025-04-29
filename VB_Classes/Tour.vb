Imports cv = OpenCvSharp
Public Class Tour_Basics : Inherits TaskParent
    Public core As New Tour_Core
    Public Sub New()
        task.tourMap = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        desc = "Track each contour using tourMap and tourList."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        core.Run(src)
        dst2 = core.dst2

        task.tourList.Clear()
        task.tourList.Add(New tourData)
        Dim colorMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        For Each td In core.tourList
            Dim index = colorMap.Get(Of Byte)(td.maxDist.Y, td.maxDist.X)
            If index <> 0 Then Continue For
            colorMap(td.rect).SetTo(td.index, td.mask)
            core.tourMap(td.rect).copyto(colorMap(td.rect), td.mask)
            DrawContour(colorMap, td.contour.ToList, td.index, task.lineWidth + 5) ' Why +2?  To fill in interior lines - change to see the impact.
            td.index = task.tourList.Count
            task.tourList.Add(td)
            task.tourMap(td.rect).SetTo(td.index, td.mask)
        Next

        dst2 = ShowPalette(colorMap * 255 / core.tourList.Count)

        dst3 = ShowPalette(colorMap * 255 / task.tourList.Count)
        If task.heartBeat Then labels(2) = "Originally detected " + CStr(core.tourList.Count) + " contours.  " + CStr(task.tourList.Count) + " after filtering out overlaps."
    End Sub
End Class






Public Class Tour_Core : Inherits TaskParent
    Public contours As New Contour_Basics
    Public tourMap = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
    Public tourList As New List(Of tourData)
    Public Sub New()
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

            td.ptMax = GetMaxDist(td.mask)
            td.maxDist = New cv.Point(td.rect.X + td.ptMax.X, td.rect.Y + td.ptMax.Y)
            td.maxDStable = td.maxDist
            tourList.Add(td)
        Next

        dst2 = ShowPalette((tourMap * 255 / tourList.Count).ToMat)
        Dim tIndex = tourMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
        task.color(tourList(tIndex).rect).SetTo(white, tourList(tIndex).mask)
        task.color.Circle(tourList(tIndex).maxDist, task.DotSize, black, -1)
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
            Static core As New Tour_Basics
            core.Run(src)
            dst2 = core.dst2
            labels(2) = core.labels(2)
        End If
        index = task.tourMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
        td = task.tourList(index)

        strOut = vbCrLf + vbCrLf
        strOut += "Index = " + CStr(td.index) + vbCrLf
        strOut += "Number of pixels in the mask: " + CStr(td.pixels) + vbCrLf
        strOut += "Number of points in the contour: " + CStr(td.contour.Count) + vbCrLf
        strOut += td.maxDist.ToString + vbCrLf
        strOut += "Age " + CStr(td.age) + vbCrLf
        dst2.Rectangle(td.rect, task.highlight, task.lineWidth)
        dst2.Circle(td.maxDist, task.DotSize, black, -1)

        SetTrueText(strOut, 3)
    End Sub
End Class
