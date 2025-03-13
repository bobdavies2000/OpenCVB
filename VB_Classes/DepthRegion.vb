Imports cv = OpenCvSharp
Public Class DepthRegion_Basics : Inherits TaskParent
    Public redM As New RedMask_Basics
    Public connect As New Connected_Rects
    Public mdLargest As New List(Of maskData)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        task.gOptions.TruncateDepth.Checked = True
        desc = "Find the main regions connected in depth and build a contour for each."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src.Clone)
        task.rcPixelThreshold = task.cellSize * task.cellSize ' eliminate singles...
        redM.Run(Not connect.dst2)

        dst1.SetTo(0)
        For Each md In redM.mdList
            dst1(md.rect).SetTo(md.index, md.mask)
        Next

        Dim minSize As Integer = src.Total / 25
        dst2.SetTo(0)
        mdLargest.Clear()
        For Each idd In task.iddList
            Dim index = dst1.Get(Of Byte)(idd.center.Y, idd.center.X)
            Dim md = redM.mdList(index)
            If index = 0 Then
                dst2(idd.cRect).SetTo(black)
            Else
                If md.pixels > minSize Then
                    dst2(idd.cRect).SetTo(task.scalarColors(index))
                    mdLargest.Add(md)
                End If
            End If
        Next

        dst3 = ShowAddweighted(src, dst2, labels(3))
        If task.heartBeat Then labels(2) = "There were " + CStr(redM.mdList.Count) + " connected contours found."
    End Sub
End Class

