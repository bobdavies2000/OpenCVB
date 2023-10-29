Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class PointCloud_Basics : Inherits VB_Algorithm
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
                        dst2.Circle(New cv.Point(x, y), task.dotSize, cv.Scalar.White, -1, task.lineType)
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
                        dst2.Circle(New cv.Point(x, y), task.dotSize, cv.Scalar.White, -1, task.lineType)
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

    Public Sub RunVB(src as cv.Mat)
        Static deltaSlider = findSlider("Delta Z threshold (cm)")
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








Public Class PointCloud_Point3f : Inherits VB_Algorithm
    Public Sub New()
        desc = "Display the point cloud CV_32FC3 format"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst2 = task.pointCloud
    End Sub
End Class







Public Class PointCloud_Spin : Inherits VB_Algorithm
    Dim gMat As New IMU_GMatrix
    Public Sub New()
        If findfrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Spin pointcloud on X-axis")
            check.addCheckBox("Spin pointcloud on Y-axis")
            check.addCheckBox("Spin pointcloud on Z-axis")
            check.Box(2).Checked = True
        End If

        gOptions.gravityPointCloud.Checked = False
        desc = "Spin the point cloud exercise"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static xCheck = findCheckBox("Spin pointcloud on X-axis")
        Static yCheck = findCheckBox("Spin pointcloud on Y-axis")
        Static zCheck = findCheckBox("Spin pointcloud on Z-axis")
        Static xRotateSlider = findSlider("Rotate pointcloud around X-axis (degrees)")
        Static yRotateSlider = findSlider("Rotate pointcloud around Y-axis (degrees)")
        Static zRotateSlider = findSlider("Rotate pointcloud around Z-axis (degrees)")

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








Public Class PointCloud_Spin2 : Inherits VB_Algorithm
    Dim spin As New PointCloud_Spin
    Dim redC As New RedBP_Basics
    Dim redCSpin As New RedBP_Basics
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






Public Class PointCloud_Continuous_VB : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Threshold of continuity in mm", 0, 1000, 10)
        End If

        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Show where the pointcloud is continuous"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static thresholdSlider = findSlider("Threshold of continuity in mm")
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







