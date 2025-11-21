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
        If src.Channels <> 1 Then src = task.gray
        If task.heartBeat Or task.optionsChanged Then dst2 = src.Clone

        diff.lastFrame = dst2
        diff.Run(src)

        motionList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim diffCount = diff.dst2(task.gridRects(i)).CountNonZero
            If diffCount >= task.motionThreshold Then
                For Each index In task.grid.gridNeighbors(i)
                    If motionList.Contains(index) = False Then motionList.Add(index)
                Next
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






Public Class Motion_Validate : Inherits TaskParent
    Dim diff As New Diff_Basics
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "Current grayscale image"
        labels(2) = "Grayscale image constructed from previous images + motion updates."
        labels(3) = "Highlighted difference of task.gray and the one built with the motion data.  "
        desc = "Compare task.gray to constructed images to verify Motion_Basics is working"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then dst1 = task.gray.Clone Else dst1 = src.Clone
        dst2 = task.motionBasics.dst2.Clone()

        diff.lastFrame = dst2
        diff.Run(dst1)
        dst3 = diff.dst3.Threshold(task.motionThreshold, 255, cv.ThresholdTypes.Binary)

        SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(task.motionThreshold) +
                    " pixels different: " + CStr(task.motionBasics.motionList.Count), 3)
    End Sub
End Class






Public Class Motion_ValidateRight : Inherits TaskParent
    Dim diff As New Diff_Basics
    Dim motionRight As New Motion_RightImage
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
                    " pixels different: " + CStr(motionRight.motion.motionList.Count), 3)

        For Each index In motionRight.motion.motionList
            dst1.Rectangle(task.gridRects(index), 255, task.lineWidth)
        Next
    End Sub
End Class




Public Class Motion_RightImage : Inherits TaskParent
    Public motion As New Motion_Basics
    Public Sub New()
        desc = "Build the MotionMask for the right camera and validate it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        motion.Run(task.rightView)
        dst1 = motion.dst1
        dst2 = motion.dst2
        dst3 = motion.dst3

        task.motionMaskRight = dst3.Clone
        labels(2) = motion.labels(2)
        labels(3) = "The motion mask for the right image - MotionMaskRight"
    End Sub
End Class
