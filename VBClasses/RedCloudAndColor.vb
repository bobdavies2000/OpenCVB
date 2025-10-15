Imports cv = OpenCvSharp
Public Class RedCloudAndColor_Basics : Inherits TaskParent
    Public rcList As New List(Of rcData)
    Public rcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Dim reduction As New Reduction_Basics
    Public Sub New()
        task.gOptions.UseMotionMask.Checked = False
        desc = "Use RedColor for regions with no depth to add cells to RedCloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedCloud(src, labels(1))

        Static rcListLast = New List(Of rcData)
        Dim pcMapLast = rcMap.clone
        rcList = New List(Of rcData)(task.redCloud.rcList)

        dst3 = task.gray
        dst3.SetTo(0, task.depthMask)
        reduction.Run(dst3)
        dst1 = reduction.dst2 - 1

        Dim index = 1
        Dim rect As cv.Rect
        Dim minCount = dst2.Total * 0.001
        Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
        Dim newList As New List(Of rcData)
        For y = 0 To dst1.Height - 1
            For x = 0 To dst1.Width - 1
                Dim pt = New cv.Point(x, y)
                ' skip the regions with no depth or those that were already floodfilled.
                If dst1.Get(Of Byte)(pt.Y, pt.X) >= index Then
                    Dim count = cv.Cv2.FloodFill(dst1, mask, pt, index, rect, 0, 0, flags)
                    If rect.Width > 0 And rect.Height > 0 Then
                        'If count >= minCount Then
                        Dim pc = MaxDist_Basics.setCloudData(dst3(rect), rect, index)
                        If pc Is Nothing Then Continue For
                        pc.color = task.scalarColors(pc.index)
                        newList.Add(pc)
                        'dst1(pc.rect).SetTo(pc.index Mod 255, pc.mask)
                        SetTrueText(CStr(pc.index), pc.rect.TopLeft)
                        index += 1
                        'Else
                        '    dst1(rect).SetTo(255, mask(rect))
                        'End If
                    End If
                End If
            Next
        Next

        strOut = RedCell_Basics.selectCell(rcMap, rcList)
        If task.rcD IsNot Nothing Then task.color(task.rcD.rect).SetTo(white, task.rcD.contourMask)
        SetTrueText(strOut, 3)

        labels(2) = "Cells found = " + CStr(rcList.Count) + " and " + CStr(newList.Count) + " were color only cells."

        rcListLast = New List(Of rcData)(rcList)
    End Sub
End Class