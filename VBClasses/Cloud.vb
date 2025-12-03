Imports System.Security.Cryptography
Imports cv = OpenCvSharp
Public Class Cloud_Basics : Inherits TaskParent
    Public Shared ppx = task.calibData.rgbIntrinsics.ppx
    Public Shared ppy = task.calibData.rgbIntrinsics.ppy
    Public Shared fx = task.calibData.rgbIntrinsics.fx
    Public Shared fy = task.calibData.rgbIntrinsics.fy
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        labels = {"", "", "Recomputed point cloud from the depth image data", "PointCloud from camera."}
        desc = "Convert depth values to a point cloud - just proving that the formulas work."
    End Sub
    Public Shared Function worldCoordinates(p As cv.Point3f) As cv.Point3f
        Dim x = (p.X - ppx) / fx
        Dim y = (p.Y - ppy) / fy
        Return New cv.Point3f(x * p.Z, y * p.Z, p.Z)
    End Function
    Public Shared Function worldCoordinates(p As cv.Vec3f) As cv.Vec3f
        Dim x = (p(0) - ppx) / fx
        Dim y = (p(1) - ppy) / fy
        Return New cv.Point3f(x * p(2), y * p(2), p(2))
    End Function
    Public Shared Function worldCoordinates(p As cv.Point, depth As Single) As cv.Point3f
        Dim x = (p.X - ppx) / fx
        Dim y = (p.Y - ppy) / fy
        Return New cv.Point3f(x * depth, y * depth, depth)
    End Function
    Public Shared Function worldCoordinates(_x As Integer, _y As Integer, depth As Single) As cv.Point3f
        Dim x = (_x - ppx) / fx
        Dim y = (_y - ppy) / fy
        Return New cv.Point3f(x * depth, y * depth, depth)
    End Function
    Public Function invert(x As Integer, y As Integer, depth As Single) As cv.Point3f
        Dim u = CInt((x * fx / depth) + ppx)
        Dim v = CInt((y * fy / depth) + ppy)

        If u >= 0 And u < task.workRes.Width And v >= 0 And v < task.workRes.Height Then
            Return New cv.Point3f(u, v, depth)
        End If
        Return New cv.Point3f
    End Function
    Public Shared Function invert(vec As cv.Point3f) As cv.Point3f
        Dim u = CInt((vec.X * fx / vec.Z) + ppx)
        Dim v = CInt((vec.Y * fy / vec.Z) + ppy)

        If u >= 0 And u < task.workRes.Width And v >= 0 And v < task.workRes.Height Then
            Return New cv.Point3f(u, v, vec.Z)
        End If
        Return New cv.Point3f
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2.SetTo(0)
        For y = 0 To dst2.Height - 1
            For x = 0 To dst2.Width - 1
                dst2.Set(Of cv.Point3f)(y, x, worldCoordinates(x, y, task.pcSplit(2).Get(Of Single)(y, x)))
            Next
        Next

        dst3 = task.pointCloud
    End Sub
End Class






Public Class Cloud_Inverse : Inherits TaskParent
    Dim colorizer As New DepthColorizer_CPP
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        desc = "Given a point cloud element, convert it to a depth image in image coordinates."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        dst2.SetTo(0)
        For y = 0 To src.Height - 1
            For x = 0 To src.Width - 1
                Dim vec As cv.Point3f = src.Get(Of cv.Point3f)(y, x)
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






