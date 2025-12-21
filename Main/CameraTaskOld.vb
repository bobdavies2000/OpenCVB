Imports cv = OpenCvSharp
Namespace MainUI
    Partial Public Class MainUI
        Public camera As GenericCamera
        Dim dstImages As CameraImages
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

            fpsTimer.Enabled = True
        End Sub
        Private Sub StopCamera()
            'If camera Is Nothing Then Exit Sub
            'camera.childStopCamera()
            camera.isCapturing = False
            'RemoveHandler camera.FrameReady, AddressOf Camera_FrameReady
            camera = Nothing
        End Sub
        Private Function releaseImages() As Boolean
            If algTask.debugSyncUI Then
                Static lastTime As DateTime = Now
                Dim timeNow As DateTime = Now
                Dim elapsedTime = timeNow.Ticks - lastTime.Ticks
                Dim spanCopy As TimeSpan = New TimeSpan(elapsedTime)
                Dim timerInterval = spanCopy.Ticks / TimeSpan.TicksPerMillisecond

                ' adjust the debugSyncUI time here - the 1000 below is in milliseconds.
                If timerInterval < 1000 Then Return False Else lastTime = timeNow
            End If

            Return True
        End Function
    End Class
End Namespace
