//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//class CS_Class
//{
//    public class CS_Task
//    {
//        public System.Timers.Timer TaskTimer = setInterval(() => { }, 1000);
//        public cppAlgorithms algoList;
//    }
//    constructor()
//    {
//        this.TaskTimer = setInterval(() => { }, 1000);
//        this.algoList = new cppAlgorithms();

//        this.algorithmObject = null;
//        this.frameCount = 0;
//        this.heartBeat = false;
//        this.quarterBeat = false;
//        this.midHeartBeat = false;
//        this.almostHeartBeat = false;
//        this.myStopWatch = null;
//        this.msWatch = 0;
//        this.msLast = 0;

//        this.toggleOnOff = false;
//        this.optionsChanged = false;
//        this.paused = false;
//        this.showAllOptions = false;

//        this.dst0 = new cv.Mat();
//        this.dst1 = new cv.Mat();
//        this.dst2 = new cv.Mat();
//        this.dst3 = new cv.Mat();

//        this.mbuf = [new InBuffer(), new InBuffer()];
//        this.mbIndex = 0;

//        this.color = new cv.Mat();
//        this.leftView = new cv.Mat();
//        this.rightView = new cv.Mat();
//        this.pointCloud = new cv.Mat();

//        this.pcSplit = [];
//        this.pcFloor = 0.0;
//        this.pcCeiling = 0.0;

//        this.debugSyncUI = false;

//        this.workingRes = new cv.Size();
//        this.resolutionRatio = 0.0;
//        this.disparityAdjustment = 0.0;

//        this.motionRect = new cv.Rect();
//        this.motionFlag = false;
//        this.motionDetected = false;

//        this.gravityVec = new PointPair();
//        this.horizonVec = new PointPair();

//        this.camMotionPixels = 0.0;
//        this.camDirection = 0.0;
//        this.cMotion = new CameraMotion_Basics();

//        this.PixelViewer = new Pixel_Viewer();
//        this.colorizer = new Depth_Colorizer_CPP();
//        this.hCloud = new History_Cloud();
//        this.motionCloud = new Motion_PointCloud();
//        this.motionColor = new Motion_Color();
//        this.motionBasics = new Motion_BasicsQuarterRes();
//        this.rgbFilter = null;

//        this.gMat = new IMU_GMatrix();
//        this.IMUBasics = new IMU_Basics();
//        this.IMU_RawAcceleration = new cv.Point3f();
//        this.IMU_Acceleration = new cv.Point3f();
//        this.IMU_AverageAcceleration = new cv.Point3f();
//        this.IMU_RawAngularVelocity = new cv.Point3f();
//        this.IMU_AngularVelocity = new cv.Point3f();
//        this.kalmanIMUacc = new cv.Point3f();
//        this.kalmanIMUvelocity = new cv.Point3f();
//        this.IMU_TimeStamp = 0.0;
//        this.IMU_Rotation = new System.Numerics.Quaternion();
//        this.IMU_Translation = new cv.Point3f();
//        this.IMU_AngularAcceleration = new cv.Point3f();
//        this.IMU_FrameTime = 0.0;
//        this.IMU_AlphaFilter = 0.0;

//        this.accRadians = new cv.Point3f();
//        this.theta = new cv.Point3f();

//        this.pitch = 0.0;
//        this.yaw = 0.0;
//        this.roll = 0.0;

//        this.gMatrix = new cv.Mat();

//        this.imuStabilityTest = new Stabilizer_VerticalIMU();
//        this.cameraStable = false;
//        this.cameraStableString = "";

//        this.noDepthMask = new cv.Mat();
//        this.depthMask = new cv.Mat();

//        this.maxDepthMask = new cv.Mat();
//        this.depthRGB = new cv.Mat();

//        this.srcThread = new cv.Mat();
//        this.recordTimings = true;

//        this.highlightColor = new cv.Scalar();
//        this.activateTaskRequest = false;

//        this.histogramBins = 0;

//        this.grid = new Grid_Basics();
//        this.gridRows = 0;
//        this.gridCols = 0;
//        this.gridIndex = [];
//        this.gridList = [];
//        this.subDivisions = [];
//        this.subDivisionCount = 9;
//        this.gridMask = new cv.Mat();
//        this.gridMap = new cv.Mat();
//        this.gridNeighbors = [];
//        this.gridROIclicked = 0;
//        this.ogl = new OpenGL_Basics();

//        this.palette = new Palette_LoadColorMap();
//        this.paletteGradient = new cv.Mat();
//        this.paletteIndex = 0;

//        this.mouseClickFlag = false;
//        this.clickPoint = new cv.Point();
//        this.mousePicTag = 0;
//        this.mouseMovePoint = new cv.Point();
//        this.mouseMovePointUpdated = false;

//        this.dotSize = 0;
//        this.lineWidth = 0;
//        this.lineType = cv.LineTypes.LINE_8;
//        this.resolutionIndex = 0;
//        this.lowRes = new cv.Size();
//        this.quarterRes = new cv.Size();
//        this.displayRes = new cv.Size();

//        this.CPU_TimeStamp = 0.0;
//        this.CPU_FrameTime = 0.0;

//        this.drawRect = new cv.Rect();
//        this.drawRectClear = false;
//        this.drawRectUpdated = false;

//        this.pixelViewerRect = new cv.Rect();
//        this.pixelViewTag = 0;

//        this.pipeName = "";

//        this.pythonPipeIn = null;
//        this.pythonPipeOut = null;
//        this.pythonTaskName = "";
//        this.pythonProcess = null;
//        this.pythonReady = false;

//        this.labels = ["", "", "", ""];
//        this.desc = "";
//        this.advice = "";
//        this.intermediateName = "";
//        this.intermediateObject = null;
//        this.activeObjects = [];
//        this.pixelViewerOn = false;

//        this.transformationMatrix = [];

//        this.scalarColors = Array(256).fill(new cv.Scalar());
//        this.vecColors = Array(256).fill(new cv.Vec3b());

//        this.topCameraPoint = new cv.Point();
//        this.sideCameraPoint = new cv.Point();

//        this.hFov = 0.0;
//        this.vFov = 0.0;
//        this.focalLength = 0.0;
//        this.baseline = 0.0;

//        this.algName = "";
//        this.cameraName = "";
//        this.calibData = new CameraInfo();
//        this.homeDir = "";
//        this.fpsRate = 0;
//        this.densityMetric = 0;
//        this.FASTthreshold = 0;

//        this.externalPythonInvocation = false;
//        this.useRecordedData = false;
//        this.testAllRunning = false;
//        this.showConsoleLog = false;

//        this.mainFormLocation = new cv.Rect();
//        this.main_hwnd = null;

//        this.trueData = [];
//        this.flowData = [];
//        this.callTrace = [];

//        this.algorithm_ms = [];
//        this.algorithmNames = [];
//        this.waitingForInput = 0.0;
//        this.inputBufferCopy = 0.0;
//        this.returnCopyTime = 0.0;
//        this.algorithmAccumulate = false;

//        this.OpenGLTitle = "";
//        this.oglRect = new cv.Rect();
//        this.polyCount = 0;

//        this.gifCreator = new Gif_OpenCVB();
//        this.gifImages = [];
//        this.gifBuild = false;
//        this.gifCaptureIndex = 0;
//    }

//    dispose()
//    {
//        clearInterval(this.TaskTimer);
//    }
//}



