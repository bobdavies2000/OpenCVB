Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes
Imports System.Drawing
Imports cvext = OpenCvSharp.Extensions
Imports OpenCvSharp

Public Class OpenGL_Basics : Inherits VB_Algorithm
    Dim memMapWriter As MemoryMappedViewAccessor
    ReadOnly startInfo As New ProcessStartInfo
    Dim memMapPtr As IntPtr
    Public dataInput As New cv.Mat
    Public pointCloudInput As New cv.Mat
    Public oglFunction As Integer = 0 ' the default function is to display a point cloud.
    Public options As New Options_OpenGLFunctions
    Dim rgbBuffer(0) As Byte
    Dim dataBuffer(0) As Byte
    Dim pointCloudBuffer(0) As Byte
    Public Sub New()
        task.OpenGLTitle = "OpenGL_Functions"
        gOptions.useHistoryCloud.Checked = True
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
            dataBufferSize, options.FOV, options.yaw, options.pitch, options.roll,
            options.zNear, options.zFar, options.PointSizeSlider.Value, dataInput.Width, dataInput.Height,
            task.IMU_AngularVelocity.X, task.IMU_AngularVelocity.Y, task.IMU_AngularVelocity.Z,
            task.IMU_Acceleration.X, task.IMU_Acceleration.Y, task.IMU_Acceleration.Z, task.IMU_TimeStamp,
            1, options.eye(0) / 100, options.eye(1) / 100, options.eye(2) / 100, options.zTrans,
            options.scaleXYZ(0) / 10, options.scaleXYZ(1) / 10, options.scaleXYZ(2) / 10, timeConversionUnits, imuAlphaFactor,
            task.OpenGLTitle.Length, pointCloudInput.Width, pointCloudInput.Height, oglFunction, task.activateTaskRequest, showXYZaxis}
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
            openGLPipe = New NamedPipeServerStream(task.pipeName, PipeDirection.InOut, 1)
        Catch ex As Exception
        End Try
        pipeCount += 1

        Dim memMap = memMapFill()
        Dim memMapbufferSize = 8 * memMap.Length

        startInfo.FileName = task.OpenGLTitle + ".exe"

        Dim windowWidth = 720
        Dim windowHeight = 720 * 240 / 320
        startInfo.Arguments = CStr(windowWidth) + " " + CStr(windowHeight) + " " + CStr(memMapbufferSize) + " " + task.pipeName
        If task.showConsoleLog = False Then startInfo.WindowStyle = ProcessWindowStyle.Hidden
        Process.Start(startInfo)

        memMapPtr = Marshal.AllocHGlobal(memMapbufferSize)
        Dim memMapFile As MemoryMappedFile = MemoryMappedFile.CreateOrOpen("OpenCVBControl", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)

        openGLPipe.WaitForConnection()

        While (1)
            openGL_hwnd = FindWindow(Nothing, task.OpenGLTitle)
            If openGL_hwnd Then Exit While
        End While
        Dim Left = CInt(GetSetting("OpenCVB1", "OpenGLtaskX", "OpenGLtaskX", task.mainFormLocation.X))
        Dim Top = CInt(GetSetting("OpenCVB1", "OpenGLtaskY", "OpenGLtaskY", task.mainFormLocation.Y))
        task.oglRect = New cv.Rect(Left, Top, windowWidth, windowHeight)
        MoveWindow(openGL_hwnd, Left, Top, task.oglRect.Width, task.oglRect.Height, True)
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then pointCloudInput = task.pointCloud

        ' adjust the point cloud if present and the 'move' sliders are non-zero
        options.RunVB()
        If pointCloudInput.Width <> 0 And options.moveAmount <> New cv.Scalar Then pointCloudInput -= options.moveAmount

        If src.Width > 0 Then
            src = src.CvtColor(cv.ColorConversionCodes.BGR2RGB) ' OpenGL needs RGB, not BGR
            If rgbBuffer.Length <> src.Total * src.ElemSize Then ReDim rgbBuffer(src.Total * src.ElemSize - 1)
        End If

        If dataInput.Width > 0 Then
            If dataBuffer.Length <> dataInput.Total * dataInput.ElemSize Then ReDim dataBuffer(dataInput.Total * dataInput.ElemSize - 1)
        Else
            ReDim dataBuffer(0)
        End If

        If pointCloudInput.Width > 0 Then
            If pointCloudBuffer.Length <> pointCloudInput.Total * pointCloudInput.ElemSize Then ReDim pointCloudBuffer(pointCloudInput.Total * pointCloudInput.ElemSize - 1)
        End If

        If firstPass Then
            StartOpenGLWindow()
        Else
            Dim readPipe(4) As Byte ' we read 4 bytes because that is the signal that the other end of the named pipe wrote 4 bytes to indicate iteration complete.
            If openGLPipe IsNot Nothing Then
                Dim bytesRead = openGLPipe.Read(readPipe, 0, 4)
                If bytesRead = 0 Then setTrueText("The OpenGL process appears to have stopped.", New cv.Point(20, 100))
            End If
        End If

        MemMapUpdate()

        If src.Width > 0 Then Marshal.Copy(src.Data, rgbBuffer, 0, rgbBuffer.Length)
        If dataInput.Width > 0 Then Marshal.Copy(dataInput.Data, dataBuffer, 0, dataBuffer.Length)
        If pointCloudInput.Width > 0 Then Marshal.Copy(pointCloudInput.Data, pointCloudBuffer, 0, pointCloudInput.Total * pointCloudInput.ElemSize)

        Try
            If src.Width > 0 Then openGLPipe.Write(rgbBuffer, 0, rgbBuffer.Length)
            If dataInput.Width > 0 Then openGLPipe.Write(dataBuffer, 0, dataBuffer.Length)
            If pointCloudInput.Width > 0 Then openGLPipe.Write(pointCloudBuffer, 0, pointCloudBuffer.Length)

            Dim buff = System.Text.Encoding.UTF8.GetBytes(task.OpenGLTitle)
            openGLPipe.Write(buff, 0, task.OpenGLTitle.Length)

            ' lose a lot of performance doing this!
            If gOptions.OpenGLCapture.Checked Then
                Dim snapshot As Bitmap = GetWindowImage(openGL_hwnd, New cv.Rect(0, 0, task.oglRect.Width * 2, task.oglRect.Height * 2))
                Dim snap = cvext.BitmapConverter.ToMat(snapshot)
                snap = snap.CvtColor(cv.ColorConversionCodes.BGRA2BGR)
                dst3 = snap.Resize(New cv.Size(dst2.Width, dst2.Height)) ', 0, 0, cv.InterpolationFlags.Nearest)
            End If
        Catch ex As Exception
            ' OpenGL window was likely closed.  
        End Try
        If standalone Then setTrueText(gMatrixToStr(task.gMatrix), 3)
    End Sub
