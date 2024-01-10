Imports cv = OpenCvSharp
Public Class HeatMap_Basics : Inherits VB_Algorithm
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

        dst2 = vbPalette(dst0.ConvertScaleAbs())
        dst3 = vbPalette(dst1.ConvertScaleAbs())
        labels(2) = "Top view of heat map with the last " + CStr(task.frameHistoryCount) + " frames"
        labels(3) = "Side view of heat map with the last " + CStr(task.frameHistoryCount) + " frames"
    End Sub
End Class








Public Class HeatMap_Grid : Inherits VB_Algorithm
    Dim heat As New HeatMap_Basics
    Public Sub New()
        gOptions.GridSize.Value = 5
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
        If gOptions.UseMultiThreading.Checked Then
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









Public Class HeatMap_NotHotSpots : Inherits VB_Algorithm
    Public heat As New HeatMap_Hotspots
    Public Sub New()
        labels = {"", "", "Mask of red in heat map for the Top View", ""}
        desc = "Isolate points with low histogram values in side and top views"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        heat.Run(src)
        dst0 = heat.dst2.ConvertScaleAbs
        dst1 = heat.dst3.ConvertScaleAbs
        dst2 = heat.dst0.SetTo(0, dst0)
        dst3 = heat.dst1.SetTo(0, dst1)
    End Sub
End Class






Public Class HeatMap_Hotspots : Inherits VB_Algorithm
    Public heatTop As New Histogram2D_Top
    Public heatSide As New Histogram2D_Side
    Public Sub New()
        labels = {"", "", "Threshold'd heat map for the Top View", "Threshold'd heat map for the Side View"}
        desc = "Isolate just the hotspots in the heat map"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        heatTop.Run(src)
        dst0 = heatTop.histogram

        heatSide.Run(src)
        dst1 = heatSide.histogram

        dst2 = dst0.Threshold(task.redThresholdTop, 255, cv.ThresholdTypes.Binary)
        dst3 = dst1.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class
