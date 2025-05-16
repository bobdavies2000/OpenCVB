Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://stackoverflow.com/questions/19093728/rotate-image-around-x-y-z-axis-in-opencv
' https://stackoverflow.com/questions/7019407/translating-and-rotating-an-image-in-3d-using-opencv
Public Class Depth_Basics : Inherits TaskParent
    Public Sub New()
        desc = "Colorize the depth data into task.depthRGB"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.pcSplit(2)

        task.pcSplit(2) = task.pcSplit(2).Threshold(task.MaxZmeters, task.MaxZmeters, cv.ThresholdTypes.Trunc)
        If task.firstPass Then
            task.maxDepthMask = task.pcSplit(2).ConvertScaleAbs().InRange(task.MaxZmeters, task.MaxZmeters)
            task.maxDepthMask.SetTo(0)
        End If
        If standalone Then dst3 = task.maxDepthMask
        SetTrueText(task.gmat.strOut, 3)
    End Sub
End Class







Public Class Depth_Display : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"task.pcSplit(2)", "task.pointcloud", "task.depthMask", "task.noDepthMask"}
        desc = "Display the task.pcSplit(2), task.pointcloud, task.depthMask, and task.noDepthMask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst0 = task.pcSplit(2)
        dst1 = task.pointCloud
        dst2 = task.depthMask
        dst3 = task.noDepthMask
    End Sub
End Class







Public Class Depth_FirstLastDistance : Inherits TaskParent
    Public Sub New()
        desc = "Monitor the first and last depth distances"
    End Sub
    Private Sub identifyMinMax(pt As cv.Point, text As String)
        DrawCircle(dst2, pt, task.DotSize, task.highlight)
        SetTrueText(text, pt, 2)

        DrawCircle(dst3, pt, task.DotSize, task.highlight)
        SetTrueText(text, pt, 3)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim mm As mmData = GetMinMax(task.pcSplit(2), task.depthMask)
        task.depthRGB.CopyTo(dst2)

        If task.heartBeat Then dst3.SetTo(0)
        labels(2) = "Min Depth " + Format(mm.minVal, fmt1) + "m"
        identifyMinMax(mm.minLoc, labels(2))

        labels(3) = "Max Depth " + Format(mm.maxVal, fmt1) + "m"
        identifyMinMax(mm.maxLoc, labels(3))
    End Sub
End Class







