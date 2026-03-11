Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedWGrid_Basics : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Dim currSet As New List(Of cv.Point3d)
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Identify where RedCloud world coordinates are changing"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim lastSet As New List(Of cv.Point3d)(currSet)
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
                dups.Add(Format(rc.wGrid.X, "000") + Format(rc.wGrid.Y, "000") + Format(rc.wGrid.Z, "000"),
                                rc.index - 1)
            Next

            Dim newList As New List(Of rcData)
            Dim rc1 As rcData = Nothing
            Dim rc2 As rcData = Nothing
            Dim r As cv.Rect
            dst1.SetTo(0)
            For i = 1 To dups.Count - 1
                If rc1 Is Nothing Then rc1 = redC.rcList(dups.Values(i - 1))
                rc2 = redC.rcList(dups.Values(i))

                If rc1.wGrid.X = rc2.wGrid.X And rc1.wGrid.Y = rc2.wGrid.Y And
                    Math.Abs(rc1.wcMean(2) - rc2.wcMean(2)) < 1.0 Then
                    r = rc1.rect.Union(rc2.rect)
                    dst1(r).SetTo(0)
                    dst1(rc1.rect).SetTo(255, rc1.mask)
                    dst1(rc2.rect).SetTo(255, rc2.mask)
                    rc1.rect = r
                    rc1.mask = dst1(r).Clone
                    ' take the values of depthdelta and wcmean from the larger of the 2 rcData's
                    If rc1.pixels < rc2.pixels Then
                        rc1.depthDelta = rc2.depthDelta
                        rc1.wcMean = rc2.wcMean
                    End If
                    rc1.multiMask = True
                    If rc1.wGrid.X = -2 And rc1.wGrid.Y = 0 Then Dim k = 0
                Else
                    If rc1.multiMask Then
                        rc1.contour = Nothing
                        rc1.hull = Nothing
                        rc1.pixels = rc1.mask.CountNonZero
                    End If
                    newList.Add(rc1)
                    rc1 = Nothing
                End If
            Next

            If rc1 IsNot Nothing Then
                rc1.contour = Nothing
                rc1.hull = Nothing
                rc1.pixels = rc1.mask.CountNonZero
                newList.Add(rc1)
            Else
                newList.Add(rc2)
            End If

            rcList.Clear()
            rcMap.SetTo(0)
            dst2.SetTo(0)
            Dim count As Integer
            For Each rc In newList
                If rc.multiMask Then count += 1
                rc.index = rcList.Count + 1
                rcMap(rc.rect).SetTo(rc.index, rc.mask)
                rcList.Add(rc)
                dst2(rc.rect).SetTo(rc.color, rc.mask)

                If task.gOptions.DebugCheckBox.Checked And rc.multiMask Then
                    dst2(rc.rect).SetTo(task.highlight, rc.mask)
                End If
            Next

            If standaloneTest() Then
                strOut = RedUtil_Basics.selectCell(rcMap, rcList)
                SetTrueText(strOut, 3)
            End If

            labels(2) = CStr(rcList.Count) + " cells remain after merging masks for " + CStr(count) + " wGrid points."
            labels(3) = CStr(count) + " multi-mask cells found"
        End Sub
    End Class
End Namespace