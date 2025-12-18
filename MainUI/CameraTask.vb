Imports cv = OpenCvSharp
Imports VBClasses
Namespace MainUI
    Partial Public Class MainUI
        Public camera As GenericCamera = Nothing
        Public testImage As cv.Mat
        Private Sub camSwitchAnnouncement()
            CameraSwitching.Visible = True
            CameraSwitching.Text = settings.cameraName + " starting"
            CameraSwitching.BringToFront()
            CamSwitchTimer.Enabled = True
            Application.DoEvents()
        End Sub
        Private Sub CamSwitchTimer_Tick(sender As Object, e As EventArgs) Handles CamSwitchTimer.Tick
            Application.DoEvents()
            Me.Refresh()
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
            If Task.readyForCameraInput = False Then Exit Sub

            Static frameProcessed As Boolean = True
            If frameProcessed = False Then Exit Sub
            frameProcessed = False

            Me.BeginInvoke(Sub()
                               sender.camImages.images(0).CopyTo(Task.color)
                               sender.camImages.images(1).CopyTo(Task.pointCloud)
                               sender.camImages.images(2).CopyTo(Task.leftView)
                               sender.camImages.images(3).CopyTo(Task.rightView)

                               Task.RunAlgorithm()
                               frameProcessed = True

                               Task.mouseClickFlag = False
                               Task.frameCount += 1

                               If RefreshTimer.Interval <> Task.refreshTimerTickCount Then
                                   RefreshTimer.Interval = Task.refreshTimerTickCount
                               End If
                           End Sub)
        End Sub
    End Class
End Namespace
'' Run algorithm on background thread
'Task.Run(Sub()
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