Public Class Cloud_Display : Inherits TaskParent
    Dim pcHistory As New List(Of cv.Mat)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC3, 0)
        desc = "Average all 3 elements of the point cloud - not just depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pcHistory.Add(task.pointCloud)
        If pcHistory.Count >= task.frameHistoryCount Then pcHistory.RemoveAt(0)

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
    Dim arcSize As Integer
    Public Sub New()
        arcSize = dst2.Width / 15
        labels(2) = "Layout markers for side view"
        desc = "Create the colorized mat used for side projections"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim distanceRatio As Single = 1
        If src.Channels() <> 3 Then src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If standaloneTest() Then dst2.SetTo(0) Else src.CopyTo(dst2)
        DrawCircle(dst2, task.sideCameraPoint, task.DotSize, cv.Scalar.BlueViolet)
        For i = 1 To task.MaxZmeters
            Dim xmeter = CInt(dst2.Width * i / task.MaxZmeters * distanceRatio)
            dst2.Line(New cv.Point(xmeter, 0), New cv.Point(xmeter, dst2.Height), cv.Scalar.AliceBlue, 1)
            SetTrueText(CStr(i) + "m", New cv.Point(xmeter - src.Width / 24, dst2.Height - 10))
        Next

        Dim cam = task.sideCameraPoint
        Dim marker As New cv.Point2f(dst2.Width / (task.MaxZmeters * distanceRatio), 0)
        marker.Y = marker.X * Math.Tan((task.vFov / 2) * cv.Cv2.PI / 180)
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
            DrawCircle(dst2, markerLeft, task.DotSize, cv.Scalar.Red)
            DrawCircle(dst2, markerRight, task.DotSize, cv.Scalar.Red)
        End If

        ' draw the arc enclosing the camera FOV
        Dim startAngle = (180 - task.vFov) / 2
        Dim y = dst2.Width / Math.Tan(startAngle * cv.Cv2.PI / 180)

        Dim fovTop = New cv.Point(dst2.Width, cam.Y - y)
        Dim fovBot = New cv.Point(dst2.Width, cam.Y + y)

        dst2.Line(cam, fovTop, white, 1, task.lineType)
        dst2.Line(cam, fovBot, white, 1, task.lineType)

        DrawCircle(dst2, markerLeft, task.DotSize + 3, cv.Scalar.Red)
        DrawCircle(dst2, markerRight, task.DotSize + 3, cv.Scalar.Red)
        dst2.Line(cam, markerLeft, cv.Scalar.Red, 1, task.lineType)
        dst2.Line(cam, markerRight, cv.Scalar.Red, 1, task.lineType)

        Dim labelLocation = New cv.Point(src.Width * 0.02, src.Height * 7 / 8)
        SetTrueText("vFOV=" + Format(180 - startAngle * 2, "0.0") + " deg.", New cv.Point(4, dst2.Height * 3 / 4))
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
        If src.Channels() <> 3 Then src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If standaloneTest() Then dst2.SetTo(0) Else src.CopyTo(dst2)
        DrawCircle(dst2, task.topCameraPoint, task.DotSize, cv.Scalar.BlueViolet)
        For i = 1 To task.MaxZmeters
            Dim ymeter = CInt(dst2.Height - dst2.Height * i / (task.MaxZmeters * distanceRatio))
            dst2.Line(New cv.Point(0, ymeter), New cv.Point(dst2.Width, ymeter), cv.Scalar.AliceBlue, 1)
            SetTrueText(CStr(i) + "m", New cv.Point(10, ymeter))
        Next

        Dim cam = task.topCameraPoint
        Dim marker As New cv.Point2f(cam.X, dst2.Height / task.MaxZmeters)
        Dim topLen = marker.Y * Math.Tan((task.hFov / 2) * cv.Cv2.PI / 180)
        Dim sideLen = marker.Y * Math.Tan((task.vFov / 2) * cv.Cv2.PI / 180)
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
        Dim x = dst2.Height / Math.Tan(startAngle * cv.Cv2.PI / 180)

        Dim fovRight = New cv.Point(task.topCameraPoint.X + x, 0)
        Dim fovLeft = New cv.Point(task.topCameraPoint.X - x, fovRight.Y)

        dst2.Line(task.topCameraPoint, fovLeft, white, 1, task.lineType)

        DrawCircle(dst2, markerLeft, task.DotSize + 3, cv.Scalar.Red)
        DrawCircle(dst2, markerRight, task.DotSize + 3, cv.Scalar.Red)
        dst2.Line(cam, markerLeft, cv.Scalar.Red, 1, task.lineType)
        dst2.Line(cam, markerRight, cv.Scalar.Red, 1, task.lineType)

        Dim shift = (src.Width - src.Height) / 2
        Dim labelLocation = New cv.Point(dst2.Width / 2 + shift, dst2.Height * 15 / 16)
        SetTrueText("hFOV=" + Format(180 - startAngle * 2, "0.0") + " deg.", New cv.Point(4, dst2.Height * 7 / 8))
        dst2.Line(task.topCameraPoint, fovRight, white, task.lineWidth, task.lineWidth)
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
        dst2 = heat.dst0.InRange(task.frameHistoryCount, task.frameHistoryCount).ConvertScaleAbs
        dst3 = heat.dst1.InRange(task.frameHistoryCount, task.frameHistoryCount).ConvertScaleAbs
    End Sub
