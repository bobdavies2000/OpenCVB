Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Namespace MainForm
    Partial Public Class MainForm
        Public camera As GenericCamera = Nothing
        Dim cameraRunning As Boolean = False
        Dim dstImages As CameraImages
        Public dst2ready As Boolean
        Public camImages As New CameraImages
        Private Sub camSwitchAnnouncement()
            CameraSwitching.Visible = True
            CameraSwitching.Text = settings.cameraName + " starting"
            CameraSwitching.BringToFront()
            CamSwitchTimer.Enabled = True
            dst2ready = False
            Application.DoEvents()
        End Sub
        Private Sub StartUpTimer_Tick(sender As Object, e As EventArgs) Handles StartUpTimer.Tick
            StartUpTimer.Enabled = False
            PausePlayButton.PerformClick()
            fpsTimer.Enabled = True
        End Sub
        Private Sub CamSwitchTimer_Tick(sender As Object, e As EventArgs) Handles CamSwitchTimer.Tick
            Me.Refresh()
        End Sub
        Private Sub StartCamera()
            camImages = New CameraImages(task.workRes)
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

            ' Subscribe to FrameReady event
            AddHandler camera.FrameReady, AddressOf Camera_FrameReady

            Cloud_Basics.ppx = mainfrm.camera.calibData.rgbIntrinsics.ppx
            Cloud_Basics.ppy = mainfrm.camera.calibData.rgbIntrinsics.ppy
            Cloud_Basics.fx = mainfrm.camera.calibData.rgbIntrinsics.fx
            Cloud_Basics.fy = mainfrm.camera.calibData.rgbIntrinsics.fy
        End Sub
        Private Sub StopCamera()
            cameraRunning = False
            If camera IsNot Nothing Then
                ' Unsubscribe from event
                RemoveHandler camera.FrameReady, AddressOf Camera_FrameReady
                camera.childStopCamera()
                camera = Nothing
            End If
        End Sub
        Private Sub Camera_FrameReady(sender As GenericCamera)
            ' This event is raised from the background thread, so we need to marshal to UI thread
            If Me.InvokeRequired Then
                Me.BeginInvoke(New Action(Of GenericCamera)(AddressOf Camera_FrameReady), sender)
                Return
            End If

            cv.Cv2.ImShow("color", sender.camImages.images(0))
            camImages = sender.camImages
            task.RunAlgorithm()
        End Sub
        Private Sub UpdatePictureBox(picBox As PictureBox, image As cv.Mat)
            If image IsNot Nothing AndAlso image.Width > 0 Then
                Dim displayImage = image.Clone()
                If drawRect.Width > 0 And drawRect.Height > 0 Then
                    displayImage.Rectangle(drawRect, cv.Scalar.White, 1)
                End If

                displayImage = displayImage.Resize(New cv.Size(settings.displayRes.Width, settings.displayRes.Height))
                Dim bitmap = cvext.BitmapConverter.ToBitmap(displayImage)
                picBox.Image = bitmap
                displayImage.Dispose()
            End If
        End Sub
        Private Sub campicRGB_Paint(sender As Object, e As PaintEventArgs) Handles campicRGB.Paint
            If camera Is Nothing Then Exit Sub
            If task Is Nothing Then Exit Sub
            If CameraSwitching.Visible Then
                If camera.cameraFrameCount > 0 Then
                    CameraSwitching.Visible = False
                    CamSwitchTimer.Enabled = False
                End If
            End If

            Try
                For i = 0 To task.dstList.Count - 1
                    UpdatePictureBox(pics(i), task.dstList(i))
                Next
            Catch ex As Exception
                Debug.WriteLine("Camera display error: " + ex.Message)
            End Try
        End Sub
    End Class
End Namespace
