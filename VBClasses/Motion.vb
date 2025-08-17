Imports System.Runtime.InteropServices
Imports System.Threading
Imports cv = OpenCvSharp
Public Class Motion_Basics : Inherits TaskParent
    Public lastColor(0) As cv.Vec3f
    Public cellAge(0) As Integer
    Public motionFlags(0) As Boolean
    Public Sub New()
        task.motionMask = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Below is the difference between the current image and the dst2 at left which is composed using the motion mask."
        desc = "Isolate all motion in the scene"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If lastColor.Count <> task.gridRects.Count Then
            ReDim lastColor(task.gridRects.Count - 1)
            ReDim cellAge(task.gridRects.Count - 1)
        End If

        If task.frameCount < 3 Then dst2 = src.Clone
        Dim colorstdev As cv.Scalar, colorMean As cv.Scalar
        ReDim motionFlags(task.gridRects.Count - 1)
        Dim motionList As New List(Of Integer)
        For i = 0 To task.gridRects.Count - 1
            Dim rect = task.gridRects(i)
            cv.Cv2.MeanStdDev(src(rect), colorMean, colorstdev)
            Dim colorVec = New cv.Vec3f(colorMean(0), colorMean(1), colorMean(2))
            Dim colorChange = distance3D(colorVec, lastColor(i))
            If colorChange > task.colorDiffThreshold Then
                lastColor(i) = colorVec
                cellAge(i) = 1
                For Each index In task.grid.gridNeighbors(i)
                    If motionList.Contains(index) = False Then
                        motionFlags(index) = True
                        motionList.Add(index)
                    End If
                Next
            Else
                cellAge(i) += 1
            End If
        Next

        task.motionMask.SetTo(0)
        For Each i In motionList
            task.motionMask(task.gridRects(i)).SetTo(255)
            motionFlags(i) = True
        Next

        task.motionPercent = motionList.Count / task.gridRects.Count
        If task.motionPercent > 0.8 Then task.motionPercent = 1
        labels(2) = Format(task.motionPercent, "00%") + " of bricks had motion."

        ' some cameras have low light images for the first few frames.
        If task.gOptions.UseMotionMask.Checked = False Or task.frameCount < 3 Or task.motionPercent = 1 Then task.motionMask.SetTo(255)

        If standaloneTest() Then
            If task.gOptions.UseMotionMask.Checked Then src.CopyTo(dst2, task.motionMask)
            Static diff As New Diff_Basics
            diff.lastFrame = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            diff.Run(src)
            dst3 = diff.dst2
            SetTrueText("NOTE: the differences below should be small - no artifacts should be present." + vbCrLf +
                        "Any differences that persist should not be visible in the RGB image at left." + vbCrLf, 3)
        End If
        If task.heartBeatLT Then dst2 = src.Clone
    End Sub
End Class







Public Class Motion_BasicsValidate : Inherits TaskParent
    Dim diff As New Diff_Basics
    Public Sub New()
        task.gOptions.showMotionMask.Checked = True
        desc = "Display the difference between task.color and src to verify Motion_Basics is working"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then dst3 = src.Clone Else src.CopyTo(dst3, task.motionMask)

        diff.lastFrame = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        diff.Run(src)
        dst2 = diff.dst2

        labels(2) = "The image below is a diff of the camera image and the task.color built" +
                    " with the motion mask.  Difference in pixels = " + CStr(dst2.CountNonZero)
        labels(3) = task.motionBasics.labels(2)
    End Sub
End Class






Public Class Motion_BGSub : Inherits TaskParent
    Public bgSub As New BGSubtract_MOG2
    Dim motion As New Motion_BGSub_QT
    Public Sub New()
        desc = "Use floodfill to find all the real motion in an image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bgSub.Run(src)
        motion.Run(bgSub.dst2)
        dst2 = motion.dst2
        labels(2) = motion.labels(2)
    End Sub
End Class






