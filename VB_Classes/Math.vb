Imports cv = OpenCvSharp
Imports System.Threading
' https://answers.opencvb.org/question/122331/how-to-subtract-a-constant-from-a-3-channel-mat/
Public Class Math_Subtract : Inherits TaskParent
    Dim options As New Options_Colors
    Public Sub New()
        desc = "Subtract a Mat using a scalar.  Set scalar to zero to see pixels saturate to zero."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Options.Run()

        Dim bgr = New cv.Scalar(options.blueS, options.greenS, options.redS)
        cv.Cv2.Subtract(bgr, src, dst2) ' or dst2 = bgr - src
        dst3 = src - bgr

        Dim scalar = "(" + CStr(bgr(0)) + "," + CStr(bgr(1)) + "," + CStr(bgr(2)) + ")"
        labels(2) = "Subtract Mat from scalar " + scalar
        labels(3) = "Subtract scalar " + scalar + " from Mat "
    End Sub
End Class
Module Math_Functions
    Public Sub CPP_Test()
        MsgBox("testing")
    End Sub
    Public Function computeMedian(src As cv.Mat, mask As cv.Mat, totalPixels As Integer, bins As Integer, rangeMin As Single, rangeMax As Single) As Double
        Dim hist As New cv.Mat()
        cv.Cv2.CalcHist({src}, {0}, mask, hist, 1, {bins}, {New cv.Rangef(rangeMin, rangeMax)})
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



Public Class Math_Median_CDF : Inherits TaskParent
    Public medianVal As Double
    Public rangeMin As Integer = 0
    Public rangeMax As Integer = 255
    Public Sub New()
        desc = "Compute the src image median"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        medianVal = computeMedian(src, New cv.Mat, src.Total, task.histogramBins, rangeMin, rangeMax)

        If standaloneTest() Then
            Dim mask = New cv.Mat
            mask = src.GreaterThan(medianVal)

            dst2.SetTo(0)
            src.CopyTo(dst2, mask)
            labels(2) = "Grayscale pixels > " + Format(medianVal, fmt1)

            dst3.SetTo(0)
            src.CopyTo(dst3, Not mask) ' show the other half.
            labels(3) = "Grayscale pixels < " + Format(medianVal, fmt1)
        End If
    End Sub
End Class





Public Class Math_DepthMeanStdev : Inherits TaskParent
    Dim minMax As New Depth_NotMissing
    Public Sub New()
        desc = "This algorithm shows that just using the max depth at each pixel does not improve quality of measurement"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        minMax.Run(src)
        Dim mean As Single = 0, stdev As Single = 0
        Dim mask = minMax.dst3 ' the mask for stable depth.
        dst3.SetTo(0)
        task.depthRGB.CopyTo(dst3, mask)
        If mask.Type <> cv.MatType.CV_8U Then mask = mask.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        cv.Cv2.MeanStdDev(task.pcSplit(2), mean, stdev, mask)
        labels(3) = "stablized depth mean=" + Format(mean, fmt1) + " stdev=" + Format(stdev, fmt1)

        dst2 = task.depthRGB
        cv.Cv2.MeanStdDev(task.pcSplit(2), mean, stdev)
        labels(2) = "raw depth mean=" + Format(mean, fmt1) + " stdev=" + Format(stdev, fmt1)
    End Sub
End Class








Public Class Math_RGBCorrelation : Inherits TaskParent
    Dim flow As New Font_FlowText
    Dim match As New Match_Basics
    Dim options As New Options_Features
    Public Sub New()
        flow.parentData = Me
        desc = "Compute the correlation coefficient of Red-Green and Red-Blue and Green-Blue"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim split = src.Split()
        match.template = split(0)
        match.Run(split(1))
        Dim blueGreenCorrelation = "Blue-Green " + match.labels(2)

        match.template = split(1)
        match.Run(split(1))
        Dim redGreenCorrelation = "Red-Green " + match.labels(2)

        match.template = split(2)
        match.Run(split(0))
        Dim redBlueCorrelation = "Red-Blue " + match.labels(2)

        flow.nextMsg = blueGreenCorrelation + " " + redGreenCorrelation + " " + redBlueCorrelation
        flow.Run(src)
        labels(2) = "Log of " + options.matchText
    End Sub
End Class









