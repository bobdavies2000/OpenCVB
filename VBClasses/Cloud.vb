Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Cloud_Basics : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public Shared ppx = task.calibData.leftIntrinsics.ppx
    Public Shared ppy = task.calibData.leftIntrinsics.ppy
    Public Shared fx = task.calibData.leftIntrinsics.fx
    Public Shared fy = task.calibData.leftIntrinsics.fy
    Public Sub New()
        dst2 = New Mat(dst2.Size, MatType.CV_32FC3, 0)
        labels = {"", "", "Recomputed cv.Point cloud from the depth image data", "PointCloud from camera."}
        desc = "Convert depth values to a cv.Point cloud - just proving that the formulas work."
    End Sub
    Public Shared Function worldCoordinates(p As Point3f) As Point3f
        Dim x = (p.X - ppx) / fx
        Dim y = (p.Y - ppy) / fy
        Return New Point3f(x * p.Z, y * p.Z, p.Z)
    End Function
    Public Shared Function worldCoordinates(p As Vec3f) As Vec3f
        Dim x = (p(0) - ppx) / fx
        Dim y = (p(1) - ppy) / fy
        Return New Point3f(x * p(2), y * p(2), p(2))
    End Function
    Public Shared Function worldCoordinates(p As cv.Point, depth As Single) As Point3f
        Dim x = (p.X - ppx) / fx
        Dim y = (p.Y - ppy) / fy
        Return New Point3f(x * depth, y * depth, depth)
    End Function
    Public Shared Function worldCoordinates(_x As Integer, _y As Integer, depth As Single) As Point3f
        Dim x = (_x - ppx) / fx
        Dim y = (_y - ppy) / fy
        Return New Point3f(x * depth, y * depth, depth)
    End Function
    Public Function invert(x As Integer, y As Integer, depth As Single) As Point3f
        Dim u = CInt((x * fx / depth) + ppx)
        Dim v = CInt((y * fy / depth) + ppy)

        If u >= 0 And u < task.workRes.Width And v >= 0 And v < task.workRes.Height Then
            Return New Point3f(u, v, depth)
        End If
        Return New Point3f
    End Function
    Public Shared Function invert(vec As Point3f) As Point3f
        Dim u = CInt((vec.X * fx / vec.Z) + ppx)
        Dim v = CInt((vec.Y * fy / vec.Z) + ppy)

        If u >= 0 And u < task.workRes.Width And v >= 0 And v < task.workRes.Height Then
            Return New Point3f(u, v, vec.Z)
        End If
        Return New Point3f
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        dst2.SetTo(0)
        For y = 0 To dst2.Height - 1
            For x = 0 To dst2.Width - 1
                dst2.Set(Of Point3f)(y, x, worldCoordinates(x, y, task.pcSplit(2).Get(Of Single)(y, x)))
            Next
        Next

        dst3 = task.pointCloud
    End Sub
End Class



Public Class Cloud_DepthToWorld : Inherits TaskParent
    Public Sub New()
        desc = "Update the world coordinates with the new depth for the mask provided."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> MatType.CV_32F Then
            SetTrueText("Input must be CV_32F depth image.", 2)
            Exit Sub
        End If

        dst2 = New Mat(src.Size, MatType.CV_32FC3, 0)
        For y = 0 To src.Height - 1
            For x = 0 To src.Width - 1
                Dim depth = src.Get(Of Single)(y, x)
                If depth = 0 Then Continue For
                dst2.Set(Of Point3f)(y, x, Cloud_Basics.worldCoordinates(x, y, depth))
            Next
        Next
    End Sub
End Class




Public Class XR_Cloud_Inverse : Inherits TaskParent
    Dim colorizer As New DepthColorizer_CPP
    Public Sub New()
        dst2 = New Mat(dst2.Size, MatType.CV_32F, 0)
        desc = "Given a cv.Point cloud element, convert it to a depth image in image coordinates."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> MatType.CV_32FC3 Then src = task.pointCloud
        dst2.SetTo(0)
        For y = 0 To src.Height - 1
            For x = 0 To src.Width - 1
                Dim vec As Point3f = src.Get(Of Point3f)(y, x)
                If Single.IsNaN(vec.X) Or Single.IsNaN(vec.Y) Or Single.IsNaN(vec.Z) Then Continue For
                If Single.IsInfinity(vec.X) Or Single.IsInfinity(vec.Y) Or Single.IsInfinity(vec.Z) Then Continue For
                If vec.Z = 0 Then Continue For
                vec = Cloud_Basics.invert(vec)
                dst2.Set(Of Single)(vec.Y, vec.X, vec.Z)
            Next
        Next

        colorizer.Run(dst2)
        dst3 = colorizer.dst2
    End Sub
