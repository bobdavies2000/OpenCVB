Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports OpenCvSharp.ML.DTrees

Public Class PointCloud_Basics : Inherits VB_Parent
    Public actualCount As Integer
    Dim deltaThreshold As Single

    Public allPointsH As New List(Of cv.Point3f)
    Public allPointsV As New List(Of cv.Point3f)

    Public hList As New List(Of List(Of cv.Point3f))
    Public xyHList As New List(Of List(Of cv.Point))

    Public vList As New List(Of List(Of cv.Point3f))
    Public xyVList As New List(Of List(Of cv.Point))
    Public Sub New()
        setPointCloudGrid()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Delta Z threshold (cm)", 0, 100, 5)
        desc = "Reduce the point cloud to a manageable number points in 3D"
    End Sub
    Public Function findHorizontalPoints(ByRef xyList As List(Of List(Of cv.Point))) As List(Of List(Of cv.Point3f))
        Dim ptlist As New List(Of List(Of cv.Point3f))
        Dim lastVec = New cv.Point3f
        For y = 0 To task.pointCloud.Height - 1 Step task.gridList(0).Height - 1
            Dim vecList As New List(Of cv.Point3f)
            Dim xyVec As New List(Of cv.Point)
            For x = 0 To task.pointCloud.Width - 1 Step task.gridList(0).Width - 1
                Dim vec = task.pointCloud.Get(Of cv.Point3f)(y, x)
                Dim jumpZ As Boolean = False
                If vec.Z > 0 Then
                    If (Math.Abs(lastVec.Z - vec.Z) < deltaThreshold And lastVec.X < vec.X) Or lastVec.Z = 0 Then
                        actualCount += 1
                        DrawCircle(dst2,New cv.Point(x, y), task.dotSize, cv.Scalar.White)
                        vecList.Add(vec)
                        xyVec.Add(New cv.Point(x, y))
                    Else
                        jumpZ = True
                    End If
                End If
                If vec.Z = 0 Or jumpZ Then
                    If vecList.Count > 1 Then
                        ptlist.Add(New List(Of cv.Point3f)(vecList))
                        xyList.Add(New List(Of cv.Point)(xyVec))
                    End If
                    vecList.Clear()
                    xyVec.Clear()
                End If
                lastVec = vec
            Next
        Next
        Return ptlist
    End Function
    Public Function findVerticalPoints(ByRef xyList As List(Of List(Of cv.Point))) As List(Of List(Of cv.Point3f))
        Dim ptlist As New List(Of List(Of cv.Point3f))
        Dim lastVec = New cv.Point3f
        For x = 0 To task.pointCloud.Width - 1 Step task.gridList(0).Width - 1
            Dim vecList As New List(Of cv.Point3f)
            Dim xyVec As New List(Of cv.Point)
            For y = 0 To task.pointCloud.Height - 1 Step task.gridList(0).Height - 1
                Dim vec = task.pointCloud.Get(Of cv.Point3f)(y, x)
                Dim jumpZ As Boolean = False
                If vec.Z > 0 Then
                    If (Math.Abs(lastVec.Z - vec.Z) < deltaThreshold And lastVec.Y < vec.Y) Or lastVec.Z = 0 Then
                        actualCount += 1
                        DrawCircle(dst2,New cv.Point(x, y), task.dotSize, cv.Scalar.White)
                        vecList.Add(vec)
                        xyVec.Add(New cv.Point(x, y))
                    Else
                        jumpZ = True
                    End If
                End If
                If vec.Z = 0 Or jumpZ Then
                    If vecList.Count > 1 Then
                        ptlist.Add(New List(Of cv.Point3f)(vecList))
                        xyList.Add(New List(Of cv.Point)(xyVec))
                    End If
                    vecList.Clear()
                    xyVec.Clear()
                End If
                lastVec = vec
            Next
        Next
        Return ptlist
    End Function

    Public Sub RunVB(src As cv.Mat)
        Static deltaSlider = FindSlider("Delta Z threshold (cm)")
        deltaThreshold = deltaSlider.value / 100

        dst2 = src
        actualCount = 0

        xyHList.Clear()
        hList = findHorizontalPoints(xyHList)

        allPointsH.Clear()
        For Each h In hList
            For Each pt In h
                allPointsH.Add(pt)
            Next
        Next

        xyVList.Clear()
        vList = findVerticalPoints(xyVList)

        allPointsV.Clear()
        For Each v In vList
            For Each pt In v
                allPointsV.Add(pt)
            Next
        Next

        labels(2) = "Point series found = " + CStr(hList.Count + vList.Count)
    End Sub
