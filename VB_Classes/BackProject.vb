Imports cv = OpenCvSharp
' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class BackProject_Basics : Inherits VBparent
    Public hist As New Histogram_Basics
    Public Sub New()
        labels(2) = "Move mouse to backproject each histogram column"
        task.desc = "Explore Backprojection of each element of a grayscale histogram."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        hist.Run(src)
        dst2 = hist.dst2

        Dim barWidth = dst2.Width / task.histogramBins
        Dim barRange = 255 / task.histogramBins
        Dim histIndex = Math.Floor(task.mousePoint.X / barWidth)

        Dim minRange = If(histIndex = task.histogramBins, 255 - barRange, histIndex * barRange)
        Dim maxRange = If(histIndex = task.histogramBins, 255, (histIndex + 1) * barRange)
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}
        Dim mask As New cv.Mat
        cv.Cv2.CalcBackProject({src}, {0}, hist.histogram, mask, ranges)
        dst3 = src
        If maxRange = 255 Then dst3.SetTo(cv.Scalar.Black, mask) Else dst3.SetTo(cv.Scalar.White, mask)
        Dim count = hist.histogram.Get(Of Single)(histIndex, 0)
        labels(3) = "Backprojecting " + CStr(CInt(minRange)) + " to " + CStr(CInt(maxRange)) + " with " +
                 Format(count, "#0") + " (" + Format(count / dst2.Total, "0.0%") + ") samples"
        dst2.Rectangle(New cv.Rect(CInt(histIndex * barWidth), 0, barWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class









Public Class BackProject_Masks : Inherits VBparent
    Dim lines As New Line_Basics
    Dim hist As New Histogram_Basics
    Public Sub New()
        task.desc = "Create all the backprojection masks from a histogram"
    End Sub
    Public Function maskLineDetect(gray As cv.Mat, histogram As cv.Mat, histIndex As Integer) As List(Of cv.Vec6f)
        Dim barWidth = dst2.Width / histogram.Rows
        Dim barRange = 255 / histogram.Rows

        Dim minRange = If(histIndex = histogram.Rows - 1, 255 - barRange, histIndex * barRange)
        Dim maxRange = If(histIndex = histogram.Rows - 1, 255, (histIndex + 1) * barRange)
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

        Dim mask As New cv.Mat
        cv.Cv2.CalcBackProject({gray}, {0}, histogram, mask, ranges)
        lines.Run(mask)
        Dim masklines As New List(Of cv.Vec6f)
        For i = 0 To lines.sortlines.Count - 1
            masklines.Add(lines.sortlines.ElementAt(i).Value)
        Next
        Return masklines
    End Function
    Public Sub Run(src As cv.Mat) ' Rank = 1
        hist.Run(src)
        dst2 = hist.dst2

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim allLines As New List(Of cv.Vec6f)
        For i = 0 To task.histogramBins - 1
            Dim masklines = maskLineDetect(gray, hist.histogram, i)
            For j = 0 To masklines.Count - 1
                allLines.Add(masklines(j))
            Next
        Next
        dst3 = src
        For i = 0 To allLines.Count - 1
            Dim v = allLines(i)
            Dim pt1 = New cv.Point(v.Item0, v.Item1)
            Dim pt2 = New cv.Point(v.Item2, v.Item3)
            dst3.Line(pt1, pt2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
    End Sub
End Class







Public Class BackProject_MasksLines : Inherits VBparent
    Dim lines As New BackProject_Masks
    Dim hist As New Histogram_Basics
    Public Sub New()
        task.desc = "Inspect the lines from individual backprojection masks from a histogram"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        hist.Run(src)
        dst2 = hist.dst2

        Dim barWidth = dst2.Width / task.histogramBins
        Dim histIndex = Math.Floor(task.mousePoint.X / barWidth)

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim allLines = lines.maskLineDetect(gray, hist.histogram, histIndex)
        dst3 = src
        For i = 0 To allLines.Count - 1
            Dim v = allLines(i)
            Dim pt1 = New cv.Point(v.Item0, v.Item1)
            Dim pt2 = New cv.Point(v.Item2, v.Item3)
            dst3.Line(pt1, pt2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
        dst2.Rectangle(New cv.Rect(CInt(histIndex * barWidth), 0, barWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class











Public Class BackProject_Surfaces : Inherits VBparent
    Public pcValid As New Motion_MinMaxPointCloud
    Dim hist As New Histogram_Basics
    Dim mats As New Mat_2to1
    Public Sub New()
        labels(2) = "Top=differences in X, Bot=differences in Y"
        task.desc = "Find solid surfaces using the pointcloud X and Y differences"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        pcValid.Run(src)
        Dim mask = pcValid.dst2.Threshold(0, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)

        Dim split = pcValid.dst3.Split()
        Dim xDiff = New cv.Mat(dst3.Size, cv.MatType.CV_32FC1, 0)
        Dim yDiff = New cv.Mat(dst3.Size, cv.MatType.CV_32FC1, 0)

        Dim r1 = New cv.Rect(0, 0, dst2.Width - 1, dst2.Height - 1)
        Dim r2 = New cv.Rect(1, 1, dst2.Width - 1, dst2.Height - 1)

        cv.Cv2.Subtract(split(0)(r1), split(0)(r2), xDiff(r1))
        cv.Cv2.Subtract(split(1)(r2), split(1)(r1), yDiff(r1))

        xDiff.SetTo(0, mask)
        yDiff.SetTo(0, mask)

        Dim xMat = xDiff.ConvertScaleAbs(255)

        hist.Run(xMat)
        Dim ranges() = New cv.Rangef() {New cv.Rangef(1, 2)}
        cv.Cv2.CalcBackProject({xMat}, {0}, hist.histogram, mats.mat(0), ranges)

        Dim yMat = yDiff.ConvertScaleAbs(255)
        hist.Run(yMat)
        cv.Cv2.CalcBackProject({yMat}, {0}, hist.histogram, mats.mat(1), ranges)

        mats.Run(src)
        dst2 = mats.dst2

        cv.Cv2.BitwiseOr(mats.mat(0), mats.mat(1), dst3)
        labels(3) = "Likely smooth surfaces, framecount = " + CStr(task.frameCount)
    End Sub
End Class










' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class BackProject_2D : Inherits VBparent
    Dim hist As New Histogram_2D_HueSaturation
    Public Sub New()
        task.desc = "Backproject from a hue and saturation histogram."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static hueBinSlider = findSlider("Hue bins")
        Static hueBins = hueBinSlider.Value
        Static satBinSlider = findSlider("Saturation bins")
        Static satBins = satBinSlider.Value

        hist.Run(src)
        dst2 = hist.dst2

        Dim unitsPerHueBin = 180 / hueBins
        Dim unitsPerSatBin = 255 / satBins
        Dim huebarWidth = dst2.Width / hueBins
        Dim satBarHeight = dst2.Height / satBins
        Dim histX = Math.Floor(task.mousePoint.X / huebarWidth)
        Dim histY = Math.Floor(satBins - task.mousePoint.Y / satBarHeight)

        Dim minHue As Integer, maxHue As Integer, minSat As Integer, maxSat As Integer
        minHue = If(histX = hueBins - 1, 180 - unitsPerHueBin, histX * unitsPerHueBin)
        maxHue = If(histX = hueBins - 1, 180, (histX + 1) * unitsPerHueBin)
        minSat = If(histY = satBins - 1, 255 - unitsPerSatBin, histY * unitsPerSatBin)
        maxSat = If(histY = satBins - 1, 255, (histY + 1) * unitsPerSatBin)

        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim maskHue As New cv.Mat, maskSat As New cv.Mat

        Dim ranges() = New cv.Rangef() {New cv.Rangef(minHue, maxHue), New cv.Rangef(0, 255)}
        cv.Cv2.CalcBackProject({hsv}, {0, 1}, hist.histogram, maskHue, ranges)

        ranges = New cv.Rangef() {New cv.Rangef(0, 180), New cv.Rangef(minSat, maxSat)}
        cv.Cv2.CalcBackProject({hsv}, {0, 1}, hist.histogram, maskSat, ranges)

        dst3.SetTo(0)
        dst3.SetTo(cv.Scalar.Yellow, maskHue)
        dst3.SetTo(cv.Scalar.Blue, maskSat)
        labels(2) = "Hue(X) min/max " + Format(minHue, "0") + "/" + Format(maxHue, "0") + " Sat(Y) min/max " + Format(minSat, "0") + "/" + Format(maxSat, "0")
        labels(3) = "Hue pixels(yellow)=" + CStr(maskHue.CountNonZero()) + " Sat pixels(blue)=" + CStr(maskSat.CountNonZero())
        dst2.Rectangle(New cv.Rect(histX * huebarWidth, 0, huebarWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth, task.lineType)
        dst2.Rectangle(New cv.Rect(0, (satBins - 1 - histY) * satBarHeight, dst2.Width, satBarHeight), cv.Scalar.Yellow, task.lineWidth, task.lineType)
    End Sub
End Class







Public Class BackProject_2DHSV : Inherits VBparent
    Dim hueSat As New PhotoShop_Hue
    Dim hist2d As New BackProject_2D
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Click to enlarge: Hue, sat, selection, histogram"
        task.desc = "Compare the hue and brightness images and the results of the histogram_backprojection2d"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        hueSat.Run(src)
        mats.mat(0) = hueSat.dst2
        mats.mat(1) = hueSat.dst3

        hist2d.Run(src)
        mats.mat(2) = hist2d.dst3
        mats.mat(3) = hist2d.dst2

        mats.Run(src)
        dst2 = mats.dst2
        dst3 = mats.dst3

        labels(3) = hist2d.labels(2)
    End Sub
End Class








Public Class BackProject_Full : Inherits VBparent
    Public hist As New Histogram_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        labels(2) = "Move mouse to backproject each histogram column"
        task.desc = "Backproject the entire histogram."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        hist.Run(src)
        dst2 = hist.dst2

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim barWidth = dst2.Width / hist.histogram.Rows
        Dim barRange = 255 / hist.histogram.Rows

        For i = 0 To hist.histogram.Rows - 1
            Dim minRange = If(i = hist.histogram.Rows - 1, 255 - barRange, i * barRange)
            Dim maxRange = If(i = hist.histogram.Rows - 1, 255, (i + 1) * barRange)
            Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

            Dim mask As New cv.Mat
            cv.Cv2.CalcBackProject({gray}, {0}, hist.histogram, mask, ranges)
            dst3.SetTo(i, mask)
        Next
        dst3 *= 255 / hist.histogram.Rows
    End Sub
End Class







Public Class BackProject_Reduction : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Dim hist As New BackProject_Basics
    Public Sub New()
        findRadio("Use bitwise reduction").Checked = True
        labels(3) = "Backprojection of highlighted histogram bin"
        task.desc = "Use the histogram of a reduced RGB image to isolate featureless portions of an image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static reductionSlider = findSlider("Reduction factor")

        reduction.Run(src)

        hist.Run(reduction.dst2)
        dst2 = hist.dst2.Clone
        dst3 = reduction.dst2
        labels(2) = "Reduction = " + CStr(reductionSlider.value) + " and bins = " + CStr(task.histogramBins)
    End Sub
End Class







Public Class BackProject_ReductionLines : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Dim lines As New Line_Basics
    Public Sub New()
        findRadio("Use bitwise reduction").Checked = True

        labels(3) = "Backprojection of highlighted histogram bin"
        task.desc = "Use the histogram of a reduced RGB image to isolate featureless portions of an image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static reductionSlider = findSlider("Reduction factor")

        reduction.Run(src)
        dst2 = reduction.dst2

        lines.Run(dst2)

        dst3 = src
        For i = 0 To lines.sortlines.Count - 1
            Dim v = lines.sortlines.ElementAt(i).Value
            Dim pt1 = New cv.Point(v.Item0, v.Item1)
            Dim pt2 = New cv.Point(v.Item2, v.Item3)
            dst3.Line(pt1, pt2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
        labels(3) = CStr(lines.sortlines.Count) + " lines were found"

        labels(2) = "Reduction = " + CStr(reductionSlider.value) + " and bins = " + CStr(task.histogramBins)
    End Sub
End Class






Public Class BackProject_FullLines : Inherits VBparent
    Dim hist As New BackProject_Full
    Dim lines As New Line_Basics
    Public Sub New()
        task.desc = "Find lines in the back projection"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        hist.Run(src)
        dst2 = hist.dst2
        lines.Run(hist.dst3)
        dst3 = src
        For i = 0 To lines.sortlines.Count - 1
            Dim v = lines.sortlines.ElementAt(i).Value
            Dim pt1 = New cv.Point(v.Item0, v.Item1)
            Dim pt2 = New cv.Point(v.Item2, v.Item3)
            dst3.Line(pt1, pt2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
        labels(3) = CStr(lines.sortlines.Count) + " lines were found"
    End Sub
End Class