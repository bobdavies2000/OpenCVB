Imports System.IO
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Namespace CVB
    Partial Public Class MainForm
        Dim camera As CVB_Camera = Nothing
        Dim cameraRunning As Boolean = False
        Dim dstImages As CameraImages.images
        Public dst2ready As Boolean
        Public camImages As CameraImages.images
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
            If camera Is Nothing AndAlso settings IsNot Nothing Then
                Try
                    ' Select camera based on settings.cameraName
                    Select Case settings.cameraName
                        Case "StereoLabs ZED 2/2i"
                            camera = New CVB_ZED2(settings.workRes, settings.captureRes, settings.cameraName)
                        Case "Intel(R) RealSense(TM) Depth Camera 435i", "Intel(R) RealSense(TM) Depth Camera 455"
                            camera = New CVB_RS2(settings.workRes, settings.captureRes, settings.cameraName)
                        Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                            camera = New CVB_ORB(settings.workRes, settings.captureRes, settings.cameraName)
                        Case Else
                            ' Default to ZED if camera name not recognized
                            camera = New CVB_ZED2(settings.workRes, settings.captureRes, "StereoLabs ZED 2/2i")
                    End Select
                    cameraRunning = True

                    ' Subscribe to FrameReady event
                    AddHandler camera.FrameReady, AddressOf Camera_FrameReady
                Catch ex As Exception
                    MessageBox.Show("Failed to start camera: " + ex.Message, "Camera Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    isPlaying = False
                End Try
            End If
        End Sub
        Private Sub PausePlayButton_Click(sender As Object, e As EventArgs) Handles PausePlayButton.Click
            isPlaying = Not isPlaying

            ' Load and set the appropriate image
            Try
                ' Dispose old image if it exists
                If PausePlayButton.Image IsNot Nothing Then
                    PausePlayButton.Image.Dispose()
                End If

                If isPlaying Then
                    Dim pausePath = Path.Combine(homeDir + "\CVB\Data", "PauseButton.png")
                    If File.Exists(pausePath) Then
                        PausePlayButton.Image = New Bitmap(pausePath)
                    End If
                Else
                    Dim playPath = Path.Combine(homeDir + "\CVB\Data", "Run.png")
                    If File.Exists(playPath) Then
                        PausePlayButton.Image = New Bitmap(playPath)
                    End If
                End If

                ' Force the button to refresh
                PausePlayButton.Invalidate()
                Application.DoEvents()
            Catch ex As Exception
                Debug.WriteLine("Error loading button image: " + ex.Message)
            End Try

            If isPlaying Then StartCamera() Else StopCamera()
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
        Private Sub Camera_FrameReady(sender As CVB_Camera)
            ' This event is raised from the background thread, so we need to marshal to UI thread
            If Me.InvokeRequired Then
                Me.BeginInvoke(New Action(Of CVB_Camera)(AddressOf Camera_FrameReady), sender)
                Return
            End If

            ' Now we're on the UI thread, safe to access UI elements
            If Not cameraRunning OrElse camera Is Nothing Then Return
            Try
                If camImages Is Nothing Then camImages = New CameraImages.images(settings.workRes)
                For i = 0 To 3
                    camImages.images(i) = sender.camImages.images(i)
                Next
                processImages(camImages)
            Catch ex As Exception
                Debug.WriteLine("Camera_FrameReady error: " + ex.Message)
            End Try
        End Sub
        Private Sub UpdatePictureBox(picBox As PictureBox, image As cv.Mat)
            If image IsNot Nothing AndAlso image.Width > 0 Then
                image = image.Resize(New cv.Size(settings.displayRes.Width, settings.displayRes.Height))
                Dim displayImage = image.Clone()
                Dim bitmap = cvext.BitmapConverter.ToBitmap(displayImage)
                If picBox.Image IsNot Nothing Then picBox.Image.Dispose()
                picBox.Image = bitmap
                displayImage.Dispose()
            End If
        End Sub
        Private Sub campicRGB_Paint(sender As Object, e As PaintEventArgs) Handles campicRGB.Paint
            If camera Is Nothing Then Exit Sub
            If myTask Is Nothing Then Exit Sub
            If CameraSwitching.Visible Then
                If camera.cameraFrameCount > 0 Then
                    CameraSwitching.Visible = False
                    CamSwitchTimer.Enabled = False
                End If
            End If

            If drawRect.Width > 0 And drawRect.Height > 0 Then
                For Each mat As cv.Mat In myTask.dstList
                    mat.Rectangle(drawRect, cv.Scalar.White, 1)
                Next
            End If

            Try
                For i = 0 To myTask.dstList.Count - 1
                    If i = 1 Then Continue For
                    UpdatePictureBox(pics(i), myTask.dstList(i))
                Next
            Catch ex As Exception
                Debug.WriteLine("Camera display error: " + ex.Message)
            End Try
        End Sub
    End Class
End Namespace