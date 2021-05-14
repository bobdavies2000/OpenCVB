Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class PointCloud_Basics : Inherits VBparent
    Public views As New PointCloud_Tracker
    Public detailText As String
    Public maskTopView As New cv.Mat(dst1.Size(), cv.MatType.CV_8UC1)
    Public maskSideView As New cv.Mat(dst1.Size(), cv.MatType.CV_8UC1)
    Public vwTop As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Public vwSide As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Dim setupSide As New PointCloud_SetupSide
    Dim setupTop As New PointCloud_SetupTop
    Public Sub New()
        task.desc = "Find the actual width in pixels for the objects detected in the top view"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static xRotateSlider = findSlider("Amount to rotate pointcloud around X-axis (degrees)")
        Static zRotateSlider = findSlider("Amount to rotate pointcloud around Z-axis (degrees)")
        Static showRectanglesCheck = findCheckBox("Draw rectangle and centroid for each mask")
        Dim showDetails = showRectanglesCheck.checked
        maskTopView.SetTo(0)
        maskSideView.SetTo(0)

        views.Run(src)

        If standalone Or task.intermediateName = caller Then
            Dim accMsg1 = "TopView - distances are accurate"
            Dim accMsg2 = "SideView - distances are accurate"
            If xRotateSlider.Value <> 0 Or zRotateSlider.Value <> 0 Then
                If task.cameraLevel Then
                    accMsg1 = "Distances are good - camera is level"
                    accMsg2 = "Distances are good - camera is level"
                Else
                    accMsg1 = "Camera NOT level - distances approximate"
                    accMsg2 = "Camera NOT level - distances approximate"
                End If
            End If

            Dim pad = CInt(src.Width / 15)
            task.trueText(accMsg1, 10, src.Height - pad)
            task.trueText(accMsg2, 10, src.Height - pad, 3)
        End If

        Static minDepth As Single, maxDepth As Single
        vwTop = views.topView.pTrack.drawRC.viewObjects
        vwSide = views.sideView.pTrack.drawRC.viewObjects
        Dim roi = New cv.Rect(0, 0, dst1.Width, dst1.Height)
        Dim minIndex As Integer
        Dim detailPoint As cv.Point
        Dim vw As New SortedList(Of Single, viewObject)

        Dim widthInfo As String = ""
        If vwTop.Count Then
            minIndex = findNearestPoint(task.mouseClickPoint, vwTop)
            Dim rView = vwTop.Values(minIndex).rectInHist
            detailPoint = New cv.Point(CInt(rView.X), CInt(rView.Y))
            Dim rFront = vwTop.Values(minIndex).rectFront

            minDepth = task.maxZ * (dst1.Height - (rView.Y + rView.Height)) / dst1.Height
            maxDepth = task.maxZ * (dst1.Height - rView.Y) / dst1.Height
            widthInfo = " & " + CStr(rView.Width) + " pixels wide or " + Format(rView.Width / task.pixelsPerMeterTop, "0.0") + "m"
            detailText = Format(minDepth, "#0.0") + "-" + Format(maxDepth, "#0.0") + "m" + widthInfo

            vw = vwTop
            If showDetails Then
                task.trueText(detailText, detailPoint.X, detailPoint.Y, 3)
                label2 = "Clicked: " + detailText
            End If
            cv.Cv2.InRange(task.depth32f, minDepth * 1000, maxDepth * 1000, maskTopView)
        End If

        If vwSide.Count Then
            minIndex = findNearestPoint(task.mouseClickPoint, vwSide)
            Dim rView = vwSide.Values(minIndex).rectInHist
            detailPoint = New cv.Point(CInt(rView.X), CInt(rView.Y - 15))
            minDepth = task.maxZ * rView.X / dst1.Width
            maxDepth = task.maxZ * (rView.X + rView.Width) / dst1.Width

            widthInfo = " & " + CStr(rView.Width) + " pixels wide or " + Format(rView.Height / task.pixelsPerMeterSide, "0.0") + "m"
            detailText = Format(minDepth, "#0.0") + "-" + Format(maxDepth, "#0.0") + "m " + widthInfo

            vw = vwSide
            If showDetails Then
                task.trueText(detailText, detailPoint.X, detailPoint.Y, 2)
                label1 = "Clicked: " + detailText
            End If
            cv.Cv2.InRange(task.depth32f, minDepth * 1000, maxDepth * 1000, maskSideView)
        End If

        setupSide.Run(views.dst1)
        dst1 = setupSide.dst1

        setupTop.Run(views.dst2)
        dst2 = setupTop.dst1
    End Sub