Module PointCloud
    ' for performance we are putting this in an optimized C++ interface to the K4A camera for convenience...
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
            If a >= b Then Return 1
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
    Public Class CompareMaskSize : Implements IComparer(Of Integer)
        Public Function Compare(ByVal a As Integer, ByVal b As Integer) As Integer Implements IComparer(Of Integer).Compare
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
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
Public Class PointCloud_SetupSide : Inherits VB_Algorithm
    Dim arcSize As Integer
    Public xCheckbox As Windows.Forms.CheckBox
    Public zCheckbox As Windows.Forms.CheckBox
    Public Sub New()
        arcSize = dst2.Width / 15
        labels(2) = "Layout markers for side view"
        desc = "Create the colorized mat used for side projections"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim distanceRatio As Single = 1
        If src.Channels <> 3 Then src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If standalone Then dst2.SetTo(0) Else src.CopyTo(dst2)
        dst2.Circle(task.sideCameraPoint, task.dotSize, cv.Scalar.BlueViolet, -1, task.lineType)
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
        If gOptions.gravityPointCloud.Checked Then
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
        If standalone = False Then
            dst2.Circle(markerLeft, task.dotSize, cv.Scalar.Red, -1, task.lineType)
            dst2.Circle(markerRight, task.dotSize, cv.Scalar.Red, -1, task.lineType)
        End If

        ' draw the arc enclosing the camera FOV
        Dim startAngle = (180 - task.vFov) / 2
        Dim y = dst2.Width / Math.Tan(startAngle * cv.Cv2.PI / 180)

        Dim fovTop = New cv.Point(dst2.Width, cam.Y - y)
        Dim fovBot = New cv.Point(dst2.Width, cam.Y + y)

        dst2.Line(cam, fovTop, cv.Scalar.White, 1, task.lineType)
        dst2.Line(cam, fovBot, cv.Scalar.White, 1, task.lineType)

        dst2.Circle(markerLeft, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        dst2.Circle(markerRight, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
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
Public Class PointCloud_SetupTop : Inherits VB_Algorithm
    Dim arcSize As Integer
    Public Sub New()
        arcSize = dst2.Width / 15

        labels(2) = "Layout markers for top view"
        desc = "Create the colorize the mat for a topdown projections"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim distanceRatio As Single = 1
        If src.Channels <> 3 Then src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If standalone Then dst2.SetTo(0) Else src.CopyTo(dst2)
        dst2.Circle(task.topCameraPoint, task.dotSize, cv.Scalar.BlueViolet, -1, task.lineType)
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

        Static zRotateSlider = findSlider("Rotate pointcloud around Z-axis (degrees)")
        Dim offset = Math.Sin(task.accRadians.Z) * topLen
        If gOptions.gravityPointCloud.Checked Then
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

        dst2.Circle(markerLeft, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        dst2.Circle(markerRight, task.dotSize + 3, cv.Scalar.Red, -1, task.lineType)
        dst2.Line(cam, markerLeft, cv.Scalar.Red, 1, task.lineType)
        dst2.Line(cam, markerRight, cv.Scalar.Red, 1, task.lineType)

        Dim shift = (src.Width - src.Height) / 2
        Dim labelLocation = New cv.Point(dst2.Width / 2 + shift, dst2.Height * 15 / 16)
        setTrueText("hFOV=" + Format(180 - startAngle * 2, "0.0") + " deg.", New cv.Point(4, dst2.Height * 7 / 8))
        dst2.Line(task.topCameraPoint, fovRight, cv.Scalar.White, task.lineWidth, task.lineType)
    End Sub
End Class







Public Class PointCloud_Raw_CPP : Inherits VB_Algorithm
    Dim depthBytes() As Byte
    Public Sub New()
        labels(2) = "Top View"
        labels(3) = "Side View"
        desc = "Project the depth data onto a top view And side view."
        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If firstPass Then ReDim depthBytes(task.pcSplit(2).Total * task.pcSplit(2).ElemSize - 1)

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





Public Class PointCloud_Raw : Inherits VB_Algorithm
    Dim depthBytes() As Byte
    Public Sub New()
        labels(2) = "Top View"
        labels(3) = "Side View"
        desc = "Project the depth data onto a top view And side view - Using only VB code (too slow.)"
        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub RunVB(src as cv.Mat)
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







Public Class PointCloud_Solo : Inherits VB_Algorithm
    Public heat As New HeatMap_Basics
    Public Sub New()
        findCheckBox("Top View (Unchecked Side View)").Checked = True
        findCheckBox("Show Frustrum").Checked = False
        labels(2) = "Top down view after inrange sampling"
        labels(3) = "Histogram after filtering For Single-only histogram bins"
        desc = "Find floor And ceiling Using gravity aligned top-down view And selecting bins With exactly 1 sample"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        heat.Run(src)
        dst2 = heat.dst0.InRange(task.historyCount, task.historyCount).ConvertScaleAbs
        dst3 = heat.dst1.InRange(task.historyCount, task.historyCount).ConvertScaleAbs
    End Sub
End Class






Public Class PointCloud_SoloRegions : Inherits VB_Algorithm
    Public solo As New PointCloud_Solo
    Dim dilate As New Dilate_Basics
    Public Sub New()
        labels(2) = "Top down view before inrange sampling"
        labels(3) = "Histogram after filtering For Single-only histogram bins"
        desc = "Find floor And ceiling Using gravity aligned top-down view And selecting bins With exactly 1 sample"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        solo.Run(src)
        dst2 = solo.dst2
        dst3 = solo.dst3
        dilate.Run(dst3.Clone)
        dst3 = dilate.dst2
    End Sub
End Class








Public Class PointCloud_SurfaceH_CPP : Inherits VB_Algorithm
    Public heat As New HeatMap_Basics
    Public plot As New Plot_Basics_CPP
    Public topRow As Integer
    Public botRow As Integer
    Public peakRow As Integer
    Public Sub New()
        findCheckBox("Show Frustrum").Checked = False
        desc = "Find the horizontal surfaces With a projects Of the SideView histogram."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        heat.Run(src)
        dst2 = heat.dst3

        ReDim plot.srcX(dst2.Height - 1)
        ReDim plot.srcY(dst2.Height - 1)
        topRow = 0
        botRow = 0
        peakRow = 0
        Dim peakVal As Integer
        For i = 0 To dst2.Height - 1
            plot.srcX(i) = i
            If dst2.Channels = 1 Then plot.srcY(i) = dst2.Row(i).CountNonZero Else plot.srcY(i) = dst2.Row(i).CvtColor(cv.ColorConversionCodes.BGR2GRAY).CountNonZero
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
        dst3 = plot.dst2.Transpose()
        dst3 = dst3.Flip(cv.FlipMode.Y)
        labels(2) = "Top row = " + CStr(topRow) + " peak row = " + CStr(peakRow) + " bottom row = " + CStr(botRow)
    End Sub
End Class







Public Class PointCloud_SurfaceH : Inherits VB_Algorithm
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
    Public Sub RunVB(src as cv.Mat)
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
        dst2.Line(New cv.Point(0, task.mouseMovePoint.Y), New cv.Point(dst2.Width, task.mouseMovePoint.Y), cv.Scalar.Yellow, task.lineWidth)
        dst3.Line(New cv.Point(0, offset), New cv.Point(dst3.Width, offset), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class









Public Class PointCloud_NeighborV : Inherits VB_Algorithm
    Dim options As New Options_Neighbors
    Public Sub New()
        desc = "Show where vertical neighbor depth values are within Y mm's"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
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








Public Class PointCloud_ReducedTopView : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Dim histOutput As New cv.Mat
    Public Sub New()
        desc = "Create a stable side view of the point cloud"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        src = task.pcSplit(2) * 1000
        src.ConvertTo(src, cv.MatType.CV_32S)
        reduction.Run(src)
        reduction.dst2.ConvertTo(task.pcSplit(2), cv.MatType.CV_32F)
        task.pcSplit(2) *= 0.001
        cv.Cv2.Merge(task.pcSplit, dst3)

        cv.Cv2.CalcHist({dst3}, task.channelsTop, New cv.Mat, histOutput, 2, task.bins2D, task.rangesTop)

        histOutput = histOutput.Flip(cv.FlipMode.X)
        dst2 = histOutput.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class





Public Class PointCloud_Visualize : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "", "Pointcloud visualized", ""}
        desc = "Display the pointcloud as a BGR image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim pcSplit = {task.pcSplit(0).ConvertScaleAbs(255), task.pcSplit(1).ConvertScaleAbs(255), task.pcSplit(2).ConvertScaleAbs(255)}
        cv.Cv2.Merge(pcSplit, dst2)
    End Sub
End Class







Public Class PointCloud_PCpointsMask : Inherits VB_Algorithm
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
                    dst2.Circle(New cv.Point(roi.X, roi.Y), task.dotSize * Math.Max(mean(2), 1), cv.Scalar.White, -1, task.lineType)
                End If
                lastMeanZ = mean(2)
            Next
        Next
        labels(2) = "PointCloud Point Points found = " + CStr(actualCount)
    End Sub
End Class







Public Class PointCloud_PCPoints : Inherits VB_Algorithm
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
                dst2.Circle(pt, task.dotSize, Choose(pt.Y Mod 3 + 1, red, blue, white), -1, task.lineType)
            End If
        Next
        labels(2) = "PointCloud Point Points found = " + CStr(pcPoints.Count / 2)
    End Sub
End Class








Public Class PointCloud_PCPointsPlane : Inherits VB_Algorithm
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








Public Class PointCloud_Inspector : Inherits VB_Algorithm
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
        dst2.Line(topPt, botPt, 255, task.lineWidth, task.lineType)

        Dim stepY = dst2.Height / yLines
        setTrueText(vbTab + "   X" + vbTab + "  Y" + vbTab + "  Z", 3)
        For i = 1 To yLines - 1
            Dim pt1 = New cv.Point2f(dst2.Width, i * stepY)
            Dim pt2 = New cv.Point2f(0, i * stepY)
            dst2.Line(pt1, pt2, cv.Scalar.White, task.lineWidth, task.lineType)

            Dim pt = New cv.Point2f(cLine, i * stepY)
            Dim xyz = task.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X)
            setTrueText("Row " + CStr(i) + vbTab + Format(xyz(0), fmt2) + vbTab + Format(xyz(1), fmt2) + vbTab + Format(xyz(2), fmt2), New cv.Point(5, pt.Y), 3)
        Next
        labels(2) = "Values displayed are the point cloud X, Y, and Z values for column " + CStr(cLine)
        labels(3) = "Move mouse in the image at left to see the point cloud X, Y, and Z values."
    End Sub
End Class









Public Class PointCloud_Average : Inherits VB_Algorithm
    Dim pcHistory As New List(Of cv.Mat)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32FC3, 0)
        desc = "Average all 3 elements of the point cloud - not just depth."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        pcHistory.Add(task.pointCloud)
        If pcHistory.Count >= task.historyCount Then pcHistory.RemoveAt(0)

        dst3.SetTo(0)
        For Each m In pcHistory
            dst3 += m
        Next
        dst3 *= 1 / pcHistory.Count
    End Sub
End Class






Public Class PointCloud_FrustrumTop : Inherits VB_Algorithm
    Dim frustrum As New Draw_Frustrum
    Dim heat As New HeatMap_Basics
    Dim setupTop As New PointCloud_SetupTop
    Public Sub New()
        gOptions.gravityPointCloud.Checked = False
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








Public Class PointCloud_FrustrumSide : Inherits VB_Algorithm
    Dim frustrum As New Draw_Frustrum
    Dim heat As New HeatMap_Basics
    Dim setupSide As New PointCloud_SetupSide
    Public Sub New()
        gOptions.gravityPointCloud.Checked = False
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






Public Class PointCloud_ReducedSideView : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Show where vertical neighbor depth values are within X mm's"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        src = task.pcSplit(2) * 1000
        src.ConvertTo(src, cv.MatType.CV_32S)
        reduction.Run(src)
        reduction.dst2.ConvertTo(task.pcSplit(2), cv.MatType.CV_32F)
        task.pcSplit(2) *= 0.001
        cv.Cv2.Merge(task.pcSplit, dst3)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange), New cv.Rangef(0, task.maxZmeters)}
        cv.Cv2.CalcHist({dst3}, task.channelsSide, New cv.Mat, dst1, 2, task.bins2D, task.rangesSide)

        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class






Public Class PointCloud_YRangeTest : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Test adjusting the Y-Range value to squeeze a histogram into dst2."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst0 = task.pcSplit(2) * 1000
        dst0.ConvertTo(dst0, cv.MatType.CV_32S)
        reduction.Run(dst0)
        reduction.dst2.ConvertTo(task.pcSplit(2), cv.MatType.CV_32F)
        task.pcSplit(2) *= 0.001
        cv.Cv2.Merge(task.pcSplit, dst3)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange),
                                        New cv.Rangef(0, task.maxZmeters)}
        cv.Cv2.CalcHist({dst3}, task.channelsSide, New cv.Mat, dst1, 2, task.bins2D, task.rangesSide)

        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1.ConvertTo(dst2, cv.MatType.CV_8UC1)
    End Sub
End Class