Public Class Depth_HolesRect : Inherits TaskParent
    Dim shadow As New Depth_Holes
    Public Sub New()
        labels(2) = "The 10 largest contours in the depth holes."
        desc = "Identify the minimum rectangles of contours of the depth shadow"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        shadow.Run(src)

        Dim contours As cv.Point()()
        If shadow.dst3.Channels() = 3 Then shadow.dst3 = shadow.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        contours = cv.Cv2.FindContoursAsArray(shadow.dst3, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        Dim sortContours As New SortedList(Of Integer, List(Of cv.Point))(New compareAllowIdenticalIntegerInverted)
        For Each c In contours
            sortContours.Add(c.Length, c.ToList)
        Next
        dst3.SetTo(0)
        For i = 0 To Math.Min(sortContours.Count, 10) - 1
            Dim contour = sortContours.ElementAt(i).Value
            Dim minRect = cv.Cv2.MinAreaRect(contour)
            Dim nextColor = New cv.Scalar(task.vecColors(i Mod 256)(0), task.vecColors(i Mod 256)(1), task.vecColors(i Mod 256)(2))
            DrawRotatedRect(minRect, dst2, nextColor)
            DrawContour(dst3, contour.ToList, white, task.lineWidth)
        Next
        cv.Cv2.AddWeighted(dst2, 0.5, task.depthRGB, 0.5, 0, dst2)
    End Sub
End Class








Public Class Depth_MeanStdev_MT : Inherits TaskParent
    Dim meanSeries As New cv.Mat
    Dim maxMeanVal As Single, maxStdevVal As Single
    Public Sub New()
        If standalone Then task.gOptions.GridSlider.Value = task.gOptions.GridSlider.Maximum
        dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Rows, dst3.Cols, cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Collect a time series of depth mean and stdev to highlight where depth is unstable."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then meanSeries = New cv.Mat(task.gridRects.Count, task.frameHistoryCount, cv.MatType.CV_32F, cv.Scalar.All(0))

        Dim index = task.frameCount Mod task.frameHistoryCount
        Dim meanValues(task.gridRects.Count - 1) As Single
        Dim stdValues(task.gridRects.Count - 1) As Single
        Parallel.For(0, task.gridRects.Count,
        Sub(i)
            Dim roi = task.gridRects(i)
            Dim mean As cv.Scalar, stdev As cv.Scalar
            cv.Cv2.MeanStdDev(task.pcSplit(2)(roi), mean, stdev, task.depthMask(roi))
            meanSeries.Set(Of Single)(i, index, mean)
            If task.frameCount >= task.frameHistoryCount - 1 Then
                cv.Cv2.MeanStdDev(meanSeries.Row(i), mean, stdev)
                meanValues(i) = mean
                stdValues(i) = stdev
            End If
        End Sub)

        If task.frameCount >= task.frameHistoryCount Then
            Dim means As cv.Mat = cv.Mat.FromPixelData(task.gridRects.Count, 1, cv.MatType.CV_32F, meanValues.ToArray)
            Dim stdevs As cv.Mat = cv.Mat.FromPixelData(task.gridRects.Count, 1, cv.MatType.CV_32F, stdValues.ToArray)
            Dim meanmask = means.Threshold(1, task.MaxZmeters, cv.ThresholdTypes.Binary).ConvertScaleAbs()
            Dim mm As mmData = GetMinMax(means, meanmask)
            Dim stdMask = stdevs.Threshold(0.001, task.MaxZmeters, cv.ThresholdTypes.Binary).ConvertScaleAbs() ' volatile region is x cm stdev.
            Dim mmStd = GetMinMax(stdevs, stdMask)

            maxMeanVal = Math.Max(maxMeanVal, mm.maxVal)
            maxStdevVal = Math.Max(maxStdevVal, mmStd.maxVal)

            Parallel.For(0, task.gridRects.Count,
            Sub(i)
                Dim roi = task.gridRects(i)
                dst3(roi).SetTo(255 * stdevs.Get(Of Single)(i, 0) / maxStdevVal)
                dst3(roi).SetTo(0, task.noDepthMask(roi))

                dst2(roi).SetTo(255 * means.Get(Of Single)(i, 0) / maxMeanVal)
                dst2(roi).SetTo(0, task.noDepthMask(roi))
            End Sub)

            If task.heartBeat Then
                maxMeanVal = 0
                maxStdevVal = 0
            End If

            If standaloneTest() Then
                For i = 0 To task.gridRects.Count - 1
                    Dim roi = task.gridRects(i)
                    SetTrueText(Format(meanValues(i), fmt3) + vbCrLf +
                                Format(stdValues(i), fmt3), roi.Location, 3)
                Next
            End If

            dst3 = dst3 Or task.gridMask
            labels(2) = "The regions where the depth is volatile are brighter.  Stdev min " + Format(mmStd.minVal, fmt3) + " Stdev Max " + Format(mmStd.maxVal, fmt3)
            labels(3) = "Mean/stdev for each ROI: Min " + Format(mm.minVal, fmt3) + " Max " + Format(mm.maxVal, fmt3)
        End If
    End Sub
End Class





Public Class Depth_MeanStdevPlot : Inherits TaskParent
    Dim plot1 As New Plot_OverTimeSingle
    Dim plot2 As New Plot_OverTimeSingle
    Public Sub New()
        desc = "Plot the mean and stdev of the depth image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim mean As cv.Scalar, stdev As cv.Scalar
        Dim depthMask As cv.Mat = task.depthMask
        cv.Cv2.MeanStdDev(task.pcSplit(2), mean, stdev, depthMask)

        plot1.plotData = mean(0)
        plot1.Run(src)
        dst2 = plot1.dst2

        plot2.plotData = stdev(0)
        plot2.Run(src)
        dst3 = plot2.dst2

        labels(2) = "Plot of mean depth = " + Format(mean(0), fmt1) + " min = " + Format(plot1.min, fmt2) + " max = " + Format(plot1.max, fmt2)
        labels(3) = "Plot of depth stdev = " + Format(stdev(0), fmt1) + " min = " + Format(plot2.min, fmt2) + " max = " + Format(plot2.max, fmt2)
    End Sub
End Class




Public Class Depth_Uncertainty : Inherits TaskParent
    Dim retina As New Retina_Basics_CPP
    Dim options As New Options_Uncertainty
    Public Sub New()
        labels(3) = "Mask of areas with stable depth"
        desc = "Use the bio-inspired retina algorithm to determine depth uncertainty."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        retina.Run(task.depthRGB)
        dst2 = retina.dst2
        dst3 = retina.dst3.Threshold(options.uncertaintyThreshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







Public Class Depth_LocalMinMax_MT : Inherits TaskParent
    Public minPoint(0) As cv.Point2f
    Public maxPoint(0) As cv.Point2f
    Public Sub New()
        labels = {"", "", "Highlight (usually yellow) is min distance, red is max distance",
                  "Highlight is min, red is max.  Lines would indicate planes are present."}
        desc = "Find min and max depth in each segment."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            src.CopyTo(dst2)
            dst2.SetTo(white, task.gridMask)
        End If

        If minPoint.Length <> task.gridRects.Count Then
            ReDim minPoint(task.gridRects.Count - 1)
            ReDim maxPoint(task.gridRects.Count - 1)
        End If

        If task.heartBeat Then dst3.SetTo(0)
        Parallel.For(0, task.gridRects.Count,
        Sub(i)
            Dim roi = task.gridRects(i)
            Dim mm As mmData = GetMinMax(task.pcSplit(2)(roi), task.depthMask(roi))
            If mm.minLoc.X < 0 Or mm.minLoc.Y < 0 Then mm.minLoc = New cv.Point2f(0, 0)
            minPoint(i) = New cv.Point(mm.minLoc.X + roi.X, mm.minLoc.Y + roi.Y)
            maxPoint(i) = New cv.Point(mm.maxLoc.X + roi.X, mm.maxLoc.Y + roi.Y)

            DrawCircle(dst2(roi), mm.minLoc, task.DotSize, task.highlight)
            DrawCircle(dst2(roi), mm.maxLoc, task.DotSize, cv.Scalar.Red)

            Dim p1 = New cv.Point(mm.minLoc.X + roi.X, mm.minLoc.Y + roi.Y)
            Dim p2 = New cv.Point(mm.maxLoc.X + roi.X, mm.maxLoc.Y + roi.Y)
            DrawCircle(dst3, p1, task.DotSize, task.highlight)
            DrawCircle(dst3, p2, task.DotSize, cv.Scalar.Red)
        End Sub)
    End Sub
End Class






Public Class Depth_ColorMap : Inherits TaskParent
    Dim options As New Options_DepthColor
    Public Sub New()
        desc = "Display the depth as a color map"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        cv.Cv2.ConvertScaleAbs(task.pcSplit(2) * 1000, dst1, options.alpha, options.beta)
        dst1 += 1
        dst2 = ShowPalette(dst1)
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class






Public Class Depth_NotMissing : Inherits TaskParent
    Public bgSub As New BGSubtract_Basics
    Public Sub New()
        labels(3) = "Stable (non-zero) Depth"
        desc = "Collect X frames, compute stable depth using the BGR and Depth image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then src = task.depthRGB
        bgSub.Run(src)
        dst2 = bgSub.dst2
        dst3 = Not bgSub.dst2
        labels(2) = "Unstable Depth" + " using " + bgSub.options.methodDesc + " method"
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class








Public Class Depth_Median : Inherits TaskParent
    Dim median As New Math_Median_CDF
    Public Sub New()
        median.rangeMax = task.MaxZmeters
        median.rangeMin = 0
        desc = "Divide the depth image ahead and behind the median."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        median.Run(task.pcSplit(2))

        Dim mask As cv.Mat
        mask = task.pcSplit(2).LessThan(median.medianVal)
        task.pcSplit(2).CopyTo(dst2, mask)

        dst2.SetTo(0, task.noDepthMask)

        labels(2) = "Median Depth < " + Format(median.medianVal, fmt1)

        dst3.SetTo(0)
        task.depthRGB.CopyTo(dst3, Not mask)
        dst3.SetTo(0, task.noDepthMask)
        labels(3) = "Median Depth > " + Format(median.medianVal, fmt1)
    End Sub
End Class






Public Class Depth_SmoothingMat : Inherits TaskParent
    Dim options As New Options_Depth
    Public Sub New()
        labels(3) = "Depth pixels after smoothing"
        desc = "Use depth rate of change to smooth the depth values in close range"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Static lastDepth = task.pcSplit(2)

        If standaloneTest() Then src = task.pcSplit(2)
        Dim rect = If(task.drawRect.Width <> 0, task.drawRect, New cv.Rect(0, 0, src.Width, src.Height))

        cv.Cv2.Subtract(lastDepth, task.pcSplit(2), dst2)

        dst2 = dst2.Threshold(options.mmThreshold, 0, cv.ThresholdTypes.TozeroInv).Threshold(-options.mmThreshold, 0, cv.ThresholdTypes.Tozero)
        cv.Cv2.Add(task.pcSplit(2), dst2, dst3)
        lastDepth = task.pcSplit(2)

        labels(2) = "Smoothing Mat: range to " + CStr(task.MaxZmeters) + " meters"
    End Sub
End Class





Public Class Depth_Smoothing : Inherits TaskParent
    Dim smooth As New Depth_SmoothingMat
    Dim reduction As New Reduction_Basics
    Public reducedDepth As New cv.Mat
    Public mats As New Mat_4to1
    Public colorize As New Depth_ColorMap
    Public Sub New()
        task.redOptions.BitwiseReduction.Checked = True
        labels(3) = "Mask of depth that is smooth"
        desc = "This attempt to get the depth data to 'calm' down is not working well enough to be useful - needs more work"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        smooth.Run(task.pcSplit(2))
        Dim input = smooth.dst2.Normalize(0, 255, cv.NormTypes.MinMax)
        input.ConvertTo(mats.mat(0), cv.MatType.CV_8UC1)
        Dim tmp As New cv.Mat
        cv.Cv2.Add(smooth.dst3, smooth.dst2, tmp)
        mats.mat(1) = tmp.Normalize(0, 255, cv.NormTypes.MinMax).ConvertScaleAbs()

        reduction.Run(task.pcSplit(2))
        reduction.dst2.ConvertTo(reducedDepth, cv.MatType.CV_32F)
        colorize.Run(reducedDepth)
        dst2 = colorize.dst2
        mats.Run(emptyMat)
        dst3 = mats.dst2
        labels(2) = smooth.labels(2)
    End Sub
End Class







Public Class Depth_HolesOverTime : Inherits TaskParent
    Dim images As New List(Of cv.Mat)
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst1 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(3) = "Latest hole mask"
        desc = "Integrate memory holes over time to identify unstable depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            images.Clear()
            dst0.SetTo(0)
        End If

        dst3 = task.noDepthMask
        dst1 = dst3.Threshold(0, 1, cv.ThresholdTypes.Binary)
        images.Add(dst1)

        dst0 += dst1
        dst2 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)

        labels(2) = "Depth holes integrated over the past " + CStr(images.Count) + " images"
        If images.Count >= task.frameHistoryCount Then
            dst0 -= images(0)
            images.RemoveAt(0)
        End If
    End Sub
End Class








Public Class Depth_Holes : Inherits TaskParent
    Dim element As New cv.Mat
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Amount of dilation of borderMask", 1, 10, 1)
            sliders.setupTrackBar("Amount of dilation of holeMask", 0, 10, 0)
        End If
        labels(3) = "Shadow Edges (use sliders to expand)"
        element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(5, 5))
        desc = "Identify holes in the depth image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static borderSlider = optiBase.FindSlider("Amount of dilation of borderMask")
        Static holeSlider = optiBase.FindSlider("Amount of dilation of holeMask")
        dst2 = task.pcSplit(2).Threshold(0.01, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)
        dst2 = dst2.Dilate(element, Nothing, holeSlider.Value)
        dst3 = dst2.Dilate(element, Nothing, borderSlider.Value)
        dst3 = dst3 Xor dst2
        If standaloneTest() Then task.depthRGB.CopyTo(dst3, dst3)
    End Sub
