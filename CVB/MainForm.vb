Imports System.IO
Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions

Namespace CVB
    Partial Public Class MainForm
        Dim isPlaying As Boolean = False
        Dim projectFilePath As String = ""
        Public settingsIO As jsonCVBIO
        Dim algHistory As New List(Of String)
        Dim recentMenu() As ToolStripMenuItem
        Dim labels As List(Of Label)
        Dim pics As List(Of PictureBox)
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
            Const MAX_RECENT = 50
            If recentMenu Is Nothing Then ReDim recentMenu(MAX_RECENT - 1)
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
            labels = New List(Of Label)({labelRGB, labelPointCloud, labelLeft, labelRight})
            pics = New List(Of PictureBox)({campicRGB, campicPointCloud, campicLeft, campicRight})
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
        Private Sub OptionsButton_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
            Dim optionsForm As New MainOptions()

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
            If Not String.IsNullOrEmpty(projectFilePath) AndAlso File.Exists(projectFilePath) Then
                Dim projectDir = Path.GetDirectoryName(projectFilePath)
                ' Go up one level from CVB to get to the root (where Data folder is)
                settings.homeDirPath = Path.GetDirectoryName(projectDir)
            Else
                settings.homeDirPath = CurDir()
            End If

            Dim groupsForm As New MainAtoZ()
            groupsForm.homeDir = New DirectoryInfo(settings.homeDirPath)

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
            myTask = Nothing
            StopCamera()
        End Sub
        Private Sub SaveSettings()
            If settings IsNot Nothing AndAlso settingsIO IsNot Nothing Then
                settings.MainFormLeft = Me.Left
                settings.MainFormTop = Me.Top
                settings.MainFormWidth = Me.Width
                settings.MainFormHeight = Me.Height
                settings.algorithm = AvailableAlgorithms.Text
                settingsIO.Save(settings)
            End If
        End Sub
        Private Sub MainForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
            If settings Is Nothing Then Exit Sub
            AlgDescription.Width = Me.Width - 540

            Dim labelHeight As Integer = 18
            Dim topStart As Integer = MainToolStrip.Height
            Dim offset = 10
            Dim h As Integer = (Me.Height - StatusLabel.Height - topStart - labelHeight * 2) / 2 - 20
            Dim w As Integer = Me.Width / 2 - offset * 2
            For i = 0 To 3
                labels(i).Location = Choose(i + 1, New Point(offset, MainToolStrip.Height), New Point(w + offset, labelRGB.Top),
                                                   New Point(offset, campicRGB.Top + h), New Point(w + offset, labelLeft.Top))

                pics(i).Location = Choose(i + 1, New Point(offset, topStart + labelHeight), New Point(w + offset, campicRGB.Top),
                                                 New Point(offset, labelLeft.Top + labelHeight), New Point(w + offset, campicLeft.Top))
                pics(i).Size = New Size(w, h)
            Next

            StatusLabel.Location = New Point(offset, campicLeft.Top + h)
            StatusLabel.Width = w * 2
        End Sub
        Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            settings = settingsIO.Load()
            Me.Location = New Point(settings.MainFormLeft, settings.MainFormTop)
            Me.Size = New Size(settings.MainFormWidth, settings.MainFormHeight)

            camSwitchAnnouncement()

            ' Set the current directory to the project path (where .vbproj file is located)
            Dim projectDir As DirectoryInfo = Nothing
            If Not String.IsNullOrEmpty(projectFilePath) AndAlso File.Exists(projectFilePath) Then
                projectDir = New DirectoryInfo(Path.GetDirectoryName(projectFilePath))
                Directory.SetCurrentDirectory(projectDir.FullName)
            End If

            LoadAvailableAlgorithms()

            setupAlgorithmHistory()

            StartUpTimer.Enabled = True
        End Sub
        Private Sub MainForm_Activated(sender As Object, e As EventArgs) Handles Me.Activated
            If myTask Is Nothing Then Exit Sub
            ' if mytask.sharpgl IsNot Nothing Then sharpGL.Activate()
            'If myTask.treeView IsNot Nothing Then myTask.treeView.Activate()
            'If myTask.allOptions IsNot Nothing Then myTask.allOptions.Activate()
        End Sub
    End Class
End Namespace

