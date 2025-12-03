Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO

Namespace CVB
    Public Class MainForm : Inherits Form
        Dim isPlaying As Boolean = False
        Dim projectFilePath As String = ""
        Dim settingsIO As CVBSettingsIO
        Dim settings As CVBSettings
        Const MAX_RECENT = 50
        Dim algHistory As New List(Of String)
        Dim recentMenu(MAX_RECENT - 1) As ToolStripMenuItem
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
        End Sub
        Private Sub SettingsToolStripButton_Click(sender As Object, e As EventArgs) Handles SettingsToolStripButton.Click
            MessageBox.Show("Settings button clicked!", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information)
            AlgDescription.Visible = True
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
            Me.Location = New Point(settings.FormLeft, settings.FormTop)
            Me.Size = New Size(settings.FormWidth, settings.FormHeight)

            PausePlayButton.PerformClick()
            PausePlayButton.Image = New Bitmap(CurDir() + "/Data/PauseButton.png")

            setupAlgorithmHistory()
        End Sub
        Private Sub Magnifier_Click(sender As Object, e As EventArgs) Handles Magnifier.Click

        End Sub
        Private Sub MainForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
            AlgDescription.Size = New Size(Me.Width - 570, AlgDescription.Height)
            AlgDescription.Text = "Description of the algorithm"
            SaveSettings()
        End Sub

        Private Sub MainForm_LocationChanged(sender As Object, e As EventArgs) Handles Me.LocationChanged
            SaveSettings()
        End Sub

        Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
            SaveSettings()
        End Sub

        Private Sub SaveSettings()
            If settings IsNot Nothing AndAlso settingsIO IsNot Nothing Then
                settings.FormLeft = Me.Left
                settings.FormTop = Me.Top
                settings.FormWidth = Me.Width
                settings.FormHeight = Me.Height
                settingsIO.Save(settings)
            End If
        End Sub
    End Class
End Namespace

