Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class BackProject_Basics : Inherits VB_Algorithm
    Public histK As New Histogram_Kalman
    Public minRange As cv.Scalar, maxRange As cv.Scalar
    Public Sub New()
        labels(2) = "Move mouse to backproject a histogram column"
        desc = "Mouse over any bin to see the color histogram backprojected."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim input = src.Clone
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        histK.Run(input)
        If histK.hist.mm.minVal = histK.hist.mm.maxVal Then
            setTrueText("The input image is empty - mm.minVal and mm.maxVal are both zero...")
            Exit Sub
        End If

        dst2 = histK.dst2

        Dim totalPixels = dst2.Total ' assume we are including zeros.
        If histK.hist.plot.removeZeroEntry Then totalPixels = input.CountNonZero

        Dim brickWidth = dst2.Width / gOptions.HistBinSlider.Value
        Dim incr = (histK.hist.mm.maxVal - histK.hist.mm.minVal) / gOptions.HistBinSlider.Value
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / brickWidth)

        minRange = New cv.Scalar(histIndex * incr)
        maxRange = New cv.Scalar((histIndex + 1) * incr)
        If histIndex + 1 = gOptions.HistBinSlider.Value Then maxRange = New cv.Scalar(255)

        '     Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}
        '     cv.Cv2.CalcBackProject({input}, {0}, histK.hist.histogram, dst0, ranges)
        ' for single dimension histograms, backprojection is the same as inrange (and this works for backproject_FeatureLess below)
        dst0 = input.InRange(minRange, maxRange)

        Dim actualCount = dst0.CountNonZero
        dst3 = task.color.Clone
        dst3.SetTo(cv.Scalar.Yellow, dst0)
        Dim count = histK.hist.histogram.Get(Of Single)(CInt(histIndex), 0)
        Dim histMax As mmData = vbMinMax(histK.hist.histogram)
        labels(3) = "Backprojecting " + CStr(CInt(minRange(0))) + " to " + CStr(CInt(maxRange(0))) + " with " +
                    CStr(count) + " of " + CStr(totalPixels) + " samples compared to " + " mask pixels = " + CStr(actualCount) +
                    " Histogram max count = " + CStr(CInt(histMax.maxVal))
        dst2.Rectangle(New cv.Rect(CInt(histIndex) * brickWidth, 0, brickWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class








' https://docs.opencv.org/3.4/da/d7f/tutorial_back_projection.html
Public Class BackProject_Full : Inherits VB_Algorithm
    Public classCount As Integer
    Public Sub New()
        desc = "Create a color histogram, normalize it, and backproject it with a palette."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        classCount = task.histogramBins
        task.gray.ConvertTo(dst1, cv.MatType.CV_32F)
        Dim histogram As New cv.Mat
        Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 255)}
        cv.Cv2.CalcHist({dst1}, {0}, New cv.Mat, histogram, 1, {classCount}, ranges)
        histogram = histogram.Normalize(0, classCount, cv.NormTypes.MinMax)

        cv.Cv2.CalcBackProject({dst1}, {0}, histogram, dst2, ranges)

        dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        If standalone Or showIntermediate() Then dst3 = vbPalette(dst2 * 255 / classCount)
    End Sub
End Class









Public Class BackProject_Reduction : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Dim backP As New BackProject_Basics
    Public Sub New()
        redOptions.SimpleReduction.Checked = True
        labels(3) = "Backprojection of highlighted histogram bin"
        desc = "Use the histogram of a reduced BGR image to isolate featureless portions of an image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)

        backP.Run(reduction.dst2)
        dst2 = backP.dst2
        dst3 = backP.dst3
        labels(2) = "Reduction = " + CStr(redOptions.SimpleReductionSlider.Value) + " and bins = " + CStr(task.histogramBins)
    End Sub
End Class







