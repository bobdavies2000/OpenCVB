Imports cv = OpenCvSharp
Imports System.Windows.Forms
Imports System.IO.Pipes
Imports System.Drawing
Imports System.IO
Imports System.Runtime.InteropServices

#Region "taskProcess"
<StructLayout(LayoutKind.Sequential)>
Public Class VBtask : Implements IDisposable
    ' add any task algorithms here.
    Public redC As RedColor_Basics
    Public gmat As IMU_GMatrix
    Public lineRGB As LineRGB_Basics
    Public contours As Contour_Basics_List
    Public edges As EdgeLine_Basics
    Public grid As Grid_Basics
    Public bricks As Brick_Basics
    Public palette As Palette_LoadColorMap
    Public PixelViewer As Pixel_Viewer
    Public rgbFilter As Filter_Basics
    Public ogl As OpenGL_Basics
    Public gravityBasics As Gravity_Basics
    Public imuBasics As IMU_Basics
    Public motionBasics As Motion_Basics
    Public colorizer As DepthColorizer_Basics

    Public brickRunFlag As Boolean

    Public rcPixelThreshold As Integer ' if pixel count < this, then make the color gray...
    Public rcOtherPixelColor = cv.Scalar.Yellow ' color for the 'other' class of redcloud cells.

    Public fpList As New List(Of fpData)
    Public regionList As New List(Of rcData)
    Public featList As New List(Of List(Of Integer))
    Public fLess As New List(Of List(Of Integer))
    Public logicalLines As New List(Of lpData)

    Public fpMap As New cv.Mat ' feature map

    Public brickD As brickData ' the currently selected brick
    Public rcD As New rcData ' the currently selected red Cell
    Public lpD As New lpData ' the currently selected line pair
    Public fpD As New fpData ' the currently selected feature point.
    Public contourD As New contourData ' the currently selected contour

    Public cellSize As Integer
    Public cellsPerCol As Integer
    Public cellsPerRow As Integer
    Public gridRects As List(Of cv.Rect)
    Public gridMask As New cv.Mat
    Public gridNeighbors As New List(Of List(Of Integer))
    Public gridNabeRects As New List(Of cv.Rect) ' The surrounding rect for every gridRect
    Public gridROIclicked As Integer
    Public depthDiffMeters As Single ' bricks > than this value are depth edges - in meters
    Public rgbLeftAligned As Boolean

    Public fpMotion As cv.Point2f

    Public topFeatures As New List(Of cv.Point2f)
    Public features As New List(Of cv.Point2f)
    Public fpFromGridCell As New List(Of Integer)
    Public fpFromGridCellLast As New List(Of Integer)
    Public fpLastList As New List(Of fpData)
    Public featurePoints As New List(Of cv.Point)

    Public featureMask As New cv.Mat
    Public fLessMask As New cv.Mat
    Public featureRects As New List(Of cv.Rect)
    Public fLessRects As New List(Of cv.Rect)
    Public flessBoundary As New cv.Mat
    Public lowResColor As New cv.Mat
    Public lowResDepth As New cv.Mat

    Public motionMask As New cv.Mat
    Public fullImageStable As Boolean ' True if the current image has no changes from the previous.
    Public motionPercent As Single
    Public motionLabel As String = " "

    Public optionsChanged As Boolean = True ' global or local options changed.
    Public rows As Integer
    Public cols As Integer
    Public workingRes As cv.Size
    Public TaskTimer As New System.Timers.Timer(1000)

    ' if true, algorithm prep means algorithm tasks will run.  If false, they have already been run...
    Public algorithmPrep As Boolean = True

    Public dst0 As New cv.Mat
    Public dst1 As New cv.Mat
    Public dst2 As New cv.Mat
    Public dst3 As New cv.Mat

    Public MainUI_Algorithm As Object
    Public myStopWatch As Stopwatch
    Public displayRes As cv.Size

    Public cvFontSize As Single = 0.8
    Public cvFontThickness As Integer = 1

    Public color As New cv.Mat
    Public gray As New cv.Mat
    Public grayStable As New cv.Mat
    Public leftView As New cv.Mat
    Public rightView As New cv.Mat
    Public leftRightMode As Boolean ' dst0 and dst1 are the left and right images.
    Public pointCloud As New cv.Mat
    Public splitOriginalCloud() As cv.Mat
    Public gravityCloud As New cv.Mat
    Public pcSplit() As cv.Mat
    Public gravitySplit() As cv.Mat

    ' transformation matrix to convert point cloud to be vertical according to gravity.
    Public gMatrix As New cv.Mat
    Public IMU_Rotation As System.Numerics.Quaternion
    Public noDepthMask As New cv.Mat
    Public depthMask As New cv.Mat
    Public depthMaskRaw As New cv.Mat
    Public maxDepthMask As New cv.Mat
    Public depthRGB As New cv.Mat
    Public srcThread As New cv.Mat

    Public camMotionPixels As Single ' distance in pixels that the camera has moved.
    Public camDirection As Single ' camera direction in radians.

    Public callTrace As List(Of String)
    Public algorithm_msMain As New List(Of Single)
    Public algorithmNamesMain As New List(Of String)
    Public algorithm_ms As New List(Of Single)
    Public algorithmNames As New List(Of String)
    Public algorithmTimes As New List(Of DateTime)
    Public algorithmStack As New Stack()

    Public paletteRandom As Palette_RandomColors
    Public kalman As Kalman_Basics

    ' end of task algorithms

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
    Public quarter(4 - 1) As Boolean
    Public midHeartBeat As Boolean
    Public almostHeartBeat As Boolean
    Public msWatch As Integer
    Public msLast As Integer
    Public firstPass As Boolean
    Public historyCount As Integer ' task.goptions.framehistory.value

    Public toggleOn As Boolean ' toggles on the heartbeat.
    Public paused As Boolean
    Public showAllOptions As Boolean ' show all options when initializing the options for all algorithms.

    Public pcFloor As Single ' y-value for floor...
    Public pcCeiling As Single ' y-value for ceiling...

    Public debugSyncUI As Boolean

    Public disparityAdjustment As Single ' adjusts for resolution and some hidden elements.

    Public gravityVec As New lpData
    Public horizonVec As New lpData

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

    Public highlight As cv.Scalar ' color to use to highlight objects in an image.

    Public histogramBins As Integer

    Public gOptions As OptionsGlobal
    Public redOptions As OptionsRedColor
    Public featureOptions As OptionsFeatures
    Public treeView As TreeviewForm

    ' RedCloud variables
    Public channelCount As Integer = 2
    Public channelIndex As Integer = 0
    Public channels() As Integer = {0, 1}
    Public histBinList() As Integer
    Public ranges() As cv.Rangef
    Public rangesBGR() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 256), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
    Public rangesHSV() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 180), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
    Public rangesCloud() As cv.Rangef

    Public paletteIndex As Integer

    Public mouseClickFlag As Boolean
    Public activateTaskForms As Boolean
    Public ClickPoint As cv.Point
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mouseMovePoint As cv.Point ' trace any mouse movements using this.
    Public mouseMovePointUpdated As Boolean

    Public DotSize As Integer
    Public lineWidth As Integer
    Public lineType As cv.LineTypes

    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double

    Public centerRect As cv.Rect ' image center - potential use for motion.

    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public drawRectUpdated As Boolean

    Public pixelViewerRect As cv.Rect
    Public pixelViewTag As Integer

    Public pipeName As String

    Public labels(4 - 1) As String
    Public desc As String
    Public displayObjectName As String
    Public activeObjects As New List(Of Object)
    Public pixelViewerOn As Boolean

    Public scalarColors(255) As cv.Scalar
    Public vecColors(255) As cv.Vec3b
    Public depthColorMap As cv.Mat
    Public colorMap As cv.Mat
    Public colorMapNoZero As cv.Mat
    Public correlationColorMap As cv.Mat

    Public topCameraPoint As cv.Point
    Public sideCameraPoint As cv.Point

    Public hFov As Single
    Public vFov As Single

    Public algName As String
    Public cameraName As String
    Public calibData As cameraInfo
    Public HomeDir As String
    Public fpsAlgorithm As Integer
    Public fpsCamera As Integer
    Public densityMetric As Integer ' how dense is the pointcloud in z - heuristic.
    Public FASTthreshold As Integer

    Public minDistance As Integer ' minimum distance between features
    Public FeatureSampleSize As Integer ' how many features do you want...
    Public featureSource As Integer ' which Feature_Basics method...
    Public fCorrThreshold As Single ' feature correlation threshold
    Public colorDiffThreshold As Integer ' this is vital to motion detection - lower to be more sensitive, higher for less.
    Public selectedFeature As Integer ' index of the feature to display.
    Public edgeMethod As String
    Public verticalLines As Boolean

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

    Public useXYRange As Boolean ' OpenGL applications don't need to adjust the ranges.
    Public xRange As Single
    Public yRange As Single
    Public xRangeDefault As Single
    Public yRangeDefault As Single
    Public MaxZmeters As Single
    Public metersPerPixel As Single
    Public OpenGL_Left As Integer
    Public OpenGL_Top As Integer
    Public displayDst1 As Boolean
    Public depthAndCorrelationText As String
    Public closeRequest As Boolean
    Public Structure inBuffer
        Dim color As cv.Mat
        Dim leftView As cv.Mat
        Dim rightView As cv.Mat
        Dim pointCloud As cv.Mat
    End Structure
    Public Structure intrinsicData
        Public ppx As Single
        Public ppy As Single
        Public fx As Single
        Public fy As Single
    End Structure
    Public Structure cameraInfo
        Public baseline As Single ' this is the baseline of the left to right cameras

        Public rgbIntrinsics As intrinsicData
        Public leftIntrinsics As intrinsicData
        Public rightIntrinsics As intrinsicData

        Public ColorToLeft_translation() As Single
        Public ColorToLeft_rotation() As Single

        Public LtoR_translation() As Single
        Public LtoR_rotation() As Single

        Public v_fov As Single ' vertical field of view in degrees.
        Public h_fov As Single ' horizontal field of view in degrees.
        Public d_fov As Single ' diagonal field of view in degrees.
    End Structure

    Public Structure algParms
