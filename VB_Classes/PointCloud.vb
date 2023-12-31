Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class PointCloud_Basics : Inherits VBparent
    Public tView As New TimeView_FloodFill
    Public sideObjects As Integer
    Public topObjects As Integer
    Public objectsFound As Integer
    Public Sub New()
        task.desc = "Find out how many objects are in the side and top views"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static minSlider = findSlider("FloodFill Minimum Size")

        tView.RunClass(src)
        dst2 = tView.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst3 = tView.dst3.Threshold(0, 255, cv.ThresholdTypes.Binary)

        sideObjects = tView.floodSide.masks.Count
        topObjects = tView.floodTop.masks.Count

        If standalone Then setTrueText("Adjust the Global Option 'Projection threshold' to see more objects", 10, dst2.Height - 40)
        labels = tView.labels

        If sideObjects + topObjects > 0 Then
            If sideObjects + topObjects = 1 Then objectsFound = 1 Else objectsFound = Math.Min(sideObjects, topObjects)

            If objectsFound > 1 Then minSlider.value -= If(minSlider.value > 50, 50, 0)
            If objectsFound = 0 Then minSlider.value += If(minSlider.value < minSlider.maximum - 10, 10, 0)
        Else
            setTrueText("Is there depth at the location clicked?", 10, dst2.Height - 80, 3)
        End If
    End Sub
End Class









Public Class PointCloud_Display : Inherits VBparent
    Public views As New PointCloud_Tracker
    Public detailText As String
    Public maskTopView As New cv.Mat(dst2.Size(), cv.MatType.CV_8UC1)
    Public maskSideView As New cv.Mat(dst2.Size(), cv.MatType.CV_8UC1)
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

        views.RunClass(src)

        If standalone Or task.intermediateActive Then
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
            setTrueText(accMsg1, 10, src.Height - pad)
            setTrueText(accMsg2, 10, src.Height - pad, 3)
        End If

        Static minDepth As Single, maxDepth As Single
        vwTop = views.topView.pTrack.drawRC.viewObjects
        vwSide = views.sideView.pTrack.drawRC.viewObjects
        Dim roi = New cv.Rect(0, 0, dst2.Width, dst2.Height)
        Dim minIndex As Integer
        Dim detailPoint As cv.Point
        Dim vw As New SortedList(Of Single, viewObject)

        Dim widthInfo As String = ""
        If vwTop.Count Then
            minIndex = findNearestPoint(task.mouseClickPoint, vwTop)
            Dim rView = vwTop.Values(minIndex).rectInHist
            detailPoint = New cv.Point(CInt(rView.X), CInt(rView.Y))
            Dim rFront = vwTop.Values(minIndex).rectFront

            minDepth = task.maxZ * (dst2.Height - (rView.Y + rView.Height)) / dst2.Height
            maxDepth = task.maxZ * (dst2.Height - rView.Y) / dst2.Height
            widthInfo = " & " + CStr(rView.Width) + " pixels wide or " + Format(rView.Width / dst2.Height / task.maxZ, "0.0") + "m"
            detailText = Format(minDepth, "#0.0") + "-" + Format(maxDepth, "#0.0") + "m" + widthInfo

            vw = vwTop
            If showDetails Then
                setTrueText(detailText, detailPoint.X, detailPoint.Y, 3)
                labels(3) = "Clicked: " + detailText
            End If
            cv.Cv2.InRange(task.depth32f, minDepth * 1000, maxDepth * 1000, maskTopView)
        End If

        If vwSide.Count Then
            minIndex = findNearestPoint(task.mouseClickPoint, vwSide)
            Dim rView = vwSide.Values(minIndex).rectInHist
            detailPoint = New cv.Point(CInt(rView.X), CInt(rView.Y - 15))
            minDepth = task.maxZ * rView.X / dst2.Width
            maxDepth = task.maxZ * (rView.X + rView.Width) / dst2.Width

            widthInfo = " & " + CStr(rView.Width) + " pixels wide or " + Format(rView.Height / dst2.Width / task.maxZ, "0.0") + "m"
            detailText = Format(minDepth, "#0.0") + "-" + Format(maxDepth, "#0.0") + "m " + widthInfo

            vw = vwSide
            If showDetails Then
                setTrueText(detailText, detailPoint.X, detailPoint.Y, 2)
                labels(2) = "Clicked: " + detailText
            End If
            cv.Cv2.InRange(task.depth32f, minDepth * 1000, maxDepth * 1000, maskSideView)
        End If

        setupSide.RunClass(views.dst2)
        dst2 = setupSide.dst2

        setupTop.RunClass(views.dst3)
        dst3 = setupTop.dst2
    End Sub
End Class








Public Class PointCloud_Inspector : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Inspection Line", 0, dst2.Width, dst2.Width / 2)
            sliders.setupTrackBar(1, "Y-Direction intervals", 0, 100, 30)
        End If
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
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
        Dim botPt = New cv.Point2f(cLine, dst2.Height)
        dst2 = task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2.Line(topPt, botPt, 255, task.lineWidth + 2, task.lineType)

        Dim stepY = dst2.Height / yLines
        For i = 0 To yLines - 1
            Dim pt1 = New cv.Point2f(dst2.Width, i * stepY)
            Dim pt2 = New cv.Point2f(0, i * stepY)
            dst2.Line(pt1, pt2, 255, task.lineWidth, cv.LineTypes.Link4)

            Dim pt = New cv.Point2f(cLine, i * stepY)
            Dim xyz = task.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X)
            pt.Y += stepY
            pt.X += 20
            If pt.X > dst2.Width * 3 / 4 Then pt.X = dst2.Width * 3 / 4
            cv.Cv2.PutText(dst2, "Row " + CStr(i) + " " + Format(xyz.Item0, "#0.00") + " " + Format(xyz.Item1, "#0.00") + " " +
                           Format(xyz.Item2, "#0.00"), pt, cv.HersheyFonts.HersheyComplexSmall, 0.7, cv.Scalar.White, 2)
        Next
        labels(2) = "Values displayed are for column " + CStr(cLine)
    End Sub
