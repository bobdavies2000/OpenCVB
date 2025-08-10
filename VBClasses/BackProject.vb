Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
' https://docs.opencvb.org/3.4/dc/df6/tutorial_py_Hist_backprojection.html
Public Class BackProject_Basics : Inherits TaskParent
    Public hist As New Hist_Basics
    Public minRange As cv.Scalar, maxRange As cv.Scalar
    Public Sub New()
        labels(2) = "Move mouse to backproject a histogram column"
        desc = "Mouse over any bin to see the histogram backprojected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.grayStable
        hist.Run(src)
        If hist.mm.minVal = hist.mm.maxVal Then
            SetTrueText("The input image is empty - mm.minVal and mm.maxVal are both zero...")
            Exit Sub
        End If

        dst2 = hist.dst2

        Dim totalPixels = dst2.Total ' assume we are including zeros.
        Dim brickWidth = dst2.Width / task.histogramBins
        Dim incr = (hist.mm.maxVal - hist.mm.minVal) / task.histogramBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / brickWidth)

        minRange = New cv.Scalar(histIndex * incr)
        maxRange = New cv.Scalar((histIndex + 1) * incr)
        If histIndex + 1 = task.histogramBins Then maxRange = New cv.Scalar(255)

        '     Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}
        '     cv.Cv2.CalcBackProject({task.gray}, {0}, histK.hist.histogram, dst0, ranges)
        ' for single dimension histograms, backprojection is the same as inrange
        ' (and this works for backproject_FeatureLess below)
        dst0 = src.InRange(minRange, maxRange)

        Dim actualCount = dst0.CountNonZero
        dst3 = task.color.Clone
        dst3.SetTo(cv.Scalar.Yellow, dst0)
        Dim count = hist.histogram.Get(Of Single)(CInt(histIndex), 0)
        Dim histMax As mmData = GetMinMax(hist.histogram)
        labels(3) = $"Backprojecting {CInt(minRange(0))} to {CInt(maxRange(0))} with {CInt(count)} of {totalPixels} compared to " +
                    $"mask pixels = {actualCount}.  Histogram max count = {CInt(histMax.maxVal)}"
        dst2.Rectangle(New cv.Rect(CInt(histIndex) * brickWidth, 0, brickWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class







Public Class BackProject_Reduction : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim bProject As New BackProject_Basics
    Public Sub New()
        OptionParent.findRadio("Use Simple Reduction").Checked = True
        labels(3) = "Backprojection of highlighted histogram bin"
        desc = "Use the histogram of a reduced BGR image to isolate featureless portions of an image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)

        bProject.Run(reduction.dst2)
        dst2 = bProject.dst2
        dst3 = bProject.dst3
        labels(2) = "Reduction = " + CStr(reduction.options.simpleReductionValue) + " and bins = " + CStr(task.histogramBins)
    End Sub
End Class







Public Class BackProject_FeatureLess : Inherits TaskParent
    Dim bProject As New BackProject_Basics
    Public Sub New()
        labels(3) = "Move mouse over the histogram to backproject a column"
        desc = "Create a histogram of the featureless regions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bProject.Run(task.contours.contourMap)
        dst2 = bProject.dst2
        dst3 = bProject.dst3
        labels(2) = "Bins = " + CStr(task.histogramBins)
    End Sub
End Class










Public Class BackProject_PointCloud : Inherits TaskParent
    Public hist As New Hist_PointCloud_XZ_YZ
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC3, 0)
        labels = {"", "", "Backprojection after histogram binning X and Z values", "Backprojection after histogram binning Y and Z values"}
        desc = "Explore Backprojection of the cloud histogram."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim threshold = hist.options.threshold
        hist.Run(src)

        dst0 = hist.dst2.Threshold(threshold, 255, cv.ThresholdTypes.Binary)
        dst1 = hist.dst3.Threshold(threshold, 255, cv.ThresholdTypes.Binary)

        dst2 = New cv.Mat(hist.dst2.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        dst3 = New cv.Mat(hist.dst3.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))

        Dim mask As New cv.Mat
        cv.Cv2.CalcBackProject({task.pointCloud}, {0, 2}, dst0, mask, hist.rangesX)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        task.pointCloud.CopyTo(dst2, mask)

        cv.Cv2.CalcBackProject({task.pointCloud}, {1, 2}, dst1, mask, hist.rangesY)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        task.pointCloud.CopyTo(dst3, mask)
    End Sub
End Class









Public Class BackProject_DisplayColor : Inherits TaskParent
    Dim backP As New BackProject_Full
    Public Sub New()
        task.gOptions.setHistogramBins(10)
        labels = {"", "", "Back projection", ""}
        desc = "Display the back projected color image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        backP.Run(src)
        dst2 = ShowPalette(backP.dst2 + 1)
        labels(2) = backP.labels(2)
    End Sub
End Class







Public Class BackProject_Unstable : Inherits TaskParent
    Dim backP As New BackProject_Full
    Dim diff As New Diff_Basics
    Public Sub New()
        task.gOptions.pixelDiffThreshold = 6
        labels = {"", "", "Backprojection output", "Unstable pixels in the backprojection.  If flashing, set 'Color Difference Threshold' higher."}
        desc = "Highlight the unstable pixels in the backprojection."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        backP.Run(src)
        dst2 = ShowPalette(backP.dst2)

        diff.Run(dst2)
        dst3 = diff.dst2
    End Sub
End Class











Public Class BackProject_FullEqualized : Inherits TaskParent
    Dim backP As New BackProject_Full
    Dim equalize As New Hist_EqualizeColor
    Public Sub New()
        labels = {"", "", "BackProject_Full output without equalization", "BackProject_Full with equalization"}
        desc = "Create a histogram from the equalized color and then backproject it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        backP.Run(src)
        backP.dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2 = ShowPalette(dst2)

        equalize.Run(task.grayStable)
        backP.Run(equalize.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        backP.dst2.ConvertTo(dst3, cv.MatType.CV_8U)
        dst3 = ShowPalette(dst3)
    End Sub
End Class









Public Class BackProject_Side : Inherits TaskParent
    Dim histSide As New Projection_HistSide
    Public Sub New()
        labels = {"", "", "Hotspots in the Side View", "Back projection of the hotspots in the Side View"}
        desc = "Display the back projection of the hotspots in the Side View"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histSide.Run(src)
        dst2 = histSide.dst2

        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histSide.histogram, dst3, task.rangesSide)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class






Public Class BackProject_Top : Inherits TaskParent
    Dim histTop As New Projection_HistTop
    Public Sub New()
        labels = {"", "", "Hotspots in the Top View", "Back projection of the hotspots in the Top View"}
        desc = "Display the back projection of the hotspots in the Top View"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histTop.Run(src)
        dst2 = histTop.dst2

        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histTop.histogram, dst3, task.rangesTop)
        dst3 = ShowPalette(dst3.ConvertScaleAbs)
    End Sub
