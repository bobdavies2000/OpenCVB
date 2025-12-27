Imports cv = OpenCvSharp
Imports VBClasses
Namespace MainUI
    Partial Public Class MainUI
        Public camera As GenericCamera = Nothing
        Private Sub CamSwitchTimer_Tick(sender As Object, e As EventArgs) Handles CamSwitchTimer.Tick
            MainForm_Resize(Nothing, Nothing)
        End Sub
        Private Sub StartCamera()
            ' Select camera based on settings.cameraName
            Select Case settings.cameraName
                Case "StereoLabs ZED 2/2i"
                    camera = New Camera_ZED2(settings.workRes, settings.captureRes, settings.cameraName)
                Case "Intel(R) RealSense(TM) Depth Camera 435i", "Intel(R) RealSense(TM) Depth Camera 455"
                    camera = New Camera_RS2(settings.workRes, settings.captureRes, settings.cameraName)
                Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                    camera = New Camera_ORB(settings.workRes, settings.captureRes, settings.cameraName)
                Case Else
                    MsgBox("Camera is not recognized!")
            End Select

            AddHandler camera.FrameReady, AddressOf Camera_FrameReady

            fpsTimer.Enabled = True
        End Sub
        Private Sub StopCamera()
            ' RemoveHandler camera.FrameReady, AddressOf Camera_FrameReady
            If camera Is Nothing Then Exit Sub
            camera.childStopCamera()
            camera.isCapturing = False
        End Sub
        Private Sub Camera_FrameReady(sender As GenericCamera)
            If taskAlg Is Nothing Then Exit Sub
            If taskAlg.readyForCameraInput = False Then Exit Sub
            Static lastPaintTime As DateTime = Now

            If camera.frameProcessed = False Then Exit Sub
            camera.frameProcessed = False

            Me.BeginInvoke(Sub()
                               If taskAlg Is Nothing Then Exit Sub
                               If taskAlg.cpu.algorithm_ms.Count = 0 Then taskAlg.cpu.startRun(settings.algorithm)

                               taskAlg.cpu.algorithmTimes(1) = Now

                               Dim elapsedWaitTicks = taskAlg.cpu.algorithmTimes(1).Ticks - taskAlg.cpu.algorithmTimes(0).Ticks
                               Dim spanWait = New TimeSpan(elapsedWaitTicks)
                               taskAlg.cpu.algorithm_ms(0) += spanWait.Ticks / TimeSpan.TicksPerMillisecond

                               taskAlg.cpu.algorithmTimes(0) = taskAlg.cpu.algorithmTimes(1)  ' start time algorithm = end time wait.

                               SyncLock camera.cameraMutex
                                   sender.camImages.images(0).CopyTo(taskAlg.color)
                                   sender.camImages.images(1).CopyTo(taskAlg.pointCloud)
                                   sender.camImages.images(2).CopyTo(taskAlg.leftView)
                                   sender.camImages.images(3).CopyTo(taskAlg.rightView)
                               End SyncLock

                               taskAlg.RunAlgorithm()

                               taskAlg.cpu.algorithmTimes(1) = Now

                               elapsedWaitTicks = taskAlg.cpu.algorithmTimes(1).Ticks - taskAlg.cpu.algorithmTimes(0).Ticks

                               spanWait = New TimeSpan(elapsedWaitTicks)
                               taskAlg.cpu.algorithm_ms(1) += spanWait.Ticks / TimeSpan.TicksPerMillisecond

                               taskAlg.cpu.algorithmTimes(0) = taskAlg.cpu.algorithmTimes(1) ' start time wait = end time algorithm

                               taskAlg.mouseClickFlag = False
                               taskAlg.frameCount += 1

                               elapsedWaitTicks = taskAlg.cpu.algorithmTimes(1).Ticks - lastPaintTime.Ticks
                               spanWait = New TimeSpan(elapsedWaitTicks)
                               Dim msSinceLastPaint = spanWait.Ticks / TimeSpan.TicksPerMillisecond

                               Dim threshold = 1000 / taskAlg.Settings.FPSPaintTarget

                               If msSinceLastPaint > threshold Then
                                   lastPaintTime = taskAlg.cpu.algorithmTimes(1)
                                   For i = 0 To pics.Count - 1
                                       pics(i).Invalidate()
                                   Next
                               End If
                               camera.frameProcessed = True
                           End Sub)
        End Sub
    End Class
End Namespace
'' Run algorithm on background thread
'taskAlg.Run(Sub()
'             Try
'                 ' Run algorithm on background thread
'                 taskAlg.RunAlgorithm()

'                 ' Update UI on UI thread after algorithm completes
'                 Me.BeginInvoke(Sub()
'                                    Try
'                                        taskAlg.mouseClickFlag = False
'                                        taskAlg.frameCount += 1

'                                        If RefreshTimer.Interval <> taskAlg.refreshTimerTickCount Then
'                                            RefreshTimer.Interval = taskAlg.refreshTimerTickCount
'                                        End If
'                                    Catch ex As Exception
'                                        Debug.WriteLine("Error updating UI after algorithm: " + ex.Message)
'                                    Finally
'                                        Interlocked.Exchange(algorithmTaskRunning, 0)
'                                    End Try
'                                End Sub)
'             Catch ex As Exception
'                 Debug.WriteLine("Error running algorithm: " + ex.Message)
'                 Me.BeginInvoke(Sub() Interlocked.Exchange(algorithmTaskRunning, 0))
'             End Try
'         End Sub)