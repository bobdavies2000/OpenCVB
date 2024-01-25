Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Threading
Public Class MotionRect_Basics : Inherits VB_Algorithm
    Dim bgSub As New BGSubtract_Basics
    Dim redCPP As New RedCloud_CPP
    Public rect As cv.Rect
    Public showDiff As Boolean
    Public Sub New()
        redOptions.UseColor.Checked = True
        redCPP.imageThresholdPercent = 1.0
        redCPP.cellMinPercent = 0
        vbAddAdvice(traceName + ": redOptions are used as well as BGSubtract options.")
        desc = "Use floodfill to find all the real motion in an image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bgSub.Run(src)
        If standalone Or showIntermediate() Or showDiff Then dst2 = bgSub.dst2

        If task.pcSplit Is Nothing Then Exit Sub

        redCPP.Run(bgSub.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary))
        If redCPP.sortedCells.Count < 3 Then Exit Sub

        rect = redCPP.sortedCells.ElementAt(2).Value.rect
        For Each key In redCPP.sortedCells
            Dim rc = key.Value
            If rc.index = 1 Then Continue For
            rect = rect.Union(rc.rect)
        Next
        If standalone Or showIntermediate() Or showDiff Then dst2.Rectangle(rect, 255, task.lineWidth)

        labels(2) = CStr(redCPP.sortedCells.Count) + " cells were found with " + CStr(redCPP.classCount) + " flood points"
    End Sub
End Class





