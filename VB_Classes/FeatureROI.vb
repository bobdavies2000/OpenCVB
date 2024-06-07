Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class FeatureROI_Basics : Inherits VB_Parent
    Dim addw As New AddWeighted_Basics
    Public rects As New List(Of cv.Rect)
    Public meanList As New List(Of Single)
    Public stdevList As New List(Of Single)
    Public stdevAverage As Single
    Public Sub New()
        task.gOptions.GridSize.Value = dst2.Width / 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use roi's to compute the stdev for each roi.  If small (<10), mark as featureLess (white)."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst1 = If(src.Channels <> 1, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src.Clone)
        stdevList.Clear()
        meanList.Clear()
        Dim mean As cv.Scalar, stdev As cv.Scalar
        For Each roi In task.gridList
            cv.Cv2.MeanStdDev(dst1(roi), mean, stdev)
            stdevList.Add(stdev(0))
            meanList.Add(mean(0))
        Next

        stdevAverage = stdevList.Average
        dst3.SetTo(0)
        rects.Clear()
        For i = 0 To stdevList.Count - 1
            Dim roi = task.gridList(i)
            Dim depthCheck = task.noDepthMask(roi)
            If stdevList(i) < stdevAverage Or depthCheck.CountNonZero / depthCheck.Total > 0.5 Then
                dst3.Rectangle(roi, cv.Scalar.White, -1)
            Else
                rects.Add(roi)
            End If
        Next
        If task.heartBeat Then
            labels(2) = CStr(rects.Count) + " of " + CStr(task.gridList.Count) + " roi's had above average standard deviation (average = " +
                        Format(stdevList.Average, fmt1) + ")"
        End If

        addw.src2 = dst3
        addw.Run(dst1)
        dst2 = addw.dst2
    End Sub
End Class








Public Class FeatureROI_Color : Inherits VB_Parent
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        FindSlider("Add Weighted %").Value = 70
        task.gOptions.GridSize.Value = dst2.Width / 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use roi's to compute the stdev for each roi.  If small (<10), mark as featureLess (white)."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim stdevList0 As New List(Of Single)
        Dim stdevList1 As New List(Of Single)
        Dim stdevList2 As New List(Of Single)
        Dim mean As cv.Scalar, stdev As cv.Scalar
        For Each roi In task.gridList
            cv.Cv2.MeanStdDev(src(roi), mean, stdev)
            stdevList0.Add(stdev(0))
            stdevList1.Add(stdev(1))
            stdevList2.Add(stdev(2))
        Next

        Dim avg0 = stdevList0.Average
        Dim avg1 = stdevList1.Average
        Dim avg2 = stdevList2.Average
        dst3.SetTo(0)
        For i = 0 To stdevList0.Count - 1
            Dim roi = task.gridList(i)
            If stdevList0(i) < avg0 And stdevList1(i) < avg1 And stdevList2(i) < avg2 Then
                dst3.Rectangle(roi, cv.Scalar.White, -1)
            End If
        Next
        labels(3) = "Stdev average X/Y/Z = " + CInt(stdevList0.Average).ToString + ", " + CInt(stdevList1.Average).ToString + ", " + CInt(stdevList2.Average).ToString

        addw.src2 = dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        addw.Run(src)
        dst2 = addw.dst2
    End Sub
End Class







Public Class FeatureROI_Canny : Inherits VB_Parent
    Dim canny As New Edge_Canny
    Dim devGrid As New FeatureROI_Basics
    Public Sub New()
        task.gOptions.GridSize.Value = dst2.Width / 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
        desc = "Create the stdev grid with the input image, then create the stdev grid for the canny output, then combine them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        canny.Run(src)
        dst3 = canny.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        devGrid.Run(src Or dst3)
        dst2 = devGrid.dst2
    End Sub
End Class








