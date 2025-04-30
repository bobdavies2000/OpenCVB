Imports cv = OpenCvSharp
Public Class Tour_Basics : Inherits TaskParent
    Public contours As New Contour_Basics
    Public Sub New()
        task.tourMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        optiBase.FindSlider("Max contours").Value = 10
        desc = "Create the tourList and tourMap from Contour_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contours.Run(src)
        labels(2) = contours.labels(2)

        task.tourList.Clear()
        task.tourList.Add(New tourData) ' placeholder for zero which is the background
        task.tourMap.SetTo(0)
        For Each tour In contours.tourlist
            Dim td = New tourData
            td.index = task.tourList.Count
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
            task.tourMap(td.rect).SetTo(td.index, td.mask)

            td.ptMax = GetMaxDist(td.mask)
            td.maxDist = New cv.Point(td.rect.X + td.ptMax.X, td.rect.Y + td.ptMax.Y)
            td.maxDStable = td.maxDist
            task.tourList.Add(td)
        Next

        dst2 = ShowPalette(task.tourMap * 255 / task.tourList.Count)
        Dim tIndex = task.tourMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
        task.color(task.tourList(tIndex).rect).SetTo(white, task.tourList(tIndex).mask)
        task.color.Circle(task.tourList(tIndex).maxDist, task.DotSize, black, -1)
    End Sub
End Class








'Public Class Tour_BasicsNot : Inherits TaskParent
'    Public core As New Tour_Core
'    Public Sub New()
'        task.tourMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
'        desc = "Track each contour using tourMap and tourList."
'    End Sub
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        core.Run(src)
'        Dim lastMap = task.tourMap.Clone

'        task.tourList.Clear()
'        task.tourList.Add(New tourData)
'        task.tourMap.SetTo(0)
'        Dim usedColors As New List(Of Byte)
'        For Each td In core.tourList
'            task.tourList.Add(td)
'            Dim index = lastMap.Get(Of Byte)(td.maxDist.Y, td.maxDist.X)
'            If usedColors.Contains(index) Or index = 0 Then index = core.tourMap.Get(Of Byte)(td.maxDist.Y, td.maxDist.X)
'            usedColors.Add(index)
'            task.tourMap(td.rect).SetTo(index, td.mask)
'        Next

'        dst2 = ShowPalette(task.tourMap * 255 / core.tourList.Count)
'        If task.heartBeat Then labels(2) = "Originally detected " + CStr(core.tourList.Count) + " contours.  " + CStr(task.tourList.Count) + " after filtering out overlaps."
'    End Sub
'End Class








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
