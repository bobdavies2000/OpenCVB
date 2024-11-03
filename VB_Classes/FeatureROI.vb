Imports System.Runtime.InteropServices
Imports OpenCvSharp
Imports System.Security.Cryptography
Imports cvb = OpenCvSharp
Public Class FeatureROI_Basics : Inherits TaskParent
    Dim addw As New AddWeighted_Basics
    Public rects As New List(Of cvb.Rect)
    Public meanList As New List(Of Single)
    Public stdevList As New List(Of Single)
    Public stdevAverage As Single
    Public Sub New()
        task.gOptions.setGridSize(CInt(dst2.Width / 40)) ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Use roi's to compute the stdev for each roi.  If small (<10), mark as featureLess (white)."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst1 = If(src.Channels() <> 1, src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY), src.Clone)
        stdevList.Clear()
        meanList.Clear()
        Dim mean As cvb.Scalar, stdev As cvb.Scalar
        For Each roi In task.gridRects
            cvb.Cv2.MeanStdDev(dst1(roi), mean, stdev)
            stdevList.Add(stdev(0))
            meanList.Add(mean(0))
        Next

        stdevAverage = stdevList.Average
        dst3.SetTo(0)
        rects.Clear()
        For i = 0 To stdevList.Count - 1
            Dim roi = task.gridRects(i)
            Dim depthCheck = task.noDepthMask(roi)
            If stdevList(i) < stdevAverage Or depthCheck.CountNonZero / depthCheck.Total > 0.5 Then
                dst3.Rectangle(roi, cvb.Scalar.White, -1)
            Else
                rects.Add(roi)
            End If
        Next
        If task.heartBeat Then
            labels(2) = CStr(rects.Count) + " of " + CStr(task.gridRects.Count) + " roi's had above average standard deviation (average = " +
                        Format(stdevList.Average, fmt1) + ")"
        End If

        addw.src2 = dst3
        addw.Run(dst1)
        dst2 = addw.dst2
    End Sub
End Class








Public Class FeatureROI_Color : Inherits TaskParent
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        FindSlider("Add Weighted %").Value = 70
        task.gOptions.setGridSize(CInt(dst2.Width / 40)) ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Use roi's to compute the stdev for each roi.  If small (<10), mark as featureLess (white)."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim stdevList0 As New List(Of Single)
        Dim stdevList1 As New List(Of Single)
        Dim stdevList2 As New List(Of Single)
        Dim mean As cvb.Scalar, stdev As cvb.Scalar
        For Each roi In task.gridRects
            cvb.Cv2.MeanStdDev(src(roi), mean, stdev)
            stdevList0.Add(stdev(0))
            stdevList1.Add(stdev(1))
            stdevList2.Add(stdev(2))
        Next

        Dim avg0 = stdevList0.Average
        Dim avg1 = stdevList1.Average
        Dim avg2 = stdevList2.Average
        dst3.SetTo(0)
        For i = 0 To stdevList0.Count - 1
            Dim roi = task.gridRects(i)
            If stdevList0(i) < avg0 And stdevList1(i) < avg1 And stdevList2(i) < avg2 Then
                dst3.Rectangle(roi, cvb.Scalar.White, -1)
            End If
        Next
        labels(3) = "Stdev average X/Y/Z = " + CInt(stdevList0.Average).ToString + ", " + CInt(stdevList1.Average).ToString + ", " + CInt(stdevList2.Average).ToString

        addw.src2 = dst3.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        addw.Run(src)
        dst2 = addw.dst2
    End Sub
End Class







Public Class FeatureROI_Canny : Inherits TaskParent
    Dim canny As New Edge_Basics
    Dim devGrid As New FeatureROI_Basics
    Public Sub New()
        task.gOptions.setGridSize(CInt(dst2.Width / 40)) ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
        desc = "Create the stdev grid with the input image, then create the stdev grid for the canny output, then combine them."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        canny.Run(src)
        dst3 = canny.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

        devGrid.Run(src Or dst3)
        dst2 = devGrid.dst2
    End Sub
