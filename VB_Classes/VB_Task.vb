Imports cv = OpenCvSharp
Imports System.Windows.Forms
Imports System.IO.Pipes
Imports System.Drawing
Imports System.IO

Public Class VBtask : Implements IDisposable
#Region "VBTask variables"
    Public TaskTimer As New System.Timers.Timer(1000)
    Public algoList As New cppAlgorithms

    Public algorithmObject As Object
    Public frameCount As Integer = 0
    Public heartBeat As Boolean
    Public quarterBeat As Boolean
    Public midHeartBeat As Boolean
    Public almostHeartBeat As Boolean
    Public myStopWatch As Stopwatch
    Public msWatch As Integer
    Public msLast As Integer

    Public toggleOnOff As Boolean ' toggles on the heartbeat.
    Public optionsChanged As Boolean ' global or local options changed.
    Public paused As Boolean
    Public showAllOptions As Boolean ' show all options when initializing the options for all algorithms.

    Public dst0 As cv.Mat
    Public dst1 As cv.Mat
    Public dst2 As cv.Mat
    Public dst3 As cv.Mat

    Public mbuf(2 - 1) As inBuffer
    Public mbIndex As Integer

    Public color As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public pointCloud As cv.Mat

    Public pcSplit() As cv.Mat
    Public pcFloor As Single ' y-value for floor...
    Public pcCeiling As Single ' y-value for ceiling...

    Public debugSyncUI As Boolean

    Public workingRes As cv.Size
    Public resolutionRatio As Single ' mousepoints/drawrects need the ratio of the display to the working resolution.
    Public disparityAdjustment As Single ' adjusts for resolution and some hidden elements.

    Public motionRect As cv.Rect
    Public motionFlag As Boolean
    Public motionDetected As Boolean

    Public gravityVec As New pointPair
    Public horizonVec As New pointPair

    Public camMotionPixels As Single ' distance in pixels that the camera has moved.
    Public camDirection As Single ' camera direction in radians.
    Public cMotion As CameraMotion_Basics

    ' add any global algorithms here
    Public PixelViewer As Pixel_Viewer
    Public colorizer As Depth_Colorizer_CPP
    Public hCloud As History_Cloud
    Public motionCloud As Motion_PointCloud
    Public motionColor As Motion_Color
    Public motionBasics As Motion_BasicsQuarterRes
    Public rgbFilter As Object

    Public gMat As IMU_GMatrix
    Public IMUBasics As IMU_Basics
    Public IMU_RawAcceleration As cv.Point3f
    Public IMU_Acceleration As cv.Point3f
    Public IMU_AverageAcceleration As cv.Point3f
    Public IMU_RawAngularVelocity As cv.Point3f
    Public IMU_AngularVelocity As cv.Point3f
    Public kalmanIMUacc As cv.Point3f
    Public kalmanIMUvelocity As cv.Point3f
    Public IMU_TimeStamp As Double
    Public IMU_Rotation As System.Numerics.Quaternion
    Public IMU_Translation As cv.Point3f
    Public IMU_AngularAcceleration As cv.Point3f
    Public IMU_FrameTime As Double
    Public IMU_AlphaFilter As Single ' high pass and low pass filter of the IMU acceleration data.

    Public accRadians As cv.Point3f  ' rotation angles around x/y/z-axis to align with gravity
    Public theta As cv.Point3f ' velocity-filtered angles around x/y/z-axis to align with gravity

    Public pitch As Single
    Public yaw As Single
    Public roll As Single

    Public gMatrix As cv.Mat ' transformation matrix to convert point cloud to be vertical according to gravity.

    Public imuStabilityTest As Stabilizer_VerticalIMU
    Public cameraStable As Boolean
    Public cameraStableString As String

    Public noDepthMask As New cv.Mat
    Public depthMask As New cv.Mat

    Public maxDepthMask As New cv.Mat
    Public depthRGB As New cv.Mat

    Public srcThread As cv.Mat
    Public recordTimings As Boolean = True

    Public highlightColor As cv.Scalar ' color to use to highlight objects in an image.
    Public activateTaskRequest As Boolean

    Public histogramBins As Integer

    Public grid As Grid_Basics
    Public gridRows As Integer
    Public gridCols As Integer
    Public gridIndex As New List(Of Integer)
    Public gridList As List(Of cv.Rect)
    Public subDivisions As New List(Of Integer)
    Public subDivisionCount As Integer = 9
    Public gridMask As cv.Mat
    Public gridMap As New cv.Mat
    Public gridNeighbors As New List(Of List(Of Integer))
    Public gridROIclicked As Integer
    Public ogl As OpenGL_Basics

    Public palette As Palette_LoadColorMap
    Public paletteGradient As cv.Mat
    Public paletteIndex As Integer

    Public mouseClickFlag As Boolean
    Public clickPoint As cv.Point
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mouseMovePoint As cv.Point ' trace any mouse movements using this.
    Public mouseMovePointUpdated As Boolean

    Public dotSize As Integer
    Public lineWidth As Integer
    Public lineType As cv.LineTypes
    Public resolutionIndex As Integer
    Public lowRes As cv.Size
    Public quarterRes As cv.Size
    Public displayRes As cv.Size

    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double

    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public drawRectUpdated As Boolean

    Public pixelViewerRect As cv.Rect
    Public pixelViewTag As Integer

    Public pipeName As String

    Public pythonPipeIn As NamedPipeServerStream
    Public pythonPipeOut As NamedPipeServerStream
    Public pythonTaskName As String
    Public pythonProcess As Process
    Public pythonReady As Boolean

    Public labels(4 - 1) As String
    Public desc As String
    Public advice As String = ""
    Public intermediateName As String
    Public intermediateObject As VB_Algorithm
    Public activeObjects As New List(Of Object)
    Public pixelViewerOn As Boolean

    Public transformationMatrix() As Single

    Public scalarColors(255) As cv.Scalar
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
    Public homeDir As String
    Public fpsRate As Integer
    Public densityMetric As Integer ' how dense is the pointcloud in z - heuristic.
    Public FASTthreshold As Integer

    Public externalPythonInvocation As Boolean
    Public useRecordedData As Boolean
    Public testAllRunning As Boolean
    Public showConsoleLog As Boolean

    Public mainFormLocation As cv.Rect
    Public main_hwnd As IntPtr

    Public trueData As New List(Of trueText)
    Public flowData As New List(Of trueText)
    Public callTrace As New List(Of String)

    Public algorithm_ms As New List(Of Single)
    Public algorithmNames As New List(Of String)
    Public waitingForInput As Single ' the amount of time waiting for buffers.
    Public inputBufferCopy As Single ' the amount of time copying the buffers.
    Public returnCopyTime As Single ' the amount of time returning buffers to the host.
    Public algorithmAccumulate As Boolean ' accumulate times or use latest interval times.

    Public OpenGLTitle As String
    Public oglRect As cv.Rect
    Public polyCount As Integer

    Public gifCreator As Gif_OpenCVB
    Public gifImages As New List(Of Bitmap)
    Public gifBuild As Boolean
    Public gifCaptureIndex As Integer
    Public Enum gifTypes
        gifdst0 = 0
        gifdst1 = 1
        gifdst2 = 2
        gifdst3 = 3
        openCVBwindow = 4
        openGLwindow = 5
    End Enum
    Public cvFontSize As Single = 0.8
    Public cvFontThickness As Integer = 1

    Public rangesTop() As cv.Rangef
    Public rangesSide() As cv.Rangef
    Public channelsTop() As Integer
    Public channelsSide() As Integer
    Public bins2D() As Integer
    Public frameHistoryCount As Integer ' count of how much history to use for the point cloud.
    Public depthThresholdPercent As Single

    Public projectionThreshold As Integer ' In heatmap views, this defines what is hot in a heatmap.

    Public rc As New rcData
    Public redCells As List(Of rcData)
    Public cellMap As cv.Mat

    Public features As New List(Of cv.Point2f)
    Public featurePoints As New List(Of cv.Point)
    Public featureMotion As Boolean ' false means that none of the features moved.

    Public useXYRange As Boolean ' OpenGL applications don't need to adjust the ranges.
    Public xRange As Single
    Public yRange As Single
    Public xRangeDefault As Single
    Public yRangeDefault As Single
    Public maxZmeters As Single
    Public metersPerPixel As Single