End Class








Public Class OpenGL_BasicsSliders : Inherits VB_Algorithm
    Dim options As New Options_OpenGL
    Public pointCloudInput As cv.Mat
    Public Sub New()
        task.OpenGLTitle = "OpenGL_Basics"
        findSlider("OpenGL FOV").Value = 150
        desc = "Show the OpenGL point cloud with sliders support."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If standalone Then task.ogl.pointCloudInput = task.pointCloud Else task.ogl.pointCloudInput = pointCloudInput

        ' update all the options from the slider values.
        task.ogl.options.FOV = options.FOV
        task.ogl.options.yaw = options.yaw
        task.ogl.options.pitch = options.pitch
        task.ogl.options.roll = options.roll
        task.ogl.options.zNear = options.zNear
        task.ogl.options.zFar = options.zFar
        task.ogl.options.PointSizeSlider.Value = options.pointSize
        task.ogl.options.zTrans = options.zTrans
        task.ogl.options.eye = options.eye
        task.ogl.options.scaleXYZ = options.scaleXYZ
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class








Public Class OpenGL_BasicsMouse : Inherits VB_Algorithm
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Show the OpenGL point cloud with mouse support."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.testAllRunning Then Exit Sub ' seems to not like it when running overnight but it runs fine.
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class









Public Class OpenGL_ReducedXYZ : Inherits VB_Algorithm
    ReadOnly reduction As New Reduction_XYZ
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Display the pointCloud after reduction in X, Y, or Z dimensions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst3

        task.ogl.pointCloudInput = reduction.dst3
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class






Public Class OpenGL_Reduction : Inherits VB_Algorithm
    ReadOnly reduction As Reduction_PointCloud
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        reduction = New Reduction_PointCloud
        desc = "Use the reduced depth pointcloud in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst2 = reduction.dst2
        task.ogl.pointCloudInput = reduction.dst3
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class







Public Class OpenGL_ReducedSideView : Inherits VB_Algorithm
    ReadOnly sideView As New PointCloud_ReducedSideView
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Use the reduced depth pointcloud in 3D but allow it to be rotated in Options_Common"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        sideView.Run(src)
        dst2 = sideView.dst2
        task.ogl.pointCloudInput = sideView.dst3
        task.ogl.Run(task.color)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        labels(2) = sideView.labels(2)
    End Sub
End Class





Module OpenGL_Module
    Public Enum pointStyle
        unFiltered = 0
        filtered = 1
        flattened = 2
        flattenedAndFiltered = 3
    End Enum
    Public Enum oCase
        pointCloudAndRGB = 0
        verticalLines = 1
        drawFloor = 2
        tessalateTriangles = 3
        drawPyramid = 4
        drawCube = 5
        simplePlane = 6
        minMaxBlocks = 7
        drawTiles = 8
        drawCell = 9
        drawCells = 10
        floorStudy = 11
        data3D = 12
        sierpinski = 13
        polygonCell = 14
        Histogram3D = 15
        pcPoints = 16
        pcLines = 17
        pcPointsAlone = 18
    End Enum
End Module







Public Class OpenGL_Rebuilt : Inherits VB_Algorithm
    ReadOnly rebuild As New Structured_Rebuild
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Review the rebuilt point cloud from Structured_Rebuild"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rebuild.Run(src)
        dst2 = rebuild.dst2
        task.ogl.pointCloudInput = rebuild.pointcloud
        task.ogl.Run(task.color)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class






Public Class OpenGL_World : Inherits VB_Algorithm
    ReadOnly world As New Depth_World
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Use Generated Pointcloud")
            radio.addRadio("Use Pointcloud from camera")
            radio.check(0).Checked = True
        End If

        labels = {"", "", "Generated Pointcloud", ""}
        desc = "Display the generated point cloud in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static generatedRadio = findRadio("Use Generated Pointcloud")

        If generatedRadio.checked Then
            world.Run(src)
            task.ogl.pointCloudInput = world.dst2
        Else
            task.ogl.pointCloudInput = If(src.Type = cv.MatType.CV_32FC3, src, task.pointCloud)
        End If

        task.ogl.Run(task.color)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class







Public Class OpenGL_VerticalSingle : Inherits VB_Algorithm
    ReadOnly vLine As New Feature_LongestV_Tutorial2
    Public Sub New()
        task.ogl.oglFunction = oCase.verticalLines
        desc = "Visualize the vertical line found with Feature_LongestV_Tutorial"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        vLine.Run(src)
        dst2 = vLine.dst2
        dst3 = vLine.dst3

        Dim pt1 = vLine.pt1
        Dim pt2 = vLine.pt2
        Dim linePoints3D As New List(Of cv.Point3f)({New cv.Point3f((pt1.X + pt2.X) / 2, pt1.Y, (pt1.Z + pt2.Z) / 2), New cv.Point3f(pt1.X, pt2.Y, pt1.Z)})
        task.ogl.dataInput = New cv.Mat(linePoints3D.Count, 1, cv.MatType.CV_32FC3, linePoints3D.ToArray)

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(task.color)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class









Public Class OpenGL_VerticalOrHorizontal : Inherits VB_Algorithm
    ReadOnly vLine As New Feature_Lines
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Show Vertical Lines")
            radio.addRadio("Show Horizontal Lines")
            radio.check(0).Checked = True
        End If

        task.ogl.oglFunction = oCase.verticalLines
        desc = "Visualize all the vertical lines found in Feature_Lines"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static verticalRadio = findRadio("Show Vertical Lines")
        Dim showVerticals = verticalRadio.checked

        vLine.Run(src)
        dst2 = vLine.dst3

        task.ogl.pointCloudInput = task.pointCloud

        Dim lines3D As New List(Of cv.Point3f)
        Dim count = If(showVerticals, vLine.sortedVerticals.Count, vLine.sortedHorizontals.Count)
        For i = 0 To count - 1
            Dim index = If(showVerticals, vLine.sortedVerticals.ElementAt(i).Value, vLine.sortedHorizontals.ElementAt(i).Value)
            lines3D.Add(vLine.lines3D(index))
            lines3D.Add(vLine.lines3D(index + 1))
        Next
        task.ogl.dataInput = New cv.Mat(lines3D.Count, 1, cv.MatType.CV_32FC3, lines3D.ToArray)
        task.ogl.Run(task.color)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class





