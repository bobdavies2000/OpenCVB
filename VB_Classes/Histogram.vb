Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Histogram_Basics
    Inherits VBparent
    Public histogram As New cv.Mat
    Public kalman As Kalman_Basics
    Public plotHist As Plot_Histogram
    Dim splitColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
    Public Sub New()
        initParent()
        plotHist = New Plot_Histogram()
        plotHist.minRange = 0

        kalman = New Kalman_Basics()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram Bins", 1, 1000, 50)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Remove the zero histogram value"
            check.Box(0).Checked = False
        End If

        label2 = "Histogram - x=bins/y=count"
        task.desc = "Create a histogram of the grayscale image and smooth the bar chart with a kalman filter."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static splitIndex = -1
        Static colorName As String
        If standalone Or task.intermediateReview = caller Then
            Dim split() = src.Split()
            If task.frameCount Mod 100 = 0 Then
                splitIndex += 1
                If splitIndex > 2 Then splitIndex = 0
            End If
            src = split(splitIndex)
            colorName = Choose(splitIndex + 1, "Blue", "Green", "Red")
        End If

        Dim input = src
        If input.Channels = 3 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static histBinSlider = findSlider("Histogram Bins")
        plotHist.bins = histBinSlider.Value
        Dim histSize() = {plotHist.bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}

        Dim dimensions() = New Integer() {plotHist.bins}
        cv.Cv2.CalcHist(New cv.Mat() {input}, New Integer() {0}, New cv.Mat, histogram, 1, dimensions, ranges)

        Static zeroCheck = findCheckBox("Remove the zero histogram value")
        If zeroCheck.checked Then histogram.Set(Of Single)(0, 0, 0)

        label2 = "Plot Histogram bins = " + CStr(plotHist.bins)

        Static kalmanCheck = findCheckBox("Turn Kalman filtering on")
        If kalmanCheck.checked Then
            ReDim kalman.kInput(plotHist.bins - 1)
            For i = 0 To plotHist.bins - 1
                kalman.kInput(i) = histogram.Get(Of Single)(i, 0)
            Next
            kalman.Run()
            For i = 0 To plotHist.bins - 1
                histogram.Set(Of Single)(i, 0, kalman.kOutput(i))
            Next
        End If

        plotHist.hist = histogram
        If standalone Or task.intermediateReview = caller Then plotHist.backColor = splitColors(splitIndex)
        plotHist.src = input
        plotHist.Run()
        dst1 = plotHist.dst1
        label1 = colorName + " input to histogram"
    End Sub
End Class







' https://github.com/opencv/opencv/blob/master/samples/python/hist.py
Public Class Histogram_Graph
    Inherits VBparent
    Public histRaw(3 - 1) As cv.Mat
    Public histNormalized(3 - 1) As cv.Mat
    Public bins = 50
    Public minRange As Single = 0
    Public maxRange As Single = 255
    Public backColor = cv.Scalar.Gray
    Public plotRequested As Boolean
    Public plotColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram Bins", 2, 256, 50)
            sliders.setupTrackBar(1, "Histogram line thickness", 1, 20, 3)
        End If
        task.desc = "Plot histograms for up to 3 channels."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static histBinSlider = findSlider("Histogram Bins")
        bins = histBinSlider.Value

        Static thicknessSlider = findSlider("Histogram line thickness")
        Dim thickness = thicknessSlider.Value
        Dim dimensions() = New Integer() {bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

        Dim lineWidth = dst1.Cols / bins

        dst1.SetTo(backColor)
        Dim maxVal As Double
        For i = 0 To src.Channels - 1
            Dim hist As New cv.Mat
            cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {i}, New cv.Mat(), hist, 1, dimensions, ranges)
            histRaw(i) = hist.Clone()
            histRaw(i).MinMaxLoc(0, maxVal)
            histNormalized(i) = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)
            If standalone Or plotRequested Then
                Dim points = New List(Of cv.Point)
                Dim listOfPoints = New List(Of List(Of cv.Point))
                For j = 0 To bins - 1
                    points.Add(New cv.Point(CInt(j * lineWidth), dst1.Rows - dst1.Rows * histRaw(i).Get(Of Single)(j, 0) / maxVal))
                Next
                listOfPoints.Add(points)
                dst1.Polylines(listOfPoints, False, plotColors(i), thickness, cv.LineTypes.AntiAlias)
            End If
        Next

        If standalone Or plotRequested Then
            maxVal = Math.Round(maxVal / 1000, 0) * 1000 + 1000 ' smooth things out a little for the scale below
            AddPlotScale(dst1, 0, maxVal, task.fontSize * 2)
            label1 = "Histogram for src image (default color) - " + CStr(bins) + " bins"
        End If
    End Sub
End Class





