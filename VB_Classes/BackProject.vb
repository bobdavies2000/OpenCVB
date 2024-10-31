Imports cvb = OpenCvSharp
' https://docs.opencvb.org/3.4/dc/df6/tutorial_py_Hist_backprojection.html
Public Class BackProject_Basics : Inherits TaskParent
    Public histK As New Hist_Kalman
    Public minRange As cvb.Scalar, maxRange As cvb.Scalar
    Public Sub New()
        labels(2) = "Move mouse to backproject a histogram column"
        UpdateAdvice(traceName + ": the global option 'Histogram Bins' controls the histogram.")
        desc = "Mouse over any bin to see the histogram backprojected."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim input = src.Clone
        If input.Channels() <> 1 Then input = input.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        histK.Run(input)
        If histK.hist.mm.minVal = histK.hist.mm.maxVal Then
            SetTrueText("The input image is empty - mm.minVal and mm.maxVal are both zero...")
            Exit Sub
        End If

        dst2 = histK.dst2

        Dim totalPixels = dst2.Total ' assume we are including zeros.
        If histK.hist.plot.removeZeroEntry Then totalPixels = input.CountNonZero

        Dim brickWidth = dst2.Width / task.histogramBins
        Dim incr = (histK.hist.mm.maxVal - histK.hist.mm.minVal) / task.histogramBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / brickWidth)

        minRange = New cvb.Scalar(histIndex * incr)
        maxRange = New cvb.Scalar((histIndex + 1) * incr)
        If histIndex + 1 = task.histogramBins Then maxRange = New cvb.Scalar(255)

        '     Dim ranges() = New cvb.Rangef() {New cvb.Rangef(minRange, maxRange)}
        '     cvb.Cv2.CalcBackProject({input}, {0}, histK.hist.histogram, dst0, ranges)
        ' for single dimension histograms, backprojection is the same as inrange (and this works for backproject_FeatureLess below)
        dst0 = input.InRange(minRange, maxRange)

        Dim actualCount = dst0.CountNonZero
        dst3 = task.color.Clone
        dst3.SetTo(cvb.Scalar.Yellow, dst0)
        Dim count = histK.hist.histogram.Get(Of Single)(CInt(histIndex), 0)
        Dim histMax As mmData = GetMinMax(histK.hist.histogram)
        labels(3) = $"Backprojecting {CInt(minRange(0))} to {CInt(maxRange(0))} with {CInt(count)} of {totalPixels} compared to " +
                    $"mask pixels = {actualCount}.  Histogram max count = {CInt(histMax.maxVal)}"
        dst2.Rectangle(New cvb.Rect(CInt(histIndex) * brickWidth, 0, brickWidth, dst2.Height), cvb.Scalar.Yellow, task.lineWidth)
    End Sub
End Class








' https://docs.opencvb.org/3.4/da/d7f/tutorial_back_projection.html
Public Class BackProject_Full : Inherits TaskParent
    Public classCount As Integer
    Public ranges() As cvb.Rangef = New cvb.Rangef() {New cvb.Rangef(0, 255)}
    Public Sub New()
        labels = {"", "", "CV_8U format of the backprojection", "dst2 presented with a palette"}
        desc = "Create a color histogram, normalize it, and backproject it with a palette."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        classCount = task.histogramBins
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cvb.MatType.CV_32F Then src.ConvertTo(src, cvb.MatType.CV_32F)
        Dim histogram As New cvb.Mat
        cvb.Cv2.CalcHist({src}, {0}, New cvb.Mat, histogram, 1, {classCount}, ranges)
        histogram = histogram.Normalize(0, classCount, cvb.NormTypes.MinMax)

        cvb.Cv2.CalcBackProject({src}, {0}, histogram, dst2, ranges)

        dst2.ConvertTo(dst2, cvb.MatType.CV_8U)
        dst3 = ShowPalette(dst2 * 255 / classCount)
    End Sub
End Class









Public Class BackProject_Reduction : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim backP As New BackProject_Basics
    Public Sub New()
        task.redOptions.checkSimpleReduction(True)
        labels(3) = "Backprojection of highlighted histogram bin"
        desc = "Use the histogram of a reduced BGR image to isolate featureless portions of an image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(src)

        backP.Run(reduction.dst2)
        dst2 = backP.dst2
        dst3 = backP.dst3
        labels(2) = "Reduction = " + CStr(task.redOptions.getSimpleReductionBar()) + " and bins = " + CStr(task.histogramBins)
    End Sub
