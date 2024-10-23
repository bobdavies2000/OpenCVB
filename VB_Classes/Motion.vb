Imports System.Runtime.InteropServices
Imports System.Threading
Imports cvb = OpenCvSharp
Public Class Motion_Basics : Inherits TaskParent
    Public measure As New LowRes_MeasureMotion
    Dim diff As New Diff_Basics
    Dim depthRGB As New cvb.Mat
    Public pointcloud As cvb.Mat
    Public color As cvb.Mat
    Public motionMask As cvb.Mat
    Public Sub New()
        motionMask = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U)
        labels(3) = "The difference between the motion-constructed image and the current image.  " +
                    "Highlighted pixels may often be nearly identical."
        desc = "Isolate all motion in the scene"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        measure.Run(src)
        color = measure.dst3.Clone
        dst2 = color
        labels(2) = measure.labels(2)

        motionMask.SetTo(0)
        If measure.motionDetected Then
            For Each roi In measure.motionRects
                motionMask(roi).SetTo(255)
            Next
        End If

        If standaloneTest() Then ' show any differences
            diff.lastFrame = dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
            diff.Run(src)
            dst3 = diff.dst2
        End If

        Dim percentChanged = measure.motionRects.Count / task.gridRects.Count

        If task.heartBeatLT And task.gOptions.UpdateOnHeartbeat.Checked Or depthRGB.Rows = 0 Then
            depthRGB = task.depthRGB.Clone
            pointcloud = task.pointCloud.Clone
        Else
            task.depthRGB.CopyTo(depthRGB, motionMask)
            task.pointCloud.CopyTo(pointcloud, motionMask)
        End If
    End Sub
End Class






Public Class Motion_BasicsTest : Inherits TaskParent
    Dim diff As New Diff_Basics
    Dim measure As New LowRes_MeasureMotion
    Public Sub New()
        task.gOptions.UseMotionConstructed.Checked = False
        task.gOptions.ShowMotionRectangle.Checked = True
        desc = "Display the difference between task.color and src to verify Motion_Basics is working"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.gOptions.UseMotionConstructed.Checked Then
            SetTrueText("Uncheck 'Use Motion-Constructed images' to validate Motion_Basics", 3)
            Exit Sub
        End If

        measure.Run(src)

        diff.lastFrame = measure.dst3.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        diff.Run(src)
        dst2 = diff.dst2

        labels(2) = "Pixels that were different: " + CStr(dst2.CountNonZero)
    End Sub
End Class






Public Class Motion_BGSub : Inherits TaskParent
    Public bgSub As New BGSubtract_MOG2
    Dim motion As New Motion_BGSub_QT
    Public Sub New()
        UpdateAdvice(traceName + ": redOptions are used as well as BGSubtract options.")
        desc = "Use floodfill to find all the real motion in an image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        bgSub.Run(src)
        motion.Run(bgSub.dst2)
        dst2 = motion.dst2
        labels(2) = motion.labels(2)
    End Sub
End Class