Public Class FeatureROI_Sorted : Inherits VB_Parent
    Dim addw As New AddWeighted_Basics
    Dim gridLow As New Grid_LowRes
    Public sortedStd As New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingle)
    Public bgrList As New List(Of cv.Vec3b)
    Public roiList As New List(Of cv.Rect)
    Public categories() As Integer
    Public options As New Options_StdevGrid
    Public maskVal As Integer = 255
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        task.gOptions.GridSize.Value = dst2.Width / 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
        If standalone = False Then maskVal = 1
        labels(2) = "Use the AddWeighted slider to observe where stdev is above average."
        desc = "Sort the roi's by the sum of their bgr stdev's to find the least volatile regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim meanS As cv.Scalar, stdev As cv.Scalar
        sortedStd.Clear()
        bgrList.Clear()
        roiList.Clear()
        ReDim categories(9)
        For Each roi In task.gridList
            cv.Cv2.MeanStdDev(src(roi), meanS, stdev)
            sortedStd.Add(stdev(0) + stdev(1) + stdev(2), roi)
            Dim colorIndex As Integer = 1
            Dim mean As cv.Vec3i = New cv.Vec3i(CInt(meanS(0)), CInt(meanS(1)), CInt(meanS(2)))
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

            Dim color = Choose(colorIndex, black, white, gray, yellow, purple, teal, blue, green, red)
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
            addw.src2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            addw.Run(src)
            dst3 = addw.dst2
        End If

        labels(3) = $"{count} roi's or " + Format(count / sortedStd.Count, "0%") + " have an average stdev sum of " +
                    Format(avg, fmt1) + " or less"
    End Sub
End Class







Public Class FeatureROI_ColorSplit : Inherits VB_Parent
    Dim devGrid As New FeatureROI_Sorted
    Public Sub New()
        devGrid.maskVal = 255
        task.gOptions.GridSize.Value = dst2.Width / 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
        desc = "Split each roi into one of 9 categories - black, white, gray, yellow, purple, teal, blue, green, or red - based on the stdev for the roi"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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
        setTrueText(strOut, 3)
    End Sub
End Class