End Class







Public Class BackProject_Horizontal : Inherits TaskParent
    Dim bpTop As New BackProject_Top
    Dim bpSide As New BackProject_Side
    Public Sub New()
        desc = "Use both the BackProject_Top to improve the results of the BackProject_Side for finding flat surfaces."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bpTop.Run(src)
        task.pointCloud.SetTo(0, bpTop.dst3)

        bpSide.Run(src)
        dst2 = bpSide.dst3
    End Sub
End Class










Public Class BackProject_SoloSide : Inherits TaskParent
    Dim histSide As New Projection_HistSide
    Public Sub New()
        labels = {"", "", "Solo samples in the Side View", "Back projection of the solo samples in the Side View"}
        desc = "Display the back projection of the solo samples in the Side View"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histSide.Run(src)

        dst3 = histSide.histogram.Threshold(1, 255, cv.ThresholdTypes.TozeroInv)
        dst2 = dst3.ConvertScaleAbs(255)

        histSide.histogram.SetTo(0, Not dst2)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histSide.histogram, dst3, task.rangesSide)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class






Public Class BackProject_SoloTop : Inherits TaskParent
    Dim histTop As New Projection_HistTop
    Public Sub New()
        labels = {"", "", "Solo samples in the Top View", "Back projection of the solo samples in the Top View"}
        desc = "Display the back projection of the solo samples in the Top View"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histTop.Run(src)

        dst3 = histTop.histogram.Threshold(1, 255, cv.ThresholdTypes.TozeroInv)
        dst2 = dst3.ConvertScaleAbs(255)

        histTop.histogram.SetTo(0, Not dst2)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histTop.histogram, dst3, task.rangesTop)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class






