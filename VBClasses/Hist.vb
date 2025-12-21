Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Hist_Basics : Inherits TaskParent
        Public histogram As New cv.Mat
        Public mm As mmData
        Public plotHist As New Plot_Histogram
        Public ranges() As cv.Rangef

        Public histArray() As Single
        Public fixedRanges() As cv.Rangef
        Public bins As Integer
        Public autoDisplay As Boolean
        Public histMask As New cv.Mat
        Dim splitIndex As Integer
        Public Sub New()
            If standalone Then taskAlg.gOptions.setHistogramBins(255)
            plotHist.removeZeroEntry = False
            desc = "Create a histogram (no Kalman)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                If taskAlg.heartBeat Then splitIndex = (splitIndex + 1) Mod 3
                mm = GetMinMax(src.ExtractChannel(splitIndex))
                plotHist.backColor = Choose(splitIndex + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red)
            Else
                If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                mm = GetMinMax(src)
            End If
            If fixedRanges Is Nothing Then
                ranges = {New cv.Rangef(mm.minVal - histDelta, mm.maxVal + histDelta)}
            Else
                ranges = fixedRanges
            End If

            ' ranges are EXclusive in OpenCV!!!
            If bins = 0 Then
                cv.Cv2.CalcHist({src}, {splitIndex}, histMask, histogram, 1, {taskAlg.histogramBins}, ranges)
            Else
                cv.Cv2.CalcHist({src}, {splitIndex}, histMask, histogram, 1, {bins}, ranges)
            End If

            ReDim histArray(histogram.Total - 1)
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            If taskAlg.heartBeat Then
                strOut = "Distance" + vbTab + "Value" + vbTab + "Count" + vbTab + "min val: " +
                         vbTab + Format(mm.minVal, fmt1) + vbTab + "max val:" + vbTab +
                         Format(mm.maxVal, fmt1) + vbCrLf
                Dim incr As Single = (mm.maxVal - mm.minVal) / taskAlg.histogramBins
                For i = 0 To histArray.Count - 1
                    strOut += CStr(i) + ":" + vbTab + Format(i * incr, fmt1) + vbTab +
                              Format(histArray(i) / 1000, fmt3) + "k" + vbCrLf
                Next
                SetTrueText(strOut, 3)
            End If

            plotHist.Run(histogram)
            histogram = plotHist.histogram ' reflect any updates to the 0 entry...  
            dst2 = plotHist.dst2

            If standalone Then
                labels(2) = Choose(splitIndex + 1, "Blue", "Green", "Red") + " histogram, bins = " +
                                   CStr(taskAlg.histogramBins) + ", X ranges from " + Format(mm.minVal, "0.0") + " to " +
                                   Format(mm.maxVal, "0.0") + ", y is sample count"
            Else
                labels(2) = "Range = " + Format(ranges(0).Start, fmt3) + " To " + Format(ranges(0).End, fmt3)
            End If
        End Sub
    End Class






    Public Class Hist_Grayscale : Inherits TaskParent
        Public hist As New Hist_Basics
        Public Sub New()
            If standalone Then taskAlg.gOptions.setHistogramBins(255)
            desc = "Create a histogram of the grayscale image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hist.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            dst2 = hist.dst2
            labels = hist.labels
        End Sub
    End Class







    ' https://github.com/opencv/opencv/blob/master/samples/python/hist.py
    Public Class Hist_Graph : Inherits TaskParent
        Public histRaw(3 - 1) As cv.Mat
        Public histNormalized(3 - 1) As cv.Mat
        Public minRange As Single = 0
        Public maxRange As Single = 255
        Public backColor = cv.Scalar.Gray
        Public plotRequested As Boolean
        Public plotColors() As cv.Scalar = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
        Public plotMaxValue As Single
        Public Sub New()
            desc = "Plot histograms for up to 3 channels."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim dimensions() = {taskAlg.histogramBins}
            Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

            Dim plotWidth = dst2.Width / taskAlg.histogramBins

            Dim mm As mmData
            dst2.SetTo(backColor)
            For i = 0 To src.Channels() - 1
                Dim hist As New cv.Mat
                cv.Cv2.CalcHist({src}, {i}, New cv.Mat(), hist, 1, dimensions, ranges)
                histRaw(i) = hist.Clone()
                mm = GetMinMax(histRaw(i))
                histNormalized(i) = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)
                If standaloneTest() Or plotRequested Then
                    Dim points = New List(Of cv.Point)
                    Dim listOfPoints = New List(Of List(Of cv.Point))
                    For j = 0 To taskAlg.histogramBins - 1
                        points.Add(New cv.Point(CInt(j * plotWidth), dst2.Rows - dst2.Rows * histRaw(i).Get(Of Single)(j, 0) / mm.maxVal))
                    Next
                    listOfPoints.Add(points)
                    dst2.Polylines(listOfPoints, False, plotColors(i), taskAlg.lineWidth, taskAlg.lineType)
                End If
            Next

            If standaloneTest() Or plotRequested Then
                plotMaxValue = Math.Round(mm.maxVal / 1000, 0) * 1000 + 1000 ' smooth things out a little for the scale below
                Plot_Basics.AddPlotScale(dst2, 0, plotMaxValue)
                labels(2) = "Histogram for src image (default color) - " + CStr(taskAlg.histogramBins) + " bins"
            End If
        End Sub
    End Class






    Public Class Hist_NormalizeGray : Inherits TaskParent
        Public histogram As New Hist_Basics
        Dim options As New Options_Histogram
        Public Sub New()
            labels(2) = "Use sliders to adjust the image and create a histogram of the results"
            desc = "Create a histogram of a normalized image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst3 = src.Normalize(options.minGray, options.maxGray, cv.NormTypes.MinMax) ' only minMax is working...
            histogram.Run(dst3)
            dst2 = histogram.dst2
        End Sub
    End Class







    Public Class Hist_Simple : Inherits TaskParent
        Public plotHist As New Plot_Histogram
        Public Sub New()
            labels(2) = "Histogram of the grayscale video stream"
            desc = "Build a simple and reusable histogram for grayscale images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}
            If plotHist.minRange = plotHist.maxRange Then
                SetTrueText("The data is just one value - " + Format(plotHist.minRange, fmt1) + vbCrLf +
                        "A histogram is not necessary.", 3)
                Exit Sub
            End If

            Dim hist As New cv.Mat
            cv.Cv2.CalcHist({src}, {0}, New cv.Mat, hist, 1, {taskAlg.histogramBins}, ranges)

            plotHist.Run(hist)
            dst2 = plotHist.dst2
        End Sub
    End Class









    Public Class Hist_ColorsAndGray : Inherits TaskParent
        Dim histogram As New Hist_Basics
        Dim mats As New Mat_4Click
        Public Sub New()
            labels(2) = "Click any quadrant at right to view it below"
            desc = "Create a histogram of a normalized image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim split = src.Split()
            ReDim Preserve split(4 - 1)
            split(4 - 1) = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) ' add a 4th image - the grayscale image to the R G and B images.
            For i = 0 To split.Length - 1
                Dim histSrc = split(i)
                histogram.plotHist.backColor = Choose(i + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.PowderBlue)
                histogram.Run(histSrc)
                mats.mat(i) = histogram.plotHist.dst2.Clone
            Next

            mats.Run(emptyMat)
            dst2 = mats.dst2
            dst3 = mats.dst3
        End Sub
    End Class








    Public Class Hist_Frustrum : Inherits TaskParent
        Dim heat As New HeatMap_Basics
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            taskAlg.gOptions.setGravityUsage(False)
            desc = "Options for the side and top view.  See OptionCommon_Histogram to make settings permanent."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            heat.Run(src)
            dst2 = heat.dst2
            dst3 = heat.dst3

            SetTrueText("This algorithm was created to tune the frustrum and camera locations." + vbCrLf +
                    "Without these tuning parameters the side and top views will look correct." + vbCrLf +
                    "To see how these adjustments work or to add a new camera, " + vbCrLf +
                    "use the HeatMap_Basics algorithm." + vbCrLf +
                    "For new cameras, make the adjustments needed, note the value, and update " + vbCrLf +
                    "the Select statement in the constructor for Options_CameraDetails.", New cv.Point(10, 80), 1)
        End Sub
    End Class









    Public Class Hist_PeakMax : Inherits TaskParent
        Dim hist As New Hist_Basics
        Dim options As New Options_Kalman
        Public Sub New()
            OptionParent.FindCheckBox("Use Kalman").Checked = False
            desc = "Create a histogram and back project into the image the grayscale color with the highest occurance."
            labels(3) = "Grayscale Histogram"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            options.useKalman = False

            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            hist.Run(src)
            dst3 = hist.dst2

            Dim mm As mmData = GetMinMax(hist.histogram)
            Dim brickWidth = dst2.Width / taskAlg.histogramBins
            Dim brickRange = 255 / taskAlg.histogramBins
            Dim histindex = mm.maxLoc.Y
            Dim pixelMin = CInt((histindex) * brickRange)
            Dim pixelMax = CInt((histindex + 1) * brickRange)

            Dim mask = src.InRange(pixelMin, pixelMax).Threshold(1, 255, cv.ThresholdTypes.Binary)
            Dim tmp = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            src.CopyTo(tmp, mask)
            dst2 = tmp.Threshold(0, 255, cv.ThresholdTypes.Binary)

            labels(2) = "BackProjection of most frequent gray pixel"
            dst3.Rectangle(New cv.Rect(brickWidth * histindex, 0, brickWidth, dst2.Height), cv.Scalar.Yellow, 1)
        End Sub
    End Class










    Public Class Hist_PeakFinder : Inherits TaskParent
        Public hist As New Hist_Basics
        Public peakCount As Integer
        Public resetPeaks As Boolean
        Public histogramPeaks As New List(Of Integer)
        Public hCount() As Single
        Dim saveHistBins = taskAlg.histogramBins
        Dim peakCounts(taskAlg.histogramBins) As Single
        Dim allPCounts As New List(Of Integer)
        Dim maxList As New List(Of Integer)
        Public Sub New()
            desc = "Find the peaks - columns taller that both neighbors - in the histogram"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() <> 1 Then src = taskAlg.pcSplit(2)

            hist.Run(src)
            dst2 = hist.dst2

            resetPeaks = False
            If saveHistBins <> taskAlg.histogramBins Then
                resetPeaks = True
                allPCounts.Clear()
                maxList.Clear()
                saveHistBins = taskAlg.histogramBins
                ReDim peakCounts(taskAlg.histogramBins)
                ReDim peakCounts(taskAlg.histogramBins)
            End If
            ReDim hCount(taskAlg.histogramBins)

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
            SetTrueText(vbTab + "Avg peaks: " + CStr(peakCount) + ".  Current: " + CStr(peaks.Count) + " peaks.", New cv.Point(0, 10), 3)

            Dim sortedPeaks = New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
            For i = 0 To peakCounts.Count - 1
                sortedPeaks.Add(peakCounts(i), i)
            Next

            Dim mm As mmData = GetMinMax(histogram)
            If mm.maxVal = 0 Then Exit Sub ' entries are all zero?  Likely camera trouble.
            Dim brickWidth = dst2.Width / histogram.Rows
            histogramPeaks.Clear()
            For i = 0 To Math.Min(sortedPeaks.Count, peakCount) - 1
                Dim index = sortedPeaks.ElementAt(i).Value
                histogramPeaks.Add(index)
                Dim h = CInt(hCount(index) * dst2.Height / mm.maxVal)
                cv.Cv2.Rectangle(dst2, New cv.Rect(index * brickWidth, dst2.Height - h, brickWidth, h), cv.Scalar.Yellow, taskAlg.lineWidth)
            Next

            If allPCounts.Count > 100 Then
                allPCounts.RemoveAt(0)
                maxList.RemoveAt(0)
            End If
            If Math.Abs(maxList.Average - maxIndex) > saveHistBins / 10 Then saveHistBins = 0
            labels(2) = "There were " + CStr(peakCount) + " depth peaks (highlighted) up to " + CStr(CInt(taskAlg.MaxZmeters)) + " meters.  " +
                    "Use global option Histogram Bins to set the number of bins."
        End Sub
    End Class










    Public Class Hist_PeaksDepth : Inherits TaskParent
        Dim peaks As New Hist_PeakFinder
        Public Sub New()
            desc = "Find the peaks - columns taller that both neighbors - in the histogram"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            peaks.Run(taskAlg.pcSplit(2))
            dst2 = peaks.dst2
            labels(2) = peaks.labels(2)
        End Sub
    End Class











    Public Class Hist_PeaksRGB : Inherits TaskParent
        Public mats As New Mat_4Click
        Dim peaks(2) As Hist_PeakFinder
        Public Sub New()
            peaks(0) = New Hist_PeakFinder
            peaks(1) = New Hist_PeakFinder
            peaks(2) = New Hist_PeakFinder
            labels(2) = "Upper left is Blue, upper right is Green, bottom left is Red"
            desc = "Find the peaks and valleys for each of the BGR channels."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim split = src.Split()
            For i = 0 To 3 - 1
                peaks(i).hist.plotHist.backColor = Choose(i + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red)
                peaks(i).hist.plotHist.addLabels = False
                peaks(i).Run(split(i))
                mats.mat(i) = peaks(i).dst2.Clone
            Next

            If taskAlg.optionsChanged Then
                taskAlg.mouseClickFlag = True
                taskAlg.mousePicTag = 2
            End If

            mats.Run(emptyMat)
            dst2 = mats.dst2
            dst3 = mats.dst3
        End Sub
    End Class








    Public Class Hist_Color : Inherits TaskParent
        Public histogram As New cv.Mat
        Public plotHist As New Plot_Histogram
        Public ranges() As cv.Rangef
        Public Sub New()
            desc = "Create a histogram of green and red."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            ranges = New cv.Rangef() {New cv.Rangef(0, 255), New cv.Rangef(0, 255)}
            cv.Cv2.CalcHist({src}, {1, 2}, New cv.Mat, histogram, 1, {taskAlg.histogramBins, taskAlg.histogramBins}, ranges)

            Dim test = histogram.Normalize(0, 255, cv.NormTypes.MinMax)

            Dim input As New cv.Mat
            src.ConvertTo(input, cv.MatType.CV_32FC3)
            Dim mask As New cv.Mat
            cv.Cv2.CalcBackProject({input}, {1, 2}, histogram, mask, ranges)

            Dim mm As mmData = GetMinMax(mask)

            plotHist.Run(test)
            dst2 = plotHist.dst2
        End Sub
    End Class








    Public Class Hist_KalmanAuto : Inherits TaskParent
        Public histogram As New cv.Mat
        Public plotHist As New Plot_Histogram
        Dim mm As mmData
        Public ranges() As cv.Rangef
        Dim splitIndex = 0
        Dim colorName = "Gray"
        Public Sub New()
            taskAlg.kalman = New Kalman_Basics
            desc = "Create a histogram of the grayscale image and smooth the bar chart with a kalman filter."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                If taskAlg.heartBeat Then splitIndex = If(splitIndex < 2, splitIndex + 1, 0)
                colorName = Choose(splitIndex + 1, "Blue", "Green", "Red")
                Dim split = src.Split()
                src = split(splitIndex)
            End If

            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            mm = GetMinMax(src)
            ranges = New cv.Rangef() {New cv.Rangef(mm.minVal, mm.maxVal)}

            If mm.minVal = mm.maxVal Then
                SetTrueText("The input image is empty - minVal and maxVal are both zero...")
                Exit Sub
            End If
            Dim dimensions() = {taskAlg.histogramBins}
            cv.Cv2.CalcHist({src}, {0}, New cv.Mat, histogram, 1, dimensions, ranges)

            If taskAlg.kalman.kInput.Length <> taskAlg.histogramBins Then ReDim taskAlg.kalman.kInput(taskAlg.histogramBins - 1)

            For i = 0 To taskAlg.histogramBins - 1
                taskAlg.kalman.kInput(i) = histogram.Get(Of Single)(i, 0)
            Next
            taskAlg.kalman.Run(emptyMat)
            histogram = cv.Mat.FromPixelData(taskAlg.kalman.kOutput.Length, 1, cv.MatType.CV_32FC1, taskAlg.kalman.kOutput)

            Dim splitColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
            If standaloneTest() Then plotHist.backColor = splitColors(splitIndex)
            plotHist.Run(histogram)
            dst2 = plotHist.dst2

            labels(2) = colorName + " histogram, bins = " + CStr(taskAlg.histogramBins) + ", X ranges from " +
                    Format(mm.minVal, "0.0") + " to " + Format(mm.maxVal, "0.0") + ", y is occurances"
        End Sub
    End Class










    ' https://docs.opencvb.org/master/d1/db7/tutorial_py_Hist_begins.html
    Public Class Hist_EqualizeColor : Inherits TaskParent
        Public kalmanEq As New Hist_Basics
        Public kalman As New Hist_Basics
        Dim mats As New Mat_2to1
        Public displayHist As Boolean = False
        Public channel = 2
        Public Sub New()
            kalmanEq.plotHist.addLabels = False
            kalman.plotHist.addLabels = False
            desc = "Create an equalized histogram of the color image."
            labels(2) = "Image Enhanced with Equalized Histogram"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim rgb(2) As cv.Mat
            Dim rgbEq(2) As cv.Mat
            rgbEq = taskAlg.color.Split()

            For i = 0 To rgb.Count - 1
                cv.Cv2.EqualizeHist(rgbEq(i), rgbEq(i))
            Next

            If standaloneTest() Or displayHist Then
                cv.Cv2.Split(taskAlg.color, rgb) ' equalizehist alters the input...
                kalman.plotHist.backColor = cv.Scalar.Red
                kalman.Run(rgb(channel).Clone())
                mats.mat(0) = kalman.dst2.Clone()

                kalmanEq.Run(rgbEq(channel).Clone())
                mats.mat(1) = kalmanEq.dst2.Clone()

                mats.Run(emptyMat)
                dst3 = mats.dst2
                labels(3) = "Before (top) and After Red Histogram"
            End If

            cv.Cv2.Merge(rgbEq, dst2)
        End Sub
    End Class









    Public Class Hist_CompareGray : Inherits TaskParent
        Public histK As New Hist_Kalman
        Dim options As New Options_HistCompare
        Public histDiff As New cv.Mat
        Public histDiffAbs As New cv.Mat
        Public normHistDiff As New cv.Mat
        Public normHistDiffAbs As New cv.Mat
        Public Sub New()
            labels(2) = "Kalman-smoothed current histogram"
            desc = "Compare grayscale histograms for successive frames"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            histK.Run(src)
            dst2 = histK.dst2.Clone
            Static lastHist As cv.Mat = histK.hist.histogram

            Dim histNorm As cv.Mat = histK.hist.histogram.Normalize(0, 1, cv.NormTypes.MinMax)
            Static lastHistNorm As cv.Mat = histNorm.Clone

            If lastHistNorm.Size = histK.hist.histogram.Size Then
                Dim Comparison = cv.Cv2.CompareHist(histNorm, lastHistNorm, options.compareMethod)
                If Double.IsNaN(Comparison) Then Comparison = 0
                labels(3) = "CompareHist output = " + Format(Comparison, fmt3) + " using " + options.compareName + " method"
                trueData = New List(Of TrueText)(histK.hist.plotHist.trueData)
                SetTrueText(labels(3), 2)
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









    Public Class Hist_ComparePlot : Inherits TaskParent
        Dim comp As New Hist_CompareGray
        Dim ttLabels As New List(Of TrueText)
        Public Sub New()
            labels(3) = "Differences have been multiplied by 1000 to build scale at the left"
            desc = "Compare grayscale histograms for successive frames and plot the difference as a histogram."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            comp.Run(src)
            dst2 = comp.dst2.Clone

            If taskAlg.heartBeat Then
                ttLabels = New List(Of TrueText)(comp.trueData)
                Dim histX = comp.histDiffAbs
                comp.histK.hist.plotHist.Run(histX)
                dst3 = comp.histK.hist.plotHist.dst2.Clone

                Dim mm As mmData = GetMinMax(histX)
                Plot_Basics.AddPlotScale(dst2, 0, mm.maxVal)
            End If
            trueData = New List(Of TrueText)(ttLabels)
        End Sub
    End Class







    Public Class Hist_CompareNumber : Inherits TaskParent
        Dim comp As New Hist_CompareGray
        Dim plot As New Plot_OverTimeScalar
        Public Sub New()
            If standaloneTest() Then taskAlg.gOptions.displayDst1.Checked = True
            plot.plotCount = 2

            labels = {"", "", "Kalman-smoothed normalized histogram output", "Plot of the sum of the differences between recent normalized histograms"}
            desc = "The idea is to reduce a comparison of 2 histograms to a single number"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            comp.Run(src)
            dst1 = comp.dst2.Clone

            Dim sum = cv.Cv2.Sum(comp.normHistDiff)(0) * 100
            Dim sumAbs = cv.Cv2.Sum(comp.normHistDiffAbs)(0) * 100
            plot.plotData = New cv.Scalar(sum, sumAbs, 0)
            plot.Run(src)
            dst2 = plot.dst2
            dst3 = plot.dst3

            SetTrueText("Upper left is the sum * 100 of the difference" + vbCrLf + "Upper right is the sum of the absolute values * 100", New cv.Point(0, dst2.Height / 2), 2)
        End Sub
    End Class






    ' https://study.marearts.com/2014/11/opencv-emdearth-mover-distance-example.html
    Public Class Hist_CompareEMD_hsv : Inherits TaskParent
        Dim hist As New Hist_Basics
        Public Sub New()
            labels = {"", "", "Kalman-smoothed normalized histogram output", "Plot of the sum of the differences between recent normalized histograms"}
            desc = "Use OpenCV's Earth Mover Distance to compare 2 images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
            Static lastHSV As cv.Mat = hsv.Clone

            Dim hBins = 30, sBins = 32
            Dim histA As New cv.Mat, histB As New cv.Mat
            Dim ranges = New cv.Rangef() {New cv.Rangef(0, 180), New cv.Rangef(0, 256)}

            cv.Cv2.CalcHist({hsv}, {0, 1}, New cv.Mat, histA, 2, {hBins, sBins}, ranges)
            Dim histNormA As cv.Mat = histA.Normalize(0, 1, cv.NormTypes.MinMax)

            cv.Cv2.CalcHist({lastHSV}, {0, 1}, New cv.Mat, histB, 2, {hBins, sBins}, ranges)
            Dim histNormB As cv.Mat = histB.Normalize(0, 1, cv.NormTypes.MinMax)

            Dim sig1 = New cv.Mat(sBins * hBins, 3, cv.MatType.CV_32F, cv.Scalar.All(0))
            Dim sig2 = New cv.Mat(sBins * hBins, 3, cv.MatType.CV_32F, cv.Scalar.All(0))
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
            SetTrueText("EMD similarity from the current image to the last is " + Format(1 - emd, "0.0%"), 2)

            lastHSV = hsv.Clone
        End Sub
    End Class








    Public Class Hist_Peaks : Inherits TaskParent
        Dim masks As New BackProject_Masks
        Public Sub New()
            desc = "Interactive Histogram"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            masks.Run(src)
            dst2 = masks.dst2
            dst3 = masks.dst3
        End Sub
    End Class







    Public Class Hist_Lab : Inherits TaskParent
        Dim hist As New Hist_Basics
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"Lab Colors ", "Lab Channel 0", "Lab Channel 1", "Lab Channel 2"}
            desc = "Create a histogram from a BGR image converted to LAB."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
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








    Public Class Hist_PointCloudXYZ : Inherits TaskParent
        Public plotHist As New Plot_Histogram
        Public Sub New()
            plotHist.createHistogram = True
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels = {"", "Histogram of the X channel", "Histogram of the Y channel", "Histogram of the Z channel"}
            desc = "Show individual channel of the point cloud data as a histogram."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static ttlists As New List(Of List(Of TrueText))({New List(Of TrueText), New List(Of TrueText), New List(Of TrueText)})
            For i = 0 To 2
                dst0 = taskAlg.pcSplit(i)
                Dim mm As mmData = GetMinMax(dst0)

                Select Case i
                    Case 0
                        plotHist.removeZeroEntry = False
                        plotHist.minRange = -taskAlg.xRange
                        plotHist.maxRange = taskAlg.xRange
                    Case 1
                        plotHist.removeZeroEntry = False
                        plotHist.minRange = -taskAlg.yRange
                        plotHist.maxRange = taskAlg.yRange
                    Case 2
                        plotHist.removeZeroEntry = True
                        plotHist.minRange = 0
                        plotHist.maxRange = taskAlg.MaxZmeters
                End Select

                plotHist.Run(dst0)
                Select Case i
                    Case 0
                        dst1 = plotHist.dst2.Clone
                    Case 1
                        dst2 = plotHist.dst2.Clone
                    Case 2
                        dst3 = plotHist.dst2.Clone
                End Select
                If taskAlg.heartBeat Then labels(i + 1) = "Histogram " + Choose(i + 1, "X", "Y", "Z") + " ranges from " +
                                   Format(plotHist.minRange, "0.0") + "m to " + Format(plotHist.maxRange, "0.0") + "m"
            Next
        End Sub
    End Class







    Public Class Hist_FlatSurfaces : Inherits TaskParent
        Dim masks As New BackProject_Masks
        Public Sub New()
            desc = "Find flat surfaces with the histogram"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim maxRange = 4
            Dim cloudY = taskAlg.pcSplit(1).Clone
            Dim mm As mmData = GetMinMax(cloudY)
            cloudY = cloudY.Threshold(maxRange, mm.maxVal, cv.ThresholdTypes.Trunc)
            Static saveMinVal = mm.minVal, saveMaxVal = mm.maxVal
            If taskAlg.heartBeat Then
                saveMinVal = mm.minVal
                saveMaxVal = mm.maxVal
            End If

            If saveMinVal > mm.minVal Then saveMinVal = mm.minVal
            If saveMaxVal < mm.maxVal Then saveMaxVal = mm.maxVal

            cloudY.Set(Of Single)(mm.minLoc.Y, mm.minLoc.X, -saveMinVal)
            cloudY.Set(Of Single)(mm.maxLoc.Y, mm.maxLoc.X, saveMaxVal)
            cloudY = (cloudY - saveMinVal).tomat
            cloudY = cloudY.ConvertScaleAbs(255 / (-saveMinVal + saveMaxVal))
            mm = GetMinMax(cloudY)
            cloudY.SetTo(0, taskAlg.noDepthMask)
            masks.Run(cloudY)
            dst2 = masks.dst2
            dst3 = src
            dst3 = dst3.SetTo(white, masks.dst1)
            labels(2) = "Range for the histogram is from " + Format(saveMinVal, fmt1) + " to " + Format(saveMaxVal, fmt1)
        End Sub
    End Class







    Public Class Hist_ShapeSide : Inherits TaskParent
        Public rc As New oldrcData
        Public Sub New()
            taskAlg.gOptions.setHistogramBins(60)
            labels = {"", "", "ZY Side View", "ZY Side View Mask"}
            desc = "Create a 2D side view for ZY histogram of depth"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If rc.pixels = 0 Then src = taskAlg.pointCloud

            cv.Cv2.CalcHist({src}, taskAlg.channelsSide, New cv.Mat, dst0, 2,
                        {taskAlg.histogramBins, taskAlg.histogramBins}, taskAlg.rangesSide)
            dst0.Col(0).SetTo(0) ' too many zero depth points...

            dst0 = Mat_Convert.Mat_32f_To_8UC3(dst0)
            dst0.ConvertTo(dst0, cv.MatType.CV_8UC1)

            Dim r As New cv.Rect(0, 0, dst2.Height, dst2.Height)
            dst2(r) = dst0.Resize(New cv.Size(dst2.Height, dst2.Height), 0, 0, cv.InterpolationFlags.Nearest)
            dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        End Sub
    End Class







    Public Class Hist_ShapeTop : Inherits TaskParent
        Public rc As New oldrcData
        Public Sub New()
            taskAlg.gOptions.setHistogramBins(60)
            labels = {"", "", "ZY Side View", "ZY Side View Mask"}
            desc = "Create a 2D top view for XZ histogram of depth"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If rc.pixels = 0 Then src = taskAlg.pointCloud

            cv.Cv2.CalcHist({src}, taskAlg.channelsTop, New cv.Mat, dst0, 2,
                        {taskAlg.histogramBins, taskAlg.histogramBins}, taskAlg.rangesTop)
            dst0.Row(0).SetTo(0) ' too many zero depth points...

            dst0 = Mat_Convert.Mat_32f_To_8UC3(dst0)
            dst0.ConvertTo(dst0, cv.MatType.CV_8UC1)

            Dim r As New cv.Rect(0, 0, dst2.Height, dst2.Height)
            dst2(r) = dst0.Resize(New cv.Size(dst2.Height, dst2.Height), 0, 0, cv.InterpolationFlags.Nearest)
            dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        End Sub
    End Class









    Public Class Hist_Gotcha2D : Inherits TaskParent
        Public histogram As New cv.Mat
        Public Sub New()
            labels(2) = "ZY (Side View)"
            desc = "Create a 2D side view for ZY histogram of depth using integer values.  Testing calcHist gotcha."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim expected = taskAlg.pcSplit(2).CountNonZero
            Dim ranges = taskAlg.rangesSide
            If taskAlg.toggleOn Then
                ranges = New cv.Rangef() {New cv.Rangef(-10, +10), New cv.Rangef(-1, 20)}
            End If
            cv.Cv2.CalcHist({taskAlg.pointCloud}, taskAlg.channelsSide, New cv.Mat, histogram, 2, taskAlg.bins2D, taskAlg.rangesSide)

            Dim actual = histogram.Sum(0)

            If taskAlg.heartBeat Then
                strOut = "Expected sample count:" + vbTab + CStr(expected) + vbCrLf +
                     "Actual sample count:" + vbTab + CStr(actual) + vbCrLf +
                     "The number of samples input is the expected value." + vbCrLf +
                     "The number of entries in the histogram is the 'actual' number of samples." + vbCrLf +
                     "How can the values not be equal?  The ranges of the histogram are exclusive." + vbCrLf +
                     "Another way that samples may be lost: X or Y range.  Use Y-Range slider to show impact." +
                     "A third way samples may not match: max depth can toss samples as well."
            End If
            SetTrueText(strOut, 3)
            dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        End Sub
    End Class






    Public Class Hist_Gotcha : Inherits TaskParent
        Public histogram As New cv.Mat
        Dim hist As New Hist_Basics
        Public Sub New()
            labels(2) = "Grayscale histogram"
            desc = "Simple test: input samples should equal histogram samples.  What is wrong?  Exclusive ranges!"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Dim expected = src.Total

            hist.Run(src)

            Dim actual = hist.histogram.Sum(0)

            If taskAlg.heartBeat Then
                strOut = "Expected sample count:" + vbTab + CStr(expected) + vbCrLf +
                     "Actual sample count:" + vbTab + CStr(actual) + vbCrLf +
                     "Difference:" + vbTab + vbTab + CStr(Math.Abs(actual - expected)) + vbCrLf +
                     "The number of samples input is the expected value." + vbCrLf +
                     "The number of entries in the histogram is the 'actual' number of samples." + vbCrLf +
                     "How can the values not be equal?  The ranges in the histogram are exclusive!"
            End If
            SetTrueText(strOut, 2)
        End Sub
    End Class





    Public Class Hist_GotchaFixed_CPP : Inherits TaskParent
        Public Sub New()
            cPtr = Hist_1D_Open()
            desc = "Testing the C++ CalcHist to investigate gotcha with sample counts"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Dim cppData(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = Hist_1D_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, taskAlg.histogramBins)
            handleSrc.Free()

            If taskAlg.heartBeat Then
                Dim actual = CInt(Hist_1D_Sum(cPtr))
                strOut = "Expected sample count:" + vbTab + CStr(dst2.Total) + vbCrLf +
                     "Actual sample count:" + vbTab + CStr(actual) + vbCrLf +
                     "Difference:" + vbTab + vbTab + CStr(Math.Abs(actual - dst2.Total)) + vbCrLf +
                     "The number of samples input is the expected value." + vbCrLf +
                     "The number of entries in the histogram is the 'actual' number of samples." + vbCrLf +
                     "How can the values not be equal?  The ranges in the histogram are exclusive!"
            End If
            SetTrueText(strOut, 2)
        End Sub
        Public Sub Close()
            Hist_1D_Close(cPtr)
        End Sub
    End Class




    Public Class Hist_Byte_CPP : Inherits TaskParent
        Public plotHist As New Plot_Histogram
        Public Sub New()
            cPtr = Hist_1D_Open()
            desc = "For Byte histograms, the C++ code works but the .Net interface doesn't honor exclusive ranges."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Dim cppData(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
            Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = Hist_1D_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, taskAlg.histogramBins)
            handleSrc.Free()

            Dim histogram = cv.Mat.FromPixelData(taskAlg.histogramBins, 1, cv.MatType.CV_32F, imagePtr)
            plotHist.Run(histogram)
            dst2 = plotHist.dst2

            SetTrueText(strOut, 2)
        End Sub
        Public Sub Close()
            Hist_1D_Close(cPtr)
        End Sub
    End Class





    Public Class Hist_Cloud : Inherits TaskParent
        Dim myPlotHist As New Hist_Depth
        Public histArray() As Single
        Public dimensionLabel As String = "X"
        Dim maxMaxVal As Double
        Public Sub New()
            myPlotHist.plotHist.removeZeroEntry = True
            desc = "Plot the histogram of the X layer of the point cloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32F Then src = taskAlg.pcSplit(0)
            Dim mm = GetMinMax(src)
            Dim norm32f = src + Math.Abs(mm.minVal)
            If mm.maxVal > maxMaxVal Then maxMaxVal = mm.maxVal
            myPlotHist.plotHist.maxRange = maxMaxVal
            myPlotHist.Run(norm32f)
            dst2 = myPlotHist.dst2
            histArray = myPlotHist.plotHist.histArray
            If taskAlg.heartBeat Then
                strOut = "Chart left = 0 " + vbCrLf + "Chart right = " + Format(mm.maxVal, fmt0) + vbCrLf
                labels(2) = "Shifted " + dimensionLabel + " Histogram Range = 0 to " + CStr(CInt(mm.maxVal))
            End If
            SetTrueText(strOut, 3)
        End Sub
    End Class





    Public Class Hist_CloudX : Inherits TaskParent
        Dim histDim As New Hist_Cloud
        Public Sub New()
            histDim.dimensionLabel = "X"
            desc = "Plot the histogram of the X layer of the point cloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            histDim.Run(taskAlg.pcSplit(0))
            dst2 = histDim.dst2
            labels = histDim.labels
            SetTrueText(histDim.strOut, 3)
        End Sub
    End Class




    Public Class Hist_CloudY : Inherits TaskParent
        Dim histDim As New Hist_Cloud
        Public Sub New()
            histDim.dimensionLabel = "Y"
            desc = "Plot the histogram of the X layer of the point cloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            histDim.Run(taskAlg.pcSplit(1))
            dst2 = histDim.dst2
            labels = histDim.labels
            SetTrueText(histDim.strOut, 3)
        End Sub
    End Class




    Public Class Hist_CloudZ : Inherits TaskParent
        Dim histDim As New Hist_Cloud
        Public Sub New()
            histDim.dimensionLabel = "Z"
            desc = "Plot the histogram of the X layer of the point cloud"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            histDim.Run(taskAlg.pcSplit(2))
            dst2 = histDim.dst2
            labels = histDim.labels
            SetTrueText(histDim.strOut, 3)
        End Sub
    End Class









    Public Class Hist_Depth : Inherits TaskParent
        Public plotHist As New Plot_Histogram
        Public rc As oldrcData
        Public mm As mmData
        Public histogram As New cv.Mat
        Public Sub New()
            plotHist.minRange = 0.1
            plotHist.removeZeroEntry = True
            taskAlg.gOptions.MaxDepthBar.Value = 10
            taskAlg.gOptions.HistBinBar.Value = 10
            desc = "Show depth data as a histogram."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Rows <= 0 Then Exit Sub
            plotHist.minRange = 0
            plotHist.maxRange = taskAlg.MaxZmeters
            If rc IsNot Nothing Then
                If rc.index = 0 Then Exit Sub
                src = taskAlg.pcSplit(2)(rc.rect).Clone
            Else
                If src.Type <> cv.MatType.CV_32F Then src = taskAlg.pcSplit(2)
                mm = GetMinMax(src)
                If mm.minVal = mm.maxVal Then Exit Sub
                plotHist.minRange = mm.minVal ' because OpenCV's histogram makes the ranges exclusive.
                plotHist.maxRange = mm.maxVal
            End If

            If plotHist.minRange = plotHist.maxRange Then Exit Sub ' at startup some cameras have no depth...
            cv.Cv2.CalcHist({src}, {0}, New cv.Mat, histogram, 1, {taskAlg.histogramBins},
                        {New cv.Rangef(plotHist.minRange, plotHist.maxRange)})

            plotHist.histogram = histogram
            plotHist.maxRange = taskAlg.MaxZmeters
            plotHist.Run(plotHist.histogram)
            dst2 = plotHist.dst2

            Dim stepsize = dst2.Width / taskAlg.MaxZmeters
            For i = 1 To CInt(taskAlg.MaxZmeters) - 1
                dst2.Line(New cv.Point(stepsize * i, 0), New cv.Point(stepsize * i, dst2.Height), white, taskAlg.cvFontThickness)
            Next

            If standaloneTest() Then
                Dim expected = src.CountNonZero
                Dim actual = CInt(plotHist.histogram.Sum(0))
                strOut = "Expected sample count (non-zero taskAlg.pcSplit(2) entries):" + vbTab + CStr(expected) + vbCrLf
                strOut += "Histogram sum (ranges can reduce):" + vbTab + vbTab + vbTab + CStr(actual) + vbCrLf
                strOut += "Difference:" + vbTab + vbTab + vbTab + vbTab + vbTab + vbTab + CStr(Math.Abs(actual - expected)) + vbCrLf
                'strOut += "Count nonzero entries in taskAlg.maxDepthMask: " + vbTab + vbTab + CStr(taskAlg.maxDepthMask.CountNonZero)
            End If
            SetTrueText(strOut, 3)
            labels(2) = "Histogram Depth to " + Format(taskAlg.MaxZmeters, "0.0") + " m"
        End Sub
    End Class





    Public Class Hist_Kalman : Inherits TaskParent
        Public hist As New Hist_Basics
        Public Sub New()
            taskAlg.kalman = New Kalman_Basics
            labels = {"", "", "With Kalman", "Without Kalman"}
            desc = "Use Kalman to smooth the histogram sharedResults.images.."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hist.Run(src)
            dst3 = hist.dst2.Clone

            If hist.histogram.Rows = 0 Then
                hist.histogram = New cv.Mat(taskAlg.histogramBins, 1, cv.MatType.CV_32F, cv.Scalar.All(0))
            End If

            If taskAlg.kalman.kInput.Length <> taskAlg.histogramBins Then ReDim taskAlg.kalman.kInput(taskAlg.histogramBins - 1)
            For i = 0 To taskAlg.histogramBins - 1
                taskAlg.kalman.kInput(i) = hist.histogram.Get(Of Single)(i, 0)
            Next
            taskAlg.kalman.Run(emptyMat)

            hist.histogram = cv.Mat.FromPixelData(taskAlg.kalman.kOutput.Length, 1, cv.MatType.CV_32FC1, taskAlg.kalman.kOutput)
            hist.plotHist.Run(hist.histogram)
            dst2 = hist.dst2
        End Sub
    End Class




    Public Class Hist_DepthSimple : Inherits TaskParent
        Public histList As New List(Of Single)
        Public histArray() As Single
        Public histogram As New cv.Mat
        Dim plotHist As New Plot_Histogram
        Dim mm As mmData
        Public inputOnlyMask As New cv.Mat
        Public ranges() As cv.Rangef
        Public Sub New()
            labels(2) = "Histogram of depth from 0 to maxZMeters."
            plotHist.addLabels = False
            desc = "Use Kalman to smooth the histogram sharedResults.images.."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                mm = GetMinMax(taskAlg.pcSplit(2))
                If mm.minVal = mm.maxVal Then Exit Sub
                ranges = {New cv.Rangef(mm.minVal, mm.maxVal)}
            End If

            cv.Cv2.CalcHist({taskAlg.pcSplit(2)}, {0}, inputOnlyMask, histogram, 1, {taskAlg.histogramBins}, ranges)
            ReDim histArray(histogram.Total - 1)
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            If standaloneTest() Then
                plotHist.Run(histogram)
                dst2 = plotHist.dst2
            End If
            histList = histArray.ToList
        End Sub
    End Class








    Public Class Hist_CloudSegments : Inherits TaskParent
        Dim plot As New Plot_Histogram
        Public trimHist As New cv.Mat
        Dim options As New Options_Outliers
        Dim index As Integer = 2
        Dim mm As mmData
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            plot.createHistogram = True
            plot.removeZeroEntry = False
            desc = "Find the segments of X, Y, and Z values from the point cloud."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If standalone Then
                If taskAlg.heartBeatLT Or taskAlg.optionsChanged Then
                    index += 1
                    If index > 2 Then index = 0
                End If
            End If

            If src.Type <> cv.MatType.CV_32FC1 Then src = taskAlg.pcSplit(index)
            If taskAlg.heartBeat Then
                mm = GetMinMax(src)
                If index < 2 Then
                    mm.minVal -= 1
                    mm.maxVal += 1
                    src = (src - mm.minVal).ToMat
                End If
            End If

            Dim incr = (mm.maxVal - mm.minVal) / taskAlg.histogramBins
            plot.minRange = mm.minVal
            plot.maxRange = mm.maxVal
            plot.Run(src)
            dst2 = plot.dst2.Clone

            labels(2) = "Plot of " + Choose(index + 1, "X", "Y", "Z") + " data in point cloud"
            labels(3) = "Min = " + Format(mm.minVal, fmt1) + " max = " + Format(mm.maxVal, fmt1)

            dst1.SetTo(0)
            For i = 0 To plot.histogram.Rows - 1
                Dim mask = src.InRange(i * incr, (i + 1) * incr).ConvertScaleAbs
                dst1.SetTo(i + 1, mask)
            Next
            dst1.SetTo(0, taskAlg.noDepthMask)
            dst3 = PaletteFull((dst1 + 1))
            dst3.SetTo(0, taskAlg.noDepthMask)
        End Sub
    End Class






    Public Class Hist_RedCell : Inherits TaskParent
        Dim hist As New Hist_Depth
        Public Sub New()
            dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
            labels = {"", "", "RedCloud cells", "Histogram of the depth for the selected cell."}
            desc = "Review depth data for a RedCloud Cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedList(src, labels(2))
            hist.rc = taskAlg.oldrcD
            If hist.rc.index = 0 Or hist.rc.mmZ.maxVal = 0 Then Exit Sub

            dst0.SetTo(0)
            taskAlg.pcSplit(2)(hist.rc.rect).CopyTo(dst0)

            hist.Run(dst0)
            dst3 = hist.dst2
        End Sub
    End Class





    Public Class Hist_GridCell : Inherits TaskParent
        Dim histogram As New cv.Mat
        Public mask As New cv.Mat
        Public histarray(0) As Single
        Public Sub New()
            desc = "Build a histogram of the cell contents"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone And src.Channels = 3 Then src = taskAlg.grayStable
            Dim mm = GetMinMax(src)
            ReDim histarray(mm.maxVal)
            If mm.maxVal > 0 Then
                cv.Cv2.CalcHist({src}, {0}, mask, histogram, 1, {mm.maxVal + 1}, New cv.Rangef() {New cv.Rangef(0, mm.maxVal + 1)})
                If histogram.Rows > 0 Then
                    Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)
                End If
            End If
            If standalone And histarray.Count > 1 Then
                Static plotHist As New Plot_Histogram
                plotHist.Run(histogram)
                dst2 = plotHist.dst2
            End If
        End Sub
    End Class









    Public Class Hist_ToggleFeatureLess : Inherits TaskParent
        Dim fLess As New BrickPoint_FeatureLess
        Dim plotHist As New Plot_Histogram
        Public Sub New()
            plotHist.maxRange = 255
            plotHist.minRange = 0
            plotHist.removeZeroEntry = False
            plotHist.createHistogram = True
            taskAlg.gOptions.setHistogramBins(255)
            desc = "Toggle between a histogram of the entire image and one of the featureless regions found with grid points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(src)
            dst3 = fLess.dst2
            labels(3) = fLess.labels(2)

            If taskAlg.toggleOn Then
                plotHist.histMask = New cv.Mat
                labels(2) = "Histogram of the whole image."
            Else
                plotHist.histMask = fLess.dst1.Clone
                labels(2) = "Histogram of just the featureless regions."
            End If
            plotHist.Run(taskAlg.grayStable)
            dst2 = plotHist.dst2
        End Sub
    End Class








    'https://docs.opencvb.org/master/d1/db7/tutorial_py_Hist_begins.html
    Public Class Hist_EqualizeGray : Inherits TaskParent
        Public histogramEQ As New Hist_Basics
        Public histogram As New Hist_Basics
        Dim mats As New Mat_4to1
        Public Sub New()
            histogramEQ.plotHist.addLabels = False
            histogram.plotHist.addLabels = False
            labels(2) = "Equalized image"
            labels(3) = "Orig. Hist, Eq. Hist, Orig. Image, Eq. Image"
            desc = "Create an equalized histogram of the grayscale image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else src = taskAlg.grayStable
            histogram.Run(src)
            cv.Cv2.EqualizeHist(taskAlg.grayStable, dst2)
            histogramEQ.Run(dst2)
            mats.mat(0) = histogram.dst2.Clone
            mats.mat(1) = histogramEQ.dst2
            mats.mat(2) = src
            mats.mat(3) = dst2
            mats.Run(emptyMat)
            dst3 = mats.dst2
        End Sub
    End Class








    Public Class Hist_PointCloud_XZ_YZ : Inherits TaskParent
        Public rangesX() As cv.Rangef
        Public rangesY() As cv.Rangef
        Public options As New Options_HistPointCloud
        Public Sub New()
            labels = {"", "", "Histogram of XZ - X on the Y-Axis and Z on the X-Axis", "Histogram of YZ with Y on the Y-Axis and Z on the X-Axis"}
            desc = "Create a 2D histogram for the pointcloud in XZ and YZ."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Type <> cv.MatType.CV_32FC3 Then src = taskAlg.pointCloud
            rangesX = New cv.Rangef() {New cv.Rangef(-taskAlg.xRange, taskAlg.xRange), New cv.Rangef(0, taskAlg.MaxZmeters)}
            rangesY = New cv.Rangef() {New cv.Rangef(-taskAlg.yRange, taskAlg.yRange), New cv.Rangef(0, taskAlg.MaxZmeters)}

            Dim sizesX() As Integer = {options.xBins, options.zBins}
            cv.Cv2.CalcHist({src}, {0, 2}, New cv.Mat(), dst2, 2, sizesX, rangesX)
            dst2.Set(Of cv.Point3f)(dst2.Height / 2, 0, New cv.Point3f)

            Dim sizesY() As Integer = {options.yBins, options.zBins}
            cv.Cv2.CalcHist({src}, {1, 2}, New cv.Mat(), dst3, 2, sizesY, rangesY)
            dst3.Set(Of cv.Point3f)(dst3.Height / 2, 0, New cv.Point3f)
        End Sub
    End Class






    Public Class Hist_PointCloud : Inherits TaskParent
        Dim grid As New Grid_Basics
        Public histogram As New cv.Mat
        Public histArray() As Single
        Dim options As New Options_HistPointCloud
        Public Sub New()
            taskAlg.gOptions.setHistogramBins(9)
            labels = {"", "", "Plot of 2D histogram", "All non-zero entries in the 2D histogram"}
            desc = "Create a 2D histogram of the point cloud data - which 2D inputs is in options."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Select Case taskAlg.reductionName
                Case "X Reduction", "Y Reduction", "Z Reduction"
                    cv.Cv2.CalcHist({taskAlg.pointCloud}, taskAlg.channels, New cv.Mat(), histogram,
                                 taskAlg.channelCount, taskAlg.histBinList, taskAlg.ranges)

                    Static plot As New Plot_Histogram
                    plot.Run(histogram)
                    dst2 = plot.histogram
                    labels(2) = "2D plot of 1D histogram."
                Case "XY Reduction", "XZ Reduction", "YZ Reduction"
                    cv.Cv2.CalcHist({taskAlg.pointCloud}, taskAlg.channels, New cv.Mat(), histogram,
                                 taskAlg.channelCount, taskAlg.histBinList, taskAlg.ranges)

                    Static plot2D As New Plot_Histogram2D
                    plot2D.Run(histogram)
                    dst2 = plot2D.dst2
                    labels(2) = "2D plot of 2D histogram."
                    If dst2.Type <> cv.MatType.CV_8U Then dst2.ConvertTo(dst2, cv.MatType.CV_8U)
                Case "XYZ Reduction"
                    Static hcloud As New Hist3Dcloud_Basics
                    dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)

                    hcloud.Run(taskAlg.pointCloud)

                    histogram = hcloud.histogram
                    Dim histData(histogram.Total - 1) As Single
                    Marshal.Copy(histogram.Data, histData, 0, histData.Length)

                    If histData.Count > 255 And taskAlg.histogramBins > 3 Then
                        taskAlg.histogramBins -= 1
                    End If
                    If histData.Count < 128 And taskAlg.histogramBins < taskAlg.gOptions.HistBinBar.Maximum Then
                        taskAlg.histogramBins += 1
                    End If
                    If taskAlg.gridRects.Count < histData.Length And taskAlg.brickSize > 2 Then
                        taskAlg.brickSize -= 1
                        grid.Run(src)
                        dst2.SetTo(0)
                    End If
                    histData(0) = 0 ' count of zero pixels - distorts sharedResults.images...

                    Dim maxVal = histData.ToList.Max
                    For i = 0 To taskAlg.gridRects.Count - 1
                        Dim roi = taskAlg.gridRects(i)
                        If i >= histData.Length Then
                            dst2(roi).SetTo(0)
                        Else
                            dst2(roi).SetTo(255 * histData(i) / maxVal)
                        End If
                    Next
                    labels(2) = "2D plot of the resulting 3D histogram."
            End Select

            dst3 = PaletteFull(dst2)

            Dim histArray(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
        End Sub
    End Class
End Namespace