Public Class FeatureROI_Correlation : Inherits VB_Parent
    Public gather As New FeatureROI_Basics
    Dim plot As New Plot_OverTimeSingle
    Dim options As New Options_Features
    Public Sub New()
        FindSlider("Feature Correlation Threshold").Value = 90
        desc = "Use the grid-based correlations with the previous image to determine if there was camera motion"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst1 = If(src.Channels <> 1, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src.Clone)
        gather.Run(dst1)
        dst2 = gather.dst2

        Static lastImage As cv.Mat = dst1.Clone

        Dim correlationMat As New cv.Mat
        Dim motionCount As Integer
        For i = 0 To gather.stdevList.Count - 1
            Dim roi = task.gridList(i)
            If gather.stdevList(i) >= gather.stdevAverage Then
                cv.Cv2.MatchTemplate(dst1(roi), lastImage(roi), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
                Dim corr = correlationMat.Get(Of Single)(0, 0)
                If corr < options.correlationMin Then setTrueText(Format(corr, fmt1), roi.TopLeft)
                If corr < options.correlationMin Then motionCount += 1
            End If
        Next

        plot.plotData = New cv.Scalar(motionCount, 0, 0)
        plot.min = -1
        plot.Run(empty)
        dst3 = plot.dst2

        labels(2) = CStr(gather.rects.Count) + " of " + CStr(task.gridList.Count) + " roi's had above average standard deviation."
        lastImage = dst1.Clone
    End Sub
End Class






Public Class FeatureROI_LowStdev : Inherits VB_Parent
    Public rects As New List(Of cv.Rect)
    Dim gather As New FeatureROI_Basics
    Public Sub New()
        desc = "Isolate the roi's with low stdev"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst1 = If(src.Channels <> 1, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src.Clone)
        gather.Run(dst1)
        dst2 = gather.dst2

        rects.Clear()
        For i = 0 To gather.stdevList.Count - 1
            Dim roi = task.gridList(i)
            If gather.stdevList(i) < gather.stdevAverage Then
                rects.Add(roi)
                setTrueText(Format(gather.stdevList(i), fmt1), roi.TopLeft, 3)
            End If
        Next
        If task.heartBeat Then labels = {"", "", CStr(task.gridList.Count - gather.rects.Count) + " roi's had low standard deviation",
                                         "Stdev average = " + Format(gather.stdevList.Average, fmt1)}
    End Sub
End Class





Public Class FeatureROI_LowStdevCorrelation : Inherits VB_Parent
    Dim gather As New FeatureROI_LowStdev
    Dim correlations As New List(Of Single)
    Dim options As New Options_Features
    Public Sub New()
        FindSlider("Feature Correlation Threshold").Value = 50
        desc = "Display the correlation coefficients for roi's with low standard deviation."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst1 = If(src.Channels <> 1, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src.Clone)
        gather.Run(dst1)
        dst2 = gather.dst2

        Static lastImage As cv.Mat = dst1.Clone

        Dim correlationMat As New cv.Mat
        correlations.Clear()
        For Each roi In gather.rects
            cv.Cv2.MatchTemplate(dst1(roi), lastImage(roi), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
            Dim corr = correlationMat.Get(Of Single)(0, 0)
            correlations.Add(corr)
        Next

        Static saveCorrs As New List(Of Single)(correlations)
        Static saveRects As New List(Of cv.Rect)(gather.rects)
        Static saveStdev As New List(Of Single)
        If task.heartBeat Then
            saveCorrs = New List(Of Single)(correlations)
            saveRects = New List(Of cv.Rect)(gather.rects)

            saveStdev.Clear()
            Dim mean As cv.Scalar, stdev As cv.Scalar
            For i = 0 To saveRects.Count - 1
                cv.Cv2.MeanStdDev(dst1(saveRects(i)), mean, stdev)
                saveStdev.Add(stdev(0))
            Next
        End If
        For i = 0 To saveRects.Count - 1
            If saveCorrs(i) < options.correlationMin Then setTrueText(Format(saveCorrs(i), fmt2), saveRects(i).TopLeft)
            If saveCorrs(i) < options.correlationMin Then setTrueText(Format(saveStdev(i), fmt2), saveRects(i).TopLeft, 3)
        Next

        lastImage = dst1.Clone
    End Sub
End Class






Public Class FeatureROI_LR : Inherits VB_Parent
    Public gLeft As New FeatureROI_Basics
    Public gRight As New FeatureROI_Basics
    Public Sub New()
        desc = "Capture the above average standard deviation roi's for the left and right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        gLeft.Run(task.leftView)
        dst2 = gLeft.dst2
        labels(2) = CStr(gLeft.rects.Count) + " roi's had above average standard deviation in the left image"

        gRight.Run(task.rightView)
        dst3 = gRight.dst2
        labels(3) = CStr(gRight.rects.Count) + " roi's had above average standard deviation in the right image"
    End Sub
End Class






Public Class FeatureROI_LRClick : Inherits VB_Parent
    Dim gather As New FeatureROI_Basics
    Dim clickPoint As cv.Point, picTag As Integer
    Dim options As New Options_Features
    Public Sub New()
        task.gOptions.GridSize.Value = 16
        FindSlider("Feature Correlation Threshold").Value = 80
        If standalone Then task.gOptions.displayDst0.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(2) = "Click the above average stdev roi's (the darker regions) to find corresponding roi in the right image."
        desc = "Capture the above average standard deviation roi's for the left and right images."
    End Sub
    Public Sub setClickPoint(pt As cv.Point, _pictag As Integer)
        clickPoint = pt
        picTag = _pictag
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst0 = src.Clone
        dst3 = If(task.rightView.Channels <> 3, task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.rightView.Clone)

        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.rightView.Channels <> 1 Then task.rightView = task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        gather.Run(src)
        dst2 = gather.dst2
        labels = gather.labels

        If gather.rects.Count = 0 Then Exit Sub
        If task.mouseClickFlag Then setClickPoint(task.clickPoint, task.mousePicTag)
        If clickPoint = newPoint Then setClickPoint(gather.rects(gather.rects.Count / 2).TopLeft, 2)
        Dim gridIndex = task.gridMap.Get(Of Integer)(clickPoint.Y, clickPoint.X)
        Dim roi = task.gridList(gridIndex)
        dst2.Rectangle(roi, cv.Scalar.White, task.lineWidth)

        Dim correlationMat As New cv.Mat
        Dim corr As New List(Of Single)
        For j = 0 To roi.X - 1
            Dim r = New cv.Rect(j, roi.Y, roi.Width, roi.Height)
            cv.Cv2.MatchTemplate(src(roi), task.rightView(r), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
            corr.Add(correlationMat.Get(Of Single)(0, 0))
        Next

        If corr.Count = 0 Then
            setTrueText("No corresponding roi found", 2)
        Else
            Dim maxCorr = corr.Max
            If maxCorr < options.correlationMin Then
                setTrueText("Correlation " + Format(maxCorr, fmt3) + " is less than " + Format(options.correlationMin, fmt1), 1)
            Else
                Dim index = corr.IndexOf(maxCorr)
                Dim rectRight = New cv.Rect(index, roi.Y, roi.Width, roi.Height)
                Dim offset = roi.TopLeft.X - rectRight.TopLeft.X
                If task.heartBeat Then
                    strOut = "CoeffNormed max correlation = " + Format(maxCorr, fmt3) + vbCrLf
                    strOut += "Left Mean = " + Format(gather.meanList(gridIndex), fmt3) + " Left stdev = " + Format(gather.stdevList(gridIndex), fmt3) + vbCrLf
                    Dim mean As cv.Scalar, stdev As cv.Scalar
                    cv.Cv2.MeanStdDev(dst3(rectRight), mean, stdev)
                    strOut += "Right Mean = " + Format(mean(0), fmt3) + " Right stdev = " + Format(stdev(0), fmt3) + vbCrLf
                    strOut += "Right rectangle is offset " + CStr(offset) + " pixels from the left image rectangle"
                End If
                dst3.Rectangle(rectRight, task.highlightColor, task.lineWidth)
                dst0.Rectangle(roi, task.highlightColor, task.lineWidth)
                dst1.SetTo(0)
                drawCircle(dst1,roi.TopLeft, task.dotSize, task.highlightColor)
                drawCircle(dst1,rectRight.TopLeft, task.dotSize + 2, task.highlightColor)
                Dim pt = New cv.Point(rectRight.X, roi.Y + 5)
                setTrueText(CStr(offset) + " pixel offset" + vbCrLf + "Larger = Right", pt, 1)
                setTrueText(strOut, 1)
                labels(3) = "Corresponding roi highlighted in yellow.  Average stdev = " + Format(gather.stdevAverage, fmt3)
            End If
        End If
    End Sub
End Class






Public Class FeatureROI_LRAll : Inherits VB_Parent
    Dim gather As New FeatureROI_Basics
    Dim options As New Options_Features
    Public sortedRects As New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingleInverted)
    Public Sub New()
        task.gOptions.GridSize.Value = 16
        FindSlider("Feature Correlation Threshold").Value = 95
        labels(3) = "The highlighted roi's are those high stdev roi's with the highest correlation between left and right images."
        desc = "Find all the roi's with high stdev and high correlation between left and right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst3 = If(task.rightView.Channels <> 3, task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.rightView.Clone)

        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.rightView.Channels <> 1 Then task.rightView = task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        gather.Run(src)
        dst2 = gather.dst2

        If gather.rects.Count = 0 Then Exit Sub

        Dim correlationMat As New cv.Mat
        sortedRects.Clear()
        For Each roi In gather.rects
            If roi.X = 0 Then Continue For
            Dim r = New cv.Rect(0, roi.Y, roi.X, roi.Height)
            cv.Cv2.MatchTemplate(src(roi), task.rightView(r), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
            Dim mm = vbMinMax(correlationMat)
            If mm.maxVal >= options.correlationMin Then sortedRects.Add(mm.maxVal, New cv.Rect(mm.maxLoc.X, roi.Y, roi.Width, roi.Height))
        Next
        labels(2) = CStr(sortedRects.Count) + " roi's had left/right correlation higher than " + Format(options.correlationMin, fmt3)

        For Each roi In sortedRects.Values
            dst3.Rectangle(roi, task.highlightColor, task.lineWidth)
        Next
    End Sub
End Class
