Imports System.IO
Imports System.IO.Pipes
Imports System.Runtime.InteropServices
Imports System.Threading
Imports SharpGL.SceneGraph.Cameras
Imports cv = OpenCvSharp

#Region "taskProcess"
<StructLayout(LayoutKind.Sequential)>
Public Class VBtask : Implements IDisposable
    Public dstList(3) As cv.Mat

    Public optionsChanged As Boolean
    Public allOptions As OptionsContainer
    Public gOptions As OptionsGlobal
    Public featureOptions As OptionsFeatures
    Public treeView As TreeViewForm
    Public settings As Object

    Public color As New cv.Mat
    Public gray As New cv.Mat
    Public grayStable As New cv.Mat
    Public leftViewStable As New cv.Mat
    Public leftView As New cv.Mat
    Public rightView As New cv.Mat
    Public pointCloud As New cv.Mat
    Public gravityCloud As New cv.Mat
    Public sharpDepth As cv.Mat
    Public sharpRGB As cv.Mat
    Public pcSplit() As cv.Mat
    Public depthmask As cv.Mat
    Public noDepthMask As cv.Mat
    Public depthRGB As cv.Mat

    Public gridRects As List(Of cv.Rect)
    Public firstPass As Boolean = True
    Public cameraName As String

    Public testAllDuration As Integer
    Public verticalLines As Boolean
    Public edgeMethod As String

    Public workRes As cv.Size
    Public rows As Integer
    Public cols As Integer
    Public captureRes As cv.Size

    ' Global Options 
    Public DotSize As Integer
    Public lineWidth As Integer
    Public cvFontThickness As Integer
    Public brickSize As Integer
    Public reductionTarget As Integer
    Public cvFontSize As Single
    Public lineType As cv.LineTypes
    Public histogramBins As Integer
    Public MaxZmeters As Single
    Public highlight As cv.Scalar
    Public closeRequest As Boolean
    Public paletteIndex As Integer
    Public fCorrThreshold As Single
    Public FeatureSampleSize As Integer
    Public clickPoint As New cv.Point ' last place where mouse was clicked.

    ' TreeView and trace Data.
    Public callTrace As List(Of String)
    Public algorithm_msMain As New List(Of Single)
    Public algorithmNamesMain As New List(Of String)
    Public algorithm_ms As New List(Of Single)
    Public algorithmNames As New List(Of String)
    Public algorithmTimes As New List(Of DateTime)
    Public algorithmStack As New Stack()
    Public displayObjectName As String
    Public activeObjects As New List(Of Object)
    Public calibData As Object

    Public fpsAlgorithm As Single
    Public fpsCamera As Single
    Public testAllRunning As Boolean
    Public main_hwnd As IntPtr

    ' color maps
    Public scalarColors(255) As cv.Scalar
    Public vecColors(255) As cv.Vec3b
    Public depthColorMap As cv.Mat
    Public colorMap As cv.Mat
    Public colorMapZeroIsBlack As cv.Mat
    Public correlationColorMap As cv.Mat

    ' task algorithms - operate on every frame regardless of which algorithm is being run.
    Public colorizer As DepthColorizer_Basics
    Public redColor As RedColor_Basics
    Public redList As RedList_Basics
    Public redCloud As RedCloud_Basics
    Public gmat As IMU_GMatrix
    Public lines As Line_Basics
    Public grid As Grid_Basics
    Public palette As Palette_LoadColorMap
    Public PixelViewer As Pixel_Viewer
    Public rgbFilter As Filter_Basics
    Public gravityBasics As Gravity_Basics
    Public imuBasics As IMU_Basics
    Public motionBasics As Motion_Basics
    Public contours As Contour_Basics_List
    Public pcMotion As Motion_PointCloud







    Public GLRequest As Integer
    Public GLcloud As cv.Mat
    Public GLrgb As cv.Mat

    Public motionThreshold As Integer ' this is vital to motion detection - lower to be more sensitive, higher for less.
    Public colorDiffThreshold As Integer

    Public motionLinkType As Integer = 8

    Public feat As Feature_Basics
    Public bricks As Brick_Basics

    Public fpList As New List(Of fpData)
    Public regionList As New List(Of oldrcData)
    Public featList As New List(Of List(Of Integer))
    Public fLess As New List(Of List(Of Integer))

    Public fpMap As New cv.Mat ' feature map

    Public brickD As brickData ' the currently selected brick
    Public rcD As New rcData ' the currently selected redCloud Cell
    Public oldrcD As New oldrcData ' the currently selected redColor Cell
    Public lpD As New lpData ' the currently selected line pair
    Public fpD As New fpData ' the currently selected feature point.
    Public contourD As New contourData ' the currently selected contour

    Public bricksPerCol As Integer
    Public bricksPerRow As Integer
    Public gridMap As New cv.Mat
    Public gridMask As New cv.Mat
    Public gridNabeRects As New List(Of cv.Rect) ' The surrounding rect for every gridRect
    Public gridROIclicked As Integer
    Public depthDiffMeters As Single ' bricks > than this value are depth edges - in meters
    Public rgbLeftAligned As Boolean ' if the rgb image is the left image...

    Public fpMotion As cv.Point2f

    Public features As New List(Of cv.Point2f)
    Public fpFromGridCell As New List(Of Integer)
    Public fpFromGridCellLast As New List(Of Integer)
    Public fpLastList As New List(Of fpData)
    Public featurePoints As New List(Of cv.Point)

    Public flessBoundary As New cv.Mat
    Public lowResColor As New cv.Mat
    Public lowResDepth As New cv.Mat

    Public motionMask As New cv.Mat
    Public motionMaskRight As New cv.Mat ' motion mask for the right view.
    Public motionRect As cv.Rect

    ' if true, algorithm prep means algorithm tasks will run.  If false, they have already been run...
    Public algorithmPrep As Boolean = True

    Public MainUI_Algorithm As Object
    Public myStopWatch As Stopwatch

    ' transformation matrix to convert point cloud to be vertical according to gravity.
    Public gMatrix As New cv.Mat
    Public IMU_Rotation As System.Numerics.Quaternion
    Public maxDepthMask As New cv.Mat

    Public camMotionPixels As Single ' distance in pixels that the camera has moved.
    Public camDirection As Single ' camera direction in radians.

    Public paletteRandom As Palette_RandomColors
    Public kalman As Kalman_Basics

    Public gifCreator As Gif_OpenCVB
    Public gifImages As New List(Of Bitmap)
    Public gifBuild As Boolean
    Public gifCaptureIndex As Integer

    Public transformationMatrix() As Single

    Public frameCount As Integer = 1
    Public heartBeat As Boolean
    Public heartBeatLT As Boolean = True ' long term heartbeat - every X seconds.
    Public quarterBeat As Boolean
    Public quarter(4 - 1) As Boolean
    Public midHeartBeat As Boolean
    Public almostHeartBeat As Boolean
    Public afterHeartBeatLT As Boolean
    Public msWatch As Integer
    Public msLast As Integer

    Public toggleOn As Boolean ' toggles on the heartbeat.
    Public paused As Boolean
    Public showAllOptions As Boolean ' show all options when initializing the options for all algorithms.

    Public pcFloor As Single ' y-value for floor...
    Public pcCeiling As Single ' y-value for ceiling...

    Public debugSyncUI As Boolean

    Public lineGravity As New lpData
    Public lineHorizon As New lpData
    Public lineLongest As New lpData
    Public lineLongestChanged As Boolean
    Public angleThreshold = 2

    Public gravityIMU As New lpData
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

    ' RedCloud variables
    Public channelCount As Integer = 2
    Public channelIndex As Integer = 0
    Public channels() As Integer = {0, 1}
    Public histBinList() As Integer
    Public ranges() As cv.Rangef
    Public rangesBGR() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 256), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
    Public rangesHSV() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 180), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
    Public rangesCloud() As cv.Rangef

    Public mouseClickFlag As Boolean
    Public activateTaskForms As Boolean
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mouseMovePoint As cv.Point ' trace any mouse movements using this.
    Public mouseMovePointUpdated As Boolean

    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double

    Public centerRect As cv.Rect ' image center - potential use for motion.

    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public drawRectUpdated As Boolean

    Public pixelViewerRect As cv.Rect
    Public pixelViewTag As Integer

    Public pipeName As String

    Public labels() = {"", "", "", ""}
    Public pixelViewerOn As Boolean

    Public topCameraPoint As cv.Point
    Public sideCameraPoint As cv.Point

    Public hFov As Single
    Public vFov As Single

    Public mainFormLocation As cv.Rect

    Public trueData As New List(Of TrueText)

    Public waitingForInput As Single ' the amount of time waiting for buffers.

    Public OpenGLTitle As String
    Public polyCount As Integer

    Public rangesTop() As cv.Rangef
    Public rangesSide() As cv.Rangef
    Public channelsTop() As Integer
    Public channelsSide() As Integer
    Public bins2D() As Integer
    Public frameHistoryCount As Integer ' count of how much history to use for the point cloud.

    Public projectionThreshold As Integer ' In heatmap views, this defines what is hot in a heatmap.

    Public useXYRange As Boolean ' OpenGL applications don't need to adjust the ranges.
    Public reductionName As String = "XY Reduction"
    Public xRange As Single
    Public yRange As Single
    Public xRangeDefault As Single
    Public yRangeDefault As Single
    Public metersPerPixel As Single
    Public OpenGL_Left As Integer
    Public OpenGL_Top As Integer
    Public displayDst1 As Boolean
    Public depthAndDepthRange As String = ""
    Public resolutionDetails As String = ""
    Public sharpGL As VBClasses.SharpGLForm