End Class










Public Class Depth_Dilate : Inherits TaskParent
    Dim dilate As New Dilate_Basics
    Public Sub New()
        desc = "Dilate the depth data to fill holes."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dilate.Run(task.pcSplit(2))
        dst2 = dilate.dst2
    End Sub
End Class








Public Class Depth_ForegroundHead : Inherits TaskParent
    Dim fgnd As New Depth_ForegroundBlob
    Public trustedRect As cv.Rect
    Public trustworthy As Boolean
    Public Sub New()
        task.kalman = New Kalman_Basics
        labels(2) = "Blue is current, red is kalman, green is trusted"
        desc = "Use Depth_ForeGround to find the foreground blob.  Then find the probable head of the person in front of the camera."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fgnd.Run(src)

        trustworthy = False
        If fgnd.dst2.CountNonZero And fgnd.maxIndex >= 0 Then
            Dim rectSize = 50
            If src.Width > 1000 Then rectSize = 250
            Dim xx = fgnd.blobLocation(fgnd.maxIndex).X - rectSize / 2
            Dim yy = fgnd.blobLocation(fgnd.maxIndex).Y
            If xx < 0 Then xx = 0
            If xx + rectSize / 2 > src.Width Then xx = src.Width - rectSize
            dst2 = fgnd.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            task.kalman.kInput = {xx, yy, rectSize, rectSize}
            task.kalman.Run(emptyMat)
            Dim nextRect = New cv.Rect(xx, yy, rectSize, rectSize)
            Dim kRect = New cv.Rect(task.kalman.kOutput(0), task.kalman.kOutput(1), task.kalman.kOutput(2), task.kalman.kOutput(3))
            dst2.Rectangle(kRect, cv.Scalar.Red, 2)
            dst2.Rectangle(nextRect, cv.Scalar.Blue, 2)
            If Math.Abs(kRect.X - nextRect.X) < rectSize / 4 And Math.Abs(kRect.Y - nextRect.Y) < rectSize / 4 Then
                trustedRect = ValidateRect(kRect)
                trustworthy = True
                dst2.Rectangle(trustedRect, cv.Scalar.Green, 5)
            End If
        End If
    End Sub
