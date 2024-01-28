Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Threading
Public Class MotionRect_Basics : Inherits VB_Algorithm
    Dim bgSub As New BGSubtract_Basics
    Dim redCPP As New RedCloud_Color
    Public showDiff As Boolean
    Public Sub New()
        redCPP.imageThresholdPercent = 1.0
        redCPP.cellMinPercent = 0
        vbAddAdvice(traceName + ": redOptions are used as well as BGSubtract options.")
        desc = "Use floodfill to find all the real motion in an image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.motionDetected = False
        task.motionReset = True

        bgSub.Run(src)
        If standalone Or showIntermediate() Or showDiff Then dst2 = bgSub.dst2

        redCPP.Run(bgSub.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary))
        If redCPP.sortedCells.Count < 2 Then
            task.motionReset = False
            task.motionRect = New cv.Rect
            Exit Sub
        End If

        Dim nextRect = redCPP.sortedCells.ElementAt(1).Value.rect
        For i = 2 To redCPP.sortedCells.Count - 1
            Dim rc = redCPP.sortedCells.ElementAt(i).Value
            nextRect = nextRect.Union(rc.rect)
        Next

        Static rectList As New List(Of cv.Rect)
        rectList.Add(nextRect)
        task.motionRect = New cv.Rect
        For Each r In rectList
            If task.motionRect.Width = 0 Then task.motionRect = r Else task.motionRect = task.motionRect.Union(r)
        Next
        If rectList.Count > gOptions.FrameHistory.Value Then rectList.RemoveAt(0)
        If task.motionRect.Width > dst2.Width / 2 And task.motionRect.Height > dst2.Height / 2 Then
            rectList.Clear()
            task.motionRect = New cv.Rect
        Else
            task.motionReset = False
            If task.motionRect.Width <> 0 Or task.motionRect.Height <> 0 Then task.motionDetected = True
        End If

        If standalone Or showIntermediate() Or showDiff Then dst2.Rectangle(task.motionRect, 255, task.lineWidth)
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






Public Class MotionRect_Rect : Inherits VB_Algorithm
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
        If task.heartBeat Or task.motionRect.Width * task.motionRect.Height > src.Total / 2 Or task.optionsChanged Then
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







Public Class MotionRect_Rect1 : Inherits VB_Algorithm
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
                If lastRects.Count > gOptions.FrameHistory.Value Then lastRects.RemoveAt(0)
            Else
                lastRects.Clear()
            End If
        End If

        If standalone Then
            Static diff As New Diff_Basics
            diff.lastFrame = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            diff.Run(dst2)
            dst3 = diff.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3.Rectangle(r, task.highlightColor, task.lineWidth, task.lineType)
        End If
    End Sub
End Class









Public Class MotionRect_HistoryTest : Inherits VB_Algorithm
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
Public Class MotionRect_History2 : Inherits VB_Algorithm
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






Public Class MotionRect_Enclosing : Inherits VB_Algorithm
    Dim redCPP As New RedCloud_Color
    Public motionRect As New cv.Rect
    Public Sub New()
        cPtr = BGSubtract_BGFG_Open(4)
        labels(2) = "MOG2 is the best option.  See BGSubtract_Basics to see more options."
        desc = "Build an enclosing rectangle for the motion"
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
        If redCPP.sortedCells.Count < 2 Then Exit Sub
        motionRect = redCPP.sortedCells.ElementAt(1).Value.rect
        For i = 2 To redCPP.sortedCells.Count - 1
            Dim cell = redCPP.sortedCells.ElementAt(i).Value
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








Public Class MotionRect_PointCloud : Inherits VB_Algorithm
    Public diff As New Diff_Depth32f
    Public Sub New()
        labels = {"", "Output of MotionRect_Basics showing motion and enclosing rectangle.", "MotionRect point cloud", "Diff of MotionRect Pointcloud and latest pointcloud"}
        desc = "Display the pointcloud after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Or task.motionReset Then dst2 = task.pointCloud.Clone

        If task.motionDetected Then task.pointCloud(task.motionRect).CopyTo(dst2(task.motionRect))

        If standalone Or showIntermediate() Then
            If diff.lastDepth32f.Width = 0 Then diff.lastDepth32f = task.pcSplit(2).Clone
            diff.Run(task.pcSplit(2))
            dst3 = diff.dst2
            dst3.Rectangle(task.motionRect, 255, task.lineWidth)
            diff.lastDepth32f = task.pcSplit(2)
        End If
    End Sub
End Class








Public Class MotionRect_Depth : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "Output of MotionRect_Basics showing motion and enclosing rectangle.", "MotionRect point cloud", "Diff of MotionRect Pointcloud and latest pointcloud"}
        desc = "Display the depth data after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Or task.motionReset Then dst2 = task.pcSplit(2).Clone

        If task.motionDetected Then task.pcSplit(2)(task.motionRect).CopyTo(dst2(task.motionRect))

        If standalone Or showIntermediate() Then
            Static diff As New Diff_Depth32f
            If diff.lastDepth32f.Width = 0 Then diff.lastDepth32f = task.pcSplit(2).Clone
            diff.Run(task.pcSplit(2))
            dst3 = diff.dst2
            dst3.Rectangle(task.motionRect, 255, task.lineWidth)
            diff.lastDepth32f = task.pcSplit(2)
        End If
    End Sub
End Class






Public Class MotionRect_Grayscale : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "MotionRect_Basics output showing motion and enclosing rectangle.", "MotionRect accumulated grayscale image",
                  "Diff of input and latest accumulated grayscale image"}
        desc = "Display the grayscale image after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Or task.motionReset Then dst2 = src.Clone
        If task.motionDetected Then src(task.motionRect).CopyTo(dst2(task.motionRect))

        If standalone Or showIntermediate() Then
            Static diff As New Diff_Basics
            If diff.lastFrame.Width = 0 Then diff.lastFrame = dst2.Clone
            diff.Run(src)
            dst3 = diff.dst3
            dst3.Rectangle(task.motionRect, 255, task.lineWidth)
            diff.lastFrame = src.Clone
        End If
    End Sub
End Class






Public Class MotionRect_Color : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "MotionRect_Basics output showing motion and enclosing rectangle.", "MotionRect accumulated color image",
                  "Diff of input and latest accumulated color image"}
        desc = "Display the color image after updating only the motion rectangle.  Resync every heartbeat."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Or task.motionReset Then dst2 = src.Clone
        If task.motionDetected Then src(task.motionRect).CopyTo(dst2(task.motionRect))

        If (standalone Or showIntermediate()) And task.motionDetected Then
            dst2.Rectangle(task.motionRect, cv.Scalar.White, task.lineWidth)
        End If
    End Sub
End Class