Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
' https://docs.opencvb.org/2.4/modules/imgproc/doc/histograms.html
Namespace VBClasses
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
        Public Shared Function GetHist2Dminmax(input As cv.Mat, chan1 As Integer, chan2 As Integer) As cv.Rangef()
            If input.Type = cv.MatType.CV_8UC3 Then
                ' ranges are exclusive in OpenCV 
                Return {New cv.Rangef(-histDelta, 256),
                    New cv.Rangef(-histDelta, 256)}
            End If

            Dim xInput = input.ExtractChannel(chan1)
            Dim yInput = input.ExtractChannel(chan2)

            Dim mmX = GetMinMax(xInput)
            Dim mmY = GetMinMax(yInput)

            ' ranges are exclusive in OpenCV 
            Return {New cv.Rangef(mmX.minVal - histDelta, mmX.maxVal + histDelta),
                New cv.Rangef(mmY.minVal - histDelta, mmY.maxVal + histDelta)}
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
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
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim r1 As cv.Vec2f, r2 As cv.Vec2f
            If taskA.channels(0) = 0 Or taskA.channels(0) = 1 Then
                r1 = New cv.Vec2f(-taskA.xRangeDefault, taskA.xRangeDefault)
            End If
            If taskA.channels(1) = 1 Then r2 = New cv.Vec2f(-taskA.yRangeDefault, taskA.yRangeDefault)
            If taskA.channels(1) = 2 Then r2 = New cv.Vec2f(0, taskA.MaxZmeters)

            ranges = New cv.Rangef() {New cv.Rangef(r1.Item0, r1.Item1),
                                  New cv.Rangef(r2.Item0, r2.Item1)}
            cv.Cv2.CalcHist({taskA.pointCloud}, taskA.channels, New cv.Mat(),
                        histogram, 2, {taskA.histogramBins, taskA.histogramBins}, ranges)

            plot1D.Run(histogram)
            dst2 = plot1D.dst2
            channels = taskA.channels
        End Sub
    End Class






    Public Class NR_Hist2D_Depth : Inherits TaskParent
        Dim hist2d As New Hist2D_Cloud
        Public channels() As Integer
        Public ranges() As cv.Rangef
        Public histogram As New cv.Mat
        Public Sub New()
            desc = "Create 2D histogram from the 3D pointcloud - use options to select dimensions."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hist2d.Run(taskA.pointCloud)

            histogram = hist2d.histogram
            ranges = hist2d.ranges
            channels = taskA.channels

            dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            dst3 = histogram.Threshold(taskA.projectionThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

            labels = {"", "", "Mask of the 2D histogram for selected channels", "Mask of 2D histogram after thresholding"}
        End Sub
    End Class









    Public Class NR_Hist2D_Zoom : Inherits TaskParent
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
            cv.Cv2.CalcHist({src}, {0, 2}, taskA.depthMask, histogram02, 2, histRowsCols, taskA.rangesHSV)
            dst2 = histogram02.Threshold(0, 255, cv.ThresholdTypes.Binary)

            cv.Cv2.CalcHist({src}, {0, 1}, taskA.depthMask, histogram01, 2, histRowsCols, taskA.rangesHSV)
            dst3 = histogram01.Threshold(0, 255, cv.ThresholdTypes.Binary)

            labels(2) = "Hue is on the X-Axis and Value is on the Y-Axis"
            labels(3) = "Hue is on the X-Axis and Saturation is on the Y-Axis"
        End Sub
    End Class






    Public Class Hist2D_BGR : Inherits TaskParent
        Public histogram01 As New cv.Mat
        Public histogram02 As New cv.Mat
        Public Sub New()
            taskA.gOptions.setHistogramBins(256)
            desc = "Create a 2D histogram for blue to red and blue to green."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim histRowsCols = {dst2.Height, dst2.Width}
            cv.Cv2.CalcHist({src}, {0, 2}, taskA.depthMask, histogram02, 2, histRowsCols, taskA.rangesBGR)
            dst2 = histogram02.Threshold(0, 255, cv.ThresholdTypes.Binary)

            cv.Cv2.CalcHist({src}, {0, 1}, taskA.depthMask, histogram01, 2, histRowsCols, taskA.rangesBGR)
            dst3 = histogram01.Threshold(0, 255, cv.ThresholdTypes.Binary)

            labels(2) = "Blue is on the X-Axis and Red is on the Y-Axis"
            labels(3) = "Blue is on the X-Axis and Green is on the Y-Axis"
        End Sub
    End Class






    Public Class NR_Hist2D_PlotHistogram1D : Inherits TaskParent
        Dim histogram As New cv.Mat
        Dim plotHist As New Plot_Histogram
        Public Sub New()
            plotHist.removeZeroEntry = False
            labels(2) = "NR_Hist2D_PlotHistogram1D output shown with plot_histogram"
            desc = "Create a 2D histogram for blue to red and blue to green."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            cv.Cv2.CalcHist({src}, taskA.channels, taskA.depthMask, histogram, 2,
                        {taskA.histogramBins, taskA.histogramBins}, taskA.rangesBGR)
            dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary)

            plotHist.Run(histogram)
            dst3 = plotHist.dst2
        End Sub
    End Class
End Namespace