End Class





Public Class PointCloud_Continuous : Inherits VBparent
    Public Sub New()

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Threshold of continuity in mm", 0, 1000, 10)
        End If

        task.desc = "Show where the pointcloud is continuous"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
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





Public Class PointCloud_Inspector : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Inspection Line", 0, dst1.Width, dst1.Width / 2)
            sliders.setupTrackBar(1, "Y-Direction intervals", 0, 100, 30)
        End If

        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)

        task.desc = "Inspect x, y, and z values in a row or column"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static yLineSlider = findSlider("Y-Direction intervals")
        Static cLineSlider = findSlider("Inspection Line")
        Dim yLines = yLineSlider.value
        Dim cLine = cLineSlider.value

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.depth32f

        Dim topPt = New cv.Point2f(cLine, 0)
        Dim botPt = New cv.Point2f(cLine, dst1.Height)
        dst1 = task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1.Line(topPt, botPt, 255, task.lineWidth + 2, task.lineType)

        Dim stepY = dst1.Height / yLines
        For i = 0 To yLines - 1
            Dim pt1 = New cv.Point2f(dst1.Width, i * stepY)
            Dim pt2 = New cv.Point2f(0, i * stepY)
            dst1.Line(pt1, pt2, 255, task.lineWidth, cv.LineTypes.Link4)

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





Public Class PointCloud_Continuous_VB : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Threshold of continuity in mm", 0, 1000, 10)
        End If

        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U)
        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_8U)
        task.desc = "Show where the pointcloud is continuous"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
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




' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
' https://www.mynteye.com/pages/mynt-eye-d
Public Class PointCloud_SetupSide : Inherits VBparent
    Dim arcSize As Integer
    Dim imu As New IMU_GVector
    Public xCheckbox As Windows.Forms.CheckBox
    Public zCheckbox As Windows.Forms.CheckBox
    Public Sub New()
        arcSize = dst1.Width / 15
        label1 = "Colorize mask for side view"
        task.desc = "Create the colorized mat used for side projections"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 2
        Static zRotateSlider = findSlider("Amount to rotate pointcloud around Z-axis (degrees)")
        Static zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX")
        Static xCheckbox = findCheckBox("Rotate pointcloud around X-axis using gravity vector angleZ")
        Static xRotateSlider = findSlider("Amount to rotate pointcloud around X-axis (degrees)")

        Dim distanceRatio As Single = 1
        Dim fsize = task.fontSize * 1.5

        If standalone Then dst1.SetTo(0) Else src.CopyTo(dst1)
        dst1.Circle(task.sideCameraPoint, task.dotSize, cv.Scalar.BlueViolet, -1, task.lineType)
        For i = 1 To task.maxZ
            Dim xmeter = CInt(dst1.Width * i / task.maxZ * distanceRatio)
            dst1.Line(New cv.Point(xmeter, 0), New cv.Point(xmeter, dst1.Height), cv.Scalar.AliceBlue, task.lineWidth)
            cv.Cv2.PutText(dst1, CStr(i) + "m", New cv.Point(xmeter - src.Width / 15, dst1.Height - 10), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, task.lineWidth, task.lineType)
        Next

        Dim cam = task.sideCameraPoint
        Dim marker As New cv.Point2f(dst1.Width / (task.maxZ * distanceRatio), 0)
        marker.Y = marker.X * Math.Tan((task.vFov / 2) * cv.Cv2.PI / 180)
        Dim topLen = marker.X * Math.Tan((task.hFov / 2) * cv.Cv2.PI / 180)
        Dim markerLeft = New cv.Point(marker.X, cam.Y - marker.Y)
        Dim markerRight = New cv.Point(marker.X, cam.Y + marker.Y)

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

        dst1.Ellipse(cam, New cv.Size(arcSize, arcSize), -startAngle + 90, startAngle, 0, cv.Scalar.White, task.lineWidth + 1, task.lineType)
        dst1.Ellipse(cam, New cv.Size(arcSize, arcSize), 90, 180, 180 + startAngle, cv.Scalar.White, task.lineWidth + 1, task.lineType)
        dst1.Line(cam, fovTop, cv.Scalar.White, task.lineWidth, task.lineType)
        dst1.Line(cam, fovBot, cv.Scalar.White, task.lineWidth, task.lineType)

        dst1.Circle(markerLeft, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        dst1.Circle(markerRight, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        dst1.Line(cam, markerLeft, cv.Scalar.Red, task.lineWidth, task.lineType)
        dst1.Line(cam, markerRight, cv.Scalar.Red, task.lineWidth, task.lineType)

        Dim labelLocation = New cv.Point(src.Width * 0.02, src.Height * 7 / 8)
        cv.Cv2.PutText(dst1, "vFOV=" + CStr(180 - startAngle * 2) + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fsize,
                       cv.Scalar.White, task.lineWidth, task.lineType)
    End Sub
End Class





' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
' https://www.mynteye.com/pages/mynt-eye-d
Public Class PointCloud_SetupTop : Inherits VBparent
    Dim arcSize As Integer
    Dim imu As New IMU_GVector
    Public xCheckbox As Windows.Forms.CheckBox
    Public Sub New()
        arcSize = dst1.Width / 15

        label1 = "Colorize mask for top down view"
        task.desc = "Create the colorize the mat for a topdown projections"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 2
        Dim distanceRatio As Single = 1
        Dim fsize = task.fontSize * 1.5
        If standalone Then dst1.SetTo(0) Else src.CopyTo(dst1)
        dst1.Circle(task.topCameraPoint, task.dotSize, cv.Scalar.BlueViolet, -1, task.lineType)
        For i = 1 To task.maxZ
            Dim ymeter = CInt(dst1.Height - dst1.Height * i / (task.maxZ * distanceRatio))
            dst1.Line(New cv.Point(0, ymeter), New cv.Point(dst1.Width, ymeter), cv.Scalar.AliceBlue, task.lineWidth)
            cv.Cv2.PutText(dst1, CStr(i) + "m", New cv.Point(10, ymeter - 10), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, task.lineWidth, task.lineType)
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

        dst1.Ellipse(task.topCameraPoint, New cv.Size(arcSize, arcSize), -startAngle, startAngle, 0, cv.Scalar.White, task.lineWidth + 1, task.lineType)
        dst1.Ellipse(task.topCameraPoint, New cv.Size(arcSize, arcSize), 0, 180, 180 + startAngle, cv.Scalar.White, task.lineWidth + 1, task.lineType)
        dst1.Line(task.topCameraPoint, fovLeft, cv.Scalar.White, task.lineWidth, task.lineType)

        dst1.Circle(markerLeft, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        dst1.Circle(markerRight, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        dst1.Line(cam, markerLeft, cv.Scalar.Red, task.lineWidth, task.lineType)
        dst1.Line(cam, markerRight, cv.Scalar.Red, task.lineWidth, task.lineType)

        Dim shift = (src.Width - src.Height) / 2
        Dim labelLocation = New cv.Point(dst1.Width / 2 + shift, dst1.Height * 15 / 16)
        cv.Cv2.PutText(dst1, "hFOV=" + CStr(180 - startAngle * 2) + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, task.lineWidth, task.lineType)
        dst1.Line(task.topCameraPoint, fovRight, cv.Scalar.White, task.lineWidth, task.lineType)
    End Sub
End Class







Public Class PointCloud_Raw_CPP : Inherits VBparent
    Dim grid As New Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New()
        findSlider("ThreadGrid Width").Value = 64
        findSlider("ThreadGrid Height").Value = 32

        label1 = "Top View"
        label2 = "Side View"
        task.desc = "Project the depth data onto a top view and side view."

        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.Run(Nothing)

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





Public Class PointCloud_Raw : Inherits VBparent
    Dim grid As New Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New()
        findSlider("ThreadGrid Width").Value = 64
        findSlider("ThreadGrid Height").Value = 32

        label1 = "Top View"
        label2 = "Side View"
        task.desc = "Project the depth data onto a top view and side view - using only VB code (too slow.)"

        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.Run(Nothing)

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









Public Class PointCloud_BackProject : Inherits VBparent
    Dim both As New PointCloud_Basics
    Dim mats As New Mat_4to1
    Public Sub New()
        label1 = "Click any quadrant below to enlarge it"
        label2 = "Click any quadrant below to enlarge it"
        quadrantIndex = 0
        task.desc = "Backproject the selected object"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
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
        mats.mat(2) = both.maskSideView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        mats.mat(3) = both.maskTopView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        mats.Run(Nothing)
        dst1 = mats.dst1
        dst2 = mats.mat(quadrantIndex)
        If quadrantIndex < 2 Then label2 = If(quadrantIndex = 0, both.label1, both.label2) Else label2 = "Click quadrant 0 or 1 to see side/top views"
        task.ttTextData.Clear()
    End Sub
End Class








Public Class PointCloud_FrustrumTop : Inherits VBparent
    Dim frustrum As New Draw_Frustrum
    Dim topView As New Histogram_TopView2D
    Dim setupTop As New PointCloud_SetupTop
    Public Sub New()
        task.hist3DThreshold = 0

        findCheckBox("Rotate pointcloud around X-axis using gravity vector angleZ").Checked = False
        findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX").Checked = False

        label2 = "Draw_Frustrum output"
        task.desc = "Translate only the frustrum with gravity"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        frustrum.Run(src)
        topView.Run(frustrum.dst2)

        setupTop.Run(topView.dst1)
        dst1 = setupTop.dst1
    End Sub
End Class








Public Class PointCloud_FrustrumSide : Inherits VBparent
    Dim frustrum As New Draw_Frustrum
    Dim sideView As New Histogram_SideView2D
    Dim setupSide As New PointCloud_SetupSide
    Public Sub New()
        task.hist3DThreshold = 0

        findCheckBox("Rotate pointcloud around X-axis using gravity vector angleZ").Checked = False
        findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX").Checked = False

        label2 = "Draw_Frustrum output"
        task.desc = "Translate only the frustrum with gravity"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        frustrum.Run(src)
        sideView.Run(frustrum.dst2)

        setupSide.Run(sideView.dst1)
        dst1 = setupSide.dst1
    End Sub
End Class










Public Class PointCloud_ReducedSideView : Inherits VBparent
    Dim gCloud As New Depth_PointCloud_IMU
    Dim reduction As New Reduction_Basics
    Dim histOutput As New cv.Mat
    Public Sub New()
        task.desc = "Create a stable side view of the point cloud"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        gCloud.Run(src)

        Dim split = gCloud.dst1.Split()
        src = split(2) * 1000
        src.ConvertTo(src, cv.MatType.CV_32S)
        reduction.Run(src)
        reduction.dst1.ConvertTo(split(2), cv.MatType.CV_32F)
        split(2) *= 0.001
        cv.Cv2.Merge(split, dst2)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.maxY, task.maxY), New cv.Rangef(0, task.maxZ)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {dst2}, New Integer() {1, 2}, New cv.Mat, histOutput, 2, histSize, ranges)

        histOutput = histOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary)
        histOutput.ConvertTo(dst1, cv.MatType.CV_8UC1)
    End Sub
End Class







Public Class PointCloud_ReducedTopView : Inherits VBparent
    Dim gCloud As New Depth_PointCloud_IMU
    Dim reduction As New Reduction_Basics
    Dim histOutput As New cv.Mat
    Public Sub New()
        task.desc = "Create a stable side view of the point cloud"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        gCloud.Run(src)

        Dim split = gCloud.dst1.Split()
        src = split(2) * 1000
        src.ConvertTo(src, cv.MatType.CV_32S)
        reduction.Run(src)
        reduction.dst1.ConvertTo(split(2), cv.MatType.CV_32F)
        split(2) *= 0.001
        cv.Cv2.Merge(split, dst2)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, task.maxZ), New cv.Rangef(-task.maxX, task.maxX)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {dst2}, New Integer() {2, 0}, New cv.Mat, histOutput, 2, histSize, ranges)

        histOutput = histOutput.Flip(cv.FlipMode.X)
        dst1 = histOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst1, cv.MatType.CV_8UC1)
    End Sub
