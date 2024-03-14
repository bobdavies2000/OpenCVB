Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Histogram_Basics : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public mm As mmData
    Public plot As New Plot_Histogram
    Public ranges() As cv.Rangef
    Dim splitIndex As Integer
    Public Sub New()
        If standaloneTest() Then gOptions.HistBinSlider.Value = 255
        desc = "Create a histogram (no Kalman)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            If task.heartBeat Then splitIndex = (splitIndex + 1) Mod 3
            mm = vbMinMax(src.ExtractChannel(splitIndex))
            plot.backColor = Choose(splitIndex + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red)
        Else
            If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            mm = vbMinMax(src)
        End If
        ranges = {New cv.Rangef(mm.minVal - histDelta, mm.maxVal + histDelta)}

        ' ranges are exclusive in OpenCV!!!
        cv.Cv2.CalcHist({src}, {splitIndex}, New cv.Mat, histogram, 1, {task.histogramBins}, ranges)

        plot.Run(histogram)
        histogram = plot.histogram ' reflect any updates to the 0 entry...

        dst2 = plot.dst2
        labels(2) = Choose(splitIndex + 1, "Blue", "Green", "Red") + " histogram, bins = " +
                    CStr(task.histogramBins) + ", X ranges from " + Format(mm.minVal, "0.0") + " to " +
                    Format(mm.maxVal, "0.0") + ", y is sample count"
    End Sub
End Class







' https://github.com/opencv/opencv/blob/master/samples/python/hist.py
Public Class Histogram_Graph : Inherits VB_Algorithm
    Public histRaw(3 - 1) As cv.Mat
    Public histNormalized(3 - 1) As cv.Mat
    Public minRange As Single = 0
    Public maxRange As Single = 255
    Public backColor = cv.Scalar.Gray
    Public plotRequested As Boolean
    Public plotColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
    Public plotMaxValue As Single
    Public Sub New()
        desc = "Plot histograms for up to 3 channels."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim dimensions() = {task.histogramBins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

        Dim plotWidth = dst2.Width / task.histogramBins

        Dim mm As mmData
        dst2.SetTo(backColor)
        For i = 0 To src.Channels - 1
            Dim hist As New cv.Mat
            cv.Cv2.CalcHist({src}, {i}, New cv.Mat(), hist, 1, dimensions, ranges)
            histRaw(i) = hist.Clone()
            mm = vbMinMax(histRaw(i))
            histNormalized(i) = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)
            If standaloneTest() Or plotRequested Then
                Dim points = New List(Of cv.Point)
                Dim listOfPoints = New List(Of List(Of cv.Point))
                For j = 0 To task.histogramBins - 1
                    points.Add(New cv.Point(CInt(j * plotWidth), dst2.Rows - dst2.Rows * histRaw(i).Get(Of Single)(j, 0) / mm.maxVal))
                Next
                listOfPoints.Add(points)
                dst2.Polylines(listOfPoints, False, plotColors(i), task.lineWidth, task.lineType)
            End If
        Next

        If standaloneTest() Or plotRequested Then
            plotMaxValue = Math.Round(mm.maxVal / 1000, 0) * 1000 + 1000 ' smooth things out a little for the scale below
            AddPlotScale(dst2, 0, plotMaxValue)
            labels(2) = "Histogram for src image (default color) - " + CStr(task.histogramBins) + " bins"
        End If
    End Sub
End Class






Public Class Histogram_NormalizeGray : Inherits VB_Algorithm
    Public histogram As New Histogram_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Min Gray", 0, 255, 50)
            sliders.setupTrackBar("Max Gray", 0, 255, 200)
        End If

        labels(2) = "Use sliders to adjust the image and create a histogram of the results"
        desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static minSlider = findSlider("Min Gray")
        Static maxSlider = findSlider("Max Gray")
        If minSlider.Value >= maxSlider.Value Then minSlider.Value = maxSlider.Value - Math.Min(10, maxSlider.Value - 1)
        If minSlider.Value = maxSlider.Value Then maxSlider.Value += 1
        dst3 = src.Normalize(minSlider.Value, maxSlider.Value, cv.NormTypes.MinMax) ' only minMax is working...
        histogram.Run(dst3)
        dst2 = histogram.dst2
    End Sub
End Class








'https://docs.opencv.org/master/d1/db7/tutorial_py_histogram_begins.html
Public Class Histogram_EqualizeGray : Inherits VB_Algorithm
    Public histogramEQ As New Histogram_Basics
    Public histogram As New Histogram_Basics
    Dim mats As New Mat_4to1
    Public Sub New()
        histogramEQ.plot.addLabels = False
        histogram.plot.addLabels = False
        labels(2) = "Equalized image"
        labels(3) = "Orig. Hist, Eq. Hist, Orig. Image, Eq. Image"
        desc = "Create an equalized histogram of the grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        histogram.Run(src)
        cv.Cv2.EqualizeHist(src, dst2)
        histogramEQ.Run(dst2)
        mats.mat(0) = histogram.dst2.Clone
        mats.mat(1) = histogramEQ.dst2
        mats.mat(2) = src
        mats.mat(3) = dst2
        mats.Run(empty)
        dst3 = mats.dst2
    End Sub
End Class






Public Class Histogram_Simple : Inherits VB_Algorithm
    Public plot As New Plot_Histogram
    Public Sub New()
        labels(2) = "Histogram of the grayscale video stream"
        desc = "Build a simple and reusable histogram for grayscale images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(plot.minRange, plot.maxRange)}
        Dim hist As New cv.Mat
        cv.Cv2.CalcHist({src}, {0}, New cv.Mat, hist, 1, {task.histogramBins}, ranges)

        plot.Run(hist)
        dst2 = plot.dst2
    End Sub