Public Class Math_StdevBoundary : Inherits TaskParent
    Dim stdev As New Math_Stdev
    Public Sub New()
        labels(2) = "Low stdev regions.  Gaps filled with OTSU results"
        labels(3) = "High stdev segments after the first pass"
        desc = "Explore how to get a better boundary on the low stdev mask"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        stdev.Run(src)
        dst2 = stdev.dst2
        stdev.saveFrame.CopyTo(dst3)

        Static stdevSlider =OptionParent.FindSlider("Stdev Threshold")
        Dim stdevThreshold = CSng(stdevSlider.Value)

        For Each roi In task.gridRects
            If roi.X + roi.Width < dst3.Width Then
                Dim m1 = dst2.Get(Of Byte)(roi.Y, roi.X)
                Dim m2 = dst2.Get(Of Byte)(roi.Y, roi.X + roi.Width)
                If m1 = 0 And m2 <> 0 Then
                    Dim meanScalar = cv.Cv2.Mean(dst3(roi))
                    dst3(roi).CopyTo(dst2(roi), dst3(roi).Threshold(meanScalar(0), 255, cv.ThresholdTypes.Otsu))
                End If
                If m1 > 0 And m2 = 0 Then
                    Dim newROI = New cv.Rect(roi.X + roi.Width, roi.Y, roi.Width, roi.Height)
                    If newROI.X + newROI.Width >= dst2.Width Then newROI.Width = dst2.Width - newROI.X - 1
                    If newROI.Y + newROI.Height >= dst2.Height Then newROI.Height = dst2.Height - newROI.Y - 1
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
                    If newROI.Y + newROI.Height >= dst3.Height Then newROI.Height = dst3.Height - newROI.Y
                    Dim meanScalar = cv.Cv2.Mean(dst3(newROI))
                    dst3(newROI).CopyTo(dst2(newROI), dst3(newROI).Threshold(meanScalar(0), 255, cv.ThresholdTypes.Otsu))
                End If
            End If
        Next
        dst3.SetTo(0, stdev.lowStdevMask)
    End Sub
End Class







Public Class Math_Template : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        labels = {"", "", "Input Template showing columns", "Input Template showing rows"}
        desc = "Build a template for use with computing the point cloud"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        For i = 0 To dst2.Width - 1
            dst2.Set(Of Single)(0, i, i) ' row with 0, 1, 2, 3....
        Next

        For i = 1 To dst2.Height - 1
            dst2.Row(0).CopyTo(dst2.Row(i)) ' duplicate above row
        Next

        For i = 0 To dst2.Height - 1
            dst3.Set(Of Single)(i, 0, i) ' col with 0, 1, 2, 3...
        Next

        For i = 1 To dst2.Width - 1
            dst3.Col(0).CopyTo(dst3.Col(i)) ' duplicate above col
        Next

        dst2 -= task.calibData.leftIntrinsics.ppx
        dst3 -= task.calibData.leftIntrinsics.ppy
    End Sub
End Class







Public Class Math_ImageAverage : Inherits TaskParent
    Dim images As New List(Of cv.Mat)
    Public Sub New()
        desc = "Create an image that is the mean of x number of previous images."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then images.Clear()
        dst3 = src.Clone
        If dst3.Type <> cv.MatType.CV_32F Then
            If dst3.Channels() <> 1 Then dst3.ConvertTo(dst3, cv.MatType.CV_32FC3) Else dst3.ConvertTo(dst3, cv.MatType.CV_32F)
        End If
        cv.Cv2.Multiply(dst3, cv.Scalar.All(1 / (images.Count + 1)), dst3)
        images.Add(dst3.Clone)
        If images.Count > task.frameHistoryCount Then images.RemoveAt(0)

        dst3.SetTo(0)
        For Each img In images
            dst3 += img
        Next
        If dst3.Type <> src.Type Then dst3.ConvertTo(dst2, src.Type) Else dst2 = dst3.Clone
        dst3 = Convert32f_To_8UC3(dst3)
        labels(2) = "Average image over previous " + CStr(task.frameHistoryCount) + " images"
    End Sub
End Class