Module histogram_Functions
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Histogram_3D_RGB(rgbPtr As IntPtr, rows As Integer, cols As Integer, bins As Integer) As IntPtr
    End Function

    Public Sub histogram2DPlot(histogram As cv.Mat, dst1 As cv.Mat, xBins As Integer, yBins As Integer)
        Dim maxVal As Double
        histogram.MinMaxLoc(0, maxVal)
        Dim xScale = dst1.Cols / xBins
        Dim yScale = dst1.Rows / yBins
        For y = 0 To yBins - 1
            For x = 0 To xBins - 1
                Dim binVal = histogram.Get(Of Single)(y, x)
                Dim intensity = Math.Round(binVal * 255 / maxVal)
                Dim pt1 = New cv.Point(x * xScale, y * yScale)
                Dim pt2 = New cv.Point((x + 1) * xScale - 1, (y + 1) * yScale - 1)
                If pt1.X >= dst1.Cols Then pt1.X = dst1.Cols - 1
                If pt1.Y >= dst1.Rows Then pt1.Y = dst1.Rows - 1
                If pt2.X >= dst1.Cols Then pt2.X = dst1.Cols - 1
                If pt2.Y >= dst1.Rows Then pt2.Y = dst1.Rows - 1
                If pt1.X <> pt2.X And pt1.Y <> pt2.Y Then
                    Dim value = cv.Scalar.All(255 - intensity)
                    'value = New cv.Scalar(pt1.X * 255 / dst1.Cols, pt1.Y * 255 / dst1.Rows, 255 - intensity)
                    value = New cv.Scalar(intensity, intensity, intensity)
                    dst1.Rectangle(pt1, pt2, value, -1, cv.LineTypes.AntiAlias)
                End If
            Next
        Next
    End Sub

    Public Sub Show_HSV_Hist(img As cv.Mat, hist As cv.Mat)
        Dim binCount = hist.Height
        Dim binWidth = img.Width / hist.Height
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)
        img.SetTo(0)
        If maxVal = 0 Then Exit Sub
        For i = 0 To binCount - 2
            Dim h = img.Height * (hist.Get(Of Single)(i, 0)) / maxVal
            If h = 0 Then h = 5 ' show the color range in the plot
            cv.Cv2.Rectangle(img, New cv.Rect(i * binWidth + 1, img.Height - h, binWidth - 2, h), New cv.Scalar(CInt(180.0 * i / binCount), 255, 255), -1)
        Next
    End Sub

    Public Sub histogramBars(hist As cv.Mat, dst1 As cv.Mat, savedMaxVal As Single)
        Dim barWidth = Int(dst1.Width / hist.Rows)
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)

        maxVal = Math.Round(maxVal / 1000, 0) * 1000 + 1000

        If maxVal < 0 Then maxVal = savedMaxVal
        If Math.Abs((maxVal - savedMaxVal)) / maxVal < 0.2 Then maxVal = savedMaxVal Else savedMaxVal = Math.Max(maxVal, savedMaxVal)

        dst1.SetTo(cv.Scalar.Red)
        If maxVal > 0 And hist.Rows > 0 Then
            Dim incr = CInt(255 / hist.Rows)
            For i = 0 To hist.Rows - 1
                Dim offset = hist.Get(Of Single)(i)
                If Single.IsNaN(offset) Then offset = 0
                Dim h = CInt(offset * dst1.Height / maxVal)
                Dim color As cv.Scalar = cv.Scalar.Black
                If hist.Rows <= 255 Then color = cv.Scalar.All((i Mod 255) * incr)
                cv.Cv2.Rectangle(dst1, New cv.Rect(i * barWidth, dst1.Height - h, barWidth, h), color, -1)
            Next
        End If
    End Sub
End Module





Public Class Histogram_NormalizeGray
    Inherits VBparent
    Public histogram As Histogram_Basics
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Min Gray", 0, 255, 0)
            sliders.setupTrackBar(1, "Max Gray", 0, 255, 255)
        End If
        histogram = New Histogram_Basics

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Normalize Before Histogram"
            check.Box(0).Checked = True
        End If

        task.desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        histogram.src = src
        If check.Box(0).Checked Then
            cv.Cv2.Normalize(histogram.src, histogram.src, sliders.trackbar(0).Value, sliders.trackbar(1).Value, cv.NormTypes.MinMax) ' only minMax is working...
        End If
        histogram.Run()
        dst1 = histogram.dst1
    End Sub
End Class






' https://docs.opencv.org/2.4/modules/imgproc/doc/histograms.html
Public Class Histogram_2D_HueSaturation
    Inherits VBparent
    Public histogram As New cv.Mat
    Public hsv As cv.Mat

    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Hue bins", 1, 180, 30) ' quantize hue to 30 levels
            sliders.setupTrackBar(1, "Saturation bins", 1, 256, 32) ' quantize sat to 32 levels
        End If
        task.desc = "Create a histogram for hue and saturation."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        hsv = src.CvtColor(cv.ColorConversionCodes.RGB2HSV)
        Dim hbins = sliders.trackbar(0).Value
        Dim sbins = sliders.trackbar(1).Value
        Dim histSize() = {sbins, hbins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, sliders.trackbar(0).Maximum - 1), New cv.Rangef(0, sliders.trackbar(1).Maximum - 1)} ' hue ranges from 0-179

        cv.Cv2.CalcHist(New cv.Mat() {hsv}, New Integer() {0, 1}, New cv.Mat(), histogram, 2, histSize, ranges)

        histogram2DPlot(histogram, dst1, hbins, sbins)
    End Sub
End Class





Public Class Histogram_Depth
    Inherits VBparent
    Public inrange As Depth_InRange
    Public plotHist As Plot_Histogram
    Public Sub New()
        initParent()
        plotHist = New Plot_Histogram()

        inrange = New Depth_InRange
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram Depth Bins", 2, src.Cols, 50)
        End If

        task.desc = "Show depth data as a histogram."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        inrange.src = src
        If inrange.src.Type = cv.MatType.CV_32F Then
            inrange.Run()
        Else
            inrange.depth32f = task.depth32f
        End If

        plotHist.minRange = task.inrange.minval
        plotHist.maxRange = task.inrange.maxval
        Static binSlider = findSlider("Histogram Depth Bins")
        plotHist.bins = binSlider.value

        Dim histSize() = {plotHist.bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}
        cv.Cv2.CalcHist(New cv.Mat() {inrange.depth32f}, New Integer() {0}, New cv.Mat, plotHist.hist, 1, histSize, ranges)

        If standalone Or task.intermediateReview = caller Then
            plotHist.Run()
            dst1 = plotHist.dst1
        End If
        label1 = "Histogram Depth: " + Format(plotHist.minRange / 1000, "0.0") + "m to " + Format(plotHist.maxRange / 1000, "0.0") + " m"
    End Sub
End Class






Public Class Histogram_2D_XZ_YZ
    Inherits VBparent
    Dim xyz As Mat_ImageXYZ_MT
    Dim minSlider As Windows.Forms.TrackBar
    Dim maxSlider As Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        xyz = New Mat_ImageXYZ_MT

        task.maxRangeSlider.Value = 1500 ' up to x meters away

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram X bins", 1, src.Cols, 30)
            sliders.setupTrackBar(1, "Histogram Y bins", 1, src.Rows, 30)
            sliders.setupTrackBar(2, "Histogram Z bins", 1, 200, 100)
        End If
        task.desc = "Create a 2D histogram for depth in XZ and YZ."
        label2 = "Left is XZ (Top View) and Right is YZ (Side View)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim xbins = sliders.trackbar(0).Value
        Dim ybins = sliders.trackbar(1).Value
        Dim zbins = sliders.trackbar(2).Value
        Dim minRange = task.minRangeSlider.Value
        Dim maxRange = task.maxRangeSlider.Value

        Dim histogram As New cv.Mat

        Dim rangesX() = New cv.Rangef() {New cv.Rangef(0, src.Width - 1), New cv.Rangef(minRange, maxRange)}
        Dim rangesY() = New cv.Rangef() {New cv.Rangef(0, src.Width - 1), New cv.Rangef(minRange, maxRange)}

        xyz.Run()
        Dim sizesX() = {xbins, zbins}
        cv.Cv2.CalcHist(New cv.Mat() {xyz.xyDepth}, New Integer() {0, 2}, New cv.Mat(), histogram, 2, sizesX, rangesX)
        histogram2DPlot(histogram, dst1, zbins, xbins)

        Dim sizesY() = {ybins, zbins}
        cv.Cv2.CalcHist(New cv.Mat() {xyz.xyDepth}, New Integer() {1, 2}, New cv.Mat(), histogram, 2, sizesY, rangesY)
        histogram2DPlot(histogram, dst2, zbins, ybins)
    End Sub