End Class









Public Class Histogram_ColorsAndGray : Inherits VB_Algorithm
    Dim histogram As New Histogram_Basics
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Click any quadrant at right to view it below"
        desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim split = src.Split()
        ReDim Preserve split(4 - 1)
        split(4 - 1) = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) ' add a 4th image - the grayscale image to the R G and B images.
        For i = 0 To split.Length - 1
            Dim histSrc = split(i)
            histogram.plot.backColor = Choose(i + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.PowderBlue)
            histogram.Run(histSrc)
            mats.mat(i) = histogram.plot.dst2.Clone
        Next

        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class








Public Class Histogram_Frustrum : Inherits VB_Algorithm
    Dim heat As New HeatMap_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        gOptions.gravityPointCloud.Checked = False
        desc = "Options for the side and top view.  See OptionCommon_Histogram to make settings permanent."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        heat.Run(src)
        dst2 = heat.dst2
        dst3 = heat.dst3

        setTrueText("This algorithm was created to tune the frustrum and camera locations." + vbCrLf +
                    "Without these tuning parameters the side and top views will look correct." + vbCrLf +
                    "To see how these adjustments work or to add a new camera, " + vbCrLf +
                    "use the HeatMap_Basics algorithm." + vbCrLf +
                    "For new cameras, make the adjustments needed, note the value, and update " + vbCrLf +
                    "the Select statement in the constructor for Options_CameraDetails.", New cv.Point(10, 80), 1)
    End Sub
End Class









Public Class Histogram_PeakMax : Inherits VB_Algorithm
    Dim hist As New Histogram_Basics
    Public Sub New()
        desc = "Create a histogram and back project into the image the grayscale color with the highest occurance."
        labels(3) = "Grayscale Histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        gOptions.UseKalman.Checked = False
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        hist.Run(src)
        dst3 = hist.dst2

        Dim mm as mmData = vbMinMax(hist.histogram)
        Dim brickWidth = dst2.Width / task.histogramBins
        Dim brickRange = 255 / task.histogramBins
        Dim histindex = mm.maxLoc.Y
        Dim pixelMin = CInt((histindex) * brickRange)
        Dim pixelMax = CInt((histindex + 1) * brickRange)

        Dim mask = src.InRange(pixelMin, pixelMax).Threshold(1, 255, cv.ThresholdTypes.Binary)
        Dim tmp = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        src.CopyTo(tmp, mask)
        dst2 = tmp.Threshold(0, 255, cv.ThresholdTypes.Binary)

        labels(2) = "BackProjection of most frequent gray pixel"
        dst3.Rectangle(New cv.Rect(brickWidth * histindex, 0, brickWidth, dst2.Height), cv.Scalar.Yellow, 1)
    End Sub
End Class










Public Class Histogram_PeakFinder : Inherits VB_Algorithm
    Public hist As New Histogram_Basics
    Public peakCount As Integer
    Public resetPeaks As Boolean
    Public histogramPeaks As New List(Of Integer)
    Public hCount() As Single
    Public Sub New()
        desc = "Find the peaks - columns taller that both neighbors - in the histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = task.pcSplit(2)

        hist.Run(src)
        dst2 = hist.dst2

        Static saveHistBins = task.histogramBins
        Static peakCounts(task.histogramBins) As Single
        Static allPCounts As New List(Of Integer)
        Static maxList As New List(Of Integer)

        resetPeaks = False
        If saveHistBins <> task.histogramBins Then
            resetPeaks = True
            allPCounts.Clear()
            maxList.Clear()
            saveHistBins = task.histogramBins
            ReDim peakCounts(task.histogramBins)
            ReDim peakCounts(task.histogramBins)
        End If
        ReDim hCount(task.histogramBins)

        Dim histogram = hist.histogram
        Dim peaks As New List(Of Integer)
        Dim maxPeak = Single.MinValue
        Dim maxIndex As Integer
        For i = 0 To histogram.Rows - 1
            Dim prev = histogram.Get(Of Single)(Math.Max(i - 1, 0), 0)
            Dim curr = histogram.Get(Of Single)(i, 0)
            Dim nextVal = histogram.Get(Of Single)(Math.Min(i + 1, histogram.Rows - 1), 0)
            hCount(i) = curr
            If i = 0 Then
                If prev >= nextVal Then
                    peaks.Add(i)
                    peakCounts(i) += 1
                End If
            Else
                If prev <= curr And curr > nextVal Then
                    peaks.Add(i)
                    peakCounts(i) += 1
                End If
            End If
            If curr > maxPeak Then
                maxPeak = curr
                maxIndex = i
            End If
        Next

        allPCounts.Add(peaks.Count)
        maxList.Add(maxIndex)

        peakCount = CInt(allPCounts.Average)
        setTrueText(vbTab + "Avg peaks: " + CStr(peakCount) + ".  Current: " + CStr(peaks.Count) + " peaks.", New cv.Point(0, 10), 3)

        Dim sortedPeaks = New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To peakCounts.Count - 1
            sortedPeaks.Add(peakCounts(i), i)
        Next

        Dim mm as mmData = vbMinMax(histogram)
        If mm.maxVal = 0 Then Exit Sub ' entries are all zero?  Likely camera trouble.
        Dim brickWidth = dst2.Width / histogram.Rows
        histogramPeaks.Clear()
        For i = 0 To Math.Min(sortedPeaks.Count, peakCount) - 1
            Dim index = sortedPeaks.ElementAt(i).Value
            histogramPeaks.Add(index)
            Dim h = CInt(hCount(index) * dst2.Height / mm.maxVal)
            cv.Cv2.Rectangle(dst2, New cv.Rect(index * brickWidth, dst2.Height - h, brickWidth, h), cv.Scalar.Yellow, task.lineWidth)
        Next

        If allPCounts.Count > 100 Then
            allPCounts.RemoveAt(0)
            maxList.RemoveAt(0)
        End If
        If Math.Abs(maxList.Average - maxIndex) > saveHistBins / 10 Then saveHistBins = 0
        labels(2) = "There were " + CStr(peakCount) + " depth peaks (highlighted) up to " + CStr(CInt(task.maxZmeters)) + " meters.  " +
                    "Use global option Histogram Bins to set the number of bins."
    End Sub