#If AZURE_SUPPORT Then
        "Azure Kinect 4K",
#End If
        Public Shared cameraNames As New List(Of String)({"StereoLabs ZED 2/2i",
                                                          "Orbbec Gemini 335L",
                                                          "Orbbec Gemini 336L",
                                                          "Oak-D camera",
                                                          "Intel(R) RealSense(TM) Depth Camera 435i",
                                                          "Intel(R) RealSense(TM) Depth Camera 455",
                                                          "MYNT-EYE-D1000",
                                                          "Orbbec Gemini 335"})
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
        Public fpsHostCamera As Integer
        Public workingRes As cv.Size
        Public captureRes As cv.Size ' DisparityIn-verted_Basics needs the full resolution to compute disparity.
        Public displayRes As cv.Size

        Public algName As String

        Public calibData As cameraInfo
    End Structure

#End Region
    Private Function findDisplayObject(lookupName As String) As TaskParent
        Dim saveObject As Object
        For Each obj In activeObjects
            If obj.tracename = lookupName Then
                saveObject = obj
                If obj.traceName <> saveObject.labels(2) Then
                    Return saveObject
                End If
            End If
        Next
        Return saveObject
    End Function
    Private Sub postProcess(src As cv.Mat)
        Try
            If PixelViewer IsNot Nothing Then
                If pixelViewerOn Then
                    PixelViewer.viewerForm.Visible = True
                    PixelViewer.viewerForm.Show()
                    PixelViewer.Run(src)
                Else
                    PixelViewer.viewerForm.Visible = False
                End If
            End If

            Dim displayObject = findDisplayObject(displayObjectName)
            If gifCreator IsNot Nothing Then gifCreator.createNextGifImage()

            ' MSER mistakenly can have 1 cell - just ignore it.
            setSelectedCell()

            If optionsChanged = True And treeView IsNot Nothing Then ' treeview is not active during 'TestAll'.
                treeView.optionsChanged = True
                Dim sender As Object, e As EventArgs
                treeView.Timer2_Tick(sender, e)
                treeView.optionsChanged = False
            End If
            optionsChanged = False
            TaskTimer.Enabled = False
            frameCount += 1
        Catch ex As Exception
            Debug.WriteLine("Active Algorithm exception occurred: " + ex.Message)
        End Try
    End Sub
    Private Sub VBTaskTimerPop(sender As Object, e As EventArgs)
        Static WarningIssued As Boolean = False
        If frameCount > 0 And WarningIssued = False Then
            WarningIssued = True
            Debug.WriteLine("Warning: " + algName + " has not completed work on a frame in a second.")
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
        displayObjectName = algName
        cameraName = parms.cameraName
        testAllRunning = parms.testAllRunning
        showConsoleLog = parms.showConsoleLog
        fpsAlgorithm = parms.fpsRate
        fpsCamera = parms.fpsHostCamera
        calibData = parms.calibData
        HomeDir = parms.HomeDir
        main_hwnd = parms.main_hwnd
        useRecordedData = parms.useRecordedData
        externalPythonInvocation = parms.externalPythonInvocation

        ' set options for specific cameras here.
        Select Case task.cameraName
            Case "StereoLabs ZED 2/2i"
            Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
            Case "Intel(R) RealSense(TM) Depth Camera 435i"
            Case "Intel(R) RealSense(TM) Depth Camera 455"
            Case "Oak-D camera"
            Case "MYNT-EYE-D1000"
