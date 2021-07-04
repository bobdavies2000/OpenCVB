Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Histogram_Basics : Inherits VBparent
    Public histogram As New cv.Mat
    Public kalman As New Kalman_Basics
    Public plotHist As New Plot_Histogram
    Public depthNoZero As Boolean
    Public Sub New()
        plotHist.minRange = 0
        task.desc = "Create a histogram of the grayscale image and smooth the bar chart with a kalman filter."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 3
        Static splitIndex = 0
        Static colorName = "Gray"
        If standalone Or task.intermediateName = caller Then
            If src.Channels <> 1 Then
                If task.frameCount Mod 100 = 0 Then splitIndex = If(splitIndex < 2, splitIndex + 1, 0)
                colorName = Choose(splitIndex + 1, "Blue", "Green", "Red")
            End If
        End If

        Dim histSize() = {task.histogramBins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}

        Dim dimensions() = New Integer() {task.histogramBins}
        cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {splitIndex}, New cv.Mat, histogram, 1, dimensions, ranges)

        If kalman.kInput.Length <> task.histogramBins Then ReDim kalman.kInput(task.histogramBins - 1)

        For i = 0 To task.histogramBins - 1
            kalman.kInput(i) = histogram.Get(Of Single)(i, 0)
        Next
        kalman.RunClass(src)
        histogram = New cv.Mat(kalman.kOutput.Length, 1, cv.MatType.CV_32FC1, kalman.kOutput)

        If depthNoZero Then histogram.Set(Of Single)(0, 0, 0) ' let's not plot the depth at zero...
        plotHist.hist = histogram
        Dim splitColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
        plotHist.backColor = splitColors(splitIndex)
        plotHist.RunClass(src)
        dst2 = plotHist.dst2
        labels(2) = colorName + " histogram, bins = " + CStr(task.histogramBins)
    End Sub
End Class