End Class






Public Class Cloud_SoloRegions : Inherits TaskParent
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





Public Class Cloud_SurfaceH_CPP : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Public plot As New Plot_Basics_CPP
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
            If dst2.Channels() = 1 Then plot.srcY.Add(dst2.Row(i).CountNonZero) Else plot.srcY.Add(dst2.Row(i).CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero)
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
        dst3 = plot.dst2.Transpose()
        dst3 = dst3.Flip(cv.FlipMode.Y)
        labels(2) = "Top row = " + CStr(topRow) + " peak row = " + CStr(peakRow) + " bottom row = " + CStr(botRow)
    End Sub
End Class




Public Class Cloud_SurfaceH : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Public plot As New Plot_Histogram
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
        Dim hist = New cv.Mat(dst2.Height, 1, cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim indexer = hist.GetGenericIndexer(Of Single)()

        topRow = 0
        botRow = 0
        peakRow = 0
        Dim peakVal As Integer
        If dst2.Channels() <> 1 Then dst1 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        For i = 0 To dst1.Height - 1
            indexer(i) = dst1.Row(i).CountNonZero
            If peakVal < indexer(i) Then
                peakVal = indexer(i)
                peakRow = i
            End If
            If topRow = 0 And indexer(i) > 10 Then topRow = i
        Next

        plot.maxRange = (Math.Floor(peakVal / 100) + 1) * 100
        For i = hist.Rows - 1 To 0 Step -1
            If botRow = 0 And indexer(i) > 10 Then botRow = i
        Next
        plot.Run(hist)
        dst3 = plot.dst2.Transpose()
        dst3 = dst3.Flip(cv.FlipMode.Y)(New cv.Rect(0, 0, dst0.Height, dst0.Height)).Resize(dst0.Size)
        labels(2) = "Top row = " + CStr(topRow) + " peak row = " + CStr(peakRow) + " bottom row = " + CStr(botRow)

        Dim ratio = task.mouseMovePoint.Y / dst2.Height
        Dim offset = ratio * dst3.Height
        dst2.Line(New cv.Point(0, task.mouseMovePoint.Y), New cv.Point(dst2.Width, task.mouseMovePoint.Y), yellow, task.lineWidth, task.lineWidth)
        dst3.Line(New cv.Point(0, offset), New cv.Point(dst3.Width, offset), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class




Public Class Cloud_GridInspector : Inherits TaskParent
    Public Sub New()
        If task.bricks Is Nothing Then task.bricks = New Brick_Basics
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        task.mouseMovePoint.X = dst2.Width / 2
        desc = "Inspect x, y, and z values by brick"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim cLine = task.mouseMovePoint.X

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

        Dim topPt = New cv.Point2f(cLine, 0)
        Dim botPt = New cv.Point2f(cLine, dst2.Height)
        dst2 = task.depthRGB
        dst2.Line(topPt, botPt, 255, task.lineWidth, task.lineWidth)

        SetTrueText("Values show brick.pt3d values at the blue line.", New cv.Point(dst2.Width / 2, 0), 3)
        For i = 0 To dst2.Height - 1 Step task.brickSize
            Dim pt = New cv.Point2f(cLine, i)
            Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            Dim center = task.bricks.brickList(index).center
            Dim xyz = task.pointCloud.Get(Of cv.Vec3f)(center.Y, center.X)
            SetTrueText("Row " + Format(i, "00") + vbTab + vbTab + Format(xyz(0), fmt2) + vbTab +
                                 Format(xyz(1), fmt2) + vbTab + Format(xyz(2), fmt2), New cv.Point(5, pt.Y), 3)
        Next
        labels(2) = "Values displayed are the point cloud X, Y, and Z values for column " + CStr(cLine)
        labels(3) = "Move mouse in the image at left to see the point cloud X, Y, and Z values."
    End Sub
End Class





Public Class Cloud_FrustrumTop : Inherits TaskParent
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

        heat.Run(frustrum.dst3.Resize(dst2.Size))

        setupTop.Run(heat.dst2)
        dst2 = setupTop.dst2
    End Sub
End Class





Public Class Cloud_FrustrumSide : Inherits TaskParent
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
        heat.Run(frustrum.dst3.Resize(dst2.Size))

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
        dst2.ConvertTo(dst2, cv.MatType.CV_32S)
        reduction.Run(dst2)
        reduction.dst2.ConvertTo(dst1, cv.MatType.CV_32F)
        dst1 *= 0.001
        If standaloneTest() Then
            dst3 = task.pointCloud
        Else
            Dim mm = GetMinMax(dst1)
            dst1 *= task.MaxZmeters / mm.maxVal
            cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1), dst1}, dst3)
        End If
    End Sub
