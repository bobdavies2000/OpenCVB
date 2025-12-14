Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Namespace MainUI
    Partial Public Class MainUI
        Public camera As GenericCamera = Nothing
        Dim cameraRunning As Boolean = False
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
                    ' Default to ZED if camera name not recognized
                    camera = New Camera_ZED2(settings.workRes, settings.captureRes, "StereoLabs ZED 2/2i")
            End Select
            cameraRunning = True

            AddHandler camera.FrameReady, AddressOf Camera_FrameReady

            fpsTimer.Enabled = True
        End Sub
        Private Sub StopCamera()
            cameraRunning = False
            If camera IsNot Nothing Then
                RemoveHandler camera.FrameReady, AddressOf Camera_FrameReady
                camera.childStopCamera()
                camera = Nothing
            End If
        End Sub
        Private Function releaseImages() As Boolean
            task.debugDrawFlag = True
            Dim ratio = pics(0).Width / settings.workRes.Width
            If task.debugSyncUI Then
                Static lastTime As DateTime = Now
                Dim timeNow As DateTime = Now
                Dim elapsedTime = timeNow.Ticks - lastTime.Ticks
                Dim spanCopy As TimeSpan = New TimeSpan(elapsedTime)
                Dim timerInterval = spanCopy.Ticks / TimeSpan.TicksPerMillisecond
                If timerInterval < 1000 Then ' adjust the debugSyncUI time here - in milliseconds.
                    task.debugDrawFlag = False
                Else
                    lastTime = timeNow
                End If
            End If

            Return task.debugDrawFlag
        End Function
        Private Sub Camera_FrameReady(sender As GenericCamera)
            If task Is Nothing Then Exit Sub
            ' This event is raised from the background thread, so we need to marshal to UI thread
            Me.Invoke(Sub()
                          sender.camImages.images(0).CopyTo(task.color)
                          sender.camImages.images(1).CopyTo(task.pointCloud)
                          sender.camImages.images(2).CopyTo(task.leftView)
                          sender.camImages.images(3).CopyTo(task.rightView)

                          task.RunAlgorithm()

                          If releaseImages() Then
                              For i = 0 To task.dstList.Count - 1
                                  Dim displayImage = task.dstList(i).Resize(New cv.Size(settings.displayRes.Width, settings.displayRes.Height))
                                  Dim bitmap = cvext.BitmapConverter.ToBitmap(displayImage)
                                  pics(i).Image?.Dispose()
                                  pics(i).Image = bitmap
                                  displayImage.Dispose()
                              Next
                              task.mouseClickFlag = False

                              For i = 0 To task.labels.Count - 1
                                  labels(i).Text = task.labels(i)
                              Next
                              Application.DoEvents() ' task.color doesn't appear for a few seconds without this.  Why?
                          End If
                      End Sub)
        End Sub
    End Class
End Namespace
