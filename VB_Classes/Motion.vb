Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Motion_Basics : Inherits VB_Algorithm
    Public options As New Options_BGSubtract
    Public Sub New()
        cPtr = BGSubtract_BGFG_Open(options.currMethod)
        labels(2) = "BGSubtract output"
        desc = "Detect motion using background subtraction algorithms in OpenCV - some only available in C++"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If task.optionsChanged Then
            BGSubtract_BGFG_Close(cPtr)
            cPtr = BGSubtract_BGFG_Open(options.currMethod)
        End If

        Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr)
        labels(2) = options.methodDesc
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = BGSubtract_BGFG_Close(cPtr)
    End Sub
End Class







'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Public Class Motion_Core : Inherits VB_Algorithm
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
        If heartBeat() Then cumulativePixels = 0
        If diff.changedPixels > 0 Or heartBeat() Then
            cumulativePixels += diff.changedPixels
            task.motionReset = cumulativePixels / src.Total > options.cumulativePercentThreshold Or
                               diff.changedPixels > options.motionThreshold Or task.optionsChanged
            If task.motionReset Or heartBeat() Then
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
    Public Sub RunVB(src as cv.Mat)
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
                Dim mm as mmData = vbMinMax(correlation)
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

        If heartBeat() Then dst2 = src.Clone Else src.CopyTo(dst2, dst3)
    End Sub
End Class







Public Class Motion_CCmerge : Inherits VB_Algorithm
    Dim motionCC As New Motion_ThruCorrelation
    Public Sub New()
        desc = "Use the correlation coefficient to maintain an up-to-date image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
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
    Public Sub RunVB(src as cv.Mat)
        src = task.gray

        Static lastFrame As cv.Mat = src
        cv.Cv2.Absdiff(src, lastFrame, dst2)
        dst2 = dst2.Threshold(gOptions.PixelDiffThreshold.Value, 255, cv.ThresholdTypes.Binary)
        changedPixels = dst2.CountNonZero
        task.motionFlag = changedPixels > 0

        Static changeCount As Integer, frames As Integer
        If task.motionFlag Then changeCount += 1
        frames += 1
        If heartBeat() Then
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
    Public motion As New MotionRect_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32FC3, 0)
        labels(2) = "The yellow rectangle indicates where the motion is and only that portion of the point cloud and depth mask is updated."
        desc = "Rebuild the point cloud based on the BGR motion history."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        motion.Run(src)
        dst2 = src

        If task.motionFlag Then
            dst0 = src.Clone
            dst1 = task.noDepthMask.Clone
            dst3 = task.pointCloud.Clone
            labels(3) = motion.labels(2)
        End If

        If task.motionRect = New cv.Rect Then Exit Sub

        src(task.motionRect).CopyTo(dst0(task.motionRect))
        task.noDepthMask(task.motionRect).CopyTo(dst1(task.motionRect))
        task.pointCloud(task.motionRect).CopyTo(dst3(task.motionRect))
    End Sub
End Class











Public Class Motion_Contours : Inherits VB_Algorithm
    Public motion As New Motion_History2
    Dim contours As New Contour_Basics
    Public intersect As New Rectangle_Intersection
    Public changedPixels As Integer
    Public cumulativePixels As Integer
    Public enclosingRects As New List(Of cv.Rect)
    Public Sub New()
        labels(2) = "Enclosing rectangles are yellow in dst2 and dst3"
        desc = "Detect contours in the motion data and the resulting rectangles"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static saveFrameCount = task.frameCount
        If saveFrameCount = task.frameCount Then Exit Sub ' results are the same
        saveFrameCount = task.frameCount

        motion.Run(src)
        dst3 = motion.dst2
        changedPixels = motion.motionCore.diff.changedPixels

        If changedPixels > 0 Then
            cumulativePixels += changedPixels
            contours.Run(dst3)
            If contours.allContours.Count Then
                intersect.inputRects.Clear()
                For Each c In contours.contourlist
                    Dim r = cv.Cv2.BoundingRect(c)
                    If r.X < 0 Then r.X = 0
                    If r.Y < 0 Then r.Y = 0
                    If r.X + r.Width > dst3.Width Then r.Width = dst3.Width - r.X
                    If r.Y + r.Height > dst3.Height Then r.Height = dst3.Height - r.Y
                    intersect.inputRects.Add(r)
                Next
                intersect.Run(src)
                If intersect.enclosingRects.Count > 0 Then enclosingRects = New List(Of cv.Rect)(intersect.enclosingRects)

                If dst3.Channels = 1 Then dst3 = dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                labels(3) = "Motion detected"
            Else
                labels(3) = "No motion detected with contours > " + CStr(gOptions.minPixelsSlider.Value)
            End If
        End If

        If enclosingRects.Count = 0 Then setTrueText("There is no motion in the image.", 3)
        For Each r In enclosingRects
            dst2.Rectangle(r, cv.Scalar.Yellow, task.lineWidth)
            dst3.Rectangle(r, cv.Scalar.Yellow, task.lineWidth)
        Next
        If task.heartBeat Then
            enclosingRects.Clear()
            dst2.SetTo(0)
        End If
    End Sub
End Class









Public Class Motion_Contours2 : Inherits VB_Algorithm
    Public motion As New Motion_MinRect
    Dim contours As New Contour_Largest
    Public cumulativePixels As Integer
    Public Sub New()
        labels(2) = "Enclosing rectangles are yellow in dst2 and dst3"
        desc = "Detect contours in the motion data and the resulting rectangles"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst2 = src
        motion.Run(src)
        dst3 = motion.dst3
        Dim changedPixels = dst3.CountNonZero

        If heartBeat() Then cumulativePixels = changedPixels Else cumulativePixels += changedPixels
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
    Public Sub RunVB(src as cv.Mat)
        motion.Run(src)
        dst2 = motion.dst2

        Dim nonzeros = dst2.FindNonZero()
        If heartBeat() Then dst3.SetTo(0)
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
        If heartBeat() Then
            dst1 = src.Clone
            dst2.SetTo(0)
        End If

        cv.Cv2.Absdiff(src, dst1, dst3)
        cumulativePixels = dst3.CountNonZero
        dst2 = dst3.Threshold(gOptions.PixelDiffThreshold.Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class