End Class










Public Class Histogram_PeaksDepth : Inherits VB_Algorithm
    Dim peaks As New Histogram_PeakFinder
    Public Sub New()
        desc = "Find the peaks - columns taller that both neighbors - in the histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        peaks.Run(task.pcSplit(2))
        dst2 = peaks.dst2
        labels(2) = peaks.labels(2)
    End Sub
End Class











Public Class Histogram_PeaksRGB : Inherits VB_Algorithm
    Public mats As New Mat_4Click
    Dim peaks(2) As Histogram_PeakFinder
    Public Sub New()
        peaks(0) = New Histogram_PeakFinder
        peaks(1) = New Histogram_PeakFinder
        peaks(2) = New Histogram_PeakFinder
        labels(2) = "Upper left is Blue, upper right is Green, bottom left is Red"
        desc = "Find the peaks and valleys for each of the BGR channels."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim split = src.Split()
        For i = 0 To 3 - 1
            peaks(i).hist.plot.backColor = Choose(i + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red)
            peaks(i).hist.plot.addLabels = False
            peaks(i).Run(split(i))
            mats.mat(i) = peaks(i).dst2.Clone
        Next

        If task.optionsChanged Then
            task.mouseClickFlag = True
            task.mousePicTag = RESULT_DST2
        End If

        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class








Public Class Histogram_SLR : Inherits VB_Algorithm
    Public slr As New SLR_Basics
    Public hist As New Histogram_Basics
    Public Sub New()
        labels = {"", "", "Original data", "Segmented Linear Regression (SLR) version of the same data.  Red line is zero."}
        desc = "Run Segmented Linear Regression on depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist.Run(src)
        hist.histogram.Set(Of Single)(0, 0, 0)
        dst2 = hist.dst2
        For i = 0 To hist.histogram.Rows - 1
            slr.input.dataX.Add(i)
            slr.input.dataY.Add(hist.histogram.Get(Of Single)(i, 0))
        Next
        slr.Run(src)
        dst3 = slr.dst3
    End Sub
End Class







Public Class Histogram_Color : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public plot As New Plot_Histogram
    Public ranges() As cv.Rangef
    Public Sub New()
        desc = "Create a histogram of green and red."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        ranges = New cv.Rangef() {New cv.Rangef(0, 255), New cv.Rangef(0, 255)}
        cv.Cv2.CalcHist({src}, {1, 2}, New cv.Mat, histogram, 1, {task.histogramBins, task.histogramBins}, ranges)

        Dim test = histogram.Normalize(0, 255, cv.NormTypes.MinMax)

        Dim input As New cv.Mat
        src.ConvertTo(input, cv.MatType.CV_32FC3)
        Dim mask As New cv.Mat
        cv.Cv2.CalcBackProject({input}, {1, 2}, histogram, mask, ranges)

        Dim mm as mmData = vbMinMax(mask)

        plot.Run(test)
        dst2 = plot.dst2
    End Sub
End Class








Public Class Histogram_KalmanAuto : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public kalman As New Kalman_Basics
    Public plot As New Plot_Histogram
    Dim mm As mmData
    Public ranges() As cv.Rangef
    Public Sub New()
        desc = "Create a histogram of the grayscale image and smooth the bar chart with a kalman filter."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static splitIndex = 0
        Static colorName = "Gray"
        If standaloneTest() Then
            If task.heartBeat Then splitIndex = If(splitIndex < 2, splitIndex + 1, 0)
            colorName = Choose(splitIndex + 1, "Blue", "Green", "Red")
            Dim split = src.Split()
            src = split(splitIndex)
        End If

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        mm = vbMinMax(src)
        ranges = New cv.Rangef() {New cv.Rangef(mm.minVal, mm.maxVal)}

        If mm.minVal = mm.maxVal Then
            setTrueText("The input image is empty - minVal and maxVal are both zero...")
            Exit Sub
        End If
        Dim dimensions() = {task.histogramBins}
        cv.Cv2.CalcHist({src}, {0}, New cv.Mat, histogram, 1, dimensions, ranges)

        If kalman.kInput.Length <> task.histogramBins Then ReDim kalman.kInput(task.histogramBins - 1)

        For i = 0 To task.histogramBins - 1
            kalman.kInput(i) = histogram.Get(Of Single)(i, 0)
        Next
        kalman.Run(src)
        histogram = New cv.Mat(kalman.kOutput.Length, 1, cv.MatType.CV_32FC1, kalman.kOutput)

        Dim splitColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
        If standaloneTest() Then plot.backColor = splitColors(splitIndex)
        plot.Run(histogram)
        dst2 = plot.dst2

        labels(2) = colorName + " histogram, bins = " + CStr(task.histogramBins) + ", X ranges from " + Format(mm.minVal, "0.0") + " to " + Format(mm.maxVal, "0.0") + ", y is occurances"
    End Sub
