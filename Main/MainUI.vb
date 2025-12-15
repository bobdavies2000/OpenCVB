Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports VBClasses.VBClasses.vbc
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions

Namespace MainUI
    Partial Public Class MainUI
        Dim isPlaying As Boolean = False
        Dim homeDir As String = ""
        Public settingsIO As jsonIO
        Dim algHistory As New List(Of String)
        Dim recentMenu() As ToolStripMenuItem
        Dim labels As List(Of Label)
        Dim pics As New List(Of PictureBox)
        Dim picImages As New List(Of Image)
        Dim testAllRunning As Boolean = False
        Dim stopTestAll As Bitmap
        Dim PausePlay As Bitmap
        Dim runPlay As Bitmap
        Dim testAllToolbarBitmap As Bitmap

        Public Sub setAlgorithmSelection()
            If AvailableAlgorithms.Items.Contains(settings.algorithm) = False Then
                AvailableAlgorithms.SelectedIndex = 0
            Else
                AvailableAlgorithms.SelectedItem = settings.algorithm
            End If
        End Sub
        Private Sub algHistory_Clicked(sender As Object, e As EventArgs)
            Dim item = TryCast(sender, ToolStripMenuItem)
            settings.algorithm = item.Text
            setAlgorithmSelection()
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

            settingsIO = New jsonIO(Path.Combine(homeDir, "Main\settings.json"))
        End Sub
        Private Sub OptionsButton_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
            If TestAllTimer.Enabled Then TestAllButton_Click(sender, e)
            If Options.ShowDialog() = DialogResult.OK Then
                If settings.workRes <> algTask.workRes And settings.cameraName <> algTask.cameraName Then
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
        Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
            If isPlaying Then
                StopCamera()
                Dim count As Integer
                While camera IsNot Nothing
                    Thread.Sleep(1)
                    count += 1
                    If count = 10 Then Exit While
                End While
            End If
            SaveSettings()
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
            For i = 0 To 3
                labels(i).Location = Choose(i + 1, New Point(offset, MainToolStrip.Height),
                                                   New Point(w + offset, labelRGB.Top),
                                                   New Point(offset, topStart + labelHeight + h),
                                                   New Point(w + offset, labelLeft.Top))
                pics(i).Size = New Size(w, h)
                pics(i).Location = Choose(i + 1, New Point(offset, topStart + labelHeight),
                                                 New Point(w + offset, labels(0).Top + labelHeight),
                                                 New Point(offset, labelLeft.Top + labelHeight),
                                                 New Point(w + offset, labels(2).Top + labelHeight))
            Next

            StatusLabel.Location = New Point(offset, pics(2).Top + h)
            StatusLabel.Width = w * 2
        End Sub
        Private Sub TestAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
            TestAllButton.Image = If(TestAllButton.Text = "Test All", stopTestAll, testAllToolbarBitmap)
            If TestAllButton.Text = "Test All" Then
                Debug.WriteLine("")
                Debug.WriteLine("Starting 'TestAll' overnight run.")

                TestAllButton.Text = "Stop Test"
                AvailableAlgorithms.Enabled = False  ' the algorithm will be started in the testAllTimer event.
                TestAllTimer.Interval = 1
                TestAllTimer.Enabled = True
            Else
                Debug.WriteLine("Stopping 'TestAll' overnight run.")
                AvailableAlgorithms.Enabled = True
                TestAllTimer.Enabled = False
                TestAllButton.Text = "Test All"
            End If
            testAllRunning = TestAllTimer.Enabled
        End Sub
        Private Sub clickPic(sender As Object, e As EventArgs)
            If algTask Is Nothing Then Exit Sub
            'If algTask IsNot Nothing Then  if algTask.sharpgl IsNot Nothing Then sharpGL.Activate()
            If algTask IsNot Nothing Then If algTask.treeView IsNot Nothing Then algTask.treeView.Activate()
            If algTask IsNot Nothing Then If algTask.allOptions IsNot Nothing Then algTask.allOptions.Activate()
        End Sub
        Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            settings = settingsIO.Load()

            PausePlay = New Bitmap(homeDir + "Main/Data/PauseButton.png")
            stopTestAll = New Bitmap(homeDir + "Main/Data/stopTestAll.png")
            testAllToolbarBitmap = New Bitmap(homeDir + "Main/Data/testall.png")
            runPlay = New Bitmap(homeDir + "Main/Data/Run.png")

            For i = 0 To 3
                Dim pic = New PictureBox()
                AddHandler pic.DoubleClick, AddressOf campic_DoubleClick
                AddHandler pic.Click, AddressOf clickPic
                AddHandler pic.Paint, AddressOf Pic_Paint
                AddHandler pic.MouseDown, AddressOf CamPic_MouseDown
                AddHandler pic.MouseUp, AddressOf CamPic_MouseUp
                AddHandler pic.MouseMove, AddressOf CamPic_MouseMove
                pic.Tag = i
                pic.Image = New Bitmap(settings.displayRes.Width, settings.displayRes.Height)
                picImages.Add(pic.Image)
                pic.BackColor = Color.Black
                pic.Visible = True
                Me.Controls.Add(pic)

                pics.Add(pic)
            Next

            Me.Location = New Point(settings.MainFormLeft, settings.MainFormTop)
            Me.Size = New Size(settings.MainFormWidth, settings.MainFormHeight)
            Me.Show()

            camSwitchAnnouncement()
            getLineCounts()

            LoadAvailableAlgorithms()

            setupAlgorithmHistory()

            PausePlayButton.PerformClick()
        End Sub
        Private Sub PausePlayButton_Click(sender As Object, e As EventArgs) Handles PausePlayButton.Click
            If TestAllTimer.Enabled Then TestAllButton_Click(sender, e)

            isPlaying = Not isPlaying

            Dim filePath = Path.Combine(homeDir + "\Main\Data", If(isPlaying, "PauseButton.png", "Run.png"))
            PausePlayButton.Image = New Bitmap(filePath)

            If isPlaying Then StartCamera() Else StopCamera()
            setAlgorithmSelection()
        End Sub
        Private Sub Pic_Paint(sender As Object, e As PaintEventArgs)
            If algTask Is Nothing Then Exit Sub
            Dim g As Graphics = e.Graphics
            Dim pic = DirectCast(sender, PictureBox)
            If pic.Image Is Nothing Then Exit Sub
            g.ScaleTransform(1, 1)

            Dim displayImage As New cv.Mat
            Select Case pic.Tag
                Case 0
                    displayImage = algTask.color.Resize(New cv.Size(settings.displayRes.Width, settings.displayRes.Height))
                Case 1
                    displayImage = algTask.color.Resize(New cv.Size(settings.displayRes.Width, settings.displayRes.Height))
                Case 2
                    displayImage = algTask.leftView.Resize(New cv.Size(settings.displayRes.Width, settings.displayRes.Height))
                Case 3
                    displayImage = algTask.rightView.Resize(New cv.Size(settings.displayRes.Width, settings.displayRes.Height))
            End Select

            'Dim bitmap = cvext.BitmapConverter.ToBitmap(displayImage)
            'pic.Image?.Dispose()
            'pic.Image = bitmap
            'g.DrawImage(pic.Image, 0, 0)
            g.DrawImage(picImages(0), 0, 0)


            cv.Cv2.ImShow("displayimage", displayImage)



            Dim ratioX = pic.Width / settings.workRes.Width
            Dim ratioY = pic.Height / settings.workRes.Height

            Static myWhitePen As New Pen(Color.White)
            Static myBlackPen As New Pen(Color.Black)

            Static saveTrueData As List(Of VBClasses.VBClasses.TrueText)
            saveTrueData = New List(Of VBClasses.VBClasses.TrueText)(algTask.trueData)
            For Each tt In saveTrueData
                If tt.text Is Nothing Then Continue For
                If tt.text.Length > 0 And tt.picTag = pic.Tag Then
                    g.DrawString(tt.text, settings.fontInfo, New SolidBrush(Color.White),
                                     CSng(tt.pt.X * ratioX), CSng(tt.pt.Y * ratioY))
                End If
            Next
            displayImage.Dispose()
        End Sub

        Private Sub TestAllTimer_Tick(sender As Object, e As EventArgs) Handles TestAllTimer.Tick
            If AvailableAlgorithms.SelectedIndex + 1 >= AvailableAlgorithms.Items.Count Then
                AvailableAlgorithms.SelectedIndex = 0
            Else
                If AvailableAlgorithms.Items(AvailableAlgorithms.SelectedIndex + 1) = "" Then
                    AvailableAlgorithms.SelectedIndex += 2
                Else
                    AvailableAlgorithms.SelectedIndex += 1
                End If
            End If

            ' skip testing the XO_ algorithms (XO.vb)  They are obsolete.
            If AvailableAlgorithms.Text.StartsWith("XO_") Then AvailableAlgorithms.SelectedIndex = 0

            TestAllTimer.Interval = settings.testAllDuration
            Static startingAlgorithm = AvailableAlgorithms.Text
            startAlgorithm()
        End Sub
        Private Sub TreeViewTimer_Tick(sender As Object, e As EventArgs) Handles TreeViewTimer.Tick
            If isPlaying = False Then Exit Sub
            If algTask Is Nothing Then Exit Sub
            If algTask.treeView IsNot Nothing Then algTask.treeView.Timer2_Tick(sender, e)
        End Sub

        Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
            Static count As Integer
            Debug.WriteLine("test timer " + CStr(count))
            count += 1
        End Sub
        Private Sub AvailableAlgorithms_SelectedIndexChanged(sender As Object, e As EventArgs) Handles AvailableAlgorithms.SelectedIndexChanged
            settings.algorithm = AvailableAlgorithms.Text
            SaveSettings()
            If algTask Is Nothing Then
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
        Private Sub startAlgorithm()
            ' If algTask IsNot Nothing Then algTask.Dispose()


            algTask = New VBClasses.VBClasses.AlgorithmTask

            algTask.homeDir = homeDir
            algTask.color = New cv.Mat(settings.workRes, cv.MatType.CV_8UC3, 0)
            algTask.pointCloud = New cv.Mat(settings.workRes, cv.MatType.CV_32FC3, 0)
            algTask.leftView = New cv.Mat(settings.workRes, cv.MatType.CV_8U, 0)
            algTask.rightView = New cv.Mat(settings.workRes, cv.MatType.CV_8U, 0)
            algTask.gridRatioX = pics(0).Width / settings.workRes.Width
            algTask.gridRatioY = pics(0).Height / settings.workRes.Height

            algTask.settings = settings ' algTask is in a separate project and needs access to settings.
            algTask.main_hwnd = Me.Handle

            algTask.Initialize()
            algTask.MainUI_Algorithm = createAlgorithm(settings.algorithm)
            AlgDescription.Text = algTask.MainUI_Algorithm.desc
            MainToolStrip.Refresh()

            If algTask.calibData IsNot Nothing Then algTask.calibData = camera.calibData

            If CameraSwitching.Visible Then
                CamSwitchTimer.Enabled = False
                CameraSwitching.Visible = False
            End If
        End Sub

    End Class
End Namespace