' https://github.com/opencv/opencv/blob/master/samples/python/hist.py
Public Class Histogram_Graph : Inherits VBparent
    Public histRaw(3 - 1) As cv.Mat
    Public histNormalized(3 - 1) As cv.Mat
    Public minRange As Single = 0
    Public maxRange As Single = 255
    Public backColor = cv.Scalar.Gray
    Public plotRequested As Boolean
    Public plotColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
    Public plotMaxValue As Single
    Public Sub New()
        task.desc = "Plot histograms for up to 3 channels."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim bins = task.histogramBins
        Dim dimensions() = New Integer() {bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

        Dim plotWidth = dst2.Width / bins

        dst2.SetTo(backColor)
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
                    points.Add(New cv.Point(CInt(j * plotWidth), dst2.Rows - dst2.Rows * histRaw(i).Get(Of Single)(j, 0) / maxVal))
                Next
                listOfPoints.Add(points)
                dst2.Polylines(listOfPoints, False, plotColors(i), task.lineWidth, task.lineType)
            End If
        Next

        If standalone Or plotRequested Then
            plotMaxValue = Math.Round(maxVal / 1000, 0) * 1000 + 1000 ' smooth things out a little for the scale below
            AddPlotScale(dst2, 0, plotMaxValue, task.fontSize * 2)
            labels(2) = "Histogram for src image (default color) - " + CStr(bins) + " bins"
        End If
    End Sub
End Class





Module histogram_Functions
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Histogram_3D_RGB(rgbPtr As IntPtr, rows As Integer, cols As Integer, bins As Integer) As IntPtr
    End Function

    Public Sub histogram2DPlot(histogram As cv.Mat, dst2 As cv.Mat, xBins As Integer, yBins As Integer)
        Dim maxVal As Double
        histogram.MinMaxLoc(0, maxVal)
        Dim xScale = dst2.Cols / xBins
        Dim yScale = dst2.Rows / yBins
        For y = 0 To yBins - 1
            For x = 0 To xBins - 1
                Dim binVal = histogram.Get(Of Single)(y, x)
                Dim intensity = Math.Round(binVal * 255 / maxVal)
                Dim pt1 = New cv.Point(x * xScale, y * yScale)
                Dim pt2 = New cv.Point((x + 1) * xScale - 1, (y + 1) * yScale - 1)
                If pt1.X >= dst2.Cols Then pt1.X = dst2.Cols - 1
                If pt1.Y >= dst2.Rows Then pt1.Y = dst2.Rows - 1
                If pt2.X >= dst2.Cols Then pt2.X = dst2.Cols - 1
                If pt2.Y >= dst2.Rows Then pt2.Y = dst2.Rows - 1
                If pt1.X <> pt2.X And pt1.Y <> pt2.Y Then
                    Dim value = cv.Scalar.All(255 - intensity)
                    'value = New cv.Scalar(pt1.X * 255 / dst2.Cols, pt1.Y * 255 / dst2.Rows, 255 - intensity)
                    value = New cv.Scalar(intensity, intensity, intensity)
                    dst2.Rectangle(pt1, pt2, value, -1, task.lineType)
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
End Module





Public Class Histogram_NormalizeGray : Inherits VBparent
    Public histogram As New Histogram_Basics
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Min Gray", 0, 255, 0)
            sliders.setupTrackBar(1, "Max Gray", 0, 255, 255)
        End If

        If check.Setup(caller, 1) Then
            check.Box(0).Text = "Normalize Before Histogram"
            check.Box(0).Checked = True
        End If

        task.desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If check.Box(0).Checked Then
            cv.Cv2.Normalize(src, src, sliders.trackbar(0).Value, sliders.trackbar(1).Value, cv.NormTypes.MinMax) ' only minMax is working...
        End If
        histogram.RunClass(src)
        dst2 = histogram.dst2
    End Sub
End Class






' https://docs.opencv.org/2.4/modules/imgproc/doc/histograms.html
Public Class Histogram_2D_HueSaturation : Inherits VBparent
    Public histogram As New cv.Mat
    Public hsv As cv.Mat

    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Hue bins", 1, 180, 30) ' quantize hue to 30 levels
            sliders.setupTrackBar(1, "Saturation bins", 1, 256, 32) ' quantize sat to 32 levels
        End If
        task.desc = "Create a histogram for hue and saturation."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        hsv = src.CvtColor(cv.ColorConversionCodes.RGB2HSV)
        Dim hbins = sliders.trackbar(0).Value
        Dim sbins = sliders.trackbar(1).Value
        Dim histSize() = {sbins, hbins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, sliders.trackbar(0).Maximum - 1), New cv.Rangef(0, sliders.trackbar(1).Maximum - 1)} ' hue ranges from 0-179

        cv.Cv2.CalcHist(New cv.Mat() {hsv}, New Integer() {0, 1}, New cv.Mat(), histogram, 2, histSize, ranges)

        histogram2DPlot(histogram, dst2, hbins, sbins)
    End Sub
End Class







Public Class Histogram_2D_XZ_YZ : Inherits VBparent
    Dim xyz As New Mat_ImageXYZ_MT
    Dim minSlider As Windows.Forms.TrackBar
    Dim maxSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Histogram X bins", 1, dst2.Cols, 30)
            sliders.setupTrackBar(1, "Histogram Y bins", 1, dst2.Rows, 30)
            sliders.setupTrackBar(2, "Histogram Z bins", 1, 200, 100)
        End If
        task.desc = "Create a 2D histogram for depth in XZ and YZ."
        labels(3) = "Left is XZ (Top View) and Right is YZ (Side View)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim xbins = sliders.trackbar(0).Value
        Dim ybins = sliders.trackbar(1).Value
        Dim zbins = sliders.trackbar(2).Value

        Dim histogram As New cv.Mat

        Dim rangesX() = New cv.Rangef() {New cv.Rangef(0, src.Width - 1), New cv.Rangef(task.minDepth, task.maxDepth)}
        Dim rangesY() = New cv.Rangef() {New cv.Rangef(0, src.Width - 1), New cv.Rangef(task.minDepth, task.maxDepth)}

        xyz.RunClass(src)
        Dim sizesX() = {xbins, zbins}
        cv.Cv2.CalcHist(New cv.Mat() {xyz.xyDepth}, New Integer() {0, 2}, New cv.Mat(), histogram, 2, sizesX, rangesX)
        histogram2DPlot(histogram, dst2, zbins, xbins)

        Dim sizesY() = {ybins, zbins}
        cv.Cv2.CalcHist(New cv.Mat() {xyz.xyDepth}, New Integer() {1, 2}, New cv.Mat(), histogram, 2, sizesY, rangesY)
        histogram2DPlot(histogram, dst3, zbins, ybins)
    End Sub
