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
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
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
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
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

        dst1 = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)

        task.desc = "Inspect x, y, and z values in a row or column"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static yLineSlider = findSlider("Y-Direction intervals")
        Static cLineSlider = findSlider("Inspection Line")
        Dim yLines = yLineSlider.value
        Dim cLine = cLineSlider.value

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.depth32f

        Dim topPt = New cv.Point2f(cLine, 0)
        Dim botPt = New cv.Point2f(cLine, dst1.Height)
        dst1 = task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1.Line(topPt, botPt, 255, 3, cv.LineTypes.AntiAlias)

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

        dst1 = New cv.Mat(src.Size, cv.MatType.CV_8U)
        dst2 = New cv.Mat(src.Size, cv.MatType.CV_8U)
        task.desc = "Show where the pointcloud is continuous"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
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
    Dim palette As Palette_Gradient
    Dim arcSize As Integer
    Public xCheckbox As Windows.Forms.CheckBox
    Public zCheckbox As Windows.Forms.CheckBox
    Public Sub New()
        initParent()

        palette = New Palette_Gradient()
        palette.color1 = cv.Scalar.Yellow
        palette.color2 = cv.Scalar.Blue
        palette.frameModulo = 1
        arcSize = src.Width / 15

        palette.Run()
        dst1 = palette.dst1

        label1 = "Colorize mask for side view"
        task.desc = "Create the colorized mat used for side projections"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone Then src = dst1 Else dst1 = src

        Dim distanceRatio As Single = 1
        Dim fsize = task.fontSize * 1.5

        dst1.Circle(task.sideCameraPoint, task.dotSize, cv.Scalar.BlueViolet, -1, cv.LineTypes.AntiAlias)
        For i = 1 To task.maxZ
            Dim xmeter = CInt(dst1.Width * i / task.maxZ * distanceRatio)
            dst1.Line(New cv.Point(xmeter, 0), New cv.Point(xmeter, dst1.Height), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst1, CStr(i) + "m", New cv.Point(xmeter - src.Width / 15, dst1.Height - 10), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next

        Dim cam = task.sideCameraPoint
        Dim marker As New cv.Point2f(dst1.Width / (task.maxZ * distanceRatio), 0)
        marker.Y = marker.X * Math.Tan((ocvb.vFov / 2) * cv.Cv2.PI / 180)
        Dim topLen = marker.X * Math.Tan((ocvb.hFov / 2) * cv.Cv2.PI / 180)
        Dim markerLeft = New cv.Point(marker.X, cam.Y - marker.Y)
        Dim markerRight = New cv.Point(marker.X, cam.Y + marker.Y)

        If task.xRotateSlider.Value <> 0 Then
            Dim offset = Math.Sin(ocvb.angleX) * marker.Y
            If ocvb.angleX > 0 Then
                markerLeft.Y = markerLeft.Y - offset
                markerRight.Y = markerRight.Y + offset
            Else
                markerLeft.Y = markerLeft.Y + offset
                markerRight.Y = markerRight.Y - offset
            End If
        End If

        If task.zRotateSlider.Value <> 0 Then
            markerLeft = New cv.Point(markerLeft.X - cam.X, markerLeft.Y - cam.Y) ' Change the origin
            markerLeft = New cv.Point(markerLeft.X * Math.Cos(ocvb.angleZ) - markerLeft.Y * Math.Sin(ocvb.angleZ), ' rotate around x-axis using angleZ
                                      markerLeft.Y * Math.Cos(ocvb.angleZ) + markerLeft.X * Math.Sin(ocvb.angleZ))
            markerLeft = New cv.Point(markerLeft.X + cam.X, markerLeft.Y + cam.Y) ' Move the origin to the side camera location.

            ' Same as above for markerLeft but consolidated algebraically.
            markerRight = New cv.Point((markerRight.X - cam.X) * Math.Cos(ocvb.angleZ) - (markerRight.Y - cam.Y) * Math.Sin(ocvb.angleZ) + cam.X,
                                       (markerRight.Y - cam.Y) * Math.Cos(ocvb.angleZ) + (markerRight.X - cam.X) * Math.Sin(ocvb.angleZ) + cam.Y)
        End If

        If standalone = False Then
            dst1.Circle(markerLeft, task.dotSize, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            dst1.Circle(markerRight, task.dotSize, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        End If

        ' draw the arc enclosing the camera FOV
        Dim startAngle = (180 - ocvb.vFov) / 2
        Dim y = dst1.Width / Math.Tan(startAngle * cv.Cv2.PI / 180)

        Dim fovTop = New cv.Point(dst1.Width, cam.Y - y)
        Dim fovBot = New cv.Point(dst1.Width, cam.Y + y)

        If standalone = False Then
            dst1.Ellipse(cam, New cv.Size(arcSize, arcSize), -startAngle + 90, startAngle, 0, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
            dst1.Ellipse(cam, New cv.Size(arcSize, arcSize), 90, 180, 180 + startAngle, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
            dst1.Line(cam, fovTop, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            dst1.Line(cam, fovBot, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

            dst1.Line(cam, markerLeft, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)
            dst1.Line(cam, markerRight, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)

            Dim labelLocation = New cv.Point(src.Width * 0.02, src.Height * 7 / 8)
            cv.Cv2.PutText(dst1, "vFOV=" + CStr(180 - startAngle * 2) + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fsize,
                       cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        End If
    End Sub
End Class





' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
' https://www.mynteye.com/pages/mynt-eye-d
Public Class PointCloud_ColorizeTop
    Inherits VBparent
    Dim palette As Palette_Gradient
    Dim arcSize As Integer
    Public xCheckbox As Windows.Forms.CheckBox
    Public Sub New()
        initParent()

        palette = New Palette_Gradient()
        palette.color1 = cv.Scalar.Yellow
        palette.color2 = cv.Scalar.Blue
        palette.frameModulo = 1
        arcSize = src.Width / 15

        palette.Run()
        dst1 = palette.dst1

        label1 = "Colorize mask for top down view"
        task.desc = "Create the colorize the mat for a topdown projections"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone Then src = dst1 Else dst1 = src

        Dim distanceRatio As Single = 1
        Dim fsize = task.fontSize * 1.5
        dst1.Circle(task.topCameraPoint, task.dotSize, cv.Scalar.BlueViolet, -1, cv.LineTypes.AntiAlias)
        For i = 1 To task.maxZ
            Dim ymeter = CInt(dst1.Height - dst1.Height * i / (task.maxZ * distanceRatio))
            dst1.Line(New cv.Point(0, ymeter), New cv.Point(dst1.Width, ymeter), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst1, CStr(i) + "m", New cv.Point(10, ymeter - 10), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next

        Dim cam = task.topCameraPoint
        Dim marker As New cv.Point2f(cam.X, dst1.Height / (task.maxZ * distanceRatio))
        Dim topLen = marker.Y * Math.Tan((ocvb.hFov / 2) * cv.Cv2.PI / 180)
        Dim sideLen = marker.Y * Math.Tan((ocvb.vFov / 2) * cv.Cv2.PI / 180)
        Dim markerLeft = New cv.Point(cam.X - topLen, dst1.Height - marker.Y)
        Dim markerRight = New cv.Point(cam.X + topLen, dst1.Height - marker.Y)

        If task.zRotateSlider.Value <> 0 Then
            Dim offset = Math.Sin(ocvb.angleZ) * topLen
            If ocvb.angleZ > 0 Then
                markerLeft.X = markerLeft.X - offset
                markerRight.X = markerRight.X + offset
            Else
                markerLeft.X = markerLeft.X + offset
                markerRight.X = markerRight.X - offset
            End If
        End If

        ' draw the arc enclosing the camera FOV
        Dim startAngle = (180 - ocvb.hFov) / 2
        Dim x = dst1.Height / Math.Tan(startAngle * cv.Cv2.PI / 180)

        Dim fovRight = New cv.Point(task.topCameraPoint.X + x, 0)
        Dim fovLeft = New cv.Point(task.topCameraPoint.X - x, fovRight.Y)

        If standalone = False Then
            dst1.Ellipse(task.topCameraPoint, New cv.Size(arcSize, arcSize), -startAngle, startAngle, 0, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
            dst1.Ellipse(task.topCameraPoint, New cv.Size(arcSize, arcSize), 0, 180, 180 + startAngle, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
            dst1.Line(task.topCameraPoint, fovLeft, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

            dst1.Circle(markerLeft, task.dotSize, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            dst1.Circle(markerRight, task.dotSize, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            dst1.Line(cam, markerLeft, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)
            dst1.Line(cam, markerRight, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)

            Dim shift = (src.Width - src.Height) / 2
            Dim labelLocation = New cv.Point(dst1.Width / 2 + shift, dst1.Height * 15 / 16)
            cv.Cv2.PutText(dst1, "hFOV=" + CStr(180 - startAngle * 2) + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            dst1.Line(task.topCameraPoint, fovRight, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
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

        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        grid.Run()

        Dim h = src.Height
        Dim w = src.Width
        Dim range = CSng(task.inrange.maxval - task.inrange.minval)
        If depthBytes Is Nothing Then
            ReDim depthBytes(task.depth32f.Total * task.depth32f.ElemSize - 1)
        End If

        Marshal.Copy(task.depth32f.Data, depthBytes, 0, depthBytes.Length)
        Dim handleDepth = GCHandle.Alloc(depthBytes, GCHandleType.Pinned)

        Dim imagePtr = SimpleProjectionRun(cPtr, handleDepth.AddrOfPinnedObject, task.inrange.minval, task.inrange.maxval, task.depth32f.Height, task.depth32f.Width)

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

        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        grid.Run()

        Dim h = src.Height
        Dim w = src.Width
        Dim range = CSng(task.inrange.maxval - task.inrange.minval)

        ' this VB.Net version is much slower than the optimized C++ version below.
        dst1 = src.EmptyClone.SetTo(cv.Scalar.White)
        dst2 = dst1.Clone()
        Dim black = New cv.Vec3b(0, 0, 0)
        Parallel.ForEach(grid.roiList,
             Sub(roi)
                 For y = roi.Y To roi.Y + roi.Height - 1
                     For x = roi.X To roi.X + roi.Width - 1
                         Dim m = task.depthmask.Get(Of Byte)(y, x)
                         If m > 0 Then
                             Dim depth = task.depth32f.Get(Of Single)(y, x)
                             Dim dy = CInt(h * (depth - task.inrange.minval) / range)
                             If dy < h And dy > 0 Then dst1.Set(Of cv.Vec3b)(h - dy, x, black)
                             Dim dx = CInt(w * (depth - task.inrange.minval) / range)
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
    Public flood As FloodFill_8bit
    Public topView As Histogram_TopView2D
    Public Sub New()
        initParent()

        pTrack = New KNN_PointTracker
        flood = New FloodFill_8bit
        Dim minFloodSlider = findSlider("FloodFill Minimum Size")
        minFloodSlider.Value = 100
        topView = New Histogram_TopView2D

        task.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        topView.Run()

        flood.src = topView.histOutput.Threshold(task.thresholdSlider.Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        flood.Run()

        If flood.dst1.Channels = 3 Then pTrack.src = flood.dst1 Else pTrack.src = flood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        pTrack.queryPoints = New List(Of cv.Point2f)(flood.basics.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.basics.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.basics.masks)
        pTrack.Run()
        dst1 = pTrack.dst1

        If standalone Or task.intermediateReview = caller Then
            topView.cmat.src = dst1
            topView.cmat.Run()
            dst1 = topView.cmat.dst1
        End If
        Dim FOV = ocvb.hFov
        label1 = Format(ocvb.pixelsPerMeter, "0") + " pixels per meter with maxZ at " + Format(task.maxZ, "0.0") + " meters"
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
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        sideView.Run()

        flood.src = sideView.histOutput.ConvertScaleAbs(255)
        flood.Run()

        pTrack.src = flood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        pTrack.queryPoints = New List(Of cv.Point2f)(flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.masks)
        pTrack.Run()
        dst1 = pTrack.dst1

        If standalone Or task.intermediateReview = caller Then
            cmat.src = dst1
            cmat.Run()
            dst1 = cmat.dst1
        End If

        Dim FOV = (180 - ocvb.vFov) / 2
        label1 = Format(ocvb.pixelsPerMeter, "0") + " pixels per meter at " + Format(task.maxZ, "0.0") + " meters"
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
    End Sub

    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If task.mouseClickFlag Then
            ' lower left image is the mat_4to1
            If task.mousePicTag = 2 Then
                If task.mouseClickFlag Then setMyActiveMat()
                task.mouseClickFlag = False ' absorb the mouse click here only
            End If
        End If
        both.Run()

        mats.mat(0) = both.dst1
        mats.mat(1) = both.dst2
        mats.mat(2) = both.backMat
        mats.mat(3) = both.backMatMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        mats.Run()
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

        task.thresholdSlider.Value = 0

        Dim xCheckbox = findCheckBox("Rotate pointcloud around X-axis using gravity vector angleZ")
        Dim zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX")
        xCheckbox.Checked = False
        zCheckbox.Checked = False

        label2 = "Draw_Frustrum output"
        task.desc = "Translate only the frustrum with gravity"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        frustrum.Run()
        topView.src = frustrum.dst2
        topView.Run()

        cmat.src = topView.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cmat.Run()
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

        task.thresholdSlider.Value = 0

        Dim xCheckbox = findCheckBox("Rotate pointcloud around X-axis using gravity vector angleZ")
        Dim zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX")
        xCheckbox.Checked = False
        zCheckbox.Checked = False

        label2 = "Draw_Frustrum output"
        task.desc = "Translate only the frustrum with gravity"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        frustrum.Run()
        sideView.src = frustrum.dst2
        sideView.Run()

        cmat.src = sideView.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cmat.Run()
        dst1 = cmat.dst1
    End Sub
End Class








'Public Class PointCloud_IMU_TopView
'    Inherits VBparent
'    Public topView As Histogram_TopView2D
'    Public kTopView As PointCloud_Kalman_TopView
'    Public lDetect As LineDetector_Basics
'    Dim angleSlider As System.Windows.Forms.TrackBar
'    Dim cmat As PointCloud_Colorize
'    Public Sub New()
'        initParent()

'        topView = New Histogram_TopView2D()
'        task.thresholdSlider.Value = 20

'        kTopView = New PointCloud_Kalman_TopView()
'        cmat = New PointCloud_Colorize()

'        lDetect = New LineDetector_Basics()
'        lDetect.drawLines = True

'        label1 = "Top view aligned using the IMU gravity vector"
'        label2 = "Top view aligned without using the IMU gravity vector"
'        task.desc = "Present the top view with and without the IMU filter."
'    End Sub
'    Public Sub Run()
'        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

'        Static xCheckbox = findCheckBox("Rotate pointcloud around X-axis using angleZ of the gravity vector")
'        Static zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using angleX of the gravity vector")
'        xCheckbox.checked = True
'        zCheckbox.checked = True

'        topView.Run()
'        lDetect.src = topView.dst1.Resize(src.Size).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
'        lDetect.Run()
'        dst1 = lDetect.dst1

'        If standalone or task.intermediateReview = caller Then
'            xCheckbox.checked = False
'            zCheckbox.checked = False
'            kTopView.Run()
'            dst2 = cmat.CameraLocationBot(kTopView.dst1)
'        End If
'    End Sub
'End Class







'Public Class PointCloud_IMU_SideView
'    Inherits VBparent
'    Public sideView As Histogram_SideView2D
'    Public kSideView As PointCloud_Kalman_SideView
'    Public lDetect As LineDetector_Basics
'    Dim cmat As PointCloud_Colorize
'    Public Sub New()
'        initParent()

'        cmat = New PointCloud_Colorize()
'        lDetect = New LineDetector_Basics()
'        lDetect.drawLines = True

'        kSideView = New PointCloud_Kalman_SideView()
'        sideView = New Histogram_SideView2D()

'        task.thresholdSlider.Value = 20

'        label1 = "side view AFTER align/threshold using gravity vector"
'        If standalone or task.intermediateReview = caller Then label2 = "side view BEFORE align/threshold using gravity vector"
'        task.desc = "Present the side view with and without the IMU filter."
'    End Sub
'    Public Sub Run()
'        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
'        Dim pointcloud = task.pointCloud.Clone

'        Static xCheckbox = findCheckBox("Rotate pointcloud around X-axis using angleZ of the gravity vector")
'        Static zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using angleX of the gravity vector")
'        xCheckbox.checked = True
'        zCheckbox.checked = True
'        cmat.imuXaxis = True
'        cmat.imuZaxis = True

'        sideView.Run()
'        lDetect.src = sideView.dst1.Resize(task.color.Size).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
'        lDetect.Run()
'        dst1 = cmat.CameraLocationSide(lDetect.dst1)

'        If standalone Or task.intermediateReview = caller Then
'            task.pointCloud = pointcloud.Clone
'            xCheckbox.checked = False
'            zCheckbox.checked = False
'            cmat.imuXaxis = False
'            cmat.imuZaxis = False
'            kSideView.Run()
'            dst2 = kSideView.dst1
'        End If
'    End Sub
'End Class







'Public Class PointCloud_DistanceSideClick
'    Inherits VBparent
'    Dim sideIMU As PointCloud_IMU_SideView
'    Dim points As New List(Of cv.Point)
'    Dim clicks As New List(Of cv.Point)
'    Public Sub New()
'        initParent()
'        sideIMU = New PointCloud_IMU_SideView()
'        label1 = "Click anywhere to get distance from camera and x dist"
'        task.desc = "Click to find distance from the camera in the rotated side view"
'    End Sub
'    Public Sub Run()
'        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
'        Static saveMaxZ As Single

'        If task.maxZ <> saveMaxZ Then
'            clicks.Clear()
'            points.Clear()
'            saveMaxZ = task.maxZ
'        End If

'        sideIMU.Run()
'        dst1 = sideIMU.dst1
'        dst2 = sideIMU.dst2

'        If task.mouseClickFlag Then clicks.Add(task.mouseClickPoint)

'        For Each pt In points
'            dst2.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
'        Next
'        For Each pt In clicks
'            dst1.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
'            dst2.Circle(pt, task.dotSize, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
'            Dim side1 = (pt.X - task.sideCameraPoint.X)
'            Dim side2 = (pt.Y - task.sideCameraPoint.Y)
'            Dim cameraDistance = Math.Sqrt(side1 * side1 + side2 * side2) / ocvb.pixelsPerMeter
'            ocvb.trueText(Format(cameraDistance, "#0.00") + "m xdist = " + Format(side1 / ocvb.pixelsPerMeter, "#0.00") + "m", pt, 3)
'        Next

'        dst1.Line(New cv.Point(task.sideCameraPoint.X, 0), New cv.Point(task.sideCameraPoint.X, dst1.Height), cv.Scalar.White, 1)
'    End Sub
'End Class







'Public Class PointCloud_GVectorFloor
'    Inherits VBparent
'    Public floor As PointCloud_GVectorPlane
'    Dim maxDepthSlider As System.Windows.Forms.TrackBar
'    Public Sub New()
'        initParent()

'        floor = New PointCloud_GVectorPlane()
'        floor.floorRun = True
'        task.desc = "Find the floor plane in a side view oriented by gravity vector"
'    End Sub
'    Public Sub Run()
'        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

'        floor.Run()
'        dst1 = floor.dst1
'        dst2 = floor.dst2
'    End Sub
'End Class








'Public Class PointCloud_GVectorCeiling
'    Inherits VBparent
'    Public ceiling As PointCloud_GVectorPlane
'    Public Sub New()
'        initParent()

'        ceiling = New PointCloud_GVectorPlane()
'        ceiling.floorRun = False

'        task.maxRangeSlider.Value = 8000

'        Dim cushionSlider = findSlider("Cushion when estimating the floor or ceiling plane (mm)")
'        cushionSlider.Value = 200

'        task.desc = "Find the floor and ceiling planes in a side view oriented by gravity vector"
'    End Sub
'    Public Sub Run()
'        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

'        ceiling.Run()
'        dst1 = ceiling.dst1
'        dst2 = ceiling.dst2
'    End Sub
'End Class








'Public Class PointCloud_GVectorPlane
'    Inherits VBparent
'    Public gLine As PointCloud_GVectorLine
'    Dim inrange As Depth_InRange
'    Public planeHeight As Integer
'    Public planePoint1 As cv.Point2f
'    Public planePoint2 As cv.Point2f
'    Public floorRun As Boolean = True ' the default is to look for a floor...  Set to False to look for ceiling....
'    Public Sub New()
'        initParent()

'        inrange = New Depth_InRange()
'        gLine = New PointCloud_GVectorLine()

'        If findfrm(caller + " Slider Options") Is Nothing Then
'            sliders.Setup(caller)
'            sliders.setupTrackBar(0, "Cushion when estimating the floor or ceiling plane (mm)", 1, 1000, 100)
'            sliders.setupTrackBar(1, "Y-coordinate consistency check count", 1, 100, 5)
'            sliders.setupTrackBar(2, "Y-coordinate up/down adjustment (mm)", -4000, 4000, 0)
'        End If

'        label1 = "Plane equation input"
'        label2 = "Side view rotated with gravity vector"
'        task.desc = "Find the floor or ceiling plane and translate it back to unrotated coordinates"
'    End Sub
'    Public Sub Run()
'        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
'        Static cushionSlider = findSlider("Cushion when estimating the floor or ceiling plane (mm)")
'        Dim cushion = cushionSlider.value / 1000
'        gLine.floorRun = floorRun
'        gLine.src = task.pointCloud
'        gLine.Run()
'        dst2 = gLine.dst1
'        Dim maskplane = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)

'        Dim leftPoint = gLine.leftPoint
'        Static leftPoints As New List(Of cv.Point2f)
'        If leftPoint.Y = 0 Then leftPoints.Clear() Else leftPoints.Add(leftPoint)

'        Static consistencySlider = findSlider("Y-coordinate consistency check count")
'        If leftPoints.Count > consistencySlider.value Then
'            planeHeight = CInt(ocvb.pixelsPerMeter * cushion)
'            If planeHeight = 0 Then planeHeight = 1
'            Dim cam = task.sideCameraPoint

'            Static adjustmentSlider = findSlider("Y-coordinate up/down adjustment (mm)")
'            Dim adjustPixels = ocvb.pixelsPerMeter * adjustmentSlider.value / 1000

'            planePoint1 = New cv.Point(0, leftPoint.Y + CInt(If(floorRun, planeHeight / 2, -planeHeight / 2) + adjustPixels))
'            planePoint2 = New cv.Point(dst2.Width, planePoint1.Y)

'            dst2.Line(planePoint1, planePoint2, cv.Scalar.Yellow, planeHeight)

'            Dim gPlaneDeltaY = Math.Abs(planePoint2.Y - cam.Y)
'            Dim maxAngle = Math.Atan(dst2.Width / gPlaneDeltaY) * 57.2958
'            Dim minRow = dst2.Height * maxAngle / 180

'            Dim planeY = gPlaneDeltaY / ocvb.pixelsPerMeter * If(floorRun, 1, -1) + adjustmentSlider.value / 1000
'            Dim split = task.pointCloud.Split()
'            Dim ySplit = split(1)
'            inrange.src = split(1)
'            inrange.minVal = planeY - If(floorRun, 0, cushion)
'            inrange.maxVal = planeY + If(floorRun, cushion, 0)
'            inrange.Run()
'            maskplane = split(1).ConvertScaleAbs(255)

'            Dim incr = task.maxZ / split(2).Width
'            Dim tmp = New cv.Mat(split(2).Size, cv.MatType.CV_32F, 0)
'            If floorRun Then
'                For i = minRow To split(2).Rows - 1
'                    tmp.Row(i).SetTo(i * incr)
'                Next
'            Else
'                For i = 0 To minRow - 1
'                    tmp.Row(i).SetTo(i * incr)
'                Next
'            End If
'            tmp.CopyTo(split(2), maskplane)
'        End If
'        dst1 = task.color.Clone
'        dst1.SetTo(cv.Scalar.White, maskplane.Resize(src.Size))
'    End Sub
'End Class





'Public Class PointCloud_GVectorLine
'    Inherits VBparent
'    Public sideIMU As PointCloud_IMU_SideView
'    Public floorRun As Boolean = True ' the default is to look for a floor...  Set to False to look for ceiling....
'    Public leftPoint As cv.Point2f
'    Public rightPoint As cv.Point2f
'    Dim kalman As Kalman_Basics
'    Public Sub New()
'        initParent()
'        sideIMU = New PointCloud_IMU_SideView()

'        kalman = New Kalman_Basics()

'        If findfrm(caller + " Slider Options") Is Nothing Then
'            sliders.Setup(caller)
'            sliders.setupTrackBar(0, "Threshold for length of line", 1, 50, 40)
'            sliders.setupTrackBar(1, "Threshold for y-displacement of line", 1, 50, 20)
'        End If

'        task.desc = "Find the floor in a side view squared up with gravity"
'    End Sub
'    Public Sub Run()
'        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
'        Static saveFrameCount = -1
'        If saveFrameCount <> task.frameCount Then
'            saveFrameCount = task.frameCount
'            sideIMU.Run()
'            dst1 = sideIMU.dst1
'            dst2 = sideIMU.dst2
'            Dim lines = sideIMU.lDetect.lines
'        End If

'        Static angleSlider = findSlider("Threshold for y-displacement of line")
'        Static lenSlider = findSlider("Threshold for length of line")

'        Dim angleTest = angleSlider.Value
'        Dim lengthTest = lenSlider.value

'        Dim sortedLines = New SortedList(Of Integer, cv.Vec4f)(New compareAllowIdenticalIntegerInverted)
'        If floorRun = False Then sortedLines = New SortedList(Of Integer, cv.Vec4f)(New compareAllowIdenticalInteger)
'        Dim leftPoints As New List(Of cv.Point2f)
'        Dim rightPoints As New List(Of cv.Point2f)
'        If sideIMU.lDetect.lines.Count > 0 Then
'            For Each line In sideIMU.lDetect.lines
'                sortedLines.Add(Math.Max(line.Item1, line.Item3), line)
'            Next
'        End If

'        If sortedLines.Count > 0 Then
'            Dim bottomY = sortedLines.ElementAt(0).Key
'            Dim bottomLine = sortedLines.ElementAt(0).Value
'            Dim bottomCount As Integer
'            Dim bottomLength = Math.Abs(bottomLine.Item0 - bottomLine.Item2)
'            Dim minLeft = Math.Min(bottomLine.Item0, bottomLine.Item2)
'            Dim maxRight = Math.Max(bottomLine.Item0, bottomLine.Item2)
'            For i = 1 To sortedLines.Count - 1
'                Dim nextY = sortedLines.ElementAt(i).Key
'                If Math.Abs(bottomY - nextY) <= angleTest Then
'                    bottomCount += 1
'                    bottomLength += Math.Abs(bottomLine.Item0 - bottomLine.Item2)
'                    Dim nextVal = sortedLines.ElementAt(i).Value
'                    minLeft = Math.Min(nextVal.Item0, minLeft)
'                    minLeft = Math.Min(nextVal.Item2, minLeft)
'                    maxRight = Math.Max(nextVal.Item0, maxRight)
'                    maxRight = Math.Max(nextVal.Item2, maxRight)
'                Else
'                    Exit For
'                End If
'            Next

'            If bottomCount >= 3 And bottomLength >= lengthTest Then
'                leftPoint = New cv.Point(minLeft, bottomY)
'                rightPoint = New cv.Point(maxRight, bottomY)
'            Else
'                Dim maxY As Integer = 0
'                For i = 0 To sortedLines.Count - 1
'                    Dim line = sortedLines.ElementAt(i).Value
'                    Dim pf1 = New cv.Point2f(line.Item0, line.Item1)
'                    Dim pf2 = New cv.Point2f(line.Item2, line.Item3)
'                    If pf1.Y > maxY Then maxY = pf1.Y
'                    If pf2.Y > maxY Then maxY = pf2.Y
'                    If Math.Abs(pf1.X - pf2.X) > lengthTest And Math.Abs(pf1.Y - pf2.Y) < angleTest Then
'                        If pf1.X < pf2.X Then
'                            leftPoints.Add(pf1)
'                            rightPoints.Add(pf2)
'                        Else
'                            leftPoints.Add(pf2)
'                            rightPoints.Add(pf1)
'                        End If
'                    Else
'                        Exit For
'                    End If
'                Next

'                Const MAX_COUNTDOWN = 5
'                Static countDown = MAX_COUNTDOWN
'                If leftPoints.Count >= 2 Then
'                    Dim leftMat = New cv.Mat(leftPoints.Count, 1, cv.MatType.CV_32FC2, leftPoints.ToArray)
'                    Dim rightMat = New cv.Mat(rightPoints.Count, 1, cv.MatType.CV_32FC2, rightPoints.ToArray)
'                    Dim meanLeft = leftMat.Mean()
'                    Dim meanRight = rightMat.Mean()

'                    minLeft = src.Width
'                    For i = 0 To leftMat.Rows - 1
'                        Dim left = leftMat.Get(Of cv.Point2f)(i, 0)
'                        If left.X < minLeft Then minLeft = left.X
'                    Next
'                    leftPoint = New cv.Point2f(minLeft, bottomY)

'                    maxRight = 0
'                    For i = 0 To rightMat.Rows - 1
'                        Dim right = rightMat.Get(Of cv.Point2f)(i, 0)
'                        If right.X > maxRight Then maxRight = right.X
'                    Next
'                    rightPoint = New cv.Point2f(maxRight, bottomY)

'                    If Math.Abs(leftPoint.Y - rightPoint.Y) > angleTest Or leftPoints.Count = 0 Then ' should be level by this point...
'                        leftPoint = New cv.Point2f
'                        rightPoint = New cv.Point2f
'                    End If
'                    countDown = MAX_COUNTDOWN
'                Else
'                    countDown -= 1
'                    If countDown <= 0 Then
'                        leftPoint = New cv.Point2f
'                        rightPoint = New cv.Point2f
'                    End If
'                End If
'            End If
'        End If

'        If leftPoint <> New cv.Point2f Then
'            kalman.kInput(0) = leftPoint.X
'            kalman.kInput(1) = rightPoint.X
'            kalman.Run()
'            leftPoint.X = kalman.kOutput(0)
'            rightPoint.X = kalman.kOutput(1)
'            dst1.Line(leftPoint, rightPoint, cv.Scalar.Yellow, task.lineSize, cv.LineTypes.AntiAlias)
'        End If
'        label1 = "Side View with gravity "
'    End Sub
'End Class







Public Class PointCloud_Singletons
    Inherits VBparent
    Public topView As Histogram_TopView2D
    Public inrange As Depth_InRange
    Dim singleFrames(3 - 1) As cv.Mat
    Public Sub New()
        initParent()
        topView = New Histogram_TopView2D()
        topView.resizeHistOutput = False
        inrange = New Depth_InRange()
        task.thresholdSlider.Value = 1

        label1 = "Top down view before inrange sampling"
        label2 = "Histogram after filtering for single-only histogram bins"
        task.desc = "Find floor and ceiling using gravity aligned top-down view and selecting bins with exactly 1 sample"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If task.frameCount = 0 Then
            For i = 0 To singleFrames.Count - 1
                singleFrames(i) = New cv.Mat(task.pointCloud.Size, cv.MatType.CV_8U, 0)
            Next
        End If
        topView.Run()
        dst1 = topView.dst1

        inrange.src = topView.histOutput
        inrange.minVal = 1
        inrange.maxVal = 1
        inrange.Run()
        Dim mask = inrange.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)

        Dim mask8uc3 = New cv.Mat(mask.Size, cv.MatType.CV_8UC3)
        singleFrames(task.frameCount Mod singleFrames.Count) = mask
        cv.Cv2.Merge(singleFrames, mask8uc3)
        dst2.SetTo(0)
        dst2.SetTo(cv.Scalar.White, mask8uc3.Resize(dst2.Size))
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
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        gCloud.Run()

        Dim split = gCloud.dst1.Split()
        reduction.src = split(2) * 1000
        reduction.src.ConvertTo(reduction.src, cv.MatType.CV_32S)
        reduction.Run()
        reduction.dst1.ConvertTo(split(2), cv.MatType.CV_32F)
        split(2) *= 0.001
        cv.Cv2.Merge(split, dst2)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.sideFrustrumAdjust, task.sideFrustrumAdjust), New cv.Rangef(0, task.maxZ)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {dst2}, New Integer() {1, 2}, New cv.Mat, histOutput, 2, histSize, ranges)

        histOutput = histOutput.Threshold(task.thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)
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
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        gCloud.Run()

        Dim split = gCloud.dst1.Split()
        reduction.src = split(2) * 1000
        reduction.src.ConvertTo(reduction.src, cv.MatType.CV_32S)
        reduction.Run()
        reduction.dst1.ConvertTo(split(2), cv.MatType.CV_32F)
        split(2) *= 0.001
        cv.Cv2.Merge(split, dst2)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, task.maxZ), New cv.Rangef(-task.topFrustrumAdjust, task.topFrustrumAdjust)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {dst2}, New Integer() {2, 0}, New cv.Mat, histOutput, 2, histSize, ranges)

        histOutput = histOutput.Flip(cv.FlipMode.X)
        dst1 = histOutput.Threshold(task.thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)
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
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static showRectanglesCheck = findCheckBox("Draw rectangle and centroid for each mask")
        Dim drawLines = showRectanglesCheck.checked
        measureTop.Run()
        dst1 = measureTop.dst1

        ocvb.pixelsPerMeter = dst1.Height / task.maxZ
        label1 = "Pixels/Meter horizontal: " + CStr(CInt(dst1.Width / task.maxZ)) + " vertical: " + CStr(CInt(ocvb.pixelsPerMeter))

        Dim FOV = ocvb.hFov / 2

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
            ocvb.trueText("Blue Line is " + CStr(pixeldistance) + " pixels from the camera" + vbCrLf +
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
            cmat.src = dst1
            cmat.Run()
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
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static showRectanglesCheck = findCheckBox("Draw rectangle and centroid for each mask")
        Dim drawLines = showRectanglesCheck.checked
        measureSide.Run()
        dst1 = measureSide.dst1

        label1 = "Pixels/Meter horizontal: " + CStr(CInt(dst1.Width / task.maxZ)) + " vertical: " + CStr(CInt(ocvb.pixelsPerMeter))
        Dim FOV = ocvb.vFov / 2

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
            ocvb.trueText("Blue Line is " + CStr(pixeldistance) + " pixels from the camera" + vbCrLf +
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
            cmat.src = dst1
            cmat.Run()
            dst1 = cmat.dst1
        End If
    End Sub
End Class








Public Class PointCloud_BothViews
    Inherits VBparent
    Public topPixel As PointCloud_ObjectsTop
    Public sidePixel As PointCloud_ObjectsSide
    Dim levelCheck As IMU_IscameraLevel
    Public detailText As String
    Public backMat As New cv.Mat
    Public backMatMask As New cv.Mat
    Public vwTop As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Public vwSide As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Dim cmatSide As PointCloud_ColorizeSide
    Dim cmatTop As PointCloud_ColorizeTop
    Public Sub New()
        initParent()

        levelCheck = New IMU_IscameraLevel
        topPixel = New PointCloud_ObjectsTop
        sidePixel = New PointCloud_ObjectsSide
        cmatSide = New PointCloud_ColorizeSide
        cmatTop = New PointCloud_ColorizeTop

        backMat = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        backMatMask = New cv.Mat(src.Size(), cv.MatType.CV_8UC1)

        task.desc = "Find the actual width in pixels for the objects detected in the top view"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static showRectanglesCheck = findCheckBox("Draw rectangle and centroid for each mask")
        Dim showDetails = showRectanglesCheck.checked

        topPixel.Run()
        sidePixel.Run()

        If standalone Or task.intermediateReview = caller Then
            Dim instructions = "Click any centroid to get details"
            Dim accMsg1 = "TopView - distances are accurate"
            Dim accMsg2 = "SideView - distances are accurate"
            If task.xRotateSlider.Value <> 0 Or task.zRotateSlider.Value <> 0 Then
                levelCheck.Run()
                If levelCheck.cameraLevel Then
                    accMsg1 = "Distances are good - camera is level"
                    accMsg2 = "Distances are good - camera is level"
                Else
                    accMsg1 = "Camera NOT level - distances approximate"
                    accMsg2 = "Camera NOT level - distances approximate"
                End If
            End If

            Dim pad = CInt(src.Width / 15)
            ocvb.trueText(accMsg1 + vbCrLf + instructions, 10, src.Height - pad)
            ocvb.trueText(accMsg2 + vbCrLf + instructions, 10, src.Height - pad, 3)
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
            widthInfo = " & " + CStr(rView.Width) + " pixels wide or " + Format(rView.Width / ocvb.pixelsPerMeter, "0.0") + "m"
            detailText = Format(minDepth, "#0.0") + "-" + Format(maxDepth, "#0.0") + "m" + widthInfo

            roi = New cv.Rect(rFront.X, 0, rFront.Width, src.Height)
            vw = vwTop
            If showDetails Then
                ocvb.trueText(detailText, detailPoint.X, detailPoint.Y, picTag:=If(standalone, 2, 3))
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

            widthInfo = " & " + CStr(rView.Width) + " pixels wide or " + Format(rView.Height / ocvb.pixelsPerMeter, "0.0") + "m"
            detailText = Format(minDepth, "#0.0") + "-" + Format(maxDepth, "#0.0") + "m " + widthInfo

            roi = New cv.Rect(0, rFront.Y, src.Width, rFront.Y + rFront.Height)
            vw = vwSide
            If showDetails Then
                ocvb.trueText(detailText, detailPoint.X, detailPoint.Y, 3)
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

        cmatSide.src = sidePixel.dst1
        cmatSide.Run()
        dst1 = cmatSide.dst1

        cmatTop.src = topPixel.dst1
        cmatTop.Run()
        dst2 = cmatTop.dst1
    End Sub
End Class








Public Class PointCloud_BackProjectTopView
    Inherits VBparent
    Dim view As PointCloud_ObjectsTop
    Dim palette As Palette_Basics
    Public Sub New()
        initParent()
        view = New PointCloud_ObjectsTop
        view.colorizeNeeded = True

        palette = New Palette_Basics
        label1 = "Back projection of objects identified in the top view"
        label2 = "Objects identified in the top view"
        task.desc = "Display only the top view of the depth data and backproject the histogram onto the RGB image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        view.Run()
        dst2 = view.dst1

        Dim rectList = New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingleInverted)
        For Each obj In view.viewObjects
            rectList.Add(obj.Value.rectInHist.Y + obj.Value.rectInHist.Height, obj.Value.rectInHist)
        Next
        If rectList.Count > 0 Then
            Dim colorBump = CInt(255 / rectList.Count)

            Static minSlider = findSlider("InRange Min Depth (mm)")
            If task.frameCount = 0 Then minSlider.Value = 1
            Dim minVal = minSlider.value

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
            palette.src = colorMask
            palette.Run()
            dst1 = palette.dst1
        Else
            ocvb.trueText("No objects found")
        End If
    End Sub
End Class








Public Class PointCloud_BackProjectSideView
    Inherits VBparent
    Dim view As PointCloud_ObjectsSide
    Dim cmatSide As PointCloud_ColorizeSide
    Dim palette As Palette_Basics
    Public Sub New()
        initParent()
        palette = New Palette_Basics

        view = New PointCloud_ObjectsSide
        cmatSide = New PointCloud_ColorizeSide
        task.desc = "Display only the side view of the depth data - with and without the IMU active"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        view.Run()
        cmatSide.src = view.dst1
        cmatSide.Run()
        dst2 = cmatSide.dst1

        Dim rectList = New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingleInverted)
        For Each obj In view.viewObjects
            rectList.Add(obj.Value.rectInHist.Y + obj.Value.rectInHist.Height, obj.Value.rectInHist)
        Next

        If rectList.Count > 0 Then
            Dim colorBump = CInt(255 / rectList.Count)

            Static minSlider = findSlider("InRange Min Depth (mm)")
            Dim minVal = minSlider.value

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
            palette.src = colorMask
            palette.Run()
            dst1 = palette.dst1
        Else
            ocvb.trueText("No objects found")
        End If
    End Sub
End Class