#End Region
    Private Sub postProcess(src As cv.Mat, dst1 As cv.Mat, dst2 As cv.Mat, dst3 As cv.Mat)
        Try
            If PixelViewer IsNot Nothing Then
                If pixelViewerOn Then
                    PixelViewer.viewerForm.Visible = True
                    PixelViewer.viewerForm.Show()
                    PixelViewer.dst0Input = src
                    PixelViewer.dst1Input = dst1
                    PixelViewer.dst2Input = dst2
                    PixelViewer.dst3Input = dst3
                    PixelViewer.Run(src)
                Else
                    PixelViewer.viewerForm.Visible = False
                End If
            End If

            If gifCreator IsNot Nothing Then gifCreator.createNextGifImage()

            If optionsChanged = True And treeView IsNot Nothing Then
                treeView.optionsChanged = True
                Dim sender As Object = Nothing, e As EventArgs = Nothing
                treeView.optionsChanged = False
            End If
            optionsChanged = False
            frameCount += 1
        Catch ex As Exception
            Debug.WriteLine("Active Algorithm exception occurred: " + ex.Message)
        End Try
    End Sub
    Public Sub New()
        task = Me
        Randomize() ' just in case anyone uses VB.Net's Rnd
        gridRects = New List(Of cv.Rect)
        optionsChanged = True
        firstPass = True
        useXYRange = True ' Most projections of pointcloud data can use the xRange and yRange to improve task.results..
    End Sub
    Public Sub Initialize()
        rgbLeftAligned = True
        If settings.cameraName.Contains("RealSense") Then rgbLeftAligned = False

        rows = settings.workRes.Height
        cols = settings.workRes.Width
        workRes = settings.workRes
        captureRes = settings.captureRes
        resolutionDetails = "RGB Input " + CStr(settings.captureRes.Width) + "x" + CStr(settings.captureRes.Height) +
                            ", workRes " + CStr(workRes.Width) + "x" + CStr(workRes.Height)

        allOptions = New OptionsContainer
        allOptions.Show()

        If settings.algorithm.StartsWith("GL_") And settings.algorithm <> "GL_MainForm" And optionsChanged Then
            If sharpGL IsNot Nothing Then sharpGL.Dispose()
            sharpGL = New SharpGLForm
            sharpGL.Show()
        End If

        gOptions = New OptionsGlobal
        featureOptions = New OptionsFeatures
        treeView = New TreeViewForm

        callTrace = New List(Of String)
        gravityCloud = New cv.Mat(workRes, cv.MatType.CV_32FC3, 0)
        task.motionMask = New cv.Mat(task.workRes, cv.MatType.CV_8U, 255)
        noDepthMask = New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
        depthmask = New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)

        colorizer = New DepthColorizer_Basics
        gmat = New IMU_GMatrix
        gravityBasics = New Gravity_Basics
        imuBasics = New IMU_Basics
        motionBasics = New Motion_Basics
        pcMotion = New Motion_PointCloud
        grid = New Grid_Basics
        lines = New Line_Basics
        rgbFilter = New Filter_Basics

        ' all the algorithms in the list are task algorithms that are children of the algorithm.
        For i = 1 To callTrace.Count - 1
            callTrace(i) = settings.algorithm + "\" + callTrace(i)
        Next

        updateSettings()
        featureOptions.Show()
        gOptions.Show()
        Options_HistPointCloud.setupCalcHist()
        treeView.Show()
        centerRect = New cv.Rect(workRes.Width / 4, workRes.Height / 4,
                                 workRes.Width / 2, workRes.Height / 2)

        fpList.Clear()

        myStopWatch = Stopwatch.StartNew()
        optionsChanged = True
    End Sub
    Public Sub TrueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New TrueText(text, pt, picTag)
        trueData.Add(str)
    End Sub
    Public Sub setSelectedCell()
        If task.redList Is Nothing Then Exit Sub
        If task.redList.oldrclist.Count = 0 Then Exit Sub
        If ClickPoint = newPoint And task.redList.oldrclist.Count > 1 Then
            ClickPoint = task.redList.oldrclist(1).maxDist
        End If
        Dim index = task.redList.rcMap.Get(Of Byte)(ClickPoint.Y, ClickPoint.X)
        If index = 0 Then Exit Sub
        If index > 0 And index < task.redList.oldrclist.Count Then
            task.oldrcD = task.redList.oldrclist(index)
            task.color(task.oldrcD.rect).SetTo(cv.Scalar.White, task.oldrcD.mask)
        Else
            ' the 0th cell is always the upper left corner with just 1 pixel.
            If task.redList.oldrclist.Count > 1 Then task.oldrcD = task.redList.oldrclist(1)
        End If
    End Sub
    Public Sub DrawLine(dst As cv.Mat, p1 As cv.Point2f, p2 As cv.Point2f, color As cv.Scalar)
        dst.Line(p1, p2, color, lineWidth, lineType)
    End Sub
    Public Sub RunAlgorithm()
        If allOptions.titlesAdded Then
            allOptions.titlesAdded = False
            allOptions.layoutOptions(normalRequest:=True)
        End If

        updateSettings()

        If algorithm_ms.Count = 0 Then
            algorithmNames.Add("waitingForInput")
            algorithmTimes.Add(Now)
            algorithm_ms.Add(0)

            algorithmNames.Add(settings.algorithm)
            algorithmTimes.Add(Now)
            algorithm_ms.Add(0)

            algorithmStack = New Stack()
            algorithmStack.Push(0)
            algorithmStack.Push(1)
        End If

        algorithm_ms(0) += waitingForInput
        algorithmTimes(3) = Now  ' starting the main algorithm

        Dim src = task.color
        If src.Width = 0 Or task.pointCloud.Width = 0 Then Exit Sub ' camera data is not ready.

        bins2D = {task.workRes.Height, task.workRes.Width}

        ' run any universal algorithms here
        IMU_RawAcceleration = IMU_Acceleration
        IMU_RawAngularVelocity = IMU_AngularVelocity
        IMU_AlphaFilter = 0.5 '  gOptions.imu_Alpha

        grid.Run(task.color)
        imuBasics.Run(emptyMat)
        gmat.Run(emptyMat)

        If gOptions.CreateGif.Checked Then
            heartBeat = False
            optionsChanged = False
        Else
            heartBeat = heartBeat Or debugSyncUI Or optionsChanged Or mouseClickFlag
        End If

        frameHistoryCount = 3 ' default value.  Use Options_History to update this value.

        If optionsChanged Then motionMask.SetTo(255)

        rgbFilter.Run(color)
        If gOptions.UseMotionMask.Checked Then
            motionBasics.Run(gray)
            If optionsChanged Or task.frameCount < 5 Then
                motionRect = New cv.Rect(0, 0, workRes.Width, workRes.Height)
                grayStable = gray.Clone
                leftViewStable = leftView.Clone
            Else
                If motionRect.Width > 0 Then
                    gray.CopyTo(grayStable, motionMask)
                    leftView.CopyTo(leftViewStable, motionMask)
                Else
                    If task.gOptions.debugSyncUI.Checked Then
                        grayStable = gray.Clone
                        leftViewStable = leftView.Clone
                    End If
                End If
            End If
        Else
            motionMask.SetTo(255)
            motionBasics.motionList.Clear()
            grayStable = gray
            leftViewStable = leftView
            motionRect = New cv.Rect(0, 0, gray.Width, gray.Height)
        End If

        If pcMotion IsNot Nothing Then
            pcMotion.Run(emptyMat) '******* this is the gravity rotation *******
        Else
            task.pcSplit = task.pointCloud.Split
        End If

        colorizer.Run(src)

        If feat IsNot Nothing Then feat.Run(src)
        If bricks IsNot Nothing Then bricks.Run(src)

        If pixelViewerOn And PixelViewer Is Nothing Then
            PixelViewer = New Pixel_Viewer
        Else
            If pixelViewerOn = False Then PixelViewer = Nothing
        End If

        If gOptions.CreateGif.Checked Then
            If gifCreator Is Nothing Then gifCreator = New Gif_OpenCVB
            gifCreator.Run(src.Clone)
            If gifCreator.gifC.options.buildCheck.Checked Then
                gifCreator.gifC.options.buildCheck.Checked = False
                For i = 0 To gifImages.Count - 1
                    Dim fileName As New FileInfo(settings.HomeDir + "Temp/image" + Format(i, "000") + ".bmp")
                    gifImages(i).Save(fileName.FullName)
                Next

                gifImages.Clear()
                Dim dirInfo As New DirectoryInfo(settings.HomeDir + "GifBuilder\bin\Debug\net8.0\")
                Dim dirData = dirInfo.GetDirectories()
                Dim gifExe As New FileInfo(dirInfo.FullName + "GifBuilder.exe")
                If gifExe.Exists = False Then
                    MessageBox.Show("GifBuilder.exe was not found!")
                Else
                    Dim gifProcess As New Process
                    gifProcess.StartInfo.FileName = gifExe.FullName
                    gifProcess.Start()
                End If
            End If
        End If

        gravityBasics.Run(src.Clone)
        lines.Run(grayStable)
        histBinList = {histogramBins, histogramBins, histogramBins}

        Dim saveOptionsChanged = optionsChanged
        If optionsChanged And treeView IsNot Nothing Then treeView.optionsChanged = True
        If activateTaskForms Then
            If sharpGL IsNot Nothing Then sharpGL.Activate()
            treeView.Activate()
            allOptions.Activate()
            If PixelViewer IsNot Nothing Then PixelViewer.viewerForm.Activate()
            activateTaskForms = False
        End If
        If paused = False Then




            algorithmPrep = False
            MainUI_Algorithm.Run(src.Clone) ' <<<<<<<< This is where the VB algorithm runs...
            algorithmPrep = True



            labels = MainUI_Algorithm.labels
            If task.gOptions.displayDst0.Checked = False Then labels(0) = task.resolutionDetails
            If task.gOptions.displayDst1.Checked = False Then labels(1) = task.depthAndDepthRange

            Dim nextTrueData As List(Of TrueText) = MainUI_Algorithm.trueData
            trueData = New List(Of TrueText)(nextTrueData)

            firstPass = False
            heartBeatLT = False

            Dim displayObject = task.MainUI_Algorithm
            ' they could have asked to display one of the algorithms in the TreeView.
            For Each obj In activeObjects
                If obj.tracename = task.displayObjectName Then
                    displayObject = obj
                    Exit For
                End If
            Next

            postProcess(src, displayObject.dst1, displayObject.dst2, displayObject.dst3)

            dstList(0) = If(gOptions.displayDst0.Checked, Check8uC3(displayObject.dst0), color)
            dstList(1) = If(gOptions.displayDst1.Checked, Check8uC3(displayObject.dst1), depthRGB)
            dstList(2) = Check8uC3(displayObject.dst2)
            dstList(3) = Check8uC3(displayObject.dst3)

            If gOptions.ShowGrid.Checked Then dstList(2).SetTo(cv.Scalar.White, gridMask)
            If gOptions.showMotionMask.Checked Then
                For Each mIndex In motionBasics.motionList
                    dstList(0).Rectangle(gridRects(mIndex), cv.Scalar.White, lineWidth)
                Next
                dstList(0).Rectangle(motionRect, white, lineWidth)
            End If

            If gOptions.CrossHairs.Checked Then
                Gravity_Basics.showVectors(dstList(0))
                Dim lp = lineLongest
                Dim pt = New cv.Point2f((lp.pE1.X + lp.pE2.X) / 2 + 5, (lp.pE1.Y + lp.pE2.Y) / 2)
                displayObject.trueData.Add(New TrueText("Longest", pt, 0))
            End If

            If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then
                For Each dst In dstList
                    dst.Rectangle(task.drawRect, cv.Scalar.White, 1)
                Next
            End If

            ' if there were no cycles spent on this routine, then it was inactive.
            ' if any active algorithm has an index = -1, it has not been run.
            Dim index = algorithmNames.IndexOf(displayObject.traceName)
            If index = -1 Then
                displayObject.trueData.Add(New TrueText("This task is not active at this time.",
                                           New cv.Point(workRes.Width / 3, workRes.Height / 2), 2))
            End If

            trueData = New List(Of TrueText) ' (displayObject.trueData)
            displayObject.trueData.Clear()
            labels = displayObject.labels
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        allOptions.Close()
        For Each algorithm In task.activeObjects
            Dim type As Type = algorithm.GetType()
            If type.GetMethod("Close") IsNot Nothing Then
                algorithm.Close()  ' Close any unmanaged classes...
            End If
        Next

        For Each m In task.dstList
            m.Dispose()
        Next
    End Sub
End Class