End Class








' https://docs.opencv.org/master/d1/db7/tutorial_py_histogram_begins.html
Public Class Histogram_EqualizeColor : Inherits VBparent
    Public kalmanEq As New Histogram_Basics
    Public kalman As New Histogram_Basics
    Dim mats As New Mat_2to1
    Public displayHist As Boolean = False
    Public channel = 2
    Public Sub New()
        task.desc = "Create an equalized histogram of the color image."
        labels(2) = "Image Enhanced with Equalized Histogram"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim rgb(2) As cv.Mat
        Dim rgbEq(2) As cv.Mat
        rgbEq = src.Split()

        For i = 0 To rgb.Count - 1
            cv.Cv2.EqualizeHist(rgbEq(i), rgbEq(i))
        Next

        If standalone Or displayHist Then
            cv.Cv2.Split(src, rgb) ' equalizehist alters the input...
            kalman.plotHist.backColor = cv.Scalar.Red
            kalman.RunClass(rgb(channel).Clone())
            mats.mat(0) = kalman.dst2.Clone()

            kalmanEq.RunClass(rgbEq(channel).Clone())
            mats.mat(1) = kalmanEq.dst2.Clone()

            mats.RunClass(src)
            dst3 = mats.dst2
            labels(3) = "Before (top) and After Red Histogram"

            cv.Cv2.Merge(rgbEq, dst2)
        End If
    End Sub
End Class






'https://docs.opencv.org/master/d1/db7/tutorial_py_histogram_begins.html
Public Class Histogram_EqualizeGray : Inherits VBparent
    Public histogram As New Histogram_Basics
    Dim mats As New Mat_2to1
    Dim matsrc As New Mat_2to1
    Public Sub New()
        labels(2) = "top original image, bottom Equalized image"
        labels(3) = "top is before and bottom is after EqualizeHist"
        task.desc = "Create an equalized histogram of the grayscale image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        task.useKalman = False
        histogram.RunClass(src)
        matsrc.mat(0) = src.Clone
        mats.mat(0) = histogram.dst2.Clone
        cv.Cv2.EqualizeHist(src, dst1)
        matsrc.mat(1) = dst1.Clone
        task.useKalman = True
        histogram.RunClass(dst1)
        If standalone Or task.intermediateActive Then
            mats.mat(1) = histogram.dst2
            mats.RunClass(Nothing)
            dst3 = mats.dst2
        End If
        matsrc.RunClass(Nothing)
        dst2 = matsrc.dst2
    End Sub
End Class






Public Class Histogram_Simple : Inherits VBparent
    Public plotHist As New Plot_Histogram
    Public Sub New()
        labels(2) = "Histogram of the grayscale video stream"
        task.desc = "Build a simple and reusable histogram for grayscale images."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim histSize() = {task.histogramBins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}
        cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {0}, New cv.Mat, plotHist.hist, 1, histSize, ranges)

        plotHist.RunClass(src)
        dst2 = plotHist.dst2
    End Sub
End Class









Public Class Histogram_ColorsAndGray : Inherits VBparent
    Dim histogram As New Histogram_Basics
    Dim mats As New Mat_4Click
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Min Gray", 0, 255, 0)
            sliders.setupTrackBar(1, "Max Gray", 0, 255, 255)
        End If

        If check.Setup(caller, 1) Then
            check.Box(0).Text = "Normalize Before Histogram"
            check.Box(0).Checked = True
        End If

        labels(3) = "Click any quadrant at left to view it below"
        task.desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        task.useKalman = False
        Dim split = src.Split()
        ReDim Preserve split(4 - 1)
        split(4 - 1) = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) ' add a 4th image - the grayscale image to the R G and B images.
        For i = 0 To split.Length - 1
            Dim histSrc = split(i).Clone()
            If check.Box(0).Checked Then
                cv.Cv2.Normalize(split(i), histSrc, sliders.trackbar(0).Value, sliders.trackbar(1).Value, cv.NormTypes.MinMax) ' only minMax is working...
            End If
            histogram.plotHist.backColor = Choose(i + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.PowderBlue)
            histogram.RunClass(histSrc)
            mats.mat(i) = histogram.dst2.Clone()
        Next

        mats.RunClass(src)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class