Public Class Motion_BGSub_QT : Inherits TaskParent
    Dim redMasks As New RedCloud_Basics
    Public bgSub As New BGSubtract_MOG2
    Dim rectList As New List(Of cvb.Rect)
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
        task.redOptions.setIdentifyCells(False)
        desc = "The option-free version of Motion_BGSub"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.motionDetected = True

        If src.Channels() <> 1 Then
            bgSub.Run(src)
            src = bgSub.dst2
        End If

        dst2 = src

        redMasks.Run(src.Threshold(0, 255, cvb.ThresholdTypes.Binary))
        If task.redCells.Count < 2 Then
            task.motionDetected = False
            rectList.Clear()
        Else
            Dim nextRect = task.redCells.ElementAt(1).rect
            For i = 2 To task.redCells.Count - 1
                Dim rc = task.redCells.ElementAt(i)
                nextRect = nextRect.Union(rc.rect)
            Next
        End If

        If standaloneTest() Then
            If task.redCells.Count > 1 Then
                labels(2) = CStr(task.redCells.Count) + " RedMask cells had motion"
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

        dst3 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Detect motion through the correlation coefficient"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static ccSlider = FindSlider("Correlation threshold X1000")
        Static padSlider = FindSlider("Pad size in pixels for the search area")
        Static stdevSlider = FindSlider("Stdev threshold for using correlation")
        Dim pad = padSlider.Value
        Dim ccThreshold = ccSlider.Value
        Dim stdevThreshold = stdevSlider.Value

        Dim input = src.Clone
        If input.Channels() <> 1 Then input = input.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cvb.Mat = input.Clone
        dst3.SetTo(0)
        Parallel.For(0, task.gridRects.Count,
        Sub(i)
            Dim roi = task.gridRects(i)
            Dim correlation As New cvb.Mat
            Dim mean As Single, stdev As Single
            cvb.Cv2.MeanStdDev(input(roi), mean, stdev)
            If stdev > stdevThreshold Then
                cvb.Cv2.MatchTemplate(lastFrame(roi), input(roi), correlation, cvb.TemplateMatchModes.CCoeffNormed)
                Dim mm As mmData = GetMinMax(correlation)
                If mm.maxVal < ccThreshold / 1000 Then
                    If (i Mod task.gridRows) <> 0 Then dst3(task.gridRects(i - 1)).SetTo(255)
                    If (i Mod task.gridRows) < task.gridRows And i < task.gridRects.Count - 1 Then dst3(task.gridRects(i + 1)).SetTo(255)
                    If i > task.gridRows Then
                        dst3(task.gridRects(i - task.gridRows)).SetTo(255)
                        dst3(task.gridRects(i - task.gridRows + 1)).SetTo(255)
                    End If
                    If i < (task.gridRects.Count - task.gridRows - 1) Then
                        dst3(task.gridRects(i + task.gridRows)).SetTo(255)
                        dst3(task.gridRects(i + task.gridRows + 1)).SetTo(255)
                    End If
                    dst3(roi).SetTo(255)
                End If
            End If
        End Sub)

        lastFrame = input.Clone

        If task.heartBeat Then dst2 = src.Clone Else src.CopyTo(dst2, dst3)
    End Sub
End Class







Public Class Motion_CCmerge : Inherits TaskParent
    Dim motionCC As New Motion_ThruCorrelation
    Dim lastFrame As cvb.Mat
    Public Sub New()
        desc = "Use the correlation coefficient to maintain an up-to-date image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then
            dst2 = src.Clone
            lastFrame = src.Clone
        End If

        motionCC.Run(src)

        If motionCC.dst3.CountNonZero > src.Total / 2 Then
            dst2 = src.Clone
            lastFrame = src.Clone
        End If

        src.CopyTo(dst2, motionCC.dst3)
        dst3 = motionCC.dst3
    End Sub
End Class









Public Class Motion_PixelDiff : Inherits TaskParent
    Public changedPixels As Integer
    Dim changeCount As Integer, frames As Integer
    Public Sub New()
        desc = "Count the number of changed pixels in the current frame and accumulate them.  If either exceeds thresholds, then set flag = true.  " +
                    "To get the Options Slider, use " + traceName + "QT"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cvb.Mat = src
        cvb.Cv2.Absdiff(src, lastFrame, dst2)
        dst2 = dst2.Threshold(task.gOptions.pixelDiffThreshold, 255, cvb.ThresholdTypes.Binary)
        changedPixels = dst2.CountNonZero
        task.motionFlag = changedPixels > 0

        If task.motionFlag Then changeCount += 1
        frames += 1
        If task.heartBeat Then
            strOut = "Pixels changed = " + CStr(changedPixels) + " at last heartbeat.  Since last heartbeat: " +
                     Format(changeCount / frames, "0%") + " of frames were different"
            changeCount = 0
            frames = 0
        End If
        SetTrueText(strOut, 3)
        If task.motionFlag Then lastFrame = src
    End Sub
End Class








