Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class RedCloud_Basics : Inherits TaskParent
    Dim redCore As New RedCloud_Core
    Public pcList As New List(Of cloudData)
    Public Sub New()
        task.redCNew = Me
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Build contours for each cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redCore.Run(src)
        labels(2) = redCore.labels(2) + "  Age of each cell is displayed as well."

        Static pcListLast = New List(Of cloudData)(pcList)
        Static pcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Static depthLast As cv.Mat = task.pcSplit(2)

        pcList.Clear()
        Dim r2 As cv.Rect
        For Each pc As cloudData In redCore.pcList
            Dim indexLast = pcMap.Get(Of Byte)(pc.maxDist.Y, pc.maxDist.X) - 1
            Dim r1 = pc.rect
            r2 = New cv.Rect(0, 0, 1, 1) ' fake rect to trigger conditional below...
            If indexLast >= 0 Then r2 = pcListLast(indexLast).rect
            If indexLast >= 0 And r1.IntersectsWith(r2) Then
                pc.age = pcListLast(indexLast).age + 1
                If pc.age > 1000 Then pc.age = 2
                pc.indexLast = indexLast
                pc.depthLast = depthLast(pc.rect).Mean(pc.mask)
                pc.maxDist = pcListLast(pc.indexLast).maxdist
                pc.color = pcListLast(pc.indexLast).color
            Else
                pc.indexLast = pc.index
                pc.color = task.vecColors(pc.index)
            End If
            pc.index = pcList.Count + 1
            pcList.Add(pc)
        Next

        dst1.SetTo(0)
        dst2.SetTo(0)
        For Each pc In pcList
            pc.contour = ContourBuild(pc.mask, cv.ContourApproximationModes.ApproxNone) ' ApproxTC89L1 or ApproxNone
            DrawTour(dst1(pc.rect), pc.contour, pc.index)
            DrawTour(dst2(pc.rect), pc.contour, pc.color)
            SetTrueText(CStr(pc.age), pc.maxDist)
        Next

        Dim pcClick = RedCell_PCBasics.displayCell()
        If pcClick IsNot Nothing Then
            If pcClick.rect.Contains(task.ClickPoint) Then
                task.color(pcClick.rect).SetTo(white, pcClick.mask)
                SetTrueText(pcClick.displayString, 3)
                SetTrueText(CStr(pcClick.index), pcClick.maxDist, 3)
            End If
        End If

        pcListLast = New List(Of cloudData)(task.redCNew.pcList)
        pcMap = dst1.Clone
        depthLast = task.pcSplit(2)
    End Sub
End Class






Public Class RedCloud_Core : Inherits TaskParent
    Public prepEdges As New RedPrep_Basics
    Public pcList As New List(Of cloudData)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Reduced point cloud - use 'Reduction Target' option to increase/decrease cell sizes."
        desc = "Find the biggest chunks of consistent depth data "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prepEdges.Run(src)
        dst3 = Not prepEdges.dst2

        Dim index As Integer = 1
        Dim rect As New cv.Rect
        Dim maskRect = New cv.Rect(1, 1, dst3.Width, dst3.Height)
        Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
        Dim minCount = dst3.Total * 0.001, maxCount = dst3.Total * 3 / 4
        Dim newList As New SortedList(Of Integer, cloudData)(New compareAllowIdenticalInteger)
        For y = 0 To dst3.Height - 1
            For x = 0 To dst3.Width - 1
                Dim pt = New cv.Point(x, y)
                ' skip the regions with no depth or those that were already floodfilled.
                If dst3.Get(Of Byte)(pt.Y, pt.X) > index Then
                    Dim count = cv.Cv2.FloodFill(dst3, mask, pt, index, rect, 0, 0, flags)
                    If rect.Width > 0 And rect.Height > 0 Then
                        If count >= minCount And count < maxCount Then
                            Dim pc = New cloudData(dst3(rect).InRange(index, index), rect, count)
                            index += 1
                            pc.index = index
                            newList.Add(pc.maxDist.Y, pc)
                        End If
                    End If
                End If
            Next
        Next

        pcList.Clear()
        dst1.SetTo(0)
        For Each pc In newList.Values
            pc.index = pcList.Count + 1
            dst1(pc.rect).SetTo(pc.index Mod 255, pc.mask)
            SetTrueText(CStr(pc.index), New cv.Point(pc.rect.X, pc.rect.Y))
            pcList.Add(pc)
        Next

        If standaloneTest() Then
            dst2 = ShowPalette254(dst1)

            For Each pc In pcList
                dst2.Circle(pc.maxDist, task.DotSize, task.highlight, -1)
            Next

            RedCell_PCBasics.displayCell()
        End If
        labels(2) = CStr(newList.Count) + " regions were identified."
    End Sub
