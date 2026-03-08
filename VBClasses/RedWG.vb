Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedWG_Basics : Inherits TaskParent
        Dim redC As New RedCloud_Flood_CPP
        Dim currSet As New List(Of cv.Point)
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Identify where RedCloud world coordinates are changing"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim lastSet As New List(Of cv.Point)(currSet)
            dst2.SetTo(0)
            Static count As Integer
            If task.heartBeatLT Or task.frameCount = 2 Then
                dst3.SetTo(0)
                count = 0
            End If
            currSet.Clear()
            For Each rc In redC.rcList
                currSet.Add(rc.wGrid)
                If lastSet.Contains(rc.wGrid) Then
                    dst2(rc.rect).SetTo(rc.color, rc.mask)
                Else
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                    count += 1
                End If
            Next

            strOut = RedUtil_Basics.selectCell(redC.rcMap, redC.rcList)
            SetTrueText(strOut, 1)

            labels(3) = CStr(count) + " cells were not matched using wc since the last heartbeatLT"
        End Sub
    End Class





    Public Class RedWG_ValidateRows : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Public Sub New()
            desc = "Validate how consistent the world grid entries are."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim ptY As New List(Of Integer)
            For Each rc In redC.rcList
                ptY.Add(rc.wGrid.Y)
            Next

            Static row As Integer = ptY.Min
            If ptY.Count = 0 Then
                SetTrueText("There are no cells available" + vbCrLf + "Increase the reduction factor.")
                Exit Sub
            End If

            For Each rc In redC.rcList
                If rc.wGrid.Y = row Then dst2(rc.rect).SetTo(white, rc.mask)
            Next

            If task.heartBeat Or row < ptY.Min Then row += 1
            SetTrueText("World Grid Row " + CStr(row) + " highlighted", 3)
            If row >= ptY.Max Then row = ptY.Min
        End Sub
    End Class





    Public Class RedWG_ValidateCols : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Public rcList As New List(Of rcData)
        Public column As Integer
        Public Sub New()
            desc = "Validate how consistent the world grid entries are."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src IsNot Nothing Then
                redC.Run(src)
                dst2 = redC.dst2
                labels(2) = redC.labels(2)
                rcList = redC.rcList
            End If

            Dim ptX As New List(Of Integer)
            For Each rc In rcList
                ptX.Add(rc.wGrid.X)
            Next

            If ptX.Count = 0 Then
                SetTrueText("There are no cells available" + vbCrLf + "Increase the reduction factor.")
                Exit Sub
            End If

            For Each rc In rcList
                If rc.wGrid.X = column Then dst2(rc.rect).SetTo(white, rc.mask)
            Next

            If task.heartBeat Then column += 1
            strOut = "World Grid Col " + CStr(column) + " highlighted"
            SetTrueText(strOut, 3)
            If column >= ptX.Max Then column = ptX.Min
        End Sub
    End Class





    Public Class RedWG_Duplicates : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Public rcList As New List(Of rcData)
        Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        Dim validC As New RedWG_ValidateCols
        Public Sub New()
            validC.column = 0
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Consolidate duplicate world grid coordinates."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim dups As New SortedList(Of String, Integer)(New compareAllowIdenticalString)
            For Each rc In redC.rcList
                dups.Add(Format(rc.wGrid.X, "000") + Format(rc.wGrid.Y, "000"), rc.index - 1)
            Next

            Dim count As Integer
            Dim newList As New List(Of rcData)
            Dim rc1 As rcData = Nothing, rc2 As rcData
            Dim r As cv.Rect
            For i = 1 To dups.Count - 1
                If dups.Keys(i - 1) = dups.Keys(i) Then
                    If rc1 Is Nothing Then rc1 = redC.rcList(dups.Values(i - 1))
                    rc2 = redC.rcList(dups.Values(i))
                    r = rc1.rect.Union(rc2.rect)
                    dst1(r).SetTo(0)
                    dst1(rc1.rect).SetTo(255, rc1.mask)
                    dst1(rc2.rect).SetTo(255, rc2.mask)
                    count += 1
                Else
                    If rc1 Is Nothing Then
                        newList.Add(redC.rcList(dups.Values(i - 1)))
                    Else
                        rc1.rect = r
                        rc1.mask = dst1(r)
                        newList.Add(rc1)
                        rc1 = Nothing
                    End If
                End If
            Next

            rcList = New List(Of rcData)(newList)
            rcMap.SetTo(0)
            For Each rc In rcList
                rcMap(rc.rect).SetTo(rc.color, rc.mask)
            Next

            validC.rcList = rcList
            validC.dst2 = dst2
            validC.Run(Nothing)
            SetTrueText(validC.strOut, 3)
            labels(3) = CStr(count) + " duplicate world grid coordinates found"
        End Sub
    End Class

End Namespace