End Class








Public Class PointCloud_Point3f : Inherits VB_Parent
    Public Sub New()
        desc = "Display the point cloud CV_32FC3 format"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = task.pointCloud
    End Sub
End Class







Public Class PointCloud_Spin : Inherits VB_Parent
    Dim options As New Options_IMU
    Dim gMat As New IMU_GMatrixWithOptions
    Public Sub New()
        If findfrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Spin pointcloud on X-axis")
            check.addCheckBox("Spin pointcloud on Y-axis")
            check.addCheckBox("Spin pointcloud on Z-axis")
            check.Box(2).Checked = True
        End If

        task.gOptions.gravityPointCloud.Checked = False
        desc = "Spin the point cloud exercise"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static xCheck = findCheckBox("Spin pointcloud on X-axis")
        Static yCheck = findCheckBox("Spin pointcloud on Y-axis")
        Static zCheck = findCheckBox("Spin pointcloud on Z-axis")
        Static xRotateSlider = FindSlider("Rotate pointcloud around X-axis (degrees)")
        Static yRotateSlider = FindSlider("Rotate pointcloud around Y-axis (degrees)")
        Static zRotateSlider = FindSlider("Rotate pointcloud around Z-axis (degrees)")

        Static xBump = 1, yBump = 1, zBump = 1

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








Public Class PointCloud_Spin2 : Inherits VB_Parent
    Dim spin As New PointCloud_Spin
    Dim redC As New RedCloud_Basics
    Dim redCSpin As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "RedCloud output", "Spinning RedCloud output - use options to spin on different axes."}
        desc = "Spin the RedCloud output exercise"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        spin.Run(src)
        task.pointCloud = spin.dst2
        redCSpin.Run(src)
        dst3 = redCSpin.dst2
    End Sub
End Class






Public Class PointCloud_Continuous_VB : Inherits VB_Parent
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold of continuity in mm", 0, 1000, 10)
        End If

        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Show where the pointcloud is continuous"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = FindSlider("Threshold of continuity in mm")
        Dim threshold = thresholdSlider.Value / 1000

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

        dst2.SetTo(0)
        dst3.SetTo(0)
        For y = 0 To input.Height - 1
            For x = 1 To input.Width - 1
                Dim p1 = input.Get(Of Single)(y, x - 1)
                Dim p2 = input.Get(Of Single)(y, x)
                If Math.Abs(p1 - p2) <= threshold Then dst2.Set(Of Byte)(y, x, 255) Else dst3.Set(Of Byte)(y, x, 255)
            Next
        Next

        dst3.SetTo(0, task.noDepthMask)
        dst2.SetTo(0, task.noDepthMask)
        labels(2) = "White pixels: Z-values within " + CStr(thresholdSlider.Value) + " mm's of X neighbor"
        labels(3) = "Mask showing discontinuities > " + CStr(thresholdSlider.Value) + " mm's of X neighbor"
    End Sub
End Class





' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
' https://www.mynteye.com/pages/mynt-eye-d
Public Class PointCloud_SetupSide : Inherits VB_Parent
    Dim arcSize As Integer
    Public xCheckbox As Windows.Forms.CheckBox
    Public zCheckbox As Windows.Forms.CheckBox
    Public Sub New()
        arcSize = dst2.Width / 15
        labels(2) = "Layout markers for side view"
        desc = "Create the colorized mat used for side projections"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim distanceRatio As Single = 1
        If src.Channels <> 3 Then src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If standaloneTest() Then dst2.SetTo(0) Else src.CopyTo(dst2)
        DrawCircle(dst2,task.sideCameraPoint, task.dotSize, cv.Scalar.BlueViolet)
        For i = 1 To task.maxZmeters
            Dim xmeter = CInt(dst2.Width * i / task.maxZmeters * distanceRatio)
            dst2.Line(New cv.Point(xmeter, 0), New cv.Point(xmeter, dst2.Height), cv.Scalar.AliceBlue, 1)
            setTrueText(CStr(i) + "m", New cv.Point(xmeter - src.Width / 24, dst2.Height - 10))
        Next

        Dim cam = task.sideCameraPoint
        Dim marker As New cv.Point2f(dst2.Width / (task.maxZmeters * distanceRatio), 0)
        marker.Y = marker.X * Math.Tan((task.vFov / 2) * cv.Cv2.PI / 180)
        Dim topLen = marker.X * Math.Tan((task.hFov / 2) * cv.Cv2.PI / 180)
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
            DrawCircle(dst2,markerLeft, task.dotSize, cv.Scalar.Red)
            DrawCircle(dst2,markerRight, task.dotSize, cv.Scalar.Red)
        End If

        ' draw the arc enclosing the camera FOV
        Dim startAngle = (180 - task.vFov) / 2
        Dim y = dst2.Width / Math.Tan(startAngle * cv.Cv2.PI / 180)

        Dim fovTop = New cv.Point(dst2.Width, cam.Y - y)
        Dim fovBot = New cv.Point(dst2.Width, cam.Y + y)

        dst2.Line(cam, fovTop, cv.Scalar.White, 1, task.lineType)
        dst2.Line(cam, fovBot, cv.Scalar.White, 1, task.lineType)

        DrawCircle(dst2,markerLeft, task.dotSize + 3, cv.Scalar.Red)
        DrawCircle(dst2,markerRight, task.dotSize + 3, cv.Scalar.Red)
        dst2.Line(cam, markerLeft, cv.Scalar.Red, 1, task.lineType)
        dst2.Line(cam, markerRight, cv.Scalar.Red, 1, task.lineType)

        Dim labelLocation = New cv.Point(src.Width * 0.02, src.Height * 7 / 8)
        setTrueText("vFOV=" + Format(180 - startAngle * 2, "0.0") + " deg.", New cv.Point(4, dst2.Height * 3 / 4))
    End Sub
End Class





' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
' https://www.mynteye.com/pages/mynt-eye-d
Public Class PointCloud_SetupTop : Inherits VB_Parent
    Dim arcSize As Integer
    Public Sub New()
        arcSize = dst2.Width / 15

        labels(2) = "Layout markers for top view"
        desc = "Create the colorize the mat for a topdown projections"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim distanceRatio As Single = 1
        If src.Channels <> 3 Then src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If standaloneTest() Then dst2.SetTo(0) Else src.CopyTo(dst2)
        DrawCircle(dst2,task.topCameraPoint, task.dotSize, cv.Scalar.BlueViolet)
        For i = 1 To task.maxZmeters
            Dim ymeter = CInt(dst2.Height - dst2.Height * i / (task.maxZmeters * distanceRatio))
            dst2.Line(New cv.Point(0, ymeter), New cv.Point(dst2.Width, ymeter), cv.Scalar.AliceBlue, 1)
            setTrueText(CStr(i) + "m", New cv.Point(10, ymeter))
        Next

        Dim cam = task.topCameraPoint
        Dim marker As New cv.Point2f(cam.X, dst2.Height / task.maxZmeters)
        Dim topLen = marker.Y * Math.Tan((task.hFov / 2) * cv.Cv2.PI / 180)
        Dim sideLen = marker.Y * Math.Tan((task.vFov / 2) * cv.Cv2.PI / 180)
        Dim markerLeft = New cv.Point(cam.X - topLen, marker.Y)
        Dim markerRight = New cv.Point(cam.X + topLen, marker.Y)

        Static zRotateSlider = FindSlider("Rotate pointcloud around Z-axis (degrees)")
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

        dst2.Line(task.topCameraPoint, fovLeft, cv.Scalar.White, 1, task.lineType)

        DrawCircle(dst2,markerLeft, task.dotSize + 3, cv.Scalar.Red)
        DrawCircle(dst2,markerRight, task.dotSize + 3, cv.Scalar.Red)
        dst2.Line(cam, markerLeft, cv.Scalar.Red, 1, task.lineType)
        dst2.Line(cam, markerRight, cv.Scalar.Red, 1, task.lineType)

        Dim shift = (src.Width - src.Height) / 2
        Dim labelLocation = New cv.Point(dst2.Width / 2 + shift, dst2.Height * 15 / 16)
        setTrueText("hFOV=" + Format(180 - startAngle * 2, "0.0") + " deg.", New cv.Point(4, dst2.Height * 7 / 8))
        DrawLine(dst2, task.topCameraPoint, fovRight, cv.Scalar.White)
    End Sub
