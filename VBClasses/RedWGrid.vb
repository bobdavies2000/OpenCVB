Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedWGrid_Basics : Inherits TaskParent
        Dim redC As New RedCloud_Basics
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

            labels(3) = CStr(count) + " unstable cells = not matched since the last heartbeatLT"
        End Sub
    End Class





    Public Class RedWGrid_Click : Inherits TaskParent
        Dim dups As New RedWGrid_Duplicates
        Dim options As New Options_WGrid
        Public Sub New()
            desc = "Click on any RedCloud cell to see similar cells connected by the wGrid point."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dups.Run(src)
            dst2 = dups.dst2
            labels(2) = dups.labels(2)

            strOut = RedUtil_Basics.selectCell(dups.rcMap, dups.rcList)
            If task.rcD Is Nothing Then
                SetTrueText("Click on any cell present in dst2", 3)
                Exit Sub
            End If

            SetTrueText(strOut, 3)

            Select Case options.clickName
                Case "Identify Row"
                    Dim row = task.rcD.wGrid.Y
                    For Each rc In dups.redC.rcList
                        If rc.wGrid.Y = row Then
                            dst2(rc.rect).SetTo(white, rc.mask)
                        End If
                    Next
                    labels(3) = "Row " + CStr(row) + " selected"
                Case "Identify Col"
                    Dim col = task.rcD.wGrid.X
                    For Each rc In dups.redC.rcList
                        If rc.wGrid.X = col Then
                            dst2(rc.rect).SetTo(white, rc.mask)
                        End If
                    Next
                    labels(3) = "Col " + CStr(col) + " selected"
                Case "Identify Neighbors"
                    Dim row = task.rcD.wGrid.Y
                    Dim col = task.rcD.wGrid.X
                    For Each rc In dups.redC.rcList
                        If Math.Abs(task.rcD.wGrid.X - rc.wGrid.X) <= 1 And
                           Math.Abs(task.rcD.wGrid.Y - rc.wGrid.Y) <= 1 Then
                            dst2(rc.rect).SetTo(white, rc.mask)
                        End If
                    Next
                Case "Identify Multi-Mask Cells"
                    For Each rc In dups.redC.rcList
                        If rc.multiMask Then dst2(rc.rect).SetTo(white, rc.mask)
                    Next
            End Select

        End Sub
    End Class





    Public Class RedWGrid_Duplicates : Inherits TaskParent
        Public redC As New RedCloud_Basics
        Public rcList As New List(Of rcData)
        Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Consolidate duplicate world grid coordinates."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            labels(2) = redC.labels(2)

            Dim dups As New SortedList(Of String, Integer)(New compareAllowIdenticalString)
            For Each rc In redC.rcList
                dups.Add(Format(rc.wGrid.X, "000") + Format(rc.wGrid.Y, "000"), rc.index - 1)
            Next

            Dim count As Integer
            Dim newList As New List(Of rcData)
            Dim rc1 As rcData = redC.rcList(dups.Values(0))
            Dim rc2 As rcData = redC.rcList(dups.Values(1))
            Dim r = rc1.rect
            dst1.SetTo(0)
            dst1(r).SetTo(255, rc1.mask)
            For i = 1 To dups.Count - 1
                If dups.Keys(i - 1) = dups.Keys(i) Then
                    rc2 = redC.rcList(dups.Values(i))
                    r = rc1.rect.Union(rc2.rect)
                    dst1(r).SetTo(0)
                    dst1(rc1.rect).SetTo(255, rc1.mask)
                    dst1(rc2.rect).SetTo(255, rc2.mask)
                    rc1.rect = r
                    rc1.multiMask = True
                    rc1.mask = dst1(r).Clone
                    count += 1
                Else
                    rc1.rect = r
                    rc1.mask = dst1(r).Clone
                    newList.Add(rc1)
                    rc1 = redC.rcList(dups.Values(i))
                End If
            Next

            rcList.Clear()
            rcMap.SetTo(0)
            dst2.SetTo(0)
            For Each rc In newList
                If rc.multiMask Then
                    rc = New rcData(rc.mask, rc.rect, rcList.Count + 1, rc.multiMask)
                End If
                rc.index = rcList.Count + 1
                rcMap(rc.rect).SetTo(rc.index, rc.mask)
                rcList.Add(rc)
                dst2(rc.rect).SetTo(rc.color, rc.mask)
            Next

            strOut = RedUtil_Basics.selectCell(rcMap, rcList)
            SetTrueText(strOut, 3)

            labels(2) = CStr(rcList.Count) + " cells remain after removing " + CStr(count) + " duplicate wGrid points."
            labels(3) = CStr(count) + " duplicate world grid coordinates found"
        End Sub
    End Class
End Namespace