End Class






Public Class XR_Cloud_Display : Inherits TaskParent
    Dim pcHistory As New List(Of Mat)
    Public Sub New()
        dst2 = New Mat(dst2.Size(), MatType.CV_32FC3, 0)
        desc = "Average all 3 elements of the cv.Point cloud - not just depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pcHistory.Add(task.pointCloud)
        If pcHistory.Count >= task.fOptions.FrameHistoryCount.Value Then pcHistory.RemoveAt(0)

        dst2.SetTo(0)
        For Each m In pcHistory
            dst2 += m
        Next
        dst2 *= 1 / pcHistory.Count
    End Sub
End Class





' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
Public Class Cloud_SetupSide : Inherits TaskParent
    Public Sub New()
        labels(2) = "Layout markers for side view"
        desc = "Create the colorized mat used for side projections"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim distanceRatio As Single = 1
        If src.Channels() <> 3 Then CvtColor(src, src, ColorConversionCodes.GRAY2BGR)

        If standaloneTest() Then dst2.SetTo(0) Else src.CopyTo(dst2)
        Circle(dst2, task.sideCameraPoint, task.DotSize, Scalar.BlueViolet, -1, task.lineType)
        For i = 1 To task.MaxZmeters
            Dim xmeter = CInt(dst2.Width * i / task.MaxZmeters * distanceRatio)
            Line(dst2, New cv.Point(xmeter, 0), New cv.Point(xmeter, dst2.Height), Scalar.AliceBlue, 1)
            SetTrueText(CStr(i) + "m", New cv.Point(xmeter - src.Width / 24, dst2.Height - 10))
        Next

        Dim cam = task.sideCameraPoint
        Dim marker As New Point2f(dst2.Width / (task.MaxZmeters * distanceRatio), 0)
        marker.Y = marker.X * Math.Tan((task.vFov / 2) * PI / 180)
        Dim markerLeft = New cv.Point(marker.X, cam.Y - marker.Y)
        Dim markerRight = New cv.Point(marker.X, cam.Y + marker.Y)

        Dim offset = Math.Sin(task.accRadians.X) * marker.Y
        If task.gOptions.gravityPointCloud.Checked Then
            If task.accRadians.X > 0 Then
                markerLeft.Y = markerLeft.Y - offset
                markerRight.Y = markerRight.Y + offset
            Else
                markerLeft.Y = markerLeft.Y + offset
                markerRight.Y = markerRight.Y - offset
            End If

            markerLeft = New cv.Point(markerLeft.X - cam.X, markerLeft.Y - cam.Y) ' Change the origin
            markerLeft = New cv.Point(markerLeft.X * Math.Cos(task.accRadians.Z) - markerLeft.Y * Math.Sin(task.accRadians.Z), ' rotate around x-axis using accRadians.Z
                                                  markerLeft.Y * Math.Cos(task.accRadians.Z) + markerLeft.X * Math.Sin(task.accRadians.Z))
            markerLeft = New cv.Point(markerLeft.X + cam.X, markerLeft.Y + cam.Y) ' Move the origin to the side camera location.

            ' Same as above for markerLeft but consolidated algebraically.
            markerRight = New cv.Point((markerRight.X - cam.X) * Math.Cos(task.accRadians.Z) - (markerRight.Y - cam.Y) * Math.Sin(task.accRadians.Z) + cam.X,
                                                   (markerRight.Y - cam.Y) * Math.Cos(task.accRadians.Z) + (markerRight.X - cam.X) * Math.Sin(task.accRadians.Z) + cam.Y)
        End If
        If standaloneTest() = False Then
            Circle(dst2, markerLeft, task.DotSize, Scalar.Red, -1, task.lineType)
            Circle(dst2, markerRight, task.DotSize, Scalar.Red, -1, task.lineType)
        End If

        ' draw the arc enclosing the camera FOV
        Dim startAngle = (180 - task.vFov) / 2
        Dim y = dst2.Width / Math.Tan(startAngle * PI / 180)

        Dim fovTop = New cv.Point(dst2.Width, cam.Y - y)
        Dim fovBot = New cv.Point(dst2.Width, cam.Y + y)

        Line(dst2, cam, fovTop, white, 1, task.lineType)
        Line(dst2, cam, fovBot, white, 1, task.lineType)

        Circle(dst2, markerLeft, task.DotSize + 3, Scalar.Red, -1, task.lineType)
        Circle(dst2, markerRight, task.DotSize + 3, Scalar.Red, -1, task.lineType)
        Line(dst2, cam, markerLeft, Scalar.Red, 1, task.lineType)
        Line(dst2, cam, markerRight, Scalar.Red, 1, task.lineType)

        Dim labelLocation = New cv.Point(src.Width * 0.02, src.Height * 7 / 8)
        SetTrueText("vFOV=" + (180 - startAngle * 2).ToString("0.0") + " deg.", New cv.Point(4, dst2.Height * 3 / 4))
    End Sub
