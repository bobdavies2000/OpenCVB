Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module PointCloud
    ' for performance we are putting this in an optimized C++ interface to the Kinect camera for convenience...
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionRun(cPtr As IntPtr, depth As IntPtr, desiredMin As Single, desiredMax As Single, rows As Integer, cols As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionSide(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionOpen() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub SimpleProjectionClose(cPtr As IntPtr)
    End Sub





    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Project_GravityHist_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Project_GravityHist_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Project_GravityHist_Side(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Project_GravityHist_Run(cPtr As IntPtr, xyzPtr As IntPtr, maxZ As Single, rows As Integer, cols As Integer) As IntPtr
    End Function

    Public Class compareAllowIdenticalSingleInverted : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Class compareAllowIdenticalSingle : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Class CompareMaskSize : Implements IComparer(Of Integer)
        Public Function Compare(ByVal a As Integer, ByVal b As Integer) As Integer Implements IComparer(Of Integer).Compare
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Class compareAllowIdenticalIntegerInverted : Implements IComparer(Of Integer)
        Public Function Compare(ByVal a As Integer, ByVal b As Integer) As Integer Implements IComparer(Of Integer).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Class compareAllowIdenticalInteger : Implements IComparer(Of Integer)
        Public Function Compare(ByVal a As Integer, ByVal b As Integer) As Integer Implements IComparer(Of Integer).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a >= b Then Return 1
            Return -1
        End Function
    End Class
    Public Function findNearestPoint(detailPoint As cv.Point, viewObjects As SortedList(Of Single, viewObject)) As Integer
        Dim minIndex As Integer
        Dim minDistance As Single = Single.MaxValue
        For i = 0 To viewObjects.Count - 1
            Dim pt = viewObjects.Values(i).centroid
            Dim distance = Math.Sqrt((detailPoint.X - pt.X) * (detailPoint.X - pt.X) + (detailPoint.Y - pt.Y) * (detailPoint.Y - pt.Y))
            If distance < minDistance Then
                minIndex = i
                minDistance = distance
            End If
        Next
        Return minIndex
    End Function
    Public Function findNearestCentroid(detailPoint As cv.Point, centroids As List(Of cv.Point)) As Integer
        Dim minIndex As Integer
        Dim minDistance As Single = Single.MaxValue
        For i = 0 To centroids.Count - 1
            Dim pt = centroids.ElementAt(i)
            Dim distance = Math.Sqrt((detailPoint.X - pt.X) * (detailPoint.X - pt.X) + (detailPoint.Y - pt.Y) * (detailPoint.Y - pt.Y))
            If distance < minDistance Then
                minIndex = i
                minDistance = distance
            End If
        Next
        Return minIndex
    End Function
End Module







Public Class PointCloud_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Display the point cloud in a 2D image for use with the PixelViewer"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        dst1 = task.pointCloud
    End Sub
End Class






Public Class PointCloud_Continuous
    Inherits VBparent
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Threshold of continuity in mm", 0, 1000, 10)
        End If

        task.desc = "Show where the pointcloud is continuous"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static thresholdSlider = findSlider("Threshold of continuity in mm")
        Dim threshold = thresholdSlider.value

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.depth32f

        Dim tmp32f = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        Dim r1 = New cv.Rect(1, 0, dst1.Width - 1, dst1.Height)
        Dim r2 = New cv.Rect(0, 0, dst1.Width - 1, dst1.Height)
        cv.Cv2.Absdiff(input(r1), input(r2), tmp32f(r1))
        tmp32f = tmp32f.Threshold(threshold, 255, cv.ThresholdTypes.BinaryInv)
        dst1 = tmp32f.ConvertScaleAbs(255)
        cv.Cv2.BitwiseNot(dst1, dst2)
        dst1.SetTo(0, task.noDepthMask)
        dst2.SetTo(0, task.noDepthMask)
        label1 = "White pixels: Z-values within " + CStr(thresholdSlider.value) + " mm's of X neighbor"
        label2 = "Mask showing discontinuities > " + CStr(thresholdSlider.value) + " mm's of X neighbor"
    End Sub
End Class





Public Class PointCloud_Inspector
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Inspection Line", 0, dst1.Width, dst1.Width / 2)
            sliders.setupTrackBar(1, "Y-Direction intervals", 0, 100, 30)
        End If

        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)

        task.desc = "Inspect x, y, and z values in a row or column"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static yLineSlider = findSlider("Y-Direction intervals")
        Static cLineSlider = findSlider("Inspection Line")
        Dim yLines = yLineSlider.value
        Dim cLine = cLineSlider.value

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.depth32f

        Dim topPt = New cv.Point2f(cLine, 0)
        Dim botPt = New cv.Point2f(cLine, dst1.Height)
        dst1 = task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1.Line(topPt, botPt, 255, 3, task.lineType)

        Dim stepY = dst1.Height / yLines
        For i = 0 To yLines - 1
            Dim pt1 = New cv.Point2f(dst1.Width, i * stepY)
            Dim pt2 = New cv.Point2f(0, i * stepY)
            dst1.Line(pt1, pt2, 255, 1, cv.LineTypes.Link4)

            Dim pt = New cv.Point2f(cLine, i * stepY)
            Dim xyz = task.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X)
            pt.Y += stepY
            pt.X += 20
            If pt.X > dst1.Width * 3 / 4 Then pt.X = dst1.Width * 3 / 4
            cv.Cv2.PutText(dst1, "Row " + CStr(i) + " " + Format(xyz.Item0, "#0.00") + " " + Format(xyz.Item1, "#0.00") + " " +
                           Format(xyz.Item2, "#0.00"), pt, cv.HersheyFonts.HersheyComplexSmall, 0.7, cv.Scalar.White, 2)
        Next
        label1 = "Values displayed are for column " + CStr(cLine)
    End Sub
End Class





Public Class PointCloud_Continuous_VB
    Inherits VBparent
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Threshold of continuity in mm", 0, 1000, 10)
        End If

        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U)
        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_8U)
        task.desc = "Show where the pointcloud is continuous"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static thresholdSlider = findSlider("Threshold of continuity in mm")
        Dim threshold = thresholdSlider.value

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.depth32f

        dst1.SetTo(0)
        dst2.SetTo(0)
        For y = 0 To input.Height - 1
            For x = 1 To input.Width - 1
                Dim p1 = input.Get(Of Single)(y, x - 1)
                Dim p2 = input.Get(Of Single)(y, x)
                If Math.Abs(p1 - p2) <= threshold Then dst1.Set(Of Byte)(y, x, 255) Else dst2.Set(Of Byte)(y, x, 255)
            Next
        Next

        dst2.SetTo(0, task.noDepthMask)
        dst1.SetTo(0, task.noDepthMask)
        label1 = "White pixels: Z-values within " + CStr(thresholdSlider.value) + " mm's of X neighbor"
        label2 = "Mask showing discontinuities > " + CStr(thresholdSlider.value) + " mm's of X neighbor"
    End Sub
