Imports cv = OpenCvSharp
Public Class RedCloud_Basics : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Public pcList As New List(Of cloudData)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Map of reduced point cloud - CV_8U"
        desc = "Find the biggest chunks of consistent depth data "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst3 = prep.dst2

        Dim index As Integer = 1
        Dim rect As New cv.Rect
        Dim maskRect = New cv.Rect(1, 1, dst3.Width, dst3.Height)
        Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags = cv.FloodFillFlags.FixedRange Or (255 << 8) Or cv.FloodFillFlags.MaskOnly
        dst1.SetTo(0)
        dst2.SetTo(0)
        pcList.Clear()
        Dim minCount = dst3.Total * 0.001, maxCount = dst3.Total * 3 / 4
        For y = 0 To dst3.Height - 1
            For x = 0 To dst3.Width - 1
                Dim pt = New cv.Point(x, y)
                Dim val = dst3.Get(Of Byte)(pt.Y, pt.X) ' skip the regions with no depth
                If val > 0 Then
                    val = dst1.Get(Of Byte)(pt.Y, pt.X) ' skip flooding need good chunks of depth data.
                    If val = 0 Then
                        Dim count = cv.Cv2.FloodFill(dst3, mask, pt, index, rect, 0, 0, flags)
                        If count >= minCount And count < maxCount Then
                            dst1.Rectangle(rect, 255, -1)
                            index += 1
                            Dim pd = New cloudData(mask(rect), rect, count)
                            dst2(rect).SetTo(pd.color, mask(rect))
                            pcList.Add(pd)
                        End If
                    End If
                End If
            Next
        Next

        For Each pd In pcList
            dst2.Circle(pd.maxDist, task.DotSize, task.highlight, -1)
        Next

        labels(2) = CStr(index) + " regions were identified"
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
        prep.Run(src)

        dst2 = runRedC(prep.dst2, labels(2))
        If standaloneTest() Then
            Static stats As New RedCell_Basics
            stats.Run(src)
            dst1 = stats.dst3
            SetTrueText(stats.strOut, 3)
        End If
    End Sub
End Class






Public Class RedCloud_Basics_CPP : Inherits TaskParent
    Dim prep As New RedPrep_Basics
    Dim stats As New RedCell_Basics
    Public Sub New()
        OptionParent.findRadio("XY Reduction").Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Run the reduced pointcloud output through the RedColor_CPP algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(src)
        dst2 = runRedOld(prep.dst2, labels(2))
        stats.Run(src)
        dst1 = stats.dst3
        SetTrueText(stats.strOut, 3)
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