Public Class Histogram_SmoothTopView2D : Inherits VBparent
    Public topView As New Histogram_TopView2D
    Dim setupTop As New PointCloud_SetupTop
    Dim stable As New Motion_MinMaxPointCloud
    Public Sub New()
        labels(2) = "XZ (Top View)"
        task.desc = "Create a 2D top view with stable depth data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        topView.gCloud.RunClass(src)

        stable.RunClass(topView.gCloud.dst2)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, task.maxZ), New cv.Rangef(-task.maxX, task.maxX)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {stable.dst3}, New Integer() {2, 0}, New cv.Mat, topView.histOutput, 2, histSize, ranges)

        topView.histOutput = topView.histOutput.Flip(cv.FlipMode.X)
        dst2 = topView.histOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)

        dst3 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        setupTop.RunClass(dst3)
        dst3 = setupTop.dst2
    End Sub
End Class







Public Class Histogram_SmoothSideView2D : Inherits VBparent
    Public sideView As New Histogram_SideView2D
    Dim setupSide As New PointCloud_SetupSide
    Dim stable As New Motion_MinMaxPointCloud
    Public Sub New()
        labels(2) = "ZY (Side View)"
        task.desc = "Create a 2D side view of stable depth data"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        sideView.gCloud.RunClass(src)

        stable.RunClass(sideView.gCloud.dst2)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.maxY, task.maxY), New cv.Rangef(0, task.maxZ)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {stable.dst3}, New Integer() {1, 2}, New cv.Mat, sideView.histOutput, 2, histSize, ranges)

        dst2 = sideView.histOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)

        dst3 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        setupSide.RunClass(dst3)
        dst3 = setupSide.dst2
    End Sub
End Class







Public Class Histogram_StableDepthClusters : Inherits VBparent
    Dim clusters As New Proximity_Clusters
    Dim motionSD As New Motion_MinMaxDepth
    Public Sub New()
        labels(2) = "Histogram of stable depth"
        labels(3) = "Backprojection of stable depth"
        task.desc = "Use the stable depth to identify the depth_clusters using histogram valleys"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        motionSD.RunClass(src)
        clusters.RunClass(motionSD.dst2)
        dst2 = clusters.dst2
        dst3 = clusters.dst3
    End Sub
End Class











Public Class Histogram_TopView2D : Inherits VBparent
    Public gCloud As New Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Public originalHistOutput As New cv.Mat
    Public markers(2 - 1) As cv.Point2f
    Public Sub New()
        labels(2) = "XZ (Top View)"
        task.desc = "Create a 2D top view for XZ histogram of depth - NOTE: x and y scales are the same"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud.Clone
        gCloud.RunClass(src) ' when displaying both top and side views, the gcloud run has already been done.

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, task.maxZ), New cv.Rangef(-task.maxX, task.maxX)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {gCloud.dst2}, New Integer() {2, 0}, New cv.Mat, originalHistOutput, 2, histSize, ranges)

        originalHistOutput = originalHistOutput.Flip(cv.FlipMode.X)
        histOutput = originalHistOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary)
        dst2 = histOutput.Clone
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class









Public Class Histogram_SideView2D : Inherits VBparent
    Public gCloud As New Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Public originalHistOutput As New cv.Mat
    Public frustrumAdjust As Single
    Public Sub New()
        labels(2) = "ZY (Side View)"
        task.desc = "Create a 2D side view for ZY histogram of depth - NOTE: x and y scales are the same"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        gCloud.RunClass(src)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.maxY, task.maxY), New cv.Rangef(0, task.maxZ)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {gCloud.dst2}, New Integer() {1, 2}, New cv.Mat, originalHistOutput, 2, histSize, ranges)

        histOutput = originalHistOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary).Resize(dst2.Size)
        histOutput.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class










