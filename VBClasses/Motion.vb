Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Motion_Basics : Inherits TaskParent
        Public motionList As New List(Of Integer)
        Dim diff As New Diff_Basics
        Public motionMask As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 255)
        Public Sub New()
            If standalone Then taskA.gOptions.showMotionMask.Checked = True
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "The motion mask"
            desc = "Find all the grid rects that had motion since the last frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = taskA.gray
            If taskA.optionsChanged Then dst2 = src.Clone

            diff.lastFrame = dst2
            diff.Run(src)

            motionList.Clear()
            For i = 0 To taskA.gridRects.Count - 1
                Dim diffCount = diff.dst2(taskA.gridRects(i)).CountNonZero
                If diffCount >= taskA.motionThreshold Then
                    For Each index In taskA.grid.gridNeighbors(i)
                        If motionList.Contains(index) = False Then motionList.Add(index)
                    Next
                End If
            Next

            motionMask.SetTo(0)
            dst3.SetTo(0)
            For Each index In motionList
                Dim rect = taskA.gridRects(index)
                src(rect).CopyTo(dst2(rect))
                dst3(rect).SetTo(255)
                motionMask(rect).SetTo(255)
            Next

            labels(2) = "Grid rects with motion: " + CStr(motionList.Count)
        End Sub
    End Class





    Public Class NR_Motion_Validate : Inherits TaskParent
        Dim diff As New Diff_Basics
        Public Sub New()
            If standalone Then taskA.gOptions.showMotionMask.Checked = True
            If standalone Then taskA.gOptions.displayDst1.Checked = True
            labels(1) = "Current grayscale image"
            labels(2) = "Grayscale image constructed from previous images + motion updates."
            labels(3) = "Highlighted difference of taskA.gray and the one built with the motion data.  "
            desc = "Compare taskA.gray to constructed images to verify Motion_Basics is working"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then dst1 = taskA.gray.Clone Else dst1 = src.Clone
            dst2 = taskA.motionRGB.dst2.Clone()

            diff.lastFrame = dst2
            diff.Run(dst1)
            dst3 = diff.dst3.Threshold(taskA.motionThreshold, 255, cv.ThresholdTypes.Binary)

            SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(taskA.motionThreshold) +
                    " pixels different: " + CStr(taskA.motionRGB.motionList.Count), 3)
        End Sub
    End Class






    Public Class NR_Motion_ValidateRight : Inherits TaskParent
        Dim diff As New Diff_Basics
        Dim motionRight As New Motion_Right
        Public Sub New()
            If standalone Then taskA.gOptions.displayDst1.Checked = True
            labels(1) = "Current right image"
            labels(2) = "Right image constructed from previous images + motion updates."
            labels(3) = "Highlighted difference of taskA.rightView and the one built with the motion data."
            desc = "Validate that the right image motion mask (Motion_RightImage) is working properly."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motionRight.Run(emptyMat)

            dst1 = taskA.rightView.Clone
            dst2 = motionRight.motion.dst2.Clone()

            diff.lastFrame = dst2
            diff.Run(dst1)
            dst3 = diff.dst3.Threshold(taskA.motionThreshold, 255, cv.ThresholdTypes.Binary)

            SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(taskA.motionThreshold) +
                    " pixels different: " + CStr(motionRight.motion.motionList.Count), 3)

            For Each index In motionRight.motion.motionList
                dst1.Rectangle(taskA.gridRects(index), 255, taskA.lineWidth)
            Next
        End Sub
    End Class





    Public Class Motion_LeftTest : Inherits TaskParent
        Dim motionLeft As New Motion_Left
        Public Sub New()
            dst0 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            If standalone Then taskA.gOptions.displayDst0.Checked = True
            If standalone Then taskA.gOptions.displayDst1.Checked = True
            taskA.featureOptions.ColorDiffSlider.Value = 6
            desc = "Emphasize differences between the accumulated left view and the left view."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskA.firstPass Then dst3 = taskA.leftView

            motionLeft.Run(Nothing)
            dst2 = motionLeft.dst3
            dst3 = motionLeft.motion.dst2

            labels(2) = CStr(motionLeft.motion.motionList.Count) + " bricks were copied to dst3"

            cv.Cv2.Absdiff(dst3, taskA.leftView, dst1)
            dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

            dst0 = dst3 - taskA.leftView
            dst0 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)
        End Sub
    End Class





    Public Class Motion_PointCloud : Inherits TaskParent
        Public originalPointcloud As cv.Mat
        Public Sub New()
            labels(1) = "The difference between the latest pointcloud and the motion-adjusted point cloud."
            labels(2) = "Point cloud after updating with the motion mask changes."
            labels(3) = "taskA.pointcloud for the current frame."
            desc = "Point cloud after updating with the motion mask"
        End Sub
        Public Shared Function checkNanInf(pc As cv.Mat) As cv.Mat
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

            Return pc
        End Function
        Public Sub preparePointcloud()
            If taskA.gOptions.gravityPointCloud.Checked Then
                '******* this is the gravity rotation *******
                taskA.gravityCloud = (taskA.pointCloud.Reshape(1,
                            taskA.rows * taskA.cols) * taskA.gMatrix).ToMat.Reshape(3, taskA.rows)
                taskA.pointCloud = taskA.gravityCloud
            End If

            ' The stereolabs camera has some weird -inf and inf values in the Y-plane 
            ' with and without gravity transform.  Probably my fault but just fix it here.
            If taskA.Settings.cameraName = "StereoLabs ZED 2/2i" Then
                taskA.pointCloud = checkNanInf(taskA.pointCloud)
            End If

            taskA.pcSplit = taskA.pointCloud.Split

            If taskA.optionsChanged Then
                taskA.maxDepthMask = New cv.Mat(taskA.pcSplit(2).Size, cv.MatType.CV_8U, 0)
            End If
            If taskA.gOptions.TruncateDepth.Checked Then
                taskA.pcSplit(2) = taskA.pcSplit(2).Threshold(taskA.MaxZmeters,
                                                        taskA.MaxZmeters, cv.ThresholdTypes.Trunc)
                taskA.maxDepthMask = taskA.pcSplit(2).InRange(taskA.MaxZmeters,
                                                        taskA.MaxZmeters).ConvertScaleAbs()
                cv.Cv2.Merge(taskA.pcSplit, taskA.pointCloud)
            End If

            taskA.depthmask = taskA.pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            taskA.noDepthMask = Not taskA.depthmask

            If taskA.xRange <> taskA.xRangeDefault Or taskA.yRange <> taskA.yRangeDefault Then
                Dim xRatio = taskA.xRangeDefault / taskA.xRange
                Dim yRatio = taskA.yRangeDefault / taskA.yRange
                taskA.pcSplit(0) *= xRatio
                taskA.pcSplit(1) *= yRatio

                cv.Cv2.Merge(taskA.pcSplit, taskA.pointCloud)
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskA.heartBeatLT Or taskA.optionsChanged Or taskA.frameCount < 5 Then
                dst2 = taskA.pointCloud.Clone
            End If
            If taskA.Settings.cameraName = "StereoLabs ZED 2/2i" Then
                originalPointcloud = checkNanInf(taskA.pointCloud).Clone
            Else
                originalPointcloud = taskA.pointCloud.Clone ' save the original camera pointcloud.
            End If

            If taskA.optionsChanged Then
                If taskA.rangesCloud Is Nothing Then
                    Dim rx = New cv.Vec2f(-taskA.xRangeDefault, taskA.xRangeDefault)
                    Dim ry = New cv.Vec2f(-taskA.yRangeDefault, taskA.yRangeDefault)
                    Dim rz = New cv.Vec2f(0, taskA.MaxZmeters)
                    taskA.rangesCloud = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1),
                                                    New cv.Rangef(ry.Item0, ry.Item1),
                                                    New cv.Rangef(rz.Item0, rz.Item1)}
                End If
            End If

            taskA.pointCloud.CopyTo(dst2, taskA.motionRGB.motionMask)
            taskA.pointCloud = dst2

            preparePointcloud()

            If standaloneTest() Then
                dst3 = originalPointcloud.Clone

                Static diff As New Diff_Depth32f
                Dim split = dst3.Split()
                diff.lastDepth32f = split(2)
                diff.Run(taskA.pcSplit(2))
            End If
        End Sub
    End Class





    Public Class Motion_Right : Inherits TaskParent
        Public motion As New Motion_Basics
        Public Sub New()
            desc = "Build the MotionMask for the right camera."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motion.Run(taskA.rightView)
            dst2 = motion.dst2
            dst3 =  motion.motionMask.Clone
            labels(2) = motion.labels(2)
            labels(3) = "The motion mask for the right image"
        End Sub
    End Class




    Public Class Motion_Left : Inherits TaskParent
        Public motion As New Motion_Basics
        Public Sub New()
            desc = "Build the MotionMask for the left camera."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motion.Run(taskA.leftView)
            dst2 = motion.dst2
            dst3 = motion.motionMask.Clone
            labels(2) = motion.labels(2)
            labels(3) = "The motion mask for the left image "
        End Sub
    End Class
End Namespace