End Class







Public Class BackProject_FeatureLess : Inherits TaskParent
    Dim backP As New BackProject_Basics
    Dim reduction As New Reduction_Basics
    Dim edges As New Edge_ColorGap_CPP_VB
    Public Sub New()
        task.redOptions.BitwiseReduction.Checked = True
        labels = {"", "", "Histogram of the grayscale image at right",
                  "Move mouse over the histogram to backproject a column"}
        desc = "Create a histogram of the featureless regions"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        edges.Run(src)
        reduction.Run(edges.dst3)
        backP.Run(reduction.dst2)
        dst2 = backP.dst2
        dst3 = backP.dst3
        labels(2) = "Reduction = " + CStr(task.redOptions.getSimpleReductionBar()) + " and bins = " + CStr(task.histogramBins)
    End Sub
End Class









Public Class BackProject_BasicsKeyboard : Inherits TaskParent
    Dim keys As New Keyboard_Basics
    Dim backP As New BackProject_Image
    Public Sub New()
        labels(2) = "Move the mouse away from OpenCVB and use the left and right arrows to move between histogram bins."
        desc = "Move the mouse off of OpenCVB and then use the left and right arrow keys move around in the backprojection histogram"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        keys.Run(src)
        Dim keyIn = New List(Of String)(keys.keyInput)
        Dim incrX = dst1.Width / task.histogramBins

        If keyIn.Count Then
            task.mouseMovePointUpdated = True
            For i = 0 To keyIn.Count - 1
                Select Case keyIn(i)
                    Case "Left"
                        task.mouseMovePoint.X -= incrX
                    Case "Right"
                        task.mouseMovePoint.X += incrX
                End Select
            Next
        End If

        backP.Run(src)
        dst2 = backP.dst2
        dst3 = backP.dst3

        ' this is intended to provide a natural behavior for the left and right arrow keys.  The Keyboard_Basics Keyboard Options text box must be active.
        If task.frameCount = 30 Then
            Dim hwnd = FindWindow(Nothing, "OpenCVB Algorithm Options")
            SetForegroundWindow(hwnd)
        End If
    End Sub
End Class







Public Class BackProject_FullLines : Inherits TaskParent
    Dim backP As New BackProject_Full
    Dim lines As New Line_Basics
    Public Sub New()
        task.gOptions.RGBFilterActive.Checked = False
        labels = {"", "", "Lines found in the back projection", "Backprojection results"}
        desc = "Find lines in the back projection"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        backP.Run(src)
        dst3 = backP.dst3

        lines.Run(backP.dst2)
        dst2 = lines.dst2
        For Each lp In lines.lpList
            DrawLine(dst2, lp.p1, lp.p2, cvb.Scalar.White)
        Next
        labels(3) = CStr(lines.lpList.Count) + " lines were found"
    End Sub
End Class









Public Class BackProject_PointCloud : Inherits TaskParent
    Public hist As New Hist_PointCloud
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_32FC3, 0)
        labels = {"", "", "Backprojection after histogram binning X and Z values", "Backprojection after histogram binning Y and Z values"}
        desc = "Explore Backprojection of the cloud histogram."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim threshold = hist.options.threshold
        hist.Run(src)

        dst0 = hist.dst2.Threshold(threshold, 255, cvb.ThresholdTypes.Binary)
        dst1 = hist.dst3.Threshold(threshold, 255, cvb.ThresholdTypes.Binary)

        dst2 = New cvb.Mat(hist.dst2.Size(), cvb.MatType.CV_32F, cvb.Scalar.All(0))
        dst3 = New cvb.Mat(hist.dst3.Size(), cvb.MatType.CV_32F, cvb.Scalar.All(0))

        Dim mask As New cvb.Mat
        cvb.Cv2.CalcBackProject({task.pointCloud}, {0, 2}, dst0, mask, hist.rangesX)
        mask.ConvertTo(mask, cvb.MatType.CV_8U)
        task.pointCloud.CopyTo(dst2, mask)

        cvb.Cv2.CalcBackProject({task.pointCloud}, {1, 2}, dst1, mask, hist.rangesY)
        mask.ConvertTo(mask, cvb.MatType.CV_8U)
        task.pointCloud.CopyTo(dst3, mask)
    End Sub