'Public Class Histogram_ViewIntersections : Inherits VBparent
'    Dim histCO As New Histogram_ViewObjects
'    Public Sub New()
'        labels(2) = "Yellow is largest intersection.  dst3 = point cloud"
'        task.desc = "Find the intersections of the rectangles found in the Histogram_ConcentrationObjects"
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1

'        histCO.RunClass(src)
'        dst2 = histCO.dst3

'        Dim offset = If(src.Width = 1280, 10, 16)
'        Dim w = dst2.Width
'        Dim h = dst2.Height
'        Dim minZ As Single, maxZ As Single
'        Dim rIntersect As New List(Of cv.Rect)
'        Dim yRange As New List(Of cv.Vec2f)
'        For Each r In histCO.side2D
'            minZ = task.maxZ * r.X / dst2.Width
'            maxZ = task.maxZ * (r.X + r.Width) / dst2.Width
'            Dim newRect = New cv.Rect(0, h - h * minZ / task.maxZ - r.Width, w, r.Width)
'            For Each r2 In histCO.top2D
'                Dim rNext = r2.Intersect(newRect)
'                If rNext.Width > 0 And rNext.Height > 0 Then
'                    rIntersect.Add(rNext)
'                    yRange.Add(New cv.Vec2f(minZ, maxZ))
'                End If
'            Next
'        Next

'        Dim maxSize As Single = Single.MinValue
'        Dim maxIndex As Integer
'        For i = 0 To rIntersect.Count - 1
'            Dim r = rIntersect(i)
'            If maxSize < r.Width * r.Height Then
'                maxSize = r.Width * r.Height
'                maxIndex = i
'            End If
'        Next

'        If rIntersect.Count > 0 Then
'            dst2.Rectangle(rIntersect(maxIndex), cv.Scalar.Yellow, 2)
'            minZ = task.maxZ * (h - rIntersect(maxIndex).Y - rIntersect(maxIndex).Height) / h
'            maxZ = task.maxZ * (h - rIntersect(maxIndex).Y) / h
'            setTrueText(Format(minZ, "0.0") + "m to " + Format(maxZ, "0.0") + "m", rIntersect(maxIndex).X, rIntersect(maxIndex).Y - offset)

'            Dim pc = histCO.histC.sideview.gCloud.dst2
'            Dim split = pc.Split()
'            Dim mask As New cv.Mat
'            cv.Cv2.InRange(split(2), minZ, maxZ, mask)
'            cv.Cv2.BitwiseNot(mask, mask)
'            split(2).SetTo(0, mask)

'            cv.Cv2.Merge(split, dst3)
'        End If
'    End Sub
'End Class












'Public Class Histogram_ViewObjects : Inherits VBparent
'    Public histC As New Concentration_Basics
'    Dim flood As New FloodFill_Basics
'    Public side2D As New List(Of cv.Rect)
'    Public top2D As New List(Of cv.Rect)
'    Public Sub New()
'        findSlider("FloodFill Minimum Size").Value = task.dotSize * task.dotSize
'        findSlider("FloodFill LoDiff").Value = 250
'        findSlider("FloodFill HiDiff").Value = 255
'        task.desc = "Use the histogram concentrations to identify objects in the field of view"
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        histC.RunClass(src)

'        dst2 = histC.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
'        dst3 = histC.dst3.Threshold(0, 255, cv.ThresholdTypes.Binary)

'        flood.RunClass(dst2)
'        dst2 = flood.dst2.Clone

'        Dim offset = If(src.Width = 1280, 10, 16)
'        Dim w = dst2.Width
'        Dim h = dst2.Height
'        Dim minZ As Single, maxZ As Single
'        side2D.Clear()

'        For Each r In flood.rects
'            side2D.Add(r)
'            dst2.Rectangle(r, cv.Scalar.White, 1)
'            minZ = task.maxZ * r.X / w
'            maxZ = task.maxZ * (r.X + r.Width) / w
'            If standalone Or task.intermediateName = caller Then setTrueText(Format(minZ, "0.0") + "m to " + Format(maxZ, "0.0") + "m", r.X, r.Y - offset)
'        Next
'        labels(2) = CStr(flood.rects.Count) + " objects were identified in the side view"

'        flood.RunClass(dst3)
'        dst3 = flood.dst2