End Class








' https://docs.opencv.org/master/d1/db7/tutorial_py_histogram_begins.html
Public Class Histogram_EqualizeColor
    Inherits VBparent
    Public kalmanEq As Histogram_Basics
    Public kalman As Histogram_Basics
    Dim mats As Mat_2to1
    Public displayHist As Boolean = False
    Public channel = 2
    Public Sub New()
        initParent()
        kalmanEq = New Histogram_Basics
        kalman = New Histogram_Basics

        Static binSlider = findSlider("Histogram Bins")
        binSlider.Value = 40

        mats = New Mat_2to1()

        task.desc = "Create an equalized histogram of the color image. Image is noticeably enhanced."
        label1 = "Image Enhanced with Equalized Histogram"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim rgb(2) As cv.Mat
        Dim rgbEq(2) As cv.Mat
        rgbEq = src.Split()

        For i = 0 To rgb.Count - 1
            cv.Cv2.EqualizeHist(rgbEq(i), rgbEq(i))
        Next

        If standalone Or displayHist Then
            cv.Cv2.Split(src, rgb) ' equalizehist alters the input...
            kalman.src = rgb(channel).Clone()
            kalman.plotHist.backColor = cv.Scalar.Red
            kalman.Run()
            mats.mat(0) = kalman.dst1.Clone()

            kalmanEq.src = rgbEq(channel).Clone()
            kalmanEq.Run()
            mats.mat(1) = kalmanEq.dst1.Clone()

            mats.Run()
            dst2 = mats.dst1
            label2 = "Before (top) and After Red Histogram"

            cv.Cv2.Merge(rgbEq, dst1)
        End If
    End Sub
End Class






'https://docs.opencv.org/master/d1/db7/tutorial_py_histogram_begins.html
Public Class Histogram_EqualizeGray
    Inherits VBparent
    Public histogramEq As Histogram_Basics
    Public histogram As Histogram_Basics
    Public Sub New()
        initParent()
        histogramEq = New Histogram_Basics

        histogram = New Histogram_Basics

        label1 = "Before EqualizeHist"
        label2 = "After EqualizeHist"
        task.desc = "Create an equalized histogram of the grayscale image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static binSlider = findSlider("Histogram Bins")
        Static eqCheckBox = findCheckBox("Turn Kalman filtering on")

        binSlider.Value = histogramEq.sliders.trackbar(0).Value
        eqCheckBox.Checked = histogramEq.kalman.check.Box(0).Checked

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        histogram.src = src.Clone
        histogram.Run()
        dst1 = histogram.dst1.Clone
        cv.Cv2.EqualizeHist(histogram.src, histogramEq.src)
        histogramEq.Run()
        dst2 = histogramEq.dst1
    End Sub
End Class





' https://docs.opencv.org/master/d1/db7/tutorial_py_histogram_begins.html
Public Class Histogram_Equalize255
    Inherits VBparent
    Dim eqHist As Histogram_EqualizeColor
    Public Sub New()
        initParent()

        eqHist = New Histogram_EqualizeColor()
        Static binSlider = findSlider("Histogram Bins")
        binSlider.Value = 255
        eqHist.displayHist = True

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Equalize the Blue channel"
            radio.check(1).Text = "Equalize the Green channel"
            radio.check(2).Text = "Equalize the Red channel"
            radio.check(2).Checked = True
        End If
        label1 = "Resulting equalized image"
        label2 = "Upper plot is before equalization.  Bottom is after."
        task.desc = "Reproduce the results of the hist.py example with existing algorithms"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        For i = 0 To 3 - 1
            If radio.check(i).Checked Then eqHist.channel = i
        Next
        eqHist.src = src
        eqHist.Run()
        dst1 = eqHist.dst1.Clone
        dst2 = eqHist.dst2.Clone
    End Sub
End Class





Public Class Histogram_Simple
    Inherits VBparent
    Public plotHist As Plot_Histogram
    Public Sub New()
        initParent()
        plotHist = New Plot_Histogram()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram Bins", 2, src.Cols, 50)
        End If

        task.desc = "Build a simple and reusable histogram for grayscale images."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        plotHist.bins = sliders.trackbar(0).Value

        Dim histSize() = {sliders.trackbar(0).Value}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}
        cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {0}, New cv.Mat, plotHist.hist, 1, histSize, ranges)

        plotHist.Run()
        dst1 = plotHist.dst1
    End Sub
End Class












Public Class Histogram_ColorsAndGray
    Inherits VBparent
    Dim histogram As Histogram_Basics
    Dim mats As Mat_4to1
    Public Sub New()
        initParent()
        mats = New Mat_4to1()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Min Gray", 0, 255, 0)
            sliders.setupTrackBar(1, "Max Gray", 0, 255, 255)
        End If
        histogram = New Histogram_Basics
        histogram.kalman.check.Box(0).Checked = False
        histogram.kalman.check.Box(0).Enabled = False
        histogram.sliders.trackbar(0).Value = 40

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Normalize Before Histogram"
            check.Box(0).Checked = True
        End If

        label2 = "Click any quadrant at left to view it below"
        task.desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim split = src.Split()
        ReDim Preserve split(4 - 1)
        split(4 - 1) = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) ' add a 4th image - the grayscale image to the R G and B images.
        histogram.src = New cv.Mat
        For i = 0 To split.Length - 1
            If check.Box(0).Checked Then
                cv.Cv2.Normalize(split(i), histogram.src, sliders.trackbar(0).Value, sliders.trackbar(1).Value, cv.NormTypes.MinMax) ' only minMax is working...
            Else
                histogram.src = split(i).Clone()
            End If
            histogram.plotHist.backColor = Choose(i + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.PowderBlue)
            histogram.Run()
            mats.mat(i) = histogram.dst1.Clone()
        Next

        mats.Run()
        dst1 = mats.dst1
        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setMyActiveMat()
        dst2 = mats.mat(quadrantIndex)
    End Sub
