Imports cv = OpenCvSharp
Public Class Flood_Basics : Inherits TaskParent
    Public Sub New()
        desc = "Build the RedCloud cells with the grayscale input."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels = 1 Then
            dst2 = runRedOld(src, labels(2), src)
        Else
            dst2 = runRedOld(src, labels(2))
        End If
        dst1 = task.redC.dst1
        SetTrueText(task.redC.strOut, 3)
    End Sub
End Class





Public Class Flood_CellStatsPlot : Inherits TaskParent
    Public Sub New()
        task.gOptions.setHistogramBins(1000)
        desc = "Provide cell stats on the flood_basics cells.  Identical to RedCell_FloodFill"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedOld(src, labels(2))
        SetTrueText(task.redC.strOut, 3)
    End Sub
End Class








Public Class Flood_ContainedCells : Inherits TaskParent
    Public Sub New()
        desc = "Find cells that have only one neighbor.  They are likely to be contained in another cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then dst2 = runRedOld(src, labels(2))

        Dim removeCells As New List(Of Integer)
        For i = task.redC.rcList.Count - 1 To 0 Step -1
            Dim rc = task.redC.rcList(i)
            Dim nabs As New List(Of Integer)
            Dim contains As New List(Of Integer)
            For j = 0 To task.redC.rcList.Count - 1
                Dim rcBig = task.redC.rcList(j)
                If rcBig.rect.IntersectsWith(rc.rect) Then nabs.Add(rcBig.index)
                If rcBig.rect.Contains(rc.rect) Then contains.Add(rcBig.index)
            Next
            If contains.Count = 1 Then removeCells.Add(rc.index)
        Next

        dst3.SetTo(0)
        For Each index In removeCells
            Dim rc = task.redC.rcList(index)
            dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next

        If task.heartBeat Then
            labels(3) = CStr(removeCells.Count) + " cells were completely contained in another cell's rect"
        End If
    End Sub
End Class







Public Class Flood_BasicsMask : Inherits TaskParent
    Public binarizedImage As cv.Mat
    Public inputRemoved As cv.Mat
    Public cellGen As New RedCell_Generate
    Dim redMask As New RedMask_Basics
    Public buildinputRemoved As Boolean
    Public showSelected As Boolean = True
    Dim color8U As New Color8U_Basics
    Public Sub New()
        labels(3) = "The inputRemoved mask is used to limit how much of the image is processed."
        desc = "Floodfill by color as usual but this is run repeatedly with the different tiers."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Or buildinputRemoved Then
            color8U.Run(src)
            inputRemoved = task.pcSplit(2).InRange(task.MaxZmeters, task.MaxZmeters).ConvertScaleAbs()
            src = color8U.dst2
        End If

        dst3 = inputRemoved
        If inputRemoved IsNot Nothing Then src.SetTo(0, inputRemoved)
        redMask.Run(src)

        cellGen.mdList = redMask.mdList
        cellGen.Run(redMask.dst2)

        dst2 = cellGen.dst2

        If task.heartBeat Then labels(2) = $"{task.redC.rcList.Count} cells identified"

        If showSelected Then task.setSelectedCell()
    End Sub
End Class





Public Class Flood_Tiers : Inherits TaskParent
    Dim flood As New Flood_BasicsMask
    Dim tiers As New Depth_Tiers
    Dim color8U As New Color8U_Basics
    Public Sub New()
        desc = "Subdivide the Flood_Basics cells using depth tiers."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim tier = task.gOptions.DebugSlider.Value

        tiers.Run(src)
        If tier >= tiers.classCount Then tier = 0

        If tier = 0 Then
            dst1 = Not tiers.dst2.InRange(0, 1)
        Else
            dst1 = Not tiers.dst2.InRange(tier, tier)
        End If

        labels(2) = tiers.labels(2) + " in tier " + CStr(tier) + ".  Use the global options 'DebugSlider' to select different tiers."

        color8U.Run(src)

        flood.inputRemoved = dst1
        flood.Run(color8U.dst2)

        dst2 = flood.dst2
        dst3 = flood.dst3

        task.setSelectedCell()
    End Sub
