Imports cv = OpenCvSharp
Public Class RedCloudAndColor_Basics : Inherits TaskParent
    Public pcList As New List(Of cloudData)
    Public pcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public redMask As New RedMask_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Use RedColor for regions with no depth to add cells to RedCloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedCloud(src, labels(1))

        Static pcListLast = New List(Of cloudData)(pcList)
        Dim pcMapLast = pcMap.clone
        pcList = New List(Of cloudData)(task.redCloud.pcList)

        reduction.Run(task.grayStable)
        reduction.dst2.SetTo(0, task.depthMask)
        redMask.Run(reduction.dst2)

        If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
        redMask.dst3.CopyTo(dst2, redMask.dst2)
        For i = 1 To redMask.mdList.Count - 1
            Dim pc = New cloudData(dst3(redMask.mdList(i).rect).InRange(i, i), redMask.mdList(i).rect)
            Dim r1 = pc.rect
            Dim r2 = New cv.Rect ' fake rect to trigger conditional below...
            Dim indexLast = pcMapLast.Get(Of Byte)(pc.maxDist.Y, pc.maxDist.X) - 1
            If indexLast > 0 Then r2 = pcListLast(indexLast).rect
            If indexLast >= 0 And r1.IntersectsWith(r2) And task.optionsChanged = False Then
                pc.age = pcListLast(indexLast).age + 1
                If pc.age > 1000 Then pc.age = 2
                If task.heartBeat = False And pc.rect.Contains(pcListLast(indexLast).maxdist) Then
                    pc.maxDist = pcListLast(indexLast).maxdist
                End If
            End If
            pc.color = redMask.dst3.Get(Of cv.Vec3b)(pc.maxDist.Y, pc.maxDist.X)
            pc.index = pcList.Count + 1

            pcList.Add(pc)
            pcMap(pc.rect).setto(pc.index, pc.mask)
            ' dst2(pc.rect).SetTo(pc.color, pc.mask)
            dst2.Circle(pc.maxDist, task.DotSize, pc.color, -1)
        Next

        pcListLast = New List(Of cloudData)(pcList)

        SetTrueText(RedCloud_Basics.selectCell(), 3)

        labels(2) = "Cells found = " + CStr(pcList.Count) + " and " + CStr(redMask.mdList.Count) + " were color only cells."
    End Sub
End Class