' https://www3.ntu.edu.sg/home/ehchua/programming/opengl/CG_Examples.html
Public Class OpenGL_Pyramid : Inherits VB_Algorithm
    Public Sub New()
        task.ogl.oglFunction = oCase.drawPyramid ' all the work is done inside the switch statement in OpenGL_Functions.
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Draw the traditional OpenGL pyramid"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.ogl.pointCloudInput = New cv.Mat
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class





'https://www3.ntu.edu.sg/home/ehchua/programming/opengl/CG_Examples.html
Public Class OpenGL_DrawCube : Inherits VB_Algorithm
    Public Sub New()
        task.ogl.oglFunction = oCase.drawCube
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Draw the traditional OpenGL cube"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.ogl.pointCloudInput = New cv.Mat()
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class






'https://www3.ntu.edu.sg/home/ehchua/programming/opengl/CG_Examples.html
Public Class OpenGL_QuadSimple : Inherits VB_Algorithm
    Dim tess As New Tessallate_QuadSimple
    Public Sub New()
        task.ogl.oglFunction = oCase.simplePlane
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Create a simple plane in each roi of the RedCloud data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        tess.Run(src)
        dst2 = tess.dst2
        dst3 = tess.dst3
        labels = tess.labels
        task.ogl.dataInput = New cv.Mat(tess.oglData.Count, 1, cv.MatType.CV_32FC3, tess.oglData.ToArray)

        task.ogl.pointCloudInput = New cv.Mat()
        task.ogl.Run(dst3)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class






'https://www3.ntu.edu.sg/home/ehchua/programming/opengl/CG_Examples.html
Public Class OpenGL_QuadHulls : Inherits VB_Algorithm
    Dim tess As New Tessallate_QuadHulls
    Public Sub New()
        task.ogl.oglFunction = oCase.simplePlane
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Create a simple plane in each roi of the RedCloud data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        tess.Run(src)
        dst2 = tess.dst2
        dst3 = tess.dst3
        labels = tess.labels
        task.ogl.dataInput = New cv.Mat(tess.oglData.Count, 1, cv.MatType.CV_32FC3, tess.oglData.ToArray)

        task.ogl.pointCloudInput = New cv.Mat()
        task.ogl.Run(dst3)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class






'https://www3.ntu.edu.sg/home/ehchua/programming/opengl/CG_Examples.html
Public Class OpenGL_QuadMinMax : Inherits VB_Algorithm
    Dim tess As New Tessallate_QuadMinMax
    Public Sub New()
        task.ogl.oglFunction = oCase.simplePlane
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Reflect the min and max for each roi of the RedCloud data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        tess.Run(src)
        dst2 = tess.dst2
        dst3 = tess.dst3
        labels = tess.labels
        task.ogl.dataInput = New cv.Mat(tess.oglData.Count, 1, cv.MatType.CV_32FC3, tess.oglData.ToArray)

        task.ogl.pointCloudInput = New cv.Mat()
        task.ogl.Run(dst3)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class





Public Class OpenGL_Bricks : Inherits VB_Algorithm
    Dim tess As New Tessallate_Bricks
    Public Sub New()
        task.ogl.oglFunction = oCase.minMaxBlocks
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Create blocks in each roi using the min and max depth values"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        tess.Run(src)
        task.ogl.dataInput = New cv.Mat(tess.oglData.Count, 1, cv.MatType.CV_32FC3, tess.oglData.ToArray)
        dst2 = tess.dst3
        dst3 = tess.hulls.dst3

        Dim index As Integer
        For Each roi In task.gridList
            If index < tess.depths.Count Then
                setTrueText(Format(tess.depths(index), fmt1) + vbCrLf + Format(tess.depths(index + 1), fmt1), New cv.Point(roi.X, roi.Y), 2)
            End If
            index += 2
        Next

        task.ogl.pointCloudInput = New cv.Mat()
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class








Public Class OpenGL_StructuredCloud : Inherits VB_Algorithm
    Dim sCloud As New Structured_Cloud
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        labels(2) = "Structured cloud 32fC3 data"
        desc = "Visualize the Structured_Cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        sCloud.Run(src)

        redC.Run(src)
        dst2 = redC.dst2
        labels = redC.labels
        task.ogl.pointCloudInput = sCloud.dst2
        task.ogl.Run(dst2)
        task.ogl.options.PointSizeSlider.Value = gOptions.GridSize.Value
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class





Public Class OpenGL_Tiles : Inherits VB_Algorithm
    Dim sCloud As New Structured_Tiles
    Public Sub New()
        task.ogl.oglFunction = oCase.drawTiles
        task.OpenGLTitle = "OpenGL_Functions"
        labels = {"", "", "Input from Structured_Tiles", ""}
        desc = "Display the quads built by Structured_Tiles in OpenGL - uses OpenGL's point size"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        sCloud.Run(src)
        dst2 = sCloud.dst2
        dst3 = sCloud.dst3

        task.ogl.dataInput = New cv.Mat(sCloud.oglData.Count, 1, cv.MatType.CV_32FC3, sCloud.oglData.ToArray)
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        task.ogl.options.PointSizeSlider.Value = gOptions.GridSize.Value
    End Sub
End Class





Public Class OpenGL_TilesQuad : Inherits VB_Algorithm
    Dim sCloud As New Structured_TilesQuad
    Public Sub New()
        task.ogl.oglFunction = oCase.simplePlane
        task.OpenGLTitle = "OpenGL_Functions"
        labels = {"", "", "Input from Structured_Tiles", ""}
        desc = "Display the quads built by Structured_TilesQuad in OpenGL - does NOT use OpenGL's point size"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        sCloud.Run(src)
        dst2 = sCloud.dst2

        task.ogl.dataInput = New cv.Mat(sCloud.oglData.Count, 1, cv.MatType.CV_32FC3, sCloud.oglData.ToArray)
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class











