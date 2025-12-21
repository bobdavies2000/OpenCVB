Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Motion_Basics : Inherits TaskParent
        Public motionList As New List(Of Integer)
        Dim diff As New Diff_Basics
        Public Sub New()
            If standalone Then taskAlg.gOptions.showMotionMask.Checked = True
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "The motion mask"
            desc = "Find all the grid rects that had motion since the last frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = taskAlg.gray
            If taskAlg.heartBeat Or taskAlg.optionsChanged Then dst2 = src.Clone

            diff.lastFrame = dst2
            diff.Run(src)

            motionList.Clear()
            For i = 0 To taskAlg.gridRects.Count - 1
                Dim diffCount = diff.dst2(taskAlg.gridRects(i)).CountNonZero
                If diffCount >= taskAlg.motionThreshold Then
                    For Each index In taskAlg.grid.gridNeighbors(i)
                        If motionList.Contains(index) = False Then motionList.Add(index)
                    Next
                End If
            Next

            dst3.SetTo(0)
            taskAlg.motionRect = If(motionList.Count > 0, taskAlg.gridRects(motionList(0)), New cv.Rect)
            For Each index In motionList
                Dim rect = taskAlg.gridRects(index)
                taskAlg.motionRect = taskAlg.motionRect.Union(rect)
                src(rect).CopyTo(dst2(rect))
                dst3(rect).SetTo(255)
            Next

            taskAlg.motionMask = dst3.Clone
            labels(2) = "Grid rects with motion: " + CStr(motionList.Count)
        End Sub
    End Class








    Public Class Motion_PointCloud : Inherits TaskParent
        Public originalPointcloud As cv.Mat
        Public Sub New()
            labels(1) = "The difference between the latest pointcloud and the motion-adjusted point cloud."
            labels(2) = "Point cloud after updating with the motion mask changes."
            labels(3) = "taskAlg.pointcloud for the current frame."
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
            If taskAlg.gOptions.gravityPointCloud.Checked Then
                '******* this is the gravity rotation *******
                taskAlg.gravityCloud = (taskAlg.pointCloud.Reshape(1,
                            taskAlg.rows * taskAlg.cols) * taskAlg.gMatrix).ToMat.Reshape(3, taskAlg.rows)
                taskAlg.pointCloud = taskAlg.gravityCloud
            End If

            ' The stereolabs camera has some weird -inf and inf values in the Y-plane 
            ' with and without gravity transform.  Probably my fault but just fix it here.
            If taskAlg.settings.cameraName = "StereoLabs ZED 2/2i" Then
                taskAlg.pointCloud = checkNanInf(taskAlg.pointCloud)
            End If

            taskAlg.pcSplit = taskAlg.pointCloud.Split

            If taskAlg.optionsChanged Then
                taskAlg.maxDepthMask = New cv.Mat(taskAlg.pcSplit(2).Size, cv.MatType.CV_8U, 0)
            End If
            If taskAlg.gOptions.TruncateDepth.Checked Then
                taskAlg.pcSplit(2) = taskAlg.pcSplit(2).Threshold(taskAlg.MaxZmeters,
                                                        taskAlg.MaxZmeters, cv.ThresholdTypes.Trunc)
                taskAlg.maxDepthMask = taskAlg.pcSplit(2).InRange(taskAlg.MaxZmeters,
                                                        taskAlg.MaxZmeters).ConvertScaleAbs()
                cv.Cv2.Merge(taskAlg.pcSplit, taskAlg.pointCloud)
            End If

            taskAlg.depthMask = taskAlg.pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            taskAlg.noDepthMask = Not taskAlg.depthMask

            If taskAlg.xRange <> taskAlg.xRangeDefault Or taskAlg.yRange <> taskAlg.yRangeDefault Then
                Dim xRatio = taskAlg.xRangeDefault / taskAlg.xRange
                Dim yRatio = taskAlg.yRangeDefault / taskAlg.yRange
                taskAlg.pcSplit(0) *= xRatio
                taskAlg.pcSplit(1) *= yRatio

                cv.Cv2.Merge(taskAlg.pcSplit, taskAlg.pointCloud)
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskAlg.heartBeatLT Or taskAlg.optionsChanged Or taskAlg.frameCount < 5 Then
                dst2 = taskAlg.pointCloud.Clone
                'dst0 = taskAlg.depthMask.Clone
            End If
            If taskAlg.settings.cameraName = "StereoLabs ZED 2/2i" Then
                originalPointcloud = checkNanInf(taskAlg.pointCloud).Clone
            Else
                originalPointcloud = taskAlg.pointCloud.Clone ' save the original camera pointcloud.
            End If
            If taskAlg.algorithmPrep = False Then Exit Sub ' this is a 'taskAlg' algorithm - run every frame.

            If taskAlg.optionsChanged Then
                If taskAlg.rangesCloud Is Nothing Then
                    Dim rx = New cv.Vec2f(-taskAlg.xRangeDefault, taskAlg.xRangeDefault)
                    Dim ry = New cv.Vec2f(-taskAlg.yRangeDefault, taskAlg.yRangeDefault)
                    Dim rz = New cv.Vec2f(0, taskAlg.MaxZmeters)
                    taskAlg.rangesCloud = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1),
                                                    New cv.Rangef(ry.Item0, ry.Item1),
                                                    New cv.Rangef(rz.Item0, rz.Item1)}
                End If
            End If

            taskAlg.pointCloud.CopyTo(dst2, taskAlg.motionMask)
            taskAlg.pointCloud = dst2
            ' dst0.CopyTo(taskAlg.depthMask, taskAlg.motionMask)

            preparePointcloud()

            If standaloneTest() Then
                dst3 = originalPointcloud.Clone

                Static diff As New Diff_Depth32f
                Dim split = dst3.Split()
                diff.lastDepth32f = split(2)
                diff.Run(taskAlg.pcSplit(2))
            End If
        End Sub
    End Class






    Public Class Motion_Validate : Inherits TaskParent
        Dim diff As New Diff_Basics
        Public Sub New()
            If standalone Then taskAlg.gOptions.showMotionMask.Checked = True
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels(1) = "Current grayscale image"
            labels(2) = "Grayscale image constructed from previous images + motion updates."
            labels(3) = "Highlighted difference of taskAlg.gray and the one built with the motion data.  "
            desc = "Compare taskAlg.gray to constructed images to verify Motion_Basics is working"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then dst1 = taskAlg.gray.Clone Else dst1 = src.Clone
            dst2 = taskAlg.motionBasics.dst2.Clone()

            diff.lastFrame = dst2
            diff.Run(dst1)
            dst3 = diff.dst3.Threshold(taskAlg.motionThreshold, 255, cv.ThresholdTypes.Binary)

            SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(taskAlg.motionThreshold) +
                    " pixels different: " + CStr(taskAlg.motionBasics.motionList.Count), 3)
        End Sub
    End Class






    Public Class Motion_ValidateRight : Inherits TaskParent
        Dim diff As New Diff_Basics
        Dim motionRight As New Motion_RightImage
        Public Sub New()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            labels(1) = "Current right image"
            labels(2) = "Right image constructed from previous images + motion updates."
            labels(3) = "Highlighted difference of taskAlg.rightView and the one built with the motion data."
            desc = "Validate that the right image motion mask (Motion_RightImage) is working properly."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motionRight.Run(emptyMat)

            dst1 = taskAlg.rightView.Clone
            dst2 = motionRight.motion.dst2.Clone()

            diff.lastFrame = dst2
            diff.Run(dst1)
            dst3 = diff.dst3.Threshold(taskAlg.motionThreshold, 255, cv.ThresholdTypes.Binary)

            SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(taskAlg.motionThreshold) +
                    " pixels different: " + CStr(motionRight.motion.motionList.Count), 3)

            For Each index In motionRight.motion.motionList
                dst1.Rectangle(taskAlg.gridRects(index), 255, taskAlg.lineWidth)
            Next
        End Sub
    End Class




    Public Class Motion_RightImage : Inherits TaskParent
        Public motion As New Motion_Basics
        Public Sub New()
            desc = "Build the MotionMask for the right camera and validate it."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motion.Run(taskAlg.rightView)
            dst1 = motion.dst1
            dst2 = motion.dst2
            dst3 = motion.dst3

            taskAlg.motionMaskRight = dst3.Clone
            labels(2) = motion.labels(2)
            labels(3) = "The motion mask for the right image - MotionMaskRight"
        End Sub
    End Class

End Namespace