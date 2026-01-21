Imports System.Security.Cryptography
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
                    ' camera = New Camera_RS2_RGB(settings.workRes, settings.captureRes, settings.cameraName)
                Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                    camera = New Camera_ORB(settings.workRes, settings.captureRes, settings.cameraName)
                Case "Oak-3D camera", "Oak-4D camera"
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
            If task Is Nothing Then Exit Sub

            If task.readyForCameraInput = False Then Exit Sub
            Static lastPaintTime As DateTime = Now

            If camera.frameProcessed = False Then Exit Sub
            camera.frameProcessed = False

            Me.BeginInvoke(Sub()
                               Try
                                   If task Is Nothing Then Exit Sub
                                   task.testAllRunning = testAllRunning

                                   ' The testall timer will occasionally not get called after running test all overnight.
                                   ' The cause can be too many GDI or User Objects created and
                                   ' none are available for the timer.  Another cause may be too many threads and
                                   ' there are a lot of threads from OpenCVSharp (harmless, I am told.)  
                                   ' But this doevents is intended to allow any shortfall to be addressed.
                                   If testAllRunning Then If camera.cameraFrameCount Mod 10 = 0 Then Application.DoEvents()

                                   If task.cpu.algorithm_ms.Count = 0 Then task.cpu.startRun(settings.algorithm)

                                   task.cpu.algorithmTimes(1) = Now

                                   Dim elapsedWaitTicks = task.cpu.algorithmTimes(1).Ticks - task.cpu.algorithmTimes(0).Ticks
                                   Dim spanWait = New TimeSpan(elapsedWaitTicks)
                                   task.cpu.algorithm_ms(0) += spanWait.Ticks / TimeSpan.TicksPerMillisecond

                                   task.cpu.algorithmTimes(0) = task.cpu.algorithmTimes(1)  ' start time algorithm = end time wait.

                                   SyncLock camera.cameraMutex
                                       camera.color.CopyTo(task.color)
                                       camera.pointCloud.CopyTo(task.pointCloud)
                                       camera.leftView.CopyTo(task.leftView)
                                       camera.rightView.CopyTo(task.rightView)
                                   End SyncLock

                                   task.RunAlgorithm()

                                   task.cpu.algorithmTimes(1) = Now

                                   elapsedWaitTicks = task.cpu.algorithmTimes(1).Ticks - task.cpu.algorithmTimes(0).Ticks

                                   spanWait = New TimeSpan(elapsedWaitTicks)
                                   task.cpu.algorithm_ms(1) += spanWait.Ticks / TimeSpan.TicksPerMillisecond

                                   task.cpu.algorithmTimes(0) = task.cpu.algorithmTimes(1) ' start time wait = end time algorithm

                                   task.mouseClickFlag = False
                                   task.frameCount += 1

                                   elapsedWaitTicks = task.cpu.algorithmTimes(1).Ticks - lastPaintTime.Ticks
                                   spanWait = New TimeSpan(elapsedWaitTicks)
                                   Dim msSinceLastPaint = spanWait.Ticks / TimeSpan.TicksPerMillisecond

                                   Dim threshold = 1000 / task.Settings.FPSPaintTarget

                                   If msSinceLastPaint > threshold Then
                                       lastPaintTime = task.cpu.algorithmTimes(1)
                                       Dim tmp As cv.Mat
                                       For i = 0 To pics.Count - 1
                                           tmp = task.dstList(i).Clone
                                           tmp.Rectangle(task.drawRect, cv.Scalar.White, 1)
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