Imports cv = OpenCvSharp
Public Class RedCell_Basics : Inherits TaskParent
    Public Sub New()
        desc = "Display the output of a cell for RedCloud_Basics."
    End Sub
    Public Shared Function displayCell() As cloudData
        Dim clickIndex = task.redCloud.pcMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X) - 1
        If clickIndex >= 0 Then
            Return task.redCloud.pcList(clickIndex)
        End If
        Return Nothing
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then dst2 = runRedCloud(src, labels(2))
        Dim pcClick = displayCell()
        If pcClick IsNot Nothing Then SetTrueText(pcClick.displayCell, 3)
    End Sub
End Class





Public Class RedCell_Color : Inherits TaskParent
    Public mdList As New List(Of maskData)
    Public Sub New()
        desc = "Generate the RedColor cells from the rects, mask, and pixel counts."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            SetTrueText("RedCell_Color is run by numerous algorithms but generates no output when standalone. ", 2)
            Exit Sub
        End If
        If task.redColor Is Nothing Then task.redColor = New RedColor_Basics

        Dim initialList As New List(Of rcData)
        For i = 0 To mdList.Count - 1
            Dim rc As New rcData
            rc.rect = mdList(i).rect
            If rc.rect.Size = dst2.Size Then Continue For ' RedColor_Basics can find a cell this big.  
            rc.mask = mdList(i).mask
            rc.maxDist = mdList(i).maxDist
            rc.maxDStable = rc.maxDist
            rc.indexLast = task.redColor.rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            rc.contour = mdList(i).contour
            DrawTour(rc.mask, rc.contour, 255, -1)
            rc.pixels = mdList(i).mask.CountNonZero
            If rc.indexLast >= task.redColor.rcList.Count Then rc.indexLast = 0
            If rc.indexLast > 0 Then
                Dim lrc = task.redColor.rcList(rc.indexLast)
                rc.age = lrc.age + 1
                rc.depth = lrc.depth
                rc.depthPixels = lrc.depthPixels
                rc.mmX = lrc.mmX
                rc.mmY = lrc.mmY
                rc.mmZ = lrc.mmZ
                rc.maxDStable = lrc.maxDStable

                If rc.pixels < task.rcPixelThreshold Then
                    rc.color = yellow
                Else
                    ' verify that the maxDStable is still good.
                    Dim v1 = task.redColor.rcMap.Get(Of Byte)(rc.maxDStable.Y, rc.maxDStable.X)
                    If v1 <> lrc.index Then
                        rc.maxDStable = rc.maxDist

                        rc.age = 1 ' a new cell was found that was probably part of another in the previous frame.
                    End If
                End If
            Else
                rc.age = 1
            End If

            Dim brickIndex = task.gridMap.Get(Of Integer)(rc.maxDStable.Y, rc.maxDStable.X)
            rc.color = task.scalarColors(brickIndex Mod 255)
            initialList.Add(rc)
        Next

        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)

        Dim rcNewCount As Integer
        Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
        For Each rc In initialList
            rc.pixels = rc.mask.CountNonZero
            If rc.pixels = 0 Then Continue For

            Dim depthMask = rc.mask.Clone
            depthMask.SetTo(0, task.noDepthMask(rc.rect))
            Dim depthPixels = depthMask.CountNonZero

            If depthPixels / rc.pixels > 0.1 Then
                rc.mmX = GetMinMax(task.pcSplit(0)(rc.rect), depthMask)
                rc.mmY = GetMinMax(task.pcSplit(1)(rc.rect), depthMask)
                rc.mmZ = GetMinMax(task.pcSplit(2)(rc.rect), depthMask)

                cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev, depthMask)
                rc.depth = depthMean(2)
                If Single.IsNaN(rc.depth) Or rc.depth < 0 Then rc.depth = 0
            End If

            If rc.age = 1 Then rcNewCount += 1
            sortedCells.Add(rc.pixels, rc)
        Next

        If task.heartBeat Then
            labels(2) = CStr(task.redColor.rcList.Count) + " total cells (shown with '" + task.gOptions.trackingLabel + "' and " +
                        CStr(task.redColor.rcList.Count - rcNewCount) + " matched to previous frame"
        End If
        dst2 = RebuildRCMap(sortedCells)
    End Sub
End Class





Public Class RedCell_Cloud : Inherits TaskParent
    Public mdList As New List(Of maskData)
    Public pcList As New List(Of cloudData)
    Public Sub New()
        desc = "Generate the RedCloud cells from the rects, mask, and pixel counts."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            SetTrueText("Test RedCell_Cloud with RedCloud_WithColor but generates no output when standalone. ", 2)
            Exit Sub
        End If

        pcList.Clear()
        For i = 1 To mdList.Count - 1
            Dim pc As New cloudData
            pc.rect = mdList(i).rect
            pc.mask = mdList(i).mask
            pc.pixels = pc.mask.CountNonZero
            DrawTour(pc.mask, pc.contour, 255, -1)
            pc.pixels = mdList(i).mask.CountNonZero

            Dim brickIndex = task.gridMap.Get(Of Integer)(pc.maxDist.Y, pc.maxDist.X)
            Dim color = task.scalarColors(brickIndex Mod 255)
            pc.color = New cv.Vec3b(color(0), color(1), color(2))
            pcList.Add(pc)
        Next

        If task.heartBeat Then labels(2) = CStr(pcList.Count) + " RedColor cells were found"
    End Sub
End Class