End Class







'Public Class PointCloud_BackProjectTopView : Inherits VBparent
'    Dim view As New PointCloud_ObjectsTop
'    Public Sub New()
'        view.colorizeNeeded = True

'        label1 = "Back projection of objects identified in the top view"
'        label2 = "Objects identified in the top view"
'        task.desc = "Display only the top view of the depth data and backproject the histogram onto the RGB image."
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        view.Run(src)
'        dst2 = view.dst1

'        Dim rectList = New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingleInverted)
'        For Each obj In view.viewObjects
'            rectList.Add(obj.Value.rectInHist.Y + obj.Value.rectInHist.Height, obj.Value.rectInHist)
'        Next
'        If rectList.Count > 0 Then
'            Dim colorBump = CInt(255 / rectList.Count)

'            Dim split = view.measureTop.topView.gCloud.dst1.Split()
'            Dim colorMask = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
'            dst1 = src
'            For i = 0 To rectList.Count - 1
'                Dim r = rectList.ElementAt(i).Value
'                If r.Width > 0 And r.Height > 0 Then
'                    Dim minDepth = task.maxZ - task.maxZ * (r.Y + r.Height) / dst2.Height
'                    Dim maxDepth = task.maxZ - task.maxZ * r.Y / dst2.Height

'                    Dim minWidth = task.maxZ * r.X / dst2.Width - task.maxY
'                    Dim maxWidth = task.maxZ * (r.X + r.Width) / dst2.Width - task.maxY

