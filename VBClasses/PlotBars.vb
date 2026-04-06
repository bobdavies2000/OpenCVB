Imports cv = OpenCvSharp
Public Class PlotBars_Basics : Inherits TaskParent
    Public histogram As New cv.Mat
    Public histArray() As Single
    Public minRange As Single
    Public maxRange As Single
    Public ranges() As cv.Rangef
    Public backgroundColor As cv.Scalar = cv.Scalar.Red
    Public plotCenter As Single
    Public barWidth As Single
    Public addLabels As Boolean = True
    Public removeZeroEntry As Boolean = True
    Public createHistogram As Boolean = False
    Public shadeValues As Boolean = True
    Public histMask As New cv.Mat
    Public mm As mmData
    Public Sub New()
        desc = "Plot the default gray scale data with a stable Y values at the left of the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim min = minRange
        Dim max = maxRange
        If standaloneTest() Or createHistogram Then
            If src.Channels() <> 1 Then src = task.grayStable.Clone
            If minRange = 0 And maxRange = 0 Then
                Dim mm = GetMinMax(src)
                min = mm.minVal
                max = mm.maxVal
                If min = 0 And max = 0 Then
                    If src.Type = cv.MatType.CV_32F Then
                        max = task.MaxZmeters
                    Else
                        max = 255
                    End If
                End If
            End If
            If Single.IsNaN(min) Or Single.IsInfinity(min) Then min = Single.MinValue
            If Single.IsNaN(max) Or Single.IsInfinity(max) Then max = Single.MaxValue
            ranges = {New cv.Rangef(min, max)}
            cv.Cv2.CalcHist({src}, {0}, histMask, histogram, 1, {task.histogramBins}, ranges)
        Else
            histogram = src
        End If

        If removeZeroEntry Then histogram.Set(Of Single)(0, 0, 0) ' let's not plot the values at zero...i.e. Depth at 0, for instance, needs to be removed.
        ReDim histArray(histogram.Rows - 1)
        histogram.GetArray(Of Single)(histArray)

        dst2.SetTo(backgroundColor)
        barWidth = dst2.Width / histogram.Rows
        plotCenter = barWidth * histogram.Rows / 2 + barWidth / 2

        mm = GetMinMax(histogram)

        ' somewacky values for the stereolabs devices.
        If mm.minVal > -100000000 And mm.maxVal < 100000000 Then
            If Math.Abs(mm.maxVal - mm.minVal) > 0 And histogram.Rows > 0 Then
                Dim incr = 255 / histogram.Rows
                Dim color As cv.Scalar
                For i = 0 To histArray.Count - 1
                    If Single.IsNaN(histArray(i)) Then histArray(i) = 0
                    If histArray(i) > 0 Then
                        Dim h As Single = histArray(i) * dst2.Height / mm.maxVal
                        If shadeValues Then
                            Dim sIncr = (i Mod 256) * incr
                            color = New cv.Scalar(sIncr, sIncr, sIncr)
                            If histogram.Rows > 255 Then color = cv.Scalar.Black
                        Else
                            color = cv.Scalar.Black
                        End If
                        cv.Cv2.Rectangle(dst2, New cv.Rect(i * barWidth, dst2.Height - h,
                                                               Math.Max(1, barWidth), h), color, -1)
                    End If
                Next
                If addLabels Then Plot_Basics.AddPlotScale(dst2, mm.minVal, mm.maxVal)
            End If
            labels(2) = "Min/Max values " + Format(mm.minVal, fmt2) + "/" + Format(mm.maxVal, fmt2)
        End If
    End Sub
End Class





Public Class PlotBars_Histogram2D : Inherits TaskParent
    Public Sub New()
        labels = {"", "", "2D Histogram", ""}
        desc = "Plot a 2D histgram from the input Mat"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram = src.Clone
        If standalone Then
            Static colorFmt As New Color_Basics
            colorFmt.Run(src)
            src = colorFmt.dst2
            Dim bins = task.histogramBins
            cv.Cv2.CalcHist({src}, {0, 1}, New cv.Mat(), histogram, 2, {bins, bins}, task.rangesBGR)
        End If

        dst2 = histogram.Resize(dst2.Size(), 0, 0, cv.InterpolationFlags.Nearest)

        If standaloneTest() Then dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class





Public Class PlotBars_HistCoreRange : Inherits TaskParent
    Public redCore As New RedPrep_Core
    Public minRange As Single
    Public maxRange As Single
    Public plotHist As New PlotBars_Basics
    Dim plotHistNew As New PlotBars_Basics
    Public Sub New()
        OptionParent.findRadio("Y Reduction").Checked = True
        plotHistNew.createHistogram = True
        plotHist.createHistogram = True
        task.gOptions.HistBinBar.Value = task.gOptions.HistBinBar.Maximum
        desc = "Remove outliers from the histogram by finding the single range with no gaps."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then redCore.Run(src)

        plotHist.minRange = wcMinVal
        plotHist.maxRange = wcMaxVal
        plotHist.Run(redCore.reduced32f)
        Dim histArray = plotHist.histArray
        Dim incr = (wcMaxVal - wcMinVal) / task.gOptions.HistBinBar.Value
        dst2 = plotHist.dst2

        Dim histList = plotHist.histArray.ToList
        Dim index = histList.IndexOf(histList.Max)

        Dim maxCount As Integer
        Dim minIndex As Integer
        Dim maxIndex As Integer
        For minIndex = index To 0 Step -1
            If histList(minIndex) = 0 Then Exit For
            maxCount += histList(minIndex)
        Next

        For maxIndex = index + 1 To histList.Count - 1
            If histList(maxIndex) = 0 Then Exit For
            maxCount += histList(maxIndex)
        Next

        Dim minRange = wcMinVal + incr * minIndex
        Dim maxRange = wcMinVal + incr * maxIndex
        labels(2) = "Histogram with fixed range from " + Format(wcMinVal, fmt0) + " to " + Format(wcMaxVal, fmt0)
        labels(3) = "Histogram after trim to " + Format(minRange, fmt0) + " to " + Format(maxRange, fmt0)

        plotHistNew.minRange = minRange
        plotHistNew.maxRange = maxRange
        plotHistNew.Run(redCore.reduced32f)
        dst3 = plotHistNew.dst2
    End Sub
End Class