End Class









Public Class Depth_RGBShadow : Inherits TaskParent
    Public Sub New()
        desc = "Merge the BGR and Depth Shadow"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class







Public Class Depth_BGSubtract : Inherits TaskParent
    Dim bgSub As New BGSubtract_Basics
    Public Sub New()
        labels = {"", "", "Latest task.noDepthMask", "BGSubtract output for the task.noDepthMask"}
        desc = "Create a mask for the missing depth across multiple frame"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.noDepthMask

        bgSub.Run(dst2)
        dst3 = bgSub.dst2
    End Sub
End Class










Public Class Depth_MaxMask : Inherits TaskParent
    Dim contour As New Contour_Basics
    Public Sub New()
        labels = {"", "", "Depth that is too far", "Contour of depth that is too far..."}
        desc = "Display the task.maxDepthMask and its contour containing depth that is greater than maxdepth (global setting)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src

        task.maxDepthMask = task.pcSplit(2).InRange(task.MaxZmeters, task.MaxZmeters).ConvertScaleAbs()
        dst2.SetTo(white, task.maxDepthMask)
        contour.Run(task.maxDepthMask)
        dst3.SetTo(0)
        For Each c In contour.contourList
            Dim hull = cv.Cv2.ConvexHull(c, True).ToList
            DrawContour(dst3, hull, white, -1)
        Next
    End Sub
End Class







Public Class Depth_ForegroundOverTime : Inherits TaskParent
    Dim options As New Options_ForeGround
    Dim fore As New Depth_Foreground
    Dim contours As New Contour_Largest
    Dim lastFrames As New List(Of cv.Mat)
    Public Sub New()
        labels = {"", "", "Foreground objects", "Edges for the Foreground Objects"}
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        task.frameHistoryCount = 5
        desc = "Create a fused foreground mask over x number of frames (task.frameHistoryCount)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.optionsChanged Then lastFrames.Clear()

        fore.Run(src)
        lastFrames.Add(fore.dst3)
        dst2.SetTo(0)
        For Each m In lastFrames
            dst2 += m
        Next
        If lastFrames.Count >= task.frameHistoryCount Then lastFrames.RemoveAt(0)

        contours.Run(dst2)
        dst2.SetTo(0)
        dst3.SetTo(0)
        For Each ctr In contours.allContours
            If ctr.Length >= options.minSizeContour Then
                DrawContour(dst2, ctr.ToList, white, -1)
                DrawContour(dst3, ctr.ToList, white)
            End If
        Next
    End Sub
End Class





Public Class Depth_ForegroundBlob : Inherits TaskParent
    Dim options As New Options_ForeGround
    Public blobLocation As New List(Of cv.Point)
    Public maxIndex As Integer
    Public Sub New()
        labels(2) = "Mask for the largest foreground blob"
        desc = "Use InRange to define foreground and find the largest blob in the foreground"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        cv.Cv2.InRange(task.pcSplit(2), 0.01, options.maxForegroundDepthInMeters, dst2)
        dst3 = dst2.Clone

        ' find the largest blob and use that to define the foreground object.
        Dim blobSize As New List(Of Integer)
        blobLocation.Clear()

        For y = 0 To dst2.Height - 1
            For x = 0 To dst2.Width - 1
                Dim nextByte = dst2.Get(Of Byte)(y, x)
                If nextByte <> 0 Then
                    Dim count = dst2.FloodFill(New cv.Point(x, y), 0)
                    If count > 10 Then
                        blobSize.Add(count)
                        blobLocation.Add(New cv.Point(x, y))
                    End If
                End If
            Next
        Next
        If blobSize.Count > 0 Then
            Dim maxBlob As Integer
            maxIndex = -1
            For i = 0 To blobSize.Count - 1
                If maxBlob < blobSize(i) Then
                    maxBlob = blobSize(i)
                    maxIndex = i
                End If
            Next
            dst3.FloodFill(blobLocation(maxIndex), 250)
            cv.Cv2.InRange(dst3, 250, 250, dst2)
            dst2.SetTo(0, task.noDepthMask)
            labels(3) = "Mask of all depth pixels < " + Format(options.maxForegroundDepthInMeters, "0.0") + "m"
        End If
    End Sub
End Class








Public Class Depth_Foreground : Inherits TaskParent
    Dim options As New Options_ForeGround
    Dim contours As New Contour_Largest
    Public Sub New()
        labels(2) = "Foreground objects"
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Create a mask for the objects in the foreground"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim dst1 = task.pcSplit(2).Threshold(options.maxForegroundDepthInMeters, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        dst1.SetTo(0, task.noDepthMask)

        contours.Run(dst1)
        dst2.SetTo(0)
        dst3.SetTo(0)
        For Each ctr In contours.allContours
            If ctr.Length >= options.minSizeContour Then
                DrawContour(dst2, ctr.ToList, white, -1)
                DrawContour(dst3, ctr.ToList, white)
            End If
        Next
    End Sub
End Class










