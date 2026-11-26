Imports System.Numerics
Imports System.Threading
Imports System.Windows.Forms.VisualStyles.VisualStyleElement
Imports Intel.RealSense
Imports OpenCvSharp.ML.DTrees
Imports cv = OpenCvSharp
Public Class GenericCamera
    Public cameraMutex = New Mutex(True, "CameraMutex")
    Public color As cv.Mat, leftView As cv.Mat, pointCloud As cv.Mat, rightView As cv.Mat, depth16u As cv.Mat, disparity As cv.Mat

    Public IMU_TimeStamp As Double
    Public IMU_Acceleration As cv.Point3f
    Public IMU_AngularAcceleration As cv.Point3f
    Public IMU_AngularVelocity As cv.Point3f

    Public IMU_FrameTime As Double
    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double
    Public cameraFrameCount As Integer
    Public baseline As Single

    Public captureRes As cv.Size
    Public workRes As cv.Size

    Public calibData As cameraInfo

    Public Event FrameReady(sender As GenericCamera)
    Public isCapturing As Boolean
    Public frameProcessed As Boolean = True
    Public captureThread As Thread = Nothing
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
            depth16u = New cv.Mat(workRes, cv.MatType.CV_16UC1, 0)
            disparity = New cv.Mat(workRes, cv.MatType.CV_8UC1, 0)
        End SyncLock
    End Sub
    Public Sub GetNextFrameCounts()
        Static myStopWatch As New System.Diagnostics.Stopwatch
        If cameraFrameCount = 0 Then myStopWatch.Start()
        CPU_TimeStamp = myStopWatch.ElapsedMilliseconds
        Static lastCPUTime As Double = CPU_TimeStamp
        CPU_FrameTime = CPU_TimeStamp - lastCPUTime
        lastCPUTime = CPU_TimeStamp

        cameraFrameCount += 1

        If cameraFrameCount Mod 10 = 0 Then GC.Collect() ' do you think this is unnecessary?  Remove it and check...
        If isCapturing Then RaiseEvent FrameReady(Me)
    End Sub
    Public Sub childStopCamera()
        isCapturing = False
        Thread.Sleep(100)
        cameraFrameCount = -1
        If captureThread IsNot Nothing Then
            captureThread.Join(300)
            StopCamera()
            captureThread = Nothing
        End If
    End Sub

    Public Function ComputePointCloud(rawDepth As cv.Mat, intrinsics As intrinsicData) As cv.Mat
        ' Same unprojection as the per-pixel loop (X, Y, Z meters, zero where depth is zero).
        ' The loop held the RealSense capture thread long enough for WaitForFrames to time out.
        Dim depth As New cv.Mat
        rawDepth.ConvertTo(depth, cv.MatType.CV_32F, 0.001)

        Dim x = UnprojectAxis(rawDepth.Cols, intrinsics.ppx, intrinsics.fx, rawDepth.Rows, rawDepth.Cols, True)
        Dim y = UnprojectAxis(rawDepth.Rows, intrinsics.ppy, intrinsics.fy, rawDepth.Rows, rawDepth.Cols, False)
        cv.Cv2.Multiply(x, depth, x)
        cv.Cv2.Multiply(y, depth, y)

        Dim pc As New cv.Mat
        cv.Cv2.Merge(New cv.Mat() {x, y, depth}, pc)

        Dim pcSplit() As cv.Mat = cv.Cv2.Split(pc)
        Dim minVal As Double, maxVal As Double
        cv.Cv2.MinMaxLoc(pcSplit(2), minVal, maxVal)
        Return pc
    End Function
    Private Function UnprojectAxis(count As Integer, principal As Single, focal As Single,
                                   rows As Integer, cols As Integer, horizontal As Boolean) As cv.Mat
        Dim line As cv.Mat
        If horizontal Then
            line = New cv.Mat(1, count, cv.MatType.CV_32F)
            For i = 0 To count - 1
                line.Set(Of Single)(0, i, CSng(i))
            Next
        Else
            line = New cv.Mat(count, 1, cv.MatType.CV_32F)
            For i = 0 To count - 1
                line.Set(Of Single)(i, 0, CSng(i))
            Next
        End If

        Dim grid As New cv.Mat
        If horizontal Then
            cv.Cv2.Repeat(line, rows, 1, grid)
        Else
            cv.Cv2.Repeat(line, 1, cols, grid)
        End If

        Using principalMat As New cv.Mat(grid.Size(), grid.Type(), New cv.Scalar(principal))
            cv.Cv2.Subtract(grid, principalMat, grid)
        End Using
        If focal <> 0 Then
            Using scale As New cv.Mat(grid.Size(), grid.Type(), New cv.Scalar(1.0 / focal))
                cv.Cv2.Multiply(grid, scale, grid)
            End Using
        End If
        Return grid
    End Function
    Public Overridable Sub StopCamera()
    End Sub
End Class