Public Class Motion_Grid_MP : Inherits TaskParent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Correlation Threshold", 800, 1000, 990)
        UpdateAdvice(traceName + ": local options 'Correlation Threshold' controls how well the image matches.")
        desc = "Detect Motion in the color image using multi-threading."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static correlationSlider = FindSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Then dst3 = src.Clone

        dst2 = src

        Dim updateCount As Integer
        Parallel.ForEach(Of cvb.Rect)(task.gridRects,
            Sub(roi)
                Dim correlation As New cvb.Mat
                cvb.Cv2.MatchTemplate(src(roi), dst3(roi), correlation, cvb.TemplateMatchModes.CCoeffNormed)
                If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                    Interlocked.Increment(updateCount)
                    src(roi).CopyTo(dst3(roi))
                    dst2.Rectangle(roi, cvb.Scalar.White, task.lineWidth)
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
        UpdateAdvice(traceName + ": local options 'Correlation Threshold' controls how well the image matches.")
        desc = "Detect Motion in the color image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static correlationSlider = FindSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Then dst3 = src.Clone

        Dim roiMotion As New List(Of cvb.Rect)
        For Each roi In task.gridRects
            Dim correlation As New cvb.Mat
            cvb.Cv2.MatchTemplate(src(roi), dst3(roi), correlation, cvb.TemplateMatchModes.CCoeffNormed)
            If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                src(roi).CopyTo(dst3(roi))
                roiMotion.Add(roi)
            End If
        Next
        dst2 = src
        For Each roi In roiMotion
            dst2.Rectangle(roi, cvb.Scalar.White, task.lineWidth)
        Next
        labels(2) = "Motion added to dst3 for " + CStr(roiMotion.Count) + " segments out of " + CStr(task.gridRects.Count)
        labels(3) = CStr(task.gridRects.Count - roiMotion.Count) + " segments out of " + CStr(task.gridRects.Count) + " had > " +
                         Format(correlationSlider.Value / 1000, "0.0%") + " correlation. "
    End Sub
End Class






Public Class Motion_Intersect : Inherits TaskParent
    Dim bgSub As New BGSubtract_Basics
    Dim minCount = 4
    Dim reconstructedRGB As Integer
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        If dst2.Width = 1280 Or dst2.Width = 640 Then minCount = 16
        desc = "Track the max rectangle that covers all the motion until there is no motion in it."
    End Sub
    Private Function buildEnclosingRect(tmp As cvb.Mat)
        Dim rectList As New List(Of cvb.Rect)
        Dim dots(tmp.Total * 2 - 1) As Integer
        Marshal.Copy(tmp.Data, dots, 0, dots.Length)
        Dim pointList As New List(Of cvb.Point)
        For i = 0 To dots.Length - 1 Step 2
            If dots(i) >= 1 And dots(i) < dst2.Width - 2 And dots(i + 1) >= 1 And dots(i + 1) < dst2.Height - 2 Then
                pointList.Add(New cvb.Point(dots(i), dots(i + 1)))
            End If
        Next

        Dim flags = 4 Or cvb.FloodFillFlags.MaskOnly Or cvb.FloodFillFlags.FixedRange
        Dim rect As cvb.Rect
        Dim motionMat = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        Dim matPoints = dst1(New cvb.Rect(1, 1, motionMat.Width - 2, motionMat.Height - 2))
        For Each pt In pointList
            If motionMat.Get(Of Byte)(pt.Y, pt.X) = 0 And matPoints.Get(Of Byte)(pt.Y, pt.X) <> 0 Then
                Dim count = matPoints.FloodFill(motionMat, pt, 255, rect, 0, 0, flags Or (255 << 8))
                If count <= minCount Then Continue For
                rectList.Add(New cvb.Rect(rect.X, rect.Y, rect.Width + 1, rect.Height + 1))
            End If
        Next

        labels(3) = "There were " + CStr(CInt(dots.Length / 2)) + " points collected"

        If rectList.Count = 0 Then Return New cvb.Rect
        Dim motionRect As cvb.Rect = rectList(0)
        For Each r In rectList
            motionRect = motionRect.Union(r)
        Next
        Return motionRect
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        Static color = src.Clone
        Static lastMotionRect As cvb.Rect = task.motionRect
        task.motionFlag = False
        If task.heartBeat Or task.motionRect.Width * task.motionRect.Height > src.Total / 2 Or task.optionsChanged Then
            task.motionFlag = True
        Else
            bgSub.Run(src)
            dst1 = bgSub.dst2
            Dim tmp As New cvb.Mat
            cvb.Cv2.FindNonZero(dst1, tmp)

            If tmp.Total > src.Total / 2 Then
                task.motionFlag = True
            ElseIf tmp.Total > 0 Then
                reconstructedRGB += 1
                task.motionRect = buildEnclosingRect(tmp)
                If task.motionRect.IntersectsWith(lastMotionRect) Then task.motionRect = task.motionRect.Union(lastMotionRect)
                If task.motionRect.Width * task.motionRect.Height > src.Total / 2 Then task.motionFlag = True
            End If
        End If

        dst3.SetTo(0)
        If task.motionFlag Then
            labels(2) = CStr(reconstructedRGB) + " frames since last full image"
            reconstructedRGB = 0
            task.motionRect = New cvb.Rect
            dst2 = src.Clone
        End If

        If standaloneTest() Then
            dst2 = dst1
            If task.motionRect.Width > 0 And task.motionRect.Height > 0 Then
                dst3(task.motionRect).SetTo(255)
                src(task.motionRect).CopyTo(dst2(task.motionRect))
            End If
        End If

        If standaloneTest() Then
            If task.motionRect.Width > 0 And task.motionRect.Height > 0 Then
                src(task.motionRect).CopyTo(dst0(task.motionRect))
                color.Rectangle(task.motionRect, cvb.Scalar.White, task.lineWidth, task.lineType)
            End If
        End If
        lastMotionRect = task.motionRect
    End Sub
End Class







Public Class Motion_RectTest : Inherits TaskParent
    Dim motion As New Motion_Enclosing
    Dim diff As New Diff_Basics
    Dim lastRects As New List(Of cvb.Rect)
    Public Sub New()
        UpdateAdvice(traceName + ": gOptions frame history slider will impact results.")
        labels(3) = "The white spots show the difference of the constructed image from the current image."
        desc = "Track the RGB image using Motion_Enclosing to isolate the motion"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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
            diff.lastFrame = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
            diff.Run(dst2)
            dst3 = diff.dst2.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
            dst3.Rectangle(r, task.HighlightColor, task.lineWidth, task.lineType)
        End If
    End Sub
End Class









Public Class Motion_HistoryTest : Inherits TaskParent
    Dim diff As New Diff_Basics
    Dim frames As New History_Basics
    Public Sub New()
        task.gOptions.pixelDiffThreshold = 10
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Detect motion using the last X images"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        diff.Run(src)
        dst1 = diff.dst2.Threshold(0, 1, cvb.ThresholdTypes.Binary)
        frames.Run(dst1)

        dst2 = frames.dst2.Threshold(0, 255, cvb.ThresholdTypes.Binary)
        labels(2) = "Cumulative diff for the last " + CStr(task.frameHistoryCount) + " frames"
    End Sub
End Class






Public Class Motion_Enclosing : Inherits TaskParent
    Dim redMasks As New RedCloud_Basics
    Dim learnRate As Double
    Public motionRect As New cvb.Rect
    Public Sub New()
        If dst2.Width >= 1280 Then learnRate = 0.5 Else learnRate = 0.1 ' learn faster with large images (slower frame rate)
        cPtr = BGSubtract_BGFG_Open(4)
        labels(2) = "MOG2 is the best option.  See BGSubtract_Basics to see more options."
        desc = "Build an enclosing rectangle for the motion"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels, learnRate)
        handleSrc.Free()

        dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC1, imagePtr).Threshold(0, 255, cvb.ThresholdTypes.Binary)

        redMasks.inputMask = Not dst2
        redMasks.Run(dst2)

        motionRect = New cvb.Rect
        If task.redCells.Count < 2 Then Exit Sub
        motionRect = task.redCells.ElementAt(1).rect
        For i = 2 To task.redCells.Count - 1
            Dim cell = task.redCells.ElementAt(i)
            motionRect = motionRect.Union(cell.rect)
        Next

        If motionRect.Width > dst2.Width / 2 And motionRect.Height > dst2.Height / 2 Then
            motionRect = New cvb.Rect(0, 0, dst2.Width, dst2.Height)
        End If
        dst2.Rectangle(motionRect, 255, task.lineWidth, task.lineType)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = BGSubtract_BGFG_Close(cPtr)
    End Sub