' https://docs.opencvb.org/3.4/dc/df6/tutorial_py_Hist_backprojection.html
Public Class BackProject_Image : Inherits TaskParent
    Public hist As New Hist_Basics
    Public mask As New cv.Mat
    Public useInrange As Boolean
    Public Sub New()
        task.kalman = New Kalman_Basics
        labels(2) = "Move mouse to backproject each histogram column"
        desc = "Explore Backprojection of each element of a grayscale histogram."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hist.Run(task.grayStable)
        If hist.mm.minVal = hist.mm.maxVal Then
            SetTrueText("The input image is empty - mm.minval and mm.maxVal are both zero...")
            Exit Sub ' the input image is empty...
        End If
        dst2 = hist.dst2

        If task.kalman.kInput.Length <> 2 Then ReDim task.kalman.kInput(2 - 1)
        task.kalman.kInput(0) = hist.mm.minVal
        task.kalman.kInput(1) = hist.mm.maxVal
        task.kalman.Run(emptyMat)
        hist.mm.minVal = Math.Min(task.kalman.kOutput(0), task.kalman.kOutput(1))
        hist.mm.maxVal = Math.Max(task.kalman.kOutput(0), task.kalman.kOutput(1))

        Dim totalPixels = dst2.Total ' assume we are including zeros.
        If hist.plotHist.removeZeroEntry Then totalPixels = task.gray.CountNonZero

        Dim brickWidth = dst2.Width / task.histogramBins
        Dim incr = (hist.mm.maxVal - hist.mm.minVal) / task.histogramBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / brickWidth)

        Dim minRange = New cv.Scalar(histIndex * incr)
        Dim maxRange = New cv.Scalar((histIndex + 1) * incr + 1)
        If histIndex + 1 = task.histogramBins Then
            minRange = New cv.Scalar(254)
            maxRange = New cv.Scalar(255)
        End If
        If useInrange Then
            If histIndex = 0 And hist.plotHist.removeZeroEntry Then
                mask = New cv.Mat(task.grayStable.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            Else
                mask = task.grayStable.InRange(minRange, maxRange)
            End If
        Else
            Dim bRange = New cv.Rangef(minRange(0), maxRange(0))
            Dim ranges() = New cv.Rangef() {bRange}
            cv.Cv2.CalcBackProject({task.grayStable}, {0}, hist.histogram, mask, ranges)
        End If
        dst3 = src
        If mask.Type <> cv.MatType.CV_8U Then mask.ConvertTo(mask, cv.MatType.CV_8U)
        dst3.SetTo(cv.Scalar.Yellow, mask)
        Dim actualCount = mask.CountNonZero
        Dim count = hist.histogram.Get(Of Single)(histIndex, 0)
        Dim histMax As mmData = GetMinMax(hist.histogram)
        labels(3) = "Backprojecting " + CStr(CInt(minRange(0))) + " to " + CStr(CInt(maxRange(0))) + " with " +
                     CStr(count) + " histogram samples and " + CStr(actualCount) + " mask count.  Histogram max count = " +
                     CStr(CInt(histMax.maxVal))
        dst2.Rectangle(New cv.Rect(CInt(histIndex * brickWidth), 0, brickWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class





Public Class BackProject_Mouse : Inherits TaskParent
    Dim backP As New BackProject_Image
    Public Sub New()
        labels(2) = "Use the mouse to select what should be shown in the backprojection of the depth histogram"
        desc = "Use the mouse to select what should be shown in the backprojection of the depth histogram"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        backP.Run(src)
        dst2 = backP.dst2
        dst3 = backP.dst3
    End Sub
End Class





Public Class BackProject_MeterByMeter : Inherits TaskParent
    Dim histogram As New cv.Mat
    Public Sub New()
        desc = "Backproject the depth data at 1 meter intervals without a histogram."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            Dim histData As New List(Of Single)
            For i = 0 To task.histogramBins - 1
                histData.Add(i + 1)
            Next

            histogram = cv.Mat.FromPixelData(task.histogramBins, 1, cv.MatType.CV_32F, histData.ToArray)
        End If
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, task.histogramBins)}
        cv.Cv2.CalcBackProject({task.pcSplit(2)}, {0}, histogram, dst2, ranges)

        dst2.SetTo(0, task.noDepthMask)
        dst3 = ShowPalette(dst2.ConvertScaleAbs)
        labels(2) = "CV_8U backprojection up to " + CStr(task.histogramBins) + " meters."
    End Sub
End Class








Public Class BackProject_Hue : Inherits TaskParent
    Dim hue As New OEX_CalcBackProject_Demo1
    Public classCount As Integer
    Public Sub New()
        desc = "Create an 8UC1 image with a backprojection of the hue."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hue.Run(src)
        classCount = hue.classCount
        dst2 = hue.dst2
        dst3 = ShowPalette(dst2)
    End Sub
End Class








Public Class BackProject_MaskLines : Inherits TaskParent
    Dim masks As New BackProject_Masks
    Dim lines As New Line_Raw
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "lines detected in the backProjection mask", "Histogram of pixels in a grayscale image.  Move mouse to see lines detected in the backprojection mask",
                  "Yellow is backProjection, lines detected are highlighted"}
        desc = "Inspect the lines from individual backprojection masks from a histogram"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        masks.Run(src)
        dst2 = masks.dst2
        dst3 = src.Clone

        Static saveHistIndex As Integer = masks.histIndex
        If masks.histIndex <> saveHistIndex Then dst1.SetTo(0)

        lines.Run(masks.mask)

        For Each lp In lines.lpList
            Dim val = masks.dst3.Get(Of Byte)(lp.p1.Y, lp.p1.X)
            If val = 255 Then DrawLine(dst1, lp.p1, lp.p2, white)
        Next
        dst3.SetTo(cv.Scalar.Yellow, masks.mask)
        dst3.SetTo(task.highlight, dst1)
    End Sub