End Class








Public Class FeatureROI_Sorted : Inherits TaskParent
    Dim addw As New AddWeighted_Basics
    Public sortedStd As New SortedList(Of Single, cvb.Rect)(New compareAllowIdenticalSingle)
    Public bgrList As New List(Of cvb.Vec3b)
    Public roiList As New List(Of cvb.Rect)
    Public categories() As Integer
    Public options As New Options_StdevGrid
    Public maskVal As Integer = 255
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        task.gOptions.setGridSize(CInt(dst2.Width / 40)) ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
        If standalone = False Then maskVal = 1
        labels(2) = "Use the AddWeighted slider to observe where stdev is above average."
        desc = "Sort the roi's by the sum of their bgr stdev's to find the least volatile regions"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim meanS As cvb.Scalar, stdev As cvb.Scalar
        sortedStd.Clear()
        bgrList.Clear()
        roiList.Clear()
        ReDim categories(9)
        For Each roi In task.gridRects
            cvb.Cv2.MeanStdDev(src(roi), meanS, stdev)
            sortedStd.Add(stdev(0) + stdev(1) + stdev(2), roi)
            Dim colorIndex As Integer = 1
            Dim mean As cvb.Vec3i = New cvb.Vec3i(CInt(meanS(0)), CInt(meanS(1)), CInt(meanS(2)))
            If mean(0) < options.minThreshold And mean(1) < options.minThreshold And mean(2) < options.minThreshold Then
                colorIndex = 1
            ElseIf mean(0) > options.maxThreshold And mean(1) > options.maxThreshold And mean(2) > options.maxThreshold Then
                colorIndex = 2
            ElseIf Math.Abs(mean(0) - mean(1)) < options.diffThreshold And Math.Abs(mean(1) - mean(2)) < options.diffThreshold Then
                colorIndex = 3
            ElseIf Math.Abs(mean(1) - mean(2)) < options.diffThreshold Then
                colorIndex = 4
            ElseIf Math.Abs(mean(0) - mean(2)) < options.diffThreshold Then
                colorIndex = 5
            ElseIf Math.Abs(mean(0) - mean(1)) < options.diffThreshold Then
                colorIndex = 6
            ElseIf Math.Abs(mean(0) - mean(1)) > options.diffThreshold And Math.Abs(mean(0) - mean(2)) > options.diffThreshold Then
                colorIndex = 7
            ElseIf Math.Abs(mean(1) - mean(0)) > options.diffThreshold And Math.Abs(mean(1) - mean(2)) > options.diffThreshold Then
                colorIndex = 8
            ElseIf Math.Abs(mean(2) - mean(0)) > options.diffThreshold And Math.Abs(mean(2) - mean(1)) > options.diffThreshold Then
                colorIndex = 9
            End If

            Dim color = Choose(colorIndex, black, white, grayColor, yellow, purple, teal, blue, green, red)
            categories(colorIndex) += 1
            bgrList.Add(color)
            roiList.Add(roi)
        Next
        Dim avg = sortedStd.Keys.Average

        Dim count As Integer
        dst2.SetTo(0)
        For i = 0 To sortedStd.Count - 1
            Dim nextStdev = sortedStd.ElementAt(i).Key
            If nextStdev < avg Then
                Dim roi = sortedStd.ElementAt(i).Value
                dst2(roi).SetTo(maskVal)
                count += 1
            End If
        Next

        If standaloneTest() Then
            addw.src2 = dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
            addw.Run(src)
            dst3 = addw.dst2
        End If

        labels(3) = $"{count} roi's or " + Format(count / sortedStd.Count, "0%") + " have an average stdev sum of " +
                    Format(avg, fmt1) + " or less"
    End Sub
End Class







