Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Windows.Forms
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
        Public camSwitchCount As Integer
        Public dst2ready As Boolean
        Public camImages As CameraImages.images
        Private Sub camSwitchAnnouncement()
            CameraSwitching.Visible = True
            CameraSwitching.Text = settings.cameraName + " starting"
            CamSwitchProgress.Visible = True
            CamSwitchProgress.Left = CameraSwitching.Left
            CamSwitchProgress.Top = CameraSwitching.Top + CameraSwitching.Height
            CamSwitchProgress.Height = CameraSwitching.Height / 2
            CameraSwitching.BringToFront()
            CamSwitchProgress.BringToFront()
            CamSwitchTimer.Enabled = True
            dst2ready = False
            camSwitchCount = 0
        End Sub
        Private Sub CamSwitchTimer_Tick(sender As Object, e As EventArgs) Handles CamSwitchTimer.Tick
            Debug.WriteLine("testing")
            If CamSwitchProgress.Visible Then
                Static frames As Integer
                Dim slideCount As Integer = 10
                CamSwitchProgress.Width = CameraSwitching.Width * frames / slideCount
                If frames >= slideCount Then frames = 1
                frames += 1
                Me.Refresh()
            End If
            camSwitchCount += 1
        End Sub
        Public Sub jumpToAlgorithm(algName As String)
            If AvailableAlgorithms.Items.Contains(algName) = False Then
                AvailableAlgorithms.SelectedIndex = 0
            Else
                AvailableAlgorithms.SelectedItem = algName
            End If
            'settings.algorithm = AvailableAlgorithms.Text
            'jsonfs.write()
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
            optionsForm.ShowDialog()
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
        Private Sub PictureBox_MouseMove(sender As Object, e As MouseEventArgs) Handles campicRGB.MouseMove, campicPointCloud.MouseMove, campicLeft.MouseMove, campicRight.MouseMove
            Dim picBox = TryCast(sender, PictureBox)
            If picBox IsNot Nothing Then
                If lastClickPoint <> Point.Empty Then
                    StatusLabel.Text = String.Format("X: {0}, Y: {1}", e.X, e.Y)
                    StatusLabel.Text += String.Format(", Last click: {0}, {1}", lastClickPoint.X, lastClickPoint.Y)
                Else
                    StatusLabel.Text = String.Format("X: {0}, Y: {1}", e.X, e.Y)
                End If
            End If
        End Sub

        Private Sub PictureBox_MouseClick(sender As Object, e As MouseEventArgs) Handles campicRGB.MouseClick, campicPointCloud.MouseClick, campicLeft.MouseClick, campicRight.MouseClick
            Dim picBox = TryCast(sender, PictureBox)
            If picBox IsNot Nothing Then lastClickPoint = New Point(e.X, e.Y)
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

            'Select Case settings.displayRes.Width
            '    Case 640
            '        Me.Height = 850
            '        Me.Width = 800
            '    Case Else
            '        Me.Width = 1362
            '        Me.Height = 891
            'End Select

            AlgDescription.Width = Me.Width - 570
            AlgDescription.Text = "Description of the algorithm"

            ' Calculate sizes for 2x2 grid with labels
            Dim labelHeight As Integer = 18
            Dim rowSpacing As Integer = 5 ' Space between top and bottom rows for labels
            Dim topStart As Integer = MainToolStrip.Height
            Dim statusLabelTop As Integer = Me.Height - StatusLabel.Height

            ' Calculate available space: from toolbar to status label, accounting for labels
            ' We need: top label + top pic + spacing + bottom label + bottom pic = statusLabelTop
            ' So: topStart + labelHeight + picHeight + rowSpacing + labelHeight + picHeight = statusLabelTop
            ' Therefore: 2 * picHeight = statusLabelTop - topStart - (2 * labelHeight) - rowSpacing
            Dim picHeight As Integer = 480
            Dim availableWidth As Integer = Me.Width
            Dim picWidth As Integer = 640
            Dim totalPicHeight As Integer = statusLabelTop - topStart - (2 * labelHeight) - rowSpacing - 40

            ' Ensure all PictureBoxes are the same size
            Dim uniformPicWidth As Integer = settings.displayRes.Width
            Dim uniformPicHeight As Integer = settings.displayRes.Height

            ' Position top row labels
            labelRGB.Location = New Point(0, topStart)
            labelPointCloud.Location = New Point(uniformPicWidth, topStart)
            labels.Add(labelRGB)
            labels.Add(labelPointCloud)

            ' Position top row PictureBoxes (same size)
            campicRGB.Location = New Point(0, topStart + labelHeight)
            camPics.Add(campicRGB)

            campicPointCloud.Location = New Point(uniformPicWidth, topStart + labelHeight)
            campicPointCloud.Size = New Size(uniformPicWidth, uniformPicHeight)
            camPics.Add(campicPointCloud)

            ' Position bottom row labels (with spacing from top row)
            Dim bottomRowLabelTop As Integer = topStart + labelHeight + uniformPicHeight + rowSpacing
            labelLeft.Location = New Point(0, bottomRowLabelTop)
            labelRight.Location = New Point(uniformPicWidth, bottomRowLabelTop)
            labels.Add(labelLeft)
            labels.Add(labelRight)

            ' Position bottom row PictureBoxes (same size, ending exactly at status label top)
            Dim bottomRowPicTop As Integer = bottomRowLabelTop + labelHeight
            campicLeft.Location = New Point(0, bottomRowPicTop)
            campicLeft.Size = New Size(uniformPicWidth, uniformPicHeight)
            camPics.Add(campicLeft)

            campicRight.Location = New Point(uniformPicWidth, bottomRowPicTop)
            campicRight.Size = New Size(uniformPicWidth, uniformPicHeight)
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
                pic.Size = settings.displayRes
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
            If dstImages Is Nothing Then Exit Sub
            If CameraSwitching.Visible Then
                If camera.cameraFrameCount > 0 Then
                    CameraSwitching.Visible = False
                    CamSwitchProgress.Visible = False
                    CamSwitchTimer.Enabled = False
                End If
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