End Class







Public Class PointCloud_Raw_CPP : Inherits VB_Parent
    Dim depthBytes() As Byte
    Public Sub New()
        labels(2) = "Top View"
        labels(3) = "Side View"
        desc = "Project the depth data onto a top view And side view."
        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.firstPass Then ReDim depthBytes(task.pcSplit(2).Total * task.pcSplit(2).ElemSize - 1)

        Marshal.Copy(task.pcSplit(2).Data, depthBytes, 0, depthBytes.Length)
        Dim handleDepth = GCHandle.Alloc(depthBytes, GCHandleType.Pinned)

        Dim imagePtr = SimpleProjectionRun(cPtr, handleDepth.AddrOfPinnedObject, 0, task.maxZmeters, task.pcSplit(2).Height, task.pcSplit(2).Width)

        dst2 = New cv.Mat(task.pcSplit(2).Rows, task.pcSplit(2).Cols, cv.MatType.CV_8U, imagePtr).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = New cv.Mat(task.pcSplit(2).Rows, task.pcSplit(2).Cols, cv.MatType.CV_8U, SimpleProjectionSide(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        handleDepth.Free()
        labels(2) = "Top View (looking down)"
        labels(3) = "Side View"
    End Sub
    Public Sub Close()
        SimpleProjectionClose(cPtr)
    End Sub
End Class





Public Class PointCloud_Raw : Inherits VB_Parent
    Dim depthBytes() As Byte
    Public Sub New()
        labels(2) = "Top View"
        labels(3) = "Side View"
        desc = "Project the depth data onto a top view And side view - Using only VB code (too slow.)"
        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim range As Single = task.maxZmeters

        ' this VB.Net version is much slower than the optimized C++ version below.
        dst2 = src.EmptyClone.SetTo(cv.Scalar.White)
        dst3 = dst2.Clone()
        Dim black = New cv.Vec3b(0, 0, 0)
        Parallel.ForEach(task.gridList,
             Sub(roi)
                 For y = roi.Y To roi.Y + roi.Height - 1
                     For x = roi.X To roi.X + roi.Width - 1
                         Dim m = task.depthMask.Get(Of Byte)(y, x)
                         If m > 0 Then
                             Dim depth = task.pcSplit(2).Get(Of Single)(y, x)
                             Dim dy = CInt(src.Height * depth / range)
                             If dy < src.Height And dy > 0 Then dst2.Set(Of cv.Vec3b)(src.Height - dy, x, black)
                             Dim dx = CInt(src.Width * depth / range)
                             If dx < src.Width And dx > 0 Then dst3.Set(Of cv.Vec3b)(y, dx, black)
                         End If
                     Next
                 Next
             End Sub)
        labels(2) = "Top View (looking down)"
        labels(3) = "Side View"
    End Sub
    Public Sub Close()
        SimpleProjectionClose(cPtr)
    End Sub
End Class







Public Class PointCloud_Solo : Inherits VB_Parent
    Public heat As New HeatMap_Basics
    Public Sub New()
        findCheckBox("Top View (Unchecked Side View)").Checked = True
        labels(2) = "Top down view after inrange sampling"
        labels(3) = "Histogram after filtering For Single-only histogram bins"
        desc = "Find floor And ceiling Using gravity aligned top-down view And selecting bins With exactly 1 sample"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        heat.Run(src)
        dst2 = heat.dst0.InRange(task.frameHistoryCount, task.frameHistoryCount).ConvertScaleAbs
        dst3 = heat.dst1.InRange(task.frameHistoryCount, task.frameHistoryCount).ConvertScaleAbs
    End Sub
End Class






Public Class PointCloud_SoloRegions : Inherits VB_Parent
    Public solo As New PointCloud_Solo
    Dim dilate As New Dilate_Basics
    Public Sub New()
        labels(2) = "Top down view before inrange sampling"
        labels(3) = "Histogram after filtering For Single-only histogram bins"
        desc = "Find floor And ceiling Using gravity aligned top-down view And selecting bins With exactly 1 sample"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        solo.Run(src)
        dst2 = solo.dst2
        dst3 = solo.dst3
        dilate.Run(dst3.Clone)
        dst3 = dilate.dst2
    End Sub
End Class








Public Class PointCloud_SurfaceH_CPP : Inherits VB_Parent
    Public heat As New HeatMap_Basics
    Public plot As New Plot_Basics_CPP
    Public topRow As Integer
    Public botRow As Integer
    Public peakRow As Integer
    Public Sub New()
        desc = "Find the horizontal surfaces With a projects Of the SideView histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        heat.Run(src)
        dst2 = heat.dst3

        topRow = 0
        botRow = 0
        peakRow = 0
        Dim peakVal As Integer
        For i = 0 To dst2.Height - 1
            plot.srcX.Add(i)
            If dst2.Channels = 1 Then plot.srcY.Add(dst2.Row(i).CountNonZero) Else plot.srcY.Add(dst2.Row(i).CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero)
            If peakVal < plot.srcY(i) Then
                peakVal = plot.srcY(i)
                peakRow = i
            End If
            If topRow = 0 And plot.srcY(i) > 10 Then topRow = i
        Next

        For i = plot.srcY.Count - 1 To 0 Step -1
            If botRow = 0 And plot.srcY(i) > 10 Then botRow = i
        Next
        plot.Run(empty)
        dst3 = plot.dst2.Transpose()
        dst3 = dst3.Flip(cv.FlipMode.Y)
        labels(2) = "Top row = " + CStr(topRow) + " peak row = " + CStr(peakRow) + " bottom row = " + CStr(botRow)
    End Sub
End Class







Public Class PointCloud_SurfaceH : Inherits VB_Parent
    Public heat As New HeatMap_Basics
    Public plot As New Plot_Histogram
    Public topRow As Integer
    Public botRow As Integer
    Public peakRow As Integer
    Public Sub New()
        findCheckBox("Top View (Unchecked Side View)").Checked = True
        labels(3) = "Histogram Of Each Of " + CStr(task.histogramBins) + " bins aligned With the sideview"
        desc = "Find the horizontal surfaces With a projects Of the SideView histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        heat.Run(src)
        dst2 = heat.dst2
        Dim hist = New cv.Mat(dst2.Height, 1, cv.MatType.CV_32F, 0)
        Dim indexer = hist.GetGenericIndexer(Of Single)()

        topRow = 0
        botRow = 0
        peakRow = 0
        Dim peakVal As Integer
        If dst2.Channels <> 1 Then dst1 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        For i = 0 To dst1.Height - 1
            indexer(i) = dst1.Row(i).CountNonZero
            If peakVal < indexer(i) Then
                peakVal = indexer(i)
                peakRow = i
            End If
            If topRow = 0 And indexer(i) > 10 Then topRow = i
        Next

        plot.maxValue = (Math.Floor(peakVal / 100) + 1) * 100
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









Public Class PointCloud_NeighborV : Inherits VB_Parent
    Dim options As New Options_Neighbors
    Public Sub New()
        desc = "Show where vertical neighbor depth values are within Y mm's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim tmp32f = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        Dim r1 = New cv.Rect(options.pixels, 0, dst2.Width - options.pixels, dst2.Height)
        Dim r2 = New cv.Rect(0, 0, dst2.Width - options.pixels, dst2.Height)
        cv.Cv2.Absdiff(src(r1), src(r2), tmp32f(r1))
        tmp32f = tmp32f.Threshold(options.threshold, 255, cv.ThresholdTypes.BinaryInv)
        dst2 = tmp32f.ConvertScaleAbs(255)
        dst2.SetTo(0, task.noDepthMask)
        dst2(New cv.Rect(0, dst2.Height - options.pixels, dst2.Width, options.pixels)).SetTo(0)
        labels(2) = "White: z is within " + Format(options.threshold * 1000, fmt0) + " mm's with Y pixel offset " + CStr(options.pixels)
    End Sub
End Class






Public Class PointCloud_Visualize : Inherits VB_Parent
    Public Sub New()
        labels = {"", "", "Pointcloud visualized", ""}
        desc = "Display the pointcloud as a BGR image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim pcSplit = {task.pcSplit(0).ConvertScaleAbs(255), task.pcSplit(1).ConvertScaleAbs(255), task.pcSplit(2).ConvertScaleAbs(255)}
        cv.Cv2.Merge(pcSplit, dst2)
    End Sub
End Class







Public Class PointCloud_PCpointsMask : Inherits VB_Parent
    Public pcPoints As cv.Mat
    Public actualCount As Integer
    Public Sub New()
        setPointCloudGrid()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Reduce the point cloud to a manageable number points in 3D representing the averages of X, Y, and Z in that roi."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.optionsChanged Then pcPoints = New cv.Mat(task.gridRows, task.gridCols, cv.MatType.CV_32FC3, 0)

        dst2.SetTo(0)
        actualCount = 0
        Dim lastMeanZ As Single
        For y = 0 To task.gridRows - 1
            For x = 0 To task.gridCols - 1
                Dim roi = task.gridList(y * task.gridCols + x)
                Dim mean = task.pointCloud(roi).Mean(task.depthMask(roi))
                Dim depthPresent = task.depthMask(roi).CountNonZero > roi.Width * roi.Height / 2
                If (depthPresent And mean(2) > 0 And Math.Abs(lastMeanZ - mean(2)) < 0.2 And
                    mean(2) < task.maxZmeters) Or (lastMeanZ = 0 And mean(2) > 0) Then

                    pcPoints.Set(Of cv.Point3f)(y, x, New cv.Point3f(mean(0), mean(1), mean(2)))
                    actualCount += 1
                    DrawCircle(dst2,New cv.Point(roi.X, roi.Y), task.dotSize * Math.Max(mean(2), 1), cv.Scalar.White)
                End If
                lastMeanZ = mean(2)
            Next
        Next
        labels(2) = "PointCloud Point Points found = " + CStr(actualCount)
    End Sub
End Class







Public Class PointCloud_PCPoints : Inherits VB_Parent
    Public pcPoints As New List(Of cv.Point3f)
    Public Sub New()
        setPointCloudGrid()
        desc = "Reduce the point cloud to a manageable number points in 3D using the mean value"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim rw = task.gridList(0).Width / 2, rh = task.gridList(0).Height / 2
        Dim red32 = New cv.Point3f(0, 0, 1), blue32 = New cv.Point3f(1, 0, 0), white32 = New cv.Point3f(1, 1, 1)
        Dim red = cv.Scalar.Red, blue = cv.Scalar.Blue, white = cv.Scalar.White

        pcPoints.Clear()
        dst2 = src
        For Each roi In task.gridList
            Dim pt = New cv.Point(roi.X + rw, roi.Y + rh)
            Dim mean = task.pointCloud(roi).Mean(task.depthMask(roi))

            If mean(2) > 0 Then
                pcPoints.Add(Choose(pt.Y Mod 3 + 1, red32, blue32, white32))
                pcPoints.Add(New cv.Point3f(mean(0), mean(1), mean(2)))
                DrawCircle(dst2,pt, task.dotSize, Choose(pt.Y Mod 3 + 1, red, blue, white))
            End If
        Next
        labels(2) = "PointCloud Point Points found = " + CStr(pcPoints.Count / 2)
    End Sub
End Class








Public Class PointCloud_PCPointsPlane : Inherits VB_Parent
    Dim pcBasics As New PointCloud_Basics
    Public pcPoints As New List(Of cv.Point3f)
    Public xyList As New List(Of cv.Point)
    Dim white32 = New cv.Point3f(1, 1, 1)
    Public Sub New()
        setPointCloudGrid()
        desc = "Find planes using a reduced set of 3D points and the intersection of vertical and horizontal lines through those points."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        pcBasics.Run(src)

        pcPoints.Clear()
        ' points in both the vertical and horizontal lists are likely to designate a plane
        For Each pt In pcBasics.allPointsH
            If pcBasics.allPointsV.Contains(pt) Then
                pcPoints.Add(white32)
                pcPoints.Add(pt)
            End If
        Next

        labels(2) = "Point series found = " + CStr(pcPoints.Count / 2)
    End Sub
End Class








Public Class PointCloud_Inspector : Inherits VB_Parent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        task.mouseMovePoint.X = dst2.Width / 2
        desc = "Inspect x, y, and z values in a row or column"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim yLines = 20
        Dim cLine = task.mouseMovePoint.X

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

        Dim topPt = New cv.Point2f(cLine, 0)
        Dim botPt = New cv.Point2f(cLine, dst2.Height)
        dst2 = task.depthRGB
        DrawLine(dst2, topPt, botPt, 255)

        Dim stepY = dst2.Height / yLines
        setTrueText(vbTab + "   X" + vbTab + "  Y" + vbTab + "  Z", 3)
        For i = 1 To yLines - 1
            Dim pt1 = New cv.Point2f(dst2.Width, i * stepY)
            Dim pt2 = New cv.Point2f(0, i * stepY)
            DrawLine(dst2, pt1, pt2, cv.Scalar.White)

            Dim pt = New cv.Point2f(cLine, i * stepY)
            Dim xyz = task.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X)
            setTrueText("Row " + CStr(i) + vbTab + Format(xyz(0), fmt2) + vbTab + Format(xyz(1), fmt2) + vbTab + Format(xyz(2), fmt2), New cv.Point(5, pt.Y), 3)
        Next
        labels(2) = "Values displayed are the point cloud X, Y, and Z values for column " + CStr(cLine)
        labels(3) = "Move mouse in the image at left to see the point cloud X, Y, and Z values."
    End Sub
End Class









Public Class PointCloud_Average : Inherits VB_Parent
    Dim pcHistory As New List(Of cv.Mat)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32FC3, 0)
        desc = "Average all 3 elements of the point cloud - not just depth."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        pcHistory.Add(task.pointCloud)
        If pcHistory.Count >= task.frameHistoryCount Then pcHistory.RemoveAt(0)

        dst3.SetTo(0)
        For Each m In pcHistory
            dst3 += m
        Next
        dst3 *= 1 / pcHistory.Count
    End Sub
