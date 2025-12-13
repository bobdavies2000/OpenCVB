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
        Public Sub jumpToAlgorithm()
            If AvailableAlgorithms.Items.Contains(settings.algorithm) = False Then
                AvailableAlgorithms.SelectedIndex = 0
            Else
                AvailableAlgorithms.SelectedItem = settings.algorithm
            End If
        End Sub
        Private Sub algHistory_Clicked(sender As Object, e As EventArgs)
            Dim item = TryCast(sender, ToolStripMenuItem)
            settings.algorithm = item.Text
            jumpToAlgorithm()
        End Sub
        Public Sub setupAlgorithmHistory()
            If recentMenu Is Nothing Then ReDim recentMenu(settings.algorithmHistory.Count - 1)
            For Each alg In settings.algorithmHistory
                algHistory.Add(alg)
                If AvailableAlgorithms.Items.Contains(alg) = False Then Continue For
                RecentList.DropDownItems.Add(alg)
                AddHandler RecentList.DropDownItems(RecentList.DropDownItems.Count - 1).Click, AddressOf algHistory_Clicked
            Next
        End Sub
        Private Sub updateAlgorithmHistory()
            If testAllRunning Then Exit Sub
            Dim copyList As List(Of String)
            Dim maxHistory As Integer = 50
            If algHistory.Contains(AvailableAlgorithms.Text) Then
                ' make it the most recent
                copyList = New List(Of String)(algHistory)
                algHistory.Clear()
                algHistory.Add(AvailableAlgorithms.Text)
                For i = 0 To copyList.Count - 1
                    If algHistory.Contains(copyList(i)) = False Then algHistory.Add(copyList(i))
                Next
            Else
                copyList = New List(Of String)(algHistory)
                algHistory.Clear()
                algHistory.Add(AvailableAlgorithms.Text)
                For i = 0 To copyList.Count - 1
                    If algHistory.Contains(copyList(i)) = False Then algHistory.Add(copyList(i))
                    If algHistory.Count >= maxHistory Then Exit For
                Next
            End If
            RecentList.DropDownItems.Clear()
            settings.algorithmHistory = New List(Of String)
            For i = 0 To algHistory.Count - 1
                RecentList.DropDownItems.Add(algHistory(i))
                AddHandler RecentList.DropDownItems(i).Click, AddressOf algHistory_Clicked
                settings.algorithmHistory.Add(algHistory(i))
                If algHistory.Count >= maxHistory Then Exit For
            Next
        End Sub
        Public Sub New(Optional projectFile As String = "")
            InitializeComponent()

            ' Set the current directory to the project path (where .vbproj file is located)
            Dim projectDir As DirectoryInfo = New DirectoryInfo(Path.GetDirectoryName(projectFile))
            Directory.SetCurrentDirectory(projectDir.FullName + "/../")
            homeDir = Path.GetDirectoryName(projectDir.FullName) + "\"

            labels = New List(Of Label)({labelRGB, labelPointCloud, labelLeft, labelRight})
            For Each lab In labels
                lab.Text = ""
            Next
            pics = New List(Of PictureBox)({campicRGB, campicPointCloud, campicLeft, campicRight})

            settingsIO = New jsonIO(Path.Combine(homeDir, "Main\settings.json"))
        End Sub
        Private Sub OptionsButton_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
            If Options.ShowDialog() = DialogResult.OK Then
                If settings.workRes <> task.workRes And settings.cameraName <> task.cameraName Then
                    getLineCounts()

                    SaveSettings()
                    StopCamera()
                    StartCamera()
                    startAlgorithm()
                End If
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
        End Sub
        Private Sub Magnifier_Click(sender As Object, e As EventArgs) Handles Magnifier.Click

        End Sub
        Private Sub AtoZ_Click(sender As Object, e As EventArgs) Handles AtoZ.Click
            Dim groupsForm As New AtoZ()
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
            SaveSettings()
            StopCamera()
        End Sub
        Private Sub startAlgorithm()
            task = New VBClasses.VBtask()

            task.color = New cv.Mat(settings.workRes, cv.MatType.CV_8UC3, 0)
            task.pointCloud = New cv.Mat(settings.workRes, cv.MatType.CV_32FC3, 0)
            task.leftView = New cv.Mat(settings.workRes, cv.MatType.CV_8U, 0)
            task.rightView = New cv.Mat(settings.workRes, cv.MatType.CV_8U, 0)
            task.dstList = {task.color, task.pointCloud, task.leftView, task.rightView}

            task.settings = settings ' task is in a separate project and needs access to settings.
            task.main_hwnd = Me.Handle

            task.Initialize()
            task.MainUI_Algorithm = createAlgorithm(settings.algorithm)
            AlgDescription.Text = task.MainUI_Algorithm.desc
            MainToolStrip.Refresh()

            task.calibData = camera.calibData

            If CameraSwitching.Visible Then
                CamSwitchTimer.Enabled = False
                CameraSwitching.Visible = False
            End If
        End Sub
        Private Sub PausePlayButton_Click(sender As Object, e As EventArgs) Handles PausePlayButton.Click
            isPlaying = Not isPlaying

            ' Load and set the appropriate image
            If PausePlayButton.Image IsNot Nothing Then PausePlayButton.Image.Dispose()

            Dim filePath = Path.Combine(homeDir + "\Main\Data", If(isPlaying, "PauseButton.png", "Run.png"))
            PausePlayButton.Image = New Bitmap(filePath)

            ' Force the button to refresh
            PausePlayButton.Invalidate()

            If isPlaying Then StartCamera() Else StopCamera()
            TreeViewTimer.Enabled = True

            jumpToAlgorithm()
        End Sub
        Private Sub getLineCounts()
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
                       CStr(algorithmCountActive) + " algorithms " + " - " +
                       CStr(CInt(CodeLineCount / algorithmCount)) + " lines each (avg) - " + settings.cameraName

        End Sub
        Private Sub SaveSettings()
            updateAlgorithmHistory()
            settings.MainFormLeft = Me.Left
            settings.MainFormTop = Me.Top
            settings.MainFormWidth = Me.Width
            settings.MainFormHeight = Me.Height
            settings.algorithm = AvailableAlgorithms.Text
            settingsIO.Save(settings)
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
            Me.Show()

            camSwitchAnnouncement()
            getLineCounts()

            LoadAvailableAlgorithms()

            setupAlgorithmHistory()

            PausePlayButton.PerformClick()
        End Sub
        Private Sub TreeViewTimer_Tick(sender As Object, e As EventArgs) Handles TreeViewTimer.Tick
            If task Is Nothing Then Exit Sub
            If task.treeView IsNot Nothing Then task.treeView.Timer2_Tick(sender, e)
        End Sub

        Private Sub TestAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
            testAllRunning = True
        End Sub
        Private Sub AvailableAlgorithms_SelectedIndexChanged(sender As Object, e As EventArgs) Handles AvailableAlgorithms.SelectedIndexChanged
            settings.algorithm = AvailableAlgorithms.Text
            If task Is Nothing Then
                startAlgorithm()
            Else
                If Trim(AvailableAlgorithms.Text) = "" Then ' Skip the space between groups
                    If AvailableAlgorithms.SelectedIndex + 1 < AvailableAlgorithms.Items.Count Then
                        AvailableAlgorithms.Text = AvailableAlgorithms.Items(AvailableAlgorithms.SelectedIndex + 1)
                    Else
                        AvailableAlgorithms.Text = AvailableAlgorithms.Items(AvailableAlgorithms.SelectedIndex - 1)
                    End If
                End If

                If AvailableAlgorithms.Enabled Then
                    startAlgorithm()
                    updateAlgorithmHistory()
                End If
            End If
        End Sub
    End Class
End Namespace