Public Class OpenGL_OnlyPlanes : Inherits VB_Algorithm
    ReadOnly planes As New Plane_OnlyPlanes
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        labels = {"", "", "RedCloud Cells", "Planes built in the point cloud"}
        desc = "Display the pointCloud as a set of RedCloud cell planes"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        planes.Run(src)
        dst2 = planes.dst2
        dst3 = planes.dst3
        task.ogl.pointCloudInput = planes.dst3
        task.ogl.Run(task.color)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class









Public Class OpenGL_FlatStudy1 : Inherits VB_Algorithm
    ReadOnly plane As New Structured_LinearizeFloor
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        labels = {"", "", "Side view of point cloud - use mouse to highlight the floor", "Highlight the floor in BGR image"}
        desc = "Convert depth cloud floor to a plane and visualize it with OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        plane.Run(src)
        dst2 = plane.dst3
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(plane.dst2)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class








Public Class OpenGL_FlatStudy2 : Inherits VB_Algorithm
    Public plane As New Structured_LinearizeFloor
    Public Sub New()
        task.ogl.oglFunction = oCase.drawFloor
        desc = "Show the floor in the pointcloud as a plane"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        plane.Run(src)
        dst2 = plane.dst3

        Dim oglData As New List(Of Single)
        Dim floorColor = task.color.Mean(plane.sliceMask)
        oglData.Add(floorColor(0))
        oglData.Add(floorColor(1))
        oglData.Add(floorColor(2))
        oglData.Add(plane.floorYPlane)
        task.ogl.dataInput = New cv.Mat(4, 1, cv.MatType.CV_32F, oglData.ToArray)
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(plane.dst2)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class






Public Class OpenGL_FlatStudy3 : Inherits VB_Algorithm
    Dim plane As New Plane_FloorStudy
    Public Sub New()
        task.ogl.oglFunction = oCase.floorStudy
        task.OpenGLTitle = "OpenGL_Functions"
        labels = {"", "", "", ""}
        desc = "Create an OpenGL display where the floor is built as a quad"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")

        plane.Run(src)
        dst2 = plane.dst2
        labels(2) = plane.labels(2)

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.dataInput = New cv.Mat(1, 1, cv.MatType.CV_32F, {plane.planeY})
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class






Public Class OpenGL_FlatFloor : Inherits VB_Algorithm
    Dim flatness As New Model_FlatSurfaces
    Public Sub New()
        task.ogl.oglFunction = oCase.floorStudy
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Using minimal cost, create an OpenGL display where the floor is built as a quad"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flatness.Run(src)
        setTrueText(flatness.labels(2), 3)

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.dataInput = New cv.Mat(1, 1, cv.MatType.CV_32F, {task.pcFloor})
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        labels(2) = flatness.labels(2)
        labels(3) = flatness.labels(3)
    End Sub
End Class






Public Class OpenGL_FlatCeiling : Inherits VB_Algorithm
    Dim flatness As New Model_FlatSurfaces
    Public Sub New()
        task.ogl.oglFunction = oCase.floorStudy
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Using minimal cost, create an OpenGL display where the ceiling is built as a quad"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flatness.Run(src)
        setTrueText(flatness.labels(2), 3)

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.dataInput = New cv.Mat(1, 1, cv.MatType.CV_32F, {task.pcCeiling})
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        labels(2) = flatness.labels(2)
        labels(3) = flatness.labels(3)
    End Sub
End Class









Public Class OpenGL_PeakFlat : Inherits VB_Algorithm
    Dim peak As New Plane_Histogram
    Dim kalman As New Kalman_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.floorStudy
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Display the peak flat level in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        peak.Run(src)
        dst2 = peak.dst2
        labels(2) = peak.labels(3)

        kalman.kInput = {peak.peakFloor, peak.peakCeiling}
        kalman.Run(Nothing)

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.dataInput = New cv.Mat(2, 1, cv.MatType.CV_32F, {kalman.kOutput(0), kalman.kOutput(1)})
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class






Public Class OpenGL_DrawHull : Inherits VB_Algorithm
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        task.ogl.oglFunction = oCase.drawCell
        task.OpenGLTitle = "OpenGL_Functions"
        labels = {"", "", "RedCloud output", ""}
        desc = "Select a cell and display its hull in OpenGL as a polygon."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2
        Dim oglData As New List(Of cv.Point3f)

        Dim rc = task.rcSelect
        Dim hull As New List(Of cv.Point3f)
        If rc.hull IsNot Nothing Then
            For Each pt In rc.hull
                hull.Add(task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X))
            Next

            For i = 0 To hull.Count - 1 Step 3
                oglData.Add(hull(i Mod hull.Count))
                oglData.Add(hull((i + 1) Mod hull.Count))
                oglData.Add(hull((i + 2) Mod hull.Count))
            Next
        End If

        task.ogl.dataInput = New cv.Mat(oglData.Count, 1, cv.MatType.CV_32FC3, oglData.ToArray)
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class










Public Class OpenGL_FPolyCloud : Inherits VB_Algorithm
    Dim fpolyPC As New FPoly_PointCloud
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        If standalone Then gOptions.displayDst1.Checked = True
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Display the pointcloud after FPoly_PointCloud identifies the changes depth pixels"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fpolyPC.Run(src)
        dst1 = fpolyPC.dst1
        dst2 = fpolyPC.dst2
        dst3 = fpolyPC.dst3
        setTrueText(fpolyPC.fMask.fImage.strOut, 1)
        labels = fpolyPC.labels

        task.ogl.pointCloudInput = fpolyPC.fPolyCloud
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class








' https://cs.lmu.edu/~ray/notes/openglexamples/
Public Class OpenGL_Sierpinski : Inherits VB_Algorithm
    Public Sub New()
        task.ogl.oglFunction = oCase.sierpinski
        task.OpenGLTitle = "OpenGL_Functions"
        findSlider("OpenGL Point Size").Value = 3
        desc = "Draw the Sierpinski triangle pattern in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class