End Class





' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
Public Class Cloud_SetupTop : Inherits TaskParent
    Public Sub New()
        labels(2) = "Layout markers for top view"
        desc = "Create the colorize the mat for a topdown projections"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim distanceRatio As Single = 1
        If src.Channels() <> 3 Then CvtColor(src, src, ColorConversionCodes.GRAY2BGR)

        If standaloneTest() Then dst2.SetTo(0) Else src.CopyTo(dst2)
        Circle(dst2, task.topCameraPoint, task.DotSize, Scalar.BlueViolet, -1, task.lineType)
        For i = 1 To task.MaxZmeters
            Dim ymeter = CInt(dst2.Height - dst2.Height * i / (task.MaxZmeters * distanceRatio))
            Line(dst2, New cv.Point(0, ymeter), New cv.Point(dst2.Width, ymeter), Scalar.AliceBlue, 1)
            SetTrueText(CStr(i) + "m", New cv.Point(10, ymeter))
        Next

        Dim cam = task.topCameraPoint
        Dim marker As New Point2f(cam.X, dst2.Height / task.MaxZmeters)
        Dim topLen = marker.Y * Math.Tan((task.hFov / 2) * PI / 180)
        Dim sideLen = marker.Y * Math.Tan((task.vFov / 2) * PI / 180)
        Dim markerLeft = New cv.Point(cam.X - topLen, marker.Y)
        Dim markerRight = New cv.Point(cam.X + topLen, marker.Y)

        Dim offset = Math.Sin(task.accRadians.Z) * topLen
        If task.gOptions.gravityPointCloud.Checked Then
            If task.accRadians.Z > 0 Then
                markerLeft.X = markerLeft.X - offset
                markerRight.X = markerRight.X + offset
            Else
                markerLeft.X = markerLeft.X + offset
                markerRight.X = markerRight.X - offset
            End If
        End If

        ' draw the arc enclosing the camera FOV
        Dim startAngle = (180 - task.hFov) / 2
        Dim x = dst2.Height / Math.Tan(startAngle * PI / 180)

        Dim fovRight = New cv.Point(task.topCameraPoint.X + x, 0)
        Dim fovLeft = New cv.Point(task.topCameraPoint.X - x, fovRight.Y)

        Line(dst2, task.topCameraPoint, fovLeft, white, 1, task.lineType)

        Circle(dst2, markerLeft, task.DotSize + 3, Scalar.Red, -1, task.lineType)
        Circle(dst2, markerRight, task.DotSize + 3, Scalar.Red, -1, task.lineType)
        Line(dst2, cam, markerLeft, Scalar.Red, 1, task.lineType)
        Line(dst2, cam, markerRight, Scalar.Red, 1, task.lineType)

        Dim shift = (src.Width - src.Height) / 2
        Dim labelLocation = New cv.Point(dst2.Width / 2 + shift, dst2.Height * 15 / 16)
        SetTrueText("hFOV=" + (180 - startAngle * 2).ToString("0.0") + " deg.", New cv.Point(4, dst2.Height * 7 / 8))
        Line(dst2, task.topCameraPoint, fovRight, white, task.lineWidth, task.lineWidth)
    End Sub
