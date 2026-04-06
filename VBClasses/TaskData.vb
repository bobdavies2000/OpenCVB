Imports PixelViewer
Imports VBClasses
Imports cv = OpenCvSharp
Partial Public Class AlgorithmTask
    Public Settings As jsonShared.Settings

    Public dstList(3) As cv.Mat

    Public optionsChanged As Boolean
    Public allOptions As OptionsContainer
    Public gOptions As OptionsGlobal
    Public fOptions As OptionsFeatures
    Public treeView As TreeViewForm
    Public homeDir As String

    Public color As New cv.Mat
    Public gray As New cv.Mat
    Public grayStable As New cv.Mat
    Public leftView As New cv.Mat
    Public rightView As New cv.Mat
    Public pointCloud As New cv.Mat
    Public gravityCloud As New cv.Mat
    Public sharpDepth As cv.Mat
    Public sharpRGB As cv.Mat
    Public pcSplit() As cv.Mat
    Public depthRGB As cv.Mat

    Public depthmask As cv.Mat
    Public noDepthMask As cv.Mat
    Public fLessMask As cv.Mat

    Public firstPass As Boolean = True

    ' treeview data
    Public cpu As New CPUTime

    Public calibData As Object
    Public fpsAlgorithm As Single
    Public fpsCamera As Single

    Public verticalLines As Boolean
    Public edgeMethod As String

    Public workRes As cv.Size
    Public smallRes As cv.Size ' the recommended small resolution for this capture size.
    Public smallBrick As Integer ' brick size for smallRes resolution.
    Public rows As Integer
    Public cols As Integer
    Public captureRes As cv.Size

    ' Global Options 
    Public DotSize As Integer
    Public lineWidth As Integer
    Public cvFontThickness As Integer
    Public brickEdgeLen As Integer
    Public cvFontSize As Single
    Public lineType As cv.LineTypes
    Public histogramBins As Integer
    Public MaxZmeters As Single
    Public reduction As Integer
    Public highlight As cv.Scalar
    Public closeRequest As Boolean
    Public paletteIndex As Integer
    Public fCorrThreshold As Single
    Public FeatureSampleSize As Integer
    Public clickPoint As New cv.Point ' last place where mouse was clicked.

    Public testAllRunning As Boolean
    Public main_hwnd As IntPtr

    ' color maps
    Public scalarColors(255) As cv.Scalar
    Public vecColors(255) As cv.Vec3b
    Public colorMapDepth As cv.Mat
    Public colorMap As cv.Mat
    Public colorMapBricks As cv.Mat

    ' task algorithms - operate on every frame regardless of which algorithm is being run.
    Public colorizer As DepthColorizer_Basics_TA
    Public filterBasics As Filter_Basics_TA
    Public gravityBasics As Gravity_Basics_TA
    Public gravityMatrix As IMU_GMatrix_TA
    Public grid As Grid_Basics_TA
    Public imuBasics As IMU_Basics_TA
    Public leftRightBrightness As LeftRight_Brightness_TA
    Public lines As Line_Basics_TA
    Public motion As Motion_Basics_TA
    Public stabilizeDepth As StableDepth_Basics
    Public cloudGravity As Cloud_Gravity_TA

    Public motionFeatures As cv.Point2f
    Public palette As Palette_LoadColorMap
    Public PixelViewer As Pixel_Viewer
    Public pixelViewerOn As Boolean
    Public redList As RedMask_List ' only referenced by XO algorithms so not really used.

    Public GLRequest As Integer
    Public GLcloud As cv.Mat
    Public GLrgb As cv.Mat

    Public motionThreshold As Integer ' this is vital to motion detection - lower to be more sensitive, higher for less.
    Public colorDiffThreshold As Integer

    Public motionLinkType As Integer = 8

    Public feat As Feature_Basics

    Public fpList As New List(Of fpData)
    Public featList As New List(Of List(Of Integer))
    Public fpMap As New cv.Mat ' feature map

    Public brickD As brickData ' the currently selected gRect
    Public rcD As New rcData ' the currently selected redCloud Cell
    Public lpD As New lpData ' the currently selected line pair
    Public fpD As New fpData ' the currently selected feature point.
    Public contourD As New contourData ' the currently selected contour

    Public bricksPerCol As Integer
    Public bricksPerRow As Integer

    Public gridRects As List(Of cv.Rect)
    Public gridNabeRects As New List(Of cv.Rect) ' The surrounding rect for every gridRect
    Public gridMap As New cv.Mat
    Public gridMask As New cv.Mat
    Public gridROIclicked As Integer
    Public depthDiffMeters As Single ' bricks > than this value are depth edges - in meters
    Public gridRatioX As Single ' translate from display width to workres to find grid element.
    Public gridRatioY As Single ' translate from display height to workres to find grid element.

    Public features As New List(Of cv.Point2f)
    Public fpFromGridCell As New List(Of Integer)
    Public fpFromGridCellLast As New List(Of Integer)
    Public fpLastList As New List(Of fpData)
    Public featurePoints As New List(Of cv.Point)

    Public flessBoundary As New cv.Mat
    Public lowResColor As New cv.Mat
    Public lowResDepth As New cv.Mat

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
    Public msWatch As Integer
    Public msLast As Integer

    Public toggleOn As Boolean ' toggles on the heartbeat.

    Public pcFloor As Single ' y-value for floor...
    Public pcCeiling As Single ' y-value for ceiling...

    Public lpGravity As New lpData
    Public lpHorizon As New lpData
    Public lineLongestChanged As Boolean
    Public angleThreshold = 2

    Public IMU_Acceleration As cv.Point3f
    Public IMU_AverageAcceleration As cv.Point3f
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
    Public reductionName As String = "XY Reduction"
    Public ranges() As cv.Rangef
    Public rangesBGR() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 256), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
    Public rangesHSV() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 180), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
    Public rangesCloud() As cv.Rangef

    Public mouseClickFlag As Boolean
    Public activateTaskForms As Boolean
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mouseDisplayPoint As cv.Point ' Mouse location in terms of the display resolution
    Public mouseMagnifyStartPoint As cv.Point ' Mouse location in terms of the display resolution
    Public mouseMagnifyEndPoint As cv.Point ' Mouse location in terms of the display resolution
    Public mouseMagnifyPicTag As Integer = -1
    Public mouseMovePoint As cv.Point ' mouse location in the workRes resolution.
    Public mouseMovePointUpdated As Boolean

    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double

    Public centerRect As cv.Rect ' image center - potential use for motion.

    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
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

    Public rangesTop() As cv.Rangef
    Public rangesSide() As cv.Rangef
    Public channelsTop() As Integer
    Public channelsSide() As Integer
    Public bins2D() As Integer
    Public frameHistoryCount As Integer ' count of how much history to use for the point cloud.

    Public projectionThreshold As Integer ' In heatmap views, this defines what is hot in a heatmap.

    Public useXYRange As Boolean ' OpenGL applications don't need to adjust the ranges.
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
    Public sharpGL As SharpGLForm
    Public readyForCameraInput As Boolean
    Public maxTrueTextLines As Integer = 18
End Class