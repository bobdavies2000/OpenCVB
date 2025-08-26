Imports System.IO
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports System.Threading
Public Class XO_Model_Basics : Inherits TaskParent
    Dim oglM As New XO_OpenGL_BasicsMouse
    Public Sub New()
        labels = {"", "", "Captured OpenGL output", ""}
        desc = "Capture the output of the OpenGL window"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then oglM.Run(src)
        dst2 = oglM.dst2
        dst3 = oglM.dst3
    End Sub
End Class








Public Class XO_Model_FlatSurfaces : Inherits TaskParent
    Public totalPixels As Integer
    Dim floorList As New List(Of Single)
    Dim ceilingList As New List(Of Single)
    Public Sub New()
        desc = "Minimalist approach to find a flat surface that is oriented to gravity (floor or ceiling)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange), New cv.Rangef(0, task.MaxZmeters)}
        cv.Cv2.CalcHist({task.pointCloud}, {1, 2}, New cv.Mat, dst0, 2,
                        {dst2.Height, dst2.Width}, ranges)

        Dim thicknessCMs = 0.1, rect As cv.Rect, nextY As Single
        totalPixels = 0
        For y = dst0.Height - 2 To 0 Step -1
            rect = New cv.Rect(0, y, dst0.Width - 1, 1)
            Dim count = dst0(rect).CountNonZero
            Dim pixelCount = dst0(rect).Sum()
            totalPixels += pixelCount.Val0
            If count > 10 Then
                nextY = -task.yRange * (task.sideCameraPoint.Y - y) / task.sideCameraPoint.Y - thicknessCMs
                Exit For
            End If
        Next

        Dim floorY = rect.Y
        floorList.Add(nextY)
        task.pcFloor = floorList.Average()
        If floorList.Count > task.frameHistoryCount Then floorList.RemoveAt(0)
        labels(2) = "Y = " + Format(task.pcFloor, fmt3) + " separates the floor.  Total pixels below floor level = " + Format(totalPixels, fmt0)

        For y = 0 To dst2.Height - 1
            rect = New cv.Rect(0, y, dst0.Width - 1, 1)
            Dim count = dst0(rect).CountNonZero
            Dim pixelCount = dst0(rect).Sum()
            totalPixels += pixelCount.Val0
            If count > 10 Then
                nextY = -task.yRange * (task.sideCameraPoint.Y - y) / task.sideCameraPoint.Y - thicknessCMs
                Exit For
            End If
        Next

        Dim ceilingY = rect.Y
        ceilingList.Add(nextY)
        task.pcCeiling = ceilingList.Average()
        If ceilingList.Count > task.frameHistoryCount Then ceilingList.RemoveAt(0)
        labels(3) = "Y = " + Format(task.pcCeiling, fmt3) + " separates the ceiling.  Total pixels above ceiling level = " + Format(totalPixels, fmt0)

        If standaloneTest() Then
            dst2 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)
            dst2.ConvertTo(dst2, cv.MatType.CV_8U)
            dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst2.Line(New cv.Point(0, floorY), New cv.Point(dst2.Width, floorY), cv.Scalar.Red, task.lineWidth + 2, task.lineType)
            dst2.Line(New cv.Point(0, ceilingY), New cv.Point(dst2.Width, ceilingY), cv.Scalar.Red, task.lineWidth + 2, task.lineType)
        End If
    End Sub
End Class







Public Class XO_Model_RedCloud : Inherits TaskParent
    Public oglD As New XO_OpenGL_DrawHulls
    Public Sub New()
        labels = {"", "", "OpenGL output", "RedCloud Output"}
        desc = "Capture the OpenGL output of the drawn cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        oglD.Run(src)
        dst2 = oglD.dst2
    End Sub
End Class








Public Class XO_Model_CellZoom : Inherits TaskParent
    Dim oglData As New XO_Model_RedCloud
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "", "RedColor_Hull output", "Selected cell in 3D"}
        desc = "Zoom in on the selected RedCloud cell in the OpenGL output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        oglData.Run(src)
        dst2 = oglData.dst2
        dst3 = oglData.oglD.dst3

        Dim rcX = task.rcD

        dst1.SetTo(0)
        Dim mask = dst3.InRange(white, white)

        dst3.CopyTo(dst1, mask)
        Dim points = mask.FindNonZero()
        If points.Rows > 0 Then
            Dim split = points.Split()
            Dim mmX = GetMinMax(split(0))
            Dim mmY = GetMinMax(split(1))

            Dim r = New cv.Rect(mmX.minVal, mmY.minVal, mmX.maxVal - mmX.minVal, mmY.maxVal - mmY.minVal)
            dst1.Rectangle(r, white, 1, task.lineType)
        End If
    End Sub
End Class




Public Class XO_OpenGL_Basics : Inherits TaskParent
    Dim memMapWriter As MemoryMappedViewAccessor
    Dim startInfo As New ProcessStartInfo
    Dim memMapPtr As IntPtr
    Public dataInput As New cv.Mat
    Public pointCloudInput As cv.Mat
    Public oglFunction As Integer = 0 ' the default function is to display a point cloud.
    Public options1 As New Options_OpenGL1
    Public options2 As New Options_OpenGL2
    Public options3 As New Options_OpenGL3
    Public options4 As New Options_OpenGL4
    Dim rgbBuffer(0) As Byte
    Dim dataBuffer(0) As Byte
    Dim pointCloudBuffer(0) As Byte
    Public Sub New()
        task.OpenGLTitle = "OpenGL_Functions"
        pointCloudInput = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC3, 0)
        desc = "Create an OpenGL window and update it with images"
    End Sub
    Private Function memMapFill() As Double()
        Dim timeConversionUnits As Double = 1000
        Dim imuAlphaFactor As Double = 0.98 ' theta is a mix of acceleration data and gyro data.
        If task.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then
            timeConversionUnits = 1000 * 1000
            imuAlphaFactor = 0.99
        End If
        Dim rgbBufferSize = 0
        If rgbBuffer.Length > 1 Then rgbBufferSize = rgbBuffer.Length
        Dim dataBufferSize = 0
        If dataBuffer.Length > 1 Then dataBufferSize = dataBuffer.Length
        Dim showXYZaxis = True
        Dim memMapValues() As Double =
            {task.frameCount, dst2.Width, dst2.Height, rgbBufferSize,
            dataBufferSize, options2.FOV, options1.yaw, options1.pitch, options1.roll,
            options3.zNear, options3.zFar, options3.pointSize, dataInput.Width, dataInput.Height,
            task.IMU_AngularVelocity.X, task.IMU_AngularVelocity.Y, task.IMU_AngularVelocity.Z,
            task.IMU_Acceleration.X, task.IMU_Acceleration.Y, task.IMU_Acceleration.Z, task.IMU_TimeStamp,
            1, options2.eye(0) / 100, options2.eye(1) / 100, options2.eye(2) / 100, options3.zTrans,
            options4.scaleXYZ(0) / 10, options4.scaleXYZ(1) / 10, options4.scaleXYZ(2) / 10,
            timeConversionUnits, imuAlphaFactor, task.OpenGLTitle.Length,
            pointCloudInput.Width, pointCloudInput.Height, oglFunction, showXYZaxis,
            options1.pcBufferCount}
        Return memMapValues
    End Function
    Private Sub MemMapUpdate()
        Dim memMap = memMapFill()
        Marshal.Copy(memMap, 0, memMapPtr, memMap.Length)
        memMapWriter.WriteArray(Of Double)(0, memMap, 0, memMap.Length)
    End Sub
    Private Sub StartOpenGLWindow()
        task.pipeName = "OpenCVBImages" + CStr(pipeCount)
        Try
            task.openGLPipe = New NamedPipeServerStream(task.pipeName, PipeDirection.InOut, 1)
        Catch ex As Exception
        End Try
        pipeCount += 1

        Dim memMap = memMapFill()
        Dim memMapbufferSize = 8 * memMap.Length

        startInfo.FileName = task.HomeDir + "bin\Release\" + task.OpenGLTitle + ".exe"

        Dim windowWidth = 720
        Dim windowHeight = 720 * 240 / 320
        startInfo.Arguments = CStr(windowWidth) + " " + CStr(windowHeight) + " " + CStr(memMapbufferSize) + " " +
                              task.pipeName
        If task.showConsoleLog = False Then startInfo.WindowStyle = ProcessWindowStyle.Hidden
        Dim proc = Process.Start(startInfo)
        'While task.openGL_hwnd = 0
        '    task.openGL_hwnd = proc.MainWindowHandle
        '    If task.openGL_hwnd <> 0 Then Exit While
        '    Thread.Sleep(100)
        'End While

        memMapPtr = Marshal.AllocHGlobal(memMapbufferSize)
        Dim memMapFile As MemoryMappedFile = MemoryMappedFile.CreateOrOpen("OpenCVBControl", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)

        task.openGLPipe.WaitForConnection()
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then pointCloudInput = task.pointCloud

        options1.Run()
        options2.Run()
        options3.Run()
        options4.Run()

        If pointCloudInput.Width <> 0 And options4.moveAmount <> New cv.Scalar Then
            pointCloudInput -= options4.moveAmount
        End If

        If src.Width > 0 Then
            src = src.CvtColor(cv.ColorConversionCodes.BGR2RGB) ' OpenGL needs RGB, not BGR
            If rgbBuffer.Length <> src.Total * src.ElemSize Then ReDim rgbBuffer(src.Total * src.ElemSize - 1)
        End If

        If dataInput.Width > 0 Then
            If dataBuffer.Length <> dataInput.Total * dataInput.ElemSize Then
                ReDim dataBuffer(dataInput.Total * dataInput.ElemSize - 1)
            End If
        Else
            ReDim dataBuffer(0)
        End If

        If pointCloudInput.Width > 0 Then
            If pointCloudBuffer.Length <> pointCloudInput.Total * pointCloudInput.ElemSize Then
                ReDim pointCloudBuffer(pointCloudInput.Total * pointCloudInput.ElemSize - 1)
            End If
        End If

        If memMapPtr = 0 Then
            StartOpenGLWindow()
        Else
            Dim readPipe(4) As Byte ' we read 4 bytes because that is the signal that the other end of the named pipe wrote 4 bytes to indicate iteration complete.
            If task.openGLPipe IsNot Nothing Then
                Dim bytesRead = task.openGLPipe.Read(readPipe, 0, 4)
                If bytesRead = 0 Then SetTrueText("The OpenGL process appears to have stopped.", New cv.Point(20, 100))
            End If
        End If

        MemMapUpdate()

        If src.Width > 0 Then Marshal.Copy(src.Data, rgbBuffer, 0, rgbBuffer.Length)
        If dataInput.Width > 0 Then Marshal.Copy(dataInput.Data, dataBuffer, 0, dataBuffer.Length)
        If pointCloudInput.Width > 0 Then Marshal.Copy(pointCloudInput.Data, pointCloudBuffer, 0, pointCloudInput.Total * pointCloudInput.ElemSize)

        Try
            If src.Width > 0 Then task.openGLPipe.Write(rgbBuffer, 0, rgbBuffer.Length)
            If dataInput.Width > 0 Then task.openGLPipe.Write(dataBuffer, 0, dataBuffer.Length - 1)
            If pointCloudInput.Width > 0 Then task.openGLPipe.Write(pointCloudBuffer, 0, pointCloudBuffer.Length)

            Dim buff = System.Text.Encoding.UTF8.GetBytes(task.OpenGLTitle)
            task.openGLPipe.Write(buff, 0, task.OpenGLTitle.Length)
        Catch ex As Exception
            ' OpenGL window was likely closed.  
        End Try
        If standaloneTest() Then SetTrueText(task.gmat.strOut, 3)
        If standalone Then dst2 = task.pointCloud
    End Sub
End Class







Public Class XO_OpenGL_BasicsMouse : Inherits TaskParent
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Show the OpenGL point cloud with mouse support."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.testAllRunning Then Exit Sub ' seems to not like it when running overnight but it runs fine.

        Static MotionCheck = OptionParent.FindCheckBox("Use Motion Mask on the pointcloud")
        task.ogl.pointCloudInput = task.pointCloud

        task.ogl.Run(src)
    End Sub
End Class









Public Class XO_OpenGL_ReducedXYZ : Inherits TaskParent
    Dim reduction As New Reduction_XYZ
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Display the pointCloud after reduction in X, Y, or Z dimensions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst3

        task.ogl.pointCloudInput = reduction.dst3
        task.ogl.Run(src)
    End Sub
End Class






Public Class XO_OpenGL_Reduction : Inherits TaskParent
    Dim reduction As Reduction_PointCloud
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        reduction = New Reduction_PointCloud
        desc = "Use the reduced depth pointcloud in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2
        task.ogl.pointCloudInput = reduction.dst3
        task.ogl.Run(src)
    End Sub
End Class






' https://www3.ntu.edu.sg/home/ehchua/programming/opengl/CG_Examples.html
Public Class XO_OpenGL_Pyramid : Inherits TaskParent
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPyramid ' all the work is done inside the switch statement in XO_OpenGL_Functions.
        desc = "Draw the traditional OpenGL pyramid"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.ogl.pointCloudInput = New cv.Mat
        task.ogl.Run(src)
    End Sub
End Class





'https://www3.ntu.edu.sg/home/ehchua/programming/opengl/CG_Examples.html
Public Class XO_OpenGL_DrawCube : Inherits TaskParent
    Public Sub New()
        task.ogl.oglFunction = oCase.drawCube
        desc = "Draw the traditional OpenGL cube"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.ogl.pointCloudInput = New cv.Mat()
        task.ogl.Run(src)
    End Sub
End Class







'https://www3.ntu.edu.sg/home/ehchua/programming/opengl/CG_Examples.html
Public Class XO_OpenGL_QuadHulls : Inherits TaskParent
    Dim quad As New Quad_Hulls
    Public Sub New()
        task.ogl.oglFunction = oCase.quadBasics
        If standalone Then task.gOptions.ColorSource.SelectedItem = "Reduction_Basics"
        desc = "Create a simple plane in each roi of the RedCloud data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        quad.Run(src)
        dst2 = quad.dst2
        dst3 = quad.dst3
        labels = quad.labels
        task.ogl.dataInput = cv.Mat.FromPixelData(quad.quadData.Count, 1, cv.MatType.CV_32FC3, quad.quadData.ToArray)

        task.ogl.pointCloudInput = New cv.Mat()
        task.ogl.Run(dst3)
    End Sub
End Class









Public Class XO_OpenGL_OnlyPlanes : Inherits TaskParent
    Dim planes As New Plane_OnlyPlanes
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        labels = {"", "", "RedCloud Cells", "Planes built in the point cloud"}
        desc = "Display the pointCloud as a set of RedCloud cell planes"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        planes.Run(src)
        dst2 = planes.dst2
        dst3 = planes.dst3
        task.ogl.pointCloudInput = planes.dst3
        task.ogl.Run(task.color)
    End Sub
End Class









Public Class XO_OpenGL_FlatStudy1 : Inherits TaskParent
    Dim plane As New Structured_LinearizeFloor
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        labels = {"", "", "Side view of point cloud - use mouse to highlight the floor", "Highlight the floor in BGR image"}
        desc = "Convert depth cloud floor to a plane and visualize it with OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        plane.Run(src)
        dst2 = plane.dst3
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(plane.dst2)
    End Sub
End Class








Public Class XO_OpenGL_FlatStudy2 : Inherits TaskParent
    Public plane As New Structured_LinearizeFloor
    Public Sub New()
        task.ogl.oglFunction = oCase.drawFloor
        desc = "Show the floor in the pointcloud as a plane"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        plane.Run(src)
        dst2 = plane.dst3

        Dim oglData As New List(Of Single)
        Dim floorColor = task.color.Mean(plane.sliceMask)
        oglData.Add(floorColor(0))
        oglData.Add(floorColor(1))
        oglData.Add(floorColor(2))
        oglData.Add(plane.floorYPlane)
        task.ogl.dataInput = cv.Mat.FromPixelData(4, 1, cv.MatType.CV_32F, oglData.ToArray)
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(plane.dst2)
    End Sub
End Class






Public Class XO_OpenGL_FlatStudy3 : Inherits TaskParent
    Dim plane As New Plane_FloorStudy
    Public Sub New()
        task.ogl.oglFunction = oCase.floorStudy
        labels = {"", "", "", ""}
        desc = "Create an OpenGL display where the floor is built as a quad"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static cushionSlider = OptionParent.FindSlider("Structured Depth slice thickness in pixels")

        plane.Run(src)
        dst2 = plane.dst2
        labels(2) = plane.labels(2)

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.dataInput = cv.Mat.FromPixelData(1, 1, cv.MatType.CV_32F, {plane.planeY})
        task.ogl.Run(src)
    End Sub
End Class






Public Class XO_OpenGL_FlatFloor : Inherits TaskParent
    Dim flatness As New XO_Model_FlatSurfaces
    Public Sub New()
        task.ogl.oglFunction = oCase.floorStudy
        desc = "Using minimal cost, create an OpenGL display where the floor is built as a quad"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        flatness.Run(src)
        SetTrueText(flatness.labels(2), 3)

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.dataInput = cv.Mat.FromPixelData(1, 1, cv.MatType.CV_32F, {task.pcFloor})
        task.ogl.Run(src)
        labels(2) = flatness.labels(2)
        labels(3) = flatness.labels(3)
    End Sub
End Class






Public Class XO_OpenGL_FlatCeiling : Inherits TaskParent
    Dim flatness As New XO_Model_FlatSurfaces
    Public Sub New()
        task.ogl.oglFunction = oCase.floorStudy
        desc = "Using minimal cost, create an OpenGL display where the ceiling is built as a quad"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        flatness.Run(src)
        SetTrueText(flatness.labels(2), 3)

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.dataInput = cv.Mat.FromPixelData(1, 1, cv.MatType.CV_32F, {task.pcCeiling})
        task.ogl.Run(src)
        labels(2) = flatness.labels(2)
        labels(3) = flatness.labels(3)
    End Sub
End Class









Public Class XO_OpenGL_PeakFlat : Inherits TaskParent
    Dim peak As New Plane_Histogram
    Public Sub New()
        task.kalman = New Kalman_Basics
        task.ogl.oglFunction = oCase.floorStudy
        desc = "Display the peak flat level in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        peak.Run(src)
        dst2 = peak.dst2
        labels(2) = peak.labels(3)

        task.kalman.kInput = {peak.peakFloor, peak.peakCeiling}
        task.kalman.Run(emptyMat)

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.dataInput = cv.Mat.FromPixelData(2, 1, cv.MatType.CV_32F, {task.kalman.kOutput(0), task.kalman.kOutput(1)})
        task.ogl.Run(src)
    End Sub
End Class









' https://cs.lmu.edu/~ray/notes/openglexamples/
Public Class XO_OpenGL_Sierpinski : Inherits TaskParent
    Public Sub New()
        task.ogl.oglFunction = oCase.sierpinski
        OptionParent.FindSlider("OpenGL Point Size").Value = 3
        desc = "Draw the Sierpinski triangle pattern in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(src)
    End Sub
End Class







Public Class XO_OpenGL_DrawHulls : Inherits TaskParent
    Public options As New Options_OpenGLFunctions
    Public hulls As New RedColor_Hulls
    Dim ogl As New XO_OpenGL_Basics
    Public Sub New()
        ogl.oglFunction = oCase.drawCells
        labels = {"", "", "", ""}
        desc = "Draw all the hulls in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        Dim ptM = options.moveAmount
        Dim shift As New cv.Point3f(ptM(0), ptM(1), ptM(2))

        hulls.Run(src)
        dst2 = hulls.dst2
        Dim rcx = task.rcD

        Dim oglData As New List(Of cv.Point3f)
        oglData.Add(New cv.Point3f)
        Dim polygonCount As Integer
        For Each rc In task.redC.rcList
            If rc.hull Is Nothing Then Continue For
            Dim hullIndex = oglData.Count
            oglData.Add(New cv.Point3f(rc.hull.Count, 0, 0))
            If rc.index = rcx.index Then
                oglData.Add(New cv.Point3f(1, 1, 1))
            Else
                oglData.Add(New cv.Point3f(rc.color(2) / 255, rc.color(1) / 255, rc.color(0) / 255))
            End If
            Dim hullPoints = 0
            For Each pt In rc.hull
                If pt.X > rc.rect.Width Then pt.X = rc.rect.Width - 1
                If pt.Y > rc.rect.Height Then pt.Y = rc.rect.Height - 1
                Dim v = task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X)
                If v.Z > 0 Then
                    hullPoints += 1
                    oglData.Add(New cv.Point3f(v.X + shift.X, v.Y + shift.Y, v.Z + shift.Z))
                End If
            Next
            oglData(hullIndex) = New cv.Point3f(hullPoints, 0, 0)
            polygonCount += 1
        Next

        oglData(0) = New cv.Point3f(polygonCount, 0, 0)
        ogl.dataInput = cv.Mat.FromPixelData(oglData.Count, 1, cv.MatType.CV_32FC3, oglData.ToArray)
        ogl.Run(dst2)
        SetTrueText(CStr(polygonCount) + " polygons were sent to OpenGL", 2)
    End Sub
End Class







Public Class XO_OpenGL_Contours : Inherits TaskParent
    Dim options2 As New Options_OpenGL_Contours
    Public options As New Options_OpenGLFunctions
    Public Sub New()
        task.ogl.oglFunction = oCase.drawCells
        OptionParent.FindSlider("OpenGL shift fwd/back (Z-axis)").Value = -150
        labels = {"", "", "Output of RedCloud", "OpenGL snapshot"}
        desc = "Draw all the RedCloud contours in OpenGL with various settings."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        Dim ptM = options.moveAmount
        Dim shift As New cv.Point3f(ptM(0), ptM(1), ptM(2))

        options2.Run()

        dst2 = runRedC(src, labels(2))
        Dim rcx = task.rcD

        Dim polygonCount As Integer
        Dim oglData As New List(Of cv.Point3f)
        Dim lastDepth As cv.Scalar
        oglData.Add(New cv.Point3f)
        For Each rc In task.redC.rcList
            Dim d = rc.depth
            If d = 0 Then Continue For

            Dim dataIndex = oglData.Count
            oglData.Add(New cv.Point3f(rc.contour.Count, 0, 0))
            If rc.index = rcx.index Then
                oglData.Add(New cv.Point3f(1, 1, 1))
            Else
                oglData.Add(New cv.Point3f(rc.color(2) / 255, rc.color(1) / 255, rc.color(0) / 255))
            End If
            lastDepth = rc.depth
            For Each pt In rc.contour
                If pt.X > rc.rect.Width Then pt.X = rc.rect.Width - 1
                If pt.Y > rc.rect.Height Then pt.Y = rc.rect.Height - 1
                Dim v = task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X)
                If options2.depthPointStyle = pointStyle.flattened Or options2.depthPointStyle = pointStyle.flattenedAndFiltered Then v.Z = d
                If options2.depthPointStyle = pointStyle.filtered Or options2.depthPointStyle = pointStyle.flattenedAndFiltered Then
                    If Math.Abs(v.X - lastDepth(0)) > options2.filterThreshold Then v.X = lastDepth(0)
                    If Math.Abs(v.Y - lastDepth(1)) > options2.filterThreshold Then v.Y = lastDepth(1)
                    If Math.Abs(v.Z - lastDepth(2)) > options2.filterThreshold Then v.Z = lastDepth(2)
                End If
                oglData.Add(New cv.Point3f(v.X + shift.X, v.Y + shift.Y, v.Z + shift.Z))
                lastDepth = v
            Next
            oglData(dataIndex) = New cv.Point3f(rc.contour.Count, 0, 0)
            polygonCount += 1
        Next

        oglData(0) = New cv.Point3f(polygonCount, 0, 0)
        task.ogl.dataInput = cv.Mat.FromPixelData(oglData.Count, 1, cv.MatType.CV_32FC3, oglData.ToArray)
        task.ogl.Run(New cv.Mat)
    End Sub
End Class








Public Class XO_OpenGL_PatchHorizontal : Inherits TaskParent
    Dim patch As New Pixel_NeighborsPatchNeighbors
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Draw the point cloud after patching z-values that are similar"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        patch.Run(src)
        dst2 = patch.dst3
        task.ogl.pointCloudInput = dst2
        task.ogl.Run(src)
    End Sub
End Class







Public Class XO_OpenGL_ProfileSweep : Inherits TaskParent
    Dim visuals As New XO_OpenGL_Profile
    Dim options As New Options_IMU
    Dim testCase As Integer = 0
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Test the X-, Y-, and Z-axis rotation in sequence"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.gOptions.setGravityUsage(False)
        If task.frameCount Mod 100 = 0 Then
            testCase += 1
            If testCase >= 3 Then testCase = 0
            options.Run()
            options.rotateX = -45
            options.rotateY = -45
            options.rotateZ = -45
        End If

        Dim bump = 1
        Static xRotateSlider = OptionParent.FindSlider("Rotate pointcloud around X-axis (degrees)")
        Static yRotateSlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis (degrees)")
        Static zRotateSlider = OptionParent.FindSlider("Rotate pointcloud around Z-axis (degrees)")
        ' NOTE: the z and x sliders are switched on purpose.
        Select Case testCase
            Case 0
                zRotateSlider.value += bump
                If zRotateSlider.value >= 45 Then zRotateSlider.value = -45
                labels(3) = "Rotating around X-axis with " + CStr(zRotateSlider.value) + " degrees"
            Case 1
                yRotateSlider.value += bump
                If yRotateSlider.value >= 45 Then yRotateSlider.value = -45
                labels(3) = "Rotating around Y-axis with " + CStr(yRotateSlider.value) + " degrees"
            Case 2
                xRotateSlider.value += bump
                If xRotateSlider.value >= 45 Then xRotateSlider.value = -45
                labels(3) = "Rotating around Z-axis with " + CStr(xRotateSlider.value) + " degrees"
        End Select
        SetTrueText("Top down view: " + labels(3), 1)

        visuals.Run(src)
        dst1 = visuals.dst1
        dst2 = visuals.dst2
        dst3 = visuals.dst3
    End Sub
End Class







Public Class XO_OpenGL_FlatSurfaces : Inherits TaskParent
    Dim flat As New RedColor_LikelyFlatSurfaces
    Public Sub New()
        labels(2) = "Display the point cloud pixels that appear to be vertical and horizontal regions."
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Review the vertical and horizontal regions from Plane_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        flat.Run(src)
        task.pointCloud.CopyTo(dst2, flat.dst2)

        task.ogl.pointCloudInput = dst2
        task.ogl.Run(src)
    End Sub
End Class







Public Class XO_OpenGL_GravityTransform : Inherits TaskParent
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Use the IMU's acceleration values to build the transformation matrix of an OpenGL viewer"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(src)
    End Sub
End Class







' https://open.gl/transformations
' https://www.codeproject.com/Articles/1247960/Learning-Basic-Math-Used-In-3D-Graphics-Engines
Public Class XO_OpenGL_GravityAverage : Inherits TaskParent
    Dim imuAvg As New IMU_Average
    Dim imu As New IMU_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Build the GMatrix with the Average IMU acceleration (not the raw or filtered values) and use the resulting GMatrix to stabilize the point cloud in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        strOut = "To remove the point cloud averaging, set the global option 'Frame History' to 1." + vbCrLf +
                 "Or, even alternatively, run the 'OpenGL_GravityTransform' algorithm." + vbCrLf + vbCrLf +
                 "Before Averaging: Average IMU acceleration: X = " + Format(task.IMU_RawAcceleration.X, fmt3) + ", Y = " + Format(task.IMU_RawAcceleration.Y, fmt3) +
                 ", Z = " + Format(task.IMU_RawAcceleration.Z, fmt3) + vbCrLf
        imuAvg.Run(src)
        task.IMU_RawAcceleration = task.IMU_AverageAcceleration
        imu.Run(src)
        task.accRadians.Z += cv.Cv2.PI / 2
        strOut += "After Averaging: Average IMU accerlation: X = " + Format(task.IMU_Acceleration.X, fmt3) + ", Y = " + Format(task.IMU_Acceleration.Y, fmt3) +
                  ", Z = " + Format(task.IMU_Acceleration.Z, fmt3) + vbCrLf

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(src)
        SetTrueText(strOut, 3)
    End Sub
End Class







' https://open.gl/transformations
' https://www.codeproject.com/Articles/1247960/Learning-Basic-Math-Used-In-3D-Graphics-Engines
Public Class XO_OpenGL_GravityKalman : Inherits TaskParent
    Dim imuKalman As New IMU_Kalman
    Dim imu As New IMU_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Build the GMatrix with the Average IMU acceleration (not the raw or filtered values) and use the resulting GMatrix to stabilize the point cloud in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        strOut = "To remove the point cloud averaging, set the global option 'Frame History' to 1." + vbCrLf +
                 "Or, even alternatively, run the 'OpenGL_GravityTransform' algorithm." + vbCrLf + vbCrLf +
                 "Before Kalman: IMU acceleration: X = " + Format(task.IMU_RawAcceleration.X, fmt3) + ", Y = " + Format(task.IMU_RawAcceleration.Y, fmt3) +
                 ", Z = " + Format(task.IMU_RawAcceleration.Z, fmt3) + vbCrLf
        imuKalman.Run(src)

        task.IMU_RawAcceleration = task.IMU_Acceleration
        imu.Run(src)
        task.accRadians.Z += cv.Cv2.PI / 2

        strOut += "After Kalman: IMU acceleration: X = " + Format(task.IMU_Acceleration.X, fmt3) + ", Y = " + Format(task.IMU_Acceleration.Y, fmt3) +
                  ", Z = " + Format(task.IMU_Acceleration.Z, fmt3) + vbCrLf

        task.IMU_Acceleration = task.kalmanIMUacc
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(src)
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class XO_OpenGL_CloudMisses : Inherits TaskParent
    Dim frames As New History_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        labels = {"", "", "Point cloud after over the last X frames", ""}
        desc = "Run OpenGL removing all pixels not present for all X frames"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        frames.Run(task.depthMask / 255)
        dst2 = frames.dst2
        dst2 = dst2.Threshold(frames.saveFrames.Count - 1, 255, cv.ThresholdTypes.Binary)

        task.ogl.pointCloudInput.SetTo(0)
        task.pointCloud.CopyTo(task.ogl.pointCloudInput, dst2)
        task.ogl.Run(src)
    End Sub
End Class








Public Class XO_OpenGL_CloudHistory : Inherits TaskParent
    Dim hCloud As New History_Cloud
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        labels = {"", "", "Point cloud after over the last X frames", "Mask to remove partially missing pixels"}
        desc = "Run OpenGL with a masked point cloud averaged over the last X frames."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hCloud.Run(task.pointCloud)
        dst2 = hCloud.dst2

        task.ogl.pointCloudInput = dst2
        task.ogl.Run(src)
    End Sub
End Class







Public Class XO_OpenGL_RedTrack : Inherits TaskParent
    Dim redCC As New RedTrack_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Display all the RedCC cells in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redCC.Run(src)
        dst2 = redCC.dst2

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(dst2)
        SetTrueText(redCC.strOut, 3)
    End Sub
End Class








Public Class XO_OpenGL_Density2D : Inherits TaskParent
    Dim dense As New Density_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC3, 0)
        desc = "Create a mask showing which pixels are close to each other and display the results."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dense.Run(src)
        dst2.SetTo(0)
        task.pointCloud.CopyTo(dst2, dense.dst2)

        task.ogl.pointCloudInput = dst2
        task.ogl.Run(New cv.Mat(dst2.Size(), cv.MatType.CV_8UC3, white))
    End Sub
End Class








Public Class XO_OpenGL_ViewObjects : Inherits TaskParent
    Dim bpDoctor As New GuidedBP_Points
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Identify the objects in the scene and display them in OpenGL with their respective colors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.pointCloud.Clone

        bpDoctor.Run(src)
        dst2 = bpDoctor.dst2

        dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1.SetTo(0, Not dst0)

        task.ogl.pointCloudInput = dst1
        task.ogl.Run(dst2)
    End Sub
End Class







Public Class XO_OpenGL_NoSolo : Inherits TaskParent
    Dim hotTop As New BackProject_SoloTop
    Dim hotSide As New BackProject_SoloSide
    Public Sub New()
        task.useXYRange = False '
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        labels(2) = "The points below were identified as solo points in the point cloud"
        desc = "Display point cloud without solo points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hotTop.Run(src)
        dst2 = hotTop.dst3

        hotSide.Run(src)
        dst2 = dst2 Or hotSide.dst3

        If task.gOptions.DebugCheckBox.Checked = False Then task.pointCloud.SetTo(0, dst2)
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(src)
        SetTrueText("Toggle the solo points on and off using the 'DebugCheckBox' global option.", 3)
    End Sub
End Class








Public Class XO_OpenGL_RedCloud : Inherits TaskParent
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Display all the RedCloud cells in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(ShowPalette(dst2))
    End Sub
End Class






Public Class XO_OpenGL_RedCloudSpectrum : Inherits TaskParent
    Dim redS As New Spectrum_RedCloud
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Display all the RedCloud cells after Spectrum filtering."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redS.Run(src)
        dst2 = redS.dst3
        task.pointCloud.SetTo(0, dst2.InRange(0, 0))

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(dst2)
    End Sub
End Class








Public Class XO_OpenGL_RedCloudCell : Inherits TaskParent
    Dim specZ As New Spectrum_Z
    Dim breakdown As New Spectrum_Breakdown
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = " Isolate a RedCloud cell - after filtering by Spectrum_Depth - in an OpenGL display"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        specZ.Run(src)
        SetTrueText(specZ.strOut, 3)

        If task.ClickPoint = newPoint And task.redC.rcList.Count > 1 Then
            task.rcD = task.redC.rcList(1) ' pick the largest cell
            task.ClickPoint = task.rcD.maxDist
        End If

        breakdown.Run(src)

        task.ogl.pointCloudInput.SetTo(0)

        task.pointCloud(task.rcD.rect).CopyTo(task.ogl.pointCloudInput(task.rcD.rect), task.rcD.mask)
        task.ogl.Run(dst2)
    End Sub
End Class







Public Class XO_OpenGL_BPFilteredSideView : Inherits TaskParent
    Dim filter As New BackProject2D_FilterSide
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Use the BackProject2D_FilterSide to remove low sample bins and trim the loose fragments in 3D"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        filter.Run(src)
        dst2 = filter.dst2

        task.ogl.pointCloudInput = dst2
        task.ogl.Run(src)
    End Sub
End Class







Public Class XO_OpenGL_BPFilteredTopView : Inherits TaskParent
    Dim filter As New BackProject2D_FilterTop
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Use the BackProject2D_FilterSide to remove low sample bins and trim the loose fragments in 3D"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        filter.Run(src)
        dst2 = filter.dst2

        task.ogl.pointCloudInput = dst2
        task.ogl.Run(src)
    End Sub
End Class







Public Class XO_OpenGL_BPFilteredBoth : Inherits TaskParent
    Dim filter As New BackProject2D_FilterBoth
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Use the BackProject2D_FilterSide/Top to remove low sample bins and trim the loose fragments in 3D"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        filter.Run(src)
        dst2 = filter.dst2

        task.ogl.pointCloudInput = dst2
        task.ogl.Run(src)
    End Sub
End Class







Public Class XO_OpenGL_BPFiltered3D : Inherits TaskParent
    Dim filter As New Hist3Dcloud_BP_Filter
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Use the BackProject2D_FilterSide/Top to remove low sample bins and trim the loose fragments in 3D"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        filter.Run(src)
        dst2 = filter.dst3

        task.ogl.pointCloudInput = dst2
        task.ogl.Run(src)
    End Sub
End Class







Public Class XO_OpenGL_HistNorm3D : Inherits TaskParent
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Create an OpenGL plot using the BGR data normalized to between 0 and 1."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        src.ConvertTo(src, cv.MatType.CV_32FC3)
        task.ogl.pointCloudInput = src.Normalize(0, 1, cv.NormTypes.MinMax)
        task.ogl.Run(New cv.Mat)
    End Sub
End Class







' https://docs.opencvb.org/3.4/d1/d1d/tutorial_histo3D.html
Public Class XO_OpenGL_HistDepth3D : Inherits TaskParent
    Dim hcloud As New Hist3Dcloud_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.Histogram3D
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        desc = "Display the 3D histogram of the depth in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static binSlider = OptionParent.FindSlider("Histogram 3D Bins")
        hcloud.Run(src)
        Dim histogram = cv.Mat.FromPixelData(binSlider.value, 1, cv.MatType.CV_32F, hcloud.histogram.Data)
        task.ogl.dataInput = histogram
        task.ogl.pointCloudInput = New cv.Mat
        task.ogl.Run(New cv.Mat)
        SetTrueText("Use the sliders for X/Y/Z histogram bins to add more points")
    End Sub
End Class






Public Class XO_OpenGL_SoloPointsRemoved : Inherits TaskParent
    Dim solos As New FindNonZero_SoloPoints
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Remove the solo points and display the pointcloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.toggleOn Then
            solos.Run(src)
            dst2 = solos.dst2
            task.pointCloud.SetTo(0, dst2)
        Else
            dst2.SetTo(0)
        End If

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(src)
        SetTrueText("You should see the difference in the OpenGL window as the solo points are toggled on an off.", 3)
    End Sub
End Class







Public Class XO_OpenGL_Duster : Inherits TaskParent
    Dim duster As New Duster_Basics
    Dim options As New Options_OpenGL_Duster
    Public Sub New()
        desc = "Show a dusted version point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        duster.Run(src)
        dst2 = duster.dst3

        task.ogl.pointCloudInput = If(options.useTaskPointCloud, task.pointCloud, duster.dst2)
        task.ogl.Run(If(options.useClusterColors = False, task.color, dst2))
    End Sub
End Class






Public Class XO_OpenGL_DusterY : Inherits TaskParent
    Dim duster As New Duster_BasicsY
    Dim options As New Options_OpenGL_Duster
    Public Sub New()
        desc = "Show a dusted version point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        duster.Run(src)
        dst2 = duster.dst3

        task.ogl.pointCloudInput = If(options.useTaskPointCloud, task.pointCloud, duster.dst2)
        task.ogl.Run(If(options.useClusterColors = False, task.color, dst2))
    End Sub
End Class








Public Class XO_OpenGL_Color3D : Inherits TaskParent
    Dim hColor As New Hist3Dcolor_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        desc = "Plot the results of a 3D histogram of the BGR data "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hColor.Run(src)
        dst2 = hColor.dst3
        labels(2) = hColor.labels(2)

        dst2.ConvertTo(dst1, cv.MatType.CV_32FC3)
        dst1 = dst1.Normalize(0, 1, cv.NormTypes.MinMax)

        Dim split = dst1.Split()
        split(1) *= -1
        cv.Cv2.Merge(split, task.ogl.pointCloudInput)

        task.ogl.Run(dst2)
    End Sub
End Class





Public Class XO_OpenGL_ColorReduced3D : Inherits TaskParent
    Dim color8U As New Color8U_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        task.gOptions.ColorSource.SelectedItem = "LUT_Basics"
        OptionParent.FindSlider("OpenGL Point Size").Value = 20
        desc = "Connect the 3D representation of the different color formats with colors in that format (see dst2)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        dst2 = color8U.dst3
        dst2.ConvertTo(dst1, cv.MatType.CV_32FC3)
        labels(2) = "There are " + CStr(color8U.classCount) + " classes for " + task.gOptions.ColorSource.Text
        dst1 = dst1.Normalize(0, 1, cv.NormTypes.MinMax)
        Dim split = dst1.Split()
        split(1) *= -1
        cv.Cv2.Merge(split, task.ogl.pointCloudInput)
        task.ogl.Run(dst2)
    End Sub
End Class








Public Class XO_OpenGL_ColorRaw : Inherits TaskParent
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        desc = "Plot the results of a 3D histogram of the BGR data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src
        src.ConvertTo(dst1, cv.MatType.CV_32FC3)
        dst1 = dst1.Normalize(0, 1, cv.NormTypes.MinMax)

        Dim split = dst1.Split()
        split(1) *= -1
        cv.Cv2.Merge(split, task.ogl.pointCloudInput)

        task.ogl.Run(dst2)
    End Sub
End Class








Public Class XO_OpenGL_ColorBin4Way : Inherits TaskParent
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8UC3, white)
        desc = "Plot the results of a 3D histogram of the lightest and darkest BGR data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        dst1.SetTo(0)
        task.color(task.rcD.rect).CopyTo(dst1(task.rcD.rect), task.rcD.mask)

        dst1.ConvertTo(dst3, cv.MatType.CV_32FC3)
        dst3 = dst3.Normalize(0, 1, cv.NormTypes.MinMax)

        Dim split = dst3.Split()
        split(1) *= -1
        cv.Cv2.Merge(split, task.ogl.pointCloudInput)

        task.ogl.Run(dst0)
    End Sub
End Class







Public Class XO_OpenGL_Grid : Inherits TaskParent
    Dim lowRes As New Brick_Edges
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Display the grid depth and color for each cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lowRes.Run(src)
        dst2 = lowRes.dst2
        If task.lowResDepth.Type <> cv.MatType.CV_32F Then Exit Sub

        Dim depth = task.lowResDepth.Resize(src.Size, 0, 0, cv.InterpolationFlags.Nearest)
        Dim pc As New cv.Mat
        cv.Cv2.Merge({task.pcSplit(0), task.pcSplit(1), depth}, pc)
        If task.toggleOn Then pc.SetTo(0, lowRes.fLessMask)
        task.ogl.pointCloudInput = pc
        task.ogl.Run(dst2)
    End Sub
End Class





Public Class XO_OpenGL_Neighbors : Inherits TaskParent
    Dim inputZ As New Linear_InputZ
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Display only pixels which are near each other in the Z dimension"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        inputZ.Run(src)
        dst2 = inputZ.dst3.ConvertScaleAbs

        task.ogl.pointCloudInput = task.pointCloud
        If task.toggleOn Then task.ogl.pointCloudInput.SetTo(0, Not dst2)
        task.ogl.Run(task.color)
    End Sub
End Class





Public Class XO_OpenGL_LinearX : Inherits TaskParent
    Dim linear As New Linear_ImageX
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Display the linear transform of the pointcloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        linear.Run(src)
        task.ogl.pointCloudInput = linear.dst3
        task.ogl.Run(task.color)
    End Sub
End Class




Public Class XO_OpenGL_LinearY : Inherits TaskParent
    Dim linear As New Linear_ImageY
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Display the linear transform of the pointcloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        linear.Run(src)
        task.ogl.pointCloudInput = linear.dst3
        task.ogl.Run(task.color)
    End Sub
End Class







Public Class XO_OpenGL_StableMinMax : Inherits TaskParent
    Dim minmax As New Depth_MinMaxNone
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        labels = {"", "", "Pointcloud Max", "Pointcloud Min"}
        desc = "display the Pointcloud Min or Max in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        minmax.Run(task.pointCloud)

        dst2 = minmax.dst2

        If minmax.options.useMax Or minmax.options.useMin Then
            task.ogl.pointCloudInput = dst2
        Else
            task.ogl.pointCloudInput = task.pointCloud
        End If
        task.ogl.Run(task.color)

        labels(2) = minmax.labels(2)
    End Sub
End Class






Public Class XO_OpenGL_PClinesAll : Inherits TaskParent
    Dim lines As New Line3D_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        task.ogl.pointCloudInput = New cv.Mat(dst3.Size, cv.MatType.CV_32FC3, 0)
        desc = "Draw the 3D lines found from the Line3D_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src.Clone)
        dst2 = lines.dst2
        dst3 = lines.dst3
        labels(2) = lines.labels(2)

        task.ogl.pointCloudInput.SetTo(0)
        task.pointCloud.CopyTo(task.ogl.pointCloudInput, lines.dst3)

        task.ogl.Run(src)
    End Sub
End Class







'https://www3.ntu.edu.sg/home/ehchua/programming/opengl/CG_Examples.html
Public Class XO_OpenGL_QuadDepth : Inherits TaskParent
    Public Sub New()
        task.needBricks = True
        OptionParent.FindSlider("OpenCVB OpenGL buffer count").Value = 1
        task.ogl.oglFunction = oCase.quadBasics
        desc = "Create a simple plane in each of bricks."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.bricks.dst2
        dst3 = task.colorizer.dst2
        labels(3) = task.colorizer.labels(2)
        Dim quadData As New List(Of cv.Point3f)
        For Each brick In task.bricks.brickList
            If brick.depth = 0 Then Continue For
            If brick.corners.Count Then quadData.Add(brick.color)
            For Each pt In brick.corners
                quadData.Add(pt)
            Next
        Next
        task.ogl.dataInput = cv.Mat.FromPixelData(quadData.Count, 1, cv.MatType.CV_32FC3, quadData.ToArray)
        task.ogl.pointCloudInput = New cv.Mat()
        task.ogl.Run(src)
        labels(2) = task.bricks.labels(2)
        labels(3) = "There were " + CStr(quadData.Count / 5) + " quads found."
    End Sub
End Class








Public Class XO_OpenGL_PCdiff : Inherits TaskParent
    Dim filter As New PCdiff_Points
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Display only pixels that are within X mm's of each other."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        filter.Run(src)
        dst2 = filter.dst3

        If task.toggleOn Then
            Dim r = New cv.Rect(0, 0, dst2.Width, 2)
            task.color(r).SetTo(white)
        End If
        task.ogl.pointCloudInput = dst2
        task.ogl.Run(task.color)
    End Sub
End Class





Public Class XO_OpenGL_Connected : Inherits TaskParent
    Dim connect As New Region_Contours
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Display the connected contours in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)
        dst2 = connect.dst2
        dst3 = connect.dst3

        Dim pc As New cv.Mat(task.pointCloud.Size, cv.MatType.CV_32FC3, 0)
        task.pointCloud.CopyTo(pc, connect.dst1)
        If task.gOptions.DebugCheckBox.Checked Then
            task.ogl.pointCloudInput = task.pointCloud
        Else
            task.ogl.pointCloudInput = pc
        End If
        task.ogl.Run(src)
    End Sub
End Class






Public Class XO_OpenGL_Lines3D : Inherits TaskParent
    Dim lines As New Line3D_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.pcLines
        desc = "Draw the 3D lines found using the task.lines.lpList and the accompanying bricks."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        If lines.lines3D.Count Then
            task.ogl.dataInput = lines.lines3DMat
            ' task.ogl.pointCloudInput = task.pointCloud
            task.ogl.Run(New cv.Mat)
        End If

        SetTrueText(lines.strOut, 3)
    End Sub
End Class





Public Class XO_OpenGL_Regions : Inherits TaskParent
    Dim options As New Options_Regions
    Dim qDepth As New XO_OpenGL_QuadDepth
    Dim connect As New XO_OpenGL_QuadConnected
    Dim regions As New Region_Basics
    Dim regionQuads As New Region_Quads
    Public Sub New()
        desc = "Compare different representations of the point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        task.ogl.oglFunction = oCase.quadBasics ' only the first case needs something else.
        task.ogl.pointCloudInput = New cv.Mat()
        Select Case options.displayIndex
            Case 0
                task.ogl.oglFunction = oCase.drawPointCloudRGB
                task.ogl.pointCloudInput = task.pointCloud
                task.ogl.Run(src)
            Case 1
                qDepth.Run(src)
                dst2 = qDepth.dst2
                dst3 = qDepth.dst3
            Case 2
                connect.Run(src)
                dst2 = connect.dst2
                dst3 = connect.dst3
            Case 3
                regions.Run(src)
                dst2 = regions.dst2

                regionQuads.inputRects = regions.vRects
                regionQuads.Run(src)

                task.ogl.dataInput = regionQuads.quadMat
                task.ogl.Run(src)
                labels = regions.labels
            Case 4
                regions.Run(src)
                dst2 = regions.dst3

                regionQuads.inputRects = regions.hRects
                regionQuads.Run(src)

                task.ogl.dataInput = regionQuads.quadMat
                task.ogl.Run(src)
                labels = regions.labels
        End Select
    End Sub
End Class





Public Class XO_OpenGL_QuadConnected : Inherits TaskParent
    Dim connect As New Region_Core
    Public Sub New()
        task.needBricks = True
        task.ogl.oglFunction = oCase.quadBasics
        desc = "Build connected bricks and remove cells that are not connected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)
        dst2 = connect.dst2
        dst3 = connect.dst3

        Dim quadData As New List(Of cv.Point3f)
        Dim brick1 As brickData = Nothing, brick2 As brickData = Nothing
        For Each tup In connect.hTuples
            brick1 = task.bricks.brickList(tup.Item1)
            brick2 = task.bricks.brickList(tup.Item2)
            For i = tup.Item1 + 1 To tup.Item2 - 1
                brick1 = task.bricks.brickList(i - 1)
                brick2 = task.bricks.brickList(i)
                If brick1.depth = 0 Or brick2.depth = 0 Then Continue For
                If brick1.corners.Count = 0 Or brick2.corners.Count = 0 Then Continue For

                quadData.Add(brick1.color)
                quadData.Add(brick1.corners(0))
                quadData.Add(brick2.corners(0))
                quadData.Add(brick2.corners(3))
                quadData.Add(brick1.corners(3))
            Next
            If brick1.corners.Count > 0 And brick2.corners.Count > 0 Then
                quadData.Add(brick2.color)
                quadData.Add(brick2.corners(0))
                quadData.Add(brick2.corners(1))
                quadData.Add(brick2.corners(2))
                quadData.Add(brick2.corners(3))
            End If
        Next

        Dim width = dst2.Width / task.brickSize
        For Each tup In connect.vTuples
            For i = tup.Item1 To tup.Item2 - width Step width
                brick1 = task.bricks.brickList(i)
                brick2 = task.bricks.brickList(i + width)
                If brick1.depth = 0 Or brick2.depth = 0 Then Continue For
                If brick1.corners.Count = 0 Or brick2.corners.Count = 0 Then Continue For

                quadData.Add(brick1.color)
                quadData.Add(brick1.corners(0))
                quadData.Add(brick1.corners(1))
                quadData.Add(brick2.corners(1))
                quadData.Add(brick2.corners(0))
            Next
            If brick1.corners.Count > 0 And brick2.corners.Count > 0 Then
                quadData.Add(brick2.color)
                quadData.Add(brick2.corners(0))
                quadData.Add(brick2.corners(1))
                quadData.Add(brick2.corners(2))
                quadData.Add(brick2.corners(3))
            End If
        Next

        task.ogl.dataInput = cv.Mat.FromPixelData(quadData.Count, 1, cv.MatType.CV_32FC3, quadData.ToArray)
        task.ogl.pointCloudInput = New cv.Mat()
        task.ogl.Run(src)
        labels = task.bricks.labels
    End Sub
End Class




Public Class XO_OpenGL_ContourPlaneOnly : Inherits TaskParent
    Public Sub New()
        task.ogl.oglFunction = oCase.quadBasics
        desc = "Display the rectangles of the contour planes in 3D"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim quadData As New List(Of cv.Point3f)
        For Each contour In task.contours.contourList
            Dim index = task.contours.contourList.IndexOf(contour)
            Dim c = task.scalarColors(index)
            Dim color As cv.Point3f = New cv.Point3f(c(0), c(1), c(2))

            Dim p0 = getWorldCoordinates(contour.rect.TopLeft, contour.depth)
            Dim p1 = getWorldCoordinates(contour.rect.BottomRight, contour.depth)

            Dim corners As New List(Of cv.Point3f) From {New cv.Point3f(p0.X, p0.Y, contour.depth),
                                                         New cv.Point3f(p1.X, p0.Y, contour.depth),
                                                         New cv.Point3f(p1.X, p1.Y, contour.depth),
                                                         New cv.Point3f(p0.X, p1.Y, contour.depth)}
            Dim noNaNs = True
            For Each pt In corners
                If Single.IsNaN(pt.Z) Then
                    noNaNs = False
                    Exit For
                End If
            Next
            If noNaNs Then
                quadData.Add(color)
                quadData.Add(corners(0))
                quadData.Add(corners(0))
                quadData.Add(corners(3))
                quadData.Add(corners(3))
                quadData.Add(color)
                quadData.Add(corners(0))
                quadData.Add(corners(1))
                quadData.Add(corners(2))
                quadData.Add(corners(3))
            End If
        Next
        dst2 = task.contours.dst2
        labels(2) = task.contours.labels(2)

        task.ogl.dataInput = cv.Mat.FromPixelData(quadData.Count, 1, cv.MatType.CV_32FC3, quadData.ToArray)
        task.ogl.pointCloudInput = New cv.Mat()
        task.ogl.Run(src)
    End Sub
End Class






Public Class XO_OpenGL_LinearXY : Inherits TaskParent
    Dim linearX As New Linear_ImageX
    Dim linearY As New Linear_ImageY
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Display the linear transform of the pointcloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        linearX.Run(src)
        linearY.Run(src)

        cv.Cv2.Merge({linearX.dst2, linearY.dst2, task.pcSplit(2)}, dst2)

        task.ogl.pointCloudInput = dst2
        task.ogl.Run(task.color)
    End Sub
End Class








Public Class XO_OpenGL_World : Inherits TaskParent
    Dim world As New Depth_World
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        labels = {"", "", "Generated Pointcloud", ""}
        desc = "Display the generated point cloud in OpenGL.  Use debug checkbox to toggle!"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.gOptions.DebugCheckBox.Checked = False Then
            world.Run(src)
            task.ogl.pointCloudInput = world.dst2
            dst2 = world.dst2
            dst3 = task.pointCloud
        Else
            task.ogl.pointCloudInput = If(src.Type = cv.MatType.CV_32FC3, src, task.pointCloud)
        End If

        task.ogl.Run(task.color)
    End Sub
End Class





Public Class XO_OpenGL_BrickCloud : Inherits TaskParent
    Dim bCloud As New Brick_Cloud
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Display the Brick_Cloud and alternate with the original point cloud."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bCloud.Run(src)
        If task.gOptions.DebugCheckBox.Checked = False Then
            dst1 = bCloud.dst1
            dst2 = bCloud.dst2
            dst3 = task.pointCloud
        Else
            dst2 = task.pointCloud
        End If
        labels(2) = bCloud.labels(2)

        task.ogl.pointCloudInput = dst2
        task.ogl.Run(task.color)
    End Sub
End Class





Public Class XO_OpenGL_GIF : Inherits TaskParent
    Dim input As New XO_Model_RedCloud
    Dim gifC As New Gif_Basics
    Public Sub New()
        desc = "Create a GIF for the XO_Model_RedCloud output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        input.Run(src)

        dst2 = input.dst3
        Dim r = New cv.Rect(0, 0, dst2.Height, dst2.Height)
        gifC.Run(dst2(r))

        SetTrueText("Select 'Gif_Basics CheckBox Options' form (see 'OpenCVB Algorithm Options')" + vbCrLf +
                    "Click the check box for each frame to be included" + vbCrLf + "Then click 'Build GIF file...' when done." +
                    vbCrLf + vbCrLf + "To adjust the GIF size, change the working size in the OpenCVB options.", 3)
        labels(2) = gifC.labels(2)
    End Sub
End Class





Public Class XO_OpenGL_GIFwithColor : Inherits TaskParent
    Dim input As New XO_Model_RedCloud
    Dim gifC As New Gif_Basics
    Public Sub New()
        desc = "Create a GIF for the XO_Model_RedCloud output and color image at the same time."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        input.Run(src)

        Dim r = New cv.Rect(0, 0, dst2.Height, dst2.Height)
        dst2 = input.dst3(r)
        Dim tmp As New cv.Mat
        cv.Cv2.HConcat(src, dst2(r), tmp)

        gifC.Run(tmp)

        SetTrueText("Select 'Gif_Basics CheckBox Options' form (see 'OpenCVB Algorithm Options')" + vbCrLf +
                    "Click the check box for each frame to be included" + vbCrLf + "Then click 'Build GIF file...' when done.", 3)
        labels(2) = gifC.labels(2)
    End Sub
End Class






Public Class XO_OpenGL_ModelwithSliders : Inherits TaskParent
    Dim model As New XO_Model_Basics
    Public Sub New()
        task.OpenGLTitle = "OpenGL_Basics"
        labels = {"", "", "Captured OpenGL output", ""}
        desc = "Capture the output of the OpenGL window"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.ogl.pointCloudInput = task.pointCloud
        If standaloneTest() Then task.ogl.Run(src)
        model.Run(src)
        dst2 = model.dst2
    End Sub
End Class







Public Class XO_OpenGL_Profile : Inherits TaskParent
    Public sides As New Profile_Basics
    Public rotate As New Profile_Rotation
    Dim heat As New HeatMap_Basics
    Dim ogl As New XO_OpenGL_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        If standalone Then task.gOptions.setGravityUsage(False)
        ogl.oglFunction = oCase.pcPointsAlone
        labels(3) = "Contour of selected cell is shown below.  Blue dot represents the minimum X (leftmost) point and red the maximum X (rightmost)"
        desc = "Visualize a RedCloud Cell and rotate it using the Options_IMU Sliders"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sides.Run(src)
        dst2 = sides.dst2

        Dim rc = task.rcD
        Dim contourMat As cv.Mat = cv.Mat.FromPixelData(rc.contour.Count, 1, cv.MatType.CV_32SC2, rc.contour.ToArray)
        If rc.contour.Count = 0 Then Exit Sub
        Dim split = contourMat.Split()
        Dim mm As mmData = GetMinMax(split(0))
        Dim p1 = rc.contour.ElementAt(mm.minLoc.Y)
        Dim p2 = rc.contour.ElementAt(mm.maxLoc.Y)

        dst3.SetTo(0)
        DrawContour(dst3(rc.rect), rc.contour, cv.Scalar.Yellow)
        DrawCircle(dst3, New cv.Point(p1.X + rc.rect.X, p1.Y + rc.rect.Y), task.DotSize + 2, cv.Scalar.Blue)
        DrawCircle(dst3, New cv.Point(p2.X + rc.rect.X, p2.Y + rc.rect.Y), task.DotSize + 2, cv.Scalar.Red)
        If rc.contour3D.Count > 0 Then
            Dim vecMat As cv.Mat = cv.Mat.FromPixelData(rc.contour3D.Count, 1, cv.MatType.CV_32FC3, rc.contour3D.ToArray)

            rotate.Run(src)
            Dim output As cv.Mat = vecMat.Reshape(1, vecMat.Rows * vecMat.Cols) * task.gmat.gMatrix ' <<<<<<<<<<<<<<<<<<<<<<< this is the XYZ-axis rotation...
            vecMat = output.Reshape(3, vecMat.Rows)

            ogl.pointCloudInput = New cv.Mat
            ogl.dataInput = vecMat

            heat.Run(vecMat)
            dst1 = heat.dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)
        End If

        ogl.Run(New cv.Mat)
    End Sub
End Class






Public Class XO_OpenGL_ProfileRC : Inherits TaskParent
    Dim sides As New Profile_Basics
    Public rotate As New Profile_Rotation
    Dim heat As New HeatMap_Basics
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_32FC3, 0)
        If standalone Then task.gOptions.setGravityUsage(False)
        task.ogl = New XO_OpenGL_Basics
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        task.ogl.oglFunction = oCase.pcPointsAlone
        desc = "Visualize just the RedCloud cell contour in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sides.Run(src)
        dst2 = sides.dst2
        dst3 = sides.dst3
        Dim rc = task.rcD

        If rc.contour3D.Count > 0 Then
            Dim vecMat As cv.Mat = cv.Mat.FromPixelData(rc.contour3D.Count, 1, cv.MatType.CV_32FC3, rc.contour3D.ToArray)
            rotate.Run(src)
            Dim output As cv.Mat = vecMat.Reshape(1, vecMat.Rows * vecMat.Cols) * task.gmat.gMatrix  ' <<<<<<<<<<<<<<<<<<<<<<< this is the XYZ-axis rotation...
            task.ogl.dataInput = output.Reshape(3, vecMat.Rows)
            task.ogl.pointCloudInput = New cv.Mat

            task.ogl.Run(New cv.Mat)
            heat.Run(vecMat)
            dst1 = heat.dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)
        End If
        SetTrueText("Select a RedCloud Cell to display the contour in OpenGL." + vbCrLf + rotate.strMsg, 3)
    End Sub
End Class

Public Class XO_Gravity_HorizonRawOld : Inherits TaskParent
    Public yLeft As Integer, yRight As Integer, xTop As Integer, xBot As Integer
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(2) = "Horizon and Gravity Vectors"
        desc = "Improved method to find gravity and horizon vectors"
    End Sub
    Private Function findFirst(points As cv.Mat, horizon As Boolean, ByRef sampleX As Integer) As Integer
        Dim ptList As New List(Of Integer)

        For i = 0 To Math.Min(10, points.Rows / 2)
            Dim pt = points.Get(Of cv.Point)(i, 0)
            If pt.X <= 0 Or pt.Y <= 0 Then Continue For
            If pt.X > dst2.Width Or pt.Y > dst2.Height Then Continue For
            If horizon Then ptList.Add(pt.Y) Else ptList.Add(pt.X)
            sampleX = pt.X ' this X value tells us if the horizon found is for the left or the right.
        Next

        If ptList.Count = 0 Then Return 0
        Return ptList.Average()
    End Function
    Private Function findLast(points As cv.Mat, horizon As Boolean, sampleX As Integer) As Integer
        Dim ptList As New List(Of Integer)

        For i = points.Rows To Math.Max(points.Rows - 10, points.Rows / 2) Step -1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            If pt.X <= 0 Or pt.Y <= 0 Then Continue For
            If pt.X > dst2.Width Or pt.Y > dst2.Height Then Continue For
            If horizon Then ptList.Add(pt.Y) Else ptList.Add(pt.X)
            sampleX = pt.X ' this X value tells us if the horizon found is for the left or the right.
        Next

        If ptList.Count = 0 Then Return 0
        Return ptList.Average()
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim threshold As Single = 0.015
        Dim work As New cv.Mat

        work = task.pcSplit(1).InRange(-threshold, threshold)
        work.SetTo(0, task.noDepthMask)
        work.ConvertTo(dst1, cv.MatType.CV_8U)
        Dim hPoints = dst1.FindNonZero()
        If hPoints.Total > 0 Then
            Dim sampleX1 As Integer, sampleX2 As Integer
            Dim y1 = findFirst(hPoints, True, sampleX1)
            Dim y2 = findLast(hPoints, True, sampleX2)

            ' This is because FindNonZero works from the top of the image down.  
            ' If the horizon has a positive slope, the first point found will be on the right.
            ' if the horizon has a negative slope, the first point found will be on the left.
            If sampleX1 < dst2.Width / 2 Then
                yLeft = y1
                yRight = y2
            Else
                yLeft = y2
                yRight = y1
            End If
        Else
            yLeft = 0
            yRight = 0
        End If

        work = task.pcSplit(0).InRange(-threshold, threshold)
        work.SetTo(0, task.noDepthMask)
        work.ConvertTo(dst3, cv.MatType.CV_8U)
        Dim gPoints = dst3.FindNonZero()
        Dim sampleUnused As Integer
        xTop = findFirst(gPoints, False, sampleUnused)
        xBot = findLast(gPoints, False, sampleUnused)

        If standaloneTest() Then
            Dim lineHorizon As lpData, lineGravity As lpData
            If hPoints.Total > 0 Then
                lineHorizon = New lpData(New cv.Point(0, yLeft), New cv.Point(dst2.Width, yRight))
            Else
                lineHorizon = New lpData
            End If

            lineGravity = New lpData(New cv.Point(xTop, 0), New cv.Point(xBot, dst2.Height))

            dst2.SetTo(0)
            DrawLine(dst2, lineGravity.p1, lineGravity.p2, task.highlight)
            DrawLine(dst2, lineHorizon.p1, lineHorizon.p2, cv.Scalar.Red)
        End If
    End Sub
End Class





Public Class XO_Horizon_FindNonZero : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        task.lineGravity = New lpData(New cv.Point2f(dst2.Width / 2, 0),
                                             New cv.Point2f(dst2.Width / 2, dst2.Height))
        task.lineHorizon = New lpData(New cv.Point2f(0, dst2.Height / 2), New cv.Point2f(dst2.Width, dst2.Height / 2))
        labels = {"", "Horizon vector mask", "Crosshairs - lineGravity (vertical) and lineHorizon (horizontal)", "Gravity vector mask"}
        desc = "Create lines for the gravity vector and horizon vector in the camera image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pc = task.pointCloud
        Dim split = pc.Split()
        split(2).SetTo(task.MaxZmeters)
        cv.Cv2.Merge(split, pc)

        pc = (pc.Reshape(1, pc.Rows * pc.Cols) * task.gMatrix).ToMat.Reshape(3, pc.Rows)

        dst1 = split(1).InRange(-0.05, 0.05)
        dst1.SetTo(0, task.noDepthMask)
        Dim pointsMat = dst1.FindNonZero()
        If pointsMat.Rows > 0 Then
            dst2.SetTo(0)
            Dim xVals As New List(Of Integer)
            Dim points As New List(Of cv.Point)
            For i = 0 To pointsMat.Rows - 1
                Dim pt = pointsMat.Get(Of cv.Point)(i, 0)
                xVals.Add(pt.X)
                points.Add(New cv.Point2f(pt.X, pt.Y))
            Next

            Dim p1 = points(xVals.IndexOf(xVals.Min()))
            Dim p2 = points(xVals.IndexOf(xVals.Max()))

            Dim lp = findEdgePoints(New lpData(p1, p2))
            task.lineHorizon = New lpData(lp.p1, lp.p2)
            DrawLine(dst2, task.lineHorizon.p1, task.lineHorizon.p2, 255)
        End If

        dst3 = split(0).InRange(-0.01, 0.01)
        dst3.SetTo(0, task.noDepthMask)
        pointsMat = dst3.FindNonZero()
        If pointsMat.Rows > 0 Then
            Dim yVals As New List(Of Integer)
            Dim points = New List(Of cv.Point)
            For i = 0 To pointsMat.Rows - 1
                Dim pt = pointsMat.Get(Of cv.Point)(i, 0)
                yVals.Add(pt.Y)
                points.Add(New cv.Point2f(pt.X, pt.Y))
            Next

            Dim p1 = points(yVals.IndexOf(yVals.Min()))
            Dim p2 = points(yVals.IndexOf(yVals.Max()))
            If Math.Abs(p1.X - p2.X) < 2 Then
                task.lineGravity = New lpData(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
            Else
                Dim lp = findEdgePoints(New lpData(p1, p2))
                task.lineGravity = New lpData(lp.p1, lp.p2)
            End If
            DrawLine(dst2, task.lineGravity.p1, task.lineGravity.p2, 255)
        End If
    End Sub
End Class







Public Class XO_Horizon_Perpendicular : Inherits TaskParent
    Dim perp As New Line_PerpendicularTest
    Public Sub New()
        labels(2) = "Yellow line is the perpendicular to the horizon.  White is gravity vector from the IMU."
        desc = "Find the gravity vector using the perpendicular to the horizon."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src
        DrawLine(dst2, task.lineHorizon.p1, task.lineHorizon.p2, white)

        perp.input = task.lineHorizon
        perp.Run(src)
        DrawLine(dst2, perp.output.p1, perp.output.p2, cv.Scalar.Yellow)

        Dim gVec = task.lineGravity
        gVec.p1.X += 10
        gVec.p2.X += 10
        DrawLine(dst2, gVec.p1, gVec.p2, white)
    End Sub
End Class





Public Class XO_Horizon_Regress : Inherits TaskParent
    Dim horizon As New XO_Horizon_Basics
    Dim regress As New LinearRegression_Basics
    Public Sub New()
        desc = "Collect the horizon points and run a linear regression on all the points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        horizon.Run(src)

        For i = 0 To horizon.points.Count - 1
            regress.x.Add(horizon.points(i).X)
            regress.y.Add(horizon.points(i).Y)
        Next

        regress.Run(Nothing)
        horizon.displayResults(regress.p1, regress.p2)
        dst2 = horizon.dst2
    End Sub
End Class





Public Class XO_Horizon_Basics : Inherits TaskParent
    Public points As New List(Of cv.Point)
    Dim resizeRatio As Integer = 1
    Public vec As New lpData
    Public autoDisplay As Boolean
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find all the points where depth Y-component transitions from positive to negative"
    End Sub
    Public Sub displayResults(p1 As cv.Point2f, p2 As cv.Point2f)
        If task.heartBeat Then
            If p1.Y >= 1 And p1.Y <= dst2.Height - 1 Then
                strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf
            End If
        End If

        dst2.SetTo(0)
        For Each pt In points
            pt = New cv.Point(pt.X * resizeRatio, pt.Y * resizeRatio)
            DrawCircle(dst2, pt, task.DotSize, white)
        Next

        DrawLine(dst2, vec.p1, vec.p2, 255)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = task.gravitySplit(1) Else dst0 = src

        dst0 = dst0.Abs()
        dst1 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        dst0.SetTo(task.MaxZmeters, Not dst1)

        points.Clear()
        For i = dst0.Width / 3 To dst0.Width * 2 / 3 - 1
            Dim mm1 = GetMinMax(dst0.Col(i))
            If mm1.minVal > 0 And mm1.minVal < 0.005 Then
                dst0.Col(i).Set(Of Single)(mm1.minLoc.Y, mm1.minLoc.X, 10)
                Dim mm2 = GetMinMax(dst0.Col(i))
                If mm2.minVal > 0 And Math.Abs(mm1.minLoc.Y - mm2.minLoc.Y) <= 1 Then
                    points.Add(New cv.Point(i, mm1.minLoc.Y))
                End If
            End If
        Next

        labels(2) = CStr(points.Count) + " points found. "
        Dim p1 As cv.Point
        Dim p2 As cv.Point
        If points.Count >= 2 Then
            p1 = New cv.Point(resizeRatio * points(points.Count - 1).X, resizeRatio * points(points.Count - 1).Y)
            p2 = New cv.Point(resizeRatio * points(0).X, resizeRatio * points(0).Y)
        End If

        Dim distance = p1.DistanceTo(p2)
        If distance < 10 Then ' enough to get a line with some credibility
            points.Clear()
            vec = New lpData
            strOut = "Horizon not found " + vbCrLf + "The distance of p1 to p2 is " + CStr(CInt(distance)) + " pixels."
        Else
            Dim lp = findEdgePoints(New lpData(p1, p2))
            vec = New lpData(lp.p1, lp.p2)
            If standaloneTest() Or autoDisplay Then
                displayResults(p1, p2)
                displayResults(New cv.Point(-p1.Y, p1.X), New cv.Point(p2.Y, -p2.X))
            End If
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class XO_Horizon_Validate : Inherits TaskParent
    Dim match As New Match_Basics
    Dim ptLeft As New cv.Point2f, ptRight As New cv.Point2f
    Dim leftTemplate As cv.Mat, rightTemplate As cv.Mat
    Dim options As New Options_Features
    Public Sub New()
        desc = "Validate the horizon points using Match_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pad = task.brickSize / 2

        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.heartBeat Then
            ptLeft = task.lineGravity.p1
            ptRight = task.lineGravity.p2
            Dim r = ValidateRect(New cv.Rect(ptLeft.X - pad, ptLeft.Y - pad, task.brickSize, task.brickSize))
            leftTemplate = src(r)

            r = ValidateRect(New cv.Rect(ptRight.X - pad, ptRight.Y - pad, task.brickSize, task.brickSize))
            rightTemplate = src(r)
        Else
            Dim r = ValidateRect(New cv.Rect(ptLeft.X - pad, ptLeft.Y - pad, task.brickSize, task.brickSize))
            match.template = leftTemplate.Clone
            match.Run(src)
            ptLeft = match.newCenter

            r = ValidateRect(New cv.Rect(ptRight.X - pad, ptRight.Y - pad, task.brickSize, task.brickSize))
            match.template = leftTemplate.Clone
            match.Run(src)
            ptLeft = match.newCenter
        End If
    End Sub
End Class






Public Class XO_Horizon_ExternalTest : Inherits TaskParent
    Dim horizon As New XO_Horizon_Basics
    Public Sub New()
        desc = "Supply the point cloud input to Horizon_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst0 = task.gravitySplit(1)
        horizon.Run(dst0)
        dst2 = horizon.dst2
    End Sub
End Class





Public Class XO_Gravity_Basics : Inherits TaskParent
    Public points As New List(Of cv.Point2f)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find all the points where depth X-component transitions from positive to negative"
    End Sub
    Public Sub displayResults(p1 As cv.Point, p2 As cv.Point)
        If task.heartBeat Then
            If p1.Y >= 1 And p1.Y <= dst2.Height - 1 Then strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf
        End If

        dst2.SetTo(0)
        dst3.SetTo(0)
        For Each pt In points
            DrawCircle(dst2, pt, task.DotSize, white)
        Next

        DrawLine(dst2, task.lineGravity.p1, task.lineGravity.p2, white)
        DrawLine(dst3, task.lineGravity.p1, task.lineGravity.p2, white)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = task.gravitySplit(0) Else dst0 = src

        dst0 = dst0.Abs()
        dst1 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        dst0.SetTo(task.MaxZmeters, Not dst1)

        points.Clear()
        For i = dst0.Height / 3 To dst0.Height * 2 / 3 - 1
            Dim mm1 = GetMinMax(dst0.Row(i))
            If mm1.minVal > 0 And mm1.minVal < 0.005 Then
                dst0.Row(i).Set(Of Single)(mm1.minLoc.Y, mm1.minLoc.X, 10)
                Dim mm2 = GetMinMax(dst0.Row(i))
                If mm2.minVal > 0 And Math.Abs(mm1.minLoc.X - mm2.minLoc.X) <= 1 Then points.Add(New cv.Point(mm1.minLoc.X, i))
            End If
        Next

        labels(2) = CStr(points.Count) + " points found. "
        Dim p1 As cv.Point2f
        Dim p2 As cv.Point2f
        If points.Count >= 2 Then
            p1 = New cv.Point2f(points(points.Count - 1).X, points(points.Count - 1).Y)
            p2 = New cv.Point2f(points(0).X, points(0).Y)
        End If

        Dim distance = p1.DistanceTo(p2)
        If distance < 10 Then ' enough to get a line with some credibility
            strOut = "Gravity vector not found " + vbCrLf + "The distance of p1 to p2 is " +
                         CStr(CInt(distance)) + " pixels." + vbCrLf
            strOut += "Using the previous value for the gravity vector."
        Else
            Dim lp = findEdgePoints(New lpData(p1, p2))
            task.lineGravity = New lpData(lp.p1, lp.p2)
            If standaloneTest() Then displayResults(p1, p2)
        End If

        task.lineHorizon = Line_PerpendicularTest.computePerp(task.lineGravity)
        SetTrueText(strOut, 3)
    End Sub
End Class




Public Class XO_CameraMotion_Basics : Inherits TaskParent
    Public translationX As Integer
    Public translationY As Integer
    Public secondOpinion As Boolean
    Dim feat As New Swarm_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        task.gOptions.DebugSlider.Value = 3
        desc = "Merge with previous image using just translation of the gravity vector and horizon vector (if present)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lineGravity = New lpData(task.lineGravity.p1, task.lineGravity.p2)
        Dim lineHorizon = New lpData(task.lineHorizon.p1, task.lineHorizon.p2)

        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        translationX = task.gOptions.DebugSlider.Value ' Math.Round(lineGravity.p1.X - task.lineGravity.p1.X)
        translationY = task.gOptions.DebugSlider.Value ' Math.Round(lineHorizon.p1.Y - task.lineHorizon.p1.Y)
        If Math.Abs(translationX) >= dst2.Width / 2 Then translationX = 0
        If lineHorizon.p1.Y >= dst2.Height Or lineHorizon.p2.Y >= dst2.Height Or Math.Abs(translationY) >= dst2.Height / 2 Then
            lineHorizon = New lpData(New cv.Point2f, New cv.Point2f(336, 0))
            translationY = 0
        End If

        Dim r1 As cv.Rect, r2 As cv.Rect
        If translationX = 0 And translationY = 0 Then
            dst2 = src
            task.camMotionPixels = 0
            task.camDirection = 0
        Else
            ' dst2.SetTo(0)
            r1 = New cv.Rect(translationX, translationY, Math.Min(dst2.Width - translationX * 2, dst2.Width),
                                                             Math.Min(dst2.Height - translationY * 2, dst2.Height))
            If r1.X < 0 Then
                r1.X = -r1.X
                r1.Width += translationX * 2
            End If
            If r1.Y < 0 Then
                r1.Y = -r1.Y
                r1.Height += translationY * 2
            End If

            r2 = New cv.Rect(Math.Abs(translationX), Math.Abs(translationY), r1.Width, r1.Height)

            task.camMotionPixels = Math.Sqrt(translationX * translationX + translationY * translationY)
            If translationX = 0 Then
                If translationY < 0 Then task.camDirection = Math.PI / 4 Else task.camDirection = Math.PI * 3 / 4
            Else
                task.camDirection = Math.Atan(translationY / translationX)
            End If

            If secondOpinion Then
                dst3.SetTo(0)
                ' the point cloud contributes one set of camera motion distance and direction.  Now confirm it with feature points
                feat.Run(src)
                strOut = "Swarm distance = " + Format(feat.distanceAvg, fmt1) + " when camMotionPixels = " + Format(task.camMotionPixels, fmt1)
                If (feat.distanceAvg < task.camMotionPixels / 2) Or task.heartBeat Then
                    task.camMotionPixels = 0
                    src.CopyTo(dst2)
                End If
                dst3 = (src - dst2).ToMat.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            End If
        End If

        lineGravity = New lpData(task.lineGravity.p1, task.lineGravity.p2)
        lineHorizon = New lpData(task.lineHorizon.p1, task.lineHorizon.p2)
        SetTrueText(strOut, 3)

        labels(2) = "Translation (X, Y) = (" + CStr(translationX) + ", " + CStr(translationY) + ")" +
                        If(lineHorizon.p1.Y = 0 And lineHorizon.p2.Y = 0, " there is no horizon present", "")
        labels(3) = "Camera direction (radians) = " + Format(task.camDirection, fmt1) + " with distance = " + Format(task.camMotionPixels, fmt1)
    End Sub
End Class




Public Class XO_CameraMotion_WithRotation : Inherits TaskParent
    Public translationX As Single
    Public rotationX As Single
    Public centerX As cv.Point2f
    Public translationY As Single
    Public rotationY As Single
    Public centerY As cv.Point2f
    Public rotate As New Rotate_BasicsQT
    Dim lineGravity As lpData
    Dim lineHorizon As lpData
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Merge with previous image using rotation AND translation of the camera motion - not as good as translation alone."
    End Sub
    Public Sub translateRotateX(x1 As Integer, x2 As Integer)
        rotationX = Math.Atan(Math.Abs((x1 - x2)) / dst2.Height) * 57.2958
        centerX = New cv.Point2f((task.lineGravity.p1.X + task.lineGravity.p2.X) / 2, (task.lineGravity.p1.Y + task.lineGravity.p2.Y) / 2)
        If x1 >= 0 And x2 > 0 Then
            translationX = If(x1 > x2, x1 - x2, x2 - x1)
            centerX = task.lineGravity.p2
        ElseIf x1 <= 0 And x2 < 0 Then
            translationX = If(x1 > x2, x1 - x2, x2 - x1)
            centerX = task.lineGravity.p1
        ElseIf x1 < 0 And x2 > 0 Then
            translationX = 0
        Else
            translationX = 0
            rotationX *= -1
        End If
    End Sub
    Public Sub translateRotateY(y1 As Integer, y2 As Integer)
        rotationY = Math.Atan(Math.Abs((y1 - y2)) / dst2.Width) * 57.2958
        centerY = New cv.Point2f((task.lineHorizon.p1.X + task.lineHorizon.p2.X) / 2, (task.lineHorizon.p1.Y + task.lineHorizon.p2.Y) / 2)
        If y1 > 0 And y2 > 0 Then
            translationY = If(y1 > y2, y1 - y2, y2 - y1)
            centerY = task.lineHorizon.p2
        ElseIf y1 < 0 And y2 < 0 Then
            translationY = If(y1 > y2, y1 - y2, y2 - y1)
            centerY = task.lineHorizon.p1
        ElseIf y1 < 0 And y2 > 0 Then
            translationY = 0
        Else
            translationY = 0
            rotationY *= -1
        End If
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.firstPass Then
            lineGravity = task.lineGravity
            lineHorizon = task.lineHorizon
        End If

        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim x1 = lineGravity.p1.X - task.lineGravity.p1.X
        Dim x2 = lineGravity.p2.X - task.lineGravity.p2.X

        Dim y1 = lineHorizon.p1.Y - task.lineHorizon.p1.Y
        Dim y2 = lineHorizon.p2.Y - task.lineHorizon.p2.Y

        translateRotateX(x1, x2)
        translateRotateY(y1, y2)

        dst1.SetTo(0)
        dst3.SetTo(0)
        If Math.Abs(x1 - x2) > 0.5 Or Math.Abs(y1 - y2) > 0.5 Then
            Dim r1 = New cv.Rect(translationX, translationY, dst2.Width - translationX, dst2.Height - translationY)
            Dim r2 = New cv.Rect(0, 0, r1.Width, r1.Height)
            dst1(r2) = src(r1)
            rotate.rotateAngle = rotationY
            rotate.rotateCenter = centerY
            rotate.Run(dst1)
            dst2 = rotate.dst2
            dst3 = (src - dst2).ToMat.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
        Else
            dst2 = src
        End If

        lineGravity = task.lineGravity
        lineHorizon = task.lineHorizon

        labels(2) = "Translation X = " + Format(translationX, fmt1) + " rotation X = " + Format(rotationX, fmt1) + " degrees " +
                        " center of rotation X = " + Format(centerX.X, fmt0) + ", " + Format(centerX.Y, fmt0)
        labels(3) = "Translation Y = " + Format(translationY, fmt1) + " rotation Y = " + Format(rotationY, fmt1) + " degrees " +
                        " center of rotation Y = " + Format(centerY.X, fmt0) + ", " + Format(centerY.Y, fmt0)
    End Sub
End Class






Public Class XO_Rotate_Horizon : Inherits TaskParent
    Dim rotate As New Rotate_Basics
    Dim edges As New XO_CameraMotion_WithRotation
    Public Sub New()
        OptionParent.FindSlider("Rotation Angle in degrees X100").Value = 3
        labels(2) = "White is the current horizon vector of the camera.  Highlighted color is the rotated horizon vector."
        desc = "Rotate the horizon independently from the rotation of the image to validate the Edge_CameraMotion algorithm."
    End Sub
    Function RotatePoint(point As cv.Point2f, center As cv.Point2f, angle As Double) As cv.Point2f
        Dim radians As Double = angle * (cv.Cv2.PI / 180.0)

        Dim sinAngle As Double = Math.Sin(radians)
        Dim cosAngle As Double = Math.Cos(radians)

        Dim x As Double = point.X - center.X
        Dim y As Double = point.Y - center.Y

        Dim xNew As Double = x * cosAngle - y * sinAngle
        Dim yNew As Double = x * sinAngle + y * cosAngle

        xNew += center.X
        yNew += center.Y

        Return New cv.Point2f(xNew, yNew)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        rotate.Run(src)
        dst2 = rotate.dst2.Clone
        dst1 = dst2.Clone

        Dim lineHorizon = New lpData(task.lineHorizon.p1, task.lineHorizon.p2)

        lineHorizon.p1 = RotatePoint(task.lineHorizon.p1, rotate.rotateCenter, -rotate.rotateAngle)
        lineHorizon.p2 = RotatePoint(task.lineHorizon.p2, rotate.rotateCenter, -rotate.rotateAngle)

        DrawLine(dst2, lineHorizon.p1, lineHorizon.p2, task.highlight)
        DrawLine(dst2, task.lineHorizon.p1, task.lineHorizon.p2, white)

        Dim y1 = lineHorizon.p1.Y - task.lineHorizon.p1.Y
        Dim y2 = lineHorizon.p2.Y - task.lineHorizon.p2.Y
        edges.translateRotateY(y1, y2)

        rotate.rotateAngle = edges.rotationY
        rotate.rotateCenter = edges.centerY
        rotate.Run(dst1)
        dst3 = rotate.dst2.Clone

        strOut = edges.strOut
    End Sub
End Class






Public Class XO_Gravity_BasicsOriginal : Inherits TaskParent
    Public vec As New lpData
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Search for the transition from positive to negative to find the gravity vector."
    End Sub
    Private Function findTransition(startRow As Integer, stopRow As Integer, stepRow As Integer) As cv.Point2f
        Dim val As Single, lastVal As Single
        Dim ptX As New List(Of Single)
        Dim ptY As New List(Of Single)
        For y = startRow To stopRow Step stepRow
            For x = 0 To dst0.Cols - 1
                lastVal = val
                val = dst0.Get(Of Single)(y, x)
                If val > 0 And lastVal < 0 Then
                    ' change to sub-pixel accuracy here 
                    Dim pt = New cv.Point2f(x + Math.Abs(val) / Math.Abs(val - lastVal), y)
                    ptX.Add(pt.X)
                    ptY.Add(pt.Y)
                    If ptX.Count >= task.frameHistoryCount Then Return New cv.Point2f(ptX.Average, ptY.Average)
                End If
            Next
        Next
        Return New cv.Point
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = task.gravitySplit(0) Else dst0 = src

        Dim p1 = findTransition(0, dst0.Height - 1, 1)
        Dim p2 = findTransition(dst0.Height - 1, 0, -1)
        Dim lp = findEdgePoints(New lpData(p1, p2))
        vec = New lpData(lp.p1, lp.p2)

        If p1.X >= 1 Then
            strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf + "      val =  " +
                          Format(dst0.Get(Of Single)(p1.Y, p1.X)) + vbCrLf + "lastVal = " + Format(dst0.Get(Of Single)(p1.Y, p1.X - 1))
        End If
        SetTrueText(strOut, 3)

        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, vec.p1, vec.p2, 255)
        End If
    End Sub
End Class




Public Class XO_Depth_MinMaxToVoronoi : Inherits TaskParent
    Public Sub New()
        task.kalman = New Kalman_Basics
        ReDim task.kalman.kInput(task.gridRects.Count * 4 - 1)

        labels = {"", "", "Red is min distance, blue is max distance", "Voronoi representation of min point (only) for each cell."}
        desc = "Find min and max depth in each roi and create a voronoi representation using the min and max points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then ReDim task.kalman.kInput(task.gridRects.Count * 4 - 1)

        Parallel.For(0, task.gridRects.Count,
            Sub(i)
                Dim roi = task.gridRects(i)
                Dim mm As mmData = GetMinMax(task.pcSplit(2)(roi), task.depthMask(roi))
                If mm.minLoc.X < 0 Or mm.minLoc.Y < 0 Then mm.minLoc = New cv.Point2f(0, 0)
                task.kalman.kInput(i * 4) = mm.minLoc.X
                task.kalman.kInput(i * 4 + 1) = mm.minLoc.Y
                task.kalman.kInput(i * 4 + 2) = mm.maxLoc.X
                task.kalman.kInput(i * 4 + 3) = mm.maxLoc.Y
            End Sub)

        task.kalman.Run(emptyMat)

        Static minList(task.gridRects.Count - 1) As cv.Point2f
        Static maxList(task.gridRects.Count - 1) As cv.Point2f
        For i = 0 To task.gridRects.Count - 1
            Dim rect = task.gridRects(i)
            If task.motionBasics.motionFlags(i) Then
                Dim ptmin = New cv.Point2f(task.kalman.kOutput(i * 4) + rect.X, task.kalman.kOutput(i * 4 + 1) + rect.Y)
                Dim ptmax = New cv.Point2f(task.kalman.kOutput(i * 4 + 2) + rect.X, task.kalman.kOutput(i * 4 + 3) + rect.Y)
                ptmin = lpData.validatePoint(ptmin)
                ptmax = lpData.validatePoint(ptmax)
                minList(i) = ptmin
                maxList(i) = ptmax
            End If
        Next

        dst1 = src.Clone()
        dst1.SetTo(white, task.gridMask)
        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, src.Width, src.Height))
        For i = 0 To minList.Count - 1
            Dim ptMin = minList(i)
            subdiv.Insert(ptMin)
            DrawCircle(dst1, ptMin, task.DotSize, cv.Scalar.Red)
            DrawCircle(dst1, maxList(i), task.DotSize, cv.Scalar.Blue)
        Next

        If task.optionsChanged Then dst2 = dst1.Clone Else dst1.CopyTo(dst2, task.motionMask)

        Dim facets = New cv.Point2f()() {Nothing}
        Dim centers() As cv.Point2f = Nothing
        subdiv.GetVoronoiFacetList(New List(Of Integer)(), facets, centers)

        Dim ifacet() As cv.Point
        Dim ifacets = New cv.Point()() {Nothing}

        For i = 0 To facets.Length - 1
            ReDim ifacet(facets(i).Length - 1)
            For j = 0 To facets(i).Length - 1
                ifacet(j) = New cv.Point(Math.Round(facets(i)(j).X), Math.Round(facets(i)(j).Y))
            Next
            ifacets(0) = ifacet
            dst3.FillConvexPoly(ifacet, task.scalarColors(i Mod task.scalarColors.Length), task.lineType)
            cv.Cv2.Polylines(dst3, ifacets, True, cv.Scalar.Black, task.lineWidth, task.lineType, 0)
        Next
    End Sub
End Class









Public Class XO_Brick_GrayScaleTest : Inherits TaskParent
    Dim options As New Options_Stdev
    Public Sub New()
        task.needBricks = True
        labels(3) = "bricks where grayscale stdev and average of the 3 color stdev's"
        desc = "Is the average of the color stdev's the same as the stdev of the grayscale?"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        Dim threshold = options.stdevThreshold

        Dim pt = task.brickD.rect.TopLeft
        Dim grayMean As cv.Scalar, grayStdev As cv.Scalar
        Dim ColorMean As cv.Scalar, colorStdev As cv.Scalar
        Static saveTrueData As New List(Of TrueText)
        If task.heartBeat Then
            dst3.SetTo(0)
            dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim count As Integer
            For Each brick In task.bricks.brickList
                cv.Cv2.MeanStdDev(dst2(brick.rect), grayMean, grayStdev)
                cv.Cv2.MeanStdDev(task.color(brick.rect), ColorMean, colorStdev)
                Dim nextColorStdev = (colorStdev(0) + colorStdev(1) + colorStdev(2)) / 3
                Dim diff = Math.Abs(grayStdev(0) - nextColorStdev)
                If diff > threshold Then
                    dst2.Rectangle(brick.rect, 255, task.lineWidth)
                    SetTrueText(Format(grayStdev(0), fmt1) + " " + Format(colorStdev, fmt1), brick.rect.TopLeft, 2)
                    dst3.Rectangle(brick.rect, task.highlight, task.lineWidth)
                    SetTrueText(Format(diff, fmt1), brick.rect.TopLeft, 3)
                    count += 1
                End If
            Next
            labels(2) = "There were " + CStr(count) + " cells where the difference was greater than " + CStr(threshold)
        End If

        If trueData.Count > 0 Then saveTrueData = New List(Of TrueText)(trueData)
        trueData = New List(Of TrueText)(saveTrueData)
    End Sub
End Class






Public Class XO_Feature_AgastHeartbeat : Inherits TaskParent
    Dim stablePoints As List(Of cv.Point2f)
    Dim agastFD As cv.AgastFeatureDetector
    Dim lastPoints As List(Of cv.Point2f)
    Public Sub New()
        agastFD = cv.AgastFeatureDetector.Create(10, True, cv.AgastFeatureDetector.DetectorType.OAST_9_16)
        desc = "Use the Agast Feature Detector in the OpenCV Contrib."
        stablePoints = New List(Of cv.Point2f)()
        lastPoints = New List(Of cv.Point2f)()
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim resizeFactor As Integer = 1
        Dim input As New cv.Mat()
        If src.Cols >= 1280 Then
            cv.Cv2.Resize(src, input, New cv.Size(src.Cols \ 4, src.Rows \ 4))
            resizeFactor = 4
        Else
            input = src
        End If
        Dim keypoints As cv.KeyPoint() = agastFD.Detect(input)
        If task.heartBeat OrElse lastPoints.Count < 10 Then
            lastPoints.Clear()
            For Each kpt As cv.KeyPoint In keypoints
                lastPoints.Add(kpt.Pt)
            Next
        End If
        stablePoints.Clear()
        dst2 = src.Clone()
        For Each pt As cv.KeyPoint In keypoints
            Dim p1 As New cv.Point2f(CSng(Math.Round(pt.Pt.X * resizeFactor)), CSng(Math.Round(pt.Pt.Y * resizeFactor)))
            If lastPoints.Contains(p1) Then
                stablePoints.Add(p1)
                DrawCircle(dst2, p1, task.DotSize, New cv.Scalar(0, 0, 255))
            End If
        Next
        lastPoints = New List(Of cv.Point2f)(stablePoints)
        If task.midHeartBeat Then
            labels(2) = $"{keypoints.Length} features found and {stablePoints.Count} of them were stable"
        End If
        labels(2) = $"Found {keypoints.Length} features"
    End Sub
End Class







Public Class XO_Line_BasicsRawOld : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public ptList As New List(Of cv.Point)
    Public subsetRect As cv.Rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
    Public Sub New()
        dst2 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
                   "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src(subsetRect))

        lpList.Clear()
        ptList.Clear()
        lpList.Add(New lpData) ' zero placeholder.
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
                   v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = lpData.validatePoint(New cv.Point(CInt(v(0) + subsetRect.X), CInt(v(1) + subsetRect.Y)))
                Dim p2 = lpData.validatePoint(New cv.Point(CInt(v(2) + subsetRect.X), CInt(v(3) + subsetRect.Y)))
                Dim lp = New lpData(p1, p2)
                lpList.Add(lp)
                ptList.Add(New cv.Point(CInt(lp.p1.X), CInt(lp.p1.Y)))
            End If
        Next

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next
        labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
    End Sub
    Public Sub Close()
        ld.Dispose()
    End Sub
End Class







Public Class XO_Line_LeftRight : Inherits TaskParent
    Dim lineCore As New XO_Line_Core
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Show lines in both the right and left images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2.Clone
        labels(2) = "Left view" + task.lines.labels(2)

        dst1 = task.rightView
        lineCore.Run(task.rightView)
        dst3 = lineCore.dst2.Clone
        labels(3) = "Right View: " + lineCore.labels(2)

        If standalone Then
            If task.gOptions.DebugCheckBox.Checked Then
                dst2.SetTo(0, task.noDepthMask)
                dst3.SetTo(0, task.noDepthMask)
            End If
        Else
            If task.toggleOn Then
                dst2.SetTo(0, task.noDepthMask)
                dst3.SetTo(0, task.noDepthMask)
            End If
        End If
    End Sub
End Class




Public Class XO_Line_Matching : Inherits TaskParent
    Public options As New Options_Line
    Dim lpMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Dim lpList As New List(Of lpData)
    Public Sub New()
        labels(2) = "Highlighted lines were combined from 2 lines.  Click on Line_Core in Treeview to see."
        desc = "Combine lines that are approximately the same line."
    End Sub
    Private Function combine2Lines(lp1 As lpData, lp2 As lpData) As lpData
        If Math.Abs(lp1.slope) >= 1 Then
            If lp1.p1.Y < lp2.p1.Y Then
                Return New lpData(lp1.p1, lp2.p2)
            Else
                Return New lpData(lp2.p1, lp1.p2)
            End If
        Else
            If lp1.p1.X < lp2.p1.X Then
                Return New lpData(lp1.p1, lp2.p2)
            Else
                Return New lpData(lp2.p1, lp1.p2)
            End If
        End If
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        dst2 = src.Clone

        If task.firstPass Then OptionParent.FindSlider("Min Line Length").Value = 30

        Dim tolerance = 0.1
        Dim newSet As New List(Of lpData)
        Dim removeList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        Dim addList As New List(Of lpData)
        Dim combineCount As Integer
        For i = 0 To task.lines.lpList.Count - 1
            Dim lp = task.lines.lpList(i)
            Dim lpRemove As Boolean = False
            For j = 0 To 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                Dim val = lpMap.Get(Of Integer)(pt.Y, pt.X)
                If val = 0 Then Continue For
                Dim mp = lpList(val - 1)
                If Math.Abs(mp.slope - lp.slope) < tolerance Then
                    Dim lpNew = combine2Lines(lp, mp)
                    If lpNew IsNot Nothing Then
                        addList.Add(lpNew)
                        DrawLine(dst2, lpNew.p1, lpNew.p2, task.highlight)
                        If removeList.Values.Contains(j) = False Then removeList.Add(j, j)
                        lpRemove = True
                        combineCount += 1
                    End If
                End If
            Next
            If lpRemove Then
                If removeList.Values.Contains(i) = False Then removeList.Add(i, i)
            End If
        Next

        For i = 0 To removeList.Count - 1
            task.lines.lpList.RemoveAt(removeList.ElementAt(i).Value)
        Next

        For Each lp In addList
            task.lines.lpList.Add(lp)
        Next
        lpList = New List(Of lpData)(task.lines.lpList)
        lpMap.SetTo(0)
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            If lp.length > options.minLength Then lpMap.Line(lp.p1, lp.p2, i + 1, 2, cv.LineTypes.Link8)
        Next
        lpMap.ConvertTo(dst3, cv.MatType.CV_8U)
        dst3 = dst3.Threshold(0, cv.Scalar.White, cv.ThresholdTypes.Binary)
        If task.heartBeat Then
            labels(2) = CStr(task.lines.lpList.Count) + " lines were input and " + CStr(combineCount) +
                            " lines were matched to the previous frame"
        End If
    End Sub
End Class





Public Class XO_Line_TopX : Inherits TaskParent
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        labels(3) = "The top X lines by length..."
        desc = "Isolate the top X lines by length - lines are already sorted by length."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2
        labels(2) = task.lines.labels(2)

        dst3.SetTo(0)
        For i = 0 To 9
            Dim lp = task.lines.lpList(i)
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next
    End Sub
End Class






Public Class XO_Line_DisplayInfoOld : Inherits TaskParent
    Public tcells As New List(Of tCell)
    Dim canny As New Edge_Basics
    Dim blur As New Blur_Basics
    Public distance As Integer
    Public maskCount As Integer
    Dim myCurrentFrame As Integer = -1
    Public Sub New()
        dst1 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(2) = "When running standaloneTest(), a pair of random points is used to test the algorithm."
        desc = "Display the line provided in mp"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src
        If standaloneTest() And task.heartBeat Then
            Dim tc As New tCell
            tcells.Clear()
            For i = 0 To 2 - 1
                tc.center = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                tcells.Add(tc)
            Next
        End If
        If tcells.Count < 2 Then Exit Sub

        If myCurrentFrame < task.frameCount Then
            canny.Run(src)
            blur.Run(canny.dst2)
            myCurrentFrame = task.frameCount
        End If
        dst1.SetTo(0)
        Dim p1 = tcells(0).center
        Dim p2 = tcells(1).center
        DrawLine(dst1, p1, p2, 255)

        dst3.SetTo(0)
        blur.dst2.Threshold(1, 255, cv.ThresholdTypes.Binary).CopyTo(dst3, dst1)
        distance = p1.DistanceTo(p2)
        maskCount = dst3.CountNonZero

        For Each tc In tcells
            'dst2.Rectangle(tc.rect, myhighlight)
            'dst2.Rectangle(tc.searchRect, white, task.lineWidth)
            SetTrueText(tc.strOut, New cv.Point(tc.rect.X, tc.rect.Y))
        Next

        strOut = "Mask count = " + CStr(maskCount) + ", Expected count = " + CStr(distance) + " or " + Format(maskCount / distance, "0%") + vbCrLf
        DrawLine(dst2, p1, p2, task.highlight)

        strOut += "Color changes when correlation falls below threshold and new line is detected." + vbCrLf +
                      "Correlation coefficient is shown with the depth in meters."
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class XO_Line_Cells : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Dim lines As New XO_Line_RawSorted
    Public Sub New()
        desc = "Identify all lines in the RedColor_Basics cell boundaries"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        lines.Run(dst2.Clone)
        dst3 = lines.dst2
        lpList = New List(Of lpData)(lines.lpList)
        labels(3) = "Number of lines identified: " + CStr(lpList.Count)
    End Sub
End Class








Public Class XO_Line_Canny : Inherits TaskParent
    Dim canny As New Edge_Basics
    Public lpList As New List(Of lpData)
    Dim lines As New XO_Line_RawSorted
    Public Sub New()
        labels(3) = "Input to Line_Basics"
        OptionParent.FindSlider("Canny Aperture").Value = 7
        OptionParent.FindSlider("Min Line Length").Value = 30
        desc = "Find lines in the Canny output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        canny.Run(src)
        dst3 = canny.dst2.Clone

        lines.Run(canny.dst2)

        dst2 = lines.dst2
        lpList = New List(Of lpData)(lines.lpList)
        labels(2) = "Number of lines identified: " + CStr(lpList.Count)
    End Sub
End Class








Public Class XO_Line_RegionsVB : Inherits TaskParent
    Dim lines As New XO_Line_TimeView
    Dim reduction As New Reduction_Basics
    Const lineMatch = 254
    Public Sub New()
        OptionParent.findRadio("Use Bitwise Reduction").Checked = True

        If OptionParent.FindFrm(traceName + " CheckBoxes") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Show intermediate vertical step results.")
            check.addCheckBox("Run horizontal without vertical step")
        End If

        desc = "Use the reduction values between lines to identify regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static noVertCheck = OptionParent.FindCheckBox("Run horizontal without vertical step")
        Static verticalCheck = OptionParent.FindCheckBox("Show intermediate vertical step results.")
        reduction.Run(src)
        dst2 = reduction.dst2
        dst3 = dst2.Clone

        lines.Run(src)

        Dim lineMask = lines.dst3
        dst2.SetTo(lineMatch, lineMask)
        dst3.SetTo(lineMatch, lineMask)

        Dim nextB As Byte
        Dim region As Integer = -1
        Dim indexer1 = dst2.GetGenericIndexer(Of Byte)()
        Dim indexer2 = dst3.GetGenericIndexer(Of Byte)()
        If noVertCheck.checked = False Then
            For x = 0 To dst2.Width - 1
                region = -1
                For y = 0 To dst2.Height - 1
                    nextB = indexer1(y, x)
                    If nextB = lineMatch Then
                        region = -1
                    Else
                        If region = -1 Then
                            region = nextB
                        Else
                            indexer1(y, x) = region
                        End If
                    End If
                Next
            Next
        End If

        For y = 0 To dst3.Height - 1
            region = -1
            For x = 0 To dst3.Width - 1
                nextB = indexer2(y, x)
                If nextB = lineMatch Then
                    region = -1
                Else
                    If region = -1 Then
                        If y = 0 Then
                            region = indexer1(y, x)
                        Else
                            Dim vals As New List(Of Integer)
                            Dim counts As New List(Of Integer)
                            For i = x To dst3.Width - 1
                                Dim nextVal = indexer1(y - 1, i)
                                If nextVal = lineMatch Then Exit For
                                If vals.Contains(nextVal) Then
                                    counts(vals.IndexOf(nextVal)) += 1
                                Else
                                    vals.Add(nextVal)
                                    counts.Add(1)
                                End If
                                Dim maxVal = counts.Max
                                region = vals(counts.IndexOf(maxVal))
                            Next
                        End If
                    Else
                        indexer2(y, x) = region
                    End If
                End If
            Next
        Next
        labels(2) = If(verticalCheck.checked, "Intermediate result of vertical step", "Lines detected (below) Regions detected (right image)")
        If noVertCheck.checked And verticalCheck.checked Then labels(2) = "Input to vertical step"
        If verticalCheck.checked = False Then dst2 = lines.dst2.Clone
    End Sub
End Class






Public Class XO_Line_Nearest : Inherits TaskParent
    Public pt As cv.Point2f ' How close is this point to the input line?
    Public lp As New lpData ' the input line.
    Public nearPoint As cv.Point2f
    Public onTheLine As Boolean
    Public distance As Single
    Public Sub New()
        labels(2) = "Yellow line is input line, white dot is the input point, and the white line is the nearest path to the input line."
        desc = "Find the nearest point on a line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() And task.heartBeat Then
            lp.p1 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            lp.p2 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            pt = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        End If

        Dim minX = Math.Min(lp.p1.X, lp.p2.X)
        Dim minY = Math.Min(lp.p1.Y, lp.p2.Y)
        Dim maxX = Math.Max(lp.p1.X, lp.p2.X)
        Dim maxY = Math.Max(lp.p1.Y, lp.p2.Y)

        onTheLine = True
        If lp.p1.X = lp.p2.X Then
            nearPoint = New cv.Point2f(lp.p1.X, pt.Y)
            If pt.Y < minY Or pt.Y > maxY Then onTheLine = False
        Else
            Dim m = (lp.p1.Y - lp.p2.Y) / (lp.p1.X - lp.p2.X)
            If m = 0 Then
                nearPoint = New cv.Point2f(pt.X, lp.p1.Y)
                If pt.X < minX Or pt.X > maxX Then onTheLine = False
            Else
                Dim b1 = lp.p1.Y - lp.p1.X * m

                Dim b2 = pt.Y + pt.X / m
                Dim a1 = New cv.Point2f(0, b2)
                Dim a2 = New cv.Point2f(dst2.Width, b2 + dst2.Width / m)
                Dim x = m * (b2 - b1) / (m * m + 1)
                nearPoint = New cv.Point2f(x, m * x + b1)

                If nearPoint.X < minX Or nearPoint.X > maxX Or nearPoint.Y < minY Or nearPoint.Y > maxY Then onTheLine = False
            End If
        End If

        Dim distance1 = Math.Sqrt(Math.Pow(pt.X - lp.p1.X, 2) + Math.Pow(pt.Y - lp.p1.Y, 2))
        Dim distance2 = Math.Sqrt(Math.Pow(pt.X - lp.p2.X, 2) + Math.Pow(pt.Y - lp.p2.Y, 2))
        If onTheLine = False Then nearPoint = If(distance1 < distance2, lp.p1, lp.p2)
        If standaloneTest() Then
            dst2.SetTo(0)
            DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
            DrawLine(dst2, pt, nearPoint, white)
            DrawCircle(dst2, pt, task.DotSize, white)
        End If
        distance = Math.Sqrt(Math.Pow(pt.X - nearPoint.X, 2) + Math.Pow(pt.Y - nearPoint.Y, 2))
    End Sub
End Class








Public Class XO_Line_TimeView : Inherits TaskParent
    Public frameList As New List(Of List(Of lpData))
    Public pixelcount As Integer
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Collect lines over time"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then frameList.Clear()
        Dim nextMpList = New List(Of lpData)(task.lines.lpList)
        frameList.Add(nextMpList)

        dst2 = src
        dst3.SetTo(0)
        lpList.Clear()
        Dim lineTotal As Integer
        For i = 0 To frameList.Count - 1
            lineTotal += frameList(i).Count
            For Each lp In frameList(i)
                DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
                DrawLine(dst3, lp.p1, lp.p2, white)
                lpList.Add(lp)
            Next
        Next

        If frameList.Count >= task.frameHistoryCount Then frameList.RemoveAt(0)
        pixelcount = dst3.CountNonZero
        labels(3) = "There were " + CStr(lineTotal) + " lines detected using " + Format(pixelcount / 1000, "#.0") + "k pixels"
    End Sub
End Class







Public Class XO_Line_ColorClass : Inherits TaskParent
    Dim color8U As New Color8U_Basics
    Dim lines As New XO_Line_RawSorted
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "", "Lines for the current color class", "Color Class input"}
        desc = "Review lines in all the different color classes"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        dst1 = color8U.dst2

        lines.Run(dst1 * 255 / color8U.classCount)
        dst2 = lines.dst2
        dst3 = lines.dst2

        labels(1) = "Input to Line_Basics"
        labels(2) = "Lines found in the " + color8U.classifier.traceName + " output"
    End Sub
End Class





Public Class XO_Line_FromContours : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim contours As New XO_Contour_Gray
    Dim lines As New XO_Line_RawSorted
    Public Sub New()
        task.gOptions.ColorSource.SelectedItem() = "Reduction_Basics" ' to enable sliders.
        task.gOptions.highlight.SelectedIndex = 3
        desc = "Find the lines in the contours."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        contours.Run(reduction.dst2)
        dst2 = contours.dst2.Clone
        lines.Run(dst2)

        dst3.SetTo(0)
        For Each lp In lines.lpList
            DrawLine(dst3, lp.p1, lp.p2, white)
        Next
    End Sub
End Class








Public Class XO_Line_ViewSide : Inherits TaskParent
    Public autoY As New XO_OpAuto_YRange
    Dim histSide As New Projection_HistSide
    Dim lines As New XO_Line_RawSorted
    Public Sub New()
        labels = {"", "", "Hotspots in the Side View", "Lines found in the hotspots of the Side View."}
        desc = "Find lines in the hotspots for the side view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histSide.Run(src)

        autoY.Run(histSide.histogram)
        dst2 = histSide.histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

        lines.Run(dst2.Clone)
        dst3 = lines.dst2
        labels(2) = lines.labels(2)
    End Sub
End Class







Public Class XO_Line_Movement : Inherits TaskParent
    Public p1 As cv.Point
    Public p2 As cv.Point
    Dim gradientColors(100) As cv.Scalar
    Dim frameCount As Integer
    Public Sub New()
        task.kalman = New Kalman_Basics
        task.kalman.kOutput = {0, 0, 0, 0}

        Dim color1 = cv.Scalar.Yellow, color2 = cv.Scalar.Blue
        Dim f As Double = 1.0
        For i = 0 To gradientColors.Length - 1
            gradientColors(i) = New cv.Scalar(f * color2(0) + (1 - f) * color1(0), f * color2(1) + (1 - f) * color1(1), f * color2(2) + (1 - f) * color1(2))
            f -= 1 / gradientColors.Length
        Next

        labels = {"", "", "Line Movement", ""}
        desc = "Show the movement of the line provided"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            Static k1 = p1
            Static k2 = p2
            If k1.DistanceTo(p1) = 0 And k2.DistanceTo(p2) = 0 Then
                k1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                k2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
                dst2.SetTo(0)
            End If
            task.kalman.kInput = {k1.X, k1.Y, k2.X, k2.Y}
            task.kalman.Run(emptyMat)
            p1 = New cv.Point(task.kalman.kOutput(0), task.kalman.kOutput(1))
            p2 = New cv.Point(task.kalman.kOutput(2), task.kalman.kOutput(3))
        End If
        frameCount += 1
        DrawLine(dst2, p1, p2, gradientColors(frameCount Mod gradientColors.Count))
    End Sub
End Class







Public Class XO_Line_InDepthAndBGR : Inherits TaskParent
    Public p1List As New List(Of cv.Point2f)
    Public p2List As New List(Of cv.Point2f)
    Public z1List As New List(Of cv.Point3f) ' the point cloud values corresponding to p1 and p2
    Public z2List As New List(Of cv.Point3f)
    Public Sub New()
        labels(2) = "Lines defined in BGR"
        labels(3) = "Lines in BGR confirmed in the point cloud"
        desc = "Find the BGR lines and confirm they are present in the cloud data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2
        If task.lines.lpList.Count = 0 Then Exit Sub

        Dim lineList = New List(Of cv.Rect)
        If task.optionsChanged Then dst3.SetTo(0)
        dst3.SetTo(0, task.motionMask)
        p1List.Clear()
        p2List.Clear()
        z1List.Clear()
        z2List.Clear()
        For Each lp In task.lines.lpList
            Dim rect = findRectFromLine(lp)
            Dim mask = New cv.Mat(New cv.Size(rect.Width, rect.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
            mask.Line(New cv.Point(CInt(lp.p1.X - rect.X), CInt(lp.p1.Y - rect.Y)),
                          New cv.Point(CInt(lp.p2.X - rect.X), CInt(lp.p2.Y - rect.Y)), 255, task.lineWidth, cv.LineTypes.Link4)
            Dim mean = task.pointCloud(rect).Mean(mask)

            If mean <> New cv.Scalar Then
                Dim mmX = GetMinMax(task.pcSplit(0)(rect), mask)
                Dim mmY = GetMinMax(task.pcSplit(1)(rect), mask)
                Dim len1 = mmX.minLoc.DistanceTo(mmX.maxLoc)
                Dim len2 = mmY.minLoc.DistanceTo(mmY.maxLoc)
                If len1 > len2 Then
                    lp.p1 = New cv.Point(mmX.minLoc.X + rect.X, mmX.minLoc.Y + rect.Y)
                    lp.p2 = New cv.Point(mmX.maxLoc.X + rect.X, mmX.maxLoc.Y + rect.Y)
                Else
                    lp.p1 = New cv.Point(mmY.minLoc.X + rect.X, mmY.minLoc.Y + rect.Y)
                    lp.p2 = New cv.Point(mmY.maxLoc.X + rect.X, mmY.maxLoc.Y + rect.Y)
                End If
                If lp.p1.DistanceTo(lp.p2) > 1 Then
                    DrawLine(dst3, lp.p1, lp.p2, cv.Scalar.Yellow)
                    p1List.Add(lp.p1)
                    p2List.Add(lp.p2)
                    z1List.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p1.Y, lp.p1.X))
                    z2List.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p2.Y, lp.p2.X))
                End If
            End If
        Next
    End Sub
End Class










Public Class XO_Line_Core : Inherits TaskParent
    Dim lines As New XO_Line_Core
    Public lpList As New List(Of lpData)
    Public lpRectMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Collect lines as always but don't update lines where there was no motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat
        Dim lastList As New List(Of lpData)(lpList)
        Dim histarray(lastList.Count - 1) As Single

        lpList.Clear()
        lpList.Add(New lpData) ' placeholder to allow us to build a map.
        If lastList.Count > 0 Then
            lpRectMap.SetTo(0, Not task.motionMask)
            cv.Cv2.CalcHist({lpRectMap}, {0}, emptyMat, histogram, 1, {lastList.Count}, New cv.Rangef() {New cv.Rangef(0, lastList.Count)})
            Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

            For i = 1 To histarray.Count - 1
                If histarray(i) = 0 Then lpList.Add(lastList(i))
            Next
        End If

        lines.Run(src.Clone)
        ReDim histarray(lines.lpList.Count - 1)

        Dim tmp = lines.lpRectMap.Clone
        tmp.SetTo(0, Not task.motionMask)
        cv.Cv2.CalcHist({tmp}, {0}, emptyMat, histogram, 1, {lines.lpList.Count}, New cv.Rangef() {New cv.Rangef(0, lines.lpList.Count)})
        Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

        For i = 1 To histarray.Count - 1
            If histarray(i) > 0 Then lpList.Add(lines.lpList(i))
        Next

        dst2.SetTo(0)
        lpRectMap.SetTo(0)
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            lpRectMap.Line(lp.p1, lp.p2, i, task.lineWidth, task.lineType)
        Next

        If task.heartBeat Then
            labels(2) = CStr(lines.lpList.Count) + " lines found in XO_Line_RawSorted in the current image with " +
                            CStr(lpList.Count) + " after filtering with the motion mask."
        End If
    End Sub
End Class





Public Class XO_Line_Basics : Inherits TaskParent
    Public lpRectMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public lpList As New List(Of lpData)
    Dim lineCore As New XO_Line_Core
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Collect lines across frames using the motion mask."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lineCore.Run(src)

        lpRectMap.SetTo(0)
        dst2 = src
        dst3.SetTo(0)
        dst2.SetTo(cv.Scalar.White, lineCore.dst2)
        For Each lp In lineCore.lpList
            lpRectMap.Line(lp.p1, lp.p2, lp.index, task.lineWidth + 1, cv.LineTypes.Link8)
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next

        lpList = New List(Of lpData)(lineCore.lpList)
        task.lines.lpList = New List(Of lpData)(lineCore.lpList)
        labels(2) = lineCore.labels(2)
    End Sub
End Class








Public Class XO_BackProject_LineSide : Inherits TaskParent
    Dim line As New XO_Line_ViewSide
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Backproject the lines found in the side view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        line.Run(src)

        dst2.SetTo(0)
        Dim w = task.lineWidth + 5
        lpList.Clear()
        For Each lp In task.lines.lpList
            If Math.Abs(lp.slope) < 0.1 Then
                lp = findEdgePoints(lp)
                dst2.Line(lp.p1, lp.p2, 255, w, task.lineType)
                lpList.Add(lp)
            End If
        Next

        Dim histogram = line.autoY.histogram
        histogram.SetTo(0, Not dst2)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, histogram, dst1, task.rangesSide)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        dst3 = src
        dst3.SetTo(white, dst1)
    End Sub
End Class






Public Class XO_OpAuto_FloorCeiling : Inherits TaskParent
    Public bpLine As New XO_BackProject_LineSide
    Public yList As New List(Of Single)
    Public floorY As Single
    Public ceilingY As Single
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Automatically find the Y values that best describes the floor and ceiling (if present)"
    End Sub
    Private Sub rebuildMask(maskLabel As String, min As Single, max As Single)
        Dim mask = task.pcSplit(1).InRange(min, max).ConvertScaleAbs

        Dim mean As cv.Scalar, stdev As cv.Scalar
        cv.Cv2.MeanStdDev(task.pointCloud, mean, stdev, mask)

        strOut += "The " + maskLabel + " mask has Y mean and stdev are:" + vbCrLf
        strOut += maskLabel + " Y Mean = " + Format(mean(1), fmt3) + vbCrLf
        strOut += maskLabel + " Y Stdev = " + Format(stdev(1), fmt3) + vbCrLf + vbCrLf

        If Math.Abs(mean(1)) > task.yRange / 4 Then dst1 = mask Or dst1
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pad As Single = 0.05 ' pad the estimate by X cm's

        dst2 = src.Clone
        bpLine.Run(src)

        If bpLine.lpList.Count > 0 Then
            strOut = "Y range = " + Format(task.yRange, fmt3) + vbCrLf + vbCrLf
            If task.heartBeat Then yList.Clear()
            If task.heartBeat Then dst1.SetTo(0)
            Dim h = dst2.Height / 2
            For Each lp In bpLine.lpList
                Dim nextY = task.yRange * (lp.p1.Y - h) / h
                If Math.Abs(nextY) > task.yRange / 4 Then yList.Add(nextY)
            Next

            If yList.Count > 0 Then
                If yList.Max > 0 Then rebuildMask("floor", yList.Max - pad, task.yRange)
                If yList.Min < 0 Then rebuildMask("ceiling", -task.yRange, yList.Min + pad)
            End If

            dst2.SetTo(white, dst1)
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class XO_Hough_Sudoku1 : Inherits TaskParent
    Dim lines As New XO_Line_RawSorted
    Public Sub New()
        desc = "FastLineDetect version for finding lines in the Sudoku input."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = cv.Cv2.ImRead(task.HomeDir + "opencv/Samples/Data/sudoku.png").Resize(dst2.Size)
        lines.Run(dst3.Clone)
        dst2 = lines.dst2
        labels(2) = lines.labels(2)
        For Each lp In lines.lpList
            lp = findEdgePoints(lp)
            dst3.Line(lp.p1, lp.p2, cv.Scalar.Red, task.lineWidth, task.lineType)
        Next
    End Sub
End Class




Public Class XO_Line_InterceptsUI : Inherits TaskParent
    Dim lines As New XO_Line_Intercepts
    Dim p2 As cv.Point
    Dim redRadio As System.Windows.Forms.RadioButton
    Dim greenRadio As System.Windows.Forms.RadioButton
    Dim yellowRadio As System.Windows.Forms.RadioButton
    Dim blueRadio As System.Windows.Forms.RadioButton
    Public Sub New()
        redRadio = OptionParent.findRadio("Show Top intercepts")
        greenRadio = OptionParent.findRadio("Show Bottom intercepts")
        yellowRadio = OptionParent.findRadio("Show Right intercepts")
        blueRadio = OptionParent.findRadio("Show Left intercepts")
        labels(2) = "Use mouse in right image to highlight lines"
        desc = "An alternative way to highlight line segments with common slope"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src)
        dst3.SetTo(0)

        Dim red = New cv.Scalar(0, 0, 255)
        Dim green = New cv.Scalar(1, 128, 0)
        Dim yellow = New cv.Scalar(2, 255, 255)
        Dim blue = New cv.Scalar(254, 0, 0)

        Dim center = New cv.Point(dst3.Width / 2, dst3.Height / 2)
        dst3.Line(New cv.Point(0, 0), center, blue, task.lineWidth, cv.LineTypes.Link4)
        dst3.Line(New cv.Point(dst2.Width, 0), center, red, task.lineWidth, cv.LineTypes.Link4)
        dst3.Line(New cv.Point(0, dst2.Height), center, blue, task.lineWidth, cv.LineTypes.Link4)
        dst3.Line(New cv.Point(dst2.Width, dst2.Height), center, yellow, task.lineWidth, cv.LineTypes.Link4)

        Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, cv.Scalar.All(0))
        Dim pt = New cv.Point(center.X, center.Y - 30)
        cv.Cv2.FloodFill(dst3, mask, pt, red, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X, center.Y + 30)
        cv.Cv2.FloodFill(dst3, mask, pt, green, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X - 30, center.Y)
        cv.Cv2.FloodFill(dst3, mask, pt, blue, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))

        pt = New cv.Point(center.X + 30, center.Y)
        cv.Cv2.FloodFill(dst3, mask, pt, yellow, New cv.Rect, 1, 1, cv.FloodFillFlags.FixedRange Or (255 << 8))
        Dim color = dst3.Get(Of cv.Vec3b)(task.mouseMovePoint.Y, task.mouseMovePoint.X)

        Dim p1 = task.mouseMovePoint
        If p1.X = center.X Then
            If p1.Y <= center.Y Then p2 = New cv.Point(dst3.Width / 2, 0) Else p2 = New cv.Point(dst3.Width, dst3.Height)
        Else
            Dim m = (center.Y - p1.Y) / (center.X - p1.X)
            Dim b = p1.Y - p1.X * m

            If color(0) = 0 Then p2 = New cv.Point(-b / m, 0) ' red zone
            If color(0) = 1 Then p2 = New cv.Point((dst3.Height - b) / m, dst3.Height) ' green
            If color(0) = 2 Then p2 = New cv.Point(dst3.Width, dst3.Width * m + b) ' yellow
            If color(0) = 254 Then p2 = New cv.Point(0, b) ' blue
            DrawLine(dst3, center, p2, cv.Scalar.Black)
        End If
        DrawCircle(dst3, center, task.DotSize, white)
        If color(0) = 0 Then redRadio.Checked = True
        If color(0) = 1 Then greenRadio.Checked = True
        If color(0) = 2 Then yellowRadio.Checked = True
        If color(0) = 254 Then blueRadio.Checked = True

        For Each inter In lines.intercept
            Select Case lines.options.selectedIntercept
                Case 0
                    dst3.Line(New cv.Point(inter.Key, 0), New cv.Point(inter.Key, 10), white,
                             task.lineWidth)
                Case 1
                    dst3.Line(New cv.Point(inter.Key, dst3.Height), New cv.Point(inter.Key, dst3.Height - 10),
                             white, task.lineWidth)
                Case 2
                    dst3.Line(New cv.Point(0, inter.Key), New cv.Point(10, inter.Key), white,
                             task.lineWidth)
                Case 3
                    dst3.Line(New cv.Point(dst3.Width, inter.Key), New cv.Point(dst3.Width - 10, inter.Key),
                             white, task.lineWidth)
            End Select
        Next
        dst2 = lines.dst2
    End Sub
End Class






Public Class XO_Diff_Heartbeat : Inherits TaskParent
    Public cumulativePixels As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "Unstable mask", "Pixel difference"}
        desc = "Diff an image with one from the last heartbeat."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            dst1 = task.gray.Clone
            dst2.SetTo(0)
        End If

        cv.Cv2.Absdiff(task.gray, dst1, dst3)
        cumulativePixels = dst3.CountNonZero
        dst2 = dst2 Or dst3.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class







Public Class XO_FitLine_Hough3D : Inherits TaskParent
    Dim hlines As New Hough_Lines_MT
    Public Sub New()
        desc = "Use visual lines to find 3D lines.  This algorithm is NOT working."
        labels(3) = "White is featureless RGB, blue depth shadow"
    End Sub
    Public Sub houghShowLines3D(ByRef dst As cv.Mat, segment As cv.Line3D)
        Dim x As Double = segment.X1 * dst.Cols
        Dim y As Double = segment.Y1 * dst.Rows
        Dim m As Double
        If segment.Vx < 0.001 Then m = 0 Else m = segment.Vy / segment.Vx ' vertical slope a no-no.
        Dim b As Double = y - m * x
        Dim pt1 As cv.Point = New cv.Point(x, y)
        Dim pt2 As cv.Point
        If m = 0 Then pt2 = New cv.Point(x, dst.Rows) Else pt2 = New cv.Point((dst.Rows - b) / m, dst.Rows)
        dst.Line(pt1, pt2, cv.Scalar.Red, task.lineWidth + 2, task.lineType, 0)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If Not task.heartBeat Then Exit Sub
        hlines.Run(src)
        dst3 = hlines.dst3
        Dim mask = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst3 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        src.CopyTo(dst2)

        Dim lines As New List(Of cv.Line3D)
        Dim nullLine = New cv.Line3D(0, 0, 0, 0, 0, 0)
        Parallel.ForEach(task.gridRects,
        Sub(roi)
            Dim depth = task.pcSplit(2)(roi)
            Dim fMask = mask(roi)
            Dim points As New List(Of cv.Point3f)
            Dim rows = src.Rows, cols = src.Cols
            For y = 0 To roi.Height - 1
                For x = 0 To roi.Width - 1
                    If fMask.Get(Of Byte)(y, x) > 0 Then
                        Dim d = depth.Get(Of Single)(y, x)
                        If d > 0 And d < 10000 Then
                            points.Add(New cv.Point3f(x / rows, y / cols, d / 10000))
                        End If
                    End If
                Next
            Next
            Dim line = nullLine
            If points.Count = 0 Then
                ' save the average color for this roi
                Dim mean = task.depthRGB(roi).Mean()
                mean(0) = 255 - mean(0)
                dst3.Rectangle(roi, mean)
            Else
                line = cv.Cv2.FitLine(points.ToArray, cv.DistanceTypes.L2, 0, 0, 0.01)
            End If
            SyncLock lines
                lines.Add(line)
            End SyncLock
        End Sub)
        ' putting this in the parallel for above causes a memory leak - could not find it...
        For i = 0 To task.gridRects.Count - 1
            houghShowLines3D(dst2(task.gridRects(i)), lines.ElementAt(i))
        Next
    End Sub
End Class



Public Class XO_Brick_Basics : Inherits TaskParent
    Public options As New Options_GridCells
    Public thresholdRangeZ As Single
    Public instantUpdate As Boolean = True
    Dim lastCorrelation() As Single
    Public quad As New XO_Quad_Basics
    Dim LRMeanSub As New MeanSubtraction_LeftRight
    Public Sub New()
        task.rgbLeftAligned = If(task.cameraName.StartsWith("StereoLabs") Or task.cameraName.StartsWith("Orbbec"), True, False)
        desc = "Create the grid of bricks that reduce depth volatility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If task.optionsChanged Then
            ReDim lastCorrelation(task.gridRects.Count - 1)
        End If

        LRMeanSub.Run(src)

        Dim stdev As cv.Scalar, mean As cv.Scalar
        Dim correlationMat As New cv.Mat

        task.bricks.brickList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim brick As New brickData
            brick.rect = task.gridRects(i)
            brick.age = task.motionBasics.cellAge(i)
            brick.rect = brick.rect
            brick.lRect = brick.rect ' for some cameras the color image and the left image are the same but not all, i.e. Intel Realsense.
            brick.center = New cv.Point(brick.rect.X + brick.rect.Width / 2, brick.rect.Y + brick.rect.Height / 2)
            If task.depthMask(brick.rect).CountNonZero Then
                cv.Cv2.MeanStdDev(task.pcSplit(2)(brick.rect), mean, stdev, task.depthMask(brick.rect))
                brick.depth = mean(0)
            End If

            If brick.depth = 0 Then
                brick.correlation = 0
                brick.rRect = emptyRect
            Else
                brick.mm = GetMinMax(task.pcSplit(2)(brick.rect), task.depthMask(brick.rect))
                If task.rgbLeftAligned Then
                    brick.lRect = brick.rect
                    brick.rRect = brick.lRect
                    brick.rRect.X -= task.calibData.baseline * task.calibData.rgbIntrinsics.fx / brick.depth
                    brick.rRect = ValidateRect(brick.rRect)
                    cv.Cv2.MatchTemplate(LRMeanSub.dst2(brick.lRect), LRMeanSub.dst3(brick.rRect), correlationMat,
                                                     cv.TemplateMatchModes.CCoeffNormed)

                    brick.correlation = correlationMat.Get(Of Single)(0, 0)
                Else
                    Dim irPt = Intrinsics_Basics.translate_LeftToRight(task.pointCloud.Get(Of cv.Point3f)(brick.rect.Y, brick.rect.X))
                    If irPt.X < 0 Or (irPt.X = 0 And irPt.Y = 0 And i > 0) Or (irPt.X >= dst2.Width Or irPt.Y >= dst2.Height) Then
                        brick.depth = 0 ' off the grid.
                        brick.lRect = emptyRect
                        brick.rRect = emptyRect
                    Else
                        brick.lRect = New cv.Rect(irPt.X, irPt.Y, brick.rect.Width, brick.rect.Height)
                        brick.lRect = ValidateRect(brick.lRect)

                        brick.rRect = brick.lRect
                        brick.rRect.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / brick.depth
                        brick.rRect = ValidateRect(brick.rRect)
                        cv.Cv2.MatchTemplate(LRMeanSub.dst2(brick.lRect), LRMeanSub.dst3(brick.rRect), correlationMat,
                                                      cv.TemplateMatchModes.CCoeffNormed)

                        brick.correlation = correlationMat.Get(Of Single)(0, 0)
                    End If
                End If
            End If

            lastCorrelation(i) = brick.correlation
            brick.index = task.bricks.brickList.Count
            task.grid.gridMap(brick.rect).SetTo(i)
            task.bricks.brickList.Add(brick)
        Next

        quad.Run(src)

        If task.heartBeat Then labels(2) = CStr(task.bricks.brickList.Count) + " bricks have the useful depth values."
    End Sub
End Class




Public Class XO_Quad_Basics : Inherits TaskParent
    Public Sub New()
        task.needBricks = True
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        desc = "Create a quad representation of the redCloud data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim shift As cv.Point3f
        If task.ogl IsNot Nothing Then
            Dim ptM = task.ogl.options4.moveAmount
            shift = New cv.Point3f(ptM(0), ptM(1), ptM(2))
        End If

        task.grid.gridMap.SetTo(0)
        dst2.SetTo(0)
        For i = 0 To task.bricks.brickList.Count - 1
            Dim brick = task.bricks.brickList(i)
            task.grid.gridMap(brick.rect).SetTo(i)
            If brick.depth > 0 Then
                brick.corners.Clear()

                Dim p0 = getWorldCoordinates(brick.rect.TopLeft, brick.depth)
                Dim p1 = getWorldCoordinates(brick.rect.BottomRight, brick.depth)

                ' clockwise around starting in upper left.
                brick.corners.Add(New cv.Point3f(p0.X + shift.X, p0.Y + shift.Y, brick.depth))
                brick.corners.Add(New cv.Point3f(p1.X + shift.X, p0.Y + shift.Y, brick.depth))
                brick.corners.Add(New cv.Point3f(p1.X + shift.X, p1.Y + shift.Y, brick.depth))
                brick.corners.Add(New cv.Point3f(p0.X + shift.X, p1.Y + shift.Y, brick.depth))
            End If
        Next
    End Sub
End Class







Public Class XO_PointCloud_Infinities : Inherits TaskParent
    Public Sub New()
        desc = "Find out if pointcloud has an nan's or inf's.  StereoLabs had some... look for PatchNans."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim infTotal(2) As Integer
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim vec = task.pointCloud.Get(Of cv.Vec3f)(y, x)
                If Single.IsInfinity(vec(0)) Then infTotal(0) += 1
                If Single.IsInfinity(vec(1)) Then infTotal(1) += 1
                If Single.IsInfinity(vec(2)) Then infTotal(2) += 1
            Next
        Next
        SetTrueText("infinities: X " + CStr(infTotal(0)) + ", Y = " + CStr(infTotal(1)) + " Z = " +
                    CStr(infTotal(2)))
    End Sub
End Class





Public Class XO_PointCloud_VerticalHorizontal : Inherits TaskParent
    Public actualCount As Integer

    Public allPointsH As New List(Of cv.Point3f)
    Public allPointsV As New List(Of cv.Point3f)

    Public hList As New List(Of List(Of cv.Point3f))
    Public xyHList As New List(Of List(Of cv.Point))

    Public vList As New List(Of List(Of cv.Point3f))
    Public xyVList As New List(Of List(Of cv.Point))
    Dim options As New Options_PointCloud()
    Public Sub New()
        setPointCloudGrid()
        desc = "Reduce the point cloud to a manageable number points in 3D"
    End Sub
    Public Function findHorizontalPoints(ByRef xyList As List(Of List(Of cv.Point))) As List(Of List(Of cv.Point3f))
        Dim ptlist As New List(Of List(Of cv.Point3f))
        Dim lastVec = New cv.Point3f
        For y = 0 To task.pointCloud.Height - 1 Step task.gridRects(0).Height - 1
            Dim vecList As New List(Of cv.Point3f)
            Dim xyVec As New List(Of cv.Point)
            For x = 0 To task.pointCloud.Width - 1 Step task.gridRects(0).Width - 1
                Dim vec = task.pointCloud.Get(Of cv.Point3f)(y, x)
                Dim jumpZ As Boolean = False
                If vec.Z > 0 Then
                    If (Math.Abs(lastVec.Z - vec.Z) < options.deltaThreshold And lastVec.X < vec.X) Or lastVec.Z = 0 Then
                        actualCount += 1
                        DrawCircle(dst2, New cv.Point(x, y), task.DotSize, white)
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
        For x = 0 To task.pointCloud.Width - 1 Step task.gridRects(0).Width - 1
            Dim vecList As New List(Of cv.Point3f)
            Dim xyVec As New List(Of cv.Point)
            For y = 0 To task.pointCloud.Height - 1 Step task.gridRects(0).Height - 1
                Dim vec = task.pointCloud.Get(Of cv.Point3f)(y, x)
                Dim jumpZ As Boolean = False
                If vec.Z > 0 Then
                    If (Math.Abs(lastVec.Z - vec.Z) < options.deltaThreshold And lastVec.Y < vec.Y) Or lastVec.Z = 0 Then
                        actualCount += 1
                        DrawCircle(dst2, New cv.Point(x, y), task.DotSize, white)
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

    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

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







Public Class XO_Line3D_CandidatesFirstLast : Inherits TaskParent
    Public pts As New XO_PointCloud_VerticalHorizontal
    Public pcLines As New List(Of cv.Point3f)
    Public pcLinesMat As cv.Mat
    Public actualCount As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Get a list of points from PointCloud_Basics.  Identify first and last as the line " +
               "in the sequence"
    End Sub
    Private Sub addLines(nextList As List(Of List(Of cv.Point3f)), xyList As List(Of List(Of cv.Point)))
        Dim white32 As New cv.Point3f(0, 1, 1)
        For i = 0 To nextList.Count - 1
            pcLines.Add(white32)
            pcLines.Add(nextList(i)(0))
            pcLines.Add(nextList(i)(nextList(i).Count - 1))
        Next

        For Each ptlist In xyList
            Dim p1 = ptlist(0)
            Dim p2 = ptlist(ptlist.Count - 1)
            DrawLine(dst2, p1, p2, white)
        Next
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pts.Run(src)
        dst2 = pts.dst2

        pcLines.Clear()
        addLines(pts.hList, pts.xyHList)
        addLines(pts.vList, pts.xyVList)

        pcLinesMat = cv.Mat.FromPixelData(pcLines.Count, 1, cv.MatType.CV_32FC3, pcLines.ToArray)
        labels(2) = "Point series found = " + CStr(pts.hList.Count + pts.vList.Count)
    End Sub
End Class







Public Class XO_PointCloud_PCPointsPlane : Inherits TaskParent
    Dim pcBasics As New XO_Line3D_CandidatesFirstLast
    Public pcPoints As New List(Of cv.Point3f)
    Public xyList As New List(Of cv.Point)
    Dim white32 = New cv.Point3f(1, 1, 1)
    Public Sub New()
        setPointCloudGrid()
        desc = "Find planes using a reduced set of 3D points and the intersection of vertical and horizontal lines through those points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pcBasics.Run(src)

        pcPoints.Clear()
        ' points in both the vertical and horizontal lists are likely to designate a plane
        For Each pt In pcBasics.pts.allPointsH
            If pcBasics.pts.allPointsV.Contains(pt) Then
                pcPoints.Add(white32)
                pcPoints.Add(pt)
            End If
        Next

        labels(2) = "Point series found = " + CStr(pcPoints.Count / 2)
    End Sub
End Class






Public Class XO_OpenGL_PClinesFirstLast : Inherits TaskParent
    Dim lines As New XO_Line3D_CandidatesFirstLast
    Public Sub New()
        task.ogl.oglFunction = oCase.pcLines
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        desc = "Draw the 3D lines found from the PCpoints"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        If lines.pcLinesMat.Rows = 0 Then task.ogl.dataInput = New cv.Mat Else task.ogl.dataInput = lines.pcLinesMat
        'task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(New cv.Mat)
        labels(2) = "OpenGL_PClines found " + CStr(lines.pcLinesMat.Rows / 3) + " lines"
    End Sub
End Class







Public Class XO_OpenGL_PCLineCandidates : Inherits TaskParent
    Dim pts As New XO_PointCloud_VerticalHorizontal
    Public Sub New()
        task.ogl.oglFunction = oCase.pcPointsAlone
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        desc = "Display the output of the PointCloud_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pts.Run(src)
        dst2 = pts.dst2

        task.ogl.dataInput = cv.Mat.FromPixelData(pts.allPointsH.Count, 1, cv.MatType.CV_32FC3, pts.allPointsH.ToArray)
        task.ogl.Run(New cv.Mat)
        labels(2) = "Point cloud points found = " + CStr(pts.actualCount / 2)
    End Sub
End Class








Public Class XO_PointCloud_NeighborV : Inherits TaskParent
    Dim options As New Options_Neighbors
    Public Sub New()
        desc = "Show where vertical neighbor depth values are within task.depthDiffMeters"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim tmp32f = New cv.Mat(dst2.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
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








Public Class XO_PointCloud_Visualize : Inherits TaskParent
    Public Sub New()
        labels = {"", "", "Pointcloud visualized", ""}
        desc = "Display the pointcloud as a BGR image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pcSplit = {task.pcSplit(0).ConvertScaleAbs(255), task.pcSplit(1).ConvertScaleAbs(255), task.pcSplit(2).ConvertScaleAbs(255)}
        cv.Cv2.Merge(pcSplit, dst2)
    End Sub
End Class







Public Class XO_PointCloud_Raw_CPP : Inherits TaskParent
    Dim depthBytes() As Byte
    Public Sub New()
        labels(2) = "Top View"
        labels(3) = "Side View"
        desc = "Project the depth data onto a top view And side view."
        cPtr = SimpleProjectionOpen()
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.firstPass Then ReDim depthBytes(task.pcSplit(2).Total * task.pcSplit(2).ElemSize - 1)

        Marshal.Copy(task.pcSplit(2).Data, depthBytes, 0, depthBytes.Length)
        Dim handleDepth = GCHandle.Alloc(depthBytes, GCHandleType.Pinned)

        Dim imagePtr = SimpleProjectionRun(cPtr, handleDepth.AddrOfPinnedObject, 0, task.MaxZmeters, task.pcSplit(2).Height, task.pcSplit(2).Width)

        dst2 = cv.Mat.FromPixelData(task.pcSplit(2).Rows, task.pcSplit(2).Cols, cv.MatType.CV_8U, imagePtr).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = cv.Mat.FromPixelData(task.pcSplit(2).Rows, task.pcSplit(2).Cols, cv.MatType.CV_8U, SimpleProjectionSide(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        handleDepth.Free()
        labels(2) = "Top View (looking down)"
        labels(3) = "Side View"
    End Sub
    Public Sub Close()
        SimpleProjectionClose(cPtr)
    End Sub
End Class





Public Class XO_PointCloud_Raw : Inherits TaskParent
    Public Sub New()
        labels(2) = "Top View"
        labels(3) = "Side View"
        desc = "Project the depth data onto a top view And side view - Using only VB code (too slow.)"
        cPtr = SimpleProjectionOpen()
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim range As Single = task.MaxZmeters

        ' this VB.Net version is much slower than the optimized C++ version below.
        dst2 = src.EmptyClone.SetTo(white)
        dst3 = dst2.Clone()
        Dim black = New cv.Vec3b(0, 0, 0)
        Parallel.ForEach(task.gridRects,
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







Public Class XO_PointCloud_PCpointsMask : Inherits TaskParent
    Public pcPoints As cv.Mat
    Public actualCount As Integer
    Public Sub New()
        setPointCloudGrid()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Reduce the point cloud to a manageable number points in 3D representing the averages of X, Y, and Z in that roi."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then pcPoints = New cv.Mat(task.bricksPerCol, task.bricksPerRow, cv.MatType.CV_32FC3, cv.Scalar.All(0))

        dst2.SetTo(0)
        actualCount = 0
        Dim lastMeanZ As Single
        For y = 0 To task.bricksPerCol - 1
            For x = 0 To task.bricksPerRow - 1
                Dim roi = task.gridRects(y * task.bricksPerRow + x)
                Dim mean = task.pointCloud(roi).Mean(task.depthMask(roi))
                If Single.IsNaN(mean(0)) Then Continue For
                If Single.IsNaN(mean(1)) Then Continue For
                If Single.IsInfinity(mean(2)) Then Continue For
                Dim depthPresent = task.depthMask(roi).CountNonZero > roi.Width * roi.Height / 2
                If (depthPresent And mean(2) > 0 And Math.Abs(lastMeanZ - mean(2)) < 0.2 And
                    mean(2) < task.MaxZmeters) Or (lastMeanZ = 0 And mean(2) > 0) Then

                    pcPoints.Set(Of cv.Point3f)(y, x, New cv.Point3f(mean(0), mean(1), mean(2)))
                    actualCount += 1
                    DrawCircle(dst2, New cv.Point(roi.X, roi.Y), task.DotSize * Math.Max(mean(2), 1), white)
                End If
                lastMeanZ = mean(2)
            Next
        Next
        labels(2) = "PointCloud Point Points found = " + CStr(actualCount)
    End Sub
End Class







Public Class XO_PointCloud_PCPoints : Inherits TaskParent
    Public pcPoints As New List(Of cv.Point3f)
    Public Sub New()
        setPointCloudGrid()
        desc = "Reduce the point cloud to a manageable number points in 3D using the mean value"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim rw = task.gridRects(0).Width / 2, rh = task.gridRects(0).Height / 2
        Dim red32 = New cv.Point3f(0, 0, 1), blue32 = New cv.Point3f(1, 0, 0), white32 = New cv.Point3f(1, 1, 1)
        Dim red = cv.Scalar.Red, blue = cv.Scalar.Blue

        pcPoints.Clear()
        dst2 = src
        For Each roi In task.gridRects
            Dim pt = New cv.Point(roi.X + rw, roi.Y + rh)
            Dim mean = task.pointCloud(roi).Mean(task.depthMask(roi))

            If mean(2) > 0 Then
                pcPoints.Add(Choose(pt.Y Mod 3 + 1, red32, blue32, white32))
                pcPoints.Add(New cv.Point3f(mean(0), mean(1), mean(2)))
                DrawCircle(dst2, pt, task.DotSize, Choose(CInt(pt.Y) Mod 3 + 1, red, blue, cv.Scalar.White))
            End If
        Next
        labels(2) = "PointCloud Point Points found = " + CStr(pcPoints.Count / 2)
    End Sub
End Class









Public Class XO_OpenGL_PCpoints : Inherits TaskParent
    Dim pts As New XO_PointCloud_PCPoints
    Public Sub New()
        task.ogl.oglFunction = oCase.pcPoints
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        desc = "Display the output of the PointCloud_Points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pts.Run(src)
        dst2 = pts.dst2

        task.ogl.dataInput = cv.Mat.FromPixelData(pts.pcPoints.Count, 1, cv.MatType.CV_32FC3, pts.pcPoints.ToArray)
        task.ogl.Run(New cv.Mat)
        labels(2) = "Point cloud points found = " + CStr(pts.pcPoints.Count / 2)
    End Sub
End Class






Public Class XO_Region_Palette : Inherits TaskParent
    Dim hRects As New XO_Region_RectsH
    Dim vRects As New XO_Region_RectsV
    Dim mats As New Mat_4Click
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Assign an index to each of vertical and horizontal rects in Region_Rects"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hRects.Run(src)

        Dim indexH As Integer
        dst1.SetTo(0)
        For Each r In hRects.hRects
            If r.Y = 0 Then
                indexH += 1
                dst1(r).SetTo(indexH)
            Else
                Dim foundLast As Boolean
                For x = r.X To r.X + r.Width - 1
                    Dim lastIndex = dst1.Get(Of Byte)(r.Y - 1, x)
                    If lastIndex <> 0 Then
                        dst1(r).SetTo(lastIndex)
                        foundLast = True
                        Exit For
                    End If
                Next
                If foundLast = False Then
                    indexH += 1
                    dst1(r).SetTo(indexH)
                End If
            End If
        Next
        mats.mat(0) = ShowPalette(dst1)

        mats.mat(1) = ShowAddweighted(src, mats.mat(0), labels(3))

        vRects.Run(src)
        Dim indexV As Integer
        dst1.SetTo(0)
        For Each r In vRects.vRects
            If r.X = 0 Then
                indexV += 1
                dst1(r).SetTo(indexV)
            Else
                Dim foundLast As Boolean
                For y = r.Y To r.Y + r.Height - 1
                    Dim lastIndex = dst1.Get(Of Byte)(y, r.X - 1)
                    If lastIndex <> 0 Then
                        dst1(r).SetTo(lastIndex)
                        foundLast = True
                        Exit For
                    End If
                Next
                If foundLast = False Then
                    indexV += 1
                    dst1(r).SetTo(indexV)
                End If
            End If
        Next
        mats.mat(2) = ShowPalette(dst1)

        mats.mat(3) = ShowAddweighted(src, mats.mat(2), labels(3))
        If task.heartBeat Then labels(2) = CStr(indexV + indexH) + " regions were found that were connected in depth."

        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class






Public Class XO_Sort_FeatureLess : Inherits TaskParent
    Public connect As New XO_Region_Palette
    Public sort As New Sort_Basics
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        task.gOptions.setHistogramBins(255)
        task.gOptions.GridSlider.Value = 8
        desc = "Sort all the featureless grayscale pixels."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)
        dst2 = connect.dst3
        labels(2) = connect.labels(2)
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1.SetTo(0, Not connect.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary))

        sort.Run(dst1)

        plot.Run(sort.dst2)
        dst3 = plot.dst2
    End Sub
End Class







Public Class XO_Region_RectsH : Inherits TaskParent
    Public hRects As New List(Of cv.Rect)
    Dim connect As New Region_Core
    Public Sub New()
        task.needBricks = True
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Connect bricks with similar depth - horizontally scanning."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)

        dst2.SetTo(0)
        dst3.SetTo(0)
        hRects.Clear()
        Dim index As Integer
        For Each tup In connect.hTuples
            If tup.Item1 = tup.Item2 Then Continue For
            Dim brick1 = task.bricks.brickList(tup.Item1)
            Dim brick2 = task.bricks.brickList(tup.Item2)

            Dim w = brick2.rect.BottomRight.X - brick1.rect.X
            Dim h = brick1.rect.Height

            Dim r = New cv.Rect(brick1.rect.X + 1, brick1.rect.Y, w - 1, h)

            hRects.Add(r)
            dst2(r).SetTo(255)

            index += 1
            dst3(r).SetTo(task.scalarColors(index Mod 256))
        Next
    End Sub
End Class






Public Class XO_Region_RectsV : Inherits TaskParent
    Public vRects As New List(Of cv.Rect)
    Dim connect As New Region_Core
    Public Sub New()
        task.needBricks = True
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Connect bricks with similar depth - vertically scanning."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)

        dst2.SetTo(0)
        dst3.SetTo(0)
        vRects.Clear()
        Dim index As Integer
        For Each tup In connect.vTuples
            If tup.Item1 = tup.Item2 Then Continue For
            Dim brick1 = task.bricks.brickList(tup.Item1)
            Dim brick2 = task.bricks.brickList(tup.Item2)

            Dim w = brick1.rect.Width
            Dim h = brick2.rect.BottomRight.Y - brick1.rect.Y

            Dim r = New cv.Rect(brick1.rect.X, brick1.rect.Y + 1, w, h - 1)
            vRects.Add(r)
            dst2(r).SetTo(255)

            index += 1
            dst3(r).SetTo(task.scalarColors(index Mod 256))
        Next
    End Sub
End Class






Public Class XO_Region_Rects : Inherits TaskParent
    Dim hConn As New XO_Region_RectsH
    Dim vConn As New XO_Region_RectsV
    Public Sub New()
        desc = "Isolate the connected depth bricks both vertically and horizontally."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hConn.Run(src)
        vConn.Run(src)

        dst2 = (Not vConn.dst2).ToMat Or (Not hConn.dst2).ToMat

        dst3 = src
        dst3.SetTo(0, dst2)
    End Sub
End Class






Public Class XO_Region_RedColor : Inherits TaskParent
    Dim connect As New Region_Contours
    Public Sub New()
        desc = "Color each redCell with the color of the nearest brick region."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)

        dst3 = runRedC(src, labels(3))
        For Each rc In task.redC.rcList
            Dim index = connect.dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            dst2(rc.rect).SetTo(task.scalarColors(index), rc.mask)
        Next
    End Sub
End Class





Public Class XO_Region_Gaps : Inherits TaskParent
    Dim connect As New Region_Core
    Public Sub New()
        task.needBricks = True
        labels(2) = "bricks with single cells removed for both vertical and horizontal connected cells."
        labels(3) = "Vertical cells with single cells removed."
        desc = "Use the horizontal/vertical connected cells to find gaps in depth and the like featureless regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)
        dst2 = connect.dst2
        dst3 = connect.dst3

        For Each tup In connect.hTuples
            If tup.Item2 - tup.Item1 = 0 Then
                Dim brick = task.bricks.brickList(tup.Item1)
                dst2(brick.rect).SetTo(0)
            End If
        Next

        For Each tup In connect.vTuples
            Dim brick1 = task.bricks.brickList(tup.Item1)
            Dim brick2 = task.bricks.brickList(tup.Item2)
            If brick2.rect.Y - brick1.rect.Y = 0 Then
                dst2(brick1.rect).SetTo(0)
                dst3(brick1.rect).SetTo(0)
            End If
        Next
    End Sub
End Class






Public Class XO_Brick_FeatureGaps : Inherits TaskParent
    Dim feat As New Brick_Features
    Dim gaps As New XO_Region_Gaps
    Public Sub New()
        labels(2) = "The output of Brick_Gaps overlaid with the output of the Brick_Features"
        desc = "Overlay the features on the image of the gaps"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(src)
        gaps.Run(src)
        dst2 = ShowAddweighted(feat.dst2, gaps.dst2, labels(3))
    End Sub
End Class




Public Class XO_FCSLine_Basics : Inherits TaskParent
    Dim delaunay As New Delaunay_Basics
    Public Sub New()
        task.needBricks = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Build a feature coordinate system (FCS) based on lines, not features."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lastMap = task.fpMap.Clone
        Dim lastCount = task.lines.lpList.Count

        dst2 = task.lines.dst2

        delaunay.inputPoints.Clear()

        For Each lp In task.lines.lpList
            Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
            delaunay.inputPoints.Add(center)
        Next

        delaunay.Run(src)

        task.fpMap.SetTo(0)
        dst1.SetTo(0)
        For i = 0 To delaunay.facetList.Count - 1
            Dim lp = task.lines.lpList(i)
            Dim facets = delaunay.facetList(i)

            DrawContour(dst1, facets, 255, task.lineWidth)
            DrawContour(task.fpMap, facets, i)
            Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
            Dim brick = task.bricks.brickList(task.grid.gridMap.Get(Of Integer)(center.Y, center.X))
            task.lines.lpList(i) = lp
        Next

        Dim index = task.fpMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
        task.lpD = task.lines.lpList(index)
        Dim facetsD = delaunay.facetList(task.lpD.index)
        DrawContour(dst2, facetsD, white, task.lineWidth)

        labels(2) = task.lines.labels(2)
        labels(3) = delaunay.labels(2)
    End Sub
End Class







Public Class XO_FCSLine_Vertical : Inherits TaskParent
    Dim verts As New XO_Line_TrigVertical
    Dim minRect As New LineRect_Basics
    Dim options As New Options_FCSLine
    Public Sub New()
        desc = "Find all verticle lines and combine them if they are 'close'."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        verts.Run(src)

        dst2.SetTo(0)
        dst3.SetTo(0)
        For i = 0 To verts.vertList.Count - 1
            Dim lp1 = verts.vertList(i)
            For j = i + 1 To verts.vertList.Count - 1
                Dim lp2 = verts.vertList(j)
                Dim center = New cv.Point(CInt((lp1.p1.X + lp1.p2.X) / 2), CInt((lp1.p1.Y + lp1.p2.Y) / 2))
                Dim lpPerp = lp1.perpendicularPoints(center)
                Dim intersectionPoint = Line_Intersection.IntersectTest(lp1, lpPerp)
                Dim distance = intersectionPoint.DistanceTo(center)
                If distance <= options.proximity Then
                    minRect.lpInput1 = lp1
                    minRect.lpInput2 = lp2
                    Dim rotatedRect1 = cv.Cv2.MinAreaRect({lp1.p1, lp1.p2})
                    Dim rotatedRect2 = cv.Cv2.MinAreaRect({lp2.p1, lp2.p2})
                    minRect.Run(src)
                    dst2.Line(lp1.p1, lp1.p2, task.highlight, task.lineWidth, task.lineType)
                    dst2.Line(lp2.p1, lp2.p2, task.highlight, task.lineWidth, task.lineType)
                    DrawRotatedOutline(minRect.rotatedRect, dst3, cv.Scalar.Yellow)
                End If
            Next
        Next
    End Sub
End Class








Public Class XO_FeatureLine_VerticalVerify : Inherits TaskParent
    Dim linesVH As New MatchLine_VH
    Public verify As New IMU_VerticalVerify
    Public Sub New()
        desc = "Select a line or group of lines and track the result"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        linesVH.Run(src)

        verify.brickCells = New List(Of gravityLine)(linesVH.brickCells)
        verify.Run(src)
        dst2 = verify.dst2
    End Sub
End Class







Public Class XO_FeatureLine_Finder3D : Inherits TaskParent
    Public lines2D As New List(Of cv.Point2f)
    Public lines3D As New List(Of cv.Point3f)
    Public sorted2DV As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public sortedVerticals As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public sortedHorizontals As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Dim options As New Options_LineFinder()
    Public Sub New()
        desc = "Find all the lines in the image and determine which are vertical and horizontal"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst3 = src.Clone

        lines2D.Clear()
        lines3D.Clear()
        sorted2DV.Clear()
        sortedVerticals.Clear()
        sortedHorizontals.Clear()

        dst2 = task.lines.dst2

        Dim raw2D As New List(Of lpData)
        Dim raw3D As New List(Of cv.Point3f)
        For Each lp In task.lines.lpList
            Dim pt1 As cv.Point3f, pt2 As cv.Point3f
            For j = 0 To 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                Dim rect = ValidateRect(New cv.Rect(pt.x - options.kSize, pt.y - options.kSize, options.kernelSize, options.kernelSize))
                Dim val = task.pointCloud(rect).Mean(task.depthMask(rect))
                If j = 0 Then pt1 = New cv.Point3f(val(0), val(1), val(2)) Else pt2 = New cv.Point3f(val(0), val(1), val(2))
            Next

            If pt1.Z > 0 And pt2.Z > 0 And pt1.Z < 4 And pt2.Z < 4 Then ' points more than X meters away are not accurate...
                raw2D.Add(lp)
                raw3D.Add(pt1)
                raw3D.Add(pt2)
            End If
        Next

        If raw3D.Count = 0 Then
            SetTrueText("No vertical or horizontal lines were found")
        Else
            Dim matLines3D As cv.Mat = (cv.Mat.FromPixelData(raw3D.Count, 3, cv.MatType.CV_32F, raw3D.ToArray)) * task.gMatrix

            For i = 0 To raw2D.Count - 2 Step 2
                Dim pt1 = matLines3D.Get(Of cv.Point3f)(i, 0)
                Dim pt2 = matLines3D.Get(Of cv.Point3f)(i + 1, 0)
                Dim len3D = distance3D(pt1, pt2)
                Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
                If Math.Abs(arcY - 90) < options.tolerance Then
                    DrawLine(dst3, raw2D(i).p1, raw2D(i).p2, cv.Scalar.Blue)
                    sortedVerticals.Add(len3D, lines3D.Count)
                    sorted2DV.Add(raw2D(i).p1.DistanceTo(raw2D(i).p2), lines2D.Count)
                    If pt1.Y > pt2.Y Then
                        lines3D.Add(pt1)
                        lines3D.Add(pt2)
                        lines2D.Add(raw2D(i).p1)
                        lines2D.Add(raw2D(i).p2)
                    Else
                        lines3D.Add(pt2)
                        lines3D.Add(pt1)
                        lines2D.Add(raw2D(i).p2)
                        lines2D.Add(raw2D(i).p1)
                    End If
                End If
                If Math.Abs(arcY) < options.tolerance Then
                    DrawLine(dst3, raw2D(i).p1, raw2D(i).p2, cv.Scalar.Yellow)
                    sortedHorizontals.Add(len3D, lines3D.Count)
                    If pt1.X < pt2.X Then
                        lines3D.Add(pt1)
                        lines3D.Add(pt2)
                        lines2D.Add(raw2D(i).p1)
                        lines2D.Add(raw2D(i).p2)
                    Else
                        lines3D.Add(pt2)
                        lines3D.Add(pt1)
                        lines2D.Add(raw2D(i).p2)
                        lines2D.Add(raw2D(i).p1)
                    End If
                End If
            Next
        End If
        labels(2) = "Starting with " + Format(task.lines.lpList.Count, "000") + " lines, there are " +
                                       Format(lines3D.Count / 2, "000") + " with depth data."
        labels(3) = "There were " + CStr(sortedVerticals.Count) + " vertical lines (blue) and " + CStr(sortedHorizontals.Count) + " horizontal lines (yellow)"
    End Sub
End Class






Public Class XO_Structured_FeatureLines : Inherits TaskParent
    Dim struct As New Structured_MultiSlice
    Public lines As New XO_FeatureLine_Finder3D
    Public Sub New()
        desc = "Find the lines in the Structured_MultiSlice algorithm output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        struct.Run(src)
        dst2 = struct.dst2

        lines.Run(struct.dst2)
        dst3 = src.Clone
        For i = 0 To lines.lines2D.Count - 1 Step 2
            Dim p1 = lines.lines2D(i), p2 = lines.lines2D(i + 1)
            dst3.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
    End Sub
End Class








Public Class XO_FeatureLine_Tutorial2 : Inherits TaskParent
    Dim options As New Options_LineFinder()
    Public Sub New()
        desc = "Find all the lines in the image and determine which are vertical and horizontal"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = task.lines.dst2

        Dim raw3D As New List(Of cv.Point3f)
        For Each lp In task.lines.lpList
            Dim pt1 As cv.Point3f, pt2 As cv.Point3f
            For j = 0 To 1
                Dim pt = Choose(j + 1, lp.p1, lp.p2)
                Dim rect = ValidateRect(New cv.Rect(pt.x - options.kSize, pt.y - options.kSize, options.kernelSize, options.kernelSize))
                Dim val = task.pointCloud(rect).Mean(task.depthMask(rect))
                If j = 0 Then pt1 = New cv.Point3f(val(0), val(1), val(2)) Else pt2 = New cv.Point3f(val(0), val(1), val(2))
            Next
            If pt1.Z > 0 And pt2.Z > 0 Then
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p1.Y, lp.p1.X))
                raw3D.Add(task.pointCloud.Get(Of cv.Point3f)(lp.p2.Y, lp.p2.X))
            End If
        Next

        If task.heartBeat Then labels(2) = "Starting with " + Format(task.lines.lpList.Count, "000") +
                               " lines, there are " + Format(raw3D.Count, "000") + " with depth data."
        If raw3D.Count = 0 Then
            SetTrueText("No vertical or horizontal lines were found")
        Else
            task.gMatrix = task.gmat.gMatrix
            Dim matLines3D = cv.Mat.FromPixelData(raw3D.Count, 3, cv.MatType.CV_32F, raw3D.ToArray) * task.gmat.gMatrix
        End If
    End Sub
End Class









Public Class XO_FeatureLine_LongestVerticalKNN : Inherits TaskParent
    Dim gLines As New Line_GCloud
    Dim longest As New XO_FeatureLine_Longest
    Public Sub New()
        labels(3) = "All vertical lines.  The numbers: index and Arc-Y for the longest X vertical lines."
        desc = "Find all the vertical lines and then track the longest one with a lightweight KNN."
    End Sub
    Private Function testLastPair(lastPair As lpData, brick As gravityLine) As Boolean
        Dim distance1 = lastPair.p1.DistanceTo(lastPair.p2)
        Dim p1 = brick.tc1.center
        Dim p2 = brick.tc2.center
        If distance1 < 0.75 * p1.DistanceTo(p2) Then Return True ' it the longest vertical * 0.75 > current lastPair, then use the longest vertical...
        Return False
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        gLines.Run(src)
        If gLines.sortedVerticals.Count = 0 Then
            SetTrueText("No vertical lines were present", 3)
            Exit Sub
        End If

        dst3 = src.Clone
        Dim index As Integer

        If testLastPair(longest.knn.lastPair, gLines.sortedVerticals.ElementAt(0).Value) Then longest.knn.lastPair = New lpData
        For Each brick In gLines.sortedVerticals.Values
            If index >= 10 Then Exit For

            Dim p1 = brick.tc1.center
            Dim p2 = brick.tc2.center
            If longest.knn.lastPair.compare(New lpData) Then longest.knn.lastPair = New lpData(p1, p2)
            Dim pt = New cv.Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2)
            SetTrueText(CStr(index) + vbCrLf + Format(brick.arcY, fmt1), pt, 3)
            index += 1

            DrawLine(dst3, p1, p2, task.highlight)
            longest.knn.trainInput.Add(p1)
            longest.knn.trainInput.Add(p2)
        Next

        longest.Run(src)
        dst2 = longest.dst2
    End Sub
End Class








Public Class XO_FeatureLine_LongestV_Tutorial1 : Inherits TaskParent
    Dim lines As New XO_FeatureLine_Finder3D
    Public Sub New()
        desc = "Use FeatureLine_Finder to find all the vertical lines and show the longest."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)

        If lines.sortedVerticals.Count = 0 Then
            SetTrueText("No vertical lines were found", 3)
            Exit Sub
        End If

        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim p1 = lines.lines2D(index)
        Dim p2 = lines.lines2D(index + 1)
        DrawLine(dst2, p1, p2, task.highlight)
        dst3.SetTo(0)
        DrawLine(dst3, p1, p2, task.highlight)
    End Sub
End Class






Public Class XO_FeatureLine_LongestV_Tutorial2 : Inherits TaskParent
    Dim lines As New XO_FeatureLine_Finder3D
    Dim knn As New KNN_N4Basics
    Public pt1 As New cv.Point3f
    Public pt2 As New cv.Point3f
    Dim lengthReject As Integer
    Public Sub New()
        desc = "Use FeatureLine_Finder to find all the vertical lines.  Use KNN_Basics4D to track each line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        lines.Run(src)
        dst1 = lines.dst3

        If lines.sortedVerticals.Count = 0 Then
            SetTrueText("No vertical lines were found", 3)
            Exit Sub
        End If

        Dim match3D As New List(Of cv.Point3f)
        knn.trainInput.Clear()
        For i = 0 To lines.sortedVerticals.Count - 1
            Dim sIndex = lines.sortedVerticals.ElementAt(i).Value
            Dim x1 = lines.lines2D(sIndex)
            Dim x2 = lines.lines2D(sIndex + 1)
            Dim vec = If(x1.Y < x2.Y, New cv.Vec4f(x1.X, x1.Y, x2.X, x2.Y), New cv.Vec4f(x2.X, x2.Y, x1.X, x1.Y))
            If knn.queries.Count = 0 Then knn.queries.Add(vec)
            knn.trainInput.Add(vec)
            match3D.Add(lines.lines3D(sIndex))
            match3D.Add(lines.lines3D(sIndex + 1))
        Next

        Dim saveVec = knn.queries(0)
        knn.Run(src)

        Dim index = knn.result(0, 0)
        Dim p1 = New cv.Point2f(knn.trainInput(index)(0), knn.trainInput(index)(1))
        Dim p2 = New cv.Point2f(knn.trainInput(index)(2), knn.trainInput(index)(3))
        pt1 = match3D(index * 2)
        pt2 = match3D(index * 2 + 1)
        DrawLine(dst2, p1, p2, task.highlight)
        dst3.SetTo(0)
        DrawLine(dst3, p1, p2, task.highlight)

        Static lastLength = lines.sorted2DV.ElementAt(0).Key
        Dim bestLength = lines.sorted2DV.ElementAt(0).Key
        knn.queries.Clear()
        If lastLength > 0.5 * bestLength Then
            knn.queries.Add(New cv.Vec4f(p1.X, p1.Y, p2.X, p2.Y))
            lastLength = p1.DistanceTo(p2)
        Else
            lengthReject += 1
            lastLength = bestLength
        End If
        labels(3) = "Length rejects = " + Format(lengthReject / (task.frameCount + 1), "0%")
    End Sub
End Class







Public Class XO_FeatureLine_VerticalLongLine : Inherits TaskParent
    Dim lines As New XO_FeatureLine_Finder3D
    Public Sub New()
        desc = "Use FeatureLine_Finder data to identify the longest lines and show its angle."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            dst2 = src.Clone
            lines.Run(src)

            If lines.sortedVerticals.Count = 0 Then
                SetTrueText("No vertical lines were found", 3)
                Exit Sub
            End If
        End If

        If lines.sortedVerticals.Count = 0 Then Exit Sub ' nothing found...
        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim p1 = lines.lines2D(index)
        Dim p2 = lines.lines2D(index + 1)
        DrawLine(dst2, p1, p2, task.highlight)
        dst3.SetTo(0)
        DrawLine(dst3, p1, p2, task.highlight)
        Dim pt1 = lines.lines3D(index)
        Dim pt2 = lines.lines3D(index + 1)
        Dim len3D = distance3D(pt1, pt2)
        Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
        SetTrueText(Format(arcY, fmt3) + vbCrLf + Format(len3D, fmt3) + "m len" + vbCrLf + Format(pt1.Z, fmt1) + "m dist", p1)
        SetTrueText(Format(arcY, fmt3) + vbCrLf + Format(len3D, fmt3) + "m len" + vbCrLf + Format(pt1.Z, fmt1) + "m distant", p1, 3)
    End Sub
End Class









Public Class XO_KNN_ClosestVertical : Inherits TaskParent
    Public lines As New XO_FeatureLine_Finder3D
    Public knn As New KNN_ClosestLine
    Public pt1 As New cv.Point3f
    Public pt2 As New cv.Point3f
    Public Sub New()
        labels = {"", "", "Highlight the tracked line", "Candidate vertical lines are in Blue"}
        desc = "Test the code find the longest line and track it using a minimized KNN test."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        lines.Run(src)
        If lines.sortedVerticals.Count = 0 Then
            SetTrueText("No vertical lines were found.")
            Exit Sub
        End If

        Dim index = lines.sortedVerticals.ElementAt(0).Value
        Dim lastDistance = knn.lastP1.DistanceTo(knn.lastP2)
        Dim bestDistance = lines.lines2D(index).DistanceTo(lines.lines2D(index + 1))
        If knn.lastP1 = New cv.Point2f Or lastDistance < 0.75 * bestDistance Then
            knn.lastP1 = lines.lines2D(index)
            knn.lastP2 = lines.lines2D(index + 1)
        End If

        knn.trainInput.Clear()
        For i = 0 To lines.sortedVerticals.Count - 1
            index = lines.sortedVerticals.ElementAt(i).Value
            knn.trainInput.Add(lines.lines2D(index))
            knn.trainInput.Add(lines.lines2D(index + 1))
        Next

        knn.Run(src)

        pt1 = lines.lines3D(knn.lastIndex)
        pt2 = lines.lines3D(knn.lastIndex + 1)

        dst3 = lines.dst3
        DrawLine(dst2, knn.lastP1, knn.lastP2, task.highlight)
    End Sub
End Class











Public Class XO_Line_VerticalHorizontalCells : Inherits TaskParent
    Dim lines As New XO_FeatureLine_Finder3D
    Dim hulls As New RedColor_Hulls
    Public Sub New()
        labels(2) = "RedColor_Hulls output with lines highlighted"
        desc = "Identify the lines created by the RedCloud Cells and separate vertical from horizontal"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2

        lines.Run(dst2.Clone)
        dst3 = src
        For i = 0 To lines.sortedHorizontals.Count - 1
            Dim index = lines.sortedHorizontals.ElementAt(i).Value
            Dim p1 = lines.lines2D(index), p2 = lines.lines2D(index + 1)
            DrawLine(dst3, p1, p2, cv.Scalar.Yellow)
        Next
        For i = 0 To lines.sortedVerticals.Count - 1
            Dim index = lines.sortedVerticals.ElementAt(i).Value
            Dim p1 = lines.lines2D(index), p2 = lines.lines2D(index + 1)
            DrawLine(dst3, p1, p2, cv.Scalar.Blue)
        Next
        labels(3) = CStr(lines.sortedVerticals.Count) + " vertical and " + CStr(lines.sortedHorizontals.Count) + " horizontal lines identified in the RedCloud output"
    End Sub
End Class







Public Class XO_Line_VerticalHorizontal1 : Inherits TaskParent
    Dim nearest As New XO_Line_Nearest
    Public Sub New()
        task.gOptions.LineWidth.Value = 2
        desc = "Find all the lines in the color image that are parallel to gravity or the horizon using distance to the line instead of slope."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pixelDiff = task.gOptions.pixelDiffThreshold

        dst2 = src.Clone
        If standaloneTest() Then dst3 = task.lines.dst2

        nearest.lp = task.lineGravity
        DrawLine(dst2, task.lineGravity.p1, task.lineGravity.p2, white)
        For Each lp In task.lines.lpList
            Dim ptInter = Line_Intersection.IntersectTest(lp.p1, lp.p2, task.lineGravity.p1, task.lineGravity.p2)
            If ptInter.X >= 0 And ptInter.X < dst2.Width And ptInter.Y >= 0 And ptInter.Y < dst2.Height Then
                Continue For
            End If

            nearest.pt = lp.p1
            nearest.Run(Nothing)
            Dim d1 = nearest.distance

            nearest.pt = lp.p2
            nearest.Run(Nothing)
            Dim d2 = nearest.distance

            If Math.Abs(d1 - d2) <= pixelDiff Then
                DrawLine(dst2, lp.p1, lp.p2, task.highlight)
            End If
        Next

        DrawLine(dst2, task.lineHorizon.p1, task.lineHorizon.p2, white)
        nearest.lp = task.lineHorizon
        For Each lp In task.lines.lpList
            Dim ptInter = Line_Intersection.IntersectTest(lp.p1, lp.p2, task.lineHorizon.p1, task.lineHorizon.p2)
            If ptInter.X >= 0 And ptInter.X < dst2.Width And ptInter.Y >= 0 And ptInter.Y < dst2.Height Then Continue For

            nearest.pt = lp.p1
            nearest.Run(Nothing)
            Dim d1 = nearest.distance

            nearest.pt = lp.p2
            nearest.Run(Nothing)
            Dim d2 = nearest.distance

            If Math.Abs(d1 - d2) <= pixelDiff Then
                DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Red)
            End If
        Next
        labels(2) = "Slope for gravity is " + Format(task.lineGravity.slope, fmt1) + ".  Slope for horizon is " +
                    Format(task.lineHorizon.slope, fmt1)
    End Sub
End Class









Public Class XO_OpenGL_VerticalOrHorizontal : Inherits TaskParent
    Dim vLine As New XO_FeatureLine_Finder3D
    Public Sub New()
        If OptionParent.FindFrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Show Vertical Lines")
            radio.addRadio("Show Horizontal Lines")
            radio.check(0).Checked = True
        End If

        task.ogl.oglFunction = oCase.drawLineAndCloud
        desc = "Visualize all the vertical lines found in FeatureLine_Finder"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static verticalRadio = OptionParent.findRadio("Show Vertical Lines")
        Dim showVerticals = verticalRadio.checked

        vLine.Run(src)
        dst2 = vLine.dst3

        task.ogl.pointCloudInput = task.pointCloud

        'Dim lines3D As New List(Of cv.Point3f)
        'Dim count = If(showVerticals, vLine.sortedVerticals.Count, vLine.sortedHorizontals.Count)
        'For i = 0 To count - 1
        '    Dim index = If(showVerticals, vLine.sortedVerticals.ElementAt(i).Value, vLine.sortedHorizontals.ElementAt(i).Value)
        '    lines3D.Add(vLine.lines3D(index))
        '    lines3D.Add(vLine.lines3D(index + 1))
        'Next
        'task.ogl.dataInput = cv.Mat.FromPixelData(lines3D.Count, 1, cv.MatType.CV_32FC3, lines3D.ToArray)
        'task.ogl.Run(task.color)
    End Sub
End Class






Public Class XO_FeatureLine_BasicsRaw : Inherits TaskParent
    Dim lines As New XO_Line_RawSubset
    Dim lineDisp As New XO_Line_DisplayInfoOld
    Dim options As New Options_Features
    Dim match As New Match_tCell
    Public tcells As List(Of tCell)
    Public Sub New()
        Dim tc As New tCell
        tcells = New List(Of tCell)({tc, tc})
        labels = {"", "", "Longest line present.", ""}
        desc = "Find and track a line using the end points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        Dim distanceThreshold = 50 ' pixels - arbitrary but realistically needs some value
        Dim linePercentThreshold = 0.7 ' if less than 70% of the pixels in the line are edges, then find a better line.  Again, arbitrary but realistic.

        Dim correlationTest = tcells(0).correlation <= task.fCorrThreshold Or tcells(1).correlation <= task.fCorrThreshold
        lineDisp.distance = tcells(0).center.DistanceTo(tcells(1).center)
        If task.optionsChanged Or correlationTest Or lineDisp.maskCount / lineDisp.distance < linePercentThreshold Or
           lineDisp.distance < distanceThreshold Then

            Dim pad = task.brickSize / 2
            lines.subsetRect = New cv.Rect(pad * 3, pad * 3, src.Width - pad * 6, src.Height - pad * 6)
            lines.Run(src.Clone)

            If lines.lpList.Count = 0 Then
                SetTrueText("No lines found.", 3)
                Exit Sub
            End If
            Dim lp = lines.lpList(0)

            tcells(0) = match.createCell(src, 0, lp.p1)
            tcells(1) = match.createCell(src, 0, lp.p2)
        End If

        dst2 = src.Clone
        For i = 0 To tcells.Count - 1
            match.tCells(0) = tcells(i)
            match.Run(src)
            tcells(i) = match.tCells(0)
            SetTrueText(tcells(i).strOut, New cv.Point(tcells(i).rect.X, tcells(i).rect.Y))
            SetTrueText(tcells(i).strOut, New cv.Point(tcells(i).rect.X, tcells(i).rect.Y), 3)
        Next

        lineDisp.tcells = New List(Of tCell)(tcells)
        lineDisp.Run(src)
        dst2 = lineDisp.dst2
        SetTrueText(lineDisp.strOut, New cv.Point(10, 40), 3)
    End Sub
End Class







Public Class XO_FeatureLine_DetailsAll : Inherits TaskParent
    Dim lines As New XO_FeatureLine_Finder3D
    Dim flow As New Font_FlowText
    Dim arcList As New List(Of Single)
    Dim arcLongAverage As New List(Of Single)
    Dim firstAverage As New List(Of Single)
    Dim firstBest As Integer
    Dim title = "ID" + vbTab + "length" + vbTab + "distance "
    Public Sub New()
        flow.parentData = Me
        flow.dst = 3
        desc = "Use FeatureLine_Finder data to collect vertical lines and measure accuracy of each."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            dst2 = src.Clone
            lines.Run(src)

            If lines.sortedVerticals.Count = 0 Then
                SetTrueText("No vertical lines were found", 3)
                Exit Sub
            End If

            dst3.SetTo(0)
            arcList.Clear()
            flow.nextMsg = title
            For i = 0 To Math.Min(10, lines.sortedVerticals.Count) - 1
                Dim index = lines.sortedVerticals.ElementAt(i).Value
                Dim p1 = lines.lines2D(index)
                Dim p2 = lines.lines2D(index + 1)
                DrawLine(dst2, p1, p2, task.highlight)
                SetTrueText(CStr(i), If(i Mod 2, p1, p2), 2)
                DrawLine(dst3, p1, p2, task.highlight)

                Dim pt1 = lines.lines3D(index)
                Dim pt2 = lines.lines3D(index + 1)
                Dim len3D = distance3D(pt1, pt2)
                If len3D > 0 Then
                    Dim arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958)
                    arcList.Add(arcY)
                    flow.nextMsg += Format(arcY, fmt3) + " degrees" + vbTab + Format(len3D, fmt3) + "m " + vbTab + Format(pt1.Z, fmt1) + "m"
                End If
            Next
            If flow.nextMsg = title Then flow.nextMsg = "No feature line found..."
        End If
        flow.Run(src)
        If arcList.Count = 0 Then Exit Sub

        Dim mostAccurate = arcList(0)
        firstAverage.Add(mostAccurate)
        For Each arc In arcList
            If arc > mostAccurate Then
                mostAccurate = arc
                Exit For
            End If
        Next
        If mostAccurate = arcList(0) Then firstBest += 1

        Dim avg = arcList.Average()
        arcLongAverage.Add(avg)
        labels(3) = "arcY avg = " + Format(avg, fmt1) + ", long term average = " + Format(arcLongAverage.Average, fmt1) +
                    ", first was best " + Format(firstBest / task.frameCount, "0%") + " of the time, Avg of longest line " + Format(firstAverage.Average, fmt1)
        If arcLongAverage.Count > 1000 Then
            arcLongAverage.RemoveAt(0)
            firstAverage.RemoveAt(0)
        End If
    End Sub
End Class







Public Class XO_FeatureLine_LongestKNN : Inherits TaskParent
    Dim glines As New Line_GCloud
    Public knn As New KNN_ClosestTracker
    Public gline As gravityLine
    Public match As New Match_Basics
    Dim p1 As cv.Point, p2 As cv.Point
    Public Sub New()
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src

        knn.Run(src.Clone)
        p1 = knn.lastPair.p1
        p2 = knn.lastPair.p2
        gline = glines.updateGLine(src, gline, p1, p2)

        Dim rect = ValidateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
        match.template = src(rect).Clone
        match.Run(src)
        If match.correlation >= task.fCorrThreshold Then
            dst3 = match.dst0.Resize(dst3.Size)
            DrawLine(dst2, p1, p2, task.highlight)
            DrawCircle(dst2, p1, task.DotSize, task.highlight)
            DrawCircle(dst2, p2, task.DotSize, task.highlight)
            rect = ValidateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
            match.template = src(rect).Clone
        Else
            task.highlight = If(task.highlight = cv.Scalar.Yellow, cv.Scalar.Blue, cv.Scalar.Yellow)
            knn.lastPair = New lpData(New cv.Point2f, New cv.Point2f)
        End If
        labels(2) = "Longest line end points had correlation of " + Format(match.correlation, fmt3) + " with the original longest line."
    End Sub
End Class






Public Class XO_FeatureLine_Longest : Inherits TaskParent
    Dim glines As New Line_GCloud
    Public knn As New KNN_ClosestTracker
    Public gline As gravityLine
    Public match1 As New Match_Basics
    Public match2 As New Match_Basics
    Public Sub New()
        labels(2) = "Longest line end points are highlighted "
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        Dim pad = task.brickSize / 2

        Static p1 As cv.Point, p2 As cv.Point
        If task.heartBeat Or match1.correlation < task.fCorrThreshold And
                             match2.correlation < task.fCorrThreshold Then
            knn.Run(src.Clone)

            p1 = knn.lastPair.p1
            Dim r1 = ValidateRect(New cv.Rect(p1.X - pad, p1.Y - pad, task.brickSize, task.brickSize))
            match1.template = src(r1).Clone

            p2 = knn.lastPair.p2
            Dim r2 = ValidateRect(New cv.Rect(p2.X - pad, p2.Y - pad, task.brickSize, task.brickSize))
            match2.template = src(r2).Clone
        End If

        match1.Run(src)
        p1 = match1.newCenter

        match2.Run(src)
        p2 = match2.newCenter

        gline = glines.updateGLine(src, gline, p1, p2)
        DrawLine(dst2, p1, p2, task.highlight)
        DrawCircle(dst2, p1, task.DotSize, task.highlight)
        DrawCircle(dst2, p2, task.DotSize, task.highlight)
        SetTrueText(Format(match1.correlation, fmt3), p1)
        SetTrueText(Format(match2.correlation, fmt3), p2)
    End Sub
End Class









Public Class XO_Structured_Cloud2 : Inherits TaskParent
    Dim mmPixel As New Pixel_Measure
    Dim options As New Options_StructuredCloud
    Public Sub New()
        desc = "Attempt to impose a structure on the point cloud data."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

        Dim stepX = dst2.Width / options.xLines
        Dim stepY = dst2.Height / options.yLines
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC3, 0)
        Dim midX = dst2.Width / 2
        Dim midY = dst2.Height / 2
        Dim halfStepX = stepX / 2
        Dim halfStepy = stepY / 2
        For y = 1 To options.yLines - 2
            For x = 1 To options.xLines - 2
                Dim p1 = New cv.Point2f(x * stepX, y * stepY)
                Dim p2 = New cv.Point2f((x + 1) * stepX, y * stepY)
                Dim d1 = task.pcSplit(2).Get(Of Single)(p1.Y, p1.X)
                Dim d2 = task.pcSplit(2).Get(Of Single)(p2.Y, p2.X)
                If stepX * options.threshold > Math.Abs(d1 - d2) And d1 > 0 And d2 > 0 Then
                    Dim p = task.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
                    Dim mmPP = mmPixel.Compute(d1)
                    If options.xConstraint Then
                        p(0) = (p1.X - midX) * mmPP
                        If p1.X = midX Then p(0) = mmPP
                    End If
                    If options.yConstraint Then
                        p(1) = (p1.Y - midY) * mmPP
                        If p1.Y = midY Then p(1) = mmPP
                    End If
                    Dim r = New cv.Rect(p1.X - halfStepX, p1.Y - halfStepy, stepX, stepY)
                    Dim meanVal = cv.Cv2.Mean(task.pcSplit(2)(r), task.depthMask(r))
                    p(2) = (d1 + d2) / 2
                    dst3.Set(Of cv.Vec3f)(y, x, p)
                End If
            Next
        Next
        dst2 = dst3(New cv.Rect(0, 0, options.xLines, options.yLines)).Resize(dst2.Size(), 0, 0,
                                                                              cv.InterpolationFlags.Nearest)
    End Sub
End Class








Public Class XO_Structured_Cloud : Inherits TaskParent
    Public options As New Options_StructuredCloud
    Public Sub New()
        task.gOptions.GridSlider.Value = 10
        desc = "Attempt to impose a linear structure on the pointcloud."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim yLines = CInt(options.xLines * dst2.Height / dst2.Width)

        Dim stepX = dst3.Width / options.xLines
        Dim stepY = dst3.Height / yLines
        dst2 = New cv.Mat(dst3.Size(), cv.MatType.CV_32FC3, 0)
        For y = 0 To yLines - 1
            For x = 0 To options.xLines - 1
                Dim r = New cv.Rect(x * stepX, y * stepY, stepX - 1, stepY - 1)
                Dim p1 = New cv.Point(r.X, r.Y)
                Dim p2 = New cv.Point(r.X + r.Width, r.Y + r.Height)
                Dim vec1 = task.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
                Dim vec2 = task.pointCloud.Get(Of cv.Vec3f)(p2.Y, p2.X)
                If vec1(2) > 0 And vec2(2) > 0 Then dst2(r).SetTo(vec1)
            Next
        Next
        labels(2) = "Structured_Cloud with " + CStr(yLines) + " rows " + CStr(options.xLines) + " columns"
    End Sub
End Class








Public Class XO_OpenGL_StructuredCloud : Inherits TaskParent
    Dim sCloud As New XO_Structured_Cloud
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        labels(2) = "Structured cloud 32fC3 data"
        desc = "Visualize the Structured_Cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sCloud.Run(src)

        dst2 = runRedC(src, labels(2))
        task.ogl.pointCloudInput = sCloud.dst2
        task.ogl.Run(dst2)
    End Sub
End Class





Public Class XO_OpenGL_PCpointsPlane : Inherits TaskParent
    Dim pts As New XO_PointCloud_PCPointsPlane
    Public Sub New()
        task.ogl.oglFunction = oCase.pcPoints
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        desc = "Display the points that are likely to be in a plane - found by both the vertical and horizontal searches"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pts.Run(src)

        task.ogl.dataInput = cv.Mat.FromPixelData(pts.pcPoints.Count, 1, cv.MatType.CV_32FC3, pts.pcPoints.ToArray)
        task.ogl.Run(New cv.Mat)
        labels(2) = "Point cloud points found = " + CStr(pts.pcPoints.Count / 2)
    End Sub
End Class










Public Class XO_Structured_Crosshairs : Inherits TaskParent
    Dim sCloud As New XO_Structured_Cloud
    Dim minX As Single, maxX As Single, minY As Single, maxY As Single
    Public Sub New()
        desc = "Connect vertical and horizontal dots that are in the same column and row."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim xLines = sCloud.options.indexX
        Dim yLines = CInt(xLines * dst2.Width / dst2.Height)
        If sCloud.options.indexX > xLines Then sCloud.options.indexX = xLines - 1
        If sCloud.options.indexY > yLines Then sCloud.options.indexY = yLines - 1

        sCloud.Run(src)
        Dim split = cv.Cv2.Split(sCloud.dst2)

        Dim mmX = GetMinMax(split(0))
        Dim mmY = GetMinMax(split(1))

        minX = If(minX > mmX.minVal, mmX.minVal, minX)
        minY = If(minY > mmY.minVal, mmY.minVal, minY)
        maxX = If(maxX < mmX.maxVal, mmX.maxVal, maxX)
        maxY = If(maxY < mmY.maxVal, mmY.maxVal, maxY)

        SetTrueText("mmx min/max = " + Format(minX, "0.00") + "/" + Format(maxX, "0.00") + " mmy min/max " + Format(minY, "0.00") +
                    "/" + Format(maxY, "0.00"), 3)

        dst2.SetTo(0)
        Dim white = New cv.Vec3b(255, 255, 255)
        Dim pointX As New cv.Mat(sCloud.dst2.Size(), cv.MatType.CV_32S, 0)
        Dim pointY As New cv.Mat(sCloud.dst2.Size(), cv.MatType.CV_32S, 0)
        Dim yy As Integer, xx As Integer
        For y = 1 To sCloud.dst2.Height - 1
            For x = 1 To sCloud.dst2.Width - 1
                Dim p = sCloud.dst2.Get(Of cv.Vec3f)(y, x)
                If p(2) > 0 Then
                    If Single.IsNaN(p(0)) Or Single.IsNaN(p(1)) Or Single.IsNaN(p(2)) Then Continue For
                    xx = dst2.Width * (maxX - p(0)) / (maxX - minX)
                    yy = dst2.Height * (maxY - p(1)) / (maxY - minY)
                    If xx < 0 Then xx = 0
                    If yy < 0 Then yy = 0
                    If xx >= dst2.Width Then xx = dst2.Width - 1
                    If yy >= dst2.Height Then yy = dst2.Height - 1
                    yy = dst2.Height - yy - 1
                    xx = dst2.Width - xx - 1
                    dst2.Set(Of cv.Vec3b)(yy, xx, white)

                    pointX.Set(Of Integer)(y, x, xx)
                    pointY.Set(Of Integer)(y, x, yy)
                    If x = sCloud.options.indexX Then
                        Dim p1 = New cv.Point(pointX.Get(Of Integer)(y - 1, x), pointY.Get(Of Integer)(y - 1, x))
                        If p1.X > 0 Then
                            Dim p2 = New cv.Point(xx, yy)
                            dst2.Line(p1, p2, task.highlight, task.lineWidth + 1, task.lineType)
                        End If
                    End If
                    If y = sCloud.options.indexY Then
                        Dim p1 = New cv.Point(pointX.Get(Of Integer)(y, x - 1), pointY.Get(Of Integer)(y, x - 1))
                        If p1.X > 0 Then
                            Dim p2 = New cv.Point(xx, yy)
                            dst2.Line(p1, p2, task.highlight, task.lineWidth + 1, task.lineType)
                        End If
                    End If
                End If
            Next
        Next
    End Sub
End Class








Public Class XO_Structured_ROI : Inherits TaskParent
    Public data As New cv.Mat
    Public oglData As New List(Of cv.Point3f)
    Public Sub New()
        task.gOptions.GridSlider.Value = 10
        desc = "Simplify the point cloud so it can be represented as quads in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = New cv.Mat(dst3.Size(), cv.MatType.CV_32FC3, 0)
        For Each roi In task.gridRects
            Dim d = task.pointCloud(roi).Mean(task.depthMask(roi))
            Dim depth = New cv.Vec3f(d.Val0, d.Val1, d.Val2)
            Dim pt = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
            Dim vec = task.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X)
            If vec(2) > 0 Then dst2(roi).SetTo(depth)
        Next

        labels(2) = traceName + " with " + CStr(task.gridRects.Count) + " regions was created"
    End Sub
End Class








Public Class XO_Structured_Tiles : Inherits TaskParent
    Public oglData As New List(Of cv.Vec3f)
    Dim hulls As New RedColor_Hulls
    Public Sub New()
        task.gOptions.GridSlider.Value = 10
        desc = "Use the OpenGL point size to represent the point cloud as data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst3

        dst3.SetTo(0)
        oglData.Clear()
        For Each roi In task.gridRects
            Dim c = dst2.Get(Of cv.Vec3b)(roi.Y, roi.X)
            If c = black Then Continue For
            oglData.Add(New cv.Vec3f(c(2) / 255, c(1) / 255, c(0) / 255))

            Dim v = task.pointCloud(roi).Mean(task.depthMask(roi))
            oglData.Add(New cv.Vec3f(v.Val0, v.Val1, v.Val2))
            dst3(roi).SetTo(c)
        Next
        labels(2) = traceName + " with " + CStr(task.gridRects.Count) + " regions was created"
    End Sub
End Class






Public Class XO_LineRect_CenterDepth : Inherits TaskParent
    Public options As New Options_LineRect
    Public Sub New()
        task.needBricks = True
        desc = "Remove lines which have similar depth in bricks on either side of a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = src.Clone
        dst3 = src.Clone

        Dim depthThreshold = options.depthThreshold
        Dim depthLines As Integer, colorLines As Integer
        For Each lp In task.lines.lpList
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
            Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
            Dim lpPerp = lp.perpendicularPoints(center)
            Dim index1 As Integer = task.grid.gridMap.Get(Of Integer)(lpPerp.p1.Y, lpPerp.p1.X)
            Dim index2 As Integer = task.grid.gridMap.Get(Of Integer)(lpPerp.p2.Y, lpPerp.p2.X)
            Dim brick1 = task.bricks.brickList(index1)
            Dim brick2 = task.bricks.brickList(index2)
            If Math.Abs(brick1.depth - brick2.depth) > depthThreshold Then
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                depthLines += 1
            Else
                dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                colorLines += 1
            End If
        Next

        If task.heartBeat Then
            labels(2) = CStr(depthLines) + " lines were found between objects (depth Lines)"
            labels(3) = CStr(colorLines) + " internal lines were indentified and are not likely important"
        End If
    End Sub
End Class





Public Class XO_LineCoin_Parallel : Inherits TaskParent
    Dim parallel As New XO_Line_Parallel
    Dim near As New XO_Line_Nearest
    Public coinList As New List(Of coinPoints)
    Public Sub New()
        desc = "Find the lines that are coincident in the parallel lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        parallel.Run(src)

        coinList.Clear()

        For Each cp In parallel.parList
            near.lp = New lpData(cp.p1, cp.p2)
            near.pt = cp.p3
            near.Run(src)
            Dim d1 = near.distance

            near.pt = cp.p4
            near.Run(src)
            If near.distance <= 1 Or d1 <= 1 Then coinList.Add(cp)
        Next

        dst2 = src.Clone
        For Each cp In coinList
            dst2.Line(cp.p3, cp.p4, cv.Scalar.Red, task.lineWidth + 2, task.lineType)
            dst2.Line(cp.p1, cp.p2, task.highlight, task.lineWidth + 1, task.lineType)
        Next
        labels(2) = CStr(coinList.Count) + " coincident lines were detected"
    End Sub
End Class










Public Class XO_Structured_Depth : Inherits TaskParent
    Dim sliceH As New Structured_SliceH
    Public Sub New()
        labels = {"", "", "Use mouse to explore slices", "Top down view of the highlighted slice (at left)"}
        desc = "Use the structured depth to enhance the depth away from the centerline."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sliceH.Run(src)
        dst0 = sliceH.dst3
        dst2 = sliceH.dst2

        Dim mask = sliceH.sliceMask
        Dim perMeter = dst3.Height / task.MaxZmeters
        dst3.SetTo(0)
        Dim white As New cv.Vec3b(255, 255, 255)
        For y = 0 To mask.Height - 1
            For x = 0 To mask.Width - 1
                Dim val = mask.Get(Of Byte)(y, x)
                If val > 0 Then
                    Dim depth = task.pcSplit(2).Get(Of Single)(y, x)
                    Dim row = dst1.Height - depth * perMeter
                    dst3.Set(Of cv.Vec3b)(If(row < 0, 0, row), x, white)
                End If
            Next
        Next
    End Sub
End Class








Public Class XO_Structured_FloorCeiling : Inherits TaskParent
    Public slice As New Structured_SliceEither
    Public Sub New()
        task.kalman = New Kalman_Basics
        ReDim task.kalman.kInput(2 - 1)
        OptionParent.FindCheckBox("Top View (Unchecked Side View)").Checked = False
        desc = "Find the floor or ceiling plane"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        slice.Run(src)
        dst2 = slice.heat.dst3

        Dim floorMax As Single
        Dim floorY As Integer
        Dim floorBuffer = dst2.Height / 4
        For i = dst2.Height - 1 To 0 Step -1
            Dim nextSum = slice.heat.dst3.Row(i).Sum()(0)
            If nextSum > 0 Then floorBuffer -= 1
            If floorBuffer = 0 Then Exit For
            If nextSum > floorMax Then
                floorMax = nextSum
                floorY = i
            End If
        Next

        Dim ceilingMax As Single
        Dim ceilingY As Integer
        Dim ceilingBuffer = dst2.Height / 4
        For i = 0 To dst3.Height - 1
            Dim nextSum = slice.heat.dst3.Row(i).Sum()(0)
            If nextSum > 0 Then ceilingBuffer -= 1
            If ceilingBuffer = 0 Then Exit For
            If nextSum > ceilingMax Then
                ceilingMax = nextSum
                ceilingY = i
            End If
        Next

        task.kalman.kInput(0) = floorY
        task.kalman.kInput(1) = ceilingY
        task.kalman.Run(emptyMat)

        labels(2) = "Current slice is at row =" + CStr(task.mouseMovePoint.Y)
        labels(3) = "Ceiling is at row =" + CStr(CInt(task.kalman.kOutput(1))) + " floor at y=" + CStr(CInt(task.kalman.kOutput(0)))

        DrawLine(dst2, New cv.Point(0, floorY), New cv.Point(dst2.Width, floorY), cv.Scalar.Yellow)
        SetTrueText("floor", New cv.Point(10, floorY + task.DotSize), 3)

        Dim rect = New cv.Rect(0, Math.Max(ceilingY - 5, 0), dst2.Width, 10)
        Dim mask = slice.heat.dst3(rect)
        Dim mean As cv.Scalar, stdev As cv.Scalar
        cv.Cv2.MeanStdDev(mask, mean, stdev)
        If mean(0) < mean(2) Then
            DrawLine(dst2, New cv.Point(0, ceilingY), New cv.Point(dst2.Width, ceilingY), cv.Scalar.Yellow)
            SetTrueText("ceiling", New cv.Point(10, ceilingY + task.DotSize), 3)
        Else
            SetTrueText("Ceiling does not appear to be present", 3)
        End If
    End Sub
End Class








Public Class XO_Structured_Rebuild : Inherits TaskParent
    Dim heat As New HeatMap_Basics
    Dim options As New Options_Structured
    Dim thickness As Single
    Public pointcloud As New cv.Mat
    Public Sub New()
        labels = {"", "", "X values in point cloud", "Y values in point cloud"}
        desc = "Rebuild the point cloud using inrange - not useful yet"
    End Sub
    Private Function rebuildX(viewX As cv.Mat) As cv.Mat
        Dim output As New cv.Mat(task.pcSplit(1).Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim firstCol As Integer
        For firstCol = 0 To viewX.Width - 1
            If viewX.Col(firstCol).CountNonZero > 0 Then Exit For
        Next

        Dim lastCol As Integer
        For lastCol = viewX.Height - 1 To 0 Step -1
            If viewX.Row(lastCol).CountNonZero > 0 Then Exit For
        Next

        Dim sliceMask As New cv.Mat
        For i = firstCol To lastCol
            Dim planeX = -task.xRange * (task.topCameraPoint.X - i) / task.topCameraPoint.X
            If i > task.topCameraPoint.X Then planeX = task.xRange * (i - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X)

            cv.Cv2.InRange(task.pcSplit(0), planeX - thickness, planeX + thickness, sliceMask)
            output.SetTo(planeX, sliceMask)
        Next
        Return output
    End Function
    Private Function rebuildY(viewY As cv.Mat) As cv.Mat
        Dim output As New cv.Mat(task.pcSplit(1).Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim firstLine As Integer
        For firstLine = 0 To viewY.Height - 1
            If viewY.Row(firstLine).CountNonZero > 0 Then Exit For
        Next

        Dim lastLine As Integer
        For lastLine = viewY.Height - 1 To 0 Step -1
            If viewY.Row(lastLine).CountNonZero > 0 Then Exit For
        Next

        Dim sliceMask As New cv.Mat
        For i = firstLine To lastLine
            Dim planeY = -task.yRange * (task.sideCameraPoint.Y - i) / task.sideCameraPoint.Y
            If i > task.sideCameraPoint.Y Then planeY = task.yRange * (i - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y)

            cv.Cv2.InRange(task.pcSplit(1), planeY - thickness, planeY + thickness, sliceMask)
            output.SetTo(planeY, sliceMask)
        Next
        Return output
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim metersPerPixel = task.MaxZmeters / dst3.Height
        thickness = options.sliceSize * metersPerPixel
        heat.Run(src)

        If options.rebuilt Then
            task.pcSplit(0) = rebuildX(heat.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            task.pcSplit(1) = rebuildY(heat.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            cv.Cv2.Merge(task.pcSplit, pointcloud)
        Else
            task.pcSplit = task.pointCloud.Split()
            pointcloud = task.pointCloud
        End If

        dst2 = Convert32f_To_8UC3(task.pcSplit(0))
        dst3 = Convert32f_To_8UC3(task.pcSplit(1))
        dst2.SetTo(0, task.noDepthMask)
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class







Public Class XO_OpenGL_Rebuilt : Inherits TaskParent
    Dim rebuild As New XO_Structured_Rebuild
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPointCloudRGB
        desc = "Review the rebuilt point cloud from Structured_Rebuild"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rebuild.Run(src)
        dst2 = rebuild.dst2
        task.ogl.pointCloudInput = rebuild.pointcloud
        task.ogl.Run(task.color)
    End Sub
End Class








Public Class XO_tructured_MouseSlice : Inherits TaskParent
    Dim slice As New Structured_SliceEither
    Dim lines As New XO_Line_RawSorted
    Public Sub New()
        labels(2) = "Center Slice in yellow"
        labels(3) = "White = SliceV output, Red Dot is avgPt"
        desc = "Find the vertical center line with accurate depth data.."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.mouseMovePoint = newPoint Then task.mouseMovePoint = New cv.Point(dst2.Width / 2, dst2.Height)
        slice.Run(src)

        lines.Run(slice.sliceMask)
        Dim tops As New List(Of Integer)
        Dim bots As New List(Of Integer)
        Dim topsList As New List(Of cv.Point)
        Dim botsList As New List(Of cv.Point)
        If task.lines.lpList.Count > 0 Then
            dst3 = lines.dst2
            For Each lp In task.lines.lpList
                dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth + 3, task.lineType)
                tops.Add(If(lp.p1.Y < lp.p2.Y, lp.p1.Y, lp.p2.Y))
                bots.Add(If(lp.p1.Y > lp.p2.Y, lp.p1.Y, lp.p2.Y))
                topsList.Add(lp.p1)
                botsList.Add(lp.p2)
            Next

            'Dim topPt = topsList(tops.IndexOf(tops.Min))
            'Dim botPt = botsList(bots.IndexOf(bots.Max))
            'DrawCircle(dst3,New cv.Point2f((topPt.X + botPt.X) / 2, (topPt.Y + botPt.Y) / 2), task.DotSize + 5, cv.Scalar.Red)
            'dst3.Line(topPt, botPt, cv.Scalar.Red, task.lineWidth, task.lineType)
            'DrawLine(dst2,topPt, botPt, task.highlight, task.lineWidth + 2, task.lineType)
        End If
        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(white, dst3)
        End If
    End Sub
End Class








Public Class XO_OpenGL_Tiles : Inherits TaskParent
    Dim sCloud As New XO_Structured_Tiles
    Public Sub New()
        task.ogl.oglFunction = oCase.drawTiles
        labels = {"", "", "Input from Structured_Tiles", ""}
        desc = "Display the quads built by Structured_Tiles in OpenGL - uses OpenGL's point size"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sCloud.Run(src)
        dst2 = sCloud.dst2
        dst3 = sCloud.dst3

        task.ogl.dataInput = cv.Mat.FromPixelData(sCloud.oglData.Count, 1, cv.MatType.CV_32FC3, sCloud.oglData.ToArray)
        task.ogl.Run(src)
    End Sub
End Class







Public Class XO_Contour_Gray : Inherits TaskParent
    Public contour As New List(Of cv.Point)
    Public options As New Options_Contours
    Dim myFrameCount As Integer = task.frameCount
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Find the contour for the src."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If myFrameCount <> task.frameCount Then
            options.Run() ' avoid running options more than once per frame.
            myFrameCount = task.frameCount
        End If

        If standalone Then
            task.gOptions.ColorSource.SelectedItem() = "Reduction_Basics"
            reduction.Run(src)
            src = reduction.dst2
        End If

        Dim allContours As cv.Point()() = Nothing
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.FindContours(src, allContours, Nothing, cv.RetrievalModes.External, options.ApproximationMode)
        If allContours.Count = 0 Then Exit Sub

        dst2 = src
        For Each tour In allContours
            DrawContour(dst2, tour.ToList, white, task.lineWidth)
        Next
        labels(2) = $"There were {allContours.Count} contours found."
    End Sub
End Class







Public Class XO_Contour_RC_AddContour : Inherits TaskParent
    Public contour As New List(Of cv.Point)
    Public options As New Options_Contours
    Dim myFrameCount As Integer = task.frameCount
    Dim reduction As New Reduction_Basics
    Dim contours As New Contour_Regions
    Public Sub New()
        desc = "Find the contour for the src."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If myFrameCount <> task.frameCount Then
            options.Run() ' avoid running options more than once per frame.
            myFrameCount = task.frameCount
        End If

        If standalone Then
            reduction.Run(src)
            src = reduction.dst2
        End If
        dst2 = src.Clone
        dst3 = ShowPalette(dst2)

        contours.Run(dst2)

        Dim maxCount As Integer, maxIndex As Integer
        For i = 0 To contours.contourList.Count - 1
            Dim len = CInt(contours.contourList(i).Count)
            If len > maxCount Then
                maxCount = len
                maxIndex = i
            End If
        Next
        If contours.contourList.Count = 0 Then Exit Sub
        Dim contour = New List(Of cv.Point)(contours.contourList(maxIndex).ToList)
        DrawContour(dst2, contour, task.highlight, task.lineWidth)
    End Sub
End Class





Public Class XO_Contour_RedCloud : Inherits TaskParent
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Show all the contours found in the RedCloud output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        dst3.SetTo(0)
        For Each rc In task.redC.rcList
            DrawContour(dst3(rc.rect), rc.contour, 255, task.lineWidth)
        Next
    End Sub
End Class







Public Class XO_OpenGL_PlaneClusters3D : Inherits TaskParent
    Dim eq As New Plane_Equation
    Public Sub New()
        task.ogl.oglFunction = oCase.pcPoints
        OptionParent.FindSlider("OpenGL Point Size").Value = 10
        labels(3) = "Only the cells with a high probability plane are presented - blue on X-axis, green on Y-axis, red on Z-axis"
        desc = "Cluster the plane equations to find major planes in the image and display the clusters in OpenGL"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        dst3 = task.redC.dst3

        Dim pcPoints As New List(Of cv.Point3f)
        Dim blue As New cv.Point3f(0, 0, 1), red As New cv.Point3f(1, 0, 0), green As New cv.Point3f(0, 1, 0) ' NOTE: RGB, not BGR...
        For Each rc In task.redC.rcList
            If rc.mmZ.maxVal > 0 Then
                eq.rc = rc
                eq.Run(src)
                rc = eq.rc
            End If
            If rc.eq = New cv.Vec4f Then Continue For

            If rc.eq.Item0 > rc.eq.Item1 And rc.eq.Item0 > rc.eq.Item2 Then pcPoints.Add(red)
            If rc.eq.Item1 > rc.eq.Item0 And rc.eq.Item1 > rc.eq.Item2 Then pcPoints.Add(green)
            If rc.eq.Item2 > rc.eq.Item0 And rc.eq.Item2 > rc.eq.Item1 Then pcPoints.Add(blue)

            pcPoints.Add(New cv.Point3f(rc.eq.Item0 * 0.5, rc.eq.Item1 * 0.5, rc.eq.Item2 * 0.5))
        Next

        task.ogl.dataInput = cv.Mat.FromPixelData(pcPoints.Count, 1, cv.MatType.CV_32FC3, pcPoints.ToArray)
        task.ogl.Run(New cv.Mat)
    End Sub
End Class





Public Class XO_Pixel_Unique_CPP : Inherits TaskParent
    Public Sub New()
        cPtr = Pixels_Vector_Open()
        desc = "Create the list of pixels in a RedCloud Cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.drawRect <> New cv.Rect Then src = src(task.drawRect)
        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim classCount = Pixels_Vector_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        If classCount = 0 Then Exit Sub
        Dim pixelData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_8UC3, Pixels_Vector_Pixels(cPtr))
        SetTrueText(CStr(classCount) + " unique BGR pixels were found in the src." + vbCrLf +
                    "Or " + Format(classCount / src.Total, "0%") + " of the input were unique pixels.")
    End Sub
    Public Sub Close()
        Pixels_Vector_Close(cPtr)
    End Sub
End Class





Public Class XO_Sides_Corner : Inherits TaskParent
    Dim sides As New XO_Contour_RedCloudCorners
    Public Sub New()
        labels = {"", "", "RedColor_Basics output", ""}
        desc = "Find the 4 points farthest from the center in each quadrant of the selected RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        sides.Run(src)
        dst3 = sides.dst3
        SetTrueText("Center point is rcSelect.maxDist", 3)
    End Sub
End Class









Public Class XO_Sides_Basics : Inherits TaskParent
    Public sides As New Profile_Basics
    Public corners As New XO_Contour_RedCloudCorners
    Public Sub New()
        labels = {"", "", "RedCloud output", "Selected Cell showing the various extrema."}
        desc = "Find the 6 extrema and the 4 farthest points in each quadrant for the selected RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        sides.Run(src)
        dst2 = sides.dst2
        dst3 = sides.dst3

        Dim corners = sides.corners.ToList
        For i = 0 To corners.Count - 1
            Dim nextColor = sides.cornerColors(i)
            Dim nextLabel = sides.cornerNames(i)
            DrawLine(dst3, task.rcD.maxDist, corners(i), white)
            SetTrueText(nextLabel, New cv.Point(corners(i).X, corners(i).Y), 3)
        Next

        If corners.Count Then SetTrueText(sides.strOut, 3) Else SetTrueText(strOut, 3)
    End Sub
End Class






Public Class XO_Contour_RedCloudCorners : Inherits TaskParent
    Public corners(4 - 1) As cv.Point
    Public rc As New rcData
    Public Sub New()
        labels(2) = "The RedCloud Output with the highlighted contour to smooth"
        desc = "Find the point farthest from the center in each cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            dst2 = runRedC(src, labels(2))
            rc = task.rcD
        End If

        dst3.SetTo(0)
        DrawCircle(dst3, rc.maxDist, task.DotSize, white)
        Dim center As New cv.Point(rc.maxDist.X - rc.rect.X, rc.maxDist.Y - rc.rect.Y)
        Dim maxDistance(4 - 1) As Single
        For i = 0 To corners.Length - 1
            corners(i) = center ' default is the center - a triangle shape can omit a corner
        Next
        If rc.contour Is Nothing Then Exit Sub
        For Each pt In rc.contour
            Dim quad As Integer
            If pt.X - center.X >= 0 And pt.Y - center.Y <= 0 Then quad = 0 ' upper right quadrant
            If pt.X - center.X >= 0 And pt.Y - center.Y >= 0 Then quad = 1 ' lower right quadrant
            If pt.X - center.X <= 0 And pt.Y - center.Y >= 0 Then quad = 2 ' lower left quadrant
            If pt.X - center.X <= 0 And pt.Y - center.Y <= 0 Then quad = 3 ' upper left quadrant
            Dim dist = center.DistanceTo(pt)
            If dist > maxDistance(quad) Then
                maxDistance(quad) = dist
                corners(quad) = pt
            End If
        Next

        DrawContour(dst3(rc.rect), rc.contour, white)
        For i = 0 To corners.Count - 1
            DrawLine(dst3(rc.rect), center, corners(i), white)
        Next
    End Sub
End Class





Public Class XO_Sides_Profile : Inherits TaskParent
    Dim sides As New Contour_SidePoints
    Public Sub New()
        labels = {"", "", "RedColor_Basics Output", "Selected Cell"}
        desc = "Find the 6 corners - left/right, top/bottom, front/back - of a RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        sides.Run(src)
        dst3 = sides.dst3
        SetTrueText(sides.strOut, 3)
    End Sub
End Class









Public Class XO_Sides_ColorC : Inherits TaskParent
    Dim sides As New XO_Sides_Basics
    Public Sub New()
        labels = {"", "", "RedColor Output", "Cell Extrema"}
        desc = "Find the extrema - top/bottom, left/right, near/far - points for a RedColor Cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        sides.Run(src)
        dst3 = sides.dst3
    End Sub
End Class






Public Class XO_Contour_RedCloudEdges : Inherits TaskParent
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "EdgeLine_Basics output", "", "Pixels below are both cell boundaries and edges."}
        desc = "Intersect the cell contours and the edges in the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        runRedC(src, labels(3))
        labels(2) = task.redC.labels(2) + " - Contours only.  Click anywhere to select a cell"

        dst2.SetTo(0)
        For Each rc In task.redC.rcList
            DrawContour(dst2(rc.rect), rc.contour, 255, task.lineWidth)
        Next

        dst3 = task.edges.dst2 And dst2
    End Sub
End Class







Public Class XO_LeftRight_Markers : Inherits TaskParent
    Dim redView As New LeftRight_Reduction
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "Reduced Left Image", "Reduced Right Image"}
        desc = "Use the left/right reductions to find hard markers - neighboring pixels of identical values"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redView.Run(src)
        dst2 = redView.reduction.dst3.Clone
        dst3 = redView.reduction.dst3.Clone

        Dim left = redView.dst2
        Dim right = redView.dst3

        ' find combinations in the left image - they are markers.
        Dim impList As New List(Of List(Of Integer))
        Dim lineLen = task.gOptions.DebugSlider.Value
        For y = 0 To left.Height - 1
            Dim important As New List(Of Integer)
            Dim impCounts As New List(Of Integer)
            For x = 0 To left.Width - 1
                Dim m1 = left.Get(Of Byte)(y, x)
                If important.Contains(m1) = False Then
                    important.Add(m1)
                    impCounts.Add(1)
                Else
                    impCounts(important.IndexOf(m1)) += 1
                End If
            Next
            impList.Add(important)
            impList.Add(impCounts)
        Next

        dst0.SetTo(0)
        dst1.SetTo(0)

        For i = 0 To left.Rows - 1
            Dim important = impList(i * 2)
            Dim impcounts = impList(i * 2 + 1)
            Dim maxVal = important(impcounts.IndexOf(impcounts.Max))

            Dim tmp = left.Row(i).InRange(maxVal, maxVal)
            dst0.Row(i).SetTo(255, tmp)

            tmp = right.Row(i).InRange(maxVal, maxVal)
            dst1.Row(i).SetTo(255, tmp)
        Next
    End Sub
End Class








Public Class XO_LeftRight_Markers1 : Inherits TaskParent
    Dim redView As New LeftRight_Reduction
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "Reduced Left Image", "Reduced Right Image"}
        desc = "Use the left/right reductions to find markers - neighboring pixels of identical values"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redView.Run(src)
        dst0 = redView.dst2
        dst1 = redView.dst3

        ' find combinations in the left image - they are markers.
        Dim impList As New List(Of List(Of Integer))
        Dim lineLen = task.gOptions.DebugSlider.Value
        For y = 0 To dst2.Height - 1
            Dim important As New List(Of Integer)
            Dim impCounts As New List(Of Integer)
            For x = 0 To dst0.Width - 1
                Dim m1 = dst0.Get(Of Byte)(y, x)
                If important.Contains(m1) = False Then
                    important.Add(m1)
                    impCounts.Add(1)
                Else
                    impCounts(important.IndexOf(m1)) += 1
                End If
            Next
            impList.Add(important)
            impList.Add(impCounts)
        Next

        dst2.SetTo(0)
        dst3.SetTo(0)

        For i = 0 To dst2.Rows - 1
            Dim important = impList(i * 2)
            Dim impcounts = impList(i * 2 + 1)
            Dim maxVal = important(impcounts.IndexOf(impcounts.Max))

            Dim tmp = dst0.Row(i).InRange(maxVal, maxVal)
            dst2.Row(i).SetTo(255, tmp)

            tmp = dst1.Row(i).InRange(maxVal, maxVal)
            dst3.Row(i).SetTo(255, tmp)
        Next
    End Sub
End Class










Public Class XO_OpenGL_QuadBricks : Inherits TaskParent
    Dim quad As New Quad_Bricks
    Public Sub New()
        task.ogl.oglFunction = oCase.minMaxBlocks
        desc = "Create blocks in each roi using the min and max depth values"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        quad.Run(src)
        task.ogl.dataInput = cv.Mat.FromPixelData(quad.quadData.Count, 1, cv.MatType.CV_32FC3, quad.quadData.ToArray)
        dst2 = quad.dst2
        labels(2) = quad.labels(2)

        Dim index As Integer
        For Each roi In task.gridRects
            If index < quad.depths.Count Then
                SetTrueText(Format(quad.depths(index), fmt1) + vbCrLf + Format(quad.depths(index + 1), fmt1), New cv.Point(roi.X, roi.Y), 2)
            End If
            index += 2
        Next

        task.ogl.pointCloudInput = New cv.Mat()
        task.ogl.Run(src)
    End Sub
End Class







Public Class XO_OpenGL_QuadGridTiles : Inherits TaskParent
    Dim tiles As New Quad_GridTiles
    Public Sub New()
        task.ogl.oglFunction = oCase.quadBasics
        desc = "Display the quads built by Quad_Hulls in OpenGL - doesn't use OpenGL's point size"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        tiles.Run(src)
        dst2 = tiles.dst2

        task.ogl.dataInput = cv.Mat.FromPixelData(tiles.quadData.Count, 1, cv.MatType.CV_32FC3,
                                                  tiles.quadData.ToArray)
        task.ogl.Run(src)
        labels = tiles.labels
    End Sub
End Class






'https://www3.ntu.edu.sg/home/ehchua/programming/opengl/CG_Examples.html
Public Class XO_OpenGL_QuadMinMax : Inherits TaskParent
    Dim quad As New Quad_MinMax
    Public Sub New()
        task.ogl.oglFunction = oCase.quadBasics
        desc = "Reflect the min and max for each roi of the RedCloud data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        quad.Run(src)
        dst2 = quad.dst2
        dst3 = quad.dst3
        labels = quad.labels
        task.ogl.dataInput = cv.Mat.FromPixelData(quad.quadData.Count, 1, cv.MatType.CV_32FC3, quad.quadData.ToArray)

        task.ogl.pointCloudInput = New cv.Mat()
        task.ogl.Run(dst3)
    End Sub
End Class






Public Class XO_Color8U_Edges : Inherits TaskParent
    Dim edges As New Edge_Canny
    Public Sub New()
        desc = "Find edges in the Color8U_Basics output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.edges.dst2

        edges.Run(dst2)
        dst3 = edges.dst2
        labels(2) = task.edges.strOut
    End Sub
End Class






Public Class XO_Brick_FitLeftInColor : Inherits TaskParent
    Public Sub New()
        task.needBricks = True
        task.drawRect = New cv.Rect(10, 10, 50, 50)
        labels(3) = "Draw a rectangle to update."
        desc = "Translate the left image into the same coordinates as the color image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim correlationMat As New cv.Mat

        Dim p1 = task.bricks.brickList(0).lRect.TopLeft
        Dim p2 = task.bricks.brickList(task.bricks.brickList.Count - 1).lRect.BottomRight

        ' Dim rect = ValidateRect(New cv.Rect(p1.X - task.brickSize, p1.Y - task.brickSize, task.brickSize * 2, task.brickSize * 2))
        cv.Cv2.MatchTemplate(task.gray(task.drawRect), task.leftView, dst2, cv.TemplateMatchModes.CCoeffNormed)
        Dim mm = GetMinMax(dst2)
        dst3 = src(ValidateRect(New cv.Rect(mm.maxLoc.X / 2, mm.maxLoc.Y / 2, dst2.Width, dst2.Height)))
        labels(2) = "Correlation coefficient peak = " + Format(mm.maxVal, fmt3)
    End Sub
End Class





Public Class XO_Depth_MeanStdev_MT : Inherits TaskParent
    Dim meanSeries As New cv.Mat
    Dim maxMeanVal As Single, maxStdevVal As Single
    Public Sub New()
        If standalone Then task.gOptions.GridSlider.Value = task.gOptions.GridSlider.Maximum
        dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst3.Rows, dst3.Cols, cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Collect a time series of depth mean and stdev to highlight where depth is unstable."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then meanSeries = New cv.Mat(task.gridRects.Count, task.frameHistoryCount, cv.MatType.CV_32F, cv.Scalar.All(0))

        Dim index = task.frameCount Mod task.frameHistoryCount
        Dim meanValues(task.gridRects.Count - 1) As Single
        Dim stdValues(task.gridRects.Count - 1) As Single
        Parallel.For(0, task.gridRects.Count,
        Sub(i)
            Dim roi = task.gridRects(i)
            Dim mean As cv.Scalar, stdev As cv.Scalar
            cv.Cv2.MeanStdDev(task.pcSplit(2)(roi), mean, stdev, task.depthMask(roi))
            meanSeries.Set(Of Single)(i, index, mean)
            If task.frameCount >= task.frameHistoryCount - 1 Then
                cv.Cv2.MeanStdDev(meanSeries.Row(i), mean, stdev)
                meanValues(i) = mean
                stdValues(i) = stdev
            End If
        End Sub)

        If task.frameCount >= task.frameHistoryCount Then
            Dim means As cv.Mat = cv.Mat.FromPixelData(task.gridRects.Count, 1, cv.MatType.CV_32F, meanValues.ToArray)
            Dim stdevs As cv.Mat = cv.Mat.FromPixelData(task.gridRects.Count, 1, cv.MatType.CV_32F, stdValues.ToArray)
            Dim meanmask = means.Threshold(1, task.MaxZmeters, cv.ThresholdTypes.Binary).ConvertScaleAbs()
            Dim mm As mmData = GetMinMax(means, meanmask)
            Dim stdMask = stdevs.Threshold(0.001, task.MaxZmeters, cv.ThresholdTypes.Binary).ConvertScaleAbs() ' volatile region is x cm stdev.
            Dim mmStd = GetMinMax(stdevs, stdMask)

            maxMeanVal = Math.Max(maxMeanVal, mm.maxVal)
            maxStdevVal = Math.Max(maxStdevVal, mmStd.maxVal)

            Parallel.For(0, task.gridRects.Count,
            Sub(i)
                Dim roi = task.gridRects(i)
                dst3(roi).SetTo(255 * stdevs.Get(Of Single)(i, 0) / maxStdevVal)
                dst3(roi).SetTo(0, task.noDepthMask(roi))

                dst2(roi).SetTo(255 * means.Get(Of Single)(i, 0) / maxMeanVal)
                dst2(roi).SetTo(0, task.noDepthMask(roi))
            End Sub)

            If task.heartBeat Then
                maxMeanVal = 0
                maxStdevVal = 0
            End If

            If standaloneTest() Then
                For i = 0 To task.gridRects.Count - 1
                    Dim roi = task.gridRects(i)
                    SetTrueText(Format(meanValues(i), fmt3) + vbCrLf +
                                Format(stdValues(i), fmt3), roi.Location, 3)
                Next
            End If

            dst3 = dst3 Or task.gridMask
            labels(2) = "The regions where the depth is volatile are brighter.  Stdev min " + Format(mmStd.minVal, fmt3) + " Stdev Max " + Format(mmStd.maxVal, fmt3)
            labels(3) = "Mean/stdev for each ROI: Min " + Format(mm.minVal, fmt3) + " Max " + Format(mm.maxVal, fmt3)
        End If
    End Sub
End Class





Public Class XO_ML_FillRGBDepth_MT : Inherits TaskParent
    Dim shadow As New Depth_Holes
    Dim colorizer As New DepthColorizer_CPP
    Public Sub New()
        task.gOptions.GridSlider.Maximum = dst2.Cols / 2
        task.gOptions.GridSlider.Value = CInt(dst2.Cols / 2)

        labels = {"", "", "ML filled shadow", ""}
        desc = "Predict depth based on color and colorize depth to confirm correctness of model.  NOTE: memory leak occurs if more multi-threading is used!"
    End Sub
    Private Class CompareVec3f : Implements IComparer(Of cv.Vec3f)
        Public Function Compare(ByVal a As cv.Vec3f, ByVal b As cv.Vec3f) As Integer Implements IComparer(Of cv.Vec3f).Compare
            If a(0) = b(0) And a(1) = b(1) And a(2) = b(2) Then Return 0
            Return If(a(0) < b(0), -1, 1)
        End Function
    End Class
    Public Function detectAndFillShadow(holeMask As cv.Mat, borderMask As cv.Mat, depth32f As cv.Mat, color As cv.Mat, minLearnCount As Integer) As cv.Mat
        Dim learnData As New SortedList(Of cv.Vec3f, Single)(New CompareVec3f)
        Dim rng As New System.Random
        Dim holeCount = cv.Cv2.CountNonZero(holeMask)
        If borderMask.Channels() <> 1 Then borderMask = borderMask.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim borderCount = cv.Cv2.CountNonZero(borderMask)
        If holeCount > 0 And borderCount > minLearnCount Then
            Dim color32f As New cv.Mat
            color.ConvertTo(color32f, cv.MatType.CV_32FC3)

            Dim learnInputList As New List(Of cv.Vec3f)
            Dim responseInputList As New List(Of Single)

            For y = 0 To holeMask.Rows - 1
                For x = 0 To holeMask.Cols - 1
                    If borderMask.Get(Of Byte)(y, x) Then
                        Dim vec = color32f.Get(Of cv.Vec3f)(y, x)
                        If learnData.ContainsKey(vec) = False Then
                            learnData.Add(vec, depth32f.Get(Of Single)(y, x)) ' keep out duplicates.
                            learnInputList.Add(vec)
                            responseInputList.Add(depth32f.Get(Of Single)(y, x))
                        End If
                    End If
                Next
            Next

            Dim learnInput As cv.Mat = cv.Mat.FromPixelData(learnData.Count, 3, cv.MatType.CV_32F, learnInputList.ToArray())
            Dim depthResponse As cv.Mat = cv.Mat.FromPixelData(learnData.Count, 1, cv.MatType.CV_32F, responseInputList.ToArray())

            ' now learn what depths are associated with which colors.
            Dim rtree = cv.ML.RTrees.Create()
            rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)

            ' now predict what the depth is based just on the color (and proximity to the region)
            Using predictMat As New cv.Mat(1, 3, cv.MatType.CV_32F)
                For y = 0 To holeMask.Rows - 1
                    For x = 0 To holeMask.Cols - 1
                        If holeMask.Get(Of Byte)(y, x) Then
                            predictMat.Set(Of cv.Vec3f)(0, 0, color32f.Get(Of cv.Vec3f)(y, x))
                            depth32f.Set(Of Single)(y, x, rtree.Predict(predictMat))
                        End If
                    Next
                Next
            End Using
        End If
        Return depth32f
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim minLearnCount = 5
        Parallel.ForEach(task.gridRects,
            Sub(roi)
                task.pcSplit(2)(roi) = detectAndFillShadow(task.noDepthMask(roi), shadow.dst3(roi), task.pcSplit(2)(roi), src(roi),
                                                           minLearnCount)
            End Sub)

        colorizer.Run(task.pcSplit(2))
        dst2 = colorizer.dst2.Clone()
        dst2.SetTo(white, task.gridMask)
    End Sub
End Class





Public Class XO_Line_BasicsAlternative : Inherits TaskParent
    Public lines As New XO_Line_RawSorted
    Public Sub New()
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0) ' can't use 32S because calcHist won't use it...
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Collect lines across frames using the motion mask.  Results are in task.lines.lpList."
    End Sub
    Private Function getLineCounts(lpList As List(Of lpData)) As Single()
        Dim histarray(lpList.Count - 1) As Single
        If lpList.Count > 0 Then
            Dim histogram As New cv.Mat
            dst1.SetTo(0)
            For Each lp In lpList
                dst1.Line(lp.p1, lp.p2, lp.index + 1, task.lineWidth, cv.LineTypes.Link4)
            Next

            cv.Cv2.CalcHist({dst1}, {0}, task.motionMask, histogram, 1, {lpList.Count}, New cv.Rangef() {New cv.Rangef(0, lpList.Count)})

            Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)
        End If

        Return histarray
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then task.lines.lpList.Clear()

        Dim histArray = getLineCounts(lines.lpList)
        Dim newList As New List(Of lpData)
        For i = histArray.Count - 1 To 0 Step -1
            If histArray(i) = 0 Then newList.Add(lines.lpList(i))
        Next

        If src.Channels = 1 Then lines.Run(src) Else lines.Run(task.grayStable.Clone)

        histArray = getLineCounts(task.lines.lpList)
        For i = histArray.Count - 1 To 1 Step -1
            If histArray(i) Then
                newList.Add(task.lines.lpList(i)) ' Add the lines in the motion mask.
            End If
        Next

        dst3.SetTo(0)
        For Each lp In newList
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, cv.LineTypes.Link4)
        Next

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each lp In newList
            If lp.length > 0 Then sortlines.Add(lp.length, lp)
        Next

        task.lines.lpList.Clear()
        ' placeholder for zero so we can distinguish line 1 from the background which is 0.
        task.lines.lpList.Add(New lpData(New cv.Point, New cv.Point))

        dst2 = src
        For Each lp In sortlines.Values
            task.lines.lpList.Add(lp)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next

        labels(2) = CStr(task.lines.lpList.Count) + " lines were found."
        labels(3) = CStr(lines.lpList.Count) + " lines were in the motion mask."
    End Sub
End Class






Public Class XO_FeatureLine_Basics : Inherits TaskParent
    Dim match As New Match_Basics
    Public cameraMotionProxy As New lpData
    Public gravityRGB As lpData
    Dim matchRuns As Integer, lineRuns As Integer, totalLineRuns As Integer
    Public runOnEachFrame As Boolean
    Public gravityMatch As New XO_Line_MatchGravity
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find and track the longest line by matching line bricks."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then task.lines.lpList.Clear()

        If matchRuns > 500 Then
            Dim percent = lineRuns / matchRuns
            lineRuns = 10
            matchRuns = lineRuns / percent
        End If

        Dim index = task.grid.gridMap.Get(Of Integer)(cameraMotionProxy.p1.Y, cameraMotionProxy.p1.X)
        Dim firstRect = task.gridNabeRects(index)
        index = task.grid.gridMap.Get(Of Integer)(cameraMotionProxy.p2.Y, cameraMotionProxy.p2.X)
        Dim lastRect = task.gridNabeRects(index)

        dst2 = src.Clone
        If task.lines.lpList.Count > 0 Then
            matchRuns += 1
            cameraMotionProxy = task.lines.lpList(0)

            Dim matchInput As New cv.Mat
            cv.Cv2.HConcat(src(firstRect), src(lastRect), matchInput)

            match.Run(matchInput)

            labels(2) = "Line end point correlation:  " + Format(match.correlation, fmt3) + " / " +
                        " with " + Format(lineRuns / matchRuns, "0%") + " requiring line detection.  " +
                        "line detection runs = " + CStr(totalLineRuns)
        End If

        If task.heartBeatLT Or task.lines.lpList.Count = 0 Or match.correlation < 0.98 Or runOnEachFrame Then
            task.motionMask.SetTo(255) ' force a complete line detection
            task.lines.Run(src.Clone)
            If task.lines.lpList.Count = 0 Then Exit Sub

            cameraMotionProxy = task.lines.lpList(0)
            lineRuns += 1
            totalLineRuns += 1

            Dim matchTemplate As New cv.Mat
            cv.Cv2.HConcat(src(firstRect), src(lastRect), matchTemplate)
            match.template = matchTemplate.Clone
        End If

        labels(3) = "Currently available lines."
        dst3 = task.lines.dst3
        labels(3) = task.lines.labels(3)

        gravityMatch.Run(src)
        If gravityMatch.gLines.Count > 0 Then gravityRGB = gravityMatch.gLines(0)

        dst2.Rectangle(firstRect, task.highlight, task.lineWidth)
        dst2.Rectangle(lastRect, task.highlight, task.lineWidth)
        dst2.Line(cameraMotionProxy.p1, cameraMotionProxy.p2, task.highlight, task.lineWidth, task.lineType)
        dst2.Line(task.lineGravity.ep1, task.lineGravity.ep2, task.highlight, task.lineWidth, task.lineType)
    End Sub
End Class







Public Class XO_MiniCloud_Basics : Inherits TaskParent
    Dim resize As Resize_Smaller
    Public rect As cv.Rect
    Public options As New Options_IMU
    Public Sub New()
        resize = New Resize_Smaller
        OptionParent.FindSlider("LowRes %").Value = 25
        desc = "Create a mini point cloud for use with histograms"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        resize.Run(task.pointCloud)

        Dim split = resize.dst2.Split()
        split(2).SetTo(0, task.noDepthMask.Resize(split(2).Size))
        rect = New cv.Rect(0, 0, resize.dst2.Width, resize.dst2.Height)
        If rect.Height < dst2.Height / 2 Then rect.Y = dst2.Height / 4 ' move it below the dst2 caption
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst2(rect) = split(2).ConvertScaleAbs(255)
        dst2.Rectangle(rect, white, 1)
        cv.Cv2.Merge(split, dst3)
        labels(2) = "MiniPC is " + CStr(rect.Width) + "x" + CStr(rect.Height) + " total pixels = " + CStr(rect.Width * rect.Height)
    End Sub
End Class








Public Class XO_MiniCloud_Rotate : Inherits TaskParent
    Public mini As New XO_MiniCloud_Basics
    Public histogram As New cv.Mat
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(3) = "Side view after resize percentage - use Y-Axis slider to rotate image."
        desc = "Create a histogram for the mini point cloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static ySlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis (degrees)")

        Dim input = src
        mini.Run(input)
        input = mini.dst3
        task.accRadians.Y = ySlider.Value

        Dim cx As Double = 1, sx As Double = 0, cy As Double = 1, sy As Double = 0, cz As Double = 1, sz As Double = 0
        Dim gM(,) As Single = {{cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                               {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                               {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz}}
        '[cos(a) 0 -sin(a)]
        '[0      1       0]
        '[sin(a) 0   cos(a] rotate the point cloud around the y-axis.
        cy = Math.Cos(task.accRadians.Y * cv.Cv2.PI / 180)
        sy = Math.Sin(task.accRadians.Y * cv.Cv2.PI / 180)
        gM = {{gM(0, 0) * cy + gM(0, 1) * 0 + gM(0, 2) * sy}, {gM(0, 0) * 0 + gM(0, 1) * 1 + gM(0, 2) * 0}, {gM(0, 0) * -sy + gM(0, 1) * 0 + gM(0, 2) * cy},
              {gM(1, 0) * cy + gM(1, 1) * 0 + gM(1, 2) * sy}, {gM(1, 0) * 0 + gM(1, 1) * 1 + gM(1, 2) * 0}, {gM(1, 0) * -sy + gM(1, 1) * 0 + gM(1, 2) * cy},
              {gM(2, 0) * cy + gM(2, 1) * 0 + gM(2, 2) * sy}, {gM(2, 0) * 0 + gM(2, 1) * 1 + gM(2, 2) * 0}, {gM(2, 0) * -sy + gM(2, 1) * 0 + gM(2, 2) * cy}}

        Dim gMat = cv.Mat.FromPixelData(3, 3, cv.MatType.CV_32F, gM)
        Dim gInput = input.Reshape(1, input.Rows * input.Cols)
        Dim gOutput = (gInput * gMat).ToMat
        input = gOutput.Reshape(3, input.Rows)

        Dim split = input.Split()
        Dim mask = split(2).Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        input.SetTo(0, mask.ConvertScaleAbs(255)) ' remove zero depth pixels with non-zero x and y.

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange), New cv.Rangef(0, task.MaxZmeters)}
        cv.Cv2.CalcHist({input}, {1, 2}, New cv.Mat, histogram, 2, {input.Height, input.Width}, ranges)

        dst2(mini.rect) = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        dst3(mini.rect) = input.ConvertScaleAbs(255)
    End Sub
End Class








Public Class XO_MiniCloud_RotateAngle : Inherits TaskParent
    Dim peak As New XO_MiniCloud_Rotate
    Dim mats As New Mat_4to1
    Public plot As New Plot_OverTimeSingle
    Dim resetCheck As System.Windows.Forms.CheckBox
    Public Sub New()
        task.accRadians.Y = -cv.Cv2.PI / 2

        labels(2) = "peak dst2, peak dst3, changed mask, maxvalues history"
        labels(3) = "Blue is maxVal, green is mean * 100"
        desc = "Find a peak value in the side view histograms"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then
            peak.mini.Run(src)
            src = peak.mini.dst3
        End If

        Static ySlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis (degrees)")
        If ySlider.Value + 1 >= ySlider.maximum Then ySlider.Value = ySlider.minimum Else ySlider.Value += 1

        peak.Run(src)
        Dim mm As mmData = GetMinMax(peak.histogram)

        Dim mean = peak.histogram.Mean()(0) * 100
        Dim mask = peak.histogram.Threshold(mean, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        mats.mat(2) = mask

        plot.plotData = New cv.Scalar(mm.maxVal)
        plot.Run(src)
        dst3 = plot.dst2
        labels(3) = "Histogram maxVal = " + Format(mm.maxVal, fmt1) + " histogram mean = " + Format(mean, fmt1)
        mats.mat(3) = peak.histogram.ConvertScaleAbs(255)

        mats.mat(0) = peak.dst2(peak.mini.rect)
        mats.mat(1) = peak.dst3(peak.mini.rect)
        mats.Run(emptyMat)
        dst2 = mats.dst2
    End Sub
End Class









Public Class XO_MiniCloud_RotateSinglePass : Inherits TaskParent
    Dim peak As New XO_MiniCloud_Rotate
    Public Sub New()
        task.accRadians.Y = -cv.Cv2.PI
        desc = "Same operation as New MiniCloud_RotateAngle but in a single pass."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static ySlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis (degrees)")
        peak.mini.Run(src)

        Dim maxHist = Single.MinValue
        Dim bestAngle As Integer
        Dim bestLoc As cv.Point
        Dim mm As mmData
        For i = ySlider.minimum To ySlider.maximum - 1
            peak.Run(peak.mini.dst3)
            ySlider.Value = i
            mm = GetMinMax(peak.histogram)
            If mm.maxVal > maxHist Then
                maxHist = mm.maxVal
                bestAngle = i
                bestLoc = mm.maxLoc
            End If
        Next
        peak.Run(peak.mini.dst3)
        task.accRadians.Y = bestAngle
        dst2 = peak.dst2
        dst3 = peak.dst3

        SetTrueText("Peak concentration in the histogram is at angle " + CStr(bestAngle) + " degrees", 3)
    End Sub
End Class




Public Class XO_OpAuto_XRange : Inherits TaskParent
    Public histogram As New cv.Mat
    Dim adjustedCount As Integer = 0
    Public Sub New()
        labels(2) = "Optimized top view to show as many samples as possible."
        desc = "Automatically adjust the X-Range option of the pointcloud to maximize visible pixels"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim expectedCount = task.depthMask.CountNonZero

        Dim diff = Math.Abs(expectedCount - adjustedCount)

        ' the input is a histogram.  If standaloneTest(), go get one...
        If standaloneTest() Then
            cv.Cv2.CalcHist({task.pointCloud}, task.channelsTop, New cv.Mat, histogram, 2, task.bins2D, task.rangesTop)
            histogram.Row(0).SetTo(0)
            dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            dst3 = histogram.Threshold(task.projectionThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            src = histogram
        End If

        histogram = src
        adjustedCount = histogram.Sum()(0)

        strOut = "Adjusted = " + vbTab + CStr(adjustedCount) + "k" + vbCrLf +
                 "Expected = " + vbTab + CStr(expectedCount) + "k" + vbCrLf +
                 "Diff = " + vbTab + vbTab + CStr(diff) + vbCrLf +
                 "xRange = " + vbTab + Format(task.xRange, fmt3)

        If task.useXYRange Then
            Dim saveOptionState = task.optionsChanged ' the xRange and yRange change frequently.  It is safe to ignore it.
            Dim leftGap = histogram.Col(0).CountNonZero
            Dim rightGap = histogram.Col(histogram.Width - 1).CountNonZero
            'If leftGap = 0 And rightGap = 0 And task.gOptions.XRangeBar.Value > 3 Then
            '    task.gOptions.XRangeBar.Value -= 1
            'Else
            '    If adjustedCount < expectedCount Then task.gOptions.XRangeBar.Value += 1 Else task.gOptions.XRangeBar.Value -= 1
            'End If
            task.optionsChanged = saveOptionState
        End If

        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class XO_OpAuto_YRange : Inherits TaskParent
    Public histogram As New cv.Mat
    Dim adjustedCount As Integer = 0
    Public Sub New()
        labels(2) = "Optimized side view to show as much as possible."
        desc = "Automatically adjust the Y-Range option of the pointcloud to maximize visible pixels"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim expectedCount = task.depthMask.CountNonZero

        Dim diff = Math.Abs(expectedCount - adjustedCount)

        ' the input is a histogram.  If standaloneTest(), go get one...
        If standaloneTest() Then
            cv.Cv2.CalcHist({task.pointCloud}, task.channelsSide, New cv.Mat, histogram, 2, task.bins2D, task.rangesSide)
            histogram.Col(0).SetTo(0)
            dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            dst3 = histogram.Threshold(task.projectionThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            src = histogram
        End If

        histogram = src
        adjustedCount = histogram.Sum()(0)

        strOut = "Adjusted = " + vbTab + CStr(adjustedCount) + "k" + vbCrLf +
                 "Expected = " + vbTab + CStr(expectedCount) + "k" + vbCrLf +
                 "Diff = " + vbTab + vbTab + CStr(diff) + vbCrLf +
                 "yRange = " + vbTab + Format(task.yRange, fmt3)

        If task.useXYRange Then
            Dim saveOptionState = task.optionsChanged ' the xRange and yRange change frequently.  It is safe to ignore it.
            Dim topGap = histogram.Row(0).CountNonZero
            Dim botGap = histogram.Row(histogram.Height - 1).CountNonZero
            'If topGap = 0 And botGap = 0 And task.gOptions.YRangeSlider.Value > 3 Then
            '    task.gOptions.YRangeSlider.Value -= 1
            'Else
            '    If adjustedCount < expectedCount Then task.gOptions.YRangeSlider.Value += 1 Else task.gOptions.YRangeSlider.Value -= 1
            'End If
            task.optionsChanged = saveOptionState
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class XO_Mat_ToList : Inherits TaskParent
    Dim autoX As New XO_OpAuto_XRange
    Dim histTop As New Projection_HistTop
    Public Sub New()
        desc = "Convert a Mat to List of points in 2 ways to measure which is better"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        histTop.Run(src)

        autoX.Run(histTop.histogram)
        dst2 = histTop.histogram.Threshold(task.projectionThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs

        Dim ptList As New List(Of cv.Point)
        If task.gOptions.DebugCheckBox.Checked Then
            For y = 0 To dst2.Height - 1
                For x = 0 To dst2.Width - 1
                    If dst2.Get(Of Byte)(y, x) <> 0 Then ptList.Add(New cv.Point(x, y))
                Next
            Next
        Else
            Dim points = dst2.FindNonZero()
            For i = 0 To points.Rows - 1
                ptList.Add(points.Get(Of cv.Point)(i, 0))
            Next
        End If

        labels(2) = "There were " + CStr(ptList.Count) + " points identified"
    End Sub
End Class










Public Class XO_RedPrep_BasicsCalcHist : Inherits TaskParent
    Public Sub New()
        desc = "Simpler transforms for the point cloud using CalcHist instead of reduction."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat

        Dim channels() As Integer = {0}
        Select Case task.reductionName
            Case "X Reduction"
                dst0 = task.pcSplit(0)
            Case "Y Reduction"
                dst0 = task.pcSplit(1)
            Case "Z Reduction"
                dst0 = task.pcSplit(2)
            Case "XY Reduction"
                dst0 = task.pcSplit(0) + task.pcSplit(1)
                channels = {0, 1}
            Case "XZ Reduction"
                dst0 = task.pcSplit(0) + task.pcSplit(2)
                channels = {0, 1}
            Case "YZ Reduction"
                dst0 = task.pcSplit(1) + task.pcSplit(2)
                channels = {0, 1}
            Case "XYZ Reduction"
                dst0 = task.pcSplit(0) + task.pcSplit(1) + task.pcSplit(2)
                channels = {0, 1}
        End Select

        Dim mm = GetMinMax(dst0)
        Dim ranges = New cv.Rangef() {New cv.Rangef(mm.minVal, mm.maxVal)}
        cv.Cv2.CalcHist({dst0}, channels, task.depthMask, histogram, 1, {task.histogramBins}, ranges)

        Dim histArray(histogram.Total - 1) As Single
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

        For i = 0 To histArray.Count - 1
            histArray(i) = i
        Next

        histogram = cv.Mat.FromPixelData(histogram.Rows, 1, cv.MatType.CV_32F, histArray)
        cv.Cv2.CalcBackProject({dst0}, {0}, histogram, dst1, ranges)
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)
        dst3 = ShowPalette(dst2)
        dst3.SetTo(0, task.noDepthMask)

        labels(2) = "Pointcloud data backprojection to " + CStr(task.histogramBins) + " classes."
    End Sub
End Class






Public Class XO_Contour_Depth : Inherits TaskParent
    Dim options As New Options_Contours
    Public depthContourList As New List(Of contourData)
    Public depthcontourMap As New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
    Dim sortContours As New Contour_Sort
    Dim prep As New RedPrep_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(3) = "ShowPalette output of the depth contours in dst2"
        desc = "Isolate the contours in the output of BackProject_Basics_Depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim mode = options.options2.ApproximationMode
        prep.Run(src)
        dst3.ConvertTo(dst1, cv.MatType.CV_32SC1)
        cv.Cv2.FindContours(dst1, sortContours.allContours, Nothing, cv.RetrievalModes.FloodFill, mode)
        If sortContours.allContours.Count <= 1 Then Exit Sub

        sortContours.Run(src)

        depthContourList = sortContours.contourList
        depthcontourMap = sortContours.contourMap
        labels(2) = sortContours.labels(2)
        dst2 = sortContours.dst2

        dst2.SetTo(0)
        For i = 0 To Math.Min(depthContourList.Count, 6) - 1
            Dim contour = depthContourList(i)
            dst2(contour.rect).SetTo(contour.ID Mod 255, contour.mask)
            Dim str = CStr(contour.ID) + " ID" + vbCrLf + CStr(contour.pixels) + " pixels" + vbCrLf +
                      Format(contour.depth, fmt3) + "m depth" + vbCrLf + Format(contour.mm.range, fmt3) + " range in m"
            SetTrueText(str, contour.maxDist, 2)
        Next

        Static saveTrueData As New List(Of TrueText)
        If task.heartBeatLT Then
            saveTrueData = New List(Of TrueText)(trueData)
        Else
            trueData = New List(Of TrueText)(saveTrueData)
        End If

        dst3 = ShowPalette(dst2)
        labels(2) = "CV_8U format of the " + CStr(depthContourList.Count) + " depth contours"
    End Sub
End Class





Public Class XO_TrackLine_Basics : Inherits TaskParent
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        task.gOptions.TrackingColor.Checked = True
        desc = "Track the line regions with RedCloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1.SetTo(0)
        For Each lp In task.lines.lpList
            dst1.Line(lp.p1, lp.p2, 255, task.lineWidth + 1, cv.LineTypes.Link8)
        Next

        dst2 = runRedC(dst1, labels(2), Not dst1)

        dst3.SetTo(0)
        For Each lp In task.lines.lpList
            DrawLine(dst3, lp.p1, lp.p2, white, task.lineWidth)
            Dim center = New cv.Point(CInt((lp.p1.X + lp.p2.X) / 2), CInt((lp.p1.Y + lp.p2.Y) / 2))
            DrawCircle(dst3, center, task.DotSize, task.highlight, -1)
        Next
    End Sub
End Class






Public Class XO_TrackLine_Map : Inherits TaskParent
    Dim lTrack As New XO_TrackLine_Basics
    Public Sub New()
        task.needBricks = True
        task.gOptions.CrossHairs.Checked = False
        desc = "Show the gridMap and fpMap (features points) "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lTrack.Run(src)
        dst2 = lTrack.dst2
        dst1 = lTrack.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels(2) = lTrack.labels(2)

        Dim count As Integer
        dst3.SetTo(0)
        Dim histarray(task.redC.rcList.Count - 1) As Single
        Dim histogram As New cv.Mat
        For Each brick In task.bricks.brickList
            cv.Cv2.CalcHist({task.redC.rcMap(brick.rect)}, {0}, emptyMat, histogram, 1, {task.redC.rcList.Count},
                             New cv.Rangef() {New cv.Rangef(1, task.redC.rcList.Count)})

            Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)
            ' if multiple lines intersect a grid rect, choose the largest redcloud cell containing them.
            ' The largest will be the index of the first non-zero histogram entry.
            For j = 1 To histarray.Count - 1
                If histarray(j) > 0 Then
                    Dim rc = task.redC.rcList(j)
                    dst3(brick.rect).SetTo(rc.color)
                    ' dst3(brick.rect).SetTo(0, Not dst1(brick.rect))
                    count += 1
                    Exit For
                End If
            Next
        Next

        labels(3) = "The redCloud cells are completely covered by " + CStr(count) + " bricks"
    End Sub
End Class





Public Class XO_TrackLine_BasicsSimple : Inherits TaskParent
    Dim lp As New lpData
    Dim match As New Match_Basics
    Public rawLines As New Line_Raw
    Dim matchRect As cv.Rect
    Public Sub New()
        desc = "Track an individual line as best as possible."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lines.lpList
        If lplist.Count = 0 Then Exit Sub

        If standalone Then
            If lplist(0).length > lp.length Then
                lp = lplist(0)
                matchRect = lp.rect
                match.template = src(matchRect).Clone
            End If
        End If

        If matchRect.Width <= 1 Then Exit Sub ' nothing yet...
        match.Run(src)
        matchRect = match.newRect

        If standaloneTest() Then
            dst2 = src
            DrawCircle(dst2, match.newCenter, task.DotSize, white)
            dst2.Rectangle(matchRect, task.highlight, task.lineWidth)
            dst3 = match.dst0.Normalize(0, 255, cv.NormTypes.MinMax)
            SetTrueText(Format(match.correlation, fmt3), match.newCenter)
        End If

        rawLines.Run(src(matchRect))
        If rawLines.lpList.Count > 0 Then lp = rawLines.lpList(0)
        dst2(matchRect).Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
    End Sub
End Class





Public Class XO_TrackLine_BasicsOld : Inherits TaskParent
    Public lpInput As lpData
    Public foundLine As Boolean
    Dim match As New XO_MatchLine_Basics
    Public rawLines As New Line_Raw
    Public Sub New()
        desc = "Track an individual line as best as possible."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lines.lpList
        If lplist.Count = 0 Then Exit Sub
        If standalone And foundLine = False Then lpInput = task.lineLongest

        Static subsetrect = lpInput.rect
        If subsetrect.width <= dst2.Height / 10 Then
            lpInput = task.lineLongest
            subsetrect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
            Exit Sub
        End If

        Dim lpLast = lpInput

        Dim index = task.lines.lineCore.lpRectMap.Get(Of Byte)(lpInput.center.Y, lpInput.center.X)
        If index > 0 Then
            Dim lp = lplist(index - 1)
            If lpInput.index = lp.index Then
                foundLine = True
            Else
                match.lpInput = lpInput
                match.Run(src)

                foundLine = match.correlation1 >= task.fCorrThreshold And match.correlation2 >= task.fCorrThreshold
                If foundLine Then
                    lpInput = match.lpOutput
                    subsetrect = lpInput.rect
                End If
            End If
        Else
            rawLines.Run(src(subsetrect))
            dst3(subsetrect) = rawLines.dst2(subsetrect)
            If rawLines.lpList.Count > 0 Then
                Dim p1 = New cv.Point(CInt(rawLines.lpList(0).p1.X + subsetrect.X), CInt(rawLines.lpList(0).p1.Y + subsetrect.Y))
                Dim p2 = New cv.Point(CInt(rawLines.lpList(0).p2.X + subsetrect.X), CInt(rawLines.lpList(0).p2.Y + subsetrect.Y))
                lpInput = New lpData(p1, p2)
            Else
                lpInput = lplist(0)
            End If

            Dim deltaX1 = Math.Abs(task.gravityIMU.ep1.X - lpInput.ep1.X)
            Dim deltaX2 = Math.Abs(task.gravityIMU.ep2.X - lpInput.ep2.X)
            If Math.Abs(deltaX1 - deltaX2) > task.gravityBasics.options.pixelThreshold Then
                lpInput = task.lineLongest
            End If
            subsetrect = lpInput.rect
        End If

        dst2 = src
        dst2.Line(lpInput.p1, lpInput.p2, task.highlight, task.lineWidth + 1, task.lineType)
        dst2.Rectangle(subsetrect, task.highlight, task.lineWidth)
    End Sub
End Class





Public Class XO_TrackLine_BasicsSave : Inherits TaskParent
    Dim match As New Match_Basics
    Dim matchRect As cv.Rect
    Public rawLines As New Line_Raw
    Dim lplist As List(Of lpData)
    Dim knn As New KNN_NNBasics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        OptionParent.FindSlider("KNN Dimension").Value = 6
        desc = "Track an individual line as best as possible."
    End Sub
    Private Function restartLine(src As cv.Mat) As lpData
        For Each lpTemp In lplist
            If Math.Abs(task.lineGravity.angle - lpTemp.angle) < task.angleThreshold Then
                matchRect = lpTemp.rect
                match.template = src(matchRect).Clone
                Return lpTemp
            End If
        Next
        Return New lpData
    End Function
    Private Sub prepEntry(knnList As List(Of Single), lpNext As lpData)
        Dim brick1 = task.grid.gridMap.Get(Of Integer)(lpNext.p1.Y, lpNext.p1.X)
        Dim brick2 = task.grid.gridMap.Get(Of Integer)(lpNext.p2.Y, lpNext.p2.X)
        knnList.Add(lpNext.p1.X)
        knnList.Add(lpNext.p1.Y)
        knnList.Add(lpNext.p2.X)
        knnList.Add(lpNext.p2.Y)
        knnList.Add(brick1)
        knnList.Add(brick2)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lplist = task.lines.lpList
        If lplist.Count = 0 Then Exit Sub

        Static lp As New lpData, lpLast As lpData
        lpLast = lp

        If match.correlation < task.fCorrThreshold Or matchRect.Width <= 1 Then ' Or task.heartBeatLT 
            lp = restartLine(src)
        End If

        match.Run(src)

        knn.trainInput.Clear()
        For Each nextlp In task.lines.lpList
            prepEntry(knn.trainInput, nextlp)
        Next

        knn.queries.Clear()
        prepEntry(knn.queries, lp)
        knn.Run(emptyMat)

        lp = task.lines.lpList(knn.result(0, 0))
        labels(3) = "Index of the current lp = " + CStr(lp.index - 1)

        If standaloneTest() Then
            dst2 = src.Clone
            DrawCircle(dst2, match.newCenter, task.DotSize, white)
            dst2.Rectangle(lp.rect, task.highlight, task.lineWidth)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            dst3 = match.dst0.Normalize(0, 255, cv.NormTypes.MinMax)
            SetTrueText(Format(match.correlation, fmt3), match.newCenter)

            dst2.Rectangle(lp.rect, task.highlight, task.lineWidth)
        End If

        dst1 = ShowPaletteNoZero(task.lines.lineCore.lpRectMap)
        dst1.Circle(lp.center, task.DotSize, task.highlight, task.lineWidth, task.lineType)

        labels(2) = "Selected line has a correlation of " + Format(match.correlation, fmt3) + " with the previous frame."
    End Sub
End Class






Public Class XO_BrickPoint_VetLines : Inherits TaskParent
    Dim bPoint As New BrickPoint_Basics
    Public lpList As New List(Of lpData)
    Public Sub New()
        desc = "Vet the lines - make sure there are at least 2 brickpoints in the line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        dst3 = src

        bPoint.Run(src.Clone)

        Dim pointsPerLine(task.gridRects.Count) As List(Of Integer)
        For Each pt In bPoint.ptList
            Dim index = task.lines.lineCore.lpRectMap.Get(Of Byte)(pt.Y, pt.X)
            If index > 0 And index < task.lines.lpList.Count Then
                Dim lp = task.lines.lpList(index)
                If pointsPerLine(lp.index) Is Nothing Then pointsPerLine(lp.index) = New List(Of Integer)
                pointsPerLine(lp.index).Add(lp.index)
                dst2.Circle(pt, task.DotSize * 3, task.scalarColors(lp.index Mod 255), -1, task.lineType)
            End If
        Next

        lpList.Clear()
        For Each ppl In pointsPerLine
            If ppl Is Nothing Then Continue For
            If ppl.Count > 1 Then lpList.Add(task.lines.lpList(ppl(0)))
        Next

        dst3 = src
        For Each lp In lpList
            dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next
        labels(3) = CStr(lpList.Count) + " lines were confirmed with brickpoints"
    End Sub
End Class




Public Class XO_Gravity_Basics1 : Inherits TaskParent
    Public options As New Options_Features
    Dim gravityRaw As New Gravity_Raw
    Public gravityMatch As New XO_Line_MatchGravity
    Public gravityRGB As lpData
    Dim nearest As New XO_Line_FindNearest
    Public Sub New()
        desc = "Use the slope of the longest RGB line to figure out if camera moved enough to obtain the IMU gravity vector."
    End Sub
    Private Shared Sub showVec(dst As cv.Mat, vec As lpData)
        dst.Line(vec.p1, vec.p2, task.highlight, task.lineWidth * 2, task.lineType)
        Dim gIndex = task.grid.gridMap.Get(Of Integer)(vec.p1.Y, vec.p1.X)
        Dim firstRect = task.gridNabeRects(gIndex)
        gIndex = task.grid.gridMap.Get(Of Integer)(vec.p2.Y, vec.p2.X)
        Dim lastRect = task.gridNabeRects(gIndex)
        dst.Rectangle(firstRect, task.highlight, task.lineWidth)
        dst.Rectangle(lastRect, task.highlight, task.lineWidth)
    End Sub
    Public Shared Sub showVectors(dst As cv.Mat)
        dst.Line(task.lineGravity.p1, task.lineGravity.p2, white, task.lineWidth, task.lineType)
        dst.Line(task.lineHorizon.p1, task.lineHorizon.p2, white, task.lineWidth, task.lineType)
        showVec(dst, task.lineLongest)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        gravityRaw.Run(emptyMat)
        gravityMatch.Run(src)
        labels(2) = CStr(gravityMatch.gLines.Count) + " of the lines found were parallel to gravity."

        Static RGBcandidate As New lpData

        Dim stillPresent As Integer
        If RGBcandidate.length = 0 Then
            If gravityMatch.gLines.Count > 0 Then RGBcandidate = gravityMatch.gLines(0)
        Else
            stillPresent = task.lines.lineCore.lpRectMap.Get(Of Byte)(RGBcandidate.center.Y, RGBcandidate.center.X)
        End If

        If stillPresent Then
            nearest.lpInput = RGBcandidate
            nearest.Run(src)
            RGBcandidate = nearest.lpOutput
            Dim deltaX1 = Math.Abs(task.lineGravity.ep1.X - RGBcandidate.ep1.X)
            Dim deltaX2 = Math.Abs(task.lineGravity.ep2.X - RGBcandidate.ep2.X)
            If Math.Abs(deltaX1 - deltaX2) > options.pixelThreshold Then
                task.lineGravity = task.gravityIMU
                RGBcandidate = New lpData
                If gravityMatch.gLines.Count > 0 Then RGBcandidate = gravityMatch.gLines(0)
            End If
        Else
            task.lineGravity = task.gravityIMU
            RGBcandidate = New lpData
            If gravityMatch.gLines.Count > 0 Then RGBcandidate = gravityMatch.gLines(0)
        End If

        task.lineHorizon = Line_PerpendicularTest.computePerp(task.lineGravity)

        gravityRGB = RGBcandidate

        If standaloneTest() Then
            dst2.SetTo(0)
            showVectors(dst2)
            dst3 = task.lines.dst3
            labels(3) = task.lines.labels(3)
        End If
    End Sub
End Class




Public Class XO_FPoly_Basics : Inherits TaskParent
    Public resync As Boolean
    Public resyncCause As String
    Public resyncFrames As Integer
    Public maskChangePercent As Single
    Dim feat As New XO_FPoly_TopFeatures
    Public sides As New XO_FPoly_Sides
    Dim options As New Options_Features
    Public Sub New()
        task.featureOptions.FeatureSampleSize.Value = 30
        If dst2.Width >= 640 Then OptionParent.FindSlider("Resync if feature moves > X pixels").Value = 15
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "Feature Polygon with perpendicular lines for center of rotation.", "Feature polygon created by highest generation counts",
                  "Ordered Feature polygons of best features - white is original, yellow latest"}
        desc = "Build a Feature polygon with the top generation counts of the good features"
    End Sub
    Public Shared Sub DrawFPoly(ByRef dst As cv.Mat, poly As List(Of cv.Point2f), color As cv.Scalar)
        Dim minMod = Math.Min(poly.Count, task.polyCount)
        For i = 0 To minMod - 1
            DrawLine(dst, poly(i), poly((i + 1) Mod minMod), color)
        Next
    End Sub
    Public Shared Sub DrawPolys(dst As cv.Mat, poly As fPolyData)
        DrawFPoly(dst, poly.prevPoly, cv.Scalar.White)
        DrawFPoly(dst, poly.currPoly, cv.Scalar.Yellow)
        dst.Line(poly.currPoly(poly.polyPrevSideIndex), poly.currPoly((poly.polyPrevSideIndex + 1) Mod task.polyCount),
                 task.highlight, task.lineWidth * 3, task.lineType)
        dst.Line(poly.prevPoly(poly.polyPrevSideIndex), poly.prevPoly((poly.polyPrevSideIndex + 1) Mod task.polyCount),
                 task.highlight, task.lineWidth * 3, task.lineType)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.firstPass Then sides.prevImage = src.Clone
        sides.options.Run()

        feat.Run(src)
        dst2 = src.Clone
        sides.currPoly = New List(Of cv.Point2f)(feat.topFeatures)
        If sides.currPoly.Count < task.polyCount Then Exit Sub
        sides.Run(src)
        dst3 = sides.dst2

        For i = 0 To sides.currPoly.Count - 1
            SetTrueText(CStr(i), sides.currPoly(i), 3)
            DrawLine(dst2, sides.currPoly(i), sides.currPoly((i + 1) Mod sides.currPoly.Count))
        Next

        Dim causes As String = ""
        If Math.Abs(sides.rotateAngle * 57.2958) > 10 Then
            resync = True
            causes += " - Rotation angle exceeded threshold."
            sides.rotateAngle = 0
        End If
        causes += vbCrLf

        If task.optionsChanged Then
            resync = True
            causes += " - Options changed"
        End If
        causes += vbCrLf

        If resyncFrames > sides.options.autoResyncAfterX Then
            resync = True
            causes += " - More than " + CStr(sides.options.autoResyncAfterX) + " frames without resync"
        End If
        causes += vbCrLf

        If Math.Abs(sides.currLengths.Sum() - sides.prevLengths.Sum()) > sides.options.removeThreshold * task.polyCount Then
            resync = True
            causes += " - The top " + CStr(task.polyCount) + " vertices have moved because of the generation counts"
        Else
            If Math.Abs(sides.prevFLineLen - sides.currFLineLen) > sides.options.removeThreshold Then
                resync = True
                causes += " - The Feature polygon's longest side (FLine) changed more than the threshold of " +
                              CStr(sides.options.removeThreshold) + " pixels"
            End If
        End If
        causes += vbCrLf

        If resync Or sides.prevPoly.Count <> task.polyCount Or task.optionsChanged Then
            sides.prevPoly = New List(Of cv.Point2f)(sides.currPoly)
            sides.prevLengths = New List(Of Single)(sides.currLengths)
            sides.prevSideIndex = sides.prevLengths.IndexOf(sides.prevLengths.Max)
            sides.prevImage = src.Clone
            resyncFrames = 0
            resyncCause = causes
        End If
        resyncFrames += 1

        strOut = "Rotation: " + Format(sides.rotateAngle * 57.2958, fmt1) + " degrees" + vbCrLf
        strOut += "Translation: " + CStr(CInt(sides.centerShift.X)) + ", " + CStr(CInt(sides.centerShift.Y)) + vbCrLf
        strOut += "Frames since last resync: " + Format(resyncFrames, "000") + vbCrLf + vbCrLf
        strOut += "Resync last caused by: " + vbCrLf + resyncCause

        For Each pt In sides.currPoly ' topFeatures.stable.goodCounts
            Dim index = feat.stable.basics.ptList.IndexOf(pt)
            If index >= 0 Then
                pt = feat.stable.basics.ptList(index)
                Dim g = feat.stable.basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                SetTrueText(CStr(g), pt)
            End If
        Next

        SetTrueText(strOut, 1)
        resync = False
    End Sub
End Class







Public Class XO_FPoly_Sides : Inherits TaskParent
    Public currPoly As New List(Of cv.Point2f)
    Public currSideIndex As Integer
    Public currLengths As New List(Of Single)
    Public currFLineLen As Single
    Public mpCurr As lpData

    Public prevPoly As New List(Of cv.Point2f)
    Public prevSideIndex As Integer
    Public prevLengths As New List(Of Single)
    Public prevFLineLen As Single
    Public mpPrev As lpData

    Public prevImage As cv.Mat

    Public rotateCenter As cv.Point2f
    Public rotateAngle As Single
    Public centerShift As cv.Point2f

    Public options As New Options_FPoly
    Dim near As New XO_Line_Nearest
    Public rotatePoly As New XO_Rotate_PolyQT
    Dim newPoly As New List(Of cv.Point2f)
    Dim random As New Random_Basics
    Public Sub New()
        labels(2) = "White is the original FPoly and yellow is the current FPoly."
        desc = "Compute the lengths of each side in a polygon"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.firstPass Then prevImage = src.Clone
        options.Run()

        If standaloneTest() And task.heartBeat Then
            random.Run(src)
            currPoly = New List(Of cv.Point2f)(random.PointList)
        End If

        dst2.SetTo(0)
        currLengths.Clear()
        For i = 0 To currPoly.Count - 2
            currLengths.Add(currPoly(i).DistanceTo(currPoly(i + 1)))
        Next
        currSideIndex = currLengths.IndexOf(currLengths.Max)

        If task.firstPass Then
            prevPoly = New List(Of cv.Point2f)(currPoly)
            prevLengths = New List(Of Single)(currLengths)
            prevSideIndex = prevLengths.IndexOf(prevLengths.Max)
        End If

        If prevPoly.Count = 0 Then Exit Sub

        mpPrev = New lpData(prevPoly(prevSideIndex), prevPoly((prevSideIndex + 1) Mod task.polyCount))
        mpCurr = New lpData(currPoly(currSideIndex), currPoly((currSideIndex + 1) Mod task.polyCount))

        prevFLineLen = mpPrev.p1.DistanceTo(mpPrev.p2)
        currFLineLen = mpCurr.p1.DistanceTo(mpCurr.p2)

        Dim d1 = mpPrev.p1.DistanceTo(mpCurr.p1)
        Dim d2 = mpPrev.p2.DistanceTo(mpCurr.p2)

        Dim newNear As lpData
        If d1 < d2 Then
            centerShift = New cv.Point2f(mpPrev.p1.X - mpCurr.p1.X, mpPrev.p1.Y - mpCurr.p1.Y)
            rotateCenter = mpPrev.p1
            newNear = New lpData(mpPrev.p2, mpCurr.p2)
        Else
            centerShift = New cv.Point2f(mpPrev.p2.X - mpCurr.p2.X, mpPrev.p2.Y - mpCurr.p2.Y)
            rotateCenter = mpPrev.p2
            newNear = New lpData(mpPrev.p1, mpCurr.p1)
        End If

        Dim transPoly As New List(Of cv.Point2f)
        For i = 0 To currPoly.Count - 1
            transPoly.Add(New cv.Point2f(currPoly(i).X - centerShift.X, currPoly(i).Y - centerShift.Y))
        Next
        newNear.p1 = New cv.Point2f(newNear.p1.X - centerShift.X, newNear.p1.Y - centerShift.Y)
        newNear.p2 = New cv.Point2f(newNear.p2.X - centerShift.X, newNear.p2.Y - centerShift.Y)
        rotateCenter = New cv.Point2f(rotateCenter.X - centerShift.X, rotateCenter.Y - centerShift.Y)

        strOut = "No rotation" + vbCrLf
        rotateAngle = 0
        If d1 <> d2 Then
            If newNear.p1.DistanceTo(newNear.p2) > options.removeThreshold Then
                near.lp = mpPrev
                near.pt = newNear.p1
                near.Run(src)
                dst1.Line(near.pt, near.nearPoint, cv.Scalar.Red, task.lineWidth + 5, task.lineType)

                Dim hypotenuse = rotateCenter.DistanceTo(near.pt)
                rotateAngle = -Math.Asin(near.nearPoint.DistanceTo(near.pt) / hypotenuse)
                If Single.IsNaN(rotateAngle) Then rotateAngle = 0
                strOut = "Angle is " + Format(rotateAngle * 57.2958, fmt1) + " degrees" + vbCrLf
            End If
        End If
        strOut += "Translation (shift) is " + Format(-centerShift.X, fmt0) + ", " + Format(-centerShift.Y, fmt0)

        If Math.Abs(rotateAngle) > 0 Then
            rotatePoly.rotateCenter = rotateCenter
            rotatePoly.rotateAngle = rotateAngle
            rotatePoly.poly.Clear()
            rotatePoly.poly.Add(newNear.p1)
            rotatePoly.Run(src)

            If near.nearPoint.DistanceTo(rotatePoly.poly(0)) > newNear.p1.DistanceTo(rotatePoly.poly(0)) Then rotateAngle *= -1

            rotatePoly.rotateAngle = rotateAngle
            rotatePoly.poly = New List(Of cv.Point2f)(transPoly)
            rotatePoly.Run(src)
            newPoly = New List(Of cv.Point2f)(rotatePoly.poly)
        End If

        XO_FPoly_Basics.DrawFPoly(dst2, prevPoly, white)
        XO_FPoly_Basics.DrawFPoly(dst2, currPoly, cv.Scalar.Yellow)
        DrawFatLine(dst2, mpPrev, white)
        DrawFatLine(dst2, mpCurr, task.highlight)
    End Sub
End Class










Public Class XO_FPoly_BasicsOriginal : Inherits TaskParent
    Public fPD As New fPolyData
    Public resyncImage As cv.Mat
    Public resync As Boolean
    Public resyncCause As String
    Public resyncFrames As Integer
    Public maskChangePercent As Single

    Dim feat As New XO_FPoly_TopFeatures
    Public options As New Options_FPoly
    Public center As Object
    Dim optionsEx As New Options_Features
    Public Sub New()
        center = New XO_FPoly_Center
        task.featureOptions.FeatureSampleSize.Value = 30
        If dst2.Width >= 640 Then OptionParent.FindSlider("Resync if feature moves > X pixels").Value = 15
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "Feature Polygon with perpendicular lines for center of rotation.",
                      "Feature polygon created by highest generation counts",
                  "Ordered Feature polygons of best features - white is original, yellow latest"}
        desc = "Build a Feature polygon with the top generation counts of the good features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.firstPass Then resyncImage = src.Clone
        options.Run()
        optionsEx.Run()

        feat.Run(src)
        dst2 = feat.dst2
        dst1 = feat.dst3
        fPD.currPoly = New List(Of cv.Point2f)(feat.topFeatures)

        If task.optionsChanged Then fPD = New fPolyData(fPD.currPoly)
        If fPD.currPoly.Count < task.polyCount Then Exit Sub

        fPD.computeCurrLengths()
        For i = 0 To fPD.currPoly.Count - 1
            SetTrueText(CStr(i), fPD.currPoly(i), 1)
        Next
        If task.firstPass Then fPD.lengthPrevious = New List(Of Single)(fPD.currLength)

        center.fPD = fPD
        center.Run(src)
        fPD = center.fPD
        dst1 = (dst1 Or center.dst2).tomat
        dst0 = center.dst3

        fPD.jitterTest(dst2, Me) ' the feature line has not really moved.

        Dim causes As String = ""
        If Math.Abs(fPD.rotateAngle * 57.2958) > 10 Then
            resync = True
            causes += " - Rotation angle exceeded threshold."
            fPD.rotateAngle = 0
        End If
        causes += vbCrLf

        If maskChangePercent > 0.2 Then
            resync = True
            causes += " - Difference of startFrame and current frame exceeded 20% of image size"
        End If
        causes += vbCrLf

        If task.optionsChanged Then
            resync = True
            causes += " - Options changed"
        End If
        causes += vbCrLf

        If resyncFrames > options.autoResyncAfterX Then
            resync = True
            causes += " - More than " + CStr(options.autoResyncAfterX) + " frames without resync"
        End If
        causes += vbCrLf

        If Math.Abs(fPD.currLength.Sum() - fPD.lengthPrevious.Sum()) > options.removeThreshold * task.polyCount Then
            resync = True
            causes += " - The top " + CStr(task.polyCount) + " vertices have moved because of the generation counts"
        Else
            If fPD.computeFLineLength() > options.removeThreshold Then
                resync = True
                causes += " - The Feature polygon's longest side (FLine) changed more than the threshold of " +
                              CStr(options.removeThreshold) + " pixels"
            End If
        End If
        causes += vbCrLf

        If resync Or fPD.prevPoly.Count <> task.polyCount Or task.optionsChanged Then
            fPD.resync()
            resyncImage = src.Clone
            resyncFrames = 0
            resyncCause = causes
        End If
        resyncFrames += 1

        XO_FPoly_Basics.DrawFPoly(dst2, fPD.currPoly, white)
        XO_FPoly_Basics.DrawPolys(dst1, fPD)
        For i = 0 To fPD.prevPoly.Count - 1
            SetTrueText(CStr(i), fPD.currPoly(i), 1)
            SetTrueText(CStr(i), fPD.currPoly(i), 1)
        Next

        strOut = "Rotation: " + Format(fPD.rotateAngle * 57.2958, fmt1) + " degrees" + vbCrLf
        strOut += "Translation: " + CStr(CInt(fPD.centerShift.X)) + ", " + CStr(CInt(fPD.centerShift.Y)) + vbCrLf
        strOut += "Frames since last resync: " + Format(resyncFrames, "000") + vbCrLf
        strOut += "Last resync cause(s): " + vbCrLf + resyncCause

        For Each keyval In feat.stable.goodCounts
            Dim pt = feat.stable.basics.ptList(keyval.Value)
            Dim g = feat.stable.basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
            SetTrueText(CStr(g), pt)
        Next

        SetTrueText(strOut, 1)
        dst3 = center.dst3
        labels(3) = center.labels(3)
        resync = False
    End Sub
End Class








Public Class XO_FPoly_Plot : Inherits TaskParent
    Public fGrid As New XO_FPoly_Core
    Dim plotHist As New Plot_Histogram
    Public hist() As Single
    Public distDiff As New List(Of Single)
    Public Sub New()
        plotHist.minRange = 0
        plotHist.removeZeroEntry = False
        labels = {"", "", "", "anchor and companions - input to distance difference"}
        desc = "Feature Grid: compute distances between good features from frame to frame and plot the distribution"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lastDistance = fGrid.dst0.Clone

        fGrid.Run(src)
        dst3 = fGrid.dst3

        dst3 = src.Clone
        ReDim hist(fGrid.threshold + 1)
        distDiff.Clear()
        For i = 0 To fGrid.stable.basics.facetGen.facet.facetList.Count - 1
            Dim pt = fGrid.stable.basics.ptList(i)
            Dim d = fGrid.anchor.DistanceTo(pt)
            Dim lastd = lastDistance.Get(Of Single)(pt.Y, pt.X)
            Dim absDiff = Math.Abs(lastd - d)
            If absDiff >= hist.Length Then absDiff = hist.Length - 1
            If absDiff < fGrid.threshold Then
                hist(CInt(absDiff)) += 1
                DrawLine(dst3, fGrid.anchor, pt, task.highlight)
                distDiff.Add(absDiff)
            Else
                hist(fGrid.threshold) += 1
            End If
        Next

        Dim hlist = hist.ToList
        Dim peak = hlist.Max
        Dim peakIndex = hlist.IndexOf(peak)

        Dim histMat = cv.Mat.FromPixelData(hist.Length, 1, cv.MatType.CV_32F, hist.ToArray)
        plotHist.maxRange = fGrid.stable.basics.ptList.Count
        plotHist.Run(histMat)
        dst2 = plotHist.dst2
        Dim avg = If(distDiff.Count > 0, distDiff.Average, 0)
        labels(2) = "Average distance change (after threshholding) = " + Format(avg, fmt3) + ", peak at " + CStr(peakIndex) +
                        " with " + Format(peak, fmt1) + " occurances"
    End Sub
End Class








Public Class XO_FPoly_PlotWeighted : Inherits TaskParent
    Public fPlot As New XO_FPoly_Plot
    Dim plotHist As New Plot_Histogram
    Public Sub New()
        task.kalman = New Kalman_Basics
        plotHist.minRange = 0
        plotHist.removeZeroEntry = False
        labels = {"", "Distance change from previous frame", "", "anchor and companions - input to distance difference"}
        desc = "Feature Grid: compute distances between good features from frame to frame and plot with weighting and Kalman to smooth results"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fPlot.Run(src)
        dst3 = fPlot.dst3

        Dim lastPlot As cv.Mat = plotHist.dst2.Clone
        If task.optionsChanged Then ReDim task.kalman.kInput(fPlot.hist.Length - 1)

        task.kalman.kInput = fPlot.hist
        task.kalman.Run(emptyMat)
        fPlot.hist = task.kalman.kOutput

        Dim hlist = fPlot.hist.ToList
        Dim peak = hlist.Max
        Dim peakIndex = hlist.IndexOf(peak)
        Dim histMat = cv.Mat.FromPixelData(fPlot.hist.Length, 1, cv.MatType.CV_32F, fPlot.hist)
        plotHist.maxRange = fPlot.fGrid.stable.basics.ptList.Count
        plotHist.Run(histMat)
        dst2 = ShowAddweighted(plotHist.dst2, lastPlot, labels(2))
        If task.heartBeat Then
            Dim avg = If(fPlot.distDiff.Count > 0, fPlot.distDiff.Average, 0)
            labels(2) = "Average distance change (after threshholding) = " + Format(avg, fmt3) + ", peak at " +
                        CStr(peakIndex) + " with " + Format(peak, fmt1) + " occurances"
        End If
    End Sub
End Class






Public Class XO_FPoly_Stablizer : Inherits TaskParent
    Public fGrid As New XO_FPoly_Core
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "Movement amount - dot is current anchor point", "SyncImage aligned to current image - slide camera left or right",
                  "current image with distance map"}
        desc = "Feature Grid: show the accumulated camera movement in X and Y (no rotation)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fGrid.Run(src.Clone)
        dst3 = fGrid.dst3
        labels(3) = fGrid.labels(2)

        Static syncImage = src.Clone
        If fGrid.startAnchor = fGrid.anchor Then syncImage = src.Clone

        Dim shift As cv.Point2f = New cv.Point2f(fGrid.startAnchor.X - fGrid.anchor.X, fGrid.startAnchor.Y - fGrid.anchor.Y)
        Dim rect As New cv.Rect
        If shift.X < 0 Then rect.X = 0 Else rect.X = shift.X
        If shift.Y < 0 Then rect.Y = 0 Else rect.Y = shift.Y
        rect.Width = dst1.Width - Math.Abs(shift.X)
        rect.Height = dst1.Height - Math.Abs(shift.Y)

        dst1.SetTo(0)
        dst1(rect) = syncImage(rect)
        Dim lp As New lpData(fGrid.startAnchor, fGrid.anchor)
        DrawFatLine(dst1, lp, white)

        DrawPolkaDot(fGrid.anchor, dst1)

        Dim r = New cv.Rect(0, 0, rect.Width, rect.Height)
        If fGrid.anchor.X > fGrid.startAnchor.X Then r.X = fGrid.anchor.X - fGrid.startAnchor.X
        If fGrid.anchor.Y > fGrid.startAnchor.Y Then r.Y = fGrid.anchor.Y - fGrid.startAnchor.Y

        dst2.SetTo(0)
        dst2(r) = syncImage(rect)
    End Sub
End Class








Public Class XO_FPoly_StartPoints : Inherits TaskParent
    Public startPoints As New List(Of cv.Point2f)
    Public goodPoints As New List(Of cv.Point2f)
    Dim fGrid As New XO_FPoly_Core
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_8U, 255)
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Track the feature grid points back to the last sync point"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static thresholdSlider = OptionParent.FindSlider("Resync if feature moves > X pixels")
        Dim threshold = thresholdSlider.Value
        Dim maxShift = fGrid.anchor.DistanceTo(fGrid.startAnchor) + threshold

        fGrid.Run(src)
        dst2 = fGrid.dst3
        Static facets As New List(Of List(Of cv.Point))
        Dim lastPoints = dst0.Clone
        If fGrid.startAnchor = fGrid.anchor Or goodPoints.Count < 5 Then
            startPoints = New List(Of cv.Point2f)(fGrid.goodPoints)
            facets = New List(Of List(Of cv.Point))(fGrid.goodFacets)
        End If

        dst0.SetTo(255)
        If standaloneTest() Then dst1.SetTo(0)
        Dim lpList As New List(Of lpData)
        goodPoints = New List(Of cv.Point2f)(fGrid.goodPoints)
        Dim facet As New List(Of cv.Point)
        Dim usedGood As New List(Of Integer)
        For i = 0 To goodPoints.Count - 1
            Dim pt = goodPoints(i)
            Dim startPoint = lastPoints.Get(Of Byte)(pt.Y, pt.X)
            If startPoint = 255 And i < 256 Then startPoint = i
            If startPoint < startPoints.Count And usedGood.Contains(startPoint) = False Then
                usedGood.Add(startPoint)
                facet = facets(startPoint)
                dst0.FillConvexPoly(facet, startPoint, cv.LineTypes.Link4)
                If standaloneTest() Then dst1.FillConvexPoly(facet, task.scalarColors(startPoint), task.lineType)
                lpList.Add(New lpData(startPoints(startPoint), pt))
            End If
        Next

        ' dst3.SetTo(0)
        For Each lp In lpList
            If lp.p1.DistanceTo(lp.p2) <= maxShift Then DrawLine(dst1, lp.p1, lp.p2, cv.Scalar.Yellow)
            DrawCircle(dst1, lp.p1, task.DotSize, cv.Scalar.Yellow)
        Next
        dst1.Line(fGrid.anchor, fGrid.startAnchor, white, task.lineWidth + 1, task.lineType)
    End Sub
End Class








Public Class XO_FPoly_Triangle : Inherits TaskParent
    Dim triangle As New FindTriangle_Basics
    Dim fGrid As New XO_FPoly_Core
    Public Sub New()
        desc = "Find the minimum triangle that contains the feature grid"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fGrid.Run(src)
        dst2 = fGrid.dst2

        triangle.srcPoints = New List(Of cv.Point2f)(fGrid.goodPoints)
        triangle.Run(src)
        dst3 = triangle.dst2
    End Sub
End Class






Public Class XO_FPoly_WarpAffinePoly : Inherits TaskParent
    Dim rotatePoly As New XO_Rotate_PolyQT
    Dim warp As New WarpAffine_BasicsQT
    Dim fPoly As New XO_FPoly_BasicsOriginal
    Public Sub New()
        labels = {"", "", "Feature polygon after just rotation - white (original), yellow (current)",
                  "Feature polygon with rotation and shift - should be aligned"}
        desc = "Rotate and shift just the Feature polygon as indicated by XO_FPoly_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fPoly.Run(src)
        Dim polyPrev = fPoly.fPD.prevPoly
        Dim poly = New List(Of cv.Point2f)(fPoly.fPD.currPoly)

        dst2.SetTo(0)
        dst3.SetTo(0)

        XO_FPoly_Basics.DrawFPoly(dst2, polyPrev, white)

        warp.rotateCenter = fPoly.fPD.rotateCenter
        warp.rotateAngle = fPoly.fPD.rotateAngle
        warp.Run(dst2)
        dst3 = warp.dst2

        rotatePoly.rotateAngle = fPoly.fPD.rotateAngle
        rotatePoly.rotateCenter = fPoly.fPD.rotateCenter
        rotatePoly.poly = New List(Of cv.Point2f)(poly)
        rotatePoly.Run(src)

        If rotatePoly.poly.Count = 0 Then Exit Sub
        If fPoly.fPD.polyPrevSideIndex > rotatePoly.poly.Count Then fPoly.fPD.polyPrevSideIndex = 0

        Dim offset = New cv.Point2f(rotatePoly.poly(fPoly.fPD.polyPrevSideIndex).X - polyPrev(fPoly.fPD.polyPrevSideIndex).X,
                                    rotatePoly.poly(fPoly.fPD.polyPrevSideIndex).Y - polyPrev(fPoly.fPD.polyPrevSideIndex).Y)

        Dim r1 = New cv.Rect(offset.X, offset.Y, dst2.Width - Math.Abs(offset.X), dst2.Height - Math.Abs(offset.Y))
        If offset.X < 0 Then r1.X = 0
        If offset.Y < 0 Then r1.Y = 0

        Dim r2 = New cv.Rect(Math.Abs(offset.X), Math.Abs(offset.Y), r1.Width, r1.Height)
        If offset.X > 0 Then r2.X = 0
        If offset.Y > 0 Then r2.Y = 0

        dst3(r1) = dst2(r2)
        dst3 = dst3 - dst2

        XO_FPoly_Basics.DrawFPoly(dst3, rotatePoly.poly, cv.Scalar.Yellow)
        XO_FPoly_Basics.DrawFPoly(dst2, rotatePoly.poly, cv.Scalar.Yellow)

        SetTrueText(fPoly.strOut, 3)
    End Sub
End Class










Public Class XO_FPoly_RotatePoints : Inherits TaskParent
    Dim rotatePoly As New XO_Rotate_PolyQT
    Public poly As New List(Of cv.Point2f)
    Public polyPrev As New List(Of cv.Point2f)
    Public rotateAngle As Single
    Public rotateCenter As cv.Point2f
    Public polyPrevSideIndex As Integer
    Public centerShift As cv.Point2f
    Public Sub New()
        labels = {"", "", "Feature polygon after just rotation - white (original), yellow (current)",
                  "Feature polygons with rotation and shift - should be aligned"}
        desc = "Rotate and shift just the Feature polygon as indicated by XO_FPoly_Basics"
    End Sub
    Public Function shiftPoly(polyPrev As List(Of cv.Point2f), poly As List(Of cv.Point2f)) As cv.Point2f
        rotatePoly.rotateAngle = rotateAngle
        rotatePoly.rotateCenter = rotateCenter
        rotatePoly.poly = New List(Of cv.Point2f)(poly)
        rotatePoly.Run(emptyMat)

        Dim totalX = rotatePoly.poly(polyPrevSideIndex).X - polyPrev(polyPrevSideIndex).X
        Dim totalY = rotatePoly.poly(polyPrevSideIndex).Y - polyPrev(polyPrevSideIndex).Y

        Return New cv.Point2f(totalX, totalY)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            SetTrueText(traceName + " is meant only to run with XO_FPoly_Basics to validate the translation")
            Exit Sub
        End If

        dst2.SetTo(0)
        dst3.SetTo(0)

        Dim rotateAndShift As New List(Of cv.Point2f)
        centerShift = shiftPoly(polyPrev, poly)
        XO_FPoly_Basics.DrawFPoly(dst2, polyPrev, white)
        XO_FPoly_Basics.DrawFPoly(dst2, rotatePoly.poly, cv.Scalar.Yellow)
        For i = 0 To polyPrev.Count - 1
            Dim p1 = New cv.Point2f(rotatePoly.poly(i).X - centerShift.X, rotatePoly.poly(i).Y - centerShift.Y)
            Dim p2 = New cv.Point2f(rotatePoly.poly((i + 1) Mod task.polyCount).X - centerShift.X,
                                    rotatePoly.poly((i + 1) Mod task.polyCount).Y - centerShift.Y)
            rotateAndShift.Add(p1)
            SetTrueText(CStr(i), rotatePoly.poly(i), 2)
            SetTrueText(CStr(i), polyPrev(i), 2)
        Next
        XO_FPoly_Basics.DrawFPoly(dst3, polyPrev, white)
        XO_FPoly_Basics.DrawFPoly(dst3, rotateAndShift, cv.Scalar.Yellow)

        strOut = "After Rotation: " + Format(rotatePoly.rotateAngle, fmt0) + " degrees " +
                 "After Translation (shift) of: " + Format(centerShift.X, fmt0) + ", " + Format(centerShift.Y, fmt0) + vbCrLf +
                 "Center of Rotation: " + Format(rotateCenter.X, fmt0) + ", " + Format(rotateCenter.Y, fmt0) + vbCrLf +
                 "If the algorithm is working properly, the white and yellow Feature polygons below " + vbCrLf +
                 "should match in size and location."
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class XO_FPoly_WarpAffineImage : Inherits TaskParent
    Dim warp As New WarpAffine_BasicsQT
    Dim fPoly As New XO_FPoly_BasicsOriginal
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use OpenCV's WarpAffine to rotate and translate the starting image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fPoly.Run(src)

        warp.rotateCenter = fPoly.fPD.rotateCenter
        warp.rotateAngle = fPoly.fPD.rotateAngle
        warp.Run(fPoly.resyncImage.Clone)
        dst2 = warp.dst2
        dst1 = fPoly.dst1

        Dim offset = fPoly.fPD.centerShift

        Dim r1 = New cv.Rect(offset.X, offset.Y, dst2.Width - Math.Abs(offset.X), dst2.Height - Math.Abs(offset.Y))
        If offset.X < 0 Then r1.X = 0
        If offset.Y < 0 Then r1.Y = 0

        Dim r2 = New cv.Rect(Math.Abs(offset.X), Math.Abs(offset.Y), r1.Width, r1.Height)
        If offset.X > 0 Then r2.X = 0
        If offset.Y > 0 Then r2.Y = 0

        dst3(r1) = dst2(r2)
        dst3 = src - dst2

        Dim tmp = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim changed = tmp.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
        Dim diffCount = changed.CountNonZero
        strOut = fPoly.strOut
        strOut += vbCrLf + Format(diffCount / 1000, fmt0) + "k pixels differ or " +
                           Format(diffCount / dst3.Total, "0%")

        SetTrueText(strOut, 1)
    End Sub
End Class








' https://www.google.com/search?q=geometry+find+the+center+of+rotation&rlz=1C1CHBF_enUS838US838&oq=geometry+find+the+center+of+rotation&aqs=chrome..69i57j0i22i30j0i390l3.9576j0j4&sourceid=chrome&ie=UTF-8#kpvalbx=_rgg1Y9rbGM3n0PEP-ae4oAc_34
Public Class XO_FPoly_Perpendiculars : Inherits TaskParent
    Public altCenterShift As cv.Point2f
    Public fPD As fPolyData
    Public rotatePoints As New XO_FPoly_RotatePoints
    Dim near As New XO_Line_Nearest
    Public Sub New()
        task.kalman = New Kalman_Basics
        labels = {"", "", "Output of XO_FPoly_Basics", "Center of rotation is where the extended lines intersect"}
        desc = "Find the center of rotation using the perpendicular lines from polymp and FLine (feature line) in XO_FPoly_Basics"
    End Sub
    Private Function findrotateAngle(p1 As cv.Point2f, p2 As cv.Point2f, pt As cv.Point2f) As Single
        near.lp = New lpData(p1, p2)
        near.pt = pt
        near.Run(emptyMat)
        DrawLine(dst2, pt, near.nearPoint, cv.Scalar.Red)
        Dim d1 = fPD.rotateCenter.DistanceTo(pt)
        Dim d2 = fPD.rotateCenter.DistanceTo(near.nearPoint)
        Dim angle = Math.Asin(near.nearPoint.DistanceTo(pt) / If(d1 > d2, d1, d2))
        If Single.IsNaN(angle) Then Return 0
        Return angle
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            SetTrueText("There is no output for the " + traceName + " algorithm when run standaloneTest().")
            Exit Sub
        End If

        Static perp1 As New Line_PerpendicularTest
        Static perp2 As New Line_PerpendicularTest

        dst2.SetTo(0)
        perp1.input = New lpData(fPD.currPoly(fPD.polyPrevSideIndex),
                                    fPD.currPoly((fPD.polyPrevSideIndex + 1) Mod task.polyCount))
        perp1.Run(src)

        DrawLine(dst2, perp1.output.p1, perp1.output.p2, cv.Scalar.Yellow)

        perp2.input = New lpData(fPD.prevPoly(fPD.polyPrevSideIndex),
                                   fPD.prevPoly((fPD.polyPrevSideIndex + 1) Mod task.polyCount))
        perp2.Run(src)
        DrawLine(dst2, perp2.output.p1, perp2.output.p2, white)

        fPD.rotateCenter = Line_Intersection.IntersectTest(perp2.output.p1, perp2.output.p2, perp1.output.p1, perp1.output.p2)
        If fPD.rotateCenter = New cv.Point2f Then
            fPD.rotateAngle = 0
        Else
            DrawCircle(dst2, fPD.rotateCenter, task.DotSize + 2, cv.Scalar.Red)
            fPD.rotateAngle = findrotateAngle(perp2.output.p1, perp2.output.p2, perp1.output.p1)
        End If
        If fPD.rotateAngle = 0 Then fPD.rotateCenter = New cv.Point2f

        altCenterShift = New cv.Point2f(fPD.currPoly(fPD.polyPrevSideIndex).X - fPD.prevPoly(fPD.polyPrevSideIndex).X,
                                        fPD.currPoly(fPD.polyPrevSideIndex).Y - fPD.prevPoly(fPD.polyPrevSideIndex).Y)

        task.kalman.kInput = {fPD.rotateAngle}
        task.kalman.Run(emptyMat)
        fPD.rotateAngle = task.kalman.kOutput(0)

        rotatePoints.poly = fPD.currPoly
        rotatePoints.polyPrev = fPD.prevPoly
        rotatePoints.polyPrevSideIndex = fPD.polyPrevSideIndex
        rotatePoints.rotateAngle = fPD.rotateAngle
        rotatePoints.Run(src)
        fPD.centerShift = rotatePoints.centerShift
        dst3 = rotatePoints.dst3
    End Sub
End Class








Public Class XO_FPoly_Image : Inherits TaskParent
    Public fpoly As New XO_FPoly_BasicsOriginal
    Dim rotate As New Rotate_BasicsQT
    Public resync As Boolean
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "Feature polygon alignment, White is original, Yellow is current, Red Dot (if present) is center of rotation",
                  "Resync Image after rotation and translation", "Difference between current image and dst2"}
        desc = "Rotate and shift the image as indicated by XO_FPoly_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim input = src.Clone
        fpoly.Run(src)
        dst1 = fpoly.dst1

        If fpoly.resync = False Then
            If fpoly.fPD.featureLineChanged = False Then
                dst2.SetTo(0)
                dst3.SetTo(0)
                rotate.rotateAngle = fpoly.fPD.rotateAngle
                rotate.rotateCenter = fpoly.fPD.rotateCenter
                rotate.Run(fpoly.resyncImage)
                dst0 = rotate.dst2

                Dim offset As cv.Point2f = fpoly.fPD.centerShift

                Dim r1 = New cv.Rect(offset.X, offset.Y, dst2.Width - Math.Abs(offset.X), dst2.Height - Math.Abs(offset.Y))
                r1 = ValidateRect(r1)
                If offset.X < 0 Then r1.X = 0
                If offset.Y < 0 Then r1.Y = 0

                Dim r2 = New cv.Rect(Math.Abs(offset.X), Math.Abs(offset.Y), r1.Width, r1.Height)
                r2.Width = r1.Width
                r2.Height = r1.Height
                If r2.X < 0 Or r2.X >= dst2.Width Then Exit Sub ' wedged...
                If r2.Y < 0 Or r2.Y >= dst2.Height Then Exit Sub ' wedged...
                If offset.X > 0 Then r2.X = 0
                If offset.Y > 0 Then r2.Y = 0

                Dim mask2 As New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 255)
                rotate.Run(mask2)
                mask2 = rotate.dst2

                Dim mask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
                mask(r1).SetTo(255)
                mask(r1) = mask2(r2)
                mask = Not mask

                dst2(r1) = dst0(r2)
                dst3 = input - dst2
                dst3.SetTo(0, mask)
            End If

            Dim tmp = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim changed = tmp.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            Dim diffCount = changed.CountNonZero
            resync = fpoly.resync
            fpoly.maskChangePercent = diffCount / dst3.Total
            strOut = fpoly.strOut
            strOut += vbCrLf + Format(diffCount / 1000, fmt0) + "k pixels differ or " + Format(fpoly.maskChangePercent, "00%")

        Else
            dst2 = fpoly.resyncImage.Clone
            dst3.SetTo(0)
        End If

        SetTrueText(strOut, 1)
    End Sub
End Class








Public Class XO_FPoly_ImageMask : Inherits TaskParent
    Public fImage As New XO_FPoly_Image
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        task.gOptions.pixelDiffThreshold = 10
        desc = "Build the image mask of the differences between the current frame and resync image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fImage.Run(src)
        dst2 = fImage.dst3
        dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst3 = dst0.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
        labels = fImage.labels
        dst1 = fImage.fpoly.dst1
        SetTrueText(fImage.strOut, 1)
    End Sub
End Class







Public Class XO_FPoly_PointCloud : Inherits TaskParent
    Public fMask As New XO_FPoly_ImageMask
    Public fPolyCloud As cv.Mat
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Update changed point cloud pixels as indicated by the FPoly_ImageMask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fMask.Run(src)
        If fMask.fImage.fpoly.resync Or task.firstPass Then fPolyCloud = task.pointCloud.Clone
        dst1 = fMask.dst1
        dst2 = fMask.dst2
        dst3 = fMask.dst3
        task.pointCloud.CopyTo(fPolyCloud, dst3)

        SetTrueText(fMask.fImage.strOut, 1)
    End Sub
End Class







Public Class XO_FPoly_ResyncCheck : Inherits TaskParent
    Dim fPoly As New XO_FPoly_BasicsOriginal
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "If there was no resync, check the longest side of the feature polygon (Feature Line) for unnecessary jitter."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fPoly.Run(src)
        dst2 = fPoly.dst1
        SetTrueText(fPoly.strOut, 2)

        Static lastPixelCount As Integer
        If fPoly.resync Then
            dst3.SetTo(0)
            lastPixelCount = 0
        End If

        If fPoly.fPD.currPoly.Count < 2 Then Exit Sub ' polygon not found...

        Dim polymp = fPoly.fPD.currmp()
        DrawLine(dst3, polymp.p1, polymp.p2, 255)

        Dim pixelCount = dst3.CountNonZero
        SetTrueText(Format(Math.Abs(lastPixelCount - pixelCount)) + " pixels ", 3)
        lastPixelCount = pixelCount
    End Sub
End Class








Public Class XO_FPoly_Center : Inherits TaskParent
    Public rotatePoly As New XO_Rotate_PolyQT
    Dim near As New XO_Line_Nearest
    Public fPD As fPolyData
    Dim newPoly As List(Of cv.Point2f)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "Layout of feature polygons after just translation - red line is used in sine computation",
                      "Layout of the starting (white) and current (yellow) feature polygons",
                      "Layout of feature polygons after rotation and translation"}
        desc = "Manually rotate and translate the current feature polygon to a previous feature polygon."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            SetTrueText(traceName + " is called by XO_FPoly_Basics to get the image movement." + vbCrLf +
                        "It does not produce any output when run standaloneTest().")
            Exit Sub
        End If

        Static thresholdSlider = OptionParent.FindSlider("Resync if feature moves > X pixels")
        Dim threshold = thresholdSlider.Value

        Dim sindex1 = fPD.polyPrevSideIndex
        Dim sIndex2 = (sindex1 + 1) Mod task.polyCount

        Dim mp1 = fPD.currmp()
        Dim mp2 = fPD.prevmp()
        Dim d1 = mp1.p1.DistanceTo(mp2.p1)
        Dim d2 = mp1.p2.DistanceTo(mp2.p2)
        Dim newNear As lpData
        If d1 < d2 Then
            fPD.centerShift = New cv.Point2f(mp1.p1.X - mp2.p1.X, mp1.p1.Y - mp2.p1.Y)
            fPD.rotateCenter = mp1.p1
            newNear = New lpData(mp1.p2, mp2.p2)
        Else
            fPD.centerShift = New cv.Point2f(mp1.p2.X - mp2.p2.X, mp1.p2.Y - mp2.p2.Y)
            fPD.rotateCenter = mp1.p2
            newNear = New lpData(mp1.p1, mp2.p1)
        End If

        Dim transPoly As New List(Of cv.Point2f)
        For i = 0 To fPD.currPoly.Count - 1
            transPoly.Add(New cv.Point2f(fPD.currPoly(i).X - fPD.centerShift.X, fPD.currPoly(i).Y - fPD.centerShift.Y))
        Next
        newNear.p1 = New cv.Point2f(newNear.p1.X - fPD.centerShift.X, newNear.p1.Y - fPD.centerShift.Y)
        newNear.p2 = New cv.Point2f(newNear.p2.X - fPD.centerShift.X, newNear.p2.Y - fPD.centerShift.Y)
        fPD.rotateCenter = New cv.Point2f(fPD.rotateCenter.X - fPD.centerShift.X, fPD.rotateCenter.Y - fPD.centerShift.Y)

        dst1.SetTo(0)
        XO_FPoly_Basics.DrawPolys(dst1, fPD)

        strOut = "No rotation" + vbCrLf
        fPD.rotateAngle = 0
        If d1 <> d2 Then
            If newNear.p1.DistanceTo(newNear.p2) > threshold Then
                near.lp = New lpData(fPD.prevPoly(sindex1), fPD.prevPoly(sIndex2))
                near.pt = newNear.p1
                near.Run(src)
                dst1.Line(near.pt, near.nearPoint, cv.Scalar.Red, task.lineWidth + 5, task.lineType)

                Dim hypotenuse = fPD.rotateCenter.DistanceTo(near.pt)
                fPD.rotateAngle = -Math.Asin(near.nearPoint.DistanceTo(near.pt) / hypotenuse)
                If Single.IsNaN(fPD.rotateAngle) Then fPD.rotateAngle = 0
                strOut = "Angle is " + Format(fPD.rotateAngle * 57.2958, fmt1) + " degrees" + vbCrLf
            End If
        End If
        strOut += "Translation (shift) is " + Format(-fPD.centerShift.X, fmt0) + ", " + Format(-fPD.centerShift.Y, fmt0)

        If Math.Abs(fPD.rotateAngle) > 0 Then
            rotatePoly.rotateCenter = fPD.rotateCenter
            rotatePoly.rotateAngle = fPD.rotateAngle
            rotatePoly.poly.Clear()
            rotatePoly.poly.Add(newNear.p1)
            rotatePoly.Run(src)

            If near.nearPoint.DistanceTo(rotatePoly.poly(0)) > newNear.p1.DistanceTo(rotatePoly.poly(0)) Then fPD.rotateAngle *= -1

            rotatePoly.rotateAngle = fPD.rotateAngle
            rotatePoly.poly = New List(Of cv.Point2f)(transPoly)
            rotatePoly.Run(src)

            newPoly = New List(Of cv.Point2f)(rotatePoly.poly)
        End If
        dst3.SetTo(0)
        XO_FPoly_Basics.DrawPolys(dst3, fPD)
        SetTrueText(strOut, 2)
    End Sub
End Class








Public Class XO_FPoly_EdgeRemoval : Inherits TaskParent
    Dim fMask As New XO_FPoly_ImageMask
    Dim edges As New Edge_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Remove edges from the FPoly_ImageMask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fMask.Run(src)
        dst2 = fMask.dst3

        edges.Run(src)
        dst1 = edges.dst2

        dst3 = dst2 And Not dst1
    End Sub
End Class








Public Class XO_FPoly_ImageNew : Inherits TaskParent
    Public fpoly As New XO_FPoly_Basics
    Dim rotate As New Rotate_BasicsQT
    Public resync As Boolean
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"", "Feature polygon alignment, White is original, Yellow is current, Red Dot (if present) is center of rotation",
                  "Resync Image after rotation and translation", "Difference between current image and dst2"}
        desc = "Rotate and shift the image as indicated by XO_FPoly_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim input = src.Clone
        fpoly.Run(src)
        dst1 = fpoly.dst3

        If fpoly.resync = False Then
            ' If fpoly.sides.featureLineChanged = False Then
            dst2.SetTo(0)
            dst3.SetTo(0)
            rotate.rotateAngle = fpoly.sides.rotateAngle
            rotate.rotateCenter = fpoly.sides.rotateCenter
            rotate.Run(fpoly.sides.prevImage)
            dst0 = rotate.dst2

            Dim offset As cv.Point2f = fpoly.sides.centerShift

            Dim r1 = New cv.Rect(offset.X, offset.Y, dst2.Width - Math.Abs(offset.X), dst2.Height - Math.Abs(offset.Y))
            If offset.X < 0 Then r1.X = 0
            If offset.Y < 0 Then r1.Y = 0

            Dim r2 = New cv.Rect(Math.Abs(offset.X), Math.Abs(offset.Y), r1.Width, r1.Height)
            If offset.X > 0 Then r2.X = 0
            If offset.Y > 0 Then r2.Y = 0

            Dim mask2 As New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 255)
            rotate.Run(mask2)
            mask2 = rotate.dst2

            Dim mask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            mask(r1).SetTo(255)
            mask(r1) = mask2(r2)
            mask = Not mask

            dst2(r1) = dst0(r2)
            dst3 = input - dst2
            dst3.SetTo(0, mask)
            ' End If

            Dim tmp = dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim changed = tmp.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            Dim diffCount = changed.CountNonZero
            resync = fpoly.resync
            fpoly.maskChangePercent = diffCount / dst3.Total
            strOut = fpoly.strOut
            strOut += vbCrLf + Format(diffCount / 1000, fmt0) + "k pixels differ or " + Format(fpoly.maskChangePercent, "00%")
        Else
            dst2 = fpoly.sides.prevImage.Clone
            dst3.SetTo(0)
        End If

        SetTrueText(strOut, 1)
    End Sub
End Class






Public Class XO_FPoly_LeftRight : Inherits TaskParent
    Dim leftPoly As New XO_FPoly_Basics
    Dim rightPoly As New XO_FPoly_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels = {"Left image", "Right image", "FPoly output for left image", "FPoly output for right image"}
        desc = "Measure camera motion through the left and right images using FPoly"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst0 = task.leftView
        dst1 = task.rightView
        leftPoly.Run(task.leftView)
        dst2 = leftPoly.dst3
        SetTrueText(leftPoly.strOut, 2)

        rightPoly.Run(task.rightView)
        dst3 = rightPoly.dst3
        SetTrueText(rightPoly.strOut, 3)
    End Sub
End Class








Public Class XO_FPoly_Core : Inherits TaskParent
    Public stable As New FCS_Basics
    Public anchor As cv.Point2f
    Public startAnchor As cv.Point2f
    Public goodPoints As New List(Of cv.Point2f)
    Public goodFacets As New List(Of List(Of cv.Point))
    Public threshold As Integer
    Dim options As New Options_FPoly
    Dim optionsCore As New Options_FPolyCore
    Dim optionsEx As New Options_Features
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        task.featureOptions.FeatureSampleSize.Value = 20
        labels(3) = "Feature points with anchor"
        desc = "Feature Grid: compute distances between good features from frame to frame"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        optionsCore.Run()
        optionsEx.Run()

        stable.Run(src)
        dst3 = stable.basics.dst3

        Dim lastDistance = dst0.Clone
        anchor = stable.basics.anchorPoint
        Static lastAnchor = anchor
        If lastAnchor.distanceto(anchor) > optionsCore.anchorMovement Then lastDistance.SetTo(0)

        dst0.SetTo(0)
        goodPoints.Clear()
        goodFacets.Clear()
        dst2.SetTo(0)
        For i = 0 To stable.basics.facetGen.facet.facetList.Count - 1
            Dim facet = stable.basics.facetGen.facet.facetList(i)
            Dim pt = stable.basics.ptList(i)
            Dim d = anchor.DistanceTo(pt)
            dst0.FillConvexPoly(facet, d, task.lineType)
            Dim lastd = lastDistance.Get(Of Single)(pt.Y, pt.X)
            Dim absDiff = Math.Abs(lastd - d)
            If absDiff < threshold Or threshold = 0 Then
                goodPoints.Add(pt)
                goodFacets.Add(facet)
                SetTrueText(Format(absDiff, fmt1), pt, 2)
                DrawLine(dst3, anchor, pt, task.highlight)
                dst2.Set(Of cv.Vec3b)(pt.Y, pt.X, white.ToVec3b)
            End If
        Next

        Dim shift As cv.Point2f = New cv.Point2f(startAnchor.X - anchor.X, startAnchor.Y - anchor.Y)
        If goodPoints.Count = 0 Or Math.Abs(shift.X) > optionsCore.maxShift Or Math.Abs(shift.Y) > optionsCore.maxShift Then startAnchor = anchor
        labels(2) = "Distance change (after threshholding) since last reset = " + shift.ToString
        lastAnchor = anchor
    End Sub
End Class







Public Class XO_FPoly_TopFeatures : Inherits TaskParent
    Public stable As New Stable_BasicsCount
    Public options As New Options_FPoly
    Dim feat As New Feature_General
    Public topFeatures As New List(Of cv.Point2f)
    Public Sub New()
        desc = "Get the top features and validate them using Delaunay regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        feat.Run(task.grayStable)

        stable.Run(src)
        dst2 = stable.dst2
        topFeatures.Clear()
        Dim showText = standaloneTest()
        For Each keyVal In stable.goodCounts
            Dim pt = stable.basics.ptList(keyVal.Value)
            Dim g = stable.basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
            If showText Then SetTrueText(CStr(g), pt)
            If topFeatures.Count < task.polyCount Then topFeatures.Add(pt)
        Next

        For i = 0 To topFeatures.Count - 2
            DrawLine(dst2, topFeatures(i), topFeatures(i + 1), white)
        Next
    End Sub
End Class






Public Class XO_FPoly_Line : Inherits TaskParent
    Dim feat As New XO_FPoly_TopFeatures
    Public lp As New lpData
    Dim ptBest As New BrickPoint_Basics
    Public Sub New()
        labels = {"", "", "Points found with FPoly_TopFeatures", "Longest line in feat.topFeatures"}
        desc = "Identify the longest line in topFeatures"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBest.Run(src)
        task.features = ptBest.features

        feat.Run(src)
        dst2.SetTo(0)
        Dim pts = feat.topFeatures
        Dim distances As New List(Of Single)
        For i = 0 To pts.Count - 2
            DrawLine(dst2, pts(i), pts(i + 1), task.highlight)
            distances.Add(pts(i).DistanceTo(pts(i + 1)))
        Next

        If distances.Count Then
            Dim index = distances.IndexOf(distances.Max)
            lp = New lpData(pts(index), pts(index + 1))
            dst3 = src
            DrawLine(dst3, lp.p1, lp.p2, task.highlight)
        End If
    End Sub
End Class






Public Class XO_FPoly_LineRect : Inherits TaskParent
    Dim fLine As New XO_FPoly_Line
    Public lpRect As New cv.Rect
    Public Sub New()
        labels(2) = "The rectangle is formed by the longest line between the task.topFeatures"
        desc = "Build the rectangle formed by the longest line in task.topFeatures."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLine.Run(src)

        Dim lp = fLine.lp
        Dim rotatedRect = cv.Cv2.MinAreaRect({lp.p1, lp.p2})
        lpRect = rotatedRect.BoundingRect

        dst2 = src
        DrawLine(dst2, lp.p1, lp.p2, task.highlight)
        dst2.Rectangle(lpRect, task.highlight, task.lineWidth)
    End Sub
End Class










Public Class XO_Delaunay_Points : Inherits TaskParent
    Dim delaunay As New Delaunay_Basics
    Dim feat As New XO_FPoly_TopFeatures
    Public Sub New()
        OptionParent.FindSlider("Points to use in Feature Poly").Value = 2
        desc = "This algorithm explores what happens when Delaunay is used on 2 points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Static ptBest As New BrickPoint_Basics
            ptBest.Run(src)
            task.features = ptBest.features
        End If
        Static ptSlider = OptionParent.FindSlider("Points to use in Feature Poly")

        feat.Run(src)
        dst3 = feat.dst3

        delaunay.inputPoints.Clear()
        For i = 0 To Math.Min(ptSlider.value, feat.topFeatures.Count) - 1
            delaunay.inputPoints.Add(feat.topFeatures(i))
        Next
        delaunay.Run(src)
        dst2 = delaunay.dst2
    End Sub
End Class









Public Class XO_Homography_FPoly : Inherits TaskParent
    Dim fPoly As New XO_FPoly_BasicsOriginal
    Dim hGraph As New Homography_Basics
    Public Sub New()
        desc = "Use the feature polygon to warp the current image to a previous image.  This is not useful but demonstrates how to use homography."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fPoly.Run(src)
        dst2 = fPoly.dst1
        If fPoly.fPD.currPoly Is Nothing Or fPoly.fPD.prevPoly Is Nothing Then Exit Sub
        If fPoly.fPD.currPoly.Count = 0 Or fPoly.fPD.prevPoly.Count = 0 Then Exit Sub
        If fPoly.fPD.currPoly.Count <> fPoly.fPD.prevPoly.Count Then Exit Sub

        hGraph.corners1.Clear()
        hGraph.corners2.Clear()
        For i = 0 To fPoly.fPD.currPoly.Count - 1
            Dim p1 = fPoly.fPD.currPoly(i)
            Dim p2 = fPoly.fPD.prevPoly(i)
            hGraph.corners1.Add(New cv.Point2d(p1.X, p1.Y))
            hGraph.corners2.Add(New cv.Point2d(p2.X, p2.Y))
        Next

        hGraph.Run(src)
        dst3 = hGraph.dst2
    End Sub
End Class






Public Class XO_Motion_FPolyRect : Inherits TaskParent
    Dim fRect As New XO_FPoly_LineRect
    Public match As New Match_Basics
    Dim srcSave As New cv.Mat
    Public Sub New()
        desc = "Confirm the FPoly_LineRect matched the previous image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fRect.Run(src)

        If task.heartBeatLT Or match.correlation < 0.5 Then
            srcSave = src.Clone
            dst2 = fRect.dst2.Clone()
        End If
        match.template = srcSave(ValidateRect(fRect.lpRect)).Clone
        match.Run(src)
        dst3 = src
        dst3.Rectangle(match.newRect, task.highlight, task.lineWidth)
        labels(3) = "Correlation Coefficient = " + Format(match.correlation * 100, fmt1)
    End Sub
End Class





Public Class XO_Motion_TopFeatures : Inherits TaskParent
    Dim feat As New XO_FPoly_TopFeatures
    Public featureRects As New List(Of cv.Rect)
    Public searchRects As New List(Of cv.Rect)
    Dim match As New Match_Basics
    Dim half As Integer
    Public Sub New()
        labels(2) = "Track the feature rect (small one) in each larger rectangle"
        desc = "Find the top feature cells and track them in the next frame."
    End Sub
    Private Sub snapShotFeatures()
        searchRects.Clear()
        featureRects.Clear()
        For Each pt In feat.topFeatures
            Dim index As Integer = task.grid.gridMap.Get(Of Integer)(pt.Y, pt.X)
            Dim roi = New cv.Rect(pt.X - half, pt.Y - half, task.brickSize, task.brickSize)
            roi = ValidateRect(roi)
            featureRects.Add(roi)
            searchRects.Add(task.gridNabeRects(index))
        Next

        dst2 = dst1.Clone
        For Each pt In feat.topFeatures
            Dim index As Integer = task.grid.gridMap.Get(Of Integer)(pt.Y, pt.X)
            Dim roi = New cv.Rect(pt.X - half, pt.Y - half, task.brickSize, task.brickSize)
            roi = ValidateRect(roi)
            dst2.Rectangle(roi, task.highlight, task.lineWidth)
            dst2.Rectangle(task.gridNabeRects(index), task.highlight, task.lineWidth)
        Next
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        half = CInt(task.brickSize / 2)

        dst1 = src.Clone
        feat.Run(src)

        If task.heartBeatLT Then
            snapShotFeatures()
        End If

        dst3 = src.Clone
        Dim matchRects As New List(Of cv.Rect)
        For i = 0 To featureRects.Count - 1
            Dim roi = featureRects(i)
            match.template = dst1(roi).Clone
            match.Run(src(searchRects(i)))
            dst3.Rectangle(match.newRect, task.highlight, task.lineWidth)
            matchRects.Add(match.newRect)
        Next

        searchRects.Clear()
        featureRects.Clear()
        For Each roi In matchRects
            Dim pt = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
            Dim index As Integer = task.grid.gridMap.Get(Of Integer)(pt.Y, pt.X)
            featureRects.Add(roi)
            searchRects.Add(task.gridNabeRects(index))
        Next
    End Sub
End Class








' https://academo.org/demos/rotation-about-point/
Public Class XO_Rotate_PolyQT : Inherits TaskParent
    Public poly As New List(Of cv.Point2f)
    Public rotateCenter As cv.Point2f
    Public rotateAngle As Single
    Public Sub New()
        labels = {"", "", "Polygon before rotation", ""}
        desc = "Rotate a triangle around a center of rotation"
    End Sub
    Private Sub drawPolygon(dst As cv.Mat, color As cv.Scalar)
        Dim minMod = Math.Min(poly.Count, task.polyCount)
        For i = 0 To minMod - 1
            DrawLine(dst, poly(i), poly((i + 1) Mod minMod), color)
        Next
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            dst2.SetTo(0)
            dst3.SetTo(0)
        End If

        drawPolygon(dst2, red)

        If standaloneTest() Then
            SetTrueText(traceName + " has no output when run standaloneTest().")
            Exit Sub
        End If

        labels(3) = "White is the original polygon, yellow has been rotated " + Format(rotateAngle * 57.2958) + " degrees"

        ' translate so the center of rotation is 0,0
        Dim translated As New List(Of cv.Point2f)
        For i = 0 To poly.Count - 1
            Dim pt = poly(i)
            translated.Add(New cv.Point2f(poly(i).X - rotateCenter.X, poly(i).Y - rotateCenter.Y))
        Next

        Dim rotated As New List(Of cv.Point2f)
        For i = 0 To poly.Count - 1
            Dim pt = translated(i)
            Dim x = pt.X * Math.Cos(rotateAngle) - pt.Y * Math.Sin(rotateAngle)
            Dim y = pt.Y * Math.Cos(rotateAngle) + pt.X * Math.Sin(rotateAngle)
            rotated.Add(New cv.Point2f(x, y))
        Next

        drawPolygon(dst3, white)

        poly.Clear()
        For Each pt In rotated
            poly.Add(New cv.Point2f(pt.X + rotateCenter.X, pt.Y + rotateCenter.Y))
        Next

        drawPolygon(dst3, task.highlight)
    End Sub
End Class







' https://academo.org/demos/rotation-about-point/
Public Class XO_Rotate_Poly : Inherits TaskParent
    Dim optionsFPoly As New Options_FPoly
    Public options As New Options_RotatePoly
    Public rotateQT As New XO_Rotate_PolyQT
    Dim rPoly As New List(Of cv.Point2f)
    Public Sub New()
        labels = {"", "", "Triangle before rotation", "Triangle after rotation"}
        desc = "Rotate a triangle around a center of rotation"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        optionsFPoly.Run()

        If options.changeCheck.Checked Or task.firstPass Then
            rPoly.Clear()
            For i = 0 To task.polyCount - 1
                rPoly.Add(New cv.Point2f(msRNG.Next(dst2.Width / 4, dst2.Width * 3 / 4), msRNG.Next(dst2.Height / 4, dst2.Height * 3 / 4)))
            Next
            rotateQT.rotateCenter = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            options.changeCheck.Checked = False
        End If

        rotateQT.poly = New List(Of cv.Point2f)(rPoly)
        rotateQT.rotateAngle = options.angleSlider.Value
        rotateQT.Run(src)
        dst2 = rotateQT.dst3

        DrawCircle(dst2, rotateQT.rotateCenter, task.DotSize + 2, cv.Scalar.Yellow)
        SetTrueText("center of rotation", rotateQT.rotateCenter)
        labels(3) = rotateQT.labels(3)
    End Sub
End Class




Public Class XO_Stabilizer_Basics : Inherits TaskParent
    Dim match As New Match_Basics
    Public shiftX As Integer
    Public shiftY As Integer
    Public templateRect As cv.Rect
    Public searchRect As cv.Rect
    Public stableRect As cv.Rect
    Dim options As New Options_Stabilizer
    Dim lastFrame As cv.Mat
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(2) = "Current frame - rectangle input to matchTemplate"
        desc = "if reasonable stdev and no motion in correlation rectangle, stabilize image across frames"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim resetImage As Boolean
        templateRect = New cv.Rect(src.Width / 2 - options.width / 2, src.Height / 2 - options.height / 2,
                                   options.width, options.height)

        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If task.firstPass Then lastFrame = src.Clone()

        dst2 = src.Clone

        Dim mean As cv.Scalar
        Dim stdev As cv.Scalar
        cv.Cv2.MeanStdDev(dst2(templateRect), mean, stdev)

        If stdev > options.minStdev Then
            Dim t = templateRect
            Dim w = t.Width + options.pad * 2
            Dim h = t.Height + options.pad * 2
            Dim x = Math.Abs(t.X - options.pad)
            Dim y = Math.Abs(t.Y - options.pad)
            searchRect = New cv.Rect(x, y, If(w < lastFrame.Width, w, lastFrame.Width - x - 1), If(h < lastFrame.Height, h, lastFrame.Height - y - 1))
            match.template = lastFrame(searchRect)
            match.Run(src(templateRect))

            If match.correlation > options.corrThreshold Then
                Dim maxLoc = New cv.Point(match.newCenter.X, match.newCenter.Y)
                shiftX = templateRect.X - maxLoc.X - searchRect.X
                shiftY = templateRect.Y - maxLoc.Y - searchRect.Y
                Dim x1 = If(shiftX < 0, Math.Abs(shiftX), 0)
                Dim y1 = If(shiftY < 0, Math.Abs(shiftY), 0)

                dst3.SetTo(0)

                Dim x2 = If(shiftX < 0, 0, shiftX)
                Dim y2 = If(shiftY < 0, 0, shiftY)
                stableRect = New cv.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
                Dim srcRect = New cv.Rect(x2, y2, stableRect.Width, stableRect.Height)
                stableRect = New cv.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
                src(srcRect).CopyTo(dst3(stableRect))
                Dim nonZero = dst3.CountNonZero / (dst3.Width * dst3.Height)
                If nonZero < (1 - options.lostMax) Then
                    labels(3) = "Lost pixels = " + Format(1 - nonZero, "00%")
                    resetImage = True
                End If
                labels(3) = "Offset (x, y) = (" + CStr(shiftX) + "," + CStr(shiftY) + "), " + Format(nonZero, "00%") + " preserved, cc=" + Format(match.correlation, fmt2)
            Else
                labels(3) = "Below correlation threshold " + Format(options.corrThreshold, fmt2) + " with " +
                            Format(match.correlation, fmt2)
                resetImage = True
            End If
        Else
            labels(3) = "Correlation rectangle stdev is " + Format(stdev(0), "00") + " - too low"
            resetImage = True
        End If

        If resetImage Then
            src.CopyTo(lastFrame)
            dst3 = lastFrame.Clone
        End If
        If standaloneTest() Then dst3.Rectangle(templateRect, white, 1) ' when not standaloneTest(), traceName doesn't want artificial rectangle.
    End Sub
End Class









Public Class XO_Stabilizer_BasicsTest : Inherits TaskParent
    Dim random As New PhaseCorrelate_RandomInput
    Dim stable As New XO_Stabilizer_Basics
    Public Sub New()
        labels(2) = "Unstable input to Stabilizer_Basics"
        desc = "Test the Stabilizer_Basics with random movement"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)

        random.Run(src)
        stable.Run(random.dst3.Clone)

        dst2 = stable.dst2
        dst3 = stable.dst3
        If standaloneTest() Then dst3.Rectangle(stable.templateRect, white, 1)
        labels(3) = stable.labels(3)
    End Sub
End Class






' https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
Public Class XO_Stabilizer_OpticalFlow : Inherits TaskParent
    Public inputFeat As New List(Of cv.Point2f)
    Public borderCrop = 30
    Dim sumScale As cv.Mat, sScale As cv.Mat, features1 As cv.Mat
    Dim errScale As cv.Mat, qScale As cv.Mat, rScale As cv.Mat
    Public Sub New()
        desc = "Stabilize video with a Kalman filter.  Shake camera to see image edges appear.  This is not really working!"
        labels(2) = "Stabilized Image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim vert_Border = borderCrop * src.Rows / src.Cols
        If task.optionsChanged Then
            errScale = New cv.Mat(New cv.Size(1, 5), cv.MatType.CV_64F, 1)
            qScale = New cv.Mat(New cv.Size(1, 5), cv.MatType.CV_64F, 0.004)
            rScale = New cv.Mat(New cv.Size(1, 5), cv.MatType.CV_64F, 0.5)
            sumScale = New cv.Mat(New cv.Size(1, 5), cv.MatType.CV_64F, 0)
            sScale = New cv.Mat(New cv.Size(1, 5), cv.MatType.CV_64F, 0)
        End If

        dst2 = src

        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        inputFeat = New List(Of cv.Point2f)(task.features)
        features1 = cv.Mat.FromPixelData(inputFeat.Count, 1, cv.MatType.CV_32FC2, inputFeat.ToArray)

        Static lastFrame As cv.Mat = src.Clone()
        If task.frameCount > 0 Then
            Dim features2 = New cv.Mat
            Dim status As New cv.Mat
            Dim err As New cv.Mat
            Dim winSize As New cv.Size(3, 3)
            cv.Cv2.CalcOpticalFlowPyrLK(src, lastFrame, features1, features2, status, err, winSize, 3, term, cv.OpticalFlowFlags.None)
            lastFrame = src.Clone()

            Dim commonPoints = New List(Of cv.Point2f)
            Dim lastFeatures As New List(Of cv.Point2f)
            For i = 0 To status.Rows - 1
                If status.Get(Of Byte)(i, 0) Then
                    Dim pt1 = features1.Get(Of cv.Point2f)(i, 0)
                    Dim pt2 = features2.Get(Of cv.Point2f)(i, 0)
                    Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                    If length < 10 Then
                        commonPoints.Add(pt1)
                        lastFeatures.Add(pt2)
                    End If
                End If
            Next

            If commonPoints.Count = 0 Or lastFeatures.Count = 0 Then Exit Sub ' nothing to work on...
            Dim affine = cv.Cv2.GetAffineTransform(commonPoints.ToArray, lastFeatures.ToArray)

            Dim dx = affine.Get(Of Double)(0, 2)
            Dim dy = affine.Get(Of Double)(1, 2)
            Dim da = Math.Atan2(affine.Get(Of Double)(1, 0), affine.Get(Of Double)(0, 0))
            Dim ds_x = affine.Get(Of Double)(0, 0) / Math.Cos(da)
            Dim ds_y = affine.Get(Of Double)(1, 1) / Math.Cos(da)
            Dim saveDX = dx, saveDY = dy, saveDA = da

            Dim text = "Original dx = " + Format(dx, fmt2) + vbCrLf + " dy = " + Format(dy, fmt2) + vbCrLf + " da = " + Format(da, fmt2)
            SetTrueText(text)

            Dim sx = ds_x, sy = ds_y

            Dim delta As cv.Mat = cv.Mat.FromPixelData(5, 1, cv.MatType.CV_64F, New Double() {ds_x, ds_y, da, dx, dy})
            cv.Cv2.Add(sumScale, delta, sumScale)

            Dim diff As New cv.Mat
            cv.Cv2.Subtract(sScale, sumScale, diff)

            da += diff.Get(Of Double)(2, 0)
            dx += diff.Get(Of Double)(3, 0)
            dy += diff.Get(Of Double)(4, 0)
            If Math.Abs(dx) > 50 Then dx = saveDX
            If Math.Abs(dy) > 50 Then dy = saveDY
            If Math.Abs(da) > 50 Then da = saveDA

            text = "dx = " + Format(dx, fmt2) + vbCrLf + " dy = " + Format(dy, fmt2) + vbCrLf + " da = " + Format(da, fmt2)
            SetTrueText(text, New cv.Point(10, 100))

            Dim smoothedMat = New cv.Mat(2, 3, cv.MatType.CV_64F)
            smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 2, dx)
            smoothedMat.Set(Of Double)(1, 2, dy)

            Dim smoothedFrame = task.color.WarpAffine(smoothedMat, src.Size())
            smoothedFrame = smoothedFrame(New cv.Range(vert_Border, smoothedFrame.Rows - vert_Border), New cv.Range(borderCrop, smoothedFrame.Cols - borderCrop))
            dst3 = smoothedFrame.Resize(src.Size())

            For i = 0 To commonPoints.Count - 1
                DrawCircle(dst2, commonPoints.ElementAt(i), task.DotSize + 3, cv.Scalar.Red)
                DrawCircle(dst2, lastFeatures.ElementAt(i), task.DotSize + 1, cv.Scalar.Blue)
            Next
        End If
        inputFeat = Nothing ' show that we consumed the current set of features.
    End Sub
End Class










Public Class XO_Stabilizer_CornerPoints : Inherits TaskParent
    Public basics As New Stable_Basics
    Public features As New List(Of cv.Point2f)
    Dim ul As cv.Rect, ur As cv.Rect, ll As cv.Rect, lr As cv.Rect
    Public Sub New()
        desc = "Track the FAST feature points found in the corners of the BGR image."
    End Sub
    Private Sub getKeyPoints(src As cv.Mat, r As cv.Rect)
        Dim kpoints() As cv.KeyPoint = cv.Cv2.FAST(src(r), task.FASTthreshold, True)
        For Each kp In kpoints
            features.Add(New cv.Point2f(kp.Pt.X + r.X, kp.Pt.Y + r.Y))
        Next
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            Dim size = task.brickSize
            ul = New cv.Rect(0, 0, size, size)
            ur = New cv.Rect(dst2.Width - size, 0, size, size)
            ll = New cv.Rect(0, dst2.Height - size, size, size)
            lr = New cv.Rect(dst2.Width - size, dst2.Height - size, size, size)
        End If

        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        features.Clear()
        getKeyPoints(src, ul)
        getKeyPoints(src, ur)
        getKeyPoints(src, ll)
        getKeyPoints(src, lr)

        dst2.SetTo(0)
        For Each pt In features
            DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Yellow)
        Next
        labels(2) = "There were " + CStr(features.Count) + " key points detected"
    End Sub
End Class




Public Class XO_MatchRect_Basics : Inherits TaskParent
    Public match As New Match_Basics
    Public rectInput As New cv.Rect
    Public rectOutput As New cv.Rect
    Dim rectSave As New cv.Rect
    Public Sub New()
        desc = "Track a RedCloud rectangle using MatchTemplate.  Click on a cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then match.correlation = 0
        If match.correlation < task.fCorrThreshold Or rectSave <> rectInput Or task.mouseClickFlag Then
            If standalone Then
                dst2 = runRedC(src, labels(2)).Clone
                rectInput = task.rcD.rect
            End If
            rectSave = rectInput
            match.template = src(rectInput).Clone
        End If

        match.Run(src)
        rectOutput = match.newRect

        If standalone Then
            If task.heartBeat Then dst3.SetTo(0)
            DrawRect(dst3, rectOutput)
        End If
    End Sub
End Class




Public Class XO_MatchRect_RedCloud : Inherits TaskParent
    Dim matchRect As New XO_MatchRect_Basics
    Public Sub New()
        desc = "Track a RedCloud cell using MatchTemplate."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))
        task.ClickPoint = task.rcD.maxDist

        If task.heartBeat Then matchRect.rectInput = task.rcD.rect

        matchRect.Run(src)
        If standalone Then
            If task.heartBeat Then dst3.SetTo(0)
            DrawRect(dst3, matchRect.rectOutput)
        End If
        labels(2) = "MatchLine correlation = " + Format(matchRect.match.correlation, fmt3) +
                    " - Red = current gravity vector, yellow is matchLine output"
    End Sub
End Class







Public Class XO_MatchLine_Basics : Inherits TaskParent
    Public lpInput As lpData
    Public lpOutput As lpData
    Dim match As New Match_Basics
    Public correlation1 As Single
    Public correlation2 As Single
    Public Sub New()
        desc = "Get the end points of the gravity RGB vector and compare them to the original template."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then lpInput = task.lineLongest
        Static lastImage = task.gray.Clone

        Dim rect = task.gridRects(lpInput.gridIndex1)
        match.template = task.gray(rect)
        match.Run(lastImage(task.gridNabeRects(lpInput.gridIndex1)))
        correlation1 = match.correlation
        Dim offsetX = match.newRect.TopLeft.X - rect.TopLeft.X
        Dim offsetY = match.newRect.TopLeft.Y - rect.TopLeft.Y
        Dim p1 = New cv.Point(lpInput.p1.X + offsetX, lpInput.p1.Y + offsetY)

        rect = task.gridRects(lpInput.gridIndex2)
        match.template = task.gray(rect)
        match.Run(lastImage(task.gridNabeRects(lpInput.gridIndex2)))
        correlation2 = match.correlation
        offsetX = match.newRect.TopLeft.X - rect.TopLeft.X
        offsetY = match.newRect.TopLeft.Y - rect.TopLeft.Y
        Dim p2 = New cv.Point(lpInput.p1.X + offsetX, lpInput.p1.Y + offsetY)

        lpOutput = New lpData(p1, p2)

        If standaloneTest() Then
            dst2 = src.Clone
            DrawLine(dst2, lpInput, task.highlight)
            DrawLine(dst2, lpOutput, task.highlight)
        End If
    End Sub
End Class





Public Class XO_MatchLine_Test : Inherits TaskParent
    Public cameraMotionProxy As New lpData
    Dim match As New XO_MatchLine_Basics
    Public Sub New()
        desc = "Find and track the longest line by matching line bricks."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then task.lines.lpList.Clear()

        dst2 = src.Clone
        If task.lines.lpList.Count > 0 Then
            cameraMotionProxy = task.lines.lpList(0)
            match.lpInput = cameraMotionProxy
            match.Run(src)
            dst1 = match.dst2

            labels(2) = "EndPoint1 correlation:  " + Format(match.correlation1, fmt3) + vbTab +
                        "EndPoint2 correlation:  " + Format(match.correlation1, fmt3)

            If match.correlation1 < task.fCorrThreshold Or task.frameCount < 10 Or
               match.correlation2 < task.fCorrThreshold Then

                task.motionMask.SetTo(255) ' force a complete line detection
                task.lines.Run(src.Clone)
                If task.lines.lpList.Count = 0 Then Exit Sub

                match.lpInput = task.lines.lpList(0)
                match.Run(src)
            End If
        End If

        dst3 = task.lines.dst3
        labels(3) = task.lines.labels(3)

        dst2.Line(cameraMotionProxy.p1, cameraMotionProxy.p2, task.highlight, task.lineWidth, task.lineType)
    End Sub
End Class










Public Class XO_Match_Points : Inherits TaskParent
    Public ptx As New List(Of cv.Point2f)
    Public correlation As New List(Of Single)
    Public mPoint As New Match_Point
    Public Sub New()
        labels(2) = "Rectangle shown is the search rectangle."
        desc = "Track the selected points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.firstPass Then mPoint.target = src.Clone

        If standaloneTest() Then
            ptx = New List(Of cv.Point2f)(task.features)
            SetTrueText("Move camera around to watch the point being tracked", 3)
        End If

        dst2 = src.Clone
        correlation.Clear()
        For i = 0 To ptx.Count - 1
            mPoint.pt = ptx(i)
            mPoint.Run(src)
            correlation.Add(mPoint.correlation)
            ptx(i) = mPoint.pt
            DrawPolkaDot(ptx(i), dst2)
        Next
        mPoint.target = src.Clone
    End Sub
End Class







Public Class Feature_PointTracker : Inherits TaskParent
    Dim flow As New Font_FlowText
    Dim mPoints As New XO_Match_Points
    Public Sub New()
        flow.parentData = Me
        flow.dst = 3
        labels(3) = "Correlation coefficients for each remaining cell"
        desc = "Use the top X goodFeatures and then use matchTemplate to find track them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim pad = task.brickSize / 2
        strOut = ""
        If mPoints.ptx.Count <= 3 Then
            mPoints.ptx.Clear()
            For Each pt In task.features
                mPoints.ptx.Add(pt)
                Dim rect = ValidateRect(New cv.Rect(pt.X - pad, pt.Y - pad, task.brickSize, task.brickSize))
            Next
            strOut = "Restart tracking -----------------------------------------------------------------------------" + vbCrLf
        End If
        mPoints.Run(src)

        dst2 = src.Clone
        For i = mPoints.ptx.Count - 1 To 0 Step -1
            If mPoints.correlation(i) > task.fCorrThreshold Then
                DrawCircle(dst2, mPoints.ptx(i), task.DotSize, task.highlight)
                strOut += Format(mPoints.correlation(i), fmt3) + ", "
            Else
                mPoints.ptx.RemoveAt(i)
            End If
        Next
        If standaloneTest() Then
            flow.nextMsg = strOut
            flow.Run(src)
        End If

        labels(2) = "Of the " + CStr(task.features.Count) + " input points, " + CStr(mPoints.ptx.Count) +
                    " points were tracked with correlation above " + Format(task.fCorrThreshold, fmt2)
    End Sub
End Class





Public Class XO_Line_LongestTest : Inherits TaskParent
    Public matchBrick As New Match_Brick
    Dim lp As New lpData
    Public Sub New()
        desc = "Identify a line by matching each of the points to the previous image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim threshold = task.fCorrThreshold
        Dim lplist = task.lines.lpList
        If lplist.Count = 0 Then
            SetTrueText("There are no lines present in the image.", 3)
            Exit Sub
        End If

        task.lineLongestChanged = False
        ' camera is often warming up for the first few images.
        If task.frameCount < 10 Or task.heartBeat Then
            lp = lplist(0)
            task.lineLongestChanged = True
        End If

        matchBrick.gridIndex = lp.gridIndex1
        matchBrick.Run(emptyMat)
        Dim correlation1 = matchBrick.correlation
        Dim p1 = New cv.Point(lp.p1.X + matchBrick.deltaX, lp.p1.Y + matchBrick.deltaY)

        strOut = matchBrick.labels(2) + vbCrLf
        labels(2) = matchBrick.labels(2) + vbTab

        matchBrick.gridIndex = lp.gridIndex2
        matchBrick.Run(emptyMat)
        Dim correlation2 = matchBrick.correlation
        Dim p2 = New cv.Point(lp.p2.X + matchBrick.deltaX, lp.p2.Y + matchBrick.deltaY)

        strOut += matchBrick.labels(2) + vbCrLf
        labels(2) += ", " + matchBrick.labels(2)

        If correlation1 >= threshold And correlation2 >= threshold Then
            lp = New lpData(p1, p2)
            task.lineLongestChanged = False
        Else
            task.lineLongestChanged = True
        End If

        If standaloneTest() Then
            dst2 = src
            DrawLine(dst2, lp)
            DrawRect(dst2, lp.rect)
            dst3 = task.lines.dst2
        End If

        ' task.lineLongest = lp
        Static strList As New List(Of String)
        strList.Add(strOut)
        If strList.Count > 10 Then strList.RemoveAt(0)
        strOut = ""
        For Each strNext In strList
            strOut += strNext
        Next
        SetTrueText(strOut, 3)
    End Sub
End Class








Public Class XO_Line_Matching2 : Inherits TaskParent
    Public match As New Match_Basics
    Public Sub New()
        desc = "For each line from the last frame, find its correlation to the current frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim correlations As New List(Of Single)
        Static lpLast = New List(Of lpData)(task.lines.lpList)
        For Each lp In lpLast
            match.template = task.gray(lp.rect)
            match.Run(task.gray.Clone)
            correlations.Add(match.correlation)
        Next

        dst2 = task.lines.dst2

        labels(2) = "Mean correlation of all the lines is " + Format(correlations.Average, fmt3)
        labels(3) = "Min/Max correlation = " + Format(correlations.Min, fmt3) + "/" + Format(correlations.Max, fmt3)
        lpLast = New List(Of lpData)(task.lines.lpList)
    End Sub
End Class





Public Class XO_Line_Gravity : Inherits TaskParent
    Dim match As New Match_Basics
    Public lp As lpData
    Public Sub New()
        desc = "Find the longest RGB line that is parallel to gravity"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lines.lpList
        If lplist.Count = 0 Then
            SetTrueText("There are no lines present in the image.", 3)
            Exit Sub
        End If

        ' camera is often warming up for the first few images.
        If match.correlation < task.fCorrThreshold Or task.frameCount < 10 Or lp Is Nothing Then
            lp = lplist(0)
            For Each lp In lplist
                If Math.Abs(task.lineGravity.angle - lp.angle) < task.angleThreshold Then Exit For
            Next
            match.template = src(lp.rect)
        End If

        If Math.Abs(task.lineGravity.angle - lp.angle) >= task.angleThreshold Then
            lp = Nothing
            Exit Sub
        End If

        match.Run(src.Clone)

        If match.correlation < task.fCorrThreshold Then
            If lplist.Count > 1 Then
                Dim histogram As New cv.Mat
                cv.Cv2.CalcHist({task.lines.dst1(lp.rect)}, {0}, emptyMat, histogram, 1, {lplist.Count},
                                 New cv.Rangef() {New cv.Rangef(1, lplist.Count)})

                Dim histArray(histogram.Total - 1) As Single
                Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

                Dim histList = histArray.ToList
                ' pick the lp that has the most pixels in the lp.rect.
                lp = lplist(histList.IndexOf(histList.Max))
                match.template = src(lp.rect)
                match.correlation = 1
            Else
                match.correlation = 0 ' force a restart
            End If
        Else
            Dim deltaX = match.newRect.X - lp.rect.X
            Dim deltaY = match.newRect.Y - lp.rect.Y
            Dim p1 = New cv.Point(lp.p1.X + deltaX, lp.p1.Y + deltaY)
            Dim p2 = New cv.Point(lp.p2.X + deltaX, lp.p2.Y + deltaY)
            lp = New lpData(p1, p2)
        End If

        If standaloneTest() Then
            dst2 = src
            dst2.Rectangle(lp.rect, task.highlight, task.lineWidth)
            DrawLine(dst2, lp.p1, lp.p2)
        End If

        labels(2) = "Selected line has a correlation of " + Format(match.correlation, fmt3) + " with the previous frame."
    End Sub
End Class






Public Class XO_Line_ExtendLineTest : Inherits TaskParent
    Public Sub New()
        labels = {"", "", "Random Line drawn", ""}
        desc = "Test lpData constructor with random values to make sure lines are extended properly"
    End Sub

    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            Dim p1 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p2 = New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))

            Dim lp = New lpData(p1, p2)
            dst2 = src
            DrawLine(dst2, lp.ep1, lp.ep2, task.highlight)
            DrawCircle(dst2, p1, task.DotSize + 2, cv.Scalar.Red)
            DrawCircle(dst2, p2, task.DotSize + 2, cv.Scalar.Red)
        End If
    End Sub
End Class




Public Class XO_Line_ExtendAll : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public Sub New()
        labels = {"", "", "Image output from Line_Core", "The extended line for each line found in Line_Core"}
        desc = "Create a list of all the extended lines in an image"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2

        dst3 = src.Clone
        lpList.Clear()
        For Each lp In task.lines.lpList
            DrawLine(dst3, lp.ep1, lp.ep2, task.highlight)
            lpList.Add(New lpData(lp.ep1, lp.ep2))
        Next
    End Sub
End Class










Public Class XO_Line_Intercepts : Inherits TaskParent
    Public extended As New XO_Line_ExtendLineTest
    Public p1List As New List(Of cv.Point2f)
    Public p2List As New List(Of cv.Point2f)
    Public options As New Options_Intercepts
    Public intercept As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public topIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public botIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public leftIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public rightIntercepts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public interceptArray = {topIntercepts, botIntercepts, leftIntercepts, rightIntercepts}
    Public Sub New()
        labels(2) = "Highlight line x- and y-intercepts.  Move mouse over the image."
        desc = "Show lines with similar y-intercepts"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If task.lines.lpList.Count = 0 Then Exit Sub

        dst2 = src
        p1List.Clear()
        p2List.Clear()
        intercept = interceptArray(options.selectedIntercept)
        topIntercepts.Clear()
        botIntercepts.Clear()
        leftIntercepts.Clear()
        rightIntercepts.Clear()
        Dim index As Integer
        For Each lp In task.lines.lpList
            Dim minXX = Math.Min(lp.p1.X, lp.p2.X)
            If lp.p1.X <> minXX Then ' leftmost point is always in p1
                Dim tmp = lp.p1
                lp.p1 = lp.p2
                lp.p2 = tmp
            End If

            p1List.Add(lp.p1)
            p2List.Add(lp.p2)
            DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)

            Dim saveP1 = lp.p1, saveP2 = lp.p2

            If lp.ep1.X = 0 Then leftIntercepts.Add(saveP1.Y, index)
            If lp.ep1.Y = 0 Then topIntercepts.Add(saveP1.X, index)
            If lp.ep1.X = dst2.Width Then rightIntercepts.Add(saveP1.Y, index)
            If lp.ep1.Y = dst2.Height Then botIntercepts.Add(saveP1.X, index)

            If lp.ep2.X = 0 Then leftIntercepts.Add(saveP2.Y, index)
            If lp.ep2.Y = 0 Then topIntercepts.Add(saveP2.X, index)
            If lp.ep2.X = dst2.Width Then rightIntercepts.Add(saveP2.Y, index)
            If lp.ep2.Y = dst2.Height Then botIntercepts.Add(saveP2.X, index)
            index += 1
        Next

        If standaloneTest() Then
            For Each inter In intercept
                If Math.Abs(options.mouseMovePoint - inter.Key) < options.interceptRange Then
                    DrawLine(dst2, p1List(inter.Value), p2List(inter.Value), cv.Scalar.Blue)
                End If
            Next
        End If
    End Sub
End Class






Public Class XO_Line_BasicsNoAging : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public lpRectMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public rawLines As New Line_Raw
    Public Sub New()
        desc = "Retain line from earlier image if not in motion mask.  If new line is in motion mask, add it."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            lpList.Clear()
            task.motionMask.SetTo(255)
        End If

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)

        rawLines.Run(src)
        dst3 = rawLines.dst2
        labels(3) = rawLines.labels(2)

        For Each lp In rawLines.lpList
            sortlines.Add(lp.length, lp)
        Next

        lpList.Clear()
        dst2 = src
        lpRectMap.SetTo(0)
        For Each lp In sortlines.Values
            lpList.Add(lp)
            DrawLine(dst2, lp.p1, lp.p2)
            lpRectMap.Line(lp.p1, lp.p2, sortlines.Values.IndexOf(lp) + 1, task.lineWidth * 3, cv.LineTypes.Link8)

            If standaloneTest() Then
                dst2.Line(lp.p1, lp.p2, task.highlight, 10, cv.LineTypes.Link8)
            End If
            If lpList.Count >= task.FeatureSampleSize Then Exit For
        Next

        If standaloneTest() Then dst1 = ShowPalette(lpRectMap)
        labels(2) = "Of the " + CStr(rawLines.lpList.Count) + " raw lines found, shown below are the " + CStr(lpList.Count) + " longest."
    End Sub
End Class





Public Class XO_Line_ViewLeftRight : Inherits TaskParent
    Dim lines As New Line_Core
    Dim linesRaw As New Line_Raw
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        desc = "Find lines in the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(task.leftView)
        dst2.SetTo(0)
        For Each lp In task.lines.lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth)
        Next
        labels(2) = lines.labels(2)

        linesRaw.Run(task.rightView)
        dst3 = linesRaw.dst2
        labels(3) = linesRaw.labels(2)
    End Sub
End Class







Public Class XO_Swarm_KNN : Inherits TaskParent
    Dim swarm As New Swarm_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Use KNN to find the nearest point to an endpoint and connect the 2 lines with a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        swarm.options.Run()
        dst2 = task.lines.dst2

        dst3.SetTo(0)
        swarm.knn.queries.Clear()
        For Each lp In task.lines.lpList
            swarm.knn.queries.Add(lp.p1)
            swarm.knn.queries.Add(lp.p2)
            DrawLine(dst3, lp.p1, lp.p2, 255)
        Next
        swarm.knn.trainInput = New List(Of cv.Point2f)(swarm.knn.queries)
        swarm.knn.Run(src)

        dst3 = swarm.DrawLines().Clone
        labels(2) = task.lines.labels(2)
    End Sub
End Class








Public Class XO_Line_MatchGravity : Inherits TaskParent
    Public gLines As New List(Of lpData)
    Public Sub New()
        desc = "Find all the lines similar to gravity."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = task.lines.dst3
        labels(3) = task.lines.labels(3)

        gLines.Clear()
        dst2 = src.Clone
        For Each lp In task.lines.lpList
            If Math.Abs(task.lineGravity.angle - lp.angle) < 2 Then
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth + 1, task.lineType)
                gLines.Add(lp)
            End If
        Next

        If gLines.Count = 0 Then
            labels(2) = "There were no lines parallel to gravity in the RGB image."
        Else
            labels(2) = "Of the " + CStr(gLines.Count) + " lines found, the best line parallel to gravity was " +
                       CStr(CInt(gLines(0).length)) + " pixels in length."
        End If
    End Sub
End Class






Public Class XO_Line_RawSubset : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public subsetRect As cv.Rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
    Public rawLines As New Line_Raw
    Public Sub New()
        task.drawRect = New cv.Rect(25, 25, 25, 25)
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then subsetRect = task.drawRect
        rawLines.Run(src(subsetRect))

        lpList.Clear()
        dst2 = task.lines.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each lp In rawLines.lpList
            dst2(subsetRect).Line(lp.p1, lp.p2, task.highlight, task.lineWidth * 3, task.lineType)
            lpList.Add(lp)
        Next
        labels(2) = CStr(lpList.Count) + " lines were detected in src(subsetRect)"
    End Sub
End Class





Public Class XO_Line_TrigHorizontal : Inherits TaskParent
    Public horizList As New List(Of lpData)
    Public Sub New()
        desc = "Find all the Horizontal lines with horizon vector"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src

        Dim p1 = task.lineHorizon.p1, p2 = task.lineHorizon.p2
        Dim sideOpposite = p2.Y - p1.Y
        If p1.X = 0 Then sideOpposite = p1.Y - p2.Y
        Dim hAngle = Math.Atan(sideOpposite / dst2.Width) * 57.2958

        horizList.Clear()
        For Each lp In task.lines.lpList
            If Math.Abs(task.lineHorizon.angle - lp.angle) < task.angleThreshold Then
                DrawLine(dst2, lp.p1, lp.p2)
                horizList.Add(lp)
            End If
        Next
        labels(2) = "There are " + CStr(horizList.Count) + " lines similar to the horizon " + Format(hAngle, fmt1) + " degrees"
    End Sub
End Class




Public Class XO_Line_TrigVertical : Inherits TaskParent
    Public vertList As New List(Of lpData)
    Public Sub New()
        desc = "Find all the vertical lines with gravity vector"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src

        Dim p1 = task.lineGravity.p1, p2 = task.lineGravity.p2
        Dim sideOpposite = p2.X - p1.X
        If p1.Y = 0 Then sideOpposite = p1.X - p2.X
        Dim gAngle = Math.Atan(sideOpposite / dst2.Height) * 57.2958

        vertList.Clear()
        For Each lp In task.lines.lpList
            If Math.Abs(task.lineGravity.angle - lp.angle) < task.angleThreshold Then
                DrawLine(dst2, lp.p1, lp.p2)
                vertList.Add(lp)
            End If
        Next
        labels(2) = "There are " + CStr(vertList.Count) + " lines similar to the Gravity " + Format(gAngle, fmt1) + " degrees"
    End Sub
End Class







Public Class XO_Line_VerticalHorizontalRaw : Inherits TaskParent
    Dim verts As New XO_Line_TrigVertical
    Dim horiz As New XO_Line_TrigHorizontal
    Public vertList As New List(Of lpData)
    Public horizList As New List(Of lpData)
    Public Sub New()
        task.gOptions.LineWidth.Value = 2
        labels(3) = "Vertical lines are in yellow and horizontal lines in red."
        desc = "Highlight both vertical and horizontal lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        verts.Run(src)
        horiz.Run(src)

        Dim vList As New SortedList(Of Integer, lpData)(New compareAllowIdenticalIntegerInverted)
        Dim hList As New SortedList(Of Integer, lpData)(New compareAllowIdenticalIntegerInverted)

        dst3.SetTo(0)
        For Each lp In verts.vertList
            vList.Add(lp.length, lp)
            DrawLine(dst2, lp.p1, lp.p2, task.highlight)
            DrawLine(dst3, lp.p1, lp.p2, task.highlight)
        Next

        For Each lp In horiz.horizList
            hList.Add(lp.length, lp)
            DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Red)
            DrawLine(dst3, lp.p1, lp.p2, cv.Scalar.Red)
        Next

        vertList = New List(Of lpData)(vList.Values)
        horizList = New List(Of lpData)(hList.Values)
        labels(2) = "Number of lines identified (vertical/horizontal): " + CStr(vList.Count) + "/" + CStr(hList.Count)
    End Sub
End Class







Public Class XO_Line_FindNearest : Inherits TaskParent
    Public lpInput As lpData
    Public lpOutput As lpData
    Public distance As Single
    Public Sub New()
        desc = "Find the line that is closest to the input line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then lpInput = task.lineLongest
        Dim lpList = task.lines.lpList
        If lpList.Count = 0 Then Exit Sub

        Dim sortDistance As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        For Each lp In lpList
            sortDistance.Add(lpInput.center.DistanceTo(lp.center), lp.index)
        Next

        lpOutput = lpList(sortDistance.ElementAt(0).Value)

        If standaloneTest() Then
            dst2 = src
            DrawLine(dst2, lpOutput.p1, lpOutput.p2)
            labels(2) = "Distance = " + Format(sortDistance.ElementAt(0).Key, fmt1)
        End If
    End Sub
End Class







Public Class XO_KNN_LongestLine : Inherits TaskParent
    Public lp As lpData
    Dim knn As New KNN_NNBasics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        OptionParent.FindSlider("KNN Dimension").Value = 6
        desc = "Track the longest line"
    End Sub
    Private Sub prepEntry(knnList As List(Of Single), lpNext As lpData)
        Dim brick1 = task.grid.gridMap.Get(Of Integer)(lpNext.p1.Y, lpNext.p1.X)
        Dim brick2 = task.grid.gridMap.Get(Of Integer)(lpNext.p2.Y, lpNext.p2.X)
        knnList.Add(lpNext.p1.X)
        knnList.Add(lpNext.p1.Y)
        knnList.Add(lpNext.p2.X)
        knnList.Add(lpNext.p2.Y)
        knnList.Add(brick1)
        knnList.Add(brick2)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lines.lpList
        If lplist.Count = 0 Then Exit Sub

        If standalone And task.heartBeatLT Then lp = lplist(0)

        knn.trainInput.Clear()
        For Each lpNext In lplist
            prepEntry(knn.trainInput, lpNext)
        Next

        knn.queries.Clear()
        prepEntry(knn.queries, lp)

        knn.Run(emptyMat)

        lp = lplist(knn.result(0, 0))
        dst2 = src
        dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth + 1, task.lineType)

        dst3 = task.lines.dst3
        labels(3) = task.lines.labels(3)

        dst1 = ShowPaletteNoZero(task.lines.lineCore.lpRectMap)
    End Sub
End Class







Public Class XO_KNN_BoundingRect : Inherits TaskParent
    Public lp As lpData
    Dim rawlines As New Line_Raw
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find the line with the largest bounding rectangle."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lines.lpList
        If lplist.Count = 0 Then Exit Sub

        If standalone And task.heartBeatLT Then
            Dim sortRects As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
            For Each lpNext In lplist
                sortRects.Add(lpNext.rect.Width * lpNext.rect.Height, lpNext.index)
            Next
            lp = lplist(sortRects.ElementAt(0).Value)
        End If

        dst1 = ShowPaletteNoZero(task.lines.lineCore.lpRectMap)
        DrawCircle(dst1, lp.center)

        Dim index = task.lines.lineCore.lpRectMap.Get(Of Byte)(lp.center.Y, lp.center.X)
        If index > 0 Then lp = lplist(index - 1)
        dst2 = src
        dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth + 1, task.lineType)

        dst3 = task.lines.dst3
        labels(3) = task.lines.labels(3)
    End Sub
End Class







' https://stackoverflow.com/questions/7446126/opencv-2d-line-intersection-helper-function
Public Class XO_Line_IntersectionPT : Inherits TaskParent
    Public p1 As cv.Point2f, p2 As cv.Point2f, p3 As cv.Point2f, p4 As cv.Point2f
    Public intersectionPoint As cv.Point2f
    Public Sub New()
        desc = "Determine if 2 lines intersect, where the point is, and if that point is in the image."
    End Sub

    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            p1 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p2 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p3 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            p4 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
        End If

        intersectionPoint = Line_Intersection.IntersectTest(p1, p2, p3, p4)
        intersectionPoint = Line_Intersection.IntersectTest(New lpData(p1, p2), New lpData(p3, p4))

        dst2.SetTo(0)
        dst2.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        dst2.Line(p3, p4, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        If intersectionPoint <> New cv.Point2f Then
            DrawCircle(dst2, intersectionPoint, task.DotSize + 4, white)
            labels(2) = "Intersection point = " + CStr(CInt(intersectionPoint.X)) + " x " + CStr(CInt(intersectionPoint.Y))
        Else
            labels(2) = "Parallel!!!"
        End If
        If intersectionPoint.X < 0 Or intersectionPoint.X > dst2.Width Or intersectionPoint.Y < 0 Or intersectionPoint.Y > dst2.Height Then
            labels(2) += " (off screen)"
        End If
    End Sub
End Class








Public Class XO_Line_Grid : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public rawLines As New Line_Raw
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "find the lines in each grid rectangle"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = src
        dst2.SetTo(0)
        For Each rect In task.gridNabeRects
            rawLines.Run(src(rect))
            For Each lp In rawLines.lpList
                dst2(rect).Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
                DrawLine(dst3, lp.p1, lp.p2)
                lpList.Add(lp)
            Next
        Next
    End Sub
End Class









Public Class XO_Line_GravityToLongest : Inherits TaskParent
    Dim kalman As New Kalman_Basics
    Dim matchLine As New XO_MatchLine_Basics
    Public Sub New()
        desc = "Highlight both vertical and horizontal lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim gravityDelta As Single = task.lineGravity.ep1.X - task.lineGravity.ep2.X

        kalman.kInput = {gravityDelta}
        kalman.Run(emptyMat)
        gravityDelta = kalman.kOutput(0)

        matchLine.lpInput = Nothing
        For Each lp In task.lines.lpList
            If Math.Abs(lp.angle) > 45 Then
                matchLine.lpInput = lp
                Exit For
            End If
        Next
        If matchLine.lpInput Is Nothing Then Exit Sub
        matchLine.Run(src)
        dst2 = matchLine.dst2
        dst3 = task.lines.lineCore.rawLines.dst2
    End Sub
End Class











Public Class XO_Line_GravityToAverage : Inherits TaskParent
    Public vertList As New List(Of lpData)
    Public Sub New()
        desc = "Highlight both vertical and horizontal lines - not terribly good..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim gravityDelta As Single = task.lineGravity.ep1.X - task.lineGravity.ep2.X

        dst2 = src
        If standalone Then dst3 = task.lines.dst2
        Dim deltaList As New List(Of Single)
        vertList.Clear()
        For Each lp In task.lines.lpList
            If Math.Abs(lp.angle) > 45 And Math.Sign(task.lineGravity.slope) = Math.Sign(lp.slope) Then
                Dim delta = lp.ep1.X - lp.ep2.X
                If Math.Abs(gravityDelta - delta) < task.gravityBasics.options.pixelThreshold Then
                    deltaList.Add(delta)
                    vertList.Add(lp)
                    DrawLine(dst2, lp.ep1, lp.ep2)
                    If standalone Then DrawLine(dst3, lp.p1, lp.p2, task.highlight)
                End If
            End If
        Next

        If task.heartBeat Then
            labels(3) = "Gravity offset at image edge = " + Format(gravityDelta, fmt3) + " and m = " +
                        Format(task.lineGravity.slope, fmt3)
            If deltaList.Count > 0 Then
                labels(2) = Format(gravityDelta, fmt3) + "/" + Format(deltaList.Average(), fmt3) + " gravity delta/line average delta"
            Else
                labels(2) = "No lines matched the gravity vector..."
            End If
        End If
    End Sub
End Class







Public Class XO_Line_Parallel : Inherits TaskParent
    Dim extendAll As New XO_Line_ExtendAll
    Dim knn As New KNN_Basics
    Public parList As New List(Of coinPoints)
    Public Sub New()
        labels = {"", "", "Image output from Line_Core", "Parallel extended lines"}
        desc = "Use KNN to find which lines are near each other and parallel"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        extendAll.Run(src)
        dst3 = extendAll.dst2

        knn.queries.Clear()
        For Each lp In extendAll.lpList
            knn.queries.Add(New cv.Point2f((lp.p1.X + lp.p2.X) / 2, (lp.p1.Y + lp.p2.Y) / 2))
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)

        If knn.queries.Count = 0 Then Exit Sub ' no input...possible in a dark room...

        knn.Run(src)
        dst2 = src.Clone
        parList.Clear()
        Dim checkList As New List(Of cv.Point)
        For i = 0 To knn.result.GetUpperBound(0) - 1
            For j = 0 To knn.queries.Count - 1
                Dim index = knn.result(i, j)
                If index >= extendAll.lpList.Count Or index < 0 Then Continue For
                Dim lp = extendAll.lpList(index)
                Dim elp = extendAll.lpList(i)
                Dim mid = knn.queries(i)
                Dim near = knn.trainInput(index)
                Dim distanceMid = mid.DistanceTo(near)
                Dim distance1 = lp.p1.DistanceTo(elp.p1)
                Dim distance2 = lp.p2.DistanceTo(elp.p2)
                If distance1 > distanceMid * 2 Then
                    distance1 = lp.p1.DistanceTo(elp.p2)
                    distance2 = lp.p2.DistanceTo(elp.p1)
                End If
                If distance1 < distanceMid * 2 And distance2 < distanceMid * 2 Then
                    Dim cp As coinPoints

                    Dim mps = task.lines.lpList(index)
                    cp.p1 = mps.p1
                    cp.p2 = mps.p2

                    mps = task.lines.lpList(i)
                    cp.p3 = mps.p1
                    cp.p4 = mps.p2

                    If checkList.Contains(cp.p1) = False And checkList.Contains(cp.p2) = False And checkList.Contains(cp.p3) = False And checkList.Contains(cp.p4) = False Then
                        If (cp.p1 = cp.p3 Or cp.p1 = cp.p4) And (cp.p2 = cp.p3 Or cp.p2 = cp.p4) Then
                            ' duplicate points...
                        Else
                            DrawLine(dst2, cp.p1, cp.p2, task.highlight)
                            DrawLine(dst2, cp.p3, cp.p4, cv.Scalar.Red)
                            parList.Add(cp)
                            checkList.Add(cp.p1)
                            checkList.Add(cp.p2)
                            checkList.Add(cp.p3)
                            checkList.Add(cp.p4)
                        End If
                    End If
                End If
            Next
        Next
        labels(2) = CStr(parList.Count) + " parallel lines were found in the image"
        labels(3) = CStr(extendAll.lpList.Count) + " lines were found in the image before finding the parallel lines"
    End Sub
End Class










Public Class XO_Line_Points : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public Sub New()
        desc = "Display end points of the lines and map them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2

        knn.queries.Clear()
        For Each lp In task.lines.lpList
            Dim rect = task.gridNabeRects(task.grid.gridMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
            dst2.Rectangle(rect, task.highlight, task.lineWidth)
            knn.queries.Add(lp.center)
        Next

        Static lastQueries As New List(Of cv.Point2f)(knn.queries)
        knn.trainInput = lastQueries


        knn.Run(emptyMat)

        dst3 = task.lines.dst3.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For i = 0 To knn.neighbors.Count - 1
            Dim p1 = knn.queries(i)
            Dim p2 = knn.trainInput(knn.neighbors(i)(0))
            dst3.Line(p1, p2, task.highlight, task.lineWidth + 3, task.lineType)
        Next

        lastQueries = New List(Of cv.Point2f)(knn.queries)
    End Sub
End Class





Public Class XO_Line_RawEPLines : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src)
        Dim tmplist As New List(Of lpData)
        dst3.SetTo(0)
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim p2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                If p1.X >= 0 And p1.X < dst2.Width And p1.Y >= 0 And p1.Y < dst2.Height And
                   p2.X >= 0 And p2.X < dst2.Width And p2.Y >= 0 And p2.Y < dst2.Height Then
                    p1 = lpData.validatePoint(p1)
                    p2 = lpData.validatePoint(p2)
                    Dim lp = New lpData(p1, p2)
                    lp.index = tmplist.Count
                    tmplist.Add(lp)
                    DrawLine(dst3, lp, white)
                End If
            End If
        Next

        Dim removeList As New List(Of Integer)
        For Each lp In tmplist
            Dim x1 = CInt(lp.ep1.X)
            Dim y1 = CInt(lp.ep1.Y)
            Dim x2 = CInt(lp.ep2.X)
            Dim y2 = CInt(lp.ep2.Y)
            For j = lp.index + 1 To tmplist.Count - 1
                If CInt(tmplist(j).ep1.X) <> x1 Then Continue For
                If CInt(tmplist(j).ep1.Y) <> y1 Then Continue For
                If CInt(tmplist(j).ep2.X) <> x2 Then Continue For
                If CInt(tmplist(j).ep2.Y) <> y2 Then Continue For
                If removeList.Contains(tmplist(j).index) = False Then removeList.Add(tmplist(j).index)
            Next
        Next

        lpList.Clear()
        For Each lp In tmplist
            If removeList.Contains(lp.index) = False Then lpList.Add(New lpData(lp.ep1, lp.ep2))
        Next

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth + 1, task.lineType)
        Next

        labels(2) = CStr(lpList.Count) + " highlighted lines were detected in the current frame. Others were too similar."
        labels(3) = "There were " + CStr(removeList.Count) + " coincident lines"
    End Sub
    Public Sub Close()
        ld.Dispose()
    End Sub
End Class









Public Class XO_Line_RawSorted : Inherits TaskParent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines in a subset " +
               "rectangle (provided externally)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Type <> cv.MatType.CV_8U Then src.ConvertTo(src, cv.MatType.CV_8U)

        Dim lines = ld.Detect(src)

        Dim sortlines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each v In lines
            If v(0) >= 0 And v(0) <= src.Cols And v(1) >= 0 And v(1) <= src.Rows And
               v(2) >= 0 And v(2) <= src.Cols And v(3) >= 0 And v(3) <= src.Rows Then
                Dim p1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim p2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                If p1.X >= 0 And p1.X < dst2.Width And p1.Y >= 0 And p1.Y < dst2.Height And
                   p2.X >= 0 And p2.X < dst2.Width And p2.Y >= 0 And p2.Y < dst2.Height Then
                    Dim lp = New lpData(p1, p2)
                    sortlines.Add(lp.length, lp)
                End If
            End If
        Next

        lpList.Clear()
        For Each lp In sortlines.Values
            lp.p1 = lpData.validatePoint(lp.p1)
            lp.p2 = lpData.validatePoint(lp.p2)
            lpList.Add(lp)
        Next

        If standaloneTest() Then
            dst2.SetTo(0)
            For Each lp In lpList
                dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            Next
        End If

        labels(2) = CStr(lpList.Count) + " lines were detected in the current frame"
    End Sub
    Public Sub Close()
        ld.Dispose()
    End Sub
End Class

Public Class XO_Python_Basics : Inherits TaskParent
    Public Function StartPython(arguments As String) As Boolean
        Dim pythonApp = New FileInfo(task.pythonTaskName)

        If pythonApp.Exists Then
            task.pythonProcess = New Process
            task.pythonProcess.StartInfo.FileName = "python"
            task.pythonProcess.StartInfo.WorkingDirectory = pythonApp.DirectoryName
            If arguments = "" Then
                task.pythonProcess.StartInfo.Arguments = """" + pythonApp.Name + """"
            Else
                task.pythonProcess.StartInfo.Arguments = """" + pythonApp.Name + """" + " " + arguments
            End If
            Debug.WriteLine("Starting Python with the following command:" + vbCrLf + task.pythonProcess.StartInfo.Arguments + vbCrLf)
            If task.showConsoleLog = False Then task.pythonProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            Try
                task.pythonProcess.Start()
            Catch ex As Exception
                MessageBox.Show("The python algorithm " + pythonApp.Name + " failed.  Is python in the path?")
            End Try
        Else
            If pythonApp.Name.EndsWith("Python_MemMap") Or pythonApp.Name.EndsWith("Python_Run") Then
                strOut = pythonApp.Name + " is a support algorithm for PyStream apps."
            Else
                strOut = pythonApp.FullName + " is missing."
            End If
            Return False
        End If
        Return True
    End Function
    Public Sub New()
        desc = "Access Python from OpenCVB - contains the startPython interface"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        SetTrueText("There is no output from " + traceName + ".  It contains the interface to python.")
    End Sub
End Class






Public Class XO_Python_Run : Inherits TaskParent
    Dim python As New XO_Python_Basics
    Public pyStream As New XO_Python_Stream
    Dim pythonApp As FileInfo
    Public Sub New()
        pythonApp = New FileInfo(task.pythonTaskName)
        If pythonApp.Name.EndsWith("_PS.py") Then
            pyStream = New XO_Python_Stream()
        Else
            python.StartPython("")
            If python.strOut <> "" Then SetTrueText(python.strOut)
        End If
        desc = "Run Python app: " + pythonApp.Name
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If pyStream IsNot Nothing Then
            pyStream.Run(src)
            dst2 = pyStream.dst2
            dst3 = pyStream.dst3
            labels(2) = "Output of Python Backend"
            labels(3) = "Second Output of Python Backend"
        Else
            If pythonApp.Name = "PyStream.py" Then
                SetTrueText("The PyStream.py algorithm is used by a wide variety of apps but has no output when run by itself.")
            End If
        End If
    End Sub
End Class






Public Class XO_Python_MemMap : Inherits TaskParent
    Dim python As New XO_Python_Basics
    Dim memMapWriter As MemoryMappedViewAccessor
    Dim memMapFile As MemoryMappedFile
    Dim memMapPtr As IntPtr
    Public memMapValues(50 - 1) As Double ' more than we need - buffer for growth.  PyStream assumes 400 bytes length!  Do not change without changing everywhere.
    Public memMapbufferSize As Integer
    Public Sub New()
        memMapbufferSize = System.Runtime.InteropServices.Marshal.SizeOf(GetType(Double)) * memMapValues.Length
        memMapPtr = Marshal.AllocHGlobal(memMapbufferSize)
        memMapFile = MemoryMappedFile.CreateOrOpen("Python_MemMap", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)
        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length)

        If standaloneTest() Then
            python.StartPython("--MemMapLength=" + CStr(memMapbufferSize))
            If python.strOut <> "" Then SetTrueText(python.strOut)
            Dim pythonApp = New FileInfo(task.pythonTaskName)
            SetTrueText("No output for Python_MemMap - see Python console log (see Options/'Show Console Log for external processes' in the main form)")
            desc = "Run Python app: " + pythonApp.Name + " to share memory with OpenCVB and Python."
        End If
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            SetTrueText(traceName + " has no output when run standaloneTest().")
            Exit Sub
        End If

        memMapValues(0) = task.frameCount
        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length)
    End Sub
End Class









Public Class XO_Python_Stream : Inherits TaskParent
    Dim python As New XO_Python_Basics
    Dim rgbBuffer(1) As Byte
    Dim depthBuffer(1) As Byte
    Dim dst1Buffer(1) As Byte
    Dim dst2Buffer(1) As Byte
    Dim memMap As XO_Python_MemMap
    Public Sub New()
        If standalone Then Exit Sub
        task.pipeName = "PyStream2Way" + CStr(task.pythonPipeIndex)
        task.pythonPipeIndex += 1
        Try
            task.pythonPipeOut = New NamedPipeServerStream(task.pipeName, PipeDirection.Out)
        Catch ex As Exception
            SetTrueText("Python_Stream: pipeOut NamedPipeServerStream failed to open.")
            Exit Sub
        End Try
        task.pythonPipeIn = New NamedPipeServerStream(task.pipeName + "Results", PipeDirection.In)

        ' Was this class invoked standaloneTest()?  Then just run something that works with BGR and depth...
        If task.pythonTaskName.EndsWith("Python_Stream") Then
            task.pythonTaskName = task.HomeDir + "Python/Python_Stream_PS.py"
        End If

        memMap = New XO_Python_MemMap()

        task.pythonReady = python.StartPython("--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + task.pipeName)
        If python.strOut <> "" Then SetTrueText(python.strOut)
        If task.pythonReady Then
            task.pythonPipeOut.WaitForConnection()
            task.pythonPipeIn.WaitForConnection()
        End If
        labels(2) = "Output of Python Backend"
        desc = "General purpose class to pipe BGR and Depth to Python scripts."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            SetTrueText(traceName + " has no output when run standaloneTest().")
            Exit Sub
        End If

        If task.pythonReady And task.pcSplit(2).Width > 0 Then
            Dim depth32f As cv.Mat = task.pcSplit(2) * 1000
            For i = 0 To memMap.memMapValues.Length - 1
                memMap.memMapValues(i) = Choose(i + 1, task.frameCount, src.Total * src.ElemSize,
                                                depth32f.Total * depth32f.ElemSize, src.Rows, src.Cols,
                                                task.drawRect.X, task.drawRect.Y, task.drawRect.Width, task.drawRect.Height)
            Next
            memMap.Run(src)

            If rgbBuffer.Length <> src.Total * src.ElemSize Then ReDim rgbBuffer(src.Total * src.ElemSize - 1)
            If depthBuffer.Length <> depth32f.Total * depth32f.ElemSize Then ReDim depthBuffer(depth32f.Total * depth32f.ElemSize - 1)
            If dst1Buffer.Length <> dst2.Total * dst2.ElemSize Then ReDim dst1Buffer(dst2.Total * dst2.ElemSize - 1)
            If dst2Buffer.Length <> dst3.Total * dst3.ElemSize Then ReDim dst2Buffer(dst3.Total * dst3.ElemSize - 1)
            Marshal.Copy(src.Data, rgbBuffer, 0, src.Total * src.ElemSize)
            Marshal.Copy(depth32f.Data, depthBuffer, 0, depthBuffer.Length)
            If task.pythonPipeOut.IsConnected Then
                On Error Resume Next
                task.pythonPipeOut.Write(rgbBuffer, 0, rgbBuffer.Length)
                task.pythonPipeOut.Write(depthBuffer, 0, depthBuffer.Length)
                task.pythonPipeIn.Read(dst1Buffer, 0, dst1Buffer.Length)
                task.pythonPipeIn.Read(dst2Buffer, 0, dst2Buffer.Length)
                Marshal.Copy(dst1Buffer, 0, dst2.Data, dst1Buffer.Length)
                Marshal.Copy(dst2Buffer, 0, dst3.Data, dst2Buffer.Length)
            End If
        End If
    End Sub
    Public Sub Close()
        If task.pythonPipeOut IsNot Nothing Then task.pythonPipeOut.Close()
        If task.pythonPipeIn IsNot Nothing Then task.pythonPipeIn.Close()
    End Sub
End Class




Public Class XO_MotionCam_MultiLine : Inherits TaskParent
    Public edgeList As New List(Of SortedList(Of Single, Integer))
    Public minDistance As Integer = dst2.Width * 0.02
    Dim knn As New KNN_EdgePoints
    Public Sub New()
        desc = "Find all the line edge points and display them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lines.dst2
        labels(3) = "The top " + CStr(task.lines.lpList.Count) + " longest lines in the image."

        knn.lpInput = task.lines.lpList
        knn.Run(emptyMat)

        For Each lpIn In task.lines.lpList
            Dim lp = HullLine_EdgePoints.EdgePointOffset(lpIn, 1)
            DrawCircle(dst2, New cv.Point(CInt(lp.ep1.X), CInt(lp.ep1.Y)))
            DrawCircle(dst2, New cv.Point(CInt(lp.ep2.X), CInt(lp.ep2.Y)))
        Next

        Static lpLast As New List(Of lpData)(task.lines.lpList)
        For Each lpIn In lpLast
            Dim lp = HullLine_EdgePoints.EdgePointOffset(lpIn, 5)
            DrawCircle(dst2, New cv.Point(CInt(lp.ep1.X), CInt(lp.ep1.Y)), white)
            DrawCircle(dst2, New cv.Point(CInt(lp.ep2.X), CInt(lp.ep2.Y)), white)
        Next

        lpLast = New List(Of lpData)(task.lines.lpList)

        labels(2) = knn.labels(2)
    End Sub
End Class






Public Class XO_MotionCam_MatchLast : Inherits TaskParent
    Dim motion As New XO_MotionCam_SideApproach
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find the common trends in the image edge points of the top, left, right, and bottom."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        motion.Run(src)
        dst1 = motion.dst1
        labels(1) = motion.labels(1)

        Static edgeList As New List(Of SortedList(Of Single, Integer))(motion.edgeList)
        Static lpLastList As New List(Of lpData)(task.lines.lpList)

        For i = 0 To edgeList.Count - 1
            If edgeList(i).Count = motion.edgeList(i).Count Then
                For j = 0 To edgeList(i).Count - 1
                    If edgeList(i).ElementAt(j).Key <> motion.edgeList(i).ElementAt(j).Key Then Dim k = 0
                Next
            Else
                Dim k = 0
            End If
        Next

        motion.buildDisplay(edgeList, lpLastList, 20, white)
        dst2 = motion.dst2
        trueData = motion.trueData

        edgeList = New List(Of SortedList(Of Single, Integer))(motion.edgeList)
        lpLastList = New List(Of lpData)(task.lines.lpList)

        labels(2) = motion.labels(2) + "  White points are for the previous frame"
    End Sub
End Class





Public Class XO_MotionCam_SideApproach : Inherits TaskParent
    Public edgeList As New List(Of SortedList(Of Single, Integer))
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find all the line edge points and display them."
    End Sub
    Public Sub buildDisplay(edgePoints As List(Of SortedList(Of Single, Integer)), lpList As List(Of lpData),
                            offset1 As Integer, color As cv.Scalar)
        Dim pt As cv.Point2f
        Dim index As Integer
        For Each sortlist In edgePoints
            Dim ptIndex As Integer = 0
            For Each ele In sortlist
                Dim lp = lpList(ele.Value)

                Select Case index
                    Case 0 ' top
                        pt = New cv.Point2f(ele.Key, offset1)
                    Case 1 ' left
                        pt = New cv.Point2f(offset1, ele.Key)
                    Case 2 ' right
                        pt = New cv.Point2f(dst2.Width - 10 - offset1, ele.Key)
                    Case 3 ' bottom
                        pt = New cv.Point2f(ele.Key, dst2.Height - 10 - offset1)
                End Select

                DrawCircle(dst2, pt, color)
                ptIndex += 1
            Next
            index += 1
        Next
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.lines.dst2
        labels(1) = "The top " + CStr(task.lines.lpList.Count) + " longest lines in the image."

        Dim top As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        Dim left As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        Dim right As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        Dim bottom As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)

        Dim lpList = task.lines.lpList
        For Each lp In lpList
            If lp.ep1.X = 0 Then left.Add(lp.ep1.Y, lp.index)
            If lp.ep1.Y = 0 Then top.Add(lp.ep1.X, lp.index)
            If lp.ep2.X = 0 Then left.Add(lp.ep2.Y, lp.index)
            If lp.ep2.Y = 0 Then top.Add(lp.ep2.X, lp.index)

            If lp.ep1.X = dst2.Width Then right.Add(lp.ep1.X, lp.index)
            If lp.ep1.Y = dst2.Height Then bottom.Add(lp.ep1.X, lp.index)
            If lp.ep2.X = dst2.Width Then right.Add(lp.ep2.Y, lp.index)
            If lp.ep2.Y = dst2.Height Then bottom.Add(lp.ep2.X, lp.index)
        Next

        edgeList.Clear()
        For i = 0 To 3
            Dim sortList = Choose(i + 1, top, left, right, bottom)
            edgeList.Add(sortList)
        Next

        dst2 = src.Clone
        buildDisplay(edgeList, lpList, 0, task.highlight)

        labels(2) = CStr(task.lines.lpList.Count * 2) + " edge points of the top " + CStr(task.lines.lpList.Count) +
                    " longest lines in the image are shown."
    End Sub
End Class






Public Class XO_MotionCam_Measure : Inherits TaskParent
    Public deltaX1 As Single, deltaX2 As Single, deltaY1 As Single, deltaY2 As Single
    Public Sub New()
        desc = "Measure how much the camera has moved."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static vecLast = task.lineLongest
        Dim vec = task.lineLongest

        deltaX1 = vec.ep1.X - vecLast.ep1.x
        deltaY1 = vec.ep1.Y - vecLast.ep1.Y

        deltaX2 = vec.ep2.X - vecLast.ep2.x
        deltaY2 = vec.ep2.Y - vecLast.ep2.Y

        Static strList As New List(Of String)
        strList.Add(Format(deltaX1, fmt1) + " " + Format(deltaX2, fmt1) + " " +
                    Format(deltaY1, fmt1) + " " + Format(deltaY2, fmt1) +
                    If(task.frameCount Mod 6 = 0, vbCrLf, vbTab))
        If strList.Count >= 132 Then strList.RemoveAt(0)

        strOut = ""
        For Each nextStr In strList
            strOut += nextStr
        Next
        SetTrueText(strOut, 3)

        vecLast = vec
    End Sub
End Class






Public Class XO_MotionCam_Plot : Inherits TaskParent
    Dim plot As New Plot_OverTime
    Dim measure As New XO_MotionCam_Measure
    Public Sub New()
        plot.minScale = -10
        plot.maxScale = 10
        desc = "Plot the variables describing the camera motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        measure.Run(src)

        plot.plotData = New cv.Scalar(measure.deltaX1, measure.deltaY1, measure.deltaX2, measure.deltaY2)
        plot.Run(src)
        dst2 = plot.dst2
        dst3 = plot.dst3
    End Sub
End Class






Public Class XO_Motion_TopFeatureFail : Inherits TaskParent
    Dim features As New Feature_General
    Public featureRects As New List(Of cv.Rect)
    Public searchRects As New List(Of cv.Rect)
    Dim match As New Match_Basics
    Dim saveMat As New cv.Mat
    Public Sub New()
        labels(2) = "Track the feature in the brick in the neighbors"
        desc = "Find the top feature cells and track them in the next frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim half As Integer = CInt(task.brickSize / 2)
        Dim pt As cv.Point
        If task.heartBeatLT Then
            features.Run(src)
            searchRects.Clear()
            featureRects.Clear()
            saveMat = src.Clone
            For Each pt In task.features
                Dim index As Integer = task.grid.gridMap.Get(Of Integer)(pt.Y, pt.X)
                Dim roi = New cv.Rect(pt.X - half, pt.Y - half, task.brickSize, task.brickSize)
                roi = ValidateRect(roi) ' stub bricks are fixed here 
                featureRects.Add(roi)
                searchRects.Add(task.gridNabeRects(index))
            Next

            dst2 = saveMat.Clone
            For Each pt In task.features
                Dim index As Integer = task.grid.gridMap.Get(Of Integer)(pt.Y, pt.X)
                Dim roi = New cv.Rect(pt.X - half, pt.Y - half, task.brickSize, task.brickSize)
                roi = ValidateRect(roi) ' stub bricks are fixed here 
                dst2.Rectangle(roi, task.highlight, task.lineWidth)
                dst2.Rectangle(task.gridNabeRects(index), task.highlight, task.lineWidth)
            Next
        End If

        dst3 = src.Clone
        Dim matchRects As New List(Of cv.Rect)
        For i = 0 To featureRects.Count - 1
            Dim roi = featureRects(i)
            match.template = saveMat(roi).Clone
            match.Run(src(searchRects(i)))
            dst3.Rectangle(match.newRect, task.highlight, task.lineWidth)
            matchRects.Add(match.newRect)
        Next

        saveMat = src.Clone
        searchRects.Clear()
        featureRects.Clear()
        For Each roi In matchRects
            half = CInt(roi.Width / 2) ' stubby bricks are those at the bottom or right side of the image.
            pt = New cv.Point(roi.X + half, roi.Y + half)
            Dim index As Integer = task.grid.gridMap.Get(Of Integer)(pt.Y, pt.X)
            featureRects.Add(roi)
            searchRects.Add(task.gridNabeRects(index))
        Next
    End Sub
End Class










Public Class XO_OpenGL_TextureShuffle : Inherits TaskParent
    Dim shuffle As New Random_Shuffle
    Dim floor As New XO_OpenGL_FlatStudy2
    Dim texture As Texture_Basics
    Public tRect As cv.Rect
    Public rgbaTexture As New cv.Mat
    Public Sub New()
        texture = New Texture_Basics()
        desc = "Use random shuffling to homogenize a texture sample of what the floor looks like."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            If dst2.Width = 320 Then
                SetTrueText("Texture_Shuffle is not supported at the 320x240 resolution.  It needs at least 256 rows in the output.")
                Exit Sub
            End If
            floor.plane.Run(src)
            dst3.SetTo(0)
            src.CopyTo(dst3, floor.plane.sliceMask)
            dst2 = floor.plane.dst2
            src = floor.plane.sliceMask
        End If

        texture.Run(src)
        dst2 = texture.dst3
        dst3.Rectangle(texture.tRect, white, task.lineWidth)
        shuffle.Run(texture.texture)
        tRect = New cv.Rect(0, 0, texture.tRect.Width * 4, texture.tRect.Height * 4)
        dst2(tRect) = shuffle.dst2.Repeat(4, 4)
        Dim split = dst2(tRect).Split()
        Dim alpha As New cv.Mat(split(0).Size(), cv.MatType.CV_8U, 1)
        Dim merged() As cv.Mat = {split(2), split(1), split(0), alpha}
        cv.Cv2.Merge(merged, rgbaTexture)
        SetTrueText("Use mouse movement over the image to display results.", 3)
    End Sub
End Class