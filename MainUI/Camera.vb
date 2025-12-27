Imports System.Numerics
Imports System.Threading
Imports System.Windows.Forms.VisualStyles.VisualStyleElement
Imports Intel.RealSense
Imports cv = OpenCvSharp
Public Class CameraImages
    Public images() As cv.Mat
    Public Sub New(workRes As cv.Size)
        images = {New cv.Mat(workRes, cv.MatType.CV_8UC3, 0),
                  New cv.Mat(workRes, cv.MatType.CV_32FC3, 0),
                  New cv.Mat(workRes, cv.MatType.CV_8UC1, 0),
                  New cv.Mat(workRes, cv.MatType.CV_8UC1, 0)}
    End Sub
    Public Sub New()
    End Sub
End Class
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

Public Class GenericCamera
    Public cameraMutex = New Mutex(True, "CameraMutex")
    Public color As cv.Mat, leftView As cv.Mat, pointCloud As cv.Mat, rightView As cv.Mat
    Public transformationMatrix() As Single
    Public IMU_TimeStamp As Double
    Public IMU_Acceleration As cv.Point3f
    Public IMU_AngularAcceleration As cv.Point3f
    Public IMU_AngularVelocity As cv.Point3f
    Public IMU_FrameTime As Double
    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double
    Public cameraFrameCount As Integer
    Public baseline As Single
    Public camImages As CameraImages

    Public captureRes As cv.Size
    Public workRes As cv.Size

    Public deviceCount As Integer
    Public calibData As cameraInfo

    Public cameraName As String = ""
    Public cPtr As IntPtr
    Public ratio As Single
    Public Event FrameReady(sender As GenericCamera)
    Public isCapturing As Boolean
    Public frameProcessed As Boolean = True
    Public captureThread As Thread = Nothing
    Public Structure imuDataStruct
        Dim r00 As Single
        Dim r01 As Single
        Dim r02 As Single
        Dim tx As Single
        Dim r10 As Single
        Dim r11 As Single
        Dim r12 As Single
        Dim ty As Single
        Dim r20 As Single
        Dim r21 As Single
        Dim r22 As Single
        Dim tz As Single
        Dim m30 As Single
        Dim m31 As Single
        Dim m32 As Single
        Dim m33 As Single
    End Structure
    Structure PoseData
        Public translation As cv.Point3f
        Public velocity As cv.Point3f
        Public acceleration As cv.Point3f
        Public rotation As Quaternion
        Public angularVelocity As cv.Point3f
        Public angularAcceleration As cv.Point3f
        Public trackerConfidence As Integer
        Public mapperConfidence As Integer
    End Structure
    Public Sub New()
        cameraFrameCount = 0
        isCapturing = True
    End Sub
    Public Sub prepImages()
        SyncLock cameraMutex
            color = New cv.Mat(workRes, cv.MatType.CV_8UC3, 0)
            leftView = New cv.Mat(workRes, cv.MatType.CV_8UC1, 0)
            pointCloud = New cv.Mat(workRes, cv.MatType.CV_32FC3, 0)
            rightView = New cv.Mat(workRes, cv.MatType.CV_8UC1, 0)

            camImages = New CameraImages(workRes)
        End SyncLock
    End Sub
    Public Sub GetNextFrameCounts(frameTime As Double)
        Static lastFrameTime = IMU_TimeStamp
        Static imuStartTime = IMU_TimeStamp
        IMU_FrameTime = IMU_TimeStamp - lastFrameTime - imuStartTime
        lastFrameTime = IMU_TimeStamp - imuStartTime

        Static myStopWatch As New System.Diagnostics.Stopwatch
        If cameraFrameCount = 0 Then myStopWatch.Start()
        CPU_TimeStamp = myStopWatch.ElapsedMilliseconds
        Static lastCPUTime = CPU_TimeStamp
        CPU_FrameTime = CPU_TimeStamp - lastCPUTime
        lastCPUTime = CPU_TimeStamp

        cameraFrameCount += 1

        SyncLock cameraMutex
            camImages.images(0) = color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            camImages.images(1) = pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            camImages.images(2) = leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            camImages.images(3) = rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
        End SyncLock

        If cameraFrameCount Mod 10 = 0 Then GC.Collect() ' do you think this is unnecessary?  Remove it and check...
        RaiseEvent FrameReady(Me)
    End Sub
    Public Sub childStopCamera()
        isCapturing = False
        StopCamera()
        cameraFrameCount = -1
    End Sub
    Public Overridable Sub StopCamera()
    End Sub
End Class