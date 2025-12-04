Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions

Namespace CVB
    Public Class MainForm : Inherits Form
        Dim isPlaying As Boolean = False
        Dim projectFilePath As String = ""
        Public settingsIO As CVBSettingsIO
        Dim settings As CVBSettings
        Dim lastClickPoint As Point = Point.Empty
        Const MAX_RECENT = 50
        Dim algHistory As New List(Of String)
        Dim recentMenu(MAX_RECENT - 1) As ToolStripMenuItem

        Dim camera As CameraZED2 = Nothing
        Dim cameraRunning As Boolean = False
        Dim cameraTimer As Timer = Nothing
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
            settingsIO = New CVBSettingsIO(settingsPath)
        End Sub
        Private Sub PausePlayButton_Click(sender As Object, e As EventArgs) Handles PausePlayButton.Click
            ' Toggle between Play and Pause
            isPlaying = Not isPlaying
            PausePlayButton.Image = If(isPlaying, New Bitmap(CurDir() + "/Data/PauseButton.png"),
                                                  New Bitmap(CurDir() + "/Data/Run.png"))
            If isPlaying Then StartCamera() Else StopCamera()
        End Sub

        Private Sub StartCamera()
            If camera Is Nothing AndAlso settings IsNot Nothing Then
                Try
                    camera = New CameraZED2(settings.workRes, settings.captureRes, "StereoLabs ZED 2/2i")
                    cameraRunning = True

                    ' Start camera timer on UI thread
                    cameraTimer = New Timer()
                    AddHandler cameraTimer.Tick, AddressOf CameraTimer_Tick
                    cameraTimer.Interval = 33 ' ~30 FPS
                    cameraTimer.Start()
                Catch ex As Exception
                    MessageBox.Show("Failed to start camera: " + ex.Message, "Camera Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    isPlaying = False
                End Try
            End If
        End Sub

        Private Sub StopCamera()
            cameraRunning = False
            If cameraTimer IsNot Nothing Then
                cameraTimer.Stop()
                cameraTimer.Dispose()
                cameraTimer = Nothing
            End If
            If camera IsNot Nothing Then
                camera.StopCamera()
                camera = Nothing
            End If
        End Sub

        Private Sub CameraTimer_Tick(sender As Object, e As EventArgs)
            If Not cameraRunning OrElse camera Is Nothing Then
                Return
            End If

            Try
                ' Capture frame on UI thread
                camera.GetNextFrame()

                ' Update all 4 PictureBoxes using camImages from GenericCamera
                UpdatePictureBox(campicRGB, camera.camImages.color)
                UpdatePictureBox(campicPointCloud, camera.camImages.pointCloud)
                UpdatePictureBox(campicLeft, camera.camImages.left)
                UpdatePictureBox(campicRight, camera.camImages.right)
            Catch ex As Exception
                Debug.WriteLine("Camera error: " + ex.Message)
            End Try
        End Sub

        Private Sub UpdatePictureBox(picBox As PictureBox, image As cv.Mat)
            If image IsNot Nothing AndAlso image.Width > 0 Then
                Try
                    Dim displayImage = image.Clone()
                    ' Convert to Bitmap for display
                    Dim bitmap = cvext.BitmapConverter.ToBitmap(displayImage)
                    If picBox.Image IsNot Nothing Then
                        picBox.Image.Dispose()
                    End If
                    picBox.Image = bitmap
                    displayImage.Dispose()
                Catch ex As Exception
                    Debug.WriteLine("Display update error: " + ex.Message)
                End Try
            End If
        End Sub
        Private Sub SettingsToolStripButton_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
            Dim optionsForm As New CVBOptions()
            optionsForm.settings = settings
            optionsForm.cameraNames = Common.cameraNames
            optionsForm.ShowDialog()
        End Sub
        Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            ' Set the current directory to the project path (where .vbproj file is located)
            Dim projectDir As DirectoryInfo = Nothing
            If Not String.IsNullOrEmpty(projectFilePath) AndAlso File.Exists(projectFilePath) Then
                projectDir = New DirectoryInfo(Path.GetDirectoryName(projectFilePath))
                Directory.SetCurrentDirectory(projectDir.FullName)
            End If

            settings = settingsIO.Load()
            LoadAvailableAlgorithms()

            PausePlayButton.PerformClick()
            PausePlayButton.Image = New Bitmap(CurDir() + "/Data/PauseButton.png")

            setupAlgorithmHistory()

            Me.Location = New Point(settings.FormLeft, settings.FormTop)
            Me.Size = New Size(settings.FormWidth, settings.FormHeight)
            MainForm_Resize(sender, e)
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
        Private Sub MainForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
            AlgDescription.Size = New Size(Me.Width - 570, AlgDescription.Height)
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
            Dim totalPicHeight As Integer = statusLabelTop - topStart - (2 * labelHeight) - rowSpacing - 40
            Dim picHeight As Integer = totalPicHeight \ 2
            Dim availableWidth As Integer = Me.Width
            Dim picWidth As Integer = availableWidth \ 2

            ' Ensure all PictureBoxes are the same size
            Dim uniformPicWidth As Integer = picWidth
            Dim uniformPicHeight As Integer = picHeight

            ' Position top row labels
            labelRGB.Location = New Point(0, topStart)
            labelPointCloud.Location = New Point(uniformPicWidth, topStart)

            ' Position top row PictureBoxes (same size)
            campicRGB.Location = New Point(0, topStart + labelHeight)
            campicRGB.Size = New Size(uniformPicWidth, uniformPicHeight)

            campicPointCloud.Location = New Point(uniformPicWidth, topStart + labelHeight)
            campicPointCloud.Size = New Size(uniformPicWidth, uniformPicHeight)

            ' Position bottom row labels (with spacing from top row)
            Dim bottomRowLabelTop As Integer = topStart + labelHeight + uniformPicHeight + rowSpacing
            labelLeft.Location = New Point(0, bottomRowLabelTop)
            labelRight.Location = New Point(uniformPicWidth, bottomRowLabelTop)

            ' Position bottom row PictureBoxes (same size, ending exactly at status label top)
            Dim bottomRowPicTop As Integer = bottomRowLabelTop + labelHeight
            campicLeft.Location = New Point(0, bottomRowPicTop)
            campicLeft.Size = New Size(uniformPicWidth, uniformPicHeight)

            campicRight.Location = New Point(uniformPicWidth, bottomRowPicTop)
            campicRight.Size = New Size(uniformPicWidth, uniformPicHeight)

            ' Position status label at the bottom
            StatusLabel.Location = New Point(0, campicLeft.Top + campicLeft.Height)
            StatusLabel.Width = Me.Width
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
                Dim X = CInt(e.X Mod (picBox.Width / 2))
                Dim Y = CInt(e.Y Mod (picBox.Height / 2))
                If lastClickPoint <> Point.Empty Then
                    StatusLabel.Text = String.Format("X: {0}, Y: {1}", X, Y)
                    StatusLabel.Text += String.Format(", Last click: {0}, {1}", CInt(lastClickPoint.X Mod (picBox.Width \ 2)),
                                                                                 CInt(lastClickPoint.Y Mod (picBox.Height \ 2)))
                Else
                    StatusLabel.Text = String.Format("X: {0}, Y: {1}", X, Y)
                End If
            End If
        End Sub

        Private Sub PictureBox_MouseClick(sender As Object, e As MouseEventArgs) Handles campicRGB.MouseClick, campicPointCloud.MouseClick, campicLeft.MouseClick, campicRight.MouseClick
            Dim picBox = TryCast(sender, PictureBox)
            If picBox IsNot Nothing Then
                lastClickPoint = New Point(e.X, e.Y)
            End If
        End Sub
    End Class
End Namespace