'                    Dim mask32f = New cv.Mat

'                    cv.Cv2.InRange(split(2), minDepth, maxDepth, mask32f)
'                    Dim mask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)

'                    cv.Cv2.InRange(split(0), minWidth, maxWidth, mask32f)
'                    Dim hMask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)
'                    cv.Cv2.BitwiseAnd(mask, hMask, mask)

'                    colorMask.SetTo((i * colorBump) Mod 255, mask)
'                End If
'            Next
'            task.palette.Run(colorMask)
'            dst1 = task.palette.dst1
'        Else
'            task.trueText("No objects found")
'        End If
'    End Sub
'End Class








'Public Class PointCloud_BackProjectSideView : Inherits VBparent
'    Dim view As New PointCloud_ObjectsSide
'    Dim setupSide As New PointCloud_SetupSide
'    Public Sub New()
'        task.desc = "Display only the side view of the depth data - with and without the IMU active"
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        view.Run(src)
'        setupSide.Run(view.dst1)
'        dst2 = setupSide.dst1

'        Dim rectList = New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingleInverted)
'        For Each obj In view.viewObjects
'            rectList.Add(obj.Value.rectInHist.Y + obj.Value.rectInHist.Height, obj.Value.rectInHist)
'        Next

'        If rectList.Count > 0 Then
'            Dim colorBump = CInt(255 / rectList.Count)

'            Dim split = view.measureSide.sideView.gCloud.dst1.Split()
'            Dim colorMask = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
'            dst1 = src
'            For i = 0 To rectList.Count - 1
'                Dim r = rectList.ElementAt(i).Value
'                If r.Width > 0 And r.Height > 0 Then
'                    Dim minDepth = task.maxZ * r.X / dst2.Width
'                    Dim maxDepth = task.maxZ * (r.X + r.Width) / dst2.Width

'                    Dim minHeight = task.maxZ - task.maxZ * (r.Y + r.Height) / dst2.Height - task.maxY
'                    Dim maxHeight = task.maxZ - task.maxZ * r.Y / dst2.Height - task.maxY

'                    Dim mask32f = New cv.Mat

'                    cv.Cv2.InRange(split(2), minDepth, maxDepth, mask32f)
'                    Dim mask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)

'                    cv.Cv2.InRange(split(1), minHeight, maxHeight, mask32f)
'                    Dim hMask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)
'                    cv.Cv2.BitwiseAnd(mask, hMask, mask)

'                    colorMask.SetTo((i * colorBump) Mod 255, mask)
'                End If
'            Next
'            task.palette.Run(colorMask)
'            dst1 = task.palette.dst1
'        Else
'            task.trueText("No objects found")
'        End If
'    End Sub
'End Class






Public Class PointCloud_Singletons : Inherits VBparent
    Public tView As New TimeView_Basics
    Public Sub New()
        label1 = "Top down view before inrange sampling"
        label2 = "Histogram after filtering for single-only histogram bins"
        task.desc = "Find floor and ceiling using gravity aligned top-down view and selecting bins with exactly 1 sample"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static frameSlider = findSlider("Number of frames to include")
        Dim frameCount = frameSlider.value

        tView.Run(src)
        dst1 = tView.dst2

        cv.Cv2.InRange(tView.topView.originalHistOutput, frameCount, frameCount, dst2)
        Dim mask = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
    End Sub
End Class










Public Class PointCloud_SingletonRegions : Inherits VBparent
    Public topView As New Histogram_TopView2D
    Dim dilate As New DilateErode_Basics
    Public Sub New()
        topView.resizeHistOutput = False
        task.hist3DThreshold = 1

        label1 = "Top down view before inrange sampling"
        label2 = "Histogram after filtering for single-only histogram bins"
        task.desc = "Find floor and ceiling using gravity aligned top-down view and selecting bins with exactly 1 sample"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        topView.Run(src)
        cv.Cv2.InRange(topView.originalHistOutput, 1, 1, dst2)
        Dim mask = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        dilate.Run(dst2.Clone)
        dst1 = dilate.dst1
    End Sub