'        top2D.Clear()
'        For Each r In flood.rects
'            top2D.Add(r)
'            dst3.Rectangle(r, cv.Scalar.White, 1)
'            minZ = task.maxZ * (h - r.Y - r.Height) / h
'            maxZ = task.maxZ * (h - r.Y) / h
'            If standalone Or task.intermediateName = caller Then setTrueText(Format(minZ, "0.0") + "m to " + Format(maxZ, "0.0") + "m", r.X, r.Y - offset, 3)
'        Next

'        labels(3) = CStr(flood.rects.Count) + " objects identified.  Largest is yellow."
'    End Sub
'End Class






Public Class Histogram_Frustrum : Inherits VBparent
    Dim sideFrustrumSlider As Windows.Forms.TrackBar
    Dim topFrustrumSlider As Windows.Forms.TrackBar
    Dim cameraXSlider As Windows.Forms.TrackBar
    Dim cameraYSlider As Windows.Forms.TrackBar
    Dim tView As New TimeView_Basics
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "SideView Frustrum adjustment", 1, 200, 57)
            sliders.setupTrackBar(1, "SideCameraPoint adjustment", -100, 100, 0)
            sliders.setupTrackBar(2, "TopView Frustrum adjustment", 1, 200, 57)
            sliders.setupTrackBar(3, "TopCameraPoint adjustment", -10, 10, 0)
        End If

        findCheckBox("Rotate pointcloud around X-axis using gravity vector angleZ").Checked = False
        findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX").Checked = False
        sideFrustrumSlider = findSlider("SideView Frustrum adjustment")
        topFrustrumSlider = findSlider("TopView Frustrum adjustment")
        cameraXSlider = findSlider("TopCameraPoint adjustment")
        cameraYSlider = findSlider("SideCameraPoint adjustment")

        sideFrustrumSlider.Value = 100 * 2 * task.maxY / task.maxZ
        topFrustrumSlider.Value = 100 * 2 * task.maxX / task.maxZ
        cameraXSlider.Value = task.topCameraPoint.X - dst2.Width / 2
        cameraYSlider.Value = task.sideCameraPoint.Y - dst2.Height / 2
        task.desc = "The global options for the side and top view.  See OptionCommon_Histogram to make settings permanent."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        task.maxX = task.maxZ * topFrustrumSlider.Value / 100 / 2
        task.maxY = task.maxZ * sideFrustrumSlider.Value / 100 / 2
        task.sideCameraPoint = New cv.Point(0, CInt(src.Height / 2 + cameraYSlider.Value))
        task.topCameraPoint = New cv.Point(CInt(src.Width / 2 + cameraXSlider.Value), CInt(src.Height))

        tView.RunClass(src)
        dst2 = tView.dst2
        dst3 = tView.dst3

        If standalone Then
            setTrueText("This algorithm was created to tune the frustrum and camera locations." + vbCrLf +
                          "Without these tuning parameters the side and top views would not be correct." + vbCrLf +
                          "To see how these adjustments work or to add a new camera, " + vbCrLf +
                          "use the Histogram_TopView2D or Histogram_SideView2D algorithms." + vbCrLf +
                          "For new cameras, make the adjustments needed, note the value, and update " + vbCrLf +
                          "the Select statement in the constructor for OptionsCommon_Histogram.")
        End If
    End Sub
End Class










Public Class Histogram_TopData : Inherits VBparent
    Public gCloud As New Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Dim kalman As New Kalman_Basics
    Public Sub New()
        task.desc = "Create a 2D top view for XZ histogram of depth in meters - NOTE: x and y scales differ!"
    End Sub

    Public Sub Run(src As cv.Mat) ' Rank = 1
        gCloud.RunClass(src)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, task.maxZ), New cv.Rangef(-task.maxX, task.maxX)}
        Dim histSize() = {dst3.Height, dst3.Width}
        cv.Cv2.CalcHist(New cv.Mat() {gCloud.dst2}, New Integer() {2, 0}, New cv.Mat, histOutput, 2, histSize, ranges)
        histOutput.Row(0).SetTo(0) ' this removes the samples for the areas with no depth.

        dst2 = histOutput.Flip(cv.FlipMode.X).Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary).Resize(dst2.Size)
        labels(2) = "Left x = " + Format(-task.maxX, "#0.00") + " Right X = " + Format(task.maxX, "#0.00") + " x and y scales differ!"
    End Sub