End Class





Public Class Histogram_BackProjectionPeak
    Inherits VBparent
    Dim hist As Histogram_Basics
    Public Sub New()
        initParent()

        hist = New Histogram_Basics
        hist.kalman.check.Box(0).Checked = False

        task.desc = "Create a histogram and back project into the image the grayscale color with the highest occurance."
        label2 = "Grayscale Histogram"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        hist.src = input
        hist.Run()
        dst2 = hist.dst1

        Dim minVal As Single, maxVal As Single
        Dim minIdx As cv.Point, maxIdx As cv.Point
        hist.histogram.MinMaxLoc(minVal, maxVal, minIdx, maxIdx)
        Dim barWidth = dst1.Width / hist.sliders.trackbar(0).Value
        Dim barRange = 255 / hist.sliders.trackbar(0).Value
        Dim histindex = maxIdx.Y
        Dim pixelMin = CInt((histindex) * barRange)
        Dim pixelMax = CInt((histindex + 1) * barRange)

        Dim mask = input.InRange(pixelMin, pixelMax).Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst1.SetTo(0)
        src.CopyTo(dst1, mask)
        dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)

        label1 = "BackProjection of most frequent gray pixel"
        dst2.Rectangle(New cv.Rect(barWidth * histindex, 0, barWidth, dst1.Height), cv.Scalar.Yellow, 1)
    End Sub
End Class









' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class Histogram_BackProjection2D
    Inherits VBparent
    Dim hist As Histogram_2D_HueSaturation
    Public Sub New()
        initParent()

        hist = New Histogram_2D_HueSaturation()

        task.desc = "Backproject from a hue and saturation histogram."
        label1 = "X-axis is Hue, Y-axis is Sat.  Draw rectangle to isolate ranges"
        label2 = "Backprojection of detected hue and saturation."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        hist.src = src
        hist.Run()
        dst1 = hist.dst1
        Static hueBins = hist.sliders.trackbar(0).Value
        Static satBins = hist.sliders.trackbar(1).Value
        If hueBins <> hist.sliders.trackbar(0).Value Or satBins <> hist.sliders.trackbar(1).Value Then
            task.drawRectClear = True
            hueBins = hist.sliders.trackbar(0).Value
            satBins = hist.sliders.trackbar(1).Value
        End If

        Dim unitsPerHueBin = 180 / hueBins
        Dim unitsPerSatBin = 255 / satBins
        Dim minHue = 0, maxHue = 180, minSat = 0, maxSat = 255
        If task.drawRect.Width <> 0 And task.drawRect.Height <> 0 Then
            Dim intBin = Math.Floor(hueBins * task.drawRect.X / dst1.Width)
            minHue = intBin * unitsPerHueBin
            intBin = Math.Ceiling(hueBins * (task.drawRect.X + task.drawRect.Width) / dst1.Width)
            maxHue = intBin * unitsPerHueBin

            intBin = Math.Floor(satBins * task.drawRect.Y / dst1.Height)
            minSat = intBin * unitsPerSatBin
            intBin = Math.Ceiling(satBins * (task.drawRect.Y + task.drawRect.Height) / dst1.Height)
            maxSat = intBin * unitsPerSatBin

            If minHue = maxHue Then maxHue = minHue + 1
            If minSat = maxSat Then maxSat = minSat + 1
            label2 = "Selection: min/max Hue " + Format(minHue, "0") + "/" + Format(maxHue, "0") + " min/max Sat " + Format(minSat, "0") + "/" + Format(maxSat, "0")
        End If
        ' Dim histogram = hist.histogram.Normalize(0, 255, cv.NormTypes.MinMax)
        Dim bins() = {0, 1}
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim mat() As cv.Mat = {hsv}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minHue, maxHue), New cv.Rangef(minSat, maxSat)}
        Dim mask As New cv.Mat
        cv.Cv2.CalcBackProject(mat, bins, hist.histogram, mask, ranges)

        dst2.SetTo(0)
        src.CopyTo(dst2, mask)
    End Sub
End Class






Public Class Histogram_HueSaturation2DPlot
    Inherits VBparent
    Dim hueSat As PhotoShop_Hue
    Dim hist2d As Histogram_BackProjection2D
    Dim mats As Mat_4to1
    Public Sub New()
        initParent()

        hueSat = New PhotoShop_Hue()
        hist2d = New Histogram_BackProjection2D()
        mats = New Mat_4to1()
        label2 = "Click any quadrant at left to view it below"
        task.desc = "Compare the hue and brightness images and the results of the histogram_backprojection2d"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        hueSat.src = src
        hueSat.Run()
        mats.mat(0) = hueSat.dst1
        mats.mat(1) = hueSat.dst2

        hist2d.src = src
        hist2d.Run()
        mats.mat(2) = hist2d.dst2
        mats.mat(3) = hist2d.dst1

        mats.Run()
        dst1 = mats.dst1
        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setMyActiveMat()
        dst2 = mats.mat(quadrantIndex)
    End Sub
End Class











Public Class Histogram_TopData
    Inherits VBparent
    Public gCloud As Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Public meterMin As Single
    Public meterMax As Single
    Dim kalman As Kalman_Basics
    Dim IntelBug As Boolean
    Public resizeHistOutput As Boolean = True
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "X scale negative value in meters (meterMin) X100", -400, -5, -200)
            sliders.setupTrackBar(1, "X scale positive value in meters (meterMax) X100", 5, 400, 200)
        End If

        kalman = New Kalman_Basics()
        gCloud = New Depth_PointCloud_IMU()
        If VB_Classes.ActiveTask.algParms.camNames.D455 = task.parms.cameraName Then IntelBug = True

        task.desc = "Create a 2D top view for XZ histogram of depth in meters - NOTE: x and y scales differ!"
    End Sub

    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        gCloud.Run()

        Static minSlider = findSlider("X scale negative value in meters (meterMin) X100")
        Static maxSlider = findSlider("X scale positive value in meters (meterMax) X100")
        meterMin = minSlider.Value / 100
        meterMax = maxSlider.value / 100

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, task.maxZ), New cv.Rangef(meterMin, meterMax)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        If resizeHistOutput Then histSize = {dst2.Height, dst2.Width}
        cv.Cv2.CalcHist(New cv.Mat() {gCloud.dst1}, New Integer() {2, 0}, New cv.Mat, histOutput, 2, histSize, ranges)

        dst1 = histOutput.Flip(cv.FlipMode.X).Threshold(task.thresholdSlider.Value, 255, cv.ThresholdTypes.Binary).Resize(dst1.Size)
        label1 = "Left x = " + Format(meterMin, "#0.00") + " Right X = " + Format(meterMax, "#0.00") + " x and y scales differ!"
    End Sub
