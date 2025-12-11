Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Namespace MainForm
    Partial Public Class MainForm
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
        Private Sub StartUpTimer_Tick(sender As Object, e As EventArgs) Handles StartUpTimer.Tick
            StartUpTimer.Enabled = False
            PausePlayButton.PerformClick()
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

            Cloud_Basics.ppx = mainfrm.camera.calibData.rgbIntrinsics.ppx
            Cloud_Basics.ppy = mainfrm.camera.calibData.rgbIntrinsics.ppy
            Cloud_Basics.fx = mainfrm.camera.calibData.rgbIntrinsics.fx
            Cloud_Basics.fy = mainfrm.camera.calibData.rgbIntrinsics.fy

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
        Private Sub Camera_FrameReady(sender As GenericCamera)
            ' This event is raised from the background thread, so we need to marshal to UI thread
            Me.Invoke(Sub()
                          sender.camImages.images(0).CopyTo(task.color)
                          sender.camImages.images(1).CopyTo(task.pointCloud)
                          sender.camImages.images(2).CopyTo(task.leftView)
                          sender.camImages.images(3).CopyTo(task.rightView)

                          task.RunAlgorithm()
                          For i = 0 To task.dstList.Count - 1
                              UpdatePictureBox(pics(i), task.dstList(i))
                          Next
                      End Sub)
        End Sub
        Private Sub UpdatePictureBox(picBox As PictureBox, image As cv.Mat)
            If task Is Nothing Then Exit Sub
            Dim displayImage = image.Clone()
            If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then
                displayImage.Rectangle(task.drawRect, cv.Scalar.White, 1)
            End If

            displayImage = displayImage.Resize(New cv.Size(settings.displayRes.Width, settings.displayRes.Height))
            Dim bitmap = cvext.BitmapConverter.ToBitmap(displayImage)
            picBox.Image?.Dispose()
            picBox.Image = bitmap
            displayImage.Dispose()
        End Sub
        Private Sub campicRGB_Paint(sender As Object, e As PaintEventArgs) Handles campicRGB.Paint
            'If task Is Nothing Then Exit Sub
            'For i = 0 To task.dstList.Count - 1
            '    UpdatePictureBox(pics(i), task.dstList(i))
            'Next
        End Sub
    End Class
End Namespace
