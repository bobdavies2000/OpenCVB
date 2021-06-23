Imports cv = OpenCvSharp
Imports System.Threading
' https://answers.opencv.org/question/122331/how-to-subtract-a-constant-from-a-3-channel-mat/
Public Class Math_Subtract : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Red", 0, 255, 255)
            sliders.setupTrackBar(1, "Green", 0, 255, 255)
            sliders.setupTrackBar(2, "Blue", 0, 255, 255)
        End If

        task.desc = "Subtract a Mat using a scalar.  Set scalar to zero to see pixels saturate to zero."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim bgr = New cv.Scalar(sliders.trackbar(2).Value, sliders.trackbar(1).Value, sliders.trackbar(0).Value)
        cv.Cv2.Subtract(bgr, src, dst2) ' or dst2 = bgr - src
        dst3 = src - bgr

        Dim scalar = "(" + CStr(bgr.Item(0)) + "," + CStr(bgr.Item(1)) + "," + CStr(bgr.Item(2)) + ")"
        label1 = "Subtract Mat from scalar " + scalar
        label2 = "Subtract scalar " + scalar + " from Mat "
    End Sub
End Class



Module Math_Functions
    Public Function computeMedian(src As cv.Mat, mask As cv.Mat, totalPixels As Integer, bins As integer, rangeMin As Single, rangeMax As Single) As Double
        Dim dimensions() = New Integer() {bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(rangeMin, rangeMax)}

        Dim hist As New cv.Mat()
        cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {0}, mask, hist, 1, dimensions, ranges)
        Dim halfPixels = totalPixels / 2

        Dim median As Double
        Dim cdfVal As Double = hist.Get(Of Single)(0)
        For i = 1 To bins - 1
            cdfVal += hist.Get(Of Single)(i)
            If cdfVal >= halfPixels Then
                median = (rangeMax - rangeMin) * i / bins
                Exit For
            End If
        Next
        Return median
    End Function
End Module



Public Class Math_Median_CDF : Inherits VBparent
    Public medianVal As Double
    Public rangeMin As Integer = 0
    Public rangeMax As Integer = 255
    Public Sub New()
        task.desc = "Compute the src image median"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        medianVal = computeMedian(src, New cv.Mat, src.Total, task.histogramBins, rangeMin, rangeMax)

        If standalone or task.intermediateName = caller Then
            Dim mask = New cv.Mat
            mask = src.GreaterThan(medianVal)

            dst2.SetTo(0)
            src.CopyTo(dst2, mask)
            label1 = "Grayscale pixels > " + Format(medianVal, "#0.0")

            cv.Cv2.BitwiseNot(mask, mask)
            dst3.SetTo(0)
            src.CopyTo(dst3, mask) ' show the other half.
            label2 = "Grayscale pixels < " + Format(medianVal, "#0.0")
        End If
    End Sub
End Class





Public Class Math_DepthMeanStdev : Inherits VBparent
    Dim minMax As New Depth_NotMissing
    Public Sub New()
        task.desc = "This algorithm shows that just using the max depth at each pixel does not improve quality of measurement"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        minMax.Run(src)
        Dim mean As Single = 0, stdev As Single = 0
        Dim mask = minMax.dst3 ' the mask for stable depth.
        dst3.SetTo(0)
        task.RGBDepth.CopyTo(dst3, mask)
        If mask.Type <> cv.MatType.CV_8U Then mask = mask.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.MeanStdDev(task.depth32f, mean, stdev, mask)
        label2 = "stablized depth mean=" + Format(mean, "#0.0") + " stdev=" + Format(stdev, "#0.0")

        dst2 = task.RGBDepth
        cv.Cv2.MeanStdDev(task.depth32f, mean, stdev)
        label1 = "raw depth mean=" + Format(mean, "#0.0") + " stdev=" + Format(stdev, "#0.0")
    End Sub
End Class