End Class









Public Class Histogram_SideData
    Inherits VBparent
    Public gCloud As Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Public meterMin As Single
    Public meterMax As Single
    Dim kalman As Kalman_Basics
    Public resizeHistOutput As Boolean = True
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Y scale negative value in meters (meterMin) X100", -400, -5, -200)
            sliders.setupTrackBar(1, "Y scale positive value in meters (meterMax) X100", 5, 400, 200)
        End If
        kalman = New Kalman_Basics()
        gCloud = New Depth_PointCloud_IMU()

        task.desc = "Create a 2D side view for ZY histogram of depth in meters - NOTE: x and y scales differ!"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        gCloud.Run()

        Static minSlider = findSlider("Y scale negative value in meters (meterMin) X100")
        Static maxSlider = findSlider("Y scale positive value in meters (meterMax) X100")
        meterMin = minSlider.value / 100
        meterMax = maxSlider.value / 100

        Dim ranges() = New cv.Rangef() {New cv.Rangef(meterMin, meterMax), New cv.Rangef(0, task.maxZ)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        If resizeHistOutput Then histSize = {dst2.Height, dst2.Width}
        cv.Cv2.CalcHist(New cv.Mat() {gCloud.dst1}, New Integer() {1, 2}, New cv.Mat, histOutput, 2, histSize, ranges)

        dst1 = histOutput.Threshold(task.thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)
        label1 = "Top y = " + Format(meterMin, "#0.00") + " Bottom Y = " + Format(meterMax, "#0.00") + " x and y scales differ!"
    End Sub
End Class








Public Class Histogram_SmoothTopView2D
    Inherits VBparent
    Public topView As Histogram_TopView2D
    Dim cmat As PointCloud_ColorizeTop
    Dim stable As Motion_MinMaxPointCloud
    Public Sub New()
        initParent()

        cmat = New PointCloud_ColorizeTop
        topView = New Histogram_TopView2D

        stable = New Motion_MinMaxPointCloud

        label1 = "XZ (Top View)"
        task.desc = "Create a 2D top view with stable depth data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        topView.gCloud.Run()

        stable.src = topView.gCloud.dst1
        stable.Run()

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, task.maxZ), New cv.Rangef(-task.topFrustrumAdjust, task.topFrustrumAdjust)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {stable.dst2}, New Integer() {2, 0}, New cv.Mat, topView.histOutput, 2, histSize, ranges)

        topView.histOutput = topView.histOutput.Flip(cv.FlipMode.X)
        dst1 = topView.histOutput.Threshold(task.thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst1, cv.MatType.CV_8UC1)

        dst2 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cmat.src = dst2
        cmat.Run()
        dst2 = cmat.dst1
    End Sub
End Class







Public Class Histogram_SmoothSideView2D
    Inherits VBparent
    Public sideView As Histogram_SideView2D
    Dim cmat As PointCloud_ColorizeSide
    Dim stable As Motion_MinMaxPointCloud
    Public Sub New()
        initParent()

        cmat = New PointCloud_ColorizeSide
        sideView = New Histogram_SideView2D

        stable = New Motion_MinMaxPointCloud

        label1 = "ZY (Side View)"
        task.desc = "Create a 2D side view of stable depth data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        sideView.gCloud.Run()

        stable.src = sideView.gCloud.dst1
        stable.Run()

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.sideFrustrumAdjust, task.sideFrustrumAdjust), New cv.Rangef(0, task.maxZ)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {stable.dst2}, New Integer() {1, 2}, New cv.Mat, sideView.histOutput, 2, histSize, ranges)

        dst1 = sideView.histOutput.Threshold(task.thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst1, cv.MatType.CV_8UC1)

        dst2 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cmat.src = dst2
        cmat.Run()
        dst2 = cmat.dst1
    End Sub
End Class








Public Class Histogram_DepthClusters
    Inherits VBparent
    Public valleys As Histogram_DepthValleys
    Public Sub New()
        initParent()
        valleys = New Histogram_DepthValleys()
        task.desc = "Color each of the Depth Clusters found with Histogram_DepthValleys - stabilized with Kalman."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f.Clone

        valleys.src = src
        valleys.Run()
        dst1 = valleys.dst1

        Dim mask As New cv.Mat
        Dim tmp As New cv.Mat
        Dim colorIncr = (255 / valleys.rangeBoundaries.Count)
        valleys.palette.src = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        For i = 0 To valleys.rangeBoundaries.Count - 1
            Dim startEndDepth = valleys.rangeBoundaries.ElementAt(i)
            cv.Cv2.InRange(src, startEndDepth.X, startEndDepth.Y, tmp)
            cv.Cv2.ConvertScaleAbs(tmp, mask)
            valleys.palette.src.SetTo(i * colorIncr + 1, mask)
        Next
        valleys.palette.Run()
        dst2 = valleys.palette.dst1
        If standalone Or task.intermediateReview = caller Then
            label1 = "Histogram of " + CStr(valleys.rangeBoundaries.Count) + " Depth Clusters"
            label2 = "Backprojection of " + CStr(valleys.rangeBoundaries.Count) + " histogram clusters"
        End If
    End Sub
End Class






Public Class Histogram_StableDepthClusters
    Inherits VBparent
    Dim clusters As Histogram_DepthClusters
    Dim motionSD As Motion_MinMaxDepth
    Public Sub New()
        initParent()

        clusters = New Histogram_DepthClusters
        motionSD = New Motion_MinMaxDepth
        label1 = "Histogram of stable depth"
        label2 = "Backprojection of stable depth"
        task.desc = "Use the stable depth to identify the depth_clusters using histogram valleys"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        motionSD.Run()
        clusters.src = motionSD.dst1
        clusters.Run()
        dst1 = clusters.dst1
        dst2 = clusters.dst2
    End Sub
End Class




