Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Imports VBClasses.VBClasses
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
        Private Sub Camera_FrameReady(sender As GenericCamera)
            If algTask Is Nothing Then Exit Sub
            Static frameProcessed As Boolean = True
            If frameProcessed = False Then Exit Sub
            frameProcessed = False

            Me.BeginInvoke(Sub()
                               sender.camImages.images(0).CopyTo(algTask.color)
                               sender.camImages.images(1).CopyTo(algTask.pointCloud)
                               sender.camImages.images(2).CopyTo(algTask.leftView)
                               sender.camImages.images(3).CopyTo(algTask.rightView)

                               algTask.RunAlgorithm()
                               algTask.mouseClickFlag = False
                               algTask.frameCount += 1

                               'For i = 0 To 3
                               '    Dim displayImage = algTask.dstList(i).Resize(New cv.Size(settings.displayRes.Width, settings.displayRes.Height))
                               '    Dim bitmap = cvext.BitmapConverter.ToBitmap(displayImage)
                               '    picImages(i) = bitmap
                               'Next
                               frameProcessed = True

                               If releaseImages() Then
                                   For i = 0 To algTask.labels.Count - 1
                                       labels(i).Text = algTask.labels(i)
                                   Next
                               End If
                           End Sub)
        End Sub
    End Class
End Namespace