Public Class BackProject_FeatureLess : Inherits VB_Algorithm
    Dim backP As New BackProject_Basics
    Dim reduction As New Reduction_Basics
    Dim edges As New Edge_ColorGap_CPP
    Public Sub New()
        redOptions.BitwiseReduction.Checked = True
        labels = {"", "", "Histogram of the grayscale image at right",
                  "Move mouse over the histogram to backproject a column"}
        desc = "Create a histogram of the featureless regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edges.Run(src)
        reduction.Run(edges.dst3)
        backP.Run(reduction.dst2)
        dst2 = backP.dst2
        dst3 = backP.dst3
        labels(2) = "Reduction = " + CStr(redOptions.SimpleReductionSlider.Value) + " and bins = " + CStr(task.histogramBins)
    End Sub
End Class









Public Class BackProject_BasicsKeyboard : Inherits VB_Algorithm
    Dim keys As New Keyboard_Basics
    Dim backP As New BackProject_Image
    Public Sub New()
        labels(2) = "Move the mouse away from OpenCVB and use the left and right arrows to move between histogram bins."
        desc = "Move the mouse off of OpenCVB and then use the left and right arrow keys move around in the backprojection histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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








Public Class BackProject_ReductionLines : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Dim lines As New Line_Basics
    Public Sub New()
        redOptions.BitwiseReduction.Checked = True

        labels(3) = "Backprojection of highlighted histogram bin"
        desc = "Use the histogram of a reduced BGR image to isolate featureless portions of an image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2

        lines.Run(dst2)

        dst3 = src
        For i = 0 To lines.sortLength.Count - 1
            Dim mps = lines.mpList(lines.sortLength.ElementAt(i).Value)
            dst3.Line(mps.p1, mps.p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
        labels(3) = CStr(lines.mpList.Count) + " lines were found"

        labels(2) = "Reduction = " + CStr(redOptions.SimpleReductionSlider.Value) + " and bins = " + CStr(task.histogramBins)
    End Sub
End Class






Public Class BackProject_FullLines : Inherits VB_Algorithm
    Dim backP As New BackProject_Full
    Dim lines As New Line_Basics
    Public Sub New()
        desc = "Find lines in the back projection"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        backP.Run(src)
        lines.Run(backP.dst2)
        dst2 = lines.dst2
        labels(3) = CStr(lines.mpList.Count) + " lines were found"
    End Sub
End Class









Public Class BackProject_PointCloud : Inherits VB_Algorithm
    Public hist As New Histogram_PointCloud
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        labels = {"", "", "Backprojection after histogram binning X and Z values", "Backprojection after histogram binning Y and Z values"}
        desc = "Explore Backprojection of the cloud histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim threshold = hist.thresholdSlider.Value
        hist.Run(src)

        dst0 = hist.dst2.Threshold(threshold, 255, cv.ThresholdTypes.Binary)
        dst1 = hist.dst3.Threshold(threshold, 255, cv.ThresholdTypes.Binary)

        dst2 = New cv.Mat(hist.dst2.Size, cv.MatType.CV_32F, 0)
        dst3 = New cv.Mat(hist.dst3.Size, cv.MatType.CV_32F, 0)

        Dim mask As New cv.Mat
        cv.Cv2.CalcBackProject({task.pointCloud}, {0, 2}, dst0, mask, hist.rangesX)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        task.pointCloud.CopyTo(dst2, mask)

        cv.Cv2.CalcBackProject({task.pointCloud}, {1, 2}, dst1, mask, hist.rangesY)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        task.pointCloud.CopyTo(dst3, mask)
    End Sub
End Class









Public Class BackProject_Display : Inherits VB_Algorithm
    Dim backP As New BackProject_Full
    Public Sub New()
        labels = {"", "", "Back projection", ""}
        desc = "Display the back projected color image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        backP.Run(src)
        dst2 = backP.dst2
        dst3 = backP.dst3
    End Sub
End Class







Public Class BackProject_Unstable : Inherits VB_Algorithm
    Dim backP As New BackProject_Full
    Dim diff As New Diff_Basics
    Public Sub New()
        gOptions.PixelDiffThreshold.Value = 6
        labels = {"", "", "Backprojection output", "Unstable pixels in the backprojection.  If flashing, set 'Pixel Difference Threshold' higher."}
        desc = "Highlight the unstable pixels in the backprojection."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        backP.Run(src)
        dst2 = vbPalette(backP.dst2)

        diff.Run(dst2)
        dst3 = diff.dst3
    End Sub
End Class











Public Class BackProject_FullEqualized : Inherits VB_Algorithm
    Dim backP As New BackProject_Full
    Dim equalize As New Histogram_EqualizeColor
    Public Sub New()
        labels = {"", "", "BackProject_Full output without equalization", "BackProject_Full with equalization"}
        desc = "Create a histogram from the equalized color and then backproject it."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        backP.Run(src)
        backP.dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2 = vbPalette(dst2)

        equalize.Run(src)
        backP.Run(equalize.dst2)

        backP.dst2.ConvertTo(dst3, cv.MatType.CV_8U)
        dst3 = vbPalette(dst3)
    End Sub
End Class








Public Class BackProject_MaskLines : Inherits VB_Algorithm
    Dim masks As New BackProject_Masks
    Dim lines As New Line_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels = {"", "lines detected in the backProjection mask", "Histogram of pixels in a grayscale image.  Move mouse to see lines detected in the backprojection mask",
                  "Yellow is backProjection, lines detected are highlighted"}
        desc = "Inspect the lines from individual backprojection masks from a histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        masks.Run(src)
        dst2 = masks.dst2
        dst3 = src.Clone

        If heartBeat() Then dst1.SetTo(0)

        lines.Run(masks.mask)
        For Each mp In lines.mpList
            Dim val = masks.dst3.Get(Of Byte)(mp.p1.Y, mp.p1.X)
            If val = 255 Then dst1.Line(mp.p1, mp.p2, cv.Scalar.White, task.lineWidth, task.lineType)
        Next
        dst3.SetTo(cv.Scalar.Yellow, masks.mask)
        dst3.SetTo(task.highlightColor, dst1)
    End Sub
End Class







Public Class BackProject_Masks : Inherits VB_Algorithm
    Public hist As New Histogram_Basics
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
            setTrueText("Input data has no values - exit " + traceName)
            Return New cv.Mat
        End If

        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

        cv.Cv2.CalcBackProject({gray}, {0}, hist.histogram, mask, ranges)
        Return mask
    End Function
    Public Sub RunVB(src As cv.Mat)
        hist.Run(src)
        dst2 = hist.dst2

        Dim brickWidth = dst2.Width / task.histogramBins
        histIndex = Math.Floor(task.mouseMovePoint.X / brickWidth)

        Dim gray = If(src.Channels = 1, src, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst3 = task.color.Clone
        dst1 = maskDetect(gray, histIndex)
        If dst1.Width = 0 Then Exit Sub
        dst3.SetTo(cv.Scalar.White, dst1)
        dst2.Rectangle(New cv.Rect(CInt(histIndex * brickWidth), 0, brickWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class








Public Class BackProject_Side : Inherits VB_Algorithm
    Dim autoY As New OpAuto_YRange
    Dim hist2d As New Histogram2D_Side
    Public Sub New()
        labels = {"", "", "Hotspots in the Side View", "Back projection of the hotspots in the Side View"}
        desc = "Display the back projection of the hotspots in the Side View"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)
        autoY.Run(hist2d.histogram)

        dst2 = autoY.histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        Dim histogram = autoY.histogram.SetTo(0, Not dst2)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histogram, dst3, task.rangesSide)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class






Public Class BackProject_Top : Inherits VB_Algorithm
    Dim hist2d As New Histogram2D_Top
    Public Sub New()
        labels = {"", "", "Hotspots in the Top View", "Back projection of the hotspots in the Top View"}
        desc = "Display the back projection of the hotspots in the Top View"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)
        dst2 = hist2d.dst2

        Dim histogram = hist2d.histogram.SetTo(0, Not dst2)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histogram, dst3, task.rangesTop)
        dst3 = vbPalette(dst3.ConvertScaleAbs)
    End Sub
