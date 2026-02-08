Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Motion_Basics : Inherits TaskParent
        Public motionList As New List(Of Integer)
        Dim diff As New Diff_Basics
        Public motionMask As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 255)
        Public Sub New()
            If standalone Then tsk.gOptions.showMotionMask.Checked = True
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "The motion mask"
            desc = "Find all the grid rects that had motion since the last frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = tsk.gray
            If tsk.optionsChanged Then dst2 = src.Clone

            diff.lastFrame = dst2
            diff.Run(src)

            motionList.Clear()
            For i = 0 To tsk.gridRects.Count - 1
                Dim diffCount = diff.dst2(tsk.gridRects(i)).CountNonZero
                If diffCount >= tsk.motionThreshold Then
                    For Each index In tsk.grid.gridNeighbors(i)
                        If motionList.Contains(index) = False Then motionList.Add(index)
                    Next
                End If
            Next

            motionMask.SetTo(0)
            dst3.SetTo(0)
            For Each index In motionList
                Dim rect = tsk.gridRects(index)
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
            If standalone Then tsk.gOptions.showMotionMask.Checked = True
            If standalone Then tsk.gOptions.displayDst1.Checked = True
            labels(1) = "Current grayscale image"
            labels(2) = "Grayscale image constructed from previous images + motion updates."
            labels(3) = "Highlighted difference of tsk.gray and the one built with the motion data.  "
            desc = "Compare tsk.gray to constructed images to verify Motion_Basics is working"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then dst1 = tsk.gray.Clone Else dst1 = src.Clone
            dst2 = tsk.motionRGB.dst2.Clone()

            diff.lastFrame = dst2
            diff.Run(dst1)
            dst3 = diff.dst3.Threshold(tsk.motionThreshold, 255, cv.ThresholdTypes.Binary)

            SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(tsk.motionThreshold) +
                    " pixels different: " + CStr(tsk.motionRGB.motionList.Count), 3)
        End Sub
    End Class






    Public Class NR_Motion_ValidateRight : Inherits TaskParent
        Dim diff As New Diff_Basics
        Dim motionRight As New Motion_Right
        Public Sub New()
            If standalone Then tsk.gOptions.displayDst1.Checked = True
            labels(1) = "Current right image"
            labels(2) = "Right image constructed from previous images + motion updates."
            labels(3) = "Highlighted difference of tsk.rightView and the one built with the motion data."
            desc = "Validate that the right image motion mask (Motion_RightImage) is working properly."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motionRight.Run(emptyMat)

            dst1 = tsk.rightView.Clone
            dst2 = motionRight.motion.dst2.Clone()

            diff.lastFrame = dst2
            diff.Run(dst1)
            dst3 = diff.dst3.Threshold(tsk.motionThreshold, 255, cv.ThresholdTypes.Binary)

            SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(tsk.motionThreshold) +
                    " pixels different: " + CStr(motionRight.motion.motionList.Count), 3)

            For Each index In motionRight.motion.motionList
                dst1.Rectangle(tsk.gridRects(index), 255, tsk.lineWidth)
            Next
        End Sub
    End Class





    Public Class Motion_LeftTest : Inherits TaskParent
        Dim motionLeft As New Motion_Left
        Public Sub New()
            dst0 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            If standalone Then tsk.gOptions.displayDst0.Checked = True
            If standalone Then tsk.gOptions.displayDst1.Checked = True
            tsk.featureOptions.ColorDiffSlider.Value = 6
            desc = "Emphasize differences between the accumulated left view and the left view."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If tsk.firstPass Then dst3 = tsk.leftView

            motionLeft.Run(Nothing)
            dst2 = motionLeft.dst3
            dst3 = motionLeft.motion.dst2

            labels(2) = CStr(motionLeft.motion.motionList.Count) + " bricks were copied to dst3"

            cv.Cv2.Absdiff(dst3, tsk.leftView, dst1)
            dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

            dst0 = dst3 - tsk.leftView
            dst0 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)
        End Sub
    End Class





    Public Class Motion_PointCloud : Inherits TaskParent
        Public originalPointcloud As cv.Mat
        Public Sub New()
            labels(1) = "The difference between the latest pointcloud and the motion-adjusted point cloud."
            labels(2) = "Point cloud after updating with the motion mask changes."
            labels(3) = "tsk.pointcloud for the current frame."
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
        Private Sub preparePointcloud()
            If tsk.gOptions.gravityPointCloud.Checked Then
                '******* this is the gravity rotation (" * tsk.gMatrix") *******
                tsk.gravityCloud = (tsk.pointCloud.Reshape(1,
                                    tsk.rows * tsk.cols) * tsk.gMatrix).ToMat.Reshape(3, tsk.rows)
                tsk.pointCloud = tsk.gravityCloud
            End If

            ' The stereolabs camera has some weird -inf and inf values in the Y-plane 
            ' with and without gravity transform.  Probably my fault but just fix it here.
            If tsk.Settings.cameraName = "StereoLabs ZED 2/2i" Then
                tsk.pointCloud = checkNanInf(tsk.pointCloud)
            End If

            tsk.pcSplit = tsk.pointCloud.Split

            If tsk.optionsChanged Then
                tsk.maxDepthMask = New cv.Mat(tsk.pcSplit(2).Size, cv.MatType.CV_8U, 0)
            End If

            If tsk.gOptions.TruncateDepth.Checked Then
                tsk.pcSplit(2) = tsk.pcSplit(2).Threshold(tsk.MaxZmeters,
                                                          tsk.MaxZmeters, cv.ThresholdTypes.Trunc)
                tsk.maxDepthMask = tsk.pcSplit(2).InRange(tsk.MaxZmeters,
                                                          tsk.MaxZmeters).ConvertScaleAbs()
                cv.Cv2.Merge(tsk.pcSplit, tsk.pointCloud)
            End If

            tsk.depthmask = tsk.pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            tsk.noDepthMask = Not tsk.depthmask

            If tsk.xRange <> tsk.xRangeDefault Or tsk.yRange <> tsk.yRangeDefault Then
                Dim xRatio = tsk.xRangeDefault / tsk.xRange
                Dim yRatio = tsk.yRangeDefault / tsk.yRange
                tsk.pcSplit(0) *= xRatio
                tsk.pcSplit(1) *= yRatio

                cv.Cv2.Merge(tsk.pcSplit, tsk.pointCloud)
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If tsk.heartBeatLT Or tsk.optionsChanged Or tsk.frameCount < 5 Then
                dst2 = tsk.pointCloud.Clone
            End If
            If tsk.Settings.cameraName = "StereoLabs ZED 2/2i" Then
                originalPointcloud = checkNanInf(tsk.pointCloud).Clone
            Else
                originalPointcloud = tsk.pointCloud.Clone ' save the original camera pointcloud.
            End If

            If tsk.optionsChanged Then
                If tsk.rangesCloud Is Nothing Then
                    Dim rx = New cv.Vec2f(-tsk.xRangeDefault, tsk.xRangeDefault)
                    Dim ry = New cv.Vec2f(-tsk.yRangeDefault, tsk.yRangeDefault)
                    Dim rz = New cv.Vec2f(0, tsk.MaxZmeters)
                    tsk.rangesCloud = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1),
                                                       New cv.Rangef(ry.Item0, ry.Item1),
                                                       New cv.Rangef(rz.Item0, rz.Item1)}
                End If
            End If

            tsk.pointCloud.CopyTo(dst2, tsk.motionRGB.motionMask)
            tsk.pointCloud = dst2

            preparePointcloud()

            If standaloneTest() Then
                dst3 = originalPointcloud.Clone

                Static diff As New Diff_Depth32f
                Dim split = dst3.Split()
                diff.lastDepth32f = split(2)
                diff.Run(tsk.pcSplit(2))
            End If
        End Sub
    End Class





    Public Class Motion_Right : Inherits TaskParent
        Public motion As New Motion_Basics
        Public Sub New()
            desc = "Build the MotionMask for the right camera."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motion.Run(tsk.rightView)
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
            motion.Run(tsk.leftView)
            dst2 = motion.dst2
            dst3 = motion.motionMask.Clone
            labels(2) = motion.labels(2)
            labels(3) = "The motion mask for the left image "
        End Sub
    End Class
End Namespace