Public Class Motion_BGSub_QT : Inherits TaskParent
    Public bgSub As New BGSubtract_MOG2
    Dim rectList As New List(Of cv.Rect)
    Public Sub New()
        task.redC = New RedColor_Basics
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "The option-free version of Motion_BGSub"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then
            bgSub.Run(src)
            src = bgSub.dst2
        End If

        dst2 = src

        task.redC.Run(src.Threshold(0, 255, cv.ThresholdTypes.Binary))
        If task.redC.rcList.Count < 2 Then
            rectList.Clear()
        Else
            Dim nextRect = task.redC.rcList.ElementAt(1).rect
            For i = 2 To task.redC.rcList.Count - 1
                Dim rc = task.redC.rcList.ElementAt(i)
                nextRect = nextRect.Union(rc.rect)
            Next
        End If

        If standaloneTest() Then
            If task.redC.rcList.Count > 1 Then
                labels(2) = CStr(task.redC.rcList.Count) + " RedMask cells had motion"
            Else
                labels(2) = "No motion detected"
            End If
            labels(3) = ""
        End If
    End Sub
End Class






Public Class Motion_ThruCorrelation : Inherits TaskParent
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Correlation threshold X1000", 0, 1000, 900)
            sliders.setupTrackBar("Stdev threshold for using correlation", 0, 100, 15)
            sliders.setupTrackBar("Pad size in pixels for the search area", 0, 100, 20)
        End If

        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Detect motion through the correlation coefficient"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static ccSlider = OptionParent.FindSlider("Correlation threshold X1000")
        Static padSlider = OptionParent.FindSlider("Pad size in pixels for the search area")
        Static stdevSlider = OptionParent.FindSlider("Stdev threshold for using correlation")
        Dim pad = padSlider.Value
        Dim ccThreshold = ccSlider.Value
        Dim stdevThreshold = stdevSlider.Value

        Dim input = src.Clone
        If input.Channels() <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cv.Mat = input.Clone
        dst3.SetTo(0)
        Parallel.For(0, task.gridRects.Count,
        Sub(i)
            Dim roi = task.gridRects(i)
            Dim correlation As New cv.Mat
            Dim mean As Single, stdev As Single
            cv.Cv2.MeanStdDev(input(roi), mean, stdev)
            If stdev > stdevThreshold Then
                cv.Cv2.MatchTemplate(lastFrame(roi), input(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                Dim mm As mmData = GetMinMax(correlation)
                If mm.maxVal < ccThreshold / 1000 Then
                    If (i Mod task.bricksPerCol) <> 0 Then dst3(task.gridRects(i - 1)).SetTo(255)
                    If (i Mod task.bricksPerCol) < task.bricksPerCol And i < task.gridRects.Count - 1 Then dst3(task.gridRects(i + 1)).SetTo(255)
                    If i > task.bricksPerCol Then
                        dst3(task.gridRects(i - task.bricksPerCol)).SetTo(255)
                        dst3(task.gridRects(i - task.bricksPerCol + 1)).SetTo(255)
                    End If
                    If i < (task.gridRects.Count - task.bricksPerCol - 1) Then
                        dst3(task.gridRects(i + task.bricksPerCol)).SetTo(255)
                        dst3(task.gridRects(i + task.bricksPerCol + 1)).SetTo(255)
                    End If
                    dst3(roi).SetTo(255)
                End If
            End If
        End Sub)

        lastFrame = input.Clone

        If task.heartBeat Then dst2 = src.Clone Else src.CopyTo(dst2, dst3)
    End Sub
End Class







Public Class Motion_PixelDiff : Inherits TaskParent
    Public changedPixels As Integer
    Dim changeCount As Integer, frames As Integer
    Public Sub New()
        desc = "Count the number of changed pixels in the current frame and accumulate them.  If either exceeds thresholds, then set flag = true.  " +
                    "To get the Options Slider, use " + traceName + "QT"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cv.Mat = src
        cv.Cv2.Absdiff(src, lastFrame, dst2)
        dst2 = dst2.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
        changedPixels = dst2.CountNonZero
        Dim motionTest = changedPixels > 0

        If motionTest Then changeCount += 1
        frames += 1
        If task.heartBeat Then
            strOut = "Pixels changed = " + CStr(changedPixels) + " at last heartbeat.  Since last heartbeat: " +
                     Format(changeCount / frames, "0%") + " of frames were different"
            changeCount = 0
            frames = 0
        End If
        SetTrueText(strOut, 3)
        If motionTest Then lastFrame = src
    End Sub
End Class








Public Class Motion_Grid_MP : Inherits TaskParent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Correlation Threshold", 800, 1000, 990)
        desc = "Detect Motion in the color image using multi-threading - slower than single-threaded!"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static correlationSlider = OptionParent.FindSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Then dst3 = src.Clone

        dst2 = src

        Dim updateCount As Integer
        Parallel.ForEach(Of cv.Rect)(task.gridRects,
            Sub(roi)
                Dim correlation As New cv.Mat
                cv.Cv2.MatchTemplate(src(roi), dst3(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                    Interlocked.Increment(updateCount)
                    src(roi).CopyTo(dst3(roi))
                    dst2.Rectangle(roi, white, task.lineWidth)
                End If
            End Sub)
        labels(2) = "Motion added to dst3 for " + CStr(updateCount) + " segments out of " + CStr(task.gridRects.Count)
        labels(3) = CStr(task.gridRects.Count - updateCount) + " segments out of " + CStr(task.gridRects.Count) + " had > " +
                         Format(correlationSlider.Value / 1000, "0.0%") + " correlation. "
    End Sub
End Class





Public Class Motion_Grid : Inherits TaskParent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Correlation Threshold", 800, 1000, 990)
        desc = "Detect Motion in the color image.  Rectangles outlines didn't have high correlation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static correlationSlider = OptionParent.FindSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Then dst3 = src.Clone

        Dim roiMotion As New List(Of cv.Rect)
        For Each roi In task.gridRects
            Dim correlation As New cv.Mat
            cv.Cv2.MatchTemplate(src(roi), dst3(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
            If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                src(roi).CopyTo(dst3(roi))
                roiMotion.Add(roi)
            End If
        Next
        dst2 = src
        For Each roi In roiMotion
            dst2.Rectangle(roi, white, task.lineWidth)
        Next
        labels(2) = "Motion added to dst3 for " + CStr(roiMotion.Count) + " segments out of " + CStr(task.gridRects.Count)
        labels(3) = CStr(task.gridRects.Count - roiMotion.Count) + " segments out of " + CStr(task.gridRects.Count) + " had > " +
                         Format(correlationSlider.Value / 1000, "0.0%") + " correlation. "
    End Sub
End Class






'Public Class Motion_Intersect : Inherits TaskParent
'    Dim bgSub As New BGSubtract_Basics
'    Dim minCount = 4
'    Dim reconstructedRGB As Integer
'    Public Sub New()
'        If standalone Then task.gOptions.displayDst1.Checked = True
'        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
'        If dst2.Width = 1280 Or dst2.Width = 640 Then minCount = 16
'        desc = "Track the max rectangle that covers all the motion until there is no motion in it."
'    End Sub
'    Private Function buildEnclosingRect(tmp As cv.Mat)
'        Dim rectList As New List(Of cv.Rect)
'        Dim dots(tmp.Total * 2 - 1) As Integer
'        Marshal.Copy(tmp.Data, dots, 0, dots.Length)
'        Dim pointList As New List(Of cv.Point)
'        For i = 0 To dots.Length - 1 Step 2
'            If dots(i) >= 1 And dots(i) < dst2.Width - 2 And dots(i + 1) >= 1 And dots(i + 1) < dst2.Height - 2 Then
'                pointList.Add(New cv.Point(dots(i), dots(i + 1)))
'            End If
'        Next

'        Dim flags = 4 Or cv.FloodFillFlags.MaskOnly Or cv.FloodFillFlags.FixedRange
'        Dim rect As cv.Rect
'        Dim motionMat = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
'        Dim matPoints = dst1(New cv.Rect(1, 1, motionMat.Width - 2, motionMat.Height - 2))
'        For Each pt In pointList
'            If motionMat.Get(Of Byte)(pt.Y, pt.X) = 0 And matPoints.Get(Of Byte)(pt.Y, pt.X) <> 0 Then
'                Dim count = matPoints.FloodFill(motionMat, pt, 255, rect, 0, 0, flags Or (255 << 8))
'                If count <= minCount Then Continue For
'                rectList.Add(New cv.Rect(rect.X, rect.Y, rect.Width + 1, rect.Height + 1))
'            End If
'        Next

'        labels(3) = "There were " + CStr(CInt(dots.Length / 2)) + " points collected"

'        If rectList.Count = 0 Then Return New cv.Rect
'        Dim motionRect As cv.Rect = rectList(0)
'        For Each r In rectList
'            motionRect = motionRect.Union(r)
'        Next
'        Return motionRect
'    End Function
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        Static color = src.Clone
'        Static lastMotionRect As cv.Rect = task.motionRect
'        Dim motionTest = False
'        If task.heartBeat Or task.motionRect.Width * task.motionRect.Height > src.Total / 2 Or task.optionsChanged Then
'            motionTest = True
'        Else
'            bgSub.Run(src)
'            dst1 = bgSub.dst2
'            Dim tmp As New cv.Mat
'            cv.Cv2.FindNonZero(dst1, tmp)

'            If tmp.Total > src.Total / 2 Then
'                motionTest = True
'            ElseIf tmp.Total > 0 Then
'                reconstructedRGB += 1
'                task.motionRect = buildEnclosingRect(tmp)
'                If task.motionRect.IntersectsWith(lastMotionRect) Then
'                    task.motionRect = task.motionRect.Union(lastMotionRect)
'                End If
'                If task.motionRect.Width * task.motionRect.Height > src.Total / 2 Then
'                    motionTest = True
'                End If
'            End If
'        End If

'        dst3.SetTo(0)
'        If motionTest Then
'            labels(2) = CStr(reconstructedRGB) + " frames since last full image"
'            reconstructedRGB = 0
'            task.motionRect = New cv.Rect
'            dst2 = src.Clone
'        End If

'        If standaloneTest() Then
'            dst2 = dst1
'            If task.motionRect.Width > 0 And task.motionRect.Height > 0 Then
'                dst3(task.motionRect).SetTo(255)
'                src(task.motionRect).CopyTo(dst2(task.motionRect))
'            End If
'        End If

'        If standaloneTest() Then
'            If task.motionRect.Width > 0 And task.motionRect.Height > 0 Then
'                src(task.motionRect).CopyTo(dst0(task.motionRect))
'                color.Rectangle(task.motionRect, white, task.lineWidth, task.lineType)
'            End If
'        End If
'        lastMotionRect = task.motionRect
'    End Sub
'End Class







Public Class Motion_RectTest : Inherits TaskParent
    Dim motion As New Motion_Enclosing
    Dim diff As New Diff_Basics
    Dim lastRects As New List(Of cv.Rect)
    Public Sub New()
        labels(3) = "The white spots show the difference of the constructed image from the current image."
        desc = "Track the RGB image using Motion_Enclosing to isolate the motion"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        motion.Run(src)
        Dim r = motion.motionRect
        If task.heartBeat Or r.Width * r.Height > src.Total / 2 Or task.frameCount < 50 Then
            dst2 = src.Clone
            lastRects.Clear()
        Else
            If r.Width > 0 And r.Height > 0 Then
                For Each rect In lastRects
                    r = r.Union(rect)
                Next
                src(r).CopyTo(dst2(r))
                lastRects.Add(r)
                If lastRects.Count > task.frameHistoryCount Then lastRects.RemoveAt(0)
            Else
                lastRects.Clear()
            End If
        End If

        If standaloneTest() Then
            diff.lastFrame = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            diff.Run(dst2)
            dst3 = diff.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            DrawRect(dst3, r)
        End If
    End Sub
End Class









Public Class Motion_HistoryTest : Inherits TaskParent
    Dim diff As New Diff_Basics
    Dim frames As New History_Basics
    Public Sub New()
        task.gOptions.pixelDiffThreshold = 10
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Detect motion using the last X images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        diff.Run(src)
        dst1 = diff.dst2.Threshold(0, 1, cv.ThresholdTypes.Binary)
        frames.Run(dst1)

        dst2 = frames.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels(2) = "Cumulative diff for the last " + CStr(task.frameHistoryCount) + " frames"
    End Sub
End Class






Public Class Motion_Enclosing : Inherits TaskParent
    Dim learnRate As Double
    Public motionRect As New cv.Rect
    Public Sub New()
        If dst2.Width >= 1280 Then learnRate = 0.5 Else learnRate = 0.1 ' learn faster with large images (slower frame rate)
        cPtr = BGSubtract_BGFG_Open(4)
        labels(2) = "MOG2 is the best option.  See BGSubtract_Basics to see more options."
        desc = "Build an enclosing rectangle for the motion"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels, learnRate)
        handleSrc.Free()

        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr).Threshold(0, 255, cv.ThresholdTypes.Binary)

        dst3 = runRedC(dst2, labels(2), Not dst2)

        motionRect = New cv.Rect
        If task.redC.rcList.Count < 2 Then Exit Sub
        motionRect = task.redC.rcList.ElementAt(1).rect
        For i = 2 To task.redC.rcList.Count - 1
            Dim rc = task.redC.rcList.ElementAt(i)
            motionRect = motionRect.Union(rc.rect)
        Next

        If motionRect.Width > dst2.Width / 2 And motionRect.Height > dst2.Height / 2 Then
            motionRect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
        End If
        DrawRect(dst2, motionRect, 255)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = BGSubtract_BGFG_Close(cPtr)
    End Sub
End Class








'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Public Class Motion_Diff : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "Unstable mask", "Pixel difference"}
        desc = "Capture an image and use absDiff/threshold to compare it to the last snapshot"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Or dst1.Channels <> 1 Then
            dst1 = src.Clone
            dst2.SetTo(0)
        End If

        cv.Cv2.Absdiff(src, dst1, dst3)
        dst2 = dst3.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
        dst1 = src.Clone
    End Sub
End Class






Public Class Motion_PointCloud : Inherits TaskParent
    Dim diff As New Diff_Depth32f
    Public Sub New()
        labels = {"", "", "Pointcloud updated only with motion mask", "Diff of dst2 and latest pointcloud"}
        desc = "Display the pointcloud after updating only with the motion mask.  Resync every heartbeat."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then dst2 = task.pointCloud
        task.pointCloud.CopyTo(dst2, task.motionMask)

        Dim split = dst2.Split()
        diff.lastDepth32f = split(2)
        diff.Run(task.pcSplit(2))
        dst3 = diff.dst2
    End Sub
End Class





Public Class Motion_FromEdge : Inherits TaskParent
    Dim cAccum As New Edge_CannyAccum
    Public Sub New()
        desc = "Detect motion from pixels less that max value in an accumulation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        cAccum.Run(src)

        Dim mm = GetMinMax(cAccum.dst2)
        labels(3) = "Max value = " + CStr(mm.maxVal)

        dst2 = cAccum.dst2.Threshold(mm.maxVal, 255, cv.ThresholdTypes.TozeroInv)
        dst3 = cAccum.dst2.InRange(0, mm.maxVal - 10)
    End Sub
End Class






Public Class Motion_FromEdgeColorize : Inherits TaskParent
    Dim cAccum As New Edge_CannyAccum
    Public Sub New()
        labels = {"", "", "Canny edges accumulated", "Colorized version of dst2 - blue indicates motion."}
        desc = "Colorize the output of Edge_CannyAccum to show values off the peak value which indicate motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        cAccum.Run(src)
        dst2 = cAccum.dst2
        dst3 = ShowPalette(dst2)
    End Sub
End Class






Public Class Motion_EdgeStability : Inherits TaskParent
    Dim gEdges As New Brick_Edges
    Public Sub New()
        labels(3) = "High population cells"
        desc = "Measure the stability of edges in each grid Rect"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        gEdges.Run(src)
        dst2 = gEdges.edges.dst2

        Dim popSorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        Dim pops As New List(Of Integer)
        For i = 0 To gEdges.featureRects.Count - 1
            Dim roi = gEdges.featureRects(i)
            Dim pop = dst2(roi).CountNonZero
            pops.Add(pop)
            popSorted.Add(pop, i)
            dst2.Rectangle(roi, 255, task.lineWidth)
        Next

        Dim popAverage = If(pops.Count > 0, pops.Average, 0)
        Dim popMin = If(pops.Count > 0, pops.Min, 0)
        Dim popMax = If(pops.Count > 0, pops.Min, 0)
        labels(2) = CStr(gEdges.featureRects.Count) + " feature rects with an average population of " +
                         Format(popAverage, fmt1) + " and with min = " + CStr(popMin) +
                         " and max = " + CStr(popMax) + ".  Circled cell has max features."

        Dim index = pops.IndexOf(pops.Max)
        Dim gSize = task.gOptions.GridSlider.Value
        Dim pt = New cv.Point(gEdges.featureRects(index).X + gSize / 2, gEdges.featureRects(index).Y + gSize / 2)
        dst2.Circle(pt, gSize * 1.5, 255, task.lineWidth * 2)

        dst3.SetTo(0)
        dst3.Circle(pt, gSize * 1.5, 255, task.lineWidth * 2)
        Dim count As Integer
        For Each index In popSorted.Values
            dst3.Rectangle(gEdges.featureRects(index), white, task.lineWidth)
            count += 1
            If count >= 20 Then Exit For
        Next
    End Sub
End Class







Public Class Motion_CenterRect : Inherits TaskParent
    Dim gravitySnap As New lpData
    Public template As cv.Mat
    Dim options As New Options_Features
    Dim correlation As Single
    Dim matchRect As cv.Rect
    Public inputRect As cv.Rect
    Public matchCenter As cv.Point
    Public translation As cv.Point2f
    Public angle As Single ' in degrees.
    Public rotatedRect As cv.RotatedRect
    Dim drawRotate As New Draw_RotatedRect
    Public Sub New()
        labels(3) = "MatchTemplate output for centerRect - center is black"
        desc = "Build a center rectangle and track it with MatchTemplate."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        ' set a low threshold to make the results more visible.
        Dim correlationThreshold = 0.95 ' If(task.gOptions.debugChecked, 0.5, 0.9)
        If task.heartBeatLT Or gravitySnap.p1.X = 0 Or correlation < correlationThreshold Then
            If inputRect.Width <> 0 Then task.centerRect = inputRect
            template = src(task.centerRect).Clone
            gravitySnap = task.lineGravity
        End If

        cv.Cv2.MatchTemplate(template, src, dst3, options.matchOption)

        Dim mm = GetMinMax(dst3)

        correlation = mm.maxVal
        Dim w = template.Width, h = template.Height
        matchCenter = New cv.Point(mm.maxLoc.X + task.centerRect.X, mm.maxLoc.Y + task.centerRect.Y)
        matchRect = New cv.Rect(mm.maxLoc.X, mm.maxLoc.Y, w, h)

        dst2 = src.Clone
        dst2.Rectangle(matchRect, task.highlight, task.lineWidth)

        dst3 = dst3.Normalize(0, 255, cv.NormTypes.MinMax).Resize(dst2.Size)
        DrawCircle(dst3, matchCenter, task.DotSize, cv.Scalar.Black)

        Dim smp = New lpData(gravitySnap.p1, gravitySnap.p2)
        dst2.Line(smp.p1, smp.p2, task.highlight, task.lineWidth + 2, task.lineType)

        Dim xDisp = matchCenter.X - dst2.Width / 2
        Dim yDisp = matchCenter.Y - dst2.Height / 2
        translation = New cv.Point2f(xDisp, yDisp)

        Dim mp = task.lineGravity
        dst2.Line(mp.p1, mp.p2, black, task.lineWidth, task.lineType)

        Dim sideAdjacent = dst2.Height / 2
        Dim sideOpposite = Math.Abs(smp.p1.X - dst2.Width / 2)
        Dim rotationSnap = Math.Atan(sideOpposite / sideAdjacent) * 180 / cv.Cv2.PI

        sideOpposite = Math.Abs(mp.p1.X - dst2.Width / 2)
        Dim rotationGravity = Math.Atan(sideOpposite / sideAdjacent) * 180 / cv.Cv2.PI

        angle = rotationSnap - rotationGravity
        rotatedRect = New cv.RotatedRect(matchCenter, matchRect.Size, angle)

        drawRotate.rr = rotatedRect
        drawRotate.Run(dst2)
        dst2 = drawRotate.dst2

        labels(2) = "Correlation = " + Format(correlation, fmt3) + ", Translation = (" +
                    Format(xDisp, fmt1) + "," + Format(yDisp, fmt1) + ") " +
                    "Rotation = " + Format(angle, fmt1) + " degrees"
    End Sub
End Class







Public Class Motion_CenterKalman : Inherits TaskParent
    Dim motion As New Motion_CenterRect
    Dim kalmanRR As New Kalman_Basics
    Dim centerRect As cv.Rect
    Dim drawRotate As New Draw_RotatedRect
    Public Sub New()
        task.kalman = New Kalman_Basics
        ReDim task.kalman.kInput(2 - 1)
        labels(3) = "Template for motion matchTemplate.  Shake the camera to see Kalman impact."
        desc = "Kalmanize the output of center rotation"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.firstPass Then centerRect = task.centerRect
        dst2 = src.Clone
        motion.Run(src)

        Dim newRect As cv.Rect
        If motion.translation.X = 0 And motion.translation.Y = 0 And motion.angle = 0 Then
            newRect = centerRect
            drawRotate.rr = New cv.RotatedRect(motion.matchCenter, task.centerRect.Size, 0)
        Else
            task.kalman.kInput = {motion.translation.X, motion.translation.Y}
            task.kalman.Run(emptyMat)

            newRect = New cv.Rect(centerRect.X + task.kalman.kOutput(0), centerRect.Y + task.kalman.kOutput(1),
                                  centerRect.Width, centerRect.Height)

            kalmanRR.kInput = New Single() {motion.matchCenter.X, motion.matchCenter.Y, motion.angle}
            kalmanRR.Run(src)

            Dim pt = New cv.Point2f(kalmanRR.kOutput(0), kalmanRR.kOutput(1))
            drawRotate.rr = New cv.RotatedRect(pt, task.centerRect.Size, kalmanRR.kOutput(2))
        End If

        drawRotate.Run(dst2)
        dst2 = drawRotate.dst2
        dst2.Rectangle(newRect, task.highlight, task.lineWidth)

        dst3(centerRect) = motion.template
        labels(2) = motion.labels(2)
    End Sub
End Class






Public Class Motion_CenterLeftRight : Inherits TaskParent
    Dim CenterC As New Motion_CenterRect
    Dim leftC As New Motion_CenterRect
    Dim rightC As New Motion_CenterRect
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Calculate translation and rotation for both left and right images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        CenterC.Run(src)
        dst1 = CenterC.dst2
        labels(1) = CenterC.labels(2)

        If task.leftView.Channels = 1 Then
            leftC.Run(task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
        Else
            leftC.Run(task.leftView)
        End If

        dst2 = leftC.dst2
        labels(2) = leftC.labels(2)

        If task.rightView.Channels = 1 Then
            rightC.Run(task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
        Else
            rightC.Run(task.rightView)
        End If

        dst3 = rightC.dst2
        labels(3) = rightC.labels(2)

        Debug.WriteLine("translation X,Y (C/L/R): " + Format(CenterC.translation.X, fmt0) + "/" +
                         Format(leftC.translation.X, fmt0) + "/" + Format(rightC.translation.X, fmt0) +
                         ", " + Format(CenterC.translation.Y, fmt0) + "/" + Format(leftC.translation.Y, fmt0) +
                         "/" + Format(rightC.translation.Y, fmt0) + " rotation angle = " + Format(CenterC.angle, fmt1) +
                         "/" + Format(leftC.angle, fmt1) + "/" + Format(rightC.angle, fmt1))
    End Sub
End Class









Public Class Motion_CenterRotation : Inherits TaskParent
    Dim motion As New Motion_CenterRect
    Dim vertRect As cv.Rect
    Dim options As New Options_Threshold
    Public mp As lpData
    Public angle As Single
    Public rotatedRect As cv.RotatedRect
    Dim drawRotate As New Draw_RotatedRect
    Public Sub New()
        Dim w = dst2.Width
        vertRect = New cv.Rect(w / 2 - w / 4, 0, w / 2, dst2.Height)
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        OptionParent.FindSlider("Threshold value").Value = 200
        desc = "Find the approximate rotation angle using the diamond shape " +
               "from the thresholded MatchTemplate output."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim dCount = dst2.CountNonZero
        Static tSlider = OptionParent.FindSlider("Threshold value")
        If dCount > dst2.Total / 100 Then
            Dim nextval = tSlider.value + 1
            If nextval < tSlider.maximum Then tSlider.value = nextval
        ElseIf dCount < dst2.Total / 200 Then
            Dim nextval = tSlider.value - 1
            If nextval >= 0 Then tSlider.value = nextval
        End If

        motion.Run(src)
        dst1 = motion.dst3

        dst1(vertRect).ConvertTo(dst0(vertRect), cv.MatType.CV_8U)

        Dim mm = GetMinMax(dst1)
        dst2 = dst0.Threshold(options.threshold, 255, cv.ThresholdTypes.Binary)

        dst3 = src.Clone

        Dim tmp As New cv.Mat
        cv.Cv2.FindNonZero(dst2, tmp)

        If tmp.Rows > 2 Then
            Dim topPoint = tmp.Get(Of cv.Point)(0, 0)
            Dim botPoint = tmp.Get(Of cv.Point)(tmp.Rows - 1, 0)

            Dim pair = New lpData(topPoint, botPoint)
            mp = findEdgePoints(pair)
            dst3.Line(mp.p1, mp.p2, task.highlight, task.lineWidth + 1, task.lineType)

            Dim sideAdjacent = dst2.Height
            Dim sideOpposite = mp.p1.X - mp.p2.X
            angle = Math.Atan(sideOpposite / sideAdjacent) * 180 / cv.Cv2.PI
            If mp.p1.Y = dst2.Height Then angle = -angle
            rotatedRect = New cv.RotatedRect(mm.maxLoc, task.centerRect.Size, angle)
            labels(3) = "angle = " + Format(angle, fmt1) + " degrees"
            drawRotate.rr = rotatedRect
            drawRotate.Run(dst3)
            dst3 = drawRotate.dst2
        End If
        labels(3) = motion.labels(2)
    End Sub
End Class






Public Class Motion_Blob : Inherits TaskParent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Threshold for punch", 0, 255, 250)
        desc = "Identify the difference in pixels from one image to the next"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static thresholdSlider = OptionParent.FindSlider("Threshold for punch")
        Dim threshold = thresholdSlider.value

        Static lastColor As cv.Mat = src.Clone

        dst2 = src.Clone
        dst2 -= lastColor
        dst3 = dst2.Threshold(0, New cv.Scalar(threshold, threshold, threshold), cv.ThresholdTypes.Binary).ConvertScaleAbs

        dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        lastColor = src.Clone
    End Sub
End Class






Public Class Motion_BlobGray : Inherits TaskParent
    Public Sub New()
        desc = "Identify the difference in pixels from one image to the next"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lastGray As cv.Mat = task.gray.Clone

        dst2 = task.gray.Clone
        dst2 -= lastGray
        dst3 = dst2.Threshold(task.featureOptions.ColorDiffSlider.Value, 255, cv.ThresholdTypes.Binary)

        lastGray = task.gray.Clone
    End Sub
End Class





Public Class Motion_BestBricks : Inherits TaskParent
    Dim match As New Match_Basics
    Dim brickLine As New BrickLine_LeftRight
    Public Sub New()
        labels(2) = "Best bricks are shown below and their match offsets show in dst3 - X/Y"
        desc = "Identify the motion for each of the 'Best' bricks"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        brickLine.Run(src)
        dst2 = brickLine.dst2
        Static lastGray = task.gray.Clone

        Dim offsetX As New List(Of Single)
        Dim offsetY As New List(Of Single)
        dst3 = lastGray.clone
        For Each index In brickLine.bestBricks
            Dim nabeRect = task.gridNabeRects(index)
            match.template = lastGray(task.gridRects(index))
            match.Run(task.gray(nabeRect))

            Dim x = match.newCenter.X - nabeRect.Width / 2
            Dim y = match.newCenter.Y - nabeRect.Height / 2
            offsetX.Add(x)
            offsetY.Add(y)

            Dim rect = match.newRect
            rect.X += nabeRect.X
            rect.Y += nabeRect.Y
            SetTrueText(Format(x, fmt0) + "/" + Format(y, fmt0), rect.TopLeft, 3)
        Next

        lastGray = task.gray.Clone
        If offsetX.Count > 0 Then
            labels(3) = "Average offset X/Y = " + Format(offsetX.Average(), fmt3) + "/" + Format(offsetY.Average(), fmt3)
        End If
    End Sub
End Class





Public Class Motion_Longest : Inherits TaskParent
    Public Sub New()
        desc = "Determine the motion of the end points of the longest line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.gray

        Dim lp = New lpData(task.lineLongest.ep1, task.lineLongest.ep2)
        DrawLine(dst2, lp, white)

    End Sub
End Class





Public Class Motion_Line : Inherits TaskParent
    Dim diff As New Diff_RGBAccum
    Dim lineHistory As New List(Of List(Of lpData))
    Public Sub New()
        labels(3) = "Wave at the camera to see results - "
        desc = "Track lines that are the result of motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        diff.Run(src)
        dst2 = diff.dst2

        If task.heartBeat Then dst3 = src
        lineHistory.Add(task.lines.lpList)
        For Each lplist In lineHistory
            For Each lp In lplist
                DrawLine(dst3, lp.p1, lp.p2)
            Next
        Next
        If lineHistory.Count > task.gOptions.FrameHistory.Value Then lineHistory.RemoveAt(0)

        labels(2) = CStr(task.lines.lpList.Count) + " lines were found in the diff output"
    End Sub
End Class