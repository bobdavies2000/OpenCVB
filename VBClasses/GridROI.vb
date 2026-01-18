Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class GridROI_Basics : Inherits TaskParent
        Public rects As New List(Of cv.Rect)
        Public meanList As New List(Of Single)
        Public stdevList As New List(Of Single)
        Public stdevAverage As Single
        Public Sub New()
            task.gOptions.GridSlider.Value = dst2.Width \ 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
            dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Compute the stdev for each roi.  If small (<10), mark as featureLess."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = If(src.Channels() <> 1, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src.Clone)
            stdevList.Clear()
            meanList.Clear()
            Dim mean As cv.Scalar, stdev As cv.Scalar
            For Each roi In task.gridRects
                cv.Cv2.MeanStdDev(dst1(roi), mean, stdev)
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
                    dst3.Rectangle(roi, white, -1)
                Else
                    rects.Add(roi)
                End If
            Next
            If task.heartBeat Then
                labels(2) = CStr(rects.Count) + " of " + CStr(task.gridRects.Count) + " roi's had above average standard deviation (average = " +
                            Format(stdevList.Average, fmt1) + ")"
            End If

            dst2 = ShowAddweighted(dst3, dst1, labels(2))
        End Sub
    End Class








    Public Class NR_GridROI_Color : Inherits TaskParent
        Public Sub New()
            task.gOptions.GridSlider.Value = dst2.Width \ 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
            dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Compute the stdev for each roi.  If small (<10), mark as featureLess."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim stdevList0 As New List(Of Single)
            Dim stdevList1 As New List(Of Single)
            Dim stdevList2 As New List(Of Single)
            Dim mean As cv.Scalar, stdev As cv.Scalar
            For Each roi In task.gridRects
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
                Dim roi = task.gridRects(i)
                If stdevList0(i) < avg0 And stdevList1(i) < avg1 And stdevList2(i) < avg2 Then
                    dst3.Rectangle(roi, white, -1)
                End If
            Next
            labels(3) = "Stdev average X/Y/Z = " + CInt(stdevList0.Average).ToString + ", " + CInt(stdevList1.Average).ToString + ", " + CInt(stdevList2.Average).ToString

            dst2 = ShowAddweighted(dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR), src, labels(2))
        End Sub
    End Class







    Public Class GridROI_Sorted : Inherits TaskParent
        Public sortedStd As New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingle)
        Public bgrList As New List(Of cv.Vec3b)
        Public roiList As New List(Of cv.Rect)
        Public categories() As Integer
        Public options As New Options_StdevGrid
        Public maskVal As Integer = 255
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            task.gOptions.GridSlider.Value = dst2.Width \ 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
            If standalone = False Then maskVal = 1
            labels(2) = "Use the AddWeighted slider to observe where stdev is above average."
            desc = "Sort the roi's by the sum of their bgr stdev's to find the least volatile regions"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim meanS As cv.Scalar, stdev As cv.Scalar
            sortedStd.Clear()
            bgrList.Clear()
            roiList.Clear()
            ReDim categories(9)
            For Each roi In task.gridRects
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

                Dim color As cv.Vec3b = Choose(colorIndex, black.ToVec3b, white.ToVec3b, grayColor.ToVec3b,
                                            yellow.ToVec3b, purple.ToVec3b, teal.ToVec3b,
                                            blue.ToVec3b, green.ToVec3b, red.ToVec3b)
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

            If standaloneTest() Then dst3 = ShowAddweighted(dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR), src, labels(3))

            labels(3) = $"{count} roi's or " + Format(count / sortedStd.Count, "0%") + " have an average stdev sum of " +
                    Format(avg, fmt1) + " or less"
        End Sub
    End Class







    Public Class NR_NF_GridROI_ColorSplit : Inherits TaskParent
        Dim devGrid As New GridROI_Sorted
        Public Sub New()
            devGrid.maskVal = 255
            task.gOptions.GridSlider.Value = dst2.Width \ 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
            desc = "Split each roi into one of 9 categories - black, white, gray, yellow, purple, teal, blue, green, or red - based on the stdev for the roi"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
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







    Public Class NR_GridROI_CorrelationMotion : Inherits TaskParent
        Public gather As New GridROI_Basics
        Dim plot As New Plot_OverTimeSingle
        Dim options As New Options_Features
        Public Sub New()
            desc = "Use the grid-based correlations with the previous image to determine if there was camera motion"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst1 = If(src.Channels() <> 1, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src.Clone)
            gather.Run(dst1)
            dst2 = gather.dst2

            Static lastImage As cv.Mat = dst1.Clone

            Dim correlationMat As New cv.Mat
            Dim motionCount As Integer
            For i = 0 To gather.stdevList.Count - 1
                Dim roi = task.gridRects(i)
                If gather.stdevList(i) >= gather.stdevAverage Then
                    cv.Cv2.MatchTemplate(dst1(roi), lastImage(roi), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
                    Dim corr = correlationMat.Get(Of Single)(0, 0)
                    If corr < task.fCorrThreshold Then SetTrueText(Format(corr, fmt1), roi.TopLeft)
                    If corr < task.fCorrThreshold Then motionCount += 1
                End If
            Next

            plot.plotData = motionCount
            plot.min = -1
            plot.Run(src)
            dst3 = plot.dst2

            labels(2) = CStr(gather.rects.Count) + " of " + CStr(task.gridRects.Count) + " roi's had above average standard deviation."
            lastImage = dst1.Clone
        End Sub
    End Class






    Public Class GridROI_LowStdev : Inherits TaskParent
        Public rects As New List(Of cv.Rect)
        Dim gather As New GridROI_Basics
        Public Sub New()
            desc = "Isolate the roi's with low stdev"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = If(src.Channels() <> 1, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src.Clone)
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





    Public Class NR_GridROI_LowStdevCorrelation : Inherits TaskParent
        Dim gather As New GridROI_LowStdev
        Dim correlations As New List(Of Single)
        Dim options As New Options_Features
        Dim saveStdev As New List(Of Single)
        Public Sub New()
            desc = "Display the correlation coefficients for roi's with low standard deviation."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst1 = If(src.Channels() <> 1, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src.Clone)
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
                If saveCorrs(i) < task.fCorrThreshold Then SetTrueText(Format(saveCorrs(i), fmt2), saveRects(i).TopLeft)
                If saveCorrs(i) < task.fCorrThreshold Then SetTrueText(Format(saveStdev(i), fmt2), saveRects(i).TopLeft, 3)
            Next

            lastImage = dst1.Clone
        End Sub
    End Class






    Public Class NR_GridROI_LR : Inherits TaskParent
        Public gLeft As New GridROI_Basics
        Public gRight As New GridROI_Basics
        Public Sub New()
            desc = "Capture the above average standard deviation roi's for the left and right images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            gLeft.Run(task.leftView)
            dst2 = gLeft.dst2
            labels(2) = CStr(gLeft.rects.Count) + " roi's had above average standard deviation in the left image"

            gRight.Run(task.rightView)
            dst3 = gRight.dst2
            labels(3) = CStr(gRight.rects.Count) + " roi's had above average standard deviation in the right image"
        End Sub
    End Class






    Public Class NR_NF_GridROI_LRClick : Inherits TaskParent
        Dim gather As New GridROI_Basics
        Dim ClickPoint As cv.Point, picTag As Integer
        Dim options As New Options_Features
        Public Sub New()
            task.gOptions.GridSlider.Value = 16
            If standalone Then task.gOptions.displayDst1.Checked = True
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels(2) = "Click the above average stdev roi's (the darker regions) to find corresponding roi in the right image."
            desc = "Capture the above average standard deviation roi's for the left and right images."
        End Sub
        Public Sub setClickPoint(pt As cv.Point, _pictag As Integer)
            ClickPoint = pt
            picTag = _pictag
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst0 = src.Clone
            dst3 = If(task.rightView.Channels() <> 3, task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.rightView.Clone)
            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If task.rightView.Channels() <> 1 Then task.rightView = task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            gather.Run(src)
            dst2 = gather.dst2
            labels = gather.labels

            If gather.rects.Count = 0 Then Exit Sub
            If task.mouseClickFlag Then setClickPoint(task.clickPoint, task.mousePicTag)
            If ClickPoint = newPoint Then setClickPoint(gather.rects(gather.rects.Count / 2).TopLeft, 2)
            Dim gridIndex As Integer = task.gridMap.Get(Of Integer)(ClickPoint.Y, ClickPoint.X)
            Dim roi = task.gridRects(gridIndex)
            dst2.Rectangle(roi, white, task.lineWidth)

            Dim correlationMat As New cv.Mat
            Dim corr As New List(Of Single)
            For j = 0 To roi.X - 1
                Dim r = New cv.Rect(j, roi.Y, roi.Width, roi.Height)
                cv.Cv2.MatchTemplate(src(roi), task.rightView(r), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
                corr.Add(correlationMat.Get(Of Single)(0, 0))
            Next

            If corr.Count = 0 Then
                SetTrueText("No corresponding roi found", 2)
            Else
                Dim maxCorr = corr.Max
                If maxCorr < task.fCorrThreshold Then
                    SetTrueText("Correlation " + Format(maxCorr, fmt3) + " is less than " + Format(task.fCorrThreshold, fmt1), 1)
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
                    dst3.Rectangle(rectRight, task.highlight, task.lineWidth)
                    dst0.Rectangle(roi, task.highlight, task.lineWidth)
                    dst1.SetTo(0)
                    DrawCircle(dst1, roi.TopLeft, task.DotSize, task.highlight)
                    DrawCircle(dst1, rectRight.TopLeft, task.DotSize + 2, task.highlight)
                    Dim pt = New cv.Point(rectRight.X, roi.Y + 5)
                    SetTrueText(CStr(offset) + " pixel offset" + vbCrLf + "Larger = Right", pt, 1)
                    SetTrueText(strOut, 1)
                    labels(3) = "Corresponding roi highlighted in yellow.  Average stdev = " + Format(gather.stdevAverage, fmt3)
                End If
            End If
        End Sub
    End Class






    Public Class NR_NF_GridROI_LRAll : Inherits TaskParent
        Dim gather As New GridROI_Basics
        Dim options As New Options_Features
        Public sortedRects As New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingleInverted)
        Public Sub New()
            task.gOptions.GridSlider.Value = 16
            labels(3) = "The highlighted roi's are those high stdev roi's with the highest correlation between left and right images."
            desc = "Find all the roi's with high stdev and high correlation between left and right images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst3 = If(task.rightView.Channels() <> 3, task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR), task.rightView.Clone)
            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If task.rightView.Channels() <> 1 Then task.rightView = task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            gather.Run(src)
            dst2 = gather.dst2

            If gather.rects.Count = 0 Then Exit Sub

            Dim correlationMat As New cv.Mat
            sortedRects.Clear()
            For Each roi In gather.rects
                If roi.X = 0 Then Continue For
                Dim r = New cv.Rect(0, roi.Y, roi.X, roi.Height)
                cv.Cv2.MatchTemplate(src(roi), task.rightView(r), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
                Dim mm = vbc.GetMinMax(correlationMat)
                If mm.maxVal >= task.fCorrThreshold Then sortedRects.Add(mm.maxVal, New cv.Rect(mm.maxLoc.X, roi.Y, roi.Width, roi.Height))
            Next
            labels(2) = CStr(sortedRects.Count) + " roi's had left/right correlation higher than " + Format(task.fCorrThreshold, fmt3)

            For Each roi In sortedRects.Values
                dst3.Rectangle(roi, task.highlight, task.lineWidth)
            Next
        End Sub
    End Class






    Public Class GridROI_Canny : Inherits TaskParent
        Dim edges As New Edge_Basics
        Public Sub New()
            If task.bricks Is Nothing Then task.bricks = New Brick_Basics
            task.gOptions.GridSlider.Value = dst2.Width \ 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
            desc = "Find all the GridCells with edges in them."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(src)
            dst3 = edges.dst2

            dst2.SetTo(0)
            For Each brick In task.bricks.brickList
                If dst3(brick.rect).CountNonZero Then src(brick.rect).CopyTo(dst2(brick.rect))
            Next
        End Sub
    End Class






    Public Class NR_GridROI_Sobel : Inherits TaskParent
        Dim edges As New GridROI_Canny
        Dim sobel As New Edge_Sobel
        Public Sub New()
            task.gOptions.GridSlider.Value = dst2.Width \ 40 ' arbitrary but the goal is to get a reasonable (< 500) number of roi's.
            desc = "Find all the GridCells with edges in them."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(src)
            dst2 = edges.dst2

            sobel.Run(dst2)
            dst3 = sobel.dst2
        End Sub
    End Class
End Namespace