End Class





Public Class PointCloud_Continuous_VB : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Threshold of continuity in mm", 0, 1000, 10)
        End If

        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        task.desc = "Show where the pointcloud is continuous"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("Threshold of continuity in mm")
        Dim threshold = thresholdSlider.value

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.depth32f

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
        labels(2) = "White pixels: Z-values within " + CStr(thresholdSlider.value) + " mm's of X neighbor"
        labels(3) = "Mask showing discontinuities > " + CStr(thresholdSlider.value) + " mm's of X neighbor"
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
    Public xCheckbox As Windows.Forms.CheckBox
    Dim imu As New IMU_GVector
    Public zCheckbox As Windows.Forms.CheckBox
    Public Sub New()
        arcSize = dst2.Width / 15
        labels(2) = "Layout markers for side view"
        task.desc = "Create the colorized mat used for side projections"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 2
        Static zRotateSlider = findSlider("Amount to rotate pointcloud around Z-axis (degrees)")
        Static zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX")
        Static xCheckbox = findCheckBox("Rotate pointcloud around X-axis using gravity vector angleZ")
        Static xRotateSlider = findSlider("Amount to rotate pointcloud around X-axis (degrees)")

        Dim distanceRatio As Single = 1
        Dim fsize = task.fontSize * 1.5

        If src.Channels <> 3 Then src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If standalone Then dst2.SetTo(0) Else src.CopyTo(dst2)
        dst2.Circle(task.sideCameraPoint, task.dotSize, cv.Scalar.BlueViolet, -1, task.lineType)
        For i = 1 To task.maxZ
            Dim xmeter = CInt(dst2.Width * i / task.maxZ * distanceRatio)
            dst2.Line(New cv.Point(xmeter, 0), New cv.Point(xmeter, dst2.Height), cv.Scalar.AliceBlue, task.lineWidth)
            cv.Cv2.PutText(dst2, CStr(i) + "m", New cv.Point(xmeter - src.Width / 15, dst2.Height - 10), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, task.lineWidth, task.lineType)
        Next

        Dim cam = task.sideCameraPoint
        Dim marker As New cv.Point2f(dst2.Width / (task.maxZ * distanceRatio), 0)
        marker.Y = marker.X * Math.Tan((task.vFov / 2) * cv.Cv2.PI / 180)
        Dim topLen = marker.X * Math.Tan((task.hFov / 2) * cv.Cv2.PI / 180)
        Dim markerLeft = New cv.Point(marker.X, cam.Y - marker.Y)
        Dim markerRight = New cv.Point(marker.X, cam.Y + marker.Y)

        If standalone Then imu.RunClass(src)

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
            dst2.Circle(markerLeft, task.dotSize, cv.Scalar.Red, -1, task.lineType)
            dst2.Circle(markerRight, task.dotSize, cv.Scalar.Red, -1, task.lineType)
        End If

        ' draw the arc enclosing the camera FOV
        Dim startAngle = (180 - task.vFov) / 2
        Dim y = dst2.Width / Math.Tan(startAngle * cv.Cv2.PI / 180)

        Dim fovTop = New cv.Point(dst2.Width, cam.Y - y)
        Dim fovBot = New cv.Point(dst2.Width, cam.Y + y)

        dst2.Ellipse(cam, New cv.Size(arcSize, arcSize), -startAngle + 90, startAngle, 0, cv.Scalar.White, task.lineWidth + 1, task.lineType)
        dst2.Ellipse(cam, New cv.Size(arcSize, arcSize), 90, 180, 180 + startAngle, cv.Scalar.White, task.lineWidth + 1, task.lineType)
        dst2.Line(cam, fovTop, cv.Scalar.White, task.lineWidth, task.lineType)
        dst2.Line(cam, fovBot, cv.Scalar.White, task.lineWidth, task.lineType)

        dst2.Circle(markerLeft, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        dst2.Circle(markerRight, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        dst2.Line(cam, markerLeft, cv.Scalar.Red, task.lineWidth, task.lineType)
        dst2.Line(cam, markerRight, cv.Scalar.Red, task.lineWidth, task.lineType)

        Dim labelLocation = New cv.Point(src.Width * 0.02, src.Height * 7 / 8)
        cv.Cv2.PutText(dst2, "vFOV=" + Format(180 - startAngle * 2, "0.0") + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fsize,
                       cv.Scalar.White, task.lineWidth, task.lineType)
    End Sub
End Class





' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
' https://www.mynteye.com/pages/mynt-eye-d
Public Class PointCloud_SetupTop : Inherits VBparent
    Dim arcSize As Integer
    Public imu As New IMU_GVector
    Public xCheckbox As Windows.Forms.CheckBox
    Public Sub New()
        arcSize = dst2.Width / 15

        labels(2) = "Layout markers for top view"
        task.desc = "Create the colorize the mat for a topdown projections"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 2
        Dim distanceRatio As Single = 1
        Dim fsize = task.fontSize * 1.5

        If src.Channels <> 3 Then src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If standalone Then dst2.SetTo(0) Else src.CopyTo(dst2)
        dst2.Circle(task.topCameraPoint, task.dotSize, cv.Scalar.BlueViolet, -1, task.lineType)
        For i = 1 To task.maxZ
            Dim ymeter = CInt(dst2.Height - dst2.Height * i / (task.maxZ * distanceRatio))
            dst2.Line(New cv.Point(0, ymeter), New cv.Point(dst2.Width, ymeter), cv.Scalar.AliceBlue, task.lineWidth)
            cv.Cv2.PutText(dst2, CStr(i) + "m", New cv.Point(10, ymeter - 10), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, task.lineWidth, task.lineType)
        Next

        Dim cam = task.topCameraPoint
        Dim marker As New cv.Point2f(cam.X, dst2.Height / (task.maxZ * distanceRatio))
        Dim topLen = marker.Y * Math.Tan((task.hFov / 2) * cv.Cv2.PI / 180)
        Dim sideLen = marker.Y * Math.Tan((task.vFov / 2) * cv.Cv2.PI / 180)
        Dim markerLeft = New cv.Point(cam.X - topLen, dst2.Height - marker.Y)
        Dim markerRight = New cv.Point(cam.X + topLen, dst2.Height - marker.Y)

        Static zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX")
        Static zRotateSlider = findSlider("Amount to rotate pointcloud around Z-axis (degrees)")
        If standalone Then imu.RunClass(src)
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
        Dim x = dst2.Height / Math.Tan(startAngle * cv.Cv2.PI / 180)

        Dim fovRight = New cv.Point(task.topCameraPoint.X + x, 0)
        Dim fovLeft = New cv.Point(task.topCameraPoint.X - x, fovRight.Y)

        dst2.Ellipse(task.topCameraPoint, New cv.Size(arcSize, arcSize), -startAngle, startAngle, 0, cv.Scalar.White, task.lineWidth + 1, task.lineType)
        dst2.Ellipse(task.topCameraPoint, New cv.Size(arcSize, arcSize), 0, 180, 180 + startAngle, cv.Scalar.White, task.lineWidth + 1, task.lineType)
        dst2.Line(task.topCameraPoint, fovLeft, cv.Scalar.White, task.lineWidth, task.lineType)

        dst2.Circle(markerLeft, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        dst2.Circle(markerRight, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        dst2.Line(cam, markerLeft, cv.Scalar.Red, task.lineWidth, task.lineType)
        dst2.Line(cam, markerRight, cv.Scalar.Red, task.lineWidth, task.lineType)

        Dim shift = (src.Width - src.Height) / 2
        Dim labelLocation = New cv.Point(dst2.Width / 2 + shift, dst2.Height * 15 / 16)
        cv.Cv2.PutText(dst2, "hFOV=" + Format(180 - startAngle * 2, "0.0") + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, task.lineWidth, task.lineType)
        dst2.Line(task.topCameraPoint, fovRight, cv.Scalar.White, task.lineWidth, task.lineType)
    End Sub
End Class







Public Class PointCloud_Raw_CPP : Inherits VBparent
    Dim grid As New Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New()
        findSlider("ThreadGrid Width").Value = 64
        findSlider("ThreadGrid Height").Value = 32

        labels(2) = "Top View"
        labels(3) = "Side View"
        task.desc = "Project the depth data onto a top view and side view."

        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.RunClass(Nothing)

        Dim h = src.Height
        Dim w = src.Width
        Dim range = CSng(task.maxDepth - task.minDepth)
        If depthBytes Is Nothing Then
            ReDim depthBytes(task.depth32f.Total * task.depth32f.ElemSize - 1)
        End If

        Marshal.Copy(task.depth32f.Data, depthBytes, 0, depthBytes.Length)
        Dim handleDepth = GCHandle.Alloc(depthBytes, GCHandleType.Pinned)

        Dim imagePtr = SimpleProjectionRun(cPtr, handleDepth.AddrOfPinnedObject, task.minDepth, task.maxDepth, task.depth32f.Height, task.depth32f.Width)

        dst2 = New cv.Mat(task.depth32f.Rows, task.depth32f.Cols, cv.MatType.CV_8U, imagePtr).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = New cv.Mat(task.depth32f.Rows, task.depth32f.Cols, cv.MatType.CV_8U, SimpleProjectionSide(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        handleDepth.Free()
        labels(2) = "Top View (looking down)"
        labels(3) = "Side View"
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

        labels(2) = "Top View"
        labels(3) = "Side View"
        task.desc = "Project the depth data onto a top view and side view - using only VB code (too slow.)"

        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.RunClass(Nothing)

        Dim h = src.Height
        Dim w = src.Width
        Dim range = CSng(task.maxDepth - task.minDepth)

        ' this VB.Net version is much slower than the optimized C++ version below.
        dst2 = src.EmptyClone.SetTo(cv.Scalar.White)
        dst3 = dst2.Clone()
        Dim black = New cv.Vec3b(0, 0, 0)
        Parallel.ForEach(grid.roiList,
             Sub(roi)
                 For y = roi.Y To roi.Y + roi.Height - 1
                     For x = roi.X To roi.X + roi.Width - 1
                         Dim m = task.depthMask.Get(Of Byte)(y, x)
                         If m > 0 Then
                             Dim depth = task.depth32f.Get(Of Single)(y, x)
                             Dim dy = CInt(h * (depth - task.minDepth) / range)
                             If dy < h And dy > 0 Then dst2.Set(Of cv.Vec3b)(h - dy, x, black)
                             Dim dx = CInt(w * (depth - task.minDepth) / range)
                             If dx < w And dx > 0 Then dst3.Set(Of cv.Vec3b)(y, dx, black)
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









Public Class PointCloud_BackProject : Inherits VBparent
    Dim both As New PointCloud_Display
    Dim mats As New Mat_4Click
    Public Sub New()
        labels(2) = "Click any quadrant below to enlarge it"
        labels(3) = "Click any quadrant below to enlarge it"
        quadrantIndex = 0
        task.desc = "Backproject the selected object"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.mouseClickFlag Then
            If task.mousePicTag = RESULT_DST2 Then
                setMyActiveMat()
                task.mouseClickFlag = False ' absorb the mouse click here only
            End If
        End If
        both.RunClass(src)

        mats.mat(0) = both.dst2
        mats.mat(1) = both.dst3
        mats.mat(2) = both.maskSideView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        mats.mat(3) = both.maskTopView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        mats.RunClass(src)
        dst2 = mats.dst2
        dst3 = mats.mat(quadrantIndex)
        If quadrantIndex < 2 Then labels(3) = If(quadrantIndex = 0, both.labels(2), both.labels(3)) Else labels(3) = "Click quadrant 0 or 1 to see side/top views"
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

        labels(3) = "Draw_Frustrum output"
        task.desc = "Translate only the frustrum with gravity"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        frustrum.RunClass(src)
        topView.RunClass(frustrum.dst3)

        setupTop.RunClass(topView.dst2)
        dst2 = setupTop.dst2
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

        labels(3) = "Draw_Frustrum output"
        task.desc = "Translate only the frustrum with gravity"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        frustrum.RunClass(src)
        sideView.RunClass(frustrum.dst3)

        setupSide.RunClass(sideView.dst2)
        dst2 = setupSide.dst2
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
        gCloud.RunClass(src)

        Dim split = gCloud.dst2.Split()
        src = split(2) * 1000
        src.ConvertTo(src, cv.MatType.CV_32S)
        reduction.RunClass(src)
        reduction.dst2.ConvertTo(split(2), cv.MatType.CV_32F)
        split(2) *= 0.001
        cv.Cv2.Merge(split, dst3)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.maxY, task.maxY), New cv.Rangef(0, task.maxZ)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {dst3}, New Integer() {1, 2}, New cv.Mat, histOutput, 2, histSize, ranges)

        histOutput = histOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary)
        histOutput.ConvertTo(dst2, cv.MatType.CV_8UC1)
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
        gCloud.RunClass(src)

        Dim split = gCloud.dst2.Split()
        src = split(2) * 1000
        src.ConvertTo(src, cv.MatType.CV_32S)
        reduction.RunClass(src)
        reduction.dst2.ConvertTo(split(2), cv.MatType.CV_32F)
        split(2) *= 0.001
        cv.Cv2.Merge(split, dst3)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, task.maxZ), New cv.Rangef(-task.maxX, task.maxX)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {dst3}, New Integer() {2, 0}, New cv.Mat, histOutput, 2, histSize, ranges)

        histOutput = histOutput.Flip(cv.FlipMode.X)
        dst2 = histOutput.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class







'Public Class PointCloud_BackProjectTopView : Inherits VBparent
'    Dim view As New PointCloud_ObjectsTop
'    Public Sub New()
'        view.colorizeNeeded = True

'        labels(2) = "Back projection of objects identified in the top view"
'        labels(3) = "Objects identified in the top view"
'        task.desc = "Display only the top view of the depth data and backproject the histogram onto the RGB image."
'    End Sub
'    Public Sub Run(src As cv.Mat) ' Rank = 1
'        view.RunClass(src)
'        dst3 = view.dst2

'        Dim rectList = New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingleInverted)
'        For Each obj In view.viewObjects
'            rectList.Add(obj.Value.rectInHist.Y + obj.Value.rectInHist.Height, obj.Value.rectInHist)
'        Next
'        If rectList.Count > 0 Then
'            Dim colorBump = CInt(255 / rectList.Count)

'            Dim split = view.measureTop.topView.gCloud.dst2.Split()
'            Dim colorMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
'            dst2 = src
'            For i = 0 To rectList.Count - 1
'                Dim r = rectList.ElementAt(i).Value
'                If r.Width > 0 And r.Height > 0 Then
'                    Dim minDepth = task.maxZ - task.maxZ * (r.Y + r.Height) / dst3.Height
'                    Dim maxDepth = task.maxZ - task.maxZ * r.Y / dst3.Height

'                    Dim minWidth = task.maxZ * r.X / dst3.Width - task.maxY
'                    Dim maxWidth = task.maxZ * (r.X + r.Width) / dst3.Width - task.maxY

'                    Dim mask32f = New cv.Mat

'                    cv.Cv2.InRange(split(2), minDepth, maxDepth, mask32f)
'                    Dim mask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)

'                    cv.Cv2.InRange(split(0), minWidth, maxWidth, mask32f)
'                    Dim hMask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)
'                    cv.Cv2.BitwiseAnd(mask, hMask, mask)

'                    colorMask.SetTo((i * colorBump) Mod 255, mask)
'                End If
'            Next
'            task.palette.RunClass(colorMask)
'            dst2 = task.palette.dst2
'        Else
'            setTrueText("No objects found")
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
'        view.RunClass(src)
'        setupSide.RunClass(view.dst2)
'        dst3 = setupSide.dst2

'        Dim rectList = New SortedList(Of Single, cv.Rect)(New compareAllowIdenticalSingleInverted)
'        For Each obj In view.viewObjects
'            rectList.Add(obj.Value.rectInHist.Y + obj.Value.rectInHist.Height, obj.Value.rectInHist)
'        Next

'        If rectList.Count > 0 Then
'            Dim colorBump = CInt(255 / rectList.Count)

'            Dim split = view.measureSide.sideView.gCloud.dst2.Split()
'            Dim colorMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
'            dst2 = src
'            For i = 0 To rectList.Count - 1
'                Dim r = rectList.ElementAt(i).Value
'                If r.Width > 0 And r.Height > 0 Then
'                    Dim minDepth = task.maxZ * r.X / dst3.Width
'                    Dim maxDepth = task.maxZ * (r.X + r.Width) / dst3.Width

'                    Dim minHeight = task.maxZ - task.maxZ * (r.Y + r.Height) / dst3.Height - task.maxY
'                    Dim maxHeight = task.maxZ - task.maxZ * r.Y / dst3.Height - task.maxY

'                    Dim mask32f = New cv.Mat

'                    cv.Cv2.InRange(split(2), minDepth, maxDepth, mask32f)
'                    Dim mask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)

'                    cv.Cv2.InRange(split(1), minHeight, maxHeight, mask32f)
'                    Dim hMask = mask32f.Threshold(0, 255, cv.ThresholdTypes.Binary)
'                    cv.Cv2.BitwiseAnd(mask, hMask, mask)

'                    colorMask.SetTo((i * colorBump) Mod 255, mask)
'                End If
'            Next
'            task.palette.RunClass(colorMask)
'            dst2 = task.palette.dst2
'        Else
'            setTrueText("No objects found")
'        End If
'    End Sub
'End Class






Public Class PointCloud_Singletons : Inherits VBparent
    Public tView As New TimeView_Basics
    Public Sub New()
        labels(2) = "Top down view before inrange sampling"
        labels(3) = "Histogram after filtering for single-only histogram bins"
        task.desc = "Find floor and ceiling using gravity aligned top-down view and selecting bins with exactly 1 sample"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static frameSlider = findSlider("Number of frames to include")
        Dim frameCount = frameSlider.value

        tView.RunClass(src)
        dst2 = tView.dst3

        cv.Cv2.InRange(tView.topView.originalHistOutput, frameCount, frameCount, dst3)
        Dim mask = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
    End Sub
End Class










Public Class PointCloud_SingletonRegions : Inherits VBparent
    Public topView As New Histogram_TopView2D
    Dim dilate As New DilateErode_Basics
    Public Sub New()
        task.hist3DThreshold = 1

        labels(2) = "Top down view before inrange sampling"
        labels(3) = "Histogram after filtering for single-only histogram bins"
        task.desc = "Find floor and ceiling using gravity aligned top-down view and selecting bins with exactly 1 sample"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        topView.RunClass(src)
        cv.Cv2.InRange(topView.originalHistOutput, 1, 1, dst3)
        Dim mask = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        dilate.RunClass(dst3.Clone)
        dst2 = dilate.dst2
    End Sub
End Class







Public Class PointCloud_TrackerTop : Inherits VBparent
    Public pTrack As New KNN_PointTracker
    Public flood As New FloodFill_Palette
    Public timeView As New TimeView_Basics
    Public setupTop As New PointCloud_SetupTop
    Public Sub New()
        findSlider("FloodFill Minimum Size").Value = 100
        task.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 3
        timeView.RunClass(src)
        src = timeView.dst3
        flood.RunClass(src.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255))

        If flood.dst2.Channels = 3 Then src = flood.dst2 Else src = flood.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        pTrack.queryPoints = New List(Of cv.Point2f)(flood.basics.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.basics.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.basics.masks)
        pTrack.RunClass(src)

        setupTop.RunClass(pTrack.dst2)
        dst2 = setupTop.dst2
        setTrueText(Format(dst2.Height / task.maxZ, "0") + " pixels per meter with maxZ at " + Format(task.maxZ, "0.0") + " meters", 10, dst3.Height - 50, 3)
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
        sideView.RunClass(sideView.timeView.dst2)
        dst2 = sideView.dst2
        labels(2) = Format(dst2.Width / task.maxZ, "0") + " pixels per meter with maxZ at " + Format(task.maxZ, "0.0") + " meters"

        topView.RunClass(sideView.timeView.dst3)
        dst3 = topView.dst2
        labels(3) = Format(dst2.Height / task.maxZ, "0") + " pixels per meter with maxZ at " + Format(task.maxZ, "0.0") + " meters"
    End Sub
End Class






Public Class PointCloud_TrackerSide : Inherits VBparent
    Public pTrack As New KNN_PointTracker
    Public flood As New FloodFill_Palette
    Public timeView As New TimeView_Basics
    Public setupSide As New PointCloud_SetupSide
    Public Sub New()
        findSlider("FloodFill Minimum Size").Value = 100
        task.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 3
        timeView.RunClass(src)
        src = timeView.dst2

        flood.RunClass(src.Threshold(task.hist3DThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255))

        If flood.dst2.Channels = 3 Then src = flood.dst2 Else src = flood.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        pTrack.queryPoints = New List(Of cv.Point2f)(flood.basics.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.basics.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.basics.masks)
        pTrack.RunClass(src)

        setupSide.RunClass(pTrack.dst2)
        dst2 = setupSide.dst2
        setTrueText(Format(dst2.Width / task.maxZ, "0") + " pixels per meter with maxZ at " + Format(task.maxZ, "0.0") + " meters", 10, 40, 2)
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
        measureTop.RunClass(src)
        dst2 = measureTop.dst2

        labels(2) = "Pixels/Meter horizontal: " + CStr(CInt(dst2.Width / task.maxZ)) + " vertical: " + CStr(CInt(dst2.Height / task.maxZ))

        Dim FOV = task.hFov / 2

        Dim xpt1 As cv.Point2f, xpt2 As cv.Point2f
        If standalone Then
            Dim pixeldistance = src.Height * (distanceSlider.Value / 1000) / task.maxZ
            Dim lineHalf = CInt(Math.Tan(FOV * 0.0174533) * pixeldistance)

            xpt1 = New cv.Point2f(task.topCameraPoint.X - lineHalf, src.Height - pixeldistance)
            xpt2 = New cv.Point2f(task.topCameraPoint.X + lineHalf, src.Height - pixeldistance)
            If drawLines Then dst2.Line(xpt1, xpt2, cv.Scalar.Blue, task.lineWidth + 2)
            Dim lineWidth = xpt2.X - xpt1.X
            Dim blueLineMeters = 0.0
            If lineWidth = 0 Then
                lineWidth = 1
            Else
                blueLineMeters = distanceSlider.Value * lineWidth / (1000 * pixeldistance)
            End If
            setTrueText("Blue Line is " + CStr(pixeldistance) + " pixels from the camera" + vbCrLf +
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

            Dim coneWidth = dst2.Width / (Math.Max(xpt2.X, dst2.Width) - Math.Max(xpt1.X, 0))
            Dim drawPt1 = New cv.Point2f(coneleft, r.Y + r.Height)
            Dim drawpt2 = New cv.Point2f(coneRight, r.Y + r.Height)

            If lineHalf = 0 Then Continue For
            If drawLines Then dst2.Line(drawPt1, drawpt2, cv.Scalar.Yellow, task.lineWidth + 2)

            Dim vo = measureTop.pTrack.drawRC.viewObjects.Values(i)
            Dim addlen As Single = 0
            ' need to add a small amount to the object width in pixels based on the angle to the camera of the back edge
            If Not (task.topCameraPoint.X > r.X And task.topCameraPoint.X < r.X + r.Width) Then
                If r.X > task.topCameraPoint.X Then
                    addlen = r.Height * Math.Abs(r.X - task.topCameraPoint.X) / (src.Height - r.Y)
                    If drawLines Then dst2.Line(New cv.Point2f(r.X, r.Y + r.Height), New cv.Point2f(r.X - addlen, r.Y + r.Height), cv.Scalar.Yellow, task.lineWidth + 2)
                    coneleft -= addlen
                Else
                    addlen = r.Height * (task.topCameraPoint.X - (r.X + r.Width)) / (src.Height - r.Y)
                    If drawLines Then dst2.Line(New cv.Point2f(r.X + r.Width, r.Y + r.Height), New cv.Point2f(r.X + r.Width + addlen, r.Y + r.Height), cv.Scalar.Yellow, task.lineWidth + 2)
                    If coneleft - addlen >= xpt1.X Then coneleft -= addlen
                End If
            End If
            Dim newX = (coneleft - xpt1.X) * src.Width / (lineHalf * 2)
            Dim newWidth = src.Width * (addlen + coneRight - coneleft) / (lineHalf * 2)
            vo.rectFront = New cv.Rect(newX, r.Y, newWidth, r.Height)
            viewObjects.Add(vo.rectFront.Width * vo.rectFront.Height, vo)
        Next
        If standalone Or task.intermediateActive Or colorizeNeeded Then
            setupTop.RunClass(dst2)
            dst2 = setupTop.dst2
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
        measureSide.RunClass(src)
        dst2 = measureSide.dst2

        labels(2) = "Pixels/Meter horizontal: " + CStr(CInt(dst2.Width / task.maxZ)) + " vertical: " + CStr(CInt(dst2.Height / task.maxZ))
        Dim FOV = task.vFov / 2

        Dim xpt1 As cv.Point2f, xpt2 As cv.Point2f
        If standalone Then
            Dim pixeldistance = src.Width * (distanceSlider.Value / 1000) / task.maxZ
            Dim lineHalf = CInt(Math.Tan(FOV * 0.0174533) * pixeldistance)

            xpt1 = New cv.Point2f(task.sideCameraPoint.X + pixeldistance, task.sideCameraPoint.Y - lineHalf)
            xpt2 = New cv.Point2f(task.sideCameraPoint.X + pixeldistance, task.sideCameraPoint.Y + lineHalf)
            If drawLines Then dst2.Line(xpt1, xpt2, cv.Scalar.Blue, task.lineWidth + 2)
            Dim lineWidth = xpt2.Y - xpt1.Y
            Dim blueLineMeters = 0.0
            If lineWidth = 0 Then
                lineWidth = 1
            Else
                blueLineMeters = distanceSlider.Value * lineWidth / (1000 * pixeldistance)
            End If
            setTrueText("Blue Line is " + CStr(pixeldistance) + " pixels from the camera" + vbCrLf +
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

            Dim coneWidth = dst2.Width / (Math.Max(xpt2.X, dst2.Width) - Math.Max(xpt1.X, 0))
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
            If drawLines Then dst2.Line(drawPt1, drawpt2, cv.Scalar.Yellow, task.lineWidth + 2)

            Dim vo = measureSide.pTrack.drawRC.viewObjects.Values(i)
            Dim addlen As Single = 0
            ' need to add a small amount to the object width in pixels based on the angle to the camera of the back edge
            If Not (task.sideCameraPoint.Y > r.Y And task.sideCameraPoint.Y < r.Y + r.Height) Then
                If r.Y > task.sideCameraPoint.Y Then
                    addlen = r.Width * (r.Y - task.sideCameraPoint.Y) / (r.X + r.Width - task.sideCameraPoint.X)
                    If drawLines Then dst2.Line(New cv.Point2f(r.X, r.Y), New cv.Point2f(r.X, r.Y - addlen), cv.Scalar.Yellow, 3)
                    r = New cv.Rect(r.X, r.Y - addlen, r.Width, coneRight - coneleft - addlen)
                    If coneRight - addlen >= xpt2.Y Then coneRight -= addlen
                Else
                    addlen = r.Width * (task.sideCameraPoint.Y - r.Y) / (r.X + r.Width - task.sideCameraPoint.X)
                    If drawLines Then dst2.Line(New cv.Point2f(r.X, r.Y + r.Height), New cv.Point2f(r.X, r.Y + r.Height + addlen), cv.Scalar.Yellow, task.lineWidth + 2)
                    r = New cv.Rect(r.X, r.Y + addlen, r.Width, coneRight - coneleft + addlen)
                    coneleft += addlen
                End If
            End If
            Dim newY = (coneleft - xpt1.Y) * src.Height / (lineHalf * 2)
            Dim newHeight = src.Height * (addlen + coneRight - coneleft) / (lineHalf * 2)
            vo.rectFront = New cv.Rect(r.X, newY, r.Width, newHeight)
            viewObjects.Add(vo.rectFront.Width * vo.rectFront.Height, vo)
        Next
        If standalone Or task.intermediateActive Then
            setupSide.RunClass(dst2)
            dst2 = setupSide.dst2
        End If
    End Sub
End Class







Public Class PointCloud_SurfaceH_CPP : Inherits VBparent
    Public tView As New TimeView_Basics
    Public plot As New Plot_Basics_CPP
    Public topRow As Integer
    Public botRow As Integer
    Public peakRow As Integer
    Public Sub New()
        task.desc = "Find the horizontal surfaces with a projects of the SideView histogram."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        tView.RunClass(src)
        dst2 = tView.dst2

        ReDim plot.srcX(dst2.Height - 1)
        ReDim plot.srcY(dst2.Height - 1)
        topRow = 0
        botRow = 0
        peakRow = 0
        Dim peakVal As Integer
        For i = 0 To dst2.Height - 1
            plot.srcX(i) = i
            If dst2.Channels = 1 Then plot.srcY(i) = dst2.Row(i).CountNonZero() Else plot.srcY(i) = dst2.Row(i).CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero()
            If peakVal < plot.srcY(i) Then
                peakVal = plot.srcY(i)
                peakRow = i
            End If
            If topRow = 0 And plot.srcY(i) > 10 Then topRow = i
        Next

        For i = plot.srcY.Count - 1 To 0 Step -1
            If botRow = 0 And plot.srcY(i) > 10 Then botRow = i
        Next
        plot.RunClass(Nothing)
        dst3 = plot.dst2.Transpose()
        dst3 = dst3.Flip(cv.FlipMode.Y)
        labels(2) = "Top row = " + CStr(topRow) + " peak row = " + CStr(peakRow) + " bottom row = " + CStr(botRow)
    End Sub
End Class







Public Class PointCloud_SurfaceH : Inherits VBparent
    Public tView As New TimeView_Basics
    Public plot As New Plot_Histogram
    Public topRow As Integer
    Public botRow As Integer
    Public peakRow As Integer
    Public Sub New()
        task.desc = "Find the horizontal surfaces with a projects of the SideView histogram."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        tView.RunClass(src)
        dst2 = tView.dst2
        plot.hist = New cv.Mat(dst2.Height, 1, cv.MatType.CV_32F, 0)
        Dim indexer = plot.hist.GetGenericIndexer(Of Single)()

        topRow = 0
        botRow = 0
        peakRow = 0
        Dim peakVal As Integer
        If dst2.Channels <> 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        For i = 0 To dst2.Height - 1
            indexer(i) = dst2.Row(i).CountNonZero()
            If peakVal < indexer(i) Then
                peakVal = indexer(i)
                peakRow = i
            End If
            If topRow = 0 And indexer(i) > 10 Then topRow = i
        Next

        plot.plotMaxValue = (Math.Floor(peakVal / 100) + 1) * 100
        For i = plot.hist.Rows - 1 To 0 Step -1
            If botRow = 0 And indexer(i) > 10 Then botRow = i
        Next
        plot.RunClass(src)
        dst3 = plot.dst2.Transpose()
        dst3 = dst3.Flip(cv.FlipMode.Y)
        labels(2) = "Top row = " + CStr(topRow) + " peak row = " + CStr(peakRow) + " bottom row = " + CStr(botRow)

        Dim ratio = task.mousePoint.Y / dst2.Height
        Dim offset = ratio * dst3.Height
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2.Line(New cv.Point(0, task.mousePoint.Y), New cv.Point(dst2.Width, task.mousePoint.Y), cv.Scalar.Yellow, task.lineWidth)
        dst3.Line(New cv.Point(0, offset), New cv.Point(dst3.Width, offset), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class







Public Class PointCloud_Neighbor_Options : Inherits VBparent
    Public thresholdSlider As Windows.Forms.TrackBar
    Public pixelSlider As Windows.Forms.TrackBar
    Public errorSlider As Windows.Forms.TrackBar
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Difference from neighbor in mm's", 0, 20, 5)
            sliders.setupTrackBar(1, "Minimum offset to neighbor pixel", 1, 300, 10)
            sliders.setupTrackBar(2, "Z-Error tolerance from vertical/horizontal in mm's", 1, 50, 20)
        End If
        thresholdSlider = findSlider("Difference from neighbor in mm's")
        pixelSlider = findSlider("Minimum offset to neighbor pixel")
        errorSlider = findSlider("Z-Error tolerance from vertical/horizontal in mm's")
        task.desc = "Display options for PointCloud_Neighbor algorithms."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        setTrueText("PointCloud_NeighborOptions has no output - just options for PointCloud_Neighbor algorithms")
    End Sub
End Class








Public Class PointCloud_NeighborV : Inherits VBparent
    Dim options As New PointCloud_Neighbor_Options
    Public Sub New()
        task.desc = "Show where vertical neighbor depth values are within Y mm's"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim shift = options.pixelSlider.Value
        Dim shiftZ = options.thresholdSlider.Value
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f

        Dim tmp32f = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        Dim r1 = New cv.Rect(shift, 0, dst2.Width - shift, dst2.Height)
        Dim r2 = New cv.Rect(0, 0, dst2.Width - shift, dst2.Height)
        cv.Cv2.Absdiff(src(r1), src(r2), tmp32f(r1))
        tmp32f = tmp32f.Threshold(shiftZ, 255, cv.ThresholdTypes.BinaryInv)
        dst2 = tmp32f.ConvertScaleAbs(255)
        dst2.SetTo(0, task.noDepthMask)
        dst2(New cv.Rect(0, dst2.Height - shift, dst2.Width, shift)).SetTo(0)
        labels(2) = "White: z is within " + CStr(shiftZ) + " mm's with Y pixel offset " + CStr(shift)
    End Sub
End Class






Public Class PointCloud_NeighborH : Inherits VBparent
    Dim options As New PointCloud_Neighbor_Options
    Public Sub New()
        task.desc = "Show where horizontal neighbor depth values are within X mm's"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim shift = options.pixelSlider.Value
        Dim shiftZ = options.thresholdSlider.Value
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f

        Dim tmp32f = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        Dim r1 = New cv.Rect(0, 0, dst2.Width, dst2.Height - shift)
        Dim r2 = New cv.Rect(0, shift, dst2.Width, dst2.Height - shift)
        cv.Cv2.Absdiff(src(r1), src(r2), tmp32f(r1))
        tmp32f = tmp32f.Threshold(shiftZ, 255, cv.ThresholdTypes.BinaryInv)
        dst2 = tmp32f.ConvertScaleAbs(255)
        dst2.SetTo(0, task.noDepthMask)
        dst2(New cv.Rect(dst2.Width - shift, 0, shift, dst2.Height)).SetTo(0)
        labels(2) = "White: z is within " + CStr(shiftZ) + " mm's with X pixel offset " + CStr(shift)
    End Sub
End Class








Public Class PointCloud_NeighborsH : Inherits VBparent
    Public options As New PointCloud_Neighbor_Options
    Public pt1 As New List(Of cv.Point)
    Public pt2 As New List(Of cv.Point)
    Public Sub New()
        options.pixelSlider.Value = options.pixelSlider.Maximum
        task.desc = "Manual step through depth data to find horizontal neighbors within x mm's"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim shift = options.pixelSlider.Value
        Dim shiftZ = options.thresholdSlider.Value
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f

        pt1.Clear()
        pt2.Clear()
        For y = 0 To src.Height - 1
            For x = 0 To src.Width - shift - 1
                Dim x1 = src.Get(Of Single)(y, x)
                Dim x2 = src.Get(Of Single)(y, x + shift)
                If x1 = 0 Or x2 = 0 Then Continue For
                If Math.Abs(x1 - x2) <= shiftZ Then
                    pt1.Add(New cv.Point(x, y))
                    pt2.Add(New cv.Point(x + shift, y))
                    x += shift
                End If
            Next
        Next

        dst2 = task.color.Clone
        For i = 0 To pt1.Count - 1
            dst2.Line(pt1(i), pt2(i), cv.Scalar.Yellow, task.lineWidth)
        Next
        labels(2) = CStr(pt1.Count) + " z-values within " + CStr(shiftZ) + " mm's with X pixel offset " + CStr(shift)
    End Sub
End Class








Public Class PointCloud_NeighborsV : Inherits VBparent
    Public options As New PointCloud_Neighbor_Options
    Public pt1 As New List(Of cv.Point)
    Public pt2 As New List(Of cv.Point)
    Public Sub New()
        options.pixelSlider.Value = options.pixelSlider.Maximum
        task.desc = "Manual step through depth data to find vertical neighbors within x mm's"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim shift = options.pixelSlider.Value
        Dim shiftZ = options.thresholdSlider.Value
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f

        pt1.Clear()
        pt2.Clear()
        For x = 0 To src.Width - 1
            For y = 0 To src.Height - shift - 1
                Dim x1 = src.Get(Of Single)(y, x)
                Dim x2 = src.Get(Of Single)(y + shift, x)
                If x1 = 0 Or x2 = 0 Then Continue For
                If Math.Abs(x1 - x2) <= shiftZ Then
                    pt1.Add(New cv.Point(x, y))
                    pt2.Add(New cv.Point(x, y + shift))
                    y += shift
                End If
            Next
        Next

        dst2 = task.color.Clone
        For i = 0 To pt1.Count - 1
            dst2.Line(pt1(i), pt2(i), cv.Scalar.Yellow, task.lineWidth)
        Next
        labels(2) = CStr(pt1.Count) + " z-values within " + CStr(shiftZ) + " mm's with Y pixel offset " + CStr(shift)
    End Sub
End Class








Public Class PointCloud_NeighborIMUH : Inherits VBparent
    Dim gCloud As New Depth_PointCloud_IMU
    Dim horiz As New PointCloud_NeighborsH
    Public Sub New()
        horiz.options.thresholdSlider.Value = 1
        horiz.options.pixelSlider.Value = 300
        task.desc = "Use the IMU adjusted cloud to find horizontals"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        gCloud.RunClass(src)

        Dim split = gCloud.dst2.Split()
        horiz.RunClass(split(2) * 1000)
        dst2 = horiz.dst2
        labels(2) = horiz.labels(2)
    End Sub
End Class







Public Class PointCloud_NeighborIMUV : Inherits VBparent
    Dim gCloud As New Depth_PointCloud_IMU
    Dim verticals As New PointCloud_NeighborsV
    Public Sub New()
        verticals.options.thresholdSlider.Value = 1
        verticals.options.pixelSlider.Value = 300
        task.desc = "Use the IMU adjusted cloud to find verticals"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        gCloud.RunClass(src)

        Dim split = gCloud.dst2.Split()
        verticals.RunClass(split(2) * 1000)
        dst2 = verticals.dst2
        labels(2) = verticals.labels(2)
    End Sub
End Class