End Class









Public Class Histogram_SideData : Inherits VBparent
    Public gCloud As New Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Dim kalman As New Kalman_Basics
    Public Sub New()
        task.desc = "Create a 2D side view for ZY histogram of depth in meters - NOTE: x and y scales differ!"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        gCloud.RunClass(src)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.maxY, task.maxY), New cv.Rangef(0, task.maxZ)}
        Dim histSize() = {dst3.Height, dst3.Width}
        cv.Cv2.CalcHist(New cv.Mat() {gCloud.dst2}, New Integer() {1, 2}, New cv.Mat, histOutput, 2, histSize, ranges)
        histOutput.Col(0).SetTo(0) ' this removes the samples for the areas with no depth.

        dst2 = histOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary)
        labels(2) = "Top y = " + Format(-task.maxY, "#0.00") + " Bottom Y = " + Format(task.maxY, "#0.00") + " x and y scales differ!"
    End Sub
End Class








Public Class Histogram_BothViews : Inherits VBparent
    Dim sideview As New Histogram_SideData
    Dim topview As New Histogram_TopData
    Public Sub New()
        labels(2) = "Side View Histogram"
        labels(3) = "Top View Histogram"
        task.desc = "Show both the side and top histograms."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        sideview.RunClass(src)
        dst2 = sideview.dst2

        topview.RunClass(src)
        dst3 = topview.dst2
    End Sub
End Class







Public Class Histogram_Peaks : Inherits VBparent
    Dim hist As New Histogram_Basics
    Public Sub New()
        task.desc = "Find the peaks - columns taller that both neighbors - in the histogram"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        hist.RunClass(src)
        dst2 = hist.dst2

        Dim histogram = hist.histogram
        Dim peaks As New List(Of Integer)
        Dim hCount As New List(Of Single)
        For i = 0 To histogram.Rows - 1
            Dim prev = histogram.Get(Of Single)(Math.Max(i - 1, 0), 0)
            Dim curr = histogram.Get(Of Single)(i, 0)
            Dim nextVal = histogram.Get(Of Single)(Math.Min(i + 1, histogram.Rows - 1), 0)
            If i = 0 Then
                If prev > nextVal Then
                    peaks.Add(i)
                    hCount.Add(prev)
                End If
            Else
                If prev < curr And curr >= nextVal Then
                    peaks.Add(i)
                    hCount.Add(curr)
                End If
            End If
        Next

        Dim valleys As New List(Of Integer)
        Dim vCount As New List(Of Single)
        For i = 0 To peaks.Count - 2
            Dim minCount = hCount(i)
            Dim minIndex As Integer
            For j = peaks(i) To peaks(i + 1)
                Dim nextCount = histogram.Get(Of Single)(j, 0)
                If nextCount < minCount Then
                    minCount = nextCount
                    minIndex = j
                End If
            Next
            valleys.Add(minIndex)
            vCount.Add(minCount)
        Next

        histogram.MinMaxLoc(minVal, maxVal)
        Dim barWidth = dst2.Width / histogram.Rows
        For i = 0 To peaks.Count - 1
            Dim h = CInt(hCount(i) * dst2.Height / maxVal)
            cv.Cv2.Rectangle(dst2, New cv.Rect(peaks(i) * barWidth, dst2.Height - h, barWidth, h), cv.Scalar.Yellow, 2)
        Next
        For i = 0 To valleys.Count - 1
            Dim h = CInt(vCount(i) * dst2.Height / maxVal)
            cv.Cv2.Rectangle(dst2, New cv.Rect(valleys(i) * barWidth, dst2.Height - h, barWidth, h), cv.Scalar.Blue, 2)
        Next

        If valleys.Count > 0 Then
            Dim incr = 255 / histogram.Rows
            Dim startLut As Integer
            Dim endLut As Integer
            Dim myLut = New cv.Mat(256, 1, cv.MatType.CV_8U, 255)
            Dim lutIncr = 255 / valleys.Count
            For i = 0 To valleys.Count - 1
                endLut = valleys(i) * incr
                For j = startLut To endLut
                    myLut.Set(Of Byte)(j, 0, CInt((i + 1) * lutIncr))
                Next
                startLut = endLut + 1
            Next
            dst3 = src.LUT(myLut)
            labels(2) = "Grayscale image: " + CStr(peaks.Count) + " peaks (yellow), valley=blue"
        End If
    End Sub
