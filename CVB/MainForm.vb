Imports System.IO
Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions

Namespace CVB
    Partial Public Class MainForm
        Dim isPlaying As Boolean = False
        Dim projectFilePath As String = ""
        Public settingsIO As jsonCVBIO
        Public settings As Json
        Const MAX_RECENT = 50
        Dim algHistory As New List(Of String)
        Dim recentMenu(MAX_RECENT - 1) As ToolStripMenuItem
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
            Dim optionsForm As New MainOptions()
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

            Dim groupsForm As New MainAtoZ()
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

            campicPointCloud.Location = New Point(picWidth, topStart + labelHeight)
            campicPointCloud.Size = New Size(picWidth + offset, picHeight)

            Dim bottomRowLabelTop As Integer = topStart + labelHeight + picHeight + rowSpacing
            labelLeft.Location = New Point(offset, bottomRowLabelTop)
            labelRight.Location = New Point(picWidth + offset, bottomRowLabelTop)
            labels.Add(labelLeft)
            labels.Add(labelRight)

            Dim bottomRowPicTop As Integer = bottomRowLabelTop + labelHeight
            campicLeft.Location = New Point(offset, bottomRowPicTop)
            campicLeft.Size = New Size(picWidth, picHeight)

            campicRight.Location = New Point(picWidth + offset, bottomRowPicTop)
            campicRight.Size = New Size(picWidth, picHeight)

            For Each lab In labels
                Dim index = labels.IndexOf(lab) + 1
                lab.Top = Choose(index, campicRGB.Top - lab.Height, campicRGB.Top - lab.Height,
                                        campicLeft.Top - lab.Height, campicLeft.Top - lab.Height)
                lab.Left = Choose(index, campicRGB.Left, campicPointCloud.Left, campicLeft.Left, campicRight.Left)
                lab.BackColor = Me.BackColor
                lab.Visible = True
            Next

            StatusLabel.Location = New Point(0, campicLeft.Top + campicLeft.Height)
            StatusLabel.Width = Me.Width
        End Sub
        Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            settings = settingsIO.Load()
            Me.Size = New Size(1297, 1100)

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
            StartUpTimer.Enabled = True
        End Sub
    End Class
End Namespace

