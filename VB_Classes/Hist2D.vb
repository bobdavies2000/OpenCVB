Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp
' https://docs.opencvb.org/2.4/modules/imgproc/doc/histograms.html
Public Class Hist2D_Basics : Inherits TaskParent
    Public histRowsCols() As Integer
    Public ranges() As cvb.Rangef
    Public histogram As New cvb.Mat
    Public channels() As Integer = {0, 2}
    Public Sub New()
        histRowsCols = {dst2.Height, dst2.Width}
        labels = {"", "", "All non-zero entries in the 2D histogram", ""}
        desc = "Create a 2D histogram from the input."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        ranges = GetHist2Dminmax(src, channels(0), channels(1))
        cvb.Cv2.CalcHist({src}, channels, New cvb.Mat(), histogram, 2, histRowsCols, ranges)
        dst2 = histogram.Threshold(0, 255, cvb.ThresholdTypes.Binary)
        dst2.ConvertTo(dst2, cvb.MatType.CV_8U)
    End Sub
End Class





Public Class Hist2D_Cloud : Inherits TaskParent
    Dim plot1D As New Plot_Histogram2D
    Dim channels() As Integer
    Public ranges() As cvb.Rangef
    Public histogram As New cvb.Mat
    Public Sub New()
        labels = {"", "", "Plot of 2D histogram", "All non-zero entries in the 2D histogram"}
        desc = "Create a 2D histogram of the point cloud data - which 2D inputs is in options."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        Dim r1 As cvb.Vec2f, r2 As cvb.Vec2f
        If task.redOptions.channels(0) = 0 Or task.redOptions.channels(0) = 1 Then
            r1 = New cvb.Vec2f(-task.xRangeDefault, task.xRangeDefault)
        End If
        If task.redOptions.channels(1) = 1 Then r2 = New cvb.Vec2f(-task.yRangeDefault, task.yRangeDefault)
        If task.redOptions.channels(1) = 2 Then r2 = New cvb.Vec2f(0, task.MaxZmeters)

        ranges = New cvb.Rangef() {New cvb.Rangef(r1.Item0, r1.Item1),
                                  New cvb.Rangef(r2.Item0, r2.Item1)}
        cvb.Cv2.CalcHist({task.pointCloud}, task.redOptions.channels, New cvb.Mat(),
                        histogram, 2, {task.histogramBins, task.histogramBins}, ranges)

        plot1D.Run(histogram)
        dst2 = plot1D.dst2
        channels = task.redOptions.channels
    End Sub
End Class






Public Class Hist2D_Depth : Inherits TaskParent
    Dim hist2d As New Hist2D_Cloud
    Public channels() As Integer
    Public ranges() As cvb.Rangef
    Public histogram As New cvb.Mat
    Public Sub New()
        desc = "Create 2D histogram from the 3D pointcloud - use options to select dimensions."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        hist2d.Run(task.pointCloud)

        histogram = hist2d.histogram
        ranges = hist2d.ranges
        channels = task.redOptions.channels

        dst2 = histogram.Threshold(0, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
        dst3 = histogram.Threshold(task.projectionThreshold, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs

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
    Public Overrides sub runAlg(src As cvb.Mat)
        hist2d.Run(src)
        dst2 = hist2d.dst2

        zoom.Run(hist2d.histogram)
        dst3 = zoom.dst3
    End Sub
End Class









' https://docs.opencvb.org/2.4/modules/imgproc/doc/histograms.html
Public Class Hist2D_HSV : Inherits TaskParent
    Public histogram01 As New cvb.Mat
    Public histogram02 As New cvb.Mat
    Public Sub New()
        labels = {"", "HSV image", "", ""}
        desc = "Create a 2D histogram for Hue to Saturation and Hue to Value."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        Dim histRowsCols = {dst2.Height, dst2.Width}

        src = src.CvtColor(cvb.ColorConversionCodes.BGR2HSV)
        cvb.Cv2.CalcHist({src}, {0, 2}, task.depthMask, histogram02, 2, histRowsCols, task.redOptions.rangesHSV)
        dst2 = histogram02.Threshold(0, 255, cvb.ThresholdTypes.Binary)

        cvb.Cv2.CalcHist({src}, {0, 1}, task.depthMask, histogram01, 2, histRowsCols, task.redOptions.rangesHSV)
        dst3 = histogram01.Threshold(0, 255, cvb.ThresholdTypes.Binary)

        labels(2) = "Hue is on the X-Axis and Value is on the Y-Axis"
        labels(3) = "Hue is on the X-Axis and Saturation is on the Y-Axis"
    End Sub
End Class






Public Class Hist2D_BGR : Inherits TaskParent
    Public histogram01 As New cvb.Mat
    Public histogram02 As New cvb.Mat
    Public Sub New()
        task.gOptions.setHistogramBins(256)
        desc = "Create a 2D histogram for blue to red and blue to green."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        Dim histRowsCols = {dst2.Height, dst2.Width}
        cvb.Cv2.CalcHist({src}, {0, 2}, task.depthMask, histogram02, 2, histRowsCols, task.redOptions.rangesBGR)
        dst2 = histogram02.Threshold(0, 255, cvb.ThresholdTypes.Binary)

        cvb.Cv2.CalcHist({src}, {0, 1}, task.depthMask, histogram01, 2, histRowsCols, task.redOptions.rangesBGR)
        dst3 = histogram01.Threshold(0, 255, cvb.ThresholdTypes.Binary)

        labels(2) = "Blue is on the X-Axis and Red is on the Y-Axis"
        labels(3) = "Blue is on the X-Axis and Green is on the Y-Axis"
    End Sub
End Class






Public Class Hist2D_PlotHistogram1D : Inherits TaskParent
    Dim histogram As New cvb.Mat
    Dim plot As New Plot_Histogram
    Public histArray() As Single
    Public Sub New()
        plot.removeZeroEntry = False
        labels(2) = "Hist2D_PlotHistogram1D output shown with plot_histogram"
        desc = "Create a 2D histogram for blue to red and blue to green."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        cvb.Cv2.CalcHist({src}, task.redOptions.channels, task.depthMask, histogram, 2, {task.histogramBins, task.histogramBins},
                        task.redOptions.rangesBGR)
        dst2 = histogram.Threshold(0, 255, cvb.ThresholdTypes.Binary)

        plot.Run(histogram)
        dst3 = plot.dst2

        'ReDim histArray(histogram.Total - 1)
        'Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
    End Sub
End Class