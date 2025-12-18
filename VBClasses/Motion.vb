Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Motion_Basics : Inherits TaskParent
        Public motionList As New List(Of Integer)
        Dim diff As New Diff_Basics
        Public Sub New()
            If standalone Then algTask.gOptions.showMotionMask.Checked = True
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "The motion mask"
            desc = "Find all the grid rects that had motion since the last frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = algTask.gray
            If algTask.heartBeat Or algTask.optionsChanged Then dst2 = src.Clone

            diff.lastFrame = dst2
            diff.Run(src)

            motionList.Clear()
            For i = 0 To algTask.gridRects.Count - 1
                Dim diffCount = diff.dst2(algTask.gridRects(i)).CountNonZero
                If diffCount >= algTask.motionThreshold Then
                    For Each index In algTask.grid.gridNeighbors(i)
                        If motionList.Contains(index) = False Then motionList.Add(index)
                    Next
                End If
            Next

            dst3.SetTo(0)
            algTask.motionRect = If(motionList.Count > 0, algTask.gridRects(motionList(0)), New cv.Rect)
            For Each index In motionList
                Dim rect = algTask.gridRects(index)
                algTask.motionRect = algTask.motionRect.Union(rect)
                src(rect).CopyTo(dst2(rect))
                dst3(rect).SetTo(255)
            Next

            algTask.motionMask = dst3.Clone
            labels(2) = CStr(motionList.Count) + " grid rects had motion."
        End Sub
    End Class








    Public Class Motion_PointCloud : Inherits TaskParent
        Public originalPointcloud As cv.Mat
        Public Sub New()
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            labels(1) = "The difference between the latest pointcloud and the motion-adjusted point cloud."
            labels(2) = "Point cloud after updating with the motion mask changes."
            labels(3) = "algTask.pointcloud for the current frame."
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
            If algTask.gOptions.gravityPointCloud.Checked Then
                '******* this is the gravity rotation *******
                algTask.gravityCloud = (algTask.pointCloud.Reshape(1,
                            algTask.rows * algTask.cols) * algTask.gMatrix).ToMat.Reshape(3, algTask.rows)
                algTask.pointCloud = algTask.gravityCloud
            End If

            ' The stereolabs camera has some weird -inf and inf values in the Y-plane 
            ' with and without gravity transform.  Probably my fault but just fix it here.
            If algTask.settings.cameraName = "StereoLabs ZED 2/2i" Then
                algTask.pointCloud = checkNanInf(algTask.pointCloud)
            End If

            algTask.pcSplit = algTask.pointCloud.Split

            If algTask.optionsChanged Then
                algTask.maxDepthMask = New cv.Mat(algTask.pcSplit(2).Size, cv.MatType.CV_8U, 0)
            End If
            If algTask.gOptions.TruncateDepth.Checked Then
                algTask.pcSplit(2) = algTask.pcSplit(2).Threshold(algTask.MaxZmeters,
                                                        algTask.MaxZmeters, cv.ThresholdTypes.Trunc)
                algTask.maxDepthMask = algTask.pcSplit(2).InRange(algTask.MaxZmeters,
                                                        algTask.MaxZmeters).ConvertScaleAbs()
                cv.Cv2.Merge(algTask.pcSplit, algTask.pointCloud)
            End If

            algTask.depthMask = algTask.pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            algTask.noDepthMask = Not algTask.depthMask

            If algTask.xRange <> algTask.xRangeDefault Or algTask.yRange <> algTask.yRangeDefault Then
                Dim xRatio = algTask.xRangeDefault / algTask.xRange
                Dim yRatio = algTask.yRangeDefault / algTask.yRange
                algTask.pcSplit(0) *= xRatio
                algTask.pcSplit(1) *= yRatio

                cv.Cv2.Merge(algTask.pcSplit, algTask.pointCloud)
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If algTask.heartBeatLT Or algTask.optionsChanged Or algTask.frameCount < 5 Then
                dst2 = algTask.pointCloud.Clone
                'dst0 = algTask.depthMask.Clone
            End If
            If algTask.settings.cameraName = "StereoLabs ZED 2/2i" Then
                originalPointcloud = checkNanInf(algTask.pointCloud).Clone
            Else
                originalPointcloud = algTask.pointCloud.Clone ' save the original camera pointcloud.
            End If
            If algTask.algorithmPrep = False Then Exit Sub ' this is a 'algTask' algorithm - run every frame.

            If algTask.optionsChanged Then
                If algTask.rangesCloud Is Nothing Then
                    Dim rx = New cv.Vec2f(-algTask.xRangeDefault, algTask.xRangeDefault)
                    Dim ry = New cv.Vec2f(-algTask.yRangeDefault, algTask.yRangeDefault)
                    Dim rz = New cv.Vec2f(0, algTask.MaxZmeters)
                    algTask.rangesCloud = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1),
                                                    New cv.Rangef(ry.Item0, ry.Item1),
                                                    New cv.Rangef(rz.Item0, rz.Item1)}
                End If
            End If

            algTask.pointCloud.CopyTo(dst2, algTask.motionMask)
            algTask.pointCloud = dst2
            ' dst0.CopyTo(algTask.depthMask, algTask.motionMask)

            preparePointcloud()

            If standaloneTest() Then
                dst3 = originalPointcloud.Clone

                Static diff As New Diff_Depth32f
                Dim split = dst3.Split()
                diff.lastDepth32f = split(2)
                diff.Run(algTask.pcSplit(2))
                dst1 = diff.dst2
            End If
        End Sub
    End Class






    Public Class Motion_Validate : Inherits TaskParent
        Dim diff As New Diff_Basics
        Public Sub New()
            If standalone Then algTask.gOptions.showMotionMask.Checked = True
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            labels(1) = "Current grayscale image"
            labels(2) = "Grayscale image constructed from previous images + motion updates."
            labels(3) = "Highlighted difference of algTask.gray and the one built with the motion data.  "
            desc = "Compare algTask.gray to constructed images to verify Motion_Basics is working"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then dst1 = algTask.gray.Clone Else dst1 = src.Clone
            dst2 = algTask.motionBasics.dst2.Clone()

            diff.lastFrame = dst2
            diff.Run(dst1)
            dst3 = diff.dst3.Threshold(algTask.motionThreshold, 255, cv.ThresholdTypes.Binary)

            SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(algTask.motionThreshold) +
                    " pixels different: " + CStr(algTask.motionBasics.motionList.Count), 3)
        End Sub
    End Class






    Public Class Motion_ValidateRight : Inherits TaskParent
        Dim diff As New Diff_Basics
        Dim motionRight As New Motion_RightImage
        Public Sub New()
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            labels(1) = "Current right image"
            labels(2) = "Right image constructed from previous images + motion updates."
            labels(3) = "Highlighted difference of algTask.rightView and the one built with the motion data."
            desc = "Validate that the right image motion mask (Motion_RightImage) is working properly."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motionRight.Run(emptyMat)

            dst1 = algTask.rightView.Clone
            dst2 = motionRight.motion.dst2.Clone()

            diff.lastFrame = dst2
            diff.Run(dst1)
            dst3 = diff.dst3.Threshold(algTask.motionThreshold, 255, cv.ThresholdTypes.Binary)

            SetTrueText("Pixels different from camera image: " + CStr(diff.dst2.CountNonZero) + vbCrLf +
                    "Grid rects with more than " + CStr(algTask.motionThreshold) +
                    " pixels different: " + CStr(motionRight.motion.motionList.Count), 3)

            For Each index In motionRight.motion.motionList
                dst1.Rectangle(algTask.gridRects(index), 255, algTask.lineWidth)
            Next
        End Sub
    End Class




    Public Class Motion_RightImage : Inherits TaskParent
        Public motion As New Motion_Basics
        Public Sub New()
            desc = "Build the MotionMask for the right camera and validate it."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            motion.Run(algTask.rightView)
            dst1 = motion.dst1
            dst2 = motion.dst2
            dst3 = motion.dst3

            algTask.motionMaskRight = dst3.Clone
            labels(2) = motion.labels(2)
            labels(3) = "The motion mask for the right image - MotionMaskRight"
        End Sub
    End Class

End Namespace