Public Class Math_RGBCorrelation : Inherits VBparent
    Dim flow As New Font_FlowText
    Dim match As New MatchTemplate_Basics
    Public Sub New()
        task.desc = "Compute the correlation coefficient of Red-Green and Red-Blue and Green-Blue"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim split = src.Split()
        match.searchArea = split(0)
        match.template = split(1)
        match.Run(src)
        Dim blueGreenCorrelation = "Blue-Green " + match.label1

        match.searchArea = split(2)
        match.template = split(1)
        match.Run(src)
        Dim redGreenCorrelation = "Red-Green " + match.label1

        match.searchArea = split(2)
        match.template = split(0)
        match.Run(src)
        Dim redBlueCorrelation = "Red-Blue " + match.label1

        flow.msgs.Add(blueGreenCorrelation + " " + redGreenCorrelation + " " + redBlueCorrelation)
        flow.Run(Nothing)
        label1 = "Log of " + match.matchText
    End Sub
End Class





Public Class Math_ImageAverage : Inherits VBparent
    Dim images As New List(Of cv.Mat)
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Average - number of input images", 1, 100, 10)
        End If
        task.desc = "Create an image that is the mean of x number of previous images."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static avgSlider = findSlider("Average - number of input images")
        Static saveImageCount = avgSlider.Value
        If avgSlider.Value <> saveImageCount Then
            saveImageCount = avgSlider.Value
            images.Clear()
        End If
        Dim nextImage As New cv.Mat
        If src.Type <> cv.MatType.CV_32F Then src.ConvertTo(nextImage, cv.MatType.CV_32F) Else nextImage = src
        cv.Cv2.Multiply(nextImage, cv.Scalar.All(1 / saveImageCount), nextImage)
        images.Add(nextImage.Clone())

        nextImage.SetTo(0)
        For Each img In images
            nextImage += img
        Next
        If images.Count > saveImageCount Then images.RemoveAt(0)
        If nextImage.Type <> src.Type Then nextImage.ConvertTo(dst2, src.Type) Else dst2 = nextImage
        label1 = "Average image over previous " + CStr(avgSlider.value) + " images"
    End Sub
End Class













Public Class Math_Stdev : Inherits VBparent
    Public grid As New Thread_Grid
    Public highStdevMask As cv.Mat
    Public lowStdevMask As cv.Mat
    Public saveFrame As cv.Mat
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Stdev Threshold", 0, 100, 10)
        End If
        findSlider("ThreadGrid Width").Value = 16
        findSlider("ThreadGrid Height").Value = 16

        If check.Setup(caller, 3) Then
            check.Box(0).Text = "Show mean"
            check.Box(1).Text = "Show Stdev"
            check.Box(2).Text = "Show Grid Mask"
        End If

        highStdevMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        lowStdevMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        task.desc = "Compute the standard deviation in each segment"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static stdevSlider = findSlider("Stdev Threshold")
        Static meanCheck = findCheckBox("Show mean")
        Static stdevCheck = findCheckBox("Show Stdev")
        Static gridCheck = findCheckBox("Show Grid Mask")
        Dim stdevThreshold = CSng(stdevSlider.Value)

        Dim updateCount As Integer
        lowStdevMask.SetTo(0)
        highStdevMask.SetTo(0)
        Dim fsize = task.fontSize / 3

        grid.Run(Nothing)

        dst2 = src.Clone
        If dst2.Channels = 3 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim showMean = meanCheck.checked
        Dim showStdev = stdevCheck.checked
        Static lastFrame As cv.Mat = dst2.Clone()
        saveFrame = dst2.Clone
        Parallel.ForEach(grid.roiList,
        Sub(roi)
            Dim mean As Single, stdev As Single
            cv.Cv2.MeanStdDev(dst2(roi), mean, stdev)
            If stdev < stdevThreshold Then
                Interlocked.Increment(updateCount)
                Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                If showMean Then cv.Cv2.PutText(dst2, Format(mean, "#0"), pt, task.font, fsize, cv.Scalar.White, task.lineWidth, task.lineType)
                If showStdev Then cv.Cv2.PutText(dst2, Format(stdev, "#0.00"), New cv.Point(pt.X, roi.Y + roi.Height - 4), task.font, fsize, cv.Scalar.White, task.lineWidth, task.lineType)
                lowStdevMask(roi).SetTo(255)
            Else
                highStdevMask(roi).SetTo(255)
                dst2(roi).SetTo(0)
            End If
        End Sub)
        If gridCheck.checked Then dst2.SetTo(255, grid.gridMask)
        dst3.SetTo(0)
        saveFrame.CopyTo(dst3, highStdevMask)
        lastFrame = saveFrame
        Dim stdevPercent = " stdev " + Format(stdevSlider.value, "0.0")
        label1 = CStr(updateCount) + " of " + CStr(grid.roiList.Count) + " segments with < " + stdevPercent
        label2 = CStr(grid.roiList.Count - updateCount) + " out of " + CStr(grid.roiList.Count) + " had stdev > " + Format(stdevSlider.value, "0.0")
    End Sub