End Class










' https://docs.opencv.org/master/d1/db7/tutorial_py_histogram_begins.html
Public Class Histogram_EqualizeColor : Inherits VB_Algorithm
    Public kalmanEq As New Histogram_Basics
    Public kalman As New Histogram_Basics
    Dim mats As New Mat_2to1
    Public displayHist As Boolean = False
    Public channel = 2
    Public Sub New()
        kalmanEq.plot.addLabels = False
        kalman.plot.addLabels = False
        desc = "Create an equalized histogram of the color image."
        labels(2) = "Image Enhanced with Equalized Histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim rgb(2) As cv.Mat
        Dim rgbEq(2) As cv.Mat
        rgbEq = src.Split()

        For i = 0 To rgb.Count - 1
            cv.Cv2.EqualizeHist(rgbEq(i), rgbEq(i))
        Next

        If standaloneTest() Or displayHist Then
            cv.Cv2.Split(src, rgb) ' equalizehist alters the input...
            kalman.plot.backColor = cv.Scalar.Red
            kalman.Run(rgb(channel).Clone())
            mats.mat(0) = kalman.dst2.Clone()

            kalmanEq.Run(rgbEq(channel).Clone())
            mats.mat(1) = kalmanEq.dst2.Clone()

            mats.Run(empty)
            dst3 = mats.dst2
            labels(3) = "Before (top) and After Red Histogram"
        End If

        cv.Cv2.Merge(rgbEq, dst2)
    End Sub
End Class









Public Class Histogram_CompareGray : Inherits VB_Algorithm
    Public histK As New Histogram_Kalman
    Dim options As New Options_HistCompare
    Public histDiff As New cv.Mat
    Public histDiffAbs As New cv.Mat
    Public normHistDiff As New cv.Mat
    Public normHistDiffAbs As New cv.Mat
    Public Sub New()
        labels(2) = "Kalman-smoothed current histogram"
        desc = "Compare grayscale histograms for successive frames"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        histK.Run(src)
        dst2 = histK.dst2.Clone
        Static lastHist As cv.Mat = histK.hist.histogram

        Dim histNorm As cv.Mat = histK.hist.histogram.Normalize(0, 1, cv.NormTypes.MinMax)
        Static lastHistNorm As cv.Mat = histNorm.Clone

        If lastHistNorm.Size = histK.hist.histogram.Size Then
            Dim Comparison = cv.Cv2.CompareHist(histNorm, lastHistNorm, options.compareMethod)
            If Double.IsNaN(Comparison) Then Comparison = 0
            labels(3) = "CompareHist output = " + Format(Comparison, fmt3) + " using " + options.compareName + " method"
            trueData = New List(Of trueText)(histK.hist.plot.trueData)
            setTrueText(labels(3), 2)
        Else
            lastHistNorm = histNorm.Clone
        End If

        If histNorm.Size = lastHistNorm.Size Then
            normHistDiff = histNorm - lastHistNorm
            cv.Cv2.Absdiff(histNorm, lastHistNorm, normHistDiffAbs)
        End If
        lastHistNorm = histNorm.Clone

        If histK.hist.histogram.Size = lastHist.Size Then
            histDiff = histK.hist.histogram - lastHist
            cv.Cv2.Absdiff(histK.hist.histogram, lastHist, histDiffAbs)
        End If
        lastHist = histK.hist.histogram.Clone
    End Sub
End Class









Public Class Histogram_ComparePlot : Inherits VB_Algorithm
    Dim comp As New Histogram_CompareGray
    Public Sub New()
        labels(3) = "Differences have been multiplied by 1000 to build scale at the left"
        desc = "Compare grayscale histograms for successive frames and plot the difference as a histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        comp.Run(src)
        dst2 = comp.dst2.Clone

        Static ttLabels As New List(Of trueText)
        If task.heartBeat Then
            ttLabels = New List(Of trueText)(comp.trueData)
            Dim histX = comp.histDiffAbs
            comp.histK.hist.plot.Run(histX)
            dst3 = comp.histK.hist.plot.dst2.Clone

            Dim mm as mmData = vbMinMax(histX)
            AddPlotScale(dst2, 0, mm.maxVal)
        End If
        trueData = New List(Of trueText)(ttLabels)
    End Sub
End Class







Public Class Histogram_CompareNumber : Inherits VB_Algorithm
    Dim comp As New Histogram_CompareGray
    Dim plot As New Plot_OverTimeScalar
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        plot.plotCount = 2

        labels = {"", "", "Kalman-smoothed normalized histogram output", "Plot of the sum of the differences between recent normalized histograms"}
        desc = "The idea is to reduce a comparison of 2 histograms to a single number"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        comp.Run(src)
        dst1 = comp.dst2.Clone

        Dim sum = cv.Cv2.Sum(comp.normHistDiff)(0) * 100
        Dim sumAbs = cv.Cv2.Sum(comp.normHistDiffAbs)(0) * 100
        plot.plotData = New cv.Scalar(sum, sumAbs, 0)
        plot.Run(empty)
        dst2 = plot.dst2
        dst3 = plot.dst3

        setTrueText("Upper left is the sum * 100 of the difference" + vbCrLf + "Upper right is the sum of the absolute values * 100", New cv.Point(0, dst2.Height / 2), 2)
    End Sub
End Class