End Class





Public Class Cloud_Solo : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Public Sub New()
        OptionParent.FindCheckBox("Top View (Unchecked Side View)").Checked = True
        labels(2) = "Top down view after inrange sampling"
        labels(3) = "Histogram after filtering For Single-only histogram bins"
        desc = "Find floor And ceiling Using gravity aligned top-down view And selecting bins With exactly 1 sample"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        heat.Run(src)
        InRange(heat.dst0, task.fOptions.FrameHistoryCount.Value, task.fOptions.FrameHistoryCount.Value, dst2)
        ConvertScaleAbs(dst2, dst2)
        InRange(heat.dst1, task.fOptions.FrameHistoryCount.Value, task.fOptions.FrameHistoryCount.Value, dst3)
        ConvertScaleAbs(dst3, dst3)
    End Sub
End Class






Public Class XR_Cloud_SoloRegions : Inherits TaskParent
    Public solo As New Cloud_Solo
    Dim dilate As New Dilate_Basics
    Public Sub New()
        labels(2) = "Top down view before inrange sampling"
        labels(3) = "Histogram after filtering For Single-only histogram bins"
        desc = "Find floor And ceiling Using gravity aligned top-down view And selecting bins With exactly 1 sample"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        solo.Run(src)
        dst2 = solo.dst2
        dst3 = solo.dst3
        dilate.Run(dst3.Clone)
        dst3 = dilate.dst2
    End Sub
End Class





Public Class XR_Cloud_SurfaceH_CPP : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Public plot As New PlotOpenCV_CPP
    Public topRow As Integer
    Public botRow As Integer
    Public peakRow As Integer
    Public Sub New()
        desc = "Find the horizontal surfaces With a projects Of the SideView histogram."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        heat.Run(src)
        dst2 = heat.dst3

        topRow = 0
        botRow = 0
        peakRow = 0
        Dim peakVal As Integer
        For i = 0 To dst2.Height - 1
            plot.srcX.Add(i)
            If dst2.Channels() = 1 Then
                plot.srcY.Add(CountNonZero(dst2.Row(i)))
            Else
                Dim _cvt1 As New Mat
                CvtColor(dst2.Row(i), _cvt1, ColorConversionCodes.BGR2GRAY)
                plot.srcY.Add(CountNonZero(_cvt1))
            End If
            If peakVal < plot.srcY(i) Then
                peakVal = plot.srcY(i)
                peakRow = i
            End If
            If topRow = 0 And plot.srcY(i) > 10 Then topRow = i
        Next

        For i = plot.srcY.Count - 1 To 0 Step -1
            If botRow = 0 And plot.srcY(i) > 10 Then botRow = i
        Next
        plot.Run(src)
        Transpose(plot.dst2, dst3)
        Flip(dst3, dst3, FlipMode.Y)
        labels(2) = "Top row = " + CStr(topRow) + " peak row = " + CStr(peakRow) + " bottom row = " + CStr(botRow)
    End Sub
End Class




Public Class Cloud_SurfaceH : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Public plotHist As New PlotBar_Basics
    Public topRow As Integer
    Public botRow As Integer
    Public peakRow As Integer
    Public Sub New()
        OptionParent.FindCheckBox("Top View (Unchecked Side View)").Checked = True
        labels(3) = "Histogram Of Each Of " + CStr(task.histogramBins) + " bins aligned With the sideview"
        desc = "Find the horizontal surfaces With a projects Of the SideView histogram."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        heat.Run(src)
        dst2 = heat.dst2
        Dim hist = New Mat(dst2.Height, 1, MatType.CV_32F, Scalar.All(0))

        topRow = 0
        botRow = 0
        peakRow = 0
        Dim peakVal As Integer
        If dst2.Channels() <> 1 Then CvtColor(dst2, dst1, ColorConversionCodes.BGR2GRAY)
        For i = 0 To dst1.Height - 1
            hist.At(Of Single)(i, 0) = CountNonZero(dst1.Row(i))
            If peakVal < hist.At(Of Single)(i, 0) Then
                peakVal = CInt(hist.At(Of Single)(i, 0))
                peakRow = i
            End If
            If topRow = 0 And hist.At(Of Single)(i, 0) > 10 Then topRow = i
        Next

        plotHist.maxRange = (Math.Floor(peakVal / 100) + 1) * 100
        For i = hist.Rows - 1 To 0 Step -1
            If botRow = 0 And hist.At(Of Single)(i, 0) > 10 Then botRow = i
        Next
        plotHist.Run(hist)
        Transpose(plotHist.dst2, dst3)
        Flip(dst3, dst3, FlipMode.Y)
        Resize(dst3, dst3, dst0.Size)
        labels(2) = "Top row = " + CStr(topRow) + " peak row = " + CStr(peakRow) + " bottom row = " + CStr(botRow)

        Dim ratio = task.mouseMovePoint.Y / dst2.Height
        Dim offset = ratio * dst3.Height
        Line(dst2, New cv.Point(0, task.mouseMovePoint.Y), New cv.Point(dst2.Width, task.mouseMovePoint.Y), yellow, task.lineWidth, task.lineWidth)
        Line(dst3, New cv.Point(0, offset), New cv.Point(dst3.Width, offset), Scalar.Yellow, task.lineWidth)
    End Sub