End Class







Public Class BackProject_Masks : Inherits TaskParent
    Public hist As New Hist_Basics
    Public histIndex As Integer
    Public mask As New cv.Mat
    Public Sub New()
        labels(2) = "Histogram for the gray scale image.  Move mouse to see backprojection of each grayscale mask."
        desc = "Create all the backprojection masks from a grayscale histogram"
    End Sub
    Public Function maskDetect(gray As cv.Mat, histIndex As Integer) As cv.Mat
        Dim brickWidth = dst2.Width / hist.histogram.Rows
        Dim brickRange = 255 / hist.histogram.Rows

        Dim minRange = If(histIndex = hist.histogram.Rows - 1, 255 - brickRange, histIndex * brickRange)
        Dim maxRange = If(histIndex = hist.histogram.Rows - 1, 255, (histIndex + 1) * brickRange)
        If Single.IsNaN(minRange) Or Single.IsInfinity(minRange) Or
           Single.IsNaN(maxRange) Or Single.IsInfinity(maxRange) Then
            SetTrueText("Input data has no values - exit " + traceName)
            Return New cv.Mat
        End If

        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

        cv.Cv2.CalcBackProject({gray}, {0}, hist.histogram, mask, ranges)
        Return mask
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        hist.Run(src)
        dst2 = hist.dst2

        Dim brickWidth = dst2.Width / task.histogramBins
        histIndex = Math.Floor(task.mouseMovePoint.X / brickWidth)

        dst3 = task.color.Clone
        dst1 = maskDetect(task.gray, histIndex)
        If dst1.Width = 0 Then Exit Sub
        dst3.SetTo(white, dst1)
        dst2.Rectangle(New cv.Rect(CInt(histIndex * brickWidth), 0, brickWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class




Public Class BackProject_MaskList : Inherits TaskParent
    Public histList As New List(Of List(Of Single))
    Public histogramList As New List(Of cv.Mat)
    Dim inputMatList As New List(Of cv.Mat)
    Dim histS As New Hist_DepthSimple
    Dim plotHist As New Plot_Histogram
    Public Sub New()
        plotHist.addLabels = False
        plotHist.removeZeroEntry = True
        task.gOptions.setHistogramBins(40)
        task.gOptions.DebugSlider.Minimum = 0
        labels(2) = "Use the debug slider (global options) to test various depth levels."
        labels(3) = "Depth mask used to build the depth histogram at left"
        desc = "Create masks for each histogram bin backprojection"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim bins = If(task.histogramBins <= 255, task.histogramBins - 1, 255)
        Dim incr = 255 / bins
        If bins <> task.gOptions.DebugSlider.Maximum Then
            task.gOptions.DebugSlider.Value = 0
            task.gOptions.DebugSlider.Maximum = bins
        End If

        If standalone Then
            Static depthIndex As Integer
            If task.heartBeat Then
                depthIndex += 1
                If depthIndex > 10 Then depthIndex = 0
                task.gOptions.DebugSlider.Value = depthIndex
            End If
        End If
        histList.Clear()
        histogramList.Clear()
        inputMatList.Clear()
        histS.ranges = {New cv.Rangef(0 - 0.01, task.MaxZmeters + 0.01)}
        For i = 0 To bins - 2
            Dim minVal = i * incr
            Dim maxVal = (i + 1) * incr
            histS.inputOnlyMask = task.gray.InRange(minVal, maxVal)
            histS.Run(task.pcSplit(2))
            histList.Add(New List(Of Single)(histS.histList))
            histogramList.Add(histS.histogram.Clone)
            inputMatList.Add(histS.inputOnlyMask.Clone)
        Next
        Dim index = Math.Min(bins, task.gOptions.DebugSlider.Value)
        If index >= inputMatList.Count Then index = inputMatList.Count - 1
        Dim tmp = inputMatList(index)
        If task.heartBeat Then strOut = CStr(tmp.CountNonZero) + " mask pixels between " + CStr(incr * index) + " and " +
                                        CStr(incr * (index + 1)) + " from " + CStr(task.pcSplit(2).CountNonZero) + " depth pixels"
        plotHist.Run(histogramList(index))
        dst2 = plotHist.dst2
        dst3 = inputMatList(index)
    End Sub
End Class





' https://docs.opencvb.org/3.4/da/d7f/tutorial_back_projection.html
Public Class BackProject_FullOld : Inherits TaskParent
    Public classCount As Integer
    Public ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 255)}
    Public Sub New()
        task.gOptions.setHistogramBins(10)
        labels = {"", "", "CV_8U format of the backprojection", "dst2 presented with a palette"}
        desc = "Create a color histogram, normalize it, and backproject it with a palette."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        classCount = task.histogramBins

        Dim histogram As New cv.Mat
        cv.Cv2.CalcHist({task.grayStable}, {0}, New cv.Mat, histogram, 1, {classCount}, ranges)
        histogram = histogram.Normalize(0, classCount, cv.NormTypes.MinMax)

        cv.Cv2.CalcBackProject({src}, {0}, histogram, dst2, ranges)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        If standaloneTest() Then dst3 = ShowPalette(dst2)
    End Sub
