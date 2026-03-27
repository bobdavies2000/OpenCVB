Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Motion_Basics_TA : Inherits TaskParent
        Public motionSort As New List(Of Integer)
        Public diff As New Diff_Basics
        Public motionMask As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 255)
        Public Sub New()
            If standalone Then task.gOptions.showMotionMask.Checked = True
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
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
                Dim diffCount = diff.dst2(r).CountNonZero
                If diffCount >= task.motionThreshold Then
                    For Each index In task.grid.gridNeighbors(i)
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

            labels(2) = "Grid rects with motion: " + CStr(motionSort.Count)
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
            dst1 = task.gray.Clone
            dst2 = task.motion.dst2.Clone

            diff.lastFrame = dst2.Clone
            diff.Run(dst1.Clone)
            dst3 = diff.dst3.Threshold(task.colorDiffThreshold, 255, cv.ThresholdTypes.Binary)

            SetTrueText("Pixels different from camera image: " + CStr(diff.dst3.CountNonZero) + vbCrLf +
                            "Grid rects with more than " + CStr(task.motionThreshold) +
                            " pixels different: " + CStr(task.motion.motionSort.Count), 3)
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






    Public Class NR_Motion_ValidateRight : Inherits TaskParent
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
            dst3 = diff.dst3.Threshold(task.motionThreshold, 255, cv.ThresholdTypes.Binary)

            SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(task.motionThreshold) +
                    " pixels different: " + CStr(motionRight.motion.motionSort.Count), 3)

            For Each index In motionRight.motion.motionSort
                dst1.Rectangle(task.gridRects(index), 255, task.lineWidth)
            Next
        End Sub
    End Class






    Public Class Motion_Cloud_TA : Inherits TaskParent
        Public originalPointcloud As cv.Mat
        Public motionSort As New List(Of Integer)
        Dim diff As New Diff_Depth32f
        Public motionMask As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 255)
        Public Sub New()
            If standalone Then task.gOptions.showMotionMask.Checked = True
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
            dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
            labels(3) = "The motion mask"
            desc = "Find all the grid rects that had motion since the last frame."
        End Sub
        Public Shared Function checkNanInf(pc As cv.Mat) As cv.Mat
            ' these don't work because there are NaN's and Infinity's (both can be present)
            ' cv.Cv2.PatchNaNs(pc, 0.0) 
            ' Dim mask As New cv.Mat
            ' cv.Cv2.Compare(pc, pc, mask, cv.CmpType.EQ)

            Dim count As Integer
            Dim vec As New cv.Vec3f(0, 0, 0)
            ' The stereolabs camera has some weird -inf and inf values in the Y-plane 
            ' with and without gravity transform.  Probably my fault but just fix it here.
            For y = 0 To pc.Rows - 1
                For x = 0 To pc.Cols - 1
                    Dim val = pc.Get(Of cv.Vec3f)(y, x)
                    If Single.IsNaN(val(0)) Or Single.IsInfinity(val(0)) Then
                        pc.Set(Of cv.Vec3f)(y, x, vec)
                        count += 1
                    End If
                Next
            Next

            'Dim mean As cv.Scalar, stdev As cv.Scalar
            'cv.Cv2.MeanStdDev(originalPointcloud, mean, stdev)
            'Debug.WriteLine("Before Motion mean " + mean.ToString())

            Return pc
        End Function
        Private Sub preparePointcloud()
            If task.gOptions.gravityPointCloud.Checked Then
                '******* this is the gravity rotation (" * task.gMatrix") *******
                task.gravityCloud = (task.pointCloud.Reshape(1,
                                    task.rows * task.cols) * task.gMatrix).ToMat.Reshape(3, task.rows)
                task.pointCloud = task.gravityCloud
            End If

            ' The stereolabs camera has some weird -inf and inf values in the Y-plane 
            ' with and without gravity transform.  Probably my fault but just fix it here.
            If task.Settings.cameraName = "StereoLabs ZED 2/2i" Then
                task.pointCloud = checkNanInf(task.pointCloud)
            End If

            task.pcSplit = task.pointCloud.Split

            If task.optionsChanged Then
                task.maxDepthMask = New cv.Mat(task.pcSplit(2).Size, cv.MatType.CV_8U, 0)
            End If

            If task.gOptions.TruncateDepth.Checked Then
                task.pcSplit(2) = task.pcSplit(2).Threshold(task.MaxZmeters,
                                                          task.MaxZmeters, cv.ThresholdTypes.Trunc)
                task.maxDepthMask = task.pcSplit(2).InRange(task.MaxZmeters,
                                                          task.MaxZmeters).ConvertScaleAbs()
                cv.Cv2.Merge(task.pcSplit, task.pointCloud)
            End If

            task.depthmask = task.pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            task.noDepthMask = Not task.depthmask

            If task.xRange <> task.xRangeDefault Or task.yRange <> task.yRangeDefault Then
                Dim xRatio = task.xRangeDefault / task.xRange
                Dim yRatio = task.yRangeDefault / task.yRange
                task.pcSplit(0) *= xRatio
                task.pcSplit(1) *= yRatio

                cv.Cv2.Merge(task.pcSplit, task.pointCloud)
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            originalPointcloud = task.pointCloud.Clone

            If task.optionsChanged Then
                If task.rangesCloud Is Nothing Then
                    Dim rx = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
                    Dim ry = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
                    Dim rz = New cv.Vec2f(0, task.MaxZmeters)
                    task.rangesCloud = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1),
                                                        New cv.Rangef(ry.Item0, ry.Item1),
                                                        New cv.Rangef(rz.Item0, rz.Item1)}
                End If
            End If

            preparePointcloud()

            diff.lastFrame = dst2
            diff.Run(task.pcSplit(2))

            motionSort.Clear()
            For i = 0 To task.gridRects.Count - 1
                Dim diffCount = diff.dst2(task.gridRects(i)).CountNonZero
                If diffCount >= task.motionThreshold Then
                    For Each index In task.grid.gridNeighbors(i)
                        If motionSort.Contains(index) = False Then motionSort.Add(index)
                    Next
                End If
            Next

            motionMask.SetTo(0)
            For Each index In motionSort
                motionMask(task.gridRects(index)).SetTo(255)
            Next

            task.pcSplit(2).CopyTo(dst2, motionMask)
            If standaloneTest() Then dst3 = motionMask

            labels(2) = "Grid rects with motion: " + CStr(motionSort.Count)
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






    Public Class Motion_Throttle_TA : Inherits TaskParent
        Dim motionPlot As New Motion_CorrelationToLast
        Public strList As New List(Of String)
        Public Sub New()
            task.gOptions.showMotionMask.Checked = True
            desc = "Adjust the motion threshold based on the histogram of high-motion images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static saveHeartBeat As Boolean
            Static hitDecreasing As Boolean
            Static incr As Integer = 5
            If hitDecreasing Then incr = 1

            If task.heartBeat Then saveHeartBeat = True
            ' When there is a lot of motion - more than X% ...
            If task.motion.motionSort.Count / task.gridRects.Count > 0.1 And task.firstPass = False Then
                ' if running as the selected algorithm, just use the strlist from the task algorithm...
                If src.Channels = 3 Then
                    strList = New List(Of String)(task.motionThrottle.strList)
                Else
                    Dim nextMsg As String = ""
                    ' only check no more than once a heartbeat...
                    If saveHeartBeat Then
                        saveHeartBeat = False

                        motionPlot.Run(task.gray.Clone)

                        Dim histArray = motionPlot.plotHist.histArray
                        Dim identicals = histArray(histArray.Length - 1)

                        dst3 = motionPlot.dst1

                        Dim currThreshold = task.fOptions.MotionPixelSlider.Value
                        Dim identicalRatio = identicals / task.motion.motionSort.Count
                        Dim increasing As Boolean
                        Dim decreasing As Boolean
                        If identicalRatio < 0.05 And currThreshold > incr Then
                            task.fOptions.MotionPixelSlider.Value -= incr
                            decreasing = True
                            If task.frameCount > 10 Then hitDecreasing = True
                        ElseIf identicalRatio > 0.01 And currThreshold < task.fOptions.MotionPixelSlider.Maximum Then
                            task.fOptions.MotionPixelSlider.Value += incr
                            increasing = True
                        End If

                        nextMsg = CStr(task.motion.motionSort.Count) + vbTab + vbTab + Format(1 - identicalRatio, "0%") +
                                vbTab + vbTab + Format(identicalRatio, "0%")
                        If increasing Then
                            nextMsg += vbTab + vbTab + "Increasing to " + CStr(currThreshold + incr)
                        ElseIf decreasing Then
                            nextMsg += vbTab + vbTab + "Decreasing to " + CStr(currThreshold - incr)
                        Else
                            nextMsg += vbTab + vbTab + "Stable at " + CStr(currThreshold)
                        End If
                    End If

                    If nextMsg.Trim.Length > 0 Then strList.Add(nextMsg)

                    If strList.Count > task.maxTrueTextLines Then strList.RemoveAt(0)
                End If

                strOut = "Motion Cells" + vbTab + "%Motion" + vbTab + "%Identical" + vbTab + "Increase/Decrease" + vbCrLf
                For Each nextStr In strList
                    strOut += nextStr + vbCrLf
                Next
            End If
            SetTrueText(strOut, 2)
        End Sub
    End Class




    Public Class Motion_CorrelationToLast : Inherits TaskParent
        Public cList As New List(Of Single)
        Public plotHist As New Plot_Histogram
        Public lastFrame As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
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
            Dim correlationMat As New cv.Mat
            dst3 = src
            cList.Clear()
            For Each index In task.motion.motionSort
                Dim r = task.gridRects(index)
                dst2.Rectangle(r, white, task.lineWidth)
                cv.Cv2.MatchTemplate(dst2(r), lastFrame(r), correlationMat, cv.TemplateMatchModes.CCoeffNormed)

                Dim corr = correlationMat.Get(Of Single)(0, 0)
                cList.Add(corr)

                If corr < maxCorrelation Then
                    dst3.Rectangle(r, white, task.lineWidth)
                    count += 1
                Else
                    dst3.Rectangle(r, black, task.lineWidth)
                End If
            Next

            lastFrame = src.Clone

            If cList.Count > 0 Then
                plotHist.Run(cv.Mat.FromPixelData(cList.Count, 1, cv.MatType.CV_32F, cList.ToArray))
                dst1 = plotHist.dst2

                labels(2) = "Min = " + Format(cList.Min, fmt1) + ", Max = " + Format(cList.Max, fmt1)
                labels(3) = CStr(count) + " had a correlation < " + Format(maxCorrelation, fmt2) + " (" +
                            Format(count / cList.Count, "0%") + ")" +
                            " The black squares have high correlation."
            End If
        End Sub
    End Class




    Public Class Motion_Correlation : Inherits TaskParent
        Public cList As New List(Of Single)
        Public plotHist As New Plot_Histogram
        Public lastFrame As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
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
            Dim correlationMat As New cv.Mat
            dst3 = src
            cList.Clear()
            For Each r In task.gridRects
                cv.Cv2.MatchTemplate(dst2(r), lastFrame(r), correlationMat, cv.TemplateMatchModes.CCoeffNormed)

                Dim corr = correlationMat.Get(Of Single)(0, 0)
                cList.Add(corr)

                If corr < maxCorrelation Then
                    dst3.Rectangle(r, white, task.lineWidth)
                    count += 1
                Else
                    dst3.Rectangle(r, black, task.lineWidth)
                End If
            Next

            lastFrame = src.Clone

            If cList.Count > 0 Then
                plotHist.Run(cv.Mat.FromPixelData(cList.Count, 1, cv.MatType.CV_32F, cList.ToArray))
                dst1 = plotHist.dst2

                labels(2) = "Min = " + Format(cList.Min, fmt1) + ", Max = " + Format(cList.Max, fmt1)
                labels(3) = CStr(count) + " had a correlation < " + Format(maxCorrelation, fmt2) + " (" +
                            Format(count / cList.Count, "0%") + ")" +
                            " The black squares have high correlation."
            End If
        End Sub
    End Class

End Namespace