' https://study.marearts.com/2014/11/opencv-emdearth-mover-distance-example.html
Public Class Histogram_CompareEMD_hsv : Inherits VB_Algorithm
    Dim hist As New Histogram_Basics
    Public Sub New()
        labels = {"", "", "Kalman-smoothed normalized histogram output", "Plot of the sum of the differences between recent normalized histograms"}
        desc = "Use OpenCV's Earth Mover Distance to compare 2 images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Static lastHSV As cv.Mat = hsv.Clone

        Dim hBins = 30, sBins = 32
        Dim histA As New cv.Mat, histB As New cv.Mat
        Dim ranges = New cv.Rangef() {New cv.Rangef(0, 180), New cv.Rangef(0, 256)}

        cv.Cv2.CalcHist({hsv}, {0, 1}, New cv.Mat, histA, 2, {hBins, sBins}, ranges)
        Dim histNormA As cv.Mat = histA.Normalize(0, 1, cv.NormTypes.MinMax)

        cv.Cv2.CalcHist({lastHSV}, {0, 1}, New cv.Mat, histB, 2, {hBins, sBins}, ranges)
        Dim histNormB As cv.Mat = histB.Normalize(0, 1, cv.NormTypes.MinMax)

        Dim sig1 = New cv.Mat(sBins * hBins, 3, cv.MatType.CV_32F, 0)
        Dim sig2 = New cv.Mat(sBins * hBins, 3, cv.MatType.CV_32F, 0)
        For h = 0 To hBins - 1
            For s = 0 To sBins - 1
                sig1.Set(Of Single)(h * sBins + s, 0, histNormA.Get(Of Single)(h, s))
                sig1.Set(Of Single)(h * sBins + s, 1, h)
                sig1.Set(Of Single)(h * sBins + s, 2, s)

                sig2.Set(Of Single)(h * sBins + s, 0, histNormB.Get(Of Single)(h, s))
                sig2.Set(Of Single)(h * sBins + s, 1, h)
                sig2.Set(Of Single)(h * sBins + s, 2, s)
            Next
        Next

        Dim emd = cv.Cv2.EMD(sig1, sig2, cv.DistanceTypes.L2)
        setTrueText("EMD similarity from the current image to the last is " + Format(1 - emd, "0.0%"), 2)

        lastHSV = hsv.Clone
    End Sub
End Class








Public Class Histogram_Peaks : Inherits VB_Algorithm
    Dim masks As New BackProject_Masks
    Public Sub New()
        desc = "Interactive Histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        masks.Run(src)
        dst2 = masks.dst2
        dst3 = masks.dst3
    End Sub
End Class







Public Class Histogram_Lab : Inherits VB_Algorithm
    Dim hist As New Histogram_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        labels = {"Lab Colors ", "Lab Channel 0", "Lab Channel 1", "Lab Channel 2"}
        desc = "Create a histogram from a BGR image converted to LAB."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst0 = src.CvtColor(cv.ColorConversionCodes.BGR2Lab)
        Dim split = dst0.Split()

        hist.Run(split(0))
        dst1 = hist.dst2.Clone

        hist.Run(split(1))
        dst2 = hist.dst2.Clone

        hist.Run(split(2))
        dst3 = hist.dst2.Clone
    End Sub
End Class








Public Class Histogram_PointCloudXYZ : Inherits VB_Algorithm
    Public plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        labels = {"", "Histogram of the X channel", "Histogram of the Y channel", "Histogram of the Z channel"}
        desc = "Show individual channel of the point cloud data as a histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static ttlists As New List(Of List(Of trueText))({New List(Of trueText), New List(Of trueText), New List(Of trueText)})
        For i = 0 To 2
            dst0 = task.pcSplit(i)
            Dim mm as mmData = vbMinMax(dst0)

            Select Case i
                Case 0
                    plot.removeZeroEntry = False
                    plot.minRange = -task.xRange
                    plot.maxRange = task.xRange
                Case 1
                    plot.removeZeroEntry = False
                    plot.minRange = -task.yRange
                    plot.maxRange = task.yRange
                Case 2
                    plot.removeZeroEntry = True
                    plot.minRange = 0
                    plot.maxRange = task.maxZmeters
            End Select

            plot.Run(dst0)
            Select Case i
                Case 0
                    dst1 = plot.dst2.Clone
                Case 1
                    dst2 = plot.dst2.Clone
                Case 2
                    dst3 = plot.dst2.Clone
            End Select
            If task.heartBeat Then labels(i + 1) = "Histogram " + Choose(i + 1, "X", "Y", "Z") + " ranges from " + Format(plot.minRange, "0.0") + "m to " +
                                                Format(plot.maxRange, "0.0") + "m"
        Next
    End Sub
End Class







Public Class Histogram_FlatSurfaces : Inherits VB_Algorithm
    Dim masks As New BackProject_Masks
    Public Sub New()
        desc = "Find flat surfaces with the histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim maxRange = 4
        Dim cloudY = task.pcSplit(1).Clone
        Dim mm as mmData = vbMinMax(cloudY)
        cloudY = cloudY.Threshold(maxRange, mm.maxVal, cv.ThresholdTypes.Trunc)
        Static saveMinVal = mm.minVal, saveMaxVal = mm.maxVal
        If task.heartBeat Then
            saveMinVal = mm.minVal
            saveMaxVal = mm.maxVal
        End If

        If saveMinVal > mm.minVal Then saveMinVal = mm.minVal
        If saveMaxVal < mm.maxVal Then saveMaxVal = mm.maxVal

        cloudY.Set(Of Single)(mm.minLoc.Y, mm.minLoc.X, -saveMinVal)
        cloudY.Set(Of Single)(mm.maxLoc.Y, mm.maxLoc.X, saveMaxVal)
        cloudY = (cloudY - saveMinVal).tomat
        cloudY = cloudY.ConvertScaleAbs(255 / (-saveMinVal + saveMaxVal))
        mm = vbMinMax(cloudY)
        cloudY.SetTo(0, task.noDepthMask)
        masks.Run(cloudY)
        dst2 = masks.dst2
        dst3 = src
        dst3 = dst3.SetTo(cv.Scalar.White, masks.dst1)
        labels(2) = "Range for the histogram is from " + Format(saveMinVal, fmt1) + " to " + Format(saveMaxVal, fmt1)
    End Sub
