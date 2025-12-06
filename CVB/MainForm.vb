Imports System.IO
Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions

Namespace CVB
    Public Class MainForm : Inherits Form
        Dim isPlaying As Boolean = False
        Dim projectFilePath As String = ""
        Public settingsIO As jsonCVBIO
        Dim settings As jsonCVB
        Dim lastClickPoint As Point = Point.Empty
        Const MAX_RECENT = 50
        Dim algHistory As New List(Of String)
        Dim recentMenu(MAX_RECENT - 1) As ToolStripMenuItem

        Dim camera As CVB_Camera = Nothing
        Dim cameraRunning As Boolean = False
        Dim dstImages As CameraImages.images
        Dim camPics As New List(Of PictureBox)
        Dim labels As New List(Of Label)
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
        Private Sub CamSwitchTimer_Tick(sender As Object, e As EventArgs) Handles CamSwitchTimer.Tick
            Me.Refresh()
        End Sub
        Public Sub jumpToAlgorithm(algName As String)
            If AvailableAlgorithms.Items.Contains(algName) = False Then
                AvailableAlgorithms.SelectedIndex = 0
            Else
                AvailableAlgorithms.SelectedItem = algName
            End If
        End Sub
        Private Sub algHistory_Clicked(sender As Object, e As EventArgs)
            Dim item = TryCast(sender, ToolStripMenuItem)
            If AvailableAlgorithms.Items.Contains(item.Text) = False Then
                MessageBox.Show("That algorithm was not found" + vbCrLf + vbCrLf + "The name may have changed.")
            Else
                jumpToAlgorithm(item.Text)
            End If
        End Sub
        Public Sub setupAlgorithmHistory()
            For i = 0 To MAX_RECENT - 1
                Dim nextA = GetSetting("OpenCVB", "algHistory" + CStr(i), "algHistory" + CStr(i), "recent algorithm " + CStr(i))
                If nextA = "" Then Exit For
                If algHistory.Contains(nextA) = False Then
                    algHistory.Add(nextA)
                    RecentList.DropDownItems.Add(nextA)
                    AddHandler RecentList.DropDownItems(RecentList.DropDownItems.Count - 1).Click,
                               AddressOf algHistory_Clicked
                End If
            Next
        End Sub
        Public Sub New(Optional projectFile As String = "")
            InitializeComponent()
            projectFilePath = projectFile

            ' Initialize settings IO
            Dim settingsPath As String
            If Not String.IsNullOrEmpty(projectFile) AndAlso File.Exists(projectFile) Then
                Dim projectDir = Path.GetDirectoryName(projectFile)
                settingsPath = Path.Combine(projectDir, "settings.json")
            Else
                ' Fallback to application directory
                Dim appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                settingsPath = Path.Combine(appDir, "settings.json")
            End If
            settingsIO = New jsonCVBIO(settingsPath)
        End Sub
        Private Sub SettingsToolStripButton_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
            Dim optionsForm As New CVBOptions()
            optionsForm.settings = settings
            optionsForm.cameraNames = Common.cameraNames

            optionsForm.MainOptions_Load(sender, e)
            optionsForm.cameraRadioButton(settings.cameraIndex).Checked = True
            Dim resStr = CStr(settings.workRes.Width) + "x" + CStr(settings.workRes.Height)
            For i = 0 To optionsForm.resolutionList.Count - 1
                If optionsForm.resolutionList(i).StartsWith(resStr) Then
                    optionsForm.workResRadio(i).Checked = True
                End If
            Next

            Dim OKcancel = optionsForm.ShowDialog()

            If OKcancel = DialogResult.OK Then
                settings.cameraName = optionsForm.cameraName
                settings.cameraIndex = optionsForm.cameraIndex

                SaveSettings()

                StopCamera()
                camSwitchAnnouncement()
                Application.DoEvents()
                StartCamera()
            End If
        End Sub
        Private Sub LoadAvailableAlgorithms()
            Try
                Dim algListPath = Path.Combine(CurDir(), "..\Data", "AvailableAlgorithms.txt")
                If File.Exists(algListPath) Then
                    AvailableAlgorithms.Items.Clear()
                    Dim lines = File.ReadAllLines(algListPath)
                    Dim lastGroup = "AddWeighted"
                    For Each line In lines
                        Dim nextline = line.Trim()
                        Dim split = nextline.Split("_")
                        If split(0) <> lastGroup Then
                            AvailableAlgorithms.Items.Add(" ")
                            lastGroup = split(0)
                        End If
                        AvailableAlgorithms.Items.Add(nextline)
                    Next

                    jumpToAlgorithm(settings.algorithm)
                End If
            Catch ex As Exception
                ' If file doesn't exist or can't be read, leave combo box empty
            End Try
        End Sub
        Private Sub Magnifier_Click(sender As Object, e As EventArgs) Handles Magnifier.Click

        End Sub
        Private Sub AtoZ_Click(sender As Object, e As EventArgs) Handles AtoZ.Click
            ' Get the home directory (Data directory parent)
            Dim homeDirPath As String
            If Not String.IsNullOrEmpty(projectFilePath) AndAlso File.Exists(projectFilePath) Then
                Dim projectDir = Path.GetDirectoryName(projectFilePath)
                ' Go up one level from CVB to get to the root (where Data folder is)
                homeDirPath = Path.GetDirectoryName(projectDir)
            Else
                homeDirPath = CurDir()
            End If

            Dim groupsForm As New Groups_AtoZ()
            groupsForm.homeDir = New DirectoryInfo(homeDirPath)

            If groupsForm.ShowDialog() = DialogResult.OK AndAlso Not String.IsNullOrEmpty(groupsForm.selectedGroup) Then
                ' Find and select the first algorithm that starts with the selected group
                For Each alg In AvailableAlgorithms.Items
                    Dim algStr = alg.ToString()
                    If Not String.IsNullOrWhiteSpace(algStr) AndAlso algStr.StartsWith(groupsForm.selectedGroup) Then
                        AvailableAlgorithms.Text = algStr
                        SaveSettings()
                        Exit For
                    End If
                Next
            End If
        End Sub
        Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
            SaveSettings()
            StopCamera()
        End Sub
        Private Sub SaveSettings()
            If settings IsNot Nothing AndAlso settingsIO IsNot Nothing Then
                settings.FormLeft = Me.Left
                settings.FormTop = Me.Top
                settings.FormWidth = Me.Width
                settings.FormHeight = Me.Height
                settings.algorithm = AvailableAlgorithms.Text
                settingsIO.Save(settings)
            End If
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
            ' Toggle between Play and Pause
            isPlaying = Not isPlaying

            ' Get the correct path to the Data directory
            Dim dataDir As String
            If Not String.IsNullOrEmpty(projectFilePath) AndAlso File.Exists(projectFilePath) Then
                Dim projectDir = Path.GetDirectoryName(projectFilePath)
                dataDir = Path.Combine(projectDir, "Data")
            Else
                Dim appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                dataDir = Path.Combine(appDir, "Data")
            End If

            ' Load and set the appropriate image
            Try
                ' Dispose old image if it exists
                If PausePlayButton.Image IsNot Nothing Then
                    PausePlayButton.Image.Dispose()
                End If

                If isPlaying Then
                    Dim pausePath = Path.Combine(dataDir, "PauseButton.png")
                    If File.Exists(pausePath) Then
                        PausePlayButton.Image = New Bitmap(pausePath)
                    End If
                Else
                    Dim playPath = Path.Combine(dataDir, "Run.png")
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
                camImages.color = sender.camImages.color.Clone
                camImages.pointCloud = sender.camImages.pointCloud.Clone
                camImages.left = sender.camImages.left.Clone
                camImages.right = sender.camImages.right.Clone
                processImages(camImages)
            Catch ex As Exception
                Debug.WriteLine("Camera_FrameReady error: " + ex.Message)
            End Try
        End Sub
        Private Sub MainForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
            If settings Is Nothing Then Exit Sub

            AlgDescription.Width = Me.Width - 570
            AlgDescription.Text = "Description of the algorithm"

            ' Calculate sizes for 2x2 grid with labels
            Dim labelHeight As Integer = 18
            Dim rowSpacing As Integer = 5 ' Space between top and bottom rows for labels
            Dim topStart As Integer = MainToolStrip.Height
            Dim statusLabelTop As Integer = Me.Height - StatusLabel.Height

            Dim offset = 10
            Dim picHeight As Integer = (statusLabelTop - topStart - labelHeight * 2) / 2 - 22
            Dim availableWidth As Integer = Me.Width
            Dim picWidth As Integer = Me.Width / 2 - offset * 2
            Dim totalPicHeight As Integer = statusLabelTop - topStart - (2 * labelHeight) - rowSpacing - 40

            labelRGB.Location = New Point(offset + offset, topStart)
            labelPointCloud.Location = New Point(picWidth, topStart)
            labels.Add(labelRGB)
            labels.Add(labelPointCloud)

            campicRGB.Location = New Point(offset, topStart + labelHeight)
            campicRGB.Size = New Size(picWidth, picHeight)
            camPics.Add(campicRGB)

            campicPointCloud.Location = New Point(picWidth, topStart + labelHeight)
            campicPointCloud.Size = New Size(picWidth + offset, picHeight)
            camPics.Add(campicPointCloud)

            Dim bottomRowLabelTop As Integer = topStart + labelHeight + picHeight + rowSpacing
            labelLeft.Location = New Point(offset, bottomRowLabelTop)
            labelRight.Location = New Point(picWidth + offset, bottomRowLabelTop)
            labels.Add(labelLeft)
            labels.Add(labelRight)

            Dim bottomRowPicTop As Integer = bottomRowLabelTop + labelHeight
            campicLeft.Location = New Point(offset, bottomRowPicTop)
            campicLeft.Size = New Size(picWidth, picHeight)
            camPics.Add(campicLeft)

            campicRight.Location = New Point(picWidth + offset, bottomRowPicTop)
            campicRight.Size = New Size(picWidth, picHeight)
            camPics.Add(campicRight)

            For Each lab In labels
                Dim index = labels.IndexOf(lab) + 1
                lab.Top = Choose(index, camPics(0).Top - lab.Height, camPics(0).Top - lab.Height,
                                        camPics(2).Top - lab.Height, camPics(2).Top - lab.Height)
                lab.Left = Choose(index, camPics(0).Left, camPics(1).Left, camPics(2).Left, camPics(3).Left)
                lab.BackColor = Me.BackColor
                lab.Text = Choose(index, "RGB Image", "Point Cloud", "Left Image", "Right Image")
                lab.Visible = True
            Next

            StatusLabel.Location = New Point(0, campicLeft.Top + campicLeft.Height)
            StatusLabel.Width = Me.Width
        End Sub
        Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            settings = settingsIO.Load()
            Me.Size = New Size(1297, 1100)
            For Each pic In camPics
                pic = New PictureBox()
                pic.Size = New Size(settings.displayRes.Width, settings.displayRes.Height)
            Next

            camSwitchAnnouncement()

            ' Set the current directory to the project path (where .vbproj file is located)
            Dim projectDir As DirectoryInfo = Nothing
            If Not String.IsNullOrEmpty(projectFilePath) AndAlso File.Exists(projectFilePath) Then
                projectDir = New DirectoryInfo(Path.GetDirectoryName(projectFilePath))
                Directory.SetCurrentDirectory(projectDir.FullName)
            End If

            LoadAvailableAlgorithms()

            setupAlgorithmHistory()

            Me.Location = New Point(settings.FormLeft, settings.FormTop)
            Me.Size = New Size(settings.FormWidth, settings.FormHeight)
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
            If dstImages Is Nothing Then Exit Sub
            If CameraSwitching.Visible Then
                If camera.cameraFrameCount > 0 Then
                    CameraSwitching.Visible = False
                    CamSwitchTimer.Enabled = False
                End If
            End If

            If drawRect.Width > 0 And drawRect.Height > 0 Then
                For Each mat As cv.Mat In {dstImages.color, dstImages.pointCloud, dstImages.left, dstImages.right}
                    mat.Rectangle(drawRect, cv.Scalar.White, 1)
                Next
            End If

            Try
                UpdatePictureBox(campicRGB, dstImages.color)
                ' UpdatePictureBox(campicPointCloud, dstImages.pointCloud)
                UpdatePictureBox(campicLeft, dstImages.left)
                UpdatePictureBox(campicRight, dstImages.right)
            Catch ex As Exception
                Debug.WriteLine("Camera display error: " + ex.Message)
            End Try
        End Sub
        Private Sub processImages(camImages As CameraImages.images)
            dstImages = camImages
        End Sub
        Private Sub StartUpTimer_Tick(sender As Object, e As EventArgs) Handles StartUpTimer.Tick
            StartUpTimer.Enabled = False
            PausePlayButton.PerformClick()
        End Sub
    End Class
End Namespace