End Class






Public Class PointCloud_TimeView : Inherits VBparent
    Dim tView As New TimeView_Basics
    Public Sub New()
        label1 = "Accumulated side view"
        label2 = "Accumulated top view"
        task.desc = "Use the undecorated TimeView input instead of latest point cloud"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        tView.Run(src)
        dst1 = tView.dst1
        dst2 = tView.dst2
    End Sub
End Class






Public Class PointCloud_TrackerTop : Inherits VBparent
    Public pTrack As New KNN_PointTracker
    Public flood As New FloodFill_Palette
    Public timeView As New PointCloud_TimeView
    Public setupTop As New PointCloud_SetupTop
    Public Sub New()
        findSlider("FloodFill Minimum Size").Value = 100
        task.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 3
        timeView.Run(src)
        src = timeView.dst2
        flood.Run(src.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255))

        If flood.dst1.Channels = 3 Then src = flood.dst1 Else src = flood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        pTrack.queryPoints = New List(Of cv.Point2f)(flood.basics.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.basics.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.basics.masks)
        pTrack.Run(src)

        setupTop.Run(pTrack.dst1)
        dst1 = setupTop.dst1
        label1 = Format(dst1.Height / task.maxZ, "0") + " pixels per meter with maxZ at " + Format(task.maxZ, "0.0") + " meters"
    End Sub
End Class






Public Class PointCloud_Tracker : Inherits VBparent
    Public topView As New PointCloud_TrackerTop
    Public sideView As New PointCloud_TrackerSide
    Public Sub New()
        findSlider("FloodFill Minimum Size").Value = 100
        task.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 3
        sideView.Run(sideView.timeView.dst1)
        dst1 = sideView.dst1
        label1 = Format(dst1.Width / task.maxZ, "0") + " pixels per meter with maxZ at " + Format(task.maxZ, "0.0") + " meters"

        topView.Run(sideView.timeView.dst2)
        dst2 = topView.dst1
        label2 = Format(dst1.Height / task.maxZ, "0") + " pixels per meter with maxZ at " + Format(task.maxZ, "0.0") + " meters"
    End Sub
End Class






Public Class PointCloud_TrackerSide : Inherits VBparent
    Public pTrack As New KNN_PointTracker
    Public flood As New FloodFill_Palette
    Public timeView As New PointCloud_TimeView
    Public setupSide As New PointCloud_SetupSide
    Public Sub New()
        findSlider("FloodFill Minimum Size").Value = 100
        task.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 3
        timeView.Run(src)
        src = timeView.dst1

        flood.Run(src.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255))

        If flood.dst1.Channels = 3 Then src = flood.dst1 Else src = flood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        pTrack.queryPoints = New List(Of cv.Point2f)(flood.basics.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.basics.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.basics.masks)
        pTrack.Run(src)

        setupSide.Run(pTrack.dst1)
        dst1 = setupSide.dst1
        label1 = Format(dst1.Width / task.maxZ, "0") + " pixels per meter with maxZ at " + Format(task.maxZ, "0.0") + " meters"
    End Sub
End Class