Public Class OpenGL_DrawHulls : Inherits VB_Algorithm
    Public options As New Options_OpenGLFunctions
    Public hulls As New RedCloud_Hulls
    Dim ogl As New OpenGL_Basics
    Public Sub New()
        ogl.oglFunction = oCase.drawCells
        task.OpenGLTitle = "OpenGL_Functions"
        labels = {"", "", "", ""}
        desc = "Draw all the hulls in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim shift = options.moveAmount

        hulls.Run(src)
        dst2 = hulls.dst2
        Dim rcx = task.rcSelect

        Dim oglData As New List(Of cv.Point3f)
        oglData.Add(New cv.Point3f)
        Dim polygonCount As Integer
        For Each rc In task.redCells
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
        ogl.dataInput = New cv.Mat(oglData.Count, 1, cv.MatType.CV_32FC3, oglData.ToArray)
        ogl.Run(dst2)
        If gOptions.OpenGLCapture.Checked Then dst3 = ogl.dst2
        setTrueText(CStr(polygonCount) + " polygons were sent to OpenGL", 2)
    End Sub
End Class







Public Class OpenGL_Contours : Inherits VB_Algorithm
    Dim options2 As New Options_OpenGL_Contours
    Public options As New Options_OpenGLFunctions
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.drawCells
        task.OpenGLTitle = "OpenGL_Functions"
        findSlider("OpenGL shift fwd/back (Z-axis)").Value = -150
        labels = {"", "", "Output of RedCloud", "OpenGL snapshot"}
        desc = "Draw all the RedCloud contours in OpenGL with various settings."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim shift = options.moveAmount

        options2.RunVB()

        redC.Run(src)
        dst2 = redC.dst2
        Dim rcx = task.rcSelect

        Dim polygonCount As Integer
        Dim oglData As New List(Of cv.Point3f)
        Dim lastDepth As cv.Scalar
        oglData.Add(New cv.Point3f)
        For Each rc In task.redCells
            Dim d = rc.depthMean.Z
            If d = 0 Then Continue For

            Dim dataIndex = oglData.Count
            oglData.Add(New cv.Point3f(rc.contour.Count, 0, 0))
            If rc.index = rcx.index Then
                oglData.Add(New cv.Point3f(1, 1, 1))
            Else
                oglData.Add(New cv.Point3f(rc.color(2) / 255, rc.color(1) / 255, rc.color(0) / 255))
            End If
            lastDepth = rc.depthMean
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
        task.ogl.dataInput = New cv.Mat(oglData.Count, 1, cv.MatType.CV_32FC3, oglData.ToArray)
        task.ogl.Run(New cv.Mat)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class








Public Class OpenGL_3DColorShape : Inherits VB_Algorithm
    Dim colorClass As New Color_Basics
    Public Sub New()
        task.OpenGLTitle = "OpenGL_Functions"
        task.ogl.oglFunction = oCase.Histogram3D
        desc = "Create an OpenGL plot using the different color formats normalized to between 0 and 1."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorClass.Run(src)
        dst2 = colorClass.dst3
        dst2.ConvertTo(dst1, cv.MatType.CV_32FC3)
        task.ogl.dataInput = dst1.Normalize(0, 1, cv.NormTypes.MinMax)
        task.ogl.pointCloudInput = New cv.Mat
        task.ogl.Run(New cv.Mat)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class







Public Class OpenGL_3D : Inherits VB_Algorithm
    Public Sub New()
        task.OpenGLTitle = "OpenGL_Functions"
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Create an OpenGL plot using the BGR data normalized to between 0 and 1."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        src.ConvertTo(src, cv.MatType.CV_32FC3)
        task.ogl.pointCloudInput = src.Normalize(0, 1, cv.NormTypes.MinMax)
        task.ogl.Run(New cv.Mat)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class





Public Class OpenGL_3DColors : Inherits VB_Algorithm
    Dim colorClass As New Color_Basics
    Public Sub New()
        task.OpenGLTitle = "OpenGL_Functions"
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Connect the 3D representation of the different color formats with colors in that format (see dst2)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorClass.Run(src)
        dst2 = colorClass.dst3
        dst2.ConvertTo(dst1, cv.MatType.CV_32FC3)
        task.ogl.pointCloudInput = dst1.Normalize(0, 1, cv.NormTypes.MinMax)
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class








Public Class OpenGL_3DRGB : Inherits VB_Algorithm
    Dim hist3d As New Histogram3D_Basics
    Public Sub New()
        task.OpenGLTitle = "OpenGL_Functions"
        task.ogl.oglFunction = oCase.Histogram3D
        task.ogl.options.PointSizeSlider.Value = 10
        desc = "Plot the results of a 3D histogram of the BGR data "
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3d.Run(src)

        task.ogl.dataInput = hist3d.dst2
        task.ogl.pointCloudInput = New cv.Mat
        task.ogl.Run(New cv.Mat)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class






' https://docs.opencv.org/3.4/d1/d1d/tutorial_histo3D.html
Public Class OpenGL_3Ddepth : Inherits VB_Algorithm
    Dim hist3d As New Histogram3D_Depth
    Public Sub New()
        task.ogl.oglFunction = oCase.Histogram3D
        task.OpenGLTitle = "OpenGL_Functions"
        task.ogl.options.PointSizeSlider.Value = 10
        desc = "Display the 3D histogram of the depth in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3d.Run(src)
        task.ogl.dataInput = hist3d.histogram
        task.ogl.pointCloudInput = New cv.Mat
        task.ogl.Run(New cv.Mat)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        setTrueText("Use the sliders for X/Y/Z histogram bins to add more points")
    End Sub
End Class











Public Class OpenGL_PCLineCandidates : Inherits VB_Algorithm
    Dim pts As New PointCloud_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.pcPointsAlone
        task.OpenGLTitle = "OpenGL_Functions"
        findSlider("OpenGL Point Size").Value = 10
        desc = "Display the output of the PointCloud_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        pts.Run(src)
        dst2 = pts.dst2

        task.ogl.dataInput = New cv.Mat(pts.allPointsH.Count, 1, cv.MatType.CV_32FC3, pts.allPointsH.ToArray)
        task.ogl.Run(New cv.Mat)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        labels(2) = "Point cloud points found = " + CStr(pts.actualCount / 2)
    End Sub
End Class