End Class







Public Class BackProject_InRangeDepthTest : Inherits TaskParent
    Public classCount As Integer
    Public Sub New()
        task.gOptions.setHistogramBins(4)
        desc = "An alternative way to get the depth histogram using InRange instead of CalcHist.."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        classCount = task.histogramBins

        Static index As Integer
        Dim binSize = task.MaxZmeters / task.histogramBins
        Dim maxRange = index * binSize
        Dim minRange = (index - 1) * binSize
        If index < 1 Then
            minRange = 0
            maxRange = 0
        ElseIf index = 1 Then
            minRange = 0.1
        End If

        If index = 0 Then
            dst2 = task.noDepthMask
        Else
            dst2 = task.pcSplit(2).InRange(minRange, maxRange).ConvertScaleAbs
            If maxRange >= task.MaxZmeters Then dst2 = dst2 Or task.maxDepthMask
        End If

        labels(2) = "Histogram bin " + CStr(index) + " for range from " + Format(minRange, fmt1) + " m to " +
                    Format(maxRange, fmt1) + " m had " + CStr(dst2.CountNonZero)
        If task.heartBeatLT And task.frameCount > 1 Then index += 1
        If maxRange > task.MaxZmeters Then index = 0
    End Sub
End Class








Public Class BackProject_InRangeDepth : Inherits TaskParent
    Public classCount As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "An alternative way to get the depth histogram using InRange instead of CalcHist."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        classCount = task.histogramBins

        Dim binSize = task.MaxZmeters / task.histogramBins
        Dim binCounts As New List(Of Integer)
        For i = 1 To classCount
            Dim maxRange = i * binSize
            Dim minRange = (i - 1) * binSize
            If i = 1 Then minRange = 0.01
            dst1 = task.pcSplit(2).InRange(minRange, maxRange).ConvertScaleAbs * i
            If maxRange >= task.MaxZmeters Then dst1 = dst1 Or task.maxDepthMask
            binCounts.Add(dst1.CountNonZero)
            dst2.SetTo(i, dst1)
        Next
        dst2.SetTo(0, task.noDepthMask)

        strOut = ""
        For i = 0 To binCounts.Count - 1
            strOut += "Class " + CStr(i) + " had " + CStr(binCounts(i)) + " pixels." + vbCrLf
        Next
        SetTrueText(strOut)

        dst3 = ShowPalette(dst2)
        labels(3) = "Below are the " + CStr(task.histogramBins) + " classes of depth data."
    End Sub