Public Class PointCloud_ObjectsTop : Inherits VBparent
    Public measureTop As New PointCloud_TrackerTop
    Public viewObjects As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Dim setupTop As New PointCloud_SetupTop
    Public colorizeNeeded As Boolean
    Public Sub New()
        If standalone Then
            If sliders.Setup(caller) Then
                sliders.Setup(caller, 1)
                sliders.setupTrackBar(0, "Test Bar Distance from camera in mm", 1, 4000, 1500)
            End If
        End If
        task.desc = "Validate the formula for pixel height as a function of distance"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static distanceSlider = findSlider("Test Bar Distance from camera in mm")
        Static showRectanglesCheck = findCheckBox("Draw rectangle and centroid for each mask")
        Dim drawLines = showRectanglesCheck.checked
        measureTop.Run(src)
        dst1 = measureTop.dst1

        label1 = "Pixels/Meter horizontal: " + CStr(CInt(dst1.Width / task.maxZ)) + " vertical: " + CStr(CInt(task.pixelsPerMeterTop))

        Dim FOV = task.hFov / 2

        Dim xpt1 As cv.Point2f, xpt2 As cv.Point2f
        If standalone Then
            Dim pixeldistance = src.Height * (distanceSlider.Value / 1000) / task.maxZ
            Dim lineHalf = CInt(Math.Tan(FOV * 0.0174533) * pixeldistance)

            xpt1 = New cv.Point2f(task.topCameraPoint.X - lineHalf, src.Height - pixeldistance)
            xpt2 = New cv.Point2f(task.topCameraPoint.X + lineHalf, src.Height - pixeldistance)
            If drawLines Then dst1.Line(xpt1, xpt2, cv.Scalar.Blue, task.lineWidth + 2)
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
            If drawLines Then dst1.Line(drawPt1, drawpt2, cv.Scalar.Yellow, task.lineWidth + 2)

            Dim vo = measureTop.pTrack.drawRC.viewObjects.Values(i)
            Dim addlen As Single = 0
            ' need to add a small amount to the object width in pixels based on the angle to the camera of the back edge
            If Not (task.topCameraPoint.X > r.X And task.topCameraPoint.X < r.X + r.Width) Then
                If r.X > task.topCameraPoint.X Then
                    addlen = r.Height * Math.Abs(r.X - task.topCameraPoint.X) / (src.Height - r.Y)
                    If drawLines Then dst1.Line(New cv.Point2f(r.X, r.Y + r.Height), New cv.Point2f(r.X - addlen, r.Y + r.Height), cv.Scalar.Yellow, task.lineWidth + 2)
                    coneleft -= addlen
                Else
                    addlen = r.Height * (task.topCameraPoint.X - (r.X + r.Width)) / (src.Height - r.Y)
                    If drawLines Then dst1.Line(New cv.Point2f(r.X + r.Width, r.Y + r.Height), New cv.Point2f(r.X + r.Width + addlen, r.Y + r.Height), cv.Scalar.Yellow, task.lineWidth + 2)
                    If coneleft - addlen >= xpt1.X Then coneleft -= addlen
                End If
            End If
            Dim newX = (coneleft - xpt1.X) * src.Width / (lineHalf * 2)
            Dim newWidth = src.Width * (addlen + coneRight - coneleft) / (lineHalf * 2)
            vo.rectFront = New cv.Rect(newX, r.Y, newWidth, r.Height)
            viewObjects.Add(vo.rectFront.Width * vo.rectFront.Height, vo)
        Next
        If standalone Or task.intermediateName = caller Or colorizeNeeded Then
            setupTop.Run(dst1)
            dst1 = setupTop.dst1
        End If
    End Sub
End Class







Public Class PointCloud_ObjectsSide : Inherits VBparent
    Public measureSide As New PointCloud_TrackerSide
    Public viewObjects As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Dim setupSide As New PointCloud_SetupSide
    Public Sub New()
        If standalone Then
            If sliders.Setup(caller) Then
                sliders.setupTrackBar(0, "Test Bar Distance from camera in mm", 1, 4000, 1500)
            End If
        End If
        task.desc = "Validate the formula for pixel width as a function of distance"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static distanceSlider = findSlider("Test Bar Distance from camera in mm")
        Static showRectanglesCheck = findCheckBox("Draw rectangle and centroid for each mask")
        Dim drawLines = showRectanglesCheck.checked
        measureSide.Run(src)
        dst1 = measureSide.dst1

        label1 = "Pixels/Meter horizontal: " + CStr(CInt(task.pixelsPerMeterSide)) + " vertical: " + CStr(CInt(task.pixelsPerMeterTop))
        Dim FOV = task.vFov / 2

        Dim xpt1 As cv.Point2f, xpt2 As cv.Point2f
        If standalone Then
            Dim pixeldistance = src.Width * (distanceSlider.Value / 1000) / task.maxZ
            Dim lineHalf = CInt(Math.Tan(FOV * 0.0174533) * pixeldistance)

            xpt1 = New cv.Point2f(task.sideCameraPoint.X + pixeldistance, task.sideCameraPoint.Y - lineHalf)
            xpt2 = New cv.Point2f(task.sideCameraPoint.X + pixeldistance, task.sideCameraPoint.Y + lineHalf)
            If drawLines Then dst1.Line(xpt1, xpt2, cv.Scalar.Blue, task.lineWidth + 2)
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
            If drawLines Then dst1.Line(drawPt1, drawpt2, cv.Scalar.Yellow, task.lineWidth + 2)

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
                    If drawLines Then dst1.Line(New cv.Point2f(r.X, r.Y + r.Height), New cv.Point2f(r.X, r.Y + r.Height + addlen), cv.Scalar.Yellow, task.lineWidth + 2)
                    r = New cv.Rect(r.X, r.Y + addlen, r.Width, coneRight - coneleft + addlen)
                    coneleft += addlen
                End If
            End If
            Dim newY = (coneleft - xpt1.Y) * src.Height / (lineHalf * 2)
            Dim newHeight = src.Height * (addlen + coneRight - coneleft) / (lineHalf * 2)
            vo.rectFront = New cv.Rect(r.X, newY, r.Width, newHeight)
            viewObjects.Add(vo.rectFront.Width * vo.rectFront.Height, vo)
        Next
        If standalone Or task.intermediateName = caller Then
            setupSide.Run(dst1)
            dst1 = setupSide.dst1
        End If
    End Sub
