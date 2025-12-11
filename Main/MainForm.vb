Imports System.IO
Imports System.Text.RegularExpressions
Imports cv = OpenCvSharp
Imports VBClasses

Namespace MainForm
    Partial Public Class MainForm
        Dim isPlaying As Boolean = False
        Dim homeDir As String = ""
        Public settingsIO As jsonIO
        Dim algHistory As New List(Of String)
        Dim recentMenu() As ToolStripMenuItem
        Dim labels As List(Of Label)
        Dim pics As List(Of PictureBox)
        Public task As VBClasses.VBtask
        Dim testAllRunning As Boolean = False
        Dim closingDown As Boolean = False
        Public Sub jumpToAlgorithm()
            If AvailableAlgorithms.Items.Contains(settings.algorithm) = False Then
                AvailableAlgorithms.SelectedIndex = 0
            Else
                AvailableAlgorithms.SelectedItem = settings.algorithm
            End If
        End Sub
        Private Sub algHistory_Clicked(sender As Object, e As EventArgs)
            Dim item = TryCast(sender, ToolStripMenuItem)
            If AvailableAlgorithms.Items.Contains(item.Text) = False Then
                MessageBox.Show("That algorithm was not found" + vbCrLf + vbCrLf + "The name may have changed.")
            Else
                settings.algorithm = item.Text
                jumpToAlgorithm()
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

            ' Set the current directory to the project path (where .vbproj file is located)
            Dim projectDir As DirectoryInfo = New DirectoryInfo(Path.GetDirectoryName(projectFile))
            Directory.SetCurrentDirectory(projectDir.FullName + "/../")
            homeDir = Path.GetDirectoryName(projectDir.FullName) + "\"

            labels = New List(Of Label)({labelRGB, labelPointCloud, labelLeft, labelRight})
            pics = New List(Of PictureBox)({campicRGB, campicPointCloud, campicLeft, campicRight})

            settingsIO = New jsonIO(Path.Combine(homeDir, "Main\settings.json"))
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
            Dim algListPath = Path.Combine(CurDir(), "Data", "AvailableAlgorithms.txt")
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

            jumpToAlgorithm()
        End Sub
        Private Sub Magnifier_Click(sender As Object, e As EventArgs) Handles Magnifier.Click

        End Sub
        Private Sub AtoZ_Click(sender As Object, e As EventArgs) Handles AtoZ.Click
            Dim groupsForm As New MainAtoZ()
            groupsForm.homeDir = New DirectoryInfo(homeDir + "\Data")

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
        Private Sub campicRGB_Click(sender As Object, e As EventArgs) Handles campicRGB.Click, campicPointCloud.Click, campicLeft.Click, campicRight.Click
            If task Is Nothing Then Exit Sub
            'If task IsNot Nothing Then  if task.sharpgl IsNot Nothing Then sharpGL.Activate()
            If task IsNot Nothing Then If task.treeView IsNot Nothing Then task.treeView.Activate()
            If task IsNot Nothing Then If task.allOptions IsNot Nothing Then task.allOptions.Activate()
        End Sub
        Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
            closingDown = True
            SaveSettings()
            task = Nothing
            StopCamera()
        End Sub
        Private Sub PausePlayButton_Click(sender As Object, e As EventArgs) Handles PausePlayButton.Click
            isPlaying = Not isPlaying

            ' Load and set the appropriate image
            If PausePlayButton.Image IsNot Nothing Then PausePlayButton.Image.Dispose()

            Dim filePath = Path.Combine(homeDir + "\Main\Data", If(isPlaying, "PauseButton.png", "Run.png"))
            PausePlayButton.Image = New Bitmap(filePath)

            ' Force the button to refresh
            PausePlayButton.Invalidate()

            task = New VBClasses.VBtask()

            mainFrm = Me
            task.settings = settings
            task.main_hwnd = Me.Handle

            task.Initialize()
            task.MainUI_Algorithm = createAlgorithm(settings.algorithm)

            If isPlaying Then StartCamera() Else StopCamera()
            TreeViewTimer.Enabled = True
        End Sub
        Private Sub codeLines()
            Dim countFileInfo = New FileInfo(homeDir + "Data/AlgorithmCounts.txt")
            If countFileInfo.Exists = False Then
                MessageBox.Show("The AlgorithmCounts.txt file is missing.  Run 'UI_Generator' or rebuild all to rebuild the user interface.")
            End If
            Dim sr = New StreamReader(countFileInfo.FullName)

            Dim infoLine = sr.ReadLine
            Dim Split = Regex.Split(infoLine, "\W+")
            Dim CodeLineCount As Integer = Split(1)

            infoLine = sr.ReadLine
            Split = Regex.Split(infoLine, "\W+")
            Dim algorithmCountActive As Integer = Split(1)

            infoLine = sr.ReadLine
            Split = Regex.Split(infoLine, "\W+")
            Dim algorithmRefs = Split(3)
            Dim algorithmCount = algorithmCountActive + algorithmRefs
            sr.Close()

            Dim algList = New FileInfo(homeDir + "Data/AvailableAlgorithms.txt")
            sr = New StreamReader(algList.FullName)
            Dim lastGroup As String = "AddWeighted"
            While (1)
                Dim nextLine = sr.ReadLine
                Dim splitLine = Regex.Split(nextLine, "_")
                If splitLine(0) <> lastGroup Then
                    lastGroup = splitLine(0)
                    AvailableAlgorithms.Items.Add("") ' add a blank line between groups.
                End If
                AvailableAlgorithms.Items.Add(nextLine)
                If sr.EndOfStream Then Exit While
            End While
            sr.Close()

            Me.Text = "OpenCVB - " + Format(CodeLineCount, "###,##0") + " lines / " +
                       CStr(algorithmCountActive) + " algorithms " + CStr(algorithmRefs) + " references = " +
                       CStr(CInt(CodeLineCount / algorithmCount)) + " lines each (avg) - " + settings.cameraName

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
            For i = 0 To pics.Count - 1
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

            LoadAvailableAlgorithms()

            codeLines()
            setupAlgorithmHistory()

            StartUpTimer.Enabled = True
            Me.Show()
        End Sub
        Private Sub TreeViewTimer_Tick(sender As Object, e As EventArgs) Handles TreeViewTimer.Tick
            If task.treeView IsNot Nothing Then task.treeView.Timer2_Tick(sender, e)
        End Sub

        Private Sub TestAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
            testAllRunning = True
        End Sub
    End Class
End Namespace