End Class






Public Class Motion_Grayscale : Inherits TaskParent
    Dim diff As New Diff_Basics
    Public Sub New()
        labels = {"", "MotionRect_Basics output showing motion and enclosing rectangle.", "MotionRect accumulated grayscale image",
                  "Diff of input and latest accumulated grayscale image"}
        desc = "Display the grayscale image after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        If standaloneTest() Then
            diff.Run(dst2)
            dst3 = diff.dst2
            dst3.Rectangle(task.motionRect, 255, task.lineWidth)
        End If
    End Sub
End Class











Public Class Motion_RedCloud : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Motion detected in the cells below"
        desc = "Use RedCloud to define where there is motion"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        For Each rc In task.redCells
            If rc.motionPixels > 0 Then dst3(rc.rect).SetTo(rc.naturalColor, rc.mask)
        Next
    End Sub
End Class









'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Public Class Motion_Diff : Inherits TaskParent
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels = {"", "", "Unstable mask", "Pixel difference"}
        desc = "Capture an image and use absDiff/threshold to compare it to the last snapshot"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Or dst1.Channels <> 1 Then
            dst1 = src.Clone
            dst2.SetTo(0)
        End If

        cvb.Cv2.Absdiff(src, dst1, dst3)
        dst2 = dst3.Threshold(task.gOptions.pixelDiffThreshold, 255, cvb.ThresholdTypes.Binary)
        dst1 = src.Clone
    End Sub