End Class







Public Class Histogram_ShapeSide : Inherits VB_Algorithm
    Public rc As New rcDataOld
    Public Sub New()
        gOptions.HistBinSlider.Value = 60
        labels = {"", "", "ZY Side View", "ZY Side View Mask"}
        desc = "Create a 2D side view for ZY histogram of depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If rc.pixels = 0 Then src = task.pointCloud

        cv.Cv2.CalcHist({src}, task.channelsSide, New cv.Mat, dst0, 2,
                        {task.histogramBins, task.histogramBins}, task.rangesSide)
        dst0.Col(0).SetTo(0) ' too many zero depth points...

        dst0 = vbNormalize32f(dst0)
        dst0.ConvertTo(dst0, cv.MatType.CV_8UC1)

        Dim r As New cv.Rect(0, 0, dst2.Height, dst2.Height)
        dst2(r) = dst0.Resize(New cv.Size(dst2.Height, dst2.Height), 0, 0, cv.InterpolationFlags.Nearest)
        dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







Public Class Histogram_ShapeTop : Inherits VB_Algorithm
    Public rc As New rcDataOld
    Public Sub New()
        gOptions.HistBinSlider.Value = 60
        labels = {"", "", "ZY Side View", "ZY Side View Mask"}
        desc = "Create a 2D top view for XZ histogram of depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If rc.pixels = 0 Then src = task.pointCloud

        cv.Cv2.CalcHist({src}, task.channelsTop, New cv.Mat, dst0, 2,
                        {task.histogramBins, task.histogramBins}, task.rangesTop)
        dst0.Row(0).SetTo(0) ' too many zero depth points...

        dst0 = vbNormalize32f(dst0)
        dst0.ConvertTo(dst0, cv.MatType.CV_8UC1)

        Dim r As New cv.Rect(0, 0, dst2.Height, dst2.Height)
        dst2(r) = dst0.Resize(New cv.Size(dst2.Height, dst2.Height), 0, 0, cv.InterpolationFlags.Nearest)
        dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class









Public Class Histogram_Gotcha2D : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public Sub New()
        labels(2) = "ZY (Side View)"
        desc = "Create a 2D side view for ZY histogram of depth using integer values.  Testing calcHist gotcha."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim expected = task.pcSplit(2).CountNonZero
        Dim ranges = task.rangesSide
        If task.toggleOnOff Then
            ranges = New cv.Rangef() {New cv.Rangef(-10, +10), New cv.Rangef(-1, 20)}
        End If
        cv.Cv2.CalcHist({task.pointCloud}, task.channelsSide, New cv.Mat, histogram, 2, task.bins2D, task.rangesSide)

        Dim actual = histogram.Sum(0)

        If task.heartBeat Then
            strOut = "Expected sample count:" + vbTab + CStr(expected) + vbCrLf +
                     "Actual sample count:" + vbTab + CStr(actual) + vbCrLf +
                     "The number of samples input is the expected value." + vbCrLf +
                     "The number of entries in the histogram is the 'actual' number of samples." + vbCrLf +
                     "How can the values not be equal?  The ranges of the histogram are exclusive." + vbCrLf +
                     "Another way that samples may be lost: X or Y range.  Use Y-Range slider to show impact." +
                     "A third way samples may not match: max depth can toss samples as well."
        End If
        setTrueText(strOut, 3)
        dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
    End Sub
End Class






Public Class Histogram_Gotcha : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Dim hist As New Histogram_Basics
    Public Sub New()
        labels(2) = "Grayscale histogram"
        desc = "Simple test: input samples should equal histogram samples.  What is wrong?  Exclusive ranges!"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim expected = src.Total

        hist.Run(src)

        Dim actual = hist.histogram.Sum(0)

        If task.heartBeat Then
            strOut = "Expected sample count:" + vbTab + CStr(expected) + vbCrLf +
                     "Actual sample count:" + vbTab + CStr(actual) + vbCrLf +
                     "Difference:" + vbTab + vbTab + CStr(Math.Abs(actual - expected)) + vbCrLf +
                     "The number of samples input is the expected value." + vbCrLf +
                     "The number of entries in the histogram is the 'actual' number of samples." + vbCrLf +
                     "How can the values not be equal?  The ranges in the histogram are exclusive!"
        End If
        setTrueText(strOut, 2)
    End Sub
End Class





