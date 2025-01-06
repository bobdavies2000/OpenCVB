Imports cv = OpenCvSharp
Imports System.Windows.Forms
Imports System.IO.Pipes
Imports System.Drawing
Imports System.IO
Imports System.Runtime.InteropServices

#Region "taskProcess"
<StructLayout(LayoutKind.Sequential)>
Public Class VBtask : Implements IDisposable
    Public lpList As New List(Of linePoints)

    Public gridSize As Integer
    Public gridRows As Integer
    Public gridCols As Integer
    Public gridIndex As New List(Of Integer)
    Public gridRects As List(Of cv.Rect)
    Public subDivisions As New List(Of Integer)
    Public subDivisionCount As Integer = 9
    Public gridMask As cv.Mat
    Public gridMap32S As New cv.Mat
    Public gridNeighbors As New List(Of List(Of Integer))
    Public gridNabeRects As New List(Of cv.Rect) ' The surrounding rect for every gridRect
    Public gridROIclicked As Integer
    Public gridPoints As New List(Of cv.Point) ' the list of each gridRect corner 

    Public fpList As New List(Of fpData)
    Public fpListLast As New List(Of fpData)
    Public fpIDlist As New List(Of Single)

    Public fpMap As cv.Mat
    Public fpMapLast As cv.Mat

    Public fpOutline As cv.Mat
    Public fpSelected As fpData
    Public fPointMinDistance As Integer
    Public fpCorners(3) As Integer
    Public fpCornerRect(3) As cv.Rect
    Public fpSearchRect(3) As cv.Rect
    Public fpTravelAvg As Single
    Public fpMotion As cv.Point2f

    Public topFeatures As New List(Of cv.Point2f)
    Public features As New List(Of cv.Point2f)
    Public featurePoints As New List(Of cv.Point)

    Public featureMask As cv.Mat
    Public fLessMask As cv.Mat
    Public featureRects As New List(Of cv.Rect)
    Public fLessRects As New List(Of cv.Rect)
    Public flessBoundary As New cv.Mat
    Public lowResColor As cv.Mat
    Public lowResDepth As cv.Mat

    Public motionRect As New cv.Rect ' get rid of this...
    Public motionRects As New List(Of cv.Rect)
    Public motionMask As cv.Mat
    Public motionPercent As Single
    Public MotionLabel As String = " "

    Public optionsChanged As Boolean = True ' global or local options changed.
    Public rows As Integer
    Public cols As Integer
    Public workingRes As cv.Size
    Public TaskTimer As New System.Timers.Timer(1000)

    Public dst0 As cv.Mat
    Public dst1 As cv.Mat
    Public dst2 As cv.Mat
    Public dst3 As cv.Mat

    Public MainUI_Algorithm As Object
    Public myStopWatch As Stopwatch
    Public lowRes As cv.Size
    Public quarterRes As cv.Size
    Public displayRes As cv.Size

    Public cvFontSize As Single = 0.8
    Public cvFontThickness As Integer = 1

    Public color As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public pointCloud As cv.Mat
    Public pcSplit() As cv.Mat
    Public gMatrix As cv.Mat ' transformation matrix to convert point cloud to be vertical according to gravity.
    Public IMU_Rotation As System.Numerics.Quaternion
    Public noDepthMask As New cv.Mat
    Public depthMask As New cv.Mat
    Public maxDepthMask As New cv.Mat
    Public depthRGB As New cv.Mat
    Public srcThread As cv.Mat

    Public camMotionPixels As Single ' distance in pixels that the camera has moved.
    Public camDirection As Single ' camera direction in radians.

    Public callTrace As List(Of String)
    Public algorithm_msMain As New List(Of Single)
    Public algorithmNamesMain As New List(Of String)
    Public algorithm_ms As New List(Of Single)
    Public algorithmNames As New List(Of String)
    Public algorithmTimes As New List(Of DateTime)
    Public algorithmStack As New Stack()

    Public algTasks() As Object
    Public gmat As IMU_GMatrix
    Public lines As Line_Basics
    Public grid As Grid_Basics
    Public palette As Palette_LoadColorMap
    Public Enum algTaskID ' match names in algTasks below...
        gMat = 0
        IMUBasics = 1
        lines = 2
        grid = 3
        colorizer = 4
        motion = 5
        gravityHorizon = 6
        palette = 7
        features = 8
        redCloud = 9
    End Enum

    ' add any task algorithms here
    Public PixelViewer As Pixel_Viewer
    Public rgbFilter As Object
    Public ogl As OpenGL_Basics
    Public feat As Feature_Basics
    Public redC As RedCloud_Basics

    Public centerRect As cv.Rect

    Public pythonPipeIn As NamedPipeServerStream
    Public pythonPipeOut As NamedPipeServerStream
    Public pythonTaskName As String
    Public pythonProcess As Process
    Public pythonReady As Boolean
    Public pythonPipeIndex As Integer

    Public openGL_hwnd As IntPtr
    Public openGLPipe As NamedPipeServerStream

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
    Public firstPass As Boolean

    Public toggleOnOff As Boolean ' toggles on the heartbeat.
    Public paused As Boolean
    Public showAllOptions As Boolean ' show all options when initializing the options for all algorithms.

    Public pcFloor As Single ' y-value for floor...
    Public pcCeiling As Single ' y-value for ceiling...

    Public debugSyncUI As Boolean

    Public disparityAdjustment As Single ' adjusts for resolution and some hidden elements.

    Public gravityVec As New linePoints
    Public horizonVec As New linePoints

    Public IMU_RawAcceleration As cv.Point3f
    Public IMU_Acceleration As cv.Point3f
    Public IMU_AverageAcceleration As cv.Point3f
    Public IMU_RawAngularVelocity As cv.Point3f
    Public IMU_AngularVelocity As cv.Point3f
    Public kalmanIMUacc As cv.Point3f
    Public kalmanIMUvelocity As cv.Point3f
    Public IMU_TimeStamp As Double
    Public IMU_Translation As cv.Point3f
    Public IMU_AngularAcceleration As cv.Point3f
    Public IMU_FrameTime As Double
    Public IMU_AlphaFilter As Single ' high pass and low pass filter of the IMU acceleration data.

    Public accRadians As cv.Point3f  ' rotation angles around x/y/z-axis to align with gravity
    Public theta As cv.Point3f ' velocity-filtered angles around x/y/z-axis to align with gravity
    Public verticalizeAngle As Double

    Public pitch As Single
    Public yaw As Single
    Public roll As Single

    Public useGravityPointcloud As Boolean

    Public recordTimings As Boolean = True

    Public HighlightColor As cv.Scalar ' color to use to highlight objects in an image.

    Public histogramBins As Integer

    Public gOptions As OptionsGlobal
    Public redOptions As OptionsRedCloud

    Public paletteIndex As Integer

    Public mouseClickFlag As Boolean
    Public ClickPoint As cv.Point
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mouseMovePoint As cv.Point ' trace any mouse movements using this.
    Public mouseMovePointUpdated As Boolean

    Public DotSize As Integer
    Public lineWidth As Integer
    Public lineType As cv.LineTypes

    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double

    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public drawRectUpdated As Boolean

    Public pixelViewerRect As cv.Rect
    Public pixelViewTag As Integer

    Public pipeName As String

    Public labels(4 - 1) As String
    Public desc As String
    Public advice As String = ""
    Public displayObjectName As String
    Public displayObject As TaskParent
    Public activeObjects As New List(Of Object)
    Public pixelViewerOn As Boolean

    Public scalarColors(255) As cv.Scalar
    Public oglColors(255) As cv.Scalar
    Public vecColors(255) As cv.Vec3b

    Public topCameraPoint As cv.Point
    Public sideCameraPoint As cv.Point

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

    Public mainFormLocation As cv.Rect
    Public main_hwnd As IntPtr

    Public trueData As New List(Of TrueText)

    Public waitingForInput As Single ' the amount of time waiting for buffers.
    Public inputBufferCopy As Single ' the amount of time copying the buffers.
    Public returnCopyTime As Single ' the amount of time returning buffers to the host.

    Public OpenGLTitle As String
    Public oglRect As cv.Rect
    Public polyCount As Integer

    Public rangesTop() As cv.Rangef
    Public rangesSide() As cv.Rangef
    Public channelsTop() As Integer
    Public channelsSide() As Integer
    Public bins2D() As Integer
    Public frameHistoryCount As Integer ' count of how much history to use for the point cloud.
    Public depthThresholdPercent As Single

    Public projectionThreshold As Integer ' In heatmap views, this defines what is hot in a heatmap.

    Public rc As New rcData
    Public redCells As New List(Of rcData)
    Public redMap As cv.Mat

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
        Dim color As cv.Mat
        Dim leftView As cv.Mat
        Dim rightView As cv.Mat
        Dim pointCloud As cv.Mat
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
        Public externalPythonInvocation As Boolean ' Opencv was initialized remotely...
        Public showConsoleLog As Boolean
        Public testAllRunning As Boolean
        Public RotationMatrix() As Single
        Public RotationVector As cv.Point3f

        Public mainFormLocation As cv.Rect
        Public main_hwnd As IntPtr

        Public fpsRate As Integer
        Public workingRes As cv.Size
        Public captureRes As cv.Size ' DisparityIn-verted_Basics needs the full resolution to compute disparity.
        Public displayRes As cv.Size

        Public algName As String

        Public cameraInfo As cameraInfo
    End Structure
    Private Sub buildColors()
        Dim rand = New Random(1)
        Dim bgr(3) As Byte
        For i = 0 To task.vecColors.Length - 1
            rand.NextBytes(bgr)
            task.vecColors(i) = New cv.Vec3b(bgr(0), bgr(1), bgr(2))
            task.scalarColors(i) = New cv.Scalar(task.vecColors(i)(0),
                                                  task.vecColors(i)(1),
                                                  task.vecColors(i)(2))
            task.oglColors(i) = New cv.Scalar(task.vecColors(i)(0) / 255,
                                               task.vecColors(i)(1) / 255,
                                               task.vecColors(i)(2) / 255)
        Next
    End Sub
