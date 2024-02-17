Imports System.Runtime.InteropServices
Imports System.Threading
Imports cv = OpenCvSharp
Public Class Motion_Basics : Inherits VB_Algorithm
    Public bgSub As New BGSubtract_MOG2
    Dim motion As New Motion_Basics_QT
    Public Sub New()
        vbAddAdvice(traceName + ": redOptions are used as well as BGSubtract options.")
        desc = "Use floodfill to find all the real motion in an image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bgSub.Run(src)
        motion.Run(bgSub.dst2)
        dst2 = motion.dst2
        labels(2) = motion.labels(2)
    End Sub
End Class







'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Public Class Motion_Simple : Inherits VB_Algorithm
    Public diff As New Diff_Basics
    Public cumulativePixels As Integer
    Public options As New Options_Motion
    Dim saveFrameCount As Integer
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Accumulated changed pixels from the last heartbeat"
        desc = "Accumulate differences from the previous BGR image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        diff.Run(src)
        dst2 = diff.dst3
        If task.heartBeat Then cumulativePixels = 0
        If diff.changedPixels > 0 Or task.heartBeat Then
            cumulativePixels += diff.changedPixels
            If cumulativePixels / src.Total > options.cumulativePercentThreshold Or diff.changedPixels > options.motionThreshold Or
                task.optionsChanged Then
                task.motionRect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
            End If
            If task.motionRect.Width = dst2.Width Or task.heartBeat Then
                dst2.CopyTo(dst3)
                cumulativePixels = 0
                saveFrameCount = task.frameCount
            Else
                dst3.SetTo(255, dst2)
            End If
        End If

        Dim threshold = src.Total * options.cumulativePercentThreshold
        strOut = "Cumulative threshold = " + CStr(CInt(threshold / 1000)) + "k "
        labels(2) = strOut + "Current cumulative pixels changed = " + CStr(CInt(cumulativePixels / 1000)) + "k"
    End Sub
End Class