End Class





' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
' https://www.mynteye.com/pages/mynt-eye-d
Public Class PointCloud_ColorizeSide
    Inherits VBparent
    Dim gpalette As Palette_Gradient
    Dim arcSize As Integer
    Dim imu As IMU_GVector
    Public xCheckbox As Windows.Forms.CheckBox
    Public zCheckbox As Windows.Forms.CheckBox
    Public Sub New()
        initParent()

        gpalette = New Palette_Gradient()
        gpalette.color1 = cv.Scalar.Yellow
        gpalette.color2 = cv.Scalar.Blue
        gpalette.frameModulo = 1
        arcSize = dst1.Width / 15

        gpalette.Run(dst1)
        dst1 = gpalette.dst1
        If standalone Then imu = New IMU_GVector
        label1 = "Colorize mask for side view"
        task.desc = "Create the colorized mat used for side projections"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me

        If standalone Then src = dst1 Else dst1 = src

        Dim distanceRatio As Single = 1
        Dim fsize = task.fontSize * 1.5

        dst1.Circle(task.sideCameraPoint, task.dotSize, cv.Scalar.BlueViolet, -1, task.lineType)
        For i = 1 To task.maxZ
            Dim xmeter = CInt(dst1.Width * i / task.maxZ * distanceRatio)
            dst1.Line(New cv.Point(xmeter, 0), New cv.Point(xmeter, dst1.Height), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst1, CStr(i) + "m", New cv.Point(xmeter - src.Width / 15, dst1.Height - 10), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, task.lineType)
        Next

        Dim cam = task.sideCameraPoint
        Dim marker As New cv.Point2f(dst1.Width / (task.maxZ * distanceRatio), 0)
        marker.Y = marker.X * Math.Tan((task.vFov / 2) * cv.Cv2.PI / 180)
        Dim topLen = marker.X * Math.Tan((task.hFov / 2) * cv.Cv2.PI / 180)
        Dim markerLeft = New cv.Point(marker.X, cam.Y - marker.Y)
        Dim markerRight = New cv.Point(marker.X, cam.Y + marker.Y)

        Static zRotateSlider = findSlider("Amount to rotate pointcloud around Z-axis (degrees)")
        Static zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX")
        If standalone Then imu.Run(src)
        Dim offset = Math.Sin(task.angleX) * marker.Y
        If zCheckbox.checked Then
            If task.angleX > 0 Then
                markerLeft.Y = markerLeft.Y - offset
                markerRight.Y = markerRight.Y + offset
            Else
                markerLeft.Y = markerLeft.Y + offset
                markerRight.Y = markerRight.Y - offset
            End If
        End If

        Static xCheckbox = findCheckBox("Rotate pointcloud around X-axis using gravity vector angleZ")
        Static xRotateSlider = findSlider("Amount to rotate pointcloud around X-axis (degrees)")
        If xCheckbox.Checked Then
            markerLeft = New cv.Point(markerLeft.X - cam.X, markerLeft.Y - cam.Y) ' Change the origin
            markerLeft = New cv.Point(markerLeft.X * Math.Cos(task.angleZ) - markerLeft.Y * Math.Sin(task.angleZ), ' rotate around x-axis using angleZ
                                              markerLeft.Y * Math.Cos(task.angleZ) + markerLeft.X * Math.Sin(task.angleZ))
            markerLeft = New cv.Point(markerLeft.X + cam.X, markerLeft.Y + cam.Y) ' Move the origin to the side camera location.

            ' Same as above for markerLeft but consolidated algebraically.
            markerRight = New cv.Point((markerRight.X - cam.X) * Math.Cos(task.angleZ) - (markerRight.Y - cam.Y) * Math.Sin(task.angleZ) + cam.X,
                                               (markerRight.Y - cam.Y) * Math.Cos(task.angleZ) + (markerRight.X - cam.X) * Math.Sin(task.angleZ) + cam.Y)
        End If
        If standalone = False Then
            dst1.Circle(markerLeft, task.dotSize, cv.Scalar.Red, -1, task.lineType)
            dst1.Circle(markerRight, task.dotSize, cv.Scalar.Red, -1, task.lineType)
        End If

        ' draw the arc enclosing the camera FOV
        Dim startAngle = (180 - task.vFov) / 2
        Dim y = dst1.Width / Math.Tan(startAngle * cv.Cv2.PI / 180)

        Dim fovTop = New cv.Point(dst1.Width, cam.Y - y)
        Dim fovBot = New cv.Point(dst1.Width, cam.Y + y)

        If standalone = False Then
            dst1.Ellipse(cam, New cv.Size(arcSize, arcSize), -startAngle + 90, startAngle, 0, cv.Scalar.White, 2, task.lineType)
            dst1.Ellipse(cam, New cv.Size(arcSize, arcSize), 90, 180, 180 + startAngle, cv.Scalar.White, 2, task.lineType)
            dst1.Line(cam, fovTop, cv.Scalar.White, 1, task.lineType)
            dst1.Line(cam, fovBot, cv.Scalar.White, 1, task.lineType)

            dst1.Line(cam, markerLeft, cv.Scalar.Yellow, 1, task.lineType)
            dst1.Line(cam, markerRight, cv.Scalar.Yellow, 1, task.lineType)

            Dim labelLocation = New cv.Point(src.Width * 0.02, src.Height * 7 / 8)
            cv.Cv2.PutText(dst1, "vFOV=" + CStr(180 - startAngle * 2) + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fsize,
                       cv.Scalar.White, 1, task.lineType)
        End If
    End Sub
End Class





' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
' https://www.mynteye.com/pages/mynt-eye-d
Public Class PointCloud_ColorizeTop
    Inherits VBparent
    Dim gpalette As Palette_Gradient
    Dim arcSize As Integer
    Dim imu As IMU_GVector
    Public xCheckbox As Windows.Forms.CheckBox
    Public Sub New()
        initParent()

        If standalone Then imu = New IMU_GVector
        gpalette = New Palette_Gradient()
        gpalette.color1 = cv.Scalar.Yellow
        gpalette.color2 = cv.Scalar.Blue
        gpalette.frameModulo = 1
        arcSize = dst1.Width / 15

        gpalette.Run(dst1)
        dst1 = gpalette.dst1

        label1 = "Colorize mask for top down view"
        task.desc = "Create the colorize the mat for a topdown projections"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me

        If standalone Then src = dst1 Else dst1 = src

        Dim distanceRatio As Single = 1
        Dim fsize = task.fontSize * 1.5
        dst1.Circle(task.topCameraPoint, task.dotSize, cv.Scalar.BlueViolet, -1, task.lineType)
        For i = 1 To task.maxZ
            Dim ymeter = CInt(dst1.Height - dst1.Height * i / (task.maxZ * distanceRatio))
            dst1.Line(New cv.Point(0, ymeter), New cv.Point(dst1.Width, ymeter), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst1, CStr(i) + "m", New cv.Point(10, ymeter - 10), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, task.lineType)
        Next

        Dim cam = task.topCameraPoint
        Dim marker As New cv.Point2f(cam.X, dst1.Height / (task.maxZ * distanceRatio))
        Dim topLen = marker.Y * Math.Tan((task.hFov / 2) * cv.Cv2.PI / 180)
        Dim sideLen = marker.Y * Math.Tan((task.vFov / 2) * cv.Cv2.PI / 180)
        Dim markerLeft = New cv.Point(cam.X - topLen, dst1.Height - marker.Y)
        Dim markerRight = New cv.Point(cam.X + topLen, dst1.Height - marker.Y)

        Static zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX")
        Static zRotateSlider = findSlider("Amount to rotate pointcloud around Z-axis (degrees)")
        If standalone Then imu.Run(src)
        Dim offset = Math.Sin(task.angleZ) * topLen
        If zCheckbox.checked Then
            If task.angleZ > 0 Then
                markerLeft.X = markerLeft.X - offset
                markerRight.X = markerRight.X + offset
            Else
                markerLeft.X = markerLeft.X + offset
                markerRight.X = markerRight.X - offset
            End If
        End If

        ' draw the arc enclosing the camera FOV
        Dim startAngle = (180 - task.hFov) / 2
        Dim x = dst1.Height / Math.Tan(startAngle * cv.Cv2.PI / 180)

        Dim fovRight = New cv.Point(task.topCameraPoint.X + x, 0)
        Dim fovLeft = New cv.Point(task.topCameraPoint.X - x, fovRight.Y)

        If standalone = False Then
            dst1.Ellipse(task.topCameraPoint, New cv.Size(arcSize, arcSize), -startAngle, startAngle, 0, cv.Scalar.White, 2, task.lineType)
            dst1.Ellipse(task.topCameraPoint, New cv.Size(arcSize, arcSize), 0, 180, 180 + startAngle, cv.Scalar.White, 2, task.lineType)
            dst1.Line(task.topCameraPoint, fovLeft, cv.Scalar.White, 1, task.lineType)

            dst1.Circle(markerLeft, task.dotSize, cv.Scalar.Red, -1, task.lineType)
            dst1.Circle(markerRight, task.dotSize, cv.Scalar.Red, -1, task.lineType)
            dst1.Line(cam, markerLeft, cv.Scalar.Yellow, 1, task.lineType)
            dst1.Line(cam, markerRight, cv.Scalar.Yellow, 1, task.lineType)

            Dim shift = (src.Width - src.Height) / 2
            Dim labelLocation = New cv.Point(dst1.Width / 2 + shift, dst1.Height * 15 / 16)
            cv.Cv2.PutText(dst1, "hFOV=" + CStr(180 - startAngle * 2) + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, task.lineType)
            dst1.Line(task.topCameraPoint, fovRight, cv.Scalar.White, 1, task.lineType)
        End If
    End Sub
End Class