Public Class FeatureROI_ColorSplit : Inherits TaskParent
    Dim devGrid As New FeatureROI_Sorted
    Public Sub New()
        devGrid.maskVal = 255
        task.gOptions.setGridSize(CInt(dst2.Width / 40)) ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
        desc = "Split each roi into one of 9 categories - black, white, gray, yellow, purple, teal, blue, green, or red - based on the stdev for the roi"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        devGrid.Run(src)

        For i = 0 To devGrid.bgrList.Count - 1
            Dim roi = devGrid.roiList(i)
            Dim color = devGrid.bgrList(i)
            dst2(roi).SetTo(color)
        Next
        dst2.SetTo(0, Not devGrid.dst2)

        strOut = "Categories:" + vbCrLf
        For i = 1 To devGrid.categories.Count - 1
            Dim colorName = Choose(i, "black", "white", "gray", "yellow", "purple", "teal", "blue", "green", "red")
            strOut += colorName + vbTab + CStr(devGrid.categories(i)) + vbCrLf
        Next
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class FeatureROI_Correlation : Inherits TaskParent
    Public gather As New FeatureROI_Basics
    Dim plot As New Plot_OverTimeSingle
    Dim options As New Options_Features
    Public Sub New()
        FindSlider("Feature Correlation Threshold").Value = 90
        desc = "Use the grid-based correlations with the previous image to determine if there was camera motion"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst1 = If(src.Channels() <> 1, src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY), src.Clone)
        gather.Run(dst1)
        dst2 = gather.dst2

        Static lastImage As cvb.Mat = dst1.Clone

        Dim correlationMat As New cvb.Mat
        Dim motionCount As Integer
        For i = 0 To gather.stdevList.Count - 1
            Dim roi = task.gridRects(i)
            If gather.stdevList(i) >= gather.stdevAverage Then
                cvb.Cv2.MatchTemplate(dst1(roi), lastImage(roi), correlationMat, cvb.TemplateMatchModes.CCoeffNormed)
                Dim corr = correlationMat.Get(Of Single)(0, 0)
                If corr < options.correlationMin Then SetTrueText(Format(corr, fmt1), roi.TopLeft)
                If corr < options.correlationMin Then motionCount += 1
            End If
        Next

        plot.plotData = motionCount
        plot.min = -1
        plot.Run(empty)
        dst3 = plot.dst2

        labels(2) = CStr(gather.rects.Count) + " of " + CStr(task.gridRects.Count) + " roi's had above average standard deviation."
        lastImage = dst1.Clone
    End Sub
End Class






Public Class FeatureROI_LowStdev : Inherits TaskParent
    Public rects As New List(Of cvb.Rect)
    Dim gather As New FeatureROI_Basics
    Public Sub New()
        desc = "Isolate the roi's with low stdev"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst1 = If(src.Channels() <> 1, src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY), src.Clone)
        gather.Run(dst1)
        dst2 = gather.dst2

        rects.Clear()
        For i = 0 To gather.stdevList.Count - 1
            Dim roi = task.gridRects(i)
            If gather.stdevList(i) < gather.stdevAverage Then
                rects.Add(roi)
                SetTrueText(Format(gather.stdevList(i), fmt1), roi.TopLeft, 3)
            End If
        Next
        If task.heartBeat Then labels = {"", "", CStr(task.gridRects.Count - gather.rects.Count) + " roi's had low standard deviation",
                                         "Stdev average = " + Format(gather.stdevList.Average, fmt1)}
    End Sub
End Class