End Class




Public Class Cloud_ReducedTopView : Inherits TaskParent
    Dim split2 As New Cloud_ReduceSplit2
    Public Sub New()
        desc = "Create a stable side view of the point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        split2.Run(task.pointCloud)

        cv.Cv2.CalcHist({split2.dst3}, task.channelsTop, New cv.Mat, dst1, 2, task.bins2D, task.rangesTop)

        dst1 = dst1.Flip(cv.FlipMode.X)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class




Public Class Cloud_ReducedSideView : Inherits TaskParent
    Dim split2 As New Cloud_ReduceSplit2
    Public Sub New()
        desc = "Show where vertical neighbor depth values are within X mm's"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        split2.Run(Nothing)

        cv.Cv2.CalcHist({split2.dst3}, task.channelsSide, New cv.Mat, dst1, 2, task.bins2D, task.rangesSide)

        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class





Public Class Cloud_ReducedViews : Inherits TaskParent
    Dim split2 As New Cloud_ReduceSplit2
    Public Sub New()
        labels = {"", "", "Reduced side view", "Reduced top view"}
        desc = "Show where vertical neighbor depth values are within X mm's"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        split2.Run(Nothing)

        cv.Cv2.CalcHist({split2.dst3}, task.channelsSide, New cv.Mat, dst1, 2, task.bins2D, task.rangesSide)

        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)

        cv.Cv2.CalcHist({split2.dst3}, task.channelsTop, New cv.Mat, dst1, 2, task.bins2D, task.rangesTop)

        dst1 = dst1.Flip(cv.FlipMode.X)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst3, cv.MatType.CV_8UC1)
    End Sub
End Class





Public Class Cloud_Split : Inherits TaskParent
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

        labels(1) = "Min/Max for X " + Format(mmx.minVal, fmt1) + " / " + Format(mmx.maxVal, fmt1)
        labels(2) = "Min/Max for Y " + Format(mmy.minVal, fmt1) + " / " + Format(mmy.maxVal, fmt1)
        labels(3) = "Min/Max for Z " + Format(mmz.minVal, fmt1) + " / " + Format(mmz.maxVal, fmt1)
    End Sub