End Class









Public Class BackProject_Display : Inherits TaskParent
    Dim backP As New BackProject_Full
    Public Sub New()
        labels = {"", "", "Back projection", ""}
        desc = "Display the back projected color image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        backP.Run(src)
        dst2 = backP.dst2
        dst3 = backP.dst3
    End Sub
End Class







Public Class BackProject_Unstable : Inherits TaskParent
    Dim backP As New BackProject_Full
    Dim diff As New Diff_Basics
    Public Sub New()
        task.gOptions.pixelDiffThreshold = 6
        labels = {"", "", "Backprojection output", "Unstable pixels in the backprojection.  If flashing, set 'Pixel Difference Threshold' higher."}
        desc = "Highlight the unstable pixels in the backprojection."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        backP.Run(src)
        dst2 = ShowPalette(backP.dst2 * 255 / backP.classCount)

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
    Public Sub RunAlg(src As cvb.Mat)
        backP.Run(src)
        backP.dst2.ConvertTo(dst2, cvb.MatType.CV_8U)
        Dim mm = GetMinMax(dst2)
        dst2 = ShowPalette(dst2 * 255 / mm.maxVal)

        equalize.Run(src)
        backP.Run(equalize.dst2)

        backP.dst2.ConvertTo(dst3, cvb.MatType.CV_8U)
        mm = GetMinMax(dst3)
        dst3 = ShowPalette(dst3 * 255 / mm.maxVal)
    End Sub
End Class









Public Class BackProject_Side : Inherits TaskParent
    Dim autoY As New OpAuto_YRange
    Dim histSide As New Projection_HistSide
    Public Sub New()
        labels = {"", "", "Hotspots in the Side View", "Back projection of the hotspots in the Side View"}
        desc = "Display the back projection of the hotspots in the Side View"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        histSide.Run(src)
        autoY.Run(histSide.histogram)

        dst2 = autoY.histogram.Threshold(task.projectionThreshold, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
        Dim histogram = autoY.histogram.SetTo(0, Not dst2)
        cvb.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histogram, dst3, task.rangesSide)
        dst3 = dst3.Threshold(0, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class






Public Class BackProject_Top : Inherits TaskParent
    Dim histTop As New Projection_HistTop
    Public Sub New()
        labels = {"", "", "Hotspots in the Top View", "Back projection of the hotspots in the Top View"}
        desc = "Display the back projection of the hotspots in the Top View"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        histTop.Run(src)
        dst2 = histTop.dst2

        Dim histogram = histTop.histogram.SetTo(0, Not dst2)
        cvb.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histogram, dst3, task.rangesTop)
        dst3 = ShowPalette(dst3.ConvertScaleAbs)
    End Sub
End Class







Public Class BackProject_Horizontal : Inherits TaskParent
    Dim bpTop As New BackProject_Top
    Dim bpSide As New BackProject_Side
    Public Sub New()
        desc = "Use both the BackProject_Top to improve the results of the BackProject_Side for finding flat surfaces."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        bpTop.Run(src)
        task.pointCloud.SetTo(0, bpTop.dst3)

        bpSide.Run(src)
        dst2 = bpSide.dst3
    End Sub
End Class










Public Class BackProject_Vertical : Inherits TaskParent
    Dim bpTop As New BackProject_Top
    Dim bpSide As New BackProject_Side
    Public Sub New()
        desc = "Use both the BackProject_Top to improve the results of the BackProject_Side for finding flat surfaces."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        bpSide.Run(src)
        task.pointCloud.SetTo(0, bpSide.dst3)

        bpTop.Run(src)
        dst2 = bpTop.dst3
    End Sub
End Class









