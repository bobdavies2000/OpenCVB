﻿Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.Pipes
Imports System.IO
Imports System.Threading
Imports rs = Intel.RealSense
Structure PyRS2data ' not working - no interface to the IMU available yet.
    Public translation As cv.Point3f
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
    Public trackerConfidence As Integer
    Public mapperConfidence As Integer
End Structure
Public Class CameraPyRS2 : Inherits Camera
    Dim pipeName As String
    Dim pipeImages As NamedPipeServerStream
    Dim pipeSync As NamedPipeServerStream
    Dim rgbBuffer(1) As Byte
    Dim depthBuffer(1) As Byte
    Dim leftBuffer(1) As Byte
    Dim rightBuffer(1) As Byte
    Dim pointCloudBuffer(1) As Byte
    Dim pythonReady As Boolean

    Public deviceNum As Integer
    Public cameraName As String
    Dim pythonProcess As Process
    Dim pipelineClosed As Boolean = True
    Public pythonApp As FileInfo
    Public pythonExeName As String
    Public Sub New()
    End Sub
    Public Function queryDeviceCount() As Integer
        Return 1
    End Function
    Public Function queryDevice(index As Integer) As String
        Return "PyRS2"
    End Function
    Public Function querySerialNumber(index As Integer) As String
        Return 0
    End Function
    Public Sub initialize(_width As Integer, _height As Integer, fps As Integer)
        width = _width
        height = _height
        deviceName = "PyRS2"

        ' Get the extrinsics before starting up the Python pipeline rather than getting them with the Python code.
        Dim cPtr = RS2Open(width, height, deviceIndex)
        Dim extrin = RS2Extrinsics(cPtr)
        Dim extrinsics As rs.Extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(extrin) ' they are both float's
        Extrinsics_VB.rotation = extrinsics.rotation
        Extrinsics_VB.translation = extrinsics.translation
        RS2Stop(cPtr)

        pipeName = "PyRS2Images"
        pipeImages = New NamedPipeServerStream(pipeName, PipeDirection.In)
        pipeSync = New NamedPipeServerStream(pipeName + "in", PipeDirection.Out)

        If pythonApp.Exists Then
            pythonProcess = New Process
            pythonProcess.StartInfo.FileName = pythonExeName
            pythonProcess.StartInfo.WorkingDirectory = pythonApp.DirectoryName
            pythonProcess.StartInfo.Arguments = """" + pythonApp.Name + """" + " --Width=" + CStr(width) + " --Height=" + CStr(height) + " --pipeName=" + pipeName
            pythonProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            If pythonProcess.Start() = False Then
                MsgBox("The Python script for the RealSense2 Python interface failed to start.  Review " + pythonApp.Name)
            Else
                pipeImages.WaitForConnection()
                pipeSync.WaitForConnection()
            End If
            pythonProcess.PriorityClass = ProcessPriorityClass.High ' this is really just a flag so switching algorithms doesn't kill our python interface
        Else
            MsgBox(pythonApp.FullName + " is missing.")
        End If

        color = New cv.Mat(height, width, cv.MatType.CV_8UC3)
        depth16 = New cv.Mat(height, width, cv.MatType.CV_16U)
        leftView = New cv.Mat(height, width, cv.MatType.CV_8U)
        rightView = New cv.Mat(height, width, cv.MatType.CV_8U)
        pointCloud = New cv.Mat(height, width, cv.MatType.CV_32FC3)
        pipelineClosed = False
        cameraRGBDepth = False
    End Sub
    Public Sub GetNextFrame()
        If pipelineClosed Then Exit Sub
        If rgbBuffer.Length <> color.Total * color.ElemSize Then ReDim rgbBuffer(color.Total * color.ElemSize - 1)
        If depthBuffer.Length <> depth16.Total * depth16.ElemSize Then ReDim depthBuffer(depth16.Total * depth16.ElemSize - 1)
        If leftBuffer.Length <> leftView.Total Then ReDim leftBuffer(leftView.Total - 1)
        If rightBuffer.Length <> rightView.Total Then ReDim rightBuffer(rightView.Total - 1)
        If pointCloudBuffer.Length <> color.Total * 12 Then ReDim pointCloudBuffer(color.Total * 12 - 1)
        pipeImages.Read(rgbBuffer, 0, rgbBuffer.Length)
        pipeImages.Read(leftBuffer, 0, leftBuffer.Length)
        pipeImages.Read(rightBuffer, 0, rightBuffer.Length)
        pipeImages.Read(depthBuffer, 0, depthBuffer.Length)
        pipeImages.Read(pointCloudBuffer, 0, pointCloudBuffer.Length)

        Dim buff() = {CByte(frameCount Mod 255)}
        pipeSync.Write(buff, 0, 1)

        Marshal.Copy(rgbBuffer, 0, color.Data, rgbBuffer.Length)
        Marshal.Copy(leftBuffer, 0, leftView.Data, leftBuffer.Length)
        Marshal.Copy(rightBuffer, 0, rightView.Data, rightBuffer.Length)
        Marshal.Copy(depthBuffer, 0, depth16.Data, depthBuffer.Length)
        Marshal.Copy(pointCloudBuffer, 0, pointCloud.Data, pointCloudBuffer.Length)

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        If pythonProcess IsNot Nothing Then pythonProcess.Kill()
        pipelineClosed = True
    End Sub
End Class