Public Class OpenGL_PClinesFirstLast : Inherits VB_Algorithm
    Dim lines As New Line3D_CandidatesFirstLast
    Public Sub New()
        task.ogl.oglFunction = oCase.pcLines
        task.OpenGLTitle = "OpenGL_Functions"
        findSlider("OpenGL Point Size").Value = 10
        desc = "Draw the 3D lines found from the PCpoints"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        If lines.pcLinesMat.Rows = 0 Then task.ogl.dataInput = New cv.Mat Else task.ogl.dataInput = lines.pcLinesMat
        task.ogl.Run(New cv.Mat)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        labels(2) = "OpenGL_PClines found " + CStr(lines.pcLinesMat.Rows / 3) + " lines"
    End Sub
End Class







Public Class OpenGL_PClinesAll : Inherits VB_Algorithm
    Dim lines As New Line3D_CandidatesAll
    Public Sub New()
        task.ogl.oglFunction = oCase.pcLines
        task.OpenGLTitle = "OpenGL_Functions"
        findSlider("OpenGL Point Size").Value = 10
        desc = "Draw the 3D lines found from the PCpoints"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2

        If lines.pcLinesMat.Rows = 0 Then task.ogl.dataInput = New cv.Mat Else task.ogl.dataInput = lines.pcLinesMat
        task.ogl.Run(New cv.Mat)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        labels(2) = "OpenGL_PClines found " + CStr(lines.pcLinesMat.Rows / 3) + " lines"
    End Sub
End Class








Public Class OpenGL_PatchHorizontal : Inherits VB_Algorithm
    Dim patch As New Pixel_NeighborsPatchNeighbors
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Draw the point cloud after patching z-values that are similar"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        patch.Run(src)
        dst2 = patch.dst3
        task.ogl.pointCloudInput = dst2
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class









Public Class OpenGL_PCpoints : Inherits VB_Algorithm
    Dim pts As New PointCloud_PCPoints
    Public Sub New()
        task.ogl.oglFunction = oCase.pcPoints
        task.OpenGLTitle = "OpenGL_Functions"
        findSlider("OpenGL Point Size").Value = 10
        desc = "Display the output of the PointCloud_Points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        pts.Run(src)
        dst2 = pts.dst2

        task.ogl.dataInput = New cv.Mat(pts.pcPoints.Count, 1, cv.MatType.CV_32FC3, pts.pcPoints.ToArray)
        task.ogl.Run(New cv.Mat)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        labels(2) = "Point cloud points found = " + CStr(pts.pcPoints.Count / 2)
    End Sub
End Class









Public Class OpenGL_PCpointsPlane : Inherits VB_Algorithm
    Dim pts As New PointCloud_PCPointsPlane
    Public Sub New()
        task.ogl.oglFunction = oCase.pcPoints
        task.OpenGLTitle = "OpenGL_Functions"
        findSlider("OpenGL Point Size").Value = 10
        desc = "Display the points that are likely to be in a plane - found by both the vertical and horizontal searches"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        pts.Run(src)

        task.ogl.dataInput = New cv.Mat(pts.pcPoints.Count, 1, cv.MatType.CV_32FC3, pts.pcPoints.ToArray)
        task.ogl.Run(New cv.Mat)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        labels(2) = "Point cloud points found = " + CStr(pts.pcPoints.Count / 2)
    End Sub
End Class







Public Class OpenGL_DepthAverage : Inherits VB_Algorithm
    Dim stable As New Depth_AveragingStable
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        labels(3) = "32-bit format stabilized depth data"
        desc = "Use the depth_stabilizer output as input the an OpenGL display"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2) * 1000

        stable.Run(src)
        dst2 = stable.dst3

        task.pcSplit(2) = stable.dst3 * 0.001
        cv.Cv2.Merge(task.pcSplit, dst1)
        task.ogl.pointCloudInput = dst1
        task.ogl.Run(task.color)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class










Public Class OpenGL_PlaneClusters3D : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim eq As New Plane_Equation
    Public Sub New()
        task.ogl.oglFunction = oCase.pcPoints
        findSlider("OpenGL Point Size").Value = 10
        labels(3) = "Only the cells with a high probability plane are presented - blue on X-axis, green on Y-axis, red on Z-axis"
        desc = "Cluster the plane equations to find major planes in the image and display the clusters in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static planeSlider = findSlider("Plane Coefficient threshold")
        Dim planeThreshold = planeSlider.value / 100
        redC.Run(src)
        dst2 = redC.dst2
        dst3 = redC.dst3

        Dim pcPoints As New List(Of cv.Point3f)
        Dim blue As New cv.Point3f(0, 0, 1), red As New cv.Point3f(1, 0, 0), green As New cv.Point3f(0, 1, 0) ' NOTE: RGB, not BGR...
        For Each rc In task.redCells
            If rc.maxVec.Z > 0 Then
                eq.rc = rc
                eq.Run(src)
                rc = eq.rc
            End If
            If rc.eq = New cv.Vec4f Then Continue For
            If Math.Abs(rc.eq.Item0) < planeThreshold And Math.Abs(rc.eq.Item1) < planeThreshold And
               Math.Abs(rc.eq.Item2) < planeThreshold Then Continue For

            If rc.eq.Item0 > rc.eq.Item1 And rc.eq.Item0 > rc.eq.Item2 Then pcPoints.Add(red)
            If rc.eq.Item1 > rc.eq.Item0 And rc.eq.Item1 > rc.eq.Item2 Then pcPoints.Add(green)
            If rc.eq.Item2 > rc.eq.Item0 And rc.eq.Item2 > rc.eq.Item1 Then pcPoints.Add(blue)

            pcPoints.Add(New cv.Point3f(rc.eq.Item0 * 0.5, rc.eq.Item1 * 0.5, rc.eq.Item2 * 0.5))
        Next

        task.ogl.dataInput = New cv.Mat(pcPoints.Count, 1, cv.MatType.CV_32FC3, pcPoints.ToArray)
        task.ogl.Run(New cv.Mat)
    End Sub
End Class








Public Class OpenGL_KWhere : Inherits VB_Algorithm
    Dim kWare As New GuidedBP_Hulls
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Show the KWhere output in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        kWare.Run(src)
        dst2 = kWare.dst2

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(dst2)
    End Sub
End Class









