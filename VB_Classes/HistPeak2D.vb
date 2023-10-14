Imports cv = OpenCvSharp
Public Class HistPeak2D_Basics : Inherits VB_Algorithm
    Public auto As New OpAuto_Peaks2DGrid
    Dim delaunay As New Delaunay_Basics
    Public options As New Options_Histogram2D
    Public histogram As New cv.Mat
    Public ranges() As cv.Rangef
    Public Sub New()
        desc = "Find the top X peaks in a 2D histogram and use Delaunay to setup the backprojection"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        ' if standalone, go get a histogram for input.  Src is the 3-channel input to the histogram.
        If standalone Then
            Static bgr As New Histogram2D_BGR
            bgr.Run(src)
            histogram = bgr.histogram
        End If
        If firstPass Or task.optionsChanged Or (heartBeat() And src.Type = cv.MatType.CV_32FC3) Then
            auto.Run(histogram)
            delaunay.inputPoints = New List(Of cv.Point2f)(auto.clusterPoints)
            delaunay.Run(src)
        End If

        Dim mask = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        delaunay.dst1.ConvertTo(histogram, cv.MatType.CV_32F)
        histogram.SetTo(0, Not mask)

        If ranges Is Nothing Or task.optionsChanged Then
            options.RunVB()
            ranges = vbHist2Dminmax(src, options.channels(0), options.channels(1))
        End If

        Dim backProjection As New cv.Mat
        cv.Cv2.CalcBackProject({src}, options.channels, histogram, backProjection, ranges)
        dst2 = vbPalette(backProjection * 255 / delaunay.inputPoints.Count)
    End Sub
End Class








Public Class HistPeak2D_HotSide : Inherits VB_Algorithm
    Dim peak As New HistPeak2D_Basics
    Dim hist2d As New Histogram2D_Side
    Public Sub New()
        labels = {"", "", "Backprojection of Side View hotspots", "Side view with highlighted hot spots"}
        desc = "Find the top X peaks in the 2D histogram of the side view and backproject it."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)
        dst3 = hist2d.histogram

        For i = 0 To peak.auto.clusterPoints.Count - 1
            Dim pt = peak.auto.clusterPoints(i)
            dst3.Circle(pt, task.dotSize * 3, cv.Scalar.White, -1, task.lineType)
        Next

        peak.histogram = hist2d.histogram
        peak.ranges = task.rangesSide
        peak.options.channels = task.channelsSide
        peak.Run(task.pointCloud)
        dst2 = peak.dst2
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class








Public Class HistPeak2D_HotTop : Inherits VB_Algorithm
    Dim peak As New HistPeak2D_Basics
    Dim hist2d As New Histogram2D_Top
    Public Sub New()
        labels = {"", "", "Backprojection of Top View hotspots", "Top view with highlighted hot spots"}
        desc = "Find the top X peaks in the 2D histogram of the top view and backproject it."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)
        dst3 = hist2d.histogram

        For i = 0 To peak.auto.clusterPoints.Count - 1
            Dim pt = peak.auto.clusterPoints(i)
            dst3.Circle(pt, task.dotSize * 3, cv.Scalar.White, -1, task.lineType)
        Next

        peak.histogram = hist2d.histogram
        peak.ranges = task.rangesTop
        peak.options.channels = task.channelsTop
        peak.Run(task.pointCloud)
        dst2 = peak.dst2
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class








Public Class HistPeak2D_TopAndSide : Inherits VB_Algorithm
    Dim peak As New HistPeak2D_Basics
    Dim histSide As New Histogram2D_Side
    Dim histTop As New Histogram2D_Top
    Public Sub New()
        desc = "Find the top X peaks in the 2D histogram of the top and side views and backproject them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.toggleEverySecond Then
            histSide.Run(src)
            peak.ranges = task.rangesSide
            peak.options.channels = task.channelsSide
            peak.histogram = histSide.histogram
        Else
            histTop.Run(src)
            peak.options.channels = task.channelsTop
            peak.ranges = task.rangesTop
            peak.histogram = histTop.histogram
        End If
        peak.Run(task.pointCloud)
        dst1 = peak.dst2
        dst2 = vbPalette(dst1)
    End Sub
End Class









Public Class HistPeak2D_NotHotTop : Inherits VB_Algorithm
    Public hist2d As New Histogram2D_Top
    Dim peak As New HistPeak2D_Basics
    Public Sub New()
        desc = "Find the regions with the non-zero (low) samples in the top view"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)
        dst1 = hist2d.histogram.InRange(0, 0).ConvertScaleAbs

        Dim mm = vbMinMax(hist2d.histogram)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32F, mm.maxVal)
        dst3 -= hist2d.histogram
        dst3.SetTo(0, dst1)

        peak.histogram = hist2d.histogram
        peak.Run(task.pointCloud)
        dst2 = peak.dst2
    End Sub
End Class







Public Class HistPeak2D_Edges : Inherits VB_Algorithm
    Dim peak As New HistPeak2D_Basics
    Dim hist2d As New Histogram2D_Top
    Dim edges As New Edge_Canny
    Public Sub New()
        desc = "Display the HistPeak2D_Basics edges in the RGB image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)

        dst3 = hist2d.histogram.Threshold(task.redThresholdTop, 255, cv.ThresholdTypes.Binary)
        peak.histogram = hist2d.histogram
        peak.Run(task.pointCloud)
        dst2 = peak.dst2

        edges.Run(dst2)
        dst3 = src
        dst3.SetTo(cv.Scalar.White, edges.dst2)
    End Sub
End Class







Public Class HistPeak2D_HSV : Inherits VB_Algorithm
    Dim hsv As New Histogram2D_HSV
    Dim peak As New HistPeak2D_Basics
    Public Sub New()
        desc = "Find the peaks in the 2D plot of the HSV image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hsv.Run(src)
        peak.histogram = hsv.histogram
        peak.Run(hsv.dst1)
        dst2 = peak.dst2
        dst3 = peak.auto.dst2
        labels(3) = hsv.labels(2)
    End Sub
End Class







Public Class HistPeak2D_BGR : Inherits VB_Algorithm
    Dim bgr As New Histogram2D_BGR
    Dim peak As New HistPeak2D_Basics
    Public Sub New()
        desc = "Find the peaks in the 2D plot of the BGR image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bgr.Run(src)

        peak.histogram = bgr.histogram
        peak.Run(src)
        dst2 = peak.dst2
        dst3 = peak.auto.dst2

        labels(3) = bgr.labels(2)
    End Sub
End Class







Public Class HistPeak2D_RGB : Inherits VB_Algorithm
    Dim peak As New HistPeak2D_BGR
    Public Sub New()
        desc = "Find the peaks in the 2D plot of the BGR image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        peak.Run(src)
        dst2 = peak.dst2
        dst3 = peak.dst3

        labels(3) = peak.labels(2)
    End Sub
End Class