Public Class Histogram_DepthValleys
    Inherits VBparent
    Dim kalman As Kalman_Basics
    Dim hist As Histogram_Depth
    Public rangeBoundaries As New List(Of cv.Point)
    Public sortedSizes As New List(Of Integer)
    Public palette As Palette_Basics
    Dim histSlider As Windows.Forms.TrackBar
    Private Class CompareCounts : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just integer?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return -1
            Return 1
        End Function
    End Class
    Private Function histogramBarsValleys(hist As cv.Mat, paletteColors() As Integer) As cv.Mat
        Dim img = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        Dim binCount = hist.Rows
        Dim binWidth = CInt(img.Width / hist.Rows)
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)
        If maxVal > 0 Then
            For i = 0 To binCount - 1
                Dim nextHistCount = hist.Get(Of Single)(i, 0)
                Dim h = CInt(img.Height * nextHistCount / maxVal)
                If h = 0 Then h = 1 ' show the color range in the plot
                Dim barRect As cv.Rect
                barRect = New cv.Rect(i * binWidth, img.Height - h, binWidth, h)
                cv.Cv2.Rectangle(img, barRect, paletteColors(i), -1)
            Next
        End If
        Return img
    End Function
    Public Sub New()
        initParent()
        palette = New Palette_Basics
        palette.whiteBack = True
        Dim radioJet = findRadio("Hsv")
        radioJet.Checked = True
        hist = New Histogram_Depth()

        histSlider = findSlider("Histogram Depth Bins")
        histSlider.Value = 40 ' number of bins.

        kalman = New Kalman_Basics()

        label1 = "Histogram clustered by valleys and smoothed"
        task.desc = "Identify valleys in the Depth histogram."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        hist.src = src
        If hist.src.Type <> cv.MatType.CV_32F Then hist.src = task.depth32f
        hist.Run()
        ReDim kalman.kInput(hist.plotHist.hist.Rows - 1)
        For i = 0 To hist.plotHist.hist.Rows - 1
            kalman.kInput(i) = hist.plotHist.hist.Get(Of Single)(i, 0)
        Next
        kalman.Run()
        For i = 0 To hist.plotHist.hist.Rows - 1
            hist.plotHist.hist.Set(Of Single)(i, 0, kalman.kOutput(i))
        Next

        Dim depthIncr = CInt(task.maxRangeSlider.Value / histSlider.Value) ' each bar represents this number of millimeters
        Dim pointCount = hist.plotHist.hist.Get(Of Single)(0, 0) + hist.plotHist.hist.Get(Of Single)(1, 0)
        Dim startDepth = 1
        Dim startEndDepth As cv.Point
        Dim depthBoundaries As New SortedList(Of Single, cv.Point)(New CompareCounts)
        For i = 2 To kalman.kOutput.Length - 3
            Dim prev2 = If(i > 2, kalman.kOutput(i - 2), 0)
            Dim prev = If(i > 1, kalman.kOutput(i - 1), 0)
            Dim curr = kalman.kOutput(i)
            Dim post = If(i < kalman.kOutput.Length - 1, kalman.kOutput(i + 1), 0)
            Dim post2 = If(i < kalman.kOutput.Length - 2, kalman.kOutput(i + 2), 0)
            pointCount += kalman.kOutput(i)
            If prev2 > 1 And prev > 1 And curr > 1 And post > 1 And post2 > 1 Then
                If curr < (prev + prev2) / 2 And curr < (post + post2) / 2 And i * depthIncr > startDepth + depthIncr Then
                    startEndDepth = New cv.Point(startDepth, i * depthIncr)
                    depthBoundaries.Add(pointCount, startEndDepth)
                    pointCount = 0
                    startDepth = i * depthIncr + 0.1
                End If
            End If
        Next

        startEndDepth = New cv.Point(startDepth, CInt(task.maxRangeSlider.Value))
        depthBoundaries.Add(pointCount, startEndDepth) ' capped at the max depth we are observing

        rangeBoundaries.Clear()
        sortedSizes.Clear()
        For i = depthBoundaries.Count - 1 To 0 Step -1
            rangeBoundaries.Add(depthBoundaries.ElementAt(i).Value)
            sortedSizes.Add(depthBoundaries.ElementAt(i).Key)
        Next

        Dim paletteColors(hist.plotHist.hist.Rows - 1) As Integer
        Dim colorIncr = (255 / rangeBoundaries.Count)
        For i = 0 To hist.plotHist.hist.Rows - 1
            Dim depth = i * depthIncr + 1
            For j = 0 To rangeBoundaries.Count - 1
                Dim startEnd = rangeBoundaries.ElementAt(j)
                If depth >= startEnd.X And depth < startEnd.Y Then
                    paletteColors(i) = j * colorIncr + 1
                    Exit For
                End If
            Next
        Next

        dst1 = histogramBarsValleys(hist.plotHist.hist, paletteColors)
        palette.src = dst1
        palette.Run()
        dst1 = palette.dst1
    End Sub
End Class











Public Class Histogram_TopView2D
    Inherits VBparent
    Public gCloud As Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Public originalHistOutput As New cv.Mat
    Public markers(2 - 1) As cv.Point2f
    Public cmat As PointCloud_ColorizeTop
    Public resizeHistOutput As Boolean = True
    Public Sub New()
        initParent()

        cmat = New PointCloud_ColorizeTop
        gCloud = New Depth_PointCloud_IMU
        If standalone Then task.viewOptions.sliders.show()

        label1 = "XZ (Top View)"
        task.desc = "Create a 2D top view for XZ histogram of depth - NOTE: x and y scales are the same"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone Then task.viewOptions.run()

        gCloud.src = src
        If gCloud.src.Type <> cv.MatType.CV_32FC3 Then gCloud.src = task.pointCloud.Clone
        gCloud.Run() ' when displaying both top and side views, the gcloud run has already been done.

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, task.maxZ), New cv.Rangef(-task.topFrustrumAdjust, task.topFrustrumAdjust)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        If resizeHistOutput Then histSize = {dst2.Height, dst2.Width}
        cv.Cv2.CalcHist(New cv.Mat() {gCloud.dst1}, New Integer() {2, 0}, New cv.Mat, originalHistOutput, 2, histSize, ranges)

        originalHistOutput = originalHistOutput.Flip(cv.FlipMode.X)
        histOutput = originalHistOutput.Threshold(task.thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)
        dst1 = histOutput.Clone
        dst1.ConvertTo(dst1, cv.MatType.CV_8UC1)
        If standalone Or task.intermediateReview = caller Then
            dst2 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            cmat.src = dst2
            cmat.Run()
            dst2 = cmat.dst1
        End If
    End Sub