End Class




Public Class XR_Cloud_GridInspector : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public Sub New()
        dst2 = New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
        task.mouseMovePoint.X = dst2.Width / 2
        desc = "Inspect x, y, and z values by brick"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)

        Dim cLine = task.mouseMovePoint.X

        Dim input = src
        If input.Type <> MatType.CV_32F Then input = task.pcSplit(2)

        Dim topPt = New Point2f(cLine, 0)
        Dim botPt = New Point2f(cLine, dst2.Height)
        dst2 = task.depthRGB
        Line(dst2, topPt, botPt, 255, task.lineWidth, task.lineWidth)

        SetTrueText("Values show r.pt3d values at the blue line.", New cv.Point(dst2.Width / 2, 0), 3)
        For i = 0 To dst2.Height - 1 Step task.gridWH
            Dim pt = New Point2f(cLine, i)
            Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            Dim center = bricks.brickList(index).center
            Dim xyz = task.pointCloud.Get(Of Vec3f)(center.Y, center.X)
            SetTrueText("Row " + i.ToString("00") + vbTab + vbTab + xyz(0).ToString(fmt2) + vbTab +
                                     xyz(1).ToString(fmt2) + vbTab + xyz(2).ToString(fmt2), New cv.Point(5, pt.Y), 3)
        Next
        labels(2) = "Values displayed are the cv.Point cloud X, Y, and Z values for column " + CStr(cLine)
        labels(3) = "Move mouse in the image at left to see the cv.Point cloud X, Y, and Z values."
    End Sub
End Class





Public Class XR_Cloud_FrustrumTop : Inherits TaskParent
    Dim frustrum As New Draw_Frustrum
    Dim heat As New HeatMap_Basics
    Dim setupTop As New Cloud_SetupTop
    Public Sub New()
        task.gOptions.setGravityUsage(False)
        OptionParent.FindCheckBox("Top View (Unchecked Side View)").Checked = True
        labels(3) = "Draw the frustrum from the top view"
        desc = "Draw the top view of the frustrum"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        frustrum.Run(src)

        Resize(frustrum.dst3, dst2, dst2.Size)
        heat.Run(dst2)

        setupTop.Run(heat.dst2)
        dst2 = setupTop.dst2
    End Sub
End Class





Public Class XR_Cloud_FrustrumSide : Inherits TaskParent
    Dim frustrum As New Draw_Frustrum
    Dim heat As New HeatMap_Basics
    Dim setupSide As New Cloud_SetupSide
    Public Sub New()
        task.gOptions.setGravityUsage(False)
        OptionParent.FindCheckBox("Top View (Unchecked Side View)").Checked = False
        labels(2) = "Draw the frustrum from the side view"
        desc = "Draw the side view of the frustrum"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        frustrum.Run(src)
        Resize(frustrum.dst3, dst2, dst2.Size)
        heat.Run(dst2)

        setupSide.Run(heat.dst3)
        dst2 = setupSide.dst2
    End Sub
End Class





