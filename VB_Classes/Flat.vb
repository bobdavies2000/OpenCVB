Imports cv = OpenCvSharp
Public Class Flat_Basics : Inherits VB_Algorithm
    Public options As New Options_HeatMap
    Dim sum32f As New History_Sum32f
    Public Sub New()
        labels = {"", "Top down mask after after thresholding heatmap", "Vertical regions", "Horizontal regions"}
        desc = "Find the regions that are mostly vertical and mostly horizontal."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim histSize() = {dst2.Height, dst2.Width}
        Dim topHist As New cv.Mat, sideHist As New cv.Mat, topBackP As New cv.Mat, sideBackP As New cv.Mat
        cv.Cv2.CalcHist({task.pointCloud}, options.channelsTop, New cv.Mat, topHist, 2, histSize, options.rangesTop)
        topHist.Row(0).SetTo(0)
        cv.Cv2.InRange(topHist, options.redThreshold, topHist.Total, dst1)
        dst1.ConvertTo(dst1, cv.MatType.CV_32F)
        cv.Cv2.CalcBackProject({task.pointCloud}, options.channelsTop, dst1, topBackP, options.rangesTop)

        sum32f.Run(topBackP)
        sum32f.dst2.ConvertTo(dst2, cv.MatType.CV_8U)

        dst3 = Not dst2
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class







Public Class Flat_SingleTons : Inherits VB_Algorithm
    Dim singleton As New PointCloud_Singleton
    Dim sum32 As New History_Sum32f
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"RGB image with highlights for likely floor or ceiling over X frames.",
                  "Heatmap top view - flipped for backprojection", "Single frame backprojection of singleton points",
                  "Thresholded heatmap top view mask"}
        desc = "Use the singleton points to find flat surfaces anywhere in the gravity-oriented point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static options = singleton.heat.core.options
        singleton.Run(src)
        dst3 = singleton.dst2
        dst1 = singleton.heat.dst0.Clone
        dst1.SetTo(0, Not dst3)
        dst1 = dst1.Flip(cv.FlipMode.X)

        cv.Cv2.CalcBackProject({task.pointCloud}, options.channelsTop, dst1, dst2, options.rangesTop)

        sum32.Run(dst2)
        sum32.dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        dst0 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        task.color.SetTo(cv.Scalar.White, dst0)
    End Sub
End Class






Public Class Flat_Histogram : Inherits VB_Algorithm
    Dim flat As New Flat_SingleTons
    Dim hist As New Histogram_Basics
    Public peakCeiling As Single
    Public peakFloor As Single
    Public ceilingPop As Single
    Public floorPop As Single
    Public Sub New()
        labels = {"", "", "Histogram of Y-Values of the point cloud after masking", "Mask used to isolate histogram input"}
        desc = "Create a histogram plot of the Y-values in the backprojection of singletons."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flat.Run(src)
        dst3 = flat.dst3

        Dim points = dst3.FindNonZero()
        Dim yList As New List(Of Single)
        For i = 0 To points.Rows - 1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            Dim yVal = task.pcSplit(1).Get(Of Single)(pt.Y, pt.X)
            If yVal <> 0 Then yList.Add(yVal)
        Next

        If yList.Count = 0 Then Exit Sub
        hist.srcMin = yList.Min
        hist.srcMax = yList.Max
        hist.Run(New cv.Mat(yList.Count, 1, cv.MatType.CV_32F, yList.ToArray))
        dst2 = hist.dst2
        Dim binWidth As Single = dst2.Width / task.histogramBins
        Dim rangePerBin = (hist.srcMax - hist.srcMin) / task.histogramBins

        Dim midHist = task.histogramBins / 2
        Dim mm = vbMinMax(hist.histogram(New cv.Rect(0, midHist, 1, midHist)))
        floorPop = mm.maxVal
        Dim peak = hist.srcMin + (midHist + mm.maxLoc.Y + 1) * rangePerBin
        Dim rX As Integer = (midHist + mm.maxLoc.Y) * binWidth
        dst2.Rectangle(New cv.Rect(rX, 0, binWidth, dst2.Height), cv.Scalar.Black, task.lineWidth)
        If Math.Abs(peak - peakCeiling) > rangePerBin Then peakCeiling = peak

        mm = vbMinMax(hist.histogram(New cv.Rect(0, 0, 1, midHist)))
        ceilingPop = mm.maxVal
        peak = hist.srcMin + (mm.maxLoc.Y + 1) * rangePerBin
        rX = mm.maxLoc.Y * binWidth
        dst2.Rectangle(New cv.Rect(rX, 0, binWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
        If Math.Abs(peak - peakFloor) > rangePerBin * 2 Then peakFloor = peak

        labels(3) = "Peak Ceiling = " + Format(peakCeiling, fmt3) + " and Peak Floor = " + Format(peakFloor, fmt3)
        setTrueText("Yellow rectangle is likely floor and black is likely ceiling.")
    End Sub
End Class






Public Class Flat_Verticals : Inherits VB_Algorithm
    Dim singleton As New PointCloud_Singleton
    Dim sum32 As New History_Sum32f
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"RGB image with highlights for likely vertical surfaces over X frames.",
                  "Heatmap top view", "Single frame backprojection of red areas in the heatmap",
                  "Thresholded heatmap top view mask - flipped for backprojection"}
        desc = "Use a heatmap to isolate vertical walls."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static redSlider = findSlider("Threshold for Red channel")
        Static options = singleton.heat.core.options

        singleton.Run(src)
        cv.Cv2.InRange(singleton.heat.topSum.dst2, redSlider.value * gOptions.FrameHistory.Value, dst2.Total, dst3)

        dst3 = dst3.Flip(cv.FlipMode.X)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32FC1, 0)
        singleton.heat.dst0.CopyTo(dst1, dst3)

        cv.Cv2.CalcBackProject({task.pointCloud}, options.channelsTop, dst1, dst2, options.rangesTop)

        sum32.Run(dst2)
        sum32.dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2 = sum32.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst0, cv.MatType.CV_8U)
        task.color.SetTo(cv.Scalar.White, dst0)

        dst1 = singleton.heat.dst2
    End Sub
End Class






Public Class Flat_Horizontals : Inherits VB_Algorithm
    Dim singleton As New PointCloud_Singleton
    Dim sum32 As New History_Sum32f
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"RGB image with highlights for likely floor or ceiling over X frames.",
                  "Heatmap side view", "Single frame backprojection read areas in the heatmap",
                  "Thresholded heatmap top view mask - flipped for backprojection"}
        desc = "Use the singleton points to isolate horizont surfaces - floor or ceiling or table tops."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static redSlider = findSlider("Threshold for Red channel")
        Static options = singleton.heat.core.options

        singleton.Run(src)
        cv.Cv2.InRange(singleton.heat.sideSum.dst2, redSlider.value * gOptions.FrameHistory.Value, dst2.Total, dst3)

        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32FC1, 0)
        singleton.heat.dst1.CopyTo(dst1, dst3)

        cv.Cv2.CalcBackProject({task.pointCloud}, options.channelsSide, dst1, dst2, options.rangesSide)

        sum32.Run(dst2)
        sum32.dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2 = sum32.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst0, cv.MatType.CV_8U)
        task.color.SetTo(cv.Scalar.White, dst0)

        dst1 = singleton.heat.dst3
    End Sub
End Class