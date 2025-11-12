Imports System.Threading
Imports cv = OpenCvSharp
Public Class Motion_Basics : Inherits TaskParent
    Public motionList As New List(Of Integer)
    Dim diff As New Diff_Basics
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "The motion mask"
        desc = "Find all the grid rects that had motion since the last frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.algorithmPrep = False Then Exit Sub

        If src.Channels <> 1 Then src = task.gray
        If task.heartBeat Or task.optionsChanged Then dst2 = src.Clone

        diff.lastFrame = dst2
        diff.Run(src)

        motionList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim diffCount = diff.dst2(task.gridRects(i)).CountNonZero
            If diffCount >= task.motionThreshold Then
                If motionList.Contains(i) = False Then motionList.Add(i)
            End If
        Next

        dst3.SetTo(0)
        task.motionRect = If(motionList.Count > 0, task.gridRects(motionList(0)), New cv.Rect)
        For Each index In motionList
            Dim rect = task.gridRects(index)
            task.motionRect = task.motionRect.Union(rect)
            src(rect).CopyTo(dst2(rect))
            dst3(rect).SetTo(255)
        Next

        task.motionMask = dst3.Clone
        labels(2) = CStr(motionList.Count) + " grid rects had motion."
    End Sub
End Class





Public Class Motion_FromCorrelation_MP : Inherits TaskParent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Correlation Threshold", 800, 1000, 950)
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





Public Class Motion_FromCorrelation : Inherits TaskParent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Correlation Threshold", 800, 1000, 950)
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






Public Class Motion_FromEdge : Inherits TaskParent
    Dim cAccum As New Edge_CannyAccum
    Public Sub New()
        desc = "Detect motion from pixels less than max value in an accumulation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        cAccum.Run(src)

        Dim mm = GetMinMax(cAccum.dst2)
        labels(3) = "Max value = " + CStr(mm.maxVal) + " min value = " + CStr(mm.minVal)

        dst2 = cAccum.dst2.Threshold(mm.maxVal, 255, cv.ThresholdTypes.TozeroInv)
        dst3 = cAccum.dst2.InRange(1, 254)
    End Sub
End Class







Public Class Motion_ValidateBasics : Inherits TaskParent
    Dim diff As New Diff_Basics
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "Current grayscale image"
        labels(2) = "Grayscale image constructed from previous image + motion rect of current image."
        labels(3) = "Highlighted difference of task.gray and the one built with the motion data.  "
        desc = "Compare task.gray to constructed image to verify Motion_Basics is working"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = task.motionBasics.dst2.Clone()

        diff.lastFrame = dst2
        diff.Run(dst1)
        dst3 = diff.dst3.Threshold(task.motionThreshold, 255, cv.ThresholdTypes.Binary)

        dst3.Rectangle(task.motionRect, white, task.lineWidth)
        SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(task.motionThreshold) +
                    " pixels different: " + CStr(task.motionBasics.motionList.Count), 3)
    End Sub
End Class





Public Class Motion_BasicsAccum : Inherits TaskParent
    Public mCore As New Motion_CoreAccum
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        labels(3) = "Updated task.motionRect"
        desc = "Use the motionlist of rects to create one motion rectangle."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        mCore.Run(src)

        If task.heartBeat Then dst2 = task.gray

        dst3.SetTo(0)
        If mCore.motionList.Count > 0 Then
            task.motionRect = task.gridRects(mCore.motionList(0))
            For Each index In mCore.motionList
                task.motionRect = task.motionRect.Union(task.gridRects(index))
            Next
            dst3(task.motionRect).SetTo(255)
            task.gray(task.motionRect).CopyTo(dst2(task.motionRect))
        Else
            task.motionRect = New cv.Rect
        End If

        labels(2) = CStr(mCore.motionList.Count) + " grid rect's or " +
                    Format(mCore.motionList.Count / task.gridRects.Count, "0.0%") +
                    " of bricks had motion."
    End Sub
End Class




Public Class Motion_CoreAccum : Inherits TaskParent
    Public motionList As New List(Of Integer)
    Dim diff As New Diff_Basics
    Dim motionLists As New List(Of List(Of Integer))
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "The motion mask"
        desc = "Accumulate grid rects that had motion in the last X frames."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray
        If task.heartBeat Or task.optionsChanged Then dst2 = src.Clone

        diff.Run(src)

        motionList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim diffCount = diff.dst2(task.gridRects(i)).CountNonZero
            If diffCount >= task.motionThreshold Then
                If motionList.Contains(i) = False Then motionList.Add(i)
            End If
        Next

        motionLists.Add(motionList)

        dst3.SetTo(0)
        For Each mList In motionLists
            For Each index In motionList
                Dim rect = task.gridRects(index)
                src(rect).CopyTo(dst2(rect))
                dst3(rect).SetTo(255)
            Next
        Next

        If motionLists.Count > 10 Then motionLists.RemoveAt(0)

        task.motionMask = dst3.Clone
        labels(2) = CStr(motionList.Count) + " grid rects had motion."
    End Sub
End Class





Public Class Motion_PointCloud : Inherits TaskParent
    Public originalPointcloud As cv.Mat
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "The difference between the latest pointcloud and the motion-adjusted point cloud."
        labels(2) = "Point cloud after updating with the motion mask changes."
        labels(3) = "Task.pointcloud for the current frame."
        desc = "Point cloud after updating with the motion mask"
    End Sub
    Public Shared Function checkNanInf(pc As cv.Mat) As cv.Mat
        ' these don't work because there are NaN's and Infinity's (both are often present)
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
    Public Sub preparePointcloud()
        If task.gOptions.gravityPointCloud.Checked Then
            '******* this is the gravity rotation *******
            task.gravityCloud = (task.pointCloud.Reshape(1,
                            task.rows * task.cols) * task.gMatrix).ToMat.Reshape(3, task.rows)
            task.pointCloud = task.gravityCloud
        End If

        ' The stereolabs camera has some weird -inf and inf values in the Y-plane 
        ' with and without gravity transform.  Probably my fault but just fix it here.
        If task.cameraName = "StereoLabs ZED 2/2i" Then
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

        task.depthMask = task.pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        task.noDepthMask = Not task.depthMask

        If task.xRange <> task.xRangeDefault Or task.yRange <> task.yRangeDefault Then
            Dim xRatio = task.xRangeDefault / task.xRange
            Dim yRatio = task.yRangeDefault / task.yRange
            task.pcSplit(0) *= xRatio
            task.pcSplit(1) *= yRatio

            cv.Cv2.Merge(task.pcSplit, task.pointCloud)
        End If
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT Or task.optionsChanged Or task.frameCount < 5 Then
            dst2 = task.pointCloud.Clone
            'dst0 = task.depthMask.Clone
        End If
        If task.cameraName = "StereoLabs ZED 2/2i" Then
            originalPointcloud = checkNanInf(task.pointCloud).Clone
        Else
            originalPointcloud = task.pointCloud.Clone ' save the original camera pointcloud.
        End If
        If task.algorithmPrep = False Then Exit Sub ' this is a 'task' algorithm - run every frame.

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

        task.pointCloud.CopyTo(dst2, task.motionMask)
        task.pointCloud = dst2
        ' dst0.CopyTo(task.depthMask, task.motionMask)

        preparePointcloud()

        If standaloneTest() Then
            dst3 = originalPointcloud.Clone

            Static diff As New Diff_Depth32f
            Dim split = dst3.Split()
            diff.lastDepth32f = split(2)
            diff.Run(task.pcSplit(2))
            dst1 = diff.dst2
        End If
    End Sub
End Class