End Class






Public Class PointCloud_FrustrumTop : Inherits VB_Parent
    Dim frustrum As New Draw_Frustrum
    Dim heat As New HeatMap_Basics
    Dim setupTop As New PointCloud_SetupTop
    Public Sub New()
        task.gOptions.gravityPointCloud.Checked = False
        findCheckBox("Top View (Unchecked Side View)").Checked = True
        labels(3) = "Draw the frustrum from the top view"
        desc = "Draw the top view of the frustrum"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        frustrum.Run(src)

        heat.Run(frustrum.dst3.Resize(dst2.Size))

        setupTop.Run(heat.dst2)
        dst2 = setupTop.dst2
    End Sub
End Class








Public Class PointCloud_FrustrumSide : Inherits VB_Parent
    Dim frustrum As New Draw_Frustrum
    Dim heat As New HeatMap_Basics
    Dim setupSide As New PointCloud_SetupSide
    Public Sub New()
        task.gOptions.gravityPointCloud.Checked = False
        findCheckBox("Top View (Unchecked Side View)").Checked = False
        labels(2) = "Draw the frustrum from the side view"
        desc = "Draw the side view of the frustrum"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        frustrum.Run(src)
        heat.Run(frustrum.dst3.Resize(dst2.Size))

        setupSide.Run(heat.dst3)
        dst2 = setupSide.dst2
    End Sub
