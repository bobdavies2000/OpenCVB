Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedUtil_Basics : Inherits TaskParent
        Public Sub New()
            desc = "Provide a home for some shared utility functions for the RedCloud algorithms."
        End Sub
        Public Shared Function rcDataMatch(rc As rcData, rcListLast As List(Of rcData),
                                           rcMapLast As cv.Mat) As rcData
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
        Public Shared Function rcMatch(rc As rcData, rcListLast As List(Of rcData),
                                       wGridLastList As List(Of cv.Point),
                                       rcMapLast As cv.Mat) As rcData
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
                ' try use the maxDist point to find the last rect.
                indexLast = rcMapLast.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
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
        Public Shared Function selectCell(rcMap As cv.Mat, rcList As List(Of rcData)) As String
            Dim clickIndex As Integer = 0, strOut As String = ""
            If rcList.Count > 0 Then
                clickIndex = rcMap.Get(Of Integer)(Task.clickPoint.Y, Task.clickPoint.X)
                If clickIndex > 0 And clickIndex < rcList.Count Then
                    Task.rcD = rcList(clickIndex - 1)
                    If Task.rcD.pixels >= 10 Then
                        strOut = Task.rcD.displayCell()
                        Task.color(Task.rcD.rect).SetTo(white, Task.rcD.mask)
                        Task.color.Rectangle(Task.rcD.rect, Task.highlight, Task.lineWidth)
                    Else
                        strOut = "RedCloud cell is too small to identify."
                        Dim gridIndex = Task.gridMap.Get(Of Integer)(Task.clickPoint.Y, Task.clickPoint.X)
                        Task.color(Task.gridRects(gridIndex)).SetTo(white)
                        Task.color.Rectangle(Task.gridRects(gridIndex), Task.highlight, Task.lineWidth)
                    End If
                Else
                    If Task.rcD IsNot Nothing Then Task.rcD = Nothing
                End If
            End If
            Return strOut
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then dst2 = runRedCloud(src, labels(2))

            selectCell(Task.redCloud.rcMap, Task.redCloud.rcList)
            SetTrueText(strOut, 3)
        End Sub
    End Class
End Namespace