Public Class Cloud_ReduceSplit2 : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Reduce the task.pcSplit(2) for use in several algorithms."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.pcSplit(2) * 1000
        dst2.ConvertTo(dst2, MatType.CV_32S)
        reduction.Run(dst2)
        reduction.dst2.ConvertTo(dst1, MatType.CV_32F)
        dst1 *= 0.001
        If standaloneTest() Then
            dst3 = task.pointCloud
        Else
            Dim mm = GetMinMax(dst1)
            dst1 *= task.MaxZmeters / mm.maxVal
            Merge({task.pcSplit(0), task.pcSplit(1), dst1}, dst3)
        End If
    End Sub
End Class




Public Class XR_Cloud_ReducedTopView : Inherits TaskParent
    Dim split2 As New Cloud_ReduceSplit2
    Public Sub New()
        desc = "Create a stable side view of the cv.Point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        split2.Run(task.pointCloud)

        CalcHist({split2.dst3}, task.channelsTop, New Mat, dst1, 2, task.bins2D, task.rangesTop)

        Flip(dst1, dst1, FlipMode.X)
        Threshold(dst1, dst1, 0, 255, ThresholdTypes.Binary)
        dst1.ConvertTo(dst2, MatType.CV_8UC1)
    End Sub
End Class




Public Class XR_Cloud_ReducedSideView : Inherits TaskParent
    Dim split2 As New Cloud_ReduceSplit2
    Public Sub New()
        desc = "Show where vertical neighbor depth values are within X mm's"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        split2.Run(Nothing)

        CalcHist({split2.dst3}, task.channelsSide, New Mat, dst1, 2, task.bins2D, task.rangesSide)

        Threshold(dst1, dst1, 0, 255, ThresholdTypes.Binary)
        Threshold(dst1, dst1, 0, 255, ThresholdTypes.Binary)
        dst1.ConvertTo(dst2, MatType.CV_8UC1)
    End Sub
End Class





Public Class XR_Cloud_ReducedViews : Inherits TaskParent
    Dim split2 As New Cloud_ReduceSplit2
    Public Sub New()
        labels = {"", "", "Reduced side view", "Reduced top view"}
        desc = "Show where vertical neighbor depth values are within X mm's"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        split2.Run(Nothing)

        CalcHist({split2.dst3}, task.channelsSide, New Mat, dst1, 2, task.bins2D, task.rangesSide)

        Threshold(dst1, dst1, 0, 255, ThresholdTypes.Binary)
        dst1.ConvertTo(dst2, MatType.CV_8UC1)

        CalcHist({split2.dst3}, task.channelsTop, New Mat, dst1, 2, task.bins2D, task.rangesTop)

        Flip(dst1, dst1, FlipMode.X)
        Threshold(dst1, dst1, 0, 255, ThresholdTypes.Binary)
        dst1.ConvertTo(dst3, MatType.CV_8UC1)
    End Sub
End Class





Public Class XR_Cloud_Split : Inherits TaskParent
    Public Sub New()
        desc = "Attempting to debug pointcloud problem - display the 3 components"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.pcSplit(0)
        dst2 = task.pcSplit(1)
        dst3 = task.pcSplit(2)

        Dim mmx = GetMinMax(dst1)
        Dim mmy = GetMinMax(dst2)
        Dim mmz = GetMinMax(dst3)

        labels(1) = "Min/Max for X " + mmx.minVal.ToString(fmt1) + " / " + mmx.maxVal.ToString(fmt1)
        labels(2) = "Min/Max for Y " + mmy.minVal.ToString(fmt1) + " / " + mmy.maxVal.ToString(fmt1)
        labels(3) = "Min/Max for Z " + mmz.minVal.ToString(fmt1) + " / " + mmz.maxVal.ToString(fmt1)
    End Sub
End Class





Public Class XR_Cloud_Continuous_VB : Inherits TaskParent
    Dim options As New Options_Features
    Public Sub New()
        dst2 = New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
        dst3 = New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
        desc = "Show where the pointcloud is continuous"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim input = src
        If input.Type <> MatType.CV_32F Then input = task.pcSplit(2)

        dst2.SetTo(0)
        dst3.SetTo(0)
        For y = 0 To input.Height - 1
            For x = 1 To input.Width - 1
                Dim p1 = input.Get(Of Single)(y, x - 1)
                Dim p2 = input.Get(Of Single)(y, x)
                If Math.Abs(p1 - p2) <= task.depthDiffMeters Then dst2.Set(Of Byte)(y, x, 255) Else dst3.Set(Of Byte)(y, x, 255)
            Next
        Next

        dst3.SetTo(0, task.noDepthMask)
        dst2.SetTo(0, task.noDepthMask)
        labels(2) = "White pixels: Z-values within " + CStr(task.depthDiffMeters) + " meters of X neighbor"
        labels(3) = "Mask showing discontinuities > " + CStr(task.depthDiffMeters) + " meters of X neighbor"
    End Sub