End Class





Public Class Cloud_Continuous_VB : Inherits TaskParent
    Dim options As New Options_Features
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Show where the pointcloud is continuous"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

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






Public Class Cloud_Continuous_GridX : Inherits TaskParent
    Dim options As New Options_Features
    Public Sub New()
        If task.bricks Is Nothing Then task.bricks = New Brick_Basics
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Show where the pointcloud is continuous at the brick resolution"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim gcPrev = task.bricks.brickList(0)
        For Each brick In task.bricks.brickList
            If brick.rect.X > 0 Then
                If Math.Abs(brick.depth - gcPrev.depth) <= task.depthDiffMeters Then
                    dst2(brick.rect).SetTo(255)
                Else
                    dst3(brick.rect).SetTo(255)
                End If
            End If
            gcPrev = brick
        Next

        labels(2) = "White pixels: Z-values within " + CStr(task.depthDiffMeters) + " meters of neighbor in X direction"
        labels(3) = "Mask showing discontinuities > " + CStr(task.depthDiffMeters) + " meters of neighbor in X direction"
    End Sub
End Class






Public Class Cloud_Continuous_GridXY : Inherits TaskParent
    Dim options As New Options_Features
    Public Sub New()
        If task.bricks Is Nothing Then task.bricks = New Brick_Basics
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Show where the pointcloud is continuous at the brick resolution"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

        dst2.SetTo(0)
        Dim gcPrev = task.bricks.brickList(0)
        Dim cellMat As New cv.Mat(task.brickSize, task.brickSize, cv.MatType.CV_8U, cv.Scalar.All(127))
        For Each brick In task.bricks.brickList
            Dim gcAbove = task.bricks.brickList(CInt(brick.index Mod task.bricksPerRow))
            If brick.correlation > task.fCorrThreshold Then
                If brick.rect.Y = 0 Or brick.rect.X = 0 Then Continue For
                If Math.Abs(brick.depth - gcPrev.depth) <= task.depthDiffMeters Then dst2(brick.rect).SetTo(128)
                If Math.Abs(brick.depth - gcAbove.depth) <= task.depthDiffMeters And
                brick.rect.Width = cellMat.Width And brick.rect.Height = cellMat.Height Then
                    cv.Cv2.Add(dst2(brick.rect), cellMat, dst2(brick.rect))
                End If
            End If
            gcPrev = brick
        Next

        labels(2) = "White pixels: Z-values within " + CStr(task.depthDiffMeters) + " meters of neighbor in X and Y direction"
    End Sub
End Class






Public Class Cloud_Templates : Inherits TaskParent
    Public templateX As New cv.Mat, templateY As New cv.Mat
    Dim contours As New Contour_Basics
    Public Sub New()
        templateX = New cv.Mat(task.workRes, cv.MatType.CV_32F)
        templateY = New cv.Mat(task.workRes, cv.MatType.CV_32F)
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
        templateX -= task.calibData.rgbIntrinsics.ppx
        templateY -= task.calibData.rgbIntrinsics.ppy

        desc = "Prepare for injecting depth into the point cloud."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contours.Run(src)
        If standalone Then
            src = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
            For Each contour In contours.contourList
                If contour.depth = 0 Then Continue For
                contour.depth = task.pcSplit(2)(contour.rect).Mean(contour.mask)
                src(contour.rect).SetTo(contour.depth, contour.mask)
            Next
        End If
        Dim fxTemplate = task.calibData.rgbIntrinsics.fx
        Dim fyTemplate = task.calibData.rgbIntrinsics.fy
        Dim worldX As New cv.Mat, worldY As New cv.Mat

        cv.Cv2.Multiply(templateX, src, worldX)
        worldX *= 1 / fxTemplate

        cv.Cv2.Multiply(templateY, src, worldY)
        worldY *= 1 / fyTemplate

        cv.Cv2.Merge({worldX, worldY, src}, dst2)
    End Sub
End Class
