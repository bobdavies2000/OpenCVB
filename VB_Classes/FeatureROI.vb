﻿Imports cv = OpenCvSharp
Public Class FeatureROI_Basics : Inherits VB_Algorithm
    Dim addw As New AddWeighted_Basics
    Public rects As New List(Of cv.Rect)
    Public stdevList As New List(Of Single)
    Public stdevAverage As Single
    Public Sub New()
        findSlider("Add Weighted %").Value = 70
        gOptions.GridSize.Value = dst2.Width / 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use roi's to compute the stdev for each roi.  If small (<10), mark as featureLess (white)."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst1 = If(src.Channels <> 1, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src.Clone)
        stdevList.Clear()
        Dim mean As cv.Scalar, stdev As cv.Scalar
        For Each roi In task.gridList
            cv.Cv2.MeanStdDev(dst1(roi), mean, stdev)
            stdevList.Add(stdev(0))
        Next

        stdevAverage = stdevList.Average
        dst3.SetTo(0)
        rects.Clear()
        For i = 0 To stdevList.Count - 1
            Dim roi = task.gridList(i)
            If stdevList(i) < stdevAverage Then
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








Public Class FeatureROI_Color : Inherits VB_Algorithm
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        findSlider("Add Weighted %").Value = 70
        gOptions.GridSize.Value = dst2.Width / 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
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







Public Class FeatureROI_Canny : Inherits VB_Algorithm
    Dim canny As New Edge_Canny
    Dim devGrid As New FeatureROI_Basics
    Public Sub New()
        gOptions.GridSize.Value = dst2.Width / 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
        desc = "Create the stdev grid with the input image, then create the stdev grid for the canny output, then combine them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        canny.Run(src)
        dst3 = canny.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        devGrid.Run(src Or dst3)
        dst2 = devGrid.dst2
    End Sub
End Class








Public Class FeatureROI_Sorted : Inherits VB_Algorithm
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
        gOptions.GridSize.Value = dst2.Width / 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
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







Public Class FeatureROI_ColorSplit : Inherits VB_Algorithm
    Dim devGrid As New FeatureROI_Sorted
    Public Sub New()
        devGrid.maskVal = 255
        gOptions.GridSize.Value = dst2.Width / 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
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







Public Class FeatureROI_Correlation : Inherits VB_Algorithm
    Public gather As New FeatureROI_Basics
    Dim plot As New Plot_OverTimeSingle
    Dim options As New Options_Features
    Public Sub New()
        findSlider("Feature Correlation Threshold").Value = 90
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






Public Class FeatureROI_LowStdev : Inherits VB_Algorithm
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





Public Class FeatureROI_LowStdevCorrelation : Inherits VB_Algorithm
    Dim gather As New FeatureROI_LowStdev
    Dim correlations As New List(Of Single)
    Dim options As New Options_Features
    Public Sub New()
        findSlider("Feature Correlation Threshold").Value = 50
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









'Public Class FeatureROI_CorrelationSearch : Inherits VB_Algorithm
'    Dim gather As New FeatureROI_Basics
'    Dim correlations As New List(Of Single)
'    Public cameraMotion As Boolean
'    Dim options As New Options_Features
'    Public Sub New()
'        findSlider("Feature Correlation Threshold").Value = 99
'        desc = "Manage the features using correlation.  Find roi's on the heartbeat."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        gather.Run(dst1)
'        dst2 = gather.dst2

'        Static lastImage As cv.Mat = dst1.Clone
'        Static lastRects As New List(Of cv.Rect)(gather.rects)

'        Dim correlationMat As New cv.Mat
'        correlations.Clear()
'        Dim motionCount As Integer
'        For Each roi In gather.rects
'            cv.Cv2.MatchTemplate(dst1(roi), lastImage(roi), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
'            Dim corr = correlationMat.Get(Of Single)(0, 0)
'            If corr < options.correlationMin Then setTrueText(Format(corr, fmt1), roi.TopLeft)
'            If corr < options.correlationMin Then motionCount += 1
'        Next

'        cameraMotion = motionCount / gather.rects.Count > 0.9
'        labels(2) = "Camera Motion = " + CStr(cameraMotion)
'        lastImage = dst1.Clone
'    End Sub
'End Class






Public Class FeatureROI_LeftRight : Inherits VB_Algorithm
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






Public Class FeatureROI_LeftRightMatch : Inherits VB_Algorithm
    Dim gather As New FeatureROI_Basics
    Dim matchList As New List(Of pointPair)
    Dim roiMatch As New List(Of cv.Rect)
    Dim ptList As New List(Of cv.Point)
    Dim clickPoint As cv.Point, picTag As Integer
    Dim options As New Options_Features
    Dim maxDisparity As Integer
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Capture the above average standard deviation roi's for the left and right images."
    End Sub
    Public Sub setClickPoint(pt As cv.Point, _pictag As Integer)
        clickPoint = pt
        picTag = _pictag
        task.drawRect = task.gridList(task.gridMap.Get(Of Integer)(pt.Y, pt.X))
        task.drawRectUpdated = True
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        gather.Run(src)
        dst2 = gather.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        labels = gather.labels

        'Dim index As Integer
        'Dim roiList = gather.gRight.rects
        'Dim correlationMat As New cv.Mat
        'roiMatch.Clear()
        'ptList.Clear()
        'For Each roi In gather.gLeft.rects
        '    Dim correlations As New List(Of Single)
        '    Dim pt = roi.TopLeft
        '    For index = index To roiList.Count - 1
        '        If roiList(index).Y > pt.Y Then Exit For
        '        cv.Cv2.MatchTemplate(task.leftView(roi), task.rightView(roiList(index)), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
        '        correlations.Add(correlationMat.Get(Of Single)(0, 0))
        '    Next
        '    If correlations.Count > 0 Then
        '        Dim best = correlations.IndexOf(correlations.Max)
        '        If correlations(best) >= options.correlationMin Then
        '            matchList.Add(New pointPair(pt, roiList(best).TopLeft))
        '            roiMatch.Add(roi)
        '            ptList.Add(pt)
        '        End If
        '    End If
        'Next

        'For Each mp In matchList
        '    dst2.Circle(mp.p1, task.dotSize, task.highlightColor, -1)
        '    dst3.Circle(mp.p1, task.dotSize, task.highlightColor, -1)
        'Next

        'If task.mouseClickFlag Then setClickPoint(task.clickPoint, task.mousePicTag)
        'If clickPoint = newPoint And matchList.Count > 0 Then setClickPoint(matchList(0).p1, 2)
        'Dim indexClick = ptList.IndexOf(task.drawRect.TopLeft)
        'dst1.SetTo(0)
        'If indexClick >= 0 Then
        '    Dim mp1 = matchList(indexClick)
        '    dst1.Circle(mp1.p1, task.dotSize, task.highlightColor, -1, task.lineType)
        '    dst1.Circle(mp1.p2, task.dotSize, task.highlightColor, -1, task.lineType)
        'End If
    End Sub
End Class