Public Class BackProject_SoloSide : Inherits TaskParent
    Dim histSide As New Projection_HistSide
    Public Sub New()
        labels = {"", "", "Solo samples in the Side View", "Back projection of the solo samples in the Side View"}
        desc = "Display the back projection of the solo samples in the Side View"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        histSide.Run(src)

        dst3 = histSide.histogram.Threshold(1, 255, cvb.ThresholdTypes.TozeroInv)
        dst2 = dst3.ConvertScaleAbs(255)

        histSide.histogram.SetTo(0, Not dst2)
        cvb.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histSide.histogram, dst3, task.rangesSide)
        dst3 = dst3.Threshold(0, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class






Public Class BackProject_SoloTop : Inherits TaskParent
    Dim histTop As New Projection_HistTop
    Public Sub New()
        labels = {"", "", "Solo samples in the Top View", "Back projection of the solo samples in the Top View"}
        desc = "Display the back projection of the solo samples in the Top View"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        histTop.Run(src)

        dst3 = histTop.histogram.Threshold(1, 255, cvb.ThresholdTypes.TozeroInv)
        dst2 = dst3.ConvertScaleAbs(255)

        histTop.histogram.SetTo(0, Not dst2)
        cvb.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histTop.histogram, dst3, task.rangesTop)
        dst3 = dst3.Threshold(0, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class






Public Class BackProject_LineTop : Inherits TaskParent
    Dim line As New Line_ViewTop
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Backproject the lines found in the top view."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        line.Run(src)

        dst2.SetTo(0)
        Dim w = task.lineWidth + 5
        For Each lp In line.lines.lpList
            dst2.Line(lp.xp1, lp.xp2, 255, w, task.lineType)
        Next

        Dim histogram = line.autoX.histogram
        histogram.SetTo(0, Not dst2)
        cvb.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histogram, dst3, task.rangesTop)
        dst3 = dst3.Threshold(0, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class








Public Class BackProject_LineSide : Inherits TaskParent
    Dim line As New Line_ViewSide
    Public lpList As New List(Of PointPair)
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Backproject the lines found in the side view."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        line.Run(src)

        dst2.SetTo(0)
        Dim w = task.lineWidth + 5
        lpList.Clear()
        For Each lp In line.lines.lpList
            If Math.Abs(lp.slope) < 0.1 Then
                dst2.Line(lp.xp1, lp.xp2, 255, w, task.lineType)
                lpList.Add(lp)
            End If
        Next

        Dim histogram = line.autoY.histogram
        histogram.SetTo(0, Not dst2)
        cvb.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histogram, dst1, task.rangesSide)
        dst1 = dst1.Threshold(0, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
        dst3 = src
        dst3.SetTo(cvb.Scalar.White, dst1)
    End Sub
End Class





' https://docs.opencvb.org/3.4/dc/df6/tutorial_py_Hist_backprojection.html
Public Class BackProject_Image : Inherits TaskParent
    Public hist As New Hist_Basics
    Public mask As New cvb.Mat
    Dim kalman As New Kalman_Basics
    Public useInrange As Boolean
    Public Sub New()
        labels(2) = "Move mouse to backproject each histogram column"
        desc = "Explore Backprojection of each element of a grayscale histogram."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim input = src
        If input.Channels() <> 1 Then input = input.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        hist.Run(input)
        If hist.mm.minVal = hist.mm.maxVal Then
            SetTrueText("The input image is empty - mm.minval and mm.maxVal are both zero...")
            Exit Sub ' the input image is empty...
        End If
        dst2 = hist.dst2

        If kalman.kInput.Length <> 2 Then ReDim kalman.kInput(2 - 1)
        kalman.kInput(0) = hist.mm.minVal
        kalman.kInput(1) = hist.mm.maxVal
        kalman.Run(empty)
        hist.mm.minVal = Math.Min(kalman.kOutput(0), kalman.kOutput(1))
        hist.mm.maxVal = Math.Max(kalman.kOutput(0), kalman.kOutput(1))

        Dim totalPixels = dst2.Total ' assume we are including zeros.
        If hist.plot.removeZeroEntry Then totalPixels = input.CountNonZero

        Dim brickWidth = dst2.Width / task.histogramBins
        Dim incr = (hist.mm.maxVal - hist.mm.minVal) / task.histogramBins
        Dim histIndex = Math.Round(task.mouseMovePoint.X / brickWidth)

        Dim minRange = New cvb.Scalar(histIndex * incr)
        Dim maxRange = New cvb.Scalar((histIndex + 1) * incr + 1)
        If histIndex + 1 = task.histogramBins Then
            minRange = New cvb.Scalar(254)
            maxRange = New cvb.Scalar(255)
        End If
        If useInrange Then
            If histIndex = 0 And hist.plot.removeZeroEntry Then mask = New cvb.Mat(input.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0)) Else mask = input.InRange(minRange, maxRange)
        Else
            Dim bRange = New cvb.Rangef(minRange(0), maxRange(0))
            Dim ranges() = New cvb.Rangef() {bRange}
            cvb.Cv2.CalcBackProject({input}, {0}, hist.histogram, mask, ranges)
        End If
        dst3 = src
        If mask.Type <> cvb.MatType.CV_8U Then mask.ConvertTo(mask, cvb.MatType.CV_8U)
        dst3.SetTo(cvb.Scalar.Yellow, mask)
        Dim actualCount = mask.CountNonZero
        Dim count = hist.histogram.Get(Of Single)(histIndex, 0)
        Dim histMax As mmData = GetMinMax(hist.histogram)
        labels(3) = "Backprojecting " + CStr(CInt(minRange(0))) + " to " + CStr(CInt(maxRange(0))) + " with " +
                     CStr(count) + " histogram samples and " + CStr(actualCount) + " mask count.  Histogram max count = " +
                     CStr(CInt(histMax.maxVal))
        dst2.Rectangle(New cvb.Rect(CInt(histIndex * brickWidth), 0, brickWidth, dst2.Height), cvb.Scalar.Yellow, task.lineWidth)
    End Sub
