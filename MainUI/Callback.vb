Imports VBClasses
Imports cv = OpenCvSharp
Namespace Main
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
                Case "Oak-D camera"
                    camera = New Camera_OakD(settings.workRes, settings.captureRes, settings.cameraName)
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
            If task Is Nothing Then Exit Sub
            If task.readyForCameraInput = False Then Exit Sub
            Static lastPaintTime As DateTime = Now

            If camera.frameProcessed = False Then Exit Sub
            camera.frameProcessed = False

            Me.BeginInvoke(Sub()
                               If task Is Nothing Then Exit Sub
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
'task.Run(Sub()
'             Try
'                 ' Run algorithm on background thread
'                 task.RunAlgorithm()

'                 ' Update UI on UI thread after algorithm completes
'                 Me.BeginInvoke(Sub()
'                                    Try
'                                        task.mouseClickFlag = False
'                                        task.frameCount += 1

'                                        If RefreshTimer.Interval <> task.refreshTimerTickCount Then
'                                            RefreshTimer.Interval = task.refreshTimerTickCount
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