End Class







Public Class PointCoud_SurfaceH_CPP : Inherits VBparent
    Public tView As New TimeView_Basics
    Public plot As New Plot_Basics_CPP
    Public topRow As Integer
    Public botRow As Integer
    Public peakRow As Integer
    Public Sub New()
        task.desc = "Find the horizontal surfaces with a projects of the SideView histogram."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        tView.Run(src)
        dst1 = tView.dst1

        ReDim plot.srcX(dst1.Height - 1)
        ReDim plot.srcY(dst1.Height - 1)
        topRow = 0
        botRow = 0
        peakRow = 0
        Dim peakVal As Integer
        For i = 0 To dst1.Height - 1
            plot.srcX(i) = i
            plot.srcY(i) = dst1.Row(i).CountNonZero()
            If peakVal < plot.srcY(i) Then
                peakVal = plot.srcY(i)
                peakRow = i
            End If
            If topRow = 0 And plot.srcY(i) > 10 Then topRow = i
        Next

        For i = plot.srcY.Count - 1 To 0 Step -1
            If botRow = 0 And plot.srcY(i) > 10 Then botRow = i
        Next
        plot.Run(Nothing)
        dst2 = plot.dst1.Transpose()
        dst2 = dst2.Flip(cv.FlipMode.Y)
        label1 = "Top row = " + CStr(topRow) + " peak row = " + CStr(peakRow) + " bottom row = " + CStr(botRow)
    End Sub
End Class







Public Class PointCoud_SurfaceH : Inherits VBparent
    Public tView As New TimeView_Basics
    Public plot As New Plot_Histogram
    Public topRow As Integer
    Public botRow As Integer
    Public peakRow As Integer
    Public Sub New()
        task.desc = "Find the horizontal surfaces with a projects of the SideView histogram."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        tView.Run(src)
        dst1 = tView.dst1
        plot.hist = New cv.Mat(dst1.Height, 1, cv.MatType.CV_32F, 0)
        Dim indexer = plot.hist.GetGenericIndexer(Of Single)()

        topRow = 0
        botRow = 0
        peakRow = 0
        Dim peakVal As Integer
        If dst1.Channels <> 1 Then dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        For i = 0 To dst1.Height - 1
            indexer(i) = dst1.Row(i).CountNonZero()
            If peakVal < indexer(i) Then
                peakVal = indexer(i)
                peakRow = i
            End If
            If topRow = 0 And indexer(i) > 10 Then topRow = i
        Next

        plot.fixedMaxVal = (Math.Floor(peakVal / 100) + 1) * 100
        For i = plot.hist.Rows - 1 To 0 Step -1
            If botRow = 0 And indexer(i) > 10 Then botRow = i
        Next
        plot.Run(src)
        dst2 = plot.dst1.Transpose()
        dst2 = dst2.Flip(cv.FlipMode.Y)
        label1 = "Top row = " + CStr(topRow) + " peak row = " + CStr(peakRow) + " bottom row = " + CStr(botRow)

        Dim ratio = task.mousePoint.Y / dst1.Height
        Dim offset = ratio * dst2.Height
        dst1 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst1.Line(New cv.Point(0, task.mousePoint.Y), New cv.Point(dst1.Width, task.mousePoint.Y), cv.Scalar.Yellow, task.lineWidth)
        dst2.Line(New cv.Point(0, offset), New cv.Point(dst2.Width, offset), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class