Public Class OpenGL_Profile : Inherits VB_Algorithm
    Public sides As New Profile_Basics
    Public rotate As New Profile_Rotation
    Dim heat As New HeatMap_Basics
    Dim ogl As New OpenGL_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        If standalone Then gOptions.gravityPointCloud.Checked = False
        sides.redCold = New RedCloud_Basics
        ogl.oglFunction = oCase.pcPointsAlone
        labels(3) = "Contour of selected cell is shown below.  Blue dot represents the minimum X (leftmost) point and red the maximum X (rightmost)"
        desc = "Visualize a RedCloud Cell and rotate it using the Options_IMU Sliders"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        sides.Run(src)
        dst2 = sides.dst2

        Dim rc = sides.rc
        Dim contourMat As New cv.Mat(rc.contour.Count, 1, cv.MatType.CV_32SC2, rc.contour.ToArray)
        If rc.contour.Count = 0 Then Exit Sub

        Dim split = contourMat.Split()
        Dim mm = vbMinMax(split(0))
        Dim p1 = rc.contour.ElementAt(mm.minLoc.Y)
        Dim p2 = rc.contour.ElementAt(mm.maxLoc.Y)

        dst3.SetTo(0)
        vbDrawContour(dst3(rc.rect), rc.contour, cv.Scalar.Yellow)
        dst3.Circle(New cv.Point(p1.X + rc.rect.X, p1.Y + rc.rect.Y), task.dotSize + 2, cv.Scalar.Blue, -1, task.lineType)
        dst3.Circle(New cv.Point(p2.X + rc.rect.X, p2.Y + rc.rect.Y), task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
        If rc.contour3D.Count > 0 Then
            Dim vecMat As New cv.Mat(rc.contour3D.Count, 1, cv.MatType.CV_32FC3, rc.contour3D.ToArray)

            rotate.Run(Nothing)
            Dim output As cv.Mat = vecMat.Reshape(1, vecMat.Rows * vecMat.Cols) * rotate.gMat.gMatrix ' <<<<<<<<<<<<<<<<<<<<<<< this is the XYZ-axis rotation...
            vecMat = output.Reshape(3, vecMat.Rows)

            ogl.pointCloudInput = New cv.Mat
            ogl.dataInput = vecMat

            heat.Run(vecMat)
            dst1 = heat.dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)
        End If

        ogl.Run(New cv.Mat)
    End Sub
End Class






Public Class OpenGL_ProfileSweep : Inherits VB_Algorithm
    Dim visuals As New OpenGL_Profile
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        gOptions.gravityPointCloud.Checked = False
        desc = "Test the X-, Y-, and Z-axis rotation in sequence"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static testCase As Integer = 0
        If task.frameCount Mod 100 = 0 Then
            testCase += 1
            If testCase >= 3 Then testCase = 0
            task.depthBasics.IMUOptions.rotateX = -45
            task.depthBasics.IMUOptions.rotateY = -45
            task.depthBasics.IMUOptions.rotateZ = -45
        End If

        Dim bump = 1
        Static xRotateSlider = findSlider("Rotate pointcloud around X-axis (degrees)")
        Static yRotateSlider = findSlider("Rotate pointcloud around Y-axis (degrees)")
        Static zRotateSlider = findSlider("Rotate pointcloud around Z-axis (degrees)")
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
        setTrueText("Top down view: " + labels(3), 1)

        visuals.Run(src)
        dst1 = visuals.dst1
        dst2 = visuals.dst2
        dst3 = visuals.dst3
    End Sub
End Class







Public Class OpenGL_FlatSurfaces : Inherits VB_Algorithm
    Dim flat As New RedCloudY_FlatSurfaces
    Public Sub New()
        labels(2) = "Display the point cloud pixels that appear to be vertical and horizontal regions."
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Review the vertical and horizontal regions from Plane_Basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flat.Run(src)
        task.pointCloud.CopyTo(dst2, flat.dst2)

        task.ogl.pointCloudInput = dst2
        task.ogl.Run(src)
    End Sub
End Class






Public Class OpenGL_GradientPhase : Inherits VB_Algorithm
    Dim gradient As New Gradient_Depth
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Show the depth gradient Phase in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        gradient.Run(src)
        dst2 = gradient.dst2
        dst3 = gradient.dst3
        dst1 = vbNormalize32f(gradient.dst3)
        labels = gradient.labels

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(dst1)
    End Sub
End Class









Public Class OpenGL_GravityTransform : Inherits VB_Algorithm
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Use the IMU's acceleration values to build the transformation matrix of an OpenGL viewer"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(src)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class







' https://open.gl/transformations
' https://www.codeproject.com/Articles/1247960/Learning-Basic-Math-Used-In-3D-Graphics-Engines
Public Class OpenGL_GravityAverage : Inherits VB_Algorithm
    ReadOnly imuAvg As New IMU_Average
    ReadOnly imu As New IMU_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Build the GMatrix with the Average IMU acceleration (not the raw or filtered values) and use the resulting GMatrix to stabilize the point cloud in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        setTrueText(strOut, 3)
    End Sub
End Class







' https://open.gl/transformations
' https://www.codeproject.com/Articles/1247960/Learning-Basic-Math-Used-In-3D-Graphics-Engines
Public Class OpenGL_GravityKalman : Inherits VB_Algorithm
    ReadOnly imuKalman As New IMU_Kalman
    ReadOnly imu As New IMU_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Build the GMatrix with the Average IMU acceleration (not the raw or filtered values) and use the resulting GMatrix to stabilize the point cloud in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        setTrueText(strOut, 3)
    End Sub
End Class









Public Class OpenGL_StableMinMaxNone : Inherits VB_Algorithm
    ReadOnly depth As New Depth_MinMaxNone
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        labels = {"", "", "Pointcloud Max", "Pointcloud Min"}
        desc = "display the Pointcloud Min or Max in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        depth.Run(task.pointCloud)
        dst2 = depth.dst2

        If depth.options.useMax Or depth.options.useMin Then task.ogl.pointCloudInput = dst2 Else task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(task.color)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
        labels(2) = depth.labels(2)
    End Sub
End Class








Public Class OpenGL_DiffDepth : Inherits VB_Algorithm
    Dim diff As New Diff_Depth
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        labels = {"", "", "Point cloud after filtering for consistent depth", ""}
        desc = "Run OpenGL with a point cloud with consistent depth data (defined with slider in Motion_PixelDiff)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        diff.Run(src)
        dst2 = diff.dst2
        If gOptions.DebugCheckBox.Checked = False Then task.pointCloud.SetTo(0, dst2)
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(src)
        labels = diff.labels
    End Sub