End Class








' https://docs.opencvb.org/3.4/da/d7f/tutorial_back_projection.html
Public Class BackProject_Full : Inherits TaskParent
    Public classCount As Integer
    Dim plotHist As New Plot_Histogram
    Dim index As Integer
    Public Sub New()
        task.gOptions.setHistogramBins(10)
        plotHist.createHistogram = True
        plotHist.removeZeroEntry = False
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Create a histogram for the grayscale image, uniquely identify each bin, and backproject it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.grayStable
        classCount = task.histogramBins
        plotHist.Run(src)
        dst1 = plotHist.dst2

        Dim histogram = plotHist.histogram
        For i = 0 To classCount
            histogram.Set(Of Single)(i, 0, i)
        Next

        cv.Cv2.CalcBackProject({src}, {0}, histogram, dst2, plotHist.ranges)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        labels(2) = "CV_8U backprojection of the " + CStr(classCount) + " histogram bins."
        If standaloneTest() Then
            If task.heartBeatLT Then index += 1
            If index >= classCount Then index = 0
            dst3 = dst2.InRange(index, index)
            labels(3) = "Class " + CStr(index) + " had " + CStr(plotHist.histArray(index)) + " pixels after backprojection."
        End If
    End Sub
End Class







Public Class BackProject_FullDepth : Inherits TaskParent
    Public classCount As Integer
    Dim plotHist As New Plot_Histogram
    Public Sub New()
        task.gOptions.setHistogramBins(20)
        plotHist.createHistogram = True
        plotHist.removeZeroEntry = False
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Create a histogram for the depth image, uniquely identify each bin, and backproject it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2).Clone
        classCount = task.histogramBins + 1

        src.SetTo(task.MaxZmeters, task.maxDepthMask)
        plotHist.Run(src)
        dst1 = plotHist.dst2

        For i = 0 To classCount - 1
            plotHist.histogram.Set(Of Single)(i, 0, i + 1)
        Next

        Dim histArray(plotHist.histogram.Rows - 1) As Single
        Marshal.Copy(plotHist.histogram.Data, histArray, 0, histArray.Length)

        cv.Cv2.CalcBackProject({src}, {0}, plotHist.histogram, dst2, plotHist.ranges)
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        labels(2) = "CV_8U backprojection of the " + CStr(classCount) + " histogram bins."
        If standaloneTest() Then
            Static index As Integer
            If task.heartBeatLT Then index += 1
            If index >= classCount Then index = 0
            dst3 = dst2.InRange(index, index)
            labels(3) = "Class " + CStr(index) + " had " + CStr(plotHist.histArray(index)) + " pixels after backprojection."
        End If
    End Sub
End Class







Public Class BackProject_Basics_Depth : Inherits TaskParent
    Public bpDepth As New BackProject_FullDepth
    Public Sub New()
        task.gOptions.setHistogramBins(20)
        desc = "Create a histogram for the depth image, uniquely identify each bin, and backproject it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bpDepth.Run(src)
        dst2 = ShowPalette(bpDepth.dst2)
        dst2.SetTo(0, task.noDepthMask)
        labels(2) = bpDepth.labels(2)
    End Sub
End Class







Public Class BackProject_DepthSlider : Inherits TaskParent
    Public bpDepth As New BackProject_FullDepth
    Public Sub New()
        task.gOptions.setHistogramBins(20)
        desc = "Create a histogram for the depth image, uniquely identify each bin, and backproject it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bpDepth.Run(src)
        dst2 = ShowPalette(bpDepth.dst2)
        dst2.SetTo(0, task.noDepthMask)
        labels(2) = bpDepth.labels(2)

        Dim index = Math.Abs(task.gOptions.DebugSlider.Value)
        If index >= bpDepth.classCount Then index = bpDepth.classCount - 1
        dst3 = bpDepth.dst2.InRange(index, index)
        Dim count = dst3.CountNonZero
        labels(3) = "Class " + CStr(index) + " had " + CStr(count) + " pixels after backprojection."
    End Sub
End Class
