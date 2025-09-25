Imports cv = OpenCvSharp
Public Class RedCloud_Basics : Inherits TaskParent
    Dim prepEdges As New RedPrep_EdgeMask
    Public pcList As New List(Of cloudData)
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Map of reduced point cloud - CV_8U"
        desc = "Find the biggest chunks of consistent depth data "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prepEdges.Run(src)
        dst3 = prepEdges.dst2

        Dim index As Integer = 1
        Dim rect As New cv.Rect
        Dim maskRect = New cv.Rect(1, 1, dst3.Width, dst3.Height)
        Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags = cv.FloodFillFlags.FixedRange Or (255 << 8) Or cv.FloodFillFlags.MaskOnly
        Dim maskUsed As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim minCount = dst3.Total * 0.001, maxCount = dst3.Total * 3 / 4
        Dim newList As New SortedList(Of Integer, cloudData)(New compareAllowIdenticalInteger)
        For y = 1 To dst3.Height - 2
            For x = 0 To dst3.Width - 2
                Dim pt = New cv.Point(x, y)
                ' skip the regions with no depth 
                If dst3.Get(Of Byte)(pt.Y, pt.X) > 0 Then
                    ' skip flooding near good chunks of depth data.
                    If maskUsed.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                        Dim count = cv.Cv2.FloodFill(dst3, mask, pt, index, rect, 0, 0, flags)
                        Dim r = ValidateRect(New cv.Rect(rect.X + 1, rect.Y + 1, rect.Width, rect.Height))
                        maskUsed.Rectangle(r, 255, -1)
                        If count >= minCount And count < maxCount Then
                            Dim pc = New cloudData(mask(r), r, count)
                            index += 1
                            newList.Add(pc.id, pc)
                        End If
                    End If
                End If
            Next
        Next

        pcList.Clear()
        dst1.SetTo(0)
        For Each pc In newList.Values
            pc.index = pcList.Count + 1
            pcList.Add(pc)
            dst1(pc.rect).SetTo(pc.index Mod 255, pc.mask)
            SetTrueText(CStr(pc.index) + ", " + CStr(pc.id), New cv.Point(pc.rect.X, pc.rect.Y))
        Next
        dst2 = ShowPalette254(dst1)

        Dim clickIndex = dst1.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X)
        If clickIndex > 0 And clickIndex < pcList.Count Then
            task.color(pcList(clickIndex - 1).rect).SetTo(white, pcList(clickIndex - 1).mask)
        End If
        labels(2) = CStr(newList.Count) + " regions were identified. Region " + CStr(clickIndex) + " was selected."
    End Sub
End Class






Public Class RedCloud_XY : Inherits TaskParent
    Dim prep As New RedPrep_Basics
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
    Dim prep As New RedPrep_Basics
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
    Dim prep As New RedPrep_Basics
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
    Dim prep As New RedPrep_Basics
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
    Dim prep As New RedPrep_Basics
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
    Dim prep As New RedPrep_Basics
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
    Dim prep As New RedPrep_Basics
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




Public Class RedCloud_BasicsNew : Inherits TaskParent
    Dim prepEdges As New RedPrep_Edges_CPP
    Public pcList As New List(Of cloudData)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Map of reduced point cloud - CV_8U"
        desc = "Find the biggest chunks of consistent depth data "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prepEdges.Run(src)
        dst3 = prepEdges.dst2

        Dim index As Integer = 1
        Dim rect As New cv.Rect
        Dim maskRect = New cv.Rect(1, 1, dst3.Width, dst3.Height)
        Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags = cv.FloodFillFlags.FixedRange Or (255 << 8) Or cv.FloodFillFlags.MaskOnly
        Dim maskUsed As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim minCount = dst3.Total * 0.001, maxCount = dst3.Total * 3 / 4
        Dim newList As New SortedList(Of Integer, cloudData)(New compareAllowIdenticalInteger)
        For y = 0 To dst3.Height - 1
            For x = 0 To dst3.Width - 1
                Dim pt = New cv.Point(x, y)
                ' skip the regions with no depth 
                If dst3.Get(Of Byte)(pt.Y, pt.X) > 0 Then
                    ' skip flooding near good chunks of depth data.
                    If maskUsed.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                        Dim count = cv.Cv2.FloodFill(dst3, mask, pt, index, rect, 0, 0, flags)
                        Dim r = ValidateRect(New cv.Rect(rect.X + 1, rect.Y + 1, rect.Width, rect.Height))
                        maskUsed.Rectangle(r, 255, -1)
                        If count >= minCount And count < maxCount Then
                            Dim pc = New cloudData(mask(r), r, count)
                            index += 1
                            newList.Add(pc.id, pc)
                        End If
                    End If
                End If
            Next
        Next

        pcList.Clear()
        dst1.SetTo(0)
        For Each pc In newList.Values
            pc.index = pcList.Count + 1
            pcList.Add(pc)
            dst1(pc.rect).SetTo(pc.index Mod 255, pc.mask)
            SetTrueText(CStr(pc.index) + ", " + CStr(pc.id), New cv.Point(pc.rect.X, pc.rect.Y))
        Next
        dst2 = ShowPalette254(dst1)

        Dim clickIndex = dst1.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X)
        If clickIndex > 0 And clickIndex < pcList.Count Then
            task.color(pcList(clickIndex - 1).rect).SetTo(white, pcList(clickIndex - 1).mask)
            maskUsed.SetTo(0)
            maskUsed(pcList(clickIndex - 1).rect).SetTo(255, pcList(clickIndex - 1).mask)
        End If
        labels(2) = CStr(newList.Count) + " regions were identified. Region " + CStr(clickIndex) + " was selected."
    End Sub
End Class

