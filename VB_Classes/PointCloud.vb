Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports OpenCvSharp
Public Class PointCloud_Basics : Inherits TaskParent
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





Public Class PointCloud_Spin : Inherits TaskParent
    Dim options As New Options_IMU
    Dim gMat As New IMU_GMatrixWithOptions
    Dim xBump = 1, yBump = 1, zBump = 1
    Public Sub New()
        If optiBase.FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Spin pointcloud on X-axis")
            check.addCheckBox("Spin pointcloud on Y-axis")
            check.addCheckBox("Spin pointcloud on Z-axis")
            check.Box(2).Checked = True
        End If

        task.gOptions.setGravityUsage(False)
        desc = "Spin the point cloud exercise"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static xCheck = optiBase.FindCheckBox("Spin pointcloud on X-axis")
        Static yCheck = optiBase.FindCheckBox("Spin pointcloud on Y-axis")
        Static zCheck = optiBase.FindCheckBox("Spin pointcloud on Z-axis")
        Static xRotateSlider = optiBase.FindSlider("Rotate pointcloud around X-axis (degrees)")
        Static yRotateSlider = optiBase.FindSlider("Rotate pointcloud around Y-axis (degrees)")
        Static zRotateSlider = optiBase.FindSlider("Rotate pointcloud around Z-axis (degrees)")

        If xCheck.checked Then
            If xRotateSlider.value = -90 Then xBump = 1
            If xRotateSlider.value = 90 Then xBump = -1
            xRotateSlider.value += xBump
        End If

        If yCheck.checked Then
            If yRotateSlider.value = -90 Then yBump = 1
            If yRotateSlider.value = 90 Then yBump = -1
            yRotateSlider.value += yBump
        End If

        If zCheck.checked Then
            If zRotateSlider.value = -90 Then zBump = 1
            If zRotateSlider.value = 90 Then zBump = -1
            zRotateSlider.value += zBump
        End If

        gMat.Run(src)

        Dim gOutput = (task.pointCloud.Reshape(1, dst2.Rows * dst2.Cols) * gMat.gMatrix).ToMat  ' <<<<<<<<<<<<<<<<<<<<<<< this is the rotation...
        dst2 = gOutput.Reshape(3, src.Rows)
    End Sub
End Class








Public Class PointCloud_Spin2 : Inherits TaskParent
    Dim spin As New PointCloud_Spin
    Dim redCSpin As New RedColor_Basics
    Public Sub New()
        labels = {"", "", "RedCloud output", "Spinning RedCloud output - use options to spin on different axes."}
        desc = "Spin the RedCloud output exercise"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        spin.Run(src)
        task.pointCloud = spin.dst2
        redCSpin.Run(src)
        dst3 = redCSpin.dst2
    End Sub
End Class






' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
' https://www.mynteye.com/pages/mynt-eye-d
Public Class PointCloud_SetupSide : Inherits TaskParent
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
        If task.useGravityPointcloud Then
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
' https://www.mynteye.com/pages/mynt-eye-d
Public Class PointCloud_SetupTop : Inherits TaskParent
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
        If task.useGravityPointcloud Then
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
        DrawLine(dst2, task.topCameraPoint, fovRight, white)
    End Sub
End Class








Public Class PointCloud_Solo : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Public Sub New()
        optiBase.FindCheckBox("Top View (Unchecked Side View)").Checked = True
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






Public Class PointCloud_SoloRegions : Inherits TaskParent
    Public solo As New PointCloud_Solo
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








Public Class PointCloud_SurfaceH_CPP : Inherits TaskParent
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







