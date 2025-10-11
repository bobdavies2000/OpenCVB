Imports cv = OpenCvSharp
Public Class RedColor_Basics : Inherits TaskParent
    Dim redCore As New RedColor_Core
    Public pcList As New List(Of cloudData)
    Public pcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        task.redColor = Me
        desc = "Run RedColor_Core on the heartbeat but just floodFill at maxDist otherwise."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Or task.optionsChanged Then
            redCore.Run(src)
            dst2 = redCore.dst2
            labels(2) = redCore.labels(2)
            pcList = New List(Of cloudData)(redCore.pcList)
            dst3 = redCore.dst2
        Else
            Dim pcListLast = New List(Of cloudData)(pcList)

            If src.Type <> cv.MatType.CV_8U Then src = task.gray
            redCore.redSweep.reduction.Run(src)
            dst1 = redCore.redSweep.reduction.dst2 + 1
            labels(3) = redCore.redSweep.reduction.labels(2)

            Dim index As Integer = 1
            Dim rect As New cv.Rect
            Dim maskRect = New cv.Rect(1, 1, dst1.Width, dst1.Height)
            Dim mask = New cv.Mat(New cv.Size(dst1.Width + 2, dst1.Height + 2), cv.MatType.CV_8U, 0)
            Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
            Dim minCount = dst1.Total * 0.001
            pcList.Clear()
            pcMap.SetTo(0)
            For Each pc In pcListLast
                Dim pt = pc.maxDist
                If pcMap.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                    Dim count = cv.Cv2.FloodFill(dst1, mask, pt, index, rect, 0, 0, flags)
                    If rect.Width > 0 And rect.Height > 0 Then
                        Dim pcc = MaxDist_Basics.setCloudData(dst1(rect).InRange(index, index), rect, index)
                        If pcc IsNot Nothing Then
                            pcc.index = pc.index
                            pcc.color = pc.color
                            pcc.age = pc.age + 1
                            index += 1
                            pcList.Add(pcc)
                            pcMap(pcc.rect).SetTo(pcc.index Mod 255, pcc.contourMask)
                        End If
                    End If
                End If
            Next

            dst2 = PaletteBlackZero(pcMap)
            labels(2) = CStr(pcList.Count) + " regions were identified "
        End If
    End Sub
End Class





Public Class RedColor_Core : Inherits TaskParent
    Public redSweep As New RedColor_Sweep
    Public pcList As New List(Of cloudData)
    Public pcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public percentImage As Single
    Public Sub New()
        desc = "Track the RedColor cells from RedColor_Core"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redSweep.Run(src)
        dst3 = redSweep.dst3

        Static pcListLast = New List(Of cloudData)(redSweep.pcList)
        Static pcMapLast As cv.Mat = redSweep.pcMap.clone

        pcList.Clear()
        Dim r2 As cv.Rect
        pcMap.SetTo(0)
        dst2.SetTo(0)
        For Each pc In redSweep.pcList
            Dim r1 = pc.rect
            r2 = New cv.Rect(0, 0, 1, 1) ' fake rect for conditional below...
            Dim indexLast = pcMapLast.Get(Of Byte)(pc.maxDist.Y, pc.maxDist.X) - 1
            If indexLast > 0 Then r2 = pcListLast(indexLast).rect
            If indexLast >= 0 And r1.IntersectsWith(r2) And task.optionsChanged = False Then
                pc.age = pcListLast(indexLast).age + 1
                If pc.age >= 1000 Then pc.age = 2
                If task.heartBeat = False And pc.rect.Contains(pcListLast(indexLast).maxdist) Then
                    pc.maxDist = pcListLast(indexLast).maxdist
                End If
                pc.color = pcListLast(indexLast).color
            End If
            pc.index = pcList.Count + 1
            pcMap(pc.rect).SetTo(pc.index, pc.mask)
            dst2(pc.rect).SetTo(pc.color, pc.mask)
            If standaloneTest() Then
                dst2.Circle(pc.maxDist, task.DotSize, task.highlight, -1)
                SetTrueText(CStr(pc.age), pc.maxDist)
            End If
            pcList.Add(pc)
        Next

        pcListLast = New List(Of cloudData)(pcList)
        pcMapLast = pcMap.Clone
    End Sub
End Class





Public Class RedColor_Sweep : Inherits TaskParent
    Public pcList As New List(Of cloudData)
    Public reduction As New Reduction_Basics
    Public pcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        desc = "Find RedColor cells in the reduced color image using a simple floodfill loop."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_8U Then src = task.gray
        reduction.Run(src)
        dst3 = reduction.dst2 + 1
        labels(3) = reduction.labels(2)

        Dim index As Integer = 1
        Dim rect As New cv.Rect
        Dim maskRect = New cv.Rect(1, 1, dst3.Width, dst3.Height)
        Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
        Dim minCount = dst3.Total * 0.001
        pcList.Clear()
        pcMap.SetTo(0)
        For y = 0 To dst3.Height - 1
            For x = 0 To dst3.Width - 1
                Dim pt = New cv.Point(x, y)
                ' skip the regions with depth (already handled elsewhere) or those that were already floodfilled.
                If dst3.Get(Of Byte)(pt.Y, pt.X) > 0 Then
                    Dim count = cv.Cv2.FloodFill(dst3, mask, pt, index, rect, 0, 0, flags)
                    If rect.Width > 0 And rect.Height > 0 And rect.Width < dst2.Width And rect.Height < dst2.Height Then
                        If count >= minCount Then
                            Dim pc = MaxDist_Basics.setCloudData(dst3(rect).InRange(index, index), rect, index)
                            pcList.Add(pc)
                            pcMap(pc.rect).SetTo(pc.index Mod 255, pc.contourMask)
                            index += 1
                        Else
                            dst3(rect).SetTo(255, mask(rect))
                        End If
                    End If
                End If
            Next
        Next

        dst2 = PaletteBlackZero(pcMap)
        labels(2) = CStr(pcList.Count) + " regions were identified "
    End Sub
End Class