#End Region
    Private Function finddisplayObject(lookupName As String) As TaskParent
        For Each obj In task.activeObjects
            If obj.traceName = lookupName Then Return obj
        Next
        Return Nothing
    End Function
    Private Sub postProcess(src As cv.Mat)
        Try
            If task.PixelViewer IsNot Nothing Then
                If task.pixelViewerOn Then
                    task.PixelViewer.viewerForm.Visible = True
                    task.PixelViewer.viewerForm.Show()
                    task.PixelViewer.Run(src)
                Else
                    task.PixelViewer.viewerForm.Visible = False
                End If
            End If

            ' mark each task as inactive so we can find which are really working.
            If task.heartBeat Then
                For Each obj In task.activeObjects
                    obj.activeTask = False
                Next
            End If

            Dim lookupName = task.displayObjectName
            task.displayObject = finddisplayObject(lookupName)

            If task.gifCreator IsNot Nothing Then task.gifCreator.createNextGifImage()

            ' MSER mistakenly can have 1 cell - just ignore it.
            If task.redCells.Count > 1 Then setSelectedCell()

            If task.redOptions.IdentifyCells.Checked Then
                ' cannot use rc as it means task.rc here!  Be careful...
                For Each rcX In task.redCells
                    Dim str As New TrueText(CStr(rcX.index), rcX.maxDist, 2)
                    trueData.Add(str)
                    If rcX.index = 20 Then Exit For
                Next

                task.color.Rectangle(task.rc.rect, cv.Scalar.Yellow, task.lineWidth)
                task.color(task.rc.rect).SetTo(cv.Scalar.White, rc.mask)
            End If

            task.optionsChanged = False
            TaskTimer.Enabled = False
            task.frameCount += 1
        Catch ex As Exception
            Debug.WriteLine("Active Algorithm exception occurred: " + ex.Message)
        End Try
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
            Dim wRect = New cv.Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top)
            SaveSetting("Opencv", "OpenGLtaskX", "OpenGLtaskX", wRect.X)
            SaveSetting("Opencv", "OpenGLtaskY", "OpenGLtaskY", wRect.Y)
            SaveSetting("Opencv", "OpenGLtaskWidth", "OpenGLtaskWidth", wRect.Width)
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
        gridRects = New List(Of cv.Rect)
        firstPass = True
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
        workingRes = parms.workingRes
        task.optionsChanged = True

        dst0 = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, New cv.Scalar)
        dst1 = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, New cv.Scalar)
        dst2 = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, New cv.Scalar)
        dst3 = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, New cv.Scalar)

        OpenGL_Left = CInt(GetSetting("Opencv", "OpenGLtaskX", "OpenGLtaskX", task.mainFormLocation.X))
        OpenGL_Top = CInt(GetSetting("Opencv", "OpenGLtaskY", "OpenGLtaskY", task.mainFormLocation.Y))

        buildColors()
        pythonTaskName = HomeDir + "Python\" + algName

        allOptions = New OptionsContainer
        allOptions.Show()

        gOptions = New OptionsGlobal
        redOptions = New OptionsRedCloud
        task.redMap = New cv.Mat(New cv.Size(task.dst2.Width, task.dst2.Height), cv.MatType.CV_8U, cv.Scalar.All(0))

        callTrace = New List(Of String)

        ' add any algorithm tasks to this list.
        algTasks = {New IMU_GMatrix, New IMU_Basics, New Line_Basics, New Grid_Basics, New Depth_Palette,
                    New Motion_Basics, New Gravity_Horizon, New Palette_LoadColorMap, New Feature_Basics,
                    New RedCloud_Basics}

        gmat = algTasks(algTaskID.gMat)
        lines = algTasks(algTaskID.lines)
        grid = algTasks(algTaskID.grid)
        palette = algTasks(algTaskID.palette)
        redC = algTasks(algTaskID.redCloud)

        If task.algName.StartsWith("OpenGL_") Then ogl = New OpenGL_Basics
        If task.algName.StartsWith("Model_") Then ogl = New OpenGL_Basics

        ' all the algorithms in the list are task algorithms that are children of the task.algname.
        For i = 1 To callTrace.Count - 1
            callTrace(i) = task.algName + "\" + callTrace(i)
        Next

        updateSettings()
        task.redOptions.Show()
        task.gOptions.Show()
        centerRect = New cv.Rect(dst2.Width / 4, dst2.Height / 4, dst2.Width / 2, dst1.Height / 2)

        If task.advice = "" Then
            task.advice = "No advice for " + algName + " yet." + vbCrLf +
                               "Please use 'UpdateAdvice(<your advice>)' in the constructor)."
        End If

        fpList.Clear()
        fpOutline = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        fpMap = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)

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
    Public Sub TrueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New TrueText(text, pt, picTag)
        task.trueData.Add(str)
    End Sub
    Public Sub setSelectedCell(ByRef redCells As List(Of rcData), ByRef cellMap As cv.Mat)
        Static ptNew As New cv.Point
        If redCells.Count = 0 Then Exit Sub
        If task.ClickPoint = ptNew And redCells.Count > 1 Then task.ClickPoint = redCells(1).maxDist
        Dim index = cellMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X)
        task.rc = redCells(index)
        If index > 0 And index < task.redCells.Count Then
            ' task.ClickPoint = redCells(index).maxDist
            task.rc = redCells(index)
        End If
    End Sub
    Public Sub setSelectedCell()
        If task.redCells.Count = 0 Then Exit Sub
        If task.ClickPoint = newPoint And task.redCells.Count > 1 Then
            task.ClickPoint = task.redCells(1).maxDist
        End If
        Dim index = task.redMap.Get(Of Byte)(task.ClickPoint.Y, task.ClickPoint.X)
        If index > 0 And index < task.redCells.Count Then
            ' task.ClickPoint = task.redCells(index).maxDist
            task.rc = task.redCells(index)
        Else
            ' the 0th cell is always the upper left corner with just 1 pixel.
            If task.redCells.Count > 1 Then task.rc = task.redCells(1)
        End If
    End Sub
    Public Sub DrawLine(dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f, color As cv.Scalar)
        dst.Line(p1, p2, color, task.lineWidth, task.lineType)
    End Sub
    Public Function GetMinMax(mat As cv.Mat, Optional mask As cv.Mat = Nothing) As mmData
        Dim mm As mmData
        If mask Is Nothing Then
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc)
        Else
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, mask)
        End If
        Return mm
    End Function
    Public Function RunAlgorithm() As Boolean
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

            If task.heartBeatLT Then
                algorithm_ms(0) = waitingForInput
                algorithm_ms(1) = inputBufferCopy
                algorithm_ms(2) = returnCopyTime
            Else
                algorithm_ms(0) += waitingForInput
                algorithm_ms(1) += inputBufferCopy
                algorithm_ms(2) += returnCopyTime
            End If
            algorithmTimes(3) = Now  ' starting the main algorithm
        End If
        If task.useRecordedData Then recordedData.Run(task.color.Clone)

        task.redOptions.Sync()

        Dim src = task.color
        If src.Size <> New cv.Size(task.dst2.Cols, task.dst2.Rows) Then dst2 = dst2.Resize(src.Size)
        If src.Size <> New cv.Size(task.dst3.Cols, task.dst3.Rows) Then dst3 = dst3.Resize(src.Size)
        task.bins2D = {task.dst2.Height, task.dst2.Width}

        ' If the WorkingRes changes, the previous generation of images needs to be reset.
        If task.pointCloud.Size <> New cv.Size(cols, rows) Or task.color.Size <> task.dst2.Size Then
            task.pointCloud = New cv.Mat(rows, cols, cv.MatType.CV_32FC3, cv.Scalar.All(0))
            task.noDepthMask = New cv.Mat(rows, cols, cv.MatType.CV_8U, cv.Scalar.All(0))
            task.depthMask = New cv.Mat(rows, cols, cv.MatType.CV_8U, cv.Scalar.All(0))
        End If

        ' run any universal algorithms here
        task.IMU_RawAcceleration = task.IMU_Acceleration
        task.IMU_RawAngularVelocity = task.IMU_AngularVelocity
        task.IMU_AlphaFilter = 0.5 '  task.gOptions.imu_Alpha

        grid.runAlg(task.color)
        algTasks(algTaskID.IMUBasics).runAlg(src)
        gmat.runAlg(src)

        If task.gOptions.RGBFilterActive.Checked Then
            Static saveFilterName As String
            Dim filterName = task.gOptions.RGBFilterList.Text
            If saveFilterName <> filterName Then
                saveFilterName = filterName
                Select Case filterName
                    Case "Blur_Basics"
                        rgbFilter = New Blur_Basics
                    Case "Brightness_Basics"
                        rgbFilter = New Brightness_Basics
                    Case "Contrast_Basics"
                        rgbFilter = New Contrast_Basics
                    Case "Dilate_Basics"
                        rgbFilter = New Dilate_Basics
                    Case "Erode_Basics"
                        rgbFilter = New Erode_Basics
                    Case "Filter_Laplacian"
                        rgbFilter = New Filter_Laplacian
                    Case "PhotoShop_SharpenDetail"
                        rgbFilter = New PhotoShop_SharpenDetail
                    Case "PhotoShop_WhiteBalance"
                        rgbFilter = New PhotoShop_WhiteBalance
                End Select
                For i = 0 To callTrace.Count - 1
                    If callTrace(i).StartsWith(task.algName) = False Then
                        callTrace(i) = task.algName + "\" + callTrace(i)
                    End If
                Next
            End If
            rgbFilter.runAlg(src)
            src = rgbFilter.dst2
        End If

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
                    task.pointCloud = New cv.Mat(src.Size, cv.MatType.CV_32FC3, 0)
                End If

                '******* this is the gravity rotation *******
                task.pointCloud = (task.pointCloud.Reshape(1, src.Rows * src.Cols) * task.gMatrix).
                                       ToMat.Reshape(3, src.Rows)
            End If

            If task.pcSplit Is Nothing Then task.pcSplit = task.pointCloud.Split

            task.gOptions.unFiltered.Checked = True
        End If

        algTasks(algTaskID.motion).Run(src)
        task.motionMask = algTasks(algTaskID.motion).motionMask

        If task.gOptions.UseMotionColor.Checked Then
            task.color = algTasks(algTaskID.motion).color.Clone
            Dim rectList As List(Of cv.Rect) = algTasks(algTaskID.motion).measure.motionRects
            task.motionRects = New List(Of cv.Rect)(rectList)
        End If

        If task.gOptions.UseMotionDepth.Checked Then
            task.pointCloud = algTasks(algTaskID.motion).pointcloud.Clone
        End If

        task.pcSplit = task.pointCloud.Split

        If task.optionsChanged Then task.maxDepthMask.SetTo(0)
        task.pcSplit(2) = task.pcSplit(2)

        If task.gOptions.TruncateDepth.Checked Then
            task.pcSplit(2) = task.pcSplit(2).Threshold(task.MaxZmeters, task.MaxZmeters,
                                                        cv.ThresholdTypes.Trunc)
            cv.Cv2.Merge(task.pcSplit, task.pointCloud)
        End If

        ' The stereolabs camera has some weird -inf and inf values in the Y-plane.  
        If task.cameraName = "StereoLabs ZED 2/2i" Then
            Dim mask = task.pcSplit(1).InRange(-100, 100)
            task.pcSplit(1).SetTo(0, Not mask)
        End If

        task.depthMask = task.pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary)
        task.depthMask = task.depthMask.ConvertScaleAbs()

        task.noDepthMask = Not task.depthMask

        If task.xRange <> task.xRangeDefault Or task.yRange <> task.yRangeDefault Then
            Dim xRatio = task.xRangeDefault / task.xRange
            Dim yRatio = task.yRangeDefault / task.yRange
            task.pcSplit(0) *= xRatio
            task.pcSplit(1) *= yRatio

            cv.Cv2.Merge(task.pcSplit, task.pointCloud)
        End If

        lines.runAlg(src)
        Dim lineList As List(Of linePoints) = lines.lpList

        ' the gravity transformation apparently can introduce some NaNs - just for StereoLabs tho.
        If task.cameraName.StartsWith("StereoLabs") Then cv.Cv2.PatchNaNs(task.pcSplit(2))

        algTasks(algTaskID.colorizer).Run(src)
        task.depthRGB = algTasks(algTaskID.colorizer).dst2

        TaskTimer.Enabled = True

        If task.pixelViewerOn And PixelViewer Is Nothing Then
            PixelViewer = New Pixel_Viewer
            For i = 1 To callTrace.Count - 1
                If callTrace(i).Contains("Pixel_Viewer") Then
                    callTrace(i) = task.algName + "\" + callTrace(i)
                    Exit For
                End If
            Next
        End If

        If task.gOptions.CreateGif.Checked Then
            If task.gifCreator Is Nothing Then task.gifCreator = New Gif_OpenCVB
            gifCreator.runAlg(src)
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
        For Each obj In task.algTasks
            If obj.traceName = "Feature_Basics" Then Continue For ' not expected to be active
            If obj.traceName = "RedCloud_Basics" Then Continue For ' not expected to be active
            obj.activeTask = True
        Next
        algTasks(algTaskID.palette).activeTask = False ' usually it is inactive

        algTasks(algTaskID.gravityHorizon).runAlg(src)

        Dim saveOptionsChanged = task.optionsChanged
        If task.paused = False Then
            MainUI_Algorithm.processFrame(src.Clone) ' <<<<<<<< This is where the VB algorithm runs...
            task.firstPass = False
            task.heartBeatLT = False

            postProcess(src)

            If task.gOptions.displayDst0.Checked Then
                dst0 = Check8uC3(task.displayObject.dst0)
            Else
                dst0 = task.color
            End If
            If task.gOptions.displayDst1.Checked Then
                dst1 = Check8uC3(task.displayObject.dst1)
            Else
                dst1 = task.depthRGB
            End If

            dst2 = Check8uC3(displayObject.dst2)
            dst3 = Check8uC3(displayObject.dst3)

            ' make sure that any outputs from the algorithm are the right size.nearest
            If dst0.Size <> task.workingRes And dst0.Width > 0 Then
                dst0 = dst0.Resize(task.workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            End If
            If dst1.Size <> task.workingRes And dst1.Width > 0 Then
                dst1 = dst1.Resize(task.workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            End If
            If dst2.Size <> task.workingRes And dst2.Width > 0 Then
                dst2 = dst2.Resize(task.workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            End If
            If dst3.Size <> task.workingRes And dst3.Width > 0 Then
                dst3 = dst3.Resize(task.workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            End If

            If task.gOptions.ShowGrid.Checked Then dst2.SetTo(cv.Scalar.White, task.gridMask)

            If task.redOptions.DisplayCellStats.Checked Then
                If task.redC IsNot Nothing Then
                    For Each tt In task.redC.trueData
                        trueData.Add(tt)
                    Next
                End If
            End If

            If task.gOptions.showMotionMask.Checked Then
                For Each roi In task.motionRects
                    dst0.Rectangle(roi, cv.Scalar.White, task.lineWidth)
                Next
            End If

            If task.gOptions.CrossHairs.Checked Then
                If task.paused = False Then
                    DrawLine(dst0, task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.White)
                    DrawLine(dst0, task.gravityVec.p1, task.gravityVec.p2, cv.Scalar.White)
                End If
            End If

            If task.displayObject.activeTask = False And task.heartBeat = False Then
                Dim str As New TrueText("This task is not active at this time.",
                                        New cv.Point(dst2.Width / 2, 0), 3)
                task.displayObject.trueData.Add(str)
            End If

            task.trueData = New List(Of TrueText)(task.displayObject.trueData)
            task.displayObject.trueData.Clear()
            task.labels = task.displayObject.labels
        End If
        Return saveOptionsChanged
    End Function
End Class