Public Class Histogram_GotchaFixed_CPP : Inherits VB_Algorithm
    Public Sub New()
        cPtr = Histogram_1D_Open()
        desc = "Testing the C++ CalcHist to investigate gotcha with sample counts"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Histogram_1D_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.histogramBins)
        handleSrc.Free()

        If task.heartBeat Then
            Dim actual = CInt(Histogram_1D_Sum(cPtr))
            strOut = "Expected sample count:" + vbTab + CStr(dst2.Total) + vbCrLf +
                     "Actual sample count:" + vbTab + CStr(actual) + vbCrLf +
                     "Difference:" + vbTab + vbTab + CStr(Math.Abs(actual - dst2.Total)) + vbCrLf +
                     "The number of samples input is the expected value." + vbCrLf +
                     "The number of entries in the histogram is the 'actual' number of samples." + vbCrLf +
                     "How can the values not be equal?  The ranges in the histogram are exclusive!"
        End If
        setTrueText(strOut, 2)
    End Sub
    Public Sub Close()
        Histogram_1D_Close(cPtr)
    End Sub
End Class




Public Class Histogram_Byte_CPP : Inherits VB_Algorithm
    Public plot As New Plot_Histogram
    Public Sub New()
        cPtr = Histogram_1D_Open()
        desc = "For Byte histograms, the C++ code works but the .Net interface doesn't honor exclusive ranges."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Histogram_1D_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.histogramBins)
        handleSrc.Free()

        Dim histogram = New cv.Mat(task.histogramBins, 1, cv.MatType.CV_32F, imagePtr)
        plot.Run(histogram)
        dst2 = plot.dst2

        setTrueText(strOut, 2)
    End Sub
    Public Sub Close()
        Histogram_1D_Close(cPtr)
    End Sub
End Class







Public Class Histogram_Xdimension : Inherits VB_Algorithm
    Dim plot As New Histogram_Depth
    Public Sub New()
        desc = "Plot the histogram of the X layer of the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        plot.Run(task.pcSplit(0))
        dst2 = plot.dst2
        setTrueText("Chart left = " + Format(plot.mm.minVal, fmt0) + vbCrLf +
                    "Chart right = " + Format(plot.mm.maxVal, fmt0), 2)
    End Sub
End Class








Public Class Histogram_Ydimension : Inherits VB_Algorithm
    Dim plot As New Histogram_Depth
    Public Sub New()
        desc = "Plot the histogram of the X layer of the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        plot.Run(task.pcSplit(1))
        dst2 = plot.dst2
        setTrueText("Chart left = " + Format(plot.mm.minVal, fmt0) + vbCrLf +
                    "Chart right = " + Format(plot.mm.maxVal, fmt0), 2)
    End Sub
End Class








Public Class Histogram_Zdimension : Inherits VB_Algorithm
    Dim plot As New Histogram_Depth
    Public Sub New()
        desc = "Plot the histogram of the X layer of the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        plot.Run(task.pcSplit(2))
        dst2 = plot.dst2
        setTrueText("Chart left = " + Format(plot.mm.minVal, fmt0) + vbCrLf +
                    "Chart right = " + Format(plot.mm.maxVal, fmt0), 2)
    End Sub
End Class








Public Class Histogram_Depth : Inherits VB_Algorithm
    Public plot As New Plot_Histogram
    Public rc As rcDataOld
    Public mm As mmData
    Public histogram As New cv.Mat
    Public Sub New()
        desc = "Show depth data as a histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        plot.minRange = 0
        plot.maxRange = task.maxZmeters
        If rc IsNot Nothing Then
            If rc.index = 0 Then Exit Sub
            src = task.pcSplit(2)(rc.rect).Clone
        Else
            If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
            mm = vbMinMax(src)
            plot.minRange = mm.minVal ' because OpenCV's histogram makes the ranges exclusive.
            plot.maxRange = mm.maxVal
        End If

        cv.Cv2.CalcHist({src}, {0}, New cv.Mat, histogram, 1, {task.histogramBins}, {New cv.Rangef(plot.minRange, plot.maxRange)})

        plot.histogram = histogram
        plot.Run(plot.histogram)
        dst2 = plot.dst2

        Dim stepsize = dst2.Width / task.maxZmeters
        For i = 1 To CInt(task.maxZmeters) - 1
            dst2.Line(New cv.Point(stepsize * i, 0), New cv.Point(stepsize * i, dst2.Height), cv.Scalar.White, task.cvFontThickness)
        Next

        If standaloneTest() Then
            Dim expected = src.CountNonZero
            Dim actual = CInt(plot.histogram.Sum(0))
            strOut = "Expected sample count (non-zero task.pcSplit(2) entries):" + vbTab + CStr(expected) + vbCrLf
            strOut += "Histogram sum (ranges can reduce):" + vbTab + vbTab + vbTab + CStr(actual) + vbCrLf
            strOut += "Difference:" + vbTab + vbTab + vbTab + vbTab + vbTab + vbTab + CStr(Math.Abs(actual - expected)) + vbCrLf
            'strOut += "Count nonzero entries in task.maxDepthMask: " + vbTab + vbTab + CStr(task.maxDepthMask.CountNonZero)
        End If
        setTrueText(strOut, 3)
        labels(2) = "Histogram Depth to " + Format(task.maxZmeters, "0.0") + " m"
    End Sub
End Class