End Class







Public Class BackProject_Horizontal : Inherits VB_Algorithm
    Dim bpTop As New BackProject_Top
    Dim bpSide As New BackProject_Side
    Public Sub New()
        desc = "Use both the BackProject_Top to improve the results of the BackProject_Side for finding flat surfaces."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bpTop.Run(src)
        task.pointCloud.SetTo(0, bpTop.dst3)

        bpSide.Run(src)
        dst2 = bpSide.dst3
    End Sub
End Class










Public Class BackProject_Vertical : Inherits VB_Algorithm
    Dim bpTop As New BackProject_Top
    Dim bpSide As New BackProject_Side
    Public Sub New()
        desc = "Use both the BackProject_Top to improve the results of the BackProject_Side for finding flat surfaces."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bpSide.Run(src)
        task.pointCloud.SetTo(0, bpSide.dst3)

        bpTop.Run(src)
        dst2 = bpTop.dst3
    End Sub
End Class









Public Class BackProject_SoloSide : Inherits VB_Algorithm
    Dim hist2d As New Histogram2D_Side
    Public Sub New()
        labels = {"", "", "Solo samples in the Side View", "Back projection of the solo samples in the Side View"}
        desc = "Display the back projection of the solo samples in the Side View"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)

        dst3 = hist2d.histogram.Threshold(1, 255, cv.ThresholdTypes.TozeroInv)
        dst2 = dst3.ConvertScaleAbs(255)

        hist2d.histogram.SetTo(0, Not dst2)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, hist2d.histogram, dst3, task.rangesSide)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class






