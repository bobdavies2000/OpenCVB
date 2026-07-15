Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class RedC_Basics : Inherits TaskParent
    Dim color8u As New Color8U_Basics
    Public rcMap As Mat = New Mat(dst2.Size, MatType.CV_8U, 0)
    Public rcMapIndex As Mat = New Mat(dst2.Size, MatType.CV_32S, 0)
    Public rcList As New List(Of rcData) ' includes cloud data.
    Dim stablePoints As New List(Of cv.Point)
    Public Sub New()
        desc = "FloodFill each color8U output and create an rclist"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim rcMapLast As cv.Mat = rcMap.Clone
        Dim rcMapIndexLast As cv.Mat = rcMapIndex.Clone
        Dim rcListLast As New List(Of rcData)(rcList)
        Dim stablePointsLast As New List(Of cv.Point)(stablePoints)

        If task.optionsChanged Then
            rcMapLast.SetTo(0)
            rcMapIndex.SetTo(0)
            rcListLast.Clear()
        End If

        color8u.Run(task.gray)

        rcMap = color8u.dst2.Clone + 1
        Dim minList As New List(Of rcData)
        minList.Add(New rcData) ' placeholder for 0
        Dim rect As cv.Rect
        Dim mask As Mat = New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
        For Each r In task.gridRects
            If mask(r).Get(Of Byte)(0, 0) = 0 Then
                Dim mapID As Integer = rcMap(r).Get(Of Byte)(0, 0)
                Dim index As Integer = minList.Count
                Dim flags = FloodFillFlags.FixedRange Or FloodFillFlags.MaskOnly Or (255 << 8)
                Dim count = FloodFill(rcMap, mask, r.TopLeft, index, rect, 0, 0, flags)
                If count > 0 Then
                    ' minList.Add(New rcData(rcMap(rect), mask(rect), rect, mapID))
                    minList.Add(New rcData(rcMap(rect), rect, mapID))
                End If
            End If
        Next

        Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To minList.Count - 1
            Dim rc = minList(i)
            rc.maxDist = rc.buildMaxDist(rc.mask)

            rc.depth = Mean(task.pcSplit(2)(rc.rect), rc.mask)
            rc.maskDepth = rc.mask.Clone
            rc.maskDepth.SetTo(0, task.noDepthMask(rc.rect))
            rc.pixelsDepth = CountNonZero(rc.maskDepth)

            sortList.Add(rc.pixels, rc)
        Next

        dst2 = Palettize(rcMap, 0)
        cv.Cv2.ImShow("Full Mask", mask)

        rcList = New List(Of rcData)(sortList.Values)
        Dim rcIndex As Integer
        rcMapIndex.SetTo(0)
        For Each rc In rcList
            rc.index = rcIndex
            rcMapIndex(rc.rect).SetTo(rc.index, rc.mask)

            If rc.index = 0 Then cv.Cv2.ImShow("Mask", rc.mask)
            rcIndex += 1
        Next

        For Each rc In rcList
            Dim mapIDCurr = rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            Dim mapIDLast = rcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            Dim indexLast = rcMapIndexLast.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)

            If indexLast < rcListLast.Count Then
                rc.maxDStable = If(mapIDCurr = mapIDLast, rcListLast(indexLast).maxDStable, rc.maxDist)
                Dim color = dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                Dim colorLast = dst2.Get(Of cv.Vec3b)(rc.maxDStable.Y, rc.maxDStable.X)
                If color <> colorLast Then rc.maxDStable = rc.maxDist
            End If
        Next

        Dim stableCount As Integer
        If standaloneTest() Then
            stablePoints.Clear()
            For Each rc In rcList
                If stablePointsLast.Contains(rc.maxDStable) Then
                    Circle(dst2, rc.maxDStable, task.DotSize, task.highlight, -1)
                    stableCount += 1
                End If
                stablePoints.Add(rc.maxDStable)
            Next
        End If

        If rcList.Count > 160 Then task.fOptions.ReductionColor.Value += 1
        If rcList.Count < 100 Then task.fOptions.ReductionColor.Value -= 1

        'strOut = Utility_Basics.selectMinCell1(rcMapIndex, rcList, stablePoints)
        strOut = Utility_Basics.selectMinCell(rcMapIndex, rcList)
        SetTrueText(strOut, 3)

        If task.rcMinD IsNot Nothing And standaloneTest() Then Rectangle(dst2, task.rcMinD.rect, task.highlight, task.lineWidth)

        If task.heartBeat Then
            labels(2) = CStr(rcList.Count) + " RedColor cells were found.  " + If(stableCount > 0, CStr(stableCount) + " stable cells", "")
        End If
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