End Class








Public Class PointCloud_Histograms : Inherits VB_Parent
    Dim plot2D As New Plot_Histogram2D
    Dim plot As New Plot_Histogram
    Dim hcloud As New Hist3Dcloud_Basics
    Dim grid As New Grid_Basics
    Public histogram As New cv.Mat
    Public Sub New()
        task.gOptions.setHistogramBins(9)
        task.redOptions.XYReduction.Checked = True
        labels = {"", "", "Plot of 2D histogram", "All non-zero entries in the 2D histogram"}
        desc = "Create a 2D histogram of the point cloud data - which 2D inputs is in options."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.redOptions.Sync() ' make sure settings are consistent

        cv.Cv2.CalcHist({task.pointCloud}, task.redOptions.channels, New cv.Mat(), histogram, task.redOptions.channelCount,
                        task.redOptions.histBinList, task.redOptions.ranges)

        Select Case task.redOptions.PointCloudReduction
            Case 0, 1, 2 ' "X Reduction", "Y Reduction", "Z Reduction"
                plot.Run(histogram)
                dst2 = plot.histogram
                labels(2) = "2D plot of 1D histogram."
            Case 3, 4, 5 ' "XY Reduction", "XZ Reduction", "YZ Reduction"
                plot2D.Run(histogram)
                dst2 = plot2D.dst2
                labels(2) = "2D plot of 2D histogram."
            Case 6 ' "XYZ Reduction"
                If dst2.Type <> cv.MatType.CV_8U Then dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)

                hcloud.Run(task.pointCloud)

                histogram = hcloud.histogram
                Dim histData(histogram.Total - 1) As Single
                Marshal.Copy(histogram.Data, histData, 0, histData.Length)

                If histData.Count > 255 And task.histogramBins > 3 Then
                    task.histogramBins -= 1
                End If
                If histData.Count < 128 And task.histogramBins < task.gOptions.HistBinBar.Maximum Then
                    task.histogramBins += 1
                End If
                If task.gridList.Count < histData.Length And task.gOptions.GridSize.Value > 2 Then
                    task.gOptions.GridSize.Value -= 1
                    grid.Run(src)
                    dst2.SetTo(0)
                End If
                histData(0) = 0 ' count of zero pixels - distorts results..

                Dim maxVal = histData.ToList.Max
                For i = 0 To task.gridList.Count - 1
                    Dim roi = task.gridList(i)
                    If i >= histData.Length Then
                        dst2(roi).SetTo(0)
                    Else
                        dst2(roi).SetTo(255 * histData(i) / maxVal)
                    End If
                Next
                labels(2) = "2D plot of the resulting 3D histogram."
        End Select

        Dim mm as mmData = GetMinMax(dst2)
        dst3 = ShowPalette(dst2 * 255 / mm.maxVal)
    End Sub
