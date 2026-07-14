Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Motion_Basics_TA : Inherits TaskParent
    Public motionSort As New List(Of Integer) ' sorted in order of grid rect index (without actually sorting.)
    Public diff As New Diff_Basics
    Public motionMask As Mat = New Mat(dst2.Size, MatType.CV_8U, 255)
    Public motionRightMask As Mat = New Mat(dst2.Size, MatType.CV_8U, 255) ' motion in the right image.
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
        desc = "Find all the grid rects that had motion since the last frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        SetTrueText("Use the Feature Options 'Color Diff Threshold' or " + vbCrLf +
                    "the Feature Option 'Motion pixel threshold'." + vbCrLf +
                    " to adjust accuracy of accumulated image.", 3)

        If src.Channels <> 1 Then src = task.gray.Clone
        If task.optionsChanged Or task.heartBeat Then
            dst2 = src.Clone
            motionMask.SetTo(255)
            Exit Sub
        End If

        diff.lastFrame = dst2
        diff.Run(src)

        motionSort.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim diffCount = CountNonZero(diff.dst2(task.gridRects(i)))
            If diffCount >= task.motionThreshold Then motionSort.Add(i)
        Next

        motionMask.SetTo(0)
        ' The following loop adds the list4 Neighbors - it is an alternative way to reduce artifacts.
        Dim nabeList As New HashSet(Of Integer)
        For Each index In motionSort
            For Each nabeIndex In task.gridNabes(index)
                If nabeList.Contains(nabeIndex) = False Then
                    nabeList.Add(nabeIndex)
                    Dim rect = task.gridRects(nabeIndex)
                    src(rect).CopyTo(dst2(rect))
                    motionMask(rect).SetTo(255)
                End If
            Next
        Next

        If motionSort.Count / task.gridRects.Count > 0.5 Then
            motionMask.SetTo(255)
            src.CopyTo(dst2)
            labels(2) = "Image below is accumulated using motion mask.  Grid rects with motion: all of them."
        Else
            labels(2) = "Image below is accumulated using motion mask.  Grid rects with motion: " + CStr(nabeList.Count)
        End If
        dst3 = motionMask

        'dst1 = task.rightView.Clone
        'motionRight.Run(task.rightView)
        'motionRightMask = motionRight.dst3
        'dst1.SetTo(255, motionRightMask)

        'motionRight.SetTo(0)
        'nabeList.Clear()
        'For Each index In motionSort
        '    For Each nabeIndex In task.gridNabes(index)
        '        If nabeList.Contains(nabeIndex) = False Then
        '            Dim rect = task.gridRects(nabeIndex)
        '            Dim depth = task.pcSplit(2)(rect).Mean(task.depthmask(rect))(0)
        '            If depth = 0 Then
        '                ' must handle the case where there is no depth data...
        '                Continue For
        '            End If
        '            rect.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth
        '            If rect.X >= 0 And rect.X + rect.Width < dst2.Width Then
        '                motionRight(rect).SetTo(255)
        '            End If
        '            nabeList.Add(nabeIndex)
        '        End If
        '    Next
        'Next
        'dst1.SetTo(255, motionRight)
    End Sub
End Class




Public Class Motion_Validate : Inherits TaskParent
    Dim diff As New Diff_Basics
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "Current grayscale image"
        labels(2) = "Grayscale image constructed from previous images + motion updates."
        labels(3) = "Highlighted difference of task.gray and the one built with the motion data.  "
        desc = "Compare task.gray to constructed images to verify Motion_Basics_TA is working"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.gray
        dst2 = task.motion.dst2.Clone

        diff.lastFrame = dst2
        diff.Run(dst1)
        Threshold(diff.dst3, dst3, task.colorDiffThreshold, 255, ThresholdTypes.Binary)

        Dim count = CountNonZero(dst3)
        strOut = "Pixels different from camera image: " + CStr(count) + " (" +
                  (count / src.Total).ToString("0%") + ")" + vbCrLf +
                 "Grid rects with more than " + CStr(task.motionThreshold) +
                 " pixels different: " + CStr(task.motion.motionSort.Count)
        SetTrueText(strOut, 3)
    End Sub
End Class