End Class





Public Class Motion_Contours : Inherits TaskParent
    Public motion As New Motion_MinRect
    Public changedPixels As Integer
    Public Sub New()
        labels(2) = "Enclosing rectangles are yellow in dst2 and dst3"
        desc = "Detect contours in the motion data and the resulting rectangles"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src
        motion.Run(src)
        dst3 = motion.dst3
    End Sub
End Class






Public Class Motion_PointCloud : Inherits TaskParent
    Dim diff As New Diff_Depth32f
    Public Sub New()
        labels = {"", "Output of MotionRect_Basics showing motion and enclosing rectangle.", "MotionRect point cloud", "Diff of MotionRect Pointcloud and latest pointcloud"}
        desc = "Display the pointcloud after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        diff.Run(task.pcSplit(2))
        dst3 = diff.dst2
        dst3.Rectangle(task.motionRect, 255, task.lineWidth)
    End Sub
End Class






Public Class Motion_Depth : Inherits TaskParent
    Dim diff As New Diff_Depth32f
    Public Sub New()
        labels = {"", "Output of MotionRect_Basics showing motion and enclosing rectangle.", "MotionRect point cloud", "Diff of MotionRect Pointcloud and latest pointcloud"}
        desc = "Display the depth data after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Then dst2 = task.pcSplit(2).Clone

        diff.lastDepth32f = dst2
        diff.Run(task.pcSplit(2))
        diff.dst2.ConvertTo(dst3, cvb.MatType.CV_8U)
    End Sub
End Class









Public Class Motion_TestSingle : Inherits TaskParent
    Dim singles As New Denoise_SinglePixels_CPP_VB
    Dim random As New Random_Basics
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
        labels(2) = "Input to the Denoise_SinglePixels_CPP_VB code"
        desc = "Make sure Denoise_SinglePixels_CPP_VB is working properly."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        random.Run(empty)
        dst2.SetTo(0)
        For Each pt In random.PointList
            dst2.Set(Of Byte)(pt.Y, pt.X, 255)
            If task.toggleOnOff Then dst2.Set(Of Byte)(pt.Y + 1, pt.X, 255)
        Next

        labels(3) = If(task.toggleOnOff, "There should be points below that are next to each other", "There should be no points below")
        singles.Run(dst2.Clone)
        dst3 = singles.dst2
    End Sub
End Class