Public Class Depth_Grid : Inherits TaskParent
    Public Sub New()
        task.gOptions.GridSlider.Value = 4
        labels = {"", "", "White regions below are likely depth edges where depth changes rapidly", "Depth 32f display"}
        desc = "Find boundaries in depth to separate featureless regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = task.pcSplit(2)
        dst2 = task.gridMask.Clone
        For Each roi In task.gridRects
            Dim mm As mmData = GetMinMax(dst3(roi))
            If Math.Abs(mm.minVal - mm.maxVal) > 0.1 Then dst2(roi).SetTo(cv.Scalar.White)
        Next
    End Sub
End Class









Public Class Depth_InRange : Inherits TaskParent
    Dim options As New Options_ForeGround
    Dim contours As New Contour_Largest
    Public classCount As Integer = 1
    Public Sub New()
        labels = {"", "", "Looks empty! But the values are there - 0 to classcount.  Run standaloneTest() to see the palette output for this", "Edges between the depth regions."}
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst3 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U)
        desc = "Create the selected number of depth ranges "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim regMats As New List(Of cv.Mat)
        For i = 0 To options.numberOfRegions - 1
            Dim upperBound = (i + 1) * options.depthPerRegion
            If i = options.numberOfRegions - 1 Then upperBound = 1000
            regMats.Add(task.pcSplit(2).InRange(i * options.depthPerRegion, upperBound))
            If i = 0 Then regMats(0).SetTo(0, task.noDepthMask)
        Next

        dst2 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3.SetTo(0)
        classCount = 1
        For i = 0 To regMats.Count - 1
            contours.Run(regMats(i))
            For Each ctr In contours.allContours
                If ctr.Length >= options.minSizeContour Then
                    DrawContour(dst2, ctr.ToList, classCount, -1)
                    classCount += 1
                    DrawContour(dst3, ctr.ToList, white)
                End If
            Next
        Next

        dst0 = src.Clone
        dst0.SetTo(white, dst3)

        If standaloneTest() Then dst2 = ShowPalette(dst2)
        If task.heartBeat Then labels(2) = Format(classCount, "000") + " regions were found"
    End Sub
End Class


















Public Class Depth_Regions : Inherits TaskParent
    Public classCount As Integer = 5
    Public Sub New()
        desc = "Separate the scene into a specified number of regions by depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.pcSplit(2).Threshold(task.MaxZmeters, task.MaxZmeters, cv.ThresholdTypes.Binary)
        dst0 = (task.pcSplit(2) / task.MaxZmeters) * 255 / classCount
        dst0.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2.SetTo(0, task.noDepthMask)

        If standaloneTest() Then dst3 = ShowPalette(dst2)
        labels(2) = CStr(classCount) + " regions defined in the depth data"
    End Sub
End Class









Public Class Depth_Colorizer_VB : Inherits TaskParent
    Dim nearColor = cv.Scalar.Yellow
    Dim farColor = cv.Scalar.Blue
    Public Sub New()
        desc = "Colorize the depth based on the near and far colors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        dst2.SetTo(0)
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim pixel = src.Get(Of Single)(y, x)
                If pixel > 0 And pixel <= task.MaxZmeters Then
                    Dim t = pixel / task.MaxZmeters
                    Dim color = New cv.Vec3b(((1 - t) * nearColor(0) + t * farColor(0)),
                                              ((1 - t) * nearColor(1) + t * farColor(1)),
                                              ((1 - t) * nearColor(2) + t * farColor(2)))
                    dst2.Set(Of cv.Vec3b)(y, x, color)
                End If
            Next
        Next
    End Sub
End Class







Public Class Depth_PunchIncreasing : Inherits TaskParent
    Public depth As New Depth_PunchDecreasing
    Public Sub New()
        depth.Increasing = True
        desc = "Identify where depth is increasing - retreating from the camera."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        depth.Run(src)
        dst2 = depth.dst2
    End Sub
End Class








Public Class Depth_PunchDecreasing : Inherits TaskParent
    Public Increasing As Boolean
    Dim fore As New Depth_Foreground
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold in millimeters", 0, 1000, 8)
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        desc = "Identify where depth is decreasing - coming toward the camera."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fore.Run(src)
        dst1.SetTo(0)
        task.pcSplit(2).CopyTo(dst1, fore.dst2)

        Static lastDepth = dst1
        Static mmSlider = optiBase.FindSlider("Threshold in millimeters")
        Dim mmThreshold = mmSlider.Value / 1000
        If Increasing Then
            cv.Cv2.Subtract(dst1, lastDepth, dst2)
        Else
            cv.Cv2.Subtract(lastDepth, dst1, dst2)
        End If
        dst2 = dst2.Threshold(mmThreshold, 0, cv.ThresholdTypes.Tozero).Threshold(0, 255, cv.ThresholdTypes.Binary)
        lastDepth = dst1.Clone
    End Sub
End Class






Public Class Depth_PunchBlob : Inherits TaskParent
    Dim depthDec As New Depth_PunchDecreasing
    Dim depthInc As New Depth_PunchDecreasing
    Dim contours As New Contour_Basics
    Dim lastContoursCount As Integer
    Dim punchCount As Integer
    Dim showMessage As Integer
    Dim showWarningInfo As Integer
    Public Sub New()
        desc = "Identify the punch with a rectangle around the largest blob"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        depthInc.Run(src)
        dst2 = depthInc.dst2

        Dim mm As mmData = GetMinMax(dst2)
        dst2.ConvertTo(dst1, cv.MatType.CV_8U)
        contours.Run(dst1)
        dst3 = contours.dst3

        If contours.contourList.Count > 0 Then showMessage = 30

        If showMessage = 30 And lastContoursCount = 0 Then punchCount += 1
        lastContoursCount = contours.contourList.Count
        labels(3) = CStr(punchCount) + " Punches Thrown"

        If showMessage > 0 Then
            SetTrueText("Punched!!!", New cv.Point(10, 100), 3)
            showMessage -= 1
        End If

        If contours.contourList.Count > 3 Then showWarningInfo = 100

        If showWarningInfo Then
            showWarningInfo -= 1
            SetTrueText("Too many contours!  Reduce the Max Depth.", New cv.Point(10, 130), 3)
        End If
    End Sub
End Class








