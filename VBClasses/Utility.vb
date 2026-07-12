Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Enum causes
    lastCellFound
    indexLastGood
    indexLastBelowZero
    indexLastAboveCount
    intersectLastRectFailed
    optionsChange
    maxDistOutsideOfLastRect
    colorSync
    wGridNotInLastList
End Enum
Public Class Utility_Basics : Inherits TaskParent
    Public Sub New()
        desc = "Provide a home for some shared utility functions."
    End Sub
    Public Shared Function getFontsize() As Single
        Dim fontSize As Single
        Select Case task.workRes.Width
            Case 1920
                fontSize = 3.5
            Case 1280
                fontSize = 2.5
            Case 960
                fontSize = 1.5
            Case 672
                fontSize = 1.5
            Case 640
                fontSize = 1.5
            Case 480
                fontSize = 1.2
            Case 240
                fontSize = 1.2
            Case 336
                fontSize = 1.0
            Case 320
                fontSize = 1.0
            Case 168
                fontSize = 0.5
            Case 160
                fontSize = 1.0
        End Select
        Return fontSize
    End Function
    Public Shared Function getThickness() As Integer
        Dim fontThickness As Integer = 1
        Select Case task.workRes.Width
            Case 1920
                fontThickness = 4
            Case 1280
                fontThickness = 2
        End Select
        Return fontThickness
    End Function
    Public Shared Sub AddPlotScale(dst As Mat, minVal As Double, maxVal As Double, Optional lineCount As Integer = 3)
        Dim fontSize = getFontsize()
        Dim fontThickness = getThickness()

        Dim spacer = dst.Height / (lineCount + 1)
        Dim spaceVal = (maxVal - minVal) / (lineCount + 1)
        If lineCount > 1 Then If spaceVal < 1 Then spaceVal = 1
        For i = 0 To lineCount
            Dim p1 = New cv.Point(0, spacer * i)
            Dim p2 = New cv.Point(dst.Width, spacer * i)
            Line(dst, p1, p2, white, fontThickness)
            Dim nextVal = (maxVal - spaceVal * i)
            Dim nextText = If(maxVal > 1000, (nextVal / 1000).ToString("N2") + "k", nextVal.ToString(fmt1))
            Dim p3 = New cv.Point(0, p1.Y + 12)
            PutText(dst, nextText, p3, HersheyFonts.HersheyPlain, fontSize, white, fontThickness, task.lineType)
        Next
    End Sub
    Public Shared Function findCause(rcMap As Mat, rcList As List(Of rcDataOld)) As String
        Dim clickIndex = rcMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        findCause = ""
        If clickIndex > 0 And clickIndex < rcList.Count Then
            Dim rc = rcList(clickIndex - 1)
            Select Case rc.colorChange
                Case causes.indexLastBelowZero
                    findCause = "indexLast = 0"
                Case causes.indexLastAboveCount
                    findCause = "last index >= last rclist"
                Case causes.intersectLastRectFailed
                    findCause = "Current/Last don't intersect"
                Case causes.optionsChange
                    findCause = "task options changed"
                Case causes.maxDistOutsideOfLastRect
                    findCause = "maxDist outside last rect"
                Case causes.colorSync
                    findCause = "Resyncing Colors"
                Case causes.wGridNotInLastList
                    findCause = "wGrid cv.Point absent"
            End Select
        End If
        Return findCause
    End Function
    Public Shared Function rcDataMatch(rc As rcDataOld, rcListLast As List(Of rcDataOld), rcMapLast As Mat) As rcDataOld
        Dim r1 = rc.rect
        Dim indexLast = rcMapLast.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)

        If indexLast = 0 Then
            rc.colorChange = causes.indexLastBelowZero
        Else
            If indexLast < rcListLast.Count Then
                ' rcList index is 1 less than the rcMap value because a 0 rcMap value means not mapped.
                ' All pixels are mapped with color but withh depth, rcMap has 0's where there is no depth.
                indexLast -= 1
                Dim r2 = rcListLast(indexLast).rect
                If r1.IntersectsWith(r2) = False Then rc.colorChange = causes.intersectLastRectFailed
            Else
                rc.colorChange = causes.indexLastAboveCount
            End If
        End If

        If task.optionsChanged Then rc.colorChange = causes.optionsChange

        If rc.colorChange = causes.lastCellFound Then
            Dim lrc = rcListLast(indexLast)
            Dim rTest = rc.rect.Intersect(lrc.rect)
            Dim rTotal = rTest.Width * rTest.Height
            Dim lastTotal = lrc.rect.Width * lrc.rect.Height
            If rc.rect.Contains(lrc.maxDist) Then
                rc.maxDist = lrc.maxDist
                rc.depthDelta = Math.Abs(lrc.wcMean(2) - rc.wcMean(2))
                If Single.IsInfinity(rc.depthDelta) Or rc.depthDelta < 0 Then
                    rc.depthDelta = 0
                    rc.wcMean(2) = 0
                End If
            Else
                rc.colorChange = causes.maxDistOutsideOfLastRect
            End If

            rc.age = lrc.age + 1
            If rc.age > 1000 Then rc.age = 2

            rc.color = lrc.color
        End If
        Return rc
    End Function
    Public Shared Function rcMatch(rc As rcDataOld, rcListLast As List(Of rcDataOld),
                                           wGridLastList As List(Of Point3d),
                                           rcMapLast As Mat) As rcDataOld
        Dim r1 = rc.rect
        Dim indexLast = wGridLastList.IndexOf(rc.wGrid)
        If indexLast >= 0 And indexLast < rcListLast.Count Then
            ' rcList index is 1 less than the rcMap value because a 0 rcMap value means not mapped.
            ' All pixels are mapped with color but withh depth, rcMap has 0's where there is no depth.
            Dim r2 = rcListLast(indexLast).rect
            If r1.IntersectsWith(r2) = False Then rc.colorChange = causes.intersectLastRectFailed
        Else
            rc.colorChange = causes.wGridNotInLastList
        End If

        If task.optionsChanged Then rc.colorChange = causes.optionsChange

        If rc.colorChange <> causes.lastCellFound Then
            ' try use the maxDist cv.Point to find the last rect.
            indexLast = rcMapLast.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X) - 1
            If indexLast >= 0 And indexLast < rcListLast.Count Then
                Dim r2 = rcListLast(indexLast).rect
                If r1.IntersectsWith(r2) = False Then
                    rc.colorChange = causes.intersectLastRectFailed
                Else
                    rc.colorChange = causes.lastCellFound
                End If
            End If
        End If

        If rc.colorChange = causes.lastCellFound Then
            Dim lrc = rcListLast(indexLast)
            Dim rTest = rc.rect.Intersect(lrc.rect)
            Dim rTotal = rTest.Width * rTest.Height
            Dim lastTotal = lrc.rect.Width * lrc.rect.Height
            If rc.rect.Contains(lrc.maxDist) Then
                rc.maxDist = lrc.maxDist
                rc.depthDelta = Math.Abs(lrc.wcMean(2) - rc.wcMean(2))
                If Single.IsInfinity(rc.depthDelta) Or rc.depthDelta < 0 Then
                    rc.depthDelta = 0
                    rc.wcMean(2) = 0
                End If
            Else
                rc.colorChange = causes.maxDistOutsideOfLastRect
            End If

            rc.age = lrc.age + 1
            If rc.age > 1000 Then rc.age = 2

            rc.color = lrc.color
        End If
        Return rc
    End Function
    Public Shared Function selectCell(rcMap As Mat, rcList As List(Of rcDataOld)) As String
        Dim clickIndex As Integer = 0, outStr As String = ""
        If rcMap.Type = MatType.CV_32S Then
            clickIndex = rcMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        Else
            clickIndex = rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
        End If

        If clickIndex = 0 Then
            If rcList.Count = 0 Then rcList.Add(New rcDataOld(task.color, New cv.Rect(0, 0, task.color.Width, task.color.Height), 1))
            task.rcD = rcList(0)
        End If

        For Each rc In rcList
            If clickIndex = rc.mapID Then
                task.rcD = rc
                If task.rcD.rect.Contains(task.clickPoint) Then Exit For
            End If
        Next
        task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
        outStr = task.rcD.displayCell()
        task.clickPoint = task.rcD.maxDist

        Return outStr
    End Function
    Public Shared Function selectMinCell(rcMap As Mat, rcList As List(Of rcData)) As String
        If rcList.Count = 0 Then Return ""

        Dim outStr As String = ""
        Static clickIndex = rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
        If task.mouseClickFlag Then clickIndex = rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)

        If clickIndex = 0 Then Return "There is no cell at that location"
        If task.rcMinD Is Nothing Then task.rcMinD = rcList(0)

        For i = rcList.Count - 1 To 0 Step -1
            Dim rc = rcList(i)
            If clickIndex = rc.mapID Then
                task.rcMinD = rc
                If task.rcMinD.rect.Contains(task.clickPoint) Then
                    task.clickPoint = task.rcMinD.maxDist
                    Exit For
                End If
            End If
        Next

        If task.rcMinD Is Nothing Then task.rcMinD = rcList(0)

        If task.rcMinD.mapID <> task.gOptions.DebugSlider.Value Then Dim k = 0

        task.color(task.rcMinD.rect).SetTo(white, task.rcMinD.mask)
        outStr = task.rcMinD.displayCell()
        task.clickPoint = task.rcMinD.maxDistDepth

        Return outStr
    End Function
    Public Shared Function DelaunaySelect(rcMap As Mat, rcList As List(Of rcDataOld)) As String
        Dim outStr As String = ""
        If rcList.Count > 0 Then
            Dim clickIndex = rcMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
            If clickIndex = 0 Then
                Return vbCrLf + vbCrLf + "Click any cell to see details." + vbCrLf
            End If

            If rcList.Count = 0 Then
                rcList.Add(New rcDataOld(task.color, New cv.Rect(0, 0, task.color.Width, task.color.Height), 1))
            End If

            task.rcD = rcList(0)
            For Each rc In rcList
                If clickIndex = rc.mapID Then
                    task.rcD = rc
                    If task.rcD.rect.Contains(task.clickPoint) Then Exit For
                End If
            Next
        End If
        Return outStr
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        SetTrueText("Utility_Basics is to make some small 'Shared' utilities available.)", 3)
    End Sub
End Class