Public Class PointCloud_Raw_CPP
    Inherits VBparent
    Dim grid As Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 64
        gridHeightSlider.Value = 32

        label1 = "Top View"
        label2 = "Side View"
        task.desc = "Project the depth data onto a top view and side view."
		' task.rank = 1

        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        grid.run(src)

        Dim h = src.Height
        Dim w = src.Width
        Dim range = CSng(task.maxDepth - task.minDepth)
        If depthBytes Is Nothing Then
            ReDim depthBytes(task.depth32f.Total * task.depth32f.ElemSize - 1)
        End If

        Marshal.Copy(task.depth32f.Data, depthBytes, 0, depthBytes.Length)
        Dim handleDepth = GCHandle.Alloc(depthBytes, GCHandleType.Pinned)

        Dim imagePtr = SimpleProjectionRun(cPtr, handleDepth.AddrOfPinnedObject, task.minDepth, task.maxDepth, task.depth32f.Height, task.depth32f.Width)

        dst1 = New cv.Mat(task.depth32f.Rows, task.depth32f.Cols, cv.MatType.CV_8U, imagePtr).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = New cv.Mat(task.depth32f.Rows, task.depth32f.Cols, cv.MatType.CV_8U, SimpleProjectionSide(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        handleDepth.Free()
        label1 = "Top View (looking down)"
        label2 = "Side View"
    End Sub
    Public Sub Close()
        SimpleProjectionClose(cPtr)
    End Sub
End Class





Public Class PointCloud_Raw
    Inherits VBparent
    Dim grid As Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 64
        gridHeightSlider.Value = 32

        label1 = "Top View"
        label2 = "Side View"
        task.desc = "Project the depth data onto a top view and side view - using only VB code (too slow.)"
		' task.rank = 1

        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        grid.run(src)

        Dim h = src.Height
        Dim w = src.Width
        Dim range = CSng(task.maxDepth - task.minDepth)

        ' this VB.Net version is much slower than the optimized C++ version below.
        dst1 = src.EmptyClone.SetTo(cv.Scalar.White)
        dst2 = dst1.Clone()
        Dim black = New cv.Vec3b(0, 0, 0)
        Parallel.ForEach(grid.roiList,
             Sub(roi)
                 For y = roi.Y To roi.Y + roi.Height - 1
                     For x = roi.X To roi.X + roi.Width - 1
                         Dim m = task.depthMask.Get(Of Byte)(y, x)
                         If m > 0 Then
                             Dim depth = task.depth32f.Get(Of Single)(y, x)
                             Dim dy = CInt(h * (depth - task.minDepth) / range)
                             If dy < h And dy > 0 Then dst1.Set(Of cv.Vec3b)(h - dy, x, black)
                             Dim dx = CInt(w * (depth - task.minDepth) / range)
                             If dx < w And dx > 0 Then dst2.Set(Of cv.Vec3b)(y, dx, black)
                         End If
                     Next
                 Next
             End Sub)
        label1 = "Top View (looking down)"
        label2 = "Side View"
    End Sub
    Public Sub Close()
        SimpleProjectionClose(cPtr)
    End Sub
End Class






Public Class PointCloud_Kalman_TopView
    Inherits VBparent
    Public pTrack As KNN_PointTracker
    Public flood As FloodFill_Palette
    Public topView As Histogram_TopView2D
    Public Sub New()
        initParent()

        pTrack = New KNN_PointTracker
        flood = New FloodFill_Palette
        Dim minFloodSlider = findSlider("FloodFill Minimum Size")
        minFloodSlider.Value = 100
        topView = New Histogram_TopView2D

        task.desc = "Measure each object found in a Centroids view and provide pixel width as well"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me

        topView.Run(src)

        flood.Run(topView.histOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255))

        If flood.dst1.Channels = 3 Then src = flood.dst1 Else src = flood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        pTrack.queryPoints = New List(Of cv.Point2f)(flood.basics.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.basics.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.basics.masks)
        pTrack.Run(src)
        dst1 = pTrack.dst1

        If standalone Or task.intermediateReview = caller Then
            topView.cmat.Run(dst1)
            dst1 = topView.cmat.dst1
        End If
        Dim FOV = task.hFov
        label1 = Format(task.pixelsPerMeter, "0") + " pixels per meter with maxZ at " + Format(task.maxZ, "0.0") + " meters"
    End Sub
End Class






Public Class PointCloud_Kalman_SideView
    Inherits VBparent
    Public flood As Floodfill_Identifiers
    Public sideView As Histogram_SideView2D
    Public pTrack As KNN_PointTracker
    Public cmat As PointCloud_ColorizeSide
    Public Sub New()
        initParent()


        pTrack = New KNN_PointTracker
        cmat = New PointCloud_ColorizeSide
        flood = New Floodfill_Identifiers

        Dim minFloodSlider = findSlider("FloodFill Minimum Size")
        minFloodSlider.Value = 100
        sideView = New Histogram_SideView2D()

        task.desc = "Measure each object found in a Centroids view and provide pixel width as well"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        sideView.Run(src)

        flood.Run(sideView.histOutput.ConvertScaleAbs(255))

        pTrack.queryPoints = New List(Of cv.Point2f)(flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.masks)
        pTrack.Run(flood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
        dst1 = pTrack.dst1

        If standalone Or task.intermediateReview = caller Then
            cmat.Run(dst1)
            dst1 = cmat.dst1
        End If

        Dim FOV = (180 - task.vFov) / 2
        label1 = Format(task.pixelsPerMeter, "0") + " pixels per meter at " + Format(task.maxZ, "0.0") + " meters"
    End Sub
End Class










Public Class PointCloud_BackProject
    Inherits VBparent
    Dim both As PointCloud_BothViews
    Dim mats As Mat_4to1
    Public Sub New()
        initParent()

        both = New PointCloud_BothViews()
        mats = New Mat_4to1()
        label1 = "Click any quadrant below to enlarge it"
        label2 = "Click any centroid to display details"
        task.desc = "Backproject the selected object"
		' task.rank = 1
    End Sub

    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        If task.mouseClickFlag Then
            ' lower left image is the mat_4to1
            If task.mousePicTag = 2 Then
                If task.mouseClickFlag Then setMyActiveMat()
                task.mouseClickFlag = False ' absorb the mouse click here only
            End If
        End If
        both.Run(src)

        mats.mat(0) = both.dst1
        mats.mat(1) = both.dst2
        mats.mat(2) = both.backMat
        mats.mat(3) = both.backMatMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        mats.Run(src)
        dst1 = mats.dst1
        dst2 = mats.mat(quadrantIndex)
        label2 = both.detailText
    End Sub
End Class








Public Class PointCloud_FrustrumTop
    Inherits VBparent
    Dim frustrum As Draw_Frustrum
    Dim topView As Histogram_TopView2D
    Dim cmat As PointCloud_ColorizeTop
    Public Sub New()
        initParent()

        cmat = New PointCloud_ColorizeTop
        frustrum = New Draw_Frustrum
        topView = New Histogram_TopView2D

        task.hist3DThreshold = 0

        Dim xCheckbox = findCheckBox("Rotate pointcloud around X-axis using gravity vector angleZ")
        Dim zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX")
        xCheckbox.Checked = False
        zCheckbox.Checked = False

        label2 = "Draw_Frustrum output"
        task.desc = "Translate only the frustrum with gravity"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me

        frustrum.Run(src)
        topView.Run(frustrum.dst2)

        cmat.Run(topView.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
        dst1 = cmat.dst1
    End Sub
End Class








Public Class PointCloud_FrustrumSide
    Inherits VBparent
    Dim frustrum As Draw_Frustrum
    Dim sideView As Histogram_SideView2D
    Dim cmat As PointCloud_ColorizeSide
    Public Sub New()
        initParent()

        cmat = New PointCloud_ColorizeSide
        frustrum = New Draw_Frustrum
        sideView = New Histogram_SideView2D

        task.hist3DThreshold = 0

        Dim xCheckbox = findCheckBox("Rotate pointcloud around X-axis using gravity vector angleZ")
        Dim zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX")
        xCheckbox.Checked = False
        zCheckbox.Checked = False

        label2 = "Draw_Frustrum output"
        task.desc = "Translate only the frustrum with gravity"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me

        frustrum.Run(src)
        sideView.Run(frustrum.dst2)

        cmat.Run(sideView.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
        dst1 = cmat.dst1
    End Sub
End Class








Public Class PointCloud_Singletons
    Inherits VBparent
    Public topView As Histogram_TopView2D
    Public Sub New()
        initParent()
        topView = New Histogram_TopView2D()
        topView.resizeHistOutput = False
        task.hist3DThreshold = 1

        label1 = "Top down view before inrange sampling"
        label2 = "Histogram after filtering for single-only histogram bins"
        task.desc = "Find floor and ceiling using gravity aligned top-down view and selecting bins with exactly 1 sample"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        topView.Run(src)
        dst1 = topView.dst1

        minVal = 1
        maxVal = 2
        cv.Cv2.InRange(topView.originalHistOutput, minVal, maxVal, dst2)
        Dim mask = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
    End Sub
End Class









Public Class PointCloud_ReducedSideView
    Inherits VBparent
    Dim gCloud As Depth_PointCloud_IMU
    Dim reduction As Reduction_Basics
    Dim histOutput As New cv.Mat
    Public Sub New()
        initParent()
        gCloud = New Depth_PointCloud_IMU
        reduction = New Reduction_Basics
        task.desc = "Create a stable side view of the point cloud"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me

        gCloud.Run(src)

        Dim split = gCloud.dst1.Split()
        src = split(2) * 1000
        src.ConvertTo(src, cv.MatType.CV_32S)
        reduction.Run(src)
        reduction.dst1.ConvertTo(split(2), cv.MatType.CV_32F)
        split(2) *= 0.001
        cv.Cv2.Merge(split, dst2)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.sideFrustrumAdjust, task.sideFrustrumAdjust), New cv.Rangef(0, task.maxZ)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {dst2}, New Integer() {1, 2}, New cv.Mat, histOutput, 2, histSize, ranges)

        histOutput = histOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary)
        histOutput.ConvertTo(dst1, cv.MatType.CV_8UC1)
    End Sub
End Class







Public Class PointCloud_ReducedTopView
    Inherits VBparent
    Dim gCloud As Depth_PointCloud_IMU
    Dim reduction As Reduction_Basics
    Dim histOutput As New cv.Mat
    Public Sub New()
        initParent()
        gCloud = New Depth_PointCloud_IMU
        reduction = New Reduction_Basics
        task.desc = "Create a stable side view of the point cloud"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me

        gCloud.Run(src)

        Dim split = gCloud.dst1.Split()
        src = split(2) * 1000
        src.ConvertTo(src, cv.MatType.CV_32S)
        reduction.Run(src)
        reduction.dst1.ConvertTo(split(2), cv.MatType.CV_32F)
        split(2) *= 0.001
        cv.Cv2.Merge(split, dst2)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, task.maxZ), New cv.Rangef(-task.topFrustrumAdjust, task.topFrustrumAdjust)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {dst2}, New Integer() {2, 0}, New cv.Mat, histOutput, 2, histSize, ranges)

        histOutput = histOutput.Flip(cv.FlipMode.X)
        dst1 = histOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst1, cv.MatType.CV_8UC1)
    End Sub
