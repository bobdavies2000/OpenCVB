Imports cv = OpenCvSharp
' https://docs.opencv.org/2.4/modules/imgproc/doc/histograms.html
Public Class Histogram2D_Basics : Inherits VB_Algorithm
    Public histRowsCols() As Integer
    Public ranges() As cv.Rangef
    Public histogram As New cv.Mat
    Public channels() As Integer = {0, 2}
    Public Sub New()
        histRowsCols = {dst2.Height, dst2.Width}
        labels = {"", "", "Plot of 2D histogram (32F format)", "All non-zero entries in the 2D histogram"}
        desc = "Create a 2D histogram from the input."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        ranges = vbHist2Dminmax(src, channels(0), channels(1))
        cv.Cv2.CalcHist({src}, channels, New cv.Mat(), histogram, 2, histRowsCols, ranges)
        dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class





Public Class Histogram2D_PointCloud : Inherits VB_Algorithm
    Dim plot2D As New Plot_Histogram2D
    Dim channels() As Integer
    Public ranges() As cv.Rangef
    Public histogram As New cv.Mat
    Public Sub New()
        labels = {"", "", "Plot of 2D histogram", "All non-zero entries in the 2D histogram"}
        desc = "Create a 2D histogram of the point cloud data - which 2D inputs is in options."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim r1 As cv.Vec2f, r2 As cv.Vec2f
        If redOptions.channels(0) = 0 Or redOptions.channels(0) = 1 Then
            r1 = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
        End If
        If redOptions.channels(1) = 1 Then r2 = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
        If redOptions.channels(1) = 2 Then r2 = New cv.Vec2f(0, task.maxZmeters)

        ranges = New cv.Rangef() {New cv.Rangef(r1.Item0, r1.Item1),
                                  New cv.Rangef(r2.Item0, r2.Item1)}
        cv.Cv2.CalcHist({task.pointCloud}, redOptions.channels, New cv.Mat(),
                        histogram, 2, {gOptions.HistBinSlider.Value, gOptions.HistBinSlider.Value}, ranges)

        plot2D.Run(histogram)
        dst2 = plot2D.dst2
        channels = redOptions.channels
    End Sub
End Class






Public Class Histogram2D_Depth : Inherits VB_Algorithm
    Dim hist2d As New Histogram2D_PointCloud
    Public channels() As Integer
    Public ranges() As cv.Rangef
    Public histogram As New cv.Mat
    Public Sub New()
        desc = "Create 2D histogram from the 3D pointcloud - use options to select dimensions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(task.pointCloud)

        histogram = hist2d.histogram
        ranges = hist2d.ranges
        channels = redOptions.channels

        dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        dst3 = histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

        labels = {"", "", "Mask of the 2D histogram for selected channels", "Mask of 2D histogram after thresholding"}
    End Sub
End Class









Public Class Histogram2D_Zoom : Inherits VB_Algorithm
    Dim hist2d As New Histogram2D_Basics
    Dim zoom As New Magnify_Basics
    Public Sub New()
        labels = {"", "", "Mask of histogram", "DrawRect area from the histogram"}
        desc = "Draw a rectangle on an area to zoom in on..."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)
        dst2 = hist2d.dst2

        zoom.Run(hist2d.histogram)
        dst3 = zoom.dst3
    End Sub
End Class





Public Class Histogram2D_Side : Inherits VB_Algorithm
    Dim autoY As New OpAuto_YRange
    Public histogram As New cv.Mat
    Public Sub New()
        labels(2) = "ZY (Side View)"
        desc = "Create a 2D side view for ZY histogram of depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cv.Cv2.CalcHist({task.pointCloud}, task.channelsSide, New cv.Mat, histogram, 2, task.bins2D, task.rangesSide)
        histogram.Col(0).SetTo(0)

        autoY.Run(histogram)

        dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        dst3 = histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        labels(3) = "Y-range = " + Format(task.yRange, fmt2)
    End Sub
End Class






Public Class Histogram2D_Top : Inherits VB_Algorithm
    Dim autoX As New OpAuto_XRange
    Public histogram As New cv.Mat
    Public Sub New()
        labels(2) = "XZ (Top View)"
        desc = "Create a 2D top view for XZ histogram of depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cv.Cv2.CalcHist({task.pointCloud}, task.channelsTop, New cv.Mat, histogram, 2, task.bins2D, task.rangesTop)
        histogram.Row(0).SetTo(0)

        If task.useXYRange Then autoX.Run(histogram)

        dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        dst3 = histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        labels(3) = "X-range = " + Format(task.xRange, fmt2)
    End Sub
End Class







' https://docs.opencv.org/2.4/modules/imgproc/doc/histograms.html
Public Class Histogram2D_HSV : Inherits VB_Algorithm
    Public hist2D As New Histogram2D_Basics
    Public histogram01 As New cv.Mat
    Public histogram02 As New cv.Mat
    Public ranges01() As cv.Rangef
    Public ranges02() As cv.Rangef
    Public Sub New()
        gOptions.HistBinSlider.Value = 256
        labels = {"", "HSV image", "", ""}
        desc = "Create a 2D histogram from an hsv input Mat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        hist2D.channels = {0, 2}
        hist2D.Run(dst1)
        ranges02 = hist2D.ranges
        dst2 = hist2D.histogram.Threshold(0, 255, cv.ThresholdTypes.Binary)
        histogram02 = hist2D.histogram

        hist2D.channels = {0, 1}
        hist2D.Run(dst1)
        histogram01 = hist2D.histogram
        ranges01 = hist2D.ranges
        dst3 = hist2D.histogram.Threshold(0, 255, cv.ThresholdTypes.Binary)

        labels(2) = "Value is on the X-Axis and Hue is on the Y-Axis"
        labels(3) = "Saturation is on the X-Axis and Hue is on the Y-Axis"
    End Sub
End Class







Public Class Histogram2D_BGR : Inherits VB_Algorithm
    Dim hist2D As New Histogram2D_Basics
    Public histogram01 As New cv.Mat
    Public histogram02 As New cv.Mat
    Public ranges01() As cv.Rangef
    Public ranges02() As cv.Rangef
    Public Sub New()
        gOptions.HistBinSlider.Value = 256
        desc = "Create a 2D histogram for blue to red and blue to green."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2D.channels = {0, 2}
        hist2D.Run(src)
        ranges02 = hist2D.ranges
        dst2 = hist2D.histogram.Threshold(0, 255, cv.ThresholdTypes.Binary)
        histogram02 = hist2D.histogram

        hist2D.channels = {0, 1}
        hist2D.Run(src)
        histogram01 = hist2D.histogram
        ranges01 = hist2D.ranges
        dst3 = hist2D.histogram.Threshold(0, 255, cv.ThresholdTypes.Binary)

        labels(2) = "Red is on the X-Axis and Blue is on the X-Axis"
        labels(3) = "Green is on the X-Axis and Blue is on the X-Axis"
    End Sub
End Class