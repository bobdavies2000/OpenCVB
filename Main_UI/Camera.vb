Imports cvb = OpenCvSharp
Imports System.Numerics
Imports VB_Classes

Public Class Camera
    Public transformationMatrix() As Single
    Public IMU_TimeStamp As Double
    Public IMU_Acceleration As cvb.Point3f
    Public IMU_AngularAcceleration As cvb.Point3f
    Public IMU_AngularVelocity As cvb.Point3f
    Public IMU_FrameTime As Double
    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double
    Public cameraFrameCount As Integer

    Public mbuf(2 - 1) As VB_Classes.VBtask.inBuffer
    Public mbIndex As Integer

    Public captureRes As cvb.Size

    Public deviceCount As Integer
    Public cameraInfo As VB_Classes.VBtask.cameraInfo
    Public colorBytes() As Byte
    Public vertices() As Byte
    Public depthBytes() As Byte
    Public leftViewBytes() As Byte
    Public rightViewBytes() As Byte
    Public pointCloudBytes() As Byte

    Public serialNumber As String
    Public deviceIndex As Integer
    Public failedImageCount As Integer
    Public modelInverse As Boolean
    Public cameraName As String = ""
    Public cPtr As IntPtr
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
        Public translation As cvb.Point3f
        Public velocity As cvb.Point3f
        Public acceleration As cvb.Point3f
        Public rotation As Quaternion
        Public angularVelocity As cvb.Point3f
        Public angularAcceleration As cvb.Point3f
        Public trackerConfidence As Integer
        Public mapperConfidence As Integer
    End Structure
    Public Sub setupMats(WorkingRes As cvb.Size)
        For i = 0 To mbuf.Count - 1
            mbuf(i).color = New cvb.Mat(WorkingRes, cvb.MatType.CV_8UC3)
            mbuf(i).leftView = New cvb.Mat(WorkingRes, cvb.MatType.CV_8UC3)
            mbuf(i).rightView = New cvb.Mat(WorkingRes, cvb.MatType.CV_8UC3)
            mbuf(i).pointCloud = New cvb.Mat(WorkingRes, cvb.MatType.CV_32FC3)
        Next
    End Sub
    Public Sub New()
        Dim cam As VBtask.cameraInfo
        cameraInfo = cam
    End Sub
    Public Function getMinMax(mat As cvb.Mat, Optional mask As cvb.Mat = Nothing) As mmData
        Dim mm As mmData
        If mask Is Nothing Then
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc)
        Else
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, mask)
        End If
        Return mm
    End Function
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
    End Sub
End Class