End Class







Public Class PointCloud_ObjectsTop
    Inherits VBparent
    Public measureTop As PointCloud_Kalman_TopView
    Public viewObjects As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Dim cmat As PointCloud_ColorizeTop
    Public colorizeNeeded As Boolean
    Public Sub New()
        initParent()

        cmat = New PointCloud_ColorizeTop
        measureTop = New PointCloud_Kalman_TopView

        If standalone Then
            If findfrm(caller + " Slider Options") Is Nothing Then
                sliders.Setup(caller, 1)
                sliders.setupTrackBar(0, "Test Bar Distance from camera in mm", 1, 4000, 1500)
            End If
        End If
        task.desc = "Validate the formula for pixel height as a function of distance"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static showRectanglesCheck = findCheckBox("Draw rectangle and centroid for each mask")
        Dim drawLines = showRectanglesCheck.checked
        measureTop.Run(src)
        dst1 = measureTop.dst1

        task.pixelsPerMeter = dst1.Height / task.maxZ
        label1 = "Pixels/Meter horizontal: " + CStr(CInt(dst1.Width / task.maxZ)) + " vertical: " + CStr(CInt(task.pixelsPerMeter))

        Dim FOV = task.hFov / 2

        Dim xpt1 As cv.Point2f, xpt2 As cv.Point2f
        If standalone Or task.intermediateReview = caller Then
            Static distanceSlider = findSlider("Test Bar Distance from camera in mm")
            Dim pixeldistance = src.Height * (distanceSlider.Value / 1000) / task.maxZ
            Dim lineHalf = CInt(Math.Tan(FOV * 0.0174533) * pixeldistance)

            xpt1 = New cv.Point2f(task.topCameraPoint.X - lineHalf, src.Height - pixeldistance)
            xpt2 = New cv.Point2f(task.topCameraPoint.X + lineHalf, src.Height - pixeldistance)
            If drawLines Then dst1.Line(xpt1, xpt2, cv.Scalar.Blue, 3)
            Dim lineWidth = xpt2.X - xpt1.X
            Dim blueLineMeters As Single
            If lineWidth = 0 Then
                lineWidth = 1
                blueLineMeters = 0
            Else
                blueLineMeters = distanceSlider.Value * lineWidth / (1000 * pixeldistance)
            End If
            task.trueText("Blue Line is " + CStr(pixeldistance) + " pixels from the camera" + vbCrLf +
                          "Blue Line is " + CStr(lineWidth) + " pixels long" + vbCrLf +
                          "Blue Line is " + Format(distanceSlider.Value / 1000, "#0.00") + " meters from the camera" + vbCrLf +
                          "Blue Line is " + Format(blueLineMeters, "#0.00") + " meters long" + vbCrLf +
                          "At the Blue Line there are " + Format(1000 * blueLineMeters / lineWidth, "#0.00") + " mm per pixel " +
                          "in this projection", 10, 60, 3)
        End If

        viewObjects.Clear()
        For i = 0 To measureTop.pTrack.drawRC.viewObjects.Count - 1
            Dim r = measureTop.pTrack.drawRC.viewObjects.Values(i).rectInHist

            Dim lineHalf = CInt(Math.Tan(FOV * 0.0174533) * (src.Height - (r.Y + r.Height)))
            Dim pixeldistance = src.Height - r.Y - r.Height
            xpt1 = New cv.Point2f(task.topCameraPoint.X - lineHalf, src.Height - pixeldistance)
            xpt2 = New cv.Point2f(task.topCameraPoint.X + lineHalf, src.Height - pixeldistance)
            Dim coneleft = Math.Max(Math.Max(xpt1.X, r.X), task.topCameraPoint.X - lineHalf)
            Dim coneRight = Math.Min(Math.Min(xpt2.X, r.X + r.Width), task.topCameraPoint.X + lineHalf)

            Dim coneWidth = dst1.Width / (Math.Max(xpt2.X, dst1.Width) - Math.Max(xpt1.X, 0))
            Dim drawPt1 = New cv.Point2f(coneleft, r.Y + r.Height)
            Dim drawpt2 = New cv.Point2f(coneRight, r.Y + r.Height)

            If lineHalf = 0 Then Continue For
            If drawLines Then dst1.Line(drawPt1, drawpt2, cv.Scalar.Yellow, 3)

            Dim vo = measureTop.pTrack.drawRC.viewObjects.Values(i)
            Dim addlen As Single = 0
            ' need to add a small amount to the object width in pixels based on the angle to the camera of the back edge
            If Not (task.topCameraPoint.X > r.X And task.topCameraPoint.X < r.X + r.Width) Then
                If r.X > task.topCameraPoint.X Then
                    addlen = r.Height * Math.Abs(r.X - task.topCameraPoint.X) / (src.Height - r.Y)
                    If drawLines Then dst1.Line(New cv.Point2f(r.X, r.Y + r.Height), New cv.Point2f(r.X - addlen, r.Y + r.Height), cv.Scalar.Yellow, 3)
                    coneleft -= addlen
                Else
                    addlen = r.Height * (task.topCameraPoint.X - (r.X + r.Width)) / (src.Height - r.Y)
                    If drawLines Then dst1.Line(New cv.Point2f(r.X + r.Width, r.Y + r.Height), New cv.Point2f(r.X + r.Width + addlen, r.Y + r.Height), cv.Scalar.Yellow, 3)
                    If coneleft - addlen >= xpt1.X Then coneleft -= addlen
                End If
            End If
            Dim newX = (coneleft - xpt1.X) * src.Width / (lineHalf * 2)
            Dim newWidth = src.Width * (addlen + coneRight - coneleft) / (lineHalf * 2)
            vo.rectFront = New cv.Rect(newX, r.Y, newWidth, r.Height)
            viewObjects.Add(vo.rectFront.Width * vo.rectFront.Height, vo)
        Next
        If standalone Or task.intermediateReview = caller Or colorizeNeeded Then
            cmat.Run(dst1)
            dst1 = cmat.dst1
        End If
    End Sub