End Class









Public Class Histogram_SideView2D
    Inherits VBparent
    Public gCloud As Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Public originalHistOutput As New cv.Mat
    Public cmat As PointCloud_ColorizeSide
    Public frustrumAdjust As Single
    Public resizeHistOutput As Boolean = True
    Public Sub New()
        initParent()

        cmat = New PointCloud_ColorizeSide
        gCloud = New Depth_PointCloud_IMU
        If standalone Or task.intermediateReview = caller Then task.yRotateSlider.Value = 1
        If standalone Then task.viewOptions.sliders.show()

        label1 = "ZY (Side View)"
        task.desc = "Create a 2D side view for ZY histogram of depth - NOTE: x and y scales are the same"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone Then task.viewOptions.run()

        gCloud.src = src
        If gCloud.src.Type <> cv.MatType.CV_32FC3 Then gCloud.src = task.pointCloud.Clone
        gCloud.Run()

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.sideFrustrumAdjust, task.sideFrustrumAdjust), New cv.Rangef(0, task.maxZ)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        If resizeHistOutput Then histSize = {dst2.Height, dst2.Width}
        cv.Cv2.CalcHist(New cv.Mat() {gCloud.dst1}, New Integer() {1, 2}, New cv.Mat, originalHistOutput, 2, histSize, ranges)

        histOutput = originalHistOutput.Threshold(task.thresholdSlider.Value, 255, cv.ThresholdTypes.Binary).Resize(dst1.Size)
        histOutput.ConvertTo(dst1, cv.MatType.CV_8UC1)

        If standalone Or task.intermediateReview = caller Then
            dst2 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            cmat.src = dst2
            cmat.Run()
            dst2 = cmat.dst1
        End If
    End Sub
End Class









' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class Histogram_BackProjectionGrayscale
    Inherits VBparent
    Dim hist As Histogram_Basics
    Public histIndex As Integer
    Public binSlider As Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        hist = New Histogram_Basics
        binSlider = findSlider("Histogram Bins")
        binSlider.Value = 10

        label1 = "Move mouse to backproject each histogram column"
        task.desc = "Explore Backprojection of each element of a grayscale histogram."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        hist.src = src
        hist.Run()
        dst1 = hist.dst1

        histIndex = CInt(hist.histogram.Rows * task.mousePoint.X / src.Width)
        Dim barWidth = dst1.Width / binSlider.Value
        Dim barRange = Math.Round(255 / binSlider.Value)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(histIndex * barRange, (histIndex + 1) * barRange)}
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim mat() As cv.Mat = {gray}
        Dim bins() = {0}
        cv.Cv2.CalcBackProject(mat, bins, hist.histogram, dst2, ranges)

        label2 = "Backprojecting " + CStr(histIndex * barRange) + " to " + CStr((histIndex + 1) * barRange) + " with " + Format(hist.histogram.Get(Of Single)(histIndex, 0), "#0") + " samples"
        dst1.Rectangle(New cv.Rect(barWidth * histIndex, 0, barWidth, dst1.Height), cv.Scalar.Yellow, 5)
    End Sub
End Class









Public Class Histogram_ViewIntersections
    Inherits VBparent
    Dim histCO As Histogram_ViewObjects
    Public Sub New()
        initParent()
        histCO = New Histogram_ViewObjects
        label1 = "Yellow is largest intersection.  dst2 = point cloud"
        task.desc = "Find the intersections of the rectangles found in the Histogram_ConcentrationObjects"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        histCO.Run()
        dst1 = histCO.dst2

        Dim offset = If(src.Width = 1280, 10, 16)
        Dim w = dst1.Width
        Dim h = dst1.Height
        Dim minZ As Single, maxZ As Single
        Dim rIntersect As New List(Of cv.Rect)
        Dim yRange As New List(Of cv.Vec2f)
        For Each r In histCO.side2D
            minZ = task.maxZ * r.X / dst1.Width
            maxZ = task.maxZ * (r.X + r.Width) / dst1.Width
            Dim newRect = New cv.Rect(0, h - h * minZ / task.maxZ - r.Width, w, r.Width)
            For Each r2 In histCO.top2D
                Dim rNext = r2.Intersect(newRect)
                If rNext.Width > 0 And rNext.Height > 0 Then
                    rIntersect.Add(rNext)
                    yRange.Add(New cv.Vec2f(minZ, maxZ))
                End If
            Next
        Next

        Dim maxSize As Single = Single.MinValue
        Dim maxIndex As Integer
        For i = 0 To rIntersect.Count - 1
            Dim r = rIntersect(i)
            If maxSize < r.Width * r.Height Then
                maxSize = r.Width * r.Height
                maxIndex = i
            End If
        Next

        If rIntersect.Count > 0 Then
            dst1.Rectangle(rIntersect(maxIndex), cv.Scalar.Yellow, 2)
            minZ = task.maxZ * (h - rIntersect(maxIndex).Y - rIntersect(maxIndex).Height) / h
            maxZ = task.maxZ * (h - rIntersect(maxIndex).Y) / h
            ocvb.trueText(Format(minZ, "0.0") + "m to " + Format(maxZ, "0.0") + "m", rIntersect(maxIndex).X, rIntersect(maxIndex).Y - offset)

            Dim pc = histCO.histC.sideview.gCloud.dst1
            Dim split = pc.Split()
            Dim mask As New cv.Mat
            cv.Cv2.InRange(split(2), minZ, maxZ, mask)
            cv.Cv2.BitwiseNot(mask, mask)
            split(2).SetTo(0, mask)

            cv.Cv2.Merge(split, dst2)
        End If
    End Sub
End Class