Public Class XR_Motion_Basics : Inherits TaskParent
    Public motionSort As New List(Of Integer)
    Public diff As New Diff_Basics
    Public motionMask As Mat = New Mat(dst2.Size, MatType.CV_8U, 255)
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
        labels(3) = "The motion mask"
        desc = "Find all the grid rects that had motion since the last frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray.Clone
        If task.optionsChanged Then dst2 = src.Clone

        diff.lastFrame = dst2
        diff.Run(src)

        Dim gridList As New SortedList(Of Integer, Integer)
        task.motionThreshold = task.fOptions.MotionPixelSlider.Value
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim diffCount = CountNonZero(diff.dst2(r))
            If diffCount >= task.motionThreshold Then
                For Each index In task.gridNabes(i)
                    If gridList.Keys.Contains(index) = False Then gridList.Add(index, index)
                Next
            End If
        Next

        motionSort = New List(Of Integer)(gridList.Keys)
        motionMask.SetTo(0)
        For Each index In motionSort
            Dim rect = task.gridRects(index)
            src(rect).CopyTo(dst2(rect))
            motionMask(rect).SetTo(255)
        Next
        dst3 = motionMask

        labels(2) = "Grid rects with motion: " + CStr(motionSort.Count)
    End Sub
End Class






Public Class Motion_ValidateColor : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        labels(2) = "Current color image."
        labels(3) = "Accumulated color image."
        desc = "Compare visually task.color to the constructed images to verify motion mask."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.color
        If task.firstPass Then dst3 = task.color.Clone Else task.color.CopyTo(dst3, task.motion.motionMask)
    End Sub
End Class






Public Class XR_Motion_ValidateRight : Inherits TaskParent
    Dim diff As New Diff_Basics
    Dim motionRight As New Motion_Right
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "Current right image"
        labels(2) = "Right image constructed from previous images + motion updates."
        labels(3) = "Highlighted difference of task.rightView and the one built with the motion data."
        desc = "Validate that the right image motion mask (Motion_RightImage) is working properly."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        motionRight.Run(emptyMat)

        dst1 = task.rightView.Clone
        dst2 = motionRight.motion.dst2.Clone()

        diff.lastFrame = dst2
        diff.Run(dst1)
        Threshold(diff.dst3, dst3, task.motionThreshold, 255, ThresholdTypes.Binary)

        SetTrueText("Pixels different from camera image: " + CStr(CountNonZero(diff.dst2)) + vbCrLf +
                        "Grid rects with more than " + CStr(task.motionThreshold) +
                        " pixels different: " + CStr(motionRight.motion.motionSort.Count), 3)

        For Each index In motionRight.motion.motionSort
            Rectangle(dst1, task.gridRects(index), Scalar.All(255), task.lineWidth)
        Next
    End Sub
End Class




Public Class Motion_Right : Inherits TaskParent
    Public motion As New Motion_Basics_TA
    Public Sub New()
        desc = "Build the MotionMask for the right camera."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        motion.Run(task.rightView)
        dst2 = motion.dst2
        dst3 = motion.motionMask.Clone
        labels(2) = motion.labels(2)
        labels(3) = "The motion mask for the right image"
    End Sub
End Class




Public Class Motion_Left : Inherits TaskParent
    Public motion As New Motion_Basics_TA
    Public Sub New()
        desc = "Build the MotionMask for the left camera."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        motion.Run(task.leftView)
        dst2 = motion.dst2
        dst3 = motion.motionMask.Clone
        labels(2) = motion.labels(2)
        labels(3) = "The motion mask for the left image "
    End Sub
End Class




Public Class Motion_CorrelationToLast : Inherits TaskParent
    Public cList As New List(Of Single)
    Public plotHist As New PlotBar_Basics
    Public lastFrame As New Mat(dst2.Size, MatType.CV_8U, 0)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        plotHist.createHistogram = True
        plotHist.minRange = -1.01
        plotHist.maxRange = 1.01
        plotHist.shadeValues = False
        task.gOptions.HistBinBar.Value = task.gOptions.HistBinBar.Maximum
        desc = "Measure the correlation of grid elements that appear to have changed."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray.Clone

        If task.optionsChanged Then lastFrame = src.Clone

        dst2 = src
        Dim maxCorrelation As Single = task.fOptions.MatchCorrSlider.Value / 100

        Dim count As Integer
        Dim correlationMat As New Mat
        dst3 = src.Clone
        cList.Clear()
        For Each index In task.motion.motionSort
            Dim r = task.gridRects(index)
            MatchTemplate(dst2(r), lastFrame(r), correlationMat, TemplateMatchModes.CCoeffNormed)

            Dim corr = correlationMat.Get(Of Single)(0, 0)
            cList.Add(corr)

            If corr < maxCorrelation Then
            Rectangle(dst3, r, white, task.lineWidth)
                count += 1
            Else
            Rectangle(dst3, r, black, task.lineWidth)
            End If
        Next

        lastFrame = src.Clone

        If cList.Count > 0 Then
            plotHist.Run(Mat.FromPixelData(cList.Count, 1, MatType.CV_32F, cList.ToArray))
            dst1 = plotHist.dst2

            labels(2) = "Min = " + cList.Min.ToString(fmt1) + ", Max = " + cList.Max.ToString(fmt1)
            labels(3) = CStr(count) + " had a correlation < " + maxCorrelation.ToString(fmt2) + " (" +
                                (count / cList.Count).ToString("0%") + ")" +
                                " The black squares have high correlation."
        End If
    End Sub