Public Class BackProject_SoloTop : Inherits VB_Algorithm
    Dim hist2d As New Histogram2D_Top
    Public Sub New()
        labels = {"", "", "Solo samples in the Top View", "Back projection of the solo samples in the Top View"}
        desc = "Display the back projection of the solo samples in the Top View"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist2d.Run(src)

        dst3 = hist2d.histogram.Threshold(1, 255, cv.ThresholdTypes.TozeroInv)
        dst2 = dst3.ConvertScaleAbs(255)

        hist2d.histogram.SetTo(0, Not dst2)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, hist2d.histogram, dst3, task.rangesTop)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class






Public Class BackProject_LineTop : Inherits VB_Algorithm
    Dim line As New Line_ViewTop
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Backproject the lines found in the top view."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        line.Run(src)

        dst2.SetTo(0)
        Dim w = task.lineWidth + 5
        For Each mp In line.lines.mpList
            Dim p1 = New cv.Point(0, mp.yIntercept)
            Dim p2 = New cv.Point(dst2.Width, dst2.Width * mp.slope + mp.yIntercept)
            dst2.Line(p1, p2, 255, w, task.lineType)
        Next

        Dim histogram = line.autoX.histogram
        histogram.SetTo(0, Not dst2)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, histogram, dst3, task.rangesTop)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class








Public Class BackProject_LineSide : Inherits VB_Algorithm
    Dim line As New Line_ViewSide
    Public mpList As New List(Of linePoints)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Backproject the lines found in the side view."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        line.Run(src)

        dst2.SetTo(0)
        Dim w = task.lineWidth + 5
        mpList.Clear()
        For Each mp In line.lines.mpList
            If Math.Abs(mp.slope) < 0.1 Then
                Dim p1 = New cv.Point(0, mp.yIntercept)
                Dim p2 = New cv.Point(dst2.Width, dst2.Width * mp.slope + mp.yIntercept)
                dst2.Line(p1, p2, 255, w, task.lineType)
                mpList.Add(mp)
            End If
        Next

        Dim histogram = line.autoY.histogram
        histogram.SetTo(0, Not dst2)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histogram, dst1, task.rangesSide)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        dst3 = src
        dst3.SetTo(cv.Scalar.White, dst1)
    End Sub
End Class





' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class BackProject_Image : Inherits VB_Algorithm
    Public hist As New Histogram_Basics
    Public mask As New cv.Mat
    Dim kalman As New Kalman_Basics
    Public useInrange As Boolean
    Public Sub New()
        labels(2) = "Move mouse to backproject each histogram column"
        desc = "Explore Backprojection of each element of a grayscale histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        hist.Run(input)
        If hist.mm.minVal = hist.mm.maxVal Then
            setTrueText("The input image is empty - mm.minval and mm.maxVal are both zero...")
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

        Dim minRange = New cv.Scalar(histIndex * incr)
        Dim maxRange = New cv.Scalar((histIndex + 1) * incr + 1)
        If histIndex + 1 = task.histogramBins Then
            minRange = New cv.Scalar(254)
            maxRange = New cv.Scalar(255)
        End If
        If useInrange Then
            If histIndex = 0 And hist.plot.removeZeroEntry Then mask = New cv.Mat(input.Size, cv.MatType.CV_8U, 0) Else mask = input.InRange(minRange, maxRange)
        Else
            Dim bRange = New cv.Rangef(minRange(0), maxRange(0))
            Dim ranges() = New cv.Rangef() {bRange}
            cv.Cv2.CalcBackProject({input}, {0}, hist.histogram, mask, ranges)
        End If
        dst3 = src
        If mask.Type <> cv.MatType.CV_8U Then mask.ConvertTo(mask, cv.MatType.CV_8U)
        dst3.SetTo(cv.Scalar.Yellow, mask)
        Dim actualCount = mask.CountNonZero
        Dim count = hist.histogram.Get(Of Single)(histIndex, 0)
        Dim histMax As mmData = vbMinMax(hist.histogram)
        labels(3) = "Backprojecting " + CStr(CInt(minRange(0))) + " to " + CStr(CInt(maxRange(0))) + " with " +
                     CStr(count) + " histogram samples and " + CStr(actualCount) + " mask count.  Histogram max count = " +
                     CStr(CInt(histMax.maxVal))
        dst2.Rectangle(New cv.Rect(CInt(histIndex * brickWidth), 0, brickWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class






Public Class BackProject_Mouse : Inherits VB_Algorithm
    Dim backP As New BackProject_Image
    Public Sub New()
        labels(2) = "Use the mouse to select what should be shown in the backprojection of the depth histogram"
        desc = "Use the mouse to select what should be shown in the backprojection of the depth histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        backP.Run(src)
        dst2 = backP.dst2
        dst3 = backP.dst3
    End Sub
End Class




Public Class BackProject_Depth : Inherits VB_Algorithm
    Dim backp As New BackProject_Image
    Public Sub New()
        desc = "Allow review of the depth backprojection"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim depth = task.pcSplit(2).Threshold(task.maxZmeters, 255, cv.ThresholdTypes.TozeroInv)
        backp.Run(depth * 1000)
        dst2 = backp.dst2
        dst3 = src
        dst3.SetTo(cv.Scalar.White, backp.mask)
    End Sub
End Class




Public Class BackProject_MeterByMeter : Inherits VB_Algorithm
    Dim histogram As New cv.Mat
    Public Sub New()
        desc = "Backproject the depth data at 1 meter intervals WITHOUT A HISTOGRAM."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If gOptions.HistBinSlider.Value < task.maxZmeters Then gOptions.HistBinSlider.Value = task.maxZmeters + 1
        If task.optionsChanged Then
            Dim incr = task.maxZmeters / task.histogramBins
            Dim histData As New List(Of Single)
            For i = 0 To task.histogramBins - 1
                histData.Add(Math.Round(i * incr))
            Next

            histogram = New cv.Mat(task.histogramBins, 1, cv.MatType.CV_32F, histData.ToArray)
        End If
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, task.maxZmeters)}
        cv.Cv2.CalcBackProject({task.pcSplit(2)}, {0}, histogram, dst1, ranges)
        dst1.SetTo(task.maxZmeters, task.maxDepthMask)
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)
        dst3 = vbPalette(dst1)
    End Sub
End Class