End Class







Public Class PointCloud_ReduceSplit2 : Inherits VB_Parent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        UpdateAdvice(traceName + ": redOptions 'X/Y-Range X100' sliders to test further.")
        desc = "Reduce the task.pcSplit(2) for use in several algorithms."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = task.pcSplit(2) * 1000
        dst2.ConvertTo(dst2, cv.MatType.CV_32S)
        reduction.Run(dst2)
        reduction.dst2.ConvertTo(dst1, cv.MatType.CV_32F)
        dst1 *= 0.001
        If standaloneTest() Then
            dst3 = task.pointCloud
        Else
            Dim mm = GetMinMax(dst1)
            dst1 *= task.maxZmeters / mm.maxVal
            cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1), dst1}, dst3)
        End If
    End Sub
End Class








Public Class PointCloud_ReducedTopView : Inherits VB_Parent
    Dim split2 As New PointCloud_ReduceSplit2
    Public Sub New()
        UpdateAdvice(traceName + ": redOptions 'Reduction Sliders' have high impact.")
        desc = "Create a stable side view of the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        split2.Run(task.pointCloud)

        cv.Cv2.CalcHist({split2.dst3}, task.channelsTop, New cv.Mat, dst1, 2, task.bins2D, task.rangesTop)

        dst1 = dst1.Flip(cv.FlipMode.X)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class