Public Class Depth_PunchBlobNew : Inherits TaskParent
    Dim depthDec As New Depth_PunchDecreasing
    Dim depthInc As New Depth_PunchDecreasing
    Dim contours As New Contour_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold for punch", 0, 255, 250)
        desc = "Identify a punch using both depth and color"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static thresholdSlider = optiBase.FindSlider("Threshold for punch")
        Dim threshold = thresholdSlider.value

        Static lastColor As cv.Mat = task.color.Clone

        dst2 = task.color.Clone
        dst2 -= lastColor
        dst3 = dst2.Threshold(0, New cv.Scalar(threshold, threshold, threshold), cv.ThresholdTypes.Binary).ConvertScaleAbs

        dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        lastColor = task.color.Clone
    End Sub
End Class








Public Class Depth_Contour : Inherits TaskParent
    Dim contour As New Contour_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(2) = "task.depthMask contour"
        desc = "Create and display the task.depthMask output as a contour."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contour.Run(task.depthMask)

        dst2.SetTo(0)
        For Each tour In contour.contourList
            DrawContour(dst2, tour.ToList, 255, -1)
        Next
    End Sub
End Class









Public Class Depth_Outline : Inherits TaskParent
    Dim contour As New Contour_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(2) = "Contour separating depth from no depth"
        desc = "Provide a line that separates depth from no depth throughout the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then src = task.depthMask
        contour.Run(src)

        dst2.SetTo(0)
        For Each tour In contour.contourList
            DrawContour(dst2, tour.ToList, 255, task.lineWidth)
        Next

        If standaloneTest() Then
            If task.heartBeat Then dst3.SetTo(0)
            dst3 = dst3 Or dst2
        End If
    End Sub
End Class






Public Class Depth_StableAverage : Inherits TaskParent
    Dim dAvg As New DepthColorizer_Mean
    Dim extrema As New Depth_StableMinMax
    Public Sub New()
        optiBase.findRadio("Use farthest distance").Checked = True
        desc = "Use Depth_StableMax to remove the artifacts from the depth averaging"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static unchangedRadio = optiBase.findRadio("Use unchanged depth input")
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        extrema.Run(src)

        If unchangedRadio.checked Then
            dst2 = extrema.dst2
            dst3 = extrema.dst3
        Else
            dAvg.Run(extrema.dst3)
            dst2 = dAvg.dst2
            dst3 = dAvg.dst3
        End If
    End Sub
End Class







Public Class Depth_StableMin : Inherits TaskParent
    Public stableMin As cv.Mat
    Dim colorize As New DepthColorizer_CPP
    Public Sub New()
        task.gOptions.unFiltered.Checked = True
        labels = {"", "", "InRange depth with low quality depth removed.", "Motion in the BGR image. Depth updated in rectangle."}
        desc = "To reduce z-Jitter, use the closest depth value at each pixel as long as the camera is stable"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(2)

        If task.heartBeat Then
            stableMin = src.Clone
            dst3.SetTo(0)
        Else
            src.CopyTo(stableMin, task.motionMask)
            If src.Type <> stableMin.Type Then src.ConvertTo(src, stableMin.Type)
            stableMin.CopyTo(src, task.noDepthMask)
            cv.Cv2.Min(src, stableMin, stableMin)
        End If

        colorize.Run(stableMin)
        dst2 = colorize.dst2
    End Sub
End Class








Public Class Depth_StableMinMax : Inherits TaskParent
    Dim colorize As New DepthColorizer_CPP
    Public dMin As New Depth_StableMin
    Public dMax As New Depth_StableMax
    Public options As New Options_MinMaxNone
    Public Sub New()
        task.gOptions.unFiltered.Checked = True
        labels(2) = "Depth map colorized"
        labels(3) = "32-bit StableDepth"
        desc = "To reduce z-Jitter, use the closest or farthest point as long as the camera is stable"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(2)
        If task.optionsChanged Then dst3 = task.pcSplit(2)

        If options.useMax Then
            dMax.Run(src)
            dst3 = dMax.stableMax
            dst2 = dMax.dst2
        ElseIf options.useMin Then
            dMin.Run(src)
            dst3 = dMin.stableMin
            dst2 = dMin.dst2
        ElseIf options.useNone Then
            dst3 = task.pcSplit(2)
            dst2 = task.depthRGB
        End If
    End Sub
End Class









Public Class Depth_WorldXYMT : Inherits TaskParent
    Public depthUnitsMeters = False
    Public Sub New()
        labels(3) = "dst3 = pointcloud"
        desc = "Create OpenGL point cloud from depth data (slow)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(2)

        dst3 = New cv.Mat(src.Size(), cv.MatType.CV_32FC3, 0)
        If depthUnitsMeters = False Then src = (src * 0.001).ToMat
        Dim multX = task.pointCloud.Width / src.Width
        Dim multY = task.pointCloud.Height / src.Height
        Parallel.ForEach(task.gridRects,
              Sub(roi)
                  Dim xy As New cv.Point3f
                  For y = roi.Y To roi.Y + roi.Height - 1
                      For x = roi.X To roi.X + roi.Width - 1
                          xy.X = x * multX
                          xy.Y = y * multY
                          xy.Z = src.Get(Of Single)(y, x)
                          If xy.Z <> 0 Then
                              Dim xyz = getWorldCoordinates(xy)
                              dst3.Set(Of cv.Point3f)(y, x, xyz)
                          End If
                      Next
                  Next
              End Sub)
        SetTrueText("OpenGL data prepared.")
    End Sub
End Class










Public Class Depth_WorldXYZ : Inherits TaskParent
    Public depthUnitsMeters = False
    Public Sub New()
        labels(3) = "dst3 = pointcloud"
        desc = "Create 32-bit XYZ format from depth data (to slow to be useful.)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(2)
        If depthUnitsMeters = False Then src = (src * 0.001).ToMat
        dst2 = New cv.Mat(src.Size(), cv.MatType.CV_32FC3, 0)
        Dim xy As New cv.Point3f
        For xy.Y = 0 To dst2.Height - 1
            For xy.X = 0 To dst2.Width - 1
                xy.Z = src.Get(Of Single)(xy.Y, xy.X)
                If xy.Z <> 0 Then
                    Dim xyz = getWorldCoordinates(xy)
                    dst2.Set(Of cv.Point3f)(xy.Y, xy.X, xyz)
                End If
            Next
        Next
        SetTrueText("OpenGL data prepared and in dst2.", 3)
    End Sub
