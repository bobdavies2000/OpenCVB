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

        ' Camera support
        Dim camera As CameraZED2 = Nothing
        Dim cameraThread As System.Threading.Thread = Nothing
        Dim cameraRunning As Boolean = False
        Dim currentDisplayMode As Integer = 0 ' 0=RGB, 1=Depth, 2=PointCloud, 3=Left, 4=Right
        Dim currentImage As cv.Mat = Nothing
        Dim imageLock As New Object()
        Dim displayTimer As Timer = Nothing
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
        Private Sub NewToolStripButton_Click(sender As Object, e As EventArgs) Handles PausePlayButton.Click
            ' Toggle between Play and Pause
            isPlaying = Not isPlaying
            PausePlayButton.Image = If(isPlaying, New Bitmap(CurDir() + "/Data/PauseButton.png"),
                                                  New Bitmap(CurDir() + "/Data/Run.png"))

            If isPlaying Then
                StartCamera()
            Else
                StopCamera()
            End If
        End Sub

        Private Sub StartCamera()
            If camera Is Nothing AndAlso settings IsNot Nothing Then
                Try
                    camera = New CameraZED2(settings.workRes, settings.captureRes, "StereoLabs ZED 2/2i")
                    cameraRunning = True
                    cameraThread = New System.Threading.Thread(AddressOf CameraTask)
                    cameraThread.Name = "CVB Camera Task"
                    cameraThread.Start()

                    ' Start display update timer
                    displayTimer = New Timer()
                    AddHandler displayTimer.Tick, AddressOf UpdateDisplay
                    displayTimer.Interval = 33 ' ~30 FPS
                    displayTimer.Start()
                Catch ex As Exception
                    MessageBox.Show("Failed to start camera: " + ex.Message, "Camera Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    isPlaying = False
                End Try
            End If
        End Sub

        Private Sub StopCamera()
            cameraRunning = False
            If displayTimer IsNot Nothing Then
                displayTimer.Stop()
                displayTimer.Dispose()
                displayTimer = Nothing
            End If
            If camera IsNot Nothing Then
                camera.StopCamera()
                camera = Nothing
            End If
            If cameraThread IsNot Nothing Then
                cameraThread.Join(1000)
                cameraThread = Nothing
            End If
        End Sub

        Private Sub CameraTask()
            While cameraRunning AndAlso camera IsNot Nothing
                Try
                    camera.GetNextFrame()

                    ' Update current image based on display mode
                    SyncLock imageLock
                        Select Case currentDisplayMode
                            Case 0 ' RGB
                                currentImage = camera.Color
                            Case 1 ' Depth
                                currentImage = camera.Depth
                            Case 2 ' Point Cloud
                                currentImage = camera.PointCloud
                            Case 3 ' Left
                                currentImage = camera.LeftView
                            Case 4 ' Right
                                currentImage = camera.RightView
                        End Select
                    End SyncLock
                Catch ex As Exception
                    Debug.WriteLine("Camera error: " + ex.Message)
                End Try
                System.Threading.Thread.Sleep(10)
            End While
        End Sub

        Private Sub UpdateDisplay(sender As Object, e As EventArgs)
            If currentImage IsNot Nothing AndAlso currentImage.Width > 0 Then
                Try
                    SyncLock imageLock
                        Dim displayImage = currentImage.Clone()
                        ' Convert to Bitmap for display
                        Dim bitmap = cvext.BitmapConverter.ToBitmap(displayImage)
                        If campics.Image IsNot Nothing Then
                            campics.Image.Dispose()
                        End If
                        campics.Image = bitmap
                        displayImage.Dispose()
                    End SyncLock
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

            ' Load settings and restore form position
            settings = settingsIO.Load()

            ' Load AvailableAlgorithms from text file
            LoadAvailableAlgorithms()

            PausePlayButton.PerformClick()
            PausePlayButton.Image = New Bitmap(CurDir() + "/Data/PauseButton.png")

            setupAlgorithmHistory()

            Me.Location = New Point(settings.FormLeft, settings.FormTop)
            Me.Size = New Size(settings.FormWidth, settings.FormHeight)
            campics.Left = StatusLabel.Left
            campics.Top += 10
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
            campics.Width = Me.Width - 28
            campics.Height = Me.Height - 101
        End Sub
        Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
            StopCamera()
            SaveSettings()
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

        Private Sub MainPictureBox_MouseMove(sender As Object, e As MouseEventArgs) Handles campics.MouseMove
            Dim X = CInt(e.X Mod (campics.Width / 2))
            Dim Y = CInt(e.Y Mod (campics.Height / 2))
            If lastClickPoint <> Point.Empty Then
                StatusLabel.Text = String.Format("X: {0}, Y: {1})", X, Y)
                StatusLabel.Text += String.Format(", Last click: ({0}, {1})", CInt(lastClickPoint.X Mod (campics.Width / 2)),
                                                                              CInt(lastClickPoint.Y Mod (campics.Height / 2)))
            Else
                StatusLabel.Text = String.Format("X: {0}, Y: {1}", X, Y)
            End If
        End Sub

        Private Sub campics_MouseClick(sender As Object, e As MouseEventArgs) Handles campics.MouseClick
            lastClickPoint = New Point(e.X, e.Y)

            ' Cycle through display modes on click: RGB -> Depth -> PointCloud -> Left -> Right -> RGB
            If camera IsNot Nothing Then
                currentDisplayMode = (currentDisplayMode + 1) Mod 5
                UpdateStatusLabelWithMode()
            End If
        End Sub

        Private Sub UpdateStatusLabelWithMode()
            Dim modeName As String = ""
            Select Case currentDisplayMode
                Case 0
                    modeName = "RGB"
                Case 1
                    modeName = "Depth"
                Case 2
                    modeName = "Point Cloud"
                Case 3
                    modeName = "Left"
                Case 4
                    modeName = "Right"
            End Select
            If StatusLabel.Text.Contains("X:") Then
                Dim posText = StatusLabel.Text.Substring(0, StatusLabel.Text.IndexOf("X:"))
                StatusLabel.Text = modeName + " - " + StatusLabel.Text
            Else
                StatusLabel.Text = modeName + " - " + StatusLabel.Text
            End If
        End Sub
    End Class
End Namespace