End Class




Public Class Motion_Correlation : Inherits TaskParent
    Public cList As New List(Of Single)
    Public plotHist As New PlotBar_Basics
    Public lastFrame As New Mat(dst2.Size, MatType.CV_8U, 0)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        plotHist.createHistogram = True
        plotHist.minRange = -1.01
        plotHist.maxRange = 1.01
        plotHist.shadeValues = False
        task.gOptions.HistBinBar.Value = task.gOptions.HistBinBar.Maximum
        desc = "Use correlation to determine differences at the grid level."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray.Clone

        If task.optionsChanged Then lastFrame = src.Clone

        dst2 = src
        Dim maxCorrelation As Single = task.fOptions.MatchCorrSlider.Value / 100

        Dim count As Integer
        Dim correlationMat As New Mat
        dst3 = src.Clone
        cList.Clear()
        For Each r In task.gridRects
            MatchTemplate(dst2(r), lastFrame(r), correlationMat, TemplateMatchModes.CCoeffNormed)

            Dim corr = correlationMat.Get(Of Single)(0, 0)
            cList.Add(corr)

            If corr < maxCorrelation Then
            Rectangle(dst3, r, white, task.lineWidth)
                count += 1
            End If
        Next

        lastFrame = src.Clone

        plotHist.Run(Mat.FromPixelData(cList.Count, 1, MatType.CV_32F, cList.ToArray))
        dst1 = plotHist.dst2

        labels(2) = "Min = " + cList.Min.ToString(fmt1) + ", Max = " + cList.Max.ToString(fmt1)
        labels(3) = CStr(count) + " had a correlation < " + maxCorrelation.ToString(fmt2) + " (" +
                            (count / cList.Count).ToString("0%") + ")"

    End Sub
End Class




Public Class Motion_Throttle : Inherits TaskParent
    Dim motionPlot As New Motion_CorrelationToLast
    Public strList As New List(Of String)
    Public Sub New()
        task.gOptions.showMotionMask.Checked = True
        desc = "Adjust the color difference threshold based on the histogram of the grid rects."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then strList.Clear()

        ' only check no more than once a heartbeat..
        If task.heartBeat Then

            Dim nextMsg As String = ""
            motionPlot.Run(task.gray.Clone)
            If motionPlot.plotHist.histArray Is Nothing Then Exit Sub

            Dim histArray = motionPlot.plotHist.histArray
            Dim identicals = histArray(histArray.Length - 1)

            dst3 = motionPlot.dst1

            ' Only update the slider when there is a lot of motion - more than X% ...
            If task.motion.motionSort.Count / task.gridRects.Count > 0.1 Then
                Dim currThreshold = task.fOptions.ColorDiffSlider.Value
                Dim identicalRatio = identicals / task.motion.motionSort.Count
                Dim increasing As Boolean
                Dim decreasing As Boolean
                If identicalRatio < 0.05 And currThreshold > 1 Then
                    task.fOptions.ColorDiffSlider.Value -= 1
                    decreasing = True
                ElseIf identicalRatio > 0.01 And currThreshold < (task.fOptions.ColorDiffSlider.Maximum - 1) Then
                    task.fOptions.ColorDiffSlider.Value += 1
                    increasing = True
                End If

                nextMsg = CStr(task.motion.motionSort.Count) + vbTab + vbTab + (1 - identicalRatio).ToString("0%") +
                                                    vbTab + vbTab + identicalRatio.ToString("0%")
                If increasing Then
                    nextMsg += vbTab + vbTab + "Increasing to " + CStr(currThreshold + 1)
                ElseIf decreasing Then
                    nextMsg += vbTab + vbTab + "Decreasing to " + CStr(currThreshold - 1)
                Else
                    nextMsg += vbTab + vbTab + "Stable at " + CStr(currThreshold)
                End If

                If nextMsg.Trim.Length > 0 Then strList.Add(nextMsg)
            End If

            If strList.Count > task.maxTrueTextLines Then strList.RemoveAt(0)
        End If

        strOut = "Motion Cells" + vbTab + "%Motion" + vbTab + "%Identical" + vbTab +
                             "Increase/Decrease" + vbCrLf
        For Each nextStr In strList
            strOut += nextStr + vbCrLf
        Next
        SetTrueText(strOut, 2)
    End Sub