Public Class Motion_MinRect : Inherits TaskParent
    Dim mRect As New Area_MinRect
    Dim history As New History_Basics8U
    Dim lastFrame As cvb.Mat
    Dim options As New Options_MinArea
    Dim singles As New Denoise_SinglePixels_CPP_VB
    Public Sub New()
        task.gOptions.setPixelDifference(10)
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Find the nonzero points of motion and fit a rotated rectangle to them."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        If task.FirstPass Then lastFrame = src.Clone
        cvb.Cv2.Absdiff(src, lastFrame, dst2)
        dst2 = dst2.Threshold(task.gOptions.pixelDiffThreshold, 255, cvb.ThresholdTypes.Binary)
        singles.Run(dst2)
        dst3 = singles.dst2
        lastFrame = src.Clone

        history.Run(dst3)
        dst2 = history.dst2

        Dim nonzeros = dst2.FindNonZero()

        dst3.SetTo(0)
        If nonzeros.Rows > options.numPoints Then
            Dim minX As Integer = Integer.MaxValue, maxX As Integer = 0, minY As Integer = Integer.MaxValue, maxY As Integer = 0
            Dim p1 As cvb.Point, p2 As cvb.Point, p3 As cvb.Point, p4 As cvb.Point
            For i = 0 To nonzeros.Rows - 1
                Dim pt = nonzeros.Get(Of cvb.Point)(i, 0)
                If pt.X < minX Then
                    minX = pt.X
                    p1 = pt
                End If
                If pt.X > maxX Then
                    maxX = pt.X
                    p2 = pt
                End If
                If pt.Y < minY Then
                    minY = pt.Y
                    p3 = pt
                End If
                If pt.Y > maxY Then
                    maxY = pt.Y
                    p4 = pt
                End If
            Next

            mRect.inputPoints = New List(Of cvb.Point2f)({p1, p2, p3, p4})
            mRect.Run(empty)
            DrawRotatedRect(mRect.minRect, dst3, cvb.Scalar.White)
            If dst3.CountNonZero > dst3.Total / 2 Then dst3.SetTo(255)
        End If
    End Sub
End Class






Public Class Motion_FromEdge : Inherits TaskParent
    Dim cAccum As New Edge_CannyAccum
    Public Sub New()
        desc = "Detect motion from pixels less that max value in an accumulation."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        cAccum.Run(src)

        Dim mm = GetMinMax(cAccum.dst2)
        labels(3) = "Max value = " + CStr(mm.maxVal)

        dst2 = cAccum.dst2.Threshold(mm.maxVal, 255, cvb.ThresholdTypes.TozeroInv)
        dst3 = cAccum.dst2.InRange(0, mm.maxVal - 10)
    End Sub
End Class






Public Class Motion_FromEdgeColorize : Inherits TaskParent
    Dim cAccum As New Edge_CannyAccum
    Public Sub New()
        labels = {"", "", "Canny edges accumulated", "Colorized version of dst2 - blue indicates motion."}
        desc = "Colorize the output of Edge_CannyAccum to show values off the peak value which indicate motion."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        cAccum.Run(src)
        dst2 = cAccum.dst2
        dst3 = ShowPalette(dst2)
    End Sub
End Class






Public Class Motion_EdgeStability : Inherits TaskParent
    Dim lowRes As New LowRes_Edges
    Public Sub New()
        labels(3) = "High population cells"
        desc = "Measure the stability of edges in each grid Rect"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lowRes.Run(src)
        dst2 = lowRes.edges.dst2

        Dim popSorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        Dim pops As New List(Of Integer)
        For i = 0 To task.featureRects.Count - 1
            Dim roi = task.featureRects(i)
            Dim pop = dst2(roi).CountNonZero
            pops.Add(pop)
            popSorted.Add(pop, i)
            dst2.Rectangle(roi, 255, task.lineWidth)
        Next

        labels(2) = CStr(task.featureRects.Count) + " feature rects with an average population of " +
                         Format(pops.Average, fmt1) + " and with min = " + CStr(pops.Min) +
                         " and max = " + CStr(pops.Max) + ".  Circled cell has max features."

        Dim index = pops.IndexOf(pops.Max)
        Dim gSize = task.gOptions.getGridSize()
        Dim pt = New cvb.Point(task.featureRects(index).X + gSize / 2, task.featureRects(index).Y + gSize / 2)
        dst2.Circle(pt, gSize * 1.5, 255, task.lineWidth * 2)

        dst3.SetTo(0)
        dst3.Circle(pt, gSize * 1.5, 255, task.lineWidth * 2)
        Dim count As Integer
        For Each index In popSorted.Values
            dst3.Rectangle(task.featureRects(index), cvb.Scalar.White, task.lineWidth)
            count += 1
            If count >= 20 Then Exit For
        Next
    End Sub
End Class






