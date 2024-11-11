Imports cvb = OpenCvSharp
Imports System.Windows.Forms
Imports System.IO.Pipes
Imports System.Drawing
Imports System.IO
Imports System.Runtime.InteropServices

<StructLayout(LayoutKind.Sequential)>
Public Class VBtask : Implements IDisposable
    Public featureMask As cvb.Mat
    Public fLessMask As cvb.Mat
    Public featureRects As New List(Of cvb.Rect)
    Public fLessRects As New List(Of cvb.Rect)
    Public flessBoundary As New cvb.Mat
    Public lowResColor As cvb.Mat
    Public lowResDepth As cvb.Mat

    Public gridSize As Integer
    Public gridRows As Integer
    Public gridCols As Integer
    Public gridIndex As New List(Of Integer)
    Public gridRects As List(Of cvb.Rect)
    Public subDivisions As New List(Of Integer)
    Public subDivisionCount As Integer = 9
    Public gridMask As cvb.Mat
    Public gridMap32S As New cvb.Mat
    Public gridNeighbors As New List(Of List(Of Integer))
    Public gridNabeRects As New List(Of cvb.Rect)
    Public gridROIclicked As Integer

    Public fpList As New List(Of fPoint)
    Public fpIDlist As New List(Of Single)
    Public fpLastList As New List(Of fPoint)
    Public fpLastIDs As New List(Of Single)
    Public fpMap As cvb.Mat
    Public fpOutline As cvb.Mat
    Public fpSelected As New fPoint
    Public fPointMinDistance As Integer
    Public fpCorners(3) As Integer
    Public fpCornerRect(3) As cvb.Rect
    Public fpSearchRect(3) As cvb.Rect
    Public fpTravelAvg As Single
    Public fpMotion As cvb.Point2f

    Public optionsChanged As Boolean = True ' global or local options changed.
    Public rows As Integer
    Public cols As Integer
    Public TaskTimer As New System.Timers.Timer(1000)

    Public dst0 As cvb.Mat
    Public dst1 As cvb.Mat
    Public dst2 As cvb.Mat
    Public dst3 As cvb.Mat

    Public MainUI_Algorithm As Object
    Public myStopWatch As Stopwatch
    Public lowRes As cvb.Size
    Public quarterRes As cvb.Size
    Public displayRes As cvb.Size

    Public cvFontSize As Single = 0.8
    Public cvFontThickness As Integer = 1

    Public color As cvb.Mat
    Public leftView As cvb.Mat
    Public rightView As cvb.Mat
    Public pointCloud As cvb.Mat
    Public pcSplit() As cvb.Mat
    Public gMatrix As cvb.Mat ' transformation matrix to convert point cloud to be vertical according to gravity.
    Public noDepthMask As New cvb.Mat
    Public depthMask As New cvb.Mat
    Public paletteGradient As cvb.Mat
    Public maxDepthMask As New cvb.Mat
    Public depthRGB As New cvb.Mat
    Public srcThread As cvb.Mat

    Public motionRect As New cvb.Rect ' get rid of this...
    Public motionRects As New List(Of cvb.Rect)
    Public motionMask As cvb.Mat
    Public noMotionMask As cvb.Mat
    Public motion As Motion_Basics
    Public motionPercent As Single
    Public MotionLabel As String = " "
    Public topFeatures As New List(Of cvb.Point2f)

    Public camMotionPixels As Single ' distance in pixels that the camera has moved.
    Public camDirection As Single ' camera direction in radians.

    ' add any global algorithms here
    Public gravityHorizon As Gravity_Horizon
    Public PixelViewer As Pixel_Viewer
    Public colorizer As Depth_Colorizer_CPP_VB
    Public rgbFilter As Object
    Public gMat As IMU_GMatrix
    Public IMUBasics As IMU_Basics
    Public IMU_Rotation As System.Numerics.Quaternion
    Public cellStats As Cell_Basics
    Public imuStabilityTest As Stabilizer_VerticalIMU
    Public grid As Grid_Basics
    Public ogl As OpenGL_Basics
    Public palette As Palette_LoadColorMap

    Public drawRotatedRect As Draw_RotatedRect
    Public centerRect As cvb.Rect

    Public pythonPipeIn As NamedPipeServerStream
    Public pythonPipeOut As NamedPipeServerStream
    Public pythonTaskName As String
    Public pythonProcess As Process
    Public pythonReady As Boolean
    Public pythonPipeIndex As Integer

    Public openGL_hwnd As IntPtr
    Public openGLPipe As NamedPipeServerStream
    Public pipeCount As Integer

    Public gifCreator As Gif_OpenCVB
    Public gifImages As New List(Of Bitmap)
    Public gifBuild As Boolean
    Public gifCaptureIndex As Integer

    Public transformationMatrix() As Single

    Public frameCount As Integer = 0
    Public heartBeat As Boolean
    Public heartBeatLT As Boolean = True ' long term heartbeat - every X seconds.
    Public quarterBeat As Boolean
    Public quarter(4) As Boolean
    Public midHeartBeat As Boolean
    Public almostHeartBeat As Boolean
    Public msWatch As Integer
    Public msLast As Integer
    Public FirstPass As Boolean

    Public toggleOnOff As Boolean ' toggles on the heartbeat.
    Public paused As Boolean
    Public showAllOptions As Boolean ' show all options when initializing the options for all algorithms.

    Public pcFloor As Single ' y-value for floor...
    Public pcCeiling As Single ' y-value for ceiling...

    Public debugSyncUI As Boolean

    Public disparityAdjustment As Single ' adjusts for resolution and some hidden elements.

    Public gravityVec As New PointPair
    Public horizonVec As New PointPair

    Public IMU_RawAcceleration As cvb.Point3f
    Public IMU_Acceleration As cvb.Point3f
    Public IMU_AverageAcceleration As cvb.Point3f
    Public IMU_RawAngularVelocity As cvb.Point3f
    Public IMU_AngularVelocity As cvb.Point3f
    Public kalmanIMUacc As cvb.Point3f
    Public kalmanIMUvelocity As cvb.Point3f
    Public IMU_TimeStamp As Double
    Public IMU_Translation As cvb.Point3f
    Public IMU_AngularAcceleration As cvb.Point3f
    Public IMU_FrameTime As Double
    Public IMU_AlphaFilter As Single ' high pass and low pass filter of the IMU acceleration data.

    Public accRadians As cvb.Point3f  ' rotation angles around x/y/z-axis to align with gravity
    Public theta As cvb.Point3f ' velocity-filtered angles around x/y/z-axis to align with gravity
    Public verticalizeAngle As Double

    Public pitch As Single
    Public yaw As Single
    Public roll As Single

    Public useGravityPointcloud As Boolean

    Public cameraStable As Boolean
    Public cameraStableString As String

    Public recordTimings As Boolean = True

    Public HighlightColor As cvb.Scalar ' color to use to highlight objects in an image.

    Public histogramBins As Integer

    Public gOptions As OptionsGlobal
    Public redOptions As OptionsRedCloud

    Public paletteIndex As Integer

    Public mouseClickFlag As Boolean
    Public ClickPoint As cvb.Point
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mouseMovePoint As cvb.Point ' trace any mouse movements using this.
    Public mouseMovePointUpdated As Boolean

    Public DotSize As Integer
    Public lineWidth As Integer
    Public lineType As cvb.LineTypes

    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double

    Public drawRect As cvb.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public drawRectUpdated As Boolean

    Public pixelViewerRect As cvb.Rect
    Public pixelViewTag As Integer

    Public pipeName As String

    Public labels(4 - 1) As String
    Public desc As String
    Public advice As String = ""
    Public intermediateName As String
    Public intermediateObject As TaskParent
    Public activeObjects As New List(Of Object)
    Public pixelViewerOn As Boolean

    Public scalarColors(255) As cvb.Scalar
    Public vecColors(255) As cvb.Vec3b

    Public topCameraPoint As cvb.Point
    Public sideCameraPoint As cvb.Point

    Public hFov As Single
    Public vFov As Single
    Public focalLength As Single ' distance between cameras
    Public baseline As Single ' in meters

    Public algName As String
    Public cameraName As String
    Public calibData As cameraInfo
    Public HomeDir As String
    Public fpsRate As Integer
    Public densityMetric As Integer ' how dense is the pointcloud in z - heuristic.
    Public FASTthreshold As Integer

    Public externalPythonInvocation As Boolean
    Public useRecordedData As Boolean
    Public testAllRunning As Boolean
    Public showConsoleLog As Boolean

    Public mainFormLocation As cvb.Rect
    Public main_hwnd As IntPtr

    Public trueData As New List(Of TrueText)

    Public callTraceMain As New List(Of String)
    Public algorithm_msMain As New List(Of Single)
    Public algorithmNamesMain As New List(Of String)

    Public waitingForInput As Single ' the amount of time waiting for buffers.
    Public inputBufferCopy As Single ' the amount of time copying the buffers.
    Public returnCopyTime As Single ' the amount of time returning buffers to the host.

    Public OpenGLTitle As String
    Public oglRect As cvb.Rect
    Public polyCount As Integer

    Public rangesTop() As cvb.Rangef
    Public rangesSide() As cvb.Rangef
    Public channelsTop() As Integer
    Public channelsSide() As Integer
    Public bins2D() As Integer
    Public frameHistoryCount As Integer ' count of how much history to use for the point cloud.
    Public depthThresholdPercent As Single

    Public projectionThreshold As Integer ' In heatmap views, this defines what is hot in a heatmap.

    Public rc As New rcData
    Public redCells As New List(Of rcData)
    Public cellMap As cvb.Mat

    Public features As New List(Of cvb.Point2f)
    Public featurePoints As New List(Of cvb.Point)
    Public featureMotion As Boolean ' false means that none of the features moved.

    Public useXYRange As Boolean ' OpenGL applications don't need to adjust the ranges.
    Public xRange As Single
    Public yRange As Single
    Public xRangeDefault As Single
    Public yRangeDefault As Single
    Public MaxZmeters As Single
    Public metersPerPixel As Single
    Public OpenGL_Left As Integer
    Public OpenGL_Top As Integer
    Public Structure inBuffer
        Dim color As cvb.Mat
        Dim leftView As cvb.Mat
        Dim rightView As cvb.Mat
        Dim pointCloud As cvb.Mat
    End Structure
    Public Structure cameraInfo
        Public ppx As Single
        Public ppy As Single
        Public fx As Single
        Public fy As Single
        Dim v_fov As Single ' vertical field of view in degrees.
        Dim h_fov As Single ' horizontal field of view in degrees.
        Dim d_fov As Single ' diagonal field of view in degrees.
    End Structure

    Public Structure algParms
        ' The order of cameras in cameraNames is important. Add new cameras at the end.
        '  "StereoLabs ZED 2/2i C++", turned off
        Public Shared cameraNames As New List(Of String)({"Azure Kinect 4K",
                                                          "Azure Kinect 4K C++",
                                                          "Intel(R) RealSense(TM) Depth Camera 435i",
                                                          "Intel(R) RealSense(TM) Depth Camera 455",
                                                          "Oak-D camera",
                                                          "StereoLabs ZED 2/2i",
                                                          "MYNT-EYE-D1000",
                                                          "Orbbec Gemini 335L"})
        Public cameraName As String
        Public cameraIndex As Integer

        Public HomeDir As String
        Public useRecordedData As Boolean
        Public externalPythonInvocation As Boolean ' OpenCVB was initialized remotely...
        Public showConsoleLog As Boolean
        Public testAllRunning As Boolean
        Public RotationMatrix() As Single
        Public RotationVector As cvb.Point3f

        Public mainFormLocation As cvb.Rect
        Public main_hwnd As IntPtr

        Public fpsRate As Integer
        Public workingRes As cvb.Size
        Public captureRes As cvb.Size ' DisparityIn-verted_Basics needs the full resolution to compute disparity.
        Public displayRes As cvb.Size

        Public algName As String

        Public cameraInfo As cameraInfo
    End Structure
    Private Sub buildColors()
        Dim rand = New Random(1)
        Dim bgr(3) As Byte
        For i = 0 To task.vecColors.Length - 1
            rand.NextBytes(bgr)
            task.vecColors(i) = New cvb.Vec3b(bgr(0), bgr(1), bgr(2))
            task.scalarColors(i) = New cvb.Scalar(task.vecColors(i)(0), task.vecColors(i)(1), task.vecColors(i)(2))
        Next
    End Sub
    Private Sub VBTaskTimerPop(sender As Object, e As EventArgs)
        Static WarningIssued As Boolean = False
        If frameCount > 0 And WarningIssued = False Then
            WarningIssued = True
            Debug.WriteLine("Warning: " + task.algName + " has not completed work on a frame in a second.")
        End If
    End Sub
    Public Sub OpenGLClose()
        If openGL_hwnd <> 0 Then
            Dim r As RECT
            GetWindowRect(openGL_hwnd, r)
            Dim wRect = New cvb.Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top)
            SaveSetting("OpenCVB", "OpenGLtaskX", "OpenGLtaskX", wRect.X)
            SaveSetting("OpenCVB", "OpenGLtaskY", "OpenGLtaskY", wRect.Y)
            SaveSetting("OpenCVB", "OpenGLtaskWidth", "OpenGLtaskWidth", wRect.Width)
            openGLPipe.Close()
            openGL_hwnd = 0
        End If
    End Sub
    Public Sub New()
    End Sub
    Public Sub New(parms As algParms)
        AddHandler TaskTimer.Elapsed, New Timers.ElapsedEventHandler(AddressOf VBTaskTimerPop)
        TaskTimer.AutoReset = True
        TaskTimer.Enabled = True
        Randomize() ' just in case anyone uses VB.Net's Rnd

        task = Me
        useXYRange = True ' Most projections of pointcloud data can use the xRange and yRange to improve results.
        gridRects = New List(Of cvb.Rect)
        FirstPass = True
        algName = parms.algName
        cameraName = parms.cameraName
        testAllRunning = parms.testAllRunning
        showConsoleLog = parms.showConsoleLog
        fpsRate = parms.fpsRate
        calibData = parms.cameraInfo
        HomeDir = parms.HomeDir
        main_hwnd = parms.main_hwnd
        useRecordedData = parms.useRecordedData
        externalPythonInvocation = parms.externalPythonInvocation

        mainFormLocation = parms.mainFormLocation
        displayRes = parms.displayRes
        rows = parms.workingRes.Height
        cols = parms.workingRes.Width
        task.optionsChanged = True

        dst0 = New cvb.Mat(rows, cols, cvb.MatType.CV_8UC3, New cvb.Scalar)
        dst1 = New cvb.Mat(rows, cols, cvb.MatType.CV_8UC3, New cvb.Scalar)
        dst2 = New cvb.Mat(rows, cols, cvb.MatType.CV_8UC3, New cvb.Scalar)
        dst3 = New cvb.Mat(rows, cols, cvb.MatType.CV_8UC3, New cvb.Scalar)

        OpenGL_Left = CInt(GetSetting("OpenCVB", "OpenGLtaskX", "OpenGLtaskX", task.mainFormLocation.X))
        OpenGL_Top = CInt(GetSetting("OpenCVB", "OpenGLtaskY", "OpenGLtaskY", task.mainFormLocation.Y))

        buildColors()
        pythonTaskName = HomeDir + "Python\" + algName

        allOptions = New OptionsContainer
        allOptions.Show()

        callTrace.Add("Options_XYRanges") ' so calltrace is not nothing on initial call...
        gOptions = New OptionsGlobal
        redOptions = New OptionsRedCloud
        task.cellMap = New cvb.Mat(New cvb.Size(task.dst2.Width, task.dst2.Height), cvb.MatType.CV_8U, cvb.Scalar.All(0))

        grid = New Grid_Basics
        PixelViewer = New Pixel_Viewer

        colorizer = New Depth_Colorizer_CPP_VB
        IMUBasics = New IMU_Basics
        gMat = New IMU_GMatrix
        gravityHorizon = New Gravity_Horizon
        imuStabilityTest = New Stabilizer_VerticalIMU
        motion = New Motion_Basics

        updateSettings()
        task.redOptions.Show()
        task.gOptions.Show()
        palette = New Palette_LoadColorMap
        ogl = New OpenGL_Basics
        drawRotatedRect = New Draw_RotatedRect
        centerRect = New cvb.Rect(dst2.Width / 4, dst2.Height / 4, dst2.Width / 2, dst1.Height / 2)

        callTrace.Clear()
        task.callTraceMain.Clear()
        task.algorithm_msMain.Clear()
        task.algorithmNamesMain.Clear()
        callTrace.Add(algName + "\")
        activeObjects.Clear()

        If task.advice = "" Then
            task.advice = "No advice for " + algName + " yet." + vbCrLf +
                               "Please use 'UpdateAdvice(<your advice>)' in the constructor)."
        End If

        fpList.Clear()
        fpOutline = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
        fpMap = New cvb.Mat(dst2.Size, cvb.MatType.CV_32S, 0)
        fpSelected = New fPoint

        If parms.useRecordedData Then recordedData = New Replay_Play()

        ' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
        ' https://www.intelrealsense.com/depth-camera-d435i/
        ' https://www.intelrealsense.com/depth-camera-d455/
        ' https://towardsdatascience.com/opinion-26190c7fed1b
        ' https://support.stereolabs.com/hc/en-us/articles/360007395634-What-is-the-camera-focal-length-and-field-of-view-
        ' https://www.stereolabs.com/assets/datasheets/zed-2i-datasheet-feb2022.pdf
        ' https://www.mynteye.com/pages/mynt-eye-d
        ' https://www.orbbec.com/products/stereo-vision-camera/gemini-335l/
        ' order of cameras is the same as the order above... see cameraNames above
        Dim vFOVangles() As Single = {59, 59, 72, 58, 42.5, 57, 57, 62, 68} ' all values from the specification - this is usually overridden by calibration data.
        Dim hFOVangles() As Single = {90, 90, 104, 105, 69.4, 86, 86, 69, 94} ' all values from the specification - this is usually overridden by calibration data.
        Dim focalLengths() As Single = {5.5, 5.5, 3.4, 1.88, 4.81, 2.31, 2.31, 2.45, 2.45}
        Dim baseLines() As Single = {0.074, 0.074, 0.073, 0.055, 0.052, 0.06, 0.06, 0.048, 0.048} ' in meters

        ' NOTE: I can't find the VFOV for the Oak-D or Oak-D Lite cameras.
        ' The 62 is based on Pythagorean theorem and knowing the 71.8 HFOV and the 81.3 DFOV.
        If parms.cameraInfo.v_fov <> 0 Then vFOVangles(parms.cameraIndex) = parms.cameraInfo.v_fov
        If parms.cameraInfo.h_fov <> 0 Then hFOVangles(parms.cameraIndex) = parms.cameraInfo.h_fov

        vFov = vFOVangles(parms.cameraIndex)  ' these are default values in case the calibration data is unavailable
        hFov = hFOVangles(parms.cameraIndex)
        focalLength = focalLengths(parms.cameraIndex)
        baseline = baseLines(parms.cameraIndex)

        task.myStopWatch = Stopwatch.StartNew()
        task.optionsChanged = True
        Application.DoEvents()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        allOptions.Close()
        If openGL_hwnd <> 0 Then
            task.OpenGLClose()
            openGL_hwnd = 0
        End If
        TaskTimer.Enabled = False
    End Sub
    Public Sub TrueText(text As String, pt As cvb.Point, Optional picTag As Integer = 2)
        Dim str As New TrueText(text, pt, picTag)
        task.trueData.Add(str)
    End Sub
    Public Sub setSelectedContour(ByRef redCells As List(Of rcData), ByRef cellMap As cvb.Mat)
        Static ptNew As New cvb.Point
        If redCells.Count = 0 Then Exit Sub
        If task.ClickPoint = ptNew And redCells.Count > 1 Then task.ClickPoint = redCells(1).maxDist
        Dim index = cellMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X)
        task.rc = redCells(index)
        If index > 0 And index < task.redCells.Count Then
            ' task.ClickPoint = redCells(index).maxDist
            task.rc = redCells(index)
        End If
    End Sub
    Public Sub setSelectedContour()
        If task.redCells.Count = 0 Then Exit Sub
        If task.ClickPoint = newPoint And task.redCells.Count > 1 Then task.ClickPoint = task.redCells(1).maxDist
        Dim index = task.cellMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X)
        If index > 0 And index < task.redCells.Count Then
            ' task.ClickPoint = task.redCells(index).maxDist
            task.rc = task.redCells(index)
        Else
            task.rc = task.redCells(0)
        End If
    End Sub
    Private Function checkIntermediateResults(lookupName As String) As TaskParent
        If task.algName.StartsWith("CPP_") Then Return Nothing ' we don't currently support intermediate results for CPP_ algorithms.
        For Each obj In task.activeObjects
            If obj.traceName = lookupName And task.FirstPass = False Then Return obj
        Next
        Return Nothing
    End Function
    Public Sub DrawLine(dst As cvb.Mat, p1 As cvb.Point2f, p2 As cvb.Point2f, color As cvb.Scalar)
        dst.Line(p1, p2, color, task.lineWidth, task.lineType)
    End Sub
    Private Sub postProcess(src As cvb.Mat)
        Try
            ' make sure that any outputs from the algorithm are the right size.nearest
            If dst0.Size <> New cvb.Size(task.color.Width, task.color.Height) And dst0.Width > 0 Then dst0 = dst0.Resize(New cvb.Size(task.color.Width, task.color.Height), 0, 0, cvb.InterpolationFlags.Nearest)
            If dst1.Size <> New cvb.Size(task.color.Width, task.color.Height) And dst1.Width > 0 Then dst1 = dst1.Resize(New cvb.Size(task.color.Width, task.color.Height), 0, 0, cvb.InterpolationFlags.Nearest)
            If dst2.Size <> New cvb.Size(task.color.Width, task.color.Height) And dst2.Width > 0 Then dst2 = dst2.Resize(New cvb.Size(task.color.Width, task.color.Height), 0, 0, cvb.InterpolationFlags.Nearest)
            If dst3.Size <> New cvb.Size(task.color.Width, task.color.Height) And dst3.Width > 0 Then dst3 = dst3.Resize(New cvb.Size(task.color.Width, task.color.Height), 0, 0, cvb.InterpolationFlags.Nearest)

            If task.pixelViewerOn Then
                task.PixelViewer.viewerForm.Visible = True
                task.PixelViewer.viewerForm.Show()
                task.PixelViewer.Run(src)
            Else
                task.PixelViewer.viewerForm.Visible = False
            End If
            If task.intermediateObject IsNot Nothing Then
                dst0 = task.intermediateObject.dst0
                dst1 = task.intermediateObject.dst1
                dst2 = task.intermediateObject.dst2
                dst3 = task.intermediateObject.dst3
            End If
            dst0 = If(task.gOptions.displayDst0.Checked, dst0, task.color)
            dst1 = If(task.gOptions.displayDst1.Checked, dst1, task.depthRGB)

            Dim lookupName = task.intermediateName
            If lookupName = "" Then lookupName = task.algName
            Dim obj = checkIntermediateResults(lookupName)
            If task.intermediateName <> "" Then task.intermediateObject = obj

            If task.algName.EndsWith("_CS") = False Then task.trueData = New List(Of TrueText)(trueData)

            If task.gOptions.displayDst0.Checked Then dst0 = Check8uC3(obj.dst0) Else dst0 = task.color
            If task.gOptions.displayDst1.Checked Then dst1 = Check8uC3(obj.dst1) Else dst1 = task.depthRGB

            If lookupName.EndsWith("_CC") Or lookupName.StartsWith("CPP_") Or lookupName.EndsWith(".py") Then
                dst2 = If(dst2.Type = cvb.MatType.CV_8UC3, dst2, Check8uC3(dst2))
                dst3 = If(dst3.Type = cvb.MatType.CV_8UC3, dst3, Check8uC3(dst3))
                task.labels = labels
            Else
                dst2 = If(obj.dst2.Type = cvb.MatType.CV_8UC3, obj.dst2, Check8uC3(obj.dst2))
                dst3 = If(obj.dst3.Type = cvb.MatType.CV_8UC3, obj.dst3, Check8uC3(obj.dst3))
                task.labels = obj.labels
                task.trueData = New List(Of TrueText)(trueData)
                If task.algName.EndsWith("_CS") = False Then task.trueData = New List(Of TrueText)(obj.trueData)
            End If

            If task.gifCreator IsNot Nothing Then task.gifCreator.createNextGifImage()

            If dst2.Size <> task.color.Size Then
                dst2 = dst2.Resize(New cvb.Size(task.color.Width, task.color.Height), cvb.InterpolationFlags.Nearest)
            End If

            If dst3.Size <> task.color.Size Then
                dst3 = dst3.Resize(New cvb.Size(task.color.Width, task.color.Height), cvb.InterpolationFlags.Nearest)
            End If

            If dst2.Width = task.dst2.Width And dst2.Height = task.dst2.Height Then
                If task.gOptions.ShowGrid.Checked Then dst2.SetTo(cvb.Scalar.White, task.gridMask)
            End If

            Dim rc = task.rc
            If task.redCells.Count > 0 Then setSelectedContour()

            If task.redOptions.IdentifyCells.Checked Then
                Dim ptNew As New cvb.Point
                Dim ptCells As New List(Of cvb.Point)
                For i = 1 To redCells.Count - 1
                    Dim rcx = redCells(i)
                    If ptCells.Contains(rcx.maxDStable) = False Then
                        If rcx.maxDStable <> ptNew And rcx.index <= task.redOptions.identifyCount Then
                            Dim str As New TrueText(CStr(rcx.index), rcx.maxDStable, 2)
                            trueData.Add(str)
                        End If
                        ptCells.Add(rcx.maxDStable)
                    End If
                Next

                If rc.index > 0 Then
                    task.color.Rectangle(rc.rect, cvb.Scalar.Yellow, task.lineWidth)
                    task.color(rc.rect).SetTo(cvb.Scalar.White, rc.mask)

                    task.depthRGB.Rectangle(rc.rect, cvb.Scalar.Yellow, task.lineWidth)
                    If task.redOptions.DisplayCellStats.Checked Then
                        dst3.SetTo(0)
                        If task.ClickPoint = newPoint Then
                            If task.redCells.Count > 1 Then
                                task.rc = task.redCells(1)
                                task.ClickPoint = task.rc.maxDist
                            End If
                        End If
                        If cellStats Is Nothing Then cellStats = New Cell_Basics
                        cellStats.statsString()
                        dst1 = cellStats.dst1
                        Dim str As New TrueText(cellStats.strOut, New cvb.Point, 3)
                        trueData.Add(str)
                    End If
                End If
            End If

            If task.redOptions.DisplayCellStats.Checked And task.ClickPoint = newPoint Then
                If task.redCells.Count > 1 Then
                    task.rc = task.redCells(1)
                    task.ClickPoint = task.rc.maxDist
                End If
            End If

            If task.gOptions.showMotionMask.Checked Then
                For Each roi In task.motionRects
                    task.color.Rectangle(roi, cvb.Scalar.White, task.lineWidth)
                Next
            End If

            gravityHorizon.RunAlg(src)
            If task.gOptions.CrossHairs.Checked Then
                If task.paused = False Then
                    DrawLine(task.color, task.horizonVec.p1, task.horizonVec.p2, cvb.Scalar.White)
                    DrawLine(task.color, task.gravityVec.p1, task.gravityVec.p2, cvb.Scalar.White)
                End If
            End If

            task.optionsChanged = False
            TaskTimer.Enabled = False
            task.frameCount += 1

            task.callTraceMain = New List(Of String)(callTrace)
            task.algorithm_msMain = New List(Of Single)(algorithm_ms)
            task.algorithmNamesMain = New List(Of String)(algorithmNames)
        Catch ex As Exception
            Debug.WriteLine("Active Algorithm exception occurred: " + ex.Message)
        End Try
    End Sub
    Public Function GetMinMax(mat As cvb.Mat, Optional mask As cvb.Mat = Nothing) As mmData
        Dim mm As mmData
        If mask Is Nothing Then
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc)
        Else
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, mask)
        End If
        Return mm
    End Function
    Public Sub RunAlgorithm()
        If allOptions.titlesAdded Then
            allOptions.titlesAdded = False
            allOptions.layoutOptions(normalRequest:=True)
        End If

        Application.DoEvents()
        updateSettings()

        If task.testAllRunning = False Then
            If algorithm_ms.Count = 0 Then
                algorithmNames.Add("waitingForInput")
                algorithmTimes.Add(Now)
                algorithm_ms.Add(0)

                algorithmNames.Add("inputBufferCopy")
                algorithmTimes.Add(Now)
                algorithm_ms.Add(0)

                algorithmNames.Add("ReturnCopyTime")
                algorithmTimes.Add(Now)
                algorithm_ms.Add(0)

                algorithmNames.Add(task.algName)
                algorithmTimes.Add(Now)
                algorithm_ms.Add(0)

                algorithmStack = New Stack()
                algorithmStack.Push(0)
                algorithmStack.Push(1)
                algorithmStack.Push(2)
                algorithmStack.Push(3)
            End If

            algorithm_ms(0) += waitingForInput
            algorithm_ms(1) += inputBufferCopy
            algorithm_ms(2) += returnCopyTime
            algorithmTimes(3) = Now  ' starting the main algorithm
        End If
        If task.useRecordedData Then recordedData.Run(task.color.Clone)

        task.redOptions.Sync()

        Dim src = task.color
        If src.Size <> New cvb.Size(task.dst2.Cols, task.dst2.Rows) Then dst2 = dst2.Resize(src.Size)
        If src.Size <> New cvb.Size(task.dst3.Cols, task.dst3.Rows) Then dst3 = dst3.Resize(src.Size)
        task.bins2D = {task.dst2.Height, task.dst2.Width}

        ' If the WorkingRes changes, the previous generation of images needs to be reset.
        If task.pointCloud.Size <> New cvb.Size(cols, rows) Or task.color.Size <> task.dst2.Size Then
            task.pointCloud = New cvb.Mat(rows, cols, cvb.MatType.CV_32FC3, cvb.Scalar.All(0))
            task.noDepthMask = New cvb.Mat(rows, cols, cvb.MatType.CV_8U, cvb.Scalar.All(0))
            task.depthMask = New cvb.Mat(rows, cols, cvb.MatType.CV_8U, cvb.Scalar.All(0))
        End If

        ' run any universal algorithms here
        task.IMU_RawAcceleration = task.IMU_Acceleration
        task.IMU_RawAngularVelocity = task.IMU_AngularVelocity
        task.IMU_AlphaFilter = 0.5 '  task.gOptions.imu_Alpha
        grid.RunAlg(task.color)

        imuStabilityTest.RunAlg(src)
        task.cameraStable = imuStabilityTest.stableTest
        task.cameraStableString = imuStabilityTest.stableStr
        IMUBasics.RunAlg(src)
        gMat.RunAlg(src)

        If task.gOptions.CreateGif.Checked Then
            heartBeat = False
            task.optionsChanged = False
        Else
            task.heartBeat = task.heartBeat Or task.debugSyncUI Or task.optionsChanged Or task.mouseClickFlag
        End If

        If task.paused = False Then
            task.frameHistoryCount = task.gOptions.FrameHistory.Value

            If task.useGravityPointcloud Then
                If task.pointCloud.Size <> src.Size Then
                    task.pointCloud = New cvb.Mat(src.Size, cvb.MatType.CV_32FC3, 0)
                End If

                '******* this is the gravity rotation *******
                task.pointCloud = (task.pointCloud.Reshape(1, src.Rows * src.Cols) * task.gMatrix).
                                   ToMat.Reshape(3, src.Rows)
            End If

            If task.pcSplit Is Nothing Then task.pcSplit = task.pointCloud.Split

            task.gOptions.unFiltered.Checked = True ' until the motion rectangle problems are resolved.
        End If

        motion.Run(src)

        If task.gOptions.UseMotionConstructed.Checked Then
            task.color = motion.color.Clone
            task.pointCloud = motion.pointcloud.Clone
            task.motionMask = motion.motionMask
            task.noMotionMask = Not motionMask
            task.motionRects = New List(Of cvb.Rect)(motion.measure.motionRects)
        End If

        task.pcSplit = task.pointCloud.Split

        If task.optionsChanged Then task.maxDepthMask.SetTo(0)
        task.pcSplit(2) = task.pcSplit(2).Threshold(task.MaxZmeters, task.MaxZmeters,
                                                            cvb.ThresholdTypes.Trunc)

        task.depthMask = task.pcSplit(2).Threshold(0, 255, cvb.ThresholdTypes.Binary).
                                         ConvertScaleAbs()

        task.noDepthMask = Not task.depthMask

        If task.xRange <> task.xRangeDefault Or task.yRange <> task.yRangeDefault Then
            Dim xRatio = task.xRangeDefault / task.xRange
            Dim yRatio = task.yRangeDefault / task.yRange
            task.pcSplit(0) *= xRatio
            task.pcSplit(1) *= yRatio

            cvb.Cv2.Merge(task.pcSplit, task.pointCloud)
        End If

        colorizer.RunAlg(task.pcSplit(2).Threshold(task.MaxZmeters, task.MaxZmeters, cvb.ThresholdTypes.Trunc))
        task.depthRGB = colorizer.dst2.Clone

        TaskTimer.Enabled = True

        If task.gOptions.CreateGif.Checked Then
            If task.gifCreator Is Nothing Then task.gifCreator = New Gif_OpenCVB
            gifCreator.RunAlg(src)
            If task.gifBuild Then
                task.gifBuild = False
                If task.gifImages.Count = 0 Then
                    MsgBox("Collect images first and then click 'Build GIF...'")
                Else
                    For i = 0 To task.gifImages.Count - 1
                        Dim fileName As New FileInfo(task.HomeDir + "Temp/image" + Format(i, "000") + ".bmp")
                        task.gifImages(i).Save(fileName.FullName)
                    Next

                    task.gifImages.Clear()
                    Dim dirInfo As New DirectoryInfo(task.HomeDir + "GifBuilder\bin\x64\Release\net48\")
                    Dim dirData = dirInfo.GetDirectories()
                    Dim gifExe As New FileInfo(dirInfo.FullName + "GifBuilder.exe")
                    If gifExe.Exists = False Then
                        MsgBox("GifBuilder.exe was not found!")
                    Else
                        Dim gifProcess As New Process
                        gifProcess.StartInfo.FileName = gifExe.FullName
                        gifProcess.StartInfo.WorkingDirectory = task.HomeDir + "Temp/"
                        gifProcess.Start()
                    End If
                End If
            End If
        End If

        'If task.gOptions.RGBFilterActive.Checked Then
        '    Dim filterName = task.gOptions.RGBFilterList.Text
        '    If rgbFilter Is Nothing Then rgbFilter = algoList.createVBAlgorithm(filterName)
        '    If rgbFilter.traceName <> filterName Then rgbFilter = algoList.createVBAlgorithm(filterName)
        '    rgbFilter.RunAlg(src)
        '    src = rgbFilter.dst2
        'End If

        If task.paused = False Then
            task.trueData.Clear()
            MainUI_Algorithm.processFrame(src.Clone) ' <<<<<<<< This is where the VB algorithm runs...
            task.FirstPass = False
            task.heartBeatLT = False
            postProcess(src)
        End If
    End Sub
End Class