End Class













Public Class PointCloud_ObjectsSide
    Inherits VBparent
    Public measureSide As PointCloud_Kalman_SideView
    Public viewObjects As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Dim cmat As PointCloud_ColorizeSide
    Public Sub New()
        initParent()

        cmat = New PointCloud_ColorizeSide
        measureSide = New PointCloud_Kalman_SideView

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Test Bar Distance from camera in mm", 1, 4000, 1500)
        End If
        task.desc = "Validate the formula for pixel height as a function of distance"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static showRectanglesCheck = findCheckBox("Draw rectangle and centroid for each mask")
        Dim drawLines = showRectanglesCheck.checked
        measureSide.Run(src)
        dst1 = measureSide.dst1

        label1 = "Pixels/Meter horizontal: " + CStr(CInt(dst1.Width / task.maxZ)) + " vertical: " + CStr(CInt(task.pixelsPerMeter))
        Dim FOV = task.vFov / 2

        Dim xpt1 As cv.Point2f, xpt2 As cv.Point2f
        If standalone Or task.intermediateReview = caller Then
            Static distanceSlider = findSlider("Test Bar Distance from camera in mm")
            Dim pixeldistance = src.Width * (distanceSlider.Value / 1000) / task.maxZ
            Dim lineHalf = CInt(Math.Tan(FOV * 0.0174533) * pixeldistance)

            xpt1 = New cv.Point2f(task.sideCameraPoint.X + pixeldistance, task.sideCameraPoint.Y - lineHalf)
            xpt2 = New cv.Point2f(task.sideCameraPoint.X + pixeldistance, task.sideCameraPoint.Y + lineHalf)
            If drawLines Then dst1.Line(xpt1, xpt2, cv.Scalar.Blue, 3)
            Dim lineWidth = xpt2.Y - xpt1.Y
            Dim blueLineMeters As Single
            If lineWidth = 0 Then
                lineWidth = 1
                blueLineMeters = 0
            Else
                blueLineMeters = distanceSlider.Value * lineWidth / (1000 * pixeldistance)
            End If
            task.trueText("Blue Line is " + CStr(pixeldistance) + " pixels from the camera" + vbCrLf +
                          "Blue Line is " + CStr(lineWidth) + " pixels long" + vbCrLf +
                          "Blue Line is " + Format(distanceSlider.Value / 1000, "#0.00") + " meters from the camera" + vbCrLf +
                          "Blue Line is " + Format(blueLineMeters, "#0.00") + " meters long" + vbCrLf +
                          "At the Blue Line there are " + Format(1000 * blueLineMeters / lineWidth, "#0.00") + " mm per pixel " +
                          "in this projection", 10, 60, 3)
        End If

        viewObjects.Clear()
        For i = 0 To measureSide.pTrack.drawRC.viewObjects.Count - 1
            Dim r = measureSide.pTrack.drawRC.viewObjects.Values(i).rectInHist
            Dim lineHalf = CInt(Math.Tan(FOV * 0.0174533) * (src.Height - (r.Y + r.Height)))
            Dim pixeldistance = src.Height - r.Y - r.Height
            xpt1 = New cv.Point2f(task.topCameraPoint.X - lineHalf, src.Height - pixeldistance)
            xpt2 = New cv.Point2f(task.topCameraPoint.X + lineHalf, src.Height - pixeldistance)
            Dim coneleft = Math.Max(Math.Max(xpt1.X, r.X), task.topCameraPoint.X - lineHalf)
            Dim coneRight = Math.Min(Math.Min(xpt2.X, r.X + r.Width), task.topCameraPoint.X + lineHalf)

            Dim coneWidth = dst1.Width / (Math.Max(xpt2.X, dst1.Width) - Math.Max(xpt1.X, 0))
            Dim drawPt1 = New cv.Point2f(coneleft, r.Y + r.Height)
            Dim drawpt2 = New cv.Point2f(coneRight, r.Y + r.Height)

            lineHalf = CInt(Math.Tan(FOV * 0.0174533) * (r.X - task.sideCameraPoint.X))
            pixeldistance = r.X - task.sideCameraPoint.X
            xpt1 = New cv.Point2f(task.sideCameraPoint.X + pixeldistance, task.sideCameraPoint.Y - lineHalf)
            xpt2 = New cv.Point2f(task.sideCameraPoint.X + pixeldistance, task.sideCameraPoint.Y + lineHalf)

            coneleft = Math.Max(Math.Max(xpt1.Y, r.Y), task.sideCameraPoint.Y - lineHalf)
            coneRight = Math.Min(Math.Min(xpt2.Y, r.Y + r.Height), task.sideCameraPoint.Y + lineHalf)
            drawPt1 = New cv.Point2f(r.X, coneleft)
            drawpt2 = New cv.Point2f(r.X, coneRight)

            If lineHalf = 0 Then Continue For
            If drawLines Then dst1.Line(drawPt1, drawpt2, cv.Scalar.Yellow, 3)

            Dim vo = measureSide.pTrack.drawRC.viewObjects.Values(i)
            Dim addlen As Single = 0
            ' need to add a small amount to the object width in pixels based on the angle to the camera of the back edge
            If Not (task.sideCameraPoint.Y > r.Y And task.sideCameraPoint.Y < r.Y + r.Height) Then
                If r.Y > task.sideCameraPoint.Y Then
                    addlen = r.Width * (r.Y - task.sideCameraPoint.Y) / (r.X + r.Width - task.sideCameraPoint.X)
                    If drawLines Then dst1.Line(New cv.Point2f(r.X, r.Y), New cv.Point2f(r.X, r.Y - addlen), cv.Scalar.Yellow, 3)
                    r = New cv.Rect(r.X, r.Y - addlen, r.Width, coneRight - coneleft - addlen)
                    If coneRight - addlen >= xpt2.Y Then coneRight -= addlen
                Else
                    addlen = r.Width * (task.sideCameraPoint.Y - r.Y) / (r.X + r.Width - task.sideCameraPoint.X)
                    If drawLines Then dst1.Line(New cv.Point2f(r.X, r.Y + r.Height), New cv.Point2f(r.X, r.Y + r.Height + addlen), cv.Scalar.Yellow, 3)
                    r = New cv.Rect(r.X, r.Y + addlen, r.Width, coneRight - coneleft + addlen)
                    coneleft += addlen
                End If
            End If
            Dim newY = (coneleft - xpt1.Y) * src.Height / (lineHalf * 2)
            Dim newHeight = src.Height * (addlen + coneRight - coneleft) / (lineHalf * 2)
            vo.rectFront = New cv.Rect(r.X, newY, r.Width, newHeight)
            viewObjects.Add(vo.rectFront.Width * vo.rectFront.Height, vo)
        Next
        If standalone Or task.intermediateReview = caller Then
            cmat.Run(dst1)
            dst1 = cmat.dst1
        End If
    End Sub
