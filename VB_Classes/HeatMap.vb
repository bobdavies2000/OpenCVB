Imports cv = OpenCvSharp
Public Class HeatMap_Basics : Inherits VB_Parent
    Public topframes As New History_Basics
    Public sideframes As New History_Basics
    Public histogramTop As New cv.Mat
    Public histogramSide As New cv.Mat
    Dim options As New Options_HeatMap
    Public Sub New()
        desc = "Highlight concentrations of depth pixels in the side view"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        cv.Cv2.CalcHist({src}, task.channelsTop, New cv.Mat, histogramTop, 2, task.bins2D, task.rangesTop)
        histogramTop.Row(0).SetTo(0)

        cv.Cv2.CalcHist({src}, task.channelsSide, New cv.Mat, histogramSide, 2, task.bins2D, task.rangesSide)
        histogramSide.Col(0).SetTo(0)

        topframes.Run(histogramTop)
        dst0 = topframes.dst2

        sideframes.Run(histogramSide)
        dst1 = sideframes.dst2

        dst2 = ShowPalette(dst0.ConvertScaleAbs())
        dst3 = ShowPalette(dst1.ConvertScaleAbs())
        labels(2) = "Top view of heat map with the last " + CStr(task.frameHistoryCount) + " frames"
        labels(3) = "Side view of heat map with the last " + CStr(task.frameHistoryCount) + " frames"
    End Sub
End Class








Public Class HeatMap_Grid : Inherits VB_Parent
    Dim heat As New HeatMap_Basics
    Public Sub New()
        task.gOptions.GridSize.Value = 5
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "Histogram mask for top-down view - original histogram in dst0", "Histogram mask for side view - original histogram in dst1"}
        desc = "Apply a grid to the HeatMap_OverTime to isolate objects."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        heat.Run(src)

        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim maxCount1 As Integer, maxCount2 As Integer
        Dim sync1 As New Object, sync2 As New Object
        If task.gOptions.UseMultiThreading.Checked Then
            Parallel.ForEach(task.gridList,
            Sub(roi)
                Dim count1 = heat.histogramTop(roi).CountNonZero
                dst2(roi).SetTo(count1)
                If count1 > maxCount1 Then
                    SyncLock sync1
                        maxCount1 = count1
                    End SyncLock
                End If

                Dim count2 = heat.histogramSide(roi).CountNonZero
                dst3(roi).SetTo(count2)
                If count2 > maxCount2 Then
                    SyncLock sync2
                        maxCount2 = count2
                    End SyncLock
                End If
            End Sub)
        Else
            For Each roi In task.gridList
                Dim count1 = heat.histogramTop(roi).CountNonZero
                dst2(roi).SetTo(count1)
                If count1 > maxCount1 Then maxCount1 = count1

                Dim count2 = heat.histogramSide(roi).CountNonZero
                dst3(roi).SetTo(count2)
                If count2 > maxCount2 Then maxCount2 = count2
            Next
        End If
        dst2 *= 255 / maxCount1
        dst3 *= 255 / maxCount2
    End Sub
End Class









Public Class HeatMap_HotNot : Inherits VB_Parent
    Dim heat As New HeatMap_Hot
    Public Sub New()
        labels = {"", "", "Mask of cool areas in the heat map - top view", "Mask of cool areas in the heat map - side view"}
        desc = "Isolate points with low histogram values in side and top views"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        heat.Run(src)
        dst0 = heat.dst2.ConvertScaleAbs
        dst1 = heat.dst3.ConvertScaleAbs
        dst2 = dst0.Threshold(task.redOptions.ProjectionThreshold.Value, 255, cv.ThresholdTypes.Binary)
        dst3 = dst1.Threshold(task.redOptions.ProjectionThreshold.Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class HeatMap_Hot : Inherits VB_Parent
    Dim histTop As New Projection_HistTop
    Dim histSide As New Projection_HistSide
    Public Sub New()
        labels = {"", "", "Mask of hotter areas for the Top View", "Mask of hotter areas for the Side View"}
        desc = "Isolate masks for just the hotspots in the heat map"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        histTop.Run(src)
        dst2 = histTop.histogram

        histSide.Run(src)
        dst3 = histSide.histogram

        Dim mmTop = GetMinMax(dst2)
        Dim mmSide = GetMinMax(dst3)
        If task.heartBeat Then labels(2) = CStr(mmTop.maxVal) + " max count " + CStr(dst2.CountNonZero) + " pixels in the top down view"
        If task.heartBeat Then labels(3) = CStr(mmSide.maxVal) + " max count " + CStr(dst3.CountNonZero) + " pixels in the side view"
    End Sub
End Class








Public Class HeatMap_Cell : Inherits VB_Parent
    Dim flood As New Flood_Basics
    Dim heat As New HeatMap_Hot
    Public Sub New()
        task.redOptions.IdentifyCells.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Display the heat map for the selected cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flood.Run(src)
        dst2 = flood.dst2
        labels(2) = flood.labels(2)

        dst0 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        task.pointCloud(task.rc.rect).CopyTo(dst0(task.rc.rect), task.rc.mask)

        heat.Run(dst0)
        dst1 = heat.dst2
        dst3 = heat.dst3

        labels(1) = heat.labels(2)
        labels(3) = heat.labels(3)
    End Sub
End Class








Public Class HeatMap_GuidedBP : Inherits VB_Parent
    Dim guided As New GuidedBP_Basics
    Public Sub New()
        task.redOptions.ProjectionThreshold.Value = 1
        desc = "This is just a placeholder to make it easy to find the GuidedBP_Basics which shows objects in top/side views."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        guided.Run(src)
        dst2 = guided.dst2
        dst3 = guided.dst3
        labels = guided.labels
    End Sub
End Class