End Class








Public Class Math_StdevBoundary : Inherits VBparent
    Dim stdev As New Math_Stdev
    Public Sub New()
        label1 = "Low stdev regions.  Gaps filled with OTSU results"
        label2 = "High stdev segments after the first pass"
        task.desc = "Explore how to get a better boundary on the low stdev mask"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1

        stdev.Run(src)
        dst2 = stdev.dst2
        stdev.saveFrame.CopyTo(dst3)

        Static stdevSlider = findSlider("Stdev Threshold")
        Dim stdevThreshold = CSng(stdevSlider.Value)

        'Parallel.ForEach(stdev.grid.roiList, ' surprisingly it runs faster in serial mode...
        'Sub(roi)
        For Each roi In stdev.grid.roiList
            If roi.X + roi.Width < dst3.Width Then
                Dim m1 = dst2.Get(Of Byte)(roi.Y, roi.X)
                Dim m2 = dst2.Get(Of Byte)(roi.Y, roi.X + roi.Width)
                If m1 = 0 And m2 <> 0 Then
                    Dim meanScalar = cv.Cv2.Mean(dst3(roi))
                    dst3(roi).CopyTo(dst2(roi), dst3(roi).Threshold(meanScalar(0), 255, cv.ThresholdTypes.Otsu))
                End If
                If m1 > 0 And m2 = 0 Then
                    Dim newROI = New cv.Rect(roi.X + roi.Width, roi.Y, roi.Width, roi.Height)
                    Dim meanScalar = cv.Cv2.Mean(dst3(newROI))
                    dst3(newROI).CopyTo(dst2(newROI), dst3(newROI).Threshold(meanScalar(0), 255, cv.ThresholdTypes.Otsu))
                End If
            End If
            If roi.Y + roi.Height < dst3.Height Then
                Dim m1 = dst2.Get(Of Byte)(roi.Y, roi.X)
                Dim m2 = dst2.Get(Of Byte)(roi.Y + roi.Height, roi.X)
                If m1 = 0 And m2 <> 0 Then
                    Dim meanScalar = cv.Cv2.Mean(dst3(roi))
                    dst3(roi).CopyTo(dst2(roi), dst3(roi).Threshold(meanScalar(0), 255, cv.ThresholdTypes.Otsu))
                End If
                If m1 > 0 And m2 = 0 Then
                    Dim newROI = New cv.Rect(roi.X, roi.Y + roi.Height, roi.Width, roi.Height)
                    Dim meanScalar = cv.Cv2.Mean(dst3(newROI))
                    dst3(newROI).CopyTo(dst2(newROI), dst3(newROI).Threshold(meanScalar(0), 255, cv.ThresholdTypes.Otsu))
                End If
            End If
            'End Sub)
        Next
        dst3.SetTo(0, stdev.lowStdevMask)
    End Sub
End Class