Public Class MotionRect_Grid : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Correlation Threshold", 800, 1000, 990)
        vbAddAdvice(traceName + ": local options 'Correlation Threshold' controls how well the image matches.")
        desc = "Detect Motion in the color image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static correlationSlider = findSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.frameCount < 10 Or task.optionsChanged Then dst3 = src.Clone

        Dim updateCount As Integer
        Parallel.ForEach(Of cv.Rect)(task.gridList,
            Sub(roi)
                Dim correlation As New cv.Mat
                cv.Cv2.MatchTemplate(src(roi), dst3(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                    Interlocked.Increment(updateCount)
                    src(roi).CopyTo(dst3(roi))
                End If
            End Sub)
        dst2 = src
        labels(2) = "Motion added to dst3 for " + CStr(updateCount) + " segments out of " + CStr(task.gridList.Count)
        labels(3) = CStr(task.gridList.Count - updateCount) + " segments out of " + CStr(task.gridList.Count) + " had > " +
                         Format(correlationSlider.Value / 1000, "0.0%") + " correlation.  Artifacts will appear below if correlation threshold is too low."
    End Sub
End Class








Public Class MotionRect_History : Inherits VB_Algorithm
    Dim motion As New Motion_History
    Dim minCount = 4
    Dim reconstructedRGB As Integer
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
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
        If heartBeat() Or task.motionRect.Width * task.motionRect.Height > src.Total / 2 Or task.optionsChanged Then
            task.motionFlag = True
        Else
            motion.Run(src)
            dst1 = motion.dst2
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

        If standalone Or showIntermediate() Then
            dst2 = dst1
            If task.motionRect.Width > 0 And task.motionRect.Height > 0 Then
                dst3(task.motionRect).SetTo(255)
                src(task.motionRect).CopyTo(dst2(task.motionRect))
            End If
        End If

        If standalone Or showIntermediate() Then
            If task.motionRect.Width > 0 And task.motionRect.Height > 0 Then
                src(task.motionRect).CopyTo(dst0(task.motionRect))
                color.Rectangle(task.motionRect, cv.Scalar.White, task.lineWidth, task.lineType)
            End If
        End If
        lastMotionRect = task.motionRect
    End Sub
End Class





Public Class Motion_RectNew : Inherits VB_Algorithm
    Dim motion As New Motion_Basics
    Dim minCount = 4
    Dim reconstructedRGB As Integer
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
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
        If heartBeat() Or task.motionRect.Width * task.motionRect.Height > src.Total / 2 Or task.optionsChanged Then
            task.motionFlag = True
        Else
            motion.Run(src)
            dst1 = motion.dst2
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

        If standalone Or showIntermediate() Then
            dst2 = dst1
            If task.motionRect.Width > 0 And task.motionRect.Height > 0 Then
                dst3(task.motionRect).SetTo(255)
                src(task.motionRect).CopyTo(dst2(task.motionRect))
            End If
        End If

        If standalone Or showIntermediate() Then
            If task.motionRect.Width > 0 And task.motionRect.Height > 0 Then
                src(task.motionRect).CopyTo(dst0(task.motionRect))
                color.Rectangle(task.motionRect, cv.Scalar.White, task.lineWidth, task.lineType)
            End If
        End If
        lastMotionRect = task.motionRect
    End Sub
End Class







Public Class Motion_Rect1 : Inherits VB_Algorithm
    Dim motion As New MotionRect_Enclosing
    Public Sub New()
        vbAddAdvice(traceName + ": gOptions frame history slider will impact results.")
        labels(3) = "The white spots show the difference of the constructed image from the current image."
        desc = "Track the RGB image using MotionRect_Enclosing to isolate the motion"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static lastRects As New List(Of cv.Rect)
        motion.Run(src)
        Dim r = motion.motionRect
        If heartBeat() Or r.Width * r.Height > src.Total / 2 Or task.frameCount < 50 Then
            dst2 = src.Clone
            lastRects.Clear()
        Else
            If r.Width > 0 And r.Height > 0 Then
                For Each rect In lastRects
                    r = r.Union(rect)
                Next
                src(r).CopyTo(dst2(r))
                lastRects.Add(r)
                If lastRects.Count > gOptions.FrameHistory.Value Then lastRects.RemoveAt(0)
            Else
                lastRects.Clear()
            End If
        End If

        If standalone Then
            Static diff As New Diff_Basics
            diff.lastGray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            diff.Run(dst2)
            dst3 = diff.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3.Rectangle(r, task.highlightColor, task.lineWidth, task.lineType)
        End If
    End Sub
End Class









Public Class Motion_History : Inherits VB_Algorithm
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
Public Class Motion_History2 : Inherits VB_Algorithm
    Public motionCore As New Motion_Core
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







Public Class Motion_History3 : Inherits VB_Algorithm
    Public bgSub As New BGSubtract_Basics
    Dim frames As New History_Basics
    Public Sub New()
        desc = "Create motion history based on background subtraction."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bgSub.Run(src)
        dst2 = bgSub.dst2
    End Sub
End Class







Public Class Motion_RectAlt : Inherits VB_Algorithm
    Dim motion As New Motion_History
    Dim options As New Options_Flood
    Dim smallSize As New cv.Size(160, 120)
    Dim minCount = 2
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        If dst0.Height = 480 Or dst0.Height = 240 Then minCount = 4
        If dst0.Height = 720 Or dst0.Height = 360 Or dst0.Height = 180 Then minCount = 16
        desc = "Construct the BGR image from a heartbeat image and the portion of the BGR image that has changed."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim rectList As New List(Of cv.Rect)
        Dim restoreF = CInt(src.Width / smallSize.Width)
        Dim srcSmall = src.Resize(smallSize, cv.InterpolationFlags.Nearest)
        dst0 = New cv.Mat(smallSize, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(smallSize, cv.MatType.CV_8UC3, 0)
        Static dst As New cv.Mat(srcSmall.Size, cv.MatType.CV_8UC3, 0)
        If heartBeat() Then
            dst = task.color.Resize(smallSize, cv.InterpolationFlags.Nearest)
            dst1.SetTo(0)
            task.motionRect = New cv.Rect
        Else
            motion.Run(srcSmall)
            dst3 = motion.dst2

            Dim tmp As New cv.Mat
            cv.Cv2.FindNonZero(dst3, tmp)

            Dim dots(tmp.Total * 2 - 1) As Integer
            If tmp.Total > srcSmall.Total / 4 Then
                dst = srcSmall
            ElseIf tmp.Total > 0 Then
                Marshal.Copy(tmp.Data, dots, 0, dots.Length)
                Dim pointList As New List(Of cv.Point)
                For i = 0 To dots.Length - 1 Step 2
                    If dots(i) >= 1 And dots(i) < dst1.Width - 2 And dots(i + 1) >= 1 And dots(i + 1) < dst1.Height - 2 Then
                        pointList.Add(New cv.Point(dots(i), dots(i + 1)))
                    End If
                Next

                Dim flags = 4 Or cv.FloodFillFlags.MaskOnly Or cv.FloodFillFlags.FixedRange
                Dim rect As cv.Rect
                dst0.SetTo(0)
                Dim matPoints = dst3(New cv.Rect(1, 1, dst3.Width - 2, dst3.Height - 2))
                For Each pt In pointList
                    If dst0.Get(Of Byte)(pt.Y, pt.X) = 0 And matPoints.Get(Of Byte)(pt.Y, pt.X) <> 0 Then
                        Dim count = matPoints.FloodFill(dst0, pt, 255, rect, 0, 0, flags Or (255 << 8))
                        rectList.Add(New cv.Rect(rect.X, rect.Y, rect.Width + 1, rect.Height + 1))
                    End If
                Next
                For Each r In rectList
                    If r.X <> 0 And r.Y <> 0 Then
                        task.motionRect = If(task.motionRect.Width = 0, r, task.motionRect.Union(r))
                        dst3.Rectangle(r, cv.Scalar.White, 1)
                    End If
                Next
                dst3.Rectangle(task.motionRect, cv.Scalar.White, 1)
            End If
            labels(3) = "There were " + CStr(CInt(dots.Length / 2)) + " points collected"
        End If
        If task.motionRect.Width > 0 And task.motionRect.Height > 0 Then
            Dim r = task.motionRect
            Dim motionRect = New cv.Rect(r.Left / restoreF, r.Top / restoreF, r.Width / restoreF, r.Height / restoreF)
            If motionRect.Width > 0 And motionRect.Height > 0 Then ' could have rounded to 0.
                srcSmall(motionRect).CopyTo(dst1(motionRect))
                srcSmall(motionRect).CopyTo(dst(motionRect))
            End If
        End If
        dst2 = dst.Resize(dst2.Size)
        dst2.Rectangle(New cv.Rect(0, 0, dst2.Width, dst2.Height), cv.Scalar.Black, 1)
        labels(2) = CStr(rectList.Count) + " rects were found"
    End Sub
End Class








Public Class MotionRect_Filter : Inherits VB_Algorithm
    Public motion As New Motion_Contours
    Public stableImg As cv.Mat
    Dim dMax As New Depth_StableMax
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Use motion-filtered pixel values")
            radio.addRadio("Use original (unchanged) pixels")
            radio.check(0).Checked = True
        End If
        labels(2) = "Motion-filtered image"
        desc = "Motion-Filtered Images (MFI) - update only the changed regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dMax.Run(src)

        motion.Run(task.gray)
        labels(3) = motion.labels(3)
        dst3 = If(motion.dst3.Channels = 1, motion.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR), motion.dst3.Clone)

        Dim radioVal As Integer
        Static frm As OptionsRadioButtons = findfrm(traceName + " Radio Buttons")
        For radioVal = 0 To frm.check.Count - 1
            If frm.check(radioVal).Checked Then Exit For
        Next

        If task.motionReset Or firstPass Or radioVal = 1 Then
            stableImg = src.Clone
        End If

        dst2 = stableImg.Clone
    End Sub
End Class








Public Class MotionRect_Sobel : Inherits VB_Algorithm
    Dim mfi As New MotionRect_Filter
    Dim sobel As New Edge_Sobel_Old
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Pixel threshold to zero", 0, 255, 100)
        End If

        labels(2) = "Sobel edges of Motion-Filtered RGB"
        desc = "Motion-Filtered Images (MFI) - Stabilize the Sobel output with MFD"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Pixel threshold to zero")
        mfi.Run(src)
        dst3 = mfi.dst3
        labels(3) = mfi.labels(3)

        sobel.Run(mfi.dst2)
        dst2 = sobel.dst2.Threshold(thresholdSlider.Value, 0, cv.ThresholdTypes.Tozero).Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







Public Class MotionRect_Enclosing : Inherits VB_Algorithm
    Dim redCPP As New RedCloud_CPP
    Public motionRect As New cv.Rect
    Public Sub New()
        redOptions.UseColor.Checked = True
        cPtr = BGSubtract_BGFG_Open(4)
        labels(2) = "MOG2 is the best option.  See BGSubtract_Basics to see more options."
        desc = "Build an enclosing rectangle for the supplied pointlist"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr).Threshold(0, 255, cv.ThresholdTypes.Binary)

        redCPP.inputMask = Not dst2
        redCPP.Run(dst2)

        motionRect = New cv.Rect
        If redCPP.sortedCells.Count > 1 Then
            Dim rect As cv.Rect = redCPP.sortedCells.ElementAt(1).Value.rect
            For i = 2 To redCPP.sortedCells.Count - 1
                Dim cell = redCPP.sortedCells.ElementAt(i).Value
                rect = rect.Union(cell.rect)
            Next
            motionRect = rect
        End If
        If motionRect.Width > dst2.Width / 2 And motionRect.Height > dst2.Height / 2 Then
            motionRect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
        End If
        dst2.Rectangle(motionRect, 255, task.lineWidth, task.lineType)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = BGSubtract_BGFG_Close(cPtr)
    End Sub
End Class







Public Class MotionRect_Depth : Inherits VB_Algorithm
    Public motion As New MotionRect_Basics
    Public rect As cv.Rect
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        If standalone Then motion.showDiff = True
        labels = {"", "Output of MotionRect_Basics showing motion and enclosing rectangle.", "MotionRect point cloud", "Diff of MotionRect Pointcloud and latest pointcloud"}
        desc = "Display the pointcloud after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If heartBeat() Then dst2 = task.pcSplit(2).Clone

        motion.Run(src)
        dst1 = motion.dst2
        If heartBeat() Or motion.rect.Width > dst2.Width / 2 And motion.rect.Height > dst2.Height / 2 Then rect = motion.rect

        If motion.rect.Width = 0 And motion.rect.Height = 0 Then Exit Sub
        rect = rect.Union(motion.rect)

        task.pcSplit(2)(rect).CopyTo(dst2(rect))

        If standalone Or showIntermediate() Then
            Static diff As New Diff_Depth32f
            diff.lastDepth32f = dst2
            diff.Run(task.pcSplit(2))
            dst3 = diff.dst2
            dst3.Rectangle(rect, 255, task.lineWidth)
        End If
    End Sub
End Class







Public Class MotionRect_PointCloud : Inherits VB_Algorithm
    Public motion As New MotionRect_Basics
    Public rect As cv.Rect = New cv.Rect(0, 0, task.workingRes.Width, task.workingRes.Height)
    Public diff As New Diff_Depth32f
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        If standalone Then motion.showDiff = True
        labels = {"", "Output of MotionRect_Basics showing motion and enclosing rectangle.", "MotionRect point cloud", "Diff of MotionRect Pointcloud and latest pointcloud"}
        desc = "Display the pointcloud after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If heartBeat() Then
            motion.rect = New cv.Rect
            rect = New cv.Rect
            dst2 = task.pointCloud.Clone
        End If

        motion.Run(src)
        dst1 = motion.dst2
        If heartBeat() Or motion.rect.Width > dst2.Width / 2 And motion.rect.Height > dst2.Height / 2 Then rect = motion.rect

        If motion.rect.Width = 0 And motion.rect.Height = 0 Then Exit Sub
        rect = rect.Union(motion.rect)

        task.pointCloud(rect).CopyTo(dst2(rect))

        If standalone Or showIntermediate() Then
            If diff.lastDepth32f.Width = 0 Then diff.lastDepth32f = task.pcSplit(2).Clone
            diff.Run(task.pcSplit(2))
            dst3 = diff.dst2
            dst3.Rectangle(rect, 255, task.lineWidth)
            diff.lastDepth32f = task.pcSplit(2)
        End If
    End Sub
End Class