End Class







Public Class Flood_Motion : Inherits TaskParent
    Dim flood As New Flood_Basics
    Dim rcList As New List(Of rcData)
    Dim cellMap As New cv.Mat
    Dim maxDists As New List(Of cv.Point2f)
    Dim maxIndex As New List(Of Integer)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Create RedCloud cells every heartbeat and compare the results against RedCloud cells created with the current frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            flood.Run(src)
            rcList = New List(Of rcData)(task.redC.rcList)
            cellMap = task.redC.rcMap.Clone
            dst2 = flood.dst2.Clone
            dst3 = flood.dst2.Clone
            labels(2) = flood.labels(2)
            labels(3) = flood.labels(2)

            maxDists.Clear()
            For Each rc In rcList
                maxDists.Add(rc.maxDist)
                maxIndex.Add(rc.index)
            Next
        Else
            flood.Run(src)
            dst1.SetTo(0)
            For Each rc In task.redC.rcList
                If maxDists.Contains(rc.maxDist) Then
                    Dim lrc = rcList(maxIndex(maxDists.IndexOf(rc.maxDist)))
                    dst1(lrc.rect).SetTo(lrc.color, lrc.mask)
                End If
            Next
            dst3 = flood.dst2
            labels(3) = flood.labels(2)
        End If
    End Sub
End Class






Public Class Flood_Minimal : Inherits TaskParent
    Dim prep As New RedPrep_ReductionChoices
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(2) = "Output is from RedPrep_ReductionChoices. Click any region to floodfill it."
        labels(3) = "Mask resulting region selected by the click."
        desc = "Floodfill the selected segment of the RedPrep image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst2 = prep.dst2

        If task.mouseClickFlag Then
            Dim rect As New cv.Rect
            Dim pt = task.ClickPoint
            Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, 0)
            Dim flags = cv.FloodFillFlags.FixedRange Or (255 << 8) Or cv.FloodFillFlags.MaskOnly
            Dim count = cv.Cv2.FloodFill(dst2, mask, pt, 255, rect, 0, 0, flags)
            dst1.SetTo(0)
            dst3 = mask(New cv.Rect(1, 1, dst2.Width, dst2.Height)).Clone
            dst1.Rectangle(rect, 255, task.lineWidth)
        End If
    End Sub
End Class




Public Class Flood_CloudData : Inherits TaskParent
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Floodfill each region of the RedPrep_ReductionChoices output."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Static prep As New RedPrep_Basics
            prep.Run(src)
            src = Not prep.dst2
        End If

        Dim index As Integer = 1
        Dim rect As New cv.Rect
        Dim maskRect = New cv.Rect(1, 1, src.Width, src.Height)
        Dim mask = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags = cv.FloodFillFlags.FixedRange Or (255 << 8) Or cv.FloodFillFlags.MaskOnly
        dst0.SetTo(0)
        dst1.SetTo(0)
        Dim minCount = src.Total * 0.001
        For y = 0 To src.Height - 1
            For x = 0 To src.Width - 1
                Dim pt = New cv.Point(x, y)
                Dim val = src.Get(Of Byte)(pt.Y, pt.X) ' skip the regions with no depth
                If val > 0 Then
                    val = dst1.Get(Of Byte)(pt.Y, pt.X)
                    If val = 0 Then
                        Dim count = cv.Cv2.FloodFill(src, mask, pt, index, rect, 0, 0, flags)
                        If count >= minCount Then
                            dst1.Rectangle(rect, 255, -1)
                            dst0(rect).SetTo(index, mask(rect))
                            index += 1
                            ' mask(rect).SetTo(0)
                        End If
                    End If
                End If
            Next
        Next

        dst3 = ShowPalette254(dst0)
        labels(2) = CStr(index) + " regions were identified"
    End Sub
End Class

