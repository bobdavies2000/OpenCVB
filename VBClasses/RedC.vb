Imports OpenCvSharp
Imports OpenCvSharp.Cv2
Imports cv = OpenCvSharp
Public Class RedC_Basics : Inherits TaskParent
    Dim color8u As New Color8U_Basics
    Public rcMap As Mat = New Mat(dst2.Size, MatType.CV_8U, 0)
    Dim rcIndexMap As Mat = New Mat(dst2.Size, MatType.CV_32S, 0)
    Public rcList As New List(Of rcData) ' includes cloud data.
    Public rcListLast As New List(Of rcData)
    Public Sub New()
        desc = "FloodFill each color8U output and create an rclist"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim rcMapLast As cv.Mat = rcMap.Clone
        Dim rcIndexMapLast As cv.Mat = rcIndexMap.Clone
        rcListLast = New List(Of rcData)(rcList)

        If task.optionsChanged Then
            rcMapLast.SetTo(0)
            rcIndexMap.SetTo(0)
            rcListLast.Clear()
        End If

        color8u.Run(task.gray)

        rcMap = color8u.dst2.Clone + 1
        Dim rect As cv.Rect
        Dim mask As Mat = New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
        Dim floodMap = rcMap.Clone
        Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        sortList.Add(0, New rcData)
        For y = 0 To floodMap.Height - 1
            For x = 0 To floodMap.Width - 1
                If mask.Get(Of Byte)(y, x) = 0 Then
                    Dim index As Integer = sortList.Count
                    Dim mapID As Integer = rcMap.Get(Of Byte)(y, x)
                    Dim flags = FloodFillFlags.FixedRange Or (index << 8) ' Or FloodFillFlags.MaskOnly
                    Dim count = FloodFill(floodMap, mask, New cv.Point(x, y), index, rect, 0, 0, flags)
                    If count > 1 Then
                        Dim rc = New rcData(floodMap(rect), rect, index)
                        rc.mapID = mapID
                        sortList.Add(rc.pixels, rc)
                    End If
                End If
            Next
        Next
        dst2 = Palettize(rcMap, 0)

        Dim rcIndex As Integer
        rcIndexMap.SetTo(0)
        rcList.Clear()
        Dim flag = FloodFillFlags.FixedRange Or (255 << 8)
        For Each rc In sortList.Values
            rc.index = rcList.Count
            rcIndexMap(rc.rect).SetTo(rc.index, rc.mask)

            rc.maxDist = rc.buildMaxDist(rc.mask)
            rc.pixels = CountNonZero(rc.mask)
            If rc.pixels > 0 Then
                rcIndex += 1
                rcList.Add(rc)
            End If
        Next

        For Each rc In rcList
            Dim mapIDCurr = rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            Dim mapIDLast = rcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            rc.indexLast = rcIndexMapLast.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)

            If rc.indexLast < rcListLast.Count Then
                rc.maxDStable = If(mapIDCurr = mapIDLast, rcListLast(rc.indexLast).maxDStable, rc.maxDist)
                Dim color = dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                Dim colorLast = dst2.Get(Of cv.Vec3b)(rc.maxDStable.Y, rc.maxDStable.X)
                If color <> colorLast Then rc.maxDStable = rc.maxDist
            Else
                If task.firstPass Then rc.maxDStable = rc.maxDist
            End If
        Next

        strOut = Utility_Basics.selectMinCell(rcIndexMap, rcMap, rcList)
        SetTrueText(strOut, 3)

        If task.rcMinD IsNot Nothing And standaloneTest() Then Rectangle(dst2, task.rcMinD.rect, task.highlight, task.lineWidth)

        If task.heartBeat Then labels(2) = CStr(rcList.Count) + " RedColor cells were found."
    End Sub
End Class






Public Class XR_RedC_Sizes : Inherits TaskParent
    Dim redC As New RedC_Basics
    Public Sub New()
        If standalone Then task.gOptions.DebugSlider.Value = 32
        desc = "Use the debug slider to display cells of X pixels or less."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        If task.heartBeat Then dst3.SetTo(0)
        Dim count As Integer
        For Each rc In redC.rcList
            If rc.pixels <= task.gOptions.DebugSlider.Value Then
                Dim vec = dst2.Get(Of Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                dst3(rc.rect).SetTo(vec, rc.mask)
                count += 1
            End If
        Next

        labels(3) = CStr(count) + " cells smaller than " + CStr(task.gOptions.DebugSlider.Value) + " pixels."
    End Sub
End Class






Public Class XR_RedC_MaxDStable : Inherits TaskParent
    Dim redC As New RedC_Basics
    Public Sub New()
        dst1 = New Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Any flickering means that the maxDStable point was not the same as it was for the previous frame."
        desc = "Find all the cells with a MaxDStable that was exactly the same in the previous frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst1.SetTo(0)
        If redC.rcListLast.Count > 0 Then
            For Each rc In redC.rcList
                If rc.maxDStable = redC.rcListLast(rc.indexLast).maxDStable Then
                    dst1(rc.rect).SetTo(rc.mapID, rc.mask)
                End If
            Next

            dst3 = Palettize(dst1, 0)
        End If
    End Sub
End Class






Public Class RedC_TrackCell : Inherits TaskParent
    Dim redC As New RedC_Basics
    Public Sub New()
        task.gOptions.displayDst1.Checked = True
        task.gOptions.DebugSlider.Minimum = 0
        desc = "Track the selected cell even after maxDStable goes beyond the edge of the cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Static saveCell As rcData
        If task.mouseClickFlag Then
            strOut = ""
            saveCell = task.rcMinD
            If task.rcMinD.maxDStable = newPoint Then task.rcMinD.maxDStable = task.rcMinD.maxDist
        End If

        Dim stablePoints As New List(Of cv.Point)
        For Each rc In redC.rcList
            stablePoints.Add(rc.maxDStable)
        Next

        dst3.SetTo(0)
        Dim index = stablePoints.IndexOf(saveCell.maxDStable)
        If index >= 0 Then
            If strOut.Length < 200 Then strOut += "Cell was found using MaxDStable..." + vbCrLf
            Dim rc = redC.rcList(index)
            dst3(rc.rect).SetTo(task.scalarColors(rc.mapID), rc.mask)
            Circle(dst3, rc.maxDStable, task.DotSize + 1, task.highlight, -1)
            If saveCell.mapID <> rc.mapID Then Dim k = 0
            saveCell = rc
        Else
            'strOut = ""
            'For i = Math.Max(saveCell.index - 2, 0) To Math.Min(redC.rcList.Count - 1, saveCell.index + 2)
            '    Dim rc = redC.rcList(i)
            '    If rc.mapID = saveCell.mapID And rc.rect.IntersectsWith(saveCell.rect) Then
            '        saveCell = rc
            '        dst3(rc.rect).SetTo(task.scalarColors(rc.mapID), rc.mask)
            '        Circle(dst3, rc.maxDStable, task.DotSize + 1, task.highlight, -1)
            '        strOut = "Cell was reacquired using the relative size and mapID." + vbCrLf
            '        Exit For
            '    End If
            'Next
        End If

        Dim tmp As New cv.Mat
        CvtColor(dst3, tmp, cv.ColorConversionCodes.BGR2GRAY)
        If CountNonZero(tmp) = 0 Then strOut = "Select a cell to track it"
        SetTrueText(strOut, 1)
    End Sub
End Class