End Class






Public Class XR_Cloud_Continuous_GridX : Inherits TaskParent
    Dim options As New Options_Features
    Dim bricks As New Brick_Basics
    Public Sub New()
        dst2 = New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
        dst3 = New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
        desc = "Show where the pointcloud is continuous at the grid square resolution"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        bricks.Run(src)

        Dim input = src
        If input.Type <> MatType.CV_32F Then input = task.pcSplit(2)

        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim brickPrev = bricks.brickList(0)
        For Each brick In bricks.brickList
            If brick.rect.X > 0 Then
                If Math.Abs(brick.depth - brickPrev.depth) <= task.depthDiffMeters Then
                    dst2(brick.rect).SetTo(255)
                Else
                    dst3(brick.rect).SetTo(255)
                End If
            End If
            brickPrev = brick
        Next

        labels(2) = "White pixels: Z-values within " + CStr(task.depthDiffMeters) + " meters of neighbor in X direction"
        labels(3) = "Mask showing discontinuities > " + CStr(task.depthDiffMeters) + " meters of neighbor in X direction"
    End Sub
End Class






Public Class XR_Cloud_Continuous_GridXY : Inherits TaskParent
    Dim options As New Options_Features
    Dim bricks As New Brick_Basics
    Public Sub New()
        dst2 = New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
        desc = "Show where the pointcloud is continuous at the grid square resolution"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        bricks.Run(src)

        Dim input = src
        If input.Type <> MatType.CV_32F Then input = task.pcSplit(2)

        dst2.SetTo(0)
        Dim brickPrev = bricks.brickList(0)
        Dim cellMat As New Mat(task.gridWH, task.gridWH, MatType.CV_8U, Scalar.All(127))
        For Each brick In bricks.brickList
            Dim brickAbove = bricks.brickList(CInt(brick.index Mod task.bricksPerRow))
            If brick.correlation > task.fCorrThreshold Then
                If brick.rect.Y = 0 Or brick.rect.X = 0 Then Continue For
                If Math.Abs(brick.depth - brickPrev.depth) <= task.depthDiffMeters Then dst2(brick.rect).SetTo(128)
                If Math.Abs(brick.depth - brickAbove.depth) <= task.depthDiffMeters And
                    brick.rect.Width = cellMat.Width And brick.rect.Height = cellMat.Height Then
                    Add(dst2(brick.rect), cellMat, dst2(brick.rect))
                End If
            End If
            brickPrev = brick
        Next

        labels(2) = "White pixels: Z-values within " + CStr(task.depthDiffMeters) + " meters of neighbor in X and Y direction"
    End Sub
End Class






Public Class XR_Cloud_Templates : Inherits TaskParent
    Public templateX As New Mat, templateY As New Mat
    Dim contours As New Contour_Basics
    Public Sub New()
        templateX = New Mat(task.workRes, MatType.CV_32F)
        templateY = New Mat(task.workRes, MatType.CV_32F)
        For i = 0 To templateX.Width - 1
            templateX.Set(Of Single)(0, i, i)
        Next

        For i = 1 To templateX.Height - 1
            templateX.Row(0).CopyTo(templateX.Row(i))
            templateY.Set(Of Single)(i, 0, i)
        Next

        For i = 1 To templateY.Width - 1
            templateY.Col(0).CopyTo(templateY.Col(i))
        Next
        templateX -= Scalar.All(task.calibData.leftIntrinsics.ppx)
        templateY -= Scalar.All(task.calibData.leftIntrinsics.ppy)

        desc = "Prepare for injecting depth into the cv.Point cloud."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contours.Run(src)
        If standalone Then
            src = New Mat(dst1.Size, MatType.CV_32F, 0)
            For Each contour In contours.contourList
                If contour.depth = 0 Then Continue For
                contour.depth = Mean(task.pcSplit(2)(contour.rect), contour.mask)
                src(contour.rect).SetTo(contour.depth, contour.mask)
            Next
        End If
        Dim fxTemplate = task.calibData.leftIntrinsics.fx
        Dim fyTemplate = task.calibData.leftIntrinsics.fy
        Dim worldX As New Mat, worldY As New Mat

        Multiply(templateX, src, worldX)
        worldX *= Scalar.All(1 / fxTemplate)

        Multiply(templateY, src, worldY)
        worldY *= Scalar.All(1 / fyTemplate)

        Merge({worldX, worldY, src}, dst2)
    End Sub