Public Class FeatureROI_LowStdevCorrelation : Inherits TaskParent
    Dim gather As New FeatureROI_LowStdev
    Dim correlations As New List(Of Single)
    Dim options As New Options_Features
    Dim saveStdev As New List(Of Single)
    Public Sub New()
        FindSlider("Feature Correlation Threshold").Value = 50
        desc = "Display the correlation coefficients for roi's with low standard deviation."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst1 = If(src.Channels() <> 1, src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY), src.Clone)
        gather.Run(dst1)
        dst2 = gather.dst2

        Static lastImage As cvb.Mat = dst1.Clone

        Dim correlationMat As New cvb.Mat
        correlations.Clear()
        For Each roi In gather.rects
            cvb.Cv2.MatchTemplate(dst1(roi), lastImage(roi), correlationMat, cvb.TemplateMatchModes.CCoeffNormed)
            Dim corr = correlationMat.Get(Of Single)(0, 0)
            correlations.Add(corr)
        Next

        Static saveCorrs As New List(Of Single)(correlations)
        Static saveRects As New List(Of cvb.Rect)(gather.rects)
        If task.heartBeat Then
            saveCorrs = New List(Of Single)(correlations)
            saveRects = New List(Of cvb.Rect)(gather.rects)

            saveStdev.Clear()
            Dim mean As cvb.Scalar, stdev As cvb.Scalar
            For i = 0 To saveRects.Count - 1
                cvb.Cv2.MeanStdDev(dst1(saveRects(i)), mean, stdev)
                saveStdev.Add(stdev(0))
            Next
        End If
        For i = 0 To saveRects.Count - 1
            If saveCorrs(i) < options.correlationMin Then SetTrueText(Format(saveCorrs(i), fmt2), saveRects(i).TopLeft)
            If saveCorrs(i) < options.correlationMin Then SetTrueText(Format(saveStdev(i), fmt2), saveRects(i).TopLeft, 3)
        Next

        lastImage = dst1.Clone
    End Sub
End Class






Public Class FeatureROI_LR : Inherits TaskParent
    Public gLeft As New FeatureROI_Basics
    Public gRight As New FeatureROI_Basics
    Public Sub New()
        desc = "Capture the above average standard deviation roi's for the left and right images."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        gLeft.Run(task.leftView)
        dst2 = gLeft.dst2
        labels(2) = CStr(gLeft.rects.Count) + " roi's had above average standard deviation in the left image"

        gRight.Run(task.rightView)
        dst3 = gRight.dst2
        labels(3) = CStr(gRight.rects.Count) + " roi's had above average standard deviation in the right image"
    End Sub
End Class






Public Class FeatureROI_LRClick : Inherits TaskParent
    Dim gather As New FeatureROI_Basics
    Dim ClickPoint As cvb.Point, picTag As Integer
    Dim options As New Options_Features
    Public Sub New()
        task.gOptions.setGridSize(16)
        FindSlider("Feature Correlation Threshold").Value = 80
        If standalone Then task.gOptions.setDisplay1()
        If standalone Then task.gOptions.setDisplay1()
        labels(2) = "Click the above average stdev roi's (the darker regions) to find corresponding roi in the right image."
        desc = "Capture the above average standard deviation roi's for the left and right images."
    End Sub
    Public Sub setClickPoint(pt As cvb.Point, _pictag As Integer)
        ClickPoint = pt
        picTag = _pictag
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst0 = src.Clone
        dst3 = If(task.rightView.Channels() <> 3, task.rightView.CvtColor(cvb.ColorConversionCodes.GRAY2BGR), task.rightView.Clone)
        src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If task.rightView.Channels() <> 1 Then task.rightView = task.rightView.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        gather.Run(src)
        dst2 = gather.dst2
        labels = gather.labels

        If gather.rects.Count = 0 Then Exit Sub
        If task.mouseClickFlag Then setClickPoint(task.ClickPoint, task.mousePicTag)
        If ClickPoint = newPoint Then setClickPoint(gather.rects(gather.rects.Count / 2).TopLeft, 2)
        Dim gridIndex = task.gridMap32S.Get(Of Integer)(ClickPoint.Y, ClickPoint.X)
        Dim roi = task.gridRects(gridIndex)
        dst2.Rectangle(roi, cvb.Scalar.White, task.lineWidth)

        Dim correlationMat As New cvb.Mat
        Dim corr As New List(Of Single)
        For j = 0 To roi.X - 1
            Dim r = New cvb.Rect(j, roi.Y, roi.Width, roi.Height)
            cvb.Cv2.MatchTemplate(src(roi), task.rightView(r), correlationMat, cvb.TemplateMatchModes.CCoeffNormed)
            corr.Add(correlationMat.Get(Of Single)(0, 0))
        Next

        If corr.Count = 0 Then
            SetTrueText("No corresponding roi found", 2)
        Else
            Dim maxCorr = corr.Max
            If maxCorr < options.correlationMin Then
                SetTrueText("Correlation " + Format(maxCorr, fmt3) + " is less than " + Format(options.correlationMin, fmt1), 1)
            Else
                Dim index = corr.IndexOf(maxCorr)
                Dim rectRight = New cvb.Rect(index, roi.Y, roi.Width, roi.Height)
                Dim offset = roi.TopLeft.X - rectRight.TopLeft.X
                If task.heartBeat Then
                    strOut = "CoeffNormed max correlation = " + Format(maxCorr, fmt3) + vbCrLf
                    strOut += "Left Mean = " + Format(gather.meanList(gridIndex), fmt3) + " Left stdev = " + Format(gather.stdevList(gridIndex), fmt3) + vbCrLf
                    Dim mean As cvb.Scalar, stdev As cvb.Scalar
                    cvb.Cv2.MeanStdDev(dst3(rectRight), mean, stdev)
                    strOut += "Right Mean = " + Format(mean(0), fmt3) + " Right stdev = " + Format(stdev(0), fmt3) + vbCrLf
                    strOut += "Right rectangle is offset " + CStr(offset) + " pixels from the left image rectangle"
                End If
                dst3.Rectangle(rectRight, task.HighlightColor, task.lineWidth)
                dst0.Rectangle(roi, task.HighlightColor, task.lineWidth)
                dst1.SetTo(0)
                DrawCircle(dst1, roi.TopLeft, task.DotSize, task.HighlightColor)
                DrawCircle(dst1, rectRight.TopLeft, task.DotSize + 2, task.HighlightColor)
                Dim pt = New cvb.Point(rectRight.X, roi.Y + 5)
                SetTrueText(CStr(offset) + " pixel offset" + vbCrLf + "Larger = Right", pt, 1)
                SetTrueText(strOut, 1)
                labels(3) = "Corresponding roi highlighted in yellow.  Average stdev = " + Format(gather.stdevAverage, fmt3)
            End If
        End If
    End Sub
