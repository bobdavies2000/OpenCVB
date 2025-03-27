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
        for each gc in task.gcList
            Dim index = dst1.Get(Of Byte)(gc.center.Y, gc.center.X)
            Dim md = redM.mdList(index)
            If index = 0 Then
                dst2(gc.rect).SetTo(black)
            Else
                If md.pixels > minSize Then
                    dst2(gc.rect).SetTo(task.scalarColors(index))
                    mdLargest.Add(md)
                End If
            End If
        Next

        dst3 = ShowAddweighted(src, dst2, labels(3))
        If task.heartBeat Then labels(2) = "There were " + CStr(redM.mdList.Count) + " connected contours found."
    End Sub
End Class








Public Class DepthRegion_Correlation : Inherits TaskParent
    Dim options As New Options_MatchCorrelation
    Public Sub New()
        optiBase.FindSlider("Min Correlation Coefficient").Value = 99
        dst0 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "The matching grid cells in the right view that were used in the correlation computation"
        desc = "Create depth region markers using a correlation threshold"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        Dim minCorr = options.MinCorrelation

        dst0.SetTo(0)
        dst1.SetTo(0)
        Dim count As Integer
        for each gc in task.gcList
            If gc.correlation > minCorr Then
                dst0.Rectangle(gc.rRect, 255, -1)
                dst1.Rectangle(gc.rect, 255, -1)
                count += 1
            End If
        Next

        dst2.SetTo(0)
        src.CopyTo(dst2, dst1)

        dst3.SetTo(0)
        task.rightView.CopyTo(dst3, dst0)

        labels(2) = Format(count / task.gcList.Count, "0%") + " of grid cells had color correlation of " + Format(minCorr, "0.0%") + " or better"
    End Sub
End Class

