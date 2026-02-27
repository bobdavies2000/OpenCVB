Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Namespace MainApp
    Partial Public Class MainUI
        Public camera As GenericCamera = Nothing
        Private Sub StartCamera()
            ' Select camera based on settings.cameraName
            Select Case settings.cameraName
                Case "StereoLabs ZED 2/2i"
                    camera = New Camera_ZED2(settings.workRes, settings.captureRes, settings.cameraName)
                Case "Intel(R) RealSense(TM) Depth Camera 435i", "Intel(R) RealSense(TM) Depth Camera 455"
                    Dim emitterOn As Integer = 0 ' does this need to be an option in the user interface?
                    camera = New Camera_RS2(settings.workRes, settings.captureRes, settings.cameraName, emitterOn)
                Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                    camera = New Camera_ORB(settings.workRes, settings.captureRes, settings.cameraName)
                'Case "Oak-3D camera"
                '    camera = New Camera_OakD(settings.workRes, settings.captureRes, settings.cameraName)
                Case "Oak-4D camera"
                    camera = New Camera_OakD(settings.workRes, settings.captureRes, settings.cameraName)
                Case Else
                    MsgBox("Camera is not recognized!")
            End Select

            AddHandler camera.FrameReady, AddressOf Camera_FrameReady

            fpsTimer.Enabled = True
        End Sub
        Private Sub StopCamera()
            If camera Is Nothing Then Exit Sub
            ' Set isCapturing to False first to prevent new events from being raised
            camera.isCapturing = False
            ' Stop the camera thread - this will wait for any in-flight events to complete
            camera.childStopCamera()
            ' Now safe to remove the handler after thread has stopped
            RemoveHandler camera.FrameReady, AddressOf Camera_FrameReady
        End Sub
        Private Sub Camera_FrameReady(sender As GenericCamera)
            ' Check if camera is still valid (might be Nothing during shutdown)
            If camera Is Nothing OrElse Not camera.isCapturing Then Exit Sub
            If vbc.task Is Nothing Then Exit Sub

            If vbc.task.readyForCameraInput = False Then Exit Sub
            Static lastPaintTime As DateTime = Now

            If camera.frameProcessed = False Then Exit Sub
            camera.frameProcessed = False

            Me.BeginInvoke(Sub()
                               Try
                                   If vbc.task Is Nothing Then Exit Sub
                                   vbc.task.testAllRunning = testAllRunning

                                   ' The testall timer will occasionally not get called after running test all overnight.
                                   ' The cause can be too many GDI or User Objects created and
                                   ' none are available for the timer.  Another cause may be too many threads and
                                   ' there are a lot of threads from OpenCVSharp (harmless, I am told.)  
                                   ' But this doevents is intended to allow any shortfall to be addressed.
                                   If testAllRunning Then If camera.cameraFrameCount Mod 10 = 0 Then Application.DoEvents()

                                   If vbc.task.cpu.algorithm_ms.Count = 0 Then vbc.task.cpu.startRun(settings.algorithm)

                                   vbc.task.cpu.algorithmTimes(1) = Now

                                   Dim elapsedWaitTicks = vbc.task.cpu.algorithmTimes(1).Ticks - vbc.task.cpu.algorithmTimes(0).Ticks
                                   Dim spanWait = New TimeSpan(elapsedWaitTicks)
                                   vbc.task.cpu.algorithm_ms(0) += spanWait.Ticks / TimeSpan.TicksPerMillisecond

                                   vbc.task.cpu.algorithmTimes(0) = vbc.task.cpu.algorithmTimes(1)  ' start time algorithm = end time wait.

                                   SyncLock camera.cameraMutex
                                       camera.color.CopyTo(vbc.task.color)
                                       camera.pointCloud.CopyTo(vbc.task.pointCloud)
                                       camera.leftView.CopyTo(vbc.task.leftView)
                                       camera.rightView.CopyTo(vbc.task.rightView)
                                       vbc.task.IMU_Acceleration = camera.IMU_Acceleration
                                       vbc.task.IMU_FrameTime = camera.IMU_FrameTime
                                       vbc.task.IMU_AngularAcceleration = camera.IMU_AngularAcceleration
                                       vbc.task.IMU_AngularVelocity = camera.IMU_AngularVelocity
                                   End SyncLock

                                   vbc.task.RunAlgorithm()

                                   vbc.task.cpu.algorithmTimes(1) = Now

                                   elapsedWaitTicks = vbc.task.cpu.algorithmTimes(1).Ticks - vbc.task.cpu.algorithmTimes(0).Ticks

                                   spanWait = New TimeSpan(elapsedWaitTicks)
                                   vbc.task.cpu.algorithm_ms(1) += spanWait.Ticks / TimeSpan.TicksPerMillisecond

                                   vbc.task.cpu.algorithmTimes(0) = vbc.task.cpu.algorithmTimes(1) ' start time wait = end time algorithm

                                   vbc.task.mouseClickFlag = False
                                   vbc.task.frameCount += 1

                                   elapsedWaitTicks = vbc.task.cpu.algorithmTimes(1).Ticks - lastPaintTime.Ticks
                                   spanWait = New TimeSpan(elapsedWaitTicks)
                                   Dim msSinceLastPaint = spanWait.Ticks / TimeSpan.TicksPerMillisecond

                                   Dim selection = vbc.task.Settings.paintFrequency
                                   Dim runPaint As Boolean = False
                                   Select Case selection
                                       Case -2 ' paint on the heartbeat
                                           If task.heartBeat Then runPaint = True
                                       Case -1 ' paint on heartbeatLT
                                           If task.heartBeatCount Mod 5 = 0 Or task.frameCount <= 10 Then
                                               task.heartBeatCount += 1
                                               runPaint = True
                                           End If
                                       Case 0 ' stop painting altogether
                                           runPaint = False
                                       Case Else
                                           If msSinceLastPaint > 1000 / selection Then runPaint = True
                                   End Select

                                   If runPaint Then
                                       lastPaintTime = vbc.task.cpu.algorithmTimes(1)
                                       Dim tmp As cv.Mat
                                       For i = 0 To pics.Count - 1
                                           tmp = vbc.task.dstList(i).Clone
                                           tmp.Rectangle(vbc.task.drawRect, white, 1)
                                           If vbc.task.pixelViewerRect.Width > 0 Then
                                               tmp.Rectangle(vbc.task.pixelViewerRect, white, 1)
                                           End If
                                           tmp = tmp.Resize(New cv.Size(settings.displayRes.Width, settings.displayRes.Height))
                                           If pics(i).Image IsNot Nothing Then pics(i).Image.Dispose()
                                           pics(i).Image = cvext.BitmapConverter.ToBitmap(tmp)
                                       Next
                                       For i = 0 To pics.Count - 1
                                           pics(i).Invalidate()
                                       Next
                                   End If
                                   camera.frameProcessed = True

                               Catch ex As Exception
                                   Debug.WriteLine($"Exception in FrameReady callback: {ex.Message}")
                                   Debug.WriteLine($"Stack trace: {ex.StackTrace}")
                               End Try
                           End Sub)
        End Sub
    End Class
End Namespace