End Class








Public Class Depth_World : Inherits TaskParent
    Dim template As New Math_Template
    Public Sub New()
        labels = {"", "", "Merged templates and depth32f - should be similar to upper right image", ""}
        desc = "Build the (approximate) point cloud using camera intrinsics - see CameraOakD.vb for comparable calculations"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.firstPass Then template.Run(src) ' intrinsics arrive with the first buffers.

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        cv.Cv2.Multiply(template.dst2, src, dst0)
        dst0 *= 1 / task.calibData.rgbIntrinsics.fx

        cv.Cv2.Multiply(template.dst3, src, dst1)
        dst1 *= 1 / task.calibData.rgbIntrinsics.fy

        cv.Cv2.Merge({dst0, dst1, src}, dst2)
        If standaloneTest() Then
            Static colorizer As New DepthColorizer_CPP
            colorizer.Run(dst2)
            dst2 = colorizer.dst2
        End If
    End Sub
End Class








Public Class Depth_Tiers : Inherits TaskParent
    Public classCount As Integer
    Dim options As New Options_DepthTiers
    Public Sub New()
        desc = "Create a reduced image of the depth data to define tiers of similar values"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        dst1 = (src * 100 / options.cmPerTier).ToMat
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)

        Dim mm = GetMinMax(src)
        If Not Single.IsInfinity(mm.minVal) And Not Single.IsInfinity(mm.maxVal) Then
            If mm.maxVal < 1000 And mm.minVal < 1000 Then
                classCount = (mm.maxVal - mm.minVal) * 100 / options.cmPerTier + 1
            End If
        End If

        dst3 = ShowPalette(dst2)
        labels(2) = $"{classCount} regions found."
    End Sub
End Class








Public Class Depth_Flatland : Inherits TaskParent
    Dim options As New Options_FlatLand
    Public Sub New()
        labels(3) = "Grayscale version"
        desc = "Attempt to stabilize the depth image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = task.depthRGB / options.reductionFactor
        dst2 *= options.reductionFactor
        dst3 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst3 = dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class







Public Class Depth_StableMax : Inherits TaskParent
    Public stableMax As cv.Mat
    Dim colorize As New DepthColorizer_CPP
    Public Sub New()
        task.gOptions.unFiltered.Checked = True
        labels = {"", "", "InRange depth with low quality depth removed.", "Motion in the BGR image. Depth updated in rectangle."}
        desc = "To reduce z-Jitter, use the farthest depth value at each pixel as long as the camera is stable"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.pcSplit(2)

        If task.heartBeat Then
            stableMax = src.Clone
            dst3.SetTo(0)
        Else
            src.CopyTo(stableMax, task.motionMask)
            If src.Type <> stableMax.Type Then src.ConvertTo(src, stableMax.Type)
            stableMax.CopyTo(src, task.noDepthMask)
            cv.Cv2.Min(src, stableMax, stableMax)
        End If

        colorize.Run(stableMax)
        dst2 = colorize.dst2
    End Sub
End Class






Public Class Depth_MinMaxNone : Inherits TaskParent
    Public options As New Options_MinMaxNone
    Dim filtered As Integer
    Public Sub New()
        desc = "To reduce z-Jitter, use the closest or farthest point as long as the camera is stable"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        Dim split() As cv.Mat
        If src.Type = cv.MatType.CV_32FC3 Then split = src.Split() Else split = task.pcSplit

        If task.heartBeat Then
            dst3 = split(2)
            filtered = 0
        End If
        labels(2) = "Point cloud unchanged"
        If options.useMax Then
            labels(2) = "Point cloud maximum values at each pixel"
            cv.Cv2.Max(split(2), dst3, split(2))
        End If
        If options.useMin Then
            labels(2) = "Point cloud minimum values at each pixel"
            Dim saveMat = split(2).Clone
            cv.Cv2.Min(split(2), dst3, split(2))
            Dim mask = split(2).InRange(0, 0.1)
            saveMat.CopyTo(split(2), mask)
        End If
        cv.Cv2.Merge(split, dst2)
        dst3 = split(2)
        filtered += 1
        labels(2) += " after " + CStr(filtered) + " images"
    End Sub
End Class






Public Class Depth_InfinityCheck : Inherits TaskParent
    Public Sub New()
        desc = "Check the pointcloud depth for infinities"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static plane As Integer = 0
        Static warnings As New List(Of String)
        Static infWarnings As Integer
        If task.heartBeatLT Then
            plane = plane + 1
            If plane > 2 Then plane = 0
        End If

        If task.gOptions.DebugCheckBox.Checked Then
            Dim mask = task.pcSplit(plane).InRange(-100, 100)
            task.pcSplit(plane).SetTo(0, Not mask)
        End If

        Dim infCount As Integer
        For y = 0 To task.pcSplit(plane).Rows - 1
            For x = 0 To task.pcSplit(plane).Cols - 1
                Dim val = task.pcSplit(plane).Get(Of Single)(y, x)
                If Single.IsInfinity(val) Or Single.IsNegativeInfinity(val) Then infCount += 1
            Next
        Next
        dst2 = task.pcSplit(plane)
        Dim planeName = Choose(plane + 1, "X ", "Y ", "Z ")
        labels(2) = CStr(infCount) + " infinite values encountered in the " + planeName + " plane"

        Dim mm = GetMinMax(task.pcSplit(plane))
        labels(3) = "min val = " + CStr(mm.minVal) + " max val = " + CStr(mm.maxVal)
        If infCount > 0 Then
            infWarnings += 1
            warnings.Add(labels(2) + " " + labels(3))
            If warnings.Count > 20 Then warnings.RemoveAt(0)
        End If

        strOut = "Infinity count was not corrected " + vbCrLf
        For i = 0 To warnings.Count - 1
            strOut += warnings(i) + vbCrLf
        Next
        strOut += CStr(infWarnings) + " warnings encountered."
        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class Depth_ColorizerOld : Inherits TaskParent
    Dim customColorMap As cv.Mat
    Dim gColor As New Gradient_Color
    Public Sub New()
        desc = "Use a palette to display depth from the raw depth data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ' couldn't do this in the constructor because it uses Gradient_Color and is called in task.
        If customColorMap Is Nothing Then
            gColor.gradientWidth = 255
            gColor.Run(src)
            customColorMap = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, gColor.gradient.Data())
            customColorMap.Set(Of cv.Vec3b)(0, 0, black.ToVec3b)
        End If
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        Dim depthNorm As cv.Mat = (src * 255 / task.MaxZmeters)
        depthNorm.ConvertTo(depthNorm, cv.MatType.CV_8U)
        cv.Cv2.ApplyColorMap(depthNorm, dst2, customColorMap)
    End Sub