End Class







Public Class Histogram_PeaksRGB : Inherits VBparent
    Public mats As New Mat_4Click
    Dim peaks As New Histogram_Peaks
    Public Sub New()
        task.desc = "Find the peaks and valleys for each of the RGB channels."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim split = src.Split()

        For i = 0 To 3 - 1
            peaks.RunClass(split(i))
            mats.mat(i) = peaks.dst3.Clone
        Next

        mats.RunClass(src)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class








Public Class Histogram_PeakEdges : Inherits VBparent
    Dim peaks As New Histogram_PeaksRGB
    Dim edges As New Edges_Sobel
    Public mats As New Mat_4to1
    Public Sub New()
        task.desc = "Find edges that are common to all channels - red, green and blue."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        peaks.RunClass(src)

        For i = 0 To 3 - 1
            edges.RunClass(peaks.mats.mat(i))
            mats.mat(i) = edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
            If i = 0 Then dst3 = mats.mat(i) Else cv.Cv2.BitwiseAnd(dst3, mats.mat(i), dst3)
        Next

        mats.RunClass(src)
        dst2 = mats.dst2
    End Sub
End Class








Public Class Histogram_PeakMax : Inherits VBparent
    Dim hist As New Histogram_Basics
    Public Sub New()
        task.desc = "Create a histogram and back project into the image the grayscale color with the highest occurance."
        labels(3) = "Grayscale Histogram"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        task.useKalman = False
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        hist.RunClass(src)
        dst3 = hist.dst2

        Dim minVal As Single, maxVal As Single
        Dim minIdx As cv.Point, maxIdx As cv.Point
        hist.histogram.MinMaxLoc(minVal, maxVal, minIdx, maxIdx)
        Dim barWidth = dst2.Width / task.histogramBins
        Dim barRange = 255 / task.histogramBins
        Dim histindex = maxIdx.Y
        Dim pixelMin = CInt((histindex) * barRange)
        Dim pixelMax = CInt((histindex + 1) * barRange)

        Dim mask = src.InRange(pixelMin, pixelMax).Threshold(1, 255, cv.ThresholdTypes.Binary)
        Dim tmp = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        src.CopyTo(tmp, mask)
        dst2 = tmp.Threshold(0, 255, cv.ThresholdTypes.Binary)

        labels(2) = "BackProjection of most frequent gray pixel"
        dst3.Rectangle(New cv.Rect(barWidth * histindex, 0, barWidth, dst2.Height), cv.Scalar.Yellow, 1)
    End Sub
End Class






Public Class Histogram_Depth : Inherits VBparent
    Public plotHist As New Plot_Histogram
    Public Sub New()
        task.desc = "Show depth data as a histogram."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        plotHist.minRange = 1
        plotHist.maxRange = task.maxDepth

        Dim histSize() = {task.histogramBins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}
        cv.Cv2.CalcHist(New cv.Mat() {task.depth32f}, New Integer() {0}, New cv.Mat, plotHist.hist, 1, histSize, ranges)

        If standalone Or task.intermediateName = caller Then
            plotHist.RunClass(src)
            dst2 = plotHist.dst2
        End If
        labels(2) = "Histogram Depth: " + Format(plotHist.minRange / 1000, "0.0") + "m to " + Format(plotHist.maxRange / 1000, "0.0") + " m"
    End Sub
End Class














Public Class Histogram_DepthEq : Inherits VBparent
    Dim hist As New Histogram_EqualizeGray
    Public Sub New()
        usingdst1 = True
        task.desc = "What impact does Histogram equalize have on a depth histogram."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst1 = task.depth32f.Normalize(0, 255, cv.NormTypes.MinMax)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
        hist.RunClass(dst2)
        dst2 = hist.dst2
        dst3 = hist.dst3
        labels = hist.labels
    End Sub
End Class