End Class








Public Class PointCloud_BothViews
    Inherits VBparent
    Public topPixel As PointCloud_ObjectsTop
    Public sidePixel As PointCloud_ObjectsSide
    Dim levelCheck As IMU_isCameraLevel
    Public detailText As String
    Public backMat As New cv.Mat
    Public backMatMask As New cv.Mat
    Public vwTop As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Public vwSide As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Dim cmatSide As PointCloud_ColorizeSide
    Dim cmatTop As PointCloud_ColorizeTop
    Public Sub New()
        initParent()

        levelCheck = New IMU_isCameraLevel
        topPixel = New PointCloud_ObjectsTop
        sidePixel = New PointCloud_ObjectsSide
        cmatSide = New PointCloud_ColorizeSide
        cmatTop = New PointCloud_ColorizeTop

        backMat = New cv.Mat(dst1.Size(), cv.MatType.CV_8UC3)
        backMatMask = New cv.Mat(dst1.Size(), cv.MatType.CV_8UC1)

        task.desc = "Find the actual width in pixels for the objects detected in the top view"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static showRectanglesCheck = findCheckBox("Draw rectangle and centroid for each mask")
        Dim showDetails = showRectanglesCheck.checked

        topPixel.Run(src)
        sidePixel.Run(src)

        If standalone Or task.intermediateReview = caller Then
            Dim instructions = "Click any centroid to get details"
            Dim accMsg1 = "TopView - distances are accurate"
            Dim accMsg2 = "SideView - distances are accurate"
            Static xRotateSlider = findSlider("Amount to rotate pointcloud around X-axis (degrees)")
            Static zRotateSlider = findSlider("Amount to rotate pointcloud around Z-axis (degrees)")
            If xRotateSlider.Value <> 0 Or zRotateSlider.Value <> 0 Then
                levelCheck.Run(src)
                If levelCheck.cameraLevel Then
                    accMsg1 = "Distances are good - camera is level"
                    accMsg2 = "Distances are good - camera is level"
                Else
                    accMsg1 = "Camera NOT level - distances approximate"
                    accMsg2 = "Camera NOT level - distances approximate"
                End If
            End If

            Dim pad = CInt(src.Width / 15)
            task.trueText(accMsg1 + vbCrLf + instructions, 10, src.Height - pad)
            task.trueText(accMsg2 + vbCrLf + instructions, 10, src.Height - pad, 3)
        End If

        Static minDepth As Single, maxDepth As Single
        vwTop = topPixel.viewObjects
        vwSide = sidePixel.viewObjects
        Dim roi = New cv.Rect(0, 0, dst1.Width, dst1.Height)
        Dim minIndex As Integer
        Dim detailPoint As cv.Point
        Dim vw As New SortedList(Of Single, viewObject)
        Dim topActive = If(standalone, True, (quadrantIndex = QUAD0 Or quadrantIndex = QUAD2))
        Dim sideActive = If(standalone, True, (quadrantIndex = QUAD1 Or quadrantIndex = QUAD3))

        Dim widthInfo As String = ""
        If vwTop.Count And topActive Then
            minIndex = findNearestPoint(task.mouseClickPoint, vwTop)
            Dim rView = vwTop.Values(minIndex).rectInHist
            detailPoint = New cv.Point(CInt(rView.X), CInt(rView.Y))
            Dim rFront = vwTop.Values(minIndex).rectFront

            minDepth = task.maxZ * (task.topCameraPoint.Y - rView.Y - rView.Height) / src.Height
            maxDepth = task.maxZ * (task.topCameraPoint.Y - rView.Y) / src.Height
            widthInfo = " & " + CStr(rView.Width) + " pixels wide or " + Format(rView.Width / task.pixelsPerMeter, "0.0") + "m"
            detailText = Format(minDepth, "#0.0") + "-" + Format(maxDepth, "#0.0") + "m" + widthInfo

            roi = New cv.Rect(rFront.X, 0, rFront.Width, src.Height)
            vw = vwTop
            If showDetails Then
                task.trueText(detailText, detailPoint.X, detailPoint.Y, picTag:=If(standalone, 2, 3))
                If standalone Or task.intermediateReview = caller Then label1 = "Clicked: " + detailText Else label2 = "Clicked: " + detailText
            End If
        End If

        If vwSide.Count And sideActive Then
            minIndex = findNearestPoint(task.mouseClickPoint, vwSide)
            Dim rView = vwSide.Values(minIndex).rectInHist
            detailPoint = New cv.Point(CInt(rView.X), CInt(rView.Y - 15))
            Dim rFront = vwSide.Values(minIndex).rectFront
            minDepth = task.maxZ * (rView.X - task.sideCameraPoint.X) / src.Height
            maxDepth = task.maxZ * (rView.X + rView.Width - task.sideCameraPoint.X) / src.Height

            widthInfo = " & " + CStr(rView.Width) + " pixels wide or " + Format(rView.Height / task.pixelsPerMeter, "0.0") + "m"
            detailText = Format(minDepth, "#0.0") + "-" + Format(maxDepth, "#0.0") + "m " + widthInfo

            roi = New cv.Rect(0, rFront.Y, src.Width, rFront.Y + rFront.Height)
            vw = vwSide
            If showDetails Then
                task.trueText(detailText, detailPoint.X, detailPoint.Y, 3)
                label2 = "Clicked: " + detailText
            End If
        End If

        If vw.Count > 0 Then
            If roi.X + roi.Width > task.depth32f.Width Then roi.Width = task.depth32f.Width - roi.X
            If roi.Y + roi.Height > task.depth32f.Height Then roi.Height = task.depth32f.Height - roi.Y
            If roi.Width > 0 And roi.Height > 0 Then
                backMatMask.SetTo(0)
                cv.Cv2.InRange(task.depth32f(roi), cv.Scalar.All(minDepth * 1000), cv.Scalar.All(maxDepth * 1000), backMatMask(roi))

                backMat.SetTo(0)
                backMat(roi).SetTo(vw.Values(minIndex).LayoutColor, backMatMask(roi))
            End If
        End If

        cmatSide.Run(sidePixel.dst1)
        dst1 = cmatSide.dst1

        cmatTop.Run(topPixel.dst1)
        dst2 = cmatTop.dst1
    End Sub