End Class






Public Class RedCloud_XY : Inherits TaskParent
    Dim prep As New RedPrep_ReductionChoices
    Public Sub New()
        OptionParent.findRadio("XY Reduction").Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        desc = "Build XY RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(prep.dst2, labels(2))
        If standaloneTest() Then
            Static stats As New RedCell_Basics
            stats.Run(src)
            SetTrueText(stats.strOut, 3)
        End If
    End Sub
End Class





Public Class RedCloud_XYZ : Inherits TaskParent
    Dim prep As New RedPrep_ReductionChoices
    Public redMask As New RedMask_Basics
    Dim rcMask As cv.Mat
    Public Sub New()
        OptionParent.findRadio("XYZ Reduction").Checked = True
        rcMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedOld(prep.dst2, labels(2))

        If task.heartBeat Then strOut = ""
        For i = 0 To task.redC.rcList.Count - 1
            Dim rc = task.redC.rcList(i)
            rcMask.SetTo(0)
            rcMask(rc.rect).SetTo(255, rc.mask)
            rc.mdList = New List(Of maskData)
            For Each md In redMask.mdList
                Dim index = rcMask.Get(Of Byte)(md.maxDist.Y, md.maxDist.X)
                If index > 0 Then rc.mdList.Add(md)
            Next
            If rc.mdList.Count > 0 Then
                For j = 0 To rc.mdList.Count - 1
                    Dim md = rc.mdList(j)
                    rcMask(md.rect) = rcMask(md.rect) And md.mask
                    md.mask = rcMask(md.rect).Clone
                    rc.mdList(j) = md
                Next
                task.redC.rcList(i) = rc
            End If
        Next

        SetTrueText(strOut, 3)
    End Sub
End Class








Public Class RedCloud_YZ : Inherits TaskParent
    Dim prep As New RedPrep_ReductionChoices
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("YZ Reduction").Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build YZ RedCloud cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedOld(prep.dst2, labels(2))

        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_XZ : Inherits TaskParent
    Dim prep As New RedPrep_ReductionChoices
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("XZ Reduction").Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build XZ RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedOld(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_X : Inherits TaskParent
    Dim prep As New RedPrep_ReductionChoices
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("X Reduction").Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build X RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedOld(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_Y : Inherits TaskParent
    Dim prep As New RedPrep_ReductionChoices
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("Y Reduction").Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build Y RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedOld(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class





Public Class RedCloud_Z : Inherits TaskParent
    Dim prep As New RedPrep_ReductionChoices
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("Z Reduction").Checked = True
        labels(3) = "Above is the depth histogram of the selected cell.  Below are the stats for the same cell"
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Build Z RedCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)

        dst2 = runRedOld(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
    End Sub
End Class






Public Class RedCloud_MotionSimple : Inherits TaskParent
    Dim redContours As New RedCloud_Basics
    Public Sub New()
        task.gOptions.HistBinBar.Maximum = 255
        task.gOptions.HistBinBar.Value = 255
        desc = "Use motion to identify which cells changed."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redContours.Run(src)
        dst1 = redContours.dst1
        dst2 = redContours.dst2
        labels(2) = redContours.labels(2)

        dst1.SetTo(0, Not task.motionMask)

        Dim histogram As New cv.Mat
        Dim ranges = {New cv.Rangef(1, 256)}
        cv.Cv2.CalcHist({dst1}, {0}, New cv.Mat, histogram, 1, {task.histogramBins}, ranges)

        Dim histArray(histogram.Rows - 1) As Single
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

        Dim pcUsed As New List(Of Integer)
        If task.heartBeatLT Then dst3 = dst2.Clone
        For i = 1 To histArray.Count - 1
            If histArray(i) > 0 And pcUsed.Contains(i) = False Then
                Dim pc = redContours.pcList(i)
                dst3(pc.rect).SetTo(task.scalarColors(pc.index), pc.mask)
                pcUsed.Add(i)
            End If
        Next
    End Sub
End Class






Public Class RedCloud_Motion : Inherits TaskParent
    Dim redContours As New RedCloud_Basics
    Public Sub New()
        task.gOptions.HistBinBar.Maximum = 255
        task.gOptions.HistBinBar.Value = 255
        desc = "Use motion to identify which cells changed."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redContours.Run(src)
        dst1 = redContours.dst1
        dst2 = redContours.dst2
        labels(2) = redContours.labels(2)

        dst3.SetTo(0)
        For Each pc In task.redCNew.pcList
            If pc.age > 10 Then dst3(pc.rect).SetTo(pc.color, pc.mask)

        Next
    End Sub
End Class