Public Class Math_ImageMaskedAverage : Inherits TaskParent
    Dim images As New List(Of cv.Mat)
    Public Sub New()
        desc = "Mask off pixels where the difference is great and create an image that is the mean of x number of previous images."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then images.Clear()
        Dim nextImage As New cv.Mat
        If src.Type <> cv.MatType.CV_32F Then src.ConvertTo(nextImage, cv.MatType.CV_32F) Else nextImage = src
        cv.Cv2.Multiply(nextImage, cv.Scalar.All(1 / task.frameHistoryCount), nextImage)
        images.Add(nextImage.Clone())
        If images.Count > task.frameHistoryCount Then images.RemoveAt(0)

        nextImage.SetTo(0)
        For Each img In images
            nextImage += img
        Next
        If nextImage.Type <> src.Type Then nextImage.ConvertTo(dst2, src.Type) Else dst2 = nextImage
        labels(2) = "Average image over previous " + CStr(task.frameHistoryCount) + " images"
    End Sub
End Class













' https://stackoverflow.com/questions/7572640/how-do-i-know-if-two-vectors-are-near-parallel
Public Class Math_ParallelTest : Inherits TaskParent
    Public v1 = New cv.Point3f(1, 0, 0)
    Public v2 = New cv.Point3f(5, 0, 0)
    Public showWork As Boolean = True
    Public Sub New()
        labels = {"", "", "Parallel Test Output", ""}
        desc = "Test if 2 vectors are parallel"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        v1 *= 1 / Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y + v1.Z * v1.Z) ' normalize the input
        v2 *= 1 / Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y + v2.Z * v2.Z)
        Dim n1 = dotProduct3D(v1, v2)

        If showWork Then
            strOut = "Input: " + vbCrLf
            strOut += "normalized v1" + " = " + Format(v1.X, fmt3) + ", " + Format(v1.Y, fmt3) + ", " + Format(v1.Z, fmt3) + vbCrLf
            strOut += "normalized v2" + " = " + Format(v2.X, fmt3) + ", " + Format(v2.Y, fmt3) + ", " + Format(v2.Z, fmt3) + vbCrLf

            strOut += "Dot Product = " + Format(n1, fmt3) + " - if close to 1, the vectors are parallel" + vbCrLf
            SetTrueText(strOut, 2)
        End If
    End Sub
End Class






Public Class Math_Stdev : Inherits TaskParent
    Public highStdevMask As cv.Mat
    Public lowStdevMask As cv.Mat
    Public saveFrame As cv.Mat
    Dim options As New Options_Math
    Dim optionsMatch As New Options_Match
    Dim stdevSlider As New System.Windows.Forms.TrackBar
    Public Sub New()
        stdevSlider =OptionParent.FindSlider("Stdev Threshold")
        task.gOptions.GridSlider.Value = 16

        highStdevMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        lowStdevMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        desc = "Compute the standard deviation in each segment"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        Dim updateCount As Integer
        lowStdevMask.SetTo(0)
        highStdevMask.SetTo(0)

        dst2 = src.Clone
        If dst2.Channels() = 3 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cv.Mat = dst2.Clone()
        saveFrame = dst2.Clone
        Parallel.ForEach(task.gridRects,
        Sub(roi)
            Dim mean As Single, stdev As Single
            cv.Cv2.MeanStdDev(dst2(roi), mean, stdev)
            If stdev < optionsMatch.stdevThreshold Then
                Interlocked.Increment(updateCount)
                Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                If options.showMean Then SetTrueText(Format(mean, fmt0), pt, 2)
                If options.showStdev Then SetTrueText(Format(stdev, fmt2), pt, 2)
                lowStdevMask(roi).SetTo(255)
            Else
                highStdevMask(roi).SetTo(255)
                dst2(roi).SetTo(0)
            End If
        End Sub)
        If task.gOptions.getShowGrid() Then dst2.SetTo(255, task.gridMask)
        dst3.SetTo(0)
        saveFrame.CopyTo(dst3, highStdevMask)
        lastFrame = saveFrame
        Dim stdevPercent = " stdev " + Format(stdevSlider.Value, "0.0")
        labels(2) = CStr(updateCount) + " of " + CStr(task.gridRects.Count) + " segments with < " + stdevPercent
        labels(3) = CStr(task.gridRects.Count - updateCount) + " out of " + CStr(task.gridRects.Count) + " had stdev > " + Format(stdevSlider.Value, "0.0")
    End Sub
End Class