End Class






Public Class BackProject_Mouse : Inherits TaskParent
    Dim backP As New BackProject_Image
    Public Sub New()
        labels(2) = "Use the mouse to select what should be shown in the backprojection of the depth histogram"
        desc = "Use the mouse to select what should be shown in the backprojection of the depth histogram"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        backP.Run(src)
        dst2 = backP.dst2
        dst3 = backP.dst3
    End Sub
End Class




Public Class BackProject_Depth : Inherits TaskParent
    Dim backp As New BackProject_Image
    Public Sub New()
        desc = "Allow review of the depth backprojection"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim depth = task.pcSplit(2).Threshold(task.MaxZmeters, 255, cvb.ThresholdTypes.TozeroInv)
        backp.Run(depth * 1000)
        dst2 = backp.dst2
        dst3 = src
        dst3.SetTo(cvb.Scalar.White, backp.mask)
    End Sub
End Class




Public Class BackProject_MeterByMeter : Inherits TaskParent
    Dim histogram As New cvb.Mat
    Public Sub New()
        desc = "Backproject the depth data at 1 meter intervals WITHOUT A HISTOGRAM."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.histogramBins < task.MaxZmeters Then task.gOptions.setHistogramBins(task.MaxZmeters + 1)
        If task.optionsChanged Then
            Dim incr = task.MaxZmeters / task.histogramBins
            Dim histData As New List(Of Single)
            For i = 0 To task.histogramBins - 1
                histData.Add(Math.Round(i * incr))
            Next

            histogram = cvb.Mat.FromPixelData(task.histogramBins, 1, cvb.MatType.CV_32F, histData.ToArray)
        End If
        Dim ranges() = New cvb.Rangef() {New cvb.Rangef(0, task.MaxZmeters)}
        cvb.Cv2.CalcBackProject({task.pcSplit(2)}, {0}, histogram, dst1, ranges)

        'dst1.SetTo(task.MaxZmeters, task.maxDepthMask)
        dst1.ConvertTo(dst2, cvb.MatType.CV_8U)
        dst3 = ShowPalette(dst1)
    End Sub
End Class








Public Class BackProject_Hue : Inherits TaskParent
    Dim hue As New OEX_CalcBackProject_Demo1
    Public classCount As Integer
    Public Sub New()
        desc = "Create an 8UC1 image with a backprojection of the hue."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        hue.Run(src)
        classCount = hue.classCount
        dst2 = hue.dst2
        dst3 = ShowPalette(dst2 * 255 / classCount)
    End Sub
End Class