Public Class Histogram_DepthNew : Inherits VB_Algorithm
    Public plot As New Plot_Histogram
    Public rc As rcDataNew
    Public mm As mmData
    Public histogram As New cv.Mat
    Public Sub New()
        desc = "Show depth data as a histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        plot.minRange = 0
        plot.maxRange = task.maxZmeters
        If rc IsNot Nothing Then
            If rc.index = 0 Then Exit Sub
            src = task.pcSplit(2)(rc.rect).Clone
        Else
            If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
            mm = vbMinMax(src)
            plot.minRange = mm.minVal ' because OpenCV's histogram makes the ranges exclusive.
            plot.maxRange = mm.maxVal
        End If

        cv.Cv2.CalcHist({src}, {0}, New cv.Mat, histogram, 1, {task.histogramBins}, {New cv.Rangef(plot.minRange, plot.maxRange)})

        plot.histogram = histogram
        plot.Run(plot.histogram)
        dst2 = plot.dst2

        Dim stepsize = dst2.Width / task.maxZmeters
        For i = 1 To CInt(task.maxZmeters) - 1
            dst2.Line(New cv.Point(stepsize * i, 0), New cv.Point(stepsize * i, dst2.Height), cv.Scalar.White, task.cvFontThickness)
        Next

        If standaloneTest() Then
            Dim expected = src.CountNonZero
            Dim actual = CInt(plot.histogram.Sum(0))
            strOut = "Expected sample count (non-zero task.pcSplit(2) entries):" + vbTab + CStr(expected) + vbCrLf
            strOut += "Histogram sum (ranges can reduce):" + vbTab + vbTab + vbTab + CStr(actual) + vbCrLf
            strOut += "Difference:" + vbTab + vbTab + vbTab + vbTab + vbTab + vbTab + CStr(Math.Abs(actual - expected)) + vbCrLf
            'strOut += "Count nonzero entries in task.maxDepthMask: " + vbTab + vbTab + CStr(task.maxDepthMask.CountNonZero)
        End If
        setTrueText(strOut, 3)
        labels(2) = "Histogram Depth to " + Format(task.maxZmeters, "0.0") + " m"
    End Sub
End Class










Public Class Histogram_Cell : Inherits VB_Algorithm
    Dim hist As New Histogram_Depth
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        labels = {"", "", "RedCloud cells", "Histogram of the depth for the selected cell."}
        desc = "Review depth data for a RedCloud Cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        hist.rc = task.rc
        If hist.rc.index = 0 Or hist.rc.maxVec.Z = 0 Then Exit Sub

        dst1.SetTo(0)
        task.pcSplit(2)(hist.rc.rect).CopyTo(dst1)

        hist.Run(dst1)
        dst3 = hist.dst2
    End Sub
End Class






Public Class Histogram_Kalman : Inherits VB_Algorithm
    Public hist As New Histogram_Basics
    Dim kalman As New Kalman_Basics
    Public Sub New()
        labels = {"", "", "With Kalman", "Without Kalman"}
        desc = "Use Kalman to smooth the histogram results."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist.Run(src)
        dst3 = hist.dst2.Clone

        If hist.histogram.Rows = 0 Then hist.histogram = New cv.Mat(task.histogramBins, 1, cv.MatType.CV_32F, 0)

        If kalman.kInput.Length <> task.histogramBins Then ReDim kalman.kInput(task.histogramBins - 1)
        For i = 0 To task.histogramBins - 1
            kalman.kInput(i) = hist.histogram.Get(Of Single)(i, 0)
        Next
        kalman.Run(src)

        hist.histogram = New cv.Mat(kalman.kOutput.Length, 1, cv.MatType.CV_32FC1, kalman.kOutput)
        hist.plot.Run(hist.histogram)
        dst2 = hist.dst2
    End Sub
End Class








Public Class Histogram_PointCloud : Inherits VB_Algorithm
    Public rangesX() As cv.Rangef
    Public rangesY() As cv.Rangef
    Public thresholdSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Histogram X bins", 1, dst2.Cols, 30)
            sliders.setupTrackBar("Histogram Y bins", 1, dst2.Rows, 30)
            sliders.setupTrackBar("Histogram Z bins", 1, 200, 100)
            sliders.setupTrackBar("Histogram threshold", 0, 1000, 500)
        End If

        thresholdSlider = findSlider("Histogram threshold")
        Select Case dst2.Width
            Case 640
                thresholdSlider.Value = 200
            Case 320
                thresholdSlider.Value = 60
            Case 160
                thresholdSlider.Value = 25
        End Select
        labels = {"", "", "Histogram of XZ - X on the Y-Axis and Z on the X-Axis", "Histogram of YZ with Y on the Y-Axis and Z on the X-Axis"}
        desc = "Create a 2D histogram for the pointcloud in XZ and YZ."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static xSlider = findSlider("Histogram X bins")
        Static ySlider = findSlider("Histogram Y bins")
        Static zSlider = findSlider("Histogram Z bins")
        Dim xbins = xSlider.Value
        Dim ybins = ySlider.Value
        Dim zbins = zSlider.Value

        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        rangesX = New cv.Rangef() {New cv.Rangef(-task.xRange, task.xRange), New cv.Rangef(0, task.maxZmeters)}
        rangesY = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange), New cv.Rangef(0, task.maxZmeters)}

        Dim sizesX() As Integer = {xbins, zbins}
        cv.Cv2.CalcHist({src}, {0, 2}, New cv.Mat(), dst2, 2, sizesX, rangesX)
        dst2.Set(Of cv.Point3f)(dst2.Height / 2, 0, New cv.Point3f)

        Dim sizesY() As Integer = {ybins, zbins}
        cv.Cv2.CalcHist({src}, {1, 2}, New cv.Mat(), dst3, 2, sizesY, rangesY)
        dst3.Set(Of cv.Point3f)(dst3.Height / 2, 0, New cv.Point3f)
    End Sub
End Class







Public Class Histogram_Gray : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public plot As New Plot_Histogram
    Public Sub New()
        gOptions.HistBinSlider.Value = 255
        plot.createHistogram = True
        desc = "Create a histogram of the grayscale image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        plot.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = plot.dst2
    End Sub
End Class