#End Region
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
        Public Shared cameraNames As New List(Of String)({"Azure Kinect 4K",
                                                         "Intel(R) RealSense(TM) Depth Camera 435i",
                                                         "Intel(R) RealSense(TM) Depth Camera 455",
                                                         "Oak-D camera",
                                                         "StereoLabs ZED 2/2i",
                                                         "MYNT-EYE-D1000"})
        Public cameraName As String
        Public cameraIndex As Integer

        Public homeDir As String
        Public useRecordedData As Boolean
        Public externalPythonInvocation As Boolean ' OpenCVB was initialized remotely...
        Public showConsoleLog As Boolean
        Public testAllRunning As Boolean
        Public RotationMatrix() As Single
        Public RotationVector As cv.Point3f
        Public pixelViewerOn As Boolean

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
            task.scalarColors(i) = New cv.Scalar(task.vecColors(i)(0), task.vecColors(i)(1), task.vecColors(i)(2))
        Next
    End Sub
    Private Sub VBTaskTimerPop(sender As Object, e As EventArgs)
        Static WarningCount As Integer
        Static saveFrameCount = -1
        If saveFrameCount = frameCount And frameCount > 0 And WarningCount = 0 Then
            Console.WriteLine("Warning: " + task.algName + " has not completed work on a frame in a second. Warning " + CStr(WarningCount))
            WarningCount += 1
        Else
            WarningCount = 0
            saveFrameCount = frameCount
        End If
        saveFrameCount = frameCount
    End Sub
    Public Sub OpenGLClose()
        If openGL_hwnd <> 0 Then
            Dim r As RECT
            GetWindowRect(openGL_hwnd, r)
            Dim wRect = New cv.Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top)
            SaveSetting("OpenCVB", "OpenGLtaskX", "OpenGLtaskX", wRect.X)
            SaveSetting("OpenCVB", "OpenGLtaskY", "OpenGLtaskY", wRect.Y)
            SaveSetting("OpenCVB", "OpenGLtaskWidth", "OpenGLtaskWidth", wRect.Width)
            openGLPipe.Close()
            openGL_hwnd = 0
        End If
    End Sub
    Public Sub New(parms As algParms)
        AddHandler TaskTimer.Elapsed, New Timers.ElapsedEventHandler(AddressOf VBTaskTimerPop)
        TaskTimer.AutoReset = True
        TaskTimer.Enabled = True
        Randomize() ' just in case anyone uses VB.Net's Rnd

        task = Me
        useXYRange = True ' Most projections of pointcloud data can use the xRange and yRange to improve results.
        gridList = New List(Of cv.Rect)
        algName = parms.algName
        cameraName = parms.cameraName
        testAllRunning = parms.testAllRunning
        showConsoleLog = parms.showConsoleLog
        fpsRate = parms.fpsRate
        calibData = parms.cameraInfo
        homeDir = parms.homeDir
        main_hwnd = parms.main_hwnd
        useRecordedData = parms.useRecordedData
        externalPythonInvocation = parms.externalPythonInvocation

        mainFormLocation = parms.mainFormLocation
        workingRes = parms.workingRes ' gets referenced a lot
        resolutionIndex = If(workingRes.Width = 640, 2, 3)
        displayRes = parms.displayRes

        buildColors()
        pythonTaskName = homeDir + "Python_Classes\" + algName

        allOptions = New OptionsContainer
        allOptions.Show()

        callTrace.Add("Options_XYRanges") ' so calltrace is not nothing on initial call...
        gOptions = New OptionsGlobal
        redOptions = New OptionsRedCloud

        grid = New Grid_Basics
        PixelViewer = New Pixel_Viewer

        colorizer = New Depth_Colorizer_CPP
        IMUBasics = New IMU_Basics
        gMat = New IMU_GMatrix
        hCloud = New History_Cloud
        motionCloud = New Motion_PointCloud
        motionColor = New Motion_Color
        motionBasics = New Motion_BasicsQuarterRes
        cMotion = New CameraMotion_Basics
        imuStabilityTest = New Stabilizer_VerticalIMU

        updateSettings()
        redOptions.Show()
        gOptions.Show()
        palette = New Palette_LoadColorMap
        If algName.StartsWith("OpenGL_") Or algName.EndsWith("_OpenGL") Or algName.StartsWith("Model_") Then
            ogl = New OpenGL_Basics
        End If

        callTrace.Clear()
        callTrace.Add(algName + "\")
        activeObjects.Clear()
        algorithmObject = algoList.createAlgorithm(algName)
        If algorithmObject Is Nothing Then
            desc = algName
        Else
            desc = algorithmObject.desc
        End If

        If task.advice = "" Then
            task.advice = "No advice for " + algName + " yet." + vbCrLf +
                           "Please use 'vbAddAdvice(<your advice>)' in the constructor)."
        End If

        If parms.useRecordedData Then recordedData = New Replay_Play()

        ' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
        ' https://www.intelrealsense.com/depth-camera-d435i/
        ' https://www.intelrealsense.com/depth-camera-d455/
        ' https://towardsdatascience.com/opinion-26190c7fed1b
        ' https://support.stereolabs.com/hc/en-us/articles/360007395634-What-is-the-camera-focal-length-and-field-of-view-
        ' https://www.stereolabs.com/assets/datasheets/zed-2i-datasheet-feb2022.pdf
        ' https://www.mynteye.com/pages/mynt-eye-d   
        ' order of cameras is the same as the order above... see cameraNames above
        Dim vFOVangles() As Single = {59, 72, 58, 42.5, 57, 62} ' all values from the specification - this is usually overridden by calibration data.
        Dim hFOVangles() As Single = {90, 104, 105, 69.4, 86, 69} ' all values from the specification - this is usually overridden by calibration data.
        Dim focalLengths() As Single = {5.5, 3.4, 1.88, 4.81, 2.31, 2.45}
        Dim baseLines() As Single = {0.074, 0.073, 0.055, 0.052, 0.06, 0.048} ' in meters

        ' NOTE: I can't find the VFOV for the Oak-D or Oak-D Lite cameras.
        ' The 62 is based on Pythagorean theorem and knowing the 71.8 HFOV and the 81.3 DFOV.
        If parms.cameraInfo.v_fov <> 0 Then vFOVangles(parms.cameraIndex) = parms.cameraInfo.v_fov
        If parms.cameraInfo.h_fov <> 0 Then hFOVangles(parms.cameraIndex) = parms.cameraInfo.h_fov

        vFov = vFOVangles(parms.cameraIndex)  ' these are default values in case the calibration data is unavailable
        hFov = hFOVangles(parms.cameraIndex)
        focalLength = focalLengths(parms.cameraIndex)
        baseline = baseLines(parms.cameraIndex)

        task.myStopWatch = Stopwatch.StartNew()
        optionsChanged = True
        Application.DoEvents()
    End Sub
    Public Sub RunAlgorithm()
        Try
            If allOptions.titlesAdded Then
                allOptions.titlesAdded = False
                allOptions.layoutOptions(normalRequest:=True)
            End If

            If task.testAllRunning = False Then
                If algorithmAccumulate = False Then
                    If task.heartBeat Then
                        For i = 0 To algorithm_ms.Count - 1
                            algorithm_ms(i) = 0
                        Next
                    End If
                End If

                algorithm_ms(0) += waitingForInput
                algorithm_ms(1) += inputBufferCopy
                algorithm_ms(2) += returnCopyTime
                algorithmTimes(3) = Now  ' starting the main algorithm
            End If
            If task.useRecordedData Then recordedData.Run(task.color.Clone)

            redOptions.Sync()

            task.bins2D = {task.workingRes.Height, task.workingRes.Width}
            Dim src = task.color

            ' If the workingRes changes, the previous generation of images needs to be reset.
            If task.pointCloud.Size <> task.workingRes Or task.color.Size <> task.workingRes Then
                task.pointCloud = New cv.Mat(task.workingRes, cv.MatType.CV_32FC3, 0)
                task.noDepthMask = New cv.Mat(task.workingRes, cv.MatType.CV_8U, 0)
                task.depthMask = New cv.Mat(task.workingRes, cv.MatType.CV_8U, 0)
            End If

            allOptions.TopMost = task.activateTaskRequest
            Application.DoEvents()

            ' run any universal algorithms here
            task.IMU_RawAcceleration = task.IMU_Acceleration
            task.IMU_RawAngularVelocity = task.IMU_AngularVelocity
            task.IMU_AlphaFilter = gOptions.IMU_Alpha.Value / 100
            grid.RunVB(task.color)

            If task.algName.StartsWith("CPP_") = False Then task.motionFlag = True

            updateSettings()
            imuStabilityTest.RunVB(src)
            task.cameraStable = imuStabilityTest.stableTest
            task.cameraStableString = imuStabilityTest.stableStr
            IMUBasics.RunVB(src)
            gMat.RunVB(src)

            If gOptions.CreateGif.Checked Then
                heartBeat = False
                task.optionsChanged = False
            Else
                task.heartBeat = task.heartBeat Or task.debugSyncUI Or task.optionsChanged Or task.mouseClickFlag
            End If

            If task.paused = False Then
                task.frameHistoryCount = gOptions.FrameHistory.Value

                If gOptions.gravityPointCloud.Checked Then
                    '******* this is the rotation *******
                    task.pointCloud = (task.pointCloud.Reshape(1, src.Rows * src.Cols) * task.gMatrix).ToMat.Reshape(3, src.Rows)
                End If

                If task.pcSplit Is Nothing Then task.pcSplit = task.pointCloud.Split




                gOptions.unFiltered.Checked = True ' until the motion rectangle problems are resolved.




                ' on each heartbeat or when options changed, update the whole image.
                If task.heartBeat Or gOptions.unFiltered.Checked Then
                    task.motionDetected = True
                    task.motionRect = New cv.Rect(0, 0, src.Width, src.Height)
                    motionColor.dst2 = src.Clone
                    motionCloud.dst2 = task.pointCloud.Clone
                Else
                    motionBasics.RunVB(src) ' get the latest motionRect
                    If gOptions.UseHistoryCloud.Checked Then
                        hCloud.RunVB(task.pointCloud)
                        task.pointCloud = hCloud.dst2
                    ElseIf gOptions.MotionFilteredColorAndCloud.Checked Then
                        motionColor.RunVB(src)
                        task.color = motionColor.dst2.Clone
                        motionCloud.RunVB(task.pointCloud)
                        task.pointCloud = motionCloud.dst2.Clone
                    ElseIf gOptions.MotionFilteredCloudOnly.Checked Then
                        motionCloud.RunVB(task.pointCloud)
                        task.pointCloud = motionCloud.dst2.Clone
                    ElseIf gOptions.MotionFilteredColorOnly.Checked Then
                        motionColor.RunVB(src)
                        task.color = motionColor.dst2.Clone
                    End If
                End If
            End If

            If task.motionDetected Or heartBeat Then
                task.pcSplit = task.pointCloud.Split

                If task.optionsChanged Then task.maxDepthMask.SetTo(0)
                task.pcSplit(2) = task.pcSplit(2).Threshold(task.maxZmeters, task.maxZmeters, cv.ThresholdTypes.Trunc)
                'task.maxDepthMask = task.pcSplit(2).InRange(task.maxZmeters, task.maxZmeters).ConvertScaleAbs()

                task.depthMask = task.pcSplit(2).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
                task.noDepthMask = Not task.depthMask

                If task.xRange <> task.xRangeDefault Or task.yRange <> task.yRangeDefault Then
                    Dim xRatio = task.xRangeDefault / task.xRange
                    Dim yRatio = task.yRangeDefault / task.yRange
                    task.pcSplit(0) *= xRatio
                    task.pcSplit(1) *= yRatio

                    cv.Cv2.Merge(task.pcSplit, task.pointCloud)
                End If
            End If

            ' small improvement to speed up colorized depth - make it smaller before colorizing.
            Dim depthRGBInput = task.pcSplit(2).Resize(task.quarterRes)
            colorizer.RunVB(depthRGBInput.Threshold(task.maxZmeters, task.maxZmeters, cv.ThresholdTypes.Trunc))
            task.depthRGB = colorizer.dst2.Resize(task.color.Size)

            TaskTimer.Enabled = True

            If gOptions.CreateGif.Checked Then
                If task.gifCreator Is Nothing Then task.gifCreator = New Gif_OpenCVB
                gifCreator.RunVB(src)
                If task.gifBuild Then
                    task.gifBuild = False
                    If task.gifImages.Count = 0 Then
                        MsgBox("Collect images first and then click 'Build GIF...'")
                    Else
                        For i = 0 To task.gifImages.Count - 1
                            Dim fileName As New FileInfo(task.homeDir + "Temp/image" + Format(i, "000") + ".bmp")
                            task.gifImages(i).Save(fileName.FullName)
                        Next

                        task.gifImages.Clear()
                        Dim dirInfo As New DirectoryInfo(task.homeDir + "\GifBuilder\bin\Release\")
                        Dim dirData = dirInfo.GetDirectories()
                        Dim gifExe As New FileInfo(dirInfo.FullName + "\" + dirData(0).ToString + "\GifBuilder.exe")
                        If gifExe.Exists = False Then
                            MsgBox("GifBuilder.exe was not found!")
                        Else
                            Dim gifProcess As New Process
                            gifProcess.StartInfo.FileName = gifExe.FullName
                            gifProcess.StartInfo.WorkingDirectory = task.homeDir + "Temp/"
                            gifProcess.Start()
                        End If
                    End If
                End If
            End If

            If gOptions.RGBFilterActive.Checked Then
                Dim filterName = gOptions.RGBFilterList.Text
                If rgbFilter Is Nothing Then rgbFilter = algoList.createAlgorithm(filterName)
                If rgbFilter.traceName <> filterName Then rgbFilter = algoList.createAlgorithm(filterName)
                rgbFilter.RunVB(src)
                src = rgbFilter.dst2
            End If

            cMotion.Run(src)
            algorithmObject.NextFrame(src.Clone)  ' <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< This is where the requested algorithm begins...

            Dim rc = task.rc
            If task.redCells.Count > 0 Then setSelectedContour()

            If redOptions.IdentifyCells.Checked Then
                Dim ptNew As New cv.Point
                Dim ptCells As New List(Of cv.Point)
                For i = 1 To redCells.Count - 1
                    Dim rcx = redCells(i)
                    If ptCells.Contains(rcx.maxDStable) = False Then
                        If rcx.maxDStable <> ptNew And rcx.index <= redOptions.identifyCount Then
                            Dim str As New trueText(CStr(rcx.index), rcx.maxDStable, 2)
                            trueData.Add(str)
                        End If
                        ptCells.Add(rcx.maxDStable)
                    End If
                Next

                If rc.index > 0 Then
                    task.color.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
                    task.color(rc.rect).SetTo(cv.Scalar.White, rc.mask)

                    task.depthRGB.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
                    If redOptions.DisplayCellStats.Checked Then
                        dst3.SetTo(0)
                        If task.clickPoint = New cv.Point Then
                            If task.redCells.Count > 1 Then
                                task.rc = task.redCells(1)
                                task.clickPoint = task.rc.maxDist
                            End If
                        End If
                        Static cellStats As New Cell_Basics
                        cellStats.statsString()
                        dst1 = cellStats.dst1
                        Dim str As New trueText(cellStats.strOut, New cv.Point, 3)
                        trueData.Add(str)
                    End If
                End If
            End If

            If redOptions.DisplayCellStats.Checked And task.clickPoint = New cv.Point Then
                If task.redCells.Count > 1 Then
                    task.rc = task.redCells(1)
                    task.clickPoint = task.rc.maxDist
                End If
            End If

            If task.motionDetected And gOptions.ShowMotionRectangle.Checked Then
                task.color.Rectangle(task.motionRect, cv.Scalar.White, task.lineWidth)
            End If

            If gOptions.CrossHairs.Checked Then
                If task.paused = False Then
                    task.color.Line(task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.White, task.lineWidth, task.lineType)
                    task.color.Line(task.gravityVec.p1, task.gravityVec.p2, cv.Scalar.White, task.lineWidth, task.lineType)
                End If
            End If

            task.activateTaskRequest = False ' let the task see the activate request so it can activate any OpenGL or Python app running externally.
            task.optionsChanged = False
            TaskTimer.Enabled = False
            task.frameCount += 1
        Catch ex As Exception
            Console.WriteLine("Active Algorithm exception occurred: " + ex.Message)
        End Try
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If openGL_hwnd <> 0 Then
            task.OpenGLClose()
            openGL_hwnd = 0
        End If
        TaskTimer.Enabled = False
        allOptions.Close()
        If algorithmObject IsNot Nothing Then algorithmObject.Dispose()
    End Sub
    Public Sub trueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New trueText(text, pt, picTag)
        task.trueData.Add(str)
    End Sub
End Class