Public Class Motion_ThruCorrelation : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Correlation threshold X1000", 0, 1000, 900)
            sliders.setupTrackBar("Stdev threshold for using correlation", 0, 100, 15)
            sliders.setupTrackBar("Pad size in pixels for the search area", 0, 100, 20)
        End If

        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Detect motion through the correlation coefficient"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static ccSlider = findSlider("Correlation threshold X1000")
        Static padSlider = findSlider("Pad size in pixels for the search area")
        Static stdevSlider = findSlider("Stdev threshold for using correlation")
        Dim pad = padSlider.Value
        Dim ccThreshold = ccSlider.Value
        Dim stdevThreshold = stdevSlider.Value

        Dim input = src.Clone
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cv.Mat = input.Clone
        dst3.SetTo(0)
        Parallel.For(0, task.gridList.Count,
        Sub(i)
            Dim roi = task.gridList(i)
            Dim correlation As New cv.Mat
            Dim mean As Single, stdev As Single
            cv.Cv2.MeanStdDev(input(roi), mean, stdev)
            If stdev > stdevThreshold Then
                cv.Cv2.MatchTemplate(lastFrame(roi), input(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                Dim mm As mmData = vbMinMax(correlation)
                If mm.maxVal < ccThreshold / 1000 Then
                    If (i Mod task.gridRows) <> 0 Then dst3(task.gridList(i - 1)).SetTo(255)
                    If (i Mod task.gridRows) < task.gridRows And i < task.gridList.Count - 1 Then dst3(task.gridList(i + 1)).SetTo(255)
                    If i > task.gridRows Then
                        dst3(task.gridList(i - task.gridRows)).SetTo(255)
                        dst3(task.gridList(i - task.gridRows + 1)).SetTo(255)
                    End If
                    If i < (task.gridList.Count - task.gridRows - 1) Then
                        dst3(task.gridList(i + task.gridRows)).SetTo(255)
                        dst3(task.gridList(i + task.gridRows + 1)).SetTo(255)
                    End If
                    dst3(roi).SetTo(255)
                End If
            End If
        End Sub)

        lastFrame = input.Clone

        If task.heartBeat Then dst2 = src.Clone Else src.CopyTo(dst2, dst3)
    End Sub
End Class







Public Class Motion_CCmerge : Inherits VB_Algorithm
    Dim motionCC As New Motion_ThruCorrelation
    Public Sub New()
        desc = "Use the correlation coefficient to maintain an up-to-date image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.frameCount < 10 Then dst2 = src.Clone

        motionCC.Run(src)

        Static lastFrame = src.Clone
        If motionCC.dst3.CountNonZero > src.Total / 2 Then
            dst2 = src.Clone
            lastFrame = src.Clone
        End If

        src.CopyTo(dst2, motionCC.dst3)
        dst3 = motionCC.dst3
    End Sub
End Class









Public Class Motion_PixelDiff : Inherits VB_Algorithm
    Public changedPixels As Integer
    Public Sub New()
        desc = "Count the number of changed pixels in the current frame and accumulate them.  If either exceeds thresholds, then set flag = true.  " +
                    "To get the Options Slider, use " + traceName + "QT"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cv.Mat = src
        cv.Cv2.Absdiff(src, lastFrame, dst2)
        dst2 = dst2.Threshold(gOptions.PixelDiffThreshold.Value, 255, cv.ThresholdTypes.Binary)
        changedPixels = dst2.CountNonZero
        task.motionFlag = changedPixels > 0

        Static changeCount As Integer, frames As Integer
        If task.motionFlag Then changeCount += 1
        frames += 1
        If task.heartBeat Then
            strOut = "Pixels changed = " + CStr(changedPixels) + " at last heartbeat.  Since last heartbeat: " +
                     Format(changeCount / frames, "0%") + " of frames were different"
            changeCount = 0
            frames = 0
        End If
        setTrueText(strOut, 3)
        If task.motionFlag Then lastFrame = src
    End Sub
End Class









Public Class Motion_DepthReconstructed : Inherits VB_Algorithm
    Public motion As New Motion_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32FC3, 0)
        labels(2) = "The yellow rectangle indicates where the motion is and only that portion of the point cloud and depth mask is updated."
        desc = "Rebuild the point cloud based on the BGR motion history."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        motion.Run(src)
        dst2 = src

        If task.motionFlag Then
            dst0 = src.Clone
            dst1 = task.noDepthMask.Clone
            dst3 = task.pointCloud.Clone
            labels(3) = motion.labels(2)
        End If

        If task.motionDetected = False Then Exit Sub

        src(task.motionRect).CopyTo(dst0(task.motionRect))
        task.noDepthMask(task.motionRect).CopyTo(dst1(task.motionRect))
        task.pointCloud(task.motionRect).CopyTo(dst3(task.motionRect))
    End Sub
End Class









Public Class Motion_Contours : Inherits VB_Algorithm
    Public motion As New Motion_MinRect
    Dim contours As New Contour_Largest
    Public cumulativePixels As Integer
    Public Sub New()
        labels(2) = "Enclosing rectangles are yellow in dst2 and dst3"
        desc = "Detect contours in the motion data and the resulting rectangles"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src
        motion.Run(src)
        dst3 = motion.dst3
        Dim changedPixels = dst3.CountNonZero

        If task.heartBeat Then cumulativePixels = changedPixels Else cumulativePixels += changedPixels
        If changedPixels > 0 Then
            contours.Run(dst3)
            vbDrawContour(dst2, contours.bestContour, cv.Scalar.Yellow)
        End If
    End Sub
End Class







Public Class Motion_MinRect : Inherits VB_Algorithm
    Public motion As New Motion_BasicsTest
    Dim mRect As New Area_MinRect
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find the nonzero points of motion and fit an ellipse to them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        motion.Run(src)
        dst2 = motion.dst2

        Dim nonzeros = dst2.FindNonZero()
        If task.heartBeat Then dst3.SetTo(0)
        If nonzeros.Rows > 5 Then
            Dim ptx As New List(Of Integer)
            Dim pty As New List(Of Integer)
            Dim inputPoints As New List(Of cv.Point)
            For i = 0 To nonzeros.Rows - 1
                Dim pt = nonzeros.Get(Of cv.Point)(i, 0)
                inputPoints.Add(pt)
                ptx.Add(pt.X)
                pty.Add(pt.Y)
            Next
            Dim p1 = inputPoints(ptx.IndexOf(ptx.Max))
            Dim p2 = inputPoints(ptx.IndexOf(ptx.Min))
            Dim p3 = inputPoints(pty.IndexOf(pty.Max))
            Dim p4 = inputPoints(pty.IndexOf(pty.Min))

            mRect.inputPoints = New List(Of cv.Point2f)({p1, p2, p3, p4})
            mRect.Run(empty)
            drawRotatedRectangle(mRect.minRect, dst3, cv.Scalar.White)
        End If
    End Sub
End Class









'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Public Class Motion_BasicsTest : Inherits VB_Algorithm
    Public cumulativePixels As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "Unstable mask", "Pixel difference"}
        desc = "Capture an image and use absDiff/threshold to compare it to the last snapshot"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Then
            dst1 = src.Clone
            dst2.SetTo(0)
        End If

        cv.Cv2.Absdiff(src, dst1, dst3)
        cumulativePixels = dst3.CountNonZero
        dst2 = dst3.Threshold(gOptions.PixelDiffThreshold.Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class





Public Class Motion_Grid_MP : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Correlation Threshold", 800, 1000, 990)
        vbAddAdvice(traceName + ": local options 'Correlation Threshold' controls how well the image matches.")
        desc = "Detect Motion in the color image using multi-threading."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static correlationSlider = findSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Then dst3 = src.Clone

        dst2 = src

        Dim updateCount As Integer
        Parallel.ForEach(Of cv.Rect)(task.gridList,
            Sub(roi)
                Dim correlation As New cv.Mat
                cv.Cv2.MatchTemplate(src(roi), dst3(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                    Interlocked.Increment(updateCount)
                    src(roi).CopyTo(dst3(roi))
                    dst2.Rectangle(roi, cv.Scalar.White, task.lineWidth)
                End If
            End Sub)
        labels(2) = "Motion added to dst3 for " + CStr(updateCount) + " segments out of " + CStr(task.gridList.Count)
        labels(3) = CStr(task.gridList.Count - updateCount) + " segments out of " + CStr(task.gridList.Count) + " had > " +
                         Format(correlationSlider.Value / 1000, "0.0%") + " correlation. "
    End Sub
End Class





Public Class Motion_Grid : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Correlation Threshold", 800, 1000, 990)
        vbAddAdvice(traceName + ": local options 'Correlation Threshold' controls how well the image matches.")
        desc = "Detect Motion in the color image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static correlationSlider = findSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Then dst3 = src.Clone

        Dim roiMotion As New List(Of cv.Rect)
        For Each roi In task.gridList
            Dim correlation As New cv.Mat
            cv.Cv2.MatchTemplate(src(roi), dst3(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
            If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                src(roi).CopyTo(dst3(roi))
                roiMotion.Add(roi)
            End If
        Next
        dst2 = src
        For Each roi In roiMotion
            dst2.Rectangle(roi, cv.Scalar.White, task.lineWidth)
        Next
        labels(2) = "Motion added to dst3 for " + CStr(roiMotion.Count) + " segments out of " + CStr(task.gridList.Count)
        labels(3) = CStr(task.gridList.Count - roiMotion.Count) + " segments out of " + CStr(task.gridList.Count) + " had > " +
                         Format(correlationSlider.Value / 1000, "0.0%") + " correlation. "
    End Sub
End Class






Public Class Motion_Intersect : Inherits VB_Algorithm
    Dim bgSub As New BGSubtract_Basics
    Dim minCount = 4
    Dim reconstructedRGB As Integer
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        If dst2.Width = 1280 Or dst2.Width = 640 Then minCount = 16
        desc = "Track the max rectangle that covers all the motion until there is no motion in it."
    End Sub
    Private Function buildEnclosingRect(tmp As cv.Mat)
        Dim rectList As New List(Of cv.Rect)
        Dim dots(tmp.Total * 2 - 1) As Integer
        Marshal.Copy(tmp.Data, dots, 0, dots.Length)
        Dim pointList As New List(Of cv.Point)
        For i = 0 To dots.Length - 1 Step 2
            If dots(i) >= 1 And dots(i) < dst2.Width - 2 And dots(i + 1) >= 1 And dots(i + 1) < dst2.Height - 2 Then
                pointList.Add(New cv.Point(dots(i), dots(i + 1)))
            End If
        Next

        Dim flags = 4 Or cv.FloodFillFlags.MaskOnly Or cv.FloodFillFlags.FixedRange
        Dim rect As cv.Rect
        Dim motionMat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim matPoints = dst1(New cv.Rect(1, 1, motionMat.Width - 2, motionMat.Height - 2))
        For Each pt In pointList
            If motionMat.Get(Of Byte)(pt.Y, pt.X) = 0 And matPoints.Get(Of Byte)(pt.Y, pt.X) <> 0 Then
                Dim count = matPoints.FloodFill(motionMat, pt, 255, rect, 0, 0, flags Or (255 << 8))
                If count <= minCount Then Continue For
                rectList.Add(New cv.Rect(rect.X, rect.Y, rect.Width + 1, rect.Height + 1))
            End If
        Next

        labels(3) = "There were " + CStr(CInt(dots.Length / 2)) + " points collected"

        If rectList.Count = 0 Then Return New cv.Rect
        Dim motionRect As cv.Rect = rectList(0)
        For Each r In rectList
            motionRect = motionRect.Union(r)
        Next
        Return motionRect
    End Function
    Public Sub RunVB(src As cv.Mat)
        Static color = src.Clone
        Static lastMotionRect As cv.Rect = task.motionRect
        task.motionFlag = False
        If task.heartBeat Or task.motionRect.Width * task.motionRect.Height > src.Total / 2 Or task.optionsChanged Then
            task.motionFlag = True
        Else
            bgSub.Run(src)
            dst1 = bgSub.dst2
            Dim tmp As New cv.Mat
            cv.Cv2.FindNonZero(dst1, tmp)

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
            task.motionRect = New cv.Rect
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
                color.Rectangle(task.motionRect, cv.Scalar.White, task.lineWidth, task.lineType)
            End If
        End If
        lastMotionRect = task.motionRect
    End Sub
End Class







Public Class Motion_RectTest : Inherits VB_Algorithm
    Dim motion As New Motion_Enclosing
    Public Sub New()
        vbAddAdvice(traceName + ": gOptions frame history slider will impact results.")
        labels(3) = "The white spots show the difference of the constructed image from the current image."
        desc = "Track the RGB image using Motion_Enclosing to isolate the motion"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static lastRects As New List(Of cv.Rect)
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
            Static diff As New Diff_Basics
            diff.lastFrame = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            diff.Run(dst2)
            dst3 = diff.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3.Rectangle(r, task.highlightColor, task.lineWidth, task.lineType)
        End If
    End Sub
End Class









Public Class Motion_HistoryTest : Inherits VB_Algorithm
    Dim diff As New Diff_Basics
    Dim frames As New History_Basics
    Public Sub New()
        gOptions.PixelDiffThreshold.Value = 10
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Detect motion using the last X images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        diff.Run(src)
        dst1 = diff.dst3.Threshold(0, 1, cv.ThresholdTypes.Binary)
        frames.Run(dst1)

        dst2 = frames.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels(2) = "Cumulative diff for the last " + CStr(task.frameHistoryCount) + " frames"
    End Sub
End Class







'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Public Class Motion_History : Inherits VB_Algorithm
    Public motionCore As New Motion_Simple
    Dim frames As New History_Basics
    Public Sub New()
        gOptions.FrameHistory.Value = 10
        desc = "Accumulate differences from the previous BGR images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        motionCore.Run(src)
        dst2 = motionCore.dst2

        frames.Run(dst2)
        dst3 = frames.dst2
    End Sub
End Class






Public Class Motion_Enclosing : Inherits VB_Algorithm
    Dim redMasks As New RedCloud_Masks
    Dim learnRate As Double
    Public motionRect As New cv.Rect
    Public Sub New()
        If dst2.Width >= 1280 Then learnRate = 0.5 Else learnRate = 0.1 ' learn faster with large images (slower frame rate)
        cPtr = BGSubtract_BGFG_Open(4)
        labels(2) = "MOG2 is the best option.  See BGSubtract_Basics to see more options."
        desc = "Build an enclosing rectangle for the motion"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels, learnRate)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr).Threshold(0, 255, cv.ThresholdTypes.Binary)

        redMasks.inputMask = Not dst2
        redMasks.Run(dst2)

        motionRect = New cv.Rect
        If redMasks.sortedCells.Count < 2 Then Exit Sub
        motionRect = redMasks.sortedCells.ElementAt(1).Value.rect
        For i = 2 To redMasks.sortedCells.Count - 1
            Dim cell = redMasks.sortedCells.ElementAt(i).Value
            motionRect = motionRect.Union(cell.rect)
        Next

        If motionRect.Width > dst2.Width / 2 And motionRect.Height > dst2.Height / 2 Then
            motionRect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
        End If
        dst2.Rectangle(motionRect, 255, task.lineWidth, task.lineType)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = BGSubtract_BGFG_Close(cPtr)
    End Sub
End Class







Public Class Motion_Depth : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "Output of MotionRect_Basics showing motion and enclosing rectangle.", "MotionRect point cloud", "Diff of MotionRect Pointcloud and latest pointcloud"}
        desc = "Display the depth data after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then dst2 = task.pcSplit(2).Clone

        If task.motionDetected Then task.pcSplit(2)(task.motionRect).CopyTo(dst2(task.motionRect))

        If standaloneTest() Then
            Static diff As New Diff_Depth32f
            If diff.lastDepth32f.Width = 0 Then diff.lastDepth32f = task.pcSplit(2).Clone
            diff.Run(task.pcSplit(2))
            dst3 = diff.dst2
            dst3.Rectangle(task.motionRect, 255, task.lineWidth)
            diff.lastDepth32f = task.pcSplit(2)
        End If
    End Sub
End Class






Public Class Motion_Grayscale : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "MotionRect_Basics output showing motion and enclosing rectangle.", "MotionRect accumulated grayscale image",
                  "Diff of input and latest accumulated grayscale image"}
        desc = "Display the grayscale image after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Then
            dst2 = src.Clone
        ElseIf task.motionDetected Then
            src(task.motionRect).CopyTo(dst2(task.motionRect))
        Else
            dst2 = src.Clone
        End If

        If standaloneTest() Then
            Static diff As New Diff_Basics
            If diff.lastFrame.Width = 0 Then diff.lastFrame = dst2.Clone
            diff.Run(src)
            dst3 = diff.dst3
            dst3.Rectangle(task.motionRect, 255, task.lineWidth)
            diff.lastFrame = src.Clone
        End If
    End Sub
End Class






Public Class Motion_Basics_QT : Inherits VB_Algorithm
    Dim redMasks As New RedCloud_Masks
    Public bgSub As New BGSubtract_MOG2
    Dim rectList As New List(Of cv.Rect)
    Public Sub New()
        redMasks.imageThresholdPercent = 1.0
        redMasks.cellMinPercent = 0
        desc = "The option-free version of Motion_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.motionDetected = True
        If task.heartBeat Then
            task.motionRect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
            Exit Sub
        End If
        task.motionRect = New cv.Rect

        If src.Channels <> 1 Then
            bgSub.Run(src)
            src = bgSub.dst2
        End If

        dst2 = src

        redMasks.Run(src.Threshold(0, 255, cv.ThresholdTypes.Binary))
        If redMasks.sortedCells.Count < 2 Then
            task.motionDetected = False
            rectList.Clear()
        Else
            Dim nextRect = redMasks.sortedCells.ElementAt(1).Value.rect
            For i = 2 To redMasks.sortedCells.Count - 1
                Dim rc = redMasks.sortedCells.ElementAt(i).Value
                nextRect = nextRect.Union(rc.rect)
            Next

            rectList.Add(nextRect)
            For Each r In rectList
                If task.motionRect.Width = 0 Then task.motionRect = r Else task.motionRect = task.motionRect.Union(r)
            Next
            If rectList.Count > task.frameHistoryCount Then rectList.RemoveAt(0)
            If task.motionRect.Width > dst2.Width / 2 And task.motionRect.Height > dst2.Height / 2 Then
                task.motionRect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
            Else
                If task.motionRect.Width = 0 Or task.motionRect.Height = 0 Then task.motionDetected = False
            End If
        End If

        If standaloneTest() Then
            dst2.Rectangle(task.motionRect, 255, task.lineWidth)
            If redMasks.sortedCells.Count > 1 Then
                labels(2) = CStr(redMasks.sortedCells.Count) + " RedMask cells had motion"
            Else
                labels(2) = "No motion detected"
            End If
            labels(3) = ""
            If task.motionRect.Width > 0 Then
                labels(3) = "Rect width = " + CStr(task.motionRect.Width) + ", height = " + CStr(task.motionRect.Height)
            End If
        End If
    End Sub
End Class








Public Class Motion_BasicsQuarterRes : Inherits VB_Algorithm
    Dim redMasks As New RedCloud_Masks
    Public bgSub As New BGSubtract_MOG2_QT
    Dim rectList As New List(Of cv.Rect)
    Public Sub New()
        redMasks.imageThresholdPercent = 1.0
        redMasks.cellMinPercent = 0
        desc = "The option-free version of Motion_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.motionDetected = True
        task.motionRect = New cv.Rect
        dst2 = src.Resize(task.quarterRes)

        If src.Channels <> 1 Then
            bgSub.Run(dst2)
            dst2 = bgSub.dst2
        End If

        redMasks.Run(dst2.Threshold(0, 255, cv.ThresholdTypes.Binary))
        If redMasks.sortedCells.Count < 2 Then
            task.motionDetected = False
            rectList.Clear()
        Else
            Dim nextRect = redMasks.sortedCells.ElementAt(1).Value.rect
            For i = 2 To redMasks.sortedCells.Count - 1
                Dim rc = redMasks.sortedCells.ElementAt(i).Value
                nextRect = nextRect.Union(rc.rect)
            Next

            rectList.Add(nextRect)
            For Each r In rectList
                If task.motionRect.Width = 0 Then task.motionRect = r Else task.motionRect = task.motionRect.Union(r)
            Next
            If rectList.Count > task.frameHistoryCount Then rectList.RemoveAt(0)
            If task.motionRect.Width > dst2.Width / 2 And task.motionRect.Height > dst2.Height / 2 Then
                task.motionRect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
            Else
                If task.motionRect.Width = 0 Or task.motionRect.Height = 0 Then task.motionDetected = False
            End If
        End If

        If standaloneTest() Then
            dst2.Rectangle(task.motionRect, 255, task.lineWidth)
            If redMasks.sortedCells.Count > 1 Then
                labels(2) = CStr(redMasks.sortedCells.Count) + " RedMask cells had motion"
            Else
                labels(2) = "No motion detected"
            End If
            labels(3) = ""
            If task.motionRect.Width > 0 Then
                labels(3) = "Rect width = " + CStr(task.motionRect.Width) + ", height = " + CStr(task.motionRect.Height)
            End If
        End If

        Dim ratio = CInt(src.Width / dst2.Width)
        If src.Size <> dst2.Size Then
            Dim r = task.motionRect
            task.motionRect = New cv.Rect(r.X * ratio, r.Y * ratio, r.Width * ratio, r.Height * ratio)
        End If

        If task.motionRect.Width < dst2.Width Then
            dst2.Rectangle(task.motionRect, 255, task.lineWidth)
            Dim pad = dst2.Width / 20
            Dim r = task.motionRect
            r = New cv.Rect(r.X - pad, r.Y - pad, r.Width + pad * 2, r.Height + pad * 2)
            task.motionRect = validateRect(r, ratio)
            dst2.Rectangle(task.motionRect, 255, task.lineWidth + 4)
        End If
    End Sub
End Class









Public Class Motion_PointCloud : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "Output of MotionRect_Basics showing motion and enclosing rectangle.", "MotionRect point cloud", "Diff of MotionRect Pointcloud and latest pointcloud"}
        desc = "Display the pointcloud after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            dst2 = task.pointCloud.Clone
        ElseIf task.motionDetected Then
            task.pointCloud(task.motionRect).CopyTo(dst2(task.motionRect))
        Else
            dst2 = task.pointCloud
        End If
        If standaloneTest() Then
            Static diff As New Diff_Depth32f
            If diff.lastDepth32f.Width = 0 Then diff.lastDepth32f = task.pcSplit(2).Clone
            diff.Run(task.pcSplit(2))
            dst3 = diff.dst2
            dst3.Rectangle(task.motionRect, 255, task.lineWidth)
            diff.lastDepth32f = task.pcSplit(2)
        End If
    End Sub
End Class






Public Class Motion_Color : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "MotionRect_Basics output showing motion and enclosing rectangle.", "MotionRect accumulated color image",
                  "Diff of input and latest accumulated color image"}
        desc = "Display the color image after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            dst2 = src.Clone
        ElseIf task.motionDetected Then
            src(task.motionRect).CopyTo(dst2(task.motionRect))
        Else
            dst2 = src
        End If
        If standaloneTest() And task.motionDetected Then dst2.Rectangle(task.motionRect, cv.Scalar.White, task.lineWidth)
    End Sub
End Class
