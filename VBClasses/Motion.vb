Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Forms.Design.AxImporter
Imports OpenCvSharp
Imports OpenCvSharp.ML.DTrees
Imports cv = OpenCvSharp
Public Class Motion_Basics : Inherits TaskParent
    Public mGrid As New Motion_Core
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        labels(3) = "Updated task.motionRect"
        desc = "Use the motionlist of rects to create one motion rectangle."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        mGrid.Run(src)

        If task.heartBeat Then dst2 = task.gray
        If mGrid.motionList.Count = 0 Then
            task.motionRect = New cv.Rect
            Exit Sub
        End If

        task.motionRect = task.gridRects(mGrid.motionList(0))
        For Each index In mGrid.motionList
            task.motionRect = task.motionRect.Union(task.gridRects(index))
        Next

        dst3.SetTo(0)
        dst3(task.motionRect).SetTo(255)
        task.gray(task.motionRect).CopyTo(dst2(task.motionRect))

        labels(2) = CStr(mGrid.motionList.Count) + " grid rect's or " +
                    Format(mGrid.motionList.Count / task.gridRects.Count, "0.0%") +
                    " of bricks had motion."
    End Sub
End Class





Public Class Motion_Core : Inherits TaskParent
    Public motionList As New List(Of Integer)
    Dim diff As New Diff_Basics
    Dim options As New Options_Motion
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "The motion mask"
        desc = "Find all the grid rects that had motion since the last frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.gOptions.UseMotionMask.Checked = False Then Exit Sub

        options.Run()

        If src.Channels <> 1 Then src = task.gray
        If task.heartBeat Or task.optionsChanged Then dst2 = src.Clone

        diff.Run(src)

        motionList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim diffCount = diff.dst2(task.gridRects(i)).CountNonZero
            If diffCount >= options.colorDiffPixels Then
                For Each index In task.grid.gridNeighbors(i)
                    If motionList.Contains(index) = False Then motionList.Add(index)
                Next
            End If
        Next

        dst3.SetTo(0)
        For Each index In motionList
            Dim rect = task.gridRects(index)
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
                If roi = task.gridRects(606) Then Dim k = 0
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






Public Class Motion_HistoryTest : Inherits TaskParent
    Dim diff As New Diff_Basics
    Dim frames As New History_Basics
    Public Sub New()
        OptionParent.FindSlider("Color Difference Threshold").Value = 10
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Detect motion using the last X images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        diff.Run(src)
        dst1 = diff.dst2.Threshold(0, 1, cv.ThresholdTypes.Binary)
        frames.Run(dst1)

        dst2 = frames.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels(2) = "Cumulative diff for the last " + CStr(task.frameHistoryCount) + " frames"
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






Public Class Motion_FromEdgeColorize : Inherits TaskParent
    Dim cAccum As New Edge_CannyAccum
    Public Sub New()
        labels = {"", "", "Canny edges accumulated", "Colorized version of dst2 - blue indicates motion."}
        desc = "Colorize the output of Edge_CannyAccum to show values off the peak value which indicate motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        cAccum.Run(src)
        dst2 = cAccum.dst2
        dst3 = PaletteFull(dst2)
    End Sub
End Class







Public Class Motion_ValidateBasics : Inherits TaskParent
    Dim diff As New Diff_Basics
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(2) = "Grayscale image constructed from previous image + motion rect of current image."
        labels(1) = "Current grayscale image"
        desc = "Compare task.gray to constructed image to verify Motion_Basics is working"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.motionBasics.dst2.Clone
        dst2 = task.gray.Clone

        diff.lastFrame = dst1
        diff.Run(dst2)
        dst3 = diff.dst3

        dst3.Rectangle(task.motionRect, white, task.lineWidth)
        labels(3) = "Diff of task.gray and the one built with the motion Rect.  " +
                     CStr(diff.dst2.CountNonZero) + " pixels different."
    End Sub
End Class





Public Class Motion_ValidateCore : Inherits TaskParent
    Dim diff As New Diff_Basics
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(2) = "Grayscale image constructed from previous image + motion rect of current image."
        labels(1) = "Current grayscale image"
        desc = "Compare task.gray to constructed image to verify Motion_Core is working"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.motionBasics.dst2.Clone
        dst2 = task.gray.Clone

        diff.lastFrame = dst1
        diff.Run(dst2)
        dst3 = diff.dst3

        dst3.Rectangle(task.motionRect, white, task.lineWidth)
        labels(3) = "Diff of task.gray and the one built with the motion Rect.  " +
                     CStr(diff.dst2.CountNonZero) + " pixels different."
    End Sub
End Class





Public Class Motion_PointCloud : Inherits TaskParent
    Public originalPointcloud As cv.Mat
    Public Sub New()
        labels = {"", "", "Pointcloud updated only with motion Rect",
                  "Diff of camera depth and motion-updated depth (always different)"}
        desc = "Update the pointcloud only with the motion Rect.  Resync heartbeatLT."
    End Sub
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

            ' these don't work...Could this be a VB.Net failing?
            ' cv.Cv2.PatchNaNs(task.pointCloud, 0.0) ' not working!
            ' Dim mask As New cv.Mat
            ' cv.Cv2.Compare(task.pointCloud, task.pointCloud, mask, cv.CmpType.EQ)

            Dim vec As New cv.Vec3f, count As Integer
            For y = 0 To task.pointCloud.Rows - 1
                For x = 0 To task.pointCloud.Cols - 1
                    Dim val = task.pointCloud.Get(Of cv.Vec3f)(y, x)
                    If Single.IsNaN(val(0)) Then
                        task.pointCloud.Set(Of cv.Vec3f)(y, x, vec)
                        count += 1
                    End If
                Next
            Next

            'Dim mean As cv.Scalar, stdev As cv.Scalar
            'cv.Cv2.MeanStdDev(task.pointCloud, mean, stdev)
            'Debug.WriteLine("Before Motion mean " + mean.ToString() + " " + CStr(count) + " inf's removed.")

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
        If task.algorithmPrep = False Then Exit Sub ' this is a 'task' algorithm - run every frame.

        originalPointcloud = task.pointCloud.Clone ' save the original camera pointcloud.

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

        If task.gOptions.UseMotionMask.Checked Then
            If task.heartBeatLT Or task.frameCount < 5 Or task.optionsChanged Then
                dst2 = task.pointCloud.Clone
            End If

            If task.motionRect.Width = 0 And task.optionsChanged = False Then Exit Sub ' nothing changed...
            task.pointCloud(task.motionRect).CopyTo(dst2(task.motionRect))
            task.pointCloud = dst2
        End If

        ' this will move the motion-updated pointcloud into production.
        preparePointcloud()

        If standaloneTest() Then
            Static diff As New Diff_Depth32f
            Dim split = originalPointcloud.Split()
            diff.lastDepth32f = split(2)
            diff.Run(task.pcSplit(2))
            dst3 = diff.dst2
            dst3.Rectangle(task.motionRect, white, task.lineWidth)
        End If
    End Sub
End Class