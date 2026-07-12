Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class PlotBar_Basics : Inherits TaskParent
    Public histogram As New Mat
    Public histArray() As Single
    Public minRange As Single
    Public maxRange As Single
    Public ranges() As Rangef
    Public backgroundColor As Scalar = Scalar.Red
    Public plotCenter As Single
    Public barWidth As Single
    Public addLabels As Boolean = True
    Public removeZeroEntry As Boolean
    Public createHistogram As Boolean = False
    Public shadeValues As Boolean = True
    Public histMask As New Mat
    Public mm As mmData
    Public Sub New()
        desc = "Plot the default gray scale data with a stable Y values at the left of the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim min = minRange
        Dim max = maxRange
        If standaloneTest() Or createHistogram Then
            If src.Channels() <> 1 Then src = task.gray.Clone
            If minRange = 0 And maxRange = 0 Then
                Dim mm = GetMinMax(src)
                min = mm.minVal - 0.01
                max = mm.maxVal + 0.01
                If min = 0 And max = 0 Then
                    If src.Type = MatType.CV_32F Then
                        max = task.MaxZmeters
                    Else
                        max = 255
                    End If
                End If
            End If
            If Single.IsNaN(min) Or Single.IsInfinity(min) Then min = Single.MinValue
            If Single.IsNaN(max) Or Single.IsInfinity(max) Then max = Single.MaxValue
            ranges = {New Rangef(min, max)}
            CalcHist({src}, {0}, histMask, histogram, 1, {task.histogramBins}, ranges)
        Else
            histogram = src
        End If

        If removeZeroEntry Then histogram.Set(Of Single)(0, 0, 0) ' let's not plot the values at zero...i.e. Depth at 0, for instance, needs to be removed.
        Dim incr = 255 / histogram.Cols
        Dim maxIndex As Integer = histogram.Cols
        If histogram.Cols = 1 Then
            ReDim histArray(histogram.Rows - 1)
            barWidth = dst2.Width / histogram.Rows
            plotCenter = barWidth * histogram.Rows / 2 + barWidth / 2
            incr = 255 / histogram.Rows
            maxIndex = histogram.Rows
        Else
            ReDim histArray(histogram.Cols - 1)
            barWidth = dst2.Width / histogram.Cols
            plotCenter = barWidth * histogram.Cols / 2 + barWidth / 2
        End If
        histogram.GetArray(Of Single)(histArray)

        dst2.SetTo(backgroundColor)

        mm = GetMinMax(histogram)

        ' some wacky values for the stereolabs devices.
        If mm.minVal > -100000000 And mm.maxVal < 100000000 Then
            If Math.Abs(mm.maxVal - mm.minVal) > 0 And histogram.Cols > 0 Then
                Dim color As Scalar
                For i = 0 To histArray.Count - 1
                    If Single.IsNaN(histArray(i)) Then histArray(i) = 0
                    If histArray(i) > 0 Then
                        Dim h As Single = histArray(i) * dst2.Height / mm.maxVal
                        If shadeValues Then
                            Dim sIncr = (i Mod 256) * incr
                            color = New Scalar(sIncr, sIncr, sIncr)
                            If maxIndex > 255 Then color = Scalar.Black
                        Else
                            color = Scalar.Black
                        End If
                        Rectangle(dst2, New cv.Rect(i * barWidth, dst2.Height - h,
                                                               Math.Max(1, barWidth), h), color, -1)
                    End If
                Next
                If addLabels Then Utility_Basics.AddPlotScale(dst2, mm.minVal, mm.maxVal)
            End If
            labels(2) = "Min/Max values " + mm.minVal.ToString(fmt2) + "/" + mm.maxVal.ToString(fmt2)
        End If
    End Sub
End Class






Public Class PlotBar_HistCoreRange : Inherits TaskParent
    Public redCore As New RedPrep_Core
    Public minRange As Single
    Public maxRange As Single
    Public plotHist As New PlotBar_Basics
    Dim plotHistNew As New PlotBar_Basics
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
        labels(2) = "Histogram with fixed range from " + wcMinVal.ToString(fmt0) + " to " + wcMaxVal.ToString(fmt0)
        labels(3) = "Histogram after trim to " + minRange.ToString(fmt0) + " to " + maxRange.ToString(fmt0)

        plotHistNew.minRange = minRange
        plotHistNew.maxRange = maxRange
        plotHistNew.Run(redCore.reduced32f)
        dst3 = plotHistNew.dst2
    End Sub
End Class





Public Class PlotBar_Histogram2D : Inherits TaskParent
    Public ranges() As Rangef = task.rangesBGR
    Public histogram As New Mat
    Public Sub New()
        labels = {"", "", "2D Histogram", ""}
        desc = "Plot a 2D histgram from the input Mat"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Static colorFmt As New Color_Basics
            colorFmt.Run(src)
            src = colorFmt.dst2
        End If
        Dim bins = task.histogramBins
        CalcHist({src}, {0, 1}, New Mat(), histogram, 2, {bins, bins}, ranges)

        Resize(histogram, dst2, task.workRes, 0, 0, InterpolationFlags.Nearest)

        If standaloneTest() Then
            Dim thresh As New Mat()
            Threshold(dst2, thresh, 0, 255, ThresholdTypes.Binary)
            dst3 = thresh
        End If
    End Sub
End Class






Public Class PlotBar_Depth : Inherits TaskParent
    Dim hist As New PlotMouse_Basics
    Public Sub New()
        desc = "Plot the depth data (duplicate of PlotMouse_Basics)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hist.Run(src)
        dst2 = hist.dst2
        labels(2) = hist.labels(2)
    End Sub
End Class