Public Class Motion_TopFeatureFail : Inherits TaskParent
    Dim fPoly As New FPoly_TopFeatures
    Public featureRects As New List(Of cvb.Rect)
    Public searchRects As New List(Of cvb.Rect)
    Dim match As New Match_Basics
    Dim saveMat As New cvb.Mat
    Public Sub New()
        labels(2) = "Track the feature rect (small one) in each larger rectangle"
        desc = "Find the top feature cells and track them in the next frame."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim half As Integer = CInt(task.gridSize / 2)

        If task.heartBeatLT Then
            fPoly.Run(src)
            searchRects.Clear()
            featureRects.Clear()
            saveMat = src.Clone
            For Each pt In task.topFeatures
                Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
                Dim roi = New cvb.Rect(pt.X - half, pt.Y - half, task.gridSize, task.gridSize)
                roi = ValidateRect(roi)
                featureRects.Add(roi)
                searchRects.Add(task.gridNabeRects(index))
            Next

            dst2 = saveMat.Clone
            For Each pt In task.topFeatures
                Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
                Dim roi = New cvb.Rect(pt.X - half, pt.Y - half, task.gridSize, task.gridSize)
                roi = ValidateRect(roi)
                dst2.Rectangle(roi, task.HighlightColor, task.lineWidth)
                dst2.Rectangle(task.gridNabeRects(index), task.HighlightColor, task.lineWidth)
            Next
        End If

        dst3 = src.Clone
        Dim matchRects As New List(Of cvb.Rect)
        For i = 0 To featureRects.Count - 1
            Dim roi = featureRects(i)
            match.template = saveMat(roi)
            match.searchRect = searchRects(i)
            match.Run(src)
            dst3.Rectangle(match.matchRect, task.HighlightColor, task.lineWidth)
            matchRects.Add(match.matchRect)
        Next

        saveMat = src.Clone
        searchRects.Clear()
        featureRects.Clear()
        For Each roi In matchRects
            Dim pt = New cvb.Point(roi.X + half, roi.Y + half)
            Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            featureRects.Add(roi)
            searchRects.Add(task.gridNabeRects(index))
        Next
    End Sub
End Class





Public Class Motion_TopFeatures : Inherits TaskParent
    Dim fPoly As New FPoly_TopFeatures
    Public featureRects As New List(Of cvb.Rect)
    Public searchRects As New List(Of cvb.Rect)
    Dim match As New Match_Basics
    Dim half As Integer
    Public Sub New()
        labels(2) = "Track the feature rect (small one) in each larger rectangle"
        desc = "Find the top feature cells and track them in the next frame."
    End Sub
    Private Sub snapShotFeatures()
        searchRects.Clear()
        featureRects.Clear()
        For Each pt In task.topFeatures
            Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            Dim roi = New cvb.Rect(pt.X - half, pt.Y - half, task.gridSize, task.gridSize)
            roi = ValidateRect(roi)
            featureRects.Add(roi)
            searchRects.Add(task.gridNabeRects(index))
        Next

        dst2 = dst1.Clone
        For Each pt In task.topFeatures
            Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            Dim roi = New cvb.Rect(pt.X - half, pt.Y - half, task.gridSize, task.gridSize)
            roi = ValidateRect(roi)
            dst2.Rectangle(roi, task.HighlightColor, task.lineWidth)
            dst2.Rectangle(task.gridNabeRects(index), task.HighlightColor, task.lineWidth)
        Next
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        half = CInt(task.gridSize / 2)

        dst1 = src.Clone
        fPoly.Run(src)

        If task.heartBeatLT Then
            snapShotFeatures()
        End If

        dst3 = src.Clone
        Dim matchRects As New List(Of cvb.Rect)
        For i = 0 To featureRects.Count - 1
            Dim roi = featureRects(i)
            match.template = dst1(roi)
            match.searchRect = searchRects(i)
            match.Run(src)
            dst3.Rectangle(match.matchRect, task.HighlightColor, task.lineWidth)
            matchRects.Add(match.matchRect)
        Next

        searchRects.Clear()
        featureRects.Clear()
        For Each roi In matchRects
            Dim pt = New cvb.Point(roi.X + half, roi.Y + half)
            Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            featureRects.Add(roi)
            searchRects.Add(task.gridNabeRects(index))
        Next
    End Sub
End Class





Public Class Motion_FPolyRect : Inherits TaskParent
    Dim fRect As New FPoly_LineRect
    Public Sub New()
        desc = "Confirm the FPoly_LineRect matched the previous image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class