End Class







Public Class Depth_TierCount : Inherits TaskParent
    Public valley As New HistValley_Depth1
    Public classCount As Integer
    Dim kValues As New List(Of Integer)
    Public Sub New()
        labels = {"", "Histogram of the depth data with instantaneous valley lines", "", ""}
        desc = "Determine the 'K' value for the best number of clusters for the depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        valley.Run(src)
        dst2 = valley.dst2

        kValues.Add(valley.valleyOrder.Count)

        classCount = CInt(kValues.Average)
        If kValues.Count > task.frameHistoryCount * 10 Then kValues.RemoveAt(0)

        SetTrueText("'K' value = " + CStr(classCount) + " after averaging.  Instanteous value = " +
                    CStr(valley.valleyOrder.Count), 3)
        labels(2) = "There are " + CStr(classCount)
    End Sub
End Class





Public Class Depth_CellTiers : Inherits TaskParent
    Public valley As New HistValley_Count
    Public Sub New()
        desc = "Find the number of valleys (tiers) in a RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        valley.standaloneFlag = standalone
        For i = 1 To Math.Min(10, task.rcList.Count - 1)
            Dim rc = task.rcList(i)
            Dim depthData = task.pcSplit(2)(rc.rect).Clone
            depthData.SetTo(0, Not rc.mask)

            valley.Run(depthData)
            If i = task.gOptions.DebugSlider.Value And standalone Then
                dst3 = valley.dst2.Clone
                labels(3) = valley.strOut
                task.ClickPoint = rc.maxDist
                task.setSelectedCell()
            End If
            If task.heartBeat Then SetTrueText(CStr(valley.classCount) + " classes", rc.maxDist)
        Next

        Static saveTrueText As New List(Of TrueText)
        If task.heartBeat Then saveTrueText = New List(Of TrueText)(trueData)
        trueData = New List(Of TrueText)(saveTrueText)
    End Sub
End Class





Public Class Depth_ErrorEstimate : Inherits TaskParent
    Public Sub New()
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_32F)
        labels(2) = "Colorized depth error estimate for the current image"
        desc = "Provide an estimate of the error based on the depth - a linear estimate based on the '2% at 2 meters' statement."
    End Sub
    Public Function ErrorEstimate(depth As Single) As Single
        Dim depthError = 0.02 * depth / 2
        Return depthError
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1.SetTo(0)
        For Each brick In task.brickList
            Dim testError = ErrorEstimate(brick.depth)
            dst1(brick.rect).SetTo(testError)
        Next

        Dim mm = GetMinMax(dst1)
        dst2 = ShowPalette(dst1)
        ' dst2.SetTo(0, task.noDepthMask)
        labels(3) = "Error estimates vary from " + Format(mm.minVal, fmt3) + " to " + Format(mm.maxVal, fmt3)
        SetTrueText(Format(ErrorEstimate(task.gcD.depth), fmt3) + " estimated error" + vbCrLf + Format(task.gcD.depth, fmt3) + "m",
                    task.mouseMovePoint, 3)
    End Sub
End Class







Public Class Depth_MinMaxToVoronoi : Inherits TaskParent
    Public Sub New()
        task.kalman = New Kalman_Basics
        ReDim task.kalman.kInput(task.gridRects.Count * 4 - 1)
        labels = {"", "", "Red is min distance, blue is max distance", "Voronoi representation of min point (only) for each cell."}
        desc = "Find min and max depth in each roi and create a voronoi representation using the min and max points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, src.Width, src.Height))

        dst1 = src.Clone()
        dst1.SetTo(white, task.gridMask)
        For Each brick In task.brickList
            Dim pt = brick.mm.minLoc
            subdiv.Insert(New cv.Point(pt.X + brick.rect.X, pt.Y + brick.rect.Y))
            DrawCircle(dst1(brick.rect), brick.mm.minLoc, task.DotSize, cv.Scalar.Red)
            DrawCircle(dst1(brick.rect), brick.mm.maxLoc, task.DotSize, cv.Scalar.Blue)
        Next

        If task.optionsChanged Then dst2 = dst1.Clone Else dst1.CopyTo(dst2, task.motionMask)

        Dim facets = New cv.Point2f()() {Nothing}
        Dim centers() As cv.Point2f
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, centers)

        Dim ifacet() As cv.Point
        Dim ifacets = New cv.Point()() {Nothing}

        For i = 0 To facets.Length - 1
            ReDim ifacet(facets(i).Length - 1)
            For j = 0 To facets(i).Length - 1
                ifacet(j) = New cv.Point(Math.Round(facets(i)(j).X), Math.Round(facets(i)(j).Y))
            Next
            ifacets(0) = ifacet
            dst3.FillConvexPoly(ifacet, task.scalarColors(i Mod task.scalarColors.Length), task.lineType)
            cv.Cv2.Polylines(dst3, ifacets, True, cv.Scalar.Black, task.lineWidth, task.lineType, 0)
        Next
    End Sub
End Class