End Class





Public Class Cloud_Gravity_TA : Inherits TaskParent
    Public originalPointcloud As Mat
    Public Sub New()
        desc = "Rebuild the cv.Point cloud with knowledge of gravity (if the option is requested.)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        originalPointcloud = task.pointCloud.Clone
        InRange(originalPointcloud, task.MaxZmeters, 10000, task.maxDepthMask)
        ConvertScaleAbs(task.maxDepthMask, task.maxDepthMask)

        If task.optionsChanged Then
            If task.rangesCloud Is Nothing Then
                Dim rx = New Vec2f(-task.xRangeDefault, task.xRangeDefault)
                Dim ry = New Vec2f(-task.yRangeDefault, task.yRangeDefault)
                Dim rz = New Vec2f(0, task.MaxZmeters)
                task.rangesCloud = New Rangef() {New Rangef(rx.Item0, rx.Item1),
                                                    New Rangef(ry.Item0, ry.Item1),
                                                    New Rangef(rz.Item0, rz.Item1)}
            End If
        End If

        If task.gOptions.gravityPointCloud.Checked Then
            '******* this is the gravity rotation (" * task.gMatrix") *******
            task.gravityCloud = (task.pointCloud.Reshape(1,
                                        task.rows * task.cols) * task.gMatrix).ToMat.Reshape(3, task.rows)
            task.pointCloud = task.gravityCloud
        End If

        task.pcSplit = Split(task.pointCloud)

        If task.Settings.cameraName.StartsWith("StereoLabs") Then
            For i = 0 To task.pcSplit.Count - 1
                Cv2.PatchNaNs(task.pcSplit(i), 0)

                Dim posInfMask As New Mat()
                Cv2.Compare(task.pcSplit(i), Double.PositiveInfinity, posInfMask, CmpTypes.EQ)

                Dim negInfMask As New Mat()
                Cv2.Compare(task.pcSplit(i), Double.NegativeInfinity, negInfMask, CmpTypes.EQ)

                task.pcSplit(i).SetTo(0, posInfMask)
                task.pcSplit(i).SetTo(0, negInfMask)
            Next

            Merge(task.pcSplit, task.pointCloud)
        End If

        Threshold(task.pcSplit(2), task.depthmask, 0, 255, ThresholdTypes.Binary)
        ConvertScaleAbs(task.depthmask, task.depthmask)
        task.noDepthMask = Not task.depthmask

        If task.xRange <> task.xRangeDefault Or task.yRange <> task.yRangeDefault Then
            Dim xRatio = task.xRangeDefault / task.xRange
            Dim yRatio = task.yRangeDefault / task.yRange
            task.pcSplit(0) *= xRatio
            task.pcSplit(1) *= yRatio

            Merge(task.pcSplit, task.pointCloud)
        End If
    End Sub
End Class





Public Class Cloud_Consistency : Inherits TaskParent
    Public pointcloud As New Mat(dst2.Size, MatType.CV_32FC3, 0)
    Public pcSplit(2) As Mat
    Public Sub New()
        desc = "A cloud pixel is only real if it is present in 2 consecutive frames."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lastDepth As Mat = task.pcSplit(2).Clone
        Dim lastMask As New Mat
        Threshold(lastDepth, lastMask, 0.001, 255, ThresholdTypes.Binary)
        ConvertScaleAbs(lastMask, lastMask)

        Dim mask = lastMask And task.depthmask
        task.pointCloud.CopyTo(pointcloud, mask)
        pcSplit = Split(pointcloud)

        dst2 = pointcloud

        lastDepth = pcSplit(2).Clone
    End Sub
End Class

