Imports cv = OpenCvSharp
' https://docs.opencv.org/2.4/modules/imgproc/doc/histograms.html
Public Class Histogram2D_Basics : Inherits VB_Algorithm
    Public histRowsCols() As Integer
    Public ranges() As cv.Rangef
    Public histogram As New cv.Mat
    Public Sub New()
        histRowsCols = {dst2.Height, dst2.Width}
        labels = {"", "", "Plot of 2D histogram (32F format)", "All non-zero entries in the 2D histogram"}
        desc = "Create a 2D histogram from the input."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        ranges = vbHist2Dminmax(src, redOptions.channels(0), redOptions.channels(1))
        cv.Cv2.CalcHist({src}, redOptions.channels, New cv.Mat(), histogram, 2, histRowsCols, ranges)
        dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class





Public Class Histogram2D_PointCloud : Inherits VB_Algorithm
    Dim plot2D As New Plot_Histogram2D
    Public channels() As Integer
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
                        histogram, 2, {redOptions.HistBinSlider.Value, redOptions.HistBinSlider.Value}, ranges)

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





' https://docs.opencv.org/2.4/modules/imgproc/doc/histograms.html
Public Class Histogram2D_HSV : Inherits VB_Algorithm
    Public hist2D As New Histogram2D_Basics
    Public histogram As New cv.Mat
    Public ranges() As cv.Rangef
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        gOptions.HistBinSlider.Value = 256
        labels = {"", "HSV image", "", ""}
        desc = "Create a 2D histogram from an hsv input Mat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        hist2D.Run(dst1)
        dst2 = hist2D.histogram
        dst3 = hist2D.dst3

        histogram = dst2
        ranges = hist2D.ranges

        labels(3) = ""
        If redOptions.channels(0) = 0 Then labels(2) += "Hue is on the X-Axis (0-180), "
        If redOptions.channels(0) = 1 Then labels(2) += "Saturation is on the X-Axis (0-255), "
        If redOptions.channels(1) = 1 Then labels(2) += "Saturation is on the Y-Axis (0-255)"
        If redOptions.channels(1) = 2 Then labels(2) += "Value is on the Y-Axis (0-255)"
        labels(2) = labels(2) + " (in 32F format)"
    End Sub
End Class







Public Class Histogram2D_BGR : Inherits VB_Algorithm
    Dim hist2D As New Histogram2D_Basics
    Public histogram As New cv.Mat
    Public ranges() As cv.Rangef
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        gOptions.HistBinSlider.Value = 256
        desc = "Create a 2D histogram from an BGR input Mat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2D.Run(src)
        dst2 = hist2D.histogram
        dst3 = hist2D.dst3

        histogram = dst2
        ranges = hist2D.ranges

        labels(2) = ""
        If redOptions.channels(0) = 0 Then labels(2) += "Blue is on the X-Axis, "
        If redOptions.channels(0) = 1 Then labels(2) += "Green is on the X-Axis, "
        If redOptions.channels(1) = 1 Then labels(2) += "Green is on the Y-Axis"
        If redOptions.channels(1) = 2 Then labels(2) += "Red is on the Y-Axis"
        labels(3) = labels(2)
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






Public Class Histogram2D_DepthOld : Inherits VB_Algorithm
    Dim hist2d As New Histogram2D_Basics
    Public channels() As Integer
    Public ranges() As cv.Rangef
    Public histogram As New cv.Mat
    Public Sub New()
        desc = "Create 2D histogram from the 3D pointcloud - use options to select dimensions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim xInput = task.pcSplit(redOptions.channels(0))
        Dim yInput = task.pcSplit(redOptions.channels(1))

        Dim mmX As mmData, mmY As mmData
        xInput.MinMaxLoc(mmX.minVal, mmX.maxVal, mmX.minLoc, mmX.maxLoc, task.depthMask)
        Static maxX = mmX.maxVal, minX = mmX.minVal

        yInput.MinMaxLoc(mmY.minVal, mmY.maxVal, mmY.minLoc, mmY.maxLoc, task.depthMask)
        Static maxY = mmY.maxVal, minY = mmY.minVal
        If task.heartBeat Then
            maxX = mmX.maxVal
            minX = mmX.minVal
            maxY = mmX.maxVal
            minY = mmX.minVal
        End If

        maxX = 2 'Math.Max(maxX, mmX.maxVal)
        minX = -2 ' Math.Min(minX, mmX.minVal)

        maxY = 2 ' Math.Max(maxY, mmY.maxVal)
        minY = -2 ' Math.Min(minY, mmY.minVal)

        ranges = New cv.Rangef() {New cv.Rangef(minX, maxX), New cv.Rangef(minY, maxY)}
        cv.Cv2.CalcHist({task.pointCloud}, redOptions.channels, New cv.Mat(), histogram, 2,
                        {task.histogramBins, task.histogramBins}, ranges)

        dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        dst3 = histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

        labels = {"", "", "Mask of the 2D histogram for selected channels", "Mask of 2D histogram after thresholding"}
        channels = redOptions.channels
    End Sub
End Class

