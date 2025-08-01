﻿Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
' https://docs.opencvb.org/2.4/modules/imgproc/doc/histograms.html
Public Class Hist2D_Basics : Inherits TaskParent
    Public histRowsCols() As Integer
    Public ranges() As cv.Rangef
    Public histogram As New cv.Mat
    Public channels() As Integer = {0, 2}
    Public Sub New()
        histRowsCols = {dst2.Height, dst2.Width}
        labels = {"", "", "All non-zero entries in the 2D histogram", ""}
        desc = "Create a 2D histogram from the input."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        ranges = GetHist2Dminmax(src, channels(0), channels(1))
        cv.Cv2.CalcHist({src}, channels, New cv.Mat(), histogram, 2, histRowsCols, ranges)
        dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)
    End Sub
End Class





Public Class Hist2D_Cloud : Inherits TaskParent
    Dim plot1D As New Plot_Histogram2D
    Dim channels() As Integer
    Public ranges() As cv.Rangef
    Public histogram As New cv.Mat
    Public Sub New()
        labels = {"", "", "Plot of 2D histogram", "All non-zero entries in the 2D histogram"}
        desc = "Create a 2D histogram of the point cloud data - which 2D inputs is in options."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim r1 As cv.Vec2f, r2 As cv.Vec2f
        If task.channels(0) = 0 Or task.channels(0) = 1 Then
            r1 = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
        End If
        If task.channels(1) = 1 Then r2 = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
        If task.channels(1) = 2 Then r2 = New cv.Vec2f(0, task.MaxZmeters)

        ranges = New cv.Rangef() {New cv.Rangef(r1.Item0, r1.Item1),
                                  New cv.Rangef(r2.Item0, r2.Item1)}
        cv.Cv2.CalcHist({task.pointCloud}, task.channels, New cv.Mat(),
                        histogram, 2, {task.histogramBins, task.histogramBins}, ranges)

        plot1D.Run(histogram)
        dst2 = plot1D.dst2
        channels = task.channels
    End Sub
End Class






Public Class Hist2D_Depth : Inherits TaskParent
    Dim hist2d As New Hist2D_Cloud
    Public channels() As Integer
    Public ranges() As cv.Rangef
    Public histogram As New cv.Mat
    Public Sub New()
        desc = "Create 2D histogram from the 3D pointcloud - use options to select dimensions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hist2d.Run(task.pointCloud)

        histogram = hist2d.histogram
        ranges = hist2d.ranges
        channels = task.channels

        dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        dst3 = histogram.Threshold(task.projectionThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

        labels = {"", "", "Mask of the 2D histogram for selected channels", "Mask of 2D histogram after thresholding"}
    End Sub
End Class









Public Class Hist2D_Zoom : Inherits TaskParent
    Dim hist2d As New Hist2D_Basics
    Dim zoom As New Magnify_Basics
    Public Sub New()
        labels = {"", "", "Mask of histogram", "DrawRect area from the histogram"}
        desc = "Draw a rectangle on an area to zoom in on..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hist2d.Run(src)
        dst2 = hist2d.dst2

        zoom.Run(hist2d.histogram)
        dst3 = zoom.dst3
    End Sub
End Class









' https://docs.opencvb.org/2.4/modules/imgproc/doc/histograms.html
Public Class Hist2D_HSV : Inherits TaskParent
    Public histogram01 As New cv.Mat
    Public histogram02 As New cv.Mat
    Public Sub New()
        labels = {"", "HSV image", "", ""}
        desc = "Create a 2D histogram for Hue to Saturation and Hue to Value."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histRowsCols = {dst2.Height, dst2.Width}

        src = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        cv.Cv2.CalcHist({src}, {0, 2}, task.depthMask, histogram02, 2, histRowsCols, task.rangesHSV)
        dst2 = histogram02.Threshold(0, 255, cv.ThresholdTypes.Binary)

        cv.Cv2.CalcHist({src}, {0, 1}, task.depthMask, histogram01, 2, histRowsCols, task.rangesHSV)
        dst3 = histogram01.Threshold(0, 255, cv.ThresholdTypes.Binary)

        labels(2) = "Hue is on the X-Axis and Value is on the Y-Axis"
        labels(3) = "Hue is on the X-Axis and Saturation is on the Y-Axis"
    End Sub
End Class






Public Class Hist2D_BGR : Inherits TaskParent
    Public histogram01 As New cv.Mat
    Public histogram02 As New cv.Mat
    Public Sub New()
        task.gOptions.setHistogramBins(256)
        desc = "Create a 2D histogram for blue to red and blue to green."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histRowsCols = {dst2.Height, dst2.Width}
        cv.Cv2.CalcHist({src}, {0, 2}, task.depthMask, histogram02, 2, histRowsCols, task.rangesBGR)
        dst2 = histogram02.Threshold(0, 255, cv.ThresholdTypes.Binary)

        cv.Cv2.CalcHist({src}, {0, 1}, task.depthMask, histogram01, 2, histRowsCols, task.rangesBGR)
        dst3 = histogram01.Threshold(0, 255, cv.ThresholdTypes.Binary)

        labels(2) = "Blue is on the X-Axis and Red is on the Y-Axis"
        labels(3) = "Blue is on the X-Axis and Green is on the Y-Axis"
    End Sub
End Class






Public Class Hist2D_PlotHistogram1D : Inherits TaskParent
    Dim histogram As New cv.Mat
    Dim plotHist As New Plot_Histogram
    Public Sub New()
        plotHist.removeZeroEntry = False
        labels(2) = "Hist2D_PlotHistogram1D output shown with plot_histogram"
        desc = "Create a 2D histogram for blue to red and blue to green."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        cv.Cv2.CalcHist({src}, task.channels, task.depthMask, histogram, 2,
                        {task.histogramBins, task.histogramBins}, task.rangesBGR)
        dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary)

        plotHist.Run(histogram)
        dst3 = plotHist.dst2
    End Sub
End Class