Public Class BackProject_MaskLines : Inherits TaskParent
    Dim masks As New BackProject_Masks
    Dim lines As New Line_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        dst1 = New cvb.Mat(dst1.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        FindSlider("Min Line Length").Value = 1 ' show all lines...
        labels = {"", "lines detected in the backProjection mask", "Histogram of pixels in a grayscale image.  Move mouse to see lines detected in the backprojection mask",
                  "Yellow is backProjection, lines detected are highlighted"}
        desc = "Inspect the lines from individual backprojection masks from a histogram"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        masks.Run(src)
        dst2 = masks.dst2
        dst3 = src.Clone

        Static saveHistIndex As Integer = masks.histIndex
        If masks.histIndex <> saveHistIndex Then
            lines.Run(src)
            lines.lpList = New List(Of PointPair)(lines.lpList)
            dst1.SetTo(0)
        End If

        lines.Run(masks.mask)
        cvb.Cv2.ImShow("mask", masks.mask)

        For Each lp In lines.lpList
            Dim val = masks.dst3.Get(Of Byte)(lp.p1.Y, lp.p1.X)
            If val = 255 Then DrawLine(dst1, lp.p1, lp.p2, cvb.Scalar.White)
        Next
        dst3.SetTo(cvb.Scalar.Yellow, masks.mask)
        dst3.SetTo(task.HighlightColor, dst1)
    End Sub
End Class







Public Class BackProject_Masks : Inherits TaskParent
    Public hist As New Hist_Basics
    Public histIndex As Integer
    Public mask As New cvb.Mat
    Public Sub New()
        labels(2) = "Histogram for the gray scale image.  Move mouse to see backprojection of each grayscale mask."
        desc = "Create all the backprojection masks from a grayscale histogram"
    End Sub
    Public Function maskDetect(gray As cvb.Mat, histIndex As Integer) As cvb.Mat
        Dim brickWidth = dst2.Width / hist.histogram.Rows
        Dim brickRange = 255 / hist.histogram.Rows

        Dim minRange = If(histIndex = hist.histogram.Rows - 1, 255 - brickRange, histIndex * brickRange)
        Dim maxRange = If(histIndex = hist.histogram.Rows - 1, 255, (histIndex + 1) * brickRange)
        If Single.IsNaN(minRange) Or Single.IsInfinity(minRange) Or
           Single.IsNaN(maxRange) Or Single.IsInfinity(maxRange) Then
            SetTrueText("Input data has no values - exit " + traceName)
            Return New cvb.Mat
        End If

        Dim ranges() = New cvb.Rangef() {New cvb.Rangef(minRange, maxRange)}

        cvb.Cv2.CalcBackProject({gray}, {0}, hist.histogram, mask, ranges)
        Return mask
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        hist.Run(src)
        dst2 = hist.dst2

        Dim brickWidth = dst2.Width / task.histogramBins
        histIndex = Math.Floor(task.mouseMovePoint.X / brickWidth)

        Dim gray = If(src.Channels() = 1, src, src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))
        dst3 = task.color.Clone
        dst1 = maskDetect(gray, histIndex)
        If dst1.Width = 0 Then Exit Sub
        dst3.SetTo(cvb.Scalar.White, dst1)
        dst2.Rectangle(New cvb.Rect(CInt(histIndex * brickWidth), 0, brickWidth, dst2.Height), cvb.Scalar.Yellow, task.lineWidth)
    End Sub
End Class




Public Class BackProject_MaskList : Inherits TaskParent
    Public histList As New List(Of List(Of Single))
    Public histogramList As New List(Of cvb.Mat)
    Dim inputMatList As New List(Of cvb.Mat)
    Dim histS As New Hist_DepthSimple
    Dim plotHist As New Plot_Histogram
    Public Sub New()
        plotHist.addLabels = False
        plotHist.removeZeroEntry = True
        task.gOptions.setHistogramBins(40)
        task.gOptions.DebugSlider.Minimum = 0
        labels(3) = "Depth mask used to build the depth histogram at left"
        desc = "Create masks for each histogram bin backprojection"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim gray = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Dim bins = If(task.histogramBins <= 255, task.histogramBins - 1, 255)
        Dim incr = 255 / bins
        If bins <> task.gOptions.DebugSlider.Maximum Then
            task.gOptions.DebugSlider.Value = 0
            task.gOptions.DebugSlider.Maximum = bins
        End If
        histList.Clear()
        histogramList.Clear()
        inputMatList.Clear()
        histS.ranges = {New cvb.Rangef(0 - 0.01, task.MaxZmeters + 0.01)}
        For i = 0 To bins - 2
            Dim minVal = i * incr
            Dim maxVal = (i + 1) * incr
            histS.inputMask = gray.InRange(minVal, maxVal)
            histS.Run(task.pcSplit(2))
            histList.Add(New List(Of Single)(histS.histList))
            histogramList.Add(histS.histogram.Clone)
            inputMatList.Add(histS.inputMask.Clone)
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
