using Cv = OpenCvSharp;
using sl;

public class CalibData
{
    public float baseline;
    public RgbIntrinsics rgbIntrinsics = new RgbIntrinsics(); // Initialize nested objects
    public float h_fov;
    public float v_fov;
}

public class RgbIntrinsics
{
    public float fx;
    public float fy;
    public float ppx;
    public float ppy;
}


public class CamZed
{
    public Cv.Mat color, rightView, leftView, pointCloud;
    public Cv.Point3f IMU_Acceleration, IMU_AngularVelocity;
    public double IMU_TimeStamp, IMU_FrameTime;

    private int captureRows;
    private int captureCols;
    private sl.Camera zed;
    private sl.InitParameters init_params; 

    // Static fields (equivalent to VB.NET's method-scoped Static for these)
    // These will be initialized once when the class is first accessed.
    private static sl.RuntimeParameters RuntimeParameters = new sl.RuntimeParameters();
    private static sl.Mat colorSL = new sl.Mat();
    private static sl.Mat rightSL = new sl.Mat();
    private static sl.Mat pointCloudSL = new sl.Mat();
    private static ulong IMU_StartTime = 0; // Use ulong for timestamps

    // Constructor
    public CamZed(Cv.Size workRes, Cv.Size captureRes, string deviceName)
    {
        init_params = new sl.InitParameters(); // Instantiate here
        init_params.sensorsRequired = true;
        init_params.depthMode = sl.DEPTH_MODE.ULTRA;
        init_params.coordinateSystem = sl.COORDINATE_SYSTEM.IMAGE;
        init_params.coordinateUnits = sl.UNIT.METER;
        init_params.cameraFPS = 0; 

        // Conditional resolution setting (C# uses 'if' instead of 'If')
        if (captureRes.Height == 720) init_params.resolution = sl.RESOLUTION.HD720;
        else if (captureRes.Height == 1080) init_params.resolution = sl.RESOLUTION.HD1080;
        else if (captureRes.Height == 1200) init_params.resolution = sl.RESOLUTION.HD1200; // Corrected to HD1200, assuming was intended
        else if (captureRes.Height == 600) init_params.resolution = sl.RESOLUTION.HDSVGA;
        else if (captureRes.Height == 376) init_params.resolution = sl.RESOLUTION.VGA;

        zed = new sl.Camera(0);
        sl.ERROR_CODE errCode = zed.Open(ref init_params);

        if (errCode != sl.ERROR_CODE.SUCCESS)
        {
            Console.WriteLine($"Error opening ZED camera: {errCode}");
            // Handle error, e.g., throw new Exception($"Failed to open ZED camera: {errCode}");
        }

        sl.CameraInformation camInfo = zed.GetCameraInformation();

        // stereolabs left camera is the RGB camera so alignment to depth and left camera is already done.
        // all we need to translate from left to right image is the baseline
        //base.calibData.baseline = camInfo.cameraConfiguration.calibrationParameters.Trans.x; 
        // C# uses .x for coordinate access
        // Dim translation = camInfo.cameraConfiguration.calibrationParameters.Trans; // Not used, so can remove

        //base.calibData.rgbIntrinsics.fx = camInfo.cameraConfiguration.calibrationParameters.LeftCam.fx;
        //base.calibData.rgbIntrinsics.fy = camInfo.cameraConfiguration.calibrationParameters.LeftCam.fy;
        //base.calibData.rgbIntrinsics.ppx = camInfo.cameraConfiguration.calibrationParameters.LeftCam.cx;
        //base.calibData.rgbIntrinsics.ppy = camInfo.cameraConfiguration.calibrationParameters.LeftCam.cy;
        //base.calibData.h_fov = camInfo.cameraConfiguration.calibrationParameters.LeftCam.hFOV;
        //base.calibData.v_fov = camInfo.cameraConfiguration.calibrationParameters.LeftCam.vFOV;

        //// Integer division in C# works like VB.NET's CInt(x / y) for positive numbers
        //int ratio = captureRes.Width / workRes.Width;
        //base.calibData.rgbIntrinsics.fx /= ratio;
        //base.calibData.rgbIntrinsics.fy /= ratio;
        //base.calibData.rgbIntrinsics.ppx /= ratio;
        //base.calibData.rgbIntrinsics.ppy /= ratio;

        sl.PositionalTrackingParameters posTrack = new sl.PositionalTrackingParameters();
        posTrack.enableAreaMemory = true; // C# uses camelCase
        zed.EnablePositionalTracking(ref posTrack);
        captureRows = captureRes.Height; // Use class-level field
        captureCols = captureRes.Width;

        color = new Cv.Mat();
        rightView = new Cv.Mat();
        leftView = new Cv.Mat();
        pointCloud = new Cv.Mat();
    }
    public void GetNextFrame(Cv.Size workRes)
    {
        sl.Mat colorSL = new sl.Mat(new sl.Resolution(captureRows, captureCols), sl.MAT_TYPE.MAT_8U_C3);
        sl.Mat rightSL = new sl.Mat(new sl.Resolution(captureRows, captureCols), sl.MAT_TYPE.MAT_8U_C3);
        sl.Mat pointCloudSL = new sl.Mat(new sl.Resolution(captureRows, captureCols), sl.MAT_TYPE.MAT_8U_C4); // Note: BGRA is 4 channels

        while (true) 
        {
            sl.ERROR_CODE rc = zed.Grab(ref RuntimeParameters);
            if (rc == sl.ERROR_CODE.SUCCESS) break; 
        }

        zed.RetrieveImage(colorSL, sl.VIEW.LEFT);
        color = Cv.Mat.FromPixelData(captureRows, captureCols, Cv.MatType.CV_8UC4, colorSL.GetPtr());
        color = color.CvtColor(Cv.ColorConversionCodes.BGRA2BGR);
        leftView = color.CvtColor(Cv.ColorConversionCodes.BGR2GRAY);

        zed.RetrieveImage(rightSL, sl.VIEW.RIGHT);
        rightView = Cv.Mat.FromPixelData(captureRows, captureCols, Cv.MatType.CV_8UC4, rightSL.GetPtr());
        rightView = rightView.CvtColor(Cv.ColorConversionCodes.BGRA2BGR);
        rightView = rightView.CvtColor(Cv.ColorConversionCodes.BGR2GRAY);

        zed.RetrieveMeasure(pointCloudSL, sl.MEASURE.XYZBGRA);
        pointCloud = Cv.Mat.FromPixelData(captureRows, captureCols, Cv.MatType.CV_32FC4, pointCloudSL.GetPtr());
        pointCloud = pointCloud.CvtColor(Cv.ColorConversionCodes.BGRA2BGR);

        sl.Pose zed_pose = new sl.Pose();
        zed.GetPosition(ref zed_pose, sl.REFERENCE_FRAME.WORLD); 
        sl.SensorsData sensordata = new sl.SensorsData();
        zed.GetSensorsData(ref sensordata, sl.TIME_REFERENCE.CURRENT);

        System.Numerics.Vector3 acc = sensordata.imu.linearAcceleration; 

        // For floating-point comparison, checking for NaN is safer than just <> 0
        if (!float.IsNaN(acc.X) && acc.X != 0 && !float.IsNaN(acc.Y) && acc.Y != 0 && !float.IsNaN(acc.Z) && acc.Z != 0)
        {
            IMU_Acceleration = new Cv.Point3f(acc.X, acc.Y, -acc.Z); // Assign to base class field
            System.Numerics.Vector3 gyro = sensordata.imu.angularVelocity;
            IMU_AngularVelocity = new Cv.Point3f(gyro.X, gyro.Y, gyro.Z) * 0.0174533f; // 'f' suffix for float literal

            if (IMU_StartTime == 0) // Initialize only once on first IMU data
            {
                IMU_StartTime = sensordata.imu.timestamp;
            }
            IMU_TimeStamp = (float)(sensordata.imu.timestamp - IMU_StartTime) / 4000000f; // crude conversion to milliseconds.
        }
    }
    public void StopCamera()
    {
        zed.Close();
    }
}