End Class








Public Class PointCloud_BackProjectTopView
    Inherits VBparent
    Dim view As PointCloud_ObjectsTop
    Public Sub New()
        initParent()
        view = New PointCloud_ObjectsTop
        view.colorizeNeeded = True

        label1 = "Back projection of objects identified in the top view"
        label2 = "Objects identified in the top view"
        task.desc = "Display only the top view of the depth data and backproject the histogram onto the RGB image."
        ' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        view.Run(src)
        dst2 = view.dst1

        Dim rectList = New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingleInverted)
        For Each obj In view.viewObjects
            rectList.Add(obj.Value.rectInHist.Y + obj.Value.rectInHist.Height, obj.Value.rectInHist)
        Next
        If rectList.Count > 0 Then
            Dim colorBump = CInt(255 / rectList.Count)

            Dim split = view.measureTop.topView.gCloud.dst1.Split()
            Dim colorMask = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            dst1 = src
            For i = 0 To rectList.Count - 1
                Dim r = rectList.ElementAt(i).Value
                If r.Width > 0 And r.Height > 0 Then
                    Dim minDepth = task.maxZ - task.maxZ * (r.Y + r.Height) / dst2.Height
                    Dim maxDepth = task.maxZ - task.maxZ * r.Y / dst2.Height

                    Dim minWidth = task.maxZ * r.X / dst2.Width - task.sideFrustrumAdjust
                    Dim maxWidth = task.maxZ * (r.X + r.Width) / dst2.Width - task.sideFrustrumAdjust

                    Dim mask32f = New cv.Mat

                    cv.Cv2.InRange(split(2), minDepth, maxDepth, mask32f)
                    Dim mask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)

                    cv.Cv2.InRange(split(0), minWidth, maxWidth, mask32f)
                    Dim hMask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)
                    cv.Cv2.BitwiseAnd(mask, hMask, mask)

                    colorMask.SetTo((i * colorBump) Mod 255, mask)
                End If
            Next
            task.palette.Run(colorMask)
            dst1 = task.palette.dst1
        Else
            task.trueText("No objects found")
        End If
    End Sub
End Class








Public Class PointCloud_BackProjectSideView
    Inherits VBparent
    Dim view As PointCloud_ObjectsSide
    Dim cmatSide As PointCloud_ColorizeSide
    Public Sub New()
        initParent()

        view = New PointCloud_ObjectsSide
        cmatSide = New PointCloud_ColorizeSide
        task.desc = "Display only the side view of the depth data - with and without the IMU active"
        ' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        view.Run(src)
        cmatSide.Run(view.dst1)
        dst2 = cmatSide.dst1

        Dim rectList = New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingleInverted)
        For Each obj In view.viewObjects
            rectList.Add(obj.Value.rectInHist.Y + obj.Value.rectInHist.Height, obj.Value.rectInHist)
        Next

        If rectList.Count > 0 Then
            Dim colorBump = CInt(255 / rectList.Count)

            Dim split = view.measureSide.sideView.gCloud.dst1.Split()
            Dim colorMask = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            dst1 = src
            For i = 0 To rectList.Count - 1
                Dim r = rectList.ElementAt(i).Value
                If r.Width > 0 And r.Height > 0 Then
                    Dim minDepth = task.maxZ * r.X / dst2.Width
                    Dim maxDepth = task.maxZ * (r.X + r.Width) / dst2.Width

                    Dim minHeight = task.maxZ - task.maxZ * (r.Y + r.Height) / dst2.Height - task.sideFrustrumAdjust
                    Dim maxHeight = task.maxZ - task.maxZ * r.Y / dst2.Height - task.sideFrustrumAdjust

                    Dim mask32f = New cv.Mat

                    cv.Cv2.InRange(split(2), minDepth, maxDepth, mask32f)
                    Dim mask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)

                    cv.Cv2.InRange(split(1), minHeight, maxHeight, mask32f)
                    Dim hMask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)
                    cv.Cv2.BitwiseAnd(mask, hMask, mask)

                    colorMask.SetTo((i * colorBump) Mod 255, mask)
                End If
            Next
            task.palette.Run(colorMask)
            dst1 = task.palette.dst1
        Else
            task.trueText("No objects found")
        End If
    End Sub
End Class

