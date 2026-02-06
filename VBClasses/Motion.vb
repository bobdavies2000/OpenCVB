Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Motion_Basics : Inherits TaskParent
        Public motionList As New List(Of Integer)
        Dim diff As New Diff_Basics
        Public motionMask As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 255)
        Public Sub New()
            If standalone Then atask.gOptions.showMotionMask.Checked = True
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "The motion mask"
            desc = "Find all the grid rects that had motion since the last frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = atask.gray
            If atask.optionsChanged Then dst2 = src.Clone

            diff.lastFrame = dst2
            diff.Run(src)

            motionList.Clear()
            For i = 0 To atask.gridRects.Count - 1
                Dim diffCount = diff.dst2(atask.gridRects(i)).CountNonZero
                If diffCount >= atask.motionThreshold Then
                    For Each index In atask.grid.gridNeighbors(i)
                        If motionList.Contains(index) = False Then motionList.Add(index)
                    Next
                End If
            Next

            motionMask.SetTo(0)
            dst3.SetTo(0)
            For Each index In motionList
                Dim rect = atask.gridRects(index)
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
            If standalone Then atask.gOptions.showMotionMask.Checked = True
            If standalone Then atask.gOptions.displayDst1.Checked = True
            labels(1) = "Current grayscale image"
            labels(2) = "Grayscale image constructed from previous images + motion updates."
            labels(3) = "Highlighted difference of atask.gray and the one built with the motion data.  "
            desc = "Compare atask.gray to constructed images to verify Motion_Basics is working"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then dst1 = atask.gray.Clone Else dst1 = src.Clone
            dst2 = atask.motionRGB.dst2.Clone()

            diff.lastFrame = dst2
            diff.Run(dst1)
            dst3 = diff.dst3.Threshold(atask.motionThreshold, 255, cv.ThresholdTypes.Binary)

            SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(atask.motionThreshold) +
                    " pixels different: " + CStr(atask.motionRGB.motionList.Count), 3)
        End Sub
    End Class






    Public Class NR_Motion_ValidateRight : Inherits TaskParent
        Dim diff As New Diff_Basics
        Dim motionRight As New Motion_Right
        Public Sub New()
            If standalone Then atask.gOptions.displayDst1.Checked = True
            labels(1) = "Current right image"
            labels(2) = "Right image constructed from previous images + motion updates."
            labels(3) = "Highlighted difference of atask.rightView and the one built with the motion data."
            desc = "Validate that the right image motion mask (Motion_RightImage) is working properly."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motionRight.Run(emptyMat)

            dst1 = atask.rightView.Clone
            dst2 = motionRight.motion.dst2.Clone()

            diff.lastFrame = dst2
            diff.Run(dst1)
            dst3 = diff.dst3.Threshold(atask.motionThreshold, 255, cv.ThresholdTypes.Binary)

            SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(atask.motionThreshold) +
                    " pixels different: " + CStr(motionRight.motion.motionList.Count), 3)

            For Each index In motionRight.motion.motionList
                dst1.Rectangle(atask.gridRects(index), 255, atask.lineWidth)
            Next
        End Sub
    End Class





    Public Class Motion_LeftTest : Inherits TaskParent
        Dim motionLeft As New Motion_Left
        Public Sub New()
            dst0 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            If standalone Then atask.gOptions.displayDst0.Checked = True
            If standalone Then atask.gOptions.displayDst1.Checked = True
            atask.featureOptions.ColorDiffSlider.Value = 6
            desc = "Emphasize differences between the accumulated left view and the left view."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If atask.firstPass Then dst3 = atask.leftView

            motionLeft.Run(Nothing)
            dst2 = motionLeft.dst3
            dst3 = motionLeft.motion.dst2

            labels(2) = CStr(motionLeft.motion.motionList.Count) + " bricks were copied to dst3"

            cv.Cv2.Absdiff(dst3, atask.leftView, dst1)
            dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

            dst0 = dst3 - atask.leftView
            dst0 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)
        End Sub
    End Class





    Public Class Motion_PointCloud : Inherits TaskParent
        Public originalPointcloud As cv.Mat
        Public Sub New()
            labels(1) = "The difference between the latest pointcloud and the motion-adjusted point cloud."
            labels(2) = "Point cloud after updating with the motion mask changes."
            labels(3) = "atask.pointcloud for the current frame."
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
            If atask.gOptions.gravityPointCloud.Checked Then
                '******* this is the gravity rotation *******
                atask.gravityCloud = (atask.pointCloud.Reshape(1,
                            atask.rows * atask.cols) * atask.gMatrix).ToMat.Reshape(3, atask.rows)
                atask.pointCloud = atask.gravityCloud
            End If

            ' The stereolabs camera has some weird -inf and inf values in the Y-plane 
            ' with and without gravity transform.  Probably my fault but just fix it here.
            If atask.Settings.cameraName = "StereoLabs ZED 2/2i" Then
                atask.pointCloud = checkNanInf(atask.pointCloud)
            End If

            atask.pcSplit = atask.pointCloud.Split

            If atask.optionsChanged Then
                atask.maxDepthMask = New cv.Mat(atask.pcSplit(2).Size, cv.MatType.CV_8U, 0)
            End If
            If atask.gOptions.TruncateDepth.Checked Then
                atask.pcSplit(2) = atask.pcSplit(2).Threshold(atask.MaxZmeters,
                                                        atask.MaxZmeters, cv.ThresholdTypes.Trunc)
                atask.maxDepthMask = atask.pcSplit(2).InRange(atask.MaxZmeters,
                                                        atask.MaxZmeters).ConvertScaleAbs()
                cv.Cv2.Merge(atask.pcSplit, atask.pointCloud)
            End If

            atask.depthmask = atask.pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            atask.noDepthMask = Not atask.depthmask

            If atask.xRange <> atask.xRangeDefault Or atask.yRange <> atask.yRangeDefault Then
                Dim xRatio = atask.xRangeDefault / atask.xRange
                Dim yRatio = atask.yRangeDefault / atask.yRange
                atask.pcSplit(0) *= xRatio
                atask.pcSplit(1) *= yRatio

                cv.Cv2.Merge(atask.pcSplit, atask.pointCloud)
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If atask.heartBeatLT Or atask.optionsChanged Or atask.frameCount < 5 Then
                dst2 = atask.pointCloud.Clone
            End If
            If atask.Settings.cameraName = "StereoLabs ZED 2/2i" Then
                originalPointcloud = checkNanInf(atask.pointCloud).Clone
            Else
                originalPointcloud = atask.pointCloud.Clone ' save the original camera pointcloud.
            End If

            If atask.optionsChanged Then
                If atask.rangesCloud Is Nothing Then
                    Dim rx = New cv.Vec2f(-atask.xRangeDefault, atask.xRangeDefault)
                    Dim ry = New cv.Vec2f(-atask.yRangeDefault, atask.yRangeDefault)
                    Dim rz = New cv.Vec2f(0, atask.MaxZmeters)
                    atask.rangesCloud = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1),
                                                    New cv.Rangef(ry.Item0, ry.Item1),
                                                    New cv.Rangef(rz.Item0, rz.Item1)}
                End If
            End If

            atask.pointCloud.CopyTo(dst2, atask.motionRGB.motionMask)
            atask.pointCloud = dst2

            preparePointcloud()

            If standaloneTest() Then
                dst3 = originalPointcloud.Clone

                Static diff As New Diff_Depth32f
                Dim split = dst3.Split()
                diff.lastDepth32f = split(2)
                diff.Run(atask.pcSplit(2))
            End If
        End Sub
    End Class





    Public Class Motion_Right : Inherits TaskParent
        Public motion As New Motion_Basics
        Public Sub New()
            desc = "Build the MotionMask for the right camera."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motion.Run(atask.rightView)
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
            motion.Run(atask.leftView)
            dst2 = motion.dst2
            dst3 = motion.motionMask.Clone
            labels(2) = motion.labels(2)
            labels(3) = "The motion mask for the left image "
        End Sub
    End Class
End Namespace