Public Class PointCloud_SurfaceH : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Public plot As New Plot_Histogram
    Public topRow As Integer
    Public botRow As Integer
    Public peakRow As Integer
    Public Sub New()
        optiBase.FindCheckBox("Top View (Unchecked Side View)").Checked = True
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
        DrawLine(dst2, New cv.Point(0, task.mouseMovePoint.Y), New cv.Point(dst2.Width, task.mouseMovePoint.Y), cv.Scalar.Yellow)
        dst3.Line(New cv.Point(0, offset), New cv.Point(dst3.Width, offset), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class










Public Class PointCloud_GridInspector : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        task.mouseMovePoint.X = dst2.Width / 2
        desc = "Inspect x, y, and z values by grid cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim cLine = task.mouseMovePoint.X

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

        Dim topPt = New cv.Point2f(cLine, 0)
        Dim botPt = New cv.Point2f(cLine, dst2.Height)
        dst2 = task.depthRGB
        DrawLine(dst2, topPt, botPt, 255)

        SetTrueText("Values show gc.pt3d values at the blue line.", New cv.Point(dst2.Width / 2, 0), 3)
        For i = 0 To dst2.Height - 1 Step task.cellSize
            Dim pt = New cv.Point2f(cLine, i)
            Dim index = task.gcMap.Get(Of Single)(pt.Y, pt.X)
            Dim xyz = task.gcList(index).pt3D
            SetTrueText("Row " + Format(i, "00") + vbTab + vbTab + Format(xyz(0), fmt2) + vbTab + Format(xyz(1), fmt2) + vbTab + Format(xyz(2), fmt2), New cv.Point(5, pt.Y), 3)
        Next
        labels(2) = "Values displayed are the point cloud X, Y, and Z values for column " + CStr(cLine)
        labels(3) = "Move mouse in the image at left to see the point cloud X, Y, and Z values."
    End Sub
End Class
















Public Class PointCloud_FrustrumTop : Inherits TaskParent
    Dim frustrum As New Draw_Frustrum
    Dim heat As New HeatMap_Basics
    Dim setupTop As New PointCloud_SetupTop
    Public Sub New()
        task.gOptions.setGravityUsage(False)
        optiBase.FindCheckBox("Top View (Unchecked Side View)").Checked = True
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








Public Class PointCloud_FrustrumSide : Inherits TaskParent
    Dim frustrum As New Draw_Frustrum
    Dim heat As New HeatMap_Basics
    Dim setupSide As New PointCloud_SetupSide
    Public Sub New()
        task.gOptions.setGravityUsage(False)
        optiBase.FindCheckBox("Top View (Unchecked Side View)").Checked = False
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








Public Class PointCloud_ReduceSplit2 : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        UpdateAdvice(traceName + ": redOptions 'X/Y-Range X100' sliders to test further.")
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








Public Class PointCloud_ReducedTopView : Inherits TaskParent
    Dim split2 As New PointCloud_ReduceSplit2
    Public Sub New()
        UpdateAdvice(traceName + ": redOptions 'Reduction Sliders' have high impact.")
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





Public Class PointCloud_ReducedSideView : Inherits TaskParent
    Dim split2 As New PointCloud_ReduceSplit2
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





Public Class PointCloud_ReducedViews : Inherits TaskParent
    Dim split2 As New PointCloud_ReduceSplit2
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







Public Class PointCloud_XRangeTest : Inherits TaskParent
    Dim split2 As New PointCloud_ReduceSplit2
    Public Sub New()
        UpdateAdvice(traceName + ": redOptions 'X-Range X100' slider has high impact.")
        desc = "Test adjusting the X-Range value to squeeze a histogram into dst2."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        split2.Run(src)

        cv.Cv2.CalcHist({split2.dst3}, task.channelsTop, New cv.Mat, dst1, 2, task.bins2D, task.rangesTop)

        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1 = dst1.Flip(cv.FlipMode.X)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class








Public Class PointCloud_YRangeTest : Inherits TaskParent
    Dim split2 As New PointCloud_ReduceSplit2
    Public Sub New()
        UpdateAdvice(traceName + ": redOptions 'Y-Range X100' slider has high impact.")
        desc = "Test adjusting the Y-Range value to squeeze a histogram into dst2."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        split2.Run(src)

        cv.Cv2.CalcHist({split2.dst3}, task.channelsSide, New cv.Mat, dst1, 2, task.bins2D, task.rangesSide)

        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class





Public Class PointCloud_Split : Inherits TaskParent
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








Public Class PointCloud_Continuous_VB : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Show where the pointcloud is continuous"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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
        labels(2) = "White pixels: Z-values within " + CStr(task.depthDiffMeters) + " mm's of X neighbor"
        labels(3) = "Mask showing discontinuities > " + CStr(task.depthDiffMeters) + " mm's of X neighbor"
    End Sub
End Class






Public Class PointCloud_Continuous_GridX : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Show where the pointcloud is continuous at the grid cell resolution"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim gcPrev = task.gcList(0)
        For Each gc In task.gcList
            If gc.rect.X > 0 Then
                If Math.Abs(gc.pt3D(2) - gcPrev.pt3D(2)) <= task.depthDiffMeters Then dst2(gc.rect).SetTo(255) Else dst3(gc.rect).SetTo(255)
            End If
            gcPrev = gc
        Next

        labels(2) = "White pixels: Z-values within " + CStr(task.depthDiffMeters) + " meters of neighbor in X direction"
        labels(3) = "Mask showing discontinuities > " + CStr(task.depthDiffMeters) + " meters of neighbor in X direction"
    End Sub
End Class






Public Class PointCloud_Continuous_GridXY : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Show where the pointcloud is continuous at the grid cell resolution"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

        dst2.SetTo(0)
        Dim gcPrev = task.gcList(0)
        Dim cellMat As New cv.Mat(task.cellSize, task.cellSize, cv.MatType.CV_8U, cv.Scalar.All(127))
        For Each gc In task.gcList
            Dim gcAbove = task.gcList(CInt(gc.index Mod task.grid.tilesPerRow))
            If gc.correlation > task.fCorrThreshold Then
                If gc.rect.Y = 0 Or gc.rect.X = 0 Then Continue For
                If Math.Abs(gc.pt3D(2) - gcPrev.pt3D(2)) <= task.depthDiffMeters Then dst2(gc.rect).SetTo(128)
                If Math.Abs(gc.pt3D(2) - gcAbove.pt3D(2)) <= task.depthDiffMeters And
                gc.rect.Width = cellMat.Width And gc.rect.Height = cellMat.Height Then
                    cv.Cv2.Add(dst2(gc.rect), cellMat, dst2(gc.rect))
                End If
            End If
            gcPrev = gc
        Next

        labels(2) = "White pixels: Z-values within " + CStr(task.depthDiffMeters) + " meters of neighbor in X direction"
        labels(3) = "Mask showing discontinuities > " + CStr(task.depthDiffMeters) + " meters of neighbor in X direction"
    End Sub
End Class