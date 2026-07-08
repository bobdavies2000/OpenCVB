Imports cv = OpenCvSharp
Public Class RedC_Basics : Inherits TaskParent
    Dim color8u As New Color8U_Basics
    Public rcMap As New cv.Mat
    Public rcList As New List(Of rcData) ' includes cloud data.
    Public rcNone As New cv.Mat
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "RedCloud cells with depth."
        desc = "FloodFill each color8U output and create an rclist"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then color8u.Run(task.gray) Else color8u.Run(src)
        src = color8u.dst2 + 1

        rcMap = src.Clone
        Dim minList As New List(Of rcData)
        Dim rect As cv.Rect
        Dim mask As cv.Mat = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, 0)
        For Each r In task.gridRects
            If mask(r).Get(Of Byte)(0, 0) = 0 Then
                Dim mapID As Integer = rcMap(r).Get(Of Byte)(0, 0)
                Dim flags = cv.FloodFillFlags.FixedRange Or cv.FloodFillFlags.MaskOnly Or (255 << 8)
                Dim count = cv.Cv2.FloodFill(rcMap, mask, r.TopLeft, mapID, rect, 0, 0, flags)
                If count > 0 Then minList.Add(New rcData(rcMap(rect), rect, mapID))
            End If
        Next

        rcNone = Not mask(New cv.Rect(1, 1, dst2.Width, dst2.Height))
        rcMap.SetTo(0, rcNone)
        dst2 = Palettize(rcMap, 0)

        If task.rcMinD IsNot Nothing And standaloneTest() Then dst2.Rectangle(task.rcMinD.rect, task.highlight, task.lineWidth)

        Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To minList.Count - 1
            Dim rc = minList(i)
            rc.maxDist = rc.buildMaxDist(rc.mask)

            rc.depth = task.pcSplit(2)(rc.rect).Mean(rc.mask)
            rc.maskDepth = rc.mask.Clone
            rc.maskDepth.SetTo(0, task.noDepthMask(rc.rect))
            rc.pixelsDepth = rc.maskDepth.CountNonZero
            rc.maxDistDepth = rc.buildMaxDist(rc.maskDepth)

            sortList.Add(rc.pixels, rc)
        Next

        rcList = New List(Of rcData)(sortList.Values)
        Dim rcIndex As Integer
        For Each rc In rcList
            rc.index = rcIndex
            rcIndex += 1
        Next

        strOut = Utility_Basics.selectMinCell(rcMap, rcList)
        SetTrueText(strOut, 1)

        labels(2) = CStr(rcList.Count) + " RedColor cells were found."
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
                Dim vec = dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                dst3(rc.rect).SetTo(vec, rc.mask)
                count += 1
            End If
        Next

        labels(3) = CStr(count) + " cells smaller than " + CStr(task.gOptions.DebugSlider.Value) + " pixels."
    End Sub
End Class






Public Class XR_RedC_Lines : Inherits TaskParent
    Dim redC As New RedC_Basics
    Dim lines As New Line_Basics
    Public Sub New()
        desc = "Use the 'Other' class of RedC cells - the unclassified areas - as input to line detection."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        lines.Run(redC.rcNone)
        dst3 = lines.dst2
        labels(3) = lines.labels(2)
    End Sub
End Class