Public Class Histogram_ViewObjects
    Inherits VBparent
    Public histC As Histogram_ViewConcentrationsTopX
    Dim flood As FloodFill_Image
    Dim minSizeSlider As Windows.Forms.TrackBar
    Dim loDiffSlider As Windows.Forms.TrackBar
    Dim hiDiffSlider As Windows.Forms.TrackBar
    Dim stepSlider As Windows.Forms.TrackBar
    Public side2D As New List(Of cv.Rect)
    Public top2D As New List(Of cv.Rect)
    Public Sub New()
        initParent()

        flood = New FloodFill_Image
        histC = New Histogram_ViewConcentrationsTopX

        minSizeSlider = findSlider("FloodFill Minimum Size")
        loDiffSlider = findSlider("FloodFill LoDiff")
        hiDiffSlider = findSlider("FloodFill HiDiff")
        stepSlider = findSlider("Step Size")
        loDiffSlider.Value = 250
        hiDiffSlider.Value = 255

        Dim dotSlider = findSlider("Dot size")
        stepSlider.Value = dotSlider.Value
        minSizeSlider.Value = dotSlider.Value * dotSlider.Value

        task.desc = "Use the histogram concentrations to identify objects in the field of view"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        histC.Run()

        dst1 = histC.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst2 = histC.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        flood.src = dst1
        flood.Run()
        dst1 = flood.dst1.Clone.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim offset = If(src.Width = 1280, 10, 16)
        Dim w = dst1.Width
        Dim h = dst1.Height
        Dim minZ As Single, maxZ As Single
        side2D.Clear()

        For Each r In flood.rects
            side2D.Add(r)
            dst1.Rectangle(r, cv.Scalar.White, 1)
            minZ = task.maxZ * r.X / w
            maxZ = task.maxZ * (r.X + r.Width) / w
            If standalone Or task.intermediateReview = caller Then ocvb.trueText(Format(minZ, "0.0") + "m to " + Format(maxZ, "0.0") + "m", r.X, r.Y - offset)
        Next
        label1 = CStr(flood.rects.Count) + " objects were identified in the side view"

        flood.src = dst2
        flood.Run()
        dst2 = flood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        top2D.Clear()
        For Each r In flood.rects
            top2D.Add(r)
            dst2.Rectangle(r, cv.Scalar.White, 1)
            minZ = task.maxZ * (h - r.Y - r.Height) / h
            maxZ = task.maxZ * (h - r.Y) / h
            If standalone Or task.intermediateReview = caller Then ocvb.trueText(Format(minZ, "0.0") + "m to " + Format(maxZ, "0.0") + "m", r.X, r.Y - offset, 3)
        Next

        label2 = CStr(flood.rects.Count) + " objects identified.  Largest is yellow."
    End Sub
End Class








Public Class Histogram_SmoothConcentration
    Inherits VBparent
    Public sideview As Histogram_SmoothSideView2D
    Public topview As Histogram_SmoothTopView2D
    Dim concent As Histogram_ViewConcentrationsTopX
    Public Sub New()
        initParent()

        sideview = New Histogram_SmoothSideView2D
        topview = New Histogram_SmoothTopView2D
        concent = New Histogram_ViewConcentrationsTopX

        task.desc = "Using stable depth data, highlight the histogram projections where concentrations are highest"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        sideview.Run()
        dst1 = sideview.dst1
        Dim noDepth = sideview.sideView.histOutput.Get(Of Single)(sideview.sideView.histOutput.Height / 2, 0)
        label1 = "SideView " + concent.plotHighlights(sideview.sideView.histOutput, dst1) + " No depth: " + CStr(CInt(noDepth / 1000)) + "k"
        dst1 = concent.palette.dst1.Clone

        topview.Run()
        dst2 = topview.dst1
        label2 = "TopView " + concent.plotHighlights(topview.topView.histOutput, dst2) + " No depth: " + CStr(CInt(noDepth / 1000)) + "k"
        dst2 = concent.palette.dst1.Clone
    End Sub
End Class








Public Class Histogram_ViewConcentrationsTopX
    Inherits VBparent
    Public sideview As Histogram_SideView2D
    Public topview As Histogram_TopView2D
    Public palette As Palette_Basics
    Public Sub New()
        initParent()

        palette = New Palette_Basics

        sideview = New Histogram_SideView2D
        topview = New Histogram_TopView2D

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Display the top x highlights", 1, 1000, 50)
            sliders.setupTrackBar(1, "Resize Factor x100", 1, 100, 10)
            sliders.setupTrackBar(2, "Concentration Threshold", 1, 100, 10)
            sliders.setupTrackBar(3, "Dot size", 1, 100, If(src.Width = 1280, 20, 10))
        End If
        task.desc = "Highlight a fixed number of histogram projections where concentrations are highest"
    End Sub
    Public Function plotHighlights(histOutput As cv.Mat, dst As cv.Mat) As String
        Static ResizeSlider = findSlider("Resize Factor x100")
        Dim resizeFactor = ResizeSlider.Value / 100

        Static cThresholdSlider = findSlider("Concentration Threshold")
        Dim concentrationThreshold = cThresholdSlider.Value

        Dim minPixel = CInt(resizeFactor * task.minRangeSlider.Value * ocvb.pixelsPerMeter / 1000)

        Dim tmp = histOutput.Resize(New cv.Size(CInt(histOutput.Width * resizeFactor), CInt(histOutput.Height * resizeFactor)))
        Dim pts As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalIntegerInverted)
        For y = 0 To tmp.Height - 1
            For x = minPixel To tmp.Width - 1
                Dim val = tmp.Get(Of Single)(y, x)
                If val > concentrationThreshold Then pts.Add(val, New cv.Point(CInt(x / resizeFactor), CInt(y / resizeFactor)))
            Next
        Next

        Static topXslider = findSlider("Display the top x highlights")
        Static dotSlider = findSlider("Dot size")
        Dim topX = topXslider.value
        Dim dotsize = dotSlider.value
        For i = 0 To Math.Min(pts.Count - 1, topX - 1)
            Dim pt = pts.ElementAt(i).Value
            dst.Rectangle(New cv.Rect(pt.X - dotsize, pt.Y - dotsize, dotsize * 2, dotsize * 2), 128, -1)
        Next
        palette.src = dst
        palette.Run()
        Dim maxConcentration = If(pts.Count > 0, pts.ElementAt(0).Key, 0)
        Return CStr(pts.Count) + " highlights. Max=" + CStr(maxConcentration)
    End Function
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        sideview.Run()
        dst1 = sideview.dst1
        Dim noDepth = sideview.histOutput.Get(Of Single)(sideview.histOutput.Height / 2, 0)
        label1 = "SideView " + plotHighlights(sideview.histOutput, dst1) + " No depth: " + CStr(CInt(noDepth / 1000)) + "k"
        If standalone Or task.intermediateReview = caller Then dst1 = palette.dst1.Clone

        topview.Run()
        dst2 = topview.dst1
        label2 = "TopView " + plotHighlights(topview.histOutput, dst2) + " No depth: " + CStr(CInt(noDepth / 1000)) + "k"
        If standalone Or task.intermediateReview = caller Then dst2 = palette.dst1.Clone
    End Sub
End Class







