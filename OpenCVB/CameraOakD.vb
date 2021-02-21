Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.Pipes
Imports System.IO
Structure OakIMUdata ' not working - no interface to the IMU available yet.
    Public translation As cv.Point3f
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
    Public trackerConfidence As Integer
    Public mapperConfidence As Integer
End Structure
Public Class CameraOakD
    Inherits Camera

    Dim pipeName As String
    Dim pipeImages As NamedPipeServerStream
    Dim pipeSync As NamedPipeServerStream
    Dim rgbBuffer(1) As Byte
    Dim depthBuffer(1) As Byte
    Dim depthRGBBuffer(1) As Byte
    Dim leftBuffer(1) As Byte
    Dim rightBuffer(1) As Byte
    Dim pythonReady As Boolean

    Public deviceNum As Integer
    Public cameraName As String
    Dim depth8bit As New cv.Mat()
    Dim OakProcess As Process
    Dim pipelineClosed As Boolean = True
    Public pythonApp As FileInfo
    Public pythonExeName As String
    Public Sub New()
    End Sub
    Public Function queryDeviceCount() As Integer
        Return 1
    End Function
    Public Function queryDevice(index As Integer) As String
        Return "Oak-D"
    End Function
    Public Function querySerialNumber(index As Integer) As String
        Return 0
    End Function
    Public Sub initialize(_width As Integer, _height As Integer, fps As Integer)
        width = _width
        height = _height
        deviceName = "Oak-D"

        Static PipeTaskIndex As Integer
        pipeName = "OakDImages" + CStr(PipeTaskIndex)
        PipeTaskIndex += 1
        pipeImages = New NamedPipeServerStream(pipeName, PipeDirection.In)
        pipeSync = New NamedPipeServerStream(pipeName + "in", PipeDirection.Out)

        If pythonApp.Exists Then
            OakProcess = New Process
            OakProcess.StartInfo.FileName = pythonExeName
            OakProcess.StartInfo.WorkingDirectory = pythonApp.DirectoryName
            OakProcess.StartInfo.Arguments = """" + pythonApp.Name + """" + " --Width=" + CStr(width) + " --Height=" + CStr(height) + " --pipeName=" + pipeName
            OakProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            If OakProcess.Start() = False Then
                MsgBox("The Python script for the Oak-D interface failed to start.  Review " + pythonApp.Name)
            Else
                pipeImages.WaitForConnection()
                pipeSync.WaitForConnection()
            End If
            OakProcess.PriorityClass = ProcessPriorityClass.High ' this is really just a flag so swithing algorithms doesn't kill our Oak-D python interface
        Else
            MsgBox(pythonApp.FullName + " is missing.")
        End If

        color = New cv.Mat(height, width, cv.MatType.CV_8UC3)
        RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3)
        depth16 = New cv.Mat(height, width, cv.MatType.CV_16U)
        depth8bit = New cv.Mat(height, width, cv.MatType.CV_8U)
        leftView = New cv.Mat(height, width, cv.MatType.CV_8U)
        rightView = New cv.Mat(height, width, cv.MatType.CV_8U)
        pointCloud = New cv.Mat(height, width, cv.MatType.CV_32FC3)
        pipelineClosed = False
    End Sub
    Public Sub GetNextFrame()
        SyncLock bufferLock
            If pipelineClosed Then Exit Sub
            If rgbBuffer.Length <> color.Total * color.ElemSize Then ReDim rgbBuffer(color.Total * color.ElemSize - 1)
            If depthBuffer.Length <> depth8bit.Total Then ReDim depthBuffer(depth8bit.Total - 1)
            If depthRGBBuffer.Length <> RGBDepth.Total * RGBDepth.ElemSize Then ReDim depthRGBBuffer(RGBDepth.Total * RGBDepth.ElemSize - 1)
            If leftBuffer.Length <> leftView.Total Then ReDim leftBuffer(leftView.Total - 1)
            If rightBuffer.Length <> rightView.Total Then ReDim rightBuffer(rightView.Total - 1)
            pipeImages.Read(rgbBuffer, 0, rgbBuffer.Length)
            pipeImages.Read(leftBuffer, 0, leftBuffer.Length)
            pipeImages.Read(rightBuffer, 0, rightBuffer.Length)
            pipeImages.Read(depthBuffer, 0, depthBuffer.Length)
            pipeImages.Read(depthRGBBuffer, 0, depthRGBBuffer.Length)

            Dim buff() = {CByte(frameCount Mod 255)}
            pipeSync.Write(buff, 0, 1)

            Marshal.Copy(rgbBuffer, 0, color.Data, rgbBuffer.Length)
            Marshal.Copy(leftBuffer, 0, leftView.Data, leftBuffer.Length)
            Marshal.Copy(rightBuffer, 0, rightView.Data, rightBuffer.Length)
            Marshal.Copy(depthBuffer, 0, depth8bit.Data, depthBuffer.Length)
            Marshal.Copy(depthRGBBuffer, 0, RGBDepth.Data, depthRGBBuffer.Length)

            depth8bit.ConvertTo(depth16, cv.MatType.CV_16U)
            depth16 *= 15 ' not sure what the units are but this lands approximately on the typical range for depth camera - up to 4 meters.

            cv.Cv2.Flip(leftView, leftView, cv.FlipMode.Y)
            cv.Cv2.Flip(rightView, rightView, cv.FlipMode.Y)
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End SyncLock
    End Sub
    Public Sub stopCamera()
        SyncLock bufferLock
            pipelineClosed = True
            frameCount = 0
        End SyncLock
    End Sub
End Class