End Class






Public Class FeatureROI_LRAll : Inherits TaskParent
    Dim gather As New FeatureROI_Basics
    Dim options As New Options_Features
    Public sortedRects As New SortedList(Of Single, cvb.Rect)(New compareAllowIdenticalSingleInverted)
    Public Sub New()
        task.gOptions.setGridSize(16)
        FindSlider("Feature Correlation Threshold").Value = 95
        labels(3) = "The highlighted roi's are those high stdev roi's with the highest correlation between left and right images."
        desc = "Find all the roi's with high stdev and high correlation between left and right images."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst3 = If(task.rightView.Channels() <> 3, task.rightView.CvtColor(cvb.ColorConversionCodes.GRAY2BGR), task.rightView.Clone)
        src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If task.rightView.Channels() <> 1 Then task.rightView = task.rightView.CvtColor(cvb.ColorConversionCodes.BGR2Gray)

        gather.Run(src)
        dst2 = gather.dst2

        If gather.rects.Count = 0 Then Exit Sub

        Dim correlationMat As New cvb.Mat
        sortedRects.Clear()
        For Each roi In gather.rects
            If roi.X = 0 Then Continue For
            Dim r = New cvb.Rect(0, roi.Y, roi.X, roi.Height)
            cvb.Cv2.MatchTemplate(src(roi), task.rightView(r), correlationMat, cvb.TemplateMatchModes.CCoeffNormed)
            Dim mm = GetMinMax(correlationMat)
            If mm.maxVal >= options.correlationMin Then sortedRects.Add(mm.maxVal, New cvb.Rect(mm.maxLoc.X, roi.Y, roi.Width, roi.Height))
        Next
        labels(2) = CStr(sortedRects.Count) + " roi's had left/right correlation higher than " + Format(options.correlationMin, fmt3)

        For Each roi In sortedRects.Values
            dst3.Rectangle(roi, task.HighlightColor, task.lineWidth)
        Next
    End Sub
End Class