End Class






Public Class Motion_CloudGrid : Inherits TaskParent
    Public motionSort As New List(Of Integer)
    Dim options As New Options_MotionCloud
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True

        dst2 = New Mat(dst2.Size, MatType.CV_8U, 255)
        desc = "Find all the grid rects that had motion since the last frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        ' assume the disparity can be off by options.pixelError pixels
        Dim disparityCoefficient = options.pixelError / (task.calibData.baseline * task.calibData.leftIntrinsics.fx)

        Static lastDepth As Mat = task.pcSplit(2).Clone
        Static lastMask As Mat = task.depthmask.Clone
        motionSort.Clear()
        dst2.SetTo(0)
        Dim index As Integer
        Dim motionRGB As New List(Of Integer)(task.motion.motionSort)
        If motionRGB.Count = 0 Then motionRGB.Add(-1)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            ' make sure the RGB motion is present as well
            If i = motionRGB(index) Then
                motionSort.Add(i)
                dst2(r).SetTo(255)
                If index < motionRGB.Count - 1 Then index += 1
                Continue For
            End If
            Dim depth = Mean(task.pcSplit(2)(r), task.depthmask(r)).Val0
            If depth > 0 Then
                Dim depthError = disparityCoefficient * depth * depth
                Dim depthLast = Mean(lastDepth(r), lastMask(r)).Val0
                If Math.Abs(depth - depthLast) > depthError Then
                    dst2(r).SetTo(255)
                    motionSort.Add(i)
                End If
            End If
        Next

        For Each index In task.motion.motionSort
            If motionSort.Contains(index) = False Then
                motionSort.Add(index)
            End If
        Next

        lastDepth = task.pcSplit(2).Clone
        lastMask = task.depthmask.Clone
        labels(2) = "PointCloud grid rects with motion: " + CStr(motionSort.Count)
    End Sub
End Class





Public Class Motion_CloudPixel : Inherits TaskParent
    Dim options As New Options_MotionCloud
    Dim optionsAccum As New Options_AddWeighted
    Public Sub New()
        dst0 = New Mat(dst0.Size, MatType.CV_32F, 0)
        OptionParent.FindSlider("Accumulation weight of each image X100").Value = 50
        desc = "Find pixels whose variability exceeds the error estimate."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        optionsAccum.Run()

        Static lastDepth As Mat = task.pcSplit(2).Clone

        ' assume the disparity can be off by options.pixelError pixels
        Dim disparityCoefficient As Single = options.pixelError / (task.calibData.baseline * task.calibData.leftIntrinsics.fx)

        Dim errorMat As New Mat
        Multiply(task.pcSplit(2), task.pcSplit(2), errorMat)
        errorMat *= disparityCoefficient

        Dim depthDelta As New Mat
        Absdiff(task.pcSplit(2), lastDepth, depthDelta)

        Subtract(depthDelta, errorMat, dst2)
        Threshold(dst2, dst2, 0, 255, ThresholdTypes.Binary)
        ' dst2.ConvertTo(dst2, MatType.CV_8U)

        AccumulateWeighted(dst2, dst0, optionsAccum.accumWeighted, New Mat)
        dst0.ConvertTo(dst2, MatType.CV_8U)
        Threshold(dst2, dst2, 0, 255, ThresholdTypes.Binary)

        lastDepth = task.pcSplit(2).Clone
    End Sub
End Class





Public Class Motion_Featureless : Inherits TaskParent
    Dim edges As New Edge_Featureless
    Dim plotdots As New PlotTime_FixedScale
    Public Sub New()
        plotdots.plotCount = 1
        plotdots.showScale = False
        plotdots.fixedScale = True
        plotdots.minScale = 0

        desc = "The count of featureless cells is used to determine if the camera has moved."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.grayOriginal)
        dst2 = edges.dst2
        labels(2) = edges.labels(2)

        plotdots.maxScale = task.gridRects.Count
        plotdots.plotData(0) = edges.fLess.brickList.Count
        plotdots.Run(src)
        dst3 = plotdots.dst2
        labels(3) = plotdots.labels(2)

        SetTrueText(edges.strOut, 3)
    End Sub
End Class