Public Class PointCloud_ReducedSideView : Inherits VB_Parent
    Dim split2 As New PointCloud_ReduceSplit2
    Public Sub New()
        desc = "Show where vertical neighbor depth values are within X mm's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        split2.Run(Nothing)

        cv.Cv2.CalcHist({split2.dst3}, task.channelsSide, New cv.Mat, dst1, 2, task.bins2D, task.rangesSide)

        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class





Public Class PointCloud_ReducedViews : Inherits VB_Parent
    Dim split2 As New PointCloud_ReduceSplit2
    Public Sub New()
        labels = {"", "", "Reduced side view", "Reduced top view"}
        desc = "Show where vertical neighbor depth values are within X mm's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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







Public Class PointCloud_XRangeTest : Inherits VB_Parent
    Dim split2 As New PointCloud_ReduceSplit2
    Public Sub New()
        UpdateAdvice(traceName + ": redOptions 'X-Range X100' slider has high impact.")
        desc = "Test adjusting the X-Range value to squeeze a histogram into dst2."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        split2.Run(src)

        cv.Cv2.CalcHist({split2.dst3}, task.channelsTop, New cv.Mat, dst1, 2, task.bins2D, task.rangesTop)

        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1 = dst1.Flip(cv.FlipMode.X)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class








Public Class PointCloud_YRangeTest : Inherits VB_Parent
    Dim split2 As New PointCloud_ReduceSplit2
    Public Sub New()
        UpdateAdvice(traceName + ": redOptions 'Y-Range X100' slider has high impact.")
        desc = "Test adjusting the Y-Range value to squeeze a histogram into dst2."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        split2.Run(src)

        cv.Cv2.CalcHist({split2.dst3}, task.channelsSide, New cv.Mat, dst1, 2, task.bins2D, task.rangesSide)

        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class