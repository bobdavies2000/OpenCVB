Imports cv = OpenCvSharp
Public Class HistPeak2D_Basics : Inherits TaskParent
    Public auto As New OpAuto_Peaks2DGrid
    Dim bgr As New Hist2D_BGR
    Dim delaunay As New Delaunay_ConsistentColor
    Public histogram As New cv.Mat
    Public ranges() As cv.Rangef
    Public Sub New()
        If standalone Then algTask.gOptions.displaydst1.checked = true
        desc = "Find the top X peaks in a 2D histogram and use Delaunay to setup the backprojection"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        ' if standaloneTest(), go get a histogram for input.  Src is the 3-channel input to the histogram.
        If standaloneTest() Then
            bgr.Run(src)
            histogram = bgr.histogram02
        End If
        If algTask.heartBeat Then
            auto.Run(histogram)
            delaunay.inputPoints = New List(Of cv.Point2f)(auto.clusterPoints)
            delaunay.Run(src)
            dst1 = auto.dst2
            dst3 = delaunay.dst2
        End If

        Dim mask = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        delaunay.dst1.ConvertTo(histogram, cv.MatType.CV_32F)
        histogram.SetTo(0, Not mask)

        If ranges Is Nothing Or algTask.optionsChanged Then
            ' the pcsplit arrays have been patched for inf and nan's already in algTask.vb.
            If algTask.settings.cameraName.StartsWith("StereoLabs") Then cv.Cv2.Merge(algTask.pcSplit, src)
            ranges = GetHist2Dminmax(src, algTask.channels(0), algTask.channels(1))
        End If

        Dim backProjection As New cv.Mat
        cv.Cv2.CalcBackProject({src}, algTask.channels, histogram, backProjection, ranges)
        dst2 = PaletteFull(backProjection)
    End Sub
End Class









Public Class HistPeak2D_TopAndSide : Inherits TaskParent
    Dim peak As New HistPeak2D_Basics
    Dim histSide As New Projection_HistSide
    Dim histTop As New Projection_HistTop
    Public Sub New()
        desc = "Find the top X peaks in the 2D histogram of the top and side views and backproject them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If algTask.toggleOn Then
            histSide.Run(src)
            peak.ranges = algTask.rangesSide
            algTask.channels = algTask.channelsSide
            peak.histogram = histSide.histogram
        Else
            histTop.Run(src)
            algTask.channels = algTask.channelsTop
            peak.ranges = algTask.rangesTop
            peak.histogram = histTop.histogram
        End If
        peak.Run(algTask.pointCloud)
        dst1 = peak.dst2
        dst2 = PaletteFull(dst1)
    End Sub
End Class









Public Class HistPeak2D_NotHotTop : Inherits TaskParent
    Public histTop As New Projection_HistTop
    Dim peak As New HistPeak2D_Basics
    Public Sub New()
        desc = "Find the regions with the non-zero (low) samples in the top view"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histTop.Run(src)
        dst1 = histTop.histogram.InRange(0, 0).ConvertScaleAbs

        Dim mm As mmData = GetMinMax(histTop.histogram)
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_32F, mm.maxVal)
        dst3 -= histTop.histogram
        dst3.SetTo(0, dst1)

        peak.histogram = histTop.histogram
        peak.Run(algTask.pointCloud)
        dst2 = peak.dst2
    End Sub
End Class







Public Class HistPeak2D_Edges : Inherits TaskParent
    Dim peak As New HistPeak2D_Basics
    Dim histTop As New Projection_HistTop
    Dim edges As New Edge_Basics
    Public Sub New()
        desc = "Display the HistPeak2D_Basics edges in the RGB image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histTop.Run(src)

        dst3 = histTop.histogram.Threshold(algTask.projectionThreshold, 255, cv.ThresholdTypes.Binary)
        peak.histogram = histTop.histogram
        peak.Run(algTask.pointCloud)
        dst2 = peak.dst2

        edges.Run(dst2)
        dst3 = src
        dst3.SetTo(white, edges.dst2)
    End Sub
End Class







Public Class HistPeak2D_HSV : Inherits TaskParent
    Dim hsv As New Hist2D_HSV
    Dim peak As New HistPeak2D_Basics
    Public Sub New()
        desc = "Find the peaks in the 2D plot of the HSV image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hsv.Run(src)
        peak.histogram = hsv.histogram01
        peak.Run(hsv.dst1)
        dst2 = peak.dst2
        dst3 = peak.auto.dst2
        labels(3) = hsv.labels(2)
    End Sub
End Class







Public Class HistPeak2D_BGR : Inherits TaskParent
    Dim bgr As New Hist2D_BGR
    Dim peak As New HistPeak2D_Basics
    Public Sub New()
        desc = "Find the peaks in the 2D plot of the BGR image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bgr.Run(src)

        peak.histogram = bgr.histogram02
        peak.Run(src)
        dst2 = peak.dst2
        dst3 = peak.auto.dst2

        labels(3) = bgr.labels(2)
    End Sub
End Class







Public Class HistPeak2D_RGB : Inherits TaskParent
    Dim peak As New HistPeak2D_BGR
    Public Sub New()
        desc = "Find the peaks in the 2D plot of the BGR image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        peak.Run(src)
        dst2 = peak.dst2
        dst3 = peak.dst3

        labels(3) = peak.labels(2)
    End Sub
End Class







Public Class HistPeak2D_HotSide : Inherits TaskParent
    Dim peak As New HistPeak2D_Basics
    Dim histSide As New Projection_HistSide
    Public Sub New()
        labels = {"", "", "Backprojection of Side View hotspots", "Side view with highlighted hot spots"}
        desc = "Find the top X peaks in the 2D histogram of the side view and backproject it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histSide.Run(src)
        dst3 = histSide.histogram

        For i = 0 To peak.auto.clusterPoints.Count - 1
            Dim pt = peak.auto.clusterPoints(i)
            DrawCircle(dst3, pt, algTask.DotSize * 3, white)
        Next

        peak.histogram = histSide.histogram
        peak.ranges = algTask.rangesSide
        algTask.channels = algTask.channelsSide
        peak.Run(algTask.pointCloud)
        dst2 = peak.dst2
        dst2.SetTo(0, algTask.noDepthMask)
    End Sub
End Class






Public Class HistPeak2D_HotTop : Inherits TaskParent
    Dim peak As New HistPeak2D_Basics
    Dim histTop As New Projection_HistTop
    Public Sub New()
        labels = {"", "", "Backprojection of Top View hotspots", "Top view with highlighted hot spots"}
        desc = "Find the top X peaks in the 2D histogram of the top view and backproject it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histTop.Run(src)
        dst3 = histTop.histogram

        For i = 0 To peak.auto.clusterPoints.Count - 1
            Dim pt = peak.auto.clusterPoints(i)
            DrawCircle(dst3, pt, algTask.DotSize * 3, white)
        Next

        peak.histogram = histTop.histogram
        peak.ranges = algTask.rangesTop
        algTask.channels = algTask.channelsTop
        peak.Run(algTask.pointCloud)
        dst2 = peak.dst2
        dst2.SetTo(0, algTask.noDepthMask)
    End Sub
End Class