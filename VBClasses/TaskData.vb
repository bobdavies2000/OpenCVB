Imports PixelViewer
Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Partial Public Class AlgorithmTask
    Public Settings As jsonShared.Settings

    Public dstList(3) As Mat

    Public optionsChanged As Boolean
    Public allOptions As OptionsContainer
    Public gOptions As OptionsGlobal
    Public fOptions As OptionsFeatures
    Public treeView As TreeViewForm
    Public homeDir As String

    Public color As New Mat
    Public gray As New Mat
    Public grayOriginal As New Mat
    Public leftView As New Mat
    Public rightView As New Mat
    Public pointCloud As New Mat
    Public gravityCloud As New Mat
    Public sharpDepth As Mat
    Public sharpRGB As Mat
    Public pcSplit() As Mat
    Public depthRGB As Mat

    Public depthmask As Mat
    Public noDepthMask As Mat
    Public foregroundMask As Mat
    Public fLessMask As Mat

    Public firstPass As Boolean = True

    ' treeview data
    Public cpu As New CPUTime

    Public calibData As Object
    Public fpsAlgorithm As Single
    Public fpsCamera As Single

    Public verticalLines As Boolean

    Public workRes As Size
    Public smallRes As Size ' the recommended small resolution for this capture size.
    Public smallBrick As Integer ' brick size for smallRes resolution.
    Public rows As Integer
    Public cols As Integer
    Public captureRes As Size

    ' Global Options 
    Public DotSize As Integer
    Public lineWidth As Integer
    Public lineType As LineTypes
    Public histogramBins As Integer
    Public MaxZmeters As Single
    Public highlight As Scalar
    Public closeRequest As Boolean
    Public paletteIndex As Integer
    Public fCorrThreshold As Single
    Public FeatureSampleSize As Integer = 100
    Public clickPoint As New cv.Point ' last place where mouse was clicked.

    Public testAllRunning As Boolean
    Public main_hwnd As IntPtr

    ' color maps
    Public scalarColors(255) As Scalar
    Public vecColors(255) As Vec3b
    Public colorMapDepth As Mat
    Public colorMap As Mat
    Public colorMapBricks As Mat

    ' task algorithms - operate on every frame regardless of which algorithm is being run.
    Public filterBasics As Filter_Basics_TA
    Public foreground As Foreground_Basics_TA
    Public gravityBasics As Gravity_Basics_TA
    Public gravityMatrix As IMU_GMatrix_TA
    Public grid As Grid_Basics_TA
    Public imuBasics As IMU_Basics_TA
    Public leftRightBrightness As LeftRight_Brightness_TA
    Public lines As Line_Basics_TA
    Public motion As Motion_Basics_TA
    ' Public motionStable As StableGray_Measure
    Public fLess As FeatureLess_Basics_TA
    Public colorizer As DepthColorizer_Basics_TA
    Public stableDepth As StableDepth_Basics_TA
    Public stableGray As StableGray_Basics_TA
    Public prepCloud As Cloud_Gravity_TA
    Public heartBeats As HeartBeat_Basics_TA
    Public edges As Edge_Basics_TA

    Public motionFeatures As Point2f
    Public palette As Palette_LoadColorMap
    Public PixelViewer As Pixel_Viewer
    Public pixelViewerOn As Boolean

    Public GLRequest As Integer
    Public GLcloud As Mat
    Public GLrgb As Mat

    Public motionThreshold As Integer ' this is vital to motion detection - lower to be more sensitive, higher for less.
    Public colorDiffThreshold As Integer

    Public motionLinkType As Integer = 8

    Public featList As New List(Of List(Of Integer))
    Public fpMap As New Mat ' feature map

    Public brickD As brickData ' the currently selected gRect
    Public rcMinD As rcData ' the currently selected redCloud Cell
    Public rcD As rcDataOld ' the currently selected redCloud Cell
    Public lpD As lpData ' the currently selected line pair
    Public fpD As fpData ' the currently selected feature cv.Point.
    Public contourD As New contourData ' the currently selected contour

    Public bricksPerCol As Integer
    Public bricksPerRow As Integer

    Public gridRects As List(Of cv.Rect)
    Public gridNabeRects As New List(Of cv.Rect) ' The surrounding rect for every gridRect
    Public gridNabes As New List(Of List(Of Integer))
    Public gridMap As New Mat
    Public gridMask As New Mat
    Public gridWH As Integer ' grid width and height.
    Public gridROIclicked As Integer
    Public depthDiffMeters As Single ' bricks > than this value are depth edges - in meters

    Public lowResColor As New Mat
    Public lowResDepth As New Mat

    Public MainUI_Algorithm As Object
    Public myStopWatch As Stopwatch

    ' transformation matrix to convert cv.Point cloud to be vertical according to gravity.
    Public gMatrix As New Mat
    Public IMU_Rotation As System.Numerics.Quaternion
    Public maxDepthMask As New Mat

    Public camMotionPixels As Single ' distance in pixels that the camera has moved.
    Public camDirection As Single ' camera direction in radians.

    Public paletteRandom As Palette_RandomColors

    Public gifCreator As Gif_OpenCVB
    Public gifImages As New List(Of Bitmap)
    Public gifBuild As Boolean
    Public gifCaptureIndex As Integer

    Public frameCount As Integer = 1
    Public heartBeat As Boolean
    Public heartBeatLT As Boolean = True ' long term heartbeat - every X seconds.
    Public heartBeatCount As Integer
    Public quarterBeat As Boolean
    Public quarter(3) As Boolean
    Public midHeartBeat As Boolean
    Public almostHeartBeat As Boolean
    Public heartbeatFrame As Integer
    Public afterHeartBeatLT As Boolean
    Public afterHeartBeat As Boolean

    Public toggleOn As Boolean ' toggles on the heartbeat.

    Public pcFloor As Single ' y-value for floor...
    Public pcCeiling As Single ' y-value for ceiling...

    Public lpGravity As lpData
    Public lpHorizon As lpData
    Public longestLine As lpData

    Public IMU_Acceleration As Point3f
    Public IMU_AverageAcceleration As Point3f
    Public IMU_AngularVelocity As Point3f

    Public kalmanIMUacc As Point3f
    Public kalmanIMUvelocity As Point3f
    Public IMU_TimeStamp As Double
    Public IMU_Translation As Point3f
    Public IMU_AngularAcceleration As Point3f
    Public IMU_FrameTime As Double
    Public IMU_AlphaFilter As Single ' high pass and low pass filter of the IMU acceleration data.

    Public accRadians As Point3f  ' rotation angles around x/y/z-axis to align with gravity
    Public theta As Point3f ' velocity-filtered angles around x/y/z-axis to align with gravity
    Public verticalizeAngle As Double

    Public pitchDeg As Single
    Public yawDeg As Single
    Public rollDeg As Single

    Public pitchIMU As Single
    Public yawIMU As Single
    Public rollIMU As Single

    ' RedCloud variables
    Public channelCount As Integer = 2
    Public channelIndex As Integer = 0
    Public channels() As Integer = {0, 1}
    Public histBinList() As Integer
    Public reductionName As String = "XY Reduction"
    Public ranges() As Rangef
    Public rangesBGR() As Rangef = New Rangef() {New Rangef(0, 256), New Rangef(0, 256), New Rangef(0, 256)}
    Public rangesHSV() As Rangef = New Rangef() {New Rangef(0, 180), New Rangef(0, 256), New Rangef(0, 256)}
    Public rangesCloud() As Rangef

    Public mouseClickFlag As Boolean
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mouseDisplayPoint As cv.Point ' Mouse location in terms of the display resolution
    Public mouseMagnifyStartPoint As cv.Point ' Mouse location in terms of the display resolution
    Public mouseMagnifyEndPoint As cv.Point ' Mouse location in terms of the display resolution
    Public mouseMagnifyPicTag As Integer = -1
    Public mouseMovePoint As cv.Point ' mouse location in the workRes resolution.
    Public mousePixelValue As Vec3b
    Public mouseMovePointUpdated As Boolean

    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double

    Public centerRect As cv.Rect ' image center - potential use for motion.

    Public drawRect As cv.Rect ' filled in as the user draws on any of the images.
    Public drawRectFinal As cv.Rect ' After the mouse up
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.

    Public pixelViewerRect As cv.Rect
    Public pixelViewTag As Integer

    Public pipeName As String

    Public labels() = {"", "", "", ""}

    Public topCameraPoint As cv.Point
    Public sideCameraPoint As cv.Point

    Public hFov As Single
    Public vFov As Single

    Public mainFormLocation As cv.Rect

    Public trueData As New List(Of TrueText)

    Public OpenGLTitle As String
    Public polyCount As Integer

    Public rangesTop() As Rangef
    Public rangesSide() As Rangef
    Public channelsTop() As Integer
    Public channelsSide() As Integer
    Public bins2D() As Integer

    Public projectionThreshold As Integer ' In heatmap views, this defines what is hot in a heatmap.

    Public useXYRange As Boolean ' OpenGL applications don't need to adjust the ranges.
    Public xRange As Single
    Public yRange As Single
    Public xRangeDefault As Single
    Public yRangeDefault As Single
    Public metersPerPixel As Single
    Public OpenGL_Left As Integer
    Public OpenGL_Top As Integer
    Public depthAndDepthRange As String = ""
    Public resolutionDetails As String = ""
    Public sharpGL As SharpGLForm
    Public readyForCameraInput As Boolean
    Public maxTrueTextLines As Integer = 18
End Class
