Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class HeatMap_Basics : Inherits TaskParent
        Public topframes As New History_Basics
        Public sideframes As New History_Basics
        Public histogramTop As New cv.Mat
        Public histogramSide As New cv.Mat
        Dim options As New Options_HeatMap
        Public Sub New()
            desc = "Highlight concentrations of depth pixels in the side view"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Type <> cv.MatType.CV_32FC3 Then src = tsk.pointCloud

            cv.Cv2.CalcHist({src}, tsk.channelsTop, New cv.Mat, histogramTop, 2, tsk.bins2D, tsk.rangesTop)
            histogramTop.Row(0).SetTo(0)

            cv.Cv2.CalcHist({src}, tsk.channelsSide, New cv.Mat, histogramSide, 2, tsk.bins2D, tsk.rangesSide)
            histogramSide.Col(0).SetTo(0)

            topframes.Run(histogramTop)
            dst0 = topframes.dst2

            sideframes.Run(histogramSide)
            dst1 = sideframes.dst2

            dst2 = PaletteBlackZero(dst0.ConvertScaleAbs()).Clone
            dst3 = PaletteBlackZero(dst1.ConvertScaleAbs())
            labels(2) = "Top view of heat map with the last " + CStr(tsk.frameHistoryCount) + " frames"
            labels(3) = "Side view of heat map with the last " + CStr(tsk.frameHistoryCount) + " frames"
        End Sub
    End Class








    Public Class NR_HeatMap_Grid : Inherits TaskParent
        Dim heat As New HeatMap_Basics
        Public Sub New()
            tsk.gOptions.GridSlider.Value = 5
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            labels = {"", "", "Histogram mask for top-down view - original histogram in dst0", "Histogram mask for side view - original histogram in dst1"}
            desc = "Apply a grid to the HeatMap_OverTime to isolate objects."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32FC3 Then src = tsk.pointCloud

            heat.Run(src)

            dst2.SetTo(0)
            dst3.SetTo(0)
            Dim maxCount1 As Integer, maxCount2 As Integer
            Dim sync1 As New Object, sync2 As New Object
            For Each roi In tsk.gridRects
                Dim count1 = heat.histogramTop(roi).CountNonZero
                dst2(roi).SetTo(count1)
                If count1 > maxCount1 Then maxCount1 = count1

                Dim count2 = heat.histogramSide(roi).CountNonZero
                dst3(roi).SetTo(count2)
                If count2 > maxCount2 Then maxCount2 = count2
            Next
            dst2 *= 255 / maxCount1
            dst3 *= 255 / maxCount2
        End Sub
    End Class





    Public Class HeatMap_Hot : Inherits TaskParent
        Dim histTop As New Projection_HistTop
        Dim histSide As New Projection_HistSide
        Public Sub New()
            labels = {"", "", "Mask of hotter areas for the Top View", "Mask of hotter areas for the Side View"}
            desc = "Isolate masks for just the hotspots in the heat map"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            histTop.Run(src)
            dst2 = histTop.histogram

            histSide.Run(src)
            dst3 = histSide.histogram

            Dim mmTop = GetMinMax(dst2)
            Dim mmSide = GetMinMax(dst3)
            If tsk.heartBeat Then labels(2) = CStr(mmTop.maxVal) + " max count " + CStr(dst2.CountNonZero) + " pixels in the top down view"
            If tsk.heartBeat Then labels(3) = CStr(mmSide.maxVal) + " max count " + CStr(dst3.CountNonZero) + " pixels in the side view"
        End Sub
    End Class








    Public Class NR_HeatMap_Cell : Inherits TaskParent
        Dim flood As New Flood_Basics
        Dim heat As New HeatMap_Hot
        Public Sub New()
            If standalone Then tsk.gOptions.displaydst1.checked = True
            desc = "Display the heat map for the selected cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            flood.Run(src)
            dst2 = flood.dst2
            labels(2) = flood.labels(2)

            dst0 = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC3, 0)
            tsk.pointCloud(tsk.oldrcD.rect).CopyTo(dst0(tsk.oldrcD.rect), tsk.oldrcD.mask)

            heat.Run(dst0)
            dst1 = heat.dst2
            dst3 = heat.dst3

            labels(1) = heat.labels(2)
            labels(3) = heat.labels(3)
        End Sub
    End Class








    Public Class NR_HeatMap_GuidedBP : Inherits TaskParent
        Dim guided As New GuidedBP_Basics
        Public Sub New()
            desc = "This is just a placeholder to make it easy to find the GuidedBP_Basics which shows objects in top/side views."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            guided.Run(src)
            dst2 = guided.dst2
            dst3 = guided.dst3
            labels = guided.labels
        End Sub
    End Class
End Namespace