#If AZURE_SUPPORT Then
            Case "Azure Kinect 4K"
#End If
        End Select

        mainFormLocation = parms.mainFormLocation
        displayRes = parms.displayRes
        rows = parms.workingRes.Height
        cols = parms.workingRes.Width
        workingRes = parms.workingRes
        optionsChanged = True

        dst0 = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, New cv.Scalar)
        dst1 = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, New cv.Scalar)
        dst2 = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, New cv.Scalar)
        dst3 = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, New cv.Scalar)

        OpenGL_Left = CInt(GetSetting("Opencv", "OpenGLtaskX", "OpenGLtaskX", mainFormLocation.X))
        OpenGL_Top = CInt(GetSetting("Opencv", "OpenGLtaskY", "OpenGLtaskY", mainFormLocation.Y))

        pythonTaskName = HomeDir + "Python\" + algName

        allOptions = New OptionsContainer
        allOptions.Show()

        gOptions = New OptionsGlobal
        redOptions = New OptionsRedColor
        featureOptions = New OptionsFeatures
        treeView = New TreeviewForm

        callTrace = New List(Of String)
        task.pointCloud = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)

        colorizer = New DepthColorizer_Basics
        gmat = New IMU_GMatrix
        gravityBasics = New Gravity_Basics
        imuBasics = New IMU_Basics
        motionBasics = New Motion_Basics
        grid = New Grid_Basics
        lineRGB = New LineRGB_Basics
        edges = New EdgeLine_Basics
        contours = New Contour_Basics_List
        rgbFilter = New Filter_Basics

        If algName.StartsWith("OpenGL_") Then ogl = New OpenGL_Basics
        If algName.StartsWith("Model_") Then ogl = New OpenGL_Basics

        ' all the algorithms in the list are task algorithms that are children of the algname.
        For i = 1 To callTrace.Count - 1
            callTrace(i) = algName + "\" + callTrace(i)
        Next

        updateSettings()
        featureOptions.Show() ' behind redOptions
        redOptions.Show()     ' behind gOptions
        gOptions.Show()       ' In front of both...

        treeView.Show()
        centerRect = New cv.Rect(dst2.Width / 4, dst2.Height / 4, dst2.Width / 2, dst1.Height / 2)

        fpList.Clear()

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

        ' NOTE: I can't find the VFOV for the Oak-D or Oak-D Lite cameras.
        ' The 62 is based on Pythagorean theorem and knowing the 71.8 HFOV and the 81.3 DFOV.
        If parms.calibData.v_fov <> 0 Then vFOVangles(parms.cameraIndex) = parms.calibData.v_fov
        If parms.calibData.h_fov <> 0 Then hFOVangles(parms.cameraIndex) = parms.calibData.h_fov

        vFov = vFOVangles(parms.cameraIndex)  ' these are default values in case the calibration data is unavailable
        hFov = hFOVangles(parms.cameraIndex)

        myStopWatch = Stopwatch.StartNew()
        optionsChanged = True
    End Sub
    Public Sub TrueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New TrueText(text, pt, picTag)
        trueData.Add(str)
    End Sub
    Public Sub setSelectedCell()
        If task.redC Is Nothing Then Exit Sub
        If task.redC.rcList.Count = 0 Then Exit Sub
        If ClickPoint = newPoint And task.redC.rcList.Count > 1 Then
            ClickPoint = task.redC.rcList(1).maxDist
        End If
        Dim index = task.redC.rcMap.Get(Of Byte)(ClickPoint.Y, ClickPoint.X)
        If index = 0 Then Exit Sub
        If index > 0 And index < task.redC.rcList.Count Then
            ' ClickPoint = rcList(index).maxDist
            task.rcD = task.redC.rcList(index)
            task.color(task.rcD.rect).SetTo(cv.Scalar.White, task.rcD.mask)
        Else
            ' the 0th cell is always the upper left corner with just 1 pixel.
            If task.redC.rcList.Count > 1 Then task.rcD = task.redC.rcList(1)
        End If
    End Sub
    Public Sub DrawLine(dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f, color As cv.Scalar)
        dst.Line(p1, p2, color, lineWidth, lineType)
    End Sub
    Public Function RunAlgorithm() As Boolean
        If allOptions.titlesAdded Then
            allOptions.titlesAdded = False
            allOptions.layoutOptions(normalRequest:=True)
        End If

        Application.DoEvents() ' this lets the options container update.
        updateSettings()

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

            algorithmNames.Add(algName)
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
        If useRecordedData Then recordedData.Run(task.color.Clone)

        redOptions.Sync() ' update the task with redCloud variables

        Dim src = task.color

        If src.Size <> New cv.Size(dst2.Cols, dst2.Rows) Then dst2 = dst2.Resize(src.Size)
        If src.Size <> New cv.Size(dst3.Cols, dst3.Rows) Then dst3 = dst3.Resize(src.Size)
        bins2D = {dst2.Height, dst2.Width}

        ' If the WorkingRes changes, the previous generation of images needs to be reset.
        If pointCloud.Size <> New cv.Size(cols, rows) Or task.color.Size <> dst2.Size Then
            pointCloud = New cv.Mat(rows, cols, cv.MatType.CV_32FC3, cv.Scalar.All(0))
            noDepthMask = New cv.Mat(rows, cols, cv.MatType.CV_8U, cv.Scalar.All(0))
            depthMask = New cv.Mat(rows, cols, cv.MatType.CV_8U, cv.Scalar.All(0))
        End If

        ' run any universal algorithms here
        IMU_RawAcceleration = IMU_Acceleration
        IMU_RawAngularVelocity = IMU_AngularVelocity
        IMU_AlphaFilter = 0.5 '  gOptions.imu_Alpha

        grid.Run(task.color)
        imuBasics.Run(emptyMat)
        gmat.Run(emptyMat)

        If task.cameraName.StartsWith("Intel(R) RealSense(TM) Depth Camera") = False Then
            leftView = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If

        If gOptions.CreateGif.Checked Then
            heartBeat = False
            optionsChanged = False
        Else
            heartBeat = heartBeat Or debugSyncUI Or optionsChanged Or mouseClickFlag
        End If

        If paused = False Then
            frameHistoryCount = gOptions.FrameHistory.Value

            If pointCloud.Size <> src.Size Then
                pointCloud = New cv.Mat(src.Size, cv.MatType.CV_32FC3, 0)
                gravityCloud = New cv.Mat(src.Size, cv.MatType.CV_32FC3, 0)
            End If



            '******* this is the gravity rotation *******
            gravityCloud = (pointCloud.Reshape(1, src.Rows * src.Cols) * gMatrix).ToMat.Reshape(3, src.Rows)



            splitOriginalCloud = gravityCloud.Split
            If useGravityPointcloud Then pointCloud = gravityCloud

            If pcSplit Is Nothing Then pcSplit = pointCloud.Split

            gOptions.unFiltered.Checked = True
        End If

        pcSplit = pointCloud.Split
        If useGravityPointcloud Then gravitySplit = pcSplit Else gravitySplit = gravityCloud.Split()

        If optionsChanged Then maxDepthMask = New cv.Mat(pcSplit(2).Size, cv.MatType.CV_8U, 0)
        If gOptions.TruncateDepth.Checked Then
            pcSplit(2) = pcSplit(2).Threshold(MaxZmeters, MaxZmeters, cv.ThresholdTypes.Trunc)
            task.maxDepthMask = task.pcSplit(2).InRange(task.MaxZmeters, task.MaxZmeters).ConvertScaleAbs()
            cv.Cv2.Merge(pcSplit, pointCloud)
        End If

        ' The stereolabs camera has some weird -inf and inf values in the Y-plane - with and without gravity transform.  
        If cameraName = "StereoLabs ZED 2/2i" Then
            Dim mask = pcSplit(0).InRange(-100, 100)
            pcSplit(0).SetTo(0, Not mask)
            mask = pcSplit(1).InRange(-100, 100)
            pcSplit(1).SetTo(0, Not mask)
            mask = pcSplit(2).InRange(-100, 100)
            pcSplit(2).SetTo(0, Not mask)
        End If

        ' the gravity transformation apparently can introduce some NaNs - just for StereoLabs tho.
        If cameraName.StartsWith("StereoLabs") Then
            cv.Cv2.PatchNaNs(pcSplit(0))
            cv.Cv2.PatchNaNs(pcSplit(1))
            cv.Cv2.PatchNaNs(pcSplit(2))
        End If

        depthMask = pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
        noDepthMask = Not depthMask

        If xRange <> xRangeDefault Or yRange <> yRangeDefault Then
            Dim xRatio = xRangeDefault / xRange
            Dim yRatio = yRangeDefault / yRange
            pcSplit(0) *= xRatio
            pcSplit(1) *= yRatio

            cv.Cv2.Merge(pcSplit, pointCloud)
        End If

        If task.rangesCloud Is Nothing Then
            Dim rx = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
            Dim ry = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
            Dim rz = New cv.Vec2f(0, task.MaxZmeters)
            task.rangesCloud = New cv.Rangef() {New cv.Rangef(rx.Item0, rx.Item1), New cv.Rangef(ry.Item0, ry.Item1),
                                                New cv.Rangef(rz.Item0, rz.Item1)}
        End If

        If task.optionsChanged Then task.motionMask.SetTo(255)

        motionBasics.Run(src)

        rgbFilter.Run(task.color)
        If rgbFilter.filterIndex > 0 Then
            task.color = rgbFilter.dst2
            src = rgbFilter.dst2
            gray = rgbFilter.dst3
        ElseIf rgbFilter.grayFilter.filterIndex > 0 Then
            rgbFilter.grayFilter.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            gray = rgbFilter.grayFilter.dst2
            src = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Else
            src = task.color
            gray = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        If task.optionsChanged Then grayStable = gray.Clone Else gray.CopyTo(grayStable, motionMask)

        edges.Run(task.grayStable)
        colorizer.Run(src)
        contours.Run(src.Clone)

        If task.brickRunFlag Then
            If task.bricks Is Nothing Then task.bricks = New Brick_Basics
            bricks.Run(src.Clone)
        End If

        TaskTimer.Enabled = True

        If pixelViewerOn And PixelViewer Is Nothing Then
            PixelViewer = New Pixel_Viewer
        Else
            If pixelViewerOn = False Then PixelViewer = Nothing
        End If

        If gOptions.CreateGif.Checked Then
            If gifCreator Is Nothing Then gifCreator = New Gif_OpenCVB
            gifCreator.Run(src.Clone)
            If gifBuild Then
                gifBuild = False
                For i = 0 To gifImages.Count - 1
                    Dim fileName As New FileInfo(HomeDir + "Temp/image" + Format(i, "000") + ".bmp")
                    gifImages(i).Save(fileName.FullName)
                Next

                gifImages.Clear()
                Dim dirInfo As New DirectoryInfo(HomeDir + "GifBuilder\bin\x64\Debug\")
                Dim dirData = dirInfo.GetDirectories()
                Dim gifExe As New FileInfo(dirInfo.FullName + "GifBuilder.exe")
                If gifExe.Exists = False Then
                    MsgBox("GifBuilder.exe was not found!")
                Else
                    Dim gifProcess As New Process
                    gifProcess.StartInfo.FileName = gifExe.FullName
                    gifProcess.StartInfo.WorkingDirectory = HomeDir + "Temp/"
                    gifProcess.Start()
                End If
            End If
        End If

        gravityBasics.Run(src.Clone)
        lineRGB.Run(grayStable)
        histBinList = {task.histogramBins, task.histogramBins, task.histogramBins}

        Dim saveOptionsChanged = task.optionsChanged
        If task.optionsChanged And treeView IsNot Nothing Then treeView.optionsChanged = True
        If activateTaskForms Then
            treeView.Activate()
            allOptions.Activate()
            activateTaskForms = False
        End If
        If task.paused = False Then




            algorithmPrep = False
            MainUI_Algorithm.processFrame(src.Clone) ' <<<<<<<< This is where the VB algorithm runs...
            algorithmPrep = True





            firstPass = False
            heartBeatLT = False

            postProcess(src)

            Dim displayObject = findDisplayObject(task.displayObjectName)
            If gOptions.displayDst0.Checked Then
                dst0 = Check8uC3(displayObject.dst0)
            Else
                dst0 = task.color.Clone
            End If
            If gOptions.displayDst1.Checked Then
                dst1 = Check8uC3(displayObject.dst1)
                displayDst1 = True
            Else
                dst1 = depthRGB
                displayDst1 = False
            End If

            dst2 = Check8uC3(displayObject.dst2)
            dst3 = Check8uC3(displayObject.dst3)

            ' make sure that any outputs from the algorithm are the right size.nearest
            If dst0.Size <> workingRes And dst0.Width > 0 Then
                dst0 = dst0.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            End If
            If dst1.Size <> workingRes And dst1.Width > 0 Then
                dst1 = dst1.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            End If
            If dst2.Size <> workingRes And dst2.Width > 0 Then
                dst2 = dst2.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            End If
            If dst3.Size <> workingRes And dst3.Width > 0 Then
                dst3 = dst3.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            End If

            If gOptions.ShowGrid.Checked Then dst2.SetTo(cv.Scalar.White, gridMask)

            If gOptions.showMotionMask.Checked Then
                For i = 0 To gridRects.Count - 1
                    If motionBasics.motionFlags(i) Then
                        dst0.Rectangle(gridRects(i), cv.Scalar.White, lineWidth)
                    End If
                Next
            End If

            If gOptions.CrossHairs.Checked And gravityBasics.gravityRGB IsNot Nothing Then
                Gravity_Basics.showVectors(dst0)
                Dim p2 = gravityBasics.gravityRGB.center
                p2 = New cv.Point(p2.X + 5, p2.Y)
                displayObject.trueData.Add(New TrueText("Gravity RGB Vector", p2, 0))
            End If

            ' if there were no cycles spent on this routine, then it was inactive.
            ' if any active algorithm has an index = -1, make sure it is running .Run, not .RunAlg
            Dim index = algorithmNames.IndexOf(displayObject.traceName)
            If index = -1 Then
                displayObject.trueData.Add(New TrueText("This task is not active at this time.",
                                           New cv.Point(dst2.Width / 3, dst2.Height / 2), 2))
                displayObject.trueData.Add(New TrueText("This task is not active at this time.",
                                           New cv.Point(dst2.Width / 3, dst2.Height / 2), 3))
            End If

            trueData = New List(Of TrueText)(displayObject.trueData)
            displayObject.trueData.Clear()
            labels = displayObject.labels
            If displayDst1 Then labels(1) = displayObject.labels(1)
            depthAndCorrelationText = task.depthAndCorrelationText
        Else
            dst1 = depthRGB
        End If

        Return saveOptionsChanged
    End Function
    Public Sub Dispose() Implements IDisposable.Dispose
        allOptions.Close()
        If openGL_hwnd <> 0 Then OpenGLClose()
        TaskTimer.Enabled = False
        For Each algorithm In task.activeObjects
            Dim type As Type = algorithm.GetType()
            If type.GetMethod("Close") IsNot Nothing Then
                algorithm.Close()  ' Close any unmanaged classes...
            End If
        Next
    End Sub
End Class