End Class









Public Class OpenGL_CloudMisses : Inherits VB_Algorithm
    Dim sum8u As New History_Sum8u
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        labels = {"", "", "Point cloud after over the last X frames", ""}
        desc = "Run OpenGL removing all pixels not present for all X frames"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        sum8u.Run(task.depthMask / 255)
        dst2 = sum8u.dst2
        dst2 = dst2.Threshold(sum8u.saveFrames.Count - 1, 255, cv.ThresholdTypes.Binary)

        task.ogl.pointCloudInput.SetTo(0)
        task.pointCloud.CopyTo(task.ogl.pointCloudInput, dst2)
        task.ogl.Run(src)
    End Sub
End Class








Public Class OpenGL_CloudHistory : Inherits VB_Algorithm
    Dim hCloud As New History_Cloud
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        labels = {"", "", "Point cloud after over the last X frames", "Mask to remove partially missing pixels"}
        desc = "Run OpenGL with a masked point cloud averaged over the last X frames."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hCloud.Run(task.pointCloud)
        dst2 = hCloud.dst2

        task.ogl.pointCloudInput = dst2
        task.ogl.Run(src)
    End Sub
End Class







Public Class OpenGL_TessellateCell : Inherits VB_Algorithm
    Dim tess As New Triangle_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.tessalateTriangles
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Display a tessellated representation of the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        tess.Run(src)
        dst2 = tess.dst2
        dst3 = tess.dst3
        task.ogl.dataInput = New cv.Mat(tess.triangles.Count, 1, cv.MatType.CV_32FC3, tess.triangles.ToArray)

        task.ogl.pointCloudInput = New cv.Mat
        task.ogl.Run(tess.dst2)
        labels = tess.labels
    End Sub
End Class






Public Class OpenGL_Tessellate : Inherits VB_Algorithm
    Dim tess As New Triangle_RedCloud
    Public Sub New()
        task.ogl.oglFunction = oCase.tessalateTriangles
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Display a tessellated representation of the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        tess.Run(src)
        dst2 = tess.dst2
        dst3 = tess.dst3
        task.ogl.dataInput = New cv.Mat(tess.triangles.Count, 1, cv.MatType.CV_32FC3, tess.triangles.ToArray)

        task.ogl.pointCloudInput = New cv.Mat
        task.ogl.Run(tess.dst2)
        labels = tess.labels
    End Sub
End Class






Public Class OpenGL_TessellateRGB : Inherits VB_Algorithm
    Dim tess As New Triangle_RedCloud
    Public Sub New()
        task.ogl.oglFunction = oCase.tessalateTriangles
        task.OpenGLTitle = "OpenGL_Functions"
        desc = "Display a tessellated representation of the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        tess.Run(src)
        dst2 = tess.dst2
        dst3 = tess.dst3
        task.ogl.dataInput = New cv.Mat(tess.triangles.Count, 1, cv.MatType.CV_32FC3, tess.triangles.ToArray)

        task.ogl.pointCloudInput = New cv.Mat
        task.ogl.Run(src)
        labels = tess.labels
    End Sub
End Class







Public Class OpenGL_RedCloudCell : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = " Isolate a RedCloud cell in an OpenGL display"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("Select a RedCloud cell to see it in the OpenGL window", 3)
        redC.Run(src)
        dst2 = redC.dst2

        If task.optionsChanged Or task.frameCount = 0 Then task.ogl.pointCloudInput = task.pointCloud.EmptyClone
        task.ogl.pointCloudInput.SetTo(0)

        task.pointCloud(task.rcSelect.rect).CopyTo(task.ogl.pointCloudInput(task.rcSelect.rect), task.rcSelect.mask)
        task.ogl.Run(dst2)
        If gOptions.OpenGLCapture.Checked Then dst3 = task.ogl.dst2
    End Sub
End Class







Public Class OpenGL_RedCloud : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Display all the RedCloud cells in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(dst2)
    End Sub
End Class






Public Class OpenGL_RedTrack : Inherits VB_Algorithm
    Dim redCC As New RedTrack_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Display all the RedCC cells in OpenGL"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redCC.Run(src)
        dst2 = redCC.dst2

        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(dst2)
        setTrueText(redCC.strOut, 3)
    End Sub
End Class








Public Class OpenGL_Density2D : Inherits VB_Algorithm
    Dim dense As New Density_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        desc = "Create a mask showing which pixels are close to each other and display the results."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dense.Run(src)
        dst2.SetTo(0)
        task.pointCloud.CopyTo(dst2, dense.dst2)

        task.ogl.pointCloudInput = dst2
        task.ogl.Run(New cv.Mat(dst2.Size, cv.MatType.CV_8UC3, cv.Scalar.White))
    End Sub
End Class








Public Class OpenGL_ViewObjects : Inherits VB_Algorithm
    Dim bpDoctor As New GuidedBP_Basics
    Public Sub New()
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        desc = "Identify the objects in the scene and display them in OpenGL with their respective colors."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst1 = task.pointCloud.Clone

        bpDoctor.Run(src)
        dst2 = bpDoctor.dst2

        dst0 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst1.SetTo(0, Not dst0)

        task.ogl.pointCloudInput = dst1
        task.ogl.Run(dst2)
    End Sub
End Class







Public Class OpenGL_NoSolo : Inherits VB_Algorithm
    Dim hotTop As New BackProject_SoloTop
    Dim hotSide As New BackProject_SoloSide
    Public Sub New()
        task.useXYRange = False '
        task.ogl.oglFunction = oCase.pointCloudAndRGB
        labels(2) = "The points below were identified as solo points in the point cloud"
        desc = "Display point cloud without solo points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hotTop.Run(src)
        dst2 = hotTop.dst3

        hotSide.Run(src)
        dst2 = dst2 Or hotSide.dst3

        If gOptions.DebugCheckBox.Checked = False Then task.pointCloud.SetTo(0, dst2)
        task.ogl.pointCloudInput = task.pointCloud
        task.ogl.Run(src)
        setTrueText("Toggle the solo